namespace WienerSort.Sort;

public interface IChunkMerger
{
    Task MergeAsync(Stream output, CancellationToken token = default);
}

public class ChunkMerger(IComparer<Entry> comparer, IEntryReader entryReader, IChunkRepository chunkRepository)
    : IChunkMerger
{
    public async Task MergeAsync(Stream output, CancellationToken token = default)
    {
        // TODO pass chunk size
        var chunks = chunkRepository.GetChunks().ToList();
        var enumerators = chunks
            .Select(chunk => entryReader.ReadEntriesAsync(chunk, token: token).GetAsyncEnumerator(token)).ToList();
        var priorityQueue = new PriorityQueue<(Entry entry, int enumeratorId), Entry>(comparer);
        try
        {
            for (var enumerableIndex = 0; enumerableIndex < enumerators.Count; enumerableIndex++)
            {
                var enumerable = enumerators[enumerableIndex];
                if (await enumerable.MoveNextAsync())
                {
                    priorityQueue.Enqueue((enumerable.Current, enumerableIndex), enumerable.Current);
                }
            }


            while (priorityQueue.Count > 0)
            {
                var (entry, enumeratorId) = priorityQueue.Dequeue();
                // TODO fix mess with ToSpan ToArray
                await output.WriteAsync(entry.ToSpan().ToArray(), token);
                var enumerator = enumerators[enumeratorId];
                if (await enumerator.MoveNextAsync())
                {
                    priorityQueue.Enqueue((enumerator.Current, enumeratorId), enumerator.Current);
                }
            }
        }
        finally
        {
            foreach (var entry in enumerators)
            {
                await entry.DisposeAsync();
            }

            foreach (var chunk in chunks)
            {
                await chunk.DisposeAsync();
            }

            await output.FlushAsync(token);
        }
    }
}