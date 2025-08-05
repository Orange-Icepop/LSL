using System.Collections.Immutable;

namespace LSL.Updater;

internal static class ArgumentMatcher
{
    internal static readonly ImmutableList<string> Arguments =
    [
        "BaseDirectory", "TempDirectory", "ZipFilePath", "StartReplacement"
    ];

    internal static string? IsValid(string[] args, out ArgumentMatchResult result)
    {
        try
        {
            if (args.Length != 3) throw new ArgumentException($"Invalid argument count: {args.Length}");
            string? br = null;
            string? tr = null;
            string? zfp = null;
            bool? sr = null;
            foreach (var arg in args)
            {
                if(!arg.StartsWith("--")) throw new ArgumentException($"Invalid argument: {arg}. An argument should start with '--'.");
                var tmp = arg.TrimStart('-');
                var pair = tmp.Split('=');
                if (pair.Length != 2) throw new ArgumentException($"Invalid argument: {arg}. The argument key and value must be split with'='.");
                if (!Arguments.Contains(pair[0])) throw new ArgumentException($"Invalid argument: {arg}");
                switch (pair[0])
                {
                    case "BaseDirectory":
                    {
                        if (!Directory.Exists(pair[1].Trim('\''))) throw new ArgumentException($"Invalid argument of path: {arg}");
                        br = pair[1].Trim('\'', '\"');
                        break;
                    }
                    case "TempDirectory":
                    {
                        if (!Directory.Exists(pair[1].Trim('\''))) throw new ArgumentException($"Invalid argument of path: {arg}");
                        tr = pair[1].Trim('\'', '\"');
                        break;
                    }
                    case "ZipFilePath":
                    {
                        if (!File.Exists(pair[1].Trim('\''))) throw new ArgumentException($"Invalid argument of path: {arg}");
                        zfp = pair[1].Trim('\'', '\"');
                        break;
                    }
                    case "StartReplacement": sr = pair[1] == "true"; break;
                }
            }
            if (br is null || zfp is null || tr is null || sr is null)throw new ArgumentException($"Invalid argument count: {args.Length}");
            result = new ArgumentMatchResult(br, tr, zfp, (bool)sr);
            return null;
        }
        catch (ArgumentException e)
        {
            result = new ArgumentMatchResult("", "", "", false);
            return e.Message;
        }
    }
}

public record struct ArgumentMatchResult(string BaseDirectory, string TempDirectory, string ZipFilePath, bool StartReplacement);