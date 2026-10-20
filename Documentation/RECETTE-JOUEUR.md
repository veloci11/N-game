# N’GAME — recette vécue par le joueur

Recette initiale du 17 octobre 2026. **Le développement et la recette technique ont repris le 20 octobre.** Les résultats exécutés sont dans [VALIDATION.md](VALIDATION.md) et [SUIVI-PRODUCTION.md](SUIVI-PRODUCTION.md) ; ce document décrit les scénarios, sans préremplir leur réussite. Aucun essai humain novice ou sur téléphone n’est déclaré réussi par les tests automatisés.

Cette recette accompagne chaque lot du [plan d’exécution](PLAN-EXECUTION.md). Elle complète les tests du moteur : une règle correcte ne prouve pas qu’un joueur comprend l’écran ou arrive à utiliser son pouvoir. Les [droits de communication](COMMUNICATIONS-ET-VUES.md) et le [jeu de règles](WEREWOLF-RULESET.md) servent de références pour les résultats attendus.

## 1. Méthode : jouer, observer, corriger, rejouer

Le testeur doit passer par les vrais boutons, le clavier, les gestes privés et les menus de la version testée. Appeler directement un contrôleur, forcer une phase ou envoyer une commande par API est utile pour tester le moteur, mais ne valide pas ce parcours.

Pour un essai de découverte, donner seulement un objectif : « Rejoins tes amis et joue une manche ». Ne pas dicter où appuyer. Le joueur peut consulter l’aide proposée par l’application. L’observateur note les hésitations, les erreurs et les demandes d’explication ; il intervient seulement pour débloquer l’essai, en enregistrant cette intervention comme une difficulté. Distinguer une règle de Loup-Garou non connue d’une consigne mal présentée par l’application.

Combiner trois niveaux d’évidence :

| Niveau | Ce qu’il permet de conclure | Limite |
| --- | --- | --- |
| Parcours par l’agent via l’interface réelle | Boutons utilisables, enchaînement, erreurs reproductibles, données et vues cohérentes | L’agent connaît déjà le fonctionnement ; ce n’est pas une preuve de découverte intuitive |
| Essai humain sans accompagnement permanent | Compréhension, gestes naturels, confusion, confort à une main et rythme social | Une personne ne couvre pas tous les rôles ou appareils |
| Partie sur plusieurs appareils | Synchronisation vécue, attente réelle, communications, interruptions et fin commune | Un petit groupe ne prouve pas l’équilibrage de tous les effectifs |

Ne pas attendre le dernier lot pour tester. Un build de développement sur téléphone est nécessaire dès que le cadre tactile est disponible ; la préparation d’une bêta distribuable arrive ensuite. Une simulation solo ou plusieurs fenêtres sur le même Mac ne remplacent pas deux vrais téléphones.

## 2. Points de vue à couvrir

| Personne / situation | Question à laquelle l’essai doit répondre |
| --- | --- |
| Hôte qui découvre l’app | « Puis-je préparer la soirée sans connaître les paramètres techniques ? » |
| Ami invité novice | « Comment rejoindre, savoir si je suis prêt et comprendre mon rôle ? » |
| Joueur avec un pouvoir | « Est-ce mon tour, qui puis-je choisir et mon action est-elle enregistrée ? » |
| Villageois qui attend | « Est-ce normal que je ne puisse rien faire, et quand dois-je regarder ? » |
| Joueur éliminé | « Pourquoi suis-je sorti, que puis-je encore voir et faire ? » |
| Joueur distant sans micro | « Puis-je participer et voter sans donner la permission audio ? » |
| Joueur interrompu | « Vais-je retrouver ma place, mon rôle et mon action sans tout recommencer ? » |
| Joueur à une main / texte agrandi | « Puis-je agir sans deux doigts obligatoires, lire et fermer les panneaux ? » |

## 3. Parcours complet, du premier lancement à la revanche

Pour chaque scénario, l’observateur conserve l’objectif et le résultat attendu. Le participant de découverte ne reçoit pas les étapes comme un tutoriel extérieur.

