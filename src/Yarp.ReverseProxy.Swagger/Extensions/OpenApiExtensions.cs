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

        private static void MergeProperties(OpenApiSchema mainSchema, OpenApiSchema addSchema)
        {
            IDictionary<string, OpenApiSchema> allMainProperties = GetAllProperties(mainSchema);
            IDictionary<string, OpenApiSchema> allAddProperties = GetAllProperties(addSchema);

            mainSchema = FixProperties(mainSchema, allMainProperties);

            foreach (var property in allMainProperties)
            {
                if (!allAddProperties.ContainsKey(property.Key))
                {
                    var mainProperty = GetPropertyByName(mainSchema, property.Key);
                    mainProperty.Nullable = true;
                    mainSchema.Properties[property.Key] = mainProperty;
                }
            }

            foreach (var property in allAddProperties)
            {
                if (allMainProperties.ContainsKey(property.Key))
                {
                    if (property.Value.Nullable)
                    {
                        var mainProperty = GetPropertyByName(mainSchema, property.Key);
                        mainProperty.Nullable = true;
                    }
                }
                else
                {
                    mainSchema.Properties.Add(property.Key, property.Value);
                }
            }
        }

        private static OpenApiSchema FixProperties(OpenApiSchema schema, IDictionary<string, OpenApiSchema> properties)
        {
            schema.AllOf.Clear();
            schema.OneOf.Clear();
            schema.Properties.Clear();

            foreach (var property in properties)
            {
                KeyValuePair<string, OpenApiSchema> newProperty = new KeyValuePair<string, OpenApiSchema>(property.Key, new OpenApiSchema(property.Value));
                schema.Properties.Add(newProperty);

            }
            return schema;
        }

        private static OpenApiSchema? GetPropertyByName(OpenApiSchema schema, string name)
        {
            if (schema.Properties != null && schema.Properties.ContainsKey(name))
            {
                return schema.Properties[name];
            }

            return null;
        }

        private static IDictionary<string, OpenApiSchema> GetAllProperties(OpenApiSchema schema, Dictionary<string, OpenApiSchema> properties = null)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, OpenApiSchema>();
            }

            if (schema.Properties != null)
            {
                foreach (var property in schema.Properties)
                {
                    properties[property.Key] = property.Value;
                }
            }

            if (schema.AllOf != null)
            {
                foreach (var subschema in schema.AllOf)
                {
                    GetAllProperties(subschema, properties);
                }
            }

            if (schema.OneOf != null)
            {
                foreach (var subschema in schema.OneOf)
                {
                    GetAllProperties(subschema, properties);
                }
            }

            return properties;
        }
    }
}
