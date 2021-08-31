# Dependabot for Azure DevOps at Scale

This project allows you to run [GitHub Dependabot](https://docs.github.com/en/code-security/supply-chain-security/managing-vulnerabilities-in-your-projects-dependencies/about-alerts-for-vulnerable-dependencies) to scan Azure DevOps repositories, via Azure Pipelines, thanks to Azure Functions.

## Current Status: _Development In Progress_

Component                               | Status| Notes
:-----                                  | :-----| :-----
__Orchestrator Trigger__                | 100%  |
__Main Orchestrator__                   | 90%   |
__ACI Orchestrator__                    | 90%   |
__ACI - Creation__                      | 100%  | Currently pulling only from public registry
__ACI - Check Status__                  | 0%    |
__ACI - Event Handler__                 | 80%   |
__ACI - Deletion__                      | 100%  |
__In-container event handler__          | 50%   |
__Container Image - Test image__        | 100%  |
__Container Image - Production Image__  | 90%   | Huge size, Missing automated creation
__Deployment Scripts__                  | 0%    |

## How it works

[Description TBC]

![Main Flow](/assets/Main_Flow.jpg)

[Description TBC]

![ACI Orchestrator Flow](/assets/ACI_Orchestration_Flow.jpg)

### Prerequisites

- PAT on Azure DevOps
- PAT on GitHub
- Service Principal in Azure to create ACI
- Resource Group in Azure

## Container

To support the flow above, a modified version of the [Dependabot Script container](https://github.com/dependabot/dependabot-script) is used.

It takes the original, and add the components needed to check the execution of the job and report back to the orchestrator.

![Main Flow](/assets/Container_Structure.jpg)

The container image is hosted in __Docker Hub__ and it's called [dependabot-azuredevops-atscale](https://hub.docker.com/r/n3wt0n/dependabot-azuredevops-atscale)

If you want to test it out manually:

```bash
docker pull n3wt0n/dependabot-azuredevops-atscale

docker run --rm \
  --env "PROJECT_PATH=organization/project/_git/repo-name" \
  --env "DIRECTORY_PATH=folder/containing/dependencies" \
  --env "BRANCH=branch_to_scan" \
  --env "AZURE_ACCESS_TOKEN=XXX_PAT_XXX" \
  --env "PULL_REQUEST_ASSIGNEE=username" \
  --env "GITHUB_ACCESS_TOKEN=xxx_PAT_xxx" \
  --env "PACKAGE_MANAGER=bundler" \
  n3wt0n/dependabot-azuredevops-atscale
```

### Environment Variables

Variable Name             | Default          | Notes
:------------             | :--------------- | :----
`DIRECTORY_PATH`          | `/`              | Directory where the base dependency files are.
`PACKAGE_MANAGER`         | `bundler`        | Valid values: `bundler`, `cargo`, `composer`, `dep`, `docker`, `elm`,  `go_modules`, `gradle`, `hex`, `maven`, `npm_and_yarn`, `nuget`, `pip` (includes pipenv), `submodules`, `terraform`
`PROJECT_PATH`            | N/A (__Required__) | Path to repository. Format `<organization>/<project>/_git/<repo-name>`.
`BRANCH`                  | N/A (Optional) | Branch to fetch manifest from and open pull requests against.
`PULL_REQUESTS_ASSIGNEE`  | N/A (Optional) | User to assign to the created pull request.
`AZURE_ACCESS_TOKEN`      | N/A (__Required__) | Personal Access Token (PAT) with access to Azure DevOps, with permissions to read the repo content and create pull requests
`GITHUB_ACCESS_TOKEN`     | N/A (Optional) | Personal Access Token (PAT) used just for Authentication purposes `*`

`*` without this token, you may receive errors of request throttling or blocked requests when checking against dependencies hosted on GitHub.
