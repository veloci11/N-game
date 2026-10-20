# Règles du moteur `ngame-classic-v1`

Ce document décrit le moteur C# local dans `Assets/NGame/Core/Multiplayer/WerewolfRoom.cs`. Il ne prouve ni le déploiement du serveur, ni la validation sur téléphones réels. Les références éditoriales et les autres rôles restent dans [ROLE-DESIGN.md](ROLE-DESIGN.md). Les décisions de vote, d’absence et de durée ci-dessous sont les conventions explicites de cette version N’GAME.

## Salon et secrets

Le salon accepte 6 à 24 joueurs, chacun avec son téléphone. Six et sept constituent des formats N’GAME à équilibrer, pas le minimum annoncé par le jeu classique. Le premier joueur est l’hôte. Tous doivent confirmer leur préparation ; changer les paramètres annule cette confirmation. La composition contient exactement une carte par personne, au moins un loup et un habitant qui n’est pas loup. Les rôles spéciaux sont uniques.

Le lancement exige aussi que tous les membres soient connectés. `startBlockedReason` expose la même raison que le refus réel de lancement : effectif, mode/composition, carte manquante, connexion ou préparation. Une proposition de réglages refusée ne remplace pas les règles actuelles. En salon seulement, l’hôte peut retirer **une autre personne** avec `remove-player` et une cible ; cela retire sa place, ses faits et ses anciens reçus, puis annule les préparations restantes. L’hôte ne peut pas se retirer par cette commande et ne peut retirer personne après la distribution. La composition automatique suit l’effectif ; une composition manuelle reste explicite et doit être ajustée si son nombre de cartes ne correspond plus au groupe. Les accès de la personne retirée sont invalidés par la couche serveur.

Huit rôles ont un moteur : Loup-Garou, Villageois, Voyante, Sorcière, Chasseur, Cupidon, Garde et Petite Fille. Les huit autres cartes du catalogue restent visuelles et leur activation est refusée. `RoleDefinition.ImplementedRuleset` nomme cette version ; `SupportsRuleset` vérifie aussi le mode. La Petite Fille n’est acceptée qu’en présence.

L’autorité appartient au serveur. L’hôte ne voit pas les cartes des autres. Une projection fournit sa propre carte, les partenaires autorisés de la meute et les faits privés reçus. La Voyante voit **la carte exacte**, pas seulement un camp. Cupidon connaît le couple choisi ; chaque amoureux connaît l’identité de son partenaire, sans recevoir automatiquement sa carte. Un éliminé ne gagne aucun droit de voir les secrets des survivants.

Les rôles des éliminés sont révélés si le paramètre correspondant est actif ; tous les rôles sont révélés à la fin. Les votes restent privés jusqu’à leur publication à la clôture. Les projections et leurs réglages sont des copies indépendantes : modifier un objet reçu ne change pas le salon.

## Distribution avant les pouvoirs

Le lancement attribue les cartes puis ouvre la phase `distribution` : aucun pouvoir nocturne n’a encore son chronomètre. Chaque joueur découvre sa propre carte puis confirme avec `acknowledge-role`. Les autres ne reçoivent que le nombre de confirmations, pas les rôles. Le client demande une découverte effective de la carte avant d’activer cette confirmation ; le moteur enregistre l’intention personnelle, pas une preuve que le joueur a lu.

La nuit commence lorsque tous ont confirmé ou après **deux minutes**. À cette échéance, un événement public explique l’absence ; les confirmations manquantes restent manquantes, elles ne deviennent pas des « j’ai lu » fictifs. Les pouvoirs des absents suivent ensuite la règle normale de passage à leur échéance. Une longue coupure ne consomme pas silencieusement toutes les fenêtres nocturnes : le premier pouvoir commence avec son nouveau délai.

## Ordre et pouvoirs nocturnes

La première nuit commence par Cupidon, s’il est présent, puis un appel « les amoureux se reconnaissent ». Cet appel est conservé même si Cupidon passe ; seuls les amoureux reçoivent le nom privé de leur partenaire. Sa fenêtre automatique dure huit secondes, écourtable par l’hôte après trois secondes. Chaque nuit suit ensuite Garde, Voyante, meute puis Sorcière. Les appels publics en présence identifient le rôle qui se réveille, jamais son joueur. La Sorcière sans potions conserve un appel sans action ; lorsque les cartes des éliminés restent cachées, les rôles morts conservent aussi leur appel. Ces précautions évitent que la narration contourne les secrets. Chaque étape possède un nouveau numéro de tour ; une commande ancienne est refusée.

