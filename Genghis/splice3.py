# -*- coding: utf-8 -*-
# Splice part 3: Step D aim-point passes rule, attack-termination gate, vel<40 clamps.
import io, sys
F = "E:/Cliffs-of-Dover/TWC-campaigns-github/TWC_Mission_Server/Genghis/Genghis-Class-CoverMission.cs"
raw = io.open(F, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
lines = raw.replace("\r\n", "\n").split("\n")
def fail(m):
    print("FAIL: " + m); sys.exit(1)

# --- 6. aim-point passes rule (Step D)
hits = [i for i, l in enumerate(lines)
        if l.strip().startswith("(nextWP as AiAirWayPoint).GAttackPasses = AiAirWayPointGAttackPasses.AUTO;")]
if len(hits) != 1: fail("AUTO passes rule: %d hits" % len(hits))
newR = '''            //2026/10 Step D - passes rule, set ONCE at the aim point (before any branching):
            //heavy & dive bombers get ONE pass (._1) - they drop their full stick and their guns
            //are not worth strafing passes with; Sturmoviks & fighters keep AUTO (drop, then strafe,
            //repeated passes) - as requested from the 2026/10 test flights.  Was AUTO for everyone,
            //with per-branch overrides below.
            (nextWP as AiAirWayPoint).GAttackPasses = (isHeavyBomber(airGroup) || isDiveBomber(airGroup)) ? AiAirWayPointGAttackPasses._1 : AiAirWayPointGAttackPasses.AUTO;  //can do ._1 ._2 ._3 ._4 OR ALL_OUT'''.split("\n")
lines[hits[0]:hits[0] + 1] = newR
print("6. passes rule: line %d -> %d lines" % (hits[0] + 1, len(newR)))

# --- 7. attack-termination gate after the BAM gate block
gidx = None
for i, l in enumerate(lines):
    if l.strip().startswith("if (bam == BAM_BombAimMode.None || bam == BAM_BombAimMode.Drop_When_I_Drop || !ordersBombGround(orders) )"):
        gidx = i; break
if gidx is None: fail("BAM gate if not found")
close = None
for i in range(gidx, gidx + 20):
    if lines[i] == "                }": close = i; break
if close is None: fail("BAM gate close brace not found")
gate = '''

                //2026/10 Step D - ATTACK-TERMINATION GATE (heavy & dive bombers only).  Their
                //GAttackPasses is ._1 (one pass only), but this routine re-issues their attack plan
                //every ~16s and a FRESH PLAN = A FRESH PASS - which would silently defeat ._1
                //(bombs 54 -> 51 -> 51 ... for ever).  Once their bombs are GONE the attack run is
                //genuinely over (their guns are not worth strafing passes with): clear the target
                //exactly like the bam gate above and fall through to the formation/escort plan
                //below.  Sturmoviks & fighters (AUTO passes) are deliberately NOT gated - they keep
                //strafing after the drop, as requested.  isBomberArmed also covers torpedos.
                if (!isBomberArmed(airGroup) && (isHeavyBomber(airGroup) || isDiveBomber(airGroup)))
                {
                    bombing = false;
                    newTargetPoint = new Point3d(-1, -1, -1);
                    if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                    if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                    if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);
                }'''.split("\n")
lines[close + 1:close + 1] = gate
print("7. gate: inserted after line %d" % (close + 1))

# --- 8. vel<40 clamps (2x) -> unified helper (bottom-up)
hits = [i for i, l in enumerate(lines) if l == "            if (vel < 40) vel = 40;"]
if len(hits) != 2: fail("vel<40: %d hits (expected 2)" % len(hits))
newV = '''            //2026/10 unified upward floor (coverSpeedFloor_mps): 0.75 x LEADER speed clamped [40,55].
            double leaderFloor_mps = coverSpeedFloor_mps(leader == null ? 0 : CoverCalcs.CalculatePointDistance(leader.Vwld()));
            if (vel < leaderFloor_mps) vel = leaderFloor_mps;'''.split("\n")
for i in reversed(hits):
    lines[i:i + 1] = newV
print("8. vel<40: %d sites unified" % len(hits))

out = "\r\n".join(lines) if crlf else "\n".join(lines)
io.open(F, "w", encoding="utf-8", newline="").write(out)
print("OK part3 written")
