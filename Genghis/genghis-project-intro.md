# TWC Mission Server - Project Knowledge Transfer

## 🎯 Active Project: Genghis
`e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\`
This is the ONLY project you should work on. Other folders (Campaign21, Tobruk_Campaign, M001-003, Testing) are separate, older, or inactive campaigns - never edit them.

## 📁 Key Files to Know

### Main Mission Code (Edit These)
| File | Purpose |
|------|---------|
| Genghis.cs | Main mission class, entry points, chat command handlers |
| Genghis-Class-CoverMission.cs | Core bomber AI logic - BAM modes, drop logic, formation, targeting |
| Genghis-Class-CloDNotes.cs | Technical documentation of CLoD engine quirks/API behavior |
| Genghis-Class-CoverMission.cs.bak2 | Backup - don't edit |

### Build/Config (Don't Edit Unless Necessary)
| File | Purpose |
|------|---------|
| Genghis.csproj | Design-time only (IDE IntelliSense) - NOT the build truth |
| check-compile.ps1 | THE build script - runs C#5 compilation matching CLoD server |
| .clinerules | Short rules (this file) |
| .clinerules-long | Full rules, module map, pitfalls - READ THIS FIRST |

## ⚙️ Critical Build Rules

### ✅ ALWAYS use this to verify:
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1

- Run from TWC_Mission_Server\ root
- Exit 0 = OK, 1 = compile errors, 2 = couldn't run
- Baseline: 0 errors, ~64 warnings (suppressed by default; use -ShowWarnings)

### ❌ NEVER DO:
- dotnet build TWC_Mission_Server.csproj - globs ALL .cs files including duplicates -> thousands of errors
- Edit any file outside Genghis\ folder

## 💻 C#5 Language Constraint (MANDATORY)

The CLoD mission compiler is C#5-era. Modern C# syntax WILL compile locally but fail on server deploy.

- Pattern matching: x is AiAircraft y -> use var y = x as AiAircraft; if (y != null)
- String interpolation: $"text {var}" -> use "text " + var
- out var x -> use out int x (named)
- Null-conditional: x?.Y -> use if (x != null) x.Y
- nameof() -> use hardcoded strings
- => expression bodies -> use full { return ...; } blocks

Genghis.csproj has LangVersion 7.3 for IDE only - does NOT apply to check-compile.ps1 which pins /langversion:5

## 🏗️ Module Map (Genghis-Class-CoverMission.cs)

### BAM (Bomb Aim Mode) System
- Enum: BAM_BombAimMode (line ~459)
- Cycle: BAM_toggleBombAimMode() (line ~552)
- Enter/Leave: BAM_enterDropMode() / BAM_leaveDropMode() (line ~612/~700)
- Helpers: BAM_isBombPoint(), BAM_isMyPositionPoint(), BAM_isNearestEnemy(), BAM_isKnickebeinPoint()

### Cover Aircraft Management
- Active groups: coverAircraftAirGroupsActive
- Orders: coverAircraftAirGroupsOrders (drop/normal/attack/escort/reserve/loiter)
- State: coverAircraftAirGroupsDropIssued, DroppedThisPass, Released, BayWaitSince, etc.

### Main Loop
- keepAircraftOnTask_recurs() (line ~6056) - ~16s cycle, THE main AI driver
  - Formation/escort path
  - Bomber path: BomberUpdateWaypoints() (line ~8393) -> BomberPosWaypoint()

### Drop When I Drop (DWID)
- Watcher: coverDropReleasePass() (line ~2343) - detects leader bomb release
- Manual: dropBombsNow_player() (line ~2510) - <cdropnow chat command
- Per-group release: dropBombsNow_airGroup() (line ~2769)
- Pre-open bays: dropPreOpenBays_airGroup()

### PBP (Player Bomb Point)
- PBP_saveBombPoint() / PBP_getPlayerLastBombOrMyPositionPoint_point3d()
- Used by: flare modes, bomb explosion point, DWID-Nearest Enemy

## 🎮 Key Chat Commands (Genghis.cs)
- <cover / <cover # -> Checkout cover aircraft -> Tab-4-4-4-4
- <cpos -> Position display toggle -> Tab-4-4-4-4-8
- <cdrop / <cdrop # -> Enter DWID mode -> Tab-4-4-4-4-6
- <cdropnow -> Manual drop trigger
- <cnormal / <cattack / <cstrict / <creserve / <cescort / <cloiter -> Order changes
- <cfdist -> Formation front/back offset
- <cplayer -> Formation slot position

## 🔄 DWID Nearest Enemy Mode (Recently Added)
- Enum: Drop_When_I_Drop_Nearest_Enemy (Tab-4-4-4-4-6 cycle position 4 of 5)
- Behavior:
  1. Player selects mode -> bombers go to tight formation, hold bombs
  2. Player drops -> target point = player position at that instant
  3. Bombers find nearest enemy ground target to that point
  4. Dive bombers -> DIVE attack; Sturmoviks -> AUTO (drop+strafe); Heavy -> LEVEL from altitude
  5. Sustained by 16s keepAircraftOnTask_recurs cycle (no 25s hold-off)
- Key fix: Added to BAM_isMyPositionPoint() so target point retrieval works

## 🧪 Testing Protocol
1. Make changes in Genghis\
2. Run check-compile.ps1 - must return exit 0
3. Deploy .mis + compiled DLL to CLoD server
4. Test with: Wellingtons (heavy), Ju-87 (dive), IL-2 (Sturmovik)
5. Verify: <cpos display, drop sync, target selection, formation holding

## ⚠️ Known Pitfalls (from .clinerules-long)
- Dictionary leaks: forgetAirGroup() must clean ALL per-airgroup dictionaries (18+ of them)
- Timer thread vs mission thread: GamePlay objects need Timeout(0.05, ...) marshaling
- One-issue-per-pass latch: coverAircraftAirGroupsDroppedThisPass prevents re-issue blackout
- 25s hold-off: coverAircraftAirGroupsDropIssued blocks keepAircraftOnTask_recurs from overwriting release plan
- Torpedo exclusion: airGroup.hasTorpedos() check in multiple places (torps dont work off GATTACK_POINT at altitude)
- Bomb bay timing: ~6s door open delay - pre-open logic exists but OFF by default (cdDropPreOpenBays)

## 📍 Where to Start for Common Tasks
- Add new bomb mode: BAM_BombAimMode enum -> BAM_toggleBombAimMode() cycle -> BAM_is*() helpers -> keepAircraftOnTask_recurs gate
- Fix formation: EscortPosWaypoint() / CurrentPosWaypoint() / calcOffset_m()
- Fix drop timing: coverDropReleasePass() / dropBombsNow_airGroup() / BomberUpdateWaypoints()
- New chat command: Genghis.cs OnPlayerChat() -> find <c... handlers
- Debug bomber AI: Enable mainmission.ON_TESTSERVER logs in keepAircraftOnTask_recurs

## 📚 Reference Docs
- Genghis-Class-CloDNotes.cs - Read sections 1, 2, 4 for CLoD engine behavior
- .clinerules-long - Full rules, file map, common bugs
- Genghis.briefing - Player-facing feature documentation

## TL;DR
Work only in Genghis\, use check-compile.ps1, write C#5 code, understand keepAircraftOnTask_recurs is the heartbeat. When in doubt, read Genghis-Class-CloDNotes.cs.
