#!/usr/bin/env bash
set -euo pipefail
server_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
dotnet_bin="${DOTNET:-/Applications/Unity/Hub/Editor/6000.6.6f1-arm64/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet}"
if [[ ! -x "$dotnet_bin" ]]; then
  dotnet_bin="$(command -v dotnet || true)"
fi
if [[ -z "$dotnet_bin" ]]; then
  printf '%s\n' 'SDK .NET 8 introuvable. Définissez DOTNET vers un exécutable compatible.' >&2
  exit 1
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
exec "$dotnet_bin" run --project "$server_dir/NGame.Server.csproj" -- "$@"
