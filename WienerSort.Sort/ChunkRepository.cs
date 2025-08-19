namespace WienerSort.Sort;

public interface IChunkRepository
{
    Task StoreChunkAsync(IEnumerable<Entry> entries, CancellationToken token = default);
    void SelectTempFile(FileInfo temporaryFile);
    IEnumerable<Stream> GetChunks();
}

record ChunkData(long Offset, long Length);

public class ChunkRepository : IChunkRepository, IDisposable, IAsyncDisposable
{
    private Stream? _stream;
    private FileInfo? _temporaryFile;
    private readonly List<ChunkData> _chunks = [];

    public void SelectTempFile(FileInfo temporaryFile)
    {
        _temporaryFile = temporaryFile;
        _stream = new FileStream(_temporaryFile.FullName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
            1 << 20);
    }

    public async Task StoreChunkAsync(IEnumerable<Entry> entries, CancellationToken token = default)
    {
        if (_stream == null)
        {
            throw new("Stream not opened");
        }

        var start = _stream.Position;

        foreach (var entry in entries)
        {
            var span = entry.ToSpan();
            await _stream.WriteAsync(span.ToArray(), token);
        }

        await _stream.FlushAsync(token);
        var end = _stream.Position;
        _chunks.Add(new(start, end - start));
    }

    public IEnumerable<Stream> GetChunks()
    {
        if (_temporaryFile == null)
        {
            throw new("File not attached");
        }

        foreach (var chunk in _chunks)
        {
            var stream = _temporaryFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            yield return new SubStream(stream, chunk.Offset, chunk.Length);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _stream?.Dispose();
        _chunks.Clear();
        _stream = null;
        _temporaryFile?.Delete();
        _temporaryFile = null;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_stream != null) await _stream.DisposeAsync();
        _chunks.Clear();
        _stream = null;
        _temporaryFile?.Delete();
        _temporaryFile = null;
    }
}