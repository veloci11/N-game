# Catalogue des rôles et adaptation connectée

Date : 17 octobre 2026. Statut : conception et recherche ; aucune nouvelle règle n’est implémentée par ce document.

N’GAME doit pouvoir accueillir un catalogue riche de rôles. Les trois rôles du préparateur actuel servent à amorcer le moteur et les essais ; ils ne constituent pas la limite du produit. « Tous les rôles » n’est cependant pas un ensemble fermé : éditions, extensions et variantes communautaires diffèrent. Le périmètre étudié ici couvre le jeu classique, des extensions officielles et les nouveautés officielles de 2026. Un rôle ne sera affiché comme jouable qu’après son implémentation et ses tests de compatibilité.

**Choix confirmé par Mathis : les deux configurations connectées, avec priorité aux soirées ensemble dans une même pièce.** Les fonctions à distance doivent être prévues sans appauvrir l’observation et les échanges en présence. La DA des cartes présentées a également été validée ; cette validation artistique n’est pas une validation de leurs moteurs de règles.

Les noms ci-dessous identifient des références de règles à étudier. Ce document ne constitue ni une annonce de licence ni une reproduction du livret. Les **principes** sont des résumés des sources ; les **interfaces, priorités et protocoles connectés** sont des propositions N’GAME.

## 1. Distinguer les configurations de soirée

| Configuration | Ce que les joueurs font | Conséquence pour les rôles |
| --- | --- | --- |
| Même pièce, téléphones discrets, tous éveillés | Discussion en face à face ; actions nocturnes privées sur écran | Très adapté aux sélections et informations secrètes. L’espionnage physique de la Petite Fille n’a plus son fonctionnement classique. |
| Même pièce, nuit jouée les yeux fermés | Narration commune ; seuls les rôles appelés observent ou agissent | Conserve les gestes et l’observation. Un meneur humain est la référence pour animer et arbitrer ; une narration automatisée peut guider un protocole accepté par le groupe. |
| À distance | Appels vocaux/vidéo pour discuter ; téléphones ou clients séparés | Les regards, voisins physiques et gestes deviennent des règles d’interface explicites. La caméra ne remplace pas fidèlement une table. Aucun rôle ne doit dépendre d’une analyse des visages. |

Le choix du mode fait partie des paramètres du salon avant distribution. Il ne se déduit ni de l’ordre d’arrivée des joueurs, ni de leur localisation supposée. L’application affichera les rôles compatibles, ceux nécessitant une variante et ceux nécessitant un meneur.

Le catalogue visuel initial contient seize entrées, dans l’ordre des illustrations validées : Loup-Garou, Villageois, Voyante, Sorcière, Chasseur, Cupidon, Salvateur, Petite Fille, Loup-Garou Blanc, Voleur, Ancien, Corbeau, Enfant Sauvage, Joueur de Flûte, Renard et Bouc Émissaire. `RoleCatalog` conserve cet ordre avec `RoleArtIndex`, des résumés publics et des indications de faisabilité par mode. Tous portent actuellement `RulesImplemented = false` : afficher leur carte n’attribue aucun rôle et ne crée aucune partie en ligne.

## 2. Catégories et progression

- **Fidèle** : le principe peut être appliqué par le serveur et un échange privé, sans changer le pouvoir.
- **Adaptable** : il faut définir un cercle, un canal, une fenêtre ou une variante. Le changement doit être annoncé avant la partie.
- **Humain** : l’observation ou l’engagement social reste essentiel ; un bouton ne peut pas prouver le respect de la règle.
- **P0** : noyau de validation du moteur, avec Villageois, Loups et Voyante. Il s’agit d’une étape technique.
- **P1** : première offre classique élargie ; priorité aux rôles familiers et aux chaînes de conséquences.
- **P2** : transformations, informations de voisinage, rôles liés et victoires alternatives.
- **P3** : bâtiments, événements et interactions avancées ou récentes, après validation de leurs dépendances.

