# TWC Genghis Campaign — Chat Commands Reference

This document lists all chat commands available in the TWC Genghis campaign. Commands are prefixed with `<` and entered in the in-game chat.

---

## General Commands (Main Mission / Genghis.cs)

| Command | Description |
|---------|-------------|
| `<help` | Show basic help: Time left, rearm/reload, AI gunner, recon photos, make landing ground |
| `<tl` | Show time left in mission |
| `<rr` | Rearm/reload at friendly airfield (see StatsMission for details) |
| `<ai` | Let AI take over your gunner position (Ju-87, Bf-110) |
| `<recon` | Take a reconnaissance photo |
| `<record` | Send reconnaissance photos to HQ (land first) |
| `<makelg` | Create a new Landing Ground at your current airfield |
| `<lghelp` | Help for Landing Ground system |
| `<stock` | Check aircraft stock levels |
| `<lost` | Show lost aircraft statistics |
| 
---

## Cover Aircraft Commands (Genghis-Class-CoverMission.cs)

### Core Cover Commands
| Command | Description |
|---------|-------------|
| `<cover` | Request cover bombers/ground attack/fighters to join you |
| `<cover [name]` | Launch cover squadron by aircraft name (e.g., `<cover Beau`) |
| `<cover [type#]` | Launch cover by type number from `<clist` |
| `<cover [type#] [count] [formation]` | Launch specific: e.g., `<cover 3 6 AS` = type 3, 6 aircraft, Astern formation |
| `<cover [type#] [count] [formation] heavy` | Add heavy bombs |
| `<cover [type#] [count] [formation] heavy x3` | Repeat command 3 times |
| `<clist` | List available cover fighters & type IDs |
| `<cpos` | Show position of your current cover aircraft |
| `<cland` | Release cover fighters to land (IMPORTANT!) |
| `<cland [group#] [group#]...` | Release specific groups by number |
| `<chelp` | Cover aircraft help (7 pages: `<chelp2` through `<chelp7`) |

### Cover Orders (apply to group numbers)
| Command | Description |
|---------|-------------|
| `<cnormal [group#]...` | Normal: bombers/sturmoviks in formation, cover engages threats |
| `<cstrict [group#]...` | Strict: rigid wingman, holds your speed/altitude, ignores `<cdist`, fires only in self-defense |
| `<cescort [group#]...` | Escort: CLoD ESCORT behavior, stays with & defends you, no ground bombing |
| `<cattack [group#]...` | Attack: air attack anything nearby + any bombing you ordered |
| `<creserve [group#]...` | Reserve: hold fire & stay in reserve, joined with you (no attacking/bombing) |
| `<cloiter [group#]...` | Loiter: circle in place (4-point NORMFLY pattern) |

### Cover Ground Attack Modes (Tab-4-4-4-4-7 menu or chat)
| Command | Description |
|---------|-------------|
| `<cdropnow` / `<cbomb` | **DROP WHEN I DROP**: Immediate release from all squadrons (except `<creserve`) |
| `<chelp6` | Details on ground attack modes: Knickebein point, nearest enemy to KB point, next bomb drop point, nearest enemy to bomb point, drop flare & target flare point, drop flare & target enemies near drop point |

### Cover Formation & Position
| Command | Description |
|---------|-------------|
| `<cdist [percent]` | Set cover formation distance: `<cdist 200` = 200% normal, `<cdist 50` = 50%, `<cdist 1000` = 10x |
| `<cfdist [meters]` | Set front/back offset: `<cfdist 10` = 10m ahead, `<cfdist -100` = 100m behind, range ±4000m |
| `<cplayer [slot]` | Change your slot in formation: `<cplayer 0` = center, `<cplayer -2` = 2 left, `<cplayer auto` = random (default) |

