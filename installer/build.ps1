# Genera el instalador de SelectiveBuild en releases\v<version>\.
#
#   powershell -ExecutionPolicy Bypass -File installer\build.ps1
#
# 1. Compila la solucion en Release (el csproj estampa la version en SelectiveBuild.addin).
# 2. Compila installer\*.cs con el csc de .NET 4 (sin Roslyn: C# 5), embebiendo la DLL y el .addin.
# 3. Deja en releases\v<version>\: el instalador, el .zip para instalacion manual y SHA256SUMS.txt.
#
# Nota: este archivo es solo ASCII a proposito, para que lo lea igual Windows PowerShell 5.1 y PowerShell 7.

$ErrorActionPreference = 'Stop'

$root     = Split-Path -Parent $PSScriptRoot
$fw       = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
$msbuild  = Join-Path $fw 'MSBuild.exe'
$csc      = Join-Path $fw 'csc.exe'
$release  = Join-Path $root 'SelectiveBuild\bin\Release'
$obj      = Join-Path $root 'obj\installer'

# --- 1. addin -------------------------------------------------------------------------------------
& $msbuild (Join-Path $root 'SelectiveBuild.sln') /p:Configuration=Release /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion del addin.' }

$dll   = Join-Path $release 'SelectiveBuild.dll'
$addin = Join-Path $release 'SelectiveBuild.addin'
$ver   = [Reflection.AssemblyName]::GetAssemblyName($dll).Version
$short = '{0}.{1}.{2}' -f $ver.Major, $ver.Minor, $ver.Build

if (-not (Select-String -Path $addin -Pattern "version=`"$short`"" -Quiet)) {
    throw "SelectiveBuild.addin no tiene la version $short estampada."
}

# --- 2. instalador --------------------------------------------------------------------------------
New-Item -ItemType Directory -Force $obj | Out-Null
$assemblyInfo = Join-Path $obj 'AssemblyInfo.g.cs'
@"
using System.Reflection;
[assembly: AssemblyTitle("SelectiveBuild Setup")]
[assembly: AssemblyProduct("SelectiveBuild")]
[assembly: AssemblyVersion("$short.0")]
[assembly: AssemblyFileVersion("$short.0")]
"@ | Set-Content -Path $assemblyInfo -Encoding UTF8

$out = Join-Path $root "releases\v$short"
New-Item -ItemType Directory -Force $out | Out-Null
$setup = Join-Path $out "SelectiveBuild-Setup-$short.exe"

$manifest = Join-Path $PSScriptRoot 'app.manifest'
$cscArgs = @(
    '/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/codepage:65001',
    "/out:$setup",
    "/win32manifest:$manifest",
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll', '/reference:System.Windows.Forms.dll',
    "/resource:$dll,SelectiveBuild.dll",
    "/resource:$addin,SelectiveBuild.addin",
    (Join-Path $PSScriptRoot 'Setup.cs'),
    (Join-Path $PSScriptRoot 'SetupForm.cs'),
    $assemblyInfo
)
& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { throw 'Fallo la compilacion del instalador.' }

# --- 3. zip manual + hashes -----------------------------------------------------------------------
$zip = Join-Path $out "SelectiveBuild-$short.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path $dll, $addin -DestinationPath $zip

$sums = Join-Path $out 'SHA256SUMS.txt'
$lines = foreach ($f in @($setup, $zip)) {
    '{0}  {1}' -f (Get-FileHash $f -Algorithm SHA256).Hash.ToLower(), (Split-Path $f -Leaf)
}
$lines | Set-Content -Path $sums -Encoding ASCII

Write-Host ''
Write-Host "Version $short generada en $out"
Get-ChildItem $out | Select-Object Name, Length | Format-Table -AutoSize
