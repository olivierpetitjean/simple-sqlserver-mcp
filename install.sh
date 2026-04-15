#!/usr/bin/env bash
set -euo pipefail

project_name="simple-sqlserver-mcp"
default_source_archive_url="https://github.com/olivierpetitjean/simple-sqlserver-mcp/releases/latest/download/simple-sqlserver-mcp-runtime-source.zip"
source_archive_url="${SIMPLE_SQLSERVER_MCP_SOURCE_ARCHIVE_URL:-$default_source_archive_url}"
install_root="${SIMPLE_SQLSERVER_MCP_INSTALL_DIR:-/opt/simple-sqlserver-mcp}"
bin_dir="${SIMPLE_SQLSERVER_MCP_BIN_DIR:-/usr/local/bin}"
app_dir="${install_root}/app"
wrapper_path="${bin_dir}/simple-sqlserver-mcp"
dotnet_channel="${SIMPLE_SQLSERVER_MCP_DOTNET_CHANNEL:-8.0}"
work_dir="$(mktemp -d)"
dotnet_cmd=""
downloaded_sdk="false"

cleanup() {
  rm -rf "$work_dir"
}

trap cleanup EXIT

log() {
  printf '[%s] %s\n' "$project_name" "$1"
}

fail() {
  printf '[%s] ERROR: %s\n' "$project_name" "$1" >&2
  exit 1
}

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

run_as_root() {
  if [ "${EUID}" -eq 0 ]; then
    "$@"
    return
  fi

  if command_exists sudo; then
    sudo "$@"
    return
  fi

  fail "Root privileges are required to write to ${install_root} and ${bin_dir}. Re-run as root or install sudo."
}

download_file() {
  local source="$1"
  local destination="$2"

  if [ -f "$source" ]; then
    cp "$source" "$destination"
    return
  fi

  case "$source" in
    file://*)
      cp "${source#file://}" "$destination"
      return
      ;;
  esac

  if command_exists curl; then
    curl -fsSL "$source" -o "$destination"
    return
  fi

  if command_exists wget; then
    wget -qO "$destination" "$source"
    return
  fi

  fail "Neither curl nor wget is available. Install one of them and try again."
}

extract_zip() {
  local archive_path="$1"
  local destination="$2"

  if command_exists unzip; then
    unzip -q "$archive_path" -d "$destination"
    return
  fi

  if command_exists python3; then
    python3 - "$archive_path" "$destination" <<'PY'
import sys
import zipfile

archive_path, destination = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(archive_path) as archive:
    archive.extractall(destination)
PY
    return
  fi

  fail "Neither unzip nor python3 is available to extract ${archive_path}."
}

resolve_runtime_identifier() {
  local machine
  machine="$(uname -m)"

  case "$machine" in
    x86_64|amd64)
      printf 'linux-x64'
      ;;
    aarch64|arm64)
      printf 'linux-arm64'
      ;;
    *)
      fail "Unsupported Linux architecture: ${machine}"
      ;;
  esac
}

ensure_dotnet_sdk() {
  if command_exists dotnet; then
    dotnet_cmd="$(command -v dotnet)"
    return
  fi

  downloaded_sdk="true"
  local install_script="${work_dir}/dotnet-install.sh"
  local sdk_dir="${work_dir}/.dotnet"

  log "dotnet was not found. Downloading a temporary .NET SDK installer."
  download_file "https://dot.net/v1/dotnet-install.sh" "$install_script"
  chmod +x "$install_script"
  "$install_script" --channel "$dotnet_channel" --install-dir "$sdk_dir" >/dev/null
  dotnet_cmd="${sdk_dir}/dotnet"
}

create_wrapper() {
  local wrapper_temp_path="${work_dir}/simple-sqlserver-mcp"

  cat > "$wrapper_temp_path" <<EOF
#!/usr/bin/env bash
set -euo pipefail

if [ -x "${app_dir}/SimpleSqlServerMcp" ]; then
  exec "${app_dir}/SimpleSqlServerMcp" "\$@"
fi

exec dotnet "${app_dir}/SimpleSqlServerMcp.dll" "\$@"
EOF

  chmod +x "$wrapper_temp_path"
  run_as_root mkdir -p "$bin_dir"
  run_as_root install -m 755 "$wrapper_temp_path" "$wrapper_path"
}

