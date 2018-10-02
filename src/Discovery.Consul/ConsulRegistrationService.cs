using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;

namespace Elders.Discovery.Consul
{
    public class ConsulRegistrationService
    {
        private readonly ConsulClient client;
        private readonly IEndpointDiscovery discovery;
        private readonly string consulNodeIp;

        public ConsulRegistrationService(ConsulClient client, IEndpointDiscovery discovery)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            this.client = client;

            this.consulNodeIp = GetCurrentNodeIp();
            if (string.IsNullOrEmpty(consulNodeIp)) throw new ArgumentNullException(nameof(consulNodeIp));

            if (discovery is null) throw new ArgumentNullException(nameof(discovery));
            this.discovery = discovery;
        }

        [Obsolete]
        public ConsulRegistrationService(ConsulClient client)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            this.client = client;

            this.consulNodeIp = GetCurrentNodeIp();
            if (string.IsNullOrEmpty(consulNodeIp)) throw new ArgumentNullException(nameof(consulNodeIp));
        }

        public async Task RegisterDiscoveredEndpointsAsync()
        {
            foreach (var endpoint in discovery.Discover())
            {
                await AppendToConsulAsync(endpoint).ConfigureAwait(false);
            }
        }

        [Obsolete]
        public async Task RegisterServicesAsync(IEnumerable<DiscoverableEndpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                await AppendToConsulAsync(endpoint).ConfigureAwait(false);
            }
        }

        [Obsolete]
        public Task RegisterServiceAsync(DiscoverableEndpoint endpoint, Uri httpCheckUri = null)
        {
            return AppendToConsulAsync(endpoint, httpCheckUri);
        }

        [Obsolete]
        public async Task RegisterServiceAsync(string serviceName, Uri httpCheckUri)
        {
            var registration = new AgentServiceRegistration()
            {
                ID = serviceName,
                Name = serviceName,
                Address = consulNodeIp,
                Check = DefaultCheck(httpCheckUri)
            };

            var unRegister = await client.Agent.ServiceDeregister(registration.ID).ConfigureAwait(false);
            var register = await client.Agent.ServiceRegister(registration).ConfigureAwait(false);
        }

        public async Task UnRegisterServicesAsync(string boundedContext)
        {
            var services = await client.Agent.Services().ConfigureAwait(false);

            foreach (var service in services.Response)
            {
                if (service.Value != null && service.Value.Tags != null)
                {
                    var parsed = ConsulHelper.Parse(service.Value.Tags);
                    if (parsed.ContainsKey(ConsulHelper.BoundedContext) == true && parsed[ConsulHelper.BoundedContext] == boundedContext)
                        await client.Agent.ServiceDeregister(service.Key).ConfigureAwait(false);
                }
            }
        }

        private AgentServiceCheck DefaultCheck(Uri httpCheckUri)
        {
            return new AgentServiceCheck { Interval = TimeSpan.FromMinutes(5), HTTP = httpCheckUri.ToString(), Timeout = TimeSpan.FromMinutes(1) };
        }

        private Task AppendToConsulAsync(DiscoverableEndpoint endpoint, Uri httpCheckUri = null)
        {
            var bcTag = $"{ConsulHelper.BoundedContext}{ConsulHelper.Separator}{endpoint.BoundedContext}";
            var publicTag = $"{ConsulHelper.Visability}{ConsulHelper.Separator}public";
            var timeTag = $"{ConsulHelper.UpdatedAt}{ConsulHelper.Separator}{DateTime.UtcNow.ToFileTimeUtc()}";

            var introducedAtVersionTag = $"{ConsulHelper.IntroducedAtVersion}{ConsulHelper.Separator}{endpoint.Version.IntroducedAtVersion}";
            var depricatedAtVersionTag = $"{ConsulHelper.DepricatedAtVersion}{ConsulHelper.Separator}{endpoint.Version.DepricatedAtVersion}";
            var endpointNameTag = $"{ConsulHelper.EndpointName}{ConsulHelper.Separator}{endpoint.Name}";
            var endpointUrlTag = $"{ConsulHelper.EndpointUrl}{ConsulHelper.Separator}{endpoint.Url}";

            var id = endpoint.FullName;
            var name = endpoint.FullName;
            var tags = new[] { bcTag, introducedAtVersionTag, depricatedAtVersionTag, endpointUrlTag, endpointNameTag, timeTag, publicTag };

            AgentServiceCheck check = null;
            if (ReferenceEquals(null, httpCheckUri) == false)
                check = DefaultCheck(httpCheckUri);

            return AppendToConsulAsync(id, name, tags, check);

        }

        private async Task<bool> IsNewOrUpdatedServiceAsync(DiscoverableEndpoint newEndpoint)
        {
            var result = await client.Catalog.Service(newEndpoint.FullName).ConfigureAwait(false);

            if (result is null) return false;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) return false;

            CatalogService[] currentServices = result.Response;
            if (currentServices is null || currentServices.Length == 0) return true;

            foreach (var currentService in currentServices)
            {
                DiscoverableEndpoint endpointInConsul = currentService.ServiceTags.ConvertConsulTagsToDiscoveryEndpoint();
                if (newEndpoint.Equals(endpointInConsul) == false)
                    return true;
            }

            return false;
        }

        private async Task AppendToConsulAsync(string id, string name, string[] tags, AgentServiceCheck check = null)
        {
            DiscoverableEndpoint newEndpoint = tags.ConvertConsulTagsToDiscoveryEndpoint();
            bool isNewOrUpdatedService = await IsNewOrUpdatedServiceAsync(newEndpoint).ConfigureAwait(false);
            if (isNewOrUpdatedService)
            {
                check = null; // Removes all health checks for now... too much noise
                var registration = new AgentServiceRegistration()
                {
                    ID = id,
                    Name = name,
                    Address = consulNodeIp,
                    Tags = tags,
                    Check = check
                };

                // this will clean old registrations
                var unRegister = await client.Agent.ServiceDeregister(registration.ID).ConfigureAwait(false);
                var register = await client.Agent.ServiceRegister(registration).ConfigureAwait(false);
            }
        }

        private string GetCurrentNodeIp()
        {
            var self = client.Agent.Self().Result;
            if (ReferenceEquals(null, self) == true) return string.Empty;

            var consulCfg = self.Response.Where(x => x.Key == "Config").FirstOrDefault();
            if (ReferenceEquals(null, consulCfg) == true) return string.Empty;

            var clientAddrCfg = consulCfg.Value.Where(x => x.Key == "ClientAddr").FirstOrDefault();
            if (ReferenceEquals(null, clientAddrCfg) == true) return string.Empty;

            var ip = clientAddrCfg.Value;
            return ip;
        }
    }
}
