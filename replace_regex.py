import re
content = open('e:/Cliffs-of-Dover/TWC-campaigns-github/TWC_Mission_Server/Genghis/Genghis-Class-StatsMission.cs', 'r', encoding='utf-8').read()

pattern = r'(            if \(!ai & !groundActorsFound\) foreach \(GroundStationary sta in GamePlay\.gpGroundStationarys\(pos\.x, pos\.y, 500\)\)\n                \{.*?\n                \}\n\n\n            //TF_GamePlay)'

new_block = '''            if (!ai & !groundActorsFound) 
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
                }'''

new_content = re.sub(pattern, new_block + '\n\n\n            //TF_GamePlay', content, flags=re.DOTALL)

open('e:/Cliffs-of-Dover/TWC-campaigns-github/TWC_Mission_Server/Genghis/Genghis-Class-StatsMission.cs', 'w', encoding='utf-8').write(new_content)
print('Replacement done!')