| ID | Objectif et gestes à faire dans l’app | Résultat attendu / question à observer |
| --- | --- | --- |
| J01 — Entrer | Ouvrir l’app, choisir le Loup-Garou, lire le principe, définir pseudo et portrait | Une action de départ évidente ; aucun compte obligatoire ; pas de fonction annoncée jouable alors qu’elle manque |
| J02 — Créer | Créer une soirée en présence, trouver son code et inviter un ami | L’hôte comprend le mode choisi et comment transmettre l’accès ; aucune information privée dans l’invitation |
| J03 — Rejoindre | Sur un autre téléphone, saisir un code erroné puis le bon ; corriger son pseudo si nécessaire | Erreur précise et saisie conservée ; retour simple ; apparition de la bonne personne dans le bon salon |
| J04 — Préparer | Lire les règles, ajuster la composition autorisée, se déclarer prêt et essayer de lancer trop tôt | Le refus explique ce qui manque ; un changement important invalide les préparations concernées ; seul un hôte autorisé peut lancer |
| J05 — Recevoir son rôle | Découvrir sa carte, relâcher/masquer, relire son pouvoir, confirmer avoir compris | Secret masqué par défaut ; même emplacement entre dos et recto ; aucune fenêtre de pouvoir ne se termine pendant cette première découverte |
| J06 — Attendre | Rester au moins une fenêtre complète sans pouvoir actif, consulter l’aide puis revenir | Attente identifiable, animation vivante, indication utile ; aucune impression de chargement bloqué ; aucun bouton invitant à jouer un autre rôle |
| J07 — Agir | À son appel, lire, choisir une cible, changer une sélection locale puis confirmer | Cibles autorisées compréhensibles, distinction sélection / enregistrement ; confirmation visible, pas de double consommation ; emplacement des commandes stable |
| J08 — Voir l’aube | Suivre les événements publics, les décès et une éventuelle réaction | Le joueur comprend les conséquences autorisées ; temps de lecture avant la prochaine action temporisée ; pas de saut direct qui escamote un pouvoir |
| J09 — Discuter | Lire le bilan ; échanger autour de la table en présence ; à distance, ouvrir ensuite le vote pour communiquer | Aucun accès messages/historique ni vocal pendant Discussion ; passage au vote compréhensible |
| J10 — Voter | Développer Messages, lire/écrire, choisir un vivant et confirmer avec le dock ouvert, puis attendre | Dock du bas semi-transparent de 230 unités maximum ; commandes publiques utilisables, carte masquée ; compteur sans publier les choix avant clôture ; fermeture du dock à la sortie du vote |
| J11 — Départager | Jouer un second tour provoqué par une égalité, puis une nouvelle égalité | Candidats et raison du second tour explicites ; ancien choix non réutilisé automatiquement ; absence d’élimination expliquée si prévue |
| J12 — Comprendre le verdict | Lire le résultat du scrutin et l’identité publiable de l’éliminé ; effectuer un dernier tir si dû | Résultat commun, lisible ; réaction avant victoire ; aucune action du joueur éliminé ne reste activable hors de ses droits |
| J13 — Finir | Continuer jusqu’à la victoire ou l’égalité finale et consulter les révélations autorisées | Même gagnant et même explication sur tous les appareils ; le récit correspond aux règles et aux actions enregistrées |
| J14 — Rejouer | Relancer avec le même groupe et consulter son nouveau rôle | Même identité publique, nouveaux secrets ; aucun vote, message privé ou brouillon de la manche précédente recyclé |

Faire ensuite le parcours distant. Les pouvoirs et les boutons restent cohérents ; les différences de protocole physique et de vocal sont annoncées avant de jouer. Le déroulement à huit est le premier essai de soirée, pas une preuve que tous les formats sont équilibrés.

## 4. Même instant, plusieurs joueurs

Utiliser une salle de test déterministe et plusieurs clients distincts. Au même identifiant de manche, même tour et même phase, comparer les écrans et les données autorisées :

