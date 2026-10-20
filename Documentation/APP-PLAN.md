# Plan de développement de N’GAME

Date : 17 octobre 2026. Statut : proposition de travail à valider par étapes.

**Mise à jour de référence :** le [plan d’exécution](PLAN-EXECUTION.md), les [vues et communications](COMMUNICATIONS-ET-VUES.md) et la [recette du joueur](RECETTE-JOUEUR.md) précisent les dernières demandes. Le présent document conserve le cadrage initial ; son ancien état de départ ne décrit pas l’ensemble du prototype local actuel. Huit rôles disposent maintenant d’un moteur local. Ce cadrage a été rédigé pendant l’étape de planification du 17 octobre. Mathis a explicitement demandé la reprise du développement en équipe le 20 octobre ; consulter [SUIVI-PRODUCTION.md](SUIVI-PRODUCTION.md) pour l’état courant des sources et des preuves.

N’GAME permet à un groupe d’amis de lancer des jeux de soirée sans cartes ni matériel. L’application fournit ce qui manque : règles, rôles secrets, cartes, chronomètres et scores. Les joueurs continuent à discuter et à se regarder dans la pièce.

La première production est le Loup-Garou dans l’univers de conte en papier choisi par Mathis. Le Président attend sa propre validation artistique. Les choix d’architecture et de lancement ci-dessous sont des recommandations ; ils ne constituent pas des fonctionnalités déjà développées.

## 1. Ce qui est acquis et ce qui reste proposé

| Sujet | État |
| --- | --- |
| Univers féerique en papier découpé et plié | Choisi par Mathis |
| Village nocturne du Loup-Garou | Référence visuelle approuvée |
| Monde animé, profondeur et rôle secret interactif | Intention retenue |
| Loup-Garou en premier | Priorité demandée |
| DA du Président | À retravailler plus tard |
| Même pièce et distance, priorité aux soirées ensemble | Choix confirmé par Mathis |
| DA des cartes de rôles présentées | Validée par Mathis ; règles à développer |
| Chat écrit et vocal distant uniquement pendant le vote de jour | Demandés ; à développer, selon le document de communication |
| Recette en se plaçant dans la peau du joueur | Demandée ; parcours et critères définis, essais à exécuter à la reprise |
| Application entière sous Unity | Recommandation pour la production |
| Loup-Garou connecté, chacun son téléphone | Direction retenue ; serveur et protocoles de meneur à développer |
| Français, portrait, pseudonyme invité | Valeurs de départ proposées |
| Tarification, nombre de jeux au lancement, narration vocale | Décisions ultérieures |

## 2. Usages et configurations de soirée

### Un téléphone pour le groupe

Ce mode fonctionne hors ligne pour les jeux qui s’y prêtent : mimes, mots à faire deviner, défis et scores d’équipe. Un téléphone circule ou reste posé au centre. Le contenu nécessaire doit être installé localement.

Un Loup-Garou avec un seul téléphone demande un fonctionnement spécifique : distribution par passage du téléphone, meneur humain et actions adaptées. Il viendra après le Loup-Garou connecté ; il ne sera pas présenté comme le même mode simplement privé de réseau.

### Chacun son téléphone

Chaque joueur conserve ses cartes, son rôle et ses actions privées. Un code ou QR permet de rejoindre la même soirée. La première version utilise Internet, même lorsque tous les joueurs sont dans la même pièce. Un vrai mode local sans Internet serait un chantier distinct à évaluer ensuite.

**Mathis a confirmé les deux configurations : même pièce et distance, avec priorité aux soirées ensemble.** En présence, la discussion reste dans la pièce. Un protocole de nuit avec yeux fermés et narration ou meneur permet les rôles d’observation physique, notamment la Petite Fille. Un mode où tous restent éveillés sur leur écran doit afficher ses différences de règles.

À distance, Mathis demande désormais du vocal intégré **uniquement pendant le vote de jour**, second tour compris. Il reste fermé pendant la phase Discussion et la nuit. Le chat écrit sert aux échanges autorisés ; ces deux fonctions ne sont pas encore implémentées. Les [vues et communications](COMMUNICATIONS-ET-VUES.md) remplacent l’ancienne proposition de canal vocal externe. Les rôles fondés sur les voisins, regards ou gestes nécessitent une convention déclarée ou une variante. L’ordre d’arrivée dans le salon ne remplace jamais silencieusement les places autour d’une table. Le mode distant doit compléter l’expérience de soirée, sans imposer ses compromis à la version en présence.

