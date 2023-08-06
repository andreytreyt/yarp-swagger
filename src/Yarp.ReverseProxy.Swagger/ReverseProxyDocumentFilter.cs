using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Swagger.Extensions;

namespace Yarp.ReverseProxy.Swagger
{
    public sealed class ReverseProxyDocumentFilter : IDocumentFilter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private ReverseProxyDocumentFilterConfig _config;

        public ReverseProxyDocumentFilter(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ReverseProxyDocumentFilterConfig> configOptions)
        {
            _config = configOptions.CurrentValue;
            _httpClientFactory = httpClientFactory;
            
            configOptions.OnChange(config => {
                _config = config;
            });
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            if (_config.IsEmpty
                || false == _config.Clusters.TryGetValue(context.DocumentName, out var cluster)
                || true != cluster.Destinations?.Any())
            {
                return;
            }
            var paths = new OpenApiPaths();
            var components = new OpenApiComponents();
            var securityRequirements = new List<OpenApiSecurityRequirement>();

            foreach (var destination in cluster.Destinations)
            {
                if (true != destination.Value.Swaggers?.Any())
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

                    Regex filterRegex = null;
                    if (false == string.IsNullOrWhiteSpace(swagger.PathFilterRegexPattern))
                    {
                        filterRegex = new Regex(swagger.PathFilterRegexPattern);
                    }
                    
                    foreach (var swaggerPath in swagger.Paths)
                    {
                        var stream = httpClient.GetStreamAsync($"{destination.Value.Address}{swaggerPath}").Result;
                        var doc = new OpenApiStreamReader().Read(stream, out _);

                        foreach (var path in doc.Paths)
                        {
                            var key = path.Key;
                            var value = path.Value;

                            if (filterRegex != null
                                && false == filterRegex.IsMatch(key))
                            {
                                continue;
                            }

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