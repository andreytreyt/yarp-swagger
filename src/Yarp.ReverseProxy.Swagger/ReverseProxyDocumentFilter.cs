using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Swagger.Extensions;

namespace Yarp.ReverseProxy.Swagger
{
    public sealed class ReverseProxyDocumentFilter : IDocumentFilter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ReverseProxyDocumentFilterConfig _config;

        public ReverseProxyDocumentFilter(
            IHttpClientFactory httpClientFactory,
            ReverseProxyDocumentFilterConfig config)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (_config.IsEmpty
                || false == _config.Clusters.TryGetValue(context.DocumentName, out var cluster)
                || cluster.Destinations?.Any() != true)
            {
                return;
            }

            var paths = new OpenApiPaths();
            var components = new OpenApiComponents();
            var securityRequirements = new List<OpenApiSecurityRequirement>();

            foreach (var destination in cluster.Destinations)
            {
                if (destination.Value.Swaggers?.Any() != true)
                {
                    continue;
                }

                var httpClient = _httpClientFactory.CreateClient($"{context.DocumentName}_{destination.Key}");

                foreach (var swagger in destination.Value.Swaggers)
                {
                    if (swagger.Paths?.Any() != true)
                    {
                        continue;
                    }
                    
                    foreach (var swaggerPath in swagger.Paths)
                    {
                        var stream = httpClient.GetStreamAsync($"{destination.Value.Address}{swaggerPath}").Result;
                        var doc = new OpenApiStreamReader().Read(stream, out _);

                        foreach (var path in doc.Paths)
                        {
                            var key = path.Key;
                            var value = path.Value;

                            paths.Add($"{swagger.PrefixPath}{key}", value);
                        }

                        components.Add(doc.Components);
                        securityRequirements.AddRange(doc.SecurityRequirements);
                    }
                }
            }

            swaggerDoc.Paths = paths;
            swaggerDoc.SecurityRequirements = securityRequirements;
            swaggerDoc.Components = components;
        }
    }
}