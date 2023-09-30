using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MensattScraper.Telemetry;

public class WebhookSender : ILogger
{
    private readonly string _identifier;

    public WebhookSender(string identifier)
    {
        _identifier = identifier;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel > LogLevel.Debug)
        {
            WebhookRelay.Messages.Enqueue(new(
                $"{{\"content\" : \"{GenerateWrap(logLevel, (Debugger.IsAttached ? "[Debug] " : string.Empty) + $"{DateTime.Now:HH:mm:ss} {logLevel}: [{_identifier}]\\n{formatter.Invoke(state, exception)}")}\"}}",
                Encoding.UTF8,
                "application/json"));
        }
    }

    private static string GenerateWrap(LogLevel level, string x) => level switch
    {
        LogLevel.Trace or LogLevel.Debug or LogLevel.Information => $@"```ini\n{x}\n```",
        LogLevel.Warning => $@"```fix\n{x}\n```",
        LogLevel.Error or LogLevel.Critical => $@"```diff\n-{x}\n```",
        LogLevel.None => x,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
}