## 3. Parcours principal

1. Ouvrir l’application et retrouver éventuellement sa partie en cours.
2. Choisir créer une soirée, rejoindre ou jouer sur un téléphone.
3. Choisir un jeu adapté au groupe et au matériel disponible.
4. Renseigner un pseudo et un avatar public, sans inscription obligatoire.
5. Inviter avec un code lisible ou un QR ; afficher les erreurs de code, salon fermé ou salon plein.
6. Voir les participants, les règles choisies et les joueurs prêts.
7. Lire une explication courte ; garder les règles détaillées accessibles.
8. Jouer jusqu’à une vraie fin de partie.
9. Prendre sa revanche ou choisir un autre jeu en gardant le groupe.

Le créateur du salon organise la soirée. Il ne reçoit pas automatiquement les rôles ou les mains des autres. Une arrivée pendant une manche reste en attente ou en observation publique, selon les règles du jeu ; elle ne rejoint pas discrètement le moteur d’une manche déjà commencée.

## 4. Écrans de l’application

| Écran | Contenu et action utile |
| --- | --- |
| Chargement initial | Livre de papier, progression réelle si nécessaire, reprise de session |
| Accueil | Livre vivant, créer, rejoindre, téléphone partagé, partie en cours |
| Bibliothèque | Jeux disponibles, joueurs, durée indicative, connexion et téléphones nécessaires |
| Fiche de jeu | Principe, règles courtes, variantes, commencer |
| Création | Jeu, paramètres utiles et ouverture du salon |
| Rejoindre | Code, QR, pseudo, retour précis en cas d’erreur |
| Salon | Amis présents, statut de connexion, invitations, règles, prêts, lancement |
| Explication | Tutoriel court consultable à nouveau |
| Partie | Décor, phase, informations publiques et commandes privées autorisées |
| Résultats | Gagnants, explication du résultat, révélations permises, revanche |
| Réglages | Son, narration, vibrations, texte et mouvements réduits |
| Aide et interruption | Règles, retour de connexion, abandon et reprise |

Un compte pourra ensuite synchroniser préférences et achats éventuels. Il ne doit pas bloquer le premier jeu entre amis. Le chat de partie est demandé et prévu ; listes d’amis, messages directs et profils sociaux élaborés attendront un besoin démontré.

## 5. Système artistique

L’accueil prend la forme d’un livre dont les chapitres ouvrent sur les jeux. Les écrans pratiques utilisent des surfaces calmes de papier et de parchemin. Les parties occupent le monde illustré.

La référence est `../ArtSource/reference-approved.png` depuis ce dossier : écran central du Loup-Garou. Palette : bleu encre, forêt désaturée, papier crème, ambre et terre cuite. Titres en sérif de conte ; instructions longues et petits libellés en police simple. Les boutons, panneaux, cartes et icônes forment une bibliothèque commune.

L’avatar représente l’identité publique d’un joueur. La carte de rôle représente un secret temporaire. Un avatar public ne doit jamais changer d’apparence ou produire un son qui dévoile un rôle. Les illustrations animales de démonstration sont des portraits décoratifs.

Chaque jeu peut avoir son lieu et sa lumière. Le Président ne reçoit pas encore de décor royal définitif : les propositions précédentes n’ont pas été validées.

## 6. Production du village animé

Les onze PNG sources du village sont des assets de prototype : fonds nuit et aube, ciel nocturne sans lune, lune, nuages, loup, recto/verso de carte, premier plan, portraits et lanterne. Deux planches supplémentaires regroupent les seize rectos de rôles validés. Les fonds restent aplatis ; le loup, le premier plan et la lanterne sont séparés sur fond transparent. La première scène ajoute déjà halos indépendants, eau par shader localisé, brume et mouvements. Il faut poursuivre la séparation pour obtenir un diorama dont tous les éléments peuvent agir indépendamment.