### Cover Help Pages
| Command | Description |
|---------|-------------|
| `<chelp` | Page 1/7: Overview, `<cover`, `<clist`, `<cpos`, `<cland`, orders, ground attack menu |
| `<chelp2` | Page 2/7: `<cover` syntax details, formations (VI, V3, AB, AS, RI, LE), `<cland`, `<cdist`, `<cfdist`, `<cplayer` |
| `<chelp3` | Page 3/7: Standing orders (`<cnormal`, `<cstrict`, `<cescort`, `<cattack`, `<creserve`, `<cloiter`) |
| `<chelp4` | Page 4/7: Ground attack target selection via Tab-4-4-4-4-7 |
| `<chelp5` | Page 5/7: DROP WHEN I DROP mode, `<cdropnow` / `<cbomb` |
| `<chelp6` | Page 6/7: Ground attack mode details (KB point, bomb point, flare point variants) |
| `<chelp7` | Page 7/7: POINT vs NEAREST ENEMY targeting for dive bombers/sturmoviks |

---

## Knickebein Navigation (Genghis-Class-Knickebein.cs)

| Command | Description |
|---------|-------------|
| `<kni [deg] [dist]` | Simple Knickebein: set target at bearing/distance from current position |
| `<kadd WP1 WP2...` | Advanced: add waypoints by sector (e.g., `<kadd AW23.2.8 AY26.1.2`) |
| `<kstart [#]` | Start Knickebein at waypoint # (default 1) |
| `<k` or `<k#` | Shortcut for `<kstart` |
| `<knext` | Skip to next waypoint |
| `<kprev` | Back to previous waypoint |
| `<koff` | Turn display off |
| `<kon` | Turn display back on |
| `<klist` | List all waypoints |
| `<kdel #` | Delete waypoint # |
| `<kclear` | Clear all waypoints |
| `<kinfo` | Info about current waypoint |
| `<khelp` | Knickebein help (6 pages: `<khelp2`–`<khelp6`) |

### Knickebein Help Pages
| Command | Description |
|---------|-------------|
| `<khelp` | Page 1/6: Basic operation, `<kadd`, Tab-4-4-4-4 menu, HQ dependency |
| `<khelp2` | Page 2/6: Advanced operation, `<kstart`, `<knext`, `<kprev`, `<koff`, `<kon`, `<klist`, `<kdel`, `<kclear`, `<kinfo` |
| `<khelp3` | Page 3/6: Command detail reference |
| `<khelp4` | Page 4/6: Vectoring from current position to any waypoint |
| `<khelp5` | Page 5/6: Historical beam operation (directional + crossing beams) |
| `<khelp6` | Page 6/6: Display symbols (+ - = for direction, \| . for distance), "riding the beam" |

---

## Aerial Intercept Radar (Genghis-Class-AerialInterceptRadar.cs)

| Command | Description |
|---------|-------------|
| `<aa` | Start Authentic/Chat-table radar (Tab-4-4-5) |
| `<ag` | Start Alternate/Chat-table radar |
| `<an` | Start Alternate/HUD radar |
| `<as` | Stop HUD radar display |
| `<ahelp` | AIRadar help (6 pages: `<ahelp2`–`<ahelp6`) |
| `<ah` | Short for `<ahelp` |

### AIRadar Help Pages
| Command | Description |
|---------|-------------|
| `<ahelp` | Page 1/6: 3 types — `<aa` (authentic/chat), `<ag` (alt/chat), `<an` (alt/HUD), `<as` (stop HUD) |
| `<ahelp2` | Page 2/6: Historical context, `<aa` & `<ag` display layouts |
| `<ahelp3` | Page 3/6: `<an` HUD display format (Direction - Distance - Altitude) |
| `<ahelp4` | Page 4/6: No definite distance scale, limited look-behind |
| `<ahelp5` | Page 5/6: `<aa` example display with 3 targets |
| `<ahelp6` | Page 6/6: `<ag` example display (numeric altitude coding: 1-4 lower, = co-alt, 5-9 higher) |

---

## Air-to-Surface Vessel Radar (Genghis-Class-AirToSurfaceVesselRadar.cs)

| Command | Description |
|---------|-------------|
| `<asv` | Start/stop ASV Radar (cycles: 1x → 2x → off) |
| `<asvhelp` | ASV Radar help (6 pages: `<asvhelp2`–`<asvhelp6`) |
| `<asvh` | Short for `<asvhelp` |

