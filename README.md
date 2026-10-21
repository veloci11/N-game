# N’GAME — Le village de papier

Application de jeux de soirée sous Unity, reconstruite dans ce nouveau dépôt. Le premier jeu est le Loup-Garou dans la direction artistique de conte en papier validée par Mathis. Le Président attend sa propre direction artistique.

## Version 0.3 — intégration en cours

L’accueil utilise désormais des éléments séparés : livre/décor, nuages, corps et tête du loup, feuillage et couronne, animés par déplacements et rotations propres à chaque élément. Cette méthode remplace la déformation de l’image entière rejetée par Mathis. Le style reste celui du livre de papier choisi ; la qualité du nouveau rendu reste à valider par l’utilisateur.

La campagne complète actuelle réussit **70/70 PlayMode**, zéro échec/ignoré/inconclusif, en **118,62 s** : elle couvre l’interface Loup-Garou et les nouveaux éléments séparés. Les **116/116 EditMode** proviennent du précédent passage ; le cœur n’a pas changé et cette suite n’a pas été rejouée. Le scénario Sorcière safe850 et la restauration du vote après saisie sont couverts. Le retour lumineux nocturne reste facultatif, sans garantie de dissimuler les gestes.

Le nouveau build Mac compile : **zéro erreur**, 346 avertissements des shaders Unity AI Inference, plist valide et signature locale ad hoc vérifiée. Le lancement ne vaut pas recette de l’interface native ; les captures et parcours UI proviennent de l’Editor. Le snapshot `ddc74e9` est poussé sur `origin/codex/werewolf-playable`, SHA distant relu et confirmé. **Hébergement public reporté à plus tard par Mathis, aucun déploiement dans ce lot.** Téléphones, acoustique et partie humaine restent non validés. [État de livraison](Documentation/qa-2026-10-20/delivery-current.md), [suivi détaillé](Documentation/SUIVI-PRODUCTION.md).

- Salons de 6 à 24 personnes : code d’invitation, pseudonymes, portraits, confirmations et composition réglable par l’hôte.
- Distribution aléatoire avec découverte et confirmation individuelle avant les pouvoirs ; informations secrètes par destinataire, actions nocturnes, discussion, votes, verdict lisible, réactions, victoire et revanche.
- Huit rôles avec règles : Loup-Garou, Villageois, Voyante, Sorcière, Chasseur, Cupidon, Garde et Petite Fille. Les huit autres illustrations restent au catalogue, sans activation de règles absentes.
- Deux configurations : en présence et à distance. La Petite Fille exige l’observation physique à table et est désactivée à distance.
- Messages écrits pendant le vote uniquement : Village pour les vivants, Village en lecture seule et Observateurs pour les éliminés. Aucun canal Meute ni historique accessible hors vote. Le dock passe les scénarios UI de vote et confidentialité ; les ACL passent 913 contrôles HTTP. Le vrai clavier mobile reste à éprouver. Relais vocal natif développé, limité au vote de jour distant et activé volontairement ; le microphone WebGL reste indisponible, avec secours écrit. Le transport audio est testé côté serveur, pas encore l’acoustique ou les permissions sur téléphone.
- Cadre commun avec commandes fixes, burger et confirmation de départ. Les actions privées s’ouvrent volontairement pour être utilisées à une main.
- Entraînement hors ligne avec sept habitants simulés et le même moteur de règles. Ce mode est explicitement individuel.
- Village animé : lune et trois groupes de nuages séparés, eau et reflets, dix lumières ancrées au décor, flammes irrégulières, brume, fumée de cheminée, étoiles, branches et loup composé d’un corps et d’une tête indépendants.
- Cartes animées : apparition, inclinaison, dorures, effets de lumière et retournement. Les portraits restent rigides tant que leurs propres éléments séparés ne sont pas créés ; les rôles n’ont pas encore tous une articulation individuelle. Maintenir pour lire sa carte, relâcher pour la masquer. Le panneau d’action privée se ferme aux interruptions et changements de tour ; aucun effet du dos commun ne dépend du rôle caché.
- Fondu progressif nuit/aube dans les vraies parties ; son ambiant optionnel et mouvements réduits persistants.

Les règles et conventions N’GAME sont détaillées dans [WEREWOLF-RULESET.md](Documentation/WEREWOLF-RULESET.md). Les compositions et les formats six/sept demandent encore des essais d’équilibrage entre amis.

Le décor utilise des illustrations 2D en plusieurs plans, des shaders d’eau et de lumière, et un loup dont le corps et la tête sont deux éléments séparés. La tête tourne au niveau du cou sans déformer son illustration ; les oreilles ne sont pas des pièces indépendantes. Les six éléments générés actuels n’incluent pas encore de soleil ni de cartes séparées pour le grand décor. Les maisons ne sont pas encore des objets 3D indépendants. La lanterne flottante a été supprimée ; les lumières du village restent à leur place.

