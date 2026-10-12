with open('Genghis-Class-CoverMission.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Find and replace just the specific block
old_block = """            if (task != AiAirGroupTask.DO_NOTHING)
            {
                //perhaps we need to set the task AFTER the updatewaypoints thing has happened?               
                if (airGroup != null && coverAircraftAirGroupsActive.ContainsKey(airGroup)) airGroup.setTask(task, tasktarget);
            }"""

new_block = """            // FIX: Defensive guard - never allow cover aircraft to be tasked to attack their own player.
            // This is a belt-and-suspenders check to catch any code path that might accidentally
            // set task=ATTACK_AIR with tasktarget=playerAirGroup.
            if (task == AiAirGroupTask.ATTACK_AIR && tasktarget == playerAirGroup)
            {
                Console.WriteLine("!!! DEFENSIVE GUARD: Cover aircraft " + airGroup.Name() + " was about to be tasked ATTACK_AIR on own player " + player.Name() + " - downgrading to FLY_WAYPOINT");
                task = AiAirGroupTask.FLY_WAYPOINT;
                tasktarget = null;
            }
            
            if (task != AiAirGroupTask.DO_NOTHING)
            {
                //perhaps we need to set the task AFTER the updatewaypoints thing has happened?               
                if (airGroup != null && coverAircraftAirGroupsActive.ContainsKey(airGroup)) airGroup.setTask(task, tasktarget);
            }"""

if old_block in content:
    content = content.replace(old_block, new_block)
    with open('Genghis-Class-CoverMission.cs', 'w', encoding='utf-8') as f:
        f.write(content)
    print('SUCCESS')
else:
    print('OLD BLOCK NOT FOUND')
    idx = content.find('if (task != AiAirGroupTask.DO_NOTHING)')
    if idx >= 0:
        print(repr(content[idx:idx+200]))