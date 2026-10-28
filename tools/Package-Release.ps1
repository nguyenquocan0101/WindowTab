param(
    [string]$Version = '',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\dist\releases')
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($Version)) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'WindowTab.csproj') -Raw
    $Version = [string]$project.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Invalid release version: $Version"
}
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path $outputRoot ('staging-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $stage 'WindowTab'
New-Item -ItemType Directory -Path $payload -Force | Out-Null

$publishArgs = @(
    'publish', (Join-Path $repoRoot 'WindowTab.csproj'), '-c', 'Release',
    '-r', 'win-x64', '--self-contained', 'true',
    '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:DebugType=None', '-p:DebugSymbols=false', "-p:Version=$Version", '-o', $payload
)
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'WindowTab publish failed.' }

Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $payload
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD-PARTY-NOTICES.md') -Destination $payload
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\getting-started.md') -Destination (Join-Path $payload 'README-FIRST.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'tools\Start-WindowTab.cmd') -Destination $payload

$assets = Get-Content (Join-Path $repoRoot 'obj\project.assets.json') -Raw | ConvertFrom-Json
$runtimeConfig = Get-Content (Join-Path $repoRoot 'bin\Release\net10.0-windows\win-x64\WindowTab.runtimeconfig.json') -Raw | ConvertFrom-Json
$licensesDir = Join-Path $payload 'licenses'
New-Item -ItemType Directory -Path $licensesDir -Force | Out-Null
foreach ($framework in $runtimeConfig.runtimeOptions.includedFrameworks) {
    $packageId = switch ($framework.name) {
        'Microsoft.NETCore.App' { 'microsoft.netcore.app.runtime.win-x64' }
        'Microsoft.WindowsDesktop.App' { 'microsoft.windowsdesktop.app.runtime.win-x64' }
        default { throw "Unknown bundled runtime: $($framework.name)" }
    }
    $packagePath = $null
    foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
        $candidate = Join-Path $folder (Join-Path $packageId $framework.version)
        if (Test-Path -LiteralPath $candidate) { $packagePath = $candidate; break }
    }
    if (-not $packagePath) { throw "Cannot locate license files for $packageId/$($framework.version)." }
    $noticeFiles = @(Get-ChildItem -LiteralPath $packagePath -File | Where-Object { $_.Name -match '^(LICENSE(?:\.TXT)?|THIRD-PARTY-NOTICES(?:\.TXT)?)$' })
    if (-not ($noticeFiles | Where-Object { $_.Name -match '^LICENSE' })) { throw "Missing runtime license: $packageId" }
    foreach ($notice in $noticeFiles) {
        Copy-Item -LiteralPath $notice.FullName -Destination (Join-Path $licensesDir "$packageId-$($notice.Name)")
    }
}

$zipPath = Join-Path $outputRoot 'WindowTab-win-x64.zip'
Compress-Archive -LiteralPath $payload -DestinationPath $zipPath -CompressionLevel Optimal -Force
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$sums = Join-Path $outputRoot 'SHA256SUMS.txt'
[System.IO.File]::WriteAllText($sums, "$hash  WindowTab-win-x64.zip`n", [System.Text.UTF8Encoding]::new($false))
Write-Output "Package: $zipPath"
Write-Output "SHA256: $hash"
