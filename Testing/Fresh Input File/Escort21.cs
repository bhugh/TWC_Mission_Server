//$reference parts/core/CLOD_Extensions.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 
//$reference WPF/PresentationFramework.dll
//$reference WPF/PresentationCore.dll
//$reference WPF/WindowsBase.dll
//$reference System.Xaml.dll
//The first two $references above + perhaps the [rts] scriptAppDomain=0 references on conf.ini & confs.ini are (perhaps!?) necessary for some of the code below to work, esp. intercepting chat messages etc.
// $reference System.Core.dll  is needed to make HashSet work.  For some reason.
///$reference parts/core/MySql.Data.dll  //THIS DOESN'T SEEM TO WORK
///$reference parts/core/System.Data.dll //THIS DOESN'T SEEM TO WORK

// v.1_19_07. script by oreva, zaltys, small_bee, bhugh, flug, fatal_error, several other contributors/online code snippets & examples

using System;
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
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using TF_Extensions;

//////////////////simply change the////////////////////////
//////////////////GamePlay.gpHUDLogCenter("Do 17's traveling towards Lympne");///////////////
/////////////////into/////////////////////////////
//////////////////sendScreenMessageTo(1, "Do 17's traveling towards Lympne", null);/////////////////////
///////////////////////so only the red pilots get the message./////////////////////////////////

public class Mission : AMission
{
    Random random, stb_random;
    //Constants constants; 
    public int PERCENT_SUBMISSIONS_TO_LOAD = 60; //percentage of the aircraft sub-missions to load.  50 will load just half of the sub-missions etc.
    public string MISSION_ID;
    public string CAMPAIGN_ID;
    public string SERVER_ID;
    public string SERVER_ID_SHORT;
    public bool DEBUG;
    public bool LOG; //Whether to log debug messages to  a log file.

    public string MISSION_FOLDER_PATH;
    public string USER_DOC_PATH;
    public string CLOD_PATH;
    public string FILE_PATH;
    public string stb_FullPath;
    public Dictionary<int, string> radarpasswords;
    public string BOMBERMISSIONS_FILE_PATH;
    public string MESSAGE_FILE_NAME;
    public string MESSAGE_FULL_PATH;
    public string STATS_FILE_NAME;
    public string STATS_FULL_PATH;
    public string LOG_FILE_NAME;
    public string LOG_FULL_PATH;
    public string STATSCS_FULL_PATH;
    public int RADAR_REALISM;
    public string RESULTS_OUT_FILE; //Added by Fatal 11/09/2018.  This allows us to have win/lose logic for next mission

    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    public enum ArmiesE { None, Red, Blue };


    Stopwatch stopwatch;
    Dictionary<string, Tuple<long, SortedDictionary<string, string>>> radar_messages_store;

    //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
    //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
    //used in method admins_privilege_level below
    public string[] admins_basic = new String[] { "TWC_" };
    public string[] admins_full = new String[] { "TWC_Flug", "TWC_Fatal_Error", "Server" };

    public long Tick_Mission_Time { get; set; }// Sets the Mission Clock for Time Remaining in Mission.
    int allowedSpitIIas = 4;
    int currentSpitIIas = 0;
    int allowed109s = 4;
    int current109s = 0;


    MissionObjectives mission_objectives;


    
    int BlueObjective1 = 2;//AV22.1 Littlestone  Bombers
    int BlueObjective2 = 2;//AN.24.2 Redhill Bomber Base
    int BlueObjective3 = 2;//AU 23.5 Ashford Train Depot
    int BlueObjective4 = 2;//AX 25.9 Manston aircraft
    int BlueObjective5 = 2;//AX 23.5 British Armor @ Dover
    int BlueObjective6 = 2;//AP 27.4 British Armor @ London Docks
    int BlueObjective7 = 3;//Creek Mouth	Corvette
    int BlueObjective8 = 2;//Creek Mouth Ship at dock	
    int BlueObjective9 = 2;//Creek Mouth Ship at dock	
    int BlueObjective10 = 2;//London Docks Tanker at dock	
    int BlueObjective11 = 2;//London Docks Tanker at dock	
    int BlueObjective12 = 2;//London Docks Tanker at dock	
    int BlueObjective13 = 2;//London Docks Corvette
    int BlueObjective14 = 1; //Westgate RADAR 
    int BlueObjective15 = 1;//Sandwich RADAR
    int BlueObjective16 = 1;// Deal RADAR
    int BlueObjective17 = 1;// Dover RADAR
    int BlueObjective18 = 1;// Brookland RADAR
    int BlueObjective19 = 1;// Dungeoness RADAR
    int BlueObjective20 = 1;// Eastbourne RADAR
    int BlueObjective21 = 1;// Worthing RADAR
    int BlueObjective22 = 1;// Ventnor of RADAR Isle of Wight
	int BlueObjective23 = 3;//Corvette at Creek Mouth dock
    int BlueObjective24 = 2;// fuel Refinery DITTON	
    int BlueObjective25 = 2;// Ditton fuel Storage
	int BlueObjective26 = 2;// Maidstone train repair station	
	int BlueObjective27 = 2;// Tumbrige wells Armory	
    int BlueObjective28 = 6;// Radar Communications HQ
	// Seperation blue / red
	
    int RedObjective1 = 2; //"BD 22.1 Motorpool near Grand-Fort Philippe";
    int RedObjective2 = 2; //"BE 19.2 St. Omar Ball bearing Factory";
    int RedObjective3 = 3; //"BB 16.4Estree Fuel Depot";
    int RedObjective4 = 2; //"AZ 19.2 Boulogne Synthetic Fuel"; 
    int RedObjective5 = 2; //"BB 21.5 CALAIS TRAIN YARD"; 
    int RedObjective6 = 2; //"BB 21.8 Calais Hydrogen"; 
    int RedObjective7 = 2; //"BB  21.9 Calais Main Fuel"; 
    int RedObjective8 = 2; //"BB 21.8  Calais LOX"; 
    int RedObjective9 = 2; //"BB 21.8 Calais Torpedo"; 
    int RedObjective10 = 2; //"BB 21.8 Calais Diesel"; 
    int RedObjective11 = 2; //"AZ 18.9 Boulogne Aviation"; 
    int RedObjective12 = 2; //"AZ 18.6 Boulogne Diesel"; 
    int RedObjective13 = 2; //"AZ 18.9 Boulogne Benzine"; 
    int RedObjective14 = 2; //"AZ 18.8  Boulogne Liquid Oxygen"; 
	int RedObjective15 = 2; //"AZ18.9  Ethanol Boulogne"; 
	int RedObjective16 = 4;	//"BI 14.1 Arras Main Fuel"; 
	int RedObjective17 = 3;	//"BI 14.2 Arras Rubber Factory"; 
	int RedObjective18 = 2;	//"BD 11 ST Ouen AAA Factory"; 
	int RedObjective19 = 3;	//"BB 12 Abbeville Main Fuel";  
	int RedObjective20 = 2;	//"Dieppe Main Fuel"; 
	int RedObjective21 = 2;  //"Le Treport Fuel"; 
	int RedObjective22 = 3;	 //"Poix Nord Fuel Storage"; 
    int RedObjective23 = 2; //"BB 21.8Chemical Research Calais";
	int RedObjective24 = 2; //"BB 21.8 Optical Research Calais";
	int RedObjective25 = 2; //"BB 21.8Chemical Storage Calais"; 
	int RedObjective26 = 1; //"BB 21.8Food Storage Calais";
	int RedObjective27 = 2; //"BB 21.8 Gunpowder Calais Facility";
	int RedObjective28 = 1; //"BC 21.8 Radar Oye Plauge "; 
	int RedObjective29 = 1; // "BA 21.6 Radar Coquells";
	int RedObjective30 = 3; //" Minensuchboote";  
	int RedObjective31 = 3; //"BI 14.1 Arras Fuel Storage 2"; 
    int RedObjective32 = 2; //"BE 20.8 Watten Armory";	//32
	int RedObjective33 = 2; //"BE 22.   Half track Factory Dunkirk";
	int RedObjective34 = 2; //"BE 22.  Steel mill Dunkirk";
	int RedObjective35 = 2; //"BE 22.  Brass Smelter Dunkirk";
	int RedObjective36 = 2; //"BE 22.  Diesel Storage Dunkirk";
	int RedObjective37 = 3; //"BE 22.  Ammunition Wharehouse Dunkirk";

	
	
	
	
	
	
	
	
//Blue Objectives	
    const string Objective_A = " AV22.1 Littlestone  Bombers";
    const string Objective_B = " AN.24.2 Redhill Bomber Base";
    const string Objective_C = " AU 23.5 Ashford Train Depot";
    const string Objective_D = " AX 25.9 Manston aircraft";
    const string Objective_E = " AX 23.5 British Armor @ Dover";
    const string Objective_F = " AP 27.4 British Armor @ London Docks";
    const string Objective_S1 = " British Corvette off shore";	
    const string Objective_S2 = " AP 27.4 Tanker @ London Docks";
    const string Objective_S3 = " AP 27.4 Cargo Ship @ Creekmouth";
    const string Objective_S4 = " AP 27.4 Cargo Ship @ Creekmouth";
    const string Objective_S5 = " AP 27.4 Cargo Ship @ London Docks";
    const string Objective_S6 = " AO 27.4 Corvette @ Creekmouth";
    const string Objective_S7 = " AO 27.4 Tanker @ London Docks";
    const string Objective_S8 = " AO 27.4 Tanker Armor @ London Docks";
    const string Objective_S9 = " AO 27.4 Tanker @ London Docks";
    const string Objective_R1 = " Westgate Radar";	
    const string Objective_R2 = " Sandwich Radar";
    const string Objective_R3 = " Deal Radar";
    const string Objective_R4 = " Dover Radar";
    const string Objective_R5 = " Brookland Radar";
    const string Objective_R6 = " Dungeness Radar";
    const string Objective_R7 = " Eastborne Radar";
    const string Objective_R8 = " Little Hampton Radar";
	const string Objective_R9 = " Ventnor Radar";
    const string Objective_1A = " Ar 25.2 Ditton fuel Refinery";
    const string Objective_1B = " Ar 25.3 Fuel Storage Ditton";
	const string Objective_1C = " AS 24.7 Maidstone train repair facility";	
	const string Objective_1D = " AQ 23.5 Tunbridge Wells Armory";
    const string Objective_1E = " AS 28.7 Radar communications HQ";	
	//Red objectives
    const string Objective_G =  " BD 22.1 Motorpool near Grand-Fort Philippe";
    const string Objective_H =  " BE 19.2 St. Omar Ball bearing Factory";
    const string Objective_I =  " BB 16.4Estree Fuel Depot";
    const string Objective_J =  " AZ 19.2 Boulogne Synthetic Fuel"; 
    const string Objective_K =  " BB 21.5 CALAIS TRAIN YARD"; 
    const string Objective_L =  " BB 21.8 Calais Hydrogen"; 
    const string Objective_M =  " BB  21.9 Calais Main Fuel"; 
    const string Objective_N =  " BB 21.8  Calais LOX"; 
    const string Objective_O =  " BB 21.8 Calais Torpedo"; 
    const string Objective_P =  " BB 21.8 Calais Diesel"; 
    const string Objective_Q =  " AZ 18.9 Boulogne Aviation"; 
    const string Objective_R =  " AZ 18.6 Boulogne Diesel"; 
    const string Objective_S =  " AZ 18.9 Boulogne Benzine"; 
    const string Objective_T =  " AZ 18.8  Boulogne Liquid Oxygen"; 
    const string Objective_U =  " AZ18.9  Ethanol Boulogne"; 
    const string Objective_V =  " BI 14.1 Arras Main Fuel"; 
    const string Objective_W =  " BI 14.2 Arras Rubber Factory"; 
    const string Objective_X =  " BD 11 ST Ouen AAA Factory"; 
    const string Objective_Y =  " BB 12 Abbeville Main Fuel";  
    const string Objective_Z =  " Dieppe Main Fuel"; 
    const string Objective_AA = " Le Treport Fuel"; 
    const string Objective_BB = " Poix Nord Fuel Storage"; 
    const string Objective_CC = " BB 21.8Chemical Research Calais";
    const string Objective_DD = " BB 21.8 Optical Research Calais";
    const string Objective_EE = " BB 21.8Chemical Storage Calais"; 
    const string Objective_FF = " BB 21.8Food Storage Calais";
    const string Objective_GG = " BB 21.8 Gunpowder Calais Facility";
    const string Objective_HH = " BC 21.8 Radar Oye Plauge "; 
    const string Objective_II = " BA 21.6 Radar Coquells";
    const string Objective_JJ = " Minensuchboote";  
    const string Objective_KK = " BI 14.1 Arras Fuel Storage 2"; 
 	const string Objective_LL = " BE 20.8 Watten Armory";  
 	const string Objective_MM =	" BE 22.  Half track Factory Dunkirk";
 	const string Objective_NN =	" BE 22.  Steel mill Dunkirk";
 	const string Objective_OO =	" BE 22.  Brass Smelter Dunkirk";
  	const string Objective_PP = " BE 22.  Diesel Storage Dunkirk";
 	const string Objective_QQ = " BE 22.  Ammunition Warehouse Dunkirk";
    

    //Constructor
    public Mission()
    {
        random = new Random();
        stb_random = random;
        //constants = new Constants();
        MISSION_ID = @"Testing";
        SERVER_ID = "Tactical Server"; //Used by General Situation Map app
        //SERVER_ID_SHORT = "Tactical"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        SERVER_ID_SHORT = "MissionTEST"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        DEBUG = false;
        LOG = false;
        radarpasswords = new Dictionary<int, string>
        {
            { -1, "twc"}, //Red army #1
            { -2, "twc"}, //Blue, army #2
            { -3, "twc2twc"}, //admin
            //note that passwords are CASEINSENSITIVE
        };

        USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
        CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/";  // DO NOT CHANGE
        FILE_PATH = @"missions/Multi/Fatal/" + MISSION_ID + "/Fresh input file";   // mission install directory (CHANGE AS NEEDED)   
        stb_FullPath = CLOD_PATH + FILE_PATH;
        MESSAGE_FILE_NAME = MISSION_ID + @"_message_log.txt";
        MESSAGE_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + MESSAGE_FILE_NAME;
        STATS_FILE_NAME = MISSION_ID + @"_stats_log.txt";
        STATS_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + STATS_FILE_NAME;
        LOG_FILE_NAME = MISSION_ID + @"_log_log.txt";
        LOG_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + LOG_FILE_NAME;
        STATSCS_FULL_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/missions/Multi/Fatal/";  // Must match location -stats.cs is saving SessStats.txt to             
        stopwatch = Stopwatch.StartNew();
        RADAR_REALISM = (int)5;
		RESULTS_OUT_FILE = CLOD_PATH + FILE_PATH + @"/" + "MissionResult.txt";		
        radar_messages_store = new Dictionary<string, Tuple<long, SortedDictionary<string, string>>>();
    }


    private void DoDamageToAirplane(AiAircraft aircraft)
    {
        Timeout(10, () =>
         {
             if (aircraft != null)
             {

                 //if (!aircraft.IsAirborne())
                 //{

                 aircraft.cutLimb(part.LimbNames.AileronL0);
                 aircraft.cutLimb(part.LimbNames.AileronR0);

                 aircraft.cutLimb(part.LimbNames.ElevatorL0);
                 aircraft.cutLimb(part.LimbNames.ElevatorR0);

                 aircraft.cutLimb(part.LimbNames.Rudder0);


                 //aircraft.cutLimb(part.LimbNames.WingL0); 
                 //aircraft.cutLimb(part.LimbNames.WingR0);
                 //aircraft.cutLimb(part.LimbNames.WingL2); // Spit2a 
                 //aircraft.cutLimb(part.LimbNames.WingR2);

                 //aircraft.cutLimb(part.LimbNames.AileronL0);
                 //aircraft.cutLimb(part.LimbNames.AileronR1);

                 //aircraft.cutLimb(part.LimbNames.ElevatorL0);
                 //aircraft.cutLimb(part.LimbNames.ElevatorR1);

                 //aircraft.cutLimb(part.LimbNames.Engine0);
                 //aircraft.cutLimb(part.LimbNames.Engine7);


                 //part.LimbNames.ElevatorL0
                 //part.LimbNames.ElevatorL1
                 //part.LimbNames.ElevatorR0
                 //part.LimbNames.ElevatorR1

                 //part.LimbNames.Engine0 - 7


                 //part.LimbNames.AileronL0
                 //part.LimbNames.AileronL1
                 //part.LimbNames.AileronR0
                 //part.LimbNames.AileronR1


                 //aircraft.cutLimb(part.LimbNames.WingL1);
                 //aircraft.cutLimb(part.LimbNames.WingL3);
                 //aircraft.cutLimb(part.LimbNames.WingL4);
                 //aircraft.cutLimb(part.LimbNames.WingL5);
                 //aircraft.cutLimb(part.LimbNames.WingL6);
                 //aircraft.cutLimb(part.LimbNames.WingL7);

                 //aircraft.cutLimb(part.LimbNames.WingR1);

                 //aircraft.cutLimb(part.LimbNames.WingR3);
                 //aircraft.cutLimb(part.LimbNames.WingR4); // 109
                 //aircraft.cutLimb(part.LimbNames.WingR5);
                 //aircraft.cutLimb(part.LimbNames.WingR6);
                 //aircraft.cutLimb(part.LimbNames.WingR7);
                 ////}
                 ////else
                 ////{
                 ////    // plane in Air Tail cut off
                 //aircraft.cutLimb(part.LimbNames.Tail0);
                 //aircraft.cutLimb(part.LimbNames.Tail1);
                 //aircraft.cutLimb(part.LimbNames.Tail2);
                 //aircraft.cutLimb(part.LimbNames.Tail3);
                 //aircraft.cutLimb(part.LimbNames.Tail4);
                 //aircraft.cutLimb(part.LimbNames.Tail5);
                 //aircraft.cutLimb(part.LimbNames.Tail6);
                 //aircraft.cutLimb(part.LimbNames.Tail7);
                 ////}
             }
         });
    }
    private void destroyPlayerPlane(AiAircraft aircraft)
    {
        if (aircraft != null)
            aircraft.Destroy();
    }
    private void damagePlayerGroup(AiActor actorMain)
    {
        foreach (AiActor actor in actorMain.Group().GetItems())
        {
            if (actor == null)
                return;

            AiAircraft aircraft = (actor as AiAircraft);

            if (aircraft != null)
            {
                DoDamageToAirplane(aircraft);
            }
        }
    }

    private void sendScreenMessageTo(int army, string msg, object[] parms)
    {
        List<Player> Players = new List<Player>();

        //Singleplayer or Dedi Server
        if (GamePlay.gpPlayer() != null)
        {
            if (GamePlay.gpPlayer().Army() == army || army == -1)
                Players.Add(GamePlay.gpPlayer());
        } // Multiplayer
        if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
        {
            foreach (Player p in GamePlay.gpRemotePlayers())
            {
                if (p.Army() == army || army == -1)
                    Players.Add(p);
            }
        }
        if (Players != null && Players.Count > 0)
            GamePlay.gpHUDLogCenter(Players.ToArray(), msg, parms);
    }


    private void sendChatMessageTo(int army, string msg, object[] parms)
    {
        List<Player> Players = new List<Player>();

        //Singleplayer or Dedi Server
        if (GamePlay.gpPlayer() != null)
        {
            if (GamePlay.gpPlayer().Army() == army || army == -1)
                Players.Add(GamePlay.gpPlayer());
        } // Multiplayer
        if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
        {
            foreach (Player p in GamePlay.gpRemotePlayers())
            {
                if (p.Army() == army || army == -1)
                    Players.Add(p);
            }
        }
        if (Players != null && Players.Count > 0)
            GamePlay.gpLogServer(Players.ToArray(), msg, parms);
    }


