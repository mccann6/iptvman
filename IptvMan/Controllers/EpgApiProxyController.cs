using System.Text;
using Microsoft.AspNetCore.Mvc;
using IptvMan.Services;

namespace IptvMan.Controllers;

[ApiController]
[Route("{id}/xmltv.php")]
public class EpgProxyController(IApiService apiService, ILogger<PlayerApiProxyController> logger) : ControllerBase
{
    [HttpGet(Name = "get_xmltv.php")]
    public async Task<IActionResult> Get(
        string id,
        [FromQuery] string username, 
        [FromQuery] string password)
    {
        try
        {
            var response = await apiService.DoEpgApiCall(id, username, password);
            return Content(Encoding.UTF8.GetString(response), "text/xml");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching API response.");
            return StatusCode(500, "Internal server error");
        }
    }
}
