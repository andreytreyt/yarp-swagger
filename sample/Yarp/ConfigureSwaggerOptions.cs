using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Swagger;

namespace Yarp;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IEnumerable<ReverseProxyDocumentFilterConfig> _reverseProxyDocumentFilterConfigs;

    public ConfigureSwaggerOptions(IEnumerable<ReverseProxyDocumentFilterConfig> reverseProxyDocumentFilterConfigs)
    {
        _reverseProxyDocumentFilterConfigs = reverseProxyDocumentFilterConfigs;
    }

    public void Configure(SwaggerGenOptions options)
    {
        var filterDescriptors = new List<FilterDescriptor>();
        
        foreach (var reverseProxyDocumentFilterConfig in _reverseProxyDocumentFilterConfigs)
        {
            foreach (var cluster in reverseProxyDocumentFilterConfig.Clusters)
            {
                options.SwaggerDoc(cluster.Key, new OpenApiInfo { Title = cluster.Key, Version = cluster.Key });
            }
            
            filterDescriptors.Add(new FilterDescriptor
            {
                Type = typeof(ReverseProxyDocumentFilter),
                Arguments = new object[]{ reverseProxyDocumentFilterConfig }
            });
        }

        options.DocumentFilterDescriptors = filterDescriptors;
    }
}