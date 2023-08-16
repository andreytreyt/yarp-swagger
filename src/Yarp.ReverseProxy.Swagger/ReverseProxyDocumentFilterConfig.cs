using System.Collections.Generic;
using System.Linq;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.ReverseProxy.Swagger
{
    public sealed class ReverseProxyDocumentFilterConfig
    {
        public IReadOnlyDictionary<string, RouteConfig> Routes {get; init; } 
        public IReadOnlyDictionary<string, Cluster> Clusters { get; init; }

        public sealed class Cluster
        {
            public IReadOnlyDictionary<string, Destination> Destinations { get; init; }

            public sealed class Destination
            {
                public string AccessTokenClientName { get; init; }
                public string Address { get; init; }
                public IReadOnlyList<Swagger> Swaggers { get; init; }

                public sealed class Swagger
                {
                    public string PrefixPath { get; init; }
                    public string PathFilterRegexPattern { get; init; }
                    public IReadOnlyList<string> Paths { get; init; }

                    public bool AddOnlyPublishedPaths { get; set; } = false;
                }
            }
        }

        public bool IsEmpty => Clusters?.Any() != true;
    }
}