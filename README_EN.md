# NINA.ShellyPower — NINA 3.x plugin for Shelly smart plugs

**Version 1.2.0** · Author: **Gérard Hurtaud** · License: MIT
· **GitHub**: https://github.com/GHD-arch/ShellyPower
· NINA 3.0+ (tested 3.2.0.9001)

Plugin for **NINA** (Nighttime Imaging 'N' Astronomy) to control **Shelly smart plugs**
(Gen1 REST API and Gen2/Gen3 RPC) — up to **4 named plugs**, each with its own IP address.

---

## Features

1. **Switch equipment** — the 4 plugs appear as a controllable switch hub in NINA's
   Equipment tab (ON/OFF, state read by automatic polling).
2. **Power meters** — each plug exposes an « info only » meter (power in watts, voltage,
   current, cumulative energy in kWh) in the Read-only section of the Equipment tab.
3. **Sequencer instructions** — `Shelly Power On` / `Shelly Power Off` with the target
   plug identified in the name (`Shelly Power On → Alimentation`).
4. **Configuration panel** — enter the 4 names + IP addresses, connection test, direct
   ON/OFF control with colors (green on / red off), live state.
5. **Automatic network detection** — « Detect plugs » button scans the local subnet and
   lists the Shelly plugs it finds.
6. **Accidental power-off protection** — confirmation is requested before any manual
   power-off (Equipment toggle, panel OFF button). The sequencer is **not** affected
   (night-time automation preserved).
7. **Bilingual UI** — English by default (en-US/en-GB), French if NINA's UI culture is
   `fr` (all labels, messages, confirmations and states).
8. **Proper icon** — « power » symbol (⏻) as vector geometry in the sequencer, dock
   and NINA menus.

## Usage surfaces

| Surface | Content | Persistence |
|---|---|---|
| **Equipment → Switch** (connect via lightning bolt) | 4 plugs (ON/OFF toggle) + 4 meters (W) | Whole session |
| **Dockable panel** (View menu) | Config + colored ON/OFF + state + test | Permanent, draggable |
| **Setup gear** (Equipment → Switch) | Same window as the panel | Modal |
| **Options → Plugins → Shelly Power** | Same panel (via `Core`) | Persistent |
| **Sequencer** | On/Off instructions with plug name | Saved with the sequence |
| **Mini-sequencer** (Imaging tab) | Plug selection ComboBox | — |

## Prerequisites

- **NINA 3.x stable** (tested 3.2, .NET 8.0)
- **.NET 8.0 SDK** to build (`dotnet build -c Release`)
- **Shelly Gen1** plugs (REST API `/relay/0`) or **Gen2/Gen3** (RPC `/rpc/Switch.*`)

## Performance — non-blocking polling

- HTTP timeout reduced to **2 s** per request.
- **Parallel prefetch**: at the start of each NINA polling cycle (reading hub.Switches),
  all 4 plugs are queried **simultaneously** in the background to warm the cache — the
  successive Poll() calls (switch + meter of each plug) read the cache instead of blocking
  sequentially.
- Panel refresh in **parallel** (Task.WhenAll): worst case ~2 s instead of ~8 s.

## Build

```powershell
cd ShellyPower
dotnet build -c Release
```

Output: `ShellyPower\bin\Release\net8.0-windows\NINA.ShellyPower.dll`

## Installation

1. **Close NINA.**
2. Copy `NINA.ShellyPower.dll` into `%LOCALAPPDATA%\NINA\Plugins\3.0.0\ShellyPower\`.
3. **Restart NINA.**

## Configuration

1. Open the **Shelly Power** panel (Setup gear in Equipment → Switch).
2. For each plug: **Name** (e.g. `Power`) + **IP address** (e.g. `192.168.1.52`).
3. Click **Test** → `OK — PlusPlugS (Gen2) — plug off`.
4. Input is **auto-saved** (NINA profile).
5. Optional: check **Protected** (default) to confirm before a manual power-off.
6. Optional: click **Detect plugs** to scan the network automatically.

## Shelly compatibility

| Generation | API | State | Consumption |
|---|---|---|---|
| Gen1 | `/relay/0`, `/meter/0` | ✅ | ✅ |
| Gen2 (Plus Plug S, etc.) | `/rpc/Switch.*` (+ Gen1 compat) | ✅ | ✅ |
| Gen3 (Plug M, etc.) | `/rpc/Switch.*` | ✅ | ✅ |

## License

MIT.

---

*Author: Gérard Hurtaud · GitHub: [GHD-arch/ShellyPower](https://github.com/GHD-arch/ShellyPower) · Version 1.2.0 · August 28, 2026*