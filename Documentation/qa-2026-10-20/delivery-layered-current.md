# Village entièrement séparé — livraison locale sauvegardée

20 octobre 2026. Cette passe répond au rejet de l’animation de régions d’un PNG entier. Elle ne déploie aucun serveur public et n’annonce pas une application publiée ou réceptionnée de A à Z.

## Ce qui a changé

Le village actif est assemblé avec des bâtiments, pont, berges, sapins et chemin indépendants. Le ciel et la rivière sont géométriques ; reflets, lune, nuages et étoiles sont distincts. `VillageStage` délègue à `LayeredVillageScene` : les anciennes illustrations complètes de village, `VillageDetails` et `PaperRiver` ne servent plus de scène active.

Les **14 fenêtres** ont chacune une flamme de foyer animée à partir d’un atlas de quatre poses, masquée à son ouverture, et un halo fixé au bâtiment. Leurs variations chaudes de couleur et d’intensité sont déphasées ; les bâtiments ne pulsent pas. Les derniers placements et UV ont été réglés sur les illustrations réelles ; leur rendu final et les flammes ont été relus dans les captures actuelles. La réception artistique par Mathis reste distincte.

Le loup marche avec huit poses sur une rue commune pendant l’appel public de la meute. Garde, Voyante, Sorcière, Cupidon et amoureux ont des vignettes d’appel distinctes, identiques pour tous et sans cible ni identité privée. Leur horloge ne redémarre pas lors du rebinding d’un snapshot caché. Les pouvoirs ont un décor privé propre et un retour lié à la soumission autoritaire. Les portraits des cartes restent rigides, avec retournement, révélation et lumière ; tous les personnages ne sont pas découpés individuellement.

L’aide « ? » ouvre une fenêtre avec deux boutons identifiables : « Comment jouer » et « Voir les rôles ».

## Nuit et activité commune

Le salon distingue deux protocoles explicites avant la distribution :

- **Traditionnel / yeux fermés** : seuls les rôles appelés regardent, Petite Fille physique conservée, aucune identité de loup fournie numériquement.
- **Écran / lucioles** : activité locale commune, mêmes positions et rythme fondés sur le temps public, lanterne posée, cibles de 44 points, retour réduit si mouvements réduits. Aucun score public, bonus ou requête réseau par contact. Les appels restent ouverts jusqu’à leur échéance côté serveur, même après toutes les soumissions et malgré une tentative de clôture de l’hôte. La Petite Fille y est refusée.

La Petite Fille reste aussi refusée à distance. La narration tient compte du protocole. L’activité est facultative, s’interrompt aux masquages/focus/pause et ne garantit pas de cacher les gestes des acteurs autour d’une table. Un vrai groupe doit mesurer ce qu’elle améliore et ce qu’elle révèle encore.

## Finitions après le passage global

Le village reste désormais visible derrière la nuit sur écran (voile opaque retiré). Le loup a été agrandi et son trajet suit le pont ; la lanterne et son support sont placés ensemble à droite de l’activité. Ces changements de présentation sont postérieurs au passage global 90/91 puis 19/19 ciblés : les trois contrôles ciblés actualisés réussissent : 5/5 village, 20/20 vue joueur, 3/3 lucioles, zéro échec/ignoré/inconclusif. Ne pas présenter les 91 cas comme une nouvelle campagne complète après ces finitions.

## Rendus actuels et interactions

Rendus réels de Unity relus indépendamment : [aide « ? »](layered-help-question.png), [nuit publique](layered-wolves-public-final.png), [action privée des loups](layered-wolves-private-final.png) et [GIF de nuit](layered-night-final.gif), dont l’image 50 a aussi été relue. Aucun P1 ergonomique bloquant confirmé dans ces captures : boutons d’aide identifiables, cibles/pagination/confirmation privées visibles, village lisible derrière l’activité. La qualité artistique et le confort restent à recevoir par Mathis.

Le GIF contient **96 images réelles 430 × 932 sur 9,677 s**, durée GIF 9,710 s, sans interpolation ; [manifest](layered-night-motion.json). Ce sont des scènes d’**entraînement Practice avec habitants simulés**, affichées comme telles, pas une partie réseau ni une séance humaine. Le loup marche sur le pont dans les images du GIF.

