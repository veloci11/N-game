# N’GAME — plan d’exécution de l’application

Plan initial du 17 octobre 2026. **Reprise du développement explicitement demandée par Mathis le 20 octobre 2026, avec une équipe d’agents.** Le cadrage ci-dessous conserve l’état initial et l’ordre des lots ; les fonctions réellement développées et leurs preuves actuelles sont dans [SUIVI-PRODUCTION.md](SUIVI-PRODUCTION.md) et [VALIDATION.md](VALIDATION.md). Les passages « actuel » ou « à la reprise » du plan initial ne constituent pas un compte rendu de la nouvelle exécution.

Objectif : transformer le prototype en un Loup-Garou compréhensible, beau, animé et finissable entre amis, en présence d’abord, puis à distance. Le plan garde le travail existant et évite de recréer une architecture ou une interface à chaque écran.

Documents de travail : [52 maquettes](../ArtSource/MenuStudies/2026-10-17/README.md), [vues et communications](COMMUNICATIONS-ET-VUES.md), [recette du joueur](RECETTE-JOUEUR.md), [règles du moteur](WEREWOLF-RULESET.md).

## 1. Ce qui est fixé et ce qui reste proposé

| Statut | Élément |
| --- | --- |
| Demandé / retenu par Mathis | Conte en papier, village nocturne, cartes validées et animations détaillées |
| Demandé | Loup-Garou en premier ; Président plus tard |
| Demandé | Présence et distance, priorité aux soirées autour de la table |
| Demandé | Vue personnelle selon le rôle et la phase ; attente animée pour les autres |
| Demandé | Emplacements cohérents des commandes, burger en partie et confirmation de sortie |
| Demandé | Chat écrit ; vocal à distance **uniquement pendant le vote de jour** |
| Demandé | Tester le parcours comme un vrai joueur, avec ses gestes, incompréhensions et interruptions |
| Retenu comme base artistique, pas approbation détaillée de chaque écran | Les 52 maquettes proposées |
| Proposition de ce plan | Architecture de composants, canaux texte, micro coupé par défaut, durées d’essai et lots ci-dessous |

Les règles détaillées proposées ne sont pas silencieusement attribuées à l’utilisateur. Les compléter dans les documents permet une décision concrète au moment de la reprise.

## 2. Point de départ réel

| Existant observé | Portée réelle |
| --- | --- |
| Projet Unity neuf `N-game` | Seul dépôt de travail ; ne pas réutiliser `NGameMobile` |
| Base Git `0bf295c` | Aperçu animé et catalogue déjà enregistrés ; de nombreuses évolutions suivantes restent locales et non commitées |
| Core C# + serveur .NET | Huit rôles, salons, projections privées, tours, votes, réactions et fins de partie dans les sources locales |
| Client Unity/uGUI | Connexion HTTP, salon, partie, entraînement, carte privée et décor animé |
| Catalogue artistique | Seize cartes ; huit rôles ont un moteur, pas seize |
| Rapports sauvegardés | 92/92 EditMode et 19/19 PlayMode dans les rapports présents |
| Vérification HTTP précédente | 452 contrôles exécutés dans la séance précédente ; ce n’est pas une validation ergonomique sur téléphone |
| Évolutions après le rapport PlayMode | Sérialiseur, reprise d’erreur d’entraînement et derniers ajustements nécessitent une nouvelle recette lors de la reprise |
| Chat / vocal | Pas encore implémentés |
| Conservation actuelle | Salons en mémoire serveur et accès en mémoire client ; fermeture complète et redémarrage ne constituent pas encore une reprise durable |
| Mobile / diffusion | Pas de validation sur appareils réels ni de distribution mobile signée revendiquée |

Ne pas confondre maquette, code local, test automatisé, build, essai humain, push et déploiement. Le présent document ne fait aucun de ces développements ou essais.

## 3. Périmètre de la première application jouable

