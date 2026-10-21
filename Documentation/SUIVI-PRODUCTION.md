# N’GAME — suivi de production et preuves

État du **20 octobre 2026**, dépôt `veloci11/N-game`, branche `codex/werewolf-playable`. Le correctif courant est sauvegardé au commit `0a74ebe82cd5f85151bbbe002c332f70229c3e9a`, SHA distant vérifié. Aucun serveur public déployé.

**Passe courante : reprise des fenêtres, interfaces de rôles et lucioles dans le décor.** Le shader `HearthWindows` module la lumière des vitres déjà illustrées, avec faibles variations et croisillons/murs préservés. Sprites de flamme et halos plaqués désactivés ; atlas quatre poses désormais historique. Reflets de rivière atténués et normalisés.

Les cartouches privés réagissent à la sélection et au reçu moteur, avec animations distinctes. Les lucioles sont ancrées dans le village par caméra/Canvas, sans panneau central ; un contact fait partir l’insecte vers la lanterne posée, sans duplication. Quatre cibles 44 points seulement, aucune information privée ni requête réseau par contact. Les protocoles yeux fermés/Petite Fille et écran sans Petite Fille restent explicites ; aucune garantie d’anonymat des gestes.

Recompilation sans erreur puis campagne complète PlayMode finale **98/98**, zéro échec/ignoré/inconclusif, **160,93 s**, incluant toutes les finitions : cartouche privé 84, chronomètre préservé, lanterne sur le chemin (.90,−1.65), halos de lucioles dégradés et Petite Fille sans faux reçu. Dernier EditMode complet **125/125** en **1,36 s**, zéro échec/ignoré/inconclusif. Deux [recettes HTTP complètes](qa-2026-10-20/full-match-http-validation.md) réussies, présence et distance, avec 10 sessions simulées chacune : 2 nuits, 11 pouvoirs, 2 votes/17 bulletins, victoire Village puis revanche. Reprise/redémarrage/chat et droits du relais PCM vérifiés ; aucun humain ou micro réel. Les anciens rapports et 91 cas par union restent historiques ; les campagnes actuelles sont 98/98 et 125/125. Huit PNG de rôles (fixtures), rendu public et frames intermédiaires des deux GIF réels Practice relus sans P1 visuel confirmé. Le GIF de nuit montre un clic luciole et une capture ; celui de Sorcière montre le choix Poison/cible puis le reçu du moteur local après confirmation. Build Mac final réussi : 0 erreur, 346 avertissements AI, 295 742 929 octets, 15 998 ms ; plist et signature ad hoc deep/strict vérifiés, processus natif exact 25993 lancé. Pas de recette UI native complète ni notarisation. Source sauvegardée au commit `0a74ebe`, SHA distant vérifié : [bilan courant](qa-2026-10-20/delivery-role-polish.md).

Huit rôles de base développés, huit extensions sans moteur, Président/Mimes en préparation. **Hébergement public reporté par Mathis.** Téléphones, groupe humain, acoustique et réception artistique non validés ; aucune publication complète de l’application annoncée.

## 1. Comment lire les états

- **Développé** : source ou asset présent et intégré au dépôt local ; cela ne prouve pas son utilisation réussie.
- **Testé techniquement** : scénario exécuté et preuve accessible, avec sa portée indiquée.
- **Vérifié visuellement** : rendu réel de la version concernée observé ; une maquette générée ou une ancienne capture ne suffit pas.
- **Non validé sur appareil** : essais iOS/Android, permissions, toucher, batterie ou audio réels encore nécessaires. Cette limite s’applique à toutes les nouvelles fonctions tant qu’un compte rendu contraire n’est pas lié.
- **À faire** : absent ou condition de livraison non remplie. **En cours** n’est jamais synonyme de réussi.

## 2. Équipe et règles de coordination

| Responsable | Mission actuelle | Limite / remise attendue |
| --- | --- | --- |
| Racine | Unity, intégration, captures, recette réelle, builds, arbitrages et livraison finale | Seule personne de l’équipe qui pilote l’éditeur et les tests Unity |
| Coordination (`ui_player`) | Suivi A à Z, dépendances, preuves, cohérence documentaire et ordre des corrections | Pas de source Unity modifiée pendant une campagne de tests ; demande de fenêtre d’intégration à la racine |
| Règles / console (`rules_engine`) | Moteur livré ; observation indépendante des consoles et résultats | Distinguer erreurs attendues des tests et exceptions réelles ; pas de commande éditeur concurrente |
| Services / conformité (`services_network`) | Packaging livré, audit indépendant du parcours face au plan et aux maquettes | Rapports HTTP persistants ; écarts UX classés P0/P1/P2 ; aucune approbation humaine simulée |
| Direction jeu / DA (`game_director`) | Revue indépendante du jeu et de la fidélité artistique | Examen borné des sources et captures actuelles ; pas de nouvelle DA ni modification concurrente des sources |

Un fichier conserve un propriétaire à la fois. Une campagne Unity bloque les modifications de ses sources jusqu’à la fin. Une correction est ensuite attribuée, intégrée et vérifiée sur le parcours concerné. L’ajout d’assets ou de bibliothèques n’est utile que s’il résout un écart identifié, avec provenance et licence.

## 3. Checklist de l’application, de A à Z

