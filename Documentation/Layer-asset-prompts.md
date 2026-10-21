# Brief des éléments de papier séparés — 20 octobre 2026

Génération avec l’outil ImageGen intégré à la session Codex, sans clé API locale. Les six PNG ci-dessous ont été générés séparément, avec fond transparent demandé. Ce document consigne le brief de production par élément ; il ne prétend pas reproduire mot pour mot les requêtes complètes et métadonnées internes du générateur.

Référence de style : le téléphone de gauche de l’image fournie par Mathis (`codex-clipboard-56aa2642-e293-4d90-a716-689c7986f65e.png`). Papier découpé et plié, conte illustré, matières chaudes, château et forêt, loup bleu nuit, couronne dorée. Pas de texte, de boutons ni de cadre de téléphone incorporés aux assets.

| Fichier sous `Assets/Resources/Hub/Layers` | Brief de l’élément autonome | Assemblage prévu |
| --- | --- | --- |
| `book-castle.png` | Livre ouvert de conte, pages et château en papier avec paysage, élément de décor isolé sur transparence. Conserver le style artisanal de la référence | Socle fixe du diorama de l’accueil |
| `wolf-body.png` | Corps du loup en papier bleu nuit, **sans tête**, isolé sur transparence ; plis et matière compatibles avec la tête générée séparément | Partie fixe ; réutilisée dans le décor Loup-Garou |
| `wolf-head.png` | **Tête seule** du loup de papier bleu nuit, cou permettant un raccord, oreilles et œil ambré, isolée sur transparence | Rotation rigide légère au niveau du cou ; réutilisée dans le décor Loup-Garou |
| `cloud.png` | Bande de nuages en papier découpé, isolée sur transparence, volumes et couleurs compatibles avec le conte | Deux instances indépendantes qui se déplacent derrière le château |
| `foliage.png` | Brin/branche de feuillage de papier, tige et feuilles isolées sur transparence, teintes naturelles assorties à la référence | Deux instances au premier plan, rotations légères depuis leur base |
| `crown.png` | Couronne pliée en papier doré, élément isolé sur transparence, volumes nets et lumière chaude | Objet posé au premier plan du livre |

Le mouvement vient des objets séparés (`HubPaperDiorama`, `IndependentWolfPuppet`), avec position, échelle uniforme et rotation. Il ne vient plus d’un déplacement des UV de l’image entière. Le changement de méthode répond au rejet des déformations par Mathis ; il ne constitue pas une nouvelle approbation visuelle de sa part.

Les dimensions, canaux alpha et empreintes SHA-256 des fichiers effectivement intégrés figurent dans [ASSETS-ET-DEPENDANCES.md](ASSETS-ET-DEPENDANCES.md). Le contrôle de transparence vérifie un canal RGBA avec valeurs alpha 0 à 255 dans chaque fichier ; il ne mesure pas à lui seul la qualité du raccord tête/corps ni la qualité perçue du mouvement.

Périmètre livré : ces six éléments uniquement. Le soleil et les cartes du grand décor n’ont pas encore leurs assets séparés. Le loup est composé d’un corps et d’une tête, sans oreilles indépendantes. Les portraits des cartes de rôles restent rigides ; leur retournement, révélation et éclairage sont conservés. Une animation propre aux parties de chaque personnage demanderait d’autres illustrations séparées.
