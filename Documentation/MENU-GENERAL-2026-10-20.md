# Menu général N’GAME — cadrage ciblé du 20 octobre 2026

**État actuel : sources intégrées ; 56/56 PlayMode global puis 10/10 ciblés après révision du fond universel animé. Rendus normaux, safe 850 et création ouverts ; livré localement pour validation de Mathis, sans acceptation revendiquée.** Design et parcours, sans modification Unity par cet auteur. Sources lues : `StorybookApp.cs`, `UITheme.cs`, référence `ArtSource/reference-approved.png`. Coordination avec l’agent `hub_developer`, propriétaire du nouveau `GameHubView`. Le menu n’est pas déclaré testé humainement ; le rendu réel reste à capturer après intégration.

## Structure retenue

Conserver la palette bleu nuit, crème et terre cuite, les textures de papier et le décor existant. Le hub présente des jeux, pas une nouvelle direction artistique. Aucun texte promotionnel ou promesse de jeu disponible sans moteur.

- En-tête : **N’GAME** ; accès aux réglages à l’emplacement habituel.
- Titre de contenu : **Choisis un jeu**.
- Carte principale : **Loup-Garou**, **Disponible**, « 6 à 24 joueurs · chacun son téléphone », « En présence ou à distance ».
- Deux cartes secondaires : **Président** / **Mimes**, chacune marquée **En préparation**, sans lancement, compteur fictif ni faux bouton jouable. Leur iconographie de papier simple n’est pas une DA finale de ces jeux.
- Dock commun : **Créer une soirée** en principal, **Rejoindre une soirée** en secondaire. Emplacements conservés : bottom124, 382×56 ; bottom62, 382×48, dans le cadre 430×932 adapté à la zone sûre.
- Si un accès existe : encart compact au-dessus du dock **Une partie est enregistrée — Reprendre**. « Enregistrée » ne promet pas que le serveur est joignable ou que la manche continue.

Composition proposée par le développeur : titre vers top136, carte Loup-Garou 382×238 entre top180–418, Président/Mimes côte à côte entre top445–615 ; reprise vers bottom203, hauteur48. Ces coordonnées sont un contrat de mise en page proposé, pas une preuve de rendu final. Ne pas réintroduire de grands panneaux textuels.

## Navigation et effet réel de chaque action

| Départ / action | Destination et effet autorisé | Retour |
| --- | --- | --- |
| Hub → Loup-Garou | Fiche Loup-Garou existante ; aucune salle créée | Hub |
| Hub → Créer une soirée | Sélecteur local du jeu à créer ; Loup-Garou seul sélectionnable | Hub sans effet réseau |
| Sélecteur → Loup-Garou | Vraie entrée `RoomFlowView` directement, sans détour par la fiche ni création automatique de salle | Retour cohérent vers hub, sans pseudo/saisie effacés |
| Hub → Rejoindre une soirée | Vraie entrée de salon, code et pseudo ; pas d’étape de choix inutile | Hub tant qu’aucun salon n’a été rejoint |
| Fiche Loup-Garou → Créer ou rejoindre | Entrée `RoomFlowView` connectée existante | Hub sans session ; burger avec session |
| Fiche Loup-Garou → S’entraîner seul | Entraînement explicitement simulé ; démarrage seulement après protection d’un accès existant | Départ confirmé si une simulation est en cours |
| Hub → Reprendre | Réactiver le client réseau existant et présenter sa reprise | Aucun nouveau joueur, aucune nouvelle salle |
| Partie → navigation | Menu burger, aucun départ immédiat | Rester dans la partie prioritaire |

Le sélecteur doit porter le titre **Créer une soirée** avec une indication **Choisis un jeu**. La simple sélection de la carte ne constitue pas une création. La fiche Loup-Garou garde des textes fonctionnels : nom du jeu, effectif, mode, actions réelles ; pas de nouvelle accroche telle que « Une nuit. Des secrets. ».

### Contrat des callbacks

`GameHubView.Initialize(AppPreferences, Action<string> onOpenGame, Action onJoin, Action onSettings, Action onResume)`, `SetHasSavedRoom(bool)` et propriété publique `WantsToCreate` (booléen, reflète le sélecteur de création).

