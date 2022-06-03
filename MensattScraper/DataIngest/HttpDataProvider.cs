using System.Diagnostics;

namespace MensattScraper.DataIngest;

public class HttpDataProvider : IDataProvider, IDisposable
{
    private readonly string _apiUrl;
    private readonly HttpClient _client;

    public HttpDataProvider(string dataUrl)
    {
        _apiUrl = dataUrl;
        _client = new();
    }

    public bool HasNextStream() => true;

    public Stream RetrieveStream()
    {
        if (!HasNextStream())
        {
            Trace.Assert(false, "This data provider has no more elements");
        }

        // Should be disposed when the stream is disposed
        var httpResponse = _client.GetAsync(_apiUrl).Result;

        return httpResponse.Content.ReadAsStream();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}