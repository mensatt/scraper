using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace MensattScraper;

internal static class Configuration
{
    internal const string ApiUrl = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";
    internal const string DbConnection = "HOST=localhost;Port=8080;Username=mensatt;Password=mensatt;Database=mensatt";

    internal static readonly ILogger SharedLogger = LoggerFactory.Create(builder =>
    {
        builder.AddSimpleConsole(options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.TimestampFormat = "hh:mm:ss ";
        });
    }).CreateLogger("Scraper");
}