using System.Collections.Concurrent;

namespace MensattScraper.Telemetry;

public static class WebhookRelay
{
    public static readonly ConcurrentQueue<StringContent> Messages = new();
    private static readonly HttpClient HttpClient = new();

    static WebhookRelay()
    {
        new Thread(() =>
        {
            while (true)
            {
                if (Messages.IsEmpty)
                    Thread.Sleep(5000);
                else
                {
                    if (!Messages.TryDequeue(out var currentMessage)) break;
                    HttpClient.PostAsync(WebhookUrl, currentMessage);
                    Thread.Sleep(500);
                }
            }
        }).Start();
    }
}