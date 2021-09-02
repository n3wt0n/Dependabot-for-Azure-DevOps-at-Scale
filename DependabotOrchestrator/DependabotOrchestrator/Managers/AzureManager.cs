using DependabotOrchestrator.Extensions;
using DependabotOrchestrator.Model;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DependabotOrchestrator.Managers
{
    public static class AzureManager
    {
        private static IAzure GetAzureClient()
        {
            var creds = new AzureCredentialsFactory().FromServicePrincipal(Settings.ServicePrincipalClientID, Settings.ServicePrincipalClientSecret, Settings.ServicePrincipalTenantID, AzureEnvironment.AzureGlobalCloud);
            return Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds).WithSubscription(Settings.SubscriptionID);
        }

        /// <summary>
        /// Creates a container group with a single container.
        /// </summary>
        /// <param name="source">DependabotSource object</param>
        /// <param name="logger"></param>
        /// <returns>Container Group Name</returns>        
        public static async Task<string> CreateContainerGroupAsync(DependabotSource source, ILogger logger)
        {
            var azure = GetAzureClient();

            //Add a random number to the ContainerGroup name to avoid conflicts
            var containerGroupName = $"{Settings.ContainerGroupName}-{source.RepoName}-{source.PackageManager.Name()}{DateTime.Now.Millisecond}".ToLower();

            logger.LogInformation($"\nCreating container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = await azure.ResourceGroups.GetByNameAsync(Settings.ResourceGroupName);
            Region azureRegion = resGroup.Region;

            var environmentVariables = new Dictionary<string, string>
            {
                { "DIRECTORY_PATH", source.DependencyPath },
                { "PACKAGE_MANAGER", source.PackageManager.Name() },
                { "PROJECT_PATH", source.ProjectPath },
                { "AZURE_ACCESS_TOKEN", Settings.AzureDevOpsAccessToken },
                { "ORCHESTRATOR_INSTANCE_ID", source.InstanceID },
                { "JOBCOMPLETE_FUNCTION_URL", $"{Settings.FunctionsBaseUrl}/jobfinished" }
            };

            if (!string.IsNullOrWhiteSpace(source.Branch))
                environmentVariables.Add("BRANCH", source.Branch);

            if (!string.IsNullOrWhiteSpace(source.PullRequestAssignee))
                environmentVariables.Add("PULL_REQUEST_ASSIGNEE", source.PullRequestAssignee);

            if (!string.IsNullOrWhiteSpace(Settings.GitHubAccessToken))
                environmentVariables.Add("GITHUB_ACCESS_TOKEN", Settings.GitHubAccessToken);

            // Create the container group
            try
            {
                var containerGroup = await azure.ContainerGroups.Define(containerGroupName)
                                            .WithRegion(azureRegion)
                                            .WithExistingResourceGroup(resGroup.Name)
                                            .WithLinux()
                                            //.WithPrivateImageRegistry(Environment.GetEnvironmentVariable("a"), Environment.GetEnvironmentVariable("b"), Environment.GetEnvironmentVariable("c"))
                                            .WithPublicImageRegistryOnly()
                                            .WithoutVolume()
                                            .DefineContainerInstance(containerGroupName)
                                                .WithImage(Settings.FullContainerImageName)
                                                .WithExternalTcpPort(80)
                                                .WithCpuCoreCount(1.0)
                                                .WithMemorySizeInGB(1)
                                                .WithEnvironmentVariables(environmentVariables)
                                                .Attach()
                                            .WithDnsPrefix(containerGroupName)
                .CreateAsync();

                logger.LogInformation($"\nCreation of container group '{containerGroupName}' completed!");
                logger.LogInformation($"Once DNS has propagated, container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
                return containerGroup.Name;                
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        /// <summary>
        /// Deletes the specified container group.
        /// </summary>
        /// <param name="containerGroupName">The name of the container group to delete.</param>
        /// <param name="logger"></param>
        public static async Task DeleteContainerGroupAsync(string containerGroupName, ILogger logger)
        {
            var azure = GetAzureClient();

            IContainerGroup containerGroup = null;

            while (containerGroup == null)
            {
                containerGroup = await azure.ContainerGroups.GetByResourceGroupAsync(Settings.ResourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            logger.LogInformation($"Deleting container group '{containerGroupName}'...");

            await azure.ContainerGroups.DeleteByIdAsync(containerGroup.Id);
        }
    }
}
