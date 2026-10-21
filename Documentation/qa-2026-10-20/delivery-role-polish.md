# Fenêtres, interfaces de rôles et lucioles — passe locale en cours

20 octobre 2026. Les lumières et la présentation des pouvoirs ont été reprises après le retour de Mathis. Aucun serveur public n’est déployé ; cette passe reste locale tant qu’un nouveau push n’est pas vérifié. Les bilans `delivery-layered-current.md` et `delivery-current.md` sont des archives des livraisons précédentes.

## Éclairage des fenêtres

`HearthWindows` module maintenant la lumière dans les vitres déjà dessinées des bâtiments indépendants. Les zones d’ouverture et la couleur du verre limitent l’effet ; murs, texture de papier et croisillons restent conservés. Les variations sont faibles et déphasées par bâtiment, plutôt que des flammes ou rectangles orange plaqués sur la façade.

La [mesure de deux pixels sur 96 images](role-polish-light-pixels.json) confirme une petite variation du verre de la maison basse gauche (23 couleurs, rouge 222 à 234), tandis que le pixel du toit voisin reste à RGB (21,14,13). Ce contrôle limité ne prouve pas la stabilité de toute l’image ni l’aspect de chaque ouverture.

Les sprites de flamme et leurs halos extérieurs sont désactivés dans ce rendu. L’atlas `hearth-flame.png` de quatre poses reste dans les assets comme source historique ; il ne faut plus le présenter comme le feu actif des fenêtres. Les traits de reflet dans la rivière ont été atténués et normalisés. Le village reste entièrement assemblé en éléments séparés, sans retour à l’animation de régions d’un PNG complet.

## Pouvoirs et mini-jeu intégrés

Le cartouche privé des pouvoirs possède un fond de lecture et réagit à la sélection locale sans exposer l’identité des cibles dans le décor animé. Sceau de la meute, orbe, bouclier, potions, arc/cœurs et arbalète ont des comportements distincts. Les effets de résultat attendent le reçu autoritaire ; passer ne doit pas produire une fausse animation de pouvoir. La Petite Fille conserve sa consigne physique ; le Villageois n’acquiert pas un faux pouvoir. Les portraits restent rigides : ce n’est pas l’articulation individuelle de chaque illustration de rôle.

Les lucioles sont placées dans le village par projection caméra monde → Canvas. Elles rejoignent une lanterne posée, avec support ancré, ailes et petite trace de vol. Une luciole capturée quitte son point d’attente au lieu de rester visible en double. Le guidage est unique, sans cadre ni panneau central ; il suit la lanterne avec des bornes de largeur. Les quatre cibles gardent 44 points et restent les seules surfaces qui reçoivent un contact. Les effets de décor ne bloquent pas les autres boutons.

L’activité reste purement locale et lit seulement phase, protocole et temps publics. Aucun réseau, score public ou information secrète par contact. Elle est disponible uniquement avec le protocole écran, se masque aux menus/interruptions et conserve une version fixe avec mouvements réduits. Le protocole traditionnel yeux fermés conserve la Petite Fille ; écran et distance la refusent. L’activité ne garantit pas de cacher les doigts des acteurs autour d’une table.

## Finitions vérifiées

Le cartouche privé final fait 84 unités et le chronomètre reste séparé à 223, y compris lors des mises à jour du contrôle vocal, sans déplacer le formulaire. La lanterne est posée sur le chemin au point monde (.90,−1.65). Les halos de lucioles utilisent un dégradé transparent. La Petite Fille ne reçoit aucune fausse confirmation d’action. La campagne complète finale **98/98 PlayMode** couvre ces finitions, puis les captures ont été relues séparément.

## Revue des interfaces de rôles

Les huit rendus Unity suivants ont été relus indépendamment : [Loup-Garou](role-polish-werewolf.png), [Voyante](role-polish-seer.png), [Sorcière](role-polish-witch.png), [Garde](role-polish-guard.png), [Cupidon](role-polish-cupid.png), [Chasseur](role-polish-hunter.png), [Villageois](role-polish-villager.png), [Petite Fille](role-polish-little-girl.png). La Petite Fille décrit l’observation physique sans fausse confirmation de choix ; le Garde explicite sa restriction et le Chasseur son dernier tir. Aucun P1 ergonomique confirmé sur ces vues : bandeau, chronomètre, cibles, pagination et boutons restent lisibles et séparés. Le bandeau final fait 84 unités.

