using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using MensattScraper.DestinationCompat;
using MensattScraper.SourceCompat;
using Microsoft.VisualBasic.CompilerServices;
using Npgsql;
using NpgsqlTypes;

namespace MensattScraper;

public class Program
{
    private const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    private const string DbConnection = "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt";
    private const int ScrapeDelayInSeconds = 1800;

    private const string SelectIdForDishSql = $"SELECT id FROM dish WHERE name=@name";

    private const string InsertDishSql =
        $"INSERT INTO dish (name) VALUES (@name) ON CONFLICT (name) DO NOTHING RETURNING id";

    private const string InsertOccurrenceSql =
        $"INSERT INTO occurrence (dish, date, review_status, kj, kcal, fat, saturated_fat, " +
        $"carbohydrates, sugar, fiber, protein, salt, price_student, " +
        $"price_staff, price_guest) " +
        $"VALUES (@dish, @date, @review_status, @kj, @kcal, @fat, @saturated_fat, " +
        $"@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
        $"@price_staff, @price_guest) RETURNING id";

    private const string InsertOccurrenceSideDishSql =
        $"INSERT INTO occurrence_side_dishes VALUES (@occurrence, @dish)";

    private const string InsertOccurrenceTagSql = $"INSERT INTO occurrence_tag VALUES (@occurrence, @tag)";

    private const string DeleteOccurrenceByUuidSql = $"DELETE FROM occurrence WHERE id=@id";

    private static Program? _instance;

    private NpgsqlConnection _dbConnection;


    private NpgsqlCommand _selectDishCommand;

    private NpgsqlCommand _insertDishCommand;
    private NpgsqlCommand _insertOccurrenceCommand;
    private NpgsqlCommand _insertOccurrenceSideDishCommand;
    private NpgsqlCommand _insertOccurrenceTagCommand;

    private NpgsqlCommand _deleteOccurrenceCommand;

    private Program()
    {
    }

    private static Program Instance => _instance ??= new Program();

    public static void Main(string[] args)
    {
        Instance.InitDbConnection();

        Instance.Scrape();
    }

    private void InitDbConnection()
    {
        _dbConnection = new NpgsqlConnection(DbConnection);
        _dbConnection.Open();

        _dbConnection.TypeMapper.MapEnum<ReviewStatus>("review_status");

        _selectDishCommand = PrepareSelectIdForDishCommand();
        _insertDishCommand = PrepareInsertDishCommand();
        _insertOccurrenceCommand = PrepareInsertOccurrenceCommand();
        _insertOccurrenceSideDishCommand = PrepareInsertOccurrenceSideDishCommand();
        _insertOccurrenceTagCommand = PrepareInsertOccurrenceTagCommand();
        _deleteOccurrenceCommand = PrepareDeleteOccurrenceCommand();
    }

