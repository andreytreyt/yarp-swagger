![dotnet_main](https://github.com/andreytreyt/yarp-swagger/actions/workflows/dotnet.yml/badge.svg?branch=main)
![release](https://github.com/andreytreyt/yarp-swagger/actions/workflows/release.yml/badge.svg)
[![nuget_v](https://img.shields.io/nuget/v/Treyt.Yarp.ReverseProxy.Swagger?logo=nuget)](https://www.nuget.org/packages/Treyt.Yarp.ReverseProxy.Swagger/)
![nuget_dt](https://img.shields.io/nuget/dt/Treyt.Yarp.ReverseProxy.Swagger?logo=nuget)

# Getting Started

Configure [Swagger](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle) and [YARP](https://microsoft.github.io/reverse-proxy/articles/getting-started.html) for your project.

### From Configuration

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

Update Program.cs:

```csharp
var configuration = builder.Configuration.GetSection("ReverseProxy");
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(configuration)
    .AddSwagger(configuration); // <-- this line
```

### From Code

Update Program.cs:

```csharp
RouteConfig[] GetRoutes()
{
    return new[]
    {
        new RouteConfig
        {
            RouteId = "App1Route",
            ClusterId = "App1Cluster",
            Match = new RouteMatch
            {
                Path = "/proxy-app1/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    {"PathPattern", "{**catch-all}"}
                }
            }
        }
    };
}

ClusterConfig[] GetClusters()
{
    return new[]
    {
        new ClusterConfig
        {
            ClusterId = "App1Cluster",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                {
                    "Default", new DestinationConfig
                    {
                        Address = "https://localhost:5101"
                    }
                }
            }
        }
    };
}

ReverseProxyDocumentFilterConfig GetSwaggerConfig()
{
    return new ReverseProxyDocumentFilterConfig
    {
        Clusters = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster>
        {
            {
                "App1Cluster", new ReverseProxyDocumentFilterConfig.Cluster
                {
                    Destinations = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster.Destination>
                    {
                        {
                            "Default", new ReverseProxyDocumentFilterConfig.Cluster.Destination
                            {
                                Address = "https://localhost:5101",
                                Swaggers = new[]
                                {
                                    new ReverseProxyDocumentFilterConfig.Cluster.Destination.Swagger
                                    {
                                        PrefixPath = "/proxy-app1",
                                        Paths = new[] {"/swagger/v1/swagger.json"}
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    };
}

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters())
    .AddSwagger(GetSwaggerConfig()); // <-- this line
```

### Common

Create (if doesn't exist) or update [ConfigureSwaggerOptions.cs](sample/Yarp/Configs/ConfigureSwaggerOptions.cs):

**When loading from code:**

```csharp
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
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
}
```

**When loading from appSettings.json**

```csharp
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IConfiguration _configuration;

        public ConfigureSwaggerOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Configure(SwaggerGenOptions options)
        {
            var filterDescriptors = new List<FilterDescriptor>();
            var clusterSectionName = "ReverseProxy:Routes";
            var clusters = _configuration.GetSection(clusterSectionName).GetChildren();

            foreach (var cluster in clusters.AsEnumerable())
            {
                var clusterId = _configuration.GetSection($"{clusterSectionName}:{cluster.Key}:ClusterId").Value;
                options.SwaggerDoc(clusterId, new OpenApiInfo { Title = clusterId, Version = clusterId });
            }

            filterDescriptors.Add(new FilterDescriptor
            {
                Type = typeof(ReverseProxyDocumentFilter),
                Arguments = Array.Empty<object>()
            });

            options.DocumentFilterDescriptors = filterDescriptors;
        }
    }
```



Update Program.cs:

```csharp
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();
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

# Authentication and Authorization

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

# Filtering of Paths

### By Regex Pattern

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

### By Only Published Paths

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

# Swagger Metadata

If you want to publish the API metadata ([OpenAPI info object](https://swagger.io/specification/#info-object)) from the configured swaggers, specify a path from the list of configured paths using the `MetadataPath` option. This will overwrite the cluster metadata with that of the swagger metadata.

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
                "MetadataPath": "/swagger/v1/swagger.json", // <-- this line
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
