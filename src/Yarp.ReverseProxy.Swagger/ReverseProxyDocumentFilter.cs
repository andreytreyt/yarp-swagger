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
        private readonly ReverseProxyDocumentFilterConfig _config;

        public ReverseProxyDocumentFilter(ReverseProxyDocumentFilterConfig config)
        {
            _config = config;
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

            using var httpClient = new HttpClient();

            foreach (var destination in cluster.Destinations)
            {
                if (destination.Value.Swaggers?.Any() != true)
                {
                    continue;
                }

                foreach (var swagger in destination.Value.Swaggers)
                {
                    if (swagger.Paths?.Any() != true)
                    {
                        continue;
                    }

                    foreach (string swaggerPath in swagger.Paths)
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
                    }
                }
            }

            swaggerDoc.Paths = paths;
            swaggerDoc.Components = components;
        }
    }
}