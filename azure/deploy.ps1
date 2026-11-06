<#
.SYNOPSIS
  Creates the Azure resources for the IMS lesson and deploys Invoice.CqrsAPI + Invoice.Functions.

  Resource group
   |- Azure SQL (server + database "Accounts")           the API's database (optional: -SkipSql)
   |- Service Bus namespace (Standard)
   |    |- topic  ims-events  -> subscriptions invoice-email, stock-watch (filtered), audit-log (all events)
   |    |- queue  stock-alerts
   |- Storage account                                    Functions host state + PDF / audit blobs
   |- Log Analytics + Application Insights               logs, traces, failures
   |- App Service plan (Linux B1)  shared by:
   |    |- Web App       Invoice.CqrsAPI                  (managed identity -> Service Bus Data Sender)
   |    |- Function App  Invoice.Functions                (managed identity -> Service Bus Data Receiver + Sender)

.PREREQUISITES
  Azure CLI (az), signed in:  az login
  .NET 10 SDK
  PowerShell 7 recommended (works in Windows PowerShell 5.1 too)

.EXAMPLE
  ./deploy.ps1 -SqlAdminPassword 'Str0ng!Passw0rd#1'
  ./deploy.ps1 -SqlAdminPassword 'x' -SkipSql -SqlConnectionString 'Server=...'

.NOTES
  Costs money while it exists. When the class is over:  az group delete -n <resource-group> --yes --no-wait
#>
param(
    [string] $Prefix = "ims",
    [string] $Location = "centralindia",
    [string] $SqlAdminUser = "imsadmin",
    [Parameter(Mandatory = $true)] [string] $SqlAdminPassword,
    [switch] $SkipSql,
    [string] $SqlConnectionString = "",
    [string] $BotUserName = "functions-bot",
    [string] $BotPassword = "Fn!Bot-Passw0rd-1"
)

$ErrorActionPreference = "Stop"

function Step($text) { Write-Host "`n=== $text" -ForegroundColor Cyan }

# ---------------------------------------------------------------- names
$suffix = Get-Random -Minimum 10000 -Maximum 99999
$rg     = "$Prefix-rg-$suffix"
$sb     = "$Prefix-sb-$suffix"
$st     = ("{0}st{1}" -f $Prefix, $suffix).ToLower() -replace "[^a-z0-9]", ""
$law    = "$Prefix-law-$suffix"
$ai     = "$Prefix-ai-$suffix"
$plan   = "$Prefix-plan-$suffix"
$api    = "$Prefix-api-$suffix"
$fn     = "$Prefix-func-$suffix"
$sql    = "$Prefix-sql-$suffix"
$dbName = "Accounts"

$root = Split-Path -Parent $PSScriptRoot
$out  = Join-Path $PSScriptRoot "publish"

# ---------------------------------------------------------------- resource group
Step "Resource group $rg ($Location)"
az group create -n $rg -l $Location -o none

# ---------------------------------------------------------------- Service Bus
Step "Service Bus namespace $sb (Standard: topics need Standard or Premium)"
az servicebus namespace create -g $rg -n $sb -l $Location --sku Standard -o none
$sbId = az servicebus namespace show -g $rg -n $sb --query id -o tsv

Step "Topic ims-events + subscriptions"
az servicebus topic create -g $rg --namespace-name $sb -n ims-events `
    --enable-duplicate-detection true --duplicate-detection-history-time-window PT10M -o none

foreach ($sub in @("invoice-email", "stock-watch", "audit-log")) {
    az servicebus topic subscription create -g $rg --namespace-name $sb --topic-name ims-events -n $sub `
        --max-delivery-count 5 --lock-duration PT1M -o none
}

# invoice-email and stock-watch only want SalesInvoicePosted. audit-log keeps its default rule (= everything).
foreach ($sub in @("invoice-email", "stock-watch")) {
    az servicebus topic subscription rule delete -g $rg --namespace-name $sb --topic-name ims-events `
        --subscription-name $sub -n '$Default' -o none
    az servicebus topic subscription rule create -g $rg --namespace-name $sb --topic-name ims-events `
        --subscription-name $sub -n invoice-posted-only --filter-sql-expression "EventType = 'SalesInvoicePosted'" -o none
}

Step "Queue stock-alerts"
az servicebus queue create -g $rg --namespace-name $sb -n stock-alerts --max-delivery-count 5 `
    --enable-duplicate-detection true --duplicate-detection-history-time-window PT10M -o none

# ---------------------------------------------------------------- Storage + monitoring
Step "Storage account $st"
az storage account create -g $rg -n $st -l $Location --sku Standard_LRS --allow-blob-public-access false -o none

Step "Application Insights"
az monitor log-analytics workspace create -g $rg -n $law -l $Location -o none
$lawId = az monitor log-analytics workspace show -g $rg -n $law --query id -o tsv
az monitor app-insights component create --app $ai -g $rg -l $Location --workspace $lawId --kind web -o none
$aiConn = az monitor app-insights component show --app $ai -g $rg --query connectionString -o tsv

# ---------------------------------------------------------------- SQL
if ($SkipSql) {
    Step "Skipping Azure SQL (using the connection string you passed)"
    if ([string]::IsNullOrWhiteSpace($SqlConnectionString)) { throw "-SkipSql needs -SqlConnectionString." }
    $dbConn = $SqlConnectionString
}
else {
    Step "Azure SQL server $sql + database $dbName"
    az sql server create -g $rg -n $sql -l $Location -u $SqlAdminUser -p $SqlAdminPassword -o none
    az sql server firewall-rule create -g $rg --server $sql -n AllowAzureServices `
        --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none
    az sql db create -g $rg -s $sql -n $dbName --service-objective Basic -o none
    $dbConn = "Server=tcp:$sql.database.windows.net,1433;Database=$dbName;User ID=$SqlAdminUser;Password=$SqlAdminPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

