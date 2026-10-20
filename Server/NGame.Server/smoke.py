#!/usr/bin/env python3
"""Real multi-client HTTP checks. Defaults to an isolated server on a free localhost port."""
import argparse
import concurrent.futures
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
import uuid

HERE = Path(__file__).resolve().parent
UNITY_DOTNET = "/Applications/Unity/Hub/Editor/6000.6.6f1-arm64/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
TOKENS = []
CHECKS = 0


def check(condition, message):
    global CHECKS
    if not condition:
        raise AssertionError(message)
    CHECKS += 1


def http(base, method, path, payload=None, token=None, expected=200, raw=None, content_type="application/json"):
    body = raw if raw is not None else (json.dumps(payload).encode() if payload is not None else None)
    headers = {"Content-Type": content_type} if body is not None else {}
    if token is not None:
        headers["Authorization"] = "Bearer " + token
    request = urllib.request.Request(base + path, data=body, headers=headers, method=method)
    try:
        response = urllib.request.urlopen(request, timeout=5)
    except urllib.error.HTTPError as error:
        response = error
    status, data, response_headers = response.status, response.read(), response.headers
    check(status == expected, f"{method} {path.split('?')[0]}: HTTP {status}, attendu {expected}; code="
          + (str(json.loads(data).get("code", "?")) if data.startswith(b"{") else "non-json"))
    if response_headers.get_content_type() == "application/json":
        return json.loads(data), response_headers
    return data, response_headers


def create(base, name, settings=None):
    payload = {"displayName": name, "avatarIndex": 0}
    if settings is not None:
        payload["settings"] = settings
    access, _ = http(base, "POST", "/api/rooms", payload, expected=201)
    TOKENS.append(access["sessionToken"])
    return access


def path(client, suffix):
    return "/api/rooms/" + client["roomCode"] + "/" + suffix


def state(base, client):
    view, headers = http(base, "GET", path(client, "state"), token=client["sessionToken"])
    check("no-store" in headers.get("Cache-Control", ""), "Les états privés doivent interdire le cache.")
    return view


def command(base, client, kind, turn=None, expected=200, command_id=None, **fields):
    if turn is None:
        turn = state(base, client)["turn"]
    payload = {"kind": kind, "turn": turn, "commandId": command_id or uuid.uuid4().hex, **fields}
    result, _ = http(base, "POST", path(client, "commands"), payload, token=client["sessionToken"], expected=expected)
    check(result["ok"] == (expected == 200), "Le résultat doit correspondre au statut HTTP.")
    return result, payload


def wait_view(base, client, predicate, timeout=8):
    end = time.monotonic() + timeout
    while time.monotonic() < end:
        view = state(base, client)
        if predicate(view):
            return view
        time.sleep(0.12)
    raise AssertionError("Le moteur n’a pas atteint la phase attendue dans le délai.")


