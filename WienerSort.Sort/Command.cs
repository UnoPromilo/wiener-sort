using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace WienerSort.Sort;

public class SortCommand : Command
{
    private const int DefaultChunkSize = 1 << 14; // 16MB
    private readonly IServiceProvider _serviceProvider;

    private Option<bool> ReadFromStdInOption { get; } = new("--read-from-std-in", "-p")
    {
        Description = "Read input from std in",
        DefaultValueFactory = _ => false,
    };

    private Option<FileInfo> InputPathOption { get; } = new("--input", "-i")
    {
        Description = "Path to the input file",
    };

    private Option<uint> ChunkSizeOption { get; } = new("--chunk-size-kb")
    {
        Description = "The size of the chunk in kilobytes while reading",
        DefaultValueFactory = _ => DefaultChunkSize,
    };

    private Option<FileInfo> TemporaryPathOption { get; } = new("--temporary-file")
    {
        Description = "Path to the temporary file",
        DefaultValueFactory = _ => new(Path.GetTempFileName()),
    };

    private Option<bool> WriteToStdOutOption { get; } = new("--write-to-std-out", "-p")
    {
        Description = "Write the output to std out",
        DefaultValueFactory = _ => false,
    };

    private Option<FileInfo> OutputPathOption { get; } = new("--output", "-o")
    {
        Description = "Path to the output file",
    };

    private Option<uint> ParallelJobsCount { get; } = new("--parallel-jobs-count", "-j")
    {
        Description = "The number of parallel jobs",
        DefaultValueFactory = _ => (uint)Environment.ProcessorCount,
    };

    public SortCommand(IServiceProvider serviceProvider) : base("sort", "Sort file")
    {
        _serviceProvider = serviceProvider;
        Options.Add(InputPathOption);
        Options.Add(ReadFromStdInOption);
        Options.Add(ChunkSizeOption);
        Options.Add(TemporaryPathOption);
        Options.Add(WriteToStdOutOption);
        Options.Add(OutputPathOption);
        Options.Add(ParallelJobsCount);

        Validators.Add(result =>
        {
            if (result.GetValue(ReadFromStdInOption) == false && result.GetValue(InputPathOption) == null)
            {
                result.AddError("Input must be specified");
            }

            if (result.GetValue(WriteToStdOutOption) == false && result.GetValue(OutputPathOption) == null)
            {
                result.AddError("Output must be specified");
            }
        });

        SetAction(OnExecuteAsync);
    }

    private async Task OnExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var readFromStdIn = parseResult.GetRequiredValue(ReadFromStdInOption);
        var inputPath = parseResult.GetValue(InputPathOption);
        var chunkSize = parseResult.GetRequiredValue(ChunkSizeOption);
        var temporaryPath = parseResult.GetRequiredValue(TemporaryPathOption);
        var writeToStdOut = parseResult.GetRequiredValue(WriteToStdOutOption);
        var outputPath = parseResult.GetRequiredValue(OutputPathOption);
        var jobsCount = parseResult.GetRequiredValue(ParallelJobsCount);
        if (inputPath == null && readFromStdIn == false)
        {
            throw new InvalidOperationException("Input must be specified");
        }

        if (outputPath == null && writeToStdOut == false)
        {
            throw new InvalidOperationException("Output must be specified");
        }

        var command = new ParsedCommand(
            readFromStdIn ? new ReadFromStdIn() : inputPath!,
            writeToStdOut ? new WriteToStdOut() : outputPath!,
            chunkSize, jobsCount, temporaryPath);

        await scope.ServiceProvider.GetRequiredService<ICommandHandler>().HandleAsync(command, token);
    }
}