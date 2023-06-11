using System.Collections.Generic;
using System.Linq;

namespace Yarp.ReverseProxy.Swagger;

public sealed class ReverseProxyDocumentFilterConfig
{
    public IReadOnlyDictionary<string, Cluster> Clusters { get; init; }
    
    public class Cluster
    {
        public IReadOnlyDictionary<string, Destination> Destinations { get; init; }
        
        public class Destination
        {
            public string Address { get; init; }
            public IReadOnlyList<Swagger> Swaggers { get; init; }
            
            public class Swagger
            {
                public string PrefixPath { get; init; }
                public IReadOnlyList<string> Paths { get; init; }
            }
        }
    }
    
    public static ReverseProxyDocumentFilterConfig Empty => new();
    public bool IsEmpty => Clusters?.Any() != true;
}