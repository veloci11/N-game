# Livraison locale en cours — 20 octobre 2026

Le serveur public sera hébergé plus tard, à la demande de Mathis. Cette passe prépare et teste le jeu local ; elle ne déploie aucun service.

## Accueil et direction artistique

**Méthode d’animation en éléments séparés intégrée, réception humaine en attente.** Mathis a rejeté la déformation de l’image entière. Le nouveau décor utilise des éléments générés séparément : livre/décor, nuages, corps et tête du loup, feuillage, couronne. Les mouvements viennent des déplacements et rotations propres à ces objets. Les shaders de déformation précédents sont historiques pour cet accueil.

Le style reste celui du téléphone de gauche : livre ouvert, château, loup de papier, couronne et cartes, puis trois vignettes. Le décor nocturne reste propre au Loup-Garou. Les huit instances de l’accueil ont été observées chargées dans Unity. Le loup du village utilise aussi les nouveaux corps et tête séparés, avec rotation rigide au cou ; les cartes conservent lumières et retournement mais échantillonnent leur illustration sans déplacement d’UV. Six éléments générés sont prêts ; soleil et cartes du grand décor restent sans assets séparés. Le loup a deux parties et ses oreilles ne sont pas indépendantes. Les portraits des cartes restent rigides, avec retournement, révélation et lumière conservés ; aucune articulation individuelle de tous les rôles n’est revendiquée. La campagne complète actuelle **70/70 PlayMode** réussit en **118,62 s**, zéro échec/ignoré/inconclusif. Les captures réelles ont été relues par revue indépendante ; cette réussite ne constitue pas une approbation humaine du rendu. Voir le [brief des six assets](../Layer-asset-prompts.md) et leurs [empreintes SHA-256](../ASSETS-ET-DEPENDANCES.md).

Rendus actuels de Unity relus : [accueil](home-cutouts-final.png), [animation de l’accueil](home-cutouts-final.gif) et [aperçu local du village](werewolf-cutouts-village-final.png). Le GIF contient **64 images réelles 430 × 932**, capturées sur **6,714 s** (durée GIF 6,810 s), sans images intermédiaires inventées ; [manifest](home-cutouts-motion.json). L’image 50 du GIF a aussi été relue. Aucun P1 visuel bloquant confirmé dans cette revue : textes et commandes restent lisibles, les zones de jeu et les boutons ne se chevauchent pas. La qualité perçue du mouvement et le raccord du loup restent à valider par Mathis. L’image du village porte « APERÇU LOCAL » : elle montre le décor, **pas une partie réseau**.

Les [captures du précédent accueil](home-reference-final.png) et de sa [zone sûre 850](home-reference-safe850.png) montrent l’état avant ce nouveau chantier. Elles ne prouvent pas le rendu en éléments séparés. Les anciens `hub-universal-*`, `hub-motion-*` et `hub-final.png` sont également historiques ; l’accueil beige a été rejeté. Le choix de la référence ne vaut pas approbation de l’app.

## Loup-Garou

Huit rôles de base sont développés : Loup-Garou, Villageois, Voyante, Sorcière, Chasseur, Cupidon, Garde et Petite Fille. La Petite Fille reste réservée au jeu en présence. Les huit cartes d’extension n’ont pas encore leurs règles ; Président et Mimes sont affichés « En préparation ».

Le vote comporte trois cibles par page et conserve les sélections pendant les mises à jour. Le dock Messages reste translucide et limité au vote. Les éliminés disposent de consignes et de droits distincts. Le vocal distant exige une activation volontaire et est limité au vote de jour. Ces droits sont contrôlés côté serveur ; l’acoustique et les permissions réelles restent à essayer sur appareils.

Le dernier correctif compacte le formulaire privé : informations nécessaires à la décision en premier, quatre cibles par page, détails complémentaires après les choix et maintien de la confidentialité. La réservation de la zone de vote suit le haut réel du dock relevé par le clavier. Pendant la saisie, le formulaire est masqué ; sa page et son choix sont restaurés ensuite. Le scénario Sorcière en zone sûre 850 et les comportements de confidentialité passent dans les rapports actualisés. Le clavier est simulé dans ces tests ; le clavier réel reste à éprouver.

La lueur nocturne facultative produit le même retour local pour chaque contact, sans score ni dépendance au rôle. Elle ne garantit pas de cacher les gestes entre voisins et ne remplace pas les consignes physiques du jeu.

## Preuves actuelles et archives

| Contrôle exécuté | Résultat | Portée |
| --- | --- | --- |
| [PlayMode éléments séparés](playmode-cutouts-final.json) | **70/70**, 118,62 s, zéro échec/ignoré/inconclusif | Campagne complète unique actuelle ; UI Loup-Garou et nouveaux éléments. Ne valide pas l’expérience humaine |
| [EditMode référence](editmode-reference-final.json) | 116/116 | Dernier passage de cette suite, non rejoué après les changements de présentation ; cœur inchangé |
| [PlayMode référence](playmode-reference-final.json) | 65/65, 79,5 s | Campagne complète antérieure aux derniers correctifs privé/chat |
| [Hub ciblé](playmode-reference-hub.json) | 11/11 | Finition des bords du grand visuel et parcours du hub |
| [Archive : passage complet privé](playmode-private-second.json) | 66/67 | Un échec de synchronisation de raycast dans le nouveau test clavier |
| [Archive : retest ciblé privé](playmode-private-target-final.json) | 15/15, 1,64 s | Fixture resynchronisée, aucun changement produit ; union des deux rapports : **67 cas uniques couverts**, pas une campagne unique 67/67 |
| [Audit Core/serveur](core-server-final-audit.md) | 67 Core, 553 HTTP partie, 1 016 HTTP cycle/reprise/vocal | Les comptes HTTP comprennent des polls ; pas autant de scénarios humains |
| [Build Mac courant](build-mac-cutouts.json) | Succeeded, **0 erreur**, 346 warnings AI, 19 281 ms, 212 989 857 octets | Plist valide, signature ad hoc vérifiée avec `--deep --strict` ; pas de recette UI native ni de notarisation |
| [Lanceur LAN](lan-launcher-checks.log) | 28 vérifications `--check` | Validation sans écoute réseau ni lancement du serveur |

Les passages [initial 65/66](playmode-private-initial.json) et [ciblé initial 13/15](playmode-private-target-initial.json) sont conservés comme archives des échecs de recherche/assertions des fixtures signalés par la racine. Ces passages sont désormais historiques : la campagne complète actuelle 70/70 les remplace dans le bilan UI.

Les anciens builds Mac sont obsolètes pour cette livraison ; le nouveau build à éléments séparés a compilé avec succès. Les 346 avertissements proviennent des shaders du package Unity AI Inference inutilisé par le jeu ; le build n’est pas annoncé sans avertissement. Le plist et la signature locale ad hoc sont vérifiés. Le lancement et son processus enfant sont contrôlés séparément : ils ne prouvent pas une recette complète de l’interface native. Les preuves UI de ce lot proviennent de Unity Editor. Il n’y a ni Developer ID ni notarisation.

Les changements actuels ne sont pas annoncés comme poussés tant que la branche distante n’a pas été relue ; le dernier snapshot poussé confirmé au début de cette passe est `71911ad`.

Il reste à éprouver une vraie partie entre joueurs et téléphones, le clavier mobile, les permissions audio, le confort des animations, l’équilibrage et les performances. Les tests automatiques ne constituent pas une réception humaine.
