namespace DependabotOrchestrator.Model
{
    public class Constants
    {
        public const string DefaultContainerGroupName = "dpbrunner";
        public const string ContainerImageName = "n3wt0n/dependabot-azuredevops-atscale";
        public const string TestContainerImageName = "n3wt0n/dependabot-azuredevops-atscale-testimage";
        public const string DefaultContainerImageTag = "latest";
        public const int DefaultMaxParallelism = 3;       
    }
}
