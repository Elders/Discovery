using System.Collections.Generic;

namespace Elders.Discovery
{
    public class DiscoveryResponse
    {
        DiscoveryResponse()
        {
            Endpoints = new List<DiscoverableEndpoint>();
        }

        public DiscoveryResponse(long updatedAt, IEnumerable<DiscoverableEndpoint> endpoints)
        {
            UpdatedAt = updatedAt;
            Endpoints = new HashSet<DiscoverableEndpoint>(endpoints);
        }

        /// <summary>
        /// FileTimeUtc
        /// </summary>
        public long UpdatedAt { get; private set; }

        public IEnumerable<DiscoverableEndpoint> Endpoints { get; private set; }
    }
}
