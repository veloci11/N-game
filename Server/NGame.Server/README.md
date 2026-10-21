# Serveur autoritaire N’GAME

Serveur HTTP ASP.NET Core 8 autonome. Il compile les sources pures de `Assets/NGame/Core` par lien dans le projet : le serveur et Unity utilisent le même moteur `WerewolfRoom` et les mêmes contrats JSON. L’éditeur Unity n’est pas lancé par ce serveur.

Cette version conserve les salles, parties, reçus de commandes, historiques texte et empreintes de sessions dans des **sauvegardes locales atomiques**. Un redémarrage retrouve les mêmes identités, cartes et effets déjà enregistrés. La place de l’hôte passe à un membre connecté quand il quitte ou devient absent. Il n’y a pas de compte utilisateur, de réplication entre serveurs ou d’hébergement public inclus.

Le chat écrit et un relais vocal natif sont intégrés. Le vocal est fermé hors du **vote de jour en mode distant** ; les éliminés peuvent écouter pendant ce vote, sans émettre. Le microphone reste volontaire et doit être réactivé après chaque scrutin ou interruption. Le client WebGL conserve le contrat de chat écrit ; cette implémentation n’expose pas le microphone du navigateur. Son exécution dans un build WebGL n’a pas été validée par les essais HTTP ci-dessous.

## Lancer

Depuis la racine du dépôt :

```sh
bash Server/NGame.Server/run.sh
```

Adresse par défaut : `http://127.0.0.1:8787`. Vérification :

```sh
curl --fail http://127.0.0.1:8787/health
```

Le script utilise d’abord le SDK Unity installé à `/Applications/Unity/Hub/Editor/6000.6.6f1-arm64/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet` (SDK 8.0.318, ASP.NET Core 8). Un autre exécutable peut être choisi avec `DOTNET`.

Pour les téléphones sur le même réseau local :

```sh
ASPNETCORE_URLS=http://0.0.0.0:8787 bash Server/NGame.Server/run.sh
```

Dans le client, utiliser l’adresse réseau du Mac, par exemple `http://192.168.1.25:8787`, avec l’adresse réelle de la machine. `127.0.0.1` dans un téléphone désigne ce téléphone.

Pour servir aussi un build WebGL existant :

```sh
NGAME_WEB_ROOT="$PWD/Builds/Web" bash Server/NGame.Server/run.sh
```

Le dossier doit exister. `/` sert son `index.html`. Les fichiers `.wasm` ont le MIME `application/wasm` ; les variantes `.br` et `.gz` conservent le MIME original et reçoivent leur `Content-Encoding`. Le serveur ne produit pas le build Unity.

Le fonctionnement local/LAN utilise HTTP. Pour un hébergement public, placer ce processus derrière un frontal HTTPS ; aucun hébergement public n’est configuré ici.

## Tester

```sh
python3 Server/NGame.Server/smoke.py
python3 Server/NGame.Server/communications_smoke.py
```

Python utilise uniquement sa bibliothèque standard. Le script compile, lance un processus isolé sur un port local libre, crée six clients HTTP avec leurs propres sessions, joue une partie complète puis arrête son processus. Il vérifie notamment :

- Authentification absente, invalide et inter-salles ; refus d’un token fourni uniquement dans l’URL.
- Création, entrées concurrentes, reprise de la même session, noms et payloads invalides.
- Permissions de l’hôte, confirmations concurrentes, distribution exacte et confidentialité des rôles/enquêtes.
- Actions et cibles autorisées, tour périmé, relance idempotente et conflit de contenu.
- Résolution nocturne, vote, victoire et revanche avec tour monotone.
- Connexion expirant après une absence réelle, retour connecté après un nouvel échange, plafond de 24 joueurs et expiration d’une salle inactive.
- Fichiers Web statiques/MIME, limites HTTP et absence des tokens dans les journaux du processus.

