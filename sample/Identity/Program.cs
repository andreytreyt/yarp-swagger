using Identity.Configs;
using Duende.IdentityServer.Models;

var builder = WebApplication.CreateBuilder(args);

var identityConfig = builder.Configuration.GetSection("Identity").Get<IdentityConfig>()!;

builder.Services.AddIdentityServer()
    .AddDeveloperSigningCredential()
    .AddInMemoryApiScopes(identityConfig.GetApiScopes().Cast<ApiScope>())
    .AddInMemoryClients(identityConfig.GetClients().Cast<Client>());

var app = builder.Build();

app.UseIdentityServer();

app.Run();