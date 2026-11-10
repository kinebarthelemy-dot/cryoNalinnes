# Gestion Cryo

Petite application Windows Forms (C# / .NET 8) pour gérer les abonnements de séances de cryothérapie.

## Fonctionnalités

- Fichier client : nom, téléphone, séances restantes (3 par défaut)
- Bouton « Séance effectuée » : décrémente le compteur et enregistre la date/heure de la séance
- Historique par client avec dates, suppression d'une séance pour corriger une erreur (+1 au compteur)
- Agenda / Journal : sélectionner une date et voir toutes les personnes venues ce jour-là, avec l'heure
- Couleurs : ligne rouge = plus de séance, jaune = dernière séance
- Les données sont sauvegardées automatiquement dans `%APPDATA%\GestionCryo\clients.json`
- Export de sauvegarde : fichier JSON complet (restaurable) et export CSV lisible dans Excel

## Compilation

Prérequis : [Visual Studio 2022](https://visualstudio.microsoft.com/) avec la charge de travail
« Développement desktop .NET », ou le [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) seul.

Ouvrir `CryoGestion.csproj` dans Visual Studio et appuyer sur F5, ou en ligne de commande :

```
dotnet run
```

Pour produire un exécutable autonome :

```
dotnet publish -c Release -r win-x64 --self-contained true
```

L'exécutable se trouve alors dans `bin\Release\net8.0-windows\win-x64\publish\GestionCryo.exe`.
