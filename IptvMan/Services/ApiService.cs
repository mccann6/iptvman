using System.Text;
using System.Text.Json;
using IptvMan.Clients;
using IptvMan.Models;

namespace IptvMan.Services;

public class ApiService : IApiService
{
    private readonly ILogger<ApiService> _logger;
    private readonly IXtreamClient _xtreamClient;

    public ApiService(IXtreamClient xtreamClient, ILogger<ApiService> logger)
    {
        _xtreamClient = xtreamClient;
        _logger = logger;
    }

    public async Task<string> DoPlayerApiCall(
        string id,
        string? action, 
        string username, 
        string password, 
        string? categoryId,
        string? streamId,
        string? vodId,
        string? seriesId)
    {
        if (action == null)
            return await GetAccountInfo(id, username, password);
        
        return action switch
        {
            "get_live_streams" => await GetLiveStreams(id, username, password, categoryId),
            "get_live_categories" => await GetLiveCategories(id, username, password),
            "get_vod_categories" => await GetVodCategories(id, username, password),
            "get_series_categories" => await GetSeriesCategories(id, username, password),
            "get_vod_streams" => await GetVodStreams(id, username, password, categoryId),
            "get_series" => await GetSeriesStreams(id, username, password, categoryId),
            "get_simple_data_table" => await GetFullEpgListings(id, username, password, streamId),
            "get_short_epg" => await GetShortEpgListings(id, username, password, streamId),
            "get_vod_info" => await GetVodInfo(id, username, password, vodId),
            "get_series_info" => await GetSeriesInfo(id, username, password, seriesId),
            _ => throw new NotImplementedException($"Action '{action}' is not implemented.")
        };
    }

    public async Task<byte[]> DoEpgApiCall(string id, string username, string password)
    {
        var account = GetAccount(id);
        var exists = CurrentEpgFileExists(id);

        if (exists)
        {
            _logger.LogInformation("Current EPG File already exists for {Id}", id);
            return await File.ReadAllBytesAsync(GetEpgFilePath(id));
        }

        var response = await _xtreamClient.GetFullXmlEpg(account.Host, username, password);
        var fileText = Encoding.UTF8.GetString(response);
        var cleanedFileText = fileText.Substring(0, fileText.LastIndexOf("</tv>", StringComparison.Ordinal) + 5);
        var fileBytes = Encoding.UTF8.GetBytes(cleanedFileText);
        
        await SaveEpgFile(id, fileBytes);
        return fileBytes;
    }

    public async Task<byte[]> DoM3uApiCall(string id, string username, string password, string output, string type)
    {
        var account = GetAccount(id);
        var exists = CurrentM3uFileExists(id);

        if (exists)
        {
            _logger.LogInformation("Current M3u File already exists for {Id}", id);
            return await File.ReadAllBytesAsync(GetM3uFilePath(id));
        }
        
        var response = await _xtreamClient.GetFullM3u(account.Host, username, password, output, type);
        await SaveM3uFile(id, response);
        return response;
    }
    
        private async Task<string> GetAccountInfo(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetAccountInfo(account.Host, username, password);
        return JsonSerializer.Serialize(response);
    }

    private async Task<string> GetVodCategories(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetVodCategories(account.Host, username, password);
        return JsonSerializer.Serialize(ApplyFilters(response));
    }

    private async Task<string> GetLiveStreams(string id, string username, string password, string? categoryId = null)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetLiveStreams(account.Host, username, password, categoryId);
        return JsonSerializer.Serialize(ApplyFilters(response));
    }
    
    private async Task<string> GetLiveCategories(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetLiveCategories(account.Host, username, password);
        return JsonSerializer.Serialize(ApplyFilters(response));
    }
    
    private async Task<string> GetSeriesCategories(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetSeriesCategories(account.Host, username, password);
        return JsonSerializer.Serialize(ApplyFilters(response));
    }
    
    private async Task<string> GetVodStreams(string id, string username, string password, string? categoryId = null)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetVodStreams(account.Host, username, password, categoryId);
        return JsonSerializer.Serialize(ApplyFilters(response));
    }
    
    private async Task<string> GetSeriesStreams(string id, string username, string password, string? categoryId = null)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetSeriesStreams(account.Host, username, password, categoryId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetFullEpgListings(string id, string username, string password, string streamId)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetFullEpgListings(account.Host, username, password, streamId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetShortEpgListings(string id, string username, string password, string streamId)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetShortEpgListings(account.Host, username, password, streamId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetVodInfo(string id, string username, string password, string vodId)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetVodInfo(account.Host, username, password, vodId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetSeriesInfo(string id, string username, string password, string seriesId)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetSeriesInfo(account.Host, username, password, seriesId);
        return JsonSerializer.Serialize(response);
    }

    private string GetEpgFilePath(string id) => Path.Combine(Configuration.AppDataDirectory, $"{id}.xml");
    private string GetM3uFilePath(string id) => Path.Combine(Configuration.AppDataDirectory, $"{id}.m3u");
    
    private bool CurrentEpgFileExists(string id)
    {
        var filePath = GetEpgFilePath(id);
        if (!File.Exists(filePath)) return false;
        return DateTime.UtcNow - File.GetCreationTimeUtc(filePath) < TimeSpan.FromMinutes(Configuration.EpgTime);
    }
    
    private bool CurrentM3uFileExists(string id)
    {
        var filePath = GetM3uFilePath(id);
        if (!File.Exists(filePath)) return false;
        return DateTime.UtcNow - File.GetCreationTimeUtc(filePath) < TimeSpan.FromMinutes(Configuration.M3uTime);
    }

    private async Task SaveEpgFile(string id, byte[] fileBytes)
    {
        var pathToWrite = Configuration.AppDataDirectory;
        Directory.CreateDirectory(pathToWrite);
        await File.WriteAllBytesAsync(GetEpgFilePath(id), fileBytes);
        _logger.LogInformation("New EPG File saved for {Id}", id);
    }
    
    private async Task SaveM3uFile(string id, byte[] fileBytes)
    {
        var pathToWrite = Configuration.AppDataDirectory;
        Directory.CreateDirectory(pathToWrite);
        await File.WriteAllBytesAsync(GetM3uFilePath(id), fileBytes);
        _logger.LogInformation("New M3U File saved for {Id}", id);
    }

    private static Account GetAccount(string id) => Configuration.Accounts.First(x => x.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));

    private static List<Category> ApplyFilters(List<Category> categories)
    {
        if (Configuration.AdultFilter)
            categories = categories.Where(x =>
                x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        return categories.Where(x => Configuration.CategoryFilters.Any(y => x.Name != null && x.Name.Contains(y)))
            .ToList();
    }

    private static List<LiveStream> ApplyFilters(List<LiveStream> liveStreams) => Configuration.AdultFilter
        ? liveStreams.Where(x => x.IsAdult != "1" && x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList()
        : liveStreams;
    
    private static List<VodStream> ApplyFilters(List<VodStream> liveStreams) => Configuration.AdultFilter
        ? liveStreams.Where(x => x.IsAdult != "1" && x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList()
        : liveStreams;
}