using System.Collections;
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
        var workers = new List<Scraper>();

        foreach (var apiUrl in ApiUrls)
        {
            new Thread(() =>
            {
                var identifier = apiUrl[(apiUrl.LastIndexOf('/') + 1)..].Replace(".xml", string.Empty);
                Console.WriteLine($"Creating worker for {identifier}");
                // Creating multiple database wrappers on the same connection should be fine, as they are pooled
                IDatabaseWrapper databaseWrapper = new NpgsqlDatabaseWrapper(DbConnection);
                IDataProvider<Speiseplan> dataProvider = new HttpDataProvider<Speiseplan>(apiUrl, WorkerFetchDelay,
                    Path.Combine(ContentDirectory, $"content_{identifier}"));
                var scraper = new Scraper(databaseWrapper, dataProvider, identifier);
                lock (workers)
                {
                    workers.Add(scraper);
                }
                scraper.Initialize();
                scraper.Scrape();
            }).Start();
            // Space out workers to make 1. the api and 2. discord happy
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        SharedLogger.LogInformation("Created all scraping threads");

        while (true)
        {

            lock (workers)
            {
                foreach (var scraper in workers)
                    scraper.PrintTelemetry();
            }

            Thread.Sleep(TimeSpan.FromMinutes(5));
        }
    }
}
