using System.Text;

namespace SWP391_AutoWashPro_BE.Service.AiService;

internal static class MarkdownBoldSanitizer
{
    internal static string RemoveBoldMarkers(string? content)
    {
        if (string.IsNullOrEmpty(content) || !content.Contains("**", StringComparison.Ordinal))
        {
            return content ?? string.Empty;
        }

        var sanitized = content;

        while (true)
        {
            var updated = RemoveOneBoldPass(sanitized);
            if (string.Equals(updated, sanitized, StringComparison.Ordinal))
            {
                return updated;
            }

            sanitized = updated;
        }
    }

    private static string RemoveOneBoldPass(string content)
    {
        var builder = new StringBuilder(content.Length);
        var index = 0;

        while (index < content.Length)
        {
            var openingIndex = content.IndexOf("**", index, StringComparison.Ordinal);
            if (openingIndex < 0)
            {
                builder.Append(content, index, content.Length - index);
                break;
            }

            var closingSearchStart = openingIndex + 2;
            var closingIndex = content.IndexOf("**", closingSearchStart, StringComparison.Ordinal);
            if (closingIndex < 0)
            {
                builder.Append(content, index, content.Length - index);
                break;
            }

            builder.Append(content, index, openingIndex - index);
            builder.Append(content, closingSearchStart, closingIndex - closingSearchStart);
            index = closingIndex + 2;
        }

        return builder.ToString();
    }
}
