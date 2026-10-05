# Genghis mission (CloD server mission script)

## Repo layout - independent projects, one per subdirectory

Genghis\, Campaign21\, M001, M002, M003, Testing\ and Tobruk_Campaign\ are SEPARATE, self-contained
campaigns that happen to share techniques and history. Genghis\ is the actively developed one.
Do not read, cite or edit a sibling campaign while working here - its code is a different campaign.

## Verify changes compile

    pwsh -NoProfile -ExecutionPolicy Bypass -File .\Genghis\check-compile.ps1

run from `TWC_Mission_Server\`.

- Exit `0` = compiled with no errors, `1` = compile errors, `2` = could not run.
- Add `-ShowWarnings` to list warnings (suppressed by default).
- Baseline verified 2026-10-05: **0 errors, ~64 warnings**.
- Requires the .NET SDK + .NET Framework 4.8 Developer Pack. Finds the CloD assemblies automatically;
  override with `-CloDPath "<...>\IL-2 Sturmovik Cliffs of Dover Blitz\parts\core"`.

## Do not use `dotnet build` on the repo

`TWC_Mission_Server.csproj` is SDK-style with no explicit `<Compile>` items, so it globs every `.cs`
in the repo - including duplicate mission copies under `Campaign21\`, `Tobruk_Campaign\` and
`Testing\`, which redefine `Mission` / `Calcs` / `CoverCalcs` and produce thousands of duplicate-type
errors.

An alternative harness exists at `%TEMP%\CoverCheck\CoverCheck.csproj`, but its `<Compile>` list is
hand-maintained and currently omits `Genghis-Class-CloDNotes.cs` and
`Genghis-Class-TacviewImplMission.cs`, so it will not catch errors in those two files.

## Design-time project for IDE IntelliSense

`Genghis.csproj` (in this folder) is a **design-time-only** project registered in
`TWC_Mission_Server.sln` for VS Code / C# Dev Kit / OmniSharp IntelliSense.

```
dotnet build Genghis\Genghis.csproj
```

- Targets `net481` because the CloD engine DLLs are built against .NET Framework 4.8.1.
- Expected baseline: **2 errors** (CS0122 for `TacviewCore` in `Genghis-Class-TacviewRecorder.cs`
  — `TacviewCore` is internal in the prebuilt `TacviewRecorder.dll`) and ~50 warnings.
- **Not the build truth** — always use `check-compile.ps1` for that.

## Files deliberately excluded by the check

- `*-initsubmission*.cs` - separate CloD missions; they redefine `Mission` / `Calcs`.
- `Genghis-Class-TacviewRecorder.cs` - `TacviewCore` is internal in the prebuilt `TacviewRecorder.dll`.

Both pre-existing and unrelated to the script. Full detail is in `check-compile.ps1`'s own header.
