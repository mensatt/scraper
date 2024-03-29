﻿using System.Xml.Serialization;

namespace MensattScraper.SourceCompat;

[XmlRoot("speiseplan")]
public class Speiseplan
{
    [XmlAttribute("locationId")]
    public int LocationId { get; set; }

    [XmlElement("tag")]
    public DayTag[]? Tags { get; set; }
}