# -*- coding: utf-8 -*-
# Splice part 2: bpFloor unification, Step D ALL_OUT override, CBCW log passes field.
import io, sys
F = "E:/Cliffs-of-Dover/TWC-campaigns-github/TWC_Mission_Server/Genghis/Genghis-Class-CoverMission.cs"
raw = io.open(F, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
lines = raw.replace("\r\n", "\n").split("\n")
def fail(m):
    print("FAIL: " + m); sys.exit(1)

# --- 3. bpFloor block
hits = [i for i, l in enumerate(lines) if l.strip() == "double bpFloor_mps = 40;"]
if len(hits) != 1: fail("bpFloor decl: %d hits" % len(hits))
st = hits[0]
en = None
for i in range(st, st + 12):
    if lines[i].strip() == "if (vel_mps < bpFloor_mps) vel_mps = bpFloor_mps;": en = i; break
if en is None: fail("bpFloor end not found")
newB = '''            //2026/10 unified via coverSpeedFloor_mps: 0.75 x LEADER speed clamped [40,55] (the
            //old Min(55, 1.15x) sat ABOVE leader speed and forced the group faster than a slow
            //leader - the drift-ahead-and-sit bug).
            double bpFloor_mps = 40;
            if (playerAirGroup != null)
            {
                Vector3d plV = playerAirGroup.Vwld();
                double plSpeed = CoverCalcs.CalculatePointDistance(plV);
                if (plSpeed > 1) bpFloor_mps = coverSpeedFloor_mps(plSpeed);
            }
            if (vel_mps < bpFloor_mps) vel_mps = bpFloor_mps;'''.split("\n")
lines[st:en + 1] = newB
print("3. bpFloor: %d..%d" % (st + 1, en + 1))

# --- 4. ALL_OUT override (Step D)
hits = [i for i, l in enumerate(lines)
        if l.strip().startswith("if (!isDiveBomber(airGroup))(nextWP as AiAirWayPoint).GAttackPasses")]
if len(hits) != 1: fail("ALL_OUT override: %d hits" % len(hits))
newO = '''                //2026/10 Step D - the old ALL_OUT override here hit EVERY non-dive-bomber with a
                //ground target: Wellingtons (should be ._1 one pass - no useful strafing guns) AND
                //Sturmoviks/fighters (should be AUTO - drop then strafe, repeated passes).  Type
                //rule instead: heavy -> ._1 (already set at the aim point above), everyone else -> AUTO.
                if (!isDiveBomber(airGroup)) (nextWP as AiAirWayPoint).GAttackPasses = isHeavyBomber(airGroup) ? AiAirWayPointGAttackPasses._1 : AiAirWayPointGAttackPasses.AUTO; //can do AUTO _1, _2, _3, _4'''.split("\n")
lines[hits[0]:hits[0] + 1] = newO
print("4. ALL_OUT override: line %d -> %d lines" % (hits[0] + 1, len(newO)))

# --- 5. CBCW log lines (2x): add passes field
n = 0
for i, l in enumerate(lines):
    if "CBCW: waypoint info of DIVETARGET" in l:
        if 'target: {4}"' not in l or "(nextWP as AiAirWayPoint).Target));" not in l:
            fail("CBCW shape mismatch at line %d" % (i + 1))
        lines[i] = (l.replace('target: {4}"', 'target: {4} passes: {5}"')
                     .replace("(nextWP as AiAirWayPoint).Target));",
                              "(nextWP as AiAirWayPoint).Target, (nextWP as AiAirWayPoint).GAttackPasses));"))
        n += 1
if n != 2: fail("CBCW replaced: %d (expected 2)" % n)
print("5. CBCW logs: %d updated" % n)

out = "\r\n".join(lines) if crlf else "\n".join(lines)
io.open(F, "w", encoding="utf-8", newline="").write(out)
print("OK part2 written")
