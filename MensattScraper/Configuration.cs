using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace MensattScraper;

internal static class Configuration
{
    internal static readonly string ApiUrl = Environment.GetEnvironmentVariable("MENSATT_SCRAPER_API_URL") ??
                                             throw new ArgumentException("Api url not set");
    internal static readonly string DbConnection = Environment.GetEnvironmentVariable("MENSATT_SCRAPER_DB") ??
                                                   throw new ArgumentException("Database string not set");

    internal static readonly ILogger SharedLogger = LoggerFactory.Create(builder =>
    {
        builder.AddSimpleConsole(options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.TimestampFormat = "hh:mm:ss ";
        });
    }).CreateLogger("Scraper");


}