| ID / tâche | Propriétaire | État réel | Dépendance | Preuve disponible | Écart / prochaine vérification |
| --- | --- | --- | --- | --- | --- |
| A — Dépôt et base existante | Racine | Committé et poussé | Aucun | Snapshot `6b71fea`, branche `codex/werewolf-playable` | Sources sauvegardées dans le nouveau dépôt ; pas de déploiement |
| B — Cadre portrait et commandes fixes | Coordination → racine | Développé ; parcours PlayMode réussis ; non validé sur appareil | Données et UI | `RoomFlowView`, `StorybookApp`, tests de dock/burger | Vérifier rendu 360/390/430, zones sûres et vrai toucher |
| C — Accueil et entrée compréhensible | Racine | Développé ; accueil vérifié visuellement | B | Hub actuel et zone sûre réduite relus par coordination, tests de navigation réussis | Catalogue distinct de la fiche ; jeux futurs désactivés, reprise protégée ; parcours novice J01 restant à éprouver |
| D — Création, code, pseudo, portrait, salon, préparation | Services + UI | Développé ; nouveau routage et reprise protégée testés dans la campagne 65/65 | A–C | `smoke.py`, contrats et UI ; [rapport services](../Server/NGame.Server/verification-2026-10-20.md) | Essai réel créateur + invité ; erreurs et clavier sans guidage |
| E — Distribution et confirmation de découverte | Règles + UI | Développé ; parcours de découverte testé dans la campagne 65/65 | D | [Campagne actuelle](qa-2026-10-20/playmode-reference-final.json), scénario de distribution | Parcours de distribution réussi dans le second PlayMode ; absence de 120 s à expliquer clairement |
| F — Huit rôles de la base | Règles | Huit rôles développés ; audit moteur 67/67, parcours UI actuels testés | E | Tableau des rôles ci-dessous, 63 cas du moteur inclus dans les premiers 111 EditMode, puis 67 cas .NET actualisés | Ne pas appeler les seize cartes « seize rôles jouables » |
| G — Pouvoirs privés, cibles, potions, couple | Règles + UI | Correctifs vote/observateur testés dans Unity, nouveau rendu attendu | F + B | Données structurées, tests de règles, `PlayerWorkflowTests` | Une main, lisibilité de chaque pouvoir et fiches réelles des huit rôles à vérifier visuellement |
| H — Nuit commune et vues simultanées | Règles + racine | Développé ; projections privées testées techniquement | F–G | Tests de destinataires, appels conservés, Petite Fille | Capturer plusieurs points de vue au même tour ; contrôler absence de signal public de rôle |
| I — Aube, discussion et ordre des réactions | Règles + UI | Développé ; règles testées techniquement | H | Tests Chasseur/amoureux/victoire, neuf phases de snapshot | Vérifier temps de lecture et passage à l’action sur l’interface réelle |
| J — Vote, abstention, second tour et temps de parole | Règles + services + UI | Développé ; règles testées techniquement | I | Vote distant maintenu, clôture hôte après minimum, tests d’égalité | Vérifier clics pendant les votes des autres ; durées et confort social non équilibrés humainement |
| K — Verdict lisible et conséquences | Règles + UI | Développé ; moteur testé techniquement | J | Verdict structuré, fenêtre de lecture, dernier tir avant victoire | Vue réelle et lisibilité du résultat avant le prochain délai |
| L — Victoires, révélations et revanche | Règles + UI | Développé ; moteur et parcours UI testés techniquement | K | Victoires village/meute/couple/égalité, reset et nouveaux secrets | Entraînement → résultat → revanche réussi en PlayMode ; partie humaine à mener |
| M — Messages de vote Village / Observateurs | Services + UI | Dock développé, testé techniquement dans Unity et rendu relu ; ACL 913 HTTP réussis | Projections de rôle | [Rapport services](../Server/NGame.Server/verification-2026-10-20.md), `RoomCommunications`, tests UI de brouillon | Clavier réel, chronologie et compréhension des canaux ; aucune fuite de brouillon vers Village |
| N — Vocal distant pendant vote de jour uniquement | Services | Capture et relais natifs développés ; transport testé techniquement | M + J | `NativeVoiceClient`, `RoomVoiceRelay`, smoke des octets PCM et droits | Aucun vrai microphone, haut-parleur, casque, écho ou latence acoustique validé ; WebGL sans vocal natif |
| O — Reprise après fermeture et redémarrage | Services + règles + UI | Sauvegarde et accès développés ; snapshots et HTTP testés | Identité + état autoritaire | Capture/restauration, sauvegardes atomiques, `RoomResumeStore` | Retrouver réellement sa place après relancement client ; reprise UI sans créer une deuxième identité |
| P — Départ, absence, hôte et place réservée | Règles + services | Développé ; moteur et HTTP testés | O | `leave`, statut connecté, succession et tests de départ | Valider l’explication donnée au joueur ; rôle/siège non redistribué silencieusement |
| Q — Erreurs, reprises d’envoi et interruptions | Toute l’équipe | Développé ; plusieurs tests techniques, recette système à faire | D–P | HTTP invalides/doublons, tests pause/focus et callback tardif | Réseau instable, appel entrant, verrouillage et permission tardive sur appareils |
| R — Lisibilité et accessibilité | UI + conformité | Hauteurs et gestes adaptés ; réglage global de taille encore à faire, non validé sur appareil | B + G + M | Textes sans balises, hauteur adaptable, gestes privés successifs | Texte système agrandi, contrastes, grandes listes, clavier et VoiceOver/TalkBack à éprouver |
| S — DA, cartes et provenance | Racine | DA retenue ; assets présents, licences incluses | Références approuvées | `ASSETS-ET-DEPENDANCES.md`, cartes et 52 maquettes | Les maquettes ne sont pas des captures de produit ; huit extensions illustrées seulement |
| T — Mouvement du village et des cartes | Racine | Développé ; nouvelle version à vérifier visuellement | S | Ciel, eau, lumières ancrées, loup articulé, shader de cartes | Captures actuelles + preuve de temps qui avance ; fonds nuit/aube encore distincts, géométrie maîtresse à confirmer ; pas de lanterne flottante ni effet du dos révélant le rôle |
| U — Ambiance, narration et confort | Racine + direction jeu/DA | Ambiance/réduction développées ; 13 annonces françaises générées et fichiers mesurés | T + appels publics | [Narration publique](NARRATION-PUBLIQUE.md), manifeste et licences ; intégration hôte et tests PlayMode présents | Écoute française, confort acoustique et intégration à vérifier ; chauffe/batterie non validées ; mode silencieux expliqué |
| V — Tests, consoles et recette vécue | Racine + règles + conformité | EditMode actuel 116/116 ; campagne PlayMode unique 65/65 réussie | Fonctionnalités intégrées | [EditMode actuel](qa-2026-10-20/editmode-reference-final.json), [recette joueur](RECETTE-JOUEUR.md) | Console après build, vues de partie et essais humains encore requis |
| W — Packaging et builds | Racine + services | Scripts et descriptions de permissions développés | V | `PlatformBuildSupport` (descriptions Apple au build), notices, fichiers Docker ; permissions Android à vérifier sur le build généré | Mac antérieur compilé et lancé ; nouveau hub de référence encore à construire et ouvrir. UI native actuelle non validée. Web actuel et iOS/Android signés non validés |
| X — Partie distante réellement accessible | Services + racine | Packaging fourni ; hébergement public reporté par Mathis, hors lot courant | W + HTTPS + infrastructure | README serveur, Docker/Compose/Caddy | Aucun domaine ni service Internet permanent prêt ; téléphone natif neuf sur 127.0.0.1, adresse LAN commune à configurer manuellement ; essai entre deux réseaux distincts requis |
| Y — GitHub et livraison | Racine | Branche poussée, distant vérifié | V + revue des fichiers | Dépôt cible `veloci11/N-game`, snapshot `6b71fea` | Pas de service Internet ni de publication mobile déduits du push |
| Z — Recette de soirée et clôture | Coordination + racine | Planifiée, non validée humainement | V–X | [RECETTE-JOUEUR.md](RECETTE-JOUEUR.md) | Deux téléphones puis 8–10 en présence et groupe distant ; novice sans coaching, revue de tous les écarts |

## 4. Rôles : la règle existe-t-elle vraiment ?

Les tests techniques des huit rôles ne constituent pas encore une recette de leurs huit écrans sur téléphone.

