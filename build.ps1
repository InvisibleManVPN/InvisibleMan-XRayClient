#Requires -Version 5.1

<#
.SYNOPSIS
    Скрипт сборки Invisible Gorilla - XRay Client.

.DESCRIPTION
    Автоматизирует полный цикл сборки проекта:
    1. Проверка и автоустановка зависимостей (Go, GCC/MinGW, .NET SDK) напрямую
    2. Сборка Go-обёртки XRayCore.dll
    3. Скачивание geoip.dat и geosite.dat
    4. Скачивание InvisibleGorilla-TUN (опционально)
    5. Сборка/публикация .NET приложения

.PARAMETER Step
    Выполнить конкретный шаг сборки:
    - All       : Все шаги (по умолчанию)
    - GoWrapper : Только сборка Go-обёртки
    - GeoFiles  : Только скачивание geo-файлов
    - TUN       : Только скачивание TUN-сервиса
    - DotNet    : Только сборка .NET приложения

.PARAMETER Configuration
    Конфигурация сборки .NET: Debug или Release. По умолчанию Release.

.PARAMETER Publish
    Если указан, выполняет dotnet publish вместо dotnet build.

.PARAMETER Runtime
    Целевой runtime для публикации. По умолчанию win-x64.

.PARAMETER OutputDir
    Директория для результата публикации. По умолчанию ./publish.

.PARAMETER SkipTUN
    Пропустить скачивание InvisibleGorilla-TUN.

.PARAMETER NoStopRunningApp
    Не закрывать автоматически запущенный Invisible Gorilla XRay перед .NET build/publish.

.EXAMPLE
    .\build.ps1
    Полная сборка со всеми шагами (с автоустановкой зависимостей).

.EXAMPLE
    .\build.ps1 -Publish
    Полная сборка с публикацией в single-file.

.EXAMPLE
    .\build.ps1 -Step GoWrapper
    Только сборка Go-обёртки.

.EXAMPLE
    .\build.ps1 -Step DotNet -Configuration Debug
    Только сборка .NET в режиме Debug.
#>

[CmdletBinding()]
param(
    [ValidateSet("All", "GoWrapper", "GeoFiles", "TUN", "DotNet")]
    [string]$Step = "All",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Publish,

    [string]$Runtime = "win-x64",

    [string]$OutputDir = ".\publish",

    [switch]$SkipTUN,

    [switch]$NoStopRunningApp
)

# ─── Настройки ────────────────────────────────────────────────────────────────

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RootDir      = $PSScriptRoot
$WrapperDir   = Join-Path $RootDir "XRay-Wrapper"
$AppDir       = Join-Path $RootDir "InvisibleGorilla-XRay"
$LibrariesDir = Join-Path $AppDir "Libraries"
$TunDir       = Join-Path $AppDir "TUN"
$SolutionFile = Join-Path $RootDir "InvisibleGorilla-XRay.sln"
$WindowsProjectFile = Join-Path $AppDir "InvisibleGorilla-XRay.csproj"
$LocalTunRepoDir = Join-Path (Split-Path $RootDir -Parent) "InvisibleGorilla-TUN"
$LocalTunBuildScript = Join-Path $LocalTunRepoDir "build.ps1"
$LocalTunProject = Join-Path $LocalTunRepoDir "InvisibleGorilla-TUN\InvisibleGorilla-TUN.csproj"
$LocalTunProjectDir = Split-Path $LocalTunProject -Parent
$LocalTunWrapperDir = Join-Path $LocalTunRepoDir "TUN-Wrapper"

# w64devkit: GCC 14.2.0 — совместим с Go cgo
# GCC 15+ генерирует объектные файлы, которые Go cgo не может распарсить
$W64DevkitVersion = "v2.0.0"
$W64DevkitFile    = "w64devkit-x64-2.0.0.exe"

$GeoIpUrl     = "https://github.com/v2fly/geoip/releases/latest/download/geoip.dat"
$GeoSiteUrl   = "https://github.com/v2fly/domain-list-community/releases/latest/download/dlc.dat"
$TunRelease   = "https://api.github.com/repos/hvkeyn/InvisibleGorilla-TUN/releases/latest"

# ─── Вспомогательные функции ──────────────────────────────────────────────────

function Write-StepHeader {
    param([string]$Message)
    $separator = "=" * 60
    Write-Host ""
    Write-Host $separator -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host $separator -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "[..] $Message" -ForegroundColor Yellow
}

function Write-Err {
    param([string]$Message)
    Write-Host "[!!] $Message" -ForegroundColor Red
}

