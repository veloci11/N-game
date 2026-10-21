# Revue indépendante jeu et direction artistique — 20 octobre 2026

Passe bornée en lecture seule des sources Unity et serveur ; seul ce rapport est créé. Références : `PLAN-EXECUTION.md`, `WEREWOLF-RULESET.md`, `RECETTE-JOUEUR.md`, `SUIVI-PRODUCTION.md`, README des 52 maquettes et planche approuvée `ArtSource/ApprovedBoards/backs-and-screens.png`. Aucun éditeur, test Unity ou appareil piloté par cet auditeur. Les numéros ci-dessous désignent les sources lues avant les corrections parallèles de la racine.

## Écarts nouveaux à arbitrer

### P1 — Une place déconnectée peut bloquer tout le salon sans commande de retrait

**Sources :** `Assets/NGame/Core/Multiplayer/WerewolfRoom.cs:218–233`, `:260–288` ; `Assets/NGame/Presentation/Game/RoomFlowView.cs:317–331`.

`CanStart` exige tous les joueurs connectés. Le refus serveur conseille « retire leurs places », mais aucun bouton hôte ni commande de retrait n’existe dans les sources Core/Server/UI parcourues. Si une personne ferme son application avant de partir du salon, les autres doivent attendre son retour ou abandonner leur salon. La succession d’hôte ne résout pas la place absente.

**Correction proposée :** retrait explicite par l’hôte d’une place hors connexion, uniquement avant distribution, avec confirmation et composition/préparations réévaluées. Ne pas supprimer automatiquement un siège de partie en cours. En attendant, retirer le conseil inexécutable du message.

**Acceptation :** sept personnes au salon, l’une ferme son client ; les six restantes peuvent retirer sa place, revoir la composition, se déclarer prêtes et démarrer depuis l’UI. Un retour tardif de l’accès retiré n’usurpe aucune nouvelle place.

### P1 — Le bouton de départ désactivé ne dit pas ce qui manque

**Sources :** `RoomFlowView.cs:280–285`, `:317–331`, `:635–640` ; `WerewolfRoom.cs:218–233`. Référence : recette J04.

Le serveur connaît les motifs de refus, mais le bouton désactivé n’envoie aucune commande et l’écran ne présente aucune raison. Le minimum de six personnes n’est pas annoncé dans cette aide. L’hôte novice avec cinq amis peut lire « Commencer la partie » sans savoir s’il manque une personne, une confirmation, une connexion ou une carte.

**Correction proposée :** afficher près du dock la première condition manquante issue d’un état autoritaire : effectif, personnes non prêtes/déconnectées, composition invalide. Conserver le bouton et son emplacement.

**Acceptation :** vérifier séparément moins de six joueurs, préparation manquante, déconnexion et composition incomplète ; chacun reçoit une explication précise sans console, API ou coaching.

### P1 de recette présence — Les yeux fermés n’ont pas encore de protocole d’appel exploitable annoncé

**Sources :** `RoomFlowView.cs:328–329`, `:640` ; `WerewolfRoom.cs:715–723` ; `Assets/NGame/Presentation/StorybookApp.cs:561–575` (ambiance). Les seules sources audio de présentation repérées sont l’ambiance et le vocal des joueurs ; aucun appel nocturne parlé ni écran de désignation d’un lecteur public n’a été trouvé.

Les consignes demandent les yeux fermés jusqu’à l’appel du rôle, mais cet appel est seulement affiché. Si tous les joueurs ferment réellement les yeux, personne ne peut connaître le prochain appel sans regarder l’écran. Le groupe peut trouver un arrangement externe ; l’application ne l’explique pas et ne doit pas présenter la Petite Fille comme une expérience physique déjà validée.

**Correction proposée :** expliciter avant préparation qui lit les annonces publiques et comment cette personne reste hors des secrets, ou fournir ultérieurement des appels publics sonores optionnels. Ne pas imposer silencieusement un joueur-hôte qui regarde pendant toute la nuit, car il gagnerait un avantage d’observation. Ce point demande une décision de protocole, pas une nouvelle DA.