| Rôle | État moteur | Preuve et limite |
| --- | --- | --- |
| Loup-Garou | Développé / testé techniquement | Alliés privés, désaccord et changement de cible ; usage meute à plusieurs téléphones à faire |
| Villageois | Développé / testé techniquement | Attente, débat et vote ; perception de l’attente à tester humainement |
| Voyante | Développé / testé techniquement | Carte exacte au seul destinataire, fait persistant ; action UI dans les scénarios PlayMode |
| Sorcière | Développé / testé techniquement | Soin, poison, deux potions en une commande, réserves et doublons ; écran réel des variantes à vérifier |
| Chasseur | Développé / testé techniquement | Dernier tir y compris mort amoureux, réaction avant victoire ; présentation à vérifier |
| Cupidon / amoureux | Développé / testé techniquement | Deux cibles, reconnaissance privée, décès liés et victoire mixte ; parcours à une main à vérifier |
| Garde | Développé / testé techniquement | Protection, précédente cible et exceptions poison/Petite Fille ; raison des cibles indisponibles à éprouver |
| Petite Fille | Développé / testé techniquement, présence seulement | Aucune identité de loup numérique ; observation physique exige une vraie table, pas une simulation seule |
| Loup Blanc | Illustré ; règles non développées | Extension, non activable dans le jeu de règles de la base |
| Voleur | Illustré ; règles non développées | Cartes de réserve et ordre initial à spécifier et implémenter |
| Ancien | Illustré ; règles non développées | Résistance et conséquences de mort à implémenter |
| Corbeau | Illustré ; règles non développées | Marque, poids des votes et variante à implémenter |
| Enfant Sauvage | Illustré ; règles non développées | Modèle et changement de camp à implémenter |
| Joueur de Flûte | Illustré ; règles non développées | Charme, reconnaissance et victoire personnelle à implémenter |
| Renard | Illustré ; règles non développées | Voisins/cercle et convention distante à définir, puis moteur |
| Bouc Émissaire | Illustré ; règles non développées | Égalité et futur électorat à implémenter |

Les extensions suivent le [document des rôles](ROLE-DESIGN.md). Le Président et les autres jeux attendent leur chantier propre ; aucune DA du Président n’est considérée approuvée.

## 5. Registre des preuves

Les preuves cutouts 70/70, anciens GIF et anciens builds ci-dessous sont historiques après la reconstruction intégrale. Le bilan courant est [delivery-layered-current.md](qa-2026-10-20/delivery-layered-current.md).

