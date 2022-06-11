namespace MensattScraper.DataIngest;

public class HttpDataProvider<T> : IDataProvider<T>, IDisposable
{
    private readonly HttpClient _client;

    internal string ApiUrl { get; }

    public HttpDataProvider(string dataUrl, uint delay = 1800)
    {
        ApiUrl = dataUrl;
        _client = new();
        GetDataDelayInSeconds = delay;
    }


    public string? CopyLocation => "content";
    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream()
    {
        // Should be disposed when the stream is disposed
        var httpResponse = _client.GetAsync(ApiUrl).Result;

        yield return httpResponse.Content.ReadAsStream();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}