Les délais de connexion et d’expiration sont raccourcis dans ce processus de test. Ces tests vérifient le transport et le moteur ; ils ne valident pas à eux seuls le rendu ni les interactions du client Unity dans un navigateur.

Le second script teste six sessions authentifiées, les permissions Village/Meute/Observateurs, les doublons et débits, plusieurs redémarrages, la conservation des cartes et de l’enquête privée, et le relais de véritables octets PCM. Il traverse un vote distant de 30 secondes puis un second scrutin. Il démontre le transport et sa fermeture aux frontières des phases ; **il ne teste pas un microphone, un haut-parleur, un casque ou une permission système sur appareil**.

`--no-build` réutilise l’assemblage existant. `--base-url http://127.0.0.1:8787` teste explicitement un serveur existant sans le lancer ni l’arrêter ; cette option crée des salles de test sur ce serveur, et ne change pas ses délais.

Compilation directe :

```sh
/Applications/Unity/Hub/Editor/6000.6.6f1-arm64/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet build Server/NGame.Server/NGame.Server.csproj --nologo
```

## API

Tous les corps sont `application/json`, avec les champs publics en camelCase des contrats Unity. Le parseur refuse les champs inconnus, les types incorrects et les objets de profondeur excessive. Aucun identifiant d’acteur envoyé par le client ne remplace l’acteur authentifié.

| Méthode et chemin | Corps / authentification | Réponse |
| --- | --- | --- |
| `GET /health` | Aucun | État du service, version API et nombre de salles |
| `POST /api/rooms` | `CreateRoomRequest` | `201 RoomAccess` |
| `POST /api/rooms/{code}/join` | `JoinRoomRequest` | `200 RoomAccess` |
| `GET /api/rooms/{code}/state` | `Authorization: Bearer <sessionToken>` | `200 RoomView`, privé pour le joueur |
| `POST /api/rooms/{code}/commands` | `RoomCommand` et même en-tête Bearer | `RoomCommandResult` avec vue privée actuelle |
| `GET /api/rooms/{code}/communications` | Même Bearer | Canaux autorisés, génération et droits vocaux |
| `GET /api/rooms/{code}/messages/{channel}?after=0` | Même Bearer | Page de 50 messages autorisés, curseur |
| `POST /api/rooms/{code}/messages` | `ChatSendRequest` et même Bearer | Message enregistré une seule fois |
| `GET /api/rooms/{code}/voice?matchId=…&turn=…&after=0` | Même Bearer, vote distant courant | Fragments audio éphémères des autres joueurs |
| `POST /api/rooms/{code}/voice` | `VoiceChunkRequest` et même Bearer | Accusé du fragment audio validé |

Création :

```json
{
  "displayName": "Léa",
  "avatarIndex": 0,
  "settings": {
    "mode": "in-person",
    "ruleset": "ngame-classic-v1",
    "discussionSeconds": 90,
    "nightSeconds": 25,
    "voteSeconds": 90,
    "revealEliminatedRoles": true,
    "composition": []
  }
}
```

Une composition vide active la proposition dynamique du moteur. Une composition explicite utilise des lignes `{ "roleId": "werewolf", "count": 2 }`. Le nombre total doit correspondre au nombre de joueurs au démarrage. Chaque spécial est unique ; `werewolf` et `villager` peuvent apparaître en plusieurs exemplaires dans leur ligne.

Le réglage public `settings.nightProtocol` accepte `traditional` (valeur par défaut, y compris anciennes sauvegardes sans champ) et `screen`. La nuit traditionnelle conserve les yeux fermés autour de la table. La nuit sur écran permet l’activité commune et les scènes publiques : chaque appel garde son échéance serveur, même si tous les pouvoirs ont été confirmés ; l’hôte ne peut pas l’écourter. La Petite Fille est refusée en nuit sur écran, et reste interdite à distance. Un changement de protocole dans le salon annule les confirmations de préparation. Les nouveaux clients qui envoient ce champ doivent utiliser ce serveur à jour : une ancienne version au parseur strict le rejetterait.

