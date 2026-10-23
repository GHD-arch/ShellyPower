# Publication v1.5.0 — pas à pas (Release GitHub + Galerie NINA)

## Étape 1 — Créer la Release GitHub (avec le zip)

Le tag `v1.5.0` existe déjà sur https://github.com/GHD-arch/ShellyPower.

1. Ouvrir https://github.com/GHD-arch/ShellyPower/releases
2. Cliquer **« Draft a new release »** (ou « Releases → New release »)
3. Dans **Choose a tag** : sélectionner le tag existant **`v1.5.0`**
4. Titre : `v1.5.0 — Sequencer automation kit (While, Toggle, Unsafe) + export/import`
5. Description (copier depuis DIGEST.md, section 1.5.0) :
   - New: condition « While », instruction « Toggle », safety trigger « Unsafe »
   - Custom power icon everywhere, compact mini-sequencer cards
   - JSON configuration export/import, panel display fixes
   - Bilingual GUIDE.md, MIT LICENSE, clean repository
6. **Attacher le fichier** `E:\DEV\AstroToggle\AstroToggle\ShellyPower\_release\NINA.ShellyPower.zip`
   (glisser-déposer dans la zone « Attach binaries »)
7. **Publish release**

⚠️ Ne pas modifier le zip après : le manifeste référence son SHA256.

## Étape 2 — Soumettre le manifeste à la galerie NINA

1. Ouvrir https://github.com/isbeorn/nina.plugin.manifests
2. **Fork** le dépôt (bouton Fork, en haut à droite)
3. Dans votre fork, créer le fichier : bouton **Add file → Create new file**
   - Nom du fichier : `Shelly Power.json` (convention : nom du plugin)
4. Coller le contenu de `_release\manifest.json` (ci-dessous)
5. **Commit** sur une nouvelle branche, puis **Create pull request** vers `isbeorn/main`
   - Titre de la PR : « Add Shelly Power plugin manifest »
   - Décrire : fonctionnalités, URL du dépôt, tag de release

Le contenu à coller (déjà prêt dans `_release\manifest.json`) :

```json
{
  "Name": "Shelly Power",
  "Author": "Gérard Hurtaud",
  "Version": "1.5",
  "Description": "Control up to 4 Shelly smart plugs from NINA: switch hub with power meters, sequencer instructions (On, Off, Wait, Toggle, While condition, Unsafe trigger), network auto-detection, safety protections and bilingual UI.",
  "Tags": [ "Power", "Switch", "Shelly" ],
  "License": "MIT",
  "Homepage": "https://github.com/GHD-arch/ShellyPower",
  "Repository": "https://github.com/GHD-arch/ShellyPower",
  "DownloadURL": "https://github.com/GHD-arch/ShellyPower/releases/download/v1.5.0/NINA.ShellyPower.zip",
  "DownloadChecksum": "B3466625F44773A79095E7359340CEDD7778A0CC1C89AA6E332C221DFEBA98F6",
  "MinimumApplicationVersion": "3.0",
  "MinimumOSVersion": "10.0",
  "AssemblyName": "NINA.ShellyPower.dll"
}
```

## Étape 3 — Après la fusion de la PR

Stefan Berg (isbeorn) relit et fusionne. Ensuite :
- tous les utilisateurs NINA voient « Shelly Power » dans Options → Plugins ;
- installation en 1 clic ;
- pour une future version (1.6.0) : build → Release GitHub (nouveau tag) → mettre à jour Version/DownloadURL/Checksum dans le manifeste (nouvelle PR ou edit).

## Récapitulatif des fichiers préparés

| Fichier | Usage |
|---|---|
| `_release/NINA.ShellyPower.zip` | À attacher à la Release GitHub (DLL 1.5.0.0 vérifié) |
| `_release/manifest.json` | À coller dans la PR sur nina.plugin.manifests |
| `_release/INSTRUCTIONS.md` | Ce pas-à-pas |