#!/usr/bin/env bash
# Lets esbuild resolve @angular/* and rxjs when bundling the NSwag client under clients/angular/.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CLIENT_NM="$ROOT/src/iM.Cloud.Client/node_modules"
API_NM="$ROOT/clients/angular/node_modules"

if [[ ! -d "$CLIENT_NM" ]]; then
  exit 0
fi

mkdir -p "$API_NM/@angular"

link_pkg() {
  local name="$1"
  local src="$CLIENT_NM/$name"
  local dest="$API_NM/$name"

  if [[ ! -e "$src" ]]; then
    return 0
  fi

  if [[ -e "$dest" || -L "$dest" ]]; then
    return 0
  fi

  ln -s "$(realpath --relative-to="$(dirname "$dest")" "$src")" "$dest"
}

link_pkg "@angular/common"
link_pkg "@angular/core"
link_pkg "rxjs"
link_pkg "tslib"
