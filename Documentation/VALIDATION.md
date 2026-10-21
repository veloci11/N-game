# Validation et limites de N’GAME

## Reconstruction complète du village — état courant

Le rendu actif utilise des objets séparés et des géométries, sans PNG complet de village ni `PaperRiver`. Foyers masqués et animés indépendamment, marche du loup en huit poses, vignettes publiques nocturnes, décors privés des pouvoirs, activité Lucioles avec protocole explicite et aide « ? » sont développés. Unity EditMode réussit 125/125 ; le passage PlayMode complet obtient 90/91, puis 19/19 ciblés après correction d’une ancienne assertion de masque : 91 cas uniques couverts par union, pas une campagne unique 91/91. Les finitions visuelles suivantes sont postérieures aux rapports globaux : retrait du voile opaque en nuit écran, taille/trajectoire du loup au pont et lanterne/support à droite. Trois contrôles ciblés actuels réussis (5/5 village, 20/20 vue joueur, 3/3 lucioles) ; captures actuelles relues et build Mac local réussi ; les anciens 70/70 et le build cutouts ci-dessous ne prouvent pas cette passe.

| Contrôle | Résultat actuel | Portée / limite |
| --- | --- | --- |
| Moteur hors Editor | **76/76**, zéro échec — [rapport et commandes](qa-2026-10-20/night-protocol-validation.md) | Protocole écran, Petite Fille, copies/sauvegardes/échéance ; pas un passage Unity EditMode |
| HTTP local isolé | **214 contrôles/requêtes**, zéro échec — [log](qa-2026-10-20/night-protocol-http.log) | Polls compris, pas 214 scénarios humains ; aucun hébergement public |
| Unity EditMode actuel | **125/125**, zéro échec/ignoré/inconclusif, **1,68 s** — [rapport](qa-2026-10-20/editmode-layered-final.json) | Pas une réception humaine ou un essai téléphone |
| Unity PlayMode complet | **90/91**, **162,02 s** — [rapport](qa-2026-10-20/playmode-layered-initial.json) | Seule une ancienne assertion attend 10 masques au lieu de 14 ; fixture corrigée sans changement produit. Retest ci-dessous réussi ; ne pas annoncer une campagne unique 91/91 |
| Unity PlayMode ciblé | **19/19**, zéro échec/ignoré/inconclusif, **17,28 s** — [rapport](qa-2026-10-20/playmode-layered-privacy-final.json) | Comparaison des FullName : union des deux rapports = **91 cas uniques réussis**, aucun cas global non couvert ; pas une campagne unique 91/91 |
| Finitions visuelles ciblées | [Village 5/5](qa-2026-10-20/playmode-layered-village-final.json), 3,79 s ; [vue joueur 20/20](qa-2026-10-20/playmode-layered-player-final.json), 3,58 s ; [lucioles 3/3](qa-2026-10-20/playmode-layered-fireflies-final.json), 0,91 s ; zéro échec/ignoré/inconclusif | Après retrait du voile écran, réglage du loup et lanterne à droite ; ces suites ciblées ne constituent pas un nouveau passage complet de 91 cas |
| Captures courantes | [Aide](qa-2026-10-20/layered-help-question.png), [public](qa-2026-10-20/layered-wolves-public-final.png), [privé](qa-2026-10-20/layered-wolves-private-final.png), [GIF](qa-2026-10-20/layered-night-final.gif) et image 50 relus ; 96 images réelles 430 × 932 sur 9,677 s, sans interpolation — [manifest](qa-2026-10-20/layered-night-motion.json) | Practice avec bots, pas une partie réseau/humaine ; qualité artistique non reçue par Mathis |
| Interaction UI courante | Pointeur simulé Unity : luciole capturée (`LocalCatchCount=1`), cible privée confirmée, `submitted=true` et `ConfirmationVersion=1` | Contrôles réels dans Practice ; pas du toucher sur téléphone |
| [Build Mac courant](qa-2026-10-20/build-mac-layered.json) | Succeeded, **0 erreur**, 346 warnings AI, 295 727 649 octets, 17 995 ms ; plist valide, signature ad hoc `--deep --strict` vérifiée ; processus natif exact 19494 confirmé | Pas de Developer ID/notarisation ni recette UI native complète ; source sauvegardée au commit `9b45359`, SHA distant vérifié |
| Réception réelle | Non réalisée | Partie humaine, téléphones, acoustique, clavier, performance et qualité perçue restent à éprouver |

Le [bilan courant](qa-2026-10-20/delivery-layered-current.md) suit les prochaines preuves. Huit rôles de base seulement ; huit extensions illustrées sans moteur. Petite Fille réservée au protocole traditionnel en présence, jamais au mode écran ou distant. Les appels écran gardent leur échéance même après soumission et refusent la clôture anticipée. Une activité commune n’offre aucune garantie contre la déduction physique des gestes.

## Archive : livraison précédente à éléments séparés

