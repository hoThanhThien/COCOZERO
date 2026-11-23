$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$dotnet = Join-Path $env:USERPROFILE ".dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$python = Join-Path $repoRoot "ai-service\.venv\Scripts\python.exe"

Write-Host "Starting Python AI Service (port 8000)..."
$aiProc = Start-Process -FilePath $python -ArgumentList "-m", "uvicorn", "app.main:app", "--host", "127.0.0.1", "--port", "8000" -WorkingDirectory "$repoRoot\ai-service" -PassThru -WindowStyle Hidden
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://127.0.0.1:8080"
$env:AI_SERVICE_INTERNAL_KEY = "dev-internal-key-local"

Write-Host "Starting C# API (port 8080)..."
$apiProc = Start-Process -FilePath $dotnet -ArgumentList "run", "--no-build", "--project", "$repoRoot\backend\src\CocoCarbon.Api\CocoCarbon.Api.csproj" -WorkingDirectory "$repoRoot\backend\src\CocoCarbon.Api" -PassThru -WindowStyle Hidden

Write-Host "Starting Next.js Web (port 3000)..."
$webProc = Start-Process -FilePath "npm.cmd" -ArgumentList "run", "start", "--", "-p", "3000" -WorkingDirectory "$repoRoot\web" -PassThru -WindowStyle Hidden

try {
    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 1
        try {
            $aiH = Invoke-RestMethod "http://127.0.0.1:8000/health" -TimeoutSec 2
            $apiH = Invoke-RestMethod "http://127.0.0.1:8080/health" -TimeoutSec 2
            $webR = Invoke-WebRequest "http://127.0.0.1:3000/" -UseBasicParsing -TimeoutSec 2
            if ($aiH.status -eq "healthy" -and $apiH.status -eq "healthy" -and $webR.StatusCode -eq 200) {
                $ready = $true
                break
            }
        } catch {}
    }

    if (-not $ready) { throw "Services did not become ready in time." }

    Write-Host "1. Health AI & API OK (factors=$($apiH.activeResearchFactors))"

    $me = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/me" -Headers @{ Authorization = "Bearer dev-demo:farmer" }
    Write-Host "2. Authenticated /me as: $($me.displayName)"

    $calc = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/carbon-calculator/estimate" -Method Post -Headers @{ Authorization = "Bearer dev-demo:farmer"; "Content-Type" = "application/json" } -Body '{"areaHa":1.0,"treeCount":200,"variety":"TallDuaTa","treeAgeYears":10}'
    Write-Host "3. Calculator CTU 2015 result: $($calc.estimatedCO2EquivalentT) tCO2-eq ref"

    $projects = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/projects"
    Write-Host "4. Projects directory count: $($projects.projects.Count)"

    $rag = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/ai/assistant/query" -Method Post -Headers @{ Authorization = "Bearer dev-demo:farmer"; "Content-Type" = "application/json" } -Body '{"role":"Farmer","question":"Hệ số CTU 2015 cho Dừa Ta 10 năm tuổi?"}'
    Write-Host "5. C# -> Python AI Assistant hasEnoughEvidence=$($rag.hasEnoughEvidence), citations=$($rag.citations.Count)"

    $swagger = Invoke-WebRequest "http://127.0.0.1:8080/swagger/v1/swagger.json" -UseBasicParsing
    Write-Host "6. OpenAPI Swagger JSON bytes=$($swagger.RawContentLength)"

    Write-Host "ALL LIVE SMOKE CHECKS PASSED!"
}
finally {
    foreach ($p in @($webProc, $apiProc, $aiProc)) {
        if ($p -and -not $p.HasExited) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
}
