namespace WienerSort.Generate;

internal interface IWriter
{
    Task WriteAsync(Stream stream, uint targetOutputSizeInMb, CancellationToken token = default);
}

public class Writer(IRepository<Sentence> sentenceRepository) : IWriter
{
    public async Task WriteAsync(Stream stream, uint targetOutputSizeInMb, CancellationToken token = default)
    {
        await using var writer = new StreamWriter(stream, bufferSize: 1 << 20);
        var sentences = await sentenceRepository.GetAll(token).ToListAsync(token);
        var written = 0UL;
        var targetBytes = targetOutputSizeInMb * 1024UL * 1024UL;
        while (written < targetBytes)
        {
            //token.ThrowIfCancellationRequested();

            var key = Random.Shared.Next(1, 2137420);
            // To increase chance of getting the same sentence twice
            var idx = (int)(Math.Pow(Random.Shared.NextDouble(), 4) * sentences.Count);
            // TODO optimize
            var line = $"{key}. {sentences[idx]}{Environment.NewLine}";
            await writer.WriteAsync(line);
            written += (ulong)writer.Encoding.GetByteCount(line);
        }
    }
}