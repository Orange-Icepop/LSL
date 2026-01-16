using System.Text;
using LSL.Common.Models.ServerConfigs;

namespace LSL.Common.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendLineIfNotNullOrEmpty(this StringBuilder builder, string? value)
    {
        if (!string.IsNullOrEmpty(value)) builder.AppendLine(value);
        return builder;
    }
}