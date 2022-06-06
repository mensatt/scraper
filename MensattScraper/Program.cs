using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.DestinationCompat;

namespace MensattScraper;

public class Program
{
    private const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    private const string DbConnection = "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt";

    private static Program? _instance;

    private static Program Instance => _instance ??= new();

    public static void Main(string[] args)
    {
        Instance.StartScraper();
    }

    private void StartScraper()
    {
        IDatabaseWrapper databaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
        IDataProvider dataProvider = new HttpDataProvider(ApiUrl);
        using var scraper = new Scraper(databaseWrapper, dataProvider);
        scraper.Initialize();
        scraper.Scrape();
    }
}