#!/usr/bin/env bash
#
# build-macos.sh — Build script for Invisible Gorilla XRay Client on macOS
#
# Tested on: macOS Sequoia 15.7.x (Apple Silicon & Intel)
#
# Automates the full build cycle:
#   1. Check & auto-install dependencies (Go, Xcode CLT, .NET SDK from global.json)
#   2. Build Go wrapper: XRayCore.dylib (c-shared) + gorilla-xray (CLI binary)
#   3. Download geoip.dat and geosite.dat
#   4. Build/publish .NET application (if cross-platform UI is available)
#   5. Package .app bundle with GUI + engine + data files
#
# The macOS GUI uses Avalonia UI (InvisibleGorilla-XRay.Mac project).
# The WPF GUI (InvisibleGorilla-XRay) is Windows-only and is skipped.
#
# Usage:
#   ./build-macos.sh                        # Full build (all steps)
#   ./build-macos.sh --step go              # Only build Go wrapper
#   ./build-macos.sh --step geo             # Only download geo databases
#   ./build-macos.sh --step dotnet          # Only build .NET app (requires UI port)
#   ./build-macos.sh --step bundle          # Only package distribution
#   ./build-macos.sh --publish              # Build + publish self-contained binary
#   ./build-macos.sh --config Debug         # Build in Debug mode
#   ./build-macos.sh --skip-dotnet          # Skip .NET build (WPF incompatible)
#   ./build-macos.sh --help                 # Show help
#

set -Eeuo pipefail

# ─── Settings ─────────────────────────────────────────────────────────────────

readonly VERSION="3.2.5.0"
readonly SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
readonly WRAPPER_DIR="$SCRIPT_DIR/XRay-Wrapper"
readonly APP_DIR="$SCRIPT_DIR/InvisibleGorilla-XRay"
readonly MAC_DIR="$SCRIPT_DIR/InvisibleGorilla-XRay.Mac"
readonly CORE_DIR="$SCRIPT_DIR/InvisibleGorilla.Core"
readonly LIBRARIES_DIR="$APP_DIR/Libraries"
readonly SOLUTION_FILE="$SCRIPT_DIR/InvisibleGorilla-XRay.sln"
readonly APP_BINARY_NAME="InvisibleGorilla-XRay.Mac"
readonly APP_BUNDLE_NAME="InvisibleGorilla-XRay.app"
readonly APP_RUNNER_NAME="run-igxray"
readonly DOTNET_FALLBACK_DIR="$SCRIPT_DIR/.dotnet-sdk"
readonly DOTNET_CLI_HOME_DIR="$SCRIPT_DIR/.dotnet-home"
readonly NUGET_PACKAGES_DIR="$SCRIPT_DIR/.nuget/packages"

readonly GEOIP_URL="https://github.com/v2fly/geoip/releases/latest/download/geoip.dat"
readonly GEOSITE_URL="https://github.com/v2fly/domain-list-community/releases/latest/download/dlc.dat"

# Defaults
STEP="all"
CONFIGURATION="Release"
PUBLISH=false
OUTPUT_DIR="./publish-macos"
DIST_DIR="./dist-macos"
SKIP_DOTNET=false
DOTNET_CMD=""
DOTNET_SDK_VERSION=""

# Detect architecture
ARCH="$(uname -m)"
if [[ "$ARCH" == "arm64" ]]; then
    RUNTIME="osx-arm64"
    GO_ARCH="arm64"
else
    RUNTIME="osx-x64"
    GO_ARCH="amd64"
fi

# ─── Colors & output helpers ──────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
DIM='\033[2m'
NC='\033[0m'

SPIN_CHARS=('⠋' '⠙' '⠹' '⠸' '⠼' '⠴' '⠦' '⠧' '⠇' '⠏')

step_header() {
    echo ""
    echo -e "${CYAN}============================================================${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}============================================================${NC}"
    echo ""
}

ok()   { echo -e "${GREEN}[OK]${NC} $1"; }
info() { echo -e "${YELLOW}[..]${NC} $1"; }
err()  { echo -e "${RED}[!!]${NC} $1" >&2; }

command_exists() { command -v "$1" &>/dev/null; }

on_error() {
    local exit_code="$1" line_no="$2" command="$3"
    echo "" >&2
    err "Build failed at line $line_no (exit code $exit_code)"
    err "Command: $command"
    err "Working directory: $(pwd)"
    err "If this happened during publish, check the output above for the first compiler/SDK error."
}