Une personne peut créer une soirée, inviter un groupe, choisir une composition valide, distribuer les secrets, faire jouer les pouvoirs, conduire le jour et les votes, recevoir un résultat cohérent et relancer avec le même groupe.

La première livraison complète garde les huit rôles déjà modélisés : Loup-Garou, Villageois, Voyante, Sorcière, Chasseur, Cupidon, Garde et Petite Fille en présence. Elle ne réduit pas le produit à trois cartes et ne prétend pas que les huit autres sont jouables.

Sont inclus : première prise en main sans compte obligatoire, pseudonyme et portrait, salons, aide contextuelle, variantes de vue, animations, chat, traitement du vocal distant limité au vote, erreurs compréhensibles et reprise. L’entraînement solo est explicitement distinct du multijoueur.

Après cette base : rôles avancés et modules selon leurs dépendances ; Président après validation artistique ; autre jeu de soirée hors ligne ensuite. Achats, publicité, réseau social, messages directs et comptes élaborés ne sont pas ajoutés pour remplir les menus.

## 4. Cadre commun de l’interface

Construire un seul cadre et des modules de contenu, **pas 52 scènes indépendantes**.

| Élément | Invariant de conception |
| --- | --- |
| Référence | Portrait 430 × 932 unités ; adaptation à la zone sûre réelle |
| En-tête | 80 unités ; retour avant d’entrer dans un salon, burger du salon à la fin |
| Aide | Même emplacement en haut à droite |
| Chat | Emplacement réservé à gauche de l’aide, sans déplacer le burger ou le titre |
| Contenu | Seul le module central change selon l’état et les droits |
| Action principale | Rangée supérieure du dock : x24 / y780 / 382 × 56 |
| Action secondaire | Rangée inférieure : x24 / y846 / 382 × 48 |
| Attente / indisponibilité | La commande reste dans son emplacement, désactivée avec une raison |
| Carte privée | Même centre et taille entre dos, retournement et recto |
| Groupe | Ordre et emplacement des portraits stables ; décès et connexion changent leur état, pas leur ordre |
| Départ | Entrée séparée du burger, confirmation avec « Rester dans la partie » comme choix principal |

Les coordonnées partent du coin supérieur gauche de la référence. Le prompt de la première planche inverse encore les rangées ; **ce tableau et le README du catalogue sont la référence**, pas une interprétation du prompt A.

La zone sûre, les petits écrans, le texte agrandi et le clavier ont un comportement défini séparément. Un clavier peut ajuster son panneau de saisie ; il ne déclenche pas une réorganisation arbitraire du jeu. Fermer un menu, un chat ou un clavier retrouve le cadre précédent.

### Composants à mutualiser

En-tête, dock d’actions, portrait public et badges, grille paginée de joueurs, carte privée, sélection de cibles, potions, faits privés, événements publics, états de connexion, menu, confirmation, catalogue, aide et conversation.

La scène `Storybook`, `VillageStage`, `LivingSky`, le loup articulé, les shaders, les polices et les cartes servent de base. `RoomFlowView` doit être décomposé progressivement en présentations ciblées derrière le contrat client existant ; le moteur de règles et le transport ne sont pas réécrits pour obtenir de nouveaux menus.

### Ergonomie à résoudre avant la finition

- Les actions actuelles dans une liste défilante doivent rejoindre le dock fixe.
- Le maintien permanent d’un doigt plus sélection de l’autre n’est pas une solution finale satisfaisante à imposer partout. Séparer le coup d’œil à la carte de l’action privée volontairement ouverte ; maquetter l’usage à une main et les interruptions avant de modifier ce comportement.
- Prévoir 6, 8, 12 et 24 joueurs : grille paginée et sélection conservée, pas portraits minuscules ou défilement de tout l’écran.
- Sortir la chronique complète du centre permanent du jeu ; conserver un résumé utile et le village visible.
- Aucun texte essentiel coupé par une troncature générale. Les instructions et erreurs doivent rester lisibles avec un texte agrandi.
- Les portraits publics appartiennent aux personnes ; leur apparence ne change pas avec le rôle secret ou une revanche.

