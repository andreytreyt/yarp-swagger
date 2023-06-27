using IdentityServer4.Models;

namespace Identity.Configs;

internal sealed class IdentityConfig
{
    public IReadOnlyList<string> Scopes { get; init; } = new List<string>();
    public IReadOnlyList<Client> Clients { get; init; } = new List<Client>();

    internal sealed class Client
    {
        public string ClientId { get; init; }
        public string ClientSecret { get; init; }
        public IReadOnlyList<string> AllowedScopes { get; init; } = new List<string>();
    }
    
    public IEnumerable<IdentityServer4.Models.Client> GetClients()
    {
        return Clients.Select(x => new IdentityServer4.Models.Client
        {
            ClientId = x.ClientId,
            ClientSecrets =
            {
                new Secret(x.ClientSecret.Sha256())
            },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = x.AllowedScopes.ToArray()
        });
    }
    
    public IEnumerable<ApiScope> GetApiScopes()
    {
        return Scopes.Select(x => new ApiScope(x));
    }
}