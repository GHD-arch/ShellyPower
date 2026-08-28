# AFAIRE — Améliorations futures et versionning

**Projet** : NINA.ShellyPower · **Version actuelle** : 1.0.0 · **Auteur** : Gérard Hurtaud
**Date** : 28 août 2026 · **GitHub** : https://github.com/GHD-arch/ShellyPower

---

## Versionning (SemVer)

| Version | Statut | Description |
|---|---|---|
| **1.0.0** | ✅ Actuelle | Version stable fonctionnelle |
| 1.0.1 | À faire | Correctifs (voir ci-dessous) |
| 1.1.0 | Planifié | Nouvelles fonctionnalités (voir ci-dessous) |
| 2.0.0 | Futur | Breaking changes (refactoring majeur) |

### Comment versionner
1. Modifier `AssemblyFileVersion` dans `AssemblyInfo.cs` (4 segments : `1.0.1.0`)
2. Mettre à jour la version dans `README.md` et `DIGEST.md`
3. `dotnet build -c Release` → déployer le DLL
4. Tag Git : `git tag v1.0.1` (si dépôt Git)

---

## Correctifs (1.0.1)

| # | Description | Priorité |
|---|---|---|
| 1 | **ComboBox dans le séquenceur complet** : injecter les templates DataType dans les Resources du SequenceBlockView (pas du ContentPresenter) via un `DispatcherTimer` 100 ms après `Loaded`, pour laisser les bindings se résoudre avant l'injection + forcer la réévaluation | Haute |
| 2 | **Polling non bloquant** : réduire le timeout HttpClient à 2 s et interroger les 4 prises en parallèle (au lieu de séquentiel) pour éviter 20 s de blocage si toutes injoignables | Moyenne |
| 3 | **Actualisation auto du panneau** : timer 10 s dans `ShellyPanelCore` pour rafraîchir l'état des prises configurées (au lieu du bouton manuel uniquement) | Moyenne |
| 4 | **Garde-fou « PC »** : avertissement renforcé si le nom de la prise contient « PC » (risque de couper la machine qui fait tourner NINA) | Basse |
| 5 | **Log des actions** : journaliser ON/OFF dans le log NINA (qui a allumé/éteint quoi, quand) | Basse |

---

## Nouvelles fonctionnalités (1.1.0)

| # | Description | Intérêt |
|---|---|---|
| 1 | **Support authentification Shelly** : identifiants dans les options + en-tête Basic/Auth pour les Gen2/Gen3 avec `auth_en=true` | Sécurité |
| 2 | **Instruction « Attendre état »** : attendre que la prise soit ON/OFF avec timeout (validable dans la séquence) | Automatisation |
| 3 | **Condition de séquence** : « Si prise allumée alors… » (`ISequenceCondition`) | Automatisation |
| 4 | **Instruction Toggle** + **durée** (« allumer 10 min puis éteindre ») | Confort |
| 5 | **Déclencheur** : couper automatiquement à la fin du safe-to-observe (`ISequenceTrigger`) | Automatisation |
| 6 | **Icône SVG propre** pour le plugin (au lieu des rectangles gris) | Esthétique |
| 7 | **Traduction anglaise** des libellés (via `Loc` de NINA) | Internationalisation |
| 8 | **Export/import config** : partage entre profils ou machines | Confort |
| 9 | **Prises individuelles** dans le sélecteur d'équipement (au lieu d'un hub unique) | Scalabilité |

---

## Publication sur la galerie NINA (1.2.0+)

| Étape | Description |
|---|---|
| 1 | Créer un release GitHub (zip du DLL + manifeste) |
| 2 | Soumettre le manifeste à [nina.plugin.manifests](https://github.com/isbeorn/nina.plugin.manifests) |
| 3 | Installation en 1 clic depuis NINA (PluginInstaller) |
| 4 | CI/CD : GitHub Actions pour build auto + release à chaque tag |

---

## Notes techniques pour les correctifs

### ComboBox dans le séquenceur complet (correctif #1)
Le `ContentPresenter` « SequenceItemContent » du `SequenceBlockView` de NINA résout
l'éditeur par lookup implicite `DataType`. Un template `DataType` dans
`Application.Resources` est aussi trouvé par l'`ItemsControl` parent (remplaçant toute
la ligne + boutons). Solution : injecter le template dans les `Resources` du
`SequenceBlockView` lui-même (scope élément) via un `DispatcherTimer` après `Loaded` :

```csharp
// Pseudo-code
DispatcherTimer t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
t.Tick += (s, e) => {
    var blockView = FindAncestor<SequenceBlockView>(cp);
    if (blockView != null && !blockView.Resources.Contains(typeof(OnInstruction))) {
        blockView.Resources[typeof(OnInstruction)] = BuildEditorTemplate("ON", "#FF66BB6A");
        cp.Content = null; cp.Content = content; // forcer réévaluation
    }
    t.Stop();
};
t.Start();
```

### Polling parallèle (correctif #2)
Remplacer la boucle séquentielle `for i in 0..3: await client.GetStatusAsync(ip)` par
`Task.WhenAll(plugs.Select(p => client.GetStatusAsync(p.Ip)))` dans le polling du hub.

---

*Auteur : Gerard · Dernière mise à jour : 27 août 2026*