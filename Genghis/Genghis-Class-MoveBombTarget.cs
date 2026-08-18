#define DEBUG  
#define TRACE  

//$reference System.Core.dll
//$reference parts/core/CloDMissionCommunicator.dll
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using System.Text;
using TWCComms;

public struct changeLimit
{
    public double XY_m; //large variation in where the target or general waypoint can be selected
    public double aimXY_m; //small variation in exact aim, when the general target/waypoin has already been selected
    public double alt_m;
    public double alt_percent;
    public double speed_percent;
    public double airport_m;  //Not used yet?
    public changeLimit(double xy =0 , double aimXY = 0, double alt = 0, double altp = 25, double spdp = 10, double ap = 0)
    {
        XY_m = xy;
        aimXY_m = aimXY;
        alt_m = alt;
        alt_percent = altp;
        speed_percent = spdp;

        airport_m = ap;
    }   
}

public class MoveBombTargetMission : AMission
{
    Dictionary<AiAirWayPointType, double> changeXY_m;
    Dictionary<AiAirWayPointType, double> changeAlt_m;

    Dictionary<AiAirWayPointType, changeLimit> changeLimits;

    bool attackObjectives; //whether or not to move targets to a different/nearby objective
    bool attackPrimaryObjectives; //whether or not to move targets to a primary objective (only or preferentially)
    int attackPrimaryObjectivesPreference_red;
    int attackPrimaryObjectivesPreference_blue;
    bool favorFrontlineAirfields = false; //preferentially attack airports nearer the front ines than any old target.  This could be done when there are quite a few enemy fighters in play etc.
    double    favorFrontlineAirfieldsdistance_km = 70; //preferentially attack airports nearer the  front

    //Below is for changing to new objectives ONLY whereas there is a separate/special routine only
    //for switching airport to airport targets, so if this adds more airports ON TOP OF that
    //then there will be A LOT of airports targeted
    int    favorFrontlineAirfields_red = 0; //0 = don't include any extra primary objective preference; >0 means favor
    int    favorFrontlineAirfields_blue = 0; //0 = don't include any extra primary objective preference; >0 means 

    bool favorRadar = false;
    int favorRadar_factor = 1;  //Same for both sides here

    bool favorLowValueObjectives = true;
    int favorLowValueObjectives_factor = 4;

    double moveObjectivesDistance_m, moveObjectivesDistance_OnEnemy_m;

    bool moveAirports; //whether or not to move targets to a different/nearby airport
    double moveAirportsDistance_m, moveAirportsDistance_OnEnemy_m; //max distance to move airports if you choose that option

    AiAirGroup airGroup;
    AiAirport AirGroupAirfield;
    bool toHome = false;

    //Map boundaries - these should match what you set in the .mis file; these are the values that work with TWC radar etc
    double twcmap_minX = 10000;
    double twcmap_minY = 10000;
    double twcmap_maxX = 360000;
    double twcmap_maxY = 310000;

    //public IMainMission mainmission;
    public Mission mainmission;
    public Random ran;
    Dictionary<string, Mission.MissionObjective> SMissionObjectivesList = new Dictionary<string, Mission.MissionObjective>();

    public MoveBombTargetMission(Mission msn)
    {
        //mainmission = TWCComms.Communicator.Instance.Main;
        mainmission = msn;

        attackObjectives = true; //whether or not to move targets to actual mission objectives
        attackPrimaryObjectives = false; //makes it preferentially attack primary targets rather than any old target.  This could be done when there are quite a few enemy fighters in play etc.
        attackPrimaryObjectivesPreference_red = 0; //0 = don't include any extra primary objective preference; >0 means favor
        attackPrimaryObjectivesPreference_blue = 0; //0 = don't include any extra primary objective preference; >0 means favor primary objectives by this X (ie, 10 = each primary objective is included in the random pool 10X extra instead of just the usual 1X).
        
        moveObjectivesDistance_m = 475000; //max distance to move objectives if you choose that option
        moveObjectivesDistance_OnEnemy_m = 47500; //max distance to move objectives, when the a/c is on enemy territory (happens ie when bomber groups split up if they are attacked)

        //SMissionObjectivesList = mainmission.SMissionObjectivesList();
        SMissionObjectivesList = mainmission.MissionObjectivesList;

        moveAirports = true; //whether or not to move targets to a different/nearby airport
        moveAirportsDistance_m = 475000; //max distance to move airports if you choose that option 475000 = ENTIRE Channel map corner to corner.
        moveAirportsDistance_OnEnemy_m = 47500; //max distance to move objectives, when the a/c is on enemy territory (happens ie when bomber groups split up if they are attacked)

        //When adjusting various types of airgroup tasks, how far (at maximum) to move the position of that waypoint in xy and in alt (meters)
        //So for altitude, there is a number in meters and a percent.  It will use whichever is LARGER.  So if your formation is at 
        //say 5000 meters & the alt changeLs are 700, 30 then it will change a max of 1500 meters (30% of 5000)
        //if it's at 500 meters it may go up or down 700 m. (larger than 30% of 500).
        //However, if it is going down 700 m then the % kicks in again & prevents it from going down more than 30%.  So as to avoiding going underground etc.
        //Reason it is done this way is percentages work better for larger altitudes but absolute distances work better for low altitudes.  500m +/- 30% isn't much of a change at 
        //all while 5000m +/- 30% is quite a large change.  By contrast 5000m +/- 1000m isn't much of a change while 500m +/- 1000m is a very large change.
        changeLimits = new Dictionary<AiAirWayPointType, changeLimit>()
        {
            { AiAirWayPointType.NORMFLY, new changeLimit (200000, 0, 700, 50, 10) },
            { AiAirWayPointType.HUNTING, new changeLimit (47000, 0, 700, 50, 10) },
            { AiAirWayPointType.RECON, new changeLimit (164000, 0, 1000, 50, 10) },
            { AiAirWayPointType.GATTACK_POINT, new changeLimit (275450, 450, 0, 0, 30, 65000) },
            { AiAirWayPointType.GATTACK_TARG, new changeLimit (275450, 500, 0, 30, 65000) }, //this will try to find a new stationary object to attack within the given radius.
            { AiAirWayPointType.AATTACK_FIGHTERS, new changeLimit (95500, 0, 800, 45, 10) },
            { AiAirWayPointType.AATTACK_BOMBERS, new changeLimit (95500, 0, 800, 45, 10) },
        };
        /*
        changeAlt_m = new Dictionary<AiAirWayPointType, double>()
        {
            { AiAirWayPointType.NORMFLY, 2000 },
            { AiAirWayPointType.HUNTING, 7000 },
            { AiAirWayPointType.RECON, 3000 },
            { AiAirWayPointType.GATTACK_POINT, 0 },
            { AiAirWayPointType.GATTACK_TARG, 0 },
            { AiAirWayPointType.AATTACK_FIGHTERS, 2500 },
            { AiAirWayPointType.AATTACK_BOMBERS, 2500 },
        };
        */

        //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
        ran = new Random();
        Console.WriteLine("-MoveBombTarget.cs successfully inited");
    }

    public override void Init(ABattle b, int missionNumber)
    {
        base.Init(b, missionNumber);

        MissionNumberListener = -1;
    }

    private bool isAiControlledAirGroup(AiAirGroup airGroup) {
        if (airGroup.GetItems().Length == 0) return true; //really should be null or something?
        else return isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft);
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

    public AiAirWayPoint CurrentPosWaypoint(AiAirGroup airGroup, AiAirWayPointType aawpt = AiAirWayPointType.NORMFLY, double offset_x = 0, double offset_y = 0, double alt_m = 0)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            Vector3d Vwld = airGroup.Vwld();
            double vel_mps = MoveBombCalcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
            if (vel_mps < 70) vel_mps = 70;
            if (vel_mps > 160) vel_mps = 160;

            Point3d CurrentPos = airGroup.Pos();
			CurrentPos.x += offset_x;
			CurrentPos.y += offset_y;
			if (alt_m > 0) CurrentPos.z = alt_m;

            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;

