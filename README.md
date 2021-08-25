# Dependabot for Azure DevOps at Scale

This project allows you run GitHub Dependabot on an Azure DevOps repository

## How it works

![Main Flow](/assets/Main_Flow.jpg)

![ACI Orchestrator Flow](/assets/ACI_Orchestration_Flow.jpg)

## Container

To support this, a modified version of the [Dependabot Script container](https://github.com/dependabot/dependabot-script) is used.

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
