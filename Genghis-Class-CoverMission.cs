$ErrorActionPreference = 'Stop'
$path = 'E:\Cliffs-of-Dover\TWC-campaigns-github\TWC_Mission_Server\Genghis\Genghis-Class-CoverMission.cs'
$bytes = [System.IO.File]::ReadAllBytes($path)
$hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
$enc = New-Object System.Text.UTF8Encoding($hasBom)
$script:lines = [System.IO.File]::ReadAllLines($path)
Write-Output ("Loaded {0} lines, BOM={1}" -f $script:lines.Count, $hasBom)

function Find-Line($re) {
    for ($i = 0; $i -lt $script:lines.Count; $i++) { if ($script:lines[$i] -match $re) { return $i } }
    throw "START not found: $re"
}

function Splice($startRe, $endRe, $newLines, [bool]$swallowBlank = $false) {
    $s = Find-Line $startRe
    $e = $s
    if ($null -ne $endRe) {
        $found = $false
        for ($i = $s; $i -lt $script:lines.Count; $i++) { if ($script:lines[$i] -match $endRe) { $e = $i; $found = $true; break } }
        if (-not $found) { throw "END not found after line $($s+1): $endRe" }
    }
    if ($swallowBlank -and ($e + 1) -lt $script:lines.Count -and $script:lines[$e + 1].Trim() -eq '') { $e++ }
    $before = @(); if ($s -gt 0) { $before = $script:lines[0..($s - 1)] }
    $after = @(); if (($e + 1) -lt $script:lines.Count) { $after = $script:lines[($e + 1)..($script:lines.Count - 1)] }
    $script:lines = @($before) + @($newLines) + @($after)
    Write-Output ("Spliced {0}..{1} ({2} lines -> {3})" -f ($s + 1), ($e + 1), ($e - $s + 1), @($newLines).Count)
}

# E1: enum + drop comment block
Splice '//  drop - "drop when I drop"' 'public enum CoverAGOrders \{' @('    public enum CoverAGOrders {none, reserve, attack, normal, strict, escort, loiter};')

# E2: BAM_BombAimMode comment line
Splice 'answers "WHEN do they bomb", so it drives the airgroup ORDERS instead' $null @('    //answers "WHEN do they bomb", so it drives the <cdrop release machinery instead (see BAM_enterDropMode()).')

# E3: coverDictRemove comment about coverOrdersBeforeDrop
Splice '//Do NOT touch coverOrdersBeforeDrop here' '//the restore loop \(it iterates live coverAircraftAirGroupsActive keys\)\.' @()

# E4: BAM_leaveDropMode call in BAM_toggleBombAimMode
Splice 'if \(prev_BAMs\.Contains\(BAM_BombAimMode\.Drop_When_I_Drop\)\) BAM_leaveDropMode\(player\);' $null @()

# E5: BAM_enterDropMode - full rewrite
$enterDrop = @(
'    //<cdrop - turn DROP WHEN I DROP on.  Called both from the Tab-4-4-4-4-6 menu (BAM cycle) and from',
'    //the <cdrop chat command, so the menu label and the actual orders can never disagree.',
'    //2026-10-11 (plan-2026-10-11-2333): the old CoverAGOrders.drop order mode has been removed - it was',
'    //a vestige of the original -covermission.cs implementation that mashed orders mode together with',
'    //targeting mode.  DROP WHEN I DROP is now carried ENTIRELY by the player''s bomb-aim mode',
'    //(Drop_When_I_Drop / Drop_When_I_Drop_Nearest_Enemy): the drop machinery (coverDropWatch,',
'    //coverDropReleasePass, the pre-open bays) keys off the aim mode via isDropAimMode(), not off the',
'    //group orders.  The groups keep whatever orders the player gave them - <cstrict gives the tight',
'    //formation, and ordersHoldFire/ordersBombGround keep them quiet until the release.',
'    public void BAM_enterDropMode(Player player, string msg = "")',
'    {',
'        if (player == null) return;',
'',
'        turnOffCoverDropWatch(player);  //<cdrop RE-ARM - the previous pass stood the watcher down, so a timer from',
'                                        //it (and its coverDropLastLeaderBombCount seed) is no longer valid.  armCoverDropWatch',
'                                        //does this too, but doing it explicitly guarantees the stale count is gone BEFORE the',
'                                        //new seed is read, so a re-arm can never fire on a count left over from the old pass.',
'        armCoverDropWatch(player);      //<cdrop RE-ARM - WITHOUT this a re-issued <cdrop armed the groups but left no timer,',
'                                        //so the leader''s next drop was never detected (2026-10-02G: third run dropped, <cdropnow',
'                                        //needed).  armCoverDropWatch re-seeds the count & clears the "already fired" latch.',
'        BAM_forceFormationRefresh(player);  //<cdrop Step D - cancel any transient menu-cycle attack run NOW (see helper)',
'    }')
Splice '//<cdrop - turn DROP WHEN I DROP on\.' '^    \}$' $enterDrop

