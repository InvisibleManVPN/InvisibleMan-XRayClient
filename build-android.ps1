#Requires -Version 5.1

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipNativeBridge,

    [switch]$SkipGeoFiles,

    [switch]$NoPublish,

    [string]$OutputDir = ".\publish-android",

    [ValidateSet("arm64-v8a", "x86_64")]
    [string]$Abi = "arm64-v8a",

    [int]$AndroidApiLevel = 24,

    [int]$AndroidTargetSdk = 34,

    [string]$AndroidBuildToolsVersion = "34.0.0",

    [string]$AndroidNdkVersion = "26.3.11579264",

    [string]$AndroidSdkDirectory = "",

    [string]$AndroidNdkDirectory = "",

    [string]$KeystorePath = "",

    [string]$KeyAlias = "",

    [string]$SigningSecretEnvName = "ANDROID_SIGNING_PASSWORD"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RootDir = $PSScriptRoot
$WrapperDir = Join-Path $RootDir "XRay-Wrapper"
$AndroidProjectDir = Join-Path $RootDir "InvisibleGorilla-XRay.Android"
$AndroidProjectFile = Join-Path $AndroidProjectDir "InvisibleGorilla-XRay.Android.csproj"
$RuntimeDir = Join-Path $AndroidProjectDir "Assets\Runtime"
$AbsoluteOutputDir = [System.IO.Path]::GetFullPath((Join-Path $RootDir $OutputDir))

$GeoIpUrl = "https://github.com/v2fly/geoip/releases/latest/download/geoip.dat"
$GeoSiteUrl = "https://github.com/v2fly/domain-list-community/releases/latest/download/dlc.dat"
$AndroidCmdlineToolsUrl = "https://dl.google.com/android/repository/commandlinetools-win-11391160_latest.zip"
$TemurinInstallerUrl = "https://api.adoptium.net/v3/installer/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse"
$TemurinWingetId = "EclipseAdoptium.Temurin.17.JDK"
$script:DotNetCommand = $null

if ([string]::IsNullOrWhiteSpace($AndroidSdkDirectory)) {
    if (-not [string]::IsNullOrWhiteSpace($env:ANDROID_SDK_ROOT)) {
        $AndroidSdkDirectory = $env:ANDROID_SDK_ROOT
    }
    elseif (-not [string]::IsNullOrWhiteSpace($env:ANDROID_HOME)) {
        $AndroidSdkDirectory = $env:ANDROID_HOME
    }
    else {
        $AndroidSdkDirectory = Join-Path $env:LOCALAPPDATA "Android\Sdk"
    }
}

if ([string]::IsNullOrWhiteSpace($AndroidNdkDirectory) -and -not [string]::IsNullOrWhiteSpace($env:ANDROID_NDK_ROOT)) {
    $AndroidNdkDirectory = $env:ANDROID_NDK_ROOT
}

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
}

function Write-Info {
    param([string]$Message)
    Write-Host "[..] $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Err {
    param([string]$Message)
    Write-Host "[!!] $Message" -ForegroundColor Red
}