| Référence | Résultat / provenance | Ce qu’elle ne prouve pas |
| --- | --- | --- |
| [build-mac-cutouts.json](qa-2026-10-20/build-mac-cutouts.json) | Build de l’étape précédente Succeeded, **0 erreur**, 346 warnings AI, 19 281 ms, 212 989 857 octets ; plist valide, signature locale ad hoc vérifiée `--deep --strict` | UI vérifiée dans l’Editor, pas de recette native complète ni notarisation ; push non déduit du build |
| [Accueil de l’étape précédente](qa-2026-10-20/home-cutouts-final.png), [GIF](qa-2026-10-20/home-cutouts-final.gif), [aperçu local village](qa-2026-10-20/werewolf-cutouts-village-final.png) et [manifest](qa-2026-10-20/home-cutouts-motion.json) | Revue indépendante des deux PNG et image 50 du GIF ; 64 images réelles 430 × 932 sur 6,714 s, sans interpolation ; aucun P1 visuel bloquant confirmé | Qualité perçue non reçue par Mathis ; aperçu village explicitement local, pas une partie réseau |
| [playmode-cutouts-final.json](qa-2026-10-20/playmode-cutouts-final.json) | Ancienne campagne complète **70/70 PlayMode**, zéro échec/ignoré/inconclusif, **118,62 s** ; UI et nouveaux éléments séparés | Ni validation humaine du rendu ni essais sur téléphone ; pas de nouvelle campagne EditMode |
| [core-63-results.log](qa-2026-10-20/core-63-results.log) et [manifest](qa-2026-10-20/core-63-manifest.json) | Agent règles : runner .NET isolé, 63 réussites / zéro échec ; log archivé après exécution | Ce runner n’est pas l’éditeur Unity, un build ou un appareil |
| [core-67-results.log](qa-2026-10-20/core-67-results.log) et [manifest](qa-2026-10-20/core-67-manifest.json) | Agent règles : 67 cas .NET réussis après les quatre cas de retrait/motif de lancement ; rappel privé du Garde contrôlé | Le résultat Unity actualisé figure dans le rapport EditMode 116/116 |
| [editmode-revised.json](qa-2026-10-20/editmode-revised.json) | Version après retour de Mathis : **116/116** EditMode dans Unity | Ne valide ni animation visible ni ergonomie du solo |
| [playmode-revised-final.json](qa-2026-10-20/playmode-revised-final.json) | **45/45**, campagne complète unique sur la version révisée, zéro échec/ignoré ; accès infos fixe, brouillon nouvelle manche, entraînement guidé et E2E revanche passent | Batch de libellés après tests ; animation perçue, chat à deux clients UI et appareils restent à vérifier |
| [playmode-revised-initial.json](qa-2026-10-20/playmode-revised-initial.json) | **43/44**, vraie régression d’accès aux informations privées détectée et corrigée ; campagne finale 45/45 réussie | Le rapport final 45/45 couvre ensuite ce défaut corrigé |
| [editmode-final.json](qa-2026-10-20/editmode-final.json) | Racine : vrai test Unity 6000.6.6f1, **116/116**, zéro échec/ignoré, 0,97 s ; premier rapport 111 conservé séparément | Pas un test ergonomique humain ou acoustique |
| [build-mac-hub-motion.json](qa-2026-10-20/build-mac-hub-motion.json) | Build Mac historique : Succeeded, zéro erreur, 346 warnings AI, 170 507 891 octets, 15 173 ms ; plist/codesign local vérifiés pour cet ancien état | Obsolète pour le nouvel accueil de référence et les derniers correctifs du jeu ; pas de notarisation |
| [playmode-reference-final.json](qa-2026-10-20/playmode-reference-final.json) / [editmode-reference-final.json](qa-2026-10-20/editmode-reference-final.json) | Nouvelle DA +jeu : **65/65 PlayMode** (79,5 s), **116/116 EditMode** (2,52 s), zéro échec/ignoré/inconclusif ; vote et feedback nocturne inclus | Finition des bords contrôlée ensuite par 11/11 Hub ; derniers correctifs privé/clavier couverts par les rapports actualisés 66/67 +15/15 ; aucun appareil/humain validé |
| [playmode-private-second.json](qa-2026-10-20/playmode-private-second.json) et [retest 15/15](qa-2026-10-20/playmode-private-target-final.json) | Complet **66/67**, ciblé **15/15** en 1,64 s, **67 cas uniques couverts par union** ; privé Sorcière safe850 réussi | Pas une campagne unique 67/67. Fixture du test de raycast clavier resynchronisée, produit inchangé entre ces deux passages ; aucune validation téléphone |
| [playmode-reference-hub.json](qa-2026-10-20/playmode-reference-hub.json) | **11/11 ciblés** après finition des bords du grand livre | Ne rejoue pas les derniers correctifs privé/chat ; aucune réception utilisateur |
| [core-server-final-audit.md](qa-2026-10-20/core-server-final-audit.md) | Audit indépendant final : 67 cas Core hors Editor, 553 HTTP partie complète et 1 016 HTTP succession/reprise/voix, polls compris | Ni hébergement public ni conversation/acoustique sur téléphone |
| [lan-launcher-checks.log](qa-2026-10-20/lan-launcher-checks.log) | 28 contrôles `--check` du script `Tools/run_lan_playtest.sh` ; préparation uniquement, aucun serveur lancé | Aucun test à deux téléphones ni ouverture publique |
| [playmode-hub-motion.json](qa-2026-10-20/playmode-hub-motion.json) | Ancien accueil animé : **10/10 ciblés**, zéro échec/ignoré/inconclusif, 3,09 s | Les 56 globaux restent la campagne complète antérieure ; pas d’acceptation humaine ou de performance mobile prouvée |
| [hub-motion-final.png](qa-2026-10-20/hub-motion-final.png), [GIF](qa-2026-10-20/hub-motion-final.gif), [manifest](qa-2026-10-20/hub-motion-manifest.json) | 64 captures réelles sur 6,37 s ; PNG et frames 10/50 ouverts par coordination, mouvements locaux du livre/feuillage/icônes, commandes principales stables | Shader+papier UI articulé, pas un livre 3D ; qualité perçue et autres appareils non validés |
| [playmode-universal-hub.json](qa-2026-10-20/playmode-universal-hub.json) | Après le nouveau fond universel : **9/9 ciblés**, zéro échec/ignoré/inconclusif, 2,45 s ; mouvement puis réduction, Home/GameHome/Home | Les 56 globaux précèdent ce fond ; pas de campagne complète de 58 ni acceptation utilisateur revendiquée |
| [hub-universal-final.png](qa-2026-10-20/hub-universal-final.png), [zone sûre](qa-2026-10-20/hub-universal-safe-area.png), [création](qa-2026-10-20/hub-universal-create.png), [décor propre au jeu](qa-2026-10-20/werewolf-own-background.png) | Rendus réels ouverts par coordination : papier universel, encre lisible, aucune carte chevauchée en zone sûre 850, village absent du fond du catalogue mais présent dans le jeu | Petit pied discret ; expérience humaine et validation Mathis en attente |
| [playmode-hub-responsive.json](qa-2026-10-20/playmode-hub-responsive.json) / [editmode-hub.json](qa-2026-10-20/editmode-hub.json) | Campagnes actuelles uniques **56/56 PlayMode** (165,22 s) et **116/116 EditMode** (0,23 s), zéro échec/ignoré/inconclusif | Pas une recette humaine, acoustique ou mobile |
| [hub-final.png](qa-2026-10-20/hub-final.png), [zone sûre](qa-2026-10-20/hub-safe-area-final.png), [création](qa-2026-10-20/hub-create-final.png), [fiche](qa-2026-10-20/werewolf-home-final.png), [vote/messages](qa-2026-10-20/vote-chat-final.png) | Rendus historiques relus par coordination ; reprise/cartes séparées même en zone sûre 850 ; vote accessible avec dock développé, vrai clic scripté jusqu’au verdict | Phase préparée via moteur Practice ; aucune vraie conversation réseau entre deux UI ni expérience novice déduite de ces images |
| [chat-vote-only.md](qa-2026-10-20/chat-vote-only.md) | Nouveau contrat : **913 contrôles HTTP réussis**, attente réelle de 30 s ; vivant Village R/W, éliminé Village lecture seule + Observateurs R/W, aucun canal hors vote, Meute absente | Les anciens rapports lobby/Meute sont historiques ; rendu du nouveau dock et recette multi-clients UI encore distincts |
| [http-chat-workflow-current.md](qa-2026-10-20/http-chat-workflow-current.md) | Services : sources actuelles, 1 048 contrôles HTTP puis 219 ciblés de reçu périmé, zéro échec ; cinq situations détaillées, empreinte DLL fournie | Les brouillons/clavier et ergonomie à deux clients ne sont pas prouvés par ces requêtes |
| [verification-2026-10-20.md](../Server/NGame.Server/verification-2026-10-20.md) | Agent services : 555 contrôles HTTP, 1 044 chat/vocal/reprises, puis 219 ciblés après correction du reçu tardif, 42 pour le remplacement d’une place et 153 pour le retrait hôte ; commandes et sorties archivées | Les nombres incluent des polls répétés, pas autant de scénarios humains ; octets PCM ≠ microphone essayé |
| [playmode-initial.json](qa-2026-10-20/playmode-initial.json) | Racine : premier passage 26/29 ; trois vrais défauts MissingReference corrigés et scénarios réussis au second passage | Ce premier rapport reste une archive des défauts détectés |
| [playmode-second.json](qa-2026-10-20/playmode-second.json) | Racine : **38/39**, 112,02 s ; les trois anciens échecs passent, ainsi que entraînement → résultat → revanche. Seul le fixture de retrait échouait ; correction du tour/révision puis 7/7 ciblé | Rapport global historique, distinct du retest ciblé |
| [playmode-consolidated.json](qa-2026-10-20/playmode-consolidated.json) | **40/40 cas couverts**, union du second passage, du [retest 7/7](qa-2026-10-20/playmode-targeted-final.json) et de la [régression cache 18/18](qa-2026-10-20/playmode-portrait-regression.json). Cache corrigé avant ce dernier passage | Trois rapports consolidés, pas une campagne unique ; ce dernier passage ne rejoue pas tous les parcours |
| [console-before-fixes.json](qa-2026-10-20/console-before-fixes.json) | Archive des défauts avant correction, destinée à comparer la console fraîche | Ce n’est pas une preuve de console propre après correction |
| [Accueil](qa-2026-10-20/home-final.png), [entrée](qa-2026-10-20/entry-final.png), [salon](qa-2026-10-20/lobby-final.png) | Racine : rendus réels après correction du cache ; création de salle HTTP via UI et minimum de joueurs visible | Ces trois états ne montrent pas tous les pouvoirs, votes ou écrans de fin ; pas de validation tactile humaine |
| Captures/GIF du 17 octobre | Ancien rendu réel d’aperçu | Nouvelle interface et nouvelles fonctions du 20 octobre non couvertes |

Ne jamais copier un `Editor.log` complet ou une sauvegarde privée dans les preuves publiques : ils peuvent contenir des données étrangères à ce projet ou des accès. Garder des rapports bornés, sans tokens, rôles de salles réelles ni corps privés.