| Pouvoir | Convention de cette version |
| --- | --- |
| Cupidon | Lie deux personnes vivantes distinctes, lui-même possible. Un passage ou une absence ne crée aucun couple. |
| Garde | Protège une personne vivante, lui-même possible ; même cible interdite deux nuits consécutives. Son panneau privé rappelle sa propre protection précédente, sans transmettre ce choix aux autres. Passer une nuit interrompt cette succession. Bloque l’attaque de la meute, pas le poison ; ne protège pas la Petite Fille des loups, conformément à l’exception du Salvateur. |
| Voyante | Consulte une autre personne vivante et reçoit son rôle exact une fois l’étape close. |
| Meute | Chaque loup peut désigner une personne vivante qui n’est pas loup, ou passer. La cible unique la plus désignée est attaquée ; égalité sans victime. Les loups peuvent modifier leur choix avec une nouvelle commande jusqu’à clôture. |
| Sorcière | Une potion de soin et une de poison pour toute la partie. Peut utiliser les deux la même nuit. Soin = victime autorisée de la meute ; poison = une autre personne vivante. Elle peut se soigner si elle est la victime de la meute. Une protection effective ne consomme pas son soin et ne lui donne pas une fausse victime à soigner. |
| Petite Fille | Aucune identité numérique offerte. Pendant la fenêtre de la meute, elle peut observer physiquement la table au risque d’être remarquée. Le serveur ne peut pas contrôler ce geste. |

Pour la Sorcière, `save` et `pass` utilisent zéro cible ; `poison` utilise une cible ; `both` utilise uniquement la cible du poison et soigne la victime déjà annoncée. Le soin ne neutralise pas un poison, y compris lorsque le poison vise cette même victime. Une potion consommée ne revient pas à la nuit suivante. Les victimes nocturnes sont résolues ensemble avant les réactions.

Les autres actions confirmées et les votes sont verrouillés. Une absence vaut un passage, sans inventer de cible ni de renseignement.

## Petite Fille autour de la table

Ce rôle suppose un protocole de nuit accepté avant la distribution : yeux fermés, réveil des seuls rôles appelés, narration ou meneur, téléphones discrets. Dans une pièce où tout le monde reste éveillé face aux écrans, l’interface ne rend pas son pouvoir classique fidèle par magie.

Quand elle est vivante, la fenêtre de la meute reste automatiquement ouverte jusqu’à son échéance, même si tous les loups ont déjà choisi. Ils peuvent corriger leur cible après une observation remarquée. L’hôte peut fermer cette fenêtre après trois secondes lorsque tous ont confirmé ; le groupe doit connaître cette possibilité. L’application ne transmet pas d’identité de loup à la Petite Fille et ne prétend pas empêcher captures d’écran, spectateurs indiscrets ou communications extérieures.

En mode distant, son activation est refusée. Une adaptation numérique éventuelle demandera d’autres règles et un autre identifiant de jeu de règles ; elle n’est pas silencieusement remplacée par une information gratuite.

## Réactions, jour et victoire

Un décès entraîne immédiatement celui de l’amoureux lié, s’il est encore vivant. Tous ces décès sont traités avant la victoire. Le Chasseur éliminé reçoit sa fenêtre de dernier tir, même s’il est mort à cause du lien amoureux ou du poison. Le tir peut à son tour déclencher d’autres décès. Dans cette version, il peut passer ; une absence de vingt secondes vaut un passage, sans tir aléatoire. C’est une convention explicite face aux différences de présentation relevées dans les sources classiques.

Après les réactions nocturnes vient l’aube, puis la discussion et le vote. Chaque clôture diurne ouvre d’abord un **verdict de cinq secondes**, écourtable par l’hôte après trois secondes : cibles et choix publiés deviennent lisibles. Si une personne est éliminée, son décès et celui de son amoureux sont enregistrés ; la fenêtre du Chasseur n’est ouverte qu’après cette lecture, avec ses vingt secondes complètes. La victoire attend toujours la fin des réactions. Sans réaction ou victoire, la nuit suivante commence ensuite ; une égalité du premier tour prépare le second scrutin après son verdict.

Chaque vivant peut voter contre une autre personne vivante ou s’abstenir. Une égalité entre les premières cibles ouvre un second tour limité à ces personnes. Une nouvelle égalité ou uniquement des abstentions n’élimine personne. Les choix sont publiés à chaque clôture. Capitaine, poids de voix spéciaux et Bouc Émissaire ne sont pas activés dans ce moteur.

Pendant le vote, chaque destinataire reçoit le nombre de réponses, le numéro de scrutin et son propre choix confirmé ; il ne reçoit pas les choix des autres avant clôture. Le vote est verrouillé après confirmation. Le verdict contient ensuite les bulletins publics et sa conséquence : élimination, second tour ou absence d’élimination. Un second tour efface les votes du premier et ouvre une nouvelle fenêtre.

