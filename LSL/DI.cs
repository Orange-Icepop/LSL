using System;
using System.Net;
using System.Net.Http;
using LSL.Services;
using LSL.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace LSL
{
    public static class DI
    {
        
        #region 添加单例
        public static void AddLogging(this IServiceCollection collection)
        {
            collection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }
        public static void AddNetworking(this IServiceCollection collection)
        {
            collection.AddHttpClient(nameof(NetService))
                .ConfigurePrimaryHttpMessageHandler(()=> new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5)
                })
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());
            collection.AddSingleton<NetService>();
        }
        public static void AddService(this IServiceCollection collection)
        {
            collection.AddSingleton<ServerOutputHandler>();
            collection.AddSingleton<ServerOutputStorage>();
            collection.AddSingleton<ServerHost>();
        }
        public static void AddViewModels(this IServiceCollection collection)
        {
            collection.AddSingleton<InteractionUnits>();
            collection.AddSingleton<AppStateLayer>(provider => new AppStateLayer(provider.GetRequiredService<InteractionUnits>()));
            collection.AddSingleton<ServiceConnector>(provider =>
                new ServiceConnector(provider.GetRequiredService<AppStateLayer>(),
                    provider.GetRequiredService<ServerHost>(), 
                    provider.GetRequiredService<ServerOutputStorage>()));
            collection.AddSingleton<PublicCommand>();
            collection.AddSingleton<BarRegionVM>();
            collection.AddSingleton<LeftRegionVM>();
            collection.AddSingleton<RightRegionVM>();
            collection.AddSingleton<ConfigViewModel>();
            collection.AddSingleton<ServerViewModel>();
            collection.AddSingleton<FormPageVM>();
            collection.AddSingleton<ShellViewModel>();
        }
        #endregion
        
        #region Polly策略
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()// 重试策略
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryCount =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        // 记录重试信息
                        context.GetLogger()?.LogWarning(
                            "Request retry #{RetryAttempt} will execute after {Timespan} . Reason: {StatusCode}",
                            retryAttempt, timespan, outcome.Result?.StatusCode);
                    });
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromSeconds(30),
                    (outcome, timespan, context) =>
                    {
                        context.GetLogger()?.LogError("Web connection meltdown activated. Restore in {Timespan}.", timespan);
                    },
                    (context) =>
                    {
                        context.GetLogger()?.LogInformation("Web connection meltdown deactivated.");
                    });
        }
        #endregion
        public static ILogger? GetLogger(this Context context) // 获取日志记录器
        {
            if (context.TryGetValue("ILogger", out var logger) && logger is ILogger value)
            {
                return value;
            }
            return null;
        }
    }
}
