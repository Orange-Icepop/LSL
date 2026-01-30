using System.Diagnostics;
using LSL.Common.Models;

namespace LSL.Common.Utilities;

public static class XPlatformOperationHelper
{
    public static Result OpenWebBrowser(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return Result.Fail(new ArgumentException("Bad url format", nameof(url)));
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    ArgumentList = { url },
                    UseShellExecute = false
                }); // xdg-utils dependency required
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    ArgumentList = { url },
                    UseShellExecute = false
                });
            }
        }
        catch (Exception e)
        {
            return Result.Fail(new InvalidOperationException($"Unable to open URL{url}", e));
        }

        return Result.Success();
    }
}