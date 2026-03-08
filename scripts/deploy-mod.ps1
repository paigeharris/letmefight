[CmdletBinding()]
param(
    [string]$ModulesPath = "${env:ProgramFiles(x86)}\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules",
    [string]$Configuration = "Debug",
    [string]$Platform = "x64",
    [string]$Branch,
    [string]$Version,
    [Nullable[int]]$VersionSuffix,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Get-GitOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & git -C $RepositoryRoot @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return ($output | Out-String).Trim()
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $Content, $utf8NoBom)
}

function Get-BannerlordVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ModulesPath
    )

    $bannerlordRoot = Split-Path -Parent $ModulesPath
    $versionXmlPath = Join-Path $bannerlordRoot "bin\Win64_Shipping_Client\Version.xml"
    if (Test-Path -LiteralPath $versionXmlPath) {
        [xml]$versionXml = Get-Content -LiteralPath $versionXmlPath
        $version = $versionXml.Version.Singleplayer.Value
        if (-not [string]::IsNullOrWhiteSpace($version)) {
            return $version.Trim()
        }
    }

    $nativeModulePath = Join-Path $ModulesPath "Native\SubModule.xml"
    if (Test-Path -LiteralPath $nativeModulePath) {
        [xml]$nativeModule = Get-Content -LiteralPath $nativeModulePath
        $version = $nativeModule.Module.Version.value
        if (-not [string]::IsNullOrWhiteSpace($version)) {
            return $version.Trim()
        }
    }

    throw "Could not determine the installed Bannerlord version from $versionXmlPath or $nativeModulePath."
}

function Get-ModuleVersionFromBannerlord {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BannerlordVersion,

        [Parameter(Mandatory = $true)]
        [int]$VersionSuffix
    )

    $normalizedVersion = $BannerlordVersion.Trim()
    if ($normalizedVersion.StartsWith("v")) {
        $normalizedVersion = $normalizedVersion.Substring(1)
    }

    $parts = @($normalizedVersion.Split('.') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($parts.Count -lt 3) {
        throw "Bannerlord version '$BannerlordVersion' is not in a supported format."
    }

    return "v{0}.{1}.{2}.{3}" -f $parts[0], $parts[1], $parts[2], $VersionSuffix
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "LetMeFight.sln"
$manifestTemplatePath = Join-Path $repoRoot "SubModule.xml"

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Could not find solution at $solutionPath"
}

if (-not (Test-Path -LiteralPath $manifestTemplatePath)) {
    throw "Could not find SubModule.xml template at $manifestTemplatePath"
}

if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = Get-GitOutput -RepositoryRoot $repoRoot -Arguments @("branch", "--show-current")
}

if ([string]::IsNullOrWhiteSpace($Branch)) {
    throw "Could not determine the current git branch."
}

$branchProfiles = @{
    "santasrescue" = @{
        DisplayName = "Just Let Me Fight! - Santa's Rescue"
        SubModuleName = "LetMeFight - Santa's Rescue"
        DeployFolder = "LetMeFight"
        VersionSuffix = 1
    }
}

if (-not $branchProfiles.ContainsKey($Branch)) {
    throw "No deployment profile is configured for branch '$Branch'. Add one in scripts/deploy-mod.ps1."
}

$profile = $branchProfiles[$Branch]
$commit = Get-GitOutput -RepositoryRoot $repoRoot -Arguments @("rev-parse", "--short", "HEAD")
$commitCount = Get-GitOutput -RepositoryRoot $repoRoot -Arguments @("rev-list", "--count", "HEAD")
$isDirty = [bool](Get-GitOutput -RepositoryRoot $repoRoot -Arguments @("status", "--short"))
$deployedAtUtc = (Get-Date).ToUniversalTime()
$bannerlordVersion = Get-BannerlordVersion -ModulesPath $ModulesPath

if ($null -eq $VersionSuffix) {
    $VersionSuffix = [int]$profile.VersionSuffix
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-ModuleVersionFromBannerlord -BannerlordVersion $bannerlordVersion -VersionSuffix $VersionSuffix
}

$stageRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("letmefight-deploy-" + $Branch)
$stageModuleRoot = Join-Path $stageRoot $profile.DeployFolder
$stageBinDir = Join-Path $stageModuleRoot "bin\Win64_Shipping_Client"
$destinationModuleRoot = Join-Path $ModulesPath $profile.DeployFolder
$destinationBinDir = Join-Path $destinationModuleRoot "bin\Win64_Shipping_Client"

if (Test-Path -LiteralPath $stageRoot) {
    Remove-Item -LiteralPath $stageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stageBinDir -Force | Out-Null
New-Item -ItemType Directory -Path $destinationBinDir -Force | Out-Null

if (-not $SkipBuild) {
    Invoke-CheckedCommand -FilePath "dotnet" -Arguments @(
        "build",
        $solutionPath,
        "-p:Configuration=$Configuration",
        "-p:Platform=$Platform",
        "-p:OutputPath=$stageBinDir"
    )
}

$stageDllPath = Join-Path $stageBinDir "LetMeFight.dll"
$stagePdbPath = Join-Path $stageBinDir "LetMeFight.pdb"

if (-not (Test-Path -LiteralPath $stageDllPath)) {
    throw "Build output is missing: $stageDllPath"
}

[xml]$manifest = Get-Content -LiteralPath $manifestTemplatePath
$manifest.Module.Name.value = $profile.DisplayName
$manifest.Module.Version.value = $Version
$manifest.Module.SubModules.SubModule.Name.value = $profile.SubModuleName

$manifestOutputPath = Join-Path $stageModuleRoot "SubModule.xml"
$manifest.Save($manifestOutputPath)

$deployInfo = [ordered]@{
    moduleId = $manifest.Module.Id.value
    moduleName = $profile.DisplayName
    subModuleName = $profile.SubModuleName
    deployFolder = $profile.DeployFolder
    branch = $Branch
    commit = $commit
    commitCount = [int]$commitCount
    dirtyWorktree = $isDirty
    bannerlordVersion = $bannerlordVersion
    versionSuffix = [int]$VersionSuffix
    version = $Version
    configuration = $Configuration
    platform = $Platform
    deployedAtUtc = $deployedAtUtc.ToString("o")
}

$deployInfoPath = Join-Path $stageModuleRoot "deploy-info.json"
Write-Utf8File -Path $deployInfoPath -Content (($deployInfo | ConvertTo-Json -Depth 3) + [Environment]::NewLine)

Copy-Item -LiteralPath $manifestOutputPath -Destination (Join-Path $destinationModuleRoot "SubModule.xml") -Force
Copy-Item -LiteralPath $deployInfoPath -Destination (Join-Path $destinationModuleRoot "deploy-info.json") -Force
Copy-Item -LiteralPath $stageDllPath -Destination (Join-Path $destinationBinDir "LetMeFight.dll") -Force

if (Test-Path -LiteralPath $stagePdbPath) {
    Copy-Item -LiteralPath $stagePdbPath -Destination (Join-Path $destinationBinDir "LetMeFight.pdb") -Force
}

Write-Host ""
Write-Host "Deploy complete."
Write-Host "Branch:       $Branch"
Write-Host "Brand:        $($profile.DisplayName)"
Write-Host "Game version: $bannerlordVersion"
Write-Host "Version:      $Version"
Write-Host "Commit:       $commit"
Write-Host "Destination:  $destinationModuleRoot"
