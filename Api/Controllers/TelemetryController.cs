/*
purpose: provides historical telemetry data for a specific turbine, used by the react frontend to draw graphs. 
this represents the "database -> interface (historical trends)" connection in architecture
what will be added later:
- call dataAccessor.GetRawTelemetryForTurbineAsync() or GetCleanedTelemetryForTurbineAsync() depending on requirements
- return the data as json
*/
using Microsoft.AspNetCore.Mvc;
using System; //for datetime & exception
using System.Threading.Tasks; //for async

namespace COMP702_WindTurbine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        [HttpGet("{turbineId}")]
        public async Task<IActionResult> GetTelemetry(string turbineId, DateTime? from = null, DateTime? to = null)
        {
            //will later fetch row or cleaned telemetry for the given turbine & time range
            throw new NotImplementedException();
        }
    }
}