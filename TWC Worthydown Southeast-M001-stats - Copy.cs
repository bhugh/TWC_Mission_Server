//TODO:
// - Create separate page for dead pilots
// - Remove dead pilots & possibly live pilots after a certain time period
// - Make sure good landings link to create a mission if takeoff from same location
// - Fix onplaceleave issue where opponents may get a kill score when the a/c has actually landed just fine but CloD doesn't register a landing
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using part;


public class Mission : AMission
{
    #region Customizable Variables
    //Log Related
    public bool stb_LogErrors = true;//creates a local file, errors in txt format, useful for IO or upload errors
    public bool stb_LogStats = true;//creates a local file, full stats in txt format
    public bool stb_LogStatsCreateHtmlLow = true;//creates a local file, summerized stats in html format
    public bool stb_LogStatsCreateHtmlMed = false;//not available yet
    public bool stb_LogStatsUploadHtmlLow = true;//set true to upload
    //Upload sorttable.js and Style.css to the same location on your site manually, created htm depends on them to be viewed properly
    public bool stb_LogStatsUploadHtmlMed = false;//not available yet
    public string stb_LogStatsUploadAddressLow = "ftp://ftp.brenthugh.com/brenthugh.com/twc/stats.htm";
    public bool stb_AnnounceStatsMessages = true;
    public int stb_AnnounceStatsMessagesFrequency=29*60; //seconds
    public string stb_LogStatsPublicAddressLow = "BrentHugh.com/twc/stats.htm";
    public string stb_LogStatsUploadAddressMed = "";//not available yet
    public string stb_LogStatsUploadUserName = "mobikefed";
    public string stb_LogStatsUploadPassword = "1Spoke$#$";
    //public double stb_LogStatsDelay = 120.0;//seconds 60.0 default
    public double stb_LogStatsDelay = 10.0;//for testing
    //Mission Related
    public bool stb_DestroyOnPlaceLeave = true; //This helps stats by eliminating the problem with people getting credit for a kill when actually the plane just landed.  However, works best if values af1, af2, af3, af4 are set for airfield locations, as below.  I think it will still help regardless, so leaving it on for now.  TODO: Get a list of actual airports in the mission, not just using the af1, af2, etc.
    public bool stb_SpawnAntiAir = false;//base AAguns and defense ships
    public double stb_SpawnAntiAirDelay = 444.0;//444.0 default, some AA guns stop firing(probably run out of ammo) in less than 250 secs
    public bool stb_SpawnFrontline1 = false;//continuous ships @ 1.path (left)
    public bool stb_SpawnFrontline2 = false;//continuous ships @ 2.path (left-center)
    public bool stb_SpawnFrontline3 = false;//continuous ships @ 3.path (right-center)
    public bool stb_SpawnFrontline4 = false;//continuous armors @ 4.path (right)
    public bool stb_SpawnBombers = false;//loads 2 bomber missions, turnbased
    //Balance Related
    public bool stb_FighterBalanceActive = false;//false to disable balance code for fighters
    public bool stb_BalanceFightersByRatio = false;//true for balance by ratio, false for balance by fighter count difference
    public double stb_DesiredRatio = 2.0;//max delta fighter ratio for balance, default 2.0
    public int stb_DesiredDelta = 5;//max delta fighter count for balance, default 5
    public int stb_MinCountForBalance = 10;//min fighter count for balance (for both ratio and delta methods), default 10
    public string stb_BalanceMsgRus = "Извините, Есть слишком много истребителей прямо сейчас." +
                                       " Выберите бомбардировщиков или Изменение вашей армии или Попробуйте еще ​​раз позже.";
    public string stb_BalanceMsgEng = "Sorry, There are too many fighters right now." +
                                       " Choose bombers or change your army or try again later.";
    public string stb_BalanceMsgHud = "Aircraft Disabled: There are too many fighters right now!";
    #endregion

