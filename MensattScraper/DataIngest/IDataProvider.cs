using System.Xml.Serialization;

namespace MensattScraper.DataIngest;

public interface IDataProvider<out T>
{
    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream();

    public IEnumerable<T?> RetrieveUnderlying(XmlSerializer serializer)
    {
        foreach (var currentStream in RetrieveStream())
            using (currentStream)
            {
                yield return (T?) serializer.Deserialize(currentStream);
            }
    }
}