    private void Scrape()
    {
        var client = new HttpClient();
        var serializer = new XmlSerializer(typeof(Speiseplan));

        // Dict<01.01.1970, List<Dish UUID -> Occurrence UUID>>
        var dailyOccurrences = new Dictionary<DateOnly, List<Tuple<Guid, Guid>>>();

        while (true)
        {
            //client.GetStreamAsync(ApiUrl).Result
            using var reader = File.OpenRead("testing.xml");

            var menu = (Speiseplan?) serializer.Deserialize(reader);

            if (menu is null)
            {
                Console.Error.WriteLine("Could not deserialize menu");
                continue;
            }

            foreach (var current in menu.Tags)
            {
                var currentDay = Converter.GetDateFromTimestamp(current.Timestamp);
                var isInFarFuture = DateOnly.FromDateTime(DateTime.Now).AddDays(2) < currentDay;
                bool firstPullOfTheDay;
                if (!dailyOccurrences.ContainsKey(currentDay))
                {
                    dailyOccurrences.Add(currentDay, new List<Tuple<Guid, Guid>>());
                    firstPullOfTheDay = true;
                }
                else
                {
                    firstPullOfTheDay = false;
                }

                var dailyDishes = new HashSet<Guid>();

                foreach (var item in current.Items)
                {
                    FillDishCommand(item.Title);
                    var dishUuid = (Guid?) _insertDishCommand.ExecuteScalar();

                    // RETURNING id does not get executed, if there is a conflict
                    // Thus we need to fetch the existing UUID explicitly
                    if (!dishUuid.HasValue)
                    {
                        FillSelectDishCommand(item.Title);
                        dishUuid = (Guid?) _selectDishCommand.ExecuteScalar();
                    }

                    dailyDishes.Add(dishUuid.Value);

                    var occurrenceStatus = firstPullOfTheDay ? ReviewStatus.AWAITING_APPROVAL : ReviewStatus.UPDATED;

                    // If this after the day after tomorrow, we just delete occurrences (where the dish doesn't exist anymore)
                    if (!firstPullOfTheDay && isInFarFuture)
                    {
                        var savedDishOccurrence = dailyOccurrences[currentDay].Find(x => x.Item1 == dishUuid);

                        // If we have seen this dish already, skip it
                        if (savedDishOccurrence is not null)
                            continue; // Update here eventually

                        // New dish, so it needs to await approval
                        occurrenceStatus = ReviewStatus.AWAITING_APPROVAL;
                    }

                    FillOccurrenceCommand(current, item, dishUuid.Value, occurrenceStatus);
                    var occurrenceUuid = (Guid) _insertOccurrenceCommand.ExecuteScalar();
                    dailyOccurrences[currentDay].Add(new(dishUuid.Value, occurrenceUuid));

                    foreach (var tag in Converter.ExtractSingleTagsFromTitle(item.Title))
                    {
                        FillTagCommand(occurrenceUuid, tag);
                        _insertOccurrenceTagCommand.ExecuteNonQuery();
                    }

                    foreach (var sideDish in Converter.GetSideDishes(item.Beilagen))
                    {
                        FillSelectDishCommand(sideDish);
                        var sideDishUuid = (Guid?) _selectDishCommand.ExecuteScalar();
                        if (!sideDishUuid.HasValue)
                        {
                            FillDishCommand(sideDish);
                            FillSideDishCommand(occurrenceUuid, (Guid) _insertDishCommand.ExecuteScalar());
                        }
                        else
                        {
                            FillSideDishCommand(occurrenceUuid, sideDishUuid.Value);
                        }

                        _insertOccurrenceSideDishCommand.ExecuteNonQuery();
                    }
                }

                // Delete all dishes, that were removed on a day which is more than two days in the future
                if (isInFarFuture)
                {
                    foreach (var previousDish in dailyOccurrences[currentDay])
                    {
                        // If this dish does not exist in the current XML, delete it
                        if (!dailyDishes.Contains(previousDish.Item1))
                        {
                            // Delete
                            FillDeleteOccurrenceCommand(previousDish.Item1);
                            try
                            {
                                _deleteOccurrenceCommand.ExecuteNonQuery();
                            }
                            catch
                            {
                                Console.Error.WriteLine("Could not delete old occurrence");
                            }
                        }
                    }
                }
            }


            // Thread.Sleep(ScrapeDelayInSeconds * 1000);
        }

        _dbConnection.Dispose();
    }

    private void FillDishCommand(string title) =>
        _insertDishCommand.Parameters["name"].Value =
            Converter.ExtractElementFromTitle(title, Converter.TitleElement.Name);

    private void FillOccurrenceCommand(Tag tag, Item item, Guid dish, ReviewStatus status)
    {
        _insertOccurrenceCommand.Parameters["dish"].Value = dish;
        _insertOccurrenceCommand.Parameters["date"].Value = Converter.GetDateFromTimestamp(tag.Timestamp);
        _insertOccurrenceCommand.Parameters["review_status"].Value = status;
        _insertOccurrenceCommand.Parameters["kj"].Value = Converter.FloatToInt(item.Kj) / 1000;
        _insertOccurrenceCommand.Parameters["kcal"].Value = Converter.FloatToInt(item.Kcal) / 1000;
        _insertOccurrenceCommand.Parameters["fat"].Value = Converter.FloatToInt(item.Fett);
        _insertOccurrenceCommand.Parameters["saturated_fat"].Value = Converter.FloatToInt(item.Gesfett);
        _insertOccurrenceCommand.Parameters["carbohydrates"].Value = Converter.FloatToInt(item.Kh);
        _insertOccurrenceCommand.Parameters["sugar"].Value = Converter.FloatToInt(item.Zucker);
        _insertOccurrenceCommand.Parameters["fiber"].Value = Converter.FloatToInt(item.Ballaststoffe);
        _insertOccurrenceCommand.Parameters["protein"].Value = Converter.FloatToInt(item.Eiweiss);
        _insertOccurrenceCommand.Parameters["salt"].Value = Converter.FloatToInt(item.Salz);
        _insertOccurrenceCommand.Parameters["price_student"].Value = Converter.FloatToInt(item.Preis1);
        _insertOccurrenceCommand.Parameters["price_staff"].Value = Converter.FloatToInt(item.Preis2);
        _insertOccurrenceCommand.Parameters["price_guest"].Value = Converter.FloatToInt(item.Preis3);
    }

