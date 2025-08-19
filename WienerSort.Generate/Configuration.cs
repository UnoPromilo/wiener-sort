using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace WienerSort.Generate;

public static class Configuration
{
    public static IServiceCollection RegisterGenerateCommand(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<Command, GenerateCommand>();
        serviceCollection.AddScoped<ICommandHandler, CommandHandler>();
        serviceCollection.AddScoped<IWriter, Writer>();
        serviceCollection.AddScoped<IRepository<Sentence>, SentenceRepository>();
        serviceCollection.AddHttpClient();
        return serviceCollection;
    }
}