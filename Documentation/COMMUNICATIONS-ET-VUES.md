# Vues des joueurs, chat et vocal — conception

Conception initiale du 17 octobre 2026. **Le développement a été repris à la demande de Mathis le 20 octobre.** Les contrats ont maintenant une implémentation locale ; son état et ses tests sont consignés dans [SUIVI-PRODUCTION.md](SUIVI-PRODUCTION.md). La solution concrète de cette reprise utilise un relais PCM natif, sans fournisseur externe ; le microphone WebGL n’est pas exposé. Le candidat Vivox cité dans la conception ci-dessous n’est pas un compte ou SDK activé. Les règles de droits restent la référence ; les paragraphes de proposition ne constituent pas une preuve de fonction validée.

Ce document complète les [52 maquettes](../ArtSource/MenuStudies/2026-10-17/README.md) et sert de contrat au [plan d’exécution](PLAN-EXECUTION.md).

## 1. Décisions utilisateur

| Sujet | Décision à respecter |
| --- | --- |
| Point de vue | Chaque joueur reçoit sa propre interface pendant la même phase |
| Joueur actif | Les commandes de son pouvoir et ses informations autorisées |
| Autres joueurs | Une attente animée, sans commandes ou secrets d’un autre rôle |
| Messages | Dock du bas semi-transparent, uniquement pendant le vote de jour ; premier et second tours |
| Vocal | Mode distant uniquement, pendant le **vote de jour** |
| Hors vote | Ni messages, ni lecture d’historique, ni vocal en salon, distribution, nuit, aube, discussion, réaction ou résultat |
| Navigation | Burger en partie ; quitter passe par le menu puis une confirmation |
| Direction artistique | Conte en papier déjà retenu ; boutons dans des zones fixes |
| Travail actuel | Développement repris le 20 octobre ; état exact et preuves dans le suivi de production |

La dernière demande de Mathis limite aussi les messages écrits au vote. Le second tour reste un vote de jour. Pendant Discussion, les joueurs présents parlent à table ; à distance, le bilan reste lisible puis l’hôte ouvre le vote pour communiquer. Aucun historique de messages n’est consultable hors vote.

Le micro est volontaire et coupé par défaut. Les ACL ci-dessous décrivent le contrat actuel livré ; leur présence en source ne remplace pas une validation acoustique ou humaine.

## 2. Une phase commune, plusieurs interfaces

Le serveur connaît l’état complet. Chaque téléphone reçoit une projection destinée à son joueur, puis choisit sa présentation. Les rôles de tous les joueurs ne transitent jamais dans une grande liste publique filtrée seulement à l’écran.

Exemple au même instant pendant l’appel de la meute :

| Destinataire | Ce qu’il voit | Action possible |
| --- | --- | --- |
| Loup vivant | Vue privée de meute, partenaires autorisés, cibles disponibles, proposition de cible | Confirmer ou modifier son choix avant la clôture |
| Villageois vivant | Village endormi, lune, nuages, eau et petites lumières | Consulter sa propre carte ; attendre |
| Voyante ayant joué | Même nuit, attente ; sa découverte reste consultable dans ses informations privées | Aucune seconde enquête |
| Sorcière | Attente jusqu’à sa propre fenêtre | Aucune potion anticipée, aucune victime annoncée avant son autorisation |
| Petite Fille, en présence | Consigne privée d’observation physique au bon moment | Observer la table ; aucune identité numérique de loup |
| Joueur éliminé | Vue d’observateur, informations publiques | Aucune action sur les vivants, sauf réaction de Chasseur encore due |
| Hôte joueur | Son propre rôle et les commandes d’organisation autorisées | Aucun accès supplémentaire aux secrets |

L’appel public peut annoncer « les loups se réveillent » comme une narration à table. Il ne doit jamais annoncer qui tient ces rôles. Aucune animation d’un avatar public, pastille de connexion, notification ou activité de saisie ne doit permettre de retrouver le joueur actif secret.

