using System.CommandLine;
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

await rootCommand.Parse(args).InvokeAsync();