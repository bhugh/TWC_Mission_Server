https://github.com/syn111/tacview-filter/blob/main/README.md

# tacview-filter

A command-line tool that reads [Tacview](https://www.tacview.net/) ACMI files (2.1 and 2.2), applies user-defined filters, and writes a modified ACMI file. Use it to trim recordings for debriefing and analysis by removing irrelevant objects or focusing on specific types (e.g. ground/sea units, coalition, or custom property rules).

Originally developed for the [476th vFighter Group](https://www.476vfightergroup.com/).

> **Project Status:** This tool is functional and battle-tested for common debriefing needs. It has been tested with ACMI exports from **DCS** and **Falcon BMS**. It is shared as a hobby project; while it reliably handles typical ACMI files, it has not been tested in every obscure edge case of the format.

## What it does

- **Parse** Tacview ACMI files (2.1 and 2.2) in both uncompressed (`.txt.acmi`) and compressed (`.zip.acmi`) form.
- **Filter** objects using a three-tier priority system:
  - **Hard filters** (highest): time crop (`--start-time` / `--end-time`), spatial track splitting (`--inside`), and discarding untyped objects (`--filter-untyped`). These cannot be overridden.
  - **Remove filters**: `--remove` and `--remove-inactive` mark objects for removal.
  - **Keep/Show filters** (override remove): `--keep` and `--show-engagements` can keep objects that would otherwise be removed.
- **Track splitting**: `--inside lat,lon,radius_nm` keeps only segments of tracks that fall inside the given circle(s). Objects with no position data pass through; mixed inside/outside tracks are split into separate instances.
- **Show-engagements**: keep weapon, target, and shooter around engagement events within a time window (default 15 s). If the target dies shortly after impact, its death is included in the window.
- **Output** a new ACMI file as compressed (ZIP) by default; use `--text` for uncompressed plain text.

With no filter options, the file is passed through unchanged.

## Requirements

- **CMake** 3.20 or later
- **C++20** compiler:
  - **Windows:** Visual Studio 2019 or later (MSVC)
  - **Linux/macOS:** GCC 10+ or Clang 10+
- **Dependencies:** Fetched by CMake via `FetchContent` (no manual install):
  - [CLI11](https://github.com/CLIUtils/CLI11) (v2.4.2) – command-line parsing
  - [miniz-cpp](https://github.com/tfussell/miniz-cpp) – ZIP read/write

## Building

From the project root, using the Ninja presets (requires a developer environment with CMake and Ninja):

```bash
cmake --preset ninja-debug
cmake --build --preset ninja-debug
```

For a release build: `cmake --preset ninja-release` then `cmake --build --preset ninja-release`. The executable is written to `bin/` at the project root.

**Windows (x64):** Configure and build from an **x64** developer environment (e.g. *x64 Native Tools Command Prompt for VS 2022* or *Developer PowerShell* with the x64 toolchain). If you see linker errors like *library machine type 'x86' conflicts with target machine type 'x64'*, delete `out/build/ninja-debug` (or the preset folder you use), then reconfigure and build from the x64 environment.

**Visual Studio:** Open the project folder (**File → Open → Folder**), select the **ninja-debug** (or **ninja-release**) preset, let CMake configure, then **Build → Build All**.

**Formatting:** If `clang-format` is found, run `cmake --build --preset ninja-debug --target format` to format the code.

## Usage

**Synopsis:**

```text
tacview-filter <input_file> [options]
```

- **`input_file`** – Path to an ACMI file (`.txt.acmi` or `.zip.acmi`; 2.1 and 2.2 supported). Required.
- **`-o, --output-file <path>`** – Output path. Default: `out.acmi`.
- **`--text`** – Save as uncompressed plain text (overrides default compressed output).
- **`--compressed`** – Save as compressed ZIP (default; use for consistency in scripts).
- **`-v, --version`** – Print version and exit.

### Filter priority

1. **Hard filters** always apply; nothing overrides them: `--start-time`, `--end-time`, `--inside`, `--filter-untyped`.
2. **Remove filters** mark objects for removal: `--remove <predicate>`, `--remove-inactive <predicate>` (both repeatable).
3. **Keep/Show filters** override removal: `--keep <predicate>`, `--show-engagements`.

### Filter options

**Hard filters**

- **`--start-time <seconds>`** – Discard all data before this time (seconds since ReferenceTime).
- **`--end-time <seconds>`** – Discard all data after this time.
- **`--inside lat,lon,radius_nm`** – Keep only track segments inside the given circle(s). Repeatable; an object is “inside” if it lies in **any** zone. Tracks that cross the boundary are split into separate instances; objects with no position data pass through.
- **`--filter-untyped`** – Discard objects with no `Type` property.

**Remove filters**

- **`--remove <predicate>`** – Remove objects matching the predicate (repeatable).
- **`--remove-inactive <predicate>`** – Remove matching objects that have at most one position sample (repeatable).

**Keep/Show filters**

- **`--keep <predicate>`** – Keep objects matching the predicate; overrides remove (repeatable).
- **`--show-engagements ["window_seconds predicate"]`** – Keep weapon, target, and shooter around engagement events within a time window (default 15 s). Optional argument: omit for default (15 s, all engagements), or e.g. `"30"`, `"Type=Air.*"`, or `"30 Type=Air.*"` (order doesn’t matter). Repeatable. An engagement is included if the predicate matches **any** of weapon, target, or launcher. When multiple rules match across the trio, the **tightest** (most specific) match wins; on a tie in specificity, the **largest** window value is used. If the target dies shortly after impact, its death time is included in the target’s window.

**Other**

- **`--downsample <interval>`** – Keep at most one position sample per interval seconds per object (0 = disabled).
- **`--strip-property <name>`** – Remove a property from all remaining objects (repeatable).
- **`--set-property Name=Value`** – Set a property on all remaining objects (repeatable).
- **`--dry-run`** – Print filtering summary without writing output.

### Advanced filters

**Fog-of-war (`--fog-of-war`)**

Fog-of-war applies proximity-based visibility: only objects that are within a given distance of a “revealer” at each moment in time are kept visible. Objects matching the fog predicate are **revealers** (e.g. your coalition’s units); all other objects are removed unless they lie within the reveal distance of at least one revealer at that time. Tracks that are sometimes in range and sometimes not are split into segments; never-visible objects are removed (unless overridden by keep/show options).

**Argument format:** One quoted string per rule: `"predicate [distance_nm]"` or `"[distance_nm] predicate"`. The **predicate** is required and must include either `Coalition=...` or `Color=...` (same Key=Regex format as other predicates). **distance** is optional, a nonnegative float in nautical miles (default 5). For DCS exports, `Color=Blue` or `Color=Red` is often simpler than Coalition (see predicate format below).

**Tightest-match:** If you pass multiple `--fog-of-war` rules, each object that is a revealer gets the distance from the **most specific** (most conditions) matching rule. For example:

- `--fog-of-war "Coalition=Allies 10" --fog-of-war "Coalition=Allies,Type=Ground.* 5"`
  Allies air units use 10 nm; Allies ground units use 5 nm.

**Interaction with other filters:** Fog-of-war is in the remove tier: never-visible objects are marked for removal. The keep/show overrides still apply: `--keep` and `--show-engagements` can keep never-visible objects (e.g. keep a specific unit, or show engagement participants that were never in range).

**Examples:**

Fog 5 nm around your side (Coalition or Color):

```bash
tacview-filter in.acmi -o out.acmi --fog-of-war "Coalition=Allies"
tacview-filter in.acmi -o out.acmi --fog-of-war "Color=Blue"
```

Fog 10 nm around Allies, show engagements (participants outside fog kept):

```bash
tacview-filter in.acmi -o out.acmi --fog-of-war "Coalition=Allies 10" --show-engagements
```

Fog with different distances: Allies air 10 nm, Allies ground 5 nm:

```bash
tacview-filter in.acmi -o out.acmi --fog-of-war "Coalition=Allies 10" --fog-of-war "Coalition=Allies,Type=Ground.* 5"
```

Keep a specific unit even if never in fog range:

```bash
tacview-filter in.acmi -o out.acmi --fog-of-war "Coalition=Allies" --keep "Name=ImportantUnit"
```

### Predicate format

Comma-separated `Key=Regex` or `Key!=Regex` pairs; **all** pairs must match (AND logic). Use `=` to require a match and `!=` to require that the value does **not** match the regex.

**Properties common to BMS and DCS:** `Type`, `Name`, `Coalition`, `Color`, and `Pilot` appear in both. **Type** is a tag list (e.g. `Air+FixedWing`, `Ground+Vehicle`). **Coalition** and **Color** are the main discriminators: DCS uses `Allies` / `Enemies` / `Neutral`(s), while BMS uses coalition names such as `DPRK` / `ROK`; **Color** is `Red` or `Blue` in both. Other properties (e.g. `Country`, `Group`, `AGL` in DCS; telemetry in BMS) may be present or absent depending on the export—predicates match whatever properties the file contains.

**DCS Tacview:** In DCS exports, `Coalition=Allies` is the **Red** side and `Coalition=Enemies` is the **Blue** side—the names are the opposite of what many people expect. You can use either **Coalition** or **Color** in predicates: `Color=Blue` / `Color=Red` match the same sides and are often more intuitive for DCS users. Use the literal values your Tacview file contains.

**Falcon BMS Tacview:** BMS exports use **Coalition** names that depend on the scenario (e.g. **DPRK** / **ROK** for Korea). **Color** is again `Red` or `Blue`, so `Color=Blue` / `Color=Red` work the same as in DCS and are often easier than remembering which coalition name is which side. Use the literal Coalition and Color values present in your file.

Examples: `Type=Ground.*` · `Type=Air\.*,Coalition=Enemies` · `Coalition=Enemies,Type!=Air.*` (enemies that are not aircraft) · `Color=Blue` (fog revealers) · `Coalition=ROK` (BMS friendly side in Korea)

### Default when no filters are given

If you pass no filter options, the file is loaded and saved as-is (passthrough). No error.

### Examples

Pass through (no filtering):

```bash
tacview-filter in.acmi -o out.acmi
```

Remove enemies, keep ships (`--keep` overrides `--remove`):

```bash
tacview-filter in.acmi -o out.acmi --remove "Coalition=Enemies" --keep "Type=Sea.*"
```

Remove enemies, show engagements (show overrides remove for weapon/target/shooter):

```bash
tacview-filter in.acmi -o out.acmi --remove "Coalition=Enemies" --show-engagements
```

Focus on a geographic area and show engagements:

```bash
tacview-filter in.acmi -o out.acmi --inside 36.2,-115.0,50 --show-engagements
```

Trim to first 10 minutes, remove weapons:

```bash
tacview-filter in.acmi -o out.acmi --end-time 600 --remove "Type=Weapon.*"
```

Multiple zones (OR logic):

```bash
tacview-filter in.acmi -o out.acmi --inside 36.2,-115.0,50 --inside 42.0,43.0,30
```

Version:

```bash
tacview-filter --version
```

## License

Licensed under the GNU General Public License v3.0 or later. See [LICENSE](LICENSE) for the full text.

## Credits

- Author: [syn111](https://github.com/syn111) (Callsign: Ahmed)