### Ordre de priorité du rendu

1. Masquer les secrets si application en arrière-plan, menu sensible ouvert, perte de focus ou interruption.
2. Montrer une reconnexion si l’état n’est plus fiable ; désactiver les actions de partie.
3. Afficher une réaction personnelle autorisée avant l’écran générique de joueur éliminé.
4. Traiter les états de salon, explication, distribution et résultat selon leur phase.
5. Si le joueur est éliminé et sans réaction : observateur.
6. Si le serveur lui propose une action : vue de ce pouvoir, avec états sélection, confirmation et choix enregistré.
7. Sinon : attente animée correspondant à la phase commune.

Une ancienne sélection ne se transforme jamais automatiquement en action du tour suivant. Une information persistante, comme une enquête de Voyante, reste accessible après le changement de sous-étape mais doit être révélée volontairement.

## 3. Matrice des pouvoirs du premier lot

| Rôle | Fenêtre et cibles | Informations privées | Hors de sa fenêtre | Maquettes |
| --- | --- | --- | --- | --- |
| Villageois | Aucun pouvoir nocturne ; vote comme les autres vivants | Sa carte | Attente | D14–D16, H29–H30 |
| Loup-Garou | Meute ; un vivant qui n’est pas loup ; choix modifiable jusqu’à clôture | Partenaires loups et leurs choix autorisés | Attente ; aucune conversation Meute, même pendant l’action nocturne | E17 |
| Voyante | Une autre personne vivante | Rôle exact consulté | Attente et accès privé à ses découvertes précédentes | E18–E19 |
| Sorcière | Soin, poison, les deux ou passage ; seule la cible du poison se sélectionne | Victime autorisée et disponibilité des deux potions | Attente, y compris après épuisement des potions | F21–F22 |
| Chasseur | Après son décès, une dernière cible vivante ou passage | Sa réaction en attente | Réaction prioritaire, puis observateur | G26, I33 |
| Cupidon | Première nuit, deux vivants distincts ; lui-même possible | Couple choisi, sans dévoiler les cartes des partenaires | Attente après son action | F23 |
| Garde | Un vivant, lui-même possible ; précédente cible interdite | Sa protection et les restrictions utiles | Attente | E20 |
| Petite Fille | Observation physique pendant la meute, présence uniquement | Consigne de table, sans identité numérique de loup | Attente ; rôle indisponible à distance | G25 |

Tout rôle peut devenir amoureux. F24 montre le partenaire autorisé pendant la reconnaissance ; il n’accorde ni sa carte ni un canal de conversation permanent. La commande de Sorcière `both` est une intention unique, pas deux confirmations successives.

Les interactions de décès, amoureux, protections, poison, dernier tir, égalités et victoire restent celles de [WEREWOLF-RULESET.md](WEREWOLF-RULESET.md). Aucune animation ne doit inventer ou supprimer une conséquence du moteur.

## 4. Messages écrits : contrat actuel

La nouvelle demande remplace les anciennes propositions de chat en salon, discussion et Meute. Le serveur vérifie les droits ; masquer une entrée UI ne suffit pas.

| Canal / situation | Lecture | Envoi |
| --- | --- | --- |
| Village, vote de jour | Joueurs vivants et éliminés de la manche | Joueurs vivants seulement |
| Observateurs, vote de jour | Joueurs éliminés de cette manche | Ces mêmes joueurs |
| Tous canaux, autre phase | Fermée, historique compris | Fermé |
| Meute, toute phase | Canal absent | Fermé |

Aucun spectateur extérieur, message direct ou canal Amoureux implicite. Le canal Observateurs reste privé aux éliminés ; l’hôte n’y entre pas au titre de son pouvoir d’organisation. Un ancien reçu d’envoi accepté peut être acquitté hors vote avec `sent=null` : cela ne renvoie aucun message et ne rouvre aucun canal. Voir les [913 contrôles HTTP](qa-2026-10-20/chat-vote-only.md), distincts du rendu Unity.

