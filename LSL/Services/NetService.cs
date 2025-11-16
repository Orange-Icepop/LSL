using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LSL.Common.Models;
using Microsoft.Extensions.Logging;

namespace LSL.Services;

public class NetService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<NetService> _logger;
    private const int BufferSize = 8192;
    public NetService(IHttpClientFactory factory, ILogger<NetService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    #region 异步下载请求
    public async Task<ServiceResult> GetFileAsync(string url, string dir, IProgress<double>? progress, CancellationToken token = new()) 
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
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true);
            fileExists = true;

            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize); // 通过ArrayPool减少GC压力
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
                        progress?.Report(webProgress);
                    }
                    else
                    {
                        progress?.Report(-readbytes); // 用负数表示字节数而不是进度
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
            return ServiceResult.Fail(ex);
        }
        return ServiceResult.Success();
    }
    #endregion
    
    #region API获取

    public async Task<ApiResult> ApiGet(string url)
    {
        _logger.LogInformation("Start getting API: {URL}", url);
        if (string.IsNullOrWhiteSpace(url)) return new ApiResult(0, "The requested URL is empty.");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"LSL/{DesktopConstant.Version}");
        try
        {
            using var response =
                await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);
            if (!response.IsSuccessStatusCode)
            {
                var code = (int)response.StatusCode;
                var msg = response.ReasonPhrase ?? string.Empty;
                _logger.LogError("API returned error code: {code}.\n{message}", code, msg);
                return new ApiResult(code, msg);
            }

            var content = await response.Content.ReadAsStringAsync();
            return new ApiResult((int)response.StatusCode, content);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("API connection timed out: {ex}", ex);
            return new ApiResult((int)HttpStatusCode.RequestTimeout, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("API returned error: {ex}", ex);
            return new ApiResult((int?)ex.StatusCode ?? 0, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("API returned error: {e}", ex.Message);
            return new ApiResult(0, ex.Message);
        }
    }
    #endregion
    
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
        if (response.Content.Headers.ContentDisposition is not null)
        {
            var filename = response.Content.Headers.ContentDisposition.FileNameStar ?? 
                        response.Content.Headers.ContentDisposition.FileName;
            if (!string.IsNullOrEmpty(filename))
            {
                return filename.Trim('"', '\'');// 返回头有文件名再好不过了
            }
        }
        var uri = response.RequestMessage?.RequestUri;
        var uriFilename = Path.GetFileName(uri?.LocalPath);// 不行再找请求头中的文件名称
        return !string.IsNullOrEmpty(uriFilename) ? uriFilename : $"{Guid.NewGuid()}.bin";// 再没有只能用GUID了
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