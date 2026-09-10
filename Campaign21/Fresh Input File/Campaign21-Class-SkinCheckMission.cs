#define DEBUG  
#define TRACE  

//$reference parts/core/CloDMissionCommunicator.dll
////$reference parts/core/CLOD_Extensions.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll
///$reference Microsoft.csharp.dll 
//$reference WPF/PresentationFramework.dll
//$reference WPF/PresentationCore.dll
//$reference WPF/WindowsBase.dll
//$reference System.Xaml.dll
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.ComponentModel;
using System.Text.RegularExpressions;
//using System.Data;
//using System.Core;
using System.Linq;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;
using part;
using Ini;
//using TF_Extensions;  //not working for now?
using TWCComms;

public class SkinCheckMission : AMission
{

    //all these values are pulled over from -main.cs so they are consistent locations
    string MISSION_ID { get; set; }
    string CAMPAIGN_ID { get; set; }
    string SERVER_ID { get; set; }
    string SERVER_ID_SHORT { get; set; }
    string CLOD_PATH { get; set; }
    string FILE_PATH { get; set; }
    string STATS_FULL_PATH { get; set; }
    string STATSCS_FULL_PATH { get; set; }

    public Mission mainmission;

    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    //public enum ArmiesE { None, Red, Blue };


    //initializer method
    public SkinCheckMission(Mission msn)
    {
            mainmission = msn;
            MISSION_ID = mainmission.MISSION_ID;
            CAMPAIGN_ID = mainmission.CAMPAIGN_ID;
            SERVER_ID = mainmission.SERVER_ID;
            SERVER_ID_SHORT = mainmission.SERVER_ID_SHORT;
            CLOD_PATH = mainmission.CLOD_PATH;
            FILE_PATH = mainmission.FILE_PATH;
            STATS_FULL_PATH = mainmission.STATS_FULL_PATH;
            STATSCS_FULL_PATH = mainmission.STATSCS_FULL_PATH;
            Console.WriteLine("SkinCheck Start: {0} {1} {2} {3} {4}", MISSION_ID, CAMPAIGN_ID, SERVER_ID, SERVER_ID_SHORT, CLOD_PATH);

    }

    public void DeleteLargeSkinFiles()
    {
        try
        {
            List<string> files = GetImageFilenamesFromDirectory(CLOD_PATH + "cache/");
            foreach (string fileName in files)
            {
                try
                {
                    FileInfo fi = new FileInfo(fileName);
                    long fileSize = fi.Length;
                    Console.WriteLine("Checking skin file " + fileName);
                    if (fileSize > 500000)
                    {
                        Console.WriteLine("Removing too-large skin file " + fileName);
                        File.Delete(fileName);
                    }
                }
                catch (Exception ex) { Console.WriteLine("DeleteLargeSkinFiles - individual file read/delete ERROR: " + ex.ToString()); }
            };
        }
        catch (Exception ex) { Console.WriteLine("DeleteLargeSkinFiles ERROR: " + ex.ToString()); }
    }

    //
    public List<string> GetImageFilenamesFromDirectory(string dirPath)
    {
        List<string> list = new List<string>();
        Console.WriteLine("Looking for skin files in: " + dirPath);
        string[] filenames = Directory.GetFiles(dirPath, "*.jpg");
        filenames = filenames.Concat(Directory.GetFiles(dirPath, "*.png")).ToArray();
        filenames = filenames.Concat(Directory.GetFiles(dirPath, "*.jpeg")).ToArray();
        filenames = filenames.Concat(Directory.GetFiles(dirPath, "*.dds")).ToArray();

        //So .bmp files seem to be different and of course LARGER.  Not sure why?  So skipping them for now.
        filenames = filenames.Concat(Directory.GetFiles(dirPath, "*.bmp")).ToArray();        

        list = new List<string>(filenames);
        Console.WriteLine("Num skin files found: " + list.Count);
        return list;
    }


    /*************************************************************************************
     * 
     * 
     * 
     * /****************************************************************
     * 
     * ADMIN PRIVILEGE
     * 
     * Determine if player is an admin, and what level
     * 
     ****************************************************************/
    public string[] admins_basic = new String[] { "TWC_", "Rostic" };
    public string[] admins_full = new String[] { "TWC_Flug", "TWC_Fatal_Error", "EvilUg", "Server" };

    public int admin_privilege_level(Player player)
    {
        if (player == null || player.Name() == null) return 0;
        string name = player.Name();
        //name = "TWC_muggle"; //for testing
        if (admins_full.Contains(name)) return 2; //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
        if (admins_basic.Any(name.Contains)) return 1; //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
        return 0;

    }

    void Mission_EventChat(Player from, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 

        Player player = from as Player;
        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;
        AiActor actor = aircraft as AiActor;

        string msg_orig = msg;
        msg = msg.ToLower();
        //Stb_Message(null, "Stats msg recvd.", null);

        /*
        if (msg.StartsWith("<!deban") && (admin_privilege_level(player) < 2))
        {

        }
        */
        /*
        if (msg.StartsWith("<suppadd") && (admin_privilege_level(player) > 1))
        {


        }

        else if (msg.StartsWith("<admin") && admin_privilege_level(player) > 1)// || msg.StartsWith("<"))
        {

        }
        */
    }

    /********************************************************************************************************
     *  END chat commands
     * *******************************************************************************************************/


}

