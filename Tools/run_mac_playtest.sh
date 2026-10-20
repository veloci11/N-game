#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
app_path="$project_dir/Builds/Mac/NGame.app"
if [[ ! -d "$app_path" ]]; then
  printf '%s\n' 'Construisez d’abord la cible StandaloneOSX vers Builds/Mac/NGame.app dans Unity.' >&2
  exit 1
fi
plutil -lint "$app_path/Contents/Info.plist"
# Signature locale pour cet essai uniquement : pas de signature Developer ID ni de notarisation.
codesign --force --deep --sign - "$app_path"
codesign --verify --deep --strict "$app_path"
exec unity build run "$project_dir" --path "$app_path" --format json
