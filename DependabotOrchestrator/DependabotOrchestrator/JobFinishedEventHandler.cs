using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Threading.Tasks;

namespace DependabotOrchestrator
{
    public static class JobFinishedEventHandler
    {
        [FunctionName("JobFinishedEventHandler")]
        public static async Task<IActionResult> EventHandler(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jobfinished/{instanceid}")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,            
            string instanceid)
        {                       
            await client.RaiseEventAsync(instanceid, "Job_Finished");
            return new OkResult();
        }
    }
}
