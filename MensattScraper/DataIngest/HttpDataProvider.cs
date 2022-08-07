﻿using Microsoft.Extensions.Logging;

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


    // TODO: Document this
    public string? CopyLocation => ApiUrl.Contains("/en/")
        ? Path.Combine(ContentDirectory, "content_en")
        : Path.Combine(ContentDirectory, "content");

    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream()
    {
        // Should be disposed when the stream is disposed
        var httpResponse = _client.GetAsync(ApiUrl).Result;
        SharedLogger.LogInformation($"Queried {ApiUrl} for new data, received: {httpResponse.StatusCode}");

        yield return httpResponse.Content.ReadAsStream();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}