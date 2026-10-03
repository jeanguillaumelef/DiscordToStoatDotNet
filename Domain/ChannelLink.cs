using System.Text.RegularExpressions;

namespace Domain;

/// <summary>Channel Link for categories (ADR 0001): the Discord ID is carried in the Mirror Category's title as a trailing "[id]".</summary>
public static partial class ChannelLink
{
    public static string CategoryTitle(string name, string sourceId) => $"{name} [{sourceId}]";

    public static string? SourceIdFromCategoryTitle(string title)
    {
        var match = TrailingId().Match(title);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"\[(\d+)\]$")]
    private static partial Regex TrailingId();
}
