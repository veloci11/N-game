# Nuit sur écran — validation moteur et HTTP

Sources ajoutent `RoomSettings.nightProtocol` : `traditional` par défaut, `screen` explicite. Les projections publiques et sauvegardes copient ce réglage. `null` issu d’une ancienne sauvegarde devient `traditional` ; les autres valeurs inconnues sont refusées. Le protocole écran refuse la Petite Fille et ne ferme aucun appel avant son échéance, même après toutes les soumissions ou sur demande de l’hôte. Les règles traditionnelles restent inchangées.

Résultats exécutés :

- NUnit moteur hors Editor : **76/76**, zéro échec, dont neuf nouveaux cas. Source réelle `WerewolfRoomTests.cs`, runner temporaire `/tmp/ngame-core-validation-20261020/CoreValidation.csproj`. Rapport `night-protocol-core.log`. Pas une exécution Unity EditMode.
- Build ASP.NET Core : **0 erreur, 37 avertissements nullable**, 1,35 s. SDK .NET fourni par Unity 6000.6.6f1.
- HTTP réel : **214 contrôles/requêtes**, zéro échec, serveur localhost isolé, arrêté en fin de recette. Rapport `night-protocol-http.log`. Les comptes incluent les polls ; ce ne sont pas 214 scénarios humains.

La recette HTTP vérifie le champ absent, les valeurs inconnues, le refus de la Petite Fille, la projection identique aux six membres, la conservation de la préparation après réglage refusé, l’appel Voyante maintenu après soumission et trois secondes d’attente, le refus d’avance anticipée, la reprise du même appel après redémarrage avec cible/passage reçu et même échéance, puis le passage automatique aux loups à l’échéance. Les vues publiques ne révèlent pas les rôles des acteurs. Les capacités de session ne sont pas imprimées ni écrites dans les logs serveur.

Le fingerprint conserve le format des anciens réglages traditionnels pour ne pas invalider leurs reçus lors d’une reprise ; le réglage écran est ajouté au fingerprint pour refuser un changement de contenu sous le même identifiant. Les tests couvrent la copie détachée, l’annulation des prêts et la restauration écran.

Reproduire :

```sh
/Applications/Unity/Hub/Editor/6000.6.6f1-arm64/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet build Server/NGame.Server/NGame.Server.csproj --nologo
python3 Server/NGame.Server/night_protocol_smoke.py
```

Cette preuve porte sur le moteur et le transport. Les animations publiques/privées, les mini-jeux, l’ergonomie sur appareil et les comportements réels autour de la table doivent être vérifiés séparément. Aucun hébergement public n’est déployé.
