#define DEBUG  
#define TRACE  
//$reference System.Core.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference parts/core/CloDMissionCommunicator.dll
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

using TWCComms;

/*   TODO:
 *   
 *   They are not climbing above 500 meters for some reason.  Maybe because of the speed restriction on currentposwaypoint? It's baffling.
 *   cover A/C catch up OK but seem to speed away once they are in front.
 *   
 *   PROBABLY need to put each cover/bomber group in a certain spot a little left/right of the main a/c instead of letting them all fight it out for the same spot
 *   
 *   Airgroups larger then 2 seem to split up here & there.  So, we need ot keep track of the split-off airgroups & do something with them to keep track of them.
 *   Right now they are just split off from the airgroup we are keeping track off and then we lose control of it.
 * 
 *   Doesn't auto-send all aircraft back to stock when battle ends.  (Because of delay after player leaves game to allow bombing runs to continue. OnBattleStoped doesn't do it because it comes along too late.)
 *   Needs to register function landAllCoverAircraft(); with TWCComs and then call it in SaveMapState somewhere before:
 *      if (TWCSupplyMission != null) TWCSupplyMission.SupplyEndMission(redMult, blueMult);
 * 
 * ******************************************/


public class Mission : AMission, ICoverMission
{
    public IMainMission TWCMainMission;
    public ISupplyMission TWCSupplyMission;
    public IStatsMission TWCStatsMission;
    public IStbStatRecorder TWCStbStatRecorder;
    public IKnickebeinMission TWCKnickebeinMission;
    public Random ran;
    public int minimumAircraftRequiredForCoverDuty { get; set; }
    public int maximumAircraftAllowedPerMission { get; set; }
    public int maximumCheckoutsAllowedAtOnce { get; set; }
    public int maxPlayersToAllowCover { get; set; } //Number of players online in players' army, above this number no cover will be allowed
    public int numPlayersToReduceCover { get; set; } //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover
    public int numPlayersToIncreaseCover { get; set; } //below this number there are additional cover a/c available.  usually this is small, like = 1, 2, 3 - lower than numPlayersToReduceCover

    public Dictionary<Player, int> numberCoverAircraftActorsCheckedOutWholeMission = new Dictionary<Player, int>();
    public Dictionary<AiActor, Player> coverAircraftActorsCheckedOut = new Dictionary<AiActor, Player>();
    public Dictionary<AiAirGroup, Player> coverAircraftAirGroupsActive = new Dictionary<AiAirGroup, Player>();
    public Dictionary<AiAirGroup, Point3d> coverAircraftAirGroupsTargetPoint = new Dictionary<AiAirGroup, Point3d>();
    public Dictionary<AiAirGroup, bool> coverAircraftAirGroupsReleased = new Dictionary<AiAirGroup, bool>(); //When pilots die, bombers can continue to attack for 5mins or so more; this sets the time to release them

    //Map boundaries - these should match what you set in the .mis file; these are the values that work with TWC radar etc
    double twcmap_minX = 10000;
    double twcmap_minY = 10000;
    double twcmap_maxX = 360000;
    double twcmap_maxY = 310000;

    public Mission()
    {
        try
        {
            TWCMainMission = TWCComms.Communicator.Instance.Main;
            TWCComms.Communicator.Instance.Cover = (ICoverMission)this; //allows -stats.cs to access this instance of Mission                        

            //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
            ran = new Random();

            MissionNumberListener = -1;
            minimumAircraftRequiredForCoverDuty = 50; //2020-01; was 200
            maximumAircraftAllowedPerMission = 20; //2020-01; was 6, then 10
            //maximumAircraftAllowedPerMission = 136; //for testing        
            maximumCheckoutsAllowedAtOnce = 8; //this was flights when flights were set to 2, but now is aircraft (the # of a/c per flight can be set per user)
            maxPlayersToAllowCover = 26; //2020-01; was 12 //Number of players online in players' army, above this number no cover will be allowed
            numPlayersToReduceCover = 14; //2020-01; was 6 //Above this number of players online in players' army, the number of allowed cover per mission will be reduced gradually until 0 at maxPlayersToAllowCover;  Should be equal or less than maxPlayersToAllowCover or else ##errors##
            numPlayersToIncreaseCover = 8; //This number of players online in players' army OR FEWER, the number of allowed cover per mission will be increased even more;  Should be equal or less than maxPlayersToAllowCover or else ##errors##

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

    AiActor[] allStaticActors = null;
    Dictionary<string, IMissionObjective> SMissionObjectivesList = new Dictionary<string, IMissionObjective>();

    private void renewAllStaticActors_recurs()
    {
        Timeout(3*60, () => renewAllStaticActors_recurs());
        allStaticActors = Calcs.gpGetAllGroundActors(this, stb_lastMissionLoaded);
        SMissionObjectivesList = TWCMainMission.SMissionObjectivesList();
    }

    //Returns an objective point & radius that point p lies within.
    //If it lies within mroe than one objective, it chooses the objective with the smallest radius to return
    private Tuple<Point3d?,double> ObjectivesRadius_m (Point3d p) //center point, radius
    {
        double r = 10000000;
        Tuple<Point3d?, double> ret = new Tuple<Point3d?, double> ( null, 0 );
        foreach (string key in SMissionObjectivesList.Keys)
        {
            IMissionObjective mo = SMissionObjectivesList[key];
            if (Calcs.CalculatePointDistance(mo.Pos, p)< mo.TriggerDestroyRadius && mo.TriggerDestroyRadius <= r)
            {
                ret = new Tuple<Point3d?, double>(mo.Pos, mo.TriggerDestroyRadius);
                r = mo.TriggerDestroyRadius;
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
                Timeout(61, () => AddOffMapAIAircraftBackToSupply_recur());

                Timeout(15, () => setCoverAircraftCurrentlyAvailable_recurs());

                Timeout(20, () => renewAllStaticActors_recurs());

                if (GamePlay is GameDef)
                {
                    //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                    (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("Cover OnMissionLoaded(): " + ex.ToString()); }
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

        if (GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
    }

    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);

        AiActor actor = aircraft as AiActor;
        if (actor == null) return;

        if (coverAircraftActorsCheckedOut.ContainsKey(actor))
        {
            if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor);
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
            if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
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
            if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
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

            //AiActor actor = aircraft as AiActor;
            if (coverAircraftActorsCheckedOut.ContainsKey(actor))
            {
                //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was checked out by Cover");

                AiAircraft aircraft = actor as AiAircraft;

                if (aircraft != null)
                {
                    //if there are no more aircraft in this airgroup then we need to remove the airgroup from our cover list
                    int numAC = aircraft.AirGroup().NOfAirc;
                    /*int countAC = 0; //counting them up as below seems to give the same answer as NOfAirc
                    foreach (AiActor a in aircraft.AirGroup().GetItems())
                    {
                        if (a == actor || !a.IsAlive()) continue;
                        countAC++;
                    }*/

                    //Console.WriteLine("CoverOnDestroy: Counting a/c left in " + actor.Name() + " {0} {1} {2}", aircraft.AirGroup().Name(), numAC, countAC);
                    if (numAC == 0 && coverAircraftAirGroupsActive.ContainsKey(aircraft.AirGroup()))
                    {
                        coverAircraftAirGroupsActive.Remove(aircraft.AirGroup());
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
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true); //valid return; the final true is softexit & forces return of a/c even though it is still flying.
                        numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was returned to stock because left map OK.");

                    }
                    else if (Z_AltitudeAGL < 5 && GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y) == LandTypes.WATER) // ON GROUND & IN THE WATER = DEAD    
                    {
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                        //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died on water.");
                    }
                    // crash landing in solid ground

                    else if (Z_AltitudeAGL < 5 && GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y) != aircraft.Army())    // landed in enemy territory
                    {
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                        //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died in enemy territory.");

                    }
                    else if (Z_AltitudeAGL < 5 && Stb_distanceToNearestAirport(actor).Item1 > 3500)  // crash landed in friendly or neutral territory, on land, not w/i 2000 meters of an airport
                    {
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
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


                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true); //true is softexit & forces return of plane even though it is in the air etc.
                        numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                        coverAircraftActorsCheckedOut.Remove(actor);
                        //Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was returned to stock because disapparated during or after LANDING.");
                    } else
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

    //Which a/c are potentially available as cover a/c
    public Dictionary<ArmiesE, Dictionary<string, bool>> CoverAircraftInitiallyAvailable = new Dictionary<ArmiesE, Dictionary<string, bool>>
    {
        { ArmiesE.Red, new Dictionary<string,bool>() {
                         

        // {bob."aircraft as known to game name",whether available for use as an escort aircraft or not},
        //mostly, bombers aren't available as escorts, or bomber-enabled fighter variants
        //also rare or very valuable aircraft are not available
		
        {"bob:Aircraft.BeaufighterMkIF", true},
        {"bob:Aircraft.BeaufighterMkINF",true},
        {"bob:Aircraft.BlenheimMkIV", true},
        {"bob:Aircraft.BlenheimMkIVF",true},
        {"bob:Aircraft.BlenheimMkIVF_Late",true},
        {"bob:Aircraft.BlenheimMkIVNF",true},
        {"bob:Aircraft.BlenheimMkIVNF_Late",true},
        {"bob:Aircraft.BlenheimMkIV_Late",true},
        {"bob:Aircraft.WellingtonMkIc",true},
        //{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below
        {"bob:Aircraft.DH82A-2",false},
        {"bob:Aircraft.HurricaneMkI",true},
        {"bob:Aircraft.HurricaneMkI_100oct",false},
        {"bob:Aircraft.HurricaneMkI_100oct-NF",true},
        {"bob:Aircraft.HurricaneMkI_dH5-20",true},
        {"bob:Aircraft.HurricaneMkI_dH5-20_100oct",true},
        {"bob:Aircraft.HurricaneMkI_FB",true},
        {"bob:Aircraft.SpitfireMkI",true},
        {"bob:Aircraft.SpitfireMkIa",true},
        {"bob:Aircraft.SpitfireMkIa_100oct",false},
        {"bob:Aircraft.SpitfireMkI_100oct",true},
        {"bob:Aircraft.SpitfireMkIIa",false}
        } },
        { ArmiesE.Blue, new Dictionary <string,bool>(){
        {"bob:Aircraft.Bf-109E-1",true},
        {"bob:Aircraft.Bf-109E-1B",true},
        {"bob:Aircraft.Bf-109E-3",true},
        {"bob:Aircraft.Bf-109E-3B",true},
        {"bob:Aircraft.Bf-109E-4",false},
        {"bob:Aircraft.Bf-109E-4_Late",false},
        {"bob:Aircraft.Bf-110C-2",true},
        {"bob:Aircraft.Bf-110C-4",true},
        {"bob:Aircraft.Bf-110C-4-NJG",true},
        {"bob:Aircraft.Bf-110C-4B" ,true},
        {"bob:Aircraft.Bf-110C-4Late",false},
        {"bob:Aircraft.Bf-110C-4N",true},
        {"bob:Aircraft.Bf-110C-6",true},  //These crash straight into the ground upon spawn FOR SOME UNKNOWN REASON so just eliminating their use altogether here.
        {"bob:Aircraft.Bf-110C-7",false},
        {"bob:Aircraft.BR-20M",true},	
        //{"bob:Aircraft.DH82A-1",10},  aircraft not on list aren't allowed as escorts, so disallow by either setting to FALSE or just remming out their line
        {"bob:Aircraft.DH82A-2",false},
        {"bob:Aircraft.G50",true},
        {"bob:Aircraft.He-111H-2",true},
        {"bob:Aircraft.He-111P-2",true},
        {"bob:Aircraft.Ju-87B-2",true},
        {"bob:Aircraft.Ju-88A-1",true},
        {"bob:Aircraft.Do-17Z-2",true},
        {"bob:Aircraft.Bf-109E-4B",false},
        {"bob:Aircraft.Bf-109E-4B_Late",false},
        {"bob:Aircraft.Bf-109E-4N",false},
        {"bob:Aircraft.Bf-109E-4N_Late",false},
        }
        }

        };

    private void setCoverAircraftCurrentlyAvailable_recurs()
    {
        Timeout(60 * 10.123125, () => setCoverAircraftCurrentlyAvailable_recurs());
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
                if (CoverAircraftInitiallyAvailable[army][acName]) {
                    if (TWCSupplyMission != null)
                    {
                        int numRemaining = TWCSupplyMission.AircraftStockRemaining(acName, (int)army);
                        //Console.WriteLine("Cover: Setting cover aircraft currently available for {0} {1} {2} {3}", army, acName, CoverAircraftInitiallyAvailable[army][acName], numRemaining);
                        if (numRemaining > minimumAircraftRequiredForCoverDuty) CoverAircraftCurrentlyAvailable[army][acName] = numRemaining;

                    } else CoverAircraftCurrentlyAvailable[army][acName] = 9999;
                }
            }
        }
    }

    //Returns shift_m (amount to shift this group right or left, in meters +/right or -/left), position slot (1 slot for each aircraft earlier on this list than this one, sorted into even=+/right and odd=-/left positions), the position of this airgroup in the list of its type for this player (bombers OR fighters), the position of this airgroup overall for this player (counting Bomber Groups AND fighter groups).
    //This is a simple/easy routine & we want to recalc it each time the cover/bomber a/c position & course is recalculated because it can change over time as aircraft or airgroups are added or crash/shot down, etc

    public Tuple<double, int, int, int> aircraftPositionAndNumber(AiAirGroup airGroup, Player player)
    {
        //Blenheim wingspan is 17m;     JU88 18m; HE111 22.5m; DO217 19 m; Wellington 26 m
        //Beaufighter 17 m; HE110 16.25
        int amtToShiftForEachBomber_m = 36;
        int amtToShiftForEachFighter_m = 27;

        int totalRight = 1; //1 for the position taken in the center by the player's bomber; also keeps the first left & first right a/c from occupying the same spot ( in the center )
        int totalLeft = 1; //1 for the position taken in the center by the player's bomber
        int count = 0;  //count of a/c of this type (fighter or bomber)
        int allcount = 0; //count of all a/c for this player
        bool type = isHeavyBomber(airGroup) || isDiveBomber(airGroup); //whether bomber or fighter
        foreach (AiAirGroup ag in coverAircraftAirGroupsActive.Keys)
        {
            if (coverAircraftAirGroupsActive[airGroup] != player) continue;
            if (airGroup.GetItems().Length == 0) continue;

            if (ag == airGroup) break;
            allcount++;
            bool agtype = isHeavyBomber(ag) || isDiveBomber(ag); //whether bomber or fighter.  We're counting dive bombers as bombers now.
            if (agtype != type) continue;
            count++;

            //putting even numbered ac/groups to the right of the main a/c; odd numbered to the left
            if (count % 2 == 0)
            {
                totalRight += ag.NOfAirc;
            }
            else
            {
                totalLeft += ag.NOfAirc;
            }
        }
        count++; //to count the current airGroup; we need to know whether odd or even.
        allcount++;
        int pos = 0;
        if (count % 2 == 0) pos = totalRight;
        else pos = -totalLeft;

        double shiftamt = amtToShiftForEachBomber_m;
        if (!type) shiftamt = amtToShiftForEachFighter_m;
        double shift_m = pos * shiftamt;
        //GamePlay.gpLogServer(new Player[] { player }, "ACPos: {0:F1} {1} {2} {3}", new object[] { shift_m, pos, count, allcount }); 

        return new Tuple<double, int, int, int>(shift_m, pos, count, allcount);


    }

    //so, we shift the escort fighter or bomber aircraft left or right a bit to allow all groups to have some horizontal space.
    //When we go to bomber targeting mode (knickebein point ON), the bombers can't shift their target point left/right or they'll miss the target point.  And that target point is the same for all bomber groups.  So in that case we shift them up/down a bit  in altitude, so they can more easily avoid crashing into each other, while all still targeting the same target point.
    public enum offsetDirection { left_right, up_down };
    public Point3d calcOffset_m(Point3d CurrentPos, AiAirGroup airGroup, Player player, Vector3d Vwld, double vel_mps, offsetDirection dir = offsetDirection.left_right)
    {
        Tuple<double, int, int, int> shifts = aircraftPositionAndNumber(airGroup, player);
        double shift_m = shifts.Item1;
        //double shiftvert_m = shifts.Item2 * 40;
        double shiftvert_m = (shifts.Item2 % 2 * 40 - 20) * (shifts.Item3 % 2 * 2 - 1); //up-down-up-down pattern on each side, but swapped direction left/right sides

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

        Point3d res = Calcs.rollingAverage(storedRollingAverages[key], newpoint, rolls);
        storedRollingAverages[key] = res;
        return res;
    }

    public string listPositionCurrentCoverAircraft(Player player = null, bool display = true, bool html = false)
    {


        string nl = Environment.NewLine;
        if (html) nl = "<br>" + nl;
        string retmsg = "";
        int count = 0;
        AiActor playerPlace = player.Place();


        Vector3d player_vwld = (player.Place() as AiAircraft).AirGroup().Vwld();

        double player_vel_mph = Calcs.meterspsec2milesphour(Calcs.distance(player_vwld.x, player_vwld.y));
        double player_vel_kph = Calcs.miles2meters(player_vel_mph) / 1000;
        string player_vel = ((double)(Calcs.RoundInterval(player_vel_mph * 1, 5)) / 1).ToString("F0") + "mph ";
        if (player.Army() == 2) player_vel = ((double)(Calcs.RoundInterval(player_vel_kph * 1, 5)) / 1).ToString("F0") + "kph";

        string smsg = ">>>> Your current speed: " + player_vel + ". Your current cover airgroups:";
        retmsg += smsg + nl;


        if (!isHeavyBomber(playerPlace as AiAircraft) && !isDiveBomber(playerPlace as AiAircraft) && admin_privilege_level(player) < 1)
        {
            string m = "****No Cover info - Cover provided for heavy bombers or dive bombers only!****";
            GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });
            return m;
        }

