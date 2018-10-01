using System.Threading.Tasks;

namespace Elders.Discovery
{
    public interface IDiscoveryReader
    {
        DiscoveryResponse Get();
        DiscoveryResponse Get(string boundedContext);

        Task<DiscoveryResponse> GetAsync();
        Task<DiscoveryResponse> GetAsync(string boundedContext);
    }
}
