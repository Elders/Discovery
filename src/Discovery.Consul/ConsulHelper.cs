using System;
using System.Collections.Generic;

namespace Elders.Discovery.Consul
{
    public static class ConsulHelper
    {
        public const string DefaultConsulUriString = "http://consul.local.com:8500";
        public const string Visability = "Visability";
        public const string BoundedContext = "BoundedContext";
        public const string UpdatedAt = "UpdatedAt";
        public const string IntroducedAtVersion = "IntroducedAtVersion";
        public const string DepricatedAtVersion = "DepricatedAtVersion";
        public const string EndpointName = "EndpointName";
        public const string EndpointUrl = "EndpointUrl";
        public const string Separator = "__";

        public static Uri DefaultConsulUri { get { return new Uri(DefaultConsulUriString); } }

        public static bool IsPublic(string tag)
        {
            if (string.IsNullOrEmpty(tag) == true) return false;

            var splitted = tag.Split(new string[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (splitted.Length < 2) return false;
            if (splitted[0] != Visability) return false;
            if (splitted[1] == "public") return true;

            return false;
        }

        public static IDictionary<string, string> Parse(string[] tags)
        {
            var parsed = new Dictionary<string, string>();
            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag) == true) continue;

                var splitted = tag.Split(new string[] { ConsulHelper.Separator }, StringSplitOptions.RemoveEmptyEntries);
                if (splitted.Length < 2) continue;

                if (parsed.ContainsKey(splitted[0]) == false)
                    parsed.Add(splitted[0], splitted[1]);
            }

            return parsed;
        }
    }
}