- `onOpenGame("werewolf")` est interprété avec `Hub.WantsToCreate` : faux depuis le catalogue → fiche `AppRoute.GameHome` ; vrai après Créer → vraie entrée de salon directement. Aucun ID Président/Mimes ni identifiant artificiel de création n’est envoyé.
- `onJoin` ouvre l’entrée connectée existante ; `onResume` ouvre le client réseau conservé ; `onSettings` ouvre les réglages.
- Le sélecteur reste local au hub. `WantsToCreate` distingue explicitement « ouvrir la fiche du catalogue » et « continuer la création après sélection ». Sélectionner le jeu ouvre l’entrée ; seule l’action connectée de création peut ensuite créer une salle.

## Protection de la place et du contexte

**Correctif intégré en source :** `StorybookApp.StartTraining()` ne rebind plus silencieusement un accès existant. Une modale propose la reprise ou le retour ; pendant une création/jonction en cours (`IsBusy`), elle propose « Voir connexion » et empêche de lancer le solo derrière la requête. Tout départ passe par la confirmation existante. Le parcours réel reste à retester après cette intégration. L’existence d’un client de pratique ne remplace jamais la vérification de l’accès réseau sauvegardé.

`SetHasSavedRoom` reçoit l’état du client réseau, pas seulement celui du client actuellement affiché. Un passage hub/catalogue/réglages masque les secrets et suspend la présentation, sans appeler `ForgetRoom`, `CreateRoom`, `JoinRoom` ou `Reset`. Un retour par l’aide/catalogue retrouve son contexte ; ne pas renvoyer systématiquement au hub si cela efface le parcours de la fiche. Aucun rôle ou accès privé dans l’encart de reprise.

Le contrôle en haut à droite est « … » hors partie pour les réglages et « ? » en partie pour l’aide contextuelle ; le burger conserve la protection du départ. Cette distinction est intégrée en source et reste à vérifier au rendu.

## Recette attendue après intégration

1. Premier lancement sans accès : trois jeux visibles, un seul disponible ; créer et rejoindre restent dans le dock ; les jeux futurs ne produisent aucune navigation trompeuse.
2. Ouvrir la fiche Loup-Garou, revenir ; ouvrir Créer, annuler ; même hub, aucun salon créé et aucune identité perdue.
3. Rejoindre directement avec un code erroné puis corrigé : erreur lisible, saisies conservées, vraie place créée une seule fois.
4. Avec un accès enregistré, passer hub/réglages/fiche puis Reprendre : même personne, même place, secrets masqués ; simulation refusée ou détournée proprement vers la reprise tant que la place n’est pas quittée explicitement.
5. Vérifier retours depuis fiche, catalogue et aide, puis burger/quitter/rester pendant salon et partie.
6. Capturer hub, sélecteur, fiche et reprise en 430×932 et écran plus petit : aucun chevauchement des cartes avec la reprise/dock ; état En préparation lisible ; village et éléments de papier restent visibles.

Cette recette n’est pas préremplie : code intégré, capture visible, test automatisé et découverte humaine restent des preuves distinctes.

## Première réception visuelle du hub

Rendu réel `Assets/NGameQA/hub-current.png` ouvert par coordination : pas de chevauchement, actions et reprise lisibles, DA papier cohérente sur cette vue. P2 : cartes futures trop transparentes ; textes de disponibilité se perdent dans le décor. La fonction désactivée doit rester explicite sans rendre la carte presque invisible. Une image ne confirme ni navigation, ni reprise, ni compréhension humaine.

## Correction du fond du catalogue

Après rejet par Mathis du village Loup-Garou derrière le hub, le fond est désormais universel en papier clair, avec encre sombre et objets de jeux. Les rendus `hub-universal-final.png`, `hub-universal-safe-area.png` et `hub-universal-create.png` ne montrent pas de chevauchement ; `werewolf-own-background.png` conserve le village dans le jeu. Dix tests ciblés contrôlent les allers-retours et le mouvement/réduction de la dernière version après la campagne globale de 56 tests. Le GIF final de 64 captures réelles sur 6,37 s et les frames 10/50 sont relus : commandes fixes, décors et icônes animés. Déformations shader et papier UI articulé, pas un livre 3D. Le stage LW reste chargé derrière le Canvas opaque du hub : ne pas affirmer qu’il est entièrement désactivé. Acceptation utilisateur encore attendue.
