param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'
$fmsCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $fmsCompiler)) {
    throw '.NET Framework x64 compiler was not found. Build on Windows with .NET Framework installed.'
}

$fmsSourceDirectory = Join-Path $PSScriptRoot 'src'
$fmsAssemblyInfo = Get-Content -LiteralPath (Join-Path $fmsSourceDirectory 'AssemblyInfo.cs') -Raw
$fmsVersionMatch = [regex]::Match($fmsAssemblyInfo, 'AssemblyFileVersion\("(?<version>\d+\.\d+\.\d+\.\d+)"\)')
if (-not $fmsVersionMatch.Success) { throw 'AssemblyFileVersion is missing from src/AssemblyInfo.cs.' }
$fmsFileVersion = [version]$fmsVersionMatch.Groups['version'].Value
$fmsVersion = '{0}.{1}.{2}' -f $fmsFileVersion.Major, $fmsFileVersion.Minor, $fmsFileVersion.Build
$fmsManifest = Join-Path $fmsSourceDirectory 'app.manifest'
if (-not (Test-Path -LiteralPath $fmsManifest)) { throw 'src/app.manifest is missing.' }
$fmsSources = @(Get-ChildItem -LiteralPath $fmsSourceDirectory -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName })
if (-not $fmsSources.Count) { throw 'The src folder is empty.' }

$fmsOutput = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $fmsOutput -Force | Out-Null
$fmsExecutable = Join-Path $fmsOutput ('Forts Mod Studio {0}.exe' -f $fmsVersion)
$fmsRuntime = '/resource:' + (Join-Path $fmsSourceDirectory 'runtime.lua') + ',FortsStudio.runtime.lua'
$fmsRange = '/resource:' + (Join-Path $fmsSourceDirectory 'range.lua') + ',FortsStudio.range.lua'
& $fmsCompiler /nologo /target:winexe /platform:x64 /optimize+ /langversion:5 /codepage:65001 ('/out:' + $fmsExecutable) ('/win32manifest:' + $fmsManifest) $fmsRuntime $fmsRange /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll $fmsSources | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Forts Mod Studio build failed.' }

# These are observations about this build, not an antivirus verdict or a signature.
$fmsSourceCommit = $null
$fmsSourceTreeDirty = $null
if ((Test-Path -LiteralPath (Join-Path $PSScriptRoot '.git')) -and (Get-Command git -ErrorAction SilentlyContinue)) {
    $fmsGitCommit = & git -C $PSScriptRoot rev-parse --verify HEAD 2>$null
    if ($LASTEXITCODE -eq 0) {
        $fmsSourceCommit = ($fmsGitCommit | Select-Object -First 1).Trim()
        $fmsGitStatus = @(& git -C $PSScriptRoot status --porcelain --untracked-files=normal 2>$null)
        if ($LASTEXITCODE -eq 0) { $fmsSourceTreeDirty = ($fmsGitStatus.Count -gt 0) }
    }
}
$fmsSignature = Get-AuthenticodeSignature -LiteralPath $fmsExecutable
$fmsBuildInfo = [ordered]@{
    schemaVersion = 1
    application = 'Forts Mod Studio'
    version = $fmsVersion
    assemblyFileVersion = $fmsFileVersion.ToString()
    sourceRepository = 'https://github.com/Yorodoshi/Forts-Mod-Studio'
    sourceCommit = $fmsSourceCommit
    sourceTreeDirty = $fmsSourceTreeDirty
    builtAtUtc = [DateTime]::UtcNow.ToString('o')
    compiler = 'Microsoft .NET Framework C# compiler'
    compilerFileVersion = (Get-Item -LiteralPath $fmsCompiler).VersionInfo.FileVersion
    target = [ordered]@{ operatingSystem = 'Windows'; architecture = 'x64'; framework = '.NET Framework 4.x'; languageVersion = 'C# 5' }
    binary = [ordered]@{
        name = [IO.Path]::GetFileName($fmsExecutable)
        sizeBytes = (Get-Item -LiteralPath $fmsExecutable).Length
        sha256 = (Get-FileHash -LiteralPath $fmsExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
        authenticode = [ordered]@{
            status = $fmsSignature.Status.ToString()
            hasSignature = ($null -ne $fmsSignature.SignerCertificate)
            signerThumbprint = $(if ($null -ne $fmsSignature.SignerCertificate) { $fmsSignature.SignerCertificate.Thumbprint } else { $null })
        }
    }
}
if ($env:GITHUB_ACTIONS -eq 'true' -and $env:GITHUB_REPOSITORY -and $env:GITHUB_RUN_ID) {
    $fmsBuildInfo.workflowRun = '{0}/{1}/actions/runs/{2}' -f $env:GITHUB_SERVER_URL, $env:GITHUB_REPOSITORY, $env:GITHUB_RUN_ID
}
$fmsBuildInfo | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $fmsOutput 'build-info.json') -Encoding UTF8
Write-Output $fmsExecutable
