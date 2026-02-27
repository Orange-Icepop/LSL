using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using LSL.Common.Extensions;
using LSL.Models.Server;
using LSL.Services;
using LSL.Services.ConfigServices;
using LSL.Services.ServerServices;
using LSL.ViewModels;
using LSL.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace LSL;

public static class InjectionHelper
{
    private static ILogger? GetLogger(this Context context, IServiceProvider provider) // 获取日志记录器
    {
        return provider.GetService<ILoggerFactory>()?
            .CreateLogger("Polly");
    }

    #region 添加单例

    extension(IServiceCollection collection)
    {
        public IServiceCollection AddLogging()
        {
            return collection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddNLog();
#if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
            });
        }

        public IServiceCollection AddNetworking()
        {
            collection.AddHttpClient("LSL", client => { client.ResetUserAgent($"LSL/{DesktopConstant.Version}"); })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromSeconds(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    UseCookies = false
                })
                .AddPolicyHandler((provider, _) => GetRetryPolicy(provider))
                .AddPolicyHandler((provider, _) => GetCircuitBreakerPolicy(provider));
            collection.AddSingleton<NetService>();
            return collection;
        }

        public IServiceCollection AddConfigManager()
        {
            return collection.AddSingleton<JavaConfigManager>()
                .AddSingleton<ServerConfigManager>()
                .AddSingleton<DaemonConfigManager>()
                .AddSingleton<WebConfigManager>()
                .AddSingleton<DesktopConfigManager>()
                .AddSingleton<ConfigManager>();
        }

        public IServiceCollection AddServerHost()
        {
            return collection.AddSingleton<IServerHost, ServerHost>()
                .AddSingleton<ClientConnector>();
        }

        public IServiceCollection AddStartUp()
        {
            return collection.AddSingleton<DialogCoordinator>()
                .AddSingleton<DialogViewModel>()
                .AddSingleton<AppStateLayer>()
                .AddSingleton<InitializationViewModel>();
        }

        public IServiceCollection AddViewModels()
        {
            return collection.AddSingleton<ServiceConnector>()
                .AddSingleton<PublicCommand>()
                .AddSingleton<BarRegionViewModel>()
                .AddSingleton<LeftRegionViewModel>()
                .AddSingleton<RightRegionViewModel>()
                .AddSingleton<ConfigViewModel>()
                .AddSingleton<MonitorViewModel>()
                .AddSingleton<ServerViewModel>()
                .AddSingleton<FormPageViewModel>()
                .AddSingleton<ShellViewModel>();
        }

        public IServiceCollection AddViews()
        {
            return collection.AddSingleton<MainWindow>()
                .AddSingleton<MainView>();
        }
    }

    #endregion

    #region Polly策略

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider provider) // 重试策略
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
                        "Request retry #{RetryAttempt} will execute after {Timespan} . Code: {StatusCode}",
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
                (_, timespan, context) =>
                {
                    context.GetLogger(provider)?.LogError("Web connection meltdown activated. Restore in {Timespan}.",
                        timespan);
                },
                context => { context.GetLogger(provider)?.LogInformation("Web connection meltdown deactivated."); });
    }

    #endregion
}

#region 自定义日志格式化器

public sealed class Utf8ConsoleFormatter : ConsoleFormatter
{
    public Utf8ConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
        : base("custom")
    {
    }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        // 强制使用 UTF-8 编码写入
        var encoding = Encoding.UTF8;
        var level = logEntry.LogLevel switch
        {
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "DEAD",
            _ => string.Empty
        };
        var message = $"[{DateTime.Now:hh:mm:ss}] [{logEntry.Category}|{level}] ";
        message += logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (logEntry.Exception != null) message += $"\n{logEntry.Exception}";

        var bytes = encoding.GetBytes(message + Environment.NewLine);
        var consoleStream = Console.OpenStandardOutput();
        consoleStream.Write(bytes, 0, bytes.Length);
    }
}

#endregion