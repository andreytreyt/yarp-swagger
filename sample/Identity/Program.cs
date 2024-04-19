using Identity.Configs;

var builder = WebApplication.CreateBuilder(args);

var identityConfig = builder.Configuration.GetSection("Identity").Get<IdentityConfig>()!;

builder.Services.AddIdentityServer()
    .AddDeveloperSigningCredential()
    .AddInMemoryApiScopes(identityConfig.GetApiScopes())
    .AddInMemoryClients(identityConfig.GetClients());

var app = builder.Build();

app.UseIdentityServer();

app.Run();