- Loup vivant : meute privée, partenaires autorisés et cibles ; après soumission, choix enregistré et modification seulement si la règle le permet.
- Villageois vivant : attente animée, sa propre carte et aucune commande de meute.
- Voyante ayant joué : attente et découverte privée encore consultable, sans seconde enquête.
- Sorcière : aucune victime ou potion avant son appel.
- Éliminé : informations publiques, aucun secret supplémentaire ; réaction Chasseur séparée si elle est encore due.
- Hôte : mêmes droits secrets que son rôle ; ses commandes d’organisation n’ouvrent pas les autres vues.

Une capture d’ensemble peut montrer les différences pour la recette interne. Ne pas intégrer ces captures dévoilant les cartes à une aide publique de la partie en cours. Le son, l’avatar public et les notifications ne doivent pas désigner le rôle qui agit.

## 5. Gestes propres aux huit rôles de départ

Les configurations déterministes sont réservées aux outils de test ou à une salle de recette explicitement isolée. Ne pas ajouter au client de production un choix libre de son rôle ou une commande pour sauter les phases.

| Rôle / cas | Manipulation réelle | Résultat à constater |
| --- | --- | --- |
| Loup-Garou | Deux loups proposent des cibles différentes puis révisent avant clôture | Choix visibles seulement à la meute autorisée ; résolution conforme aux règles ; aucun raccourci de clôture incompatible avec la Petite Fille |
| Villageois | Traverser une nuit sans pouvoir | Comprendre qu’attendre est normal ; aucune fausse sélection ; vote utilisable au jour |
| Voyante | Choisir une cible, confirmer, attendre la résolution puis rouvrir les faits privés | Résultat exact prévu par le jeu de règles ; résultat persistant au changement de tour ; pas de nouvelle enquête |
| Sorcière — soin | Choisir sauver sans désigner de cible de poison | Action possible sans cible artificielle ; une potion consommée une seule fois |
| Sorcière — poison / deux potions | Sélectionner le poison seul puis, dans un autre scénario, soin et poison ensemble | La cible concerne le poison ; les deux potions forment une seule commande ; un double appui ne produit pas deux effets |
| Sorcière — épuisement | Revenir la nuit suivante avec une ou deux potions indisponibles | Réserves et raisons lisibles ; aucun faux pouvoir ; appel public conservé quand nécessaire pour ne pas révéler son état |
| Cupidon | Sélectionner deux vivants distincts, retirer une sélection, confirmer | Compteur et sélection clairs ; pas d’envoi incomplet ; lui-même possible selon les règles |
| Amoureux | Lire la reconnaissance puis fermer son panneau ; provoquer ensuite le décès d’un partenaire | Partenaire autorisé, pas sa carte ; « J’ai compris » ne saute pas une phase pour tous ; chaîne de décès et réaction avant victoire |
| Garde | Se protéger puis tenter de choisir la même cible la nuit suivante | Cible interdite avec raison compréhensible ; les restrictions ne dévoilent pas un autre rôle |
| Chasseur | Être éliminé de nuit, au vote ou par décès amoureux ; viser puis confirmer | Dernière action accessible malgré son état mort ; cibles valides ; victoire calculée après cette réaction |
| Petite Fille, présence | Lire sa consigne, vivre réellement la fenêtre de meute à table | Protocole compris, aucune liste numérique de loups ; aucune lumière/notification privée qui trahit son rôle dans la pièce |
| Petite Fille, distance | Essayer de la sélectionner lors de la composition | Indisponibilité expliquée ; aucune fausse version distante ajoutée silencieusement |

Les protections, le poison, les morts simultanées et toutes les conditions de victoire ont aussi leurs tests de règles. Rejouer par l’interface les cas dont l’ordre ou l’explication affecte l’expérience ; ne pas considérer un rapport moteur comme une preuve de compréhension.

## 6. Conversation, clavier et vocal du vote

### Texte et panneaux

