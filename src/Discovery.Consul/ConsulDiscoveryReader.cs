using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;

namespace Elders.Discovery.Consul
{
    public class ConsulDiscoveryReader : IDiscoveryReader
    {
        readonly ConsulClient client;

        public ConsulDiscoveryReader(ConsulClient client)
        {
            if (ReferenceEquals(null, client) == true) throw new ArgumentNullException(nameof(client));
            this.client = client;
        }

        public DiscoveryResponse Get()
        {
            return Get(string.Empty);
        }

        public Task<DiscoveryResponse> GetAsync()
        {
            return GetAsync(string.Empty);
        }

        public DiscoveryResponse Get(string boundedContext)
        {
            return GetAsync(boundedContext).Result;
        }

        public async Task<DiscoveryResponse> GetAsync(string boundedContext)
        {
            long globalUpdatedAt = 0;
            var foundEndpoints = new HashSet<DiscoverableEndpoint>();

            var result = await client.Catalog.Services().ConfigureAwait(false);
            if (ReferenceEquals(null, result) || result.StatusCode != System.Net.HttpStatusCode.OK)
                return new DiscoveryResponse(globalUpdatedAt, foundEndpoints);

            var publicServices = result.Response.Where(x => x.Value.Any(y => ConsulHelper.IsPublic(y)));

            foreach (var publicService in publicServices)
            {
                var parsedTags = ConsulHelper.Parse(publicService.Value);
                if (parsedTags.ContainsDiscoverableEndpointTags() == false) continue;
                if (parsedTags.TagsArePartOfBoundedContext(boundedContext) == false) continue;

                DiscoverableEndpoint consulEndpoint = publicService.Value.ConvertConsulTagsToDiscoveryEndpoint();
                foundEndpoints.Add(consulEndpoint);
                long serviceUpdatedAt = await GetServiceUpdatedAtAsync(publicService.Key).ConfigureAwait(false);
                if (serviceUpdatedAt > globalUpdatedAt)
                    globalUpdatedAt = serviceUpdatedAt;
            }

            return new DiscoveryResponse(globalUpdatedAt, foundEndpoints);
        }

        private async Task<long> GetServiceUpdatedAtAsync(string serviceId)
        {
            var endpointsFromAllNodes = new HashSet<DiscoverableEndpoint>();

            long updatedAt = 0;
            var result = await client.Catalog.Service(serviceId).ConfigureAwait(false);
            if (ReferenceEquals(null, result) == false && result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                CatalogService[] currentServices = result.Response;
                if (ReferenceEquals(null, currentServices) == false)
                {
                    foreach (var currentService in currentServices)
                    {
                        DiscoverableEndpoint endpoint = currentService.ServiceTags.ConvertConsulTagsToDiscoveryEndpoint();
                        long currentUpdatedAt = currentService.ServiceTags.GetUpdatedAtTimestamp();

                        // We may have exactly the same service registered on different nodes at a different time which depends on deployment times
                        // In this case we get the most ancient possible timestamp
                        if (endpointsFromAllNodes.Any(ep => ep.Equals(endpoint)))
                        {
                            if (currentUpdatedAt < updatedAt)
                                updatedAt = currentUpdatedAt;
                        }
                        else
                        {
                            if (currentUpdatedAt > updatedAt)
                                updatedAt = currentUpdatedAt;
                        }

                        endpointsFromAllNodes.Add(endpoint);
                    }
                }
            }

            return updatedAt;
        }
    }
}
