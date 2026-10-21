# Assets et dépendances de N’GAME

État au 20 octobre 2026. Le dépôt actif est `veloci11/N-game`.

## Art et mouvement

Les illustrations du village, les seize cartes et les planches de menus ont été générées pour ce projet. Les sources approuvées restent dans `ArtSource`. Les images de menus sont des références de conception, pas des écrans embarqués à la place de contrôles interactifs.

Le décor utilise des éléments indépendants, des lumières ancrées, des nuages et une lune séparés. Le loup du village utilise désormais un corps et une tête générés séparément, avec rotation rigide de la tête ; l’ancien loup déformé par maillage est remplacé dans le décor actif. Les cartes conservent retournement, inclinaison et effets de lumière ; leur échantillonnage de texture ne déplace plus les UV de l’illustration. Le dos commun ne reçoit jamais un effet qui dépend du rôle caché. Aucune nouvelle illustration ne remplace les cartes approuvées dans cette reprise.

## Archive : premier fond beige du catalogue — écarté

`Assets/Resources/Hub/storybook-hub.png` est une nouvelle illustration générée pour N’GAME avec ImageGen lors de cette session, à la demande de Mathis après rejet du village Loup-Garou derrière le menu général. Elle représente du papier clair et des objets de jeux (livre, cartes, couronne, masques), sans loup ni lune. SHA-256 du fichier livré : `2d53aea7ca7a342453bd81c5d340739feda3dc00c153646f442e85d64d5f17c2`.

Il s’agit d’un décor, pas d’un écran UI aplati : cartes, boutons, textes et navigation restent des contrôles Unity. `HubBackdrop` ajoute des mouvements locaux de décor, une parallaxe et le réglage de réduction du mouvement ; leur implémentation ne constitue pas à elle seule une preuve de rendu réussi. Le décor opaque est visible seulement sur `AppRoute.Home`, y compris le sélecteur de création. Le village du jeu reste chargé derrière ce Canvas ; il n’est pas annoncé entièrement désactivé, afin de préserver notamment la caméra et l’AudioListener. La dernière révision ajoute `HubPaper.shader` (pages du livre et feuillage localisés) et des cartes/couronne/masques en papier UI articulé. Il ne s’agit pas d’un livre 3D. Les écrans Loup-Garou conservent leur DA propre.

## Polices

