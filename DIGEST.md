# DIGEST — Historique de développement du plugin Shelly Power

**Projet** : NINA.ShellyPower · **Version** : 1.4.0 · **Auteur** : Gérard Hurtaud
**Date** : 28 août 2026 · **NINA** : 3.2.0.9001 · **SDK** : .NET 8.0.424
**GitHub** : https://github.com/GHD-arch/ShellyPower

## Mises à jour — Version 1.4.0 (3 septembre 2026)

- **Instruction « Shelly Power Wait »** : attend qu'une prise soit ON ou OFF (délai
  configurable, vérification toutes les 2 s). Éditeur dans le séquenceur via le pattern
  SequenceBlockView (Prise + état attendu + délai). Échec visible : notification d'erreur
  NINA + Logger.Error au délai dépassé.
- **Correctif skip silencieux** : le timeout HTTP (TaskCanceledException) était re-lancé
  par GetStatusAsync → NINA interprétait l'annulation comme un skip silencieux. Le repli
  Gen1 est désormais tenté et l'échec retourne un résultat « Injoignable » propre.
- Ajout de ShellyPowerWaitInstruction.cs + template éditeur dans Templates.xaml.

## Mises à jour — Version 1.3.0 (28 août 2026)

- **ComboBox dans le séquenceur complet** : injection des templates DataType dans les
  Resources du parent du ContentPresenter (scope element) + re-evaluation du Content —
  le menu de sélection de prise s'affiche dans l'arbre du séquenceur, boutons préservés.
- **Actualisation automatique du panneau** : timer 15 s (DispatcherTimer) dans
  ShellyPanelCore, protégé contre les chevauchements (flag _refreshing).
- **Garde-fou « PC »** : avertissement renforcé avant d'éteindre une prise dont le nom
  contient « pc »/« ordi »/« computer » (risque de couper la machine qui fait tourner NINA).
