# CONTINUE.md - TWC Mission Server Codebase Guide

Welcome to the **TWC Mission Server** project guide! This document provides essential information to help developers and mission designers understand, extend, and maintain the codebase and mission scripts for the TWC IL-2 Sturmovik: Cliffs of Dover (DESERT WINGS / Tobruk & Blitz) multi-player server.

---

## 1. Project Overview

- **Purpose**: The TWC Mission Server software powers the dynamic multiplayer server environment for *IL-2 Sturmovik: Cliffs of Dover* (Team Fusion version 4.312+ / Desert Wings Tobruk). It features automated dynamic missions, real-time online/in-game player statistics and rank tracking, dynamic supply and aircraft inventory limits, AI cover & escort control systems, Knickebein navigation systems, radar interlock, Tacview logging, and automated ground vehicle generation.
- **Key Technologies**:
  - **C# / .NET Framework** (`System.Core`, `System.Threading.Tasks`)
  - **Maddox Game Engine APIs** (`maddox.GP`, `maddox.game`, `maddox.game.world`, `maddox.game.play`, `maddox.game.page`)
  - **TWC Inter-module Communicator** (`TWCComms.Communicator` / `CloDMissionCommunicator.dll`)
  - **IL-2 Mission Files (`.mis`)**: Standard IL-2 map and spawn point definition files
- **Architecture**: Modular event-driven dynamic mission system. A central Main Mission script coordinates specialized sub-missions (`AMission` sub-classes) like Cover, Supply, Stats, Radar, Knickebein, and Tacview through a thread-safe singleton communicator interface (`TWCComms`).

---

## 2. Getting Started

### Prerequisites
1. **IL-2 Sturmovik: Cliffs of Dover Blitz / Desert Wings Tobruk** with Team Fusion updates installed.
2. **C# Compiler / IDE**: Visual Studio or Visual Studio Code with C# extension (.NET Framework 4.0+ target environment compatible with IL-2 CloD engine references).
3. **Maddox Engine Assemblies**: Access to engine binaries located in `parts/core/`:
   - `Strategy.dll`
   - `gamePlay.dll`
   - `gamePages.dll`
   - `CloDMissionCommunicator.dll`

### Installation & Setup
1. Clone or extract this repository into your dedicated server's mission directory or local testing environment.
2. Ensure the required `.dll` references are located under `parts/core/` in your game installation path.
3. Place mission scripts (`.cs`) and mission descriptor files (`.mis`) in the target server directory (e.g., `Tobruk_Campaign/` or `M001/`).

### Running & Testing
- Load mission files directly within the IL-2 Dedicated Server or Full Game interface under Multiplayer / Server options.
- Diagnostic & chat logging can be monitored in-game via Chat commands (e.g. `<help`, `<clist`, `<cpos`, `<cover`, `<cland`, `<stock`).

---

## 3. Project Structure

```
├── TWC_MISSION_SERVER_README.txt         # Server overview and licensing
├── CloDMissionCommunicator/               # Inter-module communication assembly source
│   └── CloDMissionCommunicator.cs        # Communicator singleton implementation
├── Tobruk_Campaign/                      # Tobruk / Desert Wings Campaign scripts & .mis files
│   ├── Fresh Input File/                 # Core campaign scripts and objectives definitions
│   └── Flak areas/                       # Flak positioning mission files
├── Genghis/                              # Core campaign framework script modules
│   ├── Genghis.cs                        # Genghis main mission controller
│   ├── Genghis-Class-CoverMission.cs     # Escort/cover management
│   ├── Genghis-Class-SupplyMission.cs    # Aircraft inventory & supply handling
│   ├── Genghis-Class-StatsMission.cs     # Player stats & rank recording
│   ├── Genghis-Class-TacviewRecorder.cs # Flight logging
│   └── TacView-IL2CLOD-folder/           # Tacview output templates
├── M001/ - M003/                         # Server mission variants (Main, Radar, Vehicle Generation)
├── Campaign21/                           # Campaign 21 mission files and sub-missions
└── Testing/                              # Various scripts and ideas we were testing (e.g. supply, stats)
```