trap 'on_error "$?" "$LINENO" "$BASH_COMMAND"' ERR

prepend_path_once() {
    local dir="$1"
    [[ -n "$dir" && -d "$dir" ]] || return 0
    case ":$PATH:" in
        *":$dir:"*) ;;
        *) export PATH="$dir:$PATH" ;;
    esac
}

resolve_path() {
    local path="$1"
    case "$path" in
        /*) printf '%s' "$path" ;;
        *)  printf '%s' "$SCRIPT_DIR/$path" ;;
    esac
}

get_required_dotnet_sdk_version() {
    if [[ -n "$DOTNET_SDK_VERSION" ]]; then
        printf '%s' "$DOTNET_SDK_VERSION"
        return 0
    fi

    local global_json="$SCRIPT_DIR/global.json"
    if [[ -f "$global_json" ]]; then
        DOTNET_SDK_VERSION="$(
            grep -E '"version"[[:space:]]*:' "$global_json" |
            head -1 |
            sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/'
        )" || DOTNET_SDK_VERSION=""
    fi

    [[ -z "$DOTNET_SDK_VERSION" ]] && DOTNET_SDK_VERSION="8.0.419"
    printf '%s' "$DOTNET_SDK_VERSION"
}

configure_dotnet_cli_environment() {
    mkdir -p "$DOTNET_CLI_HOME_DIR" "$NUGET_PACKAGES_DIR"
    export DOTNET_CLI_HOME="$DOTNET_CLI_HOME_DIR"
    export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export NUGET_PACKAGES="$NUGET_PACKAGES_DIR"
}

format_size() {
    local bytes=$1
    if (( bytes >= 1073741824 )); then
        echo "$(echo "scale=1; $bytes / 1073741824" | bc) GB"
    elif (( bytes >= 1048576 )); then
        echo "$(echo "scale=1; $bytes / 1048576" | bc) MB"
    elif (( bytes >= 1024 )); then
        echo "$(echo "scale=1; $bytes / 1024" | bc) KB"
    else
        echo "$bytes B"
    fi
}

# Download with retry and progress
download_file() {
    local url="$1"
    local output="$2"
    local label="${3:-}"
    local min_size="${4:-1048576}"
    local max_retries=3

    [[ -n "$label" ]] && info "$label"

    for attempt in $(seq 1 $max_retries); do
        if curl -fSL --progress-bar \
            --retry 2 \
            --retry-delay 3 \
            -H "User-Agent: InvisibleGorilla-BuildScript/1.0" \
            -o "$output" \
            "$url"; then

            local actual_size
            actual_size=$(stat -f%z "$output" 2>/dev/null || stat -c%s "$output" 2>/dev/null || echo 0)

            if (( actual_size >= min_size )); then
                return 0
            fi

            err "Download incomplete: $(format_size "$actual_size"), minimum $(format_size "$min_size")"
            rm -f "$output"
        fi

        if (( attempt < max_retries )); then
            info "Download failed, retry ($attempt/$max_retries)..."
            sleep $((attempt * 2))
        fi
    done

    err "Failed to download after $max_retries attempts: $url"
    return 1
}

# Spinner for long-running processes
run_with_spinner() {
    local message="$1"
    shift
    local pid

    "$@" &
    pid=$!

    local i=0
    while kill -0 "$pid" 2>/dev/null; do
        printf "\r     ${DIM}%s %s${NC}%*s" "${SPIN_CHARS[$((i % ${#SPIN_CHARS[@]}))]}" "$message" 55 ""
        sleep 0.1
        i=$((i + 1))
    done

    wait "$pid"
    local exit_code=$?
    printf "\r%*s\r" 70 ""
    return $exit_code
}

# ─── Parse arguments ──────────────────────────────────────────────────────────

show_help() {
    cat <<'HELP'
Invisible Gorilla XRay Client — macOS Build Script

Usage: ./build-macos.sh [OPTIONS]

Options:
  --step <STEP>       Run specific build step:
                        all     - Full build (default)
                        go      - Only build Go wrapper (XRayCore.dylib)
                        geo     - Only download geo databases
                        dotnet  - Only build .NET application
                        bundle  - Only package distribution
  --config <CFG>      Build configuration: Debug or Release (default: Release)
  --publish           Publish as self-contained single binary
  --runtime <RID>     .NET runtime identifier (default: auto-detected)
  --output <DIR>      Output directory for raw publish files (default: ./publish-macos)
  --dist <DIR>        Distribution bundle directory (default: ./dist-macos)
  --skip-dotnet       Skip .NET build step (for WPF-only projects)
  --help              Show this help

Architecture Detection:
  Automatically detects Apple Silicon (arm64) or Intel (x86_64)
  and selects the correct Go and .NET runtime identifiers.

Platform Notes:
  The macOS GUI uses Avalonia UI (InvisibleGorilla-XRay.Mac).
  This script builds:
    - XRayCore.dylib              (xray-core proxy engine, Go c-shared)
    - geoip.dat / geosite.dat     (geo routing databases)
    - InvisibleGorilla-XRay.app   (macOS application bundle)
    - run-igxray                 (launcher next to the app bundle)

  The final runnable bundle is written to dist-macos/<runtime>/.
  The resulting .app can be launched there or dragged into /Applications.

Examples:
  ./build-macos.sh                              # Full build
  ./build-macos.sh --step go                    # Only XRayCore.dylib
  ./build-macos.sh --step go --step geo         # Go wrapper + geo files
  ./build-macos.sh --publish --skip-dotnet      # Build + bundle without .NET
  ./build-macos.sh --step dotnet --config Debug # .NET Debug (requires UI port)
HELP
    exit 0
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --step)       STEP="$2"; shift 2 ;;
        --config)     CONFIGURATION="$2"; shift 2 ;;
        --publish)    PUBLISH=true; shift ;;
        --runtime)    RUNTIME="$2"; shift 2 ;;
        --output)     OUTPUT_DIR="$2"; shift 2 ;;
        --dist)       DIST_DIR="$2"; shift 2 ;;
        --skip-dotnet) SKIP_DOTNET=true; shift ;;
        --help|-h)    show_help ;;
        *)            err "Unknown option: $1"; echo ""; show_help ;;
    esac
done

# ─── Dependency: Xcode Command Line Tools ─────────────────────────────────────

ensure_xcode_clt() {
    if xcode-select -p &>/dev/null; then
        local clt_path
        clt_path="$(xcode-select -p)"
        # Verify cc actually works
        if cc --version &>/dev/null; then
            local cc_ver
            cc_ver="$(cc --version 2>&1 | head -1)"
            ok "C compiler: $cc_ver"
            return
        fi
    fi

    info "Xcode Command Line Tools not found, installing..."
    info "A dialog will appear — click 'Install' and wait for completion"
    xcode-select --install 2>/dev/null || true

    echo -ne "${DIM}     Waiting for Xcode CLT installation"
    until xcode-select -p &>/dev/null && cc --version &>/dev/null; do
        echo -ne "."
        sleep 5
    done
    echo -e "${NC}"

    local cc_ver
    cc_ver="$(cc --version 2>&1 | head -1)"
    ok "C compiler: $cc_ver (installed)"
}

# ─── Dependency: Go ──────────────────────────────────────────────────────────

install_go_brew() {
    if ! command_exists brew; then
        return 1
    fi
    info "Installing Go via Homebrew..."
    brew install go
    return $?
}

install_go_direct() {
    info "Fetching latest Go version from go.dev..."

    local go_version go_pkg go_url
    go_version=""

    # Try to get latest version from API
    if command_exists curl; then
        go_version=$(curl -fsSL "https://go.dev/dl/?mode=json" 2>/dev/null \
            | grep -o '"version":"go[0-9.]*"' \
            | head -1 \
            | cut -d'"' -f4) || true
    fi

    # Fallback version
    if [[ -z "$go_version" ]]; then
        go_version="go1.23.6"
        info "Could not query API, using $go_version"
    fi

    go_pkg="${go_version}.darwin-${GO_ARCH}.pkg"
    go_url="https://go.dev/dl/${go_pkg}"
    local tmp_pkg="/tmp/${go_pkg}"

    download_file "$go_url" "$tmp_pkg" "Downloading $go_version..." 20000000

    info "Installing $go_version (may require password)..."
    sudo installer -pkg "$tmp_pkg" -target / 2>/dev/null
    rm -f "$tmp_pkg"

    # Update PATH for this session
    export PATH="/usr/local/go/bin:$HOME/go/bin:$PATH"
}

ensure_go() {
    if command_exists go; then
        local go_ver
        go_ver="$(go version | grep -oE 'go[0-9]+\.[0-9]+(\.[0-9]+)?')"
        ok "Go $go_ver"
        return
    fi

    info "Go not found, installing..."

    # Try Homebrew first, then direct download
    if install_go_brew 2>/dev/null; then
        true
    else
        install_go_direct
    fi

    if ! command_exists go; then
        # Check common install locations
        for p in /usr/local/go/bin "$HOME/go/bin" /opt/homebrew/bin; do
            if [[ -x "$p/go" ]]; then
                export PATH="$p:$PATH"
                break
            fi
        done
    fi

    if ! command_exists go; then
        err "Go installed but not found in PATH."
        err "Add to your shell profile (~/.zshrc or ~/.bash_profile):"
        err "  export PATH=\"/usr/local/go/bin:\$PATH\""
        err "Then restart the terminal and run this script again."
        exit 1
    fi

    local go_ver
    go_ver="$(go version | grep -oE 'go[0-9]+\.[0-9]+(\.[0-9]+)?')"
    ok "Go $go_ver (installed)"
}

# ─── Dependency: .NET SDK from global.json ───────────────────────────────────

ensure_dotnet() {
    configure_dotnet_cli_environment

    local required_sdk
    required_sdk="$(get_required_dotnet_sdk_version)"
    info "Required .NET SDK from global.json: $required_sdk"

    local candidates=()
    [[ -x "$DOTNET_FALLBACK_DIR/dotnet" ]] && candidates+=("$DOTNET_FALLBACK_DIR/dotnet")
    [[ -n "${DOTNET_ROOT:-}" && -x "${DOTNET_ROOT:-}/dotnet" ]] && candidates+=("$DOTNET_ROOT/dotnet")
    command_exists dotnet && candidates+=("$(command -v dotnet)")
    [[ -x "$HOME/.dotnet/dotnet" ]] && candidates+=("$HOME/.dotnet/dotnet")
    [[ -x "/usr/local/share/dotnet/dotnet" ]] && candidates+=("/usr/local/share/dotnet/dotnet")
    [[ -x "/opt/homebrew/bin/dotnet" ]] && candidates+=("/opt/homebrew/bin/dotnet")
    [[ -x "/usr/local/bin/dotnet" ]] && candidates+=("/usr/local/bin/dotnet")

    local candidate sdk_list
    for candidate in "${candidates[@]}"; do
        sdk_list="$("$candidate" --list-sdks 2>/dev/null || true)"
        if echo "$sdk_list" | grep -q "^$required_sdk[[:space:]]"; then
            DOTNET_CMD="$candidate"
            DOTNET_ROOT="$(cd "$(dirname "$candidate")" && pwd)"
            export DOTNET_ROOT
            prepend_path_once "$DOTNET_ROOT"
            ok ".NET SDK $required_sdk ($DOTNET_CMD)"
            return 0
        fi
    done

    if ((${#candidates[@]} > 0)); then
        info ".NET exists, but required SDK $required_sdk is missing"
        for candidate in "${candidates[@]}"; do
            info "Installed SDKs visible to $candidate:"
            "$candidate" --list-sdks 2>/dev/null | sed 's/^/     /' || true
        done
    else
        info ".NET SDK not found, installing..."
    fi

    mkdir -p "$DOTNET_FALLBACK_DIR"

    local install_script="/tmp/dotnet-install-igxray.sh"
    download_file "https://dot.net/v1/dotnet-install.sh" "$install_script" \
        "Downloading dotnet-install.sh..." 1024

    chmod +x "$install_script"
    info "Installing .NET SDK $required_sdk to $DOTNET_FALLBACK_DIR..."
    "$install_script" --version "$required_sdk" --install-dir "$DOTNET_FALLBACK_DIR"
    rm -f "$install_script"

    if [[ ! -x "$DOTNET_FALLBACK_DIR/dotnet" ]]; then
        err ".NET SDK install finished, but dotnet executable was not created:"
        err "  $DOTNET_FALLBACK_DIR/dotnet"
        exit 1
    fi

    sdk_list="$("$DOTNET_FALLBACK_DIR/dotnet" --list-sdks 2>/dev/null || true)"
    if ! echo "$sdk_list" | grep -q "^$required_sdk[[:space:]]"; then
        err ".NET SDK $required_sdk is still not visible after installation."
        err "Installed SDKs in fallback directory:"
        echo "$sdk_list" | sed 's/^/     /' >&2
        exit 1
    fi

    DOTNET_CMD="$DOTNET_FALLBACK_DIR/dotnet"
    export DOTNET_ROOT="$DOTNET_FALLBACK_DIR"
    prepend_path_once "$DOTNET_ROOT"
    ok ".NET SDK $required_sdk installed at $DOTNET_CMD"
}

# ─── Prerequisites check ─────────────────────────────────────────────────────

check_prerequisites() {
    step_header "Checking and installing dependencies"

    local need_go=false
    local need_dotnet=false

    case "$STEP" in
        all)
            need_go=true
            if ! $SKIP_DOTNET; then
                need_dotnet=true
            fi
            ;;
        go)     need_go=true ;;
        dotnet) need_dotnet=true ;;
        geo|bundle) ;; # No special deps needed
    esac

    if $need_go; then
        ensure_xcode_clt
        ensure_go
    fi

    if $need_dotnet; then
        ensure_dotnet
    fi

    ok "All dependencies ready"
}

# ─── Step 1: Build Go wrapper + CLI binary ────────────────────────────────────

build_go_wrapper() {
    step_header "Step 1: Build XRayCore (Go)"

    if [[ ! -d "$WRAPPER_DIR" ]]; then
        err "XRay-Wrapper directory not found: $WRAPPER_DIR"
        exit 1
    fi

    pushd "$WRAPPER_DIR" >/dev/null

    # 1a. Build shared library (XRayCore.dylib) for FFI/embedding
    info "Building XRayCore.dylib for $ARCH..."
    CGO_ENABLED=1 \
    GOOS=darwin \
    GOARCH="$GO_ARCH" \
    go build \
        --buildmode=c-shared \
        -o XRayCore.dylib \
        -trimpath \
        -ldflags "-s -w -buildid=" \
        .

    ok "XRayCore.dylib built ($(format_size "$(stat -f%z XRayCore.dylib 2>/dev/null || stat -c%s XRayCore.dylib)"))"

    mkdir -p "$LIBRARIES_DIR"
    cp XRayCore.dylib "$LIBRARIES_DIR/"
    ok "XRayCore.dylib → $LIBRARIES_DIR"
    rm -f XRayCore.h XRayCore.dylib

    # 1b. Build standalone CLI binary (gorilla-xray)
    info "Building gorilla-xray CLI binary for $ARCH..."
    CGO_ENABLED=1 \
    GOOS=darwin \
    GOARCH="$GO_ARCH" \
    go build \
        -o gorilla-xray \
        -trimpath \
        -ldflags "-s -w -buildid= -X main.version=$VERSION" \
        ./cmd/gorilla-xray/

    ok "gorilla-xray built ($(format_size "$(stat -f%z gorilla-xray 2>/dev/null || stat -c%s gorilla-xray)"))"

    popd >/dev/null
}

# ─── Step 2: Download geo databases ──────────────────────────────────────────

download_geo_files() {
    step_header "Step 2: Download geoip.dat and geosite.dat"

    local geoip_path="$APP_DIR/geoip.dat"
    local geosite_path="$APP_DIR/geosite.dat"

    download_file "$GEOIP_URL" "$geoip_path" "Downloading geoip.dat..."
    local geoip_size
    geoip_size="$(format_size "$(stat -f%z "$geoip_path" 2>/dev/null || stat -c%s "$geoip_path")")"
    ok "geoip.dat ($geoip_size)"

    download_file "$GEOSITE_URL" "$geosite_path" "Downloading geosite.dat..."
    local geosite_size
    geosite_size="$(format_size "$(stat -f%z "$geosite_path" 2>/dev/null || stat -c%s "$geosite_path")")"
    ok "geosite.dat ($geosite_size)"
}

# ─── Step 3: Build .NET Avalonia application ─────────────────────────────────

build_dotnet_app() {
    if $SKIP_DOTNET; then
        info "Step 3: .NET build skipped (--skip-dotnet)"
        return 0
    fi

    step_header "Step 3: Build Avalonia macOS GUI ($CONFIGURATION, $RUNTIME)"

    local mac_csproj="$MAC_DIR/InvisibleGorilla-XRay.Mac.csproj"
    if [[ ! -f "$mac_csproj" ]]; then
        err "Avalonia project not found: $mac_csproj"
        exit 1
    fi

    pushd "$SCRIPT_DIR" >/dev/null

    info "Restoring NuGet packages..."
    "$DOTNET_CMD" restore "$mac_csproj"
    ok "NuGet packages restored"

    local abs_output
    abs_output="$(resolve_path "$OUTPUT_DIR")/$RUNTIME"
    rm -rf "$abs_output"
    mkdir -p "$abs_output"

    info "Publishing Avalonia GUI..."
    "$DOTNET_CMD" publish "$mac_csproj" \
        -c "$CONFIGURATION" \
        -r "$RUNTIME" \
        --self-contained true \
        -p:PublishSingleFile=false \
        -o "$abs_output"

    local published_binary="$abs_output/$APP_BINARY_NAME"
    if [[ ! -x "$published_binary" ]]; then
        err "Publish finished but the expected macOS binary was not created:"
        err "  $published_binary"
        err "Directory contents:"
        ls -la "$abs_output" >&2 || true
        exit 1
    fi

    ok "Published raw files to: $abs_output"
    ok "Published binary: $published_binary"

    popd >/dev/null
}

# ─── Icon generation ──────────────────────────────────────────────────────────

generate_app_icon() {
    local output_icns="$1"
    local tmpdir="/tmp/igxray-icon-$$"
    local iconset="$tmpdir/AppIcon.iconset"

    mkdir -p "$iconset"

    info "Generating app icon..."

    python3 -c "
import math, struct, zlib, sys
W = H = 512
cx, cy, r = W//2, H//2, W//2 - 10
raw = bytearray()
for y in range(H):
    raw.append(0)
    for x in range(W):
        dx, dy = x - cx, y - cy
        d = math.sqrt(dx*dx + dy*dy)
        if d <= r - 3:
            raw.extend((76, 175, 80, 255))
        elif d <= r:
            t = (d - r + 3) / 3.0
            a = int(255 * (1 - t))
            raw.extend((76, 175, 80, max(0, a)))
        else:
            raw.extend((0, 0, 0, 0))
def chunk(t, d):
    c = t + d
    return struct.pack('>I', len(d)) + c + struct.pack('>I', zlib.crc32(c) & 0xffffffff)
with open(sys.argv[1], 'wb') as f:
    f.write(b'\x89PNG\r\n\x1a\n')
    f.write(chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 6, 0, 0, 0)))
    f.write(chunk(b'IDAT', zlib.compress(bytes(raw), 9)))
    f.write(chunk(b'IEND', b''))
" "$tmpdir/base_512.png"

    # Create 1024x1024 by upscaling
    sips -z 1024 1024 "$tmpdir/base_512.png" --out "$tmpdir/base_1024.png" >/dev/null 2>&1

    # Populate iconset with required sizes
    for size in 16 32 128 256 512; do
        sips -z $size $size "$tmpdir/base_512.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null 2>&1
    done
    sips -z 32 32 "$tmpdir/base_512.png" --out "$iconset/icon_16x16@2x.png" >/dev/null 2>&1
    sips -z 64 64 "$tmpdir/base_512.png" --out "$iconset/icon_32x32@2x.png" >/dev/null 2>&1
    sips -z 256 256 "$tmpdir/base_512.png" --out "$iconset/icon_128x128@2x.png" >/dev/null 2>&1
    sips -z 512 512 "$tmpdir/base_512.png" --out "$iconset/icon_256x256@2x.png" >/dev/null 2>&1
    cp "$tmpdir/base_1024.png" "$iconset/icon_512x512@2x.png"

    iconutil -c icns "$iconset" -o "$output_icns" 2>/dev/null
    rm -rf "$tmpdir"

    if [[ -f "$output_icns" ]]; then
        ok "App icon generated: $(format_size "$(stat -f%z "$output_icns" 2>/dev/null || stat -c%s "$output_icns")")"
    else
        err "Failed to generate app icon"
    fi
}

# ─── Step 4: Package distribution bundle ─────────────────────────────────────

package_bundle() {
    step_header "Step 4: Package macOS .app bundle"

    local bundle_root
    bundle_root="$(resolve_path "$DIST_DIR")/$RUNTIME"
    local app_bundle="$bundle_root/$APP_BUNDLE_NAME"
    local contents="$app_bundle/Contents"
    local macos_dir="$contents/MacOS"
    local resources="$contents/Resources"
    local frameworks="$contents/Frameworks"

    rm -rf "$bundle_root"
    mkdir -p "$macos_dir" "$resources" "$frameworks"

    local found_items=0
    local missing_required=0
    local publish_dir
    publish_dir="$(resolve_path "$OUTPUT_DIR")/$RUNTIME"

    # Copy published Avalonia app files
    if [[ -x "$publish_dir/$APP_BINARY_NAME" ]]; then
        cp -R "$publish_dir/"* "$macos_dir/"
        chmod +x "$macos_dir/$APP_BINARY_NAME" 2>/dev/null || true
        ok "Bundled: Avalonia GUI files from $publish_dir"
        found_items=$((found_items + 1))
    else
        err "Avalonia publish output not found or missing executable:"
        err "  $publish_dir/$APP_BINARY_NAME"
        err "Run: ./build-macos.sh --step dotnet --runtime $RUNTIME"
        rm -rf "$bundle_root"
        exit 1
    fi

    # Copy CLI binary as fallback
    local cli_src="$WRAPPER_DIR/gorilla-xray"
    if [[ -f "$cli_src" ]]; then
        cp "$cli_src" "$macos_dir/"
        chmod +x "$macos_dir/gorilla-xray"
        ok "Bundled: gorilla-xray (CLI binary)"
        found_items=$((found_items + 1))
    else
        info "gorilla-xray CLI binary not found; GUI bundle can still run without CLI fallback"
    fi

    # Copy XRayCore.dylib
    local dylib_src="$LIBRARIES_DIR/XRayCore.dylib"
    if [[ -f "$dylib_src" ]]; then
        mkdir -p "$macos_dir/Libraries"
        cp "$dylib_src" "$frameworks/"
        cp "$dylib_src" "$macos_dir/Libraries/"
        ok "Bundled: XRayCore.dylib"
        found_items=$((found_items + 1))
    else
        err "XRayCore.dylib not found — run build step 'go' first"
        missing_required=$((missing_required + 1))
    fi

    # Copy geo databases
    for dat in geoip.dat geosite.dat; do
        local dat_src="$APP_DIR/$dat"
        if [[ -f "$dat_src" ]]; then
            cp "$dat_src" "$resources/"
            cp "$dat_src" "$macos_dir/"
            ok "Bundled: $dat"
            found_items=$((found_items + 1))
        else
            err "$dat not found — run build step 'geo' first"
            missing_required=$((missing_required + 1))
        fi
    done

    # Generate app icon
    generate_app_icon "$resources/AppIcon.icns"
    if [[ -f "$resources/AppIcon.icns" ]]; then
        found_items=$((found_items + 1))
    fi

    if (( found_items == 0 )); then
        err "No files to bundle. Run the full build first."
        rm -rf "$app_bundle"
        return 1
    fi

    if (( missing_required > 0 )); then
        err "Bundle is incomplete: $missing_required required runtime file(s) are missing."
        err "Run a full build: ./build-macos.sh --runtime $RUNTIME"
        rm -rf "$bundle_root"
        exit 1
    fi

    # Create Info.plist
    cat > "$contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>Invisible Gorilla XRay</string>
    <key>CFBundleDisplayName</key>
    <string>Invisible Gorilla XRay</string>
    <key>CFBundleIdentifier</key>
    <string>com.invisiblegorilla.xray</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleExecutable</key>
    <string>$APP_BINARY_NAME</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>IGXR</string>
    <key>LSMinimumSystemVersion</key>
    <string>13.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSSupportsAutomaticGraphicsSwitching</key>
    <true/>
    <key>CFBundleURLTypes</key>
    <array>
        <dict>
            <key>CFBundleURLName</key>
            <string>Invisible Gorilla XRay URL</string>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>invxray</string>
            </array>
        </dict>
    </array>
</dict>
</plist>
PLIST
    ok "Bundled: Info.plist"

    local app_binary="$macos_dir/$APP_BINARY_NAME"
    if [[ ! -x "$app_binary" ]]; then
        err "Bundle was created, but the expected executable is missing:"
        err "  $app_binary"
        rm -rf "$bundle_root"
        exit 1
    fi

    cat > "$bundle_root/$APP_RUNNER_NAME" <<RUNNER
#!/usr/bin/env bash
set -euo pipefail
DIR="\$(cd "\$(dirname "\$0")" && pwd)"
open "\$DIR/$APP_BUNDLE_NAME"
RUNNER
    chmod +x "$bundle_root/$APP_RUNNER_NAME"

    cat > "$bundle_root/README-MACOS.txt" <<README
Invisible Gorilla XRay Client for macOS
Version: $VERSION
Runtime: $RUNTIME

Run from this folder:
  ./$APP_RUNNER_NAME

Or open:
  $APP_BUNDLE_NAME

Internal app executable:
  $APP_BUNDLE_NAME/Contents/MacOS/$APP_BINARY_NAME

Install manually:
  drag $APP_BUNDLE_NAME to /Applications

The folder contains everything needed to run the app bundle.
README
    ok "Bundled: $APP_RUNNER_NAME"
    ok "Bundled: README-MACOS.txt"

    # Create archive
    local archive_name="InvisibleGorilla-XRay-macOS-${RUNTIME}-v${VERSION}.tar.gz"
    pushd "$bundle_root" >/dev/null
    tar -czf "$archive_name" "$APP_BUNDLE_NAME" "$APP_RUNNER_NAME" "README-MACOS.txt"
    popd >/dev/null

    local archive_path="$bundle_root/$archive_name"
    local archive_size
    archive_size="$(format_size "$(stat -f%z "$archive_path" 2>/dev/null || stat -c%s "$archive_path")")"

    echo ""
    ok "macOS bundle created:"
    echo -e "     ${DIM}Bundle folder: $bundle_root${NC}"
    echo -e "     ${DIM}App:           $app_bundle${NC}"
    echo -e "     ${DIM}Executable:    $app_binary${NC}"
    echo -e "     ${DIM}Launcher:      $bundle_root/$APP_RUNNER_NAME${NC}"
    echo -e "     ${DIM}Archive:       $archive_path ($archive_size)${NC}"
    echo ""
    echo -e "     ${DIM}Run:     $bundle_root/$APP_RUNNER_NAME${NC}"
    echo -e "     ${DIM}Install: drag $APP_BUNDLE_NAME to /Applications${NC}"
}

# ─── Main ─────────────────────────────────────────────────────────────────────

SECONDS=0

echo ""
echo -e "${MAGENTA}  Invisible Gorilla - XRay Client :: macOS Build Script${NC}"
echo -e "${DIM}  v${VERSION} | $(uname -m) | macOS $(sw_vers -productVersion 2>/dev/null || echo 'unknown')${NC}"
echo ""

if [[ "${EUID:-$(id -u)}" -eq 0 ]]; then
    info "Script is running as root/sudo. Build outputs may be owned by root."
    info "Recommended command for normal builds: ./build-macos.sh"
fi

# Validate step
case "$STEP" in
    all|go|geo|dotnet|bundle) ;;
    *)
        err "Unknown step: $STEP"
        err "Valid steps: all, go, geo, dotnet, bundle"
        exit 1
        ;;
esac

check_prerequisites

case "$STEP" in
    all)
        build_go_wrapper
        download_geo_files
        build_dotnet_app
        package_bundle
        ;;
    go)     build_go_wrapper ;;
    geo)    download_geo_files ;;
    dotnet) build_dotnet_app ;;
    bundle) package_bundle ;;
esac

elapsed=$SECONDS
mins=$((elapsed / 60))
secs=$((elapsed % 60))

echo ""
echo -e "${GREEN}============================================================${NC}"
printf "${GREEN}  Done in %d:%02d${NC}\n" "$mins" "$secs"
echo -e "${GREEN}============================================================${NC}"

final_bundle_root="$(resolve_path "$DIST_DIR")/$RUNTIME"
final_app="$final_bundle_root/$APP_BUNDLE_NAME"
final_binary="$final_app/Contents/MacOS/$APP_BINARY_NAME"
final_runner="$final_bundle_root/$APP_RUNNER_NAME"

if [[ -d "$final_bundle_root" ]]; then
    echo ""
    ok "Final macOS output:"
    echo -e "     ${DIM}Folder:     $final_bundle_root${NC}"
    [[ -d "$final_app" ]] && echo -e "     ${DIM}App:        $final_app${NC}"
    [[ -x "$final_binary" ]] && echo -e "     ${DIM}Binary:     $final_binary${NC}"
    [[ -x "$final_runner" ]] && echo -e "     ${DIM}Run script: $final_runner${NC}"
fi
echo ""