1. Fixer la caméra et la composition maîtresse du village.
2. Préparer ciel, astres, montagnes, forêt, maisons, fenêtres, pont, eau et premier plan sur des couches compatibles.
3. Compléter les zones cachées pour éviter les trous lors des mouvements.
4. Séparer les parties du loup qui doivent bouger, avec des pivots cohérents.
5. Garder une géométrie unique entre nuit, aube et jour.
6. Ajouter ombres, brume, reflets localisés et petites lumières.
7. Comparer un plan fixe à la référence, puis tester le mouvement sur téléphone.

Les ambiances sont lentes : faible respiration du loup, rare mouvement d’oreille, brume, lanternes irrégulières et eau. La caméra suggère la profondeur ; textes et cibles tactiles restent stables. Les transitions peuvent être accélérées et simplifiées. Le jeu ne doit jamais attendre la fin d’une animation pour accepter une action valide.

Son : ambiance d’eau et de vent, papier des cartes, effets brefs et musique optionnelle. La narration est utile pour le meneur automatique, mais toutes les instructions restent lisibles. Aucun son diffusé dans la pièce ne doit identifier un rôle privé.

## 7. Premier jeu complet : Loup-Garou

### Progression de production, au-delà de trois rôles

Le moteur doit accueillir un catalogue extensible. Les trois rôles Loup-Garou, Voyante et Villageois forment uniquement le noyau technique P0 pour vérifier distribution privée, tours, votes et reprise. Ils ne sont pas la limite du produit. Les rôles classiques élargis P1, puis les interactions avancées, sont planifiés dès maintenant dans [ROLE-DESIGN.md](ROLE-DESIGN.md).

Seize rôles disposent d’une entrée de catalogue visuel correspondant aux planches validées : Loup-Garou, Villageois, Voyante, Sorcière, Chasseur, Cupidon, Salvateur, Petite Fille, Loup-Garou Blanc, Voleur, Ancien, Corbeau, Enfant Sauvage, Joueur de Flûte, Renard et Bouc Émissaire. Ce catalogue de données et d’illustrations ne signifie pas que leurs règles ou un multijoueur sont implémentés. Les autres rôles et modules officiels pertinents sont recensés dans le document de conception, sans promesse d’exhaustivité.

Les premiers essais visent un groupe dans la même pièce, chacun sur son téléphone ; le format principal est 8 à 10 joueurs. Le préparateur actuel explore 6 à 12, mais les références classiques annoncent huit joueurs au minimum. Les formats six ou sept et leur équilibrage restent des propositions N’GAME. La taille maximale du futur moteur doit pouvoir évoluer pour les rôles de grands groupes.

Exemple de composition à tester à huit : deux loups, une Voyante et cinq Villageois. Le nombre de loups selon l’effectif doit être réglé après de vraies parties. Les valeurs de durée et d’équilibrage ne sont pas des faits validés.

### Déroulement proposé

1. **Distribution et préparation.** Attribution aléatoire côté serveur. Chaque joueur reçoit uniquement ses secrets autorisés ; les loups identifient les partenaires prévus par les règles, sans forcément connaître leur spécialité. Les choix initiaux, comme Voleur, Cupidon et reconnaissance des amoureux, ont leurs fenêtres dédiées avant les actions normales.
2. **Nuit.** Le serveur construit l’ordre à partir des rôles présents. Dans le noyau classique : Voyante, loups, puis Sorcière si elle est en jeu. Les protections et autres pouvoirs s’insèrent selon leur module. La Voyante consulte la carte de rôle de la cible ; « camp seulement » est une variante explicite, pas la règle classique. La Sorcière reçoit son information après la décision correspondante des loups. Les écrans peuvent conserver une même ambiance de nuit sans exécuter tous les pouvoirs simultanément. Le consensus des loups, les échéances et leurs règles d’absence sont fixés avant la partie ; en mode Petite Fille à table, ils doivent pouvoir réagir à une observation remarquée.
3. **Aube et réactions.** Le village change de lumière. Le moteur résout les effets, les décès et les fenêtres de réaction, dont Chasseur, amoureux et succession éventuelle. Le dévoilement d’une carte attend les pouvoirs qui doivent agir avant lui. Le serveur vérifie les victoires après les réactions pertinentes.
4. **Discussion.** Échanges entre amis et chat écrit autorisé selon les droits de la phase ; le vocal intégré reste fermé. La durée de départ de 90 secondes reste une proposition à essayer, distincte de la fenêtre vocale du vote. Voir [COMMUNICATIONS-ET-VUES.md](COMMUNICATIONS-ET-VUES.md).
5. **Vote.** Sélection privée jusqu’à clôture, puis publication simultanée des choix pour conserver l’information sociale d’un vote observable à table. Un scrutin durablement anonyme, l’abstention et un verrouillage définitif après confirmation sont des options N’GAME à nommer et tester. Le serveur valide les votants, les cibles et les poids de voix.
6. **Résolution.** Le module de règles traite la cible arrivée en tête, les égalités et les pouvoirs publics. Avec le Capitaine, tenir compte de sa double voix et du départage prévu. Sans pouvoir prioritaire, prévoir le nouveau vote entre ex æquo puis l’absence d’élimination en cas de nouvelle égalité. Bouc Émissaire, Idiot, Juge Bègue et autres rôles modifient ce chemin ; les décès déclenchent à nouveau les réactions autorisées.
7. **Victoire.** Référence classique : le village élimine le dernier loup, les loups le dernier Villageois. Le couple mixte et les rôles solitaires ajoutent leurs propres conditions. L’arrêt automatique à parité est une variante qui ne doit pas couper une réaction ou une possibilité de victoire encore active. Le jeu de règles versionné décide de la résolution, pas une condition générique sur le nombre de joueurs.
8. **Fin.** Résultat identique sur tous les écrans, révélation finale des rôles et revanche.

