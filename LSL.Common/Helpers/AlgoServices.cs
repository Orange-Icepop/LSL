using System;

namespace LSL.Common.Helpers
{
    public static class AlgoServices
    {
        public static bool IsGreaterVersion(string current, string newVer)
        {
            var len = current.Length < newVer.Length ? current.Length : newVer.Length;
            for (var i = 0; i < len; i++)
            {
                if (current[i] == '.') continue;
                if (!(int.TryParse(current[i].ToString(), out var first) &&
                      int.TryParse(newVer[i].ToString(), out var second)))
                    throw new ArgumentException("version format not match or invalid.");
                if (first < second) return true;
            }
            return false;
        }
    }
}
