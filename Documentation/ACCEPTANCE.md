# Critères d’acceptation du prototype

Objectifs de vérification, pas une déclaration de conformité. Le bilan des contrôles réellement effectués et les points restant ouverts sont dans `VALIDATION.md`. L’accessibilité système et les appareils mobiles restent notamment à valider.

## Visuel et lecture

- Le village évoque la maquette approuvée : matière papier, nuit bleu encre, lumière chaude, forêt en profondeur et atmosphère de conte.
- Le loup et les éléments proches possèdent une profondeur cohérente avec le village.
- L’interface reste lisible et utilisable sur un écran portrait étroit et dans une fenêtre de bureau.
- Aucun texte, bouton ou élément important n’est coupé par le bord de l’écran ou une zone de sécurité.
- Les éléments fournis sont décrits précisément comme illustrations, plans 2D ou géométrie 3D selon leur nature réelle.

## Interactions et confidentialité

- La carte est masquée à l’ouverture et ne se révèle qu’après un appui maintenu accompli par l’interface.
- Relâcher l’appui, annuler le geste, perdre le focus, passer en arrière-plan ou quitter la phase nuit masque la carte et annule un appui en cours.
- Aucun délai d’appui restant ne peut révéler la carte après ces annulations.
- La carte révélée n’est pas présente visuellement ni annoncée aux technologies d’assistance tant qu’elle est masquée.
- La révélation fonctionne au pointeur et au clavier ; les boutons ont un nom accessible et un état de focus visible.
- Aucun rôle secret des six autres personnages n’est présent dans l’état du contrôleur.

## Phases et vote local

- Les phases nuit, aube et vote restent accessibles pour l’exploration de la démonstration.
- Chaque changement de phase masque le rôle et vide une sélection de vote non confirmée.
- Une sélection concerne uniquement un des six joueurs de démonstration et ne fonctionne que pendant le vote.
- Confirmer nécessite une sélection. Le choix confirmé ne peut être modifié avant la remise à zéro.
- Revenir à une phase antérieure ne supprime pas un vote déjà confirmé.
- La confirmation ne fabrique ni nombre de voix, ni résultat collectif, ni élimination.
- La remise à zéro repart de nuit, masque la carte, vide sélection et vote, puis permet une nouvelle démonstration.

## Préférences et limites

- La réduction des animations coupe ou simplifie les mouvements continus et les transitions sans bloquer le parcours.
- Le son nécessite une activation volontaire et peut être coupé.
- La remise à zéro conserve les préférences de son et de réduction des mouvements.
- Le cadre de prévisualisation indique clairement qu’il s’agit d’une démonstration locale et non d’un salon connecté.
- Les commandes de phase du cadre de prévisualisation sont distinctes des commandes de jeu.

## Vérifications

- Tests Unity : suites `NGame.Core.Tests` en EditMode et `NGame.Presentation.Tests` en PlayMode depuis ce projet.
- Parcours manuel : révéler → relâcher → révéler → quitter la fenêtre → revenir ; le rôle reste caché.
- Parcours manuel : maintenir moins longtemps que le seuil → quitter la fenêtre ou changer de phase ; aucune révélation tardive.
- Parcours manuel : aube → vote → sélectionner → confirmer → nuit → vote ; le choix est toujours verrouillé.
- Vérifier au minimum un format portrait étroit, un format portrait haut et une fenêtre de bureau.
- Une capture fixe confirme seulement un état du rendu. La fluidité et les performances mobiles nécessitent un essai réel en mouvement sur appareil.
