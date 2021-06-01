using System;
using System.Collections.Generic;
using System.Text;

namespace DependabotOrchestrator.Model
{
    public class Constants
    {
        public const string DefaultContainerGroupName = "DependabotACIRunner";
        public const string ContainerImageName = "n3wt0n/dependabot-azuredevops-atscale";
        public const string DefaultContainerImageTag = "latest";
        public const int DefaultMaxParallelism = 3;
    }
}
