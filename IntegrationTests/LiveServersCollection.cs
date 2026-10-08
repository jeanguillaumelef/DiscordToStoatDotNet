namespace IntegrationTests;

/// <summary>Tests that run Reconcile against the shared test guild and server. Reconcile mirrors every public channel, so two runs in parallel race each other and create duplicate Mirrors; xUnit runs a collection's tests one at a time.</summary>
[CollectionDefinition(Name)]
public sealed class LiveServersCollection
{
    public const string Name = "Live servers";
}
