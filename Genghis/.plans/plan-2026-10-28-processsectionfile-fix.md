# Plan: Fix ProcessSectionFileForMarkers "Null Path" Error

**Date:** 2026-10-28  
**Working Directory:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\`  
**Compile Gate:** `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1` (run from `TWC_Mission_Server\`)  
**C# Version:** C#5 only — NO string interpolation, NO `?.`, NO `nameof`, NO `out var`, NO pattern matching.

---

## Problem Analysis

The `ProcessSectionFileForMarkers` method still throws "Value cannot be null. Parameter name: Null Path" during `ReadInitialSubmissions`. This happens when loading submission `.mis` files — markers in those files are NOT extracted at load time and instead load as physical objects.

**Evidence from log (session starting 2026-10-27 23:53:09):**
```
[2026-10-27 23:53:19] [ProcessSectionFileForMarkers] Error: Value cannot be null.
```
(8 occurrences at that timestamp)

**Root cause:** The error occurs at `f.save(tempPath)`. Likely causes:
1. `CLOD_PATH` or `FILE_PATH` may be null/empty when the method runs
2. String concatenation with null values produces malformed paths  
3. `Directory.CreateDirectory` catch block swallows errors silently
3. No visibility into what `tempPath` actually is when the error occurs

---

## Step-by-Step Instructions

### Step 1: Replace ProcessSectionFileForMarkers Method  **[COMPLETED - 2026-10-28]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Lines:** ~9189–9270

Replace the entire method with:

```csharp
    // Process a section file to extract stationary markers and remove them from the file
    // This prevents physical stationary objects from being loaded into the game (saving network slots)
    private ISectionFile ProcessSectionFileForMarkers(ISectionFile f, string sourceFile)
    {
        try
        {
            if (f == null) return null;

            // Validate that path components are set before using them
            if (string.IsNullOrEmpty(CLOD_PATH))
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] CLOD_PATH is null or empty - skipping marker extraction");
                return f;
            }
            if (string.IsNullOrEmpty(FILE_PATH))
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] FILE_PATH is null or empty - skipping marker extraction");
                return f;
            }

            // Build section directory path safely using Path.Combine
            string sectionDir = System.IO.Path.Combine(CLOD_PATH, FILE_PATH, "sectionfiles");
            
            try
            {
                System.IO.Directory.CreateDirectory(sectionDir);
            }
            catch (Exception dirEx)
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] Cannot create sectionfiles dir '" + sectionDir + "': " + dirEx.Message);
                return f;
            }

            string tempPath = System.IO.Path.Combine(sectionDir, "markers_temp_file_" + random.Next(10, 999).ToString());
            
            f.save(tempPath);
            
            string content = System.IO.File.ReadAllText(tempPath);
            System.IO.File.Delete(tempPath);
            
            string[] lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            System.Collections.Generic.List<string> outputLines = new System.Collections.Generic.List<string>();
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
        catch (System.Exception ex)
        {
            Console.WriteLine("[ProcessSectionFileForMarkers] Error: " + ex.Message);
            if (ex.StackTrace != null)
                Console.WriteLine("[ProcessSectionFileForMarkers] StackTrace: " + ex.StackTrace);
            return f;
        }
    }
```

**Key changes:**
- Added `string.IsNullOrEmpty` checks for `CLOD_PATH` and `FILE_PATH` with descriptive logging
- Replaced string concatenation with `System.IO.Path.Combine()` for safe path building
- Log includes the actual `sectionDir` path on directory creation failure
- Added stack trace logging in catch block for debugging

### Step 0: Baseline Compile Check  **[COMPLETED - 2026-10-28]**
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```
Expected: 0 errors.

### Step 1: Replace ProcessSectionFileForMarkers Method  **[COMPLETED - 2026-10-28]**

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Lines:** ~9189–9270

Replace the entire method with:

```csharp
    // Process a section file to extract stationary markers and remove them from the file
    // This prevents physical stationary objects from being loaded into the game (saving network slots)
    private ISectionFile ProcessSectionFileForMarkers(ISectionFile f, string sourceFile)
    {
        try
        {
            if (f == null) return null;

            // Validate that path components are set before using them
            if (string.IsNullOrEmpty(CLOD_PATH))
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] CLOD_PATH is null or empty - skipping marker extraction");
                return f;
            }
            if (string.IsNullOrEmpty(FILE_PATH))
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] FILE_PATH is null or empty - skipping marker extraction");
                return f;
            }

            // Build section directory path safely using Path.Combine
            string sectionDir = System.IO.Path.Combine(CLOD_PATH, FILE_PATH, "sectionfiles");
            
            try
            {
                System.IO.Directory.CreateDirectory(sectionDir);
            }
            catch (Exception dirEx)
            {
                Console.WriteLine("[ProcessSectionFileForMarkers] Cannot create sectionfiles dir '" + sectionDir + "': " + dirEx.Message);
                return f;
            }

            string tempPath = System.IO.Path.Combine(sectionDir, "markers_temp_file_" + random.Next(10, 999).ToString());
            
            f.save(tempPath);
            
            string content = System.IO.File.ReadAllText(tempPath);
            System.IO.File.Delete(tempPath);
            
            string[] lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            System.Collections.Generic.List<string> outputLines = new System.Collections.Generic.List<string>();
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
        catch (System.Exception ex)
        {
            Console.WriteLine("[ProcessSectionFileForMarkers] Error: " + ex.Message);
            if (ex.StackTrace != null)
                Console.WriteLine("[ProcessSectionFileForMarkers] StackTrace: " + ex.StackTrace);
            return f;
        }
    }
```

**Key changes:**
- Added `string.IsNullOrEmpty` checks for `CLOD_PATH` and `FILE_PATH` with descriptive logging
- Replaced string concatenation with `System.IO.Path.Combine()` for safe path building
- Log includes the actual `sectionDir` path on directory creation failure
- Added stack trace logging in catch block for debugging
### Step 2: Compile Check  **[COMPLETED - 2026-10-28]**
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```
Expected: 0 errors.

### Step 3: Test Server Validation  **[PENDING - Requires test server deployment]**

Deploy to test server and verify:
1. **No** `[ProcessSectionFileForMarkers] Error: Value cannot be null.` messages
2. **No** `[ProcessSectionFileForMarkers] Cannot create sectionfiles dir:` messages
3. `[StationaryMarkerManager] Virtualized X physical markers from main mission` still appears
4. If errors persist, new diagnostic messages (`CLOD_PATH is null or empty` / `FILE_PATH is null or empty` / stack trace) will identify the exact cause

---

## Files Modified
| File | Changes |
|------|---------|
| `Genghis.cs` | `ProcessSectionFileForMarkers`: null checks for path components, `Path.Combine` usage, enhanced error logging |

---

**End of plan.**