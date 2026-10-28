# Plan: Add JerryCan_UK1_2-Gallon_1 and JerryCan_GER1_18 as Marker Equivalents

**Date:** 2026-10-28  
**Working Directory:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\`  
**Compile Gate:** `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1` (run from `TWC_Mission_Server\`)  
**C# Version:** C#5 only â€” NO string interpolation, NO `?.`, NO `nameof`, NO `out var`, NO pattern matching.

---

## Problem

Two marker types exist in `.mis` files but are not recognized by `StationaryMarkerManager`:

1. **`JerryCan_UK1_2-Gallon_1`** â€” should be equivalent to `JerryCan_GER1_1` (radius **71m**, enemy bombable area, small)
2. **`JerryCan_GER1_18`** â€” should be equivalent to `JerryCan_GER1_5` (radius **1410m**, enemy bombable area, x-large)

Without these additions, markers of these types are logged as "Unknown marker type" and never virtualized.

## Approach

**Six minimal changes across two files** â€” no changes needed to `ExtractShortTypeName` or `AddMarker` (both already handle exact string matching correctly).

| # | File | Change |
|---|------|--------|
| 1 | `Genghis-Class-StationaryMarkers.cs` | Add `JerryCan_UK1_2-Gallon_1` â†’ 71 to `_markerRadii` |
| 2 | `Genghis-Class-StationaryMarkers.cs` | Add `JerryCan_GER1_18` â†’ 1410 to `_markerRadii` |
| 3 | `Genghis.cs` | Add both to `ProcessSectionFileForMarkers` line-contains check |
| 4 | `Genghis.cs` | Add both to `OnBattleStarted` title-contains check |

---

## Step-by-Step Instructions

### Step 0: Baseline Compile Check [COMPLETED-2026-10-28]

**Goal:** Verify the current code compiles before making changes.

**Command:**
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```
**Run from:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\`

**Expected result:** Exit code 0 (0 errors, ~64 warnings).  
**If non-zero:** Stop and report errors. Do not proceed.

---

### Step 1: Add JerryCan_UK1_2-Gallon_1 to _markerRadii [COMPLETED-2026-10-28]

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-StationaryMarkers.cs`  
**Lines to modify:** ~43â€“51

**Current code:**
```csharp
    private static readonly Dictionary<string, double> _markerRadii = new Dictionary<string, double>
    {
        { "JerryCan_GER1_1", 71 },
        { "JerryCan_GER1_2", 141 },
        { "JerryCan_GER1_3", 282 },
        { "JerryCan_GER1_5", 1410 },
        { "AEC_Regent_II", 500 },
        { "MG_TA", 250 }
    };
```

**Replacement code:** Add `JerryCan_UK1_2-Gallon_1` entry after `JerryCan_GER1_1`:
```csharp
    private static readonly Dictionary<string, double> _markerRadii = new Dictionary<string, double>
    {
        { "JerryCan_GER1_1", 71 },
        { "JerryCan_UK1_2-Gallon_1", 71 },
        { "JerryCan_GER1_2", 141 },
        { "JerryCan_GER1_3", 282 },
        { "JerryCan_GER1_5", 1410 },
        { "AEC_Regent_II", 500 },
        { "MG_TA", 250 }
    };
```

**Notes:**
- Radius 71 matches `JerryCan_GER1_1` (enemy bombable area, small)
- `_markers` static constructor iterates `_markerRadii`, so a new empty list will be created automatically for the new key
- `AddMarker` uses exact `Equals(..., OrdinalIgnoreCase)` matching, so `JerryCan_UK1_2-Gallon_1` will be stored under its own key

**Verification:** Run compile check. Expect 0 errors.

---

### Step 2: Add JerryCan_GER1_18 to _markerRadii [COMPLETED-2026-10-28]

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-StationaryMarkers.cs`  
**Same dictionary as Step 1.**

**Current code (after Step 1):**
```csharp
        { "JerryCan_GER1_5", 1410 },
        { "AEC_Regent_II", 500 },
```

**Replacement code:** Add `JerryCan_GER1_18` entry after `JerryCan_GER1_5`:
```csharp
        { "JerryCan_GER1_5", 1410 },
        { "JerryCan_GER1_18", 1410 },
        { "AEC_Regent_II", 500 },
