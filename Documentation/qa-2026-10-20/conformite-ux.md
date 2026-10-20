# Revue indépendante de conformité UX — 20 octobre 2026

Relecture des sources de l’interface contre les [52 maquettes](../../ArtSource/MenuStudies/2026-10-17/README.md), le [plan d’exécution](../PLAN-EXECUTION.md) et la [recette du joueur](../RECETTE-JOUEUR.md). Relecteur : agent chargé des services, distinct de l’auteur des écrans. Cette revue ne valide pas son propre transport réseau ; les preuves réseau sont consignées séparément dans [le rapport serveur](../../Server/NGame.Server/verification-2026-10-20.md).

**Méthode et limite : lecture indépendante du code UI, pas une séance humaine ni une capture finale de tous les écrans.** Aucun éditeur lancé ou manipulé par cette revue. Les tests et captures pilotés par l’agent racine doivent être reliés séparément. La capture `Assets/NGameQA/home-2026-10-20.png` est antérieure aux dernières corrections de l’accueil ; elle ne vaut pas validation de la version finale.

## Écarts transmis à la coordination

Les statuts ci-dessous décrivent la détection et la correction annoncée, pas des succès de tests supposés.

| ID / priorité | Source et constat reproductible par lecture | Effet pour le joueur | Critère de correction / état |
| --- | --- | --- | --- |
| UX01 — P1 | `RoomFlowView.RenderBody` insérait initialement le micro après `RenderAction` dans la liste défilante ; la branche d’action soumise raccourcit ensuite cette liste | Micro difficile à trouver, qui change de place après le vote | Correction annoncée par l’auteur UI : micro créé une fois hors de la liste, position fixe ; test d’identité et position ajouté. Vérification finale à relier au rapport PlayMode |
| UX02 — P1 | `RoomFlowView.ConfigureDock` traite `action.kind == "vote"` avant `view.isHost && view.canAdvance` | Après son propre vote, l’hôte voit « Ton vote est enregistré » désactivé, même quand le serveur permet de clore après 30 secondes et tous les votes ; la commande n’est pas accessible | Donner une action claire à l’hôte autorisé sans déplacer le dock ; essai avant/après les 30 secondes et avec un vote manquant. Transmis, recontrôle requis |
| UX03 — P1 | `RoomFlowView.RenderAction` sélectionne implicitement `options[0]` ; chez la Sorcière, cette option est `pass`, sans cible requise | Deux appuis sur le bouton principal, ouvrir puis confirmer, peuvent passer le pouvoir sans intention choisie ; les options peuvent être plus bas dans le panneau | Demander un choix explicite de potion/passage, ou rendre l’intention sans ambiguïté et empêcher la confirmation avant cette compréhension. Rejouer soin, poison, deux potions et passage ; transmis |
| UX04 — P1 pour grands groupes et chaînes de décès | Verdict automatique après 5 secondes dans le moteur ; `RenderVerdict` liste les votes dans un panneau défilant, puis `RenderRecentEvents` ne garde que deux événements visibles | À 12–24 votants, le résultat complet ne peut pas être lu et revu ; une chaîne de plusieurs décès peut perdre des informations publiques utiles | Chronique ou dernier verdict consultable de nouveau, sans arrêter abusivement le jeu ni révéler des secrets. Tester quatre conséquences et 24 bulletins ; transmis |
| UX05 — P1 de compréhension du départ | La confirmation dit que place et rôle restent réservés « pour une reprise autorisée », mais `UnityRoomClient` efface sa capacité locale après `leave` réussi | Le joueur peut croire qu’il pourra revenir, alors qu’il ne dispose plus de son accès depuis l’application | Expliquer que le départ volontaire efface cet accès et n’est pas une simple coupure ; les autres continuent sans redistribution. Ou livrer une reprise volontaire réellement accessible. Transmis à la coordination |
| UX06 — P2 / recette à faire | `AppPreferences` expose ambiance et mouvements réduits ; pas de préférence de taille de texte. Plusieurs noms publics emploient encore des rectangles fixes | Le test « texte agrandi / noms longs » du plan ne peut pas être déclaré couvert par la seule lecture des sources | Essayer les plus petits appareils, noms de 24 caractères proches, accents et réglages de texte réellement disponibles ; corriger chevauchement ou ambiguïté observés |
| UX07 — P2 / contrôle tactile | Le nouvel accès micro annoncé occupe une ligne compacte près de l’horloge | Une position fixe est nécessaire, mais ne suffit pas à prouver le confort à une main | Vérifier la surface tactile réelle sur téléphone et ses distances avec les autres commandes, y compris clavier et texte agrandi |

Précision UX05 : le serveur permet encore une reprise explicite avec le token après un départ en manche. Le client efface cependant ce token et sa sauvegarde locale ; sans compte ou mécanisme de récupération, la reprise n’est plus accessible au joueur normal. Un départ en salon retire le siège. Une simple fermeture de l’app ou une coupure conserve au contraire la capacité de reprise. Il faut expliquer cette différence dans le dialogue.

## Correspondance des maquettes avec le produit actuel

Les repères sont des variantes d’un cadre commun, pas 52 scènes à créer. La présence d’un chemin dans les sources n’équivaut pas à sa validation sur téléphone ni à une fidélité artistique approuvée.

