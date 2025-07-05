using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using LSL.Services;
using LSL.Services.ServerServices;
using LSL.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using ServerOutputHandler = LSL.Services.ServerServices.ServerOutputHandler;

namespace LSL
{
    public static class DI
    {
        public static void InitSerilog()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LSL", "logs");
            #if DEBUG
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Logger( lc => lc
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{SourceContext}|{Level}] {Message}{NewLine}{Exception}")
                    .WriteTo.File(
                        path: Path.Combine(logPath, "log-.log"),
                        rollingInterval: RollingInterval.Minute,
                        retainedFileCountLimit: 10,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{SourceContext}|{Level}] {Message}{NewLine}{Exception}",
                        encoding: Encoding.UTF8)
                    .WriteTo.Debug(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{SourceContext}|{Level}] {Message}{NewLine}{Exception}"))
                .CreateLogger();
            #else
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{SourceContext}|{Level}] {Message}{NewLine}{Exception}")
                .WriteTo.File(
                    path: Path.Combine(logPath, "log-.log"),
                    rollingInterval: RollingInterval.Hour,
                    retainedFileCountLimit: 100,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{SourceContext}|{Level}] {Message}{NewLine}{Exception}",
                    encoding: Encoding.UTF8)
                .CreateLogger();
            #endif
        }
        #region 添加单例
        public static void AddLogging(this IServiceCollection collection)
        {
            collection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
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
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    UseCookies = false,
                })
                .AddPolicyHandler((provider, request) => GetRetryPolicy(provider))
                .AddPolicyHandler((provider, request) => GetCircuitBreakerPolicy(provider));
            collection.AddSingleton<NetService>();
        }

        public static void AddConfigManager(this IServiceCollection collection)
        {
            collection.AddSingleton<JavaConfigManager>();
            collection.AddSingleton<ServerConfigManager>();
            collection.AddSingleton<ConfigManager>();
            collection.AddSingleton<MainConfigManager>();
        }
        public static void AddServerHost(this IServiceCollection collection)
        {
            collection.AddSingleton<ServerOutputHandler>();
            collection.AddSingleton<ServerOutputStorage>();
            collection.AddSingleton<ServerMetricsBuffer>();
            collection.AddSingleton<ServerHost>();
        }
        public static void AddStartUp(this IServiceCollection collection)
        {
            collection.AddSingleton<InteractionUnits>();
            collection.AddSingleton<AppStateLayer>();
            collection.AddSingleton<InitializationVM>();
        }
        public static void AddViewModels(this IServiceCollection collection)
        {
            collection.AddSingleton<ServiceConnector>();
            collection.AddSingleton<PublicCommand>();
            collection.AddSingleton<BarRegionVM>();
            collection.AddSingleton<LeftRegionVM>();
            collection.AddSingleton<RightRegionVM>();
            collection.AddSingleton<ConfigViewModel>();
            collection.AddSingleton<MonitorViewModel>();
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
    #region 自定义日志格式化器
    public sealed class Utf8ConsoleFormatter : ConsoleFormatter
    {
        public Utf8ConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
            : base("custom") { }

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
                _ => string.Empty,
            };
            var message = $"[{DateTime.Now.ToString("hh:mm:ss")}] [{logEntry.Category}|{level}] ";
            message += logEntry.Formatter(logEntry.State, logEntry.Exception);
            
            if (logEntry.Exception != null)
            {
                message += $"\n{logEntry.Exception}";
            }
            
            var bytes = encoding.GetBytes(message + Environment.NewLine);
            var consoleStream = Console.OpenStandardOutput();
            consoleStream.Write(bytes, 0, bytes.Length);
        }
    }
    #endregion
}
