# Plan: Replace Physical Stationary Markers with Virtual Markers

## Overview
Replace physical stationary objects (JerryCan_GER1_1 through _5, AEC_Regent_II, MG_TA) used as markers for bomb statistics and civilian areas with virtual markers stored in a `ConcurrentDictionary`. This saves network slots since each physical stationary uses a network slot.

## Marker Types and Radii

| Marker Type | Purpose | Radius |
|-------------|---------|--------|
| `Stationary.Environment.JerryCan_GER1_1` | Enemy bombable area (small) | 71m |
| `Stationary.Environment.JerryCan_GER1_2` | Enemy bombable area (medium) | 141m |
| `Stationary.Environment.JerryCan_GER1_3` | Enemy bombable area (large) | 282m |
| `Stationary.Environment.JerryCan_GER1_5` | Enemy bombable area (x-large) | 1410m |
| `Stationary.AEC_Regent_II` | Civilian area (large) | 500m |
| `Stationary.MG_TA` | Civilian area (small) | 250m |

## Files to Modify

---

## Step-by-Step Implementation

### Phase 1: Create StationaryMarkerManager Class  **[COMPLETED - 2026-10-20]**

**File: Genghis/Genghis-Class-StationaryMarkers.cs** (NEW FILE) - Created and compiles successfully

Create a new class file with the following structure:

