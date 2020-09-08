////$reference parts/core/CLOD_Extensions.dll
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
        
        Console.WriteLine("SUBMISSION loaded . . . ");

        try
        {
            Console.WriteLine("SUBMISSION .cs file loading...");
            mainMission = (AMission)DataDictionary["MAIN.MISSION"];
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION initializer(): ERROR! " + ex.ToString()); }
    }

    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);
        try
        {
            mainMission.OnTrigger(missionNumber, shortName, active);
        }
        catch (Exception ex) { Console.WriteLine("SUBMISSION OnTrigger ERROR!: " + ex.ToString()); }
    }


    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

    }

    public override void OnPlayerConnected(Player player)
    {
        if (MissionNumber > -1)
        {

        }
    }

    public override void OnPlayerArmy(Player player, int Army)
    {


    }


    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);



    }




    public void Stb_RemovePlayerFromCart(AiCart cart, Player player = null) //removes a certain player from any aircraft, artillery, vehicle, ship, or whatever actor/cart the player is in.  Removes from ALL places.
                                                                            //if player = null then remove ALL players from ALL positions
                                                                            //I am not 100% sure this really works, it is quirky at teh very least.
    {
        try
        {


        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); };
    }


    public void logToFile(object data, string messageLogPath)
    {
        try
        {
            FileInfo fi = new FileInfo(messageLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(messageLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(messageLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); };
    }



}