| ID | Essai | Résultat attendu |
| --- | --- | --- |
| C01 | Au vote, ouvrir Messages après une sélection de cible ; écrire puis confirmer le vote | Envoyer ne vote pas ; le dock développé ne bloque ni cible ni confirmation publique ; carte privée masquée |
| C02 | Éliminé, rédiger dans Observateurs, perdre les droits ou changer de manche, puis revenir sur Village | Aucun transfert du brouillon privé, aucune réutilisation sur la nouvelle manche ; dock fermé lors de la révocation |
| C03 | Envoyer à la clôture du vote ou tenter un envoi Village après élimination | Nouvel envoi interdit refusé ; ancien reçu accepté éventuellement acquitté sans contenu ; aucun canal/historique hors vote |
| C04 | Couper le réseau juste après Envoyer puis réessayer | Message non dupliqué ; conservation de son identité d’envoi ; état incertain explicite |
| C05 | Tout rôle tente Meute ; un vivant tente Observateurs ; tous tentent historique hors vote | Aucun contenu, canaux refusés aussi côté serveur ; l’hôte ne contourne pas les ACL |
| C06 | Écrire avec clavier sur petit écran, texte agrandi et une seule main | Saisie et fermeture atteignables ; nom du canal visible ; aucun maintien à deux doigts imposé pendant toute la rédaction |
| C07 | Recevoir des messages en lisant l’historique | Lecture conservée, retour aux derniers messages compréhensible ; aucune notification publique révélant une activité secrète |

Contrat actuel : pendant le vote, vivants Village lecture/écriture ; éliminés Village lecture seule et Observateurs lecture/écriture. Aucun canal Meute, message ou historique hors vote. Le [rapport serveur actuel](qa-2026-10-20/chat-vote-only.md) est une preuve HTTP, pas une recette UI ou humaine.

### Voix : uniquement le vote de jour à distance

| ID | Essai | Résultat attendu |
| --- | --- | --- |
| V01 | Parcourir salon, distribution, nuit, aube, Discussion, réaction et résultat | Aucun vocal ouvert. Vérifier le canal et la capture, pas seulement l’icône |
| V02 | Entrer dans un vote distant, laisser le micro coupé puis l’activer volontairement | Écoute autorisée selon les droits ; personne n’émet automatiquement ; état micro compréhensible |
| V03 | Refuser la permission système ou ne jamais activer son micro | Texte, vote et suite de partie pleinement utilisables ; refus non bloquant |
| V04 | Tous confirment très tôt pendant qu’une personne parle | Respect de la fenêtre de parole retenue ; l’ancien raccourci « dernier vote = clôture immédiate » ne coupe pas le vocal à l’insu du groupe |
| V05 | Clôture normale ou anticipée autorisée, puis second scrutin | Arrêt réel de transmission/capture et fermeture coordonnée ; au nouveau scrutin, micro de nouveau coupé jusqu’à activation volontaire |
| V06 | Accepter la permission système après la fin du vote ; laisser arriver une ancienne connexion | Aucune réouverture tardive du micro ; droits et génération du vote courant vérifiés |
| V07 | Verrouiller, changer d’app, perdre le réseau puis revenir pendant le vote | Secret masqué, capture arrêtée ; retour silencieux ; aucune activation automatique |
| V08 | Défaillance du fournisseur ou de la fermeture de canal | Jeu et texte récupérables ; défaut de fermeture bloquant pour la validation du vocal, pas dissimulé derrière un bouton grisé |
| V09 | Être éliminé, utiliser une réaction et observer un vote | Aucune émission vers les vivants ; l’écoute éventuelle respecte la politique retenue ; la réaction de rôle reste indépendante |
| V10 | Refaire ces étapes en mode présence, avec casque/Bluetooth puis sans accessoire en distance | Aucun vocal intégré en présence ; routage audio et coupure vérifiés sur les plateformes réellement supportées |

Les durées de 90 secondes et de 30 secondes minimum sont des propositions d’essai du plan, pas des résultats validés. Mesurer les coupures, les retards et l’audio résiduel ; ne pas annoncer une coupure physique à zéro milliseconde. Le fournisseur et ses limites doivent être vérifiés avant de promettre le même comportement sur chaque plateforme.

## 7. Navigation, erreurs et interruptions

