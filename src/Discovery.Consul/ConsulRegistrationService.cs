using System;
using System.Collections.Generic;
using System.Linq;
using Consul;

namespace Elders.Discovery.Consul
{
    public class ConsulRegistrationService
    {
        readonly ConsulClient client;

        readonly string consulNodeIp;

        public ConsulRegistrationService(ConsulClient client)
        {
            if (ReferenceEquals(null, client) == true) throw new ArgumentNullException(nameof(client));
            this.client = client;

            this.consulNodeIp = GetCurrentNodeIp();
            if (string.IsNullOrEmpty(consulNodeIp) == true) throw new ArgumentNullException(nameof(consulNodeIp));
        }

        public void RegisterServices(IEnumerable<DiscoverableEndpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                AppendToConsul(endpoint);
            }
        }

        public void RegisterService(DiscoverableEndpoint endpoint, Uri httpCheckUri = null)
        {
            AppendToConsul(endpoint, httpCheckUri);
        }

        public void RegisterService(string serviceName, Uri httpCheckUri)
        {
            var registration = new AgentServiceRegistration()
            {
                ID = serviceName,
                Name = serviceName,
                Address = consulNodeIp,
                Check = DefaultCheck(httpCheckUri)
            };

            var unRegister = client.Agent.ServiceDeregister(registration.ID).Result;
            var register = client.Agent.ServiceRegister(registration).Result;
        }

        public void UnRegisterServices(string boundedContext)
        {
            var services = client.Agent.Services().Result;

            foreach (var service in services.Response)
            {
                if (service.Value != null && service.Value.Tags != null)
                {
                    var parsed = ConsulHelper.Parse(service.Value.Tags);
                    if (parsed.ContainsKey(ConsulHelper.BoundedContext) == true && parsed[ConsulHelper.BoundedContext] == boundedContext)
                        client.Agent.ServiceDeregister(service.Key);
                }
            }
        }

        AgentServiceCheck DefaultCheck(Uri httpCheckUri)
        {
            return new AgentServiceCheck { Interval = TimeSpan.FromMinutes(5), HTTP = httpCheckUri.ToString(), Timeout = TimeSpan.FromMinutes(1) };
        }

        void AppendToConsul(DiscoverableEndpoint endpoint, Uri httpCheckUri = null)
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

            AppendToConsul(id, name, tags, check);

        }

        private bool IsNewOrUpdatedService(DiscoverableEndpoint newEndpoint)
        {
            var result = client.Catalog.Service(newEndpoint.FullName).Result;

            if (ReferenceEquals(result, null)) return false;
            if (result.StatusCode != System.Net.HttpStatusCode.OK) return false;

            CatalogService[] currentServices = result.Response;
            if (ReferenceEquals(currentServices, null) || currentServices.Length == 0) return true;

            foreach (var currentService in currentServices)
            {
                DiscoverableEndpoint endpointInConsul = currentService.ServiceTags.ConvertConsulTagsToDiscoveryEndpoint();
                if (newEndpoint.Equals(endpointInConsul) == false)
                    return true;
            }

            return false;
        }

        void AppendToConsul(string id, string name, string[] tags, AgentServiceCheck check = null)
        {
            DiscoverableEndpoint newEndpoint = tags.ConvertConsulTagsToDiscoveryEndpoint();
            bool isNewOrUpdatedService = IsNewOrUpdatedService(newEndpoint);
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
                var unRegister = client.Agent.ServiceDeregister(registration.ID).Result;
                var register = client.Agent.ServiceRegister(registration).Result;
                //var result = client.Catalog.Services().Result;
                //foreach (var item in result.Response)
                //{
                //    client.Agent.ServiceDeregister(item.Key);
                //}
            }
        }

        string GetCurrentNodeIp()
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