Ces lots décrivent l’ordre de travail proposé, sans date de disponibilité. La priorité d’un rôle ne prouve pas qu’il équilibre une composition de six joueurs. Les références officielles commencent généralement à huit personnes ; six ou sept restent des formats N’GAME à tester. [S0]

## 3. Matrice des rôles classiques

Références : livret classique [S1] et présentation de l’éditeur [S2]. Les colonnes d’interface décrivent une conception proposée pour N’GAME.

| Rôle ou fonction | Principe de référence | Action / information réservée à prévoir | Interactions à spécifier | Adaptation et priorité |
| --- | --- | --- | --- | --- |
| Simple Villageois | Déduction et vote | Son propre rôle ; informations publiques | Vote, décès, pouvoirs retirés | Fidèle · P0 |
| Loup-Garou | Attaque nocturne collective | Partenaires autorisés, cible collective ; aucun dossier complet des rôles | Consensus, cible invalide, absence, protections | Fidèle · P0 |
| Voyante | Consultation d’une carte | Sélection privée et rôle exact de la cible selon le jeu de règles choisi | Transformations, infection, rôle apparent / camp actuel | Fidèle · P0 |
| Sorcière | Deux potions distinctes à usage limité | Victime autorisée et réserves propres ; soigner / empoisonner / passer | Protections, usage simultané, causes de mort | Fidèle · P1 |
| Chasseur | Dernier tir à son élimination | Fenêtre de choix après événement déclencheur | Amoureux, autres décès, pouvoir neutralisé, fin de partie | Fidèle · P1 |
| Cupidon / amoureux | Couple lié dès le départ | Cupidon choisit deux personnes ; chacune connaît son partenaire, pas automatiquement sa carte | Mort liée et victoire du couple mixte | Fidèle · P1 |
| Voleur | Choix initial parmi des cartes réservées | Deux cartes de réserve accessibles uniquement au Voleur | Choix forcé éventuel, composition effective, nouvel éveil | Fidèle · P1 |
| Petite Fille | Observation discrète au tour des loups | Aucune identité de loup offerte automatiquement à l’écran | Risque d’être vue et changement de cible des loups | Humain à table ; variante à distance · P1 à table, P2 expérimental numérique |
| Capitaine | Mandat public supplémentaire | Élection, poids du vote, départage et succession visibles | Peut se superposer à un rôle secret ; droits après décès | Fidèle · P1 |

La version classique consultée présente un tir obligatoire du Chasseur, tandis que l’article de présentation évoque la possibilité de ne pas tirer : il faut choisir un livret de référence et nommer l’option de variante, plutôt que fusionner implicitement les descriptions. [S1] [S2]

## 4. Extensions : protections, information et pouvoirs

Références : *Nouvelle Lune* [S3], *Personnages* [S4], *Le Village* [S5]. Le protocole d’interface est proposé par N’GAME.

