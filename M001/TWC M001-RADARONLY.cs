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
 * X Sometimes campaign score isn't saved at the end (CLoD . . . ), so figure out a way to save it every 10-15 minutes or so throughout the mission.  Just save the current campaign/map score as already calculated throughout the mission.
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

    Stopwatch stopwatch;
    Dictionary<string, Tuple<long, SortedDictionary<string, string>>> radar_messages_store;
    //Constructor
    public Mission() {
        random = new Random();
        stb_random = random;
        //constants = new Constants();
        MISSION_ID = "M001";
        SERVER_ID = "Mission Server"; //Used by General Situation Map app
        SERVER_ID_SHORT = "Mission"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
        //SERVER_ID_SHORT = "MissionTEST"; //Used by General Situation Map app for transfer filenames.  Should be the same for any files that run on the same server, but different for different servers
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
        CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/";  // DO NOT CHANGE
        FILE_PATH = @"missions/Multi/Fatal/" + MISSION_ID + "/";   // mission install directory (CHANGE AS NEEDED)   
        stb_FullPath = CLOD_PATH + FILE_PATH;
        MESSAGE_FILE_NAME = MISSION_ID + @"_message_log.txt";
        MESSAGE_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + MESSAGE_FILE_NAME;
        STATS_FILE_NAME = MISSION_ID + @"_stats_log.txt";
        STATS_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + STATS_FILE_NAME;
        LOG_FILE_NAME = MISSION_ID + @"_log_log.txt";
        LOG_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + LOG_FILE_NAME;
        STATSCS_FULL_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/missions/Multi/Fatal/";  // Must match location -stats.cs is saving SessStats.txt to             
        stopwatch = Stopwatch.StartNew();
        radar_messages_store = new Dictionary<string, Tuple<long, SortedDictionary<string, string>>>();
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




        //periodically remove a/c that have gone off the map
        if ((tickSinceStarted) % 2100 == 0) {

            RemoveOffMapAIAircraft();


        }


        if ((tickSinceStarted) % 10100 == 0) {
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

        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
        }

        ReadInitialSubmissions(MISSION_ID + "-stats", 1, 1);

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
            GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
            //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
        }
    }
    public override void Inited() {
        if (MissionNumber > -1) {

            setMainMenu(GamePlay.gpPlayer());
            GamePlay.gpLogServer(null, "Welcome " + GamePlay.gpPlayer().Name(), new object[] { });


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
        if (!msg.StartsWith("<") return; //trying to stop parser from being such a CPU hog . . . 
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
                //GamePlay.gpLogServer(new Player[] { player }, "<coop Use Co-Op start mode only @ beginning of mission", new object[] { });
                //GamePlay.gp(, from);
            });
        }
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
         
        if (a != null && isAiControlledPlane2(a)) {
      
      
         int ot=(destroyminutes)*60-10; //de-spawns 10 seconds before new sub-mission spawns in.
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
