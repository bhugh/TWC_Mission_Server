# -*- coding: utf-8 -*-
# Splice part 4: coverSpeedFloor_mps helper + Step C tunable constants.
import io, sys
F = "E:/Cliffs-of-Dover/TWC-campaigns-github/TWC_Mission_Server/Genghis/Genghis-Class-CoverMission.cs"
raw = io.open(F, encoding="utf-8", newline="").read()
crlf = "\r\n" in raw
lines = raw.replace("\r\n", "\n").split("\n")
def fail(m):
    print("FAIL: " + m); sys.exit(1)
def find1(s, what):
    hits = [i for i, l in enumerate(lines) if l.strip().startswith(s)]
    if len(hits) != 1: fail("%s: %d hits" % (what, len(hits)))
    return hits[0]

# --- 9. helper before getFrontBackDist
idx = find1("public double getFrontBackDist(Player player)", "getFrontBackDist")
helper = '''    //2026/10 - THE one place every upward speed floor comes from (bpFloor, cwFloor, the
    //dropPreOpen/release-plan clamps): floor = 0.75 x LEADER speed, clamped into [40,55] m/s.
    //  - 0.75x (NOT the old 1.15x or the fixed 55): the floor sits BELOW the leader, so it can
    //    never force the group to fly faster than the leader it is matching - the old rules did
    //    exactly that (1.15x up to the 55 cap; fixed 55 for any leader below 55 m/s), which is
    //    what "JU-87s kept faster than the player" and the drift-ahead-and-sit bug were.
    //  - 40 absolute: in-air stall guard, and the floor when the leader is unknown/stopped.
    //  - 55 absolute: never raises the floor above the old fixed-55 cap for fast leaders - the
    //    floor is never worse than before, and strictly better for leaders below ~74 m/s.
    //leaderSpeed_mps should be the LEADER's horizontal speed (CalculatePointDistance of Vwld).
    public double coverSpeedFloor_mps(double leaderSpeed_mps)
    {
        double floor_mps = leaderSpeed_mps * 0.75;
        if (floor_mps < 40) floor_mps = 40;
        if (floor_mps > 55) floor_mps = 55;
        return floor_mps;
    }

'''.split("\n")
lines[idx:idx] = helper
print("9. helper: inserted before line %d" % (idx + 1))

# --- 10. Step C constants after defaultAmtVerticleShift_m
hits = [i for i, l in enumerate(lines) if l.strip() == "float defaultAmtVerticleShift_m = 40;"]
if len(hits) != 1: fail("defaultAmtVerticleShift_m: %d hits" % len(hits))
consts = '''
    //2026/10 Step C - EscortPosWaypoint aim-ahead & <cfdist along-track cap tunables, all in one place:
    double coverAimAhead_s = 12;           //heavy-bomber waypoint aim-ahead, seconds of leader travel (was 20-60s general, 90-120s for the "far & angled" case - deleted)
    double coverCfdistCapSlack_m = 500;    //max absolute along-track slack allowed around the commanded <cfdist, metres
    double coverCfdistCapSlack_s = 10;     //...or this many seconds of leader travel, whichever is smaller'''.split("\n")
lines[hits[0] + 1:hits[0] + 1] = consts
print("10. constants: inserted after line %d" % (hits[0] + 1))

out = "\r\n".join(lines) if crlf else "\n".join(lines)
io.open(F, "w", encoding="utf-8", newline="").write(out)
print("OK part4 written")
