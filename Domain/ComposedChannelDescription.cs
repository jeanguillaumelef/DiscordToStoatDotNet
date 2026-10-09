namespace Domain;

/// <summary>A composed Mirror Channel description (Channel Link line plus Channel Description), and whether the Channel Description had to be truncated to fit.</summary>
public sealed record ComposedChannelDescription(string Text, bool WasTruncated);
