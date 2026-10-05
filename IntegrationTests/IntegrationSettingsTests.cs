using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public class IntegrationSettingsTests
{
    private static readonly Dictionary<string, string?> AllKeys = new()
    {
        ["Discord:BotToken"] = "discord-token",
        ["Discord:GuildId"] = "111",
        ["Stoat:BotToken"] = "stoat-token",
        ["Stoat:ServerId"] = "server-id",
    };

    [Fact]
    public void Load_WithAllFourKeys_ReturnsThem()
    {
        var settings = IntegrationSettings.Load(Configuration(AllKeys));

        Assert.Equal("discord-token", settings.DiscordBotToken);
        Assert.Equal("111", settings.DiscordGuildId);
        Assert.Equal("stoat-token", settings.StoatBotToken);
        Assert.Equal("server-id", settings.StoatServerId);
    }

    [Theory]
    [InlineData("Discord:BotToken")]
    [InlineData("Discord:GuildId")]
    [InlineData("Stoat:BotToken")]
    [InlineData("Stoat:ServerId")]
    public void Load_WithKeyMissing_ThrowsNamingThatKey(string missingKey)
    {
        var values = new Dictionary<string, string?>(AllKeys);
        values.Remove(missingKey);

        var error = Assert.Throws<InvalidOperationException>(() => IntegrationSettings.Load(Configuration(values)));

        Assert.Contains(missingKey, error.Message);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
