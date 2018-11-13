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


public class Mission : AMission
{
    public IMainMission TWCMainMission;
    public ISupplyMission TWCSupplyMission;
    public IStatsMission TWCStatsMission;
    public IStbStatRecorder TWCStbStatRecorder;
    public Random ran;
    public int minimumAircraftRequiredForCoverDuty {get; set;}
    public int maximumAircraftAllowedPerMission { get; set; }
    public int maximumCheckoutsAllowedAtOnce { get; set; }

    public Dictionary<Player, int> numberCoverAircraftActorsCheckedOutWholeMission = new Dictionary<Player, int>();
    public Dictionary<AiActor, Player> coverAircraftActorsCheckedOut = new Dictionary<AiActor, Player>();
    public Dictionary<AiAirGroup, Player> coverAircraftAirGroupsActive = new Dictionary<AiAirGroup, Player>();

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

            //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
            ran = new Random();

            MissionNumberListener = -1;
            minimumAircraftRequiredForCoverDuty = 100;
            maximumAircraftAllowedPerMission = 6;
            maximumCheckoutsAllowedAtOnce = 3;

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

    int stb_lastMissionLoaded = -1;

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        try
        {

            TWCSupplyMission = TWCComms.Communicator.Instance.Supply;

            TWCStatsMission = TWCComms.Communicator.Instance.Stats;
            if (TWCStatsMission != null) TWCStbStatRecorder = TWCStatsMission.stb_IStatRecorder;



            stb_lastMissionLoaded = missionNumber;


            if (missionNumber == MissionNumber)
            {
                Timeout(61, () => AddOffMapAIAircraftBackToSupply_recur());

                Timeout(15, () => setCoverAircraftCurrentlyAvailable_recurs());

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
            Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was destroyed; doing aircraft checkin");



            double minX = 20000;
            double minY = 20000;
            double maxX = 340000;
            double maxY = 300000;            

            //AiActor actor = aircraft as AiActor;
            if (coverAircraftActorsCheckedOut.ContainsKey(actor))
            {
                Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was checked out by Cover");

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
                    if (numAC==0 && coverAircraftAirGroupsActive.ContainsKey(aircraft.AirGroup()))
                    {
                        coverAircraftAirGroupsActive.Remove(aircraft.AirGroup());
                        Console.WriteLine("CoverOnDestroy: Removing airgroup from active list");
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
                        Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was returned to stock because left map OK.");

                    }
                    else if (Z_AltitudeAGL < 5 && GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y) == LandTypes.WATER) // ON GROUND & IN THE WATER = DEAD    
                    {
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                        //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died on water.");
                    }
                    // crash landing in solid ground

                    else if (Z_AltitudeAGL < 5 && GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y) != aircraft.Army())    // landed in enemy territory
                    {
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                        //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died in enemy territory.");

                    }
                    else if (Z_AltitudeAGL < 5 && Stb_distanceToNearestAirport(actor) > 3500)  // crash landed in friendly or neutral territory, on land, not w/i 2000 meters of an airport
                    {
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                        //numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]); //don't re-add to player's supply here bec. this one was destroyed.
                        coverAircraftActorsCheckedOut.Remove(actor);
                        Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was not returned to stock because crashed/died away from airport.");
                    }

                    else if (Z_AltitudeAGL < 800)  // movebombtarget auto-destroys ai aircraft that are in the vicinity of an airport and set to waypoint type LANDING.  Because they are too dumb to actually land.  So these count as "landed" & the aircraft is returned to supply.
                    {
                        AiAirGroup airGroup = aircraft.AirGroup();
                        if (airGroup == null || !isAiControlledPlane2(aircraft))
                        {
                            Console.WriteLine("CoverOnDestroy: " + actor.Name() + " - no AirGroup");
                            return; //only process groups that have been in place a while, have actual aircraft in the air, and ARE ai
                        }
                        AiAirGroupTask task = airGroup.getTask();
                        AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
                        int currWay = airGroup.GetCurrentWayPoint();
                        bool landingWaypoint = false;
                        Console.WriteLine("CoverOnDestroy: Checking {0} {1} {2} {3} ", CurrentWaypoints.Length, currWay, (CurrentWaypoints[currWay] as AiAirWayPoint).Action, task);

                        if (CurrentWaypoints != null && CurrentWaypoints.Length > 0 && CurrentWaypoints.Length > currWay && (CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.LANDING) landingWaypoint = true;

                        if (task != AiAirGroupTask.LANDING && !landingWaypoint) return;


                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true); //true is softexit & forces return of plane even though it is in the air etc.
                        numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                        coverAircraftActorsCheckedOut.Remove(actor);
                        Console.WriteLine("CoverOnDestroy: " + actor.Name() + " was returned to stock because disapparated while LANDING.");
                    } else
                    {
                        Console.WriteLine("CoverOnDestroy: " + actor.Name() + " didn't match anything, no action taken.");
                    }

                }
            }
        

        }
        catch (Exception ex) { Console.WriteLine("Cover OnActorDestroyed ERROR: " + ex.ToString()); }
    }

    //Which a/c are currently available as cover a/c depending on stock available etc orderedictionary = acName, num remaining as string
    public Dictionary<ArmiesE, Dictionary<string, int>> CoverAircraftCurrentlyAvailable = new Dictionary<ArmiesE, Dictionary<string,int>>();

    //Which a/c are potentially available as cover a/c
    public Dictionary<ArmiesE, Dictionary<string, bool>> CoverAircraftInitiallyAvailable = new Dictionary<ArmiesE, Dictionary<string, bool>>
    {
        { ArmiesE.Red, new Dictionary<string,bool>() {
                         

        // {bob."aircraft as known to game name",whether available for use as an escort aircraft or not},
        //mostly, bombers aren't available as escorts, or bomber-enabled fighter variants
        //also rare or very valuable aircraft are not available
		
        {"bob:Aircraft.BeaufighterMkIF", true},
        {"bob:Aircraft.BeaufighterMkINF",true},
        {"bob:Aircraft.BlenheimMkIV", false},
        {"bob:Aircraft.BlenheimMkIVF",true},
        {"bob:Aircraft.BlenheimMkIVF_Late",true},
        {"bob:Aircraft.BlenheimMkIVNF",true},
        {"bob:Aircraft.BlenheimMkIVNF_Late",true},
        {"bob:Aircraft.BlenheimMkIV_Late",false},
        //{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below
        {"bob:Aircraft.DH82A-2",false},
        {"bob:Aircraft.HurricaneMkI",true},
        {"bob:Aircraft.HurricaneMkI_100oct",true},
        {"bob:Aircraft.HurricaneMkI_100oct-NF",true},
        {"bob:Aircraft.HurricaneMkI_dH5-20",true},
        {"bob:Aircraft.HurricaneMkI_dH5-20_100oct",true},
        {"bob:Aircraft.HurricaneMkI_FB",false},
        {"bob:Aircraft.SpitfireMkI",true},
        {"bob:Aircraft.SpitfireMkIa",true},
        {"bob:Aircraft.SpitfireMkIa_100oct",false},
        {"bob:Aircraft.SpitfireMkI_100oct",true},
        {"bob:Aircraft.SpitfireMkIIa",false}
        } },
        { ArmiesE.Blue, new Dictionary <string,bool>(){
        {"bob:Aircraft.Bf-109E-1",true},
        {"bob:Aircraft.Bf-109E-1B",false},
        {"bob:Aircraft.Bf-109E-3",true},
        {"bob:Aircraft.Bf-109E-3B",false},
        {"bob:Aircraft.Bf-109E-4",false},
        {"bob:Aircraft.Bf-109E-4_Late",false},
        {"bob:Aircraft.Bf-110C-2",true},
        {"bob:Aircraft.Bf-110C-4",true},
        {"bob:Aircraft.Bf-110C-4-NJG",true},
        {"bob:Aircraft.Bf-110C-4B" ,false},
        {"bob:Aircraft.Bf-110C-4Late",false},
        {"bob:Aircraft.Bf-110C-4N",true},
        {"bob:Aircraft.Bf-110C-6",true},  //These crash straight into the ground upon spawn FOR SOME UNKNOWN REASON so just eliminating their use altogether here.
        {"bob:Aircraft.Bf-110C-7",false},
        {"bob:Aircraft.BR-20M",false},	
        //{"bob:Aircraft.DH82A-1",10},  aircraft not on list aren't allowed as escorts, so disallow by either setting to FALSE or just remming out their line
        {"bob:Aircraft.DH82A-2",false},
        {"bob:Aircraft.G50",true},
        {"bob:Aircraft.He-111H-2",false},
        {"bob:Aircraft.He-111P-2",false},
        {"bob:Aircraft.Ju-87B-2",true},
        {"bob:Aircraft.Ju-88A-1",false},
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
            CoverAircraftCurrentlyAvailable[army] = new Dictionary<string,int>();           
            foreach (string acName in CoverAircraftInitiallyAvailable[army].Keys)
            {
                //Console.WriteLine("Cover: Setting cover aircraft currently available for {0} {1} {2}", army, acName, CoverAircraftInitiallyAvailable[army][acName]);
                if (CoverAircraftInitiallyAvailable[army][acName]) {
                    if (TWCSupplyMission != null)
                    {
                        int numRemaining = TWCSupplyMission.AircraftStockRemaining(acName, (int)army);
                        //Console.WriteLine("Cover: Setting cover aircraft currently available for {0} {1} {2} {3}", army, acName, CoverAircraftInitiallyAvailable[army][acName], numRemaining);
                        if (numRemaining > minimumAircraftRequiredForCoverDuty) CoverAircraftCurrentlyAvailable[army][acName]=numRemaining;

                    } else CoverAircraftCurrentlyAvailable[army][acName] = 9999;
                }
            }
        }        
    }

    public string listPositionCurrentCoverAircraft(Player player = null, bool display = true, bool html = false)
    {
        string nl = Environment.NewLine;
        if (html) nl = "<br>" + nl;
        string retmsg = "";
        int count = 0;
        string smsg = ">>>> Your current cover airgroups:";
        retmsg += smsg + nl;
        AiActor playerPlace = player.Place();

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
                double dis_m =Calcs.CalculatePointDistance(playerPlace.Pos(), aircraft.Pos());
                double dis_mi = (Calcs.meters2miles(dis_m));
                int dis_10 = (int)dis_mi;
                double bearing = Calcs.CalculateGradientAngle(playerPlace.Pos(), aircraft.Pos());
                double bearing_10 = Calcs.GetDegreesIn10Step(bearing);
                string ang = "A" + alt_angels.ToString("F0") + " ";
                string mi = dis_mi.ToString("F0") + "mi";
                string mi_10 = dis_10.ToString("F0") + "mi";

                if (player.Army() == 2) //metric for the Germanos . . . 
                {
                    mi = (dis_m / 1000).ToString("F0") + "k";
                    mi_10 = mi;
                    if (dis_m > 30000) mi_10 = ((double)(Calcs.RoundInterval(dis_m, 10000)) / 1000).ToString("F0") + "k";

                    //ft = alt_km.ToString("F2") + "k ";                                        
                    ang = ((double)(Calcs.RoundInterval(alt_km * 10, 5)) / 10).ToString("F1") + "k ";
                }

                msg = "#" + count.ToString() + " " + mi_10 + bearing_10.ToString("F0") + "°" + ang + " - " + Calcs.GetAircraftType(aircraft);

            }
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
        if ( CoverAircraftCurrentlyAvailable[army] == null) return "Cover: Aircraft availability not initialized";

        List<ArmiesE> armylist = new List<ArmiesE>();
        if (army == ArmiesE.Blue || army == ArmiesE.Red) armylist.Add(army);
        else if (army == ArmiesE.None) { armylist.Add(ArmiesE.Red); armylist.Add(ArmiesE.Blue); }

        foreach (ArmiesE a in armylist)
        {
            string smsg = string.Format(">>>>>Available cover aircraft for {0}", a);
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, smsg, null);
            retmsg += smsg + nl;

            smsg = string.Format("ID# - Aircraft - Number remaining in supply", a);
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, smsg, null);
            retmsg += smsg + nl;

            //foreach (string acName in CoverAircraftCurrentlyAvailable[army])
            int i = 0;
            foreach (string key in  CoverAircraftCurrentlyAvailable[a].Keys)
            {
                i++;
                string msg = string.Format("#{0} {1} {2}", i, Calcs.ParseTypeName(key), CoverAircraftCurrentlyAvailable[a][key]);
                if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg, null);
                retmsg += msg + nl;
            }
            if (i == 0) {
                string msg1 = string.Format("***No cover aircraft available for {0} - aircraft available for cover duty only if 100 or more remain in supply. Use chat command <stock to check supply***",  a);
                if (player != null) GamePlay.gpLogServer(new Player[] { player }, msg1, null);
            }
        }
        string msg3 = acAvailableToPlayer_msg(player);
        GamePlay.gpLogServer(new Player[] { player }, msg3, new object[] { });
        retmsg += msg3 + nl;

        int numCheckedOut = numberAirgroupsCurrentlyCheckedOutPlayer(player);

        GamePlay.gpLogServer(new Player[] { player }, "You have {0} cover groups escorting you, of {1} maximum allowed at one time.", new object[] { numCheckedOut, maximumCheckoutsAllowedAtOnce });

        return retmsg;
    }
    public string acAvailableToPlayer_msg (Player player )
    {
        int acAllowedThisPlayer = maximumAircraftAllowedPerMission;
        string rankExpl = "";
        if (TWCStbStatRecorder != null)
        {
            double adder = ((double)TWCStbStatRecorder.StbSr_RankAsIntFromName(player.Name()) - 2.0) / 2.0;
            if (adder < 0) adder = 0;
            acAllowedThisPlayer += Convert.ToInt32(adder);
            rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name());

        }
        int acAvailable = acAllowedThisPlayer - howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);
        if (acAvailable < 0) acAvailable = 0;
        return string.Format("{0} remain available of your command squadron of {1} cover aircraft allowed{2}",acAvailable, acAllowedThisPlayer, rankExpl);
    }
    public int acAvailableToPlayer_num(Player player)
    {
        int acAllowedThisPlayer = maximumAircraftAllowedPerMission;
        string rankExpl = "";
        if (TWCStbStatRecorder != null)
        {
            double adder = ((double)TWCStbStatRecorder.StbSr_RankAsIntFromName(player.Name()) - 2.0) / 3.0;
            if (adder < 0) adder = 0;
            int numPlayer = Calcs.numPlayersInArmy(player.Army(), this);
            

            acAllowedThisPlayer += Convert.ToInt32(adder);

            if (numPlayer > 6) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling((double)acAllowedThisPlayer / 2.0)); //Ceiling to run up to nearest integer, being nice to pilots here . . . .
            if (numPlayer > 9) acAllowedThisPlayer = Convert.ToInt32(Math.Ceiling((double)acAllowedThisPlayer / 3.0));
            rankExpl = " for rank of " + TWCStbStatRecorder.StbSr_RankFromName(player.Name());

        }
        int acAvailable = acAllowedThisPlayer - howMany_numberCoverAircraftActorsCheckedOutWholeMission(player);
        if (acAvailable < 0) acAvailable = 0;
        return acAvailable;
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
            if (numChoice <=0 && acName.Length > 0 && key.ToLowerInvariant().Contains(acName.Trim().ToLowerInvariant())) aircraftChoices.AddRange(Enumerable.Repeat(key, CoverAircraftCurrentlyAvailable[army][key] - minimumAircraftRequiredForCoverDuty)); //implement substring matching "<cover beau             
            
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

    public int numberAirgroupsCurrentlyCheckedOutPlayer(Player player)
    {
        try
        {
            if (player == null) return 0;
            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int numret = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null || coverAircraftAirGroupsActive[airGroup] != player) continue;
                numret++;
            }
            return numret;
        }
            catch (Exception ex) { Console.WriteLine("Cover, numberAirGroupsCurrently: " + ex.ToString()); return 0; }
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
    public string[] admins_basic = new String[] { "TWC_","Rostic" };
    public string[] admins_full = new String[] { "TWC_Flug", "TWC_Fatal_Error", "Server" };

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
        if (msg.StartsWith("<cland") )
        {
            if (player == null) return;
            List<AiAirGroup> saveCAAGA = new List<AiAirGroup>(coverAircraftAirGroupsActive.Keys);
            int numret = 0;
            foreach (AiAirGroup airGroup in saveCAAGA)
            {
                if (airGroup == null || coverAircraftAirGroupsActive[airGroup] != player) continue;

                if (aircraft == null) { EscortMakeLand(airGroup, null); numret++; }
                else { EscortMakeLand(airGroup, aircraft.AirGroup()); numret++; }

            }
            GamePlay.gpLogServer(new Player[] { player },numret.ToString() + " groups of escort aircraft have been instructed to land at the nearest friendly airport. They are shy and will land quicker if no one is around.", new object[] { });

        }
        else if (msg.StartsWith("<clist")) //<clist
        {
            if (player == null) return;
            listCoverAircraftCurrentlyAvailable((ArmiesE)player.Army(), player);
            
        }
        else if (msg.StartsWith("<cp")) //<cpos
        {
            if (player == null) return;
            listPositionCurrentCoverAircraft(player);

        }
        else if (msg.StartsWith("<cover"))
        {
            try
            {

                /*
                int parseL = Calcs.LastIndexOfAny(msgTrim, new string[] { " " });

                if (msgTrim.Length > 0 && parseL > -1)
                {
                List<string> sections = new List<string>();
                while (parseL > -1)
                {
                    sections.Add(msgTrim.Substring(parseL));
                    msgTrim = msgTrim.Substring(0, parseL);
                    parseL = Calcs.LastIndexOfAny(msgTrim, new string[] { " " });
                }
                sections.Add(msgTrim);
                */
                int numCheckedOut = numberAirgroupsCurrentlyCheckedOutPlayer(player);

                int numInArmy = Calcs.numPlayersInArmy(player.Army(), this);

                if (numInArmy > 12) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - cover available only when 12 or fewer players on your side. Please ask you fellow pilots to cover you.", new object[] { }); return; }

                if (numInArmy > 6) { GamePlay.gpLogServer(new Player[] { player }, "Note: Fewer cover aircraft available when more than 6 players on your side.", new object[] { });}

                if (aircraft == null) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - you're not in an aircraft!", new object[] { }); return; }


                string acType = Calcs.GetAircraftType(aircraft);
                bool isHeavyBomber = false;
                if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkIV")) isHeavyBomber = true;
                if (acType.Contains("BlenheimMkIVF") || acType.Contains("BlenheimMkIVNF")) isHeavyBomber = false;

                if (!isHeavyBomber) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - cover provided for heavy bombers only!", new object[] { }); return; }

                if (acAvailableToPlayer_num(player) < 1) { GamePlay.gpLogServer(new Player[] { player }, "Can't cover you - " + acAvailableToPlayer_msg(player), new object[] { }); return; }

                if (numCheckedOut >= maximumCheckoutsAllowedAtOnce)
                {

                    GamePlay.gpLogServer(new Player[] { player }, "You already have {0} cover groups currently escorting you--the maximum allowed.", new object[] { numCheckedOut });
                    GamePlay.gpLogServer(new Player[] { player }, "When you release your escort groups to return to base, you may be able to check out more.", new object[] { numCheckedOut });
                    GamePlay.gpLogServer(new Player[] { player }, "Use command <cland to make your cover fighters land.", new object[] { numCheckedOut });
                    return;
                }

                /*
                 * //this isn't working, need to re-do it with coverAircraftActorsCheckedOut
                int numACInAir = numberAircraftCurrentlyCheckedOutFromSupply(player) - 1; //-1, making the reasonable assumption the player is  in an a/c right now
                Console.WriteLine("<cover, numberAircraftCurrentlyCheckedOutFromSupply(player) {0} ", numACInAir);
                if (numACInAir >= 8)
                {

                    GamePlay.gpLogServer(new Player[] { player }, "You currently have {0} cover or primary aircraft still in the air OR lost and never returned.", new object[] { numACInAir });
                    GamePlay.gpLogServer(new Player[] { player }, "You have a maximum of 8 aircraft available to you during the mission, including your primary aircraft and escort aircraft.", new object[] { });
                    GamePlay.gpLogServer(new Player[] { player }, "If aircraft are lost or destroyed they are no longer available; if your aircraft return to base they can refuel and rejoin you on another mission at that time.", new object[] { });
                    GamePlay.gpLogServer(new Player[] { player }, "Preserve your escorts by guiding them back to base safely. Use command <cland to instruct your cover fighters land, if they can.", new object[] { numCheckedOut });
                    return;
                }
                */

                string acName = msg_orig.Substring(6).Trim();

                Point3d loc = new Point3d(0, 0, 0);
                if (aircraft != null) loc = actor.Pos();
                loc.z += 150; //starting low, like took off from airport, but we have to make sure it is ABOVE THE ACTUAL GROUND LEVEL or else trouble.  So making it 150 meters higher than the pilot who called it in.
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


                //Point3d ac1loc = (aircraft as AiActor).Pos();

                AiAirport ap = Stb_nearestAirport(actor.Pos(), actor.Army());
                if (ap != null) { loc = ap.Pos(); loc.z = 150; } //starting low, as though taking off.  Not actually taking off, though


                bool spawnInFriendlyTerritory = (player.Army() == GamePlay.gpFrontArmy(ap.Pos().x, ap.Pos().y));
                double distanceToSpawn_m = Stb_distanceToNearestAirport(aircraft as AiActor);
                if (!spawnInFriendlyTerritory || distanceToSpawn_m > 2800)
                {
                    if (distanceToSpawn_m > 2800) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you were too far from the nearest friendly airfield to call in cover (" + distanceToSpawn_m.ToString("N0") + " meters)", new object[] { }); });
                    else if (!spawnInFriendlyTerritory) Timeout(0.5, () => { GamePlay.gpLogServer(new Player[] { player }, "Sorry, you can't call in cover at an enemy airfield.", new object[] { }); });
                    return;
                }
                //regiment determines which ARMY the new aircraft will be in BOB_RAF British, BOB_LW German. BoB_RA = Italian?
                //Anyway, if we use the pilot's current regiment it matches which is nice but also definitely keeps them in the same army.
                string regiment = "gb01";
                //if (army == 1) regiment = "BoB_RAF_F_141Sqn_Early";
                //if (army == 2) regiment = "BoB_LW_JG77_I";
                regiment = aircraft.Regiment().name();

                string newACActorName = Stb_LoadSubAircraft(loc: loc, type: plane, callsign: "26", hullNumber: "3", serialNumber: "001",
                                    regiment: regiment, fuelStr: "", weapons: "", velocity: 150, fighterbomber: "f", skin_filename: "", delay_sec: "", escortedGroup: escortedGroup);



                //create the cover a/c
                Timeout(1.05, () =>
                //Timeout(0.15, () =>
                {
                    //AiActor newActor = GamePlay.gpActorByName(newACActorName);
                    AiActor newActor = GamePlay.gpActorByName(newACActorName);
                    Console.WriteLine("NewActorloaded: " + newActor.Name() + " for " + player.Name());
                    AiAircraft newAircraft = newActor as AiAircraft;
                    AiAirGroup newAirgroup = newAircraft.AirGroup();
                    Console.WriteLine("NewAirgrouploaded: " + newAirgroup.Name() + " for " + player.Name());

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
                            GamePlay.gpLogServer(new Player[] { player }, "Created " + aircrafttype + " (" + (a as AiActor).Name() + ")", new object[] { });
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
                            keepAircraftOnTask_recurs(newAirgroup, AiAirGroupTask.DEFENDING, AiAirWayPointType.ESCORT, player, 43.2354);
                            GamePlay.gpLogServer(new Player[] { player }, "Your escort consists of {0} {1}s. They have just taken off from the nearest friendly airfield.", new object[] { itemsmade, aircrafttype });
                            try
                            {
                                GamePlay.gpLogServer(new Player[] { player }, msg6, new object[] { });
                            }
                            catch (Exception ex) { Console.WriteLine("Cover2 <cover: " + ex.ToString()); }

                            GamePlay.gpLogServer(new Player[] { player }, "Remember to preserve your aircraft supply by instructing your escorts to land when you land, crash, or die - use command <cland", new object[] { });

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
            catch (Exception ex) { Console.WriteLine("Cover <cover: " + ex.ToString()); }
        }
        else if (msg.StartsWith("<chelp"))
        {
            string msg42 = "<cover or <cover Beau or <cover 3 - New cover fighters of (optionally) type or ID#  indicated";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });            
            msg42 = "<clist - list available cover fighters; <cpos - position of your current fighters";
            GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { });
            msg42 = "<cland - release cover fighters to land (IMPORTANT!)";
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

    public void keepAircraftOnTask_recurs(AiAirGroup airGroup, AiAirGroupTask task = AiAirGroupTask.DEFENDING, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, Player player = null, double delay = 31.2354, double AltDiff_m = 1000, double AltDiff_range_m = 100)
    {
        //So, sometimes airgroups split up, say when under attack or landing.  If so, we just add the new group to the coverAircraftAirGroupsActive (but
        //only when the original groups was also there)
        //In this case the name of the motherGroup it split off from is in airGroup.motherGroup()
        //This little exercise ensures that we still keep control off the aircraft, and they continue to support & cover the main aircraft, even if their airGroups happen to split up.
        if (airGroup.motherGroup() != null && coverAircraftAirGroupsActive.ContainsKey(airGroup.motherGroup())) coverAircraftAirGroupsActive.Add(airGroup,
            coverAircraftAirGroupsActive[airGroup.motherGroup()]);

        if (!coverAircraftAirGroupsActive.ContainsKey(airGroup)) return;

        Timeout(delay, ()=>keepAircraftOnTask_recurs(airGroup, task, aawpt, player, delay));

        int numAC = airGroup.NOfAirc;
        
        if (numAC == 0)
        {
            coverAircraftAirGroupsActive.Remove(airGroup);
            Console.WriteLine("Cover KeepAircraftOnTask: Removing airgroup {0} from active list because no more aircraft in the group", airGroup.Name());
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, "Your {0} cover group has been destroyed.", new object[] { airGroup.Name() });
            return;
            //TODO: Maybe the group splits up, maybe there are daughter groups or something?
        }

        //If player isn't in game any more, or too distant, or not in an aircraft then we release the cover a/c to land
        double distToLeadAircraft = 0;

        if (player != null && player.Place() != null && (player.Place() as AiActor) != null)
        {
            distToLeadAircraft = Calcs.CalculatePointDistance((player.Place() as AiActor).Pos(), airGroup.Pos());
        }
        //Console.WriteLine("Cover KeepAconTask: dist to lead ac {0:N0}", distToLeadAircraft);
        //Console.WriteLine("Cover KeepAconTask: Cover thinking {0} {1} {2} {3:N0} ", player == null, player.Place() == null, (player.Place() as AiAircraft).AirGroup() == null, distToLeadAircraft);
        if (player == null || player.Place() == null || (player.Place() as AiAircraft).AirGroup() == null || distToLeadAircraft > 50000 )
        {
            //Console.WriteLine("Cover KeepAconTask: Cover exiting {0} {1} {2} {3:N0} ", player == null, player.Place() == null, (player.Place() as AiAircraft).AirGroup() == null, distToLeadAircraft);
            AiAircraft leadAircraft = (player.Place() as AiAircraft);

            //EscortMake Land sets the necessary waypoints & also removes the a/c from coverAircraftAirGroupsActive
            if (leadAircraft == null) { EscortMakeLand(airGroup, null); }
            else { EscortMakeLand(airGroup, leadAircraft.AirGroup()); }

        
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, "You are too far from your cover aircraft. The {0} group of escort aircraft have been instructed to land at the nearest friendly airport.", new object[] { airGroup.Name() });
            return;
        }

        AiAirGroup playerAirGroup = (player.Place() as AiAircraft).AirGroup();

        AiAirGroup attackingAirGroup = getRandomNearbyEnemyAirGroup(playerAirGroup, 4000, 1500, 2000);

        if (attackingAirGroup != null)
        {
            Console.WriteLine("ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
            airGroup.setTask(AiAirGroupTask.ATTACK_AIR, attackingAirGroup);
            airGroup.changeGoalTarget(attackingAirGroup);
            Console.WriteLine("ChangeGoalTarget (after): {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
        }
        //else
        //double AltDiff_m = 1000;
        //double AltDiff_range_m = 100;
        //if (player.Place().Pos().z<150)

        //So if the leader is trying to fly under the radar we make the escorts match this altitude closely.  Whether they will be able ot do this (without crashing etc) remains to be seen
        double Z_AltitudeAGL = (player.Place() as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
        if (Z_AltitudeAGL < 175)
        {
            AltDiff_m = 20;
            AltDiff_range_m = 20;
            aawpt = AiAirWayPointType.AATTACK_FIGHTERS; //aawpt = AiAirWayPointType.COVER seems to work better in general but the cover aircraft stay up above the a/c they are covering and thus are seen by radar even if the main a/c is below radar. Trying AATACK_FIGHTERS to see if they will stay below radar better.
        }
        {
            Console.WriteLine("ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
            EscortUpdateWaypoints(airGroup, (player.Place() as AiAircraft).AirGroup(), aawpt, altDiff_m: AltDiff_m, AltDiff_range_m: AltDiff_range_m, nodupe: true);

            
        }

        Console.WriteLine("ChangeGoalTarget: {0} " + airGroup.Name() + " to " + player.Name(), airGroup.getTask());
    }

    //use loc.x = loc.y = loc.z 0 for default location
    //returns the name of the newly created a/c, which actually won't be created until the isect file is loaded, so wait 1 sec. or so before using.
    private string Stb_LoadSubAircraft(Point3d loc, string type = "SpitfireMkIa_100oct", string callsign = "26", string hullNumber = "3", string serialNumber = "001", string regiment = "gb02", string fuelStr = "", string weapons = "", double velocity = 0, string fighterbomber = "", string skin_filename = "", string delay_sec = "", string escortedGroup ="")
    {
        /*  //sample .mis file with parked a/c
         *  [AirGroups]
            BoB_RAF_F_141Sqn_Early.01
            [BoB_RAF_F_141Sqn_Early.01]
            Flight0  1
            Class Aircraft.SpitfireMkIa_100oct
            Formation VIC3
            CallSign 26
            Fuel 100
            Weapons 1
            SetOnPark 1
            Skill 0.3 0.3 0.3 0.3 0.3 0.3 0.3 0.3
            [BoB_RAF_F_141Sqn_Early.01_Way]
            TAKEOFF 76923.96 179922.36 0 0 

          */
        //default spawn location is Bembridge, landed, 0 mph & on the ground
        string locx = "76923.96";
        //string locy = "179922.36"; //real Bembridge location
        string locy = "178322.36"; //1600 meters off Bembridge
        string locz = "0";
        string vel = "0";

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
            if (velocity > 0)
            {
                locz = loc.z.ToString("F2");
                vel = velocity.ToString("F2");
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
        k = "Flight0"; v = "1 2"; f.add(s, k, v);  //we're always going to add 2 covers in one flight, though other possibilities are open
        k = "Class"; v = "Aircraft." + type; f.add(s, k, v);
        k = "Formation"; v = "VIC3"; f.add(s, k, v);
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
        f.add(s, "Serial0", serialNumber);
        if (skin_filename.Length > 0) f.add(s, "Skin0", skin_filename);
        else f.add(s, "Skin0", "default.jpg");  //Not sure if this file needs to be in the relevant a/c folder Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\PaintSchemes\Skins\MYAIRCRAFT of the user, the server, or what.  Also don't know how to find out which skin the player is currently using
                                                //f.add(s, "Flight0", hullNumber);  
        if (skin_filename.Length > 0) f.add(s, "Skin1", skin_filename);
        else f.add(s, "Skin1", "default.jpg");  //Not sure if this file needs to be in the relevant a/c folder Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\PaintSchemes\Skins\MYAIRCRAFT of the user, the server, or what.  Also don't know how to find out which skin the player is currently using
        //f.add(s, "Flight0", hullNumber);  
        double r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2 - 1)) / 4 + 3 / 4; //number between 0.5 & 1 but weighted towards the center of that range
        double skill = r;
        k = "Skill0"; v = string.Format("{0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1}", r); ; f.add(s, k, v);
        // "0.7 0.7 0.7 0.7 0.7 0.7 0.7 0.7"; 
        r = ((ran.NextDouble() * ran.NextDouble()) * (ran.Next(2) * 2 - 1)) / 4 + 3 / 4; //number between 0.5 & 1 but weighted towards the center of that range
        skill = r;
        k = "Skill1"; v = string.Format("{0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1} {0:F1}", r); f.add(s, k, v);
        //k = "Skill1"; v = "0.6 0.6 0.6 0.6 0.6 0.6 0.6 0.6"; f.add(s, k, v);
        if (velocity <= 0)
        {
            k = "SetOnPark"; v = "1"; f.add(s, k, v);
            k = "Idle"; v = "1"; f.add(s, k, v);
        }

        s = regiment_isec + "_Way";
        //if (velocity <= 0) k = "TAKEOFF";
        //else k = "NORMFLY";
        k = "ESCORT";
        v = locx + " " + locy + " " + locz + " " + vel;
        if (escortedGroup.Length>0) v += " " + escortedGroup + " 0";  //Not sure what the final 0 does
        f.add(s, k, v);

        v = "0 0 " + locz + " " + vel;
        if (escortedGroup.Length > 0) v += " " + escortedGroup + " 0";  //Not sure what the final 0 does
        f.add(s, k, v);

        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + stb_FullPath + "aircraftSpawn-ISectionFile.txt", new object[] { }); //testing
        //f.save(stb_FullPath + "aircraftSpawn-ISectionFile.txt"); //testing

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX13", null); //testing disk output for warps
        //load it in
        GamePlay.gpPostMissionLoad(f);

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
        else if (type == ("BlenheimMkIV"))
        {
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 10 12 9 10 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 0 30 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 5 0 2"; //default (updated for 4.53
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
            }
            if (fuel == 0) fuel = 55;
        }
        else if (type == ("BlenheimMkIV_Late"))
        {
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 0 30 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 2 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
            }
            if (fuel == 0) fuel = 55;
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

            if (fuel == 0) fuel = 55;
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
            if (fuel == 0) fuel = 55;
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
            if (fuel == 0) fuel = 55;
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

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 45;

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
                weapons = "1 1 1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 45;

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
            if (fuel == 0) fuel = 55;

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
                weapons = "1 1 1 1"; //default
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
            if (fuel == 0) fuel = 55;

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
        NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, AiAirWayPointType.NORMFLY));
        NewWaypoints.Add(EscortLandingWaypoint(airGroup, targetAirGroup, AiAirWayPointType.LANDING, 0, 0, nodupe));
        airGroup.SetWay(NewWaypoints.ToArray());
        airGroup.setTask(AiAirGroupTask.LANDING, airGroup); //try to force it . . . .
        Timeout (60, ()=>
        {
            Console.WriteLine("Forcing LANDING: Current task: {0} " + airGroup.Name(), airGroup.getTask());
            airGroup.setTask(AiAirGroupTask.LANDING, airGroup);
        });
        Timeout(120, () =>
        {
            Console.WriteLine("Forcing LANDING: Current task: {0} " + airGroup.Name(), airGroup.getTask());
            airGroup.setTask(AiAirGroupTask.LANDING, airGroup);
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

        Timeout(600, () =>
        {
            Console.WriteLine("-cover Aborting LANDING: Sending off map now " + airGroup.Name(), airGroup.getTask());
            fixWayPoints(airGroup);
        });

    }

    public void EscortUpdateWaypoints(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double altDiff_m = 1000,
        double AltDiff_range_m = 700, bool nodupe = true)
    {
        
        List<AiAirWayPoint> NewWaypoints = new List<AiAirWayPoint>();
        NewWaypoints.Add(CurrentPosWaypoint(airGroup, targetAirGroup, aawpt));
        NewWaypoints.Add(EscortPosWaypoint(airGroup, targetAirGroup, aawpt, altDiff_m, AltDiff_range_m, nodupe));
        airGroup.SetWay(NewWaypoints.ToArray());
    }

    

    public AiAirWayPoint CurrentPosWaypoint(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            Vector3d Vwld = airGroup.Vwld();
            double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?

            if (targetAirGroup != null)
            {
                Vector3d targetVwld = targetAirGroup.Vwld();
                double target_vel_mps = Calcs.CalculatePointDistance(targetVwld); //Not 100% sure mps is the right unit here?

                double targetDist_m = Calcs.CalculatePointDistance(airGroup.Pos(), targetAirGroup.Pos());
                if (target_vel_mps * 1.5 > vel_mps) vel_mps = target_vel_mps * 1.5; //Go at least 20% faster than the group they're escorting, if possible
                if (targetDist_m > 500 && target_vel_mps * 2.5 > vel_mps) vel_mps = target_vel_mps * 2.5; //Go 2X as fast, if possible, the target gets more than 1km off
            }

            if (vel_mps < 110) vel_mps = 110;
            if (vel_mps > 175) vel_mps = 175;

            Point3d CurrentPos = airGroup.Pos();

            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;
            if (aawpt == AiAirWayPointType.ESCORT && airGroup.GetItems().Length>0)
            {                
                aaWP.Target = airGroup.GetItems()[0];
            }

            Console.WriteLine("CurrentPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb CurrentPosWaypoint: " + ex.ToString()); return null; }
    }
    public AiAirWayPoint EscortPosWaypoint(AiAirGroup airGroup, AiAirGroup targetAirGroup, AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS, double altDiff_m =1000, 
        double AltDiff_range_m = 700, bool nodupe = true)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            if (targetAirGroup == null) { Console.WriteLine("Cover: EscortPosWaypoint has no targetAirGroup, can't do anything to waypoints, exiting"); return aaWP; }

            Vector3d Vwld = airGroup.Vwld();
            double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
            Vector3d targetVwld = targetAirGroup.Vwld();
            double target_vel_mps = Calcs.CalculatePointDistance(targetVwld); //Not 100% sure mps is the right unit here?

            double targetDist_m = Calcs.CalculatePointDistance(airGroup.Pos(), targetAirGroup.Pos());
            if (target_vel_mps * 1.5 > vel_mps) vel_mps = target_vel_mps * 1.5; //Go at least 20% faster than the group they're escorting, if possible
            if (targetDist_m>500 && target_vel_mps * 2.5 > vel_mps) vel_mps = target_vel_mps * 2.5; //Go 2X as fast, if possible, the target gets more than 1km off
            if (vel_mps < 110) vel_mps = 110;
            if (vel_mps > 175) vel_mps = 175;
            

            Point3d CurrentPos = targetAirGroup.Pos();
            CurrentPos.x += targetVwld.x * 20; //Aim for a point slightly ahead of the main aircraft, let's say 20 seconds travel time
            CurrentPos.y += targetVwld.y * 20;
            CurrentPos.z = targetAirGroup.Pos().z + altDiff_m + ran.NextDouble()*2 * AltDiff_range_m - AltDiff_range_m;

            //So we need to be sure that this waypoint is distinct from the last waypoint,
            //and usually this will be used with currentPosWayPoint as the 1st waypoint & this as the 2nd.  So we make sure this 
            //second position is distinct from the first by 100-1000 meters
            if (nodupe && targetDist_m < 100)
            {
                CurrentPos.x  = airGroup.Pos().x + (101 + ran.NextDouble()*1000) * (ran.Next(2)*2-1);
                CurrentPos.y = airGroup.Pos().y + 101 + ran.NextDouble() * 1000 * (ran.Next(2) * 2 - 1);
            }


            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;
            if (aawpt == AiAirWayPointType.ESCORT && airGroup.GetItems().Length > 0)
            {
                aaWP.Target = airGroup.GetItems()[0];
            }

            Console.WriteLine("Cover: EscortPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb EscortPosWaypoint: " + ex.ToString()); return null; }
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

            Console.WriteLine("EscortLANDINGWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0} {5}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z, ap.Name() });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb EscortLANDINGwaypoint: " + ex.ToString()); return null; }
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

        int numremoved = 0;

        //BattleArea 10000 10000 350000 310000 10000
        //TODO: There is probably some way to access the size of the battle area programmatically
        double twcmap_minX = 10000;
        double twcmap_minY = 10000;
        double twcmap_maxX = 360000;
        double twcmap_maxY = 310000;
        double minX = twcmap_minX + 10000; //20000
        double minY = twcmap_minY + 10000; //20000
        double maxX = twcmap_maxX - 10000; //340000;
        double maxY = twcmap_maxY - 10000; // 300000;
        
        Console.WriteLine("Checking for AI Aircraft off map, to check back in (Cover)");
        foreach (AiActor actor in coverAircraftActorsCheckedOut.Keys)
        {
            AiAircraft a = actor as AiAircraft;
            /* if (DEBUG) DebugAndLog ("DEBUG: Checking for off map: " + Calcs.GetAircraftType (a) + " " 
               //+ a.CallSign() + " " //OK, not all a/c have a callsign etc, so . . . don't use this . . .  
               //+ a.Type() + " " 
               //+ a.TypedName() + " " 
               +  a.AirGroup().ID() + " Pos: " + a.Pos().x.ToString("F0") + "," + a.Pos().y.ToString("F0")
              );
            */
            

            if (a != null &&
                  (a.Pos().x <= minX ||
                    a.Pos().x >= maxX ||
                    a.Pos().y <= minY ||
                    a.Pos().y >= maxY
                  )

            )   
            {
                if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(coverAircraftActorsCheckedOut[actor], actor, 0, true); //return this a/c to supply; true = softexit which forces return of the plane even though it is still in the air & flying
                numberCoverAircraftActorsCheckedOutWholeMission_remove(coverAircraftActorsCheckedOut[actor]);
                coverAircraftActorsCheckedOut.Remove(actor);
                Console.WriteLine("CoverLeftMap: " + actor.Name() + " was returned to stock because left map OK.");
            }


        }
        // if (DEBUG && numremoved >= 1) DebugAndLog (numremoved.ToString() + " AI Aircraft were off the map and de-spawned");
    } //method removeoffmapaiaircraft

    //returns distance to nearest friendly airport to actor, in meters. Count all friendly airports, alive or not.
    //In case of birthplace find, get the nearest birthplace regardless of friendly or not
    private double Stb_distanceToNearestAirport(AiActor actor, bool birthplacefind = false)
    {
        double d2 = 10000000000000000; //we compare distanceSQUARED so this must be the square of some super-large distance in meters && we'll return anything closer than this.  Also if we don't find anything we return the sqrt of this number, which we would like to be a large number to show there is nothing nearby.  If say d2 = 1000000 then sqrt (d2) = 1000 meters which probably not too helpful.
        double d2Min = d2;
        if (actor == null) return d2Min;
        Point3d pd = actor.Pos();

        int n;
        if (birthplacefind) n = GamePlay.gpBirthPlaces().Length;
        else n = GamePlay.gpAirports().Length;

        //AiActor[] aMinSaves = new AiActor[n + 1];
        //int j = 0;
        //GamePlay.gpLogServer(null, "Checking distance to nearest airport", new object[] { });
        for (int i = 0; i < n; i++)
        {
            AiActor a;
            if (birthplacefind) a = (AiActor)GamePlay.gpBirthPlaces()[i];
            else a = (AiActor)GamePlay.gpAirports()[i];

            if (a == null) continue;
            //if (actor.Army() != a.Army()) continue; //only count friendly airports
            //if (actor.Army() != (a.Pos().x, a.Pos().y)
            //OK, so the a.Army() thing doesn't seem to be working, so we are going to try just checking whether or not it is on the territory of the Army the actor belongs to.  For some reason, airports always (or almost always?) list the army = 0.

            //GamePlay.gpLogServer(null, "Checking airport " + a.Name() + " " + GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) + " " + a.Pos().x.ToString ("N0") + " " + a.Pos().y.ToString ("N0") , new object[] { });

            if (!birthplacefind && GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != actor.Army()) continue;


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
        //GamePlay.gpLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
        return Math.Sqrt(d2Min);
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
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
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
                        Console.WriteLine("FixWayPoints - WP WAY OFF MAP! Before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                        update = true;
                        if (nextWP.P.z < 0) nextWP.P.z = 0;
                        if (nextWP.P.z > 50000) nextWP.P.z = 50000;
                        if (nextWP.P.x > twcmap_maxX + 9999) nextWP.P.x = twcmap_maxX + 9999;
                        if (nextWP.P.y > twcmap_maxY + 9999) nextWP.P.y = twcmap_maxY + 9999;
                        if (nextWP.P.x < twcmap_minX - 9999) nextWP.P.x = twcmap_minX - 9999;
                        if (nextWP.P.y < twcmap_minY - 9999) nextWP.P.y = twcmap_minY - 9999;
                        Console.WriteLine("FixWayPoints - WP WAY OFF MAP! After: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                    }
                }
                catch (Exception ex) { Console.WriteLine("MoveBomb FixWay ERROR2A: " + ex.ToString()); }


                NewWaypoints.Add(nextWP); //do add
                count++;

            }
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
                double distance_m = 1000000000;
                double tempDistance_m = 1000000000;

                for (int i = 1; i < 10; i++)
                {
                    if (ran.NextDouble() > 0.5)
                    {
                        if (army == 1) endPos.y = twcmap_maxY + 9000;
                        else if (army == 2) endPos.y = twcmap_minY - 9000;
                        else endPos.y = twcmap_maxY + 9000;
                        endPos.x = prevWP.P.x + ran.NextDouble() * 300000 - 150000;
                        if (endPos.x > twcmap_maxX + 9000) endPos.x = twcmap_maxX + 9000;
                        if (endPos.x < twcmap_minX - 9000) endPos.x = twcmap_minX - 9000;
                    }
                    else
                    {
                        if (army == 1) endPos.x = twcmap_minX - 9000;
                        else if (army == 2) endPos.x = twcmap_maxY + 9000;
                        else endPos.x = twcmap_maxX + 9000;
                        endPos.y = prevWP.P.y + ran.NextDouble() * 300000 - 150000;
                        if (army == 1) endPos.y += 120000;
                        else if (army == 2) endPos.y -= 60000;
                        if (endPos.y > twcmap_maxY + 9000) endPos.y = twcmap_maxY + 9000;
                        if (endPos.y < twcmap_minY - 9000) endPos.y = twcmap_minY - 9000;
                    }
                    //so, we want to try to find a somewhat short distance for the aircraft to exit the map.
                    //so if we hit a distance < 120km we call it good enough
                    //otherwise we take the shortest distance based on 10 random tries
                    distance_m = Calcs.CalculatePointDistance(endPos, nextWP.P);
                    if (distance_m < 120000)
                    {
                        tempEndPos = endPos;
                        continue;
                    }
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
                midPos.x = (nextWP.P.x * 1 + endPos.x * 4) / 5 + ran.NextDouble() * 10000 - 5000;
                midPos.y = (nextWP.P.y * 1 + endPos.y * 4) / 5 + ran.NextDouble() * 10000 - 5000;


                /* (Vector3d Vwld = airGroup.Vwld();
                double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
                if (vel_mps < 70) vel_mps = 70;
                if (vel_mps > 160) vel_mps = 160;                
                */



                AiAirWayPointType aawpt = AiAirWayPointType.AATTACK_FIGHTERS;
                if ((nextWP as AiAirWayPoint).Action != AiAirWayPointType.LANDING && (nextWP as AiAirWayPoint).Action != AiAirWayPointType.TAKEOFF)
                    aawpt = (nextWP as AiAirWayPoint).Action;
                else
                {
                    string type = "";
                    string t = aircraft.Type().ToString();
                    if (t.Contains("Fighter") || t.Contains("fighter")) type = "F";
                    else if (t.Contains("Bomber") || t.Contains("bomber")) type = "B";

                    if (type == "B") aawpt = AiAirWayPointType.NORMFLY;

                }

                //add the mid Point
                midaaWP = new AiAirWayPoint(ref midPos, speed);
                //aaWP.Action = AiAirWayPointType.NORMFLY;
                midaaWP.Action = aawpt; //same action for mid & end

                NewWaypoints.Add(midaaWP); //do add
                count++;

                //Console.WriteLine("FixWayPoints - adding new mid-end WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { aawpt, (midaaWP as AiAirWayPoint).Speed, midaaWP.P.x, midaaWP.P.y, midaaWP.P.z });

                //add the final Point, which is off the map
                endaaWP = new AiAirWayPoint(ref endPos, speed);
                //aaWP.Action = AiAirWayPointType.NORMFLY;
                endaaWP.Action = AiAirWayPointType.LANDING;

                NewWaypoints.Add(endaaWP); //do add
                count++;
                //Console.WriteLine("FixWayPoints - adding new end WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { aawpt, (endaaWP as AiAirWayPoint).Speed, endaaWP.P.x, endaaWP.P.y, endaaWP.P.z });
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
        catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints: " + ex.ToString()); }
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

        Console.WriteLine("xSector1: {0} {1} {2}", val0, newch[0], total);
        //Console.WriteLine("xSector: {0} {1}", ch[0], total);
        int val = (int)(newch[1]);
        Console.WriteLine("xSector1.5: {0} {1} {2}", val, newch[1], total);
        if (val < 65 || val > 90) return 0; //upper case ASCII values range from A = 65 to Z = 90

        total += (val - 65) * 10000;
        Console.WriteLine("xSector2: {0} {1} {2}", val, newch[1], total);
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

}
