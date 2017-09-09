//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//The two $references above + perhaps the [rts] scriptAppDomain=0 references on conf.ini & confs.ini are (perhaps!?) necessary for some of the code below to work, esp. intercepting chat messages etc.
///$reference parts/core/MySql.Data.dll  //THIS DOESN'T SEEM TO WORK
///$reference parts/core/System.Data.dll //THIS DOESN'T SEEM TO WORK

// v.1_18_00. script by oreva, zaltys, small_bee, bhugh, flug, fatal_error, several other contributors/online code snippets & examples

using System;
using System.Collections;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using part;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;



//[Serializable]
public class Mission : AMission
{
        Random random;
        //Constants constants; 
        
        public string MISSION_ID;
        public bool DEBUG;
        public bool LOG; //Whether to log debug messages to  a log file.
         
        public string USER_DOC_PATH;
        public string CLOD_PATH;
        public string FILE_PATH;
        public string MESSAGE_FILE_NAME;
        public string MESSAGE_FULL_PATH;
        public string STATS_FILE_NAME;
        public string STATS_FULL_PATH;
        public string LOG_FILE_NAME;
        public string LOG_FULL_PATH;
        public int RESPAWN_MINUTES;
        public int TICKS_PER_MINUTE;
        public double HOURS_PER_SESSION;
        public double NUMBER_OF_SESSIONS_IN_MISSION;
        public int END_SESSION_TICK;
        public int END_MISSION_TICK;
        public int RADAR_REALISM;
        public bool MISSION_STARTED = false;
        public int START_MISSION_TICK = -1;
        
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
    public Mission () {
        random = new Random();
        //constants = new Constants();
        respawn_on=true;
         MISSION_ID = "M001";
         DEBUG = false;
         LOG = true;
         
         USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
         CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover - MOD/";  // DO NOT CHANGE
         FILE_PATH = @"missions/Multi/Fatal/" + MISSION_ID + "/";   // mission install directory (CHANGE AS NEEDED)          
         MESSAGE_FILE_NAME = MISSION_ID + @"_message_log.txt";
         MESSAGE_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + MESSAGE_FILE_NAME;
         STATS_FILE_NAME = MISSION_ID + @"_stats_log.txt";
         STATS_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + STATS_FILE_NAME;
         LOG_FILE_NAME = MISSION_ID + @"_log_log.txt";
         LOG_FULL_PATH = CLOD_PATH + FILE_PATH + @"/" + LOG_FILE_NAME;



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
         TICKS_PER_MINUTE=1986; //empirically, based on a timeout test.  This is approximate & varies slightly.
         HOURS_PER_SESSION=1.5; //# of hours the entire session is to last before re-start
         NUMBER_OF_SESSIONS_IN_MISSION = 2; //we can repeat this entire sequence 1x, 2x, 3x, etc. OR EVEN 1.5, 2.5, 3.25 etc times  
         END_SESSION_TICK = (int) (TICKS_PER_MINUTE*60*HOURS_PER_SESSION); //When to end/restart server session
         RADAR_REALISM = (int) 5;

        respawnminutes=RESPAWN_MINUTES;
        ticksperminute=TICKS_PER_MINUTE;
        //endsessiontick = Convert.ToInt32(TICKS_PER_MINUTE*60*HOURS_PER_SESSION); //When to end/restart server session
        tickoffset=20 ; //allows resetting the sub-mission restart times if a sub-mission is re-spawned manually etc
        endsessiontick = END_SESSION_TICK;
        END_MISSION_TICK = (int)((double)END_SESSION_TICK * NUMBER_OF_SESSIONS_IN_MISSION);

        //choose 2 times for hurry strafe mission to spawn, once in 1st half of mission & once in 2nd half        
        randHurryStrafeTick1 = random.Next((int)0/90,(int)(endsessiontick*45/90));  //between minute 0 & 45        
        randHurryStrafeTick2 = random.Next((int)(endsessiontick*46/90),endsessiontick*90/90);  //between minute 46 & 90
        //Choose 4X for Blue raids to spawn 
        randBlueTick1 = random.Next((int)(endsessiontick*5/90),(int)(endsessiontick*18/90));  //between minute 5 & 18
        //randBlueTick1 = random.Next((int)(endsessiontick/180),(int)(endsessiontick/178));//FOR TESTING; load a submission about 1 minute after mission start
        randBlueTick2 = random.Next((int)(endsessiontick*19/90),(int)(endsessiontick*36/90));//another between minutes 19 & 36
        randBlueTick3 = random.Next((int)(endsessiontick*50/90),(int)(endsessiontick*68/90)); //another between minutes 50 & 68 
        randBlueTick4 = random.Next((int)(endsessiontick*70/90),(int)(endsessiontick*77/90)); //another between minutes 70 & 77
        
        randBlueTickInitRaid = random.Next((int)(endsessiontick*1/90),(int)(endsessiontick*2/90)); //spawn a major bomber raid in between minutes 1 & 2 of 90        ;
        randBlueTickRaid= random.Next((int)(endsessiontick*43/90),(int)(endsessiontick*47/90)); //spawn a major bomber raid in between minutes 43 & 47 of 90        
        randBlueTickLateRaid= random.Next((int)(endsessiontick*52/90),(int)(endsessiontick*66/90)); //spawn a major bomber raid in between minutes 54 & 64 of 90        
        randBlueTickFighterRaid1 = random.Next((int)(endsessiontick*1/90),(int)(endsessiontick*5/90)); //spawn a fighter raid in between minutes 1 & 5 of 90
        randBlueTickFighterRaid2 = random.Next((int)(endsessiontick*28/90),(int)(endsessiontick*32/90)); //spawn a fighter raid in between minutes 54 & 64 of 90
        randBlueTickFighterRaid3 = random.Next((int)(endsessiontick*57/90),(int)(endsessiontick*65/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        //GamePlay.gpLogServer(null, "Mission class initiated.", new object[] { });
        
        randRedTickFighterRaid1 = random.Next((int)(endsessiontick*2/90),(int)(endsessiontick*6/90)); //spawn a fighter raid in between minutes 1 & 5 of 90
        randRedTickFighterRaid2 = random.Next((int)(endsessiontick*13/90),(int)(endsessiontick*17/90)); //spawn a fighter raid in between minutes 54 & 64 of 90
        randRedTickFighterRaid3 = random.Next((int)(endsessiontick*23/90),(int)(endsessiontick*28/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid4 = random.Next((int)(endsessiontick*33/90),(int)(endsessiontick*37/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid5 = random.Next((int)(endsessiontick*43/90),(int)(endsessiontick*48/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid6 = random.Next((int)(endsessiontick*52/90),(int)(endsessiontick*59/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid7 = random.Next((int)(endsessiontick*62/90),(int)(endsessiontick*69/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid8 = random.Next((int)(endsessiontick*73/90),(int)(endsessiontick*78/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid9 = random.Next((int)(endsessiontick*79/90),(int)(endsessiontick*82/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid10 = random.Next((int)(endsessiontick*33/90),(int)(endsessiontick*82/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
        randRedTickFighterRaid11 = random.Next((int)(endsessiontick*17/90),(int)(endsessiontick*82/90)); //spawn a fighter raid in between minutes 57 & 65 of 90
		
        stopwatch = Stopwatch.StartNew();
        radar_messages_store = new Dictionary<string, Tuple<long, SortedDictionary<string,string>>> ();
   } 
  

    
// loading sub-missions
public override void OnTickGame()
{

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

    if (START_MISSION_TICK == -1) START_MISSION_TICK = Time.tickCounter();

    int tickSinceStarted = Time.tickCounter() - START_MISSION_TICK;

    int respawntick=respawnminutes*ticksperminute; // How often to re-spawn new sub-missions & do other repetitive tasks/messages. 27000=15 min repeat. 1800 'ticks' per minute or  108000 per hour.  I believe that this is approximate, not exact.
    
    if ( (tickSinceStarted) == 0) {
        //GamePlay.gpLogServer(null, "Mission class initiated 2.", new object[] { });
        GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });

        /*Timeout(60, () =>  //how many ticks in 60 seconds
                  {
                     //DebugAndLog ( "Debug/one minute: " + Time.tickCounter().ToString() + " " + tickoffset.ToString());
        }); */
    }

    if ( (tickSinceStarted) == 200) {
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
    if ( (tickSinceStarted) % 2100 == 0) {
       
       RemoveOffMapAIAircraft();
       
       
    }
    
    

    if ( (tickSinceStarted) % 10100 == 0) {
         //DebugAndLog ("Debug: tickcounter: " + Time.tickCounter().ToString() + " tickoffset" + tickoffset.ToString());
         
         DebugAndLog ( "Total number of AI aircraft groups currently active:");
          if (GamePlay.gpAirGroups(1) != null) DebugAndLog ( GamePlay.gpAirGroups(1).Length.ToString() + " Red airgroups"); 
          if (GamePlay.gpAirGroups(2) != null) DebugAndLog ( GamePlay.gpAirGroups(2).Length.ToString() + " Blue airgroups");          

           
         //display time left
         int timespenttick = (tickSinceStarted - tickoffset) % respawntick;
         int timelefttick = respawntick - timespenttick;
         int timespentminutes=Convert.ToInt32(((double)timespenttick/(double)ticksperminute));
         int timeleftminutes=Convert.ToInt32(((double)timelefttick/(double)ticksperminute));
         int missiontimeleftminutes = Convert.ToInt32((double)(END_MISSION_TICK- tickSinceStarted) /(double)ticksperminute);
         if ( missiontimeleftminutes > 1 ) {
             string msg = missiontimeleftminutes.ToString() + " min. left in mission " + MISSION_ID;
             if (!MISSION_STARTED) msg = "Mission not yet started - waiting for first player to enter.";
             GamePlay.gpLogServer(null, msg , new object[] { });
             GamePlay.gpHUDLogCenter(msg);
            }  
         //Write all a/c position to log
         if (LOG) {
           DebugAndLog (missiontimeleftminutes.ToString() + " min. left in mission " + MISSION_ID);
           int saveRealism=RADAR_REALISM; //save the accurate radar contact lists
           RADAR_REALISM=0;
           listPositionAllAircraft(GamePlay.gpPlayer(),1, true);
           listPositionAllAircraft(GamePlay.gpPlayer(),1, false);
           RADAR_REALISM=saveRealism;
         }

    }

    if (tickSinceStarted == 900 ) //load initial ship missions.  Note that these are loaded ONCE only @ start of mission (tickSinceStarted) NOT at the start of each session (tickSession)
    {

        LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITIALSHIPS"); // load sub-mission            
        
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
    if ( currSessTick == randBlueTick1 || currSessTick == randBlueTick2 || currSessTick == randBlueTick3 || currSessTick == randBlueTick4 )    
    {
     
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionBLUE"); // sub-mission            
    }

    if (currSessTick == 500 ) //load initial aircraft missions  &re-load them @ the start of every new session
    {

        LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITIALBLUE"); // load sub-mission            
        
     }

     if (currSessTick == 700 ) //load initial aircraft missions  &re-load them @ the start of every new session
    {

            
            LoadRandomSubmission(MISSION_ID + "-" + "randsubmissionINITIALRED"); // load sub-mission            
    }

        //Load the initial Blue bomber raids approx minutes 1-2
        if ( currSessTick == randBlueTickInitRaid)    
    {
     
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionINITBLUE-bomber"); // load sub-mission            

        
    }
    
    //Load the major Blue fighter raids approx every 30 minutes
    if ( currSessTick == randBlueTickFighterRaid1 || currSessTick == randBlueTickFighterRaid2 || currSessTick == randBlueTickFighterRaid3)    
    {
     
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionRAIDBLUE-fighter"); // load sub-mission            

        
    }     
    
    
    //Load the major RED fighter raids approx every 20 minutes
    if ( currSessTick == randRedTickFighterRaid1 || currSessTick == randRedTickFighterRaid2 || currSessTick == randRedTickFighterRaid3 || currSessTick == randRedTickFighterRaid4|| currSessTick == randRedTickFighterRaid5 || currSessTick == randRedTickFighterRaid6 || currSessTick == randRedTickFighterRaid7 || currSessTick == randRedTickFighterRaid8 || currSessTick == randRedTickFighterRaid9 || currSessTick == randRedTickFighterRaid10 || currSessTick == randRedTickFighterRaid11 )   
    {                      
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionREDaircover"); // load sub-mission            
        
    }     

    //Load the major bomber & fighter raids between 2400 & 2500 seconds in
    if ( currSessTick == randBlueTickRaid )    
    {
                     	    
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionRAIDBLUE-bomber"); // load bomber sub-mission a bit later                    	                    
        
    }  

    if ( currSessTick == randBlueTickLateRaid )    
    {
     
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionLATERAIDBLUE"); // load sub-mission                                
        
    }  
    
    
    ///load RED randHurryStrafe missions @ random times
    if ( currSessTick == randHurryStrafeTick1 || currSessTick == randHurryStrafeTick2 )
    //FOR TESTING:
    //if ( currSessTick == 1000 || currSessTick == 3000 )     
    {
     
        LoadRandomSubmission (MISSION_ID + "-" + "randsubmissionREDhurrystrafe"); // sub-mission            
    }
        
    displayMessages (tickSinceStarted, tickoffset, respawntick);
    if ( respawn_on && ( (tickSinceStarted - tickoffset) % respawntick == 0) )     
    {
      //setMainMenu(GamePlay.gpPlayer());  //initialize menu
      //LoadRandomSubmission (); // sub-mission            
    }
    
    
    if (tickSinceStarted == END_MISSION_TICK) EndMission(60);// Out of time/end server.  ATAG Server monitor (CloD Watchdog) will auto-start the next mission when this one closes the launcher -server instance.
      

    

     
}

   //TO DISPLAY VARIOUS MESSAGES AT VARIOUS TIMES IN THE MISSION CYCLE
   public void displayMessages (int tick=0, int tickoffset=0, int respawntick=20000) {
   
    int msgfrq=1; //how often to display server messages.  Will be displayed 1/msgfrq times
     

    if ( (random.Next(msgfrq+1)==1) && (tick) % 40000 == 38400) // 27000=15 min repeat. 10000=another few minutes' delay
     
    {
        GamePlay.gpLogServer(null, "The Wrecking Crew (TWC) is always looking for new pilots. Check us out at TWCCLAN.COM.", new object[] { });
    }
        if ( (random.Next(msgfrq+1)==1) && (tick) % 40000 == 1900) // 27000=15 min repeat. 10000=another few minutes' delay
     
    {
        GamePlay.gpLogServer(null, ">> SERVER TIP: Read the Mission Briefing for important instructions <<", new object[] { });
    }
    if ( (random.Next(msgfrq+1)==1) && (tick) % 40000 == 20200) 
    {
        GamePlay.gpLogServer(null, ">> SERVER TIP: Use TAB-4-1 for special server commands <<", new object[] { });
    }
    if ( (random.Next(msgfrq+1)==1) && (tick) % 40000 == 15300) 
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
      DebugAndLog ( "Num matching submissions found in directory: " + list.Count);
      return list;
  }
  
  //END MISSION WITH WARNING MESSAGES ETC/////////////////////////////////
  public void EndMission (int endseconds=0) {
            GamePlay.gpLogServer(null, "Mission is restarting soon!!!", new object[] { });
                    	GamePlay.gpHUDLogCenter("Mission is restarting soon!!!");
            Timeout(endseconds, () =>
        	    {
                    	GamePlay.gpLogServer(null, "Mission is restarting in 1 minute!!!", new object[] { });
                    	GamePlay.gpHUDLogCenter("Mission is restarting in 1 minute!!!");
        	    });
            Timeout(endseconds+30, () =>
        	    {
                    	GamePlay.gpLogServer(null, "Server Restarting in 30 seconds!!!", new object[] { });
                    	GamePlay.gpHUDLogCenter("Server Restarting in 30 seconds!!!");
        	    });
            Timeout(endseconds+60, () =>
        	    {
                    	GamePlay.gpLogServer(null, "Mission ended. Please wait 2 minutes to reconnect!!!", new object[] { });
                    	GamePlay.gpHUDLogCenter("Mission ended. Please wait 2 minutes to reconnect!!!");
                      DebugAndLog("Mission ended.");
                      //GamePlay.gpBattleStop(); //It would be nice to do this but if you do, the script stops here.
        	    });
            Timeout(endseconds+65, () =>
        	    {
                    Process.GetCurrentProcess().Kill();
        	    });

    }
  
  public bool LoadRandomSubmission (string fileID = "randsubmission") 
   {   
        //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
        //GamePlay.gpHUDLogCenter("Respawning AI air groups");
        //GamePlay.gpLogServer(null, "RESPAWNING AI AIR GROUPS. AI Aircraft groups re-spawn every " + RESPAWN_MINUTES + " minutes and have a lifetime of " + RESPAWN_MINUTES + "-" + 2*RESPAWN_MINUTES + " minutes. The map restarts every " + Convert.ToInt32((float)END_SESSION_TICK/60/TICKS_PER_MINUTE) + " hours.", new object[] { });
                
        bool ret=false;
        
        List<string> RandomMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH, fileID); // Gets all files with with text MISSION_ID-fileID (like "M001-randsubmissionREDBOMBER") in the filename and ending with .mis
        //if (DEBUG) 
        DebugAndLog ("Debug: Choosing from " + RandomMissions.Count + " missions to spawn. " + fileID + " " + CLOD_PATH + FILE_PATH);
        if (RandomMissions.Count > 0)
        {
             string RandomMission = RandomMissions[random.Next(RandomMissions.Count)];
             GamePlay.gpPostMissionLoad(RandomMission);
             ret=true;
             
             //if (DEBUG) 
             DebugAndLog ("Loading mission " + RandomMission);
             DebugAndLog ("Current time: "+DateTime.UtcNow.ToString("O"));
        }
         
        return ret;
    

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
            logToFile (data, MESSAGE_FULL_PATH); 
        }
        
   public void logStats(object data)
        {          
            logToFile (data, STATS_FULL_PATH); 
        }

   public void DebugAndLog(object data)
        {
            if (DEBUG) GamePlay.gpLogServer(null, (string)data , new object[] { });          
            if (!DEBUG && LOG) Console.WriteLine((string)data); //We're using the regular logs.txt as the logfile now logToFile (data, LOG_FULL_PATH); 
        }
   public void gpLogServerAndLog(Player[] to, object data, object[] third)
        {
            //this is already logged to logs.txt so no need for this: if (LOG) logToFile (data, LOG_FULL_PATH);
            GamePlay.gpLogServer(to, (string)data , third);          
             
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
            double M_Health=0;
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
    private bool isAiControlledPlane (AiAircraft aircraft) 
    {
  		if (aircraft == null) 
          { 
  			return false;
  		}
  
  		Player [] players = GamePlay.gpRemotePlayers ();
  		foreach (Player p in players) 
          {    
  			if (p != null && (p.Place () is AiAircraft) && (p.Place () as AiAircraft) == aircraft)
              { 
  				return false;
  			}
		}

		return true;
	}

	private void destroyPlane (AiAircraft aircraft) {
		if (aircraft != null) { 
			aircraft.Destroy ();
		}
	}

	private void explodeFuelTank (AiAircraft aircraft) 
    {
		if (aircraft != null) 
        { 
			aircraft.hitNamed (part.NamedDamageTypes.FuelTank0Exploded);
		}
	}

	private void destroyAiControlledPlane (AiAircraft aircraft) {
		if (isAiControlledPlane2 (aircraft)) {
			destroyPlane (aircraft);
		}
	}

	private void damageAiControlledPlane (AiActor actor) {
		if (actor == null || !(actor is AiAircraft)) { 
			return;
		}

		AiAircraft aircraft = (actor as AiAircraft);

		if (!isAiControlledPlane (aircraft)) {
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

        Timeout (300, () =>
				{destroyPlane (aircraft);}
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
       

	public override void OnPlaceLeave (Player player, AiActor actor, int placeIndex) 
    {
		base.OnPlaceLeave (player, actor, placeIndex);
        string pName="";
        if (player!=null) pName=player.Name();
        if (actor is AiAircraft) {
    
    	    Timeout (0.5f, () => //5 sec seems too long, the ai vigorously takes control sometimes, and immediately.  Perhaps even 1 second or .5 better than 2.
    			    {
                damageAiControlledPlane (actor);
                Console.WriteLine ("Player left plane; damaged aircraft so that AI cannot assume control " + pName + " " + (actor as AiAircraft).Type());
              }
    		    );
        }    
        DateTime utcDate = DateTime.UtcNow;  
        logStats(utcDate.ToString("u") + " " + player.Name() + " " + WritePlayerStat(player));  
      
	}

	public override void OnPlaceEnter (Player player, AiActor actor, int placeIndex) 
  {
		base.OnPlaceEnter (player, actor, placeIndex);
    if (player != null ) setMainMenu( player );
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

   
  # region onbuildingkilled
    /*
    //OnBuildingKilled only works on offline servers
    public override void OnBuildingKilled(string title, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnBuildingKilled(title, pos, initiator, eventArgInt);

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

    # endregion


    
    #region stationarykilled
    //OnStationaryKilled is basically used for dam bombing effects.  It could potentially be used for other things, however
        /*
         * FG28_Kodiak
         * http://forum.1cpublishing.eu/archive/index.php/t-34985.html
         * 
         * GamePlay.gpGroundStationarys is overloaded:
         * GamePlay.gpGroundStationarys(double x, double y, double radius)
         * GamePlay.gpGroundStationarys(string country)
         * GamePlay.gpGroundStationarys(string country, double y, double radius)
         * 
         * you get a Array of GroundStationary on a Position with a given radius, additional you can specify a country.Or you can get all Stationaries of a country you specify.
         * 
         * Statics & buildings are NOT actors!
         * 
         */
    public override void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnStationaryKilled(missionNumber,stationary, initiator, eventArgInt);

        GamePlay.gpLogServer(null, "Stationary:" + stationary.Name + " in " + stationary.country + " was destroyed by " + initiator.Player.Name() + " from the " + initiator.Player.Army() + ".", new object[] { });

        if (stationary.Name == "0:Static42" )  //we can check for various things being killed with || but then we will end up loading the submission multiple times. So just once is best.
        {
            string name = "!";
            if (initiator.Player.Name() != null) name = ", " + initiator.Player.Name() + "!";

            GamePlay.gpLogServer(null, "Dam @ Le Harve has been eliminated. Well done"+ name, new object[] { });
            GamePlay.gpHUDLogCenter("Dam @ Le Harve has been eliminated. Well done" + name);
            //Dam @ Le Harve has been eliminated. Well done PlayerName!

            //you can specify a submission here to load that will create a bunch of smoke and fire or whatever.
            //
            
                GamePlay.gpPostMissionLoad(CLOD_PATH + FILE_PATH + "TWC M002-damdestruction.mis");

                foreach (GroundStationary sta in GamePlay.gpGroundStationarys(stationary.pos.x, stationary.pos.y, 500))
            {
                if (sta.Name == "0:Static41" || sta.Name == "0:Static42" || sta.Name == "0:Static43") //Need to list the name of EACH item that should be removed/destroyed. We'll have trouble here if editing the file renumbers the statics . . . 
                {
                   //Let the fire/explosion get revved up a bit, then destroy/remove the center bit
                   Timeout(60, () =>
                   {
                       sta.Destroy();
                       DebugAndLog("Dam bombed; destroying stationary " + sta.Name);
                       GamePlay.gpLogServer(null, "Dam bombed; destroying stationary " + sta.Name, new object[] { });
                   });
                }

                //sta.Destroy();
            }

        }


    }

    # endregion

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
                Timeout(90*60, () => { 
                (actor as AiGroundActor).Destroy();
                Console.WriteLine ("Destroyed dead ground object " + actor.Name()); 
              
                });

               Console.WriteLine("Ground object has died. Name: " + actor.Name());

            }



        }
        catch (Exception ex) { Console.WriteLine("OPD: "+ ex.ToString()); }
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
                    if (player.PersonPrimary() != null && player.PersonPrimary().Health==0
                        && (player.PersonSecondary() == null 
                            || (player.PersonSecondary() != null && player.PersonSecondary().Health==0 ))){
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


    public override void OnAircraftCrashLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
		base.OnAircraftCrashLanded (missionNumber, shortName, aircraft);
		Timeout (300, () =>
            //{ destroyPlane(aircraft); } //Not sure why to destory all planes just bec. crash landed?  Best to check if a pilot is still in it & just destroy aicontrolled planes, like this:
            
            { destroyAiControlledPlane (aircraft); }
			);
	}
    public override void OnAircraftLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        Timeout(300, () =>
              //{ destroyPlane(aircraft); } //Not sure why to destory **ALL** planes just bec. landed?  Best to check if a pilot is still in it & just destroy aicontrolled planes, like this:
              
              { destroyAiControlledPlane (aircraft); }
            );
    }

 //this will destroy ALL ai controlled aircraft on the server
 public void destroyAIAircraft (Player player) {    


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
  
  //List all a/c positions to console///////////////////////////
  //Radar . . . 
  public string posmessage;
  public int poscount;
  public void listPositionAllAircraft (Player player, int playerArmy, bool inOwnArmy) {
          
       
      
        // int RADAR_REALISM;     //realism = 0 gives exact position, bearing, velocity of each a/c.  We plan to make various degrees of realism ranging from 0 to 10.  Implemented now is just 0=exact, >0 somewhat more realistic    
        AiAircraft p = null;
        Point3d pos1;
        Point3d pos2;
        Point3d VwldP, intcpt;
        Vector3d Vwld,player_Vwld;
        double player_vel_mps=0;
        double player_vel_mph=0;
        string type,player_sector;        
        bool player_place_set=false;
        double vel_mps=0;
        double vel_mph=0;
        int vel_mph_10=0;
        double heading=0;
        int heading_10=0;
        double dis=0;
        int dis_10=0;
        double bearing=0;                             
        int bearing_10=0;
        double alt=0;
        int alt_angels=0;
        string sector="";                                                      
        double intcpt_heading=0;
        double intcpt_time_min=0;
        string intcpt_sector="";
        bool intcpt_reasonable_time=false; 
        int aigroup_count=0;
        string playername="TWC_server_159273";
        string playername_index;
        string enorfriend;
        long currtime_ms=0;
        long storedtime_ms=-1;
        bool savenewmessages=true;
        Tuple<long, SortedDictionary<string,string>> message_data;
        SortedDictionary<string, string> radar_messages = 
            new SortedDictionary<string, string>(new ReverseComparer<string>());
        //string [] radar_messages, radar_messages_index;             
        int wait_s=0;
        long refreshtime_ms=0;
        if (RADAR_REALISM >= 1) { wait_s=5; refreshtime_ms=60*1000; }
        if (RADAR_REALISM >= 5) { wait_s=20; refreshtime_ms=2*60*1000; }
        if (RADAR_REALISM >= 9) { wait_s=60; refreshtime_ms=5*60*1000; }
        
        enorfriend="ENEMY";
        if (inOwnArmy) enorfriend="FRIENDLY";         
                
        if (player!=null  && (player.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
          p = player.Place() as AiAircraft;
          player_Vwld=p.AirGroup().Vwld();
          player_vel_mps=Calcs.CalculatePointDistance(player_Vwld);
          player_vel_mph=Calcs.meterspsec2milesphour(player_vel_mps);
          player_sector=GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
          player_sector = player_sector.Replace("," , ""); // remove the comma
          player_place_set=true;
          playername=player.Name();
                                 
          /* posmessage = "Radar intercepts are based on your current speed/position: " +
                       player_vel_mph.ToString("F0") +"mph " + 
                       player_sector.ToString();
          gpLogServerAndLog(new Player[] { player }, posmessage, null);
          */

        } 
        playername_index=playername + "_0";
        if (inOwnArmy) playername_index=playername + "_1";
 
        savenewmessages=true; //save the messages that are generated
        currtime_ms=stopwatch.ElapsedMilliseconds;
        //If the person has requested a new radar return too soon, just repeat the old return verbatim
        //We have 3 cases:
        // #1. ok to give new radar return
        // #2. Too soon since last radar return to give a new one
        // #3. New radar return is underway but not finished, so don't give them a new one. 
        if (radar_messages_store.TryGetValue(playername_index, out message_data)) {
          long time_elapsed_ms = currtime_ms - message_data.Item1;
          long time_until_new_s = (long)((refreshtime_ms-time_elapsed_ms)/1000);
          long time_elapsed_s= (long)time_elapsed_ms/1000;
          radar_messages=message_data.Item2;
          if ( time_elapsed_ms < refreshtime_ms || message_data.Item1==-1) {
            if (message_data.Item1==-1) posmessage = "New radar returns are in process.  Your previous radar return:";
            else posmessage = time_until_new_s.ToString("F0")+ "s until " + playername + " can receive a new radar return.  Your previous radar return:";
            gpLogServerAndLog(new Player[] { player }, posmessage, null);
            
            wait_s=0;
            storedtime_ms=message_data.Item1;
            savenewmessages=false; //don't save the messages again because we aren't generating anything new
            
            //Wait just 2 seconds, which gives people a chance to see the message about how long until they can request a new radar return.
            Timeout (2, ()=>
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
          radar_messages_store[playername_index]=new Tuple<long, SortedDictionary<string,string>>(-1,radar_messages);
          
          GamePlay.gpLogServer(new Player[] { player }, "Fetching radar contacts, please stand by . . . ", null);
            
          
                
                
                radar_messages = new SortedDictionary<string, string>(new ReverseComparer<string>());//clear it out before starting anew . . .           
                radar_messages.Add ( "9999999999" , " >>> " + enorfriend + " RADAR CONTACTS <<< ");  
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
                                    poscount=airGroup.NOfAirc;
                                    foreach (AiActor actor in airGroup.GetItems())
                                    {
                                        if (actor is AiAircraft)
                                        {
                                            AiAircraft a = actor as AiAircraft;
                                            //if (!player_place_set &&  (a.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
                                            if (!player_place_set) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"                                                                        
                                              p = actor as AiAircraft;
                                              player_Vwld=p.AirGroup().Vwld();
                                              player_vel_mps=Calcs.CalculatePointDistance(player_Vwld);
                                              player_vel_mph=Calcs.meterspsec2milesphour(player_vel_mps);
                                              player_sector=GamePlay.gpSectorName(p.Pos().x, p.Pos().y).ToString();
                                              player_sector = player_sector.Replace("," , ""); // remove the comma
                                              player_place_set=true;                                    
                                            }
                                            
                                                 
                                              type=a.Type().ToString();
                                              if (type.Contains("Fighter") || type.Contains("fighter")) type = "F";
                                              else if (type.Contains("Bomber")  || type.Contains("bomber")) type = "B";
                                              if (a==p) type = "Your position";
                                              /* if (DEBUG) GamePlay.gpLogServer(new Player[] { player }, "DEBUG: Destroying: Airgroup: " + a.AirGroup() + " " 
                                               + a.CallSign() + " " 
                                               + a.Type() + " " 
                                               + a.TypedName() + " " 
                                               +  a.AirGroup().ID(), new object[] { });
                                              */
                                              pos1=a.Pos();
                                              //Thread.Sleep(100);
                                              //pos2=a.Pos();
                                              //bearing=Calcs.CalculateGradientAngle (pos1,pos2);
                                              Vwld=airGroup.Vwld();
                                              vel_mps=Calcs.CalculatePointDistance(Vwld);
                                              vel_mph=Calcs.meterspsec2milesphour(vel_mps);
                                              vel_mph_10=Calcs.RoundInterval(vel_mph, 10);
                                              heading=(Calcs.CalculateBearingDegree(Vwld));
                                              heading_10=Calcs.GetDegreesIn10Step (heading);
                                              dis=Calcs.meters2miles(Calcs.CalculatePointDistance (a.Pos(),p.Pos()));
                                              dis_10=(int)dis;
                                              if (dis>20) dis_10=Calcs.RoundInterval(dis,10);
                                              bearing=Calcs.CalculateGradientAngle (p.Pos(),a.Pos());                             
                                              bearing_10= Calcs.GetDegreesIn10Step(bearing);
                                              
                                              alt=Calcs.meters2feet(a.Pos().z);
                                              alt_angels=Calcs.Feet2Angels(alt);
                                              sector=GamePlay.gpSectorName(a.Pos().x, a.Pos().y).ToString();
                                              sector = sector.Replace("," , ""); // remove the comma
                                              VwldP = new Point3d (Vwld.x, Vwld.y, Vwld.z);                                      
                                              
                                              intcpt= Calcs.calculateInterceptionPoint(a.Pos(), VwldP, p.Pos(), player_vel_mps);
                                              intcpt_heading=(Calcs.CalculateGradientAngle(p.Pos(),intcpt));
                                              intcpt_time_min=intcpt.z/60;
                                              intcpt_sector=GamePlay.gpSectorName(intcpt.x, intcpt.y).ToString();
                                              intcpt_sector = intcpt_sector.Replace("," , ""); // remove the comma
                                              intcpt_reasonable_time = (intcpt_time_min >= 0.02 && intcpt_time_min <20);
                                              
                                              if (RADAR_REALISM==0) { 
                                                posmessage = type + " " +
                                                  
                                                  dis.ToString("F0") + "mi" +
                                                  bearing.ToString("F0") + "dg" +
                                                  alt.ToString ("F0") + "ft " + 
                                                  vel_mph.ToString("F0") +"mph" +
                                                  heading.ToString("F0") + "dg " +
                                                  sector.ToString() + " " + 
                                                  Calcs.GetAircraftType (a);
                                                if (intcpt_time_min > 0.02 )
                                                   posmessage += 
                                                      " Intcpt: " +
                                                      intcpt_heading.ToString("F0") + "dg" +
                                                      intcpt_time_min.ToString("F0") + "min " +
                                                      intcpt_sector + " " +
                                                      intcpt.x.ToString("F0") + " " + intcpt.y.ToString("F0") ;
                                                  
                                                  /* "(" + 
                                                  Calcs.meters2miles(a.Pos().x).ToString ("F0") + ", " +
                                                  Calcs.meters2miles(a.Pos().y).ToString ("F0") + ")";
                                                  */             
                                                  //GamePlay.gpLogServer(new Player[] { player }, posmessage, new object[] { });
                                             } else if (RADAR_REALISM > 0 ) {
                                                
                                                //Trying to give at least some semblance of reality based on capabilities of Chain Home & Chain Home Low
                                                //https://en.wikipedia.org/wiki/Chain_Home
                                                //https://en.wikipedia.org/wiki/Chain_Home_Low
                                                if (random.Next(8)==1){ //oops, sometimes we get mixed up on the type.  So sad . . .
                                                   type="F";
                                                   if (random.Next(3)==1) type="B";
                                                }
                                                if (dis <= 2 && a != p ){ posmessage = type + " nearby"; }
          
                                                //Below conditions are situations where radar doesn't work/fails, working to integrate realistic conditions for radar
                                                //To do this in full realism we'd need the full locations of Chain Home & Chain Home Low stations & exact capabilities
                                                //As an approximation we're using distance from the current aircraft, altitude, etc.
                                                else if ((dis >= 50 && poscount <8 && random.Next(15) > 1 && !intcpt_reasonable_time ) ||  //don't show enemy groups too far away, unless they are quite large, or can be intercepted in reasonable time.  Except once in a while randomly show one.
                                                         (dis >= 25 && poscount <4 && random.Next(12) > 1 && !intcpt_reasonable_time ) || 
                                                         (dis >= 15 && poscount <=2 && random.Next(8) > 1 && !intcpt_reasonable_time ) || 
                                                         (dis >=70 && alt < 4500 ) || //chain home only worked above ~4500 ft & Chain Home Low had effective distance only 35 miles
                                                         //however, to implement this we really need the distance of the target from the CHL stations, not the current aircraft
                                                         //We'll approximate it by eliminating low contacts > 70 miles away from current a/c
                                                         ( dis >= 10 && alt < 250 && alt < random.Next(250)) || //low contacts become less likely to be seen the lower they go.  Chain Low could detect only to about 4500 ft, though that improved as a/c came closer to the radar facility.
                                                         //but Chain Home Low detected targets well down to 500 feet quite early in WWII and after improvements, down to 50 feet.  We'll approximate this by
                                                         //phasing out targets below 250 feet.
                                                         ( dis < 10 && alt < 65 && alt < random.Next(150)) || //Within 10 miles though you really have to be right on the deck before the radar fails. Approximating 50 foot alt lower limit.
                                                         ( poscount <=2 && random.Next(5) == 1 ) || // Small groups have a higher chance of being overlooked/dropping out
                                                         ( random.Next(7)==1     )  //it just malfunctions & shows nothing 1/7 of the time, for no reason, because.
                                                         ) { posmessage="";} 
                                                else {
                                                  posmessage = type + " " +
                                                  
                                                  dis_10.ToString("F0") + "mi" +
                                                  bearing_10.ToString("F0") + "dgA" +
                                                  alt_angels.ToString ("F0") + " " + 
                                                  vel_mph_10.ToString("F0") +"mph" + 
                                                  heading_10.ToString("F0") + "dg " +
                                                  sector.ToString();
                                                if (intcpt_time_min >= 0.02 )
                                                   posmessage += 
                                                      " Intcpt: " + 
                                                      intcpt_heading.ToString("F0") + "dg" +
                                                      intcpt_time_min.ToString("F0") + "min " +
                                                      intcpt_sector + " ";
                                                  
                                               }   
                                             
                                             }                         
                                                
                                              
                                                  
                                            
                                            //poscount+=1;
                                            break; //only get 1st a/c in each group, to save time/processing
                                            
            
                                        }
                                    }
                                    //We'll print only one message per Airgroup, to reduce clutter
                                    //GamePlay.gpLogServer(new Player[] { player }, "RPT: " + posmessage + posmessage.Length.ToString(), new object[] { });
                                    if (posmessage.Length>0) {                                
                                       //gpLogServerAndLog(new Player[] { player }, "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + "" + posmessage, null);
                                       //We add the message to the list along with an index that will allow us to reverse sort them in a logical/useful order                               
                                       int intcpt_time_index=(int)intcpt_time_min;
                                       if (intcpt_time_min<=0 || intcpt_time_min>99) intcpt_time_index=99;      
                                       
                                       try {
                                       radar_messages.Add ( 
                                          ((int)intcpt_time_index).ToString("D2") + ((int)dis).ToString("D3") + aigroup_count.ToString("D5"), //adding aigroup ensure uniqueness of index
                                          "~" + Calcs.NoOfAircraft(poscount).ToString("F0") + posmessage                                  
                                       );
                                       }
                                       catch (Exception e){
                                         GamePlay.gpLogServer(new Player[] { player }, "Error: " + e, new object[] { });
                                       }
                                                                                                        
                                    
                                    }  
                                    
                                }
                            }
                        }
                    }
                            
               }
               //There is always one message - the header.  
               if (radar_messages.Count==1) radar_messages.Add ( "0000000000" , "<NO TRADE>");                                                                   
             
             Timeout(wait_s, () => {
               //print out the radar contacts in reverse sort order, which puts closest distance/intercept @ end of the list               
               foreach (var mess in radar_messages) {
                 if (RADAR_REALISM==0) gpLogServerAndLog(new Player[] { player }, mess.Value + " : " + mess.Key, null);
                 else gpLogServerAndLog(new Player[] { player }, mess.Value, null);              
                  
               }
               radar_messages_store[playername_index]=new Tuple<long, SortedDictionary<string,string>>(currtime_ms,radar_messages); 
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

        ReadInitialSubmissions(MISSION_ID + "-auto-generate-vehicles", 0, 0); //want to load this after airports are loaded
        ReadInitialSubmissions(MISSION_ID + "-stats", 10, 1);
        ReadInitialSubmissions(MISSION_ID + "-initsubmission", 60, 10);        
        
        //ReadInitialSubmissions(MISSION_ID + "-hud", 0);
        //Timeout(5, () => { statsMission.statsStart(); });                       
                         
        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
        }
        
    }

  public override void OnBattleStoped()
    {
        base.OnBattleStoped();
    
        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
    }
    
    
    public void ReadInitialSubmissions(string filenameID, int timespread=60, int wait=0)
    {
        List<string> InitSubMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH, filenameID); // gets .mis files with with word filenameID in them
        //string[] InitSubMissions = GetFilenamesFromDirectory(CLOD_PATH + FILE_PATH, filenameID); // gets .mis files with with word filenameID in them
        //string[] array = Directory.GetFiles(FILE_PATH + @"Airfields\");
        
        DebugAndLog ("Debug: Loading " + InitSubMissions.Count + " missions to load. " + filenameID + " " + CLOD_PATH + FILE_PATH);        
        foreach (string s in InitSubMissions)
        {
            //Distribute loading of initial sub-missions over the first timespread seconds
            //If you make each sub-mission small enough it will load without a noticeable stutter
            //If they are large & make a noticeable stutter, probably best to load them all very near the beginning of the mission
            //so that all the stutters happen at that point
            if ((timespread==0) && (wait==0)) {
                GamePlay.gpPostMissionLoad(s);
                DebugAndLog( s + " file Loaded");          
            } else { 
              Timeout(wait + random.Next(timespread), () => {
              
                //string temp = @"missions\AirfieldSpawnTest\Airfields\" + Path.GetFileName(s);
    
                   GamePlay.gpPostMissionLoad(s);
                
                //string s2=@"C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\missions\Multi\Fatal\TWC-initsubmission-stationary.mis";
                //GamePlay.gpPostMissionLoad(s2);
                //if (DEBUG)
                //{
                    DebugAndLog( s + " file Loaded");
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
   private void setMainMenu( Player player ) {
    GamePlay.gpSetOrderMissionMenu( player, true, 0, new string[] { "Server Options - Users" }, new bool[] { true } );
  }

  private void setSubMenu1( Player player ) {
    if ( player.Name().Substring(0,4) == @"TWC_") {
    
      string rollovertext="(admin) End mission now/roll over to next mission";
      if (EndMissionSelected) rollovertext="(admin) CANCEL End mission now command";
      GamePlay.gpSetOrderMissionMenu( player, true, 1, new string[] { "Enemy radar", "Friendly radar", "Time left in mission", rollovertext, "(admin) Show detailed damage reports for all players (toggle)","(admin) Toggle debug mode", "Your stats"}, new bool[] { false, false, false, false, false, false, false} );
   } else {
     GamePlay.gpSetOrderMissionMenu( player, true, 1, new string[] { "Enemy radar", "Friendly radar", "Time left in mission"}, new bool[] { false, false, false} );
   
   }   
  }

  private void setSubMenu2( Player player ) {
        //GamePlay.gpSetOrderMissionMenu( player, true, 2, new string[] { "Spawn New AI Groups Now", "Dogfight mode: Remove AI Aircraft, stop new spawns (30 minutes)", "Delete all current AI Aircraft", "Show damage reports for all players", "Stop showing damage reports for all players"}, new bool[] { false, false, false, false, false } );
  }  
  
  //object plnameo= GamePlay.gpPlayer().Name();  
  //string plname= GamePlay.gpPlayer().Name() as string;
  public override void OnOrderMissionMenuSelected( Player player, int ID, int menuItemIndex ) {
    //base.OnOrderMissionMenuSelected(player, ID, menuItemIndex); //2015/05/16 - not sure why this was missing previously? We'll see . . .
    
    //main menu////////////////
    if( ID == 0 ) { // main menu
      if( menuItemIndex == 1 ) {
          setSubMenu1( player );
      } else if ( menuItemIndex == 0 ) {  
          setMainMenu( player );   
      } else { 
          setMainMenu( player );
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
      
    //1st submenu////////////////
    } else if( ID == 1 ) { // sub menu
    
    if (menuItemIndex == 0 ) {  
          setSubMenu1( player );
          setMainMenu( player );   
    } else if (menuItemIndex == 1)
	  {
        Player[] all = { player };
        listPositionAllAircraft (player, player.Army(),false); //enemy a/c  
        if (DEBUG) { 
          DebugAndLog ( "Total number of AI aircraft groups currently active:");
          if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
          {
              
                  int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                  DebugAndLog ( totalAircraft.ToString());
                  //GamePlay.gpLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
          }
        }   
        setMainMenu( player );      
    } else if (menuItemIndex == 2)
	  {
        Player[] all = { player };        
        listPositionAllAircraft (player, player.Army(),true); //friendly a/c           
        if (DEBUG) { 
          DebugAndLog ("Total number of AI aircraft groups currently active:");
          if (GamePlay.gpAirGroups(1) != null && GamePlay.gpAirGroups(2) != null)
          {
              
                  int totalAircraft = GamePlay.gpAirGroups(1).Length + GamePlay.gpAirGroups(2).Length;
                  DebugAndLog ( totalAircraft.ToString());
                  //GamePlay.gpLogServer(GamePlay.gpRemotePlayers(), totalAircraft.ToString(), null);
          }
        }   
        setMainMenu( player );  
      //TIME REMAINING ETC//////////////////////////////////  
      } else if (menuItemIndex == 3)
	    {
         //int endsessiontick = Convert.ToInt32(ticksperminute*60*HOURS_PER_SESSION); //When to end/restart server session
        showTimeLeft( player );
   
        setMainMenu( player );      
      }
            
      //immediate end of mission///////////////
      else if (menuItemIndex == 4)
      {
           if ( player.Name().Substring(0,4) == @"TWC_") {
            if (EndMissionSelected==false) {
                EndMissionSelected=true;
                GamePlay.gpLogServer(new Player[] { player }, "ENDING MISSION!! If you want to cancel the End Mission command, use Tab-4-1 again.  You have 30 seconds to cancel.", new object[] { });
                Timeout (30, () => {
                  if (EndMissionSelected) {
                    EndMission(0);
                  } else {
                    GamePlay.gpLogServer(new Player[] { player }, "End Mission CANCELLED; Mission continuing . . . ", new object[] { });
                    GamePlay.gpLogServer(new Player[] { player }, "If you want to end the mission, you can use the menu to select Mission End again now.", new object[] { });
                  }
                  
                });
                
            } else {
               GamePlay.gpLogServer(new Player[] { player }, "End Mission CANCELLED; Mission will continue", new object[] { });          
               EndMissionSelected=false;
               
            }                
           } 
           setMainMenu( player );
      }

   

   //start/stop display a/c damage inflicted info/////////////////////////// 
   else if (menuItemIndex == 5)
      {
         if ( player.Name().Substring(0,4) == @"TWC_") {
			      dmgOn = !dmgOn;
            if (dmgOn) {
               GamePlay.gpHUDLogCenter("Will show damage on all aircraft");
               GamePlay.gpLogServer(new Player[] { player }, "Detailed damage reports will be shown for all players", new object[] { });
               
            } else {
               GamePlay.gpHUDLogCenter("Will not show damage on all aircraft");
               GamePlay.gpLogServer(new Player[] { player }, "Detailed damage reports turned off", new object[] { });
            }
         }       
         setMainMenu( player );	          
      }
   else if (menuItemIndex == 6)
      {
         if ( player.Name().Substring(0,4) == @"TWC_") {
			      debugMenu = !debugMenu;
            if (debugMenu) {              
               GamePlay.gpLogServer(new Player[] { player }, "Debug & detailed radar ON for all users - extra debug messages & instant, detailed radar", new object[] { });
               radar_realismSave=RADAR_REALISM;
               DEBUG=true;
               RADAR_REALISM=0;
               
            } else {               
               GamePlay.gpLogServer(new Player[] { player }, "Debug & detailed radar OFF", new object[] { });
               RADAR_REALISM=radar_realismSave;
               DEBUG=false;               
               
            }
         }
                
         setMainMenu( player );	          
      }
      
   //Display Stats
   //WritePlayerStat(player)
   else if (menuItemIndex == 7)
      {

           string str = WritePlayerStat(player);        
           //split msg into a few chunks as gplogserver doesn't like long msgs
           int maxChunkSize=100;
           for (int i = 0; i < str.Length; i += maxChunkSize)             
              GamePlay.gpLogServer(new Player[] { player }, str.Substring(i, Math.Min(maxChunkSize, str.Length-i)), new object[] { });       
       
            
           setMainMenu( player );	          
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
         setMainMenu( player );	
      } else { //make sure there is a catch-all ELSE or ELSE menu screw-ups WILL occur
        setMainMenu( player );
      }       
  
    } //menu if   
  } // method

  //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
  public override void OnPlayerConnected( Player player ) {
    string message;
    if (!MISSION_STARTED) DebugAndLog("First player connected; Mission timer starting");
    MISSION_STARTED = true;
    if( MissionNumber > -1 ) {
      setMainMenu( player );
      
      GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
      //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
      
      DateTime utcDate = DateTime.UtcNow;
      
      //utcDate.ToString(culture), utcDate.Kind
      //Write current time in UTC, what happened, player name
      message = utcDate.ToString("u") + " Connected " + player.Name() ; 
      
      DebugAndLog ( message );
    }
  }

  //INITIATING THE MENUS FOR THE PLAYER AT VARIOUS KEY POINTS
  public override void OnPlayerDisconnected( Player player, string diagnostic ) {
    string message;
    if( MissionNumber > -1 ) {
      
      DateTime utcDate = DateTime.UtcNow;
      
      //utcDate.ToString(culture), utcDate.Kind
      //Write current time in UTC, what happened, player name
      message = utcDate.ToString("u") + " Disconnected " + player.Name() + " " + diagnostic ; 
      DebugAndLog (message);      
    }
  }
  
  public override void OnPlayerArmy( Player player, int Army   ) {
    if( MissionNumber > -1 ) {
        /* AiAircraft aircraft = (player.Place() as AiAircraft);
                        string cs = aircraft.CallSign();
                        //int p = part.ParameterTypes.I_VelocityIAS; 
                        double ias = (double) aircraft.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
                        GamePlay.gpLogServer(new Player[] { player }, "Plane: "  
                        + cs + " " + ias, new object[] { });
        */                  
        //We re-init menu & mission_started here bec. in some situations OnPlayerConnected never happens.  But, they
        //always must choose their army before entering the map, so this catches all players before entering the actual gameplay
        setMainMenu( player );
        if (!MISSION_STARTED) DebugAndLog("First player connected (OnPlayerArmy); Mission timer starting");
        MISSION_STARTED = true;
        GamePlay.gpLogServer(new Player[] { player }, "Welcome " + player.Name(), new object[] { });
      //GamePlay.gpLogServer(null, "Mission loaded.", new object[] { });
    }
  }
  public override void Inited() {
    if( MissionNumber > -1 ) {
      
      setMainMenu(GamePlay.gpPlayer());
      GamePlay.gpLogServer(null, "Welcome " + GamePlay.gpPlayer().Name(), new object[] { });
      
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
    	
    	if (DamageFrom.Player != null )
    	{
			if (dmgOn == true)
            {
    	       GamePlay.gpLogServer (new Player[] { DamageFrom.Player }, "{0} hits {1} : {2} \n", new object [] 
{DamageFrom.Player, shortName, WhatDamaged});
            }    
      }
    	
    }

    public void showTimeLeft(Player player ){
         int tickSinceStarted = Time.tickCounter() - START_MISSION_TICK;
         int respawntick=respawnminutes*ticksperminute;
         int timespenttick = (tickSinceStarted - tickoffset) % respawntick;
         int timelefttick = respawntick - timespenttick;
         int timespentminutes=Convert.ToInt32(((double)timespenttick/(double)ticksperminute));
         int timeleftminutes=Convert.ToInt32(((double)timelefttick/(double)ticksperminute));
         int missiontimeleftminutes = Convert.ToInt32((double)(END_MISSION_TICK-tickSinceStarted) /(double)ticksperminute);
         string msg = "Time left in mission " + MISSION_ID + ": " + missiontimeleftminutes.ToString() + " min.";
         if (!MISSION_STARTED) msg = "Mission not yet started - waiting for first player to enter.";
                  
         GamePlay.gpLogServer(new Player[] { player }, msg , new object[] { });
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
            GamePlay.gpLogServer(new Player[] { player }, "Please read the mission briefings (on the initial 'Flag Page') for detailed info about mission objectives.", new object[] { });
        }
        else if (msg.StartsWith("<stat"))
        {

            string str = WritePlayerStat(player);
            //split msg into a few chunks as gplogserver doesn't like long msgs
            int maxChunkSize = 100;
            for (int i = 0; i < str.Length; i += maxChunkSize)
                GamePlay.gpLogServer(new Player[] { player }, str.Substring(i, Math.Min(maxChunkSize, str.Length - i)), new object[] { });


        }
        else if (msg.StartsWith("<pos") && player.Name().Substring(0, 4) == @"TWC_")
        {
            int saveRealism = RADAR_REALISM; //save the accurate radar contact lists
            RADAR_REALISM = 0;
            listPositionAllAircraft(player, 1, true);
            listPositionAllAircraft(player, 1, false);
            RADAR_REALISM = saveRealism;

        }
        else if (msg.StartsWith("<debugon") && player.Name().Substring(0, 4) == @"TWC_")
        {

            DEBUG = true;
            GamePlay.gpLogServer(new Player[] { player }, "Debug is on", new object[] { });

        }
        else if (msg.StartsWith("<debugoff") && player.Name().Substring(0, 4) == @"TWC_")
        {

            DEBUG = false;
            GamePlay.gpLogServer(new Player[] { player }, "Debug is off", new object[] { });

        }
        else if (msg.StartsWith("<logon") && player.Name().Substring(0, 4) == @"TWC_")
        {

            LOG = true;
            GamePlay.gpLogServer(new Player[] { player }, "Log is on", new object[] { });

        }
        else if (msg.StartsWith("<logoff") && player.Name().Substring(0, 4) == @"TWC_")
        {

            LOG = false;
            GamePlay.gpLogServer(new Player[] { player }, "Log is off", new object[] { });

        }
        else if (msg.StartsWith("<des") && player.Name().Substring(0, 4) == @"TWC_")
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
        else if (msg.StartsWith("<plv") && player.Name().Substring(0, 4) == @"TWC_")
        {
            //Not really sure how this works, but this is a good guess.  
            if (player.PersonPrimary() != null) player.PlaceLeave(player.PersonPrimary().Place());
            if (player.PersonSecondary() != null) player.PlaceLeave(player.PersonSecondary().Place());
        }
        else if (msg.StartsWith("<mov") && player.Name().Substring(0, 4) == @"TWC_")
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
        else if (msg.StartsWith("<admin") && player.Name().Substring(0, 4) == @"TWC_")
        {

            GamePlay.gpLogServer(new Player[] { player }, "Admin commands: <pos (all a/c position) <debugon <debugoff <logon <logoff (turn debug/log on/off) <des (destroy an object by name)", new object[] { });


        }
        else if (msg.StartsWith("<help") || msg.StartsWith("<"))
        {
            GamePlay.gpLogServer(new Player[] { player }, "Commands: <tl Time Left; <rr How to reload; <stat Some helpful stats", new object[] { });
            //GamePlay.gp(, from);
        }
    }  

    //Ground objects (except AA Guns) will die after 55 min when counted from their birth

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);
        //Ground objects (except AA Guns) will die after X min when counted from their birth
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


} //class mission : amission

//Various helpful calculations & formulas
public static class Calcs
{

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
            return (int)firstDecimal;
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
