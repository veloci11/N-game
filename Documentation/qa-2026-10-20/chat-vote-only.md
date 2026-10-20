# Nouveau contrat : chat pendant le vote uniquement

État vérifié le 2026-10-20T21:44:10.983700+00:00. Cette consigne remplace le contrat précédent de chat en salon/discussion/nuit. Les rapports antérieurs décrivent leur version de code et ne constituent pas une validation de ce nouveau comportement.

## API et confidentialité

Aucun nouveau champ DTO ni endpoint. Pendant `phase=vote` :

- Vivants : Village en lecture et écriture ; aucun accès à Observateurs.
- Éliminés : Village en lecture seule ; Observateurs en lecture et écriture.
- Meute : aucun canal de chat, même pendant l’appel des loups. Les pouvoirs privés du moteur restent inchangés.

Hors vote, `channels=[]` pour tout le monde ; lecture d’historique et nouvel envoi sont refusés. La fermeture du scrutin retire les droits immédiatement via la projection fraîche du serveur.

Un message déjà accepté peut être acquitté sans duplication après la fermeture : `ok=true`, `code=already_accepted`, `sent=null` lorsque le canal n’est plus lisible. Aucun texte ancien n’est renvoyé par cet accusé ; les droits restent fermés. Si son canal est encore lisible au scrutin suivant, le même reçu est retourné, sans nouveau message. Une intention ancienne inédite reste refusée, comme une fausse manche ou un contenu différent sous le même identifiant.

L’historique reste borné et distinct par canal, la limite d’envoi reste appliquée, et la reprise durable conserve identité/historique/déduplication. La politique vocale n’a pas été modifiée.

## Recette ciblée HTTP réelle

Commande :

```sh
python3 Server/NGame.Server/communications_smoke.py
```

Résultat : **913 vérifications/requêtes HTTP, aucun échec**, six identités réelles. Le compte inclut le polling de la fenêtre de vote distante réelle de 30 secondes, pas 913 situations utilisateurs différentes. Build .NET : **0 erreur, 37 avertissements nullable**. Python : compilation syntaxique et contrôle des espaces réussis. Pas de lancement de Unity.

Cinq situations couvertes :

1. Salon, distribution, nuit (y compris pour le loup), aube et discussion : aucun canal ; lire ou écrire Village/Meute/Observateurs est refusé.
2. Scrutin : le vivant reçoit son unique canal Village. L’éliminé reçoit Village en lecture seule et Observateurs ; son message d’Observateurs ne paraît jamais dans Village. Le vivant ne peut lire/écrire Observateurs et le loup n’a pas de chat Meute.
3. Envoi : un autre vivant et un éliminé peuvent lire le texte public exact. Un retry produit le même reçu, une modification du même identifiant est refusée, un auteur falsifié est refusé, un quatrième envoi trop rapide est limité.
4. Redémarrage pendant le vote : la reprise retrouve le même joueur, restaure trois messages publics et reconnaît le reçu déjà accepté sans dupliquer.
5. Fermeture du scrutin puis second tour : tous les canaux deviennent illisibles, y compris Observateurs. L’ancien reçu est acquitté sans contenu ; l’intention ancienne inédite est refusée. Au prochain scrutin, seuls les droits autorisés rouvrent et l’historique relisible ne comporte aucune duplication.

Le premier passage de développement a repéré un vote vers soi-même dans le fixture : la recette a été corrigée pour créer une égalité avec des cibles légales. Le résultat ci-dessus est celui du passage final sur le contrat intégrant Village en lecture pour les éliminés.

## Limites et intégration UI

Les champs, brouillons, clavier et dock ne sont pas validés par HTTP. Le parent et l’agent UI intègrent les nouvelles permissions. Le guidage et les messages du solo doivent désormais intervenir pendant le vote ; aucune source Practice/UI/Voice n’a été modifiée dans ce lot. Ce passage ne rejoue pas la précédente campagne de transport PCM et ne prouve pas un test audio sur appareil.

Preuve persistante : [http-chat-vote-only.log](http-chat-vote-only.log).

Empreintes SHA-256 :

```text
1d96ccc68d6c5062de19f16066745604855fcd1dd931eb2f8f05c122072fecc0  Assets/NGame/Core/Multiplayer/RoomCommunications.cs
145cd752b2e4fbd18514531e5387dda580c6b7023b97f2a3bf2e319126e63a25  Server/NGame.Server/communications_smoke.py
9647e79262a83456c19ff694547657bc48e7c5f4c098e238d68ff41ce1244bf4  Server/NGame.Server/bin/Debug/net8.0/NGame.Server.dll
```
