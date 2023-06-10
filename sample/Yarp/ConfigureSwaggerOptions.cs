using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Swagger;

namespace Yarp;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public ConfigureSwaggerOptions(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

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
}