## 5. Vues personnelles et parcours de manche

La [matrice des huit rôles](COMMUNICATIONS-ET-VUES.md#3-matrice-des-pouvoirs-du-premier-lot) définit qui agit, quelles cibles il reçoit et quelles informations sont autorisées. Pendant le même appel de meute, les loups peuvent choisir, les autres attendre et la Petite Fille recevoir son protocole physique.

Déroulement cible :

1. **Salon :** mode, composition, règles et confirmations de préparation.
2. **Explication courte :** accessible de nouveau ; protocole physique explicite en présence.
3. **Distribution :** carte personnelle et confirmation réelle avant de démarrer les pouvoirs temporisés.
4. **Première nuit :** Cupidon si prévu, reconnaissance des amoureux, puis ordre des pouvoirs.
5. **Nuits suivantes :** seulement les actions autorisées, sans oublier les appels nécessaires pour ne pas révéler des rôles cachés par la narration.
6. **Résolution nocturne :** protections, potions, décès, amoureux et réactions selon les règles, puis aube publique.
7. **Discussion :** texte autorisé et échanges physiques en présence ; vocal intégré fermé.
8. **Vote de jour :** sélection personnelle, texte et vocal distant autorisés ; résultat des choix caché jusqu’à clôture.
9. **Égalité :** second tour entre les candidats autorisés, sinon résultat du scrutin.
10. **Verdict lisible :** afficher le résultat avant de démarrer la prochaine action temporisée ; toute réaction due reste prioritaire sur une victoire.
11. **Résultat et revanche :** révélations finales autorisées, remise à zéro des secrets et même groupe.

Il faut distinguer états du moteur et sous-états d’écran. Sélection, maintien, retournement et ouverture d’un panneau sont locaux. Attribution, confirmation d’action, vote, décès et progression de phase appartiennent au serveur.

## 6. Écarts à traiter entre maquettes et moteur

| Référence | Manque ou risque actuel | Travail à prévoir |
| --- | --- | --- |
| D13 | Les pouvoirs commencent immédiatement, sans confirmation de lecture préalable | Étape de distribution et confirmations individuelles côté serveur ; règle d’absence explicite |
| E17 | Choix des loups et propre soumission surtout décrits en texte | Données structurées : cible personnelle, partenaires autorisés, propositions, droit de modifier |
| E19 | L’enquête arrive au changement de tour | Résultat privé persistant et consultable sans refaire agir la Voyante |
| E20 | Motifs de cible indisponible peu structurés | Raisons lisibles et autorisées, sans déduire un rôle caché |
| F21/F22 | Soin et poison doivent être une seule intention quand les deux sont utilisés | Construire une commande unique, confirmer son effet et empêcher tout doublon |
| F24 | « J’ai compris » peut être confondu avec une avance globale | Confirmation locale ou personnelle ; pas de saut de phase pour toute la salle |
| H30 | Compteur public et rappel de mon vote absents sous forme dédiée | Nombre de votes reçus et propre choix, sans résultat des autres avant clôture |
| H31 | Numéro de scrutin non structuré dans la projection | Scrutin, candidats et règle d’égalité explicites |
| H32 | Le moteur enchaîne immédiatement vers la suite | Verdict structuré et temps de lecture autoritaire avant le prochain délai ; préserver les réactions |
| J38 | Fermer l’accès local n’est pas un abandon serveur défini | Politique de départ, place réservée, reprise et succession de l’hôte |
| J39/J40 | Reprise limitée à la vie du processus | Reprise d’identité durable et état récent, sans nouvelle carte ni double action |
| C12/K43 | Les images initiales prévoient seulement un appel externe | Compléter les maquettes selon la règle vocale nouvellement demandée |

Compléments de données à spécifier : identifiant de manche, heure serveur, état de découverte de sa carte, action/cible personnelles enregistrées, réserves de potions, scrutin, progression et résultat public du vote, capacités de communication. Ne pas parser des phrases françaises pour piloter la logique.

## 7. Communications et temps de vote

Le détail des canaux, refus, contrats et plateformes est dans [COMMUNICATIONS-ET-VUES.md](COMMUNICATIONS-ET-VUES.md). Conserver la distinction entre la demande confirmée et les choix proposés pour Village, Meute et Observateurs.

Le texte se branche sur le serveur .NET et l’identité de salle. Le vocal passe par une interface fournisseur après une preuve technique courte. Il ne remplace pas le serveur de règles et ne nécessite pas de synchroniser les positions des éléments décoratifs.

Le vote actuel se ferme dès la dernière confirmation. **Proposition à essayer :** fenêtre distante de 90 secondes, clôture anticipée par l’hôte seulement après 30 secondes et tous les votes reçus, sinon échéance automatique. Le micro démarre coupé et se referme à chaque fin de scrutin. Ces durées restent des paramètres d’essai, pas une décision utilisateur ou un comportement déjà codé.

Une panne du vocal ne signifie pas que la partie est déconnectée. Le joueur doit pouvoir continuer en texte. La fermeture d’une fenêtre doit réellement arrêter capture/transmission et révoquer les droits ; ne pas traiter cela comme un simple état d’icône.

## 8. Plan de mouvement et production des assets

| Élément / état | Mouvement prévu | Contrainte |
| --- | --- | --- |
| Ciel | Lune indépendante, nuages à vitesses différentes, scintillement discret | Pas de glissement global du décor |
| Eau | Ondulation, petites rides et reflets localisés | Les pierres et le pont restent fixes |
| Lumières | Fenêtres et lampes ancrées, cœur de flamme et halo irréguliers | Aucune lanterne sans support ; extinction progressive au jour |
| Village | Brume, fumée de cheminée, feuillage et profondeur douce | Une géométrie maîtresse nuit/aube, sans double objet ni trou |
| Loup | Respiration, tête, oreilles, regard et plis | Déformations locales ; pas un simple déplacement de tout le PNG |
| Carte | Arrivée, retournement, portrait, reflets de dorure | Cadre et texte lisibles, masquage immédiatement prioritaire |
| Voyante | Globe, lueurs et dévoilement de la carte | Effet privé dans la fenêtre autorisée |
| Sorcière | Vapeur, liquide et lumière des fioles, état utilisé | Ne pas confondre effet graphique et consommation serveur |
| Cupidon / amoureux | Fil doré et liaison de médaillons | Visible seulement aux destinataires autorisés |
| Garde / Chasseur / Petite Fille | Motifs spécifiques, respiration et petites articulations | Aucun signe public désignant leur détenteur |
| Attente | Eau, nuages, lumières, phrase claire et temps restant | Jamais écran vide ou animation identifiant qui agit |
| Vote | Sélection brève, sceau de confirmation, attente puis verdict | Cibles et textes immobiles ; choix des autres non révélé trop tôt |
| Résultat | Une courte mise en scène du camp gagnant | L’interface devient utilisable sans attendre une longue cinématique |

Produire un décor maître propre, fonds réparés derrière les éléments mobiles, couches transparentes, masques d’eau et de lumière et pivots documentés. Chaque livraison artistique indique dimensions, alpha, ancrage, ordre des plans et correspondance nuit/aube. Conserver les cartes approuvées ; décomposer progressivement les personnages nécessaires.

Ne jamais intégrer une image d’écran contenant déjà ses boutons et textes à la place d’une vraie interface. Les images générées servent à valider la composition, pas de capture figée du produit.

Prévoir un mode de mouvements réduits complet. Objectifs de travail à mesurer sur appareils : 60 images/s sur appareils principaux, possibilité d’un mode allégé à 30 ; aucune mesure mobile n’est encore déclarée. Profiler mémoire, transparence, chauffe, batterie et chargement avant de multiplier les effets.

## 9. Départ, reprise et cas difficiles

| Situation | Comportement proposé |
| --- | --- |
| Retour de menu / chat / aide | Même phase, sélection valide conservée, secrets masqués |
| Faux appui sur le burger | Ouvre seulement le menu ; aucune sortie ni action serveur |
| Départ en salon | Retrait explicite, composition à revoir et nouvelles confirmations si nécessaire |
| Départ en manche | Place et rôle réservés ; absences traitées selon les règles ; aucune carte redistribuée |
| Départ de l’hôte | Définir un successeur parmi les membres présents ; les délais continuent sans accorder de secrets supplémentaires |
| Perte réseau | Dernier état indiqué comme ancien, actions désactivées, tentative de reprise ; aucun vote simulé côté client |
| Fermeture complète | Reprise de la même identité si son accès durable est valide ; aucune récupération sur simple pseudo |
| Redémarrage serveur | Restaurer un état cohérent ou annoncer honnêtement une indisponibilité ; pas de nouvelle partie déguisée en reprise |
| Permission micro refusée / panne voix | Texte et jeu utilisables, micro fermé |
| Revanche | Nouvel identifiant de manche, nouveaux secrets ; pas de vieux brouillon ou message privé recyclé |

Les choix de départ/succession et de conservation sont à figer avant implémentation. Pour la robustesse, isoler le stockage et prévoir des écritures atomiques de l’état autoritaire et des reçus de commandes. Le choix du stockage durable et de l’hébergement vient après la preuve locale ; aucune migration ou souscription n’est autorisée par ce document.

## 10. Ordre de réalisation à la reprise

Chaque lot produit un résultat visible, des données définies et une recette du joueur. Les numéros sont un ordre de dépendances, pas une estimation de durée.

| Lot | Livrable futur | Dépendances | Critère pour passer au suivant |
| --- | --- | --- | --- |
| 0 — Réconcilier la base | Inventaire du travail local, rapports et changements depuis le dernier test ; sauvegarde sélective | Aucun | État connu ; rien d’ancien repris ou écrasé ; vérifications appropriées relancées à la reprise |
| 1 — Figer le contrat UX | Cadre, permissions, départ, fenêtre de vote ; compléments N/O ciblés à maquetter | 0 | Vues simultanées et chat lisibles ; boutons et micro sans ambiguïté |
| 2 — Compléter les données | Distribution, actions structurées, progression/scrutin/verdict, temps serveur | 1 | Tests de règles et projections privées ; données suffisantes pour les écrans |
| 3 — Construire le cadre commun | Burger, confirmation, dock, grille 6–24, panneaux et interactions privées | 1 | Parcours à une main et clavier validés ; aucune sortie accidentelle |
| 4 — Relier les vues de joueurs | Actif, attente, choix enregistré, réaction, éliminé | 2 + 3 | Plusieurs clients au même tour affichent les bonnes vues, sans secret supplémentaire |
| 5 — Finir le cycle de partie | Distribution jusqu’au verdict, résultats et revanche | 4 | Partie complète avec les vrais contrôles, y compris égalité, potions, couple et Chasseur |
| 6 — Reprise et abandon | Identité, place, erreurs, hôte et sauvegarde adaptés | 2 + 3 | Interruption puis retour sans perte de rôle, double action ou blocage |
| 7 — Chat écrit | Permissions pures, historique, envoi, brouillons, clavier et erreurs | 2 + 3 | Bonne conversation et bons refus côté serveur ; aucun brouillon privé publié ailleurs |
| 8 — Preuve vocale | Fournisseur accessible, plateformes, jetons serveur et vraie révocation | 1 | Entrée/sortie de canal et fermeture hors vote démontrées ; pas seulement une connexion réussie |
| 9 — Vocal dans le vote | Commandes micro fixes, temps de parole, changements de droits et secours texte | 5 + 7 + 8 | Micro réellement fermé hors vote distant, sur refus et sur opérations tardives |
| 10 — Art et mouvement final | Couches propres, personnages, sons autorisés et réglages | Cadre figé ; production possible en parallèle | Comparaison à la DA, attente agréable, pas de défaut de lisibilité ou de fuite par un effet |
| 11 — Recette de soirée | Présence 8–10 joueurs puis distance, cas 6/12/24, interruptions | 5–10 | Aucun blocage critique ; résultats cohérents ; parcours compris sans explication orale permanente |
| 12 — Bêta mobile | Builds adaptés, distribution de test, mesures et corrections | 11 | Essais iOS/Android réels, permissions, chauffe, performances et reprise documentés |
| 13 — Extensions | Autres rôles, autres jeux et distribution publique | Base fiable | Chaque module a ses règles, son interface, ses assets et une partie finissable |

Ne pas attendre toute la finition artistique pour essayer le parcours ; ne pas masquer un parcours cassé avec des animations. La recette humaine commence dès le cadre et accompagne chaque lot.

## 11. Travail parallèle pour aller plus vite

| Chantier | Responsabilité principale | Limite d’édition |
| --- | --- | --- |
| Règles / données | États, projections, commandes et scénarios | Core et tests de règles |
| Interface | Cadre, modules, sélection, privé, clavier et accessibilité | Présentation ; pas de changement de règle dans l’UI |
| Services | Sessions, chat, stockage et coordinateur vocal | Serveur et adaptateurs ; secrets fournisseur côté serveur |
| Art / mouvement | Couches, masques, personnages et transitions | Assets et composants visuels après contrat de gabarit |
| Intégration / recette | Parcours humain et plusieurs appareils | Vérification indépendante et corrections coordonnées |

Avant le parallèle : mêmes noms de phase, champs, actions et raisons de refus ; versions des contrats connues. Un fichier a un propriétaire à la fois. Les tâches se terminent avec références aux fichiers, comportement attendu, scénario essayé et limites connues, pas avec « ça compile ».

Gains recherchés : composants partagés, catalogue de rôles piloté par données, même moteur pour entraînement et multijoueur, réemploi de la DA validée, exemples de messages et scénarios déterministes pour les tests. Aucun changement de framework ou nouvelle DA à chaque lot.

## 12. Conditions d’une livraison réellement jouable

- Une personne qui découvre l’application peut rejoindre, comprendre ce qu’elle voit et effectuer la bonne action sans être coachée à chaque écran.
- Chaque rôle agit dans sa fenêtre ; les autres ont une attente animée et un temps compréhensible.
- Les secrets restent privés dans les vues, API, canaux, interruptions et reprise.
- Le Chasseur et les amoureux sont résolus avant la victoire ; une partie mène au même résultat chez tous.
- Les boutons gardent leurs zones et leur fonction est claire ; un envoi de chat n’envoie jamais un vote.
- Le vocal respecte exactement le vote distant, y compris aux frontières de phase et au second tour.
- Le jeu reste utilisable sans son, avec micro refusé, avec mouvements réduits et sur petit écran.
- La partie et la revanche ont été parcourues via les vrais contrôles et sur plusieurs appareils ; les rapports de règles seuls ne suffisent pas.
- Les défauts critiques ou gênants de la [recette du joueur](RECETTE-JOUEUR.md) sont corrigés et le parcours concerné est rejoué.

## 13. Prochain travail lorsqu’on reprendra le code

Commencer par les lots 0 et 1. Le premier résultat à montrer est une même étape de nuit sur plusieurs joueurs : rôle actif, autre rôle qui attend, éliminé et hôte, avec le burger et le dock figés. Le chat et le vocal viennent se brancher sur ces droits ; ils ne déterminent pas rétroactivement l’architecture du jeu.

Ce document est un plan prêt à suivre. Il ne constitue ni une validation des écrans restants, ni un lancement de développement, ni une autorisation de créer des comptes ou de publier l’application.
