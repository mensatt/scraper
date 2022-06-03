namespace MensattScraper.DataIngest;

public interface IDataProvider
{
    public uint GetDataDelayInSeconds { get; }

    public bool HasNextStream();

    public Stream RetrieveStream();
}