**Convention distante N’GAME :** le vote reste ouvert jusqu’à son échéance, même lorsque tous ont déjà confirmé. L’hôte peut le clore plus tôt seulement après trente secondes et lorsque tous les votes sont reçus. Cette fenêtre laisse la place au vocal demandé **uniquement pendant le vote de jour à distance**, second tour compris. La phase Discussion n’ouvre pas le vocal. Micro volontaire, droits des canaux et arrêt réel de la capture/transmission appartiennent aux communications, décrites dans [COMMUNICATIONS-ET-VUES.md](COMMUNICATIONS-ET-VUES.md) ; l’état du moteur ne prouve pas à lui seul la coupure audio d’un téléphone.

La victoire n’est jamais attribuée automatiquement à parité. Le village gagne lorsque le dernier loup est éliminé ; les loups lorsque le dernier adversaire du village est éliminé. Les membres morts du camp gagnant partagent la victoire. Deux amoureux de camps opposés gagnent seuls s’ils sont les deux derniers survivants ; ils ne gagnent pas avec leur camp d’origine. Tous morts produit une égalité. Toute réaction encore ouverte est résolue avant cette décision.

## Échéances et reprise

Une étape nocturne dure 3 à 120 secondes, au choix du salon ; aucun saut nocturne avant trois secondes. Sans Petite Fille active, les actions complètes permettent l’avancement automatique après ce minimum. Discussion : 5 à 600 secondes, écourtable par l’hôte. Aube : quatre secondes, écourtable après trois ; distribution : deux minutes au maximum ; verdict : cinq secondes, écourtable après trois ; Chasseur : vingt secondes. Le vote en présence dure au maximum 45 secondes et se clôt automatiquement lorsque tous ont confirmé. Le vote distant dure 90 secondes par défaut, réglable de 30 à 300 secondes, avec la clôture anticipée limitée ci-dessus.

Le serveur utilise sa propre horloge et avance aussi sans l’hôte. Après une interruption longue, une nouvelle étape commence avec une nouvelle échéance ; le moteur ne simule pas plusieurs nuits de décisions inventées. Authentification, jetons de reprise, concurrence, stockage et hébergement appartiennent à la couche serveur, pas à cette classe.

Un départ explicite en salon retire la place et annule les préparations ; pendant une manche, il conserve le rôle et la place, marqués hors connexion. Les délais continuent et les actions absentes deviennent des passages. Si l’hôte est absent, le premier membre connecté restant prend sa place d’organisation sans recevoir de secrets supplémentaires ; le retour de l’ancien hôte ne lui rend pas automatiquement ce titre. Une reprise authentifiée remet la même personne connectée, sans redistribution. À la revanche, les places explicitement abandonnées sont retirées ; une simple perte réseau ne vaut pas automatiquement abandon.

`CaptureSnapshot` et `RestoreSnapshot` fournissent une sauvegarde autoritaire complète, **réservée au serveur** : rôles, amoureux, faits privés, potions, soumissions, réactions en attente, verdict, horloges, reçus et état du mélange aléatoire. Ce contenu ne doit jamais être envoyé à un joueur. La restauration ne change ni sa carte ni ses droits et préserve aussi le mélange d’une revanche future. L’écriture durable, la confidentialité du fichier et la restauration des jetons restent à vérifier dans la couche serveur ; la présence du contrat moteur ne suffit pas à revendiquer une reprise après redémarrage réel.

Une commande exige son numéro de tour et un identifiant unique. Une répétition identique ne s’applique pas deux fois ; réutiliser le même identifiant pour un autre contenu est refusé. Le cache moteur garde au maximum 2 048 reçus. La commande de revanche `reset` n’est permise qu’en salon ou après la fin, par l’hôte : cartes, faits privés, couples, potions et états de mort sont alors remis à zéro, avec de nouveaux numéros de tour et un nouvel identifiant de manche. Il ne s’agit pas d’un moyen de revenir au salon au milieu d’une partie.

## Validation attendue

`Assets/NGame/Tests/Editor/WerewolfRoomTests.cs` couvre notamment les parties complètes, la confidentialité, les copies détachées, le non-arrêt à parité, les réactions, les amoureux, les potions, la protection et son exception Petite Fille, les égalités et les commandes rejouées. Ces tests doivent être exécutés dans le pipeline du projet avant de qualifier la version de validée. Une séance de groupe en présence et une séance distante restent nécessaires pour éprouver l’équilibrage et le protocole social.