```

**Notes:**
- Radius 1410 matches `JerryCan_GER1_5` (enemy bombable area, x-large)
- Same reasoning as Step 1

**Verification:** Run compile check. Expect 0 errors.

---

### Step 3: Update ProcessSectionFileForMarkers Type Check [COMPLETED-2026-10-28]

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Location:** Inside `ProcessSectionFileForMarkers` method, the line-contains check

**Current code:**
```csharp
                    if (trimmed.Contains("JerryCan_GER1_1") || trimmed.Contains("JerryCan_GER1_2") || 
                        trimmed.Contains("JerryCan_GER1_3") || trimmed.Contains("JerryCan_GER1_5") ||
                        trimmed.Contains("AEC_Regent_II") || trimmed.Contains("MG_TA"))
```

**Replacement code:** Add both new types:
```csharp
                    if (trimmed.Contains("JerryCan_GER1_1") || trimmed.Contains("JerryCan_GER1_2") || 
                        trimmed.Contains("JerryCan_GER1_3") || trimmed.Contains("JerryCan_GER1_5") ||
                        trimmed.Contains("JerryCan_UK1_2-Gallon_1") ||
                        trimmed.Contains("JerryCan_GER1_18") ||
                        trimmed.Contains("AEC_Regent_II") || trimmed.Contains("MG_TA"))
```

**Notes:**
- This ensures markers of both types found in section files are extracted and added to the virtual marker dictionary during `.mis` loading
- Placed before `AEC_Regent_II` to keep the JerryCan group together

### Step 4: Update OnBattleStarted Marker Scan Type Check [COMPLETED-2026-10-28]

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Location:** Inside `virtualizePhysicalMarkerObjects()` method (called from `OnBattleStarted`), the marker scan/destroy timeout

**Current code:**
```csharp
                        if (title.Contains("JerryCan_GER1_1") || title.Contains("JerryCan_GER1_2") ||
                            title.Contains("JerryCan_GER1_3") || title.Contains("JerryCan_GER1_5") ||
                            title.Contains("AEC_Regent_II") || title.Contains("MG_TA"))
```

**Replacement code:** Add both new types:
```csharp
                        if (title.Contains("JerryCan_GER1_1") || title.Contains("JerryCan_GER1_2") ||
                            title.Contains("JerryCan_GER1_3") || title.Contains("JerryCan_GER1_5") ||
                            title.Contains("JerryCan_UK1_2-Gallon_1") ||
                            title.Contains("JerryCan_GER1_18") ||
                            title.Contains("AEC_Regent_II") || title.Contains("MG_TA"))
```

**Notes:**
- This ensures physical objects of both types in the main mission are virtualized and destroyed during `OnBattleStarted`
- Placed in the same group as other JerryCan types

**Verification:** Run compile check. Expect 0 errors.

---

### Step 5: Final Compile Check [COMPLETED-2026-10-28]

**Command:**
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```
**Run from:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\`

**Expected result:** Exit code 0.  
**If errors:** Fix each error, re-run, until exit code 0.

---

### Step 6: Test Server Validation [COMPLETED-2026-10-28 - Implementation complete; awaiting test server deployment]

**Goal:** Confirm the fix works at runtime.

**What to watch for in the server log:**

1. **No** `[StationaryMarkerManager] Unknown marker type: JerryCan_UK1_2-Gallon_1` messages
2. **No** `[StationaryMarkerManager] Unknown marker type: JerryCan_GER1_18` messages
3. Both marker types counted in `[StationaryMarkerManager] Virtualized X physical markers from main mission`
4. `[StationaryMarkerManager] Total markers: N` includes both new types

**Deployment:** After successful test server validation, the changes can be promoted to production.

---

## Files Modified

| File | Changes |
|------|---------|
| `Genghis-Class-StationaryMarkers.cs` | `_markerRadii`: add `JerryCan_UK1_2-Gallon_1` â†’ 71 and `JerryCan_GER1_18` â†’ 1410 |
| `Genghis.cs` | `ProcessSectionFileForMarkers`: add both to line-contains check |
| `Genghis.cs` | `OnBattleStarted`: add both to title-contains check |

---

## Files NOT Touched (by design)

- `ExtractShortTypeName` â€” already splits on `.`, space, tab, and U+00A0; both types extracted correctly from their full `Stationary.Environment.` paths
- `AddMarker` â€” already uses exact `Equals(..., OrdinalIgnoreCase)`, so separate keys work fine
- `replace_regex.py` / `do_replace.py` â€” Python helper scripts, not part of the C# mission compiler

---

**End of plan. Ready for implementation when approved.**
