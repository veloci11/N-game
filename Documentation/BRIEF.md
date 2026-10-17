# Loup-Garou — Le village de papier

## Direction retenue

Un livre de contes prend vie sur le téléphone. Le village, la forêt et les personnages semblent construits avec du papier plié et découpé, aux bords visibles et aux ombres délicates. La nuit est bleu encre ; les lanternes, les fenêtres et la lune apportent une lumière miel. L’ambiance reste féerique et mystérieuse, sans horreur graphique.

La référence à conserver est le village nocturne approuvé : loup au premier plan, maisons nichées dans la vallée, rivière et pont, lune au-dessus des toits. Le décor occupe l’écran ; les commandes sont lisibles, discrètes et intégrées à cet univers. Les animations doivent soutenir l’atmosphère et les actions du joueur.

## Première expérience

1. **La nuit.** Le village s’endort. Les plans du décor créent une profondeur légère, la brume glisse et les lumières vacillent doucement.
2. **Ton rôle.** Un appui maintenu dévoile uniquement la carte locale de démonstration, « Loup-Garou ». Relâcher, quitter la fenêtre ou changer de phase la masque immédiatement.
3. **L’aube.** Le même village reçoit une lumière plus chaude. La transition reste continue et la carte secrète est masquée.
4. **Le vote.** Six villageois sont proposés : Léa, Max, Chloé, Hugo, Emma et Lucas. Le joueur choisit un nom puis confirme son choix local. Aucun résultat collectif ni élimination n’est simulé.

Les portraits et illustrations des six personnages sont décoratifs. Ils ne représentent aucun rôle secret attribué à ces personnages. La seule carte révélable appartient au joueur local de la démonstration.

## Mouvement et son

- Mouvements ambiants lents, de faible amplitude, avec des boucles discrètes.
- Parallaxe adaptée aux interactions disponibles, sans dépendance obligatoire au capteur du téléphone.
- Textes et commandes stables pendant l’attente.
- Préférence de réduction des animations respectée ; l’expérience reste utilisable sans mouvement.
- Son désactivé au départ et activable volontairement. Le son ne transporte jamais une information essentielle à lui seul.

## Périmètre de ce prototype

Cette première livraison valide la direction artistique, la profondeur, la révélation privée et l’enchaînement nuit → aube → vote. Le contrôleur JavaScript du navigateur représente ces interactions locales. Il n’exécute ni une partie complète ni un moteur de règles.

Aucun compte, backend, connexion entre téléphones, distribution de rôles, maître du jeu, vote collectif ou élimination n’est présent. Les joueurs sont des données de démonstration. Les commandes de changement de phase servent à explorer la maquette et devront disparaître du parcours joueur réel.

Le Président et les autres jeux restent hors de ce travail. La production repart de zéro dans le dépôt `veloci11/N-game`. Les illustrations choisies sont conservées ; le code de l’ancienne application n’est pas repris.

## Étape suivante : une vraie partie

Après validation du rendu et essai sur téléphone :

1. Définir le mode de jeu : téléphones individuels avec un hôte, ou téléphone partagé avec maître du jeu. Écrire les règles et rôles de la première version.
2. Implémenter le moteur de manche et ses transitions autorisées, avec une source de vérité qui impose les règles.
3. Créer les salons, pseudonymes, codes d’invitation, reconnexions et reprise après mise en veille.
4. Distribuer les rôles côté serveur et ne transmettre à chaque joueur que les informations autorisées. Ne jamais inclure tous les rôles dans un état client public.
5. Gérer les décisions simultanées, les délais, les égalités, les abandons et l’état des joueurs éliminés.
6. Mesurer sur appareils réels le rendu, le temps de chargement, la mémoire, la chauffe et la batterie avant de fixer les exigences de performance.

Le choix définitif du moteur de rendu doit s’appuyer sur ce premier rendu en mouvement et ses mesures. Les couches illustrées et les interactions produites ici constituent une référence artistique réutilisable pour une scène Unity en 2,5D.
