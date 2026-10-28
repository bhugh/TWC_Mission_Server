# Plan: Fix Stationary Markers Implementation (Genghis Campaign)

**Date:** 2026-10-27  
**Working Directory:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\`  
**Compile Gate:** `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1` (run from `TWC_Mission_Server\`)  
**C# Version:** C#5 only — NO string interpolation (`$"..."`), NO `?.`, NO `nameof`, NO `out var`, NO pattern matching (`x is T y`). Use `String.Format`/concatenation, `as` + null check, named `out` variables.

---

## Overview of Problems to Fix

| # | Problem | File | Severity |
|---|---------|------|----------|
| 1 | `ProcessSectionFileForMarkers` crashes: `Null Path` because `sectionfiles` directory doesn't exist | `Genghis.cs` | High |
| 2 | `ExtractShortTypeName` cannot parse type names with spaces/non-breaking spaces (`Stationary Car AEC_Regent_II`) | `Genghis-Class-StationaryMarkers.cs` | High |
| 2b | `AddMarker` uses `Contains()` instead of exact equality, causing false-positive matches | `Genghis-Class-StationaryMarkers.cs` | Medium |
| 3 | `renewAllStaticActors_recurs` constructs `HashSet<AiActor>(null)` — throws `ArgumentNullException`; also runs engine calls on background thread | `Genghis-Class-CoverMission.cs` | High |
| 4 | `BurnBabyBurn` hardcoded `Campaign21` path instead of `Genghis` | `Genghis-initsubmission-BurnBabyBurn.cs` | Medium |
| 5 | `OnBattleStarted` marker scan/destroy (Phase 3 of original plan) never implemented — physical stationaries never virtualized/destroyed from main mission | `Genghis.cs` | High |

---

## Step-by-Step Instructions

---

### Step 0: Baseline Compile Check

**Goal:** Verify the current code compiles before making changes.

**Command:**
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
---

### Step 1: Fix `renewAllStaticActors_recurs` — Null Guard + Thread Marshaling  **[COMPLETED - 2026-10-27]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-CoverMission.cs`  
**Lines to modify:** ~283–318

**Current code (lines 283–318):**
```csharp
    private void renewAllStaticActors_recurs(bool onetime = false)
    {
        if (!onetime) Timeout(3 * 60.3624, () => renewAllStaticActors_recurs());
        Task.Run(() =>
        {
            try
            {
                if (mainmission.panic() && ran.Next(2) == 0) return;
                Console.WriteLine("renewAllStaticActors running . . . ");
                HashSet<AiActor> asa = new HashSet<AiActor>(CoverCalcs.gpGetAllGroundActors(this, stb_lastMissionLoaded));
                HashSet<AiActor> gga = new HashSet<AiActor>(CoverCalcs.gpGetGroundActors(this, army: 0));
                if (asa == null) asa = gga;
                if (gga!= null) asa.UnionWith(gga);
                if (asa == null & gga == null) return;
                lock (allStaticActors_lock)
                {
                    allStaticActors = asa.ToArray();
                }
                Console.WriteLine("renewAllStaticActors running: {0:n0} found (asa: {1}, gga: {2})", allStaticActors.Length, asa.Count, gga.Count);
                SMissionObjectivesList = TWCMainMission.SMissionObjectivesList();
                Console.WriteLine("renewAllStaticActors complete . . . ");
            }
            catch (Exception ex) { Console.WriteLine("cover: renewAllStaticActors ERROR: " + ex.ToString()); }
        });
    }
```

**Replacement code:**
```csharp
    private void renewAllStaticActors_recurs(bool onetime = false)
    {
        if (!onetime) Timeout(3 * 60.3624, () => renewAllStaticActors_recurs());
        Task.Run(() =>
        {
            try
            {
                if (mainmission.panic() && ran.Next(2) == 0) return;
                Console.WriteLine("renewAllStaticActors running . . . ");
                Timeout(0.05, () =>
                {
                    try
                    {
                        List<AiActor> listAsa = CoverCalcs.gpGetAllGroundActors(this, stb_lastMissionLoaded);
                        List<AiActor> listGga = CoverCalcs.gpGetGroundActors(this, army: 0);
                        HashSet<AiActor> asa = null;
                        HashSet<AiActor> gga = null;
                        if (listAsa != null) asa = new HashSet<AiActor>(listAsa);
                        if (listGga != null) gga = new HashSet<AiActor>(listGga);
                        if (asa == null) asa = gga;
                        if (gga != null && asa != null) asa.UnionWith(gga);
                        if (asa == null && gga == null) return;
                        lock (allStaticActors_lock)
                        {
                            allStaticActors = asa.ToArray();
                        }
                        Console.WriteLine("renewAllStaticActors running: {0:n0} found (asa: {1}, gga: {2})", allStaticActors.Length, (asa != null ? asa.Count : 0), (gga != null ? gga.Count : 0));
                        SMissionObjectivesList = TWCMainMission.SMissionObjectivesList();
                        Console.WriteLine("renewAllStaticActors complete . . . ");
                    }
                    catch (Exception ex2) { Console.WriteLine("cover: renewAllStaticActors marshal ERROR: " + ex2.ToString()); }
                });
            }
            catch (Exception ex) { Console.WriteLine("cover: renewAllStaticActors ERROR: " + ex.ToString()); }
        });
    }
```