function Test-Command {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Update-SessionPath {
    $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$userPath;$machinePath"
}

function New-DirectoryIfMissing {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Set-UserEnvironmentVariable {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Value
    )

    Set-Item -Path "Env:$Name" -Value $Value
    [Environment]::SetEnvironmentVariable($Name, $Value, "User")
}

function Add-PathEntry {
    param([string]$Entry)

    if ([string]::IsNullOrWhiteSpace($Entry) -or -not (Test-Path $Entry)) {
        return
    }

    $sessionEntries = $env:Path -split ';'
    if ($sessionEntries -notcontains $Entry) {
        $env:Path = "$Entry;$env:Path"
    }

    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $userEntries = if ([string]::IsNullOrWhiteSpace($userPath)) { @() } else { $userPath -split ';' }
    if ($userEntries -notcontains $Entry) {
        $newUserPath = if ([string]::IsNullOrWhiteSpace($userPath)) { $Entry } else { "$Entry;$userPath" }
        [Environment]::SetEnvironmentVariable("Path", $newUserPath, "User")
    }
}

function Get-RemoteFile {
    param(
        [Parameter(Mandatory)][string]$Uri,
        [Parameter(Mandatory)][string]$Destination
    )

    New-DirectoryIfMissing (Split-Path $Destination -Parent)
    Write-Info "Downloading $(Split-Path $Destination -Leaf)..."
    Invoke-WebRequest -Uri $Uri -OutFile $Destination -UseBasicParsing
}

function Invoke-ExternalProcess {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string]$ArgumentList = "",
        [switch]$NoNewWindow
    )

    $startInfo = @{
        FilePath = $FilePath
        Wait = $true
        PassThru = $true
    }

    if (-not [string]::IsNullOrWhiteSpace($ArgumentList)) {
        $startInfo["ArgumentList"] = $ArgumentList
    }

    if ($NoNewWindow) {
        $startInfo["NoNewWindow"] = $true
    }

    $process = Start-Process @startInfo
    if ($null -eq $process) {
        throw "Failed to start process: $FilePath"
    }

    if ($process.ExitCode -ne 0) {
        throw "Process failed with exit code $($process.ExitCode): $FilePath $ArgumentList"
    }
}

function Join-ProcessArguments {
    param([string[]]$Arguments)

    $escaped = foreach ($argument in $Arguments) {
        if ($null -eq $argument) {
            '""'
            continue
        }

        if ($argument -match '[\s"]') {
            '"' + ($argument -replace '"', '\"') + '"'
        }
        else {
            $argument
        }
    }

    return [string]::Join(' ', $escaped)
}

function Invoke-ProcessCapture {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string]$ArgumentList = "",
        [string]$StandardInput = ""
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = $ArgumentList
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.RedirectStandardInput = $PSBoundParameters.ContainsKey("StandardInput")
    $psi.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi

    [void]$process.Start()

    if ($PSBoundParameters.ContainsKey("StandardInput")) {
        $process.StandardInput.Write($StandardInput)
        $process.StandardInput.Close()
    }

    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        StdOut = $stdout
        StdErr = $stderr
        Output = ($stdout + $stderr).Trim()
    }
}

function Resolve-DotNetCommand {
    if (-not [string]::IsNullOrWhiteSpace($script:DotNetCommand) -and (Test-Path $script:DotNetCommand)) {
        return $script:DotNetCommand
    }

    $candidates = @()
    $localDotNet = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe"
    $systemDotNet = "C:\Program Files\dotnet\dotnet.exe"

    if (Test-Path $localDotNet) {
        $candidates += $localDotNet
    }

    if (Test-Command "dotnet") {
        $resolved = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
        if (-not [string]::IsNullOrWhiteSpace($resolved)) {
            $candidates += $resolved
        }
    }

    if (Test-Path $systemDotNet) {
        $candidates += $systemDotNet
    }

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (Test-Path $candidate) {
            $script:DotNetCommand = $candidate
            return $candidate
        }
    }

    throw "dotnet.exe was not found."
}

