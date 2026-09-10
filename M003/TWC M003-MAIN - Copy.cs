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

/* TODO:
 * X Radar displays in English units even for German pilots.  Confusing.
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * **************************************************************************************/


//[Serializable]
public class Mission : AMission
{
    Random random, stb_random;
    //Constants constants; 

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
    public int RESPAWN_MINUTES;
    public int TICKS_PER_MINUTE;
    public double HOURS_PER_SESSION;
    public double NUMBER_OF_SESSIONS_IN_MISSION;
    public int END_SESSION_TICK;
    public int END_MISSION_TICK;
    public int RADAR_REALISM;
    public bool MISSION_STARTED = false;
    public int START_MISSION_TICK = -1;
    public bool COOP_START_MODE = true;
    public double COOP_MODE_TIME_SEC = 45;
    public int START_COOP_TICK = -1;
    public double COOP_TIME_LEFT_MIN = 9999;
    public double CampaignMapState = 0; //Determines which base map to load in & where the front it.  0 is the neutral map, - numbers tend more towards Blue, + numbers more towards Red
    public string CampaignMapSuffix = "-0"; //The initial initairports files will have suffix -0
    public int CampaignMapMaxRedSuffixMax = 23; //This implies you have initairports files named with suffix ie -R001, -R002, -R003, -R004
    public int CampaignMapMaxBlueSuffixMax = 17; //This implies you have initairports files named ie -B001, -B002, -B003, -B004
    public double CampaignMapRedPoints = 0; //Destruction of various targets & objectives will lead to points, 100 points will move the map/front one notch 
    public double CampaignMapBluePoints = 0;


    //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
    //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
    //used in method admins_privilege_level below
    public string[] admins_basic = new String [] { "TWC_", "69th_", "JG4_", "/JG52", "/JG26", "ATAG_" };
    public string[] admins_full = new String[] { "TWC_Flug", "TWC_Fatal_Error"};

    bool respawn_on;
    int respawnminutes;
    int ticksperminute;
    int tickoffset;
    int endsessiontick;
    int randHurryStrafeTick1;
    int randHurryStrafeTick2;
    int randBlueTick1;
    int randBlueTick2;
    int randBlueTick3;
    int randBlueTick4;
    int randBlueTickInitRaid;
    int randBlueTickRaid;
    int randBlueTickLateRaid;
    int randBlueTickFighterRaid1;
    int randBlueTickFighterRaid2;
    int randBlueTickFighterRaid3;

    int randRedTickBomberRaid1;
    int randRedTickBomberRaid2;
    int randRedTickBomberRaid3;
    int randRedTickFighterRaid1;
    int randRedTickFighterRaid2;
    int randRedTickFighterRaid3;
    int randRedTickFighterRaid4;
    int randRedTickFighterRaid5;
    int randRedTickFighterRaid6;
    int randRedTickFighterRaid7;
    int randRedTickFighterRaid8;
    int randRedTickFighterRaid9;
    int randRedTickFighterRaid10;
    int randRedTickFighterRaid11;

