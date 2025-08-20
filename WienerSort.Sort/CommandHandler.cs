using OneOf;

namespace WienerSort.Sort;

internal struct ReadFromStdIn;

internal struct WriteToStdOut;

internal record ParsedCommand(
    OneOf<ReadFromStdIn, FileInfo> Input,
    OneOf<WriteToStdOut, FileInfo> Output,
    uint ChunkSizeInKb,
    uint JobsCount,
    FileInfo TemporaryFile);

internal interface ICommandHandler
{
    Task HandleAsync(ParsedCommand command, CancellationToken token);
}

internal class CommandHandler(
    IEntryReader entryReader,
    IChunkSorter chunkSorter,
    IChunkRepository chunkRepository,
    IChunkMerger chunkMerger)
    : ICommandHandler
{
    public async Task HandleAsync(ParsedCommand command, CancellationToken token)
    {
        await using var inputStream = GetInputStream(command.Input);
        var chunkSize = (int)command.ChunkSizeInKb;
        var temporaryFile = command.TemporaryFile;
        var jobsCount = (int)command.JobsCount;
        chunkRepository.SelectTempFile(temporaryFile);
        var entries = entryReader.ReadEntriesAsync(inputStream, 1 << 20, token);
        await chunkSorter.SortAsync(entries, chunkSize, jobsCount, token);
        await using var outputStream = GetOutputStream(command.Output);
        var buffer = new byte[Entry.Size];
        await foreach (var entry in chunkMerger.MergeAsync(chunkRepository.GetChunks().ToList(), token))
        {
            var len = entry.ToBytes(buffer);
            await outputStream.WriteAsync(buffer.AsMemory(0, len), token);
        }
    }

    private static Stream GetInputStream(OneOf<ReadFromStdIn, FileInfo> target)
    {
        return target.Match(readFromStdIn => Console.OpenStandardInput(),
            fileInfo => fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    private static Stream GetOutputStream(OneOf<WriteToStdOut, FileInfo> target)
    {
        return target.Match(
            writeToStdOut => Console.OpenStandardOutput(),
            fileInfo => new FileStream(fileInfo.Name, FileMode.Create, FileAccess.Write, FileShare.Read, 1 << 30));
    }
}