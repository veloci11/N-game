# Recette transport du chat — sources actuelles

Exécution terminée le 2026-10-20T21:29:37.077605+00:00. Aucun fichier C# modifié pour ce passage. Les scripts ont ouvert des ports locaux éphémères distincts du serveur de développement 8787 ; chacun a utilisé un dossier privé temporaire, puis arrêté uniquement son propre processus.

Build serveur : **0 erreur, 37 avertissements nullable**. Suite complète : **1048 vérifications HTTP**, suite ciblée : **219 vérifications HTTP**. Ces nombres comprennent les requêtes de polling ; ce ne sont pas autant de scénarios utilisateurs différents.

Commandes :

```sh
python3 Server/NGame.Server/communications_smoke.py
python3 Server/NGame.Server/communications_smoke.py --no-build --stale-only
```

## Cinq situations concrètes vérifiées

1. **Écrire au Village et reprendre un message.** En salon, un message comportant accents, emoji et balises textuelles conserve son contenu ; sa répétition avec le même identifiant crée une seule entrée. Changer son contenu sous le même identifiant est refusé. Un auteur forgé est refusé et les envois trop rapides sont limités. Sans authentification, les communications sont refusées. Le rendu des balises à l’écran reste un contrôle UI distinct.
2. **Parler la nuit sans fuite entre canaux.** Pendant l’appel des loups, le second loup lit le message privé du premier. Un villageois ne peut pas lire Meute, et le message privé ne figure pas dans Village. Écrire au Village pendant la nuit est refusé. Dès que l’appel se termine, Meute est fermée, y compris pour reprendre le reçu d’un ancien envoi.
3. **Continuer à observer après son élimination.** Un éliminé ne peut pas écrire aux vivants ; il peut écrire dans Observateurs. Un vivant ne peut pas lire ce canal. Ces droits dépendent de la projection de chaque acteur, pas du canal demandé par le client.
4. **Reprendre après redémarrage serveur.** La capacité de reprise retrouve le même joueur ; les trois messages de salon sont restaurés exactement, avec leur déduplication. Pendant une manche, les rôles distribués, l’identité de manche, la découverte privée de la Voyante et le second scrutin restent inchangés. Une nouvelle manche repart avec un historique vide. L’audio temporaire n’est pas restauré.
5. **Recevoir une réponse tardive pendant un changement de phase.** Un message Village accepté en discussion puis rejoué pendant le vote retrouve le même reçu et apparaît une seule fois. Une intention inédite de l’ancien tour, une fausse manche et un contenu changé sont refusés. Le vote distant conserve réellement sa fenêtre de 30 secondes malgré les confirmations ; l’ancien audio est refusé après la phase et au scrutin suivant.

## Ce que cette recette ne valide pas

Les brouillons, le clavier, la lisibilité, le défilement et la fermeture visuelle du panneau ne passent pas par HTTP. **Un défaut de brouillon a été signalé au parent pendant cette lecture** : `ResetChatDrafts` purge avant `CloseChatPanel`, qui peut sauvegarder de nouveau le champ de l’ancienne manche sous la nouvelle clé. Aucune correction C# faite dans ce lot ; correction et régression UI attendues côté parent.

Pas de test acoustique sur appareil, pas de validation humaine d’une discussion ni d’un build WebGL. Le PCM échangé est réel, sa réception sonore n’a pas été essayée ici.

## Protocole UI à deux clients proposé

Utiliser deux instances UI réelles A et B, la même salle et le même serveur, et compléter les sièges nécessaires avec des acteurs HTTP de recette.

- Au salon : A écrit un message, B le lit et répond ; vérifier auteur, contenu brut, accents, champ vidé seulement après reçu, retour au bas de liste et conservation de la position si on relit plus haut.
- Avant la nuit : saisir un brouillon Village sans envoyer ; fermer/rouvrir et vérifier sa conservation. Pour un loup autorisé, basculer vers Meute avec un autre brouillon ; revenir au Village sans transporter le texte privé. Choisir les identités UI selon les rôles effectivement reçus pour tester Meute ; un acteur HTTP non-loup confirme le refus d’accès.
- Pendant la nuit : vérifier le Village lisible mais envoi désactivé, le message expliquant ce silence, et Meute accessible uniquement à son appel. Garder Meute ouverte à la transition : le panneau se ferme ou se masque immédiatement, sans laisser son texte sous un écran public.
- Couper le réseau d’une instance après un envoi : conserver le texte tant que l’issue est incertaine ; rétablir le réseau et reprendre la même identité ; vérifier une seule copie et aucune republication dans un autre canal. Le cas précis de réponse perdue après acceptation exige une coupure contrôlée/proxy ; une simple coupure préalable ne suffit pas à le prouver.
- Ouvrir le chat avec un brouillon puis changer de manche : rouvrir, le champ et l’historique de la nouvelle manche doivent être vides. Après élimination, vérifier les droits et la présentation distincte d’Observateurs ; aucune migration vers Village.

## Preuves

- `http-chat-current-sources.log`
- `http-chat-stale-current-sources.log`

Empreintes SHA-256 des fichiers utilisés :

```text
f08a5401c1378ce8dddddce3f803a388d477bfd4258c004e268aa9aa1ca8853f  Server/NGame.Server/bin/Debug/net8.0/NGame.Server.dll
f27d096ed88e4ed8595373c8a9db8645a71817bb0157e76da7acae6bb08a7f84  Server/NGame.Server/communications_smoke.py
332a816c6d53422640244a86bd4f3f79b61919f0738ef30297889f8727767ff6  Server/NGame.Server/smoke.py
```
