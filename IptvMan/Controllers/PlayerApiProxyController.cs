using Microsoft.AspNetCore.Mvc;
using IptvMan.Services;

namespace IptvMan.Controllers;

[ApiController]
[Route("{id}/player_api.php")]
public class PlayerApiProxyController(IApiService apiService, ILogger<PlayerApiProxyController> logger) : ControllerBase
{
    [HttpGet(Name = "PlayerApiGet")]
    public async Task<IActionResult> Get(
        string id,
        [FromQuery] string? username, 
        [FromQuery] string? password, 
        [FromQuery] string action=null, 
        [FromQuery] string category_id=null,
        [FromQuery] string stream_id=null,
        [FromQuery] string vod_id=null,
        [FromQuery] string series_id=null)
    {
        try
        {
            var response = await apiService.DoPlayerApiCall(id, action, username, password, category_id, stream_id, vod_id, series_id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching API response.");
            return StatusCode(500, "Internal server error");
        }
    }
}
