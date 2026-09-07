# AFAIRE — Améliorations futures et versionning

**Projet** : NINA.ShellyPower · **Version actuelle** : 1.5.0 · **Auteur** : Gérard Hurtaud
**Date** : 3 septembre 2026 · **GitHub** : https://github.com/GHD-arch/ShellyPower

---

## Versionning (SemVer)

| Version | Statut | Description |
|---|---|---|
| 1.0.0 | ✅ Livrée | Version stable fonctionnelle |
| 1.1.0 | ✅ Livrée | Icône, internationalisation, noms de prises bilingues, pied de page corrigé |
| 1.2.0 | ✅ Livrée | Correctifs : polling non bloquant |
| **1.3.0** | ✅ Livrée | ComboBox séquenceur (pattern SequenceBlockView), identification par nom, auto-refresh, garde-fou PC, log actions |
| **1.4.0** | ✅ Livrée | Instruction « Shelly Power Wait » + correctif skip silencieux |
| **1.5.0** | ✅ Actuelle | Condition While, Toggle, déclencheur Unsafe, icône, correctifs affichage, GUIDE + LICENSE + .gitignore |
| 1.6.0 | Planifié | Fonctionnalités restantes (voir ci-dessous) |
| 2.0.0 | Planifié | Publication galerie NINA (voir ci-dessous) |

### Comment versionner
1. Modifier `AssemblyFileVersion` dans `AssemblyInfo.cs` (4 segments : `1.4.0.0`)
2. Mettre à jour la version dans `README_FR.md`, `README.md` et `DIGEST.md`
3. `dotnet build -c Release` → déployer le DLL
4. Tag Git : `git tag v1.4.0` (si dépôt Git)

---

## Fonctionnalités (1.6.0)

| # | Description | Intérêt |
|---|---|---|
| 1 | ~~**Support authentification Shelly**~~ — ❌ supprimé (non souhaité) | — |
| 2 | ~~**Instruction « Attendre état »**~~ — ✅ fait en 1.4.0 (ShellyPowerWaitInstruction : prise + état ON/OFF + délai, notification d'erreur visible, correctif skip silencieux) | — |
| 3 | ~~**Condition de séquence**~~ — ✅ fait en 1.5.0 (ShellyPowerCondition : watchdog 3 s, interruption propre, nom dynamique) | — |
| 4 | ~~**Instruction Toggle + durée**~~ — ✅ fait en 1.5.0 (bascule, attente, restauration état initial) | — |
| 5 | ~~**Déclencheur unsafe**~~ — ✅ fait en 1.5.0 (ShellyPowerUnsafeTrigger : coupure sur safety monitor UNSAFE, instructions imbriquées) | — |
| 6 | ~~**Export/import config**~~ — ✅ fait en 1.5.0 (Exporter/Importer JSON : noms, IP, protections ; rechargement des états après import) | — |
| 7 | **Prises individuelles** dans le sélecteur d'équipement (au lieu d'un hub unique) | Scalabilité |
| 8 | **Canaux multi-sorties** — ❌ en attente : paramètre « canal » (0-3) par emplacement pour les appareils multi-prises (Shelly Power Strip 4 Gen4, Pro 4, 4PM — `/rpc/Switch.GetStatus?id=N`, `/relay/N`). Aujourd'hui seul le canal 0 est piloté. À faire si achat d'un appareil multi-sorties (se combine avec l'item 7). | Scalabilité |

> ✅ **Fait en 1.5.0** : icône ⏻ partout (ShellyIcons en code), templates `_Mini` des 6 entités,
> message de résultat sur une ligne (fix frame 75 % du dock Acquisition), GUIDE.md bilingue,
> LICENSE MIT, .gitignore, dépôt publié sur GitHub avec tag `v1.5.0`.

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
| 2 | ~~**Icône du plugin**~~ — ✅ fait en 1.5.0 (ShellyIcons.BuildPowerIcon construite en code, metadata Icon retirée ; icône du dockable corrigée — le carré blanc était un RectangleGeometry) | — |
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