```csharp
#define DEBUG
#define TRACE
//$reference parts/core/gamePlay.dll
//$reference System.Core.dll

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using maddox.game;
using maddox.game.world;
using part;

public class StationaryMarker
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Type { get; set; }
    public double Radius { get; set; }
    public string SourceFile { get; set; }

    public StationaryMarker(double x, double y, string type, double radius, string sourceFile = "")
    {
        X = x;
        Y = y;
        Type = type;
        Radius = radius;
        SourceFile = sourceFile;
    }

    public Point3d Position
    {
        get { return new Point3d(X, Y, 0); }
    }

    public double DistanceTo(Point3d pos)
    {
        double dx = X - pos.x;
        double dy = Y - pos.y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}

public static class StationaryMarkerManager
{
    private static ConcurrentDictionary<string, List<StationaryMarker>> _markers = 
        new ConcurrentDictionary<string, List<StationaryMarker>>();

    private static readonly Dictionary<string, double> _markerRadii = new Dictionary<string, double>
    {
        { "JerryCan_GER1_1", 71 },
        { "JerryCan_GER1_2", 141 },
        { "JerryCan_GER1_3", 282 },
        { "JerryCan_GER1_5", 1410 },
        { "AEC_Regent_II", 500 },
        { "MG_TA", 250 }
    };

    static StationaryMarkerManager()
    {
        foreach (var kvp in _markerRadii)
        {
            _markers[kvp.Key] = new List<StationaryMarker>();
        }
    }

    public static void AddMarker(string type, double x, double y, string sourceFile = "")
    {
        double radius;
        if (!_markerRadii.TryGetValue(type, out radius))
        {
            Console.WriteLine("[StationaryMarkerManager] Unknown marker type: " + type);
            return;
        }

        var marker = new StationaryMarker(x, y, type, radius, sourceFile);
        _markers.AddOrUpdate(type, 
            new List<StationaryMarker> { marker },
            (key, existing) => { existing.Add(marker); return existing; });

        if (mainmission != null && mainmission.ON_TESTSERVER)
            Console.WriteLine("[StationaryMarkerManager] Added marker: " + type + " at (" + x + ", " + y + ") radius=" + radius + " from " + sourceFile);
    }

    public static List<StationaryMarker> FindNearbyMarkers(Point3d pos, double searchRadius)
    {
        var results = new List<StationaryMarker>();
        
        foreach (var kvp in _markers)
        {
            string type = kvp.Key;
            double markerRadius = _markerRadii[type];
            double effectiveRadius = searchRadius + markerRadius;
            
            foreach (var marker in kvp.Value)
            {
                if (marker.DistanceTo(pos) <= effectiveRadius)
                {
                    results.Add(marker);
                }
            }
        }
        
        return results;
    }

    public static List<StationaryMarker> FindNearbyMarkersOfTypes(Point3d pos, double searchRadius, params string[] types)
    {
        var results = new List<StationaryMarker>();
        
        foreach (string type in types)
        {
            List<StationaryMarker> list;
            if (!_markers.TryGetValue(type, out list)) continue;
            
            double markerRadius = _markerRadii[type];
            double effectiveRadius = searchRadius + markerRadius;
            
            foreach (var marker in list)
            {
                if (marker.DistanceTo(pos) <= effectiveRadius)
                {
                    results.Add(marker);
                }
            }
        }
        
        return results;
    }

    public static List<StationaryMarker> GetMarkersOfType(string type)
    {
        List<StationaryMarker> list;
        _markers.TryGetValue(type, out list);
        return list ?? new List<StationaryMarker>();
    }

    public static int GetTotalCount()
    {
        int count = 0;
        foreach (var kvp in _markers)
        {
            count += kvp.Value.Count;
        }
        return count;
    }

    public static void Clear()
    {
        foreach (var kvp in _markers)
        {
            kvp.Value.Clear();
        }
    }

    public static void DebugPrintAll()
    {
        Console.WriteLine("[StationaryMarkerManager] Total markers: " + GetTotalCount());
        foreach (var kvp in _markers)
        {
            if (kvp.Value.Count > 0)
            {
                Console.WriteLine("  " + kvp.Key + ": " + kvp.Value.Count);
                foreach (var m in kvp.Value)
                {
                    Console.WriteLine("    (" + m.X + ", " + m.Y + ") r=" + m.Radius + " from " + m.SourceFile);
                }
            }
        }
    }

---

### Phase 2: Modify Genghis.cs - Intercept .mis File Loading  **[COMPLETED - 2026-10-20]**

**File: Genghis/Genghis.cs** - Added ProcessSectionFileForMarkers method and integrated into ReadInitialSubmissions (both instant and delayed branches)

Modify the `ReadInitialSubmissions` method to intercept and process .mis files before loading them. The method currently:
1. Loads the .mis file as an `ISectionFile`
2. Calls `Calcs.changeArmyToTerritory_SectionFile`
3. Posts it with `GamePlay.gpPostMissionLoad(f)`

**Changes needed:**

In the `ReadInitialSubmissions` method, after loading the section file but before calling `changeArmyToTerritory_SectionFile`, add code to:
1. Parse the section file for marker items
2. Extract their positions and types
3. Add them to `StationaryMarkerManager`
4. Remove those lines from the section file
5. Then continue with the existing processing

Add this helper method to Genghis class:

```csharp
// Add this method to the Genghis class
private ISectionFile ProcessSectionFileForMarkers(ISectionFile f, string sourceFile)
{
    try
    {
        string tempPath = System.IO.Path.GetTempFileName();
        f.save(tempPath);
        
        string content = System.IO.File.ReadAllText(tempPath);
        System.IO.File.Delete(tempPath);
        
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var outputLines = new List<string>();
        bool inStationarySection = false;
        
        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            
            if (trimmed.StartsWith("["))
            {
                inStationarySection = trimmed.Equals("[Stationary]", StringComparison.OrdinalIgnoreCase);
            }
            
            bool isMarker = false;
            if (inStationarySection && trimmed.StartsWith("Static"))
            {
                if (trimmed.Contains("JerryCan_GER1_1") || trimmed.Contains("JerryCan_GER1_2") || 
                    trimmed.Contains("JerryCan_GER1_3") || trimmed.Contains("JerryCan_GER1_5") ||
                    trimmed.Contains("AEC_Regent_II") || trimmed.Contains("MG_TA"))
                {
                    string[] parts = trimmed.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        string typeName = parts[1];
                        string shortType = StationaryMarkerManager.ExtractShortTypeName(typeName);
                        
                        double x = 0, y = 0;
                        if (double.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x) &&
                            double.TryParse(parts[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y))
                        {
                            StationaryMarkerManager.AddMarker(shortType, x, y, sourceFile);
                            isMarker = true;
                        }
                    }
                }
            }
            
            if (!isMarker)
            {
                outputLines.Add(line);
            }
        }
        
        string processedContent = string.Join("\r\n", outputLines);
        string processedPath = System.IO.Path.GetTempFileName();
        System.IO.File.WriteAllText(processedPath, processedContent);
        
        ISectionFile processedFile = GamePlay.gpLoadSectionFile(processedPath);
        System.IO.File.Delete(processedPath);
        
        return processedFile;
    }
    catch (Exception ex)
    {
        Console.WriteLine("[ProcessSectionFileForMarkers] Error: " + ex.Message);
        return f;
    }
}
```

Then in `ReadInitialSubmissions`, change BOTH branches (instant and delayed):

**Instant branch (around line 9115-9120):**
```csharp
---

