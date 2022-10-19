using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MensattScraper.Telemetry;

public class WebhookSender : ILogger
{
    private readonly HttpClient _httpClient;
    private readonly Queue<StringContent> _messages;
    private bool _isTaskRunning;

    public WebhookSender()
    {
        _httpClient = new();
        _messages = new();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _messages.Enqueue(new(
            $"{{\"content\" : \"{GenerateWrap(logLevel, (Debugger.IsAttached ? "[Debug] " : string.Empty) + $"{DateTime.UtcNow.ToString("HH:mm:ss")} {logLevel}: {formatter.Invoke(state, exception)}")}\"}}",
            Encoding.UTF8,
            "application/json"));
        if (!_isTaskRunning)
        {
            _isTaskRunning = true;
            Task.Run(() =>
            {
                while (_messages.Count != 0)
                {
                    var currentMessage = _messages.Dequeue();
                    _httpClient.PostAsync(WebhookUrl, currentMessage);
                    Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();
                }

                _isTaskRunning = false;
            });
        }
    }

    private static string GenerateWrap(LogLevel level, string x) => level switch
    {
        LogLevel.Trace or LogLevel.Debug or LogLevel.Information => $"```ini\\n[{x}]\\n```",
        LogLevel.Warning => $"```fix\\n{x}\\n```",
        LogLevel.Error or LogLevel.Critical => $"```diff\\n-{x}\\n```",
        LogLevel.None => x,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
}