## 6. Priorités et prochaine répartition des quatre créneaux

1. **Historique de la précédente passe :** le second PlayMode est terminé (38/39). Le retest ciblé passe 7/7 après correction du fixture, soit 39/39 cas consolidés avant le nouveau test de cache. Le cache de sprites découvert au rendu est corrigé, avec 18/18 tests de régression et portraits visibles sur les nouvelles captures ; le premier Mac compile mais ne se lance pas (plist/DTD), corrigé dans le post-traitement. Le second build démarre réellement selon son rapport ; les changements produit demandés ensuite nécessitent de nouvelles vérifications. Règles observe la console quand un run a lieu. Services audite la conformité sans éditer l’UI ; direction jeu/DA examine le jeu et les rendus. Coordination tient les états, écarts et dépendances.
2. **Après le rapport :** corriger les P0/P1 avec un seul propriétaire par fichier. Racine arbitre l’intégration ; aucune nouvelle extension ne contourne une partie de base qui ne finit pas.
3. **Accueil :** les deux entrées de démonstration sont retirées de la navigation normale ; confirmer le rendu frais et rejouer J01. Le code d’aperçu reste utilisable dans les outils de développement.
4. **Rendu :** racine capture plusieurs états réels et vérifie gestes/dock/attente. Services confronte ces captures aux maquettes et remonte les écarts ; coordination vérifie que les corrections ne contredisent pas les droits et le plan.
5. **Livrable local :** build actuel, lancement réel, statut des permissions et scénario de reprise ; relecture sélective des fichiers puis livraison GitHub vérifiée.
6. **Plus tard, hors du lot courant :** hébergement HTTPS explicitement reporté par Mathis ; aucun déploiement public maintenant. Les essais appareils/novice restent distincts et ne doivent pas être déclarés faits à partir de tests locaux.

### Écart P1 déjà établi

L’accueil mélangeait de vraies parties et les anciennes démos « Explorer le décor » / « La Veillée ». Un joueur pouvait croire avoir créé une soirée alors qu’il regardait un vote fictif. La racine a retiré ces deux liens dans les sources ; les outils de démonstration restent disponibles pour le développement. Statut : développé et accueil actuel capturé ; les quatre vraies entrées sont conservées, sans les deux liens de démonstration. Critère : J01 ne mène plus accidentellement à un faux jeu ; le parcours réel reste identifiable sans explication extérieure.

### Défauts trouvés par les vrais événements UI

- **P0 — Exception après destruction d’un contrôle sélectionné :** le premier PlayMode échoue sur trois parcours à cause du faux `null` des objets Unity. Correction : vérification explicite de l’objet sélectionné et retrait de la sélection avant destruction des panneaux. Aucun message attendu ajouté pour masquer le défaut ; les trois parcours passent au second run de 39 tests.
- **P1 — Micro déplaçable après confirmation du vote :** la commande était dans le contenu défilant dont la hauteur change à la soumission. Correction : commande unique, indépendante du corps, dans une zone fixe près de l’horloge ; test d’identité et de position étendu. Position et identité stables vérifiées en PlayMode ; rendu et vrai toucher encore attendus.
- **P2 — Glyphe de sélection absent de la police :** symbole remplacé par un signe déjà pris en charge, avec état visuel conservé ; console fraîche observée sans erreur/avertissement le 20 octobre à 21:04:03 UTC ; contrôle final à refaire après les corrections suivantes.

### Conformité indépendante et corrections en cours

Rapport détaillé : [conformite-ux.md](qa-2026-10-20/conformite-ux.md), issu d’une lecture indépendante, sans revendication d’essai humain. La racine a repris la propriété des fichiers `RoomFlowView` / `RoomFlowChat` pour les corrections suivantes ; coordination n’y édite pas en parallèle.

| Écart | Niveau | État / condition de clôture |
| --- | --- | --- |
| UX02 — Hôte ayant confirmé son vote sans commande de clôture | P1 | Développé, confirmé en lecture indépendante : branche dédiée et confirmation au même tour ; test PlayMode ciblé réussi ; rendu attendu |
| UX03 — Passage Sorcière implicitement sélectionné | P1 | Développé, confirmé en lecture indépendante : choix null au départ, garde au dock et à l’envoi ; test PlayMode ciblé réussi |
| UX04 — Verdict de cinq secondes et décès multiples sans relecture | P1 | Développé, confirmé en lecture indépendante : chronique via burger, seulement les événements publics ; test PlayMode ciblé réussi ; rendu attendu |
| UX05 — Départ volontaire présenté comme reprenable | P1 | Copie corrigée en source et relue indépendamment : accès effacé au départ, simple fermeture distincte ; rendu attendu |
| UX06 — Noms longs et texte système agrandi | À éprouver | Hauteur adaptable présente ; réglage global de taille/Dynamic Type non développé, test multi-format et appareil nécessaire ; ne pas marquer conforme sur lecture seule |
| UX07 — Micro fixe mais cible tactile trop compacte | P1 corrigé en source / appareil à éprouver | Développé : bouton fixe avec zone raycast de 44 unités, visuel de 30 ; rendu et vrai toucher à vérifier |

### Revue jeu / DA indépendante

Rapport : [REVUE-JEU-ET-DA-2026-10-20.md](REVUE-JEU-ET-DA-2026-10-20.md). Revue indépendante de sources puis des trois captures actuelles accueil/entrée/salon, sans manipulation de l’éditeur. DA cohérente, six portraits distincts, minimum de joueurs et commandes lisibles ; aucun P0/P1 visuel sur ces trois états. Les vues de partie, mouvement, audio et clavier ne sont pas couvertes.

| Écart | Niveau | Dépendance / suite |
| --- | --- | --- |
| Invité absent qui bloque le lancement, sans retrait de place par l’hôte | P1 | Contrat/moteur développés et 153 contrôles HTTP ciblés réussis ; confirmation UI et maintien de l’hôte réussis dans le retest ciblé ; rendu à vérifier |
| Démarrage grisé sans raison visible ni minimum expliqué | P1 | Champ `startBlockedReason` livré et testé côté moteur/HTTP ; motif minimum visible dans la capture du vrai salon créé via HTTP ; compréhension J04 sans coaching encore à éprouver |
| Protocole yeux fermés sans lecteur/narration réellement organisé | P1 en présence | Treize annonces publiques françaises sont livrées avec mesures/licences ; narrateur hôte en présence intégré et six tests PlayMode réussis ; un AudioListener actif vérifié sur le rendu frais. Essai acoustique et mode silencieux avec lecteur organisé restent à vérifier ; Petite Fille à éprouver à table |
| Gagnants individuels `winnerIds` non présentés | P2 | Noms des gagnants autorisés désormais présentés en source sur l’écran résultat ; vérifier rendu, notamment le couple |
| Garde : précédente cible sans rappel nominatif | P2 | Rappel privé développé et testé : `WerewolfRoom.privateInfo` est consommé par `RenderPrivate` seulement ; vérification du rendu sur appareil encore ouverte |

