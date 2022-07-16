using System.Text;
using Microsoft.Extensions.Logging;

namespace MensattScraper.Telemetry;

public class WebhookSender : ILogger
{
    private readonly HttpClient _httpClient;

    public WebhookSender()
    {
        _httpClient = new();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _httpClient.PostAsync(WebhookUrl,
            new StringContent(
                $"{{\"content\" : \"{DateTime.UtcNow.ToString("HH:mm:ss")} {logLevel}: {formatter.Invoke(state, exception)}\"}}",
                Encoding.UTF8,
                "application/json"));

    public bool IsEnabled(LogLevel logLevel) => throw new NotImplementedException();
    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();
}