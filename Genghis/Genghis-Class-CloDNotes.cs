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
 *     => do NOT bother setting GAttackType / GAttackPasses on a GATTACK_POINT waypoint.  A plan
 *        to use GAttackPasses.ALL_OUT to force a full release is a NO-OP on a point waypoint.
 *   - AiAirWayPoint.GAttackPasses = {AUTO=0, _1=1, _2=2, _3=3, _4=4, ALL_OUT=5}                [IL]
 *     AiAirWayPoint.GAttackType   = {AUTO=0, LEVEL=1, DIVE=2, TOP_MAST=3, SHALLOW_DIVE=4}    [IL]
 *     Both are public FIELDS on AiAirWayPoint; only settable usefully on GATTACK_TARG.         [IL]
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
 *       C_  = control, writable
 *              C_BombBayDoor = 40, C_BombSight
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
 *   ================================================================================================
 */