function Test-Command {
    param([string]$Name)
    $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Update-SessionPath {
    $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath    = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path    = "$machinePath;$userPath"
}

# ─── Визуализация загрузки и установки ────────────────────────────────────────

function Format-FileSize {
    param([long]$Bytes)
    if ($Bytes -ge 1GB) { return "{0:N1} GB" -f ($Bytes / 1GB) }
    if ($Bytes -ge 1MB) { return "{0:N1} MB" -f ($Bytes / 1MB) }
    if ($Bytes -ge 1KB) { return "{0:N1} KB" -f ($Bytes / 1KB) }
    return "$Bytes B"
}

function Invoke-Download {
    param(
        [Parameter(Mandatory)][string]$Uri,
        [Parameter(Mandatory)][string]$OutFile,
        [string]$Label,
        [long]$MinimumSize = 0
    )

    if ($Label) { Write-Info $Label }

    [System.Net.ServicePointManager]::SecurityProtocol =
        [System.Net.SecurityProtocolType]::Tls12 -bor [System.Net.SecurityProtocolType]::Tls11

    $barWidth   = 30
    $blockFull  = [string][char]0x2588
    $blockLight = [string][char]0x2591
    $spinChars  = @(
        [string][char]0x280B, [string][char]0x2819, [string][char]0x2839, [string][char]0x2838,
        [string][char]0x283C, [string][char]0x2834, [string][char]0x2826, [string][char]0x2827,
        [string][char]0x2807, [string][char]0x280F
    )

    $maxRetries = 3
    for ($attempt = 1; $attempt -le $maxRetries; $attempt++) {
        $wc = New-Object System.Net.WebClient
        $wc.Headers.Add("User-Agent", "InvisibleGorilla-BuildScript/1.0")

        $dlState = @{ Received = [long]0; Total = [long]0; Error = $null }
        $srcIdProgress = "DlProg_$([guid]::NewGuid().ToString('N').Substring(0,8))"
        $srcIdComplete = "DlDone_$([guid]::NewGuid().ToString('N').Substring(0,8))"

        Register-ObjectEvent $wc DownloadProgressChanged -SourceIdentifier $srcIdProgress -MessageData $dlState -Action {
            $Event.MessageData.Received = $EventArgs.BytesReceived
            $Event.MessageData.Total    = $EventArgs.TotalBytesToReceive
        } | Out-Null
        Register-ObjectEvent $wc DownloadFileCompleted -SourceIdentifier $srcIdComplete -MessageData $dlState -Action {
            if ($EventArgs.Error) { $Event.MessageData.Error = $EventArgs.Error }
        } | Out-Null

        $startTime = [System.Diagnostics.Stopwatch]::StartNew()

        try {
            $wc.DownloadFileAsync([Uri]$Uri, $OutFile)

            while ($wc.IsBusy) {
                Start-Sleep -Milliseconds 200

                $elapsedSec = $startTime.Elapsed.TotalSeconds
                $speed = if ($elapsedSec -gt 0.1 -and $dlState.Received -gt 0) { $dlState.Received / $elapsedSec } else { 0 }
                $speedStr = "$(Format-FileSize ([long]$speed))/s"

                if ($dlState.Total -gt 0) {
                    $pct = [math]::Min(100, [int]($dlState.Received * 100 / $dlState.Total))
                    $filled = [math]::Floor($barWidth * $pct / 100)
                    $empty = $barWidth - $filled
                    $bar = ($blockFull * $filled) + ($blockLight * $empty)
                    $dlSize = Format-FileSize $dlState.Received
                    $totalSize = Format-FileSize $dlState.Total

                    $etaStr = ""
                    if ($speed -gt 0) {
                        $remainSec = [int][math]::Ceiling(($dlState.Total - $dlState.Received) / $speed)
                        if ($remainSec -ge 0 -and $remainSec -lt 3600) {
                            $etaMins = [int][math]::Floor($remainSec / 60)
                            $etaSecs = $remainSec % 60
                            $etaStr = "  ETA {0}:{1:00}" -f $etaMins, $etaSecs
                        }
                    }
                    $line = "     [$bar] {0,3}%  {1} / {2}  {3}{4}" -f $pct, $dlSize, $totalSize, $speedStr, $etaStr
                }
                elseif ($dlState.Received -gt 0) {
                    $spinIdx = [int]($startTime.Elapsed.TotalMilliseconds / 120) % $spinChars.Length
                    $dlSize = Format-FileSize $dlState.Received
                    $line = "     $($spinChars[$spinIdx]) $dlSize  $speedStr"
                }
                else {
                    $spinIdx = [int]($startTime.Elapsed.TotalMilliseconds / 120) % $spinChars.Length
                    $line = "     $($spinChars[$spinIdx]) connecting..."
                }

                Write-Host "`r$($line.PadRight(90))" -NoNewline -ForegroundColor DarkGray
            }

            Start-Sleep -Milliseconds 100
            if ($dlState.Error) { throw $dlState.Error }

            if ($dlState.Total -eq 0 -and (Test-Path $OutFile)) {
                $dlState.Received = (Get-Item $OutFile).Length
                $dlState.Total = $dlState.Received
            }

            $dlSize = Format-FileSize $dlState.Received
            $elapsedSec = $startTime.Elapsed.TotalSeconds
            $avgSpeed = if ($elapsedSec -gt 0.1 -and $dlState.Received -gt 0) {
                "$(Format-FileSize ([long]($dlState.Received / $elapsedSec)))/s"
            } else { "- " }

            if ($dlState.Total -gt 0) {
                $bar = $blockFull * $barWidth
                $line = "     [$bar] 100%  $dlSize  avg $avgSpeed"
            }
            else {
                $line = "     $dlSize  avg $avgSpeed"
            }
            Write-Host "`r$($line.PadRight(90))" -ForegroundColor DarkGray

            $actualSize = if (Test-Path $OutFile) { (Get-Item $OutFile).Length } else { 0 }
            if ($MinimumSize -gt 0 -and $actualSize -lt $MinimumSize) {
                throw "Download incomplete: $(Format-FileSize $actualSize) received, minimum $(Format-FileSize $MinimumSize)"
            }
            break
        }
        catch {
            Write-Host ""
            if (Test-Path $OutFile) { Remove-Item $OutFile -Force -ErrorAction SilentlyContinue }

            if ($attempt -lt $maxRetries) {
                Write-Info "Ошибка загрузки, повтор ($attempt/$maxRetries)..."
                Start-Sleep -Seconds ($attempt * 2)
            }
            elseif ($attempt -eq $maxRetries) {
                Write-Info "Fallback: Invoke-WebRequest..."
                try {
                    $prevPref = $ProgressPreference
                    $ProgressPreference = 'SilentlyContinue'
                    Invoke-WebRequest -Uri $Uri -OutFile $OutFile -UseBasicParsing
                    $ProgressPreference = $prevPref
                    if (Test-Path $OutFile) {
                        $fbActual = (Get-Item $OutFile).Length
                        if ($MinimumSize -gt 0 -and $fbActual -lt $MinimumSize) {
                            Remove-Item $OutFile -Force -ErrorAction SilentlyContinue
                        }
                        else {
                            Write-Host "     $(Format-FileSize $fbActual)" -ForegroundColor DarkGray
                            break
                        }
                    }
                }
                catch {
                    $ProgressPreference = $prevPref
                }
                throw
            }
        }
        finally {
            Unregister-Event -SourceIdentifier $srcIdProgress -ErrorAction SilentlyContinue
            Unregister-Event -SourceIdentifier $srcIdComplete -ErrorAction SilentlyContinue
            Get-Job -Name $srcIdProgress -ErrorAction SilentlyContinue | Remove-Job -Force -ErrorAction SilentlyContinue
            Get-Job -Name $srcIdComplete -ErrorAction SilentlyContinue | Remove-Job -Force -ErrorAction SilentlyContinue
            $wc.Dispose()
        }
    }
}

function Start-ProcessWithSpinner {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string]$Message,
        [string]$ArgumentList,
        [switch]$NoNewWindow
    )

    $pArgs = @{ FilePath = $FilePath; PassThru = $true }
    if ($ArgumentList) { $pArgs['ArgumentList'] = $ArgumentList }
    if ($NoNewWindow)  { $pArgs['NoNewWindow'] = $true }

    $proc = Start-Process @pArgs
    if ($null -eq $proc) { throw "Failed to start process: $FilePath" }

    $spinChars = @(
        [string][char]0x280B, [string][char]0x2819, [string][char]0x2839, [string][char]0x2838,
        [string][char]0x283C, [string][char]0x2834, [string][char]0x2826, [string][char]0x2827,
        [string][char]0x2807, [string][char]0x280F
    )
    $i = 0

    while (-not $proc.HasExited) {
        $char = $spinChars[$i % $spinChars.Length]
        Write-Host "`r     $char $($Message.PadRight(55))" -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Milliseconds 100
        $i++
    }

    $proc.WaitForExit()
    Write-Host "`r$(' ' * 70)`r" -NoNewline
    $exitCode = $proc.ExitCode
    if ($null -eq $exitCode) { $exitCode = -1 }
    return $exitCode
}