        GamePlay.gpLogServer(new Player[] { player }, smsg, null);
        foreach (AiAirGroup airGroup in coverAircraftAirGroupsActive.Keys)
        {
            if (coverAircraftAirGroupsActive[airGroup] != player) continue;
            if (airGroup.GetItems().Length == 0) continue;
            AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;

            count++;
            double alt_m = aircraft.Pos().z;
            double alt_km = alt_m / 1000;
            double alt_angels = Calcs.Feet2Angels(Calcs.meters2feet(alt_m));
            string alt_msg = string.Format("{0:N0} m, ", alt_m);
            if (player.Army() == 1) alt_msg = string.Format("Angels {0:N0}, ", alt_angels);
            string msg = "";

            if (playerPlace == null) msg = "#" + count.ToString() + " " + Calcs.GetAircraftType(aircraft) + " at " + alt_msg + Calcs.correctedSectorNameDoubleKeypad(this, aircraft.Pos());
            else
            {
                double dis_m = Calcs.CalculatePointDistance(playerPlace.Pos(), aircraft.Pos());
                double dis_mi = (Calcs.meters2miles(dis_m));
                int dis_10 = (int)dis_mi;
                double bearing = Calcs.CalculateGradientAngle(playerPlace.Pos(), aircraft.Pos());
                double bearing_10 = Calcs.GetDegreesIn10Step(bearing);
                string ang = "A" + alt_angels.ToString("F0") + " ";
                string mi = dis_mi.ToString("F0") + "mi";
                string mi_10 = dis_10.ToString("F0") + "mi";
                double vel_mph = Calcs.meterspsec2milesphour(Calcs.distance(airGroup.Vwld().x, airGroup.Vwld().y));
                double vel_kph = Calcs.miles2meters(vel_mph) / 1000;
                string vel = ((double)(Calcs.RoundInterval(vel_mph * 1, 5)) / 1).ToString("F0") + "mph ";
                string numAC = airGroup.NOfAirc.ToString();

                if (player.Army() == 2) //metric for the Germanos . . . 
                {
                    mi = (dis_m / 1000).ToString("F0") + "k";
                    mi_10 = mi;
                    if (dis_m > 30000) mi_10 = ((double)(Calcs.RoundInterval(dis_m, 10000)) / 1000).ToString("F0") + "k";

                    //ft = alt_km.ToString("F2") + "k ";                                        
                    ang = ((double)(Calcs.RoundInterval(alt_km * 10, 5)) / 10).ToString("F1") + "k ";
                    vel = ((double)(Calcs.RoundInterval(vel_kph * 10, 5)) / 10).ToString("F0") + "kph ";
                }
                msg = "#" + count.ToString() + " " + mi_10 + bearing_10.ToString("F0") + "°" + ang + " " + vel + " - " + numAC + "x" + Calcs.GetAircraftType(aircraft);
            }

            //AiAirGroupTask task = airGroup.getTask();
            //string tsk = task.ToString();
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
            int currWay = airGroup.GetCurrentWayPoint();
            string bomb = " No bombs ";
            if (airGroup.hasBombs()) bomb = " Has bombs ";
            string action = "";
            if (CurrentWaypoints != null && CurrentWaypoints.Length > 0 && CurrentWaypoints.Length > currWay) action = (CurrentWaypoints[currWay] as AiAirWayPoint).Action.ToString();


            //msg += " " + bomb + " " + tsk + " " + action;
            msg += bomb + action;

            GamePlay.gpLogServer(new Player[] { player }, msg, null);
            retmsg += msg + nl;
        }
        if (count == 0)
        {
            string msg2 = "(none)";
            GamePlay.gpLogServer(new Player[] { player }, msg2, null);
            retmsg += msg2 + nl;

        }
        return retmsg;
    }

    //army = ArmiesE.None lists both armies
    public string listCoverAircraftCurrentlyAvailable(ArmiesE army, Player player = null, bool display = true, bool html = false)
    {
        string nl = Environment.NewLine;
        if (html) nl = "<br>" + nl;
        string retmsg = "";
        if (army != ArmiesE.Blue && army != ArmiesE.Red) return "Cover: No cover aircraft/bombers available because you are not in an army";
        if (CoverAircraftCurrentlyAvailable[army] == null) return "Cover: Aircraft availability not initialized";

        List<ArmiesE> armylist = new List<ArmiesE>();
        if (army == ArmiesE.Blue || army == ArmiesE.Red) armylist.Add(army);
        else if (army == ArmiesE.None) { armylist.Add(ArmiesE.Red); armylist.Add(ArmiesE.Blue); }

        foreach (ArmiesE a in armylist)
        {
            string smsg = string.Format(">>>>>Available cover aircraft/bombers for {0}", a);
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, smsg, null);
            retmsg += smsg + nl;

            smsg = string.Format("ID# - Aircraft - Number remaining in supply", a);
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, smsg, null);
            retmsg += smsg + nl;

            //foreach (string acName in CoverAircraftCurrentlyAvailable[army])
            int i = 0;
            foreach (string key in CoverAircraftCurrentlyAvailable[a].Keys)
            {
                i++;
                string msg = string.Format("#{0} {1} {2}", i, Calcs.ParseTypeName(key), CoverAircraftCurrentlyAvailable[a][key]);
                if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg, null);
                retmsg += msg + nl;
            }
            if (i == 0) {
                string msg1 = string.Format("***No cover aircraft/bombers available for {0} - aircraft available for cover & bomber squadron duty only if {1} or more remain in supply. Use chat command <stock to check supply***", a, minimumAircraftRequiredForCoverDuty);
                if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg1, null);
            }
        }
        AiAircraft aircraft = player.Place() as AiAircraft;
        if (aircraft == null || (!isHeavyBomber(aircraft) && !isDiveBomber(aircraft))) {
            string m = "****No Cover info - Cover provided for heavy bombers or dive bombers only!****";
            GamePlay.gpLogServer(new Player[] { player }, m, new object[] { });
            return m;
        }

        string msg3 = acAvailableToPlayer_msg(player);
        GamePlay.gpLogServer(new Player[] { player }, msg3, new object[] { });
        retmsg += msg3 + nl;

        int numCheckedOut = numberAircraftCurrentlyCheckedOutPlayer(player);

        GamePlay.gpLogServer(new Player[] { player }, "You have {0} aircraft escorting you, of {1} maximum allowed at one time.", new object[] { numCheckedOut, maximumCheckoutsAllowedAtOnce });

        return retmsg;
    }
    public string acAvailableToPlayer_msg(Player player)
    {
        int acAvailable = acAvailableToPlayer_num(player);
        string rankExpl = "";
        if (TWCStbStatRecorder != null)
        {
            int numPlayer = Calcs.numPlayersInArmy(player.Army(), this);
            rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name()) + "and with " + numPlayer.ToString() + " friendly players online";
        }
        int acAllowedThisPlayer = acAvailable + howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);
        return string.Format("{0} remain available of your command squadron of {1} bomber & cover aircraft allowed{2}; {3} more are still in the air or being readied for re-use.", acAvailable, acAllowedThisPlayer, rankExpl, coverACStillInAirForPlayer_num(player));
    }
    public int acAvailableToPlayer_num(Player player)
    {
        int acAllowedThisPlayer = maximumAircraftAllowedPerMission;
        int numPlayer = Calcs.numPlayersInArmy(player.Army(), this);

        if (numPlayer > maxPlayersToAllowCover) { return 0; }

        if (numPlayer <= numPlayersToIncreaseCover) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling(maximumAircraftAllowedPerMission * 1.5));

        string rankExpl = "";
        if (TWCStbStatRecorder != null)
        {
            double adder = ((double)TWCStbStatRecorder.StbSr_RankAsIntFromName(player.Name()) - 1.0) / 2.0;
            if (adder < 0) adder = 0;

            acAllowedThisPlayer += Convert.ToInt32(adder);

            if (numPlayer > numPlayersToReduceCover) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling((double)acAllowedThisPlayer / 2.0)); //Ceiling to run up to nearest integer, using ceiling here is being a bit nice to pilots . . .
            if (numPlayer > ((double)maxPlayersToAllowCover - (double)numPlayersToReduceCover) / 2.0 + (double)numPlayersToReduceCover) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling((double)acAllowedThisPlayer / 3.0));
            rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name()) + "and with " + numPlayer.ToString() + " friendly players online";

        }
        int acAvailable = acAllowedThisPlayer - howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);
        if (acAvailable < 0) acAvailable = 0;
        return acAvailable;
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
    public string selectCoverPlane(string acName, ArmiesE army)
    {
        string retplane = "";
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
            count++;
            if (numChoice > 0 && count == numChoice) aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty));
            if (numChoice <= 0 && acName.Length > 0 && key.ToLowerInvariant().Contains(acName.Trim().ToLowerInvariant())) aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty)); //implement substring matching "<cover beau             

        }

        //If choice by ID# or a/c name hasn't produced any matches, then we just add all a/c available.  aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key])); makes it add a choice for each a/c available so the selection is biased to select a/c for which more are available.
        if (aircraftChoices.Count == 0) foreach (string key in CoverAircraftCurrentlyAvailable[army].Keys)
            {
                //string acn = returnCoverAircraftCurrentlyAvailable[army][key];
                //string msg = string.Format("#{0} {1} {2}", i, Calcs.ParseTypeName(CoverAircraftCurrentlyAvailable[a].Key), CoverAircraftCurrentlyAvailable[a].Entry);
                count++;
                aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty));
            }

        if (aircraftChoices.Count == 0) aircraftChoices = new List<string>(CoverAircraftCurrentlyAvailable[army].Keys);

        retplane = Calcs.randSTR(aircraftChoices.ToArray());

        return Calcs.ParseTypeNameToPlainType(retplane);

    }

    public int numberAircraftCurrentlyCheckedOutPlayer(Player player)
    {
        try
        {
            if (player == null) return 0;
            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int numret = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null || coverAircraftAirGroupsActive[airGroup] != player) continue;
                numret += airGroup.NOfAirc;
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
            if (TWCSupplyMission == null) return 0;
            if (player == null || player.Name() == null) return 0;
            string playerName = player.Name();
            int numret = 0;
            HashSet<AiActor> actorsNotCheckedIn = new HashSet<AiActor>(TWCSupplyMission.aircraftCheckedOut);

            actorsNotCheckedIn.ExceptWith(TWCSupplyMission.aircraftCheckedIn); //remove all a/c that have been checked in

            foreach (AiActor actor in actorsNotCheckedIn) //Can't use aircraftCheckedOutInfo.Keys bec it includes ALL ac, even those already checked in
            {
                Tuple<int, string, string, DateTime> item = TWCSupplyMission.aircraftCheckedOutInfo[actor];
                //Console.WriteLine("Cover, numberAircraftCurrentlyCheckedOutFromSupply, item: {0} {1} {2}", item.Item2, item.Item3, item.Item4);
                //if (!item.Item2.Contains(playerName)) continue;     //playernames item could include stuff like TWC_Flug - TWC_Fatal_Error - TWC_Fark if there are multiple ppl in the a/c // but for this purpose we're only counting a/c against them if they are the only/primary pilot.  Thinking about bombers without multiple positions, etc
                if (item.Item2 != playerName) continue;
                numret++;
            }
            return numret;


        }
        catch (Exception ex) { Console.WriteLine("Cover, numberAircraftCurrentlyCheckedOutFromSupply ERROR: " + ex.ToString()); return 0; }
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
        if (msg.StartsWith("<cland"))
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
        else if (msg.StartsWith("<glist"))
        {
            AiActor[] aia = Calcs.gpGetGroundActors(this, 1);
            //exit;
            Calcs.listAllGroundActors(GamePlay);
            foreach (AiActor a in allStaticActors) Console.WriteLine(a.Name());

        }
        else if (msg.StartsWith("<chelp2"))
        {
            string msg42 = "COVER FIGHTER & BOMBER SYSTEM - HELP PAGE 2";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "<cover 3 5 - means launch a flight of 5 aircraft of type #3";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "<cover 2 6 AS - means launch a flight of 6 aircraft of type #2, formation: ASTERN";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "Formation types: VI=Vic, V3=Vic3, AB=Abreast, AS=Astern, RI=Right echelon, LE=Left echelon";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "<cland 2 release group #2.  Get group # from Tab-4 menu or <cpos";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "Cover aircraft include cover fighters and bombers. They are available only to heavy bomber pilots.";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
        }
        else if (msg.StartsWith("<chelp"))
        {
            string msg42 = "COVER FIGHTER & BOMBER SYSTEM - HELP";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "Tab-4-4-4-4 menu OR Chat Commands <cover OR <cover Beau OR <cover 3 OR <cover 3 6 AS";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = " - launch a cover squadron of aircraft name or type # indicated. Optional: Add # of aircraft to launch and formation type.";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "Tab-4 menu OR commands <clist - list available cover fighters & ID#; <cpos - position of your current fighters";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "Tab-4 menu OR command <cland - release cover fighters to land (IMPORTANT!)";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "<chelp2 for more...";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
        }

        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1.6; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.2;

            string msg41 = "<cover - request cover fighters to join you";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
    }
    private bool isHeavyBomber(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = Calcs.GetAircraftType(aircraft);
        return isHeavyBomber(acType);
    }
    private bool isHeavyBomber(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isHeavyBomber(aircraft);

    }
    private bool isHeavyBomber(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkIV") || acType.Contains("Do-17") || acType.Contains("Wellington") || acType.Contains("HurricaneMkI_FB")) ret = true;
        if (acType.Contains("BlenheimMkIVF") || acType.Contains("BlenheimMkIVNF")) ret = false;
        return ret;
    }
    private bool isDiveBomber(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = Calcs.GetAircraftType(aircraft);
        return isDiveBomber(acType);
    }
    private bool isDiveBomber(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isDiveBomber(aircraft);

    }
    private bool isDiveBomber(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("Ju-87")) ret = true; //only JU-87 Hurri/FB for now, but maybe more later?   
        return ret;
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
        GamePlay.gpLogServer(null, numret.ToString() + " groups of escort aircraft/bombers have been instructed to land at the nearest friendly airport and returned to General Supply.", new object[] { });
    }

    Dictionary<Player, int> TimeOfPlayerLastLandRequest = new Dictionary<Player, int>();

    public void landCoverAircraft(Player player)
    {
        landCoverAircraft(player, "");
    }

    public void landCoverAircraft(Player player, string msg = "")
    {
        int currTime_sec = Calcs.TimeSince2016_sec();

        //parse message to see if anything requested besides <cland [all]
        string[] sections = msg.Split(' ');

        //GamePlay.gpLogServer(new Player[] { player }, "Cover: Call " + sections.Count().ToString(), new object[] { });

        int unitToLand = -1; //-1 means, land all of them
        string unitToLand_str = "";

        if (sections.Count() > 1)
        {
            unitToLand_str = sections[1];

            try
            {
                unitToLand = Convert.ToInt32(sections[1]);
            }
            catch { unitToLand = -1; }
        }
        //System.Console.WriteLine("cover, msg :" + msg);

        //must request it 2X within 30 seconds, to prevent accidental Tab-4-4-9 cover a/c release
        //This is not required if typing <cover, though
        if (msg.Contains("<cland") || ( TimeOfPlayerLastLandRequest.ContainsKey(player) && currTime_sec - TimeOfPlayerLastLandRequest[player] < 30))
        {
            TimeOfPlayerLastLandRequest.Remove(player);
            if (player == null) return;
            AiAircraft aircraft = null;
            if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;

            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int numret = 0;
            int count = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {                
                if (airGroup == null || !coverAircraftAirGroupsActive.ContainsKey(airGroup) ||coverAircraftAirGroupsActive[airGroup] != player) continue;
                if (airGroup.GetItems().Length > 0) count++;

                if (unitToLand > 0 && count != unitToLand) continue;  //allow to instruct just one particular group to land

                if (aircraft == null) { EscortMakeLand(airGroup, null); numret++; }
                else { EscortMakeLand(airGroup, aircraft.AirGroup()); numret++; }

            }
            GamePlay.gpLogServer(new Player[] { player }, numret.ToString() + " groups of escort aircraft/bombers have been instructed to land at the nearest friendly airport.", new object[] { });
            GamePlay.gpLogServer(new Player[] { player }, "Escort aircraft & bombers will be returned to General Stock immediately but will available for use in your personal Cover Squadron again only after the aircraft actually return to base or leave the map.", new object[] { });
        } else
        {
            GamePlay.gpLogServer(new Player[] { player }, "<<<CONFIRMATION REQUIRED>>> Request Cover Aircraft release again within 30 seconds to release your aircraft.", new object[] { });
            TimeOfPlayerLastLandRequest[player] = currTime_sec;
 
        }

    }

    public void checkoutCoverAircraft(Player player, string selectString)
    {
        //try
        {

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

            string[] sections = selectString.Split(' ');
            
            //GamePlay.gpLogServer(new Player[] { player }, "Cover: Call " + sections.Count().ToString(), new object[] { });

            string aircraftName = "";
            if (sections.Count() > 0) aircraftName = sections[0];           

            int numAC = 2;
            try
            {
                if (sections.Count() > 1) numAC = Convert.ToInt32(sections[1]);
            } catch { numAC = 2; }

            //GamePlay.gpLogServer(new Player[] { player }, "Cover: numAC1 " + numAC.ToString(), new object[] { });

            int numCheckedOut = numberAircraftCurrentlyCheckedOutPlayer(player);
            if (numAC + numCheckedOut > maximumCheckoutsAllowedAtOnce) numAC = maximumCheckoutsAllowedAtOnce - numCheckedOut;
            //GamePlay.gpLogServer(new Player[] { player }, "Cover: numAC2 " + numAC.ToString(), new object[] { });

            int acAvailable = acAvailableToPlayer_num(player);

            if (acAvailable == 1) acAvailable = 2; //Always allow a final group of 2, even if only 1 remaining a/c
            if (numAC >= acAvailable ) numAC = acAvailable;

            string formation = "VIC3";
            if (sections.Count() > 2) formation = sections[2];
            formation = formation.ToUpper();

            //formation = numFlightFormation.Where(kvp => kvp.Key.Contains(formation)).Select(kvp => kvp.Key); //selects a key if the first characters are entered

            if (flightFormationAbbreviations.Keys.Contains(formation)) formation = flightFormationAbbreviations[formation];
            else
            {
                var formations = numFlightFormation.Where(kvp => kvp.Key.Contains(formation)).Select(kvp => kvp.Key); //selects a key if the first characters are entered

                if (formations.Count() > 0) formation = formations.ElementAt(0);
            }

            if (!numFlightFormation.ContainsKey(formation))
            {                
                GamePlay.gpLogServer(new Player[] { player }, "Cover ERROR: Flight formation \"" + formation + "\" does not exist! Using VIC3 instead.", new object[] { });
                formation = "VIC3";
            }
            if (formation == "VIC" && player.Army() == 1 )
            {
                GamePlay.gpLogServer(new Player[] { player }, "Cover ERROR: Flight formation \"" + formation + "\" does not work for Red aircraft! Using VIC3 instead.", new object[] { });
                formation = "VIC3";
            }


            AiAircraft aircraft = null;
            if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;
            AiActor actor = aircraft as AiActor;            

            int numInArmy = Calcs.numPlayersInArmy(player.Army(), this);

            if (numInArmy > maxPlayersToAllowCover) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - cover available only when {0} or fewer players on your side. Please ask you fellow pilots to cover you.", new object[] { maxPlayersToAllowCover }); return; }

            if (numInArmy > numPlayersToReduceCover) { GamePlay.gpLogServer(new Player[] { player }, "Note: Fewer cover aircraft/bombers available when more than {0} players on your side.", new object[] { numPlayersToReduceCover }); }

            if (aircraft == null) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - you're not in an aircraft!", new object[] { }); return; }

            //hurri FB registers as a heavy bomber so that you can lead formations of them.  But you can't lead
            //a formation as a hurriFB pilot, only heavy bombers can lead.  They could bring Hurri FB's with them, though.
            if (!isHeavyBomber(aircraft) && !isDiveBomber(aircraft) && !Calcs.GetAircraftType(aircraft).Contains("Hurricane")) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - cover provided for heavy bombers and dive bombers only!", new object[] { }); return; }

            
            if (numCheckedOut >= maximumCheckoutsAllowedAtOnce  )
            {

                GamePlay.gpLogServer(new Player[] { player }, "You already have {0} aircraft currently escorting you--the maximum allowed.", new object[] { numCheckedOut });
                GamePlay.gpLogServer(new Player[] { player }, "When you release your escorts to return to base, you may be able to check out more.", new object[] { numCheckedOut });
                GamePlay.gpLogServer(new Player[] { player }, "Use Tab-4 menu or Chat Command <cland to make your cover fighters land.", new object[] { numCheckedOut });
                return;
            }

            if (numAC <= 0) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - " + acAvailableToPlayer_msg(player), new object[] { }); return; }


            /*
             * //this isn't working, need to re-do it with coverAircraftActorsCheckedOut
            int numACInAir = numberAircraftCurrentlyCheckedOutFromSupply(player) - 1; //-1, making the reasonable assumption the player is  in an a/c right now
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

            string acName = aircraftName.Trim();
            
            Point3d loc = new Point3d(0, 0, 0);
            if (aircraft != null) loc = actor.Pos();
            loc.z += 350; //starting low, like took off from airport, but we have to make sure it is ABOVE THE ACTUAL GROUND LEVEL or else trouble.  So making it 350 meters higher than the pilot who called it in.
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

            string plane = selectCoverPlane(acName, (ArmiesE)player.Army());

            GamePlay.gpLogServer(new Player[] { player }, "Cover: Call " + numAC.ToString() + " " + plane + " in " + formation, new object[] { });
            //Point3d ac1loc = (aircraft as AiActor).Pos();

            Tuple<double, Point3d, bool> dtS = Stb_distanceToNearestAirport(aircraft as AiActor, birthplacefind: true); //<distance, airport/birthplace location, isAirSpawn> //allow birthplace to function as airport; allows cover aircraft at air spawn points

            //AiAirport ap = Stb_nearestAirport(actor.Pos(), actor.Army());
            //if (ap != null) { loc = ap.Pos(); loc.z = 150; } //starting low, as though taking off.  Not actually taking off, though
            if (dtS.Item2.x != -1 || dtS.Item2.y != -1 || dtS.Item2.z != -1 ) loc = dtS.Item2; //nearest airport location.
            loc.z = 350; //If ground airport, start the spawnees near the ground, but not too near
            if (dtS.Item3 && actor != null) loc.z = dtS.Item2.z + ran.Next(100)-50; //actor.Pos().z;//In case of airspawn we spawn them in at or near the airspawn altitude, though.            


            bool spawnInFriendlyTerritory = (player.Army() == GamePlay.gpFrontArmy(dtS.Item2.x, dtS.Item2.y));
            
            double distanceToSpawn_m = dtS.Item1 ; 
            int maxSpawnDistance_m = 2800;
            if (dtS.Item3) maxSpawnDistance_m = 4200; //in case it's an airspawn point, make the area a bit bigger
            if (!spawnInFriendlyTerritory && distanceToSpawn_m > maxSpawnDistance_m)
            {
                if (distanceToSpawn_m > maxSpawnDistance_m) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you were too far from the nearest friendly airfield to call in cover (" + distanceToSpawn_m.ToString("N0") + " meters)", new object[] { }); });
                else if (!spawnInFriendlyTerritory) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you can't call in cover at an enemy airfield.", new object[] { }); });
                return;
            }
            //regiment determines which ARMY the new aircraft will be in BOB_RAF British, BOB_LW German. BoB_RA = Italian?
            //Anyway, if we use the pilot's current regiment it matches which is nice but also definitely keeps them in the same army.
            //
            //SO problem with this scheme is that SOME unusual regiments will prevent aircraft from flying in certain formations (and thus, from being loaded at all).  This includes
            //things like observer and weather squadrons.  Don't know why!  But if the pilot has chosen (or been forced to choose, via mission design) those certain
            //regiments then sectionfileload throws an error "Bad formation for ..." and no aircraft are loaded.  The player just silently receives no aircraft.
          
            string regiment = "gb01";
            //if (army == 1) regiment = "BoB_RAF_F_141Sqn_Early";
            //if (army == 2) regiment = "BoB_LW_JG77_I";
            regiment = aircraft.Regiment().name();
            //int numAC = 2;
            //if (isHeavyBomber(plane)) numAC = 12;

            string newACActorName = Stb_LoadSubAircraft(loc: loc, type: plane, callsign: "26", hullNumber: "3", serialNumber: "001",
                                regiment: regiment, fuelStr: "", weapons: "", velocity_mps: 250, fighterbomber: "", skin_filename: "", delay_sec: "", escortedGroup: escortedGroup, numAC: numAC, formation: formation, player: player);  //higher initial velocity avoids crashes into ground etc right off the bat, especially if terrain is varied etc.



            //create the cover a/c
            Timeout(1.05, () =>
            //Timeout(0.15, () =>
            {
                //AiActor newActor = GamePlay.gpActorByName(newACActorName);
                AiActor newActor = GamePlay.gpActorByName(newACActorName);
                //Console.WriteLine("NewActorloaded: " + newActor.Name() + " for " + player.Name());
                AiAircraft newAircraft = newActor as AiAircraft;
                AiAirGroup newAirgroup = newAircraft.AirGroup();
                //Console.WriteLine("NewAirgrouploaded: " + newAirgroup.Name() + " for " + player.Name());

                if (newAirgroup != null && newAirgroup.GetItems().Length > 0)
                {
                    int itemsmade = 0;
                    string aircrafttype = "";
                    foreach (AiAircraft a in newAirgroup.GetItems())
                    {
                        //Point3d ac2loc = (a as AiActor).Pos();
                        bool supplyLimitReached = false;
                        if (TWCSupplyMission != null) supplyLimitReached = TWCSupplyMission.IsLimitReached(newActor);
                        if (supplyLimitReached)
                        {

                            GamePlay.gpLogServer(new Player[] { player }, "Supply limit reached for " + Calcs.ParseTypeName((a as AiCart).InternalTypeName()) + "; no aircraft available. Please try again to find an available aircraft.", new object[] { });
                            (a as AiCart).Destroy();
                            continue;
                        }
                        itemsmade++;
                        aircrafttype = Calcs.ParseTypeName((a as AiCart).InternalTypeName());
                        GamePlay.gpLogServer(new Player[] { player }, "Cover assigned: " + aircrafttype + " (" + (a as AiActor).Name() + ")", new object[] { });
                        //if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceEnter(player, (a as AiActor));
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyAICheckout(player, a as AiActor);
                        coverAircraftActorsCheckedOut.Add((a as AiActor), player);
                        int nca = numberCoverAircraftActorsCheckedOutWholeMission_add(player);



                    }

                    /*
                    newAirgroup.setTask(AiAirGroupTask.DEFENDING, (player.Place() as AiAircraft).AirGroup());
                    newAirgroup.changeGoalTarget(player.Place());
                    Console.WriteLine("ChangeGoalTarget: " + newAirgroup.Name() + " to " + player.Name());
                    */
                    if (itemsmade > 0)
                    {
                        string msg6 = acAvailableToPlayer_msg(player);

                        coverAircraftAirGroupsActive.Add(newAirgroup, player);
                        //keepAircraftOnTask_recurs(newAirgroup, AiAirGroupTask.ATTACK_AIR, AiAirWayPointType.AATTACK_FIGHTERS, player, 43.2354); //don't seem aggressive enough in defending with this, trying the .escort instead, with including the bomber group actor as .target
                        bool heavyBomber = false;
                        if (newAirgroup.GetItems().Length > 0 && (isHeavyBomber(newAirgroup.GetItems()[0] as AiAircraft) || isDiveBomber(newAirgroup.GetItems()[0] as AiAircraft))) heavyBomber = true;
                        double delay = 11.2354;
                        //Console.WriteLine("1Heavybomber init: {0} {1} " + newAirgroup.Name() + " to " + player.Name(), heavyBomber, delay);
                        //if (heavyBomber) delay = 2 * delay; //don't think we really need this
                        try
                        {
                            keepAircraftOnTask_recurs(newAirgroup, AiAirGroupTask.DO_NOTHING, AiAirWayPointType.ESCORT, player, delay, heavyBomber, AltDiff_m: 666, AltDiff_range_m: 100, AltDiffBomber_m: -5, AltDiffBomber_range_m: 2); //_range is how much +/- random value ot add to the AltDiff altitude change.
                            //Was AltDiffBomber_m: -14, AltDiffBomber_range_m: -45 - trying closer 2020/01/25
                            //2018/11/16 - WAS 43 seconds, trying 21 seconds instead
                            //Console.WriteLine("1recurs started");
                        }
                        catch (Exception ex) { Console.WriteLine("Cover1.5 <cover: " + ex.ToString()); }

                        GamePlay.gpLogServer(new Player[] { player }, "Your escort consists of {0} {1}s. They have just taken off from the nearest friendly airfield.", new object[] { itemsmade, aircrafttype });
                        
                        try
                        {
                            GamePlay.gpLogServer(new Player[] { player }, msg6, new object[] { });
                        }
                        catch (Exception ex) { Console.WriteLine("Cover2 <cover: " + ex.ToString()); }

                        GamePlay.gpLogServer(new Player[] { player }, "Remember to preserve your aircraft supply by instructing your escorts to land when you land, crash, or die - use Tab-4 menu or Chat Command <cland", new object[] { });

                        if (isHeavyBomber(newActor as AiAircraft))
                        {

                            Timeout(2.05, () =>

                            {
                                GamePlay.gpLogServer(new Player[] { player }, "Bombers will fly to and bomb your current active Knickebein target. If your Knickebein is turned off, bombers will wing up with you and follow you.", new object[] { });

                            });
                            Timeout(4.05, () =>

                            {
                                GamePlay.gpLogServer(new Player[] { player }, "You must stay close to your bombers or they will disengage and return to base.", new object[] { });

                            });

                        }
                    }



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
            if (numberCoverAircraftActorsCheckedOutWholeMission[player] < 0 ) numberCoverAircraftActorsCheckedOutWholeMission[player] = 0;
        }
        else numberCoverAircraftActorsCheckedOutWholeMission[player] = 0;

        return numberCoverAircraftActorsCheckedOutWholeMission[player];
    }

    private int howMany_numberCoverAircraftActorsCheckedOutWholeMission(Player player)
    {
        if (numberCoverAircraftActorsCheckedOutWholeMission.ContainsKey(player)) return numberCoverAircraftActorsCheckedOutWholeMission[player];
        else return 0;        
    }

    public void keepAircraftOnTask_recurs(AiAirGroup airGroup, AiAirGroupTask task = AiAirGroupTask.DO_NOTHING, AiAirWayPointType aawpt = AiAirWayPointType.ESCORT, Player player = null, double delay = 16.2354, bool heavyBomber = false, double AltDiff_m = 1000, double AltDiff_range_m = 100, double AltDiffBomber_m = 1000, double AltDiffBomber_range_m = 100)
    {
        AiAirGroup tasktarget = null;
        task = AiAirGroupTask.DO_NOTHING;
        //So, sometimes airgroups split up, say when under attack or landing.  If so, we just add the new group to the coverAircraftAirGroupsActive (but
        //only when the original groups was also there)
        //In this case the name of the motherGroup it split off from is in airGroup.motherGroup()
        //This little exercise ensures that we still keep control off the aircraft, and they continue to support & cover the main aircraft, even if their airGroups happen to split up.
        if (airGroup.motherGroup() != null && coverAircraftAirGroupsActive.ContainsKey(airGroup.motherGroup())) coverAircraftAirGroupsActive.Add(airGroup,
            coverAircraftAirGroupsActive[airGroup.motherGroup()]);

        if (!coverAircraftAirGroupsActive.ContainsKey(airGroup)) return;       

        Timeout(delay, ()=>keepAircraftOnTask_recurs(airGroup, task, aawpt, player, delay, heavyBomber, AltDiff_m, AltDiff_range_m, AltDiffBomber_m, AltDiffBomber_range_m));

        double AltDiffPassed_m = AltDiff_m;
        double AltDiffPassed_range_m = AltDiff_range_m;

        int numAC = airGroup.NOfAirc;
        
        if (numAC == 0)
        {
            coverAircraftAirGroupsActive.Remove(airGroup);
            //Console.WriteLine("Cover KeepAircraftOnTask: Removing airgroup {0} from active list because no more aircraft in the group", airGroup.Name());
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, "Your {0} cover group has been disbanded.", new object[] { airGroup.Name() });
            return;
            //TODO: Maybe the group splits up, maybe there are daughter groups or something?
        }

        //If player isn't in game any more, or too distant, or not in an aircraft then we release the cover a/c to land
        double distToLeadAircraft = 0;

        if (player != null && player.Place() != null && (player.Place() as AiActor) != null)
        {
            distToLeadAircraft = Calcs.CalculatePointDistance((player.Place() as AiActor).Pos(), airGroup.Pos());
        }

        Point3d oldTargetPoint = new Point3d(-1, -1, -1);
        if (coverAircraftAirGroupsTargetPoint.ContainsKey(airGroup)) oldTargetPoint = coverAircraftAirGroupsTargetPoint[airGroup];

        //Console.WriteLine("Cover KeepAconTask: dist to lead ac {0:N0}", distToLeadAircraft);
        //Console.WriteLine("Cover KeepAconTask: Cover thinking {0} {1} {2} {3:N0} ", player == null, player.Place() == null, (player.Place() as AiAircraft).AirGroup() == null, distToLeadAircraft);
        
        if (!coverAircraftAirGroupsReleased.ContainsKey(airGroup)) coverAircraftAirGroupsReleased[airGroup] = false;

        if (player == null || player.Place() == null || (player.Place() as AiAircraft).AirGroup() == null || distToLeadAircraft > 40000 )  //Was about 20,000, seemed to small. 50,000 seems too large.  
        {
            //Console.WriteLine("Cover KeepAconTask: Cover exiting {0} {1} {2} {3:N0} ", player == null, player.Place() == null, (player.Place() as AiAircraft).AirGroup() == null, distToLeadAircraft);
            AiAircraft leadAircraft = (player.Place() as AiAircraft);

            //EscortMake Land sets the necessary waypoints & also removes the a/c from coverAircraftAirGroupsActive
            if (leadAircraft == null) {

                if (heavyBomber && (oldTargetPoint.x != -1 || oldTargetPoint.y != -1))
                {
                    //This is to let any bombers on their bomb runs just continue it for 5 more minutes after the main a/c (live pilot) has been
                    //killed or crashed.  So they will continue and maybe hit the target, then be released.
                    //Timeout is bit of  kludge here, waiting 5 minutes this timer will be set a few times rather than just the once                    
                    coverAircraftAirGroupsReleased[airGroup] = false;
                    Timeout(5*60, () =>
                    {
                        coverAircraftAirGroupsTargetPoint[airGroup] = new Point3d(-1, -1, -1);
                        coverAircraftAirGroupsReleased[airGroup] = true;

                    });
                }
                else
                {
                    EscortMakeLand(airGroup, null);
                    coverAircraftAirGroupsTargetPoint[airGroup] = new Point3d(-1, -1, -1);
                    coverAircraftAirGroupsReleased[airGroup] = true;
                }

            } else {
                EscortMakeLand(airGroup, leadAircraft.AirGroup());
                coverAircraftAirGroupsTargetPoint[airGroup] = new Point3d(-1, -1, -1);
                coverAircraftAirGroupsReleased[airGroup] = true;
            }

            string acType = "escort aircraft";
            if (heavyBomber) acType = "bombers";
        
            if (coverAircraftAirGroupsReleased[airGroup] && player != null ) GamePlay.gpLogServer(new Player[] { player }, "You are too far from your {1}. The {0} group of {1} have been instructed to land at the nearest friendly airport.", new object[] { airGroup.Name(), acType });
            return;
        }

        if (heavyBomber)
        {
            //task = AiAirGroupTask.ATTACK_GROUND;
            //aawpt = AiAirWayPointType.GATTACK_POINT;
            AltDiffPassed_m = AltDiffBomber_m;
            AltDiffPassed_range_m = AltDiffBomber_range_m;

            Point3d newTargetPoint = new Point3d(-1,-1,-1);            

            if (TWCKnickebeinMission != null) newTargetPoint = TWCKnickebeinMission.KniPoint(player);

            double oldToNewTargetPointDistance_m = Calcs.CalculatePointDistance(oldTargetPoint, newTargetPoint);

            //Console.WriteLine("Cover: KBPoint, dist: {0:F0} {1:F0} {2:F0} : {3:F0} {4:F0} {5:F0} : {6:F0}  ", new object[] { newTargetPoint.x, newTargetPoint.y, newTargetPoint.z, oldTargetPoint.x, oldTargetPoint.y, oldTargetPoint.z, oldToNewTargetPointDistance_m });
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
            int currWay = airGroup.GetCurrentWayPoint();

            bool bombing = false;
            if ((CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT || (CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_TARG) bombing = true;

            //If the a/c was previous targeted at a point, and the point is still the same, and we are closer then 8km to it, and haven't bombed yet, then DON'T CHANGE IT
            //This hopefully will increase the accuracy of bombers by not messing with their final run-in
            if (bombing && (oldTargetPoint.x != -1 || oldTargetPoint.y != -1) && oldToNewTargetPointDistance_m < 10 && airGroup.hasBombs() && Calcs.CalculatePointDistance(oldTargetPoint, airGroup.Pos()) < 8000)
            {
                return;

            }

            coverAircraftAirGroupsTargetPoint[airGroup] = newTargetPoint;
        
            if ((newTargetPoint.x != -1 || newTargetPoint.y != -1) && airGroup.hasBombs())  //newTargetPoint == (-1,-1,-1) is the signal that no knickebein is set.  IN that case the a/c act just like any other escort  If a Knickebein IS set, then they go & bomb that knickebein like bombers.
            {
                //We're passing these values now as AltDiffBomber_m etc.
                //Let's try just letting them trail behind & slightly below just as they
                //Do when following the lead
                // AltDiffPassed_m = -14;
                //AltDiffPassed_range_m = 25;
                //Console.WriteLine("2ChangeGoalTarget: {0} ({1},{2})" + airGroup.Name() + " to " + player.Name(), task, Math.Round(newTargetPoint.x), Math.Round(newTargetPoint.y));
                BomberUpdateWaypoints(player, airGroup, (player.Place() as AiAircraft).AirGroup(), newTargetPoint, AiAirWayPointType.FOLLOW, AiAirWayPointType.GATTACK_POINT, AiAirWayPointType.FOLLOW, altDiff_m: AltDiffBomber_m, AltDiff_range_m: AltDiffBomber_range_m, nodupe: true);
                //airGroup.setTask(AiAirGroupTask.ATTACK_GROUND, null);
                //task = AiAirGroupTask.ATTACK_GROUND;
                //tasktarget = null;
                 
                //BomberUpdateWaypoints(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.GATTACK_GROUND, double altDiff_m = 20,
                //double AltDiff_range_m = 50, bool nodupe = true)
                return;
            }
        }

        AiAirGroup playerAirGroup = (player.Place() as AiAircraft).AirGroup();

        //Bombers will somewhat act as escorts and attack things, but not to the degree fighters will (which .ESCORT makes them do)
        //Also, bombers will jettison their bombs if they are .ESCORT and must move to defend
        //
        //with .AATACK_FIGHTERS they are pretty aggressive & attack things, which is good in a way
        //if (heavyBomber && airGroup.hasBombs()) aawpt = AiAirWayPointType.AATTACK_FIGHTERS;
        //But let's try FOLLOW to see if they will act more like bomber formations with that in place
        //bombers seem to drop bombs rather quick if they get into any trouble/attacked
        if (heavyBomber && airGroup.hasBombs()) aawpt = AiAirWayPointType.FOLLOW;  //not sure about hasBombs(), trying it without        

        //AltDiffBomber_m: 25, AltDiffBomber_range_m
        AiAirGroup attackingAirGroup = getRandomNearbyEnemyAirGroup(playerAirGroup, 4000, 1000, 2000); //escorts are supposed to be 1000m above the escorted bomber, so definitely need to attack things 1000-2000 feet (333-666m) below those bombers.  Above, add 1000m fighter altitude ot bomber alt. 

        if (attackingAirGroup != null)
        {
            //Console.WriteLine("3ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
            //if a heavy bomber with bombs, then don't go on the 
            //the attack against enemy fighters etc
            //Otherwise, attack it!
            if (heavyBomber && airGroup.hasBombs()) {            
             
               airGroup.setTask(AiAirGroupTask.DEFENDING, playerAirGroup);
               task = AiAirGroupTask.DEFENDING;
               tasktarget = playerAirGroup;
               //Console.WriteLine("4ChangeTaskbomber (after): {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
            } else {
              airGroup.setTask(AiAirGroupTask.ATTACK_AIR, attackingAirGroup);
              task = AiAirGroupTask.ATTACK_AIR;
              tasktarget = attackingAirGroup;              
              airGroup.changeGoalTarget(attackingAirGroup);
              //Console.WriteLine("4ChangeGoalTarget (after): {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
              aawpt = AiAirWayPointType.ESCORT; //THIS HELPS MAKE THEM DEFEND THE MAIN A/C
            }
        } else
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



        //So if the leader is trying to fly under the radar we make the escorts match this altitude closely.  Whether they will be able ot do this (without crashing etc) remains to be seen
        double Z_AltitudeAGL = (player.Place() as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
        if (Z_AltitudeAGL < 175)
        {
            AltDiffPassed_m = -10;
            AltDiffPassed_range_m = 5;
            //aawpt = AiAirWayPointType.AATTACK_FIGHTERS; //aawpt = AiAirWayPointType.COVER seems to work better in general but the cover aircraft stay up above the a/c they are covering and thus are seen by radar even if the main a/c is below radar. Trying AATACK_FIGHTERS to see if they will stay below radar better.
            
            if (heavyBomber && airGroup.hasBombs()) aawpt = AiAirWayPointType.FOLLOW;  //not sure about hasBombs(), trying it without
            else aawpt = AiAirWayPointType.ESCORT;
        }
        
        //Console.WriteLine("5ChangeGoalTarget: {0} hasBombs: {1} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask(), airGroup.hasBombs());
        //for just plain fighters we want .escort to be the default
        //it keeps getting switched to something else for some reason?
        if (!heavyBomber && !airGroup.hasBombs()) {
            aawpt = AiAirWayPointType.ESCORT;
        }
            //If they cover a/c are bombers and still have their bombs we try to make them fly nice & follow the leader instead of engaging
        if (heavyBomber && airGroup.hasBombs()) 
        { aawpt = AiAirWayPointType.FOLLOW;
            airGroup.setTask(AiAirGroupTask.DEFENDING, playerAirGroup);
            task = AiAirGroupTask.DEFENDING;
            tasktarget = playerAirGroup;
            //Console.WriteLine("9BomberChangeTask(before): {0} " + airGroup.Name() + " to " + player.Name(), aawpt.ToString());

        //And if the leader is too far away we try to get the escorts to disengage from whatever they are doing & follow the main a/c instead of just fighting in a furball a long ways away
        }

        //Console.WriteLine("10EscortChangeTaskTooDistantfromMain (before): is c/a too distant from lead a/c? {0:F0}m " + airGroup.Name() + " to " + player.Name(), distToLeadAircraft);
        if ((!heavyBomber || !airGroup.hasBombs()) && (distToLeadAircraft > 4000))
        {
            aawpt = AiAirWayPointType.FOLLOW; //SEtting to Escort seems to make them drop their bombs?  Maybe?
            airGroup.setTask(AiAirGroupTask.DEFENDING, playerAirGroup);
            task = AiAirGroupTask.DEFENDING;
            tasktarget = playerAirGroup;
            //Console.WriteLine("10EscortChangeTaskTooDistantfromMain (after): c/a too distant from lead a/c: {0:F0}m " + airGroup.Name() + " to " + player.Name(), distToLeadAircraft);
        }
        
        EscortUpdateWaypoints(player, airGroup, (player.Place() as AiAircraft).AirGroup(), aawpt, altDiff_m: AltDiffPassed_m, AltDiff_range_m: AltDiffPassed_range_m, nodupe: true);

        //only change task if we have specifically indicated something above
        //setting task to ATTACK_GROUND ETC SEEMS TO MAKE bombers drop their bombs?
        if (task != AiAirGroupTask.DO_NOTHING) {
            //perhaps we need to set the task AFTER the updatewaypoitns thing has happened?               
            airGroup.setTask(task, tasktarget);
        }
        
        //Console.WriteLine("8ChangeTask(after): {0} {1} {2} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask(), task.ToString(), tasktarget.ToString());            
        
        //Console.WriteLine("6ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
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

    //use loc.x = loc.y = loc.z 0 for default location
    //returns the name of the newly created a/c, which actually won't be created until the isect file is loaded, so wait 1 sec. or so before using.
    private string Stb_LoadSubAircraft(Point3d loc, string type = "SpitfireMkIa_100oct", string callsign = "26", string hullNumber = "3", string serialNumber = "001", string regiment = "gb02", string fuelStr = "", string weapons = "", double velocity_mps = 0, string fighterbomber = "", string skin_filename = "", string delay_sec = "", string escortedGroup = "", int numAC = 2, string formation = "VIC3", Player player = null)
    {
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
        //default spawn location is Bembridge, landed, 0 mph & on the ground
        string locx = "76923.96";
        //string locy = "179922.36"; //real Bembridge location
        string locy = "178322.36"; //1600 meters off Bembridge
        string locz = "0";
        string vel = "0";

        if (numAC < 1) numAC = 1;
        if (player.Army() == 1 && numAC > 24) numAC = 24; //4 flights of 6 is the max red.  (Seems to do 12 max for Red, in reality. Not sure about Blue.)
        if (player.Army() == 2 && numAC > 24) numAC = 24; //6 flights of 4 is the max for blue.  (not sure if more might be theoretically possible.)

        int numInFlight = 6;
        if (player.Army() == 2) numInFlight = 4;  //max 6 in flight for red, 4 in flight for blue.  Not sure why!

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

        if (loc.x != 0 && loc.y != 0 && loc.z != 0)
        {
            locx = (loc.x - 1000).ToString("F2"); //1000 m off the actual location
            locy = (loc.y - 1600).ToString("F2"); //1600 m off the actual location
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

        string rnumb = ".01";
        string regiment_isec = regiment + rnumb;

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
            }
            if (v.Length > 0)
            {
                k = "Flight" + flight.ToString();
                f.add(s, k, v);  //add "1 2 3 4 " . . . or similar, depending on how many a/c requested in one flight
            }
            //Console.WriteLine("CoverCreate: Flight0: " + v);
        }
        k = "Class"; v = "Aircraft." + type; f.add(s, k, v);
        k = "Formation"; v = formation; f.add(s, k, v);
        k = "CallSign"; v = callsign; f.add(s, k, v);
        //k = "Fuel"; v = fuel.ToString(); f.add(s, k, v);
        //k = "Weapons"; v = weapons; f.add(s, k, v);

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
        for (int flight = 0; flight < 4; flight++)
        {
            for (int i = 0; i < numInFlight; i++)
            {
                numACcreated++;
                if (numACcreated > numAC) break;

                string istr = i.ToString();
                if (flight > 0) istr = flight.ToString() + istr;

                f.add(s, "Serial" + istr, serialNumber + istr);
                if (skin_filename.Length > 0) f.add(s, "Skin" + istr, skin_filename);
                else f.add(s, "Skin" + istr, "default.jpg");  //Not sure if this file needs to be in the relevant a/c folder Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\PaintSchemes\Skins\MYAIRCRAFT of the user, the server, or what.  Also don't know how to find out which skin the player is currently using


                //List<string> rlist = new List<string>();
                string[] rlist = new string[8];

                for (int j = 0; j < 8; j++)
                {
                    double r = 0.9;
                    if (j == 3) r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 8.0 + 7.0 / 8.0; //number between 0.75 & 1 but weighted towards the center of that range.  j==3 means the aerial gunnery skill.
                    else r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 4.0 + 3.0 / 4.0; //number between 0.5 & 1 but weighted towards the center of that range
                    rlist[j] = r.ToString("F2");
                }
                //k = "Skill0"; v = string.Format("{0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1}", r); ; f.add(s, k, v);
                //Skills: Basic flying, advanced flying, awareness, aerial gunnery, tactics, vision, bravery, discipline
                //2020-01-22 - CHANGING DISCIPLINE SKILL to 0.98, to see if they will stay in formation more
                //Might need to do somethign different for fighter vs bomber pilots?
                //Also bravery to 0.1 as an experiment, and awareness to 0.3
                //And so, that didn't seem to do much.
                k = "Skill" + istr; v = string.Format("{0} {1} {2} {3} {4} {5} {6} 0.98", rlist); f.add(s, k, v);
                //Console.WriteLine("CoverCreate: Skill: " + v);
                //k = "Skill0"; v = string.Format("{0:F1} {0:F1} 0.3 {0:F1} {0:F1} {0:F1} {0:F1} 0.98", r); f.add(s, k, v);
                // "0.7 0.7 0.7 0.7 0.7 0.7 0.7 0.7"; 
                //r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2.0 - 1.0)) / 4.0 + 3.0 / 4.0; //number between 0.5 & 1 but weighted range
                //skill = r;
                //k = "Skill1"; v = string.Format("{0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1}", r); f.add(s, k, v);
                //k = "Skill1"; v = string.Format("{0:F1} {0:F1} 0.3 {0:F1} {0:F1} {0:F1} {0:F1} 0.98", r); f.add(s, k, v);
                //k = "Skill1"; v = "0.6 0.6 0.6 0.6 0.6 0.6 0.6 0.6"; f.add(s, k, v);

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
        v = locx + " " + locy + " " + locz + " " + vel;
        if (escortedGroup.Length > 0) v += " " + escortedGroup + " 0";  //Not sure what the final 0 does
        f.add(s, k, v);

        v = "0 0 " + locz + " " + vel;
        if (escortedGroup.Length > 0) v += " " + escortedGroup + " 0";  //Not sure what the final 0 does
        f.add(s, k, v);

        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + stb_FullPath + "aircraftSpawn-ISectionFile.txt", new object[] { }); //testing
        //f.save(stb_FullPath + "aircraftSpawn-ISectionFile.txt"); //testing
        

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX13", null); //testing disk output for warps

        //Console.Write(f.ToString());
        //load it in
        GamePlay.gpPostMissionLoad(f);
        

        string USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
        string CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover/";  // DO NOT CHANGE
        string FILE_PATH = @"missions/Multi/Fatal/";   // mission install directory (CHANGE AS NEEDED); where we save things relevant to THIS SPECIFIC MISSION
        string stb_FullPath = CLOD_PATH + FILE_PATH;
        string rnd = (ran.Next(100, 999)).ToString();
        

        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + stb_FullPath + "aircraftCover-ISectionFile" + rnd + ".txt", new object[] { }); //testing
        f.save(stb_FullPath + "aircraftCover-ISectionFile" + rnd + ".txt"); //testing

        //

        return (stb_lastMissionLoaded + 1).ToString() + ":" + regiment + ".000";  //There is a better way to do this (get the actual name via onmission loaded) but this might work for now

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
        if (type.Contains("Spitfire") || type.Contains("Hurricane"))  //We'll have to figure out what to do for DE aircraft, blennies, etc . . . 
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
        else if (type == ("WellingtonMkIc"))
        {
            ///TODO UPDATE BELTS & WEAPONS!!!!!
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 10 11 2 9 11 2 8");
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);            
            if (weapons.Length == 0)
            {
                weapons = "1 1 2";
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 50;
        }
        else if (type == ("BlenheimMkIV"))
        {
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 10 12 9 10 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 5 1 2"; //default (updated for 4.57
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
            }
            if (fuel == 0) fuel = 40;
        }
        else if (type == ("BlenheimMkIV_Late"))
        {
             //ERROR: 2020-01-18: Hook Gun combination not found!!!!! TODO!!!
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 10 11 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 5 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
            }
            if (fuel == 0) fuel = 40;
        }
        else if (type == ("BlenheimMkIVF") || type == "BlenheimMkIVNF")
        {
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 12 9 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 0 0 2"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }

            if (fuel == 0) fuel = 40;
        }
        else if (type == ("BlenheimMkIVF_Late") || type == "BlenheimMkIVNF_Late")  //still needs update 4.5 
        {
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }
            if (fuel == 0) fuel = 40;
        }
        else if (type == ("BeaufighterMkIF") || type == "BeaufighterMkINF")  //could add residuals
        {

            f.add(s, "Belt", "_Gun01 Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun03 Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 10 11 9 11 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun09 Gun.Browning303MkII MainBelt 10 11 9 11 9 0 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun08 Gun.Browning303MkII MainBelt 9 10 11 9 11 9 11 2 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 9 11 10 9 2 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 10 9 9 11 9 9 11 9 9 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 Gun.Hispano_Mk_I MainBelt 1 1 0 1 0 1 0 1 0 0");
            if (weapons.Length == 0)
            {
                weapons = "1 1"; //default                
            }
            if (fuel == 0) fuel = 35;
        }
        else if (type == "G50")
        {
            f.add(s, "Belt", "_Gun00 Gun.Breda-SAFAT-12,7mm MainBelt 7 3 0 4 6 3 0 1 4 6 Residual 50 ResidueBelt 3 4 7 4 6 7 0 3 7");
            f.add(s, "Belt", "_Gun01 Gun.Breda-SAFAT-12,7mm MainBelt 3 0 1 4 7 0 1 3 4 6 Residual 50 ResidueBelt 0 3 7 3 4 7 4 6 7");

            if (weapons.Length == 0) weapons = "1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type == "Bf-109E-1B") //we do E-1B first so that then we can cover all remaining E-1 types with type.Contains.  Similarly for E-3, E-4
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
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type == "Bf-109E-3B") //we do E-3B first so that then we can cover all remaining E-3 types with type.Contains.  Similarly for E-3, E-4
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
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-4B"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 5 1 5");

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 3"; //default
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
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 5 1 5");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }

        else if (type.Contains("He-111H")) //covers both H-2 & P-2 with just one line different
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2"); //Only the h-2 has Gun03; the P-2 lacks it & throws an error if it is included
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            //   Weapons 1 1 1 1 1 1 2

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 2"; //default
                //weapons = "1 1 1 1 1 1 4"; //default  1... 4 is the 32x50KG bombs which would be a great configuration except they drop so so so so s-l-o-w-ly from the 111P and 111H.  Like one every 200 meters.  So it never drops all 50 bombs etc.   So the 1.. x configuration is 8X250kg bombs, which it still drops slowly just the same but at least it brackets the target with those bombs and drops all of them.
                if (fighterbomber == "f") weapons = "1 1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 40;

        }
        else if (type.Contains("He-111P")) //covers both H-2 & P-2 with just one line different
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 2"; //default
                //weapons = "1 1 1 1 1 1 4"; //default  1... 4 is the 32x50KG bombs which would be a great configuration except they drop so so so so s-l-o-w-ly from the 111P and 111H.  Like one every 200 meters.  So it never drops all 50 bombs etc.   So the 1.. x configuration is 8X250kg bombs, which it still drops slowly just the same but at least it brackets the target with those bombs and drops all of them.
                if (fighterbomber == "f") weapons = "1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 40;

        }
        else if (type.Contains("110C-2"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 4 1 4");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 4 4 1");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-4"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 5 1 5");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-6")) //need to make sure this is a good load-out, copied from user-ini
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 0 1 0 0 4 4 4 4 Residual 50 ResidueBelt 0 1 0 1 0 4 1 4 4 1 4");
            f.add(s, "Belt", "_Gun04 Gun.Mk-101 MainBelt 0 2 0 2 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 0 1 0 0 4 4 4 4 Residual 50 ResidueBelt 0 1 0 1 0 4 1 4 4 1 4");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 0 1 0 0 4 4 4 4 Residual 50 ResidueBelt 0 1 0 1 0 4 1 4 4 1 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 0 1 0 0 4 4 4 4 Residual 50 ResidueBelt 0 1 0 1 0 4 1 4 4 1 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 0 1 0 0 4 4 4 4 Residual 50 ResidueBelt 0 1 0 1 0 4 1 4 4 1 4");

            if (weapons.Length == 0)
            {
                weapons = "1 1 1"; //default
                if (fighterbomber == "f") weapons = "1 1 1";
            }

            if (fuel == 0) fuel = 80;

        }
        else if (type.Contains("110C-7")) //also covers 110C-7-late
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 5 1 5");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 0 -1 " + delay_sec);
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
            f.add(s, "Belt", "_Gun00 Gun.Breda-SAFAT-7,7mm MainBelt 4 3 1 2 3 4");
            f.add(s, "Belt", "_Gun02 Gun.Breda-SAFAT-7,7mm MainBelt 1 2 3 4 1 2 3 4");

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0";
            }
            if (fuel == 0) fuel = 40;

        }
        else if (type.Contains("Ju-87"))
        {
            f.add(s, "Belt", "_Gun00 Gun.MG17_Wing MainBelt 4 4 4 0 0 0 2 5 5 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17_Wing MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");

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
        else if (type.Contains("Ju-88"))
        {
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 4 4 2 4 0 0 2 0 2 5 5 2 5 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 1 0 2 4 4 2 4 5 5 2 5 0 2 0 Residual 50 ResidueBelt 0 1 2 4 4 2 4 2 5 5 2 5 2 0 2 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 1 2 0 4 2 4 4 5 2 5 5 2 0 0 Residual 50 ResidueBelt 0 1 2 4 4 2 4 2 5 5 2 5 2 0 2 0 2");

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_A 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 2 2 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }
            if (fuel == 0) fuel = 40;

        }
        /*
         *   Class Aircraft.Do-17Z-2
  Formation VIC3
  CallSign 26
  Fuel 100
  Weapons 1 1 1 1 1 1 4 2
  Belt _Gun04 Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun05 Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun00 Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun03 Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun01 Gun.MG15 MainBelt 0 1 4 0 4
  Belt _Gun02 Gun.MG15 MainBelt 0 1 4 0 4
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
                weapons = "1 1 1 1 1 1 4 2"; //default
                if (fighterbomber == "f") weapons = "Weapons 1 1 1 1 1 1 0 0";
            }
            if (fuel == 0) fuel = 80;

        }
        k = "Weapons"; v = weapons; f.add(s, k, v);
        k = "Fuel"; v = fuel.ToString(); f.add(s, k, v);

        return f;
    }

    public void EscortMakeLand(AiAirGroup airGroup, AiAirGroup targetAirGroup = null, AiAirWayPointType aawpt = AiAirWayPointType.LANDING, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true)
    {
        if (airGroup == null || !coverAircraftAirGroupsActive.ContainsKey(airGroup)) return;
        List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
        NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, AiAirWayPointType.FOLLOW));
        NewWaypoints.Add(EscortLandingWaypoint(airGroup, targetAirGroup, AiAirWayPointType.LANDING, 0, 0, nodupe));
        airGroup.SetWay(NewWaypoints.ToArray());
        airGroup.setTask(AiAirGroupTask.LANDING, null); //try to force it . . . .
        Timeout (60, ()=>
        {
            //Console.WriteLine("Forcing LANDING: Current task: {0} " + airGroup.Name(), airGroup.getTask());
            if (airGroup != null ) airGroup.setTask(AiAirGroupTask.LANDING, null);
        });
        Timeout(120, () =>
        {
            //Console.WriteLine("Forcing LANDING: Current task: {0} " + airGroup.Name(), airGroup.getTask());
            if (airGroup != null) airGroup.setTask(AiAirGroupTask.LANDING, null);
        });
        
        //Return aircraft to supply as this is the point when the player is not longer responsible for it
        if (airGroup.GetItems().Length > 0)
        {
            foreach (AiAircraft a in airGroup.GetItems())
            {

                if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[a as AiActor], a as AiActor, 0, true); //true is softexit & forces return of plane even though it is in the air etc.
            }
        }

        coverAircraftAirGroupsActive.Remove(airGroup);

        Timeout(240, () =>
        //Timeout(6, () =>  //for testing
        {
            //Console.WriteLine("-cover Aborting LANDING: Sending off map now " + airGroup.Name(), airGroup.getTask());
            fixWayPoints(airGroup);
        });

    }

    public void BomberUpdateWaypoints(Player player, AiAirGroup airGroup, AiAirGroup targetAirGroup, Point3d newTargetPoint, AiAirWayPointType aawptstart = AiAirWayPointType.FOLLOW, AiAirWayPointType aawpttarget = AiAirWayPointType.FOLLOW, AiAirWayPointType aawptcontinue = AiAirWayPointType.FOLLOW, double altDiff_m = 20,
        double AltDiff_range_m = 50, bool nodupe = true)
    {        
        List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
        NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, aawptstart));
        Tuple<AiAirWayPoint, AiAirWayPoint> aaPs = BomberPosWaypoint(player, airGroup, targetAirGroup, newTargetPoint, aawpttarget, aawptcontinue, altDiff_m, AltDiff_range_m, nodupe);
        NewWaypoints.Add(aaPs.Item1);
        NewWaypoints.Add(aaPs.Item2);
        airGroup.SetWay(NewWaypoints.ToArray());
    }

    Dictionary<AiAirGroup, AiActor> airgroupTargets = new Dictionary<AiAirGroup, AiActor>();
    Dictionary<AiAirGroup, GroundStationary> airgroupGroundTargets = new Dictionary<AiAirGroup, GroundStationary>();
    Dictionary<AiAirGroup, Point3d> airgroupTargetPoints = new Dictionary<AiAirGroup, Point3d>();
    //AARGH

    //Dictionary<AiAirGroup, GroundStationary> airgroupTargets = new Dictionary<AiAirGroup, GroundStationary>();

    public Tuple<AiAirWayPoint, AiAirWayPoint> BomberPosWaypoint(Player player, AiAirGroup airGroup, AiAirGroup playerAirGroup, Point3d newTargetPoint, AiAirWayPointType aawpttarget = AiAirWayPointType.FOLLOW, AiAirWayPointType aawptcontinue = AiAirWayPointType.FOLLOW, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true)
    {
        try
        {
            double changeL_XY_m = 100;
            AiAirWayPoint aaWP = null;
            Vector3d Vwld = playerAirGroup.Vwld();
            //Point3d Vwld2 = storedRollingAverage(player, airGroup, "vwld", Vwld, 2); //rolling average of last 2 positions, used for direction & speed
            Point3d Vwld2 = new Point3d(Vwld.x, Vwld.y, Vwld.z);
            Point3d Vwld5 = storedRollingAverage(player, airGroup, "vwld", Vwld, 3); //rolling average of last 5 positions, used for climb rate/vertical speed. Trying just last 3 average instead of 5.
            //if (Vwld)

            double vel_mps = Calcs.CalculatePointDistance(Vwld2); //Not 100% sure mps is the right unit here?

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
            
           //Console.WriteLine("MBT: Moving airport of attack!");
            Point3d pos = newTargetPoint;

            Tuple<Point3d?, double> obj_cr = ObjectivesRadius_m(pos); //center point, radius

            double Obj_radius = 0;
            Point3d Obj_pos = new Point3d (-1,-1,-1);

            if (obj_cr.Item1.HasValue)
            {
                Obj_radius = obj_cr.Item2;
                Obj_pos = obj_cr.Item1.Value;

                //We'll pick a point within 1/2 of the target objective radius of the target point the player has given us via knickebein.
                //This will be nicely within the target radius if the knickebein point is exactly aligned with the center of the objective, and hopefully not too far off otherwise     
                
                //Some linear algebra magic . . . 
                double dist = Calcs.CalculatePointDistance(pos, airGroup.Pos());
                Point3d unit_vec_AGtoTarg = new Point3d ((pos.x-airGroup.Pos().x)/dist, (pos.y-airGroup.Pos().y)/dist, 0);
                Point3d unit_vec_perpAGtoTarg = new Point3d(-unit_vec_AGtoTarg.y, unit_vec_AGtoTarg.x, 0);
                double randadd = ran.NextDouble() * Obj_radius - Obj_radius / 2;
                double backup = -ran.NextDouble() * Obj_radius;
                Point3d newPoint = new Point3d (pos.x + backup*unit_vec_AGtoTarg.x + randadd*unit_vec_perpAGtoTarg.x,  pos.y + backup*unit_vec_AGtoTarg.y + randadd * unit_vec_perpAGtoTarg.y, pos.z);
                //We could check if the point is on water & move it if so?  But then what if it is a ship?
                pos = newPoint;

            }

            //pos.z = playerAirGroup.Pos().z; //this was the first plan - just match the player's current altitude (at the target point - which is the only point in this AAWP
            pos.z = airGroup.Pos().z; //this is the new plan - if close to the target, just keep the current airgroup position, ie, fly flat & level.
                                      //if further from the target, fly a rate of climb/dive to put them in the right altitude relative to the main a/c at the target point, if the main a/c keeps its current climb/dive rate.  Main calculations below



            //GroundStationary newTarget = null;
            AiActor newTarget = null;
            GroundStationary newGroundTarget = null;
            bool diveTarget = false;
            //Choose another ground stationary somewhere within the given radius of change, starting with the GATTACK point since we don't have an actual GATTACK target actor; make sure it is alive if possible
            //Console.WriteLine("MBT: bom,alt: {0} {1:F0}", isDiveBomber(airGroup), pos.z);

            if (isDiveBomber(airGroup) && pos.z >= 1800)//only bother to do this search for dive bombers; they need an object to glom onto to do their dive, they also need to start above 2000m or so altitude; right now only JU87 can do it.
            {
                double maxMove_m = 200;
                double preferredMove_m = 100;

                if (obj_cr.Item1.HasValue)
                {
                    maxMove_m = Obj_radius;
                    preferredMove_m = Obj_radius / 2;

                }

                Console.WriteLine("Choosing target for dive bomber, maxMove {0:F0}, preferredMove {1:F0}", maxMove_m, preferredMove_m);
                if (airgroupTargetPoints.ContainsKey(airGroup))
                {
                    var oldApos = airgroupTargetPoints[airGroup];
                    if (Calcs.CalculatePointDistance(oldApos, newTargetPoint) <= maxMove_m)
                    {
                        if (airgroupTargets.ContainsKey(airGroup) && airgroupTargets[airGroup] != null && airgroupTargets[airGroup].IsAlive()) newTarget = airgroupTargets[airGroup];
                        else if (airgroupGroundTargets.ContainsKey(airGroup) && airgroupGroundTargets[airGroup] != null && airgroupGroundTargets[airGroup].IsAlive) newGroundTarget = airgroupGroundTargets[airGroup];
                        Console.WriteLine("reusing old ground traget");
                        diveTarget = true;
                    }
                }

                //This gets all static ACTORs such as (?) ships, artillery. (?).  There is no way to get this full list from CloD that I know of.
                if (!diveTarget)
                {
                    try
                    {
                        if (allStaticActors != null)
                        {
                            List<AiActor> closeStaticActors = new List<AiActor>(Calcs.gpGetAllGroundActorsNear(allStaticActors, pos, maxMove_m).ToList()); //1000?
                            //Finding actors we're going to range wider 1500. meters IN reality maybe we could look up the objective radius.  But actors nearby will be flak, etc etc etc.  All helpful.
                            Calcs.Shuffle(closeStaticActors);
                            foreach (AiActor act in closeStaticActors)
                            {
                                if (act.IsAlive())
                                {
                                    newTarget = act;
                                    Console.WriteLine("MBT: FOUND a ground actor" + newTarget.Name());
                                    diveTarget = true;
                                    break;
                                }

                                if (diveTarget) break;
                            }
                        }
                        
                    } catch (Exception ex) { Console.WriteLine("Bomb select #1 ERROR: " + ex.ToString()); }
                }


                //THIS gets all the remaining stationaries that are NOT actors, such as jerrycans or static trucks , planes, whatever. Scenery.
                //Here, we're going more for the center of the target. Again we COULD/SHOULD look up the actual radius of the objective.
                if (!diveTarget)
                {
                    double step = (maxMove_m-preferredMove_m)/ 6;
                    for (int d = 0; d <=6; d++)
                    {


                        GroundStationary[] stationaries = GamePlay.gpGroundStationarys(pos.x, pos.y, preferredMove_m + d*step);
                        //foreach (GroundStationary s in stationaries) Console.WriteLine("List:" + s.Name + " " + s.Title + " " + s.Type);
                        //Console.WriteLine("MBT: Looking for nearby stationary at {0}m", changeL_XY_m * d);
                        for (int i = 1; i < 30; i++)
                        {
                            try
                            {
                                if (stationaries.Length == 0) break;
                                int newStaIndex = ran.Next(stationaries.Length - 1);
                                //if (stationaries[newStaIndex] != null && stationaries[newStaIndex].IsAlive && (newTarget == null ||
                                //    (Math.Pow(stationaries[newStaIndex].pos.x - pos.x, 2) + Math.Pow(stationaries[newStaIndex].pos.y - pos.y, 2) <
                                //    Math.Pow(newTarget.Pos().x - pos.x, 2) + Math.Pow(newTarget.Pos().y - pos.y, 2)))) 
                                if (stationaries[newStaIndex] != null && stationaries[newStaIndex].IsAlive && !diveTarget && !airgroupGroundTargets.ContainsValue(stationaries[newStaIndex])) //not trying to find the closest, just a random one within the given distance, and not already picked by another airgroup

                                {
                                    newGroundTarget = stationaries[newStaIndex];
                                    Console.WriteLine("Bomber Target: Found a stationary" + stationaries[newStaIndex].Name + " " + stationaries[newStaIndex].Title + " " + stationaries[newStaIndex].Type + " {0:F0} {1:F0} - {2:F0} {3:F0}", stationaries[newStaIndex].pos.x, stationaries[newStaIndex].pos.y, newTargetPoint.x, newTargetPoint.y); // + " " + newTarget.Pos().x.ToString());
                                    //if (ran.Next(5) < 3) continue; //trying to get more of list for testing
                                    diveTarget = true;
                                    break;
                                }
                            }
                            catch (Exception ex) { Console.WriteLine("Bomb select #2 ERROR: " + ex.ToString()); }

                        }
                        if (diveTarget) break;
                    }
                }


                

        }

        Point3d newPos = pos;
        Point3d savePos = pos;


        //Use the position of the newly found ground actor as the new attack position, IF the actor exists/was found
        if (diveTarget)
        {
                if (newTarget != null) {
                    //Console.WriteLine("MBT: Found a stationary, updating attack position");
                    newPos.x = newTarget.Pos().x;
                    newPos.y = newTarget.Pos().y;
                } else if (newGroundTarget != null)
                {
                    newPos.x = newGroundTarget.pos.x;
                    newPos.y = newGroundTarget.pos.y;
                }

                airgroupTargets[airGroup] = newTarget;
                airgroupGroundTargets[airGroup] = newGroundTarget;
                airgroupTargetPoints[airGroup] = newPos;
                //AARGGHHH
            }
        //3rd approach, just set it to the actual x,y
        else
        {
            //Console.WriteLine("MBT: No stationary found, updating attack position");
            newPos.x = pos.x;
            newPos.y = pos.y;

        }


        //So we're calculting the climb/dive rate of the mainAC and then setting the end point
        //So that the cover AC will have the same climb/dive rate as the main AC (have to extend it out to the target point, because that
        //is the only point in our way.
        double distance_to_target_m = Calcs.CalculatePointDistance(newPos, airGroup.Pos());
        Vector3d coverVwld = airGroup.Vwld();
        double cover_vel_mps = Calcs.CalculatePointDistance(coverVwld); 
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
                newPos.z = playerAirGroup.Pos().z;
            }

            //If we want to do Hurri_FB, they don't dive bomb.
            //They need to do low alt point bomb.  So 5km out they are at 1000m & aimed straight at target.
            //Put attack_point at 0m to get them as low as possible.  They are usually pretty on target with this strategy
            //Tested via AI mission
            if (distance_to_target_m < 10000 && airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null && Calcs.GetAircraftType(airGroup.GetItems()[0] as AiAircraft).Contains("HurricaneMkI_FB")) {
                newPos.z = 1000; //Hurri FBs need to be at 1000m 5K out
                if (distance_to_target_m < 5000) newPos.z = 0;

            }


            newPos = calcOffset_m(newPos, airGroup, player, new Vector3d(Vwld2.x, Vwld2.y, Vwld5.z), vel_mps, offsetDirection.up_down); //shift this airgroup a little UP or DOWN depending on which a/g it is and what other a/gs of its type are also flying with this player
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
            if (vel_mps < 55) vel_mps = 55;
            if (vel_mps > 160) vel_mps = 160;
            

            double targetDist_m = Calcs.CalculatePointDistance(airGroup.Pos(), newPos);
            //So we need to be sure that this waypoint is distinct from the last waypoint,
            //and usually this will be used with newPosWayPoint as the 1st waypoint & this as the 2nd.  So we make sure this 
            //second position is distinct from the first by 10 meters
            if (targetDist_m < 10)
            {
                newPos.x = airGroup.Pos().x + (21.0 + ran.NextDouble() * 100.0) * (ran.Next(2) * 2.0 - 1.0);
                newPos.y = airGroup.Pos().y + 21.0 + ran.NextDouble() * 100.0 * (ran.Next(2) * 2.0 - 1.0);
            }

            //bombers especialy don't like to out run their waypoints.  So we are going to
            //make an extra waypoint that goes 30KM in the same direction, and we'll add that to the flight
            //plan, too. 
            //If bombers run out of flightplan, they auto-switch to task "return" and that means
            //dropping all of their bombs to prepare to return.
            double dst = Calcs.CalculatePointDistance(newPos, airGroup.Pos());
            if (dst == 0) dst = 1;
            double fact = 30000 / dst;
            double LongPosZ = newPos.z;
            if (diveTarget) LongPosZ = 400; //800m suggested for after dive?  Let's try 400 m though.
            Point3d LongPos = new Point3d((newPos.x - airGroup.Pos().x) * fact + airGroup.Pos().x,
                (newPos.y - airGroup.Pos().y) * fact + airGroup.Pos().y, LongPosZ);                     

            AiAirWayPoint nextWP = new AiAirWayPoint(ref newPos, vel_mps);
            AiAirWayPoint nextWP2 = new AiAirWayPoint(ref LongPos, vel_mps);

            (nextWP as AiAirWayPoint).GAttackPasses = AiAirWayPointGAttackPasses.AUTO;  //can do ._1 ._2 ._3 etc, I think? could try different numbers here, or random
            (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.LEVEL;  //could change to dive maybe? for certain a/c?
            //if (ran.Next(2)==0) (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.DIVE;  //change to dive for 50% of bombers

            //if (airGroup.hasBombs()) airGroup.setTask(AiAirGroupTask.ATTACK_GROUND, null); //not sure if this really does anything here? Maybe not needed?  //Seems to make bombers drop bombs at ???; not sure what the 2nd variable is - should be an aiairgroup or null apparently?  Maybe only needed for ATTACK_AIR, DEFENDING, etc.

            //Console.WriteLine("MBT: bom,alt: {0} {1:F0} {2}", isDiveBomber(airGroup), pos.z, (newTarget as AiActor) != null);
            //if ((newTarget as AiActor) != null && isDiveBomber(airGroup) && pos.z >= 1800)
            if (diveTarget)
            {
                //(nextWP as AiAirWayPoint).Target = newTarget as AiActor;  //change to newly selected target
                if (newTarget != null) (nextWP as AiAirWayPoint).Target = newTarget;  //change to newly selected target
                else if (newGroundTarget != null) (nextWP as AiAirWayPoint).Target = newGroundTarget as AiActor;  //This is bizarre, becuase if you do AiActor new = newGroundTarget as AiActor and then use new here, it WON'T WORK. But just use newGroundTarget as AiActor instead and it works.  There is no rhyme or reason.
                (nextWP as AiAirWayPoint).Action = AiAirWayPointType.GATTACK_TARG;  //keep action same
                (nextWP as AiAirWayPoint).GAttackType = AiAirWayPointGAttackType.DIVE;
                //Console.WriteLine("set target to ground");
            } else
            {
                (nextWP as AiAirWayPoint).Action = AiAirWayPointType.GATTACK_POINT;  //keep action same

            }
            (nextWP2 as AiAirWayPoint).Action = AiAirWayPointType.FOLLOW;

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
            }
            catch (Exception ex) { Console.WriteLine("Cover/MoveBomb ChangeBomberWaypoint WriteLine: " + ex.ToString());
            }

            return new Tuple<AiAirWayPoint, AiAirWayPoint>(nextWP, nextWP2);

        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb ChangeBomberWaypoint: " + ex.ToString()); return null; }
    }

    

    public AiAirWayPoint CurrentPosWaypoint(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double requested_vel_mps=-1)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            Vector3d Vwld = airGroup.Vwld();
            double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
            if (requested_vel_mps >= 0) vel_mps = requested_vel_mps; //if we pass a requested velocity along (as we do for escorts etc) then use that.


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

            if (vel_mps < 60) vel_mps = 60;
            if (vel_mps > 175) vel_mps = 175;

            Point3d CurrentPos = airGroup.Pos();

            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;
            if (aawpt == AiAirWayPointType.ESCORT && targetAirGroup.GetItems().Length>0)
            {                
                aaWP.Target = targetAirGroup.GetItems()[0];
            }

            //Console.WriteLine("CurrentPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0} for " + airGroup.Name(), new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb CurrentPosWaypoint: " + ex.ToString()); return null; }
    }

    public void EscortUpdateWaypoints(Player player, AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true )
    {

        List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
        //NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, aawpt));
        Tuple<AiAirWayPoint, AiAirWayPoint, double> aaWPs = EscortPosWaypoint(player, airGroup, targetAirGroup, aawpt, altDiff_m, AltDiff_range_m, nodupe);
        NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, aawpt, aaWPs.Item3));
        NewWaypoints.Add(aaWPs.Item1);
        NewWaypoints.Add(aaWPs.Item2);
        
        airGroup.SetWay(NewWaypoints.ToArray());
    }
    public Tuple<AiAirWayPoint, AiAirWayPoint, double> EscortPosWaypoint(Player player, AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double altDiff_m =1000, double AltDiff_range_m = 700, bool nodupe = true)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            AiAirWayPoint aaWP2 = null;
            Point3d CurrentPos = new Point3d (50000,50000,500);
            double vel_mps = 100;
            double targetDist_m = 1;
            bool heavyBomber = isHeavyBomber(airGroup) || isDiveBomber(airGroup);



            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            if (targetAirGroup == null)
            {
                //Console.WriteLine("Cover: EscortPosWaypoint has no targetAirGroup, can't do anything to waypoints, picking a random target point");                

                CurrentPos.x = airGroup.Pos().x + (2000 + ran.NextDouble() * 2000) * (ran.Next(2) * 2 - 1);
                CurrentPos.y = airGroup.Pos().y + (2000 + ran.NextDouble() * 2000) * (ran.Next(2) * 2 - 1);
                CurrentPos.z = airGroup.Pos().z + (2000 + ran.NextDouble() * 2000) * (ran.Next(2) * 2 - 1);
                if (CurrentPos.z < 300) CurrentPos.z = 300;
            }
            else
            {

                Vector3d Vwld = airGroup.Vwld();
                

                vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
                Vector3d targetVwld = targetAirGroup.Vwld();
                //Point3d targetVwld2 = storedRollingAverage(player, airGroup, "vwld", targetVwld, 2); //rolling average of last 2 positions, used for direction & speed //This seems to make them way to slow to respond to turns and things.
                Point3d targetVwld2 = new Point3d(targetVwld.x, targetVwld.y, targetVwld.z);

                Point3d targetVwld5 = storedRollingAverage(player, airGroup, "vwld", targetVwld, 3); //rolling average of last 5 positions, used for climb rate/vertical speed
                if (targetVwld.z > targetVwld5.z) targetVwld.z = targetVwld5.z;  //use rolling average, but if ascending more rapidly, just use that instead

                double target_vel_mps = Calcs.CalculatePointDistance(targetVwld2); 

                targetDist_m = Calcs.CalculatePointDistance(airGroup.Pos(), targetAirGroup.Pos());

                //Point3d directionVectorTarget = new Point3d(targetVwld.x , targetVwld.y, 0); //This would set velocity based on whether the cover a/c is in front of or behind the main a/c
                Point3d directionVectorTarget = new Point3d(Vwld.x, Vwld.y, 0); //this sets it depending on whether or not the cover a/c is headed towards OR away from the main a/c

                Point3d deltaPosTarget = new Point3d((airGroup.Pos().x - targetAirGroup.Pos().x) , (airGroup.Pos().y - targetAirGroup.Pos().y), 0);
                /*
                double divisor = target_vel_mps * targetDist_m;
                if (divisor == 0) divisor = 1; //prevent divison by zero errors in case target AC has zero velocity, or both at same x/y location.

                double angleTargetToGroup = Calcs.RadiansToDegrees(Math.Acos((deltaPosTarget.x * directionVectorTarget.x + deltaPosTarget.y * directionVectorTarget.y)/divisor)); //Angle of the bomber group relative to the main aircraft/target.  Ranges =0-180, where 0 is straight ahead and 180 is straight behind.  Note that it doesn't differentiate betweem left/right (ie 45 degrees might be 45 left or 45 right).
                */

                double angleTargetToGroup = Calcs.CalculateDifferenceAngle(directionVectorTarget, deltaPosTarget); //So this gives the heading angle from the cover a/c to the  main a/c.  180 degrees means the cover a/c is heading directly away from the mai na/c               

                if (angleTargetToGroup < 90 || angleTargetToGroup > 270) //cover a/c headed straight towards main a/c, more or less
                {
                    vel_mps = target_vel_mps * 1.08; //Go 95% as fast as main aircraft when behind but kinda close
                    if (targetDist_m > 750 && target_vel_mps * 1.12 > vel_mps) vel_mps = target_vel_mps * 1.12; //Go at least 20% faster than the group they're escorting, if possible
                    if (targetDist_m > 1500 && target_vel_mps * 1.2 > vel_mps) vel_mps = target_vel_mps * 1.2; //Go at least 20% faster than the group they're escorting, if possible
                    if (targetDist_m > 3000 && target_vel_mps * 1.5 > vel_mps) vel_mps = target_vel_mps * 1.5; //Go at least 20% faster than the group they're escorting, if possible
                    if (targetDist_m > 4500 && target_vel_mps * 2.5 > vel_mps) vel_mps = target_vel_mps * 2.5; //Go 2X as fast, if possible, the target gets more than 1km off

                    if (!heavyBomber) //generally keep fighter escorts going much faster relative to the main a/c and it doesn't need to snuggle up as close and its target point is
                        //closer to the main a/c which is more how we keep it in the right area
                    {
                        vel_mps = target_vel_mps * 1.4; //Generally fighters go a fair but faster than the bombers
                        if (targetDist_m > 1000 && target_vel_mps * 1.5 > vel_mps) vel_mps = target_vel_mps * 1.5;
                        if (targetDist_m > 3000 && target_vel_mps * 2.5 > vel_mps) vel_mps = target_vel_mps * 2.5;
                        
                    }
                }  else if (angleTargetToGroup >= 90 && angleTargetToGroup <= 270)  //cover a/c headed straight away from main a/c, more or less
                {                    
                    vel_mps = target_vel_mps * 0.90; //Go 95% as fast as main aircraft when ahead but kinda close
                    if (targetDist_m > 750 && target_vel_mps * .8 < vel_mps) vel_mps = target_vel_mps * 0.8; //Go 80% as fast when the target a/c gets more than 750m off
                    if (targetDist_m > 1500 && target_vel_mps * .6 < vel_mps) vel_mps = target_vel_mps * 0.6; //Go 60% as fast when the target a/c gets more than 1.5km off
                    if (targetDist_m > 2500 && target_vel_mps * .4 < vel_mps) vel_mps = target_vel_mps * 0.4; //Go 40% as fast when the target a/c gets more than 1km off

                    if (!heavyBomber) //generally keep fighter escorts going much faster relative to the main a/c and it doesn't need to snuggle up as close and its target point is
                                      //closer to the main a/c which is more how we keep it in the right area
                    {
                        vel_mps = target_vel_mps * 1.3; //Generally fighters go a fair but faster than the bombers, but if they get TOO far away and are going in the wrong direction, slow down
                        if (targetDist_m > 3000 && target_vel_mps * 1 > vel_mps) vel_mps = target_vel_mps * 1;
                        if (targetDist_m > 7000 && target_vel_mps * 0.7 > vel_mps) vel_mps = target_vel_mps * 0.8;

                    }
                }

                //Get an angle that tells us whether we are little behind or a little in front of 90 degrees off the main a/c (right or left/ doesn't matter)
                double ninetyDiff = angleTargetToGroup;
                if (ninetyDiff > 180) ninetyDiff = 360 - ninetyDiff;
                ninetyDiff = ninetyDiff - 90;
                ninetyDiff = Math.Abs(ninetyDiff);
                

                if (targetDist_m < 100) vel_mps = (vel_mps - target_vel_mps) * targetDist_m / 100 + target_vel_mps; // if close enough in distance to main A/C gradually go same speed as main A/C
                else if (ninetyDiff < 3)  vel_mps = (vel_mps - target_vel_mps) * ninetyDiff / 3 + target_vel_mps; // if close enough in ANGLE to main A/C gradually go same speed as main A/C                


                if (vel_mps < 45) vel_mps = 45;
                if (vel_mps > 175) vel_mps = 175;
                if (target_vel_mps < 15 && vel_mps < 75) vel_mps = 75;  //faster speed here to help prevent crashes while a/c circling the airport waiting for main a/c to take off.  Or if it crashes, is dead, etc.

                
                CurrentPos = targetAirGroup.Pos();
                Point3d savePos = CurrentPos;
                CurrentPos = calcOffset_m(CurrentPos, airGroup, player, targetVwld, target_vel_mps,offsetDirection.left_right); //shift this airgroup a little left or right depending on which a/g it is and what other a/gs of its type are also flying with this player
                //GamePlay.gpLogServer(null, "PosE: " + savePos.x.ToString("F0") + " " + savePos.y.ToString("F0") + " " + savePos.z.ToString("F0") + ":"
                 //   + CurrentPos.x.ToString("F0") + " " + CurrentPos.y.ToString("F0") + " " + CurrentPos.z.ToString("F0"), new object[] { });

                if (heavyBomber)
                {
                    if (vel_mps < 60)
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

                } else 
                {
                    CurrentPos.x += targetVwld2.x * 5; //for fighters let's try setting a point a littler closer in to the main a/c
                    CurrentPos.y += targetVwld2.y * 5; 
                }

                //CurrentPos.z = targetAirGroup.Pos().z + altDiff_m + ran.NextDouble() * 2 * AltDiff_range_m - AltDiff_range_m;
                CurrentPos.z += altDiff_m + ran.NextDouble() * 2 * AltDiff_range_m - AltDiff_range_m; //now we're going to make the covers match climb/dive rates, too - why not

                //So we need to be sure that this waypoint is distinct from the last waypoint,
                //and usually this will be used with currentPosWayPoint as the 1st waypoint & this as the 2nd.  So we make sure this 
                //second position is distinct from the first by 10 meters
                if (nodupe && targetDist_m < 10)
                {
                    CurrentPos.x = airGroup.Pos().x + (21.0 + ran.NextDouble() * 100.0) * (ran.Next(2) * 2.0 - 1.0);
                    CurrentPos.y = airGroup.Pos().y + 21.0 + ran.NextDouble() * 100.0 * (ran.Next(2) * 2.0 - 1.0);
                }

                //GamePlay.gpLogServer(null, "Angle: " + angleTargetToGroup.ToString("F0") + " " + ninetyDiff.ToString("F0") + " Speed: " + vel_mps.ToString("F0") + "/" + target_vel_mps.ToString("F0") + " alt " + CurrentPos.z.ToString("F0"), new object[] { });
            }

            //bombers especialy don't like to out run their waypoints.  So we are going to
            //make an extra waypoint that goes 30KM in the same direction, and we'll add that to the flight
            //plan, too. 
            //If bombers run out of flightplan, they auto-switch to task "return" and that means
            //dropping all of their bombs to prepare to return.
            double dst = Calcs.CalculatePointDistance(CurrentPos, airGroup.Pos());
            if (dst == 0) dst = 1;
            double fact = 30000 / dst; 
            Point3d LongPos = new Point3d ((CurrentPos.x - airGroup.Pos().x) * fact + airGroup.Pos().x,
                (CurrentPos.y - airGroup.Pos().y) *fact + airGroup.Pos().y, CurrentPos.z);



            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            aaWP2 = new AiAirWayPoint(ref LongPos, vel_mps);
            //GamePlay.gpLogServer(null, "Alt " + CurrentPos.z.ToString("F0"), new object[] { });
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            //The trick of ESCORT is to set the target to the main aircraft (the player aircraft in this case)
            //Even better, make one group escort the first a/c (live pilot) and the second escort airgroup escorts the first escort ai airgroup.
            //aawpt = AiAirWayPointType.ESCORT; //EXPERIMENTAL !! 
            aaWP.Action = aawpt;
            aaWP2.Action = aawpt;
            if (aawpt == AiAirWayPointType.ESCORT && targetAirGroup.GetItems().Length > 0)
            {
                aaWP.Target = targetAirGroup.GetItems()[0];
                aaWP2.Target = targetAirGroup.GetItems()[0];
            }

            //Console.WriteLine("Cover: EscortPosWaypoint - returning: {0} {1} {2:n0} {3:n0} {4:n0} {5:n0} LONG: {6:n0} {7:n0} {8:n0} {9:n0} {10:n0} {11:n0} for " + airGroup.Name() + " to " + targetAirGroup.Name(), new object[] {(aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP2.P.z, aaWP2.P.x, aaWP2.P.y, aaWP2.P.z, targetDist_m, CurrentPos.x, CurrentPos.y, CurrentPos.z });

            return new Tuple<AiAirWayPoint, AiAirWayPoint, double> (aaWP,aaWP2, vel_mps);
        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb EscortPosWaypoint: " + ex.ToString()); return null; }
    }

    public AiAirWayPoint EscortLandingWaypoint(AiAirGroup airGroup, AiAirGroup targetAirGroup = null, AiAirWayPointType aawpt = AiAirWayPointType.LANDING, double altDiff_m = 1000,
            double AltDiff_range_m = 700, bool nodupe = true)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);


 
            double vel_mps = 45;
            Point3d CurrentPos = new Point3d(0, 0, 0);
            AiAirport ap =Stb_nearestAirport(airGroup.Pos(), airGroup.getArmy());
            if (ap != null) CurrentPos = ap.Pos();
            //if (targetAirGroup != null ) CurrentPos = targetAirGroup.Pos();

            //CurrentPos.z = targetAirGroup.Pos().z + altDiff_m + ran.NextDouble() * 2 * AltDiff_range_m - AltDiff_range_m;

            CurrentPos.z = 0;//landing

            double targetDist_m = 1000;
            if (targetAirGroup != null) targetDist_m = Calcs.CalculatePointDistance(airGroup.Pos(), targetAirGroup.Pos());


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

    public AiAirGroup getRandomNearbyEnemyAirGroup(AiAirGroup from, double distance_m, double lowAlt_m, double highAlt_m)
    {
        Point3d startPos = from.Pos();
        List<AiAirGroup> airGroups = getNearbyEnemyAirGroups(from, distance_m, lowAlt_m, highAlt_m);
        if (airGroups.Count == 0) return null;
        int choice = ran.Next(airGroups.Count);
        if (airGroups[choice].Pos().distance(ref startPos) <= distance_m / 2) //We'll somewhat favor airgroups closer to the from airgroup
            choice = ran.Next(airGroups.Count);
        return airGroups[choice];

    }

    //Gets all nearby enemy airgroup within distance_m (meters) and between alt - lowAlt_m & alt-highAlt_m altitude of the target
    public List<AiAirGroup> getNearbyEnemyAirGroups(AiAirGroup from, double distance_m, double lowAlt_m, double highAlt_m)
    {
        try
        {
            if (GamePlay == null) return null;
            if (from == null) return null;
            List<AiAirGroup> returnAirGroups = new List<AiAirGroup>();
            AiAirGroup[] Airgroups;
            Point3d StartPos = from.Pos();

            Airgroups = GamePlay.gpAirGroups((from.Army() == 1) ? 2 : 1);

            if (Airgroups != null)
            {
                foreach (AiAirGroup airGroup in Airgroups)
                {
                    if (airGroup.GetItems().Length == 0) continue;
                    //AiAircraft a = airGroup.GetItems()[0] as AiAircraft;

                    if (airGroup.Pos().z > StartPos.z - lowAlt_m && airGroup.Pos().z < StartPos.z + highAlt_m && airGroup.Pos().distance(ref StartPos) <= distance_m)
                        returnAirGroups.Add(airGroup);

                }
                return returnAirGroups;
            }
            else
                return null;
        }
        catch (Exception ex) { Console.WriteLine("-stats getNearbyEnemyAirGroups ERROR: " + ex.ToString()); return null; }

    }

    

    //If ai cover aircraft come close to the map edge we're going to say they survived & re-add them to stock.

    public void AddOffMapAIAircraftBackToSupply_recur()
    {
        Timeout(60.123232,()=>AddOffMapAIAircraftBackToSupply_recur());

        try
        {
            int numremoved = 0;

            //BattleArea 10000 10000 350000 310000 10000
            //TODO: There is probably some way to access the size of the battle area programmatically
            /* double twcmap_minX = 10000;
            double twcmap_minY = 10000;
            double twcmap_maxX = 360000;
            double twcmap_maxY = 310000;
            */

            double minX = twcmap_minX; //20000
            double minY = twcmap_minY; //20000
            double maxX = twcmap_maxX; //340000;
            double maxY = twcmap_maxY; // 300000;

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
                    numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);                    
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true); //return this a/c to supply; true = softexit which forces return of the plane even though it is still in the air & flying
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
    private Tuple <double, Point3d, bool> Stb_distanceToNearestAirport(AiActor actor, bool birthplacefind = false)
    {
        double d2 = 10000000000000000; //we compare distanceSQUARED so this must be the square of some super-large distance in meters && we'll return anything closer than this.  Also if we don't find anything we return the sqrt of this number, which we would like to be a large number to show there is nothing nearby.  If say d2 = 1000000 then sqrt (d2) = 1000 meters which probably not too helpful.
        double d2Min = d2;
        if (actor == null) return new Tuple<double, Point3d, bool> (d2Min, new Point3d(-1,-1,-1), false);
        Point3d pd = actor.Pos();
        Point3d retPoint = new Point3d(-1, -1, -1);
        int pArmy = actor.Army();
        bool isAirSpawn = false;


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
            aArmy = a.Army();


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
                    if (ps.z > 500) isAirSpawn = true;
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
    public AiAirport Stb_nearestAirport(Point3d location, int army = 0)
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


        //AirfieldDisable(NearestAirfield); //for testing
        //Console.WriteLine("Destroying airfield " + NearestAirfield.Name());
        return NearestAirfield;
    }

    //nearest airport to an actor
    public AiAirport Stb_nearestAirport(AiActor actor, int army = 0)
    {
        if (actor == null) return null;
        Point3d pd = actor.Pos();
        return Stb_nearestAirport(pd, army);
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


    //So, various fixes to WayPoints, including removing any dupes, close dupes, any w-a-y off the map, and adding two points at the end of the route to take
    //the aircraft down low and off the map north (Red) or south (Blue)
    public void fixWayPoints(AiAirGroup airGroup)
    {
        try
        {
            //AiAirGroup airGroup = intc.attackingAirGroup;
            if (airGroup == null || airGroup.GetWay() == null) return; //Not sure what else to do?
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay(); //So there is a problem if GetWay is null or doesn't return anything. Not sure what to do in that case!
            //Maybe just exit?

            //if (CurrentWaypoints == null || CurrentWaypoints.Length == 0) return;
            //if (!isAiControlledAirGroup(airGroup)) return;
            if (airGroup.GetItems().Length == 0) return; //no a/c, no need to do anything
            AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;

            //for testing

            /*
            foreach (AiWayPoint wp in CurrentWaypoints)
            {

                Console.WriteLine("FixWayPoints - Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }
            */


            int currWay = airGroup.GetCurrentWayPoint();


            //if (currWay >= CurrentWaypoints.Length) return;

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
            int count = 0;

            bool update = false;

            AiWayPoint prevWP = CurrentPosWaypoint(airGroup, null, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);

            NewWaypoints.Add(prevWP); //Always have to add current pos/speed as first point or things go w-r-o-n-g

            AiWayPoint nextWP = prevWP;

            /*
            foreach (AiWayPoint wp in CurrentWaypoints)
            {
                nextWP = wp;

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
            if (nextWP.P.x > twcmap_minX && nextWP.P.x < twcmap_maxX && nextWP.P.y > twcmap_minY && nextWP.P.y < twcmap_maxY)
            {
                update = true;
                int army = airGroup.getArmy();
                AiAirWayPoint midaaWP = null;
                AiAirWayPoint endaaWP = null;
                Point3d midPos = new Point3d(0, 0, 0);
                Point3d endPos = new Point3d(0, 0, 0);
                Point3d tempEndPos = new Point3d(0, 0, 0);
                double distance_m = 100000000000;
                double tempDistance_m = 100000000000;

                for (int i = 1; i < 13; i++)
                {
                    if (ran.NextDouble() > 0.5)
                    {
                        if (army == 1) endPos.y = twcmap_maxY + 9000;
                        else if (army == 2) endPos.y = twcmap_minY - 9000;
                        else endPos.y = twcmap_maxY + 9000;
                        endPos.x = nextWP.P.x + ran.NextDouble() * 300000 - 150000;
                        if (endPos.x > twcmap_maxX + 9000) endPos.x = twcmap_maxX + 9000;
                        if (endPos.x < twcmap_minX - 9000) endPos.x = twcmap_minX - 9000;
                    }
                    else
                    {
                        if (army == 1) endPos.x = twcmap_minX - 9000;
                        else if (army == 2) endPos.x = twcmap_maxX + 9000;
                        else endPos.x = twcmap_maxX + 9000;
                        endPos.y = nextWP.P.y + ran.NextDouble() * 300000 - 150000;
                        if (army == 1) endPos.y += 80000;
                        else if (army == 2) endPos.y -= 10000;
                        if (endPos.y > twcmap_maxY + 9000) endPos.y = twcmap_maxY + 9000;
                        if (endPos.y < twcmap_minY - 9000) endPos.y = twcmap_minY - 9000;
                    }
                    //so, we want to try to find a somewhat short distance for the aircraft to exit the map.
                    //so if we hit a distance < 120km we call it good enough
                    //otherwise we take the shortest distance based on 10 random tries
                    distance_m = Calcs.CalculatePointDistance(endPos, nextWP.P);
                    /*
                    if (distance_m < 85000)
                    {
                        tempEndPos = endPos;
                        break;
                    }
                    */
                    if (distance_m < tempDistance_m)
                    {
                        tempDistance_m = distance_m;
                        tempEndPos = endPos;
                    }

                }
                endPos = tempEndPos;

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
                //Ok, low & off radar didn't really work as they just don't go low enough.  So now objective is to make
                //them look more like normal flights, routine patrols or whatever.  So slight deviation in flight path, not just STRAIGHT off the map, 
                //and random normal altitudes
                midPos.x = (nextWP.P.x * 1.0 + endPos.x * 1.0) / 2.0 + (ran.NextDouble() * 50000.0) - 25000.0;
                midPos.y = (nextWP.P.y * 1.0 + endPos.y * 1.0) / 2.0 + (ran.NextDouble() * 50000.0) - 25000.0;


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
                AiAirWayPointType aawpt = AiAirWayPointType.ESCORT;

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


            if (update)
            {
                //Console.WriteLine("MBTITG: Updating this course");
                airGroup.SetWay(NewWaypoints.ToArray());

                //for testing

                /*
                foreach (AiWayPoint wp in NewWaypoints)
                {
                    Console.WriteLine("FixWayPoints - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }
                */

            }
        }
        catch (Exception ex) { Console.WriteLine("Cover/MoveBomb FixWayPoints: " + ex.ToString()); }
    }



}



