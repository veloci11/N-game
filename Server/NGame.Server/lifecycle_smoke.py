#!/usr/bin/env python3
"""Isolated HTTP lifecycle/voice regression: no Unity Editor, microphone or public deployment."""
import base64
import json
import os
from pathlib import Path
import tempfile
import time
import communications_smoke as communication
import smoke as api


def run(server):
    base = server.base
    settings = {'mode': 'remote', 'discussionSeconds': 60, 'nightSeconds': 12, 'voteSeconds': 30,
                'composition': [{'roleId': 'werewolf', 'count': 1}, {'roleId': 'villager', 'count': 5}]}
    original = api.create(base, 'Hôte initial', settings)
    clients = [original]
    for index in range(1, 6):
        client, _ = api.http(base, 'POST', api.path(original, 'join'),
                             {'displayName': 'Connexion ' + str(index), 'avatarIndex': index})
        api.TOKENS.append(client['sessionToken'])
        clients.append(client)
    for client in clients:
        api.command(base, client, 'ready', ready=True)
    # Keep all guests genuinely online while the original host stops making requests.
    until = time.monotonic() + 2.2
    while time.monotonic() < until:
        for client in clients[1:]:
            api.state(base, client)
        time.sleep(.15)
    guest_view = api.state(base, clients[1])
    api.check(guest_view['hostPlayerId'] == clients[1]['playerId'], 'Un hôte absent transmet l’organisation au premier invité connecté.')
    api.check(not guest_view['canStart'] and bool(guest_view['startBlockedReason']), 'Une place hors ligne bloque le lancement avec une raison.')
    refused, _ = api.command(base, clients[1], 'start', expected=409)
    api.check(refused['code'] == 'PLAYER_OFFLINE', 'Le refus réel correspond au motif de connexion.')
    resumed, _ = api.http(base, 'POST', api.path(original, 'join'), {'resumeToken': original['sessionToken']})
    api.check(resumed['playerId'] == original['playerId'] and len(resumed['view']['players']) == 6,
              'La reprise retrouve le même siège sans personne fantôme.')
    api.check(resumed['view']['hostPlayerId'] == clients[1]['playerId'], 'Le retour de l’ancien hôte ne vole pas la succession.')
    for client in clients:
        api.state(base, client)
    host = clients[1]
    api.command(base, host, 'start')
    initial_roles = {c['playerId']: api.state(base, c)['selfRoleId'] for c in clients}
    for client in clients:
        resumed, _ = api.http(base, 'POST', api.path(client, 'join'), {'resumeToken': client['sessionToken']})
        api.check(resumed['view']['selfRoleId'] == initial_roles[client['playerId']], 'Reprise pendant découverte : même carte.')
    api.http(base, 'POST', api.path(host, 'join'), {'displayName': 'Retardataire', 'avatarIndex': 0}, expected=409)
    for client in clients:
        api.command(base, client, 'acknowledge-role')
    wolf = next(c for c in clients if initial_roles[c['playerId']] == 'werewolf')
    victim = next(c for c in clients if c != host and initial_roles[c['playerId']] != 'werewolf')
    api.wait_view(base, wolf, lambda v: (v.get('action') or {}).get('kind') == 'wolves')
    night_caps = communication.comm(base, wolf)
    communication.send_voice(base, wolf, communication.voice_request(night_caps), expected=403)
    communication.poll_voice(base, wolf, night_caps, expected=403)
    resumed, _ = api.http(base, 'POST', api.path(wolf, 'join'), {'resumeToken': wolf['sessionToken']})
    api.check(resumed['view']['action']['kind'] == 'wolves' and resumed['view']['selfRoleId'] == 'werewolf',
              'Reprise pendant pouvoir : même rôle et choix privé disponibles.')
    accepted, request = api.command(base, wolf, 'act', choice='kill', targetIds=[victim['playerId']])
    replay, _ = api.http(base, 'POST', api.path(wolf, 'commands'), request, token=wolf['sessionToken'])
    api.check(replay['ok'], 'Un pouvoir dont la réponse a été perdue reste idempotent.')
    api.wait_view(base, host, lambda v: v['phase'] == 'discussion', timeout=15)
    # Restore all connections before selecting the current organizer after the night.
    for client in clients:
        api.state(base, client)
    current = api.state(base, host)
    host = next(c for c in clients if c['playerId'] == current['hostPlayerId'])
    api.command(base, host, 'advance')
    alive = [c for c in clients if c != victim]
    speaker, listener = alive[:2]
    caps = communication.comm(base, speaker)
    api.check(caps['voice']['available'] and caps['voice']['canTransmit'] and not caps['voice']['microphoneActive'],
              'Vote distant : micro volontaire, initialement coupé.')
    dead_caps = communication.comm(base, victim)
    api.check(dead_caps['voice']['canListen'] and not dead_caps['voice']['canTransmit'], 'Éliminé : écoute seule.')
    chunk = communication.voice_request(caps)
    communication.send_voice(base, victim, chunk, expected=403)
    receipt = communication.send_voice(base, speaker, chunk)
    retry = communication.send_voice(base, speaker, chunk)
    api.check(receipt['cursor'] == retry['cursor'], 'Un fragment PCM rejoué ne crée pas de doublon.')
    received = communication.poll_voice(base, listener, caps)
    api.check(len(received['packets']) == 1 and received['packets'][0]['pcmBase64'] == chunk['pcmBase64'],
              'Le second client reçoit exactement les octets PCM envoyés.')
    api.check(communication.poll_voice(base, speaker, caps)['packets'] == [], 'Pas de retour de sa propre voix.')
    changed = {**chunk, 'pcmBase64': base64.b64encode(bytes(6400)).decode()}
    communication.send_voice(base, speaker, changed, expected=409)
    for _ in range(7):
        communication.send_voice(base, speaker, communication.voice_request(caps))
    communication.send_voice(base, speaker, communication.voice_request(caps), expected=429)
    text, text_request = communication.send_text(base, speaker, 'village', 'Reprise du vote')
    saved = server.data / (host['roomCode'] + '.json')
    snapshot_text = saved.read_text()
    api.check(chunk['pcmBase64'] not in snapshot_text and all(c['sessionToken'] not in snapshot_text for c in clients),
              'Sauvegarde privée : ni audio ni jeton brut.')
    server.restart()
    for client in clients:
        resumed, _ = api.http(base, 'POST', api.path(client, 'join'), {'resumeToken': client['sessionToken']})
        api.check(resumed['view']['selfRoleId'] == initial_roles[client['playerId']], 'Redémarrage : même rôle pour chaque acteur.')
    restored_caps = communication.comm(base, listener)
    api.check(communication.poll_voice(base, listener, restored_caps)['packets'] == [], 'Aucun ancien buffer audio après redémarrage.')
    retried, _ = communication.send_text(base, speaker, 'village', text_request['text'], request=text_request)
    api.check(retried['sent']['id'] == text['sent']['id'], 'Le texte et son identifiant survivent au redémarrage.')
    first, second = alive[0]['playerId'], alive[1]['playerId']
    for index, client in enumerate(alive):
        api.command(base, client, 'vote', choice='pass' if index == 4 else 'vote',
                    targetIds=[] if index == 4 else [first if index in (1, 2) else second])
    current = api.state(base, host)
    api.check(current['phase'] == 'vote', 'Les confirmations ne ferment pas avant la fenêtre vocale distante.')
    api.wait_view(base, host, lambda v: v['phase'] == 'verdict', timeout=35)
    communication.send_voice(base, speaker, chunk, expected=409)
    communication.poll_voice(base, listener, caps, expected=409)
    verdict_caps = communication.comm(base, listener)
    communication.poll_voice(base, listener, verdict_caps, expected=403)
    api.wait_view(base, host, lambda v: v['phase'] == 'vote' and v['voteRound'] == 2, timeout=8)
    communication.send_voice(base, speaker, chunk, expected=409)
    fresh = communication.comm(base, listener)
    api.check(not fresh['voice']['microphoneActive'] and communication.poll_voice(base, listener, fresh)['packets'] == [],
              'Second scrutin : nouvelle génération, micro coupé et aucun ancien segment.')


def main():
    dotnet = os.environ.get('DOTNET') or api.UNITY_DOTNET
    with tempfile.TemporaryDirectory(prefix='ngame-lifecycle-') as temporary:
        server = communication.Server(dotnet, temporary)
        server.env['NGAME_CONNECTED_GRACE_SECONDS'] = '1'
        try:
            server.start()
            run(server)
        finally:
            server.stop()
            server.log.seek(0)
            api.check(not any(token in server.log.read() for token in api.TOKENS), 'Aucun jeton dans les logs.')
            server.log.close()
    print(f'OK — {api.CHECKS} contrôles HTTP : succession, reprise en manche, choix idempotents et gates vocaux réels. Pas de test acoustique/appareil.')


if __name__ == '__main__':
    main()