### Dock et commandes

- Barre « Messages » fixe à 247 unités du bas, visible pendant le vote seulement.
- Dock développé de 230 unités maximum, entre 270 et 500, fond semi-transparent (alpha 0,76), sans écran opaque ni titre narratif.
- Historique défilant d’environ 130 unités et compositeur de 44 ; texte aligné à gauche, pseudo distingué du message.
- Les cibles publiques et la confirmation du vote restent utilisables lorsque le dock est développé ; le contenu du vote réserve l’espace nécessaire au-dessus du dock. La carte privée reste masquée.
- Réduire le dock ne quitte pas la partie. Quitter la phase de vote ferme le dock immédiatement, y compris en cas de droits périmés encore en cache.
- Brouillons distincts par canal et manche ; un brouillon Observateurs ne devient jamais un message Village. Nouvelle manche, nouvelle identité de brouillon.
- Aucun indicateur public de saisie ou d’activité privée. L’envoi incertain garde son identité pour éviter les doublons ; un clic sur Envoyer ne confirme jamais un vote ou une potion.

### Clavier et interruptions

Aucun maintien de carte pendant la rédaction. Le dock remonte au-dessus du vrai clavier ; les commandes du vote gardent leurs coordonnées habituelles. Le clavier système peut physiquement recouvrir ces coordonnées : la confirmation est alors neutralisée, le clavier se ferme au repli/envoi et le joueur confirme son vote ensuite. Il ne faut pas annoncer qu’un bouton est visible sous le clavier. Tester ce parcours sur appareil, avec une seule main, sans inventer une validation à partir du seul clavier de l’éditeur.

Focus perdu, menu, arrière-plan ou droits révoqués ferment le dock et masquent les secrets. Les scénarios UI et captures doivent vérifier cette nouvelle version ; les anciens écrans de chat plein écran/Meute ne sont plus des références de comportement.

## 5. Vocal : règle fermée hors vote de jour

Condition d’ouverture de l’audience vocale : **partie distante + phase de vote de jour + membre authentifié + autorisation courante**. Une autorisation d’émettre ajoute : joueur vivant, activation volontaire du micro, permission système et application au premier plan.

| État | Écoute / émission proposées |
| --- | --- |
| Même pièce | Aucun vocal intégré ; la conversation reste dans la pièce |
| Salon, explication, distribution | Fermé |
| Nuit, tous rôles et Meute compris | Fermé |
| Aube et discussion | Fermé, messages et historique compris |
| Vote de jour, premier ou second tour | Écoute ouverte aux destinataires autorisés ; émission seulement pour un vivant qui l’active |
| Vote déjà confirmé, scrutin encore ouvert | Le vivant peut encore parler jusqu’à la clôture |
| Éliminé pendant un vote | Proposition : écoute seule, jamais émission ; à vérifier avec les permissions du fournisseur |
| Verdict, réaction, résultat | Fermé |
| Déconnexion, permission refusée, arrière-plan | Aucune émission ; retour silencieux et accès écrit seulement lorsque le vote et les droits l’autorisent |

Le micro commence coupé, redevient coupé à chaque nouveau scrutin et ne se réactive pas automatiquement après reconnexion ou retour de focus. Le joueur peut rester en texte uniquement. Les messages système et les sons d’ambiance ne doivent pas être confondus avec la voix d’un joueur.

### Temps de parole à prévoir dans le vote

Le moteur actuel clôt immédiatement lorsque le dernier vivant confirme son vote. Avec du vocal uniquement pendant cette phase, cela peut couper la conversation très tôt.

