param(
    [Parameter(Mandatory = $true)][string]$FirebaseApiKey,
    [Parameter(Mandatory = $true)][string]$FirebaseAuthDomain,
    [Parameter(Mandatory = $true)][string]$FirebaseProjectId,
    [Parameter(Mandatory = $true)][string]$FirebaseAppId,
    [Parameter(Mandatory = $true)][string]$ServiceAccountJsonPath,
    [bool]$KeepDemoFallback = $true
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$secretsDir = Join-Path $repoRoot "secrets"
New-Item -ItemType Directory -Force -Path $secretsDir | Out-Null

if (-not (Test-Path $ServiceAccountJsonPath)) {
    throw "Service Account JSON file not found: $ServiceAccountJsonPath"
}

$targetSa = Join-Path $secretsDir "firebase-service-account.json"
Copy-Item $ServiceAccountJsonPath -Destination $targetSa -Force

$showDemo = if ($KeepDemoFallback) { "true" } else { "false" }

$webEnv = @"
NEXT_PUBLIC_FIREBASE_API_KEY=$FirebaseApiKey
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=$FirebaseAuthDomain
NEXT_PUBLIC_FIREBASE_PROJECT_ID=$FirebaseProjectId
NEXT_PUBLIC_FIREBASE_APP_ID=$FirebaseAppId
NEXT_PUBLIC_API_BASE_URL=http://localhost:8080
NEXT_PUBLIC_SHOW_DEMO_MODE=$showDemo
"@
$webEnvPath = Join-Path $repoRoot "web\.env.local"
[System.IO.File]::WriteAllText($webEnvPath, $webEnv, [System.Text.UTF8Encoding]::new($false))

Write-Host "Saved web/.env.local and $targetSa cleanly."
Write-Host "Next steps:"
Write-Host "1) cd web && npm run build"
Write-Host "2) Set FIREBASE_PROJECT_ID=$FirebaseProjectId and FIREBASE_SERVICE_ACCOUNT_PATH=$targetSa before starting CocoCarbon.Api"
