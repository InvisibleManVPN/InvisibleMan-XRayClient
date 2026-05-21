#!/usr/bin/env bash
#
# build.sh — Build script for Invisible Gorilla XRay Client on Linux
#            (ALT Linux GNOME, Debian/Ubuntu GNOME, Fedora Workstation,
#             openSUSE, Arch, and any glibc-based GNU/Linux with GTK 3).
#
# Automates the full build cycle:
#   1. Detect distribution and install dependencies
#      (build tools, Go, .NET SDK from global.json, GTK runtime, libnotify, policykit-1,
#       iproute2 — used by the TUN handler at runtime).
#   2. Build the Go wrapper: libXRayCore.so (c-shared) + gorilla-xray (CLI).
#   3. Fetch xjasonlyu/tun2socks into TUN/ (used by LinuxTunnel).
#   4. Download geoip.dat / geosite.dat.
#   5. Publish the Avalonia GUI for linux-x64 (or linux-arm64).
#   6. Bundle into dist/ with a .desktop file + icon + run-igxray helper script.
#
# The Linux GUI uses the same Avalonia views as the macOS build via linked files
# in InvisibleGorilla-XRay.Linux project (no UI duplication).
#
# Usage:
#   ./build.sh                              # Full build (all steps)
#   ./build.sh --step go                    # Only build Go wrapper
#   ./build.sh --step tun2socks             # Only fetch tun2socks
#   ./build.sh --step geo                   # Only download geo databases
#   ./build.sh --step dotnet                # Only build .NET app
#   ./build.sh --step bundle                # Only package distribution
#   ./build.sh --skip-deps                  # Don't try to install system packages
#   ./build.sh --runtime linux-arm64        # Cross-target arm64
#   ./build.sh --config Debug               # Build in Debug mode
#   ./build.sh --help                       # Show help
#

set -euo pipefail

# ─── Settings ─────────────────────────────────────────────────────────────────

readonly APP_VERSION="3.2.5.0"
readonly SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
readonly WRAPPER_DIR="$SCRIPT_DIR/XRay-Wrapper"
readonly LINUX_DIR="$SCRIPT_DIR/InvisibleGorilla-XRay.Linux"
readonly APP_DIR="$SCRIPT_DIR/InvisibleGorilla-XRay"
readonly LIBRARIES_DIR="$APP_DIR/Libraries"
readonly APP_BINARY_NAME="InvisibleGorilla-XRay.Linux"
readonly APP_COMMAND_NAME="invisible-gorilla-xray"
readonly APP_RUNNER_NAME="run-igxray"
readonly INSTALL_DIR="/opt/invisible-gorilla-xray"
readonly DOTNET_FALLBACK_DIR="$SCRIPT_DIR/.dotnet-sdk"
readonly DOTNET_CLI_HOME_DIR="$SCRIPT_DIR/.dotnet-home"
readonly NUGET_PACKAGES_DIR="$SCRIPT_DIR/.nuget/packages"

readonly GEOIP_URL="https://github.com/v2fly/geoip/releases/latest/download/geoip.dat"
readonly GEOSITE_URL="https://github.com/v2fly/domain-list-community/releases/latest/download/dlc.dat"

# tun2socks (xjasonlyu) — auth-aware SOCKS5 client, the same component the macOS build uses.
readonly TUN2SOCKS_RELEASES="https://github.com/xjasonlyu/tun2socks/releases/latest/download"

STEP="all"
CONFIGURATION="Release"
OUTPUT_DIR="./publish-linux"
DIST_DIR="./dist-linux"
SKIP_DEPS=false
DOTNET_CMD=""
DOTNET_SDK_VERSION=""

ARCH="$(uname -m)"
case "$ARCH" in
    x86_64|amd64) RUNTIME="linux-x64";  GO_ARCH="amd64"; TUN2SOCKS_ASSET="tun2socks-linux-amd64.zip" ;;
    aarch64|arm64) RUNTIME="linux-arm64"; GO_ARCH="arm64"; TUN2SOCKS_ASSET="tun2socks-linux-arm64.zip" ;;
    *) RUNTIME="linux-x64"; GO_ARCH="amd64"; TUN2SOCKS_ASSET="tun2socks-linux-amd64.zip" ;;
esac

# ─── Output helpers ───────────────────────────────────────────────────────────

if [[ -t 1 ]]; then
    RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
    CYAN='\033[0;36m'; MAGENTA='\033[0;35m'; DIM='\033[2m'; NC='\033[0m'