La passe bornée est terminée, son créneau peut être réaffecté aux règles après arbitrage de la racine. Direction jeu/DA peut revenir pour la comparaison finale des captures.

### Narration publique et conversation vocale sont distinctes

Les [treize annonces françaises](NARRATION-PUBLIQUE.md) sont des assets synthétiques publics du jeu, générés localement et vérifiés pour format, durée, niveaux et absence d’écrêtage. Elles ne capturent aucun joueur et ne constituent pas le chat vocal. L’intégration développée et testée techniquement ne les diffuse que depuis l’hôte en présence, avec préférence de narration ; son arrêt aux interruptions/changements de tour est couvert par les tests PlayMode. Aucune écoute acoustique humaine n’est encore validée. Le chat vocal des joueurs reste fermé de nuit et limité au vote de jour distant.

### Prérequis d’accès depuis un téléphone neuf

Le client natif sans réglage commence sur `127.0.0.1` : un code de salon ne permet pas à lui seul de découvrir le Mac ou son adresse LAN. Pour l’essai local, le serveur doit écouter sur le réseau et tous les clients utiliser la même adresse du Mac, renseignée dans les réglages avancés. Le Web peut prendre son origine. Le packaging ne résout pas une URL de production encore absente ; une bêta mobile « ouvrir et rejoindre avec le code » n’est donc pas annoncée prête. Voir le [lancement serveur](../Server/NGame.Server/README.md).

### Règle de clôture

Le statut « jeu complet de A à Z » ne doit pas effacer les limites : une base locale finissable, une bêta mobile, un service Internet et une recette humaine sont des livraisons différentes. Chaque condition est cochée uniquement avec sa propre preuve ; les huit extensions et les autres jeux restent visibles dans le suivi tant qu’ils ne sont pas développés.

### Régression de rendu après tests

Le rendu frais après les passages PlayMode a révélé des portraits blancs : le cache `UITheme` retenait des sprites détruits. Le correctif de régénération après Unity-null est intégré. La régression détruit réellement un sprite puis vérifie sa recréation ; le passage confidentialité/cache réussit 18/18. Le nouveau salon montre les six portraits animaux. Cette observation empêche de qualifier les seuls tests de preuve visuelle complète.

### Réception visuelle bornée des écrans d’entrée

La direction jeu/DA a ouvert les trois PNG après correction du cache. Deux P2 restent signalés : les consignes de narration commençaient au bord inférieur du salon sans indice évident de défilement, et l’accord affichait « 1 personnes ». La racine a corrigé le singulier et ajouté la consigne d’hôte audible dans le sous-titre fixe en présence ; nouvelle capture requise pour clôture visuelle. Aucune recette humaine, acoustique ou sur téléphone n’en découle.

### Packaging Mac : compilation et lancement distincts

Le premier [rapport Mac](qa-2026-10-20/build-mac.json) annonce zéro erreur et 346 avertissements de packages AI Inference/Pipeline. Le lancement système a toutefois été rejeté : la resérialisation du plist produisait une DTD vide non acceptée par `plutil`. Le post-traitement retire désormais cette DTD optionnelle ; le [second rapport](qa-2026-10-20/build-mac-final.json) confirme compilation et processus natif démarré, plist valide et signature ad hoc vérifiée. Il mentionne aussi un avertissement de code non recompilé ; aucune recette native complète n’est revendiquée. Les textes et travaux d’animation ultérieurs ne sont pas couverts par ce build.

### Retour de Mathis : textes, solo et animation à reprendre

Mathis juge le remplissage narratif inutile, le solo insuffisant et l’animation non conforme à son attente. Les trois captures d’entrée ne constituent donc pas une validation finale du produit. Le nettoyage de `StorybookApp` remplace le titre et l’invitation par « Loup-Garou » / « Crée une soirée ou rejoins tes amis. », supprime les mentions marketing et rend les aides concrètes, sans déplacer les boutons. Nouveau rendu et nouvelle réception nécessaires. La racine reprend l’animation et les vues de partie ; le solo reste un entraînement simulé dont la qualité joueur est à revoir.

### Audit des premiers rendus de partie — P1 ouverts

La direction jeu/DA a ouvert `distribution-final.png` et `role-held-final.png`, puis comparé sources et référence approuvée. La carte de 124 × 186 représente environ 5,8 % du cadre : elle reste une vignette, alors que deux grands panneaux et les consignes répétées dominent la scène. L’amplitude du shader potion à cette taille est de l’ordre de 0,37 pixel ; le retournement de 0,18 s ne démontre pas le niveau d’animation demandé. Le solo reste une manche de huit bots, avec discussion de dix secondes sans interlocuteur ni objectif d’apprentissage. Ces trois P1 restent ouverts et ne sont pas invalidés par les tests techniques. Proposition à arbitrer : carte 220 × 330, consigne unique avec compteur, entraînement guidé utilisant le moteur et vidéo de 8–10 s montrant carte et décor. Voir [revue indépendante](REVUE-JEU-ET-DA-2026-10-20.md).

### Correctif de l’entraînement — réception à faire

Services a livré des consignes courtes `PracticeHint` (110 caractères maximum), la suspension du temps pendant les décisions humaines/distribution/discussion et hors focus, des nuits passives accélérées ×2, sept pseudos marqués « · bot », ainsi que des exemples et réponses génériques de Village explicitement étiquetés. Le client ne lit pas les cartes des autres. Quatre tests `PracticeGuidanceTests` sont ajoutés ; un harness avec stubs Unity et vrais moteur/client/chat passe 80 essais (quatre cas × vingt graines), selon le service ([résultat archivé](qa-2026-10-20/practice-guidance-harness.log), [provenance](qa-2026-10-20/practice-guidance-harness-manifest.json)). Ce n’est pas encore une preuve d’exécution Unity ni de pertinence pédagogique : intégration, rendu et vécu du joueur restent à vérifier. Le P1 solo n’est donc pas clôturé.

### Nouvelle exception observée pendant intégration

À 21:24:54 UTC, la console expose une répétition de `NullReferenceException` dans `PublicNarrator.Update` ligne 46, sans échec de compilation. [Extrait borné](qa-2026-10-20/console-narrator-reload.json). La racine identifie un rechargement des scripts pendant Play : les délégués/champs initialisés deviennent indisponibles. Play a été arrêté, une garde null de `preferences`/`clientSource` évite désormais le flood ; compilation puis session fraîche doivent confirmer le retour normal. Une console propre antérieure ne couvre pas ce nouvel état.

### Révision du rendu après retour de Mathis — sources gelées

