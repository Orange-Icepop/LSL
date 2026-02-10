using System.Text.Json;
using FluentResults;
using LSL.Common.Extensions;
using LSL.Common.Models.Api;
using LSL.Common.Options;

namespace LSL.Common.Utilities;

public static class UpdateHelper
{
    private const string StableVersionEndpoint = "https://api.orllow.cn/lsl/latest/stable";
    private const string PrereleaseVersionEndpoint = "https://api.orllow.cn/lsl/latest/prerelease";

    public static Task<Result<UpdateApiResponse>> QueryLatest(IHttpClientFactory factory, bool prerelease = false)
    {
        using var client = factory.CreateClient();
        client.ResetUserAgent($"LSL/{Constant.Version}");
        return GetUpdateApiAsync(client, prerelease ? PrereleaseVersionEndpoint : StableVersionEndpoint);
    }

    private static async Task<Result<UpdateApiResponse>> GetUpdateApiAsync(HttpClient httpClient, string url)
    {
        try
        {
            using var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var code = (int)response.StatusCode;
                var msg = response.ReasonPhrase ?? string.Empty;
                return Result.Fail<UpdateApiResponse>(
                    new Error($"API returns status code {code}:{msg}"));
            }

            return await ParseApiResultAsync(await response.Content.ReadAsStreamAsync());
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail<UpdateApiResponse>(
                new Error("Update check failed due to network connectivity issue").CausedBy(ex));
        }
        catch (TaskCanceledException ex)
        {
            return Result.Fail<UpdateApiResponse>(new Error("Update check timeout").CausedBy(ex));
        }
        catch (Exception ex)
        {
            return Result.Fail<UpdateApiResponse>(new ExceptionalError(ex));
        }
    }

    private static async Task<Result<UpdateApiResponse>> ParseApiResultAsync(Stream content)
    {
        try
        {
            var res = await JsonSerializer.DeserializeAsync(content, SnakeJsonOptions.Default.UpdateApiResponse);
            return res is null
                ? Result.Fail<UpdateApiResponse>(new Error("Unable to parse the API result"))
                : Result.Ok(res);
        }
        catch (Exception e)
        {
            return Result.Fail<UpdateApiResponse>(new Error("Unable to parse the API result").CausedBy(e));
        }
    }
}