using System;
using System.Collections.Generic;
using System.Text;

namespace DependabotOrchestrator.Model
{
    public class Constants
    {
        public const string DefaultContainerGroupName = "DependabotRunner";
        public const string ContainerImageName = "n3wt0n/dependabot-azuredevops-atscale";
        public const string TestContainerImageName = "n3wt0n/dependabot-azuredevops-atscale-testimage";
        public const string DefaultContainerImageTag = "latest";
        public const int DefaultMaxParallelism = 3;

        public const string JobCompleteFunctionUrl = "http://localhost:7071/api/jobfinished";
    }
}
