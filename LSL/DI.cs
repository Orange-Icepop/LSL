using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSL.Services;
using LSL.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LSL
{
    public static class DI
    {
        public static void AddServerHost(this IServiceCollection collection)
        {
            //collection.AddSingleton<IServerHost, ServerHost>();
            collection.AddLogging(builder => builder.AddDebug());
        }
        public static void AddViewModels(this IServiceCollection collection)
        {
            collection.AddSingleton<PopupInteraction>();
            collection.AddSingleton<AppStateLayer>(provider => new AppStateLayer(provider.GetRequiredService<PopupInteraction>()));
            collection.AddSingleton<ServiceConnector>(provider => new ServiceConnector(provider.GetRequiredService<AppStateLayer>()));
            collection.AddTransient<PublicCommand>();
            collection.AddTransient<BarRegionVM>();
            collection.AddTransient<LeftRegionVM>();
            collection.AddTransient<RightRegionVM>();
            collection.AddTransient<ConfigViewModel>();
            collection.AddTransient<ServerViewModel>();
            collection.AddTransient<ShellViewModel>();
        }
    }
}
