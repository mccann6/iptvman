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
    private readonly IChannelMappingService _channelMappingService;

    public ApiService(IXtreamClient xtreamClient, ILogger<ApiService> logger, IAccountService accountService, IChannelMappingService channelMappingService)
    {
        _xtreamClient = xtreamClient;
        _logger = logger;
        _accountService = accountService;
        _channelMappingService = channelMappingService;
    }

    public async Task<string> DoPlayerApiCall(
        string id,
        string? action, 
        string? username, 
        string? password, 
        string? categoryId,
        string? streamId,
        string? vodId,
        string? seriesId,
        bool? bypassFilters = null,
        int? page = null,
        int? pageSize = null)
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
            "get_live_streams" => await GetLiveStreams(account, user, pass, categoryId, bypassFilters ?? false, page, pageSize ?? 100),
            "get_live_categories" => await GetLiveCategories(account, user, pass, bypassFilters ?? false),
            "get_vod_categories" => await GetVodCategories(account, user, pass, bypassFilters ?? false),
            "get_series_categories" => await GetSeriesCategories(account, user, pass, bypassFilters ?? false),
            "get_vod_streams" => await GetVodStreams(account, user, pass, categoryId, bypassFilters ?? false),
            "get_series" => await GetSeriesStreams(account, user, pass, categoryId, bypassFilters ?? false),
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

    private async Task<string> GetVodCategories(Account account, string username, string password, bool bypassFilters = false)
    {
        var response = await _xtreamClient.GetVodCategories(account.Host, username, password);
        if (bypassFilters)
            return JsonSerializer.Serialize(response);
        return JsonSerializer.Serialize(ApplyVodCategoryFilters(response, account));
    }

    private async Task<string> GetLiveStreams(Account account, string username, string password, string? categoryId = null, bool bypassFilters = false, int? page = null, int pageSize = 100)
    {
        var response = await _xtreamClient.GetLiveStreams(account.Host, username, password, categoryId);

        if (!bypassFilters)
        {
            if (categoryId == null && account.FilterSettings.AllowedLiveCategoryIds.Count != 0)
            {
                _logger.LogInformation("Called LiveStreams without categoryId, getting categories to filter by allowed categories");
                var categories = await _xtreamClient.GetLiveCategories(account.Host, username, password);
                categories = ApplyLiveCategoryFilters(categories, account);
                response = ApplyLiveStreamFilters(response, categories.Where(x => x.Id != null).Select(x=>x.Id!).ToList(), account);
            }
            else
            {
                response = ApplyLiveStreamFilters(response, account);
            }
            
            // Apply channel mappings ONLY when NOT paginating (i.e., for IPTV clients, not management UI)
            if (!page.HasValue)
            {
                response = ApplyChannelMappings(response, account.Id);
            }
        }
        
        // Only paginate if explicitly requested (for management UI)
        // IPTV clients expect full array, not paginated response
        if (page.HasValue)
        {
            var totalCount = response.Count;
            var skip = (page.Value - 1) * pageSize;
            var paginatedResponse = response.Skip(skip).Take(pageSize).ToList();
            
            // Return with pagination metadata
            var result = new
            {
                streams = paginatedResponse,
                pagination = new
                {
                    current_page = page.Value,
                    page_size = pageSize,
                    total_items = totalCount,
                    total_pages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };
            
            return JsonSerializer.Serialize(result);
        }
        
        // Return full array for backward compatibility with IPTV clients
        return JsonSerializer.Serialize(response);
    }
    
    private async Task<string> GetLiveCategories(Account account, string username, string password, bool bypassFilters = false)
    {
        var response = await _xtreamClient.GetLiveCategories(account.Host, username, password);
        if (bypassFilters)
            return JsonSerializer.Serialize(response);
        return JsonSerializer.Serialize(ApplyLiveCategoryFilters(response, account));
    }
    
    private async Task<string> GetSeriesCategories(Account account, string username, string password, bool bypassFilters = false)
    {
        var response = await _xtreamClient.GetSeriesCategories(account.Host, username, password);
        if (bypassFilters)
            return JsonSerializer.Serialize(response);
        return JsonSerializer.Serialize(ApplySeriesCategoryFilters(response, account));
    }
    
    private async Task<string> GetVodStreams(Account account, string username, string password, string? categoryId = null, bool bypassFilters = false)
    {
        var response = await _xtreamClient.GetVodStreams(account.Host, username, password, categoryId);
        
        if (bypassFilters)
            return JsonSerializer.Serialize(response);
        
        if (categoryId == null && account.FilterSettings.AllowedVodCategoryIds.Count != 0)
        {
            _logger.LogInformation("Called VodStreams without categoryId, getting categories to filter by allowed categories");
            var categories = await _xtreamClient.GetVodCategories(account.Host, username, password);
            categories = ApplyVodCategoryFilters(categories, account);
            return JsonSerializer.Serialize(ApplyVodStreamFilters(response, categories.Where(x => x.Id != null).Select(x=>x.Id!).ToList(), account));
        }
        
        return JsonSerializer.Serialize(ApplyVodStreamFilters(response, account));
    }
    
    private async Task<string> GetSeriesStreams(Account account, string username, string password, string? categoryId = null, bool bypassFilters = false)
    {
        var response = await _xtreamClient.GetSeriesStreams(account.Host, username, password, categoryId);
        
        if (bypassFilters)
            return JsonSerializer.Serialize(response);
        
        if (categoryId == null && account.FilterSettings.AllowedSeriesCategoryIds.Count != 0)
        {
            _logger.LogInformation("Called SeriesStreams without categoryId, getting categories to filter by allowed categories");
            var categories = await _xtreamClient.GetSeriesCategories(account.Host, username, password);
            categories = ApplySeriesCategoryFilters(categories, account);
            return JsonSerializer.Serialize(ApplySeriesStreamFilters(response, categories.Where(x => x.Id != null).Select(x=>x.Id!).ToList()));
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

    public async Task InitializeCategoriesAsync(string id, string? username, string? password)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required.");

        _logger.LogInformation("Initializing categories for account {Id}", id);
        
        var liveCategories = await _xtreamClient.GetLiveCategories(account.Host, user, pass);
        var vodCategories = await _xtreamClient.GetVodCategories(account.Host, user, pass);
        var seriesCategories = await _xtreamClient.GetSeriesCategories(account.Host, user, pass);
        
        account.FilterSettings.AllowedLiveCategoryIds = liveCategories
            .Where(x => x.Id != null)
            .Select(x => x.Id!)
            .ToList();
        
        account.FilterSettings.AllowedVodCategoryIds = vodCategories
            .Where(x => x.Id != null)
            .Select(x => x.Id!)
            .ToList();
        
        account.FilterSettings.AllowedSeriesCategoryIds = seriesCategories
            .Where(x => x.Id != null)
            .Select(x => x.Id!)
            .ToList();
        
        _accountService.UpdateAccount(account);
        _logger.LogInformation("Categories initialized for account {Id}", id);
    }
    
    public async Task<CategoryRefreshResult> RefreshLiveCategoriesAsync(string id, string? username, string? password)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required.");

        _logger.LogInformation("Refreshing live categories for account {Id}", id);
        
        var upstreamCategories = await _xtreamClient.GetLiveCategories(account.Host, user, pass);
        var upstreamIds = upstreamCategories.Where(x => x.Id != null).Select(x => x.Id!).ToHashSet();
        
        var knownIds = account.FilterSettings.AllowedLiveCategoryIds
            .Union(account.FilterSettings.NotAllowedLiveCategoryIds)
            .ToHashSet();
        
        var newCategoryIds = upstreamIds.Except(knownIds).ToHashSet();
        var newCategories = upstreamCategories.Where(x => x.Id != null && newCategoryIds.Contains(x.Id)).ToList();
        
        account.FilterSettings.AllowedLiveCategoryIds = 
            account.FilterSettings.AllowedLiveCategoryIds.Where(x => upstreamIds.Contains(x)).ToList();
        account.FilterSettings.NotAllowedLiveCategoryIds = 
            account.FilterSettings.NotAllowedLiveCategoryIds.Where(x => upstreamIds.Contains(x)).ToList();
        
        if (newCategories.Count > 0 || 
            account.FilterSettings.AllowedLiveCategoryIds.Count + account.FilterSettings.NotAllowedLiveCategoryIds.Count != knownIds.Count)
        {
            _accountService.UpdateAccount(account);
            _logger.LogInformation("Live categories refreshed for account {Id}. New: {NewCount}, Removed: {RemovedCount}", 
                id, newCategories.Count, knownIds.Count - upstreamIds.Count);
        }
        
        return new CategoryRefreshResult { NewCategories = newCategories };
    }
    
    public async Task<CategoryRefreshResult> RefreshVodCategoriesAsync(string id, string? username, string? password)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required.");

        _logger.LogInformation("Refreshing VOD categories for account {Id}", id);
        
        var upstreamCategories = await _xtreamClient.GetVodCategories(account.Host, user, pass);
        var upstreamIds = upstreamCategories.Where(x => x.Id != null).Select(x => x.Id!).ToHashSet();
        
        var knownIds = account.FilterSettings.AllowedVodCategoryIds
            .Union(account.FilterSettings.NotAllowedVodCategoryIds)
            .ToHashSet();
        
        var newCategoryIds = upstreamIds.Except(knownIds).ToHashSet();
        var newCategories = upstreamCategories.Where(x => x.Id != null && newCategoryIds.Contains(x.Id)).ToList();
        
        account.FilterSettings.AllowedVodCategoryIds = 
            account.FilterSettings.AllowedVodCategoryIds.Where(x => upstreamIds.Contains(x)).ToList();
        account.FilterSettings.NotAllowedVodCategoryIds = 
            account.FilterSettings.NotAllowedVodCategoryIds.Where(x => upstreamIds.Contains(x)).ToList();
        
        if (newCategories.Count > 0 || 
            account.FilterSettings.AllowedVodCategoryIds.Count + account.FilterSettings.NotAllowedVodCategoryIds.Count != knownIds.Count)
        {
            _accountService.UpdateAccount(account);
            _logger.LogInformation("VOD categories refreshed for account {Id}. New: {NewCount}, Removed: {RemovedCount}", 
                id, newCategories.Count, knownIds.Count - upstreamIds.Count);
        }
        
        return new CategoryRefreshResult { NewCategories = newCategories };
    }
    
    public async Task<CategoryRefreshResult> RefreshSeriesCategoriesAsync(string id, string? username, string? password)
    {
        var account = GetAccount(id);
        var user = !string.IsNullOrEmpty(account.Username) ? account.Username : username;
        var pass = !string.IsNullOrEmpty(account.Password) ? account.Password : password;
        
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            throw new ArgumentException("Username and Password are required.");

        _logger.LogInformation("Refreshing series categories for account {Id}", id);
        
        var upstreamCategories = await _xtreamClient.GetSeriesCategories(account.Host, user, pass);
        var upstreamIds = upstreamCategories.Where(x => x.Id != null).Select(x => x.Id!).ToHashSet();
        
        var knownIds = account.FilterSettings.AllowedSeriesCategoryIds
            .Union(account.FilterSettings.NotAllowedSeriesCategoryIds)
            .ToHashSet();
        
        var newCategoryIds = upstreamIds.Except(knownIds).ToHashSet();
        var newCategories = upstreamCategories.Where(x => x.Id != null && newCategoryIds.Contains(x.Id)).ToList();
        
        account.FilterSettings.AllowedSeriesCategoryIds = 
            account.FilterSettings.AllowedSeriesCategoryIds.Where(x => upstreamIds.Contains(x)).ToList();
        account.FilterSettings.NotAllowedSeriesCategoryIds = 
            account.FilterSettings.NotAllowedSeriesCategoryIds.Where(x => upstreamIds.Contains(x)).ToList();
        
        if (newCategories.Count > 0 || 
            account.FilterSettings.AllowedSeriesCategoryIds.Count + account.FilterSettings.NotAllowedSeriesCategoryIds.Count != knownIds.Count)
        {
            _accountService.UpdateAccount(account);
            _logger.LogInformation("Series categories refreshed for account {Id}. New: {NewCount}, Removed: {RemovedCount}", 
                id, newCategories.Count, knownIds.Count - upstreamIds.Count);
        }
        
        return new CategoryRefreshResult { NewCategories = newCategories };
    }

    private static List<Category> ApplyLiveCategoryFilters(List<Category> categories, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            categories = categories.Where(x =>
                x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        if (account.FilterSettings.AllowedLiveCategoryIds.Count == 0)
            return categories;
        
        return categories.Where(x => x.Id != null && account.FilterSettings.AllowedLiveCategoryIds.Contains(x.Id)).ToList();
    }
    
    private static List<Category> ApplyVodCategoryFilters(List<Category> categories, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            categories = categories.Where(x =>
                x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        if (account.FilterSettings.AllowedVodCategoryIds.Count == 0)
            return categories;
        
        return categories.Where(x => x.Id != null && account.FilterSettings.AllowedVodCategoryIds.Contains(x.Id)).ToList();
    }
    
    private static List<Category> ApplySeriesCategoryFilters(List<Category> categories, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            categories = categories.Where(x =>
                x.Name != null && !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        if (account.FilterSettings.AllowedSeriesCategoryIds.Count == 0)
            return categories;
        
        return categories.Where(x => x.Id != null && account.FilterSettings.AllowedSeriesCategoryIds.Contains(x.Id)).ToList();
    }

    private static List<LiveStream> ApplyLiveStreamFilters(List<LiveStream> liveStreams, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            liveStreams = liveStreams.Where(x => !IsAdultContent(x.IsAdult) && x.Name != null && 
                !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        return liveStreams;
    }
    
    private static List<LiveStream> ApplyLiveStreamFilters(List<LiveStream> liveStreams, List<string> categoryIds, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            liveStreams = liveStreams.Where(x => !IsAdultContent(x.IsAdult) && x.Name != null &&
                !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        return liveStreams.Where(x => x.CategoryId != null && categoryIds.Contains(x.CategoryId)).ToList();
    }
    
    private static List<VodStream> ApplyVodStreamFilters(List<VodStream> vodStreams, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            vodStreams = vodStreams.Where(x => !IsAdultContent(x.IsAdult) && x.Name != null && 
                !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        return vodStreams;
    }
    
    private static List<VodStream> ApplyVodStreamFilters(List<VodStream> vodStreams, List<string> categoryIds, Account account)
    {
        if (account.FilterSettings.AdultFilter)
            vodStreams = vodStreams.Where(x => !IsAdultContent(x.IsAdult) && x.Name != null &&
                !x.Name.Contains("adult", StringComparison.InvariantCultureIgnoreCase)).ToList();
        
        return vodStreams.Where(x => x.CategoryId != null && categoryIds.Contains(x.CategoryId)).ToList();
    }
    
    private static List<SeriesStream> ApplySeriesStreamFilters(List<SeriesStream> seriesStreams, List<string> categoryIds)
    {
        return seriesStreams.Where(x => x.CategoryId != null && categoryIds.Contains(x.CategoryId)).ToList();
    }
    
    private List<LiveStream> ApplyChannelMappings(List<LiveStream> streams, string accountId)
    {
        var mappings = _channelMappingService.GetMappings(accountId).ToList();
        if (!mappings.Any()) return streams;
        
        var mappingDict = mappings.ToDictionary(m => m.OriginalStreamId, m => m);
        var result = new List<LiveStream>();
        
        foreach (var stream in streams)
        {
            var streamId = stream.StreamId.ToString();
            
            if (mappingDict.TryGetValue(streamId, out var mapping))
            {
                // Skip if hidden
                if (!mapping.IsVisible) continue;
                
                // Apply custom name if set
                if (!string.IsNullOrEmpty(mapping.CustomName))
                    stream.Name = mapping.CustomName;
                
                // Apply custom group if set  
                if (!string.IsNullOrEmpty(mapping.CustomGroupName))
                    stream.CategoryId = mapping.CustomGroupName;
            }
            
            result.Add(stream);
        }
        
        return result.OrderBy(s =>
        {
            var streamId = s.StreamId.ToString();
            if (mappingDict.TryGetValue(streamId, out var mapping))
                return mapping.SortOrder;
            return int.MaxValue;
        }).ToList();
    }

    private static bool IsAdultContent(dynamic? isAdult)
    {
        if (isAdult == null) return false;
        return isAdult.ToString() == "1";
    }
}