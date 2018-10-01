using System;
using System.Collections.Generic;
using System.Linq;
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

        public DiscoveryReaderResponseModel Get()
        {
            return Get(string.Empty);
        }

        public DiscoveryReaderResponseModel Get(string boundedContext)
        {
            long globalUpdatedAt = 0;
            var foundEndpoints = new HashSet<DiscoverableEndpoint>();

            var result = client.Catalog.Services().Result;
            if (ReferenceEquals(null, result) == true || result.StatusCode != System.Net.HttpStatusCode.OK)
                return new DiscoveryReaderResponseModel(globalUpdatedAt, foundEndpoints);

            var publicServices = result.Response.Where(x => x.Value.Any(y => ConsulHelper.IsPublic(y) == true));

            foreach (var publicService in publicServices)
            {
                var parsedTags = ConsulHelper.Parse(publicService.Value);
                if (parsedTags.ContainsDiscoverableEndpointTags() == false) continue;
                if (parsedTags.TagsArePartOfBoundedContext(boundedContext) == false) continue;

                DiscoverableEndpoint consulEndpoint = publicService.Value.ConvertConsulTagsToDiscoveryEndpoint();
                foundEndpoints.Add(consulEndpoint);
                long serviceUpdatedAt = GetServiceUpdatedAt(publicService.Key);
                if (serviceUpdatedAt > globalUpdatedAt)
                    globalUpdatedAt = serviceUpdatedAt;
            }

            return new DiscoveryReaderResponseModel(globalUpdatedAt, foundEndpoints);
        }

        private long GetServiceUpdatedAt(string serviceId)
        {
            var endpointsFromAllNodes = new HashSet<DiscoverableEndpoint>();

            long updatedAt = 0;
            var consulServiceResponse = client.Catalog.Service(serviceId).Result;
            if (ReferenceEquals(null, consulServiceResponse) == false && consulServiceResponse.StatusCode == System.Net.HttpStatusCode.OK)
            {
                CatalogService[] currentServices = consulServiceResponse.Response;
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
