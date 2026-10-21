# Vérification Mac — éléments séparés

Build Unity StandaloneOSX réussi : 0 erreur, 346 avertissements de shaders AI/Sentis, 19 281 ms, 212 989 857 octets. Voir `build-mac-cutouts.json`.

- `plutil -lint Builds/Mac/NGame.app/Contents/Info.plist` : OK.
- `codesign --force --deep --sign - Builds/Mac/NGame.app`, puis `codesign --verify --deep --strict` : sortie 0. Signature locale ad hoc, sans notarisation.
- `unity build run` : lanceur PID 7115 ; processus enfant exact vérifié : PID 7116, `/Users/mathis/Code/N-game/Builds/Mac/NGame.app/Contents/MacOS/N’GAME`.

Le processus natif a été lancé ; les captures relues sont celles de l’éditeur Unity. Ce contrôle ne constitue pas une recette de l’interface native ou sur téléphone. Aucun service public déployé.
