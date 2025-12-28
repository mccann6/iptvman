using IptvMan.Models;

namespace IptvMan.Services;

public interface IAccountService
{
    Account GetAccount(string id);
    IEnumerable<Account> GetAllAccounts();
    void AddAccount(Account account);
    bool UpdateAccount(Account account);
    bool DeleteAccount(string id);
}
