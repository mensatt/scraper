using MensattScraper.DatabaseSupport;
using MensattScraper.DataIngest;
using MensattScraper.SourceCompat;
using Microsoft.Extensions.Logging;

namespace MensattScraper;

public static class Program
{
    public static void Main()
    {
        Init();
    }

    private static void Init()
    {
        foreach (var apiUrl in ApiUrls)
        {
            Task.Factory.StartNew(() =>
            {
                // Creating multiple database wrappers on the same connection should be fine, as they are pooled
                IDatabaseWrapper databaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
                IDataProvider<Speiseplan> dataProvider = new HttpDataProvider<Speiseplan>(apiUrl);
                var scraper = new Scraper(databaseWrapper, dataProvider);
                scraper.Initialize();
                scraper.Scrape();
            }, TaskCreationOptions.LongRunning);
        }

        SharedLogger.LogInformation("Created all scraping tasks");

        new Thread(() =>
        {
            while (true)
            {
                var line = Console.ReadLine();
                if (line is null or not "quit") continue;
                // TODO: Proper shutdown
                Environment.Exit(0);
                return;
            }
        }).Start();
    }
}