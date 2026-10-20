# Assets et dépendances de N’GAME

État au 20 octobre 2026. Le dépôt actif est `veloci11/N-game`.

## Art et mouvement

Les illustrations du village, les seize cartes et les planches de menus ont été générées pour ce projet. Les sources approuvées restent dans `ArtSource`. Les images de menus sont des références de conception, pas des écrans embarqués à la place de contrôles interactifs.

Le décor utilise des éléments indépendants, un loup déformé par maillage, des shaders localisés pour l’eau et le feuillage, des lumières ancrées, des nuages et une lune séparés. Les cartes ajoutent retournement et effets localisés selon leur illustration. Le dos commun ne reçoit jamais un effet qui dépend du rôle caché. Aucune nouvelle illustration ne remplace les cartes approuvées dans cette reprise.

## Fond universel du catalogue — 20 octobre

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
