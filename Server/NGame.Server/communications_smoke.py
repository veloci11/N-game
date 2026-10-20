#!/usr/bin/env python3
"""Authenticated HTTP checks for vote-only text permissions, closure, idempotency and durable room restart."""
import argparse
import base64
import concurrent.futures
import json
import math
import os
from pathlib import Path
import shutil
import socket
import struct
import subprocess
import tempfile
import time
import urllib.request
import uuid
import smoke as api

HERE = Path(__file__).resolve().parent


def comm(base, client):
    return api.http(base, 'GET', api.path(client, 'communications'), token=client['sessionToken'])[0]


def send_text(base, client, channel, text, expected=200, request=None):
    caps = comm(base, client)
    request = request or {'channelId': channel, 'text': text, 'matchId': caps['matchId'],
                          'turn': caps['turn'], 'policyEpoch': caps['policyEpoch'], 'clientMessageId': uuid.uuid4().hex}
    return api.http(base, 'POST', api.path(client, 'messages'), request, token=client['sessionToken'], expected=expected)[0], request


def messages(base, client, channel, expected=200):
    return api.http(base, 'GET', api.path(client, 'messages/' + channel), token=client['sessionToken'], expected=expected)[0]


def voice_request(caps):
    pcm = b''.join(struct.pack('<h', int(6000 * math.sin(2 * math.pi * 440 * i / 16000))) for i in range(3200))
    return {'matchId': caps['matchId'], 'turn': caps['turn'], 'policyEpoch': caps['policyEpoch'],
            'chunkId': uuid.uuid4().hex, 'pcmBase64': base64.b64encode(pcm).decode()}


def send_voice(base, client, request, expected=200):
    return api.http(base, 'POST', api.path(client, 'voice'), request, token=client['sessionToken'], expected=expected)[0]


def poll_voice(base, client, caps, expected=200, after=0):
    query = '?matchId=' + caps['matchId'] + '&turn=' + str(caps['turn']) + '&after=' + str(after)
    return api.http(base, 'GET', api.path(client, 'voice') + query, token=client['sessionToken'], expected=expected)[0]


class Server:
    def __init__(self, dotnet, temporary):
        self.dotnet = dotnet
        self.data = Path(temporary) / 'private-state'
        self.log = tempfile.TemporaryFile(mode='w+')
        with socket.socket() as probe:
            probe.bind(('127.0.0.1', 0))
            self.base = 'http://127.0.0.1:' + str(probe.getsockname()[1])
        self.env = {**os.environ, 'ASPNETCORE_URLS': self.base, 'NGAME_DATA_DIRECTORY': str(self.data),
                    'NGAME_ROOM_TTL_SECONDS': '120', 'NGAME_CONNECTED_GRACE_SECONDS': '15',
                    'DOTNET_CLI_TELEMETRY_OPTOUT': '1', 'NGAME_WEB_ROOT': '', 'NGAME_ALLOWED_ORIGINS': ''}
        self.process = None

    def start(self):
        self.process = subprocess.Popen([self.dotnet, str(HERE / 'bin/Debug/net8.0/NGame.Server.dll')],
                                        env=self.env, cwd=HERE, stdout=self.log, stderr=subprocess.STDOUT)
        end = time.monotonic() + 10
        while True:
            try:
                urllib.request.urlopen(self.base + '/health', timeout=.2).close()
                return
            except OSError:
                if self.process.poll() is not None or time.monotonic() > end:
                    self.log.seek(0)
                    raise AssertionError('Serveur de recette indisponible : ' + self.log.read()[-800:])
                time.sleep(.1)

    def stop(self):
        if self.process is not None:
            self.process.terminate()
            try:
                self.process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait(timeout=5)
            self.process = None

    def restart(self):
        self.stop()
        self.start()


