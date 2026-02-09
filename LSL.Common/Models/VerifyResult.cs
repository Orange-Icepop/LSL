namespace LSL.Common.Models;

/// <summary>
///     The result of validation components.
/// </summary>
/// <param name="key">The validated key.</param>
/// <param name="passed">Whether the value passes validation.</param>
/// <param name="reason">Why the value doesn't pass validation.</param>
public class VerifyResult(string key, bool passed, string? reason)
{
    public string Key { get; } = key;
    public bool Passed { get; } = passed;
    public string? Reason { get; } = reason;

    public static VerifyResult Fail(string key, string? reason)
    {
        return new VerifyResult(key, false, reason);
    }

    public static VerifyResult Success(string key)
    {
        return new VerifyResult(key, true, null);
    }
}