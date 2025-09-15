#!/usr/bin/env bash
set -euo pipefail

# setup_and_run_mcp_server.sh
# Installs all dependencies, verifies installation, and launches the MCP server.

# -------- Configuration --------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="${SCRIPT_DIR}"
MCP_SERVER_FILE="${MCP_SERVER_FILE:-${REPO_ROOT}/mcp_server.py}"
VENV_DIR="${VENV_DIR:-${REPO_ROOT}/.venv}"
TRANSPORT="${MCP_TRANSPORT:-stdio}"
MCP_VERSION="${MCP_VERSION:-1.14.0}"

PACKAGES=(python3 python3-venv python3-pip git curl)

usage() {
  cat <<USAGE
Usage: $0 [--transport <stdio|sse|streamable-http>] [--skip-install]

Installs dependencies and runs the MCP server defined in ${MCP_SERVER_FILE}.

Options:
  --transport <value>   Transport protocol to use (default: ${TRANSPORT}).
  --skip-install        Skip system and Python dependency installation.
  -h, --help            Show this help and exit.

Environment variables:
  MCP_TRANSPORT   Default transport to use (overridden by --transport).
  MCP_SERVER_FILE Path to the MCP server Python module (default: ${MCP_SERVER_FILE}).
  VENV_DIR        Location of the Python virtual environment (default: ${VENV_DIR}).
  MCP_VERSION     Version of the "mcp" package to install (default: ${MCP_VERSION}).
USAGE
}

log() {
  printf '\033[1;32m[INFO]\033[0m %s\n' "$1"
}

warn() {
  printf '\033[1;33m[WARN]\033[0m %s\n' "$1"
}

err() {
  printf '\033[1;31m[ERROR]\033[0m %s\n' "$1" >&2
}

require_file() {
  local path=$1
  if [[ ! -f "$path" ]]; then
    err "Required server file not found: $path"
    exit 1
  fi
}

check_ubuntu() {
  if [[ ! -r /etc/os-release ]]; then
    err "Cannot verify operating system. /etc/os-release not found."
    exit 1
  fi
  source /etc/os-release
  local id_like="${ID_LIKE:-}" id="${ID:-}"
  if [[ "$id" != ubuntu && "${id_like}" != *ubuntu* && "${id_like}" != *debian* ]]; then
    err "This script is intended for Ubuntu systems. Detected ID=${id}."
    exit 1
  fi
  log "Detected Ubuntu-compatible environment (${id}${id_like:+, like: ${id_like}})."
}

ensure_root_or_sudo() {
  if [[ $(id -u) -eq 0 ]]; then
    SUDO=""
    return
  fi
  if command -v sudo >/dev/null 2>&1; then
    if sudo -n true 2>/dev/null; then
      SUDO="sudo"
    else
      warn "Sudo requires a password; you might be prompted."
      SUDO="sudo"
    fi
  else
    err "This script needs to install packages. Run as root or install sudo."
    exit 1
  fi
}

install_system_packages() {
  log "Installing system dependencies: ${PACKAGES[*]}"
  $SUDO apt-get update -y
  $SUDO apt-get install -y "${PACKAGES[@]}"
}

create_virtualenv() {
  if [[ ! -d "$VENV_DIR" ]]; then
    log "Creating Python virtual environment at $VENV_DIR"
    python3 -m venv "$VENV_DIR"
  else
    log "Using existing virtual environment at $VENV_DIR"
  fi
}

install_python_dependencies() {
  log "Installing Python dependencies into $VENV_DIR"
  "${VENV_DIR}/bin/python" -m pip install --upgrade pip
  PIP_NO_INPUT=1 "${VENV_DIR}/bin/python" -m pip install "mcp[cli]==${MCP_VERSION}"
}

verify_installation() {
  log "Verifying installation"
  "${VENV_DIR}/bin/python" -m pip check
  "${VENV_DIR}/bin/python" -m py_compile "$MCP_SERVER_FILE"
  "${VENV_DIR}/bin/mcp" version
}

run_server() {
  case "$TRANSPORT" in
    stdio|sse|streamable-http)
      ;;
    *)
      err "Unsupported transport: $TRANSPORT"
      exit 1
      ;;
  esac
  log "Launching MCP server using transport '$TRANSPORT'"
  exec "${VENV_DIR}/bin/mcp" run --transport "$TRANSPORT" "$MCP_SERVER_FILE"
}

SKIP_INSTALL=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --transport)
      if [[ $# -lt 2 ]]; then
        err "Missing value for --transport"
        exit 1
      fi
      TRANSPORT="$2"
      shift 2
      ;;
    --skip-install)
      SKIP_INSTALL=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      err "Unknown argument: $1"
      usage
      exit 1
      ;;
  esac
done

require_file "$MCP_SERVER_FILE"
check_ubuntu
ensure_root_or_sudo

if [[ $SKIP_INSTALL -eq 0 ]]; then
  install_system_packages
  create_virtualenv
  install_python_dependencies
else
  log "Skipping dependency installation as requested"
  if [[ ! -x "${VENV_DIR}/bin/python" ]]; then
    err "Virtual environment not found at $VENV_DIR. Remove --skip-install to create it."
    exit 1
  fi
fi

verify_installation
run_server