| Rôle | Principe bref | Action / information réservée | Point difficile dans N’GAME | Catégorie et priorité |
| --- | --- | --- | --- | --- |
| Salvateur | Protection renouvelable sous restriction | Cible et historique personnel de protection | Restrictions consécutives et exceptions selon édition | Fidèle · P1 |
| Idiot du Village | Grâce assortie d’une perte de vote | Révélation publique au déclenchement | Éligibilité aux votes, autres causes de décès | Fidèle · P2 |
| Ancien | Résistance et perte de pouvoirs conditionnelle | Résistance conservée côté serveur | Distinguer les causes de décès et les pouvoirs encore actifs | Fidèle · P2 |
| Bouc Émissaire | Conséquence spéciale d’une égalité | Choix ultérieur des votants autorisés | Priorité sur départage, absence de votants, décès suivants | Fidèle · P2 |
| Joueur de Flûte | Charme et victoire indépendante | Cibles ; groupe charmé informé selon règles | Nouveaux charmés, survivants, condition de victoire spécifique | Fidèle · P2 |
| Villageois-Villageois | Identité initialement publique | Affichage public de sa carte d’origine | Une infection ne doit pas être dévoilée par cet affichage | Fidèle · P2 |
| Deux Sœurs | Reconnaissance mutuelle | Identité du binôme ; concertation seulement dans les fenêtres prévues | À distance, un canal permanent renforcerait le rôle | Adaptable · P2 |
| Trois Frères | Reconnaissance d’un trio | Identités du trio ; concertation encadrée | Déconseillé aux petits groupes ; combinaison avec Sœurs à tester | Adaptable · P2 |
| Renard | Sondage d’un trio voisin | Oui / non pour le trio, pas trois cartes | Cercle explicite ; survivants adjacents, historique du pouvoir | Adaptable · P2 |
| Montreur d’Ours | Signal lié aux voisins | Signal commun ; aucune localisation exacte donnée | Cercle, voisins vivants et infection | Adaptable · P2 |
| Ange | Objectif initial distinct | Rôle et fenêtre initiale propres | La partie ne commence plus par la nuit habituelle | Fidèle · P2 |
| Juge Bègue | Second vote déclenchable | Commande privée à usage limité | Moment du déclenchement et effets du premier vote | Fidèle · P2 |
| Chevalier à l’Épée Rouillée | Conséquence différée et directionnelle | Événement différé réservé au serveur | Cercle orienté, assaillants concernés et annonce différée | Adaptable · P2 |
| Comédien | Pouvoirs temporaires choisis | Réserve de pouvoirs et usages restants | Réutiliser les règles d’un pouvoir sans recopier son implémentation | Fidèle · P3 |
| Servante Dévouée | Reprise d’un rôle avant révélation | Fenêtre avant dévoilement ; nouveau rôle privé | Transfert, remise à zéro et exceptions d’états personnels | Fidèle · P3 |
| Enfant Sauvage | Transformation liée à un modèle | Modèle privé ; notification de changement autorisée | Moment du changement, partenaires nouvellement connus | Fidèle · P2 |
| Chien-Loup | Choix initial de camp | Décision privée définitive | Rôle de carte et camp ne sont pas le même champ | Fidèle · P2 |
| Loup-Garou Blanc | Objectif solitaire dans la meute | Identité réelle privée, action particulière | Ne pas donner sa spécialité à ses partenaires ; victoire propre | Fidèle · P2 |
| Grand-Méchant-Loup | Attaque supplémentaire conditionnelle | Action supplémentaire autorisée | Condition persistante, morts antérieures et protections | Fidèle · P2 |
| Infect Père des Loups | Conversion limitée d’une victime | Usage, cible et nouvelle appartenance | Pouvoir conservé, protection, meute actualisée, secrets historiques | Fidèle · P2 |
| Abominable Sectaire | Objectif lié à deux groupes | Groupe d’appartenance et objectif | Groupes publics définis volontairement ; aucune déduction de caractéristiques personnelles | Adaptable · P3 |
| Corbeau | Influence sur un vote futur | Cible privée, marque ensuite publique | Le livret *Village* le lie aux bâtiments ; sans eux, nommer la variante | Adaptable · P2 sans bâtiments, P3 fidèle au module |
| Pyromane | Destruction d’un bâtiment | Choix de bâtiment à usage limité | Aucun bouton « tuer » générique : logements et voisinage nécessaires | Adaptable · P3 |

Les trois rôles de voisinage nécessitent une représentation stable du cercle. Dans une pièce, l’hôte renseigne les places réelles et le groupe les confirme avant distribution. À distance, un cercle virtuel public est choisi comme convention. « Voisin » ne signifie jamais « compte arrivé juste avant ». L’interface doit montrer quels survivants sont pris en compte sans dévoiler leur camp. Les directions et recompositions suivent le module de règles retenu. [S4] [S5]

## 5. Bâtiments, fonctions et événements

