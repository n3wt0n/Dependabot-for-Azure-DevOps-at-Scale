using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using DependabotOrchestrator.Model;
using DependabotOrchestrator.Extensions;
using System.Threading;
using DependabotOrchestrator.Managers;

namespace DependabotOrchestrator
{
    public static class DependabotOrchestrator
    {

        [FunctionName("Orchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
           [DurableClient] IDurableOrchestrationClient starter,
           ILogger logger)
        {
            var sources = JsonConvert.DeserializeObject<List<DependabotSource>>(await req.Content.ReadAsStringAsync());
            string instanceId = await starter.StartNewAsync("Orchestrator", sources);

            logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Orchestrator")]
        public static async Task Orchestrator([OrchestrationTrigger] IDurableOrchestrationContext context,
           ILogger logger)
        {
            Settings.Init(logger);

            var maxParallelism = Settings.MaxParallelism;

            var sources = context.GetInput<List<DependabotSource>>();

            var parallelTasks = new HashSet<Task>();
            foreach (var source in sources)
            {
                if (parallelTasks.Count >= maxParallelism)
                {
                    Task finished = await Task.WhenAny(parallelTasks);
                    parallelTasks.Remove(finished);
                }

                //parallelTasks.Add(context.CallActivityAsync(functionName, item));
                parallelTasks.Add(context.CallSubOrchestratorAsync("ACILifecycleOrchestrator", source));
            }

            await Task.WhenAll(parallelTasks);
        }

        [FunctionName("ACILifecycleOrchestrator")]
        public static async Task<List<string>> ACILifecycleOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            var source = context.GetInput<DependabotSource>();

            source.InstanceID = context.InstanceId;
            
            var containerGroupName = await context.CallActivityAsync<string>("CreateACIGroup", source);

            // This activity function calls into the container. scenarios could be check some status, or do something specifically by calling out api endpoint 
            if (await context.CallActivityAsync<bool>("CheckExecution", containerGroupName))
            {
                //Wait for the Job Finished event from the ACI container once its done with its job, or for a timeoute of 1h
                using (var timeoutCts = new CancellationTokenSource())
                {
                    // The job has 60 minutes to complete
                    DateTime expiration = context.CurrentUtcDateTime.AddMinutes(60);
                    Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

                    Task jobCompletedTask = context.WaitForExternalEvent("Job_Finished");

                    Task winner = await Task.WhenAny(jobCompletedTask, timeoutTask);
                    if (winner == jobCompletedTask)
                    {
                        //It worked! Now?
                    }

                    if (!timeoutTask.IsCompleted)
                        timeoutCts.Cancel();        // All pending timers must be complete or canceled before the function exits.
                }

                //This function deletes the ACI group once its done with its job or the timeout expired
                await context.CallActivityAsync<string>("DeleteACIGroup", containerGroupName);
            }
            
            //TODO: set this properly
            return outputs;
        }

        [FunctionName("CreateACIGroup")]
        public static async Task<string> CreateAciGroup([ActivityTrigger] DependabotSource source, ILogger logger)
        {
            logger.LogInformation($"Start Creating ContainerGroup");

            return await AzureManager.CreateContainerGroupAsync(source, logger);
        }

        [FunctionName("CheckExecution")]
        public static bool CheckExecution([ActivityTrigger] string containerGroupName, ILogger log)
        {
            //TODO

            return true;
        }

        [FunctionName("DeleteACIGroup")]
        public static async Task Orchestrator_Delete_ACI_Group([ActivityTrigger] string containerGroupName, ILogger logger)
        {
            logger.LogInformation($"Start Deleting ContainerGroup {containerGroupName}.");

            await AzureManager.DeleteContainerGroupAsync(containerGroupName, logger);
        }

        
    }
}