# E6: BAM_forceFormationRefresh comment
Splice '//orders == CoverAGOrders\.drop \(the <cdrop / <cstrict branch' $null @('    //orders == CoverAGOrders.strict (the <cstrict branch: FOLLOW, altDiff -3 +/- 15).  The')

# E7: EscortUpdateWaypoints orders param in BAM_forceFormationRefresh
Splice 'nodupe: true, orders: CoverAGOrders\.drop\);' $null @('                EscortUpdateWaypoints(player, airGroup, targetAirGroup, AiAirWayPointType.FOLLOW, altDiff_m: -3, AltDiff_range_m: 15, nodupe: true, orders: CoverAGOrders.strict);')

# E8: delete BAM_leaveDropMode entirely
Splice '//<cdrop - turn DROP WHEN I DROP off again' '^    \}$' @()

# E9: stale comment in BAM_toggleBombAimMode_withmessages
Splice '//<cdrop - entering/leaving DROP WHEN I DROP is a change' '//menu label, so capture the mode we are leaving in order to act on the transition\.' @(
'        //<cdrop - entering/leaving DROP WHEN I DROP now changes only the bomb-aim mode, not the airgroup',
'        //orders (CoverAGOrders.drop was removed 2026-10-11); this hook is kept for parity.')

# E10: dropFormationShiftFactor field + comment
Splice '//<cdrop - DROP groups fly their own fixed tight spread' 'public float dropFormationShiftFactor = 0\.6f;' @() $true

# E11: coverOrdersBeforeDrop field + comment
Splice '//<cdrop - snapshot of each player''s airgroup orders, taken when DROP WHEN I DROP is switched' 'coverOrdersBeforeDrop = new Dictionary' @() $true

# E12a: getShiftFactorForAirgroup comment trimmed
Splice '//The formation spread factor to use for this airgroup - <cstrict' '//apart - or pack the bombers inside each other''s wingspans\.' @(
'    //The formation spread factor to use for this airgroup - <cstrict (<cst) airgroups always use the fixed,',
'    //standard spread instead of the player''s <cdist setting.')
# E12b: drop branch
Splice 'if \(coverAircraftAirGroupsOrders\[airGroup\] == CoverAGOrders\.drop\) return dropFormationShiftFactor;' $null @()

# E13: onDrop diagnostic in armCoverDropWatch
Splice 'if \(coverAircraftAirGroupsOrders\.ContainsKey\(agD\) && coverAircraftAirGroupsOrders\[agD\] == CoverAGOrders\.drop\) onDrop\+\+;' $null @('                    if (isDropAimMode(player)) onDrop++;')

# E14: coverDropWatch stillWanted -> aim-mode test
Splice '//still using <cdrop at all\?  if they changed order, stand ourselves down\.' '^            \}$' @(
'            //still using <cdrop at all?  DROP WHEN I DROP is carried by the player''s bomb-aim mode now',
'            //(CoverAGOrders.drop is gone) - if that is no longer a drop mode, stand ourselves down.',
'            bool stillWanted = isDropAimMode(player);')

# E15: coverDropReleasePass group gate -> aim-mode test
Splice 'if \(!coverAircraftAirGroupsOrders\.ContainsKey\(airGroup\) \|\| coverAircraftAirGroupsOrders\[airGroup\] != CoverAGOrders\.drop\)' '^            \}$' @(
'            if (!isDropAimMode(player))',
'            {',
'                cdDropPassLog(airGroup, "SKIP: player bomb-aim mode is no longer DROP WHEN I DROP (on " + BAM_getPlayerBombAimMode_string(player) + ")");',
'                continue;',
'            }')

