using Microsoft.OpenApi;
using Yarp.ReverseProxy.Swagger;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Yarp.Transformations;

public class HeaderTransformFactory : ITransformFactory, ISwaggerTransformFactory
{
    /// <summary>
    /// Property validates for header titel rename
    /// </summary>
    /// <param name="context"></param>
    /// <param name="transformValues"></param>
    /// <returns></returns>
    public bool Validate(TransformRouteValidationContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue("RenameHeader", out var header))
        {
            if (string.IsNullOrEmpty(header))
            {
                context.Errors.Add(new ArgumentException("A non-empty RenameHeader value is required"));
            }

            if (transformValues.TryGetValue("Set", out var newHeader))
            {
                if (string.IsNullOrEmpty(newHeader))
                {
                    context.Errors.Add(new ArgumentException("A non-empty Set value is required"));
                }
            }
            else
            {
                context.Errors.Add(new ArgumentException("Set option is required"));
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Header title rename transformation
    /// </summary>
    /// <param name="context"></param>
    /// <param name="transformValues"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue("RenameHeader", out var header))
        {
            if (string.IsNullOrEmpty(header))
            {
                throw new ArgumentException("A non-empty RenameHeader value is required");
            }

            if (transformValues.TryGetValue("Set", out var newHeader))
            {
                if (string.IsNullOrEmpty(newHeader))
                {
                    throw new ArgumentException("A non-empty Set value is required");
                }
            }
            else
            {
                throw new ArgumentException("Set option is required");
            }

            context.AddRequestTransform(transformContext =>
            {
                if (transformContext.ProxyRequest.Headers.TryGetValues(header, out var headerValue))
                {
                    // Remove the original header
                    transformContext.ProxyRequest.Headers.Remove(header);

                    // Add a new header with the same value(s) as the original header
                    transformContext.ProxyRequest.Headers.Add(newHeader, headerValue);
                }

                return default;
            });

            return true;
        }

        return false;
    }

    /// <summary>
    /// Header title rename transformation for Swagger
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="transformValues"></param>
    /// <returns></returns>
    public bool Build(OpenApiOperation operation, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.ContainsKey("RenameHeader") &&
            operation.Parameters?.Any() == true)
        {
            for (var i = 0; i < operation.Parameters.Count; i++)
            {
                var parameter = operation.Parameters[i];
                if (parameter.In.HasValue && parameter.In.Value.ToString().Equals("Header"))
                {
                    if (transformValues.TryGetValue("RenameHeader", out var header)
                        && transformValues.TryGetValue("Set", out var newHeader))
                    {
                        if (parameter.Name == newHeader)
                        {
                            // Replace parameter with a copy with the new name as Name is read only
                            var copy = parameter.CreateShallowCopy();
                            var nameProperty = copy.GetType().GetProperty("Name");
                            if (nameProperty != null && nameProperty.CanWrite)
                            {
                                nameProperty.SetValue(copy, header);
                                operation.Parameters[i] = copy;
                            }
                        }
                    }
                }
            }

            return true;
        }

        return false;
    }
}