def run_checks(base, local, grace):
    health, _ = http(base, "GET", "/health")
    check(health["status"] == "ok", "Health doit confirmer le service.")
    settings = {"mode": "in-person", "ruleset": "ngame-classic-v1", "discussionSeconds": 5,
                "nightSeconds": 120, "revealEliminatedRoles": False,
                "composition": [{"roleId": "werewolf", "count": 1}, {"roleId": "seer", "count": 1},
                                {"roleId": "villager", "count": 4}]}
    host = create(base, "Hôte HTTP", settings)
    check(len(host["roomCode"]) == 6 and host["roomCode"].isalnum(), "Code de salle de six caractères.")
    check(len(host["sessionToken"]) == 43 and len(host["playerId"]) == 32, "Identifiants opaques attendus.")
    no_auth, _ = http(base, "GET", path(host, "state"), expected=401)
    check(no_auth["code"] == "unauthorized" and no_auth.get("view") is None, "Aucun état sans authentification.")
    http(base, "GET", path(host, "state") + "?sessionToken=" + host["sessionToken"], expected=401)
    http(base, "GET", path(host, "state"), token="A" * 43, expected=401)
    other = create(base, "Autre salle")
    http(base, "GET", path(other, "state"), token=host["sessionToken"], expected=401)
    http(base, "GET", "/api/rooms/AAAAAA/state", token=host["sessionToken"], expected=404)
    http(base, "POST", "/api/rooms", {"displayName": "x" * 25}, expected=400)
    http(base, "POST", "/api/rooms", {"displayName": "Avatar", "avatarIndex": 6}, expected=400)
    http(base, "POST", "/api/rooms", raw=b"{broken", expected=400)
    http(base, "POST", "/api/rooms", raw=b"{}", content_type="text/plain", expected=415)
    http(base, "POST", "/api/rooms", raw=b'{' + b' ' * 17000 + b'}', expected=413)
    result, _ = command(base, host, "start", expected=409)
    check(result["code"] == "NOT_ENOUGH_PLAYERS", "La partie exige au moins six joueurs.")

    def join(index):
        result, _ = http(base, "POST", path(host, "join"), {"displayName": "Joueur " + str(index), "avatarIndex": index % 6})
        TOKENS.append(result["sessionToken"])
        return result

    with concurrent.futures.ThreadPoolExecutor(max_workers=5) as pool:
        clients = [host] + list(pool.map(join, range(1, 6)))
    check(len({c["playerId"] for c in clients}) == 6, "Six acteurs HTTP distincts.")
    http(base, "POST", path(host, "join"), {"displayName": "Joueur 1", "avatarIndex": 0}, expected=409)
    resumed, _ = http(base, "POST", path(host, "join"), {"resumeToken": clients[1]["sessionToken"]})
    check(resumed["playerId"] == clients[1]["playerId"] and len(resumed["view"]["players"]) == 6,
          "La reprise conserve l’acteur sans ajouter de joueur.")
    http(base, "POST", path(host, "join"), {"resumeToken": "A" * 43, "displayName": "Intrus"}, expected=401)
    result, _ = command(base, clients[1], "start", expected=409)
    check(result["code"] == "HOST_ONLY", "Un invité ne démarre pas la partie.")
    result, _ = command(base, host, "start", expected=409)
    check(result["code"] == "NOT_READY", "Chacun doit se déclarer prêt.")
    http(base, "POST", path(host, "commands"), {"kind": "ready", "turn": 0, "commandId": uuid.uuid4().hex,
         "ready": True, "actorId": host["playerId"]}, token=clients[1]["sessionToken"], expected=400)
    check(not next(p for p in state(base, host)["players"] if p["id"] == host["playerId"])["ready"],
          "Un champ acteur falsifié ne modifie pas l’hôte.")
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
        list(pool.map(lambda client: command(base, client, "ready", turn=0, ready=True), clients))
    check(all(p["ready"] for p in state(base, host)["players"]), "Les six confirmations concurrentes sont conservées.")
    result, _ = command(base, clients[1], "configure", expected=409, settings=settings)
    check(result["code"] == "HOST_ONLY", "Les réglages restent réservés à l’hôte.")
    result, payload = command(base, host, "ready", ready=True)
    duplicate, _ = http(base, "POST", path(host, "commands"), payload, token=host["sessionToken"])
    check(duplicate["ok"], "Une relance identique est idempotente.")
    altered = {**payload, "ready": False}
    conflict, _ = http(base, "POST", path(host, "commands"), altered, token=host["sessionToken"], expected=409)
    check(conflict["code"] == "command_conflict", "Un commandId réutilisé avec un autre contenu est refusé.")

    started, _ = command(base, host, "start")
    check(started["view"]["phase"] == "distribution" and started["view"]["turn"] > 0, "Le démarrage laisse lire les cartes avant la nuit.")
    views = [state(base, client) for client in clients]
    role_clients = {}
    for client, view in zip(clients, views):
        check(view["selfPlayerId"] == client["playerId"] and bool(view["selfRoleId"]), "Chaque acteur reçoit son propre rôle.")
        check(all(p.get("revealedRoleId") is None for p in view["players"]), "Aucun rôle vivant dans les données publiques.")
        check(view["privateFacts"] == [], "Aucune information privée d’autrui au début.")
        role_clients.setdefault(view["selfRoleId"], []).append(client)
    check({k: len(v) for k, v in role_clients.items()} == {"werewolf": 1, "seer": 1, "villager": 4},
          "La composition autoritaire est exacte.")
    self_only, _ = http(base, "GET", path(clients[1], "state") + "?playerId=" + host["playerId"], token=clients[1]["sessionToken"])
    check(self_only["selfPlayerId"] == clients[1]["playerId"], "L’acteur est dérivé du token, jamais d’un paramètre client.")
    http(base, "POST", path(host, "join"), {"displayName": "Retard", "avatarIndex": 0}, expected=409)
    stale, _ = command(base, host, "ready", turn=0, expected=409, ready=True)
    check(stale["code"] == "STALE_TURN" and stale["view"]["turn"] > 0, "Le tour périmé est refusé avec un état actuel.")
    for client in clients:
        command(base, client, "acknowledge-role")
    wait_view(base, host, lambda view: view["phase"] == "night")
    outsider = role_clients["villager"][0]
    forbidden, _ = command(base, outsider, "act", expected=409, choice="pass", targetIds=[])
    check(forbidden["code"] == "NO_ACTION", "Une personne sans action nocturne ne peut pas agir.")

    seer, wolf = role_clients["seer"][0], role_clients["werewolf"][0]
    seer_view = state(base, seer)
    check(seer_view["action"]["kind"] == "seer", "L’étape autorise seulement la Voyante.")
    command(base, seer, "act", choice="inspect", targetIds=[wolf["playerId"]])
    wait_view(base, wolf, lambda view: (view.get("action") or {}).get("kind") == "wolves")
    seer_view = state(base, seer)
    check(any(f["playerId"] == wolf["playerId"] and f["roleId"] == "werewolf" for f in seer_view["privateFacts"]),
          "La Voyante reçoit le résultat privé de son enquête.")
    for client in clients:
        if client["playerId"] != seer["playerId"]:
            check(state(base, client)["privateFacts"] == [], "L’enquête ne fuit pas aux autres acteurs HTTP.")
    invalid, _ = command(base, wolf, "act", expected=409, choice="kill", targetIds=[wolf["playerId"]])
    check(invalid["code"] == "INVALID_TARGET", "Une cible interdite est refusée.")
    victim = role_clients["villager"][0]
    kill, kill_payload = command(base, wolf, "act", choice="kill", targetIds=[victim["playerId"]])
    http(base, "POST", path(wolf, "commands"), kill_payload, token=wolf["sessionToken"])
    dawn = wait_view(base, host, lambda view: view["phase"] == "dawn")
    check(not next(p for p in dawn["players"] if p["id"] == victim["playerId"])["alive"], "La nuit élimine la cible une seule fois.")
    check(all(p.get("revealedRoleId") is None for p in dawn["players"]), "La révélation désactivée protège aussi les morts.")
    http(base, "POST", path(seer, "commands"), {"kind": "act", "turn": seer_view["turn"],
         "commandId": uuid.uuid4().hex, "choice": "inspect", "targetIds": [wolf["playerId"]]}, token=seer["sessionToken"], expected=409)

    if local:
        time.sleep(grace + 0.2)
        connection_view = state(base, host)
        check(next(p for p in connection_view["players"] if p["id"] == host["playerId"])["connected"], "Le client qui interroge est connecté.")
        check(not next(p for p in connection_view["players"] if p["id"] == clients[1]["playerId"])["connected"],
              "Le statut connecté expire après l’absence réelle de requêtes.")
        state(base, clients[1])
        check(next(p for p in state(base, host)["players"] if p["id"] == clients[1]["playerId"])["connected"],
              "Un nouvel échange rétablit la connexion.")

    wait_view(base, host, lambda view: view["phase"] == "vote", timeout=12)
    alive = [c for c in clients if c["playerId"] != victim["playerId"]]
    for client in alive:
        if client["playerId"] == wolf["playerId"]:
            command(base, client, "vote", choice="pass", targetIds=[])
        else:
            command(base, client, "vote", choice="vote", targetIds=[wolf["playerId"]])
    verdict = wait_view(base, host, lambda view: view["phase"] == "verdict")
    check(verdict["verdict"]["eliminatedPlayerId"] == wolf["playerId"], "Le verdict est lisible avant la fin.")
    finished = wait_view(base, host, lambda view: view["phase"] == "finished")
    check(finished["phase"] == "finished" and bool(finished["winner"]), "Le vote autoritaire termine réellement la partie.")
    check(wolf["playerId"] not in finished["winnerIds"], "La meute éliminée ne figure pas parmi les gagnants.")
    replay, _ = http(base, "POST", path(wolf, "commands"), kill_payload, token=wolf["sessionToken"])
    check(replay["ok"] and replay["view"]["phase"] == "finished", "Une relance ancienne renvoie un état actuel sans rejouer la nuit.")
    host = next(c for c in clients if c["playerId"] == finished["hostPlayerId"])
    command(base, host, "reset")
    reset = state(base, host)
    check(reset["phase"] == "lobby" and reset["turn"] > finished["turn"] and not reset.get("selfRoleId"),
          "La revanche retire les rôles et conserve un tour monotone.")

    capacity = create(base, "Capacité")
    for index in range(1, 24):
        http(base, "POST", path(capacity, "join"), {"displayName": "Place " + str(index), "avatarIndex": index % 6})
    full, _ = http(base, "POST", path(capacity, "join"), {"displayName": "Place 25", "avatarIndex": 0}, expected=409)
    check(full["code"] == "room_full", "La salle est bornée à 24 acteurs.")
    unsupported = {**settings, "composition": [{"roleId": "white-wolf", "count": 1}, {"roleId": "villager", "count": 5}]}
    http(base, "POST", "/api/rooms", {"displayName": "Rôle refusé", "avatarIndex": 0, "settings": unsupported}, expected=400)
    if local:
        index, _ = http(base, "GET", "/")
        check(b"smoke-static" in index, "L’accueil Web optionnel est servi.")
        wasm, headers = http(base, "GET", "/smoke.wasm")
        check(headers.get_content_type() == "application/wasm", "Le MIME WebAssembly est correct.")
        _, headers = http(base, "GET", "/smoke.wasm.br")
        check(headers.get_content_type() == "application/wasm" and headers.get("Content-Encoding") == "br",
              "Le build compressé conserve le MIME et son encodage.")
        expiring = create(base, "Expiration")
        time.sleep(6.1)
        expired, _ = http(base, "GET", path(expiring, "state"), token=expiring["sessionToken"], expected=404)
        check(expired["code"] == "room_not_found", "Une salle inactive et ses sessions expirent.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", help="Tester explicitement un serveur existant sans le démarrer ni l’arrêter.")
    parser.add_argument("--no-build", action="store_true", help="Réutiliser le build local existant.")
    args = parser.parse_args()
    if args.base_url:
        run_checks(args.base_url.rstrip("/"), False, 15)
    else:
        dotnet = os.environ.get("DOTNET") or (UNITY_DOTNET if Path(UNITY_DOTNET).is_file() else shutil.which("dotnet"))
        if not dotnet:
            raise SystemExit("SDK .NET 8 introuvable : définir DOTNET vers l’exécutable.")
        env = {**os.environ, "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1"}
        if not args.no_build:
            subprocess.run([dotnet, "build", str(HERE / "NGame.Server.csproj"), "--nologo", "-v", "quiet"], check=True, env=env)
        with socket.socket() as probe:
            probe.bind(("127.0.0.1", 0))
            port = probe.getsockname()[1]
        base = "http://127.0.0.1:" + str(port)
        with tempfile.TemporaryDirectory(prefix="ngame-http-smoke-") as temporary:
            web = Path(temporary) / "web"
            web.mkdir()
            (web / "index.html").write_text("<!doctype html><title>smoke-static</title>")
            (web / "smoke.wasm").write_bytes(b"\0asm\x01\0\0\0")
            (web / "smoke.wasm.br").write_bytes(b"header-check-only")
            env.update({"ASPNETCORE_URLS": base, "NGAME_WEB_ROOT": str(web), "NGAME_ROOM_TTL_SECONDS": "5",
                        "NGAME_CONNECTED_GRACE_SECONDS": "2", "NGAME_ALLOWED_ORIGINS": "",
                        "NGAME_DATA_DIRECTORY": str(Path(temporary) / "private-state")})
            with tempfile.TemporaryFile(mode="w+") as log:
                process = subprocess.Popen([dotnet, str(HERE / "bin/Debug/net8.0/NGame.Server.dll")], env=env,
                                           cwd=HERE, stdout=log, stderr=subprocess.STDOUT)
                try:
                    deadline = time.monotonic() + 10
                    while True:
                        try:
                            urllib.request.urlopen(base + "/health", timeout=0.2).close()
                            break
                        except (OSError, urllib.error.URLError):
                            if process.poll() is not None or time.monotonic() > deadline:
                                raise AssertionError("Le serveur de test n’a pas démarré.")
                            time.sleep(0.1)
                    run_checks(base, True, 2)
                finally:
                    process.terminate()
                    try:
                        process.wait(timeout=5)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait(timeout=5)
                    log.seek(0)
                    output = log.read()
                    check(not any(token in output for token in TOKENS), "Aucun token de session dans les journaux serveur.")
    print(f"OK — {CHECKS} vérifications HTTP, six clients distincts, partie complète, confidentialité et limites.")


if __name__ == "__main__":
    main()
