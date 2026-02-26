using Microsoft.OpenApi;
using System.Collections.Generic;

namespace Yarp.ReverseProxy.Swagger
{
    public interface ISwaggerTransformFactory
    {
        bool Build(OpenApiOperation operation, IReadOnlyDictionary<string, string> transformValues);
    }
}
