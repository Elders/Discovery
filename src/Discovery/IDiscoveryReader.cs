namespace Elders.Discovery
{
    public interface IDiscoveryReader
    {
        DiscoveryReaderResponseModel Get();

        DiscoveryReaderResponseModel Get(string boundedContext);
    }
}
