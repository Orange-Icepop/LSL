namespace LSL.Common.Utilities;

public static class MemoryInfo
{
    static MemoryInfo()
    {
        CurrentSystemMemory = GetTotalSystemMemory();
    }
    // 获取系统总内存（跨平台实现）
    public static long CurrentSystemMemory { get; }
    private static long GetTotalSystemMemory()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        return gcInfo.TotalAvailableMemoryBytes;
    }
}