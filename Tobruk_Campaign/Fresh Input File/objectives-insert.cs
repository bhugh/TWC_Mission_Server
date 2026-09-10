//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 


using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using System.Collections;
using System.Globalization;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using maddox.game.page;
using part;


public class TWCTriggers {


private void runme (Mission msn) {

            msn.mission_objectives.addTrigger(Mission.MO_ObjectiveType.Convoy, "Tobruk-Gasr Resupply Convoy2", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-RTobrukGasrResupplyConvoy-objective2.mis", "2006_Chief", 2, 5, "RTobrukGasrResupplyConvoy2", "TGroupDestroyed", 100, 197907, 95422, 100, false, 200, 24, "", true);  //g
            
            /*
            addTrigger(MO_ObjectiveType.Convoy, "Alsmar-Gasr Resupply Convoy2", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-RAlsmarGasrResupplyConvoy-objective2.mis", "2007_Chief", 2, 5, "RAlsmarGasrResupplyConvoy2", "TGroupDestroyed", 100, 150983, 37668, 100, false, 200, 24, "", add);  //g            

            addTrigger(MO_ObjectiveType.Convoy, "Sidi-Scegga Resupply Convoy2", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-BSidiSceggaResupplyConvoy-objective2.mis", "2010_Chief", 1, 5, "BSidiSceggaResupplyConvoy2", "TGroupDestroyed", 100, 327607, 126528, 100, false, 200, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Convoy, "Siwi-Scegga Resupply Convoy2", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-BSiwiSceggaResupplyConvoy-objective2.mis", "2008_Chief", 1, 5, "RSiwiSceggaResupplyConvoy2", "TGroupDestroyed", 100, 286184, 35204, 100, false, 200, 24, "", add);  //g
            
            */
            }
            
             
            }