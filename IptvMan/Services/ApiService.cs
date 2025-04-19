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
        string action, 
        string username, 
        string password, 
        string? categoryId = null,
        string? streamId = null)
    {
        switch (action)
        {
            case "get_live_streams":
                 return await GetLiveStreams(id, username, password, categoryId);
            case "get_live_categories":
                return await GetLiveCategories(id, username, password);
            case "get_vod_categories":
                return await GetVodCategories(id, username, password);
            case "get_series_categories":
                return await GetSeriesCategories(id, username, password);
            case "get_vod_streams":
                return await GetVodStreams(id, username, password, categoryId);
            case "get_series":
                return await GetSeriesStreams(id, username, password, categoryId);
            case "get_simple_data_table":
                return await GetFullEpgListings(id, username, password, streamId);
            case "get_short_epg":
                return await GetShortEpgListings(id, username, password, streamId);
            default:
                throw new NotImplementedException($"Action '{action}' is not implemented.");
        }
    }

    public async Task<byte[]> DoEpgApiCall(string id, string username, string password)
    {
        var account = GetAccount(id);
        var exists = CurrentEpgFileExists(id);

        if (exists)
        {
            return await File.ReadAllBytesAsync(GetEpgFilePath(id));
        }

        var response = await _xtreamClient.GetFullXmlEpg(account.Host, username, password);
        await SaveEpgFile(id, response);
        return response;
    }

    private string GetEpgFilePath(string id) => Path.Combine(Configuration.AppDataDirectory, $"{id}.xml");

    private bool CurrentEpgFileExists(string id)
    {
        var filePath = GetEpgFilePath(id);
        if (!File.Exists(filePath)) return false;
        return DateTime.UtcNow - File.GetCreationTimeUtc(filePath) < TimeSpan.FromDays(1);
    }

    private async Task SaveEpgFile(string id, byte[] fileBytes)
    {
        var pathToWrite = Configuration.AppDataDirectory;
        Directory.CreateDirectory(pathToWrite);
        await File.WriteAllBytesAsync(GetEpgFilePath(id), fileBytes);
    }

    private async Task<string> GetVodCategories(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetVodCategories(account.Host, username, password);
        return JsonSerializer.Serialize(response);
    }

    private async Task<string> GetLiveStreams(string id, string username, string password, string? categoryId = null)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetLiveStreams(account.Host, username, password, categoryId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetLiveCategories(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetLiveCategories(account.Host, username, password);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetSeriesCategories(string id, string username, string password)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetSeriesCategories(account.Host, username, password);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetVodStreams(string id, string username, string password, string? categoryId = null)
    {
        var account = GetAccount(id);
        var response = await _xtreamClient.GetVodStreams(account.Host, username, password, categoryId);
        return JsonSerializer.Serialize(response);
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

    private static Account GetAccount(string id) => Configuration.Accounts.First(x => x.Id == id);
}