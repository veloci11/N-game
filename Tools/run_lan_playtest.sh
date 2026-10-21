#!/usr/bin/env bash
# Explicit, local-only listener for a table playtest. Never configures the network.
set -euo pipefail
usage() {
  cat <<'HELP'
Usage: Tools/run_lan_playtest.sh --listen-url http://ADRESSE:PORT [--check]

Adresse obligatoire : localhost, IPv4 127.x.x.x, 10.x.x.x,
172.16.x.x à 172.31.x.x, 192.168.x.x ou IPv6 [::1]. Port : 1–65535.
Aucune écoute par défaut. 0.0.0.0, adresses publiques, DNS et HTTPS refusés.
--check  Valide et affiche l’adresse sans lancer le serveur.
--help   Affiche cette aide.

Exemple à adapter à l’IP réellement attribuée au Mac :
  Tools/run_lan_playtest.sh --listen-url http://192.168.1.42:8787
HELP
}
fail() { printf '%s\n' "Erreur : $*" >&2; exit 2; }
listen_url=''
check_only=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --help|-h) usage; exit 0 ;;
    --check) check_only=true; shift ;;
    --listen-url)
      [[ $# -ge 2 && "$2" != --* ]] || fail '--listen-url exige une adresse complète.'
      [[ -z "$listen_url" ]] || fail '--listen-url ne doit apparaître qu’une fois.'
      listen_url="$2"; shift 2 ;;
    *) fail "Option inconnue : $1" ;;
  esac
done
[[ -n "$listen_url" ]] || fail 'Précise --listen-url ; aucune adresse n’est choisie automatiquement.'
listen_url="${listen_url%/}"
loopback=false
if [[ "$listen_url" =~ ^http://(localhost|\[::1\]):([0-9]{1,5})$ ]]; then
  port="${BASH_REMATCH[2]}"
  loopback=true
elif [[ "$listen_url" =~ ^http://([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3}):([0-9]{1,5})$ ]]; then
  first=$((10#${BASH_REMATCH[1]})); second=$((10#${BASH_REMATCH[2]}))
  third=$((10#${BASH_REMATCH[3]})); fourth=$((10#${BASH_REMATCH[4]}))
  port="${BASH_REMATCH[5]}"
  (( first <= 255 && second <= 255 && third <= 255 && fourth <= 255 )) || fail 'Octet IPv4 invalide.'
  if (( first == 127 )); then
    loopback=true
  elif ! (( first == 10 || (first == 172 && second >= 16 && second <= 31) || (first == 192 && second == 168) )); then
    fail 'Adresse refusée : utilise une adresse privée LAN ou de boucle locale explicite.'
  fi
  # Canonical decimal form avoids ambiguous leading-zero IPv4 notation.
  listen_url="http://${first}.${second}.${third}.${fourth}:${port}"
else
  fail 'Format attendu : http://adresse-privée:port (pas de chemin, identifiants ou adresse publique).'
fi
port=$((10#$port))
(( port >= 1 && port <= 65535 )) || fail 'Port attendu entre 1 et 65535.'
# Normalize port independently, including localhost and IPv6 loopback.
listen_url="${listen_url%:*}:$port"
printf '\n%s\n' "Serveur de recette : $listen_url" "État de santé : $listen_url/health"
if [[ "$loopback" == true ]]; then
  printf '%s\n' 'Boucle locale : accessible sur ce Mac uniquement, pas depuis les téléphones.'
else
  printf '%s\n' "Dans chaque client du même réseau, saisis exactement : $listen_url" \
    'Le code de salle sert ensuite à rejoindre ; il ne remplace pas cette adresse serveur.'
fi
printf '%s\n' 'Aucune modification de pare-feu, de routeur ou de réseau ; aucun hébergement public.'
if [[ "$check_only" == true ]]; then
  printf '%s\n' 'Validation seule : aucun serveur lancé, aucune interface réseau ouverte.'
  exit 0
fi
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
export ASPNETCORE_URLS="$listen_url"
# The explicit CLI value also prevents an inherited ASP.NET setting from widening the listener.
exec bash "$script_dir/../Server/NGame.Server/run.sh" --urls "$listen_url"
