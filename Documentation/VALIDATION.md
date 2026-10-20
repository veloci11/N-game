# Validation et limites de N’GAME

## État actuel du 20 octobre 2026

Développement repris avec une équipe. **Une réussite technique locale n’est pas une validation humaine, acoustique ou sur téléphone.**

| Vérification | Résultat actuel | Preuve / portée |
| --- | --- | --- |
| Unity EditMode | **116/116**, zéro échec/ignoré/inconclusif, 0,23 s | [Rapport actuel](qa-2026-10-20/editmode-hub.json), Unity 6000.6.6f1 |
| Unity PlayMode | **56/56**, campagne complète unique, zéro échec/ignoré/inconclusif, 165,22 s | [Rapport actuel](qa-2026-10-20/playmode-hub-responsive.json). Hub, reprise protégée, zone sûre réduite, pouvoirs privés, vote avec dock ouvert, brouillons, guidance et partie → résultat → revanche |
| Moteur / serveur | 67 cas moteur .NET ; API générale et reprises testées, contrat texte actuel 913contrôles HTTP | [Moteur](qa-2026-10-20/core-67-results.log), [services](../Server/NGame.Server/verification-2026-10-20.md), [messages pendant vote](qa-2026-10-20/chat-vote-only.md). Les anciens rapports salon/Meute documentent leur ancienne version ; octets PCM ≠ micro essayé |
| Hub universel et navigation | Ancien décor refusé remplacé par papier clair universel ; **10/10 tests ciblés**, zéro échec/ignoré/inconclusif, 3,09 s après les 56 globaux ; rendu normal/safe 850/création relu, acceptation Mathis en attente | [Rapport ciblé actuel](qa-2026-10-20/playmode-hub-motion.json), [hub](qa-2026-10-20/hub-universal-final.png), [zone sûre](qa-2026-10-20/hub-universal-safe-area.png), [création](qa-2026-10-20/hub-universal-create.png), [jeu avec son décor propre](qa-2026-10-20/werewolf-own-background.png). Tests de mouvement/réduction et Home → GameHome → Home ; aucune nouvelle campagne globale revendiquée |
| Vote / messages | Dock du bas semi-transparent, pseudos distincts, vote public utilisable ; fermeture au verdict et carte masquée | [Capture réelle](qa-2026-10-20/vote-chat-final.png). La racine a cliqué cible puis confirmation avec le dock ouvert jusqu’au verdict. Phase préparée via le moteur Practice pour capturer : ce n’est ni novice ni échange réseau entre deux UI |
| Mouvement du hub | 64 frames réelles / 6,37 s, livre et feuillage déformés localement, icônes articulées ; libellés principaux et boutons restent stables sur les frames 10/50 ouvertes | [Capture](qa-2026-10-20/hub-motion-final.png), [GIF](qa-2026-10-20/hub-motion-final.gif), [manifest](qa-2026-10-20/hub-motion-manifest.json). Pas de livre 3D, d’évaluation de fluidité mobile ni d’acceptation humaine inventée |
| Composition / mouvement | Carte 220 × 330 centrale, tête/nuages/eau bougent dans 80 images / 7,89 s ; revue indépendante | [Vue](qa-2026-10-20/role-revised.png), [GIF](qa-2026-10-20/role-motion-revised.gif). Les autres cartes, retournement complet et fluidité/performance sur téléphone ne sont pas validés |
| Narration / vocal | Treize annonces publiques livrées, droits et arrêts testés ; transport vocal natif testé | [Narration](NARRATION-PUBLIQUE.md). Aucune écoute de groupe, mesure d’écho ou permission micro réelle sur appareil validée ; WebGL sans micro natif |
| Build Mac actuel | **Compilation réussie**, zéro erreur, 346 avertissements AI, 170 507 891 octets, 15,173 s ; plist lint et codesign local deep/strict vérifiés | [Rapport actuel](qa-2026-10-20/build-mac-hub-motion.json). Lancement annoncé par la CLI, processus enfant exact à vérifier ; aucune UI native actuelle validée (outil de contrôle indisponible). Signature locale ad hoc, pas Developer ID/notarisation |
| GitHub | Dernier commit/push encore attendu | Branche `codex/werewolf-playable` ; aucun déploiement public déduit d’un futur push |
| Appareils / Internet / groupe | Non validés | Aucun iOS/Android signé, hébergement public, deux téléphones, partie de novices ou expérience acoustique complète revendiqués |

Limites visuelles encore ouvertes : dans le vote développé, la seconde ligne de cibles est partiellement coupée au bord du contenu défilant ; l’indice de défilement pourrait être plus explicite. Le compteur de distribution sur roches/reflets était peu contrasté lors de la revue précédente ; vérifier sa présentation actuelle. Le petit pied de hub reste discret sur les objets décoratifs ; le fond universel est livré localement et relu, mais n’est pas encore accepté par Mathis. Ces points ne sont pas masqués par les tests verts.

La couverture des gestes nocturnes par une activité commune à durée fixe reste **proposée, non implémentée**, sans preuve contre la déduction physique des rôles. Huit extensions illustrées restent sans moteur ; Président/Mimes sont en préparation.

Le [suivi de production](SUIVI-PRODUCTION.md) détaille les propriétaires et écarts ; la [recette du joueur](RECETTE-JOUEUR.md) définit les essais restant à mener. Les rapports antérieurs (26/29, 38/39 + retests, 45/45, 55/55) restent des archives de leur version. La campagne 56/56 est une exécution complète distincte ; le hub animé reçoit ensuite dix tests ciblés, pas une nouvelle campagne globale.

## Archive du premier aperçu du 17 octobre

Le contenu historique ci-dessous décrit ce premier aperçu seulement, y compris des éléments supprimés depuis comme la lanterne indépendante. Ses anciens comptes de tests ne sont pas le bilan actuel.

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