*Le Village* ajoute une couche de fonctions publiques distincte du rôle secret : Fermier, Confesseur, Rebouteux, Institutrice, Boulanger, Barbier, Tavernier, Bailli, Châtelain et Vagabond. Leur intérêt tient notamment aux logements, immunités, votes, déplacements et pouvoirs publics. Il faut donc construire ce module ensemble ; les traiter comme dix cartes secrètes interchangeables serait incorrect. [S5]

| Groupe | Interface envisagée | Catégorie et priorité |
| --- | --- | --- |
| Confesseur, Rebouteux, Barbier, Châtelain | Pouvoir public et cible autorisée ; information confidentielle seulement au bon destinataire | Fidèle avec moteur de bâtiments · P3 |
| Fermier, Institutrice, Tavernier | Droits et restrictions de vote visibles ; transitions d’immunité | Fidèle avec module complet · P3 |
| Bailli et Vagabond | Plan public des logements ; affectation acceptée et place mise à jour | Adaptable à distance · P3 |
| Boulanger | Observation d’un réveil physique, plutôt qu’un renseignement automatique | Humain à table ; variante ou exclusion à distance · P3 |
| Gitane sans Philtre, Spirite et Garde Champêtre | Cartes, question autorisée, événement ou réponse limitée | Adaptable ; nécessite le module d’événements · P3 |

Les cartes Événement de *Nouvelle Lune* et les fonctions bonus de *Personnages* ajoutent d’autres règles et fenêtres. La présence d’un éliminé autorisé à répondre à une question précise ne donne pas à tous les spectateurs un accès général aux secrets. [S3] [S4]

## 6. Rôles officiels récents à inscrire au catalogue de conception

L’éditeur a présenté quatre nouveautés pour l’édition anniversaire 2026. Les principes suivants proviennent de sa présentation ; les interactions de production nécessitent encore l’examen du livret exact de cette édition. [S6]

| Rôle | Principe communiqué | Travail N’GAME à prévoir | Catégorie et priorité |
| --- | --- | --- | --- |
| Colosse | Riposte informée après une attaque des loups | Révélation limitée à cette fenêtre ; cible réellement autorisée | Fidèle après spécification complète · P3 |
| Singe Savant | Exploration privée avec décision de poursuivre et risque | Suite de décisions serveur ; arrêt irréversible, absence et reprise | Fidèle après spécification complète · P3 |
| Marionnettiste | Substitution, survie et restriction de parole | État révélé et droits de vote ; silence impossible à imposer dans la pièce ou sur Discord | Fidèle pour l’état, humain pour la parole · P3 |
| Puissante Mère des Loups | Neutralisation privée de pouvoirs au début | Pouvoir initial / actif séparés et notifications réservées | Fidèle après spécification complète · P3 |

Ce relevé n’inclut pas automatiquement les rôles inventés par d’autres applications, les variantes de forums ou toutes les autres familles de jeux de loups-garous. Leur ajout demanderait une fiche séparée, une source et un nom de variante. Une carte personnalisée ne doit jamais annoncer une règle automatiquement prise en charge sans moteur correspondant.

## 7. Petite Fille : la vraie décision de produit

### Le mécanisme de référence

Le rôle classique repose sur une personne qui observe discrètement pendant le réveil des loups, avec le risque d’être remarquée. Les loups peuvent alors changer leur victime pour elle. Il ne consiste pas à recevoir une liste de loups, une notification certaine ou l’historique d’un chat. Le livret comporte aussi des consignes de posture pour rendre l’observation possible. [Livret classique, p. 8, 13–14 et 20][S1]

### Même pièce : conserver le jeu d’observation

Proposition prioritaire : un **mode table**, où le groupe ferme les yeux au bon moment, puis les loups se réveillent et choisissent discrètement sur leur téléphone. La Petite Fille peut observer physiquement, sans bouton d’information supplémentaire. Un meneur humain peut conduire les appels et arbitrer ; une narration automatique est envisageable si le groupe accepte un protocole précis.

