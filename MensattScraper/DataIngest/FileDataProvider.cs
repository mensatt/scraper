namespace MensattScraper.DataIngest;

public class FileDataProvider<T> : IDataProvider<T>
{
    private readonly string _path;
    private bool _fileRetrieved;

    public FileDataProvider(string path)
    {
        _path = path;
    }

    // Files should be read as fast as possible
    public uint GetDataDelayInSeconds => 0;

    public IEnumerable<Stream> RetrieveStream()
    {
        if (File.Exists(_path) && !_fileRetrieved)
        {
            _fileRetrieved = true;
            yield return File.OpenRead(_path);
        }


        if (!Directory.Exists(_path)) yield break;

        foreach (var file in Directory.EnumerateFiles(_path, "*.xml"))
            yield return File.OpenRead(file);
    }
}