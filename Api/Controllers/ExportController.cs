/*
purpose: provides an endpoint to download the whole database. this is a convenience for administrator users
what will be added later:
- generate a database dump (e.g. sql script or csv files) and return it as a file download
*/

using Microsoft.AspNetCore.Mvc; //for controller base & attributes
using System; //for exceptions
using System.Threading.Tasks; //for async

namespace COMP702_WindTurbine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        [HttpGet("database")]
        public async Task<IActionResult> ExportDatabase()
        {
            //placeholder - later will produce a file (sql dump or csv) and return it
            throw new NotImplementedException();
        }
    }
}