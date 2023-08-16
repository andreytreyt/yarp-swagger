using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public ReverseProxyDocumentFilter(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ReverseProxyDocumentFilterConfig> configOptions,
            IConfiguration configuration)
        {
            _config = configOptions.CurrentValue;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            configOptions.OnChange(config => { _config = config; });
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

                    Dictionary<string, List<string>> publishedRoutes = null;
                    if (swagger.AddOnlyPublishedPaths)
                    {
                        publishedRoutes = GetPublishedPaths(_configuration);
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

                            if (publishedRoutes != null)
                            {
                                if (!CheckSwaggerDefinitionIsValid(swagger, publishedRoutes, path))
                                {
                                    continue;
                                }
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

        private static Dictionary<string, List<string>> GetPublishedPaths(IConfiguration configuration)
        {
            var validRoutes = new Dictionary<string, List<string>>();
            var allConfigs = configuration.AsEnumerable().ToImmutableDictionary();
            var allPaths = allConfigs.Where(q => q.Key.EndsWith("Match:Path"));
            foreach (var (key, routeValue) in allPaths)
            {
                var methods = new List<string>();
                for (var i = 0; i < 10; i++)
                {
                    var methodKey = key.Replace("Match:Path", $"Match:Methods:{i}");
                    if (!allConfigs.TryGetValue(methodKey, out var config))
                        continue;

                    if (config != null)
                        methods.Add(config);
                }

                if (!validRoutes.ContainsKey(routeValue))
                {
                    validRoutes.Add(routeValue, methods);
                }
                else
                {
                    validRoutes[routeValue].AddRange(methods);
                }
            }

            return validRoutes;
        }

        private static bool CheckSwaggerDefinitionIsValid(ReverseProxyDocumentFilterConfig.Cluster.Destination.Swagger swagger, IReadOnlyDictionary<string, List<string>> publishedPaths, KeyValuePair<string, OpenApiPathItem> path)
        {
            var pathKey = $"{swagger.PrefixPath}{path.Key}";
            if (!publishedPaths.TryGetValue(pathKey, out var methods))
            {
                return false;
            }

            var pathMethods = path.Value.Operations.Select(q => q.Key.ToString().ToUpperInvariant()).ToList();
            return methods.All(method => pathMethods.Contains(method.ToUpperInvariant()));
        }
    }
}