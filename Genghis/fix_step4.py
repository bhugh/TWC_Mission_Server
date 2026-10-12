with open('Genghis-Class-CoverMission.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Find and replace the NODUPE gate condition - remove the leading "doorsOpen &&"
old_gate = """                else if (doorsOpen && (dh <= coverPreOpenNodupeHeading_deg)
                    && Math.Abs(ldPos.z - prev.Item5) <= coverPreOpenNodupeAlt_m
                    && Math.Abs(lv2 - prev.Item4) <= coverPreOpenNodupeSpeed_mps
                    && (DateTime.UtcNow - prev.Item6).TotalSeconds < coverPreOpenNodupeMaxAge_s)
                {
                    //geometry unchanged and the plan is healthy - keep what is flying
                    if (mainmission.ON_TESTSERVER)
                        Console.WriteLine("COVER <cdrop PREOPEN SKIP: {0} no re-issue - hdg {1:F1}d, alt {2:F0}m, vel {3:F0}, age {4:F0}s, bay={5:F3} (plan in force)",
                            airGroup.Name(), dh, Math.Abs(ldPos.z - prev.Item5), Math.Abs(lv2 - prev.Item4),
                            (DateTime.UtcNow - prev.Item6).TotalSeconds, coverDropBayDoorPos(airGroup));
                    return;
                }"""

new_gate = """                //NODUPE: skip re-issue if geometry unchanged (heading, alt, speed) and plan is healthy.
                //NOTE: removed the "doorsOpen &&" requirement so doorless planes (Blenheim, etc.) don't churn -
                //they now skip re-issue on steady flight just like real-door planes, and only re-anchor
                //when the leader actually turns/climbs/speeds up or the plan is lost/too old.
                else if ((dh <= coverPreOpenNodupeHeading_deg)
                    && Math.Abs(ldPos.z - prev.Item5) <= coverPreOpenNodupeAlt_m
                    && Math.Abs(lv2 - prev.Item4) <= coverPreOpenNodupeSpeed_mps
                    && (DateTime.UtcNow - prev.Item6).TotalSeconds < coverPreOpenNodupeMaxAge_s)
                {
                    //geometry unchanged and the plan is healthy - keep what is flying
                    if (mainmission.ON_TESTSERVER)
                        Console.WriteLine("COVER <cdrop PREOPEN SKIP: {0} no re-issue - hdg {1:F1}d, alt {2:F0}m, vel {3:F0}, age {4:F0}s, bay={5:F3} (plan in force)",
                            airGroup.Name(), dh, Math.Abs(ldPos.z - prev.Item5), Math.Abs(lv2 - prev.Item4),
                            (DateTime.UtcNow - prev.Item6).TotalSeconds, coverDropBayDoorPos(airGroup));
                    return;
                }"""

if old_gate in content:
    content = content.replace(old_gate, new_gate)
    with open('Genghis-Class-CoverMission.cs', 'w', encoding='utf-8') as f:
        f.write(content)
    print('SUCCESS: Relaxed NODUPE gate - removed doorsOpen requirement')
else:
    print('OLD GATE NOT FOUND')
    idx = content.find('else if (doorsOpen && (dh <= coverPreOpenNodupeHeading_deg)')
    if idx >= 0:
        print(repr(content[idx:idx+500]))