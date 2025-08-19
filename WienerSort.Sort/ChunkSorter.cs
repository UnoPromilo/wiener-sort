using System.Runtime.CompilerServices;

namespace WienerSort.Sort;

public interface IChunkSorter
{
    IAsyncEnumerable<List<Entry>> SortAsync(
        IAsyncEnumerable<Entry> source,
        int chunkSizeInKb,
        CancellationToken cancellationToken = default);
}

public class ChunkSorter(IComparer<Entry> comparer) : IChunkSorter
{
    public async IAsyncEnumerable<List<Entry>> SortAsync(IAsyncEnumerable<Entry> enumerable,
        int chunkSizeInKb,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var chunkSize = (chunkSizeInKb * 1024) / Entry.Size;
        if (chunkSize <= 0) throw new ArgumentException("Chunk size too small");

        await using var source = enumerable.GetAsyncEnumerator(token);

        var heap = new Entry[chunkSize];
        var frozenCount = 0;

        while (true)
        {
            var last = frozenCount - 1;
            frozenCount = 0;
            while (last + 1 < chunkSize && await source.MoveNextAsync())
            {
                token.ThrowIfCancellationRequested();
                last++;
                heap[last] = source.Current;
            }

            if (last < 0) yield break;

            BuildMinHeap(heap, last);
            var currentRun = new List<Entry>(capacity: chunkSize);

            while (last >= 0 && currentRun.Count < chunkSize)
            {
                token.ThrowIfCancellationRequested();
                var min = heap[0];
                currentRun.Add(min);
                if (await source.MoveNextAsync())
                {
                    var next = source.Current;
                    if (comparer.Compare(next, min) >= 0)
                    {
                        heap[0] = next;
                    }
                    else
                    {
                        heap[0] = heap[last];
                        heap[last] = next;
                        last--;
                        frozenCount++;
                    }
                }
                else
                {
                    heap[0] = heap[last];
                    last--;
                }

                if (last > 0)
                {
                    ShiftDown(heap, 0, last);
                }
            }

            if (last > 0)
            {
                frozenCount += last + 1;
            }

            yield return currentRun;
        }
    }

    private void BuildMinHeap(Entry[] heap, int last)
    {
        for (var elementIndex = (last - 1) / 2; elementIndex >= 0; elementIndex--)
            ShiftDown(heap, elementIndex, last);
    }

    private void ShiftDown(Entry[] heap, int elementIndex, int end)
    {
        while (true)
        {
            var leftChildIndex = 2 * elementIndex + 1;
            var rightChildIndex = 2 * elementIndex + 2;
            var smallest = elementIndex;
            if (leftChildIndex <= end && comparer.Compare(heap[leftChildIndex], heap[smallest]) < 0)
                smallest = leftChildIndex;
            if (rightChildIndex <= end && comparer.Compare(heap[rightChildIndex], heap[smallest]) < 0)
                smallest = rightChildIndex;

            if (smallest == elementIndex) break;

            Swap(heap, elementIndex, smallest);
            elementIndex = smallest;
        }
    }

    private void Swap(Entry[] heap, int firstIndex, int secondIndex)
    {
        (heap[firstIndex], heap[secondIndex]) = (heap[secondIndex], heap[firstIndex]);
    }
}