Les éliminés observent par défaut les informations publiques ; ils ne voient pas les secrets des survivants. Les seuls droits supplémentaires sont les réactions ou permissions explicitement accordées par un rôle. Les règles d’égalité, d’élimination, de parole et d’abstention sont affichées avant le lancement.

Le lot P1 développe les rôles classiques au-dessus du noyau, plutôt que de figer une application limitée à trois cartes. Chaque ajout doit définir l’ordre des effets, les morts simultanées, les informations transmises et les victoires. La Petite Fille est prioritairement conservée dans son protocole d’observation en présence ; une éventuelle variante distante reste distincte et expérimentale. Les bâtiments, événements et rôles avancés suivent leurs dépendances, selon [ROLE-DESIGN.md](ROLE-DESIGN.md).

### Interactions et interruptions

La carte locale est cachée par défaut. Relâchement, annulation du geste, changement de phase, perte de focus et arrière-plan annulent aussi les appuis en cours. Une alternative accessible à l’appui maintenu doit masquer le rôle lors des mêmes interruptions.

Le serveur garde les échéances. Une reconnexion récupère le même joueur et un état autorisé récent. Une action tardive ou rejouée n’est pas appliquée deux fois. Un joueur déconnecté garde sa place et suit les règles d’absence à l’échéance ; la partie ne s’arrête pas simplement parce que le créateur du salon quitte l’application.

## 8. Ordre des autres jeux

| Jeu | Place proposée | Travail particulier |
| --- | --- | --- |
| Mimes et mots en équipes | Premier complément hors ligne | Paquets de mots, équipes, temps, validation, manches et scores |
| Président | Deuxième grand jeu connecté | DA encore à choisir, mains privées, coups autorisés, rangs et échanges de manche |
| Bataille | Après le moteur de cartes, si intéressante à jouer | Égalités, paquets insuffisants, fin de partie et parties longues |
| Kems | Version ultérieure | Actions simultanées, concurrence sur cartes, signaux physiques et latence |
| Belote | Version ultérieure | Contrats, atout, obligations de jeu, équipes et comptage exact |
| Poker et Blackjack | Selon intérêt confirmé du groupe | Règles complètes, tours, pots et jetons de partie |
| Dessin et quiz | Extensions possibles | Outil de dessin ou contenu éditorial vérifié |

Première sortie conseillée : Loup-Garou complet et un jeu hors ligne en équipes. Le Président peut être ajouté à cette sortie si son rendu et ses règles sont prêts ; il ne doit pas retarder indéfiniment la première version fiable.

Chaque jeu traverse les mêmes étapes : règles écrites et variantes nommées, moteur testable, partie finissable, interface, habillage, tests avec un groupe. Aucun jeu annoncé comme disponible avant de fonctionner jusqu’à la revanche.

## 9. Architecture proposée

### Client Unity