| ID | Situation provoquée | Comportement à vérifier |
| --- | --- | --- |
| E01 | Appui involontaire sur le burger, ouverture Aide, fermeture du panneau | Aucun départ ; secrets masqués ; retour à la partie et action encore valide |
| E02 | Choisir Quitter puis Rester ; tenter double appui et retour système | Confirmation distincte ; Rester prioritaire ; une ouverture de menu ou fermeture de clavier ne quitte jamais la salle |
| E03 | Code invalide, salon plein, déjà commencé, chargement lent | Motif utile, sortie claire, saisie conservée quand pertinente ; aucune partie vide ou invitation trompeuse |
| E04 | Réseau interrompu avant / après confirmation d’un pouvoir ou vote | Dernier état signalé ancien, pas de faux succès ; à la reprise, état autoritaire et commande unique |
| E05 | Coupure courte puis au-delà de la fenêtre d’action | Même identité et même carte ; délai manqué expliqué ; pas de retour en arrière du moteur |
| E06 | Notification, app en arrière-plan, verrouillage ou appel entrant | Carte et chat privés masqués dès interruption ; aucun geste maintenu ni micro réactivé au retour |
| E07 | Fermer complètement l’app et la rouvrir | Reprise de la même identité si elle est encore autorisée ; pas de nouvelle carte ou récupération par simple pseudo |
| E08 | L’hôte se déconnecte puis quitte explicitement | Distinguer absence et départ ; succession conforme au choix retenu, aucun blocage indéfini ni accès secret ajouté |
| E09 | Redémarrer le serveur dans une salle d’essai | Reprise durable cohérente ou message honnête d’indisponibilité ; jamais une nouvelle manche présentée comme l’ancienne |
| E10 | Ouvrir deux fois le même accès ou rejouer un ancien envoi | Politique de session explicite ; pas de double joueur, double potion ou double vote |
| E11 | Délai atteint pendant un panneau privé ou un menu | Vue mise à jour, sélection périmée neutralisée, raison lisible ; pas d’action appliquée au tour suivant |

E07 et E09 restent des exigences de réception : conservation durable et succession existent désormais et ont des tests techniques, mais leur réussite ne remplace pas le parcours réel de reprise sur appareil. Voir le suivi pour les preuves et limites.

## 8. Lisibilité, gestes et attente animée

- Comparer les gabarits de largeur logique 360, 390 et 430, les zones sûres et les claviers ; tester les appareils réellement disponibles et noter leur modèle.
- Conserver les zones du burger, de l’aide, de la barre Messages et des deux commandes. Dock messages limité à 230 unités, au-dessus de 225 unités. Seul le dock suit le clavier ; celui-ci peut recouvrir physiquement le pied de page : confirmation neutralisée pendant la saisie, puis fermer le clavier et confirmer. Vérifier absence de clic accidentel et retour aux mêmes coordonnées.
- Utiliser noms longs, accents, grands caractères et plusieurs joueurs avec portraits proches. Aucun texte indispensable tronqué ; état sélectionné lisible sans dépendre seulement de la couleur.
- Examiner 6, 8, 12 et 24 participants : pagination, ordre stable, sélection conservée et compteur exact. Une vérification de capacité/affichage à 24 n’est pas une validation d’équilibrage à 24.
- Faire les gestes à une main : révéler/masquer, cibler, sélectionner deux amoureux, choisir ses potions, ouvrir le chat et revenir. Si une action privée exige durablement deux mains sans alternative compréhensible, enregistrer un défaut ergonomique.
- Traverser une attente réelle d’au moins 30 secondes et une fenêtre de nuit complète : nuages, lune, eau, flammes et carte vivent sans bouger les textes ou cibles ; pas de lanterne flottante ni animation publique qui indique un rôle secret.
- Désactiver le son puis activer les mouvements réduits : toutes les instructions, délais, confirmations et erreurs restent perceptibles ; aucune action n’attend inutilement une animation.
- Vérifier les contrastes au jour et de nuit, le retour de focus, les vibrations optionnelles et les informations qui seraient autrement données seulement par un son.

## 9. Ordre des séances et critères de passage

