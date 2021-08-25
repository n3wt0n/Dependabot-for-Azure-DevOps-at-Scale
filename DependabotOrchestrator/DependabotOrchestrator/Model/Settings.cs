using Microsoft.Extensions.Logging;
using System;

namespace DependabotOrchestrator.Model
{
    public class Settings
    {
        private static ILogger _logger;

        public static string SubscriptionID { get; private set; }
        public static string ServicePrincipalClientID { get; private set; }
        public static string ServicePrincipalClientSecret { get; private set; }
        public static string ServicePrincipalTenantID { get; private set; }
        public static string ResourceGroupName { get; private set; }
        public static string ContainerGroupName { get; private set; }
        private static string ContainerImageTag { get; set; }
        public static int MaxParallelism { get; private set; }
        public static string AzureDevOpsAccessToken { get; private set; }
        public static string GitHubAccessToken { get; private set; }
        public static bool UseTestImage { get; private set; }
        public static string FunctionsBaseUrl { get; set; }


        public static string FullContainerImageName { get; private set; }

        public static void Init(ILogger logger)
        {
            _logger = logger;

            SubscriptionID = Environment.GetEnvironmentVariable("SubscriptionID");
            if (string.IsNullOrWhiteSpace(SubscriptionID))
            {
                _logger.LogError("SubscriptionID environment variable is null");
                throw new ArgumentNullException("SubscriptionID environment variable is null");
            }

            ServicePrincipalClientID = Environment.GetEnvironmentVariable("ServicePrincipalClientID");
            if (string.IsNullOrWhiteSpace(ServicePrincipalClientID))
            {
                _logger.LogError("ServicePrincipalClientID environment variable is null");
                throw new ArgumentNullException("ServicePrincipalClientID environment variable is null");
            }

            ServicePrincipalClientSecret = Environment.GetEnvironmentVariable("ServicePrincipalClientSecret");
            if (string.IsNullOrWhiteSpace(ServicePrincipalClientSecret))
            {
                _logger.LogError("ServicePrincipalClientSecret environment variable is null");
                throw new ArgumentNullException("ServicePrincipalClientSecret environment variable is null");
            }

            ServicePrincipalTenantID = Environment.GetEnvironmentVariable("ServicePrincipalTenantID");
            if (string.IsNullOrWhiteSpace(ServicePrincipalTenantID))
            {
                _logger.LogError("ServicePrincipalTenantID environment variable is null");
                throw new ArgumentNullException("ServicePrincipalTenantID environment variable is null");
            }

            ResourceGroupName = Environment.GetEnvironmentVariable("ResourceGroupName");
            if (string.IsNullOrWhiteSpace(ResourceGroupName))
            {
                _logger.LogError("ResourceGroupName environment variable is null");
                throw new ArgumentNullException("ResourceGroupName environment variable is null");
            }

            //ContainerImageTag = Environment.GetEnvironmentVariable("ContainerImageTag");
            //if (string.IsNullOrWhiteSpace(ContainerImageTag))
            //{
            //    _logger.LogWarning("ContainerImageTag environment variable is null. Reverting to default");
                ContainerImageTag = Constants.DefaultContainerImageTag;
            //}

            if (int.TryParse(Environment.GetEnvironmentVariable("MaxParallelism"), out int paralles) && paralles >= 0)
                MaxParallelism = paralles;
            else
            {
                _logger.LogWarning("MaxParallelism environment variable is null or invalid. Reverting to default");
                MaxParallelism = Constants.DefaultMaxParallelism;
            }

            ContainerGroupName = Environment.GetEnvironmentVariable("ContainerGroupName");
            if (string.IsNullOrWhiteSpace(ContainerGroupName))
            {
                _logger.LogWarning("ContainerGroupName environment variable is null. Reverting to default");
                ContainerGroupName = Constants.DefaultContainerGroupName;
            }
            ContainerGroupName = ContainerGroupName.ToLower();

            AzureDevOpsAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsAccessToken");
            if (string.IsNullOrWhiteSpace(AzureDevOpsAccessToken))
            {
                _logger.LogError("AzureDevOpsAccessToken environment variable is null");
                throw new ArgumentNullException("AzureDevOpsAccessToken environment variable is null");
            }

            GitHubAccessToken = Environment.GetEnvironmentVariable("GitHubAccessToken");
            if (string.IsNullOrWhiteSpace(ContainerGroupName))
                _logger.LogWarning("GitHubAccessToken environment variable is null. Won' be used, you may incur in API limits");
            
            bool.TryParse(Environment.GetEnvironmentVariable("UseTestImage"), out bool useTestImage);
            UseTestImage = useTestImage;

            FullContainerImageName = $"{(UseTestImage ? Constants.TestContainerImageName : Constants.ContainerImageName)}:{ContainerImageTag}".ToLower();
        }
    }
}
