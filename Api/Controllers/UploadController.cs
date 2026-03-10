/*
purpose: handles manual file uploads (csv, json) from the react frontend.
reps the manual file input -> outlier detection & formatting connection
what will be added later:
- parse the uploaded file into a list of RawTelemetry objects
- sends them to the pipeline (probs through pipelineOrchestrator.ProcessAsync())
- returns a success response
*/

using Microsoft.AspNetCore.Mvc;
using System; //for exceptions
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Http; //gives us IFormFile for file uploads

namespace COMP702_WindTurbine.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file) //reads the uploaded file from the form data
        {
            //will later parse the file, create RawTelemetry objects and send them to the processing pipeline
            throw new NotImplementedException();
        }
    }
}