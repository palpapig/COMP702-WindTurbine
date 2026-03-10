/*
Purpose: this controller provides an api endpoint for the react frontend to fetch active alarms.
Aligning with the architecture, the data accessor will eventually supply a list of active alarms, which this endpoint will return

What will be added later:
- call something to ask the database for a list of alerts that are currently active (status = "Active"). the result will be used by the api to show alarms on the dashboard
- map results to  a suitable dto
- return http 200 with the list of alarms

*/

using Microsoft.AspNetCore.Mvc; //for web apis like [route] and [apicontroller]
using System; //provides basic system types, including NotImplementedExcption
using System.Threading.Tasks; //lets us work with asynchronous methods (async/await)

namespace COMP702_WindTurbine.Api.Controllers 
{
    [Route("api/[controller]")] //sets the base route to /api/alerts (the [controller] part becomes 'alerts')
    [ApiController] //tells asp.net that this class handles api requests & enables automatic model validation
    public class AlertsController : ControllerBase //inherits from ControllerBase to get http response helpers
    {
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAlarms() // returns a task that will eventually produce an http response
        {
            throw new NotImplementedException(); //stops the program from running this stub - this'll be replaced later
        }
    }
}