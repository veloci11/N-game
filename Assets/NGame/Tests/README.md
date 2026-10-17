# Vérifications du nouveau cœur N’GAME

`NGame.Core` est du C# indépendant de Unity. `VillageSession` pilote uniquement une démonstration locale : révélation de la carte locale, nuit, aube, sélection et confirmation. Aucun rôle n’est distribué aux six personnages décoratifs, aucun total de voix ou résultat collectif n’est calculé.

`PartyComposer` propose des quantités de rôles pour six à douze personnes. Il ne lance pas une partie et n’attribue pas les rôles. Les quantités restent une hypothèse d’équilibrage à tester avec de vrais groupes.

Les tests EditMode vérifient le masquage, les phases, les sélections valides, le verrouillage jusqu’à la remise à zéro, l’isolation des sessions, les compositions et la persistance des préférences via un stockage en mémoire. Le contrôleur ne possède aucun délai : l’interface doit annuler son maintien de révélation dès le relâchement, la perte de focus, le passage en arrière-plan ou un changement de phase, puis appeler `ConcealSecret()`.

Dans Unity : **Window → General → Test Runner → EditMode**, puis exécuter `NGame.Core.Tests`.

Depuis la racine du projet, avec l’Editor de ce projet fermé :

```sh
unity test . --mode EditMode --filter NGame.Core.Tests --output /tmp/ngame-core-tests.xml --timeout 180
```

La suite PlayMode crée la nouvelle présentation complète et vérifie le maintien de 0,65 seconde, le masquage effectif des illustrations privées, les annulations, les routes et les limites du préparateur. Elle vérifie aussi les vrais pixels alpha des sprites de panneaux et de sceaux via lecture GPU, les mouvements du décor, leur arrêt avec les animations réduites et l’extinction des lampes au jour. Elle restaure les préférences après chaque cas.

```sh
unity test . --mode PlayMode --filter NGame.Presentation.Tests --output /tmp/ngame-presentation-tests.xml --timeout 180
```

La validation nécessite un rapport qui recense les tests réellement exécutés. Les événements de focus et d’arrière-plan sont envoyés aux composants pour vérifier leur réaction. Ces tests ne prouvent pas la qualité visuelle, les gestes tactiles réels, le comportement du système mobile ou l’existence d’un multijoueur. Ces éléments nécessitent des vérifications séparées sur le rendu et sur appareil.
