namespace WienerSort.Sort;

public interface IChunkSorter
{
    Task SortAsync(
        IAsyncEnumerable<Entry> source,
        int chunkSizeInKb,
        int jobs = 8,
        CancellationToken cancellationToken = default);
}

public class Counter(int maxCount)
{
    public int Current { get; private set; }

    public int Next()
    {
        Current++;
        if (Current == maxCount)
        {
            Current = 0;
        }

        return Current;
    }
}

public class ChunkSorter(IComparer<Entry> comparer, IChunkRepository repository) : IChunkSorter
{
    public async Task SortAsync(IAsyncEnumerable<Entry> enumerable,
        int chunkSizeInKb, int jobs = 8, CancellationToken token = default)
    {
        var chunkSize = chunkSizeInKb * 1024 / Entry.Size;
        if (chunkSize <= 0) throw new ArgumentException("Chunk size too small");

        var tasks = new List<Task>();
        using var sortingSemaphore = new SemaphoreSlim(jobs);
        using var writingSemaphore = new SemaphoreSlim(1);
        var bufferCount = jobs * 2;
        var bufferKey = new Counter(bufferCount);
        var buffers = new List<Entry>[bufferCount];
        for (var i = 0; i < bufferCount; i++)
        {
            buffers[i] = new(chunkSize);
        }

        await foreach (var chunk in enumerable.WithCancellation(token))
        {
            var buffer = buffers[bufferKey.Current];
            buffer.Add(chunk);

            if (buffer.Count < chunkSize)
            {
                continue;
            }

            bufferKey.Next();
            await sortingSemaphore.WaitAsync(token);

            // ReSharper disable AccessToDisposedClosure
            tasks.Add(Task.Run(() => SortBufferAsync(buffer, writingSemaphore, sortingSemaphore, token), token));
            // ReSharper enable AccessToDisposedClosure
        }

        await Task.WhenAll(tasks);
    }

    private async Task SortBufferAsync(List<Entry> buffer, SemaphoreSlim writingSemaphore,
        SemaphoreSlim sortingSemaphore, CancellationToken token)
    {
        try
        {
            buffer.Sort(comparer);
            await writingSemaphore.WaitAsync(token);
            try
            {
                await repository.StoreChunkAsync(buffer, token);
                buffer.Clear();
            }
            finally
            {
                writingSemaphore.Release();
            }
        }
        finally
        {
            sortingSemaphore.Release();
        }
    }
}