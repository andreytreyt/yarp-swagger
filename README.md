![dotnet_main](https://github.com/andreytreyt/yarp-swagger/actions/workflows/dotnet.yml/badge.svg?branch=main)
![release](https://github.com/andreytreyt/yarp-swagger/actions/workflows/release.yml/badge.svg)
[![nuget_v](https://img.shields.io/nuget/v/Treyt.Yarp.ReverseProxy.Swagger?logo=nuget)](https://www.nuget.org/packages/Treyt.Yarp.ReverseProxy.Swagger/)
![nuget_dt](https://img.shields.io/nuget/dt/Treyt.Yarp.ReverseProxy.Swagger?logo=nuget)

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

Create (if doesn't exist) or update [ConfigureSwaggerOptions.cs](sample/Yarp/Configs/ConfigureSwaggerOptions.cs):

```csharp
public void Configure(SwaggerGenOptions options)
{
    var filterDescriptors = new List<FilterDescriptor>();

    foreach (var cluster in _reverseProxyDocumentFilterConfig.Clusters)
    {
        options.SwaggerDoc(cluster.Key, new OpenApiInfo {Title = cluster.Key, Version = cluster.Key});
    }

    filterDescriptors.Add(new FilterDescriptor
    {
        Type = typeof(ReverseProxyDocumentFilter),
        Arguments = Array.Empty<object>()
    });

    options.DocumentFilterDescriptors = filterDescriptors;
}
```

Update Program.cs:

```csharp
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<ReverseProxyDocumentFilter>();
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
    var config = app.Services.GetRequiredService<IOptionsMonitor<ReverseProxyDocumentFilterConfig>>().CurrentValue;
    foreach (var cluster in config.Clusters)
    {
        options.SwaggerEndpoint($"/swagger/{cluster.Key}/swagger.json", cluster.Key);
    }
});
```

After run you will get generated Swagger files by clusters:

![image](https://raw.githubusercontent.com/andreytreyt/yarp-swagger/main/README.png)

## Authentication and Authorization

Update appsettings.json:

```json lines
{
  "ReverseProxy": {
    "Clusters": {
      "App1Cluster": {
        "Destinations": {
          "Default": {
            "Address": "https://localhost:5101",
            "AccessTokenClientName": "Identity", // <-- this line
            "Swaggers": [
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

Update Program.cs:

```csharp
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
```

## Filtering of Paths

Update appsettings.json:

```json lines
{
  "ReverseProxy": {
    "Clusters": {
      "App1Cluster": {
        "Destinations": {
          "Default": {
            "Address": "https://localhost:5101",
            "Swaggers": [
              {
                "PrefixPath": "/proxy-app1",
                "PathFilterRegexPattern": ".*", // <-- this line
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

## Filtering of Published Paths

If you want to publish only some configured path in YARP, you can use the `AddOnlyPublishedPaths` option.
(For using these options, you need to add Methods configuration in the Match block of the YARP configuration.)
Update appsettings.json:

```json lines
{
  "ReverseProxy": {
    "Clusters": {
      "App1Cluster": {
        "Destinations": {
          "Default": {
            "Address": "https://localhost:5101",
            "Swaggers": [
              {
                "PrefixPath": "/proxy-app1",
                "AddOnlyPublishedPaths": true, // <-- this line
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

# Contributing

This project welcomes contributions and suggestions through issues and pull requests.