**Acceptation :** huit joueurs en présence, novice compris, suivent une nuit entière sans ouvrir les yeux hors de leur droit et sans coaching ; le rôle de lecteur éventuel est annoncé avant distribution. Le mode silencieux ne prétend pas assurer seul cette expérience.

### P2 — Les gagnants individuels calculés ne sont pas présentés

**Sources :** `WerewolfRoom.cs:127–128`, `:634–643` ; `RoomFlowView.cs:357–360`, `:410–433`.

Le moteur fournit `winnerIds` et exclut les amoureux de camps opposés de la victoire de leur camp initial. La fin affiche seulement le camp et les cartes révélées ; elle ne consomme jamais `winnerIds`. Un amoureux mixte mort peut ainsi interpréter « Le village l’emporte » comme sa propre victoire. À l’inverse, un membre mort du camp gagnant ne voit pas clairement qu’il gagne aussi.

**Correction proposée :** résultat personnel et badge de gagnant fondés exclusivement sur `winnerIds`, plus explication publique du couple mixte après révélation finale.

**Acceptation :** membre mort gagnant, amoureux mixte perdant malgré victoire de son ancien camp, victoire du couple et égalité ; aucun gagnant déduit uniquement du rôle ou de l’état vivant.

### P2 — La cible précédente du Garde disparaît sans rappel nominatif

**Sources :** `WerewolfRoom.cs:377–379`, `:497–500` ; `RoomFlowView.cs:467–470`, `:511–520`. Référence : recette du Garde.

La restriction générale est écrite, mais la cible précédente est seulement exclue des identifiants autorisés. Aucun fait privé du Garde n’est conservé pour lui rappeler son choix. Après interruption ou avec 24 personnes, une absence dans une page peut être prise pour un défaut de liste.

**Correction proposée :** fournir au seul Garde « Protection précédente : X, indisponible cette nuit » ou une entrée indisponible motivée. Ne rien révéler aux autres rôles.

**Acceptation :** nuit suivante après reprise : le Garde identifie sans mémoire externe la personne indisponible et sa raison ; passer une nuit rend cette personne de nouveau admissible selon la règle.

## DA : contraintes conservées, rendu encore à regarder

La planche approuvée montre bien le papier découpé, le village, le bleu nuit, l’ambre, les dorures végétales et le dos commun. Les cartes gardent leur provenance ; les huit extensions illustrées ne deviennent pas huit rôles jouables par leur présence au catalogue. Les 52 maquettes restent des propositions, jamais une preuve d’intégration.

Les sources du dock reproduisent les deux emplacements prévus (`RoomFlowView.cs:134–137` ; principal 382×56, secondaire 382×48). Cela ne prouve ni leur géométrie finale avec zone sûre ni leur lisibilité sur appareil. Aucun écart visuel définitif n’est prononcé sans les captures actuelles de la racine.

Sur les captures finales, vérifier : village encore visible derrière les panneaux, carte au même centre dos/recto, illustration réellement lisible, texte nocturne et diurne contrasté, pas de texte tronqué à 24 personnes, micro et dock stables, aucune lumière ou animation publique révélant le joueur actif. Les nuages, eau, lune et lumières doivent rester attachés à la composition ; aucune lanterne flottante. Une capture fixe ne valide pas le mouvement.

## Déjà connus, hors nouvelle investigation

Clôture du vote par l’hôte après son propre vote, choix initial Sorcière, verdict trop bref/chronique, promesse de reprise après départ et micro fixe sont déjà attribués à la racine : pas de doublon d’implémentation. Aucun nouveau P0 établi dans cette passe. Noms identiques écartés comme défaut : `AddPlayer` refuse déjà les doublons sans tenir compte de la casse (`WerewolfRoom.cs:96`).

Les nombres de tests techniques ne sont pas une recette humaine. Restent ouverts : captures actuelles, appareils, vraie partie autour d’une table, confort des attentes, acoustique native, distance entre réseaux et statut réel de livraison.

## Relecture finale après intégration des corrections

