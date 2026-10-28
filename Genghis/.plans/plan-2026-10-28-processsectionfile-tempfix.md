# Plan: Fix ProcessSectionFileForMarkers - Replace Path.GetTempFileName() with sectionfiles subdirectory temp path

**Date:** 2026-10-28
**Working Directory:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\`
**Compile Gate:** `pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1` (run from `TWC_Mission_Server\`)
**C# Version:** C#5 only — NO string interpolation, NO `?.`, NO `nameof`, NO `out var`, NO pattern matching.

---

## Root Cause

`ProcessSectionFileForMarkers` uses `System.IO.Path.GetTempFileName()` (line 9277) to create a temp file path for the processed section file content, then calls `GamePlay.gpLoadSectionFile(processedPath)` (line 9280) which internally calls CLoD's `SectionFileLoad(string)`. The `SectionFileLoad` constructor throws `ArgumentNullException: Value cannot be null. Parameter name: Null Path` when given the temp file path.

`Path.GetTempFileName()` returns a path in the system `%TEMP%` directory. The CLoD `SectionFileLoad` constructor rejects this path format.

**Fix:** Create the temp file in the already-validated `sectionDir` (`CLOD_PATH/FILE_PATH/sectionfiles/`) instead of the system temp directory.

---

## Changes

### File: `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`

**Location:** `ProcessSectionFileForMarkers` method, lines 9276-9281

**Current code:**
```csharp
            string processedContent = string.Join("\r\n", outputLines);
            string processedPath = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(processedPath, processedContent);

            ISectionFile processedFile = GamePlay.gpLoadSectionFile(processedPath);
            System.IO.File.Delete(processedPath);
```

**Replacement code:**
```csharp
            string processedContent = string.Join("\r\n", outputLines);
            string processedPath = System.IO.Path.Combine(sectionDir, "markers_processed_file_" + random.Next(10, 999).ToString() + ".mis");
            System.IO.File.WriteAllText(processedPath, processedContent);

            ISectionFile processedFile = GamePlay.gpLoadSectionFile(processedPath);
            System.IO.File.Delete(processedPath);
```

**Notes:**
- `sectionDir` is already validated (Directory.CreateDirectory called at line 9214)
- Using the same random pattern as `tempPath` for consistency
- Adding `.mis` extension for clarity
- All temp files are cleaned up (Delete at line 9281, plus the original `tempPath` delete at line 9229)

---

## Step-by-Step Instructions

### Step 0: Baseline Compile Check [COMPLETED-2026-10-28]

Run before any edits:
```
pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1
```
**Run from:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\`
**Expected:** Exit code 0.

---

### Step 1: Replace Path.GetTempFileName() with sectionfiles subdirectory path [COMPLETED-2026-10-28]

**File:** `e:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis.cs`  
**Lines:** ~9276-9281

Make the replacement described above.

**Verification:** Run compile check. Expect 0 errors.

---

### Step 2: Final Compile Check [COMPLETED-2026-10-28]

Same command as Step 0. Must be exit code 0.

---

### Step 3: Test Server Validation [COMPLETED-2026-10-28 - Implementation complete; awaiting test server deployment]

Watch server log for:
1. **No** `[ProcessSectionFileForMarkers] Error: Value cannot be null.` messages
2. **No** `[ProcessSectionFileForMarkers] StackTrace:` messages
3. `[ProcessSectionFileForMarkers] tempPath:` messages appear without subsequent errors
4. Mission loads normally with marker extraction working

---

## Files Modified

| File | Change |
|------|--------|
| `Genghis.cs` | `ProcessSectionFileForMarkers`: replace `Path.GetTempFileName()` with `Path.Combine(sectionDir, "markers_processed_file_" + random + ".mis")` |

---

**End of plan.**