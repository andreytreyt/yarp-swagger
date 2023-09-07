using System.Collections.Generic;
using System.Linq;
using Yarp.ReverseProxy.Configuration;

namespace Yarp.ReverseProxy.Swagger
{
    public sealed class ReverseProxyDocumentFilterConfig
    {
        public IReadOnlyDictionary<string, RouteConfig> Routes {get; set; } 
        public IReadOnlyDictionary<string, Cluster> Clusters { get; set; }

        public sealed class Cluster
        {
            public IReadOnlyDictionary<string, Destination> Destinations { get; set; }

            public sealed class Destination
            {
                public string AccessTokenClientName { get; set; }
                public string Address { get; set; }
                public IReadOnlyList<Swagger> Swaggers { get; set; }

                public sealed class Swagger
                {
                    public string PrefixPath { get; set; }
                    public string PathFilterRegexPattern { get; set; }
                    public IReadOnlyList<string> Paths { get; set; }
                    public bool AddOnlyPublishedPaths { get; set; } = false;
                }
            }
        }

        public bool IsEmpty => Clusters?.Any() != true;
    }
}