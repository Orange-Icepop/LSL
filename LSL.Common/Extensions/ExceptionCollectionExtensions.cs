namespace LSL.Common.Extensions;

public static class ExceptionCollectionExtensions
{
    extension(IEnumerable<Exception> exceptions)
    {
        public string FlattenToString()
        {
            return new AggregateException(exceptions).ToString();
        }

        public string GetMessages()
        {
            return string.Join('\n', exceptions.Select(exception => exception.Message));
        }
    }
}