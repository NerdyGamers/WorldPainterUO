# Publish script for WorldPainterUO
# Usage: .\scripts\publish.ps1 [version]
# Example: .\scripts\publish.ps1 v1.0.0

param(
    [string]$Version = "v1.0.0"
)

$Project = "src\WorldPainterUO.App\WorldPainterUO.App.csproj"
$OutputRoot = "publish"

$Runtimes = @(
    @{ Rid = "win-x64";   SingleFile = $true;  Suffix = "win-x64" },
    @{ Rid = "linux-x64";  SingleFile = $true;  Suffix = "linux-x64" },
    @{ Rid = "osx-x64";   SingleFile = $false; Suffix = "osx-x64" }
)

Write-Host "Publishing WorldPainterUO $Version" -ForegroundColor Cyan

foreach ($rt in $Runtimes) {
    $outputDir = Join-Path $OutputRoot "$Version\$($rt.Suffix)"
    Write-Host "`nPublishing for $($rt.Rid)..." -ForegroundColor Yellow

    $args = @(
        "publish", $Project,
        "--configuration", "Release",
        "--runtime", $rt.Rid,
        "--output", $outputDir,
        "--self-contained", "true"
    )

    if ($rt.SingleFile) {
        $args += "/p:PublishSingleFile=true"
        $args += "/p:IncludeNativeLibrariesForSelfExtract=true"
    }

    & dotnet $args

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed for $($rt.Rid)!" -ForegroundColor Red
        exit 1
    }

    Write-Host "Published to $outputDir" -ForegroundColor Green
}

Write-Host "`nAll builds published to $OutputRoot\$Version\" -ForegroundColor Cyan
Write-Host "Artifacts:" -ForegroundColor Cyan
Get-ChildItem -Path "$OutputRoot\$Version" -Directory | ForEach-Object {
    $dir = $_.FullName
    $exe = Get-ChildItem -Path $dir -Filter "*.exe" -Recurse | Select-Object -First 1
    if ($exe) {
        Write-Host "  $($_.Name): $($exe.FullName)" -ForegroundColor Green
    } else {
        Write-Host "  $($_.Name): (check $dir)" -ForegroundColor Yellow
    }
}
