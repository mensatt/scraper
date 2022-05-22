using System.Net;
using System.Xml.Serialization;
using MensattScraper.Compat;

namespace MensattScraper;

public class Program
{
    public const string API_URL = "https://www.max-manager.de/daten-extern/sw-erlangen-nuernberg/xml/mensa-sued.xml";

    public static void Main(string[] args)
    {
        HttpClient client = new HttpClient();

        XmlSerializer serializer = new XmlSerializer(typeof(Speiseplan));

        Speiseplan s;

        using (Stream reader = client.GetStreamAsync(API_URL).Result)
        {
            s = (Speiseplan) serializer.Deserialize(reader);
        }
        

        int x = 0;
    }
}