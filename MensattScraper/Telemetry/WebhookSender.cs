using System.Text;
using Microsoft.Extensions.Logging;

namespace MensattScraper.Telemetry;

public class WebhookSender : ILogger
{
    private readonly Thread _slowSenderThread;
    private readonly Queue<StringContent> _messages;

    public WebhookSender()
    {
        HttpClient httpClient = new();
        _messages = new();

        _slowSenderThread = new(() =>
        {
            while (_messages.Count != 0)
            {
                var currentMessage = _messages.Dequeue();
                var postedMessage = httpClient.PostAsync(WebhookUrl, currentMessage).Result;
                if (!postedMessage.IsSuccessStatusCode)
                {
                    SharedLogger.LogError(
                        $"Could not send webhook message. StatusCode: {postedMessage.StatusCode}, Reason: {postedMessage.ReasonPhrase}, Response: {postedMessage.Content.ToString()}");
                }

                // TODO: Improve rate limit handling
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
            }
        });
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _messages.Enqueue(new(
            $"{{\"content\" : \"{GenerateWrap(logLevel, $"{DateTime.UtcNow.ToString("HH:mm:ss")} {logLevel}: {formatter.Invoke(state, exception)}")}\"}}",
            Encoding.UTF8,
            "application/json"));
        if (!_slowSenderThread.IsAlive)
            _slowSenderThread.Start();
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