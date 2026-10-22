param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist')
)
$ErrorActionPreference = 'Stop'
$fmsCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $fmsCompiler)) {
    throw '.NET Framework x64 compiler was not found. Run this build on Windows with .NET Framework installed.'
}
$fmsSourceDirectory = Join-Path $PSScriptRoot 'src'
$fmsSources = Get-ChildItem -LiteralPath $fmsSourceDirectory -Filter '*.cs' | ForEach-Object { $_.FullName }
if (-not $fmsSources) { throw 'The src folder is empty.' }
$fmsOutput = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $fmsOutput -Force | Out-Null
$fmsExecutable = Join-Path $fmsOutput 'Forts Mod Studio 2.8.exe'
$fmsRuntime = '/resource:' + (Join-Path $fmsSourceDirectory 'runtime.lua') + ',FortsStudio.runtime.lua'
$fmsRange = '/resource:' + (Join-Path $fmsSourceDirectory 'range.lua') + ',FortsStudio.range.lua'
$fmsFormulas = '/resource:' + (Join-Path $fmsSourceDirectory 'formula_runtime.lua') + ',FortsStudio.formula_runtime.lua'
& $fmsCompiler /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 ('/out:' + $fmsExecutable) $fmsRuntime $fmsRange $fmsFormulas /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll $fmsSources
if ($LASTEXITCODE -ne 0) { throw 'Forts Mod Studio build failed.' }
Write-Output $fmsExecutable
