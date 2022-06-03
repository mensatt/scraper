using System.Diagnostics;

namespace MensattScraper.DataIngest;

public class FileDataProvider : IDataProvider
{
    private readonly List<string> _elements;

    public FileDataProvider(string path)
    {
        if (File.Exists(path))
            _elements = new() {path};
        else if (Directory.Exists(path))
            _elements = Directory.GetFiles(path, "*.xml").ToList();
        else
            _elements = new();
    }

    public bool HasNextStream()
    {
        return _elements.Count > 0;
    }

    public Stream RetrieveStream()
    {
        if (!HasNextStream())
        {
            Trace.Assert(false, "This data provider has no more elements");
        }

        var currentFile = _elements[0];
        _elements.RemoveAt(0);

        return File.OpenRead(currentFile);
    }
}