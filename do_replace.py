with open("Genghis/Genghis-Class-StatsMission.cs", "r", encoding="utf-8") as f:
    content = f.read()
old_code = """if (!ai & !groundActorsFound) foreach (GroundStationary sta in GamePlay.gpGroundStationarys(pos.x, pos.y, 500))
                {
                    if (sta == null) continue;
                    //if (stb_Debug) GamePlay.gpLogServer(null, "OnBombExplosion near: " + sta.Name + " " + sta.Title, new object[] { });



                    double dis_m = sta.pos.distance(ref pos);  //distance from this groundstationary to the bomb detonation location, meters

                    //GamePlay.gpLogServer(null, "OnBombExplosion near: " + sta.Name + " " + sta.Title + " " + dis_m.ToString("n0"), new object[] { });

                    if (sta.Title.Contains("JerryCan_GER1_1") || sta.Title.Contains("JerryCan_GER1_2") || sta.Title.Contains("JerryCan_GER1_3") || sta.Title.Contains("JerryCan_GER1_5"))
                    {
                        //We use jerrycans to designate area targets or bombable areas.  
                        //The JerryCan_GER1_1 (static - environment - jerrycan) covers a radius of 71 meters which is just enough to fill a 100 meter square (seen in FMB at full zoom) to all corners if placed in the center of the 100m square.
                        // JerryCan_GER1_2 covers 141m radius (covers 4 100m squares to the corners if placed in the center)
                        // JerryCan_GER1_3 covers 282m radius (covers 16 100m squares to the corners if placed in the center)
                        // JerryCan_GER1_5 covers 1410m radius (a 1km square to the corner if placed in the center)
                        if (sta.Title.Contains("JerryCan_GER1_5") && dis_m <= 1410) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                        if (sta.Title.Contains("JerryCan_GER1_3") && dis_m <= 282) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                        if (sta.Title.Contains("JerryCan_GER1_2") && dis_m <= 141) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                        if (sta.Title.Contains("JerryCan_GER1_1") && dis_m <= 71) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                        // return;: Once we have found one bombable area marker, that is all we're looking for. Skip any further search for bombable area marks AND also for the civilian penalty markers; if it is within the given diestance of a jerrycan then this is by definition an enemy target zone
                        //If we want to do anything further below, such as give credit for bombing airfields, we'll need to re-write this somehow.
                    }

                    //Regent II Bus (static vehicle) defines a circle of 500 meters radius that is "civilian territory".  Note that the 500m radius is implicit in the GamePlay.gpGroundStationarys(pos.x, pos.y, 500) above, we've added && sta.pos.distance(ref pos) <= 500 so that we can change the radius above if necessary.
                    //Maddox Games TA Sports Car (static vehicle)  defines a circle of 250 meters radius that is "civilian territory"
                    //If they manage to hit a ROAD HIGHWAY or RAILROAD within a civilian area then they don't get negative points
                    //If there is a JERRYCAN found (above) we never get to this point, so not counted
                    //as civilian area
                    if (landType != maddox.game.LandTypes.ROAD && landType != maddox.game.LandTypes.ROAD_MASK && landType != maddox.game.LandTypes.HIGHWAY && landType != maddox.game.LandTypes.RAIL)
                    {
                        if (sta.Title.Contains("AEC_Regent_II") && dis_m <= 500) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
                        if (sta.Title.Contains("MG_TA") && dis_m <= 250) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
                    }

                }"""
new_code = """if (!ai & !groundActorsFound) 
                {
                    // Use virtual markers instead of physical ground stationaries
                    var nearbyMarkers = StationaryMarkerManager.FindNearbyMarkers(pos.x, pos.y, 500);
                    
                    foreach (var marker in nearbyMarkers)
                    {
                        double dis_m = marker.DistanceTo(pos.x, pos.y);
                        
                        // Check JerryCan markers (enemy bombable areas)
                        if (marker.Type.StartsWith("JerryCan_GER1"))
                        {
                            if (marker.Type == "JerryCan_GER1_5" && dis_m <= 1410) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                            if (marker.Type == "JerryCan_GER1_3" && dis_m <= 282) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                            if (marker.Type == "JerryCan_GER1_2" && dis_m <= 141) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                            if (marker.Type == "JerryCan_GER1_1" && dis_m <= 71) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                        }
                        
                        // Check civilian markers
                        if (landType != maddox.game.LandTypes.ROAD && landType != maddox.game.LandTypes.ROAD_MASK && landType != maddox.game.LandTypes.HIGHWAY && landType != maddox.game.LandTypes.RAIL)
                        {
                            if (marker.Type == "AEC_Regent_II" && dis_m <= 500) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
                            if (marker.Type == "MG_TA" && dis_m <= 250) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
                        }
                    }
                }"""

content_norm = content.replace('\r\n', '\n').replace('\r', '\n')
old_code_norm = old_code.replace('\r\n', '\n').replace('\r', '\n')
new_code_norm = new_code.replace('\r\n', '\n').replace('\r', '\n')

print("old_code_norm length:", len(old_code_norm))
print("content_norm length:", len(content_norm))
print("old_code_norm[:200]:", repr(old_code_norm[:200]))
print("content_norm[411958:412158]:", repr(content_norm[411958:412158]))

# Check where they differ
for i in range(min(len(old_code_norm), len(content_norm) - 411958)):
    if old_code_norm[i] != content_norm[411958 + i]:
        print(f"Mismatch at position {i}: old_code_norm[{i}] = {repr(old_code_norm[i])}, content_norm[{411958+i}] = {repr(content_norm[411958+i])}")
        print(f"Context old: {repr(old_code_norm[max(0,i-20):i+20])}")
        print(f"Context content: {repr(content_norm[411958+max(0,i-20):411958+i+20])}")
        break
else:
    print("All characters match up to min length!")

if old_code_norm in content_norm:
    content_norm = content_norm.replace(old_code_norm, new_code_norm)
    with open("Genghis/Genghis-Class-StatsMission.cs", "w", encoding="utf-8") as f:
        f.write(content_norm)
    print("Replacement successful!")
else:
    print("Old code not found!")
    idx = content.find("if (!ai & !groundActorsFound) foreach")
    if idx >= 0:
        print("Found at index:", idx)