using System.Threading.Tasks;

namespace Elders.Discovery
{
    public interface IDiscoveryReader
    {
        Task<DiscoveryResponse> GetAsync();
        Task<DiscoveryResponse> GetAsync(string boundedContext);
    }
}
