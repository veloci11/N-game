#!/usr/bin/env python3
"""Isolated localhost HTTP regression for fixed screen-night calls and legacy settings."""
import copy
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import time
import urllib.request

import smoke

HERE = Path(__file__).resolve().parent


def run(base, restart):
    legacy = smoke.create(base, "Legacy")
    smoke.check(legacy["view"]["settings"]["nightProtocol"] == "traditional", "Missing JSON field retains the traditional protocol.")
    settings = {"mode": "in-person", "nightProtocol": "screen", "nightSeconds": 8, "discussionSeconds": 5,
                "composition": [{"roleId": "werewolf", "count": 1}, {"roleId": "seer", "count": 1},
                                {"roleId": "villager", "count": 4}]}
    invalid = copy.deepcopy(settings)
    invalid["nightProtocol"] = "visual"
    smoke.http(base, "POST", "/api/rooms", {"displayName": "Invalid", "settings": invalid}, expected=400)
    physical = copy.deepcopy(settings)
    physical["composition"][1]["roleId"] = "little-girl"
    smoke.http(base, "POST", "/api/rooms", {"displayName": "Physical", "settings": physical}, expected=400)
    host = smoke.create(base, "Host", settings)
    clients = [host]
    for index in range(1, 6):
        client, _ = smoke.http(base, "POST", smoke.path(host, "join"), {"displayName": "Player " + str(index)})
        smoke.TOKENS.append(client["sessionToken"])
        clients.append(client)
    for client in clients:
        view = smoke.state(base, client)
        smoke.check(view["settings"]["nightProtocol"] == "screen", "The protocol is public to every member.")
        smoke.command(base, client, "ready", ready=True)
    rejected, _ = smoke.command(base, host, "configure", settings=physical, expected=409)
    smoke.check(rejected["code"] == "PHYSICAL_ROLE", "Screen cannot silently accept the physical Little Girl.")
    smoke.check(all(p["ready"] for p in smoke.state(base, host)["players"]), "Rejected configuration does not invalidate unchanged readiness.")
    smoke.command(base, host, "start")
    for client in clients:
        smoke.command(base, client, "acknowledge-role")
    first = smoke.state(base, host)
    smoke.check(first["phase"] == "night" and first["nightStep"] == "seer", "The first public call is the seer.")
    smoke.check("yeux fermés" not in first["statusMessage"], "Screen instructions must not tell passive members to close their eyes.")
    actor = next(client for client in clients if smoke.state(base, client)["action"] is not None)
    smoke.command(base, actor, "act", choice="pass")
    time.sleep(3.2)
    after = smoke.state(base, host)
    smoke.check(after["turn"] == first["turn"] and after["deadlineUnixMs"] == first["deadlineUnixMs"],
                "All submitted choices cannot shorten a screen call after three seconds.")
    smoke.check(not after["canAdvance"], "The host cannot prematurely close screen night.")
    rejected, _ = smoke.command(base, host, "advance", expected=409)
    smoke.check(rejected["code"] == "STEP_PENDING", "Premature explicit host advance is rejected.")
    restart()
    resumed, _ = smoke.http(base, "POST", smoke.path(host, "join"), {"resumeToken": host["sessionToken"]})
    smoke.check(resumed["playerId"] == host["playerId"], "Restart retains the same authenticated seat.")
    restored = smoke.state(base, actor)
    smoke.check(restored["settings"]["nightProtocol"] == "screen" and restored["turn"] == first["turn"],
                "Restart preserves screen protocol and current call.")
    smoke.check(restored["deadlineUnixMs"] == first["deadlineUnixMs"] and restored["action"]["submitted"],
                "Restart preserves both deadline and acknowledged private choice.")
    second = smoke.wait_view(base, host, lambda view: view["turn"] != first["turn"], timeout=10)
    smoke.check(second["phase"] == "night" and second["nightStep"] == "werewolf", "The authoritative deadline advances without host intervention.")
    smoke.check(second["phaseStartedUnixMs"] >= first["deadlineUnixMs"], "The next call cannot start before the prior deadline.")
    for client in clients:
        view = smoke.state(base, client)
        smoke.check(view["turn"] == second["turn"] and view["nightStep"] == "werewolf", "Every member sees the same public call.")
        smoke.check(all(p["revealedRoleId"] is None for p in view["players"]), "Public vignette data does not reveal the actors.")


def main():
    dotnet = smoke.UNITY_DOTNET
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0))
        base = "http://127.0.0.1:" + str(probe.getsockname()[1])
    with tempfile.TemporaryDirectory(prefix="ngame-night-protocol-") as temporary, tempfile.TemporaryFile(mode="w+") as log:
        env = {**os.environ, "ASPNETCORE_URLS": base, "NGAME_DATA_DIRECTORY": str(Path(temporary) / "private"),
               "NGAME_CONNECTED_GRACE_SECONDS": "30", "NGAME_ROOM_TTL_SECONDS": "600", "NGAME_WEB_ROOT": ""}
        process = None

        def start():
            nonlocal process
            process = subprocess.Popen([dotnet, str(HERE / "bin/Debug/net8.0/NGame.Server.dll")], env=env,
                                       cwd=HERE, stdout=log, stderr=subprocess.STDOUT)
            deadline = time.monotonic() + 10
            while True:
                try:
                    urllib.request.urlopen(base + "/health", timeout=.2).close()
                    return
                except OSError:
                    if process.poll() is not None or time.monotonic() >= deadline:
                        raise AssertionError("Isolated test server did not start.")
                    time.sleep(.05)

        def stop():
            if process is not None:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)

        def restart():
            stop()
            start()

        try:
            start()
            run(base, restart)
            log.seek(0)
            text = log.read()
            smoke.check(all(token not in text for token in smoke.TOKENS), "No raw capability was written to server logs.")
            print("PASS screen night HTTP protocol: " + str(smoke.CHECKS) + " checks (requests/polls included), isolated localhost, restart exercised.")
        finally:
            stop()


if __name__ == "__main__":
    main()