# E16: EscortPosWaypoint orders param (pre-open)
Splice 'EscortPosWaypoint\(player, airGroup, leader, AiAirWayPointType\.FOLLOW, -3, 15, true, CoverAGOrders\.drop\);' $null @('            Tuple<AiAirWayPoint, AiAirWayPoint, double> fx = EscortPosWaypoint(player, airGroup, leader, AiAirWayPointType.FOLLOW, -3, 15, true, CoverAGOrders.strict);')

# E17: ordersAreDrop local in listPositionCurrentCoverAircraft
Splice 'bool ordersAreDrop = \(coverAircraftAirGroupsOrders\.ContainsKey\(airGroup\) && coverAircraftAirGroupsOrders\[airGroup\] == CoverAGOrders\.drop\);' $null @()

# E18: remove !ordersAreDrop from [[NO TGT]] condition
Splice '&& isBomberArmed\(airGroup\) && !ordersAreDrop && !ordersAreHoldFire\)' $null @('                else if ((action.Contains("FOLLOW") || action.Contains("NORMFLY")) && BAM_isNearestEnemy(player) && isBomberArmed(airGroup) && !ordersAreHoldFire)')

# E19: commented DROP-WHEN-I-DROP label line
Splice '//if \(ordersAreDrop\) msg \+= " \[\[\[DROP-WHEN-I-DROP\]\]\]";' $null @()

# E20: ordersHoldFire - drop removed
Splice 'return orders == CoverAGOrders\.reserve \|\| orders == CoverAGOrders\.strict \|\| orders == CoverAGOrders\.loiter \|\| orders == CoverAGOrders\.drop;' $null @('        return orders == CoverAGOrders.reserve || orders == CoverAGOrders.strict || orders == CoverAGOrders.loiter;')

# E21: stale comment above ordersEngageAir
Splice '//<cdrop holds fire in the air too - a bombing run formation shouldn''t scatter chasing fighters\.' $null @()

# E22a: ordersBombGround comment
Splice '//<cdrop returns FALSE - that is what makes them sit on their bombs and wait for the leader instead' '//by dropBombsNow_airGroup\(\) once we detect the leader actually dropping\.' @(
'    //When the player''s bomb-aim mode is DROP WHEN I DROP, keepAircraftOnTask_recurs() suppresses',
'    //independent bombing separately (see the bam check there) - the release is driven by',
'    //dropBombsNow_airGroup() once we detect the leader actually dropping.')
# E22b: ordersBombGround return - drop removed
Splice 'return orders == CoverAGOrders\.attack \|\| orders == CoverAGOrders\.normal \|\| orders == CoverAGOrders\.drop;' $null @('        return orders == CoverAGOrders.attack || orders == CoverAGOrders.normal;')

# E23a: command-list comment
Splice '//<cattack, <creserve, <cloiter, <cdrop\)\.  Parses any airgroup' $null @('    //<cattack, <creserve, <cloiter).  Parses any airgroup #''s out of the command - no #''s at')
# E23b: skipHoldFire doc comment
Splice '//skipHoldFire is only used entering <cdrop: a bare all-groups <cdrop' '//naming them explicitly \("<cdrop 2"\) still pulls them in\.' @()
# E23c: signature - remove skipHoldFire param
Splice 'public List<AiAirGroup> setCoverAircraftAirGroupsOrders\(Player player, string msg, CoverAGOrders order, string orderDescription, bool skipHoldFire = false\)' $null @('    public List<AiAirGroup> setCoverAircraftAirGroupsOrders(Player player, string msg, CoverAGOrders order, string orderDescription)')
# E23d: skippedHoldFire locals
Splice 'int skippedHoldFire = 0; // counter for groups skipped due to hold-fire order' 'string skippedIndxs = ""; // squadron numbers of those skipped, for the warning message below' @()
# E23e: rejoinedDropMode comment + local
Splice '//Set when a <cnormal RE-JOINS groups to drop mode' 'bool rejoinedDropMode = false;' @()
# E23f: skipHoldFire block inside loop
Splice '//<cdrop entering with a bare all-groups order skips hold-fire groups' '^                    \}$' @()
# E23g: <cnormal re-join block
Splice '//<cnormal releasing a hold-fire group re-joins it to DROP WHEN I DROP' '^                    \}$' @()
# E23h: skipped-hold-fire warning block
Splice '// WARNING: Groups were skipped due to hold-fire orders' '^            \}$' @()
# E23i: <cdrop clear-label block
Splice '//<cdrop - if the player has just overridden EVERYONE out of DROP WHEN I DROP' '^            \}$' @()

