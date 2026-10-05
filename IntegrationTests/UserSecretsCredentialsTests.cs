using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

[Trait("Category", "Integration")]
public class UserSecretsCredentialsTests
{
    // Fails, never skips, when the test user-secrets store lacks any of the four keys.
    [Fact]
    public void UserSecrets_ProvideAllFourCredentials()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<UserSecretsCredentialsTests>(optional: true)
            .Build();

        IntegrationSettings.Load(configuration);
    }
}
