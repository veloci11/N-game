# Parties complètes multiclients HTTP — 20 octobre 2026

Cette recette utilise dix sessions HTTP indépendantes, avec des choix coopératifs scriptés. Il s’agit de **clients simulés et non de dix personnes**, sans automatisation Unity, téléphone, gestes, microphone ou haut-parleur. Chaque session reçoit sa propre projection authentifiée ; l’orchestrateur les compare pour vérifier les frontières de confidentialité.

## Passage en présence — réussi

- Dix clients authentifiés ; découverte de dix cartes après préparation explicite.
- Deux nuits en protocole écran, appels de six secondes conservés jusqu’à leur échéance malgré les soumissions rapides et les tentatives de clôture anticipée.
- **Onze commandes de pouvoirs acceptées**, couvrant Cupidon, Garde, Voyante, Meute, Sorcière et Chasseur. Le Villageois reste passif. La Petite Fille est exclue de ce protocole et n’a pas été jouée dans cette recette.
- **Deux votes de jour, dix-sept bulletins acceptés**, verdicts publics correspondant exactement aux choix reçus.
- Fin réelle par **victoire du Village**, révélations exactes, puis revanche : mêmes dix places, ancienne information effacée et nouvelle phase de distribution démarrée.
- Durée observée : **77,84 s**, pour ce scénario et ses délais de test. Ce n’est pas une durée de partie humaine promise.

Preuves : [rapport JSON](full-match-http-in-person.json), [journal d’exécution](full-match-http-in-person.log). Les 2 422 requêtes et 5 583 vérifications de transport comprennent les lectures de polling, statuts HTTP et en-têtes cache. **Ce ne sont pas autant de scénarios de jeu.**

## Déroulement vérifié

1. Création, neuf entrées concurrentes, dix confirmations de préparation et distribution. Une session non hôte cesse réellement ses requêtes : elle devient absente, puis reprend la même place et la même carte avec sa capacité.
2. Cupidon lie deux Villageois ; la reconnaissance privée n’ordonne pas de fermer les yeux en nuit sur écran. Le serveur est redémarré pendant cet appel : même tour, échéance, protocole et pouvoir reçu. La répétition du même identifiant d’action ne crée pas une deuxième conséquence.
3. Le Garde protège la Voyante ; celle-ci consulte un Loup. La découverte reste uniquement dans sa projection privée. Les deux Loups désignent le troisième Villageois ; seule la Sorcière reçoit la victime autorisée, puis la soigne. Personne ne meurt à cette première aube.
4. Le premier vote élimine le Chasseur. Son dernier tir reste disponible malgré son décès ; il élimine le premier Loup avant la nuit suivante.
5. La deuxième nuit interdit au Garde sa cible précédente. La Sorcière ne dispose plus du soin, conserve le poison et empoisonne le troisième Villageois. Le décès est résolu à l’aube.
6. Au deuxième vote, les trois éliminés suivent le Village en lecture seule et échangent dans Observateurs ; les vivants n’accèdent pas à ce canal. Un éliminé ne vote pas. Le dernier Loup est éliminé, le Village gagne, puis une nouvelle manche démarre réellement.

La recette contrôle aussi que les cartes vivantes ne sont pas publiées, les capacités de pouvoir sont limitées au rôle appelé, la liste de meute ne part qu’aux Loups, les choix de vote restent cachés jusqu’au verdict et les messages sont refusés hors vote. Les scènes publiques ne nécessitent ni identité d’acteur ni victime privée. Le composant d’activité locale est relu pour confirmer l’absence de dépendance HTTP/commande ; son toucher réel dans Unity doit être vérifié séparément. Ces scénarios ne couvrent pas toutes les combinaisons de rôles, ni toutes les pannes réseau possibles.

## Passage à distance

Le second passage utilisant `--mode remote` **réussit indépendamment** : dix autres sessions simulées sur un autre serveur loopback et un dossier temporaire distinct. Même composition, deux nuits, onze commandes de pouvoirs, deux votes avec dix-sept bulletins, victoire du Village puis démarrage d’une nouvelle distribution. Durée observée : **137,73 s**.

Les deux fenêtres de parole ont respecté le minimum serveur de trente secondes, sans raccourcir la règle de production. Le relais vocal a accepté **deux fragments PCM synthétiques** pendant les votes ; un autre client authentifié a reçu ces octets. Les capacités indiquent le micro initialement coupé ; les éliminés écoutent sans pouvoir émettre, et un vrai POST audio d’éliminé est refusé. L’endpoint audio lui-même est fermé hors vote. **Aucun microphone, haut-parleur, permission système ou échange vocal humain n’a été essayé.**

Preuves : [rapport JSON distant](full-match-http-remote.json), [journal distant](full-match-http-remote.log). Ses 3 974 requêtes et 8 690 contrôles de transport incluent les polls ; ils restent distincts des étapes et actions de jeu.

| Résultat sémantique | En présence | À distance |
| --- | --- | --- |
| Clients HTTP simulés / humains | 10 / 0 | 10 / 0 |
| Nuits terminées | 2 | 2 |
| Pouvoirs acceptés | 11 | 11 |
| Votes de jour / bulletins acceptés | 2 / 17 | 2 / 17 |
| Déconnexion réelle puis reprise | Réussie | Réussie |
| Redémarrage serveur et reçu idempotent | Réussi | Réussi |
| Fin réelle | Victoire Village | Victoire Village |
| Nouvelle manche effectivement démarrée | Distribution | Distribution |
| Relais PCM scripté | Non utilisé | 2 fragments ; permissions vérifiées |

## Reproduire

```sh
python3 Server/NGame.Server/full_match_smoke.py --report /tmp/ngame-full-match-in-person.json
python3 Server/NGame.Server/full_match_smoke.py --no-build --mode remote --report /tmp/ngame-full-match-remote.json
```

Chaque invocation choisit un port loopback libre, utilise un dossier privé temporaire et arrête son processus serveur en fin de recette, y compris après échec. Les capacités brutes sont absentes des rapports/journaux et des fichiers de salle. Aucun hébergement public ni configuration utilisateur n’est modifié.