**Proposition de départ, non décidée par l’utilisateur :** conserver un vote ouvert jusqu’à son échéance, ou clôture explicite par l’hôte après un minimum de 30 secondes et lorsque tous les votes sont reçus. Durée de vote suggérée pour les essais distants : 90 secondes. Afficher un avertissement commun avant clôture. L’absence de l’hôte ne bloque pas l’échéance serveur. Ces valeurs demandent des essais ; elles ne sont pas un équilibrage validé.

Le premier lot ne contourne pas la demande en ouvrant le vocal pendant la phase Discussion. Si le groupe veut modifier cette règle ultérieurement, ce sera une nouvelle décision explicite.

### Coupure et opérations tardives

Une icône grisée n’est pas une fermeture de canal vocal. Le plan prévoit :

1. Une génération de permissions pour chaque fenêtre vocale.
2. Une demande volontaire de micro, vérifiée à nouveau côté serveur avant autorisation.
3. Un silence local immédiat à la sortie de fenêtre ou à l’interruption, et annulation des opérations de capture en attente.
4. Une révocation/fermeture côté fournisseur coordonnée avec les transitions du moteur, y compris celles déclenchées par les délais.
5. Un contrôle de génération sur chaque retour asynchrone : une permission système ou une connexion acceptée trop tard ne rouvre pas le micro.
6. Un comportement fermé en cas d’échec fournisseur ; le jeu revient au texte. Un défaut de fermeture stricte doit bloquer la validation du lot vocal.

La latence réseau et l’audio déjà en transit sont à mesurer ; aucune coupure physique instantanée à zéro milliseconde n’est promise. Un jeton de connexion expiré ne suffit pas à prouver la déconnexion d’une session déjà ouverte.

## 6. Contrats à spécifier avant le code

Ce sont des **champs et routes proposés**, pas de nouvelles classes ou API déjà créées.

| Contrat | Contenu utile |
| --- | --- |
| État de communication | Manche, tour, génération de droits, heure serveur, canaux accessibles, raisons de fermeture |
| Canal | Identifiant opaque, type, droits de lecture/envoi ; aucune liste publique de membres secrets |
| Message | Identifiant serveur, séquence, manche, canal, auteur public, heure serveur, texte |
| Demande d’envoi | Canal, tour/génération attendus, identifiant stable du message, texte ; aucun rôle ou auteur imposé par le client |
| Page d’historique | Messages autorisés, curseur de suite et indicateur de fin |
| Autorisation vocale | Fournisseur, audience opaque, génération, échéances, autorisation limitée au joueur et au vote actuels |

Routes proposées sous le salon authentifié :

- Lire les capacités de communication du destinataire.
- Lire les messages autorisés d’un canal avec curseur.
- Envoyer un message avec déduplication.
- Demander l’autorisation vocale du vote courant.
- Déclarer une intention de micro ou quitter le canal de façon idempotente.

Le serveur déduit auteur, rôle, vie, canal fournisseur et heure. Sous le verrou de salon, il avance d’abord le moteur à son heure, puis vérifie les droits. Une requête arrivée après clôture ne doit pas être acceptée avec des permissions périmées.

Bornes proposées pour le premier lot texte : 500 caractères par message, 50 messages par page, 200 messages retenus par canal, débit limité par session, aucun fichier joint ni aperçu de lien. Texte brut, sans interprétation de balises. Les historiques privés et brouillons ne migrent pas à la revanche ; suppression à expiration du salon. Ces limites seront mesurées avant une mise en production.

## 7. Architecture et points à vérifier

Conserver le serveur .NET autoritaire et le client Unity existants. Le chat texte utilise leurs identités et leurs projections. Commencer avec des curseurs et le transport actuel ; une diffusion événementielle peut ensuite réduire la latence, sans changer les règles de droits.