Sources relues : `PublicNarrator.cs`, `RoomFlowView.cs`, `RoomFlowPublicPanels.cs`, `AppPreferences.cs`, `StorybookApp.cs`. Toujours aucune modification C# par cet auditeur, ni essai acoustique/éditeur. Cette section actualise les constats historiques ci-dessus ; elle ne les laisse pas ouverts par défaut.

**P1 initiaux traités dans les sources :** `RoomFlowView.cs:330–331` présente `startBlockedReason` ; `RoomFlowPublicPanels.cs:57–93` propose le retrait confirmé des places avant distribution ; `RoomFlowView.cs:338` explique le téléphone public de l’hôte et la solution du lecteur. Le chemin `remove-player` est maintenant présent. Ces corrections demandent leurs preuves de parcours, mais le manque de code initial n’est plus constaté. Le P2 des gagnants est également traité : `RoomFlowView.cs:373–375` affiche les identités exactes de `winnerIds`. Le rappel nominatif de la protection précédente du Garde reste non trouvé.

**Narration — invariants cohérents en lecture statique :** `PublicNarrator.cs:43–59` utilise manche/tour/phase/appel/hôte et un chemin de ressource fixe ; ni nom, cible, carte ni fait privé n’alimente l’audio. `:46–48` exclut distance, entraînement, non-hôte et client déconnecté ; `:55–56` refuse une annonce dont la durée dépasse la fenêtre restante. Changement de clé, désactivation, perte de focus, pause et préférence coupée arrêtent le son (`:45`, `:62–70`). Les préférences d’ambiance et de narration sont séparées (`AppPreferences.cs:25–28`, `:38–49`) : couper l’ambiance ne coupe pas silencieusement les appels. L’annonce du Chasseur découle de la phase publique de réaction ; aucune identité supplémentaire de titulaire n’est lue, donc aucune fuite nouvelle de secret n’est établie par cette lecture.

### P2 — Activer les appels pendant un tour ne fournit pas de retour sur le silence courant

**Source :** `PublicNarrator.cs:43–49`, `:62` ; réglage dans `RoomFlowView.cs:613` et `StorybookApp.cs:517–520`.

La clé du tour est consommée même lorsque la narration est désactivée. Passer de « coupés » à « activés » dans le même tour ne lit donc aucun appel. Cela évite un appel périmé, ce qui est cohérent, mais rien n’annonce au groupe que l’audio ne reprendra qu’au prochain appel. Même limite après une interruption au milieu du tour : il n’y a pas de reprise automatique de l’ancienne parole.

**Correction proposée :** préciser près du réglage « Au prochain appel ; la phase actuelle reste affichée ». Pour une reprise après interruption, présenter l’appel public courant et la nécessité d’un lecteur sans rouvrir de secret ni rejouer automatiquement un son ancien.

**Acceptation :** couper puis activer pendant une nuit ne fait croire ni à une panne ni à une répétition du tour ; le prochain appel audible n’est joué qu’une fois. Les tests de reprise vérifient séparément absence de son périmé et compréhension du groupe.

### P2 — Une succession d’hôte peut changer silencieusement la disponibilité de la narration

**Source :** `PublicNarrator.cs:43–49`, `AppPreferences.cs:28`, `RoomFlowView.cs:338`, `:614`.

Le droit de parler suit correctement le nouvel hôte, mais la préférence de narration est locale à son appareil. Si l’ancien hôte avait les appels activés et le successeur les a désactivés, la table devient silencieuse. La phrase initiale sur le « téléphone de l’hôte » reste vraie, mais le nouvel hôte ne reçoit pas de rappel spécifique lui indiquant qu’il assure désormais les appels ou doit désigner le lecteur.

**Correction proposée :** au changement d’hôte, état public clair « Les appels viennent désormais de ce téléphone » et, s’ils sont coupés, avertissement du lecteur requis. Conserver l’absence d’activation automatique du micro.

