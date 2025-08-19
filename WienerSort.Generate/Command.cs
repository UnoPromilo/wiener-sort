using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace WienerSort.Generate;

public class GenerateCommand : Command
{
    private readonly IServiceProvider _serviceProvider;

    private Option<bool> WriteToStdOutOption { get; } = new("--write-to-std-out", "-p")
    {
        Description = "Write the output to std out",
        DefaultValueFactory = _ => false,
    };

    private Option<FileInfo> OutputPathOption { get; } = new("--output", "-o")
    {
        Description = "Path to the output file",
    };

    private Argument<uint> DesiredOutputSizeMbOption { get; } = new("desired-output-size-mb")
    {
        Description = "Target size of test file in megabytes",
    };


    public GenerateCommand(IServiceProvider serviceProvider) : base("generate", "Generate a new file")
    {
        _serviceProvider = serviceProvider;
        Arguments.Add(DesiredOutputSizeMbOption);
        Options.Add(WriteToStdOutOption);
        Options.Add(OutputPathOption);
        Validators.Add(result =>
        {
            if (result.GetValue(WriteToStdOutOption) == false && result.GetValue(OutputPathOption) == null)
            {
                result.AddError("Output must be specified");
            }

            if (result.GetValue(DesiredOutputSizeMbOption) == 0)
            {
                result.AddError("Desired output size must be higher than 0");
            }
        });
        SetAction(OnExecuteAsync);
    }

    private async Task OnExecuteAsync(ParseResult parseResult, CancellationToken token)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var writeToStdOut = parseResult.GetValue(WriteToStdOutOption);
        var outputPath = parseResult.GetValue(OutputPathOption);
        var desiredFileSizeInMb = parseResult.GetValue(DesiredOutputSizeMbOption);
        if (outputPath == null && writeToStdOut == false)
        {
            throw new InvalidOperationException("Output must be specified");
        }

        var command = new ParsedCommand(writeToStdOut ? new WriteToStdOut() : outputPath!, desiredFileSizeInMb);

        await scope.ServiceProvider.GetRequiredService<ICommandHandler>().HandleAsync(command, token);
    }
}