**Notes:** Null checks before `HashSet` constructor; `Timeout(0.05, ...)` marshals to main thread.
**Verification:** Run compile check. Expect 0 errors.
```
**Run from:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\`

**Expected result:** Exit code 0 (0 errors, ~64 warnings).  
**If non-zero:** Stop and report errors. Do not proceed.
---

### Step 2: Fix `ExtractShortTypeName` — Handle Spaces and Non-Breaking Spaces  **[COMPLETED - 2026-10-27]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-StationaryMarkers.cs`  
**Lines to modify:** ~169–177

**Current code:**
```csharp
    public static string ExtractShortTypeName(string fullTypeName)
    {
        int lastDot = fullTypeName.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < fullTypeName.Length - 1)
        {
            return fullTypeName.Substring(lastDot + 1);
        }
        return fullTypeName;
    }
```

**Replacement code:**
```csharp
    public static string ExtractShortTypeName(string fullTypeName)
    {
        if (fullTypeName == null) return "";
        string s = fullTypeName.Trim();
        char[] separators = new char[] { '.', ' ', '\t', '\u00A0' };
        int lastSep = -1;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            bool isSep = false;
            for (int si = 0; si < separators.Length; si++)
            {
                if (s[i] == separators[si]) { isSep = true; break; }
            }
            if (isSep) { lastSep = i; break; }
        }
        if (lastSep >= 0 && lastSep < s.Length - 1)
        {
            return s.Substring(lastSep + 1);
        }
        return s;
    }
```

**Notes:** C#5 compatible; handles `.`, space, tab, non-breaking space (U+00A0).
**Verification:** Run compile check. Expect 0 errors.

---

### Step 3: Fix `AddMarker` — Use Exact Equality Instead of `Contains`  **[COMPLETED - 2026-10-27]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-StationaryMarkers.cs`  
**Lines to modify:** ~61–80

**Current code:**
```csharp
    public static void AddMarker(string type, double x, double y, string sourceFile)
    {
        double radius = -1;
        foreach (KeyValuePair<string, double> kvp in _markerRadii) {
            if (! kvp.Key.ToLower().Contains(type.ToLower())) continue;
            radius = kvp.Value;
            break;
        }
        
        if (radius == -1)
        {
            Console.WriteLine("[StationaryMarkerManager] Unknown marker type: " + type);
            return;
        }

        StationaryMarker marker = new StationaryMarker(x, y, type, radius, sourceFile);
        _markers.AddOrUpdate(type, 
            new List<StationaryMarker> { marker },
            (key, existing) => { existing.Add(marker); return existing; });
    }
```

**Replacement code:**
```csharp
    public static void AddMarker(string type, double x, double y, string sourceFile)
    {
        double radius = -1;
        string matchedKey = null;
        foreach (KeyValuePair<string, double> kvp in _markerRadii) {
            if (kvp.Key.Equals(type, StringComparison.OrdinalIgnoreCase)) {
                radius = kvp.Value;
                matchedKey = kvp.Key;
                break;
            }
        }
        
        if (radius == -1)
        {
            Console.WriteLine("[StationaryMarkerManager] Unknown marker type: " + type);
            return;
        }

        StationaryMarker marker = new StationaryMarker(x, y, type, radius, sourceFile);
        List<StationaryMarker> list = null;
        if (!_markers.TryGetValue(matchedKey, out list)) {
            list = new List<StationaryMarker>();
---

### Step 4: Fix `ProcessSectionFileForMarkers` — Create Directory + Null Checks  **[COMPLETED - 2026-10-27]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Lines to modify:** ~9154–9222

**Current code (abridged):**
```csharp
    private ISectionFile ProcessSectionFileForMarkers(ISectionFile f, string sourceFile)
    {
        try
        {
            string tempPath = CLOD_PATH + FILE_PATH + "/sectionfiles" + "/markers_temp_file_"+ random.Next(10,999).ToString();
            f.save(tempPath);
            // ... rest of parsing logic ...
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("[ProcessSectionFileForMarkers] Error: " + ex.Message);
            return f;
        }
    }