# ---------------------------------------------------------------- App Service plan, Web App, Function App
Step "App Service plan $plan (Linux B1)"
az appservice plan create -g $rg -n $plan -l $Location --is-linux --sku B1 -o none

Step "Web App $api (Invoice.CqrsAPI)"
az webapp create -g $rg -n $api -p $plan --runtime "DOTNETCORE:10.0" -o none
$apiPrincipal = az webapp identity assign -g $rg -n $api --query principalId -o tsv

$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$jwtKey = [Convert]::ToBase64String($bytes)

$apiSettings = @(
    "ConnectionStrings__DefaultConnection=$dbConn",
    "Jwt__Key=$jwtKey",
    "Jwt__Issuer=Invoice.Api",
    "Jwt__Audience=Invoice.Api",
    "Jwt__ExpiryMinutes=60",
    "ServiceBus__Enabled=true",
    "ServiceBus__FullyQualifiedNamespace=$sb.servicebus.windows.net",
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$aiConn",
    "ASPNETCORE_ENVIRONMENT=Production"
)
az webapp config appsettings set -g $rg -n $api --settings $apiSettings -o none

Step "Function App $fn (Invoice.Functions)"
az functionapp create -g $rg -n $fn -p $plan -s $st --runtime dotnet-isolated --runtime-version 10.0 `
    --functions-version 4 --os-type Linux -o none
az functionapp config set -g $rg -n $fn --always-on true -o none   # timers need an always-on plan
$fnPrincipal = az functionapp identity assign -g $rg -n $fn --query principalId -o tsv

$apiUrl = "https://$api.azurewebsites.net"

$fnSettings = @(
    "ServiceBusConnection__fullyQualifiedNamespace=$sb.servicebus.windows.net",
    "ImsApi__BaseUrl=$apiUrl/",
    "ImsApi__UserName=$BotUserName",
    "ImsApi__Password=$BotPassword",
    "OverdueSchedule=0 0 8 * * *",
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$aiConn"
)
az functionapp config appsettings set -g $rg -n $fn --settings $fnSettings -o none

# ---------------------------------------------------------------- identities: no secrets for Service Bus
Step "Role assignments (managed identity -> Service Bus)"
az role assignment create --assignee-object-id $apiPrincipal --assignee-principal-type ServicePrincipal `
    --role "Azure Service Bus Data Sender" --scope $sbId -o none

az role assignment create --assignee-object-id $fnPrincipal --assignee-principal-type ServicePrincipal `
    --role "Azure Service Bus Data Receiver" --scope $sbId -o none
az role assignment create --assignee-object-id $fnPrincipal --assignee-principal-type ServicePrincipal `
    --role "Azure Service Bus Data Sender" --scope $sbId -o none

# ---------------------------------------------------------------- build + deploy
Step "Publishing Invoice.CqrsAPI"
dotnet publish (Join-Path $root "Invoice.CqrsAPI") -c Release -o (Join-Path $out "api")
Compress-Archive -Path (Join-Path $out "api\*") -DestinationPath (Join-Path $out "api.zip") -Force
az webapp deploy -g $rg -n $api --src-path (Join-Path $out "api.zip") --type zip -o none

Step "Publishing Invoice.Functions"
dotnet publish (Join-Path $root "Invoice.Functions") -c Release -o (Join-Path $out "functions")
Compress-Archive -Path (Join-Path $out "functions\*") -DestinationPath (Join-Path $out "functions.zip") -Force
az functionapp deploy -g $rg -n $fn --src-path (Join-Path $out "functions.zip") --type zip -o none

# ---------------------------------------------------------------- service account for the Functions
Step "Creating the '$BotUserName' service account through the API"
try {
    $body = @{
        userName = $BotUserName; email = "$BotUserName@example.com"; password = $BotPassword
        firstName = "Functions"; lastName = "Bot"; isActive = $true
    } | ConvertTo-Json
    Invoke-RestMethod -Method Post -Uri "$apiUrl/api/v1/User/Create" -ContentType "application/json" -Body $body -TimeoutSec 120 | Out-Null
    Write-Host "Service account created."
}
catch {
    Write-Warning "Could not create the service account yet (is the database schema in place? is the app still starting?)."
    Write-Warning "Create it later with POST $apiUrl/api/v1/User/Create using user '$BotUserName' / the password above."
}

# ---------------------------------------------------------------- summary
Step "Done"
Write-Host "Resource group : $rg"
Write-Host "API            : $apiUrl/swagger"
Write-Host "Function app   : https://$fn.azurewebsites.net"
Write-Host "Service Bus    : $sb.servicebus.windows.net  (topic ims-events, queue stock-alerts)"
Write-Host ""
if (-not $SkipSql) {
    Write-Host "NEXT: load your IMS database schema into Azure SQL '$dbName' on server $sql.database.windows.net"
    Write-Host "      (SSMS 'Export Data-tier Application' -> bacpac -> Azure portal 'Import database', or run your scripts)."
}
Write-Host "CLEAN UP when finished:  az group delete -n $rg --yes --no-wait"