Recette ciblée, après compilation : `python3 Server/NGame.Server/night_protocol_smoke.py`. Elle démarre et arrête son propre serveur localhost avec données temporaires, sans intervenir sur le serveur de développement.

Nouvelle entrée : `{ "displayName": "Max", "avatarIndex": 1 }`. Reprise : `{ "resumeToken": "<sessionToken précédemment reçu>" }`. Une reprise valide retrouve le même `playerId` et le même token, y compris après le début de la partie. Elle n’ajoute pas de joueur et ne change pas son identité. Sans ce token, un pseudo ne permet pas de récupérer un siège.

`RoomAccess` contient `roomCode`, `playerId`, `sessionToken`, `view`. Le token est une capacité privée aléatoire de 32 octets, encodée base64url (43 caractères). Le serveur n’en conserve qu’une empreinte SHA-256. Le client le transmet dans l’en-tête Bearer, jamais dans l’URL ; le corps `resumeToken` est réservé à la reprise. Il ne faut pas inclure ce token dans un lien de partage. Le code public de salle suffit pour inviter un nouvel arrivant.

Commande :

```json
{
  "kind": "ready",
  "ready": true,
  "turn": 0,
  "commandId": "9d6150b2e8d24c50bceabf14fa5b97bb"
}
```

Toujours prendre `turn` dans la dernière `RoomView` et générer un nouvel identifiant pour une nouvelle intention. En cas de transport incertain, renvoyer exactement le même objet et le même `commandId`. La réponse contient une vue fraîche, même pour une relance déjà traitée. La déduplication HTTP garde 128 reçus par session ; le moteur partagé possède également une fenêtre bornée de 2048 reçus par salle. Il ne s’agit pas d’un journal permanent.

Commandes du moteur : `ready`, `configure` (réglages complets, hôte et lobby), `start`, `acknowledge-role`, `act`, `vote`, `advance`, `reset`, `leave`, `remove-player`. Une reconfiguration remet tout le monde à « pas prêt ». `RoomView.startBlockedReason` décrit le blocage réel du lancement. L’hôte peut retirer un invité en salon avec `remove-player` et une cible unique, jamais lui-même ni un membre en cours de manche ; les autres doivent confirmer à nouveau et l’ancien accès du joueur retiré est révoqué. `start` ouvre la distribution : chacun confirme avoir lu sa carte avant la nuit, avec une échéance d’absence de deux minutes. `action.choices`, `action.allowedTargetIds` et les champs structurés d’action décrivent les choix effectivement disponibles ; le serveur revérifie les permissions et les cibles. Les autres joueurs n’obtiennent ni les rôles cachés, ni les informations privées de leur voisin. Les états privés sont envoyés avec `Cache-Control: no-store, private`.

Le vote distant conserve sa fenêtre de 30–300 secondes même si tous ont répondu. L’hôte peut le fermer après 30 secondes si tous les votes sont reçus. Un verdict lisible précède le second tour, la réaction ou la suite. `leave` retire réellement un membre en salon ; en manche, sa place reste réservée jusqu’à la revanche, sans redistribution. La réponse de départ a `view: null` et le client efface alors son accès local. Une simple coupure réseau conserve l’accès et le rôle.

Le moteur actuel prend en charge `werewolf`, `villager`, `seer`, `witch`, `hunter`, `cupid`, `guard`, `little-girl`. La Petite Fille requiert le protocole physique en mode `in-person` et est refusée en mode `remote`. Les autres rôles du catalogue artistique ne sont pas activables dans ce jeu de règles. Discussion : 5–600 secondes ; étape nocturne : 3–120 secondes ; vote distant : 30–300 secondes. Chaque phase est résolue par le serveur et ses échéances, indépendamment du rendu des clients.

## Conversations et audio

