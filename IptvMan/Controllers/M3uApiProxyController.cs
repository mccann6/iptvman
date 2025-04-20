using System.Text;
using Microsoft.AspNetCore.Mvc;
using IptvMan.Services;

namespace IptvMan.Controllers;

[ApiController]
[Route("{id}/get.php")]
public class M3uProxyController(IApiService apiService, ILogger<PlayerApiProxyController> logger) : ControllerBase
{
    [HttpGet(Name = "M3uGet")]
    public async Task<IActionResult> Get(
        string id,
        [FromQuery] string username,
        [FromQuery] string password,
        [FromQuery] string output = "ts",
        [FromQuery] string type = "m3u_plus")
    {
        try
        {
            var response = await apiService.DoM3uApiCall(id, username, password, output, type);
            return File(response, "text/plain", "id.m3u");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching API response.");
            return StatusCode(500, "Internal server error");
        }
    }
}