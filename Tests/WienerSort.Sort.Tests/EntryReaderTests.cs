namespace WienerSort.Sort.Tests;

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class EntryReaderTests
{
    private static async Task<string[]> ReadAllLinesAsync(byte[] data, int chunkSize = 16,
        CancellationToken token = default)
    {
        await using var ms = new MemoryStream(data);
        EntryReader reader = new();
        List<string> lines = [];

        await foreach (var span in reader.ReadEntriesAsync(ms, chunkSize, token))
        {
            lines.Add(Encoding.UTF8.GetString(span.ToSpan()));
        }

        return lines.ToArray();
    }

    [Test]
    public async Task SingleLineTest()
    {
        var data = "1. Hello World\n"u8.ToArray();
        var lines = await ReadAllLinesAsync(data);
        Assert.That(lines.Length, Is.EqualTo(1));
        Assert.That(lines[0], Is.EqualTo("1. Hello World\n"));
    }

    [Test]
    public async Task MultipleLinesInChunkTest()
    {
        var data = "1. 456789012345\n1. 456789012345\n3. Line3\n"u8.ToArray();
        var lines = await ReadAllLinesAsync(data, chunkSize: 32); // single line is 9 bytes long
        Assert.That(lines, Is.EqualTo(new[] { "1. 456789012345\n", "1. 456789012345\n", "3. Line3\n" }));
    }

    [Test]
    public async Task MissingLastNewLine()
    {
        var data = "1. Line1\n2. Line2\n3. Line3"u8.ToArray();
        var lines = await ReadAllLinesAsync(data);
        Assert.That(lines, Is.EqualTo(new[] { "1. Line1\n", "2. Line2\n", "3. Line3\n" }));
    }

    [Test]
    public async Task LineSplitAcrossChunksTest()
    {
        var data = "1234567. A12\n2. ABCDEFG\n"u8.ToArray();
        var lines = await ReadAllLinesAsync(data, chunkSize: 16); // force split in the middle of the second line
        Assert.That(lines, Is.EqualTo(new[] { "1234567. A12\n", "2. ABCDEFG\n" }));
    }

    [Test]
    public async Task EmptyLinesTest()
    {
        var data = "1. Line1\n\n3. Line3\n"u8.ToArray();
        var lines = await ReadAllLinesAsync(data);
        Assert.That(lines, Is.EqualTo(new[] { "1. Line1\n", "3. Line3\n" }));
    }

    [Test]
    public void TooLargeChunksTest()
    {
        var data = "1. 1234567890123456\n\n3. Line3\n"u8.ToArray();
        Assert.That(async () => await ReadAllLinesAsync(data), Throws.Exception);
    }

    [Test]
    public async Task UnixNewLinesTest()
    {
        var data = "1. Line1\n3. Line3\n"u8.ToArray();
        var lines = await ReadAllLinesAsync(data);
        Assert.That(lines, Is.EqualTo(new[] { "1. Line1\n", "3. Line3\n" }));
    }

    [Test]
    public async Task NonUnixNewLinesTest()
    {
        var data = "1. Line1\r\n3. Line3\r\n"u8.ToArray();
        var lines = await ReadAllLinesAsync(data);
        Assert.That(lines, Is.EqualTo(new[] { "1. Line1\r\n", "3. Line3\r\n" }));
    }

    [Test]
    public void CancellationTest()
    {
        var data = "1. Line1\n2. Line2\n"u8.ToArray();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () => { await ReadAllLinesAsync(data, token: cts.Token); });
    }
}