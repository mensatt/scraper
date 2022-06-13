using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.SourceCompat;
using Microsoft.Extensions.Logging;

namespace MensattScraper;

public static class Program
{
    public static void Main()
    {
        SharedLogger.LogInformation("Starting program");
        StartScraper();
    }

    private static void StartScraper()
    {
        IDatabaseWrapper privateDatabaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
        privateDatabaseWrapper.ConnectAndPrepare();
        // Creating multiple database wrappers on the same connection should be fine, as they are pooled
        IDatabaseWrapper databaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
        IDataProvider<Speiseplan> dataProvider = new HttpDataProvider<Speiseplan>(ApiUrl);
        using var scraper = new Scraper(databaseWrapper, dataProvider);
        scraper.Initialize();
        new Thread(() =>
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line is null or not "quit") continue;
                return;
            }
        }).Start();
        scraper.Scrape();
    }
}