using DependabotOrchestrator.Extensions;
using System;
using System.Linq;

namespace DependabotOrchestrator.Model
{
    public class DependabotSource
    {
        private readonly string[] AzDoHosts = { "dev.azure.com", ".visualstudio.com" };

        public Uri RepoUri { get; set; }
        public PackageManagerType PackageManager { get; set; }
        public string DependencyPath { get; set; }
        public string Branch { get; set; }
        public string PullRequestAssignee { get; set; }

        /*VIEWMODEL PROPERTIES*/

        public string InstanceID { get; set; }

        public string ProjectPath
            => AzDoHosts.Any(s => RepoUri.ToString().Contains(s)) 
                ? RepoUri.AbsolutePath.TrimStart('/')
                : ProjectPath;

        public string RepoName
            => ProjectPath.Substring(ProjectPath.LastIndexOf('/') + 1);
    }
}
