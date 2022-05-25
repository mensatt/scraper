using System.Xml.Serialization;

namespace MensattScraper.SourceCompat;

public class Tag
{
    [XmlAttribute("timestamp")]
    public int Timestamp { get; set; }

    [XmlElement("item")]
    public Item[]? Items { get; set; }
}