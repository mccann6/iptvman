using IptvMan.Models;
using IptvMan.Services;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace IptvMan.Controllers;

[ApiController]
[Route("api")]
public class ManagementController(ILiteDatabase db, IMemoryCache cache, IAccountService accountService) : ControllerBase
{
    private readonly ILiteCollection<FilterSettings> _settings = db.GetCollection<FilterSettings>("settings");

    [HttpGet("accounts")]
    public ActionResult<IEnumerable<Account>> GetAccounts()
    {
        return Ok(accountService.GetAllAccounts());
    }

    [HttpPost("accounts")]
    public IActionResult AddAccount(Account account)
    {
        try
        {
            var existing = accountService.GetAllAccounts().FirstOrDefault(x => x.Id == account.Id);
            if (existing != null)
            {
                return Conflict("Account with this ID already exists.");
            }
            
            accountService.AddAccount(account);
            return CreatedAtAction(nameof(GetAccounts), new { id = account.Id }, account);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("accounts/{id}")]
    public IActionResult UpdateAccount(string id, Account account)
    {
        if (id != account.Id)
        {
            return BadRequest("ID mismatch");
        }

        if (!accountService.UpdateAccount(account))
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("accounts/{id}")]
    public IActionResult DeleteAccount(string id)
    {
        if (!accountService.DeleteAccount(id))
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("filters")]
    public ActionResult<FilterSettings> GetFilters()
    {
        var settings = _settings.FindById(1) ?? new FilterSettings();
        return Ok(settings);
    }

    [HttpPost("filters")]
    public IActionResult SaveFilters(FilterSettings settings)
    {
        settings.Id = 1; // Ensure singleton ID
        _settings.Upsert(settings);
        return Ok(settings);
    }

    [HttpPost("cache/clear")]
    public IActionResult ClearCache()
    {
        if (cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
            return Ok("Cache cleared.");
        }
        return BadRequest("Cache type does not support clearing.");
    }
}
