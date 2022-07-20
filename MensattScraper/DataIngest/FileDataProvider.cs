namespace MensattScraper.DataIngest;

public class FileDataProvider<T> : IDataProvider<T>
{
    private bool _fileRetrieved;

    public FileDataProvider(string path)
    {
        Path = path;
    }

    internal string Path { get; }

    public string? CopyLocation
    {
        get => null;
        set => throw new NotImplementedException("Files cannot be saved currently");
    }

    // Files should be read as fast as possible
    public uint GetDataDelayInSeconds => 0;

    public IEnumerable<Stream> RetrieveStream()
    {
        if (File.Exists(Path) && !_fileRetrieved)
        {
            _fileRetrieved = true;
            yield return File.OpenRead(Path);
        }


        if (!Directory.Exists(Path)) yield break;

        foreach (var file in Directory.EnumerateFiles(Path, "*.xml"))
            yield return File.OpenRead(file);
    }
}