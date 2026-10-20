# Vérification serveur et communications — 20 octobre 2026

État local testé dans des processus isolés et des dossiers de données temporaires. Aucun serveur public, domaine, compte fournisseur ou déploiement de conteneur créé. Les résultats ci-dessous ne constituent pas un essai humain sur téléphone.

## Compilation

SDK utilisé : `/Applications/Unity/Hub/Editor/6000.6.6f1-arm64/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet`.

```text
dotnet build Server/NGame.Server/NGame.Server.csproj --nologo
La génération a réussi.
37 avertissements nullable dans les sources partagées, 0 erreur.
```

La compilation a été relancée après la correction du reçu de chat tardif ; même résultat sans erreur. Les avertissements ne sont pas présentés comme une compilation sans avertissement.

## Parcours HTTP réel

```text
python3 Server/NGame.Server/smoke.py --no-build
OK — 555 vérifications HTTP, six clients distincts, partie complète, confidentialité et limites.
```

Ce parcours couvre authentification, six acteurs, confirmations, distribution personnelle, découverte des cartes, enquête, attaque, aube, discussion, vote, verdict lisible, résultat, revanche, limites d’entrée et expiration. Le script existant a été adapté aux nouvelles étapes de distribution et de verdict. Il utilise le mode présence pour la partie principale.

```text
python3 Server/NGame.Server/communications_smoke.py
OK — 1044 vérifications HTTP chat/vocal, contrôle des phases, PCM réel et redémarrages durables. Audio sur appareil non testé.
```

Les vérifications incluent :

- Autorisation Village, Meute et Observateurs ; un éliminé n’écrit pas aux vivants ; l’hôte non autorisé n’obtient pas les secrets.
- Messages en texte brut, auteur imposé par le serveur, déduplication, conflit et limite de débit.
- Plusieurs redémarrages avec même identité, même manche, mêmes rôles, même enquête privée et même scrutin.
- Permissions audio fermées en salon, distribution, nuit, discussion et verdict.
- Relais d’un fragment réel de PCM16 connu entre deux sessions, sans écho de sa propre voix ; déduplication, format, débit et refus d’émission d’un éliminé.
- Vote distant de 30 secondes conservé malgré les cinq confirmations ; clôture anticipée refusée avant la durée minimale ; second tour avec une nouvelle génération, ancien fragment refusé et aucun ancien audio.
- Audio absent des sauvegardes et des buffers restaurés ; tokens bruts absents des fichiers et des logs ; permissions de sauvegarde 0600 sous Unix.
- Départ effectif d’un salon, succession de l’hôte, réponse sans vue et ancienne session refusée.

Les nombres de vérifications comprennent les interrogations répétées pendant l’attente des échéances ; ce ne sont pas 1044 scénarios utilisateur distincts.

## Correction après audit : réponse de chat perdue au changement de phase

Le client effaçait initialement l’identifiant d’un envoi incertain au changement de tour. Si le serveur avait accepté le message mais que sa réponse était perdue, un nouvel identifiant aurait pu produire un doublon.

Correction : intentions incertaines conservées par canal et par manche, même identifiant lors de la reprise, résolution par lecture d’historique. Un reçu ancien peut être acquitté sans nouvel envoi seulement si la manche reste identique et le canal lisible. Un message inédit avec un ancien tour reste refusé, comme tout accès à une Meute désormais fermée.

```text
python3 Server/NGame.Server/communications_smoke.py --stale-only
OK — 219 vérifications HTTP ciblées : reçu tardif idempotent, canal fermé et intentions périmées refusés.
```

Ce dernier passage est exécuté après la correction. Il rejoue une vraie nuit, envoie pendant Discussion, passe au Vote et reprend exactement l’ancien envoi. L’historique ne contient qu’une copie ; ancien tour inédit, fausse manche, contenu différent et reçu de Meute fermée sont refusés. La suite complète précédente n’a pas été répétée sans raison après ce contrôle ciblé.

## Packaging

```text
docker compose -f Server/NGame.Server/compose.example.yaml config --quiet
exit 0

docker compose --env-file Server/NGame.Server/.env.example \
  -f Server/NGame.Server/compose.example.yaml \
  -f Server/NGame.Server/compose.https.example.yaml config --quiet
exit 0
```

Docker Compose v5.0.2 est présent. Le démon Docker ne répond pas sur le socket local : image Docker non construite, conteneur et HTTPS non exécutés. Le Dockerfile utilise le Core partagé, une étape de build .NET 8, un runtime non privilégié, le volume privé, un contrôle de santé et un système de fichiers en lecture seule. Les commandes d’hébergement et leurs limites sont dans [README.md](README.md).

