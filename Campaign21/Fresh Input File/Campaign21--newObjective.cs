﻿////$include "C:\Users\tegg\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\prog\Ext.cs"
////$include "C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Campaign21\Fresh Input File\Campaign21-stats.cs"
//$include "C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Campaign21\Fresh Input File\Campaign21-Class-CoverMission.cs"
//$include "C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Campaign21\Fresh Input File\Campaign21-Class-StatsMission.cs"
////$include "C:\Users\Administrator\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Campaign21\Fresh Input File\Campaign21-Class-CoverMission.cs"
////$include "C:\Users\Administrator\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Campaign21\Fresh Input File\Campaign21-Class-StatsMission.cs"


//TODO: Check what happens when map turned just before end of mission, or even after last 30 seconds.
#define DEBUG  
#define TRACE  
////$reference GCVBackEnd.dll
//$reference parts/core/CLOD_Extensions.dll
///$reference parts/core/TWCStats.dll
//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 
///$reference Microsoft.csharp.dll
//$reference WPF/PresentationFramework.dll
//$reference WPF/PresentationCore.dll
//$reference WPF/WindowsBase.dll
//$reference System.Xaml.dll
//$reference System.xml.dll
//$reference System.runtime.serialization.dll
///$reference System.Text.Json.dll
//The first two $references above + perhaps the [rts] scriptAppDomain=0 references on conf.ini & confs.ini are (perhaps!?) necessary for some of the code below to work, esp. intercepting chat messages etc.
// $reference System.Core.dll  is needed to make HashSet work.  For some reason.
///$reference parts/core/MySql.Data.dll  //THIS DOESN'T SEEM TO WORK
///$reference parts/core/System.Data.dll //THIS DOESN'T SEEM TO WORK

// v.1_19_07. script by oreva, zaltys, small_bee, bhugh, flug, fatal_error, several other contributors/online code snippets & examples

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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;



//using System.Web.Script.Serialization;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using TF_Extensions;
//using GCVBackEnd;
using System.Timers;                 /// <= Needed for Rearm/Refuel


//////////////////simply change the////////////////////////
//////////////////GamePlay.gpHUDLogCenter("Do 17's traveling towards Lympne");///////////////
/////////////////into/////////////////////////////
//////////////////sendScreenMessageTo(1, "Do 17's traveling towards Lympne", null);/////////////////////
///////////////////////so only the red pilots get the message./////////////////////////////////


namespace coord
{
    public class Communicate
    {
        int test = 14;
        AMission ms;
    }
    public sealed class Singleton
    {
        private static readonly Singleton instance = new Singleton();
        // Explicit static constructor to tell C# compiler  
        // not to mark type as beforefieldinit  
        static Singleton()
        {
        }
        private Singleton()
        {
        }
        public static Singleton Instance
        {
            get
            {
                return instance;
            }
        }
        public AMission Main { get; set; }
        public AMission Stats { get; set; }
    }
}

//so we have the problem that we have used type Player extensively to pass back & forth to various methods.  It's convenient and the Player type has lots of useful information.
//But . . . sometimes we only have the player's name & we still need to do things like record stats and such for that player.
//CloD doesn't let us create a new Player player for anyone who has not logged in to the game (in that case cloD makes the player automatically).
//So this little class built on the Player interface from maddox.game.Player allows us to do this trick:
// aPlayer p1 = new aPlayer("Fred Jones", 1);  //name Fred Jones, army=1
// Player p2 = p1 as Player;
//Then we can send p2 to any method that usually accepts Player
//And . . . mostly those methods only need the player's name & army, so it works.  Obviously if it needs more it will fail as most of the info returned below is fake/dummy.
//
//you can also initialize as 
//   aPlayer p1 = new aPlayer(player);  //if you already have a valid Player player from Maddox
//ALSO - aPlayer is serialiable & can be saved/restored.  So it can used used as part of arrays, dictionaries, etc that are saved/restored when session  stops/restarts.
//In that case, should use "new aPlayer(player)" as invocation for the aPlayer object, otherwise it will just be nil.

[DataContract]
public class aPlayer : Player
{
    [DataMember] public string name;
    [DataMember] public int army;
    private Player player;

    
    public string Name() { return name;  }
    public bool IsConnected() { return false; }    
    public int Channel() { return 0; }    
    public string ConnectAddress() { return ""; }    
    public int ConnectPort() { return 0; }    
    public int Army() { return army; }    
    public AiActor Place() { return null; }
    public int PlacePrimary() { return 0; }
    public int PlaceSecondary() { return 0; }
    public AiPerson PersonPrimary() { return null; }
    public AiPerson PersonSecondary() { return null; }
    public bool AutopilotPrimary() { return false; }
    public bool AutopilotSecondary() { return false; }
    public bool IsExpelArmy(int i) { return false; }
    public bool IsExpelUnit(AiActor a) { return false; }
    public void SelectArmy(int i) { return; }
    public void PlaceEnter(AiActor a, int i) { return; }
    public void PlaceLeave(int i) { return; }
    public int Ping() { return 0; }
    public string LanguageName() { return ""; }

    public aPlayer()
    {
        player = null;
        name = "";
        army = 0;

    }
    public aPlayer (aPlayer p )
    {
        player = p;
        name = player.Name();
        army = player.Army();
        
    }
    public aPlayer(Player p)
    {
        player = p;
        name = player.Name();
        army = player.Army();
        
    }

    public aPlayer(string n, int a)
    {
        name = n;
        army = a;        
    }

    /*
    public override bool Equals(Object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        else return this == (aPlayer)obj;
    }

    public override int GetHashCode()
    {
        return Tuple.Create(name, army).GetHashCode();
    }

    public static bool operator ==(aPlayer x, aPlayer y)
    {
        return x.name == y.name && x.army == y.army;
    }

    public static bool operator !=(aPlayer x, aPlayer y)
    {
        return !(x == y);
    }

    public override String ToString()
    {
        return String.Format("({0}, {1})", name, army);
    }
    */
}

public class Mission : AMission, IMainMission
//public class Mission : BaseMission, IMainMission
{
    Random random, stb_random;
    //Constants constants; 
    public int PERCENT_SUBMISSIONS_TO_LOAD = 60; //percentage of the aircraft sub-missions to load.  50 will load just half of the sub-missions etc.
    public string MISSION_ID { get; set; }
    public string CAMPAIGN_ID { get; set; }
    public string SERVER_ID { get; set; }
    public string SERVER_ID_SHORT { get; set; }
    public string CLOD_PATH { get; set; }
    public string FILE_PATH { get; set; }

    public bool DEBUG { get; set; }
    public bool LOG { get; set; } //Whether to log debug messages to  a log file.
                                  //public bool WARP_CHECK { get; set; }

    public ABattle gpBattle;
    public CoverMission covermission;
    public StatsMission statsmission;

    public string MISSION_FOLDER_PATH;
    public string USER_DOC_PATH;
    public string stb_FullPath;
    public Dictionary<int, string> radarpasswords;
    public string BOMBERMISSIONS_FILE_PATH;
    public string MESSAGE_FILE_NAME;
    public string MESSAGE_FULL_PATH;
    public string STATS_FILE_NAME;
    public string STATS_FULL_PATH { get; set; }
    public string LOG_FILE_NAME;
    public string LOG_FULL_PATH;
    public string STATSCS_FULL_PATH { get; set; }
    public int RADAR_REALISM;
    public string RESULTS_OUT_FILE; //Added by Fatal 11/09/2018.  This allows us to have win/lose logic for next mission

    public Dictionary<string, MissionObjective> MissionObjectivesList { get; set; }
    //public Dictionary<string, IMissionObjective> SMissionObjectivesList { get; set; }

    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    //public enum ArmiesE { None, Red, Blue };

    public bool MISSION_STARTED = false;
    public bool WAIT_FOR_PLAYERS_BEFORE_STARTING_MISSION_ENABLED = false;
    public int START_MISSION_TICK = -1;
    //So 20:15/8:15 pm is about the latest you can run a mission and still have any light.
    //5:00AM IS GOOD FOR START, 4:45AM IS GOOD ENOUGH & NICE LOOKING.  4:30AM IS REALLY NICe looking, esp once in air, but probably too dark to taxi reasonably.   The sun is just up at 4:30am.
    //public int END_MISSION_TICK = 720000; //6 HOURS
    //public int END_MISSION_TICK = 1440000; //12 Hours
    //public int END_MISSION_TICK = 1680000; //14 Hours
    //public int END_MISSION_TICK = 1980000; //16.5 Hours, starting at 4:45am and ending at 9:15pm.
    public int END_MISSION_TICK = 1890000; //15.75 Hours, starting at 4:30am and ending at 8:15pm.  Could possibly go even 15 mins earlier to 4:15?  And a little later?
    public double END_MISSION_TIME_HRS = 20.25; //So this means, the server will never run past 20.15 hours (8:15pm) server time, regardless of then it starts.  Reason is, it gets too dark after that.  so it will either go to ENDMISSION_TIME **OR** END_MISSION_TICK, whichever happens first.
    public double DESIRED_MISSION_START_TIME_HRS = 9.5; //The time we would like to/plan to start the mission.  0430 hours, 4:30am.  //NOT IMPLEMENTED YET, doesn't do anything.  Future TODO.

    public static readonly DateTime MODERN_CAMPAIGN_START_DATE = new DateTime(2020, 2, 7); //3 variables dealing with translating the current modern date to a relevant historical date: #1. Date on the current calendar that will count as day 0 of the campaign
    public static readonly DateTime HISTORIC_CAMPAIGN_START_DATE = new DateTime(1940, 7, 10); //#2. Date on the historic/1940s calendar that will register as day 0 of the campaign.
    public static int HISTORIC_CAMPAIGN_LENGTH_DAYS = 113; //#3. After this many days the historical dates will "roll over" and start again with HISTORIC_MISSION_START_DATE.

    public bool END_MISSION_IF_PLAYERS_INACTIVE = false;
    public bool COOP_START_MODE_ENABLED = false;
    public bool COOP_START_MODE = false;
    public double COOP_MODE_TIME_SEC = 45;
    public int START_COOP_TICK = -1;
    public double COOP_TIME_LEFT_MIN = 9999;
    public int ticksperminute = 1986;
    public double CampaignMapState = 0; //Determines which base map to load in & where the front is.  0 is the neutral map, - numbers tend more towards Blue, + numbers more towards Red
    public string CampaignMapSuffix = "-0"; //The initial initairports files will have suffix -0
    public string MapPrevWinner = ""; //Winner of the previous mission, if there was one
    public int CampaignMapMaxRedSuffixMax = 1; //This implies you have initairports files named with suffix ie -R001, -R002, -R003, -R004 through the max
    public int CampaignMapMaxBlueSuffixMax = 1; //This implies you have initairports files named ie -B001, -B002, -B003, -B004 through the max

    Stopwatch stopwatch;
    Dictionary<string, Tuple<long, SortedDictionary<string, string>>> radar_messages_store;
    public Dictionary<AiAirGroup, SortedDictionary<string, IAiAirGroupRadarInfo>> ai_radar_info_store { get; set; }

    //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
    //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
    //used in method admins_privilege_level below
    public string[] admins_basic = new String[] { "TWC_" };
    public string[] admins_full = new String[] { "TWC_Flug", "EvilUg", "TWC_Fatal_Error", "Server" };
    public int[][,] GiantSectorOverview = new int[3][,];  //holds a simple count of how many enemy airgroups (index 0) & aircraft (index 1) in each giant sector (giant keypad covering entire map)


    public long Tick_Mission_Time { get; set; }// Sets the Mission Clock for Time Remaining in Mission.
    int allowedSpitIIas = 4;
    int currentSpitIIas = 0;
    int allowed109s = 4;
    int current109s = 0;
    double redMultAdmin = 0;
    double blueMultAdmin = 0;


    //MissionObjectives mission_objectives;
    MissionObjectives mission_objectives = null;

    //private RearmRefuelManager ManageRnr = new RearmRefuelManager();

    //Constructor
    public Mission()
    {
        //DifficultySetting ds = GamePlay.gpDifficultyGet();
        //ds.No_Outside_Views = false;
        //ds.set(ds);
        //Console.WriteLine("Diff setting/outside views " + ds.No_Outside_Views.ToString());
        //GameWorld.DifficultySetting.No_Outside_Views = false;
        //outPath = "C:\\GoogleDrive\\GCVData";
    

        TWCComms.Communicator.Instance.Main = (IMainMission)this; //allows -stats.cs to access this instance of Mission

        covermission = new CoverMission();
        statsmission = new StatsMission();

        //Method defined in IMainMission interface in TWCCommunicator.dll are now access to other submissions
        //Also in other submission .cs like -stats.cs you can access the AMission methods of TWCMainMission by eg (TWCMainMission as AMission).OnBattleStopped();

        //TWCComms.Communicator.Instance.Ini = (AIniFile)Ini.IniFile; //allows -stats.cs etc to access this instance of IniFile
        //Console.Write("TYPEOF: " + typeof(string).Assembly.TWCStats);
        //TWCStats.interop statsMis = new TWCStats.interop();
        random = new Random();
        stb_random = random;
        //constants = new Constants();
        MISSION_ID = @"Campaign21";
        SERVER_ID = "Tactical Campaign Server"; //Used by General Situation Map app
        SERVER_ID_SHORT = "Tactical"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        SERVER_ID_SHORT = "TacticalTEST"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        CAMPAIGN_ID = "113 Days"; //Used to name the filename that saves state for this campaign that determines which map the campaign will use, ie -R001, -B003 etc.  So any missions that are part of the same overall campaign should use the same CAMPAIGN_ID while any missions that happen to run on the same server but are part of a different campaign should have a different CAMPAIGN_ID
        DEBUG = false;
        LOG = false;
        //WARP_CHECK = false;
        radarpasswords = new Dictionary<int, string>
        {
            { -1, "north"}, //Red army #1
            { -2, "gate"}, //Blue, army #2
            { -3, "twc2twc"}, //admin
            { -4, "twc2twc"}, //admingrouped
            //note that passwords are CASEINSENSITIVE
        };

        USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
        CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/";  // DO NOT CHANGE
        FILE_PATH = @"missions/Multi/Fatal/" + MISSION_ID + "/Fresh input file";   // mission install directory (CHANGE AS NEEDED); where we save things relevant to THIS SPECIFIC MISSION
        stb_FullPath = CLOD_PATH + FILE_PATH;
        MESSAGE_FILE_NAME = MISSION_ID + @"_message_log.txt";
        MESSAGE_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + MESSAGE_FILE_NAME;
        STATS_FILE_NAME = MISSION_ID + @"_stats_log.txt";
        STATS_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + STATS_FILE_NAME;
        LOG_FILE_NAME = MISSION_ID + @"_log_log.txt";
        LOG_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + LOG_FILE_NAME;
        STATSCS_FULL_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/missions/Multi/Fatal/";  // Where we save things RELEVANT TO THE ENTIRE CAMPAIGN AS A WHOLE 
                                                                                                                   //(Note that a campaign may have several missions, each in their own FILE_PATH folder; this is the main folder that holds stats files, team stats, player registration lists, etc that are all relevant to the Campaign as a whole
                                                                                                                   // Must match location -stats.cs is saving SessStats.txt to  
                                                                                                                   //Will be updated with value from -stats.ini OnMissionLoaded

        stopwatch = Stopwatch.StartNew();
        RADAR_REALISM = (int)5;
        RESULTS_OUT_FILE = CLOD_PATH + FILE_PATH + @"/" + "MissionResult.txt";
        radar_messages_store = new Dictionary<string, Tuple<long, SortedDictionary<string, string>>>();
        ai_radar_info_store = new Dictionary<AiAirGroup, SortedDictionary<string, IAiAirGroupRadarInfo>>();
        MissionObjectivesList = new Dictionary<string, MissionObjective>();
        //SMissionObjectivesList = MissionObjectivesList as IMissionObjecit;

        //initialize giant sector overview, which gives a quick total of airgroups (ind=0) and aircraft (ind=1) for the entire map.
        GiantSectorOverview[0] = new int[10, 2]; //army = 0 is ie admins
        GiantSectorOverview[1] = new int[10, 2];
        GiantSectorOverview[2] = new int[10, 2];

    }

    public Dictionary<string, IMissionObjective> SMissionObjectivesList()
    {
        var ret = new Dictionary<string, IMissionObjective>();
        foreach (string key in MissionObjectivesList.Keys)
        {
            ret[key] = MissionObjectivesList[key] as IMissionObjective;
        }
        return ret;
    }

    /********************************************************
     * 
     * Save campaign state every 10 minutes or so, so that if
     * something messes up before end of mission, we
     * don't lose all the campaign developments this mission
     * 
     *******************************************************/
    private bool SaveCampaignStateIntermediate_firstRun = true;
    public void SaveCampaignStateIntermediate()
    {

        Timeout(302.78, () => { SaveCampaignStateIntermediate(); }); //every 5 minutes or so, save
        if (!MISSION_STARTED) return;
        if (SaveCampaignStateIntermediate_firstRun)
        {
            //StartupSave doesn't try to calculate supply adjustments etc        
            Task.Run(() => SaveMapState("", intermediateSave: true));
            //Task.Run(() => MO_WriteMissionObjects()); //no need to run this so early, there is nothing to save yet anyway
            SaveCampaignStateIntermediate_firstRun = false;
        } else
        {
            Task.Run(() => SaveMapState("", intermediateSave: true));
            Task.Run(() => MO_WriteMissionObjects());
        }

        //SaveMapState("", true);
    }

    public void CheckCoop()
    {
        /************************************************
         * 
         * Check to see if COOP mode is still on & if so,
         * make sure that no aircraft or ie tanks have moved 
         * too far, or have taken off OR are going too fast
         * 
         * If so they will just be destroyed
         * 
         * Recursive function called every X seconds
         ************************************************/
        if (!COOP_START_MODE) return;

        Timeout(5, () => { CheckCoop(); });

        if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
        {
            foreach (Player p in GamePlay.gpRemotePlayers())
            {

                if (p.Place() != null)
                {
                    AiActor act = p.Place();

                    //remove players from aircraft/destroy it, if the aircraft has taken off
                    AiAircraft air = p.Place() as AiAircraft;
                    if (air != null && air.IsAirborne())
                    {
                        Stb_RemoveAllPlayersFromAircraftandDestroy(air, p, 0, 1.0);
                        twcLogServer(new Player[] { p }, "CO-OP START: You took off before Mission Start Time.", null);
                        twcLogServer(new Player[] { p }, "Your aircraft was destroyed.", null);
                    }

                    //If it is too far away from an airport, destroy (this takes care of tanks etc going rogue overland during the coop start period)
                    else if (Stb_distanceToNearestAirport(act) > 2500)
                    {
                        Stb_RemovePlayerFromCart(act as AiCart, p);
                        twcLogServer(new Player[] { p }, "CO-OP START: You left the airport or spawn point before Mission Start Time; " + Stb_distanceToNearestAirport(act).ToString("n0") + " meters to nearest airport or spawn point", null);
                        twcLogServer(new Player[] { p }, "You have been removed from your position.", null);
                    }
                }

            }
        }
    }

    bool EndMissionIfPlayersInactive_initialized = false;
    DateTime LastTimePlayerLoggedIn = DateTime.UtcNow;
    DateTime LastTimePlayerInPlace = DateTime.UtcNow;

    public void EndMissionIfPlayersInactive()
    {
        /************************************************
         * 
         *We check every minute or so to see if any players are logged on
         * and if so, if they are actually in a place.
         * 
         * If the mission is active &  no one has logged in for 7.5 minutes the mission will end.
         * 
         * If the mission is active &  no one been in a place (ie, in an aircraft, tank, etc0 for 15 minutes the mission will end
         * 
         * This is to prevent AI from marching forward & destroying all the mission targets, and thus moving the campaign maps around by huge amounts, when no one is even playing
         * 
         * Also it could potentially ward off some cheating type behaviors, if people realize that AI tends to score more points for one side or the other when no one is playing, then
         * they could  just start a mission & leave it, just to rack up points for their side.
         * 
         * Recursive function called every X seconds
         ************************************************/


        Timeout(63.25, () => { EndMissionIfPlayersInactive(); });

        if (!END_MISSION_IF_PLAYERS_INACTIVE) return;

        //Before the mission official starts, we still update the times as though players were in place - reason is, we could get in some weird situation where the mission
        //was paused because of one of these modes, put we somehow get a sample of the time, then wait 30 minutes, then someone jumps in to play & we restart, 
        //noticing there has a been a 30 minute delay & kill the game.  Which would not be good.  So, this is a bit belt & suspenders--really we dont' even start with this routine
        //until Mission is started & coop mode is over.
        if (!MISSION_STARTED || COOP_START_MODE)
        {
            LastTimePlayerLoggedIn = DateTime.UtcNow;
            LastTimePlayerInPlace = DateTime.UtcNow;
            //Console.WriteLine("Not miss started/coopstart");
            return;
        }
        if (!EndMissionIfPlayersInactive_initialized)
        {

            LastTimePlayerLoggedIn = DateTime.UtcNow;
            LastTimePlayerInPlace = DateTime.UtcNow;
            EndMissionIfPlayersInactive_initialized = true;
            //Console.WriteLine("EMIPI initialized");
            return;
        }


        if (GamePlay.gpPlayer() != null && GamePlay.gpPlayer().Place() != null)
        {
            LastTimePlayerLoggedIn = DateTime.UtcNow;
            LastTimePlayerInPlace = DateTime.UtcNow;
            //Console.WriteLine("EMIPI single player in place");
            return; //we only need one . .. 
        }

        //if (GamePlay.gpPlayer() != null || (GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0))
        if ((GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)) //giving up on looking for the single player as GamePlay.gpPlayer() always seems to be != null
        {
            LastTimePlayerLoggedIn = DateTime.UtcNow;
            //Console.WriteLine("EMIPI a player is logged in " + (GamePlay.gpPlayer() != null).ToString() + " " + (GamePlay.gpRemotePlayers() != null).ToString() + " " + GamePlay.gpRemotePlayers().Length.ToString());


            if (GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)
            {
                foreach (Player p in GamePlay.gpRemotePlayers())
                {

                    if (p.Place() != null)
                    {
                        LastTimePlayerInPlace = DateTime.UtcNow;
                        //Console.WriteLine("EMIPI multi player in place");
                        return; //we only need one . .. 
                    }

                }
            }
        }

        //Console.WriteLine("EMIPI checking time since last player");
        //End the mission if it has been 7.5 minutes since someone logged in OR 15 minutes since they were actually in a place.
        //if (LastTimePlayerLoggedIn.AddMinutes(.5) < DateTime.UtcNow || LastTimePlayerInPlace.AddMinutes(15) < DateTime.UtcNow)  //testing
        if (LastTimePlayerLoggedIn.AddMinutes(7.5) < DateTime.UtcNow || LastTimePlayerInPlace.AddMinutes(15) < DateTime.UtcNow)
        {
            EndMission(0);
        }
    }

    public override void OnTickGame()
    {
        base.OnTickGame();
        /* Tick_Mission_Time = 720000 - Time.tickCounter();
        var Mission_Time = Tick_Mission_Time / 2000;
        TimeSpan Convert_Ticks = TimeSpan.FromMinutes(Mission_Time);
        string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);
        */
        //int tickSinceStarted = Time.tickCounter();        

        if (!MISSION_STARTED)
        {
            //if (Time.tickCounter() % 10600 == 0) {
            if (Time.tickCounter() % (2 * ticksperminute) == 0)
            {
                //DebugAndLog ("Debug: tickcounter: " + Time.tickCounter().ToString() + " tickoffset" + tickoffset.ToString());

                int timewaitingminutes = Convert.ToInt32(((double)Time.tickCounter() / (double)ticksperminute));
                DebugAndLog("Waiting for first player to join; waiting " + timewaitingminutes.ToString() + " minutes");
                if (timewaitingminutes > 60) EndMission(1); //If we wait too long before starting the mission we get darkness, other problems before it ends.  So just end it after waiting a while.

            }

            return;
        }

        if (START_COOP_TICK == -1) START_COOP_TICK = Time.tickCounter();

        if (COOP_START_MODE)
        {

            int tickSinceCoopStarted = Time.tickCounter() - START_COOP_TICK;

            if (tickSinceCoopStarted >= Convert.ToInt32((COOP_MODE_TIME_SEC * (double)ticksperminute) / 60.0))
            {
                COOP_START_MODE = false;

                Stb_Chat("CO-OP MISSION START NOW!", null);
                Stb_Chat("CO-OP START: Pilots, you may take off at will", null);

                GamePlay.gpHUDLogCenter("CO-OP MISSION START NOW!");
                Timeout(5, () => { GamePlay.gpHUDLogCenter("CO-OP MISSION START NOW!"); });
                Timeout(10, () => { GamePlay.gpHUDLogCenter("CO-OP MISSION START NOW!"); });

                return;
            }


            if (tickSinceCoopStarted % (ticksperminute / 4) == 0)
            {
                //DebugAndLog ("Debug: tickcounter: " + Time.tickCounter().ToString() + " tickoffset" + tickoffset.ToString());
                COOP_TIME_LEFT_MIN = (COOP_MODE_TIME_SEC / 60 - ((double)tickSinceCoopStarted / (double)ticksperminute));
                double timeleftseconds = (COOP_MODE_TIME_SEC - ((double)tickSinceCoopStarted) * 60.0 / (double)ticksperminute);
                string s = COOP_TIME_LEFT_MIN.ToString("n2") + " MINUTES";
                if (timeleftseconds < 120) s = timeleftseconds.ToString("n0") + " SECONDS";

                //let players who can control <coop know about the command, 1X per minute
                if (tickSinceCoopStarted % ticksperminute == 0)
                {
                    Timeout(7.5, () =>
                    {
                        foreach (Player p in GamePlay.gpRemotePlayers())
                        {
                            if (admin_privilege_level(p) >= 1) //about once a minute, a message to players who can issue coop commands
                            {
                                twcLogServer(new Player[] { p }, "CO-OP MODE CONTROL: Use chat command <coop to start immediately OR extend time", null);
                            }
                        }
                    });
                }

                //gpLogServerAndLog(null, "COOP START: You can spawn in and taxi but DO NOT TAKE OFF for " + s, null);

                Stb_Chat("CO-OP MISSION START IN " + s, null);
                Stb_Chat("CO-OP START: You can spawn on the ground and taxi near your spawn point but", null);
                Stb_Chat("DO NOT TAKE OFF OR AIR SPAWN until CO-OP mission start time", null);
                //Stb_Chat("CO-OP time: " + ( COOP_MODE_TIME_SEC / 60).ToString("n2"), null);


                string s2 = COOP_TIME_LEFT_MIN.ToString("n2") + " more minutes";
                if (timeleftseconds < 120) s = timeleftseconds.ToString("n0") + " more seconds";


                GamePlay.gpHUDLogCenter("CO-OP START: DO NOT TAKE OFF for " + s2);
                Timeout(5, () => { GamePlay.gpHUDLogCenter("CO-OP START: DO NOT TAKE OFF for " + s2); });
                Timeout(10, () => { GamePlay.gpHUDLogCenter("CO-OP START: DO NOT TAKE OFF for " + s2); });



            }


            return;
        }



        if (START_MISSION_TICK == -1) START_MISSION_TICK = Time.tickCounter();

        int tickSinceStarted = Time.tickCounter() - START_MISSION_TICK;

        //int respawntick = respawnminutes * ticksperminute; // How often to re-spawn new sub-missions & do other repetitive tasks/messages. 27000=15 min repeat. 1800 'ticks' per minute or  108000 per hour.  I believe that this is approximate, not exact.


        //periodically remove a/c that have gone off the map
        if ((tickSinceStarted) % 2100 == 0 && tickSinceStarted > 0)
        {

            RemoveOffMapAIAircraft();


        }


        if ((tickSinceStarted) % 10100 == 0)
        {
            //Write all a/c position to log
            if (LOG)
            {
                DebugAndLog(calcTimeLeft() + " left in mission " + MISSION_ID);
                //int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
                //RADAR_REALISM = 0;

                Task.Run(() => listPositionAllAircraft(GamePlay.gpPlayer(), 1, true, radar_realism: 0));
                Task.Run(() => listPositionAllAircraft(GamePlay.gpPlayer(), 1, false, radar_realism: 0));
                //listPositionAllAircraft(GamePlay.gpPlayer(), 1, true, radar_realism: 0);
                //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false, radar_realism: 0);
                //RADAR_REALISM = saveRealism;
            }

        }

        if ((tickSinceStarted) == 0)
        {
            twcLogServer(null, "Mission loaded.", new object[] { });

            //WriteResults_Out_File("3"); //1=red, 2= blue, 3=tie; we pre-set to tie in case the mission exits early etc.
            Task.Run(() => WriteResults_Out_File("3"));
            Timeout(188, () => { Task.Run(() => CheckStatsData()); }); //  Start the routine to transfer over stats, a/c killed, etc; Delay a while so sessStats.txt etc are already in place            
            Timeout(10, () => { groupAllAircraft_recurs(); });
            Timeout(15, () => { aiAirGroupRadarReturns_recurs(); });

        }

        if (tickSinceStarted % 30000 == 1000)
        //if (tickSinceStarted % 1100 == 1000)  //for testing
        {

            twcLogServer(null, "Completed Red Objectives (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):", new object[] { });
            twcLogServer(null, MissionObjectivesCompletedString[ArmiesE.Red], new object[] { });
            Timeout(10, () =>
            twcLogServer(null, "Completed Blue Objectives (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):", new object[] { }));
            Timeout(11, () =>
            twcLogServer(null, MissionObjectivesCompletedString[ArmiesE.Blue], new object[] { }));
            Timeout(12, () =>
            twcLogServer(null, showTimeLeft(), new object[] { }));

            //Console.WriteLine("Leaks: " + MO_IntelligenceLeakNearMissionEnd[ArmiesE.Blue] + " " + MO_IntelligenceLeakNearMissionEnd[ArmiesE.Red]);

            Timeout(stb_random.Next(56, 623), () =>
            //Timeout(stb_random.Next(5, 6), () => //for testing
            {
                if (MO_IntelligenceLeakNearMissionEnd[ArmiesE.Blue] != "") sendChatMessageTo((int)ArmiesE.Blue, MO_IntelligenceLeakNearMissionEnd[ArmiesE.Blue], null);
                if (MO_IntelligenceLeakNearMissionEnd[ArmiesE.Red] != "") sendChatMessageTo((int)ArmiesE.Red, MO_IntelligenceLeakNearMissionEnd[ArmiesE.Red], null);

            });

            //stopAI();//for testing
        }

        //So, this could be done every minute or whatever, instead of every tick . . . _recurs
        if (tickSinceStarted == END_MISSION_TICK || GamePlay.gpTimeofDay() > END_MISSION_TIME_HRS)// Red battle Success.
                                                 //if (Time.tickCounter() == 720)// Red battle Success.  //For testing/very short mission
        {

            //WriteResults_Out_File("3");
            Task.Run(() => WriteResults_Out_File("3"));
            Timeout(10, () =>
            {
                twcLogServer(null, "The match ends in a tie!  Objectives still left for both sides!!!", new object[] { });
                GamePlay.gpHUDLogCenter("The match ends in a tie! Objectives still left for both sides!!!");
            });
            EndMission(70, "");
        }

        //Ticks below write out TOPHAT radar files for red, blue, & admin
        //We do each every ~minute but space them out a bit from each other
        //roughly every one minute
        //do this regardless of whether players are loaded, so it must be first here
        if ((Time.tickCounter()) % 1000 == 0)
        {
            ///////////////////////////////////////////    
            //int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            //Console.WriteLine("Writing current radar returns to file");

            //Timeout(188, () => { var t = Task.Run(() => CheckStatsData()); });
            //RADAR_REALISM = -1;
            //listPositionAllAircraft(GamePlay.gpPlayer(), -1, false, radar_realism: -1); //-1 & false will list ALL aircraft of either army        
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);

            Task.Run(() => listPositionAllAircraft(GamePlay.gpPlayer(), -1, false, radar_realism: -1));
            //listPositionAllAircraft(GamePlay.gpPlayer(), -1, false, radar_realism: -1);
            //RADAR_REALISM = saveRealism;

        }
        if ((Time.tickCounter()) % 1000 == 334)
        {
            ///////////////////////////////////////////    
            //int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            //Console.WriteLine("Writing current radar returns to file");
            //RADAR_REALISM = -1;
            //listPositionAllAircraft(GamePlay.gpPlayer(), -2, false, radar_realism: -1); //-1 & false will list ALL aircraft of either army
            Task.Run(() => listPositionAllAircraft(GamePlay.gpPlayer(), -2, false, radar_realism: -1));
            //listPositionAllAircraft(GamePlay.gpPlayer(), -2, false, radar_realism: -1);
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            //RADAR_REALISM = saveRealism;

        }
        if ((Time.tickCounter()) % 1000 == 666)
        {
            ///////////////////////////////////////////    
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
                                             //Console.WriteLine("Writing current radar returns to file");
                                             //RADAR_REALISM = -1;
                                             //listPositionAllAircraft(GamePlay.gpPlayer(), -3, false, radar_realism: -1); //-1 & false will list ALL aircraft of either army
            Task.Run(() => listPositionAllAircraft(GamePlay.gpPlayer(), -3, false, radar_realism: -1));
            //listPositionAllAircraft(GamePlay.gpPlayer(), -3, false, radar_realism: -1);
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            //RADAR_REALISM = saveRealism;

        }
        if ((Time.tickCounter()) % 1000 == 813)
        {
            ///////////////////////////////////////////    
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists BUT GROUPED
                                             //Console.WriteLine("Writing current radar returns to file");
                                             //RADAR_REALISM = -1;
                                             //listPositionAllAircraft(GamePlay.gpPlayer(), -3, false, radar_realism: -1); //-1 & false will list ALL aircraft of either army
            Task.Run(() => listPositionAllAircraft(GamePlay.gpPlayer(), -4, false, radar_realism: -1));
            //listPositionAllAircraft(GamePlay.gpPlayer(), -4, false, radar_realism: -1);
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            //RADAR_REALISM = saveRealism;

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
    //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, if it s knocked out, double airfield radius, Point3d airfield center (position)
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
            int pointstoknockout = 30;  //This is about two HE111 or JU88 loads (or 1 full load & just a little more) and about 4 Blennie loads, but it depends on how accurate the bombs are, and how large

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


            if (add) AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, apName, pointstoknockout, 0, DateTime.UtcNow, radius, center)); //Adds airfield to dictionary, requires approx 2 loads of 32 X 50lb bombs of bombs to knock out.
                                                                                                                                                                                       //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius
                                                                                                                                                                                       //if you want to add only some airfields as targets, use something like: if (ap.Name().Contains("Manston")) { }

        }
        twcLogServer(null, "SetAirfieldTargets initialized.", null);
    }

    public string ListAirfieldTargetDamage(Player player = null, int army = -1, bool all = false, bool display = true)
    {
        int count = 0;
        string returnmsg = "";
        double delay = 0.1;
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
                //DateTime disabledUntil_DT = AirfieldTargets[ap].Item6;

                double percent = 0;
                if (PointsToKnockOut > 0)
                {
                    percent = PointsTaken / PointsToKnockOut;
                }

                double timereduction = 0;
                if (percent > 0)
                {
                    timereduction = (DateTime.UtcNow.Subtract(lastBombHit)).TotalSeconds;
                }

                double timetofix = PointsTaken * 20 * 60 - timereduction; //50 lb bomb scores 0.5 so will take 10 minutes to repair.  Larger bombs will take longer; 250 lb about 1.4 points so 28 minutes to repeari
                                                                          //But . . . it is ADDITIVE. So the first 50 lb bomb takes 10 minutes, the 2nd another 10, the 3rd another 10, and so on on.  So if you drop 32 50 bl bombs it will take 320 minutes before the 32nd bomb crater is repaired.
                                                                          //Sources: "A crater from a 500lb bomb could be repaired and resurfaced in about 40 minutes" says one 2nd hand source. That seems about right, depending on methods & surface. https://www.airspacemag.com/multimedia/these-portable-runways-helped-win-war-pacific-180951234/
                                                                          //unfortunately we can repair only the bomb crater; the SMOKE will remain for the entire mission because clod internals don't allow its removal.
                                                                          //TODO: We could keep track of when the last bomb was dropped at each airport and deduct time here depending on how much repair had been done since the last bomb dropped

                string msg2 = "";
                if (PointsTaken >= PointsToKnockOut) //airport knocked out
                {
                    percent = 1;
                    timetofix = 24 * 60 * 60; //24 hours to repair . . . if they achieve 100% knockout.  That is a little bonus beyond what the actual formula says, due ot total knockout
                    timetofix += (PointsTaken - PointsToKnockOut) * 20 * 60; //Plus they achieve any additional knockout/repair time due to additional bombing beyond 100%, because those will have to be repaired, too.
                    //msg2 = "; estimated " + (Math.Round(timetofix/3600.0*2.0)/2.0).ToString("F1") + "hrs to re-open";
                    msg2 = " (" + (Math.Ceiling(timetofix / 3600.0 / 24 * 2.0) / 2.0).ToString("F1") + " days)";

                }

                //AirfieldTargets[ap].Item6 = DateTime.UtcNow.AddSeconds(timetofix); //just save that since we recalced it here; keep it consistent with what we are displaying.
                //forget this, instead we'll add it to the missionobjectiveslist



                string msg = Mission + " " + (percent * 100).ToString("n0") + "% destroyed; last hit " + (timereduction / 60).ToString("n0") + " minutes ago" + msg2;
                returnmsg += msg + "\n";

                if (display)
                {
                    delay += 0.02;
                    Timeout(delay, () => { twcLogServer(new Player[] { player }, msg, new object[] { }); });
                }


            }
        if (count == 0)
        {
            string msg = "No airports damaged or destroyed yet";
            if (display) twcLogServer(new Player[] { player }, msg, new object[] { });
            returnmsg = ""; //In case of display == false we just don't return any message at all, allowing this bit to simply be omitted
        }

        return returnmsg;
    }

    //stamps a rectangular pattern of craters over an airfield to disable it
    public void AirfieldDisable(AiAirport ap, double percent = 1.0)

    {
        string apName = ap.Name();
        double radius = ap.FieldR();
        Point3d pos = ap.Pos();

        //Console.WriteLine("Disabling airport {0} {1:F0} {2:F0}", new Object[] {ap.Name(), ap.Pos().x, ap.Pos().y });

        if (AirfieldTargets.ContainsKey(ap))
        {
            apName = AirfieldTargets[ap].Item2;
            radius = AirfieldTargets[ap].Item6;
            pos = AirfieldTargets[ap].Item7;

        }

        //disable any associated birthplace - thus, no spawning here
        foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())
        {
            Point3d bp_pos = bp.Pos();
            if (ap.Pos().distance(ref bp_pos) <= ap.FieldR()) bp.destroy();//Removes the spawnpoint associated with that airport (ie, if located within the field radius of the airport)
        }

        foreach (GroundStationary gg in GamePlay.gpGroundStationarys(pos.x, pos.y, radius)) //all stationaries w/i 10 or whatever meters of this object
        {
            if (random.Next(3) < 1)
            {
                //Console.WriteLine("Airfield dest: Removing airfield item " + gg.Name);
                gg.Destroy();
            }
        }


        //GamePlay.gpHUDLogCenter(null, "Airfield " + apName + " has been disabled");

        //ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";

        string val1 = "Stationary";
        string type = "BombCrater_firmSoil_largekg";
        int count = 0;
        string value = "";

        int rounds = Convert.ToInt32(Math.Ceiling(percent * 5));

        int stripesCount = 0;

        double craterSpacing_m = 41 + random.NextDouble() * 10;

        int delay = 1;
        for (int round = 0; round < rounds; round++)
        {

            double xpos = pos.x - 400 + 800 * stb_random.NextDouble();
            double ypos = pos.y - 400 + 800 * stb_random.NextDouble();
            double angle = random.NextDouble() * 2.0 * Math.PI;
            Point3d vec = new Point3d(Math.Cos(angle) * craterSpacing_m, Math.Sin(angle) * craterSpacing_m, 0);
            //Point3d vec90deg = new Point3d(-vec.y, vec.x, vec.z);

            int stripes = random.Next(4) + 1;
            for (int stripe = 0; stripe < stripes; stripe++)
            {
                if (stripesCount > rounds) break; //we don't really need more than this many stripes altogether
                ISectionFile f = GamePlay.gpCreateSectionFile();

                Point3d startPos = new Point3d(xpos + 150 - 300 * stb_random.NextDouble(), ypos + 150 - 300 * stb_random.NextDouble(), vec.z);
                int craters = random.Next(10) + 12;

                count = 0;

                for (int crater = 0; crater < craters; crater++)
                {
                    double randAdd = random.NextDouble() * 1.8 - 0.9;

                    //Console.WriteLine("Disabling airport {0:F0} {1:F0} {2:F0} {3:F0} {4:F0} {5:F0} {6:F0}", new Object[] { startPos.x.ToString("0.00"), startPos.y.ToString("0.00"), vec.x.ToString("0.00"), vec.y.ToString("0.00"), crater, randAdd, craterSpacing_m/10 });
                    Point3d craterPos = new Point3d(startPos.x + vec.x * (crater + randAdd), startPos.y + vec.y * (crater + randAdd) + random.NextDouble() * craterSpacing_m / 10, vec.z);
                    string key = "Static" + count.ToString();
                    value = val1 + ".Environment." + type + " nn " + craterPos.x.ToString("0.00") + " " + craterPos.y.ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + craterPos.z.ToString("0.00");
                    f.add(sect, key, value);

                    //Console.WriteLine("Disabling airport {0:F0} {1:F0} {2} {3} {4}", new Object []  { craterPos.x.ToString("0.00"), craterPos.y.ToString("0.00"), round, stripe, crater });

                    if (random.Next(7) < 1)
                        Timeout(delay + 1, () =>
                          {
                              Calcs.loadSmokeOrFire(GamePlay, this, craterPos.x, craterPos.y, craterPos.z, "BuildingFireSmall", random.Next(5 * 3600) + 3600);
                          });
                    count++;
                }

                Timeout(delay, () => { GamePlay.gpPostMissionLoad(f); });
                delay += 5;

                stripesCount++;

            }
        }

        /*
         *             for (double x = pos.x - radius * 1.1; x < pos.x + radius * 1.1; x = x + 280)
            {
                for (double y = pos.y - radius * 1.1; y < pos.y + radius * 1.1; y = y + 310)
                {
                    string key = "Static" + count.ToString();
                    value = val1 + ".Environment." + type + " nn " + (x - 400 + 800 * stb_random.NextDouble()).ToString("0.00") + " " + (y - 400 + 800 * stb_random.NextDouble()).ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + pos.z.ToString("0.00");
                    f.add(sect, key, value);

                    if (random.Next(5) < 1) Calcs.loadSmokeOrFire(GamePlay, this, pos.x, pos.y, pos.z, "BuildingFireSmall", random.Next(5 * 3600) + 3600);
                    count++;

                }

            }
            */
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX7"); //Testing for potential causes of warping
                                                                                  //f.save(CLOD_PATH + FILE_PATH + "airfielddisableMAIN-ISectionFile.txt"); //testing

        //Timeout(stb_random.NextDouble() * 5, () => { GamePlay.gpPostMissionLoad(f); });

        //Calcs.loadSmokeOrFire(GamePlay, this, pos.x, pos.y, 0, "BuildingFireBig", duration_s: 6 * 3600);

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
     * OnStationaryKilled - handling routines for when a stationary/static object is killed
     * 
     *****************************************************************************/

    public override void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    {
        #region stb
        base.OnStationaryKilled(missionNumber, stationary, initiator, eventArgInt);
        try
        {
            Task.Run(() => MO_HandlePointAreaObjectives(stationary, initiator));
            //stb_KilledActors.Add(actor, damages); // save 
            //System.Console.WriteLine("Actor dead: Army " + actor.Army() );
            /* string msg = "Stationary " + stationary.Name + " " + stationary.country + " " + stationary.pos.x.ToString("F0") + " " + stationary.pos.y.ToString("F0") + " " + stationary.Title + " " + stationary.Type.ToString() + " " + "killed by ";

            Player player = null;
            if (initiator != null && initiator.Player != null) player = initiator.Player;
            */
        }
        catch (Exception ex) { Console.WriteLine("OnStationaryKilled -main: " + ex.ToString()); }
        #endregion
    }




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

        //Task.Run(() => OnBombExplosion_DoWork(title, mass_kg, pos, initiator, eventArgInt)); //OK, don't do this as when many bombs explode it also explodes the CPU with way too many threads at once.

        //Spread them out a little over time
        //TODO: this could all be done in a worker thread (just not 1000 worker threads as we attempted above)
        double wait = stb_random.NextDouble() * 10;
        Timeout(wait, () =>
            OnBombExplosion_DoWork(title, mass_kg, pos, initiator, eventArgInt)
        );
    }

    public void OnBombExplosion_DoWork(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {

        try
        {
            //twcLogServer(null, "bombe 1", null);
            //twcLogServer(null, string.Format("bombe {0:N0} {1:N0} {2:N0}", pos.x, pos.y, pos.z), null);
            bool ai = true;
            if (initiator != null && initiator.Player != null && initiator.Player.Name() != null) ai = false;

            //twcLogServer(null, "bombe 2", null);
            int isEnemy = 1; //0 friendly, 1 = enemy, 2 = neutral
            int terr = GamePlay.gpFrontArmy(pos.x, pos.y);

            //twcLogServer(null, "bombe 3", null);
            if (terr == 00) isEnemy = 2;
            if (!ai && initiator.Player.Army() == terr) isEnemy = 0;
            //twcLogServer(null, "bombe 4", null);

            MO_HandlePointAreaObjectives(title, mass_kg, pos, initiator);

            //TF_GamePlay.gpIsLandTypeCity(maddox.game.IGamePlay, pos);       

            /********************
             * 
             * Handle airport bombing
             * 
             *******************/

            //twcLogServer(null, "bombe 5", null);

            var apkeys = new List<AiAirport>(AirfieldTargets.Keys.Count);
            apkeys = AirfieldTargets.Keys.ToList();

            maddox.game.LandTypes landType = GamePlay.gpLandType(pos.x, pos.y);

            //For now, all things we handle below are on land, so if the land type is water we just
            //get out of here immediately
            if (landType == maddox.game.LandTypes.WATER) return;

            //twcLogServer(null, "bombe 6", null);

            bool blenheim = false;
            AiAircraft aircraft = initiator.Actor as AiAircraft;
            string acType = Calcs.GetAircraftType(aircraft);
            if (acType.Contains("Blenheim")) blenheim = true;

            double aircraftCorrection = 1;

            if (acType.Contains("Blenheim")) aircraftCorrection = 4;
            if (acType.Contains("He-111")) aircraftCorrection = 1.5;
            if (acType.Contains("BR-20")) aircraftCorrection = 2;

            foreach (AiAirport ap in apkeys)//Loop through the targets; we do it on a separate copy of the keys list bec. we are changing AirfieldTargets mid-loop, below
            {
                /* if (!AirfieldTargets[ap].Item1)
                {//airfield has already been knocked out so do nothing
                }
                else
                { */

                //twcLogServer(null, "bombe 7", null);
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

                    scoreBase *= aircraftCorrection;

                    //if (blenheim) scoreBase *= 8; //double score for Blenheims since their bomb load is pathetic (double the OLD score which is 4X the NEW score.  This makes 1 Blenheim (4 bombs X 4) about 50% as effective as on HE 11. (32 bombs)             

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
                                                             //twcLogServer(null, initiator.Player.Name() + " has bombed a friendly or neutral airport. Serious repercussions for player AND team.", new object[] { });
                    }



                    //TF_Extensions.TF_GamePlay.Effect smoke = TF_Extensions.TF_GamePlay.Effect.SmokeSmall;
                    // TF_Extensions.TF_GamePlay.gpCreateEffect(GamePlay, smoke, pos.x, pos.y, pos.z, 1200);
                    string firetype = "BuildingFireSmall";
                    if (mass_kg > 200) firetype = "BuildingFireBig"; //500lb bomb or larger
                    if (stb_random.NextDouble() > 0.25) firetype = "";
                    //todo: finer grained bigger/smaller fire depending on bomb tonnage

                    //twcLogServer(null, "bombe 8", null);

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

                    //twcLogServer(null, "bombe 8", null);

                    individualscore = individualscore / points_reduction_factor;  //reduce the score if needed 

                    //if (!ai) stb_RecordStatsOnActorDead(initiator, 4, individualscore, 1, initiator.Tool.Type);  //So they have dropped a bomb on a target so they get some point score


                    double timereduction = 0;
                    if (prev_percent > 0)
                    {
                        timereduction = (DateTime.UtcNow.Subtract(lastBombHit)).TotalSeconds;
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
                        timetofix += (PointsTaken - PointsToKnockOut) * 20 * 60; //Plus they achieve any additional knockout/repair time due to additional bombing beyond 100%, because those will have to be repaired, too.
                    }


                    //Advise player of hit/percent/points 
                    //(Note: USED to do this in .stats but with updated mission scoring, now we must do it here.)

                    if (!ai) twcLogServer(new Player[] { initiator.Player }, "Airport hit: " + (percent * 100).ToString("n0") + "% destroyed " + mass_kg.ToString("n0") + "kg " + individualscore.ToString("n1") + " pts " + (timetofix / 3600).ToString("n1") + " hr to repair ", new object[] { }); //+ (timereduction / 3600).ToString("n1") + " hr spent on repairs since last bomb drop"

                    //loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, timetofix, stb_FullPath, cratertype);

                    //Sometimes, advise all players of percent destroyed, but only when crossing 25, 50, 75, 100% points
                    Timeout(3, () => { if (percent * 100 % 25 < prev_percent * 100 % 25) twcLogServer(null, Mission + " " + (percent * 100).ToString("n0") + "% destroyed ", new object[] { }); });

                    //twcLogServer(null, "bombe 8", null);

                    if (PointsTaken >= PointsToKnockOut) //has points limit to knock out the airport been reached?
                    {
                        AirfieldTargets.Remove(ap);
                        AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(true, Mission, PointsToKnockOut, PointsTaken, DateTime.UtcNow, radius, APPos));
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
                            try
                            {
                                MO_DestroyObjective(Mission + "_airfield", percentdestroyed: percent, timetofix_s: timetofix); //don't need to include last hit time because MO_Dest.. always includes the current UTC time
                            }
                            catch (Exception ex) { Console.WriteLine("OnExplosion_DoWork error1: " + ex.ToString()); };
                            /*
                             * The additional score & adding points for knocking out an airport have been moved to the MissionObjectives section with MO_DestroyObjective
                            //Question: Do we want to keep these objective points for knocking out any airfield?
                            if (arm == 1)
                            {
                                MissionObjectiveScore[ArmiesE.Blue]++; //1 campaign point for knocking out an airfield
                                MissionObjectivesCompletedString[ArmiesE.Blue] += " - " + apName;
                            }
                            else if (arm == 2)
                            {
                                MissionObjectiveScore[ArmiesE.Red]++;
                                MissionObjectivesCompletedString[ArmiesE.Red] += " - " + apName;
                            }

                            Console.WriteLine("Airport destroyed, awarding points to destroying army; airport owned by army: " + arm.ToString());
                            */

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
                        MO_DestroyObjective_addTime(Mission + "_airfield", percentdestroyed: percent, timetofix_s: timetofix, TimeLastHit_UTC: DateTime.UtcNow);
                    }
                    else
                    {
                        AirfieldTargets.Remove(ap);
                        AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, Mission, PointsToKnockOut, PointsTaken, DateTime.UtcNow, radius, APPos));
                    }
                    //twcLogServer(null, "bombe 11", null);
                    break;  //sometimes airports are listed twice (for various reasons).  We award points only ONCE for each bomb & it goes to the airport FIRST ON THE LIST (dictionary) in which the bomb has landed.
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("On Bomb Explosion do_work: " + ex.ToString()); }
    }

    bool final_SaveMapState_completed = false;
    bool final_MO_WriteMissionObjects_completed = false;
    int currentEndMission = 0;
    //END MISSION WITH WARNING MESSAGES ETC/////////////////////////////////
    //string winner should be "Red" or "Blue" exactly and only!!!
    public void EndMission(int endseconds = 0, string winner = "")
    {
        currentEndMission++;
        int thisEndMission = currentEndMission; //Allows possibility to cancel/abort EndMission by incrementing currentEndMission, or, more importantly, if EndMission is called 2X (specifically if one side turns the map after EndMission has already started but before the mission has actually closed down) the 2nd time will supersede.

        if (winner == "")
        {
            twcLogServer(null, "Mission is restarting soon!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission is restarting soon!!!");
        }
        else
        {
            if (endseconds > 60)
            {
                Timeout(endseconds + 40, () =>
                {
                    if (currentEndMission == thisEndMission)
                    {
                        twcLogServer(null, winner + " has turned the map!", new object[] { });
                        GamePlay.gpHUDLogCenter(winner + " has turned the map. Congratulations, " + winner + "!");
                    }
                });
            }
            Timeout(endseconds / 2, () =>
            {
                if (currentEndMission == thisEndMission)
                {
                    twcLogServer(null, winner + " has turned the map!", new object[] { });
                    GamePlay.gpHUDLogCenter(winner + " has turned the map. Congratulations, " + winner + "!");
                }
            });
            Timeout(endseconds + 15, () =>
            {
                if (currentEndMission == thisEndMission)
                {
                    twcLogServer(null, winner + " has turned the map!", new object[] { });
                    GamePlay.gpHUDLogCenter(winner + " has turned the map - mission ending soon!");
                }
            });
            Timeout(endseconds + 45, () =>
            {
                if (currentEndMission == thisEndMission)
                {
                    twcLogServer(null, winner + " has turned the map!", new object[] { });
                    GamePlay.gpHUDLogCenter(winner + " has turned the map - mission ending soon!");
                }
            });
            Timeout(endseconds + 61, () =>
            {
                if (currentEndMission == thisEndMission)
                {
                    twcLogServer(null, "Congratulations " + winner + " for turning the map!", new object[] { });
                }

            });
        }
        Timeout(endseconds, () =>
        {
            twcLogServer(null, "Mission is restarting in 1 minute!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission is restarting in 1 minute!!!");
        });
        Timeout(endseconds + 30, () =>
        {
            if (currentEndMission == thisEndMission)
            {
                twcLogServer(null, "Server Restarting in 30 seconds!!!", new object[] { });
                GamePlay.gpHUDLogCenter("Server Restarting in 30 seconds!!!");


                //All players who are lucky enough to still be in a plane at this point have saved their plane/it's returned to their team's supply


                //Save map state & data
                double misResult = SaveMapState(winner); //here is where we save progress/winners towards moving the map & front one way or the other; also saves the Supply State

                CheckStatsData(winner); //Save campaign/map state just before final exit.  This is important because when we do (GamePlay as GameDef).gameInterface.CmdExec("exit"); to exit, the -stats.cs will read the CampaignSummary.txt file we write here as the final status for the mission in the team stats.
                MO_WriteMissionObjects(wait: true);
                final_SaveMapState_completed = true;
                final_MO_WriteMissionObjects_completed = true;
            }

        });
        Timeout(endseconds + 50, () =>
        {
            if (currentEndMission == thisEndMission)
            {
                twcLogServer(null, "Server Restarting in 10 seconds!!!", new object[] { });
            }


        });
        Timeout(endseconds + 60, () =>
        {
            if (currentEndMission == thisEndMission)
            {
                twcLogServer(null, "Mission ended. Please wait 2 minutes to reconnect!!!", new object[] { });
                GamePlay.gpHUDLogCenter("Mission ended. Please wait 2 minutes to reconnect!!!");
                DebugAndLog("Mission ended.");

                //OK, trying this for smoother exit (save stats etc)
                //(TWCStatsMission as AMission).OnBattleStoped();//This really horchs things up, basically things won't run after this.  So save until v-e-r-y last.
                //OK, we don't need to do the OnBattleStoped because it is called when you do CmdExec("exit") below.  And, if you run it 2X it actually causes problems the 2nd time.
                if (GamePlay is GameDef)
                {
                    (GamePlay as GameDef).gameInterface.CmdExec("exit");
                }
                //GamePlay.gpBattleStop(); //It would be nice to do this but if you do, the script stops here.
            }
        });
        Timeout(endseconds + 90, () =>  //still doing this as a failsafe but allowing 20 secs to save etc
        {
            if (currentEndMission == thisEndMission)
            {
                //If the CmdExec("exit") didn't work for some reason, we can call OnBattleStoped manually to clean things up, then kill.  This is just a failsafe
                //(TWCStatsMission as AMission).OnBattleStoped();//This really horchs things up, basically things won't run after this.  So save until v-e-r-y last.
                statsmission.OnBattleStoped();//This really horchs things up, basically things won't run after this.  So save until v-e-r-y last.
                Process.GetCurrentProcess().Kill();
            }
        });

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

    public bool LoadRandomSubmission(string fileID = "randsubmission", string subdir = "")
    {
        //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
        //GamePlay.gpHUDLogCenter("Respawning AI air groups");
        //twcLogServer(null, "RESPAWNING AI AIR GROUPS. AI Aircraft groups re-spawn every " + RESPAWN_MINUTES + " minutes and have a lifetime of " + RESPAWN_MINUTES + "-" + 2*RESPAWN_MINUTES + " minutes. The map restarts every " + Convert.ToInt32((float)END_SESSION_TICK/60/TICKS_PER_MINUTE) + " hours.", new object[] { });

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
            twcLogServer(Players.ToArray(), msg, parms);
    }

    /************************************************************
     * ONPLACELEAVE
     * *********************************************************/

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceLeave(player, actor, placeIndex);

        /// REARM/REFUEL: cancel possibly pending request of player
        //  ManageRnr.cancelOfPlayer(GamePlay, player);

        if (actor != null && actor is AiAircraft)
        {
            //OK, we have to wait a bit here bec. some ppl use ALT-F11 (ALT-F2) for 'external view' which allows to leave two positions
            //inhabited by bomber pilot & just return to the one position.  But how it actually works is the pilot leaves the aircraft momentarily.
            Timeout(0.5f, () =>
            {
                //Save the reconnaissance photos, if the player is on friendly territory, stopped, & alive
                //If crashlanded etc then the photos will be erased, so this applies only to a pretty good landing on friendly territory, maybe at/near an airport
                if (player != null)
                {
                    if ((actor as AiAircraft) != null &&
                        GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y) == actor.Army() &&
                        //Stb_distanceToNearestAirport(actor) < 3100 &&
                        Calcs.CalculatePointDistance((actor as AiAircraft).AirGroup().Vwld()) < 2 &&
                        actor.IsAlive()
                        )
                        MO_RecordPlayerScoutPhotos(player);

                }
                string pName = "";
                if (player != null) pName = player.Name();
                if (actor is AiAircraft)
                {

                    if (isAiControlledPlane2(actor as AiAircraft))
                    {
                        //Changing from .5 to 1.5 so that we can allow 1.4 secs in -stats.cs on <rr, for AI warmup of aircraft for 1 second longer
                        Timeout(1.5f, () => //5 sec seems too long, the ai vigorously takes control sometimes, and immediately.  Perhaps even 1 second or .5 better than 2.
                        {
                            if (isAiControlledPlane2(actor as AiAircraft))
                            {
                                damageAiControlledPlane(actor);
                                Console.WriteLine("Player has left plane; damaged aircraft so that AI cannot assume control " + pName + " " + (actor as AiAircraft).Type());
                                //check limited aircraft

                                /* this is now handled by -supply.cs system
                                    switch ((actor as AiAircraft).InternalTypeName())
                                {

                                    case "bob:Aircraft.SpitfireMkIIa":
                                        currentSpitIIas--;
                                        break;
                                    case "bob:Aircraft.Bf-109E-4N":
                                        current109s--;
                                        break;

                                }
                                */

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

    /************************************************************
    * ONPLACEENTER
    * *********************************************************/

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);

        if (player != null)
        {
            setMainMenu(player);
            //if (placeIndex == 0) placeIndex++;

            //string PlayerPlace = Enum.GetName(typeof(CrewFunction), (actor as AiCart).CrewFunctionPlace(placeIndex));
            //tells the name of the position their in - Pilot, Bombardier, Nose Gunner, etc.

            //GamePlay.gpHUDLogCenter(PlayerPlace + " " + placeIndex.ToString("F0"));

            /*
             * TESTING STUFF FOR aPlayer Player extension
            aPlayer p = new aPlayer();

            Player p1 = p as Player;

            Console.WriteLine("Player bugablug" + p1.Name());
            Console.WriteLine("Player bugablug" + p1.Name() + p1.Army().ToString());

            gpLogServerAndLog(null, "Player bugablug" + p1.Name(), null);

            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddSessStat(p1, 798, 5321);
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_IncrementSessStat(player, 848);  //848 recon photos taken, 849 # of objectives photographed
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddToMissionStat(player, 848, 1);
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_IncrementSessStat(player, 849);  //848 recon photos taken, 849 # of objectives photographed
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddToMissionStat(player, 849, 1);

            MO_AddPlayerStatsScoresForObjectiveDestruction(p1, MissionObjectivesList["RTarget28R"], 10);
            */

        }

        /* handling this via -supply.cs now
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
        */
    }


    /*************************************************************
     * CHECKSTATSDATA
     * ***********************************************************/


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
    double RedScoutPhotosI = 0;
    double RedScoutedObjectivesI = 0;
    double BlueAirF = 0;
    double BlueAAF = 0;
    double BlueNavalF = 0;
    double BlueGroundF = 0;
    double BluePlanesWrittenOffI = 0;
    double BlueScoutPhotosI = 0;
    double BlueScoutedObjectivesI = 0;

    public void CheckStatsData(string winner = "")
    {
        /************************************************
         * 
         * Check/download/transfer stats data
         * Recursive function called every X seconds
         ************************************************/
        //Timeout(188, () => { CheckStatsData(); });

        Timeout(188, () => { Task.Run(() => CheckStatsData()); });

        // Read the stats file where we tally red & blue victories for the session
        //This allows us to make red/blue victories part of our mission objectives &
        //use the victory tallying mechanism in -stats.cs to do the work of keeping track of that
        try
        {
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX1"); //Testing for potential causes of warping
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

                if (Time.AddSeconds(125).ToUniversalTime() > DateTime.UtcNow)
                {
                    RedTotalF = Convert.ToDouble(RedTotalS) / 100;
                    BlueTotalF = Convert.ToDouble(BlueTotalS) / 100;
                    //twcLogServer(null, "Read SessStats.txt: Times MATCH", null);
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

                //twcLogServer(null, string.Format("RED session total: {0:0.0} BLUE session total: {1:0.0} Time1: {2:R} Time2 {3:R}",
                //      (double)(RedTotalF) / 100, (double)(BlueTotalF) / 100, Time.ToUniversalTime(), DateTime.Now.ToUniversalTime()), null);
                //twcLogServer(null, string.Format("RED session total: {0:0.0} BLUE session total: {1:0.0} ",
                //      (double)(RedTotalF) / 100, (double)(BlueTotalF) / 100), null);

            }
        }
        catch (Exception ex) { System.Console.WriteLine("Main mission - read sessstats.txt - Exception: " + ex.ToString()); }

        //Get the current campaign state
        Tuple<double, string> res = CalcMapMove(winner, false, false, null);
        //sw.Write(res.Item2); //Item2 is a detail breakout of current campaign score.  Could be included in final stats for the mission, perhaps
        double newMapState = CampaignMapState + res.Item1;
        string campaign_summary = summarizeCurrentMapstate(newMapState, false, null);

        //Write the campaign summary text with current score etc.; this will go on the TEAM STATS page of the stats page
        try
        {
            string outputmsg = "";
            if (RedPlanesWrittenOffI >= 2)
            {
                string msg = "Red has lost " + RedPlanesWrittenOffI.ToString() + " aircraft in battle.";
                outputmsg += msg + "<br>" + Environment.NewLine;
            }
            if (BluePlanesWrittenOffI >= 2)
            {
                string msg = "Blue has lost " + BluePlanesWrittenOffI.ToString() + " aircraft in battle.";
                outputmsg += msg + "<br>" + Environment.NewLine;
            }
            if (BluePlanesWrittenOffI >= 2 || RedPlanesWrittenOffI >= 2) outputmsg += "<br>" + Environment.NewLine;

            if (RedScoutPhotosI >= 1 || RedScoutedObjectivesI >= 0)
            {
                string msg = "Red has taken " + RedScoutPhotosI.ToString() + " reconnaissance photos of " + RedScoutedObjectivesI.ToString() + " mission objectives.";
                outputmsg += msg + "<br>" + Environment.NewLine;
            }
            if (BlueScoutPhotosI >= 1 || BlueScoutedObjectivesI >= 0)
            {
                string msg = "Blue has taken " + BlueScoutPhotosI.ToString() + " reconnaissance photos of " + BlueScoutedObjectivesI.ToString() + " mission objectives.";
                outputmsg += msg + "<br>" + Environment.NewLine;
            }
            if (BlueScoutPhotosI >= 1 || BlueScoutedObjectivesI >= 0 || RedScoutPhotosI >= 1 || RedScoutedObjectivesI >= 0) outputmsg += "<br>" + Environment.NewLine;

            outputmsg = "Blue Objectives complete (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Blue]) + "<br>" + Environment.NewLine;
            outputmsg += "Red Objectives complete (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Red]) + "<br>" + Environment.NewLine;

            outputmsg += "<br>" + Environment.NewLine + "<br>" + Environment.NewLine;
            outputmsg += campaign_summary; //note: this include .NewLine but not <br>
            if (winner != "") outputmsg += "<br>" + winner.ToUpper() + " HAS TURNED THE MAP! Congratulations, " + winner + "<br>" + Environment.NewLine;

            //File.WriteAllText(STATSCS_FULL_PATH + "CampaignSummary.txt", outputmsg);
            Calcs.WriteAllTextAsync(STATSCS_FULL_PATH + "CampaignSummary.txt", outputmsg);
        }
        catch (Exception ex) { Console.WriteLine("CampaignSummary Write: " + ex.ToString()); }


        //Skipping all the points & map-turning related to stats for now.  Just transferring the data for possible use.
        /*
        //Check whether the 50-kill objective is reached.  
        if (!osk_Red50Kills && RedTotalF >= 50)
        {
            osk_RedObjCompleted += "50 total Team Kills - ";
            osk_Red50Kills = true;
            twcLogServer(null, "RED reached 50 Team Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("RED reached 50 Team Kills. Well done Red!");

        }
        if (!osk_Blue50Kills && BlueTotalF >= 50)
        {
            osk_BlueObjCompleted += "50 total Team Kills - ";
            osk_Blue50Kills = true;
            twcLogServer(null, "BLUE reached 50 Team Kills. Well done Team Blue!", new object[] { });
            GamePlay.gpHUDLogCenter("BLUE reached 50 Team Kills. Well done Blue!");
        }

        //Check whether the 50-kill objective is reached.  
        if (!osk_Red10AirKills && RedAirF >= 10)
        {
            osk_RedObjCompleted += "10 total Air Kills - ";
            osk_Red10AirKills = true;
            twcLogServer(null, "Red reached 10 total Air Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("Red reached 10  total Air Kills. Well done Red!");
        }
        if (!osk_Blue10AirKills && BlueAirF >= 10)
        {
            osk_BlueObjCompleted += "10 total Air Kills - ";
            osk_Blue10AirKills = true;
            twcLogServer(null, "BLUE reached 10 total Air Kills. Well done Team Blue!", new object[] { });
            GamePlay.gpHUDLogCenter("BLUE reached 10  total Air Kills. Well done Blue!");
        }
        if (!osk_Red10GroundKills && (RedAAF + RedNavalF + RedGroundF) >= 10)
        {
            osk_RedObjCompleted += "10 total AA/Naval/Ground Kills - ";
            osk_Red10GroundKills = true;
            twcLogServer(null, "Red reached 10 total AA/Naval/Ground Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("Red reached 10  total AA/Naval/Ground Kills. Well done Red!");
        }
        if (!osk_Blue10GroundKills && (BlueAAF + BlueNavalF + BlueGroundF) >= 10)
        {
            osk_BlueObjCompleted += "10 total AA/Naval/Ground Kills - ";
            osk_Blue10GroundKills = true;
            twcLogServer(null, "BLUE reached 10 total AA/Naval/Ground Kills. Well done Team Blue!", new object[] { });
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
        string StringToReturn = "";
        StringToReturn = "Red Objectives complete (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):\n";
        StringToReturn = StringToReturn + MissionObjectivesCompletedString[ArmiesE.Red];
        return StringToReturn;
    }

    public string GetBlueObjectivesString()
    {
        string StringToReturn = "";
        StringToReturn = "Blue Objectives complete (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):\n";
        StringToReturn = StringToReturn + MissionObjectivesCompletedString[ArmiesE.Blue];
        return StringToReturn;
    }

    public string GetTimeLeftString()
    {
        string StringToReturn = "";
        StringToReturn = "Time Remaining In Mission:\n";

        TimeSpan Convert_Ticks = TimeSpan.FromMinutes((END_MISSION_TICK - Time.tickCounter()) / 2000);//720000 denotes 6 hours of play
        string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);

        StringToReturn = StringToReturn + Time_Remaining;
        return StringToReturn;
    }

    /*************************************************************
    * END - CHECKSTATSDATA
    * ***********************************************************/

    /******************************************************************************
     * 
     * LONG-TERM CAMPAIGN METHODS 
     * 
     * Routines dealing with the LONG TERM CAMPAIGN and calculating the points
     * for each team that determine the current campaign status
     * and which map will be used next mission
     *      
     ******************************************************************************/

    //CalcMapMove - returns a double with DOUBLE the current mission score and STRING the text message detailing the score
    //This figures the score at intermediate points during the session, but also the final score at end of session or when
    //one team or the other turns the map
    public Tuple<double, string> CalcMapMove(string winner, bool final = true, bool output = true, Player player = null)
    {
        double MapMove = 0;
        string msg = "";
        string outputmsg = "";
        Player[] recipients = null;
        if (player != null) recipients = new Player[] { player };

        //Update scoring, 2020/02/15 - now we are ADDING 100 points (/100 = 1) to that side's current Map Mover score, and also adding the (miniscule) amount
        //of points they get from air victories etc.  Before it was just a flat 100 points for turning the map, but now it's whatever objective points you've accumulated PLUS 100 points more.
        if (winner == "Red")
        {
            msg = "Red moved the campaign forward by achieving all Primary Objectives and turning the map!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            return new Tuple<double, string>(1 + MissionObjectiveScore[ArmiesE.Red] / 100.0 + RedTotalF / 2000.0, outputmsg); //1 is the 100 point bonus, plus we're adding in the objective points at this time, plus we're adding in the individual victory bonus 2X
        }
        if (winner == "Blue")
        {
            msg = "Blue moved the campaign forward by achieving all Primary Objectives and turning the map!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            return new Tuple<double, string>(-1 - MissionObjectiveScore[ArmiesE.Blue] / 100.0 - BlueTotalF / 2000.0, outputmsg); //-1 is the 100 point BLUE bonus, plus we're adding (or rather SUBTRACTING since this is the blue side) in the objective points at this time, plus we're adding in the individual victory bonus 2X
        }

        if (RedTotalF > 3)
        {
            msg = "Red has moved the campaign forward through its " + RedTotalF.ToString("n1") + " total air/ground/naval victories!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += RedTotalF / 2000.0;
        }
        if (BlueTotalF > 3)
        {
            msg = "Blue has moved the campaign forward through its " + BlueTotalF.ToString("n1") + " total air/ground/naval victories!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= BlueTotalF / 2000.0;
        }

        /*
        double difference = RedTotalF - BlueTotalF;
        if (Math.Abs(difference) >= 5)
        {
            if (difference > 0)
            {
                msg = "Red has moved the campaign forward by getting " + difference.ToString("n1") + " more total victories than Blue!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (difference < 0)
            {
                msg = "Blue has moved the campaign forward by getting " + (-difference).ToString("n1") + " more total victories than Red!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += difference / 400;
        }

        double air_difference = RedAirF - BlueAirF;

        if (Math.Abs(air_difference) >= 5)
        {
            if (air_difference > 0)
            {
                msg = "Red has moved the campaign forward by getting " + air_difference.ToString("n1") + " more air victories than Blue!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (air_difference < 0)
            {
                msg = "Blue has moved the campaign forward by getting " + (-air_difference).ToString("n1") + " more air victories than Red!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += air_difference / 400;
        }
        double ground_difference = RedAAF + RedNavalF + RedGroundF - BlueAAF - BlueNavalF - BlueGroundF;
        if (Math.Abs(ground_difference) >= 5)
        {
            if (ground_difference > 0)
            {
                msg = "Red has moved the campaign forward by getting " + ground_difference.ToString("n1") + " more ground victories than Blue!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (ground_difference < 0)
            {
                msg = "Blue has moved the campaign forward by getting " + (-ground_difference).ToString("n1") + " more ground victories than Red!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += ground_difference / 400;
        }
        */

        if (MissionObjectiveScore[ArmiesE.Red] > 0)
        {
            msg = "Over the past few days, Red has moved the campaign forward " + MissionObjectiveScore[ArmiesE.Red].ToString("n0") + " points by destroying Mission Objectives!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            //MapMove += MissionObjectiveScore[ArmiesE.Red] / 100; //2020-02 - with the persistent campaign, we don't add this at the end of each session any more.  It is saved from session to session and only added in when the map is turned by this team (their full objective score + 100 points)
        }

        if (MissionObjectiveScore[ArmiesE.Blue] > 0)
        {
            msg = "Over the past few days, Blue has moved the campaign forward " + MissionObjectiveScore[ArmiesE.Blue].ToString("n0") + " points by destroying Mission Objectives!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            //MapMove -= MissionObjectiveScore[ArmiesE.Blue] / 100; //2020-02 - with the persistent campaign, we don't add this at the end of each session any more.  It is saved from session to session and only added in when the map is turned by this team (their full objective score + 100 points)
        }
        if (RedPlanesWrittenOffI >= 3 && winner != "Red") //subtract for planes written off, but only if they didn't turn the map this sssion
        {
            msg = "Red has lost ground by losing " + RedPlanesWrittenOffI.ToString() + " aircraft in battle!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= (double)RedPlanesWrittenOffI / 200;  //These are LOSSES, so - points for red & + points for blue
        }
        if (BluePlanesWrittenOffI >= 3 && winner != "Blue") //subtract for planes written off, but only if they didn't turn the map this sssion
        {
            msg = "Blue has lost ground by losing " + BluePlanesWrittenOffI.ToString() + " aircraft in battle!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += (double)BluePlanesWrittenOffI / 200; //These are LOSSES, so - points for red & + points for blue
        }

        /*
        if (final)
        {

            double portionComplete = calcProportionTimeComplete(); //0= just start, 1 = complete
            double outside = (random.NextDouble() - 0.5) * portionComplete;  //if a full mission we can get up to +/- 0.5 added by 'outside factors'.  But if we have done only a half mission it would be half that, 1/4 mission = 1/4 that, etc.


            if (outside > 0.05)
            {
                string reason = "Help from Allies";
                if (random.Next(2) == 1) reason = "A naval victory";
                msg = reason + " has strengthened Red's position";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (outside < -0.05)
            {
                string reason = "Help from Allies has";
                if (random.Next(3) == 1) reason = "A naval victory has";
                else if (random.Next(1) == 1) reason = "Positive developments on the Eastern Front have";
                msg = reason + " strengthened Blue's position";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += outside;
        }

        */

        //We can move AT MOST one notch (one map) in either direction, per mission
        //UPDATE 2020/02 - with persistent missions we are removing this restriction.  The objective points + bonus a team gets is
        //typically accumulated over many days/sessions.
        //if (MapMove > 1) MapMove = 1;
        //if (MapMove < -1) MapMove = -1;

        string word = "Currently, ";
        if (final) word = "Altogether, ";

        if (MapMove > 0)
        {
            msg = word + "this sessions has improved Red's campaign position by " + (MapMove * 100).ToString("n0") + " points.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        if (MapMove < 0)
        {
            msg = word + "this session has improved Blue's campaign position by " + (-MapMove * 100).ToString("n0") + " points.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        if (MapMove == 0)
        {
            msg = word + "this session is a COMPLETE STALEMATE!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }

        return new Tuple<double, string>(MapMove, outputmsg);

    }
    public string summarizeCurrentMapstate(double ms, bool output = true, Player player = null)
    {
        string outputmsg = "";
        string msg = "";
        Player[] recipients = null;
        if (player != null) recipients = new Player[] { player };

        if (ms > 0)
        {
            msg = "Red stands at +" + (ms * 100).ToString("n0") + " for the entire " + CAMPAIGN_ID + " campaign.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        else if (ms < 0)
        {
            msg = "Blue stands at +" + (-ms * 100).ToString("n0") + " for the entire " + CAMPAIGN_ID + " campaign.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        else
        {
            msg = "The entire " + CAMPAIGN_ID + " campaign is exactly balanced. Blue stands at +0, Red +0.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        return outputmsg;
    }

    //saves the current map state to a text file as the first line.  Previous mapstates are in reverse order from the top down, each on one line.
    //Also, saves the previous version of the _MapState file as *_MapState_old.txt
    public bool MapStateSaved = false;
    public bool MapStateBackedUp = false;
    public double SaveMapState(string winner, bool intermediateSave = false, bool startupSave = false)
    {
        //Console.WriteLine("Map Save #0");
        Tuple<double, string> res = CalcMapMove(winner, true, true, null);
        if (!intermediateSave && MapStateSaved) return res.Item1; //Due to the way it works (adding a certain value to the value in the file), we can only save map state ONCE per session.  So we can call it a few times near the end to be safe, but it only will save once at most
        try
        {

            //Console.WriteLine("Map Save #1");
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX8"); //Testing for potential causes of warping

            //Take care of updating Aircraft Supply here, based on Mission results
            //Also we can use the various failsafes in place to ensure mapsave happens, but no "double mapsave"
            //Same problem with Supply--we MUST save it once but can't save MORE THAN ONCE
            double misResult = res.Item1;
            double newMapState = CampaignMapState + misResult;
            string outputmsg = res.Item2;
            string msg = "";
            string turnString = "(none)";
            if (winner.Equals("Red") || winner.Equals("Blue")) turnString = winner;
            DateTime dt = DateTime.UtcNow;
            string date = dt.ToString("u");


            //Console.WriteLine("Map Save #2");

            bool writeOutput = true;
            //for testing
            if (intermediateSave) writeOutput = false;
            outputmsg += summarizeCurrentMapstate(newMapState, writeOutput);


            //TODO: We could write outputmsg to a file or send it to the -stats.cs or something
            //This saves the summary text to a file with CR/LF replaced with <br> so it can be used in HTML page

            /*  (removing this save here since we have it in CheckStatsData();
            try
            {
                File.WriteAllText(STATSCS_FULL_PATH + "CampaignSummary.txt", Regex.Replace(outputmsg, @"\r\n?|\n", "<br>" + Environment.NewLine));
            }
            catch (Exception ex) { Console.WriteLine("CampaignSummary Write: " + ex.ToString()); }
            */
            string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState.txt";
            string filepath_old = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState_old.txt";
            string currentContent = String.Empty;

            //Save most recent copy of Campaign Map Score with suffix _old
            try
            {
                if (File.Exists(filepath_old)) { File.Delete(filepath_old); }
                File.Copy(filepath, filepath_old);
            }
            catch (Exception ex) { Console.WriteLine("MapState Write Inner: " + ex.ToString()); }


            //Console.WriteLine("Map Save #3");

            //if (File.Exists(filepath)) { File.Delete(filepath); }
            /*fi = new System.IO.FileInfo(filepath); //file to write to
            sw = fi.CreateText();
            sw.WriteLine(newMapState.ToString());
            sw.Close(); */

            if (File.Exists(filepath))
            {
                currentContent = File.ReadAllText(filepath);
            }
            //TODO: We could trim currentContent to some certain length or whatever
            //currentContent = currentContent.Split(Environment.NewLine.ToCharArray()).FirstOrDefault(); //cut down prev content to max of 20 lines
            //currentContent = String.Join(Environment.NewLine, currentContent.Split(Environment.NewLine.ToCharArray(), 21).Take(20)); //cut down prev content to max of 20 lines

            //Console.WriteLine("Map Save #4");

            currentContent = String.Join(Environment.NewLine, currentContent.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(s => s.Trim()).Take(150)); //cut down prev content to max of $X lines & omit blank lines
            //File.WriteAllText(filepath, newMapState.ToString() + Environment.NewLine + turnString + Environment.NewLine + date + Environment.NewLine + currentContent);
            Calcs.WriteAllTextAsync(filepath, newMapState.ToString() + Environment.NewLine + turnString + Environment.NewLine + date + Environment.NewLine + currentContent);
            if (!intermediateSave) MapStateSaved = true;

            //Update supplies/resupply, but only at the very end
            if (!intermediateSave)
            {

                try
                {
                    double pcDone = calcProportionTimeComplete();

                    //This adds supply in based on how many players participated and how long the mission ran (15.5 hrs generally) as a basis
                    //then adjusting based on score & whether anyone has turned the map
                    int netRedCount = 15;
                    int netBlueCount = 15;

                    //if (TWCStatsMission != null)
                    //{
                    //Counting lines in <netstats summary from -stats.cs to get an approximation of how many active pilots there were in-game
                    //string netRed = TWCStatsMission.Display_SessionStatsAll(null, 1, false, true); //last true = NEED html version bec we're counting <br>s
                    string netRed = statsmission.Display_SessionStatsAll(null, 1, false, true); //last true = NEED html version bec we're counting <br>s
                    //string netBlue = TWCStatsMission.Display_SessionStatsAll(null, 2, false, true);
                    string netBlue = statsmission.Display_SessionStatsAll(null, 2, false, true);
                    netRed = netRed.Replace(@"***No Netstats to report***<br>", "");
                    //netRed.Replace()
                    netBlue = netBlue.Replace(@"***No Netstats to report***<br>", "");
                    //netBlue = netBlue.Replace(@"No Nets", "");
                    string target = "<br>";//Q&D way to count how many pilots active during the mission
                    Console.WriteLine("NR " + netRed);
                    Console.WriteLine("NR " + netBlue);
                    netRedCount = netRed.Select((c, i) => netRed.Substring(i)).Count(sub => sub.StartsWith(target)) - 1;
                    netBlueCount = netBlue.Select((c, i) => netBlue.Substring(i)).Count(sub => sub.StartsWith(target)) - 1;
                    //}

                    Console.WriteLine("Main-ReSupply: " + netRedCount.ToString() + " " + netBlueCount.ToString());
                    if (netRedCount < 0) netRedCount = 0;
                    if (netBlueCount < 0) netBlueCount = 0;
                    if (netRedCount > 120) netRedCount = 120;
                    if (netBlueCount > 120) netBlueCount = 120;
                    //Take care of changes to supply
                    double redMult = pcDone / 8.0 + 7.0 / 8.0 * pcDone * netRedCount / 20.0 + RedScoutedObjectivesI / 400.0;  //must cast to double first . . .
                    double blueMult = pcDone / 8.0 + 7.0 / 8.0 * pcDone * netBlueCount / 20.0 + BlueScoutedObjectivesI / 400.0; //Now also add some 'plane points' for reconnaissance, just a little bit

                    Console.WriteLine("Main-ReSupply: " + netRedCount.ToString() + " " + netBlueCount.ToString() + " " + redMult.ToString() + " " + blueMult.ToString() + " "
                        + redMultAdmin.ToString() + " " + blueMultAdmin.ToString() + " ");

                    //if one side turns the map they get a large increase in aircraft supply while the other side gets little or nothing
                    //if they don't turn the map there is still a slight tweak give the side with more overall victories a few more aircraft 
                    if (winner == "Red") { redMult += 4.0; blueMult = 0.01; } //2020/02 - now ADDING the bonus 4.0 points instead of just replacing the normal value
                    else if (winner == "Blue") { blueMult += 4.0; redMult = 0.01; }
                    else if (misResult > 0) redMult += misResult / 100.0;
                    else if (misResult < 0) blueMult += (-misResult) / 100.0;
                    redMult += redMultAdmin;
                    blueMult += blueMultAdmin;
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyEndMission(redMult, blueMult);

                    Console.WriteLine("Main-ReSupply: " + netRedCount.ToString() + " " + netBlueCount.ToString() + " " + redMult.ToString() + " " + blueMult.ToString() + " "
                        + redMultAdmin.ToString() + " " + blueMultAdmin.ToString() + " ");

                } catch (Exception ex) { Console.WriteLine("MapState Supply Save ERROR: " + ex.ToString()); }
            }

            //Save mapstate to special directory once @ beginning of mission & again at very end
            if (!MapStateBackedUp || !intermediateSave)
            {

                var backPath = STATSCS_FULL_PATH + CAMPAIGN_ID + @" campaign backups\";
                string filepath_date = backPath + CAMPAIGN_ID + "_MapState-" + dt.ToString("yyyy-MM-dd") + ".txt";

                //Create the directory for the MapState.txt backup files, if it doesn't exist
                if (!System.IO.File.Exists(backPath))
                {

                    try
                    {
                        //System.IO.File.Create(backPath);
                        System.IO.Directory.CreateDirectory(backPath);
                    }
                    catch (Exception ex) { Console.WriteLine("MapState Dir Create Date ERROR: " + ex.ToString()); }

                }

                //Save most recent copy of Campaign Map Score with suffix like  -2018-05-13.txt
                try
                {
                    if (File.Exists(filepath_date)) { File.Delete(filepath_date); }
                    File.Copy(filepath, filepath_date);
                    MapStateBackedUp = true;
                }
                catch (Exception ex) { Console.WriteLine("MapState Write Date: " + ex.ToString()); }

            }

        }
        catch (Exception ex) { Console.WriteLine("MapState Write: " + ex.ToString()); }

        return res.Item1; //mapmove score for *this mission*

    }

    public string GetMapSuffix()
    {
        double MapState = GetMapState();
        int MapState_int = Convert.ToInt32(MapState);
        if (MapState_int > CampaignMapMaxRedSuffixMax) MapState_int = CampaignMapMaxRedSuffixMax;
        if (-MapState_int > CampaignMapMaxBlueSuffixMax) MapState_int = -CampaignMapMaxBlueSuffixMax;

        if (MapState_int == 0) return "-0";
        if (MapState_int > 0) return "-R" + MapState_int.ToString("D3");  //3 digits so that our files will be named ie TWC M001-initairports-R002.mis - 002 is 3 digits
        else return "-B" + (-MapState_int).ToString("D3");
    }

    public double GetMapState()
    {

        double MapState = 0;
        string res = "";

        try
        {
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX2"); //Testing for potential causes of warping
            using (StreamReader sr = new StreamReader(STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState.txt"))
            {
                res = sr.ReadLine();
                MapState = Convert.ToDouble(res); //Total overall score; 0=tied, + = Red winning, - = Blue winning
                string prevWinner = sr.ReadLine();
                if (prevWinner == "Red" || prevWinner == "Blue") MapPrevWinner = prevWinner.Trim(); //Winner of previous mission, if there was one.

            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Main mission - read mapstate - Exception: " + ex.ToString());
            MapState = 0;
        }

        Console.WriteLine("Main mission - read mapstate: " + MapState.ToString() + " " + res + " : " + STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState.txt");

        if (MapState > 100000 || MapState < -100000) MapState = 0;
        CampaignMapState = MapState;
        return MapState;


    }
    /*  SAMPLE front markers

            FrontMarker0 122130.41 99692.54 1
          FrontMarker1 184923.58 199248.64 1
          FrontMarker2 151050.02 106909.01 1
          FrontMarker3 273615.78 251329.87 1
          FrontMarker4 318170.97 309361.61 1
          FrontMarker5 355340.23 298205.35 2
          FrontMarker6 284329.09 226250.35 2
          FrontMarker7 195643.43 103278.12 2
          FrontMarker8 88262.03 60001.96 2
          FrontMarker9 113282.01 85097.34 2
          FrontMarker10 341348.18 266004.67 2
          FrontMarker11 225658.13 150675.90 1
          FrontMarker12 253901.90 109419.35 2
          FrontMarker13 163783.69 109004.79 1
          FrontMarker14 189714.23 113052.10 1
          FrontMarker15 249954.27 141364.45 2
          FrontMarker16 171903.52 90341.35 2
          FrontMarker17 102887.45 94353.53 1
          FrontMarker18 94502.66 92980.57 1
          FrontMarker19 259759.88 188769.38 2
  FrontMarker20 230005.07 198916.29 1

        */
    public void DrawFrontLinesPerMapState(double minMapState = -25, double maxMapState = 25, double? currMapState = null, string saveName = null)
    {

        //so because of CloD weirdness, the lines need to go pretty much perpendicular to the dividing line between france & england.
        //To make the points, go into FMB, choose points WIDE apart, as wide as possible, opposite each other at the furthest
        //extremes of where the front lines will range--one furthest to the blue side, the other furthest to the red side
        //Choose the points in pairs (red side/blue side), and make them so that the resulting front line in the middle is
        //nice and smooth.
        //Then export those points from FMB, arrange them in order (left to right or whatever along the front line; FMB will likely scramble
        //them up) and then put them into the format below.


        List<List<Point2d>> frontPoints = new List<List<Point2d>>() {
    



            new List<Point2d>() {
            new Point2d (10618.86,166919.06), //furthest extreme point red side (north side) of the channel - starting at the west end of the map
            new Point2d (12535.86,80190.73), //furthest extreme point blue side (south side) of the channel
                    },

            new List<Point2d>() {
            new Point2d (43448.8,175135.64), //next points moving east, red side
            new Point2d (45083.75,77280.85), //blue side
            },

            new List<Point2d>() {
            new Point2d (63424.7,167040.97), //etc
            new Point2d (61680.21,74914.15),
            },

            new List<Point2d>() {
            new Point2d (83910.49,175416.5),
            new Point2d (78249.54,58702.71),
            },

            new List<Point2d>() {
            new Point2d (99733.32,184877.35),
            new Point2d (100949.88,42579.64),
            },

            new List<Point2d>() {
            new Point2d (110400,190272),
            new Point2d (126336,42672),
            },

            new List<Point2d>() {
            new Point2d (118688.29,190680.54),
            new Point2d (154214.29,61859.85),
            },

            new List<Point2d>() {
            new Point2d (131659.66,192833.41),
            new Point2d (159814.69,76825.13),
            },

            new List<Point2d>() {
            new Point2d (153984,192960),
            new Point2d (180864,89088),
            },

            new List<Point2d>() {
            new Point2d (163984,189460),
            new Point2d (190864,93588),
            },

            new List<Point2d>() {
            new Point2d (172992,185856),
            new Point2d (205056,97152),
            },

            new List<Point2d>() {
            new Point2d (179601.02,194613.41),
            new Point2d (231006.05,105485.13),
            },

            new List<Point2d>() {
            new Point2d (188597.32,199130.55),
            new Point2d (248621.26,119111.73),
            },

            new List<Point2d>() {
            new Point2d (202752,202944),
            new Point2d (258624,136320),
            },

            new List<Point2d>() {
            new Point2d (209664,209280),
            new Point2d (262080,165696),
            },

            new List<Point2d>() {
            new Point2d (216576,209088),
            new Point2d (262848,180672),
            },

            new List<Point2d>() {
            new Point2d (223385.59,208849.89),
            new Point2d (262894.58,192305.62),
            },

            new List<Point2d>() {
            new Point2d (227862.38,225744.41),
            new Point2d (264038.59,205787.15),
            },

            new List<Point2d>() {
            new Point2d (247504.44,234638.61),
            new Point2d (272136.04,212438.45),
            },

            new List<Point2d>() {
            new Point2d (251813.55,243841.49),
            new Point2d (281884.35,218378.42),
            },

            new List<Point2d>() {
            new Point2d (258959.67,254735.81),
            new Point2d (291874.9,221379.32),
            },

            new List<Point2d>() {
            new Point2d (255327.62,266250.02),
            new Point2d (302380.94,224076.27),
            },

            new List<Point2d>() {
            new Point2d (246498.73,285932.92),
            new Point2d (313365.97,227043.45),
            },

            new List<Point2d>() {
            new Point2d (250024.27,298217.73),
            new Point2d (325633.59,229383.98),
            },

            new List<Point2d>() {
            new Point2d (269866.47,310981.62),
            new Point2d (337857.27,235244.12),
            },

            new List<Point2d>() {
            new Point2d (302280.98,312446.52),
            new Point2d (347077.23,240365.27),
            },

            new List<Point2d>() {
            new Point2d (329328.93,313006.78),
            new Point2d (352757.32,244599.01),
            },

            new List<Point2d>() {
            new Point2d (352896,313536),
            new Point2d (358080,247296),
            },


        };

        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;
        int count = 0;

        double mapState = CampaignMapState;
        if (currMapState.HasValue) mapState = currMapState.Value;
        double neutral = (maxMapState - minMapState)/ 120;

        double mult = (mapState - neutral - minMapState) / (maxMapState - minMapState);  //mult is for red frontline (most northerly)
        double mult2 = (mapState + neutral - minMapState) / (maxMapState - minMapState);  //mult2 for blue, most southerly 
        if (mult > 1) mult = 1; //We COULD go beyond 0-1 but that might get weird.  So the Frontlines are drawn using a weird thing where the front goes halfway between p1 and p2 and PERPENDICULAR to the line from p1 to p2.  So we have to pay attention to make sure p1 & p2 form the right sort of line going the right direction.
        if (mult < 0) mult = 0;
        if (mult2 > 1) mult2 = 1; //We COULD go beyond 0-1 but that might get weird
        if (mult2 < 0) mult2 = 0;
        if (mult2 == mult) mult2 = mult + 0.05; //They can't be equal or HELP!!! So it's better to be a bit above 1 than equal.

        List<Point2d> lastPoints = null;

        foreach (List<Point2d> initpoints in frontPoints)
        {
            List<List<Point2d>> newPoints = new List<List<Point2d>>();
            
            
            //complicated little business to linearly interpolate one or perhaps several new points in between each existing point.
            //This helps smooth out the boundary A LOT.
            if (lastPoints != null)
            {
                double numPointsToInterpolate = 3;

                for (int i = 1; i < numPointsToInterpolate; i++)
                {
                    //Console.WriteLine("PTI: {0} {1}", i, numPointsToInterpolate);
                    double id = (double)i;
                    List<Point2d> newPoint = new List<Point2d>();
                    newPoint.Add(new Point2d(id*(initpoints[0].x - lastPoints[0].x) / numPointsToInterpolate + lastPoints[0].x, id*(initpoints[0].y - lastPoints[0].y) / numPointsToInterpolate + lastPoints[0].y));
                    newPoint.Add(new Point2d(id*(initpoints[1].x - lastPoints[1].x) / numPointsToInterpolate + lastPoints[1].x, id*(initpoints[1].y - lastPoints[1].y) / numPointsToInterpolate + lastPoints[1].y));
                    newPoints.Add(newPoint);
                }
            }

            newPoints.Add(initpoints);

            foreach (List<Point2d> points in newPoints)
            {

                double dist = Calcs.CalculatePointDistance(points[0], points[1]);

                double x = mult * points[1].x + (1 - mult) * points[0].x;  //linear interpolation between the two given points, based on how far we currently are between the min & max map state
                double y = mult * points[1].y + (1 - mult) * points[0].y;
                double x2 = mult2 * points[1].x + (1 - mult2) * points[0].x;  //linear interpolation between the two given points, based on how far we currently are between the min & max map state
                double y2 = mult2 * points[1].y + (1 - mult2) * points[0].y;

                sect = "FrontMarker";
                key = "FrontMarker" + count.ToString();
                value = x.ToString("F2") + " " + y.ToString("F2") + " 1"; //1 = army 1
                f.add(sect, key, value);

                //Console.WriteLine("({0:F0}, {1:F0}) - ({2:F0}, {3:F0})", x, y, x2, y2);
                count++;
                y += 1000;
                key = "FrontMarker" + count.ToString();
                value = x2.ToString("F2") + " " + y2.ToString("F2") + " 2";
                f.add(sect, key, value);
                count++;
            }

            lastPoints = initpoints;

        }
        GamePlay.gpPostMissionLoad(f);
        if (saveName != null) f.save(CLOD_PATH + FILE_PATH + "/" + saveName); //testing
        Console.WriteLine("Drew current frontline");


    }

    /******************************************************************************
     * 
     * END - LONG-TERM CAMPAIGN METHODS 
     *    
     ******************************************************************************/

    /*************************
     * ONACTORDEAD
     * **********************/

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        base.OnActorDead(missionNumber, shortName, actor, damages);
        try
        {

            if (actor as Player != null)
            {
                Task.Run(() => MO_SpoilPlayerScoutPhotos(actor as Player));
            }

            Task.Run(() => MO_HandlePointAreaObjectives(actor));

            if (actor != null && actor is AiAircraft)
            {
                //if dead, then destroy it within a reasonable time
                AiAircraft aircraft = actor as AiAircraft;

                MO_SpoilPlayerScoutPhotos(playersInPlane(aircraft));

                string pName = actor.Name();
                if (aircraft != null)
                {
                    //Timeout(300, () =>
                    Timeout(20, () => //testing
                    {
                        //Force a player into a certain place:
                        //Player.Place() = (Actor as AiAircraft).Place(placeIndex);
                        if (aircraft.Places() > 0) for (int i = 0; i < aircraft.Places(); i++)
                            {
                                //aircraft.Player(i).Place() = null;
                                //aircraft.Player(i).PlaceEnter(null,0);
                                if (aircraft.Player(i) != null) aircraft.Player(i).PlaceLeave(i);
                            }

                        //Wait 0.5 second for player(s) to leave, then destroy
                        Timeout(0.5, () =>
                        {
                            Console.WriteLine("Destroyed dead aircraft " + pName + " " + aircraft.Type());
                            destroyPlane(aircraft);  //Destroy completely when dead, after a reasonable time period.

                        });

                    });
                }

            }

            if (actor != null && actor is AiGroundActor)
            {
                //If we destroy dead ground objs too soon then eg big oil refinery fires will go out after just a few minutes
                //Ideally we'd have a filter of some time here to destroy smaller items pretty soon but other bigger ones after a longer time
                Timeout(90 * 60, () =>
                {
                    (actor as AiGroundActor).Destroy();
                    Console.WriteLine("Destroyed dead ground object " + actor.Name());

                });

                Console.WriteLine("Ground object has died. Name: " + actor.Name());

            }



        }
        catch (Exception ex) { Console.WriteLine("OPD: " + ex.ToString()); }
    }

    /*************************
     * ONPERSONHEALTH
     * **********************/
    public override void OnPersonHealth(maddox.game.world.AiPerson person, maddox.game.world.AiDamageInitiator initiator, float deltaHealth)
    {
        #region stats
        base.OnPersonHealth(person, initiator, deltaHealth);
        try
        {
            //twcLogServer(null, "Health Changed for " + person.Player().Name(), new object[] { });
            if (person != null)
            {
                Player player = person.Player();
                //if (deltaHealth>0 && player != null && player.Name() != null) {
                if (player != null && player.Name() != null)
                {
                    if (DEBUG) twcLogServer(null, "Main: OnPersonHealth for " + player.Name() + " health " + player.PersonPrimary().Health.ToString("F2"), new object[] { });
                    //if the person is completely dead we are going to force them to leave their place
                    //This prevents zombie dead players from just sitting in their planes interminably, 
                    //which clogs up the airports etc & prevents the planes from dying & de-spawning
                    //Not really sure the code below is working.
                    if (player.PersonPrimary() != null && player.PersonPrimary().Health == 0
                        && (player.PersonSecondary() == null
                            || (player.PersonSecondary() != null && player.PersonSecondary().Health == 0)))
                    {
                        //Timeout(300, () =>
                        if (DEBUG) twcLogServer(null, "Main: 2 OnPersonHealth for " + player.Name(), new object[] { });
                        Timeout(20, () => //testing
                        {
                            if (DEBUG) twcLogServer(null, "Main: 3 OnPersonHealth for " + player.Name(), new object[] { });
                            //Checking health a second time gives them a while to switch to a different position if
                            //it is available
                            if (player.PersonPrimary() != null && player.PersonPrimary().Health == 0
                                && (player.PersonSecondary() == null
                                    || (player.PersonSecondary() != null && player.PersonSecondary().Health == 0)))
                            {
                                if (DEBUG) twcLogServer(null, "Main: 4 OnPersonHealth for " + player.Name(), new object[] { });

                                //Not really sure how this works, but this is a good guess.  
                                //if (player.PersonPrimary() != null )player.PlaceLeave(0);
                                //if (player.PersonSecondary() != null) player.PlaceLeave(1);
                                if (player.PersonPrimary() != null) player.PlaceLeave(player.PersonPrimary().Place());
                                if (player.PersonSecondary() != null) player.PlaceLeave(player.PersonSecondary().Place());
                            }
                            if (DEBUG) twcLogServer(null, player.Name() + " died and was forced to leave player's current place.", new object[] { });

                            if (DEBUG) twcLogServer(null, "Main: OnPersonHealth for " + player.Name() + " health1 " + player.PersonPrimary().Health.ToString("F2")
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

        MO_SpoilPlayerScoutPhotos(playersInPlane(aircraft));

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

        try
        {
            if (GamePlay.gpFrontArmy((aircraft as AiActor).Pos().x, (aircraft as AiActor).Pos().y) == (aircraft as AiActor).Army())

                MO_RecordPlayerScoutPhotos(playersInPlane(aircraft));
        }
        catch (Exception ex) { System.Console.WriteLine("Main mission OnAircraftLanded recon record - Exception: " + ex.ToString()); }

    }

    public override void OnPersonParachuteFailed(maddox.game.world.AiPerson person)
    {
        base.OnPersonParachuteFailed(person);
        if (person.Player() != null) MO_SpoilPlayerScoutPhotos(person.Player());
    }

    public override void OnPersonParachuteLanded(maddox.game.world.AiPerson person)
    {
        base.OnPersonParachuteLanded(person);
        if (person.Player() != null) MO_SpoilPlayerScoutPhotos(person.Player());
    }

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        MO_SpoilPlayerScoutPhotos(playersInPlane(aircraft));
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


                                        /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " "                                          
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
    #region onbuildingkilled

    //OnBuildingKilled only works on offline servers
    //UPDATE. It is supposed to work in TF 4.57.  Only for individually/.mis-placed buildings however, not for the built-in in-game buildings
    //A few (random?) buildings also report to this routine in the multiplayer/online servers

    /*
     * 
     * SUMMARY OF ONBUILDINGKILLED AND TRIGGER (VIA BUILDINGS, DIFFERENT SUBMISSIONS) SITUATION AS OF 2020/02 - TF4.57
     * 
     TWC_Flug: #1. OnBuildingKilled only works with AI aircraft.  Works perfectly with them.  Anything I killed--never reported.  I have it turned on in the server now (it would, for example, save stats for a player who took out a certain building) but it won't report anything except AI kills as near as I can see.
        [1:41 AM] TWC_Flug: It is exactly as reported here: https://theairtacticalassaultgroup.com/forum/showthread.php?t=31359
        OnBuildingKilled() Issues
        So with reference to the repaired OnBuildingKilled() script function and the usage of the following script: 

        public override void OnBuildingKilled(string title, Point3d pos, AiDamageInitiator initiator, int eventArgInt) { 

        string parts = title.Split(new string { SEP...
        [1:42 AM] TWC_Flug: #2. Building kills definitely do not count towards setting off a trigger.  Even when the AI hit it and it registers.  Still, no matter how hard you hit it, no trigger.
        [1:43 AM] TWC_Flug: #3. Also I tried putting stationary objects in an area, using a submission.  I tried putting the trigger in the submission and seeing if the -main.cs script would detect it.  No.
        [1:43 AM] TWC_Flug: (Presumably you could put a little trigger detector in -submission.cs, that would detect it, then you could pass it to -main.cs.  So, possibly but painful.)
        [1:45 AM] TWC_Flug: #3. Then I tried putting the stationary objects in the submission but the trigger in the main.mis file.  I was hoping the trigger would trigger off of ANY stationary or other object in its radius, no matter what submission it was loaded from.  But no dice.  It doesn't respond even if you have killed every last one of those objects, if they are loaded from a submission file rather than the main mission file.
        [1:45 AM] TWC_Flug: So all that is kind of disappointing.  Our -stats script works by detecting any and all stationaries from any and all missions, and it works just fine.  But the built-in triggers just don't do that.
        [1:47 AM] TWC_Flug: That's one reason I think it might be better to just roll our own "simulated trigger" using some special object to indicate where the trigger area is.  You could detect any and all stationaries, buildings, or just ground explosions that happen within X distance of that special object and then that works exactly like a trigger but we're in control of exactly how it works.
        [1:48 AM] TWC_Flug: We couldn't have to keep track of the object's ID# as we were doing before.  Instead, you just keep track of it's X/Y position.  Which you want/need to know anyway.

     */

    public override void OnBuildingKilled(string title, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnBuildingKilled(title, pos, initiator, eventArgInt);

        //try
        {
            Console.WriteLine("BUILDING:" + title + " at " + pos.x.ToString("F0") + ", " + pos.y.ToString("F0"));

            Task.Run(() => MO_HandlePointAreaObjectives(null, initiator, title, pos)); //handle building killed within PointArea area
            string BuildingName = title;
            string BuildingArmy = "";
            string PlayerArmy = "Unknown/AI";
            string sectorTitle = "";
            string sectorName = GamePlay.gpSectorName(pos.x, pos.y);

            if (GamePlay.gpFrontArmy(pos.x, pos.y) == 1)
            {
                BuildingArmy = "England";
            }
            else if (GamePlay.gpFrontArmy(pos.x, pos.y) == 2)
            {
                BuildingArmy = "France";
            }
            else
            {
                BuildingArmy = "Neutral";
            }
            if (initiator != null && initiator.Player as Player != null)
            {
                if (initiator.Player.Army() == 1)
                {
                    PlayerArmy = "RAF";
                }
                else if (initiator.Player.Army() == 2)
                {
                    PlayerArmy = "Luftwaffe";
                }
                else
                {
                    PlayerArmy = "Unknown";
                }
            }
            else if (initiator != null && initiator.Actor as AiActor != null)
            {
                if (initiator.Actor.Army() == 1)
                {
                    PlayerArmy = "RAF";
                }
                else if (initiator.Actor.Army() == 2)
                {
                    PlayerArmy = "Luftwaffe";
                }
                else
                {
                    PlayerArmy = "Unknown (AI)";
                }
            }

            string killerName = "(AI)";
            if (initiator != null && initiator.Player != null) killerName = initiator.Player.Name();
            else if (initiator != null && initiator.Actor != null) killerName = initiator.Actor.Name();

            Console.WriteLine("BUILDING:" + BuildingName + " in " + BuildingArmy + " was destroyed in sector " + sectorName + " by " + killerName + " from the " + PlayerArmy + ".");
        }
        //catch (Exception ex) { Console.WriteLine("Main OnBuildingKilled ERROR: " + ex.ToString()); };

    }


    #endregion

    double nearAirGroupThreshhold_m = 7500;
    double nearAirGroupAltThreshhold_m = 2000;
    //public enum aiorhuman { AI, Mixed, Human };

    public class AirGroupInfo : IAirGroupInfo
    {
        public double time { get; set; } //Battle.time.current;
        public HashSet<AiAirGroup> nearbyAirGroups { get; set; }// = new HashSet<AiAirGroup>();  // { get; set; } //those groups that are nearby OR near any nearby aircraft of the same type (greedy)
        public HashSet<AiAirGroup> groupedAirGroups { get; set; } // = new HashSet<AiAirGroup>(); //{ get; set; } //groups that have been nearby for that past X iterations, thus counting as part of the same Group
        public Point3d pos { get; set; }
        public Point3d vel { get; set; }
        public bool belowRadar { get; set; }
        public double altAGL_ft { get; set; }
        public double altAGL_m { get; set; }
        public int count { get; set; }
        public string type { get; set; }
        public bool isHeavyBomber { get; set; }
        public bool isAI { get; set; }
        public string playerNames { get; set; }
        public AiActor actor { get; set; }
        public AiAirGroup airGroup { get; set; }
        public bool isLeader { get; set; }
        public AiAirGroup leader { get; set; }
        public string sector { get; set; }
        public string sectorKeyp { get; set; }
        public int giantKeypad { get; set; }




        //Above are individual airgroup/aircraft values - below are the composite values for the entire airgroup ("Air Group Grouping" - AGG) - in case this is the leader of the grouping.  Otherwise blank/default
        public Point3d AGGpos { get; set; }    //exact loc of the primary a/c
        public Point3d AGGavePos { get; set; } //average loc of all a/c      
        public string AGGsector { get; set; }
        public string AGGsectorKeyp { get; set; }
        public int AGGgiantKeypad { get; set; }
        public Point3d AGGvel { get; set; }
        public int AGGcount { get; set; } //total # in group, including all above & below radar
        public int AGGcountAboveRadar { get; set; } //if countAboveRadar is 0 this group won't show up at all.  This is the count that shows to ordinary players
        public int AGGcountBelowRadar { get; set; }
        public bool AGGradarDropout { get; set; }
        public double AGGminAlt_m { get; set; }
        public double AGGmaxAlt_m { get; set; }
        public double AGGaveAlt_m { get; set; }
        public double AGGavealtAGL_ft { get; set; }
        public string AGGtypeNames { get; set; }
        public string AGGplayerNames { get; set; }
        public string AGGids { get; set; }  //the actor.Name()s compiled into a string
        public aiorhuman AGGAIorHuman { get; set; }
        public string AGGtype { get; set; }    //the actual type: "F" or "B".
        public string AGGmixupType { get; set; } //the type that will actually display on user radar, which is sometimes/often "mixed up".  "F" "B" or "U" for unknown
        public bool AGGisHeavyBomber { get; set; }
        public AMission mission { get; set; }



        public AirGroupInfo()
        {

        }

        public AirGroupInfo(AiActor a, AiAirGroup aag, Point3d p, Point3d v, int c, string ty, bool i, HashSet<AiAirGroup> nag, Mission msn, double tm)
        {
            actor = a;
            pos = p;
            vel = v;
            count = c;
            type = ty;
            isHeavyBomber = i;
            nearbyAirGroups = nag;
            time = tm;
            nearbyAirGroups.Add(aag); //always add self

        }
        public AirGroupInfo(AiActor act, AiAirGroup ag, Mission msn, double tm)
        {
            //Console.WriteLine("AGI 1");

            nearbyAirGroups = new HashSet<AiAirGroup>();  // { get; set; } //those groups that are nearby OR near any nearby aircraft of the same type (greedy)
            groupedAirGroups = new HashSet<AiAirGroup>(); //{ get; set; } //groups that have been nearby for that past X iterations, thus 

            AiAircraft a = act as AiAircraft;
            actor = act;
            airGroup = ag;
            //Console.WriteLine("AGI 2");
            nearbyAirGroups.Add(ag); //always add self
            time = tm;
            //Console.WriteLine("AGI 3");
            isAI = msn.isAiControlledPlane2(a);
            if (isAI) AGGAIorHuman = aiorhuman.AI;
            else AGGAIorHuman = aiorhuman.Human;
            count = airGroup.NOfAirc;
            AGGcount = count;
            mission = msn;
            if (isAI)
            {
                playerNames = actor.Name();
                AGGplayerNames = actor.Name();
            }
            else
            {
                bool first = true;
                string aplayername = "";
                /*
                if (a.Player(0) != null && a.Player(0).Name() != null)
                {
                    aplayername = a.Player(0).Name();
                }
                */
                for (int i = 0; i < a.Places(); i++)
                {
                    if (a.Player(i) != null && a.Player(i).Name() != null)
                    {
                        if (!first) aplayername += " - ";
                        aplayername += a.Player(i).Name();
                        first = false;

                    }
                }
                playerNames = aplayername;
                AGGplayerNames = playerNames;
            }

            AGGtypeNames = Calcs.GetAircraftType(actor as AiAircraft);



            //if (!player_place_set &&  (a.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"


            //bool isAI = isAiControlledPlane2(a);
            //Console.WriteLine("AGI 4");
            string acType = Calcs.GetAircraftType(a);
            isHeavyBomber = false;
            if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkIV")) isHeavyBomber = true;
            AGGisHeavyBomber = isHeavyBomber;

            string t = a.Type().ToString();
            if (t.Contains("Fighter") || t.Contains("fighter")) type = "F";
            else if (t.Contains("Bomber") || t.Contains("bomber")) type = "B";
            AGGtype = type;

            /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
             
             + a.Type() + " " 
             + a.TypedName() + " " 
             +  a.AirGroup().ID(), new object[] { });
            */
            pos = a.Pos();
            AGGpos = pos;
            sector = Calcs.correctedSectorName(mission as Mission, pos);
            AGGsector = sector;
            sectorKeyp = Calcs.correctedSectorNameKeypad(mission as Mission, pos);
            AGGsectorKeyp = sectorKeyp;
            giantKeypad = Calcs.giantkeypad(pos);
            AGGgiantKeypad = giantKeypad;
            AGGmaxAlt_m = pos.z;
            AGGminAlt_m = pos.z;
            AGGaveAlt_m = pos.z;
            altAGL_m = a.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0); // Z_AltitudeAGL is in meters
            altAGL_ft = Calcs.meters2feet(altAGL_m);
            AGGavealtAGL_ft = altAGL_ft;

            //Thread.Sleep(100);
            //pos2=a.Pos();
            //bearing=Calcs.CalculateGradientAngle (pos1,pos2);
            Vector3d Vwld = ag.Vwld();
            /*
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
            */
            /* alt_km = a.Pos().z / 1000;
            alt_ft = Calcs.meters2feet(a.Pos().z);
            altAGL_m = (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0); // I THINK (?) that Z_AltitudeAGL is in meters?
            altAGL_ft = Calcs.meters2feet(altAGL_m);
            alt_angels = Calcs.Feet2Angels(alt_ft);
            sector = GamePlay.gpSectorName(a.Pos().x, a.Pos().y).ToString();
            sector = sector.Replace(",", ""); // remove the comma */
            //Console.WriteLine("AGI 5");
            vel = new Point3d(Vwld.x, Vwld.y, Vwld.z);
            AGGvel = vel;
            double vel_mps = Calcs.CalculatePointDistance(vel);
            belowRadar = (mission as Mission).belowRadar(altAGL_ft, vel_mps, airGroup, a);
            if (belowRadar) { AGGcountAboveRadar = 0; AGGcountBelowRadar = 1; }
            else { AGGcountAboveRadar = 1; AGGcountBelowRadar = 0; }
            AGGradarDropout = false;


        }

        public void addAG(AiAirGroup ag)
        {
            nearbyAirGroups.Add(ag);
        }
        public void addAGs(HashSet<AiAirGroup> ags)
        {
            nearbyAirGroups.UnionWith(ags);
        }

        //Check if the two a/c are closer than the threshhold and meet other criteria, such as same type of fighter/bomber, within reasonable altitude range and if so add mutually to each other's nearby airgroups list
        public void checkIfNearbyAndAdd(AirGroupInfo agi2)
        {
            Point3d tempos = agi2.pos;
            if (agi2.type == type && pos.distance(ref tempos) <= (mission as Mission).nearAirGroupThreshhold_m && (Math.Abs(agi2.pos.z - pos.z) <= (mission as Mission).nearAirGroupAltThreshhold_m))
            {
                addAG(agi2.airGroup);
                agi2.addAG(airGroup);
                //Console.WriteLine("AGI: Adding {0} {1:N0} {2:N0}, 1st: {3}, 2nd: {4}, {5}", playerNames, pos.distance(ref tempos), Math.Abs(agi2.pos.z - pos.z), nearbyAirGroups.Count, agi2.nearbyAirGroups.Count, agi2.playerNames);
            } else { // Console.WriteLine("AGI: NOT Adding {0} {1} {2}", type, pos.distance(ref tempos), mission.nearAirGroupThreshhold_m); 
            }

        }
        public void mutuallyAddNearbyAirgroups(AirGroupInfo agi2)
        {

            addAGs(agi2.nearbyAirGroups);
            agi2.addAGs(nearbyAirGroups);
            //Console.WriteLine("AGI: Adding {0} {1} 2nd: {2} {3}", playerNames, nearbyAirGroups.Count, agi2.nearbyAirGroups.Count, agi2.playerNames);


        }
        public string ToString()
        { return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {23} {24} {25} {26} {27} {28}",
                actor.Name(),
                pos,
                vel,
                belowRadar,
                altAGL_ft,
                altAGL_m,
                count,
                type,
                isHeavyBomber,
                isAI,
                airGroup,
                isLeader,
                leader,
                AGGpos,    //exact loc of the primary a/c
                AGGavePos, //average loc of all a/c        
                AGGvel,
                AGGcount, //total # in group, including all above & below radar
                AGGcountAboveRadar, //if countAboveRadar is 0 this group won't show up at all.  This is the count that shows to ordinary players
                AGGcountBelowRadar,
                AGGminAlt_m,
                AGGmaxAlt_m,
                AGGaveAlt_m,
                AGGavealtAGL_ft,
                AGGtypeNames,
                AGGplayerNames,
                AGGids,  //the actor.Name()s compiled into a string
                AGGAIorHuman,
                AGGtype,
                AGGisHeavyBomber);
        }

    }

    public CircularArray<Dictionary<AiAirGroup, AirGroupInfo>> airGroupInfoCircArr = new CircularArray<Dictionary<AiAirGroup, AirGroupInfo>>(6);

    HashSet<AiAirGroup>[] CurrentAG = new HashSet<AiAirGroup>[3]; //array with all current AirGroups, which Radar can easily loop through, for each army
    HashSet<AiAirGroup>[] CurrentAGGroupLeaders = new HashSet<AiAirGroup>[3]; //array with ONLY thos airgroups that are currently the prime lead member of a grouping

    //CircularArray <AiAirGroup> closeAircraft = new CircularArray<AiAirGroup>(4); //Here we store which a/c are close to any given aircraft this run.  We save the last 4 runs in a circular array & we can use that to determine which aircraft/airgroups are traveling together



    public void groupAllAircraft_recurs()
    {
        /************************************************
         * 
         * Check/download/transfer stats data
         * Recursive function called every X seconds
         ************************************************/
        //Timeout(188, () => { CheckStatsData(); });

        //Timeout(31, () => { Task.Run(() => groupAllAircraft()); });
        Timeout(31, () => { groupAllAircraft_recurs(); });
        //Console.WriteLine("groupAllAircraft: -1");
        Task.Run(() => groupAllAircraft());
    }



    public void groupAllAircraft()
    {
        try
        {
            Dictionary<AiAirGroup, AirGroupInfo> airGroupInfoDict = new Dictionary<AiAirGroup, AirGroupInfo>();

            //First go through & identify which airgroups are nearby to which others individually

            //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                foreach (int army in GamePlay.gpArmies())
                {
                    //Console.WriteLine("groupAllAircraft: 0");
                    CurrentAG[army] = new HashSet<AiAirGroup>();

                    HashSet<AiAirGroup> doneAG = new HashSet<AiAirGroup>();


                    //if (GamePlay.gpAirGroups(army) != null)
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {
                        //Console.WriteLine("groupAllAircraft: 0.5");
                        doneAG.Add(airGroup);
                        //aigroup_count++;
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            //Console.WriteLine("groupAllAircraft: 1");
                            //poscount = airGroup.NOfAirc;
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                //Console.WriteLine("groupAllAircraft: 1.1");
                                if (actor is AiAircraft)
                                {
                                    //Console.WriteLine("groupAllAircraft: 1.2");
                                    CurrentAG[army].Add(airGroup);
                                    //Console.WriteLine("groupAllAircraft: 1.3");

                                    //AirGroupInfo tmp = new AirGroupInfo(actor, airGroup, this, Time.current());
                                    AirGroupInfo tmp = new AirGroupInfo();
                                    //Console.WriteLine("groupAllAircraft: 1.3a");
                                    //Console.WriteLine("groupAllAircraft: 1.3a {0} {1} {2}", actor, airGroup, this);

                                    /*
                                    if (!airGroupInfoDict.TryGetValue(airGroup, out tmp))
                                    {
                                        Console.WriteLine("groupAllAircraft: 1.3b");
                                        tmp = new AirGroupInfo(actor, airGroup, this, 32);
                                        airGroupInfoDict[airGroup] = tmp;
                                    }*/

                                    if (!(airGroupInfoDict.ContainsKey(airGroup))) airGroupInfoDict[airGroup] = new AirGroupInfo(actor, airGroup, this, Time.current());

                                    //Console.WriteLine("groupAllAircraft: 1.4");
                                    foreach (AiAirGroup airGroup2 in GamePlay.gpAirGroups(army))
                                    {
                                        //Console.WriteLine("groupAllAircraft: 1.5");
                                        if (doneAG.Contains(airGroup2)) continue;
                                        if (airGroup2.GetItems() != null && airGroup2.GetItems().Length > 0)
                                        {
                                            //Console.WriteLine("groupAllAircraft: 1.6");
                                            //poscount2 = airGroup.NOfAirc;
                                            foreach (AiActor actor2 in airGroup2.GetItems())
                                            {
                                                if (actor2 is AiAircraft)
                                                {
                                                    //Console.WriteLine("groupAllAircraft: 1.7");
                                                    if (!airGroupInfoDict.ContainsKey(airGroup2)) airGroupInfoDict[airGroup2] = new AirGroupInfo(actor2, airGroup2, this, Time.current());
                                                    /*
                                                    AirGroupInfo tmp1 = new AirGroupInfo();
                                                    if (!airGroupInfoDict.TryGetValue(airGroup2, out tmp1))
                                                    {
                                                        Console.WriteLine("groupAllAircraft: 1.7b");
                                                        tmp1 = new AirGroupInfo(actor, airGroup2, this, 32);
                                                    }
                                                    */
                                                    //Console.WriteLine("groupAllAircraft: 1.8");
                                                    airGroupInfoDict[airGroup2].checkIfNearbyAndAdd(airGroupInfoDict[airGroup]);
                                                    break;  //we only need the first one of each AI group


                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Console.WriteLine("groupAllAircraft: 3");

                    //Now go through & group them into groups that are close together and the same type (bomber/fighter)
                    //CurrentAGGroupLeaders[army] = new HashSet<AiAirGroup>();
                    HashSet<AiAirGroup> DoneAGnearby = new HashSet<AiAirGroup>();

                    foreach (AiAirGroup airGroup in CurrentAG[army])
                    {
                        //Console.WriteLine("groupAllAircraft: 4");
                        if (DoneAGnearby.Contains(airGroup))
                        {
                            continue;
                        }
                        //Console.WriteLine("groupAllAircraft: 4.1");
                        //CurrentAGGroupLeaders[army].Add(airGroup); //This ag is the primary/lead of this airgroup
                        //airGroupInfoDict[airGroup].isLeader = true;    //.isLeader = true;

                        //Console.WriteLine("groupAllAircraft: 4.2");
                        bool complete = false;
                        int i = 0;
                        while (!complete && i < 40)
                        {  //keep adding nearby aircraft greedily, but with a circuit breaker of 40X
                            complete = true;
                            i++;
                            HashSet<AiAirGroup> nb = new HashSet<AiAirGroup>(airGroupInfoDict[airGroup].nearbyAirGroups);
                            //HashSet <AiAirGroup> nb = ;
                            //Console.WriteLine("groupAllAircraft: 4.3");
                            if (nb != null) foreach (AiAirGroup ag in nb)
                                {
                                    //Console.WriteLine("groupAllAircraft: 4.35");
                                    if (DoneAGnearby.Contains(ag)) continue;

                                    //Console.WriteLine("groupAllAircraft: 4.4");
                                    if (airGroupInfoDict.ContainsKey(ag))
                                    {
                                        complete = false;
                                        airGroupInfoDict[airGroup].mutuallyAddNearbyAirgroups(airGroupInfoDict[ag]);  //any a/c that is close an a/c close to the leader, is close to & grouped with the leader.  A "greedy" algorithm.
                                                                                                                      //Console.WriteLine("groupAllAircraft: 4.5");
                                        airGroupInfoDict[ag].leader = airGroup;
                                        airGroupInfoDict[ag].isLeader = false;
                                    }
                                    //Console.WriteLine("groupAllAircraft: 4.6");
                                    DoneAGnearby.Add(ag);
                                    //Console.WriteLine("groupAllAircraft: 4.7");
                                }
                            //Console.WriteLine("groupAllAircraft: 4.8");
                        }

                        //DoneAGgrouped.UnionWith(airGroupInfoDict[airGroup].nearbyAirGroups);

                    }

                    /*Dictionary<AiAirGroup, AirGroupInfo> a1 = airGroupInfoCircArr.Get(0); //Last iteration
                    Dictionary<AiAirGroup, AirGroupInfo> a2 = airGroupInfoCircArr.Get(1); //2nd to last iteration
                    Dictionary<AiAirGroup, AirGroupInfo> a3 = airGroupInfoCircArr.Get(2); //3rd to last iteration 
                    */
                    Dictionary<AiAirGroup, AirGroupInfo>[] aGICA = airGroupInfoCircArr.ArrayStack; //The array with last pushed on in 0 position

                    //airGroupInfoCircArr.Push(airGroupInfoDict);

                    //Now go through AGAIN & identify which have been nearby for X iterations, meaning that they are traveling together & part of the same group
                    CurrentAGGroupLeaders[army] = new HashSet<AiAirGroup>();
                    //Console.WriteLine("groupAllAircraft: 4.9 {0}",army);
                    HashSet<AiAirGroup> DoneAGgrouped = new HashSet<AiAirGroup>();

                    foreach (AiAirGroup airGroup in CurrentAG[army])
                    {
                        //Console.WriteLine("groupAllAircraft: a4");
                        if (DoneAGgrouped.Contains(airGroup))
                        {
                            continue;
                        }
                        //Console.WriteLine("groupAllAircraft: a4.1");
                        CurrentAGGroupLeaders[army].Add(airGroup); //This ag is the primary/lead of this airgroup
                        airGroupInfoDict[airGroup].isLeader = true;    //.isLeader = true;

                        //Console.WriteLine("groupAllAircraft: a4.2");
                        bool complete = false;
                        int i = 0;
                        HashSet<AiAirGroup> nb = airGroupInfoDict[airGroup].nearbyAirGroups;
                        HashSet<AiAirGroup> grouped = new HashSet<AiAirGroup>(nb);
                        //airGroupInfoDict[airGroup].groupedAirGroups = nb;  //we start off with the a/c that are nearby us now

                        //Console.WriteLine("Grouping: Leader {0} started with {1} groups at {2:N0}", airGroupInfoDict[airGroup].playerNames, grouped.Count, airGroupInfoDict[airGroup].time);

                        int maxStack = 6;//We are saving 6 of the aiGroupInfoCircArr; we save a new one every 30 seconds approx.  So 6 means the a/c have been flying together for about 3 minutes
                        if (aGICA.Length < maxStack) maxStack = aGICA.Length;

                        for (i = 0; i < maxStack; i++) {
                            //aGICA

                            HashSet<AiAirGroup> nba = new HashSet<AiAirGroup>();
                            if (aGICA[i] != null && aGICA[i].ContainsKey(airGroup))
                            {
                                nba = new HashSet<AiAirGroup>(aGICA[i][airGroup].nearbyAirGroups);
                                double time = aGICA[i][airGroup].time;
                                //if (nba != null)
                                if (nba.Count > 0) grouped.IntersectWith(nba);  //Now we eliminate any that were NOT nearby last run
                                //Console.WriteLine("Grouping: Leader {0} step{5}: {1} groups of {2} ({3}) possible at {4:N0} ", airGroupInfoDict[airGroup].playerNames, grouped.Count, nba.Count, aGICA[i][airGroup].nearbyAirGroups.Count, time, i);
                            }
                        }
                        /*
                        //We are intersecting here, so if the 2nd hashset is EMPTY or DOESN'T EXIST
                        //we'll end up with a grouping with 0 elements, not even the original a/c.
                        //So we check to make sure the airGroupInfoDict from previous runs exists, and then
                        //also that it's length is >0.  It should always be at least 1 as it should include itself
                        HashSet<AiAirGroup> nba = new HashSet<AiAirGroup>();
                        if (a1 != null && a1.ContainsKey(airGroup))
                        {
                            nba = new HashSet<AiAirGroup>(a1[airGroup].nearbyAirGroups);
                            double time = a1[airGroup].time;
                            //if (nba != null)
                            if (nba.Count > 0) grouped.IntersectWith(nba);  //Now we eliminate any that were NOT nearby last run
                            Console.WriteLine("Grouping: Leader {0} step2: {1} groups of {2} ({3}) possible at {4:N0} ", airGroupInfoDict[airGroup].playerNames, grouped.Count, nba.Count, a1[airGroup].nearbyAirGroups.Count, time);
                        }
                        HashSet<AiAirGroup> nbb = new HashSet<AiAirGroup>();
                        if (a2 != null && a2.ContainsKey(airGroup))
                        {
                            nbb = new HashSet<AiAirGroup>(a1[airGroup].nearbyAirGroups);
                            double time = a2[airGroup].time;
                            //if (nba != null)
                            if (nbb.Count > 0) grouped.IntersectWith(nbb);  //Now we eliminate any that were NOT nearby last run
                            Console.WriteLine("Grouping: Leader {0} step3: {1} groups of {2} ({3}) possible at {4:N0} ", airGroupInfoDict[airGroup].playerNames, grouped.Count, nbb.Count, a2[airGroup].nearbyAirGroups.Count, time);
                        }
                        */
                        /*

                        HashSet<AiAirGroup> nbb = new HashSet<AiAirGroup>();
                        if (a2 != null && a2.ContainsKey(airGroup))
                        {
                            nbb = a2[airGroup].nearbyAirGroups;
                            //if (set && nbb != null)
                            if (nbb.Count > 0) airGroupInfoDict[airGroup].groupedAirGroups.IntersectWith(nbb);  //Eliminate any NOT nearby two runs ago
                            Console.WriteLine("Grouping: Leader {0} step3: {1} groups of {2} possible ", airGroupInfoDict[airGroup].playerNames, airGroupInfoDict[airGroup].groupedAirGroups.Count, nbb.Count);
                        }
                        */

                        //HashSet<AiAirGroup> gag = new HashSet<AiAirGroup> ( airGroupInfoDict[airGroup].groupedAirGroups);
                        HashSet<AiAirGroup> toremovefrom_gag = new HashSet<AiAirGroup>();

                        //Console.WriteLine("Grouping: Leader {0} added {1} groups ", airGroupInfoDict[airGroup].playerNames, grouped.Count);

                        //Console.WriteLine("groupAllAircraft: a4.3");
                        if (grouped != null) foreach (AiAirGroup ag in grouped)
                            {


                                //Console.WriteLine("groupAllAircraft: a4.4");
                                if (airGroupInfoDict.ContainsKey(ag))
                                {

                                    //Console.WriteLine("groupAllAircraft: a4.35");
                                    if (DoneAGgrouped.Contains(ag))
                                    {
                                        if (airGroupInfoDict[ag].leader != airGroup) //ag is close to more than one a/c but another has already claimed it as leader, and that one hasn't also claimed airGroup as part of its group.  So, we have to remove ag from airGroup's group as it's already been claimed by another
                                        {
                                            //airGroupInfoDict[airGroup].groupedAirGroups.Remove(ag);
                                            //mysteriously, we can't remove it inside the foreach loop (even though we're supposedly running on a copy?!) so we save it for later removal
                                            toremovefrom_gag.Add(ag);
                                        }
                                        continue;
                                    }
                                    complete = false;
                                    //Console.WriteLine("Grouping: Leader {0} added {1} groups ", airGroupInfoDict[airGroup].actor.Name(), airGroupInfoDict[ag].actor.Name());
                                    airGroupInfoDict[ag].groupedAirGroups = grouped;  //the airgroups in this grouping
                                    //Console.WriteLine("groupAllAircraft: a4.5");
                                    if (ag != airGroup)
                                    {
                                        airGroupInfoDict[ag].leader = airGroup;
                                        airGroupInfoDict[ag].isLeader = false;
                                        if (CurrentAGGroupLeaders[army].Contains(ag)) CurrentAGGroupLeaders[army].Remove(ag); //Make sure we have but one leader for the group & it has 
                                    }
                                }
                                //Console.WriteLine("groupAllAircraft: a4.6");
                                DoneAGgrouped.Add(ag);
                                //Console.WriteLine("groupAllAircraft: a4.7");
                            }
                        else
                        {
                            Console.WriteLine("groupAllAircraft ERROR: No AirGroup in the grouping - this should never happen!");
                        }
                        //Console.WriteLine("groupAllAircraft: a4.8");

                        //now remove any ags that were claimed by another previous leader
                        //airGroupInfoDict[airGroup].groupedAirGroups.Remove(ag);
                        //foreach (AiAirGroup ai2 in toremovefrom_gag) airGroupInfoDict[airGroup].groupedAirGroups.Remove(ai2);                            
                        foreach (AiAirGroup ai2 in toremovefrom_gag) grouped.Remove(ai2);
                        foreach (AiAirGroup ai3 in grouped) airGroupInfoDict[ai3].groupedAirGroups = grouped;  //grouped just change, so we re-add it to each AG in the group
                        //airGroupInfoDict[airGroup].groupedAirGroups.Remove(ai2);


                        //DoneAGgrouped.UnionWith(airGroupInfoDict[airGroup].nearbyAirGroups);

                    }

                    //Console.WriteLine("groupAllAircraft: 5");

                    //Now go through each group & calculate needed info such as # of aircraft in group.  Later we can add fancier ways of figuring group velocity, direction, altitude etc but for now we're
                    //just using the values from the primary aircraft of the group
                    if (CurrentAGGroupLeaders[army] != null) foreach (AiAirGroup airGroup in CurrentAGGroupLeaders[army])
                        {
                            //Console.WriteLine("groupAllAircraft: 6");
                            AirGroupInfo agid = airGroupInfoDict[airGroup];
                            int c = 0;
                            int cAboveRadar = 0;
                            int cBelowRadar = 0;
                            double aveAltAGL_ft = 0;
                            aiorhuman ah = aiorhuman.Human;
                            if (agid.isAI) ah = aiorhuman.AI;
                            double minAlt = agid.pos.z;
                            double maxAlt = agid.pos.z;
                            double aveAlt = 0;
                            Point3d avePos = new Point3d(0, 0, 0);
                            Point3d vwld = new Point3d(0, 0, 0);
                            string typeName = "";
                            string playerNames = "";
                            bool first = true;
                            string ids = "";



                            if (airGroupInfoDict[airGroup].groupedAirGroups != null) foreach (AiAirGroup ag in airGroupInfoDict[airGroup].groupedAirGroups)
                                {
                                    if (!airGroupInfoDict.ContainsKey(ag)) continue;
                                    AirGroupInfo agid2 = airGroupInfoDict[ag];
                                    c += airGroupInfoDict[ag].count;
                                    if (agid2.belowRadar) cBelowRadar += airGroupInfoDict[ag].count;
                                    else cAboveRadar += airGroupInfoDict[ag].count;
                                    if (agid2.pos.z > maxAlt) maxAlt = agid2.pos.z;
                                    if (agid2.pos.z < minAlt) minAlt = agid2.pos.z;
                                    aveAlt += agid2.pos.z * agid2.count;
                                    aveAltAGL_ft += agid2.altAGL_ft * agid2.count;

                                    if (!first)
                                    {
                                        playerNames += " - ";
                                        typeName += " - ";
                                        ids += " - ";
                                    }
                                    else first = false;


                                    playerNames += agid.playerNames;
                                    typeName += Calcs.GetAircraftType(agid2.actor as AiAircraft);
                                    ids += agid2.actor.Name();
                                    if (!agid2.isAI && (agid2.actor as AiAircraft).Player(0) != null) playerNames += (agid2.actor as AiAircraft).Player(0).Name() + " - ";
                                    vwld = new Point3d(vwld.x + (double)agid2.count * agid2.vel.x, vwld.y + (double)agid2.count * agid2.vel.y, vwld.z + (double)agid2.count * agid2.vel.z); //weight the direction vector by the # of aircraft in this airgroup
                                    avePos = new Point3d(avePos.x + (double)agid2.count * agid2.pos.x, avePos.y + (double)agid2.count * agid2.pos.y, avePos.z + (double)agid2.count * agid2.pos.z); //weight the direction vector by the # of aircraft in this airgroup



                                    if (airGroupInfoDict[ag].isAI)
                                    {
                                        if (ah == aiorhuman.Human) ah = aiorhuman.Mixed;
                                    }
                                    else if (ah == aiorhuman.AI) ah = aiorhuman.Mixed;

                                    //can do other calculations here such as averaging speed, altitude, direction, whatever
                                    //Figure out speed & direction  from actual travel time over last two radar measurements, etc.

                                }
                            agid.AGGcount = c;
                            agid.AGGisHeavyBomber = agid.isHeavyBomber;
                            agid.AGGpos = agid.pos;
                            agid.AGGtype = agid.type;
                            //agid.AGGvel = agid.vel;
                            agid.AGGvel = new Point3d(vwld.x / (double)c, vwld.y / (double)c, vwld.z / (double)c);  //The 'average' of the direction vectors
                            agid.AGGavePos = new Point3d(avePos.x / (double)c, avePos.y / (double)c, avePos.z / (double)c);  //The 'average' of the position vectors
                            agid.AGGminAlt_m = minAlt;
                            agid.AGGmaxAlt_m = maxAlt;
                            agid.AGGaveAlt_m = aveAlt / (double)c;
                            agid.AGGavealtAGL_ft = aveAltAGL_ft / (double)c;
                            agid.AGGcountBelowRadar = cBelowRadar;
                            agid.AGGcountAboveRadar = cAboveRadar;
                            agid.AGGtypeNames = typeName;
                            agid.AGGplayerNames = playerNames;
                            agid.AGGids = ids;
                            agid.AGGAIorHuman = ah;

                            agid.AGGmixupType = agid.AGGtype;

                            if (random.Next(21) == 1)
                            { //oops, sometimes we get mixed up on the type.  So sad . . .  See notes below about relative inaccuracy of early radar.
                                agid.AGGmixupType = "U";
                                //if (random.Next(3) == 1) agid.AGGmixupType = "B";
                            }

                            //So if we have the old mixupType, and it actually WAS mixed up, then it will have a high probability of staying mixed up for several cycles
                            if (aGICA[0] != null && aGICA[0].ContainsKey(airGroup))
                            {
                                AiAirGroup oldLeader = aGICA[0][airGroup].leader;
                                if (aGICA[0].ContainsKey(oldLeader)) {
                                    if (agid.AGGtype != aGICA[0][oldLeader].AGGmixupType && random.Next(4) < 3)
                                    {
                                        agid.AGGmixupType = aGICA[0][oldLeader].AGGmixupType;
                                    }


                                }
                            }

                            //Calculate average speed based on actual movement over time rather than instantaneous current movement, if possible
                            //Only do this in the case the current AG leader was also leader for the past three periods  
                            //using index 4 seems too much, results are quite discrepant from the 'old' style plotting table.  Maybe just use the one a/c's vel, or the average vel
                            /*
                            int aGICAindex = 4;
                            bool useAGICA = false;
                            for (int i = 0; i <= aGICAindex; i++)
                                {
                                    if (aGICA[i] != null && aGICA[i].ContainsKey(airGroup) && aGICA[i][airGroup].leader == airGroup) useAGICA = true;
                                    else
                                    {
                                        useAGICA = false;
                                        break;
                                    }
                                }                      
                            //if (a3 != null && a3.ContainsKey(airGroup) && a2 != null && a2.ContainsKey(airGroup) && a1 != null && a1.ContainsKey(airGroup) &&
                                     a1[airGroup].leader == airGroup && a2[airGroup].leader == airGroup && a3[airGroup].leader == airGroup) 
                            if (useAGICA)
                                {
                                    //Point3d p1 = aGICA[aGICAindex][airGroup].AGGavePos; //This would be using the average position of the entire airgroup
                                    //Point3d p2 = agid.AGGavePos; 
                                    Point3d p1 = aGICA[aGICAindex][airGroup].pos; //instead we'll use this one aircraft's ACTUAL position, which might be more consistent
                                    Point3d p2 = agid.pos; 
                                    double timeDiff = Time.current() - aGICA[aGICAindex][airGroup].time;
                                    Point3d vel2 = new Point3d ((p2.x-p1.x)/timeDiff, (p2.y - p1.y) / timeDiff, 0 );
                                    //Console.WriteLine("AveVel vs Vel2: {0:0} {1:0} {2:0} {3:0} {4:0} {5:0} ", agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z, vel2.x, vel2.y, vel2.z);
                                    agid.AGGvel = vel2;                                
                                }
                        */




                            agid.AGGradarDropout = false;

                            int dropoutValue = 14;
                            if (agid.AGGcount > 5) dropoutValue = Convert.ToInt32(11 * agid.AGGcount * agid.AGGcount / 25);
                            //Some radar returns vanish for no particular reason.  Random bug in the system, flock of seagulls, whatever
                            if (random.Next(dropoutValue) == 1)
                            {
                                agid.AGGradarDropout = true;
                                //Console.WriteLine("RG: AGGradarDropout due to random 1/7 {0} {1} {2}", agid.actor.Name(), agid.AGGcount, dropoutValue);
                            }

                            //Small groups of human heavy bombers are more likely to disappear from radar, in order to give them more of a fighting chance
                            //So bombers will drop out 1/7 and the amount indicated below.  Tried dropout 3/4 of the time but that leaves only 3/4*6/7 that
                            //they would show up, which means they didn't show up hardly at all. Around 50% for heavy bomber might be OK, means they
                            //show up like 40% of the time?  This applies to 1-2 bombers.  3-4-5 bombers also drop out some but less so as the bomber group size grows
                            if ((agid.AGGAIorHuman == aiorhuman.Human) && agid.AGGisHeavyBomber && agid.AGGcount <= 2 && random.Next(100) <= 50)  //2018-10-24, was 58, trying it lower.  10-25, 42 was worse, trying 67 instead. 67 seems to basically make them disappear, trying 50 instead.
                            {
                                agid.AGGradarDropout = true;
                                //Console.WriteLine("RG: AGGradarDropout due to HeavyBomber random 47% {0}", agid.actor.Name());
                            }
                            else if ((agid.AGGAIorHuman == aiorhuman.Human) && agid.AGGisHeavyBomber && agid.AGGcount <= 5 && agid.AGGcount > 2 && random.Next(100) <= (50 - 10 * (agid.AGGcount - 2)))  //2018-10-24, was 58, trying it lower.  10-25, 42 was worse, trying 67 instead.  Now 50, same as above.
                            {
                                agid.AGGradarDropout = true;
                                //Console.WriteLine("RG: AGGradarDropout due to HeavyBomber random 47% {0}", agid.actor.Name());
                            }




                            airGroupInfoDict[airGroup] = agid;

                            //Console.WriteLine("Airgroup Grouping: {0} {1} {2} {3:0} {4:0} {5:0} ", agid.actor.Name(), agid.count, agid.AGGcount, agid.AGGcountAboveRadar, agid.AGGcountBelowRadar, agid.AGGids);
                            //agid.AGGpos.x, agid.AGGpos.y, agid.AGGpos.z, agid.AGGtype, ah, agid.AGGisHeavyBomber, agid.AGGavePos.x, agid.AGGavePos.y,agid.AGGaveAlt_m, agid.AGGmaxAlt_m, agid.AGGids, agid.AGGplayerNames, agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z);
                        }



                }

            }
            //Console.WriteLine("groupAllAircraft: 7");
            airGroupInfoCircArr.Push(airGroupInfoDict);  //We save the last ~4 iterations of infodict on a circular array, so that we can go back & look @ what airgroups/leaders were doing in the last few minutes
        } catch (Exception ex)
        { Console.WriteLine("GroupAirgroups ERROR: {0}", ex); }
    }

    public string showGiantSectorOverview(Player player = null, int army = 0, bool display = true, bool html = false)
    {

        double delay = 22;

        Player[] to = null;
        if (player != null) to = new Player[] { player };

        string newline = Environment.NewLine;
        if (html) newline = "<br>" + Environment.NewLine;
        string retmsg = "";

        if (display) Timeout(0.4, () => twcLogServer(to, "Requesting Map Overview summary from headquarters, please stand by . . . ", null));


        string msg = "***Schematic Map Overview of Enemy Activity***";
        retmsg += msg + newline;


        string msg2 = "Airgroups:Aircraft in each Large Map Keypad Area";
        retmsg += msg2 + newline;


        string msg3 = "For more details, ask your Commander or Radar Operator to consult the Contact Plotting Table - or simply patrol the area, use Tab-4-1";
        retmsg += msg3 + newline;

        /*for (int i = 1; i < 10; i++)
        {
            string tild = "~";
            if (GiantSectorOverview[player.Army()][i, 1] == 0) tild = "";

            twcLogServer(new Player[] { player }, "Sector {0}: {1} enemy airgroups, {2}{3} aircraft",new object [] { i, GiantSectorOverview[player.Army()][i, 0], tild, GiantSectorOverview[player.Army()][i, 1] });
        }*/
        //Console.WriteLine("Giant: " + GiantSectorOverview.ToString());
        Timeout(delay, () =>
        {
            if (display)
            {
                twcLogServer(to, msg, null);
                twcLogServer(to, msg2, null);
                twcLogServer(to, msg3, null);
            }
            for (int i = 2; i > -1; i--)
            {
                string msg4 = string.Format("{0:D3}:{1:D3} {2:D3}:{3:D3} {4:D3}:{5:D3} ",
                    GiantSectorOverview[army][i * 3 + 1, 0], GiantSectorOverview[army][i * 3 + 1, 1],
                    GiantSectorOverview[army][i * 3 + 2, 0], GiantSectorOverview[army][i * 3 + 2, 1],
                    GiantSectorOverview[army][i * 3 + 3, 0], GiantSectorOverview[army][i * 3 + 3, 1]
                    );

                retmsg += msg4 + newline;
                if (display) twcLogServer(to, msg4, null);
            }
        });
        return retmsg;
    }

    /************************************************
         * Get radar returns for each AI aircraft group
         * Can be used to, ie, reprogram the flight plans for aiairgroups so they intercept any
         * enemies in their area
         * Recursive function called every X seconds
         ************************************************/


    public void aiAirGroupRadarReturns_recurs()
    {


        Timeout(127, () => { aiAirGroupRadarReturns_recurs(); });
        //Console.WriteLine("groupAllAircraft: -1");
        Task.Run(() => aiAirGroupRadarReturns());
        //aiAirGroupRadarReturns();


    }

    public void aiAirGroupRadarReturns()
    {
        Dictionary<AiAirGroup, AirGroupInfo> airGroupInfoDict = airGroupInfoCircArr.Get(0); //Most recent iteration of airgroup groupings
        Console.WriteLine("AIAGRR: Checking radar returns for AI groups");

        if (airGroupInfoDict != null) foreach (AiAirGroup airGroup in airGroupInfoDict.Keys)
            {
                AirGroupInfo agi = airGroupInfoDict[airGroup];
                if (!agi.isAI || agi.type != "F") continue; //we're only doing aiaircraft here, and fighters

                //Console.WriteLine("AIAGRR: Checking radar returns for " + agi.playerNames);
                listPositionAllAircraft(player: null, playerArmy: airGroup.getArmy(), inOwnArmy: false, radar_realism: RADAR_REALISM, aiairgroup: airGroup);
            }

    }

    public class AiAirGroupRadarInfo : IAiAirGroupRadarInfo
    {
        public double time { get; set; } //Battle.time.current;
        //public SortedDictionary<string, AirGroupInfo> interceptList {get; set;}
        public IAirGroupInfo agi { get; set; } //airgroup info for TARGET airgroup
        public IAirGroupInfo pagi { get; set; } //airgroup info for SOURCE airgroup (ie the 'player' or the one that will be targeting the TARGET
        public Point3d interceptPoint { get; set; } //intcpt, with x,y as location and z as intcpt time in seconds
        public bool climbPossible { get; set; } //climb_possible
        public AMission mission { get; set; }


        public AiAirGroupRadarInfo(Mission msn, IAirGroupInfo AGI, IAirGroupInfo PAGI, Point3d InterceptPoint, bool ClimbPossible, double tm = 0)
        {
            //interceptList = InterceptList;
            mission = msn;
            if (tm != 0) time = tm;
            else time = mission.Time.current();
            interceptPoint = InterceptPoint;
            climbPossible = ClimbPossible;
            agi = AGI;
            pagi = PAGI;
        }
    }
    //returns TRUE if player is off the radar, either by being too low or in an area where the radar is out.
    public bool offRadar(Player player)
    {

        if (player.Place() == null || player.Army() == null || !MO_isRadarEnabledByArea(player.Place().Pos(), radarArmy: player.Army())) return true;
        if (belowRadar(player)) return true;
        return false;
    }
    public bool belowRadar(Player player)
    {
        if (player.Place() != null && (player.Place() as AiAircraft) != null) return belowRadar(player.Place() as AiAircraft);
        else return true;
    }

    public bool belowRadar(AiAircraft aircraft = null)
    {
        if (aircraft == null) return true;
        AiAirGroup airGroup = aircraft.AirGroup();
        double vel_mps = Calcs.CalculatePointDistance(airGroup.Vwld());
        double altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0); // I THINK (?) that Z_AltitudeAGL is in meters?
        double altAGL_ft = Calcs.meters2feet(altAGL_m);
        //Console.WriteLine("belowradar: " + altAGL_ft + " " + vel_mps);
        return belowRadar(altAGL_ft, vel_mps, airGroup, aircraft);
    }

    //unified location to determine if an a/c is low enough to drop off the radar
    //TRUE if off the radar, false otherwise
    public bool belowRadar(double altAGL_ft, double vel_mps, AiAirGroup airGroup = null, AiAircraft aircraft = null)
    {
        //So mostly flying "below radar" is quite safe and undetected.  But in some cases there will be a detection, either because radar got a return somehow, or (far
        //more likely) the aircraft was spotted by an observer or some other way.  So this type of thing is far more likely to happen when over
        //enemy ground and far, far more  likely when near an enemy objective, which is likely to be well guarded etc.
        bool onEnemyGround = false;
        bool nearMissionObjective = false;
        bool isAI = false;
        if (aircraft != null && (aircraft as AiActor != null))
        {
            bool onEnemyTerritory = false;
            int terr = GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y);

            if ((terr == 1 || terr == 2) && (aircraft as AiActor).Army() != terr) onEnemyTerritory = true;

            maddox.game.LandTypes landType = GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y);
            if (onEnemyTerritory && landType != maddox.game.LandTypes.WATER) onEnemyGround = true;

            int numMissionObjectivesNear = 0;

            if (onEnemyGround && MO_MissionObjectivesNear(aircraft.Pos(), dist_m: 8000) > 1) nearMissionObjective = true;

            isAI = isAiControlledPlane2(aircraft);

        }

        if (isAI) altAGL_ft = altAGL_ft - 350; //AI aircraft just can't fly below a certain altitude.  So we're giving them a break as far as disappearing from radar if they're low.

        double leakageRate = .21;  //was .12, seemed too low
        double below250LeakageRate = 0.105;

        if (onEnemyGround)
        {
            leakageRate = .31;  //was .24, seemed too low
            below250LeakageRate = 0.19;
        }

        if (nearMissionObjective)
        {
            leakageRate = .7;//was .64, seemed too low
            below250LeakageRate = 0.51;
        }

        bool below = ((altAGL_ft < 500 && altAGL_ft - 325 < random.Next(175)) || //Less then 400 ft AGL they start to phase out from radar     
                                                                                 //(dis_mi < 10 && altAGL_ft < 400 && altAGL_ft < random.Next(500)) || //Within 10 miles though you really have to be right on the deck before the radar starts to get flakey, less than 250 ft. Somewhat approximating 50 foot alt lower limit.
        (altAGL_ft < 350)); //And, if they are less than 350 feet AGL, they are gone from radar.  Except for a bit of leakage.

        //So, 80% of the time we cloak them if below radar, but 20% it somehow leaks out anyway . . .
        if (altAGL_ft < 20 && vel_mps < 20 && random.NextDouble() < 0.9 && Stb_distanceToNearestAirport(aircraft as AiActor) < 2500) return false; //So airplanes on the ground, taxiing at airport, etc, are picked up.  This isn't radar per se but intelligence or intercepts of radio chatter & other comms giving indications of future movements & where they are happening.
        if (random.NextDouble() < (1 - leakageRate) || (altAGL_ft < 250 && random.NextDouble() < (1 - below250LeakageRate))) return below;
        else return false;  //so, sometimes, 20% of the time or 5% of the time below 100 ft AGL, aircraft below radar elevation show up somehow, leakage probably, or maybe an observer spotted them
    }

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
    // playerArmy -4 is for TOPHAT BUT GROUPED & will also list all a/c but shown as grouped and is for ADMINS listing all kinds of details etc vs the red/blue TOPHAT which is more filtered to simulate real WWII radar

    public void listPositionAllAircraft(Player player, int playerArmy, bool inOwnArmy, int radar_realism = -10000000, AiAirGroup aiairgroup = null, bool disp = true)
    {
        //try
        {

            if (radar_realism == -10000000) radar_realism = RADAR_REALISM;
            DateTime d = DateTime.UtcNow;
            // int radar_realism;     //realism = 0 gives exact position, bearing, velocity of each a/c.  We plan to make various degrees of realism ranging from 0 to 10.  Implemented now is just 0=exact, >0 somewhat more realistic    
            // realism = -1 gives the lat/long output for radar files.

            Dictionary<AiAirGroup, AirGroupInfo> airGroupInfoDict = airGroupInfoCircArr.Get(0); //Most recent iteration of airgroup groupings

            bool display = true;

            if (disp == false || (player == null && aiairgroup != null)) display = false; //don't display for aiairgroup calcs

            string posmessage = "";
            int poscount = 0;
            int totalcount = 0;
            int belowradarcount = 0;
            AiAircraft p = null;
            AiActor pa = null;

            AirGroupInfo padig = null;

            Point3d pos1;
            Point3d pos2;
            Point3d VwldP, intcpt, longlat;
            Point3d ppos = new Point3d(0, 0, 0);
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
            bool playerOffRadar = true;
            string enorfriend;
            long currtime_ms = 0;
            long storedtime_ms = -1;
            bool savenewmessages = true;
            Tuple<long, SortedDictionary<string, string>> message_data;
            SortedDictionary<string, string> radar_messages =
                new SortedDictionary<string, string>(new ReverseComparer<string>()); //reverse sorted so "best" messages come last on the list players receive
            SortedDictionary<string, IAiAirGroupRadarInfo> ai_radar_info =
                new SortedDictionary<string, IAiAirGroupRadarInfo>(); //NOT reverse sorted so on the list of AI radar returns, the best messages come first
            //string [] radar_messages, radar_messages_index;             
            int wait_s = 0;
            long refreshtime_ms = 0;
            //There is now a delay BUILT IN to the airgroup grouping system, 0-30 seconds (15 sec on average).
            //So we cut our required wait time here from 20 sec to 5 seconds.  We don't have to wait for them
            //to be stale--they're ALREADY stale!  Plus, we use the player's position, velocity, etc
            //from the radar DB as well as other a/c location/velocity, so it' ALL somewhat stale.
            if (radar_realism >= 1) { wait_s = 1; refreshtime_ms = 60 * 1000; }
            if (radar_realism >= 5) { wait_s = 10; refreshtime_ms = 2 * 60 * 1000; }
            //if (radar_realism >= 5) { wait_s = 5; refreshtime_ms = Convert.ToInt32 (0.02 * 60 * 1000); } //FOR TESTING - MUCH FASTER
            if (radar_realism >= 9) { wait_s = 30; refreshtime_ms = 5 * 60 * 1000; }

            //if admin==true we'll list ALL aircraft regardless of position, radars out et.
            //admin==false we start filtering out depending on whether a radar station has been knocked out etc
            bool admin = false;
            if ((radar_realism == 0) || (radar_realism == -1 && playerArmy == -3) || (radar_realism == -1 && playerArmy == -4)) admin = true;

            //RadarArmy is the army/side for which the radar is being generated.
            //Some radar areas will be out for Red but still active for Blue.  Blue radar might have some inherent restrictions Red radar doesn't, etc. 
            //if radarArmy==0 it will be more an admin radar that ignores these restrictions
            int radarArmy = Math.Abs(playerArmy);
            if (radarArmy > 2) radarArmy = 0;

            //Zero out the GiantSectorOverview totals (BEFORE starting any loops involving them, dur)
            if (playerArmy == -1 || playerArmy == -2) GiantSectorOverview[-playerArmy] = new int[10, 2]; //A simple count of how many enemy airgroups & aircraft in this sector
            if (playerArmy == -4) GiantSectorOverview[0] = new int[10, 2]; //both sides, for admin purposes (army = 0)

            //wait_s = 1; //for testing
            //refreshtime_ms = 1 * 1000;

            enorfriend = "ENEMY";
            if (inOwnArmy) enorfriend = "FRIENDLY";
            if (playerArmy < 0) enorfriend = "BOTH ARMIES";


            try
            {
                if (player != null && (player.Place() is AiAircraft))
                {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
                    p = player.Place() as AiAircraft;
                    pa = player.Place();

                    if (airGroupInfoDict != null && airGroupInfoDict.ContainsKey(p.AirGroup()))
                    {
                        padig = airGroupInfoDict[p.AirGroup()];
                        player_Vwld = new Vector3d(padig.vel.x, padig.vel.y, padig.vel.z);
                        player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                        player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                        player_alt_m = padig.pos.z;
                        // player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                        //player_sector = player_sector.Replace(",", ""); // remove the comma 
                        player_sector = Calcs.correctedSectorName(this, padig.pos);
                        player_place_set = true;
                        playername = player.Name();
                        playertype = padig.type;

                        ppos = padig.pos;

                    }
                    else
                    {


                        player_Vwld = p.AirGroup().Vwld();
                        player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                        player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                        player_alt_m = p.Pos().z;
                        // player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                        //player_sector = player_sector.Replace(",", ""); // remove the comma 
                        player_sector = Calcs.correctedSectorName(this, p.Pos());
                        player_place_set = true;
                        playername = player.Name();
                        playertype = p.Type().ToString();
                        if (playertype.Contains("Fighter") || playertype.Contains("fighter")) playertype = "F";
                        else if (playertype.Contains("Bomber") || playertype.Contains("bomber")) playertype = "B";
                        else playertype = "U";

                        ppos = p.Pos();
                    }

                    string posmessageCSP;
                    string speedMsg = player_vel_mph.ToString("F0") + "mph ";
                    if (playerArmy == 2) speedMsg = (player_vel_mps * 3.6).ToString("F0") + "km/h ";
                    playerOffRadar = offRadar(player);

                    posmessageCSP = "Radar intercepts are based on your speed/position as last detected at the command center: ";
                    if (playerOffRadar) posmessageCSP += "Unknown!";
                    else posmessageCSP += speedMsg + player_sector.ToString();

                    gpLogServerAndLog(new Player[] { player }, posmessageCSP, null);



                }
                else if (player == null && aiairgroup != null) //The case where we are asking for radar returns for an AIAirgroup
                {
                    if (airGroupInfoDict != null && airGroupInfoDict.ContainsKey(aiairgroup))
                    {
                        padig = airGroupInfoDict[aiairgroup];
                        if (aiairgroup.GetItems().Length > 0) pa = aiairgroup.GetItems()[0];
                        else { Console.WriteLine("AIAGRRR: No a/c in this airgroup, returning"); return; }
                        p = pa as AiAircraft;
                        player_Vwld = new Vector3d(padig.vel.x, padig.vel.y, padig.vel.z);
                        player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                        player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                        player_alt_m = padig.pos.z;
                        // player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                        //player_sector = player_sector.Replace(",", ""); // remove the comma 
                        player_sector = Calcs.correctedSectorName(this, padig.pos);
                        player_place_set = true;
                        playername = padig.playerNames; //the actor.Name() in case of ai groups
                        playertype = padig.type;

                        ppos = padig.pos;

                    }
                    else
                    {
                        return;  //if we're requesting aiairgroup contacts & they're not in the aiGroupInfoDict yet, we'll just skip it
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Radar ERROR5: " + ex.ToString());
            }

            playername_index = playername + "_0";
            if (inOwnArmy) playername_index = playername + "_1";
            playername_index = playername_index + "_" + radar_realism.ToString();
            if (radar_realism < 0) playername_index = playername_index + "_" + playerArmy.ToString();

            savenewmessages = true; //save the messages that are generated
            currtime_ms = stopwatch.ElapsedMilliseconds;

            try {
                //If the person has requested a new radar return too soon, just repeat the old return verbatim
                //We have 3 cases:
                // #1. ok to give new radar return
                // #2. Too soon since last radar return to give a new one
                // #3. New radar return is underway but not finished, so don't give them a new one. 
                if (radar_realism > 0 && radar_messages_store.TryGetValue(playername_index, out message_data) && display && player != null)
                {
                    long time_elapsed_ms = currtime_ms - message_data.Item1;
                    long time_until_new_s = (long)((refreshtime_ms - time_elapsed_ms) / 1000);
                    long time_elapsed_s = (long)time_elapsed_ms / 1000;
                    radar_messages = message_data.Item2;
                    if (time_elapsed_ms < refreshtime_ms || message_data.Item1 == -1)
                    {
                        string posmessageIP;
                        if (message_data.Item1 == -1) posmessageIP = "New radar returns are in process.  Your previous radar return:";
                        else posmessageIP = time_until_new_s.ToString("F0") + "s until " + playername + " can receive a new radar return.  Your previous radar return:";
                        gpLogServerAndLog(new Player[] { player }, posmessageIP, null);

                        Console.WriteLine("RAD: Giving old message to " + playername_index);

                        wait_s = 0;
                        storedtime_ms = message_data.Item1;
                        savenewmessages = false; //don't save the messages again because we aren't generating anything new


                        //Wait just 2 seconds, which gives people a chance to see the message about how long until they can request a new radar return.
                        Timeout(2, () =>
                        {
                            double delay = 0;
                            //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               
                            foreach (var mess in message_data.Item2)
                            {


                                delay += 0.2;
                                Timeout(delay, () =>
                                {
                                    if (radar_realism == 0) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                                    else gpLogServerAndLog(new Player[] { player }, mess.Value, null);
                                });

                            }
                        });
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Radar ERROR4: " + ex.ToString());
            }

            //If they haven't requested a return before, or enough time has elapsed, give them a new return  
            if (savenewmessages)
            { //try
                {
                    //When we start to work on the messages we save current messages (either blank or the previous one that was fetched from radar_messages_store)
                    //with special time code -1, which means that radar returns are currently underway; don't give them any more until finished.
                    radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(-1, radar_messages);

                    if (radar_realism > 0 && display) twcLogServer(new Player[] { player }, "Fetching radar contacts, please stand by . . . ", null);




                    radar_messages = new SortedDictionary<string, string>(new ReverseComparer<string>());//clear it out before starting anew . . .           
                    radar_messages.Add("9999999999", " >>> " + enorfriend + " RADAR CONTACTS <<< ");

                    if (radar_realism < 0) radar_messages.Add("9999999998", "p" + Calcs.GetMD5Hash(radarpasswords[playerArmy].ToUpper())); //first letter 'p' indicates passward & next characters up to space or EOL are the password.  Can customize this per  type of return, randomize each mission, or whatever.
                                                                                                                                           //radar_realism < 0 is our returns for the online radar screen, -1 = red returns, -2 = blue returns, -3 = admin (ALL SEEING EYE) returns
                                                                                                                                           //passwords are CASEINSENSITIVE and the MD5 of the password is saved in the -radar.txt file for red, blue, and admin respectively



                    //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
                    if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
                    {
                        foreach (int army in GamePlay.gpArmies())
                        {
                            if ((playerArmy == -3 && radar_realism == -1) || radar_realism == 0) //case of admin TOPHAT OR admin in-game <pos, we list every a/c with no grouping
                                                                                                 //TODO: lots of dead code below now because this used to handle the cases of ALL playerArmy & ALL radar_realism types, before grouping.  This can be cleaned up now.
                            {
                                //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0 && (!inOwnArmy ^ (army == playerArmy)))
                                {
                                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                                    {
                                        posmessage = "";
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
                                                    if (!MO_isRadarEnabledByArea(a.Pos(), admin, radarArmy)) break; //breaks us out of this airGroup

                                                    if (!player_place_set)
                                                    {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"                                                                        
                                                        p = actor as AiAircraft;
                                                        player_Vwld = p.AirGroup().Vwld();
                                                        player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                                                        player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                                                        player_alt_m = p.Pos().z;
                                                        /* player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                                                        player_sector = player_sector.Replace(",", ""); // remove the comma */
                                                        player_sector = Calcs.correctedSectorName(this, p.Pos());
                                                        ppos = p.Pos();
                                                        player_place_set = true;
                                                    }

                                                    bool isAI = isAiControlledPlane2(a);

                                                    string acType = Calcs.GetAircraftType(a);
                                                    isHeavyBomber = false;
                                                    if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkIV")) isHeavyBomber = true;

                                                    type = a.Type().ToString();
                                                    if (type.Contains("Fighter") || type.Contains("fighter")) type = "F";
                                                    else if (type.Contains("Bomber") || type.Contains("bomber")) type = "B";
                                                    if (a == p && radar_realism >= 0) type = "Your position";
                                                    /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                                     
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
                                                    /* intcpt_sector = GamePlay.gpSectorName(intcpt.x, intcpt.y).ToString();
                                                    intcpt_sector = intcpt_sector.Replace(",", ""); // remove the comma */
                                                    intcpt_sector = Calcs.correctedSectorName(this, intcpt);
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
                                                    if (radar_realism < 0)
                                                    {

                                                        string numContacts = poscount.ToString();
                                                        string aircraftType = Calcs.GetAircraftType(a);
                                                        string vel = vel_mph.ToString("n0");
                                                        string alt = alt_angels.ToString("n0");
                                                        string he = heading.ToString("F0");


                                                        bool first = true;
                                                        string aplayername = "";

                                                        //if (a.Player(0) != null && a.Player(0).Name() != null) aplayername = a.Player(0).Name(); //old way, only gets one player and only if in the pilot seat

                                                        HashSet<string> namesHS = new HashSet<string>();
                                                        for (int i = 0; i < a.Places(); i++)
                                                        {
                                                            if (a.Player(i) != null && a.Player(i).Name() != null && !namesHS.Contains(a.Player(i).Name()))
                                                            {
                                                                if (!first) aplayername += " - ";
                                                                aplayername += a.Player(i).Name();
                                                                namesHS.Add(a.Player(i).Name());
                                                                first = false;

                                                            }
                                                        }

                                                        //aplayername = agid.AGGplayerNames;

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
                                                                       belowRadar(altAGL_ft, vel_mps, airGroup, a) || //unified method for deciding below radar or not
                                                                       ((!isAI && isHeavyBomber) && poscount <= 2 && random.Next(3) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out 
                                                                                                                                             //However if the player heavy bombers group up they are MUCH more likely to show up on radar.  But they will still be harder than usual to track because each individual bomber will phase in/out quite often

                                                                       (random.Next(7) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                                       )
                                                       ) { posmessage = ""; }


                                                    }
                                                    else if (radar_realism == 0)
                                                    {
                                                        posmessage = poscount.ToString() + type + " " +

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
                                                        //twcLogServer(new Player[] { player }, posmessage, new object[] { });
                                                    }
                                                    else if (radar_realism > 0)
                                                    {

                                                        //dropoutValue used below to randomly drop some readings from the radar.  As group gets larger the chance of dropouts goes to nil.
                                                        int dropoutValue = 7;
                                                        if (poscount > 5) dropoutValue = Convert.ToInt32(7 * poscount ^ 2 / 25);


                                                        //Trying to give at least some semblance of reality based on capabilities of Chain Home & Chain Home Low
                                                        //https://en.wikipedia.org/wiki/Chain_Home
                                                        //https://en.wikipedia.org/wiki/Chain_Home_Low
                                                        if (random.Next(8) == 1)
                                                        { //oops, sometimes we get mixed up on the type.  So sad . . .  See notes below about relative inaccuracy of early radar.
                                                            type = "F";
                                                            if (random.Next(3) == 1) type = "B";
                                                        }
                                                        if (dis_mi <= 2 && a != p && Math.Abs(player_alt_m - a.Pos().z) < 5000)
                                                        {
                                                            posmessage = type + " nearby";
                                                        }
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
                                                                 (dis_mi >= 10 && altAGL_ft < 650 && altAGL_ft < random.Next(500)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                                                                                                      //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                                                                                                      //phasing out targets below 250 feet.

                                                                belowRadar(altAGL_ft, vel_mps, airGroup, a) || //unified method for deciding below radar or not
                                                                 ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi > 11 && poscount <= 2 && random.Next(4) <= 2) || // Breather bombers have a higher chance of being overlooked/dropping out, especially when further away.  3/4 times it doesn't show up on radar.
                                                                 ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi <= 11 && poscount <= 2 && random.Next(5) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out when close (this is close enough it should be visual range, so we're not going to help them via radar)
                                                                                                                                                                             //((!isAI && type == "B" && army != playerArmy) && random.Next(5) > 0) || // Enemy bombers don't show up on your radar screen if less than 7 miles away as a rule - just once in a while.  You'll have to spot them visually instead at this distance!
                                                                                                                                                                             //We're always showing breather FIGHTERS here (ie, they are not included in isAI || type == "B"), because they always show up as a group of 1, and we'd like to help them find each other & fight it out
                                                                 (random.Next(dropoutValue) == 1)  //it just malfunctions & shows nothing 1/dropoutValue of the time, for no reason, because. Early radar wasn't 100% reliable at all. dropoutValue is 7 for small groups & increases rather quickly as the groups get larger, so that large groups don't drop out so much
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
                                            //twcLogServer(new Player[] { player }, "RPT: " + posmessage + posmessage.Length.ToString(), new object[] { });
                                            if (posmessage.Length > 0)
                                            {
                                                //gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                                //Console.WriteLine("ADMIN: ~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                                //We add the message to the list along with an index that will allow us to reverse sort them in a logical/useful order                               
                                                int intcpt_time_index = (int)intcpt_time_min;
                                                if (intcpt_time_min <= 0 || intcpt_time_min > 99) intcpt_time_index = 99;

                                                try
                                                {
                                                    string addMess = posmessage;
                                                    if (radar_realism > 0) addMess = "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + posmessage;
                                                    radar_messages.Add(
                                                       ((int)intcpt_time_index).ToString("D2") + ((int)dis_mi).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                                       addMess
                                                    );
                                                }
                                                catch (Exception e)
                                                {
                                                    Console.WriteLine("RadError: " + e);
                                                }

                                            }
                                        }
                                    }

                                }
                            }
                            //Using our GROUPED AirGroups instead of chunking through each individual aircraft
                            //This should apply to Red & Blue TOPHAT plus all in-game player radar (tab-7-1 etc)
                            else
                            {
                                try
                                {
                                    //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                                    if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0 && (!inOwnArmy ^ (army == playerArmy)))
                                    {
                                        //if (CurrentAGGroupLeaders[army] != null) foreach (AiAirGroup airGroup in CurrentAGGroupLeaders[army])
                                        if (airGroupInfoDict != null) foreach (AiAirGroup airGroup in airGroupInfoDict.Keys)
                                            {
                                                posmessage = "";

                                                //Console.WriteLine("LPAA: Processing ag: PA{0} {1} {2} ", playerArmy, airGroup.getArmy(), airGroup.NOfAirc);
                                                AirGroupInfo agid = airGroupInfoDict[airGroup];
                                                if (agid.actor.Army() != army) continue;
                                                if (!agid.isLeader) continue;

                                                //Console.WriteLine("LPAA: Processing ag: PA{0} {1} {2} ", playerArmy, agid.actor.Army(), agid.actor.Name());

                                                aigroup_count++;
                                                /*
                                                AirGroupInfo agid = new AirGroupInfo();
                                                if (airGroupInfoDict != null && airGroupInfoDict.ContainsKey(airGroup))
                                                {
                                                    agid = airGroupInfoDict[airGroup];
                                                }
                                                else continue; //this shouldn't happen except at startup etc.  If it does we want to continue to try each succeeding airgroup, hopefully some of them will work
                                                */

                                                AiActor actor = agid.actor;
                                                AiAircraft a = actor as AiAircraft;
                                                //if (!player_place_set &&  (a.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
                                                totalcount = agid.AGGcount;
                                                poscount = agid.AGGcountAboveRadar;
                                                belowradarcount = agid.AGGcountBelowRadar;

                                                //Check on any radar outages or restrictions for each army, and remove any radar returns from areas where radar is restricted or inoperative
                                                if (!MO_isRadarEnabledByArea(agid.AGGavePos, admin, radarArmy)) continue;

                                                //Console.WriteLine("LPAA: Processing ag2: PA{0} {1} {2} ", playerArmy, airGroup.getArmy(), airGroup.NOfAirc);

                                                if (!player_place_set)
                                                {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"                                                                        
                                                    p = actor as AiAircraft;
                                                    player_Vwld = p.AirGroup().Vwld();
                                                    player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                                                    player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                                                    player_alt_m = p.Pos().z;
                                                    /* player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                                                    player_sector = player_sector.Replace(",", ""); // remove the comma */
                                                    player_sector = Calcs.correctedSectorName(this, p.Pos());

                                                    ppos = p.Pos();

                                                    player_place_set = true;
                                                }

                                                bool isAI = (agid.AGGAIorHuman == aiorhuman.AI);

                                                string acType = agid.AGGtypeNames;
                                                isHeavyBomber = agid.AGGisHeavyBomber;


                                                type = agid.AGGtype;

                                                //if (a == p && radar_realism >= 0) type = "Your position";
                                                if (a == p && radar_realism >= 0) continue; //the player is in the DB and we don't want/need to give an intercept to self-location a while ago, as shown in the DB.
                                                                                            /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                                                                             
                                                                                             + a.Type() + " " 
                                                                                             + a.TypedName() + " " 
                                                                                             +  a.AirGroup().ID(), new object[] { });
                                                                                            */
                                                pos1 = agid.AGGavePos;
                                                //Thread.Sleep(100);
                                                //pos2=a.Pos();
                                                //bearing=Calcs.CalculateGradientAngle (pos1,pos2);
                                                Vwld = new Vector3d(agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z);
                                                vel_mps = Calcs.CalculatePointDistance(Vwld);
                                                vel_mph = Calcs.meterspsec2milesphour(vel_mps);
                                                vel_mph_10 = Calcs.RoundInterval(vel_mph, 10);
                                                heading = (Calcs.CalculateBearingDegree(Vwld));
                                                heading_10 = Calcs.GetDegreesIn10Step(heading);
                                                dis_m = Calcs.CalculatePointDistance(agid.AGGavePos, p.Pos());
                                                dis_mi = Calcs.meters2miles(dis_m);
                                                dis_10 = (int)dis_mi;
                                                if (dis_mi > 20) dis_10 = Calcs.RoundInterval(dis_mi, 10);
                                                bearing = Calcs.CalculateGradientAngle(p.Pos(), agid.AGGavePos);
                                                bearing_10 = Calcs.GetDegreesIn10Step(bearing);

                                                longlat = Calcs.Il2Point3dToLongLat(agid.AGGavePos);

                                                //alt_km = agid.AGGavePos.z / 1000;
                                                //alt_ft = Calcs.meters2feet(agid.AGGavePos.z);
                                                alt_km = agid.AGGmaxAlt_m / 1000; //So rather than using average altitude, I think we must use MAX altitude of the group here. If not that, we should indicate a range or something
                                                alt_ft = Calcs.meters2feet(agid.AGGmaxAlt_m);
                                                //altAGL_m = (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0); // I THINK (?) that Z_AltitudeAGL is in meters?

                                                //We're using group leaders alt & AGL to get aveAGL for the entire group. Formula: AveAlt - (alt-AGL) = AveAGL
                                                //altAGL_m = agid.AGGaveAlt - (agid.pos.z - (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0)); // I THINK (?) that Z_AltitudeAGL is in meters?
                                                //altAGL_ft = Calcs.meters2feet(altAGL_m);
                                                altAGL_ft = agid.AGGavealtAGL_ft;
                                                altAGL_m = Calcs.feet2meters(altAGL_ft);

                                                alt_angels = Calcs.Feet2Angels(alt_ft);
                                                sector = GamePlay.gpSectorName(agid.AGGavePos.x, agid.AGGavePos.y).ToString();
                                                sector = sector.Replace(",", ""); // remove the comma
                                                VwldP = new Point3d(agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z);

                                                intcpt = Calcs.calculateInterceptionPoint(agid.AGGavePos, VwldP, ppos, player_vel_mps);
                                                intcpt_heading = (Calcs.CalculateGradientAngle(ppos, intcpt));
                                                intcpt_time_min = intcpt.z / 60;
                                                /* intcpt_sector = GamePlay.gpSectorName(intcpt.x, intcpt.y).ToString();
                                                intcpt_sector = intcpt_sector.Replace(",", ""); // remove the comma */
                                                intcpt_sector = Calcs.correctedSectorName(this, intcpt);
                                                intcpt_reasonable_time = (intcpt_time_min >= 0.02 && intcpt_time_min < 25);

                                                climb_possible = true;
                                                if (player_alt_m <= agid.AGGminAlt_m && intcpt_time_min > 1)
                                                {
                                                    double altdiff_m = agid.AGGminAlt_m - player_alt_m;
                                                    if (intcpt_time_min > 3 && altdiff_m / intcpt_time_min > 1300) { climb_possible = false; } //109 can climb @ a little over 1000 meters per minute in a sustained way.  So anything that requires more climb than that we exclude from the listing
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
                                                if (radar_realism < 0) //This applies to Red & Blue tophat, and Admin-Grouped Tophat which uses groupings.  Admin-regular tophat still uses individual per aircraft returns.
                                                {

                                                    string numContacts = poscount.ToString();
                                                    if (playerArmy == -4 && belowradarcount > 0) numContacts = poscount.ToString() + "+" + belowradarcount.ToString();  //for admin-grouping radar
                                                    string aircraftType = agid.AGGtypeNames;
                                                    string vel = vel_mph.ToString("n0");
                                                    string alt = alt_angels.ToString("n0");
                                                    string he = heading.ToString("F0");

                                                    string aplayername = agid.AGGplayerNames;
                                                    //if (isAI) aplayername = agid.AGGids;

                                                    //red & blue tophat operators only get an approximation of how many a/c in each group and also
                                                    //don't get perfect information about whether fighters or bombers or unknown
                                                    //and also don't get the EXACT type of a/c or the name of the player
                                                    if (playerArmy == -1 || playerArmy == -2)
                                                    {
                                                        numContacts = "~" + Calcs.NoOfAircraft(poscount).ToString("F0");

                                                        aircraftType = "";
                                                        aplayername = "";

                                                        vel = vel_mph_10.ToString("n0");
                                                        alt = alt_angels.ToString("n0");
                                                        he = heading_10.ToString("F0");

                                                    }


                                                    posmessage =
                                                    agid.AGGavePos.x.ToString()
                                                    + "," + agid.AGGavePos.y.ToString() + "," +
                                                    longlat.y.ToString()
                                                    + "," + longlat.x.ToString() + "," +
                                                    army.ToString() + "," +
                                                    agid.AGGmixupType + "," + //We use this to keep the (possibly mixed-up) a/c type standard among all types of radar, it is set once for all in agid instead of randomly set here for each individual radar sweep
                                                    he + "," +
                                                    vel + "," +
                                                    alt + "," +
                                                    sector.ToString() + "," +
                                                    numContacts + "," +
                                                    aircraftType.Replace(',', '_') + "," //Replace any commas since we are using comma as a delimiter here
                                                    + a.Name().GetHashCode().ToString() + "," //unique hashcode for each actor that will allow us to be able to identify it uniquely on the other end, without giving away anything about what type of actor it is (what type of aircraft, whether AI or live player, etc)
                                                    + aplayername.Replace(',', '_'); //Replace any commas since we are using comma as a delimiter here

                                                    if (playerArmy == -4)
                                                    {
                                                        GiantSectorOverview[0][agid.AGGgiantKeypad, 0]++; //A simple count of how many enemy airgroups (index = 0) in this sector
                                                        GiantSectorOverview[0][agid.AGGgiantKeypad, 1] += poscount; //A simple count of how many enemy aircraft (index =1) in this sector
                                                    }

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

                                                    //dropoutValue used below to randomly drop some readings from the radar.  As group gets larger the chance of dropouts goes to nil.
                                                    //int dropoutValue = 7;
                                                    //if (poscount > 5) dropoutValue = Convert.ToInt32(7 * poscount ^ 2 / 25);

                                                    //For red & blue TopHot operators we give a slightly filtered view of the contacts,
                                                    //simulating what actual WWII radar could see
                                                    //For example contacts low to the ground fade out.
                                                    //TODO: We could set up radar towers & contacts could be seen better if closer to the 
                                                    //tower (ie, lower to the ground.  But then if the enemy destroys your tower you lose
                                                    //that & can only see what your remaining towers can see (which will likely be contacts only quite high in altitude)
                                                    //Also each army could have its own towers giving it better visibility on its own side of the lines where its own towers
                                                    //are etc
                                                    if ((playerArmy == -1 || playerArmy == -2) && (
                                                                   belowRadar(altAGL_ft, vel_mps, airGroup, a) || //unified method for deciding below radar or not    
                                                                   poscount == 0 || //this happens if all a/c in a group are belowRadar
                                                                   agid.AGGradarDropout
                                                                   //((!isAI && isHeavyBomber) && poscount <= 2 && random.Next(3) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out 
                                                                   //However if the player heavy bombers group up they are MUCH more likely to show up on radar.  But they will still be harder than usual to track because each individual bomber will phase in/out quite often

                                                                   //(random.Next(dropoutValue) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                                   )
                                                   )
                                                    {
                                                        //Console.WriteLine("Radar: Dropping contact {5} from map {0} bec agl {1:0} bR {2} ct {3} AGGrD {4}", playerArmy, altAGL_ft, belowRadar(altAGL_ft), poscount, agid.AGGradarDropout, a.Name());

                                                        posmessage = "";
                                                    }


                                                    //add to the GiantSectorOverview ONLY IF their is a radar return for this airgroup
                                                    if ((playerArmy == -1 || playerArmy == -2) && posmessage.Length > 0)
                                                    {
                                                        //we put this here bec. we want to tally only those ag & a/c that show in that side's radar display
                                                        if ((-playerArmy) != army)
                                                        {
                                                            GiantSectorOverview[(-playerArmy)][agid.AGGgiantKeypad, 0]++; //A simple count of how many enemy airgroups (index = 0) in this sector
                                                            GiantSectorOverview[(-playerArmy)][agid.AGGgiantKeypad, 1] += Calcs.NoOfAircraft(poscount); //A simple count of how many enemy aircraft (index =1) in this sector
                                                                                                                                                        //Console.WriteLine("RADadding: " + agid.AGGgiantKeypad.ToString() + " " + (-playerArmy).ToString() + " " + GiantSectorOverview[(-playerArmy)][agid.AGGgiantKeypad, 0].ToString() + " " + posmessage);

                                                        }
                                                        else
                                                        {
                                                            //Console.WriteLine("RADNOTadding: " + agid.AGGgiantKeypad.ToString() + " " + (-playerArmy).ToString() + " " + posmessage);
                                                        }

                                                    }


                                                }


                                                else if (radar_realism == 1)
                                                {
                                                    posmessage = poscount.ToString() + type + " " +

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
                                                    //twcLogServer(new Player[] { player }, posmessage, new object[] { });
                                                }
                                                else if (radar_realism > 0)
                                                {

                                                    //Trying to give at least some semblance of reality based on capabilities of Chain Home & Chain Home Low
                                                    //https://en.wikipedia.org/wiki/Chain_Home
                                                    //https://en.wikipedia.org/wiki/Chain_Home_Low

                                                    //dropoutValue used below to randomly drop some readings from the radar.  As group gets larger the chance of dropouts goes to nil.
                                                    int dropoutValue = 7;
                                                    if (poscount > 5) { dropoutValue = Convert.ToInt32(7 * poscount ^ 2 / 25); }

                                                    if (dis_mi <= 2 && a != p && Math.Abs(player_alt_m - agid.AGGaveAlt_m) < 5000)
                                                    {
                                                        posmessage = agid.AGGmixupType + " nearby";
                                                    }
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
                                                             (dis_mi >= 10 && altAGL_ft < 650 && altAGL_ft < random.Next(500)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                                                                                                  //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                                                                                                  //phasing out targets below 250 feet.

                                                             belowRadar(altAGL_ft, vel_mps, airGroup, a) || //unified method for deciding below radar or not

                                                             poscount == 0 || //this happens if all a/c in a group are belowRadar
                                                             agid.AGGradarDropout && random.Next(3) == 1 || //For Tab-4-1 listings we honor the heavy bomber radar dropout only 33% of the time.  This is bec. the radar screen operators have ways of picking up tracks even if they appear just once in a while, but Tab-4-1 folks less so.

                                                             //((!isAI && isHeavyBomber && army != playerArmy) && dis_mi > 11 && poscount <= 2 && random.Next(4) <= 2) || // Breather bombers have a higher chance of being overlooked/dropping out, especially when further away.  3/4 times it doesn't show up on radar.

                                                             ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi <= 4 && poscount <= 2 && random.Next(5) <= 3) // Breather bombers have a much higher chance of being overlooked/dropping out when close (this is close enough it should be visual range, so we're not going to help them via radar)
                                                                                                                                                                     //((!isAI && type == "B" && army != playerArmy) && random.Next(5) > 0) || // Enemy bombers don't show up on your radar screen if less than 7 miles away as a rule - just once in a while.  You'll have to spot them visually instead at this distance!
                                                                                                                                                                     //We're always showing breather FIGHTERS here (ie, they are not included in isAI || type == "B"), because they always show up as a group of 1, and we'd like to help them find each other & fight it out
                                                                                                                                                                     //(random.Next(dropoutValue) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                             )
                                                    {
                                                        posmessage = "";
                                                        //posmessage = string.Format (dis_mi, )

                                                        /*
                                                        posmessage = type + " " +
                                                                        mi_10 +
                                                                        bearing_10.ToString("F0") + "°" +
                                                                        ang +
                                                                        mph +
                                                                        heading_10.ToString("F0") + "° " + sector.ToString();

                                                        if (intcpt_time_min >= 0.02)
                                                        {
                                                            posmessage +=
                                                               " Intcpt: " +
                                                               intcpt_heading.ToString("F0") + "°" +
                                                               intcpt_time_min.ToString("F0") + "min ";
                                                            //+ intcpt_sector + " ";
                                                        }
                                                        */
                                                    }
                                                    else
                                                    {
                                                        posmessage = agid.AGGmixupType + " " +
                                                           mi_10 +
                                                           bearing_10.ToString("F0") + "°" +
                                                           ang +
                                                           mph +
                                                           heading_10.ToString("F0") + "° ";
                                                        //+ sector.ToString();

                                                        if (!playerOffRadar && intcpt_time_min >= 0.02) //omit intercept messages when the player is below radar (need player position/speed to calculate intercept)
                                                        {
                                                            posmessage +=
                                                               " Intcpt: " +
                                                               intcpt_heading.ToString("F0") + "°" +
                                                               intcpt_time_min.ToString("F0") + "min ";
                                                            //+ intcpt_sector + " ";
                                                        }

                                                    }

                                                }


                                                //Console.WriteLine("Rad: pos " + posmessage);

                                                //We'll print only one message per Airgroup, to reduce clutter
                                                //twcLogServer(new Player[] { player }, "RPT: " + posmessage + posmessage.Length.ToString(), new object[] { });
                                                if (posmessage.Length > 0)
                                                {
                                                    //gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                                    //Console.WriteLine("NON-ADMIN: ~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                                    //We add the message to the list along with an index that will allow us to reverse sort them in a logical/useful order                               
                                                    int intcpt_time_index = (int)intcpt_time_min;
                                                    if (intcpt_time_min <= 0 || intcpt_time_min > 99) intcpt_time_index = 99;

                                                    try
                                                    {
                                                        string addMess = posmessage;
                                                        if (radar_realism > 0) addMess = "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + posmessage;
                                                        radar_messages.Add(
                                                           ((int)intcpt_time_index).ToString("D2") + ((int)dis_mi).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                                           addMess
                                                        );


                                                        if (aiairgroup != null)
                                                        {
                                                            ai_radar_info.Add(
                                     ((int)intcpt_time_index).ToString("D2") + ((int)dis_mi).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                     new AiAirGroupRadarInfo(this, agid, padig, intcpt, climb_possible)
                                  );
                                                            //Console.WriteLine("Adding ai_radar_info: {0} {1} {2} {3} {4}", intcpt.x, intcpt.y, intcpt.z, agid.playerNames, padig.playerNames);
                                                        }


                                                    }
                                                    catch (Exception e)
                                                    {
                                                        Console.WriteLine("RadErrorAdd: " + e);
                                                    }

                                                }
                                            }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Radar ERROR2: " + ex.ToString());
                                }
                            }
                        }
                    }










                    /*
                                //Using our GROUPED AirGroups instead of chunking through each individual aircraft
                                else
                                {


                                    //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
                                    if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
                                    {
                                        foreach (int army in GamePlay.gpArmies())
                                        {
                                            //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                                            if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0 && (!inOwnArmy ^ (army == playerArmy)))
                                            {
                                                //if (CurrentAGGroupLeaders[army] != null) foreach (AiAirGroup airGroup in CurrentAGGroupLeaders[army])
                                                if (airGroupInfoDict != null) foreach (AiAirGroup airGroup in airGroupInfoDict.Keys)
                                                    {

                                                        Console.WriteLine("LPAA: Processing ag: {0} {1} ", airGroup.getArmy(), airGroup.NOfAirc);
                                                        AirGroupInfo agid = airGroupInfoDict[airGroup];
                                                        if (agid.actor.Army() != army) continue;

                                                        Console.WriteLine("LPAA: Processing ag: {0} {1} ", agid.actor.Army(), agid.actor.Name());

                                                        aigroup_count++;


                                                        AiActor actor = agid.actor;
                                                        AiAircraft a = actor as AiAircraft;
                                                        //if (!player_place_set &&  (a.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"

                                                        poscount = agid.AGGcount;

                                                        //Check on any radar outages or restrictions for each army, and remove any radar returns from areas where radar is restricted or inoperative
                                                        if (!MO_isRadarEnabledByArea(agid.AGGavePos, admin, radarArmy)) continue;

                                                        if (!player_place_set)
                                                        {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"                                                                        
                                                            p = actor as AiAircraft;
                                                            player_Vwld = p.AirGroup().Vwld();
                                                            player_vel_mps = Calcs.CalculatePointDistance(player_Vwld);
                                                            player_vel_mph = Calcs.meterspsec2milesphour(player_vel_mps);
                                                            player_alt_m = p.Pos().z;
                                                            // player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                                                            //player_sector = player_sector.Replace(",", ""); // remove the comma 
                                                            player_sector = Calcs.correctedSectorName(this, p.Pos());
                                                            ppos = p.Pos();
                                                            player_place_set = true;
                                                        }

                                                        bool isAI = (agid.AGGAIorHuman == aiorhuman.AI);

                                                        string acType = agid.AGGtypeNames;
                                                        isHeavyBomber = agid.AGGisHeavyBomber;


                                                        type = agid.AGGtype;
                                                        if (a == p && radar_realism >= 0) type = "Your position";
                                                        // if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                                        
                                                        // + a.Type() + " " 
                                                        // + a.TypedName() + " " 
                                                        // +  a.AirGroup().ID(), new object[] { });

                                                        pos1 = agid.AGGavePos;
                                                        //Thread.Sleep(100);
                                                        //pos2=a.Pos();
                                                        //bearing=Calcs.CalculateGradientAngle (pos1,pos2);
                                                        Vwld = new Vector3d(agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z);
                                                        vel_mps = Calcs.CalculatePointDistance(Vwld);
                                                        vel_mph = Calcs.meterspsec2milesphour(vel_mps);
                                                        vel_mph_10 = Calcs.RoundInterval(vel_mph, 10);
                                                        heading = (Calcs.CalculateBearingDegree(Vwld));
                                                        heading_10 = Calcs.GetDegreesIn10Step(heading);
                                                        dis_m = Calcs.CalculatePointDistance(agid.AGGavePos, p.Pos());
                                                        dis_mi = Calcs.meters2miles(dis_m);
                                                        dis_10 = (int)dis_mi;
                                                        if (dis_mi > 20) dis_10 = Calcs.RoundInterval(dis_mi, 10);
                                                        bearing = Calcs.CalculateGradientAngle(p.Pos(), agid.AGGavePos);
                                                        bearing_10 = Calcs.GetDegreesIn10Step(bearing);

                                                        longlat = Calcs.Il2Point3dToLongLat(agid.AGGavePos);

                                                        alt_km = agid.AGGavePos.z / 1000;
                                                        alt_ft = Calcs.meters2feet(agid.AGGavePos.z);
                                                        //altAGL_m = (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0); // I THINK (?) that Z_AltitudeAGL is in meters?

                                                        //We're using group leaders alt & AGL to get aveAGL for the entire group. Formula: AveAlt - (alt-AGL) = AveAGL
                                                        altAGL_m = agid.AGGaveAlt - (agid.pos.z - (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0)); // I THINK (?) that Z_AltitudeAGL is in meters?
                                                        altAGL_ft = Calcs.meters2feet(altAGL_m);
                                                        alt_angels = Calcs.Feet2Angels(alt_ft);
                                                        sector = GamePlay.gpSectorName(agid.AGGavePos.x, agid.AGGavePos.y).ToString();
                                                        sector = sector.Replace(",", ""); // remove the comma
                                                        VwldP = new Point3d(agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z);

                                                        intcpt = Calcs.calculateInterceptionPoint(agid.AGGavePos, VwldP, p.Pos(), player_vel_mps);
                                                        intcpt_heading = (Calcs.CalculateGradientAngle(agid.AGGavePos, intcpt));
                                                        intcpt_time_min = intcpt.z / 60;
                                                        // intcpt_sector = GamePlay.gpSectorName(intcpt.x, intcpt.y).ToString();
                                                        //intcpt_sector = intcpt_sector.Replace(",", ""); // remove the comma 
                                                        intcpt_sector = Calcs.correctedSectorName(this, intcpt);
                                                        intcpt_reasonable_time = (intcpt_time_min >= 0.02 && intcpt_time_min < 20);

                                                        climb_possible = true;
                                                        if (player_alt_m <= agid.AGGminAlt && intcpt_time_min > 1)
                                                        {
                                                            double altdiff_m = agid.AGGminAlt - player_alt_m;
                                                            if (intcpt_time_min > 3 && altdiff_m / intcpt_time_min > 1300) { climb_possible = false; } //109 can climb @ a little over 1000 meters per minute in a sustained way.  So anything that requires more climb than that we exclude from the listing
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
                                                        if (radar_realism < 0) //This applies to Red & Blue tophat, which uses groupings.  Admin tophat still uses individual per aircraft returns.
                                                        {

                                                            string numContacts = poscount.ToString();
                                                            string aircraftType = agid.AGGtypeNames;
                                                            string vel = vel_mph.ToString("n0");
                                                            string alt = alt_angels.ToString("n0");
                                                            string he = heading.ToString("F0");

                                                            string aplayername = agid.AGGplayerNames;

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
                                                            agid.AGGavePos.x.ToString()
                                                            + "," + agid.AGGavePos.y.ToString() + "," +
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
                                                                           belowRadar(altAGL_ft) || //unified method for deciding below radar or not
                                                                           ((!isAI && isHeavyBomber) && poscount <= 2 && random.Next(3) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out 
                                                                                                                                                 //However if the player heavy bombers group up they are MUCH more likely to show up on radar.  But they will still be harder than usual to track because each individual bomber will phase in/out quite often

                                                                           (random.Next(7) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                                           )
                                                           ) { posmessage = ""; }


                                                        }


                                                        else if (radar_realism == 0)
                                                        {
                                                            posmessage = poscount.ToString() + type + " " +

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

                                                            // "(" + 
                                                            //Calcs.meters2miles(a.Pos().x).ToString ("F0") + ", " +
                                                            //Calcs.meters2miles(a.Pos().y).ToString ("F0") + ")";

                                                            //twcLogServer(new Player[] { player }, posmessage, new object[] { });
                                                        }
                                                        else if (radar_realism > 0)
                                                        {

                                                            //Trying to give at least some semblance of reality based on capabilities of Chain Home & Chain Home Low
                                                            //https://en.wikipedia.org/wiki/Chain_Home
                                                            //https://en.wikipedia.org/wiki/Chain_Home_Low
                                                            if (random.Next(8) == 1)
                                                            { //oops, sometimes we get mixed up on the type.  So sad . . .  See notes below about relative inaccuracy of early radar.
                                                                type = "F";
                                                                if (random.Next(3) == 1) type = "B";
                                                            }
                                                            if (dis_mi <= 2 && a != p && Math.Abs(player_alt_m - agid.AGGaveAlt) < 5000) { posmessage = type + " nearby"; }


                                                            //Below conditions are situations where radar doesn't work/fails, working to integrate realistic conditions for radar
                                                            //To do this in full realism we'd need the full locations of Chain Home & Chain Home Low stations & exact capabilities
                                                            //As an approximation we're using distance from the current aircraft, altitude, etc.
                                                             //wikipedia gives an idea of how rough early CH output & methods were: CH output was read with an oscilloscope. When a pulse was sent from the broadcast towers, a visible line /
                                                             //travelled horizontally across the screen very rapidly. The output from the receiver was amplified and fed into the vertical axis of the scope, so a return from an aircraft //
                                                             //would deflect the beam upward. This formed a spike on the display, and the distance from the left side – measured with a small scale on the bottom of the screen – would give 
                                                             //target range. By rotating the receiver goniometer connected to the antennas, the operator could estimate the direction to the target (this was the reason for the cross shaped 
                                                             //antennas), while the height of the vertical displacement indicated formation size. By comparing the strengths returned from the various antennas up the tower, altitude could be
                                                                // gauged with some accuracy.
                                                              //Upshot is, exact #, position, no of aircraft, type of aircraft, altitude etc were NOT that precisely known.  Rather they were estimates/guesstimates based on strength of pulse 
                                                              //of the radar return as viewed on an oscilliscope etc.
                                                             //
                                                            else if ((dis_mi >= 50 && poscount < 8 && random.Next(15) > 1 && !intcpt_reasonable_time) ||  //don't show enemy groups too far away, unless they are quite large, or can be intercepted in reasonable time.  Except once in a while randomly show one.
                                                                     (dis_mi >= 25 && poscount < 4 && random.Next(12) > 1 && !intcpt_reasonable_time) ||
                                                                     (dis_mi >= 15 && poscount <= 2 && random.Next(8) > 1 && !intcpt_reasonable_time) ||
                                                                     (!climb_possible && playertype != "B" && army != playerArmy) ||  //If the aircraft is too high for us to be able to climb to, we exclude it from the listing, unless the player is a bomber pilot (who is going to be interested in which planes are above in attack position) OR we are getting a listing of our own army, in which case we want all nearby a/c not just ones we can attack
                                                                     (dis_mi >= 70 && altAGL_ft < 4500) || //chain home only worked above ~4500 ft & Chain Home Low had effective distance only 35 miles
                                                                                                           //however, to implement this we really need the distance of the target from the CHL stations, not the current aircraft
                                                                                                           //We'll approximate it by eliminating low contacts > 70 miles away from current a/c 
                                                                     (dis_mi >= 10 && altAGL_ft < 650 && altAGL_ft < random.Next(500)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                                                                                                          //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                                                                                                          //phasing out targets below 250 feet.

                                                                     belowRadar(altAGL_ft) || //unified method for deciding below radar or not
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







                                                    }
                                            }
                                            //We'll print only one message per Airgroup, to reduce clutter
                                            twcLogServer(new Player[] { player }, "RPT: " + posmessage + posmessage.Length.ToString(), new object[] { });
                                            if (posmessage.Length > 0)
                                            {
                                                gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                                //We add the message to the list along with an index that will allow us to reverse sort them in a logical/useful order                               
                                                int intcpt_time_index = (int)intcpt_time_min;
                                                if (intcpt_time_min <= 0 || intcpt_time_min > 99) intcpt_time_index = 99;

                                                try
                                                {
                                                    string addMess = posmessage;
                                                    if (radar_realism > 0) addMess = "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + posmessage;
                                                    radar_messages.Add(
                                                       ((int)intcpt_time_index).ToString("D2") + ((int)dis_mi).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                                       addMess
                                                    );
                                                }
                                                catch (Exception e)
                                                {
                                                    twcLogServer(new Player[] { player }, "RadError: " + e, new object[] { });
                                                }







                                            }

                                        }
                                    }
                               } 

                    */
                    //There is always one message - the header.  
                    if (radar_messages.Count == 1) radar_messages.Add("0000000000", "<NO TRADE>");


                    if (radar_realism < 0)
                    {
                        //try
                        {
                            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX9"); //Testing for potential causes of warping
                            string typeSuff = "";
                            if (playerArmy == -3) typeSuff = "_ADMIN";
                            if (playerArmy == -4) typeSuff = "_ADMINGROUP";
                            if (playerArmy == -1) typeSuff = "_RED";
                            if (playerArmy == -2) typeSuff = "_BLUE";

                            //TODO!!!!  Rewrite all this to just create a string with the file & then write using File.WriteAllText(file,data);
                            //The avoids the problem of files not being closed/disposed & then they are LOCKED!!!!!
                            //For now the try/catch should take care of any problems, but we'll see.
                            string filepath = STATSCS_FULL_PATH + SERVER_ID_SHORT.ToUpper() + typeSuff + "_radar.txt";
                            if (File.Exists(filepath)) { File.Delete(filepath); }
                            fi = new System.IO.FileInfo(filepath); //file to write to
                            sw = fi.CreateText(); // Writes Lat long & other info to file

                            try
                            {
                                foreach (var mess in radar_messages)
                                {
                                    sw.WriteLine(mess.Value);
                                }
                            }
                            catch (Exception ex) { Console.WriteLine("Radar Write2: " + ex.ToString()); }

                            sw.Close();
                            sw.Dispose();


                            //And, now we create a file with the list of players:
                            //TODO: This probably could/should be a separate method that we just call here

                            //TODO!!!!  Rewrite all this to just create a string with the file & then write using File.WriteAllText(file,data);
                            //The avoids the problem of files not being closed/disposed & then they are LOCKED!!!!!
                            //For now the try/catch should take care of any problems, but we'll see.
                            filepath = STATSCS_FULL_PATH + SERVER_ID_SHORT.ToUpper() + typeSuff + "_players.txt";
                            if (File.Exists(filepath)) { File.Delete(filepath); }
                            fi = new System.IO.FileInfo(filepath); //file to write to
                            //try
                            {
                                sw = fi.CreateText(); // Writes Lat long & other info to file
                                sw.WriteLine("[[" + DateTime.UtcNow.ToString("u").Trim() + "]] " + showTimeLeft(null, false));
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
                                            if (act == null) continue; // no point in going on if nothing here

                                            if (act as AiAircraft != null)
                                            {
                                                AiAircraft acf = act as AiAircraft;
                                                string acType = Calcs.GetAircraftType(acf);
                                                pl = acType;
                                            }

                                            if (playerArmy == -3 || playerArmy == -4)
                                            {
                                                //Point3d ps = Calcs.Il2Point3dToLongLat(act.Pos());
                                                //pl += " " + ps.y.ToString("n2") + " " + ps.x.ToString("n2");
                                                pl += " " + act.Pos().x.ToString("n0") + " " + act.Pos().y.ToString("n0");  //2018/09/20 - switching order of x & y & now need to do the same in radar.php
                                            }

                                        }
                                        msg += py.Name() + " " + py.Army() + " " + pl + "\n";

                                    }

                                }

                                sw.WriteLine("Players logged in: " + pycount.ToString() + " Active: " + pyinplace.ToString());
                                sw.WriteLine();

                                sw.WriteLine(CAMPAIGN_ID.ToUpper() + " MISSION SUMMARY");

                                sw.WriteLine(string.Format("BLUE session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", BlueTotalF,
              BlueAirF, BlueAAF, BlueNavalF, BlueGroundF));
                                sw.WriteLine(string.Format("RED session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", RedTotalF,
              RedAirF, RedAAF, RedNavalF, RedGroundF));
                                sw.WriteLine();

                                sw.WriteLine("Blue Objectives complete (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Blue]));
                                sw.WriteLine();
                                if (playerArmy == -2 || playerArmy == -3 || playerArmy == -4) sw.WriteLine(MO_ListRemainingPrimaryObjectives(player: player, army: (int)ArmiesE.Blue, numToDisplay: 50, delay: 0, display: false, html: false));//sw.WriteLine("Blue Primary Objectives: " + MissionObjectivesString[ArmiesE.Blue]);

                                sw.WriteLine("Red Objectives complete (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Red]));
                                sw.WriteLine();
                                if (playerArmy == -1 || playerArmy == -3 || playerArmy == -4) sw.WriteLine(MO_ListRemainingPrimaryObjectives(player: player, army: (int)ArmiesE.Red, numToDisplay: 50, delay: 0, display: false, html: false));//sw.WriteLine("Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red]);

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

                                sw.WriteLine(CAMPAIGN_ID.ToUpper() + " CAMPAIGN SUMMARY");

                                Tuple<double, string> res = CalcMapMove("", false, false, null);
                                sw.Write(res.Item2);
                                double newMapState = CampaignMapState + res.Item1;
                                sw.Write(summarizeCurrentMapstate(newMapState, false, null));


                                if (msg.Length > 0)
                                {
                                    sw.WriteLine();
                                    sw.WriteLine("PLAYER SUMMARY");
                                    sw.WriteLine(msg);
                                }

                                //try
                                {
                                    msg = ListRadarTargetDamage(null, -1, false, false); //Add the list of current radar airport conditions
                                    msg += Environment.NewLine;
                                    msg += ListAirfieldTargetDamage(null, -1, false, false); //Add the list of current airport conditions
                                    if (msg.Length > 0)
                                    {
                                        sw.WriteLine();
                                        sw.WriteLine("RADAR AND AIRFIELD CONDITION SUMMARY");
                                        sw.WriteLine(msg);
                                    }
                                }
                                //catch (Exception ex) { Console.WriteLine("Radar Write34: " + ex.ToString()); }

                                sw.WriteLine();
                                //string netRed = TWCStatsMission.Display_SessionStatsAll(null, 1, false, false);
                                //string netBlue = TWCStatsMission.Display_SessionStatsAll(null, 2, false, false);
                                string netRed = statsmission.Display_SessionStatsAll(null, 1, false, false);
                                string netBlue = statsmission.Display_SessionStatsAll(null, 2, false, false);
                                sw.WriteLine("PLAYER ACTIVITY SUMMARY");
                                sw.WriteLine(netBlue);
                                sw.WriteLine(netRed);

                                if (TWCSupplyMission != null)
                                {
                                    sw.WriteLine();
                                    if (playerArmy == -2 || playerArmy == -3 || playerArmy == -4) sw.WriteLine(TWCSupplyMission.ListAircraftLost(2, null, false, false));//sw.WriteLine("Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red]);
                                                                                                                                                                         //if (playerArmy == -3 || playerArmy == -4) sw.WriteLine();
                                    if (playerArmy == -1 || playerArmy == -3 || playerArmy == -4) sw.WriteLine(TWCSupplyMission.ListAircraftLost(1, null, false, false));//sw.WriteLine("Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red]);


                                }


                                msg = "";
                                if (playerArmy == -2 || playerArmy == -3 || playerArmy == -4) msg += MO_ListScoutedObjectives(null, 2) + Environment.NewLine;
                                if (playerArmy == -1 || playerArmy == -3 || playerArmy == -4) msg += MO_ListScoutedObjectives(null, 1) + Environment.NewLine;

                                if (msg.Length > 0)
                                {
                                    sw.WriteLine();
                                    sw.WriteLine(CAMPAIGN_ID.ToUpper() + " RECONNAISSANCE SUMMARY");
                                    sw.WriteLine(msg);
                                }


                                msg = "";
                                if (playerArmy == -2 || playerArmy == -3 || playerArmy == -4)
                                {
                                    if (TWCSupplyMission != null) msg += (TWCSupplyMission.DisplayNumberOfAvailablePlanes(2, null, false)) + Environment.NewLine;
                                }
                                if (playerArmy == -1 || playerArmy == -3 || playerArmy == -4)
                                {
                                    if (TWCSupplyMission != null) msg += (TWCSupplyMission.DisplayNumberOfAvailablePlanes(1, null, false)) + Environment.NewLine;
                                }

                                if (msg.Length > 0)
                                {
                                    sw.WriteLine();
                                    sw.WriteLine(CAMPAIGN_ID.ToUpper() + " CURRENT AIRCRAFT STOCK LEVELS");
                                    sw.WriteLine(msg);
                                }

                            }
                            //catch (Exception ex) { Console.WriteLine("Radar Write3: " + ex.ToString()); }

                            sw.Close();
                            sw.Dispose();


                        }
                        //catch (Exception ex) { Console.WriteLine("Radar Write1: " + ex.ToString()); }
                    }

                    TimeSpan timeDiff = DateTime.UtcNow.Subtract(d);

                    var saveradar_realism = radar_realism;
                    Timeout(wait_s, () =>
                    {
                        try
                        {
                            //So, overly long radar messages are incomprehensible & jam up the comms.  So, just trimming it down to 12 msgs max.
                            //It has to be the last 12, because the most important msgs are at the end. Can't eliminate the header, either, so always include 1st msg
                            //Maybe should be even shorter?
                            SortedDictionary<string, string> radar_messages_trim = new SortedDictionary<string, string>(new ReverseComparer<string>());
                            int trim = 0;
                            int c = 0;
                            if (!admin && playerArmy >= 0 && radar_messages.Count > 12) trim = radar_messages.Count - 12;
                            //Console.WriteLine("RadTrim: {0} {1} {2}", trim, c, radar_messages.Count);


                            //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               
                            double delay = 0;
                            foreach (var mess in radar_messages)
                            {
                                if (c > 0 && c <= trim)
                                {
                                    c++;
                                    // Console.WriteLine("RadTrim: Trimming {0} {1} {2}", trim, c, radar_messages.Count);
                                    continue;
                                }
                                //Console.WriteLine("RadTrim: NoTrim {0} {1} {2}", trim, c, radar_messages.Count);
                                c++;
                                radar_messages_trim.Add(mess.Key, mess.Value);
                                delay += 0.2;
                                Timeout(delay, () =>
                                   {
                                       if (saveradar_realism == 0 && display) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                                       else if (saveradar_realism >= 0 && display) gpLogServerAndLog(new Player[] { player }, mess.Value, null);
                                       //if (!display && aiairgroup != null) Console.WriteLine("AiAirgroup radar return: " + mess.Value); //for testing
                                   });


                            }
                            radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(currtime_ms, radar_messages_trim);
                            if (aiairgroup != null) ai_radar_info_store[aiairgroup] = ai_radar_info;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Radar ERROR1: " + ex.ToString());
                        }

                    });//timeout      

                }
                /*catch (Exception ex)
                {
                    Console.WriteLine("Radar ERROR37: " + ex.ToString());
                }*/

            }
        } /* catch (Exception ex)
        {
            Console.WriteLine("Radar ERROR: " + ex.ToString());
        }*/
    }//method radar     

    /******************************************************************************************************************** 
    * ****END****RADAR
    * **********************************************************************************************************************/


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
    public class c1
    {
        event maddox.game.GameDef.Chat EventChat;
    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        //listen to events from all missions.
        //Note: this is in Init now, so disabling it here.  See note in Init (below).
        //MissionNumberListener = -1;

        //When battle is started we re-start the Mission tick clock - setting it up to start events
        //happening when the first player connects

        CheckAndChangeStartTime(); //will check desired start time vs actual in-game time & rewrite the .mis file, restart the mission if needed to get the time at the right place


        MISSION_STARTED = true;
        if (WAIT_FOR_PLAYERS_BEFORE_STARTING_MISSION_ENABLED) MISSION_STARTED = false;
        START_MISSION_TICK = -1;

        COOP_START_MODE = false;
        if (COOP_START_MODE_ENABLED) COOP_START_MODE = true;
        START_COOP_TICK = -1;
        CheckCoop();  //Start the routine to enforce the coop start/no takeoffs etc

        CampaignMapSuffix = GetMapSuffix(); //This must happen BEFORE EndMissionIfPlayersInactive(); as this reads in the initial campaign state variable & EndMissionIfPlayersInactive(); will overwrite it.
                                            //Timeout(5, () => { SetAirfieldTargets(); });  //Delay for the situation where airfields are loaded via init submissions, which might take a while to load

        
        DrawFrontLinesPerMapState();
        
        //TESTING
        /*
        for (double i = -25; i <= 25; i = i + 12.5)
        {
            DrawFrontLinesPerMapState(-25, 25, i, "test" + i.ToString("F0") + ".mis");
            
        }
        */

        SetAirfieldTargets(); //but since we're not doing that now, we can load it immediately.  Airfields MUST be loaded before mission_objectives bec. the airfield list is used to create mission_objectives
        ReadInitialSubmissions(MISSION_ID + "-initsubmission", 0, 0); //so we can include initsubmissions if we want
        if (mission_objectives == null) Console.WriteLine("#00.4  Mission Objectives doesn't exist yet!");

        mission_objectives = new MissionObjectives(this, GamePlay); //this must be done AFTER GetMapSuffix as that reads results of previous mission & that is needed for setting up mission objectives

        if (mission_objectives == null) Console.WriteLine("#00.5  Mission Objectives doesn't exist still!");


        LoadRandomSubmission(MISSION_ID + "-" + "initairports" + CampaignMapSuffix); // choose which of the airport & front files to load initially


        //Turning EndMissionIfPlayersInactive(); off for TF 4.5 testing.
        EndMissionIfPlayersInactive(); //start routine to check if no players in game & stop the mission if so


        ReadInitialSubmissions(MISSION_ID + "-stats", 0, 0.1);
        ReadInitialSubmissions(MISSION_ID + "-supply", 0, 0.2);

        SaveCampaignStateIntermediate(); //save campaign state/score every 10 minutes so that it isn't lost of we end unexpectedly or crash etc

        /// CONFIGURE REARM/REFUEL
        /// Duration in seconds for full rearm
        // RearmRefuelConfig.REARM_DURATION = 150;

        /// Duration in seconds for 100% refuel
        /// Lesser fuel amounts result in relative fractions of that.
        /// Note: see REFUEL_MIN_DURATION below
        // RearmRefuelConfig.REFUEL_DURATION =  270;

        /// Minimum duration in seconds for refuel
        /// Refuel time cannot be shorter than that
        // RearmRefuelConfig.REFUEL_MIN_DURATION = 150;

        /// Interval in seconds of "... complete in Min::Sec messages
        // RearmRefuelConfig.MESSAGE_INTERVAL = 15;

        /// The visible airfields are sometimes bigger than the technical value configured.
        /// Even spawn points may sometimes be outside of that technical perimeter
        /// Technical airfield perimeter is multiplied with this.
        // RearmRefuelConfig.AIRFIELD_PERIMETER_ADJUST = 1.5;

        /// Allowable deviation from airfield's elevation in meter.
        /// To check if we're actually on the ground, we need to check plane's elevation
        /// against the airfield's. Unfortunately airfield elevation is not homogenous, en
        /// extreme is Maidstone with places up to 7m different from technical elevation.
        /// Note: This is strictly not necessary, because speed=0 within airfield
        ///       perimeter would mean the same, but for user friendliness we say 
        ///       'land on airfield' first and 'set chokes' later
        /// value in meters
        // public static double AIRFIELD_ELEVATION_ADJUST = 7;

        /// maximum engines to check (tanks, fuelcocks, magnetos, etc.)
        // RearmRefuelConfig.MAX_ENGINE_COUNT = 2;

        /// output debugging messages
        // public static bool DEBUG_MESSAGES = false;



        if (GamePlay is GameDef)
        {
            //Calcs.RemoveDelegates((GamePlay as GameDef).EventChat as object);
            //(GamePlay as GameDef).EventChat.ResetSubscriptions;
            //(GamePlay as GameDef).EventChat--;
            //(GamePlay as GameDef).EventChat = c1;
            //(GamePlay as GameDef).EventChat = delegate { };
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
            //Console.WriteLine((GamePlay as GameDef).EventChat.ToString());
            //(GamePlay as GameDef).EventChat = null;


            //Various attempts to hut up chat messages.
            //cevent.RemoveAllEventHandlers((GamePlay as GameDef).EventChat as object);
            //cevent.cEventHelper.RemoveAllEventHandlers((GamePlay as GameDef).EventChat as object);
            //Component[] cmp = this.components.Components;


            //Calcs.GetEventKeysList(Component);
            //System.Reflection.PropertyInfo eventsProp = typeof(Component).GetProperty("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            /*

            List<Component> cmps = (from field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                    where typeof(Component).IsAssignableFrom(field.FieldType)
                                    let component = (Component)field.GetValue(this)
                                    where component != null
                                    select component).ToList();

            foreach (Component c in cmps) Console.WriteLine("COMP EVENTS PROPS HI!" + c.ToString());


            System.Reflection.PropertyInfo eventsProp = typeof(Component).GetProperty("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            //eventsProp.
            //Calcs.GetEventKeysList(GamePlay);
            Console.WriteLine("COMP EVENTS PROPS " + cmps.ToString());
            */
            /*
            EventHandlerList events = (EventHandlerList)eventsProp.GetValue((GamePlay as GameDef).EventChat, null);
            
            foreach (EventHandler ehl in eventsProp ){
                Console.WriteLine("EVENTS PROPS " + ehl.ToString());
            }
            */
            /*
            Delegate[] dary = (GamePlay as GameDef).EventChat.GetInvocationList();

            if (dary != null)
            {
                foreach (Delegate del in dary)
                {
                    (GamePlay as GameDef).EventChat -= (Action)del;
                }
            }
            */
        }

        //Delete any old CampaignSummary.txt files so that they are not hanging around causing trouble
        try
        {
            File.Delete(STATSCS_FULL_PATH + "CampaignSummary.txt");
        }
        catch (Exception ex) { Console.WriteLine("CampaignSummary Delete: " + ex.ToString()); }


        //if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban LOAD twc_banned.txt");
        //we do the "ban" system loading in BurnBabyBurn.cs instead of here.  


    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        Console.WriteLine("Battle Stopping -- saving map state & current supply status");

        /// REARM/REFUEL: CLEANUP ANY PENDING REQUESTS
     //   ManageRnr.cancelAll(GamePlay);

        if (!final_SaveMapState_completed && Time.tickCounter() > 15000) SaveMapState(""); //A call here just to be safe; we can get here if 'exit' is called etc, and the map state may not be saved yet . . . 
        if (!final_MO_WriteMissionObjects_completed) MO_WriteMissionObjects(wait: true);

        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
        Thread.Sleep(3); //just wait for all files to save etc
    }

    /****************************************
     * READINITIALSUBMISSIONS
     * 
     * Loads any files in the FILE_PATH that match the pattern (anything or nothing) + filenameID + (anything or nothing) + .mis
     * wait tells how many seconds to wait before starting to load, timespread will spread multiple initialsubmission loads
     * over a certain time period (seconds)
     * 
     * **************************************/

    public void ReadInitialSubmissions(string filenameID, int timespread = 60, double wait = 0)
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
                DebugAndLog(s.Replace(CLOD_PATH + FILE_PATH, "") + " file loaded");
                Console.WriteLine(s.Replace(CLOD_PATH + FILE_PATH, "") + " file loaded");
            }
            else
            {
                //if (timespread>2 && random.Next(1) == 1) continue; //TESTING, skip 50% of mission loads just to try it
                Timeout(wait + random.Next(timespread), () =>
                {

                    //string temp = @"missions\AirfieldSpawnTest\Airfields\" + Path.GetFileName(s);

                    GamePlay.gpPostMissionLoad(s);

                    //string s2=@"C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\missions\Multi\Fatal\TWC-initsubmission-stationary.mis";
                    //GamePlay.gpPostMissionLoad(s2);
                    //if (DEBUG)
                    //{
                    DebugAndLog(s.Replace(CLOD_PATH + FILE_PATH, "") + " file loaded");
                    Console.WriteLine(s.Replace(CLOD_PATH + FILE_PATH, "") + " file loaded");
                    //}
                });
            }
        }

    }

    /************************************************************
     * INIT
     * *********************************************************/

    //Listen to events of every mission
    public override void Init(maddox.game.ABattle battle, int missionNumber)
    {
        base.Init(battle, missionNumber);

        gpBattle = battle;

        gpBattle.creatingMissionScript(covermission, missionNumber + 1);
        gpBattle.creatingMissionScript(statsmission, missionNumber + 2);

        MissionNumberListener = -1; //Listen to events of every mission
                                    //This is what allows you to catch all the OnTookOff, OnAircraftDamaged, and other similar events.  Vitally important to make this work!
                                    //If we load missions as sub-missions, as we often do, it is vital to have this in Init, not in "onbattlestarted" or some other place where it may never be detected or triggered if this sub-mission isn't loaded at the very start.
                                    //Initialize the mission objectives etc but wait until after initial submissions are loaded

        //This needs to be in Init (not Mission constructor) because it relies on some Battle. stuff that isn't initialized yet at Mission constructor
        //(For now this is unnecessary but in future we might load things in initial submissions & then mission_objectives potentially affected by them afterwards)
        //Timeout(30, () => { mission_objectives = new MissionObjectives(this); });
        //Timeout(1, () => { mission_objectives = new MissionObjectives(this); }); //testing


        //For testing
        /*
        Timeout(30, () => {MO_DestroyObjective("BTarget14R");
            MO_DestroyObjective("BTarget22R");
            MO_DestroyObjective("RTarget28R");
            MO_DestroyObjective("RTarget29R");


        });
        */



    }

    IStatsMission TWCStatsMission;
    ISupplyMission TWCSupplyMission;
    IKnickebeinMission TWCKnickebeinMission;
    ICoverMission TWCCoverMission;
    IStbSaveIPlayerStat TWCSaveIPlayerStat;

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);
        //if (TWCComms.Communicator.Instance.Stats != null && TWCStatsMission == null) TWCStatsMission = TWCComms.Communicator.Instance.Stats; 
        TWCStatsMission = TWCComms.Communicator.Instance.Stats;
        //if (TWCStatsMission != null) TWCSaveIPlayerStat = TWCStatsMission.stb_ISaveIPlayerStat;
        TWCSaveIPlayerStat = statsmission.stb_ISaveIPlayerStat;
        if (TWCComms.Communicator.Instance.stb_FullPath != null && TWCComms.Communicator.Instance.stb_FullPath.Length > 0) STATSCS_FULL_PATH = TWCComms.Communicator.Instance.stb_FullPath;
        //STATSCS_FULL_PATH = statsmission.stb_FullPath; //for some reason, this doesn't work correctly 2020/02/14
        TWCSupplyMission = TWCComms.Communicator.Instance.Supply;
        TWCKnickebeinMission = TWCComms.Communicator.Instance.Knickebein;
        TWCCoverMission = TWCComms.Communicator.Instance.Cover;
        //TWCStbSaveIPlayerStat = TWCComms.Communicator.Instance.Cover
        //TWCComms.Communicator.Instance.Main = this;
        //TWCMainMission = TWCComms.Communicator.Instance.Main;
        //TWCStatsMission = TWCComms.Communicator.Instance.Stats;
        //string s1= TWCComms.Communicator.Instance.string1;
        //Console.WriteLine("Statsstring: " + s1);
        //Console.WriteLine("GetType1: " + GetType().ToString());
        //Console.WriteLine("GetType2: " + s1.GetType().ToString());
        //Console.WriteLine("GetType3: " + TWCMainMission.GetType().ToString());
        //Console.WriteLine("GetType4: " + (TWCComms.Communicator.Instance.Stats == null).ToString());
        //Console.WriteLine("GetType5: " + (TWCComms.Communicator.Instance.Main.GetType().ToString()));
        //Console.WriteLine("GetType6: " + TWCStatsMission.stb_LocalMissionIniDirectory);
        //Console.WriteLine("GetType19: " + TWCStatsMission.stb_LocalMissionIniDirectory);
        //Console.WriteLine("GetType20: " + TWCStatsMission.ot_GetCivilianBombings("TWC_Flug"));

        //Assembly a = new Assembly();
        //Assembly assembly = typeof(maddox.game.AMission).Assembly;
        //Assembly assembly = Assembly.GetExecutingAssembly();
        //Assembly assembly = Assembly.GetCallingAssembly();
        //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //var namespaces = assembly.GetTypes().Select(t => t.Namespace).Distinct();
        /*
        foreach (var assembly in assemblies)
        {
            var namespaces = assembly.GetTypes();
            foreach (var n in namespaces)
            {
                Console.WriteLine("NS " + n.ToString());

            }
        }*/
        //coord.Singleton.Instance.Main = this;


        //String executablePath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //Console.WriteLine("NS " + executablePath);

        //Mission m = Battle.missions[missionNumber];
        /* if (m.stb_StatsINIFilename !=null & m.stb_StatsINIFilename=="stats.ini")
        {
            TWCStatsMission = m;
            Console.WriteLine("StatsMission! " + m.stb_StatsINIFilename);
        }*/

        /* 
         * ban LOAD blacklist.txt
         * */


    }

    /************************************************************
     * MENU SYSTEM
     * *********************************************************/

    bool dmgOn = false;
    bool EndMissionSelected = false;
    bool debugMenu = false;
    bool debugSave;
    int radar_realismSave;
    private void setMainMenu(Player player)
    {
        //GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Server Options - Users" }, new bool[] { true });


        //{true/false bool array}: TRUE here indicates that the choice is a SUBMENU so that when it is selected the user menu will be shown.  If FALSE the user menu will disappear.  Also it affects the COLOR of the menu items, which seems to be designed to indicate whether the choice is going to DO SOMETHING IMMEDIATELY or TAKE YOU TO ANOTHER MENU

        GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Enemy Activity Map Overview", "More...", "Objectives Complete", "Objectives Remaining", "Convoys and Other Suggested Targets", "Recon Photo Results with Coordinates", "Take Recon Photo" }, new bool[] { false, false, false, true, false, false, false, false, false });

    }

    private void setSubMenu1(Player player)
    {
        if (admin_privilege_level(player) >= 1)
        {

            string rollovertext = "(admin) End mission now/roll over to next mission";
            if (EndMissionSelected) rollovertext = "(admin) CANCEL End mission now command";
            if (admin_privilege_level(player) == 2)
                GamePlay.gpSetOrderMissionMenu(player, true, 2, new string[] { "(admin) Show detailed damage reports for all players (toggle)", "(admin) Toggle debug mode", "", rollovertext, "Return to User Menu" }, new bool[] { false, false, false, false, true });
            else GamePlay.gpSetOrderMissionMenu(player, true, 2, new string[] { "", "", "", rollovertext, "Return to User Menu" }, new bool[] { false, false, false, false, true });
        }
        else
        {
            setMainMenu(player);

        }
    }
    private void setSubMenu2(Player player)
    {
        if (admin_privilege_level(player) >= 1)
        {
            //ADMIN option is set to #9 for two reasons: #1. We can add or remove other options before it, up to 8 other options, without changing the Tab-4-4-9 admin access.  #2. To avoid accessing the admin menu (and it's often DANGEROUS options) by accident
            //{true/false bool array}: TRUE here indicates that the choice is a SUBMENU so that when it is selected the user menu will be shown.  If FALSE the user menu will disappear.  Also it affects the COLOR of the menu items, which seems to be designed to indicate whether the choice is going to DO SOMETHING IMMEDIATELY or TAKE YOU TO ANOTHER MENU
            GamePlay.gpSetOrderMissionMenu(player, true, 2, new string[] { "Your Career Summary", "Your Session Summary", "Netstats/All Player Summary", "More...", "Team Session Summary", "Current Campaign Status", "Detail Campaign Team/Session", "Let AI take over 2nd Position", "Admin Options" }, new bool[] { false, false, false, true, false, false, false, false, true });
        }
        else
        {
            GamePlay.gpSetOrderMissionMenu(player, true, 2, new string[] { "Your Career Summary", "Your Session Summary", "Netstats/All Player Summary", "More...", "Team Session Summary", "Current Campaign Status", "Detail Campaign Team/Session" }, new bool[] { false, false, false, true, false, false, false });
        }
    }
    private void setSubMenu3(Player player)
    {
        GamePlay.gpSetOrderMissionMenu(player, true, 3, new string[] { "Radar & airport damage summary", "Nearest friendly airport", "On friendly territory?", "More...", "Aircraft Supply", "Aircraft available to you", "Aircraft lost this mission", "Time left in mission" }, new bool[] { false, false, false, true, false, false, false, false });
    }
    private void setSubMenu4(Player player)
    {
        GamePlay.gpSetOrderMissionMenu(player, true, 4, new string[] { "Knickebein - On/Next", "Knickebein - Off", "Knickebein - Current KB Info", "Back...", "Knickebein - List", "Cover - Auto-select cover aircraft", "Cover - List available aircraft", "Cover - Aircraft position", "Cover - Release Aircraft to land" }, new bool[] { true, false, false, true, false, false, false, false, false });
    }


    //object plnameo= GamePlay.gpPlayer().Name();  
    //string plname= GamePlay.gpPlayer().Name() as string;
    public override void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex)
    {
        //base.OnOrderMissionMenuSelected(player, ID, menuItemIndex); //2015/05/16 - not sure why this was missing previously? We'll see . . .

        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;

        /*****************************************************
         * 
         * ADMIN SUBMENU (2nd submenu, ID ==1, Tab-4-4-9)
         * 
         *****************************************************/
        if (ID == 1)
        { // main menu

            if (menuItemIndex == 0)
            {
                //setSubMenu1(player);
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
                        twcLogServer(new Player[] { player }, "Detailed damage reports will be shown for all players", new object[] { });

                    }
                    else
                    {
                        GamePlay.gpHUDLogCenter("Will not show damage on all aircraft");
                        twcLogServer(new Player[] { player }, "Detailed damage reports turned off", new object[] { });
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
                        twcLogServer(new Player[] { player }, "Debug & detailed radar ON for all users - extra debug messages & instant, detailed radar", new object[] { });
                        radar_realismSave = RADAR_REALISM;
                        DEBUG = true;
                        RADAR_REALISM = 0;

                    }
                    else
                    {
                        twcLogServer(new Player[] { player }, "Debug & detailed radar OFF", new object[] { });
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
                        twcLogServer(new Player[] { player }, str.Substring(i, Math.Min(maxChunkSize, str.Length - i)), new object[] { });
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
                        twcLogServer(new Player[] { player }, "ENDING MISSION!! If you want to cancel the End Mission command, use Tab-4-4-9-4 again.  You have 30 seconds to cancel.", new object[] { });
                        Timeout(30, () =>
                        {
                            if (EndMissionSelected)
                            {
                                EndMission(0);
                            }
                            else
                            {
                                twcLogServer(new Player[] { player }, "End Mission CANCELLED; Mission continuing . . . ", new object[] { });
                                twcLogServer(new Player[] { player }, "If you want to end the mission, you can use the menu to select Mission End again now.", new object[] { });
                            }

                        });

                    }
                    else
                    {
                        twcLogServer(new Player[] { player }, "End Mission CANCELLED; Mission will continue", new object[] { });
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
                twcLogServer(new Player[] { player }, "Re-spawn: This option not working yet", new object[] { });
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
                    twcLogServer(new Player[] { player }, player.Name() + " is not authorized", new object[] { }); 
                    setSubMenu1( player );
                  }
                 */

            //}
        }

        /*****************************************************
        * 
        * USER SUBMENU (4th submenu, ID == 4, Tab-4-4-4-4)
        * 
        *****************************************************/
        //   GamePlay.gpSetOrderMissionMenu(player, true, 3, new string[] { "Knickebein - On/Next", "Knickebein - Off", "Knickebein - Current KB Info", "Back...", "Knickebein - List", "Cover - Auto-select cover aircraft", "Cover - List available aircraft", "Cover - Aircraft position", "Cover - Release Aircraft to land" }, new bool[] { false, false, false, true, false, false, false, false, false});
        else if (ID == 4)
        { // main menu

            if (menuItemIndex == 0)
            {
                setSubMenu3(player);
                //setMainMenu(player);
            }

            //Knickebein on/next
            else if (menuItemIndex == 1)
            {
                if (TWCKnickebeinMission != null)
                {
                    TWCKnickebeinMission.KniOnStartOrNext(player);
                }
                setSubMenu4(player);
            }
            //Knickebein Off
            else if (menuItemIndex == 2)
            {
                if (TWCKnickebeinMission != null)
                {
                    TWCKnickebeinMission.KniStop(player);
                }
                setMainMenu(player);
            }
            //Knickebein - current KB info
            else if (menuItemIndex == 3)
            {
                if (TWCKnickebeinMission != null)
                {
                    TWCKnickebeinMission.KniInfo(player);
                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 4)  //MORE (next) menu
            {
                setMainMenu(player);
            }
            //Knickebein - List
            else if (menuItemIndex == 5)
            {
                if (TWCKnickebeinMission != null)
                {
                    TWCKnickebeinMission.KniList(player);
                }
                setMainMenu(player);

            }
            //Cover - select/start
            else if (menuItemIndex == 6)
            {
                if (covermission != null)
                {
                    covermission.checkoutCoverAircraft(player, "");
                }

                setMainMenu(player);
            }
            //Cover - List available cover a/c
            else if (menuItemIndex == 7)
            {
                if (covermission != null)
                {
                    covermission.listCoverAircraftCurrentlyAvailable((ArmiesE)player.Army(), player);
                }
                setMainMenu(player);

            }
            //Cover - Position of your cover a/c
            else if (menuItemIndex == 8)
            {
                if (covermission != null)
                {
                    //string res = TWCCoverMission.listPositionCurrentCoverAircraft(player);
                    string res = covermission.listPositionCurrentCoverAircraft(player);
                }
                setMainMenu(player);
            }
            //Cover - CLand
            else if (menuItemIndex == 9)
            {
                if (covermission != null)
                {
                    covermission.landCoverAircraft(player);
                }
                setMainMenu(player);
            }
            else
            { //make sure there is a catch-all ELSE or ELSE menu screw-ups WILL occur
                setMainMenu(player);
            }
        }

        /*****************************************************
         * 
         * USER SUBMENU (3rd submenu, ID == 3, Tab-4-4-4)
         * 
         *****************************************************/
        //   GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Detailed Campaign Summary", "Radar & Airport damage summary", "Nearest friendly airport", "On friendly territory?","Back..."}, new bool[] { false, false, false, false, false } );
        else if (ID == 3)
        { // main menu

            if (menuItemIndex == 0)
            {
                setSubMenu2(player);
                //setMainMenu(player);
            }

            //Radar & Airport damage summary, same as <ap
            else if (menuItemIndex == 1)
            {
                //ListAirfieldTargetDamage(player, -1);//list damaged airport of both teams
                Task.Run(() => {
                    ListRadarTargetDamage(player, -1);
                    Timeout(2, () => { ListAirfieldTargetDamage(player, -1); });
                });
                setMainMenu(player);
            }
            //Directions to nearest friendly airport
            else if (menuItemIndex == 2)
            {
                Timeout(0.2, () =>
                {
                    string msg6 = "Checking your position via radar to find nearest friendly airport. Stand by . . . ";
                    twcLogServer(new Player[] { player }, msg6, null);
                });

                if (offRadar(player)) {
                    Timeout(6, () =>
                    {
                        string msg6 = "HQ has been unable to locate your position!  Sorry and good luck!";
                        twcLogServer(new Player[] { player }, msg6, null);
                    });

                } else {

                    Timeout(12, () =>
                    {

                        //double d = Calcs.distanceToNearestAirport(GamePlay, aircraft as AiActor);

                        AiAirport ap = Calcs.nearestAirport(GamePlay, aircraft as AiActor, player.Army());
                        AiActor a = ap as AiActor;

                        Point3d aPos = a.Pos();
                        double distanceToAirport_m = aircraft.Pos().distance(ref aPos);
                        double bearing_deg = Calcs.CalculateGradientAngle(aircraft.Pos(), a.Pos());
                        double bearing_deg10 = Calcs.GetDegreesIn10Step(bearing_deg);
                        string dis_string = (distanceToAirport_m / 1000).ToString("N0") + " km ";
                        if (player.Army() == 1) dis_string = (Calcs.meters2miles(distanceToAirport_m)).ToString("N0") + " mi ";

                        string message6 = dis_string + bearing_deg10.ToString("N0") + "° to the nearest friendly airport";
                        if (distanceToAirport_m < 2500) message6 = "You are AT the nearest friendly airport";
                        if (distanceToAirport_m > 100000000) message6 = "Nearest friendly airport not found";
                        twcLogServer(new Player[] { player }, message6, null);

                    });
                }
                setMainMenu(player);
            }
            //On Friendly or Enemy territory, same as <ter
            else if (menuItemIndex == 3)
            {
                if (player.Army() != null && aircraft != null)
                {

                    Timeout(0.2, () =>
                    {
                        string msg6 = "Checking your position via radar. Stand by . . . ";
                        twcLogServer(new Player[] { player }, msg6, null);
                    });

                    Timeout(12, () =>
                    {
                        int terr = GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y);
                        string msg6 = "You are in ENEMY territory";
                        if (terr == 00) msg6 = "You are in NEUTRAL territory";
                        if (player.Army() == terr) msg6 = "You are in FRIENDLY territory";
                        twcLogServer(new Player[] { player }, msg6, null);
                    });

                }
                setMainMenu(player);
            }
            else if (menuItemIndex == 4)  //MORE (next) menu
            {
                setSubMenu4(player);
            }
            else if (menuItemIndex == 5)
            {
                if (TWCSupplyMission != null) TWCSupplyMission.DisplayNumberOfAvailablePlanes(player.Army(), player, true);
                setMainMenu(player);
            }
            else if (menuItemIndex == 6)
            {
                //public string Display_AircraftAvailable_ByName(Player player, bool nextAC = false, bool display = true, bool html = false)
                //if (TWCStatsMission != null)
                //{
                    statsmission.Display_AircraftAvailable_ByName(player, nextAC: false, display: true, html: false);
                    Timeout(2.1, () =>
                    {
                        statsmission.Display_AircraftAvailable_ByName(player, nextAC: true, display: true, html: false);
                    });
                    Timeout(5.0, () =>
                    {
                        twcLogServer(new Player[] { player }, "Note that fighter & bomber pilot careers are separate & aircraft actually available depends on the ace & rank levels in the relevant career for that aircraft", null);
                    });


                //}
                setMainMenu(player);
            }

            else if (menuItemIndex == 7)
            {

                setMainMenu(player);
                if (TWCSupplyMission != null) TWCSupplyMission.ListAircraftLost(player.Army(), player, true, false);

            }
            else if (menuItemIndex == 8)
            {
                setMainMenu(player);
                //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
                showTimeLeft(player);
                //Experiment to see if we could trigger chat commands this way; it didn't work
                //twcLogServer(new Player[] { player }, "<air", new object[] { });
                //twcLogServer(new Player[] { player }, "<ter", new object[] { });                            }

            }
            else
            { //make sure there is a catch-all ELSE or ELSE menu screw-ups WILL occur
                setMainMenu(player);
            }
        }

        /*****************************************************
         * 
         * USER SUBMENU (2nd submenu, ID ==2, Tab-4-4)
         * 
         *****************************************************/
        //   GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Detailed Campaign Summary", "Airport damage summary", "Nearest friendly airport", "On friendly territory?","Back..."}, new bool[] { false, false, false, false, false } );
        else if (ID == 2)
        { // main menu

            if (menuItemIndex == 0)
            {
                setSubMenu1(player);
                //setMainMenu(player);
            }



            else if (menuItemIndex == 1)
            {
                /*
                 * Formerly <rank or <career for player
                 */
                setMainMenu(player);
                //if (TWCStatsMission != null) TWCStatsMission.Display_AceAndRank_ByName(player);
                //TWCStatsMission.Display_AceAndRank_ByName(player);
                statsmission.Display_AceAndRank_ByName(player);
            }
            else if (menuItemIndex == 2)
            {
                /*
                 * Formerly <session for player session stats
                 */
                setMainMenu(player);
                //if (TWCStatsMission != null) TWCStatsMission.Display_SessionStats(player);
                //TWCStatsMission.Display_SessionStats(player);
                statsmission.Display_SessionStats(player);
            }

            else if (menuItemIndex == 3)
            {
                /*
                 * Formerly <net for all player "netstats" 
                 */
                setMainMenu(player);
                //if (TWCStatsMission != null) TWCStatsMission.Display_SessionStatsAll(player, 0, true); //player, army (0=all), display or not
                //TWCStatsMission.Display_SessionStatsAll(player, 0, true, false); //player, army (0=all), display or not
                statsmission.Display_SessionStatsAll(player, 0, true, false); //player, army (0=all), display or not

            }
            else if (menuItemIndex == 4)  //MORE (next) menu
            {
                setSubMenu3(player);
            }
            else if (menuItemIndex == 5)
            {
                /*
                 * Detailed Team Stats for Session
                 */
                setMainMenu(player);
                //First objectives completed/Campaign points
                Timeout(0.2, () =>
                {
                    //if (player.Army() == 2) twcLogServer(new Player[] { player }, "Blue Primary Objectives: " + MissionObjectivesString[ArmiesE.Blue], new object[] { });
                    MO_ListRemainingPrimaryObjectives(player, player.Army());
                    twcLogServer(new Player[] { player }, "Blue Objectives Completed (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Blue], new object[] { });
                });

                Timeout(1.2, () =>
                {

                    //if (player.Army() == 1) twcLogServer(new Player[] { player }, "Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red], new object[] { });

                    twcLogServer(new Player[] { player }, "Red Objectives Completed (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Red], new object[] { });
                });
                //Then team stats (kills etc)
                //if (TWCStatsMission != null) TWCStatsMission.Display_SessionStatsTeam(player);
                statsmission.Display_SessionStatsTeam(player);
            }
            else if (menuItemIndex == 6)
            {
                /*
                 * Display Campaign Summary, same as <cam
                 */
                setMainMenu(player);
                Tuple<double, string> res = CalcMapMove("", false, false, player);
                double score = res.Item1 * 100;
                string mes = "Campaign score for this mission so far: ";
                if (score > 0) mes += "Red +" + score.ToString("n0");
                else if (score < 0) mes += "Blue +" + (-score).ToString("n0");
                else mes += "A tie!";
                twcLogServer(new Player[] { player }, mes, null);

                double newMapState = CampaignMapState + res.Item1;
                summarizeCurrentMapstate(newMapState, true, player);
            }
            else if (menuItemIndex == 7)
            {
                /*
                 * Detailed Campaign summary as in <camlong
                 */

                setMainMenu(player);

                Tuple<double, string> res = CalcMapMove("", false, true, player);
                //string outputmsg = res.Item2;
                //string msg = "";

                double newMapState = CampaignMapState + res.Item1;
                summarizeCurrentMapstate(newMapState, true, player);

            }
            else if (menuItemIndex == 8) 
            {
                /*
                 * Let AI take over player's 2nd position - essential for 110, JU-87, etc
                 */

                letAiTake2ndPosition(player);
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
                setMainMenu(player);
                Player[] all = { player };
                listPositionAllAircraft(player, player.Army(), false, radar_realism: RADAR_REALISM); //enemy a/c  
                if (DEBUG)
                {
                    DebugAndLog("Total number of AI aircraft groups currently active:");
                    if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
                    {

                        int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                        DebugAndLog(totalAircraft.ToString());
                        //twcLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
                    }
                }
            }
            else if (menuItemIndex == 2)
            {
                setMainMenu(player);
                Player[] all = { player };
                listPositionAllAircraft(player, player.Army(), true, radar_realism: RADAR_REALISM); //friendly a/c           
                if (DEBUG)
                {
                    DebugAndLog("Total number of AI aircraft groups currently active:");
                    if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
                    {

                        int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                        DebugAndLog(totalAircraft.ToString());
                        //twcLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
                    }
                }
                //TIME REMAINING ETC//////////////////////////////////  
            }
            else if (menuItemIndex == 3)
            {
                showGiantSectorOverview(player, player.Army());
                setMainMenu(player);

            }
            else if (menuItemIndex == 5)
            {
                /*
                 * Display objectives completed 
                 */
                setMainMenu(player);
                twcLogServer(new Player[] { player }, "Completed Red Objectives (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):", new object[] { });
                twcLogServer(new Player[] { player }, (MissionObjectivesCompletedString[ArmiesE.Red]), new object[] { });
                Timeout(2, () =>
                twcLogServer(new Player[] { player }, "Completed Blue Objectives (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):", new object[] { }));
                Timeout(3, () =>
                twcLogServer(new Player[] { player }, (MissionObjectivesCompletedString[ArmiesE.Blue]), new object[] { }));
                stopAI();//for testing                
            }
            else if (menuItemIndex == 6)
            {
                /*
                 * Display objectives remaining
                 */
                setMainMenu(player);
                MO_ListRemainingPrimaryObjectives(player, player.Army(), 12);
            }
            else if (menuItemIndex == 7)
            {
                /*
                 * Display Suggested Alternate Objectives
                 */

                //ListSuggestedObjectives(player, Player.army, 5);
                setMainMenu(player);
                MO_ListSuggestedObjectives(player, player.Army(), 5);
            }
            else if (menuItemIndex == 8)
            {
                /*
                 * List Scouted Objectives
                 */
                setMainMenu(player);
                MO_ListScoutedObjectives(player, player.Army());

            }
            else if (menuItemIndex == 9)
            {
                /*
                 * Take Recon Photo
                 */
                setMainMenu(player);
                MO_TakeScoutPhoto(player, player.Army());
            }

            else if (menuItemIndex == 4)  //MORE (next) menu
            {
                setSubMenu2(player);
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

    /************************************************************
    * END - MENU SYSTEM
    * *********************************************************/

    //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
    public override void OnPlayerConnected(Player player)
    {
        string message;
        //Not starting it here due to Coop Start Mode
        //if (!MISSION_STARTED) DebugAndLog("First player connected; Mission timer starting");
        //MISSION_STARTED = true;        

        //MO_RecordPlayerScoutPhotos(player, false, true); //for testing

        if (MissionNumber > -1)
        {
            setMainMenu(player);

            twcLogServer(new Player[] { player }, "Welcome to " + CAMPAIGN_ID + ", " + player.Name(), new object[] { });
            //twcLogServer(null, "Mission loaded.", new object[] { });

            DateTime utcDate = DateTime.UtcNow;

            //utcDate.ToString(culture), utcDate.Kind
            //Write current time in UTC, what happened, player name
            message = utcDate.ToString("u") + " Connected " + player.Name();

            DebugAndLog(message);
            if (COOP_START_MODE)
            {
                Stb_Chat("CO-OP MISSION START MODE", null);
                Stb_Chat("CO-OP START: You can spawn on the ground and taxi but", null);
                Stb_Chat("DO NOT TAKE OFF OR AIR SPAWN until CO-OP mission start time", null);
            }
        }
    }

    //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
    public override void OnPlayerDisconnected(Player player, string diagnostic)
    {
        MO_SpoilPlayerScoutPhotos(player);

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
                            string cs = ""; 
                            //int p = part.ParameterTypes.I_VelocityIAS; 
                            double ias = (double) aircraft.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
                            twcLogServer(new Player[] { player }, "Plane: "  
                            + cs + " " + ias, new object[] { });
            */
            //We re-init menu & mission_started here bec. in some situations OnPlayerConnected never happens.  But, they
            //always must choose their army before entering the map, so this catches all players before entering the actual gameplay
            setMainMenu(player);
            if (!MISSION_STARTED) DebugAndLog("First player connected (OnPlayerArmy); Mission timer starting");
            MISSION_STARTED = true;
            //twcLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            twcLogServer(new Player[] { player }, "Welcome to " + CAMPAIGN_ID + ", " + player.Name(), new object[] { });
            //twcLogServer(null, "Mission loaded.", new object[] { });
        }
    }
    public override void Inited()
    {
        if (MissionNumber > -1)
        {

            setMainMenu(GamePlay.gpPlayer());
            twcLogServer(null, "Welcome to " + CAMPAIGN_ID, new object[] { });

        }
    }





    /****************************************
    * END - CHAT COMMANDS
    * **************************************/

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

    private bool resetmission_requested = false;
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
        else if (msg.StartsWith("<stock"))
        {
            string m = msg.Substring(6).Trim();
            if (admin_privilege_level(player) < 2)
            {
                if (TWCSupplyMission != null) TWCSupplyMission.DisplayNumberOfAvailablePlanes(player.Army(), player, true, false, m);
            }
            else
            {
                if (TWCSupplyMission != null) TWCSupplyMission.DisplayNumberOfAvailablePlanes(0, player, true, false, m);
            }
        }
        else if (msg.StartsWith("<lost"))
        {
            string m = msg.Substring(5).Trim();
            if (admin_privilege_level(player) < 2)
            {
                if (TWCSupplyMission != null) TWCSupplyMission.ListAircraftLost(player.Army(), player, true, false, m);
            }
            else
            {
                if (TWCSupplyMission != null) TWCSupplyMission.ListAircraftLost(0, player, true, false, m);
            }
        }

        else if (msg.StartsWith("<redstock") && admin_privilege_level(player) >= 2)
        {
            double md = redMultAdmin;
            string ms = msg.Substring(9).Trim();
            try { if (ms.Length > 0) md = Convert.ToDouble(ms); }
            catch (Exception ex) { }

            twcLogServer(new Player[] { player }, "RedStock admin multiplier set to " + md.ToString("n2") + ". Will add " + md.ToString("n2") + "X the regular mission aircraft stock increase at the end of this mission, plus any stock additions regularly due from mission success", null);
            twcLogServer(new Player[] { player }, "To change this value, just re-enter <redstock XX before the mission is over.", null);
            twcLogServer(new Player[] { player }, "To reset the value, enter <redstock 0", null);
            redMultAdmin = md;

        }
        else if (msg.StartsWith("<bluestock") && admin_privilege_level(player) >= 2)
        {
            double md = blueMultAdmin;
            string ms = msg.Substring(10).Trim();
            try { if (ms.Length > 0) md = Convert.ToDouble(ms); }
            catch (Exception ex) { }

            twcLogServer(new Player[] { player }, "BlueStock admin multiplier set to " + md.ToString("n2") + ". Will add " + md.ToString("n2") + "X the regular mission aircraft stock increase at the end of this mission, plus any stock additions regularly due from mission success", null);
            twcLogServer(new Player[] { player }, "To change this value, just re-enter <bluestock XX before the mission is over.", null);
            twcLogServer(new Player[] { player }, "To reset the value, enter <bluestock 0", null);
            blueMultAdmin = md;

        }
        else if (msg.StartsWith("<ai")) //let the player exit the secondary Place - needed for Stuka, 110, etc
        {
            letAiTake2ndPosition(player);
        }
        else if (msg.StartsWith("<obj"))
        {
            //only allow this for admins - mostly so that we can check these items via chat commands @ the console
            if (admin_privilege_level(player) >= 0) // >= 2 previously 
            {

                twcLogServer(new Player[] { player }, "***Please use Tab-4 menu for campaign status when possible", new object[] { });


                Timeout(0.2, () =>
                {

                    MO_ListRemainingPrimaryObjectives(player, player.Army());

                    //twcLogServer(new Player[] { player }, "Blue Primary Objectives: " + MissionObjectivesString[ArmiesE.Blue], new object[] { });
                    twcLogServer(new Player[] { player }, "Blue Objectives Completed (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Blue], new object[] { });
                });

                Timeout(1.2, () =>
                  {

                      //if (player.Army() == 1) twcLogServer(new Player[] { player }, "Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red], new object[] { });                      

                      twcLogServer(new Player[] { player }, "Red Objectives Completed (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Red], new object[] { });
                  });
            }
            else
            {
                twcLogServer(new Player[] { player }, "Please use Tab-4 menu for campaign status", new object[] { });
            }

        }
        else if (msg.StartsWith("<camlong")) //show current campaign state (ie map we're on) and also the campaign results for this mission so far, longer & more detailed analysis
        {
            //only allow this for admins - mostly so that we can check these items via chat commands @ the console
            if (admin_privilege_level(player) >= 0) // >= 2 previously 
            {
                twcLogServer(new Player[] { player }, "***Please use Tab-4 menu for campaign status when possible", new object[] { });
                Tuple<double, string> res = CalcMapMove("", false, true, player);
                //string outputmsg = res.Item2;
                //string msg = "";

                double newMapState = CampaignMapState + res.Item1;
                summarizeCurrentMapstate(newMapState, true, player);


            }
            else
            {
                twcLogServer(new Player[] { player }, "Please use Tab-4 menu for campaign status", new object[] { });
            }


        }
        else if (msg.StartsWith("<cam")) //show current campaign state (ie map we're on) and also the campaign results for this mission so far
        {
            //only allow this for admins - mostly so that we can check these items via chat commands @ the console
            if (admin_privilege_level(player) >= 0) // >= 2 previously 
            {
                twcLogServer(new Player[] { player }, "***Please use Tab-4 menu for campaign status when possible", new object[] { });

                Tuple<double, string> res = CalcMapMove("", false, false, player);
                double score = res.Item1 * 100;
                string mes = "Campaign score for this mission so far: ";
                if (score > 0) mes += "Red +" + score.ToString("n0");
                else if (score < 0) mes += "Blue +" + (-score).ToString("n0");
                else mes += "A tie!";
                twcLogServer(new Player[] { player }, mes, null);

                double newMapState = CampaignMapState + res.Item1;
                summarizeCurrentMapstate(newMapState, true, player);
            }
            else
            {
                twcLogServer(new Player[] { player }, "Please use Tab-4 menu for campaign status", new object[] { });
            }

        }
        else if (msg.StartsWith("<recon")) //take recon photo
        {
            twcLogServer(new Player[] { player }, "***Please use Tab-4-9 to take recon photos when possible", new object[] { });
            MO_TakeScoutPhoto(player, player.Army());
        }
        else if (msg.StartsWith("<record")) //record recon photo
        {
            MO_RecordPlayerScoutPhotos(player, check: true);
        }
        else if (msg.StartsWith("<coop start") && admin_privilege_level(player) >= 1)
        {
            twcLogServer(new Player[] { player }, "HELP: Use command '<coop XXX' to change the co-op start time to add XXX more minutes", null);
            twcLogServer(new Player[] { player }, "HELP: Use command '<coop start' to start mission immediately", null);
            if (COOP_START_MODE)
            {

                COOP_MODE_TIME_SEC = 0;
                twcLogServer(new Player[] { player }, "CO-OP Mission will START NOW!", null);
            }
            else
            {
                twcLogServer(new Player[] { player }, "<coop start command works only during initial Co-op Start Mode period", null);
            }

        }

        else if (msg.StartsWith("<coop") && admin_privilege_level(player) >= 1)
        {
            twcLogServer(new Player[] { player }, "HELP: Use command '<coop XXX' to change the co-op start time to add XXX minutes", null);
            twcLogServer(new Player[] { player }, "HELP: Use command '<coop start' to start mission immediately", null);
            if (COOP_START_MODE)
            {
                double time_sec = 5 * 60;
                string time_str = msg.Substring(5).Trim();
                double time_min = Convert.ToDouble(time_str);
                if (time_min != 0 || time_str == "0") time_sec = time_min * 60;


                COOP_MODE_TIME_SEC += time_sec;
                double time_left_sec = COOP_TIME_LEFT_MIN * 60 + time_sec;


                twcLogServer(new Player[] { player }, "CO-OP MODE start time added " + ((double)time_sec / 60).ToString("n1") + " minutes; ", null);
                twcLogServer(new Player[] { player }, (COOP_MODE_TIME_SEC / 60).ToString("n1") + " min. total Co-Op start period; " + (time_left_sec / 60).ToString("n1") + " min. remaining", null);
                Stb_Chat("CO-OP START MODE EXTENDED: " + (time_left_sec / 60).ToString("n1") + " min. until co-op start", null);
            }
            else
            {
                twcLogServer(new Player[] { player }, "<coop command works only during initial Co-op Start Mode period", null);
            }

        }
        else if (msg.StartsWith("<pos") && admin_privilege_level(player) >= 2)
        {
            //int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            //RADAR_REALISM = 0;
            listPositionAllAircraft(player, player.Army(), false, radar_realism: 0);

            Timeout(4, () =>
            {
                listPositionAllAircraft(player, player.Army(), true, radar_realism: 0);
            });
            //RADAR_REALISM = saveRealism;

        }
        else if (msg.StartsWith("<ps") && admin_privilege_level(player) >= 2)
        {
            listPositionAllAircraft(player, player.Army(), false, radar_realism: 1);

            Timeout(4, () =>
            {
                listPositionAllAircraft(player, player.Army(), true, radar_realism: 1);
            });

        }
        else if (msg.StartsWith("<gps") && admin_privilege_level(player) >= 2)
        {
            //Dictionary<AiAirGroup, AirGroupInfo> airGroupInfoDict = airGroupInfoCircArr.GetStack(0); //Most recent iteration of airgroup groupings
            Dictionary<AiAirGroup, AirGroupInfo> airGroupInfoDict = airGroupInfoCircArr.Get(0); //Most recent iteration of airgroup groupings
            foreach (AiAirGroup ag in airGroupInfoDict.Keys)
            {
                Console.WriteLine(airGroupInfoDict[ag].ToString());
            }

        }
        else if (msg.StartsWith("<rad") && admin_privilege_level(player) >= 2)
        {
            listPositionAllAircraft(player, player.Army(), false, radar_realism: RADAR_REALISM); //enemy a/c  
        }
        else if (msg.StartsWith("<exit") && admin_privilege_level(player) >= 2)
        {
            EndMission(0); //end immediately/for testing
        }
        else if (msg.StartsWith("<rctest") && admin_privilege_level(player) >= 2)
        {
            //Point3d? test = new Point3d(237074, 243644, 10000);
            //(69050,182277)
            Point3d? test = new Point3d(69050, 182277, 15000);
            MO_TakeScoutPhoto(player, player.Army(), 0, test);
        }
        else if (msg.StartsWith("<rcrecord") && admin_privilege_level(player) >= 2)
        {
            MO_RecordPlayerScoutPhotos(player, true, true);
        }
        else if (msg.StartsWith("<rcdest") && admin_privilege_level(player) >= 2)
        {
            MO_DestroyObjective("BTarget14R", true);
            //destroy a random airport
            Timeout(5, () =>
           {
               var apkeys = new List<AiAirport>(AirfieldTargets.Keys.Count);
               apkeys = AirfieldTargets.Keys.ToList();

               AiAirport ap = apkeys.ElementAt(random.Next(apkeys.Count));
               string Mission = AirfieldTargets[ap].Item2;

               twcLogServer(new Player[] { player }, "Destroying airport " + Mission, new object[] { });

               MO_DestroyObjective(Mission + "_airfield", true);
           });
        }
        else if (msg.StartsWith("<triggerobjectivetest") && admin_privilege_level(player) >= 2)
        {

            //foreach (string ID in MissionObjectivesList.Keys)
            //MO_TestObjectiveWithFlak(MissionObjectivesList[ID], 1, 1);
            string[] words = msg_orig.Split(' ');
            twcLogServer(new Player[] { player }, "Destroying all trigger-type objectives for {0} with enemy artillery.  Note that this will not drop bombs to test the non-trigger type objectives ", new object[] { words[1], MissionObjectivesList[words[1]].Name });
            MO_TestObjectiveWithFlak(MissionObjectivesList[words[1]], 4, 4);

        }
        else if (msg.StartsWith("<resetmission") && admin_privilege_level(player) >= 2)
        {
            if (!resetmission_requested) {
                resetmission_requested = true;
                Timeout(30, () => { resetmission_requested = false; });
                twcLogServer(new Player[] { player }, "CONFIRMATION REQUIRED!!!!!! To reset all current mission objectives for both sides, objectives scouted, re-read mission objectives afresh, current team scores, enter <resetmission again within 30 seconds", new object[] { });
                return;

            }

            twcLogServer(new Player[] { player }, "RESETTING ALL mission objectives, objectives scouted, re-read mission objectives afresh, current team scores", new object[] { });

            MissionObjectivesList = new Dictionary<string, MissionObjective>();  //zero out the mission objectives list (otherwise when we run the routine below they will ADD to anything already there)
            mission_objectives.RadarPositionTriggersSetup();
            mission_objectives.MissionObjectiveTriggersSetup();
            MO_MissionObjectiveAirfieldsSetup(this, GamePlay, addNewOnly: false); //must do this after the Radar & Triggers setup, as it uses info from those objectives    
            MO_SelectPrimaryObjectives(1, 0, fresh: true);
            MO_SelectPrimaryObjectives(2, 0, fresh: true);
            mission_objectives.SelectSuggestedObjectives(ArmiesE.Blue);
            mission_objectives.SelectSuggestedObjectives(ArmiesE.Red);
            MissionObjectiveScore[ArmiesE.Red] = 0;
            MissionObjectiveScore[ArmiesE.Blue] = 0;
            MissionObjectivesCompletedString[ArmiesE.Red] = "";
            MissionObjectivesCompletedString[ArmiesE.Blue] = "";

            //MO_HandleGeneralStaffPlacement(); //Gen'l staff is already placed & this will double place it
            //MO_LoadAllPrimaryObjectiveFlak(mission_objectives.FlakMissions);  //loading double flak won't be good
            //MO_InitializeAllObjectives(); //this will double-load autoflak, any statics.  So just have to re-start server to make any changes
        }


        else if (msg.StartsWith("<testturnred") && admin_privilege_level(player) >= 2)
        {
            MO_CheckObjectivesComplete(TestingOverrideArmy: 1);
        }
        else if (msg.StartsWith("<testturnblue") && admin_privilege_level(player) >= 2)
        {
            MO_CheckObjectivesComplete(TestingOverrideArmy: 2);
        }

        else if (msg.StartsWith("<smoke") && admin_privilege_level(player) >= 2)
        {
            Point3d pos = new Point3d(10000, 100000, 0);
            if (player.Place() != null) pos = player.Place().Pos();
            Calcs.loadSmokeOrFire(GamePlay, this, pos.x + random.Next(100) + 10, pos.y + random.Next(100) + 10, 0, "BuildingFireBig", duration_s: 6 * 3600);
            Calcs.loadSmokeOrFire(GamePlay, this, pos.x + random.Next(100) + 10, pos.y + random.Next(100) + 10, 0, "BuildingFireSmall", duration_s: 6 * 3600);
            Calcs.loadSmokeOrFire(GamePlay, this, pos.x + random.Next(100) + 10, pos.y + random.Next(100) + 10, 0, "Smoke1", duration_s: 6 * 3600);
        }
        else if (msg.StartsWith("<apdest") && admin_privilege_level(player) >= 2)
        {
            AiAirport ap = Calcs.nearestAirport(GamePlay, player.Place().Pos(), player.Army());
            AirfieldDisable(ap, 1);

        }
        //string firetype = "BuildingFireSmall"; //small-ish
        //if (mass_kg > 200) firetype = "BigSitySmoke_1"; //500lb bomb or larger  //REALLY huge
        //if (mass_kg > 200) firetype = "Smoke1"; //500lb bomb or larger //larger


        else if (msg.StartsWith("<pad"))
        {

            if (admin_privilege_level(player) >= 2)
            {
                showGiantSectorOverview(player, 2);  //Blue enemies
                showGiantSectorOverview(player, 1); //Red enemies
                showGiantSectorOverview(player, 0); //all
            }
            else showGiantSectorOverview(player, player.Army()); //enemies of current player
            /*
            Timeout(0.1, () => twcLogServer(new Player[] { player }, "***Schematic Map Overview of Enemy Activity***", null));
            Timeout(0.1, () => twcLogServer(new Player[] { player }, "Airgroups:Aircraft in each Large Map Keypad area", null));
            Timeout(0.1, () => twcLogServer(new Player[] { player }, "For more details, ask your Commander or Radar Operator to consult the Contact Plotting Table: ", null));          
            //Console.WriteLine("Giant: " + GiantSectorOverview.ToString());
            Timeout(0.1, () =>
            {
                for (int i = 2; i > -1; i--)
                {

                    twcLogServer(new Player[] { player }, "{0:D3}:{1:D3} {2:D3}:{3:D3} {4:D3}:{5:D3} ", new object[] {
                    GiantSectorOverview[player.Army()][i*3+1, 0], GiantSectorOverview[player.Army()][i*3+1, 1],
                    GiantSectorOverview[player.Army()][i*3+2, 0], GiantSectorOverview[player.Army()][i*3+2, 1],
                    GiantSectorOverview[player.Army()][i*3+3, 0], GiantSectorOverview[player.Army()][i*3+3, 1]

                });
                }
            });
            */
        }
        else if (msg.StartsWith("<apall") && admin_privilege_level(player) >= 2)
        {
            ListAirfieldTargetDamage(player, -1, true);//list ALL airports, damaged or not, of both teams
        }
        else if (msg.StartsWith("<ap"))
        {
            //only allow this for admins - mostly so that we can check these items via chat commands @ the console
            if (admin_privilege_level(player) >= 2)
            {

                //ListAirfieldTargetDamage(player, -1); //list damaged airport of both teams
                ListRadarTargetDamage(player, -1);
                Timeout(2, () => { ListAirfieldTargetDamage(player, -1); });
            }
            else
            {
                twcLogServer(new Player[] { player }, "Please use Tab-4 menu to check airport status", new object[] { });
            }
        }
        else if (msg.StartsWith("<server") && admin_privilege_level(player) >= 2)
        {
            string tr = msg_orig.Substring(7).Trim();
            SERVER_ID_SHORT = tr;
            twcLogServer(new Player[] { player }, "Server renamed to " + tr + " for the remainder of this session.", new object[] { });

        }
        else if (msg.StartsWith("<trigger") && admin_privilege_level(player) >= 2)
        {


            string tr = msg_orig.Substring(8).Trim();

            twcLogServer(new Player[] { player }, "Trying to activate trigger " + tr, new object[] { });

            if (GamePlay.gpGetTrigger(tr) != null)
            {
                GamePlay.gpGetTrigger(tr).Enable = true;
                //GamePlay.gpGetTrigger(tr).Active = true;
                twcLogServer(new Player[] { player }, "Enabled trigger " + tr, new object[] { });
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
                twcLogServer(new Player[] { player }, "Activating action " + tr, new object[] { });
            }
            else
            {
                twcLogServer(new Player[] { player }, "Didn't find action " + tr + "! No action taken.", new object[] { });
            }



        }
        else if (msg.StartsWith("<nump") && admin_privilege_level(player) >= 2)
        {
            int nump = Calcs.gpNumberOfPlayers(GamePlay);
            twcLogServer(new Player[] { player }, "stopAI: " + nump.ToString() + " players currently online", new object[] { });

        }
        else if (msg.StartsWith("<warp") && admin_privilege_level(player) >= 2)
        {

            TWCComms.Communicator.Instance.WARP_CHECK = !TWCComms.Communicator.Instance.WARP_CHECK;
            twcLogServer(new Player[] { player }, "WARP_CHECK is set to " + TWCComms.Communicator.Instance.WARP_CHECK.ToString(), new object[] { });

        }

        else if (msg.StartsWith("<testend") && admin_privilege_level(player) >= 2)
        {

            SaveMapState("", false); //here is where we save progress/winners towards moving the map & front one way or the other; also saves the Supply State

            CheckStatsData(); //Save campaign/map state just before final exit.  This is important because when we do (GamePlay as GameDef).gameInterface.CmdExec("exit"); to exit, the -stats.cs will read the CampaignSummary.txt file we write here as the final status for the mission in the team stats.
            //(TWCStatsMission as AMission).OnBattleStoped();  This works but we don't want to do it, everything breaks afterwards!
            MapStateSaved = false; //fooling it, this will mess with the campaign status etc, but for testing . . . 
            twcLogServer(new Player[] { player }, "End test complete savemapstate", new object[] { });

        }
        else if (msg.StartsWith("<debugon") && admin_privilege_level(player) >= 2)
        {

            DEBUG = true;
            twcLogServer(new Player[] { player }, "Debug is on", new object[] { });

        }

        else if (msg.StartsWith("<debugoff") && admin_privilege_level(player) >= 2)
        {

            DEBUG = false;
            twcLogServer(new Player[] { player }, "Debug is off", new object[] { });

        }
        else if (msg.StartsWith("<logon") && admin_privilege_level(player) >= 2)
        {

            LOG = true;
            twcLogServer(new Player[] { player }, "Log is on", new object[] { });

        }
        else if (msg.StartsWith("<logoff") && admin_privilege_level(player) >= 2)
        {

            LOG = false;
            twcLogServer(new Player[] { player }, "Log is off", new object[] { });

        }
        else if (msg.StartsWith("<endm") && admin_privilege_level(player) >= 2)
        {
            if (EndMissionSelected == false)
            {
                EndMissionSelected = true;
                twcLogServer(new Player[] { player }, "ENDING MISSION!! If you want to cancel the End Mission command, use <endm again.  You have 10 seconds to cancel.", new object[] { });
                Timeout(10, () =>
                {
                    if (EndMissionSelected)
                    {
                        EndMission(0);
                    }
                    else
                    {
                        twcLogServer(new Player[] { player }, "End Mission CANCELLED; Mission continuing . . . ", new object[] { });
                        twcLogServer(new Player[] { player }, "If you want to end the mission, you can use the menu to select Mission End again now.", new object[] { });
                    }

                });

            }
            else
            {
                twcLogServer(new Player[] { player }, "End Mission CANCELLED; Mission will continue", new object[] { });
                EndMissionSelected = false;

            }
        }

        else if (msg.StartsWith("<test") && admin_privilege_level(player) >= 2)
        {
            var radar_messages = new Dictionary<string, string> {
                {  "1", "a;dslfkaj;ldkf ja;ldfj a;slf ja;slfjjfeifjeij aij fiajf iasjf iajdf iajf aisjfd aisdfj aisfdj asidfj asifdj aisf jaisfj " },
                {  "2", "a;dslfkaj;ldkf ja;ldfj a;slf ja;slfjjfeifjeij aij fiajf iasjf iajdf iajf aisjfd aisdfj aisfdj asidfj asifdj aisf jaisfj " },
                {  "3", "a;dslfkaj;ldkf ja;ldfj a;slf ja;slfjjfeifjeij aij fiajf iasjf iajdf iajf aisjfd aisdfj aisfdj asidfj asifdj aisf jaisfj " },
                {  "4", "a;dslfkaj;ldkf ja;ldfj a;slf ja;slfjjfeifjeij aij fiajf iasjf iajdf iajf aisjfd aisdfj aisfdj asidfj asifdj aisf jaisfj " },
                {  "5", "a;dslfkaj;ldkf ja;ldfj a;slf ja;slfjjfeifjeij aij fiajf iasjf iajdf iajf aisjfd aisdfj aisfdj asidfj asifdj aisf jaisfj " }
                };
            double delay = 0.05;
            foreach (var mess in radar_messages)
            {
                delay += 0.05;
                Timeout(delay, () =>
                {
                    gpLogServerAndLog(new Player[] { player }, mess.Value, null);
                });

            }
        }
        else if (msg.StartsWith("<admin") && admin_privilege_level(player) >= 2)
        {

            twcLogServer(new Player[] { player }, "Admin commands: <stock <lost <bluestock <redstock <coop <trigger <action <debugon <debugoff <logon <logoff <endmission", new object[] { });
            twcLogServer(new Player[] { player }, "<resetmission", new object[] { });
            twcLogServer(new Player[] { player }, "Server test commands: <rctest, <testturnred, <testturnblue, <rcdest, <rcrecord, <triggerobjectivetest objectiveID", new object[] { });

        }
        else if ((msg.StartsWith("<help") || msg.StartsWith("<")) &&
            //Don't give our help when any of these typical -stats.cs chat commands are entered
            !(msg.StartsWith("<car") || msg.StartsWith("<ses") || msg.StartsWith("<rank") || msg.StartsWith("<rr")
            || msg.StartsWith("<ter") || msg.StartsWith("<air") || msg.StartsWith("<ac") || msg.StartsWith("<nextac")
            || msg.StartsWith("<net") || msg.StartsWith("<k") || msg.StartsWith("<cov"))

            )
        {
            Timeout(0.1, () =>
            {
                string m = "Commands: <tl Time Left; <rr Rearm/reload; <ai let AI take over gunner position; <recon take recon photo; <record send recon photos to HQ";
                if (admin_privilege_level(player) >= 2) m += "; <admin";
                twcLogServer(new Player[] { player }, m, new object[] { });
                //twcLogServer(new Player[] { player }, "<ap & <apall Airport condition", new object[] { });
                //twcLogServer(new Player[] { player }, "<coop Use Co-Op start mode only @ beginning of mission", new object[] { });
                //GamePlay.gp(, from);
            });
        }
    }

    /****************************************
    * END - CHAT COMMANDS
    * **************************************/

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

    public void CheckAndChangeStartTime()
    {
        bool ret = true;
        double lastMissionCurrentTime_hr = 0;
        //MO_WriteMissionObject(GamePlay.gpTimeofDay(), "MissionCurrentTime", wait);
        object mo = MO_ReadMissionObject(lastMissionCurrentTime_hr, "MissionCurrentTime");
        if (mo != null) Console.WriteLine("Read " + mo.GetType().ToString());
        else Console.WriteLine("No read of " + "MissionCurrentTime");
        if (mo != null) ret = double.TryParse(mo.ToString(), out lastMissionCurrentTime_hr); //var d = double.TryParse(o.ToString(), out d); 
        else ret = false;

        double currTime = GamePlay.gpTimeofDay();
        double desiredStartTime_hrs = DESIRED_MISSION_START_TIME_HRS;

        twcLogServer(null,"CheckStartTime: curr/desired start time: {0:F3} {1:F3} ", currTime, desiredStartTime_hrs);
        //if the last saved mission time is a long ways from the current mission time, AND there is still more than 5 hrs
        //left before the preferred end mission time, then we'll restart the mission at/near our last desired start time
        if (ret && Math.Abs(lastMissionCurrentTime_hr - currTime) < 30 && END_MISSION_TIME_HRS - lastMissionCurrentTime_hr > 5)
            desiredStartTime_hrs = lastMissionCurrentTime_hr;
        twcLogServer(null, "CheckStartTime: curr/desired start time: {0:F3} {1:F3} ", currTime, desiredStartTime_hrs);

        if (Math.Abs(desiredStartTime_hrs - currTime) < 0.5) return; //0.5 == 30 minutes, 1/2 hour

        string desiredString = "  TIME " + desiredStartTime_hrs.ToString("F5");

        twcLogServer(null, "CheckStartTime: curr/desired start time: {0:F3} {1:F3}; Changing to {2}", currTime, desiredStartTime_hrs, desiredString);

        string filepath_mis = stb_FullPath + @"/" + MISSION_ID + ".mis";
        string filepath_mis_save = stb_FullPath + @"/" + MISSION_ID + ".mis_save";
        var backPath = STATSCS_FULL_PATH + CAMPAIGN_ID + @" campaign backups/";
        DateTime dt = DateTime.UtcNow;
        string filepath_misback_date = backPath + MISSION_ID + ".mis-" + dt.ToString("yyyy-MM-dd-HHmmss");

        try
        {
            if (File.Exists(filepath_mis_save)) { File.Delete(filepath_mis_save); }
            File.Copy(filepath_mis, filepath_mis_save); //We could use File.Move here if we want to eliminate the previous .ini file before writing new data to it, thus creating an entirely new .ini.  But perhaps better to just delete specific sections as we do below.
            Console.WriteLine("MO_Write MIS #1a");
        }
        catch (Exception ex) { Console.WriteLine("MO_Copy MIS Inner ERROR: " + ex.ToString()); return; }

        string missionFile = File.ReadAllText(filepath_mis);


        RegexOptions ro = RegexOptions.IgnoreCase | RegexOptions.Multiline;
        missionFile = Regex.Replace(missionFile, @"^\s*TIME\s*\d*\.?\d*\s*$", desiredString, ro);//looks for a line with something like <spaces> TIME <spaces> 34.234 .. Case irrelevant = replaces it


        //  TIME 4.50000011501834

        try
        {
            if (File.Exists(filepath_misback_date)) { File.Delete(filepath_misback_date); } //shouldn't exist, but just in case
            File.Move(filepath_mis, filepath_misback_date); //Move currently active copy to the backups folder
            Console.WriteLine("MO_Write MIS #2a");
        }
        catch (Exception ex) { Console.WriteLine("MO_Move MIS Inner2 ERROR: " + ex.ToString()); return; }

        try
        {
            File.WriteAllText(filepath_mis, missionFile, Encoding.UTF8);
            Console.WriteLine("MO_Write MIS #3a");
        }
        catch (Exception ex)
        {
            Console.WriteLine("MO_Write MIS Inner3 ERROR: " + ex.ToString());
            try
            {
                if (File.Exists(filepath_mis)) { File.Delete(filepath_mis); }//We're assuming this is screwed up somehow, so delete it
                File.Copy(filepath_mis_save, filepath_mis); //Move back the copy we made
                Console.WriteLine("MO_Write MIS #3b");
            }
            catch (Exception ex1) { Console.WriteLine("MO_Copy !!!!!!!!!!!!!!!!!!!!SERIOUS ERROR!!!!!!!!!!!!!!!!!!!!!!!!!!!! rewrite of .mis file failed and couldn't copy the backup to replace it!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! " + Environment.NewLine + ex1.ToString()); return; }
            return; //give up
        }
        //OK, new .mis file with new time is in place, now restart & run it!

        MO_WriteMissionObject(desiredStartTime_hrs, "MissionCurrentTime", true); //if we don't write this out here we get into a race condition!  This will set our expected time on restart, and that will match the actual running time, and everyone is happy

        //We are NOT saving any mission state here, we MUST only run this routine at the very beginning of the mission, before anything has happened.
        twcLogServer(null, "Restarting Mission to change start time of day . . . ", new object[] { });
        GamePlay.gpHUDLogCenter("Restarting Mission to change start time of day . . . ");
        DebugAndLog("Restarting Mission to change start time of day to " + desiredString);
        Console.WriteLine("Restarting Mission to change start time of day to " + desiredString);

        //OK, trying this for smoother exit (save stats etc)
        //(TWCStatsMission as AMission).OnBattleStoped();//This really horchs things up, basically things won't run after this.  So save until v-e-r-y last.
        //OK, we don't need to do the OnBattleStoped because it is called when you do CmdExec("exit") below.  And, if you run it 2X it actually causes problems the 2nd time.
        //Here we DON"T want a smooth exit saving everything.  Saving everything messes everything up. Just quit.
        /*if (GamePlay is GameDef)
        {
            (GamePlay as GameDef).gameInterface.CmdExec("exit");
        }*/
        //Process.GetCurrentProcess().Kill();
        Thread.Sleep(.01); //allow messages to show up/be logged?
        Environment.Exit(0);
    }
    //Ranges 0 to 1.  0= just started, 1=full mission time complete
    public double calcProportionTimeComplete()
    {
        double tickSinceStarted = Time.tickCounter() - START_MISSION_TICK;
        double perc = tickSinceStarted / ((double)END_MISSION_TICK);

        if (perc < 0) perc = 0;
        if (perc > 1) perc = 1;
        return perc;
    }

    public int calcTimeLeft_min()
    {

        Tick_Mission_Time = END_MISSION_TICK - Time.tickCounter();
        var Mission_Time = Tick_Mission_Time / 2000;
        TimeSpan Convert_Ticks_min = TimeSpan.FromMinutes(Mission_Time);
        //string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);

        //Method #2 - if our last allowed time in the evening is earlier than this, then we go with that instead
        double timeLeft = END_MISSION_TIME_HRS - GamePlay.gpTimeofDay();
        TimeSpan Convert_Hours_min = TimeSpan.FromMinutes(timeLeft);

        if (Convert_Hours_min.CompareTo(Convert_Ticks_min) <= 0) return Convert.ToInt32(Convert_Hours_min.TotalMinutes);  //whichever happens **soonest** we return
        else return Convert.ToInt32(Convert_Ticks_min.TotalMinutes);
    }

    //Calcs minutes left as an int
    public string calcTimeLeft()
    {

        //Method #1 - ticks/ max tick time allowed for mission
        Tick_Mission_Time = END_MISSION_TICK - Time.tickCounter();
        var Mission_Time = Tick_Mission_Time / 2000;
        TimeSpan Convert_Ticks = TimeSpan.FromMinutes(Mission_Time);
        //string Time_Remaining = string.Format("{0:D2}:{1:D2}:{2:D2}", Convert_Ticks.Hours, Convert_Ticks.Minutes, Convert_Ticks.Seconds);
        string Time_Remaining_tick = string.Format("{0:D2}hr {1:D2}min    ", Convert_Ticks.Hours, Convert_Ticks.Minutes);

        //Method #2 - if our last allowed time in the evening is earlier than this, then we go with that instead
        double timeLeft = END_MISSION_TIME_HRS - GamePlay.gpTimeofDay();
        TimeSpan Convert_Hours = TimeSpan.FromHours(timeLeft);
        string Time_Remaining_hours = string.Format("{0:D2}hr {1:D2}min    ", Convert_Hours.Hours, Convert_Hours.Minutes);

        if (Convert_Hours.CompareTo(Convert_Ticks) <= 0) return Time_Remaining_hours; //whichever happens **soonest** we return

        else return Time_Remaining_tick;
    }
    //Displays time left to player & also returns the time left message as a string
    //Calling with (null, false) will just return the message rather than displaying it

    public int month = -1;
    public int day = -1;

    public string showTimeLeft(Player player = null, bool showMessage = true)
    {
        if (month == -1) month = random.Next(7, 10);
        if (day == -1) day = random.Next(10, 30);

        TimeSpan diff_ts = DateTime.UtcNow - MODERN_CAMPAIGN_START_DATE;
        int diff_days = diff_ts.Days;
        diff_days = Math.Abs(diff_days % HISTORIC_CAMPAIGN_LENGTH_DAYS);  //After the # of days in the historic campaign has elapsed, we roll over & restart with day 0.  Thus, we always have a historic 
                                                                          //date within the range of the actual historic campaign
                                                                          //% can be negative, so Abs() of result needed.  ie -5 % 4 = -1
        DateTime inGameTime_dt = HISTORIC_CAMPAIGN_START_DATE.AddDays(diff_days);

        string missiontimeleft = calcTimeLeft();
        //double inGameTime = GameWorld.ITime.current();

        //int igt_hour =Convert.ToInt32( Math.Floor(inGameTime));

        //int igt_minute = Convert.ToInt32(Math.Floor((double)(60.0 * (inGameTime - igt_hour))));
        //double igt_minute = (double)(60.0 * inGameTime - igt_hour);
        double inGameTime_hours = GamePlay.gpTimeofDay();

        inGameTime_dt = inGameTime_dt.AddHours(inGameTime_hours);

        //DateTime inGameTime_dt = new DateTime(1940, month, day, igt_hour, igt_minute, 0);

        //string msg = "It's currently " + inGameTime_dt.ToString ("d'.'MM'.'yy', 'HHmm' hours'.") + " Time left in mission " + MISSION_ID + ": " + missiontimeleft;

        string msg = "It's currently " + inGameTime_dt.ToString("d' 'MMMM' 'yyyy', 'HHmm' hours'.") + " Time remaining until sunset: " + missiontimeleft;

        /*
         *        
        if (!MISSION_STARTED) msg = "Mission " + MISSION_ID + " not yet started - waiting for first player to enter.";
        else if (COOP_START_MODE) msg = "Mission " + MISSION_ID + " not yet started - waiting for Co-op Start.";
        */

        if (showMessage && player != null) twcLogServer(new Player[] { player }, msg, new object[] { });
        return msg;
    }


    public void logToFile(object data, string messageLogPath)
    {
        try
        {
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX3"); //Testing for potential causes of warping
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
        Task.Run(() => logToFile(data, MESSAGE_FULL_PATH));
        //logToFile(data, MESSAGE_FULL_PATH);
    }

    public void logStats(object data)
    {
        //logToFile(data, STATS_FULL_PATH);
        Task.Run(() => logToFile(data, STATS_FULL_PATH));

    }

    public void DebugAndLog(object data)
    {
        if (DEBUG) twcLogServer(null, (string)data, new object[] { });
        if (!DEBUG && LOG) Console.WriteLine((string)data); //We're using the regular logs.txt as the logfile now logToFile (data, LOG_FULL_PATH); 
    }
    public void gpLogServerAndLog(Player[] to, object data, object[] third)
    {
        //this is already logged to logs.txt so no need for this: if (LOG) logToFile (data, LOG_FULL_PATH);
        twcLogServer(to, (string)data, third);

    }

    public Int64 lastGpLogServerMsg_tick = 0;
    public Int64 GpLogServerMsgDelay_tick = 1000000; //1 mill ticks or 0.1 second
    public Int64 GpLogServerMsgOffset_tick = 500000; //Different modules or submissions can use a different offset to preclude sending gplogservermessages @ the same moment; 500K ticks or 0.05 second    


    //Should replace ALL twcLogServer usage with twcLogServer instead to avoid line overflow etc.
    //wrapper for twcLogServer so that we can do things to it, like log it, suppress all output, delay successive messages etc.
    public void twcLogServer(Player[] to, object data, object[] third = null)
    {
        //this is already logged to logs.txt so no need for this: if (LOG) logToFile (data, LOG_FULL_PATH);
        //gpLogServerWithDelay(to, (string)data, third);

        //gplogserver chokes on long chat messages, so we will break them up into chunks . . . 
        string str = (string)data;
        int maxChunkSize = 200;

        IEnumerable<string> lines = Calcs.SplitToLines(str, maxChunkSize);
        //for (int i = 0; i < str.Length; i += maxChunkSize)
        //for (int i=0; i<lines.GetLength(); i++) gpLogServerWithDelay(to, lines[i], third);

        //foreach (string line in lines) gpLogServerWithDelay(to, line, third);
        foreach (string line in lines) GamePlay.gpLogServer(to, line, third);


    }
    //This is designed to space out gplogserver calls, as (say) 5-10 of these in a row will cause a very noticeable stutter
    //It's sort of a stack for gplogserver messages
    public void gpLogServerWithDelay(Player[] to, object data, object[] third = null)
    {
        //defined above:
        //public Int64 lastGpLogServerMsg_tick = 0;
        //public Int64 GpLogServerMsgDelay_tick = 1000000; //1 mill ticks or 0.1 second
        //public Int64 GpLogServerMsgOffset_tick = 500000; //Different modules or submissions can use a different offset to preclude sending gplogservermessages @ the same moment; 500K ticks or 0.05 second
        DateTime currentDate = DateTime.UtcNow;
        //currentDate.Ticks
        Int64 nextMsg_tick = Math.Max(currentDate.Ticks, lastGpLogServerMsg_tick + GpLogServerMsgDelay_tick);
        Int64 remainder;
        Int64 roundTo = 500000; //round nextMsg_tick UP to the next 1/10 second.  This is to allow different missions/modules to output at different portions of the 0.1 second interval, with the objective of avoiding stutters when messages from different .mis files or modules pile up
        nextMsg_tick = (Math.DivRem(nextMsg_tick - 1, roundTo, out remainder) + 1) * roundTo; // -1 handles the specific but common situation where we want a 0.1 sec delay but it always rounds it up to 0.2 sec.  This makes it round up for anything greater than roundTo, rather than greater than OR EQUAL TO roundTo.
        double nextMsgDelay_sec = (double)(nextMsg_tick - currentDate.Ticks) / 10000000;
        //string msg = (string)data + "(Delayed: " + nextMsgDelay_sec.ToString("0.00") + ")"; //for testing
        string msg = (string)data;
        //twcLogServer(null, nextMsg_tick.ToString() + " " + nextMsgDelay_sec.ToString("0.00"), null); //for debugging
        Timeout(nextMsgDelay_sec, () => { twcLogServer(to, msg, third); });
        lastGpLogServerMsg_tick = nextMsg_tick; //Save the time_tick that this message will be displayed; next message will be at least GpLogServerMsgDelay_tick after this

    }


    /*****************************************************
     * ONACTORCREATED
     * ***************************************************/

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

        int destroyminutes = 150;//Destroy AI a/c this many minutes after they are spawned in.
        //Note that MoveBombTarget-.cs takes care of destroying aircraft when their task is set to "LANDING" and no live players
        //are nearby.  Plus, a/c are destroyed when they fly off the map & MoveBombTarget-.cs will make them do that at 
        //the end of any planned WAYPOINTS they have been given.
        //So this 120 min. destruction thing here is a bit of a safety valve in case random a/c are still hanging about after all the above failed.
        //a/c in the server too long tend to be useless, out of fuel, etc plus slow the server down.  Sometimes they are actually downed but still just sitting 
        //on the ground or whatever


        Timeout(1.0, () => // wait 1 second for human to load into plane
        {
            /* if (DEBUG) twcLogServer(null, "DEBUGC: Airgroup: " + a.AirGroup() + " " 

              + a.Type() + " " 
              + a.TypedName() + " " 
              +  a.AirGroup().ID(), new object[] { });
            */
            if (a == null) return;
            bool iACP = isAiControlledPlane2(a);
            if (!iACP)
            {
                if (a as AiAircraft == null) return;
                string type = Calcs.GetAircraftType(a as AiAircraft);
                if (type.Contains("110") || type.Contains("Ju-87"))
                {
                    Player[] ps = playersInPlane(a as AiAircraft).ToArray();
                    Timeout(1.732, () => { twcLogServer(ps, "JU-87 & 110 PILOTS: Use Tab-4-4-8 or chat command <ai to let AI take over your second position.");
                        //Console.WriteLine("Use Tab-4-4-8 or chat command <ai to let AI take over your second position.");
                    });
                }
            }

            else //AI controlled aircraft
            {


                int ot = (destroyminutes) * 60 - 10; //de-spawns 10 seconds before new sub-mission spawns in.
                                                     //int brk=(int)Math.Round(19/20);


                /* if (DEBUG) twcLogServer(null, "DEBUGD: Airgroup: " + a.AirGroup() + " " 

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

    /*****************************************************
     * END - ONACTORCREATED
     * ***************************************************/


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

    public HashSet<Player> playersInPlane(AiAircraft aircraft)

    { // returns list of players in the aircraft (unique list - no duplicates)
        HashSet<Player> players = new HashSet<Player>();
        if (aircraft == null) return players;

        //check if a player is in any of the "places"
        for (int i = 0; i < aircraft.Places(); i++)
        {
            if (aircraft.Player(i) != null) players.Add(aircraft.Player(i));
        }
        return players;
    }

    public void letAiTake2ndPosition(Player player)
    {
        if (player == null || player.Place() == null) return;
        //Console.WriteLine("lAT2P: {0} {1}", player.PlaceSecondary(), player.PlacePrimary());
        //Console.WriteLine("lAT2P: {0} {1} {2} ", player.PersonPrimary().Place(), player.PlaceSecondary(), player.PlacePrimary());
        //Console.WriteLine("lAT2P: {0} {1} {2} {3}", player.PersonSecondary().Place(), player.PersonPrimary().Place(), player.PlaceSecondary(), player.PlacePrimary());
        //Console.WriteLine("lAT2P: {0} {1} {2} {3}", player.PersonSecondary().Place(), player.PersonPrimary().Place(), player.PlaceSecondary(), player.PlacePrimary());
        //whichever place they are NOT currently in, remove them from
        //if (player.PersonSecondary() != null && player.Place() != (player.PersonSecondary()).Place()) player.PlaceLeave((player.PersonSecondary()).Place());
        //else if (player.PersonPrimary() != null && player.Place() != (player.PersonPrimary()).Place()) player.PlaceLeave((player.PersonPrimary()).Place());

        //Ok, that didn't work.  Just leave them in the pilot's seat always.
        //if ((player.Place() as AiCart).Player(0) == player) player.PlaceEnter(player.Place(), 1); //This should ensure they are in the pilot's seat as primary, not the gunner as primary.  Maybe, I hope?
                                                                                      //However, if someone else is in the pilot's place it won't kick that person out & replace them with this player
        //As long as there is only one player in the aircraft, I **believe** that placeprimary is always the pilot seat.  Placesecondary is whatever other 
        //seat they might be in.  So leaving PlaceSecondary will empty out the second position (gunner or bombadier or whatever) and leave the player in the pilot
        //seat only.  AI will take over the functions of the other seat.
        //Now, you don't want to do this in reverse--remove player from PlacePrimary & leave them in PlaceSecondary.
        //Reason is, now AI will take over the pilot's seat and fly the aircraft. Not what we want!
        //What happens with more than one player in the aircraft, I can't really say for certain.
        player.PlaceLeave(player.PlaceSecondary()); //Now they're kicked from the secondary, which  should leave them in the pilot's seat.
        twcLogServer(new Player[] { player }, "AI has taken over your gunner position.", null);

        
    }



    //Removes AIAircraft if they are off the map. Convenient way to get rid of
    //old a/c - just send them off the map
    public void RemoveOffMapAIAircraft()
    {
        int numremoved = 0;
        //The map parameters - if an ai a/c goes outside of these, it will be de-spawned.  You need to just figure these out based on the map you are using.  Set up some airgroups in yoru mission file along the n, s, e & w boundaries of the map & note where the waypoints are.
        //This should match the values in your .mis file, like
        //BattleArea 10000 10000 360000 310000 10000
        //TODO: There is probably some way to access the size of the battle area programmatically
        //The size below is expanded just slightly from that, as the map shown to players and on the radar map is just slightly larger.
        //Also below a "grace area" of approx 1 square off the map is added
        if (GamePlay == null) return;
        double minX = 6666;
        double minY = 6666;
        double maxX = 362000;
        double maxY = 312000;        
        //////////////Comment this out as we don`t have Your Debug mode  
        DebugAndLog("Checking for AI Aircraft off map OR stopped on ground, to despawn");
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
                            if (airGroup.GetItems().Length > 0) foreach (AiActor actor in airGroup.GetItems())
                                {
                                    if (actor != null && actor is AiAircraft)
                                    {
                                        AiAircraft a = actor as AiAircraft;
                                        /* if (DEBUG) DebugAndLog ("DEBUG: Checking for off map: " + Calcs.GetAircraftType (a) + " "                                            
                                           //+ a.Type() + " " 
                                           //+ a.TypedName() + " " 
                                           +  a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0")
                                          );
                                        */

                                        //for testing
                                        //string name = actor.Name();
                                        //if (actor.Army() == 1 && !name.Contains("gb01") && isAiControlledPlane2(a)) a.Destroy();
                                        //for testing


                                        if (a != null && isAiControlledPlane2(a)) {

                                            double Z_AltitudeAGL = a.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                                            double Z_VelocityTAS = a.getParameter(part.ParameterTypes.Z_VelocityTAS, 0);
                                            AiAirGroupTask aagt = airGroup.getTask();

                                            //so, lots of ai aircraft velicity is negative.  For some reason.  So if checking for stopped, must make it ==0 or maybe >-5 <5 or whatever
                                            /*if (Z_VelocityTAS < 0) 
                                                Console.WriteLine("DEBUG: Off Map or landed/Checking: " + Calcs.GetAircraftType(a) + " "                                                    
                                                    + a.Type() + " "
                                                    + a.TypedName() + " "
                                                    + a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0") + " {0:N0} {1:N0} {2} ",
                                                    Z_AltitudeAGL, Z_VelocityTAS, aagt
                                                   );
                                                   */

                                            if
                                          (
                                            (Z_AltitudeAGL < 5 && Z_VelocityTAS < 10 && Z_VelocityTAS > -1 && aagt != AiAirGroupTask.TAKEOFF) ||  //is stopped on ground //not sure why we get negative velocity sometimes?  AND not landing
                                            a.Pos().x <= minX - 12500 ||  //Same as players, 12500 'grace area', this is set in statsmission Stb_RemoveOffMapPlayers()
                                            a.Pos().x >= maxX + 12500 ||
                                            a.Pos().y <= minY - 12500 ||
                                            a.Pos().y >= maxY + 12500
                                          )
                                            // ai aircraft only
                                            {
                                                Console.WriteLine("DEBUG: Off Map or landed/Destroying: " + Calcs.GetAircraftType(a) + " "
                                                + a.Type() + " "
                                                + a.TypedName() + " "
                                                + a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0") + " {0:N0} {1:N0} {2} ",
                                                Z_AltitudeAGL, Z_VelocityTAS, aagt
                                               );
                                                numremoved++;
                                                Timeout(numremoved * 10, () => { a.Destroy(); }); //Destroy the a/c, but space it out a bit so there is no giant stutter 

                                            }
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
        Timeout(timeToRemove_sec, () =>
        {

            //player.PlaceLeave(0);
            Stb_RemovePlayerFromCart(aircraft as AiCart, player); //remove the primary player
            Stb_RemoveAllPlayersFromAircraft(aircraft, 0); //remove any other players
            Timeout(timetoDestroy_sec, () =>
        {
            if (isAiControlledPlane(aircraft)) Stb_DestroyPlaneUnsafe(aircraft);  //destroy if AI controlled, which SHOULD be the case all of the time now
        }); //Destroy it a bit later
        });
    }

    //Removes ALL players from an a/c after a specified period of time (seconds)
    private void Stb_RemoveAllPlayersFromAircraft(AiAircraft aircraft, double timeToRemove_sec = 1.0)
    {
        Timeout(timeToRemove_sec, () =>
        {

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
        //twcLogServer(null, "Checking distance to nearest airport", new object[] { });
        for (int i = 0; i < n; i++)
        {
            AiActor a = (AiActor)GamePlay.gpAirports()[i];
            if (a == null) continue;
            //if (actor.Army() != a.Army()) continue; //only count friendly airports
            //if (actor.Army() != (a.Pos().x, a.Pos().y)
            //OK, so the a.Army() thing doesn't seem to be working, so we are going to try just checking whether or not it is on the territory of the Army the actor belongs to.  For some reason, airports always (or almost always?) list the army = 0.

            //twcLogServer(null, "Checking airport " + a.Name() + " " + GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) + " " + a.Pos().x.ToString ("N0") + " " + a.Pos().y.ToString ("N0") , new object[] { });

            if (GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != actor.Army()) continue;


            //if (!a.IsAlive()) continue;


            Point3d pp;
            pp = a.Pos();
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                d2Min = d2;
                //twcLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString(), new object[] { });
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
                //twcLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString() + " distance " + d2.ToString("n0"), new object[] { });
            }

        }
        //twcLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
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



    /*******************************************************************************
     ******************************************************************************* 
     * METHODS DIFFERENT FOR DIFFERENT CAMPAIGNS
     * 
     * Below we're collecting the methods that are wildly different between different TWC
     * campaigns so that we can more easily use comparison tools on the similar portions above.
     * 
     * *******************************************************************************
     * *****************************************************************************/



    /******************************************************************************************************************** 
     * MISSION OBJECTIVES CLASSES & METHODS
     * 
     * Methods & classes for dealing with objectives, messages & other results of destroying objectives, awarding points, dealing with disabled radar, etc
     * 
     * All Mission Objectives should be listed & handled here, then a simple routine below can be called from OnTrigger, OnBombExploded, etc
     * rather than having code & variables related to objectives scattered hither & yon across the entire file
     * 
     * ******************************************************************************************************************/

    /*   points to turn map**********************************************
    public double InitialBlueObjectiveCount = 0;
    public double InitialRedObjectiveCount = 0;
    public string Objective_Total_Blue = "";
    public string Objective_Total_Red = "";
    osk_BlueObjDescription
    */

    //was InitialBlueObjectiveCount
    Dictionary<ArmiesE, double> MissionObjectiveScore = new Dictionary<ArmiesE, double>()
    {    {ArmiesE.Red, 0 },
         {ArmiesE.Blue, 0 }
    };  //reference as MissionObjectiveScore[ArmiesE.Red] MissionObjectiveScore[ArmiesE.Blue]

    //was Objective_Total_Blue
    Dictionary<ArmiesE, string> MissionObjectivesCompletedString = new Dictionary<ArmiesE, string>()
    {    {ArmiesE.Red, "" },
         {ArmiesE.Blue, "" }
    };

    /*  NO LONGER NEEDED!  Replaced by MO_ListRemainingPrimaryObjectives(player, player.Army()); which is better & also does the sector/recon/exact position thing
    //was osk_BlueObjDescription
    Dictionary<ArmiesE, string> MissionObjectivesString = new Dictionary<ArmiesE, string>()
    {    {ArmiesE.Red, "" },
         {ArmiesE.Blue, "" }
    };
    */

    //What percent of primary targets is actually required ot turn the map
    //If you make it 100% you have to get them all, but if some are difficult or impossible then that army will be stuck
    public Dictionary<ArmiesE, double> MO_PercentPrimaryTargetsRequired = new Dictionary<ArmiesE, double>() {
        {ArmiesE.Red, 80 },
        {ArmiesE.Blue, 80 }
    };

    //TODO: Use similar scheme for total points, objectives completed list, objectives completed
    //Points required, assuming they are doing it entirely with Primary Targets; ie, secondary or other targets do not count towards this total
    //at all
    public Dictionary<ArmiesE, double> MO_PointsRequired = new Dictionary<ArmiesE, double>() {
        {ArmiesE.Red, 45 },
        {ArmiesE.Blue, 45 }
    };
    //////////////////////******************************************/////////////////////////////
    //Amount of points require in case percent of primary is less than 100% but more than MO_PercentPrimaryTargetsRequired
    //This allows mission to be turned in case one objective is malfunctioning or super-difficult - by hitting some other alternate targets
    //So this is like a consolation prize.  You couldn't take out all primary targets?  OK, you can just take out a few MORE alternates
    //Generally thise should be more than MO_PointsRequired
    //If it's less than MO_PointsRequired then the MO_PercentPrimaryTargetsRequired becomes more the operative factor in determining the map turn
    public Dictionary<ArmiesE, double> MO_PointsRequiredWithMissingPrimary = new Dictionary<ArmiesE, double>() {
        {ArmiesE.Red, 58 },
        {ArmiesE.Blue, 58 }
    };

    public Dictionary<ArmiesE, string> MO_IntelligenceLeakNearMissionEnd = new Dictionary<ArmiesE, string>() {
        {ArmiesE.Red, "" },  //the leak FOR red army (about something Blue is doing)
        {ArmiesE.Blue, "" }  //the leak FOR blue army (about something Red is doing)
    };

    //Dictionary<string, IMissionObjective> MissionObjectivesList = new Dictionary<string, IMissionObjective>();
    Dictionary<ArmiesE, List<MissionObjective>> DestroyedObjectives = new Dictionary<ArmiesE, List<MissionObjective>>() {
        {ArmiesE.Red, new List<MissionObjective>() },
        {ArmiesE.Blue, new List<MissionObjective>() }
    };  //reference as DestroyedObjectives[ArmiesE.Red] DestroyedRadar[ArmiesE.Blue]

    Dictionary<ArmiesE, List<MissionObjective>> DestroyedRadar = new Dictionary<ArmiesE, List<MissionObjective>>() {
        {ArmiesE.Red, new List<MissionObjective>() },
        {ArmiesE.Blue, new List<MissionObjective>() }
    };  //reference as DestroyedRadar[ArmiesE.Red] DestroyedRadar[ArmiesE.Blue]

    Dictionary<ArmiesE, List<String>> MissionObjectivesSuggested = new Dictionary<ArmiesE, List<String>>() {
        {ArmiesE.Red, new List<String>() },
        {ArmiesE.Blue, new List<String>() }
    };

    public enum MO_TriggerType { Trigger, Static, Airfield, PointArea };
    public enum MO_ObjectiveType { Radar, AA, Ship, Building, Fuel, Airfield, Aircraft, Vehicles, Bridge, Dam, Dock, RRYard, Railroad, Road, AirfieldComplex, FactoryComplex, ArmyBase, IndustrialArea, MilitaryArea };
    //type Airfield is the auto-entered list of airfield objectives (every active airport in the game) whereas AirfieldComplex could be an additional specific target on or near an airfield

    [DataContract()]
    public class MissionObjective : IMissionObjective
    {
        //public string TriggerName { get; set; }

        [DataMember] public string ID { get; set; } //unique name, often the Triggername or static name
        [DataMember] public string Name { get; set; } //Name the will be displayed to the [DataMember] public in messages etc
        [DataMember] public int AttackingArmy { get; set; } // Army this is an objective for (ie, whose task is to destroy it); can be 1=red, 2=blue,0=none
        [DataMember] public int OwnerArmy { get; set; } // Army that owns this object (ie, is harmed if it is destroyed)
        [DataMember] public string FlakID { get; set; } //Flak area associated with this objective.  Flak area codes & associated .mis files are identified in FlakMissions dictionary defined below
        [DataMember] public string InitSubmissionName { get; set; } //Flak area associated with this objective.  Flak area codes & associated .mis files are identified in FlakMissions dictionary defined below
        [DataMember] public bool AutoFlakIfPrimary { get; set; } //Automatically place flak batteries near this objective, but only if it is chosen as a primary
        [DataMember] public bool AutoFlak { get; set; } //Automatically place flak batteries near this objective
        [DataMember] public int NumFlakBatteries { get; set; }
        [DataMember] public int NumInFlakBattery { get; set; }

        [DataMember] public bool IsEnabled { get; set; } //TODO: This is only partially implemented.  But could be used in case of bad data or whatever to just disable the objective.
        [DataMember] public Mission.MO_ObjectiveType MOObjectiveType { get; set; }
        [DataMember] public Mission.MO_TriggerType MOTriggerType { get; set; }
        [DataMember] public bool IsPrimaryTarget { get; set; } //One of the primary/required targets for this mission?
        [DataMember] public double PrimaryTargetWeight { get; set; } //If we select primary targets randomly etc, is this one that could be selected? Percentage weight 0-100, 0 means never chosen.  Update: Now 0-200.  2020-01.
        [DataMember] public double Points { get; set; }
        [DataMember] public double TimetoRepairIfDestroyed_hr { get; set; } //hours needed to repair this objective if it is destroyed, per 100%.  Ie if 200% destroyed it will take 2X this long.
        [DataMember] public bool Destroyed { get; set; }
        [DataMember] public double DestroyedPercent { get; set; } //some items can potentially be partially destroyed, it 50% = 0.5, 100%=1.0; also higher numbers possible such as 200% = 2.0;
        [DataMember] public bool ObjectiveAchievedForPoints { get; set; } //We set this when one side destroys an objective & gets the points for it.  We can then remove this when that side
                                                                          //turns the map so that they have a clean slate of new objectives.  But the points is independent of whether
                                                                          //it is actually still destroyed or not. 
        [DataMember] public DateTime? TimeToUndestroy_UTC { get; set; } //if destroyed, what time UTC is the d/t it should be undestroyed/repaired.  This is in real time, ie if item destroyed 10pm, Tues Jan 24, 2022 UTC for 24 hours then it will be repaired at 10pm Wed Jan 25, 2022.
        [DataMember] public DateTime? LastHitTime_UTC { get; set; } //Last time target attacked/hit

        [DataMember] public bool Scouted { get; set; } //whether or not it has been scouted or reconnoitered by the enemy; if so they can get access to exact coordinates etc
        //public Dictionary<Player,int> PlayersWhoScouted { get; set; } //list of any players who have scouted this objective //Player is not serializable, alas.
        [DataMember] public Dictionary<string, int> PlayersWhoScoutedNames { get; set; } //list of any players who have scouted this objective

        [DataMember] public bool hasGeneralStaff { get; set; } //one of the objectives for each side will have one of the top generals/staff/staff car nearby ONE of their primary objectives

        [DataMember] public Point3d Pos { get; set; }
        [DataMember] public double radius { get; set; } //extent of the object, ie, for airfields, how far center to the perimeter.  This is more FYI as info about the object, although it can be used to determine whether hits to a certain object are effective, or how effective (as we do with airfields)

        [DataMember] public string Sector { get; set; }
        [DataMember] public string bigSector { get; set; } //a block of several sectors, somewhat randomly selected, and the target is somewhere within it
        [DataMember] public string HUDMessage { get; set; }
        [DataMember] public string LOGMessage { get; set; }
        [DataMember] public string SuccessSubmissionName { get; set; } //submission to launch when objective reached; if blank nothing launched
        [DataMember] public double RadarEffectiveRadius { get; set; }
        [DataMember] public string TriggerName { get; set; }
        [DataMember] public string TriggerType { get; set; }
        [DataMember] public double TriggerPercent { get; set; }
        [DataMember] public double TriggerDestroyRadius { get; set; } //What is set in the .mis file as the effective radius for this trigger object.  This is slightly different from "radius" in that it is a number used only internally to determine if the destroy objects are within the effective area of the trigger.
        [DataMember] public double OrdnanceRequiredToTrigger_kg { get; set; } //For type PointArea
        [DataMember] public double OrdnanceOnTarget_kg { get; set; } //For PointArea & similar - how many KG ordnance have hit this target already
        [DataMember] public double ObjectsRequiredToTrigger_num { get; set; } ////For type JerryCanArea: buildings, static objects, etc within the given radius required to trigger it.  Using actual number instead of percent, because I don't think we cna get the listing of now many buildings etc in a given area. NOT IMPLEMENTED YET
        [DataMember] public double ObjectsDestroyed_num { get; set; } //how many objects have been destroyed so far. NOT IMPLEMENTED YET
        [DataMember] public List<string> StaticNames { get; set; } //for static targets, the list of static names that will determine if the target is destroyed
        [DataMember] public double StaticPercentageRequired { get; set; } //what percentage of those static targets must be destroyed to eliminate the objective
        [DataMember] public List<string> StaticRemoveNames { get; set; } //what statics to remove when the object is destroyed (allows eg dams to be breached by removal of certain portions)
        [DataMember] public double StaticRemoveDelay_sec { get; set; } //how long to wait after target destruction before removing static objects in list
        [DataMember] public double StaticRemoveSpread_sec { get; set; } //how long to spread out the static target destruction
        [DataMember] public string Comment { get; set; } //PRIVATE comment, ie for developers, internal notes, etc, not for display to end users
        public Mission msn;

        //for serialization we must  have a paramterless constructor.  So if we serialize things in we'll have to add the Mission msn value later;
        //after loading the data back in & unserializing it.
        public MissionObjective()
        {
        }

        public MissionObjective(Mission m)
        {
            msn = m;
        }
        //RADAR TRIGGER initiator
        public MissionObjective(Mission m, string tn, string n, string flak, int ownerarmy, double pts, double repairdays, string t, double p, double x, double y, double d, double e, bool pt, double ptp, string comment)
        {

            msn = m;
            MOObjectiveType = MO_ObjectiveType.Radar;
            MOTriggerType = MO_TriggerType.Trigger;
            TriggerName = tn;
            ID = tn;
            Name = n;
            FlakID = flak;
            AutoFlakIfPrimary = true;
            AutoFlak = false;
            NumFlakBatteries = 6;
            NumInFlakBattery = 6;

            //NumFlakBatteries = 3;
            //NumInFlakBattery = 4;

            IsEnabled = true;

            TimetoRepairIfDestroyed_hr = repairdays * 24;
            OwnerArmy = ownerarmy;
            AttackingArmy = 3 - ownerarmy;
            if (AttackingArmy > 2 || AttackingArmy < 1) AttackingArmy = 0;
            if (AttackingArmy != 0)
            {
                HUDMessage = ArmiesL[AttackingArmy] + " destroyed " + Name;
                LOGMessage = "Heavy damage to " + Name + " - out of action about " + this.TimetoRepairIfDestroyed_hr.ToString("F1") + " days. Good job " + ArmiesL[AttackingArmy] + "!!!";
            }
            else
            {
                HUDMessage = Name + " was destroyed";
                LOGMessage = Name + " was destroyed, " + this.TimetoRepairIfDestroyed_hr.ToString("F1") + " days to repair. " + pts + " awarded " + ArmiesL[AttackingArmy];
            }

            Points = pts;
            TriggerType = t;
            TriggerPercent = p;
            Pos = new Point3d(x, y, 0);
            /* string keyp = Calcs.doubleKeypad(Pos);
            Sector = msn.GamePlay.gpSectorName(x, y).ToString() + "." + keyp;
            Sector = Sector.Replace(",", ""); // remove the comma */
            Sector = Calcs.correctedSectorNameDoubleKeypad(msn, Pos);
            bigSector = Calcs.makeBigSector(msn, Pos);
            TriggerDestroyRadius = d;
            RadarEffectiveRadius = e;

            Destroyed = false;
            DestroyedPercent = 0;
            ObjectiveAchievedForPoints = false;
            TimeToUndestroy_UTC = null;
            LastHitTime_UTC = null;
            radius = 50;

            Scouted = false;
            PlayersWhoScoutedNames = new Dictionary<string, int>();

            hasGeneralStaff = false;


            IsPrimaryTarget = pt;
            PrimaryTargetWeight = ptp;
            Comment = comment;
        }

        //AIRFIELD initiator
        //public Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>> AirfieldTargets = new Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>>();
        //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius, Point3d airfield center (position)
        public MissionObjective(Mission m, double pts, double ptp, AiAirport airport, int arm, Tuple<bool, string, double, double, DateTime, double, Point3d> tup)
        //            string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, int ptp, string comment)
        {

            msn = m;
            MOObjectiveType = MO_ObjectiveType.Airfield;
            MOTriggerType = MO_TriggerType.Airfield;
            ID = tup.Item2 + "_airfield";
            //Console.WriteLine("New MissionObjective airport: " + ID);
            Name = tup.Item2 + " Airfield";
            FlakID = ""; //no flak files for airports . . . yet
            AutoFlakIfPrimary = true;
            AutoFlak = false;
            NumFlakBatteries = 8;
            NumInFlakBattery = 6;
            //for testing
            //NumFlakBatteries = 3;
            //NumInFlakBattery = 4;

            IsEnabled = true;

            Pos = tup.Item7;
            //OwnerArmy = airport.Army(); OK, this doesn't work as all .Army() for airports is set to 0        
            OwnerArmy = arm;
            AttackingArmy = 3 - OwnerArmy;
            if (AttackingArmy > 2 || AttackingArmy < 1) AttackingArmy = 0;

            HUDMessage = null;//hud/log messages are handled by the handle airport bombing routine
            LOGMessage = null;

            Points = pts;
            string keyp = Calcs.doubleKeypad(Pos);
            /* Sector = msn.GamePlay.gpSectorName(x, y).ToString() + "." + keyp;
            Sector = Sector.Replace(",", ""); // remove the comma     */
            Sector = Calcs.correctedSectorNameDoubleKeypad(msn, Pos);
            bigSector = Calcs.makeBigSector(msn, Pos);

            radius = 4500; //we should read this in from the ap but in the meanwhile this works OK

            TimetoRepairIfDestroyed_hr = 24; //Airfields have their own routines for figuring this out, but we are still setting this here as the typical/average value
            Destroyed = false;
            DestroyedPercent = 0;
            ObjectiveAchievedForPoints = false;
            TimeToUndestroy_UTC = null;
            LastHitTime_UTC = null;

            Scouted = false;
            PlayersWhoScoutedNames = new Dictionary<string, int>();

            hasGeneralStaff = false;

            IsPrimaryTarget = false;
            PrimaryTargetWeight = ptp;
            Comment = "Auto-generated from in-game airports list";
        }



        //TRIGGER initiator (for all trigger types except RADAR & AIRFIELD)
        public MissionObjective(Mission m, MO_ObjectiveType mot, string tn, string n, string flak, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, double ptp, double ttr_hr, string comment)
        {

            msn = m;
            MOObjectiveType = mot;
            MOTriggerType = MO_TriggerType.Trigger;
            TriggerName = tn;
            ID = tn;
            Name = n;
            FlakID = flak;
            AutoFlakIfPrimary = true;
            AutoFlak = false;
            //NumFlakBatteries = 4;
            //NumInFlakBattery = 10;
            //for testing
            NumFlakBatteries = 3;
            NumInFlakBattery = 3;

            IsEnabled = true;

            OwnerArmy = ownerarmy;
            AttackingArmy = 3 - ownerarmy;
            if (AttackingArmy > 2 || AttackingArmy < 1) AttackingArmy = 0;
            if (AttackingArmy != 0)
            {
                HUDMessage = ArmiesL[AttackingArmy] + " destroyed " + Name;
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
            string keyp = Calcs.doubleKeypad(Pos);
            /* Sector = msn.GamePlay.gpSectorName(x, y).ToString() + "." + keyp;
            Sector = Sector.Replace(",", ""); // remove the comma     */
            Sector = Calcs.correctedSectorNameDoubleKeypad(msn, Pos);
            bigSector = Calcs.makeBigSector(msn, Pos);

            radius = 75;

            TriggerDestroyRadius = d;
            TimetoRepairIfDestroyed_hr = ttr_hr;
            Destroyed = false;
            DestroyedPercent = 0;
            ObjectiveAchievedForPoints = false;
            TimeToUndestroy_UTC = null;
            LastHitTime_UTC = null;

            Scouted = false;
            PlayersWhoScoutedNames = new Dictionary<string, int>();

            hasGeneralStaff = false;

            IsPrimaryTarget = pt;
            PrimaryTargetWeight = ptp;
            Comment = comment;
        }

        //AREAPOINT initiator.  An area which is designated by a map coordinate and radius.  
        //You can designate EITHER kg tonnage of ordnance dropped in that area to destroy it, OR a certain number of objects (static objects, actors, buildings, etc) that must be killed within that radius (the buildings part working depends on TF getting the onbuildingdestroyed routine working again).
        //OR you can choose BOTH and in that case the players will have to drop the certain tonnage on the area AND kill the certain number of objects
        public MissionObjective(Mission m, MO_ObjectiveType mot, string tn, string n, string flak, string iSub, int ownerarmy, double pts, double x, double y, double rad, double trigrad, double orttkg, double orttn, double ptp, double ttr_hr, bool af, bool afip, int fb, int fnib, string comment)
        {

            Console.WriteLine("Initiating PointArea objective " + tn);
            msn = m;
            MOObjectiveType = mot;
            MOTriggerType = MO_TriggerType.PointArea;
            TriggerName = tn;
            ID = tn;
            Name = n;
            FlakID = flak;
            InitSubmissionName = iSub;
            AutoFlakIfPrimary = afip;
            AutoFlak = af;
            //These are big wide open easy bombing targets, but thye are heavily defended, must come in high.  Is the idea.
            NumFlakBatteries = fb;
            NumInFlakBattery = fnib;

            //for testing
            NumFlakBatteries = 6;
            NumInFlakBattery = 4;

            IsEnabled = true;

            OwnerArmy = ownerarmy;
            AttackingArmy = 3 - ownerarmy;
            if (AttackingArmy > 2 || AttackingArmy < 1) AttackingArmy = 0;
            if (AttackingArmy != 0)
            {
                HUDMessage = ArmiesL[AttackingArmy] + " destroyed " + Name;
                LOGMessage = "Heavy damage to " + Name + " - good job " + ArmiesL[AttackingArmy] + "!!!";
            }
            else
            {
                HUDMessage = Name + " was destroyed";
                LOGMessage = Name + " was destroyed";
            }

            Points = pts;
            TriggerType = ""; //used for CLoD internal triggers, which have a name like TGroundtarget
            TriggerPercent = 0; //we're not using this here, it is only for CloD built-in triggers
            Pos = new Point3d(x, y, 0);
            string keyp = Calcs.doubleKeypad(Pos);
            /* Sector = msn.GamePlay.gpSectorName(x, y).ToString() + "." + keyp;
            Sector = Sector.Replace(",", ""); // remove the comma     */
            Sector = Calcs.correctedSectorNameDoubleKeypad(msn, Pos);
            bigSector = Calcs.makeBigSector(msn, Pos);

            radius = rad;

            TriggerDestroyRadius = trigrad;

            OrdnanceRequiredToTrigger_kg = orttkg;
            OrdnanceOnTarget_kg = 0; //stores how much ordinance has been piled on this target
            ObjectsRequiredToTrigger_num = orttn;
            ObjectsDestroyed_num = 0;

            TimetoRepairIfDestroyed_hr = ttr_hr;
            Destroyed = false;
            DestroyedPercent = 0;
            ObjectiveAchievedForPoints = false;
            TimeToUndestroy_UTC = null;
            LastHitTime_UTC = null;

            Scouted = false;
            PlayersWhoScoutedNames = new Dictionary<string, int>();

            hasGeneralStaff = false;

            IsPrimaryTarget = false;
            PrimaryTargetWeight = ptp;
            Comment = comment;

            /*
            //HMMMM, we actually need to do this little routine every time the mission is restarted, not just when the obejctive is very first initialized.
            //Have to think how best to handle that.
            if (IsEnabled)
            {
                //load submission if requested
                string s = msn.CLOD_PATH + msn.FILE_PATH + InitSubmissionName;
                GamePlay.gpPostMissionLoad(s);                
                Console.WriteLine(s.Replace(msn.CLOD_PATH + msn.FILE_PATH, "") + " file loaded");

                //place a jerrycan in the middle of the area, covering it.  This allows stats to be counted for anything in this area, and also sets up smoke etc for any bombs hitting this area.
                string jerry = "JerryCan_GER1_1";
                if (TriggerDestroyRadius > 71) jerry = "JerryCan_GER1_2";
                if (TriggerDestroyRadius > 141) jerry = "JerryCan_GER1_3";
                if (TriggerDestroyRadius > 282) jerry = "JerryCan_GER1_5";
                //if it's greater than 1410 we'll have to figure out something else to do, place several 1_5s around, probably.
                //The JerryCan_GER1_1 (static - environment - jerrycan) covers a radius of 71 meters which is just enough to fill a 100 meter square (seen in FMB at full zoom) to all corners if placed in the center of the 100m square.
                // JerryCan_GER1_2 covers 141m radius (covers 4 100m squares to the corners if placed in the center)
                // JerryCan_GER1_3 covers 282m radius (covers 16 100m squares to the corners if placed in the center)
                // JerryCan_GER1_5 covers 1410m radius (a 1km square to the corner if placed in the center)

                Calcs.loadStatic(GamePlay, msn, Pos.x, Pos.y, 2, "Stationary." + jerry);
            }
            */
        }

        public void makeScouted(Player player)
        {
            Scouted = true;
            if (player == null || player.Name() == null) return;
            if (PlayersWhoScoutedNames.ContainsKey(player.Name())) return; //they're already in the list
            //PlayersWhoScoutedNames

            PlayersWhoScoutedNames[player.Name()] = PlayersWhoScoutedNames.Count; //Add them to the list of players who have scouted; So 0 for the first player to scout, 1 for the 2nd, etc

        }

        public string ToString(bool misformat = true)
        {
            //MissionObjective mo;

            if (misformat)
            {
                return "  " + TriggerName + " " + TriggerType + " " + TriggerPercent + " " + Pos.x + " " + Pos.y + " " + TriggerDestroyRadius;
            }
            else
            {
                string sn = ""; if (StaticNames != null && StaticNames.Count > 1) sn = string.Join(",", StaticNames);
                string srn = ""; if (StaticRemoveNames != null && StaticRemoveNames.Count > 1) sn = string.Join(",", StaticRemoveNames);
                /*return ID + " " + Name + " " + AttackingArmy.ToString() + " " + OwnerArmy.ToString() + " " + IsEnabled.ToString() + " " + MOObjectiveType.ToString() + " " + MOTriggerType + " " + IsPrimaryTarget.ToString() + " "
                + PrimaryTargetWeight.ToString() + " " + Points.ToString() + " " + Destroyed.ToString() + " " + Pos.x.ToString() + " " + Pos.y.ToString() + " " + Sector + " " + RadarEffectiveRadius.ToString() + " " + TriggerName + " "
                + TriggerType + " " + TriggerPercent.ToString() + " " + TriggerDestroyRadius.ToString() + " " + StaticPercentageRequired.ToString() + " " + StaticRemoveDelay_sec.ToString() + " " + StaticRemoveSpread_sec.ToString() + " "
                +Comment + " " + HUDMessage + " " + LOGMessage
                + " " + sn + " " + srn;*/

                return ID + "\t" + Name + "\t" + AttackingArmy.ToString() + "\t" + OwnerArmy.ToString() + "\t" + IsEnabled.ToString() + "\t" + MOObjectiveType.ToString() + "\t" + MOTriggerType + "\t" + IsPrimaryTarget.ToString() + "\t"
                    + PrimaryTargetWeight.ToString() + "\t" + Points.ToString() + "\t" + "Destroyed: " + Destroyed.ToString() + "\t" + "Scouted: " + Scouted.ToString() + "\t" + Pos.x.ToString() + "\t" + Pos.y.ToString() + "\t" + Sector + "\t" + RadarEffectiveRadius.ToString() + "\t" + TriggerName + "\t"
                    + TriggerType + "\t" + TriggerPercent.ToString() + "\t" + TriggerDestroyRadius.ToString() + "\t" + StaticPercentageRequired.ToString() + "\t" + StaticRemoveDelay_sec.ToString() + "\t" + StaticRemoveSpread_sec.ToString() + "\t"
                    + Comment + "\t" + HUDMessage + "\t" + LOGMessage
                    + "\t" + sn + "\t" + srn;

            }

        }
    }

    //List<MissionObjective> BlueDestroyedRadar = new List<MissionObjective>();

    public class MissionObjectives
    {
        private Mission msn;
        private maddox.game.IGamePlay gp;

        public MissionObjectives(Mission mission, maddox.game.IGamePlay gameplay)
        {
            msn = mission;
            gp = gameplay;


            bool[] loadPreviousMission_success = new bool[] { false, false, false, false, false, false };
            bool loadingFromDiskOK = false;
            try
            {
                loadPreviousMission_success = msn.MO_ReadMissionObjects();
            }
            catch (Exception ex)
            {
                Console.WriteLine("File Read problem on startup!!  Using defaults. Error message: " + ex.ToString());
                loadPreviousMission_success = new bool[] { false, false, false, false, false, false }; //loadPreviousMission_success = false;
            }

            if (!loadPreviousMission_success[0] || !loadPreviousMission_success[3] || !loadPreviousMission_success[4] || !loadPreviousMission_success[5])
            {   //If we couldn't load the old file we have little choice but to just  start afresh [0]
                //we couldn't read the current score [3], the list of objectives completed [4] or the full list of objectives  [5] we could just
                //reconstruct those.  But for now if we lose them we'll just re-start everythign from scratch.

                msn.MissionObjectivesList = new Dictionary<string, MissionObjective>();  //zero out the mission objectives list (otherwise when we run the routine below they will ADD to anything already there)
                RadarPositionTriggersSetup();
                MissionObjectiveTriggersSetup();
                msn.MO_MissionObjectiveAirfieldsSetup(mission, gameplay, addNewOnly: false); //must do this after the Radar & Triggers setup, as it uses info from those objectives
            }
            else
            {
                loadingFromDiskOK = true;

                //RESET PRIMARY OBJECTIVES (so they can be re-read from the .ini file, reset, or whatever, down below)
                msn.updateMissionObjectivesListOnReload(this); //We reloaded the MissionObjectivesList from disk, but if we have added or removed objectives OR changed or added fields in MissionObjective then the loaded-from-disk data will by out of sync with the changes we have made.  This updates MissionObjectivesList to reflect what is in the .cs file now, and initialize everything properly, but transfers any important data (mo.Destroyed, Mo.DestroyedPercent etc) from the saved-to-disk version to the current running version. 

                //TODO: Need to do a thing here that will re-initialize objectives any time we update the MissionObjective class, but just preserve Damage, percent, scouted, & other essential things

                //Now we have to pick up any NEW triggers or objectives that have been added to -main.cs since the last run
                //RadarPositionTriggersSetup(addNewOnly: true); // now done by updateMissionObjectivesListOnReload();
                //MissionObjectiveTriggersSetup(addNewOnly: true); // now done by updateMissionObjectivesListOnReload();

                //msn.MO_MissionObjectiveAirfieldsSetup(mission, gameplay, addNewOnly: true); //does a bunch of airfield setup, adds new objectives; must do this after the Radar & Triggers setup, as it uses info from those objectives // now done by updateMissionObjectivesListOnReload();
            }

            if (!loadPreviousMission_success[1])
            {
                Console.WriteLine("Failed to load Suggested Objectives - generating them fresh.");
                SelectSuggestedObjectives(ArmiesE.Red);
                SelectSuggestedObjectives(ArmiesE.Blue);
            }

            //Get new objectives for winner if they have turned the map OR read in the old objectives if not
            if (msn.MapPrevWinner == "Red")
            {
                Console.WriteLine("RED turned the map last time - giving reward, selecting new objectives");
                msn.MO_MissionObjectiveWinnersReward(ArmiesE.Red); //clear all destroyed radar, airfields, scouted objects, current primary objectives scored, for winner; 
                msn.MO_SelectPrimaryObjectives(1, 0, fresh: true);
                msn.MO_ReadPrimaryObjectives(2);
                SelectSuggestedObjectives(ArmiesE.Red);
            }
            else if (msn.MapPrevWinner == "Blue")
            {
                Console.WriteLine("BLUE turned the map last time - giving reward, selecting new objectives");
                msn.MO_MissionObjectiveWinnersReward(ArmiesE.Blue); //clear all destroyed radar, airfields, scouted objects, current primary objectives scored, for winner; 
                msn.MO_SelectPrimaryObjectives(2, 0, fresh: true);
                msn.MO_ReadPrimaryObjectives(1);
                SelectSuggestedObjectives(ArmiesE.Blue);
            }
            else
            {
                msn.MO_ReadPrimaryObjectives(2);
                msn.MO_ReadPrimaryObjectives(1);
                //SuggestedObjectives for both armies are read in via the .ini file
            }
            //this will go through all objectives (except airports) & disable any that need to be disabled, add any new ones in the .cs file, etc
            //needs to be done AFTER winner stuff is done this puts smoke on damaged objectives and other things that will be changed if the winner's rewards have not yet happened
            msn.MO_MissionObjectiveOnStartCheck(msn, gp);

            try
            {
                msn.MO_HandleGeneralStaffPlacement();
            }
            catch (Exception ex) { Console.WriteLine("MO_init error1! " + ex.ToString()); }
            //Now write the new objective list to file
            try
            {
                msn.MO_WritePrimaryObjectives();
            }
            catch (Exception ex) { Console.WriteLine("MO_init error2! " + ex.ToString()); }
            //Must select suggested/secondary objectives AFTER loading/choosing the primary objectives - otherwise we might end up with some items on both lists                        

            //msn.MO_WriteMissionObjects();
            //Thread.Sleep(1000); //For testing purposes, not really the best way to do it.

            try
            {
                //load the flak for the areas that have primary objectives
                msn.MO_LoadAllPrimaryObjectiveFlak(FlakMissions);
            }
            catch (Exception ex) { Console.WriteLine("MO_init error3! " + ex.ToString()); }

            try {
                msn.MO_InitializeAllObjectives();
            }
            catch (Exception ex) { Console.WriteLine("MO_init error3a! " + ex.ToString()); }


            //Write list of triggers to a file in simulated .mis format so that it is easy to verify all triggers have been read in similar
            //to the .mis file
            try
            {
                msn.MO_WriteOutAllMissionObjectives(msn.MISSION_ID + "-mission_objectives_mis_format.txt", true);
            }
            catch (Exception ex) { Console.WriteLine("MO_init error4! " + ex.ToString()); }
            try
            {
                msn.MO_WriteOutAllMissionObjectives(msn.MISSION_ID + "-mission_objectives_complete.txt", false);
            }
            catch (Exception ex) { Console.WriteLine("MO_init error5! " + ex.ToString()); }
        }

        //few little sanity checks on the objective data
        public bool MO_SanityChecks(string tn, string n, MO_TriggerType mtt)
        {
            if (msn.MissionObjectivesList.Keys.Contains(tn))
            {
                Console.WriteLine("*************MissionObjective initialize WARNING****************");
                Console.WriteLine("MissionObjective initialize: Objective Trigger ID " + tn + " : " + n + " IS DUPLICATED in the .cs file.  This duplicate occurence will be ignored.");
                return false;
            }
            if (mtt == MO_TriggerType.Trigger)
            {
                if (gp == null)
                {
                    Console.WriteLine("*************MissionObjective initialize WARNING****************");
                    Console.WriteLine("gp is null!");

                }
                if (gp.gpGetTrigger(tn) == null)
                {
                    Console.WriteLine("*************MissionObjective initialize WARNING****************");
                    Console.WriteLine("MissionObjective initialize: Objective Trigger " + tn + " : " + n + " DOES NOT EXIST in the .mis file.");
                    return false;
                }
                /*
                 * //Turning this off for now as it seems useless?  Not sure what .Enable == false means?
                if (gp.gpGetTrigger(tn) != null && gp.gpGetTrigger(tn).Enable !=null && gp.gpGetTrigger(tn).Enable == false )
                {                
                    Console.WriteLine("MissionObjective initialize: WARNING: Objective Trigger " + tn + " : " + n + " .Enable=false; including it in the mission objective list nevertheless.");
                    return true;
                    //Just because it is disabled doesn't mean we can't include it in our objectives list.  Maybe it will be enabled later or whatever.
                    //Not even really sure what .Enable == false or .Enable == null means?
                }*/
            }
            return true;

        }

        public void addRadar(string n, string flak, int ownerarmy, double pts, double repair, string tn, string t, double p, double x, double y, double d, double e, bool pt, double ptp = 100, string comment = "", bool addNewOnly = false)
        {
            //Add the item -always when add==false and if it doesn't already exist, when add==true
            if (!addNewOnly || !msn.MissionObjectivesList.ContainsKey(tn))
            {
                if (!MO_SanityChecks(tn, n, MO_TriggerType.Trigger)) return; //sanity checks - we're skipping many items with the IF statement, so no need for sanity check before this point
                msn.MissionObjectivesList.Add(tn, new MissionObjective(msn, tn, n, flak, ownerarmy, pts, repair, t, p, x, y, d, e, pt, ptp, comment));
            }
        }

        public void addTrigger(MO_ObjectiveType mot, string n, string flak, int ownerarmy, double pts, string tn, string t = "", double p = 50, double x = 0, double y = 0, double d = 100, bool pt = false, double ptp = 100, double ttr_hours = 24, string comment = "", bool addNewOnly = false)
        {
            //Console.WriteLine("Adding Trigger pre " + tn + n + " " + pts.ToString());

            //Console.WriteLine("Adding Trigger post1 " + tn + n + " " + pts.ToString());
            //MissionObjective                                    (Mission m, MO_ObjectiveType mot,  string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, bool ptp, string comment)
            //Add the item -always when add==false and if it doesn't already exist, when add==true
            if (!addNewOnly || !msn.MissionObjectivesList.ContainsKey(tn))
            {
                if (!MO_SanityChecks(tn, n, MO_TriggerType.Trigger)) return; //sanity checks - we're skipping many items with the IF statement, so no need for sanity check before this point
                //Console.WriteLine("Adding Trigger post2 " + tn + n + " " + pts.ToString());
                if (ownerarmy == 1 && x > 210000 && y > 180000 && x < 321000 && y < 270000) ptp *= 1.4; //vastly increase proportion of mission objectives in 'primary' campaign area, and reduce others, for Blue 2020-01
                else ptp *= 0.8;
                msn.MissionObjectivesList.Add(tn, new MissionObjective(msn, mot, tn, n, flak, ownerarmy, pts, t, p, x, y, d, pt, ptp, ttr_hours, comment));
            }
        }
        public void addPointArea(MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double rad = 100, double trigrad = 300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af=true, bool afip=true, int fb=7, int fnib=8, string comment = "", bool addNewOnly = false)
        {
            //Console.WriteLine("Adding Trigger pre " + tn + n + " " + pts.ToString());

            //Console.WriteLine("Adding Trigger post1 " + tn + n + " " + pts.ToString());
            //MissionObjective                                    (Mission m, MO_ObjectiveType mot,  string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, bool ptp, string comment)
            //Add the item -always when add==false and if it doesn't already exist, when add==true
            if (!addNewOnly || !msn.MissionObjectivesList.ContainsKey(tn))
            {
                if (!MO_SanityChecks(tn, n, MO_TriggerType.PointArea)) return; //sanity checks - we're skipping many items with the IF statement, so no need for sanity check before this point
                //Console.WriteLine("Adding Trigger post2 " + tn + n + " " + pts.ToString());
                msn.MissionObjectivesList.Add(tn, new MissionObjective(msn, mot, tn, n, flak, initSub, ownerarmy, pts, x, y, rad, trigrad, orttkg, ortt, ptp, ttr_hours, af, afip, fb, fnib, comment));
            }
        }

        //public MissionObjective(Mission m, MO_ObjectiveType mot, string tn, string n, string flak, int ownerarmy, double pts, string t, double p, double x, double y, double rad, double trigrad, double orttkg, double ortt, bool pt, double ptp, double ttr_hr, string comment)

        //Flak field loads the corresponding flack file = large dictionary with name--->filename elsewhere
        //add=true means, we already loaded the triggers back from disk, but run through them & add any new ones from this list not already there.
        public void RadarPositionTriggersSetup(bool addNewOnly = false)
        {

            bool add = addNewOnly;
            //ID is the ID used in the [Trigger] portion of the .mis file. The central portion of the line can be copy/pasted from the  .mis file (then lightly edited)
            //Console.Write("#1a");

            //MissionObjective(Name,          Flak ID, OwnerArmy,points,ID, Days to repair, Trigger Type,Trigger percentage, location x, location y, trigger radius, radar effective radius, isPrimaryTarget, PrimaryTargetWeight (0-200), comment) {
            //weights change 0-200, many weights below adjusted, 2020-01
            addRadar("Westgate Radar", "WesR", 1, 4, 3, "BTarget14R", "TGroundDestroyed", 39, 244791, 262681, 150, 25000, false, 30, "", add);
            addRadar("Sandwich Radar", "SanR", 1, 4, 3, "BTarget15R", "TGroundDestroyed", 50, 248579, 253159, 200, 25000, false, 30, "", add);
            addRadar("Deal Radar", "DeaR", 1, 4, 3, "BTarget16R", "TGroundDestroyed", 75, 249454, 247913, 200, 25000, false, 30, "", add);
            addRadar("Dover Radar", "DovR", 1, 4, 3, "BTarget17R", "TGroundDestroyed", 75, 246777, 235751, 200, 25000, false, 30, "", add);
            addRadar("Brookland Radar", "BroR", 1, 4, 3, "BTarget18R", "TGroundDestroyed", 75, 212973, 220079, 200, 25000, false, 30, "", add);
            addRadar("Dungeness Radar", "DunR", 1, 4, 3, "BTarget19R", "TGroundDestroyed", 50, 221278, 214167, 200, 25000, false, 30, "", add);
            addRadar("Eastbourne Radar", "EasR", 1, 4, 3, "BTarget20R", "TGroundDestroyed", 75, 178778, 197288, 200, 25000, false, 10, "", add);
            addRadar("Littlehampton Radar", "LitR", 1, 4, 3, "BTarget21R", "TGroundDestroyed", 76, 123384, 196295, 200, 35000, false, 10, "", add);
            addRadar("Ventnor Radar", "VenR", 1, 4, 3, "BTarget22R", "TGroundDestroyed", 75, 70423, 171706, 200, 35000, false, 10, "", add);
            addRadar("Radar Communications HQ", "HQR", 1, 6, 3, "BTarget28", "TGroundDestroyed", 61, 180207, 288435, 200, 350000, false, 5, "", add);
            addRadar("Radar Poole", "PooR", 1, 6, 3, "BTarget23R", "TGroundDestroyed", 75, 15645, 170552, 200, 35000, false, 5, "", add);


            addRadar("Oye Plage Freya Radar", "OypR", 2, 4, 2, "RTarget28R", "TGroundDestroyed", 61, 294183, 219444, 50, 20000, false, 35, "", add);
            addRadar("Coquelles Freya Radar", "CoqR", 2, 4, 2, "RTarget29R", "TGroundDestroyed", 63, 276566, 214150, 50, 20000, false, 35, "", add);
            addRadar("Dunkirk Radar #2", "DuRN", 2, 4, 2, "RTarget30R", "TGroundDestroyed", 77, 341887, 232695, 100, 20000, false, 35, "", add);
            //    addRadar("Dunkirk Freya Radar",           "DuRN", 2, 1, 2, "RTarget38R", "TGroundDestroyed", 77, 339793, 232797,  100, 20000, false, 35, "", add);
            addRadar("Herderlot-Plage Freya Radar", "HePR", 2, 4, 2, "RTarget39R", "TGroundDestroyed", 85, 264882, 178115, 50, 20000, false, 35, "", add); //Mission in mission file
            addRadar("Berck Freya Radar", "BrkR", 2, 4, 2, "RTarget40R", "TGroundDestroyed", 86, 263234, 153713, 50, 20000, false, 5, "", add); //Mission in mission file
            addRadar("Radar Dieppee", "DieR", 2, 4, 2, "RTarget41R", "TGroundDestroyed", 85, 232727, 103248, 50, 20000, false, 5, "", add); //Mission in mission file; this is aduplicate of Radar DiEPPE, remove one or the other here AND in the .mis file
            addRadar("Radar Le Treport", "TreR", 2, 4, 2, "RTarget42R", "TGroundDestroyed", 86, 250599, 116531, 50, 20000, false, 15, "", add); // Mission in mission file
            addRadar("Radar Somme River", "SomR", 2, 4, 2, "RTarget43R", "TGroundDestroyed", 86, 260798, 131885, 50, 20000, false, 5, "", add); //Mission in mission file
            addRadar("Radar AMBETEUSE", "AmbR", 2, 4, 2, "RTarget44R", "TGroundDestroyed", 86, 266788, 197956, 50, 20000, false, 5, "", add); //Mission in mission file
            addRadar("Radar BOULOGNE", "BlgR", 2, 4, 2, "RTarget45R", "TGroundDestroyed", 85, 264494, 188674, 50, 20000, false, 35, "", add); //Mission in mission file           
            addRadar("Radar Le Touquet", "L2kR", 2, 4, 2, "RTarget46R", "TGroundDestroyed", 66, 265307, 171427, 50, 20000, false, 5, "", add); //Mission in mission file
            addRadar("Radar Dieppe", "FreR", 2, 4, 2, "RTarget47R", "TGroundDestroyed", 99, 232580, 103325, 50, 20000, false, 15, "", add); //Mission in mission file
            addRadar("Veulettes-sur-Mer Radar", "VeuR", 2, 4, 2, "RTarget48R", "TGroundDestroyed", 100, 195165, 93441, 50, 20000, false, 5, "", add);//Mission in mission file
            addRadar("Le Havre Freya Radar", "LhvR", 2, 4, 2, "RTarget49R", "TGroundDestroyed", 100, 157636, 60683, 50, 20000, false, 15, "", add);//Mission in mission file
            addRadar("Ouistreham Freya Radar", "OuiR", 2, 4, 2, "RTarget50R", "TGroundDestroyed", 100, 135205, 29918, 50, 20000, false, 15, "", add);// Mission in mission file
            addRadar("Bayeux Beach Freya Radar", "BayR", 2, 4, 2, "RTarget51R", "TGroundDestroyed", 100, 104279, 36659, 50, 20000, false, 5, "", add); //Mission in mission file
            addRadar("Beauguillot Beach Freya Radar", "BchR", 2, 4, 2, "RTarget52R", "TGroundDestroyed", 100, 65364, 43580, 50, 20000, false, 5, "", add); //Mission in mission file
            addRadar("Radar Tatihou", "TatR", 2, 4, 2, "RTarget53R", "TGroundDestroyed", 77, 60453, 63873, 50, 30000, false, 5, "", add); //Mission in mission file
            addRadar("Radar Querqueville", "QueR", 2, 4, 2, "RTarget54R", "TGroundDestroyed", 100, 17036, 77666, 50, 30000, false, 15, "", add); // Mission in mission file

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
  RTarget29R TGroundDestroyed 63 276566 214150 50
  RTarget30R TGroundDestroyed 77 341887 232695 100
  RTarget38R TGroundDestroyed 85 341866 232710 50
  RTarget39R TGroundDestroyed 85 276567 214150 50
  RTarget40R TGroundDestroyed 86 263234 153713 50
  RTarget41R TGroundDestroyed 85 232576 103318 50
  RTarget42R TGroundDestroyed 86 250599 116531 50
  RTarget43R TGroundDestroyed 86 262560 133020 50
  RTarget44R TGroundDestroyed 86 266788 197956 50
  RTarget45R TGroundDestroyed 85 264266 188554 50
  RTarget46R TGroundDestroyed 66 266625 169936 50
  RTarget47R TGroundDestroyed 99 185931 88085 50
  RTarget48R TGroundDestroyed 100 195165 93441 50
  RTarget49R TGroundDestroyed 100 157636 60683 50
  RTarget50R TGroundDestroyed 100 135205 29918 50
  RTarget51R TGroundDestroyed 100 103641 36893 50
  RTarget52R TGroundDestroyed 100 65637 44013 50
  RTarget53R TGroundDestroyed 77 60453 63873 50
  RTarget54R TGroundDestroyed 100 17036 77666 50


            */

        }

        public void MissionObjectiveTriggersSetup(bool addNewOnly = false)
        {
            //Format: addTrigger(MO_ObjectiveType.Building (Aircraft, airport, etc), "Name,                      OwnerArmy,Points,ID,TriggerType,PercRequired,XLoc,YLoc,Radius,IsPrimaryTarget,IsPrimaryTargetWeight,TimeToRepairIfDestroyed_hours,Comment "");
            bool add = addNewOnly;
            //BLUE TARGETS
            addTrigger(MO_ObjectiveType.Aircraft, "Littlestone Bombers", "Litt", 1, 3, "BTarget1", "TGroundDestroyed", 20, 222303, 221176, 300, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.AirfieldComplex, "Redhill Bomber Base", "Redh", 1, 5, "BTarget2", "TGroundDestroyed", 20, 143336, 240806, 550, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Ashford Train Depot Armour", "Ashf", 1, 3, "BTarget3", "TGroundDestroyed", 20, 214639, 235604, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Aircraft, "Manston aircraft", "Mans", 1, 2, "BTarget4", "TGroundDestroyed", 75, 247462, 259157, 250, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Vehicles, "British Armour @ Dover", "Dove", 1, 3, "BTarget5", "TGroundDestroyed", 80, 243887, 236956, 200, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Vehicles, "British Armour @ CreekMouth", "Bext", 1, 5, "BTarget6", "TGroundDestroyed", 50, 159687, 275015, 200, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Diesel Fuel London South Docks", "Lond", 1, 3, "BTarget6S", "TGroupDestroyed", 70, 154299, 273105, 100, false, 100, 24, "", add);//removed all ships "Designation S" used oil storage instead
            addTrigger(MO_ObjectiveType.Fuel, "Hydrogen Storage @ London South Docks", "Lond", 1, 3, "BTarget7S", "TGroundDestroyed", 80, 155050, 273258, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Ethanol Storage @ London South Docks", "Lond", 1, 5, "BTarget8S", "TGroundDestroyed", 80, 155823, 273221, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Liquid Oxygen @ Beckton", "Bext", 1, 5, "BTarget9S", "TGroundDestroyed", 80, 157899, 273957, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Kerosene Storage @ Beckton", "Bext", 1, 5, "BTarget10S", "TGroundDestroyed", 80, 157547, 274527, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "High Octane Aircraft Fuel @ Beckton", "Bext", 1, 5, "BTarget11S", "TGroundDestroyed", 80, 158192, 274864, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "87 Octane Fuel Storage @ Beckton", "Bext", 1, 5, "BTarget12S", "TGroundDestroyed", 63, 157899, 275256, 50, false, 100, 24, "", add);
            //    addTrigger(MO_ObjectiveType.Fuel, "Peroxide Storage @ Beckton", "Bext", 1, 5, "BTarget13S", "TGroundDestroyed", 66, 157092, 275312, 50, false, 20, 24, "", add);
            addTrigger(MO_ObjectiveType.AA, "Peroxide Storage @ Beckton", "Lond", 1, 3, "BTarget13S", "TGroundDestroyed", 63, 160567, 275749, 10, false, 4, 24, "", add);
            addTrigger(MO_ObjectiveType.AA, "Vehicle Departure Docks", "Lond", 1, 2, "BTarget14A", "TGroundDestroyed", 63, 160025, 273824, 10, false, 4, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Ditton Fuel Refinery", "Ditt", 1, 4, "BTarget24", "TGroundDestroyed", 75, 185027, 252619, 100, false, 100, 24, "", add);// fixed triggers missing
            addTrigger(MO_ObjectiveType.Fuel, "Ditton Fuel Storage", "Ditt", 1, 4, "BTarget25", "TGroundDestroyed", 80, 186057, 251745, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Maidstone Train Repair Station ", "Ditt", 1, 3, "BTarget26", "TGroundDestroyed", 60, 189272, 249311, 50, false, 100, 24, "", add);
            //addTrigger(MO_ObjectiveType.Building, "Billicaray Factory", "RaHQ", 1, 2, "BTarget27", "TGroundDestroyed", 85, 180141, 288423, 150, false, 100, 24, "", add); //So in the .mis file BTarget27 is 180xxx / 288xxx which is the Billicaray area.  I don't know if we have flak for that?  Flak 'Tunb' is definitely noit going to work. Flug 2018/10/08       
            addTrigger(MO_ObjectiveType.Building, "Tunbridge Wells Armoury", "Tunb", 1, 4, "BTarget27", "TGroundDestroyed", 85, 173778, 233407, 100, false, 70, 24, "", add); //This target was left out of the .cs and .mis files until now, but I'm pretty sure it was what is intended for Tunbridge Wells Armory.  So I added it to the .mis and .cs files right now. Flug 2018/10/08
            addTrigger(MO_ObjectiveType.Building, "Bulford Army Facility", "Bult", 1, 7, "BTarget29", "TGroundDestroyed", 90, 35872, 236703, 200, false, 10, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Wooleston Spitfire Shop ", "Wool", 1, 7, "BTarget30", "TGroundDestroyed", 81, 56990, 203737, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Swindon Aircraft repair Station", "Swin", 1, 6, "BTarget31", "TGroundDestroyed", 75, 29968, 279722, 300, false, 3, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Reading Engine Workshop ", "Read", 1, 7, "BTarget32", "TGroundDestroyed", 83, 84241, 267444, 300, false, 10, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Propeller Repair Portsmouth", "Port", 1, 6, "BTarget33", "TGroundDestroyed", 81, 76446, 193672, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Diesel Storage Portsmouth", "Port", 1, 6, "BTarget34", "TGroundDestroyed", 75, 76476, 193844, 50, false, 100, 24, "", add); //This might have wrong location? 9/24
            addTrigger(MO_ObjectiveType.Building, "Boiler Repair Shop Portsmouth", "Port", 1, 5, "BTarget35", "TGroundDestroyed", 71, 76317, 193904, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Main Fuel Portsmouth", "Port", 1, 6, "BTarget36", "TGroundDestroyed", 75, 76378, 194163, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Depth Charge Workshop Portsmouth", "Port", 1, 5, "BTarget37", "TGroundDestroyed", 83, 76720, 194082, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Liquid Oxygen Storage Portsmouth", "Port", 1, 6, "BTarget38", "TGroundDestroyed", 75, 76805, 193918, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Wood Alcohol Fuel Storage Portsmouth", "Port", 1, 5, "BTarget39", "TGroundDestroyed", 98, 77392, 193942, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Portsmouth Hydrogen Storage", "Port", 1, 4, "BTarget40", "TGroundDestroyed", 95, 77317, 193860, 50, false, 100, 24, "", add); //This is in Portsmouth    .  
            addTrigger(MO_ObjectiveType.Building, "Portsmouth Torpedo Facility", "Port", 1, 4, "BTarget41", "TGroundDestroyed", 72, 76855, 194410, 50, false, 100, 24, "", add); //This is in Portsmouth.fixed 9/19 fatal
            addTrigger(MO_ObjectiveType.Fuel, "Guildford High Octane Plant", "Guil", 1, 5, "BTarget42", "TGroundDestroyed", 89, 112441, 243834, 200, false, 100, 24, "", add); //Guildford Target added 9/20
            addTrigger(MO_ObjectiveType.Fuel, "Sheerness Diesel Fuel Storage", "Quee", 1, 3, "BTarget43", "TGroundDestroyed", 63, 204654, 268378, 50, false, 100, 24, "", add);//Sheerness Diesel Fuel Storage
            addTrigger(MO_ObjectiveType.Building, "Queensborough Navigational jamming facilities", "Quee", 1, 4, "BTarget44", "TGroundDestroyed", 74, 204638, 265195, 50, false, 100, 24, "", add); // "Queensborough Navigational Jamming Facilities"
            addTrigger(MO_ObjectiveType.Building, "Queensborough Radio communications center", "Quee", 1, 4, "Btarget45", "TGroundDestroyed", 74, 204722, 265252, 50, false, 100, 24, "", add);  // "Queensborough Radio Communications center"
            addTrigger(MO_ObjectiveType.Building, "Queensborough Radio Transmission Booster", "Quee", 1, 4, "BTarget46", "TGroundDestroyed", 74, 204570, 265131, 50, false, 100, 24, "", add); //  "Queensborough radio Transmission booster"
            addTrigger(MO_ObjectiveType.Building, "Queensborough Electrical Research Facility", "Quee", 1, 4, "BTarget47", "TGroundDestroyed", 74, 204716, 265140, 50, false, 100, 24, "", add); //  "Queensborough Electrical Research Facility"
            addTrigger(MO_ObjectiveType.Building, "Littlestone Research Facility", "Litt", 1, 4, "littlestonehang", "TGroundDestroyed", 66, 221988, 221642, 50, false, 100, 24, "", add); //  "Littlestone research facility"
            
            addTrigger(MO_ObjectiveType.Building, "Diehl Military Train Station", "Dove", 1, 4, "BTargDiehlTrainStation", "TGroundDestroyed", 10, 251138, 245883, 50, false, 120, 24, "", add);            
            addTrigger(MO_ObjectiveType.Building, "Broadstairs Train Station Military Complex", "Mans", 1, 4, "BTargBroadstairsTrainStation", "TGroundDestroyed", 10, 252836, 261369, 50, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Brighton Army Recruitment Station", "Shor", 1, 4, "BTargBrightonMilitaryRecruitment", "TGroundDestroyed", 11, 144654, 198443, 50, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Brighton Gasoline Storage", "Shor", 1, 4, "BTargBrightonFuel", "TGroundDestroyed", 21, 144738, 198233, 50, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Tenterden Chemical Manufacture", "Litt", 1, 4, "BTargTenterdenChemicalFactory", "TGroundDestroyed", 12, 194591, 220821, 150, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Minster Synthetic Case Oil Manufacture", "Mans", 1, 4, "BTargMinsterCaseOilManufacturing", "TGroundDestroyed", 10, 240203, 256964, 100, false, 120, 24, "", add);      
            addTrigger(MO_ObjectiveType.Building, "Battle Commando Training Center", "Shor", 1, 3, "BTargBattleCommandoTrainingCenter", "TGroundDestroyed", 11, 185093, 219403, 50, false, 120, 24, "", add);
            //addTrigger(MO_ObjectiveType.Building, "Shoreham Submarine Base", "", 1, 3, "BTargShorehamSubmarineBase", "TGroundDestroyed", 10, 137054, 198034, 50, false, 120, 24, "", add);           


            /*
             * BTargPortsmouthSmallIndustrialArea TGroundDestroyed 35 75235 193676 350
              BTargPortsmouthLargeIndustrialArea TGroundDestroyed 27 77048 193985 850
              BTargPooleNorthIndustrialPortArea TGroundDestroyed 33 14518 184740 400
              BTargPooleSouthIndustrialPortArea TGroundDestroyed 34 13734 183493 400 */
            //BTargShorehamSubmarineBase TGroundDestroyed 10 137054 198034 50
            /*
			littlestonehang TGroundDestroyed 66 221988 221642 50
              BTarget6S TGroundDestroyed 70 154299 273105 100     "Diesel fuel London south docks", 1, 3, "r
              BTarget7S TGroundDestroyed 80 155050 273258 50      "Hydrogen Storage @ London south docks", 
              BTarget8S TGroundDestroyed 80 155823 273221 50      "Ethanol Storage @ London south docks", 1
              BTarget9S TGroundDestroyed 80 157899 273957 50      "Liquid Oxygen @ Beckton", 1, 2, "BTar
              BTarget10S TGroundDestroyed 80 157547 274527 100    "Kerosene Storage @ Beckton", 1, 2, 
              BTarget11S TGroundDestroyed 80 158192 274864 50     "High Octane aircraft fuel @ Beckton
              BTarget12S TGroundDestroyed 63 157899 275256 50     "87 octane fuelstorage @ Beckton", 1
              BTarget13S TGroundDestroyed 66 157092 275312 50     "Peroxide Storage @ Beckton", 1, 2, 
			  BTarget14A", "TGroundDestroyed"     63, 160025, 273824, 10,                  "Vehicle departure docks"
			    BTarget43 TGroundDestroyed 63 204654 268378 50    "Sheerness Diesel Fuel Storage"
                BTarget44 TGroundDestroyed 74 204638 265195 50    "Queensborough Navigational jamming facilities"
                Btarget45 TGroundDestroyed 74 204722 265252 50     "Queensborough Radio communications center"
                BTarget46 TGroundDestroyed 74 204570 265131 50     "Queensborough radio tramsmission booster"
                BTarget47 TGroundDestroyed 74 204716 265140 50     "Queensborough Electrical Research Facility"
            */
            /*
              BTargDoverAmmo TGroundDestroyed 8 245461 233488 50
              BTargDiehlTrainStation TGroundDestroyed 10 251138 245883 50
              BTargDoverFuel TGroundDestroyed 11 245695 233573 100
              BTargBroadstairsTrainStation TGroundDestroyed 10 252836 261369 50
              BTargBrightonMilitaryRecruitment TGroundDestroyed 11 144654 198443 50
              BTargBrightonFuel TGroundDestroyed 21 144738 198233 50
              BTargTenterdenChemicalFactory TGroundDestroyed 12 194591 220821 150
              BTargMinsterCaseOilManufacturing TGroundDestroyed 10 240203 256964 100
              BTargDoverNavalOffice TGroundDestroyed 10 245567 233499 50
 
             
              */


            //RED TARGETS
            addTrigger(MO_ObjectiveType.Vehicles, "Bapaume Rail Transit Station", "Bapu", 2, 8, "RTarget0", "TGroundDestroyed", 83, 354623, 121058, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Vehicles, "Motorpool near Grand-Fort Philippe", "MPGP", 2, 3, "RTarget1", "TGroundDestroyed", 50, 299486, 220998, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "St. Omer Ball bearing Factory", "Omar", 2, 4, "RTarget2", "TGroundDestroyed", 33, 313732, 192700, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Estree Fuel Depot", "Estr", 2, 4, "RTarget3", "TGroundDestroyed", 40, 280182, 164399, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Synthetic Fuel", "Boul", 2, 3, "RTarget4", "TGroundDestroyed", 60, 265005, 190321, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.RRYard, "Calais Rail Yard", "Cala", 2, 3, "RTarget5", "TGroundDestroyed", 60, 283995, 215369, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Hydrogen", "Cala", 2, 4, "RTarget6", "TGroundDestroyed", 60, 284867, 216414, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Calais Main Fuel", "Cala", 2, 4, "RTarget7", "TGroundDestroyed", 60, 285518, 217456, 100, false, 100, 24, "", add); //g
            addTrigger(MO_ObjectiveType.Building, "Calais LOX", "Cala", 2, 4, "RTarget8", "TGroundDestroyed", 60, 285001, 215944, 100, false, 100, 24, "", add); //g
            addTrigger(MO_ObjectiveType.Fuel, "Calais Torpedo Factory", "Cala", 2, 4, "RTarget9", "TGroundDestroyed", 60, 284831, 216887, 100, false, 100, 24, "", add); //g
            addTrigger(MO_ObjectiveType.Fuel, "Calais Diesel Storage", "Cala", 2, 4, "RTarget10", "TGroundDestroyed", 60, 285040, 217547, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Aviation", "Boul", 2, 4, "RTarget11", "TGroundDestroyed", 43, 265591, 189902, 100, false, 100, 24, "", add);   //g 
            addTrigger(MO_ObjectiveType.Building, "Boulogne Diesel", "Boul", 2, 4, "RTarget12", "TGroundDestroyed", 50, 266651, 187088, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Benzine", "Boul", 2, 4, "RTarget13", "TGroundDestroyed", 52, 266160, 189276, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne LOX", "Boul", 2, 4, "RTarget14", "TGroundDestroyed", 50, 264515, 188950, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Ethanol", "Boul", 2, 4, "RTarget15", "TGroundDestroyed", 50, 264984, 189378, 100, false, 100, 24, "", add); //g
            addTrigger(MO_ObjectiveType.Fuel, "Arras Main Fuel", "Arra", 2, 7, "RTarget16", "TGroundDestroyed", 50, 350605, 142047, 50,
                     false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Arras Rubber Factory", "Arra", 2, 6, "RTarget17", "TGroundDestroyed", 50, 352039, 141214, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "St Ouen AAA Factory", "Stou", 2, 6, "RTarget18", "TGroundDestroyed", 50, 303445, 114053, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Abbeville Fuel", "Abbe", 2, 5, "RTarget19", "TGroundDestroyed", 50, 285075, 121608, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Dieppe Fuel", "Diep", 2, 4, "RTarget20", "TGroundDestroyed", 50, 229270, 101222, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Le Treport Fuel", "LeTr", 2, 4, "RTarget21", "TGroundDestroyed", 50, 250477, 116082, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Poix Nord Fuel Trucks", "Poix", 2, 5, "RTarget22", "TGroundDestroyed", 50, 293827, 84983, 150, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Chemical Research Facility", "Cala", 2, 4, "RTarget23", "TGroundDestroyed", 75, 285254, 216717, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Optical Research Facility", "Cala", 2, 4, "RTarget24", "TGroundDestroyed", 100, 285547, 216579, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Chemical Storage", "Cala", 2, 4, "RTarget25", "TGroundDestroyed", 75, 285131, 216913, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Rations Storage", "Cala", 2, 4, "RTarget26", "TGroundDestroyed", 78, 284522, 216339, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Gunpowder Facility", "Cala", 2, 4, "RTarget27", "TGroundDestroyed", 50, 284898, 216552, 50, false, 100, 24, "", add);  //g  //  addTrigger(MO_ObjectiveType.Ship, "Minensuchboote", "Abbe", 2, 2, "RTarget30S", "TGroupDestroyed", 90, 263443, 181488, 0, false, 100, "0_Chief  Minensuchtboot");   //removed from the mission
            addTrigger(MO_ObjectiveType.Fuel, "Arras Fuel Storage 2", "Arra", 2, 7, "RTarget31", "TGroundDestroyed", 75, 351371, 141966, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Watten Armory", "watt", 2, 5, "RTarget32", "TGroundDestroyed", 60, 310395, 200888, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Half Track Factory Dunkirk", "Dunk", 2, 4, "RTarget33", "TGroundDestroyed", 50, 314794, 224432, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Steel Mill Dunkirk", "Dunk", 2, 4, "RTarget34", "TGroundDestroyed", 75, 315081, 224145, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Brass Smelter Dunkirk", "Dunk", 2, 4, "RTarget35", "TGroundDestroyed", 75, 314832, 223389, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Fuel, "Diesel Storage Dunkirk", "Dunk", 2, 4, "RTarget36", "TGroundDestroyed", 75, 314482, 223882, 200, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Ammunition Warehouse Dunkirk", "Dunk", 2, 4, "RTarget37", "TGroundDestroyed", 75, 313878, 223421, 100, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Low Smoke Diesel Le Havre", "Havr", 2, 5, "RTarget38", "TGroundDestroyed", 70, 161702, 52073, 100, false, 100, 24, "", add);  //This is in Le Havre, fuel tanks area. I added 3-4 jerry cans to the area in the .mis so it is a valid target now //g
            addTrigger(MO_ObjectiveType.Building, "Calais Water Treatment", "Cala", 2, 2, "9A", "TGroundDestroyed", 63, 296130, 218469, 50, false, 2, 24, "", add); //I think the locations of the AAA batteries are off? Ok, checking with the .mis file, the order was just reversed and the wrong name with the wrong battery. 1A..9A vs 9A..1A.  Now fixed to match .mis file 9/19/2018
            addTrigger(MO_ObjectiveType.Building, "Coastal Command Calais", "Cala", 2, 4, "8A", "TGroundDestroyed", 75, 294090, 85100, 100, false, 2, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Calais Rope Factory", "Cala", 2, 3, "7A", "TGroundDestroyed", 66, 293279, 84884, 100, false, 2, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Ammunition Warehouse Boulogne", "Boul", 2, 4, "1B", "TGroundDestroyed", 70, 264252, 189991, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Fuel Research Facility Boulogne", "Boul", 2, 5, "2B", "TGroundDestroyed", 47, 265063, 190506, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Radio Jamming Transmitter Boulogne", "Boul", 2, 2, "3B", "TGroundDestroyed", 51, 265251, 190259, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Naval  Research Facility Boulogne", "Boul", 2, 4, "4B", "TGroundDestroyed", 62, 264692, 189709, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Boulogne Army HQ", "Boul", 2, 4, "5B", "TGroundDestroyed", 54, 265643, 189603, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Propeller Repair Boulogne", "Boul", 2, 4, "6B", "TGroundDestroyed", 77, 265932, 189324, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "E-boat Factory Boulogne", "Boul", 2, 4, "7B", "TGroundDestroyed", 53, 264849, 189190, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Main Facility", "Havr", 2, 4, "LehavNaval1", "TGroundDestroyed", 84, 163216, 49915, 50, false, 100, 24, "", add);    //added to targets list in mission and here in CS  fatal 9/22
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Officer Mess", "Havr", 2, 4, "LehavNaval2", "TGroundDestroyed", 71, 163447, 49855, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Weapons Training", "Havr", 2, 4, "LehavNaval3", "TGroundDestroyed", 75, 163313, 50063, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Underwater Repair Training", "Havr", 2, 4, "LehavNaval4", "TGroundDestroyed", 81, 163039, 49798, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Naval Intelligence", "Havr", 2, 4, "LehavNaval5", "TGroundDestroyed", 71, 163172, 49816, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Meteorolgy", "Havr", 2, 4, "LehavNaval6", "TGroundDestroyed", 89, 163470, 49752, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Cryptologic HQ", "Havr", 2, 4, "LehavNaval7", "TGroundDestroyed", 75, 162993, 49927, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Le Havre Naval Diesel Storage", "Havr", 2, 4, "LehavNavalDiesel", "TGroundDestroyed", 46, 162559, 50082, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Le Havre Naval Gear Oil Storage", "Havr", 2, 4, "LehavNavalGearOil", "TGroundDestroyed", 41, 162668, 50240, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Le Havre Naval Benzine", "Havr", 2, 4, "LehavNavalBenzine", "TGroundDestroyed", 35, 161747, 50094, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Le Havre Naval LOX", "Havr", 2, 4, "LehavNavalLOX", "TGroundDestroyed", 41, 162099, 50034, 50, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Le Havre Train Station", "Havr", 2, 4, "LehavTrainStation", "TGroundDestroyed", 37, 159918, 53120, 100, false, 100, 24, "", add);
            addTrigger(MO_ObjectiveType.Fuel, "Estree Secret Facility", "Estr", 2, 8, "Estree_Secret", "TGroundDestroyed", 61, 279623, 163613, 50, false, 100, 24, "", add);  //g
            addTrigger(MO_ObjectiveType.Building, "Marquise Fuel Dump", "Quee", 2, 3, "RTargMarquiseFuelDump", "TGroundDestroyed", 13, 274209, 199150, 100, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Calais UBoot Repair", "Cala", 2, 4, "RTargCalaisUBootRepair", "TGroundDestroyed", 10, 284999, 216446, 100, false, 120, 24, "", add);
             addTrigger(MO_ObjectiveType.Building, "Calais Jackboot Storage", "Cala", 2, 4, "RTargCalaisJackbootStorage", "TGroundDestroyed", 12, 284994, 216869, 50, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Dunkirk Weapon Storage & Distribution", "Dunk", 2, 4, "RTargDunkirkWeaponStoarge", "TGroundDestroyed", 11, 315271, 224033, 100, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Dunkirk Radar Manufacture", "Dunk", 2, 4, "RTargDunkirkRadarManufacturing", "TGroundDestroyed", 10, 315295, 224146, 100, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Dunkirk Military Warehouse", "Dunk", 2, 4, "RTargDunkirkMilitaryWarehouse", "TGroundDestroyed", 10, 315300, 224265, 100, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Dunkirk Explosives Research", "Dunk", 2, 4, "RTargDunkirkExplosivesResearch", "TGroundDestroyed", 9, 314884, 223318, 50, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Desvres Aviation Fuel", "", 2, 4, "RTargDesvresAviationFuel", "TGroundDestroyed", 9, 284580, 182275, 150, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Aire-Sur-La-Lys Chemical Refinery", "", 2, 4, "RTargAireSurLaLysChemicalRefinery", "TGroundDestroyed", 12, 323803, 181252, 100, false, 120, 24, "", add);
            addTrigger(MO_ObjectiveType.Building, "Etaple Fuel Refinery/Storage", "", 2, 4, "RTargEtapleFuelDump", "TGroundDestroyed", 11, 267479, 166274, 100, false, 120, 24, "", add);


            //public void addPointArea(MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double rad = 100, double trigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)
            // n is the DESCRIPTIVE NAME for the target--what the player sees
            //rad is the radius-extent to the object itself.  Say an airfield will have a certain radius, or a general industrial area or military base.
            //trigrad is like the center of the bullseye--the radius from the center within which the bombs, killed objectives etc will count THE MOST towards disabling the objective.  Anything outside this radius (but still inside the main radius of the object) won't count as much--but still counts.  Anything inside the radius counts more.  Anything outside both radii counts zero.
            //   - for example you might have an airbase with radius 2500 meters but you want the bombs to hit more in the center of that to count, so you set rad=2500, trigrad=1000.
            // tn is the INTERNAL KEY for the object.  It can be anything you want to identify it to yourself and the computer, but it must be unique (ie,different from ALL OTHER objectives listed here).
            //orttkg = kg of ordnance that must be dumped in this area to knock it out
            //ortt = number of objects (statics, buildings, AiActors like vehicles, trains, whatever) that must be killed within the area to knock it out. Note however that there is a scoring system and for example ships count 8-20 points, artillery/tank 4, planes on the ground 2, bridge 10, trucks/armoured vehicles 2, etc.   See MO_HandlePointAreaObjectives.
            //     - if you set this higher than 0 you MUST have some objects within the radius of the area, preferably the trigger radius, or taking out the objective will be impossible
            //     - AiActors such as vehicles, ships, trains, planes, artillery, etc also count as 'objects' that count towards this goal.  BUT they must be strictly within the radius given.  If they wander outside the radius, they are no longer part of this objective
            //     - Buildings can be counted to a limited extent in the future, but for now they are not (until TF resolves the OnBuildingKilled thing, which currently works for AI but not live players
            //     - You can add objects/actors, etc to the -main.cs file OR to a XXXXMission-initsubmission.mis file, which is loaded by this .cs file right at mission startup.  It is a lot cleaner to keep things in separate submission.mis files.
            //     - Note that you cannot add BUILDINGS in an -initsubmission file.  You can add them in FMB but they don't show up in-game.  However you could add a building in the -main.mis file and then other objects in an -initsubmission.mis file.
            //You can specify orttkg OR ortt OR both - if both, the player must satisfy both conditions to knock out
            //initSub is a submission that will be loaded when the mission starts, if this objective is enabled
            //ptp = primary target weight, ie, 0-200 increase or decrease chance of selection as a primary target.        
            //ttr_hours = time to repair, in hours, if taken out 100%.
            //af = add auto-flak batteries always
            //afip = add auto-flak batteries only if a primary target
            //fb = number of flak batteries to add (if a primary target)
            //fnib = number of guns in each battery (if a primary target)
            //Note that the number of batteries & guns per battery is only used if the objective is a current primary target. Otherwise just a much smaller amount of flak is put in place.
            //That's because too many flak installations seems to bring the server to its knees.
            addPointArea(MO_ObjectiveType.Building, "Dover Naval HQ", "Dove", "", 1, 3, "BTargDoverNavalOffice", 245567, 233499, 50, 50, 800, 4, 120, 48, true, true, 4, 7, "", add);
            addPointArea(MO_ObjectiveType.Building, "Dover Ammo Dump", "Dove", "", 1, 3, "BTargDoverAmmo", 245461, 233488, 50, 50, 800, 4, 120, 48, true, true, 4, 7, "", add);
            addPointArea(MO_ObjectiveType.Building, "Dover Naval Operations Fuel", "Dove", "", 1, 4, "BTargDoverFuel", 245695, 233573, 75, 75, 800, 4, 120, 48, true, true, 4, 7, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Southhampton Docks Industrial Area", "Sout", "", 1, 4, "SouthhamptonDocks", 56298, 203668, 400, 400, 8000, 0, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.MilitaryArea, "Shoreham Submarine Base", "", "", 1, 3, "BTargShorehamSubmarineBase", 137054, 198034, 90, 150, 2000, 5, 120, 24, true, true, 8, 10, "", add);

            addPointArea(MO_ObjectiveType.IndustrialArea, "Portsmouth Small Industrial Area SW", "Port", "", 1, 4, "BTargPortsmouthSmallIndustrialArea", 75235, 193676, 350, 350, 8000, 10, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Portsmouth Large Industrial Area NE", "Port", "", 1, 5, "BTargPortsmouthLargeIndustrialArea", 77048, 193985, 850, 850, 10000, 15, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Poole North Industrial Port Area", "Pool", "", 1, 5, "BTargPooleNorthIndustrialPortArea", 14518, 184740, 400, 550, 10000, 10, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Poole South Industrial Port Area", "Pool", "", 1, 4, "BTargPooleSouthIndustrialPortArea", 13734, 183493, 400, 550, 8000, 8, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Crowborough RAF High Command Bunker", "", "", 1, 6, "CrowboroughBunker", 167289, 224222, 70, 50, 4000, 20, 120, 24, true, true, 10, 12, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Hastings Local Auxiliary Bunker", "", "", 1, 6, "HastingsBunker", 196108, 205853, 70, 50, 4000, 20, 120, 24, true, true, 10, 12, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Folkestone Navy Docks Area", "Folk", "", 1, 6, "BTargFolkestoneNavyDocks", 237398, 228979, 700, 600, 0, 80, 160, 24, true, true, 8, 10, "", add); //Because it's  a dock most bombs hit on "water", thus they don't count.  So it's hard to get a lot of ordnance KG on it.  Rely mostly on static kills for that reason.  Ships in the harbor count for 10 and there are 7-8 of them, so getting 50 points on ships = not that hard.
            //public void addPointArea(MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double rad = 100, double trigrad = 300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af = true, bool afip = true, int fb = 7, int fnib = 8, string comment = "", bool addNewOnly = false)

            addPointArea(MO_ObjectiveType.MilitaryArea, "Estree Amphibious Landing Training Center", "Estr", "", 2, 4, "RTargEstreeAmphib", 279617, 163616, 150, 200, 2000, 1, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.MilitaryArea, "Etaples Landing Craft Assembly Site", "", "", 2, 4, "RTargEtaplesLandingCraft", 269447, 166097, 150, 200, 2000, 1, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.MilitaryArea, "Berck Amphibious Craft Assembly Site", "", "", 2, 4, "RTargBerckLandingCraft", 269247, 147771, 150, 200, 2000, 1, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Calais Docks Area", "Cala", "", 2, 4, "RTargCalaisDocksArea", 284656, 217404, 250, 350, 8000, 10, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.MilitaryArea, "Veume Military Manufacturing Area", "", "", 2, 4, "RTargVeumeMilitaryManufacturingArea", 342180, 228344, 200, 250, 8000, 15, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.MilitaryArea, "Le Crotoy Landing Craft Manufacturing Area", "", "", 2, 6, "RTargLeCrotoyLandingCraftManufactureAreaBomb", 271378, 132785, 600, 1000, 12000, 5, 120, 24, true, true, 8, 10, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Le Crotoy Forest Luftwaffe High Command Bunker", "", "", 2, 6, "LeCrotoyForestBunker", 277853, 138221, 70, 50, 4000, 20, 120, 24, true, true, 10, 12, "", add);
            addPointArea(MO_ObjectiveType.IndustrialArea, "Dieppe Cliffside German Special Forces Command Bunker", "", "", 2, 6, "DieppeCliffsBunker", 238972, 107365, 70, 50, 4000, 20, 120, 24, true, true, 10, 12, "", add);

////$include "C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\Visual Studio 2015\Projects\ClodBLITZ-2018-01\Campaign21-MissionObjectivesInclude.cs"

            /* addTrigger(MO_ObjectiveType.Building, "Portsmouth Small Industrial Area SW", "Port", 1, 4, "BTargPortsmouthSmallIndustrialArea", "TGroundDestroyed", 35, 75235, 193676, 350, false, 120, 24, "", add);
addTrigger(MO_ObjectiveType.Building, "Portsmouth Large Industrial Area NE", "Port", 1, 5, "BTargPortsmouthLargeIndustrialArea", "TGroundDestroyed", 27, 77048, 193985, 850, false, 120, 24, "", add);
addTrigger(MO_ObjectiveType.Building, "Poole North Industrial Port Area", "Pool", 1, 5, "BTargPooleNorthIndustrialPortArea", "TGroundDestroyed", 33, 14518, 184740, 400, false, 120, 24, "", add);
addTrigger(MO_ObjectiveType.Building, "Poole South Industrial Port Area", "Pool", 1, 4, "BTargPooleSouthIndustrialPortArea", "TGroundDestroyed", 34, 13734, 183493, 400, false, 120, 24, "", add);
*/




            /*
             * 
                RTargBerckLandingCraft TGroundDestroyed 14 269247 147771 150
                RTargLeCrotoyLandingCraftManufactureAreaBomb TGroundDestroyed 25 271378 132785 1000 
              RTargEstreeArmyTraining TGroundDestroyed 4 279617 163616 100
              RTargEtaplesLandingCraft TGroundDestroyed 7 269447 166097 150
             * */
            /*41 162099 50034 50
         *

***************** add unfinished convoys for british attack here***********



    /*    
*/
        }
        //Names of the flak areas and link to file name
        //Name is used in list of objectives aboe & must match exactly.  You can change the name below but then the name in the addTrigger etc above must also be changed to match
        //file name must match exactly with the filename
        public Dictionary<string, string> FlakMissions = new Dictionary<string, string>()
            {
                    { "LondArea", "/Flak areas/LondonFlak.mis" },
                    { "Abbe", "/Flak areas/Abbevilleflak.mis" },
                    { "Arra", "/Flak areas/Arrasflak.mis" },
                    { "Ashf", "/Flak areas/Ashfordflak.mis" },
                    { "Bapu", "/Flak areas/Bapumeflak.mis" },// was missing
                    { "Bext", "/Flak areas/Bextonflak.mis" },
                    { "Boul", "/Flak areas/Boulogneflak.mis" },
                    { "Bult", "/Flak areas/Bultonflak.mis" },
                    { "CaeB", "/Flak areas/CaeBflak.mis" },		//not used but german caen defense			
                    { "Caen", "/Flak areas/Caenflak.mis" },
                    { "Cala", "/Flak areas/Calaisflak.mis" },
                    { "Cant", "/Flak areas/Cantflak2.mis" },//was missing
                    { "Cher", "/Flak areas/Cherbourgflak2.mis" },//romove the 2 to activate flak after testing				
                    { "Diep", "/Flak areas/Dieppeflak.mis" },
                    { "Ditt", "/Flak areas/Dittonflak.mis" },
                    { "Dove", "/Flak areas/Doverflak.mis" },
                    { "Dunk", "/Flak areas/Dunkirkflak.mis" },
                    { "Estr", "/Flak areas/Estreeflak.mis" },
                    { "Farn", "/Flak areas/Farnflak.mis" },
                    { "Guil", "/Flak areas/Guildfordflak.mis" },
                    { "Havr", "/Flak areas/LeHavreflak.mis" },
                    { "Trep", "/Flak areas/LeTreportflak.mis" },
                    { "Litt", "/Flak areas/LittleStoneflak.mis" },
                    { "Lond", "/Flak areas/LondonDocksflak.mis" },
                    { "Maid", "/Flak areas/Maidstoneflak.mis" },
                    { "Mans", "/Flak areas/Manstonflak.mis" },
                    { "MPGP", "/Flak areas/MPGPflak.mis" },// grand fort philippe flak
                    { "Poix", "/Flak areas/PoixNordflak.mis" },
                    { "Port", "/Flak areas/Portsmouthflak.mis" },
                    { "QueR", "/Flak areas/Quervilleflak.mis" },
                    { "RaHQ", "/Flak areas/RadarHQflak.mis" },
                    { "Read", "/Flak areas/Readingflak.mis" },
                    { "Redh", "/Flak areas/RedHillflak.mis" },
                    { "Shee", "/Flak areas/Sheernessflak.mis" },
                    { "Sout", "/Flak areas/Southhamptonflak.mis" },
                    { "Omar", "/Flak areas/Omarflak.mis" },
                    { "Ouen", "/Flak areas/Ouenflak.mis" },
                    { "Stel", "/Flak areas/Stellingflak.mis" },
                    { "Swin", "/Flak areas/Swindonflak.mis" },
                    { "Tunb", "/Flak areas/Tunbridgeflak.mis" },
                    { "Watt", "/Flak areas/Wattenflak.mis" },
                    { "Quee", "/Flak areas/Queeflak.mis" },//Queensborough Flak
					// Radar flak added for radar instalations and new targets 
					{ "AmbR", "/Flak areas/AmbRflak.mis" },//Radar AMBETEUSE
                    { "LeHR", "/Flak areas/LeHavreflak.mis" },//Le Havre Freya Radar
					{ "BerR", "/Flak areas/BerRflak.mis" },// Berck Freya Radar
                    { "BlgR", "/Flak areas/BlgRflak.mis" },//	Radar BOULOGNE
                    { "BroR", "/Flak areas/BroRflak.mis" },// Brookland Radar
					{ "CoqR", "/Flak areas/CoqRflak.mis" },//Coquelles Freya Radar
                    { "DeaR", "/Flak areas/DealRflak.mis" },// Deal Radar
                    { "DieR", "/Flak areas/DieRflak.mis" },//Radar Dieppee
					{ "DunR", "/Flak areas/DunRflak.mis" },//Dungeoness Radar
					{ "DuRN", "/Flak areas/DuRNflak.mis" },//Dunkirk North Radar
                    { "DovR", "/Flak areas/DovRflak.mis" },// Dover Radar
                    { "EasR", "/Flak areas/EasRflak.mis" },// Eastbourne Radar
                    { "HePR", "/Flak areas/HePRflak.mis" },//Hardalot Plauge...below boulogne
                    { "HQR", "/Flak areas/HQRflak.mis" },  // English Hq radar 
                    { "OypR", "/Flak areas/OypRflak.mis" },//Oye Plauge Freya Rada
                    { "SanR", "/Flak areas/SanRflak.mis" },// Sandwich Radar
                    { "SomR", "/Flak areas/SomR.mis" },    //Radar Somme River
                    { "TreR", "/Flak areas/TreRflak.mis" },//Radar Le Treport
                    { "VenR", "/Flak areas/VenRflak.mis" },// Ventnor Radar
					{ "VeuR", "/Flak areas/VeuRflak.mis" },// NW Freecamp Veuletts sur mer
                    { "WesR", "/Flak areas/WesRflak.mis" },// Westgate English Radar
					{ "PooR", "/Flak areas/PooRflak.mis" },//Poole Industrial Area
                    { "Pool", "/Flak areas/Poolflak.mis" },//Poole English Radar
                    { "Shor", "/Flak areas/Shorehamflak.mis" },//Poole English Radar
					{ "LitR", "/Flak areas/LitRflak.mis" },// Littlehampton Radar
					{ "Roue", "/Flak areas/Roueflak.mis" },// Rouen Flak batteries remove the 2 to activate flak batteries					
					{ "Larr", "/Flak areas/Larrowflak.mis" },
                    { "Lblu", "/Flak areas/Lblueflak.mis" },
                    { "Lgol", "/Flak areas/Lgoldflak.mis" },
                    { "Lgre", "/Flak areas/Lgreyflak.mis" },
                    { "Lhyd", "/Flak areas/Lhydrogenflak.mis" },
                    { "Lwhi", "/Flak areas/Lwhiteflak.mis" },
                    { "Pers", "/Flak areas/Persanflak.mis" },
                    { "Dun1", "/Flak areas/Dun1flak.mis" },
                    { "Dun2", "/Flak areas/Dun2flak.mis" },
                    { "Dun3", "/Flak areas/Dun3flak.mis" },
                    { "Peti", "/Flak areas/Petitflak.mis" },
                    { "None", "/Flak areas/Noneflak.mis" },
				
				
				
				
				/*	
				 	
				Oye Plage Freya Radar",    	
				Coquelles Freya Radar",    
                Dunkirk Freya Radar",      
                Herderlot-Plage Freya Radar
                Berck Freya Radar",        
                Radar Dieppee",            
                Radar Le Treport",         
                Radar Somme River",        
                Radar AMBETEUSE",          
                Radar BOULOGNE",           
                Radar Le Touquet",                      
                Veulettes-sur-Mer Radar",  
                Le Havre Freya Radar",     
                Ouistreham Freya Radar",   
                Bayeux Beach Freya Radar", 
                Beauguillot Beach Freya Rad
				Radar Tatihou",            
				Radar Querqueville", 
added Rouen Flak 
				*/	
					
					
					
					
					
					
					
					

            };

        /*
        //This creates a randomized list of Blue & Red objectives.  When asked for potential targets we can check which still have not yet been destroyed & list location, name, etc.
        public void SelectSuggestedObjectives()
        {

            List<string> keys = new List<string>(msn.MissionObjectivesList.Keys);
            Calcs.Shuffle(keys);

            foreach (var key in keys)
            {
                MissionObjective mo = msn.MissionObjectivesList[key];
                if (mo.AttackingArmy == 1 || mo.AttackingArmy == 2) msn.MissionObjectivesSuggested[(ArmiesE)mo.AttackingArmy].Add(key);

            }
        }*/
        //This creates a randomized list of Blue & Red objectives.  When asked for potential targets we can check which still have not yet been destroyed & list location, name, etc.
        //They are, however, weighted by PrimaryTargetWeight field so that we'll end up with a similar distribution of objectives to what we see in the Primary Objectives List
        public void SelectSuggestedObjectives(ArmiesE army)
        {

            List<string> keys = new List<string>(msn.MissionObjectivesList.Keys);
            Calcs.Shuffle(keys);
            int goal = msn.MissionObjectivesList.Count / 2;
            int count = 0;

            for (int x = 0; x < 10; x++) //unlikely but possible that we'd need to cycle through the list of targets multiple times to select enough targets to reach the points. Could happen though if PrimaryTargetWeights are set low, or only a few possible objectives available in the list. 
            {
                foreach (var key in keys)
                {
                    MissionObjective mo = msn.MissionObjectivesList[key];
                    if (mo.PrimaryTargetWeight > 0 && mo.IsEnabled && !mo.IsPrimaryTarget)
                    {
                        double r = msn.stb_random.NextDouble() * 200; //was 100 but now primarytargetweight goes up to 200 instead of 100
                        //Console.WriteLine("Select Primary " + mo.PrimaryTargetWeight + " " + r.ToString("N4") + " " + mo.ID);
                        if (mo.PrimaryTargetWeight < r) continue; //implement weight; if weight is less than the random number then this one is skipped; so 100% is never skipped, 50% skipped half the time, 0% skipped always
                        if (mo.AttackingArmy != (int)army) continue;
                        if (mo.AttackingArmy == 1 || mo.AttackingArmy == 2)
                        {
                            if (msn.MissionObjectivesSuggested[(ArmiesE)mo.AttackingArmy].Contains(key)) continue;
                            msn.MissionObjectivesSuggested[(ArmiesE)mo.AttackingArmy].Add(key);
                            count++;
                            if (count > goal) break;
                        }

                    }
                }
            }
        }
    }
    //When one side completes their full objective/reaches their score then they clear the decks
    //and everything destroyed is restored again
    public void MO_MissionObjectiveWinnersReward(ArmiesE army)
    {
        foreach (string ID in MissionObjectivesList.Keys)
        {
            MissionObjective mo = MissionObjectivesList[ID];

            if (mo.OwnerArmy == (int)army)
            {
                mo.Destroyed = false;
                mo.DestroyedPercent = 0;
                mo.TimeToUndestroy_UTC = null;
                mo.LastHitTime_UTC = null;
                //We don't reset mo.ObjectiveAchievedForPoints here because the other side is still working on their objectives list.  So it is restored for use by the winning side but still counts towards objectives/points for the other side.  This is getting messy . . . 
                /*  hmm, this was a mistake but we **could** really do it.  When one side turns the map, the other side loses all of its recon photos.  Hmm.
                mo.Scouted = false;
                mo.PlayersWhoScoutedNames = new Dictionary<string, int>();
                */
            }

            if (mo.AttackingArmy == (int)army)
            {
                mo.ObjectiveAchievedForPoints = false; //this resets the objectives list for scoring purposes.  If items are still actually destroyed they can stay that way until they're repaired.  But for scoring purposes, the winning side is now starting over as though nothing were destroyed.
                mo.IsPrimaryTarget = false;
                mo.hasGeneralStaff = false; //Might need to not mess with this, but it goes along with removing all primary objectives
                mo.Scouted = false; //And we have to reset scouting because now we have a full NEW set of objectives to get
                mo.PlayersWhoScoutedNames = new Dictionary<string, int>();
            }
        }

        MissionObjectiveScore[(ArmiesE)army] = 0;
        MissionObjectivesCompletedString[(ArmiesE)army] = "";

    }

    //We load the existing objectives list from the disk.  But . . . . 
    //if an objective is still destroyed from a previous mission then have to do certain things, such as set up some smokes if it was hit recently, to make it look recently
    //hit, and also (depending on he type of objective) keep its functionality turned off as long as is is still destroyed/unrepaired.
    //This handles every kind of objective except airports, which are handled separately
    //
    public void MO_MissionObjectiveOnStartCheck(Mission msn, maddox.game.IGamePlay gp)
    {
        foreach (string ID in MissionObjectivesList.Keys)
        {
            MissionObjective mo = MissionObjectivesList[ID];

            //DON'T RESET isPrimaryTarget here because we're doing this after the primary objectives are chosen & entered into this list.
            //We will do that needed reset, but earlier in the process
            //mo.IsPrimaryTarget = false; //we reset this every time we load from disk, because later we'll read the primary objectives file & set this = true if it is a primary objective based on that.

            if (mo.Destroyed)
            {
                //Sometime is has been destroyed and now it is time to undestroy it
                if (mo.TimeToUndestroy_UTC.HasValue && DateTime.Compare(mo.TimeToUndestroy_UTC.Value, DateTime.UtcNow) < 0)
                {
                    mo.Destroyed = false;
                    mo.DestroyedPercent = 0;
                    mo.TimeToUndestroy_UTC = null;
                } else //so if it has been destroyed before.  So we will mark it with smoke to show it is damaged.
                       //We also mark it with a small smoke so you can tell it has been hit recently, but not too recently.
                       //However, we DO leave it marked as "destroyed", until it's time for undestroying comes along OR until that
                       //team turns the map, which undestroys all of the objectives they own.

                //Though this objective is smoked up, the trigger is reset when the mission restarts, so the opposing team can hit it again and destroy it even more and rack
                //up a few more points, though not as much as if it were pristine to start with.


                {


                    //string firetype = "BuildingFireSmall"; //small-ish
                    //if (mass_kg > 200) firetype = "BigSitySmoke_1"; //500lb bomb or larger  //REALLY huge
                    //if (mass_kg > 200) firetype = "Smoke1"; //500lb bomb or larger //larger
                    string smoke = "BuildingFireSmall"; //was "BuildingFireBig" but that seems too big
                    double hoursSinceHit = 48;
                    if (mo.LastHitTime_UTC.HasValue) hoursSinceHit = DateTime.UtcNow.Subtract(mo.LastHitTime_UTC.Value).TotalHours;
                    if (mo.LastHitTime_UTC != null && (hoursSinceHit < 12)) smoke = "BuildingFireBig";
                    if (mo.LastHitTime_UTC != null && (hoursSinceHit < 2)) smoke = "Smoke2";


                    Calcs.loadSmokeOrFire(GamePlay, this, mo.Pos.x, mo.Pos.y, 0, smoke, duration_s: 6 * 3600);
                    if (mo.Points > 3) Calcs.loadSmokeOrFire(gp, msn, mo.Pos.x + random.Next(50) - 25, mo.Pos.y + random.Next(50) - 25, 0, smoke, duration_s: 6 * 3600); //bigger, put another


                    //Actually disabling the trigger was a mistake.  It is destroyed "for points purposes" which is reflected in mo.ObjectiveAchievedForPoints but can still be
                    //hit again as a 'normal objective' for fewer points, just not again as a primary objective
                    /*if (mo.MOTriggerType == MO_TriggerType.Trigger && GamePlay.gpGetTrigger(ID) != null)
                    {
                        Console.WriteLine("MO_DestroyObjective: Disabling trigger " + ID);
                        GamePlay.gpGetTrigger(ID).Enable = false;
                    }
                    */

                    //Turn off any radars if they are still disabled
                    if (mo.MOObjectiveType == MO_ObjectiveType.Radar)
                    {
                        if (mo.OwnerArmy == 1) DestroyedRadar[(ArmiesE.Red)].Add(mo);
                        if (mo.OwnerArmy == 2) DestroyedRadar[(ArmiesE.Blue)].Add(mo);
                    }

                    //Here is the spot to take any other action needed to show the affects if a target was destroyed and is still destroyed this session.
                    //So if an aircraft plant, turn off or down the aircraft manufacturing levels, if a bread making plant reduce the supply of bread, etc.
                    //Airports are handled specially, but everything else can be handled here.


                }
            }
        }
    }

    //AddNewOnly means only add new aps not already in the file read from disk.  addNewOnly=false means, forget about what's on the disk load, just read them all in.  Usually you only want to do this if the disk read failed.
    public void MO_MissionObjectiveAirfieldsSetup(Mission msn, maddox.game.IGamePlay gp, bool addNewOnly = false)
    {
        //public Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>> AirfieldTargets = new Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>>();
        //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius, Point3d airfield center (position)
        //TRIGGER initiator (for all types except RADAR)
        //public MissionObjective(Mission m, MO_ObjectiveType mot, double pts, double ptp, AiAirport airport, Tuple<bool, string, double, double, DateTime, double, Point3d> tup)
        //            string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, int ptp, string comment)


        //int NumNearbyTargets = MissionObjectivesNear();

        int count = AirfieldTargets.Count;
        double weight = (double)300 / (double)count; //500/count gives you about 1 airfield target about 1 of every 3 sets of targets
        int num_added = 0;
        int num_updated = 0;
        var allKeys = new List<AiAirport>(AirfieldTargets.Keys);
        if (AirfieldTargets != null) foreach (AiAirport ap in allKeys)
            {
                string af_name = AirfieldTargets[ap].Item2 + "_airfield";
                if (!addNewOnly || !MissionObjectivesList.ContainsKey(af_name))
                {

                    int NumNearbyTargets = MO_MissionObjectivesNear(AirfieldTargets[ap].Item7, 20000);  //was 15,000 - 2020-01
                    double IndWeight = weight;
                    if (NumNearbyTargets > 0) IndWeight = weight * 2;
                    else if (NumNearbyTargets > 3) IndWeight = weight * 16;  //was 4 -> 16 increasing airfields near major objects as targets with cover bomber system.  2020-01
                    else if (NumNearbyTargets > 5) IndWeight = weight * 48;  //was 12 -> 48 increasing 4X with cover bomber system.  2020-01
                                                                             //Console.WriteLine("AP: " + AirfieldTargets[ap].Item2 + "_airfield");
                    Point3d Pos = AirfieldTargets[ap].Item7;
                    int army = GamePlay.gpFrontArmy(Pos.x, Pos.y);
                    if (Pos.x > 210000 && Pos.y > 180000 && Pos.x < 321000 && Pos.y < 270000) IndWeight = 200; //vastly increase # of airports as mission objectives, in the 'main' campaign area. 2020-01
                    MissionObjectivesList.Add(af_name, new MissionObjective(msn, 3, IndWeight, ap, army, AirfieldTargets[ap]));
                    num_added++;
                } else if (MissionObjectivesList.ContainsKey(af_name))
                {
                    //AirfieldTargets = new Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>>();
                    //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius, Point3d airfield center (position)
                    Tuple<bool, string, double, double, DateTime, double, Point3d> oldAp = AirfieldTargets[ap];
                    MissionObjective mo = MissionObjectivesList[af_name];
                    double damagePoints = 0; //assumed because it is knocked out/100%
                    if (mo.DestroyedPercent > 0) damagePoints = mo.DestroyedPercent * oldAp.Item3;  //so if the airport was partially knocked out before, it still remains that way
                                                                                                    //between this & the last-hit time the airport routine can calculate airport repair time accurately.
                    bool apDestroyed = false;
                    DateTime lastHitTime = DateTime.UtcNow; //time of last damage hit //unfort we don't really have this, it happened in a previous mission; we set it to right now
                    if (mo.LastHitTime_UTC.HasValue) lastHitTime = mo.LastHitTime_UTC.Value;
                    if (mo.Destroyed)
                    {
                        //it was destroyed, but now it is time to undestroy the airport
                        if (!mo.TimeToUndestroy_UTC.HasValue || DateTime.Compare(mo.TimeToUndestroy_UTC.Value, DateTime.UtcNow) < 0) //airport should always have an undestroy time.  IF not, we assume the undestroy time is right now.
                        {
                            mo.Destroyed = false;
                            mo.DestroyedPercent = 0;
                            mo.TimeToUndestroy_UTC = null;
                            damagePoints = 0;
                        } else //it is still destroyed, so we need to mark it destroyed & actually destroy it
                        {
                            apDestroyed = true;
                            /*
                              Unfortunately we must reverse engineer the damage point total from time remaining to repair
                              here is the formula:
                                  timetofix = 24 * 60 * 60; //24 hours to repair . . . if they achieve 100% knockout.  That is a little bonus beyond what the actual formula says, due ot total knockout
                                  timetofix += (PointsTaken - PointsToKnockOut) * 20 * 60; //Plus they achieve any additional knockout/repair time due to additional bombing beyond 100%, because those will have to be 
                              */
                            double perc = 1.0;// if it's knocked out, we start there.  100%
                            double hrs = mo.TimeToUndestroy_UTC.Value.Subtract(DateTime.UtcNow).TotalHours;
                            if (hrs > 24) perc += (hrs - 24) / 24; //If it's knocked out more than 24 hours it mus t have more damage poitns than just pointstoknockout
                                                                   //figuring this allows ppl to keep piling on damage points if they so wish
                            damagePoints = oldAp.Item3 * perc;

                            AirfieldDisable(ap);

                        }
                    }

                    AirfieldTargets[ap] = new Tuple<bool, string, double, double, DateTime, double, Point3d>(
                        apDestroyed, //bool disabled
                        oldAp.Item2, //name
                        oldAp.Item3, //pointstoknockout
                        damagePoints, //damage point total
                        lastHitTime,
                        oldAp.Item6, // airfield radius
                        oldAp.Item7  //airfield center
                        );

                    num_updated++;
                }

            }
        Console.WriteLine("Mission Objectives: Added " + num_added.ToString() + " airports to Mission Objectives, updated " + num_updated.ToString() + " weight " + weight.ToString("N5"));

    }

    public int MO_MissionObjectivesNear(Point3d p, double dist_m)
    {
        int total = 0;
        List<string> keys = new List<string>(MissionObjectivesList.Keys);
        foreach (var key in keys)
        {
            MissionObjective mo = MissionObjectivesList[key];
            double d_m = Calcs.CalculatePointDistance(mo.Pos, p);
            if (d_m <= dist_m) total++;
        }
        return total;
    }

    //This creates a randomized list of Blue or Red Primary Objectives totalling (at least) the required point total
    //And sets the IsPrimaryTarget flag for each in that army, de-selecting the IsPrimaryTarget flag for all others
    //Only chooses from those in the PrimaryTargetWeight     
    //Will do either army=1 or army=2 or BOTH ARMIES if army=0   
    public void MO_SelectPrimaryObjectives(int army = 0, double totalPoints = 0, bool fresh = false)
    {

        List<string> keys = new List<string>(MissionObjectivesList.Keys);
        Calcs.Shuffle(keys);

        List<int> arms = new List<int>();
        if (army == 0) { arms.Add(1); arms.Add(2); }
        else if (army == 1 || army == 2) arms.Add(army);

        Console.WriteLine("Selecting new Mission Objectives for " + ArmiesL[army]);

        foreach (var a in arms)
        {
            string objectivesList = "";
            //first, reset all the targets (in this army) to be not primaries.  
            //But ONLY if we are starting fresh; ie, choosing ALL the primaries now
            //If some have already been chosen, we have to respect the previously chosen IsPrimaryTarget = true;          
            if (fresh) foreach (var key in keys)
                {
                    if (MissionObjectivesList[key].AttackingArmy == a) MissionObjectivesList[key].IsPrimaryTarget = false;
                }

            int counter = 1;

            for (int x = 0; x < 10; x++) //unlikely but possible that we'd need to cycle through the list of targets multiple times to select enough targets to reach the points. Could happen though if PrimaryTargetWeights are set low, or only a few possible objectives available in the list. 
            {
                foreach (var key in keys)
                {
                    MissionObjective mo = MissionObjectivesList[key];
                    if (mo.AttackingArmy == a && mo.PrimaryTargetWeight > 0 && mo.IsEnabled && !mo.IsPrimaryTarget && !mo.Destroyed && !mo.ObjectiveAchievedForPoints)
                    {
                        double r = stb_random.NextDouble() * 200; //was 100.  This will cut chance of everything being chosen, in half.  Except airports and a few other things, which we increased by a lot.  2020-01
                        //Console.WriteLine("Select Primary " + mo.PrimaryTargetWeight + " " + r.ToString("N4") + " " + mo.ID);
                        if (mo.PrimaryTargetWeight < r) continue; //implement weight; if weight is less than the random number then this one is skipped; so 200% is never skipped, 100% skipped half the time, 50% 3/4 of the time, 0% skipped always
                        if (totalPoints < MO_PointsRequired[(ArmiesE)a])
                        {
                            mo.IsPrimaryTarget = true;
                            totalPoints += mo.Points;
                            objectivesList += " - " + mo.Sector + " " + mo.Name;
                            Console.WriteLine("Adding new Mission Objective: " + mo.Sector + " " + mo.Name + " " + mo.Points.ToString("F0") + " " + totalPoints.ToString("F0"));
                            //if (counter % 3 == 0) MissionObjectivesString[(ArmiesE)a] += Environment.NewLine;
                            counter++;
                        }
                        else
                        {
                            mo.IsPrimaryTarget = false;
                        }

                    }

                }
            }
            //Console.WriteLine("Selecting/adding new Mission Objectives for " + ArmiesL[army] + ":");
            //Console.WriteLine(objectivesList);
        }
        MO_SelectPrimaryObjectiveForGeneralStaffLocation(army);

    }

    public void MO_SelectPrimaryObjectiveForGeneralStaffLocation(int army = 0, double totalPoints = 0)
    {

        List<string> keys = new List<string>(MissionObjectivesList.Keys);
        Calcs.Shuffle(keys);

        List<int> arms = new List<int>();
        if (army == 0) { arms.Add(1); arms.Add(2); }
        else if (army == 1 || army == 2) arms.Add(army);

        Console.WriteLine("Selecting new Mission General Staff Location at a Primary Objective for " + ArmiesL[army]);

        foreach (var a in arms)
        {
            //first, reset all the targets (in this army) to be not the General Staff Location.            
            foreach (var key in keys)
            {
                if (MissionObjectivesList[key].AttackingArmy == a) MissionObjectivesList[key].hasGeneralStaff = false;
            }

            int counter = 1;

            for (int x = 0; x < 10; x++) //unlikely but possible that we'd need to cycle through the list of targets multiple times to select enough targets to reach the points. Could happen though if PrimaryTargetWeights are set low, or only a few possible objectives available in the list. 
            {
                bool done = false;
                foreach (var key in keys)
                {
                    MissionObjective mo = MissionObjectivesList[key];
                    if (mo.AttackingArmy == a && mo.IsEnabled && mo.IsPrimaryTarget && !mo.Destroyed && !mo.ObjectiveAchievedForPoints)
                    {
                        mo.hasGeneralStaff = true;
                        done = true;
                        break;
                    }
                }
                if (done) break;
            }
        }
    }

    public class GeneralStaffLocationObject
    {
        public Point3d pos { get; set; }
        public string objectiveKey { get; set; }
        public string objectiveName { get; set; }
        public string staffGroupName { get; set; }
        public string sector { get; set; }
        public string sectorKeypad { get; set; }
        public string sectorDoublekeypad { get; set; }
        public bool discovered { get; set; }

        public GeneralStaffLocationObject(Point3d p, string o, string on, string sgn, string s, string sk, string sdk, bool d)
        {
            pos = p;
            objectiveKey = o;
            objectiveName = on;
            staffGroupName = sgn;
            sector = s;
            sectorKeypad = sk;
            sectorDoublekeypad = sdk;
            discovered = d;
        }
    }

    public Dictionary<ArmiesE, GeneralStaffLocationObject> GeneralStaffLocations = new Dictionary<ArmiesE, GeneralStaffLocationObject>(); //location, objective key, objective name, name of general staff group, sector, sector keypad, sector double keypad, whether discovered yet
    //public Dictionary<ArmiesE, Tuple<Point3d, string, string, string, string, string, string, bool>> GeneralStaffLocations; //location, objective key, objective name, name of general staff group, sector, sector keypad, sector double keypad, whether discovered yet
    public static Dictionary<ArmiesE, string[]> GeneralStaffNames = new Dictionary<ArmiesE, string[]>() //army here is the TARGETING army, so General etc is from the opposite army
    {
        {ArmiesE.Red, new string [] { "Generalfeldmarschall Kesselring and his staff", "Generalfeldmarschall Sperrle and his staff", "Generalfeldmarschall Kesselring and a few high Luftwaffe officers", "General Jeschonnek and his personal aides", "Luftwaffe General Kreipe and his aides" } },
        {ArmiesE.Blue, new string [] {"Air Chief Marshal Dowding and a small staff", "Air Vice-Marshal Park and his aides, checking on fighter preparations", "Air Chief Marshall Leigh-Mallory and and high-ranking RAF officers", "Air Vice-Marshal Brand and his staff", "Air Chief Marshall Breadner and his aides" }}
    };



    public bool MO_CheckForGeneralStaffReconPhoto(Player player, AiAircraft aircraft, int army, Point3d pos)
    {

        double Z_AltitudeAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
        var gsl = GeneralStaffLocations[(ArmiesE)army];

        double dist_m = Calcs.CalculatePointDistance(pos, gsl.pos);
        MissionObjective mo = MissionObjectivesList[gsl.objectiveKey];

        string af = "RAF";
        if (army == 2) af = "Luftwaffe";

        if (Z_AltitudeAGL_m < 100 && dist_m < 100)
        {

            if (gsl.discovered)
            {
                string msg = "Sorry, " + gsl.staffGroupName + " were discovered a while ago. But you are in the right area!";
                twcLogServer(new Player[] { player }, msg, new object[] { });
                return true;
            }
            gsl.discovered = true;

            twcLogServer(null, af + gsl.staffGroupName + " found in sector " + gsl.sector + " by " + player.Name(), new object[] { });
            GamePlay.gpHUDLogCenter(null, af + " Commander Found! Good Job, " + ArmiesL[army] + "!");
            return true;
        }
        else if (Calcs.CalculatePointDistance(pos, mo.Pos) < 15000 && Z_AltitudeAGL_m < 300)
        {
            string msg = "You tried to take a recon photo of the " + af + " commanders who were reported in this area.  But there was no trace of them in your photo!";
            twcLogServer(new Player[] { player }, msg, new object[] { });
            msg = "You must be below 100m AGL and within 200m of the target to take the required photo." + (Calcs.CalculatePointDistance(pos, mo.Pos)).ToString() + " " + Z_AltitudeAGL_m.ToString();
            twcLogServer(new Player[] { player }, msg, new object[] { });
            return true;
        }

        string msg2 = "No: " + (Calcs.CalculatePointDistance(pos, mo.Pos)).ToString() + " " + Z_AltitudeAGL_m.ToString();
        twcLogServer(new Player[] { player }, msg2, new object[] { });
        return false;

    }


    public void MO_HandleGeneralStaffPlacement()
    {
        List<string> keys = new List<string>(MissionObjectivesList.Keys);


        List<int> arms = new List<int>();
        arms.Add(1); arms.Add(2);


        Console.WriteLine("Handling new Mission General Staff Location at a Primary Objective for both armies");

        foreach (var a in arms)
        {
            bool done = false;
            string staffKey = "";
            for (int x = 0; x < 10; x++)
            {
                //first, reset all the targets (in this army) to be not the General Staff Location.            
                foreach (var key in keys)
                {
                    MissionObjective mo = MissionObjectivesList[key];
                    if (mo.AttackingArmy == a && mo.hasGeneralStaff)
                    {
                        staffKey = key;
                        done = true;
                    }
                }
                if (done) break;
                MO_SelectPrimaryObjectiveForGeneralStaffLocation(a);
            }


            MissionObjective mo1 = MissionObjectivesList[staffKey];

            Point3d newPos = mo1.Pos;
            double adder_m = 200;


            //try to find a place that is not water, not near an airport, outwards at least mo.radius + adder_m from the center of the objective
            for (int i = 0; i < 25; i++)
            {
                double angle = random.NextDouble() * 2.0 * Math.PI;
                double radius = random.Next(250) + adder_m + mo1.radius + i * 300.0;  //try ever-greater distances if it's not working well

                newPos.x = mo1.Pos.x + Math.Cos(angle) * radius;
                newPos.y = mo1.Pos.y + Math.Sin(angle) * radius;

                maddox.game.LandTypes landType = GamePlay.gpLandType(newPos.x, newPos.y);

                double dist = 5000;
                try
                {
                    AiAirport ap = Calcs.nearestAirport(GamePlay, newPos);
                    dist = Calcs.CalculatePointDistance(ap.Pos(), newPos);
                } catch (Exception ex) { Console.WriteLine("ERROR GENERAL! " + ex.ToString()); }

                if (landType != maddox.game.LandTypes.WATER && dist > 4000) break;
            }

            var items = new List<string> { "Stationary.Humans.150_cm_SearchLight_Gunner_1", "Stationary.Humans.Soldier_Flak38_Gunner", "Stationary.Humans.RRH_Gunner_1",
                "Stationary.Humans.Em_4m_R(H)34_Gunner_3",
                 "Stationary.Environment.Table_empty_UK-GER_1",
                "Stationary.Environment.Table_empty_UK-GER_1",
                "Stationary.Environment.Table_w_chess_UK-GER_1",
                "Stationary.Environment.Table_w_dinner_UK-GER_1",
                "Stationary.Environment.Table_empty_UK-GER_1",
                "Stationary.Environment.Table_w_chess_UK-GER_1",
                "Stationary.Environment.Table_w_dinner_UK-GER_1",
                "Stationary.Environment.Table_empty_UK-GER_1",
                "Stationary.Environment.Table_w_chess_UK-GER_1",
                "Stationary.Environment.Table_w_dinner_UK-GER_1",};

            var cars = new List<string> { "Stationary.Opel_Blitz_cargo", "Stationary.Opel_Blitz_cargo", "Stationary.Opel_Blitz_cargo", "Stationary.Opel_Blitz_cargo", "Stationary.Opel_Blitz_cargo",
                "Stationary.Opel_Blitz_cargo", "Stationary.Opel_Blitz_cargo", "Stationary.Opel_Blitz_cargo",
                "Stationary.BMW_R71_w_MG_34",
                "Stationary.BMW_R71_w_MG_34",
                "Stationary.Scammell_Pioneer_R100",
                "Stationary.Scammell_Pioneer_R100",
                "Stationary.Scammell_Pioneer_R100",
                "Stationary.Scammell_Pioneer_R100",
                "Stationary.Horch_830_B1",
                "Stationary.Horch_830_B1",
                "Stationary.Horch_830_B1" };

            string enemy = "de";
            if (a == 2) enemy = "gb";

            var trucks = new List<string> { "Stationary.Morris_CS8", "Stationary.Morris_CS8_tent", "Stationary.Bedford_MW_tent", "Stationary.Albion_AM463", "Stationary.Morris_CS8", "Stationary.Morris_CS8_tent", "Stationary.Bedford_MW_tent", "Stationary.Albion_AM463", "Stationary.Morris_CS8", "Stationary.Morris_CS8_tent", "Stationary.Bedford_MW_tent", "Stationary.Albion_AM463" };

            /*Stationary.Albion_AM463
                Stationary.Bedford_MW_tent
                Stationary.Morris_CS8
                */
            //Now actually PLACE the general and various items around there.
            ISectionFile f = GamePlay.gpCreateSectionFile();

            Calcs.Shuffle(items);
            for (int i = 0; i < 9; i++) {

                string side = "nn";
                if (random.Next(3) == 0) side = enemy; //setting them as enemy makes them show up as black dots.  These are small so should be pretty dim black dots.

                //Timeout(10 + 2 * i, () =>
                //{ 
                f = Calcs.makeStatic(f, GamePlay, this, newPos.x + random.Next(10), newPos.y + random.Next(10), newPos.z, type: items[i], heading: random.Next(360), side: side);
                //});
            }



            double hdg = random.Next(360);
            Calcs.Shuffle(cars);
            for (int i = 0; i < 4; i++)
            {
                double angle1 = i * 2.0 / 4.0 * Math.PI + random.NextDouble();
                double radius2 = random.Next(8) + 10;

                double nex = newPos.x + Math.Cos(angle1) * radius2;
                double ney = newPos.y + Math.Sin(angle1) * radius2;

                string side = "nn";
                if (random.Next(4) == 0) side = enemy;

                //Timeout(30 + 2 * i, () =>
                //{
                f = Calcs.makeStatic(f, GamePlay, this, nex, ney, newPos.z, type: cars[i], heading: hdg + random.Next(15), side: side);
                //});
            }

            Calcs.Shuffle(trucks);
            for (int i = 0; i < 7; i++)
            {
                double angle1 = i * 2.0 / 7.0 * Math.PI + random.NextDouble();
                double radius2 = random.Next(5) + 12;

                double nex = newPos.x + Math.Cos(angle1) * radius2;
                double ney = newPos.y + Math.Sin(angle1) * radius2;

                string side = "nn";
                if (random.Next(5) == 0) side = enemy;

                //Timeout(50 + 2 * i, () =>
                //{
                f = Calcs.makeStatic(f, GamePlay, this, nex, ney, newPos.z, type: trucks[i], heading: hdg + random.Next(15), side: side);
                //});
            }


            GamePlay.gpPostMissionLoad(f);


            Console.WriteLine("The general staff near a {0} objective is at {1} - ({2:F0},{3:F0})", new object[] { ArmiesL[a], staffKey, newPos.x, newPos.y });
            Console.WriteLine("msn {0} objective)", new object[] { this as Mission != null });
            Console.WriteLine("msn {0} {1} {2} {3} {4} {5} {6} {7} objective)", new object[] {newPos, staffKey, mo1.Name,    GeneralStaffNames[(ArmiesE)a][1],
            Calcs.correctedSectorName(this as Mission, newPos),
            Calcs.correctedSectorNameKeypad(this as Mission, newPos),
            Calcs.correctedSectorNameDoubleKeypad(this as Mission, newPos),
            false
            });
            GeneralStaffLocationObject gslo = null;
            try
            {
                gslo = new GeneralStaffLocationObject(newPos, staffKey, mo1.Name,
                    //Calcs.randSTR(GeneralStaffNames[(ArmiesE)a]), //name of leader/group
                    GeneralStaffNames[(ArmiesE)a][1], //name of leader/group
                    Calcs.correctedSectorName(this as Mission, newPos),
                    Calcs.correctedSectorNameKeypad(this as Mission, newPos),
                    Calcs.correctedSectorNameDoubleKeypad(this as Mission, newPos),
                    false
                    );
            } catch (Exception ex) { Console.WriteLine("gslo error! " + ex.ToString()); }
            //GeneralStaffLocations[(ArmiesE)a] =
            GeneralStaffLocations[(ArmiesE)a] = gslo;
            Console.WriteLine("The general staff near a {0} objective is at {1} - ({2:F0},{3:F0})", new object[] { ArmiesL[a], staffKey, newPos.x, newPos.y });

        }
    }
    public void MO_AutoFlakPlacement(MissionObjective mo) {
        {
            List<string> keys = new List<string>(MissionObjectivesList.Keys);



            Console.WriteLine("Handling autoFlakPlacement for {0} {1} {2}", mo.ID, mo.Pos.x, mo.Pos.y);
            Point3d newPos = mo.Pos;

            int nfb = mo.NumFlakBatteries;
            int nib = mo.NumInFlakBattery;

            //too much flak seems to bring the server to it's knees, so if not a primary just 2x2 flak, otherwise what is requested
            if (!mo.IsEnabled || !mo.IsPrimaryTarget)
            {
                nfb = 2;
                nib = 2;
            }

            ISectionFile f = GamePlay.gpCreateSectionFile();

            for (int j = 0; j < nfb; j++) {

                //try to find a place that is not water,  outwards at least mo.radius + adder_m from the center of the objective
                for (int i = 0; i < 2000; i++)
                {
                    double angle = random.NextDouble() * 2.0 * Math.PI;
                    double radius = random.Next(10) + mo.radius + i * 5.0;  //try ever-greater distances if it's not working well

                    newPos.x = mo.Pos.x + Math.Cos(angle) * radius;
                    newPos.y = mo.Pos.y + Math.Sin(angle) * radius;

                    maddox.game.LandTypes landType = GamePlay.gpLandType(newPos.x, newPos.y);

                    double dist = 1500;
                    double apRadius = 1000;
                    try
                    {
                        AiAirport ap = Calcs.nearestAirport(GamePlay, newPos);
                        dist = Calcs.CalculatePointDistance(ap.Pos(), newPos);
                        apRadius = ap.FieldR();
                    }
                    catch (Exception ex) { Console.WriteLine("ERROR FLAKPLACE! " + ex.ToString()); }

                    if (landType != maddox.game.LandTypes.WATER && dist > 1499 && dist > apRadius) break;
                }

                string enemy = "de";
                if (mo.AttackingArmy == 2) enemy = "gb";

                var flak = new List<string> { "Artillery.37mm_PaK_35_36", /*"Artillery.Flak37",*/ "Artillery.Bofors_StandAlone", "Artillery.3_7_inch_QF_Mk_I", "Artillery.Flak30_Shield", };

                //Now actually PLACE the flak.
                



                double hdg = random.Next(360);
                Calcs.Shuffle(flak);
                for (int i = 0; i < nib; i++)
                {
                    double angle1 = i * 2.0 / mo.NumInFlakBattery * Math.PI + random.NextDouble() / mo.NumInFlakBattery;
                    double radius2 = random.Next(2) + 10 + mo.NumInFlakBattery * 2;

                    double nex = newPos.x + Math.Cos(angle1) * radius2;
                    double ney = newPos.y + Math.Sin(angle1) * radius2;

                    string side = enemy;

                    //Timeout(30 + 2 * i, () =>
                    //{
                    f = Calcs.makeStatic(f, GamePlay, this, nex, ney, newPos.z, type: flak[0], heading: hdg + random.Next(15), side: side);
                    //});
                }                
                //Console.WriteLine("The flak is at {0} {1})", new object[] { newPos.x, newPos.y });
            }
            //wait to load, saves a lot of scrolling @ mission start.
            Timeout(45, () => { GamePlay.gpPostMissionLoad(f); });
        }
    }

    public void MO_TestObjectiveWithFlak(MissionObjective mo, int NumFlakBatteries, int NumInFlakBattery)
    {
        {
            List<string> keys = new List<string>(MissionObjectivesList.Keys);



            Console.WriteLine("Handling TESTFLAK for {0} {1} {2} {3} {4}", mo.ID, mo.Pos.x, mo.Pos.y, NumFlakBatteries, NumInFlakBattery);
            Point3d newPos = mo.Pos;

            for (int j = 0; j < NumFlakBatteries; j++)
            {

                double rd = mo.radius;

                if (rd < 50) rd = 50;
                if (rd < 5000) rd = 5000;

                newPos.x = mo.Pos.x + random.Next(Convert.ToInt32(mo.radius));
                newPos.y = mo.Pos.y + random.Next(Convert.ToInt32(mo.radius));


                //This is place ENEMY flake in the middle of a FRIENDLY objective, so that the enemyflak will destroy the objective
                string enemy = "de";
                if (mo.AttackingArmy == 1) enemy = "gb";

                var flak = new List<string> { "Artillery.37mm_PaK_35_36", /*"Artillery.Flak37",*/ "Artillery.Bofors_StandAlone", "Artillery.3_7_inch_QF_Mk_I", "Artillery.Flak30_Shield", };

                //Now actually PLACE the flak.
                ISectionFile f = GamePlay.gpCreateSectionFile();



                double hdg = random.Next(360);
                Calcs.Shuffle(flak);
                for (int i = 0; i < NumInFlakBattery; i++)
                {
                    double angle1 = i * 2.0 / NumInFlakBattery * Math.PI + random.NextDouble() / NumInFlakBattery;
                    double radius2 = random.Next(2) + 10 + NumInFlakBattery * 2;

                    double nex = newPos.x + Math.Cos(angle1) * radius2;
                    double ney = newPos.y + Math.Sin(angle1) * radius2;

                    //Console.WriteLine("The artillery is at {0} {1})", new object[] { nex, ney });

                    string side = enemy;

                    //Timeout(30 + 2 * i, () =>
                    //{
                    f = Calcs.makeStatic(f, GamePlay, this, nex, ney, newPos.z, type: flak[0], heading: hdg + random.Next(15), side: side);
                    //});
                }


                GamePlay.gpPostMissionLoad(f);


                //Console.WriteLine("The TESTFLAK is at {0} {1})", new object[] { newPos.x, newPos.y });



            }
        }
    }

    static ManualResetEvent resetEvent = new ManualResetEvent(false);

    public bool MO_WriteMissionObject(object mo, string name, bool wait = false)
    {
        Console.WriteLine("Writing " + name + " to file");

        string ext = ".xml";
        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_SESSIONSTATE_" + name + ext;
        string suffix = "_SESSIONSTATE_" + name;
        string fileName_base = CAMPAIGN_ID;
        string backupDir = CAMPAIGN_ID + @" campaign backups\"; //will be added at the end of STATSCS_FULL_PATH

        //WriteAllTextAsyncWithBackups(string fileDir_base, string fileName_base, string suffix, string ext, string backupDir, bool wait = false, ManualResetEvent resetEvent = null)

        try
        {
            /* 
             * //System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(mo.GetType());
            BinaryFormatter writer = new BinaryFormatter();
            using (FileStream fs = File.Create(filepath))
            {
                writer.Serialize(fs, mo);
            }
            */

            //List<Type> knownTypes = new List<Type> { typeof(List<string>), typeof(Tuple<int, int, Player>) };
            //var serializer = new DataContractSerializer(mo.GetType(), knownTypes);
            var serializer = new DataContractSerializer(mo.GetType());
            string xmlString;
            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented; // indent the Xml so it's human readable
                    serializer.WriteObject(writer, mo);
                    writer.Flush();
                    xmlString = sw.ToString();
                }
            }
            //File.WriteAllText(filepath, xmlString);

            int count = 0;

            //if wait is set we block the thread & wait until it's complete
            //we only do this at final program exit, just to make sure file write actually complete/not corrupted
            if (wait)
            {

                /*Task<bool> task = Calcs.WriteAllTextAsync(filepath, xmlString);
                bool res = await task; */
                //Calcs.WriteAllTextAsync(filepath_new, xmlString, wait: true, resetEvent: resetEvent);
                Calcs.WriteAllTextAsyncWithBackups(xmlString, STATSCS_FULL_PATH, fileName_base, suffix, ext, backupDir, wait: true, resetEvent: resetEvent);
                Console.WriteLine("MO_WriteMissionObject: waiting . . . to write " + name);
                resetEvent.WaitOne(); // Blocks the thread until until "set"
                Console.WriteLine("MO_WriteMissionObject: . . . . released.");

            } else Calcs.WriteAllTextAsyncWithBackups(xmlString, STATSCS_FULL_PATH, fileName_base, suffix, ext, backupDir);

        }
        catch (Exception ex)
        {
            Console.WriteLine("WriteMissionObjectivesClass ERROR: " + ex.ToString());
            return false;
        }
        return true;

    }

    public object MO_ReadMissionObject(object mo, string name)
    {
        Console.WriteLine("Reading " + name + " from file");

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_SESSIONSTATE_" + name + ".xml";

        try
        {
            /* 
             * //System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(mo.GetType());
            BinaryFormatter writer = new BinaryFormatter();
            using (FileStream fs = File.Create(filepath))
            {
                writer.Serialize(fs, mo);
            }
            */
            string xmlString = File.ReadAllText(filepath);

            //XmlDictionaryReader reader =
            //    XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());

            var serializer = new DataContractSerializer(mo.GetType());

            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString))) {
                DataContractSerializer formatter0 =
                    new DataContractSerializer(mo.GetType());
                mo = formatter0.ReadObject(reader);
            }

        }


        /*

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_" + name + ".json";        

        //System.Xml.Serialization.XmlSerializer reader = new System.Xml.Serialization.XmlSerializer(mo.GetType());
        BinaryFormatter writer = new BinaryFormatter();

        try
        {
            using (FileStream fs = File.OpenRead(filepath))
            {
                //jsonString = File.ReadAllText(filepath);
                mo =  writer.Deserialize(fs);
            }
        }
        */

        catch (Exception ex)
        {
            Console.WriteLine("ReadMissionObjectivesClass ERROR: " + ex.ToString());
            return null;
        }
        return mo;



    }

    //wait = true makes it wait for the async disk write & verify that it worked/no errors
    //usually we only do this right before final thread exit
    public void MO_WriteMissionObjects(bool wait = false)
    {
        MO_WriteMissionObject(MissionObjectivesList, "MissionObjectivesList", wait);
        MO_WriteMissionObject(ScoutPhotoRecord, "ScoutPhotoRecord", wait);
        MO_WriteMissionObject(DestroyedRadar, "DestroyedRadar", wait);
        MO_WriteMissionObject(MissionObjectivesSuggested, "MissionObjectivesSuggested", wait);
        MO_WriteMissionObject(MissionObjectiveScore, "MissionObjectiveScore", wait);
        MO_WriteMissionObject(MissionObjectivesCompletedString, "MissionObjectivesCompletedString", wait);
        MO_WriteMissionObject(GamePlay.gpTimeofDay(), "MissionCurrentTime", wait);

        //MO_WriteMissionObject(MissionObjectivesString, "MissionObjectivesString", wait);

    }


    public bool[] MO_ReadMissionObjects()
    {
        bool[] ret = new bool[] { true, true, true, true, true, true, true };
        var mo = MO_ReadMissionObject(MissionObjectivesList, "MissionObjectivesList");
        Console.WriteLine("Read " + mo.GetType().ToString());
        if (mo != null) MissionObjectivesList = mo as Dictionary<string, MissionObjective>;
        else ret[0] = false;

        Dictionary<string, MissionObjective> mo_dic = mo as Dictionary<string, MissionObjective>;//have to switch it from type object to Dictionary
        /*
        if (mo != null) foreach ( string key in (mo_dic as Dictionary<string, MissionObjective>).Keys)
            {
                //Console.WriteLine("RMO: Restoring " + key + " " + MissionObjectivesList[key].Scouted.ToString() + mo_dic[key].Scouted.ToString());
                if (!MissionObjectivesList.ContainsKey(key)) continue;
                MissionObjectivesList[key].Scouted = mo_dic[key].Scouted;
                MissionObjectivesList[key].PlayersWhoScoutedNames = mo_dic[key].PlayersWhoScoutedNames;
            }
            */

        var mo1 = MO_ReadMissionObject(MissionObjectivesSuggested, "MissionObjectivesSuggested");
        if (mo1 != null) Console.WriteLine("Read " + mo1.GetType().ToString());
        if (mo1 != null) MissionObjectivesSuggested = mo1 as Dictionary<ArmiesE, List<String>>;
        else ret[1] = false;

        /*
         * //We don't need this as it is reconstructed from the MissionObjectivesList .Destroyed flag for objectives
        var mo2 = MO_ReadMissionObject(DestroyedRadar, "DestroyedRadar");
        Console.WriteLine("Read " + mo1.GetType().ToString());
        if (mo2 != null) DestroyedRadar = mo2 as Dictionary<ArmiesE, List<MissionObjective>>;
        else ret[2] = false;
        */

        var mo3 = MO_ReadMissionObject(MissionObjectiveScore, "MissionObjectiveScore");
        if (mo3 != null) Console.WriteLine("Read " + mo3.GetType().ToString());
        if (mo3 != null) MissionObjectiveScore = mo3 as Dictionary<ArmiesE, double>;
        else ret[3] = false;

        var mo4 = MO_ReadMissionObject(MissionObjectivesCompletedString, "MissionObjectivesCompletedString");
        if (mo4 != null) Console.WriteLine("Read " + mo4.GetType().ToString());
        if (mo4 != null) MissionObjectivesCompletedString = mo4 as Dictionary<ArmiesE, string>;
        else ret[4] = false;

        /*
        var mo5 = MO_ReadMissionObject(MissionObjectivesString, "MissionObjectivesString");
        Console.WriteLine("Read " + mo5.GetType().ToString());
        if (mo5 != null) MissionObjectivesString = mo5 as Dictionary<ArmiesE, string>;
        else ret[5] = false;
        */
        var mo6 = MO_ReadMissionObject(ScoutPhotoRecord, "ScoutPhotoRecord");
        if (mo6 != null) Console.WriteLine("Read " + mo6.GetType().ToString());
        if (mo6 != null)
        {
            ScoutPhotoRecord = mo6 as Dictionary<Tuple<int, int, aPlayer>, List<string>>;
            try
            {
                /*foreach (KeyValuePair<Tuple<int, int, aPlayer>, List<string>> entry in ScoutPhotoRecord)
                {
                    Console.WriteLine(entry.Key);
                    Console.WriteLine(entry.Value);
                    Console.WriteLine(entry.Key.Item1.ToString());
                    Console.WriteLine(entry.Key.Item2.ToString());
                    Console.WriteLine(entry.Key.Item3.ToString());
                    Console.WriteLine(entry.Key.Item3.name);
                    Console.WriteLine(entry.Key.Item3.army.ToString());
                }
                */
            } catch (Exception ex) { Console.WriteLine("ScoutPhotoRecord read from disk ERROR: " + ex.ToString()); }
        }
        //if (mo6 != null) ScoutPhotoRecord = mo6 as ScoutPhotoRecord_class;
        else ret[6] = false;

        return ret;
        /*
        MO_WriteMissionObject(DestroyedRadar, "DestroyedRadar");
        MO_WriteMissionObject(MissionObjectivesSuggested, "MissionObjectivesSuggested");
        MO_WriteMissionObject(MissionObjectiveScore, "MissionObjectiveScore");
        MO_WriteMissionObject(MissionObjectivesCompletedString, "MissionObjectivesCompletedString");
        MO_WriteMissionObject(MissionObjectivesString, "MissionObjectivesString");
        */

    }


    //This reads the primary objectives selected from the previous mission
    //Just reads the previous objectives, but takes into consideration that objectives might have been removed, names changed
    //required point total increased or decreased, etc etc since the file was written
    public void MO_ReadPrimaryObjectives(int army = 0)
    {
        //MO_SelectPrimaryObjectives(army);
        if (army < 1 || army > 2) return;

        Console.WriteLine("Reading Mission Objectives from file for " + ArmiesL[army]);

        List<string> moKeys = new List<string>(MissionObjectivesList.Keys);

        //first, remove any/all existing primary targets marked in the MissionObjectivesList
        //reason is, we want the .ini file primary objective list to be the operative list. 
        //What it says, goes.  So you can for example change or delete objectives just by editing the .ini file.  Then MissionObjectivesList will be updated (here) to match that.
        foreach (var key in moKeys)
        {
            if (MissionObjectivesList[key].AttackingArmy == army) MissionObjectivesList[key].IsPrimaryTarget = false;
        }

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapObjectives.ini";

        //Ini.IniFile ini = new Ini.IniFile(filepath, this);
        Ini.IniFile ini = new Ini.IniFile(filepath);

        List<string> keys = ini.IniReadList(ArmiesL[army] + "_Objectives", "Objective");
        //Console.WriteLine("READ: " + keys.ToString());

        double totalPoints = 0;
        foreach (var key in keys)
        {
            //Console.WriteLine("MOReading " + key);
            //The objective that previous existed may not be in the list on this run, so we have to be careful when reading it, not just assume it exists
            var mo = new MissionObjective(this);
            if (!MissionObjectivesList.TryGetValue(key, out mo)) continue;

            if (mo.AttackingArmy == army && mo.PrimaryTargetWeight > 0 && mo.IsEnabled && !mo.IsPrimaryTarget)
            {
                if (totalPoints < MO_PointsRequired[(ArmiesE)army])
                {
                    mo.IsPrimaryTarget = true;
                    totalPoints += mo.Points;
                    //MissionObjectivesString[(ArmiesE)army] += " - " + mo.Sector + " " + mo.Name;
                    //Console.WriteLine("MOReading add:" + key);
                }
                else
                {
                    mo.IsPrimaryTarget = false;
                    //Console.WriteLine("MOReading skip:" + key);
                }

            }

        }

        //In case the total points are not enough, we can go select more additional objectives
        //Console.WriteLine(totalPoints.ToString () + " < points > " + MO_PointsRequired[(ArmiesE)army].ToString());
        if (totalPoints < MO_PointsRequired[(ArmiesE)army])
        {
            MO_SelectPrimaryObjectives(army, totalPoints, fresh: false);
            //MO_WritePrimaryObjectives(); DON'T do this here as it erases both army's info at and this point we're only doing one army, so writing now will DELETE the other army
        }
        return;


    }

    public void MO_WritePrimaryObjectives()
    {


        Console.WriteLine("MO_Write #2");

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapObjectives.ini";
        string filepath_old = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapObjectives_old.ini";
        string currentContent = String.Empty;

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX5"); //Testing for potential causes of warping
        //Save most recent copy of Campaign Map Score with suffix _old
        try
        {
            if (File.Exists(filepath_old)) { File.Delete(filepath_old); }
            File.Copy(filepath, filepath_old); //We could use File.Move here if we want to eliminate the previous .ini file before writing new data to it, thus creating an entirely new .ini.  But perhaps better to just delete specific sections as we do below.
            Console.WriteLine("MO_Write #2a");
        }
        catch (Exception ex) { Console.WriteLine("MO_Write Inner: " + ex.ToString()); }


        Console.WriteLine("MO_Write Save #3");

        try
        {

            //Ini.IniFile ini = new Ini.IniFile(filepath, this);
            Ini.IniFile ini = new Ini.IniFile(filepath);

            //.ini keeps the same file & just adds or updates entries already there. Unless you delete them.
            //Delete all entries in these sections first
            ini.IniDeleteSection("Red_Objectives");
            ini.IniDeleteSection("Blue_Objectives");

            //Console.WriteLine(MO_ListAllPrimaryObjectives((int)ArmiesE.Red).ToString());
            //Console.WriteLine(MO_ListAllPrimaryObjectives((int)ArmiesE.Blue).ToString());

            //Write the new data in the two sections
            ini.IniWriteList("Red_Objectives", "Objective", MO_ListAllPrimaryObjectives((int)ArmiesE.Red));
            ini.IniWriteList("Blue_Objectives", "Objective", MO_ListAllPrimaryObjectives((int)ArmiesE.Blue));

            //Save campaign objective list to special directory as a bit of a backup/record of objectives over time
            Console.WriteLine("MO_Write #3a");
        }
        catch (Exception ex) { Console.WriteLine("MapState Write: " + ex.ToString()); }

        MO_makeCampaignFilesBackup(filepath, @" campaign backups\", @"_MapObjectives-", ".ini");

        Console.WriteLine("MO_Write #4");

        /*
        var backPath = STATSCS_FULL_PATH + CAMPAIGN_ID + @" campaign backups\";
        string filepath_date = backPath + CAMPAIGN_ID + @"_MapObjectives-" + dt.ToString("yyyy-MM-dd-tt") + ".ini";

        //Create the directory for the MapState.txt backup files, if it doesn't exist
        if (!System.IO.File.Exists(backPath))
        {

            try
            {
                //System.IO.File.Create(backPath);
                System.IO.Directory.CreateDirectory(backPath);
            }
            catch (Exception ex) { Console.WriteLine("MO_Write Dir Create Date: " + ex.ToString()); }

        }

        //Save most recent copy of Campaign Objectives List with suffix like  -2018-05-13.ini
        try
        {
            if (File.Exists(filepath_date)) { File.Delete(filepath_date); }
            File.Copy(filepath, filepath_date);
        }
        catch (Exception ex) { Console.WriteLine("MO_Write Date: " + ex.ToString()); }
        */



    }

    public void MO_makeCampaignFilesBackup(string fullFilename, string dirName, string fileSuffix, string extension)
    {

        DateTime dt = DateTime.UtcNow;
        string date = dt.ToString("u");

        var backPath = STATSCS_FULL_PATH + CAMPAIGN_ID + dirName;
        string filepath_date = backPath + CAMPAIGN_ID + fileSuffix + dt.ToString("yyyy-MM-dd-tt") + extension;

        //Create the directory for the MapState.txt backup files, if it doesn't exist
        if (!System.IO.File.Exists(backPath))
        {

            try
            {
                //System.IO.File.Create(backPath);
                System.IO.Directory.CreateDirectory(backPath);
            }
            catch (Exception ex) { Console.WriteLine("MO_Write Dir Create Date: " + ex.ToString()); }

        }

        //Save most recent copy of corresponding file with suffix like  -2018-05-13.ini
        try
        {
            if (File.Exists(filepath_date)) { File.Delete(filepath_date); }
            File.Copy(fullFilename, filepath_date);
        }
        catch (Exception ex) { Console.WriteLine("MO_Write Date: " + ex.ToString()); }


    }

    //Lists the current secondary/suggested targets to theplayer's screen, and/or just returns the keys of the objectives as a List<string>
    public List<string> MO_ListSuggestedObjectives(Player player, int army, int numToDisplay = 5, double delay = 0.2, bool display = true)
    {

        int numDisplayed = 0;
        double totDelay = 0;
        List<string> currentSecondaryObjectives = new List<string>();

        if (player != null && display) twcLogServer(new Player[] { player }, "SUGGESTED " + ArmiesL[army].ToUpper() + " SECONDARY OBJECTIVES:", new object[] { });

        string msg1 = ">>> NOTE: If recon areas are listed, those sectors need to be scouted.";


        if (player != null && display) twcLogServer(new Player[] { player }, msg1, new object[] { });

        msg1 = ">>> Scout those sectors and take recon photos - Tab-4-9. HQ will then identify specific targets.";

        if (player != null && display) twcLogServer(new Player[] { player }, msg1, new object[] { });

        foreach (var key in MissionObjectivesSuggested[(ArmiesE)army])
        {

            if (numDisplayed >= numToDisplay) break;
            MissionObjective mo = MissionObjectivesList[key];
            if (!mo.Destroyed && !mo.ObjectiveAchievedForPoints && mo.IsEnabled)
            {
                currentSecondaryObjectives.Add(key);

                if (display && player != null)
                {
                    totDelay += delay;
                    Timeout(totDelay, () =>
                    {
                        //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               

                        // + " (" + mo.Pos.x + "," + mo.Pos.y + ")"



                        string msg = "Recon sector: " + mo.Sector.Substring(0, 4).TrimEnd('.');
                        if (mo.Scouted) msg = mo.Sector + " " + mo.Name + " (" + mo.Pos.x.ToString("F0") + ", " + mo.Pos.y.ToString("F0") + ")";
                        twcLogServer(new Player[] { player }, msg, new object[] { });

                    });//timeout    
                }


                numDisplayed++;


            }
        }

        return currentSecondaryObjectives;

    }

    public string MO_ListScoutedObjectives(Player player = null, int army = 1, int numToDisplay = -1, double delay = 0.2) //num to display less than 1 means display all
    {

        int numDisplayed = 0;
        double totDelay = 0;

        string retmsg = "";
        string msg = "Scouted " + ArmiesL[army] + " Targets with Coordinates:";

        if (player != null) twcLogServer(new Player[] { player }, msg, new object[] { });
        retmsg += msg + Environment.NewLine;

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        //foreach (var key in MissionObjectives[(ArmiesE)army])
        {
            //mo.AttackingArmy == army
            if (numToDisplay > 0 && numDisplayed >= numToDisplay) break;
            MissionObjective mo = entry.Value;


            if (mo.AttackingArmy == army && mo.IsEnabled && mo.Scouted)
            {
                totDelay += delay;
                //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               

                // + " (" + mo.Pos.x + "," + mo.Pos.y + ")"
                string msg6 = mo.Sector + " " + mo.Name + " (" + mo.Pos.x.ToString("F0") + ", " + mo.Pos.y.ToString("F0") + ")";
                if (mo.Destroyed) msg += " (destroyed)";
                else if (mo.IsPrimaryTarget) msg += " (primary objective)";
                retmsg += msg6 + Environment.NewLine;
                numDisplayed++;
                Timeout(totDelay, () =>
                {
                    if (player != null) twcLogServer(new Player[] { player }, msg6, new object[] { });
                });//timeout
                if (mo.hasGeneralStaff)
                {

                    var gsl = GeneralStaffLocations[(ArmiesE)army];
                    string af = "Luftwaffe";
                    if (army == 2) af = "RAF";
                    int timeLeft_min = calcTimeLeft_min();
                    string msg7 = ">>>Recon has identified a possible group of high-ranking " + af + " officers in sector " + gsl.sector + " near " + mo.Name;
                    if (timeLeft_min < END_MISSION_TICK / 2000 / 2) msg7 = ">>>Recon has received word that " + gsl.staffGroupName + " are in sector " + gsl.sectorKeypad + " near " + mo.Name;
                    if (timeLeft_min < END_MISSION_TICK / 2000 / 4) msg7 = ">>>Recon has received word that " + gsl.staffGroupName + " are in sector " + gsl.sectorDoublekeypad + " near " + mo.Name;
                    //if (timeLeft_min < END_MISSION_TICK / 2000 / 8) msg7 = ">>>" + gsl.staffGroupName + " may have been spotted in sector " + gsl.sectorDoublekeypad + " near " + mo.Name;
                    retmsg += msg7 + Environment.NewLine;
                    totDelay += delay;
                    Timeout(totDelay, () =>
                    {
                        if (player != null) twcLogServer(new Player[] { player }, msg7, new object[] { });
                    });//timeout

                }


            }
        }
        if (numDisplayed == 0)
        {
            msg = ">>>>> No objectives scouted yet <<<<<";
            if (player != null) twcLogServer(new Player[] { player }, msg, new object[] { });
            retmsg += msg + Environment.NewLine;
        }
        Timeout(totDelay + 2, () =>
        {
            if (player != null) twcLogServer(new Player[] { player }, ">>> To scout objectives, fly 20000ft/6000m or higher in an aircraft with no bombs on board and record a reconnaissance photo of the area via Tab-4-9.", new object[] { });
            Timeout(2, () =>
            {
                if (player != null) twcLogServer(new Player[] { player }, ">>> Return the photos to base, land safely, and use chat command <record to transfer the photos to headquarters.", new object[] { });
            });
            Timeout(4, () =>
            {
                if (player != null) twcLogServer(new Player[] { player }, ">>> The photos allow headquarters to determine precise coordinates of all potential objectives in that area.", new object[] { });
            });
            //twcLogServer(new Player[] { player }, ">>>>> The higher you fly the larger the area your photo will capture.", new object[] { });

        });

        return retmsg;
    }

    /*
    [CollectionDataContract
    (Name = "ScoutPhotoRecord",
    ItemName = "entry",
    KeyName = "Playertuple",
    ValueName = "ScoutedObjectiveList")]
    public class ScoutPhotoRecord_class : Dictionary<Tuple<int, int, aPlayer>, List<string>> { };
    */

    int ScoutPhotoID = 0;


    Dictionary<Tuple<int, int, aPlayer>, List<string>> ScoutPhotoRecord = new Dictionary<Tuple<int, int, aPlayer>, List<string>>(); //<int,int> = ScoutPhotoID, Army
    //ScoutPhotoRecord_class ScoutPhotoRecord = new ScoutPhotoRecord_class(); //<int,int> = ScoutPhotoID, Army

    //NOTE: ScoutPhotoRecord is saved/restored during our usual regular/periodic backups & then restored on mission start
    public Dictionary<Player, int> LastPhotoTime_sec = new Dictionary<Player, int>(); //last time in seconds player took a photo

    public void MO_TakeScoutPhoto(Player player, int army, double delay = 0.2, Point3d? test = null)
    {
        bool firstPhotoThisSession = true;
        if (LastPhotoTime_sec.ContainsKey(player)) firstPhotoThisSession = false;
        int currTime_sec = Time.tickCounter() / 33;
        if (LastPhotoTime_sec.ContainsKey(player) && (currTime_sec - LastPhotoTime_sec[player]) < 15)
        {
            gpLogServerAndLog(new Player[] { player }, "Recon photo NOT recorded - photo once every 15 seconds at most! ", null);
            // + currTime_sec.ToString() + " " + LastPhotoTime_sec[player].ToString()
            return;
        }

        int numScouted = 0;
        string minAlt = "20000 feet";
        if (army == 2) minAlt = "6000m";

        bool fail = false;
        if (player.Place() as AiAircraft == null && !test.HasValue) fail = true;

        AiAircraft aircraft = player.Place() as AiAircraft;

        Point3d pos = aircraft.Pos();
        if (test.HasValue) pos = test.Value; //for testing

        double altitude = pos.z;

        if (altitude < 6000)
        {
            fail = true;
            LastPhotoTime_sec[player] = currTime_sec;
            if (MO_CheckForGeneralStaffReconPhoto(player, aircraft, army, pos)) return; //If they're taking a legit general staff recon photo we don't give an error message
        }

        double radiusCovered_m = altitude / 0.342;  //(Sin (20 degrees) = 0.342)  Meaning that we can scout things 20 degrees below the horizon & lower.  This might be alittle ambitious, but also maybe not?

        string alt = (radiusCovered_m / 1000.0).ToString("F1") + " km";
        if (army == 1) alt = (Calcs.meters2feet(radiusCovered_m) / 5280.0).ToString("F1") + " miles";

        if (aircraft.AirGroup().hasBombs()) fail = true;

        if (fail)
        {
            twcLogServer(new Player[] { player }, "You must be in an aircraft, with no bombs on board, above " + minAlt + ", to successfully take a reconnaissance photo.", new object[] { });
            return;
        }

        LastPhotoTime_sec[player] = currTime_sec;
        ScoutPhotoID++;

        List<string> keys = new List<string>();

        twcLogServer(new Player[] { player }, ">>> Reconnaissance photo successfully taken, covering a radius of approx. " + alt + ".", new object[] { });

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        //foreach (var key in MissionObjectives[(ArmiesE)army])
        {
            //mo.AttackingArmy == army

            MissionObjective mo = entry.Value;

            //if (!mo.Destroyed && mo.AttackingArmy == army && mo.IsEnabled && !mo.Scouted && Calcs.CalculatePointDistance (mo.Pos, pos) < radiusCovered_m) // no reason they can't scout destroyed objects - some might be undestroyed sometime soon. Also we're allowing multiple people to scout the same objective now, they just get less points if they are not the first.
            if (mo.AttackingArmy == army && mo.IsEnabled && Calcs.CalculatePointDistance(mo.Pos, pos) < radiusCovered_m)
            {
                keys.Add(entry.Key);
                numScouted++;
            }
        }

        var recordKey = new Tuple<int, int, aPlayer>(ScoutPhotoID, army, new aPlayer(player));
        ScoutPhotoRecord.Add(recordKey, keys);

        Timeout(60 * 60, () => {
            if (ScoutPhotoRecord.ContainsKey(recordKey)) {
                int total = ScoutPhotoRecord[recordKey].Count();
                ScoutPhotoRecord.Remove(recordKey);
                if (total > 0) twcLogServer(new Player[] { player }, "I'm sorry to inform you that your reconnaissance photo taken over 1 hour ago identifying " + total.ToString() + " objectives were spoiled due to equipment malfunction during an overly extended flight.", new object[] { });
            }

        }); //spoil it after 1 hour if not returned

        if (numScouted == 0)
        {
            Timeout(3, () =>
            {
                twcLogServer(new Player[] { player }, ">>> That area did not look very promising for locating valuable objectives.", new object[] { });
                return;
            });
        }

        numScouted += Convert.ToInt32(Math.Round(random.Next(numScouted * 3) / 4.0 - numScouted * 3.0 / 8.0)); //fuzz the result a little
        if (numScouted < 0) numScouted = 0;
        string msg1 = ">>> That area did not look very promising for locating valuable objectives.";
        if (numScouted > 0) msg1 = ">>> It looks like that area may contain a few valuable military objectives.";
        if (numScouted>8) msg1 = ">>> It looks like that area may contain several valuable military objectives.";

        Timeout(1.5, () =>
        {
            twcLogServer(new Player[] { player }, msg1 , new object[] { });
        });

        if (firstPhotoThisSession) //onlyi give this announcement for the FIRST photo of this session
        {
            Timeout(3, () =>
            {
                twcLogServer(new Player[] { player }, "Reconnaissance results will be available after you land safely and headquarters has a chance to analyze the photos fully.", new object[] { });
            });
            Timeout(4.5, () =>
            {
                twcLogServer(new Player[] { player }, "You have one hour to return and land safely or the photo will be spoiled.", new object[] { });
            });
        } else
        {
            Timeout(4.5, () =>
            {
                twcLogServer(new Player[] { player }, ">>> One hour to land/record photo.", new object[] { });
            });
        }
    }

    public void MO_SpoilPlayerScoutPhotos(Player player)
    {
        int total = 0;
        var ScoutPhotoRecord_copy = new Dictionary<Tuple<int, int, aPlayer>, List<string>>(ScoutPhotoRecord); //<int,int> = ScoutPhotoID, Army
        foreach (KeyValuePair<Tuple<int, int, aPlayer>, List<string>> entry in ScoutPhotoRecord_copy)
        {
            if (entry.Key.Item3 != new aPlayer(player)) continue;
            total += ScoutPhotoRecord[entry.Key].Count;
            ScoutPhotoRecord.Remove(entry.Key);
        }
        if (total > 0)
            Timeout(20, () => //wait a while for message because it often comes when player died, crashed, etc, many other messages coming through at once.
            {
                twcLogServer(new Player[] { player }, "I'm sorry to inform you that your reconnaissance photos identifying " + total.ToString() + " objectives were lost.", new object[] { });
            });
    }

    public void MO_SpoilPlayerScoutPhotos(HashSet<Player> players)
    {

        foreach (Player player in players)
        {
            MO_SpoilPlayerScoutPhotos(player);
        }

    }

    
    HashSet<Tuple<int, int, aPlayer>> photosRecorded = new HashSet<Tuple<int, int, aPlayer>>();

    //check = true means, the player is requesting to record the photos, ie, checking.
    //Otherwise it is some automated thing & no message is required unless there is success.
    public void MO_RecordPlayerScoutPhotos(Player player, bool check = false, bool test = false)
    {
        if (player == null) return;
        if (test ||
            (player.Place() != null && (player.Place() as AiAircraft) != null &&
            GamePlay.gpFrontArmy(player.Place().Pos().x, player.Place().Pos().y) == player.Army() &&
            //Stb_distanceToNearestAirport(actor) < 3100 &&
            Calcs.CalculatePointDistance((player.Place() as AiAircraft).AirGroup().Vwld()) < 2 &&
            player.Place().IsAlive())
            )
        {//it's good 
         //(do nothing)
        }
        else
        {//it's no good
            if (check) twcLogServer(new Player[] { player }, "You can't check in your reconnaissance photos now - you must be safely landed, in your aircraft with the photos, on friendly ground and ideally at an air base.", new object[] { });
            return;
        }

        //Console.WriteLine("RCRec: #1");


        int totalPhotos = 0;
        int total = 0;
        int totalPrimary = 0;
        int totalSecondary = 0;
        int totalRadar = 0;
        int totalAirfield = 0;
        int totalShip = 0;
        int totalFuel = 0;
        int totalNeverScouted = 0;
        int army = player.Army();
        //List<String> mos = MissionObjectivesSuggested[(ArmiesE)army];
        List<String> mos = MO_ListSuggestedObjectives(null, player.Army(), display: false); //Get the current list of MissionObjectivesSuggested[(ArmiesE)OldObj.AttackingArmy];

        var ScoutPhotoRecord_copy = new Dictionary<Tuple<int, int, aPlayer>, List<string>>(ScoutPhotoRecord); //<int,int> = ScoutPhotoID, Army

        //Console.WriteLine("RCRec: #2");

        HashSet<string> objectivesIDed = new HashSet<string>();

        //Console.WriteLine("RCRec: #2a");

        foreach (KeyValuePair<Tuple<int, int, aPlayer>, List<string>> entry in ScoutPhotoRecord_copy)
        {
            try
            { //if the on-disk copy of ScoutPhotoRecord gets messed upsometimes the values are really wonky and give an error here no matter what.  So this catches them & continues, if possible.
              //Console.WriteLine("RCRec: #2b");
                if (entry.Key == null || photosRecorded.Contains(entry.Key)) continue;  //This photo already recorded this session.  prevents double-counting photos/objectives found while HQ is 'processing' the info for up to 360 seconds.
                                                                                        //if (entry.Key.Item3 != new aPlayer(player)) continue;
                                                                                        //Console.WriteLine("RCRec: #2c");
                aPlayer aplayer = new aPlayer(player);
                //Console.WriteLine("RCRec: #2d");
                if (entry.Key == null) continue;
                //Console.WriteLine("RCRec: #2da");
                //Console.WriteLine("RCRec: #2da" + entry.Key.ToString());
                if (DBNull.Value.Equals(entry.Key.Item1) || DBNull.Value.Equals(entry.Key.Item2) || DBNull.Value.Equals(entry.Key.Item3)
                    || DBNull.Value.Equals(entry.Key.Item3.name) || DBNull.Value.Equals(entry.Key.Item3.army)) continue; //This can happen if data is empty or just corrupted.
                                                                                                                         //Console.WriteLine("RCRec: #2da" + entry.Key.Item3.ToString());
                                                                                                                         //Console.WriteLine("RCRec: #2da" + (entry.Key.Item3).GetType().ToString());
                                                                                                                         //if (entry.Key.Item3.name == "") Console.WriteLine("yes"); 

                if (entry.Key.Item3 == null) continue;
                //Console.WriteLine("RCRec: #3da");
                //if (entry.Key.Item3.name == null) continue;
                Console.WriteLine("RCRec: #3da");
                //if (entry.Key.Item3.army == null) continue;
                /* Console.WriteLine("RCRec: #3da");
                Console.WriteLine("RCRec: #3db" + aplayer.name);
                Console.WriteLine("RCRec: #3dc" + entry.Key.Item3.name);
                Console.WriteLine("RCRec: #3dd" + entry.Key.Item3.army.ToString());
                Console.WriteLine("RCRec: #3de" + aplayer.army.ToString());
                */

                if (entry.Key.Item3.name != aplayer.name || entry.Key.Item3.army != aplayer.army) continue;
                //if (entry.Key.Item3 == null || entry.Key.Item3 != aplayer) continue;
                //Console.WriteLine("RCRec: #2e");

            }catch ( Exception ex)
            {
                try { ScoutPhotoRecord.Remove(entry.Key); } catch (Exception ex1) { Console.WriteLine("Record Scout Photo error2!!! " + ex1.ToString()); }
                Console.WriteLine("Record Scout Photo error!!! " + ex.ToString()); 
                continue;
            }
            totalPhotos++;

            //Console.WriteLine("RCRec: #3");

            if (player.Army() == 1) RedScoutPhotosI++;
            if (player.Army() == 2) BlueScoutPhotosI++;
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_IncrementSessStat(player, 848);  //848 recon photos taken, 849 # of objectives photographed
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddToMissionStat(player, 848, 1);

            string amalg = player.Name()+player.Army().ToString();         

            var copyList = new List<string>(entry.Value);
            copyList.Add(amalg);

            int seed = Calcs.seedObj(copyList.ToArray());

            Random repeatRandom = new Random(seed);

            Console.WriteLine("RCRec: #4 " + seed.ToString());

            foreach (string key in entry.Value)
            {
                //So, from a given recon mission we only identify 1/4 of possible objectives in that area.  Because recon photos aren't perfect, photo intelligence ID
                //of targets isn't perfect, etc. So that we reconning the same area again might result in more targets IDed.
                //But, we always identify primary targets, not because recon is specially good for them, but because  HQ has chosen the primary targets based on 
                //Recon photos returned.  
                //FUTURE: Maybe 1/4 of targets IDed is too much or too little.  Or maybe it varies day to day depending on weather, time of day photo taken, etc.

                if (!MissionObjectivesList.ContainsKey(key)) continue; //maybe this objective has been deleted/removed/whatever since it was originally scouted

                MissionObjective mo = MissionObjectivesList[key];
                int ct = objectivesIDed.Count;
                objectivesIDed.Add(key);

                //Console.WriteLine("RCRec: #5");                

                if (!mo.IsPrimaryTarget && repeatRandom.Next(4) != 0) continue;  //only "find" 1/4 of the objectives in the scouted area.  But **always** find the primary targets; always skip disabled.  Add the mo to the processed list first, THEN determine if it is one of the 1/4 identified.  Otherwise each mo could get multiple chances to be picked, if the player has taken multiple photos of the same area.
                if (!mo.IsEnabled) continue;  

                if (ct != objectivesIDed.Count) //only bother doing all this stuff if it's new/unique object not before identified during this photo processing run from this player
                {

                    if (player.Army() == 1) RedScoutedObjectivesI++;
                    if (player.Army() == 2) BlueScoutedObjectivesI++;
                    if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_IncrementSessStat(player, 849);  //848 recon photos taken, 849 # of objectives photographed
                    if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddToMissionStat(player, 849, 1);

                    //Console.WriteLine("RCRec: #6");

                    Timeout(random.Next(360), () =>
                    {
                        mo.makeScouted(player);
                        if (mo.hasGeneralStaff)
                        {
                            string af = "RAF";
                            if (army == 2) af = "Luftwaffe";
                            string name = "";
                            if (player != null && player.Name() != null) name = "by " + player.Name();
                            string msg7 = ">>>Recon" + name + " has identified a possible group of high-ranking " + af + " officers near " + mo.Name;
                            twcLogServer(null, msg7, null);
                        }
                    }); //Some delay for careful analysis, before they show up in the in-game lists. 

                    //Console.WriteLine("RCRec: #7");
                    total++;
                    if (mo.IsPrimaryTarget) totalPrimary++;
                    if (mos.Contains(key) && mos.IndexOf(key) < 5) totalSecondary++;
                    if (mo.MOObjectiveType == MO_ObjectiveType.Radar) totalRadar++;
                    if (mo.MOObjectiveType == MO_ObjectiveType.Airfield) totalAirfield++;
                    if (mo.MOObjectiveType == MO_ObjectiveType.Ship) totalShip++;
                    if (mo.MOObjectiveType == MO_ObjectiveType.Fuel) totalFuel++;
                    if (!mo.Scouted) totalNeverScouted++;

                    //Console.WriteLine("RCRec: #8");
                    //Radar, AA, Ship, Building, Fuel, Airfield, Aircraft, Vehicles, Bridge, Dam, Dock, RRYard, Railroad, Road, AirfieldComplex, FactoryComplex, ArmyBase
                }

            }
            //Console.WriteLine("RCRec: #9");
            photosRecorded.Add(entry.Key); //avoid processing this photo again this session, but leave it in the ScoutPhotoRecord until all items have been processed, just to avoid losing data when the server dies or whatever
            Timeout(360, () => ScoutPhotoRecord.Remove(entry.Key)); //to avoid losing data in case of server crash/exit/etc we delay deleting this photo until 360 seconds after initial processing, when all the mos should be marked scouted.  So if the server crashes in the meanwhile, the photo(s) can just be re-read & re-processed on next restart.
            //Console.WriteLine("RCRec: #10");
        }

        if (total == 0 && totalPhotos > 0) twcLogServer(new Player[] { player }, "I'm sorry to inform you that your " + totalPhotos.ToString() + " reconnaissance photos identified no new objectives.", new object[] { });
        else if (totalPhotos == 0 && check) twcLogServer(new Player[] { player }, "You have no reconnaissance photos to process.  Perhaps they were processed automatically when you landed?", new object[] { });

        //Console.WriteLine("RCRec: #11");
        if (total > 0) {
            int totalOthers = total - totalPrimary - totalSecondary;
            if (totalOthers < 0) totalOthers = 0;
            twcLogServer(new Player[] { player }, ">>>> Your " + totalPhotos.ToString() + " reconnaissance photos identified " + total.ToString() + " objectives, including " + totalPrimary.ToString() + " HQ has named as primary objectives, " + totalSecondary.ToString() + " HQ named as secondary objectives, and " + totalOthers.ToString() + " other important objectives.", new object[] { });
            Timeout(1.5, () =>
            {
                twcLogServer(new Player[] { player }, ">>>> Among the objectives: " + totalRadar.ToString() + " radar installations, " + totalAirfield.ToString() + " airfields, " + totalFuel.ToString() + " fuel dumps, " + totalShip.ToString() + " ships, " + totalNeverScouted.ToString() + " objectives not previously identified, and confirmation of " + (total - totalNeverScouted).ToString() + " objectives previously identified.", new object[] { });
            });
            Timeout(3, () =>
            {
                twcLogServer(new Player[] { player }, ">>>> Precise mapping coordinates for your objectives will be available via Tab-4-8 in a few minutes, after headquarters has a chance to carefully analyze the photos.", new object[] { });
            });

        }
        //Console.WriteLine("RCRec: #12");
    }

    public void MO_RecordPlayerScoutPhotos(HashSet<Player> players)
    {

        foreach (Player player in players)
        {
            MO_RecordPlayerScoutPhotos(player);
        }

    }

    //note - only lists first 12 targets BY DEFAULT
    public string MO_ListRemainingPrimaryObjectives(Player player, int army, int numToDisplay = 12, double delay = 0.2, bool display = true, bool html = false)
    {

        string newline = Environment.NewLine;
        if (html) newline = "<br>" + Environment.NewLine;
        string retmsg = "";
        string msg = "";

        int numDisplayed = 0;
        double totDelay = 0;
        msg = "REMAINING " + ArmiesL[army].ToUpper() + " PRIMARY OBJECTIVES:";
        if (display) twcLogServer(new Player[] { player }, msg, new object[] { });
        retmsg = msg + newline;

        msg = ">>> NOTE: If recon areas are listed, those sectors need to be scouted.";
        if (display) twcLogServer(new Player[] { player }, msg, new object[] { });
        retmsg += msg + newline;

        msg = ">>> Scout the areas and take recon photos - Tab-4-9. HQ will then identify specific targets & details.";
        if (display) twcLogServer(new Player[] { player }, msg, new object[] { });
        retmsg += msg + newline;

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {

            if (numDisplayed >= numToDisplay) break;
            MissionObjective mo = entry.Value;
            if (!mo.ObjectiveAchievedForPoints && mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
            {

                string msg1 = "Recon area: " + mo.bigSector;
                if (mo.Scouted) msg1 = mo.Sector + " " + mo.Name + " (" + mo.Pos.x.ToString("F0") + ", " + mo.Pos.y.ToString("F0") + ")";
                retmsg += msg1 + newline;
                totDelay += delay;
                if (display)
                {
                    Timeout(totDelay, () =>
                    {

                        //+ " (" + mo.Pos.x + "," + mo.Pos.y + ")" //to display x,y coordinates
                        twcLogServer(new Player[] { player }, msg1, new object[] { });

                    });//timeout      
                }

                numDisplayed++;

            }
        }

        return retmsg;
    }

    public string MO_SectorOfRandomRemainingPrimaryObjective(int army)
    {

        var remainingMOs = new List<MissionObjective>();

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {

            MissionObjective mo = entry.Value;
            if (!mo.ObjectiveAchievedForPoints && mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
            {
                remainingMOs.Add(mo);
            }
        }

        if (remainingMOs.Count <= 0) return "";

        MissionObjective m = remainingMOs.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

        return Calcs.correctedSectorNameKeypad(this, m.Pos);
    }

    public void MO_WriteOutAllMissionObjectives(string filename, bool misformat = true, bool triggersonly = false)
    {


        List<string> keys = MissionObjectivesList.Keys.ToList();
        //keys.Sort();       //unfortunately sorting just actually scrambles them hopelessly
        string op = "";
        DateTime utcDate = DateTime.UtcNow;
        op += utcDate.ToString("u") + Environment.NewLine;
        if (!misformat) op += "Can view with text editor or Excel (tab-delimited text file). If you open in Excel, save immediately with a new name so you don't block this file being re-written by a new mission." + Environment.NewLine;

        if (!misformat) op += "ID\tName\tAttackingArmy\tOwnerArmy\tIsEnabled\tMOObjectiveType\tMOTriggerType\tIsPrimaryTarget\tPrimaryTargetWeight\tPoints\tDestroyed\tPos.x\tPos.y\tSector\tRadarEffectiveRadius\tTriggerName\tTriggerType\tTriggerPercent\tTriggerDestroyRadius\tStaticPercentageRequired\tStaticRemoveDelay_sec\tStaticRemoveSpread_sec\tComment\tHUDMessage\tLOGMessage\tStaticNames\tStaticRemoveNames" + Environment.NewLine;


        //foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        foreach (string k in keys)
        {


            MissionObjective mo = MissionObjectivesList[k];
            if (triggersonly && !mo.MOTriggerType.Equals(MO_TriggerType.Trigger)) continue;
            if (misformat)
            {
                op += mo.ToString(misformat) + Environment.NewLine;
            }
            else
            {
                op += mo.ToString(misformat) + Environment.NewLine;
            }

        }

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX6"); //Testing for potential causes of warping
        try
        {
            string filepath = CLOD_PATH + FILE_PATH + @"/" + filename;
            //File.WriteAllText(filepath, op);
            Calcs.WriteAllTextAsync(filepath, op);
        }
        catch (Exception ex) { Console.WriteLine("MO_WriteOutAll: " + ex.ToString()); }
    }

    HashSet<string> MO_flakMissionsLoaded = new HashSet<string>();
    public bool MO_LoadAllPrimaryObjectiveFlak(Dictionary<string, string> flakMissions)
    {


        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {
            MissionObjective mo = entry.Value;
            if (mo.IsPrimaryTarget && mo.IsEnabled)
            {
                string flakID = mo.FlakID;
                string flakMission = "";

                if (flakMissions.Keys.Contains(flakID)) flakMission = flakMissions[flakID];
                else Console.WriteLine("LoadFlak: No flak found for " + mo.Name + "  " + flakID);

                if (MO_flakMissionsLoaded.Contains(flakID)) continue;  //Make sure we load each mission just once at most
                MO_flakMissionsLoaded.Add(flakID);

                if (File.Exists(CLOD_PATH + FILE_PATH + flakMission)) {
                   Timeout(48, () =>
                   {
                       GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + flakMission);
                       DebugAndLog(flakMission + " file loaded");
                       Console.WriteLine(flakMission + " file loaded");
                   });

                } else
                {
                    Console.WriteLine("LoadFlak: No flak file found for " + mo.Name + "  " + flakID + " " + flakMission);
                }
            }
        }
        return true;
    }

    //This needs to be run whenever the objectives list is reinitialized, but also after each mission restart, after the missionobjectiveslist has been reloaded from disk
    public void MO_InitializeAllObjectives()
    {

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {

            MissionObjective mo = entry.Value;
            //Console.WriteLine("Initialize - checking " + mo.ID + " " + mo.Name + " {0} {1} {2}", mo.AutoFlak, mo.AutoFlakIfPrimary, mo.IsPrimaryTarget);
            if (mo.IsEnabled)
            {

                //load submission if requested
                if (mo.InitSubmissionName != null && mo.InitSubmissionName.Length > 0)
                {
                    string s = CLOD_PATH + FILE_PATH + mo.InitSubmissionName;
                    GamePlay.gpPostMissionLoad(s);
                    Console.WriteLine(s.Replace(CLOD_PATH + FILE_PATH, "") + " file loaded");
                }
                if (mo.MOTriggerType == MO_TriggerType.PointArea)
                {
                    //place a jerrycan in the middle of the area, covering it.  This allows stats to be counted for anything in this area, and also sets up smoke etc for any bombs hitting this area.
                    string jerry = "JerryCan_GER1_1";
                    if (mo.TriggerDestroyRadius > 71) jerry = "JerryCan_GER1_2";
                    if (mo.TriggerDestroyRadius > 141) jerry = "JerryCan_GER1_3";
                    if (mo.TriggerDestroyRadius > 282) jerry = "JerryCan_GER1_5";
                    //if it's greater than 1410 we'll have to figure out something else to do, place several 1_5s around, probably.
                    //The JerryCan_GER1_1 (static - environment - jerrycan) covers a radius of 71 meters which is just enough to fill a 100 meter square (seen in FMB at full zoom) to all corners if placed in the center of the 100m square.
                    // JerryCan_GER1_2 covers 141m radius (covers 4 100m squares to the corners if placed in the center)
                    // JerryCan_GER1_3 covers 282m radius (covers 16 100m squares to the corners if placed in the center)
                    // JerryCan_GER1_5 covers 1410m radius (a 1km square to the corner if placed in the center)

                    Calcs.loadStatic(GamePlay, this, mo.Pos.x, mo.Pos.y, 2, "Stationary.Environment." + jerry);
                }
                if (mo.AutoFlak || (mo.AutoFlakIfPrimary && mo.IsPrimaryTarget)) MO_AutoFlakPlacement(mo);
            }
        }
    }

    //This needs to be run after each mission restart, after the missionobjectiveslist has been reloaded from disk
    //It accounts for the fact that MissionObjectivesList was saved to disk and now reloaded, but in the meanwhile we might have objected (for example) class MissionObjective
    //to include some new values.  They will all be set to FALSE or 0 or whatever if we just continue with the saved version.
    //On the other hand, we can't just reload the new version because we will lose current state of targets destroyed, targets for which
    //points have been awards, scouted objectives, etc. 
    //So we make a brand-new MissionObjectivesList but then we copy over any values that should persist.
    //Also adds any NEW objectives added in the .cs and removes any that were deleted
    public void updateMissionObjectivesListOnReload(MissionObjectives m_os)
    {
        Dictionary<string, MissionObjective> MissionObjectivesList_OLD = new Dictionary<string, MissionObjective>(MissionObjectivesList);
        MissionObjectivesList = new Dictionary<string, MissionObjective>();  //zero out the mission objectives list (otherwise when we run the routine below they will ADD to anything already there)
        //Console.WriteLine("#1" + (mission_objectives == null).ToString());
        m_os.RadarPositionTriggersSetup(); 
        //Console.WriteLine("#2");
        m_os.MissionObjectiveTriggersSetup();
        //Console.WriteLine("#3");
        MO_MissionObjectiveAirfieldsSetup(this, GamePlay, addNewOnly: false); //must do this after the Radar & Triggers setup, as it uses info from those objectives
        //Console.WriteLine("#4");

        foreach (string ID in MissionObjectivesList.Keys)
        {
            MissionObjectivesList[ID].IsPrimaryTarget = false;  //we reset this every time we load from disk, because later we'll read the primary objectives file & set this = true if it is a primary objective based on that.  We want the objectives .ini file, not this saved dictionary, to be the ultimate source of what is/is not a primary objective.  That way we can edit/change the .ini file as needed and it will affect the game later whenever it restarts.
            //Console.WriteLine("#5");
            if (!MissionObjectivesList_OLD.ContainsKey(ID)) continue;
            MissionObjective mo = MissionObjectivesList[ID];
            MissionObjective mo_old = MissionObjectivesList_OLD[ID];
            //Console.WriteLine("#6");
            mo.Destroyed = mo_old.Destroyed;
            mo.DestroyedPercent = mo_old.DestroyedPercent;
            mo.ObjectiveAchievedForPoints = mo_old.ObjectiveAchievedForPoints;
            mo.TimeToUndestroy_UTC = mo_old.TimeToUndestroy_UTC;
            mo.LastHitTime_UTC = mo_old.LastHitTime_UTC;
            mo.Scouted = mo_old.Scouted;
            mo.OrdnanceOnTarget_kg = mo_old.OrdnanceOnTarget_kg;
            mo.ObjectsDestroyed_num = mo_old.ObjectsDestroyed_num;

        }
    }



    public List<string> MO_ListAllPrimaryObjectives(int army)
    {
        var list = new List<string>();

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {
            MissionObjective mo = entry.Value;
            if (mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
            {
                list.Add(mo.ID);
                //Console.WriteLine("MLAPO " + mo.ID);
            }
        }
        return list;
    }


    public int MO_NumberPrimaryObjectivesComplete(int army)
    {

        int numComplete = 0;


        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {

            MissionObjective mo = entry.Value;
            if (mo.ObjectiveAchievedForPoints && mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
            {

                numComplete++;

            }
        }
        return numComplete;
    }

    int[] MO_numberprimaryobjectives = new int[3] { 0, 0, 0 };

    public int MO_NumberPrimaryObjectives(int army)
    {

        if (MO_numberprimaryobjectives[army] > 0) return MO_numberprimaryobjectives[army]; //Once we have figured this out, >0, it won't change

        int num = 0;

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {

            MissionObjective mo = entry.Value;
            if (mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
            {

                num++;

            }
        }
        MO_numberprimaryobjectives[army] = num;
        return num;
    }

    public double MO_PercentPrimaryObjectives(int army)
    {
        int npo = MO_NumberPrimaryObjectives(army);
        if (npo == 0) return 0;
        double x = MO_NumberPrimaryObjectivesComplete(army);
        return (x / (double)npo * (double)100.0);
    }
    public double MO_PrimaryObjectivesRemaining(int army)
    {
        int npo = MO_NumberPrimaryObjectives(army);
        double x = MO_NumberPrimaryObjectivesComplete(army);
        return (npo - x);
    }

    //Autosends generic scores needed by all objective types.
    //Send the score as 1/kill - this figures if they need to be multiplied by 100 or not
    public void MO_AddStatPoints(Player player, double score, List<int> ids) {
        ids.Add(798); //This is a value /100 like the others in ids

        int scoreX100_int = Convert.ToInt32(Math.Round(score * 100.0));
        int score_int = Convert.ToInt32(Math.Round(score));


        foreach (int id in ids)
        {
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddSessStat(player, id, scoreX100_int);
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddToMissionStat(player, id, scoreX100_int);
        }

        ids = new List<int> { 647, 648, 649, 794, 799 }; //These are all values NOT /100 - just done straight

        foreach (int id in ids)
        {
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddSessStat(player, id, score_int);
            if (TWCSaveIPlayerStat != null) TWCSaveIPlayerStat.StbSis_AddToMissionStat(player, id, score_int);
        }

        //return (Value[647] + Value[648] + Value[649] + Value[794]); - kill/any participation for aircraft / aa=artillery-tank / ships=naval / ground
        //NOT multiplied by 100

        //798 = total kill PERCENTAGE /100
        // 799 = num of total victories, NOT /100
    }

    public void MO_AddPlayerStatsScoresForObjectiveDestruction(Player player, string name, MissionObjective mo, double score)
    {
        //798-799 all types combined (total) -806/807 AA/Artillery/Tanks, 810/811 Naval/ship, 814/815 Other 
        //ground recon photos taken, 849 # of objectives photographed

        if (player == null) player = Calcs.PlayerFromName(this, name);

        //If the player isn't online we substitute in its place the 'stub' Player p1.  It only has the name & army of the player, but that is all that is needed by stats (thanks to 
        //clever hacking/tweaking of the stats routines).
        if (player == null)
        {
            aPlayer p1 = new aPlayer(name, mo.AttackingArmy);
            player = p1 as Player;
        }

        if (mo.MOObjectiveType == MO_ObjectiveType.AA            
            )
        {

            List<int> scoreIDs = new List<int> {  806, 807 };
            MO_AddStatPoints(player, score, scoreIDs);

        }
        else if (mo.MOObjectiveType == MO_ObjectiveType.Ship
                )
        {

            List<int> scoreIDs = new List<int> { 810, 811 };
            MO_AddStatPoints(player, score, scoreIDs);

        }
        else 
        /* covers these & a bunch more - all just classed as ground targets:
         *  (mo.MOObjectiveType == MO_ObjectiveType.Aircraft ||
             mo.MOObjectiveType == MO_ObjectiveType.ArmyBase ||
             mo.MOObjectiveType == MO_ObjectiveType.Dam ||
             mo.MOObjectiveType == MO_ObjectiveType.Dock
             //We're counting aircraft killed on the ground as ground targets, because they're not flying at the time

             )
             */
        {

            List<int> scoreIDs = new List<int> { 814, 815 };
            MO_AddStatPoints(player, score, scoreIDs);

        }

    }

    //Destroys the objective with the given ID and takes other related actions, such as 
    //adding points, displaying messages, reducing radar coverage
    public bool MO_DestroyObjective(string ID, bool active = true, double percentdestroyed = 0, double timetofix_s = 0 , DateTime? TimetoUndestroy_UTC = null)
    {
        try
        {
            Console.WriteLine(" MO_DestroyObjective1: {0} {1} {2} {3} {4}", ID, active, percentdestroyed, timetofix_s, TimetoUndestroy_UTC);
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

            if (OldObj.IsEnabled) {
                if (percentdestroyed > 0) OldObj.DestroyedPercent = percentdestroyed;
                if (timetofix_s>0) OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddSeconds(timetofix_s);
                if (TimetoUndestroy_UTC.HasValue) OldObj.TimeToUndestroy_UTC = TimetoUndestroy_UTC.Value;
                OldObj.LastHitTime_UTC = DateTime.UtcNow;
            }

            if (!OldObj.IsEnabled) return false; //This is disabled

            bool alreadyCounted = false;
            bool alreadyDestroyed = false;
            bool percentSpecified = (percentdestroyed > 0); //airports etc specify a specific percentage destroyed.  Everything else, we assume is 100%.

            Console.WriteLine(" MO_DestroyObjective1: {0} {1} {2} {3}", ID, alreadyCounted, alreadyDestroyed, percentSpecified);

            if (OldObj.ObjectiveAchievedForPoints) alreadyCounted = true; //The object has already been destroyed; don't need to do it again; we only give points/credit for destroying any given objective once
            if (OldObj.Destroyed) alreadyDestroyed = true; //The object has already been destroyed; don't need to do it again; we only give full points/credit for destroying any given objective once between times when the map is turned.  But we do give some lower point values for re-damaging it.


            OldObj.Destroyed = true;
            OldObj.ObjectiveAchievedForPoints = true;

            //So what can happen is, the objective was destroyed (and is still destroyed) after a previous mission, and/or it was destroyed and points received before.
            //But on new mission it looks like it's been rebuilt and the trigger is active again.  So you can bomb it again.
            //So bombing and objective again while its under repair still damages it, but we'll say it doesn't add the fully repair time again, but say 1/4th.

            //RADAR
            if (OldObj.MOObjectiveType == MO_ObjectiveType.Radar)
            {
                if (OldObj.OwnerArmy == 1) DestroyedRadar[(ArmiesE.Red)].Add(OldObj);
                if (OldObj.OwnerArmy == 2) DestroyedRadar[(ArmiesE.Blue)].Add(OldObj);
                if (!alreadyDestroyed)
                {
                    OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(OldObj.TimetoRepairIfDestroyed_hr);
                    if (percentSpecified) OldObj.DestroyedPercent = percentdestroyed; //Hopefully this is 100% but we leave it to the calling subroutine
                    else OldObj.DestroyedPercent = 1;
                }
                else
                {
                    if (percentSpecified) OldObj.DestroyedPercent += percentdestroyed/4; //we'll assume calling routine doesn't now it has already been destroyed so we should reduce accordingly
                    else OldObj.DestroyedPercent += 0.25; //we'll say re-destroying it adds 25% more damage & 1/4 as much more time to repair
                    OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(OldObj.TimetoRepairIfDestroyed_hr / 4.0);
                }
            } else
            //EVERYTHING ELSE (except airports, which are handled via MO_DestroyObjective_addpercentage, and radar, handled above
            {
                if (!alreadyDestroyed)
                {
                    OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(OldObj.TimetoRepairIfDestroyed_hr);
                    if (percentSpecified) OldObj.DestroyedPercent = percentdestroyed; //Hopefully this is 100% but we leave it to the calling subroutine
                    else OldObj.DestroyedPercent = 1;
                    Console.WriteLine(" MO_DestroyObjective1: not already destroyed");
                }
                else
                {
                    if (percentSpecified) OldObj.DestroyedPercent += percentdestroyed / 4; //we'll assume calling routine doesn't now it has already been destroyed so we should reduce accordingly
                    else OldObj.DestroyedPercent += 0.25; //we'll say re-destroying it adds 25% more damage & 1/4 as much more time to repair
                    OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(OldObj.TimetoRepairIfDestroyed_hr / 4.0);
                    Console.WriteLine(" MO_DestroyObjective1: already destroyed");
                }

            }

            //ADD TEAM POINTS
            if (!alreadyCounted)
            {
                Console.WriteLine(" MO_DestroyObjective1: not already count");
                if (OldObj.AttackingArmy == 1)
                {
                    MissionObjectivesCompletedString[ArmiesE.Red] += " - " + OldObj.Name;

                    Console.WriteLine("MO_DestroyObjective: Name " + OldObj.Name);
                    Console.WriteLine("MO_DestroyObjective: String " + MissionObjectivesCompletedString[ArmiesE.Red]);
                    MissionObjectiveScore[ArmiesE.Red] += OldObj.Points;
                }
                if (OldObj.AttackingArmy == 2)
                {
                    MissionObjectivesCompletedString[ArmiesE.Blue] += " - " + OldObj.Name;
                    Console.WriteLine("MO_DestroyObjective: Name " + OldObj.Name);
                    Console.WriteLine("MO_DestroyObjective: String " + MissionObjectivesCompletedString[ArmiesE.Blue]);

                    MissionObjectiveScore[ArmiesE.Blue] += OldObj.Points;
                }
            }
            else //Maybe due to error or ? the list of completed objectives doesn't include this; we'll just double check & add if necessary.
                 //It's confusing if you bomb & hit something and it doesn't show up on the list.

            {
                Console.WriteLine(" MO_DestroyObjective1: already counted");

                if (OldObj.AttackingArmy == 1)
                {
                    if (!MissionObjectivesCompletedString[ArmiesE.Red].Contains(OldObj.Name))
                    {
                        MissionObjectivesCompletedString[ArmiesE.Red] += " - " + OldObj.Name;
                    }

                }
                if (OldObj.AttackingArmy == 2)
                {
                    if (!MissionObjectivesCompletedString[ArmiesE.Blue].Contains(OldObj.Name))
                    {
                        MissionObjectivesCompletedString[ArmiesE.Blue] += " - " + OldObj.Name;
                    }

                }

                //ALREADY DESTROYED/COUNTED BUT WE CAN ADD PARTIAL POINTS IN MANY SITUATIONS
                //This has already been counted for map turning/primary target purposes but  has become undestroyed in the meanwhile
                //so it counts just as much as any other secondary target at this point:
                if (!alreadyDestroyed)
                {
                    Console.WriteLine(" MO_DestroyObjective1: already counted, not already destroyed (ie, probably destroyed earlier but now repaired");
                    //Full points for this; it's been repaired again.  But it doesn't count as a primary objective again.
                    if (percentSpecified)
                        MissionObjectiveScore[(ArmiesE)OldObj.AttackingArmy] += OldObj.Points * OldObj.DestroyedPercent; //scaled by the percentage killed 
                    else
                        MissionObjectiveScore[(ArmiesE)OldObj.AttackingArmy] += OldObj.Points;
                }
                else
                {
                    Console.WriteLine(" MO_DestroyObjective1: already counted, already destroyed");
                    //Here they are re-damaging it for the first time this session, which gets them a little extra points and extends the repair time some, but neither as much as if fully repaired
                    double pointsToAdd = 0;
                    if (percentSpecified) pointsToAdd = OldObj.Points * percentdestroyed / 4.0; //scaled by the percentage killed 
                    else pointsToAdd = OldObj.Points / 4.0;
                    if (pointsToAdd < 0.3) pointsToAdd = 0.3; //always give at least a little bit
                    MissionObjectiveScore[(ArmiesE)OldObj.AttackingArmy] += pointsToAdd;
                }
            }

            string pom = "";
            if (OldObj.IsPrimaryTarget && !alreadyCounted) pom = "Primary objective! ";

            string mes = ArmiesL[OldObj.AttackingArmy] + " destroyed " + OldObj.Name;
            if (OldObj.HUDMessage != null && OldObj.HUDMessage.Length > 0) mes = OldObj.HUDMessage;
            if (alreadyCounted || alreadyDestroyed) mes = ArmiesL[OldObj.AttackingArmy] + " damaged " + OldObj.Name;
            GamePlay.gpHUDLogCenter(mes);

            if (OldObj.LOGMessage != null && OldObj.LOGMessage.Length > 0) mes = OldObj.LOGMessage;
            if (alreadyCounted || alreadyDestroyed) mes = ArmiesL[OldObj.AttackingArmy] + " has further damaged " + OldObj.Name;
            Timeout(10, () =>
            {
                twcLogServer(null, mes, new object[] { });
                twcLogServer(null, pom + "All involved have received commendations and promotions.", new object[] { });
                MissionObjectivesList[ID] = OldObj;
            });

            MO_CheckObjectivesComplete();

            //now update related player scores

            //**add player points for any players on the side that destroyed this objective, who are within 10km of this spot
            List<String> mos = MO_ListSuggestedObjectives(null, OldObj.AttackingArmy, display: false); //Get the current list of MissionObjectivesSuggested[(ArmiesE)OldObj.AttackingArmy];

            double score = 5;        //Remember that stats kill scores are /100.  But the routines above will do that calculation; we just need the kill points here.
            if (alreadyDestroyed) score = score / 2;
            if (mos.Contains(ID) && !alreadyCounted ) score *= 2; //secondary objective
            if (OldObj.IsPrimaryTarget && !alreadyCounted) score *= 3;


            try
            {
                foreach (Player player in GamePlay.gpRemotePlayers())
                {
                    if (player.Place() == null) continue;

                    if (OldObj.AttackingArmy == player.Army() &&
                    player.Place() != null &&                    
                    Calcs.CalculatePointDistance(player.Place().Pos(), OldObj.Pos) < 10000)
                    {
                        MO_AddPlayerStatsScoresForObjectiveDestruction(player, player.Name(), OldObj, score);

                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("MO_Destroy error2: " + ex.Message); };

            try
            {
                //**add player stat points for any players who scouted/recon photo for this objective
                //It is max for first player to recon, then reduced by 50% for each succeeding player who also reconned it
                foreach (string playerName in OldObj.PlayersWhoScoutedNames.Keys)
                {
                   MO_AddPlayerStatsScoresForObjectiveDestruction(null, playerName, OldObj, score/(2^ OldObj.PlayersWhoScoutedNames[playerName]));
                }

            }
            catch (Exception ex) { Console.WriteLine("MO_Destroy error3: " + ex.Message); };



            return true;
        }
        catch (Exception ex) { Console.WriteLine("MO_Destroy error4: " + ex.Message); return true; };
    }

    //Adds additional destruction/percent/time out of commission to objects that are already destroyed
    //string ID, bool active = true, double percentdestroyed = 0, double timetofix_s = 0 , DateTime? TimetoUndestroy_UTC = null
    //Only used by airfields for now, when they are bombed additionally beyond 100%
    public bool MO_DestroyObjective_addTime(string ID, double percentdestroyed = 0, double timetofix_s = 0, DateTime? TimetoUndestroy_UTC = null,
        DateTime? TimeLastHit_UTC = null )
    {
        try
        {

            var OldObj = new MissionObjective(this);

            if (!MissionObjectivesList.TryGetValue(ID, out OldObj))
            {
                return false;
                //OldObj = new MissionObjective(msn);
            }

            //Add the extra time/destruction percent and/or update the time when the object will be undestroyed
            if (OldObj.IsEnabled)
            {
                //If we hit 200% destroyed it can count as a misison objective destroyed, again, for points.
                if (!OldObj.ObjectiveAchievedForPoints && percentdestroyed >= 2.0)
                {
                    MO_DestroyObjective(ID, true, percentdestroyed, timetofix_s, TimetoUndestroy_UTC);
                    return true;
                }
                if (percentdestroyed > 0) OldObj.DestroyedPercent = percentdestroyed;
                if (timetofix_s > 0) OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddSeconds(timetofix_s);
                if (TimetoUndestroy_UTC.HasValue) OldObj.TimeToUndestroy_UTC = TimetoUndestroy_UTC.Value;
                if (TimeLastHit_UTC.HasValue) OldObj.LastHitTime_UTC = TimeLastHit_UTC.Value;
                
                    
            }
            return true;

        }
        catch (Exception ex) { Console.WriteLine("MO_Destroy_addTime ERROR: " + ex.Message); return true; }
    }

    public void MO_CheckObjectivesComplete(int TestingOverrideArmy = 0)
    {


        //Turn the map by completing ALL primary objectives
        //OR most primary objectives (greater than specified percentage) plus reaching the higher point level required in that situation
        double bp = MO_PercentPrimaryObjectives((int)ArmiesE.Blue);

        if ((MissionObjectiveScore[ArmiesE.Blue] >= MO_PointsRequired[ArmiesE.Blue] && bp > 99)
            || bp >= MO_PercentPrimaryTargetsRequired[ArmiesE.Blue] && MissionObjectiveScore[ArmiesE.Blue] >= MO_PointsRequiredWithMissingPrimary[ArmiesE.Blue]
            || TestingOverrideArmy == 2)// Blue battle Success

        {
            //WriteResults_Out_File("2");
            Task.Run(() => WriteResults_Out_File("2"));
            Timeout(10, () =>
            {
                twcLogServer(null, "Blue has Successfully Turned the Map!!!", new object[] { });
                GamePlay.gpHUDLogCenter("Blue has Successfully Turned the Map!!!");
            });
            EndMission(70, "Blue");
        }





        double rp = MO_PercentPrimaryObjectives((int)ArmiesE.Red);
        if ((MissionObjectiveScore[ArmiesE.Red] >= MO_PointsRequired[ArmiesE.Red] && bp > 99)
            || rp >= MO_PercentPrimaryTargetsRequired[ArmiesE.Red] && MissionObjectiveScore[ArmiesE.Red] >= MO_PointsRequiredWithMissingPrimary[ArmiesE.Red]
            || TestingOverrideArmy == 1)// Red battle Success
        {
            //WriteResults_Out_File("1");
            Task.Run(() => WriteResults_Out_File("1"));
            Timeout(10, () =>
            {
                twcLogServer(null, "Red has Successfully Turned the Map!!!", new object[] { });
                GamePlay.gpHUDLogCenter("Red has Successfully Turned the Map!!!");
            });
            EndMission(70, "Red");
        }

        //Console.WriteLine("Figuring leaks:  {0} {1} {2} {3}", MissionObjectiveScore[ArmiesE.Red], MO_PointsRequired[ArmiesE.Red], bp, MO_PrimaryObjectivesRemaining( (int)ArmiesE.Red));


        if (MO_PrimaryObjectivesRemaining((int)ArmiesE.Red) == 2)
        {
            MO_IntelligenceLeakNearMissionEnd[ArmiesE.Blue] = "Intelligence sources indicate the enemy may be attacking sector " + MO_SectorOfRandomRemainingPrimaryObjective((int)ArmiesE.Red) + " soon";
            //Console.WriteLine("Figuring Leaks: " + MO_IntelligenceLeakNearMissionEnd[ArmiesE.Blue] + " " + MO_IntelligenceLeakNearMissionEnd[ArmiesE.Red]);
        }
        else if (MO_PrimaryObjectivesRemaining((int)ArmiesE.Red) == 1)
        {
            MO_IntelligenceLeakNearMissionEnd[ArmiesE.Blue] = "";
        }
    }

    public bool MO_IsPointInDestroyedRadarArea(Point3d p, int army)
    {
        var DR = new List<MissionObjective>();

        if (army == 1 || army == 2) DR = DestroyedRadar[(ArmiesE)army];
        else return false;

        foreach (MissionObjective value in DR)
        {
            for (int i = 0; i< 2; i++) {

                //So we take 2 circles starting at the radar location and moving towards heading 315 for Blue radars & 135 for Red radars
                //this works pretty well for the Channel map.
                double xadd = (double)i*value.RadarEffectiveRadius;
                double yadd = -(double)i * value.RadarEffectiveRadius;
                if (army == 2) {
                    xadd = -(double)i * value.RadarEffectiveRadius;
                    yadd = (double)i * value.RadarEffectiveRadius;
                }

                Point3d calcPos = new Point3d(value.Pos.x + xadd, value.Pos.y + yadd, p.z);

                double dist = Calcs.CalculatePointDistance(p, calcPos);
                //Console.WriteLine(value.Name + " " + army.ToString() + " " + dist.ToString("F0") + " " + value.RadarEffectiveRadius.ToString("F0") + " " + p.x.ToString("F0") + " " + p.y.ToString("F0") + " " + value.Pos.x.ToString("F0") + " " + value.Pos.y.ToString("F0"));
                if (dist < value.RadarEffectiveRadius) return true;
            }
        }
        return false;
    }

    //Figure out which radar areas are disabled depending on army, admin radar, which objectives have been destroyed, etc.
    //Returns TRUE if radar is enabled for that area/army, returns FALSE if radar is disabled/out for that area/army
    //radarArmy 1 = red, 2=blue, 0=admin, anything else is not allowed (in practice this will ignore any radar outages)
    public bool MO_isRadarEnabledByArea(Point3d pos, bool admin = false, int radarArmy = 0)
    {
        if (admin || radarArmy == 0 || radarArmy > 2) return true;

        //Console.WriteLine("#1 " + pos.x.ToString() + " " + pos.y.ToString() + " " + radarArmy.ToString());
        //WITHIN AN AREA WHERE THE RADAR HAS BEEN DESTROYED?
        //Finds if the point/ac is in an area with destroyed radar for either/both sides
        //if (mission_objectives != null) { if (MO_IsPointInDestroyedRadarArea(pos, radarArmy)) return false; }
        //else Console.WriteLine("#1.5111  Mission Objectives doesn't exist!!!!!");

        if (mission_objectives == null) Console.WriteLine("#1.51  Mission Objectives really doesn't exist!");
        else { if (MO_IsPointInDestroyedRadarArea(pos, radarArmy)) return false; }
		
	if (mission_objectives == null) Console.WriteLine("#1.52  Mission Objectives really doesn't exist!");
        //Console.WriteLine("#2.123 " + pos.x.ToString() + " " + radarArmy.ToString());

        //RED army special denied areas or areas the never have radar coverage
        if (radarArmy == 1)
        {

            if (pos.x <= 170000)  //for the portion of the map where x< 170000 (the westernmost part) we just draw a straight line across at y=130000 and count the distance from there
            {
                if (pos.y >= 145000) return true; //This is within the radar horizon, so we're just ignoring it here.
                double dist_m = 145000 - pos.y;  //we count the line y=145                                    000 as the radar horizon; the radar horizon is ~ 45km from the radar device.  So we add the 45km distance back in.
                double minHeight_m = MO_minHeightForRadarDetection(dist_m + 45000);
                //Console.WriteLine("Red Radar Area (west): {0:F0} {1:F0} {2:F0} {3:F0} {4:F0}", pos.x, pos.y, pos.z, dist_m, minHeight_m);
                if (pos.z < minHeight_m) return false;
                else return true;
            }
            else //for the easternmost  part of the map we draw line between point 1 & point 2, coordinates below, and count thedistance northwesterly from that line as the critical distance.
            {
                //(295347, 220011)
                //(170000,145000)


                Point2d p1 = new Point2d(170000, 145000); //we reverse the direction compared with the Blue line, so that points to the south/east of this line will return a positive distance.
                Point2d p2 = new Point2d(295347, 220011);  
                double dist_m = Calcs.PointToLineDistance(p1, p2, pos);  
                if (dist_m <= 0) return true; //on the "home base" side of the line, meaning that radar (and/or observer network) is always good/active
                double minHeight_m = MO_minHeightForRadarDetection(dist_m + 45000);
                //Console.WriteLine("Red Radar Area (east): {0:F0} {1:F0} {2:F0} {3:F0} {4:F0}", pos.x, pos.y, pos.z, dist_m, minHeight_m);
                if (pos.z > minHeight_m) return true; //we count the line defined by p1 & p2 as the radar horizon; the radar horizon is ~ 45km from the radar device.  So we add the 45km back in here.
                else return false; //the object is below the minimum height for radar detection.                
            }
        }

        //BLUE army special denied areas or areas that never have radar coverage
        //TODO: Could gradually remove low-level coverage the further from the stations we go (this is realistic)
        //Freya Radar had a range of 200km.  That is essentially enough to cover our entire map from the French coast.
        //Freya would have had a radar horizon of roughly 45 miles, meaning it could see everything down to a few meters elevation, out to that point.
        //After that the radar shadow (below the horizon) gradually rises in height.  the formula is
        // minimum height for detection = (distance - sqrt(2Xheight of radarXradius of earthX 4/3)^2/(2XRadius of earth X 4/3)
        // https://en.wikipedia.org/wiki/Radar_horizon
        else if (radarArmy == 2)
        {
            if (pos.x <=170000)  //for the portion of the map where x< 170000 (the westernmost part) we just draw a straight line across at y=130000 and count the distance from there
            {
                if (pos.y <= 130000) return true; //This is within the radar horizon, so we're just ignoring it here.
                double dist_m = pos.y - 130000;  //we count the line y=130000 as the radar horizon; the radar horizon is ~ 45km from the radar device.  So we add the 45km distance back in.
                double minHeight_m = MO_minHeightForRadarDetection(dist_m + 45000);
                //Console.WriteLine("Blue Radar Area (west): {0:F0} {1:F0} {2:F0} {3:F0} {4:F0}", pos.x, pos.y, pos.z, dist_m, minHeight_m);
                if (pos.z < minHeight_m) return false;
                else return true;
            } else //for the easternmost  part of the map we draw line between point 1 & point 2, coordinates below, and count thedistance northwesterly from that line as the critical distance.
            {
                //point 1: 227132, 243249
                //point 2: 170000, 130000 
                //(243249-130000)/(227132-170000) x + 
                //double a = 243249 - 139000;
                //double b = 170000 - 227132;
                //double c = b / -a * 130000 / 170000;

                Point2d p1 = new Point2d(227132, 243249);
                Point2d p2 = new Point2d(170000, 130000);
                double dist_m = Calcs.PointToLineDistance(p1, p2, pos);
                if (dist_m <= 0) return true; //Negative values mean it is on the side of the line where the radar stations are; ie, no radar shadow
                double minHeight_m = MO_minHeightForRadarDetection(dist_m + 45000);
                //Console.WriteLine("Blue Radar Area (east): {0:F0} {1:F0} {2:F0} {3:F0} {4:F0}", pos.x, pos.y, pos.z, dist_m, minHeight_m);
                if (pos.z > minHeight_m) return true; //we count the line defined by p1 & p2 as the radar horizon; the radar horizon is ~ 45km from the radar device.  So we add the 45km back in here.
                else return false; //the object is below the minimum height for radar detection.                
            }

            /*
            //BLUE radar only goes approx to English Coast.
            //This approximates that by taking a line between these points

            //  250000  313000  TopR of map, direct north of Hellfire Corner
            //  250000  236000  hellfire corner
            //  170000  194000  Eastbourne
            //  8000    180000  Edge of map near Bournemouth
            //TODO: We could make this more realistic in various ways, perhaps extending some high-level radar partially into UK or the like

            //if (pos.x > 170000 && pos.x <= 250000)
            //if (pos.x > 170000 && pos.x <= 225000)
            if (pos.x > 170000 && pos.x <= 190000)
            {
                //if ((pos.x - 170000) / 80000 * 42000 + 194000 < pos.y) return false;
                //if ((pos.x - 170000) / 80000 * 42000 + 219000 < pos.y) return false;
                if ((pos.x - 170000) / 80000 * 42000 + 250000 < pos.y) return false;
            }
            if (pos.x > 8000 && pos.x <= 170000)
            {
                //if ((pos.x - 8000) / 162000 * 14000 + 180000 < pos.y) return false;
                //if ((pos.x - 8000) / 162000 * 14000 + 205000 < pos.y) return false;
                if ((pos.x - 8000) / 162000 * 14000 + 236000 < pos.y) return false;
            }
            return true;
            */
        }
        return true;

    }
    public static double RADAR_RADIUS_OF_EARTH_m = 8494666.667;

    //(distance - sqrt(2Xheight of radarXradius of earthX 4/3)^2/(2XRadius of earth X 4/3)
    //Radar height 120 meters is a reasonable guess for both British & German radar.
    //They usually had bluffs of around 100 meters in height to place them on.
    //Freya radar was at least 20 meters tall above that; British masts were probably a bit taller but the practical difference is small, because ground clutter, details of topography, exact
    //implementation of radar etc all tend to be just as important
    //On the other hand, the 120 meter height was probably nicely operative over the ocean.  But what we're more worried about here is the
    //penetration into the other side's land area.  There, the effective height is more like 20 meters or maybe less, because both sides are about equally high and have various hills, bluffs, etc rising 100-120 meters or so above sea level.  So we're going with the 20 meter distance which is more realistic as to how hard it was then to pick things up close to teh ground (except for, over nice flat water).
    public double MO_minHeightForRadarDetection(double distance_m, double radarHeight_m=20)
    {
        return Math.Pow((distance_m - Math.Sqrt(2 * radarHeight_m * RADAR_RADIUS_OF_EARTH_m)),2) / 2 / RADAR_RADIUS_OF_EARTH_m;
    }

    public string ListRadarTargetDamage(Player player = null, int army = -1, bool all = false, bool display = true)
    {
        try
        {
            int count = 0;
            string returnmsg = "";
            double delay = 0.1;

            var DR = new List<MissionObjective>();

            List<int> armies;
            if (army == 1) armies = new List<int>() { 1 };
            else if (army == 2) armies = new List<int>() { 2 };
            else armies = new List<int>() { 1, 2 };

            foreach (int a in armies)
            {
                DR = DestroyedRadar[(ArmiesE)a];

                foreach (MissionObjective mo in DR)
                {
                    string msg2 = " (unknown)";
                    if (mo.TimeToUndestroy_UTC.HasValue)
                    {

                        double hrs = Math.Ceiling((mo.TimeToUndestroy_UTC.Value.Subtract(DateTime.UtcNow)).TotalDays * 2.0) / 2.0;
                        msg2 = "(" + hrs.ToString("F1") + " days)";
                    }
                    string msg = mo.Name + " destroyed " + msg2;
                    returnmsg += msg + "\n";

                    if (display)
                    {
                        delay += 0.02;
                        Timeout(delay, () => { twcLogServer(new Player[] { player }, msg, new object[] { }); });
                    }

                    count++;
                }
            }
            if (count == 0)
            {
                string msg = "No radar currently damaged or destroyed";
                if (display) twcLogServer(new Player[] { player }, msg, new object[] { });
                returnmsg = ""; //In case of display == false we just don't return any message at all, allowing this bit to simply be omitted
            }

            return returnmsg;
        }
        catch (Exception ex) { Console.WriteLine("MO_RadarList Error: " + ex.ToString()); return ""; };
    }
    //FOR AIACTOR TARGETS DESTROYED
    public void MO_HandlePointAreaObjectives(AiActor actor)
    {

        // Console.WriteLine("AreaPoint Actor: {0}, {1}, {2}", actor.Name(), actor.Army(), (actor as AiCart).InternalTypeName());
        //TODO: Could easily combine this with the GroundStationaries route, 95% the same
        foreach (string ID in MissionObjectivesList.Keys)
        {
            if (actor == null || actor.Army() < 1 || actor.Army() > 2) return;

            MissionObjective mo = MissionObjectivesList[ID];
            if (actor.Army() != mo.OwnerArmy) continue;
            if (mo.MOTriggerType != MO_TriggerType.PointArea) continue;
            double dist = Calcs.CalculatePointDistance(actor.Pos(), mo.Pos);            
            if (dist > mo.radius) continue;

            string type = "";
            if ((actor as AiCart) != null && (actor as AiCart).InternalTypeName() != null) type = (actor as AiCart).InternalTypeName();

            string groundType = "";
            if (actor as AiGroundActor != null) groundType = (actor as AiGroundActor).Type().ToString();



            mo.LastHitTime_UTC = DateTime.UtcNow;

            double oldOONT_num = mo.ObjectsDestroyed_num;

            //Damage bonuses for various specific types.  This is hard to do systematically.
            //type - should contain the string found in the .mis file for objects, such as Environment.TelegaBallon_UK1
            //groundType is the enum list  of type maddox.game.world.AiGroundActorType which has a bunch of the things listed below plus more

            double damageCount = 1;
            if (type.Contains("Barge")) damageCount = 7;
            else if (type.Contains("Tanker") || type.Contains("Cruiser")) damageCount = 12;
            else if (groundType.Contains("ShipCarrier") || groundType.Contains("ShipCruiser") || groundType.Contains("ShipDestroyer") || groundType.Contains("warship")) damageCount = 20;
            else if (groundType.ToLower().Contains("ship")) damageCount = 9;
            else if (groundType.Contains("AAGun") || groundType.Contains("Artillery") || groundType.Contains("Tank") || groundType.Contains("Armored") || groundType.Contains("Amphibian")) damageCount = 4;
            else if (groundType.ToLower().Contains("truck") || groundType.ToLower().Contains("tractor") || groundType.ToLower().Contains("trailer") ) damageCount = 2;
            else if (groundType.ToLower().Contains("radar")) damageCount = 6;
            else if (groundType.ToLower().Contains("plane")) damageCount = 3;
            else if (groundType.ToLower().Contains("bridge")) damageCount = 10;


            if (dist > mo.TriggerDestroyRadius) damageCount *= 0.333; //less damage effectiveness if inside radius but outside triggerradius
            mo.ObjectsDestroyed_num += damageCount;

            double divisor = 1;
            if (mo.OrdnanceRequiredToTrigger_kg > 0 && mo.ObjectsRequiredToTrigger_num > 0) divisor = 2; //If both objects & KG required to trigger, we give each of them 50% of the required 100% for destruction.

            //So this is a bit complicated, but percentage destroyed due to ordinance & KG can't go above 50% UNLESS the other type of objective is completed also.
            //The prevents, ie, showing 125% destroyed purely by bombing KG, when 0 of the 10 required targets have been killed.
            //In that case it will show 50% complete, not 125%
            //Now, once both sides are complete (KG & objects) then additional KG or objects killed will add more percent
            double dst_pc_ord = 0;
            if (mo.OrdnanceRequiredToTrigger_kg > 0) dst_pc_ord = mo.OrdnanceOnTarget_kg / mo.OrdnanceRequiredToTrigger_kg / divisor;
            double dst_pc_obj = 0;
            if (mo.ObjectsRequiredToTrigger_num > 0) dst_pc_obj = mo.ObjectsDestroyed_num / mo.ObjectsRequiredToTrigger_num / divisor;

            if (dst_pc_obj > 0.5 && mo.OrdnanceRequiredToTrigger_kg > mo.OrdnanceOnTarget_kg) dst_pc_obj = 0.5;
            if (dst_pc_ord > 0.5 && mo.ObjectsDestroyed_num < mo.ObjectsRequiredToTrigger_num) dst_pc_ord = 0.5;

            double oldDestroyedPercent = mo.DestroyedPercent;
            mo.DestroyedPercent = dst_pc_obj + dst_pc_ord;

            Console.WriteLine("AreaPoint Actor: {0:F0}% objects, {1:F0}% KG, {2:F0}% Tot, {3:F0} KG KGreq: {4:F0} Numreq: {5:F0}", dst_pc_obj * 100, dst_pc_ord * 100, mo.DestroyedPercent * 100, mo.OrdnanceOnTarget_kg, mo.OrdnanceRequiredToTrigger_kg, mo.ObjectsRequiredToTrigger_num);


            //if (mo.ObjectsDestroyed_num > mo.ObjectsRequiredToTrigger_num)
            if (oldOONT_num < mo.ObjectsRequiredToTrigger_num && mo.ObjectsDestroyed_num >= mo.ObjectsRequiredToTrigger_num && mo.OrdnanceOnTarget_kg >= mo.OrdnanceRequiredToTrigger_kg)
                MO_DestroyObjective(ID, true, percentdestroyed: mo.DestroyedPercent, timetofix_s: mo.TimetoRepairIfDestroyed_hr * 3600); // 1 because, we always get 1 object here. //hnMO_DestroyObjective(ID, true, percentdestroyed: mo.DestroyedPercent, timetofix_s: mo.TimetoRepairIfDestroyed_hr * 3600); //Note - MUST use >= here as it covers the case where ordnanceKG and/or object_numrequired = 0
            else if (mo.DestroyedPercent > 100 && mo.ObjectsRequiredToTrigger_num > 0 )
            {
                mo.DestroyedPercent = 1 + damageCount / mo.ObjectsRequiredToTrigger_num / 4.0 / divisor;  // so if it's above 100% destroyed additional bombs/ordnance still add more "dead points" but not as many/ assuming much of it is already destroy, so like 1/4 as much.
                double timeToFix_hr = (mo.TimetoRepairIfDestroyed_hr * mo.DestroyedPercent);
                if (mo.TimeToUndestroy_UTC.HasValue) mo.TimeToUndestroy_UTC.Value.AddHours(mo.TimetoRepairIfDestroyed_hr * damageCount / mo.ObjectsRequiredToTrigger_num / 4.0);  //just add time proportional to how many objects required to kill it 100%.  But divided by 4 since we are discounting the destruction
                else mo.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(mo.TimetoRepairIfDestroyed_hr * mo.DestroyedPercent);
                MO_DestroyObjective_addTime(ID, percentdestroyed: mo.DestroyedPercent);
            }
            else if (Math.Floor(oldDestroyedPercent*100.0 / 25.0) % 2 != Math.Floor(mo.DestroyedPercent*100.0 / 25.0) % 2) //if crossing threshold @ 25, 50, 75% give a message with status update of objective
            {
                twcLogServer(null, "{0} damaged: {1}% destroyed, {2} items damaged, {3} kg on target", new object[] { mo.Name, (mo.DestroyedPercent*100).ToString("F0"), mo.ObjectsDestroyed_num.ToString("F0"), mo.OrdnanceOnTarget_kg.ToString("F0") });
            }
            ///!!!!!!!!!!!!!!!!!!! might still need ot do more things here, check the missionobjectives class.
            ///
            /*
             * 
           if (percentdestroyed > 0) OldObj.DestroyedPercent = percentdestroyed;
                if (timetofix_s > 0) OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddSeconds(timetofix_s);
                if (TimetoUndestroy_UTC.HasValue) OldObj.TimeToUndestroy_UTC = TimetoUndestroy_UTC.Value;
                if (TimeLastHit_UTC.HasValue) OldObj.LastHitTime_UTC = TimeLastHit_UTC.Value;    
         */

        }


    }

    //FOR STATIONARY TARGETS DESTROYED ***OR*** BUILDINGS
    //if st==null then it is a BUILDING instead of a groundstationary object
    public void MO_HandlePointAreaObjectives(GroundStationary st, AiDamageInitiator initiator, string title = null, Point3d? BuildingPos = null)
    {
        Point3d pos = new Point3d(-1, -1, -1);       

        if (st != null) pos = st.pos;
        else if (BuildingPos.HasValue) pos = BuildingPos.Value;
        else return; //both are null, so can't really do anything here as we don't have a position

        string name = "";
        if (st != null) name = st.Name;
        else if (title != null) name = title;

        foreach (string ID in MissionObjectivesList.Keys)
        {
            MissionObjective mo = MissionObjectivesList[ID];
            if (mo.MOTriggerType != MO_TriggerType.PointArea) continue;
            double dist = Calcs.CalculatePointDistance(pos, mo.Pos);
            if (dist > mo.radius) continue;

            mo.LastHitTime_UTC = DateTime.UtcNow;

            double oldOONT_num = mo.ObjectsDestroyed_num;
            
            double damageCount = 1;
            if (name.Contains("Barge") || name.Contains("Tanker") || name.Contains("Cruiser")) damageCount = 10;
            
            if (dist > mo.TriggerDestroyRadius) damageCount *= 0.333; //less damage effectiveness if inside radius but outside triggerradius
            mo.ObjectsDestroyed_num += damageCount;

            double divisor = 1;
            if (mo.OrdnanceRequiredToTrigger_kg > 0 && mo.ObjectsRequiredToTrigger_num > 0) divisor = 2; //If both objects & KG required to trigger, we give each of them 50% of the required 100% for destruction.

            //So this is a bit complicated, but percentage destroyed due to ordinance & KG can't go above 50% UNLESS the other type of objective is completed also.
            //The prevents, ie, showing 125% destroyed purely by bombing KG, when 0 of the 10 required targets have been killed.
            //In that case it will show 50% complete, not 125%
            //Now, once both sides are complete (KG & objects) then additional KG or objects killed will add more percent
            double dst_pc_ord = 0;
            if (mo.OrdnanceRequiredToTrigger_kg > 0) dst_pc_ord = mo.OrdnanceOnTarget_kg / mo.OrdnanceRequiredToTrigger_kg / divisor;
            double dst_pc_obj = 0;
            if (mo.ObjectsRequiredToTrigger_num > 0) dst_pc_obj = mo.ObjectsDestroyed_num / mo.ObjectsRequiredToTrigger_num / divisor;

            if (dst_pc_obj > 0.5 && mo.OrdnanceRequiredToTrigger_kg > mo.OrdnanceOnTarget_kg) dst_pc_obj = 0.5;
            if (dst_pc_ord > 0.5 && mo.ObjectsDestroyed_num < mo.ObjectsRequiredToTrigger_num) dst_pc_ord = 0.5;

            double oldDestroyedPercent = mo.DestroyedPercent;
            mo.DestroyedPercent = dst_pc_obj + dst_pc_ord;

            Console.WriteLine("AreaPoint Stationary: {0:F0}% objects, {1:F0}% KG, {2:F0}% Tot, {3:F0} KG KGreq: {4:F0} Numreq: {5:F0}", dst_pc_obj * 100, dst_pc_ord * 100, mo.DestroyedPercent * 100, mo.OrdnanceOnTarget_kg, mo.OrdnanceRequiredToTrigger_kg, mo.ObjectsRequiredToTrigger_num);
            if (st!=null) Console.WriteLine("AreaPoint Stationary: {0}, {1}, {2}, {3}", st.Category, st.Name, st.Type, st.Title);
            else Console.WriteLine("AreaPoint Building: {0}", name);


            //if (mo.ObjectsDestroyed_num > mo.ObjectsRequiredToTrigger_num)
            if (oldOONT_num < mo.ObjectsRequiredToTrigger_num && mo.ObjectsDestroyed_num >= mo.ObjectsRequiredToTrigger_num && mo.OrdnanceOnTarget_kg >= mo.OrdnanceRequiredToTrigger_kg)
                MO_DestroyObjective(ID, true, percentdestroyed: mo.DestroyedPercent, timetofix_s: mo.TimetoRepairIfDestroyed_hr * 3600); // 1 because, we always get 1 object here. //hnMO_DestroyObjective(ID, true, percentdestroyed: mo.DestroyedPercent, timetofix_s: mo.TimetoRepairIfDestroyed_hr * 3600); //Note - MUST use >= here as it covers the case where ordnanceKG and/or object_numrequired = 0
            else if (mo.DestroyedPercent > 100 && mo.ObjectsRequiredToTrigger_num > 0)
            {
                mo.DestroyedPercent = 1 + damageCount / mo.ObjectsRequiredToTrigger_num / 4.0 / divisor;  // so if it's above 100% destroyed additional bombs/ordnance still add more "dead points" but not as many/ assuming much of it is already destroy, so like 1/4 as much.
                double timeToFix_hr = (mo.TimetoRepairIfDestroyed_hr * mo.DestroyedPercent);
                if (mo.TimeToUndestroy_UTC.HasValue) mo.TimeToUndestroy_UTC.Value.AddHours(mo.TimetoRepairIfDestroyed_hr * damageCount / mo.ObjectsRequiredToTrigger_num / 4.0);  //just add time proportional to how many objects required to kill it 100%.  But divided by 4 since we are discounting the destruction
                else mo.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(mo.TimetoRepairIfDestroyed_hr * mo.DestroyedPercent);
                MO_DestroyObjective_addTime(ID, percentdestroyed: mo.DestroyedPercent);

            }
            else if (Math.Floor(oldDestroyedPercent * 100.0 / 25.0) % 2 != Math.Floor(mo.DestroyedPercent * 100.0/ 25.0) % 2) //if crossing threshold @ 25, 50, 75% give a message with status update of objective
            {
                twcLogServer(null, "{0} damaged: {1}% destroyed, {2} items damaged, {3} kg on target", new object[] { mo.Name, (mo.DestroyedPercent * 100).ToString("F0"), mo.ObjectsDestroyed_num.ToString("F0"), mo.OrdnanceOnTarget_kg.ToString("F0") });
            }

                ///!!!!!!!!!!!!!!!!!!! might still need ot do more things here, check the missionobjectives class.
                ///
                /*
                 * 
               if (percentdestroyed > 0) OldObj.DestroyedPercent = percentdestroyed;
                    if (timetofix_s > 0) OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddSeconds(timetofix_s);
                    if (TimetoUndestroy_UTC.HasValue) OldObj.TimeToUndestroy_UTC = TimetoUndestroy_UTC.Value;
                    if (TimeLastHit_UTC.HasValue) OldObj.LastHitTime_UTC = TimeLastHit_UTC.Value;    
             */

            }


    }


    //FOR BOMB EXPLOSIONS/Total up ordnance
    public void MO_HandlePointAreaObjectives(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator)
    {
        //maddox.game.LandTypes landType = GamePlay.gpLandType(pos.x, pos.y);

        //For now, all things we handle below are on land, so if the land type is water we just
        //get out of here immediately
        //if (landType == maddox.game.LandTypes.WATER) return;  //Ok, doing ships & water type targets this doesn't really work.  So just X it out.

        //twcLogServer(null, "bombe 6", null);

        //So, if the damager is AI and damaging their own land area, we're not going to count it.
        //Reason is, some artillery continually hits the ground in the area where they're firing.
        int terr = GamePlay.gpFrontArmy(pos.x, pos.y);
        if (initiator == null || initiator.Player == null || initiator.Player.Name() == null) //It's AI
        {
            if (initiator.Actor != null && initiator.Actor.Army() == terr) return; //It's the same army as the territory that has been damaged
        }


            bool blenheim = false;
        AiAircraft aircraft = initiator.Actor as AiAircraft;
        string acType = Calcs.GetAircraftType(aircraft);
        if (acType.Contains("Blenheim")) blenheim = true;

        double aircraftCorrection = 1;

        if (acType.Contains("Blenheim")) aircraftCorrection = 4;
        if (acType.Contains("He-111")) aircraftCorrection = 1.5;
        if (acType.Contains("BR-20")) aircraftCorrection = 2;

        mass_kg = mass_kg * aircraftCorrection;

        foreach (string ID in MissionObjectivesList.Keys)
        {
            MissionObjective mo = MissionObjectivesList[ID];
            if (mo.MOTriggerType != MO_TriggerType.PointArea) continue;
            //if (mo.OrdnanceRequiredToTrigger_kg == 0) continue; //0 means, don't use ordinance KG to determine destruction [nevermind, we can still track it even though not actively using it.  If 0
            double dist = Calcs.CalculatePointDistance(pos, mo.Pos);
            if ( dist > mo.radius) continue;
            double oldOONTkg = mo.OrdnanceOnTarget_kg;


            if (dist > mo.TriggerDestroyRadius) mass_kg *= 0.333; //less damage effectiveness if inside radius but outside triggerradius
            mo.OrdnanceOnTarget_kg += mass_kg;
            mo.LastHitTime_UTC = DateTime.UtcNow;

            double divisor = 1;
            if (mo.OrdnanceRequiredToTrigger_kg > 0  && mo.ObjectsRequiredToTrigger_num > 0) divisor = 2; //If both objects & KG required to trigger, we give each of them 50% of the required 100% for destruction.

            //So this is a bit complicated, but percentage destroyed due to ordinance & KG can't go above 50% UNLESS the other type of objective is completed also.
            //The prevents, ie, showing 125% destroyed purely by bombing KG, when 0 of the 10 required targets have been killed.
            //In that case it will show 50% complete, not 125%
            //Now, once both sides are complete (KG & objects) then additional KG or objects killed will add more percent
            double dst_pc_ord = 0;
            if (mo.OrdnanceRequiredToTrigger_kg> 0) dst_pc_ord = mo.OrdnanceOnTarget_kg / mo.OrdnanceRequiredToTrigger_kg / divisor;
            double dst_pc_obj = 0;
            if (mo.ObjectsRequiredToTrigger_num > 0) dst_pc_obj= mo.ObjectsDestroyed_num / mo.ObjectsRequiredToTrigger_num / divisor;

            if (dst_pc_obj > 0.5 && mo.OrdnanceRequiredToTrigger_kg > mo.OrdnanceOnTarget_kg) dst_pc_obj = 0.5;
            if (dst_pc_ord > 0.5 && mo.ObjectsDestroyed_num < mo.ObjectsRequiredToTrigger_num) dst_pc_ord = 0.5;

            double oldDestroyedPercent = mo.DestroyedPercent;
            mo.DestroyedPercent = dst_pc_obj + dst_pc_ord;

            Console.WriteLine("AreaPoint Ordnance: {0:F0}% objects, {1:F0}% KG, {2:F0}% Tot, {3:F0} KG KGreq: {4:F0} Numreq: {5:F0}", dst_pc_obj * 100, dst_pc_ord * 100, mo.DestroyedPercent * 100, mo.OrdnanceOnTarget_kg, mo.OrdnanceRequiredToTrigger_kg, mo.ObjectsRequiredToTrigger_num);

            if (oldOONTkg < mo.OrdnanceRequiredToTrigger_kg && mo.OrdnanceOnTarget_kg >= mo.OrdnanceRequiredToTrigger_kg && mo.ObjectsDestroyed_num >= mo.ObjectsRequiredToTrigger_num ) MO_DestroyObjective(ID, true, percentdestroyed: mo.DestroyedPercent, timetofix_s: mo.TimetoRepairIfDestroyed_hr*3600); //Note - MUST use >= here as it covers the case where ordnanceKG and/or object_numrequired = 0
            else if (mo.DestroyedPercent > 100 && mo.OrdnanceRequiredToTrigger_kg > 0)
            {                
                mo.DestroyedPercent = 1 + (mo.OrdnanceOnTarget_kg - mo.OrdnanceRequiredToTrigger_kg) / mo.OrdnanceRequiredToTrigger_kg / 4.0;  // so if it's above 100% destroyed additional bombs/ordnance still add more "dead points" but not as many/ assuming much of it is already destroy, so like 1/4 as much.
                double timeToFix_hr = (mo.TimetoRepairIfDestroyed_hr * mo.DestroyedPercent);
                if (mo.TimeToUndestroy_UTC.HasValue) mo.TimeToUndestroy_UTC.Value.AddHours(mo.TimetoRepairIfDestroyed_hr * mass_kg / mo.OrdnanceRequiredToTrigger_kg / 4.0);  //just add time proportional to this particular bomb's kg.  But divided by 4 since we are discounting the destruction
                else mo.TimeToUndestroy_UTC = DateTime.UtcNow.AddHours(mo.TimetoRepairIfDestroyed_hr * mo.DestroyedPercent);
                MO_DestroyObjective_addTime(ID, percentdestroyed: mo.DestroyedPercent);
            } else if (Math.Floor(oldDestroyedPercent * 100.0 / 25.0) % 2 != Math.Floor(mo.DestroyedPercent * 100.0 / 25.0) % 2) //if crossing threshold @ 25, 50, 75% give a message with status update of objective
            {
                twcLogServer(null, "{0} damaged: {1}% destroyed, {2} items damaged, {3} kg on target", new object[] { mo.Name, (mo.DestroyedPercent * 100).ToString("F0"), mo.ObjectsDestroyed_num.ToString("F0"), mo.OrdnanceOnTarget_kg.ToString("F0") });
            }
            ///!!!!!!!!!!!!!!!!!!! might still need ot do more things here, check the missionobjectives class.
            ///
            /*
             * 
           if (percentdestroyed > 0) OldObj.DestroyedPercent = percentdestroyed;
                if (timetofix_s > 0) OldObj.TimeToUndestroy_UTC = DateTime.UtcNow.AddSeconds(timetofix_s);
                if (TimetoUndestroy_UTC.HasValue) OldObj.TimeToUndestroy_UTC = TimetoUndestroy_UTC.Value;
                if (TimeLastHit_UTC.HasValue) OldObj.LastHitTime_UTC = TimeLastHit_UTC.Value;    
         */

        }


    }

    /******************************************************************************************************************** 
    * ****END****MISSION OBJECTIVES CLASSES & METHODS
    * **********************************************************************************************************************/



    //return TRUE if AI spawns should be stopped bec of too many players
    //FALSE if AI/trigger missions can continue
    public bool stopAI()
    {

        int nump = Calcs.gpNumberOfPlayers(GamePlay);
        Console.WriteLine("stopAI: " + nump.ToString() + " players currently online");
        if (nump > 60 || (nump > 40 && random.NextDouble() > 0.5))
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
     *    action1 ASpawnGroup 1 BoB_LW_KuFlGr_706.03    <--- WILL RUN
     *    action2 ASpawnGroup 0 BoB_LW_KuFlGr_706.04    <--- WILL ***NOT*** RUN!!!!
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
     * Also Timers should be named as timers with the time to launch in the title to avoid confusion
     * 
     * TGroundDestroyed type triggers ONLY DETECT OBJECTS KILLED if they are labelled as de or gb.  The nn objects are simply not counted.
     * 
     * 
     * ****************************************************************************************************************/
    //  First triggers are convoys*************************************************
    public override void OnTrigger(int missionNumber, string shortName, bool active)
    //public void bartOnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);
        Console.WriteLine("OnTrigger: " + shortName + " " + missionNumber.ToString() + " Active: " + active.ToString());

        bool res = MO_DestroyObjective(shortName, active);

        //Console.WriteLine("OnTrigger: " + shortName + " Active: " + active.ToString() + " MO_DestroyObjective result: " + res.ToString());


        //Console.WriteLine("OnTrigger: Now doing ActionTriggers: " + shortName + " Active: " + active.ToString() + " !stopAI()=" + (!stopAI()).ToString() + " zonedef: " + ("zonedefenseblue1".Equals(shortName) && active && !stopAI()).ToString());  

        ///Timed raids into enemy territory////////////////////////// using the action part of the trigger//

        //if you want any patrols etc to continue running even when the server is full of live players, just remove the  && !stopAI() of that trigger
        //stopAI will slow down AI patrols with 40 players online and stop them at 50 (adjustable above).
		//Begin convoys for both sides with pass thru zones
/*	
	
"Rouen_2_Lehavre",				       "Panzer_2_Calais",			
"Rouen_2_Brombos",				       "Opel_BlitzFuel1",			
"Dieppe_2_Lehavre",				       
"Bernay_2_Hornfleur",			       "Dunkirk_2_GrandFort",		
"Beaumont_2_Forges",			       "Onkel_Albert_Staff_Car",	
"Lehavre_2_Forges",				       "LeTreport_fuel_Convoy",	
"FuelBeauvis_2_Nuefchatel",		       "Abbeville_2_PoixDePicard",	
"Huate_from_Dieppe",			       "Amiens_2_MontDidiear",		
"Clermont_2_Dieppe",			       "Arras_2_WallyBeaucamp",	
"Opel_Blitz_fuel_1",			       "GER_Fuel_Column_1",		
"Rouen_2_Dieppe2",				       "GER_Fuel_Column_2",		
"Stelling_to_Dunkirk		       "GER_Fuel_Column_3",		
"General_Tiny_Barber",			
"Rouen_Fuel_Convoy_2Dieppe",	
"Les_Andeleys_2_Brombos",		
"Axis_War_Plans",				
	
*/		
        if ("Convoy_Timer1_10min".Equals(shortName) && active )//Ai activity reguardless of number of players
		
        {
            AiAction action = GamePlay.gpGetAction("Rouen_2_Lehavre");//Chief_0


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, " Rouen_2_Lehavre Convoy Launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
		}
        else if ("Convoy_Timer2_11min".Equals(shortName) && active )//Ai activity reguardless of number of players
		
        {
            AiAction action = GamePlay.gpGetAction("Panzer_2_Calais");//Chief_0


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(15, () => sendScreenMessageTo(1, " Panzer_2_Calais Convoy Launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
		}		
        else if ("Timer3_30min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Rouen_2_Brombos");//Chief_1


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Rouen_2_Brombos convoy launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer4_31min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Opel_Blitz_fuel_1 ");//Chief_10
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Abbeville .Ligescourt Cramont area refueling", null));			
          //  Timeout(3600, () => sendScreenMessageTo(2, "Rouen Fighters restset...Check time", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }		
        else if ("Timer5_45min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Dieppe_2_Lehavre");//Chief_2


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Dieppe to Le Havre Convoy launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Beckton_Sewage".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
        //    AiAction action = GamePlay.gpGetAction("Dieppe_2_Lehavre");//Chief_2


       //     if (action != null)
       //     {
       //         action.Do();
       //         Console.WriteLine("Triggered action " + action.Name);
       //     }
            Timeout(10, () => sendScreenMessageTo(2, "Beckton Sewage Treatment facility Destroyed", null));
            Timeout(20, () => sendScreenMessageTo(2, "Crap everywhere", null));
            Timeout(10, () => sendScreenMessageTo(1, "Beckton Sewage Treatment facility Destroyed,", null));
	        Timeout(20, () => sendScreenMessageTo(1, "Crap everywhere 3 sheets in the wind", null));		
		  GamePlay.gpGetTrigger(shortName).Enable = false;
		}
        else if ("D_Sewage".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
        //    AiAction action = GamePlay.gpGetAction("Dieppe_2_Lehavre");//Chief_2


       //     if (action != null)
       //     {
        //        action.Do();
        //        Console.WriteLine("Triggered action " + action.Name); 
       //     }
            Timeout(10, () => sendScreenMessageTo(2, "Sewage Treatment facility Destroyed in Dunkirk", null));
            Timeout(20, () => sendScreenMessageTo(2, "Crap everywhere ", null));
            Timeout(10, () => sendScreenMessageTo(1, "Allied Effort results in destruction of a sewage plant in Dunkirk,", null));
	        Timeout(20, () => sendScreenMessageTo(1, "Crap everywhere", null));		
		  GamePlay.gpGetTrigger(shortName).Enable = false;
		}		
        else if ("Timer6_46min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Dunkirk_2_GrandFort");//Chief_12


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Dunkirk_2_GrandFort Convoy launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
          //  GamePlay.gpGetTrigger(shortName).Enable = false;
        }		
        else if ("Timer7_60min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Bernay_2_Hornfleur");//Chief_3


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
           Timeout(10, () => sendScreenMessageTo(1, "Bernay_2_Hornfleur Convoy launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer8_61min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Onkel_Albert_Staff_Car");//Chief_14


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(40, () => sendScreenMessageTo(1, "German General on Inspection", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer9_90min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Beaumont_2_Forges");//Chief_4


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Beaumont_2_Forges Convoy launched", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }		
        else if ("Timer10_91min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("LeTreport_fuel_Convoy");//Chief_17


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "LeTreport_fuel_Convoy launched", null));
        //    Timeout(10, () => sendScreenMessageTo(1, "Friendly Resupply Convoy Spotted AC 6 headed to Carquebut area Someone check for vehicle spawn please", null));
            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }		
        else if ("Timer11_95min".Equals(shortName) && active && !stopAI())// Trigger F1e launches escorts to go with Do 17s from trigger F1 above
        {
            AiAction action = GamePlay.gpGetAction("Lehavre_2_Forges");//Chief_5


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
       Timeout(10, () => sendScreenMessageTo(2, "Critical Fuel Supplies Lehavre_2_Forges", null));
       Timeout(10, () => sendScreenMessageTo(1, "Friendly  Fuel Supplies Flowing Lehavre_2_Forges ", null));	   
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer12_96min".Equals(shortName) && active && !stopAI())// Trigger F1e launches escorts to go with Do 17s from trigger F1 above
        {
            AiAction action = GamePlay.gpGetAction("Abbeville_2_PoixDePicard");//Chief_19


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
       Timeout(10, () => sendScreenMessageTo(1, "Enemy Tanks Spotted Abbeville to Poix Nord", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer13_120min".Equals(shortName) && active && !stopAI())// Trigger F1c launches escorts to go with escorts from trigger F1e above
        {
            AiAction action = GamePlay.gpGetAction("FuelBeauvis_2_Nuefchatel");//Chief_6


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
			 Timeout(10, () => sendScreenMessageTo(1, "Fuel Supplies Beauvis to Nuefchatel", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer14_121min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Amiens_2_MontDidiear");//Chief_20
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Supplies from Amiens to Mt. Didiear.", null));
          //  Timeout(600, () => sendScreenMessageTo(2, "Wellingtons have been spotted over Dymchurch at 6000m heading east!!!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer15_125min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Huate_from_Dieppe");//Chief_7
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Supplies from Huate_from_Dieppe.", null)); 
         //    Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @ 20K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
		}	
        else if ("Timer16_126min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Arras_2_WallyBeaucamp");//Chief_21
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
 
            Timeout(60, () => sendScreenMessageTo(1, "Arras_2_WallyBeaucamp Supplies moving now!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;			
        }
        else if ("Timer17_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Clermont_2_Dieppe");//Chief_8
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(60, () => sendScreenMessageTo(2, "Clermont_2_Dieppe fuel supplies Spotted!!!.", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer18_181min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("GER_Fuel_Column_1");//Chief 22
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(60, () => sendScreenMessageTo(2, "Liegescourt area fuel supplies Spotted!!!.", null));
        }
        else if ("Timer19_210min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Stelling_to_Dunkirk");//Chief_13
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(60, () => sendScreenMessageTo(2, "Stelling_to_Canterbury Dunkirk area in Britain moving now ", null));
        }		
       else if ("Timer20_211min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("GER_Fuel_Column_2");//Chief_
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
           Timeout(60, () => sendScreenMessageTo(1, " St omar to Colembert", null));
        }
       else if ("Timer21_240min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("General_Tiny_Barber");//Chief_23
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Scotish General touring front lines Le Havre to Dieppe.", null));
        }
				
        else if ("Timer22_241min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("GER_Fuel_Column_3");//Chief_24
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Fuel Supplies movingAbbeville to Arras!", null));
         //   Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
       else if ("Timer23_270min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Rouen_Fuel_Convoy_2Dieppe");//Chief_16
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Rouen_Fuel_Convoy_2Dieppe", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "He-111s have been spotted over Calais @ 14K ft. heading west!", null));
        }
        else if ("Timer24_271min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Les_Andeleys_2_Brombos");//Chief_18
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Les_Andeleys_2_Brombos fuel trucks heading out.", null));
        }
        else if ("Timer25_300min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Axis_War_Plans");//Chief_25
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Enemy has our battle plans single vehicle BD5 to AY8", null));
          Timeout(30, () => sendScreenMessageTo(2, "Destroy the Vehicle with the plans!!", null));
        }
        else if ("Timer24_271min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Rouen_2_Dieppe2");//Chief_13
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Rouen_2_Dieppe2 fuel trucks heading out.", null));
        }

//Aircraft timers

		else if ("ATimer2_60min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("110_Escort_2_London");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    Timeout(10, () => sendScreenMessageTo(1, "Heavy enemy raid with escorts moving North!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }	
        else if ("ATimer2_60min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("110_Escort_2_London");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    Timeout(10, () => sendScreenMessageTo(1, "Heavy enemy raid with escorts moving North!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer2_60min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("109_Escort_2_London");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   Timeout(10, () => sendScreenMessageTo(1, "Heavy enemy raid with escorts moving North!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }	
        else if ("ATimer1_60min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("Heavy_Bomber_Raid_2");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "#2 Heavy enemy raid with escorts moving North!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
          //  GamePlay.gpGetTrigger(shortName).Enable = false;
        }		
        else if ("ATimer1_210min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("110_Escort_2_London");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
          //  Timeout(10, () => sendScreenMessageTo(1, "#2 Heavy enemy raid with escorts moving North!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_210min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("109_Escort_2_London");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(40, () => sendScreenMessageTo(1, "Heavy raid with escorts moving North!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_210min".Equals(shortName) && active )//Ai activity reguardless of number of players
        {
            AiAction action = GamePlay.gpGetAction("109_late_raid_Jabo");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "109s heading north from moving Northwest from Merville!", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            GamePlay.gpGetTrigger(shortName).Enable = false;
        }		

        else if ("Timer_Convoy_Britian".Equals(shortName) && active && !stopAI())// Trigger F1e launches escorts to go with Do 17s from trigger F1 above
        {
            AiAction action = GamePlay.gpGetAction("TimedPanzerConvoy");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
       Timeout(10, () => sendScreenMessageTo(1, "Enemy Tanks Spotted AS 1 heading West", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_60min".Equals(shortName) && active && !stopAI())// Trigger F1c launches escorts to go with escorts from trigger F1e above
        {
            AiAction action = GamePlay.gpGetAction("StukaRaid");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_120min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StukaRaid");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "test of timed operation.", null));
          //  Timeout(600, () => sendScreenMessageTo(2, "Wellingtons have been spotted over Dymchurch at 6000m heading east!!!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StukaRaid2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
 
         //    Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @ 20K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
		}	
        else if ("ATimer1_270min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StukaRaid3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
 
         //    Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @ 20K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;			
        }
        else if ("ATimer2_31min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Interior_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

         //   Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympe at 6000m!!!.", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer2_90min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Interior_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

        }
        else if ("ATimer1_240min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Somme_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(600, () => sendScreenMessageTo(1, "Activity reported along Somme.", null));
        }		
       else if ("ATimer1_60min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Somme_Patrol");// every hour
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

        }
       else if ("ATimer1_120min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Somme_Patrol");//hour 2
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

        }
				
        else if ("ATimer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Somme_Patrol");//hour 3
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   Timeout(10, () => sendScreenMessageTo(1, "Testing Somme Patrol Note time please", null));
         //   Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
       else if ("ATimer1_240min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Somme_Patrol");//4 hrs
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Enemy aircraft reported along Somme.", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "He-111s have been spotted over Calais @ 14K ft. heading west!", null));
        }
        else if ("ATimer1_300min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Somme_Patrol");//Somme patrol at 5 hours
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
          //  Timeout(10, () => sendScreenMessageTo(1, "Testing Somme #3  Patrol Note time please.", null));
        }
        else if ("ATimer1_60min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("  Timed_B_Front_Patrol");//60 min after start second front patrol of 109's
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Fighters near front lines", null));
        //  Timeout(600, () => sendScreenMessageTo(2, "Our Fighters covering front lines", null));
        }		
        else if ("ATimer1_90min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Front_Patrol2");//90 min after start second front patrol then again every 90 min
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Fighters near front lines", null));
        //  Timeout(600, () => sendScreenMessageTo(2, "Our Fighters covering front lines", null));
        }
        else if ("ATimer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Front_Patrol3");// second 110 patrol 180 min later
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Beware of front line fighters!", null));
          Timeout(600, () => sendScreenMessageTo(2, "Again our fighters cover he front lines", null));
        }
        else if ("ATimer1_270min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Front_Patrol4");// 3rd patrol of front using different aircraft
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Air trafic increase near front lines!", null));
            Timeout(600, () => sendScreenMessageTo(2, "Last flight of our fighters headed to front lines!", null));
        }
          else if ("ATimer1_92min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StukaRaid2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Heavy Stuka raid on front lines Escorts needed Launching Freecamp Now!!!.", null));
            Timeout(600, () => sendScreenMessageTo(1, "Activity in Le Havre Area!!", null));
        }
        else if ("ATimer1_95min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Blenhiem_Raid2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Heading to Rouen Fuel targets low and fast", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympne at 6000m!!!.", null));
        }
        else if ("ATimer3_120min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Blenhiem_Raid3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Spies report Blenheim aircraft heading East toward Rouen.", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "BR.20Ms have been spotted over Boulogne @ 13K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_300min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Blenhiem_Raid");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
       else if ("Timer3_90min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

       //     Timeout(600, () => sendScreenMessageTo(1, "testing ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }

        else if ("ATimer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

         //   Timeout(600, () => sendScreenMessageTo(1, "testing ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }			

        else if  ("ATimer1_30min".Equals(shortName) && active && !stopAI())
        {
			AiAction action = GamePlay.gpGetAction("Cher_Cover_Hi");
			if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

        //    Timeout(600, () => sendScreenMessageTo(1, "testing cher hi patrol", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("ATimer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

        //    Timeout(600, () => sendScreenMessageTo(1, "testing ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
	        else if ("ATimer1_270min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("109_Escort_2_London");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(600, () => sendScreenMessageTo(1, "Heavy Bomber Raid  heading West w/escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }			
  /*      else if ("Timer1_90min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Front_Patrol2");//90 min after start second front patrol then again every 90 min
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Fighters near front lines", null));
        //  Timeout(600, () => sendScreenMessageTo(2, "Our Fighters covering front lines", null));
        }
        else if ("Timer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Front_Patrol3");// second 110 patrol 180 min later
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Beware of front line fighters!", null));
          Timeout(600, () => sendScreenMessageTo(2, "Again our fighters cover he front lines", null));
        }
        else if ("Timer1_270min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_B_Front_Patrol4");// 3rd patrol of front using different aircraft
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Air trafic increase near front lines!", null));
            Timeout(600, () => sendScreenMessageTo(2, "Last flight of our fighters headed to front lines!", null));
        }
          else if ("Timer1_92min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StukaRaid2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Heavy Stuka raid on front lines Escorts needed Launching Freecamp Now!!!.", null));
            Timeout(600, () => sendScreenMessageTo(1, "Activity in Le Havre Area!!", null));
        }
        else if ("Timer1_95min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Blenhiem_Raid2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Heading to Rouen Fuel targets low and fast", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympne at 6000m!!!.", null));
        }
        else if ("Timer3_120min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Blenhiem_Raid3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Spies report Blenheim aircraft heading East toward Rouen.", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "BR.20Ms have been spotted over Boulogne @ 13K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer1_300min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Blenhiem_Raid");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
       else if ("Timer3_90min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(600, () => sendScreenMessageTo(1, "testing ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }

        else if ("Timer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(600, () => sendScreenMessageTo(1, "testing ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }			

        else if  ("Timer1_30min".Equals(shortName) && active && !stopAI())
        {
			AiAction action = GamePlay.gpGetAction("Cher_Cover_Hi");
			if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(600, () => sendScreenMessageTo(1, "testing cher hi patrol", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Timer1_180min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Timed_Rouen_Diep_Patrol");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(600, () => sendScreenMessageTo(1, "testing ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
	        else if ("  Heavy_Bomber_Raid_3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("109_Escort_2_London");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            Timeout(600, () => sendScreenMessageTo(1, "testing 3.5 hours Heavy Bomber Raid w/escorts #3 ", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }	
*/
		/*       else if ("Timer1_300min".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("General_Inspection_Red");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Second Test 300 min in", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
         
		 /*     From Here raids remmed out for now Do not delete just re write
		 else if ("F15".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action15");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "He-111's requesting escort.  Meet over Calais @ 6km in 10 minutes.", null));
         //   Timeout(600, () => sendScreenMessageTo(1, "He-111's have been spotted over Calais @ 20K ft. heading west!", null));
        }
        else if ("F16".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action16");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at St. Mary's Bay at 20K ft in 10 minutes.", null));
         //   Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Blenheims have been spotted over St. Mary's Bay at 6km.", null));
        }
        else if ("F17".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action17");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            sendScreenMessageTo(1, "Do 17's have been spotted east of Calais @ 20K ft.", null);
         //   sendScreenMessageTo(2, " third run Do 17's requesting escort! Meet at Calais at 6000m in 10 minutes", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F18".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action18");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            sendScreenMessageTo(1, "Wellingtons requesting escort! Meet at Dymchurch @ 20K ft. in 10 minutes.", null);
         //   sendScreenMessageTo(2, "Wellingtons have been spotted west of Dymchurch at 6000m.  Destroy them!", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }


		// from this point on no messages about triggers should Show Fatal 10/19
         

        else if ("escort1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Escort1 109s launched", null);
         //   sendScreenMessageTo(2, "Cover 109s launched for test", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("escort2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            //sendScreenMessageTo(1, "Escort2 Cover 109s launched", null);
            //sendScreenMessageTo(2, "Secondary Cover 109s launched for test", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("escort3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            //sendScreenMessageTo(1, "Escort3 Cover 109s launched", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("redspitcover1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Spits launched", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("redhurrycover1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("redhurrycover1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Hurricanes launched", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("Willmingtondefensered".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Willmingtondefensered");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense Willmington", null);
        //    sendScreenMessageTo(2, "Spits launched launched for yet another  test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset Willmington(ZD1)30 min", null);
            });
        }
        else if ("Redhilldefensered2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Redhilldefensered2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
          //  sendScreenMessageTo(1, "Air defense Redhill", null);
         //   sendScreenMessageTo(2, "Spits launched Redhill defense test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
             //   sendScreenMessageTo(1, " Trigger reset Redhill(ZD2)30 min", null);
            });
        }

        else if ("AirdefenseCalais".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("escort2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
          //  sendScreenMessageTo(1, "Air defense Calais", null);
          //  sendScreenMessageTo(2, "Air defense Calais ", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset Calais2 30 min", null);
            });
        }

        else if ("StOmar".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("StOmar");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Air defense St Omar", null);
        //    sendScreenMessageTo(2, "Air defense St Omar test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset St Omar 30 min", null);
            });
        }
        else if ("HighAltCalais".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("HighAltCalais");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "HighAltCalais", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
             //   sendScreenMessageTo(1, " Trigger reset HighAltCalais 30 min", null);
            });
        }
        else if ("109Cover3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("109Cover3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Air defense test of code 3 hr launch", null);
         //   sendScreenMessageTo(1, "Mission time should be top of hour4", null);
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Gladiatorintercept".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Gladiatorintercept");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Gladiator intercept Test of trigger", null);
        //    sendScreenMessageTo(2, "Gladiator intercept  test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, "Timer reset Dungeness trigger active", null);
            });
        }

        else if ("zonedefenseblue1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "A 110 cover Letoquet Test of trigger", null);
        //    sendScreenMessageTo(2, "110  test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, "Timer reset Letoquet(BZ1)trigger active", null);
            });
        }
        else if ("zonedefenseblue2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense Oye Plague", null);
        //    sendScreenMessageTo(2, "Air defense Oye Plague test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset Oye Plague 30 min", null);
            });
        }



        else if ("zonedefenseblue3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense Wissant", null);
        //    sendScreenMessageTo(2, "Air defense Wissant test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset Wissant3 30 min", null);
            });
        }
        else if ("zonedefenseblue4".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("zonedefenseblue4");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Air defense Calais", null);
         //   sendScreenMessageTo(2, "Air defense Calais test", null);
            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
             //   sendScreenMessageTo(1, " Trigger reset Calais offshore 30 min", null);
            });
        }
        else if ("London1air".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("London1air");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense London", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset London 30 min", null);
            });
        }
        else if ("London2air".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("London2air");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Air defense London2", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
            //    sendScreenMessageTo(1, " Trigger reset London 30 min", null);
            });
        }
        else if ("London3air".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("London3air");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense London3", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
             //   sendScreenMessageTo(1, " Trigger reset London 30 min", null);
            });
        }
        else if ("Thems1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Thems1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense Eastchurch", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
           //     sendScreenMessageTo(1, " Trigger reset North 30 min", null);
            });
        }
        else if ("Beau1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Beau1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Air defense Brookland", null);

            GamePlay.gpGetTrigger(shortName).Enable = false;
            Timeout(3600, () => {
                GamePlay.gpGetTrigger(shortName).Enable = true;
             //   sendScreenMessageTo(1, " Trigger reset North 30 min", null);
            });
        }
        else if ("Fatal1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Coastal Patrol 1", null);
            twcLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Fatal2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Coastal Patrol 2", null);
            twcLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Fatal3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Coastal Patrol 3", null);
            twcLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Fatal4".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Fatal4");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Coastal Patrol 4", null);
            twcLogServer(null, "Check time for coastal patrol for testing", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense1");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Northern Patrol 1", null);
            twcLogServer(null, "Pegwell defense triggered look for aicraft pegwell bay", new object[] { });
            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
        //    sendScreenMessageTo(1, "Northern Patrol 2", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Northern Patrol 3", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
        else if ("Pegwelldefense1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("Pegwelldefense4");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
         //   sendScreenMessageTo(1, "Northern Patrol 4", null);

            //GamePlay.gpGetTrigger(shortName).Enable = false;

        }
                */		
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
    public bool WriteResults_Out_File(string result = "3")
    {
        try
        {
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX4"); //Testing for potential causes of warping
            using (StreamWriter file = new StreamWriter(RESULTS_OUT_FILE, false))
            {
                file.WriteLine(result);
            }
            //Console.WriteLine("WriteResults_Out_File - file & contents: " + RESULTS_OUT_FILE + " " + result);
            return true;

        }
        catch (Exception ex)
        {

            Console.WriteLine("WriteResults_Out_File( - Error writing Mission RESULTS_OUT_FILE: " + RESULTS_OUT_FILE + " " + ex.Message);
            return false;

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
    public static double feet2meters(double a)
    {

        return (a * 1609.344 / 5280);

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
                              Point2d startPoint,
                              Point3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);

        return distance(diffX, diffY);
    }
    public static double CalculatePointDistance(
                              Point2d startPoint,
                              Point2d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);

        return distance(diffX, diffY);
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
                              Point2d startPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);

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
    //Given two points lp1 & lp2 that determine a line, what is the distance between single point sp and that line?
    //if ax + by + c = 0 is the line equation, distance is
    //abs(ax + by + c)/sqrt(a^2 + b^2)
    //It returns values +/-.  + value means, the point is to the right side of the line, looking down the line from p1 to p2
    // - value means, to the left side of that line

    public static double PointToLineDistance(Point2d linePoint1, Point2d linePoint2, Point3d singlePoint)
    {
        return PointToLineDistance(linePoint1, linePoint2, new Point2d (singlePoint.x, singlePoint.y));
    }

    public static double PointToLineDistance(Point2d linePoint1, Point2d linePoint2, Point2d singlePoint)
    {

        if (linePoint1.x == linePoint2.x && linePoint1.y == linePoint2.y) return CalculatePointDistance(singlePoint,linePoint1); //Two points the same, not a line, but we can calculate the point distance
                                                                                                               //if (lp1.x == lp2.x) return Math.Sign( lp2.y-lp1.y) * (sp.x - lp1.x);                                                                                                                
        double a = linePoint2.y - linePoint1.y;
        double b = -(linePoint2.x - linePoint1.x);
        double c = (-b * linePoint1.y) - a * linePoint1.x;

        return (a * singlePoint.x + b * singlePoint.y + c) / distance(a, b);  //This will be + or - depending on the orientation of the sp and lp1/lp2.  YOu can use the +/- to determine which side of the line it's on relative to vector lp1 -> lp2
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

    public static string correctedSectorNameDoubleKeypad(AMission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + doubleKeypad(p);
        return s;

    }

    public static string correctedSectorNameKeypad(AMission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + singleKeypad(p);
        return s;

    }

    //This make a  larger, somewhat random block of sectors, with the initial point in it somewhere. 
    //MaxSectorWidth 4x10000 actually gives sector blocks 5 wide sometimes (0.5 to 4.5, say -- takes in sectors 0,1,2,3,4)
    //So, we subract 1.
    public static string makeBigSector(AMission msn, Point3d p, int maxSectorWidth = 4)
    {
        Point3d p1 = new Point3d(p.x - clc_random.Next((maxSectorWidth-1) * 10000), p.y - clc_random.Next((maxSectorWidth - 1) * 10000), p.z);
        if (p1.x < 10000) p1.x = 10000;
        if (p1.y < 10000) p1.y = 10000;
        if (p1.x > 360000) p1.x = 360000;
        if (p1.y > 310000) p1.y = 310000;
        Point3d p2 = new Point3d(p1.x + clc_random.Next((maxSectorWidth - 1) * 10000), p1.y + clc_random.Next((maxSectorWidth - 1) * 10000), p.z);

        //BattleArea 10000 10000 360000 310000 10000 is TWC standard
        if (p2.x < 10000) p2.x = 10000;
        if (p2.y < 10000) p2.y = 10000;
        if (p2.x > 360000) p2.x = 360000;
        if (p2.y > 310000) p2.y = 310000;

        return correctedSectorName(msn, p1) + "-" + correctedSectorName(msn, p2);
        
    }

    //OK, so in order for the sector # to match up with the TWC map, and
    //to work with our "double keypad" routines listed here,
    //And (most important!) in order to make the sectors match up with EASY SIMPLE
    //squares of side 10000m in the in-game coordinate system, you must use this battle area
    //in the .mis file:
    //
    //BattleArea 10000 10000 360000 310000 10000
    //
    //Key here is the 10000,10000 which makes the origin of the battle area line up with the origin of the 
    //in-game coordinate system.
    //
    //If you wanted to change this & make the battle area smaller or something, you could just increase
    //the #s in increments of 100000.
    //The 360000 310000 is important only in that it EXACTLY matches the size of the map available in CLOD 
    //in FMB etc.  So 0 0 360000 310000 10000 exactly matches the full size of the Channel Map in CloD,
    //uses the full extent of the map, and makes the sector calculations exactly match in 10,000x10,000 meter 
    //increments.

    //This is also the way the TWC online radar map works, so if you do it that way the in-game map & offline 
    //radar map will match.

    public static string correctedSectorName(AMission msn, Point3d p)
    {

        string sector = msn.GamePlay.gpSectorName(p.x, p.y);
        sector = sector.Replace(",", ""); // remove the comma
        return sector;

    }

    public static string doubleKeypad(Point3d p)
    {
        int keyp = keypad(p, 10000);
        int keyp2 = keypad(p, 10000 / 3);
        return keyp.ToString() + "." + keyp2.ToString();
    }

    public static string singleKeypad(Point3d p)
    {
        int keyp = keypad(p, 10000);
        //int keyp2 = keypad(latlng, 10000 / 3);
        return keyp.ToString();
    }

    //keypad number for area, numbered 1-9 from bottom left to top right
    //of square size
    //Called with size = 10000 for normal CloD keypad, size = 10000/3 for mini-keypad
    //
    public static int keypad(Point3d p, double size)
    {
        int lat_rem = (int)Math.Floor(3 * (p.y % size) / size);
        int lng_rem = (int)Math.Floor(3 * (p.x % size) / size);
        return lat_rem * 3 + lng_rem + 1;
    }
    //Giant keypad covering the entire map.  Lower left is 1, upper right is 9
    //
    public static int giantkeypad(Point3d p)
    {
        //These are the max x,y values on the whole map
        double sizex = 360000;
        double sizey = 310000;
        int lat_rem = (int)Math.Floor(3 * (p.y % sizey) / sizey);
        int lng_rem = (int)Math.Floor(3 * (p.x % sizex) / sizex);
        return lat_rem * 3 + lng_rem + 1;
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

    public static IEnumerable<string> SplitToLines(string stringToSplit, int maxLineLength)
    {
        if (stringToSplit.Length <= maxLineLength) { yield return stringToSplit; yield break; }
        string[] words = stringToSplit.Split(' ');
        StringBuilder line = new StringBuilder();
        foreach (string word in words)
        {
            if (word.Length + line.Length <= maxLineLength)
            {
                line.Append(word + " ");
            }
            else
            {
                if (line.Length > 0)
                {
                    yield return line.ToString().Trim();
                    line.Clear();
                }
                string overflow = word;
                while (overflow.Length > maxLineLength)
                {
                    yield return overflow.Substring(0, maxLineLength);
                    overflow = overflow.Substring(maxLineLength);
                }
                line.Append(overflow + " ");
            }
        }
        yield return line.ToString().Trim();
    }

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
    public static Player PlayerFromName(AMission msn, string name)
    {   // Purpose: Returns Player player given string name of player
        
        //multiplayer
        if (msn.GamePlay.gpRemotePlayers() != null) foreach (Player player in msn.GamePlay.gpRemotePlayers())
        {
                if (player.Name() == name) return player;
        }
        //singleplayer
        else if (msn.GamePlay.gpPlayer() != null)
        {
            if (msn.GamePlay.gpPlayer().Name() == name) return msn.GamePlay.gpPlayer();
        }
        return null;
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

    //returns distance to nearest friendly airport to actor, in meters. Count all friendly airports, alive or not.
    public static double distanceToNearestAirport(this IGamePlay GamePlay, AiActor actor)
    {
        double d2 = 10000000000000000; //we compare distanceSQUARED so this must be the square of some super-large distance in meters && we'll return anything closer than this.  Also if we don't find anything we return the sqrt of this number, which we would like to be a large number to show there is nothing nearby.  If say d2 = 1000000 then sqrt (d2) = 1000 meters which probably not too helpful.
        double d2Min = d2;
        if (actor == null) return d2Min;
        Point3d pd = actor.Pos();
        int n = GamePlay.gpAirports().Length;
        //AiActor[] aMinSaves = new AiActor[n + 1];
        //int j = 0;
        //twcLogServer(null, "Checking distance to nearest airport", new object[] { });
        for (int i = 0; i < n; i++)
        {
            AiActor a = (AiActor)GamePlay.gpAirports()[i];
            if (a == null) continue;
            //if (actor.Army() != a.Army()) continue; //only count friendly airports
            //if (actor.Army() != (a.Pos().x, a.Pos().y)
            //OK, so the a.Army() thing doesn't seem to be working, so we are going to try just checking whether or not it is on the territory of the Army the actor belongs to.  For some reason, airports always (or almost always?) list the army = 0.

            //twcLogServer(null, "Checking airport " + a.Name() + " " + GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) + " " + a.Pos().x.ToString ("N0") + " " + a.Pos().y.ToString ("N0") , new object[] { });

            if (GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != actor.Army()) continue;


            //if (!a.IsAlive()) continue;


            Point3d pp;
            pp = a.Pos();
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                d2Min = d2;
                //twcLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString(), new object[] { });
            }

        }
        //twcLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
        return Math.Sqrt(d2Min);
    }

    //nearest airport to a point
    //army=0 is neutral, meaning found airports of any army
    //otherwise, find only airports matching that army
    public static AiAirport nearestAirport(this IGamePlay GamePlay, Point3d location, int army = 0)
    {
        AiAirport NearestAirfield = null;
        AiAirport[] airports = GamePlay.gpAirports();
        Point3d StartPos = location;

        if (airports != null)
        {
            foreach (AiAirport airport in airports)
            {
                AiActor a = airport as AiActor;
                if (army != 0 && GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != army) continue;
                if (NearestAirfield != null)
                {
                    if (NearestAirfield.Pos().distanceSquared(ref StartPos) > airport.Pos().distanceSquared(ref StartPos))
                        NearestAirfield = airport;
                }
                else NearestAirfield = airport;
            }
        }
        return NearestAirfield;
    }

    //nearest airport to an actor
    public static AiAirport nearestAirport(this IGamePlay GamePlay, AiActor actor, int army = 0)
    {
        if (actor == null) return null;
        Point3d pd = actor.Pos();
        return nearestAirport(GamePlay, pd, army);
    }


    public static void Shuffle<T>(this IList<T> list)
    {
        for (var i = 0; i < list.Count; i++)
            list.Swap(i, clc_random.Next(i, list.Count));
    }

    public static void Swap<T>(this IList<T> list, int i, int j)
    {
        var temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }

    public static async Task<bool> WriteAllTextAsyncWithBackups(string data, string fileDir_base, string fileName_base, string suffix, string ext, string backupDirStub, bool wait = false, ManualResetEvent resetEvent = null)
    {

        DateTime dt = DateTime.UtcNow;
        string date = dt.ToString("u");
        TimeSpan start = new TimeSpan(0, 0, 0); //Midnight o'clock
        TimeSpan end = new TimeSpan(12, 0, 0); //12 o'clock noon
        TimeSpan now = DateTime.UtcNow.TimeOfDay;

        string backupDirFile = fileDir_base + backupDirStub + fileName_base + suffix + "-" + dt.ToString("yyyy-MM-dd");
        if (start < now && now < end) backupDirFile += "-AM" + ext; //two backups a day, morn & eve
        else backupDirFile += "-PM" + ext;       

        string filename_main = fileDir_base + fileName_base + suffix + ext;
        string backupTxtFile2 = fileDir_base + fileName_base + suffix + "_old2" + ext;
        string backupTxtFile1 = fileDir_base + fileName_base + suffix + "_old" + ext;
        string tempNewTxtFile = fileDir_base + fileName_base + suffix + "_tmp" + ext;


        //Console.WriteLine("WriteAllTextAsyncWithBackups: " + tempNewTxtFile + ": " + fileDir_base + " " + fileName_base + " " + suffix);

        try
        {
            //first, write the new file as a temp file
            try
            {
                System.IO.File.Delete(tempNewTxtFile);
            }
            catch (Exception ex) { }

            try
            {
                await WriteAllTextAsync(tempNewTxtFile, data);
            }
            catch (Exception ex) {
                Console.WriteLine("WriteAllTextAsyncWithBackups ERROR writing-ABORTING SAVE!! " + fileName_base + suffix + "_tmp" + ext + ": " + ex.Message);

                if (resetEvent != null) resetEvent.Set();
                return false;
            }

            //Now if that succeeded, we can proceed with backups etc. -  but only if it succeeded. No point in backing up if we don't have anything to backup

            if (System.IO.File.Exists(tempNewTxtFile)) {

                //First, copy the  new .tmp file to the backup directory (date plus A/B - morn & evening)
                try
                {
                    System.IO.File.Delete(backupDirFile);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR deleting " + backupDirFile + ": " + ex.Message); }

                try
                {
                    System.IO.File.Copy(tempNewTxtFile, backupDirFile);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR moving to backup " + fileName_base + suffix + "_tmp" + ext + ": " + ex.Message); }

                //Now move 1st backup to 2nd backup
                try
                {
                    System.IO.File.Delete(backupTxtFile2);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR deleting " + fileName_base + suffix + "_old2" + ext + ": " + ex.Message); }

                try
                {
                    System.IO.File.Move(backupTxtFile1, backupTxtFile2);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR moving to " + fileName_base + suffix + "_old2" + ext + ": " + ex.Message); }


                //Now move current main file to 1st backup
                try
                {
                    System.IO.File.Delete(backupTxtFile1);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR deleting " + fileName_base + suffix + "_old" + ext + ": " + ex.Message); }

                try
                {
                    System.IO.File.Copy(filename_main, backupTxtFile1);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR copying to " + fileName_base + suffix + "_old" + ext + ": " + ex.Message); }



                //Now move tmpNew main file to main file
                try
                {
                    System.IO.File.Delete(filename_main);
                }
                catch (Exception ex) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR deleting " + fileName_base + suffix + ext + ": " + ex.Message); }

                try
                {
                    System.IO.File.Move(tempNewTxtFile, filename_main);
                } catch (Exception ex)
                {
                    //If the rename of the .tmp to main fails this is DISASTROUS so we will try to
                    //save a copy of the current data file at least.  It will be namnd
                    //xxxx.EXT-3030301 with date/time appended to end
                    //Random ran = new Random();
                    //string r = ran.Next(1000000, 9999999).ToString();   
                    string r = dt.ToString("yyyy-MM-dd-HHmmss");

                    try
                    {
                        System.IO.File.Move(tempNewTxtFile, filename_main + "EMERGENCY_SAVE" + "-" + r);
                    }
                    catch (Exception ex1) { Console.WriteLine("WriteAllTextAsyncWithBackups ERROR on emergency save " + fileName_base + suffix + ext + ": " + ex1.ToString()); }
                }
            } else
            {
                Console.WriteLine("WriteAllTextAsync ERROR - file write FAILED: " + fileName_base + suffix + "_tmp" + ext);
            }

            //once all this is done we can release the hold, if present
            if (resetEvent != null) resetEvent.Set();
            return false;

        }
        catch (Exception ex)
        {
            Console.WriteLine("WriteAllTextAsync ERROR writing " + fileName_base + suffix + "_tmp" + ext + ": " + ex.Message);

            if (resetEvent != null) resetEvent.Set();
            return false;

        }
    }

    public static async Task<bool> WriteAllTextAsync(string filelocation, string data, bool wait = false, ManualResetEvent resetEvent = null )
    {
        try
        {
            using (var sw = new StreamWriter(filelocation))
            {
                /*
                int count = 0;
                if (wait)
                {
                    //try it like 10X if not successful
                    *
                    while (count < 10)
                    {
                        Task<bool> task = Calcs.WriteAllTextAsync(filepath, xmlString);
                        bool res = await task;
                        if (res) break;
                        Console.WriteLine("MO_WriteMissionObject: Attempt #" + count.ToString() + " to write " + name + " not successful");
                        Thread.Sleep(1);
                        count++;
                    }*
                    Task task = sw.WriteAsync(data);
                    await task;

                }
                else */
                await sw.WriteAsync(data);
            }
            if (resetEvent != null) resetEvent.Set(); //signals the waiting method that called this, to exit
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("WriteAllTextAsync ERROR writing " + filelocation + ": " + ex.ToString());

            if (resetEvent != null) resetEvent.Set();
            return false;
            
        }
    }

    public static void loadSmokeOrFire(maddox.game.IGamePlay GamePlay, AMission mission, double x, double y, double z, string type="BuildingFireBig", double duration_s = 300)
    {
        /* Sample: Static556 Smoke.Environment.Smoke1 nn 63718.50 187780.80 110.00 /height 16.24 
         possible types: Smoke1 Smoke2 BuildingFireSmall BuildingFireBig BigSitySmoke_0 BigSitySmoke_1

        Not sure if height is above sea level or above ground level.
 */



        //AMission mission = GamePlay as AMission;
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";
        string key = "Static1";
        string value = "Smoke.Environment." + type + " nn " + x.ToString("0.00") + " " + y.ToString("0.00") + " " + (duration_s / 60).ToString("0.0") + " /height " + z.ToString("0.00");
        f.add(sect, key, value);

        GamePlay.gpPostMissionLoad(f);


    }

    public static void loadStatic(maddox.game.IGamePlay GamePlay, AMission mission, double x, double y, double z, string type = "Stationary.Opel_Blitz_cargo", double heading = 0, string side = "nn")
    {
        /*  side = nn , gb, or de
         * Examples: https://theairtacticalassaultgroup.com/forum/showthread.php?t=8493
         *   Static4 Stationary.Environment.Table_empty_UK-GER_1 nn 14334.77 15015.75 661.10 
                Static2 Stationary.Scammell_Pioneer_R100 gb 14357.38 15036.04 661.10 
                Static7 Stationary.Airfield.Sign_Table_UK1 nn 14295.63 15054.30 661.10 
                Static5 Stationary.Environment.Table_w_chess_UK-GER_1 nn 14308.39 15048.51 661.10 
                Static6 Stationary.Environment.Table_w_dinner_UK-GER_1 nn 14306.36 15060.39 661.10 
                Static3 Stationary.BMW_R71_w_MG_34 de 14325.78 15042.71 661.10  //motorcycle
                Static1 Stationary.Opel_Blitz_cargo de 14321.43 15065.61 661.10
                Static0 Stationary.Horch_830_B1 de 14350.42 15056.62 661.10  
                Static65 Stationary.Humans.Kdo_Hi_Ger35_passenger_4 de 14345.22 15051.82 661.10 2
                Static120 Stationary.Humans.Soldier_Krupp_L2H43_Driver_1 de 14342.67 15055.75 661.10 3 
                Static48 Stationary.Humans.Soldier_MG_TA_Passenger_1 de 14341.79 15056.98 661.10 4      
                Static66 Stationary.Humans.Ladder_passenger_ger_1 de 14344.66 15053.11 661.10 5
                Static50 Stationary.Humans.150_cm_Flakscheinwerfer_gunner1 de 14343.99 15055.41 661.10 6 
                Static123 Stationary.Humans.Soldier_Krupp_L2H43_pak_Driver_1 de 14344.14 15054.25 661.10 7 
                Static119 Stationary.Humans.Soldier_Sdkfz105_Gunner de 14346.02 15052.77 661.10 8 
                Static71 Stationary.Humans.Portable_Siren_ger_passenger de 14347.25 15052.80 661.10 9  
                */

        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";
        string key = "Static1";
        string value = type + " " + side + " " + x.ToString("0.00") + " " + y.ToString("0.00") + " " +  heading.ToString("0.0") + " /height " + z.ToString("0.00");
        //Console.WriteLine("Load Static: " + value);
        f.add(sect, key, value);

        GamePlay.gpPostMissionLoad(f);
        
    }

    private static int staticCount = 0;

    public static ISectionFile makeStatic(ISectionFile f, maddox.game.IGamePlay GamePlay, AMission mission, double x, double y, double z, string type = "Stationary.Opel_Blitz_cargo", double heading = 0, string side = "nn")
    {
        /*  side = nn , gb, or de
         * Examples: https://theairtacticalassaultgroup.com/forum/showthread.php?t=8493
         *   Static4 Stationary.Environment.Table_empty_UK-GER_1 nn 14334.77 15015.75 661.10 
                Static2 Stationary.Scammell_Pioneer_R100 gb 14357.38 15036.04 661.10 
                Static7 Stationary.Airfield.Sign_Table_UK1 nn 14295.63 15054.30 661.10 
                Static5 Stationary.Environment.Table_w_chess_UK-GER_1 nn 14308.39 15048.51 661.10 
                Static6 Stationary.Environment.Table_w_dinner_UK-GER_1 nn 14306.36 15060.39 661.10 
                Static3 Stationary.BMW_R71_w_MG_34 de 14325.78 15042.71 661.10  //motorcycle
                Static1 Stationary.Opel_Blitz_cargo de 14321.43 15065.61 661.10
                Static0 Stationary.Horch_830_B1 de 14350.42 15056.62 661.10  
                Static65 Stationary.Humans.Kdo_Hi_Ger35_passenger_4 de 14345.22 15051.82 661.10 2
                Static120 Stationary.Humans.Soldier_Krupp_L2H43_Driver_1 de 14342.67 15055.75 661.10 3 
                Static48 Stationary.Humans.Soldier_MG_TA_Passenger_1 de 14341.79 15056.98 661.10 4      
                Static66 Stationary.Humans.Ladder_passenger_ger_1 de 14344.66 15053.11 661.10 5
                Static50 Stationary.Humans.150_cm_Flakscheinwerfer_gunner1 de 14343.99 15055.41 661.10 6 
                Static123 Stationary.Humans.Soldier_Krupp_L2H43_pak_Driver_1 de 14344.14 15054.25 661.10 7 
                Static119 Stationary.Humans.Soldier_Sdkfz105_Gunner de 14346.02 15052.77 661.10 8 
                Static71 Stationary.Humans.Portable_Siren_ger_passenger de 14347.25 15052.80 661.10 9  
                */

        
        string sect = "Stationary";
        string key = "Static" + staticCount.ToString();
        staticCount++;
        string value = type + " " + side + " " + x.ToString("0.00") + " " + y.ToString("0.00") + " " + heading.ToString("0.0") + " /height " + z.ToString("0.00");
        //Console.WriteLine("Load Static: " + value);
        f.add(sect, key, value);

        return f;        

    }

    /*
    /// <summary>
    /// Removes all event handlers subscribed to the specified routed event from the specified element.
    /// </summary>
    /// <param name="element">The UI element on which the routed event is defined.</param>
    /// <param name="routedEvent">The routed event for which to remove the event handlers.</param>
    public static void RemoveRoutedEventHandlers(Delegate element, Event routedEvent)
    {
        // Get the EventHandlersStore instance which holds event handlers for the specified element.
        // The EventHandlersStore class is declared as internal.
        var eventHandlersStoreProperty = typeof(UIElement).GetProperty(
            "EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
        object eventHandlersStore = eventHandlersStoreProperty.GetValue(element, null);

        // If no event handlers are subscribed, eventHandlersStore will be null.
        // Credit: https://stackoverflow.com/a/16392387/1149773
        if (eventHandlersStore == null)
            return;

        // Invoke the GetRoutedEventHandlers method on the EventHandlersStore instance 
        // for getting an array of the subscribed event handlers.
        var getRoutedEventHandlers = eventHandlersStore.GetType().GetMethod(
            "GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var routedEventHandlers = (RoutedEventHandlerInfo[])getRoutedEventHandlers.Invoke(
            eventHandlersStore, new object[] { routedEvent });

        // Iteratively remove all routed event handlers from the element.
        foreach (var routedEventHandler in routedEventHandlers)
            element.RemoveHandler(routedEvent, routedEventHandler.Handler);
    }
    //https://stackoverflow.com/questions/91778/how-to-remove-all-event-handlers-from-an-event
    public static void RemoveAllDelegatesFromEvent(Event E) //Delegate d, EventArgs A) //, FormClosingEventArgs e)
    {
        foreach (Delegate d in E.GetInvocationList())
        {
            E -= (FindClickedHandler)d;
        }
    }
    */
    //looks like this.Something = null; //erases all event handlers.  Urgh.

    public static void RemoveDelegates(Object b)
    {
        //FieldInfo f1 = typeof(Control).GetField("EventClick",
        //    BindingFlags.Static | BindingFlags.NonPublic);
        //object obj = f1.GetValue(b);
        PropertyInfo pi = b.GetType().GetProperty("Events",
            BindingFlags.NonPublic | BindingFlags.Instance);
        EventHandlerList list = (EventHandlerList)pi.GetValue(b, null);
        //list.RemoveHandler(obj, list[obj]);
    }

    public static IEnumerable<string> GetEventKeysList(Component issuer)
    {

        System.Reflection.PropertyInfo eventsProp = typeof(Component).GetProperty("Events", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return
            from key in issuer.GetType().GetFields(BindingFlags.Static |
            BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            where key.Name.StartsWith("Event")
            select key.Name;
    }
    public static string randSTR(string[] strings)
    {
        //Random clc_random = new Random();
        return strings[clc_random.Next(strings.Length)];
    }
    //So, sometimes we want a simple repeatable seed value for 
    //random that will give the same set of random numbers each time
    //a routine runs with the same underlying values, but a different set
    //if different values.  So you can compile some objects associated with the routine
    //that are different each time around but the same if it repeats, and this will give a
    //simple repeatable different int in return.  Obviously . . . not cryptographically secure or anything.
    public static int seedObj(object [] objs)
    {
        string amalg = "";        
        foreach (object o in objs) amalg += o.ToString();
        byte[] bytes = Encoding.ASCII.GetBytes(amalg);
        ulong seed = 3;
        foreach (byte b in bytes) seed += (ulong)b;
        seed = seed % int.MaxValue; //probably paranoid, but using ulong to avoid overflows & then modding by int's maxvalue & returning by int, which is what we need.
        return Convert.ToInt32(seed);
    }
} //end class Calcs


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

/*
public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
{
    Random rnd = new Random();
    return source.OrderBy<T, int>((item) => rnd.Next());
}
*/
namespace Ini
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// https://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C
    /// </summary>
    public class IniFile
    {
        public string path;
        //public Mission mission;
        public int iniErrorCount;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <param name="INIPath"></param>
        //public IniFile(string INIPath, Mission msn)
        public IniFile(string INIPath)
        {
            path = INIPath;
            //mission = msn;
            iniErrorCount = 0;

        }
        public void IniDeleteSection(string Section)
        {
            WritePrivateProfileString(Section, null, null, this.path);
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <param name="Section"></param>
        /// Section name
        /// <param name="Key"></param>
        /// Key Name
        /// <param name="Value"></param>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, "\"" + Value + "\"", this.path);
        }

        public void IniWriteList(string Section, string Key, List<string> Value)
        {
            int count = 0;
            WritePrivateProfileString(Section, "Count", Value.Count.ToString(), this.path);
            //Console.WriteLine("INIW: Count" + Value.Count.ToString());
            foreach (string s in Value)
            {
                WritePrivateProfileString(Section, Key + "[" + count.ToString() + "]", "\"" + s + "\"", this.path);
                count++;
                //Console.WriteLine("INIW: Count" + Section + " " + Key + "[" + count.ToString() + "]" + " " + s);
            }
        }
        public List<string> IniReadList(string Section, string Key)
        {
            List<string> l = new List<string>();

            int total = IniReadValue(Section, "Count", (int)0);

            if (total == 0) return l;
            for (int x = 0; x < total; x++)
            {

                l.Add(IniReadValue(Section, Key + "[" + x.ToString() + "]", ""));
            }

            return l;
        }



        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        //overloaded for string, int, double, bool.  Could do others like single, float, whatever.  String[] int[] double[] etc.
        public string IniReadValue(string Section, string Key, string def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            if (temp.Length > 0) return temp.ToString();
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public int IniReadValue(string Section, string Key, int def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            int a;
            if (temp.Length > 0 && int.TryParse(temp.ToString(), out a)) return a;
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public double IniReadValue(string Section, string Key, double def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            double a;
            if (temp.Length > 0 && double.TryParse(temp.ToString(), out a)) return a;
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public bool IniReadValue(string Section, string Key, bool def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            if (temp.ToString().Trim() == "1") temp = new StringBuilder("True", 4); //allow 0 & 1 to be used, or True/true/False/false
            if (temp.ToString().Trim() == "0") temp = new StringBuilder("False", 5);
            bool a;
            if (temp.Length > 0 && bool.TryParse(temp.ToString(), out a)) return a;
            else
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        private void IniReadError(String Section, String Key)
        {
            iniErrorCount++;
            Console.WriteLine("-main.cs: ERROR reading .ini file: Key {0} in Section {1} was not found.", Key, Section);

        }
    }
}

//Circular array which operates as a limited size queue OR stack
//Based on https://www.codeproject.com/Articles/31652/A-Generic-Circular-Array
public class CircularArray<T>
{
    private readonly T[] _baseArray;
    private readonly T[] _facadeArray;
    private int _head;
    private bool _isFilled;

    public CircularArray(int length)
    {
        _baseArray = new T[length];
        _facadeArray = new T[length];
    }

    //Array in queue order (first queued = first of array)
    public T[] Array
    {
        get
        {
            int pos = _head;
            for (int i = 0; i < _baseArray.Length; i++)
            {
                Math.DivRem(pos, _baseArray.Length, out pos);
                _facadeArray[i] = _baseArray[pos];
                pos++;
            }
            return _facadeArray;
        }
    }

    //Array in stack order (last queued = first of array)
    public T[] ArrayStack
    {
        get
        {
            int pos = _head - 1 + 2*_baseArray.Length;  //by adding 2*_baseArray.Length we can count downwards by _baseArray.Length with no worries about going below 0 for our index.  We have to go 2* bec _head might be zero meaning our starting point might be -1
            for (int i = 0; i < _baseArray.Length; i++)
            {
                Math.DivRem(pos, _baseArray.Length, out pos);
                //Console.WriteLine("ArrayStack: " + i.ToString() + " " + pos.ToString());
                 
                _facadeArray[i] = _baseArray[pos];
                pos--;
                pos = pos < 0 ? pos + _baseArray.Length : pos;
            }
            return _facadeArray;
        }
    }

    public T[] BaseArray
    {
        get { return _baseArray; }
    }

    public bool IsFilled
    {
        get { return _isFilled; }
    }

    public void Push(T value)
    {
        if (!_isFilled && _head == _baseArray.Length - 1)
            _isFilled = true;

        Math.DivRem(_head, _baseArray.Length, out _head);
        _baseArray[_head] = value;
        _head++;
    }

    //Gets end of queue (ie, the first value entered) if 0 or 2nd, 3rd, etc value entered if index 1, 2, 3 etc
    //10/2018 - this seems incorrect. This gets the last value pushed onto the array if 0, 2nd to last if 1, etc.
    public T Get(int indexBackFromHead)
    {
        int pos = _head - indexBackFromHead - 1;
        pos = pos < 0 ? pos + _baseArray.Length : pos;
        Math.DivRem(pos, _baseArray.Length, out pos);
        return _baseArray[pos];
    }

    //Gets top of the stack (ie, the last value entered) if 0 or 2nd to last, 3rd to last, etc if index 1, 2, 3 etc 
    ////10/2018 - this seems incorrect. This gets the tail of the array, ie the first value pushed onto the array (that still remains), ie the oldest value in the array, if 0, 2nd to last if 1, etc.
    public T GetStack(int indexForwardFromHead)
    {
        int pos = _head + indexForwardFromHead;
        pos = pos < 0 ? pos + _baseArray.Length : pos;
        Math.DivRem(pos, _baseArray.Length, out pos);
        return _baseArray[pos];
    }
}

namespace cevent
{
    //--------------------------------------------------------------------------------
    static public class cEventHelper
    {
        static Dictionary<Type, List<FieldInfo>> dicEventFieldInfos = new Dictionary<Type, List<FieldInfo>>();

        static BindingFlags AllBindings
        {
            get { return BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static; }
        }

        //--------------------------------------------------------------------------------
        static List<FieldInfo> GetTypeEventFields(Type t)
        {
            if (dicEventFieldInfos.ContainsKey(t))
                return dicEventFieldInfos[t];

            List<FieldInfo> lst = new List<FieldInfo>();
            BuildEventFields(t, lst);
            dicEventFieldInfos.Add(t, lst);
            return lst;
        }

        //--------------------------------------------------------------------------------
        static void BuildEventFields(Type t, List<FieldInfo> lst)
        {
            // Type.GetEvent(s) gets all Events for the type AND it's ancestors
            // Type.GetField(s) gets only Fields for the exact type.
            //  (BindingFlags.FlattenHierarchy only works on PROTECTED & PUBLIC
            //   doesn't work because Fieds are PRIVATE)

            // NEW version of this routine uses .GetEvents and then uses .DeclaringType
            // to get the correct ancestor type so that we can get the FieldInfo.
            foreach (EventInfo ei in t.GetEvents(AllBindings))
            {
                Type dt = ei.DeclaringType;
                FieldInfo fi = dt.GetField(ei.Name, AllBindings);
                if (fi != null)
                    lst.Add(fi);
            }

            // OLD version of the code - called itself recursively to get all fields
            // for 't' and ancestors and then tested each one to see if it's an EVENT
            // Much less efficient than the new code
            /*
                  foreach (FieldInfo fi in t.GetFields(AllBindings))
                  {
                    EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                    if (ei != null)
                    {
                      lst.Add(fi);
                      Console.WriteLine(ei.Name);
                    }
                  }
                  if (t.BaseType != null)
                    BuildEventFields(t.BaseType, lst);*/
        }

        //--------------------------------------------------------------------------------
        static EventHandlerList GetStaticEventHandlerList(Type t, object obj)
        {
            MethodInfo mi = t.GetMethod("get_Events", AllBindings);
            return (EventHandlerList)mi.Invoke(obj, new object[] { });
        }

        //--------------------------------------------------------------------------------
        public static void RemoveAllEventHandlers(object obj) { RemoveEventHandler(obj, ""); }

        //--------------------------------------------------------------------------------
        public static void RemoveEventHandler(object obj, string EventName)
        {
            if (obj == null)
                return;

            Type t = obj.GetType();
            List<FieldInfo> event_fields = GetTypeEventFields(t);
            EventHandlerList static_event_handlers = null;

            foreach (FieldInfo fi in event_fields)
            {
                if (EventName != "" && string.Compare(EventName, fi.Name, true) != 0)
                    continue;

                // After hours and hours of research and trial and error, it turns out that
                // STATIC Events have to be treated differently from INSTANCE Events...
                if (fi.IsStatic)
                {
                    // STATIC EVENT
                    if (static_event_handlers == null)
                        static_event_handlers = GetStaticEventHandlerList(t, obj);

                    object idx = fi.GetValue(obj);
                    Delegate eh = static_event_handlers[idx];
                    if (eh == null)
                        continue;

                    Delegate[] dels = eh.GetInvocationList();
                    if (dels == null)
                        continue;

                    EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                    foreach (Delegate del in dels)
                        ei.RemoveEventHandler(obj, del);
                }
                else
                {
                    // INSTANCE EVENT
                    EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                    if (ei != null)
                    {
                        object val = fi.GetValue(obj);
                        Delegate mdel = (val as Delegate);
                        if (mdel != null)
                        {
                            foreach (Delegate del in mdel.GetInvocationList())
                                ei.RemoveEventHandler(obj, del);
                        }
                    }
                }
            }
        }

        //--------------------------------------------------------------------------------
    }
}