- **Log des actions** : chaque ON/OFF journalisé via NINA.Core.Utility.Logger.Info
  (panneau et toggle de l'onglet Équipement).

## Mises à jour — Version 1.2.0 (28 août 2026)

- **Polling non bloquant** :
  - timeout HttpClient réduit de 5 s à **2 s** ;
  - **pré-chargement parallèle** (ShellyClient.Prefetch) déclenché à chaque lecture de
    hub.Switches (début du cycle de polling NINA) — les 4 prises sont interrogées
    simultanément en arrière-plan pour réchauffer le cache statique (TTL 2 s) ;
  - rafraîchissement du panneau (ShellyPanelCore.RefreshStatesAsync) en **parallèle**
    (Task.WhenAll) : pire cas ~2 s au lieu de ~8 s.

---

## Résumé

Plugin NINA 3.x pour piloter 4 prises connectées Shelly (Gen1/Gen2/Gen3) depuis NINA :
équipement switch avec compteurs de consommation, instructions de séquenceur ON/OFF,
panneau de configuration coloré, détection réseau, protection anti-coupure.

## Étapes clés du développement

### 1. Structure et manifeste
- Projet SDK-style `net8.0-windows` + `UseWPF`
- Manifeste via attributs d'assembly : `AssemblyTitle` (nom), `AssemblyCompany` (auteur),
  `AssemblyFileVersion` (version 4 segments), `Guid` (identifiant)
- Métadonnées complémentaires via `AssemblyMetadata` (License, Tags, MinimumApplicationVersion…)

### 2. Équipement switch
- `ShellySwitchHub` (ISwitchHub) + `ShellySwitch` (IWritableSwitch) — 4 prises binaires
- `ShellySwitchProvider` exporté comme `IEquipmentProvider` (non-générique)
- `INotifyPropertyChanged` sur les switchs pour rafraîchir l'état dans l'onglet Équipement
- Compteurs `ShellyPowerMeter` (ISwitch read-only) pour la consommation (W, V, A, kWh)

### 3. Client HTTP Shelly
- API Gen2/Gen3 RPC (`/rpc/Switch.GetStatus`, `/rpc/Switch.Set`) en premier
- Repli Gen1 (`/relay/0`, `/meter/0`)
- Cache statique 2 s par IP pour éviter de doubler les requêtes entre switch et compteur
- Timeout 5 s, lecture modèle/app/gen via `/shelly`

### 4. Panneau de configuration
- `ShellyPanelCore` : logique partagée (propriétés, commandes, persistance)
- Exposée via `Core` sur `ShellyOptionsVM` (dockable) ET `ShellyPowerPlugin` (manifeste)
- Vue `ShellyOptionsView` : code-built UserControl (Grid + TextBoxes + boutons colorés)
- Couleurs : ON vert (#2E7D32), OFF rouge (#B71C1C), état vert/rouge/orange
- Boutons : Tester, ON, OFF, Actualiser, Enregistrer, Détecter, cases Protégée
- Sauvegarde automatique à la frappe via `PluginOptionsAccessor`

### 5. Détection réseau
- `ShellyDiscovery` : scan HTTP du /24 local en parallèle (12 threads, 700-900 ms timeout)
- `ShellyDetectionWindow` : fenêtre modale listant les prises trouvées + attribution

### 6. Protection anti-coupure
- Confirmation `MessageBox` avant extinction manuelle (toggle Équipement + bouton OFF)
- Marshalée sur le thread UI avec `MainWindow` comme propriétaire (au premier plan)
- Séquenceur non concerné (automatisation nocturne préservée)
- Case « Protégée » par prise (défaut activé), persistée dans le profil

### 7. Instructions de séquenceur
- `ShellyPowerOnInstruction` / `ShellyPowerOffInstruction` (ISequenceItem + IValidatable)
- `SelectedPlugIndex` persisté via `[JsonProperty]`
- Nom dynamique (`new string Name`) : `Shelly Power On → Alimentation`
- Templates XAML (`Templates.xaml`) compilés en BAML via `PluginResources` (export MEF)

### 8. Templates et thread safety — bugs résolus
- **Manifeste vide** : `PluginBase` lit Name/Author/Version depuis `AssemblyTitle`/`Company`/`FileVersion`, pas `AssemblyMetadata` → corrigé
- **Crash composition MEF** : `ImportingConstructor` manquant → ajouté
- **Crash affinité thread (Must create DependencySource)** : géométries créées sur thread MEF → `Freeze()`
- **Crash scellement (The calling thread cannot access)** : templates UI-thread dans RD fusionné scellé par NINA sur thread composition → retiré l'export RD, clés directes `BeginInvoke`
- **Figage NINA au lancement** : `Dispatcher.Invoke` bloquant depuis thread composition → `BeginInvoke` non bloquant
- **Templates séquenceur invisibles** : templates code-built cross-thread impossibles → SDK .NET 8 + XAML compilé (BAML différé, mécanisme officiel)
- **Boutons d'action disparus** : template `DataType` implicite remplace toute la ligne du séquenceur → retrait du `DataType` de Application.Resources, injection runtime dans les `Resources` du `ContentPresenter` individuel

### 9. Compilation
- **Build hors-ligne initial** : Roslyn `csc.dll` + response file (pas de SDK)
- **Build SDK final** : `dotnet build -c Release` avec SDK .NET 8.0.424 + `UseWPF`
- `GenerateAssemblyInfo=false` (AssemblyInfo.cs manuel)
- `global.json` pinning SDK 8.0.424
- Fichiers sources en UTF-8 avec BOM

## Fichiers principaux

| Fichier | Rôle |
|---|---|
| `NINA.ShellyPower.csproj` | Projet SDK net8.0-windows + UseWPF |
| `AssemblyInfo.cs` | Manifeste (Title, Company, Version, Guid, Metadata) |
| `ShellyPowerPlugin.cs` | Point d'entrée MEF + Core pour page Plugins |
| `ShellyPanelCore.cs` | Logique partagée du panneau (propriétés, commandes) |
| `ShellyOptionsVM.cs` | VM dockable (DockableVM) + RegisterViewTemplate |
| `ShellyOptionsView.cs` | Vue WPF code-built (Grid + contrôles colorés) |
| `ShellySwitch.cs` | Hub + switchs writable + provider MEF + SetupDialog |
| `ShellyPowerMeter.cs` | Compteur consommation (ISwitch read-only) |
| `ShellyClient.cs` | Client HTTP Gen1/Gen2/Gen3 + cache |
| `ShellyOptions.cs` | Persistance options (PluginOptionsAccessor) |
| `ShellyDiscovery.cs` | Scan réseau /24 |
| `ShellyDetectionWindow.cs` | Fenêtre de détection + attribution |
| `ShellyPowerInstructions.cs` | Instructions On/Off + injection templates |
| `ShellyStrings.cs` | Libellés bilingues (anglais par défaut, français si culture fr) |
| `PluginResources.cs` | Export MEF ResourceDictionary (BAML différé) |
| `Resources/Templates.xaml` | Templates XAML (dock + options + mini-séquenceur) |
| `global.json` | Pinning SDK 8.0.424 |

---

## Mises à jour — Version 1.1.0 (28 août 2026)

- **Icône propre** : symbole « power » (⏻) en géométrie vectorielle (anneau 270° + tige)
  remplaçant les rectangles gris dans le séquenceur, le dock et les menus NINA.
- **Internationalisation** : classe `ShellyStrings` (anglais par défaut, français si
  culture UI `fr`) appliquée à tous les libellés du panneau, de la détection, des
  instructions de séquenceur, des messages de progression, des confirmations
  d'extinction et des états (on/off/unknown).
- **Description du manifeste** passée en anglais (ShortDescription + LongDescription).
- **Noms de prise par défaut bilingues** : « Plug 1 » (EN) / « Prise 1 » (FR), avec
  migration des anciennes valeurs « Prise N » persistées.
- **Pied de page du panneau** restructuré en StackPanel horizontal pleine largeur
  (le bouton « Detect plugs » était tronqué par la colonne étroite du bouton Tester).
- **Version** : 1.0.0 → 1.1.0 (AssemblyFileVersion).