| Police | Provenance | Licence incluse |
| --- | --- | --- |
| Cormorant Garamond | [Google Fonts / Cormorant Garamond](https://github.com/google/fonts/tree/main/ofl/cormorantgaramond) | [SIL Open Font License](Licenses/CormorantGaramond-OFL.txt) |
| Manrope | [Google Fonts / Manrope](https://github.com/google/fonts/tree/main/ofl/manrope) | [SIL Open Font License](Licenses/Manrope-OFL.txt) |

Les notices sont aussi placées dans `Assets/StreamingAssets/Licenses` pour accompagner les builds, et pas seulement le dépôt source.

## Outils Unity

Le projet conserve ses packages existants, dont Input System, uGUI, URP, Test Framework et Pipeline. Le plugin Unity pilote l’éditeur par la CLI et permet les captures, les tests, la compilation et les événements de pointeur. Les versions exactes sont dans `Packages/manifest.json` et `Packages/packages-lock.json`.

Le [dépôt officiel Input System](https://github.com/Unity-Technologies/InputSystem) fournit la référence des événements d’entrée. Il n’est pas cloné une seconde fois dans les assets : le package est déjà installé. Les tests de parcours emploient les vrais contrôles du projet plutôt qu’un framework UI tiers ajouté sans nécessité.

Ne pas importer un dépôt entier pour une fonction déjà présente. Toute future dépendance doit indiquer sa source, sa version, sa licence, son usage réel et les plateformes sur lesquelles elle a été essayée.

## Archive : premier grand visuel selon la référence de gauche

La nouvelle passe suit la référence de gauche retenue par Mathis et remplace le PNG précédent du hub, désormais historique. Le grand visuel est séparé du fond de papier, avec vignettes dédiées dans un atlas : il ne s’agit toujours pas d’un écran UI aplati. Les contrôles et textes restent Unity. Sources de cette étape antérieure :

| Asset | Dimensions | SHA-256 |
| --- | --- | --- |
| `Resources/Hub/storybook-hub.png` |853×1844| `5766fc53f8306f600f224fa3bf5f8033b7d9e94ab89597459378ba1b87a6a74c` |
| `Resources/Hub/catalogue-storybook.png` |2172×724, trois colonnes| `4c46bbf9a264a42f925b3fd9fc9a252f057b822e9147c78725f7b621f447bff8` |

Illustrations générées pour le projet ; cette étape et ses shaders de déformation sont désormais historiques. Le fichier de grand visuel peut rester utilisé pour le papier de fond, sans être le diorama animé courant. Les hashes et captures décrivent les fichiers de cette étape, sans accord humain déduit de la référence.

Le nouveau hero `storybook-hub.png` appartient à la génération désignée `exec-a43ac…` par la racine, distincte de l’ancien fond beige. L’atlas 2172 × 724 contient trois vignettes séparées. Choix du style confirmé par la référence ; rendu final de l’application non encore accepté par l’utilisateur. La finition shader de cette étape ne constitue pas une preuve du nouvel assemblage en éléments séparés.


## Éléments transparents séparés — méthode actuelle

Mathis a rejeté la déformation de l’image entière. Six illustrations ont été générées séparément avec **ImageGen intégré à Codex, sans clé API locale**, à partir du style de la référence de gauche. Le [brief de production](Layer-asset-prompts.md) précise chaque élément. Aucun téléchargement tiers ni achat d’asset n’a été nécessaire pour ces six fichiers.

Contrôle des fichiers intégrés : tous sont RGBA et chaque canal alpha contient des valeurs de 0 à 255. Dimensions et SHA-256 calculés sur les PNG présents dans le dépôt :

| Asset sous `Assets/Resources/Hub/Layers` | Dimensions | Alpha | SHA-256 |
| --- | --- | --- | --- |
| `book-castle.png` | 1254 × 1254 | 0–255 | `45e2003c75bda963d1e33cdb542151ec8bbb1650ec5415ab8f51f7aa59319bbd` |
| `cloud.png` | 2172 × 724 | 0–255 | `a62742e823da7d9be40884f93873873477c619d7f3ac73d9d8eb549b29e4bfbe` |
| `crown.png` | 1254 × 1254 | 0–255 | `9259ea509429fa67b2d5595745511d5b9aabec1b3ed2b8427c0aa7ae68585020` |
| `foliage.png` | 1254 × 1254 | 0–255 | `a4303af093fb5f1c4a0c6bdb277dfed2ea0e3d58274c59418ae052247013c29a` |
| `wolf-body.png` | 1254 × 1254 | 0–255 | `a70256c3ccd112d66a0bff5a9fd2fc586efce9ceaede831034d92e44b7279a5a` |
| `wolf-head.png` | 1254 × 1254 | 0–255 | `3dbdfc33886fe41558016c69dce0a2a3dc8b667c5c2af7060eddbadd4461695f` |

Le diorama d’accueil comporte **huit instances chargées à partir de ces six fichiers** : deux nuages, corps et tête du loup, livre/château, deux feuillages et couronne. `HubPaperDiorama` conserve les proportions et applique seulement des translations ou rotations aux éléments concernés ; les nuages passent derrière le château. `IndependentWolfPuppet` réutilise le corps et la tête pour le village Loup-Garou, sans ancien maillage déformé dans ce rendu. Le mode mouvements réduits arrête ces mouvements.

La racine a observé le diorama chargé dans Unity. Les [capture actuelle](qa-2026-10-20/home-cutouts-final.png), [GIF](qa-2026-10-20/home-cutouts-final.gif) et [aperçu du village](qa-2026-10-20/werewolf-cutouts-village-final.png) ont été relus indépendamment. Le GIF contient 64 images réelles sur 6,714 s, sans interpolation. La capture du village est un **aperçu local de décor**, pas une partie réseau. La campagne complète actuelle [70/70 PlayMode](qa-2026-10-20/playmode-cutouts-final.json), en 118,62 s et sans échec/ignoré/inconclusif, couvre les nouveaux éléments et remplace le bilan UI antérieur par union de 67 cas. Le raccord visuel tête/corps et la qualité du mouvement restent soumis à réception humaine.

### Limite du découpage actuel

Six éléments générés sont prêts ; il n’y a pas encore d’asset séparé pour le soleil ou les cartes du grand décor d’accueil. Le loup comporte **deux parties**, corps et tête : les oreilles ne sont pas articulées indépendamment. Les portraits des cartes de rôles restent des illustrations rigides tant que leurs propres éléments séparés ne sont pas produits. Les cartes conservent retournement, révélation et effets de lumière, sans prétendre que chaque personnage ou rôle possède déjà sa propre articulation.