La racine a développé une carte de 220 × 330 centrée pendant distribution et attente nocturne, retiré les grands panneaux et les consignes répétées, renforcé les mouvements UV des portraits/nuages/eau/feuilles et clarifié la pause pédagogique de l’horloge. Ces changements répondent aux P1 visuels mais ne les clôturent pas encore : la première capture GIF après rechargement ne constitue pas une preuve d’animation. Nouvelle session propre, tests de guidance et capture temporelle sont attendus. Aucun fichier C# ne doit changer pendant ces vérifications.

### P1 découvert en revue — brouillon au changement de manche

Services signale que `ResetChatDrafts` purge les brouillons avant `CloseChatPanel`, dont `SaveDraft` peut réinscrire l’ancien texte sous la clé du nouveau `matchId`. Défaut détecté en lecture, puis corrigé par fermeture avant purge ; un test de brouillon privé au nouveau match est ajouté, réussi dans la campagne complète 45/45. Critère : laisser Village ou Meute ouvert avec un brouillon, changer de manche, puis ouvrir les canaux de la nouvelle manche ; aucun ancien contenu ne doit revenir. La racine arbitre correction et test pendant la fenêtre appropriée ; coordination ne touche pas les sources gelées.

### Régression réelle après agrandissement de la carte

Le premier passage PlayMode de la nouvelle composition a trouvé un accès aux informations privées masqué. La racine fixe « Mes infos » au-dessus du dock (194 unités du bas), hors distribution, et réserve la zone de contenu jusqu’à 225 unités. Cette correction et le brouillon intermanches passent la nouvelle campagne complète 45/45.

### Réception de la composition et du mouvement révisés

La direction jeu/DA a ouvert [role-revised.png](qa-2026-10-20/role-revised.png) et les images 20/45/65 du [GIF de 80 images sur 7,89 s](qa-2026-10-20/role-motion-revised.gif). Sur cette vue, le P1 de composition est traité : carte 220 × 330 centrale, panneaux retirés, village/loup visibles, titre concret. Les régions comparées démontrent un déplacement local de tête (~8 pixels contre bord presque immobile), des nuages (~9 pixels) et une variation locale de l’eau par rapport au ciel témoin. Cela ne prouve ni fluidité sur appareil ni satisfaction humaine. P2 restant : compteur 7/8 peu contrasté sur roches/reflets vers y658. Les autres cartes, le retournement/relâchement et les parcours ne sont pas couverts par cette séquence.

### Nouvelle demande explicite : messages en bas pendant le vote uniquement

Mathis rejette l’écran de chat séparé. Le nouveau `RoomFlowChat` développé place une barre « Messages » à 247 unités du bas et un dock escamotable semi-transparent (alpha 0,76) de 230 unités, entre 270 et 500, avec historique défilant 130et compositeur 44. Messages alignés à gauche, pseudos distingués, aucun fond plein écran. Ouverture/lecture/envoi UI sont gardés par la phase vote ; sortir du vote ferme le dock. `IsChatOpen` décrit seulement son développement ; la racine adapte les actions publiques du vote pour rester accessibles et réserve l’espace des cibles. `ChatKeyboardInset` expose la hauteur du vrai clavier : les commandes gardent leur position normale, le clavier se ferme au repli/envoi, sans prétendre qu’un bouton physique reste visible sous le clavier système. Ce changement postérieur à 45/45 doit recevoir ses propres tests et rendus. L’ACL serveur vote uniquement est livrée et 913 contrôles HTTP réussissent selon le [rapport actuel](qa-2026-10-20/chat-vote-only.md) : vivant Village R/W, éliminé Village lecture seule et Observateurs R/W ; aucun canal hors vote, Meute absente. Les anciens rapports chat en salon/Meute sont des preuves de version antérieure, pas du comportement actuel.

### Entraînement adapté aux messages pendant le vote

Services déplace les exemples/réponses simulés exclusivement au vote. La discussion ne crée ni canal ni message et invite à « Ouvrir le vote » ; les consignes du vote expliquent les messages, avec lecture Village et Observateurs pour un éliminé. Un [nouveau harness .NET/stubs Unity](qa-2026-10-20/practice-guidance-vote-only-harness.log) passe 100 essais (cinq cas × vingt graines), [provenance](qa-2026-10-20/practice-guidance-vote-only-manifest.json). Le passage de 80 essais antérieur reste historique ; aucun test Unity de cette dernière adaptation n’est encore revendiqué.

### Tests du nouveau dock livrés, exécution Unity attendue

`PlayerCommunicationTests` contient onze cas : les deux fixtures privés sont désormais des éliminés dans Observateurs pendant le vote. Trois nouveaux cas contrôlent le refus hors vote même avec d’anciens droits lisibles, le dock bas semi-transparent et les coordonnées des commandes conservées, puis un vote effectif avec le dock développé via raycasts EventSystem et la fermeture à la phase suivante. Ces tests sont écrits mais aucun résultat actuel n’est encore annoncé. Le vrai clavier système et les gestes sur téléphone restent hors de cette preuve automatisée.

### Hub intégré : première réception visuelle actuelle

La coordination a ouvert le rendu réel `Assets/NGameQA/hub-current.png` (430 × 932). Aucun chevauchement entre cartes, reprise et dock ; titres, reprise et actions principales lisibles ; village/lune/papier cohérents sur cette vue. P2 : cartes Président/Mimes trop transparentes, statut « En préparation » peu lisible sur le décor ; conserver un fond dense et signaler l’indisponibilité par statut plutôt que diminuer l’alpha global. Le petit pied « 1 jeu disponible… » est discret mais non indispensable. Aucun P0/P1 visuel sur cette image seule ; aucune preuve du clic de reprise. La nouvelle campagne produit 54/55, l’unique fixture de reprise au premier frame est en vérification par la racine ; ne pas déclarer 55/55.

### Réception actuelle 56/56 et cinq captures finales

La racine a corrigé le chevauchement réel du hub à zone sûre 850, puis la campagne complète 56/56 réussit. La coordination a ouvert les cinq captures finales : pas de chevauchement, cartes futures lisibles et inactives, navigation cohérente, messages du bas semi-transparents avec commandes publiques dégagées. Au vote, la seconde ligne de cibles est partiellement coupée par le scroll ; indice de défilement à améliorer (P2). La racine confirme cible/confirmation réellement cliquées avec chat développé jusqu’au verdict, et reprise protégée sans changer la session ni créer Practice. Ces manipulations sont scriptées ; phase préparée par le moteur pour la capture, pas parcours novice ni échange réseau à deux UI. Le nouveau Mac et le push restent à recevoir avant clôture de leur statut.

### Hub précédent non accepté — nouveau décor universel

