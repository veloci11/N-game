# Narration publique française — N’GAME

Treize WAV générés localement le 20 octobre 2026 dans `Assets/Resources/Werewolf/Narration/`. Il s’agit d’annonces publiques de phase pour la table en présence, distinctes du chat vocal des joueurs limité au vote distant. Ce lot ne modifie aucun moteur, réglage ou script C# Unity ; l’intégration et sa recette appartiennent à la racine.

## Textes et mesures réelles

| Fichier sans extension | Texte public original | Durée PCM mesurée |
| --- | --- | --- |
| distribution | Chacun reçoit son secret. Regardez votre carte en privé, puis confirmez. La nuit commencera quand le village sera prêt. | 7.058 s |
| cupid | Yeux fermés. Cupidon s’éveille. | 1.687 s |
| lovers | Yeux fermés. Les amoureux se reconnaissent, puis se rendorment. | 3.275 s |
| guard | Yeux fermés. Le Garde s’éveille. | 1.815 s |
| seer | Yeux fermés. La Voyante s’éveille. | 1.848 s |
| wolves | Yeux fermés. Les loups s’éveillent. | 1.603 s |
| witch | Yeux fermés. La Sorcière s’éveille. | 1.922 s |
| hunter | Une dernière action est attendue. | 1.741 s |
| dawn | Le village se réveille. | 1.400 s |
| discussion | Le village débat. Partagez vos soupçons. | 2.369 s |
| vote | Le vote est ouvert. Choisissez en privé. | 2.381 s |
| verdict | Le verdict est affiché. | 1.464 s |
| finished | Le conte est terminé. Découvrez les rôles. | 2.359 s |

Tous les fichiers sont mono, PCM signé 16 bits, 24 000 Hz. Pic mesuré : −3,000 dBFS ; aucun échantillon écrêté. Les annonces de rôle de nuit et du Chasseur restent sous 2,8 s, la reconnaissance des amoureux sous 6 s, la distribution entre 5 et 8 s. Aucun son n’a été tronqué pour atteindre ces limites. La vitesse TTS vaut 1,05 pour la distribution et 1,15 pour les autres clips ; deux fondus de bord de 5 ms évitent les clics.

Le [manifeste](narration-manifest.json) conserve textes, vitesses, durées, RMS, pics, nombre d’échantillons écrêtés, formats et SHA-256 de chaque WAV. Les mesures ne prouvent pas la prononciation, le confort acoustique ou la compréhension : écoute française et vraie partie en présence restent à effectuer. L’amont signale lui-même une qualité moins stable sur les phrases très courtes.

## Contrat d’intégration

- Une seule source publique à la table, sur le téléphone de l’hôte en présence, avec préférence explicite de narration ; aucune diffusion dans l’entraînement ou à distance.
- Chaque annonce dépend seulement de la phase et de l’appel public, jamais du joueur titulaire, de sa cible, d’une carte privée ou d’une potion restante. Les appels nécessaires à la confidentialité restent publics selon le moteur.
- Un clip est attaché au couple manche/tour et s’arrête dès changement de tour, interruption ou perte des droits de narration. Un vieux chargement ou retour au premier plan ne rejoue pas un ancien appel.
- Les annonces nocturnes commencent par « Yeux fermés » : le rôle précédent se rendort et seul le rôle appelé ouvre les yeux, selon le protocole annoncé avant distribution. Les annonces ne remplacent pas la confirmation du rôle ni les actions privées.
- L’aube autorise tous les joueurs à rouvrir les yeux. Les annonces génériques de verdict/fin ne doivent pas être utilisées pour déduire une victime, un camp ou un gagnant.

Activer les appels au milieu d’un tour prend effet au prochain appel : aucun ancien son n’est rejoué volontairement. La phase actuelle reste affichée et peut être lue par le lecteur organisé.

Le réglage silencieux doit conserver les consignes écrites et expliquer le recours à un lecteur public organisé hors des secrets. Les textes n’annoncent aucune identité et aucune nouvelle règle.

## Reproduction hors ligne

Python utilisé : 3.12.9. Dépendances exactes : `Tools/narration-requirements.txt` ; versions complètes dans le manifeste. Installer ces dépendances dans un environnement séparé, puis fournir les deux fichiers déjà téléchargés :

```sh
python Tools/generate_narration.py \
  --model /chemin/externe/kokoro-v1.0.onnx \
  --voices /chemin/externe/voices-v1.0.bin
```

Pour régénérer seulement la réaction publique tout en conservant les autres WAV et en fusionnant le manifeste existant, ajouter `--only hunter`. Cette variante exige les mêmes fichiers modèle/voix.

Le script n’effectue aucun téléchargement ni requête réseau. Aucun micro, clé API, texte de joueur ou service payant n’est utilisé. Les poids restent hors du dépôt et hors du build. L’environnement de génération courant est dans `/tmp/ngame-narration-20261020/venv` ; ce chemin temporaire n’est pas un prérequis permanent.

Fichiers téléchargés par la racine depuis la publication `model-files-v1.1` du convertisseur : [kokoro-v1.0.onnx](https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.1/kokoro-v1.0.onnx) et [voices-v1.0.bin](https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.1/voices-v1.0.bin). Le nom de publication du convertisseur ne change pas le nom v1.0 des fichiers.

SHA-256 modèle : `beb0d1848dee9a49da392cc3df26958d46cfa35d321edf434f52949153f0df3a`.

SHA-256 banque de voix : `bca610b8308e8d99f32e6fe4197e7ec01679264efed0cac9140fe9c29f1fbf7d`.

La génération doit produire des fichiers conformes ; une nouvelle inférence peut légèrement varier, même dans le même environnement, et l’identité binaire n’est pas promise. Les SHA décrivent les fichiers livrés, pas une sortie garantie à toute réexécution. Le script refuse formats inattendus, silence, durées hors contrat et écrêtage. Un texte trop long doit être raccourci ; la vitesse ne dépasse pas 1,4.

## Provenance et licences vérifiées

- [Kokoro-82M, modèle officiel](https://huggingface.co/hexgrad/Kokoro-82M) : poids annoncés Apache-2.0 ; copie du texte officiel Apache conservée. Le modèle n’offre pas de fichier LICENSE distinct à cette adresse, donc la preuve de déclaration est sa fiche officielle.
- [kokoro-onnx](https://github.com/thewh1teagle/kokoro-onnx) : wrapper MIT, Copyright (c) 2025 github.com/thewh1teagle ; texte de licence du dépôt copié intégralement.
- [Liste officielle des voix](https://huggingface.co/hexgrad/Kokoro-82M/blob/main/VOICES.md) : `ff_siwis`, français, associée au corpus SIWIS. Aucune voix de l’utilisateur ou personne désignée n’est fournie au modèle.
- [SIWIS French Speech Synthesis Database](https://doi.org/10.7488/ds/1705) : Yamagishi, Junichi; Honnet, Pierre-Edouard; Garner, Philip; Lazaridis, Alexandros (2017), University of Edinburgh, Centre for Speech Technology Research. README officiel et licence CC BY 4.0 téléchargés depuis DataShare et conservés ; la fiche Kokoro confirme ce corpus dans ses données d’entraînement.

Les notices et attributions sont dans `Documentation/Licenses/Narration-NOTICE.txt`, également copiées dans `Assets/StreamingAssets/Licenses/` pour accompagner les builds. Le [registre des licences](narration-license-sources.json) conserve URL source et SHA-256. L’attribution n’implique aucun soutien des auteurs. Les WAV sont des sorties synthétiques normalisées, avec textes originaux N’GAME ; ce ne sont pas des extraits copiés du corpus.
