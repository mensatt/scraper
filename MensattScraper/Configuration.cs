using MensattScraper.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace MensattScraper;

internal static class Configuration
{
    internal static readonly string[] ApiUrls =
        Environment.GetEnvironmentVariable("MENSATT_SCRAPER_API_URL")?.Split(";") ??
        throw new ArgumentException("Api url not set");

    internal static readonly string DbConnection = Environment.GetEnvironmentVariable("MENSATT_SCRAPER_DB") ??
                                                   throw new ArgumentException("Database string not set");

    internal static readonly string? WebhookUrl = Environment.GetEnvironmentVariable("MENSATT_SCRAPER_WEBHOOK") ??
                                                  null;

    internal static readonly string ContentDirectory = Environment.GetEnvironmentVariable("MENSATT_SCRAPER_CONTENT") ??
                                                       throw new ArgumentException("Content directory not set");

    internal static readonly uint WorkerFetchDelay =
        uint.Parse(Environment.GetEnvironmentVariable("MENSATT_SCRAPER_DELAY") ?? "450"); // 7.5 minutes

    internal static readonly ILogger SharedLogger = CreateSimpleLogger("Shared");

    internal static ILogger CreateSimpleLogger(string categoryName) => new LogDelegator(
        LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.ColorBehavior = LoggerColorBehavior.Enabled;
                options.TimestampFormat = "hh:mm:ss ";
            });
        }).CreateLogger(categoryName), WebhookUrl != null ? new WebhookSender(categoryName) : null);
}