## Essayer

Dans Unity Hub, ouvrir ce dossier avec **Unity 6000.6.6f1**, puis `Assets/NGame/Scenes/Storybook.unity` et **Play**. Le menu **NGame → Portrait Game View** règle l’aperçu en 430 × 932.

**S’entraîner seul** fonctionne sans serveur. Pour une vraie soirée, lancer sur le Mac :

```sh
bash Server/NGame.Server/run.sh
```

Le client utilise `http://127.0.0.1:8787` par défaut. Créer un village, puis le rejoindre avec d’autres clients utilisant le même serveur. Pour une future recette sur le même réseau, le [guide de table locale](Documentation/RECETTE-TABLE-LOCALE.md) fournit un lanceur exigeant explicitement l’adresse privée du Mac ; sa validation seule n’ouvre aucun port. Le détail du lancement et du service WebGL est dans [le README serveur](Server/NGame.Server/README.md).

Pour lire sa carte : maintenir le bouton inférieur, ou **Espace** sur ordinateur. Pour utiliser un pouvoir : ouvrir le panneau privé, sélectionner puis confirmer avec le bouton principal ; aucun second doigt maintenu n’est nécessaire. Ouvrir un menu ou perdre le focus masque les secrets. Le vote est une action publique de jour, avec choix personnel caché jusqu’à clôture.

Le serveur conserve maintenant les salles et reçus dans des sauvegardes privées atomiques. Le client natif conserve uniquement son accès de reprise, sans rôle privé en cache ; WebGL utilise le stockage de session de l’onglet. La reprise doit retrouver la même identité et les mêmes cartes. Un départ confirmé efface cet accès ; un pseudo seul ne récupère pas une place. Les contrôles de redémarrage serveur sont décrits dans [le README serveur](Server/NGame.Server/README.md).

L’hébergement Internet permanent est reporté à plus tard à la demande de Mathis. Il n’y a pas encore de compte, de build mobile signé ni de validation sur de vrais téléphones. La latence acoustique, l’écho, les accessoires, la batterie et les permissions du vocal natif restent à mesurer. Les fichiers de packaging et de confidentialité ne prouvent pas qu’un build mobile a été produit ou accepté par un store.

Le projet conserve aussi l’ancienne démonstration visuelle, avec rôle fixe et six portraits décoratifs. Elle ne constitue pas une vraie partie. Ses liens « Explorer le décor » et « La Veillée » ont été retirés de l’accueil normal pour éviter cette confusion ; le code reste disponible pour les outils de développement.

## Organisation et validation

| Dossier | Rôle |
| --- | --- |
| `Assets/NGame/Core` | Moteur pur, vues filtrées, catalogue, préférences et ancien aperçu |
| `Assets/NGame/Presentation` | Navigation, connexion HTTP, écrans, confidentialité et village animé |
| `Assets/NGame/Tests` | Règles EditMode et présentation PlayMode |
| `Server/NGame.Server` | Serveur ASP.NET Core 8 utilisant les mêmes sources de règles |
| `Assets/Resources/Werewolf` | Illustrations, polices et shaders du jeu |
| `ArtSource` | Références approuvées, sources et provenance |
| `Documentation` | Plan, conventions, captures et rapports de validation |

Les tests actuellement exécutés et leurs limites figurent dans [VALIDATION.md](Documentation/VALIDATION.md), et les tâches restantes dans [SUIVI-PRODUCTION.md](Documentation/SUIVI-PRODUCTION.md). Les objectifs mobiles, l’accessibilité et le profilage sur appareil restent à valider.

![Accueil actuel en éléments séparés, capture Unity du 20 octobre](Documentation/qa-2026-10-20/home-cutouts-final.gif)

64 images réelles de Unity sur 6,714 s, sans interpolation. Le [village actuel](Documentation/qa-2026-10-20/werewolf-cutouts-village-final.png) est montré en aperçu local, pas en partie réseau. L’[ancien enregistrement du 17 octobre](Documentation/village-motion.gif) reste une archive ; aucun de ces fichiers ne mesure les performances sur téléphone.

## Assets

Illustrations générées à partir de la référence choisie ; inventaire initial dans [assets.json](ArtSource/Werewolf/assets.json), et nouveaux éléments séparés dans le [brief de génération](Documentation/Layer-asset-prompts.md) et les [empreintes d’assets](Documentation/ASSETS-ET-DEPENDANCES.md). Cormorant Garamond et Manrope sous SIL Open Font License, licences incluses. Aucun asset payant acheté.
