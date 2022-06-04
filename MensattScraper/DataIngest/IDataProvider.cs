namespace MensattScraper.DataIngest;

public interface IDataProvider
{
    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream();
}