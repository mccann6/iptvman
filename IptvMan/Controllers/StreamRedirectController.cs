using Microsoft.AspNetCore.Mvc;
using IptvMan.Services;

namespace IptvMan.Controllers;

[ApiController]
[Route("{id}/{type}/{username}/{password}/{stream}")]
public class StreamRedirectController(IApiService apiService, ILogger<PlayerApiProxyController> logger) : ControllerBase
{
    [HttpGet(Name = "StreamRedirectGet")]
    public async Task<IActionResult> Get(
        string id,
        string type,
        string username, 
        string password, 
        string stream)
    {
        try
        {
            var url = apiService.GetStreamUrl(id, type, username, password, stream);
            logger.LogInformation("Redirecting {Id} Url call for {Type}/{Stream}", id, type, stream);
            return Redirect(url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "StreamRedirectGet Error occurred while fetching API response.");
            return StatusCode(500, "Internal server error");
        }
    }
}