## Correction ciblée suivante : remplacer un membre dans un salon plein

L’audit a relevé que la limite de 24 utilisait le nombre de sessions, avant l’élagage des sièges quittés. Un salon rempli puis réduit à 23 personnes aurait donc pu refuser son remplaçant. La limite compte maintenant les membres encore présents dans le moteur ; le reçu de session d’une personne retirée ne consomme plus une place.

```text
python3 Server/NGame.Server/communications_smoke.py --capacity-only
OK — 42 vérifications HTTP ciblées : salon plein, départ et remplacement sans siège fantôme.
```

Le cas crée réellement 24 acteurs HTTP, fait partir un invité, reçoit un remplaçant, vérifie les 24 sièges sans l’ancien joueur, refuse l’ancien accès et refuse la 25e personne. Nouveau build .NET : 0 erreur, 37 avertissements nullable. Empreinte SHA-256 de l’assemblage après cette correction serveur uniquement : `9357bdf45a3b1339bad47911a2113c6bcf4e5f6e0bcf600429d68a2f763cb8e9`. Les sources Unity restent gelées.

## Limites à conserver dans le suivi

- Le code natif utilise réellement `Microphone`, PCM et `AudioSource`, mais capture, écoute, permissions, latence acoustique, écho et Bluetooth n’ont pas été essayés sur appareil par ce lot.
- WebGL : contrat de texte partagé et refus explicite du microphone dans cet adaptateur ; aucune exécution du chat dans un build WebGL n’est prouvée par ce lot.
- Le format PCM est non compressé, environ 43 Ko/s encodés par orateur hors en-têtes, calculé et non mesuré sur mobile. Charge et batterie restent à mesurer.
- Capacité de reprise native dans le dossier privé de l’application, fichier 0600 sur Unix ; pas de coffre matériel. Web : `sessionStorage` ne garantit pas une reprise après fermeture de l’onglet.
- Les tests de transport ne valident ni l’ergonomie d’un novice ni l’apparence des 52 maquettes ; ces vérifications appartiennent à une recette distincte.

## Retrait d’un invité par l’hôte

Après ajout partagé de `remove-player` et de `startBlockedReason`, le serveur élague les accès des personnes réellement retirées du moteur. Le parseur de commandes acceptait déjà la forme cible existante ; aucun chemin permettant à un client de choisir l’acteur n’a été ajouté. Le reçu propre à un départ volontaire reste disponible pour acquitter une réponse perdue.

```text
python3 Server/NGame.Server/communications_smoke.py --moderation-only
OK — 153 vérifications HTTP ciblées : retrait hôte, refus invité, révocation et remplacement durables.
```

Scénario réel de 24 membres : lancement possible après préparations ; invité non autorisé à retirer quelqu’un ; hôte non autorisé à se retirer lui-même par cette commande ; retrait d’un invité réinitialisant les préparations ; répétition idempotente ; ancien accès refusé pour état, reprise, commandes et communications ; remplaçant accepté ; mêmes refus et 24 places conservés après redémarrage.

Dernier assemblage serveur compilé après ce lot : 0 erreur, 37 avertissements nullable. Empreintes SHA-256 :

```text
f4ead3563de362f71dc0d68e3a0fbc31c54d88e327585b20615156e7e09a08b3  Server/NGame.Server/bin/Debug/net8.0/NGame.Server.dll
f27d096ed88e4ed8595373c8a9db8645a71817bb0157e76da7acae6bb08a7f84  Server/NGame.Server/communications_smoke.py
```

Le `NGameSession.jslib` de reprise Web a passé `node --check` ; il s’agit d’une vérification syntaxique, pas d’une exécution WebGL ni d’un build navigateur.

## Empreintes après le contrôle ciblé

Les empreintes suivantes identifient l’assemblage et les scripts disponibles après les 219 vérifications ciblées. Elles ne sont pas attribuées rétroactivement aux passages précédents, dont l’assemblage n’avait pas été empreinté.

```text
51a118eff8db7a685f2b5abe6d8254cfcaa3b6f4312e2c030cc2b2e11fda6917  Server/NGame.Server/bin/Debug/net8.0/NGame.Server.dll
7b4fe8cac482e2a137a84c1c5b131758a61f89f17816008e496628d8474ab9ad  Server/NGame.Server/communications_smoke.py
332a816c6d53422640244a86bd4f3f79b61919f0738ef30297889f8727767ff6  Server/NGame.Server/smoke.py
```
