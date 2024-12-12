using Blazing.Client.Modules.Import;
using Microsoft.AspNetCore.Mvc;

namespace Blazing.Modules.Import;

[ApiController]
[Route("api/import")]
public class ImportController(IImportService importService) : Controller
{
    [HttpGet("load")]
    public IActionResult Index()
    {
        var sw = importService.Import();
        return Ok("Imported! - time: " + (sw) + "ms");            
    }
}

public class ImportServiceApi(IImportService importService) : IImportRestApi
{
    public Task<string> Import()
    {
        var sw = importService.Import();
        return Task.FromResult($"Imported! - time: {sw}ms");            
    }
}