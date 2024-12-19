using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace Yarp.ReverseProxy.Swagger.Extensions
{
    public static class OpenApiExtensions
    {
        internal static void Add(this OpenApiComponents source, OpenApiComponents components, bool renameDuplicateComponents = false)
        {
            if (components == null)
            {
                return;
            }

            foreach (var data in components.Extensions)
            {
                source.Extensions.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Examples)
            {
                source.Examples.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Callbacks)
            {
                source.Callbacks.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Schemas)
            {
                bool added = source.Schemas.TryAdd(data.Key, data.Value);
                int i = 1;
                while(!added && renameDuplicateComponents)
                {
                    i++;
                    data.Value.Reference.Id = $"{data.Key}{i}";
                    added = source.Schemas.TryAdd($"{data.Key}{i}", data.Value);
                }
            }

            foreach (var data in components.SecuritySchemes)
            {
                source.SecuritySchemes.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Links)
            {
                source.Links.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Headers)
            {
                source.Headers.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Responses)
            {
                source.Responses.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.RequestBodies)
            {
                source.RequestBodies.TryAdd(data.Key, data.Value);
            }

            foreach (var data in components.Parameters)
            {
                source.Parameters.TryAdd(data.Key, data.Value);
            }
        }
    }
}