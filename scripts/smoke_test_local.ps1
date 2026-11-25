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
        }
        catch {}
    }

    if (-not $ready) { throw "Services did not become ready in time." }

    Write-Host "1. Health AI & API OK (factors=$($apiH.activeResearchFactors))"

    $me = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/me" -Headers @{ Authorization = "Bearer dev-demo:farmer" }
    Write-Host "2. Authenticated /me OK (uid=$($me.firebaseUid))"

    $calc = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/carbon-calculator/estimate" -Method Post -Headers @{ Authorization = "Bearer dev-demo:farmer"; "Content-Type" = "application/json" } -Body '{"areaHa":1.0,"treeCount":200,"variety":"TallDuaTa","treeAgeYears":10}'
    Write-Host "3. Calculator CTU 2015 result: $($calc.estimatedCO2EquivalentT) tCO2-eq ref"

    $farms = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/farms" -Headers @{ Authorization = "Bearer dev-demo:farmer" }
    $firstFarmId = $farms[0].id
    $ledger = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/farms/$firstFarmId/finance-ledger" -Headers @{ Authorization = "Bearer dev-demo:farmer" }
    Write-Host "4. Green Finance Ledger netDeclaredFarmCashflowVnd=$($ledger.summary.netDeclaredFarmCashflowVnd)"

    $projects = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/projects"
    Write-Host "5. Projects directory count: $($projects.projects.Count)"

    $rag = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/ai/assistant/query" -Method Post -Headers @{ Authorization = "Bearer dev-demo:farmer"; "Content-Type" = "application/json" } -Body '{"role":"Farmer","question":"tin chi la gi"}'
    if (-not $rag.hasEnoughEvidence) { throw "Expected RAG evidence for 'tin chi la gi'." }
    Write-Host "6. C# -> Python AI Assistant hasEnoughEvidence=$($rag.hasEnoughEvidence), citations=$($rag.citations.Count)"

    $posture = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/admin/security-posture" -Headers @{ Authorization = "Bearer dev-demo:admin" }
    Write-Host "7. Admin Security Posture OK (auth=$($posture.firebaseTokenVerification))"

    $aiProv = Invoke-RestMethod "http://127.0.0.1:8080/api/v1/admin/ai-provider" -Headers @{ Authorization = "Bearer dev-demo:admin" }
    Write-Host "7b. Admin Multi-AI Provider status OK (active=$($aiProv.activeProvider), presets=$($aiProv.providers.Count))"

    $finInputObj = @{
        farmName     = "Vuon Dua Demo"
        investment   = @{
            areaHa                          = 1.5
            landPrepAndSeedlingsVnd         = 12000000
            irrigationAndEquipmentVnd       = 18000000
            mrvBaselineAndAuditBudgetVnd    = 5000000
            annualCareAndFertilizerPerHaVnd = 9000000
            annualLaborAndHarvestPerHaVnd   = 7000000
            annualMonitoringAndAdminVnd     = 2000000
            horizonYears                    = 5
            isActualData                    = $false
        }
        revenue      = @{
            areaHa                         = 1.5
            annualCoconutYieldUnitsPerHa   = 9000
            coconutUnitPriceVnd            = 6500
            annualByProductRevenuePerHaVnd = 2500000
            annualGreenSupportGrantVnd     = 0
        }
        discountRate = 0.08
    }
    $finInputJson = $finInputObj | ConvertTo-Json -Depth 6
    $authFarmerHeaders = @{ Authorization = "Bearer dev-demo:farmer"; "Content-Type" = "application/json" }

    $finCalc = Invoke-RestMethod "http://127.0.0.1:8080/api/financial/calculate?farmId=$firstFarmId" -Method Post -Headers $authFarmerHeaders -Body $finInputJson
    if ($finCalc.cashFlowSchedule.Count -ne 6) { throw "Expected Year 0..5 (6 rows) cashFlowSchedule, got $($finCalc.cashFlowSchedule.Count)." }
    Write-Host "8. /api/financial/calculate OK (ROI=$($finCalc.profitAndRoi.roiPercentAgriOnly)%, NPV=$($finCalc.profitAndRoi.npvAgriOnlyVnd))"

    $nlTx = Invoke-RestMethod "http://127.0.0.1:8080/api/financial/parse-transaction" -Method Post -Headers $authFarmerHeaders -Body '{"text":"Chi 3.5 trieu mua phan huu co"}'
    if ($nlTx.suggestedEntryType -ne "CareExpense" -or $nlTx.amountVnd -ne 3500000) { throw "Unexpected NL parse output." }
    Write-Host "8b. /api/financial/parse-transaction OK (type=$($nlTx.suggestedEntryType), amount=$($nlTx.amountVnd))"

    $propCheck = Invoke-RestMethod "http://127.0.0.1:8080/api/carbon/proposal-check" -Method Post -Headers $authFarmerHeaders -Body '{"upfrontFeeRequestedVnd":5000000,"contractTermsText":"guaranteed revenue sign today"}'
    if ($propCheck.decisionRecommendation -ne "DO_NOT_SIGN_OR_PAY") { throw "Expected DO_NOT_SIGN_OR_PAY recommendation." }
    Write-Host "8c. /api/carbon/proposal-check OK (rec=$($propCheck.decisionRecommendation), score=$($propCheck.fraudRiskScore))"

    $scenJson = (@{ baseInput = $finInputObj; scenarios = @() }) | ConvertTo-Json -Depth 8
    $finScen = Invoke-RestMethod "http://127.0.0.1:8080/api/financial/scenarios" -Method Post -Headers $authFarmerHeaders -Body $scenJson
    if ($finScen.scenarios.Count -lt 1) { throw "Expected scenario simulation outputs." }
    Write-Host "9. /api/financial/scenarios OK (baseROI=$($finScen.baseSummary.roiPercentAgriOnly)%, variants=$($finScen.scenarios.Count))"

    $analysisJson = (@{ calculationInput = $finInputObj; includeScenarios = $true; question = "Analyze coconut investment cost and ROI" }) | ConvertTo-Json -Depth 8
    $finAnalyze = Invoke-RestMethod "http://127.0.0.1:8080/api/financial/analyze?farmId=$firstFarmId" -Method Post -Headers $authFarmerHeaders -Body $analysisJson
    $finReport = Invoke-RestMethod "http://127.0.0.1:8080/api/financial/reports?farmId=$firstFarmId" -Method Post -Headers $authFarmerHeaders -Body $analysisJson
    if (-not $finReport.report.executiveSummary -or -not $finReport.report.roiAnalysis -or -not $finReport.report.dataSourcesAndAssumptions) {
        throw "Missing expected sections in 9-part financial report."
    }
    Write-Host "10. /api/financial/analyze & /reports OK (reportId=$($finReport.reportId), narrativeLen=$($finAnalyze.aiNarrative.Length))"

    $chatJson = (@{ role = "Farmer"; question = "Calculate ROI for 1.5 ha Dua Ta and analyze costs"; financialInput = $finInputObj }) | ConvertTo-Json -Depth 8
    $aiChat = Invoke-RestMethod "http://127.0.0.1:8080/api/ai/assistant" -Method Post -Headers $authFarmerHeaders -Body $chatJson
    if (-not $aiChat.financialResult -or $aiChat.citations.Count -lt 1) { throw "Expected financialResult and RAG citations from /api/ai/assistant." }
    Write-Host "11. /api/ai/assistant OK (intent=$($aiChat.intent), lang=$($aiChat.language), citations=$($aiChat.citations.Count))"

    $integJson = '{"farmName":"Vuon Dua Demo","areaHa":1.5,"treeCount":300,"variety":"TallDuaTa","ageYears":10,"hasLandDocument":false,"hasFieldPhotos":true,"hasOwnerConsent":true}'
    $integ = Invoke-RestMethod "http://127.0.0.1:8080/api/carbon/integrity-check" -Method Post -Headers $authFarmerHeaders -Body $integJson
    if ($integ.documentCompletenessScore -ge 100 -or $integ.carbonIntegrityRiskScore -ge 35 -or $integ.integrityStatus -ne "IncompleteDocuments_NotFraud") {
        throw "Carbon integrity 3-score separation check failed: status=$($integ.integrityStatus), risk=$($integ.carbonIntegrityRiskScore)"
    }
    Write-Host "12. /api/carbon/integrity-check OK (readiness=$($integ.projectReadinessScore), docCompleteness=$($integ.documentCompletenessScore), fraudRisk=$($integ.carbonIntegrityRiskScore), status=$($integ.integrityStatus))"

    $finWebR = Invoke-WebRequest "http://127.0.0.1:3000/financial-assistant" -UseBasicParsing -TimeoutSec 5
    if ($finWebR.StatusCode -ne 200) { throw "Web /financial-assistant returned $($finWebR.StatusCode)" }
    Write-Host "13. Web /financial-assistant HTTP 200 OK"

    $swagger = Invoke-WebRequest "http://127.0.0.1:8080/swagger/v1/swagger.json" -UseBasicParsing
    $swagger.Content | Out-File "$repoRoot\docs\openapi.json" -Encoding utf8
    Write-Host "14. Updated docs/openapi.json bytes=$($swagger.RawContentLength)"

    Write-Host "ALL LIVE SMOKE CHECKS PASSED!"
}
finally {
    foreach ($p in @($webProc, $apiProc, $aiProc)) {
        if ($p -and -not $p.HasExited) {
            Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        }
    }
}
