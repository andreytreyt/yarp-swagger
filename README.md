![dotnet_main](https://github.com/andreytreyt/yarp-swagger/actions/workflows/dotnet.yml/badge.svg?branch=main)

## Introduction

It's an easy to use Swagger extension for [the YARP project](https://github.com/microsoft/reverse-proxy).

## Getting Started

Configure [Swagger](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle) and [YARP](https://microsoft.github.io/reverse-proxy/articles/getting-started.html) for your project.

Update appsettings.json:

```json lines
{
  "ReverseProxy": {
    "Clusters": {
      "App1Cluster": {
        "Destinations": {
          "Default": {
            "Address": "https://localhost:5101",
            "Swaggers": [ // <-- this block
              {
                "PrefixPath": "/proxy-app1",
                "Paths": [
                  "/swagger/v1/swagger.json"
                ]
              }
            ]
          }
        }
      }
    }
  }
}
```

Create (if doesn't exist) or update [ConfigureSwaggerOptions.cs](sample/Yarp/ConfigureSwaggerOptions.cs):

```csharp
public void Configure(SwaggerGenOptions options)
{
    var gatewayDocumentFilterConfig = _serviceProvider.GetService<ReverseProxyDocumentFilterConfig>();
    if (gatewayDocumentFilterConfig != null)
    {
        foreach (var cluster in gatewayDocumentFilterConfig.Clusters)
        {
            options.SwaggerDoc(cluster.Key, new OpenApiInfo { Title = cluster.Key, Version = cluster.Key });
        }
            
        options.DocumentFilterDescriptors = new List<FilterDescriptor>
        {
            new FilterDescriptor
            {
                Type = typeof(ReverseProxyDocumentFilter),
                Arguments = new object[]{ gatewayDocumentFilterConfig }
            }
        };
    }
}
```

Update Program.cs:

```csharp
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<ReverseProxyDocumentFilter>(ReverseProxyDocumentFilterConfig.Empty);
});
```

```csharp
var configuration = builder.Configuration.GetSection("ReverseProxy");
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(configuration)
    .AddSwagger(configuration); // <-- this line
```

```csharp
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
```

