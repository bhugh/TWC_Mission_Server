with open('Genghis-Class-CoverMission.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Add the new tunable variable after coverPreOpenNodupeMaxAge_s
old_vars = """    public double coverPreOpenNodupeMaxAge_s = 60;      //hard failsafe: re-issue anyway this long after the last one
                                                        //(so the 8-leg NORMFLY tail can never run out on a long
                                                        // straight hold - the whole point of the tail is to
                                                        // outlast the hold, and a stale plan defeats that)
    //last pre-open plan issued per group:"""

new_vars = """    public double coverPreOpenNodupeMaxAge_s = 60;      //hard failsafe: re-issue anyway this long after the last one
                                                        //(so the 8-leg NORMFLY tail can never run out on a long
                                                        // straight hold - the whole point of the tail is to
                                                        // outlast the hold, and a stale plan defeats that)
    //Faster re-issue cadence while DWID is armed and pre-open is active.
    //Default 4s (vs normal 16s) so Blenheims/doorless types re-anchor quickly on player turns.
    //CPU scaling (below) still applies on top, so it self-throttles under load.
    public double coverDropPreOpenReissue_s = 4;
    //last pre-open plan issued per group:"""

if old_vars in content:
    content = content.replace(old_vars, new_vars)
    with open('Genghis-Class-CoverMission.cs', 'w', encoding='utf-8') as f:
        f.write(content)
    print('SUCCESS: Added coverDropPreOpenReissue_s variable')
else:
    print('OLD VARS NOT FOUND')
    idx = content.find('coverPreOpenNodupeMaxAge_s')
    if idx >= 0:
        print(repr(content[idx:idx+400]))