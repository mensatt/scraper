using System.Xml.Serialization;

namespace MensattScraper.DataIngest;

public interface IDataProvider<out T>
{
    // Directory to output processed streams to
    public string? CopyLocation { get; }

    public uint GetDataDelayInSeconds { get; }

    public IEnumerable<Stream> RetrieveStream();

    public IEnumerable<T?> RetrieveUnderlying(XmlSerializer serializer)
    {
        foreach (var currentStream in RetrieveStream())
            using (currentStream)
            {
                if (CopyLocation is not null)
                {
                    using var outputFile =
                        File.Create(
                            $"{CopyLocation}{Path.DirectorySeparatorChar}{DateTime.UtcNow.ToString("yyyy-MM-dd_HH_mm_ss.fff")}.xml");
                    currentStream.CopyTo(outputFile);
                    currentStream.Position = 0;
                }

                yield return (T?) serializer.Deserialize(currentStream);
            }
    }
}