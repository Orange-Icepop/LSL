using System;
using System.Net;
using System.Net.Http;
using LSL.Services;
using LSL.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

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
                #if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
                #else
                builder.SetMinimumLevel(LogLevel.Information);
                #endif
            });
        }
        public static void AddNetworking(this IServiceCollection collection)
        {
            collection.AddHttpClient(nameof(NetService))
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5)
                })
                .AddPolicyHandler((provider, request) => GetRetryPolicy(provider))
                .AddPolicyHandler((provider, request) => GetCircuitBreakerPolicy(provider));
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
            collection.AddSingleton<AppStateLayer>();
            collection.AddSingleton<ServiceConnector>();
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
        private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider provider)// 重试策略
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryCount =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                        // 记录重试信息
                        context.GetLogger(provider)?.LogWarning(
                            "Request retry #{RetryAttempt} will execute after {Timespan} . Reason: {StatusCode}",
                            retryAttempt, timespan, outcome.Result?.StatusCode);
                    });
        }

        private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider provider)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromSeconds(30),
                    (outcome, timespan, context) =>
                    {
                        context.GetLogger(provider)?.LogError("Web connection meltdown activated. Restore in {Timespan}.", timespan);
                    },
                    (context) =>
                    {
                        context.GetLogger(provider)?.LogInformation("Web connection meltdown deactivated.");
                    });
        }
        #endregion
        private static ILogger? GetLogger(this Context context, IServiceProvider provider) // 获取日志记录器
        {
            return provider.GetService<ILoggerFactory>()?
                .CreateLogger("Polly");        
        }
    }
}
