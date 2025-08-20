using System.Text;

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
        var targetBytes = (long)(targetOutputSizeInMb * 1024UL * 1024UL);
        var builder = new StringBuilder(264);
        while (writer.BaseStream.Position < targetBytes)
        {
            token.ThrowIfCancellationRequested();

            var key = Random.Shared.Next(1, 2137420);
            // To increase chance of getting the same sentence twice
            var index = (int)(Math.Pow(Random.Shared.NextDouble(), 4) * sentences.Count);
            builder.Clear().Append(key).Append(". ").Append(sentences[index]).AppendLine();
            await writer.WriteAsync(builder, token);
        }
    }
}