using System.Buffers;
using System.Runtime.CompilerServices;

namespace WienerSort.Sort;

public interface IChunkMerger
{
    IAsyncEnumerable<Entry> MergeAsync(List<Stream> chunks, CancellationToken token = default);
}

public class ChunkMerger(IComparer<Entry> comparer, IEntryReader entryReader)
    : IChunkMerger
{
    // TODO allow multi pass merge to support large files
    // TODO refactor
    public async IAsyncEnumerable<Entry> MergeAsync(List<Stream> chunks,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var enumerators = chunks
            .Select(chunk => entryReader.ReadEntriesAsync(chunk, 1 << 16, token: token).GetAsyncEnumerator(token))
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
                    yield return entry;
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
        }
    }
}