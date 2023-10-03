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

    public override string ToString()
    {
        return
            $"{nameof(Category)}: {Category}, {nameof(Title)}: {Title}, {nameof(Description)}: {Description}, {nameof(Beilagen)}: {Beilagen}, {nameof(Preis1)}: {Preis1}, {nameof(Preis2)}: {Preis2}, {nameof(Preis3)}: {Preis3}, {nameof(Einheit)}: {Einheit}, {nameof(Piktogramme)}: {Piktogramme}, {nameof(Kj)}: {Kj}, {nameof(Kcal)}: {Kcal}, {nameof(Fett)}: {Fett}, {nameof(Gesfett)}: {Gesfett}, {nameof(Kh)}: {Kh}, {nameof(Zucker)}: {Zucker}, {nameof(Ballaststoffe)}: {Ballaststoffe}, {nameof(Eiweiss)}: {Eiweiss}, {nameof(Salz)}: {Salz}, {nameof(Foto)}: {Foto}";
    }

    protected bool Equals(Item other)
    {
        // NOTE: Every part that could possible be translated is *not* included in the comparison
        return Category == other.Category && Preis1 == other.Preis1 && Preis2 == other.Preis2 &&
               Preis3 == other.Preis3 && Einheit == other.Einheit && Piktogramme == other.Piktogramme &&
               Kj == other.Kj && Kcal == other.Kcal && Fett == other.Fett && Gesfett == other.Gesfett &&
               Kh == other.Kh && Zucker == other.Zucker && Ballaststoffe == other.Ballaststoffe &&
               Eiweiss == other.Eiweiss && Salz == other.Salz && Foto == other.Foto;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == this.GetType() && Equals((Item) obj);
    }

    public override int GetHashCode()
    {
        // TODO: Check if writeable properties are a problem here
        var hashCode = new HashCode();
        hashCode.Add(Category);
        hashCode.Add(Preis1);
        hashCode.Add(Preis2);
        hashCode.Add(Preis3);
        hashCode.Add(Einheit);
        hashCode.Add(Piktogramme);
        hashCode.Add(Kj);
        hashCode.Add(Kcal);
        hashCode.Add(Fett);
        hashCode.Add(Gesfett);
        hashCode.Add(Kh);
        hashCode.Add(Zucker);
        hashCode.Add(Ballaststoffe);
        hashCode.Add(Eiweiss);
        hashCode.Add(Salz);
        hashCode.Add(Foto);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(Item? left, Item? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Item? left, Item? right)
    {
        return !Equals(left, right);
    }
}