# ─── Установка Go напрямую с go.dev ──────────────────────────────────────────

function Install-Go {
    Write-Info "Запрос последней версии Go..."

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
        Write-Info "Не удалось запросить API, использую $goVersion"
    }

    $msiPath = Join-Path $env:TEMP "$goVersion.windows-amd64.msi"

    Invoke-Download -Uri $downloadUrl -OutFile $msiPath -MinimumSize 20MB `
        -Label "Скачивание $goVersion..."

    $exitCode = Start-ProcessWithSpinner -FilePath "msiexec.exe" `
        -ArgumentList "/i `"$msiPath`" /quiet /norestart" `
        -Message "Установка $goVersion..."

    Remove-Item $msiPath -Force -ErrorAction SilentlyContinue

    if ($exitCode -ne 0) {
        Write-Err "msiexec завершился с кодом $exitCode"
        Write-Err "Попробуйте запустить скрипт от имени администратора"
        exit 1
    }

    Update-SessionPath

    if (-not (Test-Command "go")) {
        $goRoot = "C:\Program Files\Go\bin"
        if (Test-Path $goRoot) {
            $env:Path = "$goRoot;$env:Path"
        }
    }

    Write-Success "$goVersion установлен"
}

# ─── Установка GCC (w64devkit) ────────────────────────────────────────────────