def run(server):
    """Current contract: text is visible and writable only during vote, split by alive/dead."""
    base = server.base
    settings = {'mode': 'remote', 'discussionSeconds': 60, 'nightSeconds': 12, 'voteSeconds': 30,
                'composition': [{'roleId': 'werewolf', 'count': 1}, {'roleId': 'villager', 'count': 5}]}
    host = api.create(base, 'Hôte vote uniquement', settings)

    def closed(client, label):
        caps = comm(base, client)
        api.check(caps['channels'] == [], label + ' : aucun canal visible.')
        for channel in ('village', 'observateurs', 'meute'):
            messages(base, client, channel, expected=403)
            send_text(base, client, channel, 'Intention hors fenêtre', expected=403)

    closed(host, 'Salon')
    api.http(base, 'GET', api.path(host, 'communications'), expected=401)
    clients = [host]
    for i in range(1, 6):
        client, _ = api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Vote ' + str(i), 'avatarIndex': i})
        api.TOKENS.append(client['sessionToken'])
        clients.append(client)
    for client in clients:
        api.command(base, client, 'ready', ready=True)
    api.command(base, host, 'start')
    closed(host, 'Distribution')
    for client in clients:
        api.command(base, client, 'acknowledge-role')
    views = [api.state(base, c) for c in clients]
    wolf = next(c for c, v in zip(clients, views) if v['selfRoleId'] == 'werewolf')
    victim = next(c for c, v in zip(clients, views) if c != host and v['selfRoleId'] == 'villager')
    api.wait_view(base, wolf, lambda v: (v.get('action') or {}).get('kind') == 'wolves')
    closed(wolf, 'Appel nocturne du loup : pas de chat Meute')
    api.command(base, wolf, 'act', choice='kill', targetIds=[victim['playerId']])
    api.wait_view(base, host, lambda v: v['phase'] == 'dawn', timeout=12)
    closed(host, 'Aube')
    api.wait_view(base, host, lambda v: v['phase'] == 'discussion', timeout=8)
    closed(host, 'Discussion avant scrutin')
    api.command(base, host, 'advance')
    alive = [c for c in clients if c != victim]
    caps = comm(base, host)
    api.check([c['id'] for c in caps['channels']] == ['village'], 'Vivant : Village seulement au vote.')
    dead_caps = comm(base, victim)
    api.check([c['id'] for c in dead_caps['channels']] == ['village', 'observateurs'], 'Éliminé : Village en lecture et Observateurs au vote.')
    api.check(dead_caps['channels'][0]['canRead'] and not dead_caps['channels'][0]['canSend'], 'Éliminé : Village strictement en lecture seule.')
    messages(base, victim, 'village')
    send_text(base, victim, 'village', 'Vers les vivants', expected=403)
    messages(base, host, 'observateurs', expected=403)
    send_text(base, host, 'observateurs', 'Vers les morts', expected=403)
    messages(base, wolf, 'meute', expected=403)
    send_text(base, wolf, 'meute', 'Pas de canal parallèle', expected=403)
    public, request = send_text(base, host, 'village', '<b>Texte brut</b> 👋')
    again, _ = send_text(base, host, 'village', request['text'], request=request)
    api.check(again['sent']['id'] == public['sent']['id'], 'Un message est conservé une fois, même après retry.')
    reader = next(c for c in alive if c != host)
    api.check(messages(base, reader, 'village')['messages'][0]['text'] == request['text'], 'Autre vivant : contenu exact reçu.')
    api.check(messages(base, victim, 'village')['messages'][0]['text'] == request['text'], 'Éliminé : échanges des vivants lisibles pendant le vote.')
    observed, _ = send_text(base, victim, 'observateurs', 'Commentaire des éliminés')
    api.check(len(messages(base, victim, 'observateurs')['messages']) == 1, 'Message visible dans Observateurs.')
    api.check(all(m['id'] != observed['sent']['id'] for m in messages(base, host, 'village')['messages']), 'Aucune migration Observateurs vers Village.')
    send_text(base, host, 'village', 'Deux')
    send_text(base, host, 'village', 'Trois')
    send_text(base, host, 'village', 'Trop vite', expected=429)
    changed = {**request, 'text': 'Contenu remplacé'}
    send_text(base, host, 'village', changed['text'], expected=409, request=changed)
    forged = {**request, 'clientMessageId': uuid.uuid4().hex, 'authorPlayerId': 'intrus'}
    api.http(base, 'POST', api.path(host, 'messages'), forged, token=host['sessionToken'], expected=400)
    server.restart()
    resumed, _ = api.http(base, 'POST', api.path(host, 'join'), {'resumeToken': host['sessionToken']})
    api.check(resumed['playerId'] == host['playerId'], 'Même joueur retrouvé après redémarrage.')
    api.check(len(messages(base, host, 'village')['messages']) == 3, 'Historique autorisé du scrutin restauré.')
    retried, _ = send_text(base, host, 'village', request['text'], request=request)
    api.check(retried['sent']['id'] == public['sent']['id'], 'Déduplication conservée après redémarrage.')
    # Force a public tie: two votes on each candidate and one abstention.
    first, second = alive[0]['playerId'], alive[1]['playerId']
    for index, client in enumerate(alive):
        api.command(base, client, 'vote', choice='pass' if index == 4 else 'vote',
                    targetIds=[] if index == 4 else [first if index in (1, 2) else second])
    verdict = api.wait_view(base, host, lambda v: v['phase'] == 'verdict', timeout=35)
    api.check(verdict['verdict']['result'] == 'runoff', 'Le scrutin se ferme sur une égalité réelle.')
    closed(host, 'Verdict après clôture')
    closed(victim, 'Verdict éliminé : Observateurs fermé aussi')
    receipt, _ = send_text(base, host, 'village', request['text'], request=request)
    api.check(receipt['ok'] and receipt.get('sent') is None and receipt['communications']['channels'] == [],
              'Ancien reçu acquitté hors vote sans corps du message ni canal rouvert.')
    unseen = {**request, 'clientMessageId': uuid.uuid4().hex}
    send_text(base, host, 'village', unseen['text'], expected=409, request=unseen)
    api.wait_view(base, host, lambda v: v['phase'] == 'vote' and v['voteRound'] == 2, timeout=8)
    api.check(len(messages(base, host, 'village')['messages']) == 3, 'Historique relisible au prochain scrutin, sans duplication.')
    receipt, _ = send_text(base, host, 'village', request['text'], request=request)
    api.check(receipt['sent']['id'] == public['sent']['id'], 'Ancien reçu lisible au scrutin suivant, pas de nouvel envoi.')
    send_text(base, host, 'village', unseen['text'], expected=409, request=unseen)
    send_text(base, host, 'village', request['text'], expected=409, request={**request, 'matchId': 'autre-manche'})
    api.check(len(messages(base, host, 'village')['messages']) == 3, 'Intentions périmées refusées sans ajouter de texte.')


