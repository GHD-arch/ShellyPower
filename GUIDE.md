# NINA.ShellyPower — Manuel d'utilisation / User Guide

**Version 1.4.0** · Auteur / Author: **Gérard Hurtaud** · Licence / License: MIT
· **GitHub** : https://github.com/GHD-arch/ShellyPower
· NINA 3.0+ (testé / tested 3.2.0.9001)

---

# 🇫🇷 FRANÇAIS

## 1. Présentation

Le plugin **Shelly Power** permet de piloter jusqu'à **4 prises connectées Shelly** directement depuis NINA :

- allumer / éteindre des matériels (PC, caméra, déwarmer, routeur WiFi…) ;
- suivre la **consommation** (watts, volts, ampères, kWh) ;
- automatiser dans le **séquenceur** : allumage/extinction, attente d'état, bascule temporisée, condition d'exécution et coupure de sécurité.

Prises compatibles : **Shelly Gen1** (`/relay/0`), **Gen2** (Plus Plug S…), **Gen3** (Plug M…) — API REST et RPC, avec repli automatique.

## 2. Installation

1. Fermer NINA.
2. Copier `NINA.ShellyPower.dll` dans `%LOCALAPPDATA%\NINA\Plugins\3.0.0\ShellyPower\`.
3. Relancer NINA — le log affiche : `Successfully loaded plugin Shelly Power …`.

## 3. Configurer les prises

Ouvrir le panneau **Shelly Power** (trois accès possibles) :

- menu **Affichage → Shelly Power** (panneau dockable, déplaçable) ;
- onglet **Équipement → Switch** → engrenage **Setup** ;
- **Options → Plugins → Shelly Power**.

Pour chaque prise :

1. **Nom** : le nom de la fonction (ex. `Alimentation`, `Caméra`, `Déwarmer`, `PC`).
2. **Adresse IP** : l'IP fixe de la prise (ex. `192.168.1.52`).
3. **Tester** : vérifie la connexion — `OK — PlusPlugS (Gen2) — prise éteinte`.
4. La saisie est **sauvegardée automatiquement** dans le profil NINA.
5. **Protégée** (coché par défaut) : confirmation avant toute extinction manuelle.
6. **Détecter les prises** : scan automatique du sous-réseau (toutes les prises Shelly trouvées sont proposées ; cliquer sur la ligne pour l'attribuer à un emplacement).
7. Les boutons colorés **ON (vert)** / **OFF (rouge)** pilotent chaque prise immédiatement.
8. L'état s'**actualise automatiquement** toutes les 15 s (ou via « Actualiser l'état »).

> 💡 Conseil : attribuez une **IP fixe** à chaque prise (réservation DHCP dans la box) pour que la configuration reste stable.

## 4. Connecter l'équipement Switch

1. Onglet **Équipement → Switch** : choisir **Shelly Power** puis cliquer sur l'éclair **Connecter**.
2. Les **4 prises** apparaissent comme switchs (toggle ON/OFF) + **compteurs de consommation** (W) en lecture seule.
3. L'état est rafraîchi par polling automatique (pré-chargement parallèle, timeout 2 s).

## 5. Les 6 entités du séquenceur

Dans le séquenceur (catégorie **Shelly Power**), avec le même éditeur que les instructions intégrées (icône ⏻, boutons d'action préservés) :

### Shelly Power On / Off — instructions
> `ON | Prise : [▼ Alimentation]` · `OFF | Prise : [▼ Alimentation]`

Allume ou éteint la prise sélectionnée. Le nom de l'instruction affiche la prise ciblée : `Shelly Power On → Alimentation`.

### Shelly Power Wait — attendre un état
> `Prise : [▼ Alimentation] [▼ OFF/ON] Délai (s) : [60]`

Interroge la prise toutes les 2 s jusqu'à l'état attendu, au plus tard le délai configuré. Au délai dépassé : **notification d'erreur visible** + échec de l'instruction (la séquence s'arrête ou applique le comportement « On Error »). Utilisez-le après un `On` pour confirmer que le matériel est réellement alimenté avant d'imager.

### Shelly Power Toggle — bascule temporisée
> `Prise : [▼ Déwarmer] Durée (min) : [10]`

1. lit l'état actuel de la prise ;
2. la **bascule** (éteinte → allumée, allumée → éteinte) ;
3. **attend la durée** (progression affichée, annulable) ;
4. **restaure l'état initial** automatiquement.

Idéal pour « allumer le déwarmer 10 min pendant les flats puis revenir comme avant ».

### Shelly Power While — condition
> `Prise : [▼ Alimentation] [▼ OFF/ON]`

Le **bloc d'instructions s'exécute tant que** la prise est dans l'état choisi. Un watchdog vérifie l'état toutes les 3 s : dès que l'état change (ou prise injoignable), le bloc est **interrompu proprement** avec une notification d'avertissement.

### Shelly Power Unsafe — déclencheur de sécurité
> `⚠ Unsafe → Prise : [▼ PC]`

Surveille le **safety monitor** : dès qu'il passe **UNSAFE** (nuages, pluie, couvercle ouvert — fin du safe-to-observe), la prise sélectionnée est **coupée automatiquement** (notification + log). Vous pouvez déposer des instructions dans le déclencheur (ex. ranger le télescope) — elles s'exécutent après la coupure. Notifications limitées à 1/minute pendant la période unsafe.

## 6. Protections

| Surface | Comportement |
|---|---|
| Panneau, bouton OFF | Confirmation (si « Protégée ») + avertissement renforcé si le nom contient `pc`/`ordi`/`computer` (risque de couper la machine qui fait tourner NINA) |
| Équipement, toggle switch | Idem |
| Séquenceur | **Inconditionnel** (automatisation nocturne préservée) — actions loggées dans le log NINA |

## 7. Journalisation

Chaque ON/OFF (panneau, Équipement, séquenceur) est journalisé dans le log NINA :
`Shelly Power: ON 'Alimentation' (192.168.1.52) → ON`.

## 8. Interface bilingue

L'interface est **anglaise par défaut** ; elle passe en **français** si la culture UI de NINA est `fr`.

---

# 🇬🇧 ENGLISH

## 1. Overview

The **Shelly Power** plugin lets you control up to **4 Shelly smart plugs** directly from NINA:

- turn equipment on / off (PC, camera, dew heater, router…);
- monitor **power consumption** (watts, volts, amps, kWh);
- automate in the **sequencer**: on/off, wait-for-state, timed toggle, run-while condition and a safety cut-off trigger.

Compatible plugs: **Shelly Gen1** (`/relay/0`), **Gen2** (Plus Plug S…), **Gen3** (Plug M…) — REST and RPC APIs with automatic fallback.

## 2. Installation

1. Close NINA.
2. Copy `NINA.ShellyPower.dll` into `%LOCALAPPDATA%\NINA\Plugins\3.0.0\ShellyPower\`.
3. Restart NINA — the log shows: `Successfully loaded plugin Shelly Power …`.

## 3. Configure the plugs

Open the **Shelly Power** panel (three entry points):

- **View → Shelly Power** menu (dockable, draggable panel);
- **Equipment → Switch** tab → **Setup** gear;
- **Options → Plugins → Shelly Power**.

For each plug:

1. **Name**: the function name (e.g. `Power`, `Camera`, `Dew heater`, `PC`).
2. **IP address**: the plug's static IP (e.g. `192.168.1.52`).
3. **Test**: checks the connection — `OK — PlusPlugS (Gen2) — plug off`.
4. Input is **auto-saved** in the NINA profile.
5. **Protected** (checked by default): confirmation before any manual power-off.
6. **Detect plugs**: automatic subnet scan; found Shelly plugs are listed — click a row to assign it to a slot.
7. The colored **ON (green)** / **OFF (red)** buttons control each plug directly.
8. State **auto-refreshes** every 15 s (or via « Refresh state »).

> 💡 Tip: reserve a **static IP** for each plug in your router's DHCP so the configuration stays valid.

## 4. The 6 sequencer entities

In the sequencer (category **Shelly Power**), with the same editor as built-in instructions (⏻ icon, action buttons preserved):

### Shelly Power On / Off — instructions
> `ON | Plug: [▼ Power]` · `OFF | Plug: [▼ Power]`

Turns the selected plug on or off. The instruction name shows the target: `Shelly Power On → Power`.

### Shelly Power Wait — wait for a state
> `Plug: [▼ Power] [▼ OFF/ON] Timeout (s): [60]`

Polls the plug every 2 s until it reaches the wanted state, at most for the configured timeout. On timeout: a **visible error notification** + instruction failure (the sequence stops or applies the configured « On Error » behavior). Use it after `On` to confirm the equipment is actually powered before imaging.

### Shelly Power Toggle — timed toggle
> `Plug: [▼ Dew heater] Duration (min): [10]`

1. reads the plug's current state;
2. **toggles** it (off → on, on → off);
3. **waits the duration** (progress shown, cancellable);
4. **restores the initial state** automatically.

Ideal for « run the dew heater 10 minutes during flats, then restore as before ».

### Shelly Power While — condition
> `Plug: [▼ Power] [▼ OFF/ON]`

The **instruction set runs while** the plug is in the selected state. A watchdog checks the state every 3 s: as soon as it changes (or the plug becomes unreachable), the set is **interrupted cleanly** with a warning notification.

### Shelly Power Unsafe — safety trigger
> `⚠ Unsafe → Plug: [▼ PC]`

Monitors the **safety monitor**: as soon as it goes **UNSAFE** (clouds, rain, roof open — end of safe-to-observe), the selected plug is **switched off automatically** (notification + log). You can drop instructions inside the trigger (e.g. park the mount) — they run after the cut-off. Notifications are limited to 1/minute during the unsafe period.

## 5. Protections

| Surface | Behavior |
|---|---|
| Panel OFF button | Confirmation (if « Protected ») + stronger warning if the name contains `pc`/`ordi`/`computer` (risk of cutting the machine running NINA) |
| Equipment toggle | Same |
| Sequencer | **Unconditional** (night-time automation preserved) — all actions logged in the NINA log |

## 6. Logging

Every ON/OFF (panel, Equipment, sequencer) is logged: `Shelly Power: ON 'Power' (192.168.1.52) → ON`.

## 7. Bilingual UI

The interface is **English by default**; it switches to **French** when NINA's UI culture is `fr`.

---

*Auteur / Author: Gérard Hurtaud · GitHub: [GHD-arch/ShellyPower](https://github.com/GHD-arch/ShellyPower) · Version 1.4.0 · 4 septembre 2026*