namespace LSL.Common.Options;

public class ConcurrencyOptions
{
    public static readonly ParallelOptions ConcurrencyLimit = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
}