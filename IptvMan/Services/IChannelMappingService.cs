using IptvMan.Models;

namespace IptvMan.Services;

public interface IChannelMappingService
{
    IEnumerable<ChannelMapping> GetMappings(string accountId);
    ChannelMapping? GetMapping(string id);
    void AddMapping(ChannelMapping mapping);
    bool UpdateMapping(ChannelMapping mapping);
    bool DeleteMapping(string id);
    void DeleteAllMappings(string accountId);
    ChannelMapping? GetMappingByStreamId(string accountId, string streamId);
}