### ASV Radar Help Pages
| Command | Description |
|---------|-------------|
| `<asvhelp` | Page 1/6: Tab-4-4 menu item 5, cycles through AI radar magnifications then ASV |
| `<asvhelp2` | Page 2/6: Display format, # = player, dots = targets, size = return strength |
| `<asvhelp3` | Page 3/6: Coverage increases with altitude, warships = strong targets |
| `<asvhelp4` | Page 4/6: Example with 5 targets at various positions |
| `<asvhelp5` | Page 5/6: Range asymmetry (32km at 3/9 o'clock, 13km at 12 o'clock) |
| `<asvhelp6` | Page 6/6: Chat command alternative, Tab-4 preferred in busy servers |

---

## Asset Repair & Ferry Missions (Genghis-Class-ObjectiveRepairMissions.cs)

| Command | Description |
|---------|-------------|
| `<pickup` / `<pi` | Pick up repair staff/material at far-rear airport (heavy bomber, no bombs) |
| `<pdefense` / `<pd` | Pick up Defense Units at far-rear airport |
| `<ferry` / `<fe` | Ferry new aircraft from far-rear to front-line airport/LG |
| `<deliver` / `<de` | Deliver repair load (land at airport) or drop over objective (600m/2000ft AGL, straight, level, slow) |
| `<dadvice` / `<da` | Jumpmaster advice on parachute drop parameters |
| `<abandon` / `<ab` | Abandon current load (land first) |
| `<phelp` | Repair/Ferry help (5 pages: `<phelp2`–`<phelp5`) |

### Repair/Ferry Help Pages
| Command | Description |
|---------|-------------|
| `<phelp` | Page 1/5: Commands overview, `<pickup`/`<pdefense`/`<ferry`/`<deliver`/`<abandon`, `<dadvice` |
| `<phelp2` | Page 2/5: `<ferry` details — spawn far corner, bring cover, fly to front-line airport/LG, land, `<deliver` |
| `<phelp3` | Page 3/5: Must start from far-rear airports, cover must be within range at delivery, repair airports = tricky landing |
| `<phelp4` | Page 4/5: Parachute drop parameters (900m/3000ft AGL for DU, 600m/2000ft for repair), altitude critical |
| `<phelp5` | Page 5/5: Good piloting challenge, navigation aids limited, abbreviate commands (`<pi`, `<fe`, `<de`, `<ab`) |

---
---

## Stats & Career Commands (Genghis-Class-StatsMission.cs)

| Command | Description |
|---------|-------------|
| `<car` / `<ran` | Show career/rank stats (use Tab-4 menu) |
| `<ses` | Show session stats (use Tab-4 menu) |
| `<net` | Show net stats for all players (use Tab-4 menu) |
| `<obj` | Show team/objective stats (use Tab-4 menu) |
| `<ac` | Show aircraft available at current rank |
| `<nextac` / `<nex` | Show aircraft unlocked at next promotion |
| `<override` | Override death timeout penalty (if enabled) |
| `<rr` | Rearm/refuel at friendly airfield (100% fuel fighters, 30% bombers default) |
| `<rr [percent]` | Set fuel load: `<rr30`, `<rr50`, `<rr100` |
| `<rrf` | Fighter mode — NO bombs loaded (useful for 109E-1B, Ju-87) |
| `<rrb` | Bomber mode — bombs WILL be loaded if possible |
| `<rrhelp` | Rearm/refuel help (3 pages: `<rrhelp1`, `<rrhelp2`) |

### Rearm/Refuel Help
| Command | Description |
|---------|-------------|
| `<rrhelp` | Page 1/3: Works at any friendly airfield, refuel/re-arm/no repair, fuel % options |
| `<rrhelp1` | Page 2/3: `<rrf` forces fighter mode, `<rrb` forces bomber mode, combine: `<rrf50` |
| `<rrhelp2` | Page 3/3: Warning — reloaded bombs on many Red bombers won't explode (sim bug) |


---

## Tacview Recorder (Genghis-Class-TacviewImplMission.cs)