**Acceptation :** transfert pendant une nuit vers un appareil avec narration coupée : nouveau responsable identifié, consigne de lecteur visible, aucun appel privé ou doublon sonore. Cela ne demande pas d’imposer un réglage audio personnel.

**Conclusion de cette passe : aucun nouveau P0/P1 certain.** Les deux limites ci-dessus sont des P2 de compréhension lors des changements de réglage/hôte. La DA approuvée n’est pas modifiée par ces corrections ; son intégration, les transitions et la vraie écoute de table ne sont toujours pas validées par une lecture de code.


**Ajustement final demandé par la racine :** l’annonce de réaction `hunter.wav` utilise désormais « Une dernière action est attendue. », sans nom de rôle. Durée mesurée 1,741 s, PCM16 mono 24 kHz, pic −3 dBFS, zéro écrêtage. Le texte public reste ainsi générique même lorsque les cartes des éliminés sont cachées. Le script et le manifeste sont alignés ; les douze autres WAV n’ont pas été régénérés. L’absence volontaire de rejeu à l’activation en cours de tour est également écrite dans `NARRATION-PUBLIQUE.md`.

## Comparaison visuelle des trois captures finales

Images réellement ouvertes avec `view_image` : `Documentation/qa-2026-10-20/home-final.png`, `entry-final.png`, `lobby-final.png`, chacune en 430 × 932. Aucun nouveau P0/P1 visuel constaté dans ces trois états.

- **Accueil :** conte en papier conservé ; village, feuillages découpés, lune, nuages et reflets cohérents avec la planche approuvée. Palette bleu nuit/ambre/crème, typographie et commandes terre cuite cohérentes. La lumière visible est attachée aux maisons et au lampadaire du village ; aucune lanterne flottante visible sur cette image. L’entraînement est explicitement marqué « Avec des villageois simulés ».
- **Entrée :** six portraits animaux réellement visibles et distincts, sans carré blanc. Pseudo, code et modes sont lisibles ; « En présence » est confirmé par une phrase sous les deux modes. Les deux commandes du dock restent aux emplacements prévus. Le panneau laisse voir le village et n’introduit pas de nouvelle DA.
- **Salon :** portrait du joueur correctement affiché, code et copie du code lisibles, burger/chat/aide cohérents ; le motif « Il faut au moins six joueurs » est bien visible. Le bouton de démarrage apparaît désactivé et la préparation personnelle reste accessible dans la rangée secondaire. La lisibilité observée à une personne ne valide pas la grille à 24.

**P2 concret de découverte — contenu inférieur peu signalé :** dans le salon, le protocole de narration commence près du bord inférieur et se poursuit hors de la zone visible, derrière la limite du panneau au-dessus du dock. La capture ne présente pas d’indice évident de défilement. Les boutons d’organisation et le reste du protocole sont donc faciles à manquer, surtout quand le roster occupe davantage de place. Le contenu est défilant dans les sources : il ne faut pas qualifier ce cas de texte définitivement tronqué. Proposition : léger indice de défilement ou résumé du protocole avant le roster ; acceptation par un novice qui trouve l’organisation et lit la consigne de narration sans coaching avec six personnes au salon.

**P2 de texte mineur :** `lobby-final.png` affiche « 1 personnes · une carte chacune ». Accord à prévoir au singulier, sans effet sur les règles.

Ces captures ne montrent ni carte privée, action de rôle, aube, discussion, vote, verdict, menu de départ, chronique, clavier, petits écrans, zone sûre d’un téléphone ou texte système agrandi. Elles ne démontrent pas l’animation, l’audio, la confidentialité simultanée, la présence physique ou un parcours finissable. La DA de ces trois états est cohérente ; l’ensemble du jeu n’est pas déclaré visuellement recetté.

## Réévaluation prioritaire : vrai rendu en partie et entraînement

Mathis a refusé les textes de façade, le solo et le résultat d’animation. La bonne cohérence de palette des trois premières captures ne vaut **pas** conformité de la partie. Deux images supplémentaires réellement ouvertes : `Assets/NGameQA/distribution-final.png` et `role-held-final.png`, confrontées à l’écran central d’`ArtSource/reference-approved.png`. Les deux vues sont explicitement celles de l’entraînement ; elles montrent néanmoins le cadre `RoomFlowView` partagé avec la vraie partie.