install_application() {
  local runtime_identifier="$1"
  local source_archive_path="${work_dir}/simple-sqlserver-mcp-runtime-source.zip"
  local extract_dir="${work_dir}/extract"
  local source_root="${extract_dir}"
  local publish_dir="${work_dir}/publish"

  log "Downloading runtime source archive."
  download_file "$source_archive_url" "$source_archive_path"

  mkdir -p "$extract_dir"
  extract_zip "$source_archive_path" "$extract_dir"

  if [ ! -f "${source_root}/src/SimpleSqlServerMcp/SimpleSqlServerMcp.csproj" ]; then
    fail "The downloaded archive does not contain src/SimpleSqlServerMcp/SimpleSqlServerMcp.csproj."
  fi

  log "Publishing SimpleSqlServerMcp for ${runtime_identifier}."
  "$dotnet_cmd" publish \
    "${source_root}/src/SimpleSqlServerMcp/SimpleSqlServerMcp.csproj" \
    -c Release \
    -r "$runtime_identifier" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$publish_dir"

  run_as_root mkdir -p "$app_dir"
  run_as_root rm -rf "${app_dir:?}/"*
  run_as_root cp -a "${publish_dir}/." "$app_dir/"

  if [ -f "${source_root}/LICENSE" ]; then
    run_as_root install -m 644 "${source_root}/LICENSE" "${install_root}/LICENSE.txt"
  fi

  if [ -f "${source_root}/THIRD-PARTY-NOTICES.txt" ]; then
    run_as_root install -m 644 "${source_root}/THIRD-PARTY-NOTICES.txt" "${install_root}/THIRD-PARTY-NOTICES.txt"
  fi
}

print_final_message() {
  cat <<EOF

Simple SQL Server MCP installation completed successfully.

Installed files:
- Application directory: ${app_dir}
- Command wrapper: ${wrapper_path}
- Executable to reference in MCP clients: ${wrapper_path}

Configure your MCP client to launch:
- Command: ${wrapper_path}

Required environment variables:
- SQLSERVER_HOST
- SQLSERVER_DATABASE
- Either SQLSERVER_INTEGRATED_SECURITY=true
- Or SQLSERVER_USERNAME and SQLSERVER_PASSWORD

Optional environment variables:
- SQLSERVER_PORT (default: 1433)
- SQLSERVER_ENCRYPT (default: true)
- SQLSERVER_TRUST_SERVER_CERTIFICATE (default: false)
- SQLSERVER_APPLICATION_NAME (default: SimpleSqlServerMcp)
- MCP_SQLSERVER_MODE (default: read-only)
- MCP_SQLSERVER_MAX_ROWS (default: 100)
- MCP_SQLSERVER_COMMAND_TIMEOUT (default: 15)
- MCP_SQLSERVER_ALLOWED_DATABASES (default: *)
- MCP_SQLSERVER_EXCLUDE_SYSTEM_DATABASES (default: true)
- MCP_SQLSERVER_UNSAFE_ALLOWED_PATTERNS (default: empty)
- MCP_SQLSERVER_LOG_LEVEL (default: Information)

Project documentation:
- https://github.com/olivierpetitjean/simple-sqlserver-mcp
EOF

  if [ "$downloaded_sdk" = "true" ]; then
    cat <<EOF

Notes:
- A temporary .NET SDK was downloaded only to build the self-contained Linux binary.
- The installed application does not depend on a system-wide .NET runtime.
EOF
  fi
}

if [ "$(uname -s)" != "Linux" ]; then
  fail "This installer currently supports Linux only."
fi

runtime_identifier="$(resolve_runtime_identifier)"
ensure_dotnet_sdk
install_application "$runtime_identifier"
create_wrapper
print_final_message