def run_stale_message_checks(server):
    # The focused vote-only journey includes both the closed-window receipt and next-vote retry.
    run(server)


def run_capacity_replacement(server):
    base = server.base
    host = api.create(base, 'Salon vingt-quatre')
    clients = [host]
    for i in range(1, 24):
        client, _ = api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Siège ' + str(i), 'avatarIndex': i % 6})
        api.TOKENS.append(client['sessionToken'])
        clients.append(client)
    api.check(len(api.state(base, host)['players']) == 24, 'Salon plein réel de vingt-quatre membres.')
    leaving = clients[1]
    api.command(base, leaving, 'leave')
    api.check(len(api.state(base, host)['players']) == 23, 'Le départ volontaire libère un siège du salon.')
    replacement, _ = api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Remplaçant', 'avatarIndex': 2})
    api.TOKENS.append(replacement['sessionToken'])
    current = api.state(base, replacement)
    api.check(len(current['players']) == 24 and all(p['id'] != leaving['playerId'] for p in current['players']),
              'La capacité se fonde sur les sièges actuels : le remplaçant rejoint sans place fantôme.')
    api.http(base, 'GET', api.path(leaving, 'state'), token=leaving['sessionToken'], expected=401)
    api.http(base, 'POST', api.path(leaving, 'join'), {'resumeToken': leaving['sessionToken']}, expected=401)
    api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Une place en trop', 'avatarIndex': 2}, expected=409)



