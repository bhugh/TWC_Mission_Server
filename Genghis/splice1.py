# -*- coding: utf-8 -*-
# Splice part 1: Step C region (aim-ahead + <cfdist cap) and cwFloor unification.
import io, sys
F = "E:/Cliffs-of-Dover/TWC-campaigns-github/TWC_Mission_Server/Genghis/Genghis-Class-CoverMission.cs"
raw = io.open(F, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
txt = raw.replace("\r\n", "\n")
lines = txt.split("\n")
def fail(m):
    print("FAIL: " + m); sys.exit(1)
def find1(s, what):
    hits = [i for i, l in enumerate(lines) if l.strip() == s]
    if len(hits) != 1: fail("%s: %d hits" % (what, len(hits)))
    return hits[0]

# --- 1. Region C: from current_vel_mps decl to the '}' just before CurrentPos.z += altDiff_m
st = find1("double current_vel_mps = CoverCalcs.CalculatePointDistance(Vwld);", "vel decl")
zi = None
for i in range(st, len(lines)):
    if "CurrentPos.z += altDiff_m + ran.NextDouble()" in lines[i]:
        zi = i; break
if zi is None: fail("z-adjust not found")
k = zi - 1
while k >= 0 and (lines[k].strip() == "" or lines[k].strip().startswith("//")): k -= 1
if lines[k].strip() != "}": fail("end brace: " + repr(lines[k]))
region = "\n".join(lines[st:k + 1])
if "not a heavy bomber, ie fighters" not in region or "heavyBomber) //ok, tried this" not in region:
    fail("region content mismatch")
newC = '''                if (heavyBomber) //ok, tried this for ALL aircraft but it didn't go so well
                {
                    //2026/10 Step C - ONE short aim-ahead (coverAimAhead_s, 12s): the old general
                    //lead was 20-60s and the "far & angled" alternate point added 90-120s more -
                    //at 70 m/s that put the waypoint 1.4-8.4km AHEAD of the leader, which both made
                    //them mill around the point and completely swamped any negative <cfdist
                    //(cfdist -3000 + 60s x 70m/s = +1200m PAST you - the "0.5km instead of 3000m"
                    //report).  CloDNotes also found the alternate point's sign inverted when they
                    //were already in front, i.e. it pushed them further ahead the more they had
                    //overshot, so it is GONE entirely.
                    CurrentPos.x += targetVwld2.x * coverAimAhead_s;
                    CurrentPos.y += targetVwld2.y * coverAimAhead_s;
                }
                else //not a heavy bomber, ie fighters
                {
                    //2021/06 - was 5 here,  trying it at 45
                    CurrentPos.x += targetVwld2.x * -10; //for fighters let's try setting a point a littler BEHIND the main a/c
                    CurrentPos.y += targetVwld2.y * -10;
                }

                //2026/10 Step C - <cfdist CAP (along-track): whenever the player has commanded a
                //front/back offset, the commanded offset is the BINDING constraint.  Project the
                //waypoint onto the leader's horizontal heading and clamp its along-track distance
                //from the leader to cfdist +/- slack, slack = min(coverCfdistCapSlack_m,
                //coverCfdistCapSlack_s x leader speed).  cfdist == 0 keeps the free aim-ahead with
                //no cap (milling protection unchanged), and the fighters' sit-behind lead above is
                //also capped when cfdist IS set, so the command is honoured for every aircraft type.
                double cfdistCmd_m = getFrontBackDist(player);
                if (cfdistCmd_m != 0 && target_vel_mps > 1)
                {
                    double ux = targetVwld2.x / target_vel_mps; //unit horizontal heading -
                    double uy = targetVwld2.y / target_vel_mps; //target_vel_mps = CalculatePointDistance = x/y only
                    Point3d leadPos = targetAirGroup.Pos();
                    double along_m = (CurrentPos.x - leadPos.x) * ux + (CurrentPos.y - leadPos.y) * uy;
                    double slack_m = Math.Min(coverCfdistCapSlack_m, coverCfdistCapSlack_s * target_vel_mps);
                    if (along_m > cfdistCmd_m + slack_m)
                    {
                        double fix_m = along_m - (cfdistCmd_m + slack_m);
                        CurrentPos.x -= ux * fix_m;
                        CurrentPos.y -= uy * fix_m;
                    }
                    else if (along_m < cfdistCmd_m - slack_m)
                    {
                        double fix_m = along_m - (cfdistCmd_m - slack_m);
                        CurrentPos.x -= ux * fix_m;
                        CurrentPos.y -= uy * fix_m;
                    }
                }'''.split("\n")
lines[st:k + 1] = newC
print("1. region C: %d..%d -> %d lines" % (st + 1, k + 1, len(newC)))

# --- 2. cwFloor block
st = find1("double cwFloor_mps = 40;", "cwFloor decl")
en = None
for i in range(st, st + 12):
    if lines[i].strip() == "if (vel_mps < cwFloor_mps) vel_mps = cwFloor_mps;": en = i; break
if en is None: fail("cwFloor end not found")
newW = '''            //2026/10 unified via coverSpeedFloor_mps: 0.75 x LEADER speed clamped [40,55].  The
            //old Min(55, 1.15x) was a floor ABOVE leader speed - it forced the group to fly faster
            //than the leader whenever 1.15x < 55, pinning them ahead of a slow leader (the
            //drift-ahead-and-sit behaviour), and it still pinned 55 for leaders up to 55 m/s.
            double cwFloor_mps = 40;
            if (targetAirGroup != null)
            {
                Vector3d tV = targetAirGroup.Vwld();
                double tSpeed = CoverCalcs.CalculatePointDistance(tV);
                if (tSpeed > 1) cwFloor_mps = coverSpeedFloor_mps(tSpeed);
            }
            if (vel_mps < cwFloor_mps) vel_mps = cwFloor_mps;'''.split("\n")
lines[st:en + 1] = newW
print("2. cwFloor: %d..%d" % (st + 1, en + 1))

out = "\n".join(lines)
if crlf: out = out.replace("\n", "\r\n")
io.open(F, "w", encoding="utf-8", newline="").write(out)
print("OK part1 written")
