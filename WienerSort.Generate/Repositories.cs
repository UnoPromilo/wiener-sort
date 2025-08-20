using System.Runtime.CompilerServices;

namespace WienerSort.Generate;

public interface IRepository<out T>
{
    IAsyncEnumerable<Sentence> GetAll(CancellationToken token = default);
}

public record Sentence(string Text)
{
    public override string ToString()
    {
        return Text;
    }
}

public class SentenceRepository(HttpClient client) : IRepository<Sentence>
{
    public async IAsyncEnumerable<Sentence> GetAll([EnumeratorCancellation] CancellationToken token = default)
    {
        const string url = "https://baconipsum.com/api/?type=meat-and-filler&paras=5&format=text";

        var content = await client.GetStringAsync(url, token);
        var lines = content.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            yield return new(line);
        }
    }
}