def run_moderation_checks(server):
    base = server.base
    host = api.create(base, 'Hôte modération')
    clients = [host]
    for i in range(1, 24):
        client, _ = api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Invité ' + str(i), 'avatarIndex': i % 6})
        api.TOKENS.append(client['sessionToken'])
        clients.append(client)
    blocked = api.state(base, host)
    api.check(bool(blocked.get('startBlockedReason')), 'Le salon expose une raison réelle quand le lancement est indisponible.')
    for client in clients:
        api.command(base, client, 'ready', ready=True)
    api.check(api.state(base, host)['canStart'], 'Les vingt-quatre membres sont prêts avant modération.')
    guest_refused, _ = api.command(base, clients[1], 'remove-player', expected=409, targetIds=[clients[2]['playerId']])
    api.check(guest_refused['code'] == 'HOST_ONLY', 'Un invité ne retire pas une autre personne.')
    api.command(base, host, 'remove-player', expected=409, targetIds=[host['playerId']])
    expelled = clients[1]
    removed, payload = api.command(base, host, 'remove-player', targetIds=[expelled['playerId']])
    api.check(len(removed['view']['players']) == 23 and all(not p['ready'] for p in removed['view']['players']),
              'Le retrait libère le siège et demande aux autres de confirmer à nouveau.')
    repeated, _ = api.http(base, 'POST', api.path(host, 'commands'), payload, token=host['sessionToken'])
    api.check(repeated['ok'] and len(repeated['view']['players']) == 23, 'Le retrait rejoué ne touche pas un autre siège.')
    api.http(base, 'GET', api.path(expelled, 'state'), token=expelled['sessionToken'], expected=401)
    api.http(base, 'POST', api.path(expelled, 'join'), {'resumeToken': expelled['sessionToken']}, expected=401)
    api.http(base, 'GET', api.path(expelled, 'communications'), token=expelled['sessionToken'], expected=401)
    api.http(base, 'POST', api.path(expelled, 'commands'), {'kind': 'ready', 'ready': True, 'turn': 0,
             'commandId': uuid.uuid4().hex}, token=expelled['sessionToken'], expected=401)
    replacement, _ = api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Invité remplacement', 'avatarIndex': 1})
    api.TOKENS.append(replacement['sessionToken'])
    api.check(len(replacement['view']['players']) == 24, 'Le siège retiré accepte immédiatement un remplaçant.')
    server.restart()
    api.http(base, 'GET', api.path(expelled, 'state'), token=expelled['sessionToken'], expected=401)
    api.check(len(api.state(base, host)['players']) == 24, 'Retrait et nouvelle composition conservés au redémarrage.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--no-build', action='store_true')
    parser.add_argument('--stale-only', action='store_true', help='Parcours ciblé vote uniquement : reprise de reçu après clôture et scrutin suivant.')
    parser.add_argument('--capacity-only', action='store_true', help='Cas ciblé : salon de 24, départ puis remplacement.')
    parser.add_argument('--moderation-only', action='store_true', help='Cas ciblé : retrait hôte en salon et invalidation de session.')
    args = parser.parse_args()
    dotnet = os.environ.get('DOTNET') or (api.UNITY_DOTNET if Path(api.UNITY_DOTNET).is_file() else shutil.which('dotnet'))
    if not dotnet:
        raise SystemExit('SDK .NET 8 introuvable.')
    if not args.no_build:
        subprocess.run([dotnet, 'build', str(HERE / 'NGame.Server.csproj'), '--nologo', '-v', 'quiet'], check=True)
    with tempfile.TemporaryDirectory(prefix='ngame-communications-') as temporary:
        server = Server(dotnet, temporary)
        try:
            server.start()
            if args.moderation_only:
                run_moderation_checks(server)
            elif args.capacity_only:
                run_capacity_replacement(server)
            elif args.stale_only:
                run_stale_message_checks(server)
            else:
                run(server)
        finally:
            server.stop()
            server.log.seek(0)
            log = server.log.read()
            api.check(not any(token in log for token in api.TOKENS), 'Aucun token dans les journaux de transport.')
            server.log.close()
    if args.moderation_only:
        print(f'OK — {api.CHECKS} vérifications HTTP ciblées : retrait hôte, refus invité, révocation et remplacement durables.')
    elif args.capacity_only:
        print(f'OK — {api.CHECKS} vérifications HTTP ciblées : salon plein, départ et remplacement sans siège fantôme.')
    elif args.stale_only:
        print(f'OK — {api.CHECKS} vérifications HTTP : chat pendant le vote seulement, reçus tardifs sans fuite et intentions périmées refusées.')
    else:
        print(f'OK — {api.CHECKS} vérifications HTTP : chat vote seulement, canaux vivants/éliminés, clôture, déduplication et reprise durable. UI et audio sur appareil non testés.')


if __name__ == '__main__':
    main()
