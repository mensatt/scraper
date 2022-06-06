using System.Xml.Serialization;

namespace MensattScraper.SourceCompat;

public class DayTag
{
    [XmlAttribute("timestamp")]
    public int Timestamp { get; set; }

    [XmlElement("item")]
    public Item[]? Items { get; set; }
}