function Install-GCC {
    $installDir   = Join-Path $env:LOCALAPPDATA "w64devkit"
    $downloadUrl  = "https://github.com/skeeto/w64devkit/releases/download/$W64DevkitVersion/$W64DevkitFile"
    $downloadPath = Join-Path $env:TEMP $W64DevkitFile

    Invoke-Download -Uri $downloadUrl -OutFile $downloadPath -MinimumSize 10MB `
        -Label "Скачивание w64devkit $W64DevkitVersion ($W64DevkitFile)..."

    if (Test-Path $installDir) {
        Remove-Item $installDir -Recurse -Force
    }

    if ($W64DevkitFile -match "\.zip$") {
        Write-Info "Распаковка w64devkit..."
        Expand-Archive -Path $downloadPath -DestinationPath $env:LOCALAPPDATA -Force
    }
    elseif ($W64DevkitFile -match "\.exe$") {
        $exitCode = Start-ProcessWithSpinner -FilePath $downloadPath `
            -ArgumentList "-o`"$env:LOCALAPPDATA`" -y" `
            -Message "Распаковка w64devkit..." `
            -NoNewWindow
        if ($exitCode -ne 0) {
            Write-Err "Распаковка 7z завершилась с ошибкой (код: $exitCode)"
            exit 1
        }
    }

    Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

    $binDir = Join-Path $installDir "bin"
    if (Test-Path $binDir) {
        $env:Path = "$binDir;$env:Path"

        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        if ($userPath -notlike "*$binDir*") {
            [Environment]::SetEnvironmentVariable("Path", "$binDir;$userPath", "User")
            Write-Info "w64devkit добавлен в PATH пользователя"
        }
    }

    Write-Success "w64devkit $W64DevkitVersion установлен (GCC)"
}

# ─── Установка .NET SDK через официальный скрипт Microsoft ───────────────────

function Install-DotNetSdk {
    param(
        [string]$Version,
        [string]$Channel = "8.0"
    )

    $installScript = Join-Path $env:TEMP "dotnet-install.ps1"

    Invoke-Download -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -Label "Скачивание dotnet-install.ps1..."

    $installArgs = @{
        InstallDir = "$env:LOCALAPPDATA\Microsoft\dotnet"
    }
    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        Write-Info "Установка .NET SDK $Version..."
        $installArgs["Version"] = $Version
    }
    else {
        Write-Info "Установка .NET SDK $Channel..."
        $installArgs["Channel"] = $Channel
    }

    & $installScript @installArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Err "dotnet-install.ps1 завершился с ошибкой (код: $LASTEXITCODE)"
        exit $LASTEXITCODE
    }

    Remove-Item $installScript -Force -ErrorAction SilentlyContinue

    $dotnetDir = "$env:LOCALAPPDATA\Microsoft\dotnet"
    if (Test-Path $dotnetDir) {
        if ($env:Path -notlike "*$dotnetDir*") {
            $env:Path = "$dotnetDir;$env:Path"
        }

        $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
        if ($userPath -notlike "*$dotnetDir*") {
            [Environment]::SetEnvironmentVariable("Path", "$dotnetDir;$userPath", "User")
            Write-Info "dotnet добавлен в PATH пользователя"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($Version)) {
        Write-Success ".NET SDK $Version установлен"
    }
    else {
        Write-Success ".NET SDK $Channel установлен"
    }
}

function Get-RequiredDotNetSdkVersion {
    $globalJsonPath = Join-Path $RootDir "global.json"
    if (Test-Path $globalJsonPath) {
        try {
            $json = Get-Content $globalJsonPath -Raw | ConvertFrom-Json
            if ($json.sdk -and $json.sdk.version) {
                return [string]$json.sdk.version
            }
        }
        catch {
            Write-Info "Не удалось прочитать global.json, будет использован .NET SDK 8.0"
        }
    }

    return "8.0"
}

function Get-DotNetSdkChannel {
    param([Parameter(Mandatory)][string]$Version)

    if ($Version -match '^(\d+\.\d+)') {
        return $Matches[1]
    }

    return $Version
}

# ─── Проверка и установка зависимостей ────────────────────────────────────────

function Ensure-Go {
    if (Test-Command "go") {
        $goVer = & go version 2>&1 | Select-String -Pattern 'go\d+\.\d+(\.\d+)?' | ForEach-Object { $_.Matches[0].Value }
        Write-Success "Go $goVer"
        return
    }

    Write-Info "Go не найден, начинаю установку..."
    Install-Go

    if (-not (Test-Command "go")) {
        Write-Err "Go установлен, но не найден в PATH."
        Write-Err "Перезапустите терминал и запустите скрипт снова."
        exit 1
    }

    $goVer = & go version 2>&1 | Select-String -Pattern 'go\d+\.\d+(\.\d+)?' | ForEach-Object { $_.Matches[0].Value }
    Write-Success "Go $goVer (установлен)"
}

function Test-GccCompatible {
    if (-not (Test-Command "gcc")) { return $false }
    $ver = (& gcc -dumpfullversion 2>&1).Trim()
    $major = [int]($ver.Split('.')[0])
    return ($major -lt 15)
}

function Ensure-GCC {
    if (Test-Command "gcc") {
        $gccVer = (& gcc -dumpfullversion 2>&1).Trim()
        $gccMajor = [int]($gccVer.Split('.')[0])
        if ($gccMajor -lt 15) {
            Write-Success "GCC $gccVer"
            return
        }
        Write-Info "GCC $gccVer несовместим с Go cgo (GCC 15+), переустановка..."
    }
    else {
        $commonPaths = @(
            "$env:LOCALAPPDATA\w64devkit\bin",
            "C:\mingw64\bin",
            "C:\msys64\mingw64\bin",
            "C:\TDM-GCC-64\bin"
        )

        foreach ($p in $commonPaths) {
            $gccExe = Join-Path $p "gcc.exe"
            if (Test-Path $gccExe) {
                $env:Path = "$p;$env:Path"
                $gccVer = (& gcc -dumpfullversion 2>&1).Trim()
                $gccMajor = [int]($gccVer.Split('.')[0])
                if ($gccMajor -lt 15) {
                    Write-Success "GCC $gccVer (найден в $p)"
                    return
                }
                Write-Info "GCC $gccVer (в $p) несовместим с Go cgo (GCC 15+), переустановка..."
                break
            }
        }

        if (-not (Test-Command "gcc")) {
            Write-Info "GCC (C компилятор) не найден, начинаю установку..."
        }
    }

    Install-GCC

    if (-not (Test-Command "gcc")) {
        Write-Err "GCC установлен, но не найден в PATH."
        Write-Err "Перезапустите терминал и запустите скрипт снова."
        exit 1
    }

    $gccVer = (& gcc -dumpfullversion 2>&1).Trim()
    Write-Success "GCC $gccVer (установлен)"
}

function Ensure-DotNet {
    $requiredSdk = Get-RequiredDotNetSdkVersion
    $requiredChannel = Get-DotNetSdkChannel -Version $requiredSdk

    $dotnetDir = "$env:LOCALAPPDATA\Microsoft\dotnet"
    if ((Test-Path $dotnetDir) -and ($env:Path -notlike "*$dotnetDir*")) {
        $env:Path = "$dotnetDir;$env:Path"
    }

    if (Test-Command "dotnet") {
        $sdkList = & dotnet --list-sdks 2>&1
        $requiredRegex = "^$([regex]::Escape($requiredSdk))\s"
        $matchingSdk = $sdkList | Select-String -Pattern $requiredRegex
        if ($matchingSdk) {
            $ver = ($matchingSdk | Select-Object -First 1).ToString().Split(' ')[0]
            Write-Success ".NET SDK $ver"
            return
        }

        Write-Info ".NET SDK найден, но версия из global.json отсутствует: $requiredSdk"
    }
    else {
        Write-Info ".NET SDK не найден, начинаю установку..."
    }

    if ($requiredSdk -match '^\d+\.\d+\.\d+$') {
        Install-DotNetSdk -Version $requiredSdk
    }
    else {
        Install-DotNetSdk -Channel $requiredChannel
    }

    if (-not (Test-Command "dotnet")) {
        Write-Err ".NET SDK установлен, но не найден в PATH."
        Write-Err "Перезапустите терминал и запустите скрипт снова."
        exit 1
    }

    $sdkList = & dotnet --list-sdks 2>&1
    $matchingSdk = $sdkList | Select-String -Pattern "^$([regex]::Escape($requiredSdk))\s"
    if (-not $matchingSdk) {
        Write-Err ".NET SDK $requiredSdk установлен не был или не виден dotnet."
        Write-Err "Текущие SDK:"
        $sdkList | ForEach-Object { Write-Err "  $_" }
        exit 1
    }

    $ver = ($matchingSdk | Select-Object -First 1).ToString().Split(' ')[0]
    Write-Success ".NET SDK $ver (установлен)"
}

function Test-Prerequisites {
    param([string]$BuildStep)

    Write-StepHeader "Проверка и установка зависимостей"

    $needGo     = $BuildStep -in @("All", "GoWrapper")
    $needDotNet = $BuildStep -in @("All", "DotNet", "TUN")

    if ($needGo) {
        Ensure-Go
        Ensure-GCC
    }

    if ($needDotNet) {
        Ensure-DotNet
    }

    Write-Success "Все зависимости готовы"
}

# ─── Шаг 1: Сборка Go-обёртки ────────────────────────────────────────────────

function Build-GoWrapper {
    Write-StepHeader "Шаг 1: Сборка XRayCore.dll (Go)"

    if (-not (Test-Path $WrapperDir)) {
        Write-Err "Директория XRay-Wrapper не найдена: $WrapperDir"
        exit 1
    }

    Push-Location $WrapperDir
    try {
        Write-Info "Сборка XRayCore.dll..."
        $env:CGO_ENABLED = "1"
        & go build --buildmode=c-shared -o XRayCore.dll -trimpath -ldflags "-s -w -buildid=" .
        if ($LASTEXITCODE -ne 0) {
            Write-Err "Сборка Go завершилась с ошибкой (код: $LASTEXITCODE)"
            exit $LASTEXITCODE
        }
        Write-Success "XRayCore.dll собран"

        if (-not (Test-Path $LibrariesDir)) {
            New-Item -ItemType Directory -Path $LibrariesDir -Force | Out-Null
            Write-Info "Создана директория: $LibrariesDir"
        }

        Copy-Item -Path "XRayCore.dll" -Destination $LibrariesDir -Force
        Write-Success "XRayCore.dll -> $LibrariesDir"

        if (Test-Path "XRayCore.h") {
            Remove-Item "XRayCore.h" -Force -ErrorAction SilentlyContinue
        }
        Remove-Item "XRayCore.dll" -Force -ErrorAction SilentlyContinue
    }
    finally {
        Pop-Location
    }
}

# ─── Шаг 2: Скачивание geo-файлов ────────────────────────────────────────────

function Get-GeoFiles {
    Write-StepHeader "Шаг 2: Скачивание geoip.dat и geosite.dat"

    $geoIpPath   = Join-Path $AppDir "geoip.dat"
    $geoSitePath = Join-Path $AppDir "geosite.dat"

    try {
        Invoke-Download -Uri $GeoIpUrl -OutFile $geoIpPath -Label "Скачивание geoip.dat..."
        Write-Success "geoip.dat ($(Format-FileSize (Get-Item $geoIpPath).Length))"
    }
    catch {
        Write-Err "Не удалось скачать geoip.dat: $_"
        exit 1
    }

    try {
        Invoke-Download -Uri $GeoSiteUrl -OutFile $geoSitePath -Label "Скачивание geosite.dat..."
        Write-Success "geosite.dat ($(Format-FileSize (Get-Item $geoSitePath).Length))"
    }
    catch {
        Write-Err "Не удалось скачать geosite.dat: $_"
        exit 1
    }
}

# ─── Шаг 3: Скачивание или сборка TUN-сервиса ───────────────────────────────

function Build-LocalTunService {
    param(
        [Parameter(Mandatory)][string]$ProjectPath,
        [Parameter(Mandatory)][string]$DestinationDir
    )

    $publishDir = Join-Path $env:TEMP ("InvisibleGorilla-TUN-" + [guid]::NewGuid().ToString("N"))
    $legacyTunExePath = Join-Path $DestinationDir "InvisibleMan-TUN.exe"
    $localTunDll = Join-Path $LocalTunProjectDir "tun.dll"
    $requiredAssets = @(
        (Join-Path $LocalTunProjectDir "Assets\Icon.ico"),
        (Join-Path $LocalTunProjectDir "tun.dll"),
        (Join-Path $LocalTunProjectDir "wintun.dll"),
        (Join-Path $LocalTunProjectDir "tun2socks.exe")
    )

    if (-not (Test-Path $DestinationDir)) {
        New-Item -ItemType Directory -Path $DestinationDir -Force | Out-Null
    }

    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

    try {
        Write-Info "Найден локальный репозиторий InvisibleGorilla-TUN: $LocalTunRepoDir"

        if (Test-Path $LocalTunBuildScript) {
            Write-Info "Найден локальный build.ps1, запускаю сборку TUN из исходников..."

            try {
                & $LocalTunBuildScript -Configuration $Configuration -Runtime $Runtime -OutputDir $publishDir -SkipPackage
                if (($LASTEXITCODE -eq 0) -and (Test-Path (Join-Path $publishDir "InvisibleGorilla-TUN.exe"))) {
                    Get-ChildItem -Path $publishDir -File | ForEach-Object {
                        Copy-Item -Path $_.FullName -Destination $DestinationDir -Force
                    }

                    if (Test-Path $legacyTunExePath) {
                        Remove-Item $legacyTunExePath -Force -ErrorAction SilentlyContinue
                    }

                    Write-Success "Локальный InvisibleGorilla-TUN собран через build.ps1 и скопирован в: $DestinationDir"
                    return $true
                }

                Write-Info "Локальный build.ps1 не создал ожидаемый publish-вывод, пробую резервный сценарий..."
            }
            catch {
                Write-Info "Локальный build.ps1 завершился с ошибкой: $($_.Exception.Message)"
                Write-Info "Пробую резервный сценарий локальной публикации..."
            }
        }

        if ((-not (Test-Path $localTunDll)) -and (Test-Path $LocalTunWrapperDir)) {
            Write-Info "Локальный tun.dll не найден, пробую собрать его из TUN-Wrapper..."

            Push-Location $LocalTunWrapperDir
            try {
                & go build --buildmode=c-shared -o tun.dll -trimpath -ldflags "-s -w -buildid=" .
                if (($LASTEXITCODE -eq 0) -and (Test-Path (Join-Path $LocalTunWrapperDir "tun.dll"))) {
                    Copy-Item -Path (Join-Path $LocalTunWrapperDir "tun.dll") -Destination $localTunDll -Force
                    Remove-Item (Join-Path $LocalTunWrapperDir "tun.h") -Force -ErrorAction SilentlyContinue
                    Remove-Item (Join-Path $LocalTunWrapperDir "tun.dll") -Force -ErrorAction SilentlyContinue
                    Write-Success "tun.dll собран из локального TUN-Wrapper"
                }
            }
            finally {
                Pop-Location
            }
        }

        $missingAssets = @($requiredAssets | Where-Object { -not (Test-Path $_) })
        if ($missingAssets.Count -gt 0) {
            Write-Info "Локальный TUN-репозиторий найден, но ещё не готов к публикации."
            Write-Info "Не хватает файлов:"
            foreach ($asset in $missingAssets) {
                Write-Info "  - $asset"
            }
            return $false
        }

        Write-Info "Публикация локального TUN-сервиса..."
        & dotnet publish $ProjectPath -c $Configuration -r $Runtime --self-contained true -o $publishDir

        if ($LASTEXITCODE -ne 0) {
            Write-Err "dotnet publish InvisibleGorilla-TUN: ошибка (код: $LASTEXITCODE)"
            exit $LASTEXITCODE
        }

        Get-ChildItem -Path $publishDir -File | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination $DestinationDir -Force
        }

        if (Test-Path $legacyTunExePath) {
            Remove-Item $legacyTunExePath -Force -ErrorAction SilentlyContinue
        }

        Write-Success "Локальный InvisibleGorilla-TUN опубликован в: $DestinationDir"
        return $true
    }
    finally {
        Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Get-TunService {
    Write-StepHeader "Шаг 3: Скачивание InvisibleGorilla-TUN"

    if (-not (Test-Path $TunDir)) {
        New-Item -ItemType Directory -Path $TunDir -Force | Out-Null
        Write-Info "Создана директория: $TunDir"
    }

    $tunExePath = Join-Path $TunDir "InvisibleGorilla-TUN.exe"
    $legacyTunExePath = Join-Path $TunDir "InvisibleMan-TUN.exe"

    if (Test-Path $LocalTunProject) {
        if (Build-LocalTunService -ProjectPath $LocalTunProject -DestinationDir $TunDir) {
            return
        }

        Write-Info "Перехожу к скачиванию release InvisibleGorilla-TUN..."
    }

    if (Test-Path $tunExePath) {
        Write-Info "InvisibleGorilla-TUN.exe уже существует, пропуск"
        return
    }

    if (Test-Path $legacyTunExePath) {
        Move-Item -Path $legacyTunExePath -Destination $tunExePath -Force
        Write-Info "Найден старый InvisibleMan-TUN.exe, переименован в InvisibleGorilla-TUN.exe"
        return
    }

    Write-Info "Получение информации о последнем релизе..."
    try {
        $release = Invoke-RestMethod -Uri $TunRelease -UseBasicParsing
        $asset = $release.assets | Where-Object {
            ($_.name -match "windows.*x64") -or
            ($_.name -match "win.*x64") -or
            ($_.name -match "x64.*windows") -or
            ($_.name -match "\.exe$")
        } | Select-Object -First 1

        if (-not $asset) {
            $asset = $release.assets | Where-Object { $_.name -match "windows|win" } | Select-Object -First 1
        }

        if (-not $asset) {
            Write-Err "Не найден подходящий файл в релизе."
            Write-Err "Скачайте вручную: https://github.com/hvkeyn/InvisibleGorilla-TUN/releases/latest"
            Write-Err "Поместите InvisibleGorilla-TUN.exe в: $TunDir"
            return
        }

        $tempFile = Join-Path $env:TEMP $asset.name
        Invoke-Download -Uri $asset.browser_download_url -OutFile $tempFile -Label "Скачивание $($asset.name)..."

        if ($asset.name -match "\.zip$") {
            Write-Info "Распаковка архива..."
            Expand-Archive -Path $tempFile -DestinationPath $TunDir -Force
            Remove-Item $tempFile -Force -ErrorAction SilentlyContinue

            if ((-not (Test-Path $tunExePath)) -and (Test-Path $legacyTunExePath)) {
                Move-Item -Path $legacyTunExePath -Destination $tunExePath -Force
            }
        }
        else {
            Move-Item -Path $tempFile -Destination $tunExePath -Force
        }

        Write-Success "InvisibleGorilla-TUN -> $TunDir"
    }
    catch {
        Write-Err "Не удалось скачать InvisibleGorilla-TUN: $_"
        Write-Err "Скачайте вручную: https://github.com/hvkeyn/InvisibleGorilla-TUN/releases/latest"
    }
}

# ─── Шаг 4: Сборка .NET приложения ───────────────────────────────────────────

function Stop-RunningWindowsAppForBuild {
    param(
        [string]$Configuration,
        [string]$PublishOutputDir
    )

    $buildOutputExe = Join-Path $AppDir "bin\$Configuration\net7.0-windows\Invisible Gorilla XRay.exe"
    $publishOutputExe = if ($PublishOutputDir) {
        Join-Path $PublishOutputDir "Invisible Gorilla XRay.exe"
    }
    else {
        $null
    }

    $targetExePaths = @($buildOutputExe, $publishOutputExe) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { [System.IO.Path]::GetFullPath($_) }

    $processes = @()
    try {
        $processes = @(Get-CimInstance Win32_Process -Filter "Name = 'Invisible Gorilla XRay.exe'" -ErrorAction SilentlyContinue)
    }
    catch {
        Write-Info "Не удалось проверить запущенный Invisible Gorilla XRay: $($_.Exception.Message)"
        return
    }

    if ($processes.Count -eq 0) {
        return
    }

    $blockingProcesses = @(
        foreach ($process in $processes) {
            $processPath = $process.ExecutablePath
            if ([string]::IsNullOrWhiteSpace($processPath)) {
                continue
            }

            $fullProcessPath = [System.IO.Path]::GetFullPath($processPath)
            foreach ($targetExePath in $targetExePaths) {
                if ($fullProcessPath.Equals($targetExePath, [StringComparison]::OrdinalIgnoreCase)) {
                    $process
                    break
                }
            }
        }
    )

    if ($blockingProcesses.Count -eq 0) {
        return
    }

    if ($NoStopRunningApp) {
        Write-Err "Выходной файл занят запущенным приложением:"
        foreach ($process in $blockingProcesses) {
            Write-Err "  PID $($process.ProcessId): $($process.ExecutablePath)"
        }
        Write-Err "Закройте Invisible Gorilla XRay и повторите сборку."
        exit 1
    }

    Write-Info "Найден запущенный Invisible Gorilla XRay, который блокирует выходной exe."
    Write-Info "Закрываю его перед сборкой, чтобы MSBuild смог заменить файл..."

    foreach ($process in $blockingProcesses) {
        Write-Info "PID $($process.ProcessId): $($process.ExecutablePath)"

        try {
            $runningProcess = Get-Process -Id $process.ProcessId -ErrorAction Stop
            if ($runningProcess.MainWindowHandle -ne 0) {
                [void]$runningProcess.CloseMainWindow()
                if ($runningProcess.WaitForExit(5000)) {
                    Write-Success "Процесс $($process.ProcessId) закрыт"
                    continue
                }
            }

            Write-Info "Процесс $($process.ProcessId) не закрылся штатно, завершаю принудительно..."
            Stop-Process -Id $process.ProcessId -Force -ErrorAction Stop

            $runningProcess = Get-Process -Id $process.ProcessId -ErrorAction SilentlyContinue
            if ($runningProcess) {
                Start-Sleep -Milliseconds 500
            }
            Write-Success "Процесс $($process.ProcessId) завершён"
        }
        catch {
            Write-Err "Не удалось закрыть процесс $($process.ProcessId): $($_.Exception.Message)"
            Write-Err "Закройте Invisible Gorilla XRay вручную и повторите сборку."
            exit 1
        }
    }
}

function Build-DotNetApp {
    if ($Publish) {
        Write-StepHeader "Шаг 4: Публикация .NET ($Configuration, $Runtime)"
    }
    else {
        Write-StepHeader "Шаг 4: Сборка .NET ($Configuration)"
    }

    if (-not (Test-Path $WindowsProjectFile)) {
        Write-Err "Проект Windows-приложения не найден: $WindowsProjectFile"
        exit 1
    }

    Push-Location $RootDir
    try {
        $absOutput = [System.IO.Path]::GetFullPath((Join-Path $RootDir $OutputDir))
        Stop-RunningWindowsAppForBuild -Configuration $Configuration -PublishOutputDir $absOutput

        Write-Info "Восстановление NuGet-пакетов..."
        & dotnet restore $WindowsProjectFile
        if ($LASTEXITCODE -ne 0) {
            Write-Err "dotnet restore: ошибка (код: $LASTEXITCODE)"
            exit $LASTEXITCODE
        }
        Write-Success "NuGet-пакеты восстановлены"

        if ($Publish) {
            Write-Info "Публикация приложения..."
            & dotnet publish $WindowsProjectFile `
                -c $Configuration `
                -r $Runtime `
                --self-contained true `
                -o $absOutput

            if ($LASTEXITCODE -ne 0) {
                Write-Err "dotnet publish: ошибка (код: $LASTEXITCODE)"
                exit $LASTEXITCODE
            }
            Write-Success "Опубликовано в: $absOutput"
        }
        else {
            Write-Info "Сборка приложения..."
            & dotnet build $WindowsProjectFile -c $Configuration

            if ($LASTEXITCODE -ne 0) {
                Write-Err "dotnet build: ошибка (код: $LASTEXITCODE)"
                exit $LASTEXITCODE
            }
            Write-Success "Приложение собрано ($Configuration)"
        }
    }
    finally {
        Pop-Location
    }
}

# ─── Основной поток ──────────────────────────────────────────────────────────

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host ""
Write-Host "  Invisible Gorilla - XRay Client :: Build Script" -ForegroundColor Magenta
Write-Host "  v3.2.5.0" -ForegroundColor DarkGray
Write-Host ""

Test-Prerequisites -BuildStep $Step

switch ($Step) {
    "All" {
        Build-GoWrapper
        Get-GeoFiles
        if (-not $SkipTUN) {
            Get-TunService
        }
        else {
            Write-Info "Шаг 3 (TUN) пропущен (-SkipTUN)"
        }
        Build-DotNetApp
    }
    "GoWrapper" { Build-GoWrapper }
    "GeoFiles"  { Get-GeoFiles }
    "TUN"       { Get-TunService }
    "DotNet"    { Build-DotNetApp }
}

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed

Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Green
Write-Host ("  Готово за {0:mm\:ss\.ff}" -f $elapsed) -ForegroundColor Green
Write-Host ("=" * 60) -ForegroundColor Green
Write-Host ""
