using Microsoft.Extensions.Logging;

namespace MensattScraper.DataIngest;

public class HttpDataProvider<T> : IDataProvider<T>, IDisposable
{
    private readonly HttpClient _client;

    internal string ApiUrl { get; }

    public string CopyLocation { get; }

    public HttpDataProvider(string dataUrl, uint delay, string copyLocation)
    {
        ApiUrl = dataUrl;
        _client = new();
        GetDataDelayInSeconds = delay;
        CopyLocation = copyLocation;
    }

    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream()
    {
        while (true)
        {
            // Should be disposed when the stream is disposed
            var httpResponse = _client.GetAsync(ApiUrl).Result;
            SharedLogger.LogInformation("Queried {ApiUrl} for new data, received: {HttpResponseStatusCode}", ApiUrl,
                httpResponse.StatusCode);

            yield return httpResponse.Content.ReadAsStream();
        }
        // ReSharper disable once IteratorNeverReturns
        // Warning can be disabled, as the same url needs to be queried endlessly
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
