namespace WienerSort.Sort;

public interface IChunkSorter
{
    Task SortAsync(
        IAsyncEnumerable<Entry> source,
        int chunkSizeInKb,
        CancellationToken cancellationToken = default);
}

public class ChunkSorter(IComparer<Entry> comparer, IChunkRepository repository) : IChunkSorter
{
    public async Task SortAsync(IAsyncEnumerable<Entry> enumerable,
        int chunkSizeInKb, CancellationToken token = default)
    {
        var chunkSize = chunkSizeInKb * 1024 / Entry.Size;
        if (chunkSize <= 0) throw new ArgumentException("Chunk size too small");

        await using var source = enumerable.GetAsyncEnumerator(token);

        var buffer = new List<Entry>(chunkSize);

        while (true)
        {
            token.ThrowIfCancellationRequested();
            while (buffer.Count < chunkSize && await source.MoveNextAsync())
            {
                buffer.Add(source.Current);
            }

            if (buffer.Count == 0) break;

            buffer.Sort(comparer);
            await repository.StoreChunkAsync(buffer, token);
            buffer.Clear();
        }
    }
}