# Validation du premier aperçu Unity

17 octobre 2026 — nouveau dépôt `veloci11/N-game`, code reconstruit sans reprendre l’ancienne application.

## Périmètre

Cette version valide une direction artistique et un parcours local. Le rôle de démonstration est fixe, les six portraits sont décoratifs et le vote confirme seulement un choix local. Elle ne permet pas encore de jouer une partie complète entre téléphones.

## Rendu et mouvement

Rendu observé dans Unity 6000.6.6f1, URP 2D, format portrait 430 × 932. Les captures `home-final.png`, `night-final.png`, `role-final.png` et `vote-final.png` proviennent du rendu de l’application, pas d’une maquette générée avec l’interface intégrée.

- Nuit : village, carte masquée puis révélée, loup opaque séparé, premier plan, lanterne et rivière visibles.
- Onze points lumineux : dix dans l’illustration, un attaché à la lanterne indépendante.
- Mesure de deux états pendant l’exécution : horloge ambiante `2.326 → 36.189`, angle de lanterne `2.061° → −2.390°`, intensité d’une fenêtre `0.697 → 0.692`. Les mouvements viennent du code exécuté dans Unity.
- Réduction des animations : eau et vacillement désactivés, angle de lanterne remis à zéro. La transition vers le jour est raccourcie.
- Le texte de narration passe progressivement à l’encre sombre à l’aube pour rester lisible sur le ciel clair.

Les fonds nuit/aube sont deux illustrations proches, avec de légères différences de géométrie. La transition est un fondu ; une production plus aboutie utilisera une géométrie unique avec éclairage pilotable. La lune et trois groupes de nuages sont désormais séparés. Le loup utilise un maillage de 2 793 sommets avec déformations locales des oreilles, de la tête et du thorax ; il ne s’agit pas d’un modèle 3D complet.

Les seize cartes validées sont consultables dans quatre pages et leurs fiches. Le shader des cartes anime le portrait et les reflets ; leurs bordures et textes restent lisibles. La carte privée effectue un retournement de 0,18 s après le maintien de 0,65 s. Le masquage annule immédiatement le retournement, même s’il vient de commencer.

`village-motion.gif` : 64 images capturées dans le moteur sur 9,13 s, puis encodées à 320 × 694. Le maintien est piloté par le contrôleur pendant cet enregistrement. Les images montrent deux révélations et masquages, la déformation du loup, les nuages, la lune, les flammes et la rivière. Le fichier conserve la cadence mesurée de la capture ; ce n’est pas une mesure de performances mobiles.

La fiche Petite Fille a aussi été rendue avec une hauteur de contenu sûr simulée à 838 unités dans la fenêtre 430 × 932 : aucune superposition entre les modes et le pied de fiche. Voir `role-small-safe-area.png`.

## Application Mac

Build `StandaloneOSX` réussi, zéro erreur, environ 165 Mo. Rapport `build-mac.json`. Le build a été lancé : accueil, polices, illustrations et rendu du village observés dans l’application native. L’automatisation des clics de cette fenêtre a rencontré une limite de l’outil de contrôle ; aucun parcours manuel natif complet n’est revendiqué. Les parcours interactifs sont vérifiés par les tests PlayMode et les rendus de l’Editor.

Le build émet 346 avertissements de shaders du package Unity AI Inference, inutilisé par le code du jeu. Ce point et les dépendances du template restent à alléger avant une distribution mobile. L’aperçu Mac se trouve dans `Builds/NGame.app`, exclu du dépôt avec les autres fichiers de compilation.

## Contrôles automatisés

Résultats exécutés dans l’Editor : **48/48 EditMode et 16/16 PlayMode réussis**, sans échec ni test ignoré. Rapports : `tests-edit.json` et `tests-play.json`. Les suites couvrent les états locaux, le catalogue, les préférences, les compositions proposées, les gestes de confidentialité et leur annulation pendant le retournement, les vrais pixels alpha des surfaces UI, la bibliothèque, les mouvements réduits et l’extinction au jour. Les shaders du village et des cartes sont contrôlés pour les erreurs de compilation.

La simulation des événements de pause/focus teste la réaction des composants. Elle ne remplace pas un essai sur le système iOS/Android.

## Points restant ouverts

- Réseau, salons, serveur de règles, distribution privée, actions des rôles, résolution et fin de partie.
- Tests sur vrais téléphones, gestes tactiles et reprise après verrouillage du système.
- Profilage du coût des shaders, transparences, mémoire, batterie et téléchargement.
- Vérification d’accessibilité VoiceOver/TalkBack et parcours complet au clavier. Aucun support système complet n’est revendiqué.
- Réglage des compositions avec de vraies parties ; les propositions 6–12 ne sont pas un équilibrage validé.
- Habillage sonore définitif. Le son actuel est un souffle procédural optionnel.
- Builds iOS/Android signés et distribution mobile. Les modules de ces plateformes ne sont pas installés sur ce Mac au moment de cette validation.

Les critères de `ACCEPTANCE.md` sont les objectifs de validation, y compris ceux encore ouverts ci-dessus.
