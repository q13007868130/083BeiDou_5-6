# Full C# decompilation of a MapleDash Windows Mono managed assembly.
# Offline inspection only; no game execution or change to existing BeiDou branch.
param(
    [Parameter(Mandatory=$true)] [string]$AssemblyPath,
    [string]$OutputDir = (Join-Path (Get-Location).Path 'MapleDash-Decompiled-CSharp'),
    [string]$ILSpyVersion = '11.2.0.10050'
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $AssemblyPath -PathType Leaf)) { throw "Assembly not found: $AssemblyPath" }
$AssemblyPath = (Resolve-Path -LiteralPath $AssemblyPath).Path
$dotnet = Get-Command 'dotnet' -ErrorAction SilentlyContinue
if (!$dotnet) { throw 'Install Microsoft .NET SDK 10.0.100 or newer before running this script.' }
$info = & dotnet --version
$major = [int]($info -split '\.')[0]
if ($major -lt 10) { throw "dotnet SDK $info found; ILSpy $ILSpyVersion tool installation requires .NET SDK 10+" }
$toolDir = Join-Path $env:TEMP 'MapleDash-ILSpyTools'
New-Item -ItemType Directory -Force -Path $toolDir | Out-Null
$exe = Join-Path $toolDir 'ilspycmd.exe'
if (!(Test-Path -LiteralPath $exe)) {
    & dotnet tool install ilspycmd --version $ILSpyVersion --tool-path $toolDir
    if ($LASTEXITCODE -ne 0) { throw 'ILSpy installation failed.' }
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
& $exe --version
& $exe --project --outputdir $OutputDir --nested-directories --disable-updatecheck $AssemblyPath
if ($LASTEXITCODE -ne 0) { throw "ILSpy failed with exit code $LASTEXITCODE" }
$sha = (Get-FileHash -Algorithm SHA256 -LiteralPath $AssemblyPath).Hash.ToLowerInvariant()
"Input: $AssemblyPath`nSHA256: $sha`nILSpy: $ILSpyVersion`nGenerated: $(Get-Date -Format o)" |
    Set-Content -Path (Join-Path $OutputDir 'RECOVERY-PROVENANCE.txt') -Encoding UTF8
Write-Host "Decompilation completed: $OutputDir"
Write-Host 'This does not reconstruct Unity scenes, AssetDatabase, original comments, or dependency assemblies.'