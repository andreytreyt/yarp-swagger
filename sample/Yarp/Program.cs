using IdentityModel.Client;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.Configs;
using Yarp.Extensions;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services.AddAccessTokenManagement(options =>
{
    var identityConfig = builder.Configuration.GetSection("Identity").Get<IdentityConfig>()!;
    
    options.Client.Clients.Add("Identity", new ClientCredentialsTokenRequest
    {
        Address = $"{identityConfig.Url}/connect/token",
        ClientId = identityConfig.ClientId,
        ClientSecret = identityConfig.ClientSecret
    });
});

var configuration = builder.Configuration.GetSection("ReverseProxy");
var configurationForOnlyPublishedRoutes = builder.Configuration.GetSection("ReverseProxyOnlyPublishedRoutes");
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(configuration)
    .LoadFromConfig(configurationForOnlyPublishedRoutes)
    .AddSwagger(configuration)
    .AddSwagger(configurationForOnlyPublishedRoutes);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var config = app.Services.GetRequiredService<IOptionsMonitor<ReverseProxyDocumentFilterConfig>>().CurrentValue;
        options.ConfigureSwaggerEndpoints(config);
    });
}

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();
