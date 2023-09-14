# Parameters
$ResourceGroupName = "az-ai-search"
$Location = "eastus" # Ensure to pick a location that supports the chat and embedding models of Azure OpenAI that you want to use, see https://learn.microsoft.com/azure/ai-services/openai/concepts/models.
$DeploymentPrefix = "aisearch" # Max 8 characters, lowercase letters only.

# Deploy Resource Group
Get-AzContext | Format-List
New-AzResourceGroup -Name $ResourceGroupName -Location $Location

# Deploy Template
$Deployment = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile .\azuredeploy.json -Name "Deployment-$(Get-Date -Format "yyyy-MM-dd-HH-mm-ss")" -Verbose -resourcePrefix $DeploymentPrefix

$WebAppUrl = $Deployment.Outputs["webAppUrl"].Value
Write-Host "Deployment status: $($Deployment.ProvisioningState)"
Write-Host "Published Web App: $WebAppUrl"

Start-Process $WebAppUrl

# Write the outputs as user secrets for the web app and function app to use locally.
foreach ($OutputKey in $Deployment.Outputs.Keys) {
    dotnet user-secrets -p ".\src\Azure.AISearch.WebApp" set $OutputKey "$($Deployment.Outputs[$OutputKey].Value)"
    dotnet user-secrets -p ".\src\Azure.AISearch.FunctionApp.DotNet" set $OutputKey "$($Deployment.Outputs[$OutputKey].Value)"
}

# Delete all deployed resources
Remove-AzResourceGroup -Name $ResourceGroupName -Force
