using OneOf;

namespace WienerSort.Generate;

internal struct WriteToStdOut;

internal record ParsedCommand(OneOf<WriteToStdOut, FileInfo> Output, uint TargetSizeMb);

internal interface ICommandHandler
{
    Task HandleAsync(ParsedCommand command, CancellationToken token);
}

internal class CommandHandler(IWriter writer) : ICommandHandler
{
    public async Task HandleAsync(ParsedCommand command, CancellationToken token)
    {
        await using var stream = GetStream(command.Output);
        await writer.WriteAsync(stream, command.TargetSizeMb, token);
    }

    private static Stream GetStream(OneOf<WriteToStdOut, FileInfo> target)
    {
        return target.Match(writeToStd => Console.OpenStandardOutput(),
            fileInfo => fileInfo.Open(FileMode.Create, FileAccess.Write));
    }
}