Mathis refuse le décor Loup-Garou derrière le catalogue général. Les captures du hub antérieur démontrent leur géométrie mais ne sont plus une direction visuelle acceptée. La racine possède `GameHubView` et `StorybookApp`, intègre un nouveau `HubBackdrop` clair en papier universel et réserve le village LW aux écrans du jeu. Textes du hub en encre sombre, boutons aux mêmes positions ; aucun changement global de palette du jeu. Asset et intégration en cours, nouveaux tests/captures requis, pas de push annoncé. Attention au routage : garder la caméra et l’AudioListener actifs si le stage les a créés comme enfants ; masquer le décor ne doit pas couper toute la sortie vidéo/audio.

### Fond universel livré localement, validation humaine en attente

Les quatre nouvelles captures universelles ont été ouvertes. Le catalogue et le sélecteur utilisent le papier clair, des objets de jeux et une encre sombre ; seule la carte Loup-Garou porte son illustration, sans village/lune comme fond du catalogue. La fiche du jeu retrouve son village nocturne. Aucun chevauchement dans le rendu de zone sûre 850 avec accès sauvegardé. Les neuf tests ciblés passent après les 56 globaux, avec mouvement puis réduction et aller-retour Home/GameHome/Home ; ce n’est pas une campagne unique de 58 tests. Le stage LW est masqué derrière un Canvas opaque sur Home, pas entièrement désactivé. L’acceptation de Mathis et l’ergonomie sur appareil restent à obtenir.

### Dernière demande : renforcer le mouvement du hub

Après un retour « pas mal » accompagné d’une demande d’animation plus aboutie, la racine ajoute `HubPaper.shader` : déformations localisées des pages et du feuillage, horloge et réduction du mouvement. Les symboles provisoires des jeux futurs sont remplacés par cartes en éventail/couronne et deux masques articulés sourire/triste, sans déplacer textes ou commandes. Ce sont des déformations shader et du papier UI articulé, pas un livre 3D. Nouveau rendu temporel, tests et build restent requis pour cette dernière révision. Le Mac du premier hub clair compile (zéro erreur, 346 avertissements AI), mais ne couvre pas ces derniers mouvements.

### Réception du mouvement final du hub

PNG et frames 10/50 réellement ouverts ; symboles cartes/couronne et masques reconnaissables. Les régions du livre, du feuillage et des icônes diffèrent, tandis que les pixels de zone libellé du CTA principal et du nom Loup-Garou restent identiques entre ces deux frames. Le titre à l’encre reste stable à quelques pixels de bord près ; l’arrière-plan bouge derrière lui. Le manifeste archive 64 captures réelles sur 6,37 s, sans interpolation. Dix tests ciblés réussissent, après les 56 globaux et les 9 ciblés antérieurs. Aucun livre 3D ni test de fluidité sur téléphone revendiqué. Les icônes des jeux futurs restent une présentation de catalogue, pas leur DA finale approuvée. Nouveau build Mac couvrant ces derniers mouvements encore attendu.

### Build actuel compilé ; gel documentaire avant Git

Le Mac couvrant le dernier hub animé compile : 170 507 891 octets en 15 173 ms, zéro erreur, 346 avertissements de packages AI. Plist lint et signature locale deep/strict sont vérifiés. La CLI annonce le lancement mais le PID enfant exact reste à confirmer ; aucune recette UI native ne découle de ce statut. Les captures et interactions observées proviennent de Unity. Dernier commit/push encore en attente de la racine. Coordination gèle ses sources et documents après cette mise à jour.

## Livraison sauvegardée

Le snapshot `6b71fea` a été poussé sur le dépôt demandé. Le build Mac final animé compile et son processus enfant 75938 a été observé au chemin `Builds/Mac/NGame.app/Contents/MacOS/N’GAME`. La CLI renvoyait le parent lanceur 75937 ; ces deux PID ne sont pas interchangeables. L’interface vérifiée demeure celle du GameView Unity. Le serveur local a repris sa sauvegarde après redémarrage sur les sources actuelles ; `/health` répond `ok`. Aucun accès ou rôle privé n’est ajouté aux preuves. Les paragraphes précédents conservent leur état historique au moment de chaque passe.

### Nouvelle passe D–G : gel avant réception

- **D / entrée :** nouveau hub suivant la référence de gauche, grand visuel et fond papier séparés, atlas de trois jeux ; boutons des jeux futurs toujours indisponibles. Les anciens PNG du hub sont des archives refusées ou dépassées, pas une acceptation du rendu actuel.
- **E / découverte :** distribution et maintien/confirme conservés ; nouveau contrôle nocturne local distinct des secrets, à vérifier en session propre.
- **F / rôles :** huit rôles de base seulement ; aucun nouveau rôle ou réglage moteur. L’audit indépendant [Core/Server](qa-2026-10-20/core-server-final-audit.md) confirme 67 cas Core hors Editor, 553 contrôles de partie HTTP et 1 016 de succession/reprise/voix, polls inclus.
- **G / ergonomie :** trois cibles par page de 44 unités minimum, pagination claire, consignes éliminées distinctes ; hauteur Messages adaptative (140–230 selon zone disponible), dock de vote inchangé. Tests sources livrés ; campagne Unity complète en cours, pas de résultat anticipé.

`NightCommonTapFeedback` est maintenant développé et branché par la racine : tout nouveau contact autorisé donne la même lueur fixe au bord, sans consommer les événements UI, son, vibration, score, réseau ou lecture des rôles. Focus/menu/modal/phase/réduction la coupent. Le tapotement est facultatif, peut se faire yeux fermés hors de l’appel de son rôle et préserve la consigne physique de Petite Fille. Ce feedback ne garantit pas de masquer les gestes ; une activité commune guidée à durée fixe n’est pas ajoutée au moteur. Trois tests du composant attendent la nouvelle campagne.

### Premier passage de la nouvelle finition : échec global détecté

La campagne de 65 tests produit 37 réussites et 28 échecs liés au nouveau découpage Sprite de `HubBackdrop` : arrondi flottant du bord supérieur dépassant la texture. [Rapport initial](qa-2026-10-20/playmode-reference-initial.json). La racine remplace les limites par des pixels entiers (papier de `floor(80 %)` jusqu’à la fin, découpage hero entier), puis recompile avant rendu et nouvelle campagne. Les tests vote et feedback nocturne font partie des 37 réussites, mais aucun succès global 65/65 n’est encore déclaré.

### Réception technique de la nouvelle DA et des lots D–G

Le nouveau passage complet 65/65 réussit après les 28 échecs de découpage Sprite corrigés ; les trois tests de lumière nocturne et les tests de vote/pagination/observateur passent. Le rapport 65 est distinct des 56 anciens globaux et 10 ciblés. La racine a ensuite adouci les bords du grand visuel par shader seulement : contrôle ciblé et rendu actuels encore attendus. Les choix artistiques de la référence de gauche sont retenus, sans inventer une validation du rendu final par Mathis. Hébergement serveur reporté explicitement : aucun déploiement public prévu. Le script LAN est seulement préparé, avec 28 vérifications de paramètres ; aucun essai appareil effectué.
