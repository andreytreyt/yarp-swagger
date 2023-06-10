using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Yarp.ReverseProxy.Swagger.Extensions;

public static class ReverseProxyBuilderExtensions
{
    public static IReverseProxyBuilder AddSwagger(
        this IReverseProxyBuilder builder,
        IConfigurationSection configurationSection)
    {
        if (configurationSection == null)
            throw new ArgumentNullException(nameof (configurationSection));
        
        builder.Services.AddSingleton(_ => configurationSection.Get<ReverseProxyDocumentFilterConfig>());
        
        return builder;
    }
}