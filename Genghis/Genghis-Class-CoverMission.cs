#define DEBUG  
#define TRACE  
//$reference System.Core.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference parts/core/CloDMissionCommunicator.dll
using System;
using System.IO;
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
 


/*   >>>  READ THIS FIRST:  Genghis-Class-CloDNotes.cs
 *
 *   That file records how CLoD's AI aircraft, waypoints and aircraft parameters actually behave -
 *   how to make AI drop bombs, the ~16s waypoint-overwrite trap, how to detect a player's bomb
 *   release, the S_BombReserve parameter conventions, and the AiAirGroup / AiActor type gotchas.
 *   None of it is documented anywhere by 1C/TF; it was all found by in-game testing and by
 *   reflecting over the game assemblies.  Consult it before changing any waypoint or bomb-release
 *   code in this file.
 */

 /*   TODO:
 *   Instead of <creserver and <cattack how about:
 * XX <cnormal formation - the usual thing, bombers/sturmo stay in formation except when ground attacking, cover stays in  place unless directly attacking/defending
 * XX <cstrict formation - ignore all and just fly in formation
 * XX <cescort - CLoD escort function, for all AC types if assigned this. Basically escort/defend the player
 * XX <cattack - air attack anything reasonably nearby.  Like if attacking a formation of bombers w/ cover
 * XX <cloiter - stay in one place circling
   <cdrop - bombers drop bombs when the player drops (IN PROGRESS - the waypoint recipe that makes
          this work is in Genghis-Class-CloDNotes.cs section 1; the AI only releases when a
          GATTACK_POINT becomes the CURRENT waypoint AND its release point is already behind it)

 * could also do "attack bombers" vs "attack cover"   
 *   xxThey are not climbing above 500 meters for some reason.  Maybe because of the speed restriction on currentposwaypoint? It's baffling.
 *   xxcover A/C catch up OK but seem to speed away once they are in front.
 *   
 *   xxPROBABLY need to put each cover/bomber group in a certain spot a little left/right of the main a/c instead of letting them all fight it out for the same spot
 *   
 *   xxAirgroups larger then 2 seem to split up here & there.  So, we need ot keep track of the split-off airgroups & do something with them to keep track of them.
 *   Right now they are just split off from the airgroup we are keeping track off and then we lose control of it.
 *   
 *      --->>  Except, this line might help with this situation: if (airGroup.motherGroup() != null && coverAircraftAirGroupsActive.ContainsKey(airGroup.motherGroup()))
 *  	--->> And, this routine: CheckSplits
 * 
 *   xxDoesn't auto-send all aircraft back to stock when battle ends.  (Because of delay after player leaves game to allow bombing runs to continue. OnBattleStoped doesn't do it because it comes along too late.)
 *   xxNeeds to register function landAllCoverAircraft(); with TWCComs and then call it in SaveMapState somewhere before:
 *      if (TWCSupplyMission != null) TWCSupplyMission.SupplyEndMission(redMult, blueMult);
 * 
 * ******************************************/

//A class where we can store various helpful/necessary info about cover a/c that are checked out
public class CoverACInfo
{
    public double MinAttackAlt_m { get; set; } //minimum altitude for this a/c when attacking, to ensure bombs explode.  Applies mostly to Wellington with 2000lb bombs?
       //Note this will still have a small vertical offset from calcOffset_m . . . set MinAttackAlt_m with that in mind.
       //amount is defaultAmtVerticleShift_m (= 20) but this is 3Xed for Wellingtons, then
       //multiplied by the position within the formation on bomb runs.  Could amount to 300-400m?
    public bool Heavy { get; set; } //set to use "heaviest" bombs avail for that a/c
    public bool IsDiveBomber { get; set; }
    public bool HasTorpedos { get; set; }
    public bool HasDepthCharges { get; set; }
    public bool HasBombs { get; set; }
    public bool StartedWithCannons { get; set; }
    public bool HasCannons { get; set; }
    public bool IsHeavyBomber { get; set; }
    public bool IsStrikeAC { get; set; } //fighter-bomber, sturmovik
    public bool IsPlayerStrikeAC { get; set; }
    public bool IsFighter { get; set; }
    public string Formation { get; set; }
    public string PlaneType { get; set; }

    public CoverACInfo()
    {
        MinAttackAlt_m = 0;
        Heavy = false;
        HasTorpedos = false;
        HasDepthCharges = false;
        HasBombs = false;
        StartedWithCannons = false;
        HasCannons = false;
        IsHeavyBomber = false;
        IsStrikeAC = false;
        IsPlayerStrikeAC = false;
        IsFighter = false;
        Formation = "";
        PlaneType = "";
    }
    public CoverACInfo(double minattackalt_m = 0, bool heavy = false, bool isdivebomber = false,
         bool hasdepthcharges = false, bool hasbombs = false, bool startedwithcannons = false, bool hascannons = false, bool isheavybomber = false, bool issturmovik = false, bool isplayersturmovik = false, bool isfighter = false){
        MinAttackAlt_m = minattackalt_m;
        Heavy = heavy;
        IsDiveBomber = isdivebomber;
        HasDepthCharges = hasdepthcharges;
        HasBombs = hasbombs;
        StartedWithCannons = startedwithcannons;
        HasCannons = hascannons;
        IsHeavyBomber = isheavybomber;
        IsStrikeAC = issturmovik;
        IsPlayerStrikeAC = isplayersturmovik;
        IsFighter = isfighter;
        }
}

//CoverMission covermission = new CoverMission();
public class CoverMission : AMission, ICoverMission
{
    public IMainMission TWCMainMission;
    public Mission mainmission;
	public SupplyMission supplymission;
    public ISupplyMission TWCSupplyMission;
    public IStatsMission TWCStatsMission;
    public IStbStatRecorder TWCStbStatRecorder;
    public IKnickebeinMission TWCKnickebeinMission;
    public Random ran;
    public int minimumAircraftRequiredForCoverDuty { get; set; }
    public int maximumAircraftAllowedPerMission_BomberPilots { get; set; }
    public int maximumCheckoutsAllowedAtOnce_BomberPilots { get; set; }
    public int maximumAircraftAllowedPerMission_FighterPilots { get; set; }
    public int maximumCheckoutsAllowedAtOnce_FighterPilots { get; set; }

    public int maximumAircraftAllowedPerMission_FighterPilots_wing { get; set; }
    public int maximumCheckoutsAllowedAtOnce_FighterPilots_wing { get; set; }
    public int maximumAircraftAllowedPerMission_RepairMission { get; set; }
    public int maximumCheckoutsAllowedAtOnce_RepairMission { get; set; }
    public int maximumCheckoutsAllowedAtOnce_FerryMission { get; set; }
	
    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    //public enum ArmiesE { None, Red, Blue };
	
	static public List<string> ArmiesSection = new List<string>() { "nn", "gb", "de" }; //armies as needed in section files ie for ground stationaries	
    
    public int maxPlayersToAllowCover { get; set; } //Number of players online in players' army, above this number no cover will be allowed
    public int numPlayersToReduceCover { get; set; } //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover
    public int numPlayersToIncreaseCover { get; set; } //below this number there are additional cover a/c available.  usually this is small, like = 1, 2, 3 - lower than numPlayersToReduceCover
    public int numPlayersToReduceCheckouts { get; set; } //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
    public int numPlayersToReduceCheckoutsMore { get; set; } //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
    public int numPlayersToReduceCheckoutsEvenMore { get; set; } //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
    //The orders a player can give to their cover airgroups, via chat command (see setCoverAircraftAirGroupsOrders)
    //  normal - the usual behavior: bombers/sturmoviks stay in formation except when ground attacking, cover
    //           stays in place unless directly attacking/defending (this is the default behavior)
    //  follow - hold fire & stay in reserve, joined with the player (no attacking & no bombing)
    //  attack - air attack anything reasonably nearby, plus any bombing the player has ordered
    //  strict - ignore all else & just fly in rigid formation with the player (holds your speed & altitude,
    //           ignores <cdist, & fires only in self defence)
    //  escort - CLoD's ESCORT behavior for all a/c types: stay with & defend the player (no ground bombing)
    //  loiter - stay in one place, circling
    //  drop - "drop when I drop".  Like reserve, they hold their bombs and fly with the leader, but
    //         the moment the leader lets his FIRST bomb go, every bomber in the formation lets go too.
    //         See Genghis-Class-CloDNotes.cs section 1 - making them actually release needs a
    //         NORMFLY + GATTACK_POINT waypoint pair, NOT setTask(ATTACK_GROUND).
    public enum CoverAGOrders {none, reserve, attack, normal, strict, escort, loiter, drop };

    public Dictionary<Player, int> numberCoverAircraftActorsCheckedOutWholeMission = new Dictionary<Player, int>();
    public Dictionary<AiActor, Player> coverAircraftActorsCheckedOut = new Dictionary<AiActor, Player>();
    public Dictionary<AiAirGroup, Player> coverAircraftAirGroupsActive = new Dictionary<AiAirGroup, Player>();
    public Dictionary<Player, int> playerIndex = new Dictionary<Player, int>();
    public Dictionary<Player, Dictionary<int, AiAirGroup>> coverAircraftAirGroupsIndexes = new Dictionary<Player, Dictionary<int, AiAirGroup>>();
    public Dictionary<Player, Dictionary<Tuple<int, AiActor>, AiAirGroup>> coverAircraftActorsIndexes = new Dictionary<Player, Dictionary<Tuple<int, AiActor>, AiAirGroup>>();
    public Dictionary<AiAirGroup, Point3d> coverAircraftAirGroupsTargetPoint = new Dictionary<AiAirGroup, Point3d>();
    public Dictionary<AiAirGroup, CoverAGOrders> coverAircraftAirGroupsOrders = new Dictionary<AiAirGroup, CoverAGOrders>();
    public Dictionary<AiAirGroup, CoverACInfo> coverACInfo = new Dictionary<AiAirGroup, CoverACInfo>();
    public Dictionary<Player, Tuple<Point3d, DateTime>> PBP_playerBombPoint = new Dictionary<Player, Tuple<Point3d, DateTime>>(); //Last point player has bombed, along with the time it was set, which can be used to target the cover bombers
    public Dictionary<Player, BAM_BombAimMode> BAM_playerAimMode = new Dictionary<Player, BAM_BombAimMode>(); //What Cover Bomber Aim Mode the player has selected

    public Dictionary<AiAirGroup, bool> coverAircraftAirGroupsReleased = new Dictionary<AiAirGroup, bool>(); //When pilots die, bombers can continue to attack for 5mins or so more; this sets the time to release them

    //Map boundaries - these should match what you set in the .mis file; these are the values that work with TWC radar etc
    //double twcmap_minX = 10000;  //orig values.
    //double twcmap_minY = 10000;
    //double twcmap_maxX = 360000;
    //double twcmap_maxY = 310000;
    //TODO: One variable in MainMission that sets these values for all subclasses
    double twcmap_minX = 6666; //working on player off map penalties, we expanded the boundaries just slightly to match what is shown on in-game and radar maps
    double twcmap_minY = 6666; //This should match what is in -main.cs AND -stats.cs OR SCREWUPS ensue.
    double twcmap_maxX = 362000;
    double twcmap_maxY = 312000;
    /*
     *  double minX = 6666; //from -main.cs
        double minY = 6666;
        double maxX = 362000;
        double maxY = 312000;
     * 
     * */

    public CoverMission(Mission msn)
    {
        try
        {

            Console.WriteLine("-cover.cs starting . . . ");
            mainmission = msn; //getting instance of mainmission via constructor
			//Timeout(10, () => {supplymission = mainmission.supplymission;}); //if supplymission is initialized a bit after statsmission then this would be null, so wait a bit...
            TWCMainMission = TWCComms.Communicator.Instance.Main;
            TWCComms.Communicator.Instance.Cover = (ICoverMission)this; //allows -stats.cs to access this instance of Mission     

			//CheckSplits_Timer_init();  //CheckSplits is causing error/program exit for now. 2026/08

            //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
            ran = new Random();

            MissionNumberListener = -1;
            minimumAircraftRequiredForCoverDuty = 5; //2020-01; was 200 //2021-06; was 50 //2021-11 was 25, now 5
            maximumAircraftAllowedPerMission_BomberPilots = 20; //2020-01; was 6, then 10.  Then 20 when missions were 6 hrs/ 6/2021 upping to 32 now that missions = 10 hrs
                                                                //maximumAircraftAllowedPerMission_BomberPilots = 136; //for testing        
            maximumCheckoutsAllowedAtOnce_BomberPilots = 10;     //this was flights when flights were set to 2, but now is aircraft (the # of a/c per flight can be set per user)

            maximumAircraftAllowedPerMission_FighterPilots = 10; //For fighter pilots, bombers allowed.  was 8 when msn was 6 hrs, 6/2021 changing to 14
                                                                 //maximumAircraftAllowedPerMission_FighterPilots = 136; //for testing        
            maximumCheckoutsAllowedAtOnce_FighterPilots = 4;    //this was flights when flights were set to 2, but now is aircraft (the # of a/c per flight can be set per user)

            maximumAircraftAllowedPerMission_FighterPilots_wing = 5; //For fighter pilots, bombers allowed.  was 8 when msn was 6 hrs, 6/2021 changing to 14
                                                                 //maximumAircraftAllowedPerMission_FighterPilots = 136; //for testing        
            maximumCheckoutsAllowedAtOnce_FighterPilots_wing = 1;    //this was flights when flights were set to 2, but now is aircraft (the # of a/c per flight can be set per user)

            maximumAircraftAllowedPerMission_RepairMission = 70;

            maximumCheckoutsAllowedAtOnce_RepairMission = 15; //This is for players flying cargo a/c to repair an airport or radar OR flying ferry with replacement aircraft to forward fields.
            maximumCheckoutsAllowedAtOnce_FerryMission = 20; //This is for players flying cargo a/c to repair an airport or radar OR flying ferry with replacement aircraft to forward fields.

            maxPlayersToAllowCover = 40; //2020-01; was 12 //Number of players online in players' army, above this number no cover will be allowed
            numPlayersToReduceCover = 20; //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
            numPlayersToIncreaseCover = 12; //This number of players online in players' army OR FEWER, the number of allowed cover per mission will be increased even more;  Should be equal or less than maxPlayersToAllowCover or else ##errors##            

            numPlayersToReduceCheckouts = 7; //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
            numPlayersToReduceCheckoutsMore = 12; //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
            numPlayersToReduceCheckoutsEvenMore = 24; //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##

            Console.WriteLine("-cover.cs successfully constructed");
        }
        catch (Exception ex) { Console.WriteLine("Cover Mission(): " + ex.ToString()); }
    }

    public override void Init(ABattle b, int missionNumber)
    {
        try
        {
            base.Init(b, missionNumber);

            MissionNumberListener = -1;
			supplymission = mainmission.supplymission; //if supplymission is initialized a bit after statsmission, and we do this in the class initializer, then this would be null, so we wait and do it here instead.
            Console.WriteLine("-cover.cs successfully inited");

        }
        catch (Exception ex) { Console.WriteLine("Cover Mission(): " + ex.ToString()); }
    }


    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {

        base.OnPlaceEnter(player, actor, placeIndex);
        //startKnickebein(player);

    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
    }

    //ToDO: make a similar list of all LIVE/VALID static actors. This will be MUCH smaller than
    //allStaticActors, which will help some things like searching for nearest target or whatever
    public AiActor[] allStaticActors = null;
    public object allStaticActors_lock = new object();

    Dictionary<string, IMissionObjective> SMissionObjectivesList = new Dictionary<string, IMissionObjective>();

    private void renewAllStaticActors_recurs(bool onetime = false)
    {
        if (!onetime) Timeout(3 * 60.3624, () => renewAllStaticActors_recurs());
        //OK, this is DEFINITELY one of the BIG causes of WARP.
        //Hopefully doing it via Task.Run will help it a lot.
        Task.Run(() =>
        {
            try
            {
                if (mainmission.panic() && ran.Next(2) == 0) return; //if in panic mode cut runs by 50%

                //if (TWCComms.Communicator.Instance.WARP_CHECK || mainmission.ON_TESTSERVER) Console.WriteLine("CVSAXX1-1 " + DateTime.UtcNow.ToString("mm:ss.fffffff")); //Testing for potential causes of warping

                Console.WriteLine("renewAllStaticActors running . . . ");
                //OK, we were missing some actors since our custom gpGetGroundActors only gets actors named "static1002" and similar.
                HashSet<AiActor> asa = new HashSet<AiActor>(CoverCalcs.gpGetAllGroundActors(this, stb_lastMissionLoaded));
                HashSet<AiActor> gga = new HashSet<AiActor>(CoverCalcs.gpGetGroundActors(this, army: 0));
                if (asa == null) asa = gga;
                if (gga!= null) asa.UnionWith(gga);
                if (asa == null & gga == null) return;//nothing to do...
                lock (allStaticActors_lock)
                {
                    allStaticActors = asa.ToArray();
                }

                Console.WriteLine("renewAllStaticActors running: {0:n0} found (asa: {1}, gga: {2})", allStaticActors.Length, asa.Count, gga.Count);
                //gpGetGroundActors(CoverMission msn, int army)
                //allStaticActors = CoverCalcs.gpGetAllGroundActors(this, stb_lastMissionLoaded);
                SMissionObjectivesList = TWCMainMission.SMissionObjectivesList();

                Console.WriteLine("renewAllStaticActors complete . . . ");
                //if (TWCComms.Communicator.Instance.WARP_CHECK || mainmission.ON_TESTSERVER) Console.WriteLine("CVSAXX1-2 " + DateTime.UtcNow.ToString("mm:ss.fffffff")); //Testing for potential causes of warping
            }
            catch (Exception ex) { Console.WriteLine("cover: renewAllStaticActors ERROR: " + ex.ToString()); }
        });
    }

    private void checkPlayersCoverACDisappeared_recurs()
    {
        Task.Run(() =>
        {
            if (GamePlay == null) return;
            Timeout(2.125432 * 60, () => checkPlayersCoverACDisappeared_recurs());
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("CVXX2 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping
            foreach (Player player in GamePlay.gpRemotePlayers()) checkPlayerAirgroups(player);
        });
    }

    //Returns an objective point & radius that point p lies within.
    //If it lies within mroe than one objective, it chooses the objective with the smallest radius to return
    private Tuple<Point3d?, double> ObjectivesRadius_m(Point3d p) //center point, radius
    {
        double r = 1000000000;
        Tuple<Point3d?, double> ret = new Tuple<Point3d?, double>(null, 0);
        foreach (Mission.MissionObjective mo in mainmission.MissionObjectivesList.Values)
        {            
            if (CoverCalcs.CalculatePointDistance(mo.returnCurrentPosWithChief(), p) < mo.radius && mo.radius <= r)
            {
                double rad = mo.radius;
                if (mo.TriggerDestroyRadius > rad) rad = mo.TriggerDestroyRadius;
                ret = new Tuple<Point3d?, double>(mo.Pos, rad);
                r = mo.radius;
            }
        }
        return ret;
    }

    int stb_lastMissionLoaded = -1;

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        try
        {


            TWCSupplyMission = TWCComms.Communicator.Instance.Supply;

            TWCStatsMission = TWCComms.Communicator.Instance.Stats;
            if (TWCStatsMission != null) TWCStbStatRecorder = TWCStatsMission.stb_IStatRecorder;

            TWCKnickebeinMission = TWCComms.Communicator.Instance.Knickebein;

            stb_lastMissionLoaded = missionNumber;


            if (missionNumber == MissionNumber)

            {
                Console.WriteLine("-cover OnMissionLoaded() {0} {1} ", missionNumber, MissionNumber);

                Timeout(61.399, () => AddOffMapAIAircraftBackToSupply_recur());

                Timeout(21.546, () => setCoverAircraftCurrentlyAvailable_recurs());

                Timeout(20.436, () => renewAllStaticActors_recurs());

                Timeout(240.59234, () => checkPlayersCoverACDisappeared_recurs());

                //Timeout(10, () => checkPlayersCoverACDisappeared_recurs()); //testings

                if (GamePlay != null && GamePlay is GameDef)
                {
                    //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                    (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover OnMissionLoaded(): " + ex.ToString()); }
    }

    public override void OnActorTaskCompleted(int missionNumber, string shortName, maddox.game.world.AiActor actor)
    {
        base.OnActorTaskCompleted(missionNumber, shortName, actor);
        Console.WriteLine("OnActorTaskCompleted: {0} {1} {2} complete: {3}", missionNumber, shortName, actor.Name(), actor.IsTaskComplete());
        if (actor as AiAirGroup != null) Console.WriteLine("OnActorTaskCompleted2: {0} ", (actor as AiAirGroup).getTask());
    }


    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        //Send all cover/bomber a/c back to stock/supply
        try
        {
            landAllCoverAircraft();
        }
        catch (Exception ex) { Console.WriteLine("Cover OnBattleStoped: " + ex.ToString()); }

        try
        {
            foreach (Player player in COVER_ListPositionTimer.Keys) if (COVER_ListPositionTimer[player] != null) COVER_ListPositionTimer[player].Dispose();
        }
        catch (Exception ex) { Console.WriteLine("Cover OnBattleStoped2: " + ex.ToString()); }

        try
        {
            foreach (Player player in COVER_DropWatchTimer.Keys) if (COVER_DropWatchTimer[player] != null) COVER_DropWatchTimer[player].Dispose();
            COVER_DropWatchTimer.Clear();
        }
        catch (Exception ex) { Console.WriteLine("Cover OnBattleStoped3: " + ex.ToString()); }
		
		if (COVER_CheckSplits_Timer != null) COVER_CheckSplits_Timer.Dispose();

        if (GamePlay != null && GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }

    }

    /*************************************************************
     //BAM - Player Bomb Aim Mode
     //
     //Sets the player's bomb aim mode
     //Defaults to (none)
     //
     //    
     //
     ***************************************************************************************************************/

    //<cdrop - "DROP WHEN I DROP" lives in this cycle too, so it sits on the Tab-4-4-4-4-6 "Cover
//Targeting" menu right alongside Knickebein Point / Bomb Explosion Point / etc, where it belongs -
//it is a bombing mode, not a formation directive.  It is deliberately the ONLY value here that
//matches none of the BAM_is* predicates: those all answer "WHERE do they bomb", whereas this one
//answers "WHEN do they bomb", so it drives the airgroup ORDERS instead (see BAM_enterDropMode()).
//Keep it immediately before None so the existing cycle order (None -> Knickebein -> ... -> Flare)
//is unchanged and everybody's muscle memory still works.
public enum BAM_BombAimMode { Knickebein_Point, Nearest_Enemy_to_Knickebein_Point, Bomb_Explosion_Point, Nearest_Enemy_to_Bomb_Explosion, Drop_Flare_Point_Here_and_Target_it, Nearest_Enemy_to_Flare_Point, Drop_When_I_Drop, None };

    public bool BAM_isBombPoint(Player player)
    {
        if (player == null) return false;
        return (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Bomb_Explosion_Point || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Bomb_Explosion);
    }

    public bool BAM_isMyPositionPoint(Player player)
    {
        if (player == null) return false;
        return (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Drop_Flare_Point_Here_and_Target_it || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Flare_Point);
    }
    public bool BAM_isKnickebeinPoint(Player player)
    {
        if (player == null) return false;
        return (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Knickebein_Point || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Knickebein_Point);
    }
    public bool BAM_isNearestEnemy(Player player)
    {
        if (player == null) return false;
        return (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Bomb_Explosion || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Knickebein_Point
            || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Flare_Point);
    }
    public bool BAM_isPoint(Player player)
    {
        if (player == null) return false;
        return (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Bomb_Explosion_Point || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Knickebein_Point
            || BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Drop_Flare_Point_Here_and_Target_it);
    }

    public BAM_BombAimMode BAM_getplayerBombAimMode_enum(Player player)
    {
        if (BAM_playerAimMode.ContainsKey(player)) return BAM_playerAimMode[player];
        //else return BAM_BombAimMode.Knickebein_Point;  //KnIckebein_Point is the default aiming mode
        if (player == null) return BAM_BombAimMode.None;
        BAM_playerAimMode[player] = BAM_BombAimMode.None;
        return BAM_playerAimMode[player];  //NONE - ie, turned off - is the default aiming mode
    }

    public string BAM_getPlayerBombAimMode_string(Player player)
    {
        if (player == null) return "(none)";
        BAM_BombAimMode bam = BAM_getplayerBombAimMode_enum(player);
        if (bam == BAM_BombAimMode.None) return "(no ground target)";
        else return (bam.ToString().Replace('_', ' '));
    }

    public string BAM_resetBombAimMode(Player player)  //resets the mode to "none" with message
    {
        if (player == null) return "(none)";
        BAM_playerAimMode[player] = BAM_BombAimMode.None;
        return "(none; follow you)";

        Timeout(4.3, () =>
        {
            if (GamePlay == null) return;
            string m = "COVER: Your cover airgroup targeting mode reset to '(none; follow you)'";
            GamePlay.gpLogServer(new Player[] { player }, m, null);
        });

    }

    //Whenever an airgroup is dropped from coverAircraftAirGroupsActive (disbanded, destroyed, or
    //released to land) we must drop our per-airgroup state too, or these dictionaries grow for the
    //whole mission.  Keyed by AiAirGroup, so each stale key is small - but it is permanent, and it
    //keeps the dead group object alive through the key reference.
    public void forgetAirGroup(AiAirGroup airGroup)
    {
        if (airGroup == null) return;
        coverAGSpeedRatio.Remove(airGroup);
        coverAGSpeedRequested.Remove(airGroup);
        coverAircraftAirGroupsOrders.Remove(airGroup);
        coverAircraftAirGroupsTargetPoint.Remove(airGroup);
        coverAircraftAirGroupsDropIssued.Remove(airGroup);
        coverAircraftAirGroupsDroppedThisPass.Remove(airGroup);  //<cdrop one-issue-per-pass latch - same leak class as the hold-off above
        coverAircraftAirGroupsReleased.Remove(airGroup);
        coverAircraftAirGroupsBayWaitSince.Remove(airGroup);   //<cdrop Step C8 - same leak class as the rest
        coverDropPassLogged.Remove(airGroup);            //<cdrop Step D - and its log throttle with it
        coverAircraftAirGroupsLoiterPoint.Remove(airGroup);
        airgroupTargets.Remove(airGroup);
        airgroupGroundTargets.Remove(airGroup);
        airgroupTargetPoints.Remove(airGroup);
        //Do NOT touch coverOrdersBeforeDrop here: BAM_leaveDropMode restores from the snapshot,
        //and a mid-drop disband must still restore the survivors.  A dead group simply never matches
        //the restore loop (it iterates live coverAircraftAirGroupsActive keys).
    }

    public string BAM_toggleBombAimMode(Player player)
    {
        if (player == null) return "(none)";
        BAM_BombAimMode bam = BAM_getplayerBombAimMode_enum(player);

        if (isOnRepairMission(player))
        {
            bam = BAM_BombAimMode.None;
        }
        else
        {
            if (bam == BAM_BombAimMode.Knickebein_Point) bam = BAM_BombAimMode.Nearest_Enemy_to_Knickebein_Point;
            else if (bam == BAM_BombAimMode.Nearest_Enemy_to_Knickebein_Point) bam = BAM_BombAimMode.Bomb_Explosion_Point;
            else if (bam == BAM_BombAimMode.Bomb_Explosion_Point) bam = BAM_BombAimMode.Nearest_Enemy_to_Bomb_Explosion;
            else if (bam == BAM_BombAimMode.Nearest_Enemy_to_Bomb_Explosion) bam = BAM_BombAimMode.Drop_Flare_Point_Here_and_Target_it;
            else if (bam == BAM_BombAimMode.Drop_Flare_Point_Here_and_Target_it) bam = BAM_BombAimMode.Nearest_Enemy_to_Flare_Point;
            else if (bam == BAM_BombAimMode.Nearest_Enemy_to_Flare_Point) bam = BAM_BombAimMode.Drop_When_I_Drop;
            else if (bam == BAM_BombAimMode.Drop_When_I_Drop) bam = BAM_BombAimMode.None;
            else if (bam == BAM_BombAimMode.None) bam = BAM_BombAimMode.Knickebein_Point;
        }

        BAM_playerAimMode[player] = bam;

        //remove any existing targets
        
        airgroupTargets = new Dictionary<AiAirGroup, AiActor>();
        airgroupGroundTargets = new Dictionary<AiAirGroup, GroundStationary>();
        airgroupTargetPoints = new Dictionary<AiAirGroup, Point3d>();
        
        if (bam != BAM_BombAimMode.Nearest_Enemy_to_Bomb_Explosion) PBP_removePlayerLastBombOrMyPositionPoint(player); //Toggling bomb mode erases the last bomb drop location, except when switching bomb point=>actor

        if (bam == BAM_BombAimMode.Nearest_Enemy_to_Flare_Point || bam == BAM_BombAimMode.Drop_Flare_Point_Here_and_Target_it)
        {
            if (player != null && player.Place() != null)
            {
                PBP_saveBombPoint(player, player.Place().Pos());
                double wait = 10;
                if (player.Place().Pos().z > 10) wait = player.Place().Pos().z / 120;  //person's terminal velocity is 50 m/s, we'll say something like a flare is a bit higher, say 120
                Timeout(wait, () =>
               {
                   Calcs.loadCratersAndSmoke(GamePlay, mainmission, player.Place().Pos().x, player.Place().Pos().y, 0, "BuildingFireSmall");  //this is the smallest type of smoke  "BuildingFireLarge" a bit larger.  Smoke1 Smoke2 BigSitySmoke etc all larger yet
               });
            }
            else { GamePlay.gpLogServer(new Player[] { player }, "COVER ERROR! Couldn't find your position because you are not in an aircraft.", null); }
        }

        return BAM_getPlayerBombAimMode_string(player);
    }

    //<cdrop - turn DROP WHEN I DROP on.  Called both from the Tab-4-4-4-4-6 menu (BAM cycle) and from
    //the <cdrop chat command, so the menu label and the actual orders can never disagree.
    //Snapshot the player's current orders FIRST, then put everyone on .drop.  Empty msg => every
    //group is selected (see setCoverAircraftAirGroupsOrders), which is what the menu can express;
    //players who want only some squadrons use "<cdrop 3 6" in chat.  The all-groups scope is
    //remembered alongside the snapshot (see coverOrdersBeforeDrop): a group later released from
    //<cstrict/<creserve with <cnormal re-joins drop mode only under an all-groups scope; under a
    //selective "<cdrop 3 6" scope it stays on normal, out of the drop.  Groups sitting on
    //<cstrict/<creserve are SKIPPED by a bare all-groups <cdrop (they were told to hold fire, and a
    //blanket drop order must not overrule that); name them explicitly ("<cdrop 2") to pull them in.
    public void BAM_enterDropMode(Player player, string msg = "")
    {
        if (player == null) return;

        //Re-issuing <cdrop to RE-ARM an existing drop must NOT overwrite the snapshot - that would
        //throw away the pre-drop orders and leave us with nowhere to restore to.  But if the re-arm
        //is a BARE <cdrop (all groups) it widens the scope: any earlier selective "<cdrop 3 6" snapshot
        //becomes all-groups, so <cnormal re-joins work from here on.  A selective re-arm never narrows
        //an all-groups scope - the Tab-4 label applies to everyone.
        if (coverOrdersBeforeDrop.ContainsKey(player))
        {
            if (msg.Trim().Length == 0)
            {
                var held = coverOrdersBeforeDrop[player];
                if (!held.Item2) coverOrdersBeforeDrop[player] = new Tuple<Dictionary<AiAirGroup, CoverAGOrders>, bool>(held.Item1, true);
            }
            setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.drop, "were ordered to DROP WHEN YOU DROP - they will hold their bombs and, the moment you let your first bomb go, release everything at the same time.", skipHoldFire: msg.Trim().Length == 0);
            BAM_forceFormationRefresh(player);  //<cdrop Step D - cancel any transient menu-cycle attack run NOW (see helper)
            return;
        }

        Dictionary<AiAirGroup, CoverAGOrders> snap = new Dictionary<AiAirGroup, CoverAGOrders>();
        try
        {
            foreach (KeyValuePair<AiAirGroup, Player> kv in coverAircraftAirGroupsActive)
            {
                if (kv.Value != player) continue;
                AiAirGroup airGroup = kv.Key;
                if (airGroup == null) continue;
                if (!coverAircraftAirGroupsOrders.ContainsKey(airGroup)) continue;
                snap[airGroup] = coverAircraftAirGroupsOrders[airGroup];
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover BAM_enterDropMode ERROR taking snapshot! " + ex.ToString()); }
        coverOrdersBeforeDrop[player] = new Tuple<Dictionary<AiAirGroup, CoverAGOrders>, bool>(snap, msg.Trim().Length == 0);

        armCoverDropWatch(player);  //start watching the leader's bomb count
        setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.drop, "were ordered to DROP WHEN YOU DROP - they will hold their bombs and, the moment you let your first bomb go, release everything at the same time.", skipHoldFire: msg.Trim().Length == 0);
        BAM_forceFormationRefresh(player);  //<cdrop Step D - cancel any transient menu-cycle attack run NOW (see helper)
    }

    //<cdrop Step D - re-issue an immediate formation (FOLLOW) flight plan to every cover group this
    //player owns.  This mirrors exactly what keepAircraftOnTask_recurs() writes for
    //orders == CoverAGOrders.drop (the <cdrop / <cstrict branch: FOLLOW, altDiff -3 +/- 15).  The
    //Tab-4 menu cycle passes THROUGH Drop_Flare_Point_Here_and_Target_it, which points the player's
    //bomb point at their CURRENT position (see BAM_toggleBombAimMode); if the ~16s AI tick fires on
    //that transient the groups pick up a GATTACK_POINT run at a point that is already behind them, so
    //they turn round and release before the player has even finished selecting DROP WHEN I DROP.
    //Clearing the target dictionaries (done on every toggle) does NOT replace a flight plan that is
    //already being flown, so we overwrite it here, immediately.  Groups still inside their 25s
    //post-release hold-off are SKIPPED, so a re-arm can never clobber a release in progress.
    public void BAM_forceFormationRefresh(Player player)
    {
        if (player == null) return;
        try
        {
            if (player.Place() == null || (player.Place() as AiAircraft) == null) return;
            AiAirGroup targetAirGroup = (player.Place() as AiAircraft).AirGroup();
            if (targetAirGroup == null) return;

            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int refreshed = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null) continue;
                if (!coverAircraftAirGroupsActive.ContainsKey(airGroup)) continue;
                if (coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (airGroup.GetItems() == null || airGroup.GetItems().Length == 0) continue;
                //Never overwrite a release plan that is still inside its hold window - the bombs may not
                //have gone yet (see coverAircraftAirGroupsDropIssued / coverDropHoldFlightPlan_s).
                if (coverAircraftAirGroupsDropIssued.ContainsKey(airGroup) &&
                    (DateTime.UtcNow - coverAircraftAirGroupsDropIssued[airGroup]).TotalSeconds < coverDropHoldFlightPlan_s) continue;

                EscortUpdateWaypoints(player, airGroup, targetAirGroup, AiAirWayPointType.FOLLOW, altDiff_m: -3, AltDiff_range_m: 15, nodupe: true, orders: CoverAGOrders.drop);
                refreshed++;
            }
            if (refreshed > 0) Console.WriteLine("COVER <cdrop: " + refreshed + " group(s) given an immediate formation plan on entering DROP WHEN I DROP (cancels any transient menu-cycle attack run)");
        }
        catch (Exception ex) { Console.WriteLine("Cover BAM_forceFormationRefresh ERROR! " + ex.ToString()); }
    }

    //<cdrop - turn DROP WHEN I DROP off again, restoring the orders that were in force when it was
    //switched on.  Only groups whose order is STILL .drop get reverted; anything the player changed
    //in the meantime (a <creserve N, most importantly) is left exactly as they set it.
    public void BAM_leaveDropMode(Player player)
    {
        if (player == null) return;
        Tuple<Dictionary<AiAirGroup, CoverAGOrders>, bool> held;
        if (!coverOrdersBeforeDrop.TryGetValue(player, out held)) return;  //nothing remembered, so nothing to undo
        Dictionary<AiAirGroup, CoverAGOrders> snap = held.Item1;
        coverOrdersBeforeDrop.Remove(player);

        try
        {
            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int restored = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null) continue;
                if (coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (!coverAircraftAirGroupsOrders.ContainsKey(airGroup)) continue;
                if (coverAircraftAirGroupsOrders[airGroup] != CoverAGOrders.drop) continue;  //player has since overridden it - leave it alone

                CoverAGOrders was = CoverAGOrders.normal;
                if (snap.ContainsKey(airGroup)) was = snap[airGroup];   //group that appeared after we started: fall back to normal
                coverAircraftAirGroupsOrders[airGroup] = was;
                restored++;
            }
            if (GamePlay != null && restored > 0) GamePlay.gpLogServer(new Player[] { player }, restored.ToString() + " groups of cover aircraft returned to their previous orders (DROP WHEN I DROP is off)", new object[] { });
        }
        catch (Exception ex) { Console.WriteLine("Cover BAM_leaveDropMode ERROR restoring orders! " + ex.ToString()); }
    }

    public void BAM_toggleBombAimMode_withmessages(Player player)
    {
        if (GamePlay == null) return;
        //<cdrop - entering/leaving DROP WHEN I DROP is a change to the airgroup ORDERS, not just to the
        //menu label, so capture the mode we are leaving in order to act on the transition.
        BAM_BombAimMode prev = BAM_getplayerBombAimMode_enum(player);
        bool removeLastBombPoint = true;
        string s = BAM_toggleBombAimMode(player);
        BAM_BombAimMode now = BAM_getplayerBombAimMode_enum(player);

        if (prev != now)
        {
            if (now == BAM_BombAimMode.Drop_When_I_Drop) BAM_enterDropMode(player);
            else if (prev == BAM_BombAimMode.Drop_When_I_Drop) BAM_leaveDropMode(player);
        }

        if (isOnRepairMission(player))
        {
            string m2 = String.Format("COVER: Cover airgroup targeting for repair/restock missions is always '{0}'", s);
            GamePlay.gpLogServer(new Player[] { player }, m2, null);
        }

        string m = String.Format("COVER: Cover airgroup targeting mode switched to '{0}'", s);
        GamePlay.gpLogServer(new Player[] { player }, m, null);
        if (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Knickebein_Point)
            GamePlay.gpLogServer(new Player[] { player }, "Cover bombers will target any enemy object close to the Knickebein Target Point, within any nearby objective radius or up to 2500m out", null);
        else if (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Bomb_Explosion_Point)
        {
            GamePlay.gpLogServer(new Player[] { player }, "Cover bombers will target the point of your next bomb explosion.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Any previous target points you have set are now erased.", null);
        }
        else if (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Bomb_Explosion)
        {
            GamePlay.gpLogServer(new Player[] { player }, "Cover bombers will target the nearest enemy object to the point of your next bomb explosion.", null);
            removeLastBombPoint = false;

        }
        else if (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Drop_Flare_Point_Here_and_Target_it)
        {
            GamePlay.gpLogServer(new Player[] { player }, "Flare dropped from your current location! Cover bombers will target the flare drop point.", null);
            //GamePlay.gpLogServer(new Player[] { player }, "Any previous target points you have set are now erased.", null);

        }
        else if (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Nearest_Enemy_to_Flare_Point)
        {
            GamePlay.gpLogServer(new Player[] { player }, "Cover bombers will target enemy ground objects nearest the flare drop point.", null);
        }
        //<cdrop - the one mode on this menu that is about WHEN, not WHERE.  No ground target is set
        //for it at all; the release is aimed off the leader's own position when he lets his bombs go.
        else if (BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Drop_When_I_Drop)
        {
            GamePlay.gpLogServer(new Player[] { player }, "DROP WHEN I DROP is ON. Your bombers hold their bombs and fly tight formation with you, then release everything the moment you drop.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Wait until every group shows GND ATTACK on the chat display, then they are awaiting your drop. Re-issue <cdrop to re-arm them for another run.", null);
            removeLastBombPoint = false;  //this mode does not use the player's bomb drop point
        }

        //if (removeLastBombPoint) PBP_removePlayerLastBombPoint(player); //Toggling bomb mode erases the last bomb drop location, except when switching point=>actor


    }


    /*************************************************************
    //PBP - Player Last Bomb Explosion
    //
    //Saves the player last bomb explosion location
    //After 2 mins the last bomb explosion location will be replaced by any new bomb explosion location
    //After 5 mins the last bomb explosion location expires.
    //
    //This allows the player's last bomb explosion location to be used to guide Cover Bomber attack point
    //The bombers will attack the point where the player dropped bombs for up to 10 minutes
    //If the player drops in a different location 2 mins or more later, that will become the new attack location.
    //
    ***************************************************************************************************************/


    //So all our normal routines use point (-1,-1,-1) to indicate no point is set, sort of like NULL
    public Point3d PBP_getPlayerLastBombOrMyPositionPoint_point3d(Player player)
    {
        Point3d nullPoint = new Point3d(-1, -1, -1);
        if (player == null) return nullPoint;
        Tuple<Point3d, DateTime> plbe = PBP_getPlayerLastBombPoint_tuple(player);

        if (plbe != null)
        {
            TimeSpan since = plbe.Item2 - DateTime.UtcNow;
            if (since.TotalSeconds < 10 * 60) return plbe.Item1;
            else return nullPoint;
        }
        else return nullPoint;
    }

    public Tuple<Point3d, DateTime> PBP_getPlayerLastBombPoint_tuple(Player player)
    {
        if (PBP_playerBombPoint.ContainsKey(player)) return PBP_playerBombPoint[player];
        else return null;
    }

    public void PBP_removePlayerLastBombOrMyPositionPoint(Player player)
    {
        if (PBP_playerBombPoint.ContainsKey(player)) PBP_playerBombPoint.Remove(player);
    }

    public void PBP_saveBombPoint(Player player, Point3d pos)
    {
        bool updated = false;
        if (player == null) return;
        Tuple<Point3d, DateTime> plbe = PBP_getPlayerLastBombPoint_tuple(player);
        //Point3d plbePoint = new Point3d
        if (plbe != null)
        {
            TimeSpan since = plbe.Item2 - DateTime.UtcNow;
            if (since.TotalSeconds > 20 * 60)
            {
                PBP_playerBombPoint[player] = new Tuple<Point3d, DateTime>(pos, DateTime.UtcNow);
                updated = true;
            }
        }
        else
        {
            PBP_playerBombPoint[player] = new Tuple<Point3d, DateTime>(pos, DateTime.UtcNow);
            updated = true;
        }
        if (updated && BAM_isBombPoint(player) && GamePlay != null)
        {
            GamePlay.gpLogServer(new Player[] { player }, "COVER {0} set new target point for cover bombers in sector {1} ", new object[] { player.Name(), Calcs.correctedSectorNameDoubleKeypad(this, pos) });
        }
    }
    /*
    public override void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        #region stb
        base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);

        try
        {

            if (actor != null && actor is AiGroundActor)
            {
                Player player = null;
                if (initiator != null && initiator.Player != null) player = initiator.Player;
                if (player == null) return;                
                Point3d pos = actor.Pos();
                Console.WriteLine("OnActorDamaged - " + player.Name() + " " + Calcs.correctedSectorNameDoubleKeypad(this, pos));
                PBP_saveBombPoint(player, pos);
            }

        }
        catch (Exception ex) { Console.WriteLine("OnActorDamaged -cover ERROR: " + ex.ToString()); }
        #endregion
    }
    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        #region stb
        base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);

        try
        {

            if (actor != null && actor is AiGroundActor)
            {
                Player player = null;
                if (initiator != null && initiator.Player != null) player = initiator.Player;
                if (player == null) return;
                Point3d pos = actor.Pos();
                Console.WriteLine("OnActorDamaged - " + player.Name() + " " + Calcs.correctedSectorNameDoubleKeypad(this, pos));
                PBP_saveBombPoint(player, pos);
            }

        }
        catch (Exception ex) { Console.WriteLine("OnActorDamaged -cover ERROR: " + ex.ToString()); }
        #endregion
    }

    public override void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    {
        base.OnStationaryKilled(missionNumber, stationary, initiator, eventArgInt);
        try
        {
            Player player = null;
            if (initiator != null && initiator.Player != null) player = initiator.Player;
            if (player == null) return;
            Point3d pos = stationary.pos;
            PBP_saveBombPoint(player, pos);
            Console.WriteLine("OnStationaryKilled - " + player.Name() + " " + Calcs.correctedSectorNameDoubleKeypad(this, pos));

        }
        catch (Exception ex) { Console.WriteLine("OnStationaryKilled -cover ERROR: " + ex.ToString()); }
    }
    */

    public override void OnBombExplosion(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        try
        {

            base.OnBombExplosion(title, mass_kg, pos, initiator, eventArgInt);
            Player player = null;
            if (initiator != null && initiator.Player != null) player = initiator.Player;
            if (player == null) return;
            PBP_saveBombPoint(player, pos);
            Console.WriteLine("OnBombExplosion - " + player.Name() + " " + Calcs.correctedSectorNameDoubleKeypad(this, pos));
        }
        catch (Exception ex) { Console.WriteLine("OnBombExplosion -cover ERROR: " + ex.ToString()); }
    }

    /*
     *This would be a clever way to reset the player's Bomb Aim mode whenever they enter a new aircraft
     *But we probably don't want to do this . . . more like, whenever they choose new cover a/c
    public Dictionary<Player, actor> playerPlaceDict = new Dictionary<Player, actor>();

    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);

        bool playerNewAircraft = false;
        if (player != null)
        {
            if (playerPlaceDict.ContainsKey(player))
            {
                if (actor == playerPlaceDict[player]) return;
            }
            playerPlaceDict[player] = actor; //either the player wasn't in a plane already OR it is a new one
            BAM_resetBombAimMode(Player player);
        }
    }
    */

    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);

        AiActor actor = aircraft as AiActor;
        if (actor == null) return;

        if (coverAircraftActorsCheckedOut.ContainsKey(actor))
        {
            Console.WriteLine("OnAircraftLanded: " + aircraft.AirGroup().Name(), coverAircraftActorsCheckedOut[actor].Name());
            if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, reason: "SAFE_cover_LandedAtAirport");
            numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
            coverAircraftActorsCheckedOut.Remove(actor);
        }
    }

    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
        AiActor actor = aircraft as AiActor;
        if (actor == null) return;
        if (coverAircraftActorsCheckedOut.ContainsKey(actor))
        {
            Console.WriteLine("OnAircraftCrashLanded: " + aircraft.AirGroup().Name(), coverAircraftActorsCheckedOut[actor].Name());
            if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                                                                                                                                         //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't give a/c back as this one was killed!
            coverAircraftActorsCheckedOut.Remove(actor);
        }
    }
    public override void OnAircraftKilled(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftKilled(missionNumber, shortName, aircraft);
        if (aircraft == null) return;
        AiActor actor = aircraft as AiActor;
        if (coverAircraftActorsCheckedOut.ContainsKey(actor))
        {
            Console.WriteLine("OnAircraftKilled: " + aircraft.AirGroup().Name(), coverAircraftActorsCheckedOut[actor].Name());
            if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1, reason: "DEAD_cover_CoverAircraftKilled"); //the final "1" forced 100% damage of aircraft/write-off
                                                                                                                                         //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't give a/c back as this one was killed!
            coverAircraftActorsCheckedOut.Remove(actor);
        }
    }

    public override void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorDestroyed(missionNumber, shortName, actor);

        try
        {
            //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was destroyed; doing aircraft checkin");



            double minX = 20000;
            double minY = 20000;
            double maxX = 340000;
            double maxY = 300000;
            //double maxY = 340000; for TOBRUK

            //AiActor actor = aircraft as AiActor;
            if (coverAircraftActorsCheckedOut.ContainsKey(actor))
            {
                Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was checked out by Cover");

                AiAircraft aircraft = actor as AiAircraft;
				
				string reason = "";
				if (mainmission.AircraftDestroyedList.ContainsKey(aircraft)) reason = mainmission.AircraftDestroyedList[aircraft];

                if (aircraft != null)
                {
                    //if there are no more aircraft in this airgroup then we need to remove the airgroup from our cover list
                    //int numAC = aircraft.AirGroup().NOfAirc; 2021-07, now I'm suspicious of this
                    int countAC = 0; //counting them up as below seems to give the same answer as NOfAirc
                    foreach (AiActor a in aircraft.AirGroup().GetItems())
                    {
                        if (a == actor || !a.IsAlive()) continue;
                        countAC++;
                    }

                    //Console.WriteLine("CoverOnDestroy: Counting a/c left in " + actor.Name() + " {0} {1} {2}", aircraft.AirGroup().Name(), numAC, countAC);
                    if (countAC == 0 && coverAircraftAirGroupsActive.ContainsKey(aircraft.AirGroup()))
                    {
                        coverAircraftAirGroupsActive.Remove(aircraft.AirGroup());
                        forgetAirGroup(aircraft.AirGroup());
                        //Console.WriteLine("CoverOnDestroy: Removing airgroup from active list");
                    }



                    double Z_AltitudeAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                    //double distNearestAirport_m = Stb_distanceToNearestAirport(actor);

                    if ((aircraft.Pos().x <= minX ||
                            aircraft.Pos().x >= maxX ||
                            aircraft.Pos().y <= minY ||
                            aircraft.Pos().y >= maxY
                          )

                    )
                    {
                        if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true, reason: "SAFE_cover_CoverAircraftOffMap: "+reason); //valid return; the final true is softexit & forces return of a/c even though it is still flying.
                        numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was returned to stock because left map OK.");

                    }
                    else if (Z_AltitudeAGL < 5 && GamePlay != null && GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y) == LandTypes.WATER) // ON GROUND & IN THE WATER = DEAD    
                    {
                        if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1,reason: "DEAD_cover_Landed/CrashedOnWater: "+reason); //the final "1" forced 100% damage of aircraft/write-off
                                                                                                                                                     //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died on water.");
                    }
                    // crash landing in solid ground

                    else if (Z_AltitudeAGL < 5 && GamePlay != null && GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y) != aircraft.Army())    // landed in enemy territory
                    {
                        if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1, reason: "DEAD_cover_CoverAircraftLanded/CrashedOnEnemyTerritory: "+reason); //the final "1" forced 100% damage of aircraft/write-off
                                                                                                                                                     //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died in enemy territory.");

                    }
                    else if (Z_AltitudeAGL < 5 && Stb_distanceToNearestFriendlyAirport(actor).Item1 > 3500)  // crash landed in friendly or neutral territory, on land, not w/i 2000 meters of an airport
                    {
                        if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1, reason: "DEAD_cover_CoverAircraftLanded/CrashedAwayFromAirport: "+reason); //the final "1" forced 100% damage of aircraft/write-off
                                                                                                                                                     //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died away from airport.");
                    }

                    else if (Z_AltitudeAGL < 800)  // movebombtarget auto-destroys ai aircraft that are in the vicinity of an airport and set to waypoint type LANDING.  Because they are too dumb to actually land.  So these count as "landed" & the aircraft is returned to supply.
                    {
                        AiAirGroup airGroup = aircraft.AirGroup();
                        if (airGroup == null || !isAiControlledPlane2(aircraft))
                        {
                            //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " - no AirGroup");
                            return; //only process groups that have been in place a while, have actual aircraft in the air, and ARE ai
                        }
                        AiAirGroupTask task = airGroup.getTask();
                        AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
                        int currWay = airGroup.GetCurrentWayPoint();
                        bool landingWaypoint = false;
                        //Console.WriteLine("CoverOnDestroy: Checking {0} {1} {2} {3} ", CurrentWaypoints.Length, currWay, (CurrentWaypoints[currWay] as AiAirWayPoint).Action, task);

                        if (CurrentWaypoints != null && CurrentWaypoints.Length > 0 && CurrentWaypoints.Length > currWay && (CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.LANDING) landingWaypoint = true;

                        if (task != AiAirGroupTask.LANDING && !landingWaypoint) return;
						
						
						if (reason.ToLower().StartsWith("dead")) {
							if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 1, false, reason: reason); //true is softexit & forces return of plane even though it is in the air etc.
							//numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
							coverAircraftActorsCheckedOut.Remove(actor);
							
						} else {


							if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true, reason: "SAFE_cover_LandedOrDisapparatedNearAirport"); //true is softexit & forces return of plane even though it is in the air etc.
							numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
							coverAircraftActorsCheckedOut.Remove(actor);
							//Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was returned to stock because disapparated during or after LANDING.");
						}
                    }
					
                    else
                    {
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " didn't match anything, no action taken.");
                    }

                }
            }


        }
        catch (Exception ex) { Console.WriteLine("Cover OnActorDestroyed ERROR: " + ex.ToString()); }
    }

    //Which a/c are currently available as cover a/c depending on stock available etc orderedictionary = acName, num remaining as string
    public Dictionary<ArmiesE, Dictionary<string, int>> CoverAircraftCurrentlyAvailable = new Dictionary<ArmiesE, Dictionary<string, int>>();

    //public Dictionary<ArmiesE, Dictionary<string, bool>> CoverAircraftInitiallyAvailable = new Dictionary<ArmiesE, Dictionary<string, bool>>();

    //Which a/c are potentially available as cover a/c
    public Dictionary<ArmiesE, Dictionary<string, bool>> CoverAircraftInitiallyAvailable = new Dictionary<ArmiesE, Dictionary<string, bool>>
    {
        { ArmiesE.Red, new Dictionary<string,bool>() {


			// {bob."aircraft as known to game name",whether available for use as an escort aircraft or not},
			//mostly, bombers aren't available as escorts, or bomber-enabled fighter variants
			//also rare or very valuable aircraft are not available
			//TODO: could make this dictionary to include info as to whether allowed as a cover aircraft, whether it is a bomber or fighter as cover a/c, whether players flying
			//that plane are allowed to have cover a/c as a fighter or a bomber pilot
			//If set to "false" the a/c can't be used as cover
			//Also, an aircraft not on this list can't be used as cover.  So you can just rem out a line to remove an aircraft.





			//{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below        
			{"bob:Aircraft.HurricaneMkI",true},
			{"bob:Aircraft.HurricaneMkI_100oct",true},
			{"bob:Aircraft.HurricaneMkI_100oct-NF",true},
			{"bob:Aircraft.HurricaneMkI_dH5-20",true},
			{"bob:Aircraft.HurricaneMkI_dH5-20_100oct",true},
			{"bob:Aircraft.HurricaneMkI_FB",true},


			{"tobruk:Aircraft.HurricaneMkI_FB-Trop", false},
			{"tobruk:Aircraft.HurricaneMkIIa", true},
			{"tobruk:Aircraft.HurricaneMkIIaTrop", false},
			{"tobruk:Aircraft.HurricaneMkIIb", true},
			{"tobruk:Aircraft.HurricaneMkIIb-Late", true},
			{"tobruk:Aircraft.HurricaneMkIIbTrop", false},
			{"tobruk:Aircraft.HurricaneMkIIbTrop-Late", false},
			{"tobruk:Aircraft.HurricaneMkIIc", true},
			{"tobruk:Aircraft.HurricaneMkIIc-Late", true},
			{"tobruk:Aircraft.HurricaneMkIIc-Trop", false},
			{"tobruk:Aircraft.HurricaneMkIIc-Trop-Late", false},
			{"tobruk:Aircraft.HurricaneMkIId", true},
			{"tobruk:Aircraft.HurricaneMkIId-Trop", false},
			{"bob:Aircraft.SpitfireMkI",true},
			{"bob:Aircraft.SpitfireMkIa",true},
			{"bob:Aircraft.SpitfireMkIa_100oct",true},
			{"bob:Aircraft.SpitfireMkI_100oct",true},
			{"bob:Aircraft.SpitfireMkIIa",false},			
			{"tobruk:Aircraft.SpitfireMkIIb", true},
			{"tobruk:Aircraft.SpitfireMkVa", true},
			{"tobruk:Aircraft.SpitfireMkVb", true},
			{"tobruk:Aircraft.SpitfireMkVb-HF", false},
			{"tobruk:Aircraft.SpitfireMkVb-HF-Late", false},
			{"tobruk:Aircraft.SpitfireMkVb-HF-Trop", false},
			{"tobruk:Aircraft.SpitfireMkVbLate", true},
			{"tobruk:Aircraft.SpitfireMkVbTrop", false},
			{"tobruk:Aircraft.KittyhawkMkIA", true},
			{"tobruk:Aircraft.KittyhawkMkIA-Trop", true},
			{"tobruk:Aircraft.MartletMkIII", true},
			{"tobruk:Aircraft.MartletMkIII_Trop", false},			
			{"tobruk:Aircraft.TomahawkMkII", true},
			{"tobruk:Aircraft.TomahawkMkII-Late", true},
			{"tobruk:Aircraft.TomahawkMkII-Late-Trop", false},
			{"tobruk:Aircraft.TomahawkMkII-Trop", false},

			//STURMOVIK

			{"bob:Aircraft.DefiantMkI",true},
			{"bob:Aircraft.BlenheimMkIF", true},
			{"bob:Aircraft.BlenheimMkINF", true},
			{"bob:Aircraft.BlenheimMkIVF",true},
			{"bob:Aircraft.BlenheimMkIVF_Late",true},
			{"bob:Aircraft.BlenheimMkIVNF",true},
			{"bob:Aircraft.BlenheimMkIVNF_Late",true},
			{"bob:Aircraft.BeaufighterMkIF", true},
			{"bob:Aircraft.BeaufighterMkINF",true},
			{"tobruk:Aircraft.BeaufighterMkIF_Late", true},
			{"tobruk:Aircraft.BeaufighterMkINF_Late", true},
			{"tobruk:Aircraft.BeaufighterMkIC", true},
			{"tobruk:Aircraft.BeaufighterMkIC_Trop", false},
			{"tobruk:Aircraft.BeaufighterMkIF_Late_Trop", false},
			{"tobruk:Aircraft.BeaufighterMkINF_Late_Trop", false},			

			//MISC - OLDER
			{ "tobruk:Aircraft.GladiatorMkII_trop", true},
			{"bob:Aircraft.DH82A-2",false},
			{"bob:Aircraft.AnsonMkI",true},		

			//BOMBERS
			{"bob:Aircraft.SunderlandMkI",true},
			{"bob:Aircraft.WalrusMkI",false}, //as of 2026-08, the AI walrus won't drop bombs, so disabling it for now. https://www.tfbt.nuvturais.de/issues/1584			
			{"bob:Aircraft.BlenheimMkI", true},			
			{"bob:Aircraft.BlenheimMkIV", true},
			{"bob:Aircraft.BlenheimMkIV_Late",true},
			
			{"tobruk:Aircraft.WellingtonMkIa_trop", false}, //This is a great plane BUT seems to cause the bug where ppl can't spawn into the server any more. 
			{"bob:Aircraft.WellingtonMkIc",true}, //actually the Welly Ia       		
			{"tobruk:Aircraft.WellingtonMkIc_Late", true},
			{"tobruk:Aircraft.WellingtonMkIc_Late_trop", false}, //5.003 WON'T DROP BOMBS (more than 1 a mission) SO ELIMINATING IT FOR NOW. 5.017 SHOULD BE FIXED (2021/02), so re-adding it
			{"tobruk:Aircraft.WellingtonMkIc_t", true}, //welly Ic
			{"tobruk:Aircraft.WellingtonMkIc_Torpedo", true },
			{"tobruk:Aircraft.WellingtonMkIc_Torpedo_Trop", true},
			{"tobruk:Aircraft.WellingtonMkIc_trop", false},
			//5.003 WON'T DROP BOMBS (more than 1 a mission) SO ELIMINATING IT FOR NOW  5.017 SHOULD BE FIXED (2021/02), so re-adding it

		} 
	},
		{ ArmiesE.Blue, new Dictionary <string,bool>(){
			
			//FIGHTERS
			{"bob:Aircraft.Bf-109E-1",true},
			{"bob:Aircraft.Bf-109E-1B",true},
			{"bob:Aircraft.Bf-109E-3",true},
			{"bob:Aircraft.Bf-109E-3B",true},
			{"bob:Aircraft.Bf-109E-4",true},
			{"bob:Aircraft.Bf-109E-4_Late",true},

			{"tobruk:Aircraft.Bf-109E-7",true},
			{"tobruk:Aircraft.Bf-109E-7_Trop",false},
			{"tobruk:Aircraft.Bf-109E-7N",true},
			{"tobruk:Aircraft.Bf-109E-7N_Trop",false},
			{"tobruk:Aircraft.Bf-109E-7Z",false},
			{"bob:Aircraft.Bf-109E-4B",true},
			{"bob:Aircraft.Bf-109E-4B_Late",true},
			{"bob:Aircraft.Bf-109E-4N",true},
			{"bob:Aircraft.Bf-109E-4N_Late",true},			
			{"tobruk:Aircraft.Bf-109F-1",true},
			{"tobruk:Aircraft.Bf-109F-2",true},
			{"tobruk:Aircraft.Bf-109F-2_Late",true},
			{"tobruk:Aircraft.Bf-109F-2_Trop",false},
			{"tobruk:Aircraft.Bf-109F-4",true},
			{"tobruk:Aircraft.Bf-109F-4_Derated",true},
			{"tobruk:Aircraft.Bf-109F-4_Trop",false},
			{"tobruk:Aircraft.Bf-109F-4_trop_Derated",false},
			{"tobruk:Aircraft.Bf-109F-4Z",false},
			{"tobruk:Aircraft.Bf-109F-4Z_Trop",false},		


			{"bob:Aircraft.G50",true},
			{"tobruk:Aircraft.G50_Trop",false},
			{"tobruk:Aircraft.Macchi-C202-SeriesIII",true},
			{"tobruk:Aircraft.Macchi-C202-SeriesIII-AltoQuota",true},
			{"tobruk:Aircraft.Macchi-C202-SeriesVII",true},
			{"tobruk:Aircraft.Macchi-C202-SeriesVII-AltoQuota",true},

			//heavy fighters/Sturmovik
			{"bob:Aircraft.Bf-110C-2",true},
			{"bob:Aircraft.Bf-110C-4",true},			
			{"tobruk:Aircraft.Bf-110C-4B_Trop",false},
			{"bob:Aircraft.Bf-110C-4B" ,true},
			{"bob:Aircraft.Bf-110C-4Late",true},
			{"bob:Aircraft.Bf-110C-4N",true},
			{"bob:Aircraft.Bf-110C-4-NJG",true},			
			{"tobruk:Aircraft.Bf-110C-4N-NJG_Trop",false},						
			//{"bob:Aircraft.Bf-110C-4-NJG",true}, //this is a good cover a/c for bombers
			{"bob:Aircraft.Bf-110C-6",true},  //These crash straight into the ground upon spawn FOR SOME UNKNOWN REASON so just eliminating their use altogether here.
			{"tobruk:Aircraft.Bf-110C-6_Trop",false},
			{"bob:Aircraft.Bf-110C-7",true},			
			{"tobruk:Aircraft.Bf-110C-7_Trop",false},		
			{"tobruk:Aircraft.Ju-88C-1",true},
			{"tobruk:Aircraft.Ju-88C-2",true},
			{"tobruk:Aircraft.Ju-88C-2_Trop",false},
			{"tobruk:Aircraft.Ju-88C-4",true},
			{"tobruk:Aircraft.Ju-88C-4_Trop",false},
			{"tobruk:Aircraft.Ju-88C-4Late",true},
			{"tobruk:Aircraft.Ju-88C-4Late_Trop",false},			

			//MISC - OLDER
			{"bob:Aircraft.DH82A-2",false},
			{"tobruk:Aircraft.Bf-108B-2_Trop",true},
			{"tobruk:Aircraft.CR42_Trop",false},
			{"tobruk:Aircraft.D520_Serie1",true},
			{"tobruk:Aircraft.D520_Serie1_Trop",false},
			{"tobruk:Aircraft.DH82A_Trop",false}, //Tiger Moth/no weapons at all

			//{"bob:Aircraft.DH82A-1",10},  aircraft not on list aren't allowed as escorts, so disallow by either setting to FALSE or just remming out their line


			//BOMBERS
			{"bob:Aircraft.BR-20M",true},	
			{"tobruk:Aircraft.BR-20M_Trop",false},
			{"bob:Aircraft.Do-17Z-2",true}, //17Z-1 also exists, but is so similar...
			{"bob:Aircraft.Do-215B-1",true},
			{"tobruk:Aircraft.He-111H-2_Trop",false},
			{"bob:Aircraft.He-111H-2",true},
			{"tobruk:Aircraft.He-111H-6",true}, //this is just the same as H-6_Hermann, below, but leaving that as the name to emphasize that it carries the Hermanns		

			{"tobruk:Aircraft.He-111H-6_Trop",false}, //this is just the same as H-6_Hermann, below, but leaving that as the name to emphasize that it carries the Hermanns
			{"tobruk:Aircraft.He-111H-6_torpedo",true}, //special mod to load the H-6 with torpedos; type is corrected in Stb_AddLoadoutForPlane()
			{"tobruk:Aircraft.He-111H-6_Trop_torpedo",false}, //special mod to load the H-6 with torpedos; type is corrected in			Stb_AddLoadoutForPlane()
			{"bob:Aircraft.He-111P-2",true},			
			//{"tobruk:Aircraft.He-111H-6_Hermann1000kg",true}, //OK, this doesn't work because it has to match SUPPLY and changing that & this & keeping it coordinated is a pain
			//{"tobruk:Aircraft.He-111H-6_Trop_Hermann1000kg",false}, // ditto ^^^^
			{"tobruk:Aircraft.Ju-87B-2_Trop",false},
			{"bob:Aircraft.Ju-87B-2",true},
			{"bob:Aircraft.Ju-88A-1",true},			
			{"tobruk:Aircraft.Ju-88A-5",true},
			{"tobruk:Aircraft.Ju-88A-5_Trop",false},
			{"tobruk:Aircraft.Ju-88A-5Late",true},
			{"tobruk:Aircraft.Ju-88A-5Late_Trop",false},


			}
        }

        };



    private void setCoverAircraftCurrentlyAvailable_recurs()
    {
        Timeout(60 * 10.123125, () => setCoverAircraftCurrentlyAvailable_recurs());
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("CVCAXX1 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping
        setCoverAircraftCurrentlyAvailable();
    }

    private void setCoverAircraftCurrentlyAvailable()
    {
        //Console.WriteLine("Cover: Setting cover aircraft currently available");
        foreach (ArmiesE army in new List<ArmiesE> { ArmiesE.Blue, ArmiesE.Red })
        {
            //Console.WriteLine("Cover: Setting cover aircraft currently available for {0}", army);
            CoverAircraftCurrentlyAvailable[army] = new Dictionary<string, int>();
            foreach (string acName in CoverAircraftInitiallyAvailable[army].Keys)
            {
                //Console.WriteLine("Cover: Setting cover aircraft currently available for {0} {1} {2}", army, acName, CoverAircraftInitiallyAvailable[army][acName]);
                if (CoverAircraftInitiallyAvailable[army][acName])
                {
                    if (supplymission != null)
                    {
                        int numRemaining = supplymission.AircraftStockRemaining(acName, (int)army);
                        //Console.WriteLine("Cover: Setting cover aircraft currently available for {0} {1} {2} {3}", army, acName, CoverAircraftInitiallyAvailable[army][acName], numRemaining);
                        if (numRemaining > minimumAircraftRequiredForCoverDuty) CoverAircraftCurrentlyAvailable[army][acName] = numRemaining;

                    }
                    else CoverAircraftCurrentlyAvailable[army][acName] = 9999;
                }
            }
        }
    }

    //Returns shift_m (amount to shift this group right or left, in meters +/right or -/left), position slot (1 slot for each aircraft earlier on this list than this one, sorted into even=+/right and odd=-/left positions), the position of this airgroup in the list of its type for this player (bombers OR fighters), the position of this airgroup overall for this player (counting Bomber Groups AND fighter groups).
    //This is a simple/easy routine & we want to recalc it each time the cover/bomber a/c position & course is recalculated because it can change over time as aircraft or airgroups are added or crash/shot down, etc

    Dictionary<Player, int> playerFormationPosition = new Dictionary<Player, int>();//position of the PLAYER within the bomber formation. 
    public int getPlayerFormationPosition(Player player)
    {
        if (player == null) return 0;
        if (!playerFormationPosition.ContainsKey(player)) playerFormationPosition[player] = ran.Next(-2, 2);
        return playerFormationPosition[player];
    }

    public int setPlayerFormationPosition(Player player, int ps = -1000)
    {
        if (ps == -1000) ps = ran.Next(-2, 2);
        playerFormationPosition[player] = ps;
        return playerFormationPosition[player];
    }

    Dictionary<string, float> playerShiftFactor_pct = new Dictionary<string, float>();// Player name & percentage value to expand formation by, so 100, 150, 200, 300 etc for 100%, 200%, 300%
    float defaultAmtToShiftForEachBomber_m = 44;
    float defaultAmtToShiftForEachFighter_m = 30;
    float defaultAmtVerticleShift_m = 40;

    public float setShiftFactor(Player player, string shiftFactor_s)
    {
        float shiftFactor = 100;
        try { shiftFactor = Convert.ToInt32(shiftFactor_s); }
        catch (Exception ex) { shiftFactor = 100; }

        shiftFactor = setShiftFactor(player, shiftFactor);
        return shiftFactor;
    }

    public float setShiftFactor(Player player, float shiftFactor)
    {
        if (shiftFactor < 10) shiftFactor = 10;
        if (shiftFactor > 5000) shiftFactor = 5000;
        if (player != null && player.Name() != null) playerShiftFactor_pct[player.Name()] = shiftFactor;
        return shiftFactor;
    }

    public float getShiftFactor(Player player) {
        float shiftFactor = 100;
        if (player != null && player.Name() != null && playerShiftFactor_pct.ContainsKey(player.Name())) shiftFactor = playerShiftFactor_pct[player.Name()];
        if (shiftFactor < 10) shiftFactor = 10;
        if (shiftFactor > 3000) shiftFactor = 3000;

        return shiftFactor / 100;
    }

    //<cstrict - in strict formation the airgroups fly the standard (100%) formation spread, whatever the player
    //has set with <cdist, and they hold the leader's speed instead of the normal escort over-speed.
    public float strictFormationShiftFactor = 1.0f;   //1.0f = 100% = the standard formation spread
    public double strictSpeedMatchDistance_m = 30;  //as long as they are within this (front/back) distance of the leader, they match the leader's speed

    //How much FASTER than the leader cover a/c are asked to fly when they are BEHIND him, as a
    //multiplier on the leader's speed.  1.0 = exactly the leader's speed, which means never closing.
    //Applied to the two "settle/converge" targets in calcCoverSpeedToMatchMain() - the resting point
    //for a/c in behind, and the target of the sub-400m convergence override.
    //
    //Why 1.06 and not something tiny: measured on the test server, the aircraft only ever achieve
    //roughly 98% of the speed commanded in their waypoint (about 1.5-1.9 m/s short at 70-77 m/s), so
    //  closure rate = (commanded overspeed) - (~1.7 m/s shortfall)
    //With the old 0.999/0.9999 the commanded overspeed was only +0.9 to +2.4 m/s - i.e. LESS than the
    //shortfall - so net closure was +0.5 to -0.6 m/s and the formation just sat ~100m+ behind forever.
    //1.06 gives ~+4.5 m/s commanded, ~+3 m/s net, so 100m closes in about 30 seconds.  They then
    //settle slightly AHEAD of the leader, where the inFront braking bands pull them back.
    //NOTE: the inFront braking bands below are deliberately left below 1.0 - do NOT bias those.
    public double coverFormationSpeedBias = 1.06;

    //<cfdist - player-set forward/back offset for their formation, in metres.  Positive = the
    //formation is asked to ride this far AHEAD of the leader, negative = this far behind.
    //<cdist sets the left/right (lateral) spread; this is the front/back axis of the same idea.
    //Both axes meet in calcOffset_m(), which only knows about left/right.
    public Dictionary<Player, double> coverFrontBackDist_m = new Dictionary<Player, double>();

    //Per-airgroup speed calibration.  A cover group only ever achieves roughly 98% of the speed
    //commanded in its waypoint (see CloDNotes section 7), and that ratio drifts with altitude,
    //aircraft type and - most visibly - whether it is still carrying bombs.  Rather than hand-tune
    //one global number, learn each group's own ratio: compare the speed we LAST asked for against
    //the speed it actually flew, keep a rolling average of that, and scale the next request by
    //1/ratio, so the speed we WANT is the speed we GET.
    //Without this, a group running at 100% while the rest of the flight runs at 98% creeps away
    //from the leader and is never pulled back: the control loop only ever compares actual speed
    //against the LEADER's speed, never against its own request.
    public Dictionary<AiAirGroup, double> coverAGSpeedRatio = new Dictionary<AiAirGroup, double>();     //smoothed actual/requested; 1.0 = delivers exactly
    public Dictionary<AiAirGroup, double> coverAGSpeedRequested = new Dictionary<AiAirGroup, double>();  //the speed we last asked this group for
    public readonly double coverAGSpeedRatioMin = 0.85;   //never trust a ratio outside this band
    public readonly double coverAGSpeedRatioMax = 1.15;
    public readonly double coverAGSpeedRatioNewWeight = 0.3;  //weight of the newest sample in the rolling average (~7 samples to settle)

    //2026/10 - Step A4: the OLD sampler clamped out-of-band samples and folded them into the
    //rolling average - a mid-turn transient (ag_vel < 0.85 x lastAsked because the a/c is still
    //turning/climbing toward the previous command) poisoned the estimate for ~7 cycles.  Two
    //changes: (1) samples outside the trust band are DISCARDED (not clamped-in); (2) a sample is
    //only "honest" when the a/c is actually cruising AT the commanded speed - when it is
    //transiently far from it, it is a transient, not a delivery shortfall.  The denominator is
    //also now the FINAL clamped command, not the pre-clamp value.
    public readonly double coverAGSpeedRatioTransientDelta_mps = 4.0;  //|ag_vel - lastAsked| threshold for "cruising"
    public int coverAGSpeedRatioDiscardedCount = 0;  //diagnostic: samples discarded by the two rules above

    //<cdrop - snapshot of each player's airgroup orders, taken when DROP WHEN I DROP is switched
    //on, so switching it off again can put everyone back where they were.  Only groups whose order
    //is STILL .drop are reverted - so a <creserve N issued while drop mode is on is left alone.
    //Second value is the SCOPE of the snapshot: true = every group (bare "<cdrop" or the Tab-4 menu,
    //which can only express all-groups), false = only named squadrons ("<cdrop 3 6").  A <cnormal that
    //releases a group from <cstrict/<creserve re-joins it to drop mode only when scope is all-groups;
    //with a selective scope that <cnormal means "fly normal, stay out of the drop".
    public Dictionary<Player, Tuple<Dictionary<AiAirGroup, CoverAGOrders>, bool>> coverOrdersBeforeDrop = new Dictionary<Player, Tuple<Dictionary<AiAirGroup, CoverAGOrders>, bool>>();

    //<cfdist - this player's front/back offset, or 0 if they never set one.
    public double getFrontBackDist(Player player)
    {
        if (player != null && coverFrontBackDist_m.ContainsKey(player)) return coverFrontBackDist_m[player];
        return 0;
    }

    //Move a point forwards (+ve) or backwards (-ve) along the leader's heading, by offset_m metres.
    //Used to apply <cfdist to the formation's target point.  Vertical is left alone - that is the
    //job of calcOffset_m's up_down mode.
    public Point3d addFrontBackOffset(Point3d p, Vector3d leaderVwld, double offset_m)
    {
        if (Math.Abs(offset_m) < 0.001) return p;
        double vlen = CoverCalcs.distance(leaderVwld.x, leaderVwld.y);
        if (vlen < 0.0001) return p;   //leader not moving - no meaningful forward direction
        double f = offset_m / vlen;
        return new Point3d(p.x + leaderVwld.x * f, p.y + leaderVwld.y * f, p.z);
    }

    //BombSpacing (metres) written into the airgroup section of the sectionfile that spawns cover
    //aircraft, in Stb_LoadSubAircraft().
    //This is the difference between the aircraft walking its bombs across the target as a spaced-out
    //STICK, or dumping the whole load as one tight SALVO.  Without it they salvo: a Wellington puts
    //its whole load down in a single instant, so instead of the usual long stick running along the
    //target you get one clump on one small part of it - measured at only ~10% of an airfield
    //destroyed, versus the usual 50-60%.
    //The stock mission .mis files have carried "BombSpacing 20" for years and it is a big improvement
    //in nearly all situations; aircraft spawned at runtime were simply never getting it - BombSpacing
    //appears throughout the .mis files but was never written by any .cs.
    //Set at spawn, so it applies for that aircraft's whole life, not just in <cdrop.
    public int coverBombSpacing_m = 20;

    //Are these the strict (<cstrict/<cst) rigid formation orders?  The formation spacing & speed routines need
    //to know this, but they don't have the airgroup's orders to hand.
    public bool isInStrictFormation(AiAirGroup airGroup)
    {
        return (airGroup != null && coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.strict);
    }

    //The formation spread factor to use for this airgroup - <cstrict (<cst) airgroups always use the fixed,
    //standard spread instead of the player's <cdist setting
    public float getShiftFactorForAirgroup(Player player, AiAirGroup airGroup)
    {
        if (isInStrictFormation(airGroup)) return strictFormationShiftFactor;
        return getShiftFactor(player);
    }

    public Tuple<double, int, int, int> aircraftPositionAndNumber(AiAirGroup airGroup, Player player)
    {
        //Blenheim wingspan is 17m;     JU88 18m; HE111 22.5m; DO217 19 m; Wellington 26 m
        //Beaufighter 17 m; HE110 16.25
        float shiftFactor = getShiftFactorForAirgroup(player, airGroup); //<cstrict airgroups use the standard formation spread, ignoring the player's <cdist setting

        float amtToShiftForEachBomber_m = defaultAmtToShiftForEachBomber_m * shiftFactor;        
        float amtToShiftForEachFighter_m = defaultAmtToShiftForEachFighter_m * shiftFactor;
        int playerSpot = getPlayerFormationPosition(player); //where the player sits in the formation - center, 1-2-3 spots left, 1-2-3 spots right etc.
        
        int totalRight = 1; //1 for the position taken in the center by the player's bomber; also keeps the first left & first right a/c from occupying the same spot ( in the center )
        int totalLeft = 1; //1 for the position taken in the center by the player's bomber
        //note this isn't actually technically the CENTER any more; it is the player's
        //position.  We might pile on more a/c to the left or right first, before alternating
        int count = 0;  //count of a/c of this type (fighter or bomber)
        int allcount = 0; //count of all a/c for this player
        int pos = 0;
        bool type = isHeavyBomber(airGroup) || isDiveBomber(airGroup); //whether bomber or fighter
        foreach (AiAirGroup ag in coverAircraftAirGroupsActive.Keys)
        {
            if (coverAircraftAirGroupsActive[airGroup] != player) continue;
            if (airGroup.GetItems() == null || airGroup.GetItems().Length == 0) continue;

            
            allcount++;
            bool agtype = isHeavyBomber(ag) || isDiveBomber(ag); //whether bomber or fighter.  We're counting dive bombers as bombers now.
            if (agtype != type) continue;
            count++;

            //putting even numbered ac/groups to the right of the main a/c; odd numbered to the left EXCEPT the first ones before playerSpot al go left OR all go right
                 

            if ((count < playerSpot || count % 2 == 0) && -count < playerSpot)
            {
                totalRight += ag.GetItems().Length;// ag.NOfAirc;
                pos = totalRight;
            }
            else
            {
                totalLeft += ag.GetItems().Length;// ag.NOfAirc;
                pos = -totalLeft;
            }
            if (ag == airGroup) break;
        }
        

        double shiftamt = amtToShiftForEachBomber_m;
        if (!type) shiftamt = amtToShiftForEachFighter_m;
        double shift_m = pos * shiftamt;
        //if (mainmission.ON_TESTSERVER) GamePlay.gpLogServer(new Player[] { player }, "ACPos: {0:F1} {1} {2} {3} : {4} ", new object[] { shift_m, pos, count, allcount, airGroup.Name() }); 

        return new Tuple<double, int, int, int>(shift_m, pos, count, allcount);


    }

    //so, we shift the escort fighter or bomber aircraft left or right a bit to allow all groups to have some horizontal space.
    //When we go to bomber targeting mode (knickebein point ON), the bombers can't shift their target point left/right or they'll miss the target point.  And that target point is the same OR very close for all bomber groups.  So in that case we shift them up/down a bit  in altitude, so they can more easily avoid crashing into each other, while all still targeting the same target point.
    public enum offsetDirection { left_right, up_down };
    public Point3d calcOffset_m(Point3d CurrentPos, AiAirGroup airGroup, Player player, Vector3d Vwld, double vel_mps, offsetDirection dir = offsetDirection.left_right, float amtToShiftWhenTargeting = 0)
    {
        //if (player==null || airGroup==null || player)
        float shiftFactor = getShiftFactorForAirgroup(player, airGroup);

        string actype = Calcs.GetAircraftType(airGroup);
        //double vertShiftFactor = 1.0f;
        //if (actype.ToLower().Contains("wellington")) vertShiftFactor = 3.0f;  //extra vertical shift for wellies

        Tuple<double, int, int, int> shifts = aircraftPositionAndNumber(airGroup, player);
        double shift_m = shifts.Item1;
        //double shiftvert_m = shifts.Item2 * 40;

        //float amtVerticleShift_m = defaultAmtVerticleShift_m * shiftFactor; //not doing this; really not needed ?  Or if needed, it could be a reduced amount?

        double shiftvert_m = (shifts.Item2 % 2 * 2 * defaultAmtVerticleShift_m - defaultAmtVerticleShift_m) * (Math.Sign(shift_m)); //up-down-up-down pattern on each side, but swapped direction left/right sides
        if (dir == offsetDirection.up_down) shiftvert_m *= shifts.Item2; //when doing verticle shift, we make the shift larger as we move "outward" in the formation

        Point3d unit_vector_vel_m = new Point3d(1, 0, 0);
        if (dir == offsetDirection.up_down) unit_vector_vel_m = new Point3d(0, 0, 1);
        if (vel_mps != 0) unit_vector_vel_m = new Point3d(Vwld.x / vel_mps, Vwld.y / vel_mps, 0);  //This is unit vector in the direction the  main a/c is traveling. vel_mps is from CalculatePointDistance which calcs only x/y velocity, neglecting the z component entirely

        Point3d unit_vector_90deg_vel_m;
        if (dir == offsetDirection.left_right)
            unit_vector_90deg_vel_m = new Point3d(-unit_vector_vel_m.y * shift_m, unit_vector_vel_m.x * shift_m, shiftvert_m); //this is a unit vector (1m) pointing 90 degrees rightwards of the a/c direction vector, multiplied by shift_m; also trying a vertical shift per airgroup to see if a bit of vertical separation helps avoid crashes
        else
            unit_vector_90deg_vel_m = new Point3d(0, 0, shiftvert_m); //this is a unit vector (1m) pointing 90 degrees upwards of the a/c direction vector, multiplied by shiftvert_m
        return new Point3d(CurrentPos.x + unit_vector_90deg_vel_m.x, CurrentPos.y + unit_vector_90deg_vel_m.y, CurrentPos.z + unit_vector_90deg_vel_m.z); // now add this vector/point to the currentpos point).  

    }

    public Dictionary<Tuple<Player, AiAirGroup, string, double>, Point3d> storedRollingAverages = new Dictionary<Tuple<Player, AiAirGroup, string, double>, Point3d>();

    public Point3d storedRollingAverage(Player player, AiAirGroup airGroup, string type, Vector3d newpoint, double rolls)
    {
        var key = new Tuple<Player, AiAirGroup, string, double>(player, airGroup, type, rolls);
        if (!storedRollingAverages.Keys.Contains(key))
        {
            Point3d p = new Point3d(newpoint.x, newpoint.y, newpoint.z);
            storedRollingAverages[key] = p;
            return p;
        }

        Point3d res = CoverCalcs.rollingAverage(storedRollingAverages[key], newpoint, rolls);
        storedRollingAverages[key] = res;
        return res;
    }

    public Dictionary<Player, System.Threading.Timer> COVER_ListPositionTimer = new Dictionary<Player, System.Threading.Timer>();
    public readonly int COVER_ListPositionTimerPeriod_ms = 20154; //20 sec

    /*************************************************************
    // <cdrop - "DROP WHEN I DROP"
    //
    //Mimics ww2 practice, where only the leader carried a bombsight and the rest of the formation
    //just watched him and pulled their own release when he pulled his.
    //
    //How it works:
    //  1. a fast timer watches the LEADER's bomb count.  There is no bomb-release event in CLoD (the
    //     only bomb callback, OnBombExplosion, fires on explosion not release) so we poll
    //     CoverCalcs.bombCount() instead - see Genghis-Class-CloDNotes.cs section 3.
    //  2. when the count goes DOWN the leader has dropped, so we record a "drop line": the leader's
    //     position + his heading at that instant.  That line is where the rest of the formation
    //     wants to be when they pull.
    //  3. each airgroup then either drops straight away (if it is already close to the leader) or, if
    //     it is further back, keeps flying normally until it reaches that line and drops there.
    //     Either way they all release over the same stretch of ground.
    //
    //One release per pass: we latch after the first drop, so a multi-salvo release by the leader does
    //not make them dribble their bombs out one group at a time.  Re-issue <cdrop to re-arm.
    //<reserve still works as an escape hatch - it takes a squadron out of this mode entirely.
    *************************************************************/

    public Dictionary<Player, System.Threading.Timer> COVER_DropWatchTimer = new Dictionary<Player, System.Threading.Timer>();
    public readonly int COVER_DropWatchPeriod_ms = 750; //0.75 sec - leader's drop detected within ~1 sec

    //<cdrop 2026/10 Step C4 - this is now only a SAFETY FLOOR, not the main gate.  The main gate is the
    //bay lead-in in coverDropReleasePass: an airgroup is told to drop when it comes within
    //coverDropBayLead_s x its OWN speed SHORT of the leader's drop line, so the bomb bays are open by
    //the time it arrives on the line.  This floor catches a group practically touching the leader, so
    //nothing can ever be left holding its bombs if the along-track maths is ever off.  It used to be
    //1500m and WAS the main gate - which is exactly why "drop when I drop" released 5-15s late and
    //the stick landed well past the leader's (2026/10 DROPTRACE bay= columns).
    public double coverDropImmediateDist_m = 300;

    //<cdrop 2026/10 Step C7 - HOW FAR BEHIND the group the release GATTACK_POINT sits.  The reference
    //   test (Genghis\Reliable-instant-bomb-drop) used 10m behind and reported 50m behind behaves the
    //   same, so the distance genuinely does not matter - kept at 50m for a little separation from
    //   the zero-length leading NORMFLY (see CloDNotes 1, "WAYPOINT SPACING").
    public double coverDropAttackGap_m = 50;

    //<cdrop 2026/10 tunables (all public so they can be tweaked in one place).
    //HOW FAR AHEAD of the leader's drop line an airgroup may be and still be told to drop.  A group
    //further ahead than this is SKIPPED for the pass - see coverDropReleasePass().
    public double coverDropMaxAhead_m = 1000;

    //HOW FAR SHORT of the drop line we command the release so the bomb bays have time to open.  Only
    //used for a group whose doors are NOT already open; with the doors open the release is instant and
    //a lead would just bias the stick short of the leader's line.  Measured: ~6s (Wellington),
    //~10-15s (Ju-88) - see the DROPTRACE "bay=" column.
    public double coverDropBayLead_s = 6;

    //BOMB-BAY DOORS: there is no write-parameter API on AiAircraft (CloDNotes 4/9b), so the only lever
    //   on the door cycle is to keep an attack waypoint live so the engine opens the doors itself, and
    //   then WAIT until A_BombBayDoor actually reads open before releasing.
    public double coverDropBayOpenThreshold = 0.98;   //A_BombBayDoor at/above this counts as OPEN
    //escape hatch: normally a group is released as soon as its doors are open, with NO timeout, so a
    //   group held back for its doors is never skipped.  But if the doors never register open at all we
    //   must not hold its bombs forever - issue anyway after this long, and log it loudly.
    public double coverDropBayWaitFallback_s = 30;
    //a group we deliberately HELD BACK for its doors will have flown on well past the line, so it gets
    //   this much more room than a group that was ready at the time.
    public double coverDropMaxAheadWait_m = 3000;

    //PRE-OPEN the bomb bays: while DROP WHEN I DROP is armed we keep each bomber pointed at a decoy
    //GATTACK_POINT this far ahead (re-issued every cycle, so they never reach it) purely to hold the
    //bays open, then swap to the real release geometry the instant the leader drops.  The engine only
    //opens the bays once an attack waypoint is current, and there is no write-parameter API on
    //AiAircraft (CloDNotes 4/9b), so this is the only lever we have on the 5-15s door cycle.
    //ON by default: this is now the PROVEN mechanism (see the reference test in
    //Genghis\Reliable-instant-bomb-drop) rather than an experiment - keeping an attack waypoint live is
    //what makes every bomber type dump its whole load.  The risk it carries is bomb-run attitude
    //instead of tight formation, which is worth watching in a test session.
    public static bool cdDropPreOpenBays = true;
    public double coverDropPreOpen_m = 10000;

    //we latch after the first drop so one leader pass = one formation release.  Reset by <cdrop.
    Dictionary<Player, bool> coverDropAlreadyFired = new Dictionary<Player, bool>();
    Dictionary<Player, Point3d> coverDropLinePoint = new Dictionary<Player, Point3d>();
    Dictionary<Player, Point3d> coverDropLineDir = new Dictionary<Player, Point3d>();   //unit vector of leader's heading
    Dictionary<Player, int> coverDropLastLeaderBombCount = new Dictionary<Player, int>();

    //airgroups we have already told to drop, and when - so keepAircraftOnTask_recurs() can leave
    //their new flight plan alone long enough for the release to actually happen (that routine runs
    //every ~16s and would otherwise overwrite it - see Genghis-Class-CloDNotes.cs section 2).
    Dictionary<AiAirGroup, DateTime> coverAircraftAirGroupsDropIssued = new Dictionary<AiAirGroup, DateTime>();

    //<cdrop - ONE ISSUE PER PASS.  This is deliberately a SEPARATE thing from
    //coverAircraftAirGroupsDropIssued, which is only the 25s hold-off that stops
    //keepAircraftOnTask_recurs() from overwriting the release flight plan.
    //Those two were the same dictionary, with the following catastrophic result:
    //  coverDropReleasePass() removed the entry after coverDropHoldFlightPlan_s (25s) and re-issued
    //  the drop plan, while keepAircraftOnTask_recurs() early-returns while the entry is <25s old.
    //  Refreshing it every ~25.5s meant that early-return won on essentially EVERY call, so no
    //  formation waypoints were ever issued again and calcCoverSpeedToMatchMain() was never called -
    //  measured in the test log as a 413s (6.9 min) silent gap in COVERSPEED, followed by another
    //  138s gap, with the group flying a straight-line bombing run the whole time.  That is what
    //  sent the bombers off to the side, without speed control, several km behind.
    //This latch is what the documentation always claimed: one release per pass, cleared when the
    //player re-arms with <cdrop.
    Dictionary<AiAirGroup, Player> coverAircraftAirGroupsDroppedThisPass = new Dictionary<AiAirGroup, Player>();
    public readonly double coverDropHoldFlightPlan_s = 25; //how long we leave the drop flight plan in place

    //Start (or re-arm) the <cdrop watcher for this player.  Records the leader's CURRENT bomb count
    //immediately, so a drop in the first fraction of a second after the order is not missed.
    public void armCoverDropWatch(Player player)
    {
        if (player == null) return;
        try
        {
            turnOffCoverDropWatch(player);

            AiAircraft pa = player.Place() as AiAircraft;
            if (pa != null)
            {
                coverDropLastLeaderBombCount[player] = CoverCalcs.bombCount(pa);
            }
            coverDropAlreadyFired[player] = false;
            coverDropLinePoint.Remove(player);
            coverDropLineDir.Remove(player);

            COVER_DropWatchTimer[player] = new System.Threading.Timer(
                coverDropWatch_obj, player, dueTime: 100, period: COVER_DropWatchPeriod_ms);
        }
        catch (Exception ex) { Console.WriteLine("Cover armCoverDropWatch ERROR! " + ex.ToString()); }
    }

    public void turnOffCoverDropWatch(Player player)
    {
        if (player == null) return;
        try
        {
            if (COVER_DropWatchTimer.ContainsKey(player))
            {
                if (COVER_DropWatchTimer[player] != null) COVER_DropWatchTimer[player].Dispose();
                COVER_DropWatchTimer.Remove(player);
            }
            coverDropAlreadyFired.Remove(player);
            coverDropLinePoint.Remove(player);
            coverDropLineDir.Remove(player);
            coverDropLastLeaderBombCount.Remove(player);
            //Release the per-pass latch and the release-hold entries for THIS player's groups.
            //armCoverDropWatch() calls us first, so re-arming with <cdrop cleanly re-opens the pass
            //and they may drop again.  The hold-off entries must go player-by-player (the dictionary
            //is keyed by airgroup across ALL players): clearing it wholesale would lift another
            //player's drop protection, letting keepAircraftOnTask_recurs() overwrite their release
            //flight plan before the bombs go.  Both are also cleared per-group by forgetAirGroup()
            //when the group itself goes away - this is the player-level equivalent.
            List<AiAirGroup> latchedThisPass = new List<AiAirGroup>(coverAircraftAirGroupsDroppedThisPass.Keys);
            foreach (AiAirGroup agL in latchedThisPass)
            {
                if (agL == null) continue;
                if (coverAircraftAirGroupsDroppedThisPass[agL] == player) coverAircraftAirGroupsDroppedThisPass.Remove(agL);
            }
            List<AiAirGroup> heldRelease = new List<AiAirGroup>(coverAircraftAirGroupsDropIssued.Keys);
            foreach (AiAirGroup agH in heldRelease)
            {
                if (agH == null) continue;
                if (!coverAircraftAirGroupsActive.ContainsKey(agH)) { coverAircraftAirGroupsDropIssued.Remove(agH); continue; }
                if (coverAircraftAirGroupsActive[agH] == player) coverAircraftAirGroupsDropIssued.Remove(agH);
            }
            //<cdrop Step C8 - and stop timing their bomb-bay doors, player by player, for the same
            //reason (the dictionary is keyed by airgroup across ALL players).
            List<AiAirGroup> bayWaiters = new List<AiAirGroup>(coverAircraftAirGroupsBayWaitSince.Keys);
            foreach (AiAirGroup agB in bayWaiters)
            {
                if (agB == null) continue;
                if (!coverAircraftAirGroupsActive.ContainsKey(agB)) { coverAircraftAirGroupsBayWaitSince.Remove(agB); continue; }
                if (coverAircraftAirGroupsActive[agB] == player) coverAircraftAirGroupsBayWaitSince.Remove(agB);
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover turnOffCoverDropWatch ERROR! " + ex.ToString()); }
    }

    public void coverDropWatch_obj(object ob)
    {
        try
        {
            Player player = ob as Player;
            if (player == null) return;
            coverDropWatch(player);
        }
        catch (Exception ex) { Console.WriteLine("Cover coverDropWatch_obj ERROR! " + ex.ToString()); }
    }

    public void coverDropWatch(Player player)
    {
        try
        {
            if (player == null) return;

            //still using <cdrop at all?  if they changed order, stand ourselves down.
            bool stillWanted = false;
            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null) continue;
                if (coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.drop)
                {
                    stillWanted = true;
                    break;
                }
            }
            if (!stillWanted) { turnOffCoverDropWatch(player); return; }

            AiAircraft pa = player.Place() as AiAircraft;
            if (pa == null || pa.AirGroup() == null)
            {
                turnOffCoverDropWatch(player);   //leader out of his aircraft - nothing to follow
                return;
            }

            //------------------------------------------ did the leader just drop?
            //NB: no hasBombs() backstop here.  It has been seen reporting "Has bombs" on an aircraft that
            //had already released everything, so it is not trustworthy enough to gate a release on -
            //and a false "he still has bombs" would suppress the drop for the whole pass.  bombCount()
            //is the signal, and its unused slots read a clean 0 (so there is no exception noise either).
            int bombsNow = CoverCalcs.bombCount(pa);

            bool justDropped = false;
            int last = bombsNow;
            if (coverDropLastLeaderBombCount.ContainsKey(player))
            {
                last = coverDropLastLeaderBombCount[player];
                //count going DOWN is the reliable signal - it catches EVERY salvo, not just the last
                if (bombsNow < last) justDropped = true;
            }
            coverDropLastLeaderBombCount[player] = bombsNow;

            bool alreadyFired = coverDropAlreadyFired.ContainsKey(player) && coverDropAlreadyFired[player];

            if (justDropped && !alreadyFired)
            {
                alreadyFired = true;
                coverDropAlreadyFired[player] = true;

                //record the line he dropped on: his position + his heading, both right now
                Point3d p = pa.Pos();
                Vector3d vwld = pa.AirGroup().Vwld();
                double vlen = CoverCalcs.CalculatePointDistance(vwld);
                Point3d dir = new Point3d(0, 1, 0);
                if (vlen > 0.1) dir = new Point3d(vwld.x / vlen, vwld.y / vlen, 0);
                coverDropLinePoint[player] = p;
                coverDropLineDir[player] = dir;

                if (mainmission.ON_TESTSERVER)
                    Console.WriteLine("COVER <cdrop: {0} dropped ({1}->{2} bombs).  Line at {3:n0} {4:n0}, heading {5:n0} {6:n0}",
                        player.Name(), last, bombsNow, p.x, p.y, dir.x, dir.y);

                //DIAGNOSTIC - stamp the moment we DETECTED the release, so the DROPTRACE "issue" lines
                //that follow give us detect->issue latency on top of issue->actual-release latency.
                if (mainmission.ON_TESTSERVER)
                    Console.WriteLine("DROPTRACE detect t={0:HH:mm:ss.fff} player={1} bombs={2}->{3} immediateDist_m={4:N0}",
                        DateTime.UtcNow, player.Name(), last, bombsNow, coverDropImmediateDist_m);
            }

            if (!alreadyFired) return;  //he hasn't dropped yet - they just keep flying formation

            coverDropReleasePass(player, pa);
        }
        catch (Exception ex) { Console.WriteLine("Cover coverDropWatch ERROR! " + ex.ToString()); }
    }

    //<cdrop 2026/10 Step C8 - WHEN did we start holding this group back because its doors were shut?
    //cleared as soon as it is released, and on re-arm.  A group that is in here has flown on while it
    //waited, so it gets a much more generous "too far ahead" bound than one that was ready at the time.
    Dictionary<AiAirGroup, DateTime> coverAircraftAirGroupsBayWaitSince = new Dictionary<AiAirGroup, DateTime>();

    //<cdrop 2026/10 Step D - the release pass used to be made entirely of SILENT continues.  In log
    //02F the player dropped, "detect" fired, and no group was ever told to release - with nothing at
    //all in the log to say which of the (many) skip conditions swallowed it.  cdDropPassLog() makes
    //every decision self-describing, throttled to one line per group per 5s so a 750ms poll cannot flood.
    Dictionary<AiAirGroup, DateTime> coverDropPassLogged = new Dictionary<AiAirGroup, DateTime>();
    public void cdDropPassLog(AiAirGroup airGroup, string why, bool always = false)
    {
        if (!mainmission.ON_TESTSERVER) return;
        try
        {
            if (airGroup == null) return;
            if (!always)
            {
                DateTime last;
                if (coverDropPassLogged.TryGetValue(airGroup, out last) && (DateTime.UtcNow - last).TotalSeconds < 5) return;
            }
            coverDropPassLogged[airGroup] = DateTime.UtcNow;
            Console.WriteLine("COVER <cdrop PASS: {0} - {1}", airGroup.Name(), why);
        }
        catch (Exception ex) { }
    }

    //A_BombBayDoor of the group's first aircraft, or -1 if it cannot be read.  READ ONLY - there is no
    //write-parameter API on AiAircraft (CloDNotes 4/9b), so this is how we know when the doors are open.
    public double coverDropBayDoorPos(AiAirGroup airGroup)
    {
        try
        {
            if (airGroup == null) return -1;
            if (airGroup.GetItems() == null || airGroup.GetItems().Length == 0) return -1;
            AiAircraft a = airGroup.GetItems()[0] as AiAircraft;
            if (a == null) return -1;
            return a.getParameter(part.ParameterTypes.A_BombBayDoor, 0);
        }
        catch (Exception ex) { return -1; }
    }

    Dictionary<AiAirGroup, DateTime> coverRtbLogged = new Dictionary<AiAirGroup, DateTime>();

    //<cdrop 2026/10 Step D - RTB PROBE.  An airgroup in task .RETURN will neither open its bomb bays
    //nor release, so a group that has quietly gone RTB looks exactly like a group ignoring us - and
    //we do not yet know WHY the engine flips them (CloDNotes 1f lists the candidates).  Log enough
    //state to tell them apart next time.  Throttled to one line per group per 30s.
    public void cdRtbProbe(AiAirGroup airGroup, Player player)
    {
        if (!mainmission.ON_TESTSERVER) return;
        try
        {
            if (airGroup == null) return;
            if (airGroup.getTask() != AiAirGroupTask.RETURN) { coverRtbLogged.Remove(airGroup); return; }
            DateTime last;
            if (coverRtbLogged.TryGetValue(airGroup, out last) && (DateTime.UtcNow - last).TotalSeconds < 30) return;
            coverRtbLogged[airGroup] = DateTime.UtcNow;
            AiWayPoint[] rwps = airGroup.GetWay();
            int cur = airGroup.GetCurrentWayPoint();
            double fuel = -1;
            if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
            {
                AiAircraft fa = airGroup.GetItems()[0] as AiAircraft;
                //NB: there is no ParameterTypes.Fuel in this build; S_FuelReserve is the closest readable proxy for
                //"is it running dry", which is one of the candidate causes of a group going RTB.
                if (fa != null) { try { fuel = fa.getParameter(part.ParameterTypes.S_FuelReserve, 0); } catch (Exception exF) { } }
            }
            double distToLeader = -1;
            if (player != null && player.Place() != null) distToLeader = CoverCalcs.CalculatePointDistance(airGroup.Pos(), player.Place().Pos());
            Console.WriteLine("COVER RTB: {0} is in task .RETURN - waypoints={1} current={2} bombs={3} fuel={4:F0} distToLeader={5:n0}m releasedByUs={6}",
                airGroup.Name(), (rwps != null ? rwps.Length : 0), cur, CoverCalcs.bombCount(airGroup), fuel, distToLeader,
                (coverAircraftAirGroupsReleased.ContainsKey(airGroup) && coverAircraftAirGroupsReleased[airGroup]));
        }
        catch (Exception ex) { }
    }

    //Is this group still flying OUR release plan?  We issued NORMFLY + GATTACK_POINT + a long
    //NORMFLY + a tail.  If no GATTACK_POINT/GATTACK_TARG is anywhere in its current plan, something
    //else rewrote it (a plain FOLLOW plan) and the release will never happen - see the Step C3
    //self-heal in coverDropReleasePass().  Returns TRUE on any error, so a transient failure never
    //triggers a needless re-issue that would restart the bomb-bay cycle.
    public bool dropPlanStillInForce(AiAirGroup airGroup)
    {
        try
        {
            if (airGroup == null) return false;
            AiWayPoint[] wps = airGroup.GetWay();
            if (wps == null || wps.Length == 0) return false;
            int cw = airGroup.GetCurrentWayPoint();
            if (cw < 0) return false;
            for (int i = cw; i < wps.Length; i++)
            {
                AiAirWayPoint w = wps[i] as AiAirWayPoint;
                if (w == null) continue;
                if (w.Action == AiAirWayPointType.GATTACK_POINT || w.Action == AiAirWayPointType.GATTACK_TARG) return true;
            }
            return false;
        }
        catch (Exception ex) { Console.WriteLine("Cover dropPlanStillInForce ERROR! " + ex.ToString()); return true; }
    }

    //Second half of <cdrop: the leader has dropped, so walk each of his airgroups and decide whether it
    //pulls now or waits until it reaches the drop line.
    //  - close to the leader, or already at/ahead of the line  ->  drop now
    //  - further back                                      ->  keep flying normally, try again next tick
    public void coverDropReleasePass(Player player, AiAircraft leaderAircraft)
    {
        Point3d lineP;
        Point3d lineD;
        if (!coverDropLinePoint.ContainsKey(player) || !coverDropLineDir.ContainsKey(player)) return;
        lineP = coverDropLinePoint[player];
        lineD = coverDropLineDir[player];

        DateTime nowUtc = DateTime.UtcNow;
        bool anyStillArmed = false;
        bool issuedAnyThisPass = false;

        List<AiAirGroup> groups = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
        foreach (AiAirGroup airGroup in groups)
        {
            if (airGroup == null || airGroup.GetItems() == null || airGroup.GetItems().Length == 0) continue;
            if (coverAircraftAirGroupsActive[airGroup] != player) continue;
            if (!coverAircraftAirGroupsOrders.ContainsKey(airGroup) || coverAircraftAirGroupsOrders[airGroup] != CoverAGOrders.drop)
            {
                cdDropPassLog(airGroup, "SKIP: not on DROP WHEN I DROP orders (on " + (coverAircraftAirGroupsOrders.ContainsKey(airGroup) ? coverAircraftAirGroupsOrders[airGroup].ToString() : "no order set") + ")");
                continue;
            }

            //only the ones that can actually bomb.  Skip torps: a torpedo pulled off a GATTACK_POINT
            //at formation altitude is a dud, and the He-111 torpedo conversions have their own attack
            //logic we don't want to disturb here.
            if (!isBomberArmed(airGroup)) { cdDropPassLog(airGroup, "SKIP: has no bombs to release"); continue; }
            if (airGroup.hasTorpedos()) { cdDropPassLog(airGroup, "SKIP: carries torpedoes"); continue; }

            //<cdrop - ONE ISSUE PER PASS.  If we have already handed this group its release plan for
            //this pass, leave it alone completely: do not re-issue, and do not count it as
            //outstanding either.  (Re-issuing is what caused the control blackout - see the comment
            //on coverAircraftAirGroupsDroppedThisPass.)  A latched group is DONE, so it must not
            //set issuedAnyThisPass, or the watcher would never stand down.
            if (coverAircraftAirGroupsDroppedThisPass.ContainsKey(airGroup)) { cdDropPassLog(airGroup, "SKIP: already released this pass (one release per pass - re-arm with <cdrop)"); continue; }

            //The 25s hold-off: our release plan stays in force and keepAircraftOnTask_recurs()
            //early-returns while this is set.  It is written ONCE now and never refreshed, so it
            //expires naturally after coverDropHoldFlightPlan_s and formation control resumes.
            if (coverAircraftAirGroupsDropIssued.ContainsKey(airGroup))
            {
                double heldFor_s = (nowUtc - coverAircraftAirGroupsDropIssued[airGroup]).TotalSeconds;
                if (heldFor_s < coverDropHoldFlightPlan_s)
                {
                    issuedAnyThisPass = true;   //just issued, still holding the plan - keep the watcher alive

                    //<cdrop 2026/10 Step C3 - SELF-HEAL.  The 2026-10-02 logs show the release plan
                    //being replaced by a plain 3-waypoint FOLLOW plan a few seconds after issue, which
                    //the 25s hold-off in keepAircraftOnTask_recurs() is supposed to make impossible:
                    //one Ju-88 was back on FOLLOW ~6s after issue, and one Wellington never released at
                    //all (its bay= stayed 0.000 for 30s).  If the group still has bombs and its plan no
                    //longer contains an attack waypoint, the plan was lost - put it back.  Re-issuing
                    //restarts the bay cycle, so only do it once the plan is demonstrably gone (past 3s,
                    //so we are not fighting the issue itself) and inside a bounded window, which also
                    //stops this from looping.
                    if (heldFor_s > 3 && heldFor_s < 15 && isBomberArmed(airGroup) && !dropPlanStillInForce(airGroup))
                    {
                        AiAirGroup ag3 = airGroup;
                        Timeout(0.05, () => dropBombsNow_airGroup(ag3, leaderAircraft != null ? leaderAircraft.AirGroup() : null));
                        coverAircraftAirGroupsDropIssued[airGroup] = nowUtc;   //restart the window for the new plan
                        if (mainmission.ON_TESTSERVER)
                            Console.WriteLine("COVER <cdrop: RE-ASSERTED the lost release plan for " + airGroup.Name() + " (" + heldFor_s.ToString("F1") + "s after issue, " + CoverCalcs.bombCount(airGroup) + " bombs left)");
                    }
                    continue;
                }
                coverAircraftAirGroupsDropIssued.Remove(airGroup);
            }

            Point3d ap = airGroup.Pos();
            double distToLeader_m = CoverCalcs.CalculatePointDistance(ap, leaderAircraft.Pos());

            //how far PAST the drop line are we?  +ve = at/ahead of it, -ve = still behind it
            double pastLine_m = (ap.x - lineP.x) * lineD.x + (ap.y - lineP.y) * lineD.y;

            //<cdrop 2026/10 Step C8 - WAIT FOR THE BOMB-BAY DOORS.  This is the whole point of the reference
            //test in Genghis\Reliable-instant-bomb-drop: if the release plan arrives while the doors
            //are still shut, the attack waypoint is consumed before they open, the engine works out
            //an "already passed" solution, and the group lets go ONE salvo and latches - which is
            //exactly the 54->51 we kept seeing.  With the doors ALREADY OPEN (which cdDropPreOpenBays
            //arranges by keeping an attack waypoint live) it dumps its WHOLE load the moment the
            //GATTACK_POINT is reached.  So: release groups whose doors are open, and WAIT for the rest
            //with no timeout - the watcher re-polls every 750ms.  The only timeout is a deadlock
            //escape for a type whose door parameter never moves at all.
            double bayPos = coverDropBayDoorPos(airGroup);
            bool doorsOpen = (bayPos >= coverDropBayOpenThreshold);
            bool everWaited = coverAircraftAirGroupsBayWaitSince.ContainsKey(airGroup);
            if (!doorsOpen)
            {
                if (!everWaited) { coverAircraftAirGroupsBayWaitSince[airGroup] = nowUtc; everWaited = true; }
                double waited_s = (nowUtc - coverAircraftAirGroupsBayWaitSince[airGroup]).TotalSeconds;
                if (waited_s < coverDropBayWaitFallback_s)
                {
                    anyStillArmed = true;   //keep the watcher alive; we come back the instant it opens
                    cdDropPassLog(airGroup, "HELD: bomb-bay doors shut, bay=" + bayPos.ToString("F3") + " (waited " + waited_s.ToString("F0") + "s)");
                    continue;               //NOTE: deliberately before the ahead-gate - we are the ones
                                            //holding it, so we must not then blame it for being late
                }
                if (mainmission.ON_TESTSERVER)
                    Console.WriteLine("COVER <cdrop: {0} bomb-bay doors never opened (bay={1:F3}) after {2:F0}s - releasing anyway",
                        airGroup.Name(), bayPos, waited_s);
            }

            //<cdrop 2026/10 - too far AHEAD of the leader's drop line.  Releasing out there plants the
            //stick a long way in front of the leader's, outside the formation, and the group is still
            //moving further away, so waiting never helps - skip it for this pass.  Deliberately does
            //NOT set anyStillArmed: a group this far ahead will never close, so the watcher should stand
            //down instead of polling forever.  (An aircraft AHEAD but within coverDropMaxAhead_m still
            //drops with the leader, which is what the player asked for.)
            double maxAhead_m = everWaited ? coverDropMaxAheadWait_m : coverDropMaxAhead_m;
            if (pastLine_m > maxAhead_m)
            {
                cdDropPassLog(airGroup, "SKIP: " + pastLine_m.ToString("F0") + "m ahead of the drop line (limit " + maxAhead_m.ToString("F0") + "m)");
                continue;
            }

            //<cdrop 2026/10 Step C4 - BAY LEAD-IN.  The engine opens the bomb bays over several seconds
            //before it lets go (measured from the DROPTRACE bay= column: ~0 -> 1 over ~3-6s on a
            //Wellington, ~10-15s on a Ju-88), so a group commanded the instant it REACHES the line
            //still releases late and its stick lands well past the leader's.  So command the release
            //this far SHORT of the line that the door cycle finishes as the group arrives on it.
            //lead_m is derived from the group's OWN speed, so a fast group automatically gets a
            //longer lead.  pastLine_m is +ve at/ahead of the line, so >= -lead_m means "abreast, or
            //within one door-cycle of it".
            double grpSpeed_mps = CoverCalcs.CalculatePointDistance(airGroup.Vwld());
            if (grpSpeed_mps < 1) grpSpeed_mps = 1;
            double lead_m = doorsOpen ? 0 : (coverDropBayLead_s * grpSpeed_mps);

            //plus coverDropImmediateDist_m as a pure safety floor for a group practically on top of
            //the leader (see its comment above - it is 300m, not the old 1500m).
            bool dropNow = (pastLine_m >= -lead_m) || (distToLeader_m <= coverDropImmediateDist_m);

            if (!dropNow)
            {
                anyStillArmed = true;
                cdDropPassLog(airGroup, "WAIT: " + (-pastLine_m).ToString("F0") + "m short of the drop line (bay " + (doorsOpen ? "open" : "shut") + ", lead " + lead_m.ToString("F0") + "m, " + distToLeader_m.ToString("F0") + "m from you)");
                continue;
            }  //further back - wait for the lead line

            //game objects want touching on the mission thread, not on the timer thread.
            //NB a small non-zero delay, as elsewhere in this file - not Timeout(0, ...).
            AiAirGroup ag2 = airGroup;
            Timeout(0.05, () => dropBombsNow_airGroup(ag2, leaderAircraft != null ? leaderAircraft.AirGroup() : null));
            coverAircraftAirGroupsDropIssued[airGroup] = nowUtc;
            coverAircraftAirGroupsDroppedThisPass[airGroup] = player;   //once per pass - never refresh this
            coverAircraftAirGroupsBayWaitSince.Remove(airGroup);   //it is away - stop timing its doors
            issuedAnyThisPass = true;
        }

        //stand down once this player has nothing left to watch: nobody still waiting for the line, and
        //nobody we just told to drop still inside its release window.  (coverAircraftAirGroupsDropIssued
        //is keyed by airgroup across ALL players, so don't test its overall Count here.)
        if (!anyStillArmed && !issuedAnyThisPass)
        {
            if (mainmission.ON_TESTSERVER)
                Console.WriteLine("COVER <cdrop PASS: watcher standing down for {0} - nobody waiting, nothing issued", player != null ? player.Name() : "(null)");
            turnOffCoverDropWatch(player);
        }
    }

    //<cdropnow / <cbomb - tell the player's cover/bomber airgroups to pull their release NOW, whatever
    //order they are currently on.  This is the manual escape hatch, and the ONLY way to use the
    //drop-formation when the automatic detection cannot work - see the unlimited-ammo note in
    //Genghis-Class-CloDNotes.cs section 3: with unlimited ammo S_BombReserve never changes and
    //hasBombs() is unreliable, so <cdrop simply cannot detect the leader dropping.
    //Squadrons put on <creserve are skipped - that is the deliberate "hold this one back" order, and
    //we do not overrule it.  Everything else goes, including <cstrict squadrons, which is a handy
    //way to get a very tight formation and then release the whole stick on your word.
    public void dropBombsNow_player(Player player)
    {
        try
        {
            if (player == null) return;
            int told = 0;
            int held = 0;
            int nothing = 0;

            //<cdropnow - pass the leader's own airgroup through to the trace, so the log shows how far each
            //squadron is from HIM at the moment it is told to release.  That distance is the thing
            //that will tell us whether the delay scales with how far back the group is.
            AiAirGroup leaderAG = null;
            try
            {
                AiAircraft pla = player.Place() as AiAircraft;
                if (pla != null) leaderAG = pla.AirGroup();
            }
            catch (Exception ex) { }

            List<AiAirGroup> groups = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            foreach (AiAirGroup airGroup in groups)
            {
                if (airGroup == null || airGroup.GetItems() == null || airGroup.GetItems().Length == 0) continue;
                if (coverAircraftAirGroupsActive[airGroup] != player) continue;

                if (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.reserve)
                {
                    held++;
                    continue;   //<creserve means "hold this squadron back" - respect it
                }

                //torps are no good off a GATTACK_POINT at formation altitude, same as <cdrop
                if (!isBomberArmed(airGroup) || airGroup.hasTorpedos()) { nothing++; continue; }

                dropBombsNow_airGroup(airGroup, leaderAG);
                //remember it, so the ~16s keepAircraftOnTask_recurs() loop leaves the release plan
                //alone long enough for the bombs to actually go (see Genghis-Class-CloDNotes.cs s.2)
                coverAircraftAirGroupsDropIssued[airGroup] = DateTime.UtcNow;
                told++;
            }

            if (told > 0) GamePlay.gpLogServer(new Player[] { player }, told + " group(s) ordered to DROP NOW.", null);
            //2026/10 - Step B1: the AI has to open its bomb bays before it can release, and that takes a
            //few seconds (~6s measured on Wellingtons - the DROPTRACE bay= column confirms it per type).
            //So if the player wants the sticks to land together, he should let HIS first bomb go shortly
            //after issuing the command, not at the same instant.  Only shown when something actually
            //went, so it never spams for an empty squadrons list.
            if (told > 0) GamePlay.gpLogServer(new Player[] { player }, "Bomber groups need ~6 seconds to open their bomb bays - let your first bomb go a moment after this for the sticks to land together.", null);
            if (held > 0) GamePlay.gpLogServer(new Player[] { player }, held + " group(s) held back on <creserve.", null);
            if (nothing > 0) GamePlay.gpLogServer(new Player[] { player }, nothing + " group(s) had nothing to drop (no bombs, or torpedoes).", null);
            if (told == 0 && held == 0 && nothing == 0) GamePlay.gpLogServer(new Player[] { player }, "No cover airgroups available to drop.", null);
        }
        catch (Exception ex) { Console.WriteLine("Cover dropBombsNow_player ERROR! " + ex.ToString()); }
    }

    //Tell one airgroup to pull its release NOW, by giving it the waypoint pair CLoD actually responds
    //to: a NORMFLY where it is, then a GATTACK_POINT a short distance ahead.  The AI only evaluates the
    //release when the GATTACK_POINT becomes the CURRENT waypoint, and because the release point is by
    //then already behind it, it lets go straight away.
    //setTask(ATTACK_GROUND) does NOT do this - see Genghis-Class-CloDNotes.cs section 1.
    //DIAGNOSTIC (ON_TESTSERVER only) - <cdrop / <cdropnow release-latency trace.
    //Prints the one thing that matters and is otherwise invisible: WHICH waypoint the group is
    //actually flying right now, how far away it is, and whether its bombs have gone yet.  Called at
    //"issue" and then again on a fixed ladder of delays, so we can see exactly when the release
    //happens relative to the moment we asked for it - and, critically, whether the new flight plan is
    //adopted immediately or only once the aircraft finishes the leg it is already flying.
    public void dropTrace(AiAirGroup airGroup, string tag, AiAirGroup leader)
    {
        try
        {
            if (airGroup == null) return;
            int nWp = 0;
            int cw = -1;
            string act = "?";
            double distToWp = -1;
            AiWayPoint[] wps = airGroup.GetWay();
            if (wps != null) nWp = wps.Length;
            cw = airGroup.GetCurrentWayPoint();
            if (wps != null && cw >= 0 && cw < wps.Length && (wps[cw] as AiAirWayPoint) != null)
            {
                act = (wps[cw] as AiAirWayPoint).Action.ToString();
                distToWp = CoverCalcs.CalculatePointDistance(wps[cw].P, airGroup.Pos());
            }
            double leadDist = -1;
            if (leader != null) leadDist = CoverCalcs.CalculatePointDistance(airGroup.Pos(), leader.Pos());
            //2026/10 - Step B1: A_BombBayDoor on the group's FIRST aircraft - the ~6s door cycle is the
            //whole of the <cdrop release latency the player sees, so this column tells us exactly when the
            //doors opened relative to the moment the bomb count fell.  (Also reveals the value convention -
            //open = 1? 0? a 0..1 position? - before the B4 prefill via C_BombBayDoor is trusted.)  The
            //parameter may be -1/unset on some types, in which case it just prints -1 and costs nothing.
            double bayDoor = -1;
            try
            {
                if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null)
                    bayDoor = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.A_BombBayDoor, 0);
            }
            catch (Exception exBayRead) { }
            Console.WriteLine("DROPTRACE {0} t={1:HH:mm:ss.fff} grp={2} bombs={3} cw={4}/{5} act={6} distToWp={7:N0} leadDist={8:N0} bay={9:F3}",
                tag, DateTime.UtcNow, airGroup.Name(), CoverCalcs.bombCount(airGroup), cw, nWp, act, distToWp, leadDist, bayDoor);
        }
        catch (Exception ex) { Console.WriteLine("Cover dropTrace ERROR! " + ex.ToString()); }
    }

    //<cdrop / <cdropnow - the "issue" trace plus the follow-up ladder.  1/3/6s are tight because that is
    //where our own latency could plausibly hide (750ms poll + Timeout(0.05)); 10-30s is where we would
    //expect to find the group still on its OLD leg if SetWay() is only adopted at a waypoint boundary.
    public void dropTraceLadder(AiAirGroup airGroup, AiAirGroup leader)
    {
        if (!mainmission.ON_TESTSERVER) return;
        dropTrace(airGroup, "issue", leader);
        AiAirGroup agT = airGroup;
        AiAirGroup ldT = leader;
        double[] delays = new double[] { 1, 3, 6, 10, 15, 20, 30 };
        foreach (double d in delays)
        {
            double dd = d;
            Timeout(dd, () => dropTrace(agT, "+" + dd.ToString("F0") + "s", ldT));
        }
    }

    //<cdrop 2026/10 Step E - PRE-OPEN WITHOUT LOSING THE FORMATION.  Log 02F showed the decoy plan
    //REPLACING the formation plan entirely: because it carried no waypoint targeting the leader, the
    //group simply flew at a point 10km away and the tight formation fell apart (leadDist drifting
    //160 -> 670 -> 826m in the DROPTRACE lines), and on one flight they stopped tracking the player's
    //climb altogether.  So the pre-open plan is now the NORMAL formation waypoint FIRST - the same one
    //EscortUpdateWaypoints writes, .Target = the leader's aircraft, ~5-6km out on the leader's line -
    //with the fake GATTACK_POINT behind it purely to keep the engine in attack mode so the doors open.
    //The moment the release is ordered, dropBombsNow_airGroup() overwrites all of this anyway.
    public void dropPreOpenBays_airGroup(Player player, AiAirGroup airGroup, AiAirGroup leader)
    {
        try
        {
            if (player == null || airGroup == null || leader == null) return;
            if (airGroup.GetItems() == null || airGroup.GetItems().Length == 0) return;

            List<AiWayPoint> wps = new List<AiWayPoint>();

            //1. the ordinary formation waypoint, so they track the leader exactly as they do normally
            Tuple<AiAirWayPoint, AiAirWayPoint, double> fx = EscortPosWaypoint(player, airGroup, leader, AiAirWayPointType.FOLLOW, -3, 15, true, CoverAGOrders.drop);
            double vel = 80;
            if (fx != null)
            {
                if (fx.Item3 > 1) vel = fx.Item3;
                if (fx.Item1 != null) wps.Add(fx.Item1);
            }
            if (vel < 40) vel = 40;
            if (vel > 175) vel = 175;
            if (wps.Count == 0)
            {
                Point3d here = airGroup.Pos();
                AiAirWayPoint fb = new AiAirWayPoint(ref here, vel);
                fb.Action = AiAirWayPointType.NORMFLY;
                wps.Add(fb);
            }

            //2. the fake GATTACK_POINT coverDropPreOpen_m ahead, purely to hold the bomb bays open
            Vector3d ldV = leader.Vwld();
            double vlen = CoverCalcs.CalculatePointDistance(ldV);
            Point3d dir = new Point3d(0, 1, 0);
            if (vlen > 0.1) dir = new Point3d(ldV.x / vlen, ldV.y / vlen, 0);
            Point3d ap = airGroup.Pos();
            Point3d attackPos = new Point3d(ap.x + dir.x * coverDropPreOpen_m, ap.y + dir.y * coverDropPreOpen_m, ap.z);
            AiAirWayPoint awp = new AiAirWayPoint(ref attackPos, vel);
            awp.Action = AiAirWayPointType.GATTACK_POINT;
            wps.Add(awp);

            //3. a long run behind it, so they never run out of plan - an airgroup with no waypoints
            //   switches itself to RTB, and an RTB group will neither open its bays nor drop
            //   (CloDNotes 1f).
            for (int i = 1; i <= 6; i++)
            {
                double gap_m = coverDropPreOpen_m + 2000 + i * 10000;
                Point3d wp = new Point3d(ap.x + dir.x * gap_m, ap.y + dir.y * gap_m, ap.z);
                AiAirWayPoint w = new AiAirWayPoint(ref wp, vel);
                w.Action = AiAirWayPointType.NORMFLY;
                wps.Add(w);
            }

            airGroup.SetWay(wps.ToArray());
            if (mainmission.ON_TESTSERVER)
                Console.WriteLine("COVER <cdrop PREOPEN: {0} formation waypoint + fake GATTACK_POINT {1:n0}m ahead, {2} bombs, bay={3:F3}, waypoints={4}",
                    airGroup.Name(), coverDropPreOpen_m, CoverCalcs.bombCount(airGroup), coverDropBayDoorPos(airGroup), wps.Count);
        }
        catch (Exception ex) { Console.WriteLine("Cover dropPreOpenBays_airGroup ERROR! " + ex.ToString()); }
    }

    public void dropBombsNow_airGroup(AiAirGroup airGroup, AiAirGroup leader = null, double runInOverride_m = -1, bool announceAsDrop = true)
    {
        try
        {
            if (airGroup == null || airGroup.GetItems() == null || airGroup.GetItems().Length == 0) return;

            //2026/10 - Step A3: the plan used to pin the group's OWN instantaneous speed and heading
            //(with an 80 m/s floor!) for the whole 25s hold-off.  That is why in the log the bombers
            //"speed up a little and change course a little" right after a release - the group was on
            //the 55-65 m/s formation law but this plan stamped 80+ m/s along its own (slightly
            //off-axis) heading - and then keepAircraftOnTask_recurs() resumes and they "revert to
            //normal".  So: keep the formation speed and use the LEADER's heading, so the stick lands
            //where the leader's stick lands.  (An 80 m/s floor on the release legs is also wrong for
            //a slow leader - if the leader is at 52 m/s, the group must be at 52 m/s for the bombs
            //to hit together.)
            Vector3d vwld = airGroup.Vwld();
            double vel = 0;
            bool haveFormationSpeed = false;
            if (coverAGSpeedRequested.ContainsKey(airGroup))
            {
                vel = coverAGSpeedRequested[airGroup];   //the last speed the formation law asked this group for
                if (vel > 1) haveFormationSpeed = true;
            }
            if (!haveFormationSpeed)
            {
                vel = CoverCalcs.CalculatePointDistance(vwld);   //fallback: what it was already doing
                if (vel < 1) vel = 80;
            }
            //Mild crash-protection floor: a cover group must never be commanded below ~40 m/s in the
            //air (see the floors in CurrentPosWaypoint/BomberPosWaypoint, Step A2).
            if (vel < 40) vel = 40;
            if (vel > 170) vel = 170;

            //Heading: the leader's if we know it, otherwise the group's own.
            Vector3d dirV = vwld;
            if (leader != null && leader.GetItems() != null && leader.GetItems().Length > 0)
            {
                dirV = leader.Vwld();
                if (CoverCalcs.CalculatePointDistance(dirV) < 0.5) dirV = vwld;
            }
            double vlen = CoverCalcs.CalculatePointDistance(dirV);
            Point3d dir = new Point3d(0, 1, 0);
            if (vlen > 0.1) dir = new Point3d(dirV.x / vlen, dirV.y / vlen, 0);

            Point3d apos = airGroup.Pos();
            List<AiWayPoint> newWaypoints = new List<AiWayPoint>();

            //1. THE LEADING WAYPOINT.  A plain NORMFLY at the aircraft's OWN position, so that the next
            //   waypoint becomes current immediately.  This is verbatim what the reference test
            //   (Genghis\Reliable-instant-bomb-drop\bombdrop_test20-return.cs, changeWP()) issues, so
            //   it is what we had always done - the .FOLLOW variant tried in Step C6 was a wrong
            //   guess and has been reverted.
            //   attack_m is the GATTACK_POINT's distance along the heading; NEGATIVE = behind the
            //   group.  The reference test used 10m behind and found 50m behind behaved the same, so
            //   the distance genuinely does not matter - what matters is that the bomb-bay doors are
            //   ALREADY OPEN when this plan arrives (see cdDropPreOpenBays / the wait in
            //   coverDropReleasePass).
            double attack_m = (runInOverride_m >= 0) ? runInOverride_m : -coverDropAttackGap_m;
            Point3d wp0pos = new Point3d(apos.x, apos.y, apos.z);
            AiAirWayPoint wp0 = new AiAirWayPoint(ref wp0pos, vel);
            wp0.Action = AiAirWayPointType.NORMFLY;
            newWaypoints.Add(wp0);

            //2. THE RELEASE POINT.  2026/10 Step C2: this used to sit 35m ahead of the group.  That is so close
            //   that the engine's release solution comes out "already passed", and it then drops ONE
            //   salvo per a/c and latches - measured three times in the 2026-10-02 logs (Wellington
            //   54->51, and one run with no release at all).  The PROVEN path (BomberPosWaypoint, used
            //   for the Knickebein / flare / bomb-point modes, which DOES drop a full stick) aims at a
            //   REAL point the group has to fly TO.  So the release is now a genuine run-in of
            //   coverDropReleaseRunIn_m along the leader's heading.  The price is that the stick lands
            //   ~coverDropReleaseRunIn_m past the leader's line, which is exactly what
            //   coverDropBayLead_s exists to compensate for.  See CloDNotes 1 + the 2026/10 logs.
            //   2026/10 - Step B2: on the TEST server cdDropPlanTestModeEnum overrides this, so the
            //   run-in can be compared at 600m / 2000m (GATTACK_POINT_FAR) against the GATTACK_TARG
            //   variants without a rebuild.  OFF = production.
            //Step C5 PRE-OPEN passes coverDropPreOpen_m here (runInOverride_m): the very same plan shape,
            //but aimed a long way AHEAD so the engine opens the bomb bays without them ever reaching
            //the point.  That is the pre-open attack leg the reference .mis carried.
            Point3d wp1pos = new Point3d(apos.x + dir.x * attack_m, apos.y + dir.y * attack_m, apos.z);
            AiAirWayPointType wp1Action = AiAirWayPointType.GATTACK_POINT;
            AiAirWayPointGAttackPasses wp1Passes = AiAirWayPointGAttackPasses.AUTO;
            AiAirWayPointGAttackType wp1Type = AiAirWayPointGAttackType.AUTO;
            if (mainmission.ON_TESTSERVER && cdDropPlanTestModeEnum != cdDropPlanTestMode.OFF)
            {
                if (cdDropPlanTestModeEnum == cdDropPlanTestMode.GATTACK_TARG_ALLOUT ||
                    cdDropPlanTestModeEnum == cdDropPlanTestMode.GATTACK_TARG_FAR)
                {
                    wp1Action = AiAirWayPointType.GATTACK_TARG;
                    wp1Passes = AiAirWayPointGAttackPasses.ALL_OUT;
                    wp1Type = AiAirWayPointGAttackType.AUTO;
                }
                else if (cdDropPlanTestModeEnum == cdDropPlanTestMode.GATTACK_POINT_FAR)
                {
                    attack_m = 2000;   //the production recipe, just a long run-in
                }
                else //(the GATTACK_POINT enum value; OFF is excluded above) - no-release control
                {
                    wp1Action = AiAirWayPointType.NORMFLY;
                }
                if (cdDropPlanTestModeEnum == cdDropPlanTestMode.GATTACK_TARG_FAR) attack_m = 2000;
                wp1pos = new Point3d(apos.x + dir.x * attack_m, apos.y + dir.y * attack_m, apos.z);
            }
            AiAirWayPoint wp1 = new AiAirWayPoint(ref wp1pos, vel);
            wp1.Action = wp1Action;

            //2026/10 Step C7 - TYPE/PASSES ARE DELIBERATELY NOT SET HERE, a deliberate reversal of Step C.
            //   Step C copied GAttackPasses=AUTO / GAttackType=LEVEL onto this waypoint to match
            //   BomberPosWaypoint, on the theory that the missing fields explained the one-salvo
            //   latch.  The reference test settles it:  changeWP() in
            //   Genghis\Reliable-instant-bomb-drop\bombdrop_test20-return.cs sets NEITHER field, never
            //   calls setTask, and still made EVERY bomber type dump its whole load.  So these fields
            //   were never the cause; the only variable that matters is the bomb-bay doors being open
            //   BEFORE this plan arrives.  Only the test modes below still set them.
            if (wp1Action == AiAirWayPointType.GATTACK_TARG)
            {
                wp1.GAttackPasses = wp1Passes;
                wp1.GAttackType = wp1Type;
                //deliberately NOT setting wp1.Target: CloDNotes 1c says the sim picks whatever it
                //finds near the point - for a leader-release that is exactly what we want (the
                //stick lands on the leader's own position).
            }
            newWaypoints.Add(wp1);

            //3. A long NORMFLY continuing along the same heading.  The reference test put its 3rd waypoint
            //   20km out and then several more at 10km intervals; BomberPosWaypoint uses a single 30km
            //   leg for the same reason ("most types need to continue straight for a while after
            //   attack for it to work").  We keep the 30km leg plus the short tail in step 4.
            Point3d wp2pos = new Point3d(apos.x + dir.x * (attack_m + 30000), apos.y + dir.y * (attack_m + 30000), apos.z);
            AiAirWayPoint wp2 = new AiAirWayPoint(ref wp2pos, vel);
            wp2.Action = AiAirWayPointType.NORMFLY;
            newWaypoints.Add(wp2);

            //4. then a run of ordinary points continuing along the same heading, purely so they do NOT
            //run out of waypoints before keepAircraftOnTask_recurs() takes over - an airgroup with an
            //empty flight plan switches itself to RTB mode and is then useless to us for good.
            for (int i = 1; i <= 8; i++)
            {
                //the run starts AFTER both the release point and the 30km leg, so the sim always has a
                //leg left to fly through
                double gap_m = attack_m + 31500 + i * 1500;
                Point3d wpn = new Point3d(apos.x + dir.x * gap_m, apos.y + dir.y * gap_m, apos.z);
                AiAirWayPoint wp = new AiAirWayPoint(ref wpn, vel);
                wp.Action = AiAirWayPointType.NORMFLY;
                newWaypoints.Add(wp);
            }

            airGroup.SetWay(newWaypoints.ToArray());
            //Step C7: NO setTask() call.  The reference test never calls it either, and the proven
            //ground-attack path (BomberUpdateWaypoints) returns before its own setTask for exactly this
            //reason - so the release plan is left alone, the same way the path that already worked is.

            //2026/10 - B4 placeholder: the AI's ~6s door cycle is the whole release latency.  We would
            //like to pre-open the bay via C_BombBayDoor (40), but the IL proves there is NO write
            //parameter API on AiAircraft - only getParameter(ParameterTypes, Int32) (probed
            //gameWorld.dll 2026/10: AiAircraft's public instance methods are ...getParameter,
            //RearmPlane, RefuelPlane, hitLimb... - no setParameter).  So B4 can only LOG the door
            //position now; if the value convention is later found in an IL dump / a new API, the real
            //write can slot in here.  ON_TESTSERVER only; cdDropPrefillBombBayDoors stays false.
            if (mainmission.ON_TESTSERVER && cdDropPrefillBombBayDoors)
            {
                try
                {
                    foreach (AiActor ba in airGroup.GetItems())
                    {
                        if (ba == null || (ba as AiAircraft) == null) continue;
                        AiAircraft baa = ba as AiAircraft;
                        double d0 = baa.getParameter(part.ParameterTypes.A_BombBayDoor, 0);
                        Console.WriteLine("COVER <cdrop B4: {0} bayDoor A={1:F3} (write API not available - see notes)", baa.Name(), d0);
                    }
                }
                catch (Exception exBay) { Console.WriteLine("COVER <cdrop B4 prefill doors ERROR: " + exBay.Message); }
            }

            if (mainmission.ON_TESTSERVER)
            {
                if (announceAsDrop)
                    Console.WriteLine("COVER <cdrop: {0} told to DROP now, {1} bombs, at {2:n0} {3:n0} [plan={4} vel={5:F1} formationVel={6} dirLead={7} runIn={8:n0}]",
                        airGroup.Name(), CoverCalcs.bombCount(airGroup), apos.x, apos.y,
                        cdDropPlanTestModeEnum.ToString(), vel, haveFormationSpeed,
                        (leader != null && leader.GetItems() != null && leader.GetItems().Length > 0), attack_m);
                else
                    Console.WriteLine("COVER <cdrop PREOPEN: {0} decoy attack point {1:n0}m ahead to hold the bomb bays open, {2} bombs, bay={3:F3}",
                        airGroup.Name(), attack_m, CoverCalcs.bombCount(airGroup),
                        (airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null)
                            ? (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.A_BombBayDoor, 0) : -1);
                dropTraceLadder(airGroup, leader);
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover dropBombsNow_airGroup ERROR! " + ex.ToString()); }
    }

    //returns false if it's been turned off or true if turned on.
    public bool toggleregularDisplay_listPositionCurrentCoverAircraft(Player player = null)
    {
        if (player == null) return false;
        if (COVER_ListPositionTimer.ContainsKey(player))
        {
            COVER_ListPositionTimer[player].Dispose();
            COVER_ListPositionTimer.Remove(player);
            return false;
        }
        COVER_ListPositionTimer[player] = new System.Threading.Timer(
           listPositionCurrentCoverAircraft_obj,
           //new Tuple<int,MissionObjective>(army, mo),
           player, //bool is whether or not thMO_BRAdvanceBumrushPhaseTimer must be restarted; ie TRUE = an interrupted timer/early restart 
           dueTime: 100, //wait time @ first startup (ms).  
           period: COVER_ListPositionTimerPeriod_ms);
        return true;

    }
    public bool isOn_Display_listPositionCurrentCoverAircraft(Player player = null)
    {
        if (player == null) return false;
        if (COVER_ListPositionTimer.ContainsKey(player)) return true;
        else return false;

    }
    //returns false if it's been turned off or true if turned on.
    public void turnOnRegularDisplay_listPositionCurrentCoverAircraft(Player player = null)
    {
        if (player == null) return;
        if (COVER_ListPositionTimer.ContainsKey(player)) return;
        bool ret = toggleregularDisplay_listPositionCurrentCoverAircraft(player);
        if (!ret) toggleregularDisplay_listPositionCurrentCoverAircraft(player);
    }

    public void turnOffRegularDisplay_listPositionCurrentCoverAircraft(Player player = null)
    {
        if (player == null) return;
        bool ret = false;
        if (isOn_Display_listPositionCurrentCoverAircraft(player))
            ret = toggleregularDisplay_listPositionCurrentCoverAircraft(player);
        if (ret) toggleregularDisplay_listPositionCurrentCoverAircraft(player);
    }

    public void listPositionCurrentCoverAircraft_obj(object ob)
    {
        try
        {
            Player player = ob as Player;
            if (player == null)
            {
                if (COVER_ListPositionTimer[player] != null) COVER_ListPositionTimer[player].Dispose();
                return;
            }
            listPositionCurrentCoverAircraft(player);
        }
        catch (Exception ex) { Console.WriteLine("Cover Mission, listPositionCurrentCoverAircraft_obj ERROR! " + ex.ToString()); }
    }

    //Along-track distance from the leader, in metres: +ve = ahead of him, -ve = behind him.
//Same measure that calcCoverSpeedToMatchMain() calls frontBackDist_m, so logging it next to the
//commanded/actual speeds tells us exactly which catch-up band (or the sub-400m override) is in play
//for that sample.  Without it the speed numbers alone can't be tied to a distance.
public double lca_fwdDist_m(AiAirGroup airGroup, AiAircraft leaderAircraft)
{
    if (airGroup == null || leaderAircraft == null) return 0;
    try
    {
        Point3d d = new Point3d(airGroup.Pos().x - leaderAircraft.Pos().x,
                                airGroup.Pos().y - leaderAircraft.Pos().y, 0);
        Vector3d v = airGroup.Vwld();
        double vlen = CoverCalcs.distance(v.x, v.y);
        if (vlen < 0.0001) return 0;   //not moving - no meaningful heading to measure against
        Point3d heading = new Point3d(v.x / vlen, v.y / vlen, 0);
        return d.x * heading.x + d.y * heading.y;
    }
    catch (Exception ex) { Console.WriteLine("Cover lca_fwdDist_m ERROR! " + ex.ToString()); return 0; }
}

public string listPositionCurrentCoverAircraft(Player player = null, bool display = true, bool html = false)
    {
        try
        {

            if (player == null || GamePlay == null) return "";
            string nl = Environment.NewLine;
            if (html) nl = "<br>" + nl;
            string retmsg = "";
            int count = 0;
            AiActor playerPlace = player.Place();

            double player_vel_mph = 0;
            AiAircraft playerPlaceAircraft = null;
			
			if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #1");

            if (playerPlace as AiAircraft != null)
            {
                playerPlaceAircraft = playerPlace as AiAircraft;
                Vector3d player_vwld = (playerPlace as AiAircraft).AirGroup().Vwld();
                player_vel_mph = CoverCalcs.meterspsec2milesphour(CoverCalcs.distance(player_vwld.x, player_vwld.y));
            }
            double player_vel_kph = CoverCalcs.miles2meters(player_vel_mph) / 1000;
            string player_vel = ((double)(CoverCalcs.RoundInterval(player_vel_mph * 1, 5)) / 1).ToString("F0") + "mph";
            if (player.Army() == 2) player_vel = ((double)(CoverCalcs.RoundInterval(player_vel_kph * 1, 5)) / 1).ToString("F0") + "kph";

            string smsg = ">>>> Your current speed: " + player_vel + ". Your current cover airgroups:";
            retmsg += smsg + nl;

			if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #2");
            //So if the player crashes (no longer in plane) we still keep their updates going as best we can; also avoids object reference errors
            if ((playerPlaceAircraft != null && !isHeavyBomber(playerPlace as AiAircraft) && !isDiveBomber(playerPlace as AiAircraft) && !isFighterAllowedCover(playerPlace as AiAircraft) && !isFighterAllowedCover_wing(playerPlace as AiAircraft) && !Calcs.isStrikeAC(player) && !isOnRepairMission(player)) && admin_privilege_level(player) < 1)
            {
                string m = "****No Cover info - Cover provided for heavy bombers, dive bombers, sturmovik/strike fighter-bombers, selected fighters, & repair/restock missions only!****";
                GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });
				turnOffRegularDisplay_listPositionCurrentCoverAircraft(player); //bhugh 10/2023 - 
                return m;
            }

            GamePlay.gpLogServer(new Player[] { player }, smsg, null);

            double delay = 0.02;            

			if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #3");
			
            //This doesn't work - apparently the indexes thing is not really working
            //var agIndex = new Dictionary<int, AiAirGroup>();
            //if (coverAircraftAirGroupsIndexes.ContainsKey(player)) agIndex = coverAircraftAirGroupsIndexes[player];           

            //foreach ( int indx in agIndex.Keys)
            foreach (AiAirGroup airGroup in coverAircraftAirGroupsActive.Keys)
            {
                //AiAirGroup airGroup = agIndex[indx];

                if (airGroup == null) continue;
                if (coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (airGroup.GetItems().Length == 0) continue;
                AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;
                if (aircraft == null) continue;


                count++;
                double alt_m = aircraft.Pos().z;
                double alt_km = alt_m / 1000;
                double alt_angels = CoverCalcs.Feet2Angels(CoverCalcs.meters2feet(alt_m), 5);
                string alt_msg = string.Format("{0:N0} m, ", alt_m);
                if (player.Army() == 1) alt_msg = string.Format("Angels {0:N0}, ", alt_angels);
                Vector3d agVwld = airGroup.Vwld();
                double heading = (CoverCalcs.CalculateBearingDegree(agVwld));
                int heading_10 = CoverCalcs.GetDegreesIn10Step(heading);

                //part.ParameterTypes.M_Health

                string msg = "";
				
				//if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #4");

                if (playerPlaceAircraft == null) msg = "#" + count.ToString() + " " + CoverCalcs.GetAircraftType(aircraft) + " at " + alt_msg + CoverCalcs.correctedSectorNameDoubleKeypad(this, aircraft.Pos());
                else
                {

                    double dis_m = CoverCalcs.CalculatePointDistance(playerPlace.Pos(), aircraft.Pos());
                    double dis_mi = (CoverCalcs.meters2miles(dis_m));
                    int dis_10 = (int)dis_mi;
                    double bearing = CoverCalcs.CalculateGradientAngle(playerPlace.Pos(), aircraft.Pos());
                    double bearing_10 = CoverCalcs.GetDegreesIn10Step(bearing);
                    string ang = "A" + alt_angels.ToString("F0") + " ";
                    string mi = dis_mi.ToString("F0") + "mi";
                    string mi_10 = dis_10.ToString("F0") + "mi";
                    double vel_mph = CoverCalcs.meterspsec2milesphour(CoverCalcs.distance(airGroup.Vwld().x, airGroup.Vwld().y));
                    double vel_kph = CoverCalcs.miles2meters(vel_mph) / 1000;
                    string vel = ((double)(CoverCalcs.RoundInterval(vel_mph * 1, 5)) / 1).ToString("F0") + "mph";
                    string numAC = airGroup.GetItems().Length.ToString();

                    if (player.Army() == 2) //metric for the Germanos . . . 
                    {
                        mi = (dis_m / 1000).ToString("F0") + "k";
                        mi_10 = mi;
                        if (dis_m > 30000) mi_10 = ((double)(CoverCalcs.RoundInterval(dis_m, 10000)) / 1000).ToString("F0") + "k";

                        //ft = alt_km.ToString("F2") + "k ";                                        
                        ang = ((double)(CoverCalcs.RoundInterval(alt_km * 10, 1)) / 10).ToString("F1") + "k ";
                        vel = ((double)(CoverCalcs.RoundInterval(vel_kph * 10, 5)) / 10).ToString("F0") + "kph";
                    }
                    msg = "#" + count.ToString() + " " + mi_10 + bearing_10.ToString("F0") + "°" + ang + " " + vel + heading_10.ToString("F0") + "°" + " - " + numAC + "x" + CoverCalcs.GetAircraftType(aircraft);
                }
				
				//if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #5");

                //AiAirGroupTask task = airGroup.getTask();
                //string tsk = task.ToString();
                AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
                int currWay = airGroup.GetCurrentWayPoint();
                string bomb = " No bombs ";
                if (isBomberArmed(airGroup)) bomb = " Has bombs ";
                string cannons = "";
                if (cannonsEmpty(airGroup)) cannons = " Cannon empty ";
                string action = "";
                string targetname = "";
                string targettype = "";
                Point3d p = new Point3d(0, 0, 0);
                if (CurrentWaypoints != null && CurrentWaypoints.Length > 0 && CurrentWaypoints.Length > currWay)
                {
                    if  (mainmission.ON_TESTSERVER) foreach (AiWayPoint wp in CurrentWaypoints)
                    {
                        string nm = "";
                        if ((wp as AiAirWayPoint).Target != null) nm = (wp as AiAirWayPoint).Target.Name();

                        Console.WriteLine("ListCoverPosition - all waypoints: currway: {7} currtask: {8} {0} speed: [{1:N0}cw/{10:N0}ca vs {9:N0}p] ({2:n0} {3:n0} {4:n0}), gp: {5}, name: {6}, fwd:{11:N0}m, behind:{12}", (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z, airGroup.Name(), nm, currWay, airGroup.getTask(), Calcs.milesphour2meterspsec(player_vel_mph), CoverCalcs.distance(airGroup.Vwld().x, airGroup.Vwld().y), lca_fwdDist_m(airGroup, playerPlaceAircraft), (lca_fwdDist_m(airGroup, playerPlaceAircraft) < 0 ? "yes" : "no")); 

                    }
                    //If the next waypoint is more interesting than the current one, display that one instead (usually it is "GATTACK_POINT" or such instead of "FOLLOW" or "ESCORT"
					if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #10");
                    if (CurrentWaypoints.Length > currWay + 1 && (CurrentWaypoints[currWay + 1] as AiAirWayPoint).Action.ToString().ToUpper().Contains("ATTACK")) currWay++;
					if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #11");
                    action = (CurrentWaypoints[currWay] as AiAirWayPoint).Action.ToString();
                    targetname = "";
                    if ((CurrentWaypoints[currWay] as AiAirWayPoint).Target != null) targetname = (CurrentWaypoints[currWay] as AiAirWayPoint).Target.Name();
                    if ((CurrentWaypoints[currWay] as AiAirWayPoint).Target as AiGroundActor != null)
                    {
                        targettype += ((CurrentWaypoints[currWay] as AiAirWayPoint).Target as AiGroundActor).Type().ToString();
                        if (targettype.Length > 1 && targettype != "SPG" && targettype.ToLower() != targettype)
                        {
                            var splits = Regex.Split(targettype, @"(?<!^)(?=[A-Z])"); //put a dash before each capitol letter, for readability.  The types are things like AmmoGun, ContainerLong, ShipSubmarine, LightTruck.  SPG is on the only one that is all caps.
                            targettype = string.Join("-", splits);
                        }
                    }
					if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #6");
                    if ((CurrentWaypoints[currWay] as AiAirWayPoint).Target as GroundStationary != null) targettype += ((CurrentWaypoints[currWay] as AiAirWayPoint).Target as GroundStationary).Title;
                    p = (CurrentWaypoints[currWay] as AiWayPoint).P;
                }
				if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #12");


                //msg += " " + bomb + " " + tsk + " " + action;
                action = action.Replace("_", " ");
                if (action.StartsWith("G")) action = "GND-" + action.Substring(1);
                if (action.StartsWith("AA")) action = "AIR-" + action.Substring(1);
                if (airGroup.getTask() == AiAirGroupTask.RETURN) action = "[RTB]";
                msg += bomb + cannons + action;

                //action for ground attack is either "GND-ATTACK POINT" or "GND-ATTACK TARG"
                //prior to the re-writing above it was GATTACK_POINT GATTACK_TARG, AATTACK_TARG etc.
                if (targetname == "" || action.Contains("GND-ATTACK POINT")) targetname = "coord";
                if ((targetname == "" || targetname == "coord") && action.Contains("GND-ATTACK TARG")) targetname = "Ground Obj";

                //Include actual target name if it's a ground object.  Getting it from our saved Dictionary
                //of items the AG is attacking rather than the AiAirWayPoint).Target, which doesn't seem to work for some reason.

                if (targetname == "Ground Obj" && airgroupGroundTargets != null && airgroupGroundTargets.ContainsKey(airGroup) && airgroupGroundTargets[airGroup] != null && airgroupGroundTargets[airGroup].Title.Length > 0)
                {
                    string tmp = airgroupGroundTargets[airGroup].Title;
					if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #13");
					
					if (airgroupGroundTargets[airGroup] is GroundStationary) {
						targettype = Calcs.CleanStationaryName(tmp);
						targetname = targettype;
						
					} else {
                    //This is usually something like "Stationary.Environment.TentTroopLarge_GER1_DMG1"
                    //So we want to get rid of the stuff before the last . and after the first _
                    if (tmp.Contains(".")) tmp = tmp.Split('.').Last();
                    if (tmp.Contains("_")) tmp = tmp.Split('_').First();
                    if (tmp.Length > 2 && !tmp.ToLower().Contains("static")) targetname = tmp; //No need to display if it is just like "103:Static1092"
					}
					
					
                }

                bool ordersAreReserve = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.reserve);
                bool ordersAreNormal = (!coverAircraftAirGroupsOrders.ContainsKey(airGroup) || (coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.normal));
                bool ordersAreAttack = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.attack);
                bool ordersAreStrict = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.strict);
                bool ordersAreEscort = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.escort);
                bool ordersAreLoiter = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.loiter);
                bool ordersAreDrop = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] == CoverAGOrders.drop);
                bool ordersAreHoldFire = (coverAircraftAirGroupsOrders.ContainsKey(airGroup) && ordersHoldFire(coverAircraftAirGroupsOrders[airGroup]));
                double distToTarget_m = CoverCalcs.CalculatePointDistance(p, aircraft.Pos());
				
				if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #14");

                if (action.Contains("GND-ATTACK"))
                {
                    if (distToTarget_m < 15000) //give the details in this case
                        if (targetname.Length > 1 && airgroupGroundTargets != null && airgroupGroundTargets.ContainsKey(airGroup)  && (airgroupGroundTargets[airGroup] is AiGroundActor))
                            msg += String.Format(" {0} (Live, {1})", targettype, Calcs.correctedSectorNameDoubleKeypad(this, p));
                        else
                            msg += String.Format(" {0} ({1})", targettype, Calcs.correctedSectorNameDoubleKeypad(this, p));
                    else //no details if too far away.  Only the sector (action is included, above, already) 
                        msg += String.Format(" ({0})", Calcs.correctedSectorNameDoubleKeypad(this, p));
						if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #14A");
                }
                //In cases where the pilot has set a target point and type of "nearest enemy" but the a/c has not found
                //a ground actor/stationary as a target. Only when FOLLOW AND BAM is nearest enemy
                //AND armed AND not set on RESERVE/FOLLOW
                else if ((action.Contains("FOLLOW") || action.Contains("NORMFLY")) && BAM_isNearestEnemy(player) && isBomberArmed(airGroup) && !ordersAreHoldFire)
                {

                    if (distToTarget_m < 15000)
                    {
						if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #15");
                        string s = "]]";
                        if (playerCurrentTargetPoint.ContainsKey(player))
                        {
                            Point3d pctp = playerCurrentTargetPoint[player];
                            if (pctp.x >= 0) s = String.Format(" @{0}]]", Calcs.correctedSectorNameDoubleKeypad(this, pctp));
                        }

                        msg += " [[NO TGT" + s;
                    } else
                    {
                        msg += String.Format(" {0} ({1})", targettype, Calcs.correctedSectorNameDoubleKeypad(this, p));
                    }

                }
				if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #16");
                //msg += String.Format(" Health: ({0:N2}, {1:N2})", health, namedDamage); //not sure what named damage is?
                bool displayHealth = false;
                string healthString = "";
                int hCount = 0;
                foreach (AiActor act in airGroup.GetItems())
                {

                    AiAircraft a = act as AiAircraft;
                    double health = a.getParameter(part.ParameterTypes.M_Health, 0);
                    double namedDamage = a.getParameter(part.ParameterTypes.M_NamedDamage, 0);

                    if (health < 1)// || namedDamage > 0)
                    {
                        displayHealth = true; 
                    }
                    //healthString += String.Format(" ({0:N0}:{1:N0})", Math.Round(health * 100) ,namedDamage*1000);
                    if (hCount > 0) healthString += ":";
                    healthString += String.Format("{0:N0}", Math.Floor(health * 100));
                    hCount++;
                }
				if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #17");
                //Display some info about aircraft health
                if (displayHealth) msg += " (" + healthString + ")";
                if (ordersAreReserve) msg += " [[[RESERVED]]]";
                if (ordersAreAttack) msg += " [[[ATTACK]]]";
                if (ordersAreStrict) msg += " [[[STRICT]]]";
                if (ordersAreEscort) msg += " [[[ESCORT]]]";
                if (ordersAreLoiter) msg += " [[[LOITER]]]";
                if (ordersAreDrop) msg += " [[[DROP-WHEN-I-DROP]]]";
                delay += 0.08; //was .06 but that seemed to cause stuttering?  Maybe needs 0.1 or even more
                Timeout(delay, () =>
                {
                    GamePlay.gpLogServer(new Player[] { player }, msg, null);
                });

                retmsg += msg + nl;
            }
			if (mainmission.ON_TESTSERVER) Console.WriteLine("LCA #18");
            if (count == 0)
            {
                string msg2 = "[[[NO COVER AIRCRAFT - turning display off]]]";
                GamePlay.gpLogServer(new Player[] { player }, msg2, null);
                retmsg += msg2 + nl;
                turnOffRegularDisplay_listPositionCurrentCoverAircraft(player);

            }
            return retmsg;
        }
        catch (Exception ex) { Console.WriteLine("listPositionCurrentCoverAircraft ERROR! " + ex.ToString()); return ""; }
    }

    //army = ArmiesE.None lists both armies
    public string listCoverAircraftCurrentlyAvailable(ArmiesE army, Player player = null, bool display = true, bool html = false)
    {
        if (GamePlay == null) return "";
        string nl = Environment.NewLine;
        if (html) nl = "<br>" + nl;
        string retmsg = "";
        if (army != ArmiesE.Blue && army != ArmiesE.Red) return "Cover: No cover aircraft/bombers available because you are not in an army";
        if (CoverAircraftCurrentlyAvailable[army] == null) return "Cover: Aircraft availability not initialized";
        double delay = 0.02;

        AiAircraft aircraft = null;
        if (player != null) aircraft = player.Place() as AiAircraft;

        //if (aircraft == null || (!isBomberAllowedCover(aircraft) && !(isFighterAllowedCover(aircraft) && !Calcs.isStrikeAC(aircraft))
		//		&& !(Calcs.isStrikeAC(aircraft))))
		// && !(Calcs.isStrikeAC(aircraft) && Calcs.playerHasBombs(player))))
		if (aircraft == null || (!isBomberAllowedCover(aircraft) && !isFighterAllowedCover(aircraft) 
                && !isFighterAllowedCover_wing(aircraft) && !(Calcs.isStrikeAC(aircraft))))
        {
            string m = "****No Cover info - Cover provided for heavy bombers, dive bombers, fighter-bombers/strike aircraft, a few select fighters, and repair/restock missions only!****";
            if (display && player != null) GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });
            return m;
        }

        if (isOnRepairMission(player))
        { //for repair/resupply mission there is always only one a/c available, same as the player's current a/c
            string msg = string.Format("#{0} {1} {2}", 1, CoverCalcs.ParseTypeName((aircraft as AiCart).InternalTypeName()), maximumCheckoutsAllowedAtOnce_RepairMission);
            if (isOnFerryMission(player)) msg = string.Format("#{0} {1} {2}", 1, CoverCalcs.ParseTypeName((aircraft as AiCart).InternalTypeName()), maximumCheckoutsAllowedAtOnce_FerryMission);
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg, null);
            retmsg += msg + nl;
        }
        else
        {

            List<ArmiesE> armylist = new List<ArmiesE>();
            if (army == ArmiesE.Blue || army == ArmiesE.Red) armylist.Add(army);
            else if (army == ArmiesE.None) { armylist.Add(ArmiesE.Red); armylist.Add(ArmiesE.Blue); }

            foreach (ArmiesE a in armylist)
            {
                string smsg = string.Format(">>>>>Available cover aircraft/bombers for {0}", a);
                Timeout(0.02, () =>
                {
                    if (display && player != null) GamePlay.gpLogServer(new Player[] { player }, smsg, null);
                });

                retmsg += smsg + nl;

                string smsg2 = "ID# - Aircraft - Number remaining in supply - Bomb default (" + a.ToString() + ")";
                Timeout(0.04, () =>
                {
                    if (display && player != null) GamePlay.gpLogServer(new Player[] { player }, smsg2, null);
                });

                retmsg += smsg2 + nl;

                //foreach (string acName in CoverAircraftCurrentlyAvailable[army])
                int i = 0;
                foreach (string key in CoverAircraftCurrentlyAvailable[a].Keys)
                {
                    //if (aircraft != null && isFighterAllowedCover(aircraft) && !isHeavyBomber(key)) continue; //for fighter-bombers, they are only allowed to choose heavy bombers to cover, no fighters.
                    if (aircraft != null && !isFighterOrStrikeAllowedCoverForThisAircraft(player, key)) continue;
                    i++;
                    string acName = CoverCalcs.ParseTypeName(key);
                    string bombs = "(no bombs)";
                    if (isHeavyBomber(acName) || isDiveBomber(acName) || (Calcs.isStrikeAC(acName) && !Calcs.isStrikeACwithNoBombs(acName))) bombs = "(bombs)";
                    string tn = CoverCalcs.ParseTypeName(key);
                    if (key == "tobruk:Aircraft.He-111H-6" || key == "tobruk:Aircraft.He-111H-6_Trop") tn += " Hermann-1000kg";
                    string msg = string.Format("#{0} {1} {2} {3}", i, tn, CoverAircraftCurrentlyAvailable[a][key] - minimumAircraftRequiredForCoverDuty, bombs);

                    if (display)
                    {
                        delay += 0.06;
                        if (i % 10 == 0) delay += 3.5;
                        Timeout(delay, () =>
                        {

                            if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg, null);
                        });
                    }
                    retmsg += msg + nl;
                }
                if (i == 0)
                {
                    string msg1 = string.Format("***No cover aircraft/bombers available for {0} - aircraft available for cover & bomber squadron duty only if {1} or more remain in supply. Use chat command <stock to check supply***", a, minimumAircraftRequiredForCoverDuty);
                    if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg1, null);
                }
            }
        }

        string msg3 = acAvailableToPlayer_msg(player);
        Timeout(0.02, () =>
        {
            GamePlay.gpLogServer(new Player[] { player }, msg3, new object[] { });
        });

        retmsg += msg3 + nl;

        int numCheckedOut = numberAircraftCurrentlyCheckedOutPlayer(player);
        /*
        int maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_BomberPilots;
        if (isFighterAllowedCover(aircraft)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_FighterPilots;*/

        int maximumCheckoutsAllowedAtOnce = checkoutsAvailableToPlayer_num(player);

        Timeout(0.02, () =>
        {
            GamePlay.gpLogServer(new Player[] { player }, "You have {0} aircraft escorting you, of {1} maximum allowed at one time.", new object[] { numCheckedOut, maximumCheckoutsAllowedAtOnce });
        });

        return retmsg;
    }

    public string acAvailableToPlayer_msg(Player player)
    {
        int acAvailable = acAvailableToPlayer_num(player);
        AiAircraft aircraft = player.Place() as AiAircraft;
        int acAllowedThisPlayer = acAvailable + howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);

        string typeOfACexpl = " flying a fighter";
        if (aircraft == null) typeOfACexpl = " not in an aircraft";
        if (isBomberAllowedCover(aircraft)) typeOfACexpl = " flying a heavy bomber";
        if (isFighterAllowedCover(aircraft)) typeOfACexpl = " flying an early fighter";
        if (isFighterAllowedCover_wing(aircraft)) typeOfACexpl = " flying a late fighter";
        if (Calcs.isStrikeAC(aircraft)) typeOfACexpl = " flying a " + mainmission.statsmission.stb_StrikeName + " aircraft";
        if (isOnRepairMission(player))
        {
            typeOfACexpl = " for flying repair/restock missions";
            return string.Format("{0} cover aircraft per day are allowed {1} ", acAllowedThisPlayer, typeOfACexpl);
        }

        string rankExpl = "";
        if (TWCStbStatRecorder != null)
        {
            int numPlayer = CoverCalcs.numPlayersInArmy(player.Army(), this);
            rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name()) + typeOfACexpl + " and with " + numPlayer.ToString() + " friendly players online";
        }

        return string.Format("{0} remain available of your command squadron of {1} bomber & cover aircraft allowed{2}; {3} more are still in the air or being readied for re-use.", acAvailable, acAllowedThisPlayer, rankExpl, coverACStillInAirForPlayer_num(player));
    }

    public int acAvailableToPlayer_num(Player player)
    {
        AiAircraft aircraft = player.Place() as AiAircraft;
        int maximumAircraftAllowedPerMission = 0;

        /*if (isBomberAllowedCover(aircraft)) maximumAircraftAllowedPerMission = maximumAircraftAllowedPerMission_BomberPilots;
        else if ((isFighterAllowedCover(aircraft) && !Calcs.isStrikeAC(aircraft)) || (Calcs.isStrikeAC(aircraft) && Calcs.playerHasBombs(player))) maximumAircraftAllowedPerMission = maximumAircraftAllowedPerMission_FighterPilots;
		*/
		if (isBomberAllowedCover(aircraft)) maximumAircraftAllowedPerMission = maximumAircraftAllowedPerMission_BomberPilots;
        else if ((isFighterAllowedCover(aircraft) && !Calcs.isStrikeAC(aircraft)) || (Calcs.isStrikeAC(aircraft))) maximumAircraftAllowedPerMission = maximumAircraftAllowedPerMission_FighterPilots;
        else if (isFighterAllowedCover_wing(aircraft)) maximumAircraftAllowedPerMission = maximumAircraftAllowedPerMission_FighterPilots_wing;
		
        if (isOnRepairMission(player)) maximumAircraftAllowedPerMission = maximumAircraftAllowedPerMission_RepairMission;

        int acAllowedThisPlayer = maximumAircraftAllowedPerMission;

        int numPlayer = CoverCalcs.numPlayersInArmy(player.Army(), this);

        if (numPlayer > maxPlayersToAllowCover) { return 0; }

        if (numPlayer <= numPlayersToIncreaseCover) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling(maximumAircraftAllowedPerMission * 1.5));

        //string rankExpl = "";
        if (TWCStbStatRecorder != null)
        {
            double adder = ((double)TWCStbStatRecorder.StbSr_RankAsIntFromName(player.Name()) - 1.0) / 2.0;
            if (adder < 0) adder = 0;

            acAllowedThisPlayer += Convert.ToInt32(adder);

            if (numPlayer > numPlayersToReduceCover) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling((double)acAllowedThisPlayer / 2.0)); //Ceiling to run up to nearest integer, using ceiling here is being a bit nice to pilots . . .
            if (numPlayer > ((double)maxPlayersToAllowCover - (double)numPlayersToReduceCover) / 2.0 + (double)numPlayersToReduceCover) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling((double)acAllowedThisPlayer / 3.0));
            //rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name()) + "and with " + numPlayer.ToString() + " friendly players online";

        }
        int acAvailable = acAllowedThisPlayer - howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);
        //Always allow at least 10 a/c for repair/ferry missions
        if (isOnRepairMission(player) && acAvailable < 20) acAvailable = 20;
        if (isOnFerryMission(player) && acAvailable < 20) acAvailable = 20;
        if (acAvailable < 0) acAvailable = 0;
        return acAvailable;
    }

    //NOT WORKING OR USED NOW

    /*
public string acSimultaneousCheckoutsAvailableToPlayer_msg(Player player)
{
    int checkoutsAvailable = checkoutsAvailableToPlayer_num(player);
    AiAircraft aircraft = player.Place() as AiAircraft;
    string typeOfACexpl = " flying a fighter";
    if (aircraft == null) typeOfACexpl = " not in an aircraft";
    if (isBomberAllowedCover(aircraft)) typeOfACexpl = " flying a heavy bomber";
    if (isFighterAllowedCover(aircraft)) typeOfACexpl = " flying a fighter-bomber";

    string rankExpl = "";
    if (TWCStbStatRecorder != null)
    {
        int numPlayer = coverCalcs.numPlayersInArmy(player.Army(), this);
        rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name()) + typeOfACexpl + " and with " + numPlayer.ToString() + " friendly players online";
    }
    int acAllowedThisPlayer = acAvailable + howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);
    return string.Format("{0} remain available of your command squadron of {1} bomber & cover aircraft allowed{2}; {3} more are still in the air or being readied for re-use.", acAvailable, acAllowedThisPlayer, rankExpl, coverACStillInAirForPlayer_num(player));
}
*/


    public int checkoutsAvailableToPlayer_num(Player player)
    {
        AiAircraft aircraft = player.Place() as AiAircraft;
        int maximumCheckoutsAllowedAtOnce = 0;

        if (isBomberAllowedCover(player)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_BomberPilots;
        //else if ((isFighterAllowedCover(aircraft) && !Calcs.isStrikeAC(aircraft)) || (Calcs.isStrikeAC(aircraft) && Calcs.playerHasBombs(player))) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_FighterPilots;
		else if ((isFighterAllowedCover(aircraft) && !Calcs.isStrikeAC(aircraft)) || Calcs.isStrikeAC(aircraft)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_FighterPilots;
        else if (isFighterAllowedCover_wing(aircraft)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_FighterPilots_wing;
		
        if (isOnRepairMission(player)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_RepairMission;
        if (isOnFerryMission(player)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_FerryMission;

        if (!isOnRepairMission(player))
        {
            int numPlayer = CoverCalcs.numPlayersInArmy(player.Army(), this);

            if (numPlayer > numPlayersToReduceCheckoutsEvenMore) { maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce / 4; }
            else if (numPlayer > numPlayersToReduceCheckoutsMore) { maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce / 2; }
            else if (numPlayer > numPlayersToReduceCheckouts) { maximumCheckoutsAllowedAtOnce = 3 * maximumCheckoutsAllowedAtOnce / 4; }
        }


        return maximumCheckoutsAllowedAtOnce;
    }

    public int coverACStillInAirForPlayer_num(Player player)
    {
        int count = 0;
        foreach (AiActor actor in coverAircraftActorsCheckedOut.Keys)
        {
            if (player == coverAircraftActorsCheckedOut[actor]) count++;
        }
        return count;
    }

    public Player getOwnerOfCoverAircraft(AiActor actor)
    {
        //AiActor actor = aircraft as AiActor;
        if (actor == null) return null;

        if (coverAircraftActorsCheckedOut.ContainsKey(actor))
        {
            Console.WriteLine("Found aircraft belonging to: " + coverAircraftActorsCheckedOut[actor].Name());
            return coverAircraftActorsCheckedOut[actor];
        }
        return null;
    }

    public bool isFighterOrStrikeAllowedCoverForThisAircraft(Player player, string key)
    {
        if (player == null || player.Place() as AiAircraft == null) return false;
        //if (isFighterAllowedCover(player) && !(Calcs.isStrikeAC(player) && Calcs.playerHasBombs(player)) && !isHeavyBomber(key)) return false;
		//if (Calcs.isStrikeAC(player) && !Calcs.playerHasBombs(player)) return false;		
        //if ((Calcs.isStrikeAC(player) && !isDiveBomber(player) && !Calcs.isStrikeAC(key) && !isHeavyBomber(key))) return false;
		
		//StrikeAC can only take other StrikeAC
		if (Calcs.isStrikeAC(player) && !Calcs.isStrikeAC(key)) return false;

        if (isFighterAllowedCover_wing(player) && !isFighterAllowedFor_fighterwing(key)) return false;
		
		//Fighters who are NOT StrikeAC can only take Heavy Bombers
		if ((isFighterAllowedCover(player) && !Calcs.isStrikeAC(player) && !isFighterAllowedCover_wing(player) && !isHeavyBomber(key))) return false;
		
		//Bombers can take anything
        return true;
    }

    public string selectCoverPlane(string acName, ArmiesE army, Player player)
    {
        string retplane = "";

        if (isOnRepairMission(player))
        {
            if (player == null || player.Place() == null || player.Place() as AiAircraft == null) return "";
            return (player.Place() as AiCart).InternalTypeName(); //repair missions, we always  just give the player their own aircraft type as cover a/c - this allow for example ferrying several aircraft of the same type the player is flying            
        }

        if (!(army == ArmiesE.Blue || army == ArmiesE.Red) || CoverAircraftCurrentlyAvailable[army] == null) return "Cover: Aircraft availability not initialized or wrong army selected";
        List<string> aircraftChoices = new List<string>();

        //else if (army == ArmiesE.None) { armylist.Add(ArmiesE.Red); armylist.Add(ArmiesE.Blue); }

        int numChoice = -1;
        if (!Int32.TryParse(acName, out numChoice)) numChoice = -1;
        //if (numChoice >= 0 && numChoice < CoverAircraftCurrentlyAvailable[army].Count) returnCoverAircraftCurrentlyAvailable[army][numChoice].Key;

        int count = 0;

        foreach (string key in CoverAircraftCurrentlyAvailable[army].Keys)
        {
            //string acn = returnCoverAircraftCurrentlyAvailable[army][key];
            //string msg = string.Format("#{0} {1} {2}", i, Calcs.ParseTypeName(CoverAircraftCurrentlyAvailable[a].Key), CoverAircraftCurrentlyAvailable[a].Entry);
            if (!isFighterOrStrikeAllowedCoverForThisAircraft(player, key)) continue; //So any of the fighters allowed cover can take a heavy bomber out.  But strike fighters can take a heavy bomber OR another strike fighter of any type
            count++;
            if (numChoice > 0 && count == numChoice) aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty));
            if (numChoice <= 0 && acName.Length > 0 && key.ToLowerInvariant().Contains(acName.Trim().ToLowerInvariant())) aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty)); //implement substring matching "<cover beau             

        }

        //If choice by ID# or a/c name hasn't produced any matches, then we just add all a/c available.  aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key])); makes it add a choice for each a/c available so the selection is biased to select a/c for which more are available.
        if (aircraftChoices.Count == 0) foreach (string key in CoverAircraftCurrentlyAvailable[army].Keys)
            {
                //string acn = returnCoverAircraftCurrentlyAvailable[army][key];
                //string msg = string.Format("#{0} {1} {2}", i, Calcs.ParseTypeName(CoverAircraftCurrentlyAvailable[a].Key), CoverAircraftCurrentlyAvailable[a].Entry);
                if (!isFighterOrStrikeAllowedCoverForThisAircraft(player, key)) continue;
                count++;
                aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty));
            }

        if (aircraftChoices.Count == 0) aircraftChoices = new List<string>(CoverAircraftCurrentlyAvailable[army].Keys);

        retplane = CoverCalcs.randSTR(aircraftChoices.ToArray());

        //return CoverCalcs.ParseTypeNameToPlainType(retplane);
        return retplane;

    }

    /*
    public void togglePlayerAircraftPointVSActorVSLastBombTargeting(Player player)
    {
        bool currenttargetActorInsteadofPoint = false;
        bool newtargetActorInsteadofPoint = true;
        bool started = false;
        GamePlay.gpLogServer(new Player[] { player }, "COVER: Trying to change your Cover Airgroup targeting mode . . . ", new object[] { });

        if (coverAircraftAirGroupsIndexes.ContainsKey(player))
        {
            var agIndex = coverAircraftAirGroupsIndexes[player];
            int numAGs = agIndex.Count;
            //GamePlay.gpLogServer(new Player[] { player }, "COVER: You have" + numAGs.ToString() + " airgroups currently in the air", new object[] { });
            foreach (KeyValuePair<int, AiAirGroup> kv in agIndex)
            {
                AiAirGroup airGroup = kv.Value;

                Point3d tpos = new Point3d(-1, -1, -1);

                //if (airGroup == null) continue;
                //So if we haven't set a target point yet (knickebein etc) there won't be anything for this airgroup in airgroupTargetPoints.  So, we'll just make it.
                if (!airgroupTargetPoints.ContainsKey(airGroup))
                {
                    currenttargetActorInsteadofPoint = false;
                    newtargetActorInsteadofPoint = !currenttargetActorInsteadofPoint;
                    started = true;
                }
                else
                {

                    tpos = airgroupTargetPoints[airGroup];
                    if (!started)
                    {
                        if (tpos.z == -5000) currenttargetActorInsteadofPoint = true;
                        else currenttargetActorInsteadofPoint = false;

                        newtargetActorInsteadofPoint = !currenttargetActorInsteadofPoint; //toggle whatever the current value is
                        started = true;
                    }
                }

                tpos.z = -1;
                
                if (newtargetActorInsteadofPoint) tpos.z = -5000;

                airgroupTargetPoints[airGroup] = tpos;

                string m = "COVER: Cover airgroup targeting mode switched to OBJECT (nearest enemy objects found within 3.5km of the point)";
                if (!newtargetActorInsteadofPoint) m = "COVER: Cover airgroup targeting mode switched to POINT.";
                GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });

            }

        } else
        {
            string m = "COVER: Sorry, you don't have any current Cover Airgroups";
            GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });
        }         
    }
    */

    //How many checked out to player, within distance dist_m
    //if dist_m ==0, ignore the distance check
    public int numberAircraftCurrentlyCheckedOutPlayer(Player player, double dist_m = 0)
    {
        try
        {
            if (player == null || player.Place() == null) return 0;
            //AiAircraftplayerAircraft = player.Place() as AiAircraft;

            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int numret = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null || coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (dist_m > 0 && CoverCalcs.CalculatePointDistance(airGroup.Pos(), player.Place().Pos()) > dist_m) continue;
                numret += airGroup.GetItems().Length;
            }
            //GamePlay.gpLogServer(new Player[] { player }, "Cover: numcheckedout " + numret.ToString(), new object[] { });
            return numret;
        }
        catch (Exception ex) { Console.WriteLine("Cover, numberAircraftCurrently: " + ex.ToString()); return 0; }
    }

    //This prob should be in -supply.cs
    public int numberAircraftCurrentlyCheckedOutFromSupply(Player player)
    {
        try
        {

            //aircraftCheckedOutInfo { get; set; } //Info about each a/c that is checked out <Army, Pilot name(s), Aircraft Type, time checked out>
            if (supplymission == null) return 0;
            if (player == null || player.Name() == null) return 0;
            string playerName = player.Name();
            int numret = 0;
            HashSet<AiActor> actorsNotCheckedIn = new HashSet<AiActor>(supplymission.aircraftCheckedOut);

            actorsNotCheckedIn.ExceptWith(supplymission.aircraftCheckedIn); //remove all a/c that have been checked in

            foreach (AiActor actor in actorsNotCheckedIn) //Can't use aircraftCheckedOutInfo.Keys bec it includes ALL ac, even those already checked in
            {
                Tuple<int, string, string, DateTime> item = supplymission.aircraftCheckedOutInfo[actor];
                //Console.WriteLine("Cover, numberAircraftCurrentlyCheckedOutFromSupply, item: {0} {1} {2}", item.Item2, item.Item3, item.Item4);
                //if (!item.Item2.Contains(playerName)) continue;     //playernames item could include stuff like TWC_Flug - TWC_Fatal_Error - TWC_Fark if there are multiple ppl in the a/c // but for this purpose we're only counting a/c against them if they are the only/primary piloft.  Thinking about bombers without multiple positions, etc
                if (item.Item2 != playerName) continue;
                numret++;
            }
            return numret;


        }
        catch (Exception ex) { Console.WriteLine("Cover, numberAircraftCurrentlyCheckedOutFromSupply ERROR: " + ex.ToString()); return 0; }
    }

    public void checkPlayerAirgroups(Player player, bool missing = false)
    {

        /*
         * 
         * 
            public Dictionary<AiActor, Player> coverAircraftActorsCheckedOut = new Dictionary<AiActor, Player>();
            public Dictionary<AiAirGroup, Player> coverAircraftAirGroupsActive = new Dictionary<AiAirGroup, Player>();
            public Dictionary<AiAirGroup, Point3d> coverAircraftAirGroupsTargetPoint = new Dictionary<AiAirGroup, Point3d>();
            public Dictionary<AiAirGroup, bool> coverAircraftAirGroupsReleased = new Dictionary<AiAirGroup, bool>(); //When pilots die, bombers can continue to attack for 5mins or so more; this sets the time to release them

        */

        //OK, so if we are here then something is null that shouldn't be.  So that means a person must have had a checked out aircraft disappear or whatever somewhere along the line.  Give it back now.
        if (missing)
        {
            numberCoverAircraftActorsCheckedOutWholeMission[player]--;

            if (numberCoverAircraftActorsCheckedOutWholeMission[player] < 0) numberCoverAircraftActorsCheckedOutWholeMission[player] = 0;
        }

        foreach (KeyValuePair<AiActor, Player> kv in coverAircraftActorsCheckedOut)
        {
            AiActor actor = kv.Key;
            if (kv.Value == player && (actor == null || (kv.Key as AiAircraft).AirGroup() == null))
            {
                if (coverAircraftActorsCheckedOut.ContainsKey(actor))
                {
                    if (player != null & player.Name() != null) Console.WriteLine("PlayerCheck, actor doesn't exist: " + player.Name());
                    if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, reason:"SAFE_cover_Player/LeaderIsGone-ReturningCoverAircraft"
				);
                    numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                    coverAircraftActorsCheckedOut.Remove(actor);
                }
            }
        }

        if (coverAircraftAirGroupsIndexes.ContainsKey(player))
        {
            var agIndex = coverAircraftAirGroupsIndexes[player];
            foreach (KeyValuePair<int, AiAirGroup> kv in coverAircraftAirGroupsIndexes[player])
            {
                Timeout(0.1, () =>
                {
                    if (kv.Value == null) removeFromAirgroupIndex(player, kv.Key);
                });
            }
        }

        if (coverAircraftActorsIndexes.ContainsKey(player))
        {
            var acIndex = coverAircraftActorsIndexes[player];
            foreach (KeyValuePair<Tuple<int, AiActor>, AiAirGroup> kv in coverAircraftActorsIndexes[player])
            {
                Timeout(0.2, () =>
                {
                    if (kv.Key.Item2 == null) removeFromAirgroupIndex(player, kv.Key);
                });
            }
        }
    }

    private void removeFromAirgroupIndex(Player player, Tuple<int, AiActor> tup)
    {
        int index = tup.Item1;
        removeFromAirgroupIndex(player, index);
    }

    private void removeFromAirgroupIndex(Player player, int index)
    {
        int count = 1;
        var agIndex = coverAircraftAirGroupsIndexes[player];
        var agCopy = new Dictionary<int, AiAirGroup>(agIndex);
        foreach (KeyValuePair<int, AiAirGroup> kv in agCopy)
        {
            if (kv.Key != index) continue;
            count++;
            agIndex.Remove(index);
        }
        coverAircraftAirGroupsIndexes[player] = agIndex;

        var acIndex = coverAircraftActorsIndexes[player];
        var acCopy = new Dictionary<Tuple<int, AiActor>, AiAirGroup>(acIndex);
        foreach (KeyValuePair<Tuple<int, AiActor>, AiAirGroup> kv in acCopy)
        {
            if (kv.Key.Item1 != index) continue;
            count++;
            //add in the checked out aircraft back to the allowed list for the player
            numberCoverAircraftActorsCheckedOutWholeMission[player]--;
            if (numberCoverAircraftActorsCheckedOutWholeMission[player] < 0) numberCoverAircraftActorsCheckedOutWholeMission[player] = 0;

            if (player != null & player.Name() != null) Console.WriteLine("removeFromAirgroupIndex1, actor doesn't exist, returning it: " + player.Name());

            AiActor actor = kv.Key.Item2;

            if (actor != null)
            {
                if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, reason:"SAFE_cover_AircraftReturned");
                numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                coverAircraftActorsCheckedOut.Remove(actor);
            }

            acIndex.Remove(kv.Key);

        }
        coverAircraftActorsIndexes[player] = acIndex;

        Console.WriteLine("removeFromAirgroupIndex1, removed from airgroups & actors: " + count.ToString());

    }

    //some indexing so we can track exactly what is checked out to a player.  some of them seem to be disappearing?
    private void addToIndexes(Player player, AiAirGroup newAirgroup)
    {
        int indx = 0;
        if (playerIndex.ContainsKey(player)) indx = playerIndex[player];
        /* 
         public Dictionary<AiAirGroup, Player> coverAircraftAirGroupsIndexes = new Dictionary<Player, Dictionary<int, AiAirGroup>>();
         public Dictionary<AiAirGroup, Player> coverAircraftActorsIndexes = new Dictionary<Player, Dictionary<int, AiActor>>();        
         */

        var agIndex = new Dictionary<int, AiAirGroup>();
        if (coverAircraftAirGroupsIndexes.ContainsKey(player)) agIndex = coverAircraftAirGroupsIndexes[player];
        agIndex[indx] = newAirgroup;
        coverAircraftAirGroupsIndexes[player] = agIndex;

        foreach (AiActor actor in (newAirgroup as AiGroup).GetItems())
        {
            var acIndex = new Dictionary<Tuple<int, AiActor>, AiAirGroup>();
            if (coverAircraftActorsIndexes.ContainsKey(player)) acIndex = coverAircraftActorsIndexes[player];
            acIndex[new Tuple<int, AiActor>(indx, actor)] = newAirgroup;
            coverAircraftActorsIndexes[player] = acIndex;
        }
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

        if (msg.StartsWith("<lactorsall") && (admin_privilege_level(player) > 1))
        {
            Console.WriteLine("FULL actor list - starting...");
			mainmission.twcLogServer(new Player[] { player }, "FULL actor list - starting (to CONSOLE only) ...");
            Point3d p = new Point3d(284703, 125257, 0);
            double r = 9000;

            if (player != null && player.Place() != null) p = player.Place().Pos();

            string[] words = msg_orig.Split(' ');

            List<AiActor> closeStaticActors = new List<AiActor>();
            //Calcs.listStatics(GamePlay, new List<string>() { "smoke", "fire", "crater", "jerry" });
            lock (allStaticActors_lock)
            {
                closeStaticActors = allStaticActors.ToList(); 
            }
            //Finding actors we're going to range wider 1500. meters IN reality maybe we could look up the objective radius.  But actors nearby will be flak, etc etc etc.  All helpful.            
            foreach (AiActor act in closeStaticActors) {
				Console.WriteLine(string.Format("Actor: {0} {1} {5} {2:N0} {3:N0} army: {4}", act.Name(), (act as AiCart).InternalTypeName(), act.Pos().x, act.Pos().y, act.Army() , Calcs.correctedSectorNameDoubleKeypad(mainmission, act.Pos())));
				
			}

        }
		else if (msg.StartsWith("<lactors") && (admin_privilege_level(player) > 1))
        {
            Console.WriteLine("Actor list - starting...");
			mainmission.twcLogServer(new Player[] { player }, "Actor list - starting...");
            Point3d p = new Point3d(284703, 125257, 0);
            double r = 9000;

            if (player != null && player.Place() != null) p = player.Place().Pos();

            string[] words = msg_orig.Split(' ');

            List<AiActor> closeStaticActors = new List<AiActor>();
            //Calcs.listStatics(GamePlay, new List<string>() { "smoke", "fire", "crater", "jerry" });
            lock (allStaticActors_lock)
            {
                closeStaticActors = new List<AiActor>(CoverCalcs.gpGetAllGroundActorsNear(allStaticActors, p, r).ToList()); //1000?
            }
            //Finding actors we're going to range wider 1500. meters IN reality maybe we could look up the objective radius.  But actors nearby will be flak, etc etc etc.  All helpful.            
            foreach (AiActor act in closeStaticActors) {
				mainmission.twcLogServer(new Player[] { player }, string.Format("Actor: {0} {1} {5} {2:N0} {3:N0} army: {4}", act.Name(), (act as AiCart).InternalTypeName(), act.Pos().x, act.Pos().y, act.Army() , Calcs.correctedSectorNameDoubleKeypad(mainmission, act.Pos())));
				//Console.WriteLine("Actor: {0} {1}", act.Name(), (act as AiCart).InternalTypeName());
			}

        }
		else if (msg.StartsWith("<lgroundall") && (admin_privilege_level(player) > 1))
        {
          
			Point3d pos = new Point3d (250000,250000,0);
			
			if (player != null && player.Place() != null) pos = player.Place().Pos();
			
			string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
			
			string saveFile =  "/sectionfiles/"+ currentDateTime +"_ALLGroundStationary-list.txt";
			
			string msg111 = string.Format("ALL Groundstationary list to file {0} - starting...", saveFile);
			
			Console.WriteLine(msg111);
			mainmission.twcLogServer(new Player[] { player }, msg111 );
		
			CoverCalcs.listAllGroundStationaries(this, GamePlay, new Player[] {player}, missionNumber: -1, initPos: pos, radius_m: -1, saveFile: saveFile);
		}		
		
		else if (msg.StartsWith("<lground") && (admin_privilege_level(player) > 1))
        {
            Console.WriteLine("Groundstationary list to file - starting...");
			mainmission.twcLogServer(new Player[] { player }, "Groundstationary list to file - starting...");
			
			Point3d pos = new Point3d (250000,250000,0);
			
			if (player != null && player.Place() != null) pos = player.Place().Pos();
			
			string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
			
			string saveFile =  "/sectionfiles/"+ currentDateTime +"_GroundStationary-list.txt";
			
			string msg111 = string.Format("Groundstationary list to file {0} - starting...", saveFile);
			
			Console.WriteLine(msg111);
			mainmission.twcLogServer(new Player[] { player }, msg111 );
		
			CoverCalcs.listAllGroundStationaries(
				this,
			 GamePlay,
				new Player[] {player},
				missionNumber: -1,
				initPos: pos,
				radius_m: 10000,
				saveFile: saveFile
			);
		}

        /*
         * \
          List<AiActor> closeStaticActors = new List<AiActor>(coverCalcs.gpGetAllGroundActorsNear(allStaticActors, pos, maxMove_m).ToList()); //1000?
                                                                                                                                                                    //Finding actors we're going to range wider 1500. meters IN reality maybe we could look up the objective radius.  But actors nearby will be flak, etc etc etc.  All helpful.
                                coverCalcs.Shuffle(closeStaticActors);
                                foreach (AiActor act in closeStaticActors)
         * */
        else if (msg.StartsWith("<cland"))
        {
            landCoverAircraft(player, msg);

        }
        else if (msg.StartsWith("<clist")) //<clist
        {
            if (player == null) return;
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for controlling your Cover/Bomber Aircraft when possible", null);
            listCoverAircraftCurrentlyAvailable((ArmiesE)player.Army(), player);

        }
        else if (msg.StartsWith("<cp")) //<cpos
        {
            if (player == null) return;
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for controlling your Cover/Bomber Aircraft when possible", null);
            listPositionCurrentCoverAircraft(player);

        }
        else if (msg.StartsWith("<cover"))
        {
            checkoutCoverAircraft(player, msg_orig.Substring(6).Trim());
        }
        else if (msg.StartsWith("<cdist"))
        {
            float shiftFactor = setShiftFactor(player, msg_orig.Substring(6).Trim());
            GamePlay.gpLogServer(new Player[] { player }, ">>>Spread factor for your cover aircraft set to " + shiftFactor.ToString("F0") + "%", null);

        }
        //<cfdist - the front/back counterpart to <cdist.  <cdist sets the lateral (left/right) spread;
        //this nudges the formation along the leader's heading, so "<cfdist 10" asks them to ride 10m
        //ahead of him and "<cfdist -10" 10m behind.  Bare "<cfdist" = back to zero.  +/-3km max.
        else if (msg.StartsWith("<cfdist"))
        {
            double fbd_m = 0;
            string arg = msg_orig.Substring(7).Trim();
            bool ok = true;
            if (arg.Length > 0)
            {
                try { fbd_m = Convert.ToDouble(arg); }
                catch (Exception ex) { ok = false; }
            }
            if (!ok)
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Usage: <cfdist [metres] - positive to have your cover aircraft sit that many metres AHEAD of you, negative to sit behind.  For example <cfdist 10, or <cfdist -100.  Bare <cfdist resets to 0.", new object[] { });
            }
            else
            {
                //keep it sane - anything beyond +/-3km is not a formation any more
                if (fbd_m > 3000) fbd_m = 3000;
                if (fbd_m < -3000) fbd_m = -3000;
                coverFrontBackDist_m[player] = fbd_m;
                string fbstr = (fbd_m > 0 ? fbd_m.ToString("F0") + "m ahead of you" : fbd_m < 0 ? Math.Abs(fbd_m).ToString("F0") + "m behind you" : "back to your own position");
                GamePlay.gpLogServer(new Player[] { player }, ">>>Cover aircraft front/back offset is now " + fbstr + " (<cfdist again to change it)", new object[] { });
            }

        }
        else if (msg.StartsWith("<cr") || msg.StartsWith("<cj"))
        { //reserve - hold fire, force the AG to follow continuously & not attack (<creserve, <cres, <cr, <cjoin, <cj)
            setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.reserve, "were ordered to stay in RESERVE, cease ground attacks, bombers stay in formation, sturmovik/cover fighters defend only immediate/very close air threats.");

        }
        else if (msg.StartsWith("<ca"))
        { //attack - the AG can attack again, as ordered by the player's bomb aim mode (<cattack, <catt, <ca)
            setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.attack, "were instructed to GROUND ATTACK as ordered; fighters/sturmovik will vigorously attack nearby and moderately distant air threats.");

        }
        else if (msg.StartsWith("<cnormal") || msg.StartsWith("<cnor") || msg.StartsWith("<cn"))
        { //normal - the usual behavior: bombers/sturmoviks stay in formation except when ground attacking, cover fighters stay in place unless directly attacking/defending (<cnormal, <cnor, <cn)
            setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.normal, "were ordered to return to their NORMAL behavior - ground attack as ordered, bombers stay in formation otherwise, fighters/sturmovik defend only against nearby air threats.");

        }
        else if (msg.StartsWith("<cstrict") || msg.StartsWith("<cstr") || msg.StartsWith("<cst") || msg.StartsWith("<cs"))
        { //strict - ignore all else & just fly in formation with the player (<cstrict, <cstr, <cst)
            setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.strict, "were ordered to fly in STRICT, close formation with you, at same altitude, and ignore all other action.");

        }
        else if (msg.StartsWith("<cescort") || msg.StartsWith("<ces") || msg.StartsWith("<ce"))
        { //escort - CLoD's ESCORT behavior for all a/c types: stay with & defend the player (<cescort, <ces, <ce)
            setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.escort, "were ordered to ESCORT you, vigorously defending you from enemy aircraft.");

        }
        else if (msg.StartsWith("<cloiter") || msg.StartsWith("<cloi") || msg.StartsWith("<clo") || msg.StartsWith("<cl"))
        { //loiter - stay in one place, circling (<cloiter, <cloi, <clo)
            List<AiAirGroup> loiterGroups = setCoverAircraftAirGroupsOrders(player, msg, CoverAGOrders.loiter, "were ordered to LOITER in place, circling until further orders.");
            //setLoiterPoints(loiterGroups, player.Place().Pos()); //can use player .pos like this
            setLoiterPoints(loiterGroups);  //but using a/g pos seems better most of the time

        }
        //ORDER MATTERS HERE: this must be tested BEFORE the <cdrop branch below.  <cdrop is matched with
        //StartsWith, so a <cdrop check placed first would swallow <cdropnow and this command would
        //never run.  Same trap for anything else we add starting "<cdrop..." - see CloDNotes.
        else if (msg.StartsWith("<cdropnow") || msg.StartsWith("<cdropn") || msg.StartsWith("<cbomb") || msg.StartsWith("<cbom"))
        { //dropnow - order an immediate release from every squadron, whatever order they are on (<cdropnow, <cbomb)
            dropBombsNow_player(player);

        }
        else if (msg.StartsWith("<cdrop") || msg.StartsWith("<cdro"))
        { //drop - "drop when I drop": hold your bombs and fly with the leader, then pull your release when he pulls his (<cdrop, <cdro)
            //<cdrop - set the BAM mode too, so the Tab-4-4-4-4-6 "Cover Targeting [..]" label shows
            //what is actually happening.  BAM_enterDropMode does the snapshot + orders + watcher.
            BAM_playerAimMode[player] = BAM_BombAimMode.Drop_When_I_Drop;
            //<cdrop 2026/10 Step C9 - the chat command is now a PURE ALIAS for the Tab-4 menu: it always means
            //EVERY group, and any squadron numbers after it are ignored.  The selective "<cdrop 3 6"
            //scope is gone: it let the chat command and the menu label disagree about who was armed,
            //and in log 02F a <cdrop that looked bare had armed only some squadrons - which is exactly
            //how a pass ends up with "detect" firing and nothing being released.  <creserve is still the
            //way to hold particular squadrons back.
            string cdropArgsIgnored = msg_orig.Substring(6).Trim();
            BAM_enterDropMode(player, "");   //blank msg = all groups, exactly what the menu sends
            if (cdropArgsIgnored.Length > 0 && GamePlay != null)
                GamePlay.gpLogServer(new Player[] { player }, ">>>COVER: <cdrop always covers ALL your cover groups - squadron numbers after it are ignored. Use <creserve 3 to hold particular squadrons back.", new object[] { });
            if (isOnRepairMission(player)) GamePlay.gpLogServer(new Player[] { player }, ">>>Note: on a repair/restock mission your cover aircraft fly a fixed formation and this will have no effect", new object[] { });

        }
        else if (msg.StartsWith("<flare"))
        {
            if (player != null && player.Place() != null)
            {
                GamePlay.gpLogServer(null, "FLARE DROPPED in sector {0}", new Object[] { Calcs.correctedSectorName(this, player.Place().Pos()) });


                double wait = 10;
                if (player.Place().Pos().z > 10) wait = player.Place().Pos().z / 120;  //person's terminal velocity is 50 m/s, we'll say something like a flare is a bit higher, say 120
                Timeout(wait, () =>
                {
                    Calcs.loadCratersAndSmoke(GamePlay, mainmission, player.Place().Pos().x, player.Place().Pos().y, 0, "BuildingFireSmall");  //this is the smallest type of smoke  "BuildingFireLarge" a bit larger.  Smoke1 Smoke2 BigSitySmoke etc all larger yet
                });
            }
            else { GamePlay.gpLogServer(new Player[] { player }, "COVER ERROR! Couldn't find your position because you are not in an aircraft.", null); }
        }
        else if (msg.StartsWith("<gnear") && (admin_privilege_level(player) > 1))
        {
            string newmsg = msg.Replace("<gnear", "").Replace(",", " ").Replace("(", " ").Replace(")", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Trim(); // remove the comma
            string[] words = newmsg.Split(' ');

            double x = 0;
            double y = 0;
            double d = 0;
            try { if (words[0].Length > 0) x = Convert.ToDouble(words[0]); }
            catch (Exception ex) { }
            try { if (words[1].Length > 0) y = Convert.ToDouble(words[1]); }
            catch (Exception ex) { }
            try { if (words[2].Length > 0) d = Convert.ToDouble(words[2]); }
            catch (Exception ex) { }

            var asa = new List<AiActor>();
            lock (allStaticActors_lock)
            {
                if (allStaticActors != null) asa = new List<AiActor>(allStaticActors);
            }

            var agan = CoverCalcs.gpGetAllGroundActorsNear(asa.ToArray(), new Point3d(x, y, 0), d).ToList();

            GamePlay.gpLogServer(new Player[] { player }, "Found {0} ground actors near the point:", new object[] { agan.Count });

            foreach (AiActor a in agan) GamePlay.gpLogServer(new Player[] { player }, string.Format(a.Name() + " {0:N0} {1:N0} {2:N0}", a.Pos().x, a.Pos().y, a.Pos().z), null);

        }
        /*
         *                
         * 
         * */
        else if (msg.StartsWith("<glist") && (admin_privilege_level(player) > 1))
        {
            AiActor[] aia = CoverCalcs.gpGetGroundActors(this, 1);
            //exit;

            GamePlay.gpLogServer(new Player[] { player }, "TWC ground actor list", null);

            var asa = new List<AiActor>();
            lock (allStaticActors_lock)
            {
                if (allStaticActors != null) asa = new List<AiActor>(allStaticActors);
            }

            if (asa != null) foreach (AiActor a in asa)
                {
                    string type = "";
                    if (a as AiGroundActor != null) type = (a as AiGroundActor).Type().ToString();
                    /*string title = "";
                    if (a as GroundStationary != null) title = (a as GroundStationary).Title;
                    string name = "";
                    if (a as GroundStationary != null) name = (a as GroundStationary).Name;
                    */
                    string typename = "";
                    if (a as AiCart != null) typename = (a as AiCart).InternalTypeName().ToString();
                    GamePlay.gpLogServer(new Player[] { player }, string.Format(a.Name() + " type: {3} InternalTypeName: {4} {0:N0} {1:N0} {2:N0} alive: {5} valid: {6}", a.Pos().x, a.Pos().y, a.Pos().z, type, typename, a.IsAlive(), a.IsValid()), null);
                }

        }
        else if (msg.StartsWith("<galist") && (admin_privilege_level(player) > 1))
        {
            AiActor[] aia = CoverCalcs.gpGetGroundActors(this, 1);
            //exit;
            GamePlay.gpLogServer(new Player[] { player }, "CLoD's ground actor list:", null);

            CoverCalcs.listAllGroundActors(this, GamePlay, new Player[] { player });

        }
        else if (msg.StartsWith("<gslist") && (admin_privilege_level(player) > 1))
        {

            GamePlay.gpLogServer(new Player[] { player }, "CLoD's ground stationary list:", null);
            CoverCalcs.listAllGroundStationaries(this, GamePlay, new Player[] { player });


        }
        else if (msg.StartsWith("<chelp7"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - HELP PAGE 6/6",
                "** Targeting by POINT (Knickebein, bomb, or flare point) is good for heavy bombers who can blanket an AREA with ordnance.",
                "** Target by NEAREST ENEMY (to Knickebein, bomb, or flare point) is required for dive bombers and sturmovik aircraft to operate correctly & target effectively.",
                "** With a NEAREST ENEMY target, dive bombers and sturmovik aircraft, will actually do a dive bomb or close ground attack. Without it, they will simply drop from altitude.",
                "** NEAREST ENEMY targeting also works for heavy bombers to drop from altitude. It can be useful if you need to target specific naval or ground objectives - even moving/mobile objects.",
                "When targeting by NEAREST ENEMY, cover pilots will look for ground and naval targets near the given point",
                "and will search a wider radius if none is found.  They choose the highest-value targets they can.",
                "Each cover pilot will choose a different ground target in the given area, if possible.",
                "Cover pilots will inform you of their chosen target and its double-keypad location in <cover info listing.",
                "They must be quite close to the target point, 5-10km generally, before they can identify a specific ground enemy target.",
                "If pilots cannot identify a nearby or even moderately distant ground enemy, they will continue looking but revert to FOLLOW YOU flight plan. Target will be still be listed as \"(coord) AZ03.4.2\" or similar",
                "<<<<END OF COVER AIRCRAFT HELP>>>>"
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }
        else if (msg.StartsWith("<chelp6"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - GROUND ATTACK MODES - HELP PAGE 6/7",
                "Tab-4-4-4-4-7 has several modes of ground attack for your bombers & fighter-bombers:",
                "KNICKEBEIN POINT - attack the position of the current Knickebein point (<khelp for info on the KB system)",
                "** NEAREST ENEMY TO KNICKEBEIN POINT - find an enemy ground vehicle, ship, AA gun, train, or other ground object near the Knickebein Point and attack it.",
                "** NEXT BOMB DROP POINT - when you drop your NEXT bomb, the cover aircraft will note that point and attack it.",
                "** NEAREST ENEMY TO BOMB DROP POINT - note point of your next bomb drop and target for ground/naval enemies near that point.",
                "** DROP FLARE & TARGET FLARE DROP POINT - at the moment you press the button to select this option, you drop a flare.  Cover aircraft will attack the flare point.",
                "** DROP FLARE & TARGET ENEMIES NEAR DROP POINT - at the moment you press the button to select this option, you drop a second flare.  Cover aircraft will attack enemies near that point.",
                "IMPORTANT NOTE: Sturmovik/ground attack aircraft & Dive Bombers require 'NEAREST ENEMY' target points to ground attack/dive bomb. See <chelp7.",
                "For all \"ENEMIES NEAR\" targeting: If no enemy is found near the specified point, bombers will generally hold their fire and revert to 'Follow'. Watch your CHAT display for clues as to current target or failure to locate targets.",
                "For KNICKEBEIN point targets, you need to check Recon Reports for exact coordinates to target - ideally before you leave home base",
                "BOMB and FLARE drop targeting are more flexible. You can fly to the enemy, drop a bomb or flare to indicate your desired target point, and cover aircraft will target it (or enemies near it, ifor 'NEAREST ENEMY' targeting).",
                "<chelp7 for more..."
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }
        else if (msg.StartsWith("<chelp5"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - SQUADRON ORDERS - HELP PAGE 5/7",
                ">>SQUADRON ORDERS: Instruct squadrons to attack and defend as normal, or hold fire and join you, or attack nearby enemy aircraft more vigorously, or cover and defend you, etc.",
                ">>SQUADRON ORDERS: All orders can apply to specific squadrons, or to all squadrons at once. For example: <cn 2 5 puts squadrons #2 and #5 on NORMAL behavior. <cn (alone, no numbers) puts ALL squadrons on NORMAL behavior.",
                "** <cnormal OR <cn - NORMAL behavior: attack ground targets if you direct via the TAB-4 menu; fighters/sturmovik will leave formation to defend against nearby air enemies (default behavior).",
                "** <cattack OR <ca - ATTACK ground targets if instructed by Tab-4 menu; fighters/sturmovik will vigorously attack any enemy aircraft they see rather than waiting for them to approach.",
                "** <cescort OR <ce - ESCORT you: stay with you & vigorously defend you from enemy aircraft near you, turn and fight nearby enemies, leaving formation if necessary (even bombers); discontinue ground attacks.",
                "** <creserve OR <cr - stay in RESERVE, joined with you; do not join the current ground attack. Stay in formation, but fighters/sturmovik will leave formation to defend against enemy approaching closely.",
                "** <cstrict OR <cs - squadrons fly in rigid STRICT, close formation with you, all aircraft at your altitude, close to you (ignoring <cdist), ignores all other action, & ordered to ignore even direct attacks and simply fly in formation with you.",
                "** <cloiter OR <cl - LOITER in place, circling. Will defend if attacked, but otherwise remain out of the action and awaiting further orders.",
                "** <cdrop OR <cdro OR Tab-4-4-4-4-6 (Cover Targeting cycles to 'Drop When I Drop') - DROP WHEN I DROP: they hold their bombs and fly in tight formation with you, and the moment you let your first bomb go they release everything at the same time - just as ww2 crews did, with only the leader carrying a bombsight. Squadrons still further back will catch up to your line and release there. <cdrop always covers ALL your cover groups (squadron numbers after it are ignored) - use <creserve 3 to hold particular squadrons back. Wait until every group shows GND ATTACK on the chat display, then they are awaiting your drop. Re-issue <cdrop to re-arm them for another run.",
                "** <cdropnow OR <cbomb - order an IMMEDIATE release from every squadron now, whatever order they are on. Squadrons sitting on <creserve are held back; everything else goes, including <cstrict squadrons.",
                "<chelp6 for more..."
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }
        else if (msg.StartsWith("<chelp4"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - BOMB LOADS - HELP PAGE 4/7",
                "Bombers, Sturmovik/Strike Aircraft, and Fighter-bombers always include bombs loaded by default.",
                "** You can force the fighter or bomber version of your cover aircraft by specifying 'fighter' or 'fi'",
                "or 'bomber' or 'bo' at the end of the <cover command.",
                "** OR specify 'heavy' or 'he' to load heavy bombs, when available (generally 2000lb/1000kg)",
                "(Default bomb load is many smaller bombs - good for extended area targets. \"Heavy\" specifies fewer but larger bombs. Less overall tonnage but will put more ordnance in one SMALL area. Good for ships, bunkers.)",
                "** Adding x3 x5 x9, etc, at the end of a command will repeat the command the specified number of times. Instead of typing \"<cover 5 1\" four times, just use \"<cover 5 1 x4\"",
                "Examples: <cover 21 fi | <cover 32 VI bomber | <cover 12 3 AS fighter x3 | <cover 3 bo | <cover 14 2 he",
                "<chelp5 for more..."
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }
        else if (msg.StartsWith("<chelp3"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - COVER AIRCRAFT AVAILABLE - HELP PAGE 3/7",
                "Cover aircraft include cover fighters and bombers. They are available to heavy bomber pilots, Sturmovik pilots and some fighter pilots.",
                "** Bomber pilots: Can call bombers to fly with you, cover fighters to fly above.",
                "** Sturmovik pilots: Can call a few Sturmovik to fly with you.",
                "** Fighter pilots (older models): Can call a few bombers to fly with you; you fly above the bombers as cover.",
                "** Fighter pilots (current models): Can call one or a few fighters to fly as your wing.",
                "NOTE: When flying low (to avoid radar detection), all aircraft will fly low at your altitude. As you gain altitude, bombers & cover fighters will start to maintain separate altitudes as described above.",
                "<chelp4 for more..."
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }
        else if (msg.StartsWith("<chelp2"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - BASIC COMMANDS - HELP PAGE 2/7",
                "** <cover 3 5 - means launch a flight of 5 aircraft of type #3",
                "** <cover 2 6 AS - means launch a flight of 6 aircraft of type #2, formation: ASTERN",
                "Formation types: VI=Vic, V3=Vic3, AB=Abreast, AS=Astern, RI=Right echelon, LE=Left echelon",
                "** <cover 4 3 heavy x3 - launch a flight of 3 aircraft type #4, loaded with heavy bombs, and repeat this command 3 times",
                "** <cland 2 4 5 release group #2, #4, and #5.  Get group # from Tab-4 menu or <cpos. <cland (or Tab-4 menu) alone lands all aircraft.",
                "** <cdist 200 - set cover formation distance 200% normal. <cdist 50 - set cover distance 50% normal. <cdist 1000 - cover distance 10X normal",
                "** <cfdist 10 - set your cover formation to ride 10m AHEAD of you (negative = behind, e.g. <cfdist -100). <cfdist alone resets to your own position. Range is +/-3000m. This is the front/back counterpart to <cdist, which sets the left/right spread.",
                "<chelp3 for more..."
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }
        else if (msg.StartsWith("<chelp"))
        {
            string[] helpMessages = {
                "COVER FIGHTER & BOMBER SYSTEM - HELP (1/7)",
                "The COVER system allows you to call in AI aircraft to fly with you and help you accomplish mission and objectives.",
                "** Tab-4-4-4-4 menu OR Chat Commands <cover OR <cover Beau OR <cover 3 OR <cover 3 6 AS:",
                "Launch a cover squadron of aircraft name or type # indicated. Optional: Add # of aircraft to launch, formation type, heavy (bombs), and x2 or x3 to repeat the command.",
                "** Tab-4 menu OR command <clist: List available cover fighters & ID#; <cpos - position of your current fighters",
                "** Tab-4-4-4-4-7: Set cover aircraft ground attack mode/target",
                "** <cnormal, <cstrict, <cescort, <cattack, <creserve & <cloiter 1 3: Give standing orders to e.g. squadrons 1 & 3 (details @ <chelp5)",
                "** Tab-4 menu OR command <cland: Release cover fighters to land (IMPORTANT!)",
                "<chelp2 for more..."
            };

            foreach (string message in helpMessages)
            {
                mainmission.twcLogServer(player, message);
            }
        }

        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1.6; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.2;

            string msg41 = "<cover - request cover bombers/ground attack/fighters to join you, <chelp - cover aircraft help, <flare - drop a flare now";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
    }

    bool getAllGroundActorsNear_asaCalled = false;

    public List<AiActor> getAllGroundActorsNear_clean(Point3d pos, double radius_m, int callCount = 0)
    { 
        try
        {
            var asa = new List<AiActor>();

            lock (allStaticActors_lock)
            {
                if (allStaticActors != null) asa = new List<AiActor>(allStaticActors);
            }

            if ((asa == null || asa.Count == 0 ) && !getAllGroundActorsNear_asaCalled)
            {
                
                renewAllStaticActors_recurs(onetime: true);
                Console.WriteLine("getAllGroundActorsNear_clean - asa is NULL, reloading asa...");
                getAllGroundActorsNear_asaCalled = true;
                return null; //meaning, asa hasn't been loaded yet
            }
            if (asa == null) return new List<AiActor>();
            var agan_arr = CoverCalcs.gpGetAllGroundActorsNear(asa.ToArray(), pos, radius_m);
            if (agan_arr == null ) return new List<AiActor>();
            var agan = agan_arr.ToList();
            return agan;
        }
        catch (Exception ex) { Console.WriteLine("getAllGroundActorsNear_clean ERROR: " + ex.ToString()); return new List<AiActor>(); }
    }
    //removes those matching this army, or if -1, ignores the army
   
    public void removeAllGroundActorsNear_clean(Point3d pos, double radius_m, int percentToRemove = 100, int armyToRemove = -1, List<string> types_to_remove = null, List<string> types_to_retain = null, bool preserveOnWater = false, int callCount = 0, bool fast = false)
    {
        try		
        {
			double wait1 = 5;
			double wait2 = 120;
			if (fast) {
				wait1 = 0;
				wait1 = 0;
			}


            List<AiActor> asa = getAllGroundActorsNear_clean(pos, radius_m);
            if (asa == null)
            {
                Console.WriteLine("getAllGroundActorsNear_clean - asa is NULL, asa so we wait and try again ...");
                if (callCount > 10) return;
                Timeout(10, () => { removeAllGroundActorsNear_clean(pos, radius_m, percentToRemove, armyToRemove, types_to_remove, types_to_retain, preserveOnWater, callCount + 1); });
                return;
            }

            Console.WriteLine("covermission removeAllGroundActorsNear: Starting");
            try
            {
                int count = 0;

                foreach (AiActor aa in asa)
                {
                    //try/catch in loop so if something goes awry with one groundactor it will continue with the rest
                    try
                    {
                        if (aa == null) continue;

                        if (armyToRemove != -1 && armyToRemove != aa.Army()) continue;


                        bool match = false;

                        //2022-12 - Don't use NAME for now as it now includes stuff like the objective ID
                        //
                        //string groundActorTypes = aa.Name() + (aa as AiCart).InternalTypeName();
                        string groundActorTypes = (aa as AiCart).InternalTypeName();
                        if (aa as AiGroundActor != null) groundActorTypes += (aa as AiGroundActor).Type();

                        if (groundActorTypes.ToLower().Contains("tank")) Console.WriteLine("RemoveGroundActor - checking: {0} ", groundActorTypes);

                        if (types_to_remove == null) match = true;
                        else foreach (string s in types_to_remove)
                            {
                                //We check NAME and groundactor TYPE and INTERNALTYPENAME for matches
                                if (groundActorTypes.ToLower().Contains(s.ToLower()))
                                {
                                    match = true;
                                    break;
                                }
                            }

                        if (match && types_to_retain != null) foreach (string s in types_to_retain)
                            {
                                //We check NAME and groundactor TYPE and INTERNALTYPENAME for matches                       
                                if (groundActorTypes.ToLower().Contains(s.ToLower()))
                                {
                                    match = false;
                                    break;
                                }
                            }

                        if (match && preserveOnWater)
                        {
                            if (Calcs.isPointInOrNearWater(GamePlay, aa.Pos(), radius_m: 10)) match = false;
                        }

                        if (!match) continue;

                        //if (groundActorTypes.ToLower().Contains("tank")) 
                        Console.WriteLine("RemoveGroundActor - this matched and will be removed! {0} ", groundActorTypes);

                        if (ran.Next(100) > percentToRemove) continue; //remove only a certain percent, if requested
						
						mainmission.statsmission.Stb_killActor((aa as AiActor), 0, "cover.Removing Enemy Ground Actors Near _clean");
                        Timeout((ran.NextDouble() * (wait2-wait1) + wait1), () => { (aa as AiCart).Destroy(); });  //somewhat cheap way to avoid deleting items in ggList while looping through it, but also spreads the removal of stationaries over 5 mins or so instead of just zapping them all at once, which usually looks fake.
                        count++;
                    }
                    catch (Exception ex)
                    {
                        if (aa != null) Console.WriteLine("Cover removeGroundActorsNear ERROR: Couldn't do something with name {0}", aa.Name());
                        else { Console.WriteLine("Cover removeGroundActorsNear ERROR: couldn't do something ERROR"); }
                    }
                }
                Console.WriteLine("cover.removeGroundActors: Removed {0} items... ", count);
            }
            catch (Exception ex) { Console.WriteLine("cover.removeGroundActors ERROR: " + ex.ToString()); }

        }
        catch (Exception ex) { Console.WriteLine("cover.removeAllGroundActorsNear_clean main ERROR: " + ex.ToString()); }

    }

    //removes ground actors matching army and types to remove, and standing on enemy territory
	//types_to_retain are not kept if their army != territory, but replaced by the same thing with correct army
    public void removeGroundActorsOnEnemyTerritory_clean(int percentToRemove = 100, int army = -1, List<string> types_to_remove = null, List<string> types_to_retain = null, bool preserveOnWater = false, List<string> types_to_skip = null, bool fast = false)
    {
        var asa = new List<AiActor>();
		
		var f = GamePlay.gpCreateSectionFile();
		
		double wait1 = 5;
		double wait2 = 120;
		if (fast) {
			wait1 = 0;
			wait2 = 0;
			asa = CoverCalcs.gpGetAllGroundActors(this).ToList();
			if (asa == null) return;
		} else {

			lock (allStaticActors_lock)
			{
				if (allStaticActors != null) asa = new List<AiActor>(allStaticActors);
			}
		}
		
        Console.WriteLine("covermission removeGroundActorsOnEnemyTerritory_clean: Starting");
        try
        {
            int count = 0;
 
            foreach (AiActor aa in asa)
            {
                try
                {

                    if (aa == null) continue;

                    if (army != -1 && army != aa.Army()) continue;

                    //2022-12 - Don't use NAME for now as it now includes stuff like the objective ID
                    //
                    //string groundActorTypes = aa.Name() + (aa as AiCart).InternalTypeName();
                    string groundActorName = (aa as AiCart).InternalTypeName();
					string groundActorTypes = groundActorName;
                    if (aa as AiGroundActor != null) groundActorTypes += (aa as AiGroundActor).Type();

                    //if (groundActorTypes.ToLower().Contains("tank")) Console.WriteLine("RemoveGroundActor - checking: {0} ", groundActorTypes);
					Console.WriteLine("RemoveGroundActor - checking: {0} {1} {2} {3}", aa.Name(), groundActorName, groundActorTypes, aa.Army());


                    bool remove = false;
					bool retain = false;
                    if (types_to_remove == null) remove = true;
                    else foreach (string s in types_to_remove)
                        {
                            if (groundActorTypes.ToLower().Contains(s.ToLower()))
                            {
                                remove = true;
                                break;
                            }
                        }
                    if (remove && types_to_retain != null) foreach (string s in types_to_retain)
                        {
                            //We check NAME and groundactor TYPE and INTERNALTYPENAME for matches                       
                            if (groundActorTypes.ToLower().Contains(s.ToLower()))
                            {
                                remove = true;
								retain = true;
                                break;
                            }
                        }
					if (remove && types_to_skip != null) foreach (string s in types_to_skip)
                        {
                            //We check NAME and groundactor TYPE and INTERNALTYPENAME for matches                       
                            if (groundActorTypes.ToLower().Contains(s.ToLower()))
                            {
                                remove = false;
                                break;
                            }
                        }	

                    if (remove && preserveOnWater)
                    {
                        if (Calcs.isPointInOrNearWater(GamePlay, aa.Pos(), radius_m: 10)) remove = false;
                    }
					
					if (!remove) continue;




                    if (ran.Next(100) > percentToRemove) continue; //remove only a certain percent, if requested

                    int terr = GamePlay.gpFrontArmy(aa.Pos().x, aa.Pos().y);

					//only nn object on neutral territory stay - if gb or de on nn, must be replaced at min
                    if (terr == 0 && aa.Army() == 0 ) continue;

					//if correct army on correct territory, it can stay
                    if (terr != 0 && aa.Army() == terr) continue;
					
					//If it is excluded from removing, but doesn't match army of its territory, 
					//it must be replaced with the same object but correct army 
					if (retain && aa.Army() != 0) {
						
						f = Calcs.makeStatic(f, GamePlay, mainmission, aa.Pos().x, aa.Pos().y, 0, groundActorName, 0, ArmiesSection[aa.Army()], staticprefix: "replace_enemy_actor");
						continue;
					}

                    //if (groundActorTypes.ToLower().Contains("tank")) 
                    Console.WriteLine("RemoveGroundActor - this matched and will be removed! {0} ", groundActorTypes);

					mainmission.statsmission.Stb_killActor((aa as AiActor), 0, "cover.Removing Enemy Ground Actors _clean");
                    Timeout((ran.NextDouble() * (wait2-wait1) + wait1), () => { 
						//Console.WriteLine("Cover removeEnemyGroundActors: Destroyed {0} {1}");
						Console.WriteLine("RemoveGroundActor - destroying now: {0} {1} {2} {3}", aa.Name(), groundActorName, groundActorTypes, aa.Army());
						(aa as AiCart).Destroy(); 
					});  //somewhat cheap way to avoid deleting items in ggList while looping through it, but also spreads the removal of stationaries over 5 mins or so instead of just zapping them all at once, which usually looks fake.
                    count++;
                }
                catch (Exception ex)
                {
                    if (aa != null) Console.WriteLine("Cover removeEnemyGroundActors ERROR: Couldn't do something with name {0}", aa.Name());
                    else { Console.WriteLine("Cover removeEnemyGroundActors ERROR: couldn't do something ERROR"); }
                }
            }
			//Note time 130s is longer than the longest possible .Destroy() time
			Timeout(wait2 + 10, () => { 
				GamePlay.gpPostMissionLoad(f); f.save(mainmission.CLOD_PATH + mainmission.FILE_PATH + "/sectionfiles" + "/" + "remove-replacenemyactors" + ran.Next(0,99).ToString()); 
				Console.WriteLine("Cover removeEnemyGroundActors: Loaded replacement aiactors");
			}); //testing}); 
            Console.WriteLine("cover.removeGroundActorsOnEnemyTerritory_clean: Removed {0} items ... ", count);
        }
        catch (Exception ex) { Console.WriteLine("cover.removeGroundActorsOnEnemyTerritory_clean ERROR: " + ex.ToString()); }


    }


    private bool isPlayerInPlane(Player player)
    {
        if (player == null) return false;
        if (player.Place() == null) return false;
        if (player.Place() as AiAircraft == null) return false;
        if (player.PersonPrimary() == null && player.PersonSecondary() == null) return false;
        return true;
    }
    private bool isOnRepairMission(Player player)
    {
        if (mainmission != null && mainmission.objectiverepairmission != null && mainmission.objectiverepairmission.orm_PlayersOnRepairMissionExpiration.ContainsKey(player)) return true; //deny cover to ppl flying repair missions
        return false;

    }
    //if they are on reapri msn AND it is a ferry msn (the ycan carry more a/c)
    private bool isOnFerryMission(Player player)
    {
        if (mainmission != null && mainmission.objectiverepairmission != null) {
            var mmorm = mainmission.objectiverepairmission;
            if (mmorm.orm_PlayersOnRepairMissionExpiration.ContainsKey(player)) {
                var player_info = mmorm.orm_PlayersOnRepairMissionExpiration[player];
                if (player_info.Item6 == ObjectiveRepairMission.RepairType.Ferry) return true;
            }            
        }
        return false;

    }
    private bool isFighterAllowedCover(Player player)
    {
        if (player == null) return false;
        if (player.Place() == null) return false;
        if (player.Place() as AiAircraft == null) return false;        
        return isFighterAllowedCover(player.Place() as AiAircraft);
    }
    private bool isFighterAllowedCover (AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isFighterAllowedCover(acType);
    }
    private bool isFighterAllowedCover(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isFighterAllowedCover(aircraft);

    }
    private bool isFighterAllowedCover(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        //all strike/sturmovik fighters except JU-87, because it is grouped in with regular bombers so can take out
        //even more a/c than the fighter-bombers
        if (acType.Equals("HurricaneMkI") || acType.Contains("HurricaneMkI_") || acType.Equals("SpitfireMkI") ||  acType.Contains("Bf-109E") 
			|| acType.Contains("Bf-109E-3B") || acType.Contains("Bf-109E-4B") 
		|| acType.Contains("G50") || acType.Contains("BlenheimMkIVF") || acType.Contains("BlenheimMkIVNF") || acType.Contains("CR42")) ret = true;
        return ret;
    }

    private bool isFighterAllowedCover_wing(Player player)
    {
        if (player == null) return false;
        if (player.Place() == null) return false;
        if (player.Place() as AiAircraft == null) return false;        
        return isFighterAllowedCover_wing(player.Place() as AiAircraft);
    }
    private bool isFighterAllowedCover_wing (AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isFighterAllowedCover_wing(acType);
    }
    private bool isFighterAllowedCover_wing(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isFighterAllowedCover_wing(aircraft);

    }
    private bool isFighterAllowedCover_wing(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        //all strike/sturmovik fighters except JU-87, because it is grouped in with regular bombers so can take out
        //even more a/c than the fighter-bombers
        if (acType.Contains("HurricaneMkII") || acType.Contains("SpitfireMkV") ||  acType.Contains("Bf-109F") 
		|| acType.Contains("Macchi") || acType.Contains("Martlet") || acType.Contains("Tomahawk") || acType.Contains("Kittyhawk"))  ret = true;
        return ret;
    }

    private bool isFighterAllowedFor_fighterwing(Player player)
    {
        if (player == null) return false;
        if (player.Place() == null) return false;
        if (player.Place() as AiAircraft == null) return false;        
        return isFighterAllowedFor_fighterwing(player.Place() as AiAircraft);
    }
    private bool isFighterAllowedFor_fighterwing (AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isFighterAllowedFor_fighterwing(acType);
    }
    private bool isFighterAllowedFor_fighterwing(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isFighterAllowedFor_fighterwing(aircraft);

    }
    private bool isFighterAllowedFor_fighterwing(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        //all strike/sturmovik fighters except JU-87, because it is grouped in with regular bombers so can take out
        //even more a/c than the fighter-bombers
        if (acType.Contains("Hurricane") || acType.Contains("Spitfire") ||  acType.Contains("Bf-109") 
		|| acType.Contains("Macchi") || acType.Contains("Martlet") || acType.Contains("Tomahawk") || acType.Contains("Kittyhawk"))  ret = true;
        return ret;
    }

    private bool isBomberAllowedCover(Player player)
    {
        if (player == null) return false;
        if (player.Place() == null) return false;
        if (player.Place() as AiAircraft == null) return false;
        return isBomberAllowedCover(player.Place() as AiAircraft);
    }
    private bool isBomberAllowedCover(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isBomberAllowedCover(acType);
    }
    private bool isBomberAllowedCover(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isBomberAllowedCover(aircraft);

    }
    private bool isBomberAllowedCover(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("Ju-88A") || acType.Contains("Ju-87") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkIV") || acType.Contains("Do-17") || acType.Contains("Wellington") || acType.Contains("HurricaneMkI_FB")) ret = true;
        if (acType.Contains("BlenheimMkIVF") || acType.Contains("BlenheimMkIVNF")) ret = false;
        return ret;
    }
    private bool isHeavyBomber(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isHeavyBomber(acType);
    }
    private bool isHeavyBomber(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isHeavyBomber(aircraft);

    }
    private bool isHeavyBomber(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        //We're including heavy two-engine fighter bombers here (strike-fighter, sturmovik), like the 110C-4B, Hurricane FBs, Beaufighter FBs, Ju-88C, so that people can fly a FB lead a squad of those while flying cover for them
        if (acType.Contains("Ju-88A") 
			//|| acType.Contains("Ju-88C") //no, this is actually a Sturmovik/ground attack type a/c
			|| acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkI") || acType.Contains("Do-17") || acType.Contains("Wellington")
          || acType.Contains("Do-215B")
		  //	  || acType.Contains("Bf-110C-4B") || acType.Contains("Bf-110C-6") || acType.Contains("Bf-110C-7") || acType.Contains("BeaufighterMkIC")
          || acType.Contains("Sunderland") || acType.Contains("Walrus") || acType.Contains("HurricaneMkI_FB") 
		  //|| acType.Contains("HurricaneMkIIb")) 
		  )
		  ret = true; //Contains("BlenheimMkI" includes BI, BIV, BIV Late, etc.
        if (acType.Contains("BlenheimMkIVF") || acType.Contains("BlenheimMkIVNF") || acType.Contains("BlenheimMkIF") || acType.Contains("BlenheimMkINF")) ret = false;
        return ret;
    }
    private bool isDiveBomber(Player player)
    {
        if (player == null || player.Place() == null || player.Place() as AiAircraft == null) return false;        
        return isDiveBomber(player.Place() as AiAircraft);
    }
    private bool isDiveBomber(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isDiveBomber(acType);
    }
    private bool isDiveBomber(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isDiveBomber(aircraft);

    }
    private bool isDiveBomber(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("Ju-87")) ret = true; //only JU-87 now, but maybe more later?   HurriFB definitely won't dive-bomb
        return ret;
    }

    public bool isSeaplane(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = CoverCalcs.GetAircraftType(aircraft);
        return isSeaplane(acType);
    }
    public bool isSeaplane(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup != null && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isSeaplane(aircraft);

    }
    public bool isSeaplane(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("HE-115") || acType.Contains("Sunderland") || acType.Contains("Walrus")) ret = true; //Can land/take-off on water
        return ret;
    }

    //Armed meaning, has bombs, torpedos, or whatever else it can drop (not guns/cannons to shoot, that's different)
    public bool isBomberArmed(AiAirGroup airGroup)
    {
        if (airGroup == null) return false;
        if (airGroup.hasBombs() || airGroup.hasTorpedos()) return true;
        return false;
    }
	
	    //Armed meaning, has bombs, torpedos, or whatever else it can drop (not guns/cannons to shoot, that's different)

    public bool cannonsEmpty(AiAirGroup airGroup)
    {
        if (airGroup == null) return false;
        if (!coverACInfo.ContainsKey(airGroup)) return false;
        if (coverACInfo[airGroup].StartedWithCannons && !airGroup.hasCourseCannon()) return true;
        return false;
    }        
    public bool isStrikeAircraftWithBombs(AiAirGroup airGroup)
    {
        if (airGroup == null) return false;
		if (!Calcs.isStrikeAC(airGroup)) return false;
        if (airGroup.hasBombs() || airGroup.hasTorpedos()) return true;
        return false;
    }

    //Lands/returns to supply every cover aircraft still in the air.
    //Use when ending mission, ending battle, etc.
    public void landAllCoverAircraft()
    {


            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int numret = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                EscortMakeLand(airGroup, null);
                numret++;
            }
            if (GamePlay != null) GamePlay.gpLogServer(null, numret.ToString() + " groups of escort aircraft/bombers have been instructed to land at the nearest friendly airport and returned to General Supply.", new object[] { });
        }

    Dictionary<Player, int> TimeOfPlayerLastLandRequest = new Dictionary<Player, int>();

    public void landCoverAircraft(Player player)
    {
        landCoverAircraft(player, "");
    }

    //<cland or Tab-4 menu will land ALL aircraft, or ie <cland 2 4 5will land 2nd, 4th, 5th listed airgroup
    //Note that <cland # - the # ranges from 1-4, it's 1-based not 0-based.  Below -1 means land all groups.
    //if call fromRepair we chang ethe player msgs somewhat
    public void landCoverAircraft(Player player, string msg = "", bool fromRepair = false)
    {
        try
        {
            int currTime_sec = CoverCalcs.TimeSince2016_sec();

            //parse message to see if anything requested besides <cland [all]
            //Console.WriteLine("LCA #1");
            string newmsg = msg.Replace(",", " ").Replace("(", " ").Replace(")", " ").Replace("[", " ").Replace("]", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Trim(); // remove the comma, parentheses etc
            string[] sections = newmsg.Split(' ');
            //Console.WriteLine("LCA #2");
            //GamePlay.gpLogServer(new Player[] { player }, "Cover: Call " + sections.Count().ToString(), new object[] { });

            HashSet<int> unitsToLand = new HashSet<int>(); //null means, land all of them
            //Console.WriteLine("LCA #3");

            if (sections is object && sections.Count() > 1)
            {
                //Console.WriteLine("LCA #3a");
                int count = 0;
                string unitToLand_str = "";
                int unitToLand = -1;
                //Console.WriteLine("LCA #3b");
                for (int i = 1; i < sections.Count(); i++)
                {
                    unitToLand_str = sections[i];
                    //Console.WriteLine("LCA #3c");
                    try
                    {
                        unitToLand = Convert.ToInt32(sections[i]);
                        
                    }
                    catch { unitToLand = -1; }
                    //Console.WriteLine("LCA #3d");
                    if (unitToLand != -1)
                    {
                        unitsToLand.Add(unitToLand);
                        count++;
                    }
                    //Console.WriteLine("LCA #4");
                }
                if (!fromRepair && unitsToLand is object && unitsToLand.Count > 0) GamePlay.gpLogServer(new Player[] { player }, "Command received to land Cover aircraft groups #{0}.", new object[] { String.Join(", #", unitsToLand) });
                //Console.WriteLine("LCA #5");
            }
            //System.Console.WriteLine("cover, msg :" + msg);

            //must request it 2X within 30 seconds, to prevent accidental Tab-4-4-9 cover a/c release
            //This is not required if typing <cover, though

            if (msg.Contains("<cland") || (TimeOfPlayerLastLandRequest.ContainsKey(player) && currTime_sec - TimeOfPlayerLastLandRequest[player] < 30) || fromRepair)
            {
                if (player == null) return;
                if (TimeOfPlayerLastLandRequest.ContainsKey(player)) TimeOfPlayerLastLandRequest.Remove(player);              
                AiAircraft aircraft = null;
                if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;
                //Console.WriteLine("LCA #6");
                if (coverAircraftAirGroupsActive == null || coverAircraftAirGroupsActive.Count == 0)
                {
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Cover Land Aircraft: You don't have any cover aircraft in the air.", new object[] { });
                    return;
                }
                List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
                int numret = 0;
                int count = 0;
                foreach (AiAirGroup airGroup in saveCAAGA)
                {
                    if (airGroup == null || !coverAircraftAirGroupsActive.ContainsKey(airGroup) || coverAircraftAirGroupsActive[airGroup] != player || airGroup.GetItems().Length == 0) continue;
                    count++;
                    //Console.WriteLine("LCA #7");
                    if (unitsToLand is object && unitsToLand.Count>0 && !unitsToLand.Contains(count)) continue;  //allow to instruct just one particular group to land

                    if (aircraft == null) { EscortMakeLand(airGroup, null, fromRepair: fromRepair); numret++; }
                    else { EscortMakeLand(airGroup, aircraft.AirGroup(), fromRepair: fromRepair); numret++; }

                    //Console.WriteLine("LCA #8");
                    //if (unitToLand > 0 && count == unitToLand) break;  //once we have deleted that one particular group, exit so as to prevent accidentally deleting others

                }

                if (count == 0 ) 
                    {
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Cover Land Aircraft: You don't have any cover aircraft in the air.", new object[] { });
                        return;
                    }

                if (!fromRepair && numret > 0) GamePlay.gpLogServer(new Player[] { player }, numret.ToString() + " groups of escort aircraft/bombers have been instructed to land at the nearest friendly airport.", new object[] { });
                //Console.WriteLine("LCA #9");
                if (!fromRepair && numret > 0) GamePlay.gpLogServer(new Player[] { player }, "If you have released the escort aircraft & bombers over friendly land, they will immediately be available for use in your personal Cover Squadron again and returned to General Stock. Otherwise, you'll wait until aircraft actually return to base.", new object[] { });
            }
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, "<<<CONFIRMATION REQUIRED>>> Request Cover Aircraft release again within 30 seconds to release your aircraft.", new object[] { });
                TimeOfPlayerLastLandRequest[player] = currTime_sec;

            }
            //Console.WriteLine("LCA #10");
        }
        catch (Exception ex) { Console.WriteLine("COVER: landCoverAircraft (final) ERROR! " + ex.ToString()); }
    }

    /*************************************************************
    //COVER AIRGROUP ORDERS - <cnormal, <cstrict, <cescort, <cattack, <creserve, <cloiter
    //
    //The player can give standing orders to the individual cover/bomber airgroups that are
    //flying with them.  Each order can be given for all of the player's airgroups (no #'s at
    //all) or just for the ones they name, ie "<cstrict 1 4".
    //
    //  <cnormal / <cn   - the usual behavior: bombers/sturmoviks stay in formation except when
    //                     ground attacking, cover stays in place unless directly attacking/defending
    //  <cstrict / <cst  - ignore all else & just fly in rigid formation with the player: holds the leader's
    //                     speed & altitude, ignores <cdist, & fires only in self defence
    //  <cescort / <ce   - CLoD's ESCORT behavior for all a/c types: stay with & defend the player
    //  <cattack / <ca   - air attack anything reasonably nearby, plus any bombing the player has ordered
    //  <creserve / <cr  - hold fire & stay in reserve, joined with the player
    //  <cloiter / <clo  - stay in one place, circling
    ***************************************************************/

    //Do these orders mean the airgroup should hold its fire (no ground bombing & no air attacks)?
    public bool ordersHoldFire(CoverAGOrders orders)
    {
        return orders == CoverAGOrders.reserve || orders == CoverAGOrders.strict || orders == CoverAGOrders.loiter || orders == CoverAGOrders.drop;
    }

    //Do these orders mean the airgroup should engage nearby enemy aircraft?
    //<cdrop holds fire in the air too - a bombing run formation shouldn't scatter chasing fighters.
    public bool ordersEngageAir(CoverAGOrders orders)
    {
        return orders == CoverAGOrders.attack || orders == CoverAGOrders.normal || orders == CoverAGOrders.escort;
    }

    //Do these orders mean the airgroup should still run ground/naval bombing attacks on the target
    //point the player has given it (Knickebein point, bomb drop point, flare point, etc)?
    //<cdrop returns FALSE - that is what makes them sit on their bombs and wait for the leader instead
    //of running their own bombing pass off the player's aim point.  The release is driven separately,
    //by dropBombsNow_airGroup() once we detect the leader actually dropping.
    public bool ordersBombGround(CoverAGOrders orders)
    {
        return orders == CoverAGOrders.attack || orders == CoverAGOrders.normal;
    }

    //Shared logic for all of the player's airgroup order commands (<cnormal, <cstrict, <cescort,
    //<cattack, <creserve, <cloiter, <cdrop).  Parses any airgroup #'s out of the command - no #'s at
    //all means ALL of the player's cover airgroups - & applies the given order to each of them.
    //Returns the list of airgroups the order was applied to.
    //Note that the command word(s) themselves need not be stripped out of the message, because
    //anything that isn't a # is simply ignored when the airgroup #'s are parsed out.
    //skipHoldFire is only used entering <cdrop: a bare all-groups <cdrop (Tab-4 menu included)
    //must not overrule a <cstrict/<creserve/<cloiter hold-fire order, so those groups are skipped;
    //naming them explicitly ("<cdrop 2") still pulls them in.
    public List<AiAirGroup> setCoverAircraftAirGroupsOrders(Player player, string msg, CoverAGOrders order, string orderDescription, bool skipHoldFire = false)
    {
        List<AiAirGroup> ret = new List<AiAirGroup>();
        try
        {
            if (player == null || GamePlay == null) return ret;

            string newmsg = msg.Replace(",", " ").Replace("(", " ").Replace(")", " ").Replace("[", " ").Replace("]", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Trim(); // remove the comma, parentheses etc

            var indxs = new List<int>();
            foreach (string word in newmsg.Split(' '))
            {
                int indx = -1;
                try { if (word.Length > 0) indx = Convert.ToInt32(word); }
                catch (Exception ex) { }
                if (indx != -1) indxs.Add(indx);
            }

            int count = 0;
            string foundIndxs = "";
            int numFoundIndxs = 0;
            //Set when a <cnormal RE-JOINS groups to drop mode (see below).  A bare all-groups <cnormal
            //is both "release everyone from hold-fire" AND "the Tab-4 label is still Drop When I Drop",
            //so the clear-aim-mode block further down must NOT fire - that would undo the re-join it
            //just performed and throw the snapshot away.
            bool rejoinedDropMode = false;

            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys); //copy the keys, so the list can safely change while we set orders
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null) continue;
                if (!coverAircraftAirGroupsActive.ContainsKey(airGroup)) continue;
                if (coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (airGroup.GetItems().Length == 0) continue;
                AiAircraft aircraft1 = airGroup.GetItems()[0] as AiAircraft;
                if (aircraft1 == null) continue;
                count++;
                if (indxs.Contains(count) || indxs.Count == 0)
                {
                    //<cdrop entering with a bare all-groups order skips hold-fire groups (<cstrict,
                    //<creserve, <cloiter): they were told to hold fire, and a blanket drop must not
                    //overrule that.  Explicit "<cdrop 2" still pulls them in (skipHoldFire is false).
                    //Groups already on .drop are never skipped (they ARE the drop).
                    if (skipHoldFire && order == CoverAGOrders.drop && coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders[airGroup] != CoverAGOrders.drop && ordersHoldFire(coverAircraftAirGroupsOrders[airGroup])) continue;
                    coverAircraftAirGroupsOrders[airGroup] = order;
                    ret.Add(airGroup);
                    foundIndxs += count.ToString() + " ";
                    numFoundIndxs++;
                    //<cnormal releasing a hold-fire group re-joins it to DROP WHEN I DROP, but ONLY under
                    //an all-groups drop scope (bare <cdrop or the Tab-4 menu, which can only express
                    //all-groups) and ONLY when the <cnormal itself was all-groups.  Selective "<cdrop 3 6"
                    //scope, or a selective "<cnormal 2", means "fly normal, stay out of the drop".
                    //The SNAPSHOT IS LEFT ALONE on purpose: it holds the order to restore when drop mode
                    //ends, and the honest answer for a re-joined group is still its pre-drop order.  Writing
                    //.drop into it would make BAM_leaveDropMode restore .drop - i.e. the group would never
                    //bomb for the rest of the mission.
                    if (order == CoverAGOrders.normal && indxs.Count == 0 && BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Drop_When_I_Drop && coverOrdersBeforeDrop.ContainsKey(player) && coverOrdersBeforeDrop[player].Item2)
                    {
                        coverAircraftAirGroupsOrders[airGroup] = CoverAGOrders.drop;
                        rejoinedDropMode = true;
                    }
                }
            }

            GamePlay.gpLogServer(new Player[] { player }, numFoundIndxs.ToString() + " groups of cover aircraft " + orderDescription + " (#{0})", new object[] { String.Join(" #", foundIndxs.Trim()) });

            //<cdrop - if the player has just overridden EVERYONE out of DROP WHEN I DROP (a bare
            //<cattack, <cstrict, <cescort, <cloiter - i.e. orders that cannot mean "join the drop"),
            //then drop mode is no longer in force and the Tab-4-4-4-4-6 "Cover Targeting [..]" label
            //would otherwise still read "Drop When I Drop" - so clear it.  (<cnormal is NOT in that
            //list: it has just RE-JOINED everyone, hence the rejoinedDropMode guard below.)
            //Deliberately NOT done for a PARTIAL command: "<creserve 3" only moves squadron 3, the rest
            //are still on .drop, and that is exactly how a player holds squadrons back during a drop run.
            //BAM_enterDropMode() passes order == drop, so this can never fight with entering drop mode.
            //And rejoinedDropMode guards the case where "<cnormal" just RE-JOINED groups to the drop:
            //then the condition below would otherwise be true again (order != drop, no #s, everyone
            //found) and would switch off a drop that is very much still in force.
            if (order != CoverAGOrders.drop && indxs.Count == 0 && numFoundIndxs > 0 && !rejoinedDropMode && BAM_getplayerBombAimMode_enum(player) == BAM_BombAimMode.Drop_When_I_Drop)
            {
                BAM_playerAimMode[player] = BAM_BombAimMode.None;
                coverOrdersBeforeDrop.Remove(player);   //they have overridden us; there is nothing left to restore
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover setCoverAircraftAirGroupsOrders ERROR: " + ex.ToString()); }
        return ret;
    }

    //<cloiter - remember the point each loitering airgroup circles around.  The circling itself is
    //done by keepAircraftLoitering(), which is called from keepAircraftOnTask_recurs().
    public Dictionary<AiAirGroup, Point3d> coverAircraftAirGroupsLoiterPoint = new Dictionary<AiAirGroup, Point3d>();

    //Set (or re-set) the point the given airgroups will circle around, to where those a/c are right now.
    //Called when the <cloiter (<cl) command is given.
    //Will normally circle PLAYER'S current point, but current loc of group
    //as backup
    public void setLoiterPoints(List<AiAirGroup> airGroups, Point3d? loiterPoint = null)
    {
        try
        {
            if (airGroups == null) return;
            foreach (AiAirGroup airGroup in airGroups)
            {
                if (airGroup == null) continue;
                Point3d pos = airGroup.Pos();
                if (loiterPoint.HasValue) pos = loiterPoint.Value;
                pos.z = CoverCalcs.checkMinAGL(pos.z, pos);
                coverAircraftAirGroupsLoiterPoint[airGroup] = pos;
                Console.WriteLine("Cover: <cloiter - airgroup {0} will loiter around {1:n0} {2:n0}", airGroup.Name(), pos.x, pos.y);
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover setLoiterPoints ERROR: " + ex.ToString()); }
    }

    //<cloiter - keep the airgroup circling around its loiter point (see setLoiterPoints, above).
    //A flight plan of 4 points on a circle is laid out, starting from wherever the a/c are now and
    //turning whichever way the a/c are already turning, so it looks natural & they don't get lost.
    public void keepAircraftLoitering(Player player, AiAirGroup airGroup)
    {
        try
        {
            if (airGroup == null || airGroup.GetItems() == null || airGroup.GetItems().Length == 0) return;

            float shiftFactor = getShiftFactor(player);
            double radius_m = 900 * shiftFactor;   //<cdist adjusts how wide the circle is
            if (radius_m < 1200) radius_m = 1200;
            if (radius_m > 3000) radius_m = 3000;

            Point3d center = airGroup.Pos();
            if (coverAircraftAirGroupsLoiterPoint.ContainsKey(airGroup)) center = coverAircraftAirGroupsLoiterPoint[airGroup];

            //If the a/c have wandered well away from where they are supposed to be circling - or if there
            //is no loiter point for them yet - then circle wherever they are now instead
            //if (!coverAircraftAirGroupsLoiterPoint.ContainsKey(airGroup) || CoverCalcs.CalculatePointDistance(airGroup.Pos(), center) > radius_m * 3)
            if (!coverAircraftAirGroupsLoiterPoint.ContainsKey(airGroup))
            {
                center = airGroup.Pos();
                center.z = CoverCalcs.checkMinAGL(center.z, center);
                coverAircraftAirGroupsLoiterPoint[airGroup] = center;
            }

            //Where are the a/c on the circle?  And are they already turning clockwise or counter-clockwise?
            //(That is the cross product of the radius vector with the aircraft's velocity vector.)
            double ang_rad = Math.Atan2(airGroup.Pos().y - center.y, airGroup.Pos().x - center.x);
            Vector3d vwld = airGroup.Vwld();
            double cross = (airGroup.Pos().x - center.x) * vwld.y - (airGroup.Pos().y - center.y) * vwld.x;
            double dir = (cross >= 0) ? 1 : -1;  //+1 = counter-clockwise, -1 = clockwise

            double vel_mps = CoverCalcs.CalculatePointDistance(vwld);
            if (vel_mps < 55) vel_mps = 55;
            if (vel_mps > 160) vel_mps = 160;
            double z_m = CoverCalcs.checkMinAGL(airGroup.Pos().z, airGroup.Pos());

            List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
            for (int leg = 1; leg <= 30; leg++) //5 points, 72 degrees apart, all the way around the circle and go around 6 full  time just to be sure we dont' run out of points  (planes turn to RTB Mode if they run out of points, then they are useless from then on)
            {
                double leg_ang_rad = ang_rad + dir * leg * 2 * Math.PI / 5;
                Point3d legPos = new Point3d(center.x + Math.Cos(leg_ang_rad) * radius_m, center.y + Math.Sin(leg_ang_rad) * radius_m, z_m);
                AiAirWayPoint legWP = new AiAirWayPoint(ref legPos, vel_mps);
                (legWP as AiAirWayPoint).Action = AiAirWayPointType.NORMFLY;
                NewWaypoints.Add(legWP);
            }
            airGroup.SetWay(NewWaypoints.ToArray());
            airGroup.setTask(AiAirGroupTask.FLY_WAYPOINT, null); //otherwise the a/c may just ignore the flight plan & keep doing whatever they were doing before

            if (mainmission.ON_TESTSERVER) Console.WriteLine("Cover: <cloiter - {0} circling {1:n0} {2:n0} at radius {3:n0}m", airGroup.Name(), center.x, center.y, radius_m);
        }
        catch (Exception ex) { Console.WriteLine("Cover keepAircraftLoitering ERROR: " + ex.ToString()); }
    }

    Dictionary<Player, DateTime> lastCheckoutTime_dt = new Dictionary<Player, DateTime>();

    public void checkoutCoverAircraft(Player player, string selectString)
    {
        checkoutCoverAircraft(player, selectString, recurs_call: 0);
    }

    public int minFrontDistance_km = 2; //was 15km, to prevent ppl from just sitting @ the front line & directing cover bombers.  But now trying 2 since they also have to be flying (different from previous where they could just sit at an airport close to the front lines & direct things).
    

    public void checkoutCoverAircraft(Player player, string selectString, int recurs_call = 0)
    {
        //try
        {
            if (player == null) return;

            //If requests are coming in too fast to handle, bounce them out in time a while
            DateTime currTime = DateTime.UtcNow;
            if (lastCheckoutTime_dt.ContainsKey(player) && currTime.Subtract(lastCheckoutTime_dt[player]).TotalSeconds < 3 )
            {
                Console.WriteLine("checkoutCoverAircraft: bouncing a request for player {0} because it came in too fast, for the {1} time", player.Name(), recurs_call + 1);
                double time_needed_s = lastCheckoutTime_dt[player].Subtract(currTime).TotalSeconds + 2;
                if (time_needed_s < 3) time_needed_s = 3;
                if (time_needed_s > 30 || recurs_call > 125)
                {
                    GamePlay.gpLogServer(new Player[] { player }, ">>>>Cover ERROR: Too many requests at once.  Ignoring this request: " + selectString, new object[] { });
                    return;
                }
                Timeout(time_needed_s, () => { checkoutCoverAircraft(player, selectString, recurs_call: recurs_call + 1); });
                return;
            }
            if (!lastCheckoutTime_dt.ContainsKey(player)) lastCheckoutTime_dt[player] = currTime;
            lastCheckoutTime_dt[player] = lastCheckoutTime_dt[player].AddSeconds(1.5); //the time it takes to load the plane's section file, plus a bit
            //GamePlay.gpLogServer(new Player[] { player }, "Cover: Call " + selectString, new object[] { });
            //int parseL = Calcs.LastIndexOfAny(selectString, new string[] { " " });
            /* List<string> sections = new List<string>();
            if (selectString.Length > 0 && parseL > -1)
            {

                while (parseL > -1)
                {
                    sections.Add(selectString.Substring(parseL));
                    selectString = selectString.Substring(0, parseL);
                    parseL = Calcs.LastIndexOfAny(selectString, new string[] { " " });
                }
            }
            sections.Add(selectString);

            string ss = "(nothing)";
            foreach (var s in sections) ss += s + " ";
            */

            AiAircraft aircraft = null;
            if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;
            AiActor actor = aircraft as AiActor;

            CoverACInfo acInfo = new CoverACInfo();

            int numInArmy = CoverCalcs.numPlayersInArmy(player.Army(), this);

            string[] sections = selectString.Split(' ');

            //GamePlay.gpLogServer(new Player[] { player }, "Cover: Call " + sections.Count().ToString(), new object[] { });

            string aircraftName = "";
            if (sections.Count() > 0) aircraftName = sections[0];

            for (int i = 0; i < sections.Count(); i++) { sections[i] = sections[i].Trim(); }

            string formation = "VIC3";
            //if (sections.Count() > 2) formation = sections[2];
            //formation = formation.ToUpper();


            if (sections.Length > 0) for (int i = 1; i < sections.Length; i++)
                {
                    string fms = sections[i].ToUpper();
                    if (flightFormationAbbreviations.Keys.Contains(fms)) formation = flightFormationAbbreviations[fms];
                    else
                    {
                        var formations = numFlightFormation.Where(kvp => kvp.Key.Contains(fms)).Select(kvp => kvp.Key); //selects a key if the first characters are entered

                        if (formations.Count() > 0) formation = formations.ElementAt(0);
                    }

                }

            //formation = numFlightFormation.Where(kvp => kvp.Key.Contains(formation)).Select(kvp => kvp.Key); //selects a key if the first characters are entered
            /*
            if (flightFormationAbbreviations.Keys.Contains(formation)) formation = flightFormationAbbreviations[formation];
            else
            {
                var formations = numFlightFormation.Where(kvp => kvp.Key.Contains(formation)).Select(kvp => kvp.Key); //selects a key if the first characters are entered

                if (formations.Count() > 0) formation = formations.ElementAt(0);
            }
            */

            var bomb_fight_abbr = new List<string> { "fi", "fighter", "bo", "bomber", "he", "heavy" };

            if (!numFlightFormation.ContainsKey(formation) && !bomb_fight_abbr.Contains(formation.ToLower()))
            {
                GamePlay.gpLogServer(new Player[] { player }, "Cover ERROR: Flight formation \"" + formation + "\" does not exist! Using VIC3 instead.", new object[] { });
                formation = "VIC3";
            }
            if (formation == "VIC" && player.Army() == 1)
            {
                GamePlay.gpLogServer(new Player[] { player }, "Cover ERROR: Flight formation \"" + formation + "\" does not work for Red aircraft! Using VIC3 instead.", new object[] { });
                formation = "VIC3";
            }

            acInfo.Formation = formation;

            string acName = aircraftName.Trim();

            Point3d loc = new Point3d(0, 0, 0);
            if (aircraft != null) loc = actor.Pos();
            loc.z += 75 + Calcs.LandElevation_m(loc);  //adjust for elevvation; //starting low, like took off from airport, but we have to make sure it is ABOVE THE ACTUAL GROUND LEVEL or else trouble.  So making it 350 meters higher than the pilot who called it in.
            string escortedGroup = aircraft.AirGroup().Name();

            /*
            //TODO: Check with aircraft supply & only allow escorts with plenty of supply left
            //"SpitfireMkIa_100oct"
            string[] redplanes = { "HurricaneMkI_100oct", "HurricaneMkI_100oct", "HurricaneMkI", "HurricaneMkI_100oct-NF", "HurricaneMkI_100oct-NF", "HurricaneMkI_dH5-20", "SpitfireMkI" };
            string[] blueplanes = { "Bf-109E-3", "Bf-109E-3", "Bf-109E-3", "Bf-110C-4", "G50" };
            string[] planes = redplanes;
            if (player.Army() == 2) planes = blueplanes;
            string plane = Calcs.randSTR(planes);
            */

            string plane = selectCoverPlane(acName, (ArmiesE)player.Army(), player);
            string plainPlaneName = CoverCalcs.ParseTypeNameToPlainType(plane);

            acInfo.PlaneType = plainPlaneName;

            int numGroupsRequested = 1;
            string ngrString = "";


            string fighterbomber = "";
            if (sections.Length > 0) for (int i = 1; i < sections.Length; i++)
                {
                    if (sections[i].ToLower() == "fi" || sections[i].ToLower() == "fighter") fighterbomber = "f";
                    if (sections[i].ToLower() == "bo" || sections[i].ToLower() == "bomber") fighterbomber = "b";
                    if (sections[i].ToLower() == "he" || sections[i].ToLower() == "heavy") fighterbomber = "h";
                    if (sections[i].Length >= 2) Console.WriteLine("NGR {0} : {1}", sections[i], sections[i].ToLower()[1]);
                    if (sections[i].Length >= 2 && sections[i].ToLower()[0] == 'x')
                    {
                        int ngr = 1;
                        Console.WriteLine("NGR {0}", sections[i]);
                        string s = sections[i].Substring(1).Trim();
                        try { if (s.Length > 0) ngr = Convert.ToInt32(s); }
                        catch (Exception ex) { ngr = 1; }
                        if (ngr > 10) ngr = 10; //little sanity checks
                        if (ngr < 1) ngr = 1;
                        numGroupsRequested = ngr;
                        ngrString = string.Format(" (X{0})", numGroupsRequested);
                        Console.WriteLine("NGR {0} | {1}", sections[i], ngrString);

                    }

                }
            

			//StrikeAC must take bombs themselves & their <cover ac just take bombs also
			if (Calcs.isStrikeAC(player) && fighterbomber == "f") fighterbomber = "b";

            //By contrast, fighter wingmen CAN'T take bombs
            if (isFighterAllowedCover_wing(player) && fighterbomber == "b") fighterbomber = "f";
			
            string fbVersion = "";
            if (fighterbomber != "")
            {
                if (fighterbomber == "f")
                {
                    fbVersion = " (fighter version - no bombs)";
                    acInfo.HasBombs = false;
                }
                if (fighterbomber == "b")
                {
                    fbVersion = " (bomber version - bombs loaded)";
                    acInfo.HasBombs = true;
                }
                if (fighterbomber == "h")
                {
                    fbVersion = " (heavy bomb version - largest bombs loaded)";
                    acInfo.HasBombs = true;
                    acInfo.Heavy = true;
                    if (plainPlaneName.ToLower().Contains("wellington"))
                    {
                        acInfo.MinAttackAlt_m = 460 + 100; //Wellington heavy (2000lb) bombs are not armed unless dropped from a minimum of about 460meters/1500ft.  460m is min safe drop alt and + 100 is because there is 20*formationlocation shift up or down when on final bomb run
                        Console.WriteLine("COVER: Wellington, setting min attack alt to {0}", acInfo.MinAttackAlt_m);
                    }
                }
            }

            if (isOnRepairMission(player))
            {
                fighterbomber = "f"; //always omit bombs when on repair mission/cover
                acInfo.HasBombs = false;
                acInfo.Heavy = false;
            }

            int numAC = 2;
            try
            {
                if (sections.Count() > 1) numAC = Convert.ToInt32(sections[1]);
            }
            catch { numAC = 2; }

            if (isOnRepairMission(player) && numAC < 3) numAC = 3;
            if (isOnFerryMission(player) && numAC < 4) numAC = 4;

            //GamePlay.gpLogServer(new Player[] { player }, "Cover: numAC1 " + numAC.ToString(), new object[] { });

            if (numAC > 6) numAC = 6; //setting max planes called in at once to 2 (for now) to see if that helps with warping/rubberbanding problems.  Blue can only get 4 at once?  Might depend on regiment, formation etc.?

            GamePlay.gpLogServer(new Player[] { player }, "Cover: You requested " + numAC.ToString() + " " + plainPlaneName + " in " + formation + fbVersion + ngrString, new object[] { });

            int maximumCheckoutsAllowedAtOnce = checkoutsAvailableToPlayer_num(player);

            int numCheckedOut_before = numberAircraftCurrentlyCheckedOutPlayer(player);
            //if (numAC + numCheckedOut > maximumCheckoutsAllowedAtOnce_BomberPilots) numAC = maximumCheckoutsAllowedAtOnce_BomberPilots - numCheckedOut;

            int acRemaining_wholemission_before = acAvailableToPlayer_num(player);

            if (numCheckedOut_before == 0) BAM_resetBombAimMode(player);
            int numCheckedOut_now = 0;

            double interval_s = 16.324;
            if (numGroupsRequested > 0) interval_s = interval_s / numGroupsRequested;


            if (numGroupsRequested > 1) lastCheckoutTime_dt[player] = lastCheckoutTime_dt[player].AddSeconds(interval_s * (numGroupsRequested - 1)/ numGroupsRequested); //The time it will take to load this whole list of x5 or whatever groups

            for (int spawnGroup = 0; spawnGroup < numGroupsRequested; spawnGroup++) {

                int numAC_thisspawn = numAC;
                if (numAC_thisspawn + numCheckedOut_before + numCheckedOut_now > maximumCheckoutsAllowedAtOnce) numAC_thisspawn = maximumCheckoutsAllowedAtOnce - numCheckedOut_before - numCheckedOut_now;
                if (numAC_thisspawn < 0) numAC_thisspawn = 0;
                if (acRemaining_wholemission_before - numCheckedOut_now - numAC_thisspawn < 0) numAC_thisspawn = acRemaining_wholemission_before - numCheckedOut_now;
                if (numAC_thisspawn < 0) numAC_thisspawn = 0;

                numCheckedOut_now += numAC_thisspawn;

                //GamePlay.gpLogServer(new Player[] { player }, "Cover: numAC_thisspawn2 " + numAC_thisspawn.ToString(), new object[] { });
                /*
                if (numAC_thisspawn <= 0)
                {
                    GamePlay.gpLogServer(new Player[] { player }, ">>>You have exhausted your available Cover aircraft for now. You can check out {0} aircraft at once and currently already have {1} in the air. You have {2} left of the number you can check out the entire mission.", new object[] { maximumCheckoutsAllowedAtOnce, numCheckedOut_before + numCheckedOut_now, acRemaining_wholemission_before - numCheckedOut_now });
                    return;
                }
                */
                

                // if (acRemaining_wholemission_before == 1 & !isOnRepairMission(player)) acRemaining_wholemission_before = 2; //Always allow a final group of 2, even if only 1 remaining a/c
                
                if (spawnGroup == 0 && numInArmy > maxPlayersToAllowCover && !isOnRepairMission(player)) { GamePlay.gpLogServer(new Player[] { player }, ">>>Can't cover you - cover available only when {0} or fewer players on your side. Please ask your fellow pilots to cover you.", new object[] { maxPlayersToAllowCover }); return; }

                if (spawnGroup == 0 && numInArmy > numPlayersToReduceCover && !isOnRepairMission(player)) { GamePlay.gpLogServer(new Player[] { player }, "Note: Fewer cover aircraft/bombers available when more than {0} players on your side.", new object[] { numPlayersToReduceCover }); }

                if (spawnGroup == 0 && aircraft == null) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - you're not in an aircraft!", new object[] { }); return; }

                //hurri FB registers as a heavy bomber so that you can lead formations of them.  But you can't lead
                //a formation as a hurriFB pilot, only heavy bombers can lead.  They could bring Hurri FB's with them, though.
                //if ((!isHeavyBomber(aircraft) && !isDiveBomber(aircraft) ) || coverCalcs.GetAircraftType(aircraft).Contains("Hurricane")) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - cover provided for heavy bombers and dive bombers only!", new object[] { }); return; }

                //if (spawnGroup == 0 && (!isBomberAllowedCover(aircraft) && !(isFighterAllowedCover(aircraft) && !Calcs.isStrikeAC(aircraft)) && !(Calcs.isStrikeAC(aircraft) && Calcs.playerHasBombs(player)) && !isOnRepairMission(player))) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you! Cover provided for heavy bombers, dive bombers, fighter-bombers WITH BOMBS, and certain fighters (Hurricane, Beaufighter, U.S. Planes, Bf110, G50, Macchis), for bombing raids - and for repair/restock missions", new object[] { }); return; }
				
				if (spawnGroup == 0 && !isBomberAllowedCover(aircraft) && !isFighterAllowedCover(aircraft) &&!isFighterAllowedCover_wing(aircraft) && !Calcs.isStrikeAC(aircraft) && !isOnRepairMission(player)) { 
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Can't cover you! Cover provided for heavy bombers, dive bombers, fighter-bombers, certain fighters (early Hurricane, early Spitfire, early 109s, Beaufighter, U.S. Planes, Bf110, G50)", new object[] { }); return; 

                    GamePlay.gpLogServer(new Player[] { player }, ">>>for ground support/bombing raids, and certain fighters (later Hurricanes, Spitfires, 109s, U.S. planes, Macchi) as wingmen - and for repair/restock missions", new object[] { }); return; 
                }

                /* int maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_BomberPilots;
                if (isFighterAllowedCover(aircraft)) maximumCheckoutsAllowedAtOnce = maximumCheckoutsAllowedAtOnce_FighterPilots; */

                if (numAC_thisspawn <= 0 && numCheckedOut_before + numCheckedOut_now >= maximumCheckoutsAllowedAtOnce)
                {
                    string s = ">>>You already have {0} aircraft currently";
                    if (spawnGroup > 0 ) s = ">>>You now have {0} aircraft";

                    GamePlay.gpLogServer(new Player[] { player }, s + " escorting you--the maximum allowed.", new object[] { numCheckedOut_before + numCheckedOut_now });
                    GamePlay.gpLogServer(new Player[] { player }, ">>>When you release your escorts to return to base, you may be able to check out more.", new object[] { });
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Use Tab-4 menu or Chat Command <cland to make your cover aircraft land.", new object[] { });
                    return;
                }

                if (numAC_thisspawn <= 0) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - " + acAvailableToPlayer_msg(player), new object[] { }); return; }


                /*
                 * //this isn't working, need to re-do it with coverAircraftActorsCheckedOut
                int numAC_thisspawnInAir = numberAircraftCurrentlyCheckedOutFromSupply(player) - 1; //-1, making the reasonable assumption the player is  in an a/c right now
                Console.WriteLine("<cover, numberAircraftCurrentlyCheckedOutFromSupply(player) {0} ", numACInAir);
                if (numACInAir >= 8)
                {

                    GamePlay.gpLogServer(new Player[] { player }, "You currently have {0} cover or primary aircraft still in the air OR lost and never returned.", new object[] { numACInAir });
                    GamePlay.gpLogServer(new Player[] { player }, "You have a maximum of 8 aircraft available to you during the mission, including your primary aircraft and escort aircraft.", new object[] { });
                    GamePlay.gpLogServer(new Player[] { player }, "If aircraft are lost or destroyed they are no longer available; if your aircraft return to base they can refuel and rejoin you on another mission at that time.", new object[] { });
                    GamePlay.gpLogServer(new Player[] { player }, "Preserve your escorts by guiding them back to base safely. Use command <cland to instruct  fighters land, if they can.", new object[] { numCheckedOut });
                    return;
                }
                */


				double playerFrontDistance_m = GamePlay.gpFrontDistance(3 - player.Army(), actor.Pos().x, actor.Pos().y);
		
				
				
				if (spawnGroup == 0 && playerFrontDistance_m < minFrontDistance_km*1000 && !mainmission.ON_TESTSERVER)
                {
                    Timeout(0.5, () => {  GamePlay.gpLogServer(new Player[] { player }, "Sorry, you cannot bring in cover aircraft closer than " + minFrontDistance_km.ToString("N0") + " km to the front line - it is too dangerous for them.", new object[] { }); });
                    //else if (!spawnInFriendlyTerritory) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you can't call in cover at an enemy airfield.", new object[] { }); });
                    return;
                }
				
				Vector3d plVwld = new Vector3d (0,0,0);
				if (aircraft != null && aircraft.AirGroup() != null) plVwld = aircraft.AirGroup().Vwld();
                double pl_vel_mps = CoverCalcs.CalculatePointDistance(plVwld);
				int min_pl_vel_mps = 40;
				
				
				if (spawnGroup == 0 && pl_vel_mps < min_pl_vel_mps)
                {
                    Timeout(0.5, () => {  GamePlay.gpLogServer(new Player[] { player }, "Sorry, you must be flying faster than " + (min_pl_vel_mps*1.944).ToString("N0") + " knots in order to call in cover aircraft.", new object[] { }); });
                    //else if (!spawnInFriendlyTerritory) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you can't call in cover at an enemy airfield.", new object[] { }); });
                    return;
                }
								
				


                //Point3d ac1loc = (aircraft as AiActor).Pos();

                Tuple<double, Point3d, bool> dtS = Stb_distanceToNearestFriendlyAirport(aircraft as AiActor, birthplacefind: true); //<distance, airport/birthplace location, isAirSpawn> //allow birthplace to function as airport; allows cover aircraft at air spawn points; this returns ONLY friendly airports  birthplaces, so don't have to worry about anything on enemy ground.

                //AiAirport ap = Stb_nearestAirport(actor.Pos(), actor.Army());
                //if (ap != null) { loc = ap.Pos(); loc.z = 150; } //starting low, as though taking off.  Not actually taking off, though
                           


                //bool spawnInFriendlyTerritory = (player.Army() == GamePlay.gpFrontArmy(dtS.Item2.x, dtS.Item2.y)); //Don't need to do this as we are getting FRIENDLY airports & airspawns only now.  But sometime airspawns are over enemy territory, which is OK.  So we actually don't want to do this check.
				

                double distanceToSpawn_m = dtS.Item1;
                int maxSpawnDistance_m = 1800; //2024-09-30 was 2800, reducing bec. ppl have been hiding out near airports & calling in AI
                if (dtS.Item3) maxSpawnDistance_m = 7200; //in case it's an airspawn point, make the area a bit bigger
                if (spawnGroup == 0 && distanceToSpawn_m > maxSpawnDistance_m)
                {
                    Timeout(0.5, () => {  GamePlay.gpLogServer(new Player[] { player }, "Sorry, you were too far from the nearest friendly airfield to call in cover (" + distanceToSpawn_m.ToString("N0") + " meters)", new object[] { }); });
                    //else if (!spawnInFriendlyTerritory) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you can't call in cover at an enemy airfield.", new object[] { }); });
                    return;
                }
                //regiment determines which ARMY the new aircraft will be in BOB_RAF British, BOB_LW German. BoB_RA = Italian?
                //Anyway, if we use the pilot's current regiment it matches which is nice but also definitely keeps them in the same army.
                //
                //SO problem with this scheme is that SOME unusual regiments will prevent aircraft from flying in certain formations (and thus, from being loaded at all).  This includes
                //things like observer and weather squadrons.  Don't know why!  But if the pilot has chosen (or been forced to choose, via mission design) those certain
                //regiments then sectionfileload throws an error "Bad formation for ..." and no aircraft are loaded.  The player just silently receives no aircraft.

                //Quote: The letters/numbers specify the number of a/c within the flight. The ID of the air unit contains the number of flights in a "bit mask" ("name of the air unit"."bit mask with number of flights")
                string regiment = "gb01";
                //if (army == 1) regiment = "BoB_RAF_F_141Sqn_Early";
                //if (army == 2) regiment = "BoB_LW_JG77_I";
                regiment = aircraft.Regiment().name();
                //int numAC = 2;
                //if (isHeavyBomber(plane)) numAC = 12;
                

                Timeout(spawnGroup * interval_s, () =>
                {

                    if (dtS.Item2.x != -1 || dtS.Item2.y != -1 || dtS.Item2.z != -1) loc = dtS.Item2; //nearest airport location.
                    loc.x = loc.x + ran.NextDouble() * 4000 - 2000; //randomize the exact start position a bit, to avoid spawning diff. a/c in on top of each other
                    loc.y = loc.y + ran.NextDouble() * 4000 - 2000;
                    loc.z = 50 + ran.Next(100) + Calcs.LandElevation_m(loc);  //adjust for elevvation; //If ground airport, start the spawns near the ground, but not too near
                    if (dtS.Item3 && actor != null) loc.z = dtS.Item2.z + ran.Next(100) - 50; //actor.Pos().z;//In case of airspawn we spawn them in at or near the airspawn altitude, though. 

                    Vector3d vwld = aircraft.AirGroup().Vwld();

                    string newACActorName = Stb_LoadSubAircraft(loc: loc, type: plane, callsign: "26", hullNumber: "3", serialNumber: "001",
                                        regiment: regiment, fuelStr: "", weapons: "", velocity_mps: 250, fighterbomber: fighterbomber, skin_filename: "", delay_sec: "", escortedGroup: escortedGroup, numAC: numAC_thisspawn, formation: formation, player: player, vwld: vwld);  //higher initial velocity avoids crashes into ground etc right off the bat, especially if terrain is varied etc.



                    //create the cover a/c
                    Timeout(1.05, () =>
                    //Timeout(0.15, () =>
                    {
                        //AiActor newActor = GamePlay.gpActorByName(newACActorName);
                        AiActor newActor = GamePlay.gpActorByName(newACActorName);
                        //Console.WriteLine("NewActorloaded: " + newActor.Name() + " for " + player.Name());
                        AiAircraft newAircraft = newActor as AiAircraft;
                        AiAirGroup newAirgroup = newAircraft.AirGroup();
                        Console.WriteLine("NewAirgrouploaded: " + newAirgroup.Name() + " for " + player.Name() + " newACActorName: " + newACActorName);

                        if (newAirgroup != null && newAirgroup.GetItems().Length > 0)
                        {
                            int itemsmade = 0;
                            string aircrafttype = "";
                            
                            

                            foreach (AiAircraft a in newAirgroup.GetItems())
                            {
                                //Point3d ac2loc = (a as AiActor).Pos();
                            
                                int numCheckedOut_spawncount = numberAircraftCurrentlyCheckedOutPlayer(player) + itemsmade;
                                int acRemaining_wholemission_spawncount = acAvailableToPlayer_num(player);

                                if (numCheckedOut_spawncount >= maximumCheckoutsAllowedAtOnce || acRemaining_wholemission_spawncount <= 0)
                                
                                {
                                    string s = ">>>You already have {0} aircraft currently";
                                    if (spawnGroup > 0) s = ">>>You now have {0} aircraft";

                                    GamePlay.gpLogServer(new Player[] { player }, s + " escorting you--the maximum allowed at this time.", new object[] { numCheckedOut_spawncount });
                                    GamePlay.gpLogServer(new Player[] { player }, ">>>Tab-4-4-4-4-7 to check how many Cover aircraft in use and remaining.", new object[] { });
                                    
									mainmission.AircraftDestroyedList[a] = "SAFE_cover_exceededAllowed";
                                    (a as AiCart).Destroy();
                                    continue;
                                }
                                    bool supplyLimitReached = false;
                                if (supplymission != null) supplyLimitReached = supplymission.IsLimitReached(newActor);
                                if (supplyLimitReached && !isOnRepairMission(player))
                                {

                                    GamePlay.gpLogServer(new Player[] { player }, "Supply limit reached for " + CoverCalcs.ParseTypeName((a as AiCart).InternalTypeName()) + "; no aircraft available. Please try again to find an available aircraft.", new object[] { });
									mainmission.AircraftDestroyedList[a] = "SAFE_cover_exceededAllowed";
                                    (a as AiCart).Destroy();
                                    continue;
                                }
                                itemsmade++;
                                aircrafttype = CoverCalcs.ParseTypeName((a as AiCart).InternalTypeName());
                                GamePlay.gpLogServer(new Player[] { player }, "Cover assigned: " + aircrafttype + " (" + (a as AiActor).Name() + ")", new object[] { });
                                //if (supplymission != null) supplymission.SupplyOnPlaceEnter(player, (a as AiActor));
                                if (supplymission != null && !isOnRepairMission(player)) supplymission.SupplyAICheckout(player, a as AiActor); //we're saying that repair mission a/c come from a different supply, not our regular limited supply
                                coverAircraftActorsCheckedOut.Add((a as AiActor), player);

                                int nac = numberCoverAircraftActorsCheckedOutWholeMission_add(player);


                            }

                            /*
                            newAirgroup.setTask(AiAirGroupTask.DEFENDING, (player.Place() as AiAircraft).AirGroup());
                            newAirgroup.changeGoalTarget(player.Place());
                            Console.WriteLine("ChangeGoalTarget: " + newAirgroup.Name() + " to " + player.Name());
                            */
                            if (itemsmade > 0)
                            {
                                if (isOnRepairMission(player))
                                {
                                    bool success = mainmission.objectiverepairmission.orm_handleCoverPickupOrFerryRequest(player, numCoverACadded: itemsmade);
                                    Console.WriteLine("mainmission.objectiverepairmission.orm_handleCoverPickupOrFerryRequest(player, numCoverACadded: itemsmade): " + player.Name() + " - # cover/repair planes added: " + itemsmade.ToString("n0"));
                                    if (!success)
                                    {
                                        Timeout(6, () =>
                                        {
                                            GamePlay.gpLogServer(new Player[] { player }, "Because you are unable to accept new Repair Loads right now, your requested cover airgroup has been disbanded.", new object[] { });
                                            return;

                                        });

                                    }
                                }

                                string msg6 = acAvailableToPlayer_msg(player);

                                coverAircraftAirGroupsActive.Add(newAirgroup, player);
                                addToIndexes(player, newAirgroup);
                                coverAircraftAirGroupsOrders[newAirgroup] = CoverAGOrders.normal;

                                //keepAircraftOnTask_recurs(newAirgroup, AiAirGroupTask.ATTACK_AIR, AiAirWayPointType.AATTACK_FIGHTERS, player, 43.2354); //don't seem aggressive enough in defending with this, trying the .escort instead, with including the bomber group actor as .target
                                bool heavyBomber = false;
                                if (isHeavyBomber(newAirgroup) || isDiveBomber(newAirgroup)) heavyBomber = true;
                                bool isStrikeAC = Calcs.isStrikeAC(newAirgroup);

                                // 2026-09-03 - calculating this but not using it anywhere for now...
								bool playerIsStrikeACwithBombs = false;
                                if (player != null & player.Place() != null && player.Place() as AiAircraft != null)
                                    playerIsStrikeACwithBombs = (Calcs.isStrikeAC(player.Place() as AiAircraft) && Calcs.playerHasBombs(player));
								

                                acInfo.IsHeavyBomber = heavyBomber;
                                acInfo.IsDiveBomber = isDiveBomber(newAirgroup);
                                acInfo.IsStrikeAC = isStrikeAC;
                                acInfo.IsPlayerStrikeAC = Calcs.isStrikeAC(player.Place() as AiAircraft);
                                coverACInfo[newAirgroup] = acInfo;
                                acInfo.StartedWithCannons = newAirgroup.hasCourseCannon();
                                acInfo.HasCannons = newAirgroup.hasCourseCannon();

                                double delay = 11.2354 + ran.NextDouble() * 2;
                                //Console.WriteLine("1Heavybomber init: {0} {1} " + newAirgroup.Name() + " to " + player.Name(), heavyBomber, delay);
                                //if (heavyBomber) delay = 2 * delay; //don't think we really need this
                                //try
                                {
                                    keepAircraftOnTask_recurs(newAirgroup, AiAirGroupTask.DO_NOTHING, AiAirWayPointType.ESCORT, player, delay, heavyBomber, isStrikeAC, acInfo.IsPlayerStrikeAC, AltDiff_m: 666, AltDiff_range_m: 100, AltDiffBomber_m: -5, AltDiffBomber_range_m: 2, AltDiffPlayerEscort_m: -666, AltDiffPlayerEscort_range_m: 2); //_range is how much +/- random value ot add to the AltDiff altitude change.
                                                                                                                                                                                                                                                                                                                                             //Was AltDiffBomber_m: -14, AltDiffBomber_range_m: -45 - trying closer 2020/01/25
                                                                                                                                                                                                                                                                                                                                             //2018/11/16 - WAS 43 seconds, trying 21 seconds instead
                                                                                                                                                                                                                                                                                                                                             //Console.WriteLine("1recurs started");
                                }
                                //catch (Exception ex) { Console.WriteLine("Cover1.5 <cover: " + ex.ToString()); }

                                GamePlay.gpLogServer(new Player[] { player }, ">>>Your escort consists of {0} {1}s. They have just taken off from the nearest friendly airfield.", new object[] { itemsmade, aircrafttype });

                                try
                                {
                                    GamePlay.gpLogServer(new Player[] { player }, msg6, new object[] { });
                                }
                                catch (Exception ex) { Console.WriteLine("Cover2 <cover: " + ex.ToString()); }

                                if (numCheckedOut_before == 0) GamePlay.gpLogServer(new Player[] { player }, "Remember to preserve your aircraft supply by instructing your escorts to land when you land, crash, or die - use Tab-4 menu or Chat Command <cland", new object[] { });

                                if (spawnGroup == 0 && isHeavyBomber(newActor as AiAircraft) && numCheckedOut_before == 0) //show only for bombers, and only for first aircraft checked out each time (numCheckedOut is the # of aircraft checked out BEFORE the current group.
                                {

                                    Timeout(2.05, () =>

                                    {
                                        GamePlay.gpLogServer(new Player[] { player }, "Tab-4-4-4-4-6 to set Cover Targeting - Bombers can follow you or attack targets, at your command.", new object[] { });

                                    });
                                    Timeout(4.05, () =>

                                    {
                                        GamePlay.gpLogServer(new Player[] { player }, "You must stay close to your bombers or they will disengage and return to base.", new object[] { });

                                    });

                                }
                            }


                            //auto turn-on regular cover a/c for player
                            Timeout(10, () => { turnOnRegularDisplay_listPositionCurrentCoverAircraft(player); });
                        }
                        else //error, IE, no actor was returned or found, because the mission failed to load or something of the sort
                        {
                            GamePlay.gpLogServer(new Player[] { player }, "COVER: ERROR assigning your cover aircraft, " + plane + ". PLEASE TRY AGAIN. If this happens repeatedly, please notify the developers.", new object[] { });
                            return;
                        }


                        setCoverAircraftCurrentlyAvailable();


                    });


                    /*
                    string units = "km";
                    if (player.Army() == 1) units = "miles";
                    string[] words = msg.Split(' ');

                    if (words.Length >= 3)
                    {
                        double angle_deg = 0;
                        double distance = 0;
                        try { if (words[1].Length > 0) angle_deg = Convert.ToDouble(words[1]); }
                        catch (Exception ex) { }
                        try { if (words[2].Length > 0) distance = Convert.ToDouble(words[2]); }
                        catch (Exception ex) { }
                    }
                    */

                });
            }
        }
        //catch (Exception ex) { Console.WriteLine("Cover <cover: " + ex.ToString()); }

    }

        private int numberCoverAircraftActorsCheckedOutWholeMission_add(Player player)
        {
            if (numberCoverAircraftActorsCheckedOutWholeMission.ContainsKey(player)) numberCoverAircraftActorsCheckedOutWholeMission[player]++;
            else numberCoverAircraftActorsCheckedOutWholeMission[player] = 1;

            return numberCoverAircraftActorsCheckedOutWholeMission[player];
        }

    private int numberCoverAircraftActorsCheckedOutWholeMission_remove(Player player)
    {
        if (numberCoverAircraftActorsCheckedOutWholeMission.ContainsKey(player))
        {
            numberCoverAircraftActorsCheckedOutWholeMission[player]--;
            if (numberCoverAircraftActorsCheckedOutWholeMission[player] < 0) numberCoverAircraftActorsCheckedOutWholeMission[player] = 0;
        }
        else numberCoverAircraftActorsCheckedOutWholeMission[player] = 0;

        return numberCoverAircraftActorsCheckedOutWholeMission[player];
    }

    private int howMany_numberCoverAircraftActorsCheckedOutWholeMission(Player player)
    {
        if (numberCoverAircraftActorsCheckedOutWholeMission.ContainsKey(player)) return numberCoverAircraftActorsCheckedOutWholeMission[player];
        else return 0;
    }

    public Dictionary<Player, Point3d> playerCurrentTargetPoint = new Dictionary<Player, Point3d>();
    public bool hasPlayerCurrentTargetPointChanged(Player player, Point3d newTargetPoint)
    {
        bool playerTargetPointChanged = true;
        Point3d player_oldTargetPoint = new Point3d(-100000, -100000, 0);
        if (playerCurrentTargetPoint.ContainsKey(player) && Calcs.Point3dEqualXY(newTargetPoint, playerCurrentTargetPoint[player])) playerTargetPointChanged = false;
        playerCurrentTargetPoint[player] = newTargetPoint;
        return playerTargetPointChanged;

    }

    //If we kept all of each player's aircraft executing their task routine simultaneously, it might help in keeping the player's group
    //more coordinated & executing manuevers together etc.
    public void keepAircraftOnTask_recurs(AiAirGroup airGroup, AiAirGroupTask task = AiAirGroupTask.DO_NOTHING, AiAirWayPointType aawpt = AiAirWayPointType.ESCORT, Player player = null, double delay = 16.2354, bool heavyBomber = false, bool isStrikeAC = false, bool isPlayerStrikeAC = false, double AltDiff_m = 1000, double AltDiff_range_m = 100, double AltDiffBomber_m = 1000, double AltDiffBomber_range_m = 100, double AltDiffPlayerEscort_m = -666, double AltDiffPlayerEscort_range_m = 2)
    {
        try
        {
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("KAOTXX1 " + DateTime.UtcNow.ToString("T.fffffff"));
            AiAirGroup tasktarget = null;
            task = AiAirGroupTask.DO_NOTHING;

         
            //So, sometimes airgroups split up, say when under attack or landing.  If so, we just add the new group to the coverAircraftAirGroupsActive (but
            //only when the original groups was also there)
            //In this case the name of the motherGroup is split off from is in airGroup.motherGroup()
            //This little exercise ensures that we still keep control off the aircraft, and they continue to support & cover the main aircraft, even if their airGroups happen to split up.
            //Not exactly sure how often this happens or when !?
            if (airGroup.motherGroup() != null && coverAircraftAirGroupsActive.ContainsKey(airGroup.motherGroup()) && !coverAircraftAirGroupsActive.ContainsKey(airGroup))
            {
                Console.WriteLine("COVER: Airgroup has a mothergroup, and the mothergroup is one of the <cover airgroups, so we add the daughter group to that pilot's controlled <cover groups");
                //If the airGroup is a split-off, and if it hasn't already transferred the 
                //orders over from its motherGroup, we do it now
                if (!coverAircraftAirGroupsActive.ContainsKey(airGroup) && !coverAircraftAirGroupsOrders.ContainsKey(airGroup) && coverAircraftAirGroupsOrders.ContainsKey(airGroup.motherGroup())) 
                { 
                    coverAircraftAirGroupsOrders[airGroup] = coverAircraftAirGroupsOrders[airGroup.motherGroup()];
                    Console.WriteLine("COVER: Airgroup has a mothergroup, the mother group has AirGroupsOrders, and they haven't been transferred to the daughter group yet, os doing that now");
                }
                //And we also add it to the active airgroups for this player
                coverAircraftAirGroupsActive.Add(airGroup,
                coverAircraftAirGroupsActive[airGroup.motherGroup()]);
            }

            if (airGroup == null)
            {
                checkPlayerAirgroups(player);
                return;
            }

            //This is where we bid farewell to an aircraft if the player has died, left the server, disappeared, etc etc etc.
            //So we need to give it a flightplan make it land and/or fly off the map, then that's all she wrote.
            //<cdrop Step D - is this group quietly in RTB?  It would neither open its bays nor release,
            //and it looks from the outside exactly like a group ignoring us.
            cdRtbProbe(airGroup, player);

            if (!coverAircraftAirGroupsActive.ContainsKey(airGroup))
            {
                EscortMakeLand(airGroup, null);
                return;
            }

            //Maybe this is a giant CPU hog?  
            //If so, we can try to slow it down a bit
            var mmtlm = mainmission.threadloadmission;
            double actual_delay = delay;
            if (mmtlm.recentCPUPercent > 98 || mmtlm.rollingAverageCPUPercent > 95) actual_delay = 4* delay;
            else if (mmtlm.recentCPUPercent > 95 || mmtlm.rollingAverageCPUPercent > 90) actual_delay = 3 * delay;
            else if (mmtlm.recentCPUPercent > 90 || mmtlm.rollingAverageCPUPercent > 85) actual_delay = 2 * delay;
            else if (mmtlm.recentCPUPercent > 85 || mmtlm.rollingAverageCPUPercent > 80) actual_delay = 1.5 * delay;

            
            //if (!heavyBomber) actual_delay = delay * 4; //experiment, kinda worked?  but maybe 2* instead of 4*?
            Timeout(actual_delay, () => keepAircraftOnTask_recurs(airGroup, task, aawpt, player, delay, heavyBomber, isStrikeAC, isPlayerStrikeAC, AltDiff_m, AltDiff_range_m, AltDiffBomber_m, AltDiffBomber_range_m, AltDiffPlayerEscort_m, AltDiffPlayerEscort_range_m));
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("KAOTXX2 " + DateTime.UtcNow.ToString("T.fffffff"));
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("CVXX3 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping

            double AltDiffPassed_m = AltDiff_m;
            double AltDiffPassed_range_m = AltDiff_range_m;
            bool isEscortAirgroup = true; //this is like a fighter that normally flies higher than the bombers, the first created/default AltDiff 
            aawpt = AiAirWayPointType.ESCORT; //default for cover fighters
            task = AiAirGroupTask.ATTACK_AIR; //default for cover fighters

            //int numAC = airGroup.NOfAirc;
            int numAC = airGroup.GetItems().Length; //2021/07/05 - I am suspicious of this method for counting a/c and it might give ZERO as answer when it shouldn't (.NOfAC())
            if (numAC == 0)
            {
                coverAircraftAirGroupsActive.Remove(airGroup);
                forgetAirGroup(airGroup);
                //Console.WriteLine("Cover KeepAircraftOnTask: Removing airgroup {0} from active list because no more aircraft in the group", airGroup.Name());
                if (player != null) GamePlay.gpLogServer(new Player[] { player }, "Your {0} cover group has been disbanded (no aircraft left in it)", new object[] { airGroup.Name() });
                return;
                //TODO: Maybe the group splits up, maybe there are daughter groups or something?
            }
            //Console.WriteLine("Cover KeepAconTask: 3");

            //If player isn't in game any more, or too distant, or not in an aircraft then we release the cover a/c to land
            double distToLeadAircraft = 0;

            if (player != null && player.Place() != null && (player.Place() as AiActor) != null)
            {
                distToLeadAircraft = CoverCalcs.CalculatePointDistance((player.Place() as AiActor).Pos(), airGroup.Pos());
            }

            Point3d oldTargetPoint = new Point3d(-1, -1, -1);
            if (coverAircraftAirGroupsTargetPoint.ContainsKey(airGroup)) oldTargetPoint = coverAircraftAirGroupsTargetPoint[airGroup];

            //Console.WriteLine("Cover KeepAconTask: dist to lead ac {0:N0}", distToLeadAircraft);
            //NOTE FOLLOWING GIVES ERRORS IF PLAYER.PLACE DOESN"T EXIST
            //Console.WriteLine("Cover KeepAconTask: Cover thinking {0} {1} {2} {3:N0} ", player == null, player.Place() == null, (player.Place() as AiAircraft).AirGroup() == null, distToLeadAircraft);

            if (!coverAircraftAirGroupsReleased.ContainsKey(airGroup)) coverAircraftAirGroupsReleased[airGroup] = false;
            else if (coverAircraftAirGroupsReleased[airGroup])
            {
                coverAircraftAirGroupsActive.Remove(airGroup);
                forgetAirGroup(airGroup);
                EscortMakeLand(airGroup, null);
                return;
            }

        CoverAGOrders orders = CoverAGOrders.normal;
        if (coverAircraftAirGroupsOrders.Keys.Contains(airGroup)) orders = coverAircraftAirGroupsOrders[airGroup];


        //This is to let any coverAC/bombers on their bomb runs just continue it for 5 more minutes after the main a / c(live pilot) has been
        //killed or crashed.  So they will continue and maybe hit the target for several minutes, then be released.  Rather than just quitting instantly when the player dies.
        //There is another issue, where we might want bombers on their final run-in do not change/move but maybe we'll have to handle that separately somehow?
        bool coverACContinuingFinalRun = false;
            //if (heavyBomber && orders == CoverAGOrders.attack && (oldTargetPoint.x != -1 || oldTargetPoint.y != -1) && (player == null || player.Place() == null || (player.Place() as AiAircraft).AirGroup() == null)) coverACContinuingFinalRun = true;
            if (ordersBombGround(orders) && (oldTargetPoint.x != -1 || oldTargetPoint.y != -1) && (player == null || player.Place() == null || (player.Place() as AiAircraft).AirGroup() == null)) coverACContinuingFinalRun = true;

            bool aircraftChangeDisband = false;
            if (!isBomberAllowedCover(player) && !isFighterAllowedCover(player) && !isFighterAllowedCover_wing(player) && !Calcs.isStrikeAC(player) && !isOnRepairMission(player) && !coverACContinuingFinalRun)
            {
                // Could use this to allow people ot jump in a/c & defend their planes, later in the mission.  Maybe. : Tuple<int, string, string, DateTime> item = supplymission.aircraftCheckedOutInfo[actor];
                //This could be made tighter . . . right now they can still jump in a bomber, grab cover, then switch to fighter-bomber to fly them.
                if (isPlayerInPlane(player))
                {
                    string m = "****Your aircraft isn't allowed cover!****";
                    GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });
                }
                aircraftChangeDisband = true;
            }

            //Console.WriteLine("Cover KeepAconTask: 6");

            if (aircraftChangeDisband || player == null || player.Place() == null || (player.Place() as AiAircraft).AirGroup() == null || distToLeadAircraft > 42000)  //Was about 20,000, seemed to small. 50,000 seems too large.  
            {
                //Console.WriteLine("Cover KeepAconTask: Cover exiting {0} {1} {2} {3:N0} ", player == null, player.Place() == null, (player.Place() as AiAircraft).AirGroup() == null, distToLeadAircraft);

                AiAircraft leadAircraft = (player.Place() as AiAircraft);
                if (aircraftChangeDisband) leadAircraft = null;

                //EscortMake Land sets the necessary waypoints & also removes the a/c from coverAircraftAirGroupsActive
                if (leadAircraft == null)
                {

                    if (coverACContinuingFinalRun)
                    {
                        //This is to let any bombers on their bomb runs just continue it for 10 more minutes after the main a/c (live pilot) has been
                        //killed or crashed.  So they will continue and maybe hit the target, then be released.
                        //Timeout is bit of  kludge here, waiting 10 minutes this timer will be set a few times rather than just the once                    
                        coverAircraftAirGroupsReleased[airGroup] = false;
                        Timeout(10 * 60, () =>
                          {
                              coverAircraftAirGroupsTargetPoint[airGroup] = new Point3d(-1, -1, -1);
                              coverAircraftAirGroupsReleased[airGroup] = true;
                              EscortMakeLand(airGroup, null);
                              turnOffRegularDisplay_listPositionCurrentCoverAircraft(player);
                          });
                    }
                    else
                    {
                        EscortMakeLand(airGroup, null);
                        coverAircraftAirGroupsTargetPoint[airGroup] = new Point3d(-1, -1, -1);
                        coverAircraftAirGroupsReleased[airGroup] = true;
                    }

                }
                else
                {
                    EscortMakeLand(airGroup, leadAircraft.AirGroup());
                    coverAircraftAirGroupsTargetPoint[airGroup] = new Point3d(-1, -1, -1);
                    coverAircraftAirGroupsReleased[airGroup] = true;
                }

                string acType = "escort aircraft";
                if (heavyBomber) acType = "bombers";
                if (isPlayerStrikeAC && isStrikeAC) acType = mainmission.statsmission.stb_StrikeName + " aircraft";

                if (coverAircraftAirGroupsReleased.ContainsKey(airGroup) && coverAircraftAirGroupsReleased[airGroup] && player != null) GamePlay.gpLogServer(new Player[] { player }, "You are too far from your {1}. The {0} group of {1} have been instructed to land at the nearest friendly airport.", new object[] { airGroup.Name(), acType });
                if (coverAircraftAirGroupsReleased.ContainsKey(airGroup) && coverAircraftAirGroupsReleased[airGroup]) {
                    EscortMakeLand(airGroup, null);
                    return; //Don't keep flying the a/c (except to land it) except for the short time when coverACContinuingFinalRun is true
                     
                }
            }

            //Console.WriteLine("Cover KeepAconTask: 778789");        
            

            //<cdrop - we have just handed this airgroup a NORMFLY+GATTACK_POINT pair to make it pull its
            //release.  This routine runs every ~16s and would otherwise rewrite the flight plan out from
            //under the drop before it happens, so skip the whole waypoint section for a short while.
            //Same trick the ground-attack path uses by returning right after BomberUpdateWaypoints().
            //See Genghis-Class-CloDNotes.cs section 2.
            if (coverAircraftAirGroupsDropIssued.ContainsKey(airGroup))
            {
                double heldRec_s = (DateTime.UtcNow - coverAircraftAirGroupsDropIssued[airGroup]).TotalSeconds;
                if (heldRec_s < coverDropHoldFlightPlan_s)
                {
                    //DIAGNOSTIC 2026/10: the logs show the release plan being replaced by a plain
                    //3-waypoint FOLLOW plan a few seconds after issue, which this early-return is
                    //supposed to prevent.  Printing every cycle proves whether the hold-off really is
                    //firing or whether something else is rewriting the plan behind its back.
                    if (mainmission.ON_TESTSERVER)
                        Console.WriteLine("COVER <cdrop HOLD: {0} still holding its release plan, {1:F1}s of {2:F0}s left",
                            airGroup.Name(), coverDropHoldFlightPlan_s - heldRec_s, coverDropHoldFlightPlan_s);
                    return;
                }
                if (mainmission.ON_TESTSERVER) Console.WriteLine("COVER <cdrop HOLD: {0} release hold-off expired after {1:F1}s - formation control resumes", airGroup.Name(), heldRec_s);
                coverAircraftAirGroupsDropIssued.Remove(airGroup);
            }

            if (
                  heavyBomber ||
                  (isPlayerStrikeAC && isStrikeAC)
                )
            {
                //task = AiAirGroupTask.ATTACK_GROUND;
                //aawpt = AiAirWayPointType.GATTACK_POINT;
                AltDiffPassed_m = AltDiffBomber_m;
                AltDiffPassed_range_m = AltDiffBomber_range_m;
                isEscortAirgroup = false;
                aawpt = AiAirWayPointType.FOLLOW; //default for bombers
                task = AiAirGroupTask.FLY_WAYPOINT; //default for bombers
				
				//If a fighter a/c then it is escorting e.g. bombers & they stay below the player (escorting a/c)
				//But if it is a strike AC flying with other strike AC then no, it is more like flying
				//with a bomber formation @ the same altitude
				//| ((isPlayerStrikeAC && !isDiveBomber(player)) && !isStrikeAC))
                if (player != null && isFighterAllowedCover(player)  && !(isPlayerStrikeAC && isStrikeAC) && !isFighterAllowedCover_wing(player))
                {
                    AltDiffPassed_m = AltDiffPlayerEscort_m;
                    AltDiffPassed_range_m = AltDiffPlayerEscort_range_m;
                    aawpt = AiAirWayPointType.FOLLOW; //default for Sturmo flying with sturmo player
                    task =  AiAirGroupTask.FLY_WAYPOINT; //default for Sturmo flying with sturmo player

                }
                //Console.WriteLine("Cover KeepAconTask: 791919191");

                //<cdrop 2026/10 Step C5 - PRE-OPEN THE BOMB BAYS.  There is no write-parameter API on
                //AiAircraft (CloDNotes 4/9b), so the only lever we have on the 5-15s door cycle is to
                //make the engine believe an attack is imminent: give the group a GATTACK_POINT a long
                //way ahead, re-issued every cycle so they never actually reach it.  The DROPTRACE bay=
                //column shows the doors follow the attack waypoint (0 -> 1 within a few seconds).
                //When the leader drops, coverDropReleasePass() overwrites this with the real release
                //geometry - and because the bays are already open the release should be near-instant.
                //OFF by default (cdDropPreOpenBays) because it puts the group into bomb-run attitude
                //instead of tight formation - A/B it in a test session before turning it on anywhere.
                if (cdDropPreOpenBays && orders == CoverAGOrders.drop && !coverACContinuingFinalRun &&
                    isBomberArmed(airGroup) && !airGroup.hasTorpedos() &&
                    player != null && player.Place() != null && (player.Place() as AiAircraft) != null)
                {
                    AiAirGroup preOpenLeader = (player.Place() as AiAircraft).AirGroup();
                    if (preOpenLeader != null && preOpenLeader != airGroup)
                    {
                        //game objects want touching on the mission thread, not on the timer thread
                        AiAirGroup agPre = airGroup;
                        AiAirGroup ldPre = preOpenLeader;
                        Player plPre = player;
                        Timeout(0.05, () => dropPreOpenBays_airGroup(plPre, agPre, ldPre));
                        return;   //this cycle's plan IS formation+decoy - do not also write a formation plan
                    }
                }

                Point3d newTargetPoint = new Point3d(-1, -1, -1);

                if (TWCKnickebeinMission != null &&
                        BAM_isKnickebeinPoint(player)
                    )
                    newTargetPoint = TWCKnickebeinMission.KniPoint(player);

                else if (BAM_isBombPoint(player) || BAM_isMyPositionPoint(player)) newTargetPoint = PBP_getPlayerLastBombOrMyPositionPoint_point3d(player);

                bool playerTargetPointHasChanged = hasPlayerCurrentTargetPointChanged(player, newTargetPoint);

                if (coverACContinuingFinalRun) newTargetPoint = oldTargetPoint; //In case of the bombers continuing their attack after player death, they don't get a NEW point from the player, but we need to continue sending them to the same OLD point just in case they need a new actor etc etc etc near that point

                double oldToNewTargetPointDistance_m = CoverCalcs.CalculatePointDistance(oldTargetPoint, newTargetPoint);

                Console.WriteLine("Cover: KBPoint, dist: {0:F0} {1:F0} {2:F0} : {3:F0} {4:F0} {5:F0} : {6:F0}  ", new object[] { newTargetPoint.x, newTargetPoint.y, newTargetPoint.z, oldTargetPoint.x, oldTargetPoint.y, oldTargetPoint.z, oldToNewTargetPointDistance_m });
                AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
                int currWay = airGroup.GetCurrentWayPoint();


                bool bombing = false;
                if ((CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT || (CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_TARG) bombing = true;

                //Console.WriteLine("Cover KeepAconTask: 555");

                //This turns off bombing for the a/c if the player turns it off via the menu
                BAM_BombAimMode bam = BAM_getplayerBombAimMode_enum(player);
                //Drop_When_I_Drop is listed with None here on purpose.  Without this, selecting drop mode and then
                //issuing <cnormal would leave bam != None AND ordersBombGround(orders) == true, so the
                //stale newTargetPoint would survive and they would fly a GATTACK_POINT at it.  Treating
                //it exactly like None means "DROP WHEN I DROP" can never produce a ground attack: the
                //release comes from the leader's own position instead (see dropBombsNow_airGroup).
                if (bam == BAM_BombAimMode.None || bam == BAM_BombAimMode.Drop_When_I_Drop || !ordersBombGround(orders) )  //no bombing, unless the orders are attack or normal (so <creserve, <cstrict, <cescort & <cloiter all hold their bombs)
                {
                bombing = false;
                    newTargetPoint = new Point3d(-1, -1, -1);
                    if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                    if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                    if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);       
                }

                //Console.WriteLine("Cover KeepAconTask: 444");
                //If the a/c was previous targeted at a point, and the point is still the same, and we are closer then 5km to it, and haven't bombed yet, and still flying towards it, then DON'T CHANGE IT
                //This hopefully will increase the accuracy of bombers by not messing with their final run-in
                //Must coordinate this distance with the random target point distances as 
                //determined by searchRadius around line 5292
                double targetChangeDistance_m = 135;
                float shiftFactor = getShiftFactor(player);
                targetChangeDistance_m *= shiftFactor;

                Point3d targetDirection = new Point3d(oldTargetPoint.x - airGroup.Pos().x, oldTargetPoint.y - airGroup.Pos().y, oldTargetPoint.z - airGroup.Pos().z);

                bool flyingTowardsTarget = CoverCalcs.roughlySameDirection(targetDirection, airGroup.Vwld(), 15);
                
                //keeps a/c just locked on the same target the last 5k in
                if (bombing && flyingTowardsTarget || (oldTargetPoint.x != -1 || oldTargetPoint.y != -1) && !playerTargetPointHasChanged && isBomberArmed(airGroup) && CoverCalcs.CalculatePointDistance(oldTargetPoint, airGroup.Pos()) < 5000)
                {
                    return;
                }

                coverAircraftAirGroupsTargetPoint[airGroup] = newTargetPoint;

                //Console.WriteLine("Cover KeepAconTask: 333");

                //if ((newTargetPoint.x != -1 || newTargetPoint.y != -1) && isBomberArmed(airGroup))  //newTargetPoint == (-1,-1,-1) is the signal that no knickebein is set.  IN that case the a/c act just like any other escort  If a Knickebein IS set, then they go & bomb that knickebein like bombers.

                //So if BOMBAIAMMODE == NONE OR there is no bomb target point yet, then the bombers act like escorts & fly that way
                //If BOMBAIAMMODE is set to something AND we have a target point to work with (either Knickebein OR the bomb drop point)
                //Then the bombers attack that point or enemy.
                //
                //newTargetPoint == (-1,-1,-1) is the signal that no bomb target is set yet, either via Knickebein OR dropping bomb OR whatever.  IN that case the a/c act use the regular Escort routine with a few mods.

                //Console.WriteLine("Cover KeepAconTask: 666");

                if (newTargetPoint.x != -1 || (newTargetPoint.y != -1) && (bam != BAM_BombAimMode.None))

                {
                    //We're passing these values now as AltDiffBomber_m etc.
                    //Let's try just letting them trail behind & slightly below just as they
                    //Do when following the lead
                    // AltDiffPassed_m = -14;
                    //AltDiffPassed_range_m = 25;
                    Console.WriteLine("2ChangeGoalTarget: {0} ({1},{2}) {3}" + airGroup.Name() + " to " + player.Name(), task, Math.Round(newTargetPoint.x), Math.Round(newTargetPoint.y), bam);

                    //avoid dreaded object error
                    AiAirGroup playerAirgroup = null;
                    if (player != null && player.Place() != null) playerAirgroup = (player.Place() as AiAircraft).AirGroup();

                    bool attacking = BomberUpdateWaypoints(player, airGroup, playerAirgroup, newTargetPoint, AiAirWayPointType.FOLLOW, AiAirWayPointType.GATTACK_POINT, AiAirWayPointType.FOLLOW, altDiff_m: AltDiffBomber_m, AltDiff_range_m: AltDiffBomber_range_m, nodupe: true, orders: orders);
                    //airGroup.setTask(AiAirGroupTask.ATTACK_GROUND, null);
                    //task = AiAirGroupTask.ATTACK_GROUND;
                    //tasktarget = null;

                    //BomberUpdateWaypoints(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.GATTACK_GROUND, double altDiff_m = 20,
                    //double AltDiff_range_m = 50, bool nodupe = true)
                    if (attacking) return; //if attacking is false that means for example no GROUND ENEMY was found, so it is not attacking anything, so we should keep it following or whatever else is normal
                }
                //if (mainmission.ON_TESTSERVER) Console.WriteLine("KAOTXX3 " + DateTime.UtcNow.ToString("T.fffffff"));
                if (coverACContinuingFinalRun) return; //we never let bombers continuing final run move on to the next part where they escort or fly with the player, since the player DOESN'T EXIST ANY MORE!
            } else
            {
                //clear the targets if the ag is not targeting anything 
                if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);
            }

            //Console.WriteLine("Cover KeepAconTask: 8");
            if (player == null || player.Place() == null || (player.Place() as AiAircraft).AirGroup() == null) return; //All the below has to do with following, escorting the player.  So if no player or player has crashed etc no point in it.

            AiAirGroup playerAirGroup = (player.Place() as AiAircraft).AirGroup();

            //Bombers will somewhat act as escorts and attack things, but not to the degree fighters will (which .ESCORT makes them do)
			//
			// Also, .ESCORT makes planes IGNORE THEIR GIVEN WAYPOINTS and just follow the escorted
			//aircraft wherever it goes (presumably unless engaged with enemies) and generally follow
			//pre-programmed ESCORT logic and not just fly in formation as usually done for e.g. bombers
			//By contrast .COVER will defend the main ac but ALSO follows the given pre-programmed path
			//
            //Also, bombers will jettison their bombs if they are .ESCORT and must move to defend
            //
            //with .AATACK_FIGHTERS they are pretty aggressive & attack things, which is good in a way
            //if (heavyBomber && isBomberArmed(airGroup)) aawpt = AiAirWayPointType.AATTACK_FIGHTERS;
            //But let's try FOLLOW to see if they will act more like bomber formations with that in place
            //bombers seem to drop bombs rather quick if they get into any trouble/attacked
            //if ((heavyBomber && isBomberArmed(airGroup)) || isOnRepairMission(player)) aawpt = AiAirWayPointType.FOLLOW;  //not sure about hasBombs(), trying it without  

            aawpt = AiAirWayPointType.FOLLOW; //making this the DEFAULT for all AC, unless nearby enemy AC or they are specifically escort AC.
            
            Point3d defendDistances_m = new Point3d (4000, 1200, 2200);
            if (CoverAGOrders.escort == orders) defendDistances_m = new Point3d (2500, 1000, 2000);
            else if (CoverAGOrders.normal == orders) defendDistances_m = new Point3d (1250, 600, 1200);

            //AltDiffBomber_m: 25, AltDiffBomber_range_m
            AiAirGroup attackingAirGroup = getRandomNearbyEnemyAirGroup(playerAirGroup, defendDistances_m.x, defendDistances_m.y, defendDistances_m.z); //escorts are supposed to be 1000m above the escorted bomber, so definitely need to attack things 1000-2000 feet (333-666m) below those bombers.  Above, add 1000m fighter altitude ot bomber alt.

            //OK, HERE is where we can make the aircraft more follow or more defend the player etc
            if (attackingAirGroup != null && !isOnRepairMission(player) && ordersEngageAir(orders) )  //<cattack/<ca, <cnormal/<cn & <cescort/<ce engage enemy a/c; <creserve/<cr, <cstrict/<cst & <cloiter/<clo hold their fire
            {
                //Console.WriteLine("3ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
                //if a heavy bomber with bombs, then don't go on the 
                //the attack against enemy fighters etc
                //Otherwise, attack it!
				
				//OK, people are complaining their bombers are flying off and attacking
				//instead of returning to base with them, so trying keep on .follow instead 2026/08
				//We COULD make "escort" vs "follow" an option in the menu
                //if (heavyBomber || dive && isBomberArmed(airGroup))
                //Set to .escort even heavy bombers will actually aggressively attack anything nearby, not just fly straight & shoot
				if (heavyBomber &&  orders != CoverAGOrders.escort)
                {

                    airGroup.setTask(AiAirGroupTask.DEFENDING, playerAirGroup);
                    task = AiAirGroupTask.DEFENDING;
                    tasktarget = playerAirGroup;
                    //Console.WriteLine("4ChangeTaskbomber (after): {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
                }
                else
                {
                    //airGroup.setTask(AiAirGroupTask.ATTACK_AIR, attackingAirGroup);
                    task = AiAirGroupTask.ATTACK_AIR;
                    tasktarget = attackingAirGroup;
                    airGroup.changeGoalTarget(attackingAirGroup);
                    airGroup.setTask(task, attackingAirGroup); 
                    Console.WriteLine("4ChangeGoalTarget (after): {0} target: {1} for " + airGroup.Name() + " of " + player.Name(), airGroup.getTask(), attackingAirGroup.Name());
                    aawpt = AiAirWayPointType.AATTACK_FIGHTERS; //trying this, perhaps it will reinforce the current setTask instead of overriding.  2026/09/30
                    //aawpt = AiAirWayPointType.ATTACK_AIR; //THIS HELPS MAKE THEM DEFEND THE MAIN A/C
                    task = AiAirGroupTask.DO_NOTHING; //prevents setTask etc from being run below
                }
            }
            else
            {
                //If no nearby airgroups to attack, then once in a while get them to 
                //disengage & come back to the mother ship
                //Otherwise they will fight enemy fighters incessantly & never return to the
                //cover the pilot
                //Instead of NORMFLY we are going to do FOLLOW, which signals to MOVEBOMBTARGET
                //to not hijack this a/c to intercept some other random enemy a/c
                //if (ran.Next(5) == 0) aawpt = AiAirWayPointType.NORMFLY;
                //if (ran.Next(5) == 0) aawpt = AiAirWayPointType.FOLLOW;
                //Not working - x-ing it out for now.
            }
            //else
            //double AltDiff_m = 1000;
            //double AltDiff_range_m = 100;
            //if (player.Place().Pos().z<150)

            //Console.WriteLine("Cover KeepAconTask: 9ajajaj");


            //So if the leader is trying to fly under the radar we make the escorts match this altitude closely.  Whether they will be able ot do this (without crashing etc) remains to be seen
            double Z_AltitudeAGL = (player.Place() as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
            if (Z_AltitudeAGL < 175)
            {
                //AltDiffPassed_m = -10;
                AltDiffPassed_m = 8; //trying to raise the cover a/c up just a bit when under radar, to avoid the porpoising
                AltDiffPassed_range_m = 4;
                //aawpt = AiAirWayPointType.AATTACK_FIGHTERS; //aawpt = AiAirWayPointType.COVER seems to work better in general but the cover aircraft stay up above the a/c they are covering and thus are seen by radar even if the main a/c is below radar. Trying AATACK_FIGHTERS to see if they will stay below radar better.

                //if ((heavyBomber && isBomberArmed(airGroup)) || !isOnRepairMission(player) || (isPlayerStrikeAC && isStrikeAC)) aawpt = AiAirWayPointType.FOLLOW;  //not sure about hasBombs(), trying it without
                //else aawpt = AiAirWayPointType.COVER;
                aawpt = AiAirWayPointType.FOLLOW;
            //} else if (!heavyBomber && !isBomberArmed(airGroup) && !isOnRepairMission(player) && !(isPlayerStrikeAC && isStrikeAC))

            } 
            
            //2026/09 - now we have already set this above
            /* else if (!heavyBomber && !isOnRepairMission(player) && !(isPlayerStrikeAC && isStrikeAC))

            //Console.WriteLine("5ChangeGoalTarget: {0} hasBombs: {1} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask(), isBomberArmed(airGroup));
            //for just plain fighters we want .escort to be the default
            //it keeps getting switched to something else for some reason?
			//switching ju-87 off of .escort

            {
                aawpt = AiAirWayPointType.ESCORT;
            }
            */
            //If the cover a/c are bombers we try to make them fly nice & follow the leader instead of engaging
            //NOTE: the isBomberArmed() test used to gate this, and that was a bug - it is FALSE the moment
            //they release, so an empty bomber fell through to the .ESCORT default set above and then
            //calcCoverSpeedToMatchMain() gave it pacePlayer == false, i.e. 1.3x the leader's speed with
            //NO braking in front and 1.5x behind.  That is 1-3km of separation in a couple of minutes and
            //the player cannot catch it up.  Whether they still carry bombs has nothing to do with how
            //they should fly WITH the leader, so test heavyBomber alone.
            if (heavyBomber)
            {
                aawpt = AiAirWayPointType.FOLLOW;
                //<cstrict - you asked these a/c to just fly in formation, so don't task them to defend the leader either.
                //They will still shoot in self defense, but they won't go off chasing enemy a/c on their own.
                if (!isOnRepairMission(player) && !isInStrictFormation(airGroup))
                {
                    airGroup.setTask(AiAirGroupTask.DEFENDING, playerAirGroup);
                    task = AiAirGroupTask.DEFENDING;
                    tasktarget = playerAirGroup;
                }
                //Console.WriteLine("9BomberChangeTask(before): {0} " + airGroup.Name() + " to " + player.Name(), aawpt.ToString());

                //And if the leader is too far away we try to get the escorts to disengage from whatever they are doing & follow the main a/c instead of just fighting in a furball a long ways away
            }

            //Console.WriteLine("10EscortChangeTaskTooDistantfromMain (before): is c/a too distant from lead a/c? {0:F0}m " + airGroup.Name() + " to " + player.Name(), distToLeadAircraft);
            if ((!heavyBomber || !isBomberArmed(airGroup)) && (distToLeadAircraft > 4000))
            {
                //2021/07, we switched to .ESCORT here & the cove r a/c seemed to not follow too well. So trying .FOLLOW again
                aawpt = AiAirWayPointType.FOLLOW; //SEtting to Escort seems to make them drop their bombs?  Maybe?
                                                  //aawpt = AiAirWayPointType.ESCORT; //SEtting to Escort seems to make them drop their bombs?  Maybe?
				tasktarget = playerAirGroup;								  

                //2021/07 - also we tried setting the task below, the a/c are not behaving, trying to remove the task
                //and see how it goes
                /*
                airGroup.setTask(AiAirGroupTask.DEFENDING, playerAirGroup);
                task = AiAirGroupTask.DEFENDING;
                tasktarget = playerAirGroup;
                */
                //Console.WriteLine("10EscortChangeTaskTooDistantfromMain (after): c/a too distant from lead a/c: {0:F0}m " + airGroup.Name() + " to " + player.Name(), distToLeadAircraft);
            }

            //For repair/restock missions the config is very basic
            //This applies to both bombers & fighters, at all times, because bomb mode always = .NONE
            //despite attacking/defending/etc if they get too far away from the main a/c just try
            //to make them come BACK
            if (isOnRepairMission(player))
            {
                aawpt = AiAirWayPointType.FOLLOW;
                task = AiAirGroupTask.FLY_WAYPOINT;
                AltDiffPassed_m = -5;
                AltDiffPassed_range_m = 2;  //this is the value actually passed to EscortUpdateWaypoints below (AltDiff_range_m is just my incoming parameter)
            }

            //Console.WriteLine("Going to Escort Update Waypoints");
            //The <cstrict, <cescort & <cloiter orders change the way the airgroup flies with the leader.
            //(Repair/restock missions always fly the simple, fixed config set just above, so they are skipped.)
            if (!isOnRepairMission(player))
            {
                //<cstrict - ignore everything else & just fly in formation with the player
                //<cdrop flies the same tight formation.  Cover aircraft hold their position best in a
                //close formation, and for a "drop when I drop" run you want them right on the leader's
                //line when he releases - so <cdrop behaves like <cstrict for FORMATION, while
                //ordersHoldFire/ordersBombGround keep them quiet and holding their bombs until then.
                if (orders == CoverAGOrders.strict || orders == CoverAGOrders.drop)
                {
                    aawpt = AiAirWayPointType.FOLLOW;
                    task = AiAirGroupTask.FLY_WAYPOINT;
                    tasktarget = null;
                    AltDiffPassed_m = -3;   //fly in formation - right at the leader's altitude, plus/minus just a couple of meters
                    AltDiffPassed_range_m = 15;  //this is the value actually passed to EscortUpdateWaypoints below (AltDiff_range_m is just my incoming parameter)
                }
                //<cloiter - circle around the loiter point that was set when the player gave the order.  This
                //sets its own flight plan, so there is nothing more to do with this airgroup this time around.
                else if (orders == CoverAGOrders.loiter)
                {
                    keepAircraftLoitering(player, airGroup);
                    return;
                }
                //<cescort - CLoD's ESCORT behavior for all types of cover a/c: stay with & defend the player.
                //(If an enemy airgroup was nearby, an ATTACK_AIR or DEFENDING task was already set for that, above.)
                else if (orders == CoverAGOrders.escort && task != AiAirGroupTask.ATTACK_AIR && task != AiAirGroupTask.DEFENDING && task != AiAirGroupTask.DO_NOTHING)//prevents setTask etc from being run below) //ATTACK_AIR & DEFENDING are set above if a nearby enemy is found. Otherwise, they should be set to ESCORT.
                {
                    aawpt = AiAirWayPointType.ESCORT;
                }
            }

            //2026/10 - hard guard: a heavy bomber must NEVER fly .ESCORT unless the player explicitly
            //ordered it with <cescort.  .ESCORT makes the group follow the escorted actor's own path,
            //staying above it like a fighter escort, and it can jettison bombs to get out of the way -
            //neither is the tight formation flight we want bombers to do with the leader.  Several paths
            //above can leave aawpt at the .ESCORT default (the fighter default set near the top of this
            //method, and the spawn calls which pass it explicitly), so force it back to .FOLLOW here,
            //just before the waypoints are written: a heavy bomber not specifically on <cescort simply
            //follows the leader.
            if (heavyBomber && orders != CoverAGOrders.escort && aawpt == AiAirWayPointType.ESCORT) aawpt = AiAirWayPointType.FOLLOW;

            EscortUpdateWaypoints(player, airGroup, (player.Place() as AiAircraft).AirGroup(), aawpt, altDiff_m: AltDiffPassed_m, AltDiff_range_m: AltDiffPassed_range_m, nodupe: true, orders: orders);

            //only change task if we have specifically indicated something above
            //setting task to ATTACK_GROUND ETC SEEMS TO MAKE bombers drop their bombs?
            if (task != AiAirGroupTask.DO_NOTHING)
            {
                //perhaps we need to set the task AFTER the updatewaypoints thing has happened?               
                airGroup.setTask(task, tasktarget);
            }

            Console.WriteLine("8ChangeTask(after): task: {0} {1} tasktarget: {2} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask(), task.ToString(), (tasktarget == null ? "none" : tasktarget.ToString()));            

            //Console.WriteLine("6ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("KAOTXX4 " + DateTime.UtcNow.ToString("T.fffffff"));
        }
        catch (Exception ex) { Console.WriteLine("KeepAircraftOnTaskRECURS ERROR: " + ex.ToString()); }
    }

    //how many per flight in each type of formation
    Dictionary<string, int> numFlightFormation = new Dictionary<string, int>() {
            {"VIC",  6},  //experimentally determined, 4 is the max for BLUE, 6 for RED.  This seems to be the big difference between Blue & Red.
            {"VIC3", 6}, //holds true for all.  EXCEPT VIC only works for blue, not for read.  VIC3 works for both. 
            {"LINEABREAST",6},  //Not sure if there is any difference at all between the two???
            {"ECHELONLEFT",6},
            {"ECHELONRIGHT",6},
            { "LINEASTERN",6} //

    };
    //how many per flight in each type of formation
    Dictionary<string, string> flightFormationAbbreviations = new Dictionary<string, string>() {
            {"VI","VIC"},  //experimentally determined, 4 is the max for BLUE, 6 for RED.  This seems to be the big difference between Blue & Red.
            {"V3","VIC3"}, //holds true for all.  EXCEPT VIC only works for blue, not for read.  VIC3 works for both. 
            {"AB","LINEABREAST"},  //Not sure if there is any difference at all between the two???
            {"LE","ECHELONLEFT"},
            {"RI","ECHELONRIGHT"},
            {"AS","LINEASTERN"} //

        };

    private int squadNum = 0;

    //use loc.x = loc.y = loc.z 0 for default location
    //returns the name of the newly created a/c, which actually won't be created until the isect file is loaded, so wait 1 sec. or so before using.
    //fighterbomber = "f" forces bombers & fighterbombers to omit bomb loads
    //fighterbomber = "b" forces fighterbombers to include bomb loads    
    //fromCover includes "_cover" in the regiment names, which keeps MOVEBOMB from interefering
    //with this AC to send it to newe destinations OR to make it attack any nearby enemies etc
    public string Stb_LoadSubAircraft(Point3d loc, string type = "SpitfireMkIa_100oct", string callsign = "26", string hullNumber = "3", string serialNumber = "001", string regiment = "gb02", string fuelStr = "", string weapons = "", double velocity_mps = 0, string fighterbomber = "", string skin_filename = "", string delay_sec = "", string escortedGroup = "", int numAC = 2, string formation = "VIC3", Player player = null, Vector3d? vwld = null, bool fromCover = true, Point3d? loc2 = null, int requestedNumInFlight = 0, int army = 0, bool exactPos = false)
    {
        try {
            /*  //sample .mis file with parked a/c
            *  [AirGroups]
                BoB_RAF_F_141Sqn_Early.01
                [BoB_RAF_F_141Sqn_Early.01]
                Flight0  1f
                Class Aircraft.SpitfireMkIa_100oct
                Formation VIC3
                CallSign 26
                Fuel 100
                Weapons 1
                SetOnPark 1
                Skill 0.3 0.3 0.3 0.3 0.3 0.3 0.3 0.3
                [BoB_RAF_F_141Sqn_Early.01_Way]
                TAKEOFF 76923.96 179922.36 0 0 
        Possible Formation values;
            VIC
            VIC3
            LINEABREAST
            ECHELONLEFT
            ECHELONRIGHT
            LINEASTERNplayer

            VIC shows up for Blenheim while VIC3 shows up for JU88 in FMB.  Not sure the practical different between them.  But in game, using vic for blenheim gives an error and no planes, while using it for JU88 gives a finger-4 like formation.  So . . .   
            VIC for JU88 allowed 4 planes max



            */
            if (GamePlay == null) return "";

            if (army == 0 & player is object) army = player.Army();

            //default spawn location is Bembridge, landed, 0 mph & on the ground
            string locx = "76923.96";
            //string locy = "179922.36"; //real Bembridge location
            string locy = "178322.36"; //1600 meters off Bembridge
            string locz = "0";
            string vel = "0";

            string loc2x = "72923.96";
            string loc2y = "172322.36"; //1600 meters off Bembridge
            string loc2z = "1000";

            // The letters/numbers specify the number of a/c within the flight. The ID of the air unit contains the number of flights in a "bit mask" ("name of the air unit"."bit mask with number of flights")
            // For # of aircraft allowed in different types of units, see: https://theairtacticalassaultgroup.com/forum/showthread.php?t=32433&p=349248#post349248
            /*
            *  e.g. LW fighters have 3 flights of 4 a/c
                LW bombers have 3 flights of 3 a/c
                RAF fighters (early) 2* flights of 6 a/c
                RAF fighters (late) 3 flights of 4 a/c
                RAF bombers 2* flights of 6 a/c
                Italian fighter and bombers 3* flights of 3 a/c
                */
            if (numAC < 1) numAC = 1;
            
            if (army == 1 && numAC > 24) numAC = 24; //4 flights of 6 is the max red.  (Seems to do`max for Red, in reality. Not sure about Blue.)
            if (army == 2 && numAC > 24) numAC = 24; //6 flights of 4 is the max for blue.  (not sure if more might be theoretically possible.)
            
            int numInFlight = 6;
            if (army == 2) numInFlight = 4;  //max 6 in flight for red, 4 in flight for blue.  Not sure why!
            if (requestedNumInFlight > 0 && requestedNumInFlight <= numInFlight) numInFlight = requestedNumInFlight;

            int hullNumber_int = 1;
            try
            {
                hullNumber_int = Convert.ToInt32(hullNumber);
            }
            catch
            {
                hullNumber_int = 1;
            }
            //if (hullNumber_int < 1) hullNumber_int = 1; //sanity check; not sure on exact highest allowed number here  //OK, CloD seems to allow any neg or positive integer so we're leaving it at that

            //Ok this is wierd. But if aiaircraft group spawn-in point is near or in the middle of the airport, then CLOD seems to use the
            //built-in spawn points for that airport, regardless of what you have put in place.
            //But if the given spawn-in point is like a thousand or a couple thousand meters away, then it finds the nearest airport AND uses the airdrome points that you have created in FMB
            //So, we're going to try it.

            //TODO: What we shoudl really do, if this idea works, is to #1. find the nearest airport,  #2, move our loc to 2000 meters or whatever away from it. #3. Make sure that target airport is still our nearest airport
            //loc.z = 150; //the a/c always start low, as though they have just taken off
            Console.WriteLine("Stb_LoadSubAircraft.  Loc before: {0:F0} {1:F0} {2:F0}", loc.x, loc.y, loc.z);
            if (loc.x != 0 && loc.y != 0 && loc.z != 0)
            {
                locx = (loc.x - 1000).ToString("F2"); //1000 m off the actual location
                locy = (loc.y - 1600).ToString("F2"); //1600 m off the actual location
                if (exactPos)
                {
                    locx = (loc.x).ToString("F2"); //1000 m off the actual location
                    locy = (loc.y).ToString("F2"); //1600 m off the actual location
                }

                if (velocity_mps > 0)
                {
                    locz = loc.z.ToString("F2");
                    vel = velocity_mps.ToString("F2");
                }
                else
                {
                    locz = "0";
                    vel = "0";
                }
            }

            Console.WriteLine("Stb_LoadSubAircraft.  Loc after: {0} {1} {2}", locx, locy, locz);

            if (loc2.HasValue)
            {
                Point3d l2 = loc2.Value;
                loc2x = (l2.x).ToString("F2"); 
                loc2y = (l2.y).ToString("F2"); 
                loc2z = (l2.z).ToString("F2");
                
            }

            //rotate the rnum from .02 through .49.  Recollection is >49 is trouble for some reason but that COULD be wrong.  .01 is presumably
            //used by the main pilot.  In experience running it, squadron number 29 was trouble already.  It might depend on regiment etc.
            //We have been getting errors related to duplicate regiment numbers.  PResumably there is already a .01, the player.
            //Haven't seen these errors before?  2021/07
            squadNum++;
            if (squadNum > 9) squadNum = 2;
            if (squadNum < 2) squadNum = 2;

            string rnumb = string.Format(".{0:D2}", squadNum);
            //was always ".01";
            string cover_add = "_cover"; //adding _cover MIGHT cause problems but it is a way we can ID the cover squadrons to movebomb, so it doesn't disturb them
            if (!fromCover) cover_add = "";

            string regiment_isec = regiment + rnumb + cover_add; 
            Console.WriteLine("<cover: regiment & number: " + regiment_isec);

            ISectionFile f = GamePlay.gpCreateSectionFile();
            string s = "";
            string k = "";
            string v = "";

            s = "AirGroups";
            k = regiment_isec; v = ""; f.add(s, k, v);
            s = regiment_isec;
            //k = "Flight0"; v = hullNumber_int.ToString(); f.add(s, k, v);

            int numACcreated = 0;

            for (int flight = 0; flight < 4; flight++)
            {

                v = "";
                for (int i = 1; i <= numInFlight; i++)
                {
                    numACcreated++;
                    if (numACcreated > numAC) break;
                    if (flight == 0) v += i.ToString() + " "; // flight0 1 2 3
                    else v += flight.ToString() + i.ToString() + " ";             // flight1 11 12 13  ... 

                    //Numbers in this table flight0 are displayed as numbers or letters on the fuselage
                    /*                     
                    * The number/letter is displayed on the fuselage. It doesn't really matter if you enter a number or a letter as the air unit type defines if a latter or number is displayed in-game (RAF = letter, LW fighter = number, LW bomber = letter). If you enter a number it is translated to a letter or reverse ( A = 1, B = 2 ... or 1 = A, B = 2, ...). IIRC for LW units you can also enter some fency symbols ("<", "<O") to mimic Stab a/c, I think there's a table in the original user manual that lists the allowed symbols.
                    * https://theairtacticalassaultgroup.com/forum/showthread.php?t=32433&p=349248#post349248
                    * */

                }
                if (v.Length > 0)
                {
                    k = "Flight" + flight.ToString();
                    f.add(s, k, v);  //add "1 2 3 4 " . . . or similar, depending on how many a/c requested in one flight
                }
                //Console.WriteLine("CoverCreate: Flight0: " + v);
            }
            //Our little trick to allow torpedo & herman models of H-6.
            //We are safe to do this here because of the "else if"
            string typeTemp = type;
            if (type.Contains("He-111H-6_Trop_torpedo") || type.Contains("He-111H-6_Trop_Hermann1000kg")) typeTemp = "tobruk:Aircraft.He-111H-6_Trop";
            else if (type.Contains("He-111H-6_torpedo") || type.Contains("He-111H-6_Hermann1000kg")) typeTemp = "tobruk:Aircraft.He-111H-6";

            //k = "Class"; v = "Aircraft." + type; f.add(s, k, v);
            //Tobruk, now we have to include the bob: tobruk: stuff and airplane.  lbahblabhalbh
            k = "Class"; v = typeTemp; f.add(s, k, v);
            k = "Formation"; v = formation; f.add(s, k, v);
            k = "CallSign"; v = callsign; f.add(s, k, v);
            //without BombSpacing the a/c dumps its whole load as one tight salvo instead of walking a
            //stick across the target.  The stock .mis files all set this; we did not.  See above.
            k = "BombSpacing"; v = coverBombSpacing_m.ToString(); f.add(s, k, v);
            //k = "Fuel"; v = fuel.ToString(); f.add(s, k, v);
            //k = "Weapons"; v = weapons; f.add(s, k, v);
            
            bool isBlenheim = type.Contains("Blenheim");
            
            if (isBlenheim) fuelStr ="90"; //trying to make Blennies have a heavier load so their performance is not so crazy
            

            f = Stb_AddLoadoutForPlane(f, s, type, fighterbomber, weapons, delay_sec, fuelStr);


            /* if (type.Contains("Spitfire") || type.Contains("Hurricane"))  //We'll have to figure out what to do for DE aircraft, blennies, etc . . . 
            {
                f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 11 11 9 11");
                f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 9 11 11 11");
                f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 11 9 11 11 11 11 10");
                f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 11 11 11");
                f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
                f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 11 11 9");
                f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 11 11 9 11");
                f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 11 11 11 9");
            } */
            
            
            
            int numFlightsCreated = 0;
            numACcreated = 0;

            bool isBomber = isHeavyBomber(type);
            bool isBR20 = type.Contains("BR-20");
            bool isSturmovik = Calcs.isStrikeAC(type);
            bool isJU87 = type.Contains("Ju-87");

            for (int flight = 0; flight < 4; flight++)
            {
                for (int i = 0; i < numInFlight; i++)
                {
                    numACcreated++;
                    if (numACcreated > numAC) break;

                    string istr = i.ToString();
                    if (flight > 0) istr = flight.ToString() + istr;

                    //string[] defSkins = { "default.jpg", "default.jpg", "default.jpg", "white1.jpg", "white2.jpg", "white3.jpg", "white1.jpg", "white2.jpg", "white3.jpg" };
                    string[] defSkins = { "default.jpg" }; //disabling all skins now due to lockups/stuttering/slideshow when aircraft spawn in. 2021-12
                    string defaultSkin = CoverCalcs.randSTR(defSkins);

                    f.add(s, "Serial" + istr, serialNumber + istr);
                    if (skin_filename.Length > 0) f.add(s, "Skin" + istr, skin_filename);
                    else f.add(s, "Skin" + istr, defaultSkin);  //Not sure if this file needs to be in the relevant a/c folder Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\PaintSchemes\Skins\MYAIRCRAFT of the user, the server, or what.  Also don't know how to find out which skin the player is currently using


                    //List<string> rlist = new List<string>();
                    string[] rlist = new string[9];



                    //So we COULD have separate skills for each crew member, esp. for bombers
                    //That way, the bombardier could be very accurate while
                    //gunners less so.
                    //Most bombers have 3 or 4 crew positions, a few (BR20, Wellington, Sutherland) have5
                    //Always Bombardier is in 2nd position, pilot in first, EXCEPT BR20 which has pilot, copilot, then bombardier
                    /* in place of the SKILL line you have lines like this:
                        Person0_0 0 0.84 0.53 0.53 1 0.37 0.53 0.53 0.53
                        Person0_1 1 0.84 0.53 0.53 1 0.37 0.84 0.53 0.53
                        Person0_2 2 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person0_3 3 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person1_0 0 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person1_1 1 0.84 0.53 0.53 1 0.37 0.53 0.53 0.53
                        Person1_2 2 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person1_3 3 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person20_0 0 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person20_1 1 0.84 0.53 0.53 1 0.37 0.53 0.53 0.53
                        Person20_2 2 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person20_3 3 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person21_0 0 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person21_1 1 0.84 0.53 0.53 1 0.37 0.53 0.53 0.53
                        Person21_2 2 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person21_3 3 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person22_0 0 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person22_1 1 0.84 0.53 0.53 1 0.37 0.53 0.53 0.53
                        Person22_2 2 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53
                        Person22_3 3 0.84 0.53 0.53 0.37 0.37 0.53 0.53 0.53

                        Person0 corresponds to istr=1 (alwasys istr-1), Person20 to istr=21, etc
                        _0, _1 etc seems to just count up, 
                        Second # is  position, 0,1,2 or 0,1,2,3 etc
                        Then the regular list of 8 skills
                    */
                    
                    for (int place = 0; place <5; place++) {
                        string placestr=place.ToString();
                        rlist[0] = placestr;
                        for (int j = 1; j < 9; j++)
                        {
                            double r = 0.9;
                            if (j == 4)
                            {
                                r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 3.0 - 1.5)) / 10.0 + 8.5 / 10.0; //number between 0.8 & 1 but weighted towards the center of that range.  j==3 means the aerial gunnery skill.
                                //was: r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 8.0 + 7.0 / 8.0; //number between 0.75 & 1 but weighted towards the center of that range.  j==3 means the aerial gunnery skill.
                                //was :if (isBomber) r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 16.0 + 15.0 / 16.0; //number between 0.875 & 1 but weighted towards the center of that range.  j==3 means the aerial gunnery skill.


                                if ( isSturmovik && !isJU87 && place==0) r = 1;  //otherwise they can't seem to hit anything on the ground
                                //We set the BOMBARDIER of bomber to high accuracy
                                //so they can hit something.  Also Sturmovik pilots,
                                //bec. otherwise they don't hit many ground targets w/cannons etc
                                //similarly JU-87s are sturmovik (there the pilot needs
                                //to be the accurate one)
                                else if (isBomber && place == 1 || isBR20 && place==2 || isJU87 && place==0 ) r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 32.0 + 31.0 / 32.0; //number between 0.9375 & 1 but weighted towards the center of that range.  j==3 means the aerial gunnery skill.
                                //improving aiming accuracy of bombers, also fighter-bombers who might strafe etc

                                

                                else if (army == 1) //So Red bomber pilots have been complaining that Blue fighter cover is more effective than theirs.  This is probably true given (especially) the formidable AI ability of a pair of 110 fighters just due to CloD's built-in 110 AI algorithms.  So . . . trying to bump up Red cover fighter abilities a little to compensate.
                                {
                                    r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 16.0 + 15.0 / 16.0; //number between 0.9375 & 1 but weighted towards the center of that range.  j==3 means the aerial gunnery skill.
                               
                                }
                            }
                            else {
                                r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 4.0 + 3.0 / 4.0; //number between 0.5 & 1 but weighted towards the center of that range
                                r = Math.Sqrt(r); //Tobruk Boost to BLUE cover smartness but still not quite as good as RED  sqrt .5 = .71; sqrt .75 = .87
                                if (isBlenheim) {
                                    //Turn down basic flying skill; much moreso if they are 'cover' vs 'bomber'
                                    if ( j == 0 && isBomber ) r = (r - 0.05).Clamp(0.65,0.8);
                                    if ( j == 0 && !isBomber ) r = (r - 0.1).Clamp(0.6,0.75);
                                    
                                    //turn down even more for advanced flying skill
                                    if ( j == 1 && isBomber ) r = (r - 0.3).Clamp(0.2,0.4);
                                    if ( j == 1 && !isBomber ) r = (r - 0.5).Clamp(0.1,0.3);
                                    
                                    //similarly for tactics
                                    if ( j == 4 && isBomber ) r = (r - 0.3).Clamp(0.3, 0.5);
                                    if ( j == 4 && !isBomber ) r = (r - 0.4).Clamp(0.2, 0.4);
                                    
                                //remainder of boost to RED fighter cover    
                                } else if (army == 1)
                                     r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 8.0 + 7.0 / 8.0; //number between 0.75 & 1 but weighted towards the center of that rang


     
                            }
                            rlist[j] = r.ToString("F2");
                        }
                    
                        
                        rlist[8] = "0.98"; //was 0.98; trying to make the a/c follow orders better
                        
                        if (isBomber) {   rlist[8] = "0.99"; } //was 0.98; trying to make the heavy bomber a/c follow orders better
                        
                        //Sunderlands drive like goofballs & drop their bombs willy-nilly for no reason.  So 
                        //trying to stop that by changing bravery & discipline to 1
                        if (type.Contains("Sunderland") ) {
                            rlist[7] = "1";
                            rlist[8] = "1";
                        }
                        //k = "Skill0"; v = string.Format("{0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1}", r); ; f.add(s, k, v);
                        //Skills: Basic flying, advanced flying, awareness, aerial gunnery, tactics, vision, bravery, discipline
                        //2020-01-22 - CHANGING DISCIPLINE SKILL to 0.98, to see if they will stay in formation more
                        //Might need to do somethign different for fighter vs bomber pilots?
                        //Also bravery to 0.1 as an experiment, and awareness to 0.3
                        //And so, that didn't seem to do much.
                        try
                        {
                            //OLD way with one skill level per aircraft
                            //k = "Skill" + istr; v = string.Format("{0:n3} {1:n3} {2:n3} {3:n3} {4:n3} {5:n3} {6:n3} {7:n3}", rlist); f.add(s, k, v);

                            //2026-09 - each crew member has skill level
                            k = "Person" + istr + "_" + placestr ; v = string.Format("{0} {1:n3} {2:n3} {3:n3} {4:n3} {5:n3} {6:n3} {7:n3} {8:n3}", rlist); f.add(s, k, v);
                            Console.WriteLine("COVER - SKILLS: {0} : {1}", k, v);
                        }
                        catch (Exception ex) { Console.WriteLine("<cover makesectionfile SKILL ERROR: " + ex.ToString()); }
                        //Console.WriteLine("CoverCreate: Skill: " + v);
                        //k = "Skill0"; v = string.Format("{0:F1} {0:F1} 0.3 {0:F1} {0:F1} {0:F1} {0:F1} 0.98", r); f.add(s, k, v);
                        // "0.7 0.7 0.7 0.7 0.7 0.7 0.7 0.7"; 
                        //r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 4.0 + 3.0 / 4.0; //number between 0.5 & 1 but weighted range
                        //skill = r;
                        //k = "Skill1"; v = string.Format("{0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1}", r); f.add(s, k, v);
                        //k = "Skill1"; v = string.Format("{0:F1} {0:F1} 0.3 {0:F1} {0:F1} {0:F1} {0:F1} 0.98", r); f.add(s, k, v);
                        //k = "Skill1"; v = "0.6 0.6 0.6 0.6 0.6 0.6 0.6 0.6"; f.add(s, k, v);
                    }

                }
                numFlightsCreated++;
            }
            if (velocity_mps <= 0)
            {
                k = "SetOnPark"; v = "1"; f.add(s, k, v);
                k = "Idle"; v = "1"; f.add(s, k, v);
            }

            s = regiment_isec + "_Way";
            //if (velocity_mpos <= 0) k = "TAKEOFF";
            //else k = "NORMFLY";        
            k = "ESCORT";
            if (!fromCover) k = "HUNTING";
            v = locx + " " + locy + " " + locz + " " + vel;
            if (escortedGroup.Length > 0) v += " " + escortedGroup + " 0";  //Not sure what the final 0 does
            f.add(s, k, v);

            Vector3d vw = new Vector3d(0, 1, 0);

            if (vwld.HasValue) vw = vwld.Value;
            double div = CoverCalcs.CalculatePointDistance(vw);
            if (div == 0) div = 1;

            //5 mins in the compass direction the player's aircraft is heading
            //So this will set the cover a/c going in the same way the player's aircraft is currently heading
            double deltaX_m = velocity_mps * 5 * 60 * vw.x/div;
            double deltaY_m = velocity_mps * 5 * 60 * vw.y/div;
            //if main a/c isn't moving then we'll head these aircraft north.
            if (div == 0) deltaY_m = velocity_mps * 5 * 60; 

            v = (loc.x + deltaX_m).ToString("n0") + " " + (loc.y + deltaY_m).ToString("n0") + " " + locz + " " + vel;
            if (loc2.HasValue) v = loc2x + " " + loc2y + " " + loc2z + " " + vel;
            if (escortedGroup.Length > 0) v += " " + escortedGroup + " 0";  //Not sure what the final 0 does
            f.add(s, k, v);

            //GamePlay.gpLogServer(null, "Writing Sectionfile to " + stb_FullPath + "aircraftSpawn-ISectionFile.txt", new object[] { }); //testing
            //f.save(mainmission.stb_FullPath + "sectionfiles/aircraftSpawn-ISectionFile"+ran.Next(0,99).ToString() + ".txt"); //testing


            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX13 " + DateTime.UtcNow.ToString("T")); //testing disk output for warps

            //Console.Write("<cover: section file:   " + f.ToString()); //doesn't do anything useful, a "random" number
            //load it in
            GamePlay.gpPostMissionLoad(f);


            /*string USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
            string CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/";  // DO NOT CHANGE
            string FILE_PATH = @"missions/Multi/Fatal/";   // mission install directory (CHANGE AS NEEDED); where we save things relevant to THIS SPECIFIC MISSION
            string stb_FullPath = CLOD_PATH + FILE_PATH;
            */

            string rnd = (ran.Next(100, 999)).ToString();


            Console.WriteLine("Writing Sectionfile to " + mainmission.stb_FullPath + "/sectionfiles/aircraftCover-ISectionFile" + rnd + ".txt");
            f.save(mainmission.stb_FullPath + "/sectionfiles/aircraftCover-ISectionFile" + rnd + ".txt"); //testing

            //

            return (stb_lastMissionLoaded + 1).ToString() + ":" + regiment + ".000";  //There is a better way to do this (get the actual name via onmission loaded) but this might work for now
        } catch (Exception ex)
        {
            Console.WriteLine("stb_LoadSubAircraft ERROR: {0}", ex);
            return "";
        }

    }

    private ISectionFile Stb_AddLoadoutForPlane(ISectionFile f, string s, string type, string fighterbomber = "", string weapons = "", string delay_sec = "", string fuelStr = "")
    {
        string k = "";
        string v = "";

        if (weapons == null) weapons = "";
        if (delay_sec == null || delay_sec == "")
        {
            delay_sec = "1"; //1 sec should work for Blenheim but maybe not for some/all DE aircraft.  So 0.08 sec delay for the DE a/c
            if (type.Contains("Bf-109") || type.Contains("He-111") || type.Contains("110C") || type.Contains("BR-20") || type.Contains("Ju-8")) delay_sec = "0.08";
        }

        int fuel = 100;
        try
        {
            fuel = Convert.ToInt32(fuelStr);

            if (fuel < 0) fuel = 100;  //negative is not sensible, we're assuming it is just nonsense
            if (fuel > 100) fuel = 100; // We're allowing fuel=0 even though I can't imagine how this is useful to anyone
        }
        catch (Exception ex) //any problem with int32 conversion
        {
            fuel = 0; //This will use the default values specified below, so ie if the fuelStr is left blank
                      //Console.WriteLine("Spawn: Using default value for fuel load, 100% or 30%");
        }

        //k = "Weapons"; v = weapons; f.add(s, k, v);
        //"Belt" must be a CAPITOL B here in the .mis file, though it is spelled "belt" in the user.ini file . . . 


        if (type.Contains("SpitfireMkVb") || type.Contains("SpitfireMkIIb"))  //could add residuals
        {


            f.add(s, "Belt", "_Gun00 bob:Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun01 bob:Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 9 10 11 9 11 9 11 2 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 9 11 10 9 2 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 10 9 9 11 9 9 11 9 9 11 Residual 50 ResidueBelt 10 9 10 11");


            if (weapons.Length == 0)
            {
                weapons = "1 1"; //default                
            }
            if (fuel == 0) fuel = 100;
        }

        else if (type.Contains("HurricaneMkIIc"))  //We'll have to figure out what to do for DE aircraft, blennies, etc . . . 
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.Hispano_Mk_I MainBelt 1 0 1 0 1 0");
            f.add(s, "Belt", "_Gun03 bob:Gun.Hispano_Mk_I MainBelt 1 0 1 0 1 0");


            if (weapons.Length == 0) weapons = "1 3"; //default 11 12 are 2X250lb, 13 is 2X500lb
            if (fighterbomber == "f") weapons = "1 0";
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 1 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 1 30 " + delay_sec);


            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("HurricaneMkIIa") || type.Contains("SpitfireMkVa")) //both have 8XBrowning 303s
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun01 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun07 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");



            if (weapons.Length == 0) weapons = "1";

            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("HurricaneMkIIb"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun01 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 ");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun07 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun08 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun09 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun10 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11");
            f.add(s, "Belt", "_Gun11 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");

            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 1 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 1 30 " + delay_sec);


            if (weapons.Length == 0) weapons = "1 3"; //default 11 12 are 2X250lb, 13 is 2X500lb
            if (fighterbomber == "f") weapons = "1 0";

            if (fuel == 0) fuel = 100;

        }

        else if (type.Contains("HurricaneMkIId"))

        {
            //guns 00 01 are Vickers-S and default loadout shoudl be fine.
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 1 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 1 30 " + delay_sec);

            if (weapons.Length == 0) weapons = "1 1";
            if (fighterbomber == "f") weapons = "1 1"; //IId doesn't have an option to carry bombs

            if (fuel == 0) fuel = 100;

        }
        //So . . . . bob:Gun is need for tobruk: aircraft but NOT FOR bob: aircraft.  AaRARARRRRGGHHHHHH!!!
        else if (type.Contains("SpitfireMkI") || (type.Contains("HurricaneMkI") && !type.Contains("HurricaneMkII")))  //We'll have to figure out what to do for DE aircraft, blennies, etc . . . 
        {
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 11 11 9 11 Residual 50 ResidueBelt 11 10 11 10 11 10 11 10");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 9 11 11 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 11 9 11 11 11 11 10 Residual 50 ResidueBelt 11 11 11 11 9");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 11 11 11");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 11 11 11 11 9");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 11 11 9");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 11 11 9 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 11 11 11 9 Residual 50 ResidueBelt 11 10 11 10 11 10 11 10");
            if (type.Contains("HurricaneMkI_FB"))
            {
                if (weapons.Length == 0) weapons = "1 2"; //default
                if (fighterbomber == "f") weapons = "1 0";
                f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            }
            else
            {
                if (weapons.Length == 0) weapons = "1"; //default
                if (fighterbomber == "b") weapons = "1 2";
            }

            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("KittyhawkMkIA"))

        {
            //6xBrowning
            f.add(s, "Belt", "_Gun00 bob:Gun.BrowningM2AN MainBelt 1 2 1 2 1 2 3");
            f.add(s, "Belt", "_Gun01 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun02 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun03 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun04 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun05 bob:Gun.BrowningM2AN MainBelt 1 2 1 2 1 2 3");
            if (weapons.Length == 0) weapons = "1";

            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("MartletMkIII"))

        {
            //6xBrowning
            f.add(s, "Belt", "_Gun00 bob:Gun.BrowningM2AN MainBelt 1 2 1 2 1 2 1 2 1 2 1 2 1 2 3");
            f.add(s, "Belt", "_Gun01 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun02 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun03 bob:Gun.BrowningM2AN MainBelt 1 2 1 2 1 2 1 2 3");

            if (weapons.Length == 0) weapons = "1";

            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("TomahawkMkII"))

        {
            //6xBrowning
            f.add(s, "Belt", "_Gun00 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun01 bob:Gun.BrowningM2AN MainBelt 1 2");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");

            if (weapons.Length == 0) weapons = "1";

            if (fuel == 0) fuel = 100;

        }
        /*
         *   Flight0  1
  Class Aircraft.WellingtonMkIc
  Formation LINEABREAST
  CallSign 28
  Fuel 100
  Weapons 1 1 2
  Belt _Gun03 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8
  Belt  MainBelt 0 0 0 2 5
  Belt  MainBelt 6 0 0 10 11 12
  Belt _Gun00 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8
  Belt _Gun01 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8
  Belt  MainBelt 0 0 0 2 5
  Belt  MainBelt 6 0 0 10 11 12
  Belt _Gun02 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8
  Belt  MainBelt 6 0 0 10 11 12
  Belt  MainBelt 6 0 0 10 11 12
  Detonator Bomb.Bomb_GP_250lb_MkIV 3 0 1
  Detonator Bomb.Bomb_GP_500lb_MkIV 3 0 1
  Skill 0.8 0.6 0.5 0.5 0.5 0.5 0.6 0.5
         * */
        
        
        else if (type.Contains("WellingtonMkIc_Torpedo")) //WellingtonMkIc_Torpedo && WellingtonMkIc_Torpedo/trop
        {
            ///TODO UPDATE BELTS & WEAPONS!!!!!
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun01 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");            
            //no detonators/bomb or torpedo fuse settings for 1c_Torpedo as of  7/2021

            if (weapons.Length == 0)
            {
                
                weapons = "1 4 1 1"; //  MkXV torpedo
                                     // 1111 is MKXII torpedo
                                     //1 2 1 1 is 2000lb bomb
                                     //As of 2021/07 ALL THREE of these weapons act like they are not armed for WellingtonMkIc_Torpedo (non-trop).  They drop & hit 
                                     //a ship (if set to "ground attack target") but they just bounce off & don't do anything. As though un-armed.
                                     //Update: On retry they all worked fine (!?!!)

                if (fighterbomber == "f") weapons = "1 0 1 1";
                if (fighterbomber == "h") weapons = "1 2 1 1";  //2X1000lb
            }
            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("WellingtonMkIc_t") || type.Contains("WellingtonMkIc_Late"))
        {
            ///TODO UPDATE BELTS & WEAPONS!!!!!
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun01 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_1000lb_MkI 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_2000lb_MkI 3 0 " + delay_sec);

            if (weapons.Length == 0)
            {
                //weapons = "1 1 2"; //18X250lb bombs.  So, that is a lot.  It drops them slowly one by one, though.  113 is 9x500 same as 111.  114 is 2x1000lb and 115 is 2x2000lb.  These could be good in situations.
                weapons = "1 1 2 1 1"; //18X500lb bombs.  So better than the 1 1 2 just because it is the same tonnage, but they aren't spread so far & wide
                                       //This works for Mk1c Torpedo also, choosing the MkXII torpedo. 1 1 2 would do the 2000lb bombs & 1 1 4  would do the MkXV torpedo.  Not sure which torpedo is best.
                if (fighterbomber == "f") weapons = "1 1 0 1 1";
                if (fighterbomber == "h") weapons = "1 1 5 1 1";
            }
            if (fuel == 0) fuel = 45;
        }

        else if (type.Contains("WellingtonMkI")) //can't use type == "XYZ" here as the type might OR MIGHT NOT include bob: tobruk:, etc, or maybe NOT
            //type.Contains("WellingtonMkI") will include bob:WellingtonMkIc (actually the MkIa as identified to the user/confusing)
            //AND tobruk:WellingtonMkIa_Late
        {
            ///TODO UPDATE BELTS & WEAPONS!!!!!
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 10 11 9 11 9 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_1000lb_MkI 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_2000lb_MkI 3 0 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 2"; //18X250lb bombs.  So, that is a lot.  It drops them slowly one by one, though.  113 is 9x500 same as 111.  114 is 2x1000lb and 115 is 2x2000lb.  These could be good in situations.
                                   //2021/07 - moving back to 1 1 2 as TF fixed the 'won't drop on salvo' problem.  Now they drop salvo and VERY accurately
                                   //weapons = "1 1 1"; //9X500lb bombs.  So better than the 1 1 2 just because it is the same tonnage, but they aren't spread so far & wide
                                   //This works for Mk1c Torpedo also, choosing the MkXII torpedo. 1 1 2 would do the 2000lb bombs & 1 1 4  would do the MkXV torpedo.  Not sure which torpedo is best. (wrong as of 2021)
                if (fighterbomber == "f") weapons = "1 1 0";
                if (fighterbomber == "h") weapons = "1 1 5";
            }
            if (fuel == 0) fuel = 50;
        }
        else if (type.Contains("BlenheimMkIVF_Late") || type.Contains("BlenheimMkIVNF_Late"))  //still needs update 4.5 
        {
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 bob:Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 1 30 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }
            if (fuel == 0) fuel = 40;
        }

        else if (type.Contains("BlenheimMkIVF") || type.Contains("BlenheimMkIVNF")) //covers regular & late IFV & IVNF
        {
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 bob:Gun.VickersK MainBelt 9 9 11 11 9 9 11 11 10");
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 1 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }

            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("BlenheimMkIV_Late"))
        {
            //ERROR: 2020-01-18: Hook Gun combination not found!!!!! TODO!!!
            f.add(s, "Belt", "_Gun00 bob:Gun.Browning303MkII MainBelt 9 11 10 11 11");
            f.add(s, "Belt", "_Gun01 bob:Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 5 0 2"; //default 2X250 + 12X40 + 8x40
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
                if (fighterbomber == "h") weapons = "1 1 3 1 2"; //2X500 + 8x40  "1 1 3 1 1" is 2X500 only 
            }
            if (fuel == 0) fuel = 40;
        }

        else if (type.Contains("BlenheimMkIV"))
        {
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 9 11 9 10 9 11 11 10");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 5 0 2"; //default 2X250 + 12X40 + 8x40
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
                if (fighterbomber == "h") weapons = "1 1 3 1 2"; //2X500 + 8x40  "1 1 3 1 1" is 2X500 only 
            }
            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("BlenheimMkIF") || type.Contains("BlenheimMkINF"))
        {
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 11 9 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }

            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("BlenheimMkI")) //must put this @ end or it matchessome earlier Blennies
        {
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 10 11 9 10 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                //weapons = "1 1 2 0 0"; //default So despite FMB, only 11200 and 11300 work (4x250 & 2X500 respectively).  The 40lb bombs just won't load, either 11400 11500 11202 etc etc
                weapons = "1 1 5 1 2";   //ok, so in 5.0+ maybe this works now?
                // So 11200 is set up with 4X250 lb bombs, which is a little less than BlIV & BlIV late but still not too bad .
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
                if (fighterbomber == "h") weapons = "1 1 3 1 2"; //2X500 + 8x40  "1 1 3 1 1" is 2X500 only 
            }
            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("AnsonMkI"))
        {
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun00 Gun.VickersK_Fuselage MainBelt 9 11 11 11 10 11 11");
            if (weapons.Length == 0)
            {
                weapons = "1 1"; //default                
            }
            if (fuel == 0) fuel = 100;
        }
        else if (type.Contains("DefiantMkI"))
        {
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
            if (weapons.Length == 0)
            {
                weapons = "1"; //default                
            }
            if (fuel == 0) fuel = 100;
        }
        else if (type.Contains("SunderlandMkI"))
        {
            f.add(s, "Belt", "_Gun03 Gun.VickersK_Pintle MainBelt 9 9 11 11 9 9 11 11 10");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.VickersK_Pintle MainBelt 9 9 11 11 9 9 11 11 10");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_DC_250lb_MkXI 3 0 16 "); //depth charge, 16 feet.  Other choice is 22 feet.
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                //weapons = "1 1 1 1 2 2"; //default  So this is 4 separate guns plus 8X250lb bombs (4x250lb in each bay)
                weapons = "1 1 1 1 1 1"; //per 2020/08 testing, this seems better than 111122.
                if (fighterbomber == "f") weapons = "1 1 1 1 0 0";
                if (fighterbomber == "h") weapons = "1 1 2 2 4 4"; //2X500 + 2X500
            }
            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("Walrus"))
        {
            f.add(s, "Belt", "_Gun00 Gun.VickersK_Pintle MainBelt 9 9 11 11 9 9 11 11 10");
            f.add(s, "Belt", "_Gun01 Gun.VickersK_Pintle MainBelt 9 9 11 11 9 9 11 11 10");

            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_DC_250lb_MkIV 0 0 16"); //16 is depth charge depth in feet.  Can be 16 or 22.  DC=Depth Charge.

            if (weapons.Length == 0)
            {
                //weapons = "1 1 1 1 2 2"; //default  So this is 4 separate guns plus 8X250lb bombs (4x250lb in each bay)
                weapons = "1 1 1 1"; //2X250lb bombs
                if (fighterbomber == "f") weapons = "1 1 0 0";
                if (fighterbomber == "h") weapons = "1 1 3 3"; //2x250lb depth charge, 16 ft
            }
            if (fuel == 0) fuel = 45;
        }
        else if (type.Contains("BeaufighterMkIC"))  //could add residuals
        {


            f.add(s, "Belt", "_Gun01 bob:Gun.Hispano_Mk_I MainBelt  1 0 1 1 1 1 1 1");
            f.add(s, "Belt", "_Gun03 bob:Gun.Hispano_Mk_I MainBelt  1 1 1 1 0 1");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun09 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 0 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Hispano_Mk_I MainBelt 1 1 1 1 1 0 1 1 1");
            f.add(s, "Belt", "_Gun08 bob:Gun.Browning303MkII MainBelt 9 10 11 9 11 9 11 2 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun07 bob:Gun.Browning303MkII MainBelt 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 9 11 10 9 2 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 10 9 9 11 9 9 11 9 9 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 bob:Gun.Hispano_Mk_I MainBelt 1 1 0 1 1 1 1 1 1");
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1"; //default                
                if (fighterbomber == "f") weapons = "1 1 0";                
            }
            
            if (fuel == 0) fuel = 35;
        }
        else if (type.Contains("BeaufighterMkIF_Late") || type.Contains("BeaufighterMkINF_Late"))  //could add residuals
        {

            f.add(s, "Belt", "_Gun01 bob:Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            f.add(s, "Belt", "_Gun03 bob:Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            f.add(s, "Belt", "_Gun06 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun09 bob:Gun.Browning303MkII MainBelt 10 11 9 11 9 0 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 bob:Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            f.add(s, "Belt", "_Gun08 bob:Gun.Browning303MkII MainBelt 9 10 11 9 11 9 11 2 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun07 bob:Gun.Browning303MkII MainBelt 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 bob:Gun.Browning303MkII MainBelt 9 11 10 9 2 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 bob:Gun.Browning303MkII MainBelt 10 9 9 11 9 9 11 9 9 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 bob:Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            if (weapons.Length == 0)
            {
                weapons = "1 1"; //default                
            }
            if (fuel == 0) fuel = 35;
        }
        else if (type.Contains("BeaufighterMkIF") || type.Contains("BeaufighterMkINF"))  //could add residuals
        {

            f.add(s, "Belt", "_Gun01 Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            f.add(s, "Belt", "_Gun03 Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 10 11 9 11 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun09 Gun.Browning303MkII MainBelt 10 11 9 11 9 0 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            f.add(s, "Belt", "_Gun08 Gun.Browning303MkII MainBelt 9 10 11 9 11 9 11 2 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 9 11 10 9 2 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 10 9 9 11 9 9 11 9 9 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 Gun.Hispano_Mk_I MainBelt 1 0 1 1 1 1 1 1 ");
            if (weapons.Length == 0)
            {
                weapons = "1 1"; //default                
            }
            if (fuel == 0) fuel = 35;
        }
        else if (type.Contains("G50"))
        {
            f.add(s, "Belt", "_Gun00 Gun.Breda-SAFAT-12,7mm MainBelt 1 3 1 3 1 3 1 3 1 3 2");
            f.add(s, "Belt", "_Gun01 Gun.Breda-SAFAT-12,7mm MainBelt 1 3 1 3 1 3 1 3 1 3 2");

            if (weapons.Length == 0) weapons = "1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-1B")) //we do E-1B first so that then we can cover all remaining E-1 types with type.Contains.  Similarly for E-3, E-4
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17_Wing MainBelt 4 4 4 0 0 0 2 5 5 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 5 5 5 1 0 0 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 0 0 0 4 4 4 1 5 5 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17_Wing MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 2"; //default
				if (fighterbomber == "h") weapons = "1 1 3";
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-3B")) //we do E-3B first so that then we can cover all remaining E-3 types with type.Contains.  Similarly for E-3, E-4
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 4 3 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 4 1 4");

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 2"; //default
				if (fighterbomber == "h") weapons = "1 1 3";
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-4B")) //no bob.!
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_M_Wing MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_M_Wing MainBelt 5 1 5");

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1"; //1 1 1 has 4x50kg, whereas 1 1 3 has 1x250kg.
				if (fighterbomber == "h") weapons = "1 1 3";
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-1"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17_Wing MainBelt 4 4 4 0 0 0 2 5 5 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 5 5 5 1 0 0 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 0 0 0 4 4 4 1 5 5 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17_Wing MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-3"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 4 3 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 4 1 4");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-4"))  //also covers E-4N E-4N-Derated etc
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 5 1 5");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 5 1 5");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-7"))  //also covers E-4N E-4Z etc.  Needs bob:!
        {
            f.add(s, "Belt", "_Gun03 bob:Gun.MGFF_M_Wing MainBelt 5 3 5 1");
            f.add(s, "Belt", "_Gun02 bob:Gun.MGFF_M_Wing MainBelt 5 3 5 1");
            f.add(s, "Belt", "_Gun00 bob:Gun.MG17 MainBelt 1 0 0 0 4 4 4");
            f.add(s, "Belt", "_Gun00 bob:Gun.MG17 MainBelt 1 0 0 0 4 4 4");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
			if (fighterbomber == "h") weapons = "1 1 3";
            if (fighterbomber == "f") weapons = "1 1 0";
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109F-1"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.MG17 MainBelt 4 0 2");
            f.add(s, "Belt", "_Gun01 bob:Gun.MG17 MainBelt 2 4 4 4 0 0 0");
            f.add(s, "Belt", "_Gun02 bob:Gun.MGFF_M MainBelt 5 3 5 1");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109F-2"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.MG17 MainBelt 4 0 2");
            f.add(s, "Belt", "_Gun01 bob:Gun.MG17 MainBelt 2 4 4 4 0 0 0");
            f.add(s, "Belt", "_Gun02 bob:Gun.MG151_20 MainBelt 0 2 1 2 1 2");

            if (weapons.Length == 0) weapons = "1 1 2"; // 1 1 is MG 151/15 and 1 2 is MG 151/20
			if (fighterbomber == "h") weapons = "1 1 3";
            if (fighterbomber == "f") weapons = "1 1 0";
            if (fuel == 0) fuel = 100;

        }
		else if (type.Contains("Bf-109F-4Z"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.MG17 MainBelt 2 4 4 4 0 0 0");
            f.add(s, "Belt", "_Gun01 bob:Gun.MG17 MainBelt 2 4 4 4 0 0 0");
            f.add(s, "Belt", "_Gun02 bob:Gun.MG151_20 MainBelt 0 2 1 2 1 2");

            if (weapons.Length == 0) weapons = "1 1"; //  
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109F-4"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.MG17 MainBelt 2 4 4 4 0 0 0");
            f.add(s, "Belt", "_Gun01 bob:Gun.MG17 MainBelt 2 4 4 4 0 0 0");
            f.add(s, "Belt", "_Gun02 bob:Gun.MG151_20 MainBelt 0 2 1 2 1 2");

            if (weapons.Length == 0) weapons = "1 1 0"; //  
			if (fighterbomber == "h") weapons = "1 1 3";
            if (fighterbomber == "f") weapons = "1 1 0";
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Macchi-C202-SeriesIII"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.Breda-SAFAT-12,7mm MainBelt 1 3 1 3 1 3 1 3 1 3 2");
            f.add(s, "Belt", "_Gun01 bob:Gun.Breda-SAFAT-12,7mm MainBelt 1 3 1 3 1 3 1 3 1 3 2");

            if (weapons.Length == 0) weapons = "1"; //  final 0 is wing guns but they don't seem to work no matter what
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Macchi-C202-SeriesVII"))
        {
            f.add(s, "Belt", "_Gun00 bob:Gun.Breda-SAFAT-12,7mm MainBelt 1 3 1 3 1 3 1 3 1 3 2");
            f.add(s, "Belt", "_Gun01 bob:Gun.Breda-SAFAT-12,7mm MainBelt 1 3 1 3 1 3 1 3 1 3 2");
            f.add(s, "Belt", "_Gun02 bob:Gun.Breda-SAFAT-7,7mm MainBelt 2 1 3 4 2");
            f.add(s, "Belt", "_Gun03 bob:Gun.Breda-SAFAT-7,7mm MainBelt 2 1 3 4 2");

            if (weapons.Length == 0) weapons = "1 1"; //  final 0 is wing guns but they don't seem to work no matter what
            if (fuel == 0) fuel = 100;
        }


        else if (type.Contains("D520"))
        {



            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 62;

        }
        else if (type.Contains("DH82")) //Tiger Moth.  IT has no weapons or bombs
        {


            //if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        //GladiatorMkII

        else if (type.Contains("GladiatorMkII"))
        {
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 9 11 11 9 11 11 9 9 11 10");




            if (weapons.Length == 0) weapons = "1 1";

            if (fuel == 0) fuel = 100;

        }

        /*  Weapons 1 1 1 1 1 1 1 1 1
  Belt _Gun03 bob:Gun.MG15 MainBelt 0 2 3 4 5
  Belt _Gun06 bob:Gun.MGFF_M MainBelt 1 2 3 4 5
  Belt _Gun04 bob:Gun.MG15 MainBelt 1 2 3 4 5
  Belt _Gun02 bob:Gun.MG15 MainBelt 1 2 3 4 5
  Belt _Gun07 bob:Gun.MG15 MainBelt 1 2 3 4 5
  Belt _Gun00 bob:Gun.MG15 MainBelt 1 2 3 4 5
  Belt _Gun01 bob:Gun.MG15 MainBelt 1 2 3 4 5
  Belt _Gun05 bob:Gun.MG15 MainBelt 1 2 3 4 5
  Detonator Bomb.SC-500_GradeIII_K 0 -1 0.08
  Detonator Bomb.Bomb_GP_250lb_MkIV 3 0 0.025
  Detonator Bomb.SC-250_Type1_J 1 -1 0.05 x
  Detonator Bomb.SC-50_GradeII_J 1 -1 0.08
  Detonator Bomb.SC-1000_C 0 -1 0.08
  Detonator Bomb.Bomb_GP_1000lb_MkI 3 0 0.025
  Detonator Bomb.Bomb_GP_500lb_MkIV 3 0 0.025
  Skill 1 0.74 0.84 0.63 0.84 0.84 0.74 0.74
        */
        else if (type.Contains("He-111H-6_torpedo") || type.Contains("He-111H-6_Trop_torpedo"))
        {
            f.add(s, "Belt", "_Gun03 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun06 bob:Gun.MGFF_M MainBelt 0 1 2 3 5");
            f.add(s, "Belt", "_Gun04 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun02 bob:Gun.MG15 MainBelt 2 0 4 4 4"); //Only the h-2 has Gun03; the P-2 lacks it & throws an error if it is included
            f.add(s, "Belt", "_Gun07 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            //Not sure why all these are listed as the only choices are torpedo or  1000lb bomb
            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K  0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-1000_C 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_1000lb_MkI 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 1 -1 " + delay_sec);            

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 1 1 1"; //default 1 1 1 1 1 1 3 1 1 is 2X1000kg bombs
                                               // 1 1 1 1 1 1 1 1 1 is 2XSchwarzkopf F5 Torpedo
                                               // 1 1 1 1 1 1 2 1 1 is 2X SC250kg bombs
                                               //weapons = "1 1 1 1 1 1 1 1 3"; //default  1... 4 is the 32x50KG bombs which would be a great configuration except they drop so so so so s-l-o-w-ly from the 111P and 111H.  Like one every 200 meters.  So it never drops all 50 bombs etc.   So the 1.. x configuration is 8X250kg bombs, which it still drops slowly just the same but at least it brackets the target with those bombs and drops all of them.
                if (fighterbomber == "f") weapons = "1 1 1 1 1 1 0 1 1";
                if (fighterbomber == "h") weapons = "1 1 1 1 1 1 3 1 1"; //2X1000kg bombs

            }
            if (fuel == 0) fuel = 40;

          

        }
        else if (type.Contains("He-111H-6")) 
        {
            f.add(s, "Belt", "_Gun03 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun06 bob:Gun.MGFF_M MainBelt 0 1 2 3 5");
            f.add(s, "Belt", "_Gun04 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun02 bob:Gun.MG15 MainBelt 2 0 4 4 4"); //Only the h-2 has Gun03; the P-2 lacks it & throws an error if it is included
            f.add(s, "Belt", "_Gun07 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 bob:Gun.MG15 MainBelt 2 0 4 4 4");
            //Not sure why all these are listed as the only choices are torpedo or  1000lb bomb
            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K  0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-1000_C 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_1000lb_MkI 1 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 1 -1 " + delay_sec);
            

            //   Weapons 1 1 1 1 1 1 2

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 3 1 1"; //default 1 1 1 1 1 1 3 1 1 is 2X1000lb bombs
                // 1 1 1 1 1 1 1 1 1 is 2XSchwarzkopf F5 Torpedo
                // 1 1 1 1 1 1 2 1 1 is 2X SC250lb bombs
                                           //weapons = "1 1 1 1 1 1 1 1 3"; //default  1... 4 is the 32x50KG bombs which would be a great configuration except they drop so so so so s-l-o-w-ly from the 111P and 111H.  Like one every 200 meters.  So it never drops all 50 bombs etc.   So the 1.. x configuration is 8X250kg bombs, which it still drops slowly just the same but at least it brackets the target with those bombs and drops all of them.
                if (fighterbomber == "f") weapons = "1 1 1 1 1 1 0";
                if (fighterbomber == "h") weapons = "1 1 1 1 1 1 1 1 1"; //2X1000kg torpedo
            }
            if (fuel == 0) fuel = 40;

        }

        else if (type.Contains("He-111H")) //covers both H-2 & P-2 with just one line different
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun03 Gun.MG15 MainBelt 2 0 4 4 4"); //Only the h-2 has Gun03; the P-2 lacks it & throws an error if it is included
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            //   Weapons 1 1 1 1 1 1 2

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 2"; //default
                                           //weapons = "1 1 1 1 1 1 4"; //default  1... 4 is the 32x50KG bombs which would be a great configuration except they drop so so so so s-l-o-w-ly from the 111P and 111H.  Like one every 200 meters.  So it never drops all 50 bombs etc.   So the 1.. x configuration is 8X250kg bombs, which it still drops slowly just the same but at least it brackets the target with those bombs and drops all of them.
                if (fighterbomber == "f") weapons = "1 1 1 1 1 1 0";
                //no "h" option - the two options are 8X250 or 32X50, 32X50 doesn't really work
            }
            if (fuel == 0) fuel = 40;

        }
        else if (type.Contains("He-111P")) //covers both H-2 & P-2 with just one line different
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 2"; //default
                                         //weapons = "1 1 1 1 1 1 4"; //default  1... 4 is the 32x50KG bombs which would be a great configuration except they drop so so so so s-l-o-w-ly from the 111P and 111H.  Like one every 200 meters.  So it never drops all 50 bombs etc.   So the 1.. x configuration is 8X250kg bombs, which it still drops slowly just the same but at least it brackets the target with those bombs and drops all of them.
                if (fighterbomber == "f") weapons = "1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 40;

        }
        else if (type.Contains("110C-2"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 2 1");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 2 1");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt  2 0 4 4 4");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-4B"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 Gun.MGFF_M MainBelt 2 1");            
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun04 Gun.MGFF_M MainBelt 2 1");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 2 0 4 4 4");

            if (weapons.Length == 0) weapons = "1 1 1 4"; //default
            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-4"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun05 Gun.MGFF_M MainBelt 2 1");
            f.add(s, "Belt", "_Gun04 Gun.MGFF_M MainBelt 2 1");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 2 0 4 4 4");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-6")) //need to make sure this is a good load-out, copied from user-ini
            //no bombs available, checked 8/2021
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 0 0 4 4 4 1");
            f.add(s, "Belt", "_Gun04 Gun.Mk-101 MainBelt 0 1 2"); //One of each of the 3 types 0=HalbPanzer (semi-armorpiercing), 1=Splitter (fragmentation), 2=Hochbrisanz (high explosive) 1 2 might be better for a/c but 0 for ground armor?
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 0 0 4 4 4 1");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 0 0 4 4 4 1");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 0 0 4 4 4 1");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 0 0 4 4 4 1");

            if (weapons.Length == 0)
            {
                weapons = "1 1 1"; //default
                if (fighterbomber == "f") weapons = "1 1 1";
            }

            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-7"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 0");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 0");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 2 1");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 2 1");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 2 0 4 4 4");

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_A 0 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0";
            }

            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("BR-20"))
        {
            f.add(s, "Belt", "_Gun01 Gun.Breda-SAFAT-12,7mm_Turret MainBelt 8 3 5 1 6 7 3 5 1 8");
            f.add(s, "Belt", "_Gun00 Gun.Breda-SAFAT-7,7mm MainBelt 4 1 2 3 4");
            f.add(s, "Belt", "_Gun02 Gun.Breda-SAFAT-7,7mm MainBelt 1 2 4 1 2 3 4");

            if (weapons.Length == 0)
            {
                //weapons = "1 1 1 4"; //default
                weapons = "1 1 1 1"; //better per 2020/08 testing.  2 bays of 4x250kg
                if (fighterbomber == "f") weapons = "1 1 1 0";
                if (fighterbomber == "h") weapons = "1 1 1 7"; //2 bays of 2X500kg bombs
            }
            if (fuel == 0) fuel = 40;

        }
        else if (type.Contains("Ju-87"))
        {
            f.add(s, "Belt", "_Gun00 Gun.MG17_Wing MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17_Wing MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 2 0 4 4 4");

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_J 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J_DivePreferred 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250_JB 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_E 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type2_J 2 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 2 1"; //default  This is  1X500 GradeIII J which seems best for dive bombing/low level and 4X SC 50.
                if (fighterbomber == "f") weapons = "1 1 0 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Ju-88A-1"))
        {
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 2 0 4 4 4");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 2 0 4 4 4");
            /*   Detonator Bomb.SC-500_GradeIII_K 0 -1 0.08
                  Detonator Bomb.SC-250_Type1_J 2 -1 0.08
                  Detonator Bomb.SC-50_GradeII_J 1 -1 0.08
            */

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_A 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 2 2 4"; //default 18x50kg + 10x50kg + 4x250kg
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
                if (fighterbomber == "h") weapons = "1 1 1 0 0 6"; //2X500kg bombs
            }
            if (fuel == 0) fuel = 50;

        }
        else if (type.Contains("Ju-88C")) //all tobruk: ju88Cs.
        {

            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            //  Detonator Bomb.SC-50_GradeII_J 1 -1 0.08
            //Detonator Bomb.SC-50_GradeII_J 1 -1 0.08 for low level/.08 sec.  
            // 0 -1 is HIGH ALT fuses and we DON'T WANT THAT FOR JABO type AC!!!!!!!

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1"; //default; this is more of a JABO type aircraft, not a heavy bomber at all
                                       //It has just 10x50kg bombs, but stil useful in a JABO type situation most of the time
                                       //Beaufighter etc we load with bombs, too . . .
                if (fighterbomber == "f") weapons = "1 1 1 1 0";
            }
            if (fuel == 0) fuel = 42;

        }
        else if (type.Contains("Ju-88A-5")) //all tobruk: ju88s.  B
        {
            /*
             * setup for low level, 0.08 (in other words, if you drop them low OR high they will explode.
             * .08 sec delay but auto 14 sec dive bomb fuse
             *   Detonator Bomb.SC-500_GradeIII_K 0 -1 0.08
                  Detonator Bomb.SC-250_Type1_J 2 -1 0.08
                  Detonator Bomb.SD-500_A 0 -1 0.08
                  Detonator Bomb.SC-50_GradeII_J 1 -1 0.08
            */

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_A 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 4"; //default 18x50kg + 10x50kg + 4x250kg
                if (fighterbomber == "f") weapons = "1 1 1 1 0 0 0";
                if (fighterbomber == "h") weapons = "1 1 1 1 0 0 6"; //2X500kg bombs
            }
            if (fuel == 0) fuel = 45;

        }
        /*
         *   Class Aircraft.Do-17Z-2
  Formation VIC3
  CallSign 26
  Fuel 100
  Weapons 1 1 1 1 1 1 4 2
  Belt _Gun04 bob:Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun05 bob:Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun00 bob:Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun03 bob:Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun01 bob:Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun02 bob:Gun.MG15 MainBelt 0 1 4 0 4
  Detonator Bomb.SC-250_Type1_J 2 -1 0.08
  Detonator Bomb.SC-50_GradeII_J 1 -1 0.08
  Skill 0.9 0.9 0.9 0.9 0.9 0.9 0.9 0.9
  Aging -100
         **/
        else if (type.Contains("Do-17Z-2"))
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun03 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 0 1 4 0 4");

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                //Do-17Z-1 also exists, and is like "weapons = "1 1 1 1 4 2";  - but is so similar to Z-2 we haven't bothered including it; blue has tons of AI bombers
                weapons = "1 1 1 1 1 1 4 2"; //default: 2 bays of 10x50kg
                if (fighterbomber == "f") weapons = "Weapons 1 1 1 1 1 1 0 0";
                if (fighterbomber == "h") weapons = "1 1 1 1 1 1 5 3"; //2 bays of: 1X250kg bombs

            }
            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("Do-215B-1"))
        {
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 0 1 4 0 4");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 0 1 4 0 4");          

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1"; //default: 10x50kg
                if (fighterbomber == "f") weapons = "Weapons 1 1 1 1 1 1 0 0";
                if (fighterbomber == "h") weapons = "1 1 1 3"; //1X250kg bombs
            }
            if (fuel == 0) fuel = 80;

        }
        if (fuel == 0) fuel = 100; //fuel 0 means use DEFAULT fuel but if perchance the plane doesn't have a default setup the fuel will still be on 0
        k = "Weapons"; v = weapons; f.add(s, k, v);
        k = "Fuel"; v = fuel.ToString(); f.add(s, k, v);

        return f;
    }

    public void EscortMakeLand(AiAirGroup airGroup, AiAirGroup targetAirGroup = null, AiAirWayPointType aawpt = AiAirWayPointType.LANDING, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true, bool fromRepair = false)
    {
        try
        {
            if (airGroup == null || !coverAircraftAirGroupsActive.ContainsKey(airGroup)) return;
            try
            {
                //Console.WriteLine("EscortMakeLand: " + airGroup.Name(), coverAircraftActorsCheckedOut[airGroup as AiActor].Name());
            }
            catch (Exception ex) { }

            try
            {
                List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
                NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, AiAirWayPointType.NORMFLY));
				
				var landingWP = EscortLandingWaypoint(airGroup, targetAirGroup, AiAirWayPointType.LANDING, 0, 0, nodupe);
				
				var newWP = landingWP;
				newWP.P.x = landingWP.P.x + (ran.Next(0,1)*2-1)*ran.Next(5000,10000);
				newWP.P.y = landingWP.P.y + (ran.Next(0,1)*2-1)*ran.Next(5000,10000);
				(newWP as AiAirWayPoint).Action = AiAirWayPointType.NORMFLY;
				NewWaypoints.Add(newWP);
				
                NewWaypoints.Add(landingWP);
                airGroup.SetWay(NewWaypoints.ToArray());
                fixWayPoints(airGroup);
                airGroup.setTask(AiAirGroupTask.FLY_WAYPOINT, null); //try to force it . . . .
                Timeout(60, () =>
               {
                   //Console.WriteLine("Forcing LANDING: Current task: {0} " + airGroup.Name(), airGroup.getTask());
                   if (airGroup != null) airGroup.setTask(AiAirGroupTask.LANDING, null);
               });
                Timeout(120, () =>
                {
                    //Console.WriteLine("Forcing LANDING: Current task: {0} " + airGroup.Name(), airGroup.getTask());
                    if (airGroup != null) airGroup.setTask(AiAirGroupTask.LANDING, null);
                });

                //try several times to just de-spawn the aircraft if no live player is around
                //need to try several times in case there are players around, which prevents de-spawn
                for (int i = 600; i <= 1800; i += 100) Timeout(i, () =>
                  {
                      if (airGroup != null)
                      {
                          airGroup.setTask(AiAirGroupTask.LANDING, null);
                          if (mainmission.movebombtargetmission != null)
                              mainmission.movebombtargetmission.checkToDespawnOldAirgroups(airGroup);
                      }
                  });

            }
            catch (Exception ex)
            {
                Console.WriteLine("COVER: EscortMakeLand#1 LANDING ERROR! . . . but continuing to remove frome supply & cover a/c " + ex.ToString());
            }
                /*************************
                * 2021/06 - we are disabling this "early return to general stock" business in order to encourage ppl to fly their cover a/c 
                * back home and help them survive.
                * 
                * We will release back to BOTH general stock & cover usage, however, if they release over friendly land (not water, land).
                * */

                //Return aircraft to supply at this is the point when the player is not longer responsible for it
                // (if released over FRIENDLY TERRITORY and OVER LAND)
                // OR if coming from a <ferry or <repair mission

                bool onFriendlyTerritory = false;
            int terr = GamePlay.gpFrontArmy(airGroup.Pos().x, airGroup.Pos().y);

            if ((terr == 1 || terr == 2) && airGroup.getArmy() == terr) onFriendlyTerritory = true;

            if (airGroup.GetItems().Length > 0 && ((GamePlay.gpLandType(airGroup.Pos().x, airGroup.Pos().y) != LandTypes.WATER && onFriendlyTerritory)
                || fromRepair))
            {
                foreach (AiAircraft a in airGroup.GetItems())
                {

                    supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[a as AiActor], a as AiActor, 0, true, reason: "SAFE_cover_CoverAircraftReturnedOverFriendlyTerritory"); //true is softexit & forces return of plane even though it is in the air etc.
                    AiActor actor = a as AiActor;
                    if (actor == null) continue;

                    if (coverAircraftActorsCheckedOut.ContainsKey(actor))
                    {
                        Console.WriteLine("Cover - EscortMakeLand: " + airGroup.Name(), coverAircraftActorsCheckedOut[actor].Name());
                        numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                        coverAircraftActorsCheckedOut.Remove(actor);
                    }
                    if (fromRepair)
                    {
                        Timeout(10 + ran.Next(25), () =>
                        {
                            //for repair aircraft, just disappear them, to keep them from crashing, getting shot down etc
							mainmission.AircraftDestroyedList[a] = "SAFE_cover_repairDelivered";
                            (a as AiCart).Destroy();
                        });
                    }

                }
            }


            coverAircraftAirGroupsActive.Remove(airGroup);
            forgetAirGroup(airGroup);

            /*Timeout(240, () =>bomberway
            //Timeout(6, () =>  //for testing
            {
                    //Console.WriteLine("-cover Aborting LANDING: Sending off map now " + airGroup.Name(), airGroup.getTask());

            });
            */
        }
        catch (Exception ex) {
            Console.WriteLine("COVER: EscortMakeLand  ERROR! " + ex.ToString());
            //try to force check-in at minimum...
            if (airGroup != null) foreach (AiAircraft a in airGroup.GetItems())
            {

                if (supplymission != null) supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[a as AiActor], a as AiActor, 0, true, reason:"SAFE_cover_CoverAircraftReturnedSafely(butError)"); //true is softexit & forces return of plane even though it is in the air etc.
                AiActor actor = a as AiActor;
                if (actor == null) continue;

                if (coverAircraftActorsCheckedOut.ContainsKey(actor))
                {
                    Console.WriteLine("Cover - EscortMakeLand: " + airGroup.Name(), coverAircraftActorsCheckedOut[actor].Name());
                    numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                    coverAircraftActorsCheckedOut.Remove(actor);
                }

            }
        }
    }

    //returns false IF no bomber target waypoint was added - say it was targeting GROUND ENEMY
    //and none was found
    public bool BomberUpdateWaypoints(Player player, AiAirGroup airGroup, AiAirGroup targetAirGroup, Point3d newTargetPoint, AiAirWayPointType aawptstart = AiAirWayPointType.FOLLOW, AiAirWayPointType aawpttarget = AiAirWayPointType.FOLLOW, AiAirWayPointType aawptcontinue = AiAirWayPointType.FOLLOW, double altDiff_m = 20,
        double AltDiff_range_m = 50, bool nodupe = true, CoverAGOrders orders = CoverAGOrders.normal)
    {
        try
        {
            List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();

            Console.WriteLine("Bomberupdatewaypoints");

            Tuple<AiAirWayPoint, AiAirWayPoint, double, bool, bool> aaPs = BomberPosWaypoint(player, airGroup, targetAirGroup, newTargetPoint, aawpttarget, aawptcontinue, altDiff_m, AltDiff_range_m, nodupe, orders: orders);
            bool noGroundTargetFound = aaPs.Item4;
            
            bool attackContinuing = aaPs.Item5;
            if (attackContinuing) return true;

            if (noGroundTargetFound)
            {
                if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);   
                return false; 
            }
            AiAirWayPoint aawp33 = CurrentPosWaypoint(airGroup, targetAirGroup, aawptstart, aaPs.Item3);
            //2026-09-29, experimental, just not sending the currentpos waypoint at all
            if (aawp33 != null) NewWaypoints.Add(aawp33);
            NewWaypoints.Add(aaPs.Item1);
            if (aaPs.Item2 != null) NewWaypoints.Add(aaPs.Item2);
            airGroup.SetWay(NewWaypoints.ToArray());
            return true;
        }
        catch (Exception ex) { Console.WriteLine("Cover BomberUpdateWaypoints() ERROR: " + ex.ToString());  return false; }
    }

    Dictionary<AiAirGroup, AiActor> airgroupTargets = new Dictionary<AiAirGroup, AiActor>(); //any ground actor this airgroup is targeting at the moment
    Dictionary<AiAirGroup, GroundStationary> airgroupGroundTargets = new Dictionary<AiAirGroup, GroundStationary>(); //any ground stationary the airgroup is targeting at the  moment
    Dictionary<AiAirGroup, Point3d> airgroupTargetPoints = new Dictionary<AiAirGroup, Point3d>(); //the point the airgroup is targeting at the moment
    Dictionary<Point3d, DateTime> targetPointNoEnemiesFound_time = new Dictionary<Point3d, DateTime>();
    //AARGH - all these three should be in one dictionary or whatever instead of 3, right?

    //Dictionary<AiAirGroup, GroundStationary> airgroupTargets = new Dictionary<AiAirGroup, GroundStationary>();

    public Tuple<AiAirWayPoint, AiAirWayPoint, double, bool, bool> BomberPosWaypoint(Player player, AiAirGroup airGroup, AiAirGroup playerAirGroup, Point3d newTargetPoint, AiAirWayPointType aawpttarget = AiAirWayPointType.FOLLOW, AiAirWayPointType aawptcontinue = AiAirWayPointType.FOLLOW, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true, CoverAGOrders orders = CoverAGOrders.normal)
    {
		//try
        //{
            if (GamePlay == null || airGroup == null) { Console.WriteLine("Cover: exiting BomberPosWaypoint; airGroup is NULL or GamePlay is NULL, no reason to continue"); return new Tuple<AiAirWayPoint, AiAirWayPoint, double, bool, bool>(null, null, 0, false, false); }
            Console.WriteLine("CBCW: Bomb Aim Mode: {0}", BAM_getPlayerBombAimMode_string(player));
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("MPWXX1 " + DateTime.UtcNow.ToString("HH:mm:ss.fffffff"));
            bool tempFlakTarget = false;
            double changeL_XY_m = 100;
            AiAirWayPoint aaWP = null;
            Vector3d Vwld = new Vector3d(0, 0, 0);
            if (playerAirGroup != null) Vwld = playerAirGroup.Vwld();
            //Point3d Vwld2 = storedRollingAverage(player, airGroup, "vwld", Vwld, 2); //rolling average of last 2 positions, used for direction & speed
            Point3d Vwld2 = new Point3d(Vwld.x, Vwld.y, Vwld.z);
            Point3d Vwld5 = storedRollingAverage(player, airGroup, "vwld", Vwld, 3); //rolling average of last 5 positions, used for climb rate/vertical speed. Trying just last 3 average instead of 5.
                                                                                     //if (Vwld)

            double target_vel_mps = CoverCalcs.CalculatePointDistance(Vwld2); //All position Point3ds in game are in meters.
            bool heavyBomber = isHeavyBomber(airGroup) || isDiveBomber(airGroup);

            bool isSturmovik = Calcs.isStrikeAC(airGroup);
            bool isJU87 = isDiveBomber(airGroup);


            Point3d playerAirGroupPos = airGroup.Pos();
            if (playerAirGroup != null) playerAirGroupPos = playerAirGroup.Pos();

            //<cfdist Option A, bomber path: the bomb AIMPOINT stays exactly as it was (BomberPosWaypoint
            //targets newTargetPoint below and must not be shifted), but the run-in speed law measured
            //"distance/angle to the leader" against the raw player position, so <cfdist did nothing on
            //this path either.  Hand it the same virtual leader point.  playerAirGroup can be null (no
            //live player group) - then leaderRef stays null and the call behaves exactly as before.
            Point3d? leaderRef = null;
            if (playerAirGroup != null) leaderRef = addFrontBackOffset(playerAirGroupPos, Vwld, getFrontBackDist(player));
            double targetDist_m = CoverCalcs.CalculatePointDistance(airGroup.Pos(), (leaderRef.HasValue ? leaderRef.Value : playerAirGroupPos));

            Tuple<double, double> ret = calcCoverSpeedToMatchMain(airGroup, playerAirGroup, Vwld, target_vel_mps, targetDist_m, heavyBomber, isSturmovik, true, orders, aawpttarget, player, leaderRef);
            double vel_mps = ret.Item1;
            double angleTargetToGroup = ret.Item2;

            //Console.WriteLine( "Updating, current TASK: {0}", new object[] { airGroup.getTask() });
            //Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
            //Point3d pos = airGroup.Pos();
            //So we can't just return NO point so if there is no target point we return
            //a random point 20000-40000km away.
            if (newTargetPoint.x == -1 && newTargetPoint.y == -1)
            {
                newTargetPoint.x = airGroup.Pos().x + (20000 + ran.NextDouble() * 20000) * (ran.Next(2) * 2 - 1);
                newTargetPoint.y = airGroup.Pos().y + (20000 + ran.NextDouble() * 20000) * (ran.Next(2) * 2 - 1);
            }

            Point3d pos = newTargetPoint;

            Console.WriteLine(String.Format("CBCW: newtargetpoint {0:F0} {1:F0} {2:F0}  ", pos.x, pos.y, pos.z));
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("MPWXX2 " + DateTime.UtcNow.ToString("HH:mm:ss.fffffff"));
            Tuple<Point3d?, double> obj_cr = ObjectivesRadius_m(pos); //FIND the TUPLE center point, radius of any objective this point is in.
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("MPWXX3 " + DateTime.UtcNow.ToString("HH:mm:ss.fffffff"));

            double distToNearestACTOR_m = 1000000;
            double distToNearestGROUND_m = 1000000;
            double distToNearestCHOSEN_m = 1000000;

            double Obj_radius = 0;
            Point3d Obj_pos = new Point3d(-1, -1, -1);

            double randPointSearchRadius = 60;
            if (obj_cr.Item1.HasValue)
            {
                Obj_radius = obj_cr.Item2;
                Obj_pos = obj_cr.Item1.Value;

                //We'll pick a point within 1/2 the distance of the target objective radius of the target point the player has given us via knickebein, IF the knickebein point is within some identified target objective.
                //This will be nicely within the target radius if the knickebein point is exactly aligned with the center of the objective, and hopefully not too far off otherwise     

                //so, if the given point isn't exactly in the center of the objective circle, we will reduce our search radius
                //to ensure that the final point is always within the objective circle
                double distPosToCenter = CoverCalcs.CalculatePointDistance(Obj_pos, pos);
                randPointSearchRadius = Obj_radius/10;
                if (randPointSearchRadius > 125) randPointSearchRadius = 125;
                //if (distPosToCenter < Obj_radius) searchRadius = Obj_radius - distPosToCenter;
                if (randPointSearchRadius < 20) randPointSearchRadius = 20; //But, sometimes we are not aiming exactly at the center of the target (wind, etc), and all targets are AT LEAST 80-100m radius.  So we can always go with a 100m radius at least. (epsecially since we are searching for just 100/2 in reality.

                Console.WriteLine(String.Format("CBCW: target point was within an objective. RandPointSearchradius: {3:F0} ({0:F0} {1:F0} {2:F0})", pos.x, pos.y, pos.z, randPointSearchRadius));

            }

            float shiftFactor = getShiftFactor(player);
            randPointSearchRadius *= shiftFactor;
            //Some linear algebra magic . . . 
            double dist = CoverCalcs.CalculatePointDistance(pos, airGroup.Pos());
            Point3d unit_vec_AGtoTarg = new Point3d((pos.x - airGroup.Pos().x) / dist, (pos.y - airGroup.Pos().y) / dist, 0);
            Point3d unit_vec_perpAGtoTarg = new Point3d(-unit_vec_AGtoTarg.y, unit_vec_AGtoTarg.x, 0);
            double randadd = ran.NextDouble() * randPointSearchRadius - randPointSearchRadius / 2;
            double backup = -ran.NextDouble() * randPointSearchRadius / 4;
            Point3d newPoint = new Point3d(pos.x + backup * unit_vec_AGtoTarg.x + randadd * unit_vec_perpAGtoTarg.x, pos.y + backup * unit_vec_AGtoTarg.y + randadd * unit_vec_perpAGtoTarg.y, pos.z);
            //We could check if the point is on water & move it if so?  But then what if it is a ship?
            pos = newPoint;

            Console.WriteLine(String.Format("CBCW: newtargetpoint after randomizing with RandPointsearchradius {3:F0}: {0:F0} {1:F0} {2:F0}  ", pos.x, pos.y, pos.z, randPointSearchRadius));
            

            //pos.z = playerAirGroup.Pos().z; //this was the first plan - just match the player's current altitude (at the target point - which is the only point in this AAWP
            pos.z = airGroup.Pos().z; //this is the new plan - if close to the target, just keep the current airgroup position, ie, fly flat & level.
                                      //if further from the target, fly a rate of climb/dive to put them in the right altitude relative to the main a/c at the target point, if the main a/c keeps its current climb/dive rate.  Main calculations below



            //GroundStationary newTarget = null;
            AiActor newTarget = null;
            GroundStationary newGroundTarget = null;
            bool diveTarget = false; //ground actor target        
            bool stationaryDiveTarget = false; //stationary/static target
            //Choose another ground stationary somewhere within the given radius of change, starting with the GATTACK point since we don't have an actual GATTACK target actor; make sure it is alive if possible
            //Console.WriteLine("CBCW: bom,alt: {0} {1:F0}", isDiveBomber(airGroup), pos.z);

            if (isDiveBomber(airGroup) && pos.z >= 1600 + Calcs.LandElevation_m(pos)  
				|| BAM_isNearestEnemy(player))//only bother to do this search for dive bombers OR if we have specially requested as as "nearest enemy" type target
                                                                                        //Divebombs need an object to glom onto to do their dive, they also need to start above 2000m or so altitude (let's say 1600, that gives 1000 meters to aim & drop as usually set up; right now only JU87 can do dive bombing but many can target ground actors/stationaries somehow.
            {
                //Console.WriteLine("TARGETING BY ACTOR!@!!!11!!!");
                //was 200/100 for dive bombers & 3500/75 if BAM_nearestenemy . . .switching both to 2000/250
                double maxMove_m = 3500;
                double preferredMove_m = 200;

                if (obj_cr.Item1.HasValue) //we're trying to keep it within the radius of a MIssion Objective, if it is in/near one.
                {
                    maxMove_m = Obj_radius * 3;
                    preferredMove_m = 1.2 * Obj_radius;
                }
                if (maxMove_m < 3500) maxMove_m = 3500;

                Console.WriteLine("CBCW: before MaxMove {0:N0} preferredMove {1:N0}", maxMove_m, preferredMove_m);
                //allow <cdist to affect the spread of target selection
                //float shiftFact = getShiftFactor(player);
                if (shiftFactor<1) {
                    maxMove_m *= shiftFactor;
                    preferredMove_m *= shiftFactor;
                } else {
                    double fact1 = Math.Log(shiftFactor) + 1;
                    maxMove_m *= fact1;
                    preferredMove_m *= fact1;
                }
                Console.WriteLine("CBCW: after MaxMove {0:N0} preferredMove {1:N0}", maxMove_m, preferredMove_m);


                
                var mmtlm = mainmission.threadloadmission;
                int panic = 0;
                if (mmtlm.recentCPUPercent > 98 || mmtlm.rollingAverageCPUPercent > 95) panic = 3;
                else if (mmtlm.recentCPUPercent > 95 || mmtlm.rollingAverageCPUPercent > 90) panic = 2;
                else if (mmtlm.recentCPUPercent > 90 || mmtlm.rollingAverageCPUPercent > 85) panic = 1;
                //if (mainmission.ON_TESTSERVER) Console.WriteLine("MPWXX4 " + DateTime.UtcNow.ToString("HH:mm:ss.fffffff"));
                //if the dropped bomb/flare is in an objective radius, then we try to 
                //pick objects in OR near that objective
                //CLOD special - TOBRUK is different
                /*
                if (BAM_isNearestEnemy(player) && obj_cr.Item1.HasValue )
                {
                    maxMove_m = Obj_radius*1.4;
                    preferredMove_m = Obj_radius*0.7;

                }
                */

                /*
                else if (BAM_isNearestEnemy(player))  //further distances allowed for player-requested "nearest enemy" targets vs dive bomb, and if the point isn't in an objective, we ignore that part
                {
                    maxMove_m = 3500;
                    preferredMove_m = 75;
                }
                */

                //OK, now we are going to get a new target if the task is RETURN or UNKNOWN
                AiAirGroupTask task = airGroup.getTask();
                //if currWay is 0 it is basically lost...needs new instructions
                int currWay = airGroup.GetCurrentWayPoint();

                //Console.WriteLine("Choosing target for dive bomber/player targeted enemy, maxMove {0:F0}, preferredMove {1:F0}", maxMove_m, preferredMove_m);

                if (mainmission.ON_TESTSERVER) Console.WriteLine("CBCW: Deciding whether to keep existing point. Have a point: {0} task: {1} currway: {2}", airgroupTargetPoints.ContainsKey(airGroup),  task, currWay);

                if (airgroupTargetPoints.ContainsKey(airGroup) && airgroupTargetPoints[airGroup].x != -1 && airgroupTargetPoints[airGroup].y != -1 && task != AiAirGroupTask.RETURN && task != AiAirGroupTask.UNKNOWN && currWay < 2) //x,y == -1,-1 means we're actually not targeted at anything.  currWay == 2 means we have already completed/passed the attack point and are not continuing.
                {
                    
                    var oldApos = airgroupTargetPoints[airGroup];

                    if (mainmission.ON_TESTSERVER) Console.WriteLine("CBCW: Deciding whether to keep existing point. Have a point: {0:n0} {1:n0} distance: {2:n0}", oldApos.x, oldApos.y, CoverCalcs.CalculatePointDistance(oldApos, newTargetPoint));

                    if (CoverCalcs.CalculatePointDistance(oldApos, newTargetPoint) <= maxMove_m && ran.Next(10 + 3*panic) > 0 && (airGroup.hasBombs() || ! CoverCalcs.areCratersBuildingsFactoriesNear(mainmission, oldApos, 250, airGroup))) //if old target it still good, stick with it most of the time.  Delay is 16 seconds, better if we could make this change relative to delay.  But it will choose a new target about every 10*16 seconds.  And if there are craters/buildings near it, skip sooner.  The ground attack planes just attack them above all else
                    {
                        
                        if (mainmission.ON_TESTSERVER) Console.WriteLine("reusing old ground target");
                        if (airgroupTargets.ContainsKey(airGroup) && airgroupTargets[airGroup] != null && airgroupTargets[airGroup].IsAlive()) {
                            newTarget = airgroupTargets[airGroup];
                            diveTarget = true;
                            stationaryDiveTarget = true;
                            return new Tuple<AiAirWayPoint, AiAirWayPoint, double, bool, bool>(null, null, 0, false, true); //final TRUE means we're going to keep the old target point, we're still in the middle of the attack, just keep on keeping on.
                        }
                        else if (airgroupGroundTargets.ContainsKey(airGroup) && airgroupGroundTargets[airGroup] != null && airgroupGroundTargets[airGroup].IsAlive)
                        {
                            stationaryDiveTarget = true;
                            diveTarget = true;
                            newGroundTarget = airgroupGroundTargets[airGroup];
                            return new Tuple<AiAirWayPoint, AiAirWayPoint, double, bool, bool>(null, null, 0, false, true); //final TRUE means we're going to keep the old target point, we're still in the middle of the attack, just keep on keeping on.
                        }
                        else
                        {
                            diveTarget = false;
                            stationaryDiveTarget = false;
                            airgroupTargetPoints[airGroup] = new Point3d(-1,-1,-1);

                            //also remove any old targets 
                            if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                            if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                
                            if (mainmission.ON_TESTSERVER) Console.WriteLine("old ground target bad, couldn't find actor OR stationary, not using it after all actor : {0} stationary: {1}", airgroupTargets.ContainsKey(airGroup) && airgroupTargets[airGroup] != null, airgroupGroundTargets.ContainsKey(airGroup) && airgroupGroundTargets[airGroup] != null );
                        }


                    }
                }

                //We're getting new target points now, so remove any old targets
                //otherwise they can interfere with e.g. counts of how many a/g are
                //targeting a certain objective or stationary or groundactor
                if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);

                string groundType = "";


                //This gets all static ACTORs such as (?) ships, artillery. (?).  There is no way to get this full list from CloD that I know of.
                maddox.game.LandTypes landType = GamePlay.gpLandType(airGroup.Pos().x, airGroup.Pos().y);
                double closest_m = 2 * maxMove_m;
                
                if (!diveTarget)
                {
                    Console.WriteLine("CBCW: Trying to find a ground actor near {0:n0}/{1:n0}", pos.x, pos.y);

                    var currTime = DateTime.UtcNow;

                    //Console.WriteLine("CBCW: contains: {0} currtime {1} savetime {2}", targetPointNoEnemiesFound_time.Keys.Contains(newTargetPoint), currTime, targetPointNoEnemiesFound_time.Keys.Contains(newTargetPoint) ? 0 : targetPointNoEnemiesFound_time[newTargetPoint]);
                    //if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX1 " + DateTime.UtcNow.ToString("T.fffffff"));

                    //If this area has been searched for enemies & none found, don't keep doing
                    //it repeatedly again (CPU hog).  Will stop ALL cover groups from
                    //re-searching the same area, except one can do it each one minute.
                    
                    if (!targetPointNoEnemiesFound_time.Keys.Contains(newTargetPoint) ||
                        currTime.Subtract(targetPointNoEnemiesFound_time[newTargetPoint]).TotalSeconds > 15 ) 
                    {
                        try
                        {

                            if (allStaticActors != null)
                            {
                                if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX1A " + DateTime.UtcNow.ToString("T.fffffff"));
                                var asa = new List<AiActor>();
                                lock (allStaticActors_lock)
                                {
                                    if (allStaticActors != null) asa = new List<AiActor>(allStaticActors);
                                }
                                if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX1B " + DateTime.UtcNow.ToString("T.fffffff"));
                                int p = 0;
                                if (panic > 0) p = panic + 2;
                                int numsteps = 6 - p;


                                double step = (maxMove_m - preferredMove_m) / numsteps;
                                double origStep = step;

                                for (int d = 0; d <= numsteps; d++)
                                {
                                    if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX2 " + DateTime.UtcNow.ToString("T.fffffff"));
                                    if (asa == null || asa.Count == 0)
                                    {
                                        renewAllStaticActors_recurs(onetime: true);
                                    }
                                    //if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX2A " + DateTime.UtcNow.ToString("T.fffffff"));
                                    List<AiActor> closeStaticActors = new List<AiActor>(CoverCalcs.gpGetAllGroundActorsNear(asa.ToArray(), pos, preferredMove_m + d * step).ToList()); //1000?
                                    if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX2B " + DateTime.UtcNow.ToString("T.fffffff"));                                                                                                                                                   //Finding actors we're going to range wider 1500. meters IN reality maybe we could look up the objective radius.  But actors nearby will be flak, etc etc etc.  All helpful.

                                    if (closeStaticActors == null || closeStaticActors.Count == 0)
                                    {
                                        Console.WriteLine("CBCW: Ground actor not found at distance {0:N0}", preferredMove_m + d * step);
                                        continue;
                                    }
                                    Console.WriteLine("CBCW: Ground actor checking {1} possible ground actors at distance {0:N0}", preferredMove_m + d * step, closeStaticActors.Count);
                                    CoverCalcs.Shuffle(closeStaticActors);

                                    //double closest_m = 2 * maxMove_m;
                                    AiActor bestAct = null;
                                    foreach (AiActor act in closeStaticActors)
                                    {
                                        if (act == null || act.Army() == airGroup.getArmy()) continue;
                                        double dist_m = CoverCalcs.CalculatePointDistance(pos, act.Pos());
                                        Console.WriteLine(act.Name() + " " + dist_m.ToString("N0"));
                                        groundType = "";
                                        if (act as AiGroundActor != null) groundType = (act as AiGroundActor).Type().ToString();
                                        //newTarget = act; //arrgh, don't set it here, only after an actual target is found

                                        int numToTargetOne = 1;
                                        if (groundType.ToLower().Contains("ship") && landType == maddox.game.LandTypes.WATER )
                                        {
                                            //step = origStep * 1 .5; // if ships in the neighborhood we search a bit wider, they are very spread out.
                                            numToTargetOne = 2; //for ships, we can have two bombers target same ship. Myabe even 3-4?
                                        }

                                        if (act.Name().ToLower().Contains("chief")) numToTargetOne = 3;

                                        Console.WriteLine("CBCW: Keep this one? {0} IsAlive: {1} isAIgroundactor: {2} numalreadytargetingit: {3} maxnumToTargetit: {4} ", act.Name(), act.IsAlive(), act as AiGroundActor != null, airgroupTargets.Values.Count(x => x == act), numToTargetOne);

                                        //if (dist_m < closest_m && act.IsAlive() && act as AiGroundActor != null)
                                        if (act.IsAlive() && act as AiGroundActor != null && airgroupTargets.Values.Count(x => x == act) < numToTargetOne )                                        
                                        {
                                            //
                                            distToNearestACTOR_m = distToNearestAirgroupTargetPoint(act.Pos(),  act); // adding ,act allows > 1 target for this exact actor, but more than numToTargetOne is disallowed above in the if statement

                                            Console.WriteLine(act.Name() + " distNactor: {0:N0} distLIMIT: {1:N0} reject: {2} numToTarget: {3}",distToNearestACTOR_m, 500.0 - 500.0 * d / (double)numsteps, distToNearestACTOR_m <= 500.0 - 500.0 * d / (double)numsteps, numToTargetOne );
                                            
                                            //groundType = "";

                                            if (!airGroup.hasBombs() && distToNearestACTOR_m <= 500.0 - 500.0 * (double)d / (double)numsteps ) continue; //can't have any targets within abt 500m of each other because due to CLoD, they will all just switch attack the SAME target instead of different ones.

                                            if (!airGroup.hasBombs() && CoverCalcs.areCratersBuildingsFactoriesNear(mainmission, act.Pos(), 500.0 - 500.0 * (double)d / numsteps, airGroup )) continue; //the ai ground attackers will attack craters (even if only NEAR the target) so trying to avoid that. Can't hit things if they're inside a building. Also they randomly choose a "thing" nearby the point, not necessary the one we specify.  So we try to avoid all such AREAS not just pick one GG or act that is OK.  Because the ground attacker will just switch targets to a bad one, invariably, if available.

                                            closest_m = dist_m;
                                            bestAct = act;
                                            diveTarget = true;
                                            newTarget = act;
                                            Console.WriteLine("CBCW: FOUND a ground actor" + newTarget.Name() + " " + groundType + " dist from nearest targeted: {0:n0}", distToNearestACTOR_m);

                                            if (act.Name().ToLower().Contains("_tflak_")) {
                                                List<GroundStationary> stationaries = GamePlay.gpGroundStationarys(act.Pos().x, act.Pos().y,25).ToList();
                                                if (stationaries.Count > 0 ) {
                                                    CoverCalcs.Shuffle(stationaries);
                                                    newGroundTarget = stationaries[0];
                                                    newTarget = null;
                                                    tempFlakTarget = true;
                                                    Console.WriteLine("CBCW: FOUND tflak - using nearby stationary instead" + newGroundTarget.Title + " " + newGroundTarget.Name + " dist from nearest targeted: {0:n0}", distToNearestACTOR_m);
                                                    break;

                                                } else continue;
                                                

                                            }

                                            


                                            break;
                                            /*
                                            if (dist_m < preferredMove_m)
                                            {
                                                //Console.WriteLine("CBCW: FOUND a ground actor within preferredMove - breaking");                                        
                                                Console.WriteLine("CBCW: FOUND a ground actor for cover target: " + newTarget.Name() + " " + groundType + " " + closest_m.ToString("N0") + " ({0:N0}, {1:N1}) ", act.Pos().x, act.Pos().y);
                                                break;
                                            }
                                            */

                                        }

                                    }
                                    if (diveTarget) break;
                                }


                            }
                            //if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX3 " + DateTime.UtcNow.ToString("T.fffffff"));
                        }
                        catch (Exception ex) { Console.WriteLine("Bomb select #1 ERROR: " + ex.ToString()); }


                        bool isAA = false;
                        if (tempFlakTarget || groundType == "AAGun" || groundType == "Artillery") isAA = true;


                        //THIS gets all the remaining stationaries that are NOT actors, such as jerrycans or static trucks , planes, whatever. Scenery.
                        //Here, we're going more for the center of the target. Again we COULD/SHOULD look up the actual radius of the objective.
                        //do this if #1. We haven't found an actor target, #2. It isn't with the preferred mo #3. Sometimes randomly just for variety
                        int ml = 0;
                        
                        if (!tempFlakTarget && (
                                !diveTarget  || (Obj_radius > 0 && closest_m > Obj_radius * 1.2) || closest_m > maxMove_m /2.0 || (ran.Next(3) == 0)
                                || (ran.Next(2) == 0 && isAA))  //one time in 3, choose a ground stationary instead of an actor, even if the actor was found; make it one in two if it is AA.
                        )

                        Console.WriteLine("CBCW: Trying to find a ground stationary near {0:n0}/{1:n0}", pos.x, pos.y);

                        // && !BAM_isNearestEnemy(player)
                        //(but only for auto dive bomber; never for live player choosing "nearest enemy")
                        {
                            //prevent cpu hog
                            
                            
                            int steps = 7;
                            if (mmtlm.recentCPUPercent > 98 || mmtlm.rollingAverageCPUPercent > 95) steps = 1;
                            else if (mmtlm.recentCPUPercent > 95 || mmtlm.rollingAverageCPUPercent > 90) steps = 3;
                            else if (mmtlm.recentCPUPercent > 90 || mmtlm.rollingAverageCPUPercent > 85) steps = 4;

                            double step = (maxMove_m - preferredMove_m) / steps;
                            for (int d = 0; d <= steps; d++)
                            {

                                //if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX4 " + DateTime.UtcNow.ToString("T.fffffff"));
                                Console.WriteLine("CBCW: Trying to find a ground stationary");
                                List<GroundStationary> stationaries = GamePlay.gpGroundStationarys(pos.x, pos.y, preferredMove_m + d * step).ToList();
                                //foreach (GroundStationary s in stationaries) Console.WriteLine("List:" + s.Name + " " + s.Title + " " + s.Type);
                                Console.WriteLine("CBCW: Looking for nearby stationary at {0}m, found {1} stationaries", preferredMove_m + d * step, stationaries.Count);
                                int numToCheck = 30;
                                if (stationaries.Count < numToCheck) numToCheck = stationaries.Count;
								
								int timesThru = (d * 3) / steps;
								
								for (int j = timesThru; j>=0; j--) { //run through this three times, looking for better then worse targets
									CoverCalcs.Shuffle(stationaries);
									for (int i = 0; i < numToCheck; i++)
									{
										try
										{
											if (stationaries.Count == 0) break;
											//int newStaIndex = ran.Next(stationaries.Length - 1);
											var gg = stationaries[i];
											//if (gg != null && gg.IsAlive && (newTarget == null ||
											//    (Math.Pow(gg.pos.x - pos.x, 2) + Math.Pow(gg.pos.y - pos.y, 2) <
											//    Math.Pow(newTarget.Pos().x - pos.x, 2) + Math.Pow(newTarget.Pos().y - pos.y, 2)))) 

											//Figuring out "army" of stationaries if not so easy.  If they have an army "gb" or "de" we go with that.  If set to "nn" however we go with
											//whatever TERRITORY they are on.  If they are in neutral territory I guess they are neutral?!
											//UPDATE: .country is just the DEFAULT country for an object, as shown in e.g. FMB
											//Now we have a ConcurrentDictionary with actual country/ army.
											int statArmy = 0;
											if (gg != null && gg.IsAlive)
											{
												/*if (gg.country == "de") statArmy = 2;
												else if (gg.country == "gb") statArmy = 1;
												else { statArmy = GamePlay.gpFrontArmy(gg.pos.x, gg.pos.y); } */
												
												int ggarmy = -1;
												string ggctry = "";
												string cleanName = Calcs.CleanStationaryID(gg.Name);
												if (mainmission.GroundStationary_army.ContainsKey(cleanName)) {
													ggarmy = mainmission.GroundStationary_army[cleanName].Army;
													ggctry = mainmission.GroundStationary_army[cleanName].Country;
												}
											
												if (ggarmy != -1 && ggarmy != 0) statArmy = ggarmy;                                            
												else { statArmy = GamePlay.gpFrontArmy(gg.pos.x, gg.pos.y); }
											}

                                            


											if (gg != null && gg.IsAlive && !airgroupGroundTargets.ContainsValue(gg)
											&& statArmy == 3 - airGroup.getArmy() && !gg.Title.ToLower().Contains("crater")
											&& !gg.Title.ToLower().Contains("smoke") && !gg.Title.ToLower().Contains("fire") && !gg.Title.ToLower().Contains("_dmg")  && !gg.Title.ToLower().Contains("AEC_Regent_II")  && !gg.Title.ToLower().Contains("MG_TA")  //avoid choosing a smoke, fire, or crater, or damaged object to attack. There might be some other types to avoid, too but these are the main offenders.
											) //not trying to find the closest, just a random one within the given distance, and not already picked by another airgroup && enemy
											  //Names including _DMG are damaged items, don't need to target them.  IE Stationary.Environment.Ladder_UK1_DMG1
											  //We use things like this in our detritus fields, which mean target is destroyed or moved
											  //distToNearestAirgroupTargetPoint is trying to choose targets >500-600m from any others. If possible

                                              //GROUND ATTACK BEHAVIOR noted by experiment:
                                              //Must be type GATTACK_TARG not GATTACK_POINT
                                              //GATTACK_POINT only works for aerial bombing, NOT ground attack/strafing/dive bombing
                                              //IF TARGET GIVEN IT MUST BE AN ACTOR (not GROUNDSTATIONARY)
                                              //If no ACTOR, just specifying the point is just as good (Point3d of AiAirWaypoint with task GATTACK_TARG )
                                              //AirGroup.ChangeGoalTarget(AiActor) seems to do the same thing, maybe.  But must be ACTOR not STATIONARY and can't also set the point.
                                              //Either way, it doesn't actually attack just that ACTOR:
                                              // - will attack ANY groundactor OR stationary in the area
                                              // - all aircraft attacking in a small area (ca. 500m radius)
                                              // will ALL attack THE SAME object, either an groundactor or
                                              // stationary
                                              // >500m apart, or so, they will attack different targets
                                              // - As one stationary or actor is killed, they will move
                                              // on to the next in the immediate area
                                              // They seem to go for groundactors esp. moving vehicles first, maybe aa/artillery next, stationaries last
                                              // This is a PROBLEM if they are all stuck shooting a certain stationary that is e.g. inside a building and never gets killed
                                              // - For that reason, above algo TRIES to find different targets > 500m apart for each airgroup

										
											  

											{
                                                distToNearestGROUND_m = distToNearestAirgroupTargetPoint(gg.pos);
                                                if (!airGroup.hasBombs() && distToNearestGROUND_m <= 600.0 - 600.0 * (double)d / (double)steps) continue; //trying to avoid having AI all attack the same area (even if technically different targets, they will all switch and just attack the same one, due to CLoD)

                                                if (!airGroup.hasBombs() && CoverCalcs.areCratersBuildingsFactoriesNear(mainmission, gg.pos, 400.0 - 400.0 * (double)d / (double)steps, airGroup)) continue; //the ai ground attackers will attack craters (even if only NEAR the target) so trying to avoid that.

												bool bk = false;
												string types = (gg.Title + gg.Type.ToString() + gg.Category).ToLower(); 
												string category = gg.Category;
												
												
												if (gg.Category == "Aircraft" || types.Contains("aircraft")) ml = 3;
												else if (gg.Category == "Car") ml = 2;
												else if (gg.Category == "ArmoredCar") ml = 3;
												else if (gg.Category == "Tank") ml = 3;
                                                else if (types.Contains("ammo")) ml = 3; //associated with AA nests...
                                                else if (types.Contains("bofors")) ml = 3; //associated with AA nests...
                                                else if (types.Contains("flak37")) ml = 3; //associated with AA nests...
                                                else if (types.Contains("pdr_mk")) ml = 3; //associated with AA nests...
												else if (types.Contains("ship")) ml = 3;
												else if (types.Contains("plane") || types.Contains("aagun") 
													|| types.Contains("artillery")) ml = 2;
												else if (types.Contains("tractor") || types.Contains("spg") 
													|| types.Contains("truck") || types.Contains("trailer")
													|| types.Contains("amphibian")) ml = 2;
												else if (types.Contains("radar") || types.Contains("radio")) ml = 3;
												else if (types.Contains("tent")) ml = 1;
												else if (types.Contains("camonet")) ml = 2;
												else if (types.Contains("hangar")) ml = 2;
												//else if (types.Contains("fuel") || types.Contains("ammo")) ml = 1;
												//else if (types.Contains("weapons_")) ml = 1;	
												
												//At first onlyh accept score 3.
												//Then...2.  Then...1.  Then finally 0;
												int accept = (3 - timesThru - j).Clamp(1,3);
												if (d == steps && j == 0) accept = 0;

												Console.WriteLine("CBCW: Checkingstationary for target: " + gg.Name + " 1 " + gg.Title + " 2 " + gg.Type + " | {0:F0} {1:F0} - {2:F0} {3:F0} : score {4} && required {5} distToNearest {6:N0}", gg.pos.x, gg.pos.y, newTargetPoint.x, newTargetPoint.y, ml, accept, distToNearestGROUND_m); //
												
												if (ml < accept ) continue;
												
												newGroundTarget = gg;
												Console.WriteLine("CBCW: Found a stationary for target: " + gg.Name + " 1 " + gg.Title + " 2 " + gg.Type + " | {0:F0} {1:F0} - {2:F0} {3:F0} distToNearest: {4:N0}", gg.pos.x, gg.pos.y, newTargetPoint.x, newTargetPoint.y, distToNearestGROUND_m); // + " " + newTarget.Pos().x.ToString());
																																																																																										  //if (ran.Next(5) < 3) continue; //trying to get more of list for testing
												stationaryDiveTarget = true;
												break;
											}
										}
										catch (Exception ex) { Console.WriteLine("Bomb select #2 ERROR: " + ex.ToString()); }

									}
								}
                                if (stationaryDiveTarget) break;
                            }
                        }

                        //If the found gg was low quality and the ACTOR was pretty good, we can stick with the ACTOR instead
                        if (stationaryDiveTarget && newTarget != null && ml < 2 && closest_m < maxMove_m / 2.0) newGroundTarget = null;
                    

                        //if we FOUND a target we reset targetPointNoEnemiesFound_time so others can also search
                        //if we DID NOT FIND then we set it so no others will waste CPU searching for 60 seconds
                        if (diveTarget || stationaryDiveTarget)
                        {
                            if (targetPointNoEnemiesFound_time.Keys.Contains(newTargetPoint))
                                targetPointNoEnemiesFound_time.Remove(newTargetPoint);
                        }
                        else targetPointNoEnemiesFound_time[newTargetPoint] = currTime;
                    }
                }


                if (!diveTarget && !stationaryDiveTarget) Console.WriteLine("CBCW: Didn't find Actor or Stationary for target, going to NORMFLY instead");

            }
            //if (mainmission.ON_TESTSERVER) Console.WriteLine("MBTXX5 " + DateTime.UtcNow.ToString("T.fffffff"));
            Point3d newPos = pos;
            Point3d savePos = pos;


            //Use the position of the newly found ground actor as the new attack position, IF the actor exists/was found
            if (diveTarget || stationaryDiveTarget)
            {
                if (diveTarget && newTarget != null)
                {
                    distToNearestCHOSEN_m = distToNearestACTOR_m;
                    newPos.x = newTarget.Pos().x;
                    newPos.y = newTarget.Pos().y;
                    savePos = newPos;
                    Console.WriteLine("CBCW: Found a groundactor, updating attack position");
                   
                }
                else if ( stationaryDiveTarget && newGroundTarget != null)
                {
                    distToNearestCHOSEN_m = distToNearestGROUND_m;
                    newPos.x = newGroundTarget.pos.x;
                    newPos.y = newGroundTarget.pos.y;
                    Console.WriteLine("CBCW: Found a ground stationary, updating attack position");
                }

                airgroupTargets[airGroup] = newTarget;
                airgroupGroundTargets[airGroup] = newGroundTarget;
                airgroupTargetPoints[airGroup] = newPos;
                //AARGGHHH

                //sooo . . . if the final distance from other targets is still too small, the
                //a/c will just target the same ACTOR or GG anyway.  So we will just ADD
                //Something to it to get some separation.  Hopefully.  
                //Just moving in a random direction for now, could move opposite the nearest point
                //or whatever.  Or by more distance.
                double addDist=0;
                double addAngle=0;
                if (!tempFlakTarget && distToNearestCHOSEN_m < 500) {
                    addDist = 500 - distToNearestCHOSEN_m + 200;
                    addAngle = ran.NextDouble() * Math.PI * 2;
                } 
                newPos.x += addDist * Math.Cos(addAngle);
                newPos.y += addDist * Math.Sin(addAngle);
            }
            //3rd approach, just set it to the actual x,y
            else
            {
                //Console.WriteLine("CBCW: No stationary found, updating attack position");
                newPos.x = pos.x;
                newPos.y = pos.y;
                if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);
                

            }

            Console.WriteLine(String.Format("CBCW: newtargetpoint of currTarget: {0:F0} {1:F0} {2:F0} Actor: {3} Stationary: {4}", newPos.x, newPos.y, newPos.z, newTarget!=null, newGroundTarget!=null));


            //So we're calculting the climb/dive rate of the mainAC and then setting the end point
            //So that the cover AC will have the same climb/dive rate as the main AC (have to extend it out to the target point, because that
            //is the only point in our way.
            double distance_to_target_m = CoverCalcs.CalculatePointDistance(newPos, airGroup.Pos());
            Vector3d coverVwld = airGroup.Vwld();
            double cover_vel_mps = CoverCalcs.CalculatePointDistance(coverVwld);
            double time_to_target_s = distance_to_target_m / cover_vel_mps;
            if (time_to_target_s > 45)
            {

                /* so this little scheme didn't work because the airgroups don't **gradually** descend to the given altitude over the entire way, instead they just instantly change
                 * to that altitude.  So if we want them to match the player's altitude we just need to give them that altitude now, not trickily try to get them to descend or climb gradually.
                 * In the test, the player lost an engine so was gradually descending to the target point.  The bombers all dropped to the ground immediately.
                 * newPos.z = playerAirGroup.Pos().z + time_to_target_s * Vwld5.z;
                if (newPos.z > 5) newPos.z = 5;
                if (newPos.z > -5) newPos.z = -5;
                */


                //Point3d playerAirGroupPos = airGroup.Pos();
                //if (playerAirGroup != null) playerAirGroupPos = playerAirGroup.Pos();

                if (isBomberAllowedCover(playerAirGroup) || (Calcs.isStrikeAC(playerAirGroup) && Calcs.isStrikeAC(airGroup)) || isFighterAllowedCover_wing(player)) newPos.z = playerAirGroupPos.z; //if leader is a bomber, OR both leader & AI are strike aircraft, OR if it is a fighter with a wingman they fly same alt as the lead plane
                //if (isFighterAllowedCover(playerAirGroup))
                else
                {
                    if (playerAirGroupPos.z >= newPos.z + 750) newPos.z = playerAirGroupPos.z - 750;
                    //But . . . when Knickebein is on they won't follow the escort player down; they'll just stay level.  This allows the escort to fly around & defend them without driving them down.
                    //But they WILL follow the escort player up if the player goes up.  
                    //Generally a fighter escorting bombers, the bombers will fly 750m lower than the escort.
                }
            }

            //If we want to do Hurri_FB, they don't dive bomb.
            //They need to do low alt point bomb.  So 5km out they are at 1000m & aimed straight at target.
            //Put attack_point at 0m to get them as low as possible.  They are usually pretty on target with this strategy
            //Tested via AI mission
            if (distance_to_target_m < 20000 && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null && CoverCalcs.GetAircraftType(airGroup.GetItems()[0] as AiAircraft).Contains("HurricaneMkI_FB"))
            {
                newPos.z = 1000; //Hurri FBs need to be at 1000m 5K out
                if (distance_to_target_m < 5000) newPos.z = 0;
				newPos.z += Calcs.LandElevation_m(newPos);  //adjust for elevvation

            }

            //Some a/c, specifically wellingtons loaded with 2000lb pounds, have a MINIMUM ALTITUDE
            //if they are below that, the bombs will not explode.  So...
            //Note this will still have a small vertical offset from calcOffset_m . . . set MinAttackAlt_m with that in mind.
            double min_z = 0;
            if (coverACInfo.ContainsKey(airGroup)) min_z = coverACInfo[airGroup].MinAttackAlt_m;

            if (!diveTarget && !stationaryDiveTarget) newPos = calcOffset_m(newPos, airGroup, player, new Vector3d(Vwld2.x, Vwld2.y, Vwld5.z), vel_mps, offsetDirection.up_down); //shift this airgroup a little UP or DOWN depending on which a/g it is and what other a/gs of its type are also flying with this player
                                                                                                                                        //so here we are NOT doing the shift right/left of the main target/ac, because we want the bombers to target this precise point, not shift or offset it by some amount.  Instead, shift a little up/down

            //GamePlay.gpLogServer(null, "PosB: " + savePos.x.ToString("F0") + " " + savePos.y.ToString("F0") + " " + savePos.z.ToString("F0") + ":"
            //        + newPos.x.ToString("F0") + " " + newPos.y.ToString("F0") + " " + newPos.z.ToString("F0"), new object[] { });



            /* 
             * 
             * So this didn't work too well--they move up/down too rapidly.  Just match player alt instead..
             * Might should add a limit on how far up/down they move due to this here, because it does happen very abruptly

            double targetVwldZ = Vwld.z;            
            //match the climb/dive of the target a/c, but limit it to relatively normal climb/dive rate of 7 mps, so if the main a/c crashes or whatever it will affect the bomber's run in to target but only by a limited amount.
            if (targetVwldZ > 7) targetVwldZ = 7;
            if (targetVwldZ < -7) targetVwldZ = -7;
            newPos.z += targetVwldZ * time_to_target; //projecting out the current Main AC climb/dive rate out to the target point.       
            */

            //restrict bomber run altitude change to 50 meters at most.  We should key this to the delay on the task_recurs
            //method, but for now this will be OK/ 11 seconds delay and 50 meters means keeping alt change at less than about 5 meters/second which
            //is fairly normal
            /*
            if (airGroup.Pos().z - newPos.z > 50) newPos.z = airGroup.Pos().z - 50;
            if (airGroup.Pos().z - newPos.z < -50) newPos.z = airGroup.Pos().z + 50;
            newPos.z += altDiff_m;
            */


            if (vel_mps < 15) vel_mps = 70;  //help prevent crashes while a/c circling the airport waiting for main a/c to take off.  Or if it crashes, is dead, etc.
            //2026/10 - Step A2: the fixed 55 m/s floor here is replaced by a leader-relative one,
            //for the same reason as in CurrentPosWaypoint above: the bomber path's waypoints also
            //carry the formation speed, and a 55 m/s floor made bombers unable to match a leader
            //flying 51-52 m/s - they sat 3-7 m/s ahead until the leader sped up.  The 70/maybe-<15
            //crash-protection above still applies; the absolute floor is 40 and the leader-relative
            //ceiling on it is 55 (as before), so a fast leader can ask for up to 55+ m/s.
            double bpFloor_mps = 40;
            if (playerAirGroup != null)
            {
                Vector3d plV = playerAirGroup.Vwld();
                double plSpeed = CoverCalcs.CalculatePointDistance(plV);
                if (plSpeed > 1) bpFloor_mps = Math.Min(55, plSpeed * 1.15);
            }
            if (vel_mps < bpFloor_mps) vel_mps = bpFloor_mps;
            if (vel_mps > 170) vel_mps = 170;

            double minDistance_m = 200;
            double newTargetDist_m = CoverCalcs.CalculatePointDistance(airGroup.Pos(), newPos);
            //So we need to be sure that this waypoint is distinct from the last waypoint,
            //and usually this will be used with newPosWayPoint as the 1st waypoint & this as the 2nd.  So we make sure this 
            //second position is distinct from the first by 150 meters
            if (newTargetDist_m == 0)  //if 0 meters, we don't know what to do, just pick a random point of some kind.  This should never happen.
            {
                newPos.x = airGroup.Pos().x + (21.0 + ran.NextDouble() * 100.0) * (ran.Next(2) * 2.0 - 1.0);
                newPos.y = airGroup.Pos().y + 21.0 + ran.NextDouble() * 100.0 * (ran.Next(2) * 2.0 - 1.0);
            }
            else if (newTargetDist_m < minDistance_m) //this is less than 2 seconds travel at our slowest flyable speed.  If less than 200 meters distant but more than 0, we just extend it in the same direction to be at least 200 meters.
            {
                double mult = minDistance_m / newTargetDist_m;
                newPos.x = newPos.x + (newPos.x - airGroup.Pos().x) * mult;
                newPos.y = newPos.y + (newPos.y - airGroup.Pos().y) * mult;
            }

            newPos.z = CoverCalcs.checkMinAGL(newPos.z, newPos);
			
			//In case target is nearest enemy & we don't find one, we just head the a/c towards the player
			if (!diveTarget && !stationaryDiveTarget && BAM_isNearestEnemy(player))
				if (player != null & player.Place() != null && player.Place() as AiAircraft != null) 
					newPos = player.Place().Pos();  
				
			

            //bombers especialy don't like to out run their waypoints.  So we are going to
            //make an extra waypoint that goes 30KM in the same direction, and we'll add that to the flight
            //plan, too. 
            //If bombers run out of flightplan, they auto-switch to task "return" and that means
            //dropping all of their bombs to prepare to return.
            double dst = CoverCalcs.CalculatePointDistance(newPos, airGroup.Pos());
            if (dst == 0) dst = 1;
            double fact = 30000 / dst;
            double LongPosZ = newPos.z;
            if (diveTarget || stationaryDiveTarget) LongPosZ = 400; //800m suggested for after dive?  Let's try 400 m though.
            Point3d LongPos = new Point3d((newPos.x - airGroup.Pos().x) * fact + airGroup.Pos().x,
                (newPos.y - airGroup.Pos().y) * fact + airGroup.Pos().y, LongPosZ);

            AiAirWayPoint nextWP = new AiAirWayPoint(ref newPos, vel_mps);
            AiAirWayPoint nextWP2 = new AiAirWayPoint(ref LongPos, vel_mps);


            Console.WriteLine(String.Format("CBCW: newtargetpoint as sent to AiAirWayPoint: ({0:F0} {1:F0} {2:F0} target mps: {3:F0} main mps: {4:F0})  ", newPos.x, newPos.y, newPos.z, vel_mps, target_vel_mps));

            (nextWP as AiAirWayPoint).GAttackPasses = AiAirWayPointGAttackPasses.AUTO;  //can do ._1 ._2 ._3 ._4 OR ALL_OUT.  But it is hard to say if it really affects the AI behavior much?
            (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.LEVEL;
            //if (ran.Next(2)==0) (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.DIVE;  //change to dive for 50% of bombers

            //if (isBomberArmed(airGroup)) airGroup.setTask(AiAirGroupTask.ATTACK_GROUND, null); //not sure if this really does anything here? Maybe not needed?  //Seems to make bombers drop bombs at ???; not sure what the 2nd variable is - should be an aiairgroup or null apparently?  Maybe only needed for ATTACK_AIR, DEFENDING, etc.
			
			Console.WriteLine("CBCW: 1");
			 //Console.WriteLine("CBCW: bom,alt: {0} {1:F0} {2}", isDiveBomber(airGroup), pos.z, (newTarget as AiActor) != null);
            //if ((newTarget as AiActor) != null && isDiveBomber(airGroup) && pos.z >= 1800)
            bool noGroundEnemyFound = false;
            if (diveTarget || stationaryDiveTarget)
            {
                //(nextWP as AiAirWayPoint).Target = newTarget as AiActor;  //change to newly selected target
                if (newTarget != null) (nextWP as AiAirWayPoint).Target = newTarget;  //change to newly selected target
                else if (newGroundTarget != null) (nextWP as AiAirWayPoint).Target = newGroundTarget as AiActor;  //This is bizarre, becuase if you do AiActor new = newGroundTarget as AiActor and then use new here, it WON'T WORK. But just use newGroundTarget as AiActor instead and it works.  There is no rhyme or reason.
				
				//OK< actually (newGroundTarget as AiActor) is NULL. 
				//Explains all.
				//so for GroundStationaries we can just set the point & forget .Target

				
				//Console.WriteLine ("(newGroundTarget as AiActor): {0} {1} {2}", (newGroundTarget as AiActor), (newGroundTarget as AiActor).Pos().x, (newGroundTarget as AiActor).Pos().y);
				Console.WriteLine ("(newGroundTarget as AiActor): {0}", (newGroundTarget as AiActor));

                Console.WriteLine(String.Format("CBCW: waypoint info of DIVETARGET {0:F0} {1:F0} {2:F0} action: {3} target: {4}", nextWP.P.x, nextWP.P.y, nextWP.P.z, (nextWP as AiAirWayPoint).Action, (nextWP as AiAirWayPoint).Target));
				
                //(nextWP as AiAirWayPoint).Action = AiAirWayPointType.GATTACK_POINT; //OK< that didn't work AT ALL
                (nextWP as AiAirWayPoint).Action = AiAirWayPointType.GATTACK_TARG;  //keep action same

                Console.WriteLine(String.Format("CBCW: waypoint info of DIVETARGET {0:F0} {1:F0} {2:F0} action: {3} target: {4}", nextWP.P.x, nextWP.P.y, nextWP.P.z, (nextWP as AiAirWayPoint).Action, (nextWP as AiAirWayPoint).Target));
				
                if (isDiveBomber(airGroup)) (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.DIVE;
				else (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.AUTO;
                //Console.WriteLine("set target to ground");
				if (!isDiveBomber(airGroup))(nextWP as AiAirWayPoint).GAttackPasses = AiAirWayPointGAttackPasses.ALL_OUT; //can do AUTO _1, _2, _3, _4
				Console.WriteLine("CBCW: 2");
            }
            //case where the target is a point, just target the point (for dive bombers, we always search for divetarget ,even if the target is a point.  Bec. otherwise they won't dive. But if we can't find diveTarget, we can still target the point. So his applies to divebombers where no object found plus ALL point targets)
            else if (!BAM_isNearestEnemy(player)) {//aim point isn't "nearest enemy, ie it's just a point
				Console.WriteLine("CBCW: Attack point OR no ground object found to attack");
                (nextWP as AiAirWayPoint).Action = AiAirWayPointType.GATTACK_POINT;  //keep action same
				//case where the target is ENEMY but we didn't find one.  In that case, DON'T drop
				Console.WriteLine("CBCW: 3");
            }
			else
            {
				Console.WriteLine("CBCW: Couldn't figure out what to do, just normflying towards pilot");
                (nextWP as AiAirWayPoint).Action = AiAirWayPointType.NORMFLY;
                noGroundEnemyFound = true;
				Console.WriteLine("CBCW: 4");
                if (airgroupTargets.ContainsKey(airGroup)) airgroupTargets.Remove(airGroup);
                if (airgroupGroundTargets.ContainsKey(airGroup)) airgroupGroundTargets.Remove(airGroup);
                if (airgroupTargetPoints.ContainsKey(airGroup)) airgroupTargetPoints.Remove(airGroup);
			              
            }
			
			Console.WriteLine("CBCW: 5");

            (nextWP2 as AiAirWayPoint).Action = AiAirWayPointType.NORMFLY; //the 2nd point is always in straight line from the attack point as most types need to continue straight for a while after attack for it to work



           
            //Console.WriteLine( "Target after: {0}", new object[] { wp });
            //Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
            string nm = "(null)";
            try
            {
                //if (((wp as AiAirWayPoint).Target as AiActor) != null) nm = ((wp as AiAirWayPoint).Target as AiActor).Name(); //doesn't work bec. grounstationaries are never AiActors.  We could try looking for AiGroundActors AiGroundGroups, or even AirGroups instead, maybe.  
                //Console.WriteLine("Old Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (wp as AiAirWayPoint).Action, nm, (wp as AiAirWayPoint).P.x, (wp as AiAirWayPoint).P.y, (wp as AiAirWayPoint).GAttackPasses, (wp as AiAirWayPoint).GAttackType });
                //Console.WriteLine("Target Waypoint: {0:F0} {1:F0} {2} {3} {4} for " + airGroup.Name(), new object[] { (nextWP as AiAirWayPoint).P.x, (nextWP as AiAirWayPoint).P.y, (nextWP as AiAirWayPoint).GAttackPasses, (nextWP as AiAirWayPoint).GAttackType, (nextWP as AiAirWayPoint).Action.ToString() });
                //Console.WriteLine ("After Target Waypoint: {0:F0} {1:F0} {2} {3} {4} for " + airGroup.Name(), new object[] { (nextWP2 as AiAirWayPoint).P.x, (nextWP2 as AiAirWayPoint).P.y, (nextWP2 as AiAirWayPoint).GAttackPasses, (nextWP2 as AiAirWayPoint).GAttackType, (nextWP2 as AiAirWayPoint).Action.ToString() });
                /* Console.WriteLine( "New Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (nextWP as AiAirWayPoint).Action, (nextWP as AiAirWayPoint).Target.Name(), (nextWP as AiAirWayPoint).Target.Pos().x, (nextWP as AiAirWayPoint).Target.Pos().y, (nextWP as AiAirWayPoint).GAttackPasses, (nextWP as AiAirWayPoint).GAttackType }); */

                //Console.WriteLine("BomberPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0} {5} {6} LONG: {7:n0} {8:n0} {9:n0}", new object[] { (nextWP as AiAirWayPoint).Action, (nextWP as AiAirWayPoint).Speed, nextWP.P.x, nextWP.P.y, nextWP.P.z, (nextWP as AiAirWayPoint).Target, (nextWP as AiAirWayPoint).GAttackType, nextWP2.P.x, nextWP2.P.y, nextWP2.P.z});
            /*}
            catch (Exception ex)
            {
                Console.WriteLine("Cover/MoveBomb ChangeBomberWaypoint WriteLine: " + ex.ToString());
            }*/
			
			Console.WriteLine("CBCW: 6");

            //In case of strikeAC doing ground attacks, we make the Groundattack_Targ thing their
            //final waypoint, seeing if we can get them to attack better
            if ( Calcs.isStrikeAC(airGroup) && !isHeavyBomber(airGroup) && !isDiveBomber(airGroup) && !airGroup.hasBombs()) nextWP2 = null;

            return new Tuple<AiAirWayPoint, AiAirWayPoint, double, bool, bool>(nextWP, nextWP2, vel_mps, noGroundEnemyFound, false);

        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb ChangeBomberWaypoint: " + ex.ToString()); return null; }
    }


    //2026/10 - <cdrop B test (Step B2).  What a leader-release in <cdrop / <cdropnow hands the group:
    //  GATTACK_POINT     - the old, hand-built immediate pair: NORMFLY at own position +
    //                      GATTACK_POINT 35m ahead.  Log evidence 2026-10-02: a 3xWellington group
    //                      released EXACTLY one bomb per a/c and never again for the rest of the
    //                      mission, no matter how many times a fresh plan was issued (54 -> 51,
    //                      then 51 forever).  The release IS adopted, so the plan works - the
    //                      airframe just won't repeat it.  H1: the engine treats one immediate
    //                      point-attack as one salvo per a/c and marks the attack complete.
    //  GATTACK_TARG_ALLOUT - a GATTACK_TARG at the same 35m-ahead point with GAttackType = AUTO and
    //                      GAttackPasses = ALL_OUT - the same pair the proven ground-attack path
    //                      (BomberPosWaypoint) always sets on its non-dive bombers, which "has
    //                      always worked well in previous versions".  .Target deliberately NOT
    //                      set: the sim picks whatever it finds near the point (CloDNotes 1c), so
    //                      the whole stick releases on the leader's own position - exactly what
    //                      <cdrop wants.
    //  GATTACK_TARG_FAR  - GATTACK_TARG + ALL_OUT at a point FAR AHEAD along the leader's heading
    //                      instead of 35m: the "proper targeting" regime (CloDNotes 1) - the AI
    //                      is supposed to fly on and run a full bomb pass at that point.  Expected
    //                      result: a complete stick, centred on a point forward of the release -
    //                      not on the leader's position.
    //  OFF               - the GATTACK waypoint is simply a trailing NORMFLY: a control group, to
    //                      confirm the group still releases nothing even with a full load intact.
    //No effect unless ON_TESTSERVER - a production server always uses GATTACK_POINT, exactly as
    //before this change.
    public enum cdDropPlanTestMode { GATTACK_POINT, GATTACK_TARG_ALLOUT, GATTACK_TARG_FAR, GATTACK_POINT_FAR, OFF }
    public static cdDropPlanTestMode cdDropPlanTestModeEnum = cdDropPlanTestMode.OFF;
    public static int cdDropPlanTestMode_switches = 0;
    public static int cdDropPlanTestMode_switchesAllowed = 5;

    //2026/10 - B1/B4 bomb-bay experiment (Step B1).  ON_TESTSERVER only.
    //Hypothesis: the release latency of ~6s seen on Wellingtons is the AI cycling the bomb bay
    //doors itself; if the doors are already open when the GATTACK waypoint becomes current, the
    //release should come sooner.  A_BombBayDoor (73, per CloDNotes section 4/6) is READ in the
    //DROPTRACE ladder (the "bay=" column) so we can confirm when the doors open and what the value
    //convention is.  B4's intended pre-open via C_BombBayDoor (40) is NOT yet possible: the IL proves
    //AiAircraft has only getParameter - no setParameter - so this flag only LOGS the door position
    //at issue time, it does not write anything.  Keep it false until a write API / value convention
    //is established; the DROPTRACE bay= column will give us the convention first.
    public static bool cdDropPrefillBombBayDoors = false;

    //targetAirGroup is (typically) the player who has the cover/escort a/c
    public AiAirWayPoint CurrentPosWaypoint(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double requested_vel_mps = -1)
    {

        try
        {
            if (airGroup == null) return null;

            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
            

            Vector3d Vwld = airGroup.Vwld();            
            double vel_mps = CoverCalcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
            double save_vel = vel_mps;
            if (requested_vel_mps >= 0) vel_mps = requested_vel_mps; //if we pass a requested velocity along (as we do for escorts etc) then use that. -1 means, nothing special requested

            //2026/10 - Step A2: the old fixed 55 m/s floor that used to live here was the primary
            //cause of the observed "drift ahead and sit" behaviour: this is the FIRST waypoint
            //written, and the speed on the CURRENT waypoint is the one the AI adopts immediately
            //("when asking AI a/c to change speed it seems to help a lot to put the requested speed
            //in the CURRENT waypoint, not the NEXT", see the note below).  With the floor at 55 m/s
            //a formation leader at 51-52 m/s can never be matched - the group held 55 for ~10s of
            //every ~16s cycle and netted +3..+7 m/s of separation per log (COVERSPEED,
            //genghis-cover-log-2026-10-02B.log, fbDist 444 -> 1485 in ~2.5 min).  So the floor is
            //now leader-relative: it still binds while the leader flies slowly enough to matter
            //and thus still prevents the in-air stalls the fixed floor guarded against, but it no
            //longer pins the group ahead of a slow leader.  The absolute floor is lowered to 40
            //to leave the braking bands the room they actually need.
            double cwFloor_mps = 40;
            if (targetAirGroup != null)
            {
                Vector3d tV = targetAirGroup.Vwld();
                double tSpeed = CoverCalcs.CalculatePointDistance(tV);
                if (tSpeed > 1) cwFloor_mps = Math.Min(55, tSpeed * 1.15);
            }
            if (vel_mps < cwFloor_mps) vel_mps = cwFloor_mps;
            //when asking AI aircraft ot change speed, it seems to help a lot to put the requested speed in the current waypoint, not the NEXT waypoint.  If you don't
            //put it in the current waypoint, it waits until getting to the next waypoint to change speed.
            //Also . . .hard learned bit of info, the requested airspeed is (apparently?) in IAS *****NOT****** TAS.
            //So you get thee current velocity by way of Vwld and it is, of course, True Airspeed.  But apparently
            //What the system expects a waypoint velocity to be, is indicated airspeed - what the pilot would see on the dial in the cockpit.  2020/02.

            //Console.WriteLine("Cover currposwp vel:req_vel:curr_vel {0:F0} : {1:F0} : {2:F0}", vel_mps, requested_vel_mps, save_vel);


            //Not sure if all this velocity thing is really necessary. Maybe this should just match the current cover a/c velocity & the next POS waypoint gives the speed it will try to change to

            /*
            if (targetAirGroup != null)
            {
                Vector3d targetVwld = targetAirGroup.Vwld();
                double target_vel_mps = Calcs.CalculatePointDistance(targetVwld); //Not 100% sure mps is the right unit here?

                double targetDist_m = Calcs.CalculatePointDistance(airGroup.Pos(), targetAirGroup.Pos());
                if (target_vel_mps * 1.5 > vel_mps) vel_mps = target_vel_mps * 1.5; //Go at least 20% faster than the group they're escorting, if possible
                if (targetDist_m > 500 && target_vel_mps * 2.5 > vel_mps) vel_mps = target_vel_mps * 2.5; //Go 2X as fast, if possible, the target gets more than 1km off
            }
            */

            //2026/10 - the old `if (vel_mps < 55) vel_mps = 55;` that lived here is gone: see the
            //leader-relative floor above (Step A2).  The >175 cap stays.
            if (vel_mps > 175) vel_mps = 175;

            Point3d CurrentPos = airGroup.Pos();

             
            CurrentPos.x += Vwld.x * 10; //OK, it seems if the first point is BEHIND the a/c, it causes some kind of a panic and the a/c reverts to task .RETURN.  Once in this RTB mode it accepts no further tasks etc.  So we try to avoid this by putting it 1 second forward.  Could try a few more seconds if this is still flaky.
            CurrentPos.y += Vwld.y * 10; 
                        

            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            if (aawpt != null) aaWP.Action = aawpt;
            bool doit = true;

            try
            {
                if (targetAirGroup == null) doit = false;
                else if (aawpt == null) doit = false;
                else if (targetAirGroup.GetItems() == null) doit = false;
                else if (targetAirGroup.GetItems().Length == 0) doit = false;
                else if (targetAirGroup.GetItems()[0] == null) doit = false;
            }
            catch (Exception ex) { Console.WriteLine("Cover CurrentPosWaypoint TARGETAIRGROUP ERROR: " + ex.ToString()); doit = false; }


            if (doit && aawpt == AiAirWayPointType.ESCORT || aawpt == AiAirWayPointType.FOLLOW) 
            {
                if (targetAirGroup != null) aaWP.Target = targetAirGroup.GetItems()[0]; //targetAirGroup!=null  seems redundant per doit, but still...
            }

            /*
            try
            {
                //Console.WriteLine("CurrentPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0} for " + airGroup.Name(), new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });
            }
            catch (Exception ex) { Console.WriteLine("CurrentPosWaypoint - error writing debug string " + ex.ToString());}
            */

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("Cover CurrentPosWaypoint ERROR: " + ex.ToString()); return null; } //So the try/catch here seems to cause an nullexception error itself? It's crazy . . . 
    }

    public void EscortUpdateWaypoints(Player player, AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true, CoverAGOrders orders = CoverAGOrders.normal)
    {

        //Console.WriteLine("Escort UpdateWaypoints");
        List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
        //NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, aawpt));
        Tuple<AiAirWayPoint, AiAirWayPoint, double> aaWPs = EscortPosWaypoint(player, airGroup, targetAirGroup, aawpt, altDiff_m, AltDiff_range_m, nodupe);
        AiAirWayPoint aawp2 = aaWPs.Item1;
        AiAirWayPoint aawp3 = aaWPs.Item2;
        if (aawp2 != null && aawp2.Action != null) aawpt = aawp2.Action;

        AiAirWayPoint aawp33 = CurrentPosWaypoint(airGroup, targetAirGroup, aawpt, aaWPs.Item3);
        //2026-09-29 - experimental, just not sending the current waypoint at all
        if (aawp33 != null) NewWaypoints.Add(aawp33);
        NewWaypoints.Add(aawp2);
        NewWaypoints.Add(aawp3);

        airGroup.SetWay(NewWaypoints.ToArray());
        
		if (mainmission.ON_TESTSERVER) {
			try {
				Console.WriteLine("Escort - newwaypoints set to: #1: {0:n0} {1:n0} {2:n0} {3} {4} {5:n0} \n #2: {6:n0} {7:n0} {8:n0} {9} {10} {11:n0} \n #3: {12:n0} {13:n0} {14:n0} {15} {16} {17:n0}", new object[] { aawp33.P.x, aawp33.P.y, aawp33.P.z, (aawp33 as AiAirWayPoint).Action, (aawp33 as AiAirWayPoint).Target.Name(), (aawp33 as AiAirWayPoint).Speed, aawp2.P.x, aawp2.P.y, aawp2.P.z, (aawp2 as AiAirWayPoint).Action, (aawp2 as AiAirWayPoint).Target.Name(), (aawp2 as AiAirWayPoint).Speed, aawp3.P.x, aawp3.P.y, aawp3.P.z, (aawp3 as AiAirWayPoint).Action, (aawp3 as AiAirWayPoint).Target.Name(), (aawp3 as AiAirWayPoint).Speed });
			} catch (Exception ex) { Console.WriteLine("EscortUpdateWaypoints console writeline ERROR: " + ex.ToString()); }
		}
        
    }
    public Tuple<AiAirWayPoint, AiAirWayPoint, double> EscortPosWaypoint(Player player, AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double altDiff_m = 1000, double AltDiff_range_m = 700, bool nodupe = true, CoverAGOrders orders = CoverAGOrders.normal)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            AiAirWayPoint aaWP2 = null;
            Point3d CurrentPos = new Point3d(50000, 50000, 500);
            Point3d LongPos = new Point3d(75000, 75000, 500);
            double vel_mps = 100;
            double targetDist_m = 1;
            bool heavyBomber = isHeavyBomber(airGroup) || isDiveBomber(airGroup);


            //Console.WriteLine("Starting EscortPosWaypoint");
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            if (targetAirGroup == null)
            {
                //Console.WriteLine("Cover: EscortPosWaypoint has no targetAirGroup, can't do anything to waypoints, picking a random target point");                

                CurrentPos.x = airGroup.Pos().x + (2000 + ran.NextDouble() * 2000) * (ran.Next(2) * 2 - 1);
                CurrentPos.y = airGroup.Pos().y + (2000 + ran.NextDouble() * 2000) * (ran.Next(2) * 2 - 1);
                CurrentPos.z = airGroup.Pos().z + (2000 + ran.NextDouble() * 2000) * (ran.Next(2) * 2 - 1);
                if (CurrentPos.z < 75) CurrentPos.z = 75;

                LongPos.x = CurrentPos.x + (20000 + ran.NextDouble() * 20000) * (ran.Next(2) * 2 - 1);
                LongPos.y = CurrentPos.y + (20000 + ran.NextDouble() * 20000) * (ran.Next(2) * 2 - 1);
                LongPos.z = CurrentPos.z + (20000 + ran.NextDouble() * 20000) * (ran.Next(2) * 2 - 1);                

            }
            else
            {
                Vector3d Vwld = airGroup.Vwld();

                Vector3d targetVwld = targetAirGroup.Vwld();
                //Point3d targetVwld2 = storedRollingAverage(player, airGroup, "vwld", targetVwld, 2); //rolling average of last 2 positions, used for direction & speed //This seems to make them way to slow to respond to turns and things.
                Point3d targetVwld2 = new Point3d(targetVwld.x, targetVwld.y, targetVwld.z);

                Point3d targetVwld5 = storedRollingAverage(player, airGroup, "vwld", targetVwld, 3); //rolling average of last 5 positions, used for climb rate/vertical speed
                if (targetVwld.z > targetVwld5.z) targetVwld.z = targetVwld5.z;  //use rolling average, but if ascending more rapidly, just use that instead

                double target_vel_mps = CoverCalcs.CalculatePointDistance(targetVwld2);

                //<cfdist Option A: build the virtual leader point ONCE (player position + <cfdist along
                //the leader's heading) and use it for BOTH the speed law and the waypoint base.  The old
                //code applied the offset to the waypoint alone, which is invisible: a heavy-bomber FOLLOW
                //waypoint sits ~5-6 km ahead with .Target = the player, and calcCoverSpeedToMatchMain()
                //equilibrates on the leader's own position - so the formation never moved.  cfdist 0 ->
                //leaderRef == targetAirGroup.Pos(), so nothing changes unless <cfdist is actually set.
                Point3d leaderRef = addFrontBackOffset(targetAirGroup.Pos(), targetVwld, getFrontBackDist(player));

                targetDist_m = CoverCalcs.CalculatePointDistance(airGroup.Pos(), leaderRef);
                Tuple<double, double> ret = calcCoverSpeedToMatchMain(airGroup, targetAirGroup, Vwld, target_vel_mps, targetDist_m, heavyBomber, false, false, orders, aawpt, player, leaderRef);
                vel_mps = ret.Item1;
                double angleTargetToGroup = ret.Item2;


                //<cfdist Option A: base the waypoint on leaderRef too, so the whole flight plan (this
                //point, savePos_offset, LongPos and the trailing run) is centred on the virtual leader
                //point.  The old standalone addFrontBackOffset() call is GONE - the offset is already in
                //leaderRef, and re-applying it here would double it.  calcOffset_m() still does the
                //lateral (left/right) shift, which is an independent axis.
                CurrentPos = leaderRef;
                Point3d savePos = CurrentPos;
                CurrentPos = calcOffset_m(CurrentPos, airGroup, player, targetVwld, target_vel_mps, offsetDirection.left_right); //shift this airgroup a little left or right depending on which a/g it is and what other a/gs of its type are also flying with this player

                Point3d savePos_offset = CurrentPos;
                //GamePlay.gpLogServer(null, "PosE: " + savePos.x.ToString("F0") + " " + savePos.y.ToString("F0") + " " + savePos.z.ToString("F0") + ":"
                //   + CurrentPos.x.ToString("F0") + " " + CurrentPos.y.ToString("F0") + " " + CurrentPos.z.ToString("F0"), new object[] { });

                double current_vel_mps = CoverCalcs.CalculatePointDistance(Vwld); 
                if (heavyBomber) //ok, tried this for ALL aircraft but it didn't go so well
                {
                    if (current_vel_mps < 60) //We set the target waypoint closer if the cover a/c speed is lower, and quite a bit further out of it's going faster
                    {

                        //if ahead of the main ac & more than 400 meters away, try setting target point a lot further ahead
                        //Otherwise when they get up there near the point they seem to mill around a bit.
                        //Also trying more dramatically reducing their speed when they get too far ahead
                        //Note that the alternate target point will also apply if they are far behind & facing away from the main
                        //a/c.  However in that situation I don't think the exact target point makes much difference.
                        //11 seconds at 83mps/300kph is about 900 meters; 60 seconds about 5km
                        if (targetDist_m > 400 && angleTargetToGroup > 120 && angleTargetToGroup < 240)
                        {
                            CurrentPos.x += targetVwld2.x * 90; //Aim for a point slightly ahead of the main aircraft, let's say 20 seconds travel time
                            CurrentPos.y += targetVwld2.y * 90; //20 seconds didn't work too well; they get there & they fly kind of randomly.  Try 60 seconds if a/c going fast
                        }
                        else
                        {
                            CurrentPos.x += targetVwld2.x * 20; //Aim for a point slightly ahead of the main aircraft, let's say 20 seconds travel time if the a/c is going quite slow.  This would be, say, the main a/c is injured and RTB.
                            CurrentPos.y += targetVwld2.y * 20; //20 seconds didn't work too well; they get there & they fly kind of randomly. 
                        }


                        //sO THIS didn't work - they just climb & dive way too abruptly, not over the next 20 or 60 seconds or whatever
                        double targetVwldZ = targetVwld5.z;
                        //also match the climb/dive of the target a/c, but limit it to relatively normal climb/dive rate of 7 mps.
                        if (targetVwldZ > 8) targetVwldZ = 8;
                        if (targetVwldZ < -7) targetVwldZ = -7;
                        //CurrentPos.z += targetVwldZ * 20; //20 seconds didn't work too well; they get there & they fly kind of randomly.                             
                        //^ trying just keeping it at main a/c current altitude instead of adjusting for current a/c climb or dive

                    }
                    else
                    {
                        //if ahead of the main ac & more than 400 meters away, try setting target point a lot further ahead
                        //11 seconds at 83mps/300kph is about 900 meters; 60 seconds about 5km
                        if (targetDist_m > 400 && angleTargetToGroup > 120 && angleTargetToGroup < 240)
                        {
                            CurrentPos.x += targetVwld2.x * 120; //Aim for a point slightly ahead of the main aircraft, let's say 20 seconds travel time
                            CurrentPos.y += targetVwld2.y * 120; //20 seconds didn't work too well; they get there & they fly kind of randomly.  Try 60 seconds if a/c going fast
                        }
                        else
                        {

                            CurrentPos.x += targetVwld2.x * 60; //Aim for a point slightly ahead of the main aircraft, let's say 20 seconds travel time
                            CurrentPos.y += targetVwld2.y * 60; //20 seconds didn't work too well; they get there & they fly kind of randomly.  Try 60 seconds if a/c going fast
                        }


                        double targetVwldZ = targetVwld5.z;
                        //also match the climb/dive of the target a/c, but limit it to relatively normal climb/dive rate of 7 mps.
                        if (targetVwldZ > 8) targetVwldZ = 8;
                        if (targetVwldZ < -8) targetVwldZ = -8;
                        //CurrentPos.z += targetVwldZ * 20; //try just 20 seconds alt here, rather than 60
                        //^ trying just keeping it at main a/c current altitude instead of adjusting for current a/c climb or dive

                    }

                }
                else //not a heavy bomber, ie fighters
                {
                    //2021/06 - was 5 here,  trying it at 45
                    CurrentPos.x += targetVwld2.x * -10; //for fighters let's try setting a point a littler BEHIND the main a/c
                    CurrentPos.y += targetVwld2.y * -10;
                }

                //CurrentPos.z = targetAirGroup.Pos().z + altDiff_m + ran.NextDouble() * 2 * AltDiff_range_m - AltDiff_range_m;
                CurrentPos.z += altDiff_m + ran.NextDouble() * 2 * AltDiff_range_m - AltDiff_range_m; //now we're going to make the covers match climb/dive rates, too - why not

                double waypointDist_m = CoverCalcs.CalculatePointDistance(airGroup.Pos(), CurrentPos);
                //So we need to be sure that this waypoint is distinct from the last waypoint,
                //and usually this will be used with currentPosWayPoint as the 1st waypoint & this as the 2nd.  So we make sure this 
                //second position is distinct from the first by 10 meters
                //This is a failsafe & really should not ever happen.
                if (nodupe && waypointDist_m < 10)
                {
                    CurrentPos.x = airGroup.Pos().x + (21.0 + ran.NextDouble() * 100.0) * (ran.Next(2) * 2.0 - 1.0);
                    CurrentPos.y = airGroup.Pos().y + 21.0 + ran.NextDouble() * 100.0 * (ran.Next(2) * 2.0 - 1.0);
                }

                //GamePlay.gpLogServer(null, "Angle: " + angleTargetToGroup.ToString("F0") + " " + ninetyDiff.ToString("F0") + " Speed: " + vel_mps.ToString("F0") + "/" + target_vel_mps.ToString("F0") + " alt " + CurrentPos.z.ToString("F0"), new object[] { });

                //bombers especialy don't like to out run their waypoints.  So we are going to
                //make an extra waypoint that goes 7KM in the same direction, and we'll add that to the flight
                //plan, too. 
                //If bombers run out of flightplan, they auto-switch to task "return" and that means
                //dropping all of their bombs to prepare to return.
                /*
                double dst = CoverCalcs.CalculatePointDistance(CurrentPos, airGroup.Pos());
                if (dst == 0) dst = 1;
                double fact = 7000 / dst;
                Point3d LongPos = new Point3d((CurrentPos.x - airGroup.Pos().x) * fact + airGroup.Pos().x,
                    (CurrentPos.y - airGroup.Pos().y) * fact + airGroup.Pos().y, CurrentPos.z);

                */
                //update - we'll make it offset from the current player's location by 300 seconds/5 mins of flight time
                //savePos_offset
                if (heavyBomber)
                {
                    LongPos = new Point3d(savePos_offset.x + targetVwld2.x * 300,
                        savePos_offset.y + targetVwld2.y * 300, CurrentPos.z);
                }else
                {
                    //fighters etc
                    //This will put their long point out a 90 degrees (30 seconds travel) from the main a/c
                    //randomly to left or right
                    //hopefully causing them to circle a little bit?
                    //double direction = ran.Next(2) *2 - 1; //1 or -1                    
                    //LongPos = new Point3d(savePos_offset.x + direction* targetVwld2.y * 30,
                    //   savePos_offset.y - direction* targetVwld2.y * 30, CurrentPos.z);

                    //A point 30 sec ahead of the main a/c, so hopefully they circle between just ahead/just behind the main a/c
                    //So escorts follow their own plan pretty much no matter what
                    // Having a really long point doesn't hurt, it will just be replaced whenever the new
                    //flightplan is put in place.
                    //BUT if  they run out of flightplan, they are SCREWED as they switch to RTB (.RETURN task)
                    //and once in that mode, you can't get them back out for love nor money
                    LongPos = new Point3d(savePos_offset.x + targetVwld2.x * 200,
                        savePos_offset.y + targetVwld2.y * 30, CurrentPos.z);
                }



            }
            CurrentPos.z = CoverCalcs.checkMinAGL(CurrentPos.z, CurrentPos);
            LongPos.z = CoverCalcs.checkMinAGL(LongPos.z, LongPos);

            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            aaWP2 = new AiAirWayPoint(ref LongPos, vel_mps);
            //GamePlay.gpLogServer(null, "Alt " + CurrentPos.z.ToString("F0"), new object[] { });
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            //The trick of ESCORT is to set the target to the main aircraft (the player aircraft in this case)
            //Even better, make one group escort the first a/c (live pilot) and the second escort airgroup escorts the first escort ai airgroup.
			/*
            if (!heavyBomber)  //should change this in the sending routine, not here...
            {
                aawpt = AiAirWayPointType.ESCORT; //EXPERIMENTAL !! 
                if (targetDist_m > 7000) aawpt = AiAirWayPointType.FOLLOW; //EXPERIMENTAL !! 
            }
			*/
            aaWP.Action = aawpt;
            aaWP2.Action = aawpt;
            if ((aawpt == AiAirWayPointType.ESCORT || aawpt == AiAirWayPointType.FOLLOW) && targetAirGroup.GetItems().Length > 0)
            {
                aaWP.Target = targetAirGroup.GetItems()[0];
                aaWP2.Target = targetAirGroup.GetItems()[0];
            }


            if(mainmission.ON_TESTSERVER) {
				try
				{
					Console.WriteLine("Cover: EscortPosWaypoint - returning: {0} {1} {2:n0}/{3:n0} {4:n0} LONG: {5:n0}/{6:n0} {7:n0} Dist: {8:n0} Currpos: {9:n0}/{10:n0} {11:n0} for " + airGroup.Name() + " to " + targetAirGroup.Name() + " player at {12:n0}/{13:n0} {14:n0}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z, aaWP2.P.x, aaWP2.P.y, aaWP2.P.z, targetDist_m, airGroup.Pos().x, airGroup.Pos().y, airGroup.Pos().z, targetAirGroup.Pos().x, targetAirGroup.Pos().y, targetAirGroup.Pos().z });
				} catch (Exception ex) { Console.WriteLine("Cover: EscortPosWaypoint ERROR printing to console - " + ex.ToString()); }
			}
            

            return new Tuple<AiAirWayPoint, AiAirWayPoint, double>(aaWP, aaWP2, vel_mps);
        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb EscortPosWaypoint: " + ex.ToString()); return null;}
    }

    //Instead of landing at nearest friendly airport, we send the a/c off map & find a nearby friendly airport along the way off map
    public AiAirWayPoint EscortLandingWaypoint(AiAirGroup airGroup, AiAirGroup targetAirGroup = null, AiAirWayPointType aawpt = AiAirWayPointType.LANDING, double altDiff_m = 1000,
            double AltDiff_range_m = 700, bool nodupe = true)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);



            double vel_mps = 65;
            Point3d CurrentPos = new Point3d(0, 0, 0);

            /*******CLOD Way - weakly try to send to distant airport, then just get them off the MAP*******
            Point3d landingPos = getMidPoint(airGroup.Pos(), getGoodOffMapPoint(airGroup.Pos(), airGroup.getArmy()));            

            AiAirport ap = Stb_nearestAirport(landingPos, airGroup.getArmy());
            if (ap != null) CurrentPos = ap.Pos();
            *////////////////////////////////////////

            Point3d midPos = getMidPoint(airGroup.Pos(), getGoodOffMapPoint(airGroup.Pos(), airGroup.getArmy())); //We still get ready to send them off the map if necessary.

            //But first we TRY to have them land at the nearest airport
            //But if it is far away
            AiAirport ap = Stb_nearestAirport(airGroup.Pos(), airGroup.getArmy(), isSeaplane(airGroup));

            double airportMax_m = 20000;
            if (isSeaplane(airGroup)) airportMax_m = 60000;

            double distApCurrentPos_m = 300000;
            if (ap != null) distApCurrentPos_m = CoverCalcs.CalculatePointDistance(airGroup.Pos(), ap.Pos());

            if (ap != null && distApCurrentPos_m > airportMax_m && CoverCalcs.CalculatePointDistance(airGroup.Pos(), ap.Pos()) < CoverCalcs.CalculatePointDistance(airGroup.Pos(), midPos)) CurrentPos = ap.Pos();
            else CurrentPos = midPos;

            //if (targetAirGroup != null ) CurrentPos = targetAirGroup.Pos();

            //CurrentPos.z = targetAirGroup.Pos().z + altDiff_m + ran.NextDouble() * 2 * AltDiff_range_m - AltDiff_range_m;

            CurrentPos.z = 175;//landing            

            double targetDist_m = 1000;
            if (targetAirGroup != null) targetDist_m = CoverCalcs.CalculatePointDistance(airGroup.Pos(), targetAirGroup.Pos());



            //So we need to be sure that this waypoint is distinct from the last waypoint,
            //and usually this will be used with currentPosWayPoint as the 1st waypoint & this as the 2nd.  So we make sure this 
            //second position is distinct from the first by 100-1000 meters
            if (nodupe && targetDist_m < 10)
            {
                CurrentPos.x = airGroup.Pos().x + (11 + ran.NextDouble() * 100) * (ran.Next(2) * 2 - 1);
                CurrentPos.y = airGroup.Pos().y + 11 + ran.NextDouble() * 100 * (ran.Next(2) * 2 - 1);
            }


            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;
            aaWP.Target = ap as AiActor;
            

            //Console.WriteLine("EscortLANDINGWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0} {5}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z, ap.Name() });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb EscortLANDINGwaypoint: " + ex.ToString()); return null; }
    }

    //Calculate the speed needed for the cover group to catch up to and then fly along with the player/target group.
    //Speed up a lot when far behind, slow down when ahead, gradually match target a/c speed when close in front or behind
    //return new Tuple<double,double> (vel_mps, angleTargetToGroup);
    //2026/10 - Step C (<cfdist, Option A): leaderRef lets the caller hand in the VIRTUAL leader point
    //(the player's position + <cfdist along the player's heading).  Everything below then measures
    //"distance to / angle to the leader" against THAT, so the speed law's resting point actually moves
    //with <cfdist.  Without this the offset only nudged a ~5-6 km-ahead FOLLOW waypoint and had no
    //visible effect.  leaderRef == null => exactly the old behaviour (targetAirGroup.Pos()).
    public Tuple<double, double> calcCoverSpeedToMatchMain(AiAirGroup airGroup, AiAirGroup targetAirGroup, Vector3d Vwld, double target_vel_mps_TAS, double targetDist_m, bool heavyBomber, bool isSturmovik, bool hasGroundTarget, CoverAGOrders orders,  AiAirWayPointType aawpt, Player player, Point3d? leaderRef = null)
    {
        try
        {
            double vel_mps = 125;
            //Point3d directionVectorTarget = new Point3d(targetVwld.x , targetVwld.y, 0); //This would set velocity based on whether the cover a/c is in front of or behind the main a/c
            Point3d directionVectorTarget = new Point3d(Vwld.x, Vwld.y, 0); //this sets it depending on whether or not the cover a/c is headed towards OR away from the main a/c

            double alt_m = 1500;
            if (targetAirGroup != null) alt_m = targetAirGroup.Pos().z;
            //double alt_1000s = CoverCalcs.Feet2Angels(CoverCalcs.meters2feet(alt_m));
            //rough IAS to TAS is IAS + 2% per 1000 feet altitude = TAS.
            //double iasMult = 1 + alt_1000s * 2.0 / 100.0;
            //double target_vel_mps_IAS = target_vel_mps_TAS / iasMult;

            //more precise 1 + alt_m * mult, where mult = 0.000037758346582
            //determined from in-game figures.  0.00005039179621558  seems to be a little more accurate
            //double iasMult = 1 + alt_m * 0.000037758346582;
            //OK, so supposedly the airspeed with give in an AIRWAYPOINT is the TAS, not the IAS. So we need to SKIP  the conversion to IAS.
            //double iasMult = 1 + alt_m * 0.00005039179621558;

            //double target_vel_mps_IAS = target_vel_mps_TAS / iasMult;

            Point3d targetPos = new Point3d(0, 0, 0);
            if (targetAirGroup != null) targetPos = targetAirGroup.Pos();
            //<cfdist Option A: when the caller supplied a virtual leader point, settle/converge on THAT
            //(leader position + cfdist along the leader's heading) instead of the leader's raw position.
            if (leaderRef != null) targetPos = leaderRef.Value;

            Point3d deltaPosTarget = new Point3d((airGroup.Pos().x - targetPos.x), (airGroup.Pos().y - targetPos.y), 0);
            /*
            double divisor = target_vel_mps * targetDist_m;
            if (divisor == 0) divisor = 1; //prevent divison by zero errors in case target AC has zero velocity, or both at same x/y location.

            double angleTargetToGroup = Calcs.RadiansToDegrees(Math.Acos((deltaPosTarget.x * directionVectorTarget.x + deltaPosTarget.y * directionVectorTarget.y)/divisor)); //Angle of the bomber group relative to the main aircraft/target.  Ranges =0-180, where 0 is straight ahead and 180 is straight behind.  Note that it doesn't differentiate betweem left/right (ie 45 degrees might be 45 left or 45 right).
            */

            Vector3d agVwld = airGroup.Vwld();
            double ag_vel_mps = CoverCalcs.CalculatePointDistance(agVwld);

            double angleTargetToGroup = CoverCalcs.CalculateDifferenceAngle(directionVectorTarget, deltaPosTarget); //So this gives the heading angle from the cover a/c to the  main a/c.  180 degrees means the cover a/c is heading directly away from the mai na/c               

            double frontBackDist_m = Math.Abs(targetDist_m * Math.Cos(CoverCalcs.DegreesToRadians(angleTargetToGroup))); //so, really, we only care about the front/back distance when they are catching up/matching speed/position.  Sometimes they are 100 meters or more off the the side & that part of the distance is irrelevant.  frontBackDist_m

            bool inFront = false;
            if (angleTargetToGroup > 90 && angleTargetToGroup < 270) inFront = true;
            double overSpeed = ag_vel_mps / target_vel_mps_TAS;

            /*
            bool pacePlayer = true;

            //case fighter & not strict
            if ( !heavyBomber && !isSturmovik && (orders != CoverAGOrders.strict)) pacePlayer = false; //cover fighters fly above & faster unless orders=strict
            else if (orders == CoverAGOrders.escort) pacePlayer = false; //when put into escort/defend player mode they can go as fast as needed
            else if ( !hasGroundTarget && isSturmovik && orders == CoverAGOrders.attack) pacePlayer = false; //sturmovik w/ no ground target & orders=attack can go faster
            else if ( hasGroundTarget && isSturmovik && !airGroup.hasBombs()) pacePlayer = false; //if sturmovik & independently attacking ground targets, can go as fast as needed
            */

            bool pacePlayer = true;
            if (aawpt == AiAirWayPointType.ESCORT) pacePlayer = false; //when put into escort/defend player mode they can go as fast as needed




            if (!inFront) //IN BACK, ie, cover a/c headed straight towards main a/c, more or less
            {
                if (pacePlayer )
                {
                    vel_mps = target_vel_mps_TAS * coverFormationSpeedBias; //Resting/stability point for a/c in behind.  Biased ABOVE the leader's speed on purpose - at 1.0 they never close the gap, they just sit wherever they are.  See coverFormationSpeedBias.
                    if (frontBackDist_m > 10 && target_vel_mps_TAS * 1.03 > vel_mps) vel_mps = target_vel_mps_TAS * 1.03; //Go at least 20% faster than the group they're escorting, if possible
                    if (frontBackDist_m > 250 && target_vel_mps_TAS * 1.05 > vel_mps) vel_mps = target_vel_mps_TAS * 1.05; //Go at least 20% faster than the group they're escorting, if possible
                    if (frontBackDist_m > 400 && target_vel_mps_TAS * 1.1 > vel_mps) vel_mps = target_vel_mps_TAS * 1.1; //Go at least 20% faster than the group they're escorting, if possible
                    if (frontBackDist_m > 600 && target_vel_mps_TAS * 1.2 > vel_mps) vel_mps = target_vel_mps_TAS * 1.2; //Go at least 20% faster than the group they're escorting, if possible
                    if (frontBackDist_m > 1200 && target_vel_mps_TAS * 1.3 > vel_mps) vel_mps = target_vel_mps_TAS * 1.3; //Go at least 20% faster than the group they're escorting, if possible
                    if (frontBackDist_m > 2000 && target_vel_mps_TAS * 1.4 > vel_mps) vel_mps = target_vel_mps_TAS * 1.4; //Go at least 20% faster than the group they're escorting, if possible
                    if (frontBackDist_m > 4500 && target_vel_mps_TAS * 1.5 > vel_mps) vel_mps = target_vel_mps_TAS * 1.5; //Go 2X as fast, if possible, the target gets more than 1km off //try BIG BRAKES for one cycle
                    if (frontBackDist_m > 6500 && target_vel_mps_TAS * 1.7 > vel_mps) vel_mps = target_vel_mps_TAS * 1.7; //Go 1.7X as fast, if possible, the target gets more than 1km off
                    if (frontBackDist_m > 10000 && target_vel_mps_TAS * 2.0 > vel_mps) vel_mps = target_vel_mps_TAS * 2.0; //etc
                    if (frontBackDist_m > 18000 && target_vel_mps_TAS * 3.0 > vel_mps) vel_mps = target_vel_mps_TAS * 3.0;

                }
            
                else  //generally keep fighter escorts going much faster relative to the main a/c and it doesn't need to snuggle up as close and its target point is
                                  //closer to the main a/c which is more how we keep it in the right area
                {
                    vel_mps = target_vel_mps_TAS * 1.5; //Generally fighters go a fair bit faster than the bombers//2020-02-16 - cutting this back to try to keep them closer in
                    if (targetDist_m > 1000 && target_vel_mps_TAS * 1.0 > vel_mps) vel_mps = target_vel_mps_TAS * 1.0;
                    if (targetDist_m > 3000 && target_vel_mps_TAS * 2.5 > vel_mps) vel_mps = target_vel_mps_TAS * 2.5;

                }
            }
            else //IN FRONT, ie, cover a/c headed straight away from main a/c, more or less
            {
                if (pacePlayer){
                     vel_mps = target_vel_mps_TAS * 0.99; //Go 75% as fast as main aircraft when ahead but kinda close
                    if (frontBackDist_m > 0 && target_vel_mps_TAS * .98 < vel_mps) vel_mps = target_vel_mps_TAS * 0.98; //Go 80% as fast when the target a/c gets more than 750m off
                    if (frontBackDist_m > 80 && target_vel_mps_TAS * .95 < vel_mps) vel_mps = target_vel_mps_TAS * 0.97; //Go 80% as fast when the target a/c gets more than 750m off
                    if (frontBackDist_m > 120 && target_vel_mps_TAS * .85 < vel_mps) vel_mps = target_vel_mps_TAS * 0.97; //Go 80% as fast when the target a/c gets more than 750m off
                    if (frontBackDist_m > 300 && target_vel_mps_TAS * .8 < vel_mps) vel_mps = target_vel_mps_TAS * 0.7; //Go 80% as fast when the target a/c gets more than 750m off
                    if (frontBackDist_m > 500 && target_vel_mps_TAS * .75 < vel_mps) vel_mps = target_vel_mps_TAS * 0.7; //Go 80% as fast when the target a/c gets more than 750m off
                    if (frontBackDist_m > 1500 && target_vel_mps_TAS * .6 < vel_mps) vel_mps = target_vel_mps_TAS * 0.6; //Go 60% as fast when the target a/c gets more than 1.5km off
                    if (frontBackDist_m > 2500 && target_vel_mps_TAS * .4 < vel_mps) vel_mps = target_vel_mps_TAS * 0.4; //Go 40% as fast when the target a/c gets more than 1km off
                }


                //if (!heavyBomber) //generally keep fighter escorts going much faster relative to the main a/c and it doesn't need to snuggle up as close and its target point is
                                  //closer to the main a/c which is more how we keep it in the right area
                else                  
                {
                    vel_mps = target_vel_mps_TAS * 1.3; //Generally fighters go a fair but faster than the bombers, but if they get TOO far away and are going in the wrong direction, slow down
                    if (targetDist_m > 3000 && target_vel_mps_TAS * 1 > vel_mps) vel_mps = target_vel_mps_TAS * 1;
                    if (targetDist_m > 7000 && target_vel_mps_TAS * 0.7 > vel_mps) vel_mps = target_vel_mps_TAS * 0.9;

                }
            }
            double vel_save = vel_mps;

        /*
        //The banhammer drops if the bomber formation members are going too fast
        if (heavyBomber)
        {
            if ((frontBackDist_m < 6000 || inFront) && overSpeed > 1.2) vel_mps = target_vel_mps_TAS * 0.4; //if its closer than 6km in the rear, or in front, and going faster than 120% of main a/c speed, then put brakes on HARD
            else if ((frontBackDist_m < 3000 || inFront) && overSpeed > 1.1) vel_mps = target_vel_mps_TAS * 0.7; //if its closer than 6km in the rear, or in front, and going faster than 120% of main a/c 
            else if ((frontBackDist_m < 1500 || inFront) && overSpeed > 1.05) vel_mps = target_vel_mps_TAS * 0.85;
            else if ((frontBackDist_m > 100 && inFront) && overSpeed > 1) vel_mps = target_vel_mps_TAS * 0.9;
            //else if (inFront && overSpeed > 1) vel_mps = target_vel_mps_TAS * 0.95; //if its closer than 6km in the rear, or in front, and going faster than 120% of main a/c 
        }
        */

        double vel_save2 = vel_mps;


        //Get an angle that tells us whether we are little behind or a little in front of 90 degrees off the main a/c (right or left/ doesn't matter)
        double ninetyDiff = angleTargetToGroup;
        if (ninetyDiff > 180) ninetyDiff = 360 - ninetyDiff;
        ninetyDiff = ninetyDiff - 90;
        ninetyDiff = Math.Abs(ninetyDiff);

        double sign = -1;
        if (angleTargetToGroup >= 90 && angleTargetToGroup <= 270) sign = 1;

        //if (frontBackDist_m < 1000) vel_mps = (vel_mps -target_vel_mps) * frontBackDist_m / 1000 + target_vel_mps; // if close enough in distance to main A/C gradually go same speed as main A/C

            //Only do this when close AND within 2% of the correct speed.
            //NB this OVERWRITES the catch-up band table above it, so whatever the bands decided gets
            //replaced here - which is why the old 0.9999 target made the whole band table pointless
            //inside 400m.  The target is now biased above the leader's speed, so a group that has
            //drifted behind actually keeps closing instead of settling at a fixed offset.
            //
            //The bias is applied BEHIND ONLY (inFront == false).  That matters a lot here: this override
            //REPLACES the inFront braking bands, so while in front it would have been commanding at
            //least 1.06x the leader's speed - i.e. telling a group that was ALREADY ahead to keep
            //accelerating, with the braking bands underneath it silently discarded.  In front we want
            //the bands, so we leave the target at the leader's own speed and just let them slow down.
            if (!inFront && frontBackDist_m < 400 && Math.Abs(target_vel_mps_TAS * coverFormationSpeedBias - ag_vel_mps) < target_vel_mps_TAS / 9) vel_mps = (target_vel_mps_TAS - ag_vel_mps) * frontBackDist_m * sign / 1000 + coverFormationSpeedBias * target_vel_mps_TAS; // brakes/accelerator plan.  Only do this if close to the main a/c AND within 5% in velocity.
            //DIAGNOSTIC - capture what the bands decided (vel_save) vs what the override decided, so we
            //can see which of the two is actually steering.  Currently only the former is logged.
            double velAfterOverride = vel_mps;
                                                                                                                                                                                                                                 //else if (ninetyDiff < 10) vel_mps = (vel_mps - target_vel_mps) * ninetyDiff / 3 + target_vel_mps; // if close enough in ANGLE to main A/C gradually go same speed as main A/C   

        AiAircraft targetAircraft = player.Place() as AiAircraft;
        double ias = 0;
        double tas = 0;
        double mach = 0;

        if (targetAircraft != null) tas = (double)targetAircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
        if (targetAircraft != null) ias = (double)targetAircraft.getParameter(part.ParameterTypes.Z_VelocityIAS, -1);
        if (targetAircraft != null) mach = (double)targetAircraft.getParameter(part.ParameterTypes.Z_VelocityMach, -1);


        if (mainmission.ON_TESTSERVER) Console.WriteLine("Cover vel before:after:target:gp dist ang #{9:D3} Req:{1:D3} : {0:D3} : {8:D3} {10:D3} {11:D3} {12:F2} | TTAS:{2:D3} TIAS:{13:D3} CMPS{3:D3} | {4:F0} FBD:{5:F0} | {6:F0} {7:F0} pacePlayer: {14} task: {15} {16}", (int)vel_save, (int)vel_mps, (int)target_vel_mps_TAS, (int)ag_vel_mps, (int)targetDist_m, frontBackDist_m, angleTargetToGroup, ninetyDiff, (int)vel_save2,airGroup.ID(), (int)tas, (int)ias, (int)mach, (int)target_vel_mps_TAS, pacePlayer, aawpt.ToString(), orders.ToString());


            //<cstrict - hold a rigid formation: match the leader's speed, instead of the 10% over-speed (weaving)
            //that escorts normally use.  Only do this when we are roughly in position, though - if we have fallen
            //well behind, the catch-up speeds that were calculated above are left in place, so we can rejoin.
            //DIAGNOSTIC - did the <cstrict exact-match override fire this cycle?  <cstrict AND <cdrop both run
            //strict formation, and inside strictSpeedMatchDistance_m this forces vel to the leader's own
            //speed regardless of everything above it - a FOURTH discontinuity in the steering law, and
            //one that only shows up on bombing runs.  Worth seeing in the numbers before we re-tune.
            bool strictApplied = false;
            if (isInStrictFormation(airGroup) && frontBackDist_m < strictSpeedMatchDistance_m)
            {
                vel_mps = target_vel_mps_TAS;
                strictApplied = true;
            }

            //<cover - per-airgroup speed calibration.  Everything above decides what speed we WANT; this
            //converts that into what we must ASK FOR to actually get it.  Each group only ever
            //delivers ~98% of the speed commanded in its waypoint, and that fraction varies with
            //altitude, aircraft type and bomb load - so measure it per group instead of assuming.
            //Learn step: compare the speed we asked for LAST time against what it actually flew.
            //The previous cycle's command is the only honest denominator, because ag_vel_mps is this
            //group's response to THAT command, not to the target we are computing right now.
            //Apply step: scale the request by 1/ratio, so the speed we want is the speed we get.
            //This is deliberately applied AFTER the <cstrict exact-match block below is decided in the
            //previous version - it goes in before the clamps, so the [45,175] limits still bound it.
            double coverSpeedRatio = 1.0;
            double coverLastAsked = 0;
            int coverRatioDiscardedThisCycle = 0;
            try
            {
                if (coverAGSpeedRequested.ContainsKey(airGroup))
                {
                    double lastAsked = coverAGSpeedRequested[airGroup];
                    coverLastAsked = lastAsked;
                    if (lastAsked > 5 && ag_vel_mps > 5)
                    {
                        //2026/10 - Step A4, gate 1 (transient): a sample is only "honest" when the
                        //aircraft is actually cruising AT the speed it was last commanded for.  While
                        //it is still turning/climbing toward that command, |ag_vel - lastAsked| is a
                        //transient, not a delivery shortfall, and folding it in poisoned the estimate
                        //(genghis-cover-log-2026-10-02B.log: the ratio ratcheted 1.085 -> 1.137 through
                        //a braking phase and then stayed high into the next acceleration).
                        if (Math.Abs(ag_vel_mps - lastAsked) <= coverAGSpeedRatioTransientDelta_mps)
                        {
                            double observed = ag_vel_mps / lastAsked;
                            //gate 2 (out-of-band): a ratio beyond the trust band is NOT a real delivery
                            //fraction - clamp it INTO the average and it lingers for ~7 samples.  Discard
                            //it instead; the stored estimate is left exactly where it was.
                            if (observed < coverAGSpeedRatioMin || observed > coverAGSpeedRatioMax)
                            {
                                coverRatioDiscardedThisCycle++;
                                coverAGSpeedRatioDiscardedCount++;
                            }
                            else
                            {
                                double prevRatio = coverAGSpeedRatio.ContainsKey(airGroup) ? coverAGSpeedRatio[airGroup] : 1.0;
                                coverAGSpeedRatio[airGroup] = (prevRatio * (1.0 - coverAGSpeedRatioNewWeight)) + (observed * coverAGSpeedRatioNewWeight);
                            }
                        }
                        else
                        {
                            coverRatioDiscardedThisCycle++;
                            coverAGSpeedRatioDiscardedCount++;
                        }
                    }
                }
                if (coverAGSpeedRatio.ContainsKey(airGroup))
                {
                    coverSpeedRatio = coverAGSpeedRatio[airGroup];
                    if (coverSpeedRatio < coverAGSpeedRatioMin) coverSpeedRatio = coverAGSpeedRatioMin;
                    if (coverSpeedRatio > coverAGSpeedRatioMax) coverSpeedRatio = coverAGSpeedRatioMax;
                    vel_mps = vel_mps / coverSpeedRatio;
                }
                //NB the denominator is NOT stored here any more - see Step A4 below the clamps.  The old
                //line stored the pre-clamp value, so a clamped cycle (e.g. the <15 => 75 ground guard)
                //poisoned the next sample by exactly that clamp ratio.
            }
            catch (Exception ex) { Console.WriteLine("Cover speed ratio calibration ERROR: " + ex.ToString()); }

            if (mainmission.ON_TESTSERVER) Console.WriteLine("Cover speed ratio: {0} at {1:N0}m (last asked {2:N0}, actually flew {3:N0}, ratio {4:F3}, now asking {5:N0} for a wanted {6:N0}, discarded this cycle {7})", airGroup.Name(), targetDist_m, coverLastAsked, ag_vel_mps, coverSpeedRatio, vel_mps, vel_mps * coverSpeedRatio, coverRatioDiscardedThisCycle);

            if (vel_mps < 45) vel_mps = 45;
            if (vel_mps > 175) vel_mps = 175;
            if (target_vel_mps_TAS < 15 && vel_mps < 75) vel_mps = 75;  //faster speed here to help prevent crashes while a/c circling the airport waiting for main a/c to take off.  Or if it crashes, is dead, etc.

            //2026/10 - Step A4, gate 3 (denominator): coverAGSpeedRequested now stores the FINAL clamped
            //command - the speed actually written to the waypoints - not the pre-clamp value.  The ratio
            //learn step divides the group's actual speed by this, so if a clamp fired this cycle the
            //denominator was previously wrong by exactly that clamp and the next sample was biased.
            coverAGSpeedRequested[airGroup] = vel_mps;

            //DIAGNOSTIC (ON_TESTSERVER) - one line per group per ~16s cycle summarising every stage of
            //the speed decision, so the formation oscillation can be read off directly instead of
            //inferred.  The key column is "mult" = final commanded speed as a fraction of the leader's:
            //  - behind the leader the band table pins it at coverFormationSpeedBias (1.06) for ALL
            //    distances out to 400m - a constant, so nothing eases off as the gap closes;
            //  - ahead of the leader it is 0.98/0.97 out to 300m, then drops to 0.70 - a -27% step from
            //    one metre of movement.  That asymmetry is what produces the back-and-forth surge.
            //timestamps also show the ~16s re-planning quantisation: consecutive lines for one group
            //are ~16s apart, so the command is HELD while the offset keeps drifting underneath it.
            if (mainmission.ON_TESTSERVER)
            {
                double mult = (target_vel_mps_TAS > 1) ? vel_mps / target_vel_mps_TAS : 0;
                double multBands = (target_vel_mps_TAS > 1) ? vel_save / target_vel_mps_TAS : 0;
                Console.WriteLine("COVERSPEED t={0:HH:mm:ss.fff} grp={1} inFront={2} fbDist={3:N0} ang={4:F0} tgtDist={5:N0} | leaderV={6:N1} grpV={7:N1} ratio={8:F3} | bands={9:N1} afterOvr={10:N1} strict={11} final={12:N1} mult={13:F3} multBands={14:F3} | pace={15} {16} {17}",
                    DateTime.UtcNow, airGroup.Name(), inFront, frontBackDist_m, angleTargetToGroup, targetDist_m,
                    target_vel_mps_TAS, ag_vel_mps, coverSpeedRatio,
                    vel_save, velAfterOverride, strictApplied, vel_mps, mult, multBands,
                    pacePlayer, aawpt.ToString(), orders.ToString());
            }

            return new Tuple<double, double>(vel_mps, angleTargetToGroup);
        }
        catch (Exception ex) { Console.WriteLine("Cover CalcCoverSpeedToMatch ERROR: " + ex.ToString()); return null; }
    }

    public AiAirGroup getRandomNearbyEnemyAirGroup(AiAirGroup from, double distance_m, double lowAlt_m, double highAlt_m)
    {
        try
        {
            Point3d startPos = from.Pos();
            List<AiAirGroup> airGroups = getNearbyEnemyAirGroups(from, distance_m, lowAlt_m, highAlt_m);
            if (airGroups == null || airGroups.Count == 0) return null;
            int choice = ran.Next(airGroups.Count);
            if (airGroups[choice].Pos().distance(ref startPos) >= distance_m / 4) //We'll somewhat favor airgroups very to the from airgroup
                choice = ran.Next(airGroups.Count);
            if (airGroups[choice].Pos().distance(ref startPos) >= distance_m / 2) //We'll somewhat favor airgroups closer to the from airgroup
                choice = ran.Next(airGroups.Count);
            return airGroups[choice];
        }
        catch (Exception ex) { Console.WriteLine("Cover getRandomNearbyEnemyAirGroup ERROR: " + ex.ToString()); return null; }

    }

    //Gets all nearby enemy airgroup within distance_m (meters) and between alt - lowAlt_m & alt-highAlt_m altitude of the target
    public List<AiAirGroup> getNearbyEnemyAirGroups(AiAirGroup from, double distance_m, double lowAlt_m, double highAlt_m)
    {
        try
        {
            if (GamePlay == null) return new List<AiAirGroup>() { }; 
            if (from == null) return new List<AiAirGroup>() { }; 
            List<AiAirGroup> returnAirGroups = new List<AiAirGroup>();
            AiAirGroup[] Airgroups;
            Point3d StartPos = from.Pos();

            int army = from.Army();
            if (army < 1 || army > 2) return returnAirGroups;
            Airgroups = GamePlay.gpAirGroups((army == 1) ? 2 : 1);

            if (Airgroups != null)
            {
                foreach (AiAirGroup airGroup in Airgroups)
                {
                    if (airGroup == null || airGroup.GetItems().Length == 0 || !(airGroup as AiActor).IsAlive() || !(airGroup as AiActor).IsValid() || (airGroup as AiActor).Name().ToLower().Contains("noname") || (airGroup as AiActor).Army() ==from.Army()) continue;
                    //AiAircraft a = airGroup.GetItems()[0] as AiAircraft;

                    if (airGroup.Pos().z > StartPos.z - lowAlt_m && airGroup.Pos().z < StartPos.z + highAlt_m && airGroup.Pos().distance(ref StartPos) <= distance_m)
                        returnAirGroups.Add(airGroup);

                }
                return returnAirGroups;
            }
            else
            return new List<AiAirGroup>() { };
        }
        catch (Exception ex) { Console.WriteLine("-COVER getNearbyEnemyAirGroups ERROR: " + ex.ToString()); return new List<AiAirGroup>() { }; }

    }



    //If ai cover aircraft come close to the map edge we're going to say they survived & re-add them to stock.

    public void AddOffMapAIAircraftBackToSupply_recur()
    {
        Timeout(60.123232, () => AddOffMapAIAircraftBackToSupply_recur());
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("CVXX4 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping

        try
        {
            int numremoved = 0;

            //BattleArea 10000 10000 360000 310000 10000
            //TODO: There is probably some way to access the size of the battle area programmatically
            /* double twcmap_minX = 10000;
            double twcmap_minY = 10000;
            double twcmap_maxX = 360000;
            double twcmap_maxY = 310000;
            */

            double minX = twcmap_minX - 12500; //20000 //So -main & -stats both give a 12500m buffer beyond the map edge "grace area".  So we should do the same here            
            double minY = twcmap_minY - 12500; //20000 //TODO: Set a variable for grace area & use the same for all 3 places
            double maxX = twcmap_maxX + 12500; //340000;
            double maxY = twcmap_maxY + 12500; // 300000;

            //Console.WriteLine("Checking for AI Aircraft off map, to check back in (Cover)");
            foreach (AiActor actor in coverAircraftActorsCheckedOut.Keys)
            {
                AiAircraft a = actor as AiAircraft;
                /*Console.WriteLine("COVER: Checking for off map: " + Calcs.GetAircraftType(a) + " "
                + actor.Name() + " "
                + a.Type() + " "
                + a.TypedName() + " "
                + a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0")
                  );
                  */



                if (a != null &&
                      (actor.Pos().x <= minX ||
                        actor.Pos().x >= maxX ||
                        actor.Pos().y <= minY ||
                        actor.Pos().y >= maxY
                      )

                )
                {

                    Console.WriteLine("Cover AI Aircraft off map, checking back in: " + Calcs.GetAircraftType(a) + " "
                        + actor.Name() + " "
                        + a.Type() + " "
                        + a.TypedName() + " "
                        + a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0"));

                    EscortMakeLand(a.AirGroup(), null); //fixing bug - cover aircraft flew off map & were 'returned' but they were actually still in the air & following the main a/c.                    

                    numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                    supplymission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true, reason: "SAFE_cover_CoverAircraftOffMap"); //return this a/c to supply; true = softexit which forces return of the plane even though it is still in the air & flying
                                                                                                                                             //Console.WriteLine("CoverLeftMap: " + actor.Name() + " was returned to stock because left map OK.");
                    Timeout(0.1, () => { coverAircraftActorsCheckedOut.Remove(actor); }); //Little cheap trick to remove an item from coverAircraftActorsCheckedOut even though we are presently looping through its keys
                }


            }
        }
        catch (Exception ex) { Console.WriteLine("Cover removeoffmap: " + ex.ToString()); }

        // if (DEBUG && numremoved >= 1) DebugAndLog (numremoved.ToString() + " AI Aircraft were off the map and de-spawned");
    } //method removeoffmapaiaircraft

    //returns distance to nearest friendly airport to actor, in meters. Count all friendly airports, alive or not.
    //In case of birthplace find, get the nearest birthplace regardless of friendly or not
    //2020-01 - rewrote so that birthplaces work.  They worked before, I thought?  Maybe something changed with CloD 4.5+?
    //Finds either airports alone OR airports & birthplaces/spawn points.  
    //Double is distance, bool is true if closest airport is an AIRSPAWN
    private Tuple<double, Point3d, bool> Stb_distanceToNearestFriendlyAirport(AiActor actor, bool birthplacefind = false)  //<distance, location, whether or not an airspawn
    {
        double d2 = 10000000000000000; //we compare distanceSQUARED so this must be the square of some super-large distance in meters && we'll return anything closer than this.  Also if we don't find anything we return the sqrt of this number, which we would like to be a large number to show there is nothing nearby.  If say d2 = 1000000 then sqrt (d2) = 1000 meters which probably not too helpful.
        if (GamePlay == null) return new Tuple<double, Point3d, bool>(Math.Sqrt(d2), new Point3d(0,0,0),false);
        double d2Min = d2;
        if (actor == null) return new Tuple<double, Point3d, bool>(d2Min, new Point3d(-1, -1, -1), false);
        Point3d pd = actor.Pos();
        Point3d retPoint = new Point3d(-1, -1, -1);
        int pArmy = actor.Army();
        bool isAirSpawn = false;

        //int retArmy = 0; //0 indicates no army, aiairfields don't have army included; you have to get it from the location.  But Birthplaces do.
        int n;

        n = GamePlay.gpAirports().Length;


        //AiActor[] aMinSaves = new AiActor[n + 1];
        //int j = 0;
        //GamePlay.gpLogServer(null, "Checking distance to nearest airport", new object[] { });
        for (int i = 0; i < n; i++)
        {


            AiActor a;
            Point3d ps = new Point3d(-1, -1, -1);
            int aArmy = -1;


            a = (AiActor)GamePlay.gpAirports()[i];
            if (a == null) continue;
            ps = a.Pos();
            //aArmy = a.Army();


            //if (actor.Army() != a.Army()) continue; //only count friendly airports
            //if (actor.Army() != (a.Pos().x, a.Pos().y)
            //OK, so the a.Army() thing doesn't seem to be working, so we are going to try just checking whether or not it is on the territory of the Army the actor belongs to.  For some reason, airports always (or almost always?) list the army = 0.

            //GamePlay.gpLogServer(null, "Checking airport " + a.Name() + " " + GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) + " " + a.Pos().x.ToString ("N0") + " " + a.Pos().y.ToString ("N0") , new object[] { });

            if (GamePlay.gpFrontArmy(ps.x, ps.y) != pArmy) continue;


            //if (!a.IsAlive()) continue;


            Point3d pp;
            pp = ps;
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                retPoint = ps;
                d2Min = d2;
                //GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString(), new object[] { });
            }
        }

        if (birthplacefind)
        {
            n = GamePlay.gpBirthPlaces().Length;
            for (int i = 0; i < n; i++)
            {
                AiActor a;
                Point3d ps = new Point3d(-1, -1, -1);
                int aArmy = -1;


                ps = GamePlay.gpBirthPlaces()[i].Pos();
                aArmy = GamePlay.gpBirthPlaces()[i].Army();

                if (aArmy != pArmy)  //We want to allow for cases where the Birthplace doesn't have an army assigned (?!) but is still on the home territory of the player, but also maybe it is an airspawn and happens to be over friendly territory and belongs to the player's army . That would be OK.
                {
                    if (aArmy != 0) continue;
                    if (GamePlay.gpFrontArmy(ps.x, ps.y) != pArmy) continue;
                }


                //if (!a.IsAlive()) continue;


                Point3d pp;
                pp = ps;
                pd.z = pp.z; //we only care about the horizontal distance for this purpose
                d2 = pd.distanceSquared(ref pp);
                if (d2 < d2Min)
                {
                    d2Min = d2;
                    retPoint = ps;
                    //GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString(), new object[] { });
                    if (ps.z > 300) isAirSpawn = true;
                    else isAirSpawn = false;
                }

            }

        }
        //GamePlay.gpLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
        return new Tuple<double, Point3d, bool>(Math.Sqrt(d2Min), retPoint, isAirSpawn);
    }


    //nearest airport to a point
    //army=0 is neutral, meaning found airports of any army
    //otherwise, find only airports matching that army
    //Will return water airports ONLY for seaplane=true, land airports ONLY for seaplane=false and both types for seaplane=null
    public AiAirport Stb_nearestAirport(Point3d location, int army = 0, bool? isSeaplane = null)
    {
        AiAirport NearestAirfield = null;
        if (GamePlay == null) return null;
        AiAirport[] airports = GamePlay.gpAirports();
        Point3d StartPos = location;

        if (airports != null)
        {
            foreach (AiAirport airport in airports)
            {
                AiActor a = airport as AiActor;
                if (army != 0 && GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != army) continue;


                if (isSeaplane.HasValue)
                {
                    maddox.game.LandTypes landType = GamePlay.gpLandType(a.Pos().x, a.Pos().y);
                    if (isSeaplane.Value && landType != maddox.game.LandTypes.WATER) continue;
                    if (!isSeaplane.Value && landType == maddox.game.LandTypes.WATER) continue;
                }
                if (NearestAirfield != null)
                {
                    if (NearestAirfield.Pos().distanceSquared(ref StartPos) > airport.Pos().distanceSquared(ref StartPos))
                        NearestAirfield = airport;
                }
                else NearestAirfield = airport;
            }
        }


        //AirfieldDisable(NearestAirfield); //for testing
        //Console.WriteLine("Destroying airfield " + NearestAirfield.Name());
        return NearestAirfield;
    }

    //nearest airport to an actor
    public AiAirport Stb_nearestAirport(AiActor actor, int army = 0, bool? isSeaplane = null)
    {
        if (actor == null) return null;
        Point3d pd = actor.Pos();
        return Stb_nearestAirport(pd, army, isSeaplane);
    }

    public double Stb_nearestAirport_distance_m(Point3d location, int army = 0, bool? isSeaplane = null)
    {
        AiAirport ap = Stb_nearestAirport(location, army , isSeaplane);
        if (ap == null) return 10000000; //if no airport, return a very large #
        double dist_m = CoverCalcs.CalculatePointDistance(location, ap.Pos());
        return dist_m;
    }

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

    public Point3d getGoodOffMapPoint(Point3d currPos, int army)
    {
        Point3d endPos = new Point3d(0, 0, 0);
        Point3d retPos = new Point3d(0, 0, 0);

        double tempDistance_m = 10000000;
        double offMapBuffer = 25000;
        for (int i = 1; i < 16; i++)
        {
            if (ran.NextDouble() > 0.5)
            {

                if (army == 1) endPos.y = twcmap_maxY + offMapBuffer;
                else if (army == 2) endPos.y = twcmap_minY - offMapBuffer;
                else endPos.y = twcmap_maxY + offMapBuffer;
                endPos.x = currPos.x + ran.NextDouble() * 300000 - 150000;
                if (endPos.x > twcmap_maxX + offMapBuffer) endPos.x = twcmap_maxX + offMapBuffer;
                if (endPos.x < twcmap_minX - offMapBuffer) endPos.x = twcmap_minX - offMapBuffer;
            }
            else
            {
                if (army == 1) endPos.x = twcmap_minX - offMapBuffer;
                else if (army == 2) endPos.x = twcmap_maxX + offMapBuffer;
                else endPos.x = twcmap_maxX + offMapBuffer;
                endPos.y = currPos.y + ran.NextDouble() * 300000 - 150000;
                if (army == 1) endPos.y += 80000;
                else if (army == 2) endPos.y -= 10000;
                if (endPos.y > twcmap_maxY + offMapBuffer) endPos.y = twcmap_maxY + offMapBuffer;
                if (endPos.y < twcmap_minY - offMapBuffer) endPos.y = twcmap_minY - offMapBuffer;
            }
            /*
              //TOBRUK way.  It only makes sense to go east or west.  North==ocean, south==desert
                if (army == 1) endPos.x = twcmap_maxX + offMapBuffer;
                else if (army == 2) endPos.x = twcmap_minX - offMapBuffer;
                else endPos.x = twcmap_maxX + offMapBuffer;
                endPos.y = currPos.y + (ran.NextDouble() * 300000 - 150000); //Math.Sqrt(ran.NextDouble()) makes it favor things closer to 0; ie .y usuallyi won't move up OR down by too much
                if (endPos.y > twcmap_maxY + offMapBuffer) endPos.y = twcmap_maxY + offMapBuffer;
                if (endPos.y < twcmap_minY - offMapBuffer) endPos.y = twcmap_minY - offMapBuffer;
            */

            //so, we want to try to find a somewhat short distance for the aircraft to exit the map.
            //We take the shortest distance based on several random tries
            double distance_m = CoverCalcs.CalculatePointDistance(endPos, currPos);

            if (distance_m < tempDistance_m || i == 1)
            {
                tempDistance_m = distance_m;
                retPos = endPos;
            }

        }
        return retPos;
    }

    //Gets midpoint of exit path, with a slight dogleg
    public Point3d getMidPoint(Point3d p1, Point3d p2)
    {
        Point3d midPos = p1;
        midPos.x = (p1.x * 1.0 + p2.x * 1.0) / 2.0 + (ran.NextDouble() * 25000.0) - 12500.0;
        midPos.y = (p1.y * 1.0 + p2.y * 1.0) / 2.0 + (ran.NextDouble() * 25000.0) - 12500.0;
        return midPos;
    }

    //If actor is given, will skip any points that are the same as the actor's position
    public double distToNearestAirgroupTargetPoint(Point3d p, AiActor act = null) {
        bool isActor = act != null;
        Point3d retPos = new Point3d(-1,-1,0);
        if (isActor) retPos = act.Pos();
        //if NO point selected it is set to -1,-1, which should still work for this purpose 
        //without special handling
        double distsq_m2 = 100000000000000;
        foreach (Point3d pos in airgroupTargetPoints.Values) {
            if (isActor && Calcs.Point3dEqualXY(pos,retPos)) continue;
            double diffX = p.x-pos.x;
            double diffY = p.y-pos.y;
            double dist_temp = diffX*diffX + diffY*diffY;
            distsq_m2 = dist_temp<distsq_m2 ? dist_temp : distsq_m2;

        }
        if (act != null && distsq_m2 <0.2) return 10000000; //if that close, assuming it is  actually the same actor, return a large number so it will be ignored
        return Math.Sqrt(distsq_m2);
    }


	

    //*************************************
	//2026-08 - for now just using movebombtarget fixway points, we'll see how it goes.  They are largely duplicative.
	//*************************************
	
    //So, various fixes to WayPoints, including removing any dupes, close dupes, any w-a-y off the map, and adding two points at the end of the route to take
    //the aircraft down low and off the map north (Red) or south (Blue)
    public void fixWayPoints(AiAirGroup airGroup)
    {
		if (mainmission.movebombtargetmission != null) {
            mainmission.movebombtargetmission.fixWayPoints(airGroup);
			return;
		}
		
        try
        {
            //AiAirGroup airGroup = intc.attackingAirGroup;
            if (airGroup == null || airGroup.GetWay() == null || airGroup.GetCurrentWayPoint() == null) return; //Not sure what else to do?
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay(); //So there is a problem if GetWay is null or doesn't return anything. Not sure what to do in that case!
                                                               //Maybe just exit?

            //if (CurrentWaypoints == null || CurrentWaypoints.Length == 0) return;
            //if (!isAiControlledAirGroup(airGroup)) return;
            if (airGroup.GetItems().Length == 0) return; //no a/c, no need to do anything
            AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;

            //for testing


            foreach (AiWayPoint wp in CurrentWaypoints)
            {

                //Console.WriteLine("FixWayPointsCover - Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }



            int currWay = airGroup.GetCurrentWayPoint();


            //if (currWay >= CurrentWaypoints.Length) return;

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
            int count = 0;

            bool update = false;

            AiAirWayPoint prevWP = CurrentPosWaypoint(airGroup, null, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);

            NewWaypoints.Add(prevWP); //Always have to add current pos/speed as first point or things go w-r-o-n-g

            AiAirWayPoint nextWP = mainmission.movebombtargetmission.makeNewAiAirWaypointFromOld(prevWP as AiAirWayPoint); //NOTE THIS DOESN"T WORK - just a new name for same object...

            bool landing = false; //keep track of whether or not the last waypoint is "landing".


            foreach (AiAirWayPoint wp in CurrentWaypoints)
            {

                if ((wp as AiAirWayPoint).Action == AiAirWayPointType.LANDING)
                {
                    wp.P.z = 50; //if landing set the altitude very low. //lowest ap in Channel is about close to 0 m.
                    wp.Speed = 75; //around 100mph speed for landing
                    landing = true;
                }
                else
                {
                    landing = false;
                    wp.P.z = CoverCalcs.checkMinAGL(wp.P.z, wp.P);
                }
                if (count > currWay)
                {
                    nextWP = wp;
                    NewWaypoints.Add(nextWP); //do add                 
                }
                count++;
            }
            /*
                //eliminate any exact duplicate points
                if (Math.Abs(nextWP.P.x - prevWP.P.x) < 1 && Math.Abs(nextWP.P.y - prevWP.P.y) < 1 && Math.Abs(nextWP.P.z - prevWP.P.z) < 1
                    && (nextWP as AiAirWayPoint).Action == (prevWP as AiAirWayPoint).Action)
                {
                    //if the Task is different for the 2nd point, it will only be operative for 50 meters . So skipping it?
                    update = true;
                    //Console.WriteLine("FixWayPoints - eliminating identical WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                    continue;
                }
                //eliminate any  close duplicates, except in the hopefully rare case the 2nd .Action is some kind of ground attack                 
                if (Math.Abs(nextWP.P.x - prevWP.P.x) < 50 && Math.Abs(nextWP.P.y - prevWP.P.y) < 50 && Math.Abs(nextWP.P.z - prevWP.P.z) < 50 &&
                    (nextWP as AiAirWayPoint).Action != AiAirWayPointType.GATTACK_TARG && (nextWP as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT)
                {
                    //if the Task is different for the 2nd point, it will only be operative for 50 meters . So skipping it?
                    update = true;
                    //Console.WriteLine("FixWayPoints - eliminating close match WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                    continue;
                }


                try
                {
                    //So, a waypoint could be way off the map which results in terrible aircraft malfunction (stopped dead in mid-air, etc?)
                    if (nextWP.P.x > twcmap_maxX + 9999 || nextWP.P.y > twcmap_maxY + 9999 || nextWP.P.x < twcmap_minX - 9999 || nextWP.P.y < twcmap_minY - 9999 || nextWP.P.z < 0 || nextWP.P.z > 50000)
                    {
                        Console.WriteLine("CoverFixWayPoints - WP WAY OFF MAP! Before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                        update = true;
                        if (nextWP.P.z < 0) nextWP.P.z = 0;
                        if (nextWP.P.z > 50000) nextWP.P.z = 50000;
                        if (nextWP.P.x > twcmap_maxX + 9999) nextWP.P.x = twcmap_maxX + 9999;
                        if (nextWP.P.y > twcmap_maxY + 9999) nextWP.P.y = twcmap_maxY + 9999;
                        if (nextWP.P.x < twcmap_minX - 9999) nextWP.P.x = twcmap_minX - 9999;
                        if (nextWP.P.y < twcmap_minY - 9999) nextWP.P.y = twcmap_minY - 9999;
                        Console.WriteLine("CoverFixWayPoints - WP WAY OFF MAP! After: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                    }
                }
                catch (Exception ex) { Console.WriteLine("Cover/MoveBomb FixWay ERROR2A: " + ex.ToString()); }


                NewWaypoints.Add(nextWP); //do add
                count++;

            }
            */

            //So, if the last point is somewhere on the map, we'll just make them discreetly fly off the map at some nice alt
            if (nextWP.P.x > twcmap_minX - 10000 && nextWP.P.x < twcmap_maxX + 10000 && nextWP.P.y > twcmap_minY - 10000 && nextWP.P.y < twcmap_maxY + 10000)
            {
                update = true;
                int army = airGroup.getArmy();
                AiAirWayPoint landaaWP = null;
                AiAirWayPoint midaaWP = null;
                AiAirWayPoint endaaWP = null;
                Point3d landPos = new Point3d(0, 0, 0);
                Point3d midPos = new Point3d(0, 0, 0);
                Point3d endPos = new Point3d(0, 0, 0);
                Point3d tempEndPos = new Point3d(0, 0, 0);
                double distance_m = 100000000000;
                double tempDistance_m = 100000000000;

                //so we expanded the grace area for players to fly off the map, to 10,000m plus the actual sides of the map
                //And we made AI match
                //as shown.  So . . . now sending them 9000m off the map isn't getting them off far enough.
                //So, make it a solid 25000 just to be safe
                //However, I'm a bit worried about what will happen with negative numbers in the map coordinates.  Not sure if it is possible.
                double offMapBuffer = 25000;

                endPos = getGoodOffMapPoint(nextWP.P, army);

                //endPos.z = 25;  //Make them drop down so they drop off the radar 
                //Ok, that was as bad idea for various reasons
                //nextWP is the most recent WP, ie the last WP in the 'old' waypoint list
                //prevWP is where the a/c is right now, ie the first on the old waypoint list
                //We choose one or the other 50% of the time as they are both 'typical' altitudes for this a/c ?
                endPos.z = nextWP.P.z;
                if (ran.NextDouble() < 0.5) endPos.z = prevWP.P.z;
                midPos.z = endPos.z;
                endPos.z = ran.NextDouble() * 200 + 30;
                midPos.z = midPos.z + ran.NextDouble() * 4000 - 1700;
                if (endPos.z < 30) endPos.z = 30;
                if (midPos.z < 30) midPos.z = 30;

                double speed = prevWP.Speed;


                //A point in the direction of our final point but quite close to the previous endpoint.  We'll add this in as a 2nd to
                //last point where the goal will be to have the airgroup low & off the radar at this point.
                //Ok, low & off radar didn't really work as they just don't go low enough.  So now objective is to make.  UPDATE 2020/03 - now AI is under radar if it's below about 600ft.
                //  AI alt-800ft  is the same as breather altitude for below-radar purposes. public bool belowRadar in main.cs
                //them look more like normal flights, routine patrols or whatever.  So slight deviation in flight path, not just STRAIGHT off the map, 
                //and random normal altitudes
                /*midPos.x = (nextWP.P.x * 1.0 + endPos.x * 1.0) / 2.0 + (ran.NextDouble() * 50000.0) - 25000.0;
                midPos.y = (nextWP.P.y * 1.0 + endPos.y * 1.0) / 2.0 + (ran.NextDouble() * 50000.0) - 25000.0;
                */

                double saveZ = endPos.z;
                midPos = getMidPoint(nextWP.P, endPos);
                midPos.z = saveZ;
                bool foundAirport = false;

                if (landing)
                {
                    try
                    {
                        //the CLOD way - they try an airport somewhere halfway off the map
                        //AiAirport ap = CoverCalcs.GetRandomAirfieldNear(GamePlay, midPos, 32000);
                        AiAirport ap = CoverCalcs.GetRandomAirfieldNear(GamePlay, midPos, 32000);
                        if (ap != null)
                        {
                            landPos = ap.Pos();
                            if (Math.Abs(landPos.x - prevWP.P.x) < 200 && Math.Abs(landPos.y - prevWP.P.y) < 200)
                            {
                                landPos.x += ran.Next(200, 600); //Just in case the previous landing point is at this same airport, prevent the double/exact repeat point.
                                landPos.y += ran.Next(200, 600);
                            }

                            landPos.z += 70; //trying to keep them from ground crashing near airports . . . 
                            AiAirWayPointType landaawpt = AiAirWayPointType.LANDING;
                            landaaWP = new AiAirWayPoint(ref landPos, 50); // 50 mps ~= 100 mph, so reasonable pre-landing speed.                    
                            landaaWP.Action = landaawpt;
                            NewWaypoints.Add(landaaWP); //do add
                            count++;
                            update = true;
                            foundAirport = true;
                        }

                        /*
                        //TOBRUK Way - find a good nearby airport, and just land
                        //AiAirport ap = CoverCalcs.GetRandomAirfieldNear(GamePlay, nextWP.P, 32000);
                        AiAirport ap = Stb_nearestAirport(nextWP.P, airGroup.getArmy(), isSeaplane(airGroup));

                        if (ap != null)
                        {
                            landPos = ap.Pos();
                            if (Math.Abs(landPos.x - prevWP.P.x) < 200 && Math.Abs(landPos.y - prevWP.P.y) < 200)
                            {
                                landPos.x += ran.Next(200, 600); //Just in case the previous landing point is at this same airport, prevent the double/exact repeat point.
                                landPos.y += ran.Next(200, 600);
                            }

                            landPos.z += 70; //trying to keep them from ground crashing near airports . . . 
                            AiAirWayPointType landaawpt = AiAirWayPointType.LANDING;
                            landaaWP = new AiAirWayPoint(ref landPos, 50); // 50 mps ~= 100 mph, so reasonable pre-landing speed.                    
                            landaaWP.Action = landaawpt;
                            NewWaypoints.Add(landaaWP); //do add
                            count++;
                            update = true;
                            foundAirport = true;
                            Console.WriteLine("COVER FixWayPoints - airport found, adding landing-at-airport WP: {5}  {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { landaawpt, (landaaWP as AiAirWayPoint).Speed, landaaWP.P.x, landaaWP.P.y, landaaWP.P.z, (ap as AiActor).Name() });
                        }
                        */
                    }
                    catch (Exception ex) { Console.WriteLine("Cover FixWayPoints #3: " + ex.ToString()); }
                }

                if (!foundAirport)
                {

                    /* (Vector3d Vwld = airGroup.Vwld();
                    double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
                    if (vel_mps < 70) vel_mps = 70;
                    if (vel_mps > 160) vel_mps = 160;                
                    */


                    /*
                     * //Trying to give reasonable airwaypointtypes to the flight, but this just confusing -MoveBombTarget
                     * //Instead, we'll give all aircraft .ESCORT which prevents them from being shanghaied by -MoveBombTarget and reprogrammed
                    AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS;
                    if ((nextWP as AiAirWayPoint).Action != AiAirWayPointType.LANDING && (nextWP as AiAirWayPoint).Action != AiAirWayPointType.TAKEOFF)
                        aawpt = (nextWP as AiAirWayPoint).Action;
                    else
                    {
                        string type = "";
                        string t = aircraft.Type().ToString();
                        if (t.Contains("Fighter") || t.Contains("fighter")) type = "F";
                        else if (t.Contains("Bomber") || t.Contains("bomber")) type = "B";

                        if (type == "B") aawpt = AiAirWayPointType.FOLLOW;

                    }
                    */

                    //.Escort stops MoveBombTarget from re-programming the a/c, so it should just fly off the map no problem.
                    //we could also try .LANDING .FOLLOW .TAKEOFF etc per MoveBombTarget line ~1209
                    AiAirWayPointType aawpt = AiAirWayPointType.NORMFLY;

                    //add the mid Point
                    midaaWP = new AiAirWayPoint(ref midPos, speed);
                    //aaWP.Action = AiAirWayPointType.NORMFLY;
                    midaaWP.Action = aawpt; //same action for mid & end

                    NewWaypoints.Add(midaaWP); //do add
                    count++;

                    //Console.WriteLine("CoverFixWayPoints - adding new mid-end WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { aawpt, (midaaWP as AiAirWayPoint).Speed, midaaWP.P.x, midaaWP.P.y, midaaWP.P.z });

                    //add the final Point, which is off the map
                    endaaWP = new AiAirWayPoint(ref endPos, speed);
                    //aaWP.Action = AiAirWayPointType.NORMFLY;
                    //endaaWP.Action = AiAirWayPointType.NORMFLY;
                    endaaWP.Action = aawpt;

                    NewWaypoints.Add(endaaWP); //do add
                    count++;
                    //Console.WriteLine("CoverFixWayPoints - adding new end WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { aawpt, (endaaWP as AiAirWayPoint).Speed, endaaWP.P.x, endaaWP.P.y, endaaWP.P.z });
                }
            }


            if (update)
            {
                //Console.WriteLine("MBTITG: Updating this course");
                airGroup.SetWay(NewWaypoints.ToArray());

                //for testing
                /*
                try
                {
                    foreach (AiWayPoint wp in NewWaypoints)
                    {
                        Console.WriteLine("FixWayPointsCover - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                    }
                }
                catch (Exception ex) { Console.WriteLine("Cover/MoveBomb FixWayPoints print: " + ex.ToString()); }
                */


            }
        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb FixWayPoints: " + ex.ToString()); }
    }
	
	
	public System.Threading.Timer COVER_CheckSplits_Timer;
    public readonly int COVER_CheckSplits_TimerPeriod_ms = 120164; //2 minutes

    //returns false if it's been turned off or true if turned on.
    public void CheckSplits_Timer_init ()
    {
		Console.WriteLine("COVERMISSION COVER_CheckSplits_Timer: Starting timer! " + DateTime.UtcNow.ToString("T"));
        
        COVER_CheckSplits_Timer = new System.Threading.Timer(
           new TimerCallback(CheckSplits),
		   null,           
           dueTime: 240000, //wait time @ first startup (ms).  
           period: COVER_CheckSplits_TimerPeriod_ms);        
    }
	
	//Checks periodically to see if any new "daughter" groups have been created, split off from previous cover groups
	//If so, adds in the newly created "daughter" group as a <cover aircraft for that pilot, just
	//as before, but now there is a new group add.
	
	private void CheckSplits(object obj) {
	  for (int army =1; army<3; army ++) 	
		if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
		{
			foreach (AiAirGroup airgroup in GamePlay.gpAirGroups(army))
			{
				if (airgroup != null && airgroup.motherGroup() != null && coverAircraftAirGroupsActive.ContainsKey(airgroup.motherGroup()))
				{
					Console.WriteLine("COVER: airgroup has a mothergroup, and the mothergroup is one of the <cover airgroups, so we add the daughter group to that pilot's controlled <cover groups");
					//If the airgroup is a split-off, and if it hasn't already transferred the 
					//orders over from its motherGroup, we do it now
					if (!coverAircraftAirGroupsActive.ContainsKey(airgroup) && !coverAircraftAirGroupsOrders.ContainsKey(airgroup) && coverAircraftAirGroupsOrders.ContainsKey(airgroup.motherGroup())) 
					{ 
						coverAircraftAirGroupsOrders[airgroup] = coverAircraftAirGroupsOrders[airgroup.motherGroup()];
						Console.WriteLine("COVER: airgroup has a mothergroup, the mother group has AirGroupsOrders, and they haven't been transferred to the daughter group yet, os doing that now");
					}
					Player player = coverAircraftAirGroupsActive[airgroup.motherGroup()];
					
					//And we also add it to the active airgroups for this player
					coverAircraftAirGroupsActive.Add(airgroup, player);
					

                    addToIndexes(player, airgroup);
                    
					bool heavyBomber = false;
                    if (isHeavyBomber(airgroup) || isDiveBomber(airgroup)) heavyBomber = true;
					bool isStrikeAC = Calcs.isStrikeAC(airgroup);

					bool playerIsStrikeACwithBombs = false;
					if (player != null & player.Place() != null && player.Place() as AiAircraft != null)
						playerIsStrikeACwithBombs = (Calcs.isStrikeAC(player.Place() as AiAircraft) && Calcs.playerHasBombs(player));

					CoverACInfo acInfo = new CoverACInfo();
					acInfo.PlaneType ="(unknown)";
					if (airgroup.GetItems().Length > 0 && (airgroup.GetItems()[0] as AiAircraft) != null)  acInfo.PlaneType = CoverCalcs.GetAircraftType(airgroup.GetItems()[0] as AiAircraft);
					acInfo.IsHeavyBomber = heavyBomber;
					acInfo.IsDiveBomber = isDiveBomber(airgroup);
					acInfo.IsStrikeAC = isStrikeAC;
					acInfo.IsPlayerStrikeAC = Calcs.isStrikeAC(player.Place() as AiAircraft);
					coverACInfo[airgroup] = acInfo;
                    acInfo.StartedWithCannons = airgroup.hasCourseCannon();
                    acInfo.HasCannons = airgroup.hasCourseCannon();

					double delay = 11.2354 + ran.NextDouble() * 2;
					//Console.WriteLine("1Heavybomber init: {0} {1} " + airgroup.Name() + " to " + player.Name(), heavyBomber, delay);
					//if (heavyBomber) delay = 2 * delay; //don't think we really need this
					//try
					
						keepAircraftOnTask_recurs(airgroup, AiAirGroupTask.DO_NOTHING, AiAirWayPointType.ESCORT, player, delay, heavyBomber, isStrikeAC, acInfo.IsPlayerStrikeAC, AltDiff_m: 666, AltDiff_range_m: 100, AltDiffBomber_m: -5, AltDiffBomber_range_m: 2, AltDiffPlayerEscort_m: -666, AltDiffPlayerEscort_range_m: 2); //_range is how much +/- random value ot add to the AltDiff altitude change.
                              
					
				}
			}
		}
	}

} //end class



//Various helpful calculations, formulas, etc.
public static class CoverCalcs
{
    //Various public/static methods
    //http://stackoverflow.com/questions/6499334/best-way-to-change-dictionary-key    

    private static Random clc_random = new Random();

    public static bool changeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey oldKey, TKey newKey)
    {
        TValue value;
        if (!dict.TryGetValue(oldKey, out value)) 
            return false;

        dict.Remove(oldKey);  // do not change order
        dict[newKey] = value;  // or dict.Add(newKey, value) depending on ur comfort
        return true;
    }

    //gets LAST occurence of any element of a specified string[] ; CASE INSENSITIVE
    public static int LastIndexOfAny(string test, string[] values)
    {
        int last = -1;
        test = test.ToLower();
        foreach (string item in values)
        {
            int i = test.IndexOf(item.ToLower());
            if (i >= 0)
            {
                if (last > 0)
                {
                    if (i > last)
                    {
                        last = i;
                    }
                }
                else
                {
                    last = i;
                }
            }
        }
        return last;
    }

    public static string escapeColon(string s)
    {
        return s.Replace("##", "##*").Replace(":", "##@");
    }

    public static string unescapeColon(string s)
    {
        return s.Replace("##@", ":").Replace("##*", "##");
    }

    public static string escapeSemicolon(string s)
    {
        return s.Replace("%%", "%%*").Replace(";", "%%@");
    }

    public static string unescapeSemicolon(string s)
    {
        return s.Replace("%%@", ";").Replace("%%*", "%%");
    }
    //True if EVERY char in s is a digit
    public static bool isDigit(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsDigit(c)) return false;
        }
        return true;
    }
    //Allows digits, . - + 
    public static bool isDigitOrPlusMinusPoint(string s)
    {
        foreach (char c in s)
        {
            if (!(char.IsDigit(c) || c == '.' || c == '+' || c == '-')) return false;
        }
        return true;
    }

    public static double distance(double a, double b)
    {

        return (double)Math.Sqrt(a * a + b * b);

    }

    public static double meters2miles(double a)
    {

        return (a / 1609.344);

    }

    public static double miles2meters(double a)
    {

        return (a * 1609.344);

    }
    public static double meterspsec2milesphour(double a)
    {
        return (a * 2.23694);
    }

    public static double meters2feet(double a)
    {

        return (a / 1609.344 * 5280);

    }

    //tenths = 1 means round to nearest 1000 ft or 1 Angel
    //tenths = 5 means round to A1.0 A1.5 A2.0 etc
    //tenths = 10 means round to A1.0 A1.1 A1.2 A1.3 etc
    public static double Feet2Angels(double altitude, double tenths = 1)
    {
        double altAngels = (altitude) / 1000;

        if (altAngels > 1 || tenths > 1)
            altAngels = Math.Round(altAngels*tenths, MidpointRounding.AwayFromZero)/tenths;
        else
            altAngels = 1;

        return altAngels;
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

    //returns difference angle etween two vectors; vector1 is primary, angle from primary to secondary, 0-360, angle degrees like a compass
    public static double CalculateDifferenceAngle(
                          Point3d vector1,
                          Point3d vector2)
    {




        double radAngle = Math.Atan2(vector1.x, vector1.y) - Math.Atan2(vector2.x, vector2.y);

        //Converts the radians in degrees
        double degAngle = RadiansToDegrees(radAngle);

        degAngle = 180 - degAngle; //This seems necessary to align it with compass directions (siwtch from counterclocwise to clockwise, plus the 180 makes the orientation work for v1 vs v2.
        if (degAngle < 0) degAngle = degAngle + 360;
        if (degAngle > 360) degAngle = degAngle - 360;


        return degAngle;
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

    public static Point3d rollingAverage(Point3d oldaverage, Vector3d newpoint, double rolls)
    {
        return new Point3d(rollingAverage(oldaverage.x, newpoint.x, rolls),
                            rollingAverage(oldaverage.y, newpoint.y, rolls),
                            rollingAverage(oldaverage.z, newpoint.z, rolls));


    }

    public static Point3d rollingAverage(Point3d oldaverage, Point3d newpoint, double rolls)
    {
        return new Point3d(rollingAverage(oldaverage.x, newpoint.x, rolls),
                            rollingAverage(oldaverage.y, newpoint.y, rolls),
                            rollingAverage(oldaverage.z, newpoint.z, rolls));


    }

    public static double rollingAverage(double oldaverage, double newpoint, double rolls)
    {
        return ((rolls - 1) * oldaverage + newpoint) / rolls;
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
    //Given start point, angle, distance calculate endpoint
    //Gives EndPoint in same units as startPoint & dist were in
    //(those must both be in the same units)
    //works only on x&y coordinates, just returns the .z unchanged from startPoint
    public static Point3d EndPointfromStartPointAngleDist(
                        Point3d startPoint, double angle_deg, double dist)
    {
        Point3d ret = startPoint;
        ret.x = startPoint.x + Math.Sin(CoverCalcs.DegreesToRadians(angle_deg)) * dist;
        ret.y = startPoint.y + Math.Cos(CoverCalcs.DegreesToRadians(angle_deg)) * dist;
        return ret;
    }

    //distance from a point to a line defined by two other points
    public static double distancePointToLine(
                        Point3d startPoint, Point3d endPoint, Point3d distPoint)
    {
        double denom = Math.Sqrt((endPoint.y - startPoint.y) * (endPoint.y - startPoint.y) + (endPoint.x - startPoint.x) * (endPoint.x - startPoint.x));
        if (denom == 0) return (CalculatePointDistance(distPoint, startPoint));  //both line points are same meaning line is undefined but we can give a distance to that single point
        double numer = Math.Abs((endPoint.y - startPoint.y) * distPoint.x - (endPoint.x - startPoint.x) * distPoint.y + endPoint.x * startPoint.y - endPoint.y * startPoint.x);
        return numer / denom;

    }

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
        return (bearing > 0.0 ? bearing : (360.0 + bearing));  //we want bearing to be 0-360, generally
    }
    //True if the two vectors point in roughly the same direction, i.e. the angle between them is
    //less than tolerance_deg.  Uses the cosine of the angle between them (the dot product of the
    //NORMALIZED vectors) - both must be normalized, otherwise a fast and a slow a/c flying the
    //identical heading would score differently just because of their different speeds.
    //  cos = +1 = same direction, 0 = perpendicular, -1 = opposite.
    //Useful angles:  within 15deg => >0.966, within 30deg => >0.866, within 45deg => >0.707, within 60deg => >0.5
    //Note we deliberately do NOT use Vector3d.angle() here, because we could not verify whether it
    //returns degrees or radians; this version is unambiguous either way.
    //Returns FALSE if either vector is (near) zero-length - a stationary a/c has no heading at all,
    //and dividing by ~0 would give NaN.  Every NaN comparison is false, so without this guard a
    //stopped aircraft would silently read as "not pointing that way".
    public static bool roughlySameDirection(Point3d a, Vector3d b, double tolerance_deg)
    {
        return roughlySameDirection(a, new Point3d(b.x, b.y, b.z), tolerance_deg);
    }

    public static bool roughlySameDirection(Point3d a, Point3d b, double tolerance_deg)
    {
        double lenA = CalculatePointDistance(a);  //note this is x/y only - headings here are horizontal
        double lenB = CalculatePointDistance(b);
        if (lenA < 0.0001 || lenB < 0.0001) return false;

        double cos = (a.x * b.x + a.y * b.y) / (lenA * lenB);
        //floating point can nudge this a hair outside [-1,1]; would break a later Math.Acos
        if (cos > 1.0) cos = 1.0;
        if (cos < -1.0) cos = -1.0;

        return cos > Math.Cos(DegreesToRadians(tolerance_deg));
    }

    public static double CalculatePitchDegree(Vector3d vector)
    {
        double d = distance(vector.x, vector.y);  //size of vector in x/y plane
        Vector2d matVector = new Vector2d(d, vector.z);
        // the value of direction is in rad so we need *180/Pi to get the value in degrees.  

        double pitch = (matVector.direction()) * 180.0 / Math.PI;
        return (pitch < 180 ? pitch : (pitch - 360.0)); //we want pitch to be between -180 and 180, generally
    }

    //Map bearings are 10 degrees off from magnetic headings in 1940s as modelled in CloD.
    //A compass showing 0 deg will actually be pointing to 350 deg in true degrees/on the map.
    //So for example of the desired actual heading is 90 the pilot will have to put compass on 100 to achieve that.
    public static double realBearingDegreetoCompass(double realBearing_deg)
    {
        double bearing = realBearing_deg + 10;
        return (bearing < 360.0 ? bearing : (bearing - 360.0));
    }


    public static int TimeSince2016_sec()
    {
        DateTime epochStart = new DateTime(2016, 1, 1); //we need to fit this into an int; Starting 2016/01/01 it should last longer than CloD does . . . 
        DateTime currentDate = DateTime.Now;

        long elapsedTicks = currentDate.Ticks - epochStart.Ticks;
        int elapsedSeconds = (int)(elapsedTicks / 10000000);
        return elapsedSeconds;
    }

    public static long TimeSince2016_ticks()
    {
        DateTime epochStart = new DateTime(2016, 1, 1); //we need to fit this into an int; Starting 2016/01/01 it should last longer than CloD does . . . 
        DateTime currentDate = DateTime.Now;

        long elapsedTicks = currentDate.Ticks - epochStart.Ticks;
        return elapsedTicks;
    }

    public static long TimeNow_ticks()
    {
        DateTime currentDate = DateTime.Now;
        return currentDate.Ticks;
    }

    public static string SecondsToFormattedString(int sec)
    {
        try
        {
            var timespan = TimeSpan.FromSeconds(sec);
            if (sec < 10 * 60) return timespan.ToString(@"m\mss\s");
            if (sec < 60 * 60) return timespan.ToString(@"m\m");
            if (sec < 24 * 60 * 60) return timespan.ToString(@"hh\hmm\m");
            else return timespan.ToString(@"d\dhh\hmm\m");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Calcs.SecondsToFormatted - Exception: " + ex.ToString());
            return sec.ToString();
        }
    }

    public static string correctedSectorNameDoubleKeypad(CoverMission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + doubleKeypad(p);
        return s;

    }

    public static string correctedSectorNameKeypad(CoverMission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + singleKeypad(p);
        return s;

    }

    //OK, so in order for the sector # to match up with the TWC map, and
    //to work with our "double keypad" routines listed here,
    //And (most important!) in order to make the sectors match up with EASY SIMPLE
    //squares of side 10000m in the in-game coordinate system, you must use this battle area
    //in the .mis file:
    //
    //BattleArea 10000 10000 350000 310000 10000
    //
    //Key here is the 10000,10000 which makes the origin of the battle area line up with the origin of the 
    //in-game coordinate system.
    //
    //If you wanted to change this & make the battle area smaller or something, you could just increase
    //the #s in increments of 100000.
    //The 350000 310000 is important only in that it EXACTLY matches the size of the map available in CLOD 
    //in FMB etc.  So 0 0 350000 310000 10000 exactly matches the full size of the Channel Map in CloD,
    //uses the full extent of the map, and makes the sector calculations exactly match in 10,000x10,000 meter 
    //increments.

    //This is also the way the TWC online radar map works, so if you do it that way the in-game map & offline 
    //radar map will match.

    public static string correctedSectorName(CoverMission msn, Point3d p)
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

    //Sectors range AA to BI and represents points 10000 through 360000
    //this is given our battle area defined in the .mis file and radar map we use, which uses this grid & definition:
    //
    //BattleArea 10000 10000 350000 310000 10000
    //
    //Key here is the 10000,10000 which makes the origin of the battle area line up with the origin of the 
    //in-game coordinate system.
    public static int xSector2Meters(string s)
    {
        s = s.Trim().ToUpper();
        if (s.Length == 0) return 0;
        //char[] ch = s.ToCharArray();
        List<char> ch = new List<char>(s.ToCharArray());

        //new list where we are sure each char is a letter
        //we throw out any chars that are NOT letters
        List<char> newch = new List<char>();
        foreach (char c in ch)
        {
            if (char.IsLetter(c)) newch.Add(c);
        }
        if (newch.Count == 0) return 0;
        if (newch.Count == 1) { newch.Add(newch[0]); newch[0] = ' '; } //if just one letter, then we shift it to the least significant position (to the rightmost position)
        if (newch.Count > 2) //If  more than 2 letters we only accept the right-most (least significant) two & just ignore the rest
        {
            newch[0] = newch[newch.Count - 2];
            newch[1] = newch[newch.Count - 1];
        }
        int total = 10000; //AA represents point 10000 - if map changes we'll have to change this
                           //if (ch[0] == 'A') total += 0;
                           //else if (ch[0] == 'B') total += 260000;
        int val0 = (int)(newch[0]);
        total += (val0 - 65) * 260000;

        //Console.WriteLine("xSector1: {0} {1} {2}", val0, newch[0], total);
        //Console.WriteLine("xSector: {0} {1}", ch[0], total);
        int val = (int)(newch[1]);
        //Console.WriteLine("xSector1.5: {0} {1} {2}", val, newch[1], total);
        if (val < 65 || val > 90) return 0; //upper case ASCII values range from A = 65 to Z = 90

        total += (val - 65) * 10000;
        //Console.WriteLine("xSector2: {0} {1} {2}", val, newch[1], total);
        return total;
    }
    //In TWC maps under scheme outlined above, battle area ranges 10000 10000 350000 310000 10000
    //but we could allow these to range 0 to 99 (future growth)
    public static int ySector2Meters(string s)
    {
        s = s.Trim().ToUpper();
        int i = 0;
        try { if (s.Length > 0) i = Convert.ToInt32(s); }
        catch (Exception ex) { }
        if (i < 0 || i > 99) return 0;
        int total = i * 10000;
        return total;
    }
    //keypad number for area, numbered 1-9 from bottom left to top right
    //of square size
    //Called with size = 10000 for normal CloD keypad, size = 10000/3 for mini-keypad
    //
    public static Point3d keypad2meters(int keyp, double size)
    {
        keyp -= 1;
        if (keyp < 0 || keyp > 8) return new Point3d(0, 0, 0);
        int xK = keyp % 3;
        int yK = keyp / 3; //integer division, remember
        return new Point3d((xK * size) / 3, (yK * size) / 3, 0); //div by 3 because we end up with a number 0-2 and the range (0-3) should be the full size.  If we dont' /3 then we get 3x the range we really want
    }

    //if returnCenterPoint returns the center point of the requested sector or keypad or doublekeypad area
    //if returnCenterpoint == false then the lower left corner of the area is returned
    //Works with Depending on just sector, singlekeypad, or doublekeypad area
    //Formats like: AA31.3.9 - BA3.1.3 - BD22.3 - AZ19 should all work 
    //First portion is AA29, CloD map sectors; second is each sector divided into a keypad 1-9, third is each
    //small keypad divided into a smaller keypad 1-9
    public static Point3d sectordoublekeypad2point(string s, bool returnCenterpoint = true)
    {
        Point3d retpoint = new Point3d(0, 0, 0);
        s = s.ToUpper();
        string[] sarr = s.Split('.');
        string sector = "";
        string sectorAlpha = "";
        string sectorDigits = "";
        string singlekeypad = "";
        string doublekeypad = "";
        if (sarr.Length == 0) return retpoint;

        if (sarr.Length > 0)
        {
            sector = sarr[0];
            foreach (char c in sector.ToCharArray())
            {
                if (Char.IsDigit(c)) sectorDigits += c.ToString();
                if (Char.IsLetter(c)) sectorAlpha += c.ToString();
            }
            retpoint.x += xSector2Meters(sectorAlpha);
            retpoint.y += ySector2Meters(sectorDigits);


        }
        if (sarr.Length > 1)
        {
            singlekeypad = sarr[1];
            int skint = 0;
            try { if (singlekeypad.Length > 0) skint = Convert.ToInt32(singlekeypad); }
            catch (Exception ex) { }
            Point3d singlepoint = keypad2meters(skint, 10000);
            retpoint.x += singlepoint.x;
            retpoint.y += singlepoint.y;
        }
        if (sarr.Length > 2)
        {
            doublekeypad = sarr[2];
            int dkint = 0;
            try { if (doublekeypad.Length > 0) dkint = Convert.ToInt32(doublekeypad); }
            catch (Exception ex) { }
            Point3d doublepoint = keypad2meters(dkint, 10000 / 3);
            retpoint.x += doublepoint.x;
            retpoint.y += doublepoint.y;
        }

        if (returnCenterpoint)
        {
            //We make the return point the CENTER of the requested sector rather than the corner
            if (sarr.Length > 2) { retpoint.x += 10000 / 9 / 2; retpoint.y += 10000 / 9 / 2; }
            else if (sarr.Length > 1) { retpoint.x += 10000 / 3 / 2; retpoint.y += 10000 / 3 / 2; }
            else if (sarr.Length > 0) { retpoint.x += 10000 / 2; retpoint.y += 10000 / 2; }
        }
        return retpoint;
    }


    //returns index of largest array element which is equal to OR less than the value
    //assumes a sorted list of in values. 
    //If less than the 1st element or array empty, returns -1
    public static Int32 array_find_equalorless(int[] arr, Int32 value)
    {
        if (arr == null || arr.GetLength(0) == 0 || value < arr[0]) return -1;
        int index = Array.BinarySearch(arr, value);
        if (index < 0)
        {
            index = ~index - 1;
        }
        if (index < 0) return -1;
        return index;
    }

    //Splits a long string into a maxLineLength respecting word boundaries (IF possible)
    //http://stackoverflow.com/questions/22368434/best-way-to-split-string-into-lines-with-maximum-length-without-breaking-words
    public static IEnumerable<string> SplitToLines(string stringToSplit, int maxLineLength)
    {
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
            string type = aircraft.InternalTypeName(); // eg type = "bob:Aircraft.Bf-109E-3".  FYI this is a property of AiCart inherited by AiAircraft as a descendant class.  So we could do this with any type of AiActor or AiCart
            string[] part = type.Trim().Split('.');
            result = part[1]; // get the part after the "." in the type string
        }
        return result;
    }

    public static int numPlayersInArmy(int army, CoverMission mission)
    {
        int ret = 0;
        if (mission != null && mission.GamePlay !=null && mission.GamePlay.gpRemotePlayers() != null && mission.GamePlay.gpRemotePlayers().Length > 0)
        {
            foreach (Player p in mission.GamePlay.gpRemotePlayers())
            {
                if (!p.IsConnected()) continue;
                if (p.Army() == army) ret++;
            }
        }
        return ret;
    }

    public static AiAirport GetRandomAirfieldNear(IGamePlay GamePlay, Point3d location, double distance)
    {
        List<AiAirport> CloseAirfields = new List<AiAirport>();
        if (GamePlay == null) return null;

        AiAirport[] airports = GamePlay.gpAirports();
        Point3d StartPos = location;

        if (airports != null)
        {
            foreach (AiAirport airport in airports)
            {

                if (Calcs.CalculatePointDistance(airport.Pos(), StartPos) < distance) //use 2d distance, MUCH different than 3d distance for ie high-level bombers
                    CloseAirfields.Add(airport);
            }
        }
        int ind = 0;
        if (CloseAirfields.Count > 0)
        {
            ind = clc_random.Next(CloseAirfields.Count - 1);            
            return CloseAirfields[ind];

        }
        else return null;
    }
    public static int bombCount(AiAirGroup airGroup)
    {
        int count = 0;
        if (airGroup == null) return 0;
        if (airGroup.GetItems().Length == 0) return 0;
        foreach (AiActor a in airGroup.GetItems())
        {
            if (a == null || (a as AiAircraft) == null) continue;
            AiAircraft aircraft = a as AiAircraft;            
            count += bombCount(aircraft);
            
        }
        return count;
    }

    public static int bombCount(AiAircraft a)
    {
        //NB: "ParameterTypes pt = ..." not "ParameterTypes.S_BombReserve pt = ..." - the left of a
        //declaration is a TYPE, and ParameterTypes.S_BombReserve is an enum VALUE, not a nested type.
        ParameterTypes pt = ParameterTypes.S_BombReserve;
        //NB: getParameter returns DOUBLE, so accumulate in a double and round once at the end -
        //    count += a.getParameter(...) straight into an int will not compile.
        double count = 0;
        int numToCount = 50;
        for (int i = 0; i <numToCount ; i++) {
            try {
                //Console.WriteLine ("Param {0} #{1}: {2}", pt, i, a.getParameter(pt, i));
                count += a.getParameter(pt, i);
            } catch (Exception ex) {}
        }
        return (int)Math.Round(count);
    }

    //Trying to reduce AI crashes by setting minimum alt.  70m is below radar, shouldn't be a problem.
    public static double checkMinAGL(double currZ_m, Point3d currPos, double minAGL_m = 70)
    {
        double minElev_m = Calcs.LandElevation_m(currPos) + minAGL_m;
        if (minElev_m <= currZ_m) return currZ_m;
        else return minElev_m;
    }

    public static string randSTR(string[] strings)
    {
        //Random clc_random = new Random();
        return strings[clc_random.Next(strings.Length)];
    }

    public static void loadSmokeOrFire(maddox.game.IGamePlay GamePlay, CoverMission mission, double x, double y, double z, string type, double duration_s = 300, string path = "")
    {
        /* Samples: 
         * Static555 Smoke.Environment.Smoke1 nn 63748.22 187791.27 110.00 /height 16.24
        Static556 Smoke.Environment.Smoke1 nn 63718.50 187780.80 110.00 /height 16.24
        Static557 Smoke.Environment.Smoke2 nn 63688.12 187764.03 110.00 /height 16.24
        Static534 Smoke.Environment.BuildingFireSmall nn 63432.15 187668.28 110.00 /height 15.08
        Static542 Smoke.Environment.BuildingFireBig nn 63703.02 187760.81 110.00 /height 15.08
        Static580 Smoke.Environment.BigSitySmoke_0 nn 63561.45 187794.80 110.00 /height 17.01
        Static580 Smoke.Environment.BigSitySmoke_1 nn 63561.45 187794.80 110.00 /height 17.01

        Not sure if height is above sea level or above ground level.
        */
        if (GamePlay == null) return;

        //mission.Timeout(2.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete)", new object[] { }); });
        //GamePlay.gpLogServer(null, "Setting up to delete stationary smokes in " + duration_s.ToString("0.0") + " seconds.", new object[] { });
        mission.Timeout(3.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete2)", new object[] { }); });
        mission.Timeout(4.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete3)", new object[] { }); });
        mission.Timeout(4.5, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete4)", new object[] { }); });

        mission.Timeout(5.0, () =>
        {
            //GamePlay.gpLogServer(null, "Executing the timeout (delete5)", new object[] { });
            //Point2d P = new Point2d(x, y);
            //GamePlay.gpRemoveGroundStationarys(P, 10);
        });
        /*
        mission.Timeout(duration_s, () =>
        {
            //Console.WriteLine("Deleting stationary smokes . . . ");
            GamePlay.gpLogServer(null, "Deleting stationary smokes . . . ", new object[] { });
            Point2d P = new Point2d(x, y);
            GamePlay.gpRemoveGroundStationarys(P, 10);
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(x, y, z + 1))
            {
                if (sta == null) continue;
                Console.WriteLine("Deleting , , , " + sta.Name + " " + sta.Title);
                if (sta.Name.Contains(key) || sta.Title.Contains(key)) {
                    Console.WriteLine("Deleting stationary smoke " + sta.Name + " - end of life");
                    sta.Destroy();
                }
            }


        });

     */
        //AMission mission = GamePlay as AMission;
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";
        string key = "Static1";
        string value = "Smoke.Environment." + type + " nn " + x.ToString("0.00") + " " + y.ToString("0.00") + " " + (duration_s / 60).ToString("0.0") + " /height " + z.ToString("0.00");
        f.add(sect, key, value);

        /*
        sect = "Stationary";
        key = "Static2";
        value = "Smoke.Environment." + "Smoke1" + " nn " + x.ToString("0.00") + " " + (y  + 130).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static3";
        value = "Smoke.Environment." + "Smoke2" + " nn " + x.ToString("0.00") + " " + (y + 260).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static4";
        value = "Smoke.Environment." + "BuildingFireSmall" + " nn " + x.ToString("0.00") + " " + (y + 390).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static5";
        value = "Smoke.Environment." + "BuildingFireBig" + " nn " + x.ToString("0.00") + " " + (y + 420).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static6";
        value = "Smoke.Environment." + "BigSitySmoke_0" + " nn " + x.ToString("0.00") + " " + (y + 550).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static7";
        value = "Smoke.Environment." + "BigSitySmoke_1" + " nn " + x.ToString("0.00") + " " + (y + 680).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static8";
        value = "Smoke.Environment." + "BigSitySmoke_2" + " nn " + x.ToString("0.00") + " " + (y + 710).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);
        */



        //maybe this part dies silently some times, due to f.save or perhaps section file load?  PRobably needs try/catch
        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + path + "smoke-ISectionFile.txt", new object[] { }); //testing
        //f.save(path + "smoke-ISectionFile.txt"); //testing        
        GamePlay.gpPostMissionLoad(f);


        //TODO: This part isn't working; it never finds any of the smokes again.
        //get rid of it after the specified period



    }

    public static void PrintValues(IEnumerable myList, int myWidth)
    {
        int i = myWidth;
        foreach (Object obj in myList)
        {
            if (i <= 0)
            {
                i = myWidth;
                Console.WriteLine();
            }
            i--;
            Console.Write("{0,8}", obj);
        }
        Console.WriteLine();
    }

    //for public consumption like bob:Aircraft.SpitfireMkIa_100oct ---> SpitfireMkIa 100oct
    public static string ParseTypeName(string typeName)
    {
        string[] tempString = null;
        string parsedName = "";
        tempString = typeName.Split('.');

        parsedName = tempString[1].Replace("_", " ");

        return parsedName;
    }

    //for internal use like bob:Aircraft.SpitfireMkIa_100oct ----> SpitfireMkIa_100oct
    public static string ParseTypeNameToPlainType(string typeName)
    {
        string[] tempString = null;
        string parsedName = "";
        tempString = typeName.Split('.');
        if (tempString.Length > 0) return tempString[1];
        else return typeName;
    }

    //army == 0 gets both armies
    public static AiActor[] gpGetGroundActors(CoverMission msn, int army)
    {   // Purpose: Returns an array of all the AiActors in the game.
        // Use: GamePlay.gpGetActors();
        List<AiActor> result = new List<AiActor>();

        if (msn.GamePlay == null) return result.ToArray();

        //List<int> armies = new List<int>(msn.GamePlay.gpArmies());
        List<int> armies = new List<int>() { 1, 2 };
        for (int i = 0; i < armies.Count; i++)
        {
            if (i != army && i > 0) continue;
            // ground actors
            AiGroundGroup[] agg = msn.GamePlay.gpGroundGroups(armies[i]);
            if (agg == null)
            {
                //Console.WriteLine("# it's nulL!");
                return null;
            }
            //Console.WriteLine("#" + agg.ToString());// + " " + agg.Length.ToString());
            //return null;
            if (agg == null) return null;
            List<AiGroundGroup> gg = new List<AiGroundGroup>(msn.GamePlay.gpGroundGroups(armies[i]));
            for (int j = 0; j < gg.Count; j++)
            {
                List<AiActor> act = new List<AiActor>(gg[j].GetItems());
                for (int k = 0; k < act.Count; k++)
                {
                    result.Add(act[k] as AiActor);
                    //Console.WriteLine("Actor: " + (act[k] as AiActor).Name());
                }
            }
            /*
            // air actors
            List<AiAirGroup> airgroups = new List<AiAirGroup>(IG.gpAirGroups(armies[i]));
            for (int j = 0; j < airgroups.Count; j++)
            {
                List<AiActor> act = new List<AiActor>(airgroups[j].GetItems());
                for (int k = 0; k < act.Count; k++) result.Add(act[k] as AiActor);
            }
            */
        }
        return result.ToArray();
    }

    public static void listAllGroundActors(CoverMission msn, IGamePlay gp, Player[] to = null, int missionNumber = -1, string message = "")
    {
		try {

            // --- STEP 1: SNAPSHOT EVERYTHING ON THE MAIN THREAD ---
			List<string> linesToSave = new List<string>();
			string playername = "";
            Point3d pos = new Point3d (200000,200000, 0);
			if (to != null && to.Length >  0 && to[0] != null){
                             playername = to[0].Name();
                             if (to[0].Place() != null) pos = to[0].Place().Pos();
            }
			
			string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
			
			string msg = string.Format("{0} Player: {1}",currentDateTime, playername);
			linesToSave.Add(msg);
			msg = string.Format("({0} {1} {2})", pos.x, pos.y, pos.z);
			linesToSave.Add(msg);
			if (message.Length > 0 ) linesToSave.Add(message);
			linesToSave.Add("");
			linesToSave.Add("==========================================================================================");
			linesToSave.Add("");
			
			//TODO: Make it list only the actors in that mission by prefixing "XX:" if missionNumber is included.
			if (gp == null) return;
			gp.gpLogServer(null, "Listing all ground actors (and to file sectionfiles/AllActors....txt):", new object[] { });

			int group_count = 0;
			if (gp.gpArmies() != null && gp.gpArmies().Length > 0)
			{
				foreach (int army in gp.gpArmies())
				{
					//List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
					if (gp.gpGroundGroups(army) != null && gp.gpGroundGroups(army).Length > 0)
					{
						foreach (AiGroundGroup group in gp.gpGroundGroups(army))
						{
							group_count++;
							if (group.GetItems() != null && group.GetItems().Length > 0)
							{
								//poscount = group.NOfAirc;
								foreach (AiActor actor in group.GetItems())
								{
									if (actor != null)
									{
										gp.gpLogServer(to, actor.Name(), new object[] { });
										AiGroundGroup actorSubGroup = actor as AiGroundGroup;
										if (actorSubGroup != null && (actorSubGroup).GetItems() != null && actorSubGroup.GetItems().Length > 0)
										{
											foreach (AiActor a in actorSubGroup.GetItems())
											{
												//gp.gpLogServer(null, a.Name(), new object[] { });
												msn.mainmission.gpLogServerWithDelay(to, a.Name(), null);

											}
										}
									}
								}
							}
						}
					}
				}
			}

            // Determine paths safely before leaving the thread
			string tempFile = "sectionfiles/AllActors" + currentDateTime +".txt";
			string fullPath = msn.mainmission.CLOD_PATH + msn.mainmission.FILE_PATH + tempFile;

			// --- STEP 2: OFF-LOAD ONLY DISK I/O TO THE BACKGROUND THREAD ---
			// We only pass the strings, which are immutable and 100% thread-safe.
			//Task.Run(() => {
				try 
				{
					using (StreamWriter writer = new StreamWriter(fullPath))
					{
						foreach (string line in linesToSave)
						{
							writer.WriteLine(line);
						}
					}
					// Note: gp.gpLogServer might fail inside Task.Run if it requires main-thread context. 
					// If it causes glitches, move it outside or use a game-provided tick/timeout callback.
					Console.WriteLine("Ground stationary export complete to disk.");
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error writing ground stationary file: " + ex.Message);
				}

            
		} catch (Exception ex){ Console.WriteLine("Cover/List All Ground Actors, ERROR: {0}", ex); }

    }
	
	public static void listAllGroundStationaries(CoverMission msn, IGamePlay gp, Player[] to = null, int missionNumber = -1, Point3d? initPos = null, double radius_m = -1, string saveFile = "", string message = "")
	{
		try {
			if (gp == null) return;

			GroundStationary[] gs = new GroundStationary[] {};
			Point3d pos = new Point3d (250000, 250000, 0);
			
			if (initPos.HasValue && radius_m > 0) {
				gp.gpLogServer(null, "Listing to console (and file) all ground stationaries within radius...", new object[] { });
				pos = initPos.Value;
				gs = gp.gpGroundStationarys(pos.x, pos.y, radius_m);
			} else {
				gp.gpLogServer(null, "Listing to console (and file) all ground stationaries...", new object[] { });
				gs = gp.gpGroundStationarys();
			}
			
			// --- STEP 1: SNAPSHOT EVERYTHING ON THE MAIN THREAD ---
			List<string> linesToSave = new List<string>();
			string playername = "";
			if (to != null && to.Length >  0 && to[0] != null) playername = to[0].Name();
			
			string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
			
			string msg = string.Format("{0} Player: {1}",currentDateTime, playername);
			linesToSave.Add(msg);
			msg = string.Format("({0} {1} {2}) - radius {3:N0}", pos.x, pos.y, pos.z, radius_m);
			linesToSave.Add(msg);
			if (message.Length > 0 ) linesToSave.Add(message);
			linesToSave.Add("");
			linesToSave.Add("==========================================================================================");
			linesToSave.Add("");
			

			foreach (GroundStationary a in gs)
			{
				if (a == null) continue;
				string type = "";
				string category = "";
				string name = "";
				int army = -1;
				string country = "";
			
				int ggarmy = -1;
				string ggctry = "";
				
				string gatype = "";
				
				string ganame = "";
				int gaarmy = -1;
				
				string carttype = "";
				string typename = "";
				
				bool isAlive = false;
				bool gaisAlive = false;
				Point3d aPos = new Point3d(0, 0, 0);
				Point3d gaaPos = new Point3d(0, 0, 0);
				
				GroundStationary gg = (a as GroundStationary);
				
				// Safely extract primitives while on the main thread
				if (a as AiGroundActor != null) {
					gatype = (a as AiGroundActor).Type().ToString();
					gaaPos = (a as AiActor).Pos();
					ganame = (a as AiActor).Name();
					gaarmy = (a as AiActor).Army();
					gaisAlive = (a as AiGroundActor).IsAlive();
				}
				
				
				if (a as AiCart != null) {
					carttype = (a as AiCart).InternalTypeName().ToString();
					//cartarmy = (a as AiCart).Army().
				}
				//string typename2 = a.InternalTypeName();
				if (gg != null) {
					typename = gg.Title; 
					category = gg.Category;
					type = gg.Type.ToString();
					name = gg.Name;
					aPos = gg.pos;
					isAlive = gg.IsAlive;
					country = gg.country;
					string cleanName = Calcs.CleanStationaryID(gg.Name);
					if (msn.mainmission.GroundStationary_army.ContainsKey(cleanName)) {
						ggarmy = msn.mainmission.GroundStationary_army[cleanName].Army;
						ggctry = msn.mainmission.GroundStationary_army[cleanName].Country;
					}
					
				}
                double  dist = CoverCalcs.CalculatePointDistance(gg.pos, pos);
				string line = string.Format("{0:N3} {1:N3} {2:N2} ({15:N0}m) {3} {4} {5} {6} {7} {8} {9} GA:  {10} {11} {12} {13} {14}", a.pos.x, a.pos.y, a.pos.z, name, type, typename, isAlive, country, ggarmy, ggctry, gaarmy, ganame, gatype, carttype, gaisAlive, dist );

				//string line = string.Format("Name: {0} | Type: {1} | Army: {2} | Country: {3} | Pos: {4},{5},{6} | Alive: {7}", 
				//	name, type, army, country, aPos.x.ToString("F2"), aPos.y.ToString("F2"), aPos.z.ToString("F2"), isAlive);
				
				linesToSave.Add(line);

				// Immediate game logging/chat must stay on the main thread
				/* if (to != null) {
					//foreach (Player p in to) {
						gp.gpLogServer(to, line, new object[] { });
					//}
				}*/
			}

			// Determine paths safely before leaving the thread
			string tempFile = string.IsNullOrEmpty(saveFile) ? "tempfile.txt" : saveFile;
			string fullPath = msn.mainmission.CLOD_PATH + msn.mainmission.FILE_PATH + tempFile;

			// --- STEP 2: OFF-LOAD ONLY DISK I/O TO THE BACKGROUND THREAD ---
			// We only pass the strings, which are immutable and 100% thread-safe.
			//Task.Run(() => {
				try 
				{
					using (StreamWriter writer = new StreamWriter(fullPath))
					{
						foreach (string line in linesToSave)
						{
							writer.WriteLine(line);
						}
					}
					// Note: gp.gpLogServer might fail inside Task.Run if it requires main-thread context. 
					// If it causes glitches, move it outside or use a game-provided tick/timeout callback.
					Console.WriteLine("Ground stationary export complete to disk.");
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error writing ground stationary file: " + ex.Message);
				}
			//});
		} catch (Exception ex){ Console.WriteLine("Cover/List All Ground Actors, ERROR: {0}", ex); }
	}
	
	/*
	
	//List ALL ground stationaries OR those within a radius OR save them to a file
    public static void listAllGroundStationaries(CoverMission msn, IGamePlay gp, Player[] to = null, int missionNumber = -1, Point3d? initPos = null, double radius_m	= -1, string saveFile = "")
    {
		// Point3d pos, double radius
		GroundStationary[] gs = new GroundStationary[] {};
		
		Point3d pos = new Point3d (250000, 250000, 0);
		
		if (initPos.HasValue && radius_m > 0) {
			gp.gpLogServer(null, "Listing all ground stationaries within , per CloD:", new object[] { });
			pos = initPos.Value;
			gs =  gp.gpGroundStationarys(pos.x, pos.y, radius_m);
		} else {
			gp.gpLogServer(null, "Listing all ground stationaries, per CloD:", new object[] { });
			gs = gp.gpGroundStationarys();
		}
        
        if (gp == null) return;
		
		Task.Run( () => {
		
			// 1. Get the directory where the script/application is currently running
			//string baseDir = AppDomain.CurrentDomain.BaseDirectory;
			
			string tempFile = saveFile;
			if (saveFile == "") tempFile = "tempfile.txt";

			// 3. Combine them safely to create the absolute path
			string fullPath = msn.mainmission.CLOD_PATH + msn.mainmission.FILE_PATH+tempFile;
			//string fullPath = tempFile;
			Console.WriteLine ("Saving groundstationary list to: {0}", fullPath);

			// 4. Automatically create the subdirectory if it does not exist

			
			using (StreamWriter writer = new StreamWriter(fullPath))
			{
			
			
				foreach (GroundStationary a in gs)
				{
					if (a == null) continue;
					string type = "";
					string category = "";
					string name = "";
					int army = -1;
					string country = "";
					bool isAlive = false;
					Point3d aPos = new Point3d (0,0,0);
					
					if (a as AiGroundActor != null) {
						type = (a as AiGroundActor).Type().ToString();
						aPos = (a as AiActor).Pos();
						name = (a as AiActor).Name();
						army = (a as AiActor).Army();
						isAlive = (a as AiGroundActor).IsAlive();
					}
					string typename = "";
					if (a as AiCart != null) typename = (a as AiCart).InternalTypeName().ToString();
					//string typename2 = a.InternalTypeName();
					if (a as GroundStationary != null) {
						typename = gg.Title; 
						category = gg.Category;
						type = gg.Type.ToString();
						name = gg.Name;
						aPos = gg.pos;
						isAlive = gg.IsAlive;
						country = gg.country;
					}
					string msg = string.Format("{0:N3} {1:N3} {2:N3} {8} {3} {4} {5} {6} {7}", a.pos.x, a.pos.y, a.pos.z, type, typename, isAlive, army, country, name);
					 if (saveFile=="") gp.gpLogServer(to, msg, null);
					 else {
						 writer.WriteLine(msg);
					 }
				}
			}
		});
    }
	*/

    private static int maxStatics = 10000; //so, these are guesses or somewhat reasonable maximums & might be wrong . . .
    private static int maxSubmissions = 4000;
    //gets all ground actors.  Nothing in the CloD code can do this, it misses things like static ships
    //It gets them all without worrying about army or which side they're on.  We only care if they are close to a certain point, so army is irrelevant (& many are neutral etc, so it's complicated).
    //THIS HAS HUGE POTENTIAL TO CAUSE WARPING AND RUBBER BANDING. It must be run on a background thread, ie via Task.Run.
    //3 pronged approach:
    //#1. get clods list by drilling down into list of groundactor groups
    //#2. get all actors that appear via onactorcreated (groundactos & others)
    //#3. run through the list of missions loaded & check EVERY static name like 114:static14
    //this is the routine below. This catches other slightly odd things that CloD makes but does not
    //put through OnActorCreated.  Like a the LOAD on the back of a truck, or some static/actor ships
    //-->Then merged all 3 of these lists together/union 
    public static AiActor[] gpGetAllGroundActors(CoverMission msn, int lastMissionLoaded = 0)
    {
        List<AiActor> result = new List<AiActor>();
        if (msn.GamePlay == null) return result.ToArray();
        
		List<int> armies = new List<int>(msn.GamePlay.gpArmies());
		
		if (lastMissionLoaded == 0) lastMissionLoaded = msn.GamePlay.gpNextMissionNumber();
        //List<int> armies = new List<int>() { 1, 2 };
        if (msn.mainmission.AllGroundDict!= null) result = msn.mainmission.AllGroundDict.Values.ToList();        // everything picked up by onactorcreated
        for (int s = 0; s < lastMissionLoaded + 2; s++)
        {
            for (int i = 0; i < maxStatics; i++)
            {
                string subName = s.ToString() + ":" + "Static" + (i).ToString();

                AiGroundActor subActor = msn.GamePlay.gpActorByName(subName) as AiGroundActor;
                if (subActor != null)
                {
                    if (msn.mainmission.ON_TESTSERVER && !result.Contains(subActor)) Console.WriteLine("gpGetAllGroundActors found a new GroundStationary (not from onactorcreated)) {0} - {1} ", subActor.Name(), (subActor as AiGroundActor).Type().ToString());
                    result.Add(subActor);
                    
                }
                //GroundStationary subStat = msn.GamePlay.gpActorByName(subName) as GroundStationary;
                /*
                if (subStat != null)
                {
                    Console.WriteLine("GroundStationary {0} ", subStat.Name);
                }
                */
            }
        }

        return result.ToArray();
    }
    public static AiActor[] gpGetAllGroundActorsNear(AiActor[] aia, Point3d pos, double radius)
    { try
        {
            List<AiActor> result = new List<AiActor>();
            if (aia == null || aia.Length == 0) return new AiActor[] { };
            foreach (AiActor a in aia)
            {
                if (CoverCalcs.CalculatePointDistance(a.Pos(), pos) < radius) result.Add(a);
            }
            return result.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine("gpGetAllGroundActorsNear ERROR " + ex.ToString());
            return new AiActor[] { };
        }
    }
	/*
	public static GroundStationary[] gpGetAllGroundStationariesNear(AiActor[] aia, Point3d pos, double radius)
    { try
        {
            List<AiActor> result = new List<AiActor>();
            if (aia == null || aia.Length == 0) return new AiActor[] { };
            foreach (AiActor a in aia)
            {
                if (CoverCalcs.CalculatePointDistance(a.Pos(), pos) < radius) result.Add(a);
            }
            return result.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine("gpGetAllGroundActorsNear ERROR " + ex.ToString());
            return new AiActor[] { };
        }
    }*/
	public static bool areCratersBuildingsFactoriesNear(Mission msn, Point3d pos, double radius_m, AiAirGroup airGroup = null)
    { try
        {
            if (airGroup != null && airGroup.hasBombs()) return false; //we don't need to worry about this when the a/c has bombs; they still work. It's shooting that is the problem.
            List<GroundStationary> stationaries = msn.GamePlay.gpGroundStationarys(pos.x, pos.y, radius_m).ToList();
            
                foreach (GroundStationary gg in stationaries) {
                    string tt = gg.Title.ToLower();
                    if (tt.Contains("crater") 
                        || tt.Contains("building") 
                        || tt.Contains("factory") 
                        || tt.Contains("hangar")) 
                        {
                            if (msn.ON_TESTSERVER) Console.WriteLine("areCratersBuildings... FOUND at {0:n0} {1:N0} radius {2:N0}", pos.x, pos.y, radius_m  );
                            return true;
                        }
                }
                return false;



        }
        catch (Exception ex)
        {
            Console.WriteLine("areCratersBuildingsNear ERROR " + ex.ToString());
            return false;
        }
    }
	
    public static void Shuffle<T>(this IList<T> list)
    {
        if (list == null || list.Count == 0) return;
        for (var i = 0; i < list.Count; i++)
            list.Swap(i, clc_random.Next(i, list.Count));

        
    }

    /*
     * // so this function is already added in -main.cs class Calcs so if we have everything together in the same namespace etc we don't need this again here
    private static void Swap<T>(this IList<T> list, int i, int j)
    {
        var temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
    */

} // END Class CoverMission