using FluentResults;

namespace LSL.Common.Models.Api;

public class UpdateApiResponse
{
    public string Url { get; set; } = string.Empty;
    public string AssetsUrl { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Prerelease { get; set; } = false;
    public List<GitHubApiAsset> Assets { get; set; } = [];
    public string TarballUrl { get; set; } = string.Empty;
    public string ZipballUrl { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public Result<bool> IsNewerVersion(string oldVersionTag)
    {
        if (string.IsNullOrWhiteSpace(TagName))
            return Result.Fail<bool>(new Error("TagName is empty"));

        if (string.IsNullOrWhiteSpace(oldVersionTag))
            return Result.Fail<bool>(new Error("oldVersionTag is empty"));

        try
        {
            // 清理版本字符串
            var newVerStr = TagName.TrimStart('v', 'V');
            var oldVerStr = oldVersionTag.TrimStart('v', 'V');

            // 解析版本
            if (!Version.TryParse(newVerStr, out var newVersion))
                return Result.Fail<bool>(new Error("TagName is of invalid format"));

            if (!Version.TryParse(oldVerStr, out var oldVersion))
                return Result.Fail<bool>(new Error("oldVersionTag is of invalid format"));

            // 比较版本
            return Result.Ok(newVersion > oldVersion);
        }
        catch (Exception ex)
        {
            return Result.Fail<bool>(new ExceptionalError("An error occured when comparing versions", ex));
        }
    }

    public UpdateApiResponse FormatBody()
    {
        Body = Body.Replace(@"\r", "").Replace(@"\n", "\n");
        return this;
    }
}