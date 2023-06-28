using System.Collections.Generic;
using System.Linq;

namespace Yarp.ReverseProxy.Swagger
{
    public sealed class ReverseProxyDocumentFilterConfig
    {
        public IReadOnlyDictionary<string, Cluster> Clusters { get; init; } = new Dictionary<string, Cluster>();

        public sealed class Cluster
        {
            public IReadOnlyDictionary<string, Destination> Destinations { get; init; } = new Dictionary<string,Destination>();

            public sealed class Destination
            {
                public string AccessTokenClientName { get; init; }
                public string Address { get; init; }
                public IReadOnlyList<Swagger> Swaggers { get; init; } = new List<Swagger>();

                public sealed class Swagger
                {
                    public string PrefixPath { get; init; }
                    public IReadOnlyList<string> Paths { get; init; } = new List<string>();
                }
            }
        }

        public static ReverseProxyDocumentFilterConfig Empty => new ReverseProxyDocumentFilterConfig();
        public bool IsEmpty => Clusters?.Any() != true;
    }
}