`Communications.channels` fournit les droits actuels et les raisons de fermeture. Le serveur décide à partir d’une projection fraîche, après le tick de l’horloge. Les canaux sont `village`, `meute` et `observateurs` ; un joueur ne peut pas obtenir un canal en le nommant simplement. La Meute n’est ouverte qu’aux loups vivants pendant leur appel, les éliminés n’écrivent pas aux vivants, et l’hôte n’obtient aucun droit privé supplémentaire.

Un `ChatSendRequest` contient `channelId`, `matchId`, `turn`, `policyEpoch`, `clientMessageId`, `text`. Auteur et heure viennent du serveur. Texte brut de 1–500 caractères, historique borné à 200 messages par canal, pages de 50 ; 3 nouveaux envois maximum par 2 secondes et 30 par minute et par acteur. Un identifiant rejoué avec le même contenu ne crée pas de second message, tant qu’il est dans l’historique retenu ; un contenu différent est refusé. Après un changement de tour, un reçu déjà enregistré peut encore être acquitté si la manche est la même et le canal reste lisible ; cela n’autorise jamais un nouvel envoi périmé. Une ancienne Meute désormais fermée reste inaccessible, même pour acquitter un reçu. La nouvelle manche vide l’historique. Les noms et messages doivent être affichés sans interpréter de balises.

Le fournisseur vocal de cette version est `ngame-pcm16`, un transport HTTP propre au jeu, sans compte ni SDK externe. `VoiceChunkRequest` contient `matchId`, `turn`, `policyEpoch`, `chunkId`, `pcmBase64` : exactement 3200 échantillons mono PCM16 little-endian à 16 kHz, soit 200 ms / 6400 octets. Le serveur déduit l’émetteur. Chaque fragment, y compris rejoué, doit appartenir à la fenêtre autorisée ; un ancien fragment est refusé après le verdict ou au second tour. Limite de 8 nouveaux fragments par seconde et joueur, reçus bornés, file commune bornée à 384 fragments, expiration de l’audio après 1,8 seconde. Une lecture n’envoie pas sa propre voix et restitue au maximum 128 fragments.

Ces données audio ne sont ni journalisées ni sauvegardées. Les clients ferment réellement la capture, vident les buffers et arrêtent les sources à l’échéance, au changement de phase, à la perte de focus et au défaut réseau. La permission système reçue trop tard ne rouvre pas le micro. La fermeture locale utilise l’heure serveur, l’âge de la réponse et une estimation conservatrice du trajet ; elle ne prolonge pas une fenêtre depuis un état ancien.

Ce PCM sans compression est une première solution native à mesurer sur les appareils : un orateur représente environ 43 Ko/s de base64 hors en-têtes, à multiplier par les destinataires. Ce chiffre est calculé depuis le format, pas une mesure mobile. Latence acoustique, batterie, écho et montée en charge ne sont pas encore validés. Un fournisseur compressé pourra remplacer cet adaptateur sans changer les droits du jeu. Pour l’accès Internet, le serveur doit être servi en HTTPS comme le reste des données privées.

## Conservation et reprise

Les sauvegardes sont dans `NGAME_DATA_DIRECTORY`, par défaut le dossier de données utilisateur `NGame/ServerState` du système. Le serveur impose un dossier privé distinct du dossier Web public ; sous Unix, le dossier est en 0700 et les fichiers en 0600. Il écrit un fichier temporaire, force l’écriture, puis le remplace atomiquement. Les empreintes de sessions et les reçus HTTP sont conservés avec le moteur ; jamais le token Bearer brut. Les messages privés et rôles figurent dans cette sauvegarde privée, qui n’est pas un fichier à publier. Une sauvegarde illisible est conservée pour diagnostic et la salle reste indisponible ; elle n’est pas réinventée avec de nouvelles cartes.

