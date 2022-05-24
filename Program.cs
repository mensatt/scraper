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


    private static Program? _instance;

    private NpgsqlConnection _dbConnection;

    private NpgsqlCommand _insertDishCommand;
    private NpgsqlCommand _insertOccurrenceCommand;
    private NpgsqlCommand _insertOccurrenceSideDishCommand;
    private NpgsqlCommand _insertOccurrenceTagCommand;
    private NpgsqlCommand _selectDishCommand;

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

        _insertDishCommand = PrepareInsertDishCommand();
        _insertOccurrenceCommand = PrepareInsertOccurrenceCommand();
        _insertOccurrenceSideDishCommand = PrepareInsertOccurrenceSideDishCommand();
        _insertOccurrenceTagCommand = PrepareInsertOccurrenceTagCommand();
        _selectDishCommand = PrepareSelectIdForDishCommand();
    }

    private void Scrape()
    {
        var client = new HttpClient();
        var serializer = new XmlSerializer(typeof(Speiseplan));

        while (true)
        {
            //client.GetStreamAsync(ApiUrl).Result
            using var reader = File.OpenRead("mensa-sued.xml");

            var menu = (Speiseplan?) serializer.Deserialize(reader);

            if (menu is null)
            {
                Console.Error.WriteLine("Could not deserialize menu");
                continue;
            }

            foreach (var current in menu.Tags)
            {



                //var current = menu.Tags.OrderByDescending(x => x.Timestamp).First();

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


                    FillOccurrenceCommand(current, item, dishUuid.Value, ReviewStatus.AWAITING_APPROVAL);

                    var occurrenceUuid = (Guid) _insertOccurrenceCommand.ExecuteScalar();

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
            }


            // Thread.Sleep(ScrapeDelayInSeconds * 1000);
            break;
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
        _insertOccurrenceCommand.Parameters["kj"].Value = Converter.FloatToInt(item.Kj);
        _insertOccurrenceCommand.Parameters["kcal"].Value = Converter.FloatToInt(item.Kcal);
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
        _selectDishCommand.Parameters["name"].Value = Converter.ExtractElementFromTitle(name, Converter.TitleElement.Name);
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
}