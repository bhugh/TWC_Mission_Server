/*   CLoD AI AIRCRAFT / WAYPOINT / PARAMETER BEHAVIOUR NOTES

 *   This file contains NO CODE - it is a block comment only, so it compiles to nothing and costs
 *   nothing at runtime.  It exists purely to record how CLoD's AI aircraft, waypoints and aircraft
 *   parameters actually behave, because none of this is documented by 1C/TF anywhere we could find.
 *
 *   These findings were established two ways:
 *       - empirically, by in-game / mission-builder testing
 *       - by reflecting over the shipped assemblies
 *         (maddox.dll / core.dll / gameWorld.dll / part.dll in <install>\parts\core)
 *
 *   Every claim below is tagged so a later reader knows how much to trust it:
 *       [GAME]  = confirmed by in-game / mission-builder testing
 *       [IL]    = confirmed by reflecting over the CLoD assemblies
 *       [OBS]   = observed, but the mechanism is not fully understood
 *       [??]    = suspected / untested - treat as a hypothesis, not a fact
 *
 *   If you change anything related to waypoints or bomb release, read this first.  Half the battle
 *   with CLoD programming is working out exactly how the in-game actors work - or don't.
 *
 *
 *   ================================================================================================
 *   1.  MAKING AI DROP BOMBS
 *   ================================================================================================
 *
 *   - Setting the airgroup task to AiAirGroupTask.ATTACK_GROUND on its own does NOT make a bomber
 *     release its bombs.                                                          [GAME]
 *
 *   - The working recipe is a WAYPOINT PAIR:  a NORMFLY (or similar) waypoint, then a
 *     GATTACK_POINT waypoint shortly after it.                                     [GAME]
 *
 *   - The AI only evaluates its release geometry when the GATTACK_POINT becomes the CURRENT
 *     waypoint - it cannot "see past" the next point.                              [GAME]
 *
 *   - IMMEDIATE-DROP rule:  when the GATTACK_POINT becomes current, the AI works out where it
 *     *should* have released in order to hit the given point.  If that release point is already
 *     BEHIND the aircraft, it drops instantly.                                     [GAME]
 *
 *   - PROPER-TARGETING rule:  if that release point is still AHEAD of the previous waypoint, the
 *     AI waits, releases at the correct moment, and hits the given point fairly accurately.  [GAME]
 *
 *   - So the GAP between the last NORMFLY and the GATTACK_POINT selects the mode:
 *         short gap   ->   "release NOW, wherever I am"      (this is what <cdrop needs)
 *         long gap    ->   "fly on and bomb that point properly"
 *     Confirmed to produce an immediate drop at gaps of ~10m, ~30-40m and ~100m.       [GAME]
 *
 *   - Verified working for all aircraft types / altitudes / speeds tested, including bombing at
 *     treetop height.                                                                [GAME]
 *
 *   - The gap at which it flips from "drop now" to "bomb that point properly" is altitude- and
 *     speed-dependent, and the two regimes get closer together at low altitude.         [OBS]
 *
 *
 *   WAYPOINT SPACING - CAREFUL:
 *
 *   - Waypoints placed too close together can be SKIPPED ENTIRELY.  The game tick is about 30ms
 *     but slows under load.  At ~120 m/s a 30ms tick covers ~3.6m, so a 10m gap is only ~3 ticks -
 *     a single slow tick can straddle BOTH points and lose one.                        [GAME]
 *   - Keep at least ~30-40m between consecutive waypoints for anything that matters.   [GAME]
 *
 *
 *
 *   ================================================================================================
 *   1b.  GATTACK_POINT vs GATTACK_TARG, and the TYPE / PASSES options
 *   ================================================================================================
 *
 *   - In the FMB waypoint menu, GATTACK_POINT GREYS OUT both TYPE and PASSES - they cannot be
 *     selected, and the engine ignores them.  GATTACK_TARG makes them available.              [GAME]
 *     => In the FMB they cannot be set on a point waypoint, and using GAttackPasses.ALL_OUT to
 *        force a full release is a NO-OP on a point waypoint.  SEE THE CAVEAT BELOW: the proven
 *        ground-attack path nevertheless writes AUTO/LEVEL on its point waypoints, and the <cdrop
 *        path now does the same (Step C) - whether that is what fixes the 1-bomb latch is under
 *        test, not established.
 *   - AiAirWayPoint.GAttackPasses = {AUTO=0, _1=1, _2=2, _3=3, _4=4, ALL_OUT=5}                [IL]
 *     AiAirWayPoint.GAttackType   = {AUTO=0, LEVEL=1, DIVE=2, TOP_MAST=3, SHALLOW_DIVE=4}    [IL]
 *     Both are public FIELDS on AiAirWayPoint; only settable usefully on GATTACK_TARG.         [IL]
 *     CAVEAT 2026/10 - SUPERSEDED, see 1e.  For a while we copied BomberPosWaypoint's
 *     GAttackPasses=AUTO / GAttackType=LEVEL onto the <cdrop release waypoint on the theory that the
 *     missing fields caused the one-salvo latch.  The reference test settles it: it sets NEITHER
 *     field and still dumps every bomber's whole load, so those fields were never the cause and
 *     Step C7 has REVERTED them.  TYPE/PASSES remain irrelevant to a point waypoint, as first noted.
 *   - RELEASE IS ALL-OR-NOTHING: tested with NORMFLY then successive GATTACK_POINTs, every
 *     aircraft with bombs dropped its WHOLE load on the first attack point - Weltingtons,
 *     Blenheims, all of them - regardless of how many passes were set, and regardless of whether
 *     the waypoint was GATTACK_POINT or GATTACK_TARG.                                       [GAME]
 *   - So a "they only dropped some of their bombs" report is NOT a partial release.  It is
 *     almost always BombSpacing - see the salvo/stick note below.                              [GAME]
 *   - What PASSES actually controls is the number of ATTACK PASSES, not how many bombs:
 *       * heavy bombers  -> one bombing pass, then they fly on, whatever the pass count       [GAME]
 *       * strafers (Beaufighter, Ju-88C, Hurricane) -> bomb pass first, then STRAFE for the
 *         remaining passes.  The pass count genuinely works.                                  [GAME]
 *   - ALL_OUT: not established.  Possibly "keep attacking until the object is destroyed".     [??]
 *   - Attack TYPE only works on aircraft that can do it - only Ju-87s will dive.  Give an
 *     aircraft a type it cannot perform and it just level bombs instead.                      [GAME]
 *   - GATTACK_TARG target must be a DIFFERENT army (neutral 0 or the enemy).  If the target has
 *     the same army as the attacker, the engine ignores the whole attack.                      [GAME]
 *
 *   - FMB CAVEAT: making a mission and playing it straight away on the same machine sometimes
 *     behaves differently from a real multiplayer server.  Worth re-confirming anything crucial
 *     (BombSpacing was checked on the server and behaved the same).                           [GAME]
 *
 *   - 2026/10 LOG-B FINDING - the all-or-nothing claim above was measured on the FMB/mission path
 *     (waypoints the .mis already carried).  It does NOT necessarily transfer to the runtime case:
 *     when CLoD hand-builds the immediate pair and SetWay()s it MID-FLIGHT, at the group's own
 *     position, with a ~35m gap, a 3xWellington group released EXACTLY ONE bomb per a/c and then
 *     LATCHED - 54 -> 51, and 51 forever for the rest of the mission, through 21 re-issues of a
 *     fresh plan.  The bays closed around the 51 remaining bombs and never reopened.  The release
 *     was adopted (the plan is visible on the group), so it is not a rejected waypoint - it is the
 *     engine treating one immediate point-attack as one salvo per a/c and then marking the attack
 *     done.  H1: the GATTACK_POINT immediate pair is a no-repeat salvo.  Under test (see the
 *     cdDropPlanTestMode enum in Genghis-Class-CoverMission.cs): GATTACK_TARG with
 *     GAttackType=AUTO + GAttackPasses=ALL_OUT, at the same point and also far ahead, to see
 *     whether those settings - which are greyed out in FMB for GATTACK_POINT but are settable on
 *     GATTACK_TARG, and are exactly what the proven ground-attack path always sets - force a full
 *     stick instead.  A runtime-spawned airgroup also has NO BombSpacing (see 1d), which is the
 *     classic signature of a single-bomb-per-aircraft salvo; so 1d and this finding may be the same
 *     defect seen from two angles.                                                            [GAME]
 *   - 2026/10 STEP C (applied - and it did NOT fix this on its own): dropBombsNow_airGroup now
 *     writes GAttackPasses=AUTO + GAttackType=LEVEL on its GATTACK_POINT waypoint, exactly as the
 *     proven path does, so TYPE/PASSES are no longer a difference between <cdrop and the
 *     Knickebein / flare-point path.  The 2026-10-02 logs still show a Wellington group going
 *     54 -> 51 (ONE salvo) and latching, and one run with NO release at all.                    [GAME]
 *   - 2026/10 STEP C2 (applied, then SUPERSEDED - see 1e): the theory at this point was that what was
 *     left was the RELEASE GEOMETRY, and the release was changed to a 600m run-in.  The reference
 *     test later showed the geometry is NOT the variable (10m and 50m behave identically, and 600m
 *     did not fix it either), so C2's geometry is no longer treated as the fix.
 *   - 2026/10 STEPS C7 + C8 (applied) - the real answer, and it is the doors:  dropBombsNow_airGroup
 *     now reproduces the reference test's plan exactly (NORMFLY at its own position, GATTACK_POINT
 *     50m behind, long trailing NORMFLY) and, crucially, sets NEITHER GAttackPasses/GAttackType and
 *     no longer calls setTask - so Step C's TYPE/PASSES change has been reverted.  The doors are held
 *     open beforehand by cdDropPreOpenBays, and coverDropReleasePass() then WAITS (no timeout) until
 *     the group's A_BombBayDoor reads open before releasing.  Full details in 1e.                [CODE]
 *
 *
 *   ================================================================================================
 *   1c.  GROUND-TARGET SELECTION (GATTACK_TARG) - a shared-target trap
 *   ================================================================================================
 *
 *   - The aircraft does NOT attack the object you actually picked.  It attacks whatever ground
 *     actor / stationary it happens to find nearby.                                          [GAME]
 *   - ALL aircraft targeting that area (~500m radius) converge on the SAME object.  When it dies
 *     they all move on to another in the same area, and so on.                                 [GAME]
 *   - The failure mode: if that object cannot be killed - a bomb crater, a large object that
 *     cannot be strafed down, or something inside a building - then EVERY aircraft in that area
 *     grinds away at that one unkillable object indefinitely.                                 [GAME]
 *     CoverCalcs.areCratersBuildingsFactoriesNear() exists to steer them away from exactly this,
 *     and a lot of BomberPosWaypoint() exists to pick targets that are actually killable.
 *
 *
 *   ================================================================================================
 *   1d.  BOMB SPACING - why a Wellington could only damage 10% of an airfield
 *   ================================================================================================
 *
 *   - "BombSpacing <metres>" is a key in the AIRGROUP section of a mission sectionfile, alongside
 *     Class / Formation / CallSign / Fuel / Weapons.  It is what separates dropping the bombs as a
 *     spaced-out STICK across the target from dumping the whole load as one tight SALVO.     [GAME]
 *   - Every .mis in this repo carries "BombSpacing 20"; no .cs file ever wrote it, so cover
 *     aircraft spawned at runtime had no spacing and salved instead.  Measured: a full squadron of
 *     Wellingtons on Shoreham took out ~10% of the airfield salving, vs the usual 50-60%.      [GAME]
 *   - Stock spacing is 20 for heavy bombers, dive bombers and fighter-bombers alike.          [GAME]
 *   - Set at spawn time, so it applies for that aircraft's whole life.  There is no way to
 *     change it once the aircraft exists, and no runtime control for it.                        [GAME]
 *
 *
 *   ================================================================================================
 *   1e.  THE PROVEN FULL-LOAD RELEASE RECIPE  (the "reliable instant bomb drop")
 *   ================================================================================================
 *
 *   - REFERENCE TEST, kept in the repo:  Genghis\Reliable-instant-bomb-drop\  (bombdrop_test20-
 *     return.mis + bombdrop_test20-return.cs).  A full-mission-builder mission built by the mission
 *     owner, and the ONLY configuration found so far in which EVERY bomber type dumps its WHOLE load
 *     on demand.  Types covered: Blenheim MkI/MkIV, Ju-88C/A, Wellington MkIc, He-111P, BR-20M,
 *     Do-17Z.                                                                                 [GAME]
 *
 *   - THE SEQUENCE, exactly as the test does it:
 *       #1  Give the group a GATTACK_POINT a LONG way ahead (the .mis used one ~41km out).
 *       #2  ALL types reliably OPEN their bomb-bay doors once they are ~10km from that point.
 *       #3  WAIT until the doors are fully open.
 *       #4  THEN issue a fresh flight plan:
 *               waypoint 1:  NORMFLY        at the group's CURRENT position (zero length)
 *               waypoint 2:  GATTACK_POINT  10-50m BEHIND it
 *               waypoint 3:  NORMFLY        ~10-20km further on, plus a few more like it
 *           Every bomber then dropped its full load THE MOMENT waypoint 2 was reached, all squadrons
 *           simultaneously.  10m and 50m behind were both tested and behaved the same.        [GAME]
 *
 *   - WHY it works, and why ours did not - this is the whole answer to the "one salvo then latch"
 *     problem, and it retires several earlier theories:
 *       * The RELEASE GEOMETRY IS NOT THE VARIABLE.  We have now tried 0m, 35m, 600m and 2000m of
 *         run-in; the reference test found 10m and 50m identical.  Distance does not matter, which
 *         retires Steps C2 and C6 as "the fix" (their geometry was not wrong, just irrelevant).
 *       * TYPE / PASSES ARE NOT THE VARIABLE.  The reference test sets NEITHER GAttackPasses nor
 *         GAttackType, and never calls setTask - yet it works on every type.  Step C's attempt to
 *         copy BomberPosWaypoint's AUTO/LEVEL onto the release waypoint has therefore been REVERTED
 *         (Step C7).  §1b's "TYPE/PASSES are a no-op on a point waypoint" was not wrong - it was
 *         simply never the problem.
 *       * THE DOORS ARE THE VARIABLE.  Our plan arrives while the bays are still shut; the engine
 *         consumes the attack waypoint, opens the doors 3-15s later (Wellington ~3-6s, Ju-88
 *         ~10-15s, measured from the DROPTRACE bay= column), and by then the release point is BEHIND
 *         the aircraft - an "already passed" solution, so it lets go once and marks the attack done.
 *         That is precisely the 54 -> 51 in the 2026-10-02 logs, and the one run with no release at
 *         all (its doors never finished opening before the plan was replaced).                [GAME]
 *
 *   - SO, IN CODE:  cdDropPreOpenBays (ON) keeps an attack waypoint live ~coverDropPreOpen_m (10km)
 *     ahead of each armed group, re-issued every cycle so it is never reached, and
 *     coverDropReleasePass() WAITS - with no timeout, as the mission owner specified - until that
 *     group's A_BombBayDoor actually reads open (coverDropBayOpenThreshold) before issuing the
 *     release.  A group whose door parameter never moves is released anyway after
 *     coverDropBayWaitFallback_s, purely as a deadlock escape.                              [CODE]
 *
 *   - STILL OPEN: keeping an attack waypoint live puts the group into bomb-run attitude rather than
 *     tight formation.  That is the price of the only mechanism known to work; worth watching for a
 *     test session.                                                                      [CODE]
 *
 *   ================================================================================================
 *   1f.  TASK .RETURN (RTB) - WHY A GROUP WILL NOT OPEN ITS BAYS OR RELEASE
 *   ================================================================================================
 *
 *   - OBSERVED: once an airgroup is in task .RETURN it will not open its bomb bays and will not drop,
 *     even when we hand it a GATTACK_POINT plan.  Sometimes the correct task shows briefly and then
 *     reverts to .RETURN.  This has been seen with GATTACK_POINT and GATTACK_AREA plans, and also
 *     with the plain <cdrop release.                                                      [GAME]
 *
 *   - SO IT MATTERS TWICE OVER for <cdrop:  a group that has gone RTB looks, from the outside,
 *     exactly like a group that is ignoring us, and it can never be recovered by another waypoint -
 *     EscortMakeLand/coverACContinuingFinalRun comment already notes that "once in that mode you
 *     can't get them back out for love nor money".
 *
 *   - WHY THEY DO IT IS STILL AN OPEN MYSTERY.  The candidates we can think of, none of them yet
 *     proven:  low fuel;  damage;  running out of waypoints (no current waypoint);  a formation that
 *     has split up too far;  abrupt direction changes leaving them "upset".  It also happens when
 *     none of those should apply.  Any of these is easy to CONFIRM or KILL with the log, which is
 *     why cdRtbProbe() exists now.                                                       [GAME]
 *
 *   - THE PROBE (ON_TESTSERVER):  keepAircraftOnTask_recurs() calls cdRtbProbe() every cycle, which
 *     logs any cover group sitting in task .RETURN - name, waypoint count, current waypoint index,
 *     bombs left, FUEL, distance to the leader, and whether WE released it (i.e. whether the switch
 *     was ours via EscortMakeLand at >42km, or the engine did it to itself).  One line per group
 *     per 30s.  That last field is the important one: "releasedByUs=False" means the engine
 *     switched them and we have a genuine mystery to chase.                                [CODE]
 *
 *   - PREVENTION, meanwhile:  the code now never issues a short or empty flight plan.  An airgroup
 *     with no waypoints switches itself to RTB, so both the release plan and the pre-open decoy plan
 *     carry a long tail of NORMFLY points.                                               [CODE]
 *
 *   ================================================================================================
 *   2.  THE ~16 SECOND WAYPOINT OVERWRITE  (a silent-failure trap)
 *   ================================================================================================
 *
 *   - keepAircraftOnTask_recurs() runs per airgroup on a ~16s cycle (base 16.2354s, multiplied by
 *     1.5x-4x when the mission is under CPU load) and UNCONDITIONALLY rewrites that airgroup's
 *     flight plan via EscortUpdateWaypoints().                                     [IL/code]
 *
 *   - So ANY flight plan set by hand is thrown away within ~16 seconds UNLESS you return before
 *     EscortUpdateWaypoints() is reached.  The ground-attack path already does exactly this - it
 *     returns immediately after BomberUpdateWaypoints().  Any new feature that sets a flight plan
 *     MUST do the same, or it will appear to work briefly and then silently half-work.  [IL/code]
 *
 *   - Airgroup TASKS get overwritten by the same loop, which is why long-lived tasks (e.g. LANDING)
 *     are re-asserted repeatedly over time - see EscortMakeLand.
 *
 *
 *   ================================================================================================
 *   3.  DETECTING THAT A PLAYER HAS DROPPED BOMBS
 *   ================================================================================================
 *
 *   - There is NO bomb-release event.  The only bomb callback on AMission is OnBombExplosion(),
 *     which fires on EXPLOSION, not release - so it is tens of seconds late when bombing from
 *     altitude, and never fires at all for duds or water hits.  Do not use it to trigger anything
 *     time-critical.                                                              [IL]
 *
 *   - part.ParameterTypes.S_BombReserve (=109), read via
 *     AiAircraft.getParameter(S_BombReserve, i), is a live PER-BOMB-SLOT state:
 *         1 = still loaded, 0 = already dropped
 *     Summing the slots gives a bomb count that decrements on EVERY salvo - see
 *     CoverCalcs.bombCount().                                                       [GAME]
 *
 *   - The slot indices are NOT contiguous.  An aircraft may show 0s, then a 1, then more 0s, then
 *     another 1 - it depends on how that particular aircraft's bombs are racked.  Just sum them;
 *     never assume a solid block of 1s at the start.                                 [GAME]
 *
 *   - Largest bomb load found so far is 32, so scanning slots 0..49 is safe.            [GAME]
 *
 *   - AiAirGroup.hasBombs() is a cheap bool and a good failsafe, but it only flips when the
 *     aircraft is COMPLETELY empty - i.e. it catches only the LAST salvo.              [GAME]
 *
 *   - Polling at 1-2 Hz is plenty.  Against a 1000-2000m target, a 1Hz poll puts the release
 *     within ~100-120m of the intended line, comfortably inside the target.            [GAME]
 *
 *   - The UNUSED S_BombReserve subtypes return 0; they do NOT throw.  So CoverCalcs.bombCount()
 *     never actually takes an exception, its try/catch is belt-and-braces only, the 0..49 scan is
 *     cheap, and - most importantly - a CHANGE in the count is genuine signal rather than an
 *     artefact of a swallowed exception.  1-2 Hz polling is therefore comfortably affordable.  [GAME]
 *
 *   *** THE BIG LIMITATION: UNLIMITED AMMO ***
 *
 *   - If the server (or an offline mission) is set to UNLIMITED AMMO, S_BombReserve never changes,
 *     and hasBombs() cannot be relied on either.  So bomb-drop detection does not work AT ALL in
 *     that mode, and the automatic <cdrop cannot fire.                                        [GAME]
 *     This is why the manual <cdropnow / <cbomb command exists - it is the only way to run a
 *     drop formation on an unlimited-ammo server.
 *
 *   - *** SERVER MAINTAINERS: DO NOT SET THIS SERVER TO UNLIMITED AMMO. ***  <cdrop's whole design
 *     depends on noticing the LEADER's bomb count fall, and on an unlimited-ammo server it can never
 *     happen, so the automatic release simply never fires and every squadron silently holds its
 *     bombs.  This is a server configuration requirement, not a player setting, so the warning to
 *     players has been REMOVED from the chat messages and from <chelp (2026/10) - the live server is
 *     never in that mode, so telling players about it was only noise.  If you ever need unlimited
 *     ammo here, expect <cdrop to be non-functional and use <cdropnow instead.          [CODE]
 *
 *   - hasBombs() is separately SUSPECT: it has been seen reporting "Has bombs" for an aircraft
 *     that had already released its whole load.  Treat it as unreliable for anything that
 *     matters; prefer CoverCalcs.bombCount().                                                  [GAME]

 *  - The bomb-drop detection system doesn't work if the server (or offline mission) is set to unlimited ammo.  In that case the S_BombReserve parameter doesn't change.  I don't think .hasBombs() is reliable either.  So this function won't work really at all in unlimited ammo mode.

 *   ================================================================================================
 *   4.  part.ParameterTypes - NAMING CONVENTION
 *   ================================================================================================
 *
 *       S_  = state, readable via getParameter
 *              S_BombReserve = 109        <- the bomb count, see section 3
 *              S_FuelReserve = 98
 *              S_GunReserve  = 107
 *              S_GunClipReserve = 108
 *              S_Bombenabwurfgerat = 112   (German: "bomb release mechanism")
 *       C_  = control (the C_ prefix says CONTROL - NOT proven writable)
 *              C_BombBayDoor = 40, C_BombSight
 *     NB "writable" was assumed from the C_ prefix.  IL-verified 2026/10: AiAircraft's public
 *     instance methods are getParameter(ParameterTypes,Int32), RearmPlane, RefuelPlane, hitLimb,
 *     cutLimb, hitNamed, hitSelfNamed, SayToGroup, ...  There is NO setParameter / write-parameter
 *     method at all.  So C_-prefixed parameters are NOT actually writable through the API as
 *     exposed - the "writable" claim must be treated as unverified until a write path is found.
 *     (This kills the Step-B4 "pre-open the bomb bays via C_BombBayDoor" plan; see section 9b.)  [IL]
 *       A_  = animated / actual position
 *              A_BombBayDoor = 73
 *       I_  = instrument readout
 *              I_BombSight = 186
 *       M_  = damage / misc
 *              M_Health = 7, M_NamedDamage = 5
 *       Z_  = derived values
 *              Z_AltitudeAGL = 86, Z_AltitudeMSL = 87, Z_VelocityIAS = 88, Z_VelocityTAS = 89
 *       M_Reserved002 .. M_Reserved01F  (= 116..145) are unused by the sim - possible scratch
 *       space for our own use.                                                            [IL]
 *
 *   - getParameter(ParameterTypes, int subtype) is declared on AiAircraft ONLY - not on AiCart,
 *     and not on AiActor.  Cast to AiAircraft first.                                   [IL]
 *
 *
 *   ================================================================================================
 *   5.  TYPE / API GOTCHAS
 *   ================================================================================================
 *
 *   - AiAirGroup derives directly from object and implements the AiActor and AiGroup INTERFACES.
 *     It is NOT a subclass of AiActor - which is why casts like (airGroup as AiActor) are needed,
 *     and why some seemingly-inherited members are really interface members.            [IL]
 *
 *   - AiActor.Army() (interface method) and AiAirGroup.getArmy() both exist, and both return
 *     Int32.  Prefer getArmy(); the codebase idiom for the opposite army is 3 - army.  [IL]
 *
 *   - The ternary (army == 1) ? 2 : 1 is equivalent to 3 - army for armies 1 and 2, but it
 *     silently returns army 1 for army 0 (neutral).                                     [IL]
 *
 *   - Player.Place() returns an AiActor, not an AiAircraft - cast before calling anything
 *     aircraft-specific (getParameter, AirGroup(), etc).
 *
 *   - hasBombs() lives on AiAirGroup, not on AiActor/AiCart, so the player's armed check must be
 *     (player.Place() as AiAircraft).AirGroup().hasBombs().
 *
 *   - GamePlay.gpAirGroups(army) can return null - always null-check before iterating.
 *
 *
 *   - AiAirGroupTask values: UNKNOWN, FLY_WAYPOINT, ATTACH, DEFENDING, ATTACK_AIR, ATTACK_GROUND,
 *     PURSUIT, TAKEOFF, RETURN, LANDING, DO_NOTHING.
 *     ATTACH / ATTACK_AIR / DEFENDING / PURSUIT expect a meaningful target - passing null risks an
 *     engine-side fault.                                                             [IL]
 *
 *   - AiAirWayPoint is built as  new AiAirWayPoint(ref Point3d pos, double speed)  and then has its
 *     .Action / .Target / .GAttackPasses / .GAttackType properties assigned.             [IL]
 *
 *   - GATTACK_POINT is for AERIAL BOMBING only.  Genuine ground attack / strafing / dive bombing
 *     needs GATTACK_TARG instead.                                                       [OBS]
 *
 *
 *   ================================================================================================
 *   6.  STILL UNKNOWN - WORTH TESTING
 *   ================================================================================================
 *
 *   - Does an immediate drop release ONE salvo, or the ENTIRE remaining load?
 *       ANSWERED - the ENTIRE load.  See section 1b: release is all-or-nothing on the first
 *       attack point, whatever the pass count.                                               [GAME]
 *
 *   - (ANSWERED: unused S_BombReserve subtypes return 0, they do not throw - see section 3.  The
 *     exception-cost worry was unfounded and bombCount() is cheap.)
 *   - (ANSWERED: GAttackPasses / GAttackType DO change behaviour, but only on a GATTACK_TARG
 *     waypoint - GATTACK_POINT greys them out and ignores them.  See section 1b.)
 *
 *   - maddox.GP.Vector3d.angle() exists, but we could not confirm whether it returns degrees or
 *     radians.  Use CoverCalcs.roughlySameDirection() instead - that one is unambiguous.       [??]
 *
 *   - A_BombBayDoor (=73) may give a few seconds of advance warning of a drop, on aircraft that
 *     have bomb bay doors.  Untested.                                                         [??]
 *
 *
 *   ================================================================================================
 *   7.  WAYPOINT SPEED - aircraft only deliver ~98% of the speed you command
 *   ================================================================================================
 *
 *   - Measured on the server by logging commanded waypoint speed, the airgroup's actual ground
 *     speed, and the player's: commanded 70.4 vs actual 68.5 (player 68) at 1300m, and commanded
 *     76.5 vs actual 75.1 (player 75.7) at 16000ft.  Consistently ~1.5-1.9 m/s / ~2% SHORT of the
 *     commanded figure, at both altitudes.                                                    [GAME]
 *   - This is NOT an IAS/TAS units problem.  At 16000ft TAS/IAS is ~1.26, so a units mismatch
 *     would show actual ~20 m/s HIGHER than commanded, not 1.5 lower.  Checked and ruled out.
 *   - Probably the aircraft is always in transient toward a commanded value that is re-derived
 *     every ~16s, so actual speed lags a moving target.                                       [OBS]
 *   - CONSEQUENCE: any "catch-up" speed command must exceed this ~2% shortfall or the formation
 *     can never close a gap - commanding exactly the leader's speed means zero closure.  That is
 *     why CoverMission's coverFormationSpeedBias is 1.06 rather than 1.0.
 *   - Beware: a later "convergence" block that overrides an earlier catch-up speed table with a
 *     target of 0.9999x the leader's speed silently disables the whole table in its range.  In
 *     CoverMission that block (frontBackDist < 400m) was the reason the formation sat 100m+ behind
 *     forever, with a measured closure rate of +0.5 to -0.6 m/s.
 *
 *
 *   ================================================================================================
 *   8.  THE "SPEED AWAY AFTER DROPPING" BUG  -  and per-airgroup speed calibration
 *   ================================================================================================
 *
 *   - THE SYMPTOM: bombers held formation correctly, then the moment they released they accelerated
 *     to 1-3km ahead and the player could not catch them.  Reproduced with 3 different bomber types.
 *                                                                                             [GAME]
 *
 *   - THE CAUSE is a chain of two facts, and NEITHER of them is about speed:
 *       1. In keepAircraftOnTask_recurs() the default for aawpt was .ESCORT, and the only line that
 *          overrode it for bombers was  if (heavyBomber && isBomberArmed(airGroup)).
 *          isBomberArmed() goes FALSE the instant they release, so an EMPTY bomber fell back through
 *          to .ESCORT.
 *       2. In calcCoverSpeedToMatchMain(),  if (aawpt == .ESCORT) pacePlayer = false;  That skips
 *          the normal braking bands entirely and uses the fighter-escort numbers instead:
 *          1.5x the leader's speed when behind, and 1.3x - WITH NO BRAKING AT ALL - when in front.
 *     At 75 m/s that is +22.5 m/s: 1km in 45 seconds, 3km in about 2 minutes.             [CODE+]
 *   - Separately, the sub-400m convergence override REPLACES whatever the bands decided, so it also
 *     had to be restricted to the not-inFront case: while in front it was commanding >= 1.06x the
 *     leader's speed, i.e. telling groups that were ALREADY ahead to keep accelerating.     [CODE]
 *
 *   - FIX 1: test heavyBomber alone.  Whether they still carry bombs has nothing to do with how they
 *     should fly WITH the leader.
 *   - FIX 2: apply the convergence override only when !inFront, so the braking bands survive.
 *
 *   *** 2026/10 LOG-B FINDINGS - THREE MORE CAUSES OF "DRIFT AHEAD AND SIT", PLUS A HARD GUARD. ***
 *   A 3xWellington run (genghis-cover-log-2026-10-02B.log, leader at 51-56 m/s) showed the group
 *   closing from 3.4km behind, crossing to IN FRONT, then drifting out to 1.5km AHEAD and sitting
 *   there - until the player's own speed rose above ~56 m/s, whereupon the gap finally closed.
 *   pacePlayer was NEVER false (the .ESCORT runaway of the bullet above did not fire), so this is a
 *   different defect, and the log shows three independent causes:
 *   - CAUSE A - the fixed 55 m/s FLOOR in CurrentPosWaypoint() / BomberPosWaypoint().  This is the
 *     FIRST waypoint written and its speed is the one the AI adopts immediately, so for ~10s of every
 *     ~16s cycle the group was forced to >= 55 m/s no matter what the bands asked.  At a 51-52 m/s
 *     leader that is +3..+7 m/s of net separation, which is almost exactly the 444 -> 1485 m drift
 *     measured.  FIX (done): the floor is now leader-relative - 40 m/s absolute, or Min(55,
 *     leader_speed * 1.15) when a leader is known.  It still binds for a slow leader (the in-air
 *     stall it guarded against) but can no longer pin the group ahead of a slow one.
 *   - CAUSE B - the in-front AIM POINT lead.  In EscortPosWaypoint(), when a group is already ahead
 *     (angleTargetToGroup 120..240) the target point is pushed FURTHER ahead by targetVwld2 * 90 or
 *     * 120 - i.e. 90-120 SECONDS of the leader's RAW instantaneous velocity (up to ~10.8km at 90
 *     m/s).  That points a group that is already ahead at a point yet further ahead.  This is the
 *     remembered "aim at where it should be in N seconds" approach - present, but with N far too
 *     large, derived from raw velocity (so it swings on turns), and its SIGN inverted when in front.
 *     DEFERRED by decision: run one test-server session after CAUSE A is fixed and read the log
 *     before choosing the in-front lead sign (aim at the leader vs ~10s behind vs capped-30s-ahead).
 *   - CAUSE C - the per-airgroup ratio sampler (below) folded TRANSIENTS and OUT-OF-BAND samples
 *     into the rolling average, so the estimate ratcheted to its 1.15 clamp through a braking phase
 *     and stayed high into the next acceleration, fighting the bands in both directions; and it
 *     stored the PRE-clamp command as the denominator, biasing the next sample whenever a clamp
 *     fired.  FIX (done, Step A4): samples outside |ag_vel - lastAsked| <= 4 m/s are DISCARDED,
 *     out-of-band ratios are discarded rather than clamped-in, and coverAGSpeedRequested now stores
 *     the FINAL clamped command.  A "discarded this cycle N" counter is on the COVERSPEED line.
 *   - HARD GUARD (done, Step A1): a heavy bomber is now forced OFF .ESCORT (back to .FOLLOW) before
 *     its waypoints are written unless the player explicitly set <cescort.  .ESCORT makes the group
 *     follow the escorted actor's own path, staying above it like a fighter, and it can jettison
 *     bombs to get clear - neither is the tight formation flight a cover bomber should do.  Several
 *     paths could leave aawpt at the .ESCORT default (the fighter default near the top of
 *     keepAircraftOnTask_recurs, and the spawn calls), so the guard sits at the very last moment.
 *
 *   *** WHY ONE GLOBAL bias IS THE WRONG ANSWER ***                                         [CODE]
 *   - The ~2% shortfall in section 7 is an AVERAGE, and it MOVES - with altitude, with aircraft
 *     type, and most of all with bomb load: a bomber that has just dumped its load is aerodynamically
 *     a different aircraft from the same bomber 30 seconds earlier.  No single number is right for
 *     all of them at once, and when it is wrong there is no restoring force, because the loop only
 *     ever compares a group's actual speed against the LEADER's - never against the group's own
 *     last request.  A group that over-delivers simply runs away, with nothing pulling it back.
 *   - CoverMission now learns a ratio PER AIRGROUP: compare the speed we last asked for against what
 *     it actually flew, keep a rolling average (~7 samples), clamp to [0.85,1.15], and divide the
 *     request by it.  The denominator MUST be the previous cycle's command - ag_vel_mps is the
 *     response to THAT command, not to the target being computed this cycle.  Using the current
 *     target as the denominator is a trap: it silently drives the ratio to 1 and disables itself.
 *   - So the bias still creates positive closure, while the ratio makes each group actually achieve
 *     the speed it was asked for.  Bias alone can never fix an over-delivering group.        [CODE]
 *
 *   - Any per-airgroup state added here must also be removed when the group is dropped from
 *     coverAircraftAirGroupsActive - see forgetAirGroup().  Those dictionary keys hold a reference to
 *     the airgroup, so a leak is permanent for the life of the server, not just the mission. [CODE]
 *
 *
 *   ------------------------------------------------------------------------------------------------
 *   8b.  <cdrop - ORDER & SCOPE RULES (how it interacts with <cstrict/<creserve/<cnormal)
 *   ------------------------------------------------------------------------------------------------
 *   <cdrop is a BOMB AIM MODE (it lives on the Tab-4-4-4-4-6 targeting menu) that also sets airgroup
 *   ORDERS, because it has to: .drop means "hold your bombs and wait for the leader".  It is therefore
 *   the one menu mode that collides with the <cnormal/<cstrict/<creserve/<cattack/<cescort/<cloiter
 *   family.  The rules below are what keep the two from fighting:
 *
 *   SCOPE.  There are two ways in, and they mean different things:
 *     - ALL GROUPS  : bare "<cdrop" in chat, or the Tab-4-4-4-4-6 menu (the menu can only express
 *                     all-groups).  Recorded as coverOrdersBeforeDrop[player].Item2 == true.
 *     - SELECTIVE   : "<cdrop 3 6".  Item2 == false.  This is the "only these squadrons join the
 *                     drop" order, exactly like "<creserve 3" but for drop mode.
 *   Each player's pre-drop orders are snapshotted when drop mode is entered, so leaving it puts
 *   everyone back where they were.  The scope rides along with the snapshot because it decides how
 *   the OTHER order commands below are interpreted.
 *
 *   ENTERING (BAM_enterDropMode).  A bare all-groups <cdrop SKIPS groups sitting on a hold-fire order
 *   (<cstrict, <creserve, <cloiter): they were explicitly told to hold fire, and a blanket drop order
 *   must not overrule that.  Name them ("<cdrop 2") and they come in.  Re-arming an existing <cdrop
 *   in chat is also how you widen a selective scope to all-groups - a selective re-arm never narrows
 *   an all-groups scope, since the menu label applies to everyone.
 *
 *   <cstrict / <creserve WHILE IN DROP MODE.  These just overwrite .drop for the named (or all)
 *   groups, as they always have.  That is the documented way to hold squadrons back out of a drop.
 *   The position listing then shows [[[STRICT]]] / [[[RESERVED]]] rather than [[[DROP-WHEN-I-DROP]]],
 *   which is correct - they are NOT waiting to drop any more.
 *
 *   <cnormal AFTER a <cstrict/<creserve WHILE IN DROP MODE.  This is the subtle one, and the reason
 *   the scope is remembered.  <cnormal means "you may bomb again", so under an ALL-GROUPS drop scope
 *   the group RE-JOINS drop mode - note this is exactly what rescues the <cstrict group a bare
 *   <cdrop REFUSED to convert (the bare <cdrop above skips it, so Tab-4-4-4-4-6 can leave a squadron
 *   entered as <cstrict sitting OUTSIDE the drop; the <cnormal puts it back in).
 *   Under a SELECTIVE "<cdrop 3 6" scope a <cnormal means "fly normal, stay out of the drop": the
 *   player has asked for a specific set of squadrons on this run, and <cnormal is not an invitation
 *   for a squadron that was never in the set.  A selective "<cnormal 2" is likewise always plain
 *   normal - naming one squadron is not an all-groups command.
 *   The SNAPSHOT IS NOT TOUCHED by the re-join.  It holds the order to restore when drop mode ends,
 *   and for a re-joined group the honest answer is still its original pre-drop order; writing .drop
 *   into it would make BAM_leaveDropMode restore .drop and the group would never bomb again.
 *
 *   LEAVING (BAM_leaveDropMode).  Only groups whose order is STILL .drop are restored from the
 *   snapshot; anything the player changed in the meantime (a <creserve N, importantly) is left
 *   exactly as they set it, and a group that appeared after the drop started falls back to normal.
 *   NOTE the setCoverAircraftAirGroupsOrders() side effect: a bare all-groups <cattack/<cstrict/
 *   <cescort/<cloiter also CLEARS the aim mode back to None and discards the snapshot, because the
 *   Tab-4 label would otherwise still read "Drop When I Drop" when nobody is on .drop any more.  That
 *   is deliberately NOT done for a PARTIAL command - "<creserve 3" is the documented way to hold
 *   squadrons back DURING a drop run and must not switch the mode off - and NOT done for a bare
 *   <cnormal either, because that command has just RE-JOINED everyone (see above).                [CODE]
 *
 *
 *   ================================================================================================
 *   9.  OPEN ISSUES - instrumentation in place, diagnosis pending test data
 *   ================================================================================================
 *
 *   *** (a) RUBBER BANDING: the formation oscillates +-300-500m instead of holding station. ***
 *   - The pacing branch of calcCoverSpeedToMatchMain() is a RELAY controller, not a proportional one.
 *     As a multiple of the leader's speed:
 *         behind  10-400m : 1.06   <- a CONSTANT; nothing eases off as the gap closes
 *         behind  400m+   : 1.10 / 1.20 / 1.30 / ...
 *         ahead   0-300m  : 0.98, then 0.97   <- only -2%, i.e. almost no braking
 *         ahead   300m+   : 0.70              <- a -27% STEP, from one metre of movement
 *     There is NO proportional feedback across the entire +-300m band, then a cliff.  A limit cycle
 *     is the expected behaviour of that shape, not a defect in the AI.  It maps exactly onto the
 *     report: steady behind -> still closing at +3m/s so it overshoots -> almost nothing brakes it
 *     for 300m -> violent correction -> repeat.                                        [CODE+]
 *   - A FOURTH discontinuity exists as well: <cstrict AND <cdrop both fly strict formation, which
 *     forces vel_mps = the leader's speed inside strictSpeedMatchDistance_m (30m).  So a bombing run
 *     has yet another step, at 30m, that a normal run does not.                            [CODE]
 *   - Contributing: the law is only re-evaluated every ~16s (keepAircraftOnTask_recurs), so the
 *     command is HELD while the offset keeps drifting underneath it.  That is the "steady for a
 *     while, then move" part of the report - a relaxation oscillator on a 16s dead-time sample.
 *   - ALSO: coverFormationSpeedBias (1.06) was tuned to OVERCOME the section-7 delivery shortfall.
 *     So the per-airgroup ratio calibration and the bias were both correcting the same error, and
 *     once the ratio made delivery exact they STACKED - raising the loop gain by ~50%.            [CODE]
 *   - Suspected fix (NOT yet done - awaiting data): replace the near-field bands with a single
 *     symmetric proportional law on front/back offset, deadbanded ~30-50m, keyed to <cfdist, keep
 *     the big catch-up bands only beyond ~1200m, and retire coverFormationSpeedBias.
 *   - <cfdist 2026/10 (Step C, "Option A"): <cfdist did nothing because the offset was applied to
 *     the escort WAYPOINT only.  A heavy-bomber FOLLOW waypoint sits ~5-6 km ahead with
 *     .Target = the player, and calcCoverSpeedToMatchMain() equilibrates on the leader's RAW
 *     position - so a +-1000m nudge on the waypoint never moved the resting point.  Now both the
 *     escort path (EscortPosWaypoint) and the bomber run-in path build ONE "virtual leader point"
 *     = player position + <cfdist along the player's heading, and feed it BOTH to the waypoint
 *     base AND to calcCoverSpeedToMatchMain() (new optional leaderRef parameter).  So <cfdist now
 *     shifts where the formation actually sits, ahead (+) or behind (-), with everything else
 *     unchanged; cfdist 0 => leaderRef == the player position => old behaviour exactly.  The bomb
 *     AIMPOINT is deliberately never shifted.                                                   [CODE]
 *   - 2026/10 STATUS - what HAS changed vs this list, and what is deliberately held for the next log:
 *       * DONE (Step A2): the 55 m/s floor is now leader-relative (40 absolute / Min(55, leader*1.15)).
 *       * DONE (Step A4): the ratio sampler now DISCARDS transients (|ag_vel - lastAsked| > 4 m/s) and
 *         out-of-band samples instead of folding them in, and stores the final clamped command as its
 *         denominator.  A "discarded this cycle N" counter is on the COVERSPEED line.
 *       * DONE (Step A1): heavy bombers are forced off .ESCORT back to .FOLLOW unless <cescort.
 *       * DEFERRED by decision (Step A5): the in-front aim-point lead (the 90/120 s targetVwld2 lead in
 *         EscortPosWaypoint) is NOT yet changed.  Run one test-server session after the A2/A4/A1 fixes
 *         and read the log before choosing the in-front lead sign (aim at the leader vs ~10s behind vs
 *         capped-30s-ahead).  The proportional near-field law above stays not-done, for the same reason.
 *
 *   *** (b) <cdrop / <cdropnow RELEASE LATENCY. ***
 *   - Our own latency is under a second: a 750ms bombCount() poll plus Timeout(0.05).  So the
 *     delay the player sees has to be in the SIM adopting the new flight plan.                [CODE]
 *   - Prime suspect: SetWay() is NOT adopted until the aircraft reaches its current waypoint.
 *     That would explain the observed symptom exactly - <cdropnow releasing when the group ARRIVES
 *     at the leader's old position, rather than at the GATTACK_POINT we placed at the group's own
 *     position.  The trailing NORMFLYs are 1500m apart (~20s each), which bounds the worst case. [??]
 *   - If confirmed, the fix is to shorten the leg the aircraft is already flying.  If instead the
 *     new GATTACK_POINT becomes current within a tick and the bombs STILL do not go, the delay is
 *     in the AI's release logic and no waypoint trick will help.  NOTE 2026/10: the once-suspected
 *     "writable bomb-release parameter" is NOT available - IL proves AiAircraft has no setParameter
 *     (section 4), so the C_-prefixed "writable" assumption was wrong.  The release latency we have
 *     measured is the ~6s BOMB BAY DOOR cycle, and the only lever on it is to TIME the player's own
 *     release (Step B1's chat note) or, eventually, to find a real door write path in the IL.
 *   - 2026/10 STATUS - release latency: confirmed to be the BOMB BAY DOOR cycle (~6s on Wellingtons),
 *     not a plan-adoption problem (the release IS adopted, per the DROPTRACE nWp/cw columns).  Step A3
 *     made the release legs inherit the FORMATION speed and the LEADER's heading instead of the group's
 *     own 80 m/s-stamped heading, so they no longer "speed up a little and change course a little" after
 *     a release.  Step B1 (a) reads A_BombBayDoor into the DROPTRACE "bay=" column so the exact door
 *     timing and value convention are measurable; (b) tells the player in chat that bombers need ~6s to
 *     open their bays, so they should let their own first bomb go a moment AFTER the release command.
 *     Step B4 is a LOG-ONLY placeholder (flag, OFF by default): IL proves there is no write-parameter
 *     API (AiAircraft has no setParameter), so it cannot pre-open the bays yet - it only logs the door
 *     position.  The bay= column in DROPTRACE is what first has to tell us the value convention and
 *     when the doors actually open, before any real pre-open can be attempted.
 *
 *   - DIAGNOSTICS ADDED, both ON_TESTSERVER only:  COVERSPEED logs every stage of the speed
 *     decision for one group per cycle, including the effective multiplier, whether the strict
 *     override fired, and how many ratio samples were discarded this cycle;  DROPTRACE logs a
 *     release-latency ladder (detect, issue, +1/3/6/10/15/20/30s) with the group's CURRENT waypoint
 *     index, action, distance to it, and the first aircraft's A_BombBayDoor value.
 *   - Deliberately UNCHANGED for the baseline run: the band values, COVER_DropWatchPeriod_ms (750),
 *     coverFormationSpeedBias, coverDropImmediateDist_m, the 1500m trailing waypoint spacing, and the
 *     in-front aim-point lead (Step A5, see above).
 *   - Step C 2026/10 additions: (i) <cfdist now shifts the speed-law reference (see above);
 *     (ii) dropBombsNow_airGroup writes GAttackPasses=AUTO / GAttackType=LEVEL on its point waypoint
 *     to match the proven path; (iii) BAM_forceFormationRefresh() re-issues a FOLLOW formation plan
 *     the instant DROP WHEN I DROP is entered, so a transient Tab-4 menu click onto the flare-point
 *     entry can no longer leave the groups flying a spurious GATTACK_POINT run (they used to turn
 *     round and release before the player finished selecting).  Groups inside the 25s release
 *     hold-off are skipped, so a re-arm never clobbers a release in progress.
 *
 *   *** 2026/10 LOG EVIDENCE (Wellingtons log 02D, Ju-88s log 02E) and STEPS C3/C4/C5. ***
 *   - CONFIRMED, <cfdist: "seems to work fine" on both types after the Option A change above.
 *   - CONFIRMED, the one-salvo latch is STILL THERE on a Wellington after Step C:  54 -> 51 twice,
 *     and once with no release at all (bay= stayed 0.000 for the whole 30s ladder).  Step C2 is the
 *     response - see section 1.
 *   - CONFIRMED, Ju-88s DO release more than one salvo through <cdrop (96 -> 75 -> 63 and
 *     96 -> 69 -> 63 in log E), i.e. the one-salvo latch is not universal - it is a Wellington /
 *     heavy-bomber-on-a-35m-point behaviour, which is what pointed Step C2 at the geometry.
 *   - FOUND, the release plan is sometimes OVERWRITTEN inside its own 25s hold-off.  In three of five
 *     drops the group was back on a plain 3-waypoint FOLLOW plan 3-6s after issue (DROPTRACE cw=1/3
 *     act=FOLLOW), which keepAircraftOnTask_recurs() should have been early-returning through.  One
 *     Wellington never released at all, which is consistent with this.                           [GAME]
 *   - STEP C3 (applied): dropPlanStillInForce() checks whether the group is still flying a plan that
 *     contains an attack waypoint; if not, and it still has bombs, the release plan is re-asserted
 *     once (bounded to 3-15s after issue, so it cannot loop and cannot fight the original issue).
 *     keepAircraftOnTask_recurs() now also logs "<cdrop HOLD" every cycle it early-returns, so the
 *     next log proves whether the hold-off really is firing.                                    [CODE]
 *   - MEASURED, the bomb-bay cycle is the whole release latency and it is TYPE dependent:  from the
 *     bay= column, a Wellington goes 0 -> 1 over ~3-6s (first bombs ~+5s) and a Ju-88 over ~10-15s
 *     (first bombs ~+13s).  Nothing in the code waited for this, so the AI's stick was always
 *     landing 5-15s of flight PAST the leader's line.                                          [GAME]
 *   - STEP C4 (applied): the release is now commanded when the group is coverDropBayLead_s (6s) x
 *     its OWN speed SHORT of the drop line, so the doors are open as it arrives on the line.  The
 *     old gate - "within 1500m of the leader OR at the line" - fired immediately for everyone in
 *     formation, which is precisely what made it late;  coverDropImmediateDist_m is now a 300m
 *     safety floor only.  coverDropMaxAhead_m (1000m) skips a group that is too far AHEAD: the stick
 *     would land well in front of the leader's and it will never close the gap.                 [CODE]
 *   - STEP C5 (applied, OFF by default - cdDropPreOpenBays): the only lever on the door cycle is to
 *     make the engine think an attack is imminent, so while <cdrop is armed each bomber is handed a
 *     GATTACK_POINT coverDropPreOpen_m (8km) ahead, re-issued every cycle so it never reaches it.
 *     The doors follow the attack waypoint, so they stay open, and the real release issued by
 *     coverDropReleasePass() should then be near-instant.  RISK: this is bomb-run attitude, not
 *     tight formation, so it must be A/B'd in a test session before being enabled anywhere.     [CODE]
 *   - <cfdist limits raised to +/-3000m (was +/-1000m), and the wording dropped "further".         [CODE]
 *   - STEP C7 + C8 (applied, and this is the important one - full write-up in section 1e):  the
 *     mission owner proved in the FMB that the ONLY thing that makes every bomber type dump its whole
 *     load is the bomb-bay doors being OPEN BEFORE the release plan arrives.  So the release plan
 *     now mirrors the reference test exactly (NORMFLY at own position, GATTACK_POINT 50m BEHIND,
 *     long trailing NORMFLY), the TYPE/PASSES writes and the setTask call are REMOVED, the doors are
 *     held open continuously by cdDropPreOpenBays (ON, 10km decoy), and the release pass WAITS - with
 *     no timeout - for A_BombBayDoor to read open before letting a group go.  A group whose door
 *     parameter never moves is released anyway after coverDropBayWaitFallback_s as a deadlock escape.
 *     The bay lead-in (C4) is now applied ONLY when a group's doors are not yet open, since with them
 *     open the release is instant and a lead would bias the stick short of the leader's line.   [CODE]
 *
 *
 *   ================================================================================================
 */