#define DEBUG  
#define TRACE  
//$reference System.Core.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference parts/core/CloDMissionCommunicator.dll

//$reference parts/core/TacviewRecorder.dll 
using TacviewRecorder;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;
using part;
using System.Text.RegularExpressions;

using TWCComms;
using System.Media;


//CoverMission covermission = new CoverMission();
public class TacviewImplMission : TacviewMission
//public class TacviewImplMission : AMission
{

    public Mission mainmission;
	public SupplyMission supplymission;    
    public Random ran;

	
    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    //public enum ArmiesE { None, Red, Blue };
	
	static public List<string> ArmiesSection = new List<string>() { "nn", "gb", "de" }; //armies as needed in section files ie for ground stationaries	
	
	//to match :TacviewMission's constructor, hopefully...
	public TacviewImplMission()
    {}

    public TacviewImplMission(Mission msn)
    {
        try
        {

            Console.WriteLine("-TacviewImpl.cs starting . . . ");
            mainmission = msn; 
            ran = new Random();

            MissionNumberListener = -1;
			
			//DestinationFolder = mainmission.CLOD_PATH + mainmission.FILE_PATH + "/tacview";
			
			//TypeOfMission.BigMission = Aircraft + ground units + static objects. 
			//TypeOfMission.DogFight = Aircraft only.   )Note DogFight w/ F rather than f, different from documentation.
			//TypeOfMission.Normal = Aircraft + ground units. 
			//The default mission type is BigMission 
			//MissionType = TypeOfMission.DogFight;
			
			//ShowPlayer = true; 
			//ShowPlayerAsHuman = false; 
			
			//Value = 0 : recorder starts immediately 
			//Value > 0: starts x second(s) after the mission is loaded.  
			//Value = -1: the recorder does not start automatically and must be started using the 
			//StartRecorder() function. 
			//The timer can be cancelled by calling the StartRecorder() function, which cancels the 
			//timed start and starts the recorder immediately.  
			//The StopRecorder() method cancels the timed start and permanently stops the recorder.  
			//The PauseRecorder() method has no effect on the timed start.
			//DisableRecorder() Prevents the recorder from starting up.  
			//Allows you to run a mission without taking the recorder into account 
			//and without having to change the mission's base class.
			//StartDelay = 300; // Start in 300 seconds 
			//ZipFinalFile = true; // compress the file 
				
            
			//base.~();
			//base.TacviewMission();
            Console.WriteLine("-TacviewImplMission.cs successfully constructed");
        }
        catch (Exception ex) { Console.WriteLine("TacviewImpl Mission() ERROR: " + ex.ToString()); }
    }

    public override void Init(ABattle b, int missionNumber)
    {
        try
        {  
			Console.WriteLine("-TacviewImplMission.cs starting init()...");

            MissionNumberListener = -1;
			supplymission = mainmission.supplymission; //if supplymission is initialized a bit after statsmission, and we do this in the class initializer, then this would be null, so we wait and do it here instead.
			
			
			//DestinationFolder = mainmission.CLOD_PATH + mainmission.FILE_PATH + "/tacview";
			
			//TypeOfMission.BigMission = Aircraft + ground units + static objects. 
			//TypeOfMission.DogFight = Aircraft only.   )Note DogFight w/ F rather than f, different from documentation.
			//TypeOfMission.Normal = Aircraft + ground units. 
			//The default mission type is BigMission 
			//MissionType = TypeOfMission.DogFight;
			
			//ShowPlayer = true; 
			//ShowPlayerAsHuman = false; 
			
			//Value = 0 : recorder starts immediately 
			//Value > 0: starts x second(s) after the mission is loaded.  
			//Value = -1: the recorder does not start automatically and must be started using the 
			//StartRecorder() function. 
			//The timer can be cancelled by calling the StartRecorder() function, which cancels the 
			//timed start and starts the recorder immediately.  
			//The StopRecorder() method cancels the timed start and permanently stops the recorder.  
			//The PauseRecorder() method has no effect on the timed start.
			//DisableRecorder() Prevents the recorder from starting up.  
			//Allows you to run a mission without taking the recorder into account 
			//and without having to change the mission's base class.
			//StartDelay = 0; // Start in 300 seconds 
			//ZipFinalFile = true; // compress the file 
			
			//AddWaypoint(name, x, y, z, army= 0) 
			//RemoveWaypoint(name)
			
			//AddBookmark(message)  //don't use AddBookemark in inited but throughout the  mission
			
			
			
            Console.WriteLine("-TacviewImplMission.cs successfully ran init()");

        }
        catch (Exception ex) { Console.WriteLine("TacviewImpl Mission(): " + ex.ToString()); }
		base.Init(b, missionNumber);
    }
	
	public override void Inited()
    {		
        try
        {
			Console.WriteLine("TacviewImpl starting Inited . . .");
			DestinationFolder = mainmission.CLOD_PATH + mainmission.FILE_PATH + "/tacview";
			
			//TypeOfMission.BigMission = Aircraft + ground units + static objects. 
			//TypeOfMission.DogFight = Aircraft only.   )Note DogFight w/ F rather than f, different from documentation.
			//TypeOfMission.Normal = Aircraft + ground units. 
			//The default mission type is BigMission 
			//MissionType = TypeOfMission.DogFight;
			MissionType = TacviewMission.TypeOfMission.DogFight;
			//TacviewCore.missionType = TacviewMission.TypeOfMission.DogFight;
			
			ShowPlayer = true; 
			ShowPlayerAsHuman = false; 
			
			//Value = 0 : recorder starts immediately 
			//Value > 0: starts x second(s) after the mission is loaded.  
			//Value = -1: the recorder does not start automatically and must be started using the 
			//StartRecorder() function. 
			//The timer can be cancelled by calling the StartRecorder() function, which cancels the 
			//timed start and starts the recorder immediately.  
			//The StopRecorder() method cancels the timed start and permanently stops the recorder.  
			//The PauseRecorder() method has no effect on the timed start.
			//DisableRecorder() Prevents the recorder from starting up.  
			//Allows you to run a mission without taking the recorder into account 
			//and without having to change the mission's base class.
			StartDelay = 0; // Start in 300 seconds 
			ZipFinalFile = true; // compress the file 
			
			//AddWaypoint(name, x, y, z, army= 0) 
			//RemoveWaypoint(name)
			
			//AddBookmark(message)  //don't use AddBookemark in inited but throughout the  mission
			
			Console.WriteLine("TacviewImpl Inited . . .");
			

        }
        catch (Exception ex) { Console.WriteLine("TacviewImpl Inited ERROR: " + ex.Message); };
		base.Inited();
    }
	
	public virtual void OnBattleInit()
    {
      try
      {
		Console.WriteLine("TacviewImpl OnBattleInit() starting . . .");

			//DestinationFolder = mainmission.CLOD_PATH + mainmission.FILE_PATH + "/tacview";
			
			//TypeOfMission.BigMission = Aircraft + ground units + static objects. 
			//TypeOfMission.DogFight = Aircraft only.   )Note DogFight w/ F rather than f, different from documentation.
			//TypeOfMission.Normal = Aircraft + ground units. 
			//The default mission type is BigMission 
			//MissionType = TypeOfMission.DogFight;
			
			//ShowPlayer = true; 
			//ShowPlayerAsHuman = false; 
			
			//Value = 0 : recorder starts immediately 
			//Value > 0: starts x second(s) after the mission is loaded.  
			//Value = -1: the recorder does not start automatically and must be started using the 
			//StartRecorder() function. 
			//The timer can be cancelled by calling the StartRecorder() function, which cancels the 
			//timed start and starts the recorder immediately.  
			//The StopRecorder() method cancels the timed start and permanently stops the recorder.  
			//The PauseRecorder() method has no effect on the timed start.
			//DisableRecorder() Prevents the recorder from starting up.  
			//Allows you to run a mission without taking the recorder into account 
			//and without having to change the mission's base class.
			//StartDelay = 300; // Start in 300 seconds 
			//ZipFinalFile = true; // compress the file 
			
			//AddWaypoint(name, x, y, z, army= 0) 
			//RemoveWaypoint(name)
			
			//AddBookmark(message)  //don't use AddBookemark in inited but throughout the  mission
			
			Console.WriteLine("TacviewImpl OnBattleInit() complete . . .");
        base.OnBattleInit();
      }
      catch (Exception ex)
      {
        Console.WriteLine("TacviewImpl OnBattleInit() ERROR: " + ex.ToString()); 
      }
    }
	
	
	
	
	public void AddTacviewBookmark (string message = "") {
		AddBookmark(message);		
	}


    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {

        base.OnPlaceEnter(player, actor, placeIndex);
        //startKnickebein(player);

    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
		
		Console.WriteLine("TacviewImpl OnBattleStarted . . .");
    }

    int stb_lastMissionLoaded = -1;
	//MissionNumberListener = MissionNumber;

    public override void OnMissionLoaded(int missionNumber)
    {
        

        try
        {
			MissionNumberListener = MissionNumber;



            stb_lastMissionLoaded = missionNumber;
			
			Console.WriteLine("-TacviewImplMission OnMissionLoaded() {0} {1} ", missionNumber, MissionNumber);


            if (missionNumber == MissionNumber)

            {
                Console.WriteLine("-TacviewImplMission OnMissionLoaded() {0} {1} ", missionNumber, MissionNumber);

                if (GamePlay != null && GamePlay is GameDef)
                {
                    //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                    (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("TacfiewImpl OnMissionLoaded() ERROR: " + ex.ToString()); }
		
		base.OnMissionLoaded(missionNumber);
    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();
		
		



        if (GamePlay != null && GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
		Console.WriteLine("TacviewImpl OnBattleStoped, EventChat disconnected . . .");

    }
    
    public override void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        
        if (actor as AiAircraft != null) base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);

        
    }
	
	public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        if (actor as AiAircraft != null) base.OnActorDead(missionNumber, shortName, actor, damages);

       
    }
	
	public override void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
    {
        if (actor as AiAircraft != null) base.OnActorDestroyed(missionNumber, shortName, actor);

         
    }
	
	public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        if (actor as AiAircraft != null) base.OnActorCreated(missionNumber, shortName, actor);

       
    }

    public override void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    {
       base.OnStationaryKilled(missionNumber, stationary, initiator, eventArgInt); 
    }
    

    public override void OnBombExplosion(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {

        base.OnBombExplosion(title, mass_kg, pos, initiator, eventArgInt);
            
    }



    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);

       
    }

    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        
    }
    public override void OnAircraftKilled(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftKilled(missionNumber, shortName, aircraft);
        
    }

    


    /****************************************************************
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
        if (msg.StartsWith("<tmes"))
        {
            Console.WriteLine("Adding message to Tacview...");
			mainmission.twcLogServer(new Player[] { player }, "Adding your message to Tacview...");            

            
			string ms = msg.Substring(5).Trim();
			AddBookmark(ms);

 
        }
				

  
        else if (msg.StartsWith("<tachelp"))
        {
            string msg42 = "TACVIEW RECORDER HELP";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "Tacview is installed and recording this mission!";
			GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
			msg42 = "<tmes I want to save this message";
			GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
			msg42 = "will put your message in the Tacview file at that point in time.";
			GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
			msg42 = "Thanks to FlyBy for creating the Tacview Recorder!";
			GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            
        }

        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1.6; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 7.2;

            string msg41 = "<tachelp - info about Tacview recording of this mission";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
    }

   

} //end class


