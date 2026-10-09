namespace Domain;

/// <summary>Overwrites a Mirror Channel's description (text or voice) because it differs from what the Bridge would write.</summary>
public sealed record UpdateChannelDescription(string StoatChannelId, string Description) : Change;
