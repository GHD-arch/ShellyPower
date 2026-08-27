# NINA.ShellyPower — Plugin NINA 3.x pour prises connectées Shelly

**Version 1.0.0** · Auteur : **Gerard** · Licence : MIT · NINA 3.0+ (testé 3.2.0.9001)

Plugin pour **NINA** (Nighttime Imaging 'N' Astronomy) permettant de piloter des **prises
connectées Shelly** (API REST Gen1 et RPC Gen2/Gen3) — jusqu'à **4 prises nommées**,
chacune avec son adresse IP.

---

## Fonctionnalités

1. **Équipement Switch** — les 4 prises apparaissent comme un hub de switches pilotables
   dans l'onglet Équipement de NINA (ON/OFF, état lu par polling automatique).
2. **Compteurs de consommation** — chaque prise expose un compteur « info only »
   (puissance en watts, tension, courant, énergie cumulée en kWh) dans la section
   Read-only de l'onglet Équipement.
3. **Instructions de séquenceur** — `Shelly Power On` / `Shelly Power Off` avec
   identification de la prise ciblée dans le nom (`Shelly Power On → Alimentation`).
4. **Panneau de configuration** — saisie des 4 noms + adresses IP, test de connexion,
   pilotage ON/OFF direct avec couleurs (vert allumée / rouge éteinte), état en direct.
5. **Détection réseau automatique** — bouton « Détecter les prises » qui scanne le
   sous-réseau local et propose les prises Shelly trouvées.
6. **Protection anti-coupure** — confirmation demandée avant toute extinction manuelle
   (toggle Équipement, bouton OFF du panneau). Le séquenceur n'est **pas** concerné
   (automatisation nocturne préservée).

## Surfaces d'utilisation

| Surface | Contenu | Persistance |
|---|---|---|
| **Équipement → Switch** (connecté via éclair) | 4 prises (toggle ON/OFF) + 4 compteurs (W) | Toute la session |
| **Panneau dockable** (menu Affichage) | Config + ON/OFF colorés + état + test | Permanent, déplaçable |
| **Engrenage Setup** (Équipement → Switch) | Même fenêtre que le panneau | Modale |
| **Options → Plugins → Shelly Power** | Même panneau (via `Core`) | Persistant |
| **Séquenceur** | Instructions On/Off avec nom de prise | Sauvegardé avec la séquence |
| **Mini-séquenceur** (onglet Imagerie) | ComboBox de sélection de prise | — |

## Prérequis

- **NINA 3.x stable** (testé 3.2, .NET 8.0)
- **SDK .NET 8.0** pour compiler (`dotnet build -c Release`)
- Prises **Shelly Gen1** (API REST `/relay/0`) ou **Gen2/Gen3** (RPC `/rpc/Switch.*`)

## Compilation

```powershell
cd ShellyPower
dotnet build -c Release
```

Résultat : `ShellyPower\bin\Release\net8.0-windows\NINA.ShellyPower.dll`

> Le `.csproj` référence les assemblies NINA depuis le dossier d'installation
> (`C:\Program Files\N.I.N.A. - Nighttime Imaging 'N' Astronomy`) avec `Private=false`.
> Si votre installation est ailleurs : `dotnet build -p:NinaInstallDir="<chemin>"`.

## Installation

1. **Fermer NINA.**
2. Copier `NINA.ShellyPower.dll` dans `%LOCALAPPDATA%\NINA\Plugins\3.0.0\ShellyPower\`.
3. **Redémarrer NINA.**

## Configuration

1. Ouvrir le panneau **Shelly Power** (engrenage Setup dans Équipement → Switch,
   ou menu Affichage, ou Options → Plugins).
2. Pour chaque prise : **Nom** (ex. `Alimentation`) + **Adresse IP** (ex. `192.168.1.52`).
3. Cliquer **Tester** → `OK — PlusPlugS (Gen2) — prise éteinte`.
4. La saisie est **sauvegardée automatiquement** (profil NINA).
5. Optionnel : cocher **Protégée** (par défaut) pour confirmer avant extinction manuelle.
6. Optionnel : cliquer **Détecter les prises** pour scanner le réseau automatiquement.

## Utilisation

### Équipement → Switch
Sélectionner **Shelly Power** → cliquer **Connecter** (éclair) → les 4 prises apparaissent
avec toggle binaire + compteurs de puissance. État actualisé par polling NINA.

### Séquenceur
Ajouter une instruction **Shelly Power On** ou **Shelly Power Off** (catégorie « Shelly Power »).
Le nom affiche la prise ciblée : `Shelly Power On → Alimentation`.
La sélection de la prise se fait via le mini-séquenceur (onglet Imagerie, ComboBox).

## Architecture technique

- **Cible** : `net8.0-windows`, `UseWPF`, `AssemblyName=NINA.ShellyPower`
- **Manifeste** : `AssemblyTitle` (nom), `AssemblyCompany` (auteur),
  `AssemblyFileVersion` (version), `Guid` (identifiant), `AssemblyMetadata` (reste)
- **MEF** : `IPluginManifest`, `IDockableVM`, `IEquipmentProvider`, `ISequenceItem` ×2,
  `ResourceDictionary` (BAML différé)
- **Templates XAML** : `Resources/Templates.xaml` compilé en BAML via `PluginResources`
  (export MEF → fusionné par NINA dans `Application.Current.Resources.MergedDictionaries`)
- **Templates d'instructions** : injection runtime dans les `Resources` du
  `ContentPresenter` du SequenceBlockView (préserve les boutons d'action de NINA)
- **Thread safety** : géométries figées (`Freeze`), `BeginInvoke` non bloquant,
  `MessageBox` marshalé sur le thread UI avec fenêtre principale comme propriétaire

## Compatibilité Shelly

| Génération | API | État | Consommation |
|---|---|---|---|
| Gen1 | `/relay/0`, `/meter/0` | ✅ | ✅ |
| Gen2 (Plus Plug S, etc.) | `/rpc/Switch.*` (+ compat Gen1) | ✅ | ✅ |
| Gen3 (Plug M, etc.) | `/rpc/Switch.*` | ✅ | ✅ |

## Licence

MIT.

---

*Auteur : Gerard · Créé le 27 août 2026 · Version 1.0.0*