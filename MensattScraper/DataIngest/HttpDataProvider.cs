namespace MensattScraper.DataIngest;

public class HttpDataProvider : IDataProvider, IDisposable
{
    private readonly string _apiUrl;
    private readonly HttpClient _client;

    public HttpDataProvider(string dataUrl, uint delay = 1800)
    {
        _apiUrl = dataUrl;
        _client = new();
        GetDataDelayInSeconds = delay;
    }

    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream()
    {
        // Should be disposed when the stream is disposed
        var httpResponse = _client.GetAsync(_apiUrl).Result;

        yield return httpResponse.Content.ReadAsStream();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}