            //Console.WriteLine("CurrentPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb CurrentPosWaypoint: " + ex.ToString()); return null; }
    }
	
	public AiAirWayPoint makeNewAiAirWaypointFromOld(AiAirWayPoint currWP)
    {
        try
        {
            AiAirWayPoint aaWP = new AiAirWayPoint(ref currWP.P, currWP.Speed);

            aaWP.Action = currWP.Action;
			aaWP.Formation = currWP.Formation;
			aaWP.GAttackPasses = currWP.GAttackPasses;
			aaWP.GAttackType = currWP.GAttackType;
			aaWP.Target = currWP.Target;
			aaWP.m_CirclebackTimeout = currWP.m_CirclebackTimeout;
			aaWP.m_idxCirclebackTo = currWP.m_idxCirclebackTo;			

            //Console.WriteLine("CurrentPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb makeNewAiAirWaypointFromOld: " + ex.ToString()); return null; }
    }
	
    public AiWayPoint offMapWaypoint(AiAirGroup airGroup, AiAirWayPointType aawpt = AiAirWayPointType.NORMFLY, int army = 0)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            Vector3d Vwld = airGroup.Vwld();
            double vel_mps = MoveBombCalcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
            if (vel_mps < 70) vel_mps = 70;
            if (vel_mps > 160) vel_mps = 160;

            Point3d CurrentPos = airGroup.Pos();
			Point3d NewPos = CurrentPos;
			
			NewPos.x += ran.Next(100000) - 50000.0;
			if (army == 2) NewPos.y = -50000.0;
			else if (army == 1) NewPos.y = 380000.0;
			
			if (army ==0  || army > 2) {
				NewPos.x = -50000;
				NewPos.y += ran.Next(50000) - 25000.0;
				
			}
			NewPos.z = 25; //low, try to keep off the radar
			
			

            aaWP = new AiAirWayPoint(ref NewPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;

            //Console.WriteLine("CurrentPosWaypoint - returning: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (aaWP as AiAirWayPoint).Action, (aaWP as AiAirWayPoint).Speed, aaWP.P.x, aaWP.P.y, aaWP.P.z });

            return aaWP;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb CurrentPosWaypoint: " + ex.ToString()); return null; }
    }	


    public AiWayPoint GetLandingWaypoint(AiAirport landingAirfield, double ApproachHeight)
    {
        AiAirWayPoint aaWP = null;
        double speed = 100.0;
        Point3d point = new Point3d(landingAirfield.Pos().x, landingAirfield.Pos().y, ApproachHeight);

        point = landingAirfield.Pos();

        aaWP = new AiAirWayPoint(ref point, speed);
        aaWP.Action = AiAirWayPointType.LANDING;
        aaWP.Target = landingAirfield;

        return aaWP;
    }


    public AiWayPoint[] WaitingWayPoints(Point2d location, double height, double speed, double AreaWidthX, double AreaWidthY, int numberOfCycles, AiAirWayPointType wayPointType)
    {
        List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();

        Point3d curPoint = new Point3d(location.x, location.y, height);

        AiAirWayPoint aaWP;

        aaWP = new AiAirWayPoint(ref curPoint, speed);
        aaWP.Action = wayPointType;

        NewWaypoints.Add(aaWP);

        for (int i = 0; i < numberOfCycles; i++)
        {
            curPoint.add(AreaWidthX, 0, 0);
            aaWP = new AiAirWayPoint(ref curPoint, speed);
            aaWP.Action = wayPointType;

            NewWaypoints.Add(aaWP);

            curPoint.add(0, AreaWidthY, 0);
            aaWP = new AiAirWayPoint(ref curPoint, speed);
            aaWP.Action = wayPointType;

            NewWaypoints.Add(aaWP);

            curPoint.add(-AreaWidthX, 0, 0);
            aaWP = new AiAirWayPoint(ref curPoint, speed);
            aaWP.Action = wayPointType;

            NewWaypoints.Add(aaWP);

            curPoint.add(0, -AreaWidthY, 0);
            aaWP = new AiAirWayPoint(ref curPoint, speed);
            aaWP.Action = wayPointType;

            NewWaypoints.Add(aaWP);
        }

        return NewWaypoints.ToArray();
    }


    public Point2d GetXYCoord(AiActor actor)
    {
        Point2d CurrentPoint = new Point2d(actor.Pos().x, actor.Pos().y);
        return CurrentPoint;
    }

    //returns distance to nearest friendly airport to actor, in meters. Count all friendly airports, alive or not.
    //Includes airports AND spawnpoints
    private double DistanceToNearestAirport(AiActor actor)
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


    public AiAirport GetAirfieldAt(Point3d location)
    {
        AiAirport NearestAirfield = null;
        AiAirport[] airports = GamePlay.gpAirports();
        Point3d StartPos = location;

        if (airports != null)
        {
            foreach (AiAirport airport in airports)
            {
                if (NearestAirfield != null)
                {
                    if (NearestAirfield.Pos().distance(ref StartPos) > airport.Pos().distance(ref StartPos))
                        NearestAirfield = airport;
                }
                else NearestAirfield = airport;
            }
        }
        return NearestAirfield;
    }

    //if distance_onenemy_m > 0 we use that in case the a/c is on enemy territory
    //if distance_onenemy_m == 0 we just ignore it & give any old random airfield near
    //favorfrontline_pct is the degree to which we'll favor airports near the front line vs airports further from the front line. 0= no favor, 1 = max favor
    public AiAirport GetRandomAirfieldNear(Point3d location, double distance_m, double distance_onenemy_m, int airportArmy, AiAirGroup airGroup, Point3d airGroup_pos, double favorfrontline_pct = 0)
    {
        if (GamePlay == null) return null;
        List<AiAirport> CloseAirfields = new List<AiAirport>();
        //AiAirport[] airports = GamePlay.gpAirports();

        var airport_names = new List<string>(mainmission.AirfieldTargets.Keys);
        Point3d StartPos = location;

        double distance_effective_m = distance_m;
        if (distance_onenemy_m > 0 && airGroup != null)
        {
            int airGroup_terr = GamePlay.gpFrontArmy(airGroup_pos.x, airGroup_pos.y);
            if (airGroup.getArmy() != airGroup_terr)
            {
                distance_effective_m = distance_onenemy_m; //If the group is on enemy territory then we choose an objective much closer to where they already are.       
            }
        }

        if (airport_names != null)
        {
            foreach (string apName in airport_names)
            {
                AiAirport airport = Calcs.AirportByName(GamePlay, apName);
                if (airport == null) continue;

                if (MoveBombCalcs.CalculatePointDistance(airport.Pos(), StartPos) < distance_effective_m && GamePlay.gpFrontArmy(airport.Pos().x, airport.Pos().y) == airportArmy) //use 2d distance, MUCH different than 3d distance for ie high-level bombers
                    CloseAirfields.Add(airport);
            }
        }

        Calcs.Shuffle(CloseAirfields);

        //Remove (some of, randomly) the airports far back from the front line, if requested
        double frontDist_m = favorFrontlineAirfieldsdistance_km * 1000;
        if (favorfrontline_pct>=0)
        {            
            var CloseAirFields_copy = new List<AiAirport>(CloseAirfields);
            foreach (AiAirport airport in CloseAirFields_copy) {
                double distanceToFront_m = GamePlay.gpFrontDistance(3-airportArmy, airport.Pos().x, airport.Pos().y);
                if (distanceToFront_m < frontDist_m) continue; // keep all airports 20km or less from the front;
                if ((distanceToFront_m - frontDist_m) / ( 360000 - frontDist_m) * ran.NextDouble() >= (1-favorfrontline_pct)) continue;  //Longest map distance is about 360000
                if (CloseAirfields.Count < 2) continue; // don't ever remove the very last one
                CloseAirfields.Remove(airport);
            }
        }

        int ind = 0;
        if (CloseAirfields.Count > 0) {
            ind = ran.Next(CloseAirfields.Count - 1);
            return CloseAirfields[ind];

        }
        else return null;
    }
    /*

        public override void OnBattleStarted()
        {
            base.OnBattleStarted();

            MissionNumberListener = -1;

        }
        */

    HashSet<AiAirGroup> airGroups = new HashSet<AiAirGroup>();
    HashSet<AiAirGroup> AirgroupsWayPointProcessed = new HashSet<AiAirGroup>();

    public void GetCurrentAiAirGroups()
    {
        try
        {
            airGroups = new HashSet<AiAirGroup>(); //we're getting the full list each time, of currently active groups, so don't need to keep saving all the old ones . . .
            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                foreach (int army in GamePlay.gpArmies())
                {
                    //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                    if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                    {
                        foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                        {
                            //Console.WriteLine("AG: " + airGroup.Name());
                            airGroups.Add(airGroup);
                        }
                    }
                }
            }
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb GetCurrentAG ERROR: " + ex.ToString()); }
    }


    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        if (missionNumber != MissionNumber) return; //only do this when this particular mission is loaded.


        checkAirgroupsIntercept_recurs();
        avoidAttackingAIEnemy_recurs();
        Console.WriteLine("-MoveBombTarget.cs successfully loaded");
        //GetCurrentAiAirGroups();

        //airGroup = GamePlay.gpActorByName("0:BoB_LW_StG2_II.07") as AiAirGroup;

    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        Console.WriteLine("Battle Stopping -- saving map state & current supply status");

        /// REARM/REFUEL: CLEANUP ANY PENDING REQUESTS
        //   ManageRnr.cancelAll(GamePlay);
        if (avoidAttackingAIEnemyTimer != null) avoidAttackingAIEnemyTimer.Dispose();
        if (checkAirgroupsInterceptTimer!= null) checkAirgroupsInterceptTimer.Dispose();
    }

    //AirGroupAirfield = GetAirfieldAt(airGroup.Pos());



    public Point3d CalculateWayPoint(Point3d startPoint, Point3d endPoint, double x, double height)
    {
        double m = 0.0;
        double b = 0.0;

        m = (endPoint.y - startPoint.y) / (endPoint.x - startPoint.x);
        b = startPoint.y - m * startPoint.x;
        Point3d point = new Point3d(x, m * x + b, height);

        return point;
    }


    public AiWayPoint[] SetWaypointBetween(Point3d startLocation, Point3d targetLocation, double height, double speed)
    {

        List<AiWayPoint> Wps = new List<AiWayPoint>();

        AiAirWayPoint aaWP = null;

        double X1;
        double X2;
        double halfway = 0.0;
        Point3d point;

        X1 = startLocation.x;
        X2 = targetLocation.x;

        halfway = (X2 - X1) / 2;

        point = CalculateWayPoint(startLocation, targetLocation, X2 - halfway, height);

        aaWP = new AiAirWayPoint(ref point, speed);
        aaWP.Action = AiAirWayPointType.NORMFLY;

        Wps.Add(aaWP);

        return Wps.ToArray();
    }

    //Distance (meters, 2D XY distance), altitude difference (meters)
    //returns -1, -1 if no pilot found.
    //armyToMatch==0 means match both armies
    public Tuple<double, double> getDistanceToNearestLivePilot(AiAirGroup from, int armyToMatch = 0)
    {
        try
        {
            AiAirGroup airGroup = getNearestLivePilot(from, armyToMatch);
            if (airGroup == null) return new Tuple<double, double>(-1, -1);
            double dist = MoveBombCalcs.CalculatePointDistance(from.Pos(), airGroup.Pos());
            double alt_diff = from.Pos().z - airGroup.Pos().z;
            return new Tuple<double, double>(dist, alt_diff);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb LivePilotDist ERROR: " + ex.ToString()); return new Tuple<double, double>(-1, -1); }
    }
	
	public Tuple<double, double> getDistanceToNearestLivePilot(AiAircraft from, int armyToMatch = 0)
    {
        try
        {
            AiAirGroup airGroup = getNearestLivePilot(from as AiActor, armyToMatch);
            if (airGroup == null) return new Tuple<double, double>(-1, -1);
            double dist = MoveBombCalcs.CalculatePointDistance(from.Pos(), airGroup.Pos());
            double alt_diff = from.Pos().z - airGroup.Pos().z;
            return new Tuple<double, double>(dist, alt_diff);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb LivePilotDist ERROR: " + ex.ToString()); return new Tuple<double, double>(-1, -1); }
    }

    public double getPlainDistanceToNearestLivePilot(Point3d pos, int armyToMatch = 0)
    {
        AiAirGroup ag = getNearestLivePilot(pos, armyToMatch);
        if (ag == null) return 100000000000;
        return MoveBombCalcs.CalculatePointDistance(pos, ag.Pos());

    }

    public AiAirGroup getNearestLivePilot(AiActor from, int armyToMatch = 0)
    {
        if (from == null) return null;
        Point3d StartPos = from.Pos();
        return getNearestLivePilot(StartPos, armyToMatch);
    }

    public AiAirGroup getNearestLivePilot(AiAirGroup from, int armyToMatch = 0)
    {
        if (from == null) return null;
        Point3d StartPos = from.Pos();
        return getNearestLivePilot(StartPos, armyToMatch);
    }

    public AiAirGroup getNearestLivePilot(Point3d StartPos, int armyToMatch = 0, bool includeAI = false)
    {
        try
        {
            if (GamePlay == null) return null;
            
            AiAirGroup NearestAirgroup = null;
            AiAirGroup[] empty = Array.Empty<AiAirGroup>();
            AiAirGroup[] Airgroups = Array.Empty<AiAirGroup>();


            //?? stuff below is because either gpAirGroups(1) OR gpAirGroups(2) OR both can be
            //null, which blows up concat - and other things.
            //Airgroups = GamePlay.gpAirGroups((from.Army() == 1) ? 1 : 2);
            if (armyToMatch > 0 ) Airgroups = (GamePlay.gpAirGroups(armyToMatch) ?? empty); //?? if left thing is null, uses right thing instead

            else Airgroups = (GamePlay.gpAirGroups(1) ?? empty).Concat((GamePlay.gpAirGroups(2)?? empty)).ToArray();  //army 1 pilots UNION army 2 pilots.  Not sure why we can't just get a simple list of ALL pilots in the game, but this is one way to do it
            
                    

            //Concat(back).ToArray()
            if (Airgroups != null)
            {
                foreach (AiAirGroup airGroup in Airgroups)
                {
                    if (airGroup == null) continue;
                    if (!includeAI && isAiControlledAirGroup(airGroup)) continue;
                    if (NearestAirgroup != null)
                    {
                        if (NearestAirgroup.Pos().distance(ref StartPos) > airGroup.Pos().distance(ref StartPos))
                            NearestAirgroup = airGroup;
                    }
                    else NearestAirgroup = airGroup;
                }
                return NearestAirgroup;
            }
            else
                return null;

        }
        catch (Exception ex) { Console.WriteLine("MoveBomb LivePilot ERROR: " + ex.ToString()); return null; }

    }

    //Distance (meters), altitude difference (meters)
    public Tuple<double?, double?> getDistanceToNearestFriendlyBombergroup(AiAirGroup from)
    {
        try
        {
            AiAirGroup airGroup = getNearestFriendlyBombergroup(from);
            if (airGroup == null) return new Tuple<double?, double?>(null, null);
            double dist = MoveBombCalcs.CalculatePointDistance(from.Pos(), airGroup.Pos());
            double alt_diff = from.Pos().z - airGroup.Pos().z;
            return new Tuple<double?, double?>(dist, alt_diff);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb FriendlyBomberDist ERROR: " + ex.ToString()); return new Tuple<double?, double?>(null, null); }
    }

    public AiAirGroup getNearestFriendlyBombergroup(AiAirGroup from)
    {
        try
        {
            if (GamePlay == null) return null;
            if (from == null) return null;
            AiAirGroup NearestAirgroup = null;
            AiAirGroup[] Airgroups;
            Point3d StartPos = from.Pos();

            Airgroups = GamePlay.gpAirGroups((from.Army() == 1) ? 1 : 2);

            if (Airgroups != null)
            {
                foreach (AiAirGroup airGroup in Airgroups)
                {
                    if (airGroup.GetItems().Length == 0) continue;
                    AiAircraft a = airGroup.GetItems()[0] as AiAircraft;
                    string acType = MoveBombCalcs.GetAircraftType(a);

                    //This includes JU-87s, so it's slightly different from the configuration we've used before.  But we often have Ju-87s flying with 
                    //escorts, which is hte context here
                    bool isHeavyBomber = false;
                    if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType == ("BlenheimMkIV") || acType == ("Ju-87")) isHeavyBomber = true;
                    if (!isHeavyBomber) continue;
                    if (NearestAirgroup != null)
                    {
                        if (NearestAirgroup.Pos().distance(ref StartPos) > airGroup.Pos().distance(ref StartPos))
                            NearestAirgroup = airGroup;
                    }
                    else NearestAirgroup = airGroup;
                }
                return NearestAirgroup;
            }
            else
                return null;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb FriendlyBomber ERROR: " + ex.ToString()); return null; }

    }

    public AiAirGroup getNearestEnemyAirgroup(AiAirGroup from)
    {
        AiAirGroup NearestAirgroup = null;
        AiAirGroup[] EnemyAirgroups;
        Point3d StartPos = from.Pos();

        EnemyAirgroups = GamePlay.gpAirGroups((from.Army() == 1) ? 2 : 1);

        if (EnemyAirgroups != null)
        {
            foreach (AiAirGroup airgroup in EnemyAirgroups)
            {
                if (NearestAirgroup != null)
                {
                    if (NearestAirgroup.Pos().distance(ref StartPos) > airgroup.Pos().distance(ref StartPos))
                        NearestAirgroup = airgroup;
                }
                else NearestAirgroup = airgroup;
            }
            return NearestAirgroup;
        }
        else
            return null;

    }


    public double? getDistanceToNearestEnemyAirgroup(AiAirGroup from)
    {
        AiAirGroup NearestAirgroup = null;
        AiAirGroup[] EnemyAirgroups;
        Point3d StartPos = from.Pos();

        EnemyAirgroups = GamePlay.gpAirGroups((from.Army() == 1) ? 2 : 1);

        if (EnemyAirgroups != null)
        {
            foreach (AiAirGroup airgroup in EnemyAirgroups)
            {
                if (NearestAirgroup != null)
                {
                    if (NearestAirgroup.Pos().distance(ref StartPos) > airgroup.Pos().distance(ref StartPos))
                        NearestAirgroup = airgroup;
                }
                else NearestAirgroup = airgroup;
            }
            return NearestAirgroup.Pos().distance(ref StartPos);
        }
        else
            return null;
    }
    //Returns a point in/near the changed objective within the given radius OR
    //Null if the attack point is not within/near an airport OR no suitable airport found
    public Tuple<Point3d?,Mission.MissionObjective> ChangeObjectives(Point3d p, int enemyArmy, AiAirGroup airGroup, Point3d airGroup_pos)
    {
        if (airGroup == null)
        {
            Console.WriteLine("Movebomb: ChangeObjectives / Randomobjective - the AIRGROUP was NULL, exiting.");
            return new Tuple<Point3d?, Mission.MissionObjective>(p, null);
        }
        try {
            Point3d retPos;

            var tup = RandomObjectiveWithin(p, enemyArmy, moveObjectivesDistance_m, moveObjectivesDistance_OnEnemy_m, airGroup, airGroup_pos);
            if (!tup.Item1.HasValue) return new Tuple<Point3d?, Mission.MissionObjective>(null,null);
            
            int numEnemyPlayers = MoveBombCalcs.gpNumberOfPlayersActive(GamePlay, enemyArmy);

            retPos = tup.Item1.Value;
            double radius = tup.Item2;
            double dist_m = tup.Item3;
            Mission.MissionObjective mo = tup.Item4;
            if (numEnemyPlayers > 10) numEnemyPlayers = 10;

            //For test server purposes we imagine a moderate amount of players always on the server
            if (mainmission.ON_TESTSERVER) numEnemyPlayers = 5;
            //If not scouted, no precision location is available
            //We'll say we only know it within 5K
            if (mo != null && !mo.Scouted) {
                //radius *= 5;
                retPos.x += ran.NextDouble() * 10000 - 5000;
                retPos.y += ran.NextDouble() * 10000 - 5000;
            }

            //bhugh temp XX2021-10 this will make all AI bombers miss their target by a long ways, 10K in either direction
            //Thus should pretty much take them out of the game, but they will still be flying around etc
            //2021-11 leaving it be for now
            retPos.x += ran.NextDouble() * 2000 - 1000; //2023-01 - was ran*60000 - 30000
            retPos.y += ran.NextDouble() * 2000 - 1000; //now just +/-1000m

            //Mult & add will push target points further from the center of the objective with no or few enemy players online
            //and then bring it in close when there are a lot of enemy players
            double mult = (24 - numEnemyPlayers) / 7; //2023-01, was 12/7, now 24/7
            if (numEnemyPlayers == 0) mult = 20;
            if (mult < 0.5) mult = 0.5;

            double add = (6 - numEnemyPlayers) / 6;
            if (add <= 0) add = 0;



            double dist = ran.NextDouble() * radius * mult + radius * add;
            double angl = ran.NextDouble() * 2 * Math.PI;

            //Console.WriteLine("MoveBomb: Position before # player adjustment {0:n0} {1:n0}", retPos.x, retPos.y);

            retPos.x = Math.Cos(angl) * dist + retPos.x;
            retPos.y = Math.Sin(angl) * dist + retPos.y;
            retPos.z = 0;


            if (mainmission.ON_TESTSERVER) Console.WriteLine("MoveBomb: New OBJECTIVE attack point: {0:n0} {1:n0} {2:n0}", retPos.x, retPos.y, dist_m);
            return new Tuple<Point3d?, Mission.MissionObjective>(retPos, mo);
            
        }
        catch (Exception ex) {
            Console.WriteLine("MoveBomb ChangeObjectives ERROR: " + ex.ToString());
            return new Tuple<Point3d?, Mission.MissionObjective>(p, null);
        }
    }

    //Returns an objective point & radius & distance that point p lies nearest.    
    private Tuple<Point3d?, double, double> NearestObjectivePoint(Point3d p, int army) //center point, radius, distance
    {
        double r = 10000000;
        Tuple<Point3d?, double, double> ret = new Tuple<Point3d?, double, double>(null, 0, r);
        foreach (string key in mainmission.MissionObjectivesList.Keys)
        {
            Mission.MissionObjective mo = mainmission.MissionObjectivesList[key];
            if (mo.OwnerArmy != army) continue;
            double dist = MoveBombCalcs.CalculatePointDistance(mo.Pos, p);
            if (dist <= r)
            {
                ret = new Tuple<Point3d?, double, double>(mo.Pos, mo.TriggerDestroyRadius, dist);
                r = dist;
                //Console.WriteLine("MBF: Best objective - " + mo.Name);
            }
        }
        return ret;
    }
    //Returns an objective point & radius & distance that point p lies nearest.    
    private Tuple<Point3d?, double, double, Mission.MissionObjective> RandomObjectiveWithin(Point3d p, int enemy_army, double radius_m, double radius_onenemy_m, AiAirGroup airGroup, Point3d airGroup_pos) //center point, radius, distance
    {

        Tuple<Point3d?, double, double, Mission.MissionObjective> ret = new Tuple<Point3d?, double, double, Mission.MissionObjective>(null, 0, 1000000, null);

        List<Mission.MissionObjective> CloseObjectives = new List<Mission.MissionObjective>();

        bool airGroupOnEnemyTerritory = false;
        int airGroup_terr = GamePlay.gpFrontArmy(airGroup_pos.x, airGroup_pos.y);
        double radius_effective_m = radius_m;
        if (enemy_army == airGroup_terr)
        {
            radius_effective_m = radius_onenemy_m; //If the group is on enemy territory then we choose an objective much closer to where they already are.
            airGroupOnEnemyTerritory = true;
        }
        //Console.WriteLine("MB:1");

        try {

            foreach (string key in mainmission.MissionObjectivesList.Keys)
            {
                try
                {
                    Mission.MissionObjective mo = mainmission.MissionObjectivesList[key];
                    if (mo == null) continue;
                    if (mo.OwnerArmy != enemy_army) continue;
                    double dist = MoveBombCalcs.CalculatePointDistance(mo.Pos, p);
                    if (dist <= radius_effective_m)
                    {
                        if (mo.MOObjectiveType == Mission.MO_ObjectiveType.Submarine) continue; //no attack the submarine, that's for breathers
                        if ((mo.MOObjectiveType == Mission.MO_ObjectiveType.Naval_Ship || mo.MOObjectiveType == Mission.MO_ObjectiveType.Freighter_Ship || mo.MOObjectiveType == Mission.MO_ObjectiveType.Tanker_Ship  || mo.MOObjectiveType == Mission.MO_ObjectiveType.Naval_Freighter_Convoy || mo.MOObjectiveType == Mission.MO_ObjectiveType.Naval_Tanker_Convoy) && ran.NextDouble() > 0.25) continue; //attack the navy/ship objectives only seldom 

                        if (mo.MOObjectiveType == Mission.MO_ObjectiveType.Military_Convoy && ran.NextDouble() > 0.2) continue; //mostly avoid attacking the convoys, they will probably just miss anyway.

                        //skip low priority targets mostly
                        //2021-11 now we are concentrating on low-value targets for the AI, and skipping
                        //primary targets
                        //if (mo.PrimaryTargetWeight < 100 & ran.Next(0, 100) > mo.PrimaryTargetWeight) continue;

                        //2021-11 now we are concentrating on low-value targets for the AI, and skipping
                        if (ran.Next(100, 200) < mo.PrimaryTargetWeight) continue;

                        //Mostly skip re-bombing objectives already destroyed OR achieved for points
                        //if (mo.ObjectiveAchievedForPoints && ran.NextDouble() > 0.1) continue;
                        //if (mo.DestroyedPercent > 1.0 && ran.NextDouble() > 0.1) continue;

                        //mostly skip targeting primaries 2021-11
                        if (mo.IsPrimaryTarget && ran.NextDouble() > 0.01) continue; //2022-12 - was 0.05

                        //ret = new Tuple<Point3d?, double, double>(mo.Pos, mo.TriggerDestroyRadius, dist);
                        //r = dist;
                        //Console.WriteLine("MBF: Best objective - " + mo.Name);
                            CloseObjectives.Add(mo);

                        //FAVOR ATTACKING SCOUTED PRIMARY OBJECTIVES
                        int attackPrimaryObjectivesPreference = 0;
                        if (enemy_army == 1) attackPrimaryObjectivesPreference = attackPrimaryObjectivesPreference_blue; //"army" here = enemyarmy
                        if (enemy_army == 2) attackPrimaryObjectivesPreference = attackPrimaryObjectivesPreference_red;

                        //In case we are preferring primary objectives, we just add them into the pool X more times here
                        //But ONLY IF they are scouted
                        if (attackPrimaryObjectives && mo.IsPrimaryTarget && !mo.ObjectiveAchievedForPoints && mo.Scouted && attackPrimaryObjectivesPreference > 0)
                        {
                            for (int i = 0; i < attackPrimaryObjectivesPreference; i++)
                            {
                                CloseObjectives.Add(mo);
                            }
                        }

                        //bool favorLowValueObjectives = true;
                        //int favorLowValueObjectives_factor = 3;

                        //In case we are preferring low-value objectives, we just add them into the pool X more times here
                        //But ONLY IF they are scouted
                        if (favorLowValueObjectives && mo.Points<=4 && mo.DestroyedPercent <= 2.5  && favorLowValueObjectives_factor > 0)
                        {
                            for (int i = 0; i < favorLowValueObjectives_factor; i++)
                            {
                                if (!mo.Scouted && ran.NextDouble() < 0.5) continue; //attacked scouted objectives more often
                                    CloseObjectives.Add(mo);
                            }
                        }

                        //Increase chance of picking airfields closer in to the front line, if requested
                        int favorFrontlineAirfields_factor = 0;
                        if (enemy_army == 1) favorFrontlineAirfields_factor = favorFrontlineAirfields_blue; //"army" here = enemyarmy
                        if (enemy_army == 2) favorFrontlineAirfields_factor = favorFrontlineAirfields_red;

                        double distanceToFront_m = GamePlay.gpFrontDistance(mo.AttackingArmy, mo.Pos.x, mo.Pos.y);
                        double favorFrontlineAirfieldsdistance_local_km = favorFrontlineAirfieldsdistance_km * (1 + ran.NextDouble() / 5.0); //Add in AF up to 20% further from the front sometimes.
                                                                                                                                             //In case we are preferring airfields near the front, we just add them into the pool X more times here

                        if (favorFrontlineAirfields && mo.MOObjectiveType == Mission.MO_ObjectiveType.Military_Airfield && !mo.Destroyed && mo.DestroyedPercent< 1.1 && favorFrontlineAirfields_factor > 0 && distanceToFront_m <= favorFrontlineAirfieldsdistance_local_km * 1000)
                        {
                            for (int i = 0; i < favorFrontlineAirfields_factor; i++)
                            {
                                CloseObjectives.Add(mo);
                            }
                        }


                        if (favorRadar && mo.MOObjectiveType == Mission.MO_ObjectiveType.Radar && !mo.Destroyed)
                        {
                            for (int i = 0; i < favorRadar_factor; i++)
                            {
                                CloseObjectives.Add(mo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("MoveBomb RandomObjectiveWithin - #1: " + ex.ToString()); return ret;
                }
                //Console.WriteLine("MB:2");
            }


            //Console.WriteLine("MB:3");
            int ind = 0;
            Mission.MissionObjective retMO = null;
            if (CloseObjectives.Count > 0)
            {
                /*
                Console.WriteLine("=====================");
                Console.WriteLine("Movebomb: Close Objectives List");
                foreach (Mission.MissionObjective m in CloseObjectives)
                {
                    Console.WriteLine(m.Name);
                }
                Console.WriteLine("=====================");
                */

                try
                {
                    //Console.WriteLine("MB:4");
                    ind = ran.Next(CloseObjectives.Count - 1);
                    retMO = CloseObjectives[ind];

                    //If our objective isn't scouted then aiming gets pretty sketchy
                    Point3d retMOpos = retMO.Pos;
                    
                    Console.WriteLine("Chosen Objective: " + retMO.Name);

                    if (!retMO.Scouted)
                    {
                        retMOpos.x += -3000 + ran.Next(0, 1500);
                        retMOpos.y += -3000 + ran.Next(0, 1500);

                    }

                    double return_radius = retMO.TriggerDestroyRadius;
                    if (retMO.radius > return_radius) return_radius = retMO.radius;
                    
                    ret = new Tuple<Point3d?, double, double, Mission.MissionObjective>(retMOpos, retMO.TriggerDestroyRadius, MoveBombCalcs.CalculatePointDistance(retMOpos, p), retMO);
                    //Console.WriteLine("MB:5");
                    try
                    {
                        Console.WriteLine("MoveBomb: Chosen objective - " + retMO.Name + " IsPrimary? " + retMO.IsPrimaryTarget.ToString() + " Scouted? " + retMO.Scouted.ToString() + " Previously Destroyed/points? " + retMO.ObjectiveAchievedForPoints.ToString());

                        /*
                        if (!airGroupOnEnemyTerritory && airGroup != null && airGroup.GetItems() != null & airGroup.GetItems().Length > 0)
                        {
                            int sendArmy = retMO.AttackingArmy;
                            if (retMO.AttackingArmy == 1 && Mission.DataDictionary.ContainsKey("generalstaff_discovered_blue")) sendArmy = -1; //If blue has found the GeneralStaff, broadcast to ALL
                            if (retMO.AttackingArmy == 2 && Mission.DataDictionary.ContainsKey("generalstaff_discovered_red")) sendArmy = -1; //If red has found the GeneralStaff, broadcast to ALL

                            string acType = Calcs.GetAircraftType(airGroup);
                            string acNumber = airGroup.GetItems().Length.ToString();

                            sendScreenMessageTo(sendArmy, "Bomber Mission Departing for " + retMO.Name, new object[] { });


                            string keyp = Calcs.correctedSectorNameKeypad(this, airGroup_pos);
                            string alt = "A" + Calcs.Meters2Angels(airGroup_pos.z).ToString();
                            //if (retMO.OwnerArmy == 2) alt = Calcs.RoundInterval(pos.z, 1000).ToString() + "m";

                            if (retMO.AttackingArmy == 2) alt = "alt. " + ((double)(Calcs.RoundInterval(airGroup_pos.z / 1000 * 10, 5)) / 10).ToString("F1") + "km";
                            string ar = "RED";
                            if (retMO.AttackingArmy == 2) ar = "BLUE";

                            string retMOsector = Calcs.correctedSectorNameKeypad(this, retMOpos);

                            string msg = ">>>" + ar + " BOMBER SORTIE at " + keyp + " (" + alt + ") now targeting " + retMO.Name + " " + retMOsector;
                            if (retMO.IsPrimaryTarget && retMO.Scouted) msg += " [[PRIMARY]]";
                            msg += " (" + acNumber + "x" + acType + ")";

                            sendChatMessageTo(sendArmy, msg, new object[] { });

                            Console.WriteLine("MoveBomb: " + msg);

                            string key = "bomber_messages_red";
                            if (retMO.AttackingArmy == 2) key = "bomber_messages_blue";

                            //Save the info message to DataDictionary so that MainMission can display it/save to radar etc
                            string timestring = "[[" + DateTime.UtcNow.ToString("HH:mmZ").Trim() + "]] ";
                            string msgs = "";
                            if (Mission.DataDictionary.ContainsKey(key)) msgs = (string)Mission.DataDictionary[key];
                            Mission.DataDictionary[key] = msgs + timestring + msg + Environment.NewLine;
                        }
                        */
                        //Console.WriteLine("MB:6");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("MoveBomb RandomObjectiveWithin - messages: " + ex.ToString()); return ret;
                    }
                    //Console.WriteLine("MB:7");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("MoveBomb RandomObjectiveWithin - #2: " + ex.ToString()); return ret;
                }
                }
            //Console.WriteLine("MB:8");
            return ret;
        }
        catch (Exception ex) {
            Console.WriteLine("MoveBomb RandomObjectiveWithin: " + ex.ToString()); return ret;
        }

    }

    //We send msg re: the bomber group's target.  BUT only if it is on Friendly territory (ie, leaving home for destination)
    //If it rejiggers its destination over enemy territory (happens when attacked, airGroup breaks up, etc)
    //then NO announcement.  That's just a thing that happens spontaneously as the result of mission events.
    public string sendObjectiveMessage(int sendArmy, string objectiveName, string IsPrimaryMsg, Point3d? lastWaypointPos, Point3d objectivePos, AiAirGroup airGroup, bool leakIt)
    {
        try {
            //if (leakIt) sendArmy = -1;

            string acType = MoveBombCalcs.GetAircraftType(airGroup);
            //string acNumber = airGroup.GetItems().Length.ToString();
            string acNumber = airGroup.NOfAirc.ToString("n0");
            Point3d airGroup_pos = airGroup.Pos();

            int chatArmy = sendArmy;
            if (leakIt) chatArmy = -1;

            if (sendArmy == 1 && Mission.DataDictionary.ContainsKey("generalstaff_discovered_blue")) chatArmy = -1; //If blue has found the GeneralStaff, broadcast to ALL rather than red only
            if (sendArmy == 2 && Mission.DataDictionary.ContainsKey("generalstaff_discovered_red")) chatArmy = -1; //If red has found the GeneralStaff, broadcast to ALL


            string ms = "Bomber Mission Departing for " + objectiveName;
            if (leakIt) ms += " (LEAKED!)";

            //bhugh, temp, XX2021-10, final battle temp thing
            //ms = ">>>>> FINAL BATTLE! Objective: Attack/Defend Blue COMM HQ - BD04";
            
            sendScreenMessageTo(chatArmy, ms, new object[] { });

            string keyp = MoveBombCalcs.correctedSectorNameKeypad(this, airGroup_pos);
            string alt = "A" + MoveBombCalcs.Meters2Angels(airGroup_pos.z).ToString();
            //if (retMO.OwnerArmy == 2) alt = Calcs.RoundInterval(pos.z, 1000).ToString() + "m";

            if (sendArmy == 2) alt = "" + ((double)(MoveBombCalcs.RoundInterval(airGroup_pos.z / 1000 * 10, 5)) / 10).ToString("F1") + "k";

            string altObj = "A" + MoveBombCalcs.Meters2Angels(objectivePos.z).ToString();
            //if (retMO.OwnerArmy == 2) alt = Calcs.RoundInterval(pos.z, 1000).ToString() + "m";

            if (sendArmy == 2) altObj = "" + ((double)(MoveBombCalcs.RoundInterval(objectivePos.z / 1000 * 10, 5)) / 10).ToString("F1") + "k";

            string LastWPalt = "";
            string lastWPSector = "";
            if (lastWaypointPos.HasValue)
            {
                LastWPalt = "A" + MoveBombCalcs.Meters2Angels(lastWaypointPos.Value.z).ToString();
                //if (retMO.OwnerArmy == 2) alt = Calcs.RoundInterval(pos.z, 1000).ToString() + "m";

                if (sendArmy == 2) LastWPalt = "" + ((double)(MoveBombCalcs.RoundInterval(lastWaypointPos.Value.z / 1000 * 10, 5)) / 10).ToString("F1") + "k";

                lastWPSector = MoveBombCalcs.correctedSectorNameKeypad(this, lastWaypointPos.Value);
            }

            string ar = "RED";
            if (sendArmy == 2) ar = "BLUE";

            string objectiveSector = MoveBombCalcs.correctedSectorNameKeypad(this, objectivePos);

            string msg = ">>>" + ar + " BOMBER SORTIE @ " + keyp + "/" + alt;
            if (lastWaypointPos.HasValue) msg += " to " + lastWPSector + "/" + LastWPalt + "";
            msg += " targeting " + objectiveName + " " + objectiveSector + "/" + altObj + "";
            
            msg += " (" + acNumber + "x" + acType + ")";
            //if (retMO.IsPrimaryTarget && retMO.Scouted) msg += " [[PRIMARY]]";
            msg += IsPrimaryMsg; //" [[PRIMARY]]" or similar if it is a primary objective or important objective, otherwise ""
            if (leakIt) msg += " [[Leaked!]]";

            //bhugh, temp, XX2021-10, final battle temp thing
            //msg = ">>>>> FINAL BATTLE! Only objective: Attack/Defend Blue RESERVE COMMUNICATIONS HQ in sector BD04";

            sendChatMessageTo(chatArmy, msg, new object[] { });

            Console.WriteLine("MoveBomb: " + msg);

            string key = "bomber_messages_red";
            if (sendArmy == 2) key = "bomber_messages_blue";

            //Save the info message to DataDictionary so that MainMission can display it/save to radar etc
            string timestring = "[[" + DateTime.UtcNow.ToString("HH:mmZ").Trim() + "]] ";
            string msgs = "";
            if (Mission.DataDictionary.ContainsKey(key)) msgs = (string)Mission.DataDictionary[key];
            Mission.DataDictionary[key] = msgs + timestring + msg + Environment.NewLine;

            //If leaking, add it to the enemy's message list, too
            //Note that if the enemy as reconned the General, then ALL enemy messages will be added to their
            //radar summary screen.  This is different--just a single leak about one mission.  So we need
            //to add it here.
            if (leakIt)
            {
                key = "bomber_messages_blue";
                if (sendArmy == 2) key = "bomber_messages_red";
                msgs = "";
                if (Mission.DataDictionary.ContainsKey(key)) msgs = (string)Mission.DataDictionary[key];
                Mission.DataDictionary[key] = msgs + timestring + msg + Environment.NewLine;
            }
            return msg;
        }

        catch (Exception ex)
        {
            Console.WriteLine("MoveBomb sendObjectiveMessages ERROR: " + ex.ToString());
            return "";
        }
    }



    //Returns a point within the changed airport within the given radius OR
    //Null if the attack point is not within/near an airport OR no suitable airport found
    public Tuple<Point3d?,AiAirport> ChangeAirports(Point3d p, int airportArmy, AiAirGroup airGroup, Point3d airGroup_pos, double favorfrontline_pct = 0)
    {
        Point3d retPos;

        AiAirport nearestAirfield = GetAirfieldAt(p);

        //check whether the attack point is within or very near an airfield
        //if (nearestAirfield.Pos().distance(ref p) > nearestAirfield.FieldR() * 1.25)
        if (MoveBombCalcs.CalculatePointDistance(nearestAirfield.Pos(), p) > 2000) //The GATTACK_POINT distance is often quite far from the target itself
        {
            //Console.WriteLine("MoveBomb: Attack point NOT within an airfield {0:n0} {1:n0} {2:n0} {3:n0} {4:n0}", nearestAirfield.Pos().x, nearestAirfield.Pos().y, p.x, p.y, Calcs.CalculatePointDistance(nearestAirfield.Pos(), p));
            return new Tuple<Point3d?, AiAirport>(null, null);
        }




        //Get the random airport within the given radius
        AiAirport ap = GetRandomAirfieldNear(p, moveAirportsDistance_m, moveAirportsDistance_OnEnemy_m, airportArmy, airGroup, airGroup_pos, favorfrontline_pct);

        //Console.WriteLine("MoveBomb: Attack point IS within an airfield {0:n0} {1:n0} {2:n0} {3:n0} {4:n0} {5} to {6}", nearestAirfield.Pos().x, nearestAirfield.Pos().y, p.x, p.y, Calcs.CalculatePointDistance(nearestAirfield.Pos(), p), nearestAirfield.Name(), ap.Name());

        if (ap != null)
        {

            //Choose a random point within the airfield radius
            double radius = ap.FieldR();
            Point3d center = ap.Pos();
            double dist = ran.NextDouble() * radius / 2;
            double angl = ran.NextDouble() * 2 * Math.PI;

            int numEnemyPlayers = MoveBombCalcs.gpNumberOfPlayersActive(GamePlay, airportArmy);

            if (numEnemyPlayers > 10) numEnemyPlayers = 10;

            //Mult & add will push target points further from the center of the objective with no or few enemy players online
            //and then bring it in close when there are a lot of enemy players
            //double mult = (12 - numEnemyPlayers) / 7; //prior to 2022/12
            double mult = (24 - numEnemyPlayers) / 7; //2022/12, making bombers generally LESS accurate unless quite a lot of players are online
            if (numEnemyPlayers == 0) mult = 20;
            if (mult < 0.5) mult = 0.5;

            //double add = (6 - numEnemyPlayers) / 4; //prior to 2022/12
            double add = (9 - numEnemyPlayers) / 4; //raising this # to 9/4 to make the AI bombers less accurate
            if (add <= 0) add = 0;

            dist = ran.NextDouble() * radius * mult + radius * add;

            retPos.x = Math.Cos(angl) * dist + center.x;
            retPos.y = Math.Sin(angl) * dist + center.y;
            retPos.z = 0;

            //return the SAME relative position to this new airfield as we had with the old airfield
            //This is important because the attack point is often quite distant from the airfield itself, in order to actually hit the airfield accurately
            //retPos.x = p.x - nearestAirfield.Pos().x + ap.Pos().x;
            //retPos.y = p.y - nearestAirfield.Pos().y + ap.Pos().y;

            //Ok, we're going to make the airport attacks more effective by just centering them more on the new airport (plus/minus the radius defined above, of course)
            //With Campaign21 3.0 this looks to be TOO accurate now, we'll go back to kinda inaccurate
            /*
            retPos.x = ap.Pos().x;
            retPos.y = ap.Pos().y;
            retPos.z = 0;
            */
            Console.WriteLine("MoveBomb: New attack point: {0:n0} {1:n0} {2:n0} {3:n0} {4:n0} {5} to {6}", ap.Pos().x, ap.Pos().y, retPos.x, retPos.y, MoveBombCalcs.CalculatePointDistance(ap.Pos(), retPos), nearestAirfield.Name(), ap.Name());
            return new Tuple<Point3d?, AiAirport>(retPos,ap);
        }
        else return new Tuple<Point3d?, AiAirport>(null, null); 

    }

    //return a point that is within the map coordinates.  Selecting any points & then correcting it to within the coordinates gives undue weight to points
    //near the edge of the map so lets just pick a random point within the radius & on the  map, without any undue weight to edges
    //XY_m is the large/major correction whereas aimXY_m is a small additional correction of the precise aim.
    public Point3d safePointSelect(Point3d pos, double XY_m, double aimXY_m)
    {
        double offMapBufferForAvoidingLeaveMap = -1000; //Max off map amount to allow as part of a normal flight plan
        Point3d NewPos = pos;
        for (int i = 0; i < 100; i++)
        {
            NewPos = pos;
            double XY = XY_m + aimXY_m;
            NewPos.x += ran.NextDouble() * 2 * XY - XY;
            NewPos.y += ran.NextDouble() * 2 * XY - XY;
            if (NewPos.x > twcmap_maxX + offMapBufferForAvoidingLeaveMap || NewPos.y > twcmap_maxY + offMapBufferForAvoidingLeaveMap || NewPos.x < twcmap_minX - offMapBufferForAvoidingLeaveMap || NewPos.y < twcmap_minY - offMapBufferForAvoidingLeaveMap) continue;
            return NewPos;            
        }
        return NewPos;

    }
	
	//.ESCORT follows path of target closely (REGARDLESS of the escorting a/c's own path)
			//.COVER ranges more widely and follows on path as set, (presumably coming ot rescue of target if needed?)
			//.FOLLOW is more similar to .COVER and follows OWN PATH not TARGET PATH
			//SO if ESCORTS are set to actually waypoint ESCORT with proper target
			//we can pretty much move bomber waypoints at will and escorts will follow them
			//As long as ESCORTS start at same place as bombers and have a km/h on the so they can keep up
			
			//.ESCORT - several .ESCORT waypoints in a row are kind of ignored - the currWay just skips to the last one and stays there as long as it is escorting the client.
			//That is why .ESCORT a/c currWay is often like 3 or 4 or even 5 or 6 or whatever when you would expect it to be 1
			
			//.NORMFLY - ALSO ###!!!!!! shows this same kind of behavior, at least sometimes.  Like Sunderland on pure ".NORMFLY" points just skips to currWay 37 and lands.  Instead of flying its whole route.
			
			//Maybe some of the others do this also?

    public bool updateAirWaypoints(AiAirGroup airGroup)
    {
        try
        {   
		 
            if (airGroup == null || !isAiControlledAirGroup(airGroup)) return false;					
			
            AiActor[] acs = airGroup.GetItems();
			
			string airGroupName = airGroup.Name();
			//sometimes airgroup doesn't have a name, if so it seems to have a "mother" that does.  We'll see. 2026/08
			if (airGroupName=="NONAME" && airGroup.motherGroup()!=null) airGroupName = airGroup.motherGroup().Name();
			
			/*
			//FOR TESTING - just delete all ai a/c as soon as spawned in...
			//ON_TESTSERVER
			
			if (mainmission.ON_TESTSERVER) {
				 List<AiActor> items = new List<AiActor>(airGroup.GetItems());

				Point3d pos = airGroup.Pos();

				if (items != null) foreach (AiActor actor in items.ToList())
				{
					AiAircraft aircraft = actor as AiAircraft;
					
					if (aircraft != null && isAiControlledPlane2(aircraft)) Timeout(5, ()=> aircraft.Destroy()); //trying timeout as a way to get around changing/deleting the items on the list while stepping through the list.
				}
				return false;
				
			}
			*/
			
			//So the "lost" a/c in the server that just circle endlessly, seem to be disattached or lost
			//<cover aircraft.  So we are putting this check here in hopes of catching them.			
			AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
			
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{ 
				fixNullWayPoints(airGroup);
				return false;
			}

            //So, to make aircraft (in say a submission) AVOID being reprogrammed by movebomb here
            //you just include 'nochange' or 'cover' in the airgroup name.  it is a bit tricky - include
            //it as part of the "number" no the regiment name etc.  Like:
            //[AirGroups]
            //  BoB_LW_KG26_Stab.02_NOCHANGE
            //  BoB_LW_KG26_Stab.01_NOCHANGE
            //[BoB_LW_KG26_Stab.02_NOCHANGE]
            //Change teh airgroups _way and everything else accordingly
			
			//Also ignoring ASR aircraft (Walrus & HE-115) as a couple of those patrol the respective coasts but
			//they are mostly for scenery purposes, not actively bombing etc
			
			int currWay = airGroup.GetCurrentWayPoint();
			
			//Sunderland without bombs is ASR for movebomb purposes...
			bool isASR = Calcs.isAsrAC(airGroup) || (Calcs.GetAircraftType(airGroup).ToLower().Contains("sunderland") && !airGroup.hasBombs() );
			
			if (acs == null || acs.Length == 0 ) { 
				Console.WriteLine("Movebomb MBTITG1: AIRGROUP HAS NO AIRCRAFT!!!! airgroup: {0} aircraft list & #: {1} {8} Mother: {2} Leader: {3} Daughters: {4} Clients: {5} Current Waypt: {6} isASR? {7}", airGroup.Name(), acs, airGroup.motherGroup()==null?"(no mother)":airGroup.motherGroup().Name(), airGroup.leaderGroup()==null?"(no leader)":airGroup.leaderGroup().Name(), airGroup.daughterGroups()==null?0:airGroup.daughterGroups().Length, airGroup.clientGroup()==null?"(no client)":airGroup.clientGroup().Name(), currWay, isASR, acs.Length );
				return false;
			}

            if (airGroupName.ToLower().Contains("cover")  || airGroupName.ToLower().Contains("nochange") || isASR ) {
                if (acs != null && acs.Length > 0) Console.WriteLine("Movebomb MBTITG2: airGroup.Name() includes NOCHANGE/COVER or isASR so not changing this airgroup: {0} : {1} Mother: {2} Leader: {3} Daughters: {4} Clients: {5} Current Waypt: {6} isASR? {7}", acs[0].Name(), airGroup.Name(), airGroup.motherGroup()==null?"(no mother)":airGroup.motherGroup().Name(), airGroup.leaderGroup()==null?"(no leader)":airGroup.leaderGroup().Name(), airGroup.daughterGroups()==null?0:airGroup.daughterGroups().Length, airGroup.clientGroup()==null?"(no client)":airGroup.clientGroup().Name(), currWay, isASR );
                return false;
            }

            if (mainmission.ON_TESTSERVER) Console.WriteLine("Movebomb MBTITG3: airGroup.Name() does not include NOCHANGE so changing this airgroup: {0} : {1} Mother: {2} Leader: {3} Daughters: {4} Clients: {5} Current Waypt: {6} isASR? {7}", acs[0].Name(), airGroup.Name(), airGroup.motherGroup()==null?"(no mother)":airGroup.motherGroup().Name(), airGroup.leaderGroup()==null?"(no leader)":airGroup.leaderGroup().Name(), airGroup.daughterGroups()==null?0:airGroup.daughterGroups().Length, airGroup.clientGroup()==null?"(no client)":airGroup.clientGroup().Name(), currWay, isASR );

            //Sometimes, just leave the route as-is
            if (ran.Next(20) == 1)
            {
                fixWayPoints(airGroup); //fix any problems, particularly add the two endpoints that will take the a/c off the map @ the end
                return false; //Just leave it as originally written sometimes
            }
			
			bool noAttacks =  true; //if the route includes NO attack points we'll just leave it alone.
			bool hasLanding = false; //if it lacks a landing we'll send to fixpoints to fix that up



            //for testing
			
            Console.WriteLine("MBT: Updating waypoints for {0} : {1} : {2} : {3}",  acs[0].Name(), (acs[0] as AiCart).InternalTypeName(), airGroup.Name(), airGroupName);
            foreach (AiWayPoint wp in CurrentWaypoints)
            {
                //AiWayPoint nextWP = wp; //NOITE: don't do this, doesn't make a new copy of the object
                Console.WriteLine("MBT: Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }
            

            
            double speedDiff = 0;
            double altDiff_m = 0;

            //Console.WriteLine("MBTITG: 2");
            //if (currWay< CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { CurrentWaypoints[currWay] });
            //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { CurrentWaypoints[currWay].Speed });
            //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { (CurrentWaypoints[currWay] as AiAirWayPoint).Action });

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
            int count = 0;
            //Console.WriteLine("MBTITG: 3");

            int army = airGroup.getArmy();
            int enemyArmy = 3 - army;

            bool isBomber = MoveBombCalcs.isHeavyBomber(airGroup) || MoveBombCalcs.isDiveBomber(airGroup);

            Point3d CurrentPos = airGroup.Pos();

            bool underRadar = false;
            if (isBomber && ran.Next(0, 15) == 0) underRadar = true; //One in 15 or so bomber groups fly under the radar
            //Console.WriteLine("Movebomb: Under radar: {0}", underRadar);

            bool update = false;
            AiWayPoint wpAdd = CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);

            if (wpAdd != null) NewWaypoints.Add(wpAdd); //Always have to add current pos/speed as first point or things go w-r-o-n-g

            AiWayPoint lastWP = null;

            foreach (AiWayPoint wp in CurrentWaypoints)
            {
                try
                {
                    AiAirWayPoint nextWP = makeNewAiAirWaypointFromOld( wp as AiAirWayPoint);
                    bool defendUltimate = false; //bhugh, 2021-10, final battle temp fix
                    //Console.WriteLine( "Target: {0}", new object[] { wp });

                    if ((wp as AiAirWayPoint).Action == null) return false;

                    Point3d? newAirportPosition = wp.P as Point3d?;
                    AiAirport newAirport = null;
                    Point3d? newObjectivePosition = wp.P as Point3d?;
                    Mission.MissionObjective newMissionObjective = null;
                    Point3d? lastWaypointPos = null;
                    if (lastWP != null) lastWaypointPos = lastWP.P;
                    bool leakIt = false;

                    if (moveAirports && ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_TARG || (wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT))
                    {
                        double favorfrontlineairports_pct = 0.5;   //0 = no favor, 1 = fully favor
                        var tup = ChangeAirports(wp.P, enemyArmy, airGroup, CurrentPos, favorfrontlineairports_pct);
                        newAirportPosition = tup.Item1;
                        newAirport = tup.Item2;
                        //newAirportPosition = ChangeAirports(wp.P, enemyArmy, airGroup, CurrentPos); 
                    }

                    //Because of announcing the bomber group targets, which happens via ChangeObjectives,
                    //we don't want to run this in case the target is an airfield.
                    //Also, sometimes we'll switch from attacking an airfield to attacking a target.
                    double targetObjectiveChance = 0.60;
                    if (army==2) targetObjectiveChance = 0.60; //blues are a bit pathetic so we are helping them with disabling objectives/primaries
                    if (newAirport == null || ran.NextDouble() > targetObjectiveChance)
                    {
                        newAirportPosition = null; //Make sure there is no new airport position; this forces the newObjectivePosition to be used
                        if (attackObjectives && ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_TARG || (wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT))
                        {
                            var tup = ChangeObjectives(wp.P, enemyArmy, airGroup, CurrentPos);
                            newObjectivePosition = tup.Item1;
                            newMissionObjective = tup.Item2;

                        }
                    }
					
					if ((wp as AiAirWayPoint).Action ==AiAirWayPointType.LANDING) 	hasLanding = true;								
                    changeLimit changeL = new changeLimit();
                    if (changeLimits.ContainsKey((wp as AiAirWayPoint).Action))
                    {
                        Point3d pos;
                        double speed;

                        changeL = changeLimits[(wp as AiAirWayPoint).Action];
                        //if (count == 1 && (isBomber)) //for heavy/dive bombers the 2nd waypoint is more of a dogleg than a giant cross-map trip.
						if ( isBomber && (wp as AiAirWayPoint).Action == AiAirWayPointType.NORMFLY) //for heavy/dive bombers legs except for bomb drop are more of a dogleg than a giant cross-map trip.
                        {
                            changeL.XY_m = changeL.XY_m / 5;
                        }

                        //TODO: We could have higher/lower altitude & speed apply to the entire mission for this airgroup rather than varying waypoint by waypoint. 
                        //that might be a more sensible approach

                        switch ((wp as AiAirWayPoint).Action)
                        {
                            /*case AiAirWayPointType.GATTACK_POINT:
                                //Console.WriteLine( "Updating, current TASK: {0}", new object[] { airGroup.getTask() });
                                //Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
                                pos = wp.P;                        
                                speed = wp.Speed;
                                pos.x += ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;
                                pos.y += ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;
                                speed += speed * (ran.NextDouble() * 2 * changeL.speed_percent/100 - changeL.speed_percent / 100);
                                //don't change the altitude/pos.z for GATTACK_POINT type (it should generally be on the ground anyway?  There could be problems if our attack point is too far above or below the ground maybe?  If so we might need to specify ground level for our chosen x,y point?)
                                //Update: actually the pos.z of the GATTACK_POINT is the altitude of the bombers when attacking, not the altitude of the point to attack
                                //So, we can treat this exactly like all the other task types                      
                                nextWP = new AiAirWayPoint(ref pos, speed);
                                (nextWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;
                                //Console.WriteLine( "Target after: {0}", new object[] { wp });
                                //Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                                //Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                                update = true;
                                break;
                                */
                            case AiAirWayPointType.GATTACK_TARG:
                                //Console.WriteLine( "Updating, current TASK: {0}", new object[] { airGroup.getTask() });
                                Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
								noAttacks = false;
                                pos = wp.P;
                                if (newAirport != null && newAirportPosition.HasValue)
                                {
                                    Console.WriteLine("MoveBomb: Moving to attack an airport! {0:F0} {1:F0}", wp.P.x, wp.P.y);
                                    pos = newAirportPosition.Value;
                                    pos.z = wp.P.z;
                                }
                                else if (newMissionObjective !=null && newObjectivePosition.HasValue)
                                {
                                    Console.WriteLine("MoveBomb: Moving to attack an objective! {0:F0} {1:F0}", wp.P.x, wp.P.y);
                                    pos = newObjectivePosition.Value;
                                    pos.z = wp.P.z;

                                }

                                if (speedDiff == 0) speedDiff = wp.Speed * (ran.NextDouble() * 2.0 * changeL.speed_percent / 100.0 - changeL.speed_percent / 100);
                                speed = wp.Speed + speedDiff;
                                //pos.x += ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;
                                //pos.y += ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;

                                //so, (wp as AiAirWayPoint).Target; is NULL for SOME REASON, even though the groundactor to attack is set in the .mis file
                                //AiActor currTarget = (wp as AiAirWayPoint).Target;
                                /*
                                if (currTarget == null)
                                {
                                    Console.WriteLine("MoveBomb: Target is NULL!! Breaking");
                                    Console.WriteLine("MoveBomb: {0} {1} {2} {3}", (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Target.Name(), (wp as AiAirWayPoint).GAttackPasses, (wp as AiAirWayPoint).GAttackType);

                                    AiActor[] acts = airGroup.GetItems();
                                    foreach (AiActor act in acts)
                                    {
                                        Console.WriteLine("MoveBomb: {0}", act.Name());
                                    }
                                    break;
                                }
                                */

                                GroundStationary newTarget = null;
                                //Choose another ground stationary somewhere within the given radius of change, starting with the GATTACK point since we don't have an actual GATTACK target actor; make sure it is alive if possible
                                GroundStationary[] stationaries = GamePlay.gpGroundStationarys(pos.x, pos.y, changeL.XY_m);
                                //Console.WriteLine("MoveBomb: Looking for nearby stationary");
                                for (int i = 1; i < 20; i++)
                                {

                                    if (stationaries.Length == 0) break;
                                    int newStaIndex = ran.Next(stationaries.Length - 1);
                                    if (stationaries[newStaIndex] != null && stationaries[newStaIndex].IsAlive &&
                                        (stationaries[newStaIndex].pos.x != pos.x ||
                                        stationaries[newStaIndex].pos.y != pos.y)
                                        && GamePlay.gpFrontArmy(stationaries[newStaIndex].pos.x, stationaries[newStaIndex].pos.y) == enemyArmy)
                                    {
                                        newTarget = stationaries[newStaIndex];
                                        //Console.WriteLine("MoveBomb: FOUND a stationary");
                                        break;
                                    }
                                }
                                //In case we didn't find a ground target there, expand the search radius a bit & try again
                                if (newTarget == null)
                                {
                                    //Console.WriteLine("MoveBomb: Looking for further afield stationaries");
                                    GroundStationary[] stationaries2 = GamePlay.gpGroundStationarys(pos.x, pos.y, 3 * changeL.XY_m);
                                    for (int i = 1; i < 20; i++)
                                    {
                                        if (stationaries2.Length == 0) break;
                                        int newStaIndex = ran.Next(stationaries2.Length - 1);
                                        if (stationaries2[newStaIndex] != null && stationaries2[newStaIndex].IsAlive &&
                                        (stationaries2[newStaIndex].pos.x != pos.x ||
                                        stationaries2[newStaIndex].pos.y != pos.y))
                                        {
                                            newTarget = stationaries2[newStaIndex];
                                            break;
                                        }
                                    }
                                }

                                Point3d newPos = pos;
                                //Use the position of the newly found ground actor as the new attack position, IF the actor exists/was found
                                if (newTarget != null)
                                {
                                    //Console.WriteLine("MoveBomb: Found a stationary, updating attack position");
                                    newPos.x = newTarget.pos.x;
                                    newPos.y = newTarget.pos.y;
                                }
                                //3rd approach, just move the attack point by our usual amount
                                else
                                {
                                    //Console.WriteLine("MoveBomb: No stationary found, updating attack position");
                                    newPos = safePointSelect(pos, changeL.XY_m, changeL.aimXY_m);

                                }

                                newPos.z += altDiff_m;


                                nextWP = new AiAirWayPoint(ref newPos, speed);
                                (nextWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;  //keep action same
                                (nextWP as AiAirWayPoint).GAttackPasses = (wp as AiAirWayPoint).GAttackPasses;  //keep # passes the same.  TODO: could change this in some reasonable but random way.
                                (nextWP as AiAirWayPoint).GAttackType = (wp as AiAirWayPoint).GAttackType;  //keep attack type the same. TODO: could change this randomly

                                if ((newTarget as AiActor) != null) (nextWP as AiAirWayPoint).Target = newTarget as AiActor;  //change to newly selected target
                                                                                                                              //Console.WriteLine( "Target after: {0}", new object[] { wp });
                                                                                                                              //Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                                string nm = "(null)";
                                //if (((wp as AiAirWayPoint).Target as AiActor) != null) nm = ((wp as AiAirWayPoint).Target as AiActor).Name(); //doesn't work bec. grounstationaries are never AiActors.  We could try looking for AiGroundActors AiGroundGroups, or even AirGroups instead, maybe.  
                                //Console.WriteLine("Old Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (wp as AiAirWayPoint).Action, nm, (wp as AiAirWayPoint).P.x, (wp as AiAirWayPoint).P.y, (wp as AiAirWayPoint).GAttackPasses, (wp as AiAirWayPoint).GAttackType });
                                //Console.WriteLine ("New Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (wp as AiAirWayPoint).Action, nm, (nextWP as AiAirWayPoint).P.x, (nextWP as AiAirWayPoint).P.y, (nextWP as AiAirWayPoint).GAttackPasses, (nextWP as AiAirWayPoint).GAttackType });
                                /* Console.WriteLine( "New Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (nextWP as AiAirWayPoint).Action, (nextWP as AiAirWayPoint).Target.Name(), (nextWP as AiAirWayPoint).Target.Pos().x, (nextWP as AiAirWayPoint).Target.Pos().y, (nextWP as AiAirWayPoint).GAttackPasses, (nextWP as AiAirWayPoint).GAttackType }); */

                                update = true;
                                break;
								

								
                            case AiAirWayPointType.GATTACK_POINT:
                            case AiAirWayPointType.HUNTING:
                            case AiAirWayPointType.NORMFLY:
                            case AiAirWayPointType.RECON:
                            case AiAirWayPointType.AATTACK_FIGHTERS:
                            case AiAirWayPointType.AATTACK_BOMBERS:
								noAttacks = false;

                                //If fuel is low they still get some patrols but can't keep coming
                                //back to the area over & over
                                double fuelReserveStrength = mainmission.MO_CalculateMilitaryStrength((ArmiesE)army, Mission.MO_MilitaryStrengthType.Military_Fuel_Supply);
                                bool fuelChance = true;
                                if (count > 2 && fuelReserveStrength < 0.4 && ran.Next() > fuelReserveStrength * 2.5) fuelChance = false;
                                if (!fuelChance) continue;  //Low on fuel, sometimes we just skip points where we attack, bomb, patrol, etc.

                                //Console.WriteLine( "Updating, current TASK: {0}", new object[] { airGroup.getTask() });
                                //Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
                                pos = wp.P;

                                //Console.WriteLine("Movebomb - Target before: {0:F0} {1:F0} {2:F0}", pos.x, pos.y, pos.z);

                                //GetRandomAirfieldNear(p, moveAirportsDistance_m, airportArmy);

                                //Add first "airfield switch" for bomber groups, most of the time.  They fly low/under radar to some relatively nearby airport, making it appear as though they are starting
                                //at different airports, not just at the one spot where they spawn in.
                                /*
                                if (count == 0 && currWay >= count && isBomber && (wp as AiAirWayPoint).Action == AiAirWayPointType.NORMFLY && ran.Next(4) > 0)
                                {
                                    double airportChange_m = 25000;
                                    AiAirport ap = GetRandomAirfieldNear(pos, airportChange_m, 0, army, null, new Point3d (0,0,0));
                                    Point3d airportPos = ap.Pos();
                                    airportPos.z = airportPos.z + 200;
                                    AiAirWayPoint airportWP = new AiAirWayPoint(ref airportPos, wp.Speed);
                                    (airportWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;
                                    NewWaypoints.Add(airportWP);
                                }
                                */

                                /*
                                //Add first extra waypoint that's just a little jog, for bomber groups
                                if (count == 0 && currWay >= count && isBomber && (wp as AiAirWayPoint).Action == AiAirWayPointType.NORMFLY)
                                {
                                    double firstChange = changeL.XY_m / 10;
                                    Point3d firstPos = safePointSelect(pos, firstChange, 0);
                                    AiAirWayPoint firstWP = new AiAirWayPoint(ref firstPos, wp.Speed);
                                    (firstWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;
                                    NewWaypoints.Add(firstWP);
                                }
                                */


                                if ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT && newAirportPosition.HasValue)
                                {
                                    //Console.WriteLine("MoveBomb: Moving airport of attack!");
                                    Console.WriteLine("MoveBomb: Moving to attack an airport! {0:F0} {1:F0}", wp.P.x, wp.P.y);
                                    pos = safePointSelect(newAirportPosition.Value, 0, changeL.aimXY_m);
                                    pos.z = wp.P.z;
                                }
                                else if ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT && newObjectivePosition.HasValue)
                                {
                                    Console.WriteLine("MoveBomb: Moving to attack an objective! {0:F0} {1:F0}", wp.P.x, wp.P.y);
                                    pos = safePointSelect(newObjectivePosition.Value, 0, changeL.aimXY_m);
                                    pos.z = wp.P.z;

                                }
                                else
                                {
                                    double XYm = changeL.XY_m;
                                    if (!isBomber && count > 2) XYm = XYm / 8; //fighters are more or less  regular patrol routes
                                    //So to randomize them a bit we allow the 1st 2 waypoints to be more random,
                                    //But the final ones follow more the planned patrol route with just a bit of variation

                                    //bhugh, temp, XX2021-10 - for end of campaign ONLY
                                    
                                    int numBluePlayers = MoveBombCalcs.gpNumberOfPlayersActive(GamePlay, 2);
                                    int chance = numBluePlayers;
                                    if (chance > 3) chance = 3;

                                    

                                    //2021-11 setting ot FALSE ot disable this part but leaving
                                    //so we can concentrate cover on a certain area if we want to, easily
                                    if ( false && !isBomber&& army == 2 && ran.Next(0, 4) <= (3 - chance) && fuelChance)
                                    {
                                        if (count % 2 == 0)
                                        {
                                            //int numEnemyPlayers = MoveBombCalcs.gpNumberOfPlayersActive(GamePlay, 2);
                                            pos = safePointSelect(new Point3d(298220, 30862, 262), 9500, 0);
                                            //AiAirWayPointType.AATTACK_BOMBERS:
                                            //(wp as AiAirWayPoint).Action ultimate
                                            //AiWayPoint wpAdd = CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);
                                            defendUltimate = true;
                                        } else
                                        {
                                            pos = safePointSelect(new Point3d(252687, 67682, 262), 95000, 0);
                                            //AiAirWayPointType.AATTACK_BOMBERS:
                                            //(wp as AiAirWayPoint).Action ultimate
                                            //AiWayPoint wpAdd = CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);
                                            defendUltimate = true;

                                        }
                                    }
                                    else
                                    {
                                        pos = safePointSelect(pos, XYm, 0);
                                     
                                    }
                                  

                                    pos.z = wp.P.z;
                                }
                                    

                                speed = wp.Speed;

                                if (speedDiff == 0) speedDiff = speed * (ran.NextDouble() * 2 * changeL.speed_percent / 100 - changeL.speed_percent / 100);
                                //Note that bombers can outrun their cover aircraft here if we're not careful.  For now we're dealing by making cover a/c go a fair bit faster than their bombers in the .mis file
                                //We're adjusting bomber speed here but NOT cover a/c speed (ESCORT)
                                if (isBomber && speedDiff > .08 * speed) speedDiff = .08 * speed; //limit bomber speed increase, so they don't ditch their escorts
                                speed += speedDiff;
                                double zSave = pos.z;

                                //Keep the same delta altitude, unless it hasn't been set yet OR it is too low
                                if (altDiff_m == 0 || zSave * (1 - changeL.alt_percent / 100) > zSave + altDiff_m)
                                {

                                    //Figure alt change by both the absolute (meters) and percent method, then pick which to use
                                    double zChangeAbs = ran.NextDouble() * 2.0 * changeL.alt_m - changeL.alt_m;
                                    double zChangePerc = zSave * (ran.NextDouble() * 2.0 * changeL.alt_percent / 100.0 - changeL.alt_percent / 100.0);
                                    double zChangeFinal = zChangeAbs;
                                    if (changeL.alt_percent / 100.0 * zSave > changeL.alt_m) zChangeFinal = zChangePerc;  //if (potential max) perc change is larger then abs change then we go with perc change
                                    if (zSave * (1 - changeL.alt_percent / 100) > zChangeAbs) zChangeFinal = zChangePerc; //if actual abs change is less than min possible perc change than we go with perc change (to prevent setting altitude unreasonably low)
                                    altDiff_m = zChangeFinal;
                                }

                                pos.z += altDiff_m;

                                //if (zSave<changeL.alt_m && pos.z < zSave) pos.z = zSave;  //
                                if (pos.z < 100 && pos.z < zSave) pos.z = 100; //Never altitude less than 100m, unless the pre-set alt was less than 100m & this is equal to or greater than the previous set altitude                        
                                                                               //Console.WriteLine("Target after: {0:F0} {1:F0} {2:F0}", pos.x, pos.y, pos.z);

                                nextWP = new AiAirWayPoint(ref pos, speed);
                                (nextWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;
                                if (defendUltimate) (nextWP as AiAirWayPoint).Action = AiAirWayPointType.AATTACK_BOMBERS; //bhugh, XX2021-10, temp, final ultimate battle temp fix - 2021-11 leaving in place so we can use as needed
                                //Console.WriteLine( "Target after: {0}", new object[] { nextWP });
                                //Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                                //Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                                update = true;
                                break;


                        }
                        if (underRadar)
                        {
                            //AI get a break of 600ft altitude for staying-below-radar purposes.  soo 800-900 ft or below is off radar.
                            //There isn't much point in sending AI missions that are COMPLETELY off radar as no one will even know about them.  But if we keep them at the alt where they will 
                            //kind of phase in/out of radar that would be ideal
                            //Radar starts to phase out @ 500ft AGL & phases all the way out at 175ft AGL.  60 m is 196 ft, 63m = 207ft, so somewhere in there for the min.  5000m/40+40 = 165m or 550ft, so just above the start of "phase-out altitude"
                            //nextWP.P.z = 375 + altDiff_m / 10;
                            nextWP.P.z = 40 + altDiff_m / 40;  //Want it below 500 ft OR thereabouts
                            //if (nextWP.P.z < 300) nextWP.P.z = 300; //275m is about 900 ft alt = 300ft for breathers = still mostly off radar when over water (and probably right off it over land. where AGL will be lower).
                            if (nextWP.P.z < 63) nextWP.P.z = 63; //UPdate: As of 2021, AI aircraft can fly low just fine.  So radar thing is changed.
                            update = true;
                        }
                    }



                    if (count >= currWay)
                    {
                        NewWaypoints.Add(nextWP);
                        
                        //The MO position is correct but lacks altitude.  so we add it from the waypoint.
                        //We could just push the waypoint exact Pos but sometimes it is W-A-Y off the actual
                        //objective & we don't need the players to know that.  From their perspective the bombers
                        //are targeting that particular objective and for various reasons sometimes they get mixed
                        //up and miss or whatever.                        

                        try
                        {
                            if (newMissionObjective != null)
                            {
                                Point3d objectivePosWithAlt = newMissionObjective.Pos;
                                objectivePosWithAlt.z = nextWP.P.z;
                                string IsPrimaryMsg = "";

                                //BOMBER MISSION LEAKS.  Leak raids on primaries quite often, larger primary raids very often.
                                //Also, very large raids of any kind quite often
                                if (newMissionObjective.IsPrimaryTarget && newMissionObjective.Scouted)
                                {
                                    IsPrimaryMsg = " [[PRIMARY]]";
                                    //Leak primary missions sometimes, but way more often if large attacking group
                                    if (ran.NextDouble() > 0.9) leakIt = true;
                                    else if (airGroup.GetItems().Length >= 5 && airGroup.GetItems().Length < 8 && ran.NextDouble() > 0.7) leakIt = true;
                                    else if (airGroup.GetItems().Length >= 5 && ran.NextDouble() > 0.25) leakIt = true;
                                }
                                else if (airGroup.GetItems().Length >= 6 && ran.NextDouble() > 0.97) leakIt = true;

                                sendObjectiveMessage(army, newMissionObjective.Name, IsPrimaryMsg, lastWaypointPos, objectivePosWithAlt, airGroup, leakIt);
                            }
                            else if (newAirport != null)
                            {
                                Point3d objectivePosWithAlt = newAirport.Pos();
                                objectivePosWithAlt.z = nextWP.P.z;
                                string IsPrimaryMsg = "";
                                if (airGroup.GetItems().Length >= 6 && ran.NextDouble() > 0.5) leakIt = true;
                                //if (newMissionObjective.IsPrimaryTarget && newMissionObjective.Scouted) IsPrimaryMsg = " [[PRIMARY]]";
                                sendObjectiveMessage(army, mainmission.AirfieldTargets[newAirport.Name()].Item2, IsPrimaryMsg, lastWaypointPos, objectivePosWithAlt, airGroup, leakIt);
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb CurrentPosWaypointLOOP MESSAGES ERROR: " + ex.ToString()); }

                        lastWP = makeNewAiAirWaypointFromOld( nextWP);

                        /*
                        if (update)
                        {
                            Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                            Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                        }
                        */

                    }

                    //Console.WriteLine("MBTITG: 4");
                    count++;
                }
                catch (Exception ex) { Console.WriteLine("MoveBomb CurrentPosWaypointLOOP ERROR: " + ex.ToString()); }

            }
			
			if (!hasLanding) {
				Console.WriteLine( "MBT: The plane's route had no LANDING anywhere so we will update it & fixwaypoints.");
				update = true;
				AiAirport ap = CoverCalcs.GetRandomAirfieldNear(GamePlay, lastWP.P, 32000);
                        if (ap != null)
                        {
                            Point3d landPos = ap.Pos();
                            
							landPos.x +=50;
                            landPos.z += 70; //trying to keep them from ground crashing near airports . . . 
                            AiAirWayPointType landaawpt = AiAirWayPointType.LANDING;
                            AiAirWayPoint landaaWP = new AiAirWayPoint(ref landPos, 50); // 50 mps ~= 100 mph, so reasonable pre-landing speed.                    
                            landaaWP.Action = landaawpt;
                            NewWaypoints.Add(landaaWP); //do add
                        }
				
			} else if (noAttacks) {
				 Console.WriteLine( "MBT: The plane's route had no ATTACKS so we are leaving it unchanged");
				 return false;
				
			}

            foreach (AiWayPoint wp in NewWaypoints)
            {
                AiWayPoint nextWP = wp;
                Console.WriteLine( "MBT: Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }


            //NewWaypoints.Add(CurrentPosWaypoint(airGroup));
            //NewWaypoints.AddRange(SetWaypointBetween(airGroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));
            //NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));


            if (update)
            {
                //Console.WriteLine("MBTITG: Updating this course");
                airGroup.SetWay(NewWaypoints.ToArray());
                fixWayPoints(airGroup); //fix any problems that might have resulted from the new waypoint fixes.
                return true;
            }
            else
            { return false; }

        }

        catch (Exception ex) { Console.WriteLine("MoveBomb UpdateWaypoint: " + ex.ToString()); return false; }
    }

    public bool playersNearby(AiAirGroup airGroup, double dist_m = 14000)
    {
        Tuple<double, double> dist = getDistanceToNearestLivePilot(airGroup);
        //Console.WriteLine("MoveBomb: Players nearby {0} {1} ", dist.Item1 == null, (double)(dist.Item1));
        if (dist.Item1 == -1 || (double)(dist.Item1) > dist_m) return false; //no players nearby, at least 10km away  OR the airGroup doesn't even exist, whatever
        return true; //Players nearby
    }
	
	public bool playersNearby(AiAircraft aircraft, double dist_m = 14000)
    {
        Tuple<double, double> dist = getDistanceToNearestLivePilot(aircraft);
        //Console.WriteLine("MoveBomb: Players nearby {0} {1} ", dist.Item1 == null, (double)(dist.Item1));
        if (dist.Item1 == -1 || (double)(dist.Item1) > dist_m) return false; //no players nearby, at least 10km away  OR the airGroup doesn't even exist, whatever
        return true; //Players nearby
    }
	

    //So setting AI airgroups to LANDING is our clue that we are free to despawn them at any time. We first check there
    //are no live players nearby to see the despawn
    public void checkToDespawnOldAirgroups(AiAirGroup airGroup, bool timeout = false) {
        try
        {
            //Console.WriteLine("MoveBomb: Checking AI airgroups whose mission is complete with task LANDING: " + airGroup.Name() + " {0} {1} {2} ",
            //!AirgroupsWayPointProcessed.Contains(airGroup), airGroup.GetItems().Length == 0, !isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft));
            //if (!AirgroupsWayPointProcessed.Contains(airGroup) || airGroup == null || airGroup.GetItems() == null || airGroup.GetItems().Length == 0 || !isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft)) return; //only process groups that have been in place a while, have actual aircraft in the air, and ARE ai
			
			//2026/08 - we're trying again to use this to remove ANY AI controlled airgroup IF it is task==landing and on friendly territory far from a human etc.
			if (airGroup == null || airGroup.GetItems() == null || airGroup.GetItems().Length == 0 || !isAiControlledAirGroup(airGroup)) return;
			
			string name = airGroup.Name();
			string type = Calcs.GetAircraftType(airGroup);
			
            AiAirGroupTask task = airGroup.getTask();
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
			
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{ 
				fixNullWayPoints(airGroup);
				return;
			}
            int currWay = airGroup.GetCurrentWayPoint();
            bool landingWaypoint = false;
            Console.WriteLine("MoveBomb-checkToDespawnOldAirgroups: {5} {6} {0} {1} {2} {3} {4} ", CurrentWaypoints.Length, currWay, (CurrentWaypoints[currWay] as AiAirWayPoint).Action, task, (playersNearby(airGroup)), airGroup.Name(), Calcs.GetAircraftType(airGroup) );

            if (CurrentWaypoints.Length >= currWay && (CurrentWaypoints[currWay] as AiAirWayPoint).Action == AiAirWayPointType.LANDING) landingWaypoint = true;

            //if (task != AiAirGroupTask.LANDING || !landingWaypoint) return; //Task LANDING is our clue these are ready to get out of here, accepting EITHER task landing OR LANDING is current Waypoint action caused trouble (because waypoing "landing" can be set many hundreds of miles from the actual landing spot), so we require BOTH of these set to LANDING before actually disapparating them.
            if (!landingWaypoint) return; //Task LANDING is our clue these are ready to get out of here, loosening this up to try to get rid of useless/finished airgroups more quickly.
            //if (playersNearby(airGroup, 14000)) return; //Don't dis-apparate them if there are any players nearby to see it happen. Checking this in safeDestroy routine now.

            double airportDistance_m = DistanceToNearestAirport(airGroup as AiActor);

            if (airportDistance_m > 8000) return;

            if (GamePlay.gpFrontArmy(airGroup.Pos().x, airGroup.Pos().y) != airGroup.getArmy()) return;

            List<AiActor> items = new List<AiActor>(airGroup.GetItems());

            Point3d pos = airGroup.Pos();

            if (items != null) foreach (AiActor actor in items.ToList())
            {
				AiAircraft aircraft = actor as AiAircraft;
				Console.WriteLine("MoveBomb-checkToDespawnOldAirgroups: REMOVING AIRCRAFT (should be landing & far from players OR timed out at {0:N0} {1:N0} Action: {2} Task: {3} Players nearby? {4} : {5} {6} ", aircraft.Pos().x, aircraft.Pos().y, (CurrentWaypoints[currWay] as AiAirWayPoint).Action, task, (playersNearby(airGroup)), airGroup.Name(), Calcs.GetAircraftType(airGroup) );
				
				safeDestroyOldAircraft( aircraft, "SAFE_MoveBomb_Landing", name: name, type: type);
               
            }
            //Console.WriteLine("MoveBomb: Checking {0} {1} {2} {3} {4} {5:N0}", CurrentWaypoints.Length, currWay, (CurrentWaypoints[currWay] as AiAirWayPoint).Action, task, (playersNearby(airGroup)), airportDistance_m);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb Check LANDING ERROR: " + ex.ToString()); }
    }
	
	Dictionary<AiAircraft,DateTime> safeDestroyOldAircraft_list = new Dictionary<AiAircraft,DateTime> ();
	
	public void safeDestroyOldAircraft (AiAircraft aircraft, string reason = "", bool force = false, int count = 0, string name = "", string type = "", bool recursive = false) {			
		try {	
			bool newPlane = false;
			Console.WriteLine("MoveBomb SDOA: 1");
			mainmission.AircraftDestroyedList[aircraft] = reason; //add this early in case a/c is somehow .destroy() before we get to it
			
			if (!safeDestroyOldAircraft_list.ContainsKey(aircraft)) {
				newPlane = true;
				safeDestroyOldAircraft_list[aircraft] = DateTime.UtcNow;
				Console.WriteLine("safeDestroyOldAircraft: New aircraft to destroy: {0} {1} {2})",name, type, reason);
			} else if (!recursive) {
				Console.WriteLine("safeDestroyOldAircraft: Aircraft to destroy but already in queue, dropping: {0} {1} {2}",name, type, reason);
				return;				
			}
			
			double timeDiff_s = DateTime.UtcNow.Subtract(safeDestroyOldAircraft_list[aircraft]).TotalSeconds;
			
			Console.WriteLine("MoveBomb SDOA: 2 Time waiting {0}s", timeDiff_s);
			if ((playersNearby(aircraft, 10000) && !force) ||  !isAiControlledPlane2(aircraft)) {
					Timeout(30.0, () => {safeDestroyOldAircraft(aircraft, reason, force,  count + 1, name, type, recursive: true);});					//if this happens to be far from its group, could be near a player; we'll check
					Console.WriteLine("MoveBomb SDOA: 3 - Not .destroying {0} {1} because players nearby or not AI controlled. Time waiting {2}s. {3} recursive calls.", name, type, count, timeDiff_s);
					return;
			}
			
			if (aircraft == null) { 
				Console.WriteLine("MoveBom SDOA: ERROR: Aircraft is NULL - this should never happen! {0} {1}", name, type);
				return;
			}

				

			Console.WriteLine("MoveBomb SDOA: 6");

			mainmission.AircraftDestroyedList[aircraft] = reason;
			Console.WriteLine("MoveBomb SDOA: 7");			
			Timeout(1, ()=> aircraft.Destroy()); 
			Console.WriteLine("MoveBomb SDOA: 8");
			Console.WriteLine("MoveBomb-safeDestroyOldAircraft: Just destroyed {0} {1} after waiting {2}s  ", name, type, timeDiff_s );				
			Console.WriteLine("MoveBomb SDOA: 9");
					
					
			
		}
        catch (Exception ex) { Console.WriteLine("MoveBomb Check SDOA LANDING ERROR: " + ex.ToString()); }
	}
	
    public void printAirgroupNames(AiAirGroup[] airGroups)
    {
        foreach (AiAirGroup airGroup in airGroups)
        {
            Console.Write(airGroup.Name() + " ");
        }
        Console.WriteLine();
    }

    public void printAttachedAirgroups(AiAirGroup airGroup)
    {
        /*
         * So, airgroups that are ie escorting bombers have task "defending".  The airgroup they are defending is the "client"
         * 
         * Task RETURN *might* mean that the escorts are returning to their client group.  Not 100% sure however.
         * 
         * If airgroups split up, say when landing (or maybe other situations?) then the new split-off airgroup has motherGroup() set to the original group it split off from.
         * 
         * Airgroups that are attacking some aircraft are "ATTACK_AIR".  There doesn't seem to be the target of the attack available anyway.
         * 
         * enemies, candidates, mothergroups, daughtergroups, attachedgroups I haven't found used at all, yet.
         * 
         * 
         * */
        try
        {
            if (!AirgroupsWayPointProcessed.Contains(airGroup) || airGroup.GetItems().Length == 0 || !isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft)) return; //only process groups that have been in place a while, have actual aircraft in the air, and ARE ai
            AiAirGroupTask task = airGroup.getTask();
            Console.WriteLine("Airgroup {0} info & attached groups: {1}", airGroup.Name(), task);
            
            if (airGroup.clientGroup() != null) Console.WriteLine("client: {0}", airGroup.clientGroup().Name());
            if (airGroup.leaderGroup() != null) Console.WriteLine("leader: {0}", airGroup.leaderGroup().Name());
            if (airGroup.motherGroup() != null) Console.WriteLine("mother: {0}", airGroup.motherGroup().Name());

            if (airGroup.attachedGroups().Length > 0)
            {
                Console.WriteLine("Attached groups");
                printAirgroupNames(airGroup.attachedGroups());
            }
            
            if (airGroup.candidates().Length > 0)
            { 
                Console.WriteLine("Candidates");
                printAirgroupNames(airGroup.candidates());
            }
            if (airGroup.enemies().Length > 0)
            {
                Console.WriteLine("Enemies");
                printAirgroupNames(airGroup.enemies());
            }
            
            if (airGroup.daughterGroups().Length > 0)
            {
                Console.WriteLine("Daughter groups");
                printAirgroupNames(airGroup.daughterGroups());
            }
            
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb print groups ERROR: " + ex.ToString()); }


    }



    public void checkNewAirgroups()
    {
		//2026/08: So sometimes airgroups get splintered.  YOu can find this out by looking for
		//airgroup name becomes NONAME and it has a MOTHERGROUP pointing to the old airgroup
		//So right now these get reprocessed with new targets etc.  This OK  most of hte time probably?
		//
		//One problem is that _COVER or _NOCHANGE groups lose that information (it can be retrieved from
		//mother group name if that exists, but . . .
		//
		//Also sometimes they get a NONAME but there is no mother group.  So then the waypoints
		//are  reprocessed regardless of _COVER and _NOCHANGE.
		//
		//WE COULD fix that by keeping a database of AIRCRAFT name instead of just airgroup name.  IF the aircraft
		//has already been processed we could skip that maybe (though maybe it is OK to not do that for most
		//AC), but also we could track more precisely _COVER and _NOCHANGE a/c and not change them
		//if they turn into NONAMEs
		
         
        GetCurrentAiAirGroups();
        foreach (AiAirGroup airGroup in airGroups)
        {
            try
            {
                //printAttachedAirgroups(airGroup); //for testing
                checkToDespawnOldAirgroups(airGroup);
                if (airGroup == null) continue;
                if (AirgroupsWayPointProcessed.Contains(airGroup)) continue;

                AirgroupsWayPointProcessed.Add(airGroup);

                if (airGroup.GetItems().Length == 0 || !isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft)) continue;

                updateAirWaypoints(airGroup);
            }
            catch (Exception ex) { Console.WriteLine("MoveBomb checkNewAirgroups() ERROR: " + ex.ToString()); }
        }
    }

    public System.Threading.Timer checkAirgroupsInterceptTimer;

    public void checkAirgroupsIntercept_recurs()
    {
        Console.WriteLine("checkAirgroupsIntercept_recurs: Starting timer! " + DateTime.UtcNow.ToString("T"));
        checkAirgroupsInterceptTimer = new System.Threading.Timer(
            new TimerCallback(checkAirgroupsIntercept),
            null,
            dueTime: 30000, //wait time @ startup
            period: 187000); //periodically call the callback at this interval, every 4-6 minutes say
    }

    public System.Threading.Timer avoidAttackingAIEnemyTimer;

    public void avoidAttackingAIEnemy_recurs()
    {
        Console.WriteLine("balanceAILoad: Starting timer! " + DateTime.UtcNow.ToString("T"));
        avoidAttackingAIEnemyTimer = new System.Threading.Timer(
            new TimerCallback(checkAirgroupsAvoidAttackingAIEnemy),
            null,
            dueTime: 33000, //wait time @ startup
            period:  45300); //periodically call the callback at this interval, every 4-6 minutes say
    }
    /*

    public void checkAirgroupsIntercept_recur()
    {
        /************************************************
         * 
         * Change airgroups to intercept nearest interesting enemy
         * Recursive function called every X seconds
         ************************************************ /

        Timeout(187, () => { checkAirgroupsIntercept_recur(); });
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("MBTXX1 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping
        //Timeout()
        //Timeout(27, () => { checkAirgroupsIntercept_recur(); }); //for testing

        Task.Run(() => checkAirgroupsIntercept());
        //checkAirgroupsIntercept();
    }
    */

    public void checkAirgroupsIntercept(object o)
    {
        //Console.WriteLine("MoveBomb: Checking airgroups intercepts, groups: " + airGroups.Count.ToString());
        foreach (AiAirGroup airGroup in airGroups)
        {
            bool intcp = false;
            if (airGroup.GetItems().Length > 0 && isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft))
            {
                //Console.WriteLine("MoveBomb: Checking airgroups intercept for airgroup " + airGroup.Name());
                intcp = interceptNearestEnemyOnRadar(airGroup);
                //if (!intcp) avoidAttackingAIEnemy(airGroup);
				fixStuckPlanes(airGroup);
            } else
            {
                //Console.WriteLine("MoveBomb: Skipping airgroup" + airGroup.Name());
            }
            
        }
    }
    public void checkAirgroupsAvoidAttackingAIEnemy(object o)
    {
        //Console.WriteLine("MoveBomb: Checking airgroups intercepts, groups: " + airGroups.Count.ToString());
        foreach (AiAirGroup airGroup in airGroups)
        {
            if (airGroup.GetItems().Length > 0 && isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft))
            {
                //Console.WriteLine("MoveBomb: Checking airgroups to avoid attacking AI " + airGroup.Name());                
                avoidAttackingAIEnemy(airGroup);
            }
        }
    }

    //So each attacking a/g can only do one intercept until it is complete, plus some extra time
    //Also each target a/g can only have one attacking a/g going to intercept it, until that interception is complete + maybe some extra time
    public class incpt
    {

        // Instance Variables 
        public double timeToIntercept { get; set; }
        public double timeToWait { get; set; } //pause time after this intercept happens
        public AiAirGroup attackingAirGroup { get; set; }
        public AiAirGroup targetAirGroup { get; set; }
        public Point3d pos { get; set; }
        public bool positionintercept { get; set; }
        public double timeInterceptStarted { get; set; }
        MoveBombTargetMission mission;


        // Constructor Declaration of Class 
        public incpt (double timeToIntercept, double timeToWait, AiAirGroup attackingAirGroup, AiAirGroup targetAirGroup, Point3d pos, bool positionintercept, MoveBombTargetMission mission, double timeInterceptStarted = -1)
        {
            this.timeToIntercept = timeToIntercept;
            this.timeToWait = timeToWait;
            this.attackingAirGroup = attackingAirGroup;
            this.targetAirGroup = targetAirGroup;
            this.pos = pos;
            this.positionintercept = positionintercept;
            this.mission = mission;
            if (timeInterceptStarted == -1) this.timeInterceptStarted = mission.Time.current();
            else this.timeInterceptStarted = timeInterceptStarted;
        }

    }
    Dictionary<AiAirGroup, incpt> attackingAirgroupTimeToIntercept = new Dictionary<AiAirGroup, incpt>();
    Dictionary<AiAirGroup, incpt> targetAirgroupTimeToIntercept = new Dictionary<AiAirGroup, incpt>();

    public bool avoidAttackingAIEnemy(AiAirGroup airGroup)
    {
        try
        {
            //Console.WriteLine("MoveBomb: #1, Avoid attacking AI, starting " + airGroup.Name());                
			
            if (airGroup == null || !isAiControlledAirGroup(airGroup) || airGroup.GetItems().Length == 0)
            {
                //Console.WriteLine("MoveBomb:airGroup is null, has no aircraft, or not AI, exiting");
                return false;
            }

            AiActor agActor = airGroup.GetItems()[0];
            if (agActor == null) return false;
            AiAircraft agAircraft = agActor as AiAircraft;
            if (agAircraft == null) return false;
            AiAirGroupTask task = airGroup.getTask();
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
			
			string airGroupName = airGroup.Name();
			//sometimes airgroup doesn't have a name, if so it seems to have a "mother" that does.  We'll see. 2026/08
			if (airGroupName=="NONAME" && airGroup.motherGroup()!=null) airGroupName = airGroup.motherGroup().Name();
			
			
				
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{ 
				fixNullWayPoints(airGroup);
				return false;
			}
			
            int currWay = airGroup.GetCurrentWayPoint();
            int AiAirGroupArmy = airGroup.getArmy();
            AiWayPoint currWP = CurrentWaypoints[currWay];

            if (task == null || currWP == null) return false;

            //one way we ID cover airgroups and any others we want to leave unchanged
            if (airGroupName.ToLower().Contains("cover")  || airGroupName.ToLower().Contains("nochange")) return false;

            //Don't do this if it's a cover airgroup, or bombers, fighters covering bombers, etc
			//.ESCORT follows path of target closely (REGARDLESS of the escorting a/c's own path)
			//.COVER ranges more widely and follows on path as set, (presumably coming ot rescue of target if needed?)
			//.FOLLOW is more similar to .COVER and follows OWN PATH not TARGET PATH
            var aawpt_list = new List<AiAirWayPointType> {
                        AiAirWayPointType.GATTACK_TARG,
                            AiAirWayPointType.GATTACK_POINT,
                            AiAirWayPointType.COVER,
                            AiAirWayPointType.ESCORT,
                            AiAirWayPointType.FOLLOW};
        
            if (currWP == null || aawpt_list.Contains((currWP as AiAirWayPoint).Action)) return false;

            if ((MoveBombCalcs.isDiveBomber(airGroup) && airGroup.hasBombs()) || MoveBombCalcs.isHeavyBomber(airGroup)) return false;
            
            if (task != null && ( task == AiAirGroupTask.DEFENDING || task == AiAirGroupTask.LANDING || task == AiAirGroupTask.ATTACK_GROUND)) //Note that task LANDING is our clue that the a/g is at end of mission & just needs to be retired gracefully.  Shouldn't be attacking etc.  Probably low on fuel, ammo etc.
                return false;

            double distToNearestEnemyBreather_m = getDistanceToNearestLivePilot(airGroup, AiAirGroupArmy).Item1;
            if (distToNearestEnemyBreather_m <0) distToNearestEnemyBreather_m = 10000000; //returns -1 if nothing at all found
            //If there are breather enemy close & we are just "fly_waypoint" set them to attack.
						//Don't do this if it's a cover airgroup, or bombers, fighters covering bombers, etc            
			
            if ( ( task == AiAirGroupTask.FLY_WAYPOINT || task == AiAirGroupTask.DO_NOTHING || task == AiAirGroupTask.UNKNOWN ) && distToNearestEnemyBreather_m < 25000)
            {
                //airGroup.setTask(AiAirGroupTask.ATTACK_AIR, airGroup); //OK. aha! attack_air your or airGroup makes the planes attack  themselves !!! Rather than anyone else.  Thus all the reports of groups getting into dogfights with themselves.  Hopefully attack_air, null will do better!
                try
                {
                    AiAirGroup newAG = mainmission.statsmission.getRandomNearbyEnemyAirGroup(airGroup, 12000, 4000, 2000);

                    //So start attacking nearby stuff if a player is near, but ONLY if the thing we were going to attack
                    //is the player OR something very near the player
                    double newAG_dist_m = getDistanceToNearestLivePilot(newAG, 0).Item1;
                    if (newAG_dist_m < 0) newAG_dist_m = 1000000000; //-1 means, nothing found
                    if (newAG !=null && ( !isAiControlledAirGroup(newAG) ||  newAG_dist_m < 10000)) airGroup.setTask(AiAirGroupTask.ATTACK_AIR, newAG); // we think ATTACK_AIR, null causes problems as we need an airgroup, not null  Mayhbe DEFENDING is better but maybe not
                }
                catch (Exception ex) { Console.WriteLine("MoveBomb avoidAttackingAIEnemy ERROR2: " + ex.ToString()); return true; }

                //Console.WriteLine("MoveBomb: Setting airgroup to ATTACK_AIR, null because nearby enemy breather " + airGroupName);
                return true;

            }


            Tuple<double?, double?> dist_altdiff = getDistanceToNearestFriendlyBombergroup(airGroup); //item1 = distance(meters), item2=altdiff(meters) + if this group is higher than bombers
			

             if (task == AiAirGroupTask.DEFENDING  && dist_altdiff.Item1 != null && dist_altdiff.Item1 < 10000 && dist_altdiff.Item2 > -1050 && dist_altdiff.Item2 < 1750)
            {
                //Console.WriteLine("MoveBomb: Near bombers && task == .COVER or .ESCORT, should be escorting them--not chasing things {0} {1} ", agActor.Name(), agAircraft.InternalTypeName());
                return false;

            } 
			
            
            //If there aren't enemy breathers near set them to just fly_waypoint instead of attacking things
        try{
            if (distToNearestEnemyBreather_m > 90000) airGroup.setTask(AiAirGroupTask.FLY_WAYPOINT, null);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb avoidAttackingAIEnemy ERROR3: " + ex.ToString()); return true; }
        //Console.WriteLine("MoveBomb: 1 Setting airgroup to AVOID attacking AI " + airGroupName);                
        return true;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb avoidAttackingAIEnemy ERROR: " + ex.ToString()); return false; }
    }

    public bool interceptNearestEnemyOnRadar(AiAirGroup airGroup)
    {
        try
        {


            if (airGroup == null || !isAiControlledAirGroup(airGroup) || airGroup.GetItems().Length == 0)
            {
                //Console.WriteLine("MoveBomb:airGroup is null, has no aircraft, or not AI, exiting");
                return false;
            }
            
            AiActor agActor = airGroup.GetItems()[0];
			if (agActor == null) return false;
            AiAircraft agAircraft = agActor as AiAircraft;
			if (agAircraft == null) return false;
            AiAirGroupTask task = airGroup.getTask();
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
			
			string airGroupName = airGroup.Name();
			//sometimes airgroup doesn't have a name, if so it seems to have a "mother" that does.  We'll see. 2026/08
			if (airGroupName=="NONAME" && airGroup.motherGroup()!=null) airGroupName = airGroup.motherGroup().Name();
				
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{ 
				fixNullWayPoints(airGroup);
				return false;
			}
			
            int currWay = airGroup.GetCurrentWayPoint();
            int AiAirGroupArmy = airGroup.getArmy();
            AiWayPoint currWP = CurrentWaypoints[currWay];

            if (mainmission.ON_TESTSERVER) Console.WriteLine("MoveBomb: Should we check radar returns for airGroup? " + agActor.Name() + " " + agAircraft.InternalTypeName() + " airGroup Name: " + airGroupName + " task: " + task.ToString());
			
			//Don't do this if it's a cover airgroup, or bombers, fighters covering bombers, etc
            var aawpt_list = new List<AiAirWayPointType> {
                        AiAirWayPointType.GATTACK_TARG,
                            AiAirWayPointType.GATTACK_POINT,
                            AiAirWayPointType.COVER,
                            AiAirWayPointType.ESCORT,
                            AiAirWayPointType.FOLLOW};
        
            if (aawpt_list.Contains((currWP as AiAirWayPoint).Action)) return false; 

            //usually we don't do this with heavy bombers (unless a small group and already dropped bombs); or Stukas if they still have their bombs
            bool isBomber = ( MoveBombCalcs.isHeavyBomber(airGroup) && airGroup.hasBombs() && airGroup.GetItems().Length > 4 ) || ( MoveBombCalcs.isDiveBomber(airGroup) && airGroup.hasBombs() );
            if (isBomber) return false;
            
            //The "cover" or "nochange" is in the AIRGROUP name, not the actor/aircraft name
            if (airGroupName.ToLower().Contains("cover")  || airGroupName.ToLower().Contains("nochange")) return false;

            if (mainmission.ON_TESTSERVER) Console.WriteLine("MoveBomb: Checking radar returns for airGroup: " + agActor.Name() + " " + agAircraft.InternalTypeName());
            
            ConcurrentDictionary<AiAirGroup, SortedDictionary<string, IAiAirGroupRadarInfo>> aris;
            double interceptTime_sec = 0;



                //reportAircraftFuel(agAircraft);

                /*if (airGroup.GetWay() == null)
                {
                    Console.WriteLine("MoveBomb:airGroup.GetWay() is null for " + agActor.Name());
                }
                */

                
                if (mainmission != null && mainmission.ai_radar_info_store != null) aris = mainmission.ai_radar_info_store;
                else
                {
                    //Console.WriteLine("MoveBomb: No TWCMainMission connected, returning");
                    return false;
                }

                if (airGroup == null || !aris.ContainsKey(airGroup))
                {
                    //Console.WriteLine("MoveBomb: No radar returns exist for this group, returning: " + agActor.Name());
                    return false;
                }
                SortedDictionary<string, IAiAirGroupRadarInfo> ai_radar_info = new SortedDictionary<string, IAiAirGroupRadarInfo>(aris[airGroup]);

                double fuel = 100; // = getAircraftFuel(agAircraft);
                int ammo = 100; // getAircraftAmmo(airGroup);
                try
                {
                    fuel = getAircraftFuel(agAircraft);
                    ammo = getAircraftAmmo(airGroup);
                }
                catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR7A: " + ex.ToString()); return false; }

                if ((!airGroup.hasCourseWeapon() && !airGroup.hasCourseCannon()) || ammo < 40)
                {
                    //Console.WriteLine("MoveBomb: Skipping no weapons & no cannon {0} {1} ammo: {2} ", airGroup.hasCourseWeapon(), airGroup.hasCourseCannon(), ammo);
                    return false;
                }


            if (fuel < 10)
                {
                    //Console.WriteLine("MoveBomb: Skipping, low fuel: {0:N0} kg ", fuel);
                    return false;
                }
                //AiAirGroupTask task = airGroup.getTask();
                if (task == AiAirGroupTask.DEFENDING || task == AiAirGroupTask.LANDING) //Note that task LANDING is our clue that the a/g is at end of mission & just needs to be retired gracefully.  Shouldn't be attacking etc.  Probably low on fuel, ammo etc.
                {
                    //Console.WriteLine("MoveBomb: Busy because {2}, can't attack {0} {1} ", agActor.Name(), agAircraft.InternalTypeName(), task);
                    return false;
                }

                Tuple<double?, double?> dist_altdiff = getDistanceToNearestFriendlyBombergroup(airGroup); //item1 = distance(meters), item2=altdiff(meters) + if this group is higher than bombers
                if (dist_altdiff.Item1 != null && dist_altdiff.Item1<9000 && dist_altdiff.Item2 > -1050 && dist_altdiff.Item2 < 1750)
                {
                    //Console.WriteLine("MoveBomb: Near bombers, should be escorting them--not chasing things {0} {1} ", agActor.Name(), agAircraft.InternalTypeName());
                    return false;

                }

                
				if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
				{ 
					fixNullWayPoints(airGroup);
					return false;
				}
				
                if (CurrentWaypoints != null || CurrentWaypoints.Length > 0) {

                    int currWay2 = airGroup.GetCurrentWayPoint();
                    AiAirWayPointType aawp = new AiAirWayPointType();
                    if (currWay2 <= CurrentWaypoints.Length) aawp = (CurrentWaypoints[currWay2] as AiAirWayPoint).Action;
                    if (aawp != null && aawp == AiAirWayPointType.GATTACK_TARG ||
                            aawp == AiAirWayPointType.GATTACK_POINT ||
                            aawp == AiAirWayPointType.COVER ||
                            aawp == AiAirWayPointType.ESCORT ||
                            aawp == AiAirWayPointType.FOLLOW)
                    {
                        //Console.WriteLine("MoveBomb: Busy escorting or attacking, can't take time to attack another target {0} {1} {2} ", agActor.Name(), agAircraft.InternalTypeName(), aawp);
                        return false;
                    }
                }



            
            IAiAirGroupRadarInfo aagri = null;
            IAiAirGroupRadarInfo bestAagri = null;
            IAiAirGroupRadarInfo bestNoninterceptAagri = null;
            Point3d iPoint = new Point3d(0, 0, 0);

            if (ai_radar_info.Count == 0)
            {
                Console.WriteLine("MoveBomb:No radar returns exist for this airGroup, exiting: " + agActor.Name());
                return false;
            }

            bool goodintercept = false;
            bool positionintercept = false;
            foreach (string key in ai_radar_info.Keys)
            {
                Point3d tempAagriIntcpPt = new Point3d (0,0,100000); 

                if (ai_radar_info[key] != null)
                {
                    aagri = ai_radar_info[key];

                    if (aagri.pagi.type != "F") continue; //We only get radar returns for fighters, so bombers are auto-skipped in this whole system.  But, we might as well be double sure here.

                    tempAagriIntcpPt = new Point3d(aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z);

                    try
                    {
                        //Sometimes the intercept value is off the map for one reason or another
                        if (tempAagriIntcpPt.x > twcmap_maxX || tempAagriIntcpPt.y > twcmap_maxY || tempAagriIntcpPt.x < twcmap_minX || tempAagriIntcpPt.y < twcmap_minY)
                        {
                            if (tempAagriIntcpPt.z < 0) tempAagriIntcpPt.z = 0;
                            if (tempAagriIntcpPt.z > 1000000) tempAagriIntcpPt.z = 1000000;
                            if (tempAagriIntcpPt.x > twcmap_maxX) tempAagriIntcpPt.x = twcmap_maxX;
                            if (tempAagriIntcpPt.y > twcmap_maxY) tempAagriIntcpPt.y = twcmap_maxY;
                            if (tempAagriIntcpPt.x < twcmap_minX) tempAagriIntcpPt.x = twcmap_minX;
                            if (tempAagriIntcpPt.y < twcmap_minY) tempAagriIntcpPt.y = twcmap_minY;
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR2A: " + ex.ToString()); return false; }


                    try
                    {
                        //So, because of the radar grouping system we should get ONLY grouped AirGroups & we can always plan on the one we get being the leader & we can/should use the AGG side of position, velocity, etc rather than the ag side.
                        if (aagri.agi.AGGAIorHuman == aiorhuman.AI || isAiControlledAirGroup(aagri.agi.airGroup))  //belt & suspenders
                        //if (false) //for testing, just chase all airgroups including AI
                        {
                            //Console.WriteLine("MoveBomb: Skipping because 100% AI airgroup {0}", aagri.agi.AGGAIorHuman);
                            continue; //we don't make AI attack other ai - that would be . . . futile plus waste CPU cycles
                        }
                        //If anything we should incorporate a scheme here to encourage AI to **avoid** attacking each other if possible
                        //  For example we could store the heading to AI groups within X km and Y altitude
                        //  in a table or something, and then pick a new heading that avoids all of them, in the case where
                        //  the AI aren't sent off to intercept some breather pilots.
                        //But for now we'll just avoid CAUSING THE AI GROUPS to attack each other.
                    }
                    catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR5: " + ex.ToString()); return false; }

                    if (attackingAirgroupTimeToIntercept.ContainsKey(airGroup) && attackingAirgroupTimeToIntercept[airGroup].timeToIntercept > Time.current() && attackingAirgroupTimeToIntercept[airGroup].targetAirGroup == aagri.agi.airGroup)  //meaning that this airgroup is already attacking, the attack is current, and the target of the attack is the same target airGroup we are looking at on radar right now 
                    {
                        //Most of the time we just accept an updated radar plot for an airgroup we are already chasing

                        try
                        {
                            //Console.WriteLine("MoveBomb: Looking to update the same target we were previously attacking: {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask(), aagri.agi.playerNames);

                            //so we ALWAYS accept an updated radar plot for the airgroup we are already chasing IF it is better than the other possibilities
                            if (bestAagri == null)
                            {
                                iPoint = tempAagriIntcpPt;
                                bestAagri = aagri;
                                goodintercept = true;
                                //Console.WriteLine("MoveBomb: Possibly updating {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask(), aagri.agi.playerNames);
                            }
                            else if (iPoint.z > tempAagriIntcpPt.z)
                            {
                                iPoint = tempAagriIntcpPt;
                                bestAagri = aagri;
                                goodintercept = true;
                                //Console.WriteLine("MoveBomb: Possibly updating {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask(), aagri.agi.playerNames);
                            }
                            //But sometimes we stick with our previous chase even if it is worse - especially if we're already quite close, then we always do
                            if (ran.NextDouble() > 0.9 || tempAagriIntcpPt.z < 3.5 * 60)
                            {
                                goodintercept = true;
                                iPoint = tempAagriIntcpPt;
                                bestAagri = aagri;
                                //Console.WriteLine("MoveBomb: Definitely updating {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask(), aagri.agi.playerNames);
                                break;
                            }
                            //So, the case where there is no good intercept, or it is very long intercept, but still we are quite close
                            //And we are already chasing, then this will become the bestNoninterceptAagri for sure
                            else if (ran.NextDouble()>0.85 && MoveBombCalcs.CalculatePointDistance(aagri.agi.pos,aagri.pagi.pos) < 35000 && ( tempAagriIntcpPt.z == 0 || tempAagriIntcpPt.z > 10*60))
                            {
                                bestNoninterceptAagri = aagri;
                            }
                            else continue;
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR6: " + ex.ToString()); return false; }

                    }
                    else if
                      (tempAagriIntcpPt.x == null || tempAagriIntcpPt.x == 0 || tempAagriIntcpPt.y == 0 || tempAagriIntcpPt.z > 10 * 60 || tempAagriIntcpPt.z <= 0 ||
                          (targetAirgroupTimeToIntercept.ContainsKey(aagri.agi.airGroup) && targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept > Time.current() && (tempAagriIntcpPt.z > targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept - 120 ||
                             targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept < Time.current() + 120
                            )
                           ) ||  // In case this target already has an a/g attacking it, skip - unless the old intercept time is still in the future more than 2 minutes out & new intercept time is better than the old one by a fair bit (120 seconds). In other words skip it, unless  the new one is quite a bit better than the old one, and the old one isn't almost ready to be intercepted regardless

                          (attackingAirgroupTimeToIntercept.ContainsKey(airGroup) && attackingAirgroupTimeToIntercept[airGroup].timeToIntercept > Time.current() &&
                             (
                                  attackingAirgroupTimeToIntercept[airGroup].timeToIntercept < Time.current() + 120 ||
                                  tempAagriIntcpPt.z > attackingAirgroupTimeToIntercept[airGroup].timeToIntercept - 120  //In case this a/g already has an existing intercept, unless this one is quite a bit better than the current one (ie, a quicker intercept), skip it
                             )
                          )
                       )
                    {
                        try
                        {
                            //Console.WriteLine("MoveBomb: Skipping {0} intercept {1} {2} {3} " + agActor.Name(),aagri.agi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z);

                            if (tempAagriIntcpPt.x == null || tempAagriIntcpPt.x == 0 || tempAagriIntcpPt.y == 0 || tempAagriIntcpPt.z > 10 * 60 || tempAagriIntcpPt.z <= 0) { Console.WriteLine("MoveBomb: Skipping {0} intercept because no intercept or too distant {1} {2} {3} " + agActor.Name(), aagri.agi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z); }
                            else
                            {

                                //Console.WriteLine("MoveBomb: Skipping for another reason . . . target: {0} attacker:" + agActor.Name(), aagri.agi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z);
                                if (attackingAirgroupTimeToIntercept.ContainsKey(airGroup) && attackingAirgroupTimeToIntercept[airGroup].timeToIntercept > Time.current()) Console.WriteLine("MoveBomb: Skipping because attacker {0} already has an existing intercept {1} " + attackingAirgroupTimeToIntercept[airGroup].timeToIntercept.ToString("N0"), aagri.pagi.playerNames, aagri.agi.playerNames);
                                if (targetAirgroupTimeToIntercept.ContainsKey(aagri.agi.airGroup) && targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept > Time.current()) Console.WriteLine("MoveBomb: Skipping because target {1} already has an existing interceptor {0} " + targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept.ToString("N0"), aagri.pagi.playerNames, aagri.agi.playerNames);
                            }

                            //this is the best non-intercept contact if either none already exists, or it is closest to the contact in question
                            if (bestNoninterceptAagri == null) bestNoninterceptAagri = aagri;
                            if (MoveBombCalcs.CalculatePointDistance(bestNoninterceptAagri.pagi.pos, bestNoninterceptAagri.agi.pos) > MoveBombCalcs.CalculatePointDistance(aagri.agi.pos, aagri.pagi.pos)) bestNoninterceptAagri = aagri;

                            continue; //skip this one if there is no intcpt point OR the intcpt time is longer than 5*60 seconds
                                      //TODO: also need to skip if AI group is target && if altitude difference is too great
                                      //Also check whether they are already on an intercept and whether the target group is already being intercepted by some other group
                                      //if ()

                            //Also, can check whether the group is already engaged with some target?
                            //TODO: Once an a/g has picked a target, it probably should update the intercept every time the radar updates, rather than just sticking with the first one they got.
                            //TODO: Rather than just going to the first a/g that has an intercept in the list, there probably should be some way to make the intcpt go to the group that has
                            //the best or closest intercept.  Maybe if someone else has a better/closer intercept that a/g can take over the intercept & release the first a/g that was
                            //intercepting
                            //TODO: Check airgroup TASK when re-assigning & don't reassign if LANDING, DEFENDING, maybe some other things like ATTACK_GROUND< ATTACK_AIR PURSUIT? All task types listed at maddox.game.world.AiAirGroupTask
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR4: " + ex.ToString()); return false; }
                    }
                    else
                    {
                        try
                        {
                            //Console.WriteLine("MoveBomb: Moving {0} to intercept {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask());

                            //Console.WriteLine("MoveBomb: Found an acceptable intercept! {0} to best intercept so far {1} {2:N0} {3:N0} {4:N0} {5} . Now, is it better?" + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.agi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask());

                            //If this is the first one we have found (iPoint.z==0) or better than our best interception point so far, then we accept it as the new intercept point
                            if (bestAagri == null || iPoint.z == 0)
                            {
                                iPoint = tempAagriIntcpPt;
                                bestAagri = aagri;
                                goodintercept = true;
                                //Console.WriteLine("MoveBomb: Moving {0} to best intercept so far {1} {2:N0} {3:N0} {4:N0} {5} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.agi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask());
                            }
                            else if (tempAagriIntcpPt.z > 0 && iPoint.z > tempAagriIntcpPt.z)
                            {
                                iPoint = tempAagriIntcpPt;
                                bestAagri = aagri;
                                goodintercept = true;
                                //Console.WriteLine("MoveBomb: Moving {0} to best intercept so far {1} {2:N0} {3:N0} {4:N0} {5} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.agi.playerNames, tempAagriIntcpPt.x, tempAagriIntcpPt.y, tempAagriIntcpPt.z, airGroup.getTask());
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR3: " + ex.ToString()); return false; }

                    }

                    //TODO: Check appropriate altitude, whether or not near enough, inctp time (intcp.z) short enough, whether we've recently chased another different airgrouop, etc etc etc
                }
            }



            try
            {
                if (!goodintercept && bestNoninterceptAagri != null)
                {
                    //if there is no 'good' intercept we'll still make them chase if they are within about 13 iles and reasonable altitude difference
                    //(less than 1700m to climb or 4000m to dive
                    double dis_m = MoveBombCalcs.CalculatePointDistance(bestNoninterceptAagri.agi.AGGpos, bestNoninterceptAagri.pagi.pos);
                    if (dis_m < 20000 && (Math.Abs(bestNoninterceptAagri.agi.AGGaveAlt_m - bestNoninterceptAagri.pagi.pos.z) < 1700 || bestNoninterceptAagri.pagi.pos.z > bestNoninterceptAagri.agi.AGGmaxAlt_m && bestNoninterceptAagri.pagi.pos.z - bestNoninterceptAagri.agi.AGGmaxAlt_m < 5000))
                    {
                        bestAagri = bestNoninterceptAagri;
                        iPoint = bestAagri.agi.AGGpos; //x,y is x/y pos, z is time to intercept in seconds  (only leaders here, should use AGG data)
                        iPoint.z = 20*60; //so, no intercept at all, which we represent by quite a long time, 20 minutes, and also this means we can replace this intercept at any time with a better one
                        positionintercept = true; //meaning it is an intercept of the target's current position, not a "real" intercept of its future position.
                        goodintercept = true;
                    }
                }



                if (!goodintercept)
                {
                    //Console.WriteLine("MoveBombINER: Returning - no good intercept found for airgroup: " + agActor.Name());
                    //So here is where would could implement avoiding nearby enemy AI groups etc

                    return false;
                }

                //OK, so now iPoint becomes our actual x,y,z point of intercept, which is our calculated intercept point, plus some potential
                //altitude over the target, with some randomness added to it in x,y,z
                interceptTime_sec = iPoint.z;
                iPoint.x += ran.NextDouble() * 3000 - 1500;
                iPoint.y += ran.NextDouble() * 3000 - 1500;
                iPoint.z = bestAagri.agi.AGGmaxAlt_m + 750 + ran.NextDouble() * 1000 - 500;
                if (iPoint.z > 6500) iPoint.z = bestAagri.agi.pos.z + ran.NextDouble() * 1000 - 750;
                if (iPoint.z > 8500) iPoint.z = 8500 + ran.NextDouble() * 2000 - 1500;
                if (iPoint.z < 100 ) iPoint.z = 100 + ran.NextDouble() * 150 - 20;


                //Console.WriteLine("MoveBombINER: Making new intercept for " + bestAagri.pagi.playerNames + " to attack " + bestAagri.agi.playerNames);

                //we have an actual good intercept not just a "best non intercept" vector, so we register
                //
                // if (bestAagri != null && bestNoninterceptAagri != bestAagri)
                
                if (targetAirgroupTimeToIntercept.ContainsKey(bestAagri.agi.airGroup))
                {
                    //Console.WriteLine("MoveBombINER: Adding new/improved attacker " + bestAagri.pagi.playerNames + " for " + bestAagri.agi.playerNames);
                    //Do something to get rid of the old/worse pursuer
                    //AiAirGroup airGroupToRemove = targetAirgroupTimeToIntercept[bestAagri.agi.airGroup].attackingAirGroup;
                    //TODO: Sometimes removeAttackingAG ends up duplicating the first waypoint (bec. we just updated the WPs previously in this loop & are now doing it again)
                    //fixWayPoints fixes the problem BUT it would be better to just address it right awayin removeAttackingAG
                    removeAttackingAirGroup(targetAirgroupTimeToIntercept[bestAagri.agi.airGroup], targetAirgroupTimeToIntercept[bestAagri.agi.airGroup].attackingAirGroup);
                    fixWayPoints(targetAirgroupTimeToIntercept[bestAagri.agi.airGroup].attackingAirGroup); //fix any problems that might have resulted from the new waypoint fixes.
                }
                //targetAirgroupTimeToIntercept.Add(bestAagri.agi.airGroup, Time.current() + bestAagri.interceptPoint.z + 125.0 + ran.NextDouble() * 240.0 - 120.0);  //target can't get another interceptor assigned until this time is up, the actual time to the intercept plus 2 mins +/- 2 mins
                //targetAirgroupTimeToIntercept[bestAagri.agi.airGroup] = new incpt(Time.current() + interceptTime_sec, 125.0 + ran.NextDouble() * 240.0 - 120.0, bestAagri.pagi.airGroup, bestAagri.agi.airGroup, iPoint, positionintercept, this); //pagi is the attacker ("player" airgroup), agi is the target
                targetAirgroupTimeToIntercept[bestAagri.agi.airGroup] = new incpt(Time.current() + interceptTime_sec, 125.0 + ran.NextDouble() * 240.0 - 120.0, airGroup, bestAagri.agi.airGroup, iPoint, positionintercept, this); //pagi is the attacker ("player" airgroup), agi is the target

                //however, if this is a "bestNoninterceptAagri" type intercept, we don't consider it an actual intercept (because they WON'T intercept) but rather a move to see if
                //the attacker can get in position to actually have an intercept.  So we don't register a targetAirgroupTimeToIntercept at all, which allows another attacker to take an intercept if there is one.

                //if (attackingAirgroupTimeToIntercept.ContainsKey(bestAagri.pagi.airGroup)) Console.WriteLine("MoveBombINER: Adding new/improved intercept for attacker " + bestAagri.pagi.playerNames + " to attack " + bestAagri.agi.playerNames);  //This is only an FYI to let us know that this airGroup had a previous target we were attacking & now we are updating it.

                //attackingAirgroupTimeToIntercept[bestAagri.pagi.airGroup] = Time.current() + bestAagri.interceptPoint.z + 125.0 + ran.NextDouble() * 240.0 - 120.0;  //attacker can't get another intercept unti lthis time is up, the actual time to the intercept plus 2 mins +/- 2 mins

                //we always replace this value as it is represents what our current airgroup is doing, and we have decided to attack this target.  Sometimes it replaces
                //a previous target sometimes is just a new target
                //attackingAirgroupTimeToIntercept[bestAagri.pagi.airGroup] = new incpt(Time.current() + interceptTime_sec, 125.0 + ran.NextDouble() * 240.0 - 120.0, bestAagri.pagi.airGroup, bestAagri.agi.airGroup, iPoint, positionintercept, this);
                attackingAirgroupTimeToIntercept[bestAagri.pagi.airGroup] = new incpt(Time.current() + interceptTime_sec, 125.0 + ran.NextDouble() * 240.0 - 120.0, airGroup, bestAagri.agi.airGroup, iPoint, positionintercept, this);

                //AiWayPoint[] CurrentWaypoints = airGroup.GetWay();                

                //for testing
                /*
                foreach (AiWayPoint wp in CurrentWaypoints)
                {
                    AiWayPoint nextWP = wp;
                    Console.WriteLine("Add intcpt -  Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }
                */


                int currWay3 = airGroup.GetCurrentWayPoint();
                double speedDiff = 0;
                double altDiff_m = 0;

                //Console.WriteLine("MBTITG: 2");
                if (currWay3 < CurrentWaypoints.Length) Console.WriteLine("IntcpCalc: {0}", new object[] { CurrentWaypoints[currWay3] });
                //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { CurrentWaypoints[currWay].Speed });
                //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { (CurrentWaypoints[currWay] as AiAirWayPoint).Action });

                List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
                int count = 0;
                //Console.WriteLine("MBTITG: 3");

                bool update = false;

                NewWaypoints.Add(CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay3] as AiAirWayPoint).Action)); //Always have to add current pos/speed as first point or things go w-r-o-n-g

                foreach (AiWayPoint wp in CurrentWaypoints)
                {
                    AiWayPoint nextWP = wp;
                    //Console.WriteLine( "Target: {0}", new object[] { wp });

                    if ((wp as AiAirWayPoint).Action == null) return false;


                    if (count == currWay3)
                    {
                        Point3d pos;
                        double speed;
                        /*
                        switch ((wp as AiAirWayPoint).Action)
                        {

                            case AiAirWayPointType.GATTACK_TARG:
                            case AiAirWayPointType.GATTACK_POINT:
                            case AiAirWayPointType.COVER:
                            case AiAirWayPointType.ESCORT:
                            case AiAirWayPointType.FOLLOW:
                                break; //THESE types do nothing, no reconfiguration of route for intercept
                            case AiAirWayPointType.HUNTING:
                            case AiAirWayPointType.NORMFLY:
                            case AiAirWayPointType.RECON:
                            case AiAirWayPointType.AATTACK_FIGHTERS:
                            case AiAirWayPointType.AATTACK_BOMBERS:
                            */
                                //Console.WriteLine( "Updating, current TASK: {0}", new object[] { airGroup.getTask() });
                                //Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
                                //Console.WriteLine("WP before{0}: {1:N0} {2:N0} {3:N0} {4:N0}", new object[] { count, wp.Speed, wp.P.x, wp.P.y, wp.P.z });
                                pos = wp.P;

                                speed = wp.Speed;

                                double zSave = pos.z;

                                //Go to intercept point given, generally higher than the intercepting a/c and also +/- a few km in x,y, and alt
                                pos = new Point3d(iPoint.x , iPoint.y, iPoint.z);

                                nextWP = new AiAirWayPoint(ref pos, speed);
                                if (bestAagri.agi.type == "F") (nextWP as AiAirWayPoint).Action = AiAirWayPointType.AATTACK_FIGHTERS;
                                else if (bestAagri.agi.type == "B") (nextWP as AiAirWayPoint).Action = AiAirWayPointType.AATTACK_BOMBERS;
                                else (nextWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;
                                //Console.WriteLine( "Target after: {0}", new object[] { nextWP });
                                //Console.WriteLine("Added{0}: {1:N0} {2:N0} {3:N0} {4:N0}", new object[] { count, nextWP.Speed, nextWP.P.x, nextWP.P.y, nextWP.P.z });
                                //Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                                update = true;

                        /*
                                break;


                        }
                        */
                    }
                    if (count >= currWay3)
                    {
                        NewWaypoints.Add(nextWP);

                        if (update)
                        {
                            //Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                            //Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                        }

                    }

                    //Console.WriteLine("MBTITG: 4");
                    count++;



                }

                //for testing
                /*
                foreach (AiWayPoint wp in NewWaypoints)
                {
                    AiWayPoint nextWP = wp;
                    Console.WriteLine( "Add intcpt - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }
                */


                //NewWaypoints.Add(CurrentPosWaypoint(airGroup));
                //NewWaypoints.AddRange(SetWaypointBetween(airGroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));
                //NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));


                if (update)
                {
                    //Console.WriteLine("MBTITG: Updating this course");
                    airGroup.SetWay(NewWaypoints.ToArray());
                    fixWayPoints(airGroup);
                    return true;
                }
                else
                { return false; }
            }
            catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR1: " + ex.ToString()); return false; }
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR: " + ex.ToString()); return false; }
    }

    //If we have found a better intercept we remove the old intercept waypoint from that ag's waypoints list & that airgroup just returns to its usual course
    //TODO: Sometimes removeAttackingAG often ends up duplicating the first waypoint (bec. we just updated the WPs previously in the loop from which it is called & are now doing it again)
    //fixWayPoints fixes the problem BUT it would be better to just address it right awayin removeAttackingAG, by tracking WPs added & making sure no two adjacent WPs duplicate each other's position.  This happens because we add the first WP as the a/c's current position, and so if we do it again within the same tick we get the same exactly position as the first waypoint again.  When this is put into place in the airgroup it stops the airgroup mid-air dead stop.  Not good.
    public void removeAttackingAirGroup(incpt intc, AiAirGroup airGroup)
    {
        try
        {
            //AiAirGroup airGroup = intc.attackingAirGroup;
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
            //if (CurrentWaypoints == null || CurrentWaypoints.Length == 0) return;
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{ 
				fixNullWayPoints(airGroup);
				return;
			}

            //for testing
            /*
            foreach (AiWayPoint wp in CurrentWaypoints)
            {
               
                Console.WriteLine("RemoveAttackingAG - Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }
            */


            int currWay = airGroup.GetCurrentWayPoint();
            //Console.WriteLine("RemoveAttackingAG - currWay: {0} {1:n0} {2:n0}", new object[] {currWay, intc.pos.x, intc.pos.y});

            if (currWay >= CurrentWaypoints.Length) return;

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
            int count = 0;

            bool update = false;

            NewWaypoints.Add(CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action)); //Always have to add current pos/speed as first point or things go w-r-o-n-g

            foreach (AiWayPoint wp in CurrentWaypoints)
            {
                AiWayPoint nextWP = wp;                
                
                if (count >= currWay)
                {
                    //If we find the intercept point we previously set, then we'll just omit it from the listing of the waypoints
                    if (Math.Abs(nextWP.P.x - intc.pos.x) < 100 && Math.Abs(nextWP.P.y - intc.pos.y) < 100 && Math.Abs(nextWP.P.z - intc.pos.z) < 100 &&
                          ((nextWP as AiAirWayPoint).Action == AiAirWayPointType.AATTACK_FIGHTERS || (nextWP as AiAirWayPoint).Action == AiAirWayPointType.AATTACK_BOMBERS))
                    {
                        update = true;

                        //Console.WriteLine("RemoveAttackingAG - skipping this WayPoint: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                        //skip adding                        
                    }
                    else
                    {
                        NewWaypoints.Add(nextWP); //do add
                    }
                }
                count++;

            }
            if (update)
            {
                //Console.WriteLine("MBTITG: Updating this course");
                airGroup.SetWay(NewWaypoints.ToArray());

                //for testing
                /*
				Console.WriteLine("MBT: RemoveAttackingAG for {0} : {1} : {2}",  + agActor.Name(), agAircraft.InternalTypeName(), airGroupName);
                foreach (AiWayPoint wp in NewWaypoints)
                {                    
                    Console.WriteLine("MBT: RemoveAttackingAG - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }
				*/
                

            }
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb RemoveIntercept: " + ex.ToString()); }
    }

    
    //So, various fixes to WayPoints, including removing any dupes, close dupes, any w-a-y off the map, and adding two points at the end of the route to take
    //the aircraft down low and off the map north (Red) or south (Blue)
    //TODO: This exactly duplicates a function in Class-CoverMission, so now that we can call methods of other mission classes we should consolidate the two,
    //and probably several other functions similar/identical in both classes.
    public void fixWayPoints(AiAirGroup airGroup)
    {

        try
        {

            if (airGroup == null || airGroup.GetWay() == null || airGroup.GetCurrentWayPoint() == null) return; //Not sure what else to do?
			 
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay(); //So there is a problem if GetWay is null or doesn't return anything. Not sure what to do in that case!
            //Maybe just exit?
			
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{ 
				fixNullWayPoints(airGroup);
				return;
			}

            double offMapBufferForLeavingMap = 25000; //How far off the map to drive a/c to make them disappear/get rid of them.
            double offMapBufferForAvoidingLeaveMap = -1000; //Max off map amount to allow as part of a normal flight plan


            //if (CurrentWaypoints == null || CurrentWaypoints.Length == 0) return;
            if (!isAiControlledAirGroup(airGroup)) return;            
            AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;
			
			int currWay = airGroup.GetCurrentWayPoint();

            //for testing
            
            
            Console.WriteLine("MBT: FixWayPoints for {0} : {1} : {2} Currway: {3}",  aircraft.Name(), aircraft.InternalTypeName(), airGroup.Name(), currWay);
            foreach (AiWayPoint wp in CurrentWaypoints)
            {

                Console.WriteLine("MBT: FixWayPoints - Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }
            
            
            
            


            
            

            //if (currWay >= CurrentWaypoints.Length) return;

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
            int count = 0;

            bool update = false;

            AiWayPoint prevWP = CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);

            NewWaypoints.Add(prevWP); //Always have to add current pos/speed as first point or things go w-r-o-n-g

            AiAirWayPoint nextWP = makeNewAiAirWaypointFromOld(prevWP as AiAirWayPoint);

            bool landing = false; //keep track of whether or not the last waypoint is "landing".
			bool firstLanding = true; //we end up with several "landing" WPs at the end, keep track of which is first so we can add another "fly waypoint" close to it, to avoid long periods of RTB/docile behavior
			bool secondLanding = false;
			
			//int count = 0;

            foreach (AiWayPoint wp in CurrentWaypoints)
            {
                try
                {
                    nextWP = makeNewAiAirWaypointFromOld(wp as AiAirWayPoint); //NOTE: DOESN'T WORK!!!! just new name for same object
					if (count < currWay) {
						count ++;
						continue;
					}

                    //eliminate any exact duplicate points
                    if (Math.Abs(nextWP.P.x - prevWP.P.x) < 1 && Math.Abs(nextWP.P.y - prevWP.P.y) < 1 && Math.Abs(nextWP.P.z - prevWP.P.z) < 1
                        && (nextWP as AiAirWayPoint).Action == (prevWP as AiAirWayPoint).Action)
                    {
                        //if the Task is different for the 2nd point, it will only be operative for 50 meters . So skipping it?
                        update = true;
                        //Console.WriteLine("FixWayPoints - eliminating identical WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
						count ++;
                        continue;
                    }
                    //eliminate any  close duplicates, except in the hopefully rare case the 2nd .Action is some kind of ground attack                 
                    if (Math.Abs(nextWP.P.x - prevWP.P.x) < 50 && Math.Abs(nextWP.P.y - prevWP.P.y) < 50 && Math.Abs(nextWP.P.z - prevWP.P.z) < 50 &&
                        (nextWP as AiAirWayPoint).Action != AiAirWayPointType.GATTACK_TARG && (nextWP as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT)
                    {
                        //if the Task is different for the 2nd point, it will only be operative for 50 meters . So skipping it?
                        update = true;
                        //Console.WriteLine("FixWayPoints - eliminating close match WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
						count++;
                        continue;
                    }


                    try
                    {
                        //So, a waypoint could be way off the map which results in terrible aircraft malfunction (stopped dead in mid-air, etc?)
                        if (nextWP.P.x > twcmap_maxX + offMapBufferForAvoidingLeaveMap || nextWP.P.y > twcmap_maxY + offMapBufferForAvoidingLeaveMap || nextWP.P.x < twcmap_minX - offMapBufferForAvoidingLeaveMap || nextWP.P.y < twcmap_minY - offMapBufferForAvoidingLeaveMap || nextWP.P.z < 0 || nextWP.P.z > 50000)
                        {
                            //Console.WriteLine("FixWayPoints - WP WAY OFF MAP! Before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                            update = true;
                            if (nextWP.P.z < 0) nextWP.P.z = 0;
                            if (nextWP.P.z > 50000) nextWP.P.z = 50000;
                            //So we'll keep the aircraft from getting very near the border, if the point assigned was way off the map
                            if (nextWP.P.x > twcmap_maxX + offMapBufferForAvoidingLeaveMap) nextWP.P.x = twcmap_maxX - ran.Next(2000, 15000);
                            if (nextWP.P.y > twcmap_maxY + offMapBufferForAvoidingLeaveMap) nextWP.P.y = twcmap_maxY - ran.Next(2000, 15000);
                            if (nextWP.P.x < twcmap_minX - offMapBufferForAvoidingLeaveMap) nextWP.P.x = twcmap_minX + ran.Next(2000, 15000);
                            if (nextWP.P.y < twcmap_minY - offMapBufferForAvoidingLeaveMap) nextWP.P.y = twcmap_minY + ran.Next(2000, 15000);
                            //Console.WriteLine("FixWayPoints - WP WAY OFF MAP! After: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("MoveBomb FixWay ERROR2A: " + ex.ToString()); }
					
					try {

						if ((nextWP as AiAirWayPoint).Action == AiAirWayPointType.LANDING)
						{
							if (firstLanding || secondLanding) {  //add a new WP just near the landing point, and it will have task "fly waypoint" to avoid RTB type behavior until actually landing
							
							//Do this for the first LP AND the second LP.  So they have a chance to actually land in these places (does the currWay slip to the last of listed landing points as well, like ESCORT?)
							
							//After that it is all task=landing and heading off the map (though also past airports where it COULD land if it can, and also will be low & task=landing which gives them a chance to be removed by checkToDespawnOldAirgroups )
								
								//AiAirWayPoint newWP = nextWP as AiAirWayPoint; //doesn't work!!!! same obj, just 2 names now
								AiAirWayPoint newWP = makeNewAiAirWaypointFromOld(nextWP as AiAirWayPoint);
								newWP.P.x = nextWP.P.x + (ran.Next(0,1)*2-1)*ran.Next(5000,10000);
								newWP.P.y = nextWP.P.y + (ran.Next(0,1)*2-1)*ran.Next(5000,10000);
								(newWP as AiAirWayPoint).Action = AiAirWayPointType.NORMFLY;
								NewWaypoints.Add(newWP);
								update = true;
								//don't increment count here bec count is for waypoints of OLD waypoint list
								if (firstLanding) secondLanding = true;
								else secondLanding = false;
							}
							firstLanding = false;
							nextWP.P.z = 175; //if landing set the altitude very low.  Lowest ap is about 155m thought.
							nextWP.Speed = 50; //around 100mph speed for landing
							landing = true;
						}
						else landing = false;
					
					}
                    catch (Exception ex) { Console.WriteLine("MoveBomb FixWay ERROR2B: " + ex.ToString()); }


                    NewWaypoints.Add(nextWP); //do add
                    count++;
                }
                catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints #1: " + ex.ToString()); }

            }
            //So, if the last point is somewhere on the map, we'll just make them discreetly fly off the map at some nice alt
            if (nextWP.P.x > twcmap_minX && nextWP.P.x < twcmap_maxX && nextWP.P.y > twcmap_minY && nextWP.P.y < twcmap_maxY)
            {
                try
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


                    for (int i = 1; i < 13; i++)
                    {
                        try
                        {
                            if (ran.NextDouble() > 0.5)
                            {
                                if (army == 1) endPos.y = twcmap_maxY + offMapBufferForLeavingMap;
                                else if (army == 2) endPos.y = twcmap_minY - offMapBufferForLeavingMap;
                                else endPos.y = twcmap_maxY + offMapBufferForLeavingMap;
                                endPos.x = nextWP.P.x + ran.NextDouble() * 300000 - 150000;
                                if (endPos.x > twcmap_maxX + offMapBufferForLeavingMap) endPos.x = twcmap_maxX + offMapBufferForLeavingMap;
                                if (endPos.x < twcmap_minX - offMapBufferForLeavingMap) endPos.x = twcmap_minX - offMapBufferForLeavingMap;
                            }
                            else
                            {
                                if (army == 1) endPos.x = twcmap_minX - offMapBufferForLeavingMap;
                                else if (army == 2) endPos.x = twcmap_maxX + offMapBufferForLeavingMap;
                                else endPos.x = twcmap_maxX + offMapBufferForLeavingMap;
                                endPos.y = nextWP.P.y + ran.NextDouble() * 300000 - 150000;
                                if (army == 1) endPos.y += 80000;
                                else if (army == 2) endPos.y -= 10000;
                                if (endPos.y > twcmap_maxY + offMapBufferForLeavingMap) endPos.y = twcmap_maxY + offMapBufferForLeavingMap;
                                if (endPos.y < twcmap_minY - offMapBufferForLeavingMap) endPos.y = twcmap_minY - offMapBufferForLeavingMap;
                            }
                            //so, we want to try to find a somewhat short distance for the aircraft to exit the map.
                            //so if we hit a distance < 120km we call it good enough
                            //otherwise we take the shortest distance based on 10 random tries
                            distance_m = MoveBombCalcs.CalculatePointDistance(endPos, nextWP.P);

                            if (distance_m < 85000)
                            {
                                tempEndPos = endPos;
                                break;
                            }

                            if (distance_m < tempDistance_m)
                            {
                                tempDistance_m = distance_m;
                                tempEndPos = endPos;
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints #2: " + ex.ToString()); }
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
                    midPos.x = (nextWP.P.x * 1 + endPos.x * 1) / 2 + ran.NextDouble() * 70000 - 35000;
                    midPos.y = (nextWP.P.y * 1 + endPos.y * 1) / 2 + ran.NextDouble() * 70000 - 35000;

                    if (landing)
                    {
                        try
                        {
                            AiAirport ap = GetRandomAirfieldNear(midPos, 32000, 0, army, null, new Point3d(0,0,0));
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
                                landaaWP = new AiAirWayPoint(ref landPos, 55); // 50 mps ~= 100 mph, so reasonable pre-landing speed.                    
                                landaaWP.Action = landaawpt;
                                NewWaypoints.Add(landaaWP); //do add
                                count++;
                                update = true;
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints #3: " + ex.ToString()); }
                    }




                    /* (Vector3d Vwld = airGroup.Vwld();
                    double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?
                    if (vel_mps < 70) vel_mps = 70;
                    if (vel_mps > 160) vel_mps = 160;                
                    */

                    /*
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
                    */

                    //OK, skipping all that now & just making all ending/exitin waypoints LANDING so that hopefully many a/c can just disapparate.  2020/04/01
                    AiAirWayPointType aawpt = AiAirWayPointType.LANDING;

                    //add the mid Point
                    //midaaWP = new AiAirWayPoint(ref midPos, speed);
                    midaaWP = new AiAirWayPoint(ref midPos, 135); //135mps = 300mph.  Trying to get them to **move off the map** as quick as possible.
                    //aaWP.Action = AiAirWayPointType.NORMFLY;
                    midaaWP.Action = aawpt; //same action for mid & end

                    NewWaypoints.Add(midaaWP); //do add
                    count++;


                    //Console.WriteLine("FixWayPoints - adding new mid-end WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { aawpt, (midaaWP as AiAirWayPoint).Speed, midaaWP.P.x, midaaWP.P.y, midaaWP.P.z });

                    //add the final Point, which is off the map
                    //endaaWP = new AiAirWayPoint(ref endPos, speed);
                    endaaWP = new AiAirWayPoint(ref endPos, 135); //135mps = 300mph.  Trying to get them to **move off the map** as quick as possible.  Presumably if they find an airport & land they will slow down as needed.
                    //aaWP.Action = AiAirWayPointType.NORMFLY;
                    //endaaWP.Action = AiAirWayPointType.NORMFLY;
                    endaaWP.Action = AiAirWayPointType.LANDING; 

                    NewWaypoints.Add(endaaWP); //do add
                    count++;
                    //Console.WriteLine("FixWayPoints - adding new end WP: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { aawpt, (endaaWP as AiAirWayPoint).Speed, endaaWP.P.x, endaaWP.P.y, endaaWP.P.z });
                }
                catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints #4: " + ex.ToString()); }
            }
      

            if (update)
            {
                //Console.WriteLine("MBTITG: Updating this course");
                airGroup.SetWay(NewWaypoints.ToArray());

                //for testing

                
                try
                {

                    foreach (AiWayPoint wp in NewWaypoints)
                    {
                        Console.WriteLine("MBT: FixWayPoints - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });
                    }


                }
                catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints #5: " + ex.ToString()); }
                



            }
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb FixWayPoints: " + ex.ToString()); }
    }
	
	//***********************************************
	//TODO: Keep track of planes each time intcp is called, every 2-3 minutes
	//Check if plane is on its last waypoint, or doesn't have a waypoint, and if so
	//save the plane & current waypoint in a dictionary
	//If the plane stays stuck on that same waypoint for say 15-20 minutes, then
	//Give it a path to fly off the map
	
	public class finalWaypointRecord
    {

        // Instance Variables 
        public AiWayPoint lastAiWayPoint { get; set; }
        public int lastWayPoint_index  { get; set; }
        public Point3d lastPlanePos { get; set; }
        public int planeStuckCount { get; set; }        
        //MoveBombTargetMission mission;


        // Constructor Declaration of Class 
		public finalWaypointRecord (  
			AiWayPoint lastAiWayPoint,
			int lastWayPoint_index,
			Point3d lastPlanePos,
			int planeStuckCount
			//MoveBombTargetMission mission 
			)
        {
            this.lastAiWayPoint = lastAiWayPoint;
			this.lastWayPoint_index = lastWayPoint_index;
			this.lastPlanePos = lastPlanePos;
			this.planeStuckCount = planeStuckCount;
			//s.mission = mission;
        }

    }
	
	Dictionary<AiAirGroup, finalWaypointRecord> finalWayPointRecords = new Dictionary<AiAirGroup, finalWaypointRecord>();
	
	public void fixStuckPlanes(AiAirGroup airGroup)
	{
		 if (airGroup == null || !isAiControlledAirGroup(airGroup)) return;
		 
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
			AiWayPoint lastWayPoint = null;
			int currWay = 0;
			Point3d currPos = airGroup.Pos();
			if (CurrentWaypoints == null  || CurrentWaypoints.Length == 0)
			{
				lastWayPoint = null;
		    } else {
				
				currWay = airGroup.GetCurrentWayPoint();
				lastWayPoint = CurrentWaypoints[currWay];
				
			}
			
			if (finalWayPointRecords.ContainsKey(airGroup) || currWay == CurrentWaypoints.Length) {
				if (!finalWayPointRecords.ContainsKey(airGroup)) {
					finalWayPointRecords[airGroup] = new finalWaypointRecord (lastWayPoint, currWay, currPos, 0);
					return;
				}
				var fwpr = finalWayPointRecords[airGroup];
				if (fwpr.lastAiWayPoint.P.x != lastWayPoint.P.x ||
					fwpr.lastAiWayPoint.P.y != lastWayPoint.P.y ||
					fwpr.lastAiWayPoint.P.z != lastWayPoint.P.z ||
					fwpr.lastAiWayPoint.P.z != lastWayPoint.P.z ||
					fwpr.lastAiWayPoint.Speed != lastWayPoint.Speed ||
			
					fwpr.lastWayPoint_index != currWay || 
					fwpr.lastPlanePos.distance(ref currPos) > 40000) {
						finalWayPointRecords.Remove(airGroup);
						return;
				}
				fwpr.planeStuckCount ++;
				
				if (fwpr.planeStuckCount<7) return;
				
				Console.WriteLine("fixStuckPlanes: {0} appears to be stuck circling its last waypoint. Sending it off the map. Waypoint: {1:N0} {2:N0} {3:N0} CurrPos: {4:N0} {5:N0} {6:N0}", airGroup.Name(), lastWayPoint.P.x, lastWayPoint.P.y, lastWayPoint.P.z, currPos.x, currPos.y, currPos.z );
				
				fixNullWayPoints(airGroup);								
					
					
			}
			
	}
	
	
    public void fixNullWayPoints(AiAirGroup airGroup)
    {
        try
        {

            if (airGroup == null) return; //Not sure what else to do?
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay(); 
			

            double offMapBufferForLeavingMap = 25000; //How far off the map to drive a/c to make them disappear/get rid of them.
            double offMapBufferForAvoidingLeaveMap = -1000; //Max off map amount to allow as part of a normal flight plan


            //if (CurrentWaypoints == null || CurrentWaypoints.Length == 0) return;
            if (!isAiControlledAirGroup(airGroup)) return;
            if (airGroup.GetItems().Length == 0) return; //no a/c, no need to do anything
            AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;	
			
			Console.WriteLine("MBT: FixWayPoints for {0} : {1} : {2}",  aircraft.Name(), (aircraft as AiCart).InternalTypeName(), airGroup.Name());
            foreach (AiWayPoint wp in CurrentWaypoints)
            {
                //AiWayPoint nextWP = wp;
                Console.WriteLine("MBT: Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }
			
			
			List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
            int count = 0;

            bool update = false;

            AiWayPoint firstWP = CurrentPosWaypoint(airGroup, AiAirWayPointType.NORMFLY);

            NewWaypoints.Add(firstWP); //Always have to add current pos/speed as first point or things go w-r-o-n-g
			
			//Get it down on the ground, under radar
			NewWaypoints.Add( CurrentPosWaypoint(airGroup,offset_x: 10000, offset_y: 10000, alt_m: 25));

            AiWayPoint lastWP = offMapWaypoint(airGroup, AiAirWayPointType.NORMFLY, airGroup.Army());
			
			NewWaypoints.Add(lastWP);
			
			
			Console.WriteLine("fixNullWayPoints: {0} has a null or stuck waypoint.  Sending it off the map via CurrPos: {1:N0} {2:N0} {3:N0} OffMapPos: {4:N0} {5:N0} {6:N0}", airGroup.Name(), lastWP.P.x, lastWP.P.y, lastWP.P.z, firstWP.P.x, firstWP.P.y, firstWP.P.z );
				
			
			
			
			airGroup.SetWay(NewWaypoints.ToArray());
			

            foreach (AiWayPoint wp in NewWaypoints)
            {
                //AiWayPoint nextWP = wp;
                Console.WriteLine("MBT: Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }
			
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb FixNullWayPoints: " + ex.ToString()); }
    }			
	
	  /* { 
				fixNullWayPoints(airGroup);
				return false;
			}
			*/
    



    public override void OnTickGame()
    {
        base.OnTickGame();

        //if (Time.tickCounter() % 305 == 41) //about 1.5 seconds?
        if (Time.tickCounter() % 5105 == 41) //2105 about 5 seconds?  2020/03/31; 5105 - 2023-01
        {
            Task.Run(() => checkNewAirgroups());
            //checkNewAirgroups();
        }
    }


    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);

        /*
        if (shortName.Equals("trigger"))
        {
            GamePlay.gpGetTrigger(shortName).Enable = false;

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();

            NewWaypoints.Add(CurrentPosWaypoint(airGroup));
            NewWaypoints.AddRange(WaitingWayPoints(GetXYCoord(airGroup), 4000.0, 80.0, 10000.0, 5000.0, 20, AiAirWayPointType.HUNTING));

            NewWaypoints.AddRange(SetWaypointBetween(airGroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));

            NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));

            airGroup.SetWay(NewWaypoints.ToArray());
        }

        if (shortName.Equals("AttackTrigger"))
        {

            GamePlay.gpGetTrigger(shortName).Enable = false;

            AiAirGroup TestGroup = getNearestEnemyAirgroup(airGroup);

            if (TestGroup != null)
            {
                Console.WriteLine( "Nächste Airgroup: {0}", new object[] { TestGroup.Name() });
            }

            if (getDistanceToNearestEnemyAirgroup(airGroup).HasValue)
            {
                if (getDistanceToNearestEnemyAirgroup(airGroup).Value < 10000.0)
                {
                    Console.WriteLine( "Entfernung: {0}", new object[] { getDistanceToNearestEnemyAirgroup(airGroup).Value });
                    airGroup.setTask(AiAirGroupTask.ATTACK_AIR, TestGroup);
                }
            }
        }
        */
    }


    //So, I_FuelReserve never shows anything.  Maybe for launcher client only?
    //S_FuelReserve, -1 shows the TOTAL fuel available
    //S_FuelReserve, 0-x shows the amount in each of 2-3-4, however many, tanks in the aircraft.  The total of all the 0-x tanks always equals the -1 total
    //S_GunReserver, -1 doesn't seem to show anything at all and to get the total Gunreserve you have to total the S_GunServe, 0-x to get ammo in each area
    //Z_VelocityTAS gives the same for -1 and any x

    public double getAircraftFuel(AiAircraft aircraft)
    {
        double sFuel = -1;
        try
        {
            sFuel = aircraft.getParameter(part.ParameterTypes.S_FuelReserve, -1); // kgs
        }
        catch (Exception ex) { }
        return sFuel;
    }

    public int getAircraftAmmo(AiAircraft aircraft)
    {
        int ammo = -1;
        for (int i = 0; i < 9; i++)
        {
            try
            {
                ammo += (int)(aircraft.getParameter(part.ParameterTypes.S_GunReserve, i)); // qty
            }
            catch (Exception ex) { }
        };
        return ammo;
    }

    //returns the MAX of ammo in any aircraft in the airgroup
    //idea being, if any plane has enough ammo to attack, then the AG should be able to attack
    //they shouldn't turn off the attack just because (say) one or two a/c are low on ammo - only if the ALL are
    public int getAircraftAmmo(AiAirGroup airGroup)
    {
        int ammo = -1;
        if (airGroup.GetItems().Length == 0) return -1;

        foreach (AiAircraft a in airGroup.GetItems())
        {
            int ammo_temp = getAircraftAmmo(a);
            if (ammo_temp > ammo) ammo = ammo_temp;
        }
        
        return ammo;
    }




    //  AiAirGroup airGroup = aircraft.AirGroup();
    // if(aircraft == airGroup.GetItems()[0])		        
    public double reportAircraftFuel(AiAircraft aircraft)
    {
        double speed=0;
        double sFuel = 0;
        double iFuel = 0;
        try
        {

            //So, I_FuelReserve never shows anything.  Maybe for launcher client only?
            //S_FuelReserve, -1 shows the TOTAL fuel available
            //S_FuelReserve, 0-x shows the amount in each of 2-3-4, however many, tanks in the aircraft.  The total of all the 0-x tanks always equals the -1 total
            //S_GunReserver, -1 doesn't seem to show anything at all and to get the total Gunreserve you have to total the S_GunServe, 0-x to get ammo in each area
            //Z_VelocityTAS gives the same for -1 and any x

            try
            {

                speed = aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
            }
            catch (Exception ex) {}
            try
            {
                sFuel = aircraft.getParameter(part.ParameterTypes.S_FuelReserve, -1); // kgs
            }

            catch (Exception ex) { }


            //So, this one doesn't seem to work at all?
            try
            {
                iFuel = aircraft.getParameter(part.ParameterTypes.I_FuelReserve, -1); // kgs
            }

            catch (Exception ex) { }

            int ammo = 0;
            try
            {
                ammo += (int)(aircraft.getParameter(part.ParameterTypes.S_GunReserve, -1)); // qty
            }
            catch (Exception ex) { }

            //Console.WriteLine("MoveBomb: Aircraft levels speed {0:N0} ammo {1:N0} sFuel {2:N0} iFuel {3:N0} {4}", speed, ammo, sFuel, iFuel, aircraft.InternalTypeName());
            for (int i = 0; i < 9; i++)
            {
                try
                {
                    ammo += (int)(aircraft.getParameter(part.ParameterTypes.S_GunReserve, i)); // qty
                }
                catch (Exception ex) { }
                try
                {
                    sFuel += (int)(aircraft.getParameter(part.ParameterTypes.S_FuelReserve, i)); // qty
                }
                catch (Exception ex) { }
                try
                {
                    iFuel += (int)(aircraft.getParameter(part.ParameterTypes.I_FuelReserve, i)); // qty		
                }
                catch (Exception ex) { }
                //Console.WriteLine("MoveBomb: Aircraft levels {5} speed {0:N0} ammo {1:N0} sFuel {2:N0} iFuel {3:N0} {4}", speed, ammo, sFuel, iFuel, aircraft.InternalTypeName(), i);

            };

            /* //not sure of the reasoning behind this bit?
            if (ammo == 0 || sFuel < 20)

                sFuel += 40;
            iFuel += 40;
            ammo += 1;
            */
            //Console.WriteLine("MoveBomb: Aircraft levels speed {0:N0} ammo {1:N0} sFuel {2:N0} iFuel {3:N0} {4}", speed, ammo, sFuel, iFuel, aircraft.InternalTypeName());
            return iFuel;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb Fuelreport ERROR: " + ex.ToString()); return -1; }

    }
    
    //Message code: Kodiak
    //http://forum.1cpublishing.eu/archive/index.php/t-26623.html
    private void sendScreenMessageTo(int army, string msg, object[] parms)
	{
        try {
    	if (army != -1)
    	{
    	//Singleplayer (for Testing)
    	if (GamePlay.gpRemotePlayers() == null || GamePlay.gpRemotePlayers().Length <= 0)
    	{
    	if (GamePlay.gpPlayer() != null && GamePlay.gpPlayer().Army() == army)
    	GamePlay.gpHUDLogCenter(null, msg, parms);
    
    	}
    	else // Multiplayer
    	{
    	List<Player> Players = new List<Player>();
    
    	foreach (Player p in GamePlay.gpRemotePlayers())
    	{
    	if (p.Army() == army)
    	Players.Add(p);
    	}
    	GamePlay.gpHUDLogCenter(Players.ToArray(), msg, parms);
    	}
    	}
    	else GamePlay.gpHUDLogCenter(null, msg, parms);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb sendScreenMessage: " + ex.ToString()); return; }
	}

    private void sendChatMessageTo(int army, string msg, object[] parms)
    {
        try
        {
            if (army != -1)
            {
                //Singleplayer (for Testing)
                if (GamePlay.gpRemotePlayers() == null || GamePlay.gpRemotePlayers().Length <= 0)
                {
                    if (GamePlay.gpPlayer() != null && GamePlay.gpPlayer().Army() == army)
                        GamePlay.gpLogServer(null, msg, parms);

                }
                else // Multiplayer
                {
                    List<Player> Players = new List<Player>();

                    foreach (Player p in GamePlay.gpRemotePlayers())
                    {
                        if (p.Army() == army)
                            Players.Add(p);
                    }

                    if (Players.Count > 0) GamePlay.gpLogServer(Players.ToArray(), msg, parms);
                }
            }
            else GamePlay.gpLogServer(null, msg, parms);
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb sendChatMessage: " + ex.ToString()); return; }
    }
    
    private void sendScreenAndChatMessageTo(int army, string msg, object[] parms)
    {
      try{
      if (army != -1)
      {
      //Singleplayer (for Testing)
      if (GamePlay.gpRemotePlayers() == null || GamePlay.gpRemotePlayers().Length <= 0)
      {
      if (GamePlay.gpPlayer() != null && GamePlay.gpPlayer().Army() == army)
      GamePlay.gpLogServer(null, msg, parms);
      GamePlay.gpHUDLogCenter(null, msg, parms);
      
      }
      else // Multiplayer
      {
      List<Player> Players = new List<Player>();
      
      foreach (Player p in GamePlay.gpRemotePlayers())
      {
      if (p.Army() == army)
      Players.Add(p);
      }
      GamePlay.gpLogServer(Players.ToArray(), msg, parms);
      GamePlay.gpHUDLogCenter(Players.ToArray(), msg, parms);
      }
      }
      else {
       GamePlay.gpLogServer(null, msg, parms);
       GamePlay.gpHUDLogCenter(null, msg, parms);
       }
       
       }
        catch (Exception ex) { Console.WriteLine("MoveBomb sendScreenAndChatMessage: " + ex.ToString()); return; }
    }    
}

//Various helpful calculations, formulas, etc.
public static class MoveBombCalcs
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

    public static int GetDegreesIn10Step(double degrees)
    {
        degrees = Math.Round((degrees / 10), MidpointRounding.AwayFromZero) * 10;

        if ((int)degrees == 360)
            degrees = 0.0;

        return (int)degrees;
    }

    public static int Meters2Angels(double altitude)
    {
        double altAngels = (altitude / 0.3048) / 1000;

        if (altAngels > 1)
            altAngels = Math.Round(altAngels, MidpointRounding.AwayFromZero);
        else
            altAngels = 1;

        return (int)altAngels;
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
        Point3d p1 = new Point3d(p.x - clc_random.Next((maxSectorWidth - 1) * 10000), p.y - clc_random.Next((maxSectorWidth - 1) * 10000), p.z);
        if (p1.x < 10000) p1.x = 10000;
        if (p1.y < 10000) p1.y = 10000;
        if (p1.x > 359000) p1.x = 359000; //so, sometimes we get sector BJ? ???  if the max is 360000.  So cutting it down to 359K just for safety
        if (p1.y > 309000) p1.y = 309000; //same, 310K the limit, cut to 309 for safety.
        Point3d p2 = new Point3d(p.x + clc_random.Next((maxSectorWidth - 1) * 10000), p.y + clc_random.Next((maxSectorWidth - 1) * 10000), p.z);

        //BattleArea 10000 10000 360000 310000 10000 is TWC standard
        if (p1.x < 10000) p1.x = 10000;
        if (p1.y < 10000) p1.y = 10000;
        if (p2.x > 359000) p2.x = 359000; //see about, not 360K or 310K
        if (p2.y > 309000) p2.y = 309000;

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

    public static string GetAircraftType(AiAirGroup airGroup)
    { // returns the type of the aircraft in this airGroup
        string result = "";
        if (airGroup != null && airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
        {
            AiAircraft aircraft = airGroup.GetItems()[0] as AiAircraft;
            result = GetAircraftType(aircraft);
        }
        return result;
    }

    //Salmo @ http://theairtacticalassaultgroup.com/forum/archive/index.php/t-4785.html
    public static string GetAircraftType(AiAircraft aircraft)
    { // returns the type of the specified aircraft
        string result = "";
        if (aircraft != null)
        {
            string type = aircraft.InternalTypeName(); // eg type = "bob:Aircraft.Bf-109E-3".  FYI this is a property of AiCart inherited by AiAircraft as a descendant class.  So we could do this with any type of AiActor or AiCart
            string[] part = type.Trim().Split('.');
            result = part[1]; // get the part after the "." in the type string
        }
        return result;
    }

    public static string randSTR(string[] strings)
    {
        //Random clc_random = new Random();
        return strings[clc_random.Next(strings.Length)];
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
    public static int gpNumberOfPlayersActive(this IGamePlay GamePlay, int army)
    {   // Purpose: Returns the number of human players in the game in the 
        //          specified army, who are in planes and in the air.
        // Use: GamePlay.NumberOfPlayersActive(GamePlay, army); 
        int result = 0;
        if (GamePlay.gpRemotePlayers() != null || GamePlay.gpRemotePlayers().Length > 0)
        {
            List<Player> players = new List<Player>(GamePlay.gpRemotePlayers());
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Army() == army)
                {
                    if (players[i].Place() == null) continue;
                    if (players[i].Place() as AiAircraft == null) continue;
                    AiAircraft aircraft = players[i].Place() as AiAircraft;
                    double altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                    if (altAGL_m > 5) result += 1;  //only count players in plane & off the ground/in flight
                }
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

    public static bool isHeavyBomber(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = GetAircraftType(aircraft);
        return isHeavyBomber(acType);
    }

    public static bool isHeavyBomber(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isHeavyBomber(aircraft);

    }
    //Walrus here is debateable . . .
    public static bool isHeavyBomber(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("BlenheimMkI") || acType.Contains("Do-17") || acType.Contains("Wellington")
         || acType.Contains("Sunderland") || acType.Contains("Walrus") || acType.Contains("HurricaneMkI_FB")) ret = true; //Contains("BlenheimMkI" includes BI, BIV, BIV Late, etc.
        if (acType.Contains("BlenheimMkIVF") || acType.Contains("BlenheimMkIVNF") || acType.Contains("BlenheimMkIF") || acType.Contains("BlenheimMkINF")) ret = false;
        return ret;
    }
    public static bool isDiveBomber(AiAircraft aircraft)
    {
        if (aircraft == null) return false;
        string acType = GetAircraftType(aircraft);
        return isDiveBomber(acType);
    }
    public static bool isDiveBomber(AiAirGroup airGroup)
    {
        AiAircraft aircraft = null;
        if (airGroup.GetItems().Length > 0 && (airGroup.GetItems()[0] as AiAircraft) != null) aircraft = airGroup.GetItems()[0] as AiAircraft;
        return isDiveBomber(aircraft);

    }
    public static bool isDiveBomber(string acType)
    {
        if (acType == "") return false;
        bool ret = false;
        if (acType.Contains("Ju-87")) ret = true; //only JU-87 now, but maybe more later?   HurriFB definitely won't dive-bomb
        return ret;
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

        mission.Timeout(5.0, () => {
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
	
	



}