//Various helpful calculations, formulas, etc.
public static class Calcs
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
    public static int Feet2Angels(double altitude)
    {
        double altAngels = (altitude) / 1000;

        if (altAngels > 1)
            altAngels = Math.Round(altAngels, MidpointRounding.AwayFromZero);
        else
            altAngels = 1;

        return (int)altAngels;
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

    public static Point3d rollingAverage (Point3d oldaverage, Point3d newpoint, double rolls)
    {
        return new Point3d(rollingAverage(oldaverage.x, newpoint.x, rolls),
                            rollingAverage(oldaverage.y, newpoint.y, rolls),
                            rollingAverage(oldaverage.z, newpoint.z, rolls));


    }

    public static double rollingAverage (double oldaverage, double newpoint, double rolls)
    {
        return ((rolls-1)*oldaverage + newpoint)/ rolls;
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
        ret.x = startPoint.x + Math.Sin(Calcs.DegreesToRadians(angle_deg)) * dist;
        ret.y = startPoint.y + Math.Cos(Calcs.DegreesToRadians(angle_deg)) * dist;
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

    public static int numPlayersInArmy(int army, Mission mission)
    {
        int ret = 0;
        if (mission.GamePlay.gpRemotePlayers() != null && mission.GamePlay.gpRemotePlayers().Length > 0)
        {
            foreach (Player p in mission.GamePlay.gpRemotePlayers())
            {
                if (!p.IsConnected()) continue;
                if (p.Army() == army) ret++;
            }
        }
        return ret;
    }

    public static string randSTR(string[] strings)
    {
        //Random clc_random = new Random();
        return strings[clc_random.Next(strings.Length)];
    }

    public static void loadSmokeOrFire(maddox.game.IGamePlay GamePlay, Mission mission, double x, double y, double z, string type, double duration_s = 300, string path = "")
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

        mission.Timeout(2.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete)", new object[] { }); });
        //GamePlay.gpLogServer(null, "Setting up to delete stationary smokes in " + duration_s.ToString("0.0") + " seconds.", new object[] { });
        mission.Timeout(3.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete2)", new object[] { }); });
        mission.Timeout(4.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete3)", new object[] { }); });
        mission.Timeout(4.5, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete4)", new object[] { }); });

        mission.Timeout(5.0, () =>
        {
            GamePlay.gpLogServer(null, "Executing the timeout (delete5)", new object[] { });
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

        return tempString[1];
    }

    public static AiActor[] gpGetGroundActors(Mission msn, int army)
    {   // Purpose: Returns an array of all the AiActors in the game.
        // Use: GamePlay.gpGetActors();
        List<AiActor> result = new List<AiActor>();
        //List<int> armies = new List<int>(msn.GamePlay.gpArmies());
        List<int> armies = new List<int>() { 1, 2 };
        for (int i = 0; i < armies.Count; i++)
        {
            if (i != army) continue;
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
            List <AiGroundGroup> gg = new List<AiGroundGroup>(msn.GamePlay.gpGroundGroups(armies[i]));
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
    public static void listAllGroundActors(IGamePlay gp, int missionNumber = -1)
    {
        //TODO: Make it list only the actors in that mission by prefixing "XX:" if missionNumber is included.
        gp.gpLogServer(null, "Listing all ground actors:", new object[] { });

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
                                    gp.gpLogServer(null, actor.Name(), new object[] { });
                                    AiGroundGroup actorSubGroup = actor as AiGroundGroup;
                                    if (actorSubGroup != null && (actorSubGroup).GetItems() != null && actorSubGroup.GetItems().Length > 0)
                                    {
                                        foreach (AiActor a in actorSubGroup.GetItems())
                                        {
                                            gp.gpLogServer(null, a.Name(), new object[] { });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }

    private static int maxStatics = 10000; //so, these are guesses or somewhat reasonable maximums & might be wrong . . .
    private static int maxSubmissions = 300;
    //gets all ground actors.  Nothing in the CloD code can do this, it misses things like static ships
    //It gets them all without worrying about army or which side they're on.  We only care if they are close to a certain point, so army is irrelevant (& many are neutral etc, so it's complicated).
    public static AiActor[] gpGetAllGroundActors(Mission msn, int lastMissionLoaded)
    {
        List<AiActor> result = new List<AiActor>();        
        List<int> armies = new List<int>(msn.GamePlay.gpArmies());
        //List<int> armies = new List<int>() { 1, 2 };

            for (int s = 0; s < lastMissionLoaded + 2; s++) {
                for (int i = 0; i < maxStatics; i++)
                {
                    string subName = s.ToString() + ":" + "Static" + (i).ToString();
                    
                    AiGroundActor subActor = msn.GamePlay.gpActorByName(subName) as AiGroundActor;
                    if (subActor != null)
                    {
                        result.Add(subActor);
                    }
                }
            }
        
        return result.ToArray();
    }
    public static AiActor[] gpGetAllGroundActorsNear(AiActor[] aia, Point3d pos, double radius)
    {
        List<AiActor> result = new List<AiActor>();
        foreach (AiActor a in aia)
        {
            if (Calcs.CalculatePointDistance(a.Pos(), pos) < radius) result.Add(a);
        }
        return result.ToArray();
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


}
