namespace LSL.Common.Extensions;

public static class HttpClientExtensions
{
    public static void ResetUserAgent(this HttpClient client, string userAgent)
    {
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    }
}