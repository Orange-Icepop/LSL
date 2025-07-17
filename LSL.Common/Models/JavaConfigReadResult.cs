using System.ComponentModel;

namespace LSL.Common.Models;
[ImmutableObject(true)]
public class JavaConfigReadResult
{
    public IEnumerable<string> NotFound { get; }
    public IEnumerable<string> NotJava { get; }

    public JavaConfigReadResult(IEnumerable<string>? notFound = null, IEnumerable<string>? notJava = null)
    {
        NotFound = notFound ?? new List<string>();
        NotJava = notJava ?? new List<string>();
    }
}