    #region stb Core
    //these variables change in runtime, do not alter their default values
    private Point3d af1 = new Point3d(13800.0, 30890.0, 1.0);
    private Point3d af2 = new Point3d(18180.0, 30760.0, 1.0);
    private Point3d af3 = new Point3d(12180.0, 10520.0, 1.0);
    private Point3d af4 = new Point3d(18830.0, 9810.0, 1.0);
    private bool stb_BomberMissionTurn = true;
    private int stb_RedFighters = 0;
    private int stb_BlueFighters = 0;
    private double stb_Ratio = 0d;
    private double stb_Delta = 0d;
    private string stb_AppPath = (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
    private string stb_FullPath = "";//will be created in runtime
    private string stb_ErrorLogPath = @"TWC Worthydown Southeast-M001_errorlog.txt";//will be combined with fullpath in runtime
    private string stb_StatsPathTxt = @"TWC Worthydown Southeast-M001_playerstats_full.txt";//"
    private string stb_StatsPathHtmlLow = @"TWC Worthydown Southeast-M001_playerstats_low.htm";//"
    private string stb_StatsPathHtmlMed = @"TWC Worthydown Southeast-M001_playerstats_med.htm";//"
    private ISectionFile stb_BaseAntiAirDefenses;
    private ISectionFile stb_Frontline1;
    private ISectionFile stb_Frontline2;
    private ISectionFile stb_Frontline3;
    private ISectionFile stb_Frontline4;
    private ISectionFile stb_Bombers1;
    private ISectionFile stb_Bombers2;
    private NumberFormatInfo stb_nf = new NumberFormatInfo();
    private int stb_MissionsCount = 1;
    
    public struct StbContinueMission
    {
        public Point3d placeLeaveLoc;
        public bool alive;
        public bool mayContinue;
        
        public StbContinueMission (Point3d plc, bool al=true, bool mc=true)
        {
            placeLeaveLoc = plc;
            alive=al;
            mayContinue=mc;
        }
        
        public string ToString (string frmt)
        {
            return "(" + placeLeaveLoc.x.ToString(frmt) + ", " 
               + placeLeaveLoc.y.ToString(frmt) + ", " 
               + placeLeaveLoc.z.ToString(frmt) + ") "
               + "Alive: " + alive.ToString() 
               + " MC: " + mayContinue.ToString();                                    
        }
    }

    public StbContinueMissionRecorder stb_ContinueMissionRecorder;
    
    public class StbContinueMissionRecorder
    {
        //private readonly Mission outer; //allows us to reference methods etc from the Mission class as 'outer'
        public Dictionary<string, StbContinueMission> stbCmr_ContinueMissionInfo;
        bool stbCmr_LogErrors;
        string stbCmr_ErrorLogPath;

        public StbContinueMissionRecorder (bool le, string elp) {
        
          stbCmr_ContinueMissionInfo = new Dictionary<string, StbContinueMission>();        
          stbCmr_LogErrors = le;
          stbCmr_ErrorLogPath = elp;
        
        }
        
        public void StbCmr_SavePositionEnter (Player player, AiActor actor) 
        {   
          try 
          {
            if (player is Player && actor is AiActor && player!=null && actor != null && player.Name() != null ) {
            
              StbContinueMission cm = new StbContinueMission ();              
              if ( stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm))
              { //Ok the person has newly spawned in and was previously flying, we check to see if 1. the person has died since last takeoff and 2. mayContinue is true (mayContinue is a signal from the previous event, the positionLeave in this case, that it was Ok to continue the mission @ that point). 3. Is taking off from the same place they recently landed.  If haven't died, mayContinue is true & taking off from same place, within 2000 of the place where they left their previous plane/position, they can continue the mission.
                                
                  double dis_meters = Calcs.CalculatePointDistance (cm.placeLeaveLoc, actor.Pos());
                  bool maycont=false;
                  if (cm.mayContinue && cm.alive && ( dis_meters < 2000) ) maycont=true;
                  
                  cm.mayContinue=maycont;                   
              
              } else { //newly entered player; this can't be a continuation
                  //if the player doesn't yet exist, we initialize it
                  //with start position; so far the player is alive & bu may not continue (ie, the current flight is not a continuation of a previous successful flight)
                  //p = player.Place() as AiAircraft;          
                  //p.Pos().x, p.Pos().y)
                  cm.placeLeaveLoc.x=-1000000;
                  cm.placeLeaveLoc.y=-1000000;
                  cm.alive=true;
                  cm.mayContinue=false;
                  
              }                                                                                      
              stbCmr_ContinueMissionInfo[player.Name()]= cm;
              Console.WriteLine( "PosEnter: " + player.Name() + " " + cm.ToString("F0"));
            }                          
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        } 
        
        public void StbCmr_SavePositionLeave (Player player, AiActor actor) 
        {   
          try 
          {
            if (player!=null && actor != null && player.Name() != null ) {
            
              StbContinueMission cm = new StbContinueMission ();
              bool ret; 
              ret = stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm);
                          
              //p = player.Place() as AiAircraft;          
              //p.Pos().x, p.Pos().y)
              //Save the position where they left the a/c.  If the player is alive, the player may continue the mission, if they (can) take off from near the same location again.
              cm.placeLeaveLoc.x=actor.Pos().x;
              cm.placeLeaveLoc.y=actor.Pos().y;
              bool actorAlive=actor.IsAlive();
              if (cm.alive && actorAlive) cm.mayContinue=true;                 
            
              stbCmr_ContinueMissionInfo[player.Name()]= cm;
              Console.WriteLine( "PosLeave: " + player.Name() + " " + cm.ToString("F0"));
            }                          
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        } 
        
        //OK, this one isn't really necessary/somewhat redundant, but if
        //LeavePosition somehow doesn't trigger, this will catch any crash landings
        //(ie, off of an official airport) and flag them as not continue
        public void StbCmr_SaveOnCrashLanded (AiAircraft a) 
        {   
          try 
          {  
            if (! ( a is AiAircraft)) return;
            //Do this for every place/position in the a/c that took off
            for (int i = 0; i < a.Places(); i++)
            {
              //if (aiAircraft.Player(i) != null) return false;
              //OK, we go through each individual player in the a/c and decide
              //individually whether this is a mission continuation or not
              if ( a.Player(i) is Player && a.Player(i) != null && a.Player(i).Name() != null ) 
              {
                  
                  Player player = a.Player(i);
                  StbContinueMission cm = new StbContinueMission ();
                  bool ret; 
                  ret = stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm);
                              
                  //p = player.Place() as AiAircraft;          
                  //p.Pos().x, p.Pos().y)
                  //Save the position where they left the a/c.  If the player is alive, the player may continue the mission, if they (can) take off from near the same location again.
                  AiActor actor=a as AiActor;
                  cm.placeLeaveLoc.x=-1000000;
                  cm.placeLeaveLoc.y=-1000000;
                  cm.alive=actor.IsAlive();
                  cm.mayContinue=false;                 
                
                  stbCmr_ContinueMissionInfo[player.Name()]= cm;
                  Console.WriteLine( "CrashLand: " + player.Name() + " " + cm.ToString("F0"));
              }                          
            }
          }  
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        }
        
        public void StbCmr_SavePositionDied (string playerName) 
        {   
          try 
          {
            if ( playerName != null ) {
            
              StbContinueMission cm = new StbContinueMission ();
              bool ret; 
              ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
                          
              //p = player.Place() as AiAircraft;          
              //p.Pos().x, p.Pos().y)
              //When the player dies they cannot continue the mission any more
              //Setting alive=false is the key value here
              cm.placeLeaveLoc.x=-1000000;
              cm.placeLeaveLoc.y=-1000000;
              cm.alive=false;
              cm.mayContinue=false;                 
            
              stbCmr_ContinueMissionInfo[playerName]= cm;
              
              Console.WriteLine( "Died: " + playerName + " " + cm.ToString("F0"));
            }                          
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        }
        
        public bool StbCmr_OnTookOff (string playerName) 
        {   
          try 
          {
                bool continueMission=true;                                                                
                StbContinueMission cm = new StbContinueMission ();
                cm.alive=false;
                cm.mayContinue=false;
                
                bool ret; 
                ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
                            
                //If the player died since last take off OR doesn't have 
                //'maycontinue' permission from the last placeEnter
                //then this is a New Mission & we increment the new mission counter
                //Otherwise, this is a continuation of a mission & we skip incrementing the mission counter            
                if ( ! cm.alive || !cm.mayContinue) {
                   //StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 779 });
                   //outer.stb_StatRecorder.StbSr_EnqueueTask(sst1);
                   continueMission=false;                 
                }
                                
                //This is the moment we reset alive & may continue 
                //ie, successful takeoff is where the player actually
                //becomes 'alive' for stats & continueMission purposes
                cm.placeLeaveLoc.x=-1000000;
                cm.placeLeaveLoc.y=-1000000;
                cm.alive=true;
                cm.mayContinue=true;                 
              
                stbCmr_ContinueMissionInfo[playerName]= cm;
                
                Console.WriteLine( "TookOff: " + playerName 
                  + " Mission Continuing: " 
                  + continueMission.ToString() + " " + cm.ToString("F0"));                                            
                
                return continueMission;                                        
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); return false;}
        } //method
        
        public void StbCmr_PrepareErrorMessage(Exception ex)
        {
            if (stbCmr_LogErrors)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(StbCmr_LogError),
                    (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
            }
        }
        
        public void StbCmr_LogError(object data)
        {
            try
            {
                FileInfo fi = new FileInfo(stbCmr_ErrorLogPath);
                StreamWriter sw;
                if (fi.Exists) { sw = new StreamWriter(stbCmr_ErrorLogPath, true, System.Text.Encoding.UTF8); }
                else { sw = new StreamWriter(stbCmr_ErrorLogPath, false, System.Text.Encoding.UTF8); }
                sw.WriteLine((string)data);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); };
        }
    } //class    

    public enum StbStatCommands : int { None = 0, Damage = 1, Dead = 2, CutLimb = 3, TaskCurrent = 4, Save = 5, PlayerKilled = 6, Mission = 7 };

    public struct StbStatTask
    {
        public StbStatCommands command;
        public string player;
        public int[] parameters;
        public StbStatTask(StbStatCommands cmd, string pname, int[] prms)
        {
            command = cmd;
            player = pname;
            parameters = prms;
        }
    }
        
    //for use in cases where you know the aiaircraft but NOT the playername
    //if the a/c has actual players they will get the stats.  Otherwise, nothing
    //happens
    public void StbStatTaskAircraft (StbStatCommands cmd, AiAircraft a, int [] prms) {
         Console.WriteLine ("Starting Aircraft Stat Update for " + a.Type());
         for (int i = 0; i < a.Places(); i++)
            {
              //if (aiAircraft.Player(i) != null) return false;
              if ( a.Player(i) is Player && a.Player(i) != null && a.Player(i).Name() != null ) {
                string playerName=a.Player(i).Name();                  
                Console.WriteLine ("Aircraft Stat Update for " + playerName);                                 
                StbStatTask sst1 = new StbStatTask(cmd, playerName, prms);
                stb_StatRecorder.StbSr_EnqueueTask(sst1);
              }    
            }
    }   

    public StbStatRecorder stb_StatRecorder;

    public class StbStatRecorder
    {
        //private readonly Mission outer; //allows us to reference methods etc from the Mission class as 'outer'
        EventWaitHandle stbSr_Wh = new AutoResetEvent(false);
        Thread stbSr_Worker;
        readonly object stbSr_Locker = new object();
        Queue<StbStatTask> stbSr_Tasks = new Queue<StbStatTask>(2000);
        NumberFormatInfo stbSr_nf = new NumberFormatInfo();
        public Dictionary<string, int[]> stbSr_AllPlayerStats;
        bool stbSr_LogStats;
        bool stbSr_LogStatsCreateHtmlLow;
        bool stbSr_LogStatsCreateHtmlMed;
        bool stbSr_LogStatsUploadHtmlLow;
        bool stbSr_LogStatsUploadHtmlMed;
        string stbSr_LogStatsUploadAddressLow;
        string stbSr_LogStatsUploadAddressMed;
        string stbSr_LogStatsUploadUserName;
        string stbSr_LogStatsUploadPassword;
        bool stbSr_LogErrors;
        string stbSr_ErrorLogPath;
        string stbSr_PlayerStatsPathTxt;
        string stbSr_PlayerStatsPathHtmlLow;
        string stbSr_PlayerStatsPathHtmlMed; 
        int stbSr_numStats; //# of fields recorded in the stats Dictionary/File etc      

        public StbStatRecorder(bool logStats, bool logStatsCreateHtmlLow, bool logStatsCreateHtmlMed, string statsPathTxt,
                               bool logErrors, string errorLogPath, string statsPathHtmlLow, string statsPathHtmlMed,
                               bool logStatsUploadHtmlLow, bool logStatsUploadHtmlMed,
                               string logStatsUploadAddressLow, string logStatsUploadAddressMed,
                               string logStatsUploadUserName, string logStatsUploadPassword)
        {
            stbSr_AllPlayerStats = new Dictionary<string, int[]>();
            stbSr_LogStats = logStats;
            stbSr_LogStatsCreateHtmlLow = logStatsCreateHtmlLow;
            stbSr_LogStatsCreateHtmlMed = logStatsCreateHtmlMed;
            stbSr_LogStatsUploadHtmlLow = logStatsUploadHtmlLow;
            stbSr_LogStatsUploadHtmlMed = logStatsUploadHtmlMed;
            stbSr_LogStatsUploadAddressLow = logStatsUploadAddressLow;
            stbSr_LogStatsUploadAddressMed = logStatsUploadAddressMed;
            stbSr_LogStatsUploadUserName = logStatsUploadUserName;
            stbSr_LogStatsUploadPassword = logStatsUploadPassword;
            stbSr_PlayerStatsPathTxt = statsPathTxt;
            stbSr_PlayerStatsPathHtmlLow = statsPathHtmlLow;
            stbSr_PlayerStatsPathHtmlMed = statsPathHtmlMed;
            stbSr_LogErrors = logErrors;
            stbSr_ErrorLogPath = errorLogPath;
            stbSr_numStats=800;
            StbSr_ReadStatsFromFile();
            stbSr_Worker = new Thread(StbSr_Work);
            //stbSr_Worker.Priority = ThreadPriority.BelowNormal;
            stbSr_Worker.Start();
        }

        public void StbSr_UpdateStatsForDamage(string playerName, int[] p)//p[0]=NamedDamageTypeNo,p[1]=DamageType
        {
            try
            {
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    if (p[1] == 1) //p[1]=DamageType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[642]++; //temp[642]=AirTasksCompletedCount
                        }
                        temp[(p[0])]++;//p[0]=NamedDamageTypeNo
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[1] == 2) //p[1]=DamageType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 2 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[643]++; //temp[643]=GroundTasksCompletedCount
                        }
                        temp[p[0]]++; //p[0]=NamedDamageTypeNo
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[1] == 3) //p[1]=DamageType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[644]++; //temp[644]=NavalTasksCompletedCount
                        }
                        temp[p[0]]++; //p[0]=NamedDamageTypeNo
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForDead(string playerName, int[] p)//p[0]=KillType
        {
            try
            {
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    if (p[0] == 1)//p[0]=KillType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[642]++; //temp[642]=AirTasksCompletedCount
                        }
                        temp[647]++; //temp[647]=AirKillParticipationCount
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[0] == 2)//p[0]=KillType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 2 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[643]++; //temp[643]=GroundTasksCompletedCount
                        }
                        temp[648]++; //temp[648]=GroundKillParticipationCount
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[0] == 3)//p[0]=KillType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[644]++; //temp[644]=NavalTasksCompletedCount
                        }
                        temp[649]++; //temp[649]=NavalKillParticipationCount
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForKilledPlayer(string playerName) //p[0]=KillType
        {
            try
            {                
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                   temp[778]++; //temp[778]=Deaths
                }   
                //When a player is killed, just rename their existing stats/dictionary key to something indicated that player is dead now.
                string date = DateTime.UtcNow.ToString("u").Replace(":", "."); // ":" is the escape character in the stats.txt save file, so we can have it in strings but it is a bit awkward looking; we'll just leave it out
                string newplayerName="||PLAYER DIED " + date + " || " + playerName;
                Calcs.changeKey (stbSr_AllPlayerStats, playerName, newplayerName);                
                              
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForMission(string playerName, int[] p)//p[0]=Mission update type.  
          // 770 Takeoff Count 
          // 771 Safe Landing Count
          // 772 Crash Landing Count
          // 773 Health Damaged Count
          // 774 Parachute Failed Count
          // 775 Parachute Landing Count
          // 776 Player Connected Count
          // 777 Player Disconnected Count
          // 778 Player Death Count
          // 779 Missions Count (took off & flew for at least 5 minutes) //TODO
          //generally, anything you put into p[0] will be incremented by 1 . . .
          //so, ah,  you'd better make sure what you put there actually exists & is what you want

        {
            try
            {
                Console.WriteLine ("Updating MISSION writing " + playerName + " " + p[0]); 
                //int[] temp = new int[stbSr_numStats];
                int [] temp = new int[800];
                
                if ( stbSr_AllPlayerStats is Dictionary<string, int[]> ) { Console.WriteLine ("Dict YES"); } else {Console.WriteLine ("Dict NO");} 
                if  ( stbSr_AllPlayerStats.TryGetValue(playerName, out temp)) {                               
                     temp[p[0]]++ ;
                } else {
                     temp = new int[800];
                     temp[p[0]]++ ;                  
                }                                           
                
                stbSr_AllPlayerStats[playerName] = temp;
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        } 
               
        public void StbSr_UpdateStatsForCutLimb(string playerName, int[] p)//p[0]=LimbNamesNo,p[1]=TaskNo
        {
            //try
            {
                int[] temp = new int[stbSr_numStats];
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))//p[0]=LimbNo
                {
                    if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                    {
                        temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                        temp[642]++; //temp[642]=AirTasksCompletedCount
                    }                     //p[0]=LimbNamesNo
                    temp[(p[0] + 649)]++; //temp[(p[0] + 649)]=CorrespondingLimbNamesNo
                    stbSr_AllPlayerStats[playerName] = temp;
                }
            }
            //catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForTaskCurrent(string playerName, int[] p)//p[0]=TaskNo
        {
            try
            {
                int[] temp = new int[stbSr_numStats];
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    temp[0]++; //temp[0]=PlaceEnters
                    temp[645] = p[0]; //temp[645]=CurrentTaskNo p[0]=TaskNo(1:air,2:ground,3:naval)
                    temp[646] = 0; //temp[646]=CurrentTaskCompletedBool
                    stbSr_AllPlayerStats[playerName] = temp;
                }
                else//for first time user
                {
                    //int[] temp = new int[stbSr_numStats];
                    temp = new int[stbSr_numStats];
                    temp[0]++; //temp[0]=PlaceEnters
                    temp[645] = p[0]; //temp[645]=CurrentTaskNo p[0]=TaskNo(1:air,2:ground,3:naval)
                    stbSr_AllPlayerStats.Add(playerName, temp);
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_ReadStatsFromFile()
        {
            try
            {
                if (stbSr_LogStats)
                {
                    FileInfo fi = new FileInfo(stbSr_PlayerStatsPathTxt);
                    if (fi.Exists)
                    {
                        StreamReader sr = File.OpenText(stbSr_PlayerStatsPathTxt);
                        string s = sr.ReadToEnd();
                        sr.Close();
                        if (s == null) return;
                        if (s == "") return;
                        string[] retrievedStrings = s.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string retrievedStr in retrievedStrings)
                        {
                            StbSr_UpdateSingleUserStat(Calcs.unescapeSemicolon(retrievedStr));
                        }
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateSingleUserStat(string retrievedString)
        {
            try
            {
                string userName = "";
                int[] userData;
                 //Takes care of allowing ; and : characters in usernames.  Should be rare/nonexistent but  let's make sure 
                string[] retrievedData = retrievedString.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine("Retrieving: " + retrievedData.Length + " entries for " + retrievedData[0]);
                if (retrievedData.Length == 2)
                {
                    userName = Calcs.unescapeColon(retrievedData[0]);
                    userData = new int[800];
                    string[] retrievedLines = retrievedData[1].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    
                    int numRet=0;
                    if (retrievedLines.Length == 16)
                    {
                        
                        for (int j = 0; j < 16; j++)
                        {
                            string[] values = retrievedLines[j].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (j < 15 && values.Length == 50)
                            {
                                for (int i = 0; i < 50; i++)
                                {
                                    userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                                    numRet++;
                                }
                            }
                            else if (j == 15 && values.Length == 50)
                            {
                                for (int i = 0; i < 50; i++)
                                {
                                    userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                                    numRet++;
                                }
                            }
                        }
                    }
                    userData[645] = 0;
                    userData[646] = 0;
                    stbSr_AllPlayerStats.Add(userName, userData);
                    Console.WriteLine("Retrieved: " + numRet + " entries for " + retrievedData[0]);
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStats()
        {
            try
            {
                StbSr_SavePlayerStatsStringToFileFull();
                StbSr_SavePlayerStatsStringToFileMedium();
                StbSr_SavePlayerStatsStringToFileLow();
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStatsStringToFileFull()
        {
            try
            {
                if (stbSr_LogStats)
                {
                    if (stbSr_AllPlayerStats.Count == 0) return;
                    using (StreamWriter sw = new StreamWriter(stbSr_PlayerStatsPathTxt, false, System.Text.Encoding.UTF8))
                    {
                        foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                        {
                            sw.Write(";" + Calcs.escapeSemicolon(Calcs.escapeColon(entry.Key)) + ":");
                            sw.WriteLine();
                            for (int j = 0; j < 15; j++)
                            {
                                for (int i = 0; i < 50; i++)
                                {
                                    sw.Write(entry.Value[i + (j * 50)].ToString());
                                    sw.Write(",");
                                }
                                sw.WriteLine();
                            }
                            for (int k = 750; k < 800; k++)
                            {
                                sw.Write(entry.Value[k].ToString());
                                sw.Write(",");
                            }
                            sw.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStatsStringToFileMedium()
        {
            try
            {
                if (stbSr_LogStats)
                {
                    if (stbSr_LogStatsCreateHtmlMed)
                    {
                        //later
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStatsStringToFileLow()
        {
            try
            {
                if (stbSr_LogStats)
                {
                    if (stbSr_LogStatsCreateHtmlLow)
                    {
                        using (StreamWriter sw = new StreamWriter(stbSr_PlayerStatsPathHtmlLow, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
                            sw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");
                            sw.WriteLine("<head>");
                            sw.WriteLine("<title>StatsCompact</title>");
                            sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\" />");
                            sw.WriteLine("<script type=\"text/javascript\" src=\"sorttable.js\"></script>");
                            sw.WriteLine("<link rel=\"stylesheet\" href=\"Style.css\" type=\"text/css\" />");
                            sw.WriteLine("</head>");
                            sw.WriteLine("<body onload= \"initSort()\">");
                            sw.WriteLine("<h1>TWC Mission Server Stats</h1>");
                            sw.WriteLine("<p>Last Update: " + DateTime.Now.ToString("R") + "</p>");
                            
                            sw.WriteLine("<table border=\"0\" cellpadding=\"0\" cellspacing=\"1\">");
                            sw.WriteLine("<thead>");
                            sw.WriteLine("<tr class=\"rh1\"><td class=\"dh bg0\">Name<hr size=\"1\" noshade=\"noshade\"/></td><td class=\"dh bg1\">Player Missions<hr size=\"1\" noshade=\"noshade\"/></td>");
                            sw.WriteLine("<td class=\"dh bg2\" colspan=\"3\">Player Kills<hr size=\"1\" noshade=\"noshade\"/></td>");
                            sw.WriteLine("<td class=\"dh bg3\" colspan=\"3\">Player Damage to Enemy<hr size=\"1\" noshade=\"noshade\"/></td>");
                            sw.WriteLine("<td class=\"dh bg4\" colspan=\"7\">Misc<hr size=\"1\" noshade=\"noshade\"/></td>");                           
                            sw.WriteLine("</table>");
                            
                            
                            
                            sw.WriteLine("<table class=\"sortable\" border=\"0\" cellpadding=\"0\" cellspacing=\"1\">");
                            sw.WriteLine("<thead>");
                            sw.WriteLine("<tr class=\"rh2\">");
                            sw.WriteLine("<th class=\"bg0\">Name<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg4\">Flights<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg1\">Miss- ions<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg1\">Land- ings at/ away from Air- port<hr size=\"1\" noshade=\"noshade\"/></th>");                            
                            sw.WriteLine("<th class=\"bg1\">Para- chute Suc- cess/ Fail <hr size=\"1\" noshade=\"noshade\"/></th>");            
                            sw.WriteLine("<th class=\"bg1\">Times Player Health Damaged/ Aircraft Damaged/ Limbs Cut<hr size=\"1\" noshade=\"noshade\"/></th>");                                                        
                            sw.WriteLine("<th class=\"bg2\">Air/ Ground/ Naval Kills<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg2\">Ave. Kills Per Miss- ion<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Total Dam- age Done<hr size=\"1\" noshade=\"noshade\"/></th>");
                                                                                                                                         
                            sw.WriteLine("<th class=\"bg3\">Fuel / Systems / Guns / Controls-Flaps-Wheels / Cockpit / Engine Damage<hr size=\"1\" noshade=\"noshade\"/></th>");                            
                            sw.WriteLine("<th class=\"bg3\">Damage to Ship or Tank<hr size=\"1\" noshade=\"noshade\"/></th>");         
                            
                            sw.WriteLine("<th class=\"bg3\">Wing/ Other Parts Cut<hr size=\"1\" noshade=\"noshade\"/></th>");
                                                                                      
     
                            sw.WriteLine("<th class=\"bg4\">Entered/ Left Position<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg4\">Moved Pos- it- ion<hr size=\"1\" noshade=\"noshade\"/></th>");                            
                            sw.WriteLine("<th class=\"bg4\">Con- nected/ Dis- con- nected<hr size=\"1\" noshade=\"noshade\"/></th>");                                
                            
                            sw.WriteLine("</tr></thead>");
                            sw.WriteLine("<tbody>");
                            bool alternate = true;
                            foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                            {
                                if (alternate) { sw.WriteLine("<tr class=\"r1\">"); }
                                else { sw.WriteLine("<tr class=\"r2\">"); }
                                alternate = !alternate;
                                sw.WriteLine("<td class=\"dn\">" + entry.Key + "</td>");//Name
                                sw.WriteLine("<td>" + entry.Value[770].ToString() + "</td>");//Take Offs
                                sw.WriteLine("<td>" + entry.Value[779].ToString() + "</td>");//Missions
                                sw.WriteLine("<td>" + entry.Value[771].ToString() + "/" + entry.Value[772].ToString() + "</td>");//Safe landing / Crash Landing
 
                                sw.WriteLine("<td>" + entry.Value[775].ToString() + "/" + entry.Value[774].ToString() + "</td>");//Parachute success / fail                         
                                sw.WriteLine("<td>" + entry.Value[773].ToString() + "/" + (entry.Value[781]+ "/" + entry.Value[782]).ToString() + "</td>");//Health Damaged times / Aircraft Damaged times / Limb Cut times
                                sw.WriteLine("<td>" + entry.Value[647].ToString()  + "/" + entry.Value[648].ToString()  + "/" + entry.Value[649].ToString() + "</td>");//Air Kills
                                                                
                                double kpm = 0;                                
                                if ( entry.Value[779] != 0) {kpm = (double)(entry.Value[647] + entry.Value[648] + entry.Value[649]) / (double) entry.Value[779]; }  
                                sw.WriteLine("<td>" + kpm.ToString("F2") + "</td>");//Total Kills per Mission
                                int sumAllDamage = 0;
                                for (int i = 1; i < 642; i++) { sumAllDamage += entry.Value[i]; }//Damage is 1 to 641 & 650-769
                                for (int i = 650; i < 770; i++) { sumAllDamage += entry.Value[i]; }//Damage is 1 to 641 & 650-769 
                                sw.WriteLine("<td>" + sumAllDamage.ToString() + "</td>");//All Damage Total
                                
                                int sumFuel = 0;
                                for (int i = 1; i < 58; i++) { sumFuel += entry.Value[i]; }//Fuel part damages are [1...57]                                
                                int sumElecHydPne = 0;
                                for (int i = 58; i < 78; i++) { sumElecHydPne += entry.Value[i]; }//ElecHydPne part damages are [58...77]
                                
                                int sumGun = 0;
                                for (int i = 78; i < 138; i++) { sumGun += entry.Value[i]; }//Gun part damages are [78...137]
                                
                                int sumCFBW = 0;
                                for (int i = 138; i < 169; i++) { sumCFBW += entry.Value[i]; }//CFBW part damages are [138...168]
                                
                                int sumCockpit = 0;
                                for (int i = 169; i < 184; i++) { sumCockpit += entry.Value[i]; }//Cockpit part damages are [169...183]                                
                                int sumEng = 0;
                                for (int i = 184; i < 632; i++) { sumEng += entry.Value[i]; }//Engine part damages are [184...631]
                                int sumShipTank = 0;
                                for (int i = 632; i < 642; i++) { sumShipTank += entry.Value[i]; }//ShipTank part damages are [632...641]
                                sw.WriteLine("<td>" + sumFuel.ToString() + "/" +  sumElecHydPne.ToString() + "/" +  sumCFBW.ToString() + "/" +  sumCockpit.ToString() + "/" + sumEng.ToString() + "/" + sumShipTank.ToString() + "</td>");//Cockpit Parts
                                sw.WriteLine("<td>" + sumShipTank.ToString() + "</td>");//Cockpit Parts
                                int sumWingPartsCut = 0;
                                for (int i = 650; i < 683; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                                for (int i = 687; i < 691; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                                for (int i = 697; i < 705; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]    
                                int sumAllOthersCut = 0;
                                for (int i = 683; i < 687; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                                for (int i = 691; i < 697; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                                for (int i = 705; i < 770; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                                sw.WriteLine("<td>" +sumWingPartsCut.ToString() + "/" + sumAllOthersCut.ToString() + "</td>");//All Others Cut                                                                                                
                                sw.WriteLine("<td>" + entry.Value[787].ToString() + "/" + entry.Value[788].ToString() + "</td>");//Entered/Left Position
                                sw.WriteLine("<td>" + entry.Value[780].ToString() + "</td>");//Moved position                    
                                sw.WriteLine("<td>" + entry.Value[776].ToString() + "/" + entry.Value[776].ToString() + "</td>");//Connected/Disconnected                                                       
                                
                                sw.WriteLine("</tr>");
                            }
                            sw.WriteLine("</tbody></table>");
                            sw.WriteLine("<br><br>");
                            sw.WriteLine("<p>Visit <a href=\"http://twcclan.com\">TWCClan.com</a> for more information about TWC and the TWC Mission Server.</p>");
                            sw.WriteLine("<p><b>STATS NOTES:</b> Stats run the duration of one life, until you are killed or captured. A \"flight\" is one take-off/one landing. A \"Mission\" can be several connected flights, if each time you land safely and take off again from the same airport where you landed.</p>");
                            sw.WriteLine("<p>You are awarded a full kill if you have any participation in the kill. Damage counts how many times damage was done without necessarily factoring in the amount or severity of damage. </p>");
                            sw.WriteLine("<p>All of these details are under development and subject to change.  Some are only partly working for now.</p>");
                            sw.WriteLine("<p><i>Note that stats are for fun and for information only. These stats will differ from in-game Netstats for a variety of reasons. CloD does not give servers access to Netstats, so these stats are completely independent of the in-game Netstats. These statistics depend on the information passed by CLoD to the server, which is not always complete or reliable. In addition, the statistics code may have bugs or be incomplete. In short, take these statistics as some potentially fun and useful information about your flying, but not necessarily complete or completely accurate.</i></p>");
                            sw.WriteLine("</body></html>");
                        }
                        if (stbSr_LogStatsUploadHtmlLow)
                        {
                            StbSr_Upload(stbSr_LogStatsUploadAddressLow, stbSr_LogStatsUploadUserName, stbSr_LogStatsUploadPassword, stbSr_PlayerStatsPathHtmlLow);
                        }
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        private void StbSr_Upload(string ftpServer, string userName, string password, string filename)
        {
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Credentials = new System.Net.NetworkCredential(userName, password);
                    client.UploadFile(ftpServer, "STOR", filename);
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_EnqueueTask(StbStatTask task)
        {
            lock (stbSr_Locker) stbSr_Tasks.Enqueue(task);
            Console.WriteLine ("Task enqueued");
            stbSr_Wh.Set();
        }

        void StbSr_Work()
        {
            while (true)
            {
                StbStatTask task = new StbStatTask(StbStatCommands.None, "", new int[] { 0 });
                lock (stbSr_Locker)
                    if (stbSr_Tasks.Count > 0)
                    {
                        task = stbSr_Tasks.Dequeue();
                        if (task.command == StbStatCommands.None) return;
                    }
                if (task.command != StbStatCommands.None)
                {
                    switch (task.command)
                    {
                        case StbStatCommands.Damage:
                            StbSr_UpdateStatsForDamage(task.player, task.parameters);
                            break;
                        case StbStatCommands.Dead:
                            StbSr_UpdateStatsForDead(task.player, task.parameters);
                            break;
                        case StbStatCommands.PlayerKilled:
                            StbSr_UpdateStatsForKilledPlayer(task.player);
                            break;
                        case StbStatCommands.Mission:
                            StbSr_UpdateStatsForMission(task.player, task.parameters);
                            break;        
                        case StbStatCommands.CutLimb:
                            StbSr_UpdateStatsForCutLimb(task.player, task.parameters);
                            break;
                        case StbStatCommands.TaskCurrent:
                            StbSr_UpdateStatsForTaskCurrent(task.player, task.parameters);
                            break;
                        case StbStatCommands.Save:
                            StbSr_SavePlayerStats();
                            break;
                        default:
                            break;
                    }
                }
                else
                    stbSr_Wh.WaitOne();
            }
        }

        public void StbSr_FinishWaitingTasks()
        {
            StbStatTask t = new StbStatTask(StbStatCommands.None, "", new int[] { 0 });
            StbSr_EnqueueTask(t);// Signal to exit.
            stbSr_Worker.Join();// Wait for the thread to finish.
            stbSr_Wh.Close();// Release resources.
        }

        public void StbSr_PrepareErrorMessage(Exception ex)
        {
            if (stbSr_LogErrors)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(StbSr_LogError),
                    (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
            }
        }

        public void StbSr_LogError(object data)
        {
            try
            {
                FileInfo fi = new FileInfo(stbSr_ErrorLogPath);
                StreamWriter sw;
                if (fi.Exists) { sw = new StreamWriter(stbSr_ErrorLogPath, true, System.Text.Encoding.UTF8); }
                else { sw = new StreamWriter(stbSr_ErrorLogPath, false, System.Text.Encoding.UTF8); }
                sw.WriteLine((string)data);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); };
        }
    }

    #endregion

    #region stb Methods

    // Error Methods-----------------------------------------------------------------------------------------------------------

    public void Stb_PrepareErrorMessage(Exception ex)
    {
        if (stb_LogErrors)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(Stb_LogError),
                (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
        }
    }

    public void Stb_LogError(object data)
    {
        try
        {
            FileInfo fi = new FileInfo(stb_ErrorLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(stb_ErrorLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(stb_ErrorLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); };
    }

    // Balance Methods---------------------------------------------------------------------------------------------------------

    private void Stb_BalanceUpdate(Player p, AiActor actor, int pIndex, bool entered)
    {
        try
        {
            AiAircraft aircraft = actor as AiAircraft;
            if (aircraft == null) { return; }
            if (aircraft.Type() == AircraftType.Fighter || aircraft.Type() == AircraftType.HeavyFighter)
            {
                if (aircraft.CrewFunctionPlace(pIndex) == CrewFunction.Pilot)
                {
                    if (p.Army() == 1) { stb_RedFighters += entered ? 1 : -1; return; }
                    if (p.Army() == 2) { stb_BlueFighters += entered ? 1 : -1; return; }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private bool Stb_BalanceCheckForBan(Player p, AiActor actor, int placeIndex)
    {
        try
        {
            stb_Ratio = (stb_RedFighters > stb_BlueFighters) ?
                ((double)stb_RedFighters / (double)stb_BlueFighters) : ((double)stb_BlueFighters / (double)stb_RedFighters);
            stb_Delta = (stb_RedFighters > stb_BlueFighters) ?
                (stb_RedFighters - stb_BlueFighters) : (stb_BlueFighters - stb_RedFighters);
            if (stb_BalanceFightersByRatio)
            {
                if (stb_Ratio > stb_DesiredRatio &&
                    (stb_RedFighters + stb_BlueFighters) > stb_MinCountForBalance)
                {
                    Stb_BalanceMessageForBan(p, actor, placeIndex); return true;
                }
            }
            else
            {
                if (stb_Delta > stb_DesiredDelta &&
                    (stb_RedFighters + stb_BlueFighters) > stb_MinCountForBalance)
                {
                    Stb_BalanceMessageForBan(p, actor, placeIndex); return true;
                }
            }
            return false;
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); return false; }
    }

    private void Stb_BalanceMessageForBan(Player p, AiActor actor, int placeIndex)
    {
        try
        {
            GamePlay.gpLogServerBegin(new Player[] { p }, stb_BalanceMsgRus);
            GamePlay.gpLogServerEnd();
            GamePlay.gpLogServerBegin(new Player[] { p }, stb_BalanceMsgEng);
            GamePlay.gpLogServerEnd();
            GamePlay.gpHUDLogCenter(new Player[] { p }, stb_BalanceMsgHud);
            Stb_BalanceDisableForBan(actor);
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_BalanceDisableForBan(AiActor actor)
    {
        try
        {
            AiAircraft aircraft = actor as AiAircraft;
            if (aircraft != null)
            {
                Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsElevatorDisabled);
                Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsAileronsDisabled);
                Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsRudderDisabled);
                Stb_DamagePlane(aircraft, NamedDamageTypes.Eng0TotalSeizure);
                AiAirGroup aag = aircraft.AirGroup();
                if (aag != null)
                {
                    if (aag.aircraftEnginesNum() == 2) { Stb_DamagePlane(aircraft, NamedDamageTypes.Eng1TotalSeizure); }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    // StartUp Methods---------------------------------------------------------------------------------------------------------

    private void Stb_LoadAntiAirDraftToMemory()
    {
        string[,] shipsRed = new string[6, 2];
        string[,] shipsBlue = new string[6, 2];

        shipsRed[0, 0] = "13630.00 31515.00 38.40  0 2 5.56";
        shipsRed[0, 1] = "13610.00 31555.00 38.40";
        shipsRed[1, 0] = "14210.00 31680.00 38.40  0 2 5.56";
        shipsRed[1, 1] = "14255.00 31690.00 38.40";
        shipsRed[2, 0] = "13100.00 31150.00 38.40  0 2 5.56";
        shipsRed[2, 1] = "13065.00 31130.00 38.40";
        shipsRed[3, 0] = "18600.00 31645.00 38.40  0 2 5.56";
        shipsRed[3, 1] = "18615.00 31685.00 38.40";
        shipsRed[4, 0] = "18035.00 31795.00 38.40  0 2 5.56";
        shipsRed[4, 1] = "17970.00 31800.00 38.40";
        shipsRed[5, 0] = "19030.00 31310.00 38.40  0 2 5.56";
        shipsRed[5, 1] = "19060.00 31285.00 38.40";

        shipsBlue[0, 0] = "11690.00 9905.00 38.40  0 2 5.56";
        shipsBlue[0, 1] = "11620.00 9905.00 38.40";
        shipsBlue[1, 0] = "13025.00 10260.00 38.40  0 2 5.56";
        shipsBlue[1, 1] = "13075.00 10275.00 38.40";
        shipsBlue[2, 0] = "12450.00 10030.00 38.40  0 2 5.56";
        shipsBlue[2, 1] = "12455.00 9990.00 38.40";
        shipsBlue[3, 0] = "18260.00 9100.00 38.40  0 2 5.56";
        shipsBlue[3, 1] = "18240.00 9070.00 38.40";
        shipsBlue[4, 0] = "17930.00 9345.00 38.40  0 2 5.56";
        shipsBlue[4, 1] = "17895.00 9370.00 38.40";
        shipsBlue[5, 0] = "18675.00 8940.00 38.40  0 2 5.56";
        shipsBlue[5, 1] = "18695.00 8930.00 38.40";

        string[] gunsRed = new string[28];
        string[] gunsBlue = new string[28];

        gunsBlue[0] = "Artillery.4_cm_Flak_28 de 18030 9660 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[1] = "Artillery.4_cm_Flak_28 de 18370 9650 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[2] = "Artillery.4_cm_Flak_28 de 18710 9640 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[3] = "Artillery.4_cm_Flak_28 de 18990 9630 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[4] = "Artillery.4_cm_Flak_28 de 19400 9620 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[5] = "Artillery.4_cm_Flak_28 de 11630 10310 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[6] = "Artillery.4_cm_Flak_28 de 11980 10400 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[7] = "Artillery.4_cm_Flak_28 de 12300 10480 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[8] = "Artillery.4_cm_Flak_28 de 12650 10570 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[9] = "Artillery.4_cm_Flak_28 de 12970 10650 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[10] = "Artillery.Flak37 de 18710 10110 240 /timeout 0/radius_hide 3000 ";
        gunsBlue[11] = "Artillery.Flak37 de 18775 10110 -90 /timeout 0/radius_hide 3000 ";
        gunsBlue[12] = "Artillery.Flak37 de 18840 10110 -45 /timeout 0/radius_hide 3000 ";
        gunsBlue[13] = "Artillery.Flak37 de 18710 10190 135 /timeout 0/radius_hide 3000 ";
        gunsBlue[14] = "Artillery.Flak37 de 18775 10190 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[15] = "Artillery.Flak37 de 18840 10190 45 /timeout 0/radius_hide 3000 ";
        gunsBlue[16] = "Artillery.Flak37 de 12410 10710 225 /timeout 0/radius_hide 3000 ";
        gunsBlue[17] = "Artillery.Flak37 de 12475 10710 -90 /timeout 0/radius_hide 3000 ";
        gunsBlue[18] = "Artillery.Flak37 de 12540 10710 -45 /timeout 0/radius_hide 3000 ";
        gunsBlue[19] = "Artillery.Flak37 de 12410 10790 135 /timeout 0/radius_hide 3000 ";
        gunsBlue[20] = "Artillery.Flak37 de 12475 10790 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[21] = "Artillery.Flak37 de 12540 10790 45 /timeout 0/radius_hide 3000 ";
        gunsBlue[22] = "Artillery.RRH_GER1 de 18710 10150 180 /timeout 0/radius_hide 3000 ";
        gunsBlue[23] = "Artillery.RRH_GER1 de 18775 10150 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[24] = "Artillery.RRH_GER1 de 18840 10150 0 /timeout 0/radius_hide 3000 ";
        gunsBlue[25] = "Artillery.RRH_GER1 de 12410 10750 180 /timeout 0/radius_hide 3000 ";
        gunsBlue[26] = "Artillery.RRH_GER1 de 12475 10750 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[27] = "Artillery.RRH_GER1 de 12540 10750 0 /timeout 0/radius_hide 3000 ";
        gunsRed[0] = "Artillery.Bofors gb 13280 30560 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[1] = "Artillery.Bofors gb 18770 30530 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[2] = "Artillery.Bofors gb 18430 30620 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[3] = "Artillery.Bofors gb 13610 30730 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[4] = "Artillery.Bofors gb 18110 30700 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[5] = "Artillery.Bofors gb 17770 30790 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[6] = "Artillery.Bofors gb 13890 30880 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[7] = "Artillery.Bofors gb 17440 30880 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[8] = "Artillery.Bofors gb 14190 31040 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[9] = "Artillery.Bofors gb 14500 31210 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[10] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13710 30610 -135 /timeout 0/radius_hide 3000 ";
        gunsRed[11] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13775 30610 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[12] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13840 30610 -45 /timeout 0/radius_hide 3000 ";
        gunsRed[13] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17710 30610 -135 /timeout 0/radius_hide 3000 ";
        gunsRed[14] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17775 30610 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[15] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13710 30690 135 /timeout 0/radius_hide 3000 ";
        gunsRed[16] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17840 30610 -45 /timeout 0/radius_hide 3000 ";
        gunsRed[17] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13775 30690 90 /timeout 0/radius_hide 3000 ";
        gunsRed[18] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13840 30690 45 /timeout 0/radius_hide 3000 ";
        gunsRed[19] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17710 30690 135 /timeout 0/radius_hide 3000 ";
        gunsRed[20] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17775 30690 90 /timeout 0/radius_hide 3000 ";
        gunsRed[21] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17840 30690 45 /timeout 0/radius_hide 3000 ";
        gunsRed[22] = "Artillery.SoundLocator_MkIIIV_UK1 gb 13710 30650 180 /timeout 0/radius_hide 3000 ";
        gunsRed[23] = "Artillery.SoundLocator_MkIIIV_UK1 gb 13775 30650 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[24] = "Artillery.SoundLocator_MkIIIV_UK1 gb 13840 30650 0 /timeout 0/radius_hide 3000 ";
        gunsRed[25] = "Artillery.SoundLocator_MkIIIV_UK1 gb 17720 30650 180 /timeout 0/radius_hide 3000 ";
        gunsRed[26] = "Artillery.SoundLocator_MkIIIV_UK1 gb 17775 30650 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[27] = "Artillery.SoundLocator_MkIIIV_UK1 gb 17840 30650 0 /timeout 0/radius_hide 3000 ";

        ISectionFile f = GamePlay.gpCreateSectionFile();
        string s = "";
        string k = "";
        string v = "";

        s = "Chiefs";
        v = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        for (int i = 0; i < 6; i++)
        {
            k = "baseDefenseShip_R_" + i.ToString("00");
            f.add(s, k, v);
        }
        v = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        for (int i = 0; i < 6; i++)
        {
            k = "baseDefenseShip_B_" + i.ToString("00");
            f.add(s, k, v);
        }

        v = "";
        for (int i = 0; i < 6; i++)
        {
            s = "baseDefenseShip_R_" + i.ToString("00") + "_Road";
            k = shipsRed[i, 0];
            f.add(s, k, v);
            k = shipsRed[i, 1];
            f.add(s, k, v);
        }
        for (int i = 0; i < 6; i++)
        {
            s = "baseDefenseShip_B_" + i.ToString("00") + "_Road";
            k = shipsBlue[i, 0];
            f.add(s, k, v);
            k = shipsBlue[i, 1];
            f.add(s, k, v);
        }

        s = "Stationary";
        for (int i = 0; i < 28; i++)
        {
            k = "baseDefenseGun_R_" + i.ToString("00");
            v = gunsRed[i];
            f.add(s, k, v);
        }
        for (int i = 0; i < 28; i++)
        {
            k = "baseDefenseGun_B_" + i.ToString("00");
            v = gunsBlue[i];
            f.add(s, k, v);
        }

        stb_BaseAntiAirDefenses = f;
    }

    private void Stb_LoadFrontline1DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontShip_R1";
        value = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        key = "FrontShip_B1";
        value = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        value = "";

        sect = "FrontShip_R1_Road";
        key = "7500.00 27500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "7500.00 14400.00 38.4"; f.add(sect, key, value);

        sect = "FrontShip_B1_Road";
        key = "7500.00 14400.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "7500.00 27500.00 38.40"; f.add(sect, key, value);

        stb_Frontline1 = f;
    }

    private void Stb_LoadFrontline2DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontShip_R2";
        value = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        key = "FrontShip_B2";
        value = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        value = "";

        sect = "FrontShip_R2_Road";
        key = "13600.00 27500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "13427.00 18871.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "13171.00 17635.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "13600.00 14400.00 38.4"; f.add(sect, key, value);

        sect = "FrontShip_B2_Road";
        key = "13600.00 14400.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "13171.00 17635.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "13427.00 18871.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "13600.00 27500.00 38.40"; f.add(sect, key, value);

        stb_Frontline2 = f;
    }

    private void Stb_LoadFrontline3DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontShip_R3";
        value = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        key = "FrontShip_B3";
        value = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        value = "";

        sect = "FrontShip_R3_Road";
        key = "15900.00 27500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "15700.00 23500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "17500.00 20600.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "17100.00 14400.00 38.4"; f.add(sect, key, value);

        sect = "FrontShip_B3_Road";
        key = "17100.00 14400.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "17500.00 20600.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "15700.00 23500.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "15900.00 27500.00 38.40"; f.add(sect, key, value);

        stb_Frontline3 = f;
    }

    private void Stb_LoadFrontline4DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontArmor_R1";
        value = "Armor.Cruiser_Mk_IVA gb /num_units 6";
        f.add(sect, key, value);

        key = "FrontArmor_B1";
        value = "Armor.Pz_IIC de /num_units 6";
        f.add(sect, key, value);

        value = "";

        sect = "FrontArmor_R1_Road";
        key = "20700.00 25800.00 38.4  0 2 7.14"; f.add(sect, key, value);
        key = "20900.00 25000.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "20609.00 23948.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "20849.00 23059.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21284.00 22438.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21591.00 22044.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21700.00 21400.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21638.00 20728.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21806.00 20209.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22187.00 19677.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22400.00 19100.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22200.00 18400.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22300.00 17700.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22211.00 16941.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21854.00 16291.00 38.4"; f.add(sect, key, value);

        sect = "FrontArmor_B1_Road";
        key = "21854.00 16291.00 38.40  0 2 7.14"; f.add(sect, key, value);
        key = "22211.00 16941.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22300.00 17700.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22200.00 18400.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22400.00 19100.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22187.00 19677.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21806.00 20209.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21638.00 20728.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21700.00 21400.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21591.00 22044.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21284.00 22438.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20849.00 23059.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20609.00 23948.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20900.00 25000.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20700.00 25800.00 38.40"; f.add(sect, key, value);

        stb_Frontline4 = f;
    }

    private void Stb_LoadBombers1DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string s = "";
        string k = "";
        string v = "";

        s = "AirGroups";
        k = "gb02.03"; v = ""; f.add(s, k, v);
        k = "g02.03"; v = ""; f.add(s, k, v);
        s = "gb02.03";
        k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
        k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
        k = "Class"; v = "Aircraft.WellingtonMkIc"; f.add(s, k, v);
        k = "Formation"; v = "VIC3"; f.add(s, k, v);
        k = "CallSign"; v = "26"; f.add(s, k, v);
        k = "Fuel"; v = "66"; f.add(s, k, v);
        k = "Weapons"; v = "1 1 3"; f.add(s, k, v);
        k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        s = "gb02.03_Way";
        k = "NORMFLY"; v = "19500.00 39000.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "19500.00 31000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "17500.00 24000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "17000.00 18000.00 2000.00 250.00"; f.add(s, k, v);
        k = "GATTACK_POINT"; v = "15660.00 10800.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "15660.00 9800.00 2200.00 333.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "19500.00 39000.00 3000.00 366.00"; f.add(s, k, v);
        s = "g02.03";
        k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
        k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
        k = "Class"; v = "Aircraft.He-111H-2"; f.add(s, k, v);
        k = "Formation"; v = "VIC3"; f.add(s, k, v);
        k = "CallSign"; v = "30"; f.add(s, k, v);
        k = "Fuel"; v = "66"; f.add(s, k, v);
        k = "Weapons"; v = "1 1 1 1 1 1 2"; f.add(s, k, v);
        k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        s = "g02.03_Way";
        k = "NORMFLY"; v = "10500.00 2000.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "10500.00 10000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "12500.00 18000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "13000.00 24000.00 2000.00 250.00"; f.add(s, k, v);
        k = "GATTACK_POINT"; v = "16300.00 31350.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "16300.00 32350.00 2200.00 333.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "10500.00 2000.00 3000.00 366.00"; f.add(s, k, v);
        s = "Stationary";
        k = "BmbArt_B0"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_B1"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_B2"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_B3"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R0"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R1"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R2"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R3"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);

        stb_Bombers1 = f;
    }

    private void Stb_LoadBombers2DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string s = "";
        string k = "";
        string v = "";

        s = "AirGroups";
        k = "gb02.03"; v = ""; f.add(s, k, v);
        k = "g02.03"; v = ""; f.add(s, k, v);
        s = "gb02.03";
        k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
        k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
        k = "Class"; v = "Aircraft.WellingtonMkIc"; f.add(s, k, v);
        k = "Formation"; v = "VIC3"; f.add(s, k, v);
        k = "CallSign"; v = "26"; f.add(s, k, v);
        k = "Fuel"; v = "66"; f.add(s, k, v);
        k = "Weapons"; v = "1 1 3"; f.add(s, k, v);
        k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        s = "gb02.03_Way";
        k = "NORMFLY"; v = "11600.00 38900.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "12500.00 30000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "11500.00 24000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "12500.00 18000.00 2000.00 250.00"; f.add(s, k, v);
        k = "GATTACK_POINT"; v = "15660.00 10800.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "15660.00 9800.00 2200.00 333.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "11600.00 38900.00 3000.00 366.00"; f.add(s, k, v);
        s = "g02.03";
        k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
        k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
        k = "Class"; v = "Aircraft.He-111H-2"; f.add(s, k, v);
        k = "Formation"; v = "VIC3"; f.add(s, k, v);
        k = "CallSign"; v = "30"; f.add(s, k, v);
        k = "Fuel"; v = "66"; f.add(s, k, v);
        k = "Weapons"; v = "1 1 1 1 1 1 2"; f.add(s, k, v);
        k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
        k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
        s = "g02.03_Way";
        k = "NORMFLY"; v = "20500.00 2000.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "20500.00 10000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "18500.00 18000.00 2000.00 240.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "18500.00 24000.00 2000.00 250.00"; f.add(s, k, v);
        k = "GATTACK_POINT"; v = "16300.00 31350.00 2000.00 250.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "16300.00 32350.00 2200.00 333.00"; f.add(s, k, v);
        k = "NORMFLY"; v = "20500.00 2000.00 3000.00 366.00"; f.add(s, k, v);
        s = "Stationary";
        k = "BmbArt_B0"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_B1"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_B2"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_B3"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R0"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R1"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R2"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
        k = "BmbArt_R3"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);

        stb_Bombers2 = f;
    }

    // Recursive Methods-------------------------------------------------------------------------------------------------------

    private void Stb_SpawnAntiAirRecursive()
    {
        try
        {
            for (int i = (stb_MissionsCount - 1); i > (-1); i--)
            {
                bool willBreak = false;
                for (int j = 0; j < 6; j++)
                {
                    string fullActorName = i.ToString() + ":baseDefenseShip_R_" + j.ToString("00");
                    AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                    if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                }
                for (int j = 0; j < 6; j++)
                {
                    string fullActorName = i.ToString() + ":baseDefenseShip_B_" + j.ToString("00");
                    AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                    if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                }
                if (willBreak) { break; }
            }
            for (int i = (stb_MissionsCount - 1); i > (-1); i--)
            {
                bool willBreak = false;
                for (int j = 0; j < 28; j++)
                {
                    string fullActorName = i.ToString() + ":baseDefenseGun_R_" + j.ToString("00");
                    AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                    if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                }
                for (int j = 0; j < 28; j++)
                {
                    string fullActorName = i.ToString() + ":baseDefenseGun_B_" + j.ToString("00");
                    AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                    if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                }
                if (willBreak) { break; }
            }
            GamePlay.gpPostMissionLoad(stb_BaseAntiAirDefenses);
            Timeout(stb_SpawnAntiAirDelay, Stb_SpawnAntiAirRecursive);
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_SpawnFrontline1Recursive()
    {
        try
        {
            if (stb_SpawnFrontline1)
            {
                GamePlay.gpPostMissionLoad(stb_Frontline1);
                Timeout(600.0, Stb_SpawnFrontline1Recursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_SpawnFrontline2Recursive()
    {
        try
        {
            if (stb_SpawnFrontline2)
            {
                GamePlay.gpPostMissionLoad(stb_Frontline2);
                Timeout(660.0, Stb_SpawnFrontline2Recursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_SpawnFrontline3Recursive()
    {
        try
        {
            if (stb_SpawnFrontline3)
            {
                GamePlay.gpPostMissionLoad(stb_Frontline3);
                Timeout(720.0, Stb_SpawnFrontline3Recursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_SpawnFrontline4Recursive()
    {
        try
        {
            if (stb_SpawnFrontline4)
            {
                GamePlay.gpPostMissionLoad(stb_Frontline4);
                Timeout(420.0, Stb_SpawnFrontline4Recursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_SpawnBombersRecursive()
    {
        try
        {
            if (stb_SpawnBombers)
            {
                for (int i = (stb_MissionsCount - 1); i > (-1); i--)
                {
                    bool willBreak = false;
                    for (int k = 0; k < 2; k++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            string fullActorName = i.ToString() + ":g02.0" + k.ToString() + j.ToString();
                            AiAircraft aiAircraft = GamePlay.gpActorByName(fullActorName) as AiAircraft;
                            if (aiAircraft != null)
                            {
                                Timeout(456.0, () => { Stb_DestroyPlaneUnsafe(aiAircraft); });
                                willBreak = true;
                            }
                        }
                    }
                    for (int k = 0; k < 2; k++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            string fullActorName = i.ToString() + ":gb02.0" + k.ToString() + j.ToString();
                            AiAircraft aiAircraft = GamePlay.gpActorByName(fullActorName) as AiAircraft;
                            if (aiAircraft != null)
                            {
                                Timeout(456.0, () => { Stb_DestroyPlaneUnsafe(aiAircraft); });
                                willBreak = true;
                            }
                        }
                    }
                    if (willBreak) { break; }
                }
                for (int i = (stb_MissionsCount - 1); i > (-1); i--)
                {
                    bool willBreak = false;
                    for (int j = 0; j < 4; j++)
                    {
                        string fullActorName = i.ToString() + ":BmbArt_R" + j.ToString("0");
                        AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                        if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        string fullActorName = i.ToString() + ":BmbArt_B" + j.ToString("0");
                        AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                        if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                    }
                    if (willBreak) { break; }
                }
                if (stb_BomberMissionTurn)
                {
                    GamePlay.gpPostMissionLoad(stb_Bombers1);
                    stb_BomberMissionTurn = !stb_BomberMissionTurn;
                }
                else
                {
                    GamePlay.gpPostMissionLoad(stb_Bombers2);
                    stb_BomberMissionTurn = !stb_BomberMissionTurn;
                }
                Timeout(600.0, Stb_SpawnBombersRecursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_LogStatsRecursive()
    {
        try
        {
            if (stb_LogStats)
            {
                StbStatTask sst = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 0 });
                stb_StatRecorder.StbSr_EnqueueTask(sst);
                Timeout(stb_LogStatsDelay, Stb_LogStatsRecursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }
    
    
    private void Stb_StatsServerAnnounceRecursive()
    {
        try
        {   
          if (stb_AnnounceStatsMessages)         
            {
                GamePlay.gpLogServer(null, "Check your stats online at " + stb_LogStatsPublicAddressLow, new object[] { });                          
                Timeout(stb_AnnounceStatsMessagesFrequency, Stb_StatsServerAnnounceRecursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }
    
                      

    // Other Methods-----------------------------------------------------------------------------------------------------------

    private void Stb_CutPlane(AiAircraft aircraft, LimbNames ln)
    {
        try { aircraft.cutLimb(ln); }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DamagePlane(AiAircraft aircraft, NamedDamageTypes ndt)
    {
        try { aircraft.hitNamed(ndt); }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyPlaneSafe(AiAircraft aircraft)
    {
        try
        {
            if (aircraft == null) { return; }
            for (int place = 0; place < aircraft.Places(); place++)
            {
                if (aircraft.Player(place) != null) { return; }
            }
            if (aircraft.IsAirborne())
            {
                bool insideAf = false;
                if ((aircraft.Pos().distance(ref af1) < 500.0) && (aircraft.Pos().z < 10.0)) { insideAf = true; }
                if ((aircraft.Pos().distance(ref af2) < 500.0) && (aircraft.Pos().z < 70.0)) { insideAf = true; }
                if ((aircraft.Pos().distance(ref af3) < 500.0) && (aircraft.Pos().z < 40.0)) { insideAf = true; }
                if ((aircraft.Pos().distance(ref af4) < 500.0) && (aircraft.Pos().z < 70.0)) { insideAf = true; }
                if (insideAf) { aircraft.Destroy(); }
                else
                {
                    Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsElevatorDisabled);
                    Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsAileronsDisabled);
                    Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsRudderDisabled);
                    Stb_DamagePlane(aircraft, NamedDamageTypes.Eng0TotalSeizure);
                    AiAirGroup aag = aircraft.AirGroup();
                    if (aag != null)
                    {
                        if (aag.aircraftEnginesNum() == 2) { Stb_DamagePlane(aircraft, NamedDamageTypes.Eng1TotalSeizure); }
                    }
                }
            }
            else { aircraft.Destroy(); }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyPlaneUnsafe(AiAircraft aircraft)
    {
        try
        {
            if (aircraft != null)
                aircraft.Destroy();
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyFrontShip(AiGroundActor aiGroundActor)
    {
        try
        {
            if (aiGroundActor != null) { if (aiGroundActor.Name().Contains("FrontShip")) { aiGroundActor.Destroy(); } }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyFrontArmor(AiGroundActor aiGroundActor)
    {
        try
        {
            if (aiGroundActor != null) { if (aiGroundActor.Name().Contains("FrontArmor")) { aiGroundActor.Destroy(); } }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }
    

    #endregion

    // Overrides---------------------------------------------------------------------------------------------------------------

    public override void Init(ABattle battle, int missionNumber)
    {
        base.Init(battle, missionNumber);
        MissionNumberListener = -1;
    }

    public override void OnBattleStarted()
    {
        #region stb
        base.OnBattleStarted();
        #endregion
        //add your code here
    }

    public override void OnMissionLoaded(int missionNumber)
    {
        #region stb
        base.OnMissionLoaded(missionNumber);
        stb_MissionsCount++;
        #endregion
        //add your code here
        if (missionNumber==MissionNumber ) {
          string s = stb_AppPath.Remove(stb_AppPath.Length - 5, 5);
          stb_FullPath = s + @"missions\Multi\Fatal\";
          stb_ErrorLogPath = stb_FullPath + stb_ErrorLogPath;
          stb_StatsPathTxt = stb_FullPath + stb_StatsPathTxt;
          stb_StatsPathHtmlLow = stb_FullPath + stb_StatsPathHtmlLow;
          Stb_LoadAntiAirDraftToMemory();
          Stb_LoadFrontline1DraftToMemory();
          Stb_LoadFrontline2DraftToMemory();
          Stb_LoadFrontline3DraftToMemory();
          Stb_LoadFrontline4DraftToMemory();
          Stb_LoadBombers1DraftToMemory();
          Stb_LoadBombers2DraftToMemory();
          stb_StatRecorder = new StbStatRecorder(stb_LogStats, stb_LogStatsCreateHtmlLow, stb_LogStatsCreateHtmlMed, stb_StatsPathTxt,
                                                 stb_LogErrors, stb_ErrorLogPath, stb_StatsPathHtmlLow, stb_StatsPathHtmlMed,
                                                 stb_LogStatsUploadHtmlLow, stb_LogStatsUploadHtmlMed,
                                                 stb_LogStatsUploadAddressLow, stb_LogStatsUploadAddressMed,
                                                 stb_LogStatsUploadUserName, stb_LogStatsUploadPassword);
          stb_ContinueMissionRecorder=new StbContinueMissionRecorder(                                                stb_LogErrors, stb_ErrorLogPath);                                                 
          if (stb_SpawnAntiAir) { Stb_SpawnAntiAirRecursive(); }
          if (stb_SpawnFrontline1) { Stb_SpawnFrontline1Recursive(); }
          if (stb_SpawnFrontline2) { Stb_SpawnFrontline2Recursive(); }
          if (stb_SpawnFrontline3) { Stb_SpawnFrontline3Recursive(); }
          if (stb_SpawnFrontline4) { Stb_SpawnFrontline4Recursive(); }
          if (stb_SpawnBombers) { Stb_SpawnBombersRecursive(); }
          if (stb_LogStats) { Stb_LogStatsRecursive(); }
      }    

    }

    /*******************************************************************
    //We're not using their onplaceenter bec. it does balance etc etc that we don't want or need. 
    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceEnter(player, actor, placeIndex);
        try
        {
            Stb_BalanceUpdate(player, actor, placeIndex, true);
            if (!Stb_BalanceCheckForBan(player, actor, placeIndex))
            {
                if (placeIndex == 0 && player.PlacePrimary() == 0 && player.PlaceSecondary() == -1)
                {
                    AiAircraft aircraft = actor as AiAircraft;
                    if (aircraft != null)
                    {
                        StbStatTask sst;
                        if (player.Army() == 1 && actor.Name().Contains(":BoB_RAF_B_7Sqn.000"))
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 2 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ground Units");
                        }
                        else if (player.Army() == 2 && actor.Name().Contains(":BoB_LW_KG1_I.000"))
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 2 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ground Units");
                        }
                        else if (aircraft.Type() == AircraftType.Fighter || aircraft.Type() == AircraftType.HeavyFighter)
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 1 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Aircrafts");
                        }
                        else
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 3 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ships");
                        }
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }
    
    ***************************************************************/

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceEnter(player, actor, placeIndex);
        try               
        {
          StbStatTask sst;
          sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 3 });
          GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ships");
        
          stb_StatRecorder.StbSr_EnqueueTask(sst); 

          StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 787 });
          stb_StatRecorder.StbSr_EnqueueTask(sst1);
          
          //set up the possibility to continue a mission over numerous flights
          stb_ContinueMissionRecorder.StbCmr_SavePositionEnter (player, actor);

        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    
    //787 Enter place
    //788 Leave place

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceLeave(player, actor, placeIndex);
        try
        {        
        
          StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 788 });
          stb_StatRecorder.StbSr_EnqueueTask(sst1);
          
          //set up the possibility to continue a mission over numerous flights
          stb_ContinueMissionRecorder.StbCmr_SavePositionLeave (player, actor);
          
              Stb_BalanceUpdate(player, actor, placeIndex, false);
              if (stb_DestroyOnPlaceLeave)
              {
                  AiAircraft aircraft = actor as AiAircraft;
                  if (aircraft != null)
                  {
                      Stb_DestroyPlaneSafe(aircraft);//works fine if you havent take off or flying in neutral zone
                      Timeout(10.0, () => { Stb_DestroyPlaneSafe(aircraft); });//second call for cleanup
                      //game thinks that you are still airborne after you landed inside an airfield perimeter
                      //because we havent set landing waypoints & airfields properly
                      //this is why we call Stb_DestroyPlaneSafe again, we could do the same by adding an ai with airport.cpp
                      //instead of calling this method twice but airport.cpp does the cleanup by killing planes 
                      //which leads to wrong stats collection, for ex a user who just damaged an enemy plane normally takes only
                      //damage bonus, but with airport.cpp, if the damaged plane lands, he would also take a kill participation bonus
                  }
              }          
        }  
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        #region stb
        base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);
        try
        {
            bool willReportDamage = false;
            if (initiator != null)
            {
                if (initiator.Player != null)
                {
                    if (initiator.Player.Army() == 1 && actor.Army() == 2) { willReportDamage = true; }
                    if (initiator.Player.Army() == 2 && actor.Army() == 1) { willReportDamage = true; }
                }
            }
            if (willReportDamage)
            {
                AiGroundActor ga = actor as AiGroundActor;
                if (ga != null)
                {
                    if (ga.Type() == AiGroundActorType.AAGun || ga.Type() == AiGroundActorType.Artillery || ga.Type() == AiGroundActorType.Tank)
                    {
                        StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 2 });
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                    else if (ga.Type() == AiGroundActorType.ShipBattleship || ga.Type() == AiGroundActorType.ShipCarrier ||
                             ga.Type() == AiGroundActorType.ShipCruiser || ga.Type() == AiGroundActorType.ShipDestroyer ||
                             ga.Type() == AiGroundActorType.ShipMisc || ga.Type() == AiGroundActorType.ShipSmallWarship ||
                             ga.Type() == AiGroundActorType.ShipSubmarine || ga.Type() == AiGroundActorType.ShipTransport)
                    {
                        StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 3 });
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnAircraftDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        #region stb
        base.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
        try
        {
            StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 781 }); //player damaged, 781                                                                        
            bool willReportDamage = false;
            if (initiator != null)
            {
                if (initiator.Player != null)
                {
                    if (initiator.Player.Army() == 1 && aircraft.Army() == 2) { willReportDamage = true; }
                    if (initiator.Player.Army() == 2 && aircraft.Army() == 1) { willReportDamage = true; }
                }
            }
            if (willReportDamage)
            {
                StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 1 });
                stb_StatRecorder.StbSr_EnqueueTask(sst);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnAircraftCutLimb(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, LimbNames limbName)
    {
        #region stb
        base.OnAircraftCutLimb(missionNumber, shortName, aircraft, initiator, limbName);
        try
        {
        
            StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 782 }); //player limb cut 782
            bool willReportCutLimb = false;
            if (initiator != null && aircraft != null)
            {
                if (initiator.Player != null)
                {
                    if (aircraft.Army() == 1 && initiator.Player.Army() == 2) { willReportCutLimb = true; }
                    if (aircraft.Army() == 2 && initiator.Player.Army() == 1) { willReportCutLimb = true; }
                }
            }
            if (willReportCutLimb)
            {
                if (((int)limbName > 0) && ((int)limbName < 121))
                {
                    StbStatTask sst = new StbStatTask(StbStatCommands.CutLimb, initiator.Player.Name(), new int[] { (int)limbName });
                    stb_StatRecorder.StbSr_EnqueueTask(sst);
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        #region stb
        base.OnActorDead(missionNumber, shortName, actor, damages);
        try
        {
            AiAircraft aiAircraft = actor as AiAircraft;
            if (aiAircraft != null)
            {
                //So, this seems to report 1 kill for everyone who had any damage at all on the kill?
                foreach (DamagerScore ds in damages)
                {
                    bool willReportDead = false;
                    if (ds.initiator != null)
                    {
                        if (ds.initiator.Player != null)
                        {
                            if (aiAircraft.Army() == 1 && ds.initiator.Player.Army() == 2) { willReportDead = true; }
                            if (aiAircraft.Army() == 2 && ds.initiator.Player.Army() == 1) { willReportDead = true; }
                        }
                    }
                    if (willReportDead)
                    {
                        StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 1 });
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                }
                
                  // if (player.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
                  //p = player.Place() as AiAircraft;
                  
                  
                  //check if a player is in any of the "places"
                  for (int i = 0; i < aiAircraft.Places(); i++)
                  {
                    //if (aiAircraft.Player(i) != null) return false;
                    if ( aiAircraft.Player(i) is Player && aiAircraft.Player(i) != null && aiAircraft.Player(i).Name() != null ) {
                      string playerName=aiAircraft.Player(i).Name();                  
                      //StbSr_UpdateStatsForKilledPlayer(playerName);
                       
                      //GamePlay.gpLogServer(null, "Player killed; updating stats: " + playerName, new object[] { });
                                            
                      StbStatTask sst1 = new StbStatTask(StbStatCommands.PlayerKilled, playerName, new int[] { });
                      stb_StatRecorder.StbSr_EnqueueTask(sst1);
                      
                      
                      //And now that they have died, they can't continue their mission
                      //for stats purposes
                      stb_ContinueMissionRecorder.StbCmr_SavePositionDied (playerName);  
                              
                      
                    }    
                  }
                                                                  
                 
            }
            else
            {
                AiGroundActor aiGroundActor = actor as AiGroundActor;
                if (aiGroundActor != null)
                {
                    if (shortName.Length == 12) { Timeout(99.9, () => { Stb_DestroyFrontShip(aiGroundActor); }); }
                    else if (shortName.Length == 14) { Timeout(99.9, () => { Stb_DestroyFrontArmor(aiGroundActor); }); }

                    foreach (DamagerScore ds in damages)
                    {
                        bool willReportDead = false;
                        if (ds.initiator != null)
                        {
                            if (ds.initiator.Player != null)
                            {
                                if (aiGroundActor.Army() == 1 && ds.initiator.Player.Army() == 2) { willReportDead = true; }
                                if (aiGroundActor.Army() == 2 && ds.initiator.Player.Army() == 1) { willReportDead = true; }
                            }
                        }
                        if (willReportDead)
                        {
                            if (aiGroundActor.Type() == AiGroundActorType.AAGun ||
                                aiGroundActor.Type() == AiGroundActorType.Artillery ||
                                aiGroundActor.Type() == AiGroundActorType.Tank)
                            {
                                StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 2 });
                                stb_StatRecorder.StbSr_EnqueueTask(sst);
                            }
                            else if (aiGroundActor.Type() == AiGroundActorType.ShipBattleship ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipCarrier ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipCruiser ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipDestroyer ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipMisc ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipSmallWarship ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipSubmarine ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipTransport)
                            {
                                StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 3 });
                                stb_StatRecorder.StbSr_EnqueueTask(sst);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }
    
    //For various overrides below, we use these codes to collect info
          // 770 Takeoff 
          // 771 Safe Landing 
          // 772 Crash Landing
          // 773 Health Damaged
          // 774 Parachute Failed
          // 775 Parachute Landing
          // 776 Player Connected
          // 777 Player Disconnected
          // 778 Player Death
          // 779 Missions Count (took off & flew for at least 5 minutes) //TODO
          // 780 Player moved
          // 781 Player a/c damage (number of times)
  	public override void OnAircraftCrashLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
		  base.OnAircraftCrashLanded (missionNumber, shortName, aircraft);              
      StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 772 });
      
      stb_ContinueMissionRecorder.StbCmr_SaveOnCrashLanded (aircraft);
                 
	  }

    public override void OnAircraftLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        
        StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 771 });    
        
    }
    
    public override void OnPersonHealth(maddox.game.world.AiPerson person, maddox.game.world.AiDamageInitiator initiator, float deltaHealth)
    {
        #region stats
        base.OnPersonHealth(person, initiator, deltaHealth);
        try
        {
            Player player = person as Player;
            if (deltaHealth>0 && player != null && player.Name() != null) {
              StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 773 });
              stb_StatRecorder.StbSr_EnqueueTask(sst1);
            }  
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonHealth - Exception: " + ex);
        }
        #endregion

    }

    public override void OnPersonParachuteFailed(maddox.game.world.AiPerson person)
    {
        #region stats
        base.OnPersonParachuteFailed(person);
        try
        {
            Player player = person as Player;            
            if (player != null && player.Name() != null) {
              StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 774 });
              stb_StatRecorder.StbSr_EnqueueTask(sst1);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonParachuteFailed - Exception: " + ex);
        }
        #endregion
    }

    public override void OnPersonParachuteLanded(maddox.game.world.AiPerson person)
    {
        #region stats
        base.OnPersonParachuteLanded(person);
        try
        {
            Player player = person as Player;
            if (player != null && player.Name() != null) {
              StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 775 });
              stb_StatRecorder.StbSr_EnqueueTask(sst1);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonParachuteLanded - Exception: " + ex);
        }
        #endregion
    }

    public override void OnPlayerArmy(maddox.game.Player player, int army)
    {
        #region stats
        base.OnPlayerArmy(player, army);
        try
        {
        
           //783 Joined Red army (Army 1)
           //784 Joined Blue army (Army 2)
           //785 Joined Army 3
           //786 Joined Army 4

           if (army >=1 && army <= 4) {
             int cmd=782 + army;  //1= red, 2=blue, 3 & 4 might be used sometimes?
             StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { cmd });
             stb_StatRecorder.StbSr_EnqueueTask(sst1);
           
           
           }
                        
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPlayerArmy - Exception: " + ex);
        }
        #endregion

    } 

    
    public override void OnPlayerConnected(maddox.game.Player player)
    {
        #region stats
        base.OnPlayerConnected(player);
        try
        {
            System.Console.WriteLine("OnPlayerConnected");
            StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 776 });
            stb_StatRecorder.StbSr_EnqueueTask(sst1);
            
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPlayerConnected - Exception: " + ex);
        }

        #endregion
        // Your code here
    }
    
    
    public override void OnPlayerDisconnected(maddox.game.Player player, string diagnostic)
    {
        #region stats
        base.OnPlayerDisconnected(player, diagnostic);
        try
        {
            System.Console.WriteLine("OnPlayerDisconnected");            
            StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 777 });
            stb_StatRecorder.StbSr_EnqueueTask(sst1);

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPlayerDisconnected - Exception: " + ex);
        }

        #endregion
        // Your code here
    }    
    
    public virtual void OnPersonMoved(maddox.game.world.AiPerson person, maddox.game.world.AiActor fromCart, int fromPlaceIndex){
        base.OnPersonMoved(person, fromCart, fromPlaceIndex);
        try
        {
            Player player = person as Player;
            if (player != null && player.Name() != null) {
              StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 780 });
              stb_StatRecorder.StbSr_EnqueueTask(sst1);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonMoved - Exception: " + ex);
        }
    
    }  

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        #region stats
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        try
        {
            //770 = take off
            StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 770 });
            
            for (int i = 0; i < aircraft.Places(); i++)
            {
              //if (aiAircraft.Player(i) != null) return false;
              if ( aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null ) {
                string playerName=aircraft.Player(i).Name();                  
                Console.WriteLine ("Took Off " + playerName);
                
                //If this is not a continuation mission, we increment the mission counter for this player                                 
                if ( ! stb_ContinueMissionRecorder.StbCmr_OnTookOff(playerName))    
                {
                   
                   Console.WriteLine ("New Mission for " + playerName);
                   StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 779 });
                   stb_StatRecorder.StbSr_EnqueueTask(sst1);
                };    
                
              }    
            }

            
            //779 = Mission 
            
            //check if this is a continuation of the previous flight from near the
            //same position the a/c previously landed etc.  If so, it's not a "new mission" but a continuation of the previous mission, so don't increment the mission counter.  If not, we increment the mission counter.
                              
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnAircraftTookOff - Exception: " + ex);
        }
        #endregion
        //add your code here
    }
	  	

    


    public override void OnBattleStoped()
    {
        #region
        base.OnBattleStoped();
        try
        {
            if (stb_LogStats)
            {
                StbStatTask sst = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 0 });
                stb_StatRecorder.StbSr_EnqueueTask(sst);
            }
            stb_StatRecorder.StbSr_FinishWaitingTasks();
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

}

//Various helpful calculations, formulas, etc.
public static class Calcs
{
//Various public/static methods
    //http://stackoverflow.com/questions/6499334/best-way-to-change-dictionary-key    
    
    public static bool changeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey oldKey, TKey newKey)
    {
      TValue value;
      if (!dict.TryGetValue(oldKey, out value))
          return false;
    
      dict.Remove(oldKey);  // do not change order
      dict[newKey] = value;  // or dict.Add(newKey, value) depending on ur comfort
      return true;
    }
    
    public static string escapeColon(string s) {
     return s.Replace("##", "##*").Replace(":", "##@");
    }
    
    public static string unescapeColon(string s) {
     return s.Replace("##@", ":").Replace("##*", "##");
    }
    
    public static string escapeSemicolon(string s) {
     return s.Replace("%%", "%%*").Replace(";", "%%@");
    }
    
    public static string unescapeSemicolon(string s) {
     return s.Replace("%%@", ";").Replace("%%*", "%%");
    }            
        
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
}   
    
    
