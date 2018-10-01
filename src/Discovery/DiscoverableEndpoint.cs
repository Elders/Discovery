using System;
using System.Collections.Generic;

namespace Elders.Discovery
{
    public class DiscoverableEndpoint : IEqualityComparer<DiscoverableEndpoint>, IEquatable<DiscoverableEndpoint>
    {
        DiscoverableEndpoint() { }

        public DiscoverableEndpoint(string name, Uri uri, string boundedContext, DiscoveryVersion version)
        {
            if (string.IsNullOrEmpty(name) == true) throw new ArgumentNullException(nameof(name));
            if (ReferenceEquals(null, uri) == true) throw new ArgumentNullException(nameof(uri));
            if (string.IsNullOrEmpty(boundedContext) == true) throw new ArgumentNullException(nameof(boundedContext));
            if (ReferenceEquals(null, version) == true) throw new ArgumentNullException(nameof(version));

            Name = name;
            Url = uri;
            BoundedContext = boundedContext;
            Version = version;
            FullName = $"{BoundedContext}-{Name}-{Version.IntroducedAtVersion}";
        }

        public string Name { get; private set; }

        public Uri Url { get; private set; }

        public string BoundedContext { get; private set; }

        public DiscoveryVersion Version { get; private set; }

        public string FullName { get; private set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as DiscoverableEndpoint);
        }

        public bool Equals(DiscoverableEndpoint left, DiscoverableEndpoint right)
        {
            if (ReferenceEquals(null, left) && ReferenceEquals(null, right)) return true;
            if (ReferenceEquals(null, left))
                return false;
            else
                return left.Equals(right);
        }

        public virtual bool Equals(DiscoverableEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var t = GetType();
            if (t != other.GetType())
                return false;

            return
                Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                BoundedContext.Equals(other.BoundedContext, StringComparison.OrdinalIgnoreCase) &&
                Url == other.Url &&
                Version.Equals(other.Version);
        }

        public int GetHashCode(DiscoverableEndpoint obj)
        {
            return obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 3301;
                int multiplier = 79043;

                hashCode = (hashCode * multiplier) ^ Name.GetHashCode();
                hashCode = (hashCode * multiplier) ^ BoundedContext.GetHashCode();
                hashCode = (hashCode * multiplier) ^ Url.GetHashCode();
                hashCode = (hashCode * multiplier) ^ Version.GetHashCode();

                return hashCode;
            }
        }
    }
}
