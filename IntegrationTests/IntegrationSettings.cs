using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public sealed record IntegrationSettings(
    string DiscordBotToken,
    string DiscordGuildId,
    string StoatBotToken,
    string StoatServerId)
{
    public static IntegrationSettings Load(IConfiguration configuration) => new(
        Require(configuration, "Discord:BotToken"),
        Require(configuration, "Discord:GuildId"),
        Require(configuration, "Stoat:BotToken"),
        Require(configuration, "Stoat:ServerId"));

    private static string Require(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"Missing integration test setting '{key}'. Set it with: dotnet user-secrets --project IntegrationTests set \"{key}\" \"<value>\"");
}
