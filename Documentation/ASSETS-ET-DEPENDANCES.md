# Assets et dépendances de N’GAME

État au 20 octobre 2026. Le dépôt actif est `veloci11/N-game`.

**Usage courant des fenêtres :** shader `HearthWindows` sur les vitres illustrées, sans sprites de flammes/halos plaqués. Les inventaires ci-dessous conservent les sources générées et les étapes historiques ; l’atlas `hearth-flame.png` n’est plus utilisé pour afficher les fenêtres. La [dernière correction](qa-2026-10-20/delivery-role-polish.md) décrit les preuves actuelles.

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

## Village reconstruit et pouvoirs — quatorze nouveaux assets

La nouvelle passe reconstruit **tout le village visible** avec des objets indépendants : maisons, auberge, église, pont, berges, arbres et chemin. `VillageStage` délègue à `LayeredVillageScene` ; les anciens PNG complets du village, `VillageDetails` et `PaperRiver` ne sont plus utilisés par ce rendu. Le ciel et la rivière sont des géométries séparées, les reflets sont des traits indépendants. Lune et nuages réutilisent leurs véritables illustrations isolées.

Quatorze PNG générés avec ImageGen intégré à Codex, sans clé API locale. [Briefs de production normalisés](Village-layered-asset-prompts.md), sans prétendre une transcription verbatim des prompts. Aucune bibliothèque graphique tierce ni achat nécessaire pour ces fichiers. Inventaire mesuré sur les fichiers intégrés :

| Asset sous `Assets/Resources/Werewolf` | Dimensions | Mode / alpha mesuré | SHA-256 |
| --- | --- | --- | --- |
| `Layered/bridge.png` | 1536 × 1024 | RGBA, 0–254 | `03549cd536692f26f88f07718f186e9ebaab5d7f0556ab1d5cde39befa554f39` |
| `Layered/church.png` | 1275 × 1234 | RGBA, 0–255 | `410391917100838c76f3fe286e284e14417aaf96192a3c22051996bf3ab1dad7` |
| `Layered/cottage.png` | 1312 × 1199 | RGBA, 0–255 | `94b3a761b8dfee0e0cfb16f2acd2cf8ea92e48e48cc86df8e573839190ad10de` |
| `Layered/hearth-flame.png` | 2172 × 724 | RGBA, 0–255 | `e309a78808115515df1685ec2edff1c1f5af334b8c0777c7d847ed8fe5437d4b` |
| `Layered/inn.png` | 1312 × 1199 | RGBA, 0–255 | `ff2373101e7bd6d517b1a22683f3517b85d62dbad1c95fd1423a6b418b8475fd` |
| `Layered/path.png` | 1024 × 1536 | RGBA, 0–254 | `f1a383d7eb01695e9a9fd91108d9066afdddf0eff1de28a89b9d1eec7ae2a806` |
| `Layered/pine.png` | 1024 × 1536 | RGBA, 0–254 | `b9fa8c008b138068193a38b50fa108c4e1daad62e62db4349bb1db8e6564a2a2` |
| `Layered/rock-bank.png` | 2172 × 724 | RGBA, 0–255 | `1b3540f14beaa6f33c333635690e73b0f37cbef75f8302f581106dc8fa46899e` |
| `Layered/wolf-walk.png` | 1774 × 887 | RGBA, 0–255 | `2e875eed30a0e86367eab106cf028b9925abfcef8e86909506191655caf82c3d` |
| `Actions/cupid-bow.png` | 1254 × 1254 | RGBA, 0–255 | `bc989295a8b8aee3f6ebfdcc710334dd4a4163076da05ba36b018e9274859478` |
| `Actions/guard-shield.png` | 1254 × 1254 | RGBA, 0–255 | `aaac9d91b3c2195e78fd6eee32453f41e30b8c78d2b6b4bde088cc94d9b04acc` |
| `Actions/hunter-crossbow.png` | 1254 × 1254 | RGBA, 0–255 | `5b3da4352a93a59f55021f1d7bb3e9b304eabe93bd08bc58cdcfee1c08b95523` |
| `Actions/seer-orb.png` | 1254 × 1254 | RGBA, 0–255 | `3131da3fcb4434a676169853258daad062289bea29c4b79445c79d0bdc5c7e39` |
| `Actions/witch-bottle.png` | 1254 × 1254 | RGBA, 0–255 | `e7c86b25882a540bb081f4a300875a022bb632c6eec0240e810e3cd14f039025` |

Les maxima alpha 254 de `bridge`, `path` et `pine` sont les valeurs réelles, proches de l’opacité ; ils ne sont pas annoncés comme 255. Tous les fichiers contiennent une transparence réelle. La présence d’un alpha ne valide pas à elle seule les raccords, le placement ou la qualité perçue.

Le loup de marche comporte huit poses. Les 14 fenêtres utilisent leurs propres flammes de foyer sur atlas quatre poses, ouverture masquée, halo extérieur et variations de couleur/intensité déphasées ; les bâtiments eux-mêmes ne pulsent pas. Les symboles Garde/Voyante/Sorcière/Cupidon/amoureux sont des appels publics génériques, sans information de cible. Les illustrations privées de pouvoirs utilisent les cinq assets `Actions`. Les anciens rapports 70/70 et le build `build-mac-cutouts.json` précèdent cette reconstruction et ne la valident pas. Les nouvelles preuves Unity et le build courant seront consignés dans [le bilan de cette passe](qa-2026-10-20/delivery-layered-current.md).


## Correction courante de l’éclairage et de l’activité

Le code `HearthWindows.shader` éclaire désormais les vitres originales dans les textures des bâtiments, limité par leurs zones UV et la couleur du verre ; il conserve murs, papier et croisillons. Il n’y a aucun déplacement d’UV, déformation de façade ou gros rectangle coloré superposé. Les sprites de flamme et leurs halos sont désactivés ; **`Layered/hearth-flame.png` reste un asset historique inutilisé pour le rendu courant des fenêtres**. Ses dimensions et SHA précédents décrivent toujours le fichier, pas une animation active. Les reflets de rivière ont été atténués et normalisés.

La reprise ne génère pas de nouveaux PNG : elle réutilise les illustrations séparées, ajoute un shader local au projet et améliore les objets/retours d’interface. Les lucioles, ailes et traces sont des éléments UI autonomes, projetés depuis les positions du village via la caméra, sans panneau de mini-jeu ; leur lanterne et son support restent ancrés. Seules quatre cibles de 44 points reçoivent les contacts. Les contrôles actuels sont suivis dans [delivery-role-polish.md](qa-2026-10-20/delivery-role-polish.md) ; les anciennes captures de flammes plaquées restent historiques.
