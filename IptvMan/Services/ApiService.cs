using System.Text;
using System.Text.Json;
using IptvMan.Clients;
using IptvMan.Models;

namespace IptvMan.Services;

public class ApiService : IApiService
{
    private readonly ILogger<ApiService> _logger;
    private readonly IXtreamClient _xtreamClient;
    private readonly IAccountService _accountService;

    public ApiService(IXtreamClient xtreamClient, ILogger<ApiService> logger, IAccountService accountService)
    {
        _xtreamClient = xtreamClient;
        _logger = logger;
        _accountService = accountService;
    }

    public async Task<string> DoPlayerApiCall(
        string id,
        string? action, 
        string? username, 
        string? password, 
        string? categoryId,
        string? streamId,
        string? vodId,
        string? seriesId)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required either in the request or in the account settings.");

        if (action == null)
            return await GetAccountInfo(account, user, pass);
        
        _logger.LogInformation("Call made to action {Action} for account {Id}", action, id);
        
        return action switch
        {
            "get_live_streams" => await GetLiveStreams(account, user, pass, categoryId),
            "get_live_categories" => await GetLiveCategories(account, user, pass),
            "get_vod_categories" => await GetVodCategories(account, user, pass),
            "get_series_categories" => await GetSeriesCategories(account, user, pass),
            "get_vod_streams" => await GetVodStreams(account, user, pass, categoryId),
            "get_series" => await GetSeriesStreams(account, user, pass, categoryId),
            "get_simple_data_table" => await GetFullEpgListings(account, user, pass, streamId),
            "get_short_epg" => await GetShortEpgListings(account, user, pass, streamId),
            "get_vod_info" => await GetVodInfo(account, user, pass, vodId),
            "get_series_info" => await GetSeriesInfo(account, user, pass, seriesId),
            _ => throw new NotImplementedException($"Action '{action}' is not implemented.")
        };
    }

    public async Task<byte[]> DoEpgApiCall(string id, string? username, string? password)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required.");

        var exists = CurrentEpgFileExists(id);

        if (exists)
        {
            _logger.LogInformation("Current EPG File already exists for {Id}", id);
            return await File.ReadAllBytesAsync(GetEpgFilePath(id));
        }

        var response = await _xtreamClient.GetFullXmlEpg(account.Host, user, pass);
        var fileText = Encoding.UTF8.GetString(response);
        var cleanedFileText = fileText.Substring(0, fileText.LastIndexOf("</tv>", StringComparison.Ordinal) + 5);
        var fileBytes = Encoding.UTF8.GetBytes(cleanedFileText);
        
        await SaveEpgFile(id, fileBytes);
        return fileBytes;
    }

    public async Task<byte[]> DoM3uApiCall(string id, string? username, string? password, string output, string type)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required.");

        var exists = CurrentM3uFileExists(id);

        if (exists)
        {
            _logger.LogInformation("Current M3u File already exists for {Id}", id);
            return await File.ReadAllBytesAsync(GetM3uFilePath(id));
        }
        
        var response = await _xtreamClient.GetFullM3u(account.Host, user, pass, output, type);
        await SaveM3uFile(id, response);
        return response;
    }

    public string GetStreamUrl(string id, string type, string? username, string? password, string stream)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        return $"{account.Host}/{type}/{user}/{pass}/{stream}";
    }

    private async Task<string> GetAccountInfo(Account account, string username, string password)
    {
        var response = await _xtreamClient.GetAccountInfo(account.Host, username, password);
        return JsonSerializer.Serialize(response);
    }

    private async Task<string> GetVodCategories(Account account, string username, string password)
    {
        var response = await _xtreamClient.GetVodCategories(account.Host, username, password);
        return JsonSerializer.Serialize(ApplyFilters(response, account));
    }

    private async Task<string> GetLiveStreams(Account account, string username, string password, string? categoryId = null)
    {
        var response = await _xtreamClient.GetLiveStreams(account.Host, username, password, categoryId);

        if (categoryId == null && account.FilterSettings.CategoryFilters.Count != 0)
        {
            _logger.LogInformation("Called LiveStreams without categoryId, getting categories to filter by filtered categories");
            var categories = await _xtreamClient.GetLiveCategories(account.Host, username, password);
            categories = ApplyFilters(categories, account);
            return JsonSerializer.Serialize(ApplyFilters(response, categories.Select(x=>x.Id.ToString()).ToList(), account));
        }
        
        return JsonSerializer.Serialize(ApplyFilters(response, account));
    }
    
    private async Task<string> GetLiveCategories(Account account, string username, string password)
    {
        var response = await _xtreamClient.GetLiveCategories(account.Host, username, password);
        return JsonSerializer.Serialize(ApplyFilters(response, account));
    }
    
    private async Task<string> GetSeriesCategories(Account account, string username, string password)
    {
        var response = await _xtreamClient.GetSeriesCategories(account.Host, username, password);
        return JsonSerializer.Serialize(ApplyFilters(response, account));
    }
    
    private async Task<string> GetVodStreams(Account account, string username, string password, string? categoryId = null)
    {
        var response = await _xtreamClient.GetVodStreams(account.Host, username, password, categoryId);
        
        if (categoryId == null && account.FilterSettings.CategoryFilters.Count != 0)
        {
            _logger.LogInformation("Called VodStreams without categoryId, getting categories to filter by filtered categories");
            var categories = await _xtreamClient.GetVodCategories(account.Host, username, password);
            categories = ApplyFilters(categories, account);
            return JsonSerializer.Serialize(ApplyFilters(response, categories.Select(x=>x.Id.ToString()).ToList(), account));
        }
        
        return JsonSerializer.Serialize(ApplyFilters(response, account));
    }
    
    private async Task<string> GetSeriesStreams(Account account, string username, string password, string? categoryId = null)
    {
        var response = await _xtreamClient.GetSeriesStreams(account.Host, username, password, categoryId);
        
        if (categoryId == null && account.FilterSettings.CategoryFilters.Count != 0)
        {
            _logger.LogInformation("Called SeriesStreams without categoryId, getting categories to filter by filtered categories");
            var categories = await _xtreamClient.GetSeriesCategories(account.Host, username, password);
            categories = ApplyFilters(categories, account);
            return JsonSerializer.Serialize(ApplyFilters(response, categories.Select(x=>x.Id.ToString()).ToList()));
        }
        
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetFullEpgListings(Account account, string username, string password, string streamId)
    {
        var response = await _xtreamClient.GetFullEpgListings(account.Host, username, password, streamId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetShortEpgListings(Account account, string username, string password, string streamId)
    {
        var response = await _xtreamClient.GetShortEpgListings(account.Host, username, password, streamId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetVodInfo(Account account, string username, string password, string vodId)
    {
        var response = await _xtreamClient.GetVodInfo(account.Host, username, password, vodId);
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetSeriesInfo(Account account, string username, string password, string seriesId)
    {
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
    
    private Account GetAccount(string id) => _accountService.GetAccount(id);

    private static List<Category> ApplyFilters(List<Category> categories, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            categories = categories.Where(x =>
                x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        return categories.Where(x => account.FilterSettings.CategoryFilters.Any(y => x.Name != null && x.Name.Contains(y)))
            .ToList();
    }

    private static List<LiveStream> ApplyFilters(List<LiveStream> liveStreams, Account account) => account.FilterSettings.AdultFilter
        ? liveStreams.Where(x => !IsAdultContent(x.IsAdult) && x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList()
        : liveStreams;
    
    private static List<LiveStream> ApplyFilters(List<LiveStream> liveStreams, List<string> categoryIds, Account account)
    {
        return account.FilterSettings.AdultFilter
            ? liveStreams.Where(x =>
                !IsAdultContent(x.IsAdult) && x.Name != null &&
                !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase) &&
                categoryIds.Contains(x.CategoryId)).ToList()
            : liveStreams.Where(x => categoryIds.Contains(x.CategoryId)).ToList();
    }
    
    private static List<VodStream> ApplyFilters(List<VodStream> vodStreams, List<string> categoryIds, Account account)
    {
        return account.FilterSettings.AdultFilter
            ? vodStreams.Where(x =>
                !IsAdultContent(x.IsAdult) && x.Name != null &&
                !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase) &&
                categoryIds.Contains(x.CategoryId)).ToList()
            : vodStreams.Where(x => categoryIds.Contains(x.CategoryId)).ToList();
    }
    
    private static List<SeriesStream> ApplyFilters(List<SeriesStream> seriesStreams, List<string> categoryIds)
    {
        return seriesStreams.Where(x => categoryIds.Contains(x.CategoryId)).ToList();
    }

    private static List<VodStream> ApplyFilters(List<VodStream> liveStreams, Account account) => account.FilterSettings.AdultFilter
        ? liveStreams.Where(x => !IsAdultContent(x.IsAdult) && x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList()
        : liveStreams;

    private static bool IsAdultContent(dynamic? isAdult)
    {
        if (isAdult == null) return false;
        return isAdult.ToString() == "1";
    }
}