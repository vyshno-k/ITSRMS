param([string]$BaseUrl = 'http://localhost:5103')

Write-Host "Checking $BaseUrl/api/databricks/status ..." -ForegroundColor Cyan
try {
    $status = Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/databricks/status"
    $status | ConvertTo-Json -Depth 10
    if (-not $status.configured) { throw "Databricks is not configured: $($status.message)" }
    Write-Host 'Configuration check passed.' -ForegroundColor Green
} catch {
    Write-Host "Status check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$body = @{
    employeeId = $null
    categoryId = $null
    serviceTypeId = $null
    search = $null
    from = $null
    to = $null
} | ConvertTo-Json

Write-Host ''
Write-Host 'Preparing SQLite snapshots (does NOT start the Databricks job) ...' -ForegroundColor Cyan
try {
    $prepared = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/databricks/prepare" -ContentType 'application/json' -Body $body
    $prepared | ConvertTo-Json -Depth 20
    Write-Host 'Prepare succeeded. The returned jobParameters are exactly what the job uses.' -ForegroundColor Green
} catch {
    Write-Host "Prepare failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
