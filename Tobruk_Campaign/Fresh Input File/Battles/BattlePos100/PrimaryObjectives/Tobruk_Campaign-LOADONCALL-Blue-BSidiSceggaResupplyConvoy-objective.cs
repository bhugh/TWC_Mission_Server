//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll


using System;
using System.Collections.Generic;
using System.IO;

using maddox.game;
using maddox.game.world;
using maddox.GP;
using maddox.game.page;
using part;


public class Mission : AMission
{

    AMission mainMission;

    public Mission()
    {

        Console.WriteLine("SUBMISSION2 loaded . . . ");

        try
        {
            Console.WriteLine("SUBMISSION2 .cs file loading...");
            mainMission = (AMission)DataDictionary["MAIN.MISSION"];
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION2 initializer(): ERROR! " + ex.ToString()); }
    }

    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {        
        try
        {
            mainMission.OnTrigger(missionNumber, shortName, active);
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION2 OnTrigger ERROR!: " + ex.ToString()); }
    }

}
