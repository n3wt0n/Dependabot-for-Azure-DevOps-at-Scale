# Dependabot for Azure DevOps at Scale

This project allows you run GitHub Dependabot on an Azure DevOps repository

## How it works

![Main Flow](/assets/Main_Flow.jpg)

![ACI Orchestrator Flow](/assets/ACI_Orchestration_Flow.jpg)

## Container

To support this, a modified version of the [Dependabot Script container](https://github.com/dependabot/dependabot-script) is used.

It takes the original, and add the components needed to check the execution of the job and report back to the orchestrator.

![Main Flow](/assets/Container_Structure.jpg)