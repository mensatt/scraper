using System.Diagnostics;

namespace MensattScraper.DataIngest;

public class HttpDataProvider : IDataProvider, IDisposable
{
    private readonly string _apiUrl;
    private readonly HttpClient _client;
    private readonly uint _delay;

    public HttpDataProvider(string dataUrl, uint delay = 1800)
    {
        _apiUrl = dataUrl;
        _client = new();
        _delay = delay;
    }

    public uint GetDataDelayInSeconds => _delay;

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