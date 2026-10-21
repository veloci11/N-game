#!/usr/bin/env python3
"""Ten simulated HTTP clients: full screen-night match, role actions, privacy, recovery and rematch.

Starts only its own ephemeral loopback server and removes its temporary test data on exit.
This does not automate Unity, emulate phone input, or stand in for human play testing.
"""
import argparse
import base64
import concurrent.futures
import copy
import json
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import time
import urllib.request
import uuid

import smoke

HTTP_REQUESTS = 0
_original_http = smoke.http

def counted_http(*args, **kwargs):
    global HTTP_REQUESTS
    HTTP_REQUESTS += 1
    return _original_http(*args, **kwargs)

smoke.http = counted_http

HERE = Path(__file__).resolve().parent


class Match:
    def __init__(self, base, restart, night_seconds, mode):
        self.base, self.restart, self.night_seconds, self.mode = base, restart, night_seconds, mode
        self.clients = []
        self.roles = {}
        self.transitions = []
        self.actions = set()
        self.last_heartbeat = 0
        self.polls = 0
        self.restarted = False
        self.checked_seer = False
        self.role_action_commands = 0
        self.ballots_submitted = 0

    def state(self, client):
        self.polls += 1
        return smoke.state(self.base, client)

    def command(self, client, kind, **fields):
        return smoke.command(self.base, client, kind, **fields)

    def heartbeat(self, exclude=None):
        for client in self.clients:
            if exclude is not None and client["playerId"] == exclude["playerId"]:
                continue
            self.state(client)
        self.last_heartbeat = time.monotonic()

    def wait(self, predicate, timeout=15):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            view = self.state(self.host)
            if predicate(view):
                return view
            if time.monotonic() - self.last_heartbeat > .6:
                self.heartbeat()
            time.sleep(.1)
        raise AssertionError("Expected match transition was not reached; last phase=" + view["phase"])

    def capabilities(self, client):
        value, _ = smoke.http(self.base, "GET", smoke.path(client, "communications"), token=client["sessionToken"])
        return value

    def chat(self, client, channel, text, expected=200):
        view = self.state(client)
        value, _ = smoke.http(self.base, "POST", smoke.path(client, "messages"),
                              {"matchId": view["matchId"], "turn": view["turn"], "policyEpoch": view["turn"],
                               "clientMessageId": uuid.uuid4().hex, "channelId": channel, "text": text},
                              token=client["sessionToken"], expected=expected)
        return value

    def no_chat(self, client):
        cap = self.capabilities(client)
        smoke.check(cap["channels"] == [], "Text is closed outside the day vote.")
        smoke.check(not cap["voice"]["available"], "Voice is closed outside the day vote.")
        if self.mode == "remote":
            url = "voice?matchId=" + cap["matchId"] + "&turn=" + str(cap["turn"]) + "&after=0"
            rejected, _ = smoke.http(self.base, "GET", smoke.path(client, url), token=client["sessionToken"], expected=403)
            smoke.check(rejected["code"] == "voice_closed", "The remote audio endpoint itself is closed outside voting.")
        smoke.http(self.base, "GET", smoke.path(client, "messages/village"), token=client["sessionToken"], expected=403)
        self.chat(client, "village", "Not allowed outside vote", expected=403)

    def privacy(self):
        views = {client["playerId"]: self.state(client) for client in self.clients}
        first = views[self.host["playerId"]]
        for client in self.clients:
            identity = client["playerId"]
            view = views[identity]
            smoke.check(view["selfPlayerId"] == identity and view["selfRoleId"] == self.roles[identity],
                        "Every authenticated seat retains only its own assigned card.")
            smoke.check((view["turn"], view["phase"], view["nightStep"]) ==
                        (first["turn"], first["phase"], first["nightStep"]), "All clients share the public call and turn.")
            smoke.check(all(p["revealedRoleId"] is None for p in view["players"] if p["alive"]),
                        "Living assigned cards are not revealed in the public roster.")
            if self.roles[identity] != "werewolf":
                smoke.check(not view["wolfAllyIds"], "Only wolves receive the private wolf roster.")
            if self.roles[identity] != "seer":
                smoke.check(all(not fact["key"].startswith("seer-") for fact in view["privateFacts"]), "The seer's discoveries remain private.")
            if view["phase"] == "night":
                action = view["action"]
                step = view["nightStep"]
                expected = "wolves" if step == "werewolf" else step
                if action is not None:
                    if step == "lovers":
                        smoke.check(identity in self.lovers and action["kind"] == "observe", "Only the actual lovers receive their recognition.")
                    else:
                        smoke.check(self.roles[identity] == step and action["kind"] == expected,
                                    "Only the actor's authorized view exposes the current power.")
                if step != "witch" or self.roles[identity] != "witch":
                    smoke.check(action is None or not action.get("wolfVictimId"), "The wolf victim is not exposed in another role's action.")
        return first

    def setup(self):
        settings = {"mode": self.mode, "nightProtocol": "screen", "ruleset": "ngame-classic-v1",
                    "discussionSeconds": 60, "nightSeconds": self.night_seconds, "voteSeconds": 35,
                    "revealEliminatedRoles": True,
                    "composition": [{"roleId": role, "count": count} for role, count in
                                    [("werewolf", 2), ("seer", 1), ("witch", 1), ("hunter", 1),
                                     ("guard", 1), ("cupid", 1), ("villager", 3)]]}
        self.host = smoke.create(self.base, "HTTP Host", settings)
        self.clients = [self.host]
        def join(index):
            client, _ = smoke.http(self.base, "POST", smoke.path(self.host, "join"),
                                   {"displayName": "HTTP Player " + str(index), "avatarIndex": index % 6})
            smoke.TOKENS.append(client["sessionToken"])
            return client
        with concurrent.futures.ThreadPoolExecutor(max_workers=9) as pool:
            self.clients += list(pool.map(join, range(1, 10)))
        smoke.check(len({c["playerId"] for c in self.clients}) == 10, "Ten independently authenticated HTTP clients joined.")
        self.no_chat(self.host)
        for client in self.clients:
            self.command(client, "ready", ready=True)
        self.command(self.host, "start")
        views = [self.state(client) for client in self.clients]
        smoke.check(all(view["phase"] == "distribution" for view in views), "Every client reaches card discovery.")
        self.roles = {client["playerId"]: view["selfRoleId"] for client, view in zip(self.clients, views)}
        self.by_role = {role: [client for client in self.clients if self.roles[client["playerId"]] == role]
                        for role in set(self.roles.values())}
        self.wolves = self.by_role["werewolf"]
        self.seer, self.witch, self.guard, self.cupid, self.hunter = [self.by_role[r][0] for r in ["seer", "witch", "guard", "cupid", "hunter"]]
        self.villagers = self.by_role["villager"]
        self.lovers = {self.villagers[0]["playerId"], self.villagers[1]["playerId"]}
        self.target = self.villagers[2]
        self.privacy()
        # Real expiry of one non-host member while other devices keep polling.
        absent = next(client for client in self.clients if client["playerId"] != self.host["playerId"])
        old = self.state(absent)
        until = time.monotonic() + 2.5
        while time.monotonic() < until:
            self.heartbeat(exclude=absent)
            time.sleep(.2)
        connected = next(p for p in self.state(self.host)["players"] if p["id"] == absent["playerId"])["connected"]
        smoke.check(not connected, "A genuinely silent session becomes disconnected.")
        resumed, _ = smoke.http(self.base, "POST", smoke.path(absent, "join"), {"resumeToken": absent["sessionToken"]})
        smoke.check(resumed["playerId"] == absent["playerId"] and resumed["view"]["selfRoleId"] == old["selfRoleId"],
                    "Reconnection preserves the same assigned card and seat.")
        for client in self.clients:
            self.command(client, "acknowledge-role")
        self.wait(lambda view: view["phase"] == "night")
        print("PASS setup: ten sessions, readiness, private discovery, real disconnection and resume.", flush=True)

    def play_night(self, day):
        while True:
            before = self.state(self.host)
            if before["phase"] != "night":
                smoke.check(before["phase"] == "dawn", "The completed night resolves to dawn.")
                self.transitions.append({"day": day, "phase": "dawn"})
                self.no_chat(self.host)
                return
            smoke.check(before["day"] == day, "The night number follows the completed day.")
            self.privacy()
            step, turn, deadline = before["nightStep"], before["turn"], before["deadlineUnixMs"]
            if step == "lovers":
                for client in self.clients:
                    if client["playerId"] not in self.lovers: continue
                    recognition = self.state(client)["action"]
                    smoke.check(recognition is not None and "yeux" not in recognition["description"].lower(),
                                "Screen-night lovers are told to hide their private view, not to close their eyes.")
            self.transitions.append({"day": day, "phase": "night", "step": step})
            issued = []
            for client in self.clients:
                view = self.state(client)
                action = view["action"]
                if not action or action["kind"] == "observe" or action["submitted"]:
                    continue
                kind = action["kind"]
                choice, targets = "pass", []
                if kind == "cupid": choice, targets = "link", sorted(self.lovers)
                elif kind == "guard":
                    protected = self.seer if day == 1 else self.cupid
                    choice, targets = "protect", [protected["playerId"]]
                    if day == 2:
                        smoke.check(self.seer["playerId"] not in action["allowedTargetIds"], "The guard cannot protect the same target twice in a row.")
                elif kind == "seer": choice, targets = "inspect", [self.wolves[0 if day == 1 else 1]["playerId"]]
                elif kind == "wolves" and day == 1: choice, targets = "kill", [self.target["playerId"]]
                elif kind == "witch":
                    if day == 1:
                        smoke.check(action["wolfVictimId"] == self.target["playerId"] and action["healingAvailable"], "The witch receives the actual authorized victim and unused healing.")
                        choice = "save"
                    else:
                        smoke.check(not action["healingAvailable"] and action["poisonAvailable"] and "save" not in action["choices"],
                                    "Spent healing stays spent while poison remains available.")
                        choice, targets = "poison", [self.target["playerId"]]
                result, payload = self.command(client, "act", turn=turn, choice=choice, targetIds=targets)
                self.actions.add(kind)
                self.role_action_commands += 1
                smoke.check(result["view"]["action"]["submitted"], "The authoritative actor view acknowledges the power.")
                issued.append((client, payload))
            held = self.state(self.host)
            smoke.check(held["turn"] == turn and held["deadlineUnixMs"] == deadline, "Fast submissions cannot shorten a screen-night call.")
            if deadline - held["serverTimeUnixMs"] > 500:
                result, _ = self.command(self.host, "advance", turn=turn, expected=409)
                smoke.check(result["code"] == "STEP_PENDING", "The host cannot close screen night early.")
            if day == 1 and step == "cupid" and not self.restarted:
                self.restart()
                self.restarted = True
                for client in self.clients:
                    restored = self.state(client)
                    smoke.check(restored["turn"] == turn and restored["deadlineUnixMs"] == deadline
                                and restored["settings"]["nightProtocol"] == "screen", "Process restart retains the same public call and deadline.")
                actor, payload = issued[0]
                again, _ = smoke.http(self.base, "POST", smoke.path(actor, "commands"), payload, token=actor["sessionToken"])
                smoke.check(again["ok"] and again["view"]["action"]["submitted"], "An accepted power replays idempotently after process restart.")
            next_view = self.wait(lambda view: view["turn"] != turn, timeout=self.night_seconds + 4)
            smoke.check(next_view["phaseStartedUnixMs"] >= deadline, "No next call can start before the authoritative deadline.")
            if step == "seer":
                seer = self.state(self.seer)
                smoke.check(any(f["key"] == "seer-" + str(day) and f["roleId"] == "werewolf" for f in seer["privateFacts"]), "The seer receives the actual consulted card only after the power resolves.")
                self.checked_seer = True
            print("PASS night " + str(day) + ": " + step + ", fixed deadline and recipient rights.", flush=True)

    def advance_public(self, phase):
        view = self.wait(lambda value: value["phase"] != phase or value["canAdvance"], timeout=8)
        if view["phase"] == phase:
            self.command(self.host, "advance", turn=view["turn"])
        return self.state(self.host)

    def vote(self, target, second=False):
        view = self.advance_public("dawn")
        smoke.check(view["phase"] == "discussion", "Dawn leads to public discussion.")
        self.no_chat(self.host)
        self.command(self.host, "advance")
        view = self.state(self.host)
        smoke.check(view["phase"] == "vote", "The host opens the day vote explicitly.")
        self.transitions.append({"day": view["day"], "phase": "vote"})
        alive = {p["id"] for p in view["players"] if p["alive"]}
        living = [c for c in self.clients if c["playerId"] in alive]
        dead = [c for c in self.clients if c["playerId"] not in alive]
        speaker = living[0]
        cap = self.capabilities(speaker)
        smoke.check(any(c["id"] == "village" and c["canRead"] and c["canSend"] for c in cap["channels"]), "Living participants may write in the Village during the vote.")
        smoke.check(cap["voice"]["available"] == (self.mode == "remote"), "Integrated voice is offered only for a remote day vote.")
        if self.mode == "remote":
            smoke.check(cap["voice"]["canListen"] and cap["voice"]["canTransmit"] and not cap["voice"]["microphoneActive"],
                        "A living remote voter has audio permission, but the client microphone starts off.")
            packet = {"matchId": view["matchId"], "turn": view["turn"], "policyEpoch": view["turn"],
                      "chunkId": uuid.uuid4().hex, "pcmBase64": base64.b64encode(bytes([1, 0]) * 3200).decode()}
            smoke.http(self.base, "POST", smoke.path(speaker, "voice"), packet, token=speaker["sessionToken"])
            listener = living[1]
            url = "voice?matchId=" + view["matchId"] + "&turn=" + str(view["turn"]) + "&after=0"
            audio, _ = smoke.http(self.base, "GET", smoke.path(listener, url), token=listener["sessionToken"])
            smoke.check(any(p["pcmBase64"] == packet["pcmBase64"] for p in audio["packets"]),
                        "A second authenticated client receives the scripted PCM packet during the remote day vote.")
        sent = self.chat(speaker, "village", "VOTE_SCOPE_MARKER")
        another = living[1]
        history, _ = smoke.http(self.base, "GET", smoke.path(another, "messages/village"), token=another["sessionToken"])
        smoke.check(any(m["text"] == "VOTE_SCOPE_MARKER" for m in history["messages"]), "A second authenticated client receives the written message.")
        if second:
            smoke.check(len(dead) == 3, "Hunter, first wolf and poisoned villager are now observers.")
            observer = dead[0]
            cap = self.capabilities(observer)
            smoke.check(any(c["id"] == "village" and c["canRead"] and not c["canSend"] for c in cap["channels"]), "Eliminated players can follow Village but cannot speak to living players.")
            if self.mode == "remote":
                smoke.check(cap["voice"]["canListen"] and not cap["voice"]["canTransmit"], "Eliminated remote voters may listen, never transmit.")
                blocked_packet = copy.deepcopy(packet); blocked_packet["chunkId"] = uuid.uuid4().hex
                rejected, _ = smoke.http(self.base, "POST", smoke.path(observer, "voice"), blocked_packet,
                                         token=observer["sessionToken"], expected=403)
                smoke.check(rejected["code"] == "voice_closed", "The server rejects actual audio upload by an eliminated voter.")
            self.chat(observer, "village", "DEAD_MUST_NOT_SEND", expected=403)
            self.chat(observer, "observateurs", "PRIVATE_OBSERVER_MARKER")
            smoke.http(self.base, "GET", smoke.path(speaker, "messages/observateurs"), token=speaker["sessionToken"], expected=403)
            other_dead = dead[1]
            page, _ = smoke.http(self.base, "GET", smoke.path(other_dead, "messages/observateurs"), token=other_dead["sessionToken"])
            smoke.check(any(m["text"] == "PRIVATE_OBSERVER_MARKER" for m in page["messages"]), "Observer conversation is shared only among the eliminated.")
            self.command(observer, "vote", expected=409, choice="pass", targetIds=[])
        vote_opened_at = time.monotonic()
        for index, client in enumerate(living):
            before = self.state(client)
            smoke.check(before["verdict"] is None, "Ballots are not disclosed before closure.")
            own = client["playerId"] == target["playerId"]
            self.command(client, "vote", turn=view["turn"], choice="pass" if own else "vote",
                         targetIds=[] if own else [target["playerId"]])
            self.ballots_submitted += 1
            if index == 0:
                hidden = self.state(another)
                smoke.check(hidden["verdict"] is None and hidden["selfVoteTargetId"] is None,
                            "One client's choice does not become another client's private vote.")
        after = self.state(self.host)
        if self.mode == "remote" and after["phase"] == "vote":
            after = self.wait(lambda value: value["phase"] != "vote" or value["canAdvance"], timeout=40)
            if after["phase"] == "vote": self.command(self.host, "advance", turn=after["turn"])
            smoke.check(time.monotonic() - vote_opened_at >= 29, "Remote speech remains open for its minimum thirty-second window.")
        verdict = self.wait(lambda value: value["phase"] == "verdict", timeout=5)
        smoke.check(verdict["verdict"]["eliminatedPlayerId"] == target["playerId"], "The public verdict matches the accepted ballots.")
        smoke.check(len(verdict["verdict"]["votes"]) == len(living), "The verdict publishes each accepted ballot exactly once.")
        self.no_chat(self.host)
        return self.advance_public("verdict")

    def finish(self):
        self.setup()
        self.play_night(1)
        smoke.check(all(p["alive"] for p in self.state(self.host)["players"]), "Healing prevented the first wolf attack; no fake death was published.")
        view = self.vote(self.hunter)
        smoke.check(view["phase"] == "reaction", "The hunter's death opens the hunter reaction before the next night.")
        hunter = self.state(self.hunter)
        smoke.check(hunter["action"]["kind"] == "hunter", "The eliminated hunter retains the authorized last shot.")
        self.command(self.hunter, "act", turn=hunter["turn"], choice="shoot", targetIds=[self.wolves[0]["playerId"]])
        self.actions.add("hunter")
        self.role_action_commands += 1
        self.wait(lambda value: value["phase"] == "night")
        self.play_night(2)
        alive = {p["id"] for p in self.state(self.host)["players"] if p["alive"]}
        smoke.check(self.target["playerId"] not in alive, "The witch's poison resolves at dawn.")
        self.vote(self.wolves[1], second=True)
        final = self.wait(lambda value: value["phase"] == "finished")
        smoke.check(final["winner"] == "village", "The village wins after the final wolf is eliminated.")
        smoke.check(all(p["revealedRoleId"] == self.roles[p["id"]] for p in final["players"]), "Finished match reveals the real cards for all participants.")
        smoke.check(set(final["winnerIds"]) == {identity for identity, role in self.roles.items() if role != "werewolf"}, "The winning identities match the actual non-wolf team.")
        smoke.check(self.actions == {"cupid", "guard", "seer", "wolves", "witch", "hunter"}, "Every compatible active base power was exercised.")
        self.no_chat(self.host)
        old_match, old_turn = final["matchId"], final["turn"]
        self.command(self.host, "reset", turn=old_turn)
        lobby = self.state(self.host)
        smoke.check(lobby["phase"] == "lobby" and len(lobby["players"]) == 10, "Rematch retains all ten authenticated seats.")
        smoke.check(lobby["matchId"] != old_match and lobby["turn"] > old_turn, "Rematch receives a new identity and monotone turn.")
        for client in self.clients:
            view = self.state(client)
            smoke.check(view["selfRoleId"] is None and view["privateFacts"] == [] and not view["roleAcknowledged"], "Rematch erases old cards, discoveries and acknowledgements.")
            smoke.check(view["settings"]["nightProtocol"] == "screen", "Chosen screen protocol survives rematch.")
            self.command(client, "ready", ready=True)
        self.command(self.host, "start")
        new = self.state(self.host)
        smoke.check(new["phase"] == "distribution" and new["matchId"] != old_match, "A real second match starts with a fresh discovery phase.")
        self.no_chat(self.host)
        # This is a source-level boundary check, not a claim of clicking Unity's minigame.
        component = (HERE.parents[1] / "Assets/NGame/Presentation/NightFireflyActivity.cs").read_text()
        smoke.check("IRoomClient" not in component and ".Send(" not in component and "UnityWebRequest" not in component,
                    "Common firefly input has no room command or HTTP dependency; Unity interaction is tested separately.")
        print("PASS complete match: two nights, all compatible powers, two day votes, private chat, victory and fresh rematch.", flush=True)
        return {"mode": self.mode, "simulatedHttpClients": 10, "humanPlayers": 0, "nightProtocol": "screen",
                "nightSeconds": self.night_seconds, "activePowerKindsExercised": sorted(self.actions),
                "completedNights": 2, "completedDayVotes": 2, "acceptedRoleActionCommands": self.role_action_commands,
                "acceptedBallots": self.ballots_submitted,
                "observedTransitions": self.transitions, "stateReads": self.polls, "httpRequests": HTTP_REQUESTS,
                "transportChecksIncludingStatusCacheAndPolls": smoke.CHECKS,
                "serverRestartExercised": self.restarted, "scriptedPcmPacketsSent": 2 if self.mode == "remote" else 0, "finishedWinner": final["winner"], "secondMatchStarted": True,
                "scope": "Real HTTP sessions with scripted cooperative choices, not ten humans, Unity UI, phone gestures or audio-device validation."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--no-build", action="store_true")
    parser.add_argument("--mode", choices=["in-person", "remote"], default="in-person")
    parser.add_argument("--night-seconds", type=int, default=6)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    if not 3 <= args.night_seconds <= 120: raise SystemExit("night-seconds must be 3..120")
    dotnet = smoke.UNITY_DOTNET
    if not args.no_build:
        subprocess.run([dotnet, "build", str(HERE / "NGame.Server.csproj"), "--nologo", "-v", "quiet"], check=True)
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0)); base = "http://127.0.0.1:" + str(probe.getsockname()[1])
    with tempfile.TemporaryDirectory(prefix="ngame-full-match-") as temporary, tempfile.TemporaryFile(mode="w+") as log:
        env = {**os.environ, "ASPNETCORE_URLS": base, "NGAME_DATA_DIRECTORY": str(Path(temporary) / "private"),
               "NGAME_CONNECTED_GRACE_SECONDS": "2", "NGAME_ROOM_TTL_SECONDS": "600", "NGAME_WEB_ROOT": ""}
        process = None
        def start():
            nonlocal process
            process = subprocess.Popen([dotnet, str(HERE / "bin/Debug/net8.0/NGame.Server.dll")], cwd=HERE,
                                       env=env, stdout=log, stderr=subprocess.STDOUT)
            deadline = time.monotonic() + 10
            while True:
                try: urllib.request.urlopen(base + "/health", timeout=.2).close(); return
                except OSError:
                    if process.poll() is not None or time.monotonic() >= deadline: raise AssertionError("Isolated server did not start.")
                    time.sleep(.05)
        def stop():
            if process is not None:
                process.terminate()
                try: process.wait(timeout=5)
                except subprocess.TimeoutExpired: process.kill(); process.wait(timeout=5)
        def restart(): stop(); start()
        try:
            start()
            started = time.monotonic()
            report = Match(base, restart, args.night_seconds, args.mode).finish()
            report["elapsedSeconds"] = round(time.monotonic() - started, 2)
            log.seek(0); output = log.read()
            smoke.check(not any(token in output for token in smoke.TOKENS), "No raw session capability appears in server logs.")
            for path in (Path(temporary) / "private").rglob("*.json"):
                smoke.check(not any(token in path.read_text() for token in smoke.TOKENS), "Private snapshots store hashes, not raw capabilities.")
            report["transportChecksIncludingStatusCacheAndPolls"] = smoke.CHECKS
            report["httpRequests"] = HTTP_REQUESTS
            print(json.dumps(report, indent=2))
            if args.report: args.report.write_text(json.dumps(report, indent=2) + "\n")
        finally:
            stop()


if __name__ == "__main__": main()
