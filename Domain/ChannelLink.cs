using System.Text.RegularExpressions;

namespace Domain;

/// <summary>Channel Link (ADR 0001): a Mirror Category carries the Discord ID in its title as a trailing "[id]"; a Mirror Channel carries it as the first line of its description.</summary>
public static partial class ChannelLink
{
    public static string CategoryTitle(string name, string sourceId) => $"{name} [{sourceId}]";

    public static string? SourceIdFromCategoryTitle(string title)
    {
        var match = TrailingId().Match(title);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string ChannelDescription(string sourceId) => sourceId;

    public static string? SourceIdFromChannelDescription(string description)
    {
        var firstLine = description.Split('\n')[0].TrimEnd('\r');
        return firstLine.Length > 0 && firstLine.All(char.IsAsciiDigit) ? firstLine : null;
    }

    [GeneratedRegex(@"\[(\d+)\]$")]
    private static partial Regex TrailingId();
}
