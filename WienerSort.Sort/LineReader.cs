using System.Buffers;
using System.Runtime.CompilerServices;

namespace WienerSort.Sort;

public interface IEntryReader
{
    IAsyncEnumerable<Entry> ReadEntriesAsync(Stream stream, int bufferSize,
        CancellationToken token = default); // TODO remove enumeration cancellation
}

public class EntryReader : IEntryReader
{
    public async IAsyncEnumerable<Entry> ReadEntriesAsync(Stream stream, int bufferSize,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        bufferSize = buffer.Length; // Rented buffer probably is bigger so it is worth to use it whole
        var leftover = 0;

        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(leftover, bufferSize - leftover), token)) > 0)
            {
                var start = 0;

                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    var notProcessedBufferSpan = new ReadOnlySpan<byte>(buffer, start, bytesRead + leftover - start);
                    var length = notProcessedBufferSpan.IndexOf((byte)'\n');
                    if (length < 0) break;
                    if (length == 0)
                    {
                        start++;
                        continue;
                    }

                    var line = notProcessedBufferSpan[..length];
                    yield return Entry.FromSpan(line);
                    start += length + 1;
                }

                leftover = bytesRead + leftover - start;
                if (leftover <= 0) continue;

                if (leftover == bufferSize)
                {
                    throw new("Single line exceed chunk size");
                }

                var leftoverSpan = new ReadOnlySpan<byte>(buffer, start, leftover);
                leftoverSpan.CopyTo(buffer);
            }

            if (leftover <= 0) yield break;

            var lastLine = new ReadOnlySpan<byte>(buffer, 0, leftover);
            yield return Entry.FromSpan(lastLine);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}