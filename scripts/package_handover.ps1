param(
    [string]$OutputZip = "$PSScriptRoot\..\dist\CocoCarbon-AI-source.zip"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path
$distDir = Split-Path -Parent $OutputZip
New-Item -ItemType Directory -Force -Path $distDir | Out-Null
if (Test-Path $OutputZip) { Remove-Item $OutputZip -Force }

$excludeDirs = @(
    ".git", "bin", "obj", "node_modules", ".next", ".venv", "__pycache__",
    ".pytest_cache", ".gradle", "build", "dist", "storage", "TestResults"
)

$tempStage = Join-Path $env:TEMP ("cococarbon_stage_" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempStage | Out-Null

try {
    Get-ChildItem -Path $repoRoot -Recurse -Force | ForEach-Object {
        $rel = $_.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
        if (-not $rel) { return }

        $segments = $rel -split '[\\/]'
        foreach ($seg in $segments) {
            if ($excludeDirs -contains $seg) { return }
        }
        if ($_.Name -like ".env*" -and $_.Name -ne ".env.example") { return }
        if ($_.Name -eq ".runtime_ai_config.json" -or $_.Name -eq "google-services.json" -or $_.Name -like "*.db*" -or $_.Extension -in @(".db", ".sqlite", ".sqlite3", ".keystore", ".jks", ".pfx", ".pem", ".key")) { return }

        $dest = Join-Path $tempStage $rel
        if ($_.PSIsContainer) {
            New-Item -ItemType Directory -Force -Path $dest | Out-Null
        } else {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dest) | Out-Null
            Copy-Item $_.FullName -Destination $dest -Force
        }
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($tempStage, $OutputZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    Write-Host "Created clean source package: $OutputZip"
}
finally {
    if (Test-Path $tempStage) { Remove-Item $tempStage -Recurse -Force }
}