Le [manifest de captures](role-polish-visual-manifest.json) qualifie ces écrans comme des **fixtures contrôlées d’interface**, sans partie réseau ni participants humains. Ils prouvent le rendu de ces états ; les véritables parties HTTP sont vérifiées séparément. La réception artistique et le confort restent à valider par Mathis. 

Le [rendu public](role-polish-public.png) et les frames 0/20/40 des deux GIF ont également été relus indépendamment : pas de P1 visuel confirmé. Le [GIF de nuit](role-polish-night.gif), [manifest](role-polish-night.json), conserve 96 images réelles sur 9,471 s (GIF 9 690 ms), sans interpolation ; clic raycast EventSystem à l’image 12 puis une luciole capturée. La lanterne est posée sur le chemin, les vitres gardent leurs croisillons, les insectes et le loup se déplacent sans ancien panneau central.

Le [GIF Sorcière](role-polish-witch-action.gif), [manifest](role-polish-witch-action.json), conserve 64 images réelles sur 6,089 s (GIF 6 170 ms). Poison et cible sont sélectionnés via les commandes, puis le vrai clic de confirmation à l’image 12 produit `action.submitted=true` et `ConfirmationVersion=1` dans le moteur local. Le formulaire devient le reçu « Ton choix est enregistré », le soin reste disponible. Ces deux enregistrements utilisent le véritable `PracticeRoomClient` avec habitants simulés, pas les fixtures des huit PNG, ni une partie HTTP ou humaine.

## Preuves de cette passe

| Contrôle | État actuel | Portée / limite |
| --- | --- | --- |
| Compilation Unity | Recompilation sans erreur signalée par la racine | Ne remplace pas les tests ou l’observation du rendu |
| [Unity PlayMode complet](playmode-role-polish.json) | **98/98**, zéro échec/ignoré/inconclusif, **160,93 s** | Campagne complète unique finale, toutes finitions comprises ; pas une réception humaine |
| [Unity EditMode actuel](editmode-role-polish.json) | **125/125**, zéro échec/ignoré/inconclusif, **1,36 s** | Nouveau passage complet ; pas une réception humaine |
| [HTTP multi-session](full-match-http-validation.md) | Deux passages indépendants réussis : présence et distance, 10 sessions simulées chacun, 2 nuits, 11 pouvoirs, 2 votes/17 bulletins, victoire Village puis nouvelle distribution | 2 422 et 3 974 requêtes de transport avec polls ; pas autant de scénarios humains. Reprise, redémarrage et droits de chat vérifiés |
| Vocal distant | Deux fragments PCM synthétiques relayés pendant les votes ; émission éliminé et endpoint hors vote refusés | Pas de microphone, haut-parleur, permission système ou conversation humaine essayés |
| Rendus actuels | Huit PNG de rôles, rendu public et frames intermédiaires des deux GIF relus | PNG : fixtures UI. GIF : moteur local Practice, clics réels contrôlés ; pas une partie réseau/humaine. Anciennes captures `layered-*` historiques |
| [Build Mac final](build-mac-role-polish.json) | Succeeded, **0 erreur**, 346 avertissements AI, **295 742 929 octets**, **15 998 ms** ; plist valide, signature ad hoc `--deep --strict` vérifiée ; processus natif exact **25993** confirmé | Preuve de compilation, signature locale et lancement ; rendu relu dans Unity, pas une recette UI native complète ou notarisation |
| Git | Local, nouveau push à vérifier | Aucun déploiement public |

Huit rôles de base ont un moteur ; les huit extensions illustrées restent sans règles. Président et Mimes sont en préparation. Clavier et toucher sur vrais téléphones, permissions et acoustique, partie humaine, équilibre et réception artistique restent à vérifier. Aucun achèvement publié de l’application entière n’est revendiqué.
