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

    /// <summary>Stoat allows at most 1024 characters (UTF-16 units) in a channel description.</summary>
    public const int MaxDescriptionLength = 1024;

    /// <summary>Builds the Mirror Channel description: the Discord ID on line 1, then the Channel Description. Truncates the Channel Description, never the ID line, and never splits a surrogate pair.</summary>
    public static ComposedChannelDescription ComposeChannelDescription(string sourceId, string channelDescription)
    {
        if (channelDescription.Length == 0)
        {
            return new ComposedChannelDescription(sourceId, false);
        }

        var full = $"{sourceId}\n{channelDescription}";
        if (full.Length <= MaxDescriptionLength)
        {
            return new ComposedChannelDescription(full, false);
        }

        var keep = MaxDescriptionLength;
        if (char.IsHighSurrogate(full[keep - 1]))
        {
            keep--;
        }

        return new ComposedChannelDescription(full[..keep], true);
    }

    public static string? SourceIdFromChannelDescription(string description)
    {
        var firstLine = description.Split('\n')[0].TrimEnd('\r');
        return firstLine.Length > 0 && firstLine.All(char.IsAsciiDigit) ? firstLine : null;
    }

    [GeneratedRegex(@"\[(\d+)\]$")]
    private static partial Regex TrailingId();
}
