//$reference System.Core.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll

using System;
using System.Text;
using part;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;

public class subMission : AMission
{
    AMission mainMission;

    public subMission()
    {
        try
        {
            Console.WriteLine("SUBMISSION .cs file loading...");
            mainMission = (AMission)DataDictionary["MAIN.MISSION"];
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION initializer(): ERROR! " + ex.ToString()); }
    }



    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        try
        {
            Console.WriteLine("SUBMISSION OnMissionLoaded() running...");
            mainMission = (AMission)DataDictionary["MAIN.MISSION"];
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION OnMissionLoaded(): ERROR! " + ex.ToString()); }
    }



    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        try
        {
            mainMission.OnTrigger(missionNumber, shortName, active);
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION OnTrigger ERROR!: " + ex.ToString()); }
    }
}
