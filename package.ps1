param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'
$fmsOutput = [IO.Path]::GetFullPath($OutputDirectory)
$fmsExecutable = & (Join-Path $PSScriptRoot 'build.ps1') -OutputDirectory $fmsOutput
if (-not (Test-Path -LiteralPath $fmsExecutable)) { throw 'The build did not produce an executable.' }
$fmsBuildInfoPath = Join-Path $fmsOutput 'build-info.json'
$fmsBuildInfo = Get-Content -LiteralPath $fmsBuildInfoPath -Raw | ConvertFrom-Json
$fmsVersion = $fmsBuildInfo.version
$fmsZipPath = Join-Path $fmsOutput ('Forts-Mod-Studio-{0}-win-x64.zip' -f $fmsVersion)

# The portable archive contains only the editor and its public documentation.
# Game resources and personal mod projects are never packaged.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$fmsPackageFiles = @(
    @{ Path = $fmsExecutable; Name = [IO.Path]::GetFileName($fmsExecutable) },
    @{ Path = (Join-Path $PSScriptRoot 'README.md'); Name = 'README.md' },
    @{ Path = (Join-Path $PSScriptRoot 'README.en.md'); Name = 'README.en.md' },
    @{ Path = (Join-Path $PSScriptRoot 'SECURITY.md'); Name = 'SECURITY.md' },
    @{ Path = $fmsBuildInfoPath; Name = 'build-info.json' }
)
foreach ($fmsPackageFile in $fmsPackageFiles) {
    if (-not (Test-Path -LiteralPath $fmsPackageFile.Path -PathType Leaf)) {
        throw ('A required release file is missing: {0}' -f $fmsPackageFile.Name)
    }
}
$fmsZipStream = [IO.File]::Open($fmsZipPath, [IO.FileMode]::Create, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$fmsArchive = $null
try {
    $fmsArchive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList @($fmsZipStream, [IO.Compression.ZipArchiveMode]::Create, $true)
    foreach ($fmsPackageFile in $fmsPackageFiles) {
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($fmsArchive, $fmsPackageFile.Path, $fmsPackageFile.Name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally {
    if ($null -ne $fmsArchive) { $fmsArchive.Dispose() }
    $fmsZipStream.Dispose()
}
$fmsChecksums = foreach ($fmsReleaseFile in @($fmsExecutable, $fmsZipPath)) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $fmsReleaseFile -Algorithm SHA256).Hash.ToLowerInvariant(), [IO.Path]::GetFileName($fmsReleaseFile)
}
$fmsChecksums | Set-Content -LiteralPath (Join-Path $fmsOutput 'SHA256SUMS.txt') -Encoding ASCII
Write-Output $fmsExecutable
Write-Output $fmsZipPath
