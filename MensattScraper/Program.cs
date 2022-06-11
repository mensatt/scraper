using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.SourceCompat;

namespace MensattScraper;

public static class Program
{
    public static void Main()
    {
        StartScraper();
    }

    private static void StartScraper()
    {
        IDatabaseWrapper privateDatabaseWrapper = new NpgsqlDatabaseWrapper(Configuration.DbConnection);
        privateDatabaseWrapper.ConnectAndPrepare();
        // Creating multiple database wrappers on the same connection should be fine, as they are pooled
        IDatabaseWrapper databaseWrapper = new NpgsqlDatabaseWrapper(Configuration.DbConnection);
        IDataProvider<Speiseplan> dataProvider = new HttpDataProvider<Speiseplan>(Configuration.ApiUrl);
        using var scraper = new Scraper(databaseWrapper, dataProvider);
        scraper.Initialize();
        scraper.Scrape();
    }
}