else
    RED=''; GREEN=''; YELLOW=''; CYAN=''; MAGENTA=''; DIM=''; NC=''
fi

step_header() {
    echo
    echo -e "${CYAN}============================================================${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}============================================================${NC}"
    echo
}

ok()   { echo -e "${GREEN}[OK]${NC} $1"; }
info() { echo -e "${YELLOW}[..]${NC} $1"; }
err()  { echo -e "${RED}[!!]${NC} $1" >&2; }

command_exists() { command -v "$1" &>/dev/null; }

prepend_path_once() {
    local dir="$1"
    [[ -n "$dir" && -d "$dir" ]] || return 0
    case ":$PATH:" in
        *":$dir:"*) ;;
        *) export PATH="$dir:$PATH" ;;
    esac
}

configure_dotnet_cli_environment() {
    mkdir -p "$DOTNET_CLI_HOME_DIR" "$NUGET_PACKAGES_DIR"
    export DOTNET_CLI_HOME="$DOTNET_CLI_HOME_DIR"
    export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    export DOTNET_CLI_TELEMETRY_OPTOUT=1
    export NUGET_PACKAGES="$NUGET_PACKAGES_DIR"
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
        )"
    fi

    [[ -z "$DOTNET_SDK_VERSION" ]] && DOTNET_SDK_VERSION="8.0.419"
    printf '%s' "$DOTNET_SDK_VERSION"
}