### Phase 3: Modify Genghis.cs - OnBattleStarted Cleanup  **[COMPLETED - 2026-10-20]**

**File: Genghis/Genghis.cs** - Added Timeout(1.0) after DrawFrontLinesPerMapState to scan for physical markers in main mission and virtualize them

After the initial front line drawing and before loading initial submissions, add a scan to find and remove any markers that were in the MAIN .mis file (which loads before OnBattleStarted).

Add this code in `OnBattleStarted` after `DrawFrontLinesPerMapState` call (around line 8775):

```csharp
// NEW: Scan for and remove physical marker objects from the main mission
Timeout(1.0, () => 
{
    try 
    {
        Console.WriteLine("[StationaryMarkerManager] Scanning for physical markers to virtualize...");
        
        GroundStationary[] allStatics = GamePlay.gpGroundStationarys(250000, 252000, 500000);
        
        int removed = 0;
        foreach (GroundStationary sta in allStatics)
        {
            if (sta == null) continue;
            
            string title = sta.Title;
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
    catch (Exception ex)
    {
        Console.WriteLine("[StationaryMarkerManager] Error during main mission scan: " + ex.Message);
    }
});
```
ISectionFile f = GamePlay.gpLoadSectionFile(s);
---

### Phase 4: Modify Genghis-Class-StatsMission.cs - Bomb Explosion Handling  **[COMPLETED - 2026-10-20]**

**File: Genghis/Genghis-Class-StatsMission.cs** - Replaced gpGroundStationarys bomb handling with virtual marker lookup using StationaryMarkerManager.FindNearbyMarkers

The current code searches for ground stationaries and checks their titles. Replace with virtual marker lookup.

**Current code to replace (lines ~7164-7210):**
```csharp
if (!ai & !groundActorsFound) foreach (GroundStationary sta in GamePlay.gpGroundStationarys(pos.x, pos.y, 500))
{
    if (sta == null) continue;
    
    double dis_m = sta.pos.distance(ref pos);
    
    if (sta.Title.Contains("JerryCan_GER1_1") || sta.Title.Contains("JerryCan_GER1_2") || sta.Title.Contains("JerryCan_GER1_3") || sta.Title.Contains("JerryCan_GER1_5"))
    {
        if (sta.Title.Contains("JerryCan_GER1_5") && dis_m <= 1410) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
        if (sta.Title.Contains("JerryCan_GER1_3") && dis_m <= 282) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
        if (sta.Title.Contains("JerryCan_GER1_2") && dis_m <= 141) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
        if (sta.Title.Contains("JerryCan_GER1_1") && dis_m <= 71) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
    }
    
    if (landType != maddox.game.LandTypes.ROAD && landType != maddox.game.LandTypes.ROAD_MASK && landType != maddox.game.LandTypes.HIGHWAY && landType != maddox.game.LandTypes.RAIL)
    {
        if (sta.Title.Contains("AEC_Regent_II") && dis_m <= 500) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
        if (sta.Title.Contains("MG_TA") && dis_m <= 250) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
    }
}
```

