## Microsoft Orleans cluster on Azure-Container Apps advanced

### This solution uses GitHub Actions based CI/CD. For Azure Pipelines samples please [see this repository](https://github.com/Async-Hub/Orleans-Cluster-on-Azure-Container-Apps-Advanced).

This is the modernized and up to date version of  sample provided by Microsoft ([Orleans Cluster on Azure Container Apps](https://github.com/Azure-Samples/Orleans-Cluster-on-Azure-Container-Apps)). It uses:

* Fully automated GitHub Actions based CI/CD to deploy from code to cloud with minimal effort
* Blazor Server model which works on .NET 8 and Microsoft Orleans 8
* Scalable server-side Blazor app on Azure Container Apps
* Azure SignalR Service, Azure Key Vault, Azure Storage Account, Azure Application Insights, Azure Load Testing, Microsoft Playwright for E2E Tests, and many more

What is required for this solution?  
> - Microsoft Azure Subscription, [you can create a free account](https://azure.microsoft.com/en-us/free/) if you don't have any.

### 1. Create an GitHub repo for the solution.

1. Open GitHub portal : https://github.com/new/import
2. Put the URL for your source repository: https://github.com/Async-Hub/Orleans-Cluster-on-Azure-Container-Apps-Advanced-GitHub.git
3. Repository name for example: **orleans-on-ctap1**
4. Chouse **Public** repository type radio button.
   
### 2. Install Docker Desktop on your machine.

Install [Docker Desktop](https://docs.docker.com/desktop/install/windows-install/) on operation system which you are using.  

>Attention! The steps outlined below, up to section 3, are optional

If you are using Windows 11 or Windows 10 it is more appropriate to use WSL 2 and install Docker Desktop on Ubuntu-22.04. Here 
we have two options:

- The first option is to use WSL 2 on the host operationg system.
- The second option is to keep the host operating system clean and create a Windows 10/11 Virtual Machine with Hyper-V using [Nested Virtualization](https://learn.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/nested-virtualization). What are required for *Nested Virtualization*?  
> - [Windows machine with virtualization technology (AMD-V / Intel VT-x)](https://learn.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/nested-virtualization)
>   - Windows Server 2016/Windows 10 or greater for Intel processor with VT-x
>   - Windows Server 2022/Windows 11 or greater AMD EPYC/Ryzen processor
> - Enable [nested virtualization](https://learn.microsoft.com/en-us/windows/wsl/faq#can-i-run-wsl-2-in-a-virtual-machine-) on the Virtual Machine

Regardless of the option you chose above, you need to open a terminal on the host (for the first option) or on the VM (for the second option). Then enable WSL 2 and install Ubuntu 22.04 with the following command, typing it in the terminal window:
```
wsl --install -d Ubuntu-22.04
```
Install [Docker Engine](https://docs.docker.com/engine/install/ubuntu/) on Ubuntu.
```
$ sudo apt-get update
$ sudo apt-get install ca-certificates curl
$ sudo install -m 0755 -d /etc/apt/keyrings
$ sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
$ sudo chmod a+r /etc/apt/keyrings/docker.asc
$ echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
$ sudo apt-get update
$ sudo apt-get install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```
Start Docker service.
```
sudo service docker start
sudo service docker status
```
You can clone the solution you have imported https://github.com/Replace_This_With_Org_or_User_Name/orleans-on-ctap1.git into C:\ drive on your Windows machine ([Git CLI](https://git-scm.com/download/win) is required)
```
mkdir C:\Repos
cd C:\Repos
git clone https://github.com/Replace_This_With_Org_or_User_Name/orleans-on-ctap1.git
```
and interop with it from Ubuntu-22.04 by the following way:
```
$ cd /mnt/c/Repos/orleans-on-ctap1/build/azure-pipelines-agents/debian-12.2/
$ sudo docker build -t azure-pipelines-agents-debian-12.2:11072024 .
```

### 3. Create a self-hosted runner.

Build a runner docker image by using files from "build\self-hosted-runners" based on Debian image
```
$ sudo docker build -t github-actions-runner-debian-12.2:23072024 .
```
or on Playwright image.
```
$ sudo docker build -t github-actions-runner-playwright-1.x:1.45.0.23072024 .
```
Create [Fine-grained personal access token](https://github.com/settings/tokens). Or if you use an organization please 
install [GitHub CLI](https://cli.github.com/) and [use the following script](https://docs.github.com/en/rest/actions/self-hosted-runners?apiVersion=2022-11-28#create-a-registration-token-for-an-organization):
PowerShell
```
gh auth login -h github.com -s admin:org
gh api --method POST -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28" `
  /orgs/PUT_YOUR_ORG_NAME_HERE/actions/runners/registration-token
```
Bash
```
gh api --method POST -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28" \
  /orgs/PUT_YOUR_ORG_NAME_HERE/actions/runners/registration-token
```
Run Debian or Playwright based runner by using the following command:
```
sudo docker run -v /var/run/docker.sock:/var/run/docker.sock \
    -e GITHUB_ORG_URL=https://github.com/<organization name> \
    -e GITHUB_RUNNER_NAME=01_Debian-12.2 \
    -e GITHUB_ORG_TOKEN=<TOKEN> --name 01_Debian-12.2 github-actions-runner-debian-12.2:23072024
```
The syntax above uses PowerShell. If you use Bash shell, just replace "`" (backtick) with "\\" (backslash).  
  
>Warning! Doing Docker within a Docker by using Docker socket has serious security implications. The code inside the container can now run as root on your Docker host. Please be very careful.

### 4. Use GitHub Actions to connect to Azure.

Use your existing Microsoft Azure Subscription, [you can create a free account](https://azure.microsoft.com/en-us/free/) if you don't have any.  
By using Azure CLI create a service principal and configure its access to Azure resources. To retrieve current subscription ID, run:  
```
az account show --query id --output tsv
```
Configure its access to Azure subscription:
```
az ad sp create-for-rbac --name orleans-on-ctap1 --role Owner --scopes /subscriptions/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```
Copy the JSON object for your service principal:
```
{
  "appId": "<GUID>",
  "displayName": "orleans-on-ctap1",
  "password": "client secret",
  "tenant": "<GUID>"
}
```
Make a new JSON object with values from the previous object:
```
{
    "clientId": "Replace with appId value",
    "clientSecret": "Replace with password value",
    "subscriptionId": "Put the Azure subscribtion Id here",
    "tenantId": "Replace with tenant value"
}
```
Add the JSON [as a GitHub secret](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure) with replaced values, required name: **AZURE_CREDENTIALS**.