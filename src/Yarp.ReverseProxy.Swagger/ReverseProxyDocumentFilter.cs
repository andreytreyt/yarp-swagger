using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Swashbuckle.AspNetCore.SwaggerGen;
using Yarp.ReverseProxy.Swagger.Extensions;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Yarp.ReverseProxy.Swagger
{
    public sealed class ReverseProxyDocumentFilter : IDocumentFilter
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IReadOnlyDictionary<string, OperationType> _operationTypeMapping;
        private readonly List<ITransformFactory> _factories;
        
        private ReverseProxyDocumentFilterConfig config;

        public ReverseProxyDocumentFilter(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<ReverseProxyDocumentFilterConfig> configOptions,
            IEnumerable<ITransformFactory> factories)
        {
            _factories = factories?.ToList();
            config = configOptions.CurrentValue;
            _httpClientFactory = httpClientFactory;

            configOptions.OnChange(x => { config = x; });

            _operationTypeMapping = new Dictionary<string, OperationType>
            {
                { "GET", OperationType.Get },
                { "POST", OperationType.Post },
                { "PUT", OperationType.Put },
                { "DELETE", OperationType.Delete },
                { "PATCH", OperationType.Patch },
                { "HEAD", OperationType.Head },
                { "OPTIONS", OperationType.Options },
                { "TRACE", OperationType.Trace },
            };
        }

        public void Apply(
            OpenApiDocument swaggerDoc,
            DocumentFilterContext context
        )
        {
            if (config.IsEmpty)
            {
                return;
            }

            IReadOnlyDictionary<string, ReverseProxyDocumentFilterConfig.Cluster> clusters;

            if (config.Swagger.IsCommonDocument)
            {
                clusters = config.Clusters;
            }
            else
            {
                clusters = config.Clusters
                    .Where(x => x.Key == context.DocumentName)
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            Apply(swaggerDoc, clusters);
        }

        private void Apply(
            OpenApiDocument swaggerDoc,
            IReadOnlyDictionary<string, ReverseProxyDocumentFilterConfig.Cluster> clusters
        )
        {
            if (true != clusters?.Any())
            {
                return;
            }

            var info = swaggerDoc.Info;
            var paths = new OpenApiPaths();
            var components = new OpenApiComponents();
            var securityRequirements = new List<OpenApiSecurityRequirement>();
            var tags = new List<OpenApiTag>();

            foreach (var clusterKeyValuePair in clusters)
            {
                var clusterKey = clusterKeyValuePair.Key;
                var cluster = clusterKeyValuePair.Value;

                if (true != cluster.Destinations?.Any())
                {
                    continue;
                }

                foreach (var destination in cluster.Destinations)
                {
                    if (true != destination.Value.Swaggers?.Any())
                    {
                        continue;
                    }

                    var httpClient = _httpClientFactory.CreateClient($"{clusterKey}_{destination.Key}");

                    foreach (var swagger in destination.Value.Swaggers)
                    {
                        if (swagger.Paths?.Any() != true)
                        {
                            continue;
                        }

                        IReadOnlyDictionary<string, IEnumerable<string>> publishedRoutes = null;
                        if (swagger.AddOnlyPublishedPaths)
                        {
                            publishedRoutes = GetPublishedPaths(config);
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

                            if (swagger.MetadataPath == swaggerPath)
                            {
                                info = doc.Info;
                            }

                            foreach (var path in doc.Paths)
                            {
                                var key = path.Key;
                                var value = path.Value;

                                if (filterRegex != null
                                    && false == filterRegex.IsMatch(key))
                                {
                                    continue;
                                }

                                var operationKeys = path.Value.Operations.Keys.ToList();
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

                                    foreach (var operationKey in operationKeys)
                                    {
                                        if (false == operations.Contains(operationKey))
                                        {
                                            path.Value.Operations.Remove(operationKey);
                                        }
                                    }
                                }

                                ApplySwaggerTransformation(operationKeys, path, clusterKey);

                                paths.TryAdd($"{swagger.PrefixPath}{key}", value);
                            }

                            components.Add(doc.Components);
                            securityRequirements.AddRange(doc.SecurityRequirements);
                            tags.AddRange(doc.Tags);
                        }
                    }
                }
            }

            swaggerDoc.Info = info;
            swaggerDoc.Paths = paths;
            swaggerDoc.SecurityRequirements = securityRequirements;
            swaggerDoc.Components = components;
            swaggerDoc.Tags = tags;
        }

        private static IReadOnlyDictionary<string, IEnumerable<string>> GetPublishedPaths(
            ReverseProxyDocumentFilterConfig config)
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
                    {
                        validRoutes[route.Value.Match.Path] =
                            validRoutes[route.Value.Match.Path].Concat(route.Value.Match.Methods);
                    }
                }
            }

            return validRoutes;
        }

        private void ApplySwaggerTransformation(List<OperationType> operationKeys,
            KeyValuePair<string, OpenApiPathItem> path, string clusterKey)
        {
            var factories = _factories?.Where(x => x is ISwaggerTransformFactory).ToList();

            if (factories == null) return;

            foreach (var operationKey in operationKeys)
            {
                path.Value.Operations.TryGetValue(operationKey, out var operation);

                var transforms = config.Routes
                    .Where(x => x.Value.ClusterId == clusterKey)
                    .Where(x => x.Value.Transforms != null)
                    .SelectMany(x => x.Value.Transforms)
                    .ToList();

                foreach (var swaggerFactory in factories.Select(factory => factory as ISwaggerTransformFactory))
                {
                    foreach (var transform in transforms)
                    {
                        swaggerFactory?.Build(operation, transform);
                    }
                }
            }
        }
    }
}