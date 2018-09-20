## Background
There are scenarios where you want to automate deployment or operations on a subscription using a non-interactive service or background job, e.g. to create or manage storage accounts or other Azure resources, or add or remove role assignments. 

The Azure portal has an **Automation scripts** tab for every resource, that shows how to deploy that resource using an ARM template using PowerShell, Azure CLI, .NET etc. 

The .NET code there uses an Azure AD Service Principal. There are two issues with it:
1. The Azure AD application credentials are hard coded in the source code. Developers tend to push the code to source repositories as-is, which leads to credentials in source. 
2. The Azure AD application credentials expire, and so need to be renewed, else can lead to application downtime.

With [Managed Service Identity (MSI)](https://docs.microsoft.com/en-us/azure/active-directory/msi-overview), both these problems are solved. This sample is a slight modification of the C# code available in the **Automation scripts** on the portal. 
It uses MSI, instead of an explicitly created service principal, to deploy resources, so you do not need to create or renew app credentials. 

>Here's another sample that shows how to fetch a secret from Azure Key Vault at run-time from an App Service with a Managed Service Identity (MSI) - [https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet/](https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet/)

>Here's another .NET Core sample that shows how to programmatically call Azure Services from an Azure Linux VM with a Managed Service Identity (MSI). - [https://github.com/Azure-Samples/linuxvm-msi-keyvault-arm-dotnet](https://github.com/Azure-Samples/linuxvm-msi-keyvault-arm-dotnet)


## Prerequisites
To run and deploy this sample, you need the following:
1. Azure subscription to create an Azure VM with MSI. 
2. [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) to run the application on your local development machine.

## Step 1: Create an Azure VM with a Managed Service Identity (MSI) 
<a href="https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fwindowsvm-msi-arm-dotnet%2Fmaster%2Fazuredeploy.json" target="_blank">
    <img src="http://azuredeploy.net/deploybutton.png"/>
</a>

Use the "Deploy to Azure" button to deploy an ARM template to create an Azure VM with a Managed Service Identity. When you create a VM with MSI, an Azure AD service principal with the same name is created, and can be used to grant access to resources. 

## Step 2: Grant the Managed Service Identity "contributor" access to your subscription
Using the Azure Portal, grant the Managed Service Identity contributor access to the subscription. You can do this using the **Access Control (IAM)** tab in the subscription. 

Click on **Add**, set the role as **Contributor**, and search for the VM name you just created. 

## Step 3: Clone the repo 
Clone the repo to your development machine. 

The relevant Nuget packages are:
1. Microsoft.Azure.Services.AppAuthentication - makes it easy to fetch access tokens for service to Azure service authentication scenarios. 
2. Microsoft.Azure.Management.ResourceManager - contains methods for interacting with Azure Resource Manager. 

The relevant code is in DeploymentHelper.cs file. The AzureServiceTokenProvider class (which is part of Microsoft.Azure.Services.AppAuthentication) tries the following methods to get an access token, to call ARM:-
1. Managed Service Identity (MSI) - for scenarios where the code is deployed to Azure, and the Azure resource supports MSI. 
2. [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) (for local development) - Azure CLI version 2.0.12 and above supports the **get-access-token** option. AzureServiceTokenProvider uses this option to get an access token for local development. 
3. Active Directory Integrated Authentication (for local development). To use integrated Windows authentication, your domain’s Active Directory must be federated with Azure Active Directory. Your application must be running on a domain-joined machine under a user’s domain credentials.

```csharp    
AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

var serviceCreds = new TokenCredentials(await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/").ConfigureAwait(false));

var resourceManagementClient = new ResourceManagementClient(serviceCreds);
```

## Step 4: Change the subscription ID, storage account name, and other parameters
1. In the DeploymentHelper.cs file, change the **subscriptionId** to your own subscription ID, and optionally make changes to resource group name, location, etc. as required. 
2. In deploymentParameters.json, change the **storageAccountName**. This is important since the storage account name must be unique, and so the sample may fail later if the name is already taken.

## Step 5: Run the application on your local development machine
Since this is on the development machine, AzureServiceTokenProvider will use the developer's security context to get a token to authenticate to ARM. This removes the need to create a service principal, and share it with the development team. It also prevents credentials from being checked in to source code. 
AzureServiceTokenProvider will use **Azure CLI** or **Active Directory Integrated Authentication** to authenticate to Azure AD to get a token.  

Azure CLI will work if the following conditions are met:
 1. You have [Azure CLI 2.0](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) installed. Version 2.0.12 supports the get-access-token option used by AzureServiceTokenProvider. If you have an earlier version, please upgrade. 
 2. You are logged into Azure CLI. You can login using **az login** command.
 
Azure Active Directory Authentication will only work if the following conditions are met:
 1. Your on-premise active directory is synced with Azure AD. 
 2. You are running this code on a domain joined machine.   

Since your developer account has access to the subscription, the resource group, and storage account should be created. 

You can also use a service principal to run the application on your local development machine. See the section "Running the application using a service principal" later in the tutorial on how to do this. 

## Step 6: Deploy the application to the Azure VM

In the Azure Portal, browse to the Azure VM you created, and click on "Connect". RDP into the Azure VM, and copy the build output to a folder on the VM. 

Run **DeployArmTemplate.exe**. It will run the same code that was run on the local development machine, but will use the Managed Service Identity, instead of your developer context, to create/ update the resource. 

## Summary
You were successfully able to write an application that can deploy ARM resources without explicitly creating a service principal credential. 

## Troubleshooting

### Common issues during local development:

1. Azure CLI is not installed, or you are not logged in, or you do not have the latest version. 
Run **az account get-access-token** to see if Azure CLI shows a token for you. If it says no such program found, please install Azure CLI 2.0. If you have installed it, you may be prompted to login. 

2. AzureServiceTokenProvider cannot find the path for Azure CLI.
AzureServiceTokenProvider finds Azure CLI at its default install locations. If it cannot find Azure CLI, please set environment variable **AzureCLIPath** to the Azure CLI installation folder. AzureServiceTokenProvider will add the environment variable to the Path environment variable.

### Common issues across environments:

1. Access denied (Forbidden)

The principal used does not have access to the subscription. Grant the MSI contributor access to the subscription.

## Running the application using a service principal in local development environment

>Note: It is recommended to use your developer context for local development, since you do not need to create or share a service principal for that. If that does not work for you, you can use a service principal, but do not check in the certificate or secret in source repos, and share them securely.

To run the application using a service principal in the local development environment, follow these steps

Service principal using a certificate:
1. Create a service principal certificate. Follow steps [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authenticate-service-principal) to create a service principal. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=_AppId_;TenantId=_TenantId_;CertificateThumbprint=_Thumbprint_;CertificateStoreLocation=_CurrentUser_**. 
You need to replace AppId, TenantId, and Thumbprint with actual values from step #1.
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the certificate to authenticate to Azure AD. 

Service principal using a password:
1. Create a service principal with a password. Follow steps [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-authenticate-service-principal) to create a service principal and grant it permissions to the Key Vault. 
2. Set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=_AppId_;TenantId=_TenantId_;AppKey=_Secret_**. You need to replace AppId, TenantId, and Secret with actual values from step #1. 
3. Run the application in your local development environment. No code change is required. AzureServiceTokenProvider will use this environment variable and use the service principal to authenticate to Azure AD. 

## Running the application using a user-assigned managed identity in Azure VM

To run the application using a user-assigned managed identity while deployed to an Azure VM, follow these steps:

1. Create a user-assigned managed identity. Follow steps [here](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/how-to-manage-ua-identity-portal#create-a-user-assigned-managed-identity) to create a user-assigned managed identity.
2. After creating the managed identity, record the Client ID of the newly created managed identity.
3. Assign the user-assigned managed identity to your Azure VM. Follow steps [here](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/qs-configure-portal-windows-vm#user-assigned-managed-identity) to assign the identity to the VM.
4. While in your Azure VM, set an environment variable named **AzureServicesAuthConnectionString** to **RunAs=App;AppId=_AppId_;TenantId=_TenantId_**. You need to replace AppId with the value of the Client ID you recorded in step #2 and TenantId with your Tenant ID. Follow steps [here](https://docs.microsoft.com/en-us/onedrive/find-your-office-365-tenant-id) to find your Tenant ID.
5. Run the application in your Azure VM. No code change is required. AzureServiceTokenProvider will use this environment variable and use the user-assigned managed identity to authenticate to Azure AD. 