### P1 — La carte et le village deviennent secondaires derrière deux panneaux de formulaire

**Preuve visuelle :** bloc horizontal brun au milieu haut, petite carte à gauche, résumé et bouton à droite ; sous ce bloc, grand panneau de consignes répétées. Le décor est surtout un fond qui transparaît. La référence centrale donne la priorité au village, au loup et à une carte substantielle au premier plan.

**Sources :** `RoomFlowView.cs:118–131`, `:356–360` ; `RoomSecretCard.cs:36–38`. Carte fixée à **124 × 186**, environ 5,8 % de la surface 430 × 932. Le panneau supérieur fait 382 × 202 ; le corps inférieur occupe encore plus de 300 px de hauteur. Le titre, le sous-titre, le résumé de carte et le paragraphe central répètent le même geste. Dans `role-held-final.png`, l’illustration Sorcière paraît être une vignette, pas le moment fort de la découverte.

**Correction ciblée :** pour la distribution et le coup d’œil privé, réutiliser les objets existants dans une composition centrée : carte autour de **220 × 330** (dimensions proposées à confirmer au rendu), même centre et même taille entre dos et recto ; retirer le large panneau latéral et le bloc de rappel répété pour cette phase. Conserver une seule instruction et le compteur de confirmations. Réserver la description détaillée au panneau privé volontaire. Le dock fixe et les droits serveur restent les mêmes. L’espace libéré doit laisser apparaître le loup et la perspective du village, pas être rempli d’un nouveau panneau.

**Acceptation :** captures dos/recto côte à côte : même silhouette de carte, illustration lisible sans zoom, village/loup visibles, une consigne principale, compteur compréhensible. Aucun secret affiché automatiquement. Vérifier ensuite au moins une action de rôle : cette correction de distribution ne suffit pas à valider toutes les phases.

### P1 — Les effets codés ne constituent pas encore une animation de carte convaincante

**Sources :** `RoleCardMotion.cs:90–104` ; `UICard.shader:72–76`, `:103–108` ; `RoomSecretCard.cs:113–118`.

Le mouvement général est un flottement de 3 px et une rotation de 0,65°. Le shader déplace un portrait aplati : amplitude principale 0,011 UV (environ 1,4 px sur une carte de 124 px), remous des potions 0,003 UV (environ 0,37 px). Le retournement dure 0,18 s. Ce sont des effets de surface et un petit basculement, pas la démonstration d’un personnage papier vivant ; les panneaux réduisent encore leur visibilité. On ne peut pas annoncer une animation satisfaisante à partir de PNG ou parce que `_Clock` avance.

**Correction ciblée :** agrandir d’abord la carte ; puis vérifier sur vidéo courte le mouvement de la Sorcière déjà ciblé par le shader : lueur des fioles localisée, mouvement identifiable du contenu, respiration discrète sans déformer le cadre ni le titre. Allonger légèrement le retournement (proposition 0,30–0,40 s) afin que le geste soit perceptible, sans conserver un secret après relâchement. Si l’attente porte réellement sur des éléments découpés articulés, le shader d’un portrait plat ne suffit pas : découper seulement les éléments nécessaires des cartes existantes, sans réillustrer la DA, en annonçant cette limite au lieu de qualifier un filtre d’animation détaillée.

**Acceptation :** boucle vidéo 8–10 s de la carte tenue, plus un retournement et un relâchement : effet reconnaissable à taille téléphone, aucune déformation des titres, masquage immédiat, dos commun sans indice de rôle. Comparer mouvement normal et réduit. Aucune réussite perceptive n’est déclarée sans cette observation.

### P1 — L’entraînement est une simulation multijoueur, pas encore un apprentissage solo

**Sources :** `PracticeRoomClient.cs:39–50`, `:144–167` ; `PracticeRoomCommunications.cs:25–36`, `:49–58` ; `RoomFlowView.cs:388–390`.

