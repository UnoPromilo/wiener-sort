using System.Buffers;

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
        var chunks = chunkRepository.GetChunks().ToList();
        var enumerators = chunks
            .Select(chunk => entryReader.ReadEntriesAsync(chunk, 1 << 20, token: token).GetAsyncEnumerator(token))
            .ToList();
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


            var buffer = ArrayPool<byte>.Shared.Rent(Entry.Size);
            try
            {
                while (priorityQueue.Count > 0)
                {
                    var (entry, enumeratorId) = priorityQueue.Dequeue();
                    var len = entry.ToBytes(buffer);
                    await output.WriteAsync(buffer.AsMemory(0, len), token);
                    var enumerator = enumerators[enumeratorId];
                    if (await enumerator.MoveNextAsync())
                    {
                        priorityQueue.Enqueue((enumerator.Current, enumeratorId), enumerator.Current);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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