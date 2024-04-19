using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Yarp.ReverseProxy.Swagger.Extensions
{
    public static class ReverseProxyBuilderExtensions
    {
        public static IReverseProxyBuilder AddSwagger(
            this IReverseProxyBuilder builder,
            IConfigurationSection configurationSection)
        {
            if (configurationSection == null)
                throw new ArgumentNullException(nameof(configurationSection));

            builder.Services.Configure<ReverseProxyDocumentFilterConfig>(configurationSection);

            var config = configurationSection.Get<ReverseProxyDocumentFilterConfig>();

            ConfigureHttpClient(builder, config);

            return builder;
        }

        public static IReverseProxyBuilder AddSwagger(
            this IReverseProxyBuilder builder,
            ReverseProxyDocumentFilterConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            builder.Services.Configure<ReverseProxyDocumentFilterConfig>(overriddenConfig =>
            {
                overriddenConfig.Routes = config.Routes;
                overriddenConfig.Clusters = config.Clusters;
            });

            ConfigureHttpClient(builder, config);

            return builder;
        }

        private static void ConfigureHttpClient(
            IReverseProxyBuilder builder,
            ReverseProxyDocumentFilterConfig config)
        {
            foreach (var cluster in config.Clusters)
            {
                foreach (var destination in cluster.Value.Destinations)
                {
                    var httpClientBuilder = builder.Services.AddHttpClient($"{cluster.Key}_{destination.Key}");

                    if (false == string.IsNullOrWhiteSpace(destination.Value.AccessTokenClientName))
                    {
                        httpClientBuilder.AddClientAccessTokenHandler(destination.Value.AccessTokenClientName);
                    }
                }
            }
        }
    }
}