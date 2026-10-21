# Lancer une recette autour de la table

Cette commande prépare le **backend local**, pas une version mobile installée ou une partie déjà validée sur appareils. Aucun serveur public n’est nécessaire pour cet essai. Utiliser le même réseau local pour le Mac serveur et tous les appareils ; éviter un Wi-Fi invité isolant les clients.

## 1. Choisir explicitement l’adresse du Mac

Dans les réglages réseau de macOS, relever l’adresse IPv4 privée de l’interface actuellement utilisée, par exemple `192.168.1.42`. Cette adresse est un exemple : la remplacer par celle réellement attribuée au Mac. Le script n’essaie pas de choisir ou modifier le réseau.

Depuis la racine du projet :

```sh
Tools/run_lan_playtest.sh --listen-url http://192.168.1.42:8787 --check
Tools/run_lan_playtest.sh --listen-url http://192.168.1.42:8787
```

`--check` valide sans lancer. Sans `--listen-url`, le script refuse de démarrer. Il accepte seulement HTTP sur une adresse privée IPv4 ou une boucle locale et un port de 1 à 65535. Il refuse notamment `0.0.0.0`, les adresses publiques et les noms DNS autres que `localhost`. Il ne modifie ni pare-feu, ni routeur, ni configuration Wi-Fi. Une adresse privée valide mais absente des interfaces du Mac fera échouer l’écoute : vérifier alors l’adresse choisie.

Le script utilise `Server/NGame.Server/run.sh`, le SDK .NET fourni avec Unity ou `DOTNET` si configuré, et transmet l’adresse à ASP.NET via environnement et argument explicite. Les sauvegardes privées du serveur restent hors du dossier Web. Garder ce terminal ouvert ; `Ctrl+C` arrête ce serveur. Ne pas partager ni publier les fichiers de sauvegarde ou les jetons de reprise.

## 2. Configurer chaque client

Saisir la **même adresse complète**, par exemple `http://192.168.1.42:8787`, dans les réglages serveur de chaque client. Ouvrir `http://192.168.1.42:8787/health` depuis un appareil permet de contrôler uniquement la joignabilité HTTP. Ce contrôle ne prouve pas que le jeu mobile fonctionne.

`localhost` ou `127.0.0.1` désignent l’appareil lui-même : utiles pour deux clients sur ce Mac, pas pour un téléphone. Le créateur ouvre ensuite sa salle et communique le code ; le code n’est ni une URL ni un mécanisme de connexion à travers Internet.

Pour un essai rapide, réunir 6–8 identités, vérifier chacun prêt, puis suivre carte → confirmation → pouvoir éventuel → vote → résultat. Vérifier les cartes privées, le chat uniquement pendant le vote, sa fermeture à la clôture et la reprise après une coupure volontaire. Ces étapes restent une recette à exécuter avec les joueurs ; aucune durée ou ergonomie sur appareils n’est déclarée validée ici.

## 3. Si un appareil ne se connecte pas

Vérifier l’adresse actuelle du Mac, le port, le réseau commun et l’absence d’isolation des clients Wi-Fi. Une éventuelle demande de pare-feu doit être examinée manuellement ; le script ne crée aucune règle. Les pare-feu macOS/Windows, les autorisations iOS de réseau local/ATS pour HTTP et les politiques Android de trafic HTTP doivent être vérifiés dans les builds cibles : leur compatibilité réelle n’est pas établie par cet outil. Un navigateur HTTPS peut aussi refuser un backend HTTP pour contenu mixte.

Ce lot ne lance aucune écoute LAN pendant sa validation, n’installe rien et ne configure aucun hébergement public. Le vocal natif, le clavier mobile, les permissions et l’écoute sur appareils restent à essayer physiquement.
