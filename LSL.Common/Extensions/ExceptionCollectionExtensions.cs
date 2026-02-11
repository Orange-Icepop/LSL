namespace LSL.Common.Extensions;

public static class ExceptionCollectionExtensions
{
    public static string FlattenToString(this IEnumerable<Exception> exceptions)
    {
        return new AggregateException(exceptions).ToString();
    }
    public static string GetMessages(this IEnumerable<Exception> exceptions)
    {
        return string.Join('\n', exceptions.Select(exception => exception.Message));
    }
}