function Get-DotNetSdkList {
    try {
        $dotNet = Resolve-DotNetCommand
    }
    catch {
        return @()
    }

    $result = Invoke-ProcessCapture -FilePath $dotNet -ArgumentList "--list-sdks"
    if ($result.ExitCode -ne 0) {
        return @()
    }

    return @($result.Output -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Install-Go {
    Write-Info "Checking latest Go release..."

    try {
        $dlInfo = Invoke-RestMethod -Uri "https://go.dev/dl/?mode=json" -UseBasicParsing
        $latest = $dlInfo[0]
        $goVersion = $latest.version

        $file = $latest.files | Where-Object {
            $_.os -eq "windows" -and $_.arch -eq "amd64" -and $_.kind -eq "installer"
        } | Select-Object -First 1

        if (-not $file) {
            $file = $latest.files | Where-Object {
                $_.os -eq "windows" -and $_.arch -eq "amd64"
            } | Select-Object -First 1
        }

        $downloadUrl = "https://go.dev/dl/$($file.filename)"
    }
    catch {
        $goVersion = "go1.23.6"
        $downloadUrl = "https://go.dev/dl/go1.23.6.windows-amd64.msi"
        Write-Info "Go API lookup failed, using fallback $goVersion"
    }

    $msiPath = Join-Path $env:TEMP "$goVersion.windows-amd64.msi"
    Get-RemoteFile -Uri $downloadUrl -Destination $msiPath

    Write-Info "Installing $goVersion..."
    Invoke-ExternalProcess -FilePath "msiexec.exe" -ArgumentList "/i `"$msiPath`" /quiet /norestart"
    Remove-Item $msiPath -Force -ErrorAction SilentlyContinue

    Update-SessionPath

    $goRoot = "C:\Program Files\Go\bin"
    if (Test-Path $goRoot) {
        Add-PathEntry $goRoot
    }

    if (-not (Test-Command "go")) {
        throw "Go was installed but is still unavailable in PATH. Restart the terminal and rerun the script."
    }

    Write-Success "Go installed"
}

function Install-DotNetSdk {
    param([string]$Channel = "8.0")

    $installScript = Join-Path $env:TEMP "dotnet-install.ps1"
    Get-RemoteFile -Uri "https://dot.net/v1/dotnet-install.ps1" -Destination $installScript

    $installDir = "$env:LOCALAPPDATA\Microsoft\dotnet"
    Write-Info "Installing .NET SDK $Channel..."
    & $installScript -Channel $Channel -InstallDir $installDir

    Remove-Item $installScript -Force -ErrorAction SilentlyContinue

    if (-not (Test-Path $installDir)) {
        throw ".NET SDK install directory was not created: $installDir"
    }

    $script:DotNetCommand = Join-Path $installDir "dotnet.exe"
    Add-PathEntry $installDir
    Update-SessionPath

    if (-not (Test-Path $script:DotNetCommand)) {
        throw ".NET SDK was installed but dotnet.exe was not found in $installDir."
    }

    Write-Success ".NET SDK $Channel installed"
}

function Ensure-Go {
    if (Test-Command "go") {
        $goVersion = & go version 2>&1 | Select-String -Pattern 'go\d+\.\d+(\.\d+)?' | ForEach-Object { $_.Matches[0].Value } | Select-Object -First 1
        Write-Success "Go $goVersion"
        return
    }

    Write-Info "Go is missing, installing..."
    Install-Go
}

function Ensure-DotNet {
    $sdkList = Get-DotNetSdkList
    $has8 = $sdkList | Select-String -Pattern '^8\.'
    if ($has8) {
        $version = ($has8 | Select-Object -First 1).ToString().Split(' ')[0]
        Write-Success ".NET SDK $version"
        return
    }

    if ($sdkList.Count -gt 0) {
        Write-Info ".NET SDK found, but 8.x is missing"
    }
    else {
        Write-Info ".NET SDK is missing, installing..."
    }

    Install-DotNetSdk -Channel "8.0"

    $sdkList = Get-DotNetSdkList
    $has8 = $sdkList | Select-String -Pattern '^8\.'
    if (-not $has8) {
        throw ".NET 8 SDK install completed, but the script still cannot find an 8.x SDK."
    }
}

function Get-JavaMajorVersion {
    $javaExecutable = $null
    if (-not [string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
        $javaFromHome = Join-Path $env:JAVA_HOME "bin\java.exe"
        if (Test-Path $javaFromHome) {
            $javaExecutable = $javaFromHome
        }
    }

    if (-not $javaExecutable) {
        $resolvedJavaHome = Resolve-JavaHome
        if (-not [string]::IsNullOrWhiteSpace($resolvedJavaHome)) {
            $javaFromResolvedHome = Join-Path $resolvedJavaHome "bin\java.exe"
            if (Test-Path $javaFromResolvedHome) {
                $javaExecutable = $javaFromResolvedHome
            }
        }
    }

    if (-not $javaExecutable) {
        $javaCommand = Get-Command "java" -ErrorAction SilentlyContinue
        if ($null -ne $javaCommand) {
            $javaExecutable = $javaCommand.Source
        }
    }

    if ([string]::IsNullOrWhiteSpace($javaExecutable)) {
        return 0
    }

    $result = Invoke-ProcessCapture -FilePath $javaExecutable -ArgumentList "-version"
    if ($result.ExitCode -ne 0) {
        return 0
    }

    $match = [regex]::Match($result.Output, 'version "(?<version>[^"]+)"')
    if (-not $match.Success) {
        return 0
    }

    $versionText = $match.Groups["version"].Value
    if ($versionText.StartsWith("1.")) {
        return [int]($versionText.Split('.')[1])
    }

    return [int]($versionText.Split('.')[0])
}

function Resolve-JavaHome {
    if (-not [string]::IsNullOrWhiteSpace($env:JAVA_HOME)) {
        $envJava = Join-Path $env:JAVA_HOME "bin\java.exe"
        if (Test-Path $envJava) {
            return $env:JAVA_HOME
        }
    }

    $roots = @(
        "C:\Program Files\Eclipse Adoptium",
        "C:\Program Files\Microsoft",
        "C:\Program Files\Java"
    )

    $candidates = @()
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) {
            continue
        }

        $candidates += Get-ChildItem $root -Directory -ErrorAction SilentlyContinue | Where-Object {
            Test-Path (Join-Path $_.FullName "bin\java.exe")
        }
    }

    $selected = $candidates | Sort-Object Name -Descending | Select-Object -First 1
    if ($selected) {
        return $selected.FullName
    }

    return $null
}

function Configure-JavaHome {
    param([Parameter(Mandatory)][string]$JavaHome)

    Set-UserEnvironmentVariable -Name "JAVA_HOME" -Value $JavaHome
    Add-PathEntry (Join-Path $JavaHome "bin")
    Update-SessionPath
}

function Install-Java {
    if (Test-Command "winget") {
        Write-Info "Installing Temurin JDK 17 via winget..."
        $wingetArgs = Join-ProcessArguments @(
            "install",
            "-e",
            "--id", $TemurinWingetId,
            "--accept-source-agreements",
            "--accept-package-agreements",
            "--silent",
            "--disable-interactivity"
        )
        $wingetResult = Invoke-ProcessCapture -FilePath "winget" -ArgumentList $wingetArgs

        if ($wingetResult.ExitCode -ne 0) {
            Write-Info "winget install failed, trying direct Temurin installer..."
        }
        else {
            Update-SessionPath
        }
    }

    if ((Get-JavaMajorVersion) -ge 11) {
        $javaHome = Resolve-JavaHome
        if (-not [string]::IsNullOrWhiteSpace($javaHome)) {
            Configure-JavaHome -JavaHome $javaHome
        }

        Write-Success "JDK installed"
        return
    }

    $installerPath = Join-Path $env:TEMP "temurin-17-jdk.msi"
    Get-RemoteFile -Uri $TemurinInstallerUrl -Destination $installerPath

    Write-Info "Installing Temurin JDK 17 via MSI..."
    Invoke-ExternalProcess -FilePath "msiexec.exe" -ArgumentList "/i `"$installerPath`" /quiet /norestart"
    Remove-Item $installerPath -Force -ErrorAction SilentlyContinue

    Update-SessionPath
    $resolvedJavaHome = Resolve-JavaHome
    if ([string]::IsNullOrWhiteSpace($resolvedJavaHome)) {
        throw "JDK install finished, but JAVA_HOME could not be resolved automatically."
    }

    Configure-JavaHome -JavaHome $resolvedJavaHome

    if ((Get-JavaMajorVersion) -lt 11) {
        throw "JDK install finished, but java is still unavailable."
    }

    Write-Success "JDK installed"
}

function Ensure-Java {
    $javaMajor = Get-JavaMajorVersion
    if ($javaMajor -ge 11) {
        $javaHome = Resolve-JavaHome
        if (-not [string]::IsNullOrWhiteSpace($javaHome)) {
            Configure-JavaHome -JavaHome $javaHome
        }

        Write-Success "JDK $javaMajor"
        return
    }

    Write-Info "JDK 11+ is missing, installing..."
    Install-Java
}

function Resolve-SdkManagerPath {
    $candidates = @(
        (Join-Path $AndroidSdkDirectory "cmdline-tools\latest\bin\sdkmanager.bat"),
        (Join-Path $AndroidSdkDirectory "cmdline-tools\latest\bin\sdkmanager")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

function Configure-AndroidEnvironment {
    New-DirectoryIfMissing $AndroidSdkDirectory

    Set-UserEnvironmentVariable -Name "ANDROID_SDK_ROOT" -Value $AndroidSdkDirectory
    Set-UserEnvironmentVariable -Name "ANDROID_HOME" -Value $AndroidSdkDirectory

    Add-PathEntry (Join-Path $AndroidSdkDirectory "platform-tools")
    Add-PathEntry (Join-Path $AndroidSdkDirectory "cmdline-tools\latest\bin")

    if (-not [string]::IsNullOrWhiteSpace($AndroidNdkDirectory) -and (Test-Path $AndroidNdkDirectory)) {
        Set-UserEnvironmentVariable -Name "ANDROID_NDK_ROOT" -Value $AndroidNdkDirectory
    }

    Update-SessionPath
}

function Install-AndroidCommandLineTools {
    Write-Info "Installing Android command-line tools..."

    New-DirectoryIfMissing $AndroidSdkDirectory
    $zipPath = Join-Path $env:TEMP "android-commandlinetools.zip"
    $extractDir = Join-Path $env:TEMP ("android-cmdline-tools-" + [guid]::NewGuid().ToString("N"))

    Get-RemoteFile -Uri $AndroidCmdlineToolsUrl -Destination $zipPath
    New-DirectoryIfMissing $extractDir

    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

    $sourceRoot = Join-Path $extractDir "cmdline-tools"
    if (-not (Test-Path $sourceRoot)) {
        $sourceRoot = (Get-ChildItem -Path $extractDir -Directory | Select-Object -First 1).FullName
    }

    if ([string]::IsNullOrWhiteSpace($sourceRoot) -or -not (Test-Path $sourceRoot)) {
        throw "Unable to locate extracted Android command-line tools."
    }

    $cmdlineToolsDir = Join-Path $AndroidSdkDirectory "cmdline-tools"
    $latestDir = Join-Path $cmdlineToolsDir "latest"

    New-DirectoryIfMissing $cmdlineToolsDir
    if (Test-Path $latestDir) {
        Remove-Item $latestDir -Recurse -Force
    }

    New-DirectoryIfMissing $latestDir
    Copy-Item -Path (Join-Path $sourceRoot "*") -Destination $latestDir -Recurse -Force

    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item $extractDir -Recurse -Force -ErrorAction SilentlyContinue

    Configure-AndroidEnvironment

    if (-not (Resolve-SdkManagerPath)) {
        throw "Android command-line tools were installed, but sdkmanager is still unavailable."
    }

    Write-Success "Android command-line tools installed"
}

function Get-SdkManager {
    $sdkManager = Resolve-SdkManagerPath
    if (-not $sdkManager) {
        Install-AndroidCommandLineTools
        $sdkManager = Resolve-SdkManagerPath
    }

    if (-not $sdkManager) {
        throw "sdkmanager was not found after installing Android command-line tools."
    }

    return $sdkManager
}

function Invoke-SdkManager {
    param([string[]]$Arguments)

    $sdkManager = Get-SdkManager
    $sdkArgs = @("--sdk_root=$AndroidSdkDirectory") + $Arguments
    $sdkArgLine = Join-ProcessArguments $sdkArgs

    $result = Invoke-ProcessCapture -FilePath $sdkManager -ArgumentList $sdkArgLine
    if ($result.ExitCode -ne 0) {
        throw "sdkmanager failed with exit code $($result.ExitCode). $($result.Output)"
    }
}

function Accept-AndroidLicenses {
    Write-Info "Accepting Android SDK licenses..."

    $sdkManager = Get-SdkManager
    $answers = ((1..200) | ForEach-Object { "y" }) -join [Environment]::NewLine

    $licenseArgs = Join-ProcessArguments @("--sdk_root=$AndroidSdkDirectory", "--licenses")
    $result = Invoke-ProcessCapture -FilePath $sdkManager -ArgumentList $licenseArgs -StandardInput $answers
    if ($result.ExitCode -ne 0) {
        throw "Failed to accept Android SDK licenses."
    }
}

function Resolve-InstalledNdkDirectory {
    if (-not [string]::IsNullOrWhiteSpace($AndroidNdkDirectory) -and (Test-Path $AndroidNdkDirectory)) {
        return [System.IO.Path]::GetFullPath($AndroidNdkDirectory)
    }

    $preferred = Join-Path $AndroidSdkDirectory "ndk\$AndroidNdkVersion"
    if (Test-Path $preferred) {
        return $preferred
    }

    $ndkParent = Join-Path $AndroidSdkDirectory "ndk"
    if (Test-Path $ndkParent) {
        $latest = Get-ChildItem -Path $ndkParent -Directory -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
        if ($latest) {
            return $latest.FullName
        }
    }

    return $null
}

function Ensure-AndroidToolchain {
    param([switch]$NeedNdk)

    Ensure-Java
    Configure-AndroidEnvironment
    $null = Get-SdkManager
    Accept-AndroidLicenses

    $packages = @(
        "platform-tools",
        "platforms;android-$AndroidTargetSdk",
        "build-tools;$AndroidBuildToolsVersion"
    )

    if ($NeedNdk -and [string]::IsNullOrWhiteSpace((Resolve-InstalledNdkDirectory))) {
        $packages += "ndk;$AndroidNdkVersion"
    }

    if ($packages.Count -gt 0) {
        Write-Info "Installing Android SDK packages..."
        Invoke-SdkManager -Arguments (@("--install") + $packages)
    }

    $resolvedNdk = Resolve-InstalledNdkDirectory
    if ($NeedNdk -and [string]::IsNullOrWhiteSpace($resolvedNdk)) {
        throw "Android NDK could not be resolved after installation."
    }

    if (-not [string]::IsNullOrWhiteSpace($resolvedNdk)) {
        $script:AndroidNdkDirectory = $resolvedNdk
        Set-UserEnvironmentVariable -Name "ANDROID_NDK_ROOT" -Value $resolvedNdk
    }

    Configure-AndroidEnvironment
    Write-Success "Android SDK toolchain is ready"
}

function Ensure-AndroidWorkload {
    $dotNet = Resolve-DotNetCommand
    $workloadResult = Invoke-ProcessCapture -FilePath $dotNet -ArgumentList "workload list"
    $workloadLines = $workloadResult.Output -split "\r?\n"
    if ($workloadResult.ExitCode -eq 0 -and ($workloadLines | Select-String -Pattern '^\s*android\s')) {
        Write-Success ".NET Android workload"
        return
    }

    Write-Info "Installing .NET Android workload..."
    $installResult = Invoke-ProcessCapture -FilePath $dotNet -ArgumentList "workload install android"

    if ($installResult.ExitCode -ne 0) {
        throw "Failed to install .NET Android workload. $($installResult.Output)"
    }

    Write-Success ".NET Android workload installed"
}

function Test-Prerequisites {
    Write-Step "Checking and installing prerequisites"

    $needGo = -not $SkipNativeBridge
    $needDotNet = -not $NoPublish
    $needJava = (-not $NoPublish) -or (-not $SkipNativeBridge)
    $needNdk = -not $SkipNativeBridge
    $needAndroidSdk = (-not $NoPublish) -or $needNdk

    if ($needGo) {
        Ensure-Go
    }

    if ($needDotNet) {
        Ensure-DotNet
        Ensure-AndroidWorkload
    }

    if ($needJava) {
        Ensure-Java
    }

    if ($needAndroidSdk) {
        Ensure-AndroidToolchain -NeedNdk:$needNdk
    }

    Write-Success "All prerequisites are ready"
}

function Get-GeoFiles {
    Write-Step "Preparing geo files"

    New-DirectoryIfMissing $RuntimeDir
    Get-RemoteFile -Uri $GeoIpUrl -Destination (Join-Path $RuntimeDir "geoip.dat")
    Get-RemoteFile -Uri $GeoSiteUrl -Destination (Join-Path $RuntimeDir "geosite.dat")

    Write-Success "Android runtime geo files are ready"
}

function Resolve-AndroidClang {
    param(
        [Parameter(Mandatory)][string]$NdkRoot,
        [Parameter(Mandatory)][int]$ApiLevel,
        [Parameter(Mandatory)][string]$TargetTriple
    )

    $toolchainDir = Join-Path $NdkRoot "toolchains\llvm\prebuilt\windows-x86_64\bin"
    $candidates = @(
        (Join-Path $toolchainDir "$TargetTriple$ApiLevel-clang.cmd"),
        (Join-Path $toolchainDir "$TargetTriple$ApiLevel-clang.exe"),
        (Join-Path $toolchainDir "$TargetTriple$ApiLevel-clang")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Unable to find Android clang for API $ApiLevel in $toolchainDir"
}

function Invoke-NativeBridgeBuild {
    Write-Step "Building libXRayCore.so"

    if ([string]::IsNullOrWhiteSpace($AndroidNdkDirectory) -or -not (Test-Path $AndroidNdkDirectory)) {
        throw "ANDROID_NDK_ROOT is not configured. The prerequisite step should have installed it automatically."
    }

    New-DirectoryIfMissing $RuntimeDir

    Push-Location $WrapperDir
    try {
        $oldGoos = $env:GOOS
        $oldGoarch = $env:GOARCH
        $oldGoamd64 = $env:GOAMD64
        $oldCgo = $env:CGO_ENABLED
        $oldCc = $env:CC

        $targets = @(
            @{
                Label = "arm64-v8a"
                GoArch = "arm64"
                TargetTriple = "aarch64-linux-android"
                OutputDir = (Join-Path $RuntimeDir "arm64-v8a")
            },
            @{
                Label = "x86_64"
                GoArch = "amd64"
                TargetTriple = "x86_64-linux-android"
                OutputDir = (Join-Path $RuntimeDir "x86_64")
            }
        )

        foreach ($target in $targets) {
            $clang = Resolve-AndroidClang `
                -NdkRoot $AndroidNdkDirectory `
                -ApiLevel $AndroidApiLevel `
                -TargetTriple $target.TargetTriple

            New-DirectoryIfMissing $target.OutputDir

            $env:GOOS = "android"
            $env:GOARCH = $target.GoArch
            $env:CGO_ENABLED = "1"
            $env:CC = $clang

            if ($target.GoArch -eq "amd64") {
                $env:GOAMD64 = "v1"
            }
            else {
                Remove-Item Env:GOAMD64 -ErrorAction SilentlyContinue
            }

            $nativeLibPath = Join-Path $target.OutputDir "libXRayCore.so"
            Remove-Item $nativeLibPath -Force -ErrorAction SilentlyContinue

            & go build --buildmode=c-shared `
                -o $nativeLibPath `
                -trimpath `
                -ldflags "-s -w -buildid=" .

            if ($LASTEXITCODE -ne 0) {
                throw "go build failed for $($target.Label) with exit code $LASTEXITCODE"
            }

            Remove-Item (Join-Path $target.OutputDir "libXRayCore.h") -Force -ErrorAction SilentlyContinue
            Write-Success "libXRayCore native library created for $($target.Label)"
        }

        Remove-Item (Join-Path $RuntimeDir "libXRayCore.bin") -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $RuntimeDir "libXRayCore.so") -Force -ErrorAction SilentlyContinue
        Remove-Item (Join-Path $RuntimeDir "libXRayCore.so.asset-id") -Force -ErrorAction SilentlyContinue
    }
    finally {
        $env:GOOS = $oldGoos
        $env:GOARCH = $oldGoarch
        if ([string]::IsNullOrWhiteSpace($oldGoamd64)) {
            Remove-Item Env:GOAMD64 -ErrorAction SilentlyContinue
        }
        else {
            $env:GOAMD64 = $oldGoamd64
        }
        $env:CGO_ENABLED = $oldCgo
        $env:CC = $oldCc
        Pop-Location
    }
}

function Convert-AndroidAbiToRuntimeIdentifier {
    param([Parameter(Mandatory)][string]$AndroidAbi)

    switch ($AndroidAbi) {
        "arm64-v8a" { return "android-arm64" }
        "x86_64" { return "android-x64" }
        default { throw "Unsupported Android ABI: $AndroidAbi" }
    }
}

function Publish-AndroidApk {
    Write-Step "Publishing Android APK"

    $dotNet = Resolve-DotNetCommand
    New-DirectoryIfMissing $AbsoluteOutputDir
    $runtimeIdentifier = Convert-AndroidAbiToRuntimeIdentifier -AndroidAbi $Abi

    $publishArgs = @(
        "publish",
        $AndroidProjectFile,
        "-f", "net8.0-android",
        "-c", $Configuration,
        "-r", $runtimeIdentifier,
        "-o", $AbsoluteOutputDir,
        "-p:AndroidPackageFormats=apk",
        "-p:AndroidSdkDirectory=$AndroidSdkDirectory"
    )

    if (-not [string]::IsNullOrWhiteSpace($env:JAVA_HOME) -and (Test-Path $env:JAVA_HOME)) {
        $publishArgs += "-p:JavaSdkDirectory=$env:JAVA_HOME"
    }

    if (-not [string]::IsNullOrWhiteSpace($KeystorePath)) {
        if ([string]::IsNullOrWhiteSpace($KeyAlias)) {
            throw "When -KeystorePath is provided, -KeyAlias is also required."
        }

        $publishArgs += @(
            "-p:AndroidKeyStore=true",
            "-p:AndroidSigningKeyStore=$KeystorePath",
            "-p:AndroidSigningKeyAlias=$KeyAlias",
            "-p:AndroidSigningKeyPass=env:$SigningSecretEnvName",
            "-p:AndroidSigningStorePass=env:$SigningSecretEnvName"
        )
    }

    $publishResult = Invoke-ProcessCapture -FilePath $dotNet -ArgumentList (Join-ProcessArguments $publishArgs)
    if ($publishResult.ExitCode -ne 0) {
        throw "dotnet publish failed with exit code $($publishResult.ExitCode). $($publishResult.Output)"
    }

    Write-Success "APK output is available in $AbsoluteOutputDir"
}

Write-Host ""
Write-Host "  Invisible Gorilla XRay :: Android Build Script" -ForegroundColor Magenta
Write-Host ""

if (-not (Test-Path $AndroidProjectFile)) {
    throw "Android project not found: $AndroidProjectFile"
}

Test-Prerequisites

New-DirectoryIfMissing $RuntimeDir

if (-not $SkipGeoFiles) {
    Get-GeoFiles
}
else {
    Write-Info "Skipping geo files"
}

if (-not $SkipNativeBridge) {
    Invoke-NativeBridgeBuild
}
else {
    Write-Info "Skipping native bridge build"
}

if (-not $NoPublish) {
    Publish-AndroidApk
}
else {
    Write-Info "Skipping APK publish"
}

Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Green
Write-Host "  Android build script completed" -ForegroundColor Green
Write-Host ("=" * 60) -ForegroundColor Green
