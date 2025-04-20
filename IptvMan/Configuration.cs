using IptvMan.Models;

namespace IptvMan;

public static class Configuration
{
    private const string ApplicationName = "IptvMan";
    private static bool IsContainer => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")) ? false :
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER").Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER").Equals("1", StringComparison.InvariantCultureIgnoreCase);
    public static List<Account> Accounts => GetAccounts();
    public static List<string> CategoryFilters => GetCategoryFilters();
    public static bool AdultFilter => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ADULT_FILTER")) ? false :
        Environment.GetEnvironmentVariable("ADULT_FILTER").Equals("true", StringComparison.InvariantCultureIgnoreCase);
    public static string AppDataDirectory => IsContainer ? "/app" :
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationName);

    public static int CacheTime => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CACHE_TIME"))
        ? 60
        : int.Parse(Environment.GetEnvironmentVariable("CACHE_TIME"));
    
    public static int EpgTime => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("EPG_TIME"))
        ? 1440
        : int.Parse(Environment.GetEnvironmentVariable("EPG_TIME"));
    
    public static int M3uTime => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("M3U_TIME"))
        ? 720
        : int.Parse(Environment.GetEnvironmentVariable("M3U_TIME"));


    private static List<Account> GetAccounts()
    {
        var accountsVariable = Environment.GetEnvironmentVariable("IPTV_ACCOUNTS") ??
                                  throw new Exception("IPTV_ACCOUNTS environment variable not set");
        
        return accountsVariable.Split('|')
            .Select(accountSettings => accountSettings.Split(";"))
            .Select(splitSettings =>
                new Account(splitSettings[0], splitSettings[1])).ToList();
    }

    private static List<string> GetCategoryFilters()
    {
        var filters = Environment.GetEnvironmentVariable("CATEGORY_FILTERS") ?? "";
        return filters.Split(';').ToList();
    }
}