    private void FillSideDishCommand(Guid occurrence, Guid sideDish)
    {
        _insertOccurrenceSideDishCommand.Parameters["occurrence"].Value = occurrence;
        _insertOccurrenceSideDishCommand.Parameters["dish"].Value = sideDish;
    }

    private void FillTagCommand(Guid occurrence, string tag)
    {
        _insertOccurrenceTagCommand.Parameters["occurrence"].Value = occurrence;
        _insertOccurrenceTagCommand.Parameters["tag"].Value = tag;
    }

    private void FillSelectDishCommand(string name)
    {
        _selectDishCommand.Parameters["name"].Value =
            Converter.ExtractElementFromTitle(name, Converter.TitleElement.Name);
    }

    private void FillDeleteOccurrenceCommand(Guid id)
    {
        _deleteOccurrenceCommand.Parameters["id"].Value = id;
    }

    private NpgsqlCommand PrepareInsertDishCommand()
    {
        var insertDishCommand = new NpgsqlCommand(InsertDishSql, _dbConnection);
        insertDishCommand.Parameters.Add("name", NpgsqlDbType.Varchar);
        insertDishCommand.Prepare();
        return insertDishCommand;
    }

    private NpgsqlCommand PrepareInsertOccurrenceSideDishCommand()
    {
        var insertOccurrenceSideDishCommand = new NpgsqlCommand(InsertOccurrenceSideDishSql, _dbConnection);
        insertOccurrenceSideDishCommand.Parameters.Add("occurrence", NpgsqlDbType.Uuid);
        insertOccurrenceSideDishCommand.Parameters.Add("dish", NpgsqlDbType.Uuid);
        return insertOccurrenceSideDishCommand;
    }

    private NpgsqlCommand PrepareInsertOccurrenceTagCommand()
    {
        var insertOccurrenceTagCommand = new NpgsqlCommand(InsertOccurrenceTagSql, _dbConnection);
        insertOccurrenceTagCommand.Parameters.Add("occurrence", NpgsqlDbType.Uuid);
        insertOccurrenceTagCommand.Parameters.Add("tag", NpgsqlDbType.Varchar);
        return insertOccurrenceTagCommand;
    }

    private NpgsqlCommand PrepareInsertOccurrenceCommand()
    {
        var insertOccurrenceCommand = new NpgsqlCommand(InsertOccurrenceSql, _dbConnection);
        insertOccurrenceCommand.Parameters.Add("dish", NpgsqlDbType.Uuid);
        insertOccurrenceCommand.Parameters.Add("date", NpgsqlDbType.Date);
        insertOccurrenceCommand.Parameters.Add("review_status", NpgsqlDbType.Unknown);
        insertOccurrenceCommand.Parameters.Add("kj", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("kcal", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("fat", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("saturated_fat", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("carbohydrates", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("sugar", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("fiber", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("protein", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("salt", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("price_student", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("price_staff", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("price_guest", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Prepare();
        return insertOccurrenceCommand;
    }

    private NpgsqlCommand PrepareSelectIdForDishCommand()
    {
        var selectIdCommand = new NpgsqlCommand(SelectIdForDishSql, _dbConnection);
        selectIdCommand.Parameters.Add("name", NpgsqlDbType.Varchar);
        selectIdCommand.Prepare();
        return selectIdCommand;
    }

    private NpgsqlCommand PrepareDeleteOccurrenceCommand()
    {
        var deleteOccurrenceCommand = new NpgsqlCommand(DeleteOccurrenceByUuidSql, _dbConnection);
        deleteOccurrenceCommand.Parameters.Add("id", NpgsqlDbType.Uuid);
        deleteOccurrenceCommand.Prepare();
        return deleteOccurrenceCommand;
    }
}