Les commandes et messages sont sauvegardés avant leur réponse de succès. Les ticks qui changent les règles sont sauvegardés ; l’activité de connexion est enregistrée au plus toutes les cinq secondes quand l’état n’a pas changé. Une salle expirée est supprimée, sauvegarde comprise. L’audio est volontairement absent de cette conservation.

Sur le client natif, seule la capacité de reprise (adresse, salle, identifiant public, token) est conservée dans le dossier privé de l’application, avec fichier 0600 sur Unix. Aucun rôle ou état privé n’est mis en cache. Ce fichier dépend de la protection du compte et du stockage de l’appareil ; ce n’est pas un coffre matériel. Sur WebGL, la capacité reste dans `sessionStorage` pour l’onglet et permet un rechargement ; fermer l’onglet n’est pas une reprise durable garantie. Une erreur d’authentification ou un départ volontaire efface cette capacité. Une nouvelle adresse de serveur efface l’ancien accès.

## Erreurs et limites

Les erreurs applicatives utilisent aussi `RoomCommandResult` : `ok: false`, `code`, `message` français, `view` facultative. Les erreurs de règles ont une vue actuelle et HTTP 409 (`HOST_ONLY`, `NOT_READY`, `STALE_TURN`, `INVALID_TARGET`, etc.). Le transport utilise notamment HTTP 400 (`invalid_json`, `invalid_name`, etc.), 401 (`unauthorized`), 404 (`room_not_found`), 413 (`body_too_large`), 415 (`json_required`), 429 (`rate_limited`) et 503 (`room_capacity`). `command_conflict` signale un identifiant de commande réutilisé avec un contenu différent.

- 200 salles maximum et 24 acteurs maximum par salle ; démarrage à partir de six joueurs.
- Corps limité à 16 Kio, profondeur JSON 16 ; pseudo de 1–24 caractères ; six avatars (indices 0–5).
- Tableau de composition limité à 16 lignes ; nombre et longueur des cibles/commandes bornés avant le moteur.
- Créations et entrées partagent un budget global de 100 requêtes par minute ; les interrogations d’état et les commandes ne consomment pas ce budget.
- Verrou par salle autour de chaque accès au moteur, avec tick serveur toutes les 500 ms.
- Une absence d’échange authentifié pendant 15 secondes projette `connected: false`. Un nouveau poll, une commande ou une reprise rétablit ce statut. Cela n’élimine pas le joueur et ne libère pas son siège.
- Une salle sans activité authentifiée depuis deux heures est supprimée avec toutes ses sessions. Les ticks automatiques ne prolongent pas ce délai. Interroger une salle avec un token invalide ne la maintient pas en vie.
- Aucun état privé, token ou corps de requête n’est écrit dans les logs applicatifs ; les exceptions renvoient un message générique sans trace interne.

## Hébergement préparé, pas déployé

Les fichiers `Dockerfile`, `.dockerignore` à la racine, `compose.example.yaml`, `compose.https.example.yaml` et `Caddyfile` fournissent un packaging autonome. **Le contexte de construction est la racine du dépôt**, car le projet partage les sources Core avec Unity :

```sh
docker build -f Server/NGame.Server/Dockerfile -t ngame-server:local .
docker compose -f Server/NGame.Server/compose.example.yaml config --quiet
docker compose -f Server/NGame.Server/compose.example.yaml up --build -d
curl --fail http://127.0.0.1:8080/health
```

L’image finale tourne avec l’utilisateur non privilégié de .NET, écoute sur 8080 et ne reçoit que les sources nécessaires. Le volume nommé `ngame-state` contient les sauvegardes privées ; le système de fichiers du conteneur est en lecture seule hors volume et `/tmp`. Le contrôle de santé utilise `/health`. Ce Compose lie le port uniquement à la boucle locale de l’hôte ; il attend un frontal pour l’accès Internet.

Pour une mise en service ultérieure, renseigner un vrai domaine dans un fichier d’environnement local et pointer son DNS sur l’hôte, puis utiliser aussi la configuration HTTPS :