La déformation du visuel entier rejetée par Mathis est remplacée par des éléments générés séparément et animés par transformations rigides. La campagne complète de cette étape **70/70 PlayMode** couvre ce nouveau rendu et les parcours UI. La réception visuelle humaine reste distincte de ces tests. Les rapports antérieurs ci-dessous restent historiques ; le build Mac courant compile et sa signature locale est vérifiée ; le snapshot `ddc74e9` est poussé et son SHA distant est vérifié.

| Vérification | Preuve de cette étape | Limite |
| --- | --- | --- |
| Unity PlayMode de cette étape | **70/70**, zéro échec/ignoré/inconclusif, **118,62 s** — [rapport](qa-2026-10-20/playmode-cutouts-final.json) | Campagne complète unique : UI Loup-Garou et éléments séparés ; pas une partie humaine ni un essai téléphone |
| Unity EditMode antérieur | **116/116**, zéro échec/ignoré/inconclusif, 2,52 s — [rapport](qa-2026-10-20/editmode-reference-final.json) | Cœur inchangé depuis ce passage ; suite non rejouée après les derniers changements de présentation, pas une recette humaine |
| Unity PlayMode | **65/65**, campagne complète unique, zéro échec/ignoré/inconclusif, 79,5 s — [rapport](qa-2026-10-20/playmode-reference-final.json) | Comprend vote compact, observateur et feedback nocturne ; campagne antérieure aux derniers correctifs privé/clavier, complétée par les deux rapports ci-dessous |
| Privé / clavier actualisés | [Passage complet 66/67](qa-2026-10-20/playmode-private-second.json), puis [retest ciblé 15/15](qa-2026-10-20/playmode-private-target-final.json), 1,64 s : **67 cas uniques couverts par union** | Pas une campagne unique 67/67. La fixture du test de raycast clavier attend deux frames après reconstruction ; aucun changement produit après le passage complet |
| Rendu de cette étape | [Accueil](qa-2026-10-20/home-cutouts-final.png), [GIF](qa-2026-10-20/home-cutouts-final.gif), [aperçu local village](qa-2026-10-20/werewolf-cutouts-village-final.png) relus indépendamment ; GIF 64 images réelles sur 6,714 s, image 50 également relue | Aucun P1 visuel bloquant confirmé sur ces captures ; pas une réception Mathis, ni une partie réseau pour l’aperçu village |
| Hub / art antérieur | **11/11 ciblés** avant abandon de la déformation du visuel, après finition des bords — [rapport](qa-2026-10-20/playmode-reference-hub.json). Grand livre, fond papier et atlas de trois vignettes | Captures Unity de l’état antérieur référencées dans le [bilan courant](qa-2026-10-20/delivery-current.md) ; ancien accueil beige rejeté, PNG/GIF historiques |
| Pouvoirs privés | Formulaire compact développé : zone utile élargie, informations critiques avant les choix, quatre cibles par page | Scénario Sorcière safe850 et confidentialité couverts par les rapports actualisés ; autres appareils et gestes humains non validés |
| Vote / Messages | Trois cibles par page ≥44 ; pagination claire et choix conservé, actions publiques avec dock ouvert ; consignes éliminées distinctes | Réservation du haut réel du dock et masquage du formulaire pendant la saisie ajoutés ; sélection et page restaurées à la fermeture du clavier, scénario ciblé réussi. Clavier mobile réel à éprouver. H Messages 140–230, valeur 158 pour zone 850 ; boutons principaux inchangés |
| Gestes nocturnes | Feedback local facultatif : même lueur au bord, aucun rôle/réseau/score ; arrêt phase/menu/focus/pause/réduction testé | Ne garantit pas de cacher les gestes. Yeux fermés hors appel, consigne physique Petite Fille conservée ; pas de nouvelle règle moteur |
| Core / Server | 67/67 Core hors Editor, 553 HTTP partie complète,1016HTTP succession/reprise/voix — [audit indépendant](qa-2026-10-20/core-server-final-audit.md) | Polls inclus ; pas une réception multi-appareils ou acoustique |
| Préparation LAN | 28 contrôles `--check` du lanceur — [log](qa-2026-10-20/lan-launcher-checks.log) | Script préparé seulement, aucun serveur démarré par ces contrôles, aucun téléphone testé |
| Build Mac de cette étape | [Rapport](qa-2026-10-20/build-mac-cutouts.json) : Succeeded, **0 erreur**, 346 warnings AI, 19 281 ms, 212 989 857 octets ; plist lint et signature ad hoc `--deep --strict` vérifiés | Pas de Developer ID/notarisation, pas de recette de l’interface native ; preuve UI dans l’Editor |
| Git | Snapshot `ddc74e9` poussé sur `codex/werewolf-playable`, SHA distant vérifié | Sources sauvegardées ; aucun déploiement public |
| Déploiement / réception | Hébergement public explicitement reporté à plus tard par Mathis | Aucun déploiement prévu maintenant, aucun téléphone/novice/acoustique validé |

Huit rôles de base jouables et huit extensions illustrées sans moteur ; Président/Mimes en préparation. Les anciens résultats 56 globaux et 10 ciblés ainsi que leurs captures restent historiques. Voir le [suivi](SUIVI-PRODUCTION.md) et la [recette joueur](RECETTE-JOUEUR.md).

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