resolve_path() {
    local path="$1"
    case "$path" in
        /*) printf '%s' "$path" ;;
        *)  printf '%s' "$SCRIPT_DIR/$path" ;;
    esac
}

format_size() {
    local bytes=$1
    if (( bytes >= 1073741824 )); then awk "BEGIN { printf \"%.1f GB\", $bytes/1073741824 }"
    elif (( bytes >= 1048576 ));   then awk "BEGIN { printf \"%.1f MB\", $bytes/1048576 }"
    elif (( bytes >= 1024 ));      then awk "BEGIN { printf \"%.1f KB\", $bytes/1024 }"
    else echo "$bytes B"; fi
}

download_file() {
    local url="$1" output="$2" label="${3:-}" min_size="${4:-1024}"
    [[ -n "$label" ]] && info "$label"

    for attempt in 1 2 3; do
        if curl -fSL --progress-bar --retry 2 --retry-delay 3 \
                -H "User-Agent: InvisibleGorilla-LinuxBuild/1.0" \
                -o "$output" "$url"; then
            local sz; sz=$(stat -c%s "$output" 2>/dev/null || echo 0)
            if (( sz >= min_size )); then return 0; fi
            err "Download incomplete: $(format_size "$sz"), minimum $(format_size "$min_size")"
            rm -f "$output"
        fi
        (( attempt < 3 )) && { info "Retry ($attempt/3)..."; sleep $((attempt*2)); }
    done

    err "Failed to download after 3 attempts: $url"
    return 1
}

# ─── Distribution detection ───────────────────────────────────────────────────

DISTRO_ID=""
DISTRO_FAMILY=""
PKG_INSTALL=""
SUDO=""

read_os_release_field() {
    # Pure-text parser instead of `. /etc/os-release`, because sourcing it
    # assigns keys like VERSION=, NAME=, ID= into the current shell, and any
    # collision with an existing readonly variable aborts the script under
    # `set -e` (this is exactly how ALT Linux bit us with `readonly VERSION`).
    # `readonly` also propagates into subshells, so a `( . /etc/os-release )`
    # wouldn't help either. Parse the file instead.
    local field="$1"
    [[ -r /etc/os-release ]] || { printf ''; return 0; }
    local line
    line="$(grep -E "^${field}=" /etc/os-release 2>/dev/null | tail -n 1)" || true
    [[ -z "$line" ]] && { printf ''; return 0; }
    local value="${line#*=}"
    # Strip surrounding single or double quotes if present
    if [[ "$value" == \"*\" ]]; then
        value="${value#\"}"; value="${value%\"}"
    elif [[ "$value" == \'*\' ]]; then
        value="${value#\'}"; value="${value%\'}"
    fi
    printf '%s' "$value"
}

detect_distro() {
    local pretty_name=""
    if [[ -r /etc/os-release ]]; then
        DISTRO_ID="$(read_os_release_field ID)"
        [[ -z "$DISTRO_ID" ]] && DISTRO_ID="unknown"
        local id_like
        id_like="$(read_os_release_field ID_LIKE)"
        pretty_name="$(read_os_release_field PRETTY_NAME)"

        case "$DISTRO_ID:$id_like" in
            altlinux:*|*:altlinux*)
                DISTRO_FAMILY="alt"
                PKG_INSTALL="apt-get install -y"
                ;;
            debian:*|ubuntu:*|*:debian*|*:ubuntu*|linuxmint:*|pop:*)
                DISTRO_FAMILY="debian"
                PKG_INSTALL="apt-get install -y"
                ;;
            fedora:*|rhel:*|centos:*|almalinux:*|rocky:*|*:fedora*|*:rhel*)
                DISTRO_FAMILY="rhel"
                if command_exists dnf; then PKG_INSTALL="dnf install -y"
                else PKG_INSTALL="yum install -y"
                fi
                ;;
            opensuse*|sles:*|*:suse*|*:opensuse*)
                DISTRO_FAMILY="suse"
                PKG_INSTALL="zypper install -y"
                ;;
            arch:*|manjaro:*|endeavouros:*|*:arch*)
                DISTRO_FAMILY="arch"
                PKG_INSTALL="pacman -S --noconfirm --needed"
                ;;
            *)
                DISTRO_FAMILY="unknown"
                ;;
        esac
    fi

    if [[ "$(id -u)" != "0" ]] && command_exists sudo; then
        SUDO="sudo"
    fi

    ok "Distro: ${pretty_name:-unknown} (family=$DISTRO_FAMILY, arch=$ARCH)"
}

install_packages() {
    local pkgs=("$@")
    [[ ${#pkgs[@]} -eq 0 ]] && return 0

    if [[ -z "$PKG_INSTALL" ]]; then
        info "Unknown distribution — skipping system package install. Required: ${pkgs[*]}"
        return 0
    fi

    info "Installing: ${pkgs[*]}"

    case "$DISTRO_FAMILY" in
        alt|debian)
            $SUDO apt-get update -y || true
            if ! $SUDO $PKG_INSTALL "${pkgs[@]}"; then
                err "Package group install failed; retrying packages one by one (non-fatal)."
                local pkg
                for pkg in "${pkgs[@]}"; do
                    $SUDO $PKG_INSTALL "$pkg" || err "Optional package not installed: $pkg"
                done
            fi
            ;;
        rhel|suse|arch)
            if ! $SUDO $PKG_INSTALL "${pkgs[@]}"; then
                err "Package group install failed; retrying packages one by one (non-fatal)."
                local pkg
                for pkg in "${pkgs[@]}"; do
                    $SUDO $PKG_INSTALL "$pkg" || err "Optional package not installed: $pkg"
                done
            fi
            ;;
    esac
}

# ─── Dependencies ─────────────────────────────────────────────────────────────

ensure_build_tools() {
    if $SKIP_DEPS; then return 0; fi
    case "$DISTRO_FAMILY" in
        alt)    install_packages gcc make pkg-config curl unzip ;;
        debian) install_packages build-essential pkg-config curl unzip ca-certificates ;;
        rhel)   install_packages gcc make pkgconf-pkg-config curl unzip ca-certificates ;;
        suse)   install_packages gcc make pkg-config curl unzip ca-certificates ;;
        arch)   install_packages base-devel pkgconf curl unzip ca-certificates ;;
    esac
}

ensure_runtime_libs() {
    # GUI (Avalonia 11 / GTK 3 / X11) + system tray (libdbusmenu / StatusNotifierItem) +
    # libnotify (notify-send), policykit-1 (pkexec), iproute2 (ip).
    if $SKIP_DEPS; then return 0; fi
    case "$DISTRO_FAMILY" in
        alt)    install_packages libgtk+3 notify-send polkit iproute2 fontconfig libICE libSM libX11 libXi libXrandr libxcb-cursor ;;
        debian) install_packages libgtk-3-0 libnotify-bin policykit-1 iproute2 fontconfig libice6 libsm6 libx11-6 libxi6 libxrandr2 libxcb-cursor0 ;;
        rhel)   install_packages gtk3 libnotify polkit iproute fontconfig libICE libSM libX11 libXi libXrandr xcb-util-cursor ;;
        suse)   install_packages gtk3 libnotify-tools polkit iproute2 fontconfig libICE6 libSM6 libX11-6 libXi6 libXrandr2 xcb-util-cursor0 ;;
        arch)   install_packages gtk3 libnotify polkit iproute2 fontconfig libice libsm libx11 libxi libxrandr xcb-util-cursor ;;
    esac
}

ensure_go() {
    if command_exists go; then
        ok "Go $(go version | grep -oE 'go[0-9]+\.[0-9]+(\.[0-9]+)?')"
        return
    fi

    if ! $SKIP_DEPS; then
        case "$DISTRO_FAMILY" in
            alt|debian) install_packages golang ;;
            rhel)       install_packages golang ;;
            suse)       install_packages go ;;
            arch)       install_packages go ;;
        esac
    fi

    if command_exists go; then
        ok "Go $(go version | grep -oE 'go[0-9]+\.[0-9]+(\.[0-9]+)?') (from packages)"
        return
    fi

    info "Falling back to direct Go tarball install"
    local go_version="go1.23.6"
    local go_tar="${go_version}.linux-${GO_ARCH}.tar.gz"
    local tmp="/tmp/$go_tar"
    download_file "https://go.dev/dl/$go_tar" "$tmp" "Downloading $go_version..." 20000000

    $SUDO rm -rf /usr/local/go
    $SUDO tar -C /usr/local -xzf "$tmp"
    rm -f "$tmp"
    export PATH="/usr/local/go/bin:$HOME/go/bin:$PATH"

    if ! command_exists go; then
        err "Go installed but not on PATH. Add /usr/local/go/bin to PATH and re-run."
        exit 1
    fi
    ok "Go $(go version | grep -oE 'go[0-9]+\.[0-9]+(\.[0-9]+)?') (installed)"
}

try_use_dotnet() {
    local candidate="$1"
    local required_sdk
    required_sdk="$(get_required_dotnet_sdk_version)"
    [[ -n "$candidate" ]] || return 1

    if [[ "$candidate" != "dotnet" && ! -x "$candidate" ]]; then
        return 1
    fi

    configure_dotnet_cli_environment

    if "$candidate" --list-sdks 2>/dev/null | awk '{print $1}' | grep -Fxq "$required_sdk"; then
        DOTNET_CMD="$candidate"
        if [[ "$candidate" != "dotnet" ]]; then
            local dotnet_dir
            dotnet_dir="$(dirname "$candidate")"
            if [[ -d "$dotnet_dir/sdk" ]]; then
                export DOTNET_ROOT="$dotnet_dir"
            fi
            prepend_path_once "$dotnet_dir"
            prepend_path_once "$dotnet_dir/tools"
        fi
        return 0
    fi

    return 1
}

resolve_dotnet() {
    local system_dotnet=""
    system_dotnet="$(command -v dotnet 2>/dev/null || true)"

    local candidates=(
        "$system_dotnet"
        "$DOTNET_FALLBACK_DIR/dotnet"
        "$HOME/.dotnet/dotnet"
        "/usr/share/dotnet/dotnet"
        "/usr/local/share/dotnet/dotnet"
        "/usr/local/bin/dotnet"
        "/usr/bin/dotnet"
        "/root/.dotnet/dotnet"
    )

    local candidate
    for candidate in "${candidates[@]}"; do
        if try_use_dotnet "$candidate"; then
            return 0
        fi
    done

    return 1
}

ensure_dotnet() {
    local required_sdk
    required_sdk="$(get_required_dotnet_sdk_version)"

    if resolve_dotnet; then
        ok ".NET SDK $required_sdk"
        return
    fi

    if ! $SKIP_DEPS; then
        case "$DISTRO_FAMILY" in
            alt|debian|rhel|suse|arch)
                # Distro packages can be too old / missing the pinned SDK; try them but expect fallback.
                case "$DISTRO_FAMILY" in
                    alt|debian) install_packages dotnet-sdk-8.0 || true ;;
                    rhel)       install_packages dotnet-sdk-8.0 || true ;;
                    suse)       install_packages dotnet-sdk-8.0 || true ;;
                    arch)       install_packages dotnet-sdk || true ;;
                esac
                ;;
        esac
    fi

    if resolve_dotnet; then
        ok ".NET SDK $required_sdk (from packages)"
        return
    fi

    info "Falling back to dotnet-install.sh (version $required_sdk)"
    local installer="/tmp/dotnet-install.sh"
    download_file "https://dot.net/v1/dotnet-install.sh" "$installer" \
        "Downloading dotnet-install.sh..." 1024
    chmod +x "$installer"
    mkdir -p "$DOTNET_FALLBACK_DIR"
    "$installer" --version "$required_sdk" --install-dir "$DOTNET_FALLBACK_DIR"
    rm -f "$installer"

    configure_dotnet_cli_environment
    export DOTNET_ROOT="$DOTNET_FALLBACK_DIR"
    prepend_path_once "$DOTNET_ROOT"
    prepend_path_once "$DOTNET_ROOT/tools"

    if ! resolve_dotnet; then
        err ".NET SDK installed, but build script cannot find a usable dotnet executable."
        err "Required SDK: $required_sdk"
        err "Expected path: $DOTNET_FALLBACK_DIR/dotnet"
        err "For manual commands run: export PATH=\"$DOTNET_FALLBACK_DIR:$DOTNET_FALLBACK_DIR/tools:\$PATH\""
        exit 1
    fi

    local ver; ver="$("$DOTNET_CMD" --version 2>/dev/null)"
    ok ".NET SDK $ver (installed at $DOTNET_CMD)"
}

# ─── Step 1: Build Go wrapper ─────────────────────────────────────────────────

build_go_wrapper() {
    step_header "Step 1: Build libXRayCore.so + gorilla-xray (Go)"

    if [[ ! -d "$WRAPPER_DIR" ]]; then
        err "XRay-Wrapper directory not found: $WRAPPER_DIR"
        exit 1
    fi

    pushd "$WRAPPER_DIR" >/dev/null

    local native_lib="libXRayCore.so"

    info "Building $native_lib for $RUNTIME..."
    CGO_ENABLED=1 GOOS=linux GOARCH="$GO_ARCH" \
    go build \
        --buildmode=c-shared \
        -o "$native_lib" \
        -trimpath \
        -ldflags "-s -w -buildid=" \
        .
    ok "$native_lib built ($(format_size "$(stat -c%s "$native_lib")"))"

    mkdir -p "$LIBRARIES_DIR"
    cp "$native_lib" "$LIBRARIES_DIR/"
    rm -f libXRayCore.h "$native_lib"

    info "Building gorilla-xray CLI binary..."
    CGO_ENABLED=1 GOOS=linux GOARCH="$GO_ARCH" \
    go build \
        -o gorilla-xray \
        -trimpath \
        -ldflags "-s -w -buildid= -X main.version=$APP_VERSION" \
        ./cmd/gorilla-xray/
    ok "gorilla-xray built ($(format_size "$(stat -c%s gorilla-xray)"))"

    popd >/dev/null
}

# ─── Step 2: Fetch tun2socks ──────────────────────────────────────────────────

fetch_tun2socks() {
    step_header "Step 2: Fetch tun2socks (xjasonlyu)"

    local tun_dir="$LINUX_DIR/TUN"
    mkdir -p "$tun_dir"

    local tmp="/tmp/$TUN2SOCKS_ASSET"
    download_file "$TUN2SOCKS_RELEASES/$TUN2SOCKS_ASSET" "$tmp" \
        "Downloading $TUN2SOCKS_ASSET..." 1000000

    rm -rf /tmp/tun2socks-extract
    mkdir -p /tmp/tun2socks-extract
    unzip -o "$tmp" -d /tmp/tun2socks-extract >/dev/null
    rm -f "$tmp"

    local bin
    bin="$(find /tmp/tun2socks-extract -type f -name 'tun2socks*' | head -1)"
    if [[ -z "$bin" ]]; then
        err "tun2socks binary not found in archive"
        exit 1
    fi

    cp "$bin" "$tun_dir/tun2socks"
    chmod +x "$tun_dir/tun2socks"
    rm -rf /tmp/tun2socks-extract

    ok "tun2socks → $tun_dir/tun2socks ($(format_size "$(stat -c%s "$tun_dir/tun2socks")"))"
}

# ─── Step 3: Geo files ────────────────────────────────────────────────────────

download_geo_files() {
    step_header "Step 3: Download geoip.dat / geosite.dat"

    download_file "$GEOIP_URL"   "$APP_DIR/geoip.dat"   "Downloading geoip.dat..."
    ok "geoip.dat ($(format_size "$(stat -c%s "$APP_DIR/geoip.dat")"))"

    download_file "$GEOSITE_URL" "$APP_DIR/geosite.dat" "Downloading geosite.dat..."
    ok "geosite.dat ($(format_size "$(stat -c%s "$APP_DIR/geosite.dat")"))"
}

# ─── Step 4: Build .NET app ───────────────────────────────────────────────────

build_dotnet_app() {
    step_header "Step 4: Build Avalonia Linux GUI ($CONFIGURATION, $RUNTIME)"

    local csproj="$LINUX_DIR/InvisibleGorilla-XRay.Linux.csproj"
    if [[ ! -f "$csproj" ]]; then
        err "Linux project not found: $csproj"
        exit 1
    fi

    pushd "$SCRIPT_DIR" >/dev/null

    info "Restoring NuGet packages..."
    "$DOTNET_CMD" restore "$csproj"

    local output_base; output_base="$(resolve_path "$OUTPUT_DIR")"
    local abs_output="$output_base/$RUNTIME"
    mkdir -p "$abs_output"

    info "Publishing self-contained single-file Linux build..."
    "$DOTNET_CMD" publish "$csproj" \
        -c "$CONFIGURATION" \
        -r "$RUNTIME" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$abs_output"

    local app_binary="$abs_output/$APP_BINARY_NAME"
    if [[ ! -f "$app_binary" ]]; then
        err "dotnet publish finished, but Linux binary was not created."
        err "Expected: $app_binary"
        err "Publish directory contents:"
        (cd "$abs_output" && find . -maxdepth 2 -type f | sort) >&2 || true
        exit 1
    fi

    chmod +x "$app_binary"
    ok "Published to: $abs_output"
    ok "Linux binary: $app_binary ($(format_size "$(stat -c%s "$app_binary")"))"
    popd >/dev/null
}

# ─── Step 5: Bundle ───────────────────────────────────────────────────────────

generate_png_icon() {
    local out_png="$1"
    info "Generating app icon..."
    python3 - <<PY "$out_png"
import math, struct, zlib, sys
W = H = 256
cx, cy, r = W//2, H//2, W//2 - 6
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
PY
    ok "App icon → $out_png"
}

package_bundle() {
    step_header "Step 5: Package Linux distribution"

    local dist_base; dist_base="$(resolve_path "$DIST_DIR")"
    local stage="$dist_base/InvisibleGorilla-XRay-${RUNTIME}"
    rm -rf "$stage"
    mkdir -p "$stage/bin" "$stage/share/applications" "$stage/share/icons" "$stage/share/autostart"

    local publish_dir="$(resolve_path "$OUTPUT_DIR")/$RUNTIME"
    local app_binary="$publish_dir/$APP_BINARY_NAME"
    if [[ ! -f "$app_binary" ]]; then
        err "Avalonia Linux binary missing — run './build.sh --step dotnet' first."
        err "Expected: $app_binary"
        if [[ -d "$publish_dir" ]]; then
            err "Publish directory contents:"
            (cd "$publish_dir" && find . -maxdepth 2 -type f | sort) >&2 || true
        fi
        exit 1
    fi
    chmod +x "$app_binary"

    cp -R "$publish_dir/"* "$stage/bin/"
    chmod +x "$stage/bin/$APP_BINARY_NAME"
    ok "Bundled: Avalonia GUI binary"

    cat > "$stage/$APP_RUNNER_NAME" <<'RUNNER'
#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec "$SCRIPT_DIR/bin/InvisibleGorilla-XRay.Linux" "$@"
RUNNER
    chmod +x "$stage/$APP_RUNNER_NAME"
    ok "Bundled: $APP_RUNNER_NAME launcher"

    cat > "$stage/bin/$APP_COMMAND_NAME" <<'COMMAND'
#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
exec "$SCRIPT_DIR/InvisibleGorilla-XRay.Linux" "$@"
COMMAND
    chmod +x "$stage/bin/$APP_COMMAND_NAME"
    ok "Bundled: $APP_COMMAND_NAME command wrapper"

    local libraries_src="$APP_DIR/Libraries/libXRayCore.so"
    if [[ -f "$libraries_src" ]]; then
        mkdir -p "$stage/bin/Libraries"
        cp "$libraries_src" "$stage/bin/Libraries/"
        chmod +x "$stage/bin/Libraries/libXRayCore.so"
        ok "Bundled: libXRayCore.so"
    else
        err "libXRayCore.so missing — run './build.sh --step go' first."
        exit 1
    fi

    local cli_src="$WRAPPER_DIR/gorilla-xray"
    if [[ -f "$cli_src" ]]; then
        cp "$cli_src" "$stage/bin/"
        chmod +x "$stage/bin/gorilla-xray"
        ok "Bundled: gorilla-xray CLI"
    fi

    local tun_src="$LINUX_DIR/TUN/tun2socks"
    if [[ -f "$tun_src" ]]; then
        mkdir -p "$stage/bin/TUN"
        cp "$tun_src" "$stage/bin/TUN/"
        chmod +x "$stage/bin/TUN/tun2socks"
        ok "Bundled: tun2socks"
    fi

    for dat in geoip.dat geosite.dat; do
        if [[ -f "$APP_DIR/$dat" ]]; then
            cp "$APP_DIR/$dat" "$stage/bin/"
            ok "Bundled: $dat"
        fi
    done

    if command_exists python3; then
        generate_png_icon "$stage/share/icons/invisible-gorilla-xray.png"
    fi

    cat > "$stage/share/applications/invisible-gorilla-xray.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=Invisible Gorilla XRay
GenericName=XRay client
Comment=Secure local proxy / TUN client
Exec=/opt/invisible-gorilla-xray/bin/invisible-gorilla-xray %u
Icon=invisible-gorilla-xray
Terminal=false
Categories=Network;
MimeType=x-scheme-handler/vless;x-scheme-handler/vmess;x-scheme-handler/invxray;x-scheme-handler/ig-xray;
StartupWMClass=InvisibleGorilla-XRay.Linux
DESKTOP
    ok "Bundled: invisible-gorilla-xray.desktop"

    cat > "$stage/README-LINUX.txt" <<README
Invisible Gorilla XRay Linux bundle

Run without installing:
  ./$APP_RUNNER_NAME

Install system-wide:
  ./install.sh

After install you can start it from the application menu or by command:
  $APP_COMMAND_NAME

Installed binary path:
  $INSTALL_DIR/bin/$APP_BINARY_NAME
README
    ok "Bundled: README-LINUX.txt"

    cat > "$stage/install.sh" <<'INSTALL'
#!/usr/bin/env bash
# Installs Invisible Gorilla XRay (Linux) into /opt and the user's XDG paths.
set -euo pipefail
SUDO="${SUDO:-sudo}"; [[ "$(id -u)" == "0" ]] && SUDO=""

INSTALL_DIR="/opt/invisible-gorilla-xray"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

$SUDO mkdir -p "$INSTALL_DIR"
$SUDO rm -rf "$INSTALL_DIR/bin" "$INSTALL_DIR/share"
$SUDO cp -R "$SCRIPT_DIR/bin" "$INSTALL_DIR/"
$SUDO cp -R "$SCRIPT_DIR/share" "$INSTALL_DIR/"

$SUDO install -Dm644 "$SCRIPT_DIR/share/applications/invisible-gorilla-xray.desktop" \
    /usr/share/applications/invisible-gorilla-xray.desktop
$SUDO install -Dm644 "$SCRIPT_DIR/share/icons/invisible-gorilla-xray.png" \
    /usr/share/icons/hicolor/256x256/apps/invisible-gorilla-xray.png || true
$SUDO install -Dm755 "$INSTALL_DIR/bin/invisible-gorilla-xray" \
    /usr/local/bin/invisible-gorilla-xray
$SUDO ln -sfn /usr/local/bin/invisible-gorilla-xray /usr/local/bin/igxray

if command -v update-desktop-database >/dev/null 2>&1; then
    $SUDO update-desktop-database /usr/share/applications || true
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    $SUDO gtk-update-icon-cache -t /usr/share/icons/hicolor || true
fi

cat <<DONE
Installed.

  Launcher:       Invisible Gorilla XRay (in your application menu)
  Command:        invisible-gorilla-xray
  Short command:  igxray
  Binary:         $INSTALL_DIR/bin/InvisibleGorilla-XRay.Linux

To uninstall: $SUDO rm -rf $INSTALL_DIR /usr/share/applications/invisible-gorilla-xray.desktop \\
              /usr/share/icons/hicolor/256x256/apps/invisible-gorilla-xray.png \\
              /usr/local/bin/invisible-gorilla-xray /usr/local/bin/igxray
DONE
INSTALL
    chmod +x "$stage/install.sh"
    ok "Bundled: install.sh"

    pushd "$dist_base" >/dev/null
    local archive="InvisibleGorilla-XRay-Linux-${RUNTIME}-v${APP_VERSION}.tar.gz"
    tar -czf "$archive" "$(basename "$stage")"
    popd >/dev/null

    local archive_path="$dist_base/$archive"
    echo
    ok "Distribution ready:"
    echo -e "     ${DIM}Stage:    $stage${NC}"
    echo -e "     ${DIM}Archive:  $archive_path ($(format_size "$(stat -c%s "$archive_path")"))${NC}"
    echo -e "     ${DIM}Run:      cd $stage && ./$APP_RUNNER_NAME${NC}"
    echo -e "     ${DIM}Install:  cd $stage && ./install.sh${NC}"
}

# ─── Argument parsing ─────────────────────────────────────────────────────────

show_help() {
    cat <<'HELP'
Invisible Gorilla XRay Client — Linux Build Script

Usage: ./build.sh [OPTIONS]

Options:
  --step <STEP>       Run specific build step:
                        all       - Full build (default)
                        deps      - Only install system dependencies
                        go        - Only build Go wrapper (XRayCore.so)
                        tun2socks - Only fetch tun2socks
                        geo       - Only download geo databases
                        dotnet    - Only build .NET application
                        bundle    - Only package distribution
  --config <CFG>      Build configuration: Debug or Release (default: Release)
  --runtime <RID>     .NET runtime identifier (default: auto-detected)
                        Supported: linux-x64, linux-arm64
  --output <DIR>      Publish directory (default: ./publish-linux)
  --dist <DIR>        Distribution directory (default: ./dist-linux)
  --skip-deps         Don't try to install system packages
  --help              Show this help

Distribution Detection:
  ALT Linux GNOME, Debian/Ubuntu (incl. Linux Mint, Pop!_OS), Fedora/RHEL,
  openSUSE, Arch — auto-detected via /etc/os-release.

Notes:
  * The Linux GUI uses Avalonia 11 with GTK 3 and a system tray indicator
    (StatusNotifierItem). On GNOME, the AppIndicator extension is required
    for the tray icon to be visible.
  * TUN mode requires CAP_NET_ADMIN: at runtime the app calls 'pkexec ip ...'
    (or sudo as a fallback) for ip/route/resolvectl operations.
  * Proxy mode uses the GNOME schema 'org.gnome.system.proxy' via gsettings.
    KDE-only sessions without that schema should use TUN mode instead.

Examples:
  ./build.sh                              # Full build
  ./build.sh --step go                    # Only XRayCore.so
  ./build.sh --runtime linux-arm64        # Cross-target arm64
  ./build.sh --skip-deps                  # Skip system package install
HELP
    exit 0
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --step)       STEP="$2"; shift 2 ;;
        --config)     CONFIGURATION="$2"; shift 2 ;;
        --runtime)    RUNTIME="$2"; shift 2 ;;
        --output)     OUTPUT_DIR="$2"; shift 2 ;;
        --dist)       DIST_DIR="$2"; shift 2 ;;
        --skip-deps)  SKIP_DEPS=true; shift ;;
        --help|-h)    show_help ;;
        *)            err "Unknown option: $1"; show_help ;;
    esac
done

case "$STEP" in
    all|deps|go|tun2socks|geo|dotnet|bundle) ;;
    *) err "Unknown step: $STEP"; exit 1 ;;
esac

# Re-resolve TUN2SOCKS_ASSET if user overrode RUNTIME
case "$RUNTIME" in
    linux-arm64) GO_ARCH="arm64"; TUN2SOCKS_ASSET="tun2socks-linux-arm64.zip" ;;
    linux-x64)   GO_ARCH="amd64"; TUN2SOCKS_ASSET="tun2socks-linux-amd64.zip" ;;
esac

# ─── Main ─────────────────────────────────────────────────────────────────────

SECONDS=0

echo
echo -e "${MAGENTA}  Invisible Gorilla - XRay Client :: Linux Build Script${NC}"
echo -e "${DIM}  v${APP_VERSION} | $RUNTIME | $(uname -srm)${NC}"
echo

detect_distro

install_deps() {
    step_header "Installing dependencies"
    ensure_build_tools
    ensure_runtime_libs
    ensure_go
    ensure_dotnet
}

case "$STEP" in
    all)
        install_deps
        build_go_wrapper
        fetch_tun2socks
        download_geo_files
        build_dotnet_app
        package_bundle
        ;;
    deps)      install_deps ;;
    go)        ensure_go; build_go_wrapper ;;
    tun2socks) fetch_tun2socks ;;
    geo)       download_geo_files ;;
    dotnet)    ensure_dotnet; build_dotnet_app ;;
    bundle)    package_bundle ;;
esac

elapsed=$SECONDS
mins=$((elapsed / 60))
secs=$((elapsed % 60))

echo
echo -e "${GREEN}============================================================${NC}"
printf "${GREEN}  Done in %d:%02d${NC}\n" "$mins" "$secs"
echo -e "${GREEN}============================================================${NC}"
echo
