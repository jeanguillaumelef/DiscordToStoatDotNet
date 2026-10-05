# DiscordToStoat

## Integration tests

The `IntegrationTests` project runs against real Discord and Stoat test servers. It fails, rather than skips, when a credential is missing. Set the four keys in its own user-secrets store (separate from the app's):

```bash
dotnet user-secrets --project IntegrationTests set "Discord:BotToken" "<token>"
dotnet user-secrets --project IntegrationTests set "Discord:GuildId" "<guild id>"
dotnet user-secrets --project IntegrationTests set "Stoat:BotToken" "<token>"
dotnet user-secrets --project IntegrationTests set "Stoat:ServerId" "<server id>"
```

Unit tests only: `dotnet test --filter "Category!=Integration"`. Integration tests only: `dotnet test --filter "Category=Integration"`.
