using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LSL.Services;

public class NetService
{
    private IHttpClientFactory _factory { get; }
    private ILogger<NetService> _logger { get; }
    private const int bufferSize = 8192;
    public NetService(IHttpClientFactory factory, ILogger<NetService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task GetAsync(string url, string dir, IProgress<double> progress, CancellationToken token) 
    {
        using var client = _factory.CreateClient();
        string? path = null;
        var fileExists = false;
        try
        {
            Directory.CreateDirectory(dir);
            // 开始使用GET返回项
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            // 构建完整文件路径
            var fname = GetFileNameFromResponse(response);
            path = Path.Combine(dir, fname);
            var size = response.Content.Headers.ContentLength ?? -1L; // 能读到大小就显示大小，否则显示负数
            // 开始使用网络文件流与文件写入流
            await using var wstream = await response.Content.ReadAsStreamAsync(token);
            await using var fstream =
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);
            fileExists = true;

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize); // 通过ArrayPool减少GC压力
            try
            {
                var readbytes = 0L;
                var webProgress = 0d;
                while (true)
                {
                    var tempbytes = await wstream.ReadAsync(buffer, token); // 当前一轮的下载
                    if (tempbytes == 0) break; // 已结束则退出
                    await fstream.WriteAsync(buffer.AsMemory(0, tempbytes), token); // 写入文件buffer
                    readbytes += tempbytes; // 更新已读取字节数
                    if (size > 0) // 能否获取大小
                    {
                        var curProgress = (double)readbytes / size;
                        if (!(curProgress - webProgress >= 0.01) && readbytes != size) continue; // 进度过小则不报告
                        webProgress = curProgress;
                        progress.Report(webProgress);
                    }
                    else
                    {
                        progress.Report(-readbytes); // 用负数表示字节数而不是进度
                    }
                }
            }
            finally // 唯一麻烦的是需要保证返还
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception ex) when (HandleException(ex, fileExists, path))
        {
            
        }
    }
    
    private static bool HandleException(Exception ex, bool fileCreated, string? path)
    {
        TryDeleteFile(fileCreated, path);
        
        // 保留原始异常信息
        var wrappedEx = ex switch
        {
            TaskCanceledException => new OperationCanceledException("下载已取消", ex),
            HttpRequestException => new Exception($"网络错误: {ex.Message}", ex),
            IOException => new Exception($"文件IO错误: {ex.Message}", ex),
            _ => new Exception($"下载失败: {ex.Message}", ex)
        };
        
        throw wrappedEx;
    }

    private static string GetFileNameFromResponse(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentDisposition != null)
        {
            var fname = response.Content.Headers.ContentDisposition.FileNameStar ?? 
                        response.Content.Headers.ContentDisposition.FileName;
            if (!string.IsNullOrEmpty(fname))
            {
                return fname.Trim('"', '\'');// 返回头有文件名再好不过了
            }
        }
        var uri = response.RequestMessage?.RequestUri;
        var urifilename = Path.GetFileName(uri?.LocalPath);// 不行再找请求头中的文件名称
        return !string.IsNullOrEmpty(urifilename) ? urifilename : $"{Guid.NewGuid()}.bin";// 再没有只能用GUID了
    }

    private static bool TryDeleteFile(bool hasFile, string? path)
    {
        if (!hasFile || !File.Exists(path)) return true;
        try
        {
            File.Delete(path);
        }
        catch
        {
            return false;
        }
        return true;
    }
}