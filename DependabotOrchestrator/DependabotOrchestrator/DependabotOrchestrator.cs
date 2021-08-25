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

            //This should return url for the api
            var ipAddress = await context.CallActivityAsync<string>("CreateACIGroup", source);
            // This activity function calls into the container. scenarios could be check some status, or do something specifically by calling out api endpoint 
            await context.CallActivityAsync<string>("CheckExecution", (ipAddress, source));
            //Return Boolean, this will be invoked from the ACI container once its done with its job
            await context.WaitForExternalEvent("Job_Finished");
            //This activition function delete the ACI group once its done with its job
            await context.CallActivityAsync<string>("DeleteACIGroup", "customer1");


            // Replace "hello" with the name of your Durable Activity Function.

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("CreateACIGroup")]
        public static async Task<string> CreateAciGroup([ActivityTrigger] DependabotSource source, ILogger logger)
        {
            var creds = new AzureCredentialsFactory().FromServicePrincipal(Settings.ServicePrincipalClientID, Settings.ServicePrincipalClientSecret, Settings.ServicePrincipalTenantID, AzureEnvironment.AzureGlobalCloud);
            var azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(Settings.SubscriptionID);
            
            return await CreateContainerGroup(azure, Settings.ResourceGroupName, $"{Settings.ContainerGroupName}-{source.RepoName}-{source.PackageManager.Name()}".ToLower(), Settings.FullContainerImageName, source);
        }

        /// <summary>
        /// Creates a container group with a single container.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group in which to create the container group.</param>
        /// <param name="containerGroupName">The name of the container group to create.</param>
        /// <param name="containerImage">The container image name and tag, for example 'microsoft\aci-helloworld:latest'.</param>
        /// <param name="instanceId"></param>
        private static async Task<string> CreateContainerGroup(IAzure azure,
            string resourceGroupName,
            string containerGroupName,
            string containerImage,
            DependabotSource source)
        {
            //Add a random number to the ContainerGroup name to avoid conflicts
            containerGroupName += DateTime.Now.Millisecond;

            Console.WriteLine($"\nCreating container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = await azure.ResourceGroups.GetByNameAsync(resourceGroupName);
            Region azureRegion = resGroup.Region;

            var environmentVariables = new Dictionary<string, string>
            {
                { "DIRECTORY_PATH", source.DependencyPath },
                { "PACKAGE_MANAGER", source.PackageManager.Name() },
                { "PROJECT_PATH", source.ProjectPath },
                { "AZURE_ACCESS_TOKEN", Settings.AzureDevOpsAccessToken }
            };

            if(!string.IsNullOrWhiteSpace(source.Branch))
                environmentVariables.Add("BRANCH", source.Branch);
            
            if (!string.IsNullOrWhiteSpace(source.PullRequestAssignee))
                environmentVariables.Add("PULL_REQUEST_ASSIGNEE", source.PullRequestAssignee);

            if (!string.IsNullOrWhiteSpace(Settings.GitHubAccessToken))
                environmentVariables.Add("GITHUB_ACCESS_TOKEN", Settings.GitHubAccessToken);

            // Create the container group
            try
            {
                var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                //.WithPrivateImageRegistry(Environment.GetEnvironmentVariable("a"), Environment.GetEnvironmentVariable("b"), Environment.GetEnvironmentVariable("c"))
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName)
                    .WithImage(containerImage)
                    .WithExternalTcpPort(80)
                    .WithCpuCoreCount(1.0)
                    .WithMemorySizeInGB(1)
                    .WithEnvironmentVariables(environmentVariables)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();

                Console.WriteLine($"Once DNS has propagated, container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
                return containerGroup.IPAddress;
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        [FunctionName("CheckExecution")]
        public static string CheckExecution([ActivityTrigger] Tuple<string, string> args, ILogger log)
        {
            //using (HttpClient client = new HttpClient())
            //{
            //    var content = new StringContent(args.Item2);

            //    client.PostAsync(args.Item1, content);
            //}

            //TODO: and here?

            return $"Hello !";
        }

        [FunctionName("DeleteACIGroup")]
        public static string Orchestrator_Delete_ACI_Group([ActivityTrigger] string name, ILogger log)
        {
            var creds = new AzureCredentialsFactory().FromServicePrincipal(Environment.GetEnvironmentVariable("client"), Environment.GetEnvironmentVariable("key"), Environment.GetEnvironmentVariable("tenant"), AzureEnvironment.AzureGlobalCloud);
            var azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(Environment.GetEnvironmentVariable("subscriptionId"));
            DeleteContainerGroup(azure, "azure-poc-rg", "extractor" + name);

            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        /// <summary>
        /// Deletes the specified container group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the container group.</param>
        /// <param name="containerGroupName">The name of the container group to delete.</param>
        private static void DeleteContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName)
        {
            IContainerGroup containerGroup = null;

            while (containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine($"Deleting container group '{containerGroupName}'...");

            azure.ContainerGroups.DeleteById(containerGroup.Id);
        }
    }
}