Le solo garde un salon de huit, les mêmes étapes de table, des bots qui soumettent après environ 1,8 s et une discussion qui avance après dix secondes. Le chat ne produit aucune réponse de bot dans cette couche. Les consignes invitent pourtant à respecter celles de « la table ». La mention « habitants simulés » est honnête sur le statut, mais elle ne donne ni objectif d’apprentissage ni retour sur une bonne/mauvaise compréhension. Les bots savent jouer leurs actions ; cela ne crée pas à soi seul un entraînement intéressant.

**Correction ciblée :** garder le moteur et séparer une courte progression guidée : découvrir sa carte, comprendre une attente normale, effectuer un pouvoir, voter, lire la conséquence. Remplacer les textes de présence par des consignes de simulation. Supprimer ou rendre volontairement franchissables les discussions sans interlocuteur ; présenter un exemple public de soupçon plutôt que laisser croire à une conversation réelle. Aucun bot ne doit obtenir de secrets interdits pour fabriquer une réussite du joueur. Ne pas présenter ce parcours comme une partie sociale complète.

**Acceptation :** un novice comprend ce qu’il doit apprendre et pourquoi il attend ; il réalise un choix puis lit son effet sans coaching. Tester aussi un rôle sans pouvoir nocturne. Aucun paragraphe ne lui demande de parler à une table inexistante.

### P2 — Mouvement du village : travailler la visibilité avant d’ajouter des effets

**Sources :** `VillageStage.cs:97–125`, `:216–257` ; `LivingSky.cs:67–91`.

Les nuages, lune indépendante, eau, éclairages ancrés et loup articulé ont bien des chemins de mise à jour. La lune et les nuages changent entre les deux PNG, ce qui constitue une différence d’état visible, sans permettre de juger le mouvement continu. Le village couvert par les blocs empêche d’apprécier précisément ces effets. Les nuages oscillent lentement (`sin(t × 0,095)`) ; la lune oscille aussi. Ne pas augmenter toutes les amplitudes à la fois pour masquer le problème de composition.

**Correction ciblée :** dégager la scène puis capturer dix secondes au même état : déplacement des nuages lisible, reflets contenus dans l’eau, lumières attachées aux fenêtres, respiration/oreilles du loup visibles au premier plan. Recaler uniquement les éléments qui restent hors champ ou cachés derrière la carte. Aucun ajout de lanterne flottante et aucune réillustration du village.

**Priorité d’exécution proposée :** 1) composition carte/village et répétitions ; 2) vrai parcours d’apprentissage solo ; 3) vidéo de carte et réglage des effets déjà présents ; 4) vidéo du décor et corrections locales. Ces propositions ne modifient pas le moteur de règles, les secrets ou le dock demandé. Les captures précédentes restent des preuves d’états précis, jamais une validation de DA/animation en partie.

## Nouveau rendu de carte et séquence courte après correction

Rendus réellement ouverts : `Documentation/qa-2026-10-20/role-revised.png`, puis frames **20, 45 et 65** de `/tmp/ngame-motion-fresh2/`. Le GIF livré `role-motion-revised.gif` contient 80 images, pour 7,89 s annoncées ; ce constat porte sur cette séquence de découverte du Loup-Garou et ne vaut pas contrôle des autres cartes.

**P1 de composition précédent traité dans ce rendu :** la carte de 220 × 330 devient le premier plan utile, son titre et son illustration sont lisibles ; les deux grands blocs ont disparu. Village, loup de scène, eau et lumières retrouvent une place réelle. Le titre est fonctionnel (« Découverte des rôles »), et l’entraînement précise de prendre son temps. La comparaison est maintenant bien plus proche de la hiérarchie de la référence centrale. Aucun nouveau P0/P1 visuel établi sur cette seule vue.

