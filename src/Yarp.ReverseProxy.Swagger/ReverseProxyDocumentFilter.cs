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
        private readonly IReadOnlyDictionary<string, OperationType> _operationTypeMapping;

        public ReverseProxyDocumentFilter(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ReverseProxyDocumentFilterConfig> configOptions)
        {
            _config = configOptions.CurrentValue;
            _httpClientFactory = httpClientFactory;
            configOptions.OnChange(config => { _config = config; });
            _operationTypeMapping = new Dictionary<string, OperationType>
            {
                {"GET", OperationType.Get},
                {"POST", OperationType.Post},
                {"PUT", OperationType.Put},
                {"DELETE", OperationType.Delete},
                {"PATCH", OperationType.Patch},
                {"HEAD", OperationType.Head},
                {"OPTIONS", OperationType.Options},
                {"TRACE", OperationType.Trace},
            };
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

                    IReadOnlyDictionary<string, IEnumerable<string>> publishedRoutes = null;
                    if (swagger.AddOnlyPublishedPaths)
                    {
                        publishedRoutes = GetPublishedPaths(_config);
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
                                var pathKey = $"{swagger.PrefixPath}{path.Key}";
                                if (!publishedRoutes.ContainsKey(pathKey))
                                {
                                    continue;
                                }

                                var methods = publishedRoutes[pathKey];
                                var operations = _operationTypeMapping
                                    .Where(q => methods.Contains(q.Key))
                                    .Select(q => q.Value)
                                    .ToList();
                                var operationKeys = path.Value.Operations.Keys.ToList();
                                
                                foreach (var operationKey in operationKeys)
                                {
                                    if (false == operations.Contains(operationKey))
                                    {
                                        path.Value.Operations.Remove(operationKey);
                                    }
                                }
                            }

                            paths.TryAdd($"{swagger.PrefixPath}{key}", value);
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

        private static IReadOnlyDictionary<string, IEnumerable<string>> GetPublishedPaths(ReverseProxyDocumentFilterConfig config)
        {
            var validRoutes = new Dictionary<string, IEnumerable<string>>();
            foreach (var route in config.Routes)
            {
                if (route.Value?.Match.Path == null)
                {
                    continue;
                }

                if (false == validRoutes.ContainsKey(route.Value.Match.Path))
                {
                    validRoutes.TryAdd(route.Value.Match.Path, route.Value.Match.Methods);
                }
                else
                {
                    if (route.Value.Match.Methods != null)
                        validRoutes[route.Value.Match.Path] =
                            validRoutes[route.Value.Match.Path].Concat(route.Value.Match.Methods);
                }
            }

            return validRoutes;
        }
    }
}