Des clics simulés par le pointeur Unity sur les contrôles réels ont attrapé une luciole (`LocalCatchCount=1`), puis sélectionné et confirmé une cible privée ; le moteur a retourné `submitted=true` et le décor privé `ConfirmationVersion=1`. Cela vérifie cette chaîne d’interface dans Practice ; cela ne valide pas le toucher physique sur téléphone ni un groupe de joueurs. Les autres rôles disposent de tests et de décors, sans prétendre que ces captures de loups montrent leur rendu à tous.

## Assets et preuve technique

[Quatorze briefs de production normalisés](../Village-layered-asset-prompts.md), générateur ImageGen intégré sans clé API locale ; [dimensions, alpha et SHA-256 mesurés](../ASSETS-ET-DEPENDANCES.md). Neuf assets village/animation et cinq objets de pouvoirs. `bridge`, `path` et `pine` ont un maximum alpha réel de 254, les autres de 255 ; tous ont des pixels transparents à 0.

| Contrôle | État réel | Limite |
| --- | --- | --- |
| [Moteur protocole nocturne](night-protocol-validation.md) | **76/76 hors Editor**, zéro échec | Pas un passage Unity EditMode |
| [HTTP protocole](night-protocol-http.log) | **214 contrôles/requêtes locaux**, zéro échec | Polls compris ; serveur isolé arrêté en fin de recette ; aucun hébergement public |
| [Unity EditMode courant](editmode-layered-final.json) | **125/125**, zéro échec/ignoré/inconclusif, **1,68 s** | Tests Unity actuels ; pas une validation humaine |
| [Unity PlayMode complet](playmode-layered-initial.json) | **90/91**, **162,02 s** | Seul échec : ancienne assertion de 10 masques contre 14 ; fixture corrigée sans changement produit ; retest 19/19 ci-dessous réussi. Aucun passage unique 91/91 annoncé |
| [Unity PlayMode ciblé](playmode-layered-privacy-final.json) | **19/19**, zéro échec/ignoré/inconclusif, **17,28 s** | Union avec le global 90/91 : **91 cas uniques réussis**, vérifiés par leurs FullName ; pas une campagne unique 91/91 |
| Finitions visuelles postérieures | [Village 5/5](playmode-layered-village-final.json), 3,79 s ; [vue joueur 20/20](playmode-layered-player-final.json), 3,58 s ; [lucioles 3/3](playmode-layered-fireflies-final.json), 0,91 s ; zéro échec/ignoré/inconclusif | Après les dernières finitions ; pas une nouvelle campagne complète de 91 cas |
| Rendus actuels | Aide, nuit publique/privée des loups et GIF relus indépendamment ; 96 images réelles sur 9,677 s | Entraînement Practice ; aucun groupe humain, téléphone ou partie réseau prouvé par ces images |
| [Build Mac courant](build-mac-layered.json) | Succeeded (`build_6de2384b8f40`), **0 erreur**, 346 warnings AI, 295 727 649 octets, 17 995 ms ; plist lint et signature ad hoc `--deep --strict` vérifiés ; processus natif exact confirmé | Prêt localement ; pas de recette UI native complète ni Developer ID/notarisation. Les preuves UI décrites viennent de l’Editor |
| Git | Source sauvegardée au commit `9b45359b852b3260f314613f3bba1bf4e2b052d7` sur `codex/werewolf-playable`, SHA distant vérifié | Sauvegarde GitHub ; aucun déploiement public |

Le lancement natif courant est confirmé : processus enfant **19494**, chemin exact `Builds/Mac/NGame.app/Contents/MacOS/N’GAME`, vérifié par la racine. Cette preuve de processus ne doit pas être confondue avec une recette interactive de l’application native. Aucun serveur public n’est déployé.

Huit rôles ont un moteur ; les huit extensions illustrées restent sans moteur. Président et Mimes restent « En préparation ». Les essais sur téléphones, clavier réel, permissions et acoustique, performance, équilibrage, compréhension et discrétion physique ne sont pas validés. Une partie réelle à plusieurs clients puis une revanche restent distinctes des contrôles automatisés.
