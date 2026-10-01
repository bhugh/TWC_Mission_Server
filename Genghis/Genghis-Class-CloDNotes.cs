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

     - The bomb-drop detection system doesn't work if the server (or offline mission) is set to unlimited ammo.  In that case the S_BombReserve parameter is always doesn't change.  I don't think .hasBombs() is reliable either.  So this function won't work really at all in unlimited ammo mode.
     
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
 *   - Does an immediate drop release ONE salvo, or the ENTIRE remaining load?                     [??]
 *
 *   - Do the unused S_BombReserve subtypes THROW, or do they return 0?  CoverCalcs.bombCount()
 *     swallows exceptions either way - but if they throw, that is up to 50 exceptions per aircraft
 *     per poll, on a mission that already fights warping.  Worth logging once to find out.     [??]
 *
 *   - Do GAttackPasses (AUTO / _1.._4 / ALL_OUT) and GAttackType (LEVEL / DIVE) actually change
 *     behaviour?  The existing bomber code sets them but nobody is certain they do anything.     [??]
 *
 *   - maddox.GP.Vector3d.angle() exists, but we could not confirm whether it returns degrees or
 *     radians.  Use CoverCalcs.roughlySameDirection() instead - that one is unambiguous.       [??]
 *
 *   - A_BombBayDoor (=73) may give a few seconds of advance warning of a drop, on aircraft that
 *     have bomb bay doors.  Untested.                                                         [??]
 *
 *
 *   ================================================================================================
 */