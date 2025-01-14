using System.Collections.Generic;
using Microsoft.OpenApi.Models;

namespace Yarp.ReverseProxy.Swagger.Extensions
{
    public static class OpenApiExtensions
    {
        internal static void Add(this OpenApiComponents source, OpenApiComponents components, string duplicateSchemas)
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

                switch (duplicateSchemas)
                {
                    case "Rename":
                        int i = 1;
                        while(!added)
                        {
                            i++;
                            var key = $"{data.Key}{i}";
                            data.Value.Reference.Id = key;
                            added = source.Schemas.TryAdd(key, data.Value);
                        }
                        break;
                    case "Combine":
                        if (!added)
                        {
                            var existing = source.Schemas[data.Key];
                            MergeProperties(source, existing, data.Value, true);
                        }
                        break;
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

        private static void MergeProperties(OpenApiComponents source, OpenApiSchema mainSchema, OpenApiSchema addSchema, bool baseCall)
        {
            foreach (var property in addSchema.Properties)
            {
                var added = mainSchema.Properties.TryAdd(property.Key, property.Value);
                if (!added)
                {
                    if(!string.IsNullOrWhiteSpace(property.Value.Reference?.ReferenceV3))
                    {
                        var sourceReference = source.Schemas[property.Key];
                        MergeProperties(source, sourceReference, property.Value, false);
                    }
                    else if (mainSchema.Properties[property.Key].Type != property.Value.Type)
                    {
                        throw new System.Exception("Cannot merge properties with the same name but different types");
                    }

                    if (property.Value.Nullable)
                    {
                        mainSchema.Properties[property.Key].Nullable = true;
                    }
                }
                else
                {
                    property.Value.Nullable = true;
                    mainSchema.Properties[property.Key] = property.Value;
                }
            }

            if (addSchema.AllOf != null)
            {
                foreach (var allOfSchema in addSchema.AllOf)
                {
                    MergeProperties(source, mainSchema, allOfSchema, false);
                }
            }

            if (addSchema.OneOf != null)
            {
                foreach (var oneOfSchema in addSchema.OneOf)
                {
                    MergeProperties(source, mainSchema, oneOfSchema, false);
                }
            }

            if (baseCall)
            {
                foreach (var propertyKey in mainSchema.Properties.Keys)
                {
                    if (addSchema.Properties.Keys.Contains(propertyKey))
                    {
                        return;
                    }

                    foreach (var allOfSchema in addSchema.AllOf)
                    {
                        if (allOfSchema.Properties.Keys.Contains(propertyKey))
                        {
                            return;
                        }
                    }

                    foreach (var oneOfSchema in addSchema.OneOf)
                    {
                        if (oneOfSchema.Properties.Keys.Contains(propertyKey))
                        {
                            return;
                        }
                    }

                    mainSchema.Properties[propertyKey].Nullable = true;
                }
            }
        }
    }
}