**Le mouvement n’est plus une simple hypothèse de code :** les trois frames montrent des positions différentes de la tête illustrée, des nuages et de l’eau. Un repérage simple de patches entre les frames 20 et 65 estime environ 9 px de déplacement horizontal d’un nuage. Sur la carte, le patch de tête se retrouve à environ 8 px horizontalement, contre environ 0 px pour le bord supérieur : il y a donc un changement local, au-delà du seul flottement de la carte. Cette estimation compare des textures déformées et éclairées, ce n’est pas une mesure biomécanique ni une estimation précise de l’angle de tête. Les yeux restent lumineux ; le changement de leur forme/lueur ne permet pas à lui seul de chiffrer un clignement ou un regard.

Dans une zone d’eau visible de 80 × 60 px (x330/y670), environ 7,4 % des pixels diffèrent de plus de 8 niveaux RGB entre ces deux frames ; dans une petite zone de ciel immobile témoin, aucun pixel ne change. Cela distingue un mouvement local de l’eau d’un simple changement global d’image. L’effet est subtil ; les PNG espacés ne prouvent ni fluidité, ni confort, ni perception humaine spontanée.

**P2 restant concret :** le compteur « 7 / 8 rôles confirmés », vers y658, se superpose aux roches/reflets et manque de contraste. Le nombre est facile à perdre dans la scène. Préférer un contour/ombre de texte ou une toute petite réserve locale, sans réintroduire un grand panneau. Critère : lire immédiatement le compteur dans la boucle, sans zoom et sans cacher le village.

Retournement dos/recto, relâchement immédiat, menus/interruption, autres rôles, mouvement réduit, autres appareils et expérience sociale restent à vérifier séparément. Ce rendu traite une part substantielle de l’écart carte/village ; il ne transforme pas tous les écrans et toutes les animations en éléments validés.

## Audit borné des contrats des huit rôles après dernières corrections

Lecture seule de `WerewolfRoom`, `RoomContracts` et `RoomFlowView`, plus ouverture réelle de `Assets/NGameQA/werewolf-private-reference.png` et `werewolf-night-reference.png`. Le panneau privé étouffé est déjà attribué à `ui_player` ; les images précédant son correctif ne constituent pas une preuve de rendu corrigé.

Aucun **nouveau P1** établi dans les contrats examinés : Cupidon demande deux vivants distincts, lui-même possible, et l’UI compte deux cibles ; soin/passage Sorcière demandent zéro cible, poison/deux potions une cible de poison, avec victime et réserves dans le privé ; les faits exacts de Voyante persistent ; alliés et soumissions des autres loups restent dans le panneau privé ; seules les actions des loups sont révisables ; le Garde reçoit maintenant un rappel nominatif de sa cible précédente ; le Chasseur reçoit son action de réaction malgré son état éliminé ; la Petite Fille a une action d’observation physique sans cible ni identité numérique de loup, en présence seulement. Les observations `submitted=true` ne deviennent pas des actions envoyables : titre/description/fait sont rendus avant l’accusé générique.

**P2 de texte vérifié, indépendant du panneau privé :** dans `werewolf-night-reference.png`, « C’est à toi de jouer » et le bouton « Ouvrir mon action privée » coexistent avec « Les habitants simulés jouent. Ton action apparaîtra à ton tour. ». `RoomFlowView.cs:427–429` teste l’action pour le titre mais choisit le sous-texte d’entraînement sans tester cette disponibilité. Correction : sous-texte « Ouvre ton action privée, choisis puis confirme » quand une action est disponible, message d’attente seulement sinon. Critère : aucune promesse d’action future si l’action est déjà utilisable. Aucun effet erroné sur la règle n’a été trouvé derrière cette contradiction.

**Décision de portée :** rien dans cette passe n’ajoute un blocage aux huit rôles de la base locale au-delà de la correction du panneau privé et de sa vérification réelle en cours. La mention « prête localement » doit s’appuyer sur le parcours UI corrigé et une manche finissable, pas seulement sur les nombres de tests Core/HTTP. Les huit cartes d’extension ne sont pas jouables. Hébergement reporté, signature mobile, appareils et acoustique restent des travaux distincts ; ils ne doivent pas être inventés comme prérequis supplémentaires à la clôture de la base locale demandée.