    //The lines below in OnBattleStarted() start the chat parser.  See method Mission_EventChat for the rest of the code
    //
    //Note that to make this work, you need all (or maybe just most) of these:  
    //
    //  //$reference parts/core/Strategy.dll
    //  //$reference parts/core/gamePlay.dll
    //  using maddox.game;
    //  using maddox.game.world;
    //  using maddox.GP;
    //
    //PLUS you need lines like this in your conf.ini and confs.ini files:
    //
    //  [rts]
    //  scriptAppDomain=0 
    //  ;avoids the dreaded serialization runtime error when running server
    //  ;per http://forum.1cpublishing.eu/showthread.php?t=34797


    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        //listen to events from all missions.
        //Note: this is in Init now, so disabling it here.  See note in Init (below).
        //MissionNumberListener = -1;

        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
        }

        ReadInitialSubmissions(MISSION_ID + "-stats", 1, 1);
        ReadInitialSubmissions(MISSION_ID + "-initsubmission", 10, 1); //so we can include initsubmissions if we want

        //Delete any old CampaignSummary.txt files so that they are not hanging around causing trouble
        try
        {
            File.Delete(STATSCS_FULL_PATH + "CampaignSummary.txt");
        }
        catch (Exception ex) { Console.WriteLine("CampaignSummary Delete: " + ex.ToString()); }
    }

    /****************************************
     * READINITIALSUBMISSIONS
     * 
     * Loads any files in the FILE_PATH that match the pattern (anything or nothing) + filenameID + (anything or nothing) + .mis
     * wait tells how many seconds to wait before starting to load, timespread will spread multiple initialsubmission loads
     * over a certain time period (seconds)
     * 
     * **************************************/

    public void ReadInitialSubmissions(string filenameID, int timespread = 60, int wait = 0)
    {
        List<string> InitSubMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH, filenameID); // gets .mis files with with word filenameID in them
                                                                                                     //string[] InitSubMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH, filenameID); // gets .mis files with with word filenameID in them
                                                                                                     //string[] array = Directory.GetFiles(FILE_PATH + @"Airfields\");

        DebugAndLog("Debug: Loading " + InitSubMissions.Count + " missions to load. " + filenameID + " " + CLOD_PATH + FILE_PATH);
        foreach (string s in InitSubMissions)
        {
            //Distribute loading of initial sub-missions over the first timespread seconds
            //If you make each sub-mission small enough it will load without a noticeable stutter
            //If they are large & make a noticeable stutter, probably best to load them all very near the beginning of the mission
            //so that all the stutters happen at that point
            if ((timespread == 0) && (wait == 0))
            {
                GamePlay.gpPostMissionLoad(s);
                DebugAndLog(s + " file loaded");
                Console.WriteLine(s + " file loaded");
            }
            else
            {
                //if (timespread>2 && random.Next(1) == 1) continue; //TESTING, skip 50% of mission loads just to try it
                Timeout(wait + random.Next(timespread), () => {

                    //string temp = @"missions\AirfieldSpawnTest\Airfields\" + Path.GetFileName(s);

                    GamePlay.gpPostMissionLoad(s);

                    //string s2=@"C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\missions\Multi\Fatal\TWC-initsubmission-stationary.mis";
                    //GamePlay.gpPostMissionLoad(s2);
                    //if (DEBUG)
                    //{
                    DebugAndLog(s + " file loaded");
                    Console.WriteLine(s + " file loaded");
                    //}
                });
            }
        }

    }

    /****************************************
     * LOADRANDOMSUBMISSION
     * 
     * Finds all files in the FILE_PATH whose filenames match the pattern (anything or nothing) + fileID + (anything or nothing) + .mis
     * and randomly selects ONE of them to load.
     * 
     * This can be used in mission to, e.g., have a fighter sweep that always launches 12 minutes into the mission.  Instead of 
     * running the same mission each time,  you can
     * have 5 different variants of the mission available, and one of them is selected randomly each time.
     * 
     * subdir is relative to the FILE_PATH and should not include a leading or trailing / or \
     * 
     * Examples:
     * 
     *   LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionBOMBERPATROLBLUE"); // load one of several available submissions
     *   
     *   LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionFIGHTERPATROLRED", "patrols"); // keep sub-missions in subdirectory "patrols" of your main mission directory (FILE_PATH, above)
     * 
     * **************************************/

    public bool LoadRandomSubmission(string fileID = "randsubmission", string subdir ="")
    {
        //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
        //GamePlay.gpHUDLogCenter("Respawning AI air groups");
        //GamePlay.gpLogServer(null, "RESPAWNING AI AIR GROUPS. AI Aircraft groups re-spawn every " + RESPAWN_MINUTES + " minutes and have a lifetime of " + RESPAWN_MINUTES + "-" + 2*RESPAWN_MINUTES + " minutes. The map restarts every " + Convert.ToInt32((float)END_SESSION_TICK/60/TICKS_PER_MINUTE) + " hours.", new object[] { });

        bool ret = false;

        List<string> RandomMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH + "/" + subdir, fileID); // Gets all files with with text MISSION_ID-fileID (like "M001-randsubmissionREDBOMBER") in the filename and ending with .mis
        //if (DEBUG) 


        //random.Next(100) > XX adjust what percentage of AI aicraft sub-missions are actually loaded
        //random.Next(100) > 85 loads 85% of missions, random.Next(100) > 50 loads 50% of missions, random.Next(100) > 100 loads 100% of missions etc.
        if (fileID.StartsWith(MISSION_ID + "-" + "randsubmission") && !fileID.StartsWith(MISSION_ID + "-" + "randsubmissionINITIALSHIPS") && random.Next(100) > PERCENT_SUBMISSIONS_TO_LOAD)
        {
            Console.WriteLine("Skipping load of " + fileID + " to reduce submission files loaded.");
            return ret;
        }
        DebugAndLog("Debug: Choosing from " + RandomMissions.Count + " missions to spawn. " + fileID + " " + CLOD_PATH + FILE_PATH);
        if (RandomMissions.Count > 0)
        {
            string RandomMission = RandomMissions[random.Next(RandomMissions.Count)];
            GamePlay.gpPostMissionLoad(RandomMission);
            ret = true;

            //if (DEBUG) 
            DebugAndLog("Loading mission " + RandomMission);
            Console.WriteLine("Loading mission " + Path.GetFileName(RandomMission));
            DebugAndLog("Current time: " + DateTime.UtcNow.ToString("O"));
        }

        return ret;
        /////////////////////////////////////ends submission calls ////////////////////// 0000000000000000000000000000000000000000000000000000000000000000000000000/////////////

        //GamePlay.gpPostMissionLoad("missions/Multi/Flug/Blue vs Red - Scimitar-Flug-2016-04-19-submis");



    }

    //LOADING OUR RANDOM SUB-MISSIONS//////////////////////////////
    public List<string> GetFilenamesFromDirectory(string dirPath, string mask = null)
    {
        List<string> list = new List<string>();
        string[] filenames = Directory.GetFiles(dirPath, "*" + mask + "*.mis");

        list = new List<string>(filenames);
        DebugAndLog("Num matching submissions found in directory: " + list.Count);
        return list;
    }
    //END MISSION WITH WARNING MESSAGES ETC/////////////////////////////////
    public void EndMission(int endseconds = 0, string winner = "")
    {
        if (winner == "")
        {
            GamePlay.gpLogServer(null, "Mission is restarting soon!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission is restarting soon!!!");
        }
        else
        {
            if (endseconds > 60)
            {
                Timeout(endseconds + 40, () =>
                {
                    GamePlay.gpLogServer(null, winner + " has turned the map!", new object[] { });
                    GamePlay.gpHUDLogCenter(winner + " has turned the map. Congratulations, " + winner + "!");
                });
            }
            Timeout(endseconds / 2, () =>
            {
                GamePlay.gpLogServer(null, winner + " has turned the map!", new object[] { });
                GamePlay.gpHUDLogCenter(winner + " has turned the map. Congratulations, " + winner + "!");
            });
            Timeout(endseconds + 15, () =>
            {
                GamePlay.gpLogServer(null, winner + " has turned the map!", new object[] { });
                GamePlay.gpHUDLogCenter(winner + " has turned the map - mission ending soon!");
            });
            Timeout(endseconds + 45, () =>
            {
                GamePlay.gpLogServer(null, winner + " has turned the map!", new object[] { });
                GamePlay.gpHUDLogCenter(winner + " has turned the map - mission ending soon!");
            });
            Timeout(endseconds + 61, () =>
            {
                GamePlay.gpLogServer(null, "Congratulations " + winner + " for turning the map!", new object[] { });

            });
        }
        Timeout(endseconds, () =>
        {
            GamePlay.gpLogServer(null, "Mission is restarting in 1 minute!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission is restarting in 1 minute!!!");
        });
        Timeout(endseconds + 30, () =>
        {
            GamePlay.gpLogServer(null, "Server Restarting in 30 seconds!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Server Restarting in 30 seconds!!!");
            //SaveMapState(winner); //here is where we save progress/winners towards moving the map & front one way or the other
            CheckStatsData(); //Save campaign/map state just before final exit.  This is important because when we do (GamePlay as GameDef).gameInterface.CmdExec("exit"); to exit, the -stats.cs will read the CampaignSummary.txt file we write here as the final status for the mission in the team stats.
        });
        Timeout(endseconds + 60, () =>
        {
            GamePlay.gpLogServer(null, "Mission ended. Please wait 2 minutes to reconnect!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission ended. Please wait 2 minutes to reconnect!!!");
            DebugAndLog("Mission ended.");

            //OK, trying this for smoother exit (save stats etc)
            if (GamePlay is GameDef)
            {
                (GamePlay as GameDef).gameInterface.CmdExec("exit");
            }
            //GamePlay.gpBattleStop(); //It would be nice to do this but if you do, the script stops here.
        });
        Timeout(endseconds + 90, () =>  //still doing this as a failsafe but allowing 20 secs to save etc
        {
            Process.GetCurrentProcess().Kill();
        });

    }


    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        Console.WriteLine("Battle Stopping");
        //SaveMapState(""); //A call here just to be safe; we can get here if 'exit' is called etc, and the map state may not be saved yet . . . 
        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
    }

    //Listen to events of every mission
    public override void Init(maddox.game.ABattle battle, int missionNumber)
    {
        base.Init(battle, missionNumber);
        MissionNumberListener = -1; //Listen to events of every mission
                                    //This is what allows you to catch all the OnTookOff, OnAircraftDamaged, and other similar events.  Vitally important to make this work!
                                    //If we load missions as sub-missions, as we often do, it is vital to have this in Init, not in "onbattlestarted" or some other place where it may never be detected or triggered if this sub-mission isn't loaded at the very start.
                                    //Initialize the mission objectives etc but wait until after initial submissions are loaded

        //This needs to be in Init (not Mission constructor) because it relies on some Battle. stuff that isn't initialized yet at Mission constructor
        //(For now this is unnecessary but in future we might load things in initial submissions & then mission_objectives potentially affected by them afterwards)
        //Timeout(30, () => { mission_objectives = new MissionObjectives(this); });
        //Timeout(1, () => { mission_objectives = new MissionObjectives(this); }); //testing
        mission_objectives = new MissionObjectives(this);

        //For testing
        /*
        Timeout(30, () => {MO_DestroyObjective("BTarget14R");
            MO_DestroyObjective("BTarget22R");
            MO_DestroyObjective("RTarget28R");
            MO_DestroyObjective("RTarget29R");


        });
        */
        


    }

    //////////////////////////////////////////////////////////////////////////////////////////////////

    ///MENU SYSTEM////////////////////////////

    bool dmgOn = false;
    bool EndMissionSelected = false;
    bool debugMenu = false;
    bool debugSave;
    int radar_realismSave;
    private void setMainMenu(Player player)
    {
        //GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Server Options - Users" }, new bool[] { true });
        if (admin_privilege_level(player) >= 1)
        {
            //ADMIN option is set to #9 for two reasons: #1. We can add or remove other options before it, up to 8 other options, without changing the Tab-4-9 admin access.  #2. To avoid accessing the admin menu (and it's often DANGEROUS options) by accident
            //{true/false bool array}: TRUE here indicates that the choice is a SUBMENU so that when it is selected the user menu will be shown.  If FALSE the user menu will disappear.  Also it affects the COLOR of the menu items, which seems to be designed to indicate whether the choice is going to DO SOMETHING IMMEDIATE or TAKE YOU TO ANOTHER MENU
            GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", "Mission Objectives", "", "", "", "", "Admin options" }, new bool[] { false, false, false, false, false, false, false, false, true });
        }
        else
        {
            GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", "Mission Objectives" }, new bool[] { false, false, false, false });

        }
    }

    private void setSubMenu1(Player player)
    {
        if (admin_privilege_level(player) >= 1)
        {

            string rollovertext = "(admin) End mission now/roll over to next mission";
            if (EndMissionSelected) rollovertext = "(admin) CANCEL End mission now command";
            if (admin_privilege_level(player) == 2)
                GamePlay.gpSetOrderMissionMenu(player, true, 1, new string[] { "(admin) Show detailed damage reports for all players (toggle)", "(admin) Toggle debug mode", "", rollovertext, "Return to User Menu" }, new bool[] { false, false, false, false, true });
            else GamePlay.gpSetOrderMissionMenu(player, true, 1, new string[] { "", "", "", rollovertext, "Return to User Menu" }, new bool[] { false, false, false, false, true });
        }
        else
        {
            setMainMenu(player);

        }
    }

    private void setSubMenu2(Player player)
    {
        //GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Spawn New AI Groups Now", "Dogfight mode: Remove AI Aircraft, stop new spawns (30 minutes)", "Delete all current AI Aircraft", "Show damage reports for all players", "Stop showing damage reports for all players"}, new bool[] { false, false, false, false, false } );
    }

    //object plnameo= GamePlay.gpPlayer().Name();  
    //string plname= GamePlay.gpPlayer().Name() as string;
    public override void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex)
    {
        //base.OnOrderMissionMenuSelected(player, ID, menuItemIndex); //2015/05/16 - not sure why this was missing previously? We'll see . . .

        /*****************************************************
         * 
         * ADMIN SUBMENU (2nd submenu, ID ==1, Tab-4-8)
         * 
         *****************************************************/
        if (ID == 1)
        { // main menu

            if (menuItemIndex == 0)
            {
                setSubMenu1(player);
                setMainMenu(player);
            }

            //start/stop display a/c damage inflicted info/////////////////////////// 
            else if (menuItemIndex == 1)
            {
                if (admin_privilege_level(player) == 2)
                {
                    dmgOn = !dmgOn;
                    if (dmgOn)
                    {
                        GamePlay.gpHUDLogCenter("Will show damage on all aircraft");
                        GamePlay.gpLogServer(new Player[] { player }, "Detailed damage reports will be shown for all players", new object[] { });

                    }
                    else
                    {
                        GamePlay.gpHUDLogCenter("Will not show damage on all aircraft");
                        GamePlay.gpLogServer(new Player[] { player }, "Detailed damage reports turned off", new object[] { });
                    }
                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 2)
            {
                if (admin_privilege_level(player) == 2)
                {
                    debugMenu = !debugMenu;
                    if (debugMenu)
                    {
                        GamePlay.gpLogServer(new Player[] { player }, "Debug & detailed radar ON for all users - extra debug messages & instant, detailed radar", new object[] { });
                        radar_realismSave = RADAR_REALISM;
                        DEBUG = true;
                        RADAR_REALISM = 0;

                    }
                    else
                    {
                        GamePlay.gpLogServer(new Player[] { player }, "Debug & detailed radar OFF", new object[] { });
                        RADAR_REALISM = radar_realismSave;
                        DEBUG = false;

                    }
                }

                setMainMenu(player);
            }

            //Display Stats
            //WritePlayerStat(player)
            /*
             * else if (menuItemIndex == 3)
            {
                if (admin_privilege_level(player) == 2)
                {
                    string str = WritePlayerStat(player);
                    //split msg into a few chunks as gplogserver doesn't like long msgs
                    int maxChunkSize = 100;
                    for (int i = 0; i < str.Length; i += maxChunkSize)
                        GamePlay.gpLogServer(new Player[] { player }, str.Substring(i, Math.Min(maxChunkSize, str.Length - i)), new object[] { });
                }

                setMainMenu(player);
            }
            */
            else if (menuItemIndex == 4)
            {
                if (admin_privilege_level(player) >= 1)
                {
                    if (EndMissionSelected == false)
                    {
                        EndMissionSelected = true;
                        GamePlay.gpLogServer(new Player[] { player }, "ENDING MISSION!! If you want to cancel the End Mission command, use Tab-4-9-4 again.  You have 30 seconds to cancel.", new object[] { });
                        Timeout(30, () => {
                            if (EndMissionSelected)
                            {
                                EndMission(0);
                            }
                            else
                            {
                                GamePlay.gpLogServer(new Player[] { player }, "End Mission CANCELLED; Mission continuing . . . ", new object[] { });
                                GamePlay.gpLogServer(new Player[] { player }, "If you want to end the mission, you can use the menu to select Mission End again now.", new object[] { });
                            }

                        });

                    }
                    else
                    {
                        GamePlay.gpLogServer(new Player[] { player }, "End Mission CANCELLED; Mission will continue", new object[] { });
                        EndMissionSelected = false;

                    }
                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 5)
            {

                setMainMenu(player);
            }




            //Respawn/rearm   
            else if (menuItemIndex == 9)
            {
                GamePlay.gpLogServer(new Player[] { player }, "Re-spawn: This option not working yet", new object[] { });
                //Spawn in mission file with 1 copy of any/all needed aircraft included
                //copy the one matching the player's plane to the player's current spot or nearby
                //also copy existing plane's position, direction, location etc etc etc
                //move player to new a/c 
                //player.PlaceEnter(aircraft,0);
                //destroy old a/c
                setMainMenu(player);
            }
            else
            { //make sure there is a catch-all ELSE or ELSE menu screw-ups WILL occur
                setMainMenu(player);
            }

            /* else if( menuItemIndex == 2 ) {
                setSubMenu1( player );
                /* if ( player.Name().Substring(0,3) == @"TWC") {
                    setSubMenu2( player );
                  } else {
                    GamePlay.gpLogServer(new Player[] { player }, player.Name() + " is not authorized", new object[] { }); 
                    setSubMenu1( player );
                  }
                 */

            //}

            /*****************************************************
             * 
             * USER SUBMENU (1st submenu, ID == 0, Tab-4)
             * 
             *****************************************************/
        }
        else if (ID == 0)
        { // sub menu

            if (menuItemIndex == 0)
            {
                //setSubMenu1(player);
                setMainMenu(player);
            }
            else if (menuItemIndex == 1)
            {
                Player[] all = { player };
                listPositionAllAircraft(player, player.Army(), false); //enemy a/c  
                if (DEBUG)
                {
                    DebugAndLog("Total number of AI aircraft groups currently active:");
                    if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
                    {

                        int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                        DebugAndLog(totalAircraft.ToString());
                        //GamePlay.gpLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
                    }
                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 2)
            {
                Player[] all = { player };
                listPositionAllAircraft(player, player.Army(), true); //friendly a/c           
                if (DEBUG)
                {
                    DebugAndLog("Total number of AI aircraft groups currently active:");
                    if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
                    {

                        int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                        DebugAndLog(totalAircraft.ToString());
                        //GamePlay.gpLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
                    }
                }
                setMainMenu(player);
                //TIME REMAINING ETC//////////////////////////////////  
            }
            else if (menuItemIndex == 3)
            {
                //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
                showTimeLeft(player);
                //Experiment to see if we could trigger chat commands this way; it didn't work
                //GamePlay.gpLogServer(new Player[] { player }, "<air", new object[] { });
                //GamePlay.gpLogServer(new Player[] { player }, "<ter", new object[] { });

                setMainMenu(player);
            }
            else if (menuItemIndex == 4)
            {
                /*
                 * Display objectives completed 
                 */

                GamePlay.gpLogServer(new Player[] { player }, "Completed Red Objectives (" + InitialRedObjectiveCount.ToString() + " points):", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, (Objective_Total_Red), new object[] { });
                Timeout(2, () =>
                GamePlay.gpLogServer(new Player[] { player }, "Completed Blue Objectives (" + InitialBlueObjectiveCount.ToString() + " points):", new object[] { }));
                Timeout(3, () =>
                GamePlay.gpLogServer(new Player[] { player }, (Objective_Total_Blue), new object[] { }));
                stopAI();//for testing

                setMainMenu(player);
            }
            //ADMIN sub-menu
            else if (menuItemIndex == 9)
            {
                setSubMenu1(player);
            }
            else
            { //make sure there is a catch-all ELSE or ELSE menu screw-ups WILL occur
                setMainMenu(player);
            }

            //immediate end of mission///////////////


        } //menu if   
    } // method


    /****************************************************************
     * 
     * ADMIN PRIVILEGE
     * 
     * Determine if player is an admin, and what level
     * 
     ****************************************************************/

    public int admin_privilege_level(Player player)
    {
        if (player == null || player.Name() == null) return 0;
        string name = player.Name();
        //name = "TWC_muggle"; //for testing
        if (admins_full.Contains(name)) return 2; //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
        if (admins_basic.Any(name.Contains)) return 1; //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
        return 0;

    }

    //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
    public override void OnPlayerConnected(Player player)
    {
        string message;
        //Not starting it here due to Coop Start Mode
        //if (!MISSION_STARTED) DebugAndLog("First player connected; Mission timer starting");
        //MISSION_STARTED = true;

        if (MissionNumber > -1)
        {
            setMainMenu(player);

            GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });

            DateTime utcDate = DateTime.UtcNow;

            //utcDate.ToString(culture), utcDate.Kind
            //Write current time in UTC, what happened, player name
            message = utcDate.ToString("u") + " Connected " + player.Name();
        }
    }

    //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
    public override void OnPlayerDisconnected(Player player, string diagnostic)
    {
        string message;
        if (MissionNumber > -1)
        {

            DateTime utcDate = DateTime.UtcNow;

            //utcDate.ToString(culture), utcDate.Kind
            //Write current time in UTC, what happened, player name
            message = utcDate.ToString("u") + " Disconnected " + player.Name() + " " + diagnostic;
            DebugAndLog(message);
        }
    }

    public override void OnPlayerArmy(Player player, int Army)
    {
        if (MissionNumber > -1)
        {
            /* AiAircraft aircraft = (player.Place() as AiAircraft);
                            string cs = aircraft.CallSign();
                            //int p = part.ParameterTypes.I_VelocityIAS; 
                            double ias = (double) aircraft.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
                            GamePlay.gpLogServer(new Player[] { player }, "Plane: "  
                            + cs + " " + ias, new object[] { });
            */
            //We re-init menu & mission_started here bec. in some situations OnPlayerConnected never happens.  But, they
            //always must choose their army before entering the map, so this catches all players before entering the actual gameplay
            setMainMenu(player);
            GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
        }
    }
    public override void Inited()
    {
        if (MissionNumber > -1)
        {

            setMainMenu(GamePlay.gpPlayer());
            GamePlay.gpLogServer(null, "Welcome " + GamePlay.gpPlayer().Name(), new object[] { });

            Timeout(90, () => { SetAirfieldTargets(); });


        }
    }





    /////////////////////

    /////////////////////////CHAT COMMANDS//////////////////////////////////////////////////////////////

    /* public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        if (GamePlay is GameDef)
        {
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
        }
    } */

    //The lines below implement the the chat parser.  See method  OnBattleInit() for how to initialize it.
    //
    //Note that to make this work, you need all (or maybe just most) of these:  
    //
    //  //$reference parts/core/Strategy.dll
    //  //$reference parts/core/gamePlay.dll
    //  using maddox.game;
    //  using maddox.game.world;
    //  using maddox.GP;
    //
    //PLUS you need lines like this in your conf.ini and confs.ini files:
    //
    //  [rts]
    //  scriptAppDomain=0 
    //  ;avoids the dreaded serialization runtime error when running server
    //  ;per http://forum.1cpublishing.eu/showthread.php?t=34797
    //
    // PLUS you need code like this in OnBattleInit() to get it initialized:
    //
    // if (GamePlay is GameDef) (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
    //
    // PLUS you need code like this in OnBattleStoped() to remove the chat parser when you're done with it:
    //
    // (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
    //
    //If we don't remove the new EventChat when the battle is stopped
    //we tend to get several copies of it operating, if we're not careful
    //
    //BONUS: How to send a command to server:
    // public void Chat(string line, Player player)
    //{
    //  if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("chat " + line + " TO " + player.Name());
    //}
    //And, server commands (not all of them may work or be sensible to use from a script):
    /*
     ?         admin     alias     ban       channel   chat
   console   del       deny      difficulty exit     expel
   f         file      help      history   host      kick
   kick#     mp_dotrange param   sc        secure    set
   show      socket    timeout

   */


    void Mission_EventChat(IPlayer from, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 
        string msg_orig = msg;
        msg = msg.ToLower();
        Player player = from as Player;
        if (msg.StartsWith("<tl"))
        {
            showTimeLeft(from);
            //GamePlay.gp(, from);

        }
        else if (msg.StartsWith("<pos") && admin_privilege_level(player) >= 2)
        {
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            RADAR_REALISM = 0;
            listPositionAllAircraft(player, player.Army(), true);
            listPositionAllAircraft(player, player.Army(), false);
            RADAR_REALISM = saveRealism;

        }
        else if (msg.StartsWith("<rad"))
        {
            listPositionAllAircraft(player, player.Army(), false); //enemy a/c  
        }
        else if (msg.StartsWith("<apall"))
        {
            ListAirfieldTargetDamage(player, -1, true);//list ALL airports, damaged or not, of both teams
        }
        else if (msg.StartsWith("<ap"))
        {
            ListAirfieldTargetDamage(player, -1);//list damaged airport of both teams
        }
        else if (msg.StartsWith("<trigger") && admin_privilege_level(player) >= 2)
        {

            
            string tr = msg_orig.Substring(8).Trim();

            GamePlay.gpLogServer(new Player[] { player }, "Trying to activate trigger " + tr, new object[] { });

            if (GamePlay.gpGetTrigger(tr) != null ) { 
                GamePlay.gpGetTrigger(tr).Enable = true;
                //GamePlay.gpGetTrigger(tr).Active = true;
                GamePlay.gpLogServer(new Player[] { player }, "Enabled trigger " + tr, new object[] { });
            }

            //this.OnTrigger(1, tr, true);

            Battle.OnEventGame(GameEventId.Trigger, tr, true, 1);
            
            /*
            AiAction action = GamePlay.gpGetAction("action1");

            if (action != null)
            {
                action.Do();
            }
            */
        }
        else if (msg.StartsWith("<action") && admin_privilege_level(player) >= 2)
        {

            
            string tr = msg_orig.Substring(7).Trim();            

            AiAction action = GamePlay.gpGetAction(tr);

            if (action != null)
            {
                action.Do();
                GamePlay.gpLogServer(new Player[] { player }, "Activating action " + tr, new object[] { });
            } else
            {
                GamePlay.gpLogServer(new Player[] { player }, "Didn't find action " + tr + "! No action taken.", new object[] { });
            }
            


        }
        else if (msg.StartsWith("<debugon") && admin_privilege_level(player) >= 2)
        {

            DEBUG = true;
            GamePlay.gpLogServer(new Player[] { player }, "Debug is on", new object[] { });

        }

        else if (msg.StartsWith("<debugoff") && admin_privilege_level(player) >= 2)
        {

            DEBUG = false;
            GamePlay.gpLogServer(new Player[] { player }, "Debug is off", new object[] { });

        }
        else if (msg.StartsWith("<logon") && admin_privilege_level(player) >= 2)
        {

            LOG = true;
            GamePlay.gpLogServer(new Player[] { player }, "Log is on", new object[] { });

        }
        else if (msg.StartsWith("<logoff") && admin_privilege_level(player) >= 2)
        {

            LOG = false;
            GamePlay.gpLogServer(new Player[] { player }, "Log is off", new object[] { });

        }

        else if ((msg.StartsWith("<help") || msg.StartsWith("<")) &&
            //Don't give our help when any of these typical -stats.cs chat commands are entered
            !(msg.StartsWith("<car") || msg.StartsWith("<ses") || msg.StartsWith("<rank") || msg.StartsWith("<rr")
            || msg.StartsWith("<ter") || msg.StartsWith("<air") || msg.StartsWith("<ac") || msg.StartsWith("<nextac"))

            )
        {
            Timeout(0.1, () =>
            {
                GamePlay.gpLogServer(new Player[] { player }, "Commands: <tl Time Left; <rr How to reload; <rad radar", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "<ap & <apall Airport condition", new object[] { });
                //GamePlay.gpLogServer(new Player[] { player }, "<coop Use Co-Op start mode only @ beginning of mission", new object[] { });
                //GamePlay.gp(, from);
            });
        }
    }


    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);

        if (player != null)
        {
            setMainMenu(player);
        }

        if (actor != null && actor is AiAircraft)
        {
            //check limited aircraft
            switch ((actor as AiAircraft).InternalTypeName())
            {
                case "bob:Aircraft.SpitfireMkIIa":
                    {
                        currentSpitIIas++;
                        if (currentSpitIIas > allowedSpitIIas)
                        {
                            damagePlayerGroup(actor);
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "SpitIIa aircraft limit reached. Choose a different plane! Plane disabled in 10 seconds!");
                        }
                        break;
                    }
                case "bob:Aircraft.Bf-109E-4N":
                    {
                        current109s++;
                        if (current109s > allowed109s)
                        {
                            damagePlayerGroup(actor);
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "109 E-4N aircraft limit reached. Choose a different plane! Plane disabled in 10 seconds!");
                        }
                        break;
                    }

            }
        }
    }

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceLeave(player, actor, placeIndex);

        if (actor != null && actor is AiAircraft)
        {
                    //OK, we have to wait a bit here bec. some ppl use ALT-F11 (ALT-F2) for 'external view' which allows to leave two positions
        //inhabited by bomber pilot & just return to the one position.  But how it actually works is the pilot leaves the aircraft momentarily.
        Timeout(0.5f, () =>
        {
            string pName = "";
            if (player != null) pName = player.Name();
            if (actor is AiAircraft) {

                if (isAiControlledPlane2(actor as AiAircraft))
                {
                    Timeout(0.5f, () => //5 sec seems too long, the ai vigorously takes control sometimes, and immediately.  Perhaps even 1 second or .5 better than 2.
                        {
                            if (isAiControlledPlane2(actor as AiAircraft))
                            {
                                damageAiControlledPlane(actor);
                                Console.WriteLine("Player has left plane; damaged aircraft so that AI cannot assume control " + pName + " " + (actor as AiAircraft).Type());
                                //check limited aircraft
                                switch ((actor as AiAircraft).InternalTypeName())
                                {

                                    case "bob:Aircraft.SpitfireMkIIa":
                                        currentSpitIIas--;
                                        break;
                                    case "bob:Aircraft.Bf-109E-4N":
                                        current109s--;
                                        break;

                                }

                            }
                        }
                    );
                }
            }
            DateTime utcDate = DateTime.UtcNow;
            //logStats(utcDate.ToString("u") + " " + player.Name() + " " + WritePlayerStat(player));
        }
        );

        }

    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        base.OnActorDead(missionNumber, shortName, actor, damages);
        try
        {
            if (actor != null && actor is AiAircraft)
            {
                //if dead, then destroy it within a reasonable time
                AiAircraft aircraft = actor as AiAircraft;
                string pName = actor.Name();
                if (aircraft != null)
                {
                    //Timeout(300, () =>
                    Timeout(20, () => //testing
                    {
                        //Force a player into a certain place:
                        //Player.Place() = (Actor as AiAircraft).Place(placeIndex);
                        for (int i = 0; i < aircraft.Places(); i++)
                        {
                            //aircraft.Player(i).Place() = null;
                            //aircraft.Player(i).PlaceEnter(null,0);
                            aircraft.Player(i).PlaceLeave(i);
                        }

                        //Wait 0.5 second for player(s) to leave, then destroy
                        Timeout(0.5, () =>
                        {
                            destroyPlane(aircraft);  //Destroy completely when dead, after a reasonable time period.
                            Console.WriteLine("Destroyed dead aircraft " + pName + " " + aircraft.Type());
                        });

                    });
                }

            }

            if (actor != null && actor is AiGroundActor)
            {
                //If we destroy dead ground objs too soon then eg big oil refinery fires will go out after just a few minutes
                //Ideally we'd have a filter of some time here to destroy smaller items pretty soon but other bigger ones after a longer time
                Timeout(90 * 60, () => {
                    (actor as AiGroundActor).Destroy();
                    Console.WriteLine("Destroyed dead ground object " + actor.Name());

                });

                Console.WriteLine("Ground object has died. Name: " + actor.Name());

            }



        }
        catch (Exception ex) { Console.WriteLine("OPD: " + ex.ToString()); }
    }

    public override void OnPersonHealth(maddox.game.world.AiPerson person, maddox.game.world.AiDamageInitiator initiator, float deltaHealth)
    {
        #region stats
        base.OnPersonHealth(person, initiator, deltaHealth);
        try
        {
            //GamePlay.gpLogServer(null, "Health Changed for " + person.Player().Name(), new object[] { });
            if (person != null)
            {
                Player player = person.Player();
                //if (deltaHealth>0 && player != null && player.Name() != null) {
                if (player != null && player.Name() != null)
                {
                    if (DEBUG) GamePlay.gpLogServer(null, "Main: OnPersonHealth for " + player.Name() + " health " + player.PersonPrimary().Health.ToString("F2"), new object[] { });
                    //if the person is completely dead we are going to force them to leave their place
                    //This prevents zombie dead players from just sitting in their planes interminably, 
                    //which clogs up the airports etc & prevents the planes from dying & de-spawning
                    //Not really sure the code below is working.
                    if (player.PersonPrimary() != null && player.PersonPrimary().Health == 0
                        && (player.PersonSecondary() == null
                            || (player.PersonSecondary() != null && player.PersonSecondary().Health == 0)))
                    {
                        //Timeout(300, () =>
                        if (DEBUG) GamePlay.gpLogServer(null, "Main: 2 OnPersonHealth for " + player.Name(), new object[] { });
                        Timeout(20, () => //testing
                        {
                            if (DEBUG) GamePlay.gpLogServer(null, "Main: 3 OnPersonHealth for " + player.Name(), new object[] { });
                            //Checking health a second time gives them a while to switch to a different position if
                            //it is available
                            if (player.PersonPrimary() != null && player.PersonPrimary().Health == 0
                                && (player.PersonSecondary() == null
                                    || (player.PersonSecondary() != null && player.PersonSecondary().Health == 0)))
                            {
                                if (DEBUG) GamePlay.gpLogServer(null, "Main: 4 OnPersonHealth for " + player.Name(), new object[] { });

                                //Not really sure how this works, but this is a good guess.  
                                //if (player.PersonPrimary() != null )player.PlaceLeave(0);
                                //if (player.PersonSecondary() != null) player.PlaceLeave(1);
                                if (player.PersonPrimary() != null) player.PlaceLeave(player.PersonPrimary().Place());
                                if (player.PersonSecondary() != null) player.PlaceLeave(player.PersonSecondary().Place());
                            }
                            if (DEBUG) GamePlay.gpLogServer(null, player.Name() + " died and was forced to leave player's current place.", new object[] { });

                            if (DEBUG) GamePlay.gpLogServer(null, "Main: OnPersonHealth for " + player.Name() + " health1 " + player.PersonPrimary().Health.ToString("F2")
                                    + " health2 " + player.PersonSecondary().Health.ToString("F2"), new object[] { });

                        });

                    }

                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Main.OnPersonHealth - Exception: " + ex.ToString());
        }
        #endregion

    }


    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        Timeout(300, () =>
        //{ destroyPlane(aircraft); } //Not sure why to destory all planes just bec. crash landed?  Best to check if a pilot is still in it & just destroy aicontrolled planes, like this:

        { destroyAiControlledPlane(aircraft); }
            );
    }
    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        Timeout(300, () =>
        //{ destroyPlane(aircraft); } //Not sure why to destory **ALL** planes just bec. landed?  Best to check if a pilot is still in it & just destroy aicontrolled planes, like this:

        { destroyAiControlledPlane(aircraft); }
            );
    }

    //this will destroy ALL ai controlled aircraft on the server
    public void destroyAIAircraft(Player player)
    {


        //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in GamePlay.gpArmies())
            {
                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                {
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft a = actor as AiAircraft;
                                    if (a != null && isAiControlledPlane2(a))
                                    {


                                        /* if (DEBUG) GamePlay.gpLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                         + a.CallSign() + " " 
                                         + a.Type() + " " 
                                         + a.TypedName() + " " 
                                         +  a.AirGroup().ID(), new object[] { });
                                        */
                                        a.Destroy();

                                    }


                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /******************************************************************************************************************** 
     * MISSION OBJECTIVES CLASSES & METHODS
     * 
     * Methods & classes for dealing with objectives, messages & other results of destroying objectives, awarding points, dealing with disabled radar, etc
     * 
     * All Mission Objectives should be listed & handled here, then a simple routine below can be called from OnTrigger, OnBombExploded, etc
     * rather than having code & variables related to objectives scattered hither & yon across the entire file
     * 
     * ******************************************************************************************************************/
    public double InitialBlueObjectiveCount = 0;
    public double InitialRedObjectiveCount = 0;
    public string Objective_Total_Blue = "";
    public string Objective_Total_Red = "";

    //TODO: This percentage is not operative yet
    public Dictionary<ArmiesE, double> MO_PercentPrimaryTargetsRequired = new Dictionary<ArmiesE, double>() {
        {ArmiesE.Red, 75 },
        {ArmiesE.Blue, 75 }
    };

    //TODO: Use similar scheme for total points, objectives completed list, objectives completed
    public Dictionary<ArmiesE, double> MO_PointsRequired = new Dictionary<ArmiesE, double>() {
        {ArmiesE.Red, 12 },
        {ArmiesE.Blue, 12 }
    };

    Dictionary<string, MissionObjective> MissionObjectivesList = new Dictionary<string, MissionObjective>();
    Dictionary<ArmiesE, List<MissionObjective>> DestroyedObjectives = new Dictionary<ArmiesE, List<MissionObjective>>() {
        {ArmiesE.Red, new List<MissionObjective>() },
        {ArmiesE.Blue, new List<MissionObjective>() }
    };  //reference as DestroyedObjectives[ArmiesE.Red] DestroyedRadar[ArmiesE.Blue]

    Dictionary<ArmiesE, List<MissionObjective>> DestroyedRadar = new Dictionary<ArmiesE, List<MissionObjective>>() {
        {ArmiesE.Red, new List<MissionObjective>() },
        {ArmiesE.Blue, new List<MissionObjective>() }
    };  //reference as DestroyedRadar[ArmiesE.Red] DestroyedRadar[ArmiesE.Blue]



    public enum MO_TriggerType { Trigger, Static };
    public enum MO_ObjectiveType { Radar, AA, Ship, Building, Fuel, Airport, Aircraft, Vehicles, Bridge, Dam, Dock, RRStation, Railroad, Road };

    public class MissionObjective
    {
        //public string TriggerName { get; set; }
        public string ID { get; set; } //unique name, often the Triggername or static name
        public string Name { get; set; } //Name the will be displayed to the public in messages etc
        public int AttackingArmy { get; set; } // Army this is an objective for (ie, whose task is to destroy it); can be 1=red, 2=blue,0=none
        public int OwnerArmy { get; set; } // Army that owns this object (ie, is harmed if it is destroyed)
        public Mission.MO_ObjectiveType MOObjectiveType { get; set; }
        public Mission.MO_TriggerType MOTriggerType { get; set; }
        public bool IsPrimaryTarget { get; set; } //One of the primary/required targets for this mission?
        public bool InPrimaryTargetPool { get; set; } //If we select primary targets randomly etc, is this one that could be selected?
        public double Points { get; set; }
        public bool Destroyed { get; set; }
        public Point3d Pos { get; set; }
        public string Sector { get; set; }
        public string HUDMessage { get; set; }
        public string LOGMessage { get; set; }
        public string SuccessSubmissionName { get; set; } //submission to launch when objective reached; if blank nothing launched
        public double RadarEffectiveRadius { get; set; }
        public string TriggerName { get; set; }
        public string TriggerType { get; set; }
        public double TriggerPercent { get; set; }
        public double TriggerDestroyRadius { get; set; }
        public List<string> StaticNames { get; set; } //for static targets, the list of static names that will determine if the target is destroyed
        public double StaticPercentageRequired { get; set; } //what percentage of those static targets must be destroyed to eliminate the objective
        public List<string> StaticRemoveNames { get; set; } //what statics to remove when the object is destroyed (allows eg dams to be breached by removal of certain portions)
        public double StaticRemoveDelay_sec { get; set; } //how long to wait after target destruction before removing static objects in list
        public double StaticRemoveSpread_sec { get; set; } //how long to spread out the static target destruction
        public string Comment { get; set; } //PRIVATE comment, ie for developers, internal notes, etc
        public Mission msn;
        public MissionObjective(Mission m)
        {
            msn = m;
        }
        //RADAR TRIGGER initiator
        public MissionObjective(Mission m, string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, double e, bool pt, bool ptp, string comment) {

            msn = m;
            MOObjectiveType = MO_ObjectiveType.Radar;
            MOTriggerType = MO_TriggerType.Trigger;            
            TriggerName = tn;
            ID = tn;
            Name = n;
            
            OwnerArmy = ownerarmy;
            AttackingArmy = 3 - ownerarmy;
            if (AttackingArmy > 2 || AttackingArmy < 1) AttackingArmy = 0;
            if (AttackingArmy != 0)
            {
                HUDMessage = "Red destroyed " + Name;
                LOGMessage = "Heavy damage to " + Name + " - good job " + ArmiesL[AttackingArmy] + "!!!";
            }
            else
            {
                HUDMessage = Name + " was destroyed";
                LOGMessage = Name + " was destroyed";
            }

            Points = pts;
            TriggerType = t;
            TriggerPercent = p;
            Pos = new Point3d(x, y, 0);
            Sector = msn.GamePlay.gpSectorName(x,y).ToString();
            Sector = Sector.Replace(",", ""); // remove the comma
            TriggerDestroyRadius = d;
            RadarEffectiveRadius = e;
            Destroyed = false;


            IsPrimaryTarget = pt;
            InPrimaryTargetPool = ptp;
            Comment = comment;
        }

        //TRIGGER initiator (for all types except RADAR)
        public MissionObjective(Mission m, MO_ObjectiveType mot,  string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, bool ptp, string comment)
        {

            msn = m;
            MOObjectiveType = mot;
            MOTriggerType = MO_TriggerType.Trigger;
            TriggerName = tn;
            ID = tn;
            Name = n;

            OwnerArmy = ownerarmy;
            AttackingArmy = 3 - ownerarmy;
            if (AttackingArmy > 2 || AttackingArmy < 1) AttackingArmy = 0;
            if (AttackingArmy != 0)
            {
                HUDMessage = "Red destroyed " + Name;
                LOGMessage = "Heavy damage to " + Name + " - good job " + ArmiesL[AttackingArmy] + "!!!";
            }
            else
            {
                HUDMessage = Name + " was destroyed";
                LOGMessage = Name + " was destroyed";
            }

            Points = pts;
            TriggerType = t;
            TriggerPercent = p;
            Pos = new Point3d(x, y, 0);
            Sector = msn.GamePlay.gpSectorName(x, y).ToString();
            Sector = Sector.Replace(",", ""); // remove the comma
            TriggerDestroyRadius = d;            
            Destroyed = false;


            IsPrimaryTarget = pt;
            InPrimaryTargetPool = ptp;
            Comment = comment;
        }
    }

    //List<MissionObjective> BlueDestroyedRadar = new List<MissionObjective>();

    public class MissionObjectives
    {
        private Mission msn;

        public MissionObjectives(Mission mission)
        {
            msn = mission;
            RadarPositionTriggersSetup();            
            MissionObjectiveTriggersSetup();
        }

        public void addRadar(string n, int ownerarmy, double pts, string tn, string t, double p, double x, double y, double d, double e, bool pt, bool ptp, string comment)
        {
            msn.MissionObjectivesList.Add(tn, new MissionObjective(msn, tn, n, ownerarmy, pts, t, p, x, y, d, e, pt, ptp, comment));
        }

        public void addTrigger(MO_ObjectiveType mot, string n, int ownerarmy, double pts, string tn, string t="", double p=50, double x=0, double y=0, double d=100, bool pt=false, bool ptp=false, string comment ="")
        {
            //MissionObjective                                    (Mission m, MO_ObjectiveType mot,  string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, bool ptp, string comment)
            msn.MissionObjectivesList.Add(tn, new MissionObjective(msn,       mot,                   tn,       n,          ownerarmy,   pts,         t,         p,       x,        y,       d,          pt,     ptp,       comment));
        }
        public void RadarPositionTriggersSetup()
        {
            //MissionObjective(Name, OwnerArmy, points, ID, Trigger Type, Trigger percentage, location x, location y, trigger radius, radar effective radius, isPrimaryTarget, isInPrimaryTargetPool) {
            //ID is the ID used in the [Trigger] portion of the .mis file. The central portion of the line can be copy/pasted from the  .mis file (then lightly edited)
            addRadar("Westgate Radar", 1, 1, "BTarget14R", "TGroundDestroyed", 39, 244791, 262681, 150, 25000, false, false, "");
            addRadar("Sandwich Radar", 1, 1, "BTarget15R", "TGroundDestroyed", 75, 248739, 253036, 200, 25000, false, false, "");
            addRadar("Deal Radar", 1, 1, "BTarget16R", "TGroundDestroyed", 75, 249454, 247913, 200, 25000, false, false, "");
            addRadar("Dover Radar", 1, 1, "BTarget17R", "TGroundDestroyed", 75, 246777, 235751, 200, 25000, false, false, "");
            addRadar("Brookland Radar", 1, 1, "BTarget18R", "TGroundDestroyed", 75, 212973, 220079, 200, 25000, false, false, "");
            addRadar("Dungeness Radar", 1, 1, "BTarget19R", "TGroundDestroyed", 50, 221278, 214167, 200, 25000, false, false, "");
            addRadar("Eastbourne Radar", 1, 1, "BTarget20R", "TGroundDestroyed", 75, 178778, 197288, 200, 25000, false, false, "");
            addRadar("Littlehampton Radar", 1, 1, "BTarget21R", "TGroundDestroyed", 76, 123384, 196295, 200, 25000, false, false, "");
            addRadar("Ventnor Radar", 1, 1, "BTarget22R", "TGroundDestroyed", 75, 70423, 171706, 200, 25000, false, false, "");
            addRadar("Radar Communications HQ", 1, 6, "BTarget28", "TGroundDestroyed", 61, 180207, 288435, 200, 100000, false, false, "");
            addRadar("Oye Plage Freya Radar", 2, 1, "RTarget28R", "TGroundDestroyed", 61, 294183, 219444, 50, 35000, false, false, "");
            addRadar("Coquelles Freya Radar", 2, 1, "RTarget29R", "TGroundDestroyed", 63, 276566, 214150, 50, 35000, false, false, "");
            /*
            BTarget15R TGroundDestroyed 75 248739 253036 200
            BTarget16R TGroundDestroyed 75 249454 247913 200
            BTarget17R TGroundDestroyed 75 246777 235751 200
            BTarget18R TGroundDestroyed 75 212973 220079 200
            BTarget19R TGroundDestroyed 50 221278 214167 200
            BTarget20R TGroundDestroyed 75 178778 197288 200
            BTarget21R TGroundDestroyed 76 123384 196295 200
            BTarget22R TGroundDestroyed 75 70423 171706 200
            BTarget28 TGroundDestroyed 61 180207 288435 200
            RTarget28R TGroundDestroyed 61 294183 219444 50
            Rtarget29R TGroundDestroyed 63 276566 214150 50

            */

        }

        public void MissionObjectiveTriggersSetup()
        {
            //Format: addTrigger(MO_ObjectiveType.Building, "Name,OwnerArmy,Points,ID,TriggerType,PercRequired,XLoc,YLoc,Radius,IsPrimaryTarget,IsInPrimaryTargetPool,Comment "");
            addTrigger(MO_ObjectiveType.Aircraft, "AV22.1 Littlestone Bombers",         1, 2, "BTarget1","TGroundDestroyed",20,222303,221176,300,false,false, "");
            addTrigger(MO_ObjectiveType.Airport, "AN.24.2 Redhill Bomber Base",         1, 2, "BTarget2", "TGroundDestroyed", 20, 143336, 240806, 550, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "AU 23.5 Ashford Train Depot",        1, 2, "BTarget3", "TGroundDestroyed", 20, 214639, 235604, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Aircraft, "AX 25.9 Manston aircraft",           1, 2, "BTarget4", "TGroundDestroyed", 75, 247462, 259157, 250, false, false, "");
            addTrigger(MO_ObjectiveType.Vehicles, "AX 23.5 British Armor @ Dover",      1, 2, "BTarget5", "TGroundDestroyed", 80, 243887, 236956, 200, false, false, "");
            addTrigger(MO_ObjectiveType.Vehicles, "AP 27.4 British Armor @ CreekMouth", 1, 2, "BTarget6", "TGroundDestroyed", 50, 159687, 275015, 200, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "British Corvette Destroyed",             1, 3, "BTarget6S", "TGroupDestroyed", 90, 208155.61, 207542.83, 0, false, false, "4_Chief ? HMS Flower Corvette ");
            addTrigger(MO_ObjectiveType.Ship, "Cargo Ship @ Creekmouth",                1, 2, "BTarget7S", "TGroundDestroyed", 80, 160045, 274813, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Cargo Ship @ Creekmouth",                1, 2, "BTarget8S", "TGroundDestroyed", 80, 160172, 274841, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Cargo Ship @ Creekmouth",                1, 2, "BTarget9S", "TGroundDestroyed", 80, 159888, 274837, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Cargo Ship @ London Docks",              1, 2, "BTarget10S", "TGroundDestroyed", 80, 154957, 273914, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Cargo Ship @ London Docks",              1, 2, "BTarget11S", "TGroundDestroyed", 80, 154104, 273901, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Cargo Ship @ London Docks",              1, 2, "BTarget12S", "TGroundDestroyed", 63, 154488, 273912, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Corvette @ London Docks",                1, 2, "BTarget13S", "TGroundDestroyed", 66, 155847, 273960, 50, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "AAA London area destroyed",                1, 2, "BTarget13A", "TGroundDestroyed", 63, 160567, 275749, 100, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "AAA London area destroyed",                1, 2, "BTarget14A", "TGroundDestroyed", 63, 160025, 273824, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Ditton fuel refinery",                   1, 2, "BTarget24", "", 0, 0, 0, 0, false, false, "Trigger missing in .mis file");
            addTrigger(MO_ObjectiveType.Fuel, "Ditton fuel Storage",                    1, 2, "BTarget25", "", 0, 0, 0, 0, false, false, "Trigger missing in .mis file");
            addTrigger(MO_ObjectiveType.Building, "Maidstone train repair station ",    1, 2, "BTarget26", "", 0, 0, 0, 0, false, false, "Trigger missing in .mis file");
            addTrigger(MO_ObjectiveType.Building, "Tunbridge Wells Armory",             1, 2, "BTarget27", "", 0, 0, 0, 0, false, false, "Trigger missing in .mis file");
            addTrigger(MO_ObjectiveType.Building, "Unknown",                            1, 2, "RTarget0", "TGroundDestroyed", 100, 251259, 116909, 500, false, false, "No info/points in .cs file for this one?") ;
            addTrigger(MO_ObjectiveType.Vehicles, "BD 22.1 Motorpool near Grand-Fort Philippe", 2, 2, "RTarget1", "TGroundDestroyed", 50, 299486, 220998, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "BE 19.2 St. Omar Ball bearing Factory", 2, 2, "RTarget2", "TGroundDestroyed", 33, 313732, 192700, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "BB 16.4 Estree Fuel Depot", 2, 3, "RTarget3", "TGroundDestroyed", 40, 214728, 235509, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Synthetic Fuel", 2, 2, "RTarget4", "TGroundDestroyed", 60, 265005, 190321, 100, false, false, "");
            addTrigger(MO_ObjectiveType.RRStation, "Calais Rail Yard", 2, 2, "RTarget5", "TGroundDestroyed", 60, 283995, 215369, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Calais Hydrogen", 2, 2, "RTarget6", "TGroundDestroyed", 60, 284867, 216414, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Calais Main Fuel Fuel", 2, 2, "RTarget7", "TGroundDestroyed", 60, 285518, 217456, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Calais LOX", 2, 2, "RTarget8", "TGroundDestroyed", 60, 265590, 189900, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Calais Torpedo", 2, 2, "RTarget9", "TGroundDestroyed", 60, 266649, 187099, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Calais Diesel", 2, 2, "RTarget10", "TGroundDestroyed", 60, 266150, 189291, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Aviation Fuel", 2, 2, "RTarget11", "TGroundDestroyed", 43, 264966, 189374, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Diesel", 2, 2, "RTarget12", "TGroundDestroyed", 50, 284978, 215920, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Benzine", 2, 2, "RTarget13", "TGroundDestroyed", 52, 284845, 216884, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Liquid Oxygen", 2, 2, "RTarget14", "TGroundDestroyed", 50, 285019, 217566, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Ethanol Storage Boulogne", 2, 2, "RTarget15", "TGroundDestroyed", 50, 284153, 216913, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Arras Main Fuel", 2, 4, "RTarget16", "TGroundDestroyed", 50, 350605, 142047, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Arras Rubber Factory", 2, 3, "RTarget17", "TGroundDestroyed", 50, 352039, 141214, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "St Ouen AAA Factory", 2, 2, "RTarget18", "TGroundDestroyed", 50, 303445, 114053, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Abbeville Fuel", 2, 2, "RTarget19", "TGroundDestroyed", 50, 285075, 121608, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Dieppe Fuel", 2, 2, "RTarget20", "TGroundDestroyed", 50, 229270, 101222, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Le Treport Fuel", 2, 2, "RTarget21", "TGroundDestroyed", 50, 250477, 116082, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Poix Nord Fuel Storage", 2, 3, "RTarget22", "TGroundDestroyed", 50, 293827, 84983, 150, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Calais Chemical Research Facility", 2, 2, "RTarget23", "TGroundDestroyed", 75, 285254, 216717, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Optical Research Facility", 2, 2, "RTarget24", "TGroundDestroyed", 100, 285547, 216579, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Chemical Storage", 2, 2, "RTarget25", "TGroundDestroyed", 75, 285131, 216913, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Rations Storage", 2, 1, "RTarget26", "TGroundDestroyed", 78, 284522, 216339, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Gunpowder Facility", 2, 2, "RTarget27", "TGroundDestroyed", 50, 284898, 216552, 50, false, false, "");
            addTrigger(MO_ObjectiveType.Ship, "Minensuchboote", 2, 2, "RTarget30S", "TGroupDestroyed", 90, 263442.72, 181487.64, 0, false, false, "0_Chief ? Minensuchtboot");
            addTrigger(MO_ObjectiveType.Fuel, "Arras Fuel Storage 2", 2, 3, "RTarget31", "TGroundDestroyed", 100, 351371, 141966, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Watten Armory", 2, 2, "RTarget32", "TGroundDestroyed", 100, 310395, 200888, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Half track Factory", 2, 2, "RTarget33", "TGroundDestroyed", 100, 314794, 224432, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Steel mill Dunkirk", 2, 2, "RTarget34", "TGroundDestroyed", 100, 315081, 224145, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Brass Smelter Dunkirk", 2, 2, "RTarget35", "TGroundDestroyed", 100, 314832, 223389, 100, false, false, "");
            addTrigger(MO_ObjectiveType.Fuel, "Diesel Storage Dunkirk", 2, 2, "RTarget36", "TGroundDestroyed", 100, 314482, 223882, 200, false, false, "");
            addTrigger(MO_ObjectiveType.Building, "Ammunition Warehouse Dunkirk", 2, 3, "RTarget37", "TGroundDestroyed", 100, 313878, 223421, 100, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Calais AAA battery", 2, 1, "1A", "TGroundDestroyed", 63, 296130, 218469, 50, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Calais AAA battery", 2, 1, "2A", "TGroundDestroyed", 75, 294090, 85100, 100, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Calais AAA battery", 2, 1, "3A", "TGroundDestroyed", 66, 293279, 84884, 100, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Boulogne AAA battery 1", 2, 1, "4A", "TGroundDestroyed", 70, 317402, 196038, 100, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Boulogne AAA battery 2", 2, 1, "5A", "TGroundDestroyed", 47, 266055, 190488, 100, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Boulogne AAA battery 3", 2, 1, "6A", "TGroundDestroyed", 51, 264801, 188807, 50, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Poix Nord AAA battery 2", 2, 1, "7A", "TGroundDestroyed", 62, 285982, 216833, 50, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Poix Nord AAA battery 1", 2, 1, "8A", "TGroundDestroyed", 54, 283234, 215851, 50, false, false, "");
            addTrigger(MO_ObjectiveType.AA, "Poix Nord AAA battery 3", 2, 1, "9A", "TGroundDestroyed", 77, 283224, 216619, 50, false, false, "");
        }
    }

    //foreach (AiAirport apk in AirfieldTargets.Keys)

    //Destroys the objective with the given ID and takes other related actions, such as 
    //adding points, displaying messages, reducing radar coverage
    public bool MO_DestroyObjective(string ID, bool active=true)
    {
        if (!active) return false;  //If a trigger is passed with flag active=false that (generally) means the trigger has already been activated once before & we don't want to repeat it again
                                    //this is passed when coming from onTrigger, otherwise it should just be TRUE by default

        var OldObj = new MissionObjective(this);

        if (!MissionObjectivesList.TryGetValue(ID, out OldObj))
        {
            return false;
            //OldObj = new MissionObjective(msn);
        }

        //Turn off the trigger so it won't trigger again - IF this objective type is a trigger
        if (OldObj.MOTriggerType == MO_TriggerType.Trigger && GamePlay.gpGetTrigger(ID) != null)
        {
            Console.WriteLine("MO_DestroyObjective: Disabling trigger " + ID);
            GamePlay.gpGetTrigger(ID).Enable = false;
        }

        if (OldObj.Destroyed) return false; //The object has already been destroyed; don't need to do it again; we only give points/credit for destroying any given objective once

        OldObj.Destroyed = true;
        if (OldObj.MOObjectiveType == MO_ObjectiveType.Radar)
        {
            if (OldObj.OwnerArmy == 1) DestroyedRadar[(ArmiesE.Red)].Add(OldObj);
            if (OldObj.OwnerArmy == 2) DestroyedRadar[(ArmiesE.Blue)].Add(OldObj);
        }
        
        if (OldObj.AttackingArmy == 1)
        {
            Objective_Total_Red += " - " + OldObj.Name;
            InitialBlueObjectiveCount += OldObj.Points;
        }
        if (OldObj.AttackingArmy == 2)
        {
            Objective_Total_Blue += " - " + OldObj.Name;
            InitialRedObjectiveCount += OldObj.Points;
        }


        GamePlay.gpHUDLogCenter(OldObj.HUDMessage);
        Timeout(10, () =>
        {
            GamePlay.gpLogServer(null, OldObj.LOGMessage, new object[] { });
        MissionObjectivesList[ID] = OldObj;
        });

        MO_CheckObjectivesComplete();

        return true;
    }

    public void MO_CheckObjectivesComplete()
    {
        if (InitialBlueObjectiveCount >= MO_PercentPrimaryTargetsRequired[ArmiesE.Blue])// Blue battle Success
        {
            WriteResults_Out_File("2");
            Timeout(10, () =>
            {
                GamePlay.gpLogServer(null, "Blue has Successfully Completed All Objectives!!!", new object[] { });
                GamePlay.gpHUDLogCenter("Blue has Successfully Completed All Objectives!!!");
            });
            EndMission(70, "Blue");
        }
        if (InitialRedObjectiveCount >= MO_PercentPrimaryTargetsRequired[ArmiesE.Red])// Red battle Success
        {
            WriteResults_Out_File("1");
            Timeout(10, () =>
            {
                GamePlay.gpLogServer(null, "Red has Successfully Completed All Objectives!!!", new object[] { });
                GamePlay.gpHUDLogCenter("Red has Successfully Completed All Objectives!!!");
            });
            EndMission(70, "Red");
        }
    }

    public bool MO_IsPointInDestroyedRadarArea(Point3d p, int army)
    {
        var DR = new List<MissionObjective>();

        if (army == 1 || army == 2) DR = DestroyedRadar[(ArmiesE)army];        
        else return false;

        foreach (MissionObjective value in DR)
        {
            double dist = Calcs.CalculatePointDistance(p, value.Pos);
            //if (value.ID == "BTarget14R") Console.WriteLine(value.Name + " " + army.ToString() + " " + dist.ToString("F0") + " " + value.RadarEffectiveRadius.ToString("F0") + " " + p.x.ToString("F0") + " " + p.y.ToString("F0") + " " + value.Pos.x.ToString("F0") + " " + value.Pos.y.ToString("F0"));
            if (dist < value.RadarEffectiveRadius) return true;
        }
        return false;
    }

    //Figure out which radar areas are disabled depending on army, admin radar, which objectives have been destroyed, etc.
    //Returns TRUE if radar is enabled for that area/army, returns FALSE if radar is disabled/out for that area/army
    //radarArmy 1 = red, 2=blue, 0=admin, anything else is not allowed (in practice this will ignore any radar outages)
    public bool MO_isRadarEnabledByArea( Point3d pos, bool admin=false, int radarArmy=0) {
        if (admin || radarArmy==0 || radarArmy>2) return true;

        //Console.WriteLine("#1 " + pos.x.ToString() + " " + pos.y.ToString() + " " + radarArmy.ToString());
        //WITHIN AN AREA WHERE THE RADAR HAS BEEN DESTROYED?
        //Finds if the point/ac is in an area with destroyed radar for either/both sides
        if (mission_objectives != null) { if (MO_IsPointInDestroyedRadarArea(pos, radarArmy)) return false; }
        else Console.WriteLine("#1.5  Mission Objectives doesn't exist!");
        //Console.WriteLine("#2 " + pos.x.ToString() + " " + radarArmy.ToString());

        //RED army special denied areas or areas the never have radar coverage
        if (radarArmy==1)
        {

            //Red doesn't have any special denied areas for now.
            //TODO: We could make furthest reaches of France out of radar range, or perhaps just start to remove more low-level 
            //radar the further into France we go.
            return true;

        }

        //BLUE army special denied areas or areas that never have radar coverage
        //TODO: Could gradually remove low-level coverage the further from the stations we go (this is realistic)
        else if (radarArmy == 2)
        {
            //BLUE radar only goes approx to English Coast.
            //This approximates that by taking a line between these points

            //  250000  313000  TopR of map, direct north of Hellfire Corner
            //  250000  236000  hellfire corner
            //  170000  194000  Eastbourne
            //  8000    180000  Edge of map near Bournemouth
            //TODO: We could make this more realistic in various ways, perhaps extending some high-level radar partially into UK or the like


            if (pos.x > 170000 && pos.x <= 250000)
            {
                if ((pos.x - 170000) / 80000 * 42000 + 194000 < pos.y) return false;                 
            }
            if (pos.x > 8000 && pos.x <= 170000)
            {
                if ((pos.x - 8000) / 162000 * 14000 + 180000 < pos.y) return false;
            }
            return true;
        }
        return true;

    }

    /******************************************************************************************************************** 
    * ****END****MISSION OBJECTIVES CLASSES & METHODS
    * **********************************************************************************************************************/
    
    /******************************************************************************************************************** 
    * RADAR
    * 
    * 
    * **********************************************************************************************************************/


    public System.IO.FileInfo fi;// = new System.IO.FileInfo(STATSCS_FULL_PATH + MISSION_ID + "_radar.txt"); //file to write to
    public System.IO.StreamWriter sw;

    //List all a/c positions to console///////////////////////////
    //Radar . . . 
    // RADAR_REALISM = 0 lists <pos detailed info to chat window, for admins
    // RADAR_REALISM > 0 is regular tab-4-1 <rad filtered radar returns for in-game
    // RADAR_REALISM = -1 is for online radar/TOPHAT system and saves the info for both armies to a file
    // inOwnArmy = true list aircraft in own army, false in opposing army
    //BUT inOwnArmy = false & playerArmy -1, -2, -3 will list ALL aircraft regardless
    // playerArmy -1 is for TOPHAT & will list all a/c but with the red TOPHAT slant
    // playerArmy -2 is for TOPHAT & will list all a/c but with the blue TOPHAT slant
    // playerArmy -3 is for TOPHAT & will list all a/c but is for ADMINS listing all kinds of details etc vs the red/blue TOPHAT which is more filtered to simulate real WWII radar
    public string posmessage;
    public int poscount;
    public void listPositionAllAircraft(Player player, int playerArmy, bool inOwnArmy)
    {

        // int RADAR_REALISM;     //realism = 0 gives exact position, bearing, velocity of each a/c.  We plan to make various degrees of realism ranging from 0 to 10.  Implemented now is just 0=exact, >0 somewhat more realistic    
        // realism = -1 gives the lat/long output for radar files.
        AiAircraft p = null;
        Point3d pos1;
        Point3d pos2;
        Point3d VwldP, intcpt, longlat;
        Vector3d Vwld, player_Vwld;
        double player_vel_mps = 0;
        double player_vel_mph = 0;
        double player_alt_m = 0;
        string type, player_sector;
        string playertype = "";
        bool player_place_set = false;
        bool isHeavyBomber = false;
        double vel_mps = 0;
        double vel_mph = 0;
        int vel_mph_10 = 0;
        double heading = 0;
        int heading_10 = 0;
        double dis_m = 0;
        double dis_mi = 0;
        int dis_10 = 0;
        double bearing = 0;
        int bearing_10 = 0;
        double alt_ft = 0;
        double alt_km = 0;
        double altAGL_m = 0;
        double altAGL_ft = 0;
        int alt_angels = 0;
        string sector = "";
        double intcpt_heading = 0;
        double intcpt_time_min = 0;
        string intcpt_sector = "";
        bool intcpt_reasonable_time = false;
        bool climb_possible = true;
        int aigroup_count = 0;
        string playername = "TWC_server_159273";
        string playername_index;
        string enorfriend;
        long currtime_ms = 0;
        long storedtime_ms = -1;
        bool savenewmessages = true;
        Tuple<long, SortedDictionary<string, string>> message_data;
        SortedDictionary<string, string> radar_messages =
            new SortedDictionary<string, string>(new ReverseComparer<string>());
        //string [] radar_messages, radar_messages_index;             
        int wait_s = 0;
        long refreshtime_ms = 0;
        if (RADAR_REALISM >= 1) { wait_s = 5; refreshtime_ms = 60 * 1000; }
        if (RADAR_REALISM >= 5) { wait_s = 20; refreshtime_ms = 2 * 60 * 1000; }
        if (RADAR_REALISM >= 9) { wait_s = 60; refreshtime_ms = 5 * 60 * 1000; }

        //if admin==true we'll list ALL aircraft regardless of position, radars out et.
        //admin==false we start filtering out depending on whether a radar station has been knocked out etc
        bool admin = false;
        if ((RADAR_REALISM == 0) || (RADAR_REALISM == -1 && playerArmy == -3)) admin = true;

        //RadarArmy is the army/side for which the radar is being generated.
        //Some radar areas will be out for Red but still active for Blue.  Blue radar might have some inherent restrictions Red radar doesn't, etc. 
        //if radarArmy==0 it will be more an admin radar that ignores these restrictions
        int radarArmy = Math.Abs(playerArmy);
        if (radarArmy > 2) radarArmy = 0;

        //wait_s = 1; //for testing
        //refreshtime_ms = 1 * 1000;

        enorfriend = "ENEMY";
        if (inOwnArmy) enorfriend = "FRIENDLY";
        if (playerArmy < 0) enorfriend = "BOTH ARMIES";

        if (player != null && (player.Place() is AiAircraft))
        {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
            p = player.Place() as AiAircraft;
            player_Vwld = p.AirGroup().Vwld();
            player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
            player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
            player_alt_m = p.Pos().z;
            player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
            player_sector = player_sector.Replace(",", ""); // remove the comma
            player_place_set = true;
            playername = player.Name();
            playertype = p.Type().ToString();
            if (playertype.Contains("Fighter") || playertype.Contains("fighter")) playertype = "F";
            else if (playertype.Contains("Bomber") || playertype.Contains("bomber")) playertype = "B";
            else playertype = "U";

            /* posmessage = "Radar intercepts are based on your current speed/position: " +
                         player_vel_mph.ToString("F0") +"mph " + 
                         player_sector.ToString();
            gpLogServerAndLog(new Player[] { player }, posmessage, null);
            */

        }
        playername_index = playername + "_0";
        if (inOwnArmy) playername_index = playername + "_1";
        playername_index = playername_index + "_" + RADAR_REALISM.ToString();

        savenewmessages = true; //save the messages that are generated
        currtime_ms = stopwatch.ElapsedMilliseconds;
        //If the person has requested a new radar return too soon, just repeat the old return verbatim
        //We have 3 cases:
        // #1. ok to give new radar return
        // #2. Too soon since last radar return to give a new one
        // #3. New radar return is underway but not finished, so don't give them a new one. 
        if (radar_messages_store.TryGetValue(playername_index, out message_data))
        {
            long time_elapsed_ms = currtime_ms - message_data.Item1;
            long time_until_new_s = (long)((refreshtime_ms - time_elapsed_ms) / 1000);
            long time_elapsed_s = (long)time_elapsed_ms / 1000;
            radar_messages = message_data.Item2;
            if (time_elapsed_ms < refreshtime_ms || message_data.Item1 == -1)
            {
                if (message_data.Item1 == -1) posmessage = "New radar returns are in process.  Your previous radar return:";
                else posmessage = time_until_new_s.ToString("F0") + "s until " + playername + " can receive a new radar return.  Your previous radar return:";
                gpLogServerAndLog(new Player[] { player }, posmessage, null);

                wait_s = 0;
                storedtime_ms = message_data.Item1;
                savenewmessages = false; //don't save the messages again because we aren't generating anything new

                //Wait just 2 seconds, which gives people a chance to see the message about how long until they can request a new radar return.
                Timeout(2, () =>
                {
                            //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               
                            foreach (var mess in message_data.Item2)
                    {
                        if (RADAR_REALISM == 0) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                        else gpLogServerAndLog(new Player[] { player }, mess.Value, null);

                    }
                });
            }

        }

        //If they haven't requested a return before, or enough time has elapsed, give them a new return  
        if (savenewmessages)
        {
            //When we start to work on the messages we save current messages (either blank or the previous one that was fetched from radar_messages_store)
            //with special time code -1, which means that radar returns are currently underway; don't give them any more until finished.
            radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(-1, radar_messages);

            if (RADAR_REALISM > 0) GamePlay.gpLogServer(new Player[] { player }, "Fetching radar contacts, please stand by . . . ", null);




            radar_messages = new SortedDictionary<string, string>(new ReverseComparer<string>());//clear it out before starting anew . . .           
            radar_messages.Add("9999999999", " >>> " + enorfriend + " RADAR CONTACTS <<< ");

            if (RADAR_REALISM < 0) radar_messages.Add("9999999998", "p" + Calcs.GetMD5Hash(radarpasswords[playerArmy].ToUpper())); //first letter 'p' indicates passward & next characters up to space or EOL are the password.  Can customize this per  type of return, randomize each mission, or whatever.
                                                                                                                                   //RADAR_REALISM < 0 is our returns for the online radar screen, -1 = red returns, -2 = blue returns, -3 = admin (ALL SEEING EYE) returns
                                                                                                                                   //passwords are CASEINSENSITIVE and the MD5 of the password is saved in the -radar.txt file for red, blue, and admin respectively

            //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                foreach (int army in GamePlay.gpArmies())
                {
                    //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                    if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0 && (!inOwnArmy ^ (army == playerArmy)))
                    {
                        foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                        {
                            aigroup_count++;
                            if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                            {
                                poscount = airGroup.NOfAirc;
                                foreach (AiActor actor in airGroup.GetItems())
                                {
                                    if (actor is AiAircraft)
                                    {
                                        AiAircraft a = actor as AiAircraft;
                                        //if (!player_place_set &&  (a.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"


                                        //Check on any radar outages or restrictions for each army, and remove any radar returns from areas where radar is restricted or inoperative
                                        if (!MO_isRadarEnabledByArea(a.Pos(), admin, radarArmy)) break;

                                        if (!player_place_set)
                                        {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"                                                                        
                                            p = actor as AiAircraft;
                                            player_Vwld = p.AirGroup().Vwld();
                                            player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                                            player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                                            player_alt_m = p.Pos().z;
                                            player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                                            player_sector = player_sector.Replace(",", ""); // remove the comma
                                            player_place_set = true;
                                        }

                                        bool isAI = isAiControlledPlane2(a);

                                        string acType = Calcs.GetAircraftType(a);
                                        isHeavyBomber = false;
                                        if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType == ("BlenheimMkIV")) isHeavyBomber = true;

                                        type = a.Type().ToString();
                                        if (type.Contains("Fighter") || type.Contains("fighter")) type = "F";
                                        else if (type.Contains("Bomber") || type.Contains("bomber")) type = "B";
                                        if (a == p && RADAR_REALISM >= 0) type = "Your position";
                                        /* if (DEBUG) GamePlay.gpLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                         + a.CallSign() + " " 
                                         + a.Type() + " " 
                                         + a.TypedName() + " " 
                                         +  a.AirGroup().ID(), new object[] { });
                                        */
                                        pos1 = a.Pos();
                                        //Thread.Sleep(100);
                                        //pos2=a.Pos();
                                        //bearing=Calcs.CalculateGradientAngle (pos1,pos2);
                                        Vwld = airGroup.Vwld();
                                        vel_mps = Calcs.CalculatePointDistance(Vwld);
                                        vel_mph = Calcs.meterspsec2milesphour(vel_mps);
                                        vel_mph_10 = Calcs.RoundInterval(vel_mph, 10);
                                        heading = (Calcs.CalculateBearingDegree(Vwld));
                                        heading_10 = Calcs.GetDegreesIn10Step(heading);
                                        dis_m = Calcs.CalculatePointDistance(a.Pos(), p.Pos());
                                        dis_mi = Calcs.meters2miles(dis_m);
                                        dis_10 = (int)dis_mi;
                                        if (dis_mi > 20) dis_10 = Calcs.RoundInterval(dis_mi, 10);
                                        bearing = Calcs.CalculateGradientAngle(p.Pos(), a.Pos());
                                        bearing_10 = Calcs.GetDegreesIn10Step(bearing);

                                        longlat = Calcs.Il2Point3dToLongLat(a.Pos());

                                        alt_km = a.Pos().z / 1000;
                                        alt_ft = Calcs.meters2feet(a.Pos().z);
                                        altAGL_m = (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0); // I THINK (?) that Z_AltitudeAGL is in meters?
                                        altAGL_ft = Calcs.meters2feet(altAGL_m);
                                        alt_angels = Calcs.Feet2Angels(alt_ft);
                                        sector = GamePlay.gpSectorName(a.Pos().x, a.Pos().y).ToString();
                                        sector = sector.Replace(",", ""); // remove the comma
                                        VwldP = new Point3d(Vwld.x, Vwld.y, Vwld.z);

                                        intcpt = Calcs.calculateInterceptionPoint(a.Pos(), VwldP, p.Pos(), player_vel_mps);
                                        intcpt_heading = (Calcs.CalculateGradientAngle(p.Pos(), intcpt));
                                        intcpt_time_min = intcpt.z / 60;
                                        intcpt_sector = GamePlay.gpSectorName(intcpt.x, intcpt.y).ToString();
                                        intcpt_sector = intcpt_sector.Replace(",", ""); // remove the comma
                                        intcpt_reasonable_time = (intcpt_time_min >= 0.02 && intcpt_time_min < 20);

                                        climb_possible = true;
                                        if (player_alt_m <= a.Pos().z && intcpt_time_min > 1)
                                        {
                                            double altdiff_m = a.Pos().z - player_alt_m;
                                            if (intcpt_time_min > 3 && altdiff_m / intcpt_time_min > 1100) { climb_possible = false; } //109 can climb @ a little over 1000 meters per minute in a sustained way.  So anything that requires more climb than that we exclude from the listing
                                            else if (altdiff_m / intcpt_time_min > 2500) climb_possible = false; //We allow for the possibility of more climb for a brief time, less then 3 minutes

                                        }

                                        string mi = dis_mi.ToString("F0") + "mi";
                                        string mi_10 = dis_10.ToString("F0") + "mi";
                                        string ft = alt_ft.ToString("F0") + "ft ";
                                        string ftAGL = altAGL_ft.ToString("F0") + "ftAGL ";
                                        string mph = vel_mph.ToString("F0") + "mph";
                                        string ang = "A" + alt_angels.ToString("F0") + " ";

                                        if (playerArmy == 2) //metric for the Germanos . . . 
                                        {
                                            mi = (dis_m / 1000).ToString("F0") + "k";
                                            mi_10 = mi;
                                            if (dis_m > 30000) mi_10 = ((double)(Calcs.RoundInterval(dis_m, 10000)) / 1000).ToString("F0") + "k";

                                            ft = alt_km.ToString("F2") + "k ";
                                            ftAGL = altAGL_m.ToString("F0") + "mAGL ";
                                            mph = (Calcs.RoundInterval(vel_mps * 3.6, 10)).ToString("F0") + "k/h";
                                            ang = ((double)(Calcs.RoundInterval(alt_km * 10, 5)) / 10).ToString("F1") + "k ";
                                        }

                                        //comprehensive radar returns for tophat/sysadmin purposes
                                        //TODO:
                                        //Add strong server-generated password for each session that can be communicated to admins etc
                                        //Make a more filtered "TopHat" version that could be actually used by a commander/mission 
                                        //control during missions, and also broadcast instructions & password for one person
                                        //(or maybe a couple of people, or maybe everyone ? ) from each side to be able to use
                                        //the more filtered version that is pretty comparable to what is already 
                                        //shown as text radar in-mission
                                        //TODO: 
                                        //We could give Blue tophat measurements in metric units, maybe
                                        if (RADAR_REALISM < 0)
                                        {

                                            string numContacts = poscount.ToString();
                                            string aircraftType = Calcs.GetAircraftType(a);
                                            string vel = vel_mph.ToString("n0");
                                            string alt = alt_angels.ToString("n0");
                                            string he = heading.ToString("F0");

                                            string aplayername = "";
                                            if (a.Player(0) != null && a.Player(0).Name() != null)
                                            {
                                                aplayername = a.Player(0).Name();
                                            }

                                            //red & blue tophat operators only get an approximation of how many a/c in each group and also
                                            //don't get perfect information about whether fighters or bombers or unknown
                                            //and also don't get the EXACT type of a/c or the name of the player
                                            if (playerArmy == -1 || playerArmy == -2)
                                            {
                                                numContacts = "~" + Calcs.NoOfAircraft(poscount).ToString("F0");
                                                if (random.Next(8) == 1)
                                                { //oops, sometimes we get mixed up on the type.  So sad . . .  See notes below about relative inaccuracy of early radar.
                                                    type = "F";
                                                    if (random.Next(3) == 1) type = "B";
                                                    if (random.Next(8) == 1) type = "U";
                                                }
                                                aircraftType = "";
                                                aplayername = "";

                                                vel = vel_mph_10.ToString("n0");
                                                alt = alt_angels.ToString("n0");
                                                he = heading_10.ToString("F0");

                                            }


                                            posmessage =
                                            a.Pos().x.ToString()
                                            + "," + a.Pos().y.ToString() + "," +
                                            longlat.y.ToString()
                                            + "," + longlat.x.ToString() + "," +
                                            army.ToString() + "," +
                                            type + "," +
                                            he + "," +
                                            vel + "," +
                                            alt + "," +
                                            sector.ToString() + "," +
                                            numContacts + "," +
                                            aircraftType.Replace(',', '_') + "," //Replace any commas since we are using comma as a delimiter here
                                            + a.Name().GetHashCode().ToString() + "," //unique hashcode for each actor that will allow us to be able to identify it uniquely on the other end, without giving away anything about what type of actor it is (what type of aircraft, whether AI or live player, etc)
                                            + aplayername.Replace(',', '_'); //Replace any commas since we are using comma as a delimiter here

                                            //-radar.txt data file structure is:
                                            //First line header info - just ignore it
                                            //Each succeeding line is comma delimited, no quotation marks:
                                            //0 & 1 - posx & posy (meters), 2 & 3 - lat & long (approximate & not too accurate) 
                                            //4 - army (int), 5 type (string, F or B), 6 Heading (degrees), 7 vel (int, MPH), 
                                            //8 - altitude (int, Angels, 1000s of feet), 9 - IL2 CloD sector (string)
                                            //10 - how many in this formation (int, exact)
                                            //11 - aircraft type (string, exact type & CloD a/c name)
                                            //12 - unique hashcode (int) for this actor (not actually 100% guaranteed to be unique but fast & probably good enough for our simple purposes)
                                            //13 - player steam name (string) if it exists


                                            //For red & blue TopHot operators we give a slightly filtered view of the contacts,
                                            //simulating what actual WWII radar could see
                                            //For example contacts low to the ground fade out.
                                            //TODO: We could set up radar towers & contacts could be seen better if closer to the 
                                            //tower (ie, lower to the ground.  But then if the enemy destroys your tower you lose
                                            //that & can only see what your remaining towers can see (which will likely be contacts only quite high in altitude)
                                            //Also each army could have its own towers giving it better visibility on its own side of the lines where its own towers
                                            //are etc
                                            if ((playerArmy == -1 || playerArmy == -2) && (
                                                           (altAGL_ft < 250 && altAGL_ft < random.Next(500)) || //Less then 250 ft AGL they start to phase out from radar
                                                           (altAGL_ft < 175) || //And, if they are less than 175 feet AGL, they are gone from radar                                                     
                                                           ((!isAI && isHeavyBomber) && poscount <= 2 && random.Next(4) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out 
                                                                                                                                 //However if the player heavy bombers group up they are MUCH more likely to show up on radar.  But they will still be harder than usual to track because each individual bomber will phase in/out quite often

                                                           (random.Next(7) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                           )
                                           ) { posmessage = ""; }


                                        }
                                        else if (RADAR_REALISM == 0)
                                        {
                                            posmessage = type + " " +

                                              mi +
                                              bearing.ToString("F0") + "°" +
                                              ft +
                                              ftAGL +
                                              mph +
                                              heading.ToString("F0") + "° " +
                                              sector.ToString() + " " +
                                              Calcs.GetAircraftType(a);
                                            if (intcpt_time_min > 0.02)
                                            {
                                                posmessage +=
                                                   " Intcpt: " +
                                                   intcpt_heading.ToString("F0") + "°" +
                                                   intcpt_time_min.ToString("F0") + "min " +
                                                   intcpt_sector + " " +
                                                   intcpt.x.ToString("F0") + " " + intcpt.y.ToString("F0");
                                            }

                                            /* "(" + 
                                            Calcs.meters2miles(a.Pos().x).ToString ("F0") + ", " +
                                            Calcs.meters2miles(a.Pos().y).ToString ("F0") + ")";
                                            */
                                            //GamePlay.gpLogServer(new Player[] { player }, posmessage, new object[] { });
                                        }
                                        else if (RADAR_REALISM > 0)
                                        {

                                            //Trying to give at least some semblance of reality based on capabilities of Chain Home & Chain Home Low
                                            //https://en.wikipedia.org/wiki/Chain_Home
                                            //https://en.wikipedia.org/wiki/Chain_Home_Low
                                            if (random.Next(8) == 1)
                                            { //oops, sometimes we get mixed up on the type.  So sad . . .  See notes below about relative inaccuracy of early radar.
                                                type = "F";
                                                if (random.Next(3) == 1) type = "B";
                                            }
                                            if (dis_mi <= 2 && a != p && Math.Abs(player_alt_m - a.Pos().z) < 5000) { posmessage = type + " nearby"; }


                                            //Below conditions are situations where radar doesn't work/fails, working to integrate realistic conditions for radar
                                            //To do this in full realism we'd need the full locations of Chain Home & Chain Home Low stations & exact capabilities
                                            //As an approximation we're using distance from the current aircraft, altitude, etc.
                                            /* wikipedia gives an idea of how rough early CH output & methods were: CH output was read with an oscilloscope. When a pulse was sent from the broadcast towers, a visible line travelled horizontally across the screen very rapidly. The output from the receiver was amplified and fed into the vertical axis of the scope, so a return from an aircraft would deflect the beam upward. This formed a spike on the display, and the distance from the left side – measured with a small scale on the bottom of the screen – would give target range. By rotating the receiver goniometer connected to the antennas, the operator could estimate the direction to the target (this was the reason for the cross shaped antennas), while the height of the vertical displacement indicated formation size. By comparing the strengths returned from the various antennas up the tower, altitude could be gauged with some accuracy.
                                             * Upshot is, exact #, position, no of aircraft, type of aircraft, altitude etc were NOT that precisely known.  Rather they were estimates/guesstimates based on strength of pulse of the radar return as viewed on an oscilliscope etc.
                                             * ******************/
                                            else if ((dis_mi >= 50 && poscount < 8 && random.Next(15) > 1 && !intcpt_reasonable_time) ||  //don't show enemy groups too far away, unless they are quite large, or can be intercepted in reasonable time.  Except once in a while randomly show one.
                                                     (dis_mi >= 25 && poscount < 4 && random.Next(12) > 1 && !intcpt_reasonable_time) ||
                                                     (dis_mi >= 15 && poscount <= 2 && random.Next(8) > 1 && !intcpt_reasonable_time) ||
                                                     (!climb_possible && playertype != "B" && army != playerArmy) ||  //If the aircraft is too high for us to be able to climb to, we exclude it from the listing, unless the player is a bomber pilot (who is going to be interested in which planes are above in attack position) OR we are getting a listing of our own army, in which case we want all nearby a/c not just ones we can attack
                                                     (dis_mi >= 70 && altAGL_ft < 4500) || //chain home only worked above ~4500 ft & Chain Home Low had effective distance only 35 miles
                                                                                           //however, to implement this we really need the distance of the target from the CHL stations, not the current aircraft
                                                                                           //We'll approximate it by eliminating low contacts > 70 miles away from current a/c 
                                                     (dis_mi >= 10 && altAGL_ft < 500 && altAGL_ft < random.Next(500)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                                                                                          //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                                                                                          //phasing out targets below 250 feet.
                                                     (dis_mi < 10 && altAGL_ft < 250 && altAGL_ft < random.Next(500)) || //Within 10 miles though you really have to be right on the deck before the radar starts to get flakey, less than 250 ft. Somewhat approximating 50 foot alt lower limit.
                                                     (altAGL_ft < 175) || //And, if they are less than 175 feet AGL, they are gone from radar
                                                     ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi > 11 && poscount <= 2 && random.Next(4) <= 2) || // Breather bombers have a higher chance of being overlooked/dropping out, especially when further away.  3/4 times it doesn't show up on radar.
                                                     ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi <= 11 && poscount <= 2 && random.Next(5) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out when close (this is close enough it should be visual range, so we're not going to help them via radar)
                                                                                                                                                                 //((!isAI && type == "B" && army != playerArmy) && random.Next(5) > 0) || // Enemy bombers don't show up on your radar screen if less than 7 miles away as a rule - just once in a while.  You'll have to spot them visually instead at this distance!
                                                                                                                                                                 //We're always showing breather FIGHTERS here (ie, they are not included in isAI || type == "B"), because they always show up as a group of 1, and we'd like to help them find each other & fight it out
                                                     (random.Next(7) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                     ) { posmessage = ""; }
                                            else
                                            {
                                                posmessage = type + " " +
                                                   mi_10 +
                                                   bearing_10.ToString("F0") + "°" +
                                                   ang +
                                                   mph +
                                                   heading_10.ToString("F0") + "° ";
                                                //+ sector.ToString();
                                                if (intcpt_time_min >= 0.02)
                                                {
                                                    posmessage +=
                                                       " Intcpt: " +
                                                       intcpt_heading.ToString("F0") + "°" +
                                                       intcpt_time_min.ToString("F0") + "min ";
                                                    //+ intcpt_sector + " ";
                                                }

                                            }

                                        }




                                        //poscount+=1;
                                        break; //only get 1st a/c in each group, to save time/processing


                                    }
                                }
                                //We'll print only one message per Airgroup, to reduce clutter
                                //GamePlay.gpLogServer(new Player[] { player }, "RPT: " + posmessage + posmessage.Length.ToString(), new object[] { });
                                if (posmessage.Length > 0)
                                {
                                    //gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                    //We add the message to the list along with an index that will allow us to reverse sort them in a logical/useful order                               
                                    int intcpt_time_index = (int)intcpt_time_min;
                                    if (intcpt_time_min <= 0 || intcpt_time_min > 99) intcpt_time_index = 99;

                                    try
                                    {
                                        string addMess = posmessage;
                                        if (RADAR_REALISM > 0) addMess = "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + posmessage;
                                        radar_messages.Add(
                                           ((int)intcpt_time_index).ToString("D2") + ((int)dis_mi).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                           addMess
                                        );
                                    }
                                    catch (Exception e)
                                    {
                                        GamePlay.gpLogServer(new Player[] { player }, "RadError: " + e, new object[] { });
                                    }


                                }

                            }
                        }
                    }
                }

            }
            //There is always one message - the header.  
            if (radar_messages.Count == 1) radar_messages.Add("0000000000", "<NO TRADE>");


            if (RADAR_REALISM < 0)
            {
                try
                {
                    string typeSuff = "";
                    if (playerArmy == -3) typeSuff = "_ADMIN";
                    if (playerArmy == -1) typeSuff = "_RED";
                    if (playerArmy == -2) typeSuff = "_BLUE";
                    string filepath = STATSCS_FULL_PATH + SERVER_ID_SHORT.ToUpper() + typeSuff + "_radar.txt";
                    if (File.Exists(filepath)) { File.Delete(filepath); }
                    fi = new System.IO.FileInfo(filepath); //file to write to
                    sw = fi.CreateText(); // Writes Lat long & other info to file

                    foreach (var mess in radar_messages)
                    {
                        sw.WriteLine(mess.Value);
                    }

                    sw.Close();


                    //And, now we create a file with the list of players:
                    //TODO: This probably could/should be a separate method that we just call here
                    filepath = STATSCS_FULL_PATH + SERVER_ID_SHORT.ToUpper() + typeSuff + "_players.txt";
                    if (File.Exists(filepath)) { File.Delete(filepath); }
                    fi = new System.IO.FileInfo(filepath); //file to write to
                    sw = fi.CreateText(); // Writes Lat long & other info to file
                    sw.WriteLine(DateTime.UtcNow.ToString("u").Trim() + " - " + showTimeLeft(null, false));
                    sw.WriteLine();

                    int pycount = 0;
                    int pyinplace = 0;
                    string msg = "";
                    if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
                    {

                        foreach (Player py in GamePlay.gpRemotePlayers())
                        {
                            pycount++;
                            string pl = "(none)";
                            if (py.Place() != null)
                            {
                                pyinplace++;
                                AiActor act = py.Place();
                                pl = act.Name();

                                if (act as AiAircraft != null)
                                {
                                    AiAircraft acf = act as AiAircraft;
                                    string acType = Calcs.GetAircraftType(acf);
                                    pl = acType;
                                }

                                if (playerArmy == -3)
                                {
                                    //Point3d ps = Calcs.Il2Point3dToLongLat(act.Pos());
                                    //pl += " " + ps.y.ToString("n2") + " " + ps.x.ToString("n2");
                                    pl += " " + act.Pos().y.ToString("n0") + " " + act.Pos().x.ToString("n0");
                                }

                            }
                            msg += py.Name() + " " + py.Army() + " " + pl + "\n";

                        }

                    }

                    sw.WriteLine("Players logged in: " + pycount.ToString() + " Active: " + pyinplace.ToString());
                    sw.WriteLine();

                    sw.WriteLine("MISSION SUMMARY");

                    sw.WriteLine("Blue Objectives complete (" + InitialBlueObjectiveCount.ToString() + " points):" + (Objective_Total_Blue));
                    sw.WriteLine("Red Objectives complete (" + InitialRedObjectiveCount.ToString() + " points):" + (Objective_Total_Red));
                    /***TODO: Need to include some kind of current mission & campaign summary here
                     * 
                     */

                    /*
                    if (playerArmy == -2 || playerArmy == -3) sw.WriteLine(osk_BlueObjDescription);
                    if (playerArmy == -1 || playerArmy == -3) sw.WriteLine(osk_RedObjDescription);
                    sw.WriteLine("Blue Objectives complete: " + osk_BlueObjCompleted);
                    sw.WriteLine("Red Objectives complete: " + osk_RedObjCompleted);
                    //sw.WriteLine("Blue/Red total score: " + (BlueTotalF).ToString("N1") + "/" + (RedTotalF).ToString("N1"));
                    */
                    sw.WriteLine(string.Format("BLUE session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", BlueTotalF,
  BlueAirF, BlueAAF, BlueNavalF, BlueGroundF));
                    sw.WriteLine(string.Format("RED session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", RedTotalF,
  RedAirF, RedAAF, RedNavalF, RedGroundF));
                    sw.WriteLine();

                    /*
                    sw.WriteLine("CAMPAIGN SUMMARY");

                    Tuple<double, string> res = CalcMapMove("", false, false, null);
                    sw.Write(res.Item2);
                    double newMapState = CampaignMapState + res.Item1;
                    sw.Write(summarizeCurrentMapstate(newMapState, false, null));
                    */

                    sw.WriteLine();
                    if (msg.Length > 0) sw.WriteLine("PLAYER SUMMARY");
                    sw.WriteLine(msg);

                    sw.WriteLine();
                    msg = ListAirfieldTargetDamage(null, -1, false, false); //Add the list of current airport conditions
                    if (msg.Length > 0) sw.WriteLine("AIRFIELD CONDITION SUMMARY");
                    sw.WriteLine(msg);

                    sw.Close();


                }
                catch (Exception ex) { Console.WriteLine("Radar Write1: " + ex.ToString()); }
            }

            var saveRADAR_REALISM = RADAR_REALISM;
            Timeout(wait_s, () =>
            {
                //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               


                foreach (var mess in radar_messages)
                {

                    if (saveRADAR_REALISM == 0) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                    else if (saveRADAR_REALISM >= 0) gpLogServerAndLog(new Player[] { player }, mess.Value, null);

                }
                radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(currtime_ms, radar_messages);

            });//timeout      
        }
    }//method radar     

    /******************************************************************************************************************** 
    * ****END****RADAR
    * **********************************************************************************************************************/


    //Red & blue point totals transferred from -stats.cs
    //REMEMBER that these are ints and they are percentage X100 (so that we have decimal percentageand so you must DIVIDE BY 100 to get decimal percentage points)
    //So we are going to do that here & keep them as doubles
    double RedTotalF = 0;
    double BlueTotalF = 0;
    double RedAirF = 0;
    double RedAAF = 0;
    double RedNavalF = 0;
    double RedGroundF = 0;
    double RedPlanesWrittenOffI = 0;
    double BlueAirF = 0;
    double BlueAAF = 0;
    double BlueNavalF = 0;
    double BlueGroundF = 0;
    double BluePlanesWrittenOffI = 0;
    public void CheckStatsData()
    {
        /************************************************
         * 
         * Check/download/transfer stats data
         * Recursive function called every X seconds
         ************************************************/
        Timeout(188, () => { CheckStatsData(); });



        // Read the stats file where we tally red & blue victories for the session
        //This allows us to make red/blue victories part of our mission objectives &
        //use the victory tallying mechanism in -stats.cs to do the work of keeping track of that
        try
        {
            using (StreamReader sr = new StreamReader(STATSCS_FULL_PATH + "SessStats.txt"))
            {
                string RedTotalS = sr.ReadLine();
                string BlueTotalS = sr.ReadLine();
                string TimeS = sr.ReadLine();
                string RedAirS = sr.ReadLine();
                string RedAAS = sr.ReadLine();
                string RedNavalS = sr.ReadLine();
                string RedGroundS = sr.ReadLine();
                string RedPlanesWrittenOffS = sr.ReadLine();
                string BlueAirS = sr.ReadLine();
                string BlueAAS = sr.ReadLine();
                string BlueNavalS = sr.ReadLine();
                string BlueGroundS = sr.ReadLine();
                string BluePlanesWrittenOffS = sr.ReadLine();

                //Only if they are recent (less than 125 seconds old) do we accept the numbers.
                //-stats.cs generally writes this data every 2 minutes, so older than that is an old mission or something
                DateTime Time = Convert.ToDateTime(TimeS);

                if (Time.AddSeconds(125).ToUniversalTime() > DateTime.Now.ToUniversalTime())
                {
                    RedTotalF = Convert.ToDouble(RedTotalS) / 100;
                    BlueTotalF = Convert.ToDouble(BlueTotalS) / 100;
                    //GamePlay.gpLogServer(null, "Read SessStats.txt: Times MATCH", null);
                    RedAirF = Convert.ToDouble(RedAirS) / 100;
                    RedAAF = Convert.ToDouble(RedAAS) / 100;
                    RedNavalF = Convert.ToDouble(RedNavalS) / 100;
                    RedGroundF = Convert.ToDouble(RedGroundS) / 100;
                    RedPlanesWrittenOffI = Convert.ToInt32(RedPlanesWrittenOffS);
                    BlueAirF = Convert.ToDouble(BlueAirS) / 100;
                    BlueAAF = Convert.ToDouble(BlueAAS) / 100;
                    BlueNavalF = Convert.ToDouble(BlueNavalS) / 100;
                    BlueGroundF = Convert.ToDouble(BlueGroundS) / 100;
                    BluePlanesWrittenOffI = Convert.ToInt32(BluePlanesWrittenOffS);
                }

                //GamePlay.gpLogServer(null, string.Format("RED session total: {0:0.0} BLUE session total: {1:0.0} Time1: {2:R} Time2 {3:R}",
                //      (double)(RedTotalF) / 100, (double)(BlueTotalF) / 100, Time.ToUniversalTime(), DateTime.Now.ToUniversalTime()), null);
                //GamePlay.gpLogServer(null, string.Format("RED session total: {0:0.0} BLUE session total: {1:0.0} ",
                //      (double)(RedTotalF) / 100, (double)(BlueTotalF) / 100), null);

            }
        }
        catch (Exception ex) { System.Console.WriteLine("Main mission - read sessstats.txt - Exception: " + ex.ToString()); }

        //Write the campaign summary text with current score etc.; this will go on the TEAM STATS page of the stats page
        try
        {
            string outputmsg = "Blue Objectives complete (" + InitialBlueObjectiveCount.ToString() + " points):" + (Objective_Total_Blue) + "<br>" + Environment.NewLine;
            outputmsg +="Red Objectives complete (" + InitialRedObjectiveCount.ToString() + " points):" + (Objective_Total_Red); 
            File.WriteAllText(STATSCS_FULL_PATH + "CampaignSummary.txt", outputmsg);
        }
        catch (Exception ex) { Console.WriteLine("CampaignSummary Write: " + ex.ToString()); }


        //Skipping all the points & map-turning related to stats for now.  Just transferring the data for possible use.
        /*
        //Check whether the 50-kill objective is reached.  
        if (!osk_Red50Kills && RedTotalF >= 50)
        {
            osk_RedObjCompleted += "50 total Team Kills - ";
            osk_Red50Kills = true;
            GamePlay.gpLogServer(null, "RED reached 50 Team Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("RED reached 50 Team Kills. Well done Red!");

        }
        if (!osk_Blue50Kills && BlueTotalF >= 50)
        {
            osk_BlueObjCompleted += "50 total Team Kills - ";
            osk_Blue50Kills = true;
            GamePlay.gpLogServer(null, "BLUE reached 50 Team Kills. Well done Team Blue!", new object[] { });
            GamePlay.gpHUDLogCenter("BLUE reached 50 Team Kills. Well done Blue!");
        }

        //Check whether the 50-kill objective is reached.  
        if (!osk_Red10AirKills && RedAirF >= 10)
        {
            osk_RedObjCompleted += "10 total Air Kills - ";
            osk_Red10AirKills = true;
            GamePlay.gpLogServer(null, "Red reached 10 total Air Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("Red reached 10  total Air Kills. Well done Red!");
        }
        if (!osk_Blue10AirKills && BlueAirF >= 10)
        {
            osk_BlueObjCompleted += "10 total Air Kills - ";
            osk_Blue10AirKills = true;
            GamePlay.gpLogServer(null, "BLUE reached 10 total Air Kills. Well done Team Blue!", new object[] { });
            GamePlay.gpHUDLogCenter("BLUE reached 10  total Air Kills. Well done Blue!");
        }
        if (!osk_Red10GroundKills && (RedAAF + RedNavalF + RedGroundF) >= 10)
        {
            osk_RedObjCompleted += "10 total AA/Naval/Ground Kills - ";
            osk_Red10GroundKills = true;
            GamePlay.gpLogServer(null, "Red reached 10 total AA/Naval/Ground Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("Red reached 10  total AA/Naval/Ground Kills. Well done Red!");
        }
        if (!osk_Blue10GroundKills && (BlueAAF + BlueNavalF + BlueGroundF) >= 10)
        {
            osk_BlueObjCompleted += "10 total AA/Naval/Ground Kills - ";
            osk_Blue10GroundKills = true;
            GamePlay.gpLogServer(null, "BLUE reached 10 total AA/Naval/Ground Kills. Well done Team Blue!", new object[] { });
            GamePlay.gpHUDLogCenter("BLUE reached 10  total AA/Naval/Ground Kills. Well done Blue!");
        }


        //RED has turned the map
        if (!osk_MapTurned && osk_LeHavreDam_destroyed && osk_OuistrehamDam_destroyed && osk_LeHavreFuelStorage_destroyed && osk_Red10AirKills && osk_Red10GroundKills && RedTotalF >= 50 && RedTotalF > BlueTotalF + 10)//We use RedTotalF >= 50 here, rather than osk_Red50Kills == true, because the team may get 50 kills but then LOSE SOME due to penalty points.
        {
            osk_RedObjCompleted += "10 more Team Kills than Blue - ";
            osk_MapTurned = true;
            EndMission(300, "RED");

        }


        //BLUE has turned the map
        if (!osk_MapTurned && osk_HambleDam_destroyed && osk_CowesDam_destroyed && osk_PortsmouthFuelStorage_destroyed && osk_Blue10AirKills && osk_Blue10GroundKills && BlueTotalF >= 50 && BlueTotalF > RedTotalF + 10)
        {
            osk_BlueObjCompleted += "10 more Team Kills than Red - ";
            osk_MapTurned = true;
            EndMission(300, "BLUE");

        }

        */


    }





    public string GetRedObjectivesString()
	{
		string StringToReturn="";
		StringToReturn= "Red Objectives complete (" + InitialRedObjectiveCount.ToString() + " points):\n";
		StringToReturn=StringToReturn + Objective_Total_Red;
        return StringToReturn;
	}
	
	public string GetBlueObjectivesString()
	{
		string StringToReturn="";
		StringToReturn= "Blue Objectives complete (" + InitialBlueObjectiveCount.ToString() + " points):\n";
        StringToReturn =StringToReturn + Objective_Total_Blue;
		return StringToReturn;
	}
	
	public string GetTimeLeftString()
	{
		string StringToReturn="";
		StringToReturn="Time Remaining In Mission:\n";
		
        TimeSpan Convert_Ticks = TimeSpan.FromMinutes((720000 - Time.tickCounter()) / 2000);//720000 denotes 6 hours of play
        string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);
		
		StringToReturn=StringToReturn + Time_Remaining;
		return StringToReturn;
	}

    ////////////////////////////////////////////////////////////////////////////////////////////////////

    // destroys aircraft abandoned by a player.
    private bool isAiControlledPlane(AiAircraft aircraft)
    {
        if (aircraft == null)
        {
            return false;
        }

        Player[] players = GamePlay.gpRemotePlayers();
        foreach (Player p in players)
        {
            if (p != null && (p.Place() is AiAircraft) && (p.Place() as AiAircraft) == aircraft)
            {
                return false;
            }
        }

        return true;
    }

    private void destroyPlane(AiAircraft aircraft)
    {
        if (aircraft != null)
        {
            aircraft.Destroy();
        }
    }

    private void explodeFuelTank(AiAircraft aircraft)
    {
        if (aircraft != null)
        {
            aircraft.hitNamed(part.NamedDamageTypes.FuelTank0Exploded);
        }
    }

    private void destroyAiControlledPlane(AiAircraft aircraft)
    {
        if (isAiControlledPlane2(aircraft))
        {
            destroyPlane(aircraft);
        }
    }

    private void damageAiControlledPlane(AiActor actor)
    {
        if (actor == null || !(actor is AiAircraft))
        {
            return;
        }

        AiAircraft aircraft = (actor as AiAircraft);

        if (!isAiControlledPlane2(aircraft))
        {
            return;
        }

        if (aircraft == null)
        {
            return;
        }

        aircraft.hitNamed(part.NamedDamageTypes.ControlsElevatorDisabled);
        aircraft.hitNamed(part.NamedDamageTypes.ControlsAileronsDisabled);
        aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
        aircraft.hitNamed(part.NamedDamageTypes.FuelPumpFailure);
        aircraft.hitNamed(part.NamedDamageTypes.Eng0TotalFailure);
        aircraft.hitNamed(part.NamedDamageTypes.ElecPrimaryFailure);
        aircraft.hitNamed(part.NamedDamageTypes.ElecBatteryFailure);

        aircraft.hitLimb(part.LimbNames.WingL1, -0.5);
        aircraft.hitLimb(part.LimbNames.WingL2, -0.5);
        aircraft.hitLimb(part.LimbNames.WingL3, -0.5);
        aircraft.hitLimb(part.LimbNames.WingL4, -0.5);
        aircraft.hitLimb(part.LimbNames.WingL5, -0.5);
        aircraft.hitLimb(part.LimbNames.WingL6, -0.5);
        aircraft.hitLimb(part.LimbNames.WingL7, -0.5);

        int iNumOfEngines = (aircraft.Group() as AiAirGroup).aircraftEnginesNum();
        for (int i = 0; i < iNumOfEngines; i++)
        {
            aircraft.hitNamed((part.NamedDamageTypes)Enum.Parse(typeof(part.NamedDamageTypes), "Eng" + i.ToString() + "TotalFailure"));
        }

        /***Timeout (240, () =>
                {explodeFuelTank (aircraft);}
            );
         * ***/

        Timeout(300, () =>
        { destroyPlane(aircraft); }
            );
    }

    //return TRUE if AI spawns should be stopped bec of too many players
    //FALSE if AI/trigger missions can continue
    public bool stopAI()
    {
        
        int nump = Calcs.gpNumberOfPlayers(GamePlay);
        Console.WriteLine("stopAI: " + nump.ToString() + " players currently online");
        if (nump > 50 || (nump > 40 && random.NextDouble() > 0.5))
        {
            Console.WriteLine("stopAI: Stopping AI Trigger/too many players online");
            return true;
        }
        else
        {
            Console.WriteLine("stopAI: NOT stopping AI Trigger/too few players online");
            return false;
        }
    }

    //The generic ONTRIGGER code often used to trigger all actions whenever a trigger is called
    /*
        public override void OnTrigger(int missionNumber, string shortName, bool active)
        {
            base.OnTrigger(missionNumber, shortName, active);
        Console.WriteLine("OnTrigger: Received trigger " + shortName + " mission#: " + missionNumber + " active: "+active.ToString());
        //AiAction action = GamePlay.gpGetAction(ActorName.Full(missionNumber, shortName));
        AiAction action = GamePlay.gpGetAction(shortName);
        if (action != null && active)
        {
            Console.WriteLine("OnTrigger: Activating action " + shortName + " from trigger " + shortName + " mission#: " + missionNumber);
            action.Do();
        }

          
        

            //AiAction action = GamePlay.gpGetAction(ActorName.Full(missionNumber, shortName));
            //if (action != null)
            //{
            //    action.Do();
            //}
            
        }
    */



    /*********************************************************************************************************************
     * *******************************************************************************************************************
     * ONTRIGGER
     * 
     * Handle bombing/objective destruction triggers as well as
     * triggers that launch various air raids and other events
     * 
     * In an action entry, the "1" indicates the action is enabled - 0 means disabled.  If set to disabled, then (it appears?)
     * the action wont activate if you call it.
     * 
     *  [Action]
     *    action1 ASpawnGroup 1 BoB_LW_KuFlGr_706.03
     * 
     * In OnTrigger, we can trigger any action if we know its name via action.Do() - it doesn't need to be the associated action in the .mis file
     * 
     * If there is no .cs file, CloD will automatically .Do() the action with the exact same name as the trigger when the trigger is called and the action is enabled. 
     * However, if you have .cs file then you must include OnTrigger include a bit of code to manually call the action with the same name as the trigger.  That is 
     * included below in the 'else' portion of the method.
     * 
     * You can call actions at any time; you don't need to rely on the trigger to set it off.  You only need to know the exact name of the 
     * trigger from the .mis file Sample:
     * 
     *      AiAction action = GamePlay.gpGetAction(tr);
     *      if (action != null) action.Do();
     * 
     * You can also call a Trigger at any time using this sample code:
     *             
     *             if (GamePlay.gpGetTrigger(tr) != null ) { 
     *                 GamePlay.gpGetTrigger(tr).Enable = true; //only needed if you want to be sure the Trigger is enabled               
     *                 Battle.OnEventGame(GameEventId.Trigger, tr, true, 1);
     *             }
     *             
     * You can call both triggers and actions from any mission--either the main mission file or any sub-mission loaded via gpPostMissionLoad.
     * You only need the EXACT name from the .mis file.
     *             
     * 
     * ****************************************************************************************************************/
    //
    public override void OnTrigger(int missionNumber, string shortName, bool active)
    //public void bartOnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);
        Console.WriteLine("OnTrigger: " + shortName + " Active: " + active.ToString());

        bool res = MO_DestroyObjective(shortName, active);

        Console.WriteLine("OnTrigger: " + shortName + " Active: " + active.ToString() + "MO_DestroyObjective result: " + res.ToString());


        Console.WriteLine("OnTrigger: Now doing ActionTriggers: " + shortName + " Active: " + active.ToString() + " !stopAI()=" + (!stopAI()).ToString() + " zonedef: " + ("zonedefenseblue1".Equals(shortName) && active && !stopAI()).ToString());

        ///Timed raids into enemy territory////////////////////////// using the action part of the trigger//

        //if you want any patrols etc to continue running even when the server is full of live players, just remove the  && !stopAI() of that trigger
        //stopAI will slow down AI patrols with 40 players online and stop them at 50 (adjustable above).

        if ("F1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action1");


            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "Do 17s requesting escort! Meet at Calais at 6km in 10 minutes", null));
            Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F1e".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action1e");


            if (action != null)
            {
                action.Do();
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F1c".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action1c");


            if (action != null)
            {
                action.Do();
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action2");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort! Meet at Dymchurch @ 20K ft. in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "Wellingtons have been spotted over Dymchurch at 6000m heading east!!!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action3");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "Ju88s requesting escort. Meet at Oye-Plage @ 6000m in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @ 20K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F4".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action4");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at 20K ft. over Lypne in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympe at 6000m!!!.", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F5".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action5");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "BR.20Ms requesting escort.  Meet at Boulogne @ 6km in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(1, "BR.20Ms have been spotted over Boulogne @ 12000 ft. heading west! with escorts", null));
        }
        else if ("F5e".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action5e");
            if (action != null)
            {
                action.Do();
            }

        }
        else if ("F6".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action6");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort.  Meet at 20K ft. over St. Mary's Bay in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
        else if ("F7".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action7");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "He-111s requesting escort.  Meet over Calais @ 6km in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(1, "He-111s have been spotted over Calais @ 14K ft. heading west!", null));
        }
        else if ("F7e".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action7e");
            if (action != null)
            {
                action.Do();
            }

        }
        else if ("F8".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action8");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at St. Mary's Bay at 20K ft in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Blenheims have been spotted over St. Mary's Bay at 6km.", null));
        }
        else if ("F9".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action9");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "Do 17s requesting escort! Meet at Calais at 6km in 10 minutes", null));
            Timeout(600, () => sendScreenMessageTo(1, "Second run Do 17s spotted over Calais @ 6000m heading west!!!", null));
        }
        else if ("F10".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action10");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort! Meet at Dymchurch @ 20K ft. in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "Wellingtons have been spotted over Dymchurch at 6000m heading east!!!", null));
        }
        else if ("F11".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action11");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "Ju88s requesting escort. Meet at Oye-Plage @ 6000m in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @20K ft. heading west!", null));
        }
        else if ("F12".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action12");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at 20K ft. over Lympne in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympne at 6000m!!!.", null));
        }
        else if ("F13".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "BR.20Ms requesting escort.  Meet at Boulogne @ 6km in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(1, "BR.20Ms have been spotted over Boulogne @ 13K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F13e".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13e");
            if (action != null)
            {
                action.Do();
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F13c".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13c");
            if (action != null)
            {
                action.Do();
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F13cc".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13cc");
            if (action != null)
            {
                action.Do();
            }

            Timeout(600, () => sendScreenMessageTo(1, "testing Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F14".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action14");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort.  Meet at 20K ft. over St. Mary's Bay in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
        else if ("F15".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action15");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(2, "He-111's requesting escort.  Meet over Calais @ 6km in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(1, "He-111's have been spotted over Calais @ 20K ft. heading west!", null));
        }
        else if ("F16".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action16");
            if (action != null)
            {
                action.Do();
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at St. Mary's Bay at 20K ft in 10 minutes.", null));
            Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Blenheims have been spotted over St. Mary's Bay at 6km.", null));
        }
        else if ("F17".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action17");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Do 17's have been spotted east of Calais @ 20K ft.", null);
            sendScreenMessageTo(2, " third run Do 17's requesting escort! Meet at Calais at 6000m in 10 minutes", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F18".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action18");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Wellingtons requesting escort! Meet at Dymchurch @ 20K ft. in 10 minutes.", null);
            sendScreenMessageTo(2, "Wellingtons have been spotted west of Dymchurch at 6000m.  Destroy them!", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("escort1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort1");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Escort1 109s launched", null);
            sendScreenMessageTo(2, "Cover 109s launched for test", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("escort2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Escort2 Cover 109s launched", null);
            sendScreenMessageTo(2, "Secondary Cover 109s launched for test", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("escort3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Escort3 Cover 109s launched", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("redspitcover1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Spits launched", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("redhurrycover1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("redhurrycover1");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Hurricanes launched", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Willmingtondefensered".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Willmingtondefensered");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Willmington", null);
            sendScreenMessageTo(2, "Spits launched launched for yet another  test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset Willmington(ZD1)30 min", null);
            });
        }
        else if ("Redhilldefensered2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Redhilldefensered2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Redhill", null);
            sendScreenMessageTo(2, "Spits launched Redhill defense test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset Redhill(ZD2)30 min", null);
            });
        }

        else if ("AirdefenseCalais".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Calais", null);
            sendScreenMessageTo(2, "Air defense Calais ", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset Calais2 30 min", null);
            });
        }

        else if ("StOmar".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StOmar");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense St Omar", null);
            sendScreenMessageTo(2, "Air defense St Omar test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset St Omar 30 min", null);
            });
        }
        else if ("HighAltCalais".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("HighAltCalais");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "HighAltCalais", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset HighAltCalais 30 min", null);
            });
        }
        else if ("109Cover3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("109Cover3");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense test of code 3 hr launch", null);
            sendScreenMessageTo(1, "Mission time should be top of hour4", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Gladiatorintercept".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Gladiatorintercept");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Gladiator intercept Test of trigger", null);
            sendScreenMessageTo(2, "Gladiator intercept  test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(2600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, "Timer reset Dungeness trigger active", null);
            });
        }

        else if ("zonedefenseblue1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue1");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "A 110 cover Letoquet Test of trigger", null);
            sendScreenMessageTo(2, "110  test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, "Timer reset Letoquet(BZ1)trigger active", null);
            });                      
        }
        else if ("zonedefenseblue2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Oye Plague", null);
            sendScreenMessageTo(2, "Air defense Oye Plague test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset Oye Plague 30 min", null);
            });
        }



        else if ("zonedefenseblue3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue3");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Wissant", null);
            sendScreenMessageTo(2, "Air defense Wissant test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset Wissant3 30 min", null);
            });
        }
        else if ("zonedefenseblue4".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue4");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Calais", null);
            sendScreenMessageTo(2, "Air defense Calais test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset Calais offshore 30 min", null);
            });
        }
        else if ("London1air".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("London1air");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense London", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset London 30 min", null);
            });
        }
        else if ("London2air".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("London2air");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense London2", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset London 30 min", null);
            });
        }
        else if ("London3air".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("London3air");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense London3", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset London 30 min", null);
            });
        }
        else if ("Thems1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Thems1");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Eastchurch", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset North 30 min", null);
            });
        }
        else if ("Beau11".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Beau11");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Air defense Brookland", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(1800, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
                sendScreenMessageTo(1, " Trigger reset North 30 min", null);
            });
        }
        else if ("Fatal1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal1");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Coastal Patrol 1", null);
            GamePlay.gpLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Fatal2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Coastal Patrol 2", null);
            GamePlay.gpLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Fatal3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal3");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Coastal Patrol 3", null);
            GamePlay.gpLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Fatal4".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal4");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Coastal Patrol 4", null);
            GamePlay.gpLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense1");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Northern Patrol 1", null);
            GamePlay.gpLogServer(null, "Pegwell defense triggered look for aicraft pegwell bay", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense2");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Northern Patrol 2", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense3");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Northern Patrol 3", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense4");
            if (action != null)
            {
                action.Do();
            }
            sendScreenMessageTo(1, "Northern Patrol 4", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;

        } 
        else
        {
            //This final ELSE ensures that any other triggers in the mission file (ie time triggers)
            //that have an action with matching name, and that are not handled individually in the code above
            //will still activate their triggers
            //
            //This way you can EITHER handle triggers & actions individually using code above, OR just
            //enter triggers & actions with exactly matching names in the .mis file (including submission files loaded later)
            //and they will still work.

            Console.WriteLine("OnTrigger: Received trigger " + shortName + " mission#: " + missionNumber);
            //AiAction action = GamePlay.gpGetAction(ActorName.Full(missionNumber, shortName));
            AiAction action = GamePlay.gpGetAction(shortName);
            if (action != null && active && !stopAI())
            {
                Console.WriteLine("OnTrigger: Activating action " + shortName + " from trigger " + shortName + " mission#: " + missionNumber);
                action.Do();
            }
        }



    }  //End OnTrigger

    //Save results to file that will be read by the WatchDog program.  1= red win, 2 = blue win, 3= tie
    public bool WriteResults_Out_File(string result = "3") {
		try {
			using (StreamWriter file = new StreamWriter(RESULTS_OUT_FILE,false))
			{
				file.WriteLine(result);
			}			
			Console.WriteLine ("WriteResults_Out_File - file & contents: " + RESULTS_OUT_FILE + " " + result); 			
			return true;
			
		}
        catch (Exception ex) { 
		
			Console.WriteLine("WriteResults_Out_File( - Error writing Mission RESULTS_OUT_FILE: " + RESULTS_OUT_FILE + " " + ex.Message); 
			return false;
		
		}	
	}
    
    public override void OnTickGame()
    {
        /* Tick_Mission_Time = 720000 - Time.tickCounter();
        var Mission_Time = Tick_Mission_Time / 2000;
        TimeSpan Convert_Ticks = TimeSpan.FromMinutes(Mission_Time);
        string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);
        */
        int tickSinceStarted = Time.tickCounter();

        if ((tickSinceStarted) == 0)
        {
            GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
			WriteResults_Out_File("3"); //1=red, 2= blue, 3=tie; we pre-set to tie in case the mission exits early etc.
            Timeout(188, () => { CheckStatsData(); }); //  Start the routine to transfer over stats, a/c killed, etc; Delay a while so sessStats.txt etc are already in place
        }        

        if (Time.tickCounter() % 30000 == 1000)
        {
			
            GamePlay.gpLogServer(null, "Completed Red Objectives (" + InitialRedObjectiveCount.ToString() + " points):", new object[] { });
            GamePlay.gpLogServer(null, (Objective_Total_Red), new object[] { });
            Timeout(10, () =>
            GamePlay.gpLogServer(null, "Completed Blue Objectives (" + InitialBlueObjectiveCount.ToString() + " points):", new object[] { }));
            Timeout(11, () =>
            GamePlay.gpLogServer(null, (Objective_Total_Blue), new object[] { }));
            Timeout(12, () =>
            GamePlay.gpLogServer(null, showTimeLeft(), new object[] { }));

            stopAI();//for testing
        }

        if (Time.tickCounter() == 720000)// Red battle Success.
		//if (Time.tickCounter() == 720)// Red battle Success.  //For testing/very short mission
        {
			
		WriteResults_Out_File("3");	
	    Timeout(10, () =>
	    {
            	GamePlay.gpLogServer(null, "The match ends in a tie!  Objectives still left for both sides!!!", new object[] { });
            	GamePlay.gpHUDLogCenter("The match ends in a tie! Objectives still left for both sides!!!");
	    });
        EndMission(70,"");
        }

        //Ticks below write out TOPHAT radar files for red, blue, & admin
        //We do each every ~minute but space them out a bit from each other
        //roughly every one minute
        //do this regardless of whether players are loaded, so it must be first here
        if ((Time.tickCounter()) % 1000 == 0)
        {
            ///////////////////////////////////////////    
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            //Console.WriteLine("Writing current radar returns to file");
            RADAR_REALISM = -1;
            listPositionAllAircraft(GamePlay.gpPlayer(), -1, false); //-1 & false will list ALL aircraft of either army
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            RADAR_REALISM = saveRealism;

        }
        if ((Time.tickCounter()) % 1000 == 334)
        {
            ///////////////////////////////////////////    
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            //Console.WriteLine("Writing current radar returns to file");
            RADAR_REALISM = -1;
            listPositionAllAircraft(GamePlay.gpPlayer(), -2, false); //-1 & false will list ALL aircraft of either army
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            RADAR_REALISM = saveRealism;

        }
        if ((Time.tickCounter()) % 1000 == 666)
        {
            ///////////////////////////////////////////    
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            //Console.WriteLine("Writing current radar returns to file");
            RADAR_REALISM = -1;
            listPositionAllAircraft(GamePlay.gpPlayer(), -3, false); //-1 & false will list ALL aircraft of either army
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            RADAR_REALISM = saveRealism;

        }




        //periodically remove a/c that have gone off the map
        if ((tickSinceStarted) % 2100 == 0)
        {

            RemoveOffMapAIAircraft();


        }


        if ((tickSinceStarted) % 10100 == 0)
        {
            //Write all a/c position to log
            if (LOG)
            {
                DebugAndLog(calcTimeLeft() + " left in mission " + MISSION_ID);
                int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
                RADAR_REALISM = 0;
                listPositionAllAircraft(GamePlay.gpPlayer(), 1, true);
                listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
                RADAR_REALISM = saveRealism;
            }

        }


    }

    /************************************************************
    * 
    * handle airport bombing
    * most credit/script idea for airport bombing & destruction goes to reddog/Storm of War
    * 
    * We give credit (points) for any bomb that hits within the radius of an airfield.
    * Also, these bomb hits are marked with a plume of smoke and additionally a bomb crater is added that is dangerous/will kill aircraft taxiing on the ground
    * 
    * Craters are different sizes, depending on tonnage of bomb dropped.  Also, craters will be repaired, taking a shorter time for smaller craters & a longer time for bigger craters
    * Additionally, the more craters dropped on an airport the longer it will take to get to the next crater  & repair it.
    * Also, if a threshold of tonnage (counted as points, which are proportional to damage done) is reached, the airport is put out of commission by severely cratering it
    * 
    * //Version for -MAIN.cs//
    *************************************************************/

    public Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>> AirfieldTargets = new Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>>();
    //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius, Point3d airfield center (position)
    //TODO: it would nice to have a struct or something to hold this instead of a tuple . . . 

    public void SetAirfieldTargets()
    {
        foreach (AiAirport ap in GamePlay.gpAirports()) //Loop through all airfields in the game
        {

            //We're just going to add ALL airfields as targets, but then make sure there are no duplicates (bec. built-in & .mis-added airports sometimes overlap).

            //It's going to take blue pilots more points/bombs to knock out an airfield, vs Red (Blenheims very limited as far as the # of bombs they can carry)

            ////Use this for TACTICAL SERVER (where Reds only have Blenheims)
            //UPDATE 2017/11/06: We don't need this adjustment bec. we have adjusted the points received
            //so that blenheims receive relatively more & the blue bombers relatively less.  So this 
            //should handle the discrepancy between the sides with no further adjustment necessary
            //int pointstoknockout = 30;
            //if (ap.Army() != null && ap.Army() == 1) pointstoknockout = 65;

            ////Use this for MISSION SERVER (where Reds have access to HE111 and JU88)
            ////Use this for MISSION SERVER  && TACTICAL SERVER 
            int pointstoknockout = 65;  //This is about two HE111 or JU88 loads (or 1 full load & just a little more) and about 4 Blennie loads, but it depends on how accurate the bombs are, and how large

            double radius = ap.FieldR();
            Point3d center = ap.Pos();


            //GamePlay.gpAirports() includes both built-in airports and any new airports we have added in our .mis files. This results in duplication since
            //most .mis airports are placed on top of an existing built-in airport. We check whether this airport has already been added & skip adding it if so.
            Point3d pos = ap.Pos();
            bool add = true;
            foreach (AiAirport apk in AirfieldTargets.Keys)//Loop through the targets
            {
                if (apk != null & apk.Pos().distance(ref pos) <= apk.FieldR())
                {
                    //AirfieldTargets[apk].Item3
                    add = false; //
                    if (apk.FieldR() != null && apk.FieldR() > 1) radius = apk.FieldR(); //The field radius set in the .mis file becomes operative if it exists & is reasonable
                    center = apk.Pos();  //We use the position of the airport set i nthe .mis file for the center, if it exists - thus we can change/move the center position as we wish
                    break;
                }
            }

            //We'll get the NAME of the airport from the birthplace/spawn point declare in a .mis file, if it exists

            string apName = ap.Name();
            foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())
            {
                if (bp != null & bp.Pos().distance(ref pos) <= ap.FieldR())
                {
                    if (bp.Name() != null && !(bp.Name().ToUpper().Contains("BIRTHPLACE"))) apName = bp.Name();  //We will use the spawn point/birthplace name UNLESS it is just "BirthPlace0" or whatever
                    break;
                }
            }


            if (add) AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, apName, pointstoknockout, 0, DateTime.Now, radius, center)); //Adds airfield to dictionary, requires approx 2 loads of 32 X 50lb bombs of bombs to knock out.
                                                                                                                                                                                    //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius
                                                                                                                                                                                    //if you want to add only some airfields as targets, use something like: if (ap.Name().Contains("Manston")) { }

        }
        GamePlay.gpLogServer(null, "SetAirfieldTargets initialized.", null);
    }

    public string ListAirfieldTargetDamage(Player player = null, int army = -1, bool all = false, bool display = true)
    {
        int count = 0;
        string returnmsg = "";
        if (AirfieldTargets != null) foreach (AiAirport ap in AirfieldTargets.Keys)
            {

                double PointsTaken = AirfieldTargets[ap].Item4;
                bool disabled = AirfieldTargets[ap].Item1;

                if (!all && PointsTaken == 0 && !disabled) continue; //we'll list only airports damaged or disabled, skipping those with no damage at all, unless called with all=true
                if (army != -1 & army != ap.Army()) continue; //List only the army requested, skipping the others.  army = -1 means list both/all armies

                count++;
                double PointsToKnockOut = AirfieldTargets[ap].Item3;
                string Mission = AirfieldTargets[ap].Item2;
                DateTime lastBombHit = AirfieldTargets[ap].Item5;

                double percent = 0;
                if (PointsToKnockOut > 0)
                {
                    percent = PointsTaken / PointsToKnockOut;
                }

                double timereduction = 0;
                if (percent > 0)
                {
                    timereduction = (DateTime.Now - lastBombHit).TotalSeconds;
                }

                double timetofix = PointsTaken * 20 * 60 - timereduction; //50 lb bomb scores 0.5 so will take 10 minutes to repair.  Larger bombs will take longer; 250 lb about 1.4 points so 28 minutes to repeari
                                                                          //But . . . it is ADDITIVE. So the first 50 lb bomb takes 10 minutes, the 2nd another 10, the 3rd another 10, and so on on.  So if you drop 32 50 bl bombs it will take 320 minutes before the 32nd bomb crater is repaired.
                                                                          //Sources: "A crater from a 500lb bomb could be repaired and resurfaced in about 40 minutes" says one 2nd hand source. That seems about right, depending on methods & surface. https://www.airspacemag.com/multimedia/these-portable-runways-helped-win-war-pacific-180951234/
                                                                          //unfortunately we can repair only the bomb crater; the SMOKE will remain for the entire mission because clod internals don't allow its removal.
                                                                          //TODO: We could keep track of when the last bomb was dropped at each airport and deduct time here depending on how much repair had been done since the last bomb dropped

                if (PointsTaken >= PointsToKnockOut) //airport knocked out
                {
                    percent = 1;
                    timetofix = 24 * 60 * 60; //24 hours to repair . . . 
                }

                string msg = Mission + " " + (percent * 100).ToString("n0") + "% destroyed; last hit " + (timereduction / 60).ToString("n0") + " minutes ago";
                returnmsg += msg + "\n";

                if (display) GamePlay.gpLogServer(new Player[] { player }, msg, new object[] { });


            }
        if (count == 0)
        {
            string msg = "No airports damaged or destroyed yet";
            if (display) GamePlay.gpLogServer(new Player[] { player }, msg, new object[] { });
            returnmsg = ""; //In case of display == false we just don't return any message at all, allowing this bit to simply be omitted
        }

        return returnmsg;
    }

    //stamps a rectangular pattern of craters over an airfield to disable it
    public void AirfieldDisable(AiAirport ap)

    {
        string apName = ap.Name();
        double radius = ap.FieldR();
        Point3d pos = ap.Pos();

        if (AirfieldTargets.ContainsKey(ap))
        {
            apName = AirfieldTargets[ap].Item2;
            radius = AirfieldTargets[ap].Item6;
            pos = AirfieldTargets[ap].Item7;

        }

        GamePlay.gpHUDLogCenter(null, "Airfield " + apName + " has been disabled");

        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";

        string val1 = "Stationary";
        string type = "BombCrater_firmSoil_largekg";
        int count = 0;
        string value = "";


        for (double x = pos.x - radius * 1.1; x < pos.x + radius * 1.1; x = x + 80)
        {
            for (double y = pos.y - radius * 1.1; y < pos.y + radius * 1.1; y = y + 80)
            {
                string key = "Static" + count.ToString();
                value = val1 + ".Environment." + type + " nn " + (x - 100 + 200 * stb_random.NextDouble()).ToString("0.00") + " " + (y - 100 + 200 * stb_random.NextDouble()).ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + pos.z.ToString("0.00");
                f.add(sect, key, value);
                count++;

            }

        }
        //f.save(CLOD_PATH + FILE_PATH + "airfielddisableMAIN-ISectionFile.txt"); //testing
        GamePlay.gpPostMissionLoad(f);
        //Timeout(stb_random.NextDouble() * 5, () => { GamePlay.gpPostMissionLoad(f); });

    }
    /*
        public void LoadAirfieldSpawns()
        {

            return;

            //Ok, this part would need to be placed in -MAIN.cs to work, and also know that mission ID and also we would need to set up special .mis files with each airport.
            //so, maybe we will do all that later, and maybe not
            //For now we are just skipping this part altogether (return;)
            //Instead we just disable the destroyed airport in-game by covering it with the dangerous type of bomb crater

            foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())

            {
                bp.destroy();//Removes all spawnpoints
            }
        //        GamePlay.gpPostMissionLoad("missions/London Raids/nondestroyable.mis");

            foreach (AiAirport ap in AirfieldTargets.Keys)
            {
                if (AirfieldTargets[ap].Item1)
                {
                    //Airfield still active so load mission
                    GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "YOUR MISSION FOLDER/" + AirfieldTargets[ap].Item2 + ".mis");
                }
            }


        }
    */



    /*****************************************************************************
     * 
     * OnBombExplosion - handling routines for area bombing, bombing of civilian areas, and bombing of airports
     * 
     *****************************************************************************/

    //Do various things when a bomb is dropped/explodes.  For now we are assessing whether or not the bomb is dropped in a civilian area, and giving penalties if that happens.
    //TODO: X Give points/credit for bombs dropped on enemy airfields and/or possibly other targets of interest.
    //TODO: This is the sort of thing that could be pushed to the 2nd thread/multi-threaded
    public override void OnBombExplosion(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {

        base.OnBombExplosion(title, mass_kg, pos, initiator, eventArgInt);

        //GamePlay.gpLogServer(null, "bombe 1", null);
        bool ai = true;
        if (initiator != null && initiator.Player != null && initiator.Player.Name() != null) ai = false;

        //GamePlay.gpLogServer(null, "bombe 2", null);
        int isEnemy = 1; //0 friendly, 1 = enemy, 2 = neutral
        int terr = GamePlay.gpFrontArmy(pos.x, pos.y);

        //GamePlay.gpLogServer(null, "bombe 3", null);
        if (terr == 00) isEnemy = 2;
        if (!ai && initiator.Player.Army() == terr) isEnemy = 0;
        //GamePlay.gpLogServer(null, "bombe 4", null);


        //TF_GamePlay.gpIsLandTypeCity(maddox.game.IGamePlay, pos);       

        /********************
         * 
         * Handle airport bombing
         * 
         *******************/

        //GamePlay.gpLogServer(null, "bombe 5", null);

        var apkeys = new List<AiAirport>(AirfieldTargets.Keys.Count);
        apkeys = AirfieldTargets.Keys.ToList();

        maddox.game.LandTypes landType = GamePlay.gpLandType(pos.x, pos.y);

        //For now, all things we handle below are on land, so if the land type is water we just
        //get out of here immediately
        if (landType == maddox.game.LandTypes.WATER) return;

        //GamePlay.gpLogServer(null, "bombe 6", null);

        bool blenheim = false;
        AiAircraft aircraft = initiator.Actor as AiAircraft;
        string acType = Calcs.GetAircraftType(aircraft);
        if (acType.Contains("Blenheim")) blenheim = true;

        foreach (AiAirport ap in apkeys)//Loop through the targets; we do it on a separate copy of the keys list bec. we are changing AirfieldTargets mid-loop, below
        {
            /* if (!AirfieldTargets[ap].Item1)
            {//airfield has already been knocked out so do nothing
            }
            else
            { */

            //GamePlay.gpLogServer(null, "bombe 7", null);
            double radius = AirfieldTargets[ap].Item6;
            Point3d APPos = AirfieldTargets[ap].Item7;
            string apName = AirfieldTargets[ap].Item2;
            double distFromCenter = 1000000000;
            if (ap != null) distFromCenter = APPos.distance(ref pos);
            //Check if bomb fell inside radius and if so increment up
            if (ap != null & distFromCenter <= radius)//has bomb landed inside airfield check
            {


                //So, the Sadovsky formula is a way of estimating the effect of a blast wave from an explosion. https://www.metabunk.org/attachments/blast-effect-calculation-1-pdf.2578/
                //Simplifying slightly, it turns out that the radius of at least partial destruction/partial collapse of buildings is:
                // 50 lb - 30m; 100 lb - 40 m; 250 lb - 54 m; 500 lb - 67 m; 100 lb - 85 m; etc.
                //Turning this radius to an 'area of destruction' (pi * r^2) gives us an "area of destruction factor" for that size bomb.  
                //Since we are scoring the amount of destruction in e.g. an industrialized area, counting the destruction points as area (square footage, square meters, whatever) is reasonable.
                //Scaling our points in proportion to this "area of destruction factor" so that a 50 lb pound bomb gives 0.5 points, then we see that destruction increases with size, but lower than linearly.
                //So if a 50 lb bomb gives 0.5 points, a 100 lb bomb gives 0.72 points; 250 lb 1.41 points; 500 lb 2.33 points, 1000 lb 4.0 points, 2000 lb 6.48 points, etc
                //The formula below is somewhat simplified from this but approximates it pretty closely and gives a reasonable value for any mass_kg
                //This score is also closely related to the amount of ground churn the explosive will do, which is going to be our main effect on airport closure


                //double scoreBase = 0.06303;
                double scoreBase = 0.031515; //halving the score we were giving at first, since the Bomber pilot point totals seem to be coming up quite high in comparison with fighter kills
                if (blenheim) scoreBase *= 8; //double score for Blenheims since their bomb load is pathetic (double the OLD score which is 4X the NEW score.  This makes 1 Blenheim (4 bombs X 4) about 50% as effective as on HE 11. (32 bombs)             

                //Give more points for hitting more near the center of the airfield.  This will be the (colored) airfield marker that shows up IE on the map screen
                //TODO: Could also give more if exactly on the the runway, or near it, or whatever
                double multiplier = 0.5;
                if (distFromCenter <= 2 * radius / 3) multiplier = 1;
                if (distFromCenter <= radius / 3) multiplier = 1.5;

                //If 'road' then this seems to mean it is a PAVED runway or taxiway, so we give extra credit                
                if (landType == maddox.game.LandTypes.ROAD || landType == maddox.game.LandTypes.ROAD_MASK || landType == maddox.game.LandTypes.HIGHWAY)
                {
                    multiplier = 1.6;
                }

                scoreBase *= multiplier;


                if (mass_kg <= 0) mass_kg = 22;  //50 lb bomb; 22kg
                double score = scoreBase * Math.Pow(mass_kg, 0.67);

                /* Another way to reach the same end- probably quicker but less flexible & doesn't interpolate:
                 * 
                 * //Default is 0.5 points for ie 50 lb bomb
                 * if (mass_kg > 45) score = 0.722; //100 lb  (calcs assume radius of partial/serious building destruction per Sadovsky formula, dP > 0.10, explosion on surface of ground, and that 50% of bomb weight is TNT)
                if (mass_kg > 110) score = 1.41; //250 
                if (mass_kg > 220) score = 2.33; //500
                if (mass_kg > 440) score = 3.70; //1000
                if (mass_kg > 880) score = 5.92; //2000
                if (mass_kg > 1760) score = 9.33 ; //4000

                //UPDATE 5 Nov 2017: Bomber scores seem relatively too high so cutting this in half (though doubling it for Blennies since they are bomb-impaired)

                 */

                double individualscore = score;

                if (!ai && (isEnemy == 0 || isEnemy == 2))
                {
                    individualscore = -individualscore;  //Bombing on friendly/neutral territory earns you a NEGATIVE score
                                                         //but, still helps destroy that base (for your enemies) as usual
                                                         //GamePlay.gpLogServer(null, initiator.Player.Name() + " has bombed a friendly or neutral airport. Serious repercussions for player AND team.", new object[] { });
                }



                //TF_Extensions.TF_GamePlay.Effect smoke = TF_Extensions.TF_GamePlay.Effect.SmokeSmall;
                // TF_Extensions.TF_GamePlay.gpCreateEffect(GamePlay, smoke, pos.x, pos.y, pos.z, 1200);
                string firetype = "BuildingFireSmall";
                if (mass_kg > 200) firetype = "BuildingFireBig"; //500lb bomb or larger
                if (stb_random.NextDouble() > 0.25) firetype = "";
                //todo: finer grained bigger/smaller fire depending on bomb tonnage

                //GamePlay.gpLogServer(null, "bombe 8", null);

                //set placeholder variables
                double PointsToKnockOut = AirfieldTargets[ap].Item3;
                double PointsTaken = AirfieldTargets[ap].Item4 + score;
                string Mission = AirfieldTargets[ap].Item2;
                bool disabled = AirfieldTargets[ap].Item1;
                DateTime lastBombHit = AirfieldTargets[ap].Item5;


                string cratertype = "BombCrater_firmSoil_mediumkg";
                if (mass_kg > 100) cratertype = "BombCrater_firmSoil_largekg"; //250lb bomb or larger
                if (mass_kg > 200) cratertype = "BombCrater_firmSoil_EXlargekg"; //500lb bomb or larger.  EXLarge is actually 3 large craters slightly offset to make 1 bigger crater

                double percent = 0;
                double prev_percent = 0;
                double points_reduction_factor = 1;
                if (PointsToKnockOut > 0)
                {
                    percent = PointsTaken / PointsToKnockOut;
                    prev_percent = (PointsTaken - score) / PointsToKnockOut;
                    if (prev_percent > 1) prev_percent = 1;
                    if ((prev_percent == 1) && (percent > 1)) points_reduction_factor = percent * 2; // So if they keep bombing after the airport is 100% knocked out, they keep getting points but not very many.  The more bombing the less the points per bomb.  So they can keep bombing for strategic reasons if they way (deny use of the AP) but they won't continue to accrue a whole bunch of points for it.
                }

                //GamePlay.gpLogServer(null, "bombe 8", null);

                individualscore = individualscore / points_reduction_factor;  //reduce the score if needed 

                //if (!ai) stb_RecordStatsOnActorDead(initiator, 4, individualscore, 1, initiator.Tool.Type);  //So they have dropped a bomb on a target so they get some point score


                double timereduction = 0;
                if (prev_percent > 0)
                {
                    timereduction = (DateTime.Now - lastBombHit).TotalSeconds;
                }

                double timetofix = PointsTaken * 20 * 60 - timereduction; //50 lb bomb scores 0.5 so will take 10 minutes to repair.  Larger bombs will take longer; 250 lb about 1.4 points so 28 minutes to repeari
                                                                          //But . . . it is ADDITIVE. So the first 50 lb bomb takes 10 minutes, the 2nd another 10, the 3rd another 10, and so on on.  So if you drop 32 50 bl bombs it will take 320 minutes before the 32nd bomb crater is repaired.
                                                                          //Sources: "A crater from a 500lb bomb could be repaired and resurfaced in about 40 minutes" says one 2nd hand source. That seems about right, depending on methods & surface. https://www.airspacemag.com/multimedia/these-portable-runways-helped-win-war-pacific-180951234/
                                                                          //unfortunately we can repair only the bomb crater; the SMOKE will remain for the entire mission because clod internals don't allow its removal.
                                                                          //TODO: We could keep track of when the last bomb was dropped at each airport and deduct time here depending on how much repair had been done since the last bomb dropped

                if (timetofix < score * 20 * 60) timetofix = score * 20 * 60; //timetofix is never less than the time needed to fix this one bomb crater, even if the airport has accrued some repair time

                if (PointsTaken >= PointsToKnockOut) //airport knocked out
                {
                    percent = 1;
                    timetofix = 24 * 60 * 60; //24 hours to repair . . . 
                }
                //Advise player of hit/percent/points
                //if (!ai) GamePlay.gpLogServer(new Player[] { initiator.Player }, "Airport hit: " + (percent * 100).ToString("n0") + "% destroyed " + mass_kg.ToString("n0") + "kg " + individualscore.ToString("n1") + " pts " + (timetofix/3600).ToString("n1") + " hr to repair " , new object[] { }); //+ (timereduction / 3600).ToString("n1") + " hr spent on repairs since last bomb drop"

                //loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, timetofix, stb_FullPath, cratertype);

                //Sometimes, advise all players of percent destroyed, but only when crossing 25, 50, 75, 100% points
                Timeout(3, () => { if (percent * 100 % 25 < prev_percent * 100 % 25) GamePlay.gpLogServer(null, Mission + " " + (percent * 100).ToString("n0") + "% destroyed ", new object[] { }); });

                //GamePlay.gpLogServer(null, "bombe 8", null);

                if (PointsTaken >= PointsToKnockOut) //has points limit to knock out the airport been reached?
                {
                    AirfieldTargets.Remove(ap);
                    AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(true, Mission, PointsToKnockOut, PointsTaken, DateTime.Now, radius, APPos));
                    if (!disabled)
                    {
                        //TODO: Sometimes this doesn't seem to add points to the correct army?  Maybe the army # is wrong or doesn't exist for some ap's ?
                        //Code below is supposed to fix this, but we'll see.
                        int arm = ap.Army();
                        if (arm != 1 && arm != 2)
                        {
                            arm = GamePlay.gpFrontArmy(APPos.x, APPos.y);  //This can be 1,2, or 0 for neutral territory.  
                        }

                        /*
                         
                        if (arm == 1) CampaignMapBluePoints += 5; //5 campaign points for knocking out an airfield
                        else if (arm == 2) CampaignMapRedPoints += 5;
                        */

                        //Question: Do we want to keep these objective points for knocking out any airfield?
                        if (arm == 1)
                        {
                            InitialBlueObjectiveCount++; //1 campaign point for knocking out an airfield
                            Objective_Total_Blue += " " + apName;
                        }
                        else if (arm == 2)
                        {
                            InitialRedObjectiveCount++;
                            Objective_Total_Red += " " + apName;
                        }

                        Console.WriteLine("Airport destroyed, awarding points to destroying army; airport owned by army: " + arm.ToString());


                        //LoadAirfieldSpawns(); //loads airfield spawns and removes inactive airfields. (on TWC this is not working/not doing anything for now)
                        //This airport has been destroyed, so remove the spawn point
                        if (ap != null)
                        {
                            foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())
                            {
                                Point3d bp_pos = bp.Pos();
                                if (ap.Pos().distance(ref bp_pos) <= ap.FieldR()) bp.destroy();//Removes the spawnpoint associated with that airport (ie, if located within the field radius of the airport)
                            }
                        }

                    }
                }
                else
                {
                    AirfieldTargets.Remove(ap);
                    AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, Mission, PointsToKnockOut, PointsTaken, DateTime.Now, radius, APPos));
                }
                //GamePlay.gpLogServer(null, "bombe 11", null);
                break;  //sometimes airports are listed twice (for various reasons).  We award points only ONCE for each bomb & it goes to the airport FIRST ON THE LIST (dictionary) in which the bomb has landed.
            }
        }
    }

    //Calcs minutes left as an int
    public string calcTimeLeft()
    {

        Tick_Mission_Time = 720000 - Time.tickCounter();
        var Mission_Time = Tick_Mission_Time / 2000;
        TimeSpan Convert_Ticks = TimeSpan.FromMinutes(Mission_Time);
        string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);


        return Time_Remaining;
    }
    //Displays time left to player & also returns the time left message as a string
    //Calling with (null, false) will just return the message rather than displaying it
    public string showTimeLeft(Player player = null, bool showMessage = true)
    {
        string missiontimeleft = calcTimeLeft();
        string msg = "Time left in mission " + MISSION_ID + ": " + missiontimeleft ;

        /*
         *        
        if (!MISSION_STARTED) msg = "Mission " + MISSION_ID + " not yet started - waiting for first player to enter.";
        else if (COOP_START_MODE) msg = "Mission " + MISSION_ID + " not yet started - waiting for Co-op Start.";
        */

        if (showMessage && player != null) GamePlay.gpLogServer(new Player[] { player }, msg, new object[] { });
        return msg;
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

    public void logMessage(object data)
    {
        logToFile(data, MESSAGE_FULL_PATH);
    }

    public void logStats(object data)
    {
        logToFile(data, STATS_FULL_PATH);
    }

    public void DebugAndLog(object data)
    {
        if (DEBUG) GamePlay.gpLogServer(null, (string)data, new object[] { });
        if (!DEBUG && LOG) Console.WriteLine((string)data); //We're using the regular logs.txt as the logfile now logToFile (data, LOG_FULL_PATH); 
    }
    public void gpLogServerAndLog(Player[] to, object data, object[] third)
    {
        //this is already logged to logs.txt so no need for this: if (LOG) logToFile (data, LOG_FULL_PATH);
        GamePlay.gpLogServer(to, (string)data, third);

    }

    //Kill off AI aircraft after a pre-set length of time.  Stops AI aircraft from hanging around too long after they have
    //crashlanded, landed in the water, wandered off the map, etc
    //You can CHANGE the amount of time here depending on the type of AI aircraft you plan to use in your missions
    //Reason for this is #1. CloD tends to leave a/c hanging around for a long time after they really should be gone (crashed, landed, off map, whatever)
    //#2. AI aircraft tend to become useless after a certain amount of time anyway, they will just fly straight, be completely unresponsive, etc
    //#3. You should use a variety of strategies to eliminate AI aircraft when their useful life is done (check when they are crashed and de-spawn, fly them off the 
    //map at the end of their mission and then de-spawn them, etc) but this is sort of a last resort if all other methods fail

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);
        //AI Aircraft will be destroyed after airspawn minutes (set above)
        //lesson learned: For some reason if the Callsign of a group is high (higher than 50 or so?) then that object is not sent through this routine.  ??!!
        //eg, 12, 22, 32, 45 all work, but not 91 or 88.  They just never come
        //to OnActorCreated at all . . . But they are in fact created
        AiAircraft a = actor as AiAircraft;

        int destroyminutes = 90;//Destroy AI a/c this many minutes after they are spawned in.


        Timeout(1.0, () => // wait 1 second for human to load into plane
        {
            /* if (DEBUG) GamePlay.gpLogServer(null, "DEBUGC: Airgroup: " + a.AirGroup() + " " 
              + a.CallSign() + " " 
              + a.Type() + " " 
              + a.TypedName() + " " 
              +  a.AirGroup().ID(), new object[] { });
            */

            if (a != null && isAiControlledPlane2(a))
            {


                int ot = (destroyminutes) * 60 - 10; //de-spawns 10 seconds before new sub-mission spawns in.
                                                     //int brk=(int)Math.Round(19/20);


                /* if (DEBUG) GamePlay.gpLogServer(null, "DEBUGD: Airgroup: " + a.AirGroup() + " " 
                  + a.CallSign() + " " 
                  + a.Type() + " " 
                  + a.TypedName() + " " 
                  +  a.AirGroup().ID() + " timeout: " + ot, new object[] { });
                */

                Timeout(ot - 60, () =>  //message 60 seconds before de-spawning.
                {
                    if (actor != null && isAiControlledPlane2(actor as AiAircraft))
                    {
                        //GamePlay.gpHUDLogCenter("(Some) Old AI Aircraft de-spawning in 60 seconds");
                    }

                }
                              );

                Timeout(ot - 5, () =>
                {
                    if (actor != null && isAiControlledPlane2(actor as AiAircraft))
                    {
                        //GamePlay.gpHUDLogCenter("(Some) Old AI Aircraft de-spawning now!");  
                    }
                }
                              );

                //Timeout(75, () =>  //75 sec - 1.5 minutes for testing
                Timeout(ot, () =>  //960 sec - 16 minutes for real use
                {
                    DebugAndLog("DEBUG: Destroying: " + a.AirGroup() + " "
                      + a.CallSign() + " "
                      + a.Type() + " "
                      + a.TypedName() + " "
                      + a.AirGroup().ID() + " timeout: " + ot);
                    if (actor != null && isAiControlledPlane2(actor as AiAircraft))
                    { (actor as AiAircraft).Destroy(); }
                }
                );
            }
        });




    }



    #region Returns whether aircraft is an Ai plane (no humans in any seats)
    private bool isAiControlledPlane2(AiAircraft aircraft)

    { // returns true if specified aircraft is AI controlled with no humans aboard, otherwise false
        if (aircraft == null) return false;
        //check if a player is in any of the "places"
        for (int i = 0; i < aircraft.Places(); i++)
        {
            if (aircraft.Player(i) != null) return false;
        }
        return true;
    }
    #endregion




    //Removes AIAircraft if they are off the map. Convenient way to get rid of
    //old a/c - just send them off the map
    public void RemoveOffMapAIAircraft()
    {
        int numremoved = 0;
        //The map parameters - if an ai a/c goes outside of these, it will be de-spawned.  You need to just figure these out based on the map you are using.  Set up some airgroups in yoru mission file along the n, s, e & w boundaries of the map & note where the waypoints are.
        double minX = 8200;
        double minY = 14500;
        double maxX = 350000;
        double maxY = 307500;
        //////////////Comment this out as we don`t have Your Debug mode  
        DebugAndLog("Checking for AI Aircraft off map, to despawn");
        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in GamePlay.gpArmies())
            {
                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            //if (DEBUG) DebugAndLog ("DEBUG: Army, # in airgroup:" + army.ToString() + " " + airGroup.GetItems().Length.ToString());            
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor != null && actor is AiAircraft)
                                {
                                    AiAircraft a = actor as AiAircraft;
                                    /* if (DEBUG) DebugAndLog ("DEBUG: Checking for off map: " + Calcs.GetAircraftType (a) + " " 
                                       //+ a.CallSign() + " " //OK, not all a/c have a callsign etc, so . . . don't use this . . .  
                                       //+ a.Type() + " " 
                                       //+ a.TypedName() + " " 
                                       +  a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0")
                                      );
                                    */
                                    if (a != null && isAiControlledPlane2(a) &&
                                          (a.Pos().x <= minX ||
                                            a.Pos().x >= maxX ||
                                            a.Pos().y <= minY ||
                                            a.Pos().y >= maxY
                                          )

                                    )   // ai aircraft only
                                    {
                                        /* if (DEBUG) DebugAndLog ("DEBUG: Off Map/Destroying: " + Calcs.GetAircraftType (a) + " " 
                                        //+ a.CallSign() + " " 
                                        //+ a.Type() + " " 
                                        //+ a.TypedName() + " " 
                                        +  a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0")
                                       );  */
                                        numremoved++;
                                        Timeout(numremoved * 10, () => { a.Destroy(); }); //Destory the a/c, but space it out a bit so there is no giant stutter 

                                    }


                                }
                            }


                        }
                    }

            }
        }
        // if (DEBUG && numremoved >= 1) DebugAndLog (numremoved.ToString() + " AI Aircraft were off the map and de-spawned");
    } //method removeoffmapaiaircraft


    //Put a player into a certain place of a certain plane.
    private bool putPlayerIntoAircraftPosition(Player player, AiActor actor, int place)
    {
        if (player != null && actor != null && (actor as AiAircraft != null))
        {
            AiAircraft aircraft = actor as AiAircraft;
            player.PlaceEnter(aircraft, place);
            return true;
        }
        return false;
    }

    private void Stb_DestroyPlaneUnsafe(AiAircraft aircraft)
    {
        try
        {
            if (aircraft != null)
            {
                //Console.WriteLine("Destroying aircraft -stats.cs DPU");
                aircraft.Destroy();
            }
        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
    }

    private void Stb_RemovePlayerFromCart(AiCart cart, Player player = null) //removes a certain player from any aircraft, artillery, vehicle, ship, or whatever actor/cart the player is in.  Removes from ALL places.
                                                                             //if player = null then remove ALL players from ALL positions
    {
        try
        {

            if (cart == null)
                return;

            //check if the player is in any of the "places" - if so remove
            for (int i = 0; i < cart.Places(); i++)
            {
                if (cart.Player(i) == null) continue;
                if (player != null)
                {
                    if (cart.Player(i).Name() == player.Name()) player.PlaceLeave(i); //we tell if they are the same player by their username.  Not sure if there is a better way.
                }
                else
                {
                    cart.Player(i).PlaceLeave(i);
                }
            }

        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
    }
    //First removes the player from the aircraft (after 1 second), ALL POSITIONS, then removes any other players from the aircraft, then destroys the aircraft itself (IF it is AI controlled), after 3 more seconds
    private void Stb_RemoveAllPlayersFromAircraftandDestroy(AiAircraft aircraft, Player player, double timeToRemove_sec = 1.0, double timetoDestroy_sec = 3.0)
    {
        Timeout(timeToRemove_sec, () => {

            //player.PlaceLeave(0);
            Stb_RemovePlayerFromCart(aircraft as AiCart, player); //remove the primary player
            Stb_RemoveAllPlayersFromAircraft(aircraft, 0); //remove any other players
            Timeout(timetoDestroy_sec, () => {
                if (isAiControlledPlane(aircraft)) Stb_DestroyPlaneUnsafe(aircraft);  //destroy if AI controlled, which SHOULD be the case all of the time now
            }); //Destroy it a bit later
        });
    }

    //Removes ALL players from an a/c after a specified period of time (seconds)
    private void Stb_RemoveAllPlayersFromAircraft(AiAircraft aircraft, double timeToRemove_sec = 1.0)
    {
        Timeout(timeToRemove_sec, () => {

            //player.PlaceLeave(0);

            for (int place = 0; place < aircraft.Places(); place++)
            {
                if (aircraft.Player(place) != null)
                {
                    //Stb_RemovePlayerFromCart(aircraft as AiCart, aircraft.Player(place));
                    Stb_RemovePlayerFromCart(aircraft as AiCart); //BEC. we're removing ALL players from this a/c we don't care about matching by name.  This can cause problems if the player is ie in a bomber in two different places, so better just to remove ALL no matter what.
                }
            }

        });
    }
    //returns distance to nearest friendly airport to actor, in meters. Count all friendly airports, alive or not.
    //Includes airports AND spawnpoints
    private double Stb_distanceToNearestAirport(AiActor actor)
    {
        double d2 = 10000000000000000; //we compare distanceSQUARED so this must be the square of some super-large distance in meters && we'll return anything closer than this.  Also if we don't find anything we return the sqrt of this number, which we would like to be a large number to show there is nothing nearby.  If say d2 = 1000000 then sqrt (d2) = 1000 meters which probably not too helpful.
        double d2Min = d2;
        if (actor == null) return d2Min;
        Point3d pd = actor.Pos();
        int n = GamePlay.gpAirports().Length;
        //AiActor[] aMinSaves = new AiActor[n + 1];
        //int j = 0;
        //GamePlay.gpLogServer(null, "Checking distance to nearest airport", new object[] { });
        for (int i = 0; i < n; i++)
        {
            AiActor a = (AiActor)GamePlay.gpAirports()[i];
            if (a == null) continue;
            //if (actor.Army() != a.Army()) continue; //only count friendly airports
            //if (actor.Army() != (a.Pos().x, a.Pos().y)
            //OK, so the a.Army() thing doesn't seem to be working, so we are going to try just checking whether or not it is on the territory of the Army the actor belongs to.  For some reason, airports always (or almost always?) list the army = 0.

            //GamePlay.gpLogServer(null, "Checking airport " + a.Name() + " " + GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) + " " + a.Pos().x.ToString ("N0") + " " + a.Pos().y.ToString ("N0") , new object[] { });

            if (GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != actor.Army()) continue;


            //if (!a.IsAlive()) continue;


            Point3d pp;
            pp = a.Pos();
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                d2Min = d2;
                //GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString(), new object[] { });
            }

        }

        foreach (AiBirthPlace a in GamePlay.gpBirthPlaces())
        {
            if (a.Army() != actor.Army()) continue;


            //if (!a.IsAlive()) continue;


            Point3d pp;
            pp = a.Pos();
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                d2Min = d2;
                //GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString() + " distance " + d2.ToString("n0"), new object[] { });
            }

        }
        //GamePlay.gpLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
        return Math.Sqrt(d2Min);
    }

    //This is broken (broadcasts to everyone, not just the Player) but has one BIG advantage:
    //the messages can be seen on the lobby/map screen
    public void Stb_Chat(string line, Player player)
    {
        string to = " TO ";
        if (player != null && player.Name() != null) to += player.Name();
        if (GamePlay is GameDef)
        {
            (GamePlay as GameDef).gameInterface.CmdExec("chat " + line + to);
        }
    }

} //class mission : amission

