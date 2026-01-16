using System.Text;

namespace LSL.Common.Extensions;

public static class StringBuilderExtensions
{
    /// <summary>
    /// Appends a copy of the specified string followed by the default line terminator to the end of the current StringBuilder object if the specified string is not null or empty.
    /// </summary>
    /// <param name="builder">The current StringBuilder object.</param>
    /// <param name="value">The nullable string to append.</param>
    /// <returns>A reference to this instance after the append operation has completed.</returns>
    public static StringBuilder AppendLineIfNotNullOrEmpty(this StringBuilder builder, string? value)
    {
        if (!string.IsNullOrEmpty(value)) builder.AppendLine(value);
        return builder;
    }
}