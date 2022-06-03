namespace MensattScraper.DataIngest;

public interface IDataProvider 
{

    public bool HasNextStream();
    
    public Stream RetrieveStream();

}