using System.Xml.Serialization;

namespace MensattScraper.Compat;

[XmlRoot("speiseplan")]
public class Speiseplan
{
    [XmlAttribute("locationId")]
    public int LocationId { get; set; }

    [XmlElement("tag")]
    public Tag[] Tags { get; set; }
}