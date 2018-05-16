﻿using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using Consul;
using Discovery.Contracts;

namespace Discovery.Consul
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

        public void RegisterServices(HttpConfiguration config, Assembly assembly, string boundedContext, Uri boundedContextBaseUri)
        {
            var methodsWithDiscoverableAttribute = assembly.GetTypes()
              .SelectMany(t => t.GetMethods())
              .Where(m => m.GetCustomAttributes(typeof(DiscoverableAttribute), false).Length > 0)
              .Select(m => new { Method = m, Attr = (DiscoverableAttribute)m.GetCustomAttribute(typeof(DiscoverableAttribute), false) })
              .ToArray();

            foreach (var methodWithDiscoverableAttribute in methodsWithDiscoverableAttribute)
            {
                var method = methodWithDiscoverableAttribute.Method;
                var attr = methodWithDiscoverableAttribute.Attr;

                var endpoint = new DiscoverableEndpoint(attr.EndpointName, new Uri(boundedContextBaseUri, GetUrl(config, method)), boundedContext, new DiscoveryVersion(attr.Version, attr.DepricateVersion));
                AppendToConsul(endpoint);
            }
        }

        public void RegisterService(DiscoverableEndpoint endpoint, Uri httpCheckUri = null)
        {
            AppendToConsul(endpoint, httpCheckUri);
        }

        public void RegisterService(string serviceName, Uri httpCheckUri)
        {
            AppendToConsul(serviceName, serviceName, null, DefaultCheck(httpCheckUri));
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

            var id = $"{endpoint.BoundedContext}-{endpoint.Name}-{endpoint.Version.IntroducedAtVersion}";
            var name = $"{endpoint.BoundedContext}-{endpoint.Name}-{endpoint.Version.IntroducedAtVersion}";
            var tags = new[] { bcTag, introducedAtVersionTag, depricatedAtVersionTag, endpointUrlTag, endpointNameTag, timeTag, publicTag };

            AgentServiceCheck check = null;
            if (ReferenceEquals(null, httpCheckUri) == false)
                check = DefaultCheck(httpCheckUri);

            AppendToConsul(id, name, tags, check);

        }

        private bool IsNewOrUpdatedService(string id, string name, string[] tags)
        {
            var response = client.Catalog.Service(id).Result;
            if (ReferenceEquals(null, response) == false && response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                CatalogService[] currentServices = response.Response;
                if (ReferenceEquals(null, currentServices) == false)
                {
                    foreach (var currentService in currentServices)
                    {
                        bool hasIdenticalIdAndName = currentService.ServiceID.Equals(id, StringComparison.OrdinalIgnoreCase) && currentService.ServiceName.Equals(name, StringComparison.OrdinalIgnoreCase);
                        if (hasIdenticalIdAndName == false)
                            continue;

                        bool hasIdenticalTags = true;
                        foreach (var tag in currentService.ServiceTags)
                        {
                            if (tag.StartsWith(ConsulHelper.UpdatedAt)) continue;
                            hasIdenticalTags &= tags.Contains(tag);
                        }

                        if (hasIdenticalTags)
                            return false;
                    }
                }
            }

            return true;
        }

        void AppendToConsul(string id, string name, string[] tags, AgentServiceCheck check = null)
        {
            bool isNewOrUpdatedService = IsNewOrUpdatedService(id, name, tags);
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

        static string GetUrl(HttpConfiguration config, MethodInfo method)
        {
            return config
                   .Services
                   .GetApiExplorer()
                   .ApiDescriptions.Where(x => x.ActionDescriptor.ActionName == method.Name && x.ActionDescriptor.ControllerDescriptor.ControllerType == method.DeclaringType && x.HttpMethod != HttpMethod.Head)
                   .Single().Route.RouteTemplate;
        }
    }
}