//Various helpful calculations & formulas
public static class Calcs
{

    private static Random clc_random = new Random();

    public static double distance(double a, double b)
    {

        return (double)Math.Sqrt(a * a + b * b);

    }

    public static double meters2miles(double a)
    {

        return (a / 1609.344);

    }

    public static double meterspsec2milesphour(double a)
    {
        return (a * 2.23694);
    }

    public static double meters2feet(double a)
    {

        return (a / 1609.344 * 5280);

    }
    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
    public static double RadiansToDegrees(double radians)
    {
        return radians * (180.0 / Math.PI);
    }
    public static double CalculateGradientAngle(
                              Point3d startPoint,
                              Point3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = endPoint.x - startPoint.x;
        double diffY = endPoint.y - startPoint.y;

        //Calculates the Tan to get the radians (TAN(alpha) = opposite / adjacent)
        //Math.PI/2 - atan becase we need to change to bearing where North =0, East = 90 vs regular math coordinates where East=0 and North=90.
        double radAngle = Math.PI / 2 - Math.Atan2(diffY, diffX);

        //Converts the radians in degrees
        double degAngle = RadiansToDegrees(radAngle);

        if (degAngle < 0)
        {
            degAngle = degAngle + 360;
        }

        return degAngle;
    }

    public static double CalculatePointDistance(
                              Point3d startPoint,
                              Point3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);

