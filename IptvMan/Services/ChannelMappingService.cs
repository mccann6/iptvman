using IptvMan.Models;
using LiteDB;

namespace IptvMan.Services;

public class ChannelMappingService : IChannelMappingService
{
    private readonly ILiteCollection<ChannelMapping> _mappings;

    public ChannelMappingService(ILiteDatabase db)
    {
        _mappings = db.GetCollection<ChannelMapping>("channel_mappings");
        _mappings.EnsureIndex(x => x.AccountId);
        _mappings.EnsureIndex(x => x.OriginalStreamId);
    }

    public IEnumerable<ChannelMapping> GetMappings(string accountId)
    {
        return _mappings.Find(x => x.AccountId == accountId).OrderBy(x => x.SortOrder);
    }

    public ChannelMapping? GetMapping(string id)
    {
        return _mappings.FindById(id);
    }

    public void AddMapping(ChannelMapping mapping)
    {
        _mappings.Insert(mapping);
    }

    public bool UpdateMapping(ChannelMapping mapping)
    {
        return _mappings.Update(mapping);
    }

    public bool DeleteMapping(string id)
    {
        return _mappings.Delete(id);
    }

    public void DeleteAllMappings(string accountId)
    {
        _mappings.DeleteMany(x => x.AccountId == accountId);
    }

    public ChannelMapping? GetMappingByStreamId(string accountId, string streamId)
    {
        return _mappings.FindOne(x => x.AccountId == accountId && x.OriginalStreamId == streamId);
    }
}