Le fournisseur vocal est isolé derrière une interface. **Vivox est un candidat, pas un choix activé sur ce compte.** La clé de signature reste sur le serveur. Avant sélection définitive, vérifier l’accès réel à la création d’autorisations et aux commandes de révocation/modération : l’API de contrôle serveur Vivox demande une activation auprès du fournisseur. [Documentation officielle du contrôle serveur](https://docs.unity.com/en-us/vivox-core/server-to-server-api-reference/server-to-server-web-api-overview).

Valider le vocal sur les plateformes cibles avant de figer l’UI : le support WebGL Vivox est limité, avec notamment un seul canal par instance et des limites sur les événements de parole. Ne pas dessiner un indicateur « parle » en prétendant qu’il est déjà disponible partout. Les noms peuvent venir de l’identité publique du jeu. [Limites officielles WebGL](https://docs.unity.com/en-us/vivox-unity/developer-guide/vivox-webgl).

Le prototype de contrôle doit démontrer une destination de transmission unique, son arrêt et la reprise silencieuse. [Modes de transmission](https://docs.unity.com/en-us/vivox-unity/developer-guide/transmission/transmission-modes), [fournisseur de jetons personnalisé](https://docs.unity.com/en-us/vivox-unity/access-token-guide/interface-implementation-tokens), [récupération de connexion](https://docs.unity.com/en-us/vivox-unity/developer-guide/channels/new-connection-recovery).

Les permissions système micro, le refus, les interruptions audio, les écouteurs et le Web en contexte sécurisé seront testés sur appareils réels. Aucun compte, coût de service, accès fournisseur ou déploiement public n’est considéré prêt par ce document.

## 8. Maquettes complémentaires à prévoir

Les 52 images existantes restent les références artistiques. Les compléments suivants sont **planifiés, non générés** dans cette étape :

- N : au même tour serveur, vue Loup actif, Villageois en attente, Voyante ayant joué et observateur éliminé.
- O : dock du bas Village/Observateurs pendant le vote, clavier et confirmation publique accessibles ; aucun message ni micro de nuit.
- Variantes d’interruption : refus micro, retour de focus silencieux, changement de canal après décès, permission acceptée trop tard.

Le texte C12 qui renvoie seulement à un appel externe est une ancienne proposition visuelle. Il doit être adapté à cette nouvelle règle de vocal intégré avant implémentation.

## 9. Scénarios de réception

- Quatre clients, même tour : bonnes vues distinctes et mêmes événements publics, sans secret supplémentaire.
- Tout joueur demandant Meute, ou vivant demandant Observateurs : refus serveur ; même résultat en appelant directement l’API.
- Fin du vote : aucun canal ni chargement d’historique, même si l’interface envoie un ancien identifiant.
- Joueur mort : aucun message envoyé aux vivants ; éventuelle réaction Chasseur toujours utilisable.
- Message rejoué, changement de canal pendant saisie, ancienne génération, fermeture de vote à la dernière milliseconde : pas de doublon ni fuite de brouillon.
- Vocal refusé en présence et dans chaque phase autre que Vote de jour, même si le client réclame l’accès.
- Second tour, élimination, passage en arrière-plan et retour tardif de permission : micro fermé jusqu’à une nouvelle activation autorisée.
- Dernier vote confirmé : la durée de parole retenue n’est pas supprimée par l’ancien raccourci du moteur.
- Panne ou redémarrage : aucun canal vocal orphelin ouvert ; reprise fidèle ou message honnête d’indisponibilité.

Ces scénarios restent des critères de réception ; les résultats réellement exécutés sont liés dans le suivi. Les tests de l’ancienne politique de messages ne qualifient pas la nouvelle UI.

## Proposition de couverture des gestes nocturnes — non implémentée

Un écran identique pour les joueurs passifs ne suffit pas à cacher les gestes des rôles actifs autour d’une table. Une piste est une activité commune, à durée fixe, demandée à tous pendant les appels nocturnes ; elle devra préserver le protocole yeux fermés et les fenêtres du moteur. Ce n’est ni une fonction livrée ni une preuve contre la déduction des rôles à partir des mouvements physiques. Évaluer la compréhension et les signaux observables avec un vrai groupe avant de prétendre résoudre cette limite.
