using System.Collections.Generic;
using Microsoft.OpenApi;

namespace Yarp.ReverseProxy.Swagger.Extensions
{
    public static class OpenApiExtensions
    {
        internal static void Add(this OpenApiComponents source, OpenApiComponents components, bool renameDuplicateSchemas = false)
        {
            if (components == null)
            {
                return;
            }
            
            if(components.Extensions != null)
            {
                foreach (var data in components.Extensions)
                {
                    source.Extensions.TryAdd(data.Key, data.Value);
                }
            }

            if(components.Examples != null)
            {
                foreach (var data in components.Examples)
                {
                    source.Examples.TryAdd(data.Key, data.Value);
                }
            }
            
            if(components.Callbacks != null)
            {
                foreach (var data in components.Callbacks)
                {
                    source.Callbacks.TryAdd(data.Key, data.Value);
                }
            }

            if(components.Schemas != null)
            {
                foreach ((string key, IOpenApiSchema openApiSchema) in components.Schemas)
                {
                    bool added = source.Schemas.TryAdd(key, openApiSchema);
                    int i = 1;
                    while(!added && renameDuplicateSchemas)
                    {
                        i++;
                        var newKey = $"{key}{i}";
                        var newOpenApiSchema = openApiSchema as OpenApiSchema;
                        newOpenApiSchema.Id = newKey;
                        added = source.Schemas.TryAdd(newKey, newOpenApiSchema);
                    }
                }
            }

            if(components.SecuritySchemes != null)
            {
                foreach (var data in components.SecuritySchemes)
                {
                    source.SecuritySchemes.TryAdd(data.Key, data.Value);
                }
            }

            if(components.Links != null)
            {
                foreach (var data in components.Links)
                {
                    source.Links.TryAdd(data.Key, data.Value);
                }
            }

            if(components.Headers != null)
            {
                foreach (var data in components.Headers)
                {
                    source.Headers.TryAdd(data.Key, data.Value);
                }
            }

            if(components.Responses != null)
            {
                foreach (var data in components.Responses)
                {
                    source.Responses.TryAdd(data.Key, data.Value);
                }
            }

            if(components.RequestBodies != null)
            {
                foreach (var data in components.RequestBodies)
                {
                    source.RequestBodies.TryAdd(data.Key, data.Value);
                }
            }

            if(components.Parameters != null)
            {
                foreach (var data in components.Parameters)
                {
                    source.Parameters.TryAdd(data.Key, data.Value);
                }
            }
        }
    }
}