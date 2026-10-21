# Brief des nouveaux éléments du village et des pouvoirs — 20 octobre 2026

Génération avec ImageGen intégré à Codex, sans clé API locale. Les quatorze PNG ont été demandés comme illustrations isolées sur fond transparent, dans le style de conte en papier choisi par Mathis. Ce fichier consigne les briefs de production normalisés ; il ne reproduit pas mot pour mot les requêtes complètes du générateur.

Exigences communes : objets réellement séparés, silhouettes et plis lisibles, matières papier, lumière chaude assortie au village, pas de texte ni d’interface incorporés, pas de paysage entier derrière les objets. Les dimensions, transparences mesurées et SHA-256 effectifs figurent dans [l’inventaire](ASSETS-ET-DEPENDANCES.md).

| Fichier sous `Assets/Resources/Werewolf` | Brief de production | Usage |
| --- | --- | --- |
| `Layered/cottage.png` | Maison de village de conte en papier découpé, toit, murs et fenêtres lisibles, objet entier isolé | Maison indépendante réutilisable dans le village |
| `Layered/inn.png` | Auberge de village en papier, façade et fenêtres permettant un éclairage ancré, silhouette isolée | Bâtiment indépendant, variantes de placement et d’échelle |
| `Layered/church.png` | Église/clocher de village en papier, fenêtres et volumes cohérents avec les maisons, isolé | Repère architectural du village |
| `Layered/bridge.png` | Petit pont de pierre représenté en papier, arche et parapet, isolé sans rivière ni paysage complet | Objet posé au-dessus de la rivière géométrique |
| `Layered/rock-bank.png` | Fragment de berge rocheuse en papier, irrégularités et végétation locale, isolé | Instances de berges et premiers plans |
| `Layered/pine.png` | Sapin de papier avec branches et tronc, entier et isolé | Arbres indépendants de plusieurs profondeurs |
| `Layered/wolf-walk.png` | Même loup de papier en huit poses de marche cohérentes, planche quatre colonnes par deux lignes, fond transparent | Animation image par image, déplacement rigide sur une rue commune |
| `Layered/path.png` | Chemin/escalier de village en papier, perspective cohérente, fragment isolé | Sol indépendant entre les bâtiments |
| `Layered/hearth-flame.png` | Séquence de flammes de foyer chaudes, variations de silhouette, sur transparence | Animation de foyer distincte à l’intérieur des fenêtres masquées |
| `Actions/seer-orb.png` | Orbe/cristal de Voyante sur support, objet de papier isolé | Décor privé de Voyante et symbole d’appel public générique |
| `Actions/witch-bottle.png` | Flacon de potion avec bouchon, objet de papier isolé | Deux instances teintées pour soin et poison, disponibilité privée |
| `Actions/guard-shield.png` | Bouclier de Garde en papier, silhouette nette, isolé | Décor privé du Garde et emblème d’appel public |
| `Actions/cupid-bow.png` | Arc de Cupidon en papier, objet isolé, style du conte | Décor privé et constellation d’appel public sans identité de couple |
| `Actions/hunter-crossbow.png` | Arbalète du Chasseur en papier, arme de conte isolée, volumes lisibles | Décor de réaction privé du Chasseur |

Les animations du village reposent sur des objets, déplacements, rotations, masques de fenêtres et séquences de poses. Elles ne consistent plus à animer les régions d’un PNG complet de village. Les symboles publics suivent uniquement l’appel et l’horloge publics ; ils ne désignent pas une maison de victime ou une identité privée. Les retours privés attendent la soumission autoritaire du pouvoir ; ils ne modifient pas les règles du moteur.

Les portraits des cartes restent des illustrations rigides avec retournement, révélation et lumière. Ces quatorze assets ne constituent pas le découpage de tous les portraits ni l’implémentation des huit rôles d’extension.
