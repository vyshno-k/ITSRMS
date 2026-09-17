param(
    [Parameter(Mandatory=$true)][string]$Host,
    [Parameter(Mandatory=$true)][string]$JobId,
    [Parameter(Mandatory=$true)][string]$Token,
    [string]$VolumePath = '/Volumes/srms_catalog/srms/integration'
)

$env:Databricks__Host = $Host.TrimEnd('/')
$env:Databricks__Token = $Token
$env:Databricks__JobId = $JobId
$env:Databricks__VolumePath = $VolumePath.TrimEnd('/')
$env:Databricks__PollIntervalSeconds = '3'
$env:Databricks__PollTimeoutSeconds = '600'

Write-Host ''
Write-Host 'Databricks environment variables configured for THIS PowerShell window.' -ForegroundColor Green
Write-Host "Host:        $env:Databricks__Host"
Write-Host "Job ID:      $env:Databricks__JobId"
Write-Host "Volume path: $env:Databricks__VolumePath"
Write-Host 'Token:       configured (hidden)'
Write-Host ''
Write-Host 'Now run from the backend folder:' -ForegroundColor Cyan
Write-Host '  dotnet restore'
Write-Host '  dotnet build'
Write-Host '  dotnet run --launch-profile http'
