namespace Domain;

public sealed record TextChannel(string Id, string Name, string? CategoryId, bool EveryoneCanView, string Description);