| Moment | Séance prévue | Passage au lot suivant |
| --- | --- | --- |
| Cadre et navigation disponibles | Deux appareils, créateur et invité ; novice si possible ; menu, privé, une main et clavier | Pas de sortie accidentelle, commandes atteignables, retour aux bons emplacements |
| Vues et cycle disponibles | Salles déterministes, tous les rôles et perspectives, vraie action jusqu’à une fin puis revanche | Aucun pouvoir incompréhensible ou bloquant ; même issue pour tous ; réactions respectées |
| Reprise et texte disponibles | Interruptions, brouillons, erreurs d’envoi, absence et retour d’hôte | Pas de perte de rôle ni double action ; canaux et erreurs compréhensibles |
| Vocal disponible | Appareils distants, permission accordée/refusée, toutes les frontières de phase | Émission volontaire seulement pendant vote distant ; coupure et retour silencieux démontrés |
| Avant finition complète | Soirée de 8–10 en présence, puis soirée distante ; au moins un participant qui découvre l’app | Une manche complète et une revanche sans coaching permanent ; problèmes observés consignés |
| Avant bêta | Petits/grands effectifs, appareils cibles, longs noms, mouvements réduits et sessions prolongées | Défauts bloquants corrigés, reprises vérifiées, limites connues explicites |

Les tests techniques ciblés restent requis pour les règles, droits, transitions et doublons. La recette vécue commence tôt et se répète seulement sur les parcours affectés par une correction ; ne pas rejouer toute la matrice après une retouche sans impact.

### Classer les défauts

| Gravité | Exemples | Suite |
| --- | --- | --- |
| P0 — Bloquant | Partie impossible à finir, secret divulgué, départ accidentel, action doublée, micro ouvert hors droits | Corriger avant validation du lot ; rejouer le cas et les transitions liées |
| P1 — Gênant | Joueur obligé d’être guidé pour agir, mauvais canal choisi sans le comprendre, texte essentiel masqué, attente prise pour un blocage | Corriger avant validation du parcours concerné et le refaire sans guidage |
| P2 — Finition | Petit défaut visuel ou de rythme sans gêne de compréhension ni action | Consigner et prioriser ; aucun défaut de confidentialité reclassé en simple finition |

## 10. Fiche de preuve à remplir après chaque essai

Ne pas préremplir un succès dans cette fiche. Les images générées et les rapports automatisés ne sont pas des captures de la version essayée.

```text
Date / observateur :
Version / commit ou empreinte du build :
Appareil / OS / dimensions / mode de saisie :
Mode présence ou distance / effectif :
Scénario (J, C, V, E ou rôle) / configuration de test :
Point de vue du joueur / expérience du Loup-Garou et de l’app :
Objectif donné au participant :
Gestes réellement effectués dans l’interface :
Résultat attendu :
Résultat observé :
Temps pour comprendre / agir ; hésitations et mauvais appuis :
Explication ou aide extérieure nécessaire :
Ce que le joueur a compris de l’enregistrement et de la suite :
Conforme / échec / non essayé :
Capture ou vidéo réelle de la version, si utile :
Défaut / gravité / correction à faire :
Version et résultat du nouvel essai :
Limites de la séance :
```

Mesurer le temps, les erreurs, le recours à l’aide et le ressenti d’attente ; ne pas inventer de taux de réussite. Le critère central reste concret : **un nouveau joueur peut rejoindre, comprendre son rôle, agir, attendre, voter, retrouver sa partie et terminer sans explication orale permanente ; aucun secret ni départ accidentel ne dépend de sa chance.**

Statut initial de cette recette : scénarios rédigés, exécution à venir. Le code Unity et le serveur ne sont pas modifiés pour produire ce plan.

### Couverture des gestes nocturnes à expérimenter

Un écran commun aux seuls passifs ne cache pas les doigts des joueurs actifs. Une activité commune à durée fixe est une proposition à tester avec un groupe, sous réserve du protocole yeux fermés et des fenêtres du moteur. Elle n’est pas implémentée et aucune preuve anti-déduction physique n’est revendiquée.
