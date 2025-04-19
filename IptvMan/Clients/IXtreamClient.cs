using IptvMan.Models;

namespace IptvMan.Clients;

public interface IXtreamClient
{
    Task<List<LiveStream>> GetLiveStreams(string host, string username, string password, string? categoryId = null);
    Task<List<Category>> GetLiveCategories(string host, string username, string password);
    Task<List<VodStream>> GetVodStreams(string host, string username, string password, string? categoryId = null);
    Task<List<Category>> GetVodCategories(string host, string username, string password);
    Task<List<SeriesStream>> GetSeriesStreams(string host, string username, string password, string? categoryId = null);
    Task<List<Category>> GetSeriesCategories(string host, string username, string password);
    Task<EpgListings> GetFullEpgListings(string host, string username, string password, string streamId);
    Task<EpgListings> GetShortEpgListings(string host, string username, string password, string streamId);
    Task<byte[]> GetFullXmlEpg(string host, string username, string password);
}