La fenêtre des loups doit permettre de changer de cible jusqu’à sa fermeture : verrouiller immédiatement le premier choix détruirait le risque associé à l’espionnage. Le serveur ne prétend pas détecter un regard. Les autres appareils restent discrets : aucun son privé, aucune vibration reconnaissable, aucun écran de rôle visible à distance.

Il faut tester la lumière des écrans, la position des téléphones, la durée des appels et l’accessibilité. Si tout le monde reste éveillé à regarder son écran, ce mode n’est plus fidèle à l’observation classique ; le salon doit l’annoncer et proposer une autre composition.

### À distance : ne pas fabriquer une fausse équivalence

Un flux vidéo découpé, une analyse des yeux ou un extrait du chat des loups ne reproduisent pas le mécanisme. Version fidèle : rôle indisponible dans ce mode. Autre option : une **variante N’GAME explicitement expérimentale**, choisie avant distribution.

Piste de test, non règle officielle et non choix définitif : une fenêtre d’espionnage volontaire, limitée à un usage, dévoile une seule identité de loup à la joueuse ; simultanément, son identité est dévoilée aux loups. Les deux révélations sont une décision serveur atomique. La fenêtre intervient assez tôt pour que la meute puisse réagir sur sa cible avant clôture. Aucun historique, aucune autre identité et aucun renseignement en cas d’appui annulé avant confirmation.

Cette piste introduit un coût réel d’exposition, mais sa valeur et son plaisir restent à mesurer. Elle change le rôle : elle doit porter un intitulé de variante et être testée avec les protections, potions, déconnexions et cas de fin de partie. Il ne faut pas annoncer « Petite Fille classique à distance » avant cet arbitrage. Le mode avec meneur humain dans une même pièce reste l’alternative fidèle de référence.

### Captures et communications externes

Un secret reçu peut être photographié ou partagé sur un autre canal. L’application peut masquer rapidement, éviter les notifications explicites et limiter ce qu’elle transmet ; elle ne peut pas garantir qu’une capture, un spectateur présent ou Discord ne divulguera rien. Les joueurs éliminés reçoivent par défaut les seuls faits publics, pas une carte complète des survivants.

Les salons amicaux reposent aussi sur une règle de groupe : ne pas montrer son écran, ne pas transmettre de capture et ne pas communiquer avec les éliminés hors des permissions de la partie. Si une fonction de spectateur omniscient existe un jour, elle doit être séparée, consentie et inaccessible aux joueurs actifs sur la même session ; elle n’est pas nécessaire à la première version.

## 8. Contrat technique pour accueillir ces rôles

La version actuelle de `VillageSession` est une maquette locale. Le moteur connecté doit être un composant distinct et autoritaire côté serveur, avec :

1. Une fiche versionnée par rôle : source, camp initial, pouvoirs, charges, phases autorisées, dépendances, visibilité, compatibilités et état de disponibilité.
2. Des attributs séparés pour carte d’origine, rôle actuel, camp actuel, pouvoirs actifs, amoureux, infection, charme, mandat public et causes de décès. Une transformation n’efface pas arbitrairement tout le joueur.
3. Un calendrier d’appels construit à partir de la composition. Les loups doivent avoir décidé avant que la Sorcière reçoive une information dépendant de cette attaque. Un même affichage de nuit pour tous n’impose pas une résolution simultanée des pouvoirs.
4. Un moteur d’événements : protection, attaque, conversion, soin, poison, décès, réaction, succession, rôle repris et victoire. L’ordre et les priorités sont écrits par version de règles ; ils ne dépendent pas de la vitesse des requêtes réseau.
5. Une projection propre à chaque destinataire. Un loup connaît ses partenaires autorisés, sans forcément connaître leur spécialité réelle. Une Voyante ne reçoit que son résultat ; le créateur du salon n’est pas omniscient.
6. Des commandes identifiées et rejouables sans double effet, avec auteur, échéance et phase attendue. Une reconnexion ne rend ni une potion ni un espionnage déjà consommé.
7. Une matrice d’interactions testée et des compositions vérifiées. La sélection libre peut signaler les conflits et séparer les compositions recommandées des expériences personnalisées.

