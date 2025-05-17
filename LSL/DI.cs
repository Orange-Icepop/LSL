using LSL.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LSL
{
    public static class DI
    {
        public static void AddServerHost(this IServiceCollection collection)
        {
            collection.AddLogging(builder => builder.AddDebug());
        }
        public static void AddViewModels(this IServiceCollection collection)
        {
            collection.AddSingleton<InteractionUnits>();
            collection.AddSingleton<AppStateLayer>(provider => new AppStateLayer(provider.GetRequiredService<InteractionUnits>()));
            collection.AddSingleton<ServiceConnector>(provider => new ServiceConnector(provider.GetRequiredService<AppStateLayer>()));
            collection.AddSingleton<PublicCommand>();
            collection.AddSingleton<BarRegionVM>();
            collection.AddSingleton<LeftRegionVM>();
            collection.AddSingleton<RightRegionVM>();
            collection.AddSingleton<ConfigViewModel>();
            collection.AddSingleton<ServerViewModel>();
            collection.AddSingleton<FormPageVM>();
            collection.AddSingleton<ShellViewModel>();
        }
    }
}
