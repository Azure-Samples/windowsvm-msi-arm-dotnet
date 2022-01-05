using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using JsonObject = System.Collections.Generic.Dictionary<string, object>;
using System.Collections.Generic;

namespace PortalGenerated
{
    /// <summary>
    /// This is a helper class for deploying an Azure Resource Manager template
    /// More info about template deployments can be found here https://go.microsoft.com/fwLink/?LinkID=733371
    /// </summary>
    class DeploymentHelper
    {
        string subscriptionId = "<<subscriptionid>>";

        // ClientId and ClientSecret are no longer needed!
        // string clientId = "your-service-principal-clientId";
        // string clientSecret = "your-service-principal-client-secret";

        string resourceGroupName = "myrg";
        string deploymentName = "mydeployment";
        Location location = Location.SouthCentralUS;

        public async Task Run()
        {
            // Create the resource manager client
            var armClient = new ArmClient(subscriptionId, new DefaultAzureCredential());
            Subscription subscription = armClient.GetDefaultSubscription();
            ResourceGroupCollection rgCollection = subscription.GetResourceGroups();
            // Create or check that resource group exists
            EnsureResourceGroupExists(rgCollection, resourceGroupName, location);

            ResourceGroup resourceGroup = rgCollection.Get(resourceGroupName).Value;
            DeploymentCollection deploymentCollection = resourceGroup.GetDeployments();
            // Start a deployment
            DeployTemplate(deploymentCollection, resourceGroupName, deploymentName);
        }

        /// <summary>
        /// Ensures that a resource group with the specified name exists. If it does not, will attempt to create one.
        /// </summary>
        /// <param name="rgCollection">A class representing collection of ResourceGroupCollection.</param>
        /// <param name="resourceGroupName">The name of the resource group.</param>
        /// <param name="location">The resource group location. Required when creating a new resource group.</param>
        private static void EnsureResourceGroupExists(ResourceGroupCollection rgCollection, string resourceGroupName, string location)
        {
            if (rgCollection.CheckIfExists(resourceGroupName) != true)
            {
                Console.WriteLine(string.Format("Creating resource group '{0}' in location '{1}'", resourceGroupName, location));
                ResourceGroupCreateOrUpdateOperation lro =  rgCollection.CreateOrUpdate(resourceGroupName, new ResourceGroupData(location));
            }
            else
            {
                Console.WriteLine(string.Format("Using existing resource group '{0}'", resourceGroupName));
            }
        }

        /// <summary>
        /// Starts a template deployment.
        /// </summary>
        /// <param name="deploymentCollection">The collection of deployment.</param>
        /// <param name="resourceGroupName">The name of the resource group.</param>
        /// <param name="deploymentName">The name of the deployment.</param>
        private static void DeployTemplate(DeploymentCollection deploymentCollection, string resourceGroupName, string deploymentName)
        {
            Console.WriteLine(string.Format("Starting template deployment '{0}' in resource group '{1}'", deploymentName, resourceGroupName));
            var input = new DeploymentInput(new DeploymentProperties(DeploymentMode.Incremental)
            {
                TemplateLink = new TemplateLink()
                {
                    Uri = "https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/quickstarts/microsoft.storage/storage-account-create/azuredeploy.json"
                },
                Parameters = new JsonObject()
                {
                    {"storageAccountType", new JsonObject()
                        {
                            {"value", "Standard_GRS" }
                        }
                    }
                }
            });

            DeploymentCreateOrUpdateAtScopeOperation deploymentResult = deploymentCollection.CreateOrUpdate(deploymentName, input);

            Console.WriteLine(string.Format("Deployment status: {0}", deploymentResult.Value.Data.Properties.ProvisioningState));
        }
    }
}