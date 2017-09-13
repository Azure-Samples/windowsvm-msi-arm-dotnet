using PortalGenerated;

namespace DeployArmTemplate
{
    class Program
    {
        static void Main()
        {
            DeploymentHelper deploymentHelper = new DeploymentHelper();

            deploymentHelper.Run().Wait();
        }
    }
}
