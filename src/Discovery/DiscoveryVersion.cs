using System;
using System.Collections.Generic;

namespace Elders.Discovery
{
    public class DiscoveryVersion : IEqualityComparer<DiscoveryVersion>, IEquatable<DiscoveryVersion>
    {
        DiscoveryVersion() { }

        public DiscoveryVersion(string introducedAtVersion, string depricatedAtVersion)
        {
            if (string.IsNullOrEmpty(introducedAtVersion) == true) throw new ArgumentNullException(nameof(introducedAtVersion));
            if (depricatedAtVersion == null) depricatedAtVersion = string.Empty;

            IntroducedAtVersion = introducedAtVersion;
            DepricatedAtVersion = depricatedAtVersion;
        }

        public DiscoveryVersion(string introducedAtVersion) : this(introducedAtVersion, string.Empty) { }

        public string IntroducedAtVersion { get; private set; }

        public string DepricatedAtVersion { get; private set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as DiscoveryVersion);
        }

        public bool Equals(DiscoveryVersion left, DiscoveryVersion right)
        {
            if (ReferenceEquals(null, left) && ReferenceEquals(null, right)) return true;
            if (ReferenceEquals(null, left))
                return false;
            else
                return left.Equals(right);
        }

        public virtual bool Equals(DiscoveryVersion other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            var t = GetType();
            if (t != other.GetType())
                return false;

            return
                IntroducedAtVersion.Equals(other.IntroducedAtVersion, StringComparison.OrdinalIgnoreCase) &&
                DepricatedAtVersion.Equals(other.DepricatedAtVersion, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(DiscoveryVersion obj)
        {
            return obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 99689;
                int multiplier = 102437;

                hashCode = (hashCode * multiplier) ^ IntroducedAtVersion.GetHashCode();
                hashCode = (hashCode * multiplier) ^ (string.IsNullOrEmpty(DepricatedAtVersion) ? 0 : DepricatedAtVersion.GetHashCode());

                return hashCode;
            }
        }
    }
}
