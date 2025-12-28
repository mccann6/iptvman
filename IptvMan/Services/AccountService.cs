using IptvMan.Models;
using LiteDB;

namespace IptvMan.Services;

public class AccountService : IAccountService
{
    private readonly ILiteCollection<Account> _accounts;

    public AccountService(ILiteDatabase db)
    {
        _accounts = db.GetCollection<Account>("accounts");
    }

    public Account GetAccount(string id)
    {
        var dbAccount = _accounts.FindById(id);
        if (dbAccount != null) return dbAccount;

        throw new KeyNotFoundException($"Account '{id}' not found.");
    }

    public IEnumerable<Account> GetAllAccounts()
    {
        return _accounts.FindAll();
    }

    public void AddAccount(Account account)
    {
        _accounts.Insert(account);
    }

    public bool UpdateAccount(Account account)
    {
        return _accounts.Update(account);
    }

    public bool DeleteAccount(string id)
    {
        return _accounts.Delete(id);
    }
}
