namespace LSL.Common.Options;

public static class ConcurrencyOptions
{
    public static readonly ParallelOptions ConcurrencyLimit = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
    public static ParallelOptions Create(CancellationToken ct) => new() { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct };
}