Une seule application Unity pour accueil, salons et jeux. L’ancien projet Expo reste une référence historique. Le prototype utilise une scène persistante de services, une scène de livre/menus et des scènes de jeu chargées au besoin.

uGUI et TextMeshPro portent les interfaces. Les commandes stables sont séparées du décor animé. Les réglages, l’audio, les joueurs, les règles courtes, les invitations, les résultats et la revanche sont réutilisables.

```text
App/Core                  Navigation, préférences, audio, identité
App/UI                    Composants visuels et accessibilité
App/Party                 Salons, invitations, participants, reprise
Games/Shared              Joueurs, cartes, votes, scores et temps
Games/Werewolf/Domain     États, actions et règles indépendants du rendu
Games/Werewolf/View       Village, caméra, phases et cartes
Infrastructure            Connexion, messages et données locales
```

### Serveur de règles

Le serveur décide des rôles, cartes, actions autorisées, échéances et résultats. Les téléphones envoient des intentions, puis affichent l’état autorisé reçu. Un hôte joueur ne détient pas tous les secrets.

Recommandation de départ : règles en C# pur réutilisables et testables, service applicatif .NET, échanges de commandes et d’événements, stockage PostgreSQL. Supabase peut fournir la base et l’identité invitée après audit de l’existant. La couche de connexion restera isolée pour ne pas lier les jeux à un fournisseur.

Avant de figer ce choix, réaliser une petite preuve technique : deux clients Unity rejoignent un salon, reçoivent chacun un secret différent, votent, se déconnectent et retrouvent le bon état. Comparer la complexité réelle avec l’offre Unity si celle-ci simplifie ce parcours. Un service de regroupement ou de transport ne remplace pas la logique de règles et de confidentialité.

Pour chaque commande : identité vérifiée, identifiant unique, phase/version attendue et validation côté serveur. Pour la reprise : instantané public et données privées du seul destinataire. Les rôles et mains de tous les joueurs ne doivent jamais figurer dans un gros état public.

Les jeux ne nécessitent pas de synchroniser en permanence les arbres, la brume ou les positions des cartes. Le serveur transmet les faits de jeu ; chaque appareil anime localement.

## 10. Données et exploitation

Prévoir joueurs invités, salons, membres, parties, règles et versions, actions, état public, données privées et préférences. Stocker séparément ce qui peut être montré au groupe et ce qui appartient à une personne.

Les paramètres d’une manche sont figés au démarrage. Les codes expirent, les salons inactifs se nettoient et les invitations ne donnent pas accès aux secrets. Les journaux d’erreur ne contiennent pas les rôles ni les mains privées.

Enregistrer les erreurs et les interruptions nécessaires au diagnostic, avec une durée de conservation définie. Les sauvegardes et la restauration du stockage sont testées avant de dépendre du service en soirée.

## 11. Qualité attendue

- Portrait, petits et grands écrans, zones sûres et texte adaptable.
- Son coupé et mouvements réduits sans perte d’instructions essentielles.
- Cibles tactiles confortables et états exprimés autrement que par la couleur seule.
- Cible de fluidité à valider : 60 images/s sur les appareils principaux, mode allégé à 30 si nécessaire.
- Décors chargés par jeu, textures compressées, atlas adaptés, arrêt du travail inutile en arrière-plan.
- Profilage réel mémoire, chauffe, batterie et chargement avant de fixer les budgets de production.
- Tests de règles, des commandes invalides, des secrets, des doublons et de la reprise.
- Vérification réseau défavorable et vrais essais sur iPhone et Android.

Une animation réussie dans l’éditeur n’est pas une validation de performance sur téléphone. Une partie de démonstration locale n’est pas une partie multijoueur.

## 12. Jalons et critères de passage