### Key Files
- `CloDMissionCommunicator/CloDMissionCommunicator.cs`: Central hub (`TWCComms.Communicator`) connecting `Main`, `Cover`, `Supply`, `Stats`, `Knickebein`, and `Radar` module instances.
- `Tobruk_Campaign/Fresh Input File/Tobruk_Campaign-Class-CoverMission.cs`: Implementation of AI escort fighter and bomber group management, formation spacing (`<cdist`), spawning, and landing logic.
- `Genghis/Genghis.cs`: Primary battle loop, event handling (`OnBattleStarted`, `OnPlaceEnter`, `OnActorDestroyed`), and objective triggers.

---

## 4. Development Workflow

### Coding Standards
- **Preprocessor Directives**: Core mission scripts require `#define DEBUG` and `#define TRACE` at the top, along with engine reference definitions (`//$reference parts/core/gamePlay.dll`).
- **Safety & Exception Handling**: Always wrap sub-mission event hooks in `try-catch` blocks and output errors to `Console.WriteLine` or server logs (`GamePlay.gpLogServer`).
- **Async & Thread Safety**: High-overhead searches (such as iterating ground static actors or objective distances) must execute asynchronously using `Task.Run()` or engine `Timeout()` delays to prevent server frame warping.

### Contribution Guidelines
- Contributions and mission snippet reuse are welcomed under the condition that credit is given to **TWC - The Wrecking Crew** (`http://twcclan.com/`).
- Test new sub-missions in isolated test missions (under `Testing/`) before integrating into primary campaign loops (`Tobruk_Campaign/` or `Genghis/`).

---

## 5. Key Concepts & Architecture

### Communicator Pattern (`TWCComms`)
The sub-missions do not tightly couple to each other directly. Instead, they register themselves with `TWCComms.Communicator.Instance` during startup:
```csharp
TWCMainMission = TWCComms.Communicator.Instance.Main;
TWCComms.Communicator.Instance.Cover = (ICoverMission)this;
```
This design allows components like `StatsMission` or `SupplyMission` to check checkout limits or record events seamlessly without hard dependencies.

### Dynamic Cover & Escort Subsystem
- Bomber/fighter-bomber pilots can request AI cover flights using in-game Chat commands (`<cover`) or the `Tab-4` menu.
- Tracks player command squadron limits according to rank and current online friendly player counts (`acAvailableToPlayer_num`).
- Dynamically calculates offset formations (`left_right`, `up_down`) to prevent AI collisions using rolling averages.

---

## 6. Common Tasks

### Adding or Modifying Escort Aircraft
1. Open the relevant `CoverMission.cs` file (e.g., in `Tobruk_Campaign/` or `Genghis/`).
2. Update the `CoverAircraftInitiallyAvailable` dictionary under `ArmiesE.Red` or `ArmiesE.Blue` with the internal plane name and boolean flag (`true`/`false`).
3. Ensure minimum stock thresholds (`minimumAircraftRequiredForCoverDuty`) are satisfied in `setCoverAircraftCurrentlyAvailable()`.

### Creating a New Objective Module
1. Implement the `IMissionObjective` interface or extend `AMission`.
2. Register the objective within `SMissionObjectivesList`.
3. Hook into `OnActorDestroyed` and `OnAircraftLanded` to calculate victory points and broadcast status messages to players.

---

## 7. Troubleshooting

- **Server Warping / Rubberbanding**:
  - Verify static actor searches are offloaded to background threads (`Task.Run`).
  - Ensure `WARP_CHECK` logs are monitored when debugging performance bottlenecks.
- **AI Aircraft Crashing on Spawn**:
  - Check altitude offsets in `Stb_LoadSubAircraft` (`loc.z` should be at least 350m for ground spawns or match airspawn coordinates).
  - Verify plane variant compatibility in `CoverAircraftInitiallyAvailable` (certain variants like `Bf-110C-6` may require exclusion due to engine AI bug handling).
- **Sub-mission References Missing**:
  - Ensure all required `.dll` files in `parts/core/` are accessible and correctly referenced in script headers.

---

## 8. References

- **TWC Official Website**: [http://twcclan.com/](http://twcclan.com/)
- **IL-2 Sturmovik Official Forums**: [https://forum.il2sturmovik.com/](https://forum.il2sturmovik.com/)
- **Air Combat Group / Team Fusion Wiki**: Community resources for Cliffs of Dover mission scripting and Maddox engine API documentation.