**Replace with:**
```csharp
if (!ai & !groundActorsFound) 
{
    var nearbyMarkers = StationaryMarkerManager.FindNearbyMarkers(pos, 500);
    
    foreach (var marker in nearbyMarkers)
    {
        double dis_m = marker.DistanceTo(pos);
        
        if (marker.Type.StartsWith("JerryCan_GER1"))
        {
            if (marker.Type == "JerryCan_GER1_5" && dis_m <= 1410) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
            if (marker.Type == "JerryCan_GER1_3" && dis_m <= 282) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
            if (marker.Type == "JerryCan_GER1_2" && dis_m <= 141) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
            if (marker.Type == "JerryCan_GER1_1" && dis_m <= 71) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
        }
        
        if (landType != maddox.game.LandTypes.ROAD && landType != maddox.game.LandTypes.ROAD_MASK && landType != maddox.game.LandTypes.HIGHWAY && landType != maddox.game.LandTypes.RAIL)
        {
            if (marker.Type == "AEC_Regent_II" && dis_m <= 500) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
            if (marker.Type == "MG_TA" && dis_m <= 250) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
        }
    }
}
```
f = ProcessSectionFileForMarkers(f, s); // NEW: Extract markers
f = Calcs.changeArmyToTerritory_SectionFile(f, GamePlay, this);
```

**Delayed branch (around line 9135-9140):**
```csharp
ISectionFile f = GamePlay.gpLoadSectionFile(s);
f = ProcessSectionFileForMarkers(f, s); // NEW: Extract markers
f = Calcs.changeArmyToTerritory_SectionFile(f, GamePlay, this);
```
---

### Phase 5: Modify Dynamic JerryCan Placement  **[COMPLETED - 2026-10-20]**

**File: Genghis/Genghis.cs** - Replaced all dynamic Calcs.makeStatic calls for JerryCan markers with StationaryMarkerManager.AddMarker:
- Line 20262: HQ placement JerryCan_GER1_1
- Line 21722: GroundThingRemoval with marker type check
- Line 21883: TriggerObjectiveRefresh with marker type check
- Line 25647: BlueRadarInit JerryCan_GER1_1
- Line 33295: RemoveEnemyNeutral with marker type check

All other types (CamoNetPlaneBig, TentTroopLarge_GER1, etc.) remain as physical objects.

**Locations to find and replace:**

1. **Around line 20153:**
```csharp
f = Calcs.makeStatic(f, GamePlay, this, newPos.x + random.Next(4), newPos.y + random.Next(3), 0, type: "Stationary.Environment.JerryCan_GER1_1", heading: random.Next(360), side: "nn");
```
**Replace with:**
```csharp
StationaryMarkerManager.AddMarker("JerryCan_GER1_1", newPos.x + random.Next(4), newPos.y + random.Next(3), "DynamicPlacement");
```

2. **Around line 21601** (and surrounding loop):
```csharp
string typ = "Stationary.Environment.JerryCan_GER1_2";
```
Find the `Calcs.makeStatic` call using `typ` and replace.

3. **Around line 21768:**
```csharp
string sname = "Stationary.Environment.JerryCan_GER1_1";
```
Find the `Calcs.makeStatic` call using `sname` and replace.

4. **Around lines 25210-25213 and 25268:**
```csharp
string jerry = "JerryCan_GER1_1";
if (mo.TriggerDestroyRadius > 71) jerry = "JerryCan_GER1_2";
if (mo.TriggerDestroyRadius > 141) jerry = "JerryCan_GER1_3";
if (mo.TriggerDestroyRadius > 282) jerry = "JerryCan_GER1_5";
```
And the `Calcs.makeStatic` calls using `jerry` variable. Replace with `AddMarker` calls.

5. **Around lines 25543-25545:**
```csharp
f = Calcs.makeStatic(f, GamePlay, this, x - 3.7, y + 4.5, mo.Pos.z, "Stationary.Environment.JerryCan_GER1_1", side: "de");
f = Calcs.makeStatic(f, GamePlay, this, x - 4.6, y - 4.6, mo.Pos.z, "Stationary. Environment.JerryCan_GER1_1", side: "de");
f = Calcs.makeStatic(f, GamePlay, this, x + 5.2, y - 5.6, mo.Pos.z, "Stationary.Environment.JerryCan_GER1_1", side: "de");
```
Replace each with `AddMarker` calls.

6. **Around lines 33174, 33458:**
Similar dynamic placements in embedded .mis content strings - these are likely in string constants for mission generation. Check if they are actually executed or just templates.

**Search pattern in VS Code / editor:**
- `Calcs.makeStatic`
- `"Stationary.Environment.JerryCan_GER1_"`
- `"Stationary.AEC_Regent_II"`
- `"Stationary.MG_TA"`

For each found location, determine the marker type from the string and call:
```csharp
StationaryMarkerManager.AddMarker("SHORT_TYPE_NAME", x, y, "ContextDescription");
```
Where SHORT_TYPE_NAME is one of: `JerryCan_GER1_1`, `JerryCan_GER1_2`, `JerryCan_GER1_3`, `JerryCan_GER1_5`, `AEC_Regent_II`, `MG_TA`.
    public static string ExtractShortTypeName(string fullTypeName)
    {
        int lastDot = fullTypeName.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < fullTypeName.Length - 1)
        {
            return fullTypeName.Substring(lastDot + 1);
        }
        return fullTypeName;
    }
}
```
1. **Genghis/Genghis.cs** - Main mission file
2. **Genghis/Genghis-Class-StatsMission.cs** - Stats mission with bomb handling
3. **New file: Genghis/Genghis-Class-StationaryMarkers.cs** - New marker manager class
---

