param(
$name = "cogitoheapkeeper"
,$resourceGroup = "CherwellAzureDevOpsMapper" # This is used if you want to add this to an existing resource group. if null or empty, the resource group will be set to $name.
,$location = "centralus" # To see possible locations: az account list-locations -o table
)
#A1
Write-Host "Logging you in, follow the prompts"
az login | OUT-NULL

if([string]::IsnUllOrEmpty($resourceGroup)){
    $resourceGroup = $name
}


$getSubscription = $true
while($getSubscription){
    try{
    Write-Host "We are using the following azure subscription, is this correct?:"
    az account show

        
    $response = Read-Host "(Y)es/No"
    if($response -in @("Yes","y")){
        $getSubscription = $false
        break
    }
        Write-Host "All accounts:"
        $AllAccounts = az account list | ConvertFrom-Json

        $AllAccounts | select id,name,cloudName, homeTenantID, state | ft
            
        $subscription = Read-Host "What subscription should we use? Enter the ID or name"

        az account set --subscription $subscription
    }
    catch{
        Write-Error "There was an error setting the subscription" -ErrorAction Stop
    }
}
#A3
try{
$result = az cosmosdb check-name-exists --name $name
}
catch{
    Write-Error "There was an error checking cosmosDB for the name: $name. Ensure you are logged in and that your name does not have underscores"
}
if($result -eq "true"){
    Write-Error "$name is taken, try again" -ErrorAction Stop
}

#A5
az group create --location $location --resource-group $name
#A6
az appservice plan create --name $name --resource-group $resourceGroup --location $location --sku F1
#A7
$webAppResult = az webapp create --name $name --resource-group $resourceGroup --plan $name
$webAppResult
Write-Host "The defaultHostName is $($webAppResult.defaultHostName)"
#A8
az extension add --name application-insights
#A9
$appInsightResult = az monitor app-insights component create --app $name --location $location --resource-group $resourceGroup
$appInsightResult
Write-Host "The instrumentation key is: $($appInsightResult.instrumentationkey)"
#A10
$cosmosDBResult = az cosmosdb create --name $name --resource-group $resourceGroup --enable-free-tier true
$cosmosDBResult
Write-Host "The documentEndpoint is: $($cosmosDBResultdocumentEndpoint)"