| Repères | Correspondance observée dans les sources UI | Limite / réception attendue |
| --- | --- | --- |
| A01–A04 | Accueil, catalogue de rôles, aide et formulaire pseudo/portrait | Une bibliothèque multi-jeux complète n’est pas requise pour le premier Loup-Garou ; vérifier que les anciens parcours de démonstration ne se présentent pas comme des parties |
| B05–B08 | Formulaire commun créer/rejoindre, choix présence/distance, salon et statuts prêt | Essai invité novice, code incorrect puis bon code, hôte prêt et composition modifiée |
| C09–C12 | Composition des huit rôles, durées de nuit/discussion, aide présence/distance | La fenêtre de vote est expliquée ; toute valeur affichée doit correspondre à la configuration réelle. Le texte n’ouvre pas le vocal pendant Discussion |
| D13–D16 | Distribution, maintien de carte, recto/verso au même emplacement, attente | Confirmer après avoir réellement découvert le rôle ; interruptions ; attente vécue de 30 secondes |
| E17–E20 | Meute, Voyante et Garde dans un panneau privé, faits consultables | Les faits persistants sont textuels ; une animation de résultat spécifique n’est pas prouvée par ce rendu générique |
| F21–F24 | Réserves de potion, soin/poison/les deux, deux cibles Cupidon, reconnaissance privée | UX03 ; vérifier la commande unique pour les deux potions et l’absence de saut global pendant reconnaissance |
| G25–G28 | Consigne physique Petite Fille, réaction Chasseur, aube et Discussion | Aucun équivalent numérique improvisé à distance ; UX04 pour plusieurs conséquences ; vraie table encore nécessaire |
| H29–H32 | Cibles et choix enregistré, progression, second tour et verdict | UX01/02/04 ; les choix d’autrui restent cachés avant clôture |
| I33–I36 | Éliminé, résultat du camp et revanche | Le dernier tir dû reste prioritaire ; même résultat sur tous les téléphones à essayer |
| J37–J40 | Burger, confirmation « Rester » prioritaire, erreur de connexion et reprise | UX05 ; retour système et mauvais appui à tester avec les vrais contrôles |
| K41–K44 | Seize cartes de catalogue, huit rôles jouables annoncés, fiche publique, aide et préférences | Pas de confusion entre illustration disponible et moteur actif ; UX06 |
| L45–L48 | Erreurs transport, entraînement explicitement simulé et fin sans victoire | Messages utiles plutôt que faux écrans de succès ; fin sans gagnant à parcourir réellement |
| M49–M52 | Création/connexion occupées, partie déjà commencée, revanche et révélations finales | Pas de fausse progression de chargement ; secrets et brouillons remis à zéro à la nouvelle manche |
| Compléments N/O | Vues dérivées par rôle et communication privée/publique présentes comme modules | Les planches N/O restent non générées ; il faut comparer plusieurs clients au même tour et capturer les variantes réelles |

## Huit rôles : éléments de parcours présents à essayer

| Rôle | Comportement UI observé | Point de réception |
| --- | --- | --- |
| Villageois | Attente sans commande nocturne, carte personnelle et vote | Il comprend qu’attendre est normal ; pas de signal public sur le rôle actif |
| Loup-Garou | Panneau privé, partenaires autorisés et choix modifiable | Deux loups et deux cibles ; autres joueurs sans vue privée supplémentaire |
| Voyante | Cible privée et découverte dans les faits personnels | Lire de nouveau le résultat après le changement de tour sans réagir une seconde fois |
| Sorcière | Réserves, soin, poison et deux potions ; besoin de cible adapté | UX03 ; aucun double effet ni passage involontaire |
| Chasseur | Action privée pendant réaction, malgré son décès | Dernier tir avant victoire, y compris après décès amoureux |
| Cupidon | Deux cibles distinctes et faits amoureux | Sélections sur plusieurs pages, lui-même si autorisé, pas de carte du partenaire divulguée |
| Garde | Liste des cibles autorisées et consigne de restriction | Comprendre l’impossibilité de répéter la cible précédente sans deviner un autre secret |
| Petite Fille | Consigne de table et indisponibilité en mode distant | Vraie fenêtre physique et absence de liste numérique de loups |

## Ce que cette revue ne permet pas d’affirmer

Aucun nouveau défaut P0 n’a été identifié par cette lecture UI ; cela ne signifie pas qu’une partie multi-appareils est validée ou qu’il n’existe aucun défaut. Les P1 transmis doivent être corrigés puis rejoués et reliés à une preuve. Les éventuelles corrections ultérieures ne sont pas automatiquement acquittées par ce document.

Restent distincts : suite de règles, tests PlayMode, partie HTTP, usage des vrais contrôles, capture esthétique, expérience d’un novice, deux téléphones, soirée de 8–10, grands groupes, micro et audio acoustique. Les tests verts d’un niveau ne valent pas réussite des autres. La fidélité finale au conte en papier demande les captures actuelles et une validation artistique ; les maquettes générées ne sont pas cette preuve.

Prérequis du parcours mobile : le client natif sans préférence utilise actuellement `127.0.0.1`. Sur un téléphone, cela ne désigne pas le Mac. Tous les participants doivent encore recevoir/configurer la même adresse LAN, ou disposer d’un serveur public et d’une adresse de production réellement préparés. Un simple code de salon ne résout pas ce choix de serveur. Tant que cette préparation n’est pas faite, ne pas présenter le premier lancement sur deux téléphones comme un parcours prêt à jouer sans accompagnement. Le packaging et les instructions LAN sont disponibles, pas un hébergement public actif.