    Stopwatch stopwatch;
    Dictionary<string, Tuple<long, SortedDictionary<string, string>>> radar_messages_store;
    //Constructor
    public Mission() {
        random = new Random();
        stb_random = random;
        //constants = new Constants();
        respawn_on = true;
        MISSION_ID = "M003";
        SERVER_ID = "Mission Server";
        SERVER_ID_SHORT = "Mission";
        SERVER_ID_SHORT = "MissionTEST"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        CAMPAIGN_ID = "Franco Fandango"; //Used to name the filename that saves state for this campaign that determines which map the campaign will use, ie -R001, -B003 etc.  So any missions that are part of the same overall campaign should use the same CAMPAIGN_ID while any missions that happen to run on the same server but are part of a different campaign should have a different CAMPAIGN_ID
        DEBUG = false;
        LOG = true;
        radarpasswords = new Dictionary<int, string>
        {
            { -1, "twc"}, //Red army #1
            { -2, "twc"}, //Blue, army #2
            { -3, "twc2twc"}, //admin
            //note that passwords are CASEINSENSITIVE
        };

        USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
        CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover - MOD/";  // DO NOT CHANGE
        FILE_PATH = @"missions/Multi/Fatal/" + MISSION_ID + "/";   // mission install directory (CHANGE AS NEEDED)   
        stb_FullPath = CLOD_PATH + FILE_PATH;
        MESSAGE_FILE_NAME = MISSION_ID + @"_message_log.txt";
        MESSAGE_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + MESSAGE_FILE_NAME;
        STATS_FILE_NAME = MISSION_ID + @"_stats_log.txt";
        STATS_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + STATS_FILE_NAME;
        LOG_FILE_NAME = MISSION_ID + @"_log_log.txt";
        LOG_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + LOG_FILE_NAME;
        STATSCS_FULL_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover - MOD/missions/Multi/Fatal/";  // Must match location -stats.cs is saving SessStats.txt to
              



        /******************************************************************************
         * 
         * Timekeeping is a bit of a kludge . . . 
         * There are "sessions" - define the length below.
         * Then create variables like randBlueTickFighterRaid1 to determine at which tick 
         * in the session various random sub-missions will run
         * 
         * Then you can make the entire mission some multiple of that session - so you can
         * run the session 1x, 2x, 3x, or event 2.5x etc.
         * 
         * This we can have fairly long missions run, each one repeating a certain sequence
         * of sub-missions one or several times.
         * 
         ******************************************************************************/
        RESPAWN_MINUTES = 90; //For this mission this is used only as max life length for AI aircraft.  So set it a little longer than the entire mission timeline.    
        TICKS_PER_MINUTE = 1986; //empirically, based on a timeout test.  This is approximate & varies slightly.
        HOURS_PER_SESSION = 1.5; //# of hours the entire session is to last before re-start
        NUMBER_OF_SESSIONS_IN_MISSION = 2; //we can repeat this entire sequence 1x, 2x, 3x, etc. OR EVEN 1.5, 2.5, 3.25 etc times  
        END_SESSION_TICK = (int)(TICKS_PER_MINUTE * 60 * HOURS_PER_SESSION); //When to end/restart server session
        RADAR_REALISM = (int)5;
        
        respawnminutes = RESPAWN_MINUTES;
        ticksperminute = TICKS_PER_MINUTE;
        //endsessiontick = Convert.ToInt32(TICKS_PER_MINUTE*60*HOURS_PER_SESSION); //When to end/restart server session
        tickoffset = 20; //allows resetting the sub-mission restart times if a sub-mission is re-spawned manually etc
        endsessiontick = END_SESSION_TICK;
        END_MISSION_TICK = (int)((double)END_SESSION_TICK * NUMBER_OF_SESSIONS_IN_MISSION);

        //choose 2 times for hurry strafe mission to spawn, once in 1st half of mission & once in 2nd half        
        randHurryStrafeTick1 = random.Next((int)0 / 90, (int)(endsessiontick * 45 / 90));  //between minute 0 & 45        
        randHurryStrafeTick2 = random.Next((int)(endsessiontick * 46 / 90), endsessiontick * 90 / 90);  //between minute 46 & 90
        //Choose 4X for Blue raids to spawn 
        randBlueTick1 = random.Next((int)(endsessiontick * 5 / 90), (int)(endsessiontick * 18 / 90));  //between minute 5 & 18
        //randBlueTick1 = random.Next((int)(endsessiontick/180),(int)(endsessiontick/178));//FOR TESTING; load a submission about 1 minute after mission start
        randBlueTick2 = random.Next((int)(endsessiontick * 19 / 90), (int)(endsessiontick * 36 / 90));//another between minutes 19 & 36
        randBlueTick3 = random.Next((int)(endsessiontick * 50 / 90), (int)(endsessiontick * 68 / 90)); //another between minutes 50 & 68 
        randBlueTick4 = random.Next((int)(endsessiontick * 70 / 90), (int)(endsessiontick * 77 / 90)); //another between minutes 70 & 77

        randBlueTickInitRaid = random.Next((int)(endsessiontick * 3 / 90), (int)(endsessiontick * 6 / 90)); //spawn a major bomber raid in between minutes 1 & 2 of 90        ;
        randBlueTickRaid = random.Next((int)(endsessiontick * 33 / 90), (int)(endsessiontick * 37 / 90)); //spawn a major bomber raid in between minutes 43 & 47 of 90        
        randBlueTickLateRaid = random.Next((int)(endsessiontick * 42 / 90), (int)(endsessiontick * 56 / 90)); //spawn a major bomber raid in between minutes 42 & 56 of 90 (previously 54 & 64 of 90 but we stretched out the start of the missions by about 10 minutes so moved up their spawn in times by the same amount, 11/14/2017)
        randBlueTickFighterRaid1 = random.Next((int)(endsessiontick * 1 / 90), (int)(endsessiontick * 5 / 90)); //spawn a fighter raid in between minutes 1 & 5 of 90
        randBlueTickFighterRaid2 = random.Next((int)(endsessiontick * 28 / 90), (int)(endsessiontick * 32 / 90)); //spawn a fighter raid in between minutes 54 & 64 of 90
        randBlueTickFighterRaid3 = random.Next((int)(endsessiontick * 57 / 90), (int)(endsessiontick * 65 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
                                                                                                                 
        randRedTickBomberRaid1 = random.Next((int)(endsessiontick * 1 / 90), (int)(endsessiontick * 5 / 90)); //Red bomber raids targeting priority blue objectives
        randRedTickBomberRaid2 = random.Next((int)(endsessiontick * 20 / 90), (int)(endsessiontick * 32 / 90));
        randRedTickBomberRaid3 = random.Next((int)(endsessiontick * 48 / 90), (int)(endsessiontick * 58 / 90));


        randRedTickFighterRaid1 = random.Next((int)(endsessiontick * 2 / 90), (int)(endsessiontick * 6 / 90)); //spawn a fighter raid in between minutes 1 & 5 of 90
        randRedTickFighterRaid2 = random.Next((int)(endsessiontick * 13 / 90), (int)(endsessiontick * 17 / 90)); //spawn a fighter raid in between minutes 54 & 64 of 90
        randRedTickFighterRaid3 = random.Next((int)(endsessiontick * 23 / 90), (int)(endsessiontick * 28 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid4 = random.Next((int)(endsessiontick * 33 / 90), (int)(endsessiontick * 37 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid5 = random.Next((int)(endsessiontick * 43 / 90), (int)(endsessiontick * 48 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid6 = random.Next((int)(endsessiontick * 52 / 90), (int)(endsessiontick * 59 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid7 = random.Next((int)(endsessiontick * 62 / 90), (int)(endsessiontick * 69 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid8 = random.Next((int)(endsessiontick * 73 / 90), (int)(endsessiontick * 78 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid9 = random.Next((int)(endsessiontick * 79 / 90), (int)(endsessiontick * 82 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid10 = random.Next((int)(endsessiontick * 33 / 90), (int)(endsessiontick * 85 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid11 = random.Next((int)(endsessiontick * 17 / 90), (int)(endsessiontick * 85 / 90)); //spawn a fighter raid in between minutes 57 & 65 of 90

        stopwatch = Stopwatch.StartNew();
        radar_messages_store = new Dictionary<string, Tuple<long, SortedDictionary<string, string>>>();
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
                        GamePlay.gpLogServer(new Player[] { p }, "CO-OP START: You took off before Mission Start Time.", null);
                        GamePlay.gpLogServer(new Player[] { p }, "Your aircraft was destroyed.", null);
                    }

                    //If it is too far away from an airport, destroy (this takes care of tanks etc going rogue overland during the coop start period)
                    else if (Stb_distanceToNearestAirport(act) > 2500)
                    {
                        Stb_RemovePlayerFromCart(act as AiCart, p);
                        GamePlay.gpLogServer(new Player[] { p }, "CO-OP START: You left the airport or spawn point before Mission Start Time; " + Stb_distanceToNearestAirport(act).ToString("n0") + " meters to nearest airport or spawn point", null);
                        GamePlay.gpLogServer(new Player[] { p }, "You have been removed from your position.", null);
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
         * Also it coudl potentially ward off some cheating type behaviors, if people realize that AI tends to score more points for one side or the other when no one is playing, then
         * they could  just start a mission & leave it, just to rack up points for their side.
         * 
         * Recursive function called every X seconds
         ************************************************/
 

        Timeout(63.25, () => { EndMissionIfPlayersInactive(); });

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




    // loading sub-missions
    public override void OnTickGame()
    {
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



        if (!MISSION_STARTED)
        {
            //if (Time.tickCounter() % 10600 == 0) {
            if (Time.tickCounter() % (2 * ticksperminute) == 0)
            {
                //DebugAndLog ("Debug: tickcounter: " + Time.tickCounter().ToString() + " tickoffset" + tickoffset.ToString());

                int timewaitingminutes = Convert.ToInt32(((double)Time.tickCounter() / (double)ticksperminute));
                DebugAndLog("Waiting for first player to join; waiting " + timewaitingminutes.ToString() + " minutes");

            }

            return;
        }

        if (START_COOP_TICK == -1) START_COOP_TICK = Time.tickCounter();

        if (COOP_START_MODE)
        {

            int tickSinceCoopStarted = Time.tickCounter() - START_COOP_TICK;

            if (tickSinceCoopStarted >= Convert.ToInt32((COOP_MODE_TIME_SEC * (double)TICKS_PER_MINUTE)/60.0) ) {
                COOP_START_MODE = false;

                Stb_Chat("CO-OP MISSION START NOW!", null);
                Stb_Chat("CO-OP START: Pilots, you may take off at will", null);

                GamePlay.gpHUDLogCenter("CO-OP MISSION START NOW!");
                Timeout(5, () => { GamePlay.gpHUDLogCenter("CO-OP MISSION START NOW!"); });
                Timeout(10, () => { GamePlay.gpHUDLogCenter("CO-OP MISSION START NOW!"); });

                return;
            }


            if (tickSinceCoopStarted % (ticksperminute/4) == 0)
            {
                //DebugAndLog ("Debug: tickcounter: " + Time.tickCounter().ToString() + " tickoffset" + tickoffset.ToString());
                COOP_TIME_LEFT_MIN = (COOP_MODE_TIME_SEC/60 - ((double)tickSinceCoopStarted / (double)ticksperminute));
                double timeleftseconds = (COOP_MODE_TIME_SEC - ((double)tickSinceCoopStarted) * 60.0 / (double)ticksperminute);
                string s = COOP_TIME_LEFT_MIN.ToString("n2") + " MINUTES";
                if (timeleftseconds < 120) s = timeleftseconds.ToString("n0") + " SECONDS";

                //let players who can control <coop know about the command, 1X per minute
                if (tickSinceCoopStarted % ticksperminute == 0)
                {
                    Timeout (7.5, () => {
                        foreach (Player p in GamePlay.gpRemotePlayers())
                        {
                            if (admin_privilege_level(p) >= 1) //about once a minute, a message to players who can issue coop commands
                            {
                                GamePlay.gpLogServer(new Player[] { p }, "CO-OP MODE CONTROL: Use chat command <coop to start immediately OR extend time", null);
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

        int respawntick = respawnminutes * ticksperminute; // How often to re-spawn new sub-missions & do other repetitive tasks/messages. 27000=15 min repeat. 1800 'ticks' per minute or  108000 per hour.  I believe that this is approximate, not exact.

        if ((tickSinceStarted) == 0) {
            //GamePlay.gpLogServer(null, "Mission class initiated 2.", new object[] { });
            GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
            CheckMapTurned(); //Start the routine to check for objectives completed etc
    

            /*Timeout(60, () =>  //how many ticks in 60 seconds
                      {
                         //DebugAndLog ( "Debug/one minute: " + Time.tickCounter().ToString() + " " + tickoffset.ToString());
            }); */
        }

        if ((tickSinceStarted) == 200) {
            //GamePlay.gpLogServer(null, "Loading initial sub-missions.", new object[] { });  
            //ReadInitialSubmissions(MISSION_ID + "-initsubmission", 60);
        }


        /* SAMPLE CODE TO RESET THE MENUS PERIODICALLY IF THEY GET MESSED UP OVER TIME
        //THIS WOULD NEED TO BE REWRITTEN TO LOOP THROUGH ALL PLAYERS ONLINE
        if ( (tickSinceStarted) % 2000 == 0) {

           setSubMenu1(GamePlay.gpPlayer());
           setMainMenu(GamePlay.gpPlayer());


        }
        */

        //periodically remove a/c that have gone off the map
        if ((tickSinceStarted) % 2100 == 0) {

            RemoveOffMapAIAircraft();


        }

        /**** Turning this off for now bec. the radar/plotting table scheme
         * runs the radar every minute or so
        //roughly every two minutes
        if ((Time.tickCounter()) % 2000 == 0)
        {
            ///////////////////////////////////////////    
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            Console.WriteLine("Writing current radar returns to file");
            RADAR_REALISM = -1;
            listPositionAllAircraft(GamePlay.gpPlayer(), -1, false); //-1 & false will list ALL aircraft of either army
            //listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
            RADAR_REALISM = saveRealism;
        }
        */



        if ((tickSinceStarted) % 10100 == 0) {
            //DebugAndLog ("Debug: tickcounter: " + Time.tickCounter().ToString() + " tickoffset" + tickoffset.ToString());

            DebugAndLog("Total number of AI aircraft groups currently active:");
            if (GamePlay.gpAirGroups(1) != null) DebugAndLog(GamePlay.gpAirGroups(1).Length.ToString() + " Red airgroups");
            if (GamePlay.gpAirGroups(2) != null) DebugAndLog(GamePlay.gpAirGroups(2).Length.ToString() + " Blue airgroups");


            //display time left
            int timespenttick = (tickSinceStarted - tickoffset) % respawntick;
            int timelefttick = respawntick - timespenttick;
            int timespentminutes = Convert.ToInt32(((double)timespenttick / (double)ticksperminute));
            int timeleftminutes = Convert.ToInt32(((double)timelefttick / (double)ticksperminute));
            int missiontimeleftminutes = Convert.ToInt32((double)(END_MISSION_TICK - tickSinceStarted) / (double)ticksperminute);
            if (missiontimeleftminutes > 1) {
                string msg = missiontimeleftminutes.ToString() + " min. left in mission " + MISSION_ID;
                if (!MISSION_STARTED) msg = "Mission not yet started - waiting for first player to enter.";
                GamePlay.gpLogServer(null, msg, new object[] { });
                GamePlay.gpHUDLogCenter(msg);
            }
            //Write all a/c position to log
            if (LOG) {
                DebugAndLog(missiontimeleftminutes.ToString() + " min. left in mission " + MISSION_ID);
                int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
                RADAR_REALISM = 0;
                listPositionAllAircraft(GamePlay.gpPlayer(), 1, true);
                listPositionAllAircraft(GamePlay.gpPlayer(), 1, false);
                RADAR_REALISM = saveRealism;
            }

        }

        if (tickSinceStarted == 900) //load initial ship missions.  Note that these are loaded ONCE only @ start of mission (tickSinceStarted) NOT at the start of each session (tickSession)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITIALSHIPS"); // load sub-mission            

        }

        if ((tickSinceStarted) % (ticksperminute * 20) == 100)
        {//load TRAIN missions.

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionTRAINSBLUE"); // load sub-mission            
            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionTRAINSRED"); // load sub-mission

        }

        ////load RED random missions @ random times
        // After 4 minutes & before 70 minutes
        // Load one every 10 minutes with an offset of 7 minutes
        // We can come up with better/more devious ways to space out the timing of random mission starts later
        /* if ( (tickSinceStarted > TICKS_PER_MINUTE * 4) 
                    && (tickSinceStarted < TICKS_PER_MINUTE * 70) 
                    && (tickSinceStarted % (TICKS_PER_MINUTE * 10 ) == (int)((double)TICKS_PER_MINUTE * 7)  )
        */
        //FOR TESTING:
        //if ( tickSinceStarted > TICKS_PER_MINUTE * 1 && tickSinceStarted < TICKS_PER_MINUTE * 70 
        //         && tickSinceStarted % (TICKS_PER_MINUTE * 1 ) == (int)((double)TICKS_PER_MINUTE * .5)  )
        int currSessTick = tickSinceStarted % END_SESSION_TICK;
        if (currSessTick == randBlueTick1 || currSessTick == randBlueTick2 || currSessTick == randBlueTick3 || currSessTick == randBlueTick4)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionBLUE"); // sub-mission 

            //load red & blue bomber patrols here, too, as it is about the right timing but just wait a little bit so as to avoid the freezes
            Timeout(37, () =>
        {
            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionBOMBERPATROLBLUE"); // sub-mission     
        });
            Timeout(74, () =>
            {
                LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionBOMBERPATROLRED"); // sub-mission     
        });

        }

        if (currSessTick == 500) //load initial aircraft missions  &re-load them @ the start of every new session
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITIALBLUE"); // load sub-mission            

        }

        if (currSessTick == 700) //load initial aircraft missions  &re-load them @ the start of every new session
        {


            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITIALRED"); // load sub-mission            
        }

        //Load the initial Blue bomber raids approx minutes 1-2
        if (currSessTick == randBlueTickInitRaid)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITBLUE-bomber"); // load sub-mission            


        }

        //Load the major Blue fighter raids approx every 30 minutes
        if (currSessTick == randBlueTickFighterRaid1 || currSessTick == randBlueTickFighterRaid2 || currSessTick == randBlueTickFighterRaid3)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionRAIDBLUE-fighter"); // load sub-mission            


        }
        //Load the major Red fighter raids approx every 20-30 minutes - targeting major objectives on the Blue side
        if (currSessTick == randRedTickBomberRaid1 || currSessTick == randRedTickBomberRaid2 || currSessTick == randRedTickBomberRaid3)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionREDBomber"); // load sub-mission            


        }

        


        //Load the major RED fighter raids approx every 20 minutes
        if (currSessTick == randRedTickFighterRaid1 || currSessTick == randRedTickFighterRaid2 || currSessTick == randRedTickFighterRaid3 || currSessTick == randRedTickFighterRaid4 || currSessTick == randRedTickFighterRaid5 || currSessTick == randRedTickFighterRaid6 || currSessTick == randRedTickFighterRaid7 || currSessTick == randRedTickFighterRaid8 || currSessTick == randRedTickFighterRaid9 || currSessTick == randRedTickFighterRaid10 || currSessTick == randRedTickFighterRaid11)
        {
            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionREDaircover"); // load sub-mission            

        }

        //Load the major bomber & fighter raids between 2400 & 2500 seconds in
        if (currSessTick == randBlueTickRaid)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionRAIDBLUE-bomber"); // load bomber sub-mission a bit later                    	                    

        }

        if (currSessTick == randBlueTickLateRaid)
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionLATERAIDBLUE"); // load sub-mission                                

        }


        ///load RED randHurryStrafe missions @ random times
        if (currSessTick == randHurryStrafeTick1 || currSessTick == randHurryStrafeTick2)
        //FOR TESTING:
        //if ( currSessTick == 1000 || currSessTick == 3000 )     
        {

            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionREDhurrystrafe"); // sub-mission            
        }

        displayMessages(tickSinceStarted, tickoffset, respawntick);
        if (respawn_on && ((tickSinceStarted - tickoffset) % respawntick == 0))
        {
            //setMainMenu(GamePlay.gpPlayer());  //initialize menu
            //LoadRandomSubmission (); // sub-mission            
        }


        if (tickSinceStarted == END_MISSION_TICK) EndMission(60);// Out of time/end server.  ATAG Server monitor (CloD Watchdog) will auto-start the next mission when this one closes the launcher -server instance.





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

            if (display) GamePlay.gpLogServer(new Player[] { player }, msg , new object[] { });


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
            if (blenheim) scoreBase *= 4; //double score for Blenheims since their bomb load is pathetic (double the OLD score which is 4X the NEW score.  This makes 1 Blenheim (4 bombs X 4) about 50% as effective as on HE 11. (32 bombs)             

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
                    if (arm != 1 && arm !=2 )
                        {
                            arm = GamePlay.gpFrontArmy(APPos.x, APPos.y);  //This can be 1,2, or 0 for neutral territory.  
                        }                   
                    if (arm == 1) CampaignMapBluePoints += 5; //5 campaigns points for knocking out an airfield
                    else if (arm == 2) CampaignMapRedPoints += 5;

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

//TO DISPLAY VARIOUS MESSAGES AT VARIOUS TIMES IN THE MISSION CYCLE
public void displayMessages(int tick = 0, int tickoffset = 0, int respawntick = 20000) {

        int msgfrq = 1; //how often to display server messages.  Will be displayed 1/msgfrq times


        if ((random.Next(msgfrq + 1) == 1) && (tick) % 40000 == 38400) // 27000=15 min repeat. 10000=another few minutes' delay

        {
            GamePlay.gpLogServer(null, "The Wrecking Crew (TWC) is always looking for new pilots. Check us out at TWCCLAN.COM.", new object[] { });
        }
        if ((random.Next(msgfrq + 1) == 1) && (tick) % 40000 == 1900) // 27000=15 min repeat. 10000=another few minutes' delay

        {
            GamePlay.gpLogServer(null, ">> SERVER TIP: Read the Mission Briefing for important instructions <<", new object[] { });
        }
        if ((random.Next(msgfrq + 1) == 1) && (tick) % 40000 == 20200)
        {
            GamePlay.gpLogServer(null, ">> SERVER TIP: Use TAB-4-1 for special server commands <<", new object[] { });
        }
        if ((random.Next(msgfrq + 1) == 1) && (tick) % 40000 == 15300)
        {
            GamePlay.gpLogServer(null, ">> SERVER TIP: Type <help in chat for several useful commands <<", new object[] { });
        }
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
    public void EndMission(int endseconds = 0, string winner = "") {
        if (winner == "")
        {
            GamePlay.gpLogServer(null, "Mission is restarting soon!!!", new object[] { });
            GamePlay.gpHUDLogCenter("Mission is restarting soon!!!");
        } else {
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
                  SaveMapState(winner); //here is where we save progress/winners towards moving the map & front one way or the other
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

    public bool LoadRandomSubmission(string fileID = "randsubmission")
    {
        //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
        //GamePlay.gpHUDLogCenter("Respawning AI air groups");
        //GamePlay.gpLogServer(null, "RESPAWNING AI AIR GROUPS. AI Aircraft groups re-spawn every " + RESPAWN_MINUTES + " minutes and have a lifetime of " + RESPAWN_MINUTES + "-" + 2*RESPAWN_MINUTES + " minutes. The map restarts every " + Convert.ToInt32((float)END_SESSION_TICK/60/TICKS_PER_MINUTE) + " hours.", new object[] { });

        bool ret = false;

        List<string> RandomMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH, fileID); // Gets all files with with text MISSION_ID-fileID (like "M001-randsubmissionREDBOMBER") in the filename and ending with .mis
        //if (DEBUG) 
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

    private void sendScreenMessageTo(int army, string msg, object[] parms)
    {
        List<Player> Players = new List<Player>();

        // on Dedi the server or for singleplayertesting
        if (GamePlay.gpPlayer() != null)
        {
            if (GamePlay.gpPlayer().Army() == army || army == -1)
                Players.Add(GamePlay.gpPlayer());
        }
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

    //WRITE OUT PLAYER STATS////////////////////////////////////////////    
    //http://theairtacticalassaultgroup.com/forum/archive/index.php/t-5766.html
    //Nephilim
    //Sep-24-2013, 06:16
    //Here is method that allow You to mirror kills straight from IPlayer:
    //
    private string GetDictionary<T>(Dictionary<string, T> ds)
    {
        StringBuilder sb = new StringBuilder();
        foreach (string key in ds.Keys)
        {
            T d = ds[key];
            if (sb.Length != 0)
            {
                sb.Append(", ");
            }
            //sb.AppnedFormat("[{0}]={1}", key, d);
            sb.AppendFormat("[{0}]={1}", key, d);
        }
        return sb.ToString();
    }

    public string WritePlayerStat(Player player)
    {
        double M_Health = 0;
        double Z_Overload = 0;
        double M_CabinState = 0;
        double M_SystemWear = 0;
        double I_EngineTemperature = 0;
        double I_EngineCarbTemp = 0;
        double cdam = 0;
        double ndam = 0;

        if (player is IPlayer)
        {
            IPlayerStat st = (player as IPlayer).GetBattleStat() as IPlayerStat;


            AiAircraft aircraft = (player.Place() as AiAircraft);
            if (aircraft != null) {

                M_Health = aircraft.getParameter(part.ParameterTypes.M_Health, 0);
                Z_Overload = aircraft.getParameter(part.ParameterTypes.Z_Overload, -1);
                M_CabinState = aircraft.getParameter(part.ParameterTypes.M_CabinState, -1);
                M_SystemWear = aircraft.getParameter(part.ParameterTypes.M_SystemWear, -1);

                //The two below don't work for some unknown reason!
                //I think it is that all the values lower on the table shown here don't work, note sure why not:
                //http://theairtacticalassaultgroup.com/forum/showthread.php?t=3887
                //
                /*I_EngineTemperature = aircraft.getParameter(part.ParameterTypes.I_EngineTemperature, -1);
                I_EngineCarbTemp = aircraft.getParameter(part.ParameterTypes.I_EngineCarbTemp, -1);*/
                /*I_EngineCarbTemp = aircraft.getParameter(part.ParameterTypes.I_AmbientTemp, -1); */


                cdam = aircraft.getParameter(part.ParameterTypes.M_CabinDamage, 1);
                ndam = aircraft.getParameter(part.ParameterTypes.M_NamedDamage, 1);

            }

            /* Sample code:
                      if (player is IPlayer)
          {
              IPlayerStat st = (player as IPlayer).GetBattleStat();
              string stats = (String.Format("PlayerStat[{0}] bulletsFire={1}, landings={2}, kills={3}, fkills={4}, deaths={5}, bails={6}, ditches={7}, planeChanges={8}, planesWrittenOff={9}, netBattles={10}, singleBattles={11}, tccountry={12}, killsTypes=\"{13}\"",
                              player.Name(), st.bulletsFire, st.landings, st.kills, st.fkills, st.deaths, st.bails, st.ditches, st.planeChanges, st.planesWrittenOff, st.netBattles, st.singleBattles, st.tccountry, GetDictionary(st.killsTypes)));
              ColoredConsoleWrite(ConsoleColor.DarkCyan, "Stats: " + stats);

          }
            */

            string stats = (String.Format("CloD internal stats for {0}: bulletsFired={1}, landings={2}, kills={3}, fkills={4}, deaths={5}, bails={6}, ditches={7}, planeChanges={8}, planesWrittenOff={9}, MHealth={10}, CabinState={11}, SystemWear={12}, Cabin Damage={13}, NamedDame={14}, killsTypes=\"{15}\", tTotalTypes=\"{16}\"",
            player.Name(), st.bulletsFire, st.landings, st.kills, st.fkills, st.deaths, st.bails, st.ditches, st.planeChanges, st.planesWrittenOff, M_Health, M_CabinState, M_SystemWear, cdam, ndam, GetDictionary(st.killsTypes), GetDictionary(st.tTotalTypes)));




            return stats;
        }
        else
        {
            return string.Empty;
        }

        // return "Not working";      
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

    private void destroyPlane(AiAircraft aircraft) {
        if (aircraft != null) {
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

    private void destroyAiControlledPlane(AiAircraft aircraft) {
        if (isAiControlledPlane2(aircraft)) {
            destroyPlane(aircraft);
        }
    }

    private void damageAiControlledPlane(AiActor actor) {
        if (actor == null || !(actor is AiAircraft)) {
            return;
        }

        AiAircraft aircraft = (actor as AiAircraft);

        if (!isAiControlledPlane2(aircraft)) {
            return;
        }

        if (aircraft == null) {
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

    //////////////////////////////////////////

    public override void OnMissionLoaded(int missionNumber)
    {
        #region stb
        base.OnMissionLoaded(missionNumber);
        //GamePlay.gpLogServer(null, "Main mission started 1", new object[] { });        
        #endregion
    }


    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceLeave(player, actor, placeIndex);
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
                            }
                        }
                    );
                }
            }
            DateTime utcDate = DateTime.UtcNow;
            logStats(utcDate.ToString("u") + " " + player.Name() + " " + WritePlayerStat(player));
        }
        );

    }

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);
        if (player != null)
        {
            setMainMenu(player);
            MISSION_STARTED = true;
        }
        //Still getting object reference not set to an instance of the object error
        //I think because the aircraft.getParameter method is set to private
        /*
        AiAircraft aircraft = actor as AiAircraft;
        string cs = aircraft.CallSign();
        int p = (int)part.ParameterTypes.I_VelocityIAS;
        GamePlay.gpLogServer(new Player[] { player }, "Parm: " + p + " CS: " + cs, new object[] { });
        if (!(aircraft == null)) GamePlay.gpLogServer(new Player[] { player }, "Aircraft is not null", new object[] { });
        if (!(part.ParameterTypes.I_VelocityIAS == null)) GamePlay.gpLogServer(new Player[] { player }, "parametertypes is not null", new object[] { });
        if ((aircraft.GetType().GetMethod("getParameter")) != null) GamePlay.gpLogServer(new Player[] { player }, "a/c.GetParameter is not null", new object[] { });

        // part.ParameterTypes.I_VelocityIAS 
        //double ias = aircraft.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
        //GamePlay.gpLogServer(new Player[] { player }, "Plane: "  
        //    + cs + " " + ias.ToString(), new object[] { });
        */
    }


    #region onbuildingkilled

    //OnBuildingKilled only works on offline servers
    //A few (random?) buildings also report to this routine in the multiplayer/online servers
    /*
    public override void OnBuildingKilled(string title, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnBuildingKilled(title, pos, initiator, eventArgInt);

        GamePlay.gpLogServer(null, "BUILDING:", new object[] { });
        GamePlay.gpLogServer(null, "BUILDING:" + title + " at " + pos.x.ToString() + ".", new object[] { });

        string BuildingName = title;
        string BuildingArmy = "";
        string PlayerArmy = "";
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

        GamePlay.gpLogServer(null, "BUILDING:" + BuildingName + " in " + BuildingArmy + " was destroyed in sector " + sectorName + " by " + initiator.Player.Name() + " from the " + PlayerArmy + ".", new object[] { });
    }
    */

    #endregion

    #region onstationarykilled
    /*************************************************
     * 
     * Dam bombing objectives explanation
     * 
     *************************************************/
    //OnStationaryKilled includes code to deal with dams that are bombed.  Creating the dams is a multi-step process:
    //  #1. Generally the main portion of the dam is built with 'buildings' - which includes walls, turrets, and other dam-like objects.  But 'buildings' must be loaded in the main .mis file (not a sub-mission) and do NOT report to onStationaryKilled when they are bombed or killed.
    //
    //  #2. So to work around that, we build a portion of the dam using stationary ("static") objects.  Some that work well:
    //       Static10 Stationary.Industrial.Huge_Factory.Part_C de 269351.97 165975.80 720.00
    //       Static11 Stationary.Industrial.Huge_Factory.Part_B de 269351.97 165975.80 720.00
    //       Static12 Stationary.Industrial.Huge_Factory.Part_A de 269351.97 165975.80 720.00
    // 
    //  #2. Then select which of those objects (probably all/any of them) to trigger the dam failure, and use that ID in the "if" statement below, e.g.:
    //
    //            if (!osk_LeHavreDam_destroyed && (
    //              stationary.Name == "0:Static41" ||
    //              stationary.Name == "0:Static42" ||
    //              stationary.Name == "0:Static43"               
    //              ))
    //
    //  #3. Then enter ALL of the objects in another "if" statement below, which will remove the objects after 60 seconds.  This will make the 'hole' in the dam.  E.G.:
    //         if (sta.Name == "0:Static10" || sta.Name == "0:Static11" || sta.Name == "0:Static12") //Need to list the name of EACH item that should be removed/destroyed. We'll have trouble here if editing the file renumbers the statics . . .
    //
    //  #4. Then to make a deal of smoke and explosions to let bombers know they have successfully hit the dam, it makes sense to load the area under the stationary/static with
    //     stationary objects that explode nicely.  Adding these also adds to the ground target victory total for the player who destroys the dam. It also makes sense at times
    //      to hide these objects by putting them a few meters underground -2 meters in this case.  For proper stats reporting, MAKE SURE THE STATIONARY OBJECTS ARE ASSIGNED
    //     TO THE CORRECT ARMY **AND** TO PROPER COUNTRY DE OR GB:
    //      Static86 Stationary.Environment.JerryCan_GER1_1 gb 172036.72 46587.18 720.00 /hstart -2  (Static Environment Jerry Can)
    //      Static17 Stationary.Environment.TelegaBallon_UK1 de 269402.91 166013.78 720.00   (Static Environment Hydrogen Tank Cart)
    //
    //     DO NOT use this stationary--or any of the similar fuel drum stationaries (X3, X2, X1):
    // 
    //      Static21 Stationary.Environment.FuelDrum_UK1_9 de 269440.16 165939.81 720.00 /hstart -2   (Static Environment British Fuel Drum X9)
    //
    //     The problem with the fuel cans is that they explode and they cause other nearby objects to explode.  But when the fuel can causes the explosion, the stationary is killed
    //     and soon disappears, but it is never sent through OnStationaryKilled.  So if the stationary is destroyed this way we never see it here & cannot register the dam
    //     as being destroyed.
    //
    //     Hydrogen & jerry can seem to create some nice smoke & fire while avoiding this problem.  
    //
    //  #5. Another technique for making smoke/explosions is to put an oil bunker inside the static/stationary structure.  This must be in the main .mis file (not a submission) 
    //     and will not count as a ground kill for the player.  But it makes and excellent explosion etc. (Big, med, small all work--though you may run into the same problems 
    //     as with fuel drums--more testing required):
    //
    //       154_bld buildings.House$Oil_Bunker-Big 1 269059.75 165974.34 720.00  (Buildings - Generic - Fuel Storage - large, medium, or small)
    //
    //  #6. You can also load a sub-mission at this point that would include smoke etc all along the dam to indicate that it has failed and is destroyed.   See code below for examples.  It will be something like:
    //        GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
    //       Note that you cannot load "BUILDINGS" in sub-missions; they only load in the main .mis file.  But the sub-mission could include various stationaries/static objects, some of which are 'buildlings' (confusing, but that is how CLOD works for now)
    //
    //  #7. For each dam you want to handle, you will need a separate "if" section below, within OnStationaryKilled, customized to be triggered by the right staticXX and removing the right staticX1, static X2, etc, and 
    // a corresponding variable, like osk_LeHavreDam_destroyed, to track whether or not that dam has been destroyed.
    //
    //  #8. Buildings MUST be in the main .mis file, or they are not loaded. The statics that we use to detect whether objectives
    //  have been destroyed, or that we want to destroy via this .cs file, should be in the main .mis file, so that we can easily
    //  find them via looking for 0:StaticXX.  But it makes sense to move all OTHER statics out of the main .mis file into
    // a separate initsubmission file.  For example, see TWC M002-initsubmission-LeHavDam.mis & similar.  These stationaries 
    // will then be loaded at the mission start but don't clutter up the main .mis file, which is already very complex.

    bool osk_MapTurned = false;
    bool osk_LeHavreDam_destroyed = false;
    bool osk_LesAndelysDam_destroyed = false;
    bool osk_OuistrehamDam_destroyed = false;
    bool osk_SouthamptonDam_destroyed = false;
    bool osk_HambleDam_destroyed = false;
    bool osk_CowesDam_destroyed = false;
    bool osk_DieppeDam_destroyed = false;
    bool osk_LeTreportDam_destroyed = false;
    bool osk_BerckDam_destroyed = false;
    bool osk_EtaplesDam_destroyed = false;
    bool osk_BoulogneUpperDam_destroyed = false;
    bool osk_BoulogneLowerDam_destroyed = false;
    bool osk_FreventFactory_destroyed = false;
    bool osk_ArrasFuelStorage_destroyed = false;
    bool osk_NeufchatelFactory_destroyed = false;
    bool osk_ForgesFuelStorage_destroyed = false;
    bool osk_Blue50Kills = false;
    bool osk_Red50Kills = false;
    bool osk_Blue10AirKills = false;
    bool osk_Red10AirKills = false;
    bool osk_Blue10GroundKills = false;
    bool osk_Red10GroundKills = false;
    string osk_RedObjCompleted = "- ";
    string osk_BlueObjCompleted = "- ";
    string osk_RedObjDescription = "Red Objectives: Boulogne Lower Dam (BA20.2) - Frevent Secret Factory (BF14.9) - Arras Fuel Dump (BI14.1) - 50 total Team Kills - 10 Air Kills - 10 AA/Naval/Ground Kills - 10 more Team Kills than Blue";
    string osk_BlueObjDescription = "Blue Objectives: Dieppe Dam (AH-20.3) - Neufchatel-en-Bray Secret Factory (AZ09.4) - Forges-les-Eaux Fuel Dumps (BA07.4) - Hamble Dam (AG21.1) - 50 total Team Kills - 10 Air Kills - 10 AA/Naval/Ground Kills -10 more Team Kills than Red";
    int osk_RedGroundTargets = 0;
    int osk_BlueGroundTargets = 0;

    /****
*MOO3:
* 229,101: Dieppe, static261, 262, 263
*   LeTreport, 258, 259, 260
*Berck, 556,558,559
*Etaples, 403,405, 406
*Boulogne Upper 444 509
*Boulogne Lower 631, 632, 637
***********/

    public override void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnStationaryKilled(missionNumber, stationary, initiator, eventArgInt);
        
        HashSet<string> targets;

        if (initiator!=null && initiator.Player != null && initiator.Player.Name() != null) GamePlay.gpLogServer(new Player[] { initiator.Player }, "You destroyed a ground target (" + stationary.Name +")", new object[] { });
        // + " in " + stationary.country + " was destroyed by " + initiator.Player.Name() + " from the " + initiator.Player.Army() + ".", new object[] { });

        /******************************************************************
         *
         * Handle Le Havre Dam bombing
         *
         ******************************************************************/
 
        targets = new HashSet<string>(new string[] { "0:Static41", "0:Static42", "0:Static43" });       

        if (!osk_LeHavreDam_destroyed && targets.Contains(stationary.Name))  //any of these stationaries being killed will kill the dam, but we want to be sure we go through this routine once only, not 2X or 3X
        {

            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "Le Havre Dam eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("The Le Havre Dam has been eliminated. Well done" + name);

            osk_LeHavreDam_destroyed = true;
            CampaignMapBluePoints += 10;

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID +"-LeHavDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears)
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name)) 
                    {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60 + random.Next(-15,30) , () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }

        }

        /******************************************************************
         *
         * Handle LesAndelys Dam bombing         
         *
         ******************************************************************/
        // 0:Staticxx is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static63", "0:Static65", "0:Static66" });

  
        if (!osk_LesAndelysDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_LesAndelysDam_destroyed = true;
            CampaignMapBluePoints += 10;

            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "Les Andelys Dam eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("The Les Andelys Dam has been eliminated. Well done" + name);
 
            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID +"-LesAndelysDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }


        }

        /******************************************************************
         *
         * Handle Ouistreham Dam bombing         
         *
         ******************************************************************/
        // 0:Staticxx is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static680", "0:Static681", "0:Static686" });

        if (!osk_OuistrehamDam_destroyed && targets.Contains(stationary.Name))

        {
            osk_OuistrehamDam_destroyed = true;
            CampaignMapBluePoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "Ouistreham Dam eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("The Ouistreham Dam has been eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID +"-OuistrehamDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }


        }


        /******************************************************************
         *
         *Handle Southampton Dam bombing
         * - Uses a sub-mission to create smoke effects around the bombed dam, etc
         *
         ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static307", "0:Static309", "0:Static310" });

        if (!osk_SouthamptonDam_destroyed && targets.Contains(stationary.Name))

        {

            osk_SouthamptonDam_destroyed = true;
            CampaignMapBluePoints += 10;

            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "Southampton Dam eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("The Southamtpon Dam has been eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-SouthamptonDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }

                //sta.Destroy();
            }
        }


        /******************************************************************
         *
         *Handle Cowes Dam bombing
         * - Uses a sub-mission to create smoke effects around the bombed dam, etc
         *
         ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static427", "0:Static429", "0:Static430" });

        if (!osk_CowesDam_destroyed && targets.Contains(stationary.Name))

        {
            osk_CowesDam_destroyed = true;
            CampaignMapBluePoints += 10;

            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "Cowes Dam eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("The Cowes Dam has been eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-CowesDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                            //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                        });
                }

                //sta.Destroy();
            }


        }

        /******************************************************************
        *
        * Handle Hamble Dam bombing  
        *
        ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static68", "0:Static133" });

        if (!osk_HambleDam_destroyed && targets.Contains(stationary.Name))        
        {
            osk_HambleDam_destroyed = true;
            CampaignMapBluePoints += 10;

            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Hamble Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Hamble Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-HambleDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }                
            }


        }

        /******************************************************************
        *
        * Handle BoulogneLower Dam bombing  
        *
        ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static631", "0:Static632", "0:Static637" });

        if (!osk_BoulogneLowerDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_BoulogneLowerDam_destroyed = true;
            osk_RedObjCompleted += "Boulogne Lower Dam - ";
            CampaignMapRedPoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Boulogne Lower Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Boulogne Lower Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-BoulogneLowerDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }
        }

        /******************************************************************
        *
        * Handle BoulogneUpper Dam bombing  
        *
        ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static444", "0:Static509" });

        if (!osk_BoulogneUpperDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_BoulogneUpperDam_destroyed = true;
            CampaignMapRedPoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Boulogne Upper Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Boulogne Upper Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-BoulogneUpperDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }
        }


        /******************************************************************
        *
        * Handle Etaples Dam bombing  
        *
        ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static403", "0:Static405", "0:Static406" });

        if (!osk_EtaplesDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_EtaplesDam_destroyed = true;
            CampaignMapRedPoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Etaples Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Etaples Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-EtaplesDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }
        }


        /******************************************************************
        *
        * Handle Berck Dam bombing  
        *
        ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static556", "0:Static558", "0:Static559" });

        if (!osk_BerckDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_BerckDam_destroyed = true;
            CampaignMapRedPoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Berck Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Berck Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-BerckDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }


        }

        /******************************************************************
       *
       * Handle LeTreport Dam bombing  
       *
       ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static258", "0:Static259", "0:Static260" });

        if (!osk_LeTreportDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_LeTreportDam_destroyed = true;
            CampaignMapBluePoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The LeTreport Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("LeTreport Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-LeTreportDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }


        }

        /******************************************************************
        *
        * Handle Dieppe Dam bombing  
        *
        ******************************************************************/
        // 0:Static10 is the large industrial building the forms the center part of the dam.  When the player hits this part of the dam & blows it up, that will be the trigger to determine that the dam has actually been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static261", "0:Static262", "0:Static263" });

        if (!osk_DieppeDam_destroyed && targets.Contains(stationary.Name))
        {
            osk_DieppeDam_destroyed = true;
            osk_BlueObjCompleted += "Dieppe Dam - ";
            CampaignMapBluePoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Dieppe Dam has been eliminated. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Dieppe Dam eliminated. Well done" + name);

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.

            //GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "LeHavDam-inactive.mis");
            GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC " + MISSION_ID + "-DieppeDam-inactive.mis");

            //Now make the central buildings in the dam disappear (to make the dam appear to have a break in it, once the smoke clears) 
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (targets.Contains(sta.Name))
                {
                    //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                    Timeout(60, () =>
                    {
                        sta.Destroy();
                        DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                        //GamePlay.gpLogServer(null, "The bombed dam is disintegrating . . . ", new object[] { });
                    });
                }
            }


        }

        /******************************************************************
        *
        * Handle Fuel Dump & Other Misc. Objectives
        *
        ******************************************************************/
        // Fuel Dump Objectives are slightly more simply than dams.  Simply put a jerry can inside each (building) fuel storage tank
        // Select just ONE jerry can (say, one in the middle of your cluster of fuel storage tanks) to use to detect if 
        // the objective has been destroyed.  Unlike the situation with the dams, the jerry cans seem to be easily destroyed
        // when the fuel storage tanks explode, and the player always seems to get credit.  So we need to check only one jerry can
        // to see if the objective has been destroyed.

        targets = new HashSet<string>(new string[] { "0:Static162", "0:Static230", "0:Static37", "0:Static109" });

        if (!osk_FreventFactory_destroyed && targets.Contains(stationary.Name))
        {
            osk_FreventFactory_destroyed = true;
            osk_RedObjCompleted += "Frevent Secret Factory - ";
            CampaignMapRedPoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Frevent Secret Factory has been destroyed. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Frevent Secret Factory destroyed. Well done" + name);
        }

        targets = new HashSet<string>(new string[] { "0:Static390", "0:Static1315", "0:Static1314", "0:Static1317", "0:Static1322", "0:Static1310" });

        if (!osk_ArrasFuelStorage_destroyed && targets.Contains(stationary.Name))
        {
            osk_ArrasFuelStorage_destroyed = true;
            osk_RedObjCompleted += "Arras Fuel Storage - ";
            CampaignMapRedPoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Arras Fuel Storage has been destroyed. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Arras Fuel Storage destroyed. Well done" + name);
        }
        targets = new HashSet<string>(new string[] { "0:Static1555", "0:Static1559" });

        if (!osk_NeufchatelFactory_destroyed && targets.Contains(stationary.Name))
        {
            osk_NeufchatelFactory_destroyed = true;
            osk_BlueObjCompleted += "Neufchatel Secret Factory - ";
            CampaignMapBluePoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Neufchatel Secret Factory has been destroyed. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Neufchatel Secret Factory. Well done" + name);
        }
        targets = new HashSet<string>(new string[] { "0:Static1841", "0:Static1847", "0:Static1853", "0:Static1858", "0:Static1863", "0:Static1873", "0:Static1884", "0:Static1866" });

        if (!osk_ForgesFuelStorage_destroyed && targets.Contains(stationary.Name))
        {
            osk_ForgesFuelStorage_destroyed = true;
            osk_BlueObjCompleted += "Forges Fuel Storage - ";
            CampaignMapBluePoints += 10;
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "The Forges Fuel Storage has been destroyed. Well done" + name, new object[] { });
            GamePlay.gpHUDLogCenter("Forges Fuel Storage destroyed. Well done" + name);
        }

    }

    #endregion

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
    public void CheckMapTurned()
    {
        /************************************************
         * 
         * Check to see if anyone has turned the map
         * Recursive function called every X seconds
         ************************************************/
        Timeout(28, () => { CheckMapTurned(); }); 
        
        

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
                    RedTotalF = Convert.ToDouble(RedTotalS)/100;
                    BlueTotalF = Convert.ToDouble(BlueTotalS)/100;
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

        //Check whether the 50-kill objective is reached.  
        if (!osk_Red50Kills && RedTotalF >= 50) {
            osk_RedObjCompleted += "50 total Team Kills - ";
            osk_Red50Kills = true;
            GamePlay.gpLogServer(null, "RED reached 50 Team Kills. Well done Team Red!", new object[] { });
            GamePlay.gpHUDLogCenter("RED reached 50 Team Kills. Well done Red!");

        }
        if (!osk_Blue50Kills && BlueTotalF >= 50) {
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
        if (!osk_MapTurned && osk_BoulogneLowerDam_destroyed && osk_FreventFactory_destroyed && osk_ArrasFuelStorage_destroyed && osk_Red10AirKills && osk_Red10GroundKills && RedTotalF >= 50 && RedTotalF > BlueTotalF + 10)//We use RedTotalF >= 50 here, rather than osk_Red50Kills == true, because the team may get 50 kills but then LOSE SOME due to penalty points.)
        {
            osk_RedObjCompleted += "10 more Team Kills than Blue - ";
            osk_MapTurned = true;
            EndMission(300, "RED");

        }


        //BLUE has turned the map
        if (!osk_MapTurned && osk_DieppeDam_destroyed && osk_NeufchatelFactory_destroyed && osk_ForgesFuelStorage_destroyed && osk_Blue10AirKills && osk_Blue10GroundKills && BlueTotalF >= 50 && BlueTotalF > RedTotalF + 10)
        {
            osk_BlueObjCompleted += "10 more Team Kills than Red - ";
            osk_MapTurned = true;
            EndMission(300, "BLUE");

        }


    }

    /******************************************************************************
     * 
     * Routines dealing with the LONG TERM CAMPAIGN and calculating the points
     * for each team that determine the current campaign status
     * and which map will be used next mission
     *      
     ******************************************************************************/

    //CalcMapMove - returns a double with DOUBLE the current mission score and STRING the text message detailing the score
    public Tuple <double, string> CalcMapMove(string winner, bool final = true, bool output = true, Player player = null) {
        double MapMove = 0;
        string msg = "";
        string outputmsg = "";
        Player[] recipients = null;
        if (player != null) recipients = new Player[] { player };
        if (winner == "Red")
        {
            msg = "Red moved the campaign forward by 100 points by achieving all Mission Objectives and turning the map!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            return new Tuple<double, string>(1, outputmsg);
        }
        if (winner == "Blue")
        {
            msg = "Blue moved the campaign forward by 100 points by achieving all Mission Objectives and turning the map!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            return new Tuple<double, string>(-1, outputmsg);
        }

        if (RedTotalF > 3)
        {
            msg = "Red has moved the campaign forward through its " + RedTotalF.ToString("n1") + " total victories!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += RedTotalF / 100;
        }
        if (BlueTotalF > 3)
        {
            msg = "Blue has moved the campaign forward through its " + BlueTotalF.ToString("n1") + " total victories!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= BlueTotalF / 100;
        }

        double difference = RedTotalF - BlueTotalF;
        if (Math.Abs(difference) >= 5)
        {
            if (difference > 0)
            {
                msg = "Red has moved the campaign forward by getting " + difference.ToString("n0") + " more total victories than Blue!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (difference < 0) {
                msg = "Blue has front the campaign forward by getting " + (-difference).ToString("n0") + " more total victories than Red!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += difference / 100;
        }

        double air_difference = RedAirF - BlueAirF;
        
        if (Math.Abs(air_difference) >= 5)
        {
            if (air_difference > 0)
            {
                msg = "Red has moved the campaign forward by getting " + air_difference.ToString("n0") + " more air victories than Blue!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (air_difference < 0) {
                msg = "Blue has moved the campaign forward by getting " + (-air_difference).ToString("n0") + " more air victories than Red!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += air_difference / 100;
        }
        double ground_difference = RedAAF + RedNavalF + RedGroundF - BlueAAF - BlueNavalF - BlueGroundF;
        if (Math.Abs(ground_difference) >= 5)
        {
            if (ground_difference > 0)
            {
                msg = "Red has moved the campaign forward by getting " + ground_difference.ToString("n0") + " more ground victories than Blue!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            if (ground_difference < 0)
            {
                msg = "Blue has moved the campaign forward by getting " + (-ground_difference).ToString("n0") + " more ground victories than Red!";
                outputmsg += msg + Environment.NewLine;
                if (output) gpLogServerAndLog(recipients, msg, null);
            }
            MapMove += ground_difference / 100;
        }

        if (CampaignMapRedPoints > 0)
        {
            msg = "Red has moved the campaign forward by getting " + CampaignMapRedPoints.ToString("n0") + " points from destroying airports, important objectives, and Mission Objectives!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += CampaignMapRedPoints / 100;
        }

        if (CampaignMapBluePoints > 0)
        {
            msg = "Blue has moved the campaign forward by getting " + CampaignMapBluePoints.ToString("n0") + " points from destroying airports, important objectives, and Mission Objectives!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= CampaignMapBluePoints / 100;
        }
        if (RedPlanesWrittenOffI >= 3)
        {
            msg = "Red has lost ground by losing " + RedPlanesWrittenOffI.ToString() + " aircraft in battle!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove -= (double)RedPlanesWrittenOffI / 100;  //These are LOSSES, so - points for red & + points for blue
        }
        if (BluePlanesWrittenOffI >= 3)
        {
            msg = "Blue has lost ground by losing " + BluePlanesWrittenOffI.ToString() + " aircraft in battle!";
            outputmsg += msg + Environment.NewLine;
            if (output) gpLogServerAndLog(recipients, msg, null);
            MapMove += (double)BluePlanesWrittenOffI / 100; //These are LOSSES, so - points for red & + points for blue
        }

        if (final) {

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

        return new Tuple<double, string> (MapMove, outputmsg);

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
            if ( output ) gpLogServerAndLog(recipients, msg, null);
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
    public void SaveMapState(string winner)
    {
        if (MapStateSaved) return; //Due to the way it works (adding a certain value to the value in the file), we can only save map state ONCE per session.  So we can call it a few times near the end to be safe, but it only will save once at most
        try
        {

            Tuple<double, string> res = CalcMapMove(winner,true, true, null);
            double newMapState = CampaignMapState + res.Item1;
            string outputmsg = res.Item2;
            string msg = "";

            outputmsg += summarizeCurrentMapstate(newMapState, true);


            //TODO: We could write outputmsg to a file or send it to the -stats.cs or something
            //This saves the summary text to a file with CR/LF replaced with <br> so it can be used in HTML page

            try
            {
                File.WriteAllText(STATSCS_FULL_PATH + "CampaignSummary.txt", Regex.Replace(outputmsg, @"\r\n?|\n", "<br>" + Environment.NewLine));
            }
            catch (Exception ex) { Console.WriteLine("CampaignSummary Write: " + ex.ToString()); }

            string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState.txt";
            string filepath_old = STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState_old.txt";
            string currentContent = String.Empty;

            try
            {
                if (File.Exists(filepath_old)) { File.Delete(filepath_old); }
                File.Copy(filepath, filepath_old);
            } catch (Exception ex) { Console.WriteLine("MapState Write Inner: " + ex.ToString()); }

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

            currentContent = String.Join(Environment.NewLine, currentContent.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(s => s.Trim()).Take(20)); //cut down prev content to max of 20 lines & omit blank lines
            File.WriteAllText(filepath, newMapState.ToString() + Environment.NewLine + currentContent);
            MapStateSaved = true;
        }
        catch (Exception ex) { Console.WriteLine("MapState Write: " + ex.ToString()); }

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

        try
        {
            using (StreamReader sr = new StreamReader(STATSCS_FULL_PATH + CAMPAIGN_ID + "_MapState.txt"))
            {
                MapState = Convert.ToDouble(sr.ReadLine());
            }
        }
        catch (Exception ex) {
            System.Console.WriteLine("Main mission - read mapstate - Exception: " + ex.ToString());
            MapState = 0;
        }

        if (MapState > 100000 || MapState < -100000) MapState = 0;
        CampaignMapState = MapState;
        return MapState;


    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        #region stb
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
        #endregion
        //add your code here
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
                            || (player.PersonSecondary() != null && player.PersonSecondary().Health == 0))) {
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
    public void destroyAIAircraft(Player player) {


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
    public void listPositionAllAircraft(Player player, int playerArmy, bool inOwnArmy) {



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

        //wait_s = 1; //for testing
        //refreshtime_ms = 1 * 1000;

        enorfriend = "ENEMY";
        if (inOwnArmy) enorfriend = "FRIENDLY";
        if (playerArmy < 0 ) enorfriend = "BOTH ARMIES";

        if (player != null && (player.Place() is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
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
        if (radar_messages_store.TryGetValue(playername_index, out message_data)) {
            long time_elapsed_ms = currtime_ms - message_data.Item1;
            long time_until_new_s = (long)((refreshtime_ms - time_elapsed_ms) / 1000);
            long time_elapsed_s = (long)time_elapsed_ms / 1000;
            radar_messages = message_data.Item2;
            if (time_elapsed_ms < refreshtime_ms || message_data.Item1 == -1) {
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
        if (savenewmessages) {
            //When we start to work on the messages we save current messages (either blank or the previous one that was fetched from radar_messages_store)
            //with special time code -1, which means that radar returns are currently underway; don't give them any more until finished.
            radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(-1, radar_messages);

            if (RADAR_REALISM>0) GamePlay.gpLogServer(new Player[] { player }, "Fetching radar contacts, please stand by . . . ", null);




            radar_messages = new SortedDictionary<string, string>(new ReverseComparer<string>());//clear it out before starting anew . . .           
            radar_messages.Add("9999999999", " >>> " + enorfriend + " RADAR CONTACTS <<< ");

            if (RADAR_REALISM < 0) radar_messages.Add("9999999998", "p"+Calcs.GetMD5Hash(radarpasswords[playerArmy].ToUpper())); //first letter 'p' indicates passward & next characters up to space or EOL are the password.  Can customize this per  type of return, randomize each mission, or whatever.
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
                                        if (!player_place_set) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"                                                                        
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

                                        alt_km = a.Pos().z/1000;
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

                                    if (playerArmy==2) //metric for the Germanos . . . 
                                    {
                                        mi = (dis_m/1000).ToString("F0") + "k";
                                        mi_10 = mi;
                                        if (dis_m>30000) mi_10 = ((double)(Calcs.RoundInterval(dis_m,10000)) / 1000).ToString("F0") + "k";

                                        ft = alt_km.ToString("F2") + "k ";
                                        ftAGL = altAGL_m.ToString("F0") + "mAGL ";
                                        mph = (Calcs.RoundInterval(vel_mps *3.6,10)) .ToString("F0") + "k/h";
                                        ang = ((double)(Calcs.RoundInterval(alt_km*10,5))/10).ToString("F1") + "k ";
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
                                                he =heading_10.ToString("F0");

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
                                            aircraftType.Replace(',','_') + "," //Replace any commas since we are using comma as a delimiter here
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
                                            if ((playerArmy == -1 || playerArmy == -2)  && (
                                                           (altAGL_ft < 250 && altAGL_ft < random.Next(500)) || //Less then 250 ft AGL they start to phase out from radar
                                                           (altAGL_ft < 175) || //And, if they are less than 175 feet AGL, they are gone from radar                                                     
                                                           ((!isAI && isHeavyBomber) && poscount <= 2 && random.Next(4) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out 
                                                                                                                                 //However if the player heavy bombers group up they are MUCH more likely to show up on radar.  But they will still be harder than usual to track because each individual bomber will phase in/out quite often

                                                           (random.Next(7) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                           ) 
                                           ){ posmessage = ""; }


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
                                    } else if (RADAR_REALISM > 0) {

                                            //Trying to give at least some semblance of reality based on capabilities of Chain Home & Chain Home Low
                                            //https://en.wikipedia.org/wiki/Chain_Home
                                            //https://en.wikipedia.org/wiki/Chain_Home_Low
                                            if (random.Next(8) == 1) { //oops, sometimes we get mixed up on the type.  So sad . . .  See notes below about relative inaccuracy of early radar.
                                                type = "F";
                                                if (random.Next(3) == 1) type = "B";
                                            }                                            
                                            if (dis_mi <= 2 && a != p && Math.Abs( player_alt_m - a.Pos().z) < 5000) { posmessage = type + " nearby"; }


                                            //Below conditions are situations where radar doesn't work/fails, working to integrate realistic conditions for radar
                                            //To do this in full realism we'd need the full locations of Chain Home & Chain Home Low stations & exact capabilities
                                            //As an approximation we're using distance from the current aircraft, altitude, etc.
                                            /* wikipedia gives an idea of how rough early CH output & methods were: CH output was read with an oscilloscope. When a pulse was sent from the broadcast towers, a visible line travelled horizontally across the screen very rapidly. The output from the receiver was amplified and fed into the vertical axis of the scope, so a return from an aircraft would deflect the beam upward. This formed a spike on the display, and the distance from the left side – measured with a small scale on the bottom of the screen – would give target range. By rotating the receiver goniometer connected to the antennas, the operator could estimate the direction to the target (this was the reason for the cross shaped antennas), while the height of the vertical displacement indicated formation size. By comparing the strengths returned from the various antennas up the tower, altitude could be gauged with some accuracy.
                                             * Upshot is, exact #, position, no of aircraft, type of aircraft, altitude etc were NOT that precisely known.  Rather they were estimates/guesstimates based on strength of pulse of the radar return as viewed on an oscilliscope etc.
                                             * ******************/
                                            else if ((dis_mi >= 50 && poscount < 8 && random.Next(15) > 1 && !intcpt_reasonable_time) ||  //don't show enemy groups too far away, unless they are quite large, or can be intercepted in reasonable time.  Except once in a while randomly show one.
                                                     (dis_mi >= 25 && poscount < 4 && random.Next(12) > 1 && !intcpt_reasonable_time) ||
                                                     (dis_mi >= 15 && poscount <= 2 && random.Next(8) > 1 && !intcpt_reasonable_time) ||
                                                     (!climb_possible && playertype != "B" && army != playerArmy ) ||  //If the aircraft is too high for us to be able to climb to, we exclude it from the listing, unless the player is a bomber pilot (who is going to be interested in which planes are above in attack position) OR we are getting a listing of our own army, in which case we want all nearby a/c not just ones we can attack
                                                     (dis_mi >= 70 && altAGL_ft < 4500) || //chain home only worked above ~4500 ft & Chain Home Low had effective distance only 35 miles
                                                                                  //however, to implement this we really need the distance of the target from the CHL stations, not the current aircraft
                                                                                  //We'll approximate it by eliminating low contacts > 70 miles away from current a/c 
                                                     (dis_mi >= 10 && altAGL_ft < 500 && altAGL_ft < random.Next(500)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                                                                           //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                                                                           //phasing out targets below 250 feet.
                                                     (dis_mi < 10 && altAGL_ft < 250 && altAGL_ft < random.Next(500)) || //Within 10 miles though you really have to be right on the deck before the radar starts to get flakey, less than 250 ft. Somewhat approximating 50 foot alt lower limit.
                                                     (altAGL_ft < 175 ) || //And, if they are less than 175 feet AGL, they are gone from radar
                                                     ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi > 11 && poscount <= 2 && random.Next(4) <= 2) || // Breather bombers have a higher chance of being overlooked/dropping out, especially when further away.  3/4 times it doesn't show up on radar.
                                                     ((!isAI && isHeavyBomber && army != playerArmy) && dis_mi <= 11 && poscount <= 2 && random.Next(5) == 1) || // Breather bombers have a much higher chance of being overlooked/dropping out when close (this is close enough it should be visual range, so we're not going to help them via radar)
                                                     //((!isAI && type == "B" && army != playerArmy) && random.Next(5) > 0) || // Enemy bombers don't show up on your radar screen if less than 7 miles away as a rule - just once in a while.  You'll have to spot them visually instead at this distance!
                                                                                                                                       //We're always showing breather FIGHTERS here (ie, they are not included in isAI || type == "B"), because they always show up as a group of 1, and we'd like to help them find each other & fight it out
                                                     (random.Next(7) == 1)  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because. Early radar wasn't 100% reliable at all
                                                     ) { posmessage = ""; }
                                            else {
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
                                if (posmessage.Length > 0) {
                                    //gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                    //We add the message to the list along with an index that will allow us to reverse sort them in a logical/useful order                               
                                    int intcpt_time_index = (int)intcpt_time_min;
                                    if (intcpt_time_min <= 0 || intcpt_time_min > 99) intcpt_time_index = 99;

                                    try {
                                        string addMess = posmessage;
                                        if (RADAR_REALISM > 0) addMess = "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + posmessage;
                                        radar_messages.Add(
                                           ((int)intcpt_time_index).ToString("D2") + ((int)dis_mi).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                           addMess
                                        );
                                    }
                                    catch (Exception e) {
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
                            msg += py.Name() + " " + py.Army() + " " + pl +"\n";
                            
                        }
                        
                    }

                    sw.WriteLine("Players logged in: " + pycount.ToString() + " Active: " + pyinplace.ToString());
                    sw.WriteLine();

                    sw.WriteLine("MISSION SUMMARY");
                    if (playerArmy == -2 || playerArmy == -3) sw.WriteLine(osk_BlueObjDescription);
                    if (playerArmy == -1 || playerArmy == -3) sw.WriteLine(osk_RedObjDescription);                    
                    sw.WriteLine("Blue Objectives complete: " + osk_BlueObjCompleted);
                    sw.WriteLine("Red Objectives complete: " + osk_RedObjCompleted);
                    //sw.WriteLine("Blue/Red total score: " + (BlueTotalF).ToString("N1") + "/" + (RedTotalF).ToString("N1"));
                    sw.WriteLine( string.Format("BLUE session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", BlueTotalF,
  BlueAirF, BlueAAF, BlueNavalF,BlueGroundF));
                    sw.WriteLine(string.Format("RED session totals: {0:0.0} total points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground points", RedTotalF,
  RedAirF, RedAAF, RedNavalF, RedGroundF));
                    sw.WriteLine();

                    sw.WriteLine("CAMPAIGN SUMMARY");

                    Tuple<double, string> res = CalcMapMove("", false, false, null);
                    sw.Write( res.Item2);
                    double newMapState = CampaignMapState + res.Item1; 
                    sw.Write (summarizeCurrentMapstate(newMapState, false, null));

                    sw.WriteLine();
                    if (msg.Length > 0) sw.WriteLine("PLAYER SUMMARY");
                    sw.WriteLine(msg);
                    sw.WriteLine();
                    msg = ListAirfieldTargetDamage( null, -1, false, false); //Add the list of current airport conditions
                    if (msg.Length > 0 ) sw.WriteLine("AIRFIELD CONDITION SUMMARY");
                    sw.WriteLine(msg);

                    sw.Close();


                }
                catch (Exception ex) { Console.WriteLine("Radar Write1: " + ex.ToString()); }
            }

            var saveRADAR_REALISM = RADAR_REALISM;
            Timeout(wait_s, () => {
                //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               


                foreach (var mess in radar_messages) {

                    if (saveRADAR_REALISM == 0) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                    else if (saveRADAR_REALISM >= 0) gpLogServerAndLog(new Player[] { player }, mess.Value, null);

                }
                radar_messages_store[playername_index] = new Tuple<long, SortedDictionary<string, string>>(currtime_ms, radar_messages);
                               
            });//timeout      
        }
    }//method radar     




    /////////////On Battle Started, load initial submissions////////////////////////
    /////This is to speed up initial start of the mission//////////////////
    ///TODO: Read initial submissions could wait until a player actually enters the mission, then load everything
    ///We would have to make sure all flights load via a trigger, not automatically when the mission begins
    ///We could also re-set the 90-minute server clock at that point
    ///Only problem is, triggers are set to a certain point on the mission clock.  Maybe there is a way to re-start the mission at that point to re-set that mission clock.

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
        //DebugAndLog( "Loading initial sub-missions: " + MISSION_ID + "-initsubmission");

        //When battle is started we re-start the Mission tick clock - setting it up to start events
        //happening when the first player connects
        MISSION_STARTED = false;
        START_MISSION_TICK = -1;
        COOP_START_MODE = true;
        START_COOP_TICK = -1;
        CheckCoop();  //Start the routine to enforce the coop start/no takeoffs etc
        EndMissionIfPlayersInactive(); //start routine to check if no players in game & stop the mission if so

        ReadInitialSubmissions(MISSION_ID + "-auto-generate-vehicles", 0, 0); //want to load this after airports are loaded
        ReadInitialSubmissions(MISSION_ID + "-stats", 1, 1);

        CampaignMapSuffix = GetMapSuffix();
        LoadRandomSubmission(MISSION_ID + "-" + "initairports" + CampaignMapSuffix); // choose which of the airport & front files to load initially
                                                                 //TODO: Make this dependent on which side has been winning or has turned the map or whatever
        ReadInitialSubmissions(MISSION_ID + "-initsubmission", 10, 1);

        //ReadInitialSubmissions(MISSION_ID + "-hud", 0);
        //Timeout(5, () => { statsMission.statsStart(); });                       

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

        Console.WriteLine("Battle Stopping");
        SaveMapState(""); //A call here just to be safe; we can get here if 'exit' is called etc, and the map state may not be saved yet . . . 
        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
    }


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
            if ((timespread == 0) && (wait == 0)) {
                GamePlay.gpPostMissionLoad(s);
                DebugAndLog(s + " file loaded");
                Console.WriteLine(s + " file loaded");
            } else {
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





    //////////////////////////////////////////////////////////////////////////////////////////////////

    //Listen to events of every mission
    public override void Init(maddox.game.ABattle battle, int missionNumber)
    {
        base.Init(battle, missionNumber);
        MissionNumberListener = -1; //Listen to events of every mission
        //This is what allows you to catch all the OnTookOff, OnAircraftDamaged, and other similar events.  Vitally important to make this work!
        //If we load missions as sub-missions, as we often do, it is vital to have this in Init, not in "onbattlestarted" or some other place where it may never be detected or triggered if this sub-mission isn't loaded at the very start.
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////

    ///MENU SYSTEM////////////////////////////

    bool dmgOn = false;
    bool EndMissionSelected = false;
    bool debugMenu = false;
    bool debugSave;
    int radar_realismSave;
    private void setMainMenu(Player player) {
        //GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Server Options - Users" }, new bool[] { true });
        if (admin_privilege_level(player) >= 1)
        {
            //ADMIN option is set to #9 for two reasons: #1. We can add or remove other options before it, up to 8 other options, without changing the Tab-4-9 admin access.  #2. To avoid accessing the admin menu (and it's often DANGEROUS options) by accident
            //{true/false bool array}: TRUE here indicates that the choice is a SUBMENU so that when it is selected the user menu will be shown.  If FALSE the user menu will disappear.  Also it affects the COLOR of the menu items, which seems to be designed to indicate whether the choice is going to DO SOMETHING IMMEDIATE or TAKE YOU TO ANOTHER MENU
            GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", "Mission Objectives", "", "", "", "", "Admin options" }, new bool[] { false, false, false, false, false, false, false, false, true });
        }                        
        else
        {
            GamePlay.gpSetOrderMissionMenu(player, true, 0, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", "Mission Objectives" }, new bool[] { false, false, false, false});

        }
    }

    private void setSubMenu1(Player player) {
        if (admin_privilege_level(player) >= 1) {

            string rollovertext = "(admin) End mission now/roll over to next mission";
            if (EndMissionSelected) rollovertext = "(admin) CANCEL End mission now command";
            if (admin_privilege_level(player) == 2)
                GamePlay.gpSetOrderMissionMenu(player, true, 1, new string[] { "(admin) Show detailed damage reports for all players (toggle)", "(admin) Toggle debug mode", "(admin) Show some internal stats",  rollovertext, "Return to User Menu" }, new bool[] { false, false, false,false, true });
            else GamePlay.gpSetOrderMissionMenu(player, true, 1, new string[] { "", "", "", rollovertext, "Return to User Menu" }, new bool[] { false, false, false, false, true });
        } else {
            setMainMenu(player);

        }
    }

    private void setSubMenu2(Player player) {
        //GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Spawn New AI Groups Now", "Dogfight mode: Remove AI Aircraft, stop new spawns (30 minutes)", "Delete all current AI Aircraft", "Show damage reports for all players", "Stop showing damage reports for all players"}, new bool[] { false, false, false, false, false } );
    }

    //object plnameo= GamePlay.gpPlayer().Name();  
    //string plname= GamePlay.gpPlayer().Name() as string;
    public override void OnOrderMissionMenuSelected(Player player, int ID, int menuItemIndex) {
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
            else if (menuItemIndex == 3)
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
          else if (ID == 0) { // sub menu

            if (menuItemIndex == 0) {
                //setSubMenu1(player);
                setMainMenu(player);
            } else if (menuItemIndex == 1)
            {
                Player[] all = { player };
                listPositionAllAircraft(player, player.Army(), false); //enemy a/c  
                if (DEBUG) {
                    DebugAndLog("Total number of AI aircraft groups currently active:");
                    if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
                    {

                        int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                        DebugAndLog(totalAircraft.ToString());
                        //GamePlay.gpLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
                    }
                }
                setMainMenu(player);
            } else if (menuItemIndex == 2)
            {
                Player[] all = { player };
                listPositionAllAircraft(player, player.Army(), true); //friendly a/c           
                if (DEBUG) {
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
            } else if (menuItemIndex == 3)
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
  
                    GamePlay.gpLogServer(null, osk_BlueObjDescription, new object[] { });
                    GamePlay.gpLogServer(null, osk_RedObjDescription, new object[] { });
                    GamePlay.gpLogServer(null, "Blue Objectives Completed: " + osk_BlueObjCompleted, new object[] { });

                    GamePlay.gpLogServer(null, "Red Objectives Completed: " + osk_RedObjCompleted, new object[] { });

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

    public int admin_privilege_level (Player player)
    {
        if (player == null || player.Name() == null) return 0;
        string name = player.Name();
        //name = "TWC_muggle"; //for testing
        if (admins_full.Contains(name)) return 2; //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
        if (admins_basic.Any(name.Contains)) return 1; //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
        return 0;

    }


    //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
    public override void OnPlayerConnected(Player player) {
        string message;
        //Not starting it here due to Coop Start Mode
        //if (!MISSION_STARTED) DebugAndLog("First player connected; Mission timer starting");
        //MISSION_STARTED = true;
        
        if (MissionNumber > -1) {
            setMainMenu(player);

            GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });

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
    public override void OnPlayerDisconnected(Player player, string diagnostic) {
        string message;
        if (MissionNumber > -1) {

            DateTime utcDate = DateTime.UtcNow;

            //utcDate.ToString(culture), utcDate.Kind
            //Write current time in UTC, what happened, player name
            message = utcDate.ToString("u") + " Disconnected " + player.Name() + " " + diagnostic;
            DebugAndLog(message);
        }
    }

    public override void OnPlayerArmy(Player player, int Army) {
        if (MissionNumber > -1) {
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
            if (!MISSION_STARTED) DebugAndLog("First player connected (OnPlayerArmy); Mission timer starting");
            MISSION_STARTED = true;
            GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
        }
    }
    public override void Inited() {
        if (MissionNumber > -1) {

            setMainMenu(GamePlay.gpPlayer());
            GamePlay.gpLogServer(null, "Welcome " + GamePlay.gpPlayer().Name(), new object[] { });

            Timeout(90, () => { SetAirfieldTargets(); });

            //OK, these are kludges to ensure that ATAG CLOD Commander works.  If the misison
            //takes too long to load etc it will just time out.
            //This approach may have unexpected ramifications, so we'll have to test.

            /*GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
            GamePlay.gpLogServer(null, "Battle begins!", new object[] { });


            Timeout(0, () =>
                      {
                          GamePlay.gpLogServer(null, "Battle begins!", new object[] { });
                      });
           */
        }
    }



    //DAMAGE REPORT SUBROUTINE

    public override void OnAircraftDamaged(int missionNumber, string shortName,
AiAircraft Aircraft, AiDamageInitiator DamageFrom, part.NamedDamageTypes WhatDamaged)
    {
        base.OnAircraftDamaged(missionNumber, shortName, Aircraft, DamageFrom, WhatDamaged);

        if (DamageFrom.Player != null)
        {
            if (dmgOn == true)
            {
                GamePlay.gpLogServer(new Player[] { DamageFrom.Player }, "{0} hits {1} : {2} \n", new object[]
 {DamageFrom.Player, shortName, WhatDamaged});
            }
        }

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
    public int calcTimeLeft()
    {
        int tickSinceStarted = Time.tickCounter() - START_MISSION_TICK;
        //int respawntick = respawnminutes * ticksperminute;
        //int timespenttick = (tickSinceStarted - tickoffset) % respawntick;
        //int timelefttick = respawntick - timespenttick;
        //int timespentminutes = Convert.ToInt32(((double)timespenttick / (double)ticksperminute));
        //int timeleftminutes = Convert.ToInt32(((double)timelefttick / (double)ticksperminute)); //This is going to be time left in session, given our new scheme (2X sessions per mission)
        int missiontimeleftminutes = Convert.ToInt32((double)(END_MISSION_TICK - tickSinceStarted) / (double)ticksperminute);
        
        return missiontimeleftminutes;
    }
    //Displays time left to player & also returns the time left message as a string
    //Calling with (null, false) will just return the message rather than displaying it
    public string showTimeLeft(Player player, bool showMessage = true) {
        int missiontimeleftminutes = calcTimeLeft();
        string msg = "Time left in mission " + MISSION_ID + ": " + missiontimeleftminutes.ToString() + " min.";
        if (!MISSION_STARTED) msg = "Mission " + MISSION_ID + " not yet started - waiting for first player to enter.";
        else if (COOP_START_MODE) msg = "Mission " + MISSION_ID + " not yet started - waiting for Co-op Start.";

        if (showMessage && player != null) GamePlay.gpLogServer(new Player[] { player }, msg, new object[] { });
        return msg;
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
        string msg_orig = msg;
        msg = msg.ToLower();
        Player player = from as Player;
        if (msg.StartsWith("<tl"))
        {
            showTimeLeft(from);
            //GamePlay.gp(, from);

        }
        else if (msg.StartsWith("<rr"))
        {
            // GamePlay.gpLogServer(new Player [] { player } , "On this server, you can't reload & refuel (historically unrealistic . . . ).  But if you land and take off again from the same base, your mission will continue unbroken (for stats purposes). EXCEPTION: If you have to land due to self-damage, your mission will end (for stats purposes). ", new object[] { });
            //GamePlay.gp(, from);

            //GamePlay.gpLogServer(new Player[] { player }, "On this server, we don't RR aircraft. But if you land, get a new plane, and take off again from the same base, your mission will continue unbroken (for stats purposes).", new object[] { });
        }
        else if (msg.StartsWith("<obj"))
        {

            Timeout(8, () =>
            {
                GamePlay.gpLogServer(null, osk_BlueObjDescription, new object[] { });
                GamePlay.gpLogServer(null, osk_RedObjDescription, new object[] { });
                GamePlay.gpLogServer(null, "Blue Objectives Completed: " + osk_BlueObjCompleted, new object[] { });

                GamePlay.gpLogServer(null, "Red Objectives Completed: " + osk_RedObjCompleted, new object[] { });
            });

        }
        else if (msg.StartsWith("<camlong")) //show current campaign state (ie map we're on) and also the campaign results for this mission so far, longer & more detailed analysis
        {
            Tuple<double, string> res = CalcMapMove("", false, true, player);
            //string outputmsg = res.Item2;
            //string msg = "";

            double newMapState = CampaignMapState + res.Item1;

            summarizeCurrentMapstate(newMapState, true, player);

        }
        else if (msg.StartsWith("<cam")) //show current campaign state (ie map we're on) and also the campaign results for this mission so far
        {


            Tuple<double, string> res = CalcMapMove("", false, false, player);
            double score = res.Item1 * 100;
            string mes = "Campaign score for this mission so far: ";
            if (score > 0) mes += "Red +" + score.ToString("n0");
            else if (score < 0) mes += "Blue +" + (-score).ToString("n0");
            else mes += "A tie!";
            GamePlay.gpLogServer(new Player[] { player }, mes, null);
            summarizeCurrentMapstate(CampaignMapState, true, player);

        }
        else if (msg.StartsWith("<stat"))
        {

            string str = WritePlayerStat(player);
            //split msg into a few chunks as gplogserver doesn't like long msgs
            int maxChunkSize = 100;
            for (int i = 0; i < str.Length; i += maxChunkSize)
                GamePlay.gpLogServer(new Player[] { player }, str.Substring(i, Math.Min(maxChunkSize, str.Length - i)), new object[] { });


        }
        else if (msg.StartsWith("<coop start") && admin_privilege_level(player) >= 1)
        {
            GamePlay.gpLogServer(new Player[] { player }, "HELP: Use command '<coop XXX' to change the co-op start time to add XXX more minutes", null);
            GamePlay.gpLogServer(new Player[] { player }, "HELP: Use command '<coop start' to start mission immediately", null);
            if (COOP_START_MODE)
            {

                COOP_MODE_TIME_SEC = 0;
                GamePlay.gpLogServer(new Player[] { player }, "CO-OP Mission will START NOW!", null);
            }
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, "<coop start command works only during initial Co-op Start Mode period", null);
            }

        }

        else if (msg.StartsWith("<coop") && admin_privilege_level(player) >= 1)
        {
            GamePlay.gpLogServer(new Player[] { player }, "HELP: Use command '<coop XXX' to change the co-op start time to add XXX minutes", null);
            GamePlay.gpLogServer(new Player[] { player }, "HELP: Use command '<coop start' to start mission immediately", null);
            if (COOP_START_MODE)
            {
                double time_sec = 5 * 60;
                string time_str = msg.Substring(5).Trim();
                double time_min = Convert.ToDouble(time_str);
                if (time_min != 0 || time_str == "0") time_sec = time_min * 60;


                COOP_MODE_TIME_SEC += time_sec;
                double time_left_sec = COOP_TIME_LEFT_MIN * 60 + time_sec;


                GamePlay.gpLogServer(new Player[] { player }, "CO-OP MODE start time added " + ((double)time_sec / 60).ToString("n1") + " minutes; ", null);
                GamePlay.gpLogServer(new Player[] { player }, (COOP_MODE_TIME_SEC / 60).ToString("n1") + " min. total Co-Op start period; " + (time_left_sec / 60).ToString("n1") + " min. remaining", null);
                Stb_Chat("CO-OP START MODE EXTENDED: " + (time_left_sec / 60).ToString("n1") + " min. until co-op start", null);
            }
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, "<coop command works only during initial Co-op Start Mode period", null);
            }

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
        else if (msg.StartsWith("<des") && admin_privilege_level(player) >= 2)
        {

            string name = msg.Substring(5);
            AiActor actor = GamePlay.gpActorByName(name) as AiActor;
            if (actor != null) GamePlay.gpLogServer(new Player[] { player }, "Destroying " + name, new object[] { });
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, name + " not found.  Must have exact name as reported in right-click/View Aircraft-Ships-Misc", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "with 'Show Object Names' selected.Example: '23:BoB_RAF_F_141Sqn_Early.000'", new object[] { });
            }
            (actor as AiCart).Destroy();

        }
        else if (msg.StartsWith("<plv") && admin_privilege_level(player) >= 2)
        {
            //Not really sure how this works, but this is a good guess.  
            if (player.PersonPrimary() != null) player.PlaceLeave(player.PersonPrimary().Place());
            if (player.PersonSecondary() != null) player.PlaceLeave(player.PersonSecondary().Place());
        }
        else if (msg.StartsWith("<mov") && admin_privilege_level(player) >= 2)
        {

            string name = msg.Substring(5);
            AiActor actor = GamePlay.gpActorByName(name) as AiActor;
            if (actor != null) GamePlay.gpLogServer(new Player[] { player }, "Moving to " + name, new object[] { });
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, name + " not found.  Must have exact name as reported in right-click/View Aircraft-Ships-Misc", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "with 'Show Object Names' selected.Example: '23:BoB_RAF_F_141Sqn_Early.000'", new object[] { });
            }
            putPlayerIntoAircraftPosition(player, actor, 0);

        }
        else if (msg.StartsWith("<admin") && admin_privilege_level(player) >= 1)
        {

            GamePlay.gpLogServer(new Player[] { player }, "Admin commands: <coop set initial co-op start length <pos (all a/c position)", new object[] { });


        }
        else if (msg.StartsWith("<admin") && admin_privilege_level(player) >= 2)
        {

            GamePlay.gpLogServer(new Player[] { player }, "FULL Admin commands: <pos full a/c position listing <debugon <debugoff <logon <logoff (turn debug/log on/off)", new object[] { });
            GamePlay.gpLogServer(new Player[] { player }, "<des (destroy an object by name) <plv force place leave <mov move actor", new object[] { });


        }
        else if ((msg.StartsWith("<help") || msg.StartsWith("<")) &&
            //Don't give our help when any of these typical -stats.cs chat commands are entered
            !(msg.StartsWith("<car") || msg.StartsWith("<ses") || msg.StartsWith("<rank") || msg.StartsWith("<rr")
            || msg.StartsWith("<ter") || msg.StartsWith("<air") || msg.StartsWith("<ac") || msg.StartsWith("<nextac"))

            )
        {
            Timeout(0.1, () =>
            {
                GamePlay.gpLogServer(new Player[] { player }, "Commands: <tl Time Left; <rr How to reload; <cam, <camlong Campaign status (short/long)", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "<coop Use Co-Op start mode only @ beginning of mission", new object[] { });
                //GamePlay.gp(, from);
            });
        }
    }  

    //Ground objects (except AA Guns) will die after 55 min when counted from their birth

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);
        //Ground objects (except AA Guns) will die after X min when counted from their birth
        //TODO: This is either 1. not working right or 2. causing problems when e.g. ships, jerrycans, etc suddenly disappear after 2 hours?
        if (actor is AiGroundActor)
            if ((actor as AiGroundActor).Type() != maddox.game.world.AiGroundActorType.AAGun)
                Timeout(2*60*60, () =>
                {
                    if (actor != null)
                    { (actor as AiGroundActor).Destroy(); }
                }
                        );
      //AI Aircraft will be destroyed after airspawn minutes (set above)
      //lesson learned: For some reason if the Callsign of a group is high (higher than 50 or so?) then that object is not sent through this routine.  ??!!
      //eg, 12, 22, 32, 45 all work, but not 91 or 88.  They just never come
      //to OnActorCreated at all . . . But they are in fact created
      AiAircraft a = actor as AiAircraft;
      
      Timeout(1.0, () => // wait 1 second for human to load into plane
      {
         /* if (DEBUG) GamePlay.gpLogServer(null, "DEBUGC: Airgroup: " + a.AirGroup() + " " 
           + a.CallSign() + " " 
           + a.Type() + " " 
           + a.TypedName() + " " 
           +  a.AirGroup().ID(), new object[] { });
         */
         
        if (a != null && isAiControlledPlane2(a)) {
      
      
         int ot=(respawnminutes)*60-10; //de-spawns 10 seconds before new sub-mission spawns in.
         //int brk=(int)Math.Round(19/20);
  
        
         /* if (DEBUG) GamePlay.gpLogServer(null, "DEBUGD: Airgroup: " + a.AirGroup() + " " 
           + a.CallSign() + " " 
           + a.Type() + " " 
           + a.TypedName() + " " 
           +  a.AirGroup().ID() + " timeout: " + ot, new object[] { });
         */   
        
        Timeout(ot-60, () =>  //message 60 seconds before de-spawning.
              {
                	if (actor != null && isAiControlledPlane2(actor as AiAircraft)) {
                    //GamePlay.gpHUDLogCenter("(Some) Old AI Aircraft de-spawning in 60 seconds");
                  }
  
              }
                      );
        
        Timeout(ot-5, () =>  
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
                   DebugAndLog ("DEBUG: Destroying: " + a.AirGroup() + " " 
                     + a.CallSign() + " " 
                     + a.Type() + " " 
                     + a.TypedName() + " " 
                     +  a.AirGroup().ID() + " timeout: " + ot);
                  if (actor != null && isAiControlledPlane2(actor as AiAircraft) )
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
      
      
   public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);        
        AiAction action = GamePlay.gpGetAction(ActorName.Full(missionNumber, shortName));
        if (action != null)
            action.Do();
            if (DEBUG) GamePlay.gpLogServer(null, "Mission trigger " + shortName + " executing now." , new object[] { });
    }

    /* EXPERIMENTAL VERSION; THIS ENDED UP IN -STATS.CS INSTEAD
    /////////////////////////??CIVILIAN TRIGGERS//////////////////////////////////  
    //Here we have our triggers and actions:  files for each new trigger an appropriate action needs to be called  


    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);

        //GamePlay.gpLogServer(null, "OnTrigger called: " + shortName + " " + missionNumber.ToString() + " " + active.ToString(), new object[] { });
        if (shortName.Contains("CaenCiv") && active) //simple triggers to start, 
        {
            AiAction action = GamePlay.gpGetAction(shortName);
            if (action != null)
            {
                action.Do(); // doesn't matter what the pre-set trigger action was, we'll do something different IF THIS LINE IS COMMENTED OUT
                             //if you un-comment action.Do() it will do the pre-set action from the mission file, then you can ALSO do some additional actions below if you like
                             //It is a good idea to un-comment action.Do() if you have some of your triggers set up with nice actions in the mission file, that you want to keep using                    

            }
            GamePlay.gpGetTrigger(shortName).Enable = true;  // need to reactivate the trigger, can be time delayed if u want
        }
    }
    */
    
    //Removes AIAircraft if they are off the map. Convenient way to get rid of
    //old a/c - just send them off the map
    public void RemoveOffMapAIAircraft()
    {  
      int numremoved=0;
      //The map parameters - if an ai a/c goes outside of these, it will be de-spawned.  You need to just figure these out based on the map you are using.  Set up some airgroups in yoru mission file along the n, s, e & w boundaries of the map & note where the waypoints are.
      double minX =8200;
      double minY =14500;
      double maxX =350000;
      double maxY =307500; 
      //////////////Comment this out as we don`t have Your Debug mode  
      DebugAndLog ("Checking for AI Aircraft off map, to despawn");
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
                              ( a.Pos().x <= minX ||
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
                          Timeout (numremoved * 10, () => { a.Destroy(); }); //Destory the a/c, but space it out a bit so there is no giant stutter 
                        
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

  public static double distance (double a, double b){
  
     return (double)Math.Sqrt(a*a+b*b);
  
  }

  public static double meters2miles (double a){
  
       return (a / 1609.344);
  
  }
  
  public static double meterspsec2milesphour (double a) {
       return (a * 2.23694);
  }
  
  public static double meters2feet (double a){
  
       return (a / 1609.344*5280);
  
  }
  public static double DegreesToRadians(double degrees) {
        return degrees * (Math.PI / 180.0);
  }
  public static double RadiansToDegrees(double radians) {
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
        double radAngle = Math.PI/2 - Math.Atan2(diffY, diffX);

        //Converts the radians in degrees
        double degAngle = RadiansToDegrees(radAngle);
        
         if (degAngle < 0) {
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

        return distance(diffX,diffY);
    }
   public static double CalculatePointDistance(
                            Vector3d startPoint,
                            Vector3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);

        return distance(diffX,diffY);
    } 
  public static double CalculatePointDistance(
                            Point3d startPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);

        return distance(diffX,diffY);
    }
   public static double CalculatePointDistance(
                            Vector3d startPoint)                            
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);

        return distance(diffX,diffY);
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
	public static Point3d calculateInterceptionPoint(Point3d a, Point3d v, Point3d b, double s) {
		double ox = a.x - b.x;
		double oy = a.y - b.y;
 
		double h1 = v.x * v.x + v.y * v.y - s * s;
		double h2 = ox * v.x + oy * v.y;
		double t;
		if (h1 == 0) { // problem collapses into a simple linear equation 
			t = -(ox * ox + oy * oy) / (2*h2);
		} else { // solve the quadratic equation
			double minusPHalf = -h2 / h1;
 
			double discriminant = minusPHalf * minusPHalf - (ox * ox + oy * oy) / h1; // term in brackets is h3
			if (discriminant < 0) { // no (real) solution then...
				return new Point3d(0,0,0);;;
			}
 
			double root = Math.Sqrt(discriminant);
 
			double t1 = minusPHalf + root;
			double t2 = minusPHalf - root;
 
			double tMin = Math.Min(t1, t2);
			double tMax = Math.Max(t1, t2);
 
			t = tMin > 0 ? tMin : tMax; // get the smaller of the two times, unless it's negative
			if (t < 0) { // we don't want a solution in the past
				return new Point3d(0,0,0);;
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

        return (int) degrees;
    }
    
    public static int RoundInterval(double number, int interval=10)
    {
        number = Math.Round((number / interval), MidpointRounding.AwayFromZero) * interval;

        
        return (int) number;
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
            if (firstDecimal >0 && firstDecimal <= 3) //If # is 1,2,3 then 50% of the time we get mixed up & get it wrong.  This is bec. radar can't always distinguish between 1,2,3 etc contacts.  "the height of the vertical displacement indicated formation size" - in other words it was a ROUGH estimate of the strength of the radar return, which they then turned into a guesstimate of how many a/c were in the formation.
            {
                firstDecimal = 2;
            }
            return (int)firstDecimal;
        }
    }
    
    #endregion
    
    //Salmo @ http://theairtacticalassaultgroup.com/forum/archive/index.php/t-4785.html
    public static string GetAircraftType (AiAircraft aircraft)
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


}

public class ReverseComparer2: IComparer<string>
{
    public int Compare(string x, string y)
    {
        // Compare y and x in reverse order.
        return y.CompareTo(x);
    }
}

public class ReverseComparer3<T> : IComparer<T> where T : IComparable<T> {
    public int Compare(T x, T y) {
        return y.CompareTo(x);
    }
}

public sealed class ReverseComparer<T> : IComparer<T> {
    private readonly IComparer<T> inner;
    public ReverseComparer() : this(null) { }
    public ReverseComparer(IComparer<T> inner) {
        this.inner = inner ?? Comparer<T>.Default;
    }
    int IComparer<T>.Compare(T x, T y) { return inner.Compare(y, x); }
}
