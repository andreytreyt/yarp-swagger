using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<ReverseProxyDocumentFilter>(ReverseProxyDocumentFilterConfig.Empty);
});

var configuration = builder.Configuration.GetSection("ReverseProxy");
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(configuration)
    .AddSwagger(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var gatewayDocumentFilterConfig = app.Services.GetService<ReverseProxyDocumentFilterConfig>();
        if (gatewayDocumentFilterConfig != null)
        {
            foreach (var cluster in gatewayDocumentFilterConfig.Clusters)
            {
                options.SwaggerEndpoint($"/swagger/{cluster.Key}/swagger.json", cluster.Key);
            }
        }
    });
}

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();