### Phase 6: Include the New Class in Genghis.cs  **[COMPLETED - 2026-10-20]**

**File: Genghis/Genghis.cs** - Added include for Genghis-Class-StationaryMarkers.cs at line 15 (after TacviewImplMission include)

Add the include for the new class after the existing includes (after line 15):
```csharp
//$include "$user\missions\Multi\Fatal\Genghis\Fresh Input File\Genghis-Class-StationaryMarkers.cs"
```

### Phase 7: Compile and Test

1. Run the compile check:
```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```

2. Verify 0 errors (warnings are OK)

3. Test in-game:
   - Load the mission
   - Check console for "[StationaryMarkerManager] Virtualized X physical markers from main mission"
   - Drop bombs on known marker locations
   - Verify bomb hits still count as enemy hits
   - Verify civilian bombing penalties still work
   - Check network slot usage is reduced

---

## Testing Checklist

- [ ] Compile passes with 0 errors
- [ ] Mission loads without errors
- [ ] Console shows marker virtualization messages
- [ ] Bombing on JerryCan_GER1_1 locations registers as enemy hits
- [ ] Bombing on JerryCan_GER1_2/3/5 locations registers as enemy hits
- [ ] Bombing near AEC_Regent_II registers as civilian bombing
- [ ] Bombing near MG_TA registers as civilian bombing
- [ ] Roads/highways/rails in civilian areas don't trigger penalties
- [ ] Dynamic jerrycan placement (objective creation) works
- [ ] Network slot usage reduced (check server logs)
- [ ] No memory leaks (marker dictionary doesn't grow unbounded)

---

## Rollback Plan

If issues arise:
1. Comment out the `$include` for Genghis-Class-StationaryMarkers.cs
2. Revert changes to `ReadInitialSubmissions`
3. Revert changes to `OnBattleStarted`
4. Revert changes to bomb explosion handling in StatsMission
5. Revert dynamic placement changes
6. Run compile check

---

## Notes for Implementation

1. **C#5 Compatibility**: Use old syntax - no string interpolation, no `nameof`, no `?.`, no pattern matching. Use `String.Format` or concatenation. Use `as` + null check instead of pattern matching.

2. **Thread Safety**: `ConcurrentDictionary` is thread-safe for reads/writes. The `List<StationaryMarker>` inside is not thread-safe for concurrent modification, but we only add during initialization (single-threaded) and read during bomb explosions (which are serialized via Timeout). This should be safe.

3. **Memory**: Markers are never removed after initialization. This is fine for a mission session. If missions run for weeks, consider adding a cleanup, but not needed for now.

4. **Main Mission Markers**: The main .mis file loads before `OnBattleStarted`, so we must scan and destroy those markers in `OnBattleStarted` with a small delay.

5. **Submission .mis Files**: These are loaded via `ReadInitialSubmissions` which we intercept, so markers in those files never become physical objects.

6. **Precision**: Use `double` for coordinates, `Math.Sqrt` for distance. The game uses meters.

7. **Line Endings**: Ensure CRLF line endings in all edited files.

---

## Detailed Line References for Genghis-Class-StatsMission.cs

| Code Section | Approximate Line | Action |
|--------------|------------------|--------|
| JerryCan check block | 7174-7184 | Replace with virtual marker lookup |
| Civilian marker check | 7197-7198 | Replace with virtual marker lookup |
| gpGroundStationarys call | 7164 | Remove, use StationaryMarkerManager |

## Detailed Line References for Genghis.cs

| Code Section | Approximate Line | Action |
|--------------|------------------|--------|
| ReadInitialSubmissions (instant) | 9115-9120 | Add ProcessSectionFileForMarkers call |
| ReadInitialSubmissions (delayed) | 9135-9140 | Add ProcessSectionFileForMarkers call |
| OnBattleStarted | 8775 | Add marker scan/destroy timeout |
| Dynamic JerryCan placement | 20153 | Replace with AddMarker |
| Dynamic JerryCan placement | 21601 | Replace with AddMarker |
| Dynamic JerryCan placement | 21768 | Replace with AddMarker |
| Dynamic JerryCan placement | 25210-25213 | Replace with AddMarker |
| Dynamic JerryCan placement | 25268 | Replace with AddMarker |
| Dynamic JerryCan placement | 25543-25545 | Replace with AddMarker |
| Dynamic JerryCan placement | 33174 | Replace with AddMarker |
| Dynamic JerryCan placement | 33458 | Replace with AddMarker |
| Includes section | 15 | Add new class include |

---

## Estimated Effort

- Phase 1 (New class): ~30 minutes
- Phase 2 (Intercept loading): ~45 minutes
- Phase 3 (OnBattleStarted cleanup): ~20 minutes
- Phase 4 (Bomb handling refactor): ~30 minutes
- Phase 5 (Dynamic placement): ~45 minutes
- Phase 6 (Include): ~5 minutes
- Phase 7 (Compile & test): ~30 minutes

**Total: ~3.5 hours**

---

## Dependencies

- Requires `System.Collections.Concurrent` (available in .NET 4.0+)
- Requires `System.IO` for file operations in `ProcessSectionFileForMarkers`
- The `Calcs.makeStatic` method creates physical stationaries - we replace calls to it

---

## Risk Mitigation

1. **Test on test server first** - Use ON_TESTSERVER flags for debug output
2. **Keep physical markers as fallback** - If virtual markers fail, the old gpGroundStationarys code could be kept as fallback (commented out)
3. **Monitor performance** - Virtual lookup should be faster than gpGroundStationarys
4. **Verify all marker types** - Check both JerryCan variants and both civilian marker types