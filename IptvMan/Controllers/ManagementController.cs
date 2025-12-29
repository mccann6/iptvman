using IptvMan.Models;
using IptvMan.Services;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace IptvMan.Controllers;

[ApiController]
[Route("api")]
public class ManagementController(ILiteDatabase db, IMemoryCache cache, IAccountService accountService, IApiService apiService) : ControllerBase
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
        settings.Id = 1;
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
    
    [HttpPost("accounts/{id}/categories/initialize")]
    public async Task<IActionResult> InitializeCategories(string id, [FromQuery] string? username, [FromQuery] string? password)
    {
        try
        {
            await apiService.InitializeCategoriesAsync(id, username, password);
            return Ok("Categories initialized successfully.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("accounts/{id}/categories/live/refresh")]
    public async Task<ActionResult<CategoryRefreshResult>> RefreshLiveCategories(string id, [FromQuery] string? username, [FromQuery] string? password)
    {
        try
        {
            var result = await apiService.RefreshLiveCategoriesAsync(id, username, password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("accounts/{id}/categories/vod/refresh")]
    public async Task<ActionResult<CategoryRefreshResult>> RefreshVodCategories(string id, [FromQuery] string? username, [FromQuery] string? password)
    {
        try
        {
            var result = await apiService.RefreshVodCategoriesAsync(id, username, password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPost("accounts/{id}/categories/series/refresh")]
    public async Task<ActionResult<CategoryRefreshResult>> RefreshSeriesCategories(string id, [FromQuery] string? username, [FromQuery] string? password)
    {
        try
        {
            var result = await apiService.RefreshSeriesCategoriesAsync(id, username, password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("accounts/{id}/categories/live")]
    public IActionResult UpdateLiveCategories(string id, [FromBody] UpdateCategoriesRequest request)
    {
        try
        {
            var account = accountService.GetAllAccounts().FirstOrDefault(x => x.Id == id);
            if (account == null)
                return NotFound("Account not found.");
            
            account.FilterSettings.AllowedLiveCategoryIds = request.AllowedCategoryIds;
            account.FilterSettings.NotAllowedLiveCategoryIds = request.NotAllowedCategoryIds;
            
            accountService.UpdateAccount(account);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("accounts/{id}/categories/vod")]
    public IActionResult UpdateVodCategories(string id, [FromBody] UpdateCategoriesRequest request)
    {
        try
        {
            var account = accountService.GetAllAccounts().FirstOrDefault(x => x.Id == id);
            if (account == null)
                return NotFound("Account not found.");
            
            account.FilterSettings.AllowedVodCategoryIds = request.AllowedCategoryIds;
            account.FilterSettings.NotAllowedVodCategoryIds = request.NotAllowedCategoryIds;
            
            accountService.UpdateAccount(account);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpPut("accounts/{id}/categories/series")]
    public IActionResult UpdateSeriesCategories(string id, [FromBody] UpdateCategoriesRequest request)
    {
        try
        {
            var account = accountService.GetAllAccounts().FirstOrDefault(x => x.Id == id);
            if (account == null)
                return NotFound("Account not found.");
            
            account.FilterSettings.AllowedSeriesCategoryIds = request.AllowedCategoryIds;
            account.FilterSettings.NotAllowedSeriesCategoryIds = request.NotAllowedCategoryIds;
            
            accountService.UpdateAccount(account);
            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
