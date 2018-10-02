using System.Collections.Generic;

namespace Elders.Discovery
{
    public interface IEndpointDiscovery
    {
        IEnumerable<DiscoverableEndpoint> Discover();
    }
}
