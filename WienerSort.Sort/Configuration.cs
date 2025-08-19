using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace WienerSort.Sort;

public static class Configuration
{
    public static IServiceCollection RegisterSortCommand(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<Command, SortCommand>();
        serviceCollection.AddScoped<ICommandHandler, CommandHandler>();
        serviceCollection.AddScoped<IEntryReader, EntryReader>();
        serviceCollection.AddScoped<IChunkSorter, ChunkSorter>();
        serviceCollection.AddScoped<IComparer<Entry>, EntryComparer>();
        serviceCollection.AddScoped<IChunkRepository, ChunkRepository>();
        serviceCollection.AddScoped<IChunkMerger, ChunkMerger>();
        return serviceCollection;
    }
}