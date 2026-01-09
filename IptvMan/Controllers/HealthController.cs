using Microsoft.AspNetCore.Mvc;

namespace IptvMan.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("memory")]
    public IActionResult GetMemoryStats()
    {
        var gcInfo = GC.GetGCMemoryInfo();
        var totalMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);
        var workingSet = Environment.WorkingSet;

        var stats = new
        {
            timestamp = DateTime.UtcNow,
            memory = new
            {
                workingSetMB = Math.Round(workingSet / 1024.0 / 1024.0, 2),
                gcHeapSizeMB = Math.Round(gcInfo.HeapSizeBytes / 1024.0 / 1024.0, 2),
                totalAllocatedMB = Math.Round(totalMemoryBytes / 1024.0 / 1024.0, 2),
                fragmentedBytesMB = Math.Round(gcInfo.FragmentedBytes / 1024.0 / 1024.0, 2)
            },
            gc = new
            {
                gen0Collections = GC.CollectionCount(0),
                gen1Collections = GC.CollectionCount(1),
                gen2Collections = GC.CollectionCount(2)
            }
        };

        return Ok(stats);
    }
}
