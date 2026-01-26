namespace LSL.Common.Models.Api;

public class GitHubApiAsset
{
    public string Url { get; set; } = string.Empty;
    public long Id { get; set; } = -1;
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; } = -1;
    public string Digest { get; set; } = string.Empty;
    public string BrowserDownloadUrl { get; set; } = string.Empty;
}