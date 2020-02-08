#define DEBUG  
#define TRACE  
//$reference parts/core/CLOD_Extensions.dll
//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 
//$reference WPF/PresentationFramework.dll
//$reference WPF/PresentationCore.dll
//$reference WPF/WindowsBase.dll
//$reference System.Xaml.dll
// $reference System.Core.dll  is needed to make HashSet work.  For some reason.


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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Dynamic;
using TF_Extensions;

public class Mission : AMission
{
    bool autoAdd = true; //whether or not to auto-add people who are not already registered to the army they first click on
    IMainMission TWCMainMission;
    //Red side check for signed up players
    HashSet<string> reds;
    HashSet<string> blues;

/*    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    set.Add("john");
Debug.Assert(set.Contains("JohN"));
*/



    //Constructor
    public Mission()
    {
        TWCMainMission = TWCComms.Communicator.Instance.Main;
        if (TWCMainMission == null)
        {
            Console.WriteLine("CheckPlayerSignedUp - Read Files ERROR: No -main.cs file loaded!");
            return;
        }

        //string full_file_path = TWCMainMission.CLOD_PATH + TWCMainMission.FILE_PATH;
        string full_file_prefix = TWCMainMission.STATSCS_FULL_PATH + TWCMainMission.CAMPAIGN_ID + "_EnrolledPlayers_"; //this is the directory where we save files relevant to the CAMPAIGN, something like il-2 sturmovik cliffs of dover/missions/Multi/Fatal/
          //Plus the prefix relevant to this campaign + _EnrolledPLayers_.  So something like "The Big War_EnrolledPlayers_reds.txt" will be the required filename

        //if (missionNumber == MissionNumber)
        {
            List<string> red_temp;
            List<string> blue_temp;

            try
            {
                if (!File.Exists(full_file_prefix + "reds.txt")) { File.CreateText(full_file_prefix + "reds.txt"); }                
            }
            catch (Exception ex) { Console.WriteLine("CheckPlayerSignedUp - error checking/creating player sign-up files RED." + ex.ToString()); }

            try
            {
                if (!File.Exists(full_file_prefix + "blues.txt")) { File.CreateText(full_file_prefix + "blues.txt"); }                
            }
            catch (Exception ex) { Console.WriteLine("CheckPlayerSignedUp - error checking/creating player sign-up files BLUE." + ex.ToString()); }


            try
            {
                red_temp = new List<string>((System.IO.File.ReadAllLines(full_file_prefix + "reds.txt")).ToList().TrimAll().ToLowerAll());//.TrimAll().ToLowerAll());//, StringComparer.OrdinalIgnoreCase); //.ToList());
                blue_temp = new List<string>((System.IO.File.ReadAllLines(full_file_prefix + "blues.txt")).ToList().TrimAll().ToLowerAll());//, StringComparer.OrdinalIgnoreCase); //.ToList());
                reds = new HashSet<string>(red_temp);
                blues = new HashSet<string>(blue_temp);
            }
            catch (Exception ex) { Console.WriteLine("CheckPlayerSignedUp - Read Files ERROR: " + ex.ToString()); }
            Console.WriteLine("-----------------------");
            Console.Write("RED team members: ");
            foreach (string r in reds)
            {
                Console.Write(r + ", ");
            }
            Console.WriteLine();
            Console.WriteLine("-----------------------");
            Console.Write("BLUE team members: ");
            foreach (string r in blues)
            {
                Console.Write(r + ", ");
            }
            Console.WriteLine();
            Console.WriteLine("-----------------------");


        }

    }

