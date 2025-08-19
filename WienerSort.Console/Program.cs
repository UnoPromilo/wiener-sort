using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using WienerSort.Generate;
using WienerSort.Sort;

var serviceCollection = new ServiceCollection()
    .RegisterGenerateCommand()
    .RegisterSortCommand();

var serviceProvider = serviceCollection.BuildServiceProvider();


var rootCommand = new RootCommand();
foreach (var command in serviceProvider.GetServices<Command>())
{
    rootCommand.Add(command);
}

var stopwatch = Stopwatch.StartNew();
await rootCommand.Parse(args).InvokeAsync();
var elapsed = stopwatch.Elapsed;
Console.WriteLine($"Elapsed time: {elapsed}");