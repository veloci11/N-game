# Relecture indépendante moteur et serveur

Passage terminé le 2026-10-20T22:31:06.188659+00:00. Pas de nouveau P1 reproduit dans le périmètre vérifié. Aucune source de production modifiée ; ajout d’une recette HTTP ciblée `Server/NGame.Server/lifecycle_smoke.py`. Sources figées après ce passage, sans Unity Editor ni déploiement public.

## Preuves actuelles

| Contrôle | Résultat | Preuve |
| --- | --- | --- |
| Moteur C# pur, tests NUnit via .NET fourni avec Unity | 67/67 | `core-final-audit.log` |
| Partie HTTP complète, six identités, concurrence, secrets, erreurs et nouvelle manche | 553 contrôles/requêtes, aucun échec | `http-full-final-audit.log` |
| Parcours supplémentaire succession, reprise pendant choix et transport vocal | 1016 contrôles/requêtes, aucun échec | `http-lifecycle-final-audit.log` |

Les comptes HTTP comprennent les lectures de polling, notamment l’attente du vote distant de 30 secondes ; ce ne sont pas 1569 scénarios distincts. Build serveur : 0 erreur, 37 avertissements nullable. Syntaxe Python de la nouvelle recette vérifiée. Serveurs isolés sur ports temporaires, données privées temporaires ; aucun serveur de développement interrompu.

## Ce qui a réellement été exercé

- L’hôte cesse ses requêtes, pendant que les invités restent connectés : le premier invité prend l’organisation. La place absente bloque réellement le démarrage avec `PLAYER_OFFLINE`. La reprise retrouve le même siège et ne reprend pas le titre d’hôte au successeur.
- Après lancement, chaque capacité reprend sa carte pendant Distribution ; un nouveau joueur est refusé. Pendant le pouvoir de la meute, le loup retrouve sa propre action, puis rejoue le même identifiant sans double conséquence.
- Voix refusée en lecture/émission la nuit ; autorisée au vote distant avec micro volontaire initialement coupé. Un éliminé ne peut émettre. Un autre acteur reçoit exactement les octets PCM, une répétition n’ajoute pas de paquet, aucun écho local, contenu modifié et débit excessif refusés.
- Le fichier de salle ne contient ni jetons bruts ni audio. Après redémarrage, chaque acteur retrouve son rôle, le message texte et son reçu restent idempotents ; aucun tampon audio n’est restauré. Un ancien segment est refusé au verdict et au scrutin suivant, dont le micro reste coupé.
- Les tests du moteur couvrent les huit rôles, restrictions de mode, potions/protection, amoureux, dernier tir, votes et égalités, secrets, horloges et capture/restauration des phases. Les conditions de victoire suivent la convention N’GAME documentée, sans victoire automatique à parité.

Le contrat texte « vote uniquement » reste celui de [chat-vote-only.md](chat-vote-only.md) : Village R/W pour les vivants ; Village R seul et Observateurs R/W pour les éliminés ; Meute absente ; aucune lecture/envoi hors vote. Sa dernière campagne ciblée de 913 contrôles n’a pas été répétée sans changement de cette source. Le nouveau parcours contrôle aussi sa reprise durable pendant le vote.

## Pour une partie jouable de 4–8 minutes

Le moteur est utilisable avec 6–24 identités ; huit rôles sont implémentés, mais la Petite Fille est physique, présence uniquement. À distance, sept types de rôle sont donc activables. Le nombre de tours, la lecture des cartes et les discussions empêchent de garantir une durée fixe.

Pour une recette rapide à 6–8 personnes : proposer discussion 30 s, étapes nocturnes 12 s, vote distant 30 s, et expliquer les choix avant le lancement. Ce sont des réglages de recette proposés, pas un nouveau preset déjà livré ni une durée humaine mesurée. La découverte initiale, les appels Cupidon/amoureux et les réactions ajoutent leur temps réel.

Restes concrets pour annoncer la fin du jeu :

1. **Interréseaux : hébergement réel.** Le serveur local fonctionne par adresse commune joignable ; le code de salle ne traverse pas le NAT à lui seul. Un serveur public HTTPS et son nom de domaine doivent être déployés et vérifiés avant de promettre que des joueurs sur des réseaux différents se rejoignent. Le packaging existe ; aucun déploiement extérieur n’a été effectué ici.
2. **Deux appareils réels et six joueurs.** Exécuter entrée/reprise, carte privée, pouvoir, vote, chat et retour réseau sur plateformes cibles. Les tests HTTP ne prouvent ni le clavier mobile ni l’ergonomie des choix, et aucun novice n’a joué une vraie partie dans ce lot.
3. **Voix native et plateformes.** Le PCM réseau est réel ; capture, écoute, permissions, écho, Bluetooth, latence et batterie restent à mesurer physiquement. WebGL ne dispose pas du micro dans cet adaptateur. Signature, distribution mobile et validation plateforme ne sont pas réalisées par cet audit.

Aucune conclusion « application mobile publiée de A à Z » n’est établie.

## Reproduire

```sh
python3 Server/NGame.Server/smoke.py
# Après le build ci-dessus, même serveur binaire mais port/données isolés :
python3 Server/NGame.Server/lifecycle_smoke.py
```

Le runner NUnit hors Editor de ce passage est dans `/tmp/ngame-core-validation-20261020` ; il compile les vraies sources et le fichier `WerewolfRoomTests.cs` du dépôt. Il ne constitue pas une exécution Unity EditMode.

## Empreintes SHA-256

```text
f4d95f81b978810572ce5fdee28dc2f45de7e6c4d283a92a79ab7c2984ca846a  Assets/NGame/Core/Multiplayer/WerewolfRoom.cs
1d96ccc68d6c5062de19f16066745604855fcd1dd931eb2f8f05c122072fecc0  Assets/NGame/Core/Multiplayer/RoomCommunications.cs
dac6e5be80b1c01e6819094bfb434e8119587acc4b84ffe52b542f877ab1b520  Assets/NGame/Core/Multiplayer/RoomVoiceRelay.cs
a82fefdfde330fda70735c7f51bc4c98f4e39003fb322f53e9cf0b357be6b4f1  Server/NGame.Server/bin/Debug/net8.0/NGame.Server.dll
b278d00acb33a8e62241804ebdead606ede27c48fa6ef8db455362defb51dd45  Server/NGame.Server/lifecycle_smoke.py
```