| Command | Description |
|---------|-------------|
| `<tachelp` | Info about Tacview recording of this mission |
| `<tmes [message]` | Save a message to Tacview recorder at current timestamp (experimental, commented) |

---

## Other Commands (Referenced in Genghis.cs)

| Command | Description |
|---------|-------------|
| `<car` | Career stats (StatsMission) |
| `<ses` | Session stats (StatsMission) |
| `<rank` | Rank display (StatsMission) |
| `<rr` | Rearm/reload (StatsMission) |
| `<ter` | Team stats (StatsMission) |
| `<air` | Aircraft available (StatsMission) |
| `<ac` | Aircraft list (StatsMission) |
| `<nextac` | Next aircraft unlock (StatsMission) |
| `<asv` | ASV Radar (AirToSurfaceVesselRadar) |
| `<net` | Net stats (StatsMission) |
| `<k` | Knickebein (Knickebein) |
| `<chelp` | Cover help (CoverMission) |
| `<cdist` | Cover distance (CoverMission) |
| `<cfdist` | Cover front/back distance (CoverMission) |
| `<cdro` / `<cdrop` | Cover drop (CoverMission) |
| `<cbom` | Cover bomb (CoverMission) |
| `<cover` | Cover system (CoverMission) |
| `<cpos` | Cover position (CoverMission) |
| `<cland` | Cover land (CoverMission) |
| `<ca` | Cover attack (CoverMission) |
| `<cr` | Cover reserve (CoverMission) |
| `<ce` | Cover escort (CoverMission) |
| `<cl` | Cover loiter (CoverMission) |
| `<cs` | Cover strict (CoverMission) |
| `<cn` | Cover normal (CoverMission) |
| `<de` | Deliver (ObjectiveRepairMissions) |
| `<da` | Dadvice (ObjectiveRepairMissions) |
| `<pi` | Pickup (ObjectiveRepairMissions) |
| `<fe` | Ferry (ObjectiveRepairMissions) |
| `<ab` | Abandon (ObjectiveRepairMissions) |
| `<cplayer` | Cover player slot (CoverMission) |
| `<phelp` | Repair/Ferry help (ObjectiveRepairMissions) |
| `<wa` / `<wind` | Weather/wind commands |
| `<ma` | Map commands |
| `<khelp` | Knickebein help (Knickebein) |
| `<ahelp` | AIRadar help (AerialInterceptRadar) |
| `<pd` | Pdefense (ObjectiveRepairMissions) |
| `<lg` | Landing Ground commands |
| `<tac` | Tacview commands |
| `<tmes` | Tacview message save |

---

## Key Bindings (Tab-4 Menus)

Many commands are also accessible via the **Tab-4** in-game menu system:

- **Tab-4-4** — Radar Menu: `<aa`, `<ag`, `<an`, `<asv`
- **Tab-4-4-4** — Cover Aircraft Menu: `<cover`, `<clist`, `<cpos`, `<cland`, orders, ground attack
- **Tab-4-4-4-4** — Cover Orders & Settings: `<cnormal`, `<cstrict`, `<cescort`, `<cattack`, `<creserve`, `<cloiter`, `<cdist`, `<cfdist`, `<cplayer`
- **Tab-4-4-4-4-6** — Ground Attack Modes
- **Tab-4-4-4-4-7** — Ground Attack Target Selection
- **Tab-4-4-4-4-8** — Toggle cover position display on/off
- **Tab-4-4-5** — Aerial Intercept Radar (`<aa`)
- **Tab-4-4-4-4** — Knickebein Navigation

---

## Notes

- **Case insensitive**: `<HELP` works same as `<help`
- **Abbreviations**: Many commands have short forms (e.g., `<pi` = `<pickup`, `<fe` = `<ferry`, `<de` = `<deliver`, `<ab` = `<abandon`)
- **HQ Dependencies**: Knickebein and Radar require their respective HQs to be operational
- **Cover Aircraft**: Must be released with `<cland` or they persist and consume slots
- **Formation Types**: VI=Vic, V3=Vic3, AB=Abreast, AS=Astern, RI=Right echelon, LE=Left echelon