```

**Replacement code:**
```csharp
    private ISectionFile ProcessSectionFileForMarkers(ISectionFile f, string sourceFile)
    {
        try
        {
            if (f == null) return null;

            string sectionDir = CLOD_PATH + FILE_PATH + "/sectionfiles";
            try
            {
                System.IO.Directory.CreateDirectory(sectionDir);
            }
            catch (Exception dirEx)
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] Cannot create sectionfiles dir: " + dirEx.Message);
                return f;
            }

            string tempPath = sectionDir + "/markers_temp_file_" + random.Next(10, 999).ToString();
            f.save(tempPath);
            // ... rest of parsing logic unchanged ...
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("[ProcessSectionFileForMarkers] Error: " + ex.Message);
            return f;
        }
    }
```

**Notes:** Added `if (f == null) return null;` guard; `Directory.CreateDirectory(sectionDir)` before `f.save()`.
**Verification:** Run compile check. Expect 0 errors.

---

### Step 5: Fix BurnBabyBurn Hardcoded Path  **[COMPLETED - 2026-10-27]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-initsubmission-BurnBabyBurn.cs`  
**Line to modify:** 26

**Current code (line 26):**
```csharp
    private static string PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"/1C SoftClub/il-2 sturmovik cliffs of dover/missions/Multi/Fatal/Campaign21/Fresh Input File/";
```
---

### Step 6: Implement `OnBattleStarted` Marker Scan/Destroy (Phase 3)  **[COMPLETED - 2026-10-27]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Location:** Inside `OnBattleStarted` method (near line 8775)

**Add this code near the end of `OnBattleStarted`:**
```csharp
        // NEW: Virtualize physical stationary markers from the main mission
        Timeout(10.0, () =>
        {
            try
            {
                Console.WriteLine("[StationaryMarkerManager] Scanning for physical markers to virtualize...");
                GroundStationary[] allStatics = GamePlay.gpGroundStationarys(250000, 252000, 500000);
                if (allStatics != null)
                {
                    int removed = 0;
                    foreach (GroundStationary sta in allStatics)
                    {
                        if (sta == null) continue;
                        string title = sta.Title;
                        if (title == null) continue;
                        if (title.Contains("JerryCan_GER1_1") || title.Contains("JerryCan_GER1_2") ||
                            title.Contains("JerryCan_GER1_3") || title.Contains("JerryCan_GER1_5") ||
                            title.Contains("AEC_Regent_II") || title.Contains("MG_TA"))
                        {
                            string shortType = StationaryMarkerManager.ExtractShortTypeName(title);
                            StationaryMarkerManager.AddMarker(shortType, sta.Pos.x, sta.Pos.y, "MainMission.mis");
                            sta.destroy();
                            removed++;
                        }
                    }
                    Console.WriteLine("[StationaryMarkerManager] Virtualized " + removed + " physical markers from main mission");
                    StationaryMarkerManager.DebugPrintAll();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[StationaryMarkerManager] Error during main mission scan: " + ex.Message);
            }
        });
```

**Notes:** `Timeout(10.0, ...)` runs after mission loads; catches exceptions; calls `sta.destroy()`; logs results.
**Verification:** Run compile check. Expect 0 errors.

---

### Step 7: Final Compile Check  **[COMPLETED - 2026-10-27]**

**Command:**
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```
**Run from:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\`

**Expected result:** Exit code 0.  
**If errors:** Fix each error, re-run, until exit code 0.

---

### Step 8: In-Game Validation (Test Server)  **[PENDING - Requires test server deployment]**

**Watch for in server log:**
1. No `[ProcessSectionFileForMarkers] Error: Value cannot be null.`
2. No `Unknown marker type: Stationary Car AEC_Regent_II`
3. `[StationaryMarkerManager] Virtualized X physical markers from main mission` (X > 0)
4. `[StationaryMarkerManager] Total markers: N` (hundreds, not 68)
5. No `cover: renewAllStaticActors ERROR: ... ArgumentNullException`
6. No `BurnBabyBurn ERROR reading bannedPlayers.txt`

**If any appear:** Stop and report.

---

### Summary of Files Modified

| File | Changes |
|------|---------|
| `Genghis-Class-CoverMission.cs` | `renewAllStaticActors_recurs`: null-guard, `Timeout(0.05, ...)` marshaling |
| `Genghis-Class-StationaryMarkers.cs` | `ExtractShortTypeName`: spaces/non-breaking spaces; `AddMarker`: exact match |
| `Genghis.cs` | `ProcessSectionFileForMarkers`: `Directory.CreateDirectory` + null guard; `OnBattleStarted`: marker scan/destroy |
| `Genghis-initsubmission-BurnBabyBurn.cs` | `PATH`: `Campaign21` → `Genghis` |

---

**End of plan.**