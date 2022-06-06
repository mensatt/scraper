using System.Xml.Serialization;

namespace MensattScraper.SourceCompat;

public class Item
{
    [XmlElement(ElementName = "category")]
    public string? Category { get; set; }

    [XmlElement(ElementName = "title")]
    public string? Title { get; set; }

    [XmlElement(ElementName = "description")]
    public string? Description { get; set; }

    [XmlElement(ElementName = "beilagen")]
    public string? Beilagen { get; set; }

    [XmlElement(ElementName = "preis1")]
    public string? Preis1 { get; set; }

    [XmlElement(ElementName = "preis2")]
    public string? Preis2 { get; set; }

    [XmlElement(ElementName = "preis3")]
    public string? Preis3 { get; set; }

    [XmlElement(ElementName = "einheit")]
    public string? Einheit { get; set; }

    [XmlElement(ElementName = "piktogramme")]
    public string? Piktogramme { get; set; }

    [XmlElement(ElementName = "kj")]
    public string? Kj { get; set; }

    [XmlElement(ElementName = "kcal")]
    public string? Kcal { get; set; }

    [XmlElement(ElementName = "fett")]
    public string? Fett { get; set; }

    [XmlElement(ElementName = "gesfett")]
    public string? Gesfett { get; set; }

    [XmlElement(ElementName = "kh")]
    public string? Kh { get; set; }

    [XmlElement(ElementName = "zucker")]
    public string? Zucker { get; set; }

    [XmlElement(ElementName = "ballaststoffe")]
    public string? Ballaststoffe { get; set; }

    [XmlElement(ElementName = "eiweiss")]
    public string? Eiweiss { get; set; }

    [XmlElement(ElementName = "salz")]
    public string? Salz { get; set; }

    [XmlElement(ElementName = "foto")]
    public string? Foto { get; set; }
}