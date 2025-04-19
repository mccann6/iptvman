using IptvMan.Models;

namespace IptvMan;

public static class Configuration
{
    private const string ApplicationName = "IptvMan";
    private static bool IsContainer => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER")) ? false :
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER").Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
        Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER").Equals("1", StringComparison.InvariantCultureIgnoreCase);
    public static List<Account> Accounts => GetAccounts();
    public static string AppDataDirectory => IsContainer ? "/iptv" :
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationName);


    private static List<Account> GetAccounts()
    {
        var accountsVariable = Environment.GetEnvironmentVariable("IPTV_ACCOUNTS") ??
                                  throw new Exception("IPTV_ACCOUNTS environment variable not set");
        
        return accountsVariable.Split('|')
            .Select(accountSettings => accountSettings.Split(";"))
            .Select(splitSettings =>
                new Account(splitSettings[0], splitSettings[1])).ToList();
    }
}