using Domain;
using DiscordRepository;
using Microsoft.Extensions.Configuration;
using StoatRepository;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

if (!ulong.TryParse(Require(configuration, "Discord:GuildId"), out var guildId))
{
    throw new InvalidOperationException("Setting 'Discord:GuildId' must be a numeric Discord server ID.");
}

var discordOptions = new DiscordRepositoryOptions(Require(configuration, "Discord:BotToken"), guildId);
var stoatOptions = new StoatRepositoryOptions(
    Require(configuration, "Stoat:BotToken"),
    Require(configuration, "Stoat:ServerId"));

await using var discord = new DiscordRepository.DiscordRepository(discordOptions);
var stoat = new StoatRepository.StoatRepository(stoatOptions);

var result = await new ReconcileRunner(discord, stoat).RunAsync(CancellationToken.None);

foreach (var skipped in result.Skipped)
{
    Console.WriteLine($"Skipped {skipped.SourceId}: {skipped.Reason}");
}

Console.WriteLine($"Reconcile done: {result.Changes.Count} change(s) applied, {result.Skipped.Count} skipped.");

static string Require(IConfiguration configuration, string key) =>
    configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Missing required setting '{key}'. Set it via user-secrets or an environment variable (e.g. {key.Replace(":", "__")}).");
