using System.Xml.Serialization;
using MensattScraper.SourceCompat;
using Npgsql;
using NpgsqlTypes;

namespace MensattScraper;

public class Program
{
    private const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    private const string DbConnection = "HOST=localhost;Port=7432;Username=mensatt;Password=mensatt;Database=mensatt";
    private const int ScrapeDelayInSeconds = 1800;

    private const string InsertDishSql = $"INSERT INTO dish (name) VALUES (@name) RETURNING id";

    private const string InsertOccurrenceSql = $"INSERT INTO occurrence (dish, date, kj, kcal, fat, saturated_fat, " +
                                               $"carbohydrates, sugar, fiber, protein, salt, price_student, " +
                                               $"price_staff, price_guest) " +
                                               $"VALUES (@dish, @date, @kj, @kcal, @fat, @saturated_fat, " +
                                               $"@carbohydrates, @sugar, @fiber, @protein, @salt, @price_student, " +
                                               $"@price_staff, @price_guest) RETURNING id";

    private const string InsertOccurrenceSideDishSql = $"INSERT INTO occurrence_side_dish VALUES (@occurrence, @dish)";
    private const string InsertOccurrenceTagSql = $"INSERT INTO occurrence_tag VALUES (@occurrence, @tag)";


    private static Program? _instance;

    private Program()
    {
    }

    private static Program Instance => _instance ??= new Program();

    public static void Main(string[] args)
    {
        Instance.Scrape();
    }

    private void Scrape()
    {
        var client = new HttpClient();
        var serializer = new XmlSerializer(typeof(Speiseplan));

        using var dbConnection = new NpgsqlConnection(DbConnection);
        dbConnection.Open();

        var insertDishCommand = PrepareInsertDishCommand(dbConnection);
        var insertOccurrenceCommand = PrepareInsertOccurrenceCommand(dbConnection);
        var insertOccurrenceSideDishCommand = PrepareInsertOccurrenceSideDishCommand(dbConnection);
        var insertOccurrenceTagCommand = PrepareInsertOccurrenceTagCommand(dbConnection);

        while (true)
        {
            Speiseplan? menu;
            //client.GetStreamAsync(ApiUrl).Result
            using (var reader = File.OpenRead("mensa-sued.xml"))
            {
                menu = (Speiseplan?) serializer.Deserialize(reader);

                if (menu is null)
                {
                    Console.Error.WriteLine("Could not deserialize menu");
                    continue;
                }

                var current = menu.Tags.OrderByDescending(x => x.Timestamp).First();

                foreach (var item in current.Items)
                {
                    FillDishCommand(insertDishCommand, item);
                    var dishUuid = (Guid) insertDishCommand.ExecuteScalar();
                    
                    FillOccurrenceCommand(insertOccurrenceCommand, current, item, dishUuid);
                    var occurrenceUuid = (Guid) insertOccurrenceCommand.ExecuteScalar();
                    
                    


                }
            }

            Thread.Sleep(ScrapeDelayInSeconds * 1000);
        }
    }

    private void FillDishCommand(NpgsqlCommand dishCommand, Item item) =>
        dishCommand.Parameters["name"].Value =
            Converter.ExtractElementFromTitle(item.Title, Converter.TitleElement.Name);

    private void FillOccurrenceCommand(NpgsqlCommand occurrenceCommand, Tag tag, Item item, Guid dish)
    {
        occurrenceCommand.Parameters["dish"].Value = dish;
        occurrenceCommand.Parameters["date"].Value = Converter.GetDateFromTimestamp(tag.Timestamp);
        occurrenceCommand.Parameters["kj"].Value = item.Kj;
        occurrenceCommand.Parameters["kcal"].Value = item.Kcal;
        occurrenceCommand.Parameters["fat"].Value = item.Fett;
        occurrenceCommand.Parameters["saturated_fat"].Value = item.Gesfett;
        occurrenceCommand.Parameters["carbohydrates"].Value = item.Kh;
        occurrenceCommand.Parameters["sugar"].Value = item.Zucker;
        occurrenceCommand.Parameters["fiber"].Value = item.Ballaststoffe;
        occurrenceCommand.Parameters["protein"].Value = item.Eiweiss;
        occurrenceCommand.Parameters["salt"].Value = item.Salz;
        occurrenceCommand.Parameters["price_student"].Value = Converter.EuroToCents(item.Preis1);
        occurrenceCommand.Parameters["price_staff"].Value = Converter.EuroToCents(item.Preis2);
        occurrenceCommand.Parameters["price_guest"].Value = Converter.EuroToCents(item.Preis3);
    }

    private void FillSideDishCommand(NpgsqlCommand sideDishCommand, Guid occurrence, Guid sideDish)
    {
        sideDishCommand.Parameters["occurrence"].Value = occurrence;
        sideDishCommand.Parameters["dish"].Value = sideDish;
    }

    private void FillTagCommand(NpgsqlCommand tagCommand, Guid occurrence, string tag)
    {
        tagCommand.Parameters["occurrence"].Value = occurrence;
        tagCommand.Parameters["tag"].Value = tag;
    }

    private NpgsqlCommand PrepareInsertDishCommand(NpgsqlConnection dbConnection)
    {
        var insertDishCommand = new NpgsqlCommand(InsertDishSql, dbConnection);
        insertDishCommand.Parameters.Add("name", NpgsqlDbType.Varchar);
        insertDishCommand.Prepare();
        return insertDishCommand;
    }

    private NpgsqlCommand PrepareInsertOccurrenceSideDishCommand(NpgsqlConnection dbConnection)
    {
        var insertOccurrenceSideDishCommand = new NpgsqlCommand(InsertOccurrenceSideDishSql, dbConnection);
        insertOccurrenceSideDishCommand.Parameters.Add("occurrence", NpgsqlDbType.Uuid);
        insertOccurrenceSideDishCommand.Parameters.Add("dish", NpgsqlDbType.Uuid);
        return insertOccurrenceSideDishCommand;
    }

    private NpgsqlCommand PrepareInsertOccurrenceTagCommand(NpgsqlConnection dbConnection)
    {
        var insertOccurrenceTagCommand = new NpgsqlCommand(InsertOccurrenceSideDishSql, dbConnection);
        insertOccurrenceTagCommand.Parameters.Add("occurrence", NpgsqlDbType.Uuid);
        insertOccurrenceTagCommand.Parameters.Add("tag", NpgsqlDbType.Varchar);
        return insertOccurrenceTagCommand;
    }

    private NpgsqlCommand PrepareInsertOccurrenceCommand(NpgsqlConnection dbConnection)
    {
        var insertOccurrenceCommand = new NpgsqlCommand(InsertOccurrenceSql, dbConnection);
        insertOccurrenceCommand.Parameters.Add("dish", NpgsqlDbType.Uuid);
        insertOccurrenceCommand.Parameters.Add("date", NpgsqlDbType.Date);
        insertOccurrenceCommand.Parameters.Add("kj", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("kcal", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("fat", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("saturated_fat", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("carbohydrates", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("sugar", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("fiber", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("protein", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("salt", NpgsqlDbType.Real);
        insertOccurrenceCommand.Parameters.Add("price_student", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("price_staff", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Parameters.Add("price_guest", NpgsqlDbType.Integer);
        insertOccurrenceCommand.Prepare();
        return insertOccurrenceCommand;
    }
}