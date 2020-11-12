# HeapKeeper 

Do you reference tickets in other systems using structured text in your Azure DevOps work items?  If so, let Heap Keeper convert those structured text strings into hyperlinks for you.

## TO DO: Demo GIF

## If you trust me, click [here](https://heapkeeper.azurewebsites.net/):

## Host it yourself instructions

**A. Setup Azure CLI on your computer and configure services**

1. Using the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/), login and select the azure subscription which will run HeapKeeper
2. Define a variable for the name of the service: `$name = "heapkeeper"`
3. Confirm that your name is a "good" one for Azure Cosmos DB using `az cosmosdb check-name-exists --name $name`.  If you get `false` you're good to go.
4. Define a variable for the location of the service: `$location = "centralus"`
5. Create the Azure Resource Group: `az group create --location $location --resource-group $name`
6. Create the Azure App Service Plan: `az appservice plan create --name $name --resource-group $name --location $location --sku F1`
7. Create the Azure App Service Webapp: `az webapp create --name $name --resource-group $name --plan $name`.  Make note of the defaultHostName.  You'll use that in a future step
8. Add the [application insights extension](https://docs.microsoft.com/en-us/cli/azure/ext/application-insights/?view=azure-cli-latest) to Azure CLI: `az extension add -name application-insights`
9. Create an Azure App Insights instrumentaiton key: `az monitor app-insights component create --app $name --location $location --resource-group $name`.  In addition, make note of the instrumentation key.  We will store it in a pipeline variable later.
10. Create cosmos db account `az cosmosdb create --name $name --resource-group $name --enable-free-tier true`.  Be patient.  This took ten minutes when I ran this.  Make note of the documentEndpoint.
11. Login to the Azure Portal and find your CosmosDB account.  Navigate to the Keys section on the left and make note of the Primary Key.  You'll need it in a future step.

**B. Setup an Azure DevOps Application and associate your Azure Subscription to to your Azure DevOps Organization**
1. Register an [OAuth client app in Azure DevOps](https://app.vsaex.visualstudio.com/app/register).  The callback URL should be https://WebAppDefaultHostName/signin-azdo.  The scopes should be Work Items Read and Write.
2. Make note of the `AzureAppId` GUID.  We'll use it later
3. Click show on the Client Secret.  Make note of it; you'll store it in a variable in the pipeline.
4. [Create an Azure Resource Manager service connection using automated security](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=azure-devops#create-an-azure-resource-manager-service-connection-using-automated-security)

**C. Configure the application and Azure DevOps Pipeline to deploy**
1. Fork / Clone the repo
2. Push to a new Azure Repo in your Azure DevOps account
3. Add regular expressions to the `Injections` node in the [appsettings.json](/HeapKeeper/appsettings.json) file to meet your needs.  Begin the `RegexToFind` with a negative lookbehind `(?<!>)` which prohibits a match if the string begins with `>` character.  This is important because the linking code creates a HTML link and by using this approach we can avoid an endless loop of comment updates.
4. Create a new pipeline, select Existing Azure Pipelines YAML file, choose /DevOpsPipelines/AzureDevOpsPipeline.yaml
5. Create the following variables in your pipeline

    | Name | Value | Secret? |
    | --- | --- | --- |
    | AppInsightsIKey| GUID from step A-9 | No
    | AzureDevOpsAppId| GUID from step B-2 | No
    | AzureDevOpsClientSecret| Value from step B-3 | Yes
    | AzureSubscription| Value from step B-4 | No
    | BasicAuthPassword| Do not use WeakPassword | Yes
    | CosmosDbAccount | URI from step A-10 | No
    | CosmosDbKey | value from step A-11 | Yes

6. Click Run to deploy the application to your Azure resources.
7. Your first pipeline run will likely fail because the subscription isn't authorized to the pipeline.  Click Authorize resources and Run New to try again.

**D. Add a webhook to your project in Azure DevOps**
1.  In Project Settings, go to Service Hooks and click the green plus to add a new hook
2.  Select Web Hooks as the service and click Next
3.  Choose Work Item Commented On in the trigger
4.  Enter `https://WebAppDefaultHostName/comment`, `user` for the basic auth user, and the password you selected in C-5 for the password

**E. Distribute the link to your web app to your colleagues so their text will get auto linked**
Azure DevOps does not allow anyone else to edit a comment.  Since HeapKeeper needs to update the user's comments, it needs their permissions to do so.  We gather their permissions at the web app.  