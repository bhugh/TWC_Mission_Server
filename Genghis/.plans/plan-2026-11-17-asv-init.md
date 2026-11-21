# ASV Radar Initialization Fix — Detailed Implementation Plan

**Mode: PLAN** — No files will be edited here. This document is written for an ACT-mode worker to follow step by step.

---

## 1. Bug Summary

ASV radar shows no ships on the first mission run. After issuing `<lactorsall` and re-starting ASV radar, ships appear correctly.

**Root cause:** The ASV radar's `AllShipsNear` method calls `covermission.getAllGroundActorsNear_clean()`, which returns `null` on its first call (while `renewAllStaticActors_recurs` runs asynchronously to populate `allStaticActors`). The code then does `new List<AiActor>(null)`, which throws `ArgumentNullException`. This exception is caught by the try/catch added in the previous fix, causing `calculateGrid` to fail silently, `displayGrid` never runs, and the radar shows nothing. After `<lactorsall` forces the async load to complete, subsequent ASV radar restarts work.

---

## 2. File to Edit (absolute path)

```
e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-AirToSurfaceVesselRadar.cs
```

Only this one file needs changes.

---

## 3. C#5 Language Constraints (MUST FOLLOW)

The mission compiler uses `/langversion:5`. Do **not** use any of the following:
- String interpolation: `$"..."` → use string concatenation `"..." + var`
- Null-conditional operator: `?.`
- `out var`
- Pattern matching: `x is AiGroundActor y`
- `nameof(...)`

**Allowed/required idioms:**
- `as` + null check: `AiGroundActor ga = actor as AiGroundActor; if (ga != null) { ... }`
- String concatenation: `"prefix " + var.ToString()`
- `Console.WriteLine("...{0}...", var)`
- Named `out` variables

---

## 4. Step-by-Step Edit

### Edit F — Handle null return from `getAllGroundActorsNear_clean` in `AllShipsNear`

**Goal:** Guard against the null return when `allStaticActors` hasn't been loaded yet, returning an empty list instead of throwing `ArgumentNullException`.

**Location:** `ASVRadarMission.AllShipsNear` method (around line 1553-1559)

**Exact old_text:**
```csharp
    public List<AiActor> AllShipsNear(Point3d pos, double radius_m)
    {

        if ( mainmission.covermission == null) return new List<AiActor>();
        return new List<AiActor>(mainmission.covermission.getAllGroundActorsNear_clean(pos, radius_m));
    }
```

**Exact new_text:**
```csharp
    public List<AiActor> AllShipsNear(Point3d pos, double radius_m)
    {

        if ( mainmission.covermission == null) return new List<AiActor>();
        var result = mainmission.covermission.getAllGroundActorsNear_clean(pos, radius_m);
        if (result == null) return new List<AiActor>();
        return new List<AiActor>(result);
    }
```

**Verification after edit:** The method should now read (with proper indentation):
```csharp
    public List<AiActor> AllShipsNear(Point3d pos, double radius_m)
    {

        if ( mainmission.covermission == null) return new List<AiActor>();
        var result = mainmission.covermission.getAllGroundActorsNear_clean(pos, radius_m);
        if (result == null) return new List<AiActor>();
        return new List<AiActor>(result);
    }
```

---

## 5. Compilation Verification (required after edit)

From the directory `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\`, run:

```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```

**Expected result:** Exit code `0`, 0 errors. (The 2 pre-existing CS0122 errors in `Genghis-Class-TacviewRecorder.cs` are unrelated and skipped by the script; ~64 warnings are normal.)

**Important:** Do **not** run `dotnet build` on the repo `.csproj` — it will glob every `.cs` in the tree including duplicate campaign copies and produce thousands of duplicate-type errors. Use `check-compile.ps1` only.

---

## 6. In-Game Sanity Pass (after compile succeeds)

1. Join a fresh mission (server restart) as a player in an aircraft allowed for ASV radar (2+ seat aircraft).
2. Open Tab-4-4 menu, item 5 (Aerial Intercept Radar).
3. Press `5` repeatedly to cycle to ASV radar (1X then 2X).
4. **Wait 15-30 seconds** — the first `display_recurs` cycle may find empty list while `allStaticActors` loads, but subsequent cycles should pick up ships once `renewAllStaticActors_recurs` completes.
5. Confirm you see:
   - Log message `Air to Surface Vessel (Authentic) command received.`
   - Log message `Aerial Radar: New Magnification: 2` (on the 2X toggle)
   - Log message `AIRadar: Display magnification set to 2`
   - **Ship pips/dots appearing on screen** — previously missing on first run.
6. No `<lactorsall` command should be needed.

---

## 7. Checklist for the ACT Worker

- [ ] Edit F applied (line ~1553-1559, `AllShipsNear` null guard)
- [ ] `check-compile.ps1` returns exit code 0
- [ ] No C#6+ syntax introduced (no `$"..."`, `?.`, `out var`, `is T y`, `nameof`)
- [ ] In-game sanity pass: ASV grid shows ships on first mission run without `<lactorsall`

---

## 8. Notes / Assumptions

- Only `Genghis-Class-AirToSurfaceVesselRadar.cs` is modified.
- The `display_recurs` loop runs every ~7 seconds (configurable via `delay_s`), so ships will appear automatically once the async `renewAllStaticActors_recurs` finishes populating `allStaticActors`.
- This fix works with the try/catch logging added in the previous fix (Edit D/E) — if any other exception occurs, it will be logged instead of silently swallowed.

---

**This plan is complete. When toggled to Act mode, apply Edit F, then run the compile check and report the result.**