# E24: new-sortie reset - coverOrdersBeforeDrop lines
Splice 'if \(coverOrdersBeforeDrop\.ContainsKey\(player\)\) wasNonDefault = true; //anything in snapshot' $null @()
Splice 'coverOrdersBeforeDrop\.Remove\(player\); //clear the snapshot' $null @()

# E25: pre-open gate -> aim-mode test
Splice 'if \(cdDropPreOpenBays && orders == CoverAGOrders\.drop && !coverACContinuingFinalRun &&' $null @('                if (cdDropPreOpenBays && isDropAimMode(player) && !coverACContinuingFinalRun &&')

# E26a: <cdrop formation comment in keepAircraftOnTask_recurs
Splice '//<cdrop flies the same tight formation' '//ordersHoldFire/ordersBombGround keep them quiet and holding their bombs until then\.' @()
# E26b: strict||drop -> strict
Splice 'if \(orders == CoverAGOrders\.strict \|\| orders == CoverAGOrders\.drop\)' $null @('                if (orders == CoverAGOrders.strict)')

# E27a: DIAGNOSTIC comment
Splice '//DIAGNOSTIC - did the <cstrict exact-match override fire this cycle\?  <cstrict AND <cdrop both run' $null @('            //DIAGNOSTIC - did the <cstrict exact-match override fire this cycle?  <cstrict runs')
# E27b: drop-groups comment block
Splice '//Drop groups join the exact-speed-match gate too' '//are already roughly in position\.' @()
# E27c: inDropFormation local
Splice 'bool inDropFormation = coverAircraftAirGroupsOrders\.ContainsKey\(airGroup\) && coverAircraftAirGroupsOrders\[airGroup\] == CoverAGOrders\.drop;' $null @()
# E27d: condition
Splice 'if \(\(isInStrictFormation\(airGroup\) \|\| inDropFormation\) && frontBackDist_m < strictSpeedMatchDistance_m\)' $null @('            if (isInStrictFormation(airGroup) && frontBackDist_m < strictSpeedMatchDistance_m)')

# E28: insert isDropAimMode helper before BAM_isPoint
$helper = @(
'    //2026-10-11 (plan-2026-10-11-2333): with CoverAGOrders.drop removed, the question',
'    //"does this player have DROP WHEN I DROP armed?" is answered by the bomb-aim mode alone.',
'    //This is the single test the <cdrop machinery (coverDropWatch, coverDropReleasePass,',
'    //the pre-open bays, the onDrop diagnostic) uses.',
'    public bool isDropAimMode(Player player)',
'    {',
'        if (player == null) return false;',
'        BAM_BombAimMode bam = BAM_getplayerBombAimMode_enum(player);',
'        return (bam == BAM_BombAimMode.Drop_When_I_Drop || bam == BAM_BombAimMode.Drop_When_I_Drop_Nearest_Enemy);',
'    }',
'')
$bp = Find-Line 'public bool BAM_isPoint\(Player player\)'
Splice 'public bool BAM_isPoint\(Player player\)' 'public bool BAM_isPoint\(Player player\)' (@($helper) + @($script:lines[$bp]))

# E29: comment at <cdrop chat command
Splice '//what is actually happening\.  BAM_enterDropMode does the snapshot \+ orders \+ watcher\.' $null @('            //what is actually happening.  BAM_enterDropMode arms the watcher and refreshes the formation.')

[System.IO.File]::WriteAllLines($path, $script:lines, $enc)
Write-Output ("WROTE {0} lines" -f $script:lines.Count)

$text = [System.IO.File]::ReadAllText($path)
foreach ($pat in @('CoverAGOrders\.drop', 'dropFormationShiftFactor', 'coverOrdersBeforeDrop', 'rejoinedDropMode', 'skipHoldFire', 'ordersAreDrop', 'inDropFormation', 'BAM_leaveDropMode')) {
    $m = [regex]::Matches($text, $pat)
    Write-Output ("CHECK {0}: {1} matches" -f $pat, $m.Count)
}

