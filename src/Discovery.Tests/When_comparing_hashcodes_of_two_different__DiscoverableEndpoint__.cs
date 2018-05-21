﻿using System;
using Discovery.Contracts;
using Machine.Specifications;

namespace Discovery.Tests
{
    public class When_comparing_hashcodes_of_two_different__DiscoverableEndpoint__
    {
        Establish context = () =>
        {
            first = new DiscoverableEndpoint("endpoint", new Uri("https://eldersoss.com"), "elders", new DiscoveryVersion("v1"));
            second = new DiscoverableEndpoint("endpoint", new Uri("https://eldersoss.com"), "elders", new DiscoveryVersion("v2"));
        };

        Because of = () => result = first.GetHashCode() == second.GetHashCode();

        It should_return_true = () => result.ShouldBeFalse();

        static DiscoverableEndpoint first;
        static DiscoverableEndpoint second;
        static bool result;
    }
}
