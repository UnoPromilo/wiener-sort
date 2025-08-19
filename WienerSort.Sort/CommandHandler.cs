using OneOf;

namespace WienerSort.Sort;

internal struct ReadFromStdIn;

internal struct WriteToStdOut;

internal record ParsedCommand(
    OneOf<ReadFromStdIn, FileInfo> Input,
    OneOf<WriteToStdOut, FileInfo> Output,
    uint ChunkSizeInKb,
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
        chunkRepository.SelectTempFile(temporaryFile);


        var entries = entryReader.ReadEntriesAsync(inputStream, chunkSize, token);
        await foreach (var chunk in chunkSorter.SortAsync(entries, chunkSize, token))
        {
            await chunkRepository.StoreChunkAsync(chunk, token);
        }

        await using var outputStream = GetOutputStream(command.Output);
        await chunkMerger.MergeAsync(outputStream, token);
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
            fileInfo => fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.Read));
    }
}