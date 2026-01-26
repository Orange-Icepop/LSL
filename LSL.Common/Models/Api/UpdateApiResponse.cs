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

    public ServiceResult<bool> IsNewerVersion(string oldVersionTag)
    {
        if (string.IsNullOrWhiteSpace(TagName))
            return ServiceResult.Fail<bool>(new ArgumentException("TagName is empty"));
    
        if (string.IsNullOrWhiteSpace(oldVersionTag))
            return ServiceResult.Fail<bool>(new ArgumentException("oldVersionTag is empty", nameof(oldVersionTag)));
    
        try
        {
            // 清理版本字符串
            string newVerStr = TagName.TrimStart('v', 'V');
            string oldVerStr = oldVersionTag.TrimStart('v', 'V');
        
            // 解析版本
            if (!Version.TryParse(newVerStr, out var newVersion))
                return ServiceResult.Fail<bool>(new ArgumentException("TagName is of invalid format"));
        
            if (!Version.TryParse(oldVerStr, out var oldVersion))
                return ServiceResult.Fail<bool>(new ArgumentException("oldVersionTag is of invalid format", nameof(oldVersionTag)));
        
            // 比较版本
            return ServiceResult.Success(newVersion > oldVersion);
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail<bool>(new Exception("An error occured when comparing versions", ex));
        }
    }

    public UpdateApiResponse FormatBody()
    {
        Body = Body.Replace(@"\r", "").Replace(@"\n", "\n");
        return this;
    }
}