        return distance(diffX, diffY);
    }
    public static double CalculatePointDistance(
                             Vector3d startPoint,
                             Vector3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);

        return distance(diffX, diffY);
    }
    public static double CalculatePointDistance(
                              Point3d startPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);

        return distance(diffX, diffY);
    }
    public static double CalculatePointDistance(
                             Vector3d startPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);

        return distance(diffX, diffY);
    }
    /**
      * Calculates the point of interception for one object starting at point
      * <code>a</code> with speed vector <code>v</code> and another object
      * starting at point <code>b</code> with a speed of <code>s</code>.
      * 
      * @see <a
      *      href="http://jaran.de/goodbits/2011/07/17/calculating-an-intercept-course-to-a-target-with-constant-direction-and-velocity-in-a-2-dimensional-plane/">Calculating
      *      an intercept course to a target with constant direction and velocity
      *      (in a 2-dimensional plane)</a>
      * 
      * @param a
      *            start vector of the object to be intercepted
      * @param v
      *            speed vector of the object to be intercepted
      * @param b
      *            start vector of the intercepting object
      * @param s
      *            speed of the intercepting object
      * @return Point3d where x,y is vvector of interception & z is time; or <code>null</code> if object cannot be
      *         intercepted or calculation fails
      * 
      * @author Jens Seiler
      * http://jaran.de/goodbits/2011/07/17/calculating-an-intercept-course-to-a-target-with-constant-direction-and-velocity-in-a-2-dimensional-plane/   
      */
    public static Point3d calculateInterceptionPoint(Point3d a, Point3d v, Point3d b, double s)
    {
        double ox = a.x - b.x;
        double oy = a.y - b.y;

        double h1 = v.x * v.x + v.y * v.y - s * s;
        double h2 = ox * v.x + oy * v.y;
        double t;
        if (h1 == 0)
        { // problem collapses into a simple linear equation 
            t = -(ox * ox + oy * oy) / (2 * h2);
        }
        else
        { // solve the quadratic equation
            double minusPHalf = -h2 / h1;

            double discriminant = minusPHalf * minusPHalf - (ox * ox + oy * oy) / h1; // term in brackets is h3
            if (discriminant < 0)
            { // no (real) solution then...
                return new Point3d(0, 0, 0); ; ;
            }

            double root = Math.Sqrt(discriminant);

            double t1 = minusPHalf + root;
            double t2 = minusPHalf - root;

            double tMin = Math.Min(t1, t2);
            double tMax = Math.Max(t1, t2);

            t = tMin > 0 ? tMin : tMax; // get the smaller of the two times, unless it's negative
            if (t < 0)
            { // we don't want a solution in the past
                return new Point3d(0, 0, 0); ;
            }
        }

        // calculate the point of interception using the found intercept time and return it
        return new Point3d(a.x + t * v.x, a.y + t * v.y, t);
    }


    //methods below from http://forum.1cpublishing.eu/showthread.php?t=32402&page=27  5./JG27.Farber 
    #region Calculations
    public static int Meters2Angels(double altitude)
    {
        double altAngels = (altitude / 0.3048) / 1000;

        if (altAngels > 1)
            altAngels = Math.Round(altAngels, MidpointRounding.AwayFromZero);
        else
            altAngels = 1;

        return (int)altAngels;
    }
    public static int Feet2Angels(double altitude)
    {
        double altAngels = (altitude) / 1000;

        if (altAngels > 1)
            altAngels = Math.Round(altAngels, MidpointRounding.AwayFromZero);
        else
            altAngels = 1;

        return (int)altAngels;
    }

    public static int ToMiles(double distance)
    {
        double distanceMiles = 0;
        distanceMiles = Math.Round(((distance / 1609.3426)), 0, MidpointRounding.AwayFromZero);   // distance in Miles

        return (int)distanceMiles;
    }

    public static Point3d Il2Point3dToLongLat(Point3d pos)
    {
        //This is an approximate calc. 
        //Courtesy mevans, https://theairtacticalassaultgroup.com/forum/showthread.php?t=26483
        /*pos.x = (int)pos.x - 146643;
        pos.x = pos.x / 63763.30751;
        pos.x = pos.x - 0.095287;
        */
        /* pos.y = (int)pos.y - 250937;
        pos.y = pos.y / 112533.651;
        pos.y = pos.y + 51.303649;
        */

        //Cubic Regression Curve Fit for four points on various corners of the map done 
        //via https://www.mycurvefit.com/ 
        //Should be accurate to about the 6th decimal place of the lat/long at those four points,
        //however is inaccurate to 10-20km in some points more in the middle of the map, not sure why.
        pos.x = -2.017013 + 0.00001256447 * pos.x + 5.532674e-12 * pos.x * pos.x - 6.478475e-18 * pos.x * pos.x * pos.x;
        pos.y = 49.01039 + 0.00000923699 * pos.y - 2.674155e-12 * pos.y * pos.y + 7.598837e-18 * pos.y * pos.y * pos.y;

        //mycurvefit.com came up with these possible curve fitting formulas based on those 4 points,
        //perhaps one of the others will work better:
        //#1:
        //lat =0.00000907759*y+49.0913
        //lon = 0.00001390456 * x - 2.097487
        //
        //#2:
        //lat = 49.01703 + 0.000008851254*y+7.476433*10^-13*y^2
        //lon = -2.041605 + 0.00001312839 * x + 1.959212e-12 * x ^ 2

        //#3: (implemented above)
        //lat = 49.01039 + 0.00000923699*y - 2.674155e-12*y^2 + 7.598837e-18*y^3
        //lon = -2.017013 + 0.00001256447 * x + 5.532674e-12 * x ^ 2 - 6.478475e-18 * x ^ 3
        return pos;
    }


    public static string DegreesToWindRose(double degrees)
    {
        String[] directions = { "North", "North East", "East", "South East", "South", "South West", "West", "North West", "North" };
        return directions[(int)Math.Round((((double)degrees % 360) / 45))];
    }

    // to get the correct bearing its nessesary to make a litte enter the matrix operation.
    // the Vector2d.direction() (same as atan2) has 0? at the x-axis and goes counter clockwise, but we need 0? at the y-axis
    // and clockwise direction
    // so to convert it we need 
    // |0 1| |x|   |0*x + 1*y|    |y|
    // |   | | | = |         | =  | |   // ok not very surprising ;)
    // |1 0| |y|   |1*x + 0*y|    |x|

    public static double CalculateBearingDegree(Vector3d vector)
    {
        Vector2d matVector = new Vector2d(vector.y, vector.x);
        // the value of direction is in rad so we need *180/Pi to get the value in degrees.  We subtract from pi/2 to convert to compass directions

        double bearing = (matVector.direction()) * 180.0 / Math.PI;
        return (bearing > 0.0 ? bearing : (360.0 + bearing));
    }


    public static double CalculateBearingDegree(Vector2d vector)
    {
        Vector2d newVector = new Vector2d(vector.y, vector.x);
        // the value of direction is in rad so we need *180/Pi to get the value in degrees.  We subtract from pi/2 to convert to compass directions
        double bearing = (newVector.direction()) * 180.0 / Math.PI;
        return (bearing > 0.0 ? bearing : (360.0 + bearing));
    }


    public static double CalculateBearingFromOrigin(Point2d targetLocation, Point2d originLocation)
    {

        double deltaX = targetLocation.x - originLocation.x;
        double deltaY = targetLocation.y - originLocation.y;

        double bearing = Math.Atan2(deltaX, deltaY);
        bearing = bearing * (180.0 / Math.PI);

        return (bearing > 0.0 ? bearing : (360.0 + bearing));
    }


    public static double CalculateBearingFromOrigin(Point3d targetLocation, Point3d originLocation)
    {

        double deltaX = targetLocation.x - originLocation.x;
        double deltaY = targetLocation.y - originLocation.y;


        double bearing = Math.Atan2(deltaX, deltaY);
        bearing = bearing * (180.0 / Math.PI);

        return (bearing > 0.0 ? bearing : (360.0 + bearing));
    }


    public static int GetDegreesIn10Step(double degrees)
    {
        degrees = Math.Round((degrees / 10), MidpointRounding.AwayFromZero) * 10;

        if ((int)degrees == 360)
            degrees = 0.0;

        return (int)degrees;
    }

    public static int RoundInterval(double number, int interval = 10)
    {
        number = Math.Round((number / interval), MidpointRounding.AwayFromZero) * interval;


        return (int)number;
    }

    public static int NoOfAircraft(int number)
    {
        int firstDecimal = 0;
        int higherDecimal = 0;

        higherDecimal = Math.DivRem(number, 10, out firstDecimal);

        if (firstDecimal > 3 && firstDecimal <= 8)
            firstDecimal = 5;
        else if (firstDecimal > 8)
            higherDecimal += 1;

        if (higherDecimal > 0)
            return (int)higherDecimal * 10;
        else
        {
            if (firstDecimal > 0 && firstDecimal <= 3) //If # is 1,2,3 then 50% of the time we get mixed up & get it wrong.  This is bec. radar can't always distinguish between 1,2,3 etc contacts.  "the height of the vertical displacement indicated formation size" - in other words it was a ROUGH estimate of the strength of the radar return, which they then turned into a guesstimate of how many a/c were in the formation.
            {
                firstDecimal = 2;
            }
            return (int)firstDecimal;
        }
    }

    #endregion

    //Salmo @ http://theairtacticalassaultgroup.com/forum/archive/index.php/t-4785.html
    public static string GetAircraftType(AiAircraft aircraft)
    { // returns the type of the specified aircraft
        string result = null;
        if (aircraft != null)
        {
            string type = aircraft.InternalTypeName(); // eg type = "bob:Aircraft.Bf-109E-3"
            string[] part = type.Trim().Split('.');
            result = part[1]; // get the part after the "." in the type string
        }
        return result;
    }

    /// <summary>
    /// Returns a MD5 hash as a string
    /// </summary>
    /// <param name="TextToHash">String to be hashed.</param>
    /// <returns>Hash as string.</returns>
    /// Matches with javascript MD5 output
    public static string GetMD5Hash(string input)
    {

        //Calculate MD5 hash. This requires that the string is splitted into a byte[].
        MD5 md5Hash = new MD5CryptoServiceProvider();
        // Convert the input string to a byte array and compute the hash.
        byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        StringBuilder sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }

    public static int gpNumberOfPlayers(this IGamePlay GamePlay)
    {   // Purpose: Returns the number of human players in the game.
        // Use: GamePlay.NumberOfPlayers(); 
        int result = 0;
        
        //multiplayer
        if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
        {
            return GamePlay.gpRemotePlayers().ToList().Count;
        }
        //singleplayer
        else if (GamePlay.gpPlayer() != null)
        {
            result = 1;
        }
        return result;
    }

    public static int gpNumberOfPlayers(this IGamePlay GamePlay, int army)
    {   // Purpose: Returns the number of human players in the game in the 
        //          specified army.
        // Use: GamePlay.NumberOfPlayers(army); 
        int result = 0;
        if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
        {
            List<Player> players = new List<Player>(GamePlay.gpRemotePlayers());
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Army() == army) result += 1;
            }
        }
        // on Dedi the server:
        else if (GamePlay.gpPlayer() != null)
        {
            if (GamePlay.gpPlayer().Army() == army) return 1;
            result = 0;
        }
        return result;
    }


}

public class ReverseComparer2 : IComparer<string>
{
    public int Compare(string x, string y)
    {
        // Compare y and x in reverse order.
        return y.CompareTo(x);
    }
}

public class ReverseComparer3<T> : IComparer<T> where T : IComparable<T>
{
    public int Compare(T x, T y)
    {
        return y.CompareTo(x);
    }
}

public sealed class ReverseComparer<T> : IComparer<T>
{
    private readonly IComparer<T> inner;
    public ReverseComparer() : this(null) { }
    public ReverseComparer(IComparer<T> inner)
    {
        this.inner = inner ?? Comparer<T>.Default;
    }
    int IComparer<T>.Compare(T x, T y) { return inner.Compare(y, x); }
}