```sh
docker compose --env-file Server/NGame.Server/.env.example \
  -f Server/NGame.Server/compose.example.yaml \
  -f Server/NGame.Server/compose.https.example.yaml config --quiet
```

La commande ci-dessus vérifie seulement la configuration d’exemple ; elle ne publie rien et ne demande aucun certificat. Pour le démarrage réel, remplacer `.env.example` par le fichier local contenant le **domaine réel**, puis employer `up --build -d` après la préparation de l’hôte. Caddy transmet à `ngame:8080` sur le réseau interne et conserve ses certificats dans ses propres volumes. Les ports 80/443 doivent alors être disponibles. Les téléphones utilisent tous `https://<domaine-réel>`, jamais leur propre `localhost`.

Pour un essai LAN sans conteneur, la commande `ASPNETCORE_URLS=http://0.0.0.0:8787 bash Server/NGame.Server/run.sh` reste disponible. Tous les téléphones doivent saisir la **même adresse LAN du Mac**, pas `127.0.0.1` ; cette dernière désigne chaque téléphone lui-même.

Le client natif ne nécessite pas d’en-tête CORS. Pour un build Web servi par le même domaine, laisser `NGAME_ALLOWED_ORIGINS` vide. Si le Web est sur un autre domaine, inscrire ses origines HTTPS exactes, séparées par des virgules, par exemple `https://jeu.example.com` ; ne pas mettre `*`. Aucun ancien code ou outil tiers ne doit partager le dossier public avec les sauvegardes. Un build WebGL réellement produit peut être monté séparément en lecture seule et désigné par `NGAME_WEB_ROOT` ; ce packaging ne fabrique pas le build Unity.

Cette version emploie une seule instance serveur pour ses salles. Dupliquer le conteneur derrière un répartiteur ne constitue pas une mise à l’échelle valide : les verrous et buffers audio sont locaux. Conserver le volume lors des mises à jour ; ne pas utiliser `down --volumes` pour une mise à jour ordinaire. Les sauvegardes de ce volume sont privées et doivent conserver les permissions de leurs fichiers.

Au 20 octobre 2026, les deux configurations Compose ont été validées par `docker compose … config --quiet`. Le démon Docker de ce Mac n’est pas accessible : **l’image et les conteneurs n’ont pas été construits ni exécutés dans cette séance**, et aucun domaine ou HTTPS public n’a été configuré. Le serveur .NET a été construit et essayé directement, séparément de ce packaging.

Références : [conteneurs .NET 8 et utilisateur non privilégié](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/containers), [limite de corps Caddy](https://caddyserver.com/docs/caddyfile/directives/request_body), [reverse proxy Caddy](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy).

## Configuration

| Variable | Défaut | Effet |
| --- | --- | --- |
| `ASPNETCORE_URLS` | `http://127.0.0.1:8787` | Adresses d’écoute, par exemple `http://0.0.0.0:8787` pour LAN |
| `DOTNET` | SDK installé avec Unity, puis SDK du PATH | Exécutable utilisé par `run.sh` et le smoke |
| `NGAME_WEB_ROOT` | Désactivé | Dossier du build Web à servir |
| `NGAME_ALLOWED_ORIGINS` | Aucune origine externe | Liste séparée par virgules d’origines Web autorisées ; même origine fonctionne sans ce réglage |
| `NGAME_ROOM_TTL_SECONDS` | `7200` | Expiration d’inactivité, 5–86400 secondes |
| `NGAME_CONNECTED_GRACE_SECONDS` | `15` | Fenêtre du statut connecté, 1–120 secondes |
| `NGAME_DATA_DIRECTORY` | Données utilisateur / `NGame/ServerState` | Sauvegardes privées, hors du dossier Web ; choisir un dossier isolé pour les tests |

Une configuration temporelle hors limites fait échouer le démarrage avec un message explicite. Aucun paquet NuGet externe ni service hébergé n’est nécessaire pour cette version.
