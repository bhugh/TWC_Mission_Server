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
using TF_Extensions;
//using GCVBackEnd;

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

    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    public enum ArmiesE { None, Red, Blue };

    public bool MISSION_STARTED = false;
    public bool WAIT_FOR_PLAYERS_BEFORE_STARTING_MISSION_ENABLED = false;
    public int START_MISSION_TICK = -1;
    public int END_MISSION_TICK = 720000;
    public bool END_MISSION_IF_PLAYERS_INACTIVE = false;
    public bool COOP_START_MODE_ENABLED = false;
    public bool COOP_START_MODE = false;
    public double COOP_MODE_TIME_SEC = 45;
    public int START_COOP_TICK = -1;
    public double COOP_TIME_LEFT_MIN = 9999;
    public int ticksperminute = 1986;
    public double CampaignMapState = 0; //Determines which base map to load in & where the front it.  0 is the neutral map, - numbers tend more towards Blue, + numbers more towards Red
    public string CampaignMapSuffix = "-0"; //The initial initairports files will have suffix -0
    public string MapPrevWinner = ""; //Winner of the previous mission, if there was one
    public int CampaignMapMaxRedSuffixMax = 1; //This implies you have initairports files named with suffix ie -R001, -R002, -R003, -R004 through the max
    public int CampaignMapMaxBlueSuffixMax = 1; //This implies you have initairports files named ie -B001, -B002, -B003, -B004 through the max

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
    double redMultAdmin = 0;
    double blueMultAdmin = 0;


    MissionObjectives mission_objectives;

    //Constructor
    public Mission()
    {
        //outPath = "C:\\GoogleDrive\\GCVData";
        TWCComms.Communicator.Instance.Main = (IMainMission)this; //allows -stats.cs to access this instance of Mission

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
        //SERVER_ID_SHORT = "Tactical"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        SERVER_ID_SHORT = "MissionTEST"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        CAMPAIGN_ID = "The Big War"; //Used to name the filename that saves state for this campaign that determines which map the campaign will use, ie -R001, -B003 etc.  So any missions that are part of the same overall campaign should use the same CAMPAIGN_ID while any missions that happen to run on the same server but are part of a different campaign should have a different CAMPAIGN_ID
        DEBUG = false;
        LOG = false;
        //WARP_CHECK = false;
        radarpasswords = new Dictionary<int, string>
        {
            { -1, "north"}, //Red army #1
            { -2, "gate"}, //Blue, army #2
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
        //Will be updated with value from -stats.ini OnMissionLoaded
                   
        stopwatch = Stopwatch.StartNew();
        RADAR_REALISM = (int)5;
        RESULTS_OUT_FILE = CLOD_PATH + FILE_PATH + @"/" + "MissionResult.txt";
        radar_messages_store = new Dictionary<string, Tuple<long, SortedDictionary<string, string>>>();
    }

    /********************************************************
     * 
     * Save campaign state every 10 minutes so that if
     * something messes up before end of mission, we
     * don't lose all the campaign developments this mission
     * 
     *******************************************************/

    public void SaveCampaignStateIntermediate()
    {

        Timeout(1800, () => { SaveCampaignStateIntermediate(); });
        if (!MISSION_STARTED) return;
        Task.Run(() => SaveMapState("", true));
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
    DateTime LastTimePlayerLoggedIn = DateTime.Now;
    DateTime LastTimePlayerInPlace = DateTime.Now;

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
            LastTimePlayerLoggedIn = DateTime.Now;
            LastTimePlayerInPlace = DateTime.Now;
            //Console.WriteLine("Not miss started/coopstart");
            return;
        }
        if (!EndMissionIfPlayersInactive_initialized)
        {

            LastTimePlayerLoggedIn = DateTime.Now;
            LastTimePlayerInPlace = DateTime.Now;
            EndMissionIfPlayersInactive_initialized = true;
            //Console.WriteLine("EMIPI initialized");
            return;
        }


        if (GamePlay.gpPlayer() != null && GamePlay.gpPlayer().Place() != null)
        {
            LastTimePlayerLoggedIn = DateTime.Now;
            LastTimePlayerInPlace = DateTime.Now;
            //Console.WriteLine("EMIPI single player in place");
            return; //we only need one . .. 
        }

        //if (GamePlay.gpPlayer() != null || (GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0))
        if ((GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)) //giving up on looking for the single player as GamePlay.gpPlayer() always seems to be != null
        {
            LastTimePlayerLoggedIn = DateTime.Now;
            //Console.WriteLine("EMIPI a player is logged in " + (GamePlay.gpPlayer() != null).ToString() + " " + (GamePlay.gpRemotePlayers() != null).ToString() + " " + GamePlay.gpRemotePlayers().Length.ToString());


            if (GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)
            {
                foreach (Player p in GamePlay.gpRemotePlayers())
                {

                    if (p.Place() != null)
                    {
                        LastTimePlayerInPlace = DateTime.Now;
                        Console.WriteLine("EMIPI multi player in place");
                        return; //we only need one . .. 
                    }

                }
            }
        }

        //Console.WriteLine("EMIPI checking time since last player");
        //End the mission if it has been 7.5 minutes since someone logged in OR 15 minutes since they were actually in a place.
        //if (LastTimePlayerLoggedIn.AddMinutes(.5) < DateTime.Now || LastTimePlayerInPlace.AddMinutes(15) < DateTime.Now)  //testing
        if (LastTimePlayerLoggedIn.AddMinutes(7.5) < DateTime.Now || LastTimePlayerInPlace.AddMinutes(15) < DateTime.Now)
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
                //int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
                //RADAR_REALISM = 0;
                listPositionAllAircraft(GamePlay.gpPlayer(), 1, true, radar_realism: 0);
                listPositionAllAircraft(GamePlay.gpPlayer(), 1, false, radar_realism: 0);
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
        }

        if (tickSinceStarted % 30000 == 1000)
        {

            twcLogServer(null, "Completed Red Objectives (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):", new object[] { });
            twcLogServer(null, MissionObjectivesCompletedString[ArmiesE.Red], new object[] { });
            Timeout(10, () =>
            twcLogServer(null, "Completed Blue Objectives (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):", new object[] { }));
            Timeout(11, () =>
            twcLogServer(null, MissionObjectivesCompletedString[ArmiesE.Blue], new object[] { }));
            Timeout(12, () =>
            twcLogServer(null, showTimeLeft(), new object[] { }));

            stopAI();//for testing
        }

        if (tickSinceStarted == END_MISSION_TICK)// Red battle Success.
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

                if (display)
                {
                    delay += 0.2;
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
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX7"); //Testing for potential causes of warping
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

        Task.Run(() => OnBombExplosion_DoWork(title, mass_kg, pos, initiator, eventArgInt));
    }

    public void OnBombExplosion_DoWork (string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    { 

        //twcLogServer(null, "bombe 1", null);
        bool ai = true;
        if (initiator != null && initiator.Player != null && initiator.Player.Name() != null) ai = false;

        //twcLogServer(null, "bombe 2", null);
        int isEnemy = 1; //0 friendly, 1 = enemy, 2 = neutral
        int terr = GamePlay.gpFrontArmy(pos.x, pos.y);

        //twcLogServer(null, "bombe 3", null);
        if (terr == 00) isEnemy = 2;
        if (!ai && initiator.Player.Army() == terr) isEnemy = 0;
        //twcLogServer(null, "bombe 4", null);


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
                //Advise player of hit/percent/points (Note: Doing all messages & actual creation of craters, smoke etc in -stats.cs
                /*
                if (!ai) twcLogServer(new Player[] { initiator.Player }, "Airport hit: " + (percent * 100).ToString("n0") + "% destroyed " + mass_kg.ToString("n0") + "kg " + individualscore.ToString("n1") + " pts " + (timetofix/3600).ToString("n1") + " hr to repair " , new object[] { }); //+ (timereduction / 3600).ToString("n1") + " hr spent on repairs since last bomb drop"
                */
                //loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, timetofix, stb_FullPath, cratertype);

                //Sometimes, advise all players of percent destroyed, but only when crossing 25, 50, 75, 100% points
                //Timeout(3, () => { if (percent * 100 % 25 < prev_percent * 100 % 25) twcLogServer(null, Mission + " " + (percent * 100).ToString("n0") + "% destroyed ", new object[] { }); });

                //twcLogServer(null, "bombe 8", null);

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

                        MO_DestroyObjective(Mission + "_airfield");
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
                }
                else
                {
                    AirfieldTargets.Remove(ap);
                    AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, Mission, PointsToKnockOut, PointsTaken, DateTime.Now, radius, APPos));
                }
                //twcLogServer(null, "bombe 11", null);
                break;  //sometimes airports are listed twice (for various reasons).  We award points only ONCE for each bomb & it goes to the airport FIRST ON THE LIST (dictionary) in which the bomb has landed.
            }
        }
    }


    //END MISSION WITH WARNING MESSAGES ETC/////////////////////////////////
    //string winner should be "Red" or "Blue" exactly and only!!!
    public void EndMission(int endseconds = 0, string winner = "")
    {
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
                    twcLogServer(null, winner + " has turned the map!", new object[] { });
                    GamePlay.gpHUDLogCenter(winner + " has turned the map. Congratulations, " + winner + "!");
                });
            }
            Timeout(endseconds / 2, () =>
            {
                twcLogServer(null, winner + " has turned the map!", new object[] { });
                GamePlay.gpHUDLogCenter(winner + " has turned the map. Congratulations, " + winner + "!");
            });
            Timeout(endseconds + 15, () =>
            {
                twcLogServer(null, winner + " has turned the map!", new object[] { });
                GamePlay.gpHUDLogCenter(winner + " has turned the map - mission ending soon!");
            });
            Timeout(endseconds + 45, () =>
            {
                twcLogServer(null, winner + " has turned the map!", new object[] { });
                GamePlay.gpHUDLogCenter(winner + " has turned the map - mission ending soon!");
            });
            Timeout(endseconds + 61, () =>
            {
                twcLogServer(null, "Congratulations " + winner + " for turning the map!", new object[] { });

            });
        }
        Timeout(endseconds, () =>
        {
            twcLogServer(null, "Mission is restarting in 1 minute!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission is restarting in 1 minute!!!");
        });
        Timeout(endseconds + 30, () =>
        {
            twcLogServer(null, "Server Restarting in 30 seconds!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Server Restarting in 30 seconds!!!");

            
            //All players who are lucky enough to still be in a plane at this point have saved their plane/it's returned to their team's supply


            //Save map state & data
            double misResult = SaveMapState(winner); //here is where we save progress/winners towards moving the map & front one way or the other; also saves the Supply State

            CheckStatsData(winner); //Save campaign/map state just before final exit.  This is important because when we do (GamePlay as GameDef).gameInterface.CmdExec("exit"); to exit, the -stats.cs will read the CampaignSummary.txt file we write here as the final status for the mission in the team stats.
            
        });
        Timeout(endseconds + 50, () =>
        {
            twcLogServer(null, "Server Restarting in 10 seconds!!!", new object[] { });
            
        });
        Timeout(endseconds + 60, () =>
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
            });
        Timeout(endseconds + 90, () =>  //still doing this as a failsafe but allowing 20 secs to save etc
        {
            //If the CmdExec("exit") didn't work for some reason, we can call OnBattleStoped manually to clean things up, then kill.  This is just a failsafe
            (TWCStatsMission as AMission).OnBattleStoped();//This really horchs things up, basically things won't run after this.  So save until v-e-r-y last.
            Process.GetCurrentProcess().Kill();
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

        if (actor != null && actor is AiAircraft)
        {
            //OK, we have to wait a bit here bec. some ppl use ALT-F11 (ALT-F2) for 'external view' which allows to leave two positions
            //inhabited by bomber pilot & just return to the one position.  But how it actually works is the pilot leaves the aircraft momentarily.
            Timeout(0.5f, () =>
            {
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
    double BlueAirF = 0;
    double BlueAAF = 0;
    double BlueNavalF = 0;
    double BlueGroundF = 0;
    double BluePlanesWrittenOffI = 0;

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

                if (Time.AddSeconds(125).ToUniversalTime() > DateTime.Now.ToUniversalTime())
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

            outputmsg = "Blue Objectives complete (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Blue]) + "<br>" + Environment.NewLine;
            outputmsg += "Red Objectives complete (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Red]) + "<br>" + Environment.NewLine;

            outputmsg += "<br>" + Environment.NewLine + "<br>" + Environment.NewLine;
            outputmsg += campaign_summary; //note: this include .NewLine but not <br>
            if (winner != "") outputmsg += "<br>" + winner.ToUpper() + " HAS TURNED THE MAP! Congratulations, " + winner + "<br>" + Environment.NewLine;

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

        TimeSpan Convert_Ticks = TimeSpan.FromMinutes((720000 - Time.tickCounter()) / 2000);//720000 denotes 6 hours of play
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
    public Tuple<double, string> CalcMapMove(string winner, bool final = true, bool output = true, Player player = null)
    {
        double MapMove = 0;
        string msg = "";
        string outputmsg = "";
        Player[] recipients = null;
        if (player != null) recipients = new Player[] { player };

        if (winner == "Red")
        {
            msg = "Red moved the campaign forward by achieving all Mission Objectives and turning the map!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            return new Tuple<double, string>(1, outputmsg);
        }
        if (winner == "Blue")
        {
            msg = "Blue moved the campaign forward by achieving all Mission Objectives and turning the map!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            return new Tuple<double, string>(-1, outputmsg);
        }

        if (RedTotalF > 3)
        {
            msg = "Red has moved the campaign forward through its " + RedTotalF.ToString("n1") + " total victories!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += RedTotalF / 2000;
        }
        if (BlueTotalF > 3)
        {
            msg = "Blue has moved the campaign forward through its " + BlueTotalF.ToString("n1") + " total victories!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= BlueTotalF / 2000;
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
            msg = "Red has moved the campaign forward " + MissionObjectiveScore[ArmiesE.Red].ToString("n0") + " points by destroying Mission Objectives!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += MissionObjectiveScore[ArmiesE.Red] / 100;
        }

        if (MissionObjectiveScore[ArmiesE.Blue] > 0)
        {
            msg = "Blue has moved the campaign forward " + MissionObjectiveScore[ArmiesE.Blue].ToString("n0") + " points by destroying Mission Objectives!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= MissionObjectiveScore[ArmiesE.Blue] / 100;
        }
        if (RedPlanesWrittenOffI >= 3)
        {
            msg = "Red has lost ground by losing " + RedPlanesWrittenOffI.ToString() + " aircraft in battle!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= (double)RedPlanesWrittenOffI / 200;  //These are LOSSES, so - points for red & + points for blue
        }
        if (BluePlanesWrittenOffI >= 3)
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
        if (MapMove > 1) MapMove = 1;
        if (MapMove < -1) MapMove = -1;

        string word = "Currently, ";
        if (final) word = "Altogether, ";

        if (MapMove > 0)
        {
            msg = word + "this mission has improved Red's campaign position by " + (MapMove * 100).ToString("n0") + " points.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        if (MapMove < 0)
        {
            msg = word + "this mission has improved Blue's campaign position by " + (-MapMove * 100).ToString("n0") + " points.";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
        }
        if (MapMove == 0)
        {
            msg = word + "this mission is a COMPLETE STALEMATE!";
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
    public double SaveMapState(string winner, bool intermediateSave = false)
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
            File.WriteAllText(filepath, newMapState.ToString() + Environment.NewLine + turnString + Environment.NewLine + date + Environment.NewLine + currentContent);
            if (!intermediateSave) MapStateSaved = true;

            //Save mapstate to special directory once @ beginning of mission & again at very end
            if (!MapStateBackedUp || !intermediateSave)
            {

                try
                {
                    double pcDone = calcProportionTimeComplete();

                    //This adds supply in based on how many player participated and how long the mission ran (6 hrs generally) as a basis
                    //then adjusting based on score & whether anyone has turned the map
                    int netRedCount = 15;
                    int netBlueCount = 15;

                    if (TWCStatsMission != null)
                    {
                        //Counting lines in <netstats summary from -stats.cs to get an approximation of how many active pilots there were in-game
                        string netRed = TWCStatsMission.Display_SessionStatsAll(null, 1, false, true); //last true = NEED html version bec we're counting <br>s
                        string netBlue = TWCStatsMission.Display_SessionStatsAll(null, 2, false, true);
                        netRed = netRed.Replace(@"***No Netstats to report***<br>", "");
                        //netRed.Replace()
                        netBlue = netBlue.Replace(@"***No Netstats to report***<br>", "");
                        //netBlue = netBlue.Replace(@"No Nets", "");
                        string target = "<br>";//Q&D way to count how many pilots active during the mission
                        Console.WriteLine("NR " + netRed);
                        Console.WriteLine("NR " + netBlue);
                        netRedCount = netRed.Select((c, i) => netRed.Substring(i)).Count(sub => sub.StartsWith(target)) - 1;
                        netBlueCount = netBlue.Select((c, i) => netBlue.Substring(i)).Count(sub => sub.StartsWith(target)) - 1 ;
                    }

                    Console.WriteLine("Main-ReSupply: " + netRedCount.ToString() + " " + netBlueCount.ToString() );
                    if (netRedCount < 0) netRedCount = 0;
                    if (netBlueCount < 0) netBlueCount = 0;
                    if (netRedCount > 120) netRedCount = 120;
                    if (netBlueCount > 120) netBlueCount = 120;
                    //Take care of changes to supply
                    double redMult = pcDone / 8.0 + 7.0 / 8.0 * pcDone * netRedCount / 20.0;  //must cast to double first . . .
                    double blueMult = pcDone / 8.0 + 7.0 / 8.0 * pcDone * netBlueCount / 20.0;

                    Console.WriteLine("Main-ReSupply: " + netRedCount.ToString() + " " + netBlueCount.ToString() + " " + redMult.ToString() + " " + blueMult.ToString() + " "
                        + redMultAdmin.ToString() + " " + blueMultAdmin.ToString() + " ");

                    //if one side turns the map they get a large increase in aircraft supply while the other side gets little or nothing
                    //if they don't turn the map there is still a slight tweak give the side with more overall victories a few more aircraft 
                    if (winner == "Red") { redMult = 3; blueMult = 0.01; }
                    else if (winner == "Blue") { blueMult = 3; redMult = 0.01; }
                    else if (misResult > 0) redMult +=  misResult / 100.0;
                    else if (misResult < 0) blueMult +=  (-misResult) / 100.0;
                    redMult += redMultAdmin;
                    blueMult += blueMultAdmin;
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyEndMission(redMult, blueMult);

                    Console.WriteLine("Main-ReSupply: " + netRedCount.ToString() + " " + netBlueCount.ToString() + " " + redMult.ToString() + " " + blueMult.ToString() + " "
                        + redMultAdmin.ToString() + " " + blueMultAdmin.ToString() + " ");

                } catch (Exception ex) { Console.WriteLine("MapState Supply Save ERROR: " + ex.ToString()); }

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


                                        /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
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

    double nearAirGroupThreshhold_m = 10000;
    public enum aiorhuman { AI, Mixed, Human };

    public class AirGroupInfo
    {
        public double time; //Battle.time.current;
        public HashSet<AiAirGroup> nearbyAirGroups = new HashSet<AiAirGroup>();  // { get; set; } //those groups that are nearby OR near any nearby aircraft of the same type (greedy)
        public HashSet<AiAirGroup> groupedAirGroups = new HashSet<AiAirGroup>(); //{ get; set; } //groups that have been nearby for that past X iterations, thus counting as part of the same Group
        public Point3d pos { get; set; }
        public Point3d vel { get; set; }
        public int count { get; set; }
        public string type { get; set; }
        public bool isHeavyBomber { get; set; }
        public bool isAI { get; set; }
        public AiActor actor { get; set; }
        public AiAirGroup airGroup { get; set; }
        public bool isLeader { get; set; }
        public AiAirGroup leader { get; set; }


        //Above are individual airgroup/aircraft values - below are the composite values for the entire airgroup ("Air Group Grouping" - AGG) - in case this is the leader of the grouping.  Otherwise blank/default
        public Point3d AGGpos { get; set; }    //exact loc of the primary a/c
        public Point3d AGGavePos { get; set; } //average loc of all a/c
        public Point3d AGGvel { get; set; }
        public int AGGcount { get; set; }
        public double AGGminAlt { get; set; }
        public double AGGmaxAlt { get; set; }
        public double AGGaveAlt { get; set; }
        public string AGGtypeNames { get; set; }
        public string AGGplayerNames { get; set; }
        public aiorhuman AGGAIorHuman { get; set; }
        public string AGGtype { get; set; }
        public bool AGGisHeavyBomber { get; set; }                        
        public Mission mission;



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

            AGGtypeNames = Calcs.GetAircraftType(actor as AiAircraft);
            AGGplayerNames = actor.Name();


            //if (!player_place_set &&  (a.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"


            //bool isAI = isAiControlledPlane2(a);
            //Console.WriteLine("AGI 4");
            string acType = Calcs.GetAircraftType(a);
            isHeavyBomber = false;
            if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType == ("BlenheimMkIV")) isHeavyBomber = true;
            AGGisHeavyBomber = isHeavyBomber;

            string t = a.Type().ToString();
            if (t.Contains("Fighter") || t.Contains("fighter")) type = "F";
            else if (t.Contains("Bomber") || t.Contains("bomber")) type = "B";
            AGGtype = type;

            /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
             + a.CallSign() + " " 
             + a.Type() + " " 
             + a.TypedName() + " " 
             +  a.AirGroup().ID(), new object[] { });
            */
            pos = a.Pos();
            AGGpos = pos;
            AGGmaxAlt = pos.z;
            AGGminAlt = pos.z;
            AGGaveAlt = pos.z;
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


        }

        public void addAG(AiAirGroup ag)
        {
            nearbyAirGroups.Add(ag);
        }
        public void addAGs(HashSet<AiAirGroup> ags)
        {
            nearbyAirGroups.UnionWith(ags);
        }

        //Check if the two a/c are closer than the threshhold and meet other criteria, such as same type of fighter/bomber, and if so add mutually to each other's nearby airgroups list
        public void checkIfNearbyAndAdd(AirGroupInfo agi2)
        {
            Point3d tempos = agi2.pos;
            if (agi2.type == type && pos.distance(ref tempos) <= mission.nearAirGroupThreshhold_m)
            {
                addAG(agi2.airGroup);
                agi2.addAG(airGroup);
                //Console.WriteLine("AGI: Adding {0} {1} {2}", type, pos.distance(ref tempos), mission.nearAirGroupThreshhold_m);
            } else { // Console.WriteLine("AGI: NOT Adding {0} {1} {2}", type, pos.distance(ref tempos), mission.nearAirGroupThreshhold_m); 
            }

        }
        public void mutuallyAddNearbyAirgroups(AirGroupInfo agi2)
        {

                addAGs(agi2.nearbyAirGroups);
                agi2.addAGs(nearbyAirGroups);


        }
    }

    CircularArray<Dictionary<AiAirGroup, AirGroupInfo>> airGroupInfoCircArr = new CircularArray<Dictionary<AiAirGroup, AirGroupInfo>>(4);
    
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

        //Timeout(29, () => { Task.Run(() => groupAllAircraft()); });
        Timeout(9, () => { groupAllAircraft_recurs(); });
        Console.WriteLine("groupAllAircraft: -1");
        groupAllAircraft();
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
                                                    if (!airGroupInfoDict.ContainsKey(airGroup2)) airGroupInfoDict[airGroup2]=new AirGroupInfo(actor2, airGroup2, this, Time.current());
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
                            if (nb !=null) foreach (AiAirGroup ag in nb)
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

                    Dictionary<AiAirGroup, AirGroupInfo> a1 = airGroupInfoCircArr.GetStack(0); //Last iteration
                    Dictionary<AiAirGroup, AirGroupInfo> a2 = airGroupInfoCircArr.GetStack(1); //2nd to last iteration

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
                        airGroupInfoDict[airGroup].groupedAirGroups = nb;  //we start off with the a/c that are nearby us now

                        //We are intersecting here, so if the 2nd hashset is EMPTY or DOESN'T EXIST
                        //we'll end up with a grouping with 0 elements, not even the original a/c.
                        //So we check to make sure the airGroupInfoDict from previous runs exists, and then
                        //also that it's length is >0.  It should always be at least 1 as it should include itself
                        HashSet<AiAirGroup> nba = new HashSet<AiAirGroup>();
                        if (a1 != null && a1.ContainsKey(airGroup))
                        {
                            nba = a1[airGroup].nearbyAirGroups;
                            //if (nba != null)
                            if (nba.Count > 0) airGroupInfoDict[airGroup].groupedAirGroups.IntersectWith(nba);  //Now we eliminate any that were NOT nearby last run
                        }

                        HashSet<AiAirGroup> nbb = new HashSet<AiAirGroup>();
                        if (a2 != null && a2.ContainsKey(airGroup))
                        {
                            nbb = a2[airGroup].nearbyAirGroups;
                            //if (set && nbb != null)
                            if (nbb.Count > 0) airGroupInfoDict[airGroup].groupedAirGroups.IntersectWith(nbb);  //Eliminate any NOT nearby two runs ago
                        }

                        HashSet<AiAirGroup> gag = new HashSet<AiAirGroup> ( airGroupInfoDict[airGroup].groupedAirGroups);

                        //Console.WriteLine("groupAllAircraft: a4.3");
                        if (gag != null) foreach (AiAirGroup ag in gag)
                            {
                                //Console.WriteLine("groupAllAircraft: a4.35");
                                if (DoneAGgrouped.Contains(ag)) continue;

                                //Console.WriteLine("groupAllAircraft: a4.4");
                                if (airGroupInfoDict.ContainsKey(ag))
                                {
                                    complete = false;
                                    airGroupInfoDict[airGroup].groupedAirGroups = gag;  //the airgroups in this grouping
                                    //Console.WriteLine("groupAllAircraft: a4.5");
                                    airGroupInfoDict[ag].leader = airGroup;
                                    airGroupInfoDict[ag].isLeader = false;
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
                        aiorhuman ah = aiorhuman.Human;
                        if (agid.isAI) ah = aiorhuman.AI;
                        double minAlt = agid.pos.z;
                        double maxAlt = agid.pos.z;
                        double aveAlt = 0;
                        Point3d avePos = new Point3d(0, 0, 0);
                        Point3d vwld = new Point3d(0, 0, 0);
                        string typeName = "";
                        string playerName = "";


                        if (airGroupInfoDict[airGroup].nearbyAirGroups != null) foreach(AiAirGroup ag in airGroupInfoDict[airGroup].nearbyAirGroups)
                        {
                            if (!airGroupInfoDict.ContainsKey(ag)) continue;
                            AirGroupInfo agid2 = airGroupInfoDict[ag];
                            c += airGroupInfoDict[ag].count;
                            if (agid2.pos.z > maxAlt) maxAlt = agid2.pos.z;
                            if (agid2.pos.z < minAlt) minAlt = agid2.pos.z;
                            aveAlt += agid2.pos.z * agid2.count;
                            typeName += Calcs.GetAircraftType(agid2.actor as AiAircraft) + " - ";
                            if (!agid2.isAI && (agid2.actor as AiAircraft).Player(0) != null) playerName += (agid2.actor as AiAircraft).Player(0).Name() + " - ";
                            vwld = new Point3d (vwld.x+(double)agid2.count*agid2.vel.x,vwld.y + (double)agid2.count * agid2.vel.y, vwld.z + (double)agid2.count * agid2.vel.z ); //weight the direction vector by the # of aircraft in this airgroup
                            avePos = new Point3d(avePos.x + (double)agid2.count * agid2.pos.x, avePos.y + (double)agid2.count * agid2.pos.y, avePos.z + (double)agid2.count * agid2.pos.z); //weight the direction vector by the # of aircraft in this airgroup


                                    if (airGroupInfoDict[ag].isAI)
                            {
                                if (ah == aiorhuman.Human) ah = aiorhuman.Mixed;
                            }
                            else if (ah == aiorhuman.AI) ah = aiorhuman.Mixed;

                            //can do other calculations here such as averaging speed, altitude, direction, whatever

                        }
                        agid.AGGcount = c;
                        agid.AGGisHeavyBomber = agid.isHeavyBomber;
                        agid.AGGpos = agid.pos;
                        agid.AGGtype = agid.type;
                        //agid.AGGvel = agid.vel;
                        agid.AGGvel = new Point3d (vwld.x/(double)c, vwld.y/ (double)c, vwld.z/ (double)c);  //The 'average' of the direction vectors
                        agid.AGGavePos = new Point3d(avePos.x / (double)c, avePos.y / (double)c, avePos.z / (double)c);  //The 'average' of the position vectors
                        agid.AGGminAlt = minAlt;
                        agid.AGGmaxAlt = maxAlt;
                        agid.AGGaveAlt = aveAlt / (double)c;
                        agid.AGGtypeNames = typeName;
                        agid.AGGplayerNames = playerName;
                        agid.AGGAIorHuman = ah;
                        airGroupInfoDict[airGroup] = agid;

                        Console.WriteLine("Airgroup Grouping: {0} {1} {2} {3:0} {4:0} {5:0} {6} {7} {8} {9} {10} {11:0} {12:0} {13:0} {14:0} {15:0} {16:0} {17:0} ", agid.actor.Name(), agid.count, agid.AGGcount, agid.AGGpos.x, agid.AGGpos.y, agid.AGGpos.z, agid.AGGtype, ah, agid.AGGisHeavyBomber, agid.AGGavePos.x, agid.AGGavePos.y,agid.AGGaveAlt, agid.AGGmaxAlt, agid.AGGtypeNames, agid.AGGplayerNames, agid.AGGvel.x, agid.AGGvel.y, agid.AGGvel.z);
                    }



                }

            }
            Console.WriteLine("groupAllAircraft: 7");
            airGroupInfoCircArr.Push(airGroupInfoDict);  //We save the last ~4 iterations of infodict on a circular array, so that we can go back & look @ what airgroups/leaders were doing in the last few minutes
        } catch (Exception ex )
        { Console.WriteLine("GroupAirgroups ERROR: {0}", ex); }
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
    
    public void listPositionAllAircraft(Player player, int playerArmy, bool inOwnArmy, int radar_realism = -10000000)
    {

        if (radar_realism == -10000000) radar_realism = RADAR_REALISM;
        DateTime d = DateTime.Now;
        // int radar_realism;     //realism = 0 gives exact position, bearing, velocity of each a/c.  We plan to make various degrees of realism ranging from 0 to 10.  Implemented now is just 0=exact, >0 somewhat more realistic    
        // realism = -1 gives the lat/long output for radar files.
        
        Dictionary<AiAirGroup, AirGroupInfo> airGroupInfoDict = airGroupInfoCircArr.GetStack(0); //Most recent iteration of airgroup groupings

        string posmessage = "";
        int poscount = 0;
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
        if (radar_realism >= 1) { wait_s = 5; refreshtime_ms = 60 * 1000; }
        if (radar_realism >= 5) { wait_s = 20; refreshtime_ms = 2 * 60 * 1000; }
        if (radar_realism >= 9) { wait_s = 60; refreshtime_ms = 5 * 60 * 1000; }

        //if admin==true we'll list ALL aircraft regardless of position, radars out et.
        //admin==false we start filtering out depending on whether a radar station has been knocked out etc
        bool admin = false;
        if ((radar_realism == 0) || (radar_realism == -1 && playerArmy == -3)) admin = true;

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
            /* player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
            player_sector = player_sector.Replace(",", ""); // remove the comma */
            player_sector = Calcs.correctedSectorName(this, p.Pos());
            player_place_set = true;
            playername = player.Name();
            playertype = p.Type().ToString();
            if (playertype.Contains("Fighter") || playertype.Contains("fighter")) playertype = "F";
            else if (playertype.Contains("Bomber") || playertype.Contains("bomber")) playertype = "B";
            else playertype = "U";
            string posmessageCSP;

            posmessageCSP = "Radar intercepts are based on your current speed/position: " +
                        player_vel_mph.ToString("F0") + "mph " +
                        player_sector.ToString();
            gpLogServerAndLog(new Player[] { player }, posmessageCSP, null);


        }
        playername_index = playername + "_0";
        if (inOwnArmy) playername_index = playername + "_1";
        playername_index = playername_index + "_" + radar_realism.ToString();

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
                string posmessageIP;
                if (message_data.Item1 == -1) posmessageIP = "New radar returns are in process.  Your previous radar return:";
                else posmessageIP = time_until_new_s.ToString("F0") + "s until " + playername + " can receive a new radar return.  Your previous radar return:";
                gpLogServerAndLog(new Player[] { player }, posmessageIP, null);

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

        //If they haven't requested a return before, or enough time has elapsed, give them a new return  
        if (savenewmessages)
        {
            //When we start to work on the messages we save current messages (either blank or the previous one that was fetched from radar_messages_store)
            //with special time code -1, which means that radar returns are currently underway; don't give them any more until finished.
            radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(-1, radar_messages);

            if (radar_realism > 0) twcLogServer(new Player[] { player }, "Fetching radar contacts, please stand by . . . ", null);




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
                    if (radar_realism == -3)
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
                                                /* player_sector = GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                                                player_sector = player_sector.Replace(",", ""); // remove the comma */
                                                player_sector = Calcs.correctedSectorName(this, p.Pos());
                                                player_place_set = true;
                                            }

                                            bool isAI = isAiControlledPlane2(a);

                                            string acType = Calcs.GetAircraftType(a);
                                            isHeavyBomber = false;
                                            if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType == ("BlenheimMkIV")) isHeavyBomber = true;

                                            type = a.Type().ToString();
                                            if (type.Contains("Fighter") || type.Contains("fighter")) type = "F";
                                            else if (type.Contains("Bomber") || type.Contains("bomber")) type = "B";
                                            if (a == p && radar_realism >= 0) type = "Your position";
                                            /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
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
                                                               (altAGL_ft < 400 && altAGL_ft - 225 < random.Next(175)) || //Less then 300 ft AGL they start to phase out from radar
                                                               (altAGL_ft < 250) || //And, if they are less than 200 feet AGL, they are gone from radar                                                     
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
                                                         (dis_mi >= 10 && altAGL_ft < 650 && altAGL_ft < random.Next(500)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                                                                                              //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                                                                                              //phasing out targets below 250 feet.

                                                         (altAGL_ft < 400 && altAGL_ft - 225 < random.Next(175)) || //Less then 300 ft AGL they start to phase out from radar     
                                                                                                                    //(dis_mi < 10 && altAGL_ft < 400 && altAGL_ft < random.Next(500)) || //Within 10 miles though you really have to be right on the deck before the radar starts to get flakey, less than 250 ft. Somewhat approximating 50 foot alt lower limit.
                                                         (altAGL_ft < 250) || //And, if they are less than 175 feet AGL, they are gone from radar
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
                                }
                            }
                        }
                    }
                    //Using our GROUPED AirGroups instead of chunking through each individual aircraft
                    else
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

                                    poscount = agid.AGGcount;

                                    //Check on any radar outages or restrictions for each army, and remove any radar returns from areas where radar is restricted or inoperative
                                    if (!MO_isRadarEnabledByArea(agid.AGGavePos, admin, radarArmy)) break;

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
                                        player_place_set = true;
                                    }

                                    bool isAI = (agid.AGGAIorHuman == aiorhuman.AI);

                                    string acType = agid.AGGtypeNames;
                                    isHeavyBomber = agid.AGGisHeavyBomber;


                                    type = agid.AGGtype;
                                    if (a == p && radar_realism >= 0) type = "Your position";
                                    /* if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                     + a.CallSign() + " " 
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
                                    /* intcpt_sector = GamePlay.gpSectorName(intcpt.x, intcpt.y).ToString();
                                    intcpt_sector = intcpt_sector.Replace(",", ""); // remove the comma */
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
                                                       (altAGL_ft < 400 && altAGL_ft - 225 < random.Next(175)) || //Less then 300 ft AGL they start to phase out from radar
                                                       (altAGL_ft < 250) || //And, if they are less than 200 feet AGL, they are gone from radar                                                     
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

                                                 (altAGL_ft < 400 && altAGL_ft - 225 < random.Next(175)) || //Less then 300 ft AGL they start to phase out from radar     
                                                                                                            //(dis_mi < 10 && altAGL_ft < 400 && altAGL_ft < random.Next(500)) || //Within 10 miles though you really have to be right on the deck before the radar starts to get flakey, less than 250 ft. Somewhat approximating 50 foot alt lower limit.
                                                 (altAGL_ft < 250) || //And, if they are less than 175 feet AGL, they are gone from radar
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

                            //We'll print only one message per Airgroup, to reduce clutter
                            //twcLogServer(new Player[] { player }, "RPT: " + posmessage + posmessage.Length.ToString(), new object[] { });
                            if (posmessage.Length > 0)
                            {
                                //gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
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
                                                    if (!MO_isRadarEnabledByArea(agid.AGGavePos, admin, radarArmy)) break;

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
                                                        player_place_set = true;
                                                    }

                                                    bool isAI = (agid.AGGAIorHuman == aiorhuman.AI);

                                                    string acType = agid.AGGtypeNames;
                                                    isHeavyBomber = agid.AGGisHeavyBomber;


                                                    type = agid.AGGtype;
                                                    if (a == p && radar_realism >= 0) type = "Your position";
                                                    // if (DEBUG) twcLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                                    // + a.CallSign() + " " 
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
                                                                       (altAGL_ft < 400 && altAGL_ft - 225 < random.Next(175)) || //Less then 300 ft AGL they start to phase out from radar
                                                                       (altAGL_ft < 250) || //And, if they are less than 200 feet AGL, they are gone from radar                                                     
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

                                                                 (altAGL_ft < 400 && altAGL_ft - 225 < random.Next(175)) || //Less then 300 ft AGL they start to phase out from radar     
                                                                                                                            //(dis_mi < 10 && altAGL_ft < 400 && altAGL_ft < random.Next(500)) || //Within 10 miles though you really have to be right on the deck before the radar starts to get flakey, less than 250 ft. Somewhat approximating 50 foot alt lower limit.
                                                                 (altAGL_ft < 250) || //And, if they are less than 175 feet AGL, they are gone from radar
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
                    try
                    {
                        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX9"); //Testing for potential causes of warping
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
                                        pl += " " + act.Pos().x.ToString("n0") + " " + act.Pos().y.ToString("n0");  //2018/09/20 - switching order of x & y & now need to do the same in radar.php
                                    }

                                }
                                msg += py.Name() + " " + py.Army() + " " + pl + "\n";

                            }

                        }

                        sw.WriteLine("Players logged in: " + pycount.ToString() + " Active: " + pyinplace.ToString());
                        sw.WriteLine();

                        sw.WriteLine("MISSION SUMMARY");

                        sw.WriteLine(string.Format("BLUE session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", BlueTotalF,
      BlueAirF, BlueAAF, BlueNavalF, BlueGroundF));
                        sw.WriteLine(string.Format("RED session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", RedTotalF,
      RedAirF, RedAAF, RedNavalF, RedGroundF));
                        sw.WriteLine();

                        sw.WriteLine("Blue Objectives complete (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Blue]));
                        if (playerArmy == -2 || playerArmy == -3) sw.WriteLine(MO_ListRemainingPrimaryObjectives(player: player, army: (int)ArmiesE.Blue, numToDisplay: 50, delay: 0, display: false, html: false));//sw.WriteLine("Blue Primary Objectives: " + MissionObjectivesString[ArmiesE.Blue]);

                        sw.WriteLine("Red Objectives complete (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + (MissionObjectivesCompletedString[ArmiesE.Red]));
                        if (playerArmy == -1 || playerArmy == -3) sw.WriteLine(MO_ListRemainingPrimaryObjectives(player: player, army: (int)ArmiesE.Red, numToDisplay: 50, delay: 0, display: false, html: false));//sw.WriteLine("Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red]);

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

                        sw.WriteLine("CAMPAIGN SUMMARY");

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


                        msg = ListAirfieldTargetDamage(null, -1, false, false); //Add the list of current airport conditions
                        if (msg.Length > 0)
                        {
                            sw.WriteLine();
                            sw.WriteLine("AIRFIELD CONDITION SUMMARY");
                            sw.WriteLine(msg);
                        }

                        sw.WriteLine();
                        string netRed = TWCStatsMission.Display_SessionStatsAll(null, 1, false, false);
                        string netBlue = TWCStatsMission.Display_SessionStatsAll(null, 2, false, false);
                        sw.WriteLine("PLAYER ACTIVITY SUMMARY");
                        sw.WriteLine(netBlue);
                        sw.WriteLine(netRed);

                        sw.Close();


                    }
                    catch (Exception ex) { Console.WriteLine("Radar Write1: " + ex.ToString()); }
                }

                TimeSpan timeDiff = DateTime.Now.Subtract(d);

                var saveradar_realism = radar_realism;
                Timeout(wait_s, () =>
                {
                //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               

                double delay = 0;
                    foreach (var mess in radar_messages)
                    {
                        delay += 0.2;
                        Timeout(delay, () =>
                           {
                               if (saveradar_realism == 0) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                               else if (saveradar_realism >= 0) gpLogServerAndLog(new Player[] { player }, mess.Value, null);
                           });

                    }
                    radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(currtime_ms, radar_messages);

                });//timeout      
            }
        }
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


    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        //listen to events from all missions.
        //Note: this is in Init now, so disabling it here.  See note in Init (below).
        //MissionNumberListener = -1;

        //When battle is started we re-start the Mission tick clock - setting it up to start events
        //happening when the first player connects

        MISSION_STARTED = true;
        if (WAIT_FOR_PLAYERS_BEFORE_STARTING_MISSION_ENABLED) MISSION_STARTED = false;
        START_MISSION_TICK = -1;

        COOP_START_MODE = false;
        if (COOP_START_MODE_ENABLED) COOP_START_MODE = true;
        START_COOP_TICK = -1;
        CheckCoop();  //Start the routine to enforce the coop start/no takeoffs etc

        CampaignMapSuffix = GetMapSuffix(); //This must happen BEFORE EndMissionIfPlayersInactive(); as this reads in the initial campaign state variable & EndMissionIfPlayersInactive(); will overwrite it.
                                            //Timeout(5, () => { SetAirfieldTargets(); });  //Delay for the situation where airfields are loaded via init submissions, which might take a while to load
        SetAirfieldTargets(); //but since we're not doing that now, we can load it immediately.  Airfields MUST be loaded before mission_objectives bec. the airfield list is used to create mission_objectives
        mission_objectives = new MissionObjectives(this, GamePlay); //this must be done AFTER GetMapSuffix as that reads results of previous mission & that is needed for setting up mission objectives
        LoadRandomSubmission(MISSION_ID + "-" + "initairports" + CampaignMapSuffix); // choose which of the airport & front files to load initially


        //Turning EndMissionIfPlayersInactive(); off for TF 4.5 testing.
        EndMissionIfPlayersInactive(); //start routine to check if no players in game & stop the mission if so
        SaveCampaignStateIntermediate(); //save campaign state/score every 10 minutes so that it isn't lost of we end unexpectedly or crash etc

        ReadInitialSubmissions(MISSION_ID + "-stats", 0, 1);
        ReadInitialSubmissions(MISSION_ID + "-supply", 0, 2);
        ReadInitialSubmissions(MISSION_ID + "-initsubmission", 10, 3); //so we can include initsubmissions if we want



        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
        }

        //Delete any old CampaignSummary.txt files so that they are not hanging around causing trouble
        try
        {
            File.Delete(STATSCS_FULL_PATH + "CampaignSummary.txt");
        }
        catch (Exception ex) { Console.WriteLine("CampaignSummary Delete: " + ex.ToString()); }
    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        Console.WriteLine("Battle Stopping -- saving map state & current supply status");

        if (Time.tickCounter()>15000) SaveMapState(""); //A call here just to be safe; we can get here if 'exit' is called etc, and the map state may not be saved yet . . . 
        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
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
                Timeout(wait + random.Next(timespread), () =>
                {

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

    /************************************************************
     * INIT
     * *********************************************************/

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

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);
        //if (TWCComms.Communicator.Instance.Stats != null && TWCStatsMission == null) TWCStatsMission = TWCComms.Communicator.Instance.Stats; 
        TWCStatsMission = TWCComms.Communicator.Instance.Stats;
        if (TWCComms.Communicator.Instance.stb_FullPath != null && TWCComms.Communicator.Instance.stb_FullPath.Length > 0) STATSCS_FULL_PATH = TWCComms.Communicator.Instance.stb_FullPath;
        TWCSupplyMission = TWCComms.Communicator.Instance.Supply;
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
        if (admin_privilege_level(player) >= 1)
        {
            //ADMIN option is set to #9 for two reasons: #1. We can add or remove other options before it, up to 8 other options, without changing the Tab-4-9 admin access.  #2. To avoid accessing the admin menu (and it's often DANGEROUS options) by accident
            //{true/false bool array}: TRUE here indicates that the choice is a SUBMENU so that when it is selected the user menu will be shown.  If FALSE the user menu will disappear.  Also it affects the COLOR of the menu items, which seems to be designed to indicate whether the choice is going to DO SOMETHING IMMEDIATELY or TAKE YOU TO ANOTHER MENU
            GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", "More...", "Objectives Complete", "Objectives Remaining", "Other Suggested Targets", "Current Campaign Status", "Admin options" }, new bool[] { false, false, false, true, false, false, false, false, true });
        }
        else
        {
            GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", "More...", "Objectives Complete", "Objectives Remaining", "More Suggested Targets", "Current Campaign Status" }, new bool[] { false, false, false, true, false, false, false, false });

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
        GamePlay.gpSetOrderMissionMenu(player, true, 2, new string[] { "Your Career Summary", "Your Session Summary", "Netstats/All Player Summary", "More...", "Team Session Summary", "Detail Campaign Team/Session" }, new bool[] { false, false, false, true, false, false });
    }
    private void setSubMenu3(Player player)
    {
        GamePlay.gpSetOrderMissionMenu(player, true, 3, new string[] { "Airport damage summary", "Nearest friendly airport", "On friendly territory?", "Back...", "Aircraft Supply", "Aircraft available to you" }, new bool[] { false, false, false, true, false, false });
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
         * ADMIN SUBMENU (2nd submenu, ID ==1, Tab-4-9)
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
                        twcLogServer(new Player[] { player }, "ENDING MISSION!! If you want to cancel the End Mission command, use Tab-4-9-4 again.  You have 30 seconds to cancel.", new object[] { });
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
         * USER SUBMENU (3rd submenu, ID == 3, Tab-4-4-4)
         * 
         *****************************************************/
        //   GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Detailed Campaign Summary", "Airport damage summary", "Nearest friendly airport", "On friendly territory?","Back..."}, new bool[] { false, false, false, false, false } );
        else if (ID == 3)
        { // main menu

            if (menuItemIndex == 0)
            {
                setSubMenu2(player);
                setMainMenu(player);
            }

            //Airport damage summary, same as <ap
            else if (menuItemIndex == 1)
            {
                //ListAirfieldTargetDamage(player, -1);//list damaged airport of both teams
                Task.Run(() => ListAirfieldTargetDamage(player, -1));
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
                setMainMenu(player);

                
            }
            else if (menuItemIndex == 5)
            {
                if (TWCSupplyMission != null) TWCSupplyMission.DisplayNumberOfAvailablePlanes(player.Army(), player, true);
                setMainMenu(player);
            }
            else if (menuItemIndex == 6)
            {
                //public string Display_AircraftAvailable_ByName(Player player, bool nextAC = false, bool display = true, bool html = false)
                if (TWCStatsMission != null) {
                    TWCStatsMission.Display_AircraftAvailable_ByName(player, nextAC: false, display: true, html: false);
                    Timeout(2.1, () =>
                    {
                        TWCStatsMission.Display_AircraftAvailable_ByName(player, nextAC: true, display: true, html: false);
                    });
                    Timeout(5.0, () =>
                    {
                        twcLogServer(new Player[] { player }, "Note that fighter & bomber pilot careers are separate & aircraft actually available depends on the ace & rank levels in the relevant career for that aircraft", null);
                    });


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
         * USER SUBMENU (2nd submenu, ID ==2, Tab-4-4)
         * 
         *****************************************************/
        //   GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Detailed Campaign Summary", "Airport damage summary", "Nearest friendly airport", "On friendly territory?","Back..."}, new bool[] { false, false, false, false, false } );
        else if (ID == 2)
        { // main menu

            if (menuItemIndex == 0)
            {
                setSubMenu1(player);
                setMainMenu(player);
            }


            //Airport damage summary, same as <ap
            else if (menuItemIndex == 1)
            {
                /*
                 * Formerly <rank or <career for player
                 */
                setMainMenu(player);
                //if (TWCStatsMission != null) TWCStatsMission.Display_AceAndRank_ByName(player);
                TWCStatsMission.Display_AceAndRank_ByName(player);
            }
            else if (menuItemIndex == 2)
            {
                /*
                 * Formerly <session for player session stats
                 */
                setMainMenu(player);
                //if (TWCStatsMission != null) TWCStatsMission.Display_SessionStats(player);
                TWCStatsMission.Display_SessionStats(player);
            }
            
            else if (menuItemIndex == 3)
            {
                /*
                 * Formerly <net for all player "netstats" 
                 */
                setMainMenu(player);
                //if (TWCStatsMission != null) TWCStatsMission.Display_SessionStatsAll(player, 0, true); //player, army (0=all), display or not
                TWCStatsMission.Display_SessionStatsAll(player, 0, true, false); //player, army (0=all), display or not

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
                    if (player.Army() == 2) twcLogServer(new Player[] { player }, "Blue Primary Objectives: " + MissionObjectivesString[ArmiesE.Blue], new object[] { });
                    twcLogServer(new Player[] { player }, "Blue Objectives Completed (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Blue], new object[] { });
                });

                Timeout(1.2, () =>
                {

                    if (player.Army() == 1) twcLogServer(new Player[] { player }, "Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red], new object[] { });

                    twcLogServer(new Player[] { player }, "Red Objectives Completed (" + MissionObjectiveScore[ArmiesE.Red].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Red], new object[] { });
                });
                //Then team stats (kills etc)
                if (TWCStatsMission != null) TWCStatsMission.Display_SessionStatsTeam(player);
            }
            else if (menuItemIndex == 6)
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
                setMainMenu(player);
                //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
                showTimeLeft(player);
                //Experiment to see if we could trigger chat commands this way; it didn't work
                //twcLogServer(new Player[] { player }, "<air", new object[] { });
                //twcLogServer(new Player[] { player }, "<ter", new object[] { });                
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
                MO_ListRemainingPrimaryObjectives(player, player.Army(), 10);                
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
            else if (menuItemIndex == 4)  //MORE (next) menu
            {
                setSubMenu2(player);
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

        if (MissionNumber > -1)
        {
            setMainMenu(player);

            twcLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
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
                            twcLogServer(new Player[] { player }, "Plane: "  
                            + cs + " " + ias, new object[] { });
            */
            //We re-init menu & mission_started here bec. in some situations OnPlayerConnected never happens.  But, they
            //always must choose their army before entering the map, so this catches all players before entering the actual gameplay
            setMainMenu(player);
            if (!MISSION_STARTED) DebugAndLog("First player connected (OnPlayerArmy); Mission timer starting");
            MISSION_STARTED = true;
            twcLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            //twcLogServer(null, "Mission loaded.", new object[] { });
        }
    }
    public override void Inited()
    {
        if (MissionNumber > -1)
        {

            setMainMenu(GamePlay.gpPlayer());
            twcLogServer(null, "Welcome " + GamePlay.gpPlayer().Name(), new object[] { });

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
            if (admin_privilege_level(player) < 2) {
                if (TWCSupplyMission != null) TWCSupplyMission.DisplayNumberOfAvailablePlanes(player.Army(), player, true, false, m);
            } else {
                if (TWCSupplyMission != null) TWCSupplyMission.DisplayNumberOfAvailablePlanes(0, player, true, false, m);
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
            double md = redMultAdmin;
            string ms = msg.Substring(10).Trim();
            try { if (ms.Length > 0) md = Convert.ToDouble(ms); }
            catch (Exception ex) { }

            twcLogServer(new Player[] { player }, "BlueStock admin multiplier set to " + md.ToString("n2") + ". Will add " + md.ToString("n2") + "X the regular mission aircraft stock increase at the end of this mission, plus any stock additions regularly due from mission success", null);
            twcLogServer(new Player[] { player }, "To change this value, just re-enter <bluestock XX before the mission is over.", null);
            twcLogServer(new Player[] { player }, "To reset the value, enter <bluestock 0", null);
            redMultAdmin = md;

        }
        else if (msg.StartsWith("<obj"))
        {
            //only allow this for admins - mostly so that we can check these items via chat commands @ the console
            if (admin_privilege_level(player) >= 2)
            {

                Timeout(0.2, () =>
                {
                    if (player.Army() == 2) twcLogServer(new Player[] { player }, "Blue Primary Objectives: " + MissionObjectivesString[ArmiesE.Blue], new object[] { });
                    twcLogServer(new Player[] { player }, "Blue Objectives Completed (" + MissionObjectiveScore[ArmiesE.Blue].ToString() + " points):" + MissionObjectivesCompletedString[ArmiesE.Blue], new object[] { });
                });

                Timeout(1.2, () =>
                  {

                      if (player.Army() == 1) twcLogServer(new Player[] { player }, "Red Primary Objectives: " + MissionObjectivesString[ArmiesE.Red], new object[] { });

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
            if (admin_privilege_level(player) >= 2)
            {
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
            if (admin_privilege_level(player) >= 2)
            {
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
            listPositionAllAircraft(player, player.Army(), true, radar_realism: 0);
            listPositionAllAircraft(player, player.Army(), false, radar_realism: 0);
            //RADAR_REALISM = saveRealism;

        }
        else if (msg.StartsWith("<rad") && admin_privilege_level(player) >= 2)
        {
            listPositionAllAircraft(player, player.Army(), false, radar_realism: RADAR_REALISM); //enemy a/c  
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

                ListAirfieldTargetDamage(player, -1); //list damaged airport of both teams
            }
            else
            {
                twcLogServer(new Player[] { player }, "Please use Tab-4 menu to check airport status", new object[] { });
            }
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

            twcLogServer(new Player[] { player }, "Admin commands: <stock <bluestock <redstock <coop <trigger <action <debugon <debugoff <logon <logoff <endmission", new object[] { });

        }
        else if ((msg.StartsWith("<help") || msg.StartsWith("<")) &&
            //Don't give our help when any of these typical -stats.cs chat commands are entered
            !(msg.StartsWith("<car") || msg.StartsWith("<ses") || msg.StartsWith("<rank") || msg.StartsWith("<rr")
            || msg.StartsWith("<ter") || msg.StartsWith("<air") || msg.StartsWith("<ac") || msg.StartsWith("<nextac")
            || msg.StartsWith("<net"))

            )
        {
            Timeout(0.1, () =>
            {
                string m = "Commands: <tl Time Left; <rr How to reload; <stock aircraft reserve levels";
                if (admin_privilege_level(player) >= 2) m += "; <admin";
                twcLogServer(new Player[] { player }, "Commands: <tl Time Left; <rr How to reload; <stock aircraft reserve levels; <admin", new object[] { });
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

    //Ranges 0 to 1.  0= just started, 1=full mission time complete
    public double calcProportionTimeComplete()
    {
        double tickSinceStarted = Time.tickCounter() - START_MISSION_TICK;
        double perc = tickSinceStarted / ((double)END_MISSION_TICK);

        if (perc < 0) perc = 0;
        if (perc > 1) perc = 1;
        return perc;
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
        string msg = "Time left in mission " + MISSION_ID + ": " + missiontimeleft;

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
        DateTime currentDate = DateTime.Now;
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

        int destroyminutes = 90;//Destroy AI a/c this many minutes after they are spawned in.


        Timeout(1.0, () => // wait 1 second for human to load into plane
        {
                /* if (DEBUG) twcLogServer(null, "DEBUGC: Airgroup: " + a.AirGroup() + " " 
                  + a.CallSign() + " " 
                  + a.Type() + " " 
                  + a.TypedName() + " " 
                  +  a.AirGroup().ID(), new object[] { });
                */

            if (a != null && isAiControlledPlane2(a))
            {


                int ot = (destroyminutes) * 60 - 10; //de-spawns 10 seconds before new sub-mission spawns in.
                                                     //int brk=(int)Math.Round(19/20);


                    /* if (DEBUG) twcLogServer(null, "DEBUGD: Airgroup: " + a.AirGroup() + " " 
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

    /*
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

    //was osk_BlueObjDescription
    Dictionary<ArmiesE, string> MissionObjectivesString = new Dictionary<ArmiesE, string>()
    {    {ArmiesE.Red, "" },
         {ArmiesE.Blue, "" }
    };

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

    //Amount of points require in case percent of primary is less than 100% but more than MO_PercentPrimaryTargetsRequired
    //This allows mission to be turned in case one objective is malfunctioning or super-difficult - by hitting some other alternate targets
    public Dictionary<ArmiesE, double> MO_PointsRequiredWithMissingPrimary = new Dictionary<ArmiesE, double>() {
        {ArmiesE.Red, 16 },
        {ArmiesE.Blue, 16 }
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

    Dictionary<ArmiesE, List<String>> MissionObjectivesSuggested = new Dictionary<ArmiesE, List<String>>() {
        {ArmiesE.Red, new List<String>() },
        {ArmiesE.Blue, new List<String>() }
    };

    public enum MO_TriggerType { Trigger, Static, Airfield };
    public enum MO_ObjectiveType { Radar, AA, Ship, Building, Fuel, Airfield, Aircraft, Vehicles, Bridge, Dam, Dock, RRYard, Railroad, Road, AirfieldComplex, FactoryComplex, ArmyBase };
    //type Airfield is the auto-entered list of airfield objectives (every active airport in the game) whereas AirfieldComplex could be an additional specific target on or near an airfield

    public class MissionObjective
    {
        //public string TriggerName { get; set; }
        public string ID { get; set; } //unique name, often the Triggername or static name
        public string Name { get; set; } //Name the will be displayed to the public in messages etc
        public int AttackingArmy { get; set; } // Army this is an objective for (ie, whose task is to destroy it); can be 1=red, 2=blue,0=none
        public int OwnerArmy { get; set; } // Army that owns this object (ie, is harmed if it is destroyed)
        public string FlakID { get; set; } //Flak area associated with this objective.  Flak area codes & associated .mis files are identified in FlakMissions dictionary defined below
        public bool IsEnabled { get; set; } //TODO: This is only partially implemented.  But could be used in case of bad data or whatever to just disable the objective.
        public Mission.MO_ObjectiveType MOObjectiveType { get; set; }
        public Mission.MO_TriggerType MOTriggerType { get; set; }
        public bool IsPrimaryTarget { get; set; } //One of the primary/required targets for this mission?
        public double PrimaryTargetWeight { get; set; } //If we select primary targets randomly etc, is this one that could be selected? Percentage weight 0-100, 0 means never chosen.
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
        public string Comment { get; set; } //PRIVATE comment, ie for developers, internal notes, etc, not for display to end users
        public Mission msn;
        public MissionObjective(Mission m)
        {
            msn = m;
        }
        //RADAR TRIGGER initiator
        public MissionObjective(Mission m, string tn, string n, string flak, int ownerarmy, double pts, string t, double p, double x, double y, double d, double e, bool pt, double ptp, string comment)
        {

            msn = m;
            MOObjectiveType = MO_ObjectiveType.Radar;
            MOTriggerType = MO_TriggerType.Trigger;
            TriggerName = tn;
            ID = tn;
            Name = n;
            FlakID = flak;

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
            /* string keyp = Calcs.doubleKeypad(Pos);
            Sector = msn.GamePlay.gpSectorName(x, y).ToString() + "." + keyp;
            Sector = Sector.Replace(",", ""); // remove the comma */
            Sector = Calcs.correctedSectorNameDoubleKeypad(msn, Pos);
            TriggerDestroyRadius = d;
            RadarEffectiveRadius = e;
            Destroyed = false;


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

            Destroyed = false;


            IsPrimaryTarget = false;
            PrimaryTargetWeight = ptp;
            Comment = "Auto-generated from in-game airports list";
        }



        //TRIGGER initiator (for all types except RADAR & AIRFIELD)
        public MissionObjective(Mission m, MO_ObjectiveType mot, string tn, string n, string flak, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, double ptp, string comment)
        {

            msn = m;
            MOObjectiveType = mot;
            MOTriggerType = MO_TriggerType.Trigger;
            TriggerName = tn;
            ID = tn;
            Name = n;
            FlakID = flak;

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



            TriggerDestroyRadius = d;
            Destroyed = false;


            IsPrimaryTarget = pt;
            PrimaryTargetWeight = ptp;
            Comment = comment;
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
                    + PrimaryTargetWeight.ToString() + "\t" + Points.ToString() + "\t" + Destroyed.ToString() + "\t" + Pos.x.ToString() + "\t" + Pos.y.ToString() + "\t" + Sector + "\t" + RadarEffectiveRadius.ToString() + "\t" + TriggerName + "\t"
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
            RadarPositionTriggersSetup();
            MissionObjectiveTriggersSetup();
            msn.MO_MissionObjectiveAirfieldsSetup(mission, gameplay); //must do this after the Radar & Triggers setup, as it uses info from those objectives
            SelectSuggestedObjectives();

            //Get new objectives for winner if they have turned the map OR read in the old objectives if not
            if (msn.MapPrevWinner == "Red")
            {
                msn.MO_SelectPrimaryObjectives(1);
                msn.MO_ReadPrimaryObjectives(2);
            }
            else if (msn.MapPrevWinner == "Blue")
            {
                msn.MO_SelectPrimaryObjectives(2);
                msn.MO_ReadPrimaryObjectives(1);
            }
            else
            {
                msn.MO_ReadPrimaryObjectives(2);
                msn.MO_ReadPrimaryObjectives(1);
            }
            //Now write the new objective list to file
            msn.MO_WritePrimaryObjectives();

            //load the flak for the areas that have primary objectives
            msn.MO_LoadAllPrimaryObjectiveFlak(FlakMissions);

            //Write list of triggers to a file in simulated .mis format so that it is easy to verify all triggers have been read in similar
            //to the .mis file
            msn.MO_WriteOutAllMissionObjectives(msn.MISSION_ID + "-mission_objectives_mis_format.txt", true);
            msn.MO_WriteOutAllMissionObjectives(msn.MISSION_ID + "-mission_objectives_complete.txt", false);
        }

        //few little sanity checks on the objective data
        public bool MO_SanityChecks(string tn, string n)
        {
            if (msn.MissionObjectivesList.Keys.Contains(tn))
            {
                Console.WriteLine("*************MissionObjective initialize WARNING****************");
                Console.WriteLine("MissionObjective initialize: Objective Trigger ID " + tn + " : " + n + " IS DUPLICATED in the .cs file.  This duplicate occurence will be ignored.");
                return false;
            }
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
            return true;

        }

        public void addRadar(string n, string flak, int ownerarmy, double pts, string tn, string t, double p, double x, double y, double d, double e, bool pt, double ptp = 100, string comment = "")
        {
            if (!MO_SanityChecks(tn, n)) return;
            msn.MissionObjectivesList.Add(tn, new MissionObjective(msn, tn, n, flak, ownerarmy, pts, t, p, x, y, d, e, pt, ptp, comment));

        }

        public void addTrigger(MO_ObjectiveType mot, string n, string flak, int ownerarmy, double pts, string tn, string t = "", double p = 50, double x = 0, double y = 0, double d = 100, bool pt = false, double ptp = 100, string comment = "")
        {
            if (!MO_SanityChecks(tn, n)) return;
            //MissionObjective                                    (Mission m, MO_ObjectiveType mot,  string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, bool ptp, string comment)
            msn.MissionObjectivesList.Add(tn, new MissionObjective(msn, mot, tn, n, flak, ownerarmy, pts, t, p, x, y, d, pt, ptp, comment));
        }

        //TODO: Flak doesn't DO ANYTHING yet, it is just in the addRadar & addTrigger methods so we can use it later if desired.
        public void RadarPositionTriggersSetup()
        {

            //ID is the ID used in the [Trigger] portion of the .mis file. The central portion of the line can be copy/pasted from the  .mis file (then lightly edited)

            //MissionObjective(Name,          Flak ID, OwnerArmy,points,ID,Trigger Type,Trigger percentage, location x, location y, trigger radius, radar effective radius, isPrimaryTarget, PrimaryTargetWeight (0-100), comment) {
            addRadar("Westgate Radar",          "WesR", 1, 1, "BTarget14R", "TGroundDestroyed", 39, 244791, 262681, 150, 25000, false, 50, "");
            addRadar("Sandwich Radar",          "SanR", 1, 1, "BTarget15R", "TGroundDestroyed", 75, 248739, 253036, 200, 25000, false, 50, "");
            addRadar("Deal Radar",              "DeaR", 1, 1, "BTarget16R", "TGroundDestroyed", 75, 249454, 247913, 200, 25000, false, 50, "");
            addRadar("Dover Radar",             "DovR", 1, 1, "BTarget17R", "TGroundDestroyed", 75, 246777, 235751, 200, 25000, false, 50, "");
            addRadar("Brookland Radar",         "BroR", 1, 1, "BTarget18R", "TGroundDestroyed", 75, 212973, 220079, 200, 25000, false, 50, "");
            addRadar("Dungeness Radar",         "DunR", 1, 1, "BTarget19R", "TGroundDestroyed", 50, 221278, 214167, 200, 25000, false, 50, "");
            addRadar("Eastbourne Radar",        "EasR", 1, 1, "BTarget20R", "TGroundDestroyed", 75, 178778, 197288, 200, 25000, false, 50, "");
            addRadar("Littlehampton Radar",     "LitR", 1, 1, "BTarget21R", "TGroundDestroyed", 76, 123384, 196295, 200, 25000, false, 50, "");
            addRadar("Ventnor Radar",           "VenR", 1, 1, "BTarget22R", "TGroundDestroyed", 75, 70423, 171706, 200, 25000, false, 50, "");
            addRadar("Radar Communications HQ", "HQR", 1, 6,  "BTarget28", "TGroundDestroyed",   61, 180207, 288435, 200, 100000, false, 50, "");
            addRadar("Radar Poole",             "PooR", 1, 2, "BTarget23R", "TGroundDestroyed",  75,  15645,  170552, 200, 25000,  false, 50, "");


            addRadar("Oye Plage Freya Radar",         "OypR", 2, 1, "RTarget28R", "TGroundDestroyed", 61,294183, 219444,  50, 15000, false, 35, "");
            addRadar("Coquelles Freya Radar",         "CoqR", 2, 1, "RTarget29R", "TGroundDestroyed", 63,276566, 214150,  50, 15000, false, 35, "");
            addRadar("Dunkirk Freya Radar",           "DuRN", 2, 1, "RTarget38R", "TGroundDestroyed", 77,341887, 232695,  100, 15000, false, 35, "");
            addRadar("Herderlot-Plage Freya Radar",   "HePR", 2, 1, "RTarget39R", "TGroundDestroyed", 85,341866, 232710,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Berck Freya Radar",             "BrkR", 2, 1, "RTarget40R", "TGroundDestroyed", 86,263234, 153713,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar Dieppee",                 "DieR", 2, 1, "RTarget41R", "TGroundDestroyed", 85,232576, 103318,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar Le Treport",              "TreR", 2, 1, "RTarget42R", "TGroundDestroyed", 86,250599, 116531,  50, 15000, false, 15, ""); // Mission in mission file
            addRadar("Radar Somme River",             "SomR", 2, 1, "RTarget43R", "TGroundDestroyed", 86,262560, 133020,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar AMBETEUSE",               "AmbR", 2, 1, "RTarget44R", "TGroundDestroyed", 86,266788, 197956,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar BOULOGNE",                "BlgR", 2, 1, "RTarget45R", "TGroundDestroyed", 85,264266, 188554,  50, 15000, false, 35, ""); //Mission in mission file           
            addRadar("Radar Le Touquet",              "L2kR", 2, 1, "RTarget46R", "TGroundDestroyed", 66,266625, 169936,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar Dieppe",                  "FreR", 2, 1, "RTarget47R", "TGroundDestroyed", 99, 185931, 88085, 50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Veulettes-sur-Mer Radar",       "VeuR", 2, 1, "RTarget48R", "TGroundDestroyed", 100, 195165,93441,  50, 15000, false, 15, "");//Mission in mission file
            addRadar("Le Havre Freya Radar",          "LhvR", 2, 1, "RTarget49R", "TGroundDestroyed", 100, 157636,60683,  50, 15000, false, 35, "");//Mission in mission file
            addRadar("Ouistreham Freya Radar",        "OuiR", 2, 1, "RTarget50R", "TGroundDestroyed", 100, 135205,29918,  50, 15000, false, 35, "");// Mission in mission file
            addRadar("Bayeux Beach Freya Radar",      "BayR", 2, 1, "RTarget51R", "TGroundDestroyed", 100, 103641,36893,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Beauguillot Beach Freya Radar", "BchR", 2, 1, "RTarget52R", "TGroundDestroyed", 100, 65637, 44013,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar Tatihou",                 "TatR", 2, 1, "RTarget53R", "TGroundDestroyed", 77, 60453,  63873,  50, 15000, false, 15, ""); //Mission in mission file
            addRadar("Radar Querqueville",            "QueR", 2, 1, "RTarget54R", "TGroundDestroyed", 100, 17036, 77666,  50, 15000, false, 35, ""); // Mission in mission file

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

        public void MissionObjectiveTriggersSetup()
        {
            //Format: addTrigger(MO_ObjectiveType.Building (Aircraft, airport, etc), "Name,                      OwnerArmy,Points,ID,TriggerType,PercRequired,XLoc,YLoc,Radius,IsPrimaryTarget,IsPrimaryTargetWeight,Comment "");

            //BLUE TARGETS
            addTrigger(MO_ObjectiveType.Aircraft, "Littlestone Bombers", "Litt", 1, 2, "BTarget1", "TGroundDestroyed", 20, 222303, 221176, 300, false, 100, "");
            addTrigger(MO_ObjectiveType.AirfieldComplex, "Redhill Bomber Base", "Redh", 1, 2, "BTarget2", "TGroundDestroyed", 20, 143336, 240806, 550, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Ashford Train Depot", "Ashf", 1, 2, "BTarget3", "TGroundDestroyed", 20, 214639, 235604, 100, false, 100, "");
            addTrigger(MO_ObjectiveType.Aircraft, "Manston aircraft", "Mans", 1, 2, "BTarget4", "TGroundDestroyed", 75, 247462, 259157, 250, false, 100, "");
            addTrigger(MO_ObjectiveType.Vehicles, "British Armor @ Dover", "Dove", 1, 2, "BTarget5", "TGroundDestroyed", 80, 243887, 236956, 200, false, 100, "");
            addTrigger(MO_ObjectiveType.Vehicles, "British Armor @ CreekMouth", "Bext", 1, 2, "BTarget6", "TGroundDestroyed", 50, 159687, 275015, 200, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Diesel fuel London south docks", "Lond", 1, 3, "BTarget6S", "TGroupDestroyed", 70, 154299, 273105, 100, false, 100, " ");//removed all ships "Designation S" used oil storage instead
            addTrigger(MO_ObjectiveType.Fuel, "Hydrogen Storage @ London south docks", "Lond", 1, 3, "BTarget7S", "TGroundDestroyed", 80, 155050, 273258, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Ethanol Storage @ London south docks", "Lond", 1, 2, "BTarget8S", "TGroundDestroyed", 80, 155823, 273221, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Liquid Oxygen @ Beckton", "Bext", 1, 2, "BTarget9S", "TGroundDestroyed", 80, 157899, 273957, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Kerosene Storage @ Beckton", "Bext", 1, 2, "BTarget10S", "TGroundDestroyed", 80, 157547, 274527, 100, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "High Octane aircraft fuel @ Beckton", "Bext", 1, 2, "BTarget11S", "TGroundDestroyed", 80, 158192, 274864, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "87 octane fuelstorage @ Beckton", "Bext", 1, 2, "BTarget12S", "TGroundDestroyed", 63, 157899, 275256, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Peroxide Storage @ Beckton", "Bext", 1, 2, "BTarget13S", "TGroundDestroyed", 66, 157092, 275312, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.AA, "AAA London area", "Lond", 1, 2, "BTarget13A", "TGroundDestroyed", 63, 160567, 275749, 100, false, 4, "");
            addTrigger(MO_ObjectiveType.AA, "AAA London area", "Lond", 1, 2, "BTarget14A", "TGroundDestroyed", 63, 160025, 273824, 100, false, 4, "");
            addTrigger(MO_ObjectiveType.Fuel, "Ditton fuel refinery", "Ditt", 1, 2, "BTarget24", "TGroundDestroyed", 75, 185027, 252619, 100, false, 100, "");// fixed triggers missing
            addTrigger(MO_ObjectiveType.Fuel, "Ditton fuel Storage", "Ditt", 1, 2, "BTarget25", "TGroundDestroyed", 80, 186057, 251745, 100, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Maidstone train repair station ", "Ditt", 1, 2, "BTarget26", "TGroundDestroyed", 85, 189262, 249274, 100, false, 100, "");
            //addTrigger(MO_ObjectiveType.Building, "Billicaray Factory", "RaHQ", 1, 2, "BTarget27", "TGroundDestroyed", 85, 180141, 288423, 150, false, 100, ""); //So in the .mis file BTarget27 is 180xxx / 288xxx which is the Billicaray area.  I don't know if we have flak for that?  Flak 'Tunb' is definitely noit going to work. Flug 2018/10/08       
            addTrigger(MO_ObjectiveType.Building, "Tunbridge Wells Armory", "Tunb", 1, 2, "BTarget27", "TGroundDestroyed", 85, 173778, 233407, 100, false, 100, ""); //This target was left out of the .cs and .mis files until now, but I'm pretty sure it was what is intended for Tunbridge Wells Armory.  So I added it to the .mis and .cs files right now. Flug 2018/10/08
            addTrigger(MO_ObjectiveType.Building, "Bulford Army Facility", "Bulf", 1, 4, "BTarget29", "TGroundDestroyed", 90, 35872, 236703, 200, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Wooleston Spitfire Shop ", "Wool", 1, 2, "BTarget30", "TGroundDestroyed", 81, 56990, 203737, 100, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Swindon Aircraft repair Station", "Swin", 1, 6, "BTarget31", "TGroundDestroyed", 75, 29968, 279722, 300, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Reading Engine Workshop ", "Read", 1, 4, "BTarget32", "TGroundDestroyed", 83, 84241, 267444, 300, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Propeller repair Portsmouth", "Port", 1, 2, "BTarget33", "TGroundDestroyed", 81, 76446, 193672, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Diesel Storage Portsmouth", "Port", 1, 6, "BTarget34", "TGroundDestroyed", 75, 76476, 193844, 50, false, 100, ""); //This might have wrong location? 9/24
            addTrigger(MO_ObjectiveType.Building, "Boiler Repair Shop Portsmouth", "Port", 1, 2, "BTarget35", "TGroundDestroyed", 71, 76317, 193904, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Main Fuel Portsmouth ", "Port", 1, 2, "BTarget36", "TGroundDestroyed", 75, 76378, 194163, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Depth Charge Workshop Portsmouth", "Port", 1, 2, "BTarget37", "TGroundDestroyed", 83, 76720, 194082, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Liquid Oxygen Storage Portsmouth", "Port", 1, 6, "BTarget38", "TGroundDestroyed", 75, 76805, 193918, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Wood Alcohol Fuel Storage Portsmouth", "Port", 1, 4, "BTarget39", "TGroundDestroyed", 98, 77392, 193942, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Fuel, "Portsmouth Hydrogen Storage", "Port", 1, 4, "BTarget40", "TGroundDestroyed", 95, 77317, 193860, 50, false, 100, ""); //This is in Portsmouth    .  
            addTrigger(MO_ObjectiveType.Building, "Portsmouth Torpedo Facility", "Port", 1, 4, "BTarget41", "TGroundDestroyed", 72, 76855, 194410, 50, false, 100, ""); //This is in Portsmouth.fixed 9/19 fatal
            addTrigger(MO_ObjectiveType.Fuel, "Guildford High Octane Plant", "Guil", 1, 5, "BTarget42", "TGroundDestroyed", 89, 112441, 243834, 200, false, 100, ""); //Guildford Target added 9/20
            addTrigger(MO_ObjectiveType.Fuel,  "Sheerness Diesel Fuel Storage", "Quee", 1, 2,"BTarget43", "TGroundDestroyed", 63, 204654, 268378, 50, false, 100, "");//Sheerness Diesel Fuel Storage
            addTrigger(MO_ObjectiveType.Building,"Queensborough Navigational jamming facilities", "Quee",1,2,  "BTarget44", "TGroundDestroyed", 74, 204638, 265195, 50, false, 100,""); // "Queensborough Navigational jamming facilities"
			addTrigger(MO_ObjectiveType.Building, "Queensborough Radio communications center",    "Quee",1,2,  "Btarget45", "TGroundDestroyed", 74, 204722, 265252, 50, false, 100,"");  // "Queensborough Radio communications center"
			addTrigger(MO_ObjectiveType.Building,"Queensborough radio tramsmission booster" ,     "Quee",1,2,  "BTarget46", "TGroundDestroyed", 74, 204570, 265131, 50,  false, 100,""); //  "Queensborough radio tramsmission booster"
			addTrigger(MO_ObjectiveType.Building,"Queensborough Electrical Research Facility",    "Quee",1,2,  "BTarget47", "TGroundDestroyed", 74, 204716, 265140, 50, false, 100,""); //  "Queensborough Electrical Research Facility"
			
            /*
              BTarget6S TGroundDestroyed 70 154299 273105 100     "Diesel fuel London south docks", 1, 3, "
              BTarget7S TGroundDestroyed 80 155050 273258 50      "Hydrogen Storage @ London south docks", 
              BTarget8S TGroundDestroyed 80 155823 273221 50      "Ethanol Storage @ London south docks", 1
              BTarget9S TGroundDestroyed 80 157899 273957 50      "Liquid Oxygen @ Beckton", 1, 2, "BTar
              BTarget10S TGroundDestroyed 80 157547 274527 100    "Kerosene Storage @ Beckton", 1, 2, 
              BTarget11S TGroundDestroyed 80 158192 274864 50     "High Octane aircraft fuel @ Beckton
              BTarget12S TGroundDestroyed 63 157899 275256 50     "87 octane fuelstorage @ Beckton", 1
              BTarget13S TGroundDestroyed 66 157092 275312 50     "Peroxide Storage @ Beckton", 1, 2, 
			  
			    BTarget43 TGroundDestroyed 63 204654 268378 50    "Sheerness Diesel Fuel Storage"
                BTarget44 TGroundDestroyed 74 204638 265195 50    "Queensborough Navigational jamming facilities"
                Btarget45 TGroundDestroyed 74 204722 265252 50     "Queensborough Radio communications center"
                BTarget46 TGroundDestroyed 74 204570 265131 50     "Queensborough radio tramsmission booster"
                BTarget47 TGroundDestroyed 74 204716 265140 50     "Queensborough Electrical Research Facility"
            */


            //RED TARGETS
            addTrigger(MO_ObjectiveType.Vehicles, "Bapaume Rail Transit Station", "Bapu", 1, 2, "RTarget0", "TGroundDestroyed", 83, 354623, 121058, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Vehicles, "Motorpool near Grand-Fort Philippe", "MPGP", 2, 2, "RTarget1", "TGroundDestroyed", 50, 299486, 220998, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "St. Omer Ball bearing Factory", "Stom", 2, 2, "RTarget2", "TGroundDestroyed", 33, 313732, 192700, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Estree Fuel Depot", "Estr", 2, 3, "RTarget3", "TGroundDestroyed", 40, 280182, 164399, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Synthetic Fuel", "Boul", 2, 2, "RTarget4", "TGroundDestroyed", 60, 265005, 190321, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.RRYard, "Calais Rail Yard", "Cala", 2, 2, "RTarget5", "TGroundDestroyed", 60, 283995, 215369, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Hydrogen", "Cala", 2, 2, "RTarget6", "TGroundDestroyed", 60, 284867, 216414, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Calais Main Fuel", "Cala", 2, 2, "RTarget7", "TGroundDestroyed", 60, 285518, 217456, 100, false, 100, ""); //g
            addTrigger(MO_ObjectiveType.Building, "Calais LOX", "Cala", 2, 2, "RTarget8", "TGroundDestroyed", 60, 285001, 215944, 100, false, 100, ""); //g
            addTrigger(MO_ObjectiveType.Fuel, "Calais Torpedo Factory", "Cala", 2, 2, "RTarget9", "TGroundDestroyed", 60, 284831, 216887, 100, false, 100, ""); //g
            addTrigger(MO_ObjectiveType.Fuel, "Calais Diesel Storage", "Cala", 2, 2, "RTarget10", "TGroundDestroyed", 60, 285040, 217547, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Aviation", "Boul", 2, 2, "RTarget11", "TGroundDestroyed", 43, 265591, 189902, 100, false, 100, "");   //g 
            addTrigger(MO_ObjectiveType.Building, "Boulogne Diesel", "Boul", 2, 2, "RTarget12", "TGroundDestroyed", 50, 266651, 187088, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Benzine", "Boul", 2, 2, "RTarget13", "TGroundDestroyed", 52, 266160, 189276, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne LOX", "Boul", 2, 2, "RTarget14", "TGroundDestroyed", 50, 264515, 188950, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Boulogne Ethanol", "Boul", 2, 2, "RTarget15", "TGroundDestroyed", 50, 264984, 189378, 100, false, 100, ""); //g
            addTrigger(MO_ObjectiveType.Fuel, "Arras Main Fuel", "Arra", 2, 4, "RTarget16", "TGroundDestroyed", 50, 350605, 142047, 50,
                     false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Arras Rubber Factory", "Arra", 2, 3, "RTarget17", "TGroundDestroyed", 50, 352039, 141214, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "St Ouen AAA Factory", "Stou", 2, 2, "RTarget18", "TGroundDestroyed", 50, 303445, 114053, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Abbeville Fuel", "Abbe", 2, 2, "RTarget19", "TGroundDestroyed", 50, 285075, 121608, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Dieppe Fuel", "Diep", 2, 2, "RTarget20", "TGroundDestroyed", 50, 229270, 101222, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Le Treport Fuel", "LeTr", 2, 2, "RTarget21", "TGroundDestroyed", 50, 250477, 116082, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Poix Nord Fuel Storage", "Poix", 2, 3, "RTarget22", "TGroundDestroyed", 50, 293827, 84983, 150, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Chemical Research Facility", "Cala", 2, 2, "RTarget23", "TGroundDestroyed", 75, 285254, 216717, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Optical Research Facility", "Cala", 2, 2, "RTarget24", "TGroundDestroyed", 100, 285547, 216579, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Chemical Storage", "Cala", 2, 2, "RTarget25", "TGroundDestroyed", 75, 285131, 216913, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Calais Rations Storage", "Cala", 2, 1, "RTarget26", "TGroundDestroyed", 78, 284522, 216339, 50, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Gunpowder Facility", "Cala", 2, 2, "RTarget27", "TGroundDestroyed", 50, 284898, 216552, 50, false, 100, "");  //g  //  addTrigger(MO_ObjectiveType.Ship, "Minensuchboote", "Abbe", 2, 2, "RTarget30S", "TGroupDestroyed", 90, 263443, 181488, 0, false, 100, "0_Chief  Minensuchtboot");   //removed from the mission
            addTrigger(MO_ObjectiveType.Fuel, "Arras Fuel Storage 2", "Arra", 2, 3, "RTarget31", "TGroundDestroyed", 75, 351371, 141966, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Watten Armory", "watt", 2, 2, "RTarget32", "TGroundDestroyed", 60, 310395, 200888, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Half Track Factory", "Dunk", 2, 2, "RTarget33", "TGroundDestroyed", 50, 314794, 224432, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Steel Mill Dunkirk", "Dunk", 2, 2, "RTarget34", "TGroundDestroyed", 75, 315081, 224145, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Brass Smelter Dunkirk", "Dunk", 2, 2, "RTarget35", "TGroundDestroyed", 75, 314832, 223389, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Fuel, "Diesel Storage Dunkirk", "Dunk", 2, 2, "RTarget36", "TGroundDestroyed", 75, 314482, 223882, 200, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Ammunition Warehouse Dunkirk", "Dunk", 2, 3, "RTarget37", "TGroundDestroyed", 75, 313878, 223421, 100, false, 100, "");  //g
            addTrigger(MO_ObjectiveType.Building, "Low smoke Diesel Le Havre", "Havr", 2, 2, "RTarget38", "TGroundDestroyed", 70, 161702, 52073, 100, false, 100, "");  //This is in Le Havre, fuel tanks area. I added 3-4 jerry cans to the area in the .mis so it is a valid target now //g
            addTrigger(MO_ObjectiveType.Building, "Calais Water treatment", "Cala", 2, 1, "9A", "TGroundDestroyed", 63, 296130, 218469, 50, false, 2, ""); //I think the locations of the AAA batteries are off? Ok, checking with the .mis file, the order was just reversed and the wrong name with the wrong battery. 1A..9A vs 9A..1A.  Now fixed to match .mis file 9/19/2018
            addTrigger(MO_ObjectiveType.Building, "Coastal Command Calais", "Cala", 2, 1, "8A", "TGroundDestroyed", 75, 294090, 85100, 100, false, 2, "");
            addTrigger(MO_ObjectiveType.Building, "Calais Rope Factory", "Cala", 2, 1, "7A", "TGroundDestroyed", 66, 293279, 84884, 100, false, 2, "");
            addTrigger(MO_ObjectiveType.Building, "Amunition Wharehouse", "Boul", 2, 1, "1B", "TGroundDestroyed", 70, 264252, 189991, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "Fuel Reasearch Facility", "Boul", 2, 1, "2B", "TGroundDestroyed", 47, 265063, 190506, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "Radio Jamming Transmitter", "Boul", 2, 1, "3B", "TGroundDestroyed", 51, 265251, 190259, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "Naval  Reasearch Facility", "Boul", 2, 1, "4B", "TGroundDestroyed", 62, 264692, 189709, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "Boulogne Army HQ", "Boul", 2, 1, "5B", "TGroundDestroyed", 54, 265643, 189603, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "Propeller repair Boulogne", "Boul", 2, 1, "6B", "TGroundDestroyed", 77, 265932, 189324, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "E-boat factory", "Boul", 2, 1, "7B", "TGroundDestroyed", 53, 264849, 189190, 50, false, 75, "");
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval main facility", "Havr", 2, 1, "LehavNaval1", "TGroundDestroyed", 84, 163216, 49915, 50, false, 100, "");    //added to targets list in mission and here in CS  fatal 9/22
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Officer mess", "Havr", 2, 1, "LehavNaval2", "TGroundDestroyed", 71, 163447, 49855, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval weapons training", "Havr", 2, 1, "LehavNaval3", "TGroundDestroyed", 75, 163313, 50063, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Underwater repair training", "Havr", 2, 1, "LehavNaval4", "TGroundDestroyed", 81, 163039, 49798, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Naval Intelligence", "Havr", 2, 1, "LehavNaval5", "TGroundDestroyed", 71, 163172, 49816, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Meteorolgy", "Havr", 2, 1, "LehavNaval6", "TGroundDestroyed", 89, 163470, 49752, 50, false, 100, "");
            addTrigger(MO_ObjectiveType.Building, "Le Havre Naval Naval cryptologic HQ", "Havr", 2, 1, "LehavNaval7", "TGroundDestroyed", 75, 162993, 49927, 50, false, 100, "");

            //*************************
            //Some leftover objectives after last edit 9/19/2018.  Can delete these after a while if not needed.
            //
            //addTrigger(MO_ObjectiveType.Fuel,      "Boulogne Diesel",                    2, 2, "RTarget12",.              "TGroundDestroyed", 50,  284978, 215920,    50,       false,       100,       ""); //Not sure what this one is?
            //addTrigger(MO_ObjectiveType.Fuel,      "Ethanol Storage Boulogne",           2, 2, "RTarget15",              "TGroundDestroyed", 50,  284153, 216913,    50,       false,      100,       ""); //Not sure what this one is?
            /* //These targets have the wrong x-y locations per the mission file, so saving this backup of them before changing above 9/19/2018
                        addTrigger(MO_ObjectiveType.Fuel,      "Boulogne Diesel",                    2, 2, "RTarget10",              "TGroundDestroyed", 60,  266150, 189291,    100,      false,       100,       "");
                        addTrigger(MO_ObjectiveType.Fuel,      "Boulogne Aviation Fuel",             2, 2, "RTarget11",              "TGroundDestroyed", 43,  264966, 189374,    100,      false,      100,       "");
                        addTrigger(MO_ObjectiveType.Fuel,      "Boulogne Diesel",                    2, 2, "RTarget12",              "TGroundDestroyed", 50,  284978, 215920,    50,       false,       100,       "");
                        addTrigger(MO_ObjectiveType.Fuel,      "Boulogne Benzine",                   2, 2, "RTarget13",              "TGroundDestroyed", 52,  284845, 216884,    50,       false,       100,       "");
                        addTrigger(MO_ObjectiveType.Fuel,      "Boulogne Liquid Oxygen",             2, 2, "RTarget14",              "TGroundDestroyed", 50,  285019, 217566,    50,       false,       100,      "");
                        addTrigger(MO_ObjectiveType.Fuel,      "Ethanol Storage Boulogne",           2, 2, "RTarget15",              "TGroundDestroyed", 50,  284153, 216913,    50,       false,      100,       "");
                       adding new le havre naval targets
  1B TGroundDestroyed 70 264252 189991 50 b      Targets to replace AAA targets in Boulogne
  2B TGroundDestroyed 47 265063 190506 50
  3B TGroundDestroyed 51 265251 190259 50
  4B TGroundDestroyed 62 264692 189709 50
  5B TGroundDestroyed 54 265643 189603 50
  6B TGroundDestroyed 77 265932 189324 50
  7B TGroundDestroyed 53 264849 189190 50
						*/
        }
            //Names of the flak areas and link to file name
            //Name is used in list of objectives aboe & must match exactly.  You can change the name below but then the name in the addTrigger etc above must also be changed to match
            //file name must match exactly with the filename
            public Dictionary<string, string> FlakMissions = new Dictionary<string, string>()
            {
                    { "Abbe", "/Flak areas/Abbevilleflak.mis" },
                    { "Arra", "/Flak areas/Arrasflak.mis" },
                    { "Ashf", "/Flak areas/Ashfordflak.mis" },
					{ "Bapu", "/Flak areas/Bapumeflak.mis" },// was missing
                    { "Bext", "/Flak areas/Bextonflak.mis" },
                    { "Boul", "/Flak areas/Boulogneflak.mis" },
                    { "Bult", "/Flak areas/Bultonflak.mis" },
                    { "Caen", "/Flak areas/Caenflak.mis" },
                    { "Cala", "/Flak areas/Calaisflak.mis" },
                    { "Diep", "/Flak areas/Dieppeflak.mis" },
                    { "Ditt", "/Flak areas/Dittonflak.mis" },
                    { "Dove", "/Flak areas/Doverflak.mis" },
                    { "Dunk", "/Flak areas/Dunkirkflak.mis" },
                    { "Estr", "/Flak areas/Estreeflak.mis" },
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
                    { "Quer", "/Flak areas/Quervilleflak.mis" },
                    { "RaHQ", "/Flak areas/RadarHQflak.mis" },
                    { "Read", "/Flak areas/Readingflak.mis" },
                    { "Redh", "/Flak areas/RedHillflak.mis" },
                    { "Shee", "/Flak areas/Sheernessflak.mis" },
                    { "Sout", "/Flak areas/Southhamptonflak.mis" },
                    { "Omar", "/Flak areas/St Omarflak.mis" },
                    { "Ouen", "/Flak areas/StOuenflak.mis" },
                    { "Swin", "/Flak areas/Swindonflak.mis" },
                    { "Tunb", "/Flak areas/Tunbridgeflak.mis" },
                    { "Watt", "/Flak areas/Wattenflak.mis" },
					{ "Quee", "/Flak areas/Queeflak.mis" },//Queensborough Flak
					// Radar flak added for radar instalations 
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
					//{ "DunR", "/Flak areas/DunRflak.mis" },//Dunkirk Freya Radar //dup so removing for now
					{ "PooR", "/Flak areas/PooRflak.mis" },//Poole English Radar
					{ "LitR", "/Flak areas/LitRflak.mis" },// Littlehampton Radar
					
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
        public void SelectSuggestedObjectives()
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
                        double r = msn.stb_random.NextDouble() * 100;
                        //Console.WriteLine("Select Primary " + mo.PrimaryTargetWeight + " " + r.ToString("N4") + " " + mo.ID);
                        if (mo.PrimaryTargetWeight < r) continue; //implement weight; if weight is less than the random number then this one is skipped; so 100% is never skipped, 50% skipped half the time, 0% skipped always

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

    public void MO_MissionObjectiveAirfieldsSetup(Mission msn, maddox.game.IGamePlay gp)
    {
        //public Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>> AirfieldTargets = new Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>>();
        //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius, Point3d airfield center (position)
        //TRIGGER initiator (for all types except RADAR)
        //public MissionObjective(Mission m, MO_ObjectiveType mot, double pts, double ptp, AiAirport airport, Tuple<bool, string, double, double, DateTime, double, Point3d> tup)
        //            string tn, string n, int ownerarmy, double pts, string t, double p, double x, double y, double d, bool pt, int ptp, string comment)


        //int NumNearbyTargets = MissionObjectivesNear();

        int count = AirfieldTargets.Count;
        double weight = (double)400 / (double)count; //500/count gives you about 1 airfield target about 1 of every 3 sets of targets
        if (AirfieldTargets != null) foreach (AiAirport ap in AirfieldTargets.Keys)
            {
                int NumNearbyTargets = MO_MissionObjectivesNear(AirfieldTargets[ap].Item7, 15000);
                double IndWeight = weight;
                if (NumNearbyTargets > 0) IndWeight = weight * 2;
                else if (NumNearbyTargets > 3) IndWeight = weight * 4;
                else if (NumNearbyTargets > 5) IndWeight = weight * 8;
                //Console.WriteLine("AP: " + AirfieldTargets[ap].Item2 + "_airfield");
                Point3d Pos = AirfieldTargets[ap].Item7;
                int army = GamePlay.gpFrontArmy(Pos.x, Pos.y);
                MissionObjectivesList.Add(AirfieldTargets[ap].Item2 + "_airfield", new MissionObjective(msn, 2, IndWeight, ap, army, AirfieldTargets[ap]));
                count++;
            }
        Console.WriteLine("Mission Objectives: Added " + count.ToString() + " airports to Mission Objectives, weight " + weight.ToString("N5"));

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
    public void MO_SelectPrimaryObjectives(int army = 0, double totalPoints = 0)
    {

        List<string> keys = new List<string>(MissionObjectivesList.Keys);
        Calcs.Shuffle(keys);

        List<int> arms = new List<int>();
        if (army == 0) { arms.Add(1); arms.Add(2); }
        else if (army == 1 || army == 2) arms.Add(army);

        Console.WriteLine("Selecting new Mission Objectives for " + ArmiesL[army]);

        foreach (var a in arms)
        {
            int counter = 1;

            for (int x = 0; x < 10; x++) //unlikely but possible that we'd need to cycle through the list of targets multiple times to select enough targets to reach the points. Could happen though if PrimaryTargetWeights are set low, or only a few possible objectives available in the list. 
            {
                foreach (var key in keys)
                {
                    MissionObjective mo = MissionObjectivesList[key];
                    if (mo.AttackingArmy == a && mo.PrimaryTargetWeight > 0 && mo.IsEnabled && !mo.IsPrimaryTarget)
                    {
                        double r = stb_random.NextDouble() * 100;
                        //Console.WriteLine("Select Primary " + mo.PrimaryTargetWeight + " " + r.ToString("N4") + " " + mo.ID);
                        if (mo.PrimaryTargetWeight < r) continue; //implement weight; if weight is less than the random number then this one is skipped; so 100% is never skipped, 50% skipped half the time, 0% skipped always
                        if (totalPoints < MO_PointsRequired[(ArmiesE)a])
                        {
                            mo.IsPrimaryTarget = true;
                            totalPoints += mo.Points;
                            MissionObjectivesString[(ArmiesE)a] += " - " + mo.Sector + " " + mo.Name;
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
            Console.WriteLine("Selecting new Mission Objectives for " + ArmiesL[army] + ":");
            Console.WriteLine(MissionObjectivesString[(ArmiesE)a]);
        }

    }

    //This reads the primary objectives selected from the previous mission
    //Just reads the previous objectives, but takes into consideration that objectives might have been removed, names changed
    //required point total increased or decreased, etc etc since the file was written
    public void MO_ReadPrimaryObjectives(int army = 0)
    {
        //MO_SelectPrimaryObjectives(army);
        if (army < 1 || army > 2) return;

        Console.WriteLine("Reading Mission Objectives from file for " + ArmiesL[army]);

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapObjectives.ini";

        //Ini.IniFile ini = new Ini.IniFile(filepath, this);
        Ini.IniFile ini = new Ini.IniFile(filepath);

        List<string> keys = ini.IniReadList(ArmiesL[army] + "_Objectives", "Objective");
        //Console.WriteLine("READ: " + l.ToString());

        double totalPoints = 0;
        foreach (var key in keys)
        {

            //The objective that previous existed may not be in the list on this run, so we have to be careful when reading it, not just assume it exists
            var mo = new MissionObjective(this);
            if (!MissionObjectivesList.TryGetValue(key, out mo)) continue;

            if (mo.AttackingArmy == army && mo.PrimaryTargetWeight > 0 && mo.IsEnabled && !mo.IsPrimaryTarget)
            {
                if (totalPoints < MO_PointsRequired[(ArmiesE)army])
                {
                    mo.IsPrimaryTarget = true;
                    totalPoints += mo.Points;
                    MissionObjectivesString[(ArmiesE)army] += " - " + mo.Sector + " " + mo.Name;
                }
                else
                {
                    mo.IsPrimaryTarget = false;
                }

            }

        }

        //In case the total points are not enough, we can go select more additional objectives
        if (totalPoints < MO_PointsRequired[(ArmiesE)army]) MO_SelectPrimaryObjectives(army, totalPoints);
        return;


    }

    public void MO_WritePrimaryObjectives()
    {

        DateTime dt = DateTime.UtcNow;
        string date = dt.ToString("u");


        //Console.WriteLine("MO_Write #2");

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapObjectives.ini";
        string filepath_old = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapObjectives_old.ini";
        string currentContent = String.Empty;

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MXX5"); //Testing for potential causes of warping
        //Save most recent copy of Campaign Map Score with suffix _old
        try
        {
            if (File.Exists(filepath_old)) { File.Delete(filepath_old); }
            File.Copy(filepath, filepath_old); //We could use File.Move here if we want to eliminate the previous .ini file before writing new data to it, thus creating an entirely new .ini.  But perhaps better to just delete specific sections as we do below.
        }
        catch (Exception ex) { Console.WriteLine("MO_Write Inner: " + ex.ToString()); }


        //Console.WriteLine("MO_Write Save #3");

        try
        {

            //Ini.IniFile ini = new Ini.IniFile(filepath, this);
            Ini.IniFile ini = new Ini.IniFile(filepath);

            //.ini keeps the same file & just adds or updates entries already there. Unless you delete them.
            //Delete all entries in these sections first
            ini.IniDeleteSection("Red_Objectives");
            ini.IniDeleteSection("Blue_Objectives");

            //Write the new data in the two sections
            ini.IniWriteList("Red_Objectives", "Objective", MO_ListAllPrimaryObjectives((int)ArmiesE.Red));
            ini.IniWriteList("Blue_Objectives", "Objective", MO_ListAllPrimaryObjectives((int)ArmiesE.Blue));

            //Save campaign objective list to special directory as a bit of a backup/record of objectives over time
        }
        catch (Exception ex) { Console.WriteLine("MapState Write: " + ex.ToString()); }

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



    }

    public void MO_ListSuggestedObjectives(Player player, int army, int numToDisplay = 5, double delay = 0.2)
    {

        int numDisplayed = 0;
        double totDelay = 0;

        twcLogServer(new Player[] { player }, "Suggested " + ArmiesL[army] + " Secondary Objectives:", new object[] { });

        foreach (var key in MissionObjectivesSuggested[(ArmiesE)army])
        {

            if (numDisplayed >= numToDisplay) break;
            MissionObjective mo = MissionObjectivesList[key];
            if (!mo.Destroyed && mo.IsEnabled)
            {
                totDelay += delay;
                Timeout(totDelay, () =>
                {
                        //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               

                        // + " (" + mo.Pos.x + "," + mo.Pos.y + ")"
                        twcLogServer(new Player[] { player }, mo.Sector + " " + mo.Name, new object[] { });

                });//timeout      


                numDisplayed++;


            }
        }
    }


    public string MO_ListRemainingPrimaryObjectives(Player player, int army, int numToDisplay = 10, double delay = 0.2, bool display = true, bool html = false)
    {

        string newline = Environment.NewLine;
        if (html) newline = "<br>" + Environment.NewLine;
        string retmsg = "";
        string msg = "";

        int numDisplayed = 0;
        double totDelay = 0;
        msg = "Remaining " + ArmiesL[army] + " Primary Objectives:";
        retmsg = msg + newline;

        if (display) twcLogServer(new Player[] { player }, msg, new object[] { });

        foreach (KeyValuePair<string, MissionObjective> entry in MissionObjectivesList)
        {

            if (numDisplayed >= numToDisplay) break;
            MissionObjective mo = entry.Value;
            if (!mo.Destroyed && mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
            {
                string msg1 = mo.Sector + " " + mo.Name;
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
            File.WriteAllText(filepath, op);
        }
        catch (Exception ex) { Console.WriteLine("MO_WriteOutAll: " + ex.ToString()); }
    }

    HashSet<string> MO_flakMissionsLoaded = new HashSet<string>();
    public bool MO_LoadAllPrimaryObjectiveFlak(Dictionary<string,string> flakMissions)
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
                    GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + flakMission);                    
                    DebugAndLog(CLOD_PATH + FILE_PATH + flakMission + " file loaded");
                    Console.WriteLine(CLOD_PATH + FILE_PATH + flakMission + " file loaded");
                    
                } else
                {
                    Console.WriteLine("LoadFlak: No flak file found for " + mo.Name + "  " + flakID + " " + flakMission);
                }                
            }
        }
        return true;
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
            if (mo.Destroyed && mo.AttackingArmy == army && mo.IsPrimaryTarget && mo.IsEnabled)
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



    //Destroys the objective with the given ID and takes other related actions, such as 
    //adding points, displaying messages, reducing radar coverage
    public bool MO_DestroyObjective(string ID, bool active = true)
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

        if (OldObj.Destroyed || !OldObj.IsEnabled) return false; //The object has already been destroyed; don't need to do it again; we only give points/credit for destroying any given objective once

        OldObj.Destroyed = true;
        if (OldObj.MOObjectiveType == MO_ObjectiveType.Radar)
        {
            if (OldObj.OwnerArmy == 1) DestroyedRadar[(ArmiesE.Red)].Add(OldObj);
            if (OldObj.OwnerArmy == 2) DestroyedRadar[(ArmiesE.Blue)].Add(OldObj);
        }

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


        if (OldObj.HUDMessage != null && OldObj.HUDMessage.Length > 0) GamePlay.gpHUDLogCenter(OldObj.HUDMessage);

        if (OldObj.LOGMessage != null && OldObj.LOGMessage.Length > 0) Timeout(10, () =>
        {
            twcLogServer(null, OldObj.LOGMessage, new object[] { });
            MissionObjectivesList[ID] = OldObj;
        });

        MO_CheckObjectivesComplete();

        return true;
    }

    public void MO_CheckObjectivesComplete()
    {


        //Turn the map by completing ALL primary objectives
        //OR most primary objectives (greater than specified percentage) plus reaching the higher point level required in that situation
        double bp = MO_PercentPrimaryObjectives((int)ArmiesE.Blue);

        if ((MissionObjectiveScore[ArmiesE.Blue] >= MO_PointsRequired[ArmiesE.Blue] && bp > 99)
            || bp >= MO_PercentPrimaryTargetsRequired[ArmiesE.Blue] && MissionObjectiveScore[ArmiesE.Blue] >= MO_PointsRequiredWithMissingPrimary[ArmiesE.Blue])// Blue battle Success

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
            || rp >= MO_PercentPrimaryTargetsRequired[ArmiesE.Red] && MissionObjectiveScore[ArmiesE.Red] >= MO_PointsRequiredWithMissingPrimary[ArmiesE.Red])// Blue battle Success
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
    public bool MO_isRadarEnabledByArea(Point3d pos, bool admin = false, int radarArmy = 0)
    {
        if (admin || radarArmy == 0 || radarArmy > 2) return true;

        //Console.WriteLine("#1 " + pos.x.ToString() + " " + pos.y.ToString() + " " + radarArmy.ToString());
        //WITHIN AN AREA WHERE THE RADAR HAS BEEN DESTROYED?
        //Finds if the point/ac is in an area with destroyed radar for either/both sides
        if (mission_objectives != null) { if (MO_IsPointInDestroyedRadarArea(pos, radarArmy)) return false; }
        else Console.WriteLine("#1.5  Mission Objectives doesn't exist!");
        //Console.WriteLine("#2 " + pos.x.ToString() + " " + radarArmy.ToString());

        //RED army special denied areas or areas the never have radar coverage
        if (radarArmy == 1)
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
     * 
     * ****************************************************************************************************************/
    //
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

        if ("F1".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action1");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Do 17s requesting escort! Meet at Calais at 6km in 10 minutes", null));

            
        //    Timeout(15, () => sendScreenMessageTo(1, " Testing...Do 17s have been spotted  east Calais @ 4000m heading west! Check for Escorts", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F1e".Equals(shortName) && active && !stopAI())// Trigger F1e launches escorts to go with Do 17s from trigger F1 above
        {
            AiAction action = GamePlay.gpGetAction("action1e");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F1c".Equals(shortName) && active && !stopAI())// Trigger F1c launches escorts to go with escorts from trigger F1e above
        {
            AiAction action = GamePlay.gpGetAction("action1c");


            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F2".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action2");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort! Meet at Dymchurch @ 20K ft. in 10 minutes.", null));
          //  Timeout(600, () => sendScreenMessageTo(2, "Wellingtons have been spotted over Dymchurch at 6000m heading east!!!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F3".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action3");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Ju88s requesting escort. Meet at Oye-Plage @ 6000m in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @ 20K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F4".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action4");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at 20K ft. over Lypne in 10 minutes.", null));
         //   Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympe at 6000m!!!.", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F5".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action5");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
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
                Console.WriteLine("Triggered action " + action.Name);
            }

        }
        else if ("F6".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action6");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort.  Meet at 20K ft. over St. Mary's Bay in 10 minutes.", null));
         //   Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
        else if ("F7".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action7");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "He-111s requesting escort.  Meet over Calais @ 6km in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "He-111s have been spotted over Calais @ 14K ft. heading west!", null));
        }
        else if ("F7e".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action7e");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

        }
        else if ("F8".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action8");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at St. Mary's Bay at 20K ft in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Blenheims have been spotted over St. Mary's Bay at 6km.", null));
        }
        else if ("F9".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action9");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Do 17s requesting escort! Meet at Calais at 6km in 10 minutes", null));
         //   Timeout(600, () => sendScreenMessageTo(1, "Second run Do 17s spotted over Calais @ 6000m heading west!!!", null));
        }
        else if ("F10".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action10");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort! Meet at Dymchurch @ 20K ft. in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "Wellingtons have been spotted over Dymchurch at 6000m heading east!!!", null));
        }
        else if ("F11".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action11");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "Ju88s requesting escort. Meet at Oye-Plage @ 6000m in 10 minutes.", null));
         //   Timeout(600, () => sendScreenMessageTo(1, "Ju88s have been spotted over Oye-Plage @20K ft. heading west!", null));
        }
        else if ("F12".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action12");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Blenheims requesting escort. Meet at 20K ft. over Lympne in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "A formation of eastbound Blenheims have been spotted over Lympne at 6000m!!!.", null));
        }
        else if ("F13".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(2, "BR.20Ms requesting escort.  Meet at Boulogne @ 6km in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(1, "BR.20Ms have been spotted over Boulogne @ 13K ft. heading west!", null));
            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F13e".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13e");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F13c".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13c");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
            }

            //GamePlay.gpGetTrigger(shortName).Enable = false;
        }
        else if ("F13cc".Equals(shortName) && active && !stopAI())
        {
            AiAction action = GamePlay.gpGetAction("action13cc");
            if (action != null)
            {
                action.Do();
                Console.WriteLine("Triggered action " + action.Name);
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
                Console.WriteLine("Triggered action " + action.Name);
            }
            Timeout(10, () => sendScreenMessageTo(1, "Wellingtons requesting escort.  Meet at 20K ft. over St. Mary's Bay in 10 minutes.", null));
        //    Timeout(600, () => sendScreenMessageTo(2, "An eastbound formation of Wellingtons have been spotted over St. Mary's Bay @ 6km!", null));
        }
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

    public static string correctedSectorNameDoubleKeypad(Mission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + doubleKeypad(p);
        return s;

    }

    public static string correctedSectorNameKeypad(Mission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + singleKeypad(p);
        return s;

    }

    //So, for whatever reason the gpSectorName puts out a section that is one off in both X & Y from
    //the sectors shown on the map.  So . . . subtracting 10000km from both x & y corrects this.  I guess?
    //Maybe a bug in 4.5?
    public static string correctedSectorName(Mission msn, Point3d p)
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
    public static int keypad(Point3d p, double size)
    {
        int lat_rem = (int)Math.Floor(3 * (p.y % size) / size);
        int lng_rem = (int)Math.Floor(3 * (p.x % size) / size);
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
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public void IniWriteList(string Section, string Key, List<string> Value)
        {
            int count = 0;
            WritePrivateProfileString(Section, "Count", Value.Count.ToString(), this.path);
            foreach (string s in Value)
            {
                WritePrivateProfileString(Section, Key + "[" + count.ToString() + "]", s, this.path);
                count++;
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
            int pos = _head - 1; // + 2*_baseArray.Length;  //by adding 2*_baseArray.Length we can count downwards by _baseArray.Length with no worries about going below 0 for our index.  We have to go 2* bec _head might be zero meaning our starting point might be -1
            for (int i = 0; i < _baseArray.Length; i++)
            {
                Math.DivRem(pos, _baseArray.Length, out pos);
                //Console.WriteLine("ArrayStack: " + i.ToString());
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
    public T Get(int indexBackFromHead)
    {
        int pos = _head - indexBackFromHead - 1;
        pos = pos < 0 ? pos + _baseArray.Length : pos;
        Math.DivRem(pos, _baseArray.Length, out pos);
        return _baseArray[pos];
    }

    //Gets top of the stack (ie, the last value entered) if 0 or 2nd to last, 3rd to last, etc if index 1, 2, 3 etc 
    public T GetStack(int indexForwardFromHead)
    {
        int pos = _head + indexForwardFromHead;
        pos = pos < 0 ? pos + _baseArray.Length : pos;
        Math.DivRem(pos, _baseArray.Length, out pos);
        return _baseArray[pos];
    }
}
