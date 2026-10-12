with open('Genghis-Class-CoverMission.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the old tasktarget = playerAirGroup; line in the >4000m block
old = """aawpt = AiAirWayPointType.FOLLOW; //SEtting to Escort seems to make them drop their bombs?  Maybe?
                                                  //aawpt = AiAirWayPointType.ESCORT; //SEtting to Escort seems to make them drop their bombs?  Maybe?
\t\t\t\ttasktarget = playerAirGroup;\t\t\t\t\t\t\t\t  

                // FIX: Prevent cover aircraft from attacking their own player when >4000m away."""

new = """aawpt = AiAirWayPointType.FOLLOW; //SEtting to Escort seems to make them drop their bombs?  Maybe?
                                                  //aawpt = AiAirWayPointType.ESCORT; //SEtting to Escort seems to make them drop their bombs?  Maybe?

                // FIX: Prevent cover aircraft from attacking their own player when >4000m away."""

if old in content:
    content = content.replace(old, new)
    with open('Genghis-Class-CoverMission.cs', 'w', encoding='utf-8') as f:
        f.write(content)
    print('SUCCESS')
else:
    print('OLD NOT FOUND')
    # Debug: show what's actually there
    idx = content.find('tasktarget = playerAirGroup')
    if idx >= 0:
        print(repr(content[idx-100:idx+100]))