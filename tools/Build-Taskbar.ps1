param(
    [Parameter(Mandatory)] [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$source = Join-Path $PSScriptRoot '..\Platform\Taskbar\TaskbarTransparency.cpp'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$visualStudio = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (-not $visualStudio) { throw 'Install the Visual Studio Desktop development with C++ workload and Windows SDK.' }

& (Join-Path $visualStudio 'Common7\Tools\Launch-VsDevShell.ps1') -SkipAutomaticLocation -Arch amd64 -HostArch amd64
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$objectPath = Join-Path $outputDirectory 'TaskbarTransparency.obj'
& cl.exe /nologo /std:c++20 /EHsc /O2 /MT /LD "/Fo$objectPath" "/Fe$OutputPath" $source /link /EXPORT:DllGetClassObject=GetTaskbarClassObject,PRIVATE ole32.lib oleaut32.lib runtimeobject.lib user32.lib
if ($LASTEXITCODE -ne 0) { throw 'WindowTab taskbar module build failed.' }