    public override void OnPlayerArmy(maddox.game.Player player, int army)
    {
        base.OnPlayerArmy(player, army);
        if (player == null) return;

        string arm = "None";
        string notarm = "None";
        if (army == 1) { arm = "Red"; notarm = "Blue"; }
        if (army == 2) { arm = "Blue"; notarm = "Red"; }

        //the case where the player isn't in either army (OR the army list is BLANK) we'll just add the player to the army the player is joining now
        if ( 
             autoAdd &&
             (reds  == null ||  !reds.Contains(player.Name().Trim().ToLower())  ) &&
             (blues == null ||  !blues.Contains(player.Name().Trim().ToLower()) )
           )
        {
            addNameToTeam(player, army);
        }
        bool onRedTeam = (reds == null || reds.Contains(player.Name().Trim().ToLower())); //we're trying to join Red army & this player is indeed in red
        bool onBlueTeam = (blues == null || blues.Contains(player.Name().Trim().ToLower())); //we're trying to join Red army & this player is indeed in red

        bool joiningAndOnRedTeam = (army == 1) && onRedTeam;
        bool joiningAndOnBlueTeam = ( army == 2 ) && onBlueTeam; //we're trying to join Red army & this player is indeed in red army (OR there is no -red.txt file available)

        //If they've joined already as one army but clicked on the opposite army, we just flip them back their "real" army
        //On the flag screen, this means they can click the other army but nothing happens. They only get airports, the ability to spawn in, with their own army
        if ( 
             (army == 1 && !onRedTeam && onBlueTeam) ||
             (army == 2 && !onBlueTeam && onRedTeam)
             )
        {
            Timeout(0.2, () =>(GamePlay as GameDef).selectArmyRequest(player as IPlayer, 3 - army)); //We just shift them to the correct army
            Stb_Chat(">>>>>" + player.Name() + " just tried to join " + arm + " but is signed up for " + notarm + ".");
        }
        
        else if (
                (army < 1 || army > 2) ||
                (
                    !onRedTeam && !onBlueTeam
                )
           )
        {            
            Stb_Chat(">>>>>" + player.Name() + " tried to join the Campaign but is not registered.");
            Stb_Chat(">>>>>Contact TWC_Fatal_Error @ ATAG forums if an error OR register at twcpilots.com");
            //Timeout(1, () => (GamePlay as GameDef).gameInterface.CmdExec("kick " + player.Name()));

            //(GamePlay as GameDef).selectArmyRequest(player as IPlayer, 3-army);
            (GamePlay as GameDef).gameInterface.CmdExec("kick " + player.Name()); //we kick them out in this case

        }
    }

    public void addNameToTeam(Player player, int army) {

        if (TWCMainMission == null)
        {
            Console.WriteLine("CheckPlayerSignedUp - Read Files ERROR: No -main.cs file loaded!");
            return;
        }

        if (player == null || army < 1 || army > 2) return;

        if (army == 1) reds.Add(player.Name().Trim().ToLower());
        if (army == 2) blues.Add(player.Name().Trim().ToLower());

        string full_file = TWCMainMission.STATSCS_FULL_PATH + TWCMainMission.CAMPAIGN_ID + "_EnrolledPlayers_"; //this is the directory where we save files relevant to the 
        if (army == 1) full_file += "reds.txt";
        else if (army == 2) full_file += "blues.txt";
        else return;

        try
        {
            System.IO.File.AppendAllText(full_file, Environment.NewLine + player.Name().Trim().ToLower());//.TrimAll().ToLowerAll());//, StringComparer.OrdinalIgnoreCase); //.ToList());
            
        }
        catch (Exception ex) { Console.WriteLine("CheckPlayerSignedUp - addNameToTeam ERROR: " + ex.ToString()); }

    }

    //This is broken (broadcasts to everyone, not just the Player) but has one BIG advantage:
    //the messages can be seen on the lobby/map screen
    public void Stb_Chat(string line, Player player = null)
    {
        if (GamePlay is GameDef)
        {
            if (player != null) (GamePlay as GameDef).gameInterface.CmdExec("chat " + line + " TO " + player.Name());
            else (GamePlay as GameDef).gameInterface.CmdExec("chat " + line);
        }
    }

}

public static class Extensions
{
    public static bool CaseInsensitiveContains(this string text, string value,
        StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
    {
        return text.IndexOf(value, stringComparison) >= 0;
    }
}

public static class StringListExtensions
{
    public static List<string> TrimAll(this List<string> stringList)
    {
        for (int i = 0; i < stringList.Count; i++)
        {
            stringList[i] = stringList[i].Trim(); //warning: do not change this to lambda expression (.ForEach() uses a copy)
        }
        return stringList;
    }
    public static List<string> ToLowerAll(this List<string> stringList)
    {
        for (int i = 0; i < stringList.Count; i++)
        {
            stringList[i] = stringList[i].ToLower(); //warning: do not change this to lambda expression (.ForEach() uses a copy)
        }
        return stringList;
    }
}