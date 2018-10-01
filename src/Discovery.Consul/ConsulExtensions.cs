using System;
using System.Collections.Generic;

namespace Elders.Discovery.Consul
{
    public static class ConsulExtensions
    {
        public static bool ContainsDiscoverableEndpointTags(this IDictionary<string, string> parsedTags)
        {
            if (parsedTags.ContainsKey(ConsulHelper.BoundedContext) == false) return false;
            if (parsedTags.ContainsKey(ConsulHelper.UpdatedAt) == false) return false;
            if (parsedTags.ContainsKey(ConsulHelper.IntroducedAtVersion) == false) return false;
            if (parsedTags.ContainsKey(ConsulHelper.EndpointName) == false) return false;
            if (parsedTags.ContainsKey(ConsulHelper.EndpointUrl) == false) return false;

            return true;
        }

        public static DiscoverableEndpoint ConvertConsulTagsToDiscoveryEndpoint(this string[] tags)
        {
            var parsed = ConsulHelper.Parse(tags);

            if (parsed.ContainsKey(ConsulHelper.BoundedContext) == false) return null;
            if (parsed.ContainsKey(ConsulHelper.UpdatedAt) == false) return null;
            if (parsed.ContainsKey(ConsulHelper.IntroducedAtVersion) == false) return null;
            if (parsed.ContainsKey(ConsulHelper.EndpointName) == false) return null;
            if (parsed.ContainsKey(ConsulHelper.EndpointUrl) == false) return null;

            string name = parsed[ConsulHelper.EndpointName];
            string boundedContext = parsed[ConsulHelper.BoundedContext];
            string endpointUrl = parsed[ConsulHelper.EndpointUrl];
            string introducedAtVersion = parsed[ConsulHelper.IntroducedAtVersion];
            var depricatedAtVersion = string.Empty;
            parsed.TryGetValue(ConsulHelper.DepricatedAtVersion, out depricatedAtVersion);

            return new DiscoverableEndpoint(name, new Uri(endpointUrl), boundedContext, new DiscoveryVersion(introducedAtVersion, depricatedAtVersion));
        }

        public static long GetUpdatedAtTimestamp(this IDictionary<string, string> parsedTags)
        {
            if (long.TryParse(parsedTags[ConsulHelper.UpdatedAt], out long timestamp))
            {
                return timestamp;
            }

            return 0;
        }

        public static long GetUpdatedAtTimestamp(this string[] tags)
        {
            var parsed = ConsulHelper.Parse(tags);
            return GetUpdatedAtTimestamp(parsed);
        }

        public static bool TagsArePartOfBoundedContext(this IDictionary<string, string> parsedTags, string boundedContext = null)
        {
            if (string.IsNullOrEmpty(boundedContext))
                return true;

            return parsedTags[ConsulHelper.BoundedContext].Equals(boundedContext, StringComparison.OrdinalIgnoreCase);
        }
    }
}