Avant d’activer un rôle, tester au minimum : action normale, refus d’une cible interdite, absence, doublon, reprise, effet neutralisé, transformation, chaîne de morts et victoire alternative. Les combinaisons Sorcière/Chasseur/Ancien, Cupidon/réactions de décès, infection/pouvoirs conservés, Servante/états transférés et voisins/transformation demandent des scénarios explicites.

Le serveur doit résoudre toutes les réactions pertinentes avant d’annoncer une victoire. La règle classique des loups vise le dernier Villageois ; une fin automatique à parité est une variante de raccourci qui n’est pas sûre avec tous ces pouvoirs. Le vote classique est observable par la table ; dans N’GAME, révéler ensemble les choix à la clôture préserve davantage cette information qu’un scrutin durablement anonyme. Ces options doivent être nommées, pas cachées dans le moteur. [S1]

## 9. Décisions et livrables suivants

- Appliquer le choix confirmé : les deux modes connectés, avec priorité à la soirée physique. Tester séparément leurs protocoles et leur information disponible.
- Fixer le livret et les variantes de référence : résultat de Voyante, votes, potions, réactions de décès et conditions de victoire.
- Ajouter le catalogue de conception dès maintenant, tout en séparant « prévu », « expérimental » et « jouable ».
- Produire les fiches détaillées P1 et leur matrice de scénarios, puis les implémenter au-dessus du moteur autoritaire.
- Tester la Petite Fille à table avant de choisir une éventuelle variante numérique ; ne pas bloquer tous les autres rôles sur cette décision.
- Réévaluer les compositions 6–12 et prévoir un nombre de places extensible pour les rôles de grands groupes, sans promettre une limite supérieure testée aujourd’hui.

## Sources primaires consultées

[S0]: https://www.zygomatic-games.com/fr/game/les-loups-garous-de-thiercelieux/
[S1]: https://cdn.svc.asmodee.net/production-zygomatic/uploads/2025/03/Loups-Garous-de-Thiercelieux-Regles-du-jeu.pdf
[S2]: https://www.asmodee.fr/les-roles-des-loups-garous-de-thiercelieux-que-vous-allez-adorer/
[S3]: https://cdn.svc.asmodee.net/production-zygomatic/uploads/2026/05/Loups-Garous-de-Thiercelieux-Nouvelle-Lune-Regles-du-jeu.pdf
[S4]: https://cdn.svc.asmodee.net/production-zygomatic/uploads/2026/05/Loups-Garous-de-Thiercelieux-Personnages-Regles-du-jeu.pdf
[S5]: https://cdn.svc.asmodee.net/production-zygomatic/uploads/2026/05/Loups-Garous-de-Thiercelieux-Le-Village-Regles-du-jeu.pdf
[S6]: https://www.asmodee.fr/tout-savoir-sur-les-nouveaux-personnages-des-loups-garous-de-thiercelieux-a-decouvrir-dans-ledition-collector-25-ans/

- [S0 — Éditeur, jeu classique et effectif annoncé][S0].
- [S1 — Livret officiel classique][S1], notamment p. 5–9, 13–18 et 20.
- [S2 — Asmodee, présentation de rôles][S2], page modifiée le 2 juin 2026.
- [S3 — Livret officiel Nouvelle Lune][S3], notamment p. 24–28.
- [S4 — Livret officiel Personnages][S4], notamment p. 4–20, 23 et 26–27.
- [S5 — Livret officiel Le Village][S5], notamment p. 6–18.
- [S6 — Asmodee, nouveautés anniversaire 2026][S6], article du 3 septembre 2026.

Les PDF ont été récupérés depuis les liens de l’éditeur et leur texte a été consulté. Les résumés ne remplacent pas les livrets pour trancher un détail. Les propositions d’adaptation et de priorité sont l’analyse N’GAME, pas des règles publiées par Asmodee.
