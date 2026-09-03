# AFAIRE — Améliorations futures et versionning

**Projet** : NINA.ShellyPower · **Version actuelle** : 1.4.0 · **Auteur** : Gérard Hurtaud
**Date** : 3 septembre 2026 · **GitHub** : https://github.com/GHD-arch/ShellyPower

---

## Versionning (SemVer)

| Version | Statut | Description |
|---|---|---|
| 1.0.0 | ✅ Livrée | Version stable fonctionnelle |
| 1.1.0 | ✅ Livrée | Icône, internationalisation, noms de prises bilingues, pied de page corrigé |
| 1.2.0 | ✅ Livrée | Correctifs : polling non bloquant |
| **1.3.0** | ✅ Livrée | ComboBox séquenceur (pattern SequenceBlockView), identification par nom, auto-refresh, garde-fou PC, log actions |
| **1.4.0** | ✅ Actuelle | Instruction « Shelly Power Wait » + correctif skip silencieux |
| 1.5.0 | Planifié | Fonctionnalités restantes (voir ci-dessous) |
| 2.0.0 | Planifié | Publication galerie NINA (voir ci-dessous) |

### Comment versionner
1. Modifier `AssemblyFileVersion` dans `AssemblyInfo.cs` (4 segments : `1.4.0.0`)
2. Mettre à jour la version dans `README_FR.md`, `README_EN.md` et `DIGEST.md`
3. `dotnet build -c Release` → déployer le DLL
4. Tag Git : `git tag v1.4.0` (si dépôt Git)

---

## Fonctionnalités (1.5.0)

| # | Description | Intérêt |
|---|---|---|
| 1 | ~~**Support authentification Shelly**~~ — ❌ supprimé (non souhaité) | — |
| 2 | ~~**Instruction « Attendre état »**~~ — ✅ fait en 1.4.0 (ShellyPowerWaitInstruction : prise + état ON/OFF + délai, notification d'erreur visible, correctif skip silencieux) | — |
| 3 | **Condition de séquence** : « Si prise allumée alors… » (`ISequenceCondition`) | Automatisation |
| 4 | **Instruction Toggle** + **durée** (« allumer 10 min puis éteindre ») | Confort |
| 5 | **Déclencheur** : couper automatiquement à la fin du safe-to-observe (`ISequenceTrigger`) | Automatisation |
| 6 | **Export/import config** : partage entre profils ou machines | Confort |
| 7 | **Prises individuelles** dans le sélecteur d'équipement (au lieu d'un hub unique) | Scalabilité |

---

## Publication sur la galerie NINA (2.0.0)

| Étape | Description |
|---|---|
| 1 | Créer un dépôt GitHub `GHD-arch/ShellyPower` avec le code + release (zip du DLL) |
| 2 | Soumettre le manifeste à [nina.plugin.manifests](https://github.com/isbeorn/nina.plugin.manifests) |
| 3 | Installation en 1 clic depuis NINA (PluginInstaller) |
| 4 | CI/CD : GitHub Actions pour build auto + release à chaque tag |

---

## Corrections mineures en attente

| # | Description | Priorité |
|---|---|---|
| 1 | **Garde-fou « PC »** dans les instructions de séquenceur (actuellement seulement panneau + Équipement) | Basse |
| 2 | **Icône du plugin dans la sidebar** du séquenceur (l'ExportMetadata "Icon" = "ButtonSVG" générique) | Basse |
| 3 | **Mise à jour automatique du nom** des instructions quand une prise est renommée dans le panneau (actuellement au rechargement de la séquence) | Basse |

---

## Notes — problème ComboBox du séquenceur : RÉSOLU (1.3.0)

Trouvé dans la source NINA clonée (`NINA.Sequencer\SequenceItem\Switch\Datatemplates.xaml`) :
le pattern officiel consiste à envelopper l'éditeur dans un **nouveau `SequenceBlockView`**
dont la propriété `SequenceItemContent` (DependencyProperty du code-behind) reçoit les
contrôles. Ce blockview interne fournit la ligne complète avec TOUS les boutons d'action
de NINA autour de l'éditeur personnalisé :

```xml
<DataTemplate DataType="{x:Type sp:ShellyPowerOnInstruction}">
    <view:SequenceBlockView>
        <view:SequenceBlockView.SequenceItemContent>
            <StackPanel Orientation="Horizontal">
                <!-- contrôles de l'éditeur (ON/OFF + ComboBox) -->
            </StackPanel>
        </view:SequenceBlockView.SequenceItemContent>
    </view:SequenceBlockView>
</DataTemplate>
```

Les approches antérieures (DataType dans Application.Resources, injection dans le
ContentPresenter ou son parent, thème Generic.xaml) remplaçaient la ligne entière ou
arrivaient trop tard — ce pattern les rend obsolètes.

---

*Auteur : Gérard Hurtaud · Dernière mise à jour : 3 septembre 2026*