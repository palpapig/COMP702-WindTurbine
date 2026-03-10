/*
purpose: provides endpoints to list all turbines & to add a new turbine.
reps the windows service -> database "add new turbine" in our architecture 
and also to the dashboard's need to show current status
what will be added later:
- get: call a method that pulls together the latest telemetry & analysis for all turbines & return the summary. the dashboard uses this to display a summary card for each turbine
- post: call a method that inserts a new turbine record into the databased. called when an admin adds a turbine through the UI
*/

using Microsoft.AspNetCore.Mvc;
using System; //for exceptions
using System.Threading.Tasks;
using COMP702_WindTurbine.Models; //brings in the Turbine Model (used in the post method)

namespace COMP702_WindTurbine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TurbinesController : ControllerBase
    {
        [HttpGet] //GET all turbines with their latest status
        public async Task<IActionResult> GetTurbines()
        {
            //will later fetch a summary of all turbines (latest telemetry + analysis)
            throw new NotImplementedException();
        }

        [HttpPost] //add a new turbine
        public async Task<IActionResult> AddTurbine([FromBody] Turbine turbine) //reads the turbine data from the request body
        {
            //will later call dataAccessor.AddTurbineAsync(turbine)
            throw new NotImplementedException();
        }
    }
}