| Lot | Livraison | Condition pour poursuivre |
| --- | --- | --- |
| 0 — Projet importé | Unity ouvre le projet, compile et lance la scène | Aucune erreur bloquante, ressources et polices importées |
| 1 — Village vivant | Nuit, rôle maintenu, aube et vote local | DA proche de la référence, essai sur téléphone, rôle toujours remasqué |
| 2 — Noyau de règles et catalogue | Cycle P0, commandes privées et registre extensible des rôles | Scénarios normaux, égalités et absences testés ; aucune carte simplement illustrée annoncée jouable |
| 3 — Connexion privée | Salon, invités, secrets, actions et reprise | Deux puis plusieurs clients, informations privées correctement isolées |
| 4 — Loup-Garou élargi | Rôles P1 validés et application jouable jusqu’à la revanche | Essais prioritaires en présence à 8–10, puis autres tailles ; distance vérifiée séparément, interruptions et résultats cohérents |
| 5 — Application et jeu hors ligne | Accueil, bibliothèque, réglages et équipes | Jeu hors ligne sans Internet, groupe réutilisable entre parties connectées |
| 6 — Bêta mobile | Distribution de test iOS/Android | Profils de performance, petits écrans, parties hors du groupe de développement |
| 7 — Sortie | Builds, présentation boutique, aide et suivi | Identité finale, crédits/licences, politique de données, parcours complet vérifié |
| 8 — Extensions | Président, rôles avancés, bâtiments et autres jeux | Chaque ajout passe les mêmes tests ; variante distante explicitée quand nécessaire |

Les dates seront estimées après la scène animée et la preuve de connexion. Les principaux coûts de travail sont le découpage artistique, les règles réellement finies et la robustesse réseau.

## 13. Répartition de l’équipe

| Mission | Responsabilité |
| --- | --- |
| Coordination produit | Cohérence, priorités, arbitrages, suivi des critères |
| Direction artistique et assets | Référence, couches, personnages, cartes et cohérence des jeux |
| Unity et mouvement | Scènes, matériaux, caméra, interface, interactions et performances |
| Règles | États, variantes, résolutions et tests |
| Serveur et sessions | Salons, identité, secrets, validation et reprise |
| Vérification | Tests appareils, réseau, confidentialité, ergonomie et comparaison visuelle |

Ces responsabilités peuvent être traitées en parallèle quand leurs contrats sont stables. Une même scène ou un même fichier n’a qu’un propriétaire à la fois. Les sous-agents ne valident pas leur propre résultat comme preuve suffisante : l’intégration reçoit une vérification distincte.

## 14. Économie et diffusion

Pour le prototype : aucun achat et aucune publicité. Pour la sortie, comparer une application payante, un achat de packs de jeux et des personnalisations. Recommandation produit : préserver un parcours simple pour les invités et éviter toute interruption publicitaire au milieu d’une partie.

Les prix et le modèle final seront décidés après essais de jeu et analyse des coûts réels de service. Les visuels, noms et illustrations finaux devront avoir leur identité propre et leurs droits de diffusion vérifiés.

Diffusion progressive : essais internes, groupe d’amis, bêta iOS/Android puis publication. Les objectifs à suivre sont le délai avant de commencer, les abandons avant le lancement, les parties terminées, les revanches, les déconnexions récupérées et les incidents. Ce sont des mesures d’amélioration, pas des résultats actuels.

## 15. Situation du dépôt au début du plan

Le dépôt `veloci11/N-game` accueille un projet Unity neuf. Seuls les illustrations et le cadrage produit choisis sont conservés. Le code est réécrit en modules Core et Presentation ; les tests et les scènes appartiennent à ce nouveau projet. Les vérifications courantes sont consignées dans le README et le rapport de validation.

Le prochain travail concret après installation de l’éditeur est le lot 0, puis le lot 1. Le prototype visuel doit conserver explicitement son statut de démonstration jusqu’à l’existence d’une vraie partie.

## Références techniques

Les règles et adaptations de rôles sont sourcées dans [ROLE-DESIGN.md](ROLE-DESIGN.md), à partir des livrets officiels Zygomatic et des présentations Asmodee. Les options proposées par N’GAME y sont distinguées des règles de référence.

- [Comparatif des interfaces Unity](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html)
- [Autorité serveur Unity](https://docs.unity.com/en-us/cloud-code/server-authority)
- [Sessions multijoueur Unity](https://docs.unity.com/en-us/mps-sdk/sessions-overview)
- [Autorisations temps réel Supabase](https://supabase.com/docs/guides/realtime/authorization)
- [Sécurité par lignes PostgreSQL dans Supabase](https://supabase.com/docs/guides/database/postgres/row-level-security)

Ces références décrivent les capacités des outils. L’architecture ci-dessus est une proposition adaptée à N’GAME ; aucune intégration serveur n’est encore validée.
