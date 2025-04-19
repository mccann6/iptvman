using IptvMan.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IptvMan.Clients;

public class XtreamClient(HttpClient httpClient, IMemoryCache memoryCache)
    : BaseClient(httpClient, memoryCache), IXtreamClient
{
    private const string PlayerApi = "player_api.php";
    private const string EpgApi = "xmltv.php";
    private const string GetLiveCategoriesAction = "get_live_categories";
    private const string GetVodCategoriesAction = "get_vod_categories";
    private const string GetLiveStreamsAction = "get_live_streams";
    private const string GetVodStreamsAction = "get_vod_streams";
    private const string GetSeriesCategoriesAction = "get_series_categories";
    private const string GetSeriesStreamsAction = "get_series";
    private const string GetShortEpgAction = "get_short_epg";
    private const string GetFullEpgAction = "get_simple_data_table";

    public Task<AccountInfo> GetAccountInfo(string host, string username, string password)
    {
        return DoCachedHttpCall<AccountInfo>(host, $"/{PlayerApi}?username={username}&password={password}");
    }

    public Task<List<LiveStream>> GetLiveStreams(string host, string username, string password, string? categoryId = null)
    {
        var resource = string.IsNullOrWhiteSpace(categoryId)
            ? $"/{PlayerApi}?username={username}&password={password}&action={GetLiveStreamsAction}"
            : $"/{PlayerApi}?username={username}&password={password}&action={GetLiveStreamsAction}&category_id={categoryId}";
        return DoCachedHttpCall<List<LiveStream>>(host, resource);
    }

    public Task<List<Category>> GetLiveCategories(string host, string username, string password)
    {
        return DoCachedHttpCall<List<Category>>(host, $"/{PlayerApi}?username={username}&password={password}&action={GetLiveCategoriesAction}");
    }
    
    public Task<List<VodStream>> GetVodStreams(string host, string username, string password, string? categoryId = null)
    {
        var resource = string.IsNullOrWhiteSpace(categoryId)
            ? $"/{PlayerApi}?username={username}&password={password}&action={GetVodStreamsAction}"
            : $"/{PlayerApi}?username={username}&password={password}&action={GetVodStreamsAction}&category_id={categoryId}";
        return DoCachedHttpCall<List<VodStream>>(host, resource);
    }

    public Task<List<Category>> GetVodCategories(string host, string username, string password)
    {
        return DoCachedHttpCall<List<Category>>(host, $"/{PlayerApi}?username={username}&password={password}&action={GetVodCategoriesAction}");
    }
    
    public Task<List<SeriesStream>> GetSeriesStreams(string host, string username, string password, string? categoryId = null)
    {
        var resource = string.IsNullOrWhiteSpace(categoryId)
            ? $"/{PlayerApi}?username={username}&password={password}&action={GetSeriesStreamsAction}"
            : $"/{PlayerApi}?username={username}&password={password}&action={GetSeriesStreamsAction}&category_id={categoryId}";
        return DoCachedHttpCall<List<SeriesStream>>(host, resource);
    }
    
    public Task<List<Category>> GetSeriesCategories(string host, string username, string password)
    {
        return DoCachedHttpCall<List<Category>>(host, $"/{PlayerApi}?username={username}&password={password}&action={GetSeriesCategoriesAction}");
    }

    public Task<EpgListings> GetFullEpgListings(string host, string username, string password, string streamId)
    {
        return DoCachedHttpCall<EpgListings>(host, $"/{PlayerApi}?username={username}&password={password}&action={GetFullEpgAction}&stream_id={streamId}");
    }

    public Task<EpgListings> GetShortEpgListings(string host, string username, string password, string streamId)
    {
        return DoCachedHttpCall<EpgListings>(host, $"/{PlayerApi}?username={username}&password={password}&action={GetShortEpgAction}&stream_id={streamId}");
    }

    public Task<byte[]> GetFullXmlEpg(string host, string username, string password)
    {
        return DoHttpGetBytes($"{host}/{EpgApi}?username={username}&password={password}");
    }
}
