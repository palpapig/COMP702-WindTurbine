/*
purpose: provides 2 endpoints: one to read the current configuration (key-value pairs) and one to update it. 
these correspond to the "interface <-> database(configuration)" arrow in our architecture
what will be added later:
- get: call a method that reads all key-value configuration entries from the database and return the dictionary. the UI will display these so users can see current settings
- put: call a mehod that replaces the entire config table with a new set of key-value pairs. this is called when the user saves changes in the config page
*/
using Microsoft.AspNetCore.Mvc; //mvc attributes & base classes
using System; //for exceptions & general types
using System.Collections.Generic; //for dictionary
using System.Threading.Tasks; //for async methods

namespace COMP702_WindTurbine.Api.Controllers
{
    [Route("api/[controller]")] //route becomes /api/configuration
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        [HttpGet] //handles GET requests to /api/configuration
        public async Task<IActionResult> GetConfiguration()
        {
            //will later fetch all config entries from the database via the data accessor
            throw new NotImplementedException();
        }

        [HttpPut] //handles PUT requests to 
        public async Task<IActionResult> UpdateConfiguration([FromBody] Dictionary<string, string> config) //reads the json body into a dictionary
        {
            //will later pass the updated config to the data accessor to save in the database
            throw new NotImplementedException();
        }
    }
}