//$reference System.Core.dll
//$reference parts/core/CloDMissionCommunicator.dll
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using System.Text;
using TWCComms;

public struct changeLimit
{
    public double XY_m;
    public double alt_m;
    public double alt_percent;
    public double speed_percent;
    public double airport_m;  //Not used yet?
    public changeLimit(double xy =0 , double alt = 0, double altp = 25, double spdp = 10, double ap = 0)
    {
        XY_m = xy;
        alt_m = alt;
        alt_percent = altp;
        speed_percent = spdp;

        airport_m = ap;
    }   
}

public class Mission : AMission
{
    Dictionary<AiAirWayPointType, double> changeXY_m;
    Dictionary<AiAirWayPointType, double> changeAlt_m;

    Dictionary<AiAirWayPointType, changeLimit> changeLimits;

    bool moveAirports; //whether or not to move targets to a different/nearby airport
    double moveAirportsDistance_m; //max distance to move airports if you choose that option

    AiAirGroup airGroup;
    AiAirport AirGroupAirfield;
    bool toHome = false;

    public IMainMission TWCMainMission;
    public Random ran;

    public Mission()
    {
        TWCMainMission = TWCComms.Communicator.Instance.Main;

        moveAirports = true; //whether or not to move targets to a different/nearby airport
        moveAirportsDistance_m = 40000; //max distance to move airports if you choose that option

        //When adjusting various types of airgroup tasks, how far (at maximum) to move the position of that waypoint in xy and in alt (meters)
        //So for altitude, there is a number in meters and a percent.  It will use whichever is LARGER.  So if your formation is at 
        //say 5000 meters & the alt changeLs are 700, 30 then it will change a max of 1500 meters (30% of 5000)
        //if it's at 500 meters it may go up or down 700 m. (larger than 30% of 500).
        //However, if it is going down 700 m then the % kicks in again & prevents it from going down more than 30%.  So as to avoiding going underground etc.
        //Reason it is done this way is percentages work better for larger altitudes but absolute distances work better for low altitudes.  500m +/- 30% isn't much of a change at 
        //all while 5000m +/- 30% is quite a large change.  By contrast 5000m +/- 1000m isn't much of a change while 500m +/- 1000m is a very large change.
        changeLimits = new Dictionary<AiAirWayPointType, changeLimit>()
        {
            { AiAirWayPointType.NORMFLY, new changeLimit (7000, 700, 30, 10) },
            { AiAirWayPointType.HUNTING, new changeLimit (7000, 700, 30, 10) },
            { AiAirWayPointType.RECON, new changeLimit (4000, 1000, 50, 10) },
            { AiAirWayPointType.GATTACK_POINT, new changeLimit (450, 0, 0, 10, 15000) },
            { AiAirWayPointType.GATTACK_TARG, new changeLimit (450, 0, 0, 10, 15000) },
            { AiAirWayPointType.AATTACK_FIGHTERS, new changeLimit (5500, 800, 25, 10) },
            { AiAirWayPointType.AATTACK_BOMBERS, new changeLimit (5500, 800, 25, 10) },
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

    private bool isAiControlledAirGroup(AiAirGroup airGroup) {
        return isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft);
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

    public AiWayPoint CurrentPosWaypoint(AiAirGroup airGroup, AiAirWayPointType aawpt = AiAirWayPointType.NORMFLY)
    {
        try
        {
            AiAirWayPoint aaWP = null;
            //double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

            Vector3d Vwld = airGroup.Vwld();            
            double vel_mps = Calcs.CalculatePointDistance(Vwld); //Not 100% sure mps is the right unit here?

            Point3d CurrentPos = airGroup.Pos();

            aaWP = new AiAirWayPoint(ref CurrentPos, vel_mps);
            //aaWP.Action = AiAirWayPointType.NORMFLY;
            aaWP.Action = aawpt;

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

    public AiAirport GetRandomAirfieldNear(Point3d location, double distance)
    {
        List<AiAirport> CloseAirfields = new List<AiAirport>();
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

    public void GetCurrentAiAirgroups()
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
        catch (Exception ex) { Console.WriteLine("MoveBomb GetCurrentAG ERROR: " + ex.ToString());  }
    }


    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        if (missionNumber != MissionNumber) return; //only do this when this particular mission is loaded.
        
        Timeout(21, () => { checkAirgroupsIntercept_recur(); });
        Console.WriteLine("-MoveBombTarget.cs successfully loaded");
        //GetCurrentAiAirgroups();

        //airGroup = GamePlay.gpActorByName("0:BoB_LW_StG2_II.07") as AiAirGroup;

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

    //Distance (meters), altitude difference (meters)
    public Tuple<double?,double?> getDistanceToNearestFriendlyBombergroup(AiAirGroup from)
    {
        try
        {
            AiAirGroup airGroup = getNearestFriendlyBombergroup(from);
            if (airGroup == null) return new Tuple<double?, double?>(null, null);
            double dist = Calcs.CalculatePointDistance(from.Pos(), airGroup.Pos());
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
                    string acType = Calcs.GetAircraftType(a);
                    bool isHeavyBomber = false;
                    if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType == ("BlenheimMkIV")) isHeavyBomber = true;
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

    //Returns a point within the changed airport within the given radius OR
    //Null if the attack point is not within/near an airport OR no suitable airport found
    public Point3d? ChangeAirports(Point3d p)
    {
        Point3d retPos;

        AiAirport nearestAirfield = GetAirfieldAt(p);

        //check whether the attack point is within or very near an airfield
        //if (nearestAirfield.Pos().distance(ref p) > nearestAirfield.FieldR() * 1.25)
        if (Calcs.CalculatePointDistance(nearestAirfield.Pos(), p) > 2000) //The GATTACK_POINT distance is often quite far from the target itself
        {
            //Console.WriteLine("MBT: Attack point NOT within an airfield {0:n0} {1:n0} {2:n0} {3:n0} {4:n0}", nearestAirfield.Pos().x, nearestAirfield.Pos().y, p.x, p.y, Calcs.CalculatePointDistance(nearestAirfield.Pos(), p));
            return null;
        }




        //Get the random airport within the given radius
        AiAirport ap = GetRandomAirfieldNear(p, moveAirportsDistance_m);

        //Console.WriteLine("MBT: Attack point IS within an airfield {0:n0} {1:n0} {2:n0} {3:n0} {4:n0} {5} to {6}", nearestAirfield.Pos().x, nearestAirfield.Pos().y, p.x, p.y, Calcs.CalculatePointDistance(nearestAirfield.Pos(), p), nearestAirfield.Name(), ap.Name());

        if (ap != null)
        {
            /*
               //Choose a random point within the airfield radius
               double radius = ap.FieldR();
               Point3d center = ap.Pos();
               double dist = ran.NextDouble() * radius;
               double angl = ran.NextDouble() * 2 * Math.PI;

               retPos.x = Math.Cos(angl) * dist + center.x;
               retPos.y = Math.Sin(angl) * dist + center.y;
               retPos.z = 0;
               */
            //return the SAME relative position to this new airfield as we had with the old airfield
            //This is important because the attack point is often quite distant from the airfield itself, in order to actually hit the airfield accurately
            //retPos.x = p.x - nearestAirfield.Pos().x + ap.Pos().x;
            //retPos.y = p.y - nearestAirfield.Pos().y + ap.Pos().y;
            //Ok, we're going to make the airport attacks more effective by just centering them more on the new airport (plus/minus the radius defined above, of course)
            retPos.x = ap.Pos().x;
            retPos.y = ap.Pos().y;
            retPos.z = 0;
            Console.WriteLine("MBT: New attack point: {0:n0} {1:n0} {2:n0} {3:n0} {4:n0} {5} to {6}", ap.Pos().x, ap.Pos().y, retPos.x, retPos.y, Calcs.CalculatePointDistance(ap.Pos(), retPos), nearestAirfield.Name(), ap.Name());
            return retPos;
        }
        else return null;

    }

    public bool updateAirWaypoints(AiAirGroup airGroup)
    {
        if (airGroup == null || airGroup.GetWay() == null || !isAiControlledAirGroup(airGroup)) return false;
        if (ran.Next(10) == 1) return false; //Just leave it as originally written sometimes

        AiWayPoint[] CurrentWaypoints = airGroup.GetWay();

        /* //for testing
        foreach (AiWayPoint wp in CurrentWaypoints)
        {
            AiWayPoint nextWP = wp;
            //Console.WriteLine("Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

        }
        */
        int currWay = airGroup.GetCurrentWayPoint();
        double speedDiff = 0;
        double altDiff_m = 0;

        //Console.WriteLine("MBTITG: 2");
        //if (currWay< CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { CurrentWaypoints[currWay] });
        //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { CurrentWaypoints[currWay].Speed });
        //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { (CurrentWaypoints[currWay] as AiAirWayPoint).Action });

        List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
        int count = 0;
        //Console.WriteLine("MBTITG: 3");

        bool update = false;
        AiWayPoint wpAdd = CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action);

        if (wpAdd != null)        NewWaypoints.Add(wpAdd); //Always have to add current pos/speed as first point or things go w-r-o-n-g

        foreach (AiWayPoint wp in CurrentWaypoints)
        {
            AiWayPoint nextWP = wp;
            //Console.WriteLine( "Target: {0}", new object[] { wp });

            if ((wp as AiAirWayPoint).Action == null) return false;

            Point3d? newAirportPosition = wp.P as Point3d?;

            if (moveAirports && ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_TARG || (wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT)) { newAirportPosition = ChangeAirports(wp.P); }

            changeLimit changeL = new changeLimit();
            if (changeLimits.ContainsKey((wp as AiAirWayPoint).Action))
            {
                Point3d pos;
                double speed;

                changeL = changeLimits[(wp as AiAirWayPoint).Action];

                //TODO: We could have higher/lower altitude & speed apply to the entire mission for this airgroup rather than varying waypoint by waypoing. 
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
                        //Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
                        pos = wp.P;
                        if (newAirportPosition != null)
                        {
                            //Console.WriteLine("MBT: Moving airport of attack!");
                            pos = (Point3d)newAirportPosition;
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
                            Console.WriteLine("MBT: Target is NULL!! Breaking");
                            Console.WriteLine("MBT: {0} {1} {2} {3}", (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Target.Name(), (wp as AiAirWayPoint).GAttackPasses, (wp as AiAirWayPoint).GAttackType);

                            AiActor[] acts = airGroup.GetItems();
                            foreach (AiActor act in acts)
                            {
                                Console.WriteLine("MBT: {0}", act.Name());
                            }
                            break;
                        }
                        */

                        GroundStationary newTarget = null;
                        //Choose another ground stationary somewhere within the given radius of change, starting with the GATTACK point since we don't have an actual GATTACK target actor; make sure it is alive if possible
                        GroundStationary[] stationaries = GamePlay.gpGroundStationarys(pos.x, pos.y, changeL.XY_m);
                        //Console.WriteLine("MBT: Looking for nearby stationary");
                        for (int i = 1; i < 20; i++)
                        {

                            if (stationaries.Length == 0) break;
                            int newStaIndex = ran.Next(stationaries.Length - 1);
                            if (stationaries[newStaIndex] != null && stationaries[newStaIndex].IsAlive &&
                                (stationaries[newStaIndex].pos.x != pos.x ||
                                stationaries[newStaIndex].pos.y != pos.y))
                            {
                                newTarget = stationaries[newStaIndex];
                                //Console.WriteLine("MBT: FOUND a stationary");
                                break;
                            }
                        }
                        //In case we didn't find a ground target there, expand the search radius a bit & try again
                        if (newTarget == null)
                        {
                            //Console.WriteLine("MBT: Looking for further afield stationaries");
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
                            //Console.WriteLine("MBT: Found a stationary, updating attack position");
                            newPos.x = newTarget.pos.x;
                            newPos.y = newTarget.pos.y;
                        }
                        //3rd approach, just move the attack point by our usual amount
                        else
                        {
                            //Console.WriteLine("MBT: No stationary found, updating attack position");
                            newPos.x = pos.x + ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;
                            newPos.y = pos.y + ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;

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
                        //Console.WriteLine( "Updating, current TASK: {0}", new object[] { airGroup.getTask() });
                        //Console.WriteLine( "Target before: {0}", new object[] { (wp as AiAirWayPoint).Action });
                        pos = wp.P;

                        if ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT && newAirportPosition != null)
                        {
                            //Console.WriteLine("MBT: Moving airport of attack!");
                            pos = (Point3d)newAirportPosition;
                            pos.z = wp.P.z;
                        }

                        speed = wp.Speed;
                        pos.x += ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;
                        pos.y += ran.NextDouble() * 2 * changeL.XY_m - changeL.XY_m;
                        if (speedDiff == 0) speedDiff = speed * (ran.NextDouble() * 2 * changeL.speed_percent / 100 - changeL.speed_percent / 100);
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

                        nextWP = new AiAirWayPoint(ref pos, speed);
                        (nextWP as AiAirWayPoint).Action = (wp as AiAirWayPoint).Action;
                        //Console.WriteLine( "Target after: {0}", new object[] { nextWP });
                        //Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                        //Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                        update = true;
                        break;


                }
            }
            if (count >= currWay)
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

        foreach (AiWayPoint wp in NewWaypoints)
        {
            AiWayPoint nextWP = wp;
            //Console.WriteLine( "Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

        }


        //NewWaypoints.Add(CurrentPosWaypoint(airGroup));
        //NewWaypoints.AddRange(SetWaypointBetween(airGroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));
        //NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));


        if (update)
        {
            //Console.WriteLine("MBTITG: Updating this course");
            airGroup.SetWay(NewWaypoints.ToArray());
            return true;
        } else
        { return false; }
    }

    public void checkNewAirgroups()
    {
        GetCurrentAiAirgroups();
        foreach (AiAirGroup airGroup in airGroups)
        {

            if (AirgroupsWayPointProcessed.Contains(airGroup)) continue;

            AirgroupsWayPointProcessed.Add(airGroup);

            if (!isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft)) continue;

            updateAirWaypoints(airGroup);
        }
    }


    public void checkAirgroupsIntercept_recur()
    {
        /************************************************
         * 
         * Change airgroups to intercept nearest interesting enemy
         * Recursive function called every X seconds
         ************************************************/

        //Timeout(127, () => { checkAirgroupsIntercept_recur(); });

        Timeout(27, () => { checkAirgroupsIntercept_recur(); }); //for testing

        Task.Run(() => checkAirgroupsIntercept());
        //checkAirgroupsIntercept();
    }

    public void checkAirgroupsIntercept()
    {
        Console.WriteLine("MoveBomb: Checking airgroups intercepts, groups: " + airGroups.Count.ToString());
        foreach (AiAirGroup airGroup in airGroups)
        {

            if (airGroup.GetItems().Length > 0 && isAiControlledPlane2(airGroup.GetItems()[0] as AiAircraft))
            {
                Console.WriteLine("MoveBomb: Checking airgroups intercept for airgroup" + airGroup.Name());
                interceptNearestEnemyOnRadar(airGroup);
            } else
            {
                Console.WriteLine("MoveBomb: Skipping airgroup" + airGroup.Name());
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
        Mission mission;


        // Constructor Declaration of Class 
        public incpt (double timeToIntercept, double timeToWait, AiAirGroup attackingAirGroup, AiAirGroup targetAirGroup, Point3d pos, bool positionintercept, Mission mission, double timeInterceptStarted = -1)
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



    public bool interceptNearestEnemyOnRadar(AiAirGroup airGroup)
    {
        try
        {

            if (airGroup == null || !isAiControlledAirGroup(airGroup) || airGroup.GetItems().Length == 0)
            {
                Console.WriteLine("MoveBomb:airGroup is null, has no aircraft, or not AI, exiting");
                return false;
            }
            
            AiActor agActor = airGroup.GetItems()[0];
            AiAircraft agAircraft = agActor as AiAircraft;
            Console.WriteLine("MoveBomb: Checking radar returns for airGroup: " + agActor.Name() + " " + agAircraft.InternalTypeName());
            Dictionary<AiAirGroup, SortedDictionary<string, IAiAirGroupRadarInfo>> aris;
            double interceptTime_sec = 0;

            try
            {
                //TODO: In case of any of these as current task/airway point then we should skip chasing things altogether
                /*

                        case AiAirWayPointType.GATTACK_TARG:
                            case AiAirWayPointType.GATTACK_POINT:
                            case AiAirWayPointType.COVER:
                            case AiAirWayPointType.ESCORT:
                            case AiAirWayPointType.FOLLOW:
                */

                //reportAircraftFuel(agAircraft);

                /*if (airGroup.GetWay() == null)
                {
                    Console.WriteLine("MoveBomb:airGroup.GetWay() is null for " + agActor.Name());
                }
                */

                
                if (TWCMainMission != null) aris = TWCMainMission.ai_radar_info_store;
                else
                {
                    Console.WriteLine("MoveBomb: No TWCMainMission connected, returning");
                    return false;
                }

                if (!aris.ContainsKey(airGroup))
                {
                    Console.WriteLine("MoveBomb: No radar returns exist for this group, returning: " + agActor.Name());
                    return false;
                }

                double fuel = getAircraftFuel(agAircraft);
                int ammo = getAircraftAmmo(airGroup);

                if ((!airGroup.hasCourseWeapon() && !airGroup.hasCourseCannon()) || ammo < 40)
                {
                    Console.WriteLine("MoveBomb: Skipping no weapons & no cannon {0} {1} ammo: {2} " + airGroup.hasCourseWeapon(), airGroup.hasCourseCannon(), ammo);
                    return false;
                }

                if (fuel < 30)
                {
                    Console.WriteLine("MoveBomb: Skipping, low fuel: {0:N0} kg ", fuel);
                    return false;
                }
                if (airGroup.getTask() == AiAirGroupTask.DEFENDING)
                {
                    Console.WriteLine("MoveBomb: Busy defending, can't attack {0} {1} ", agActor.Name(), agAircraft.InternalTypeName());
                    return false;
                }

                Tuple<double?, double?> dist_altdiff = getDistanceToNearestFriendlyBombergroup(airGroup); //item1 = distance(meters), item2=altdiff(meters) + if this group is higher than bombers
                if (dist_altdiff.Item1 != null && dist_altdiff.Item1<9000 && dist_altdiff.Item2 > -1250)
                {
                    Console.WriteLine("MoveBomb: Near bombers, should be escorting them--not chasing things {0} {1} ", agActor.Name(), agAircraft.InternalTypeName());
                    return false;

                }


            }
            catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR7: " + ex.ToString()); return false; }



            SortedDictionary<string, IAiAirGroupRadarInfo> ai_radar_info = aris[airGroup];
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
                if (ai_radar_info[key] != null)
                {
                    aagri = ai_radar_info[key];

                    if (aagri.pagi.type != "F") continue; //We only get radar returns for fighters, so bombers are auto-skipped in this whole system.  But, we might as well be double sure here.

                    try
                    {
                        //So, because of the radar grouping system we should get ONLY grouped AirGroups & we can always plan on the one we get being the leader & we can/should use the AGG side of position, velocity, etc rather than the ag side.
                        //if (aagri.agi.AGGAIorHuman == aiorhuman.AI)
                        if (false) //for testing, just chase all airgroups including AI
                        {
                            Console.WriteLine("MoveBomb: Skipping because 100% AI airgroup {0}", aagri.agi.AGGAIorHuman);
                            continue; //we don't make AI attack other ai - that would be . . . futile plus waste CPU cycles
                        }
                        //If anything we should incorporate a scheme here to encourage AI to **avoid** attacking each other if possible
                    }
                    catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR5: " + ex.ToString()); return false; }

                    if (attackingAirgroupTimeToIntercept.ContainsKey(airGroup) && attackingAirgroupTimeToIntercept[airGroup].timeToIntercept > Time.current() && attackingAirgroupTimeToIntercept[airGroup].targetAirGroup == aagri.agi.airGroup)  //meaning that this airgroup is already attacking, the attack is current, and the target of the attack is the same target airGroup we are looking at on radar right now 
                    {
                        //Most of the time we just accept an updated radar plot for an airgroup we are already chasing

                        try
                        {
                            Console.WriteLine("MoveBomb: Looking to update the same target we were previously attacking: {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask(), aagri.agi.playerNames);

                            //so we ALWAYS accept an updated radar plot for the airgroup we are already chasing IF it is better than the other possibilities
                            if (bestAagri == null)
                            {
                                iPoint = aagri.interceptPoint;
                                bestAagri = aagri;
                                goodintercept = true;
                                Console.WriteLine("MoveBomb: Possibly updating {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask(), aagri.agi.playerNames);
                            }
                            else if (iPoint.z > aagri.interceptPoint.z)
                            {
                                iPoint = aagri.interceptPoint;
                                bestAagri = aagri;
                                goodintercept = true;
                                Console.WriteLine("MoveBomb: Possibly updating {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask(), aagri.agi.playerNames);
                            }
                            //But sometimes we stick with our previous chase even if it is worse - especially if we're already quite close, then we always do
                            if (ran.NextDouble() > 0.9 || aagri.interceptPoint.z < 3.5 * 60)
                            {
                                goodintercept = true;
                                iPoint = aagri.interceptPoint;
                                bestAagri = aagri;
                                Console.WriteLine("MoveBomb: Definitely updating {0} to intercept {5} {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask(), aagri.agi.playerNames);
                                break;
                            }
                            //So, the case where there is no good intercept, or it is very long intercept, but still we are quite close
                            //And we are already chasing, then this will become the bestNoninterceptAagri for sure
                            else if (ran.NextDouble()>0.85 && Calcs.CalculatePointDistance(aagri.agi.pos,aagri.pagi.pos) < 35000 && ( aagri.interceptPoint.z == 0 || aagri.interceptPoint.z > 10*60))
                            {
                                bestNoninterceptAagri = aagri;
                            }
                            else continue;
                        }
                        catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR6: " + ex.ToString()); return false; }

                    }
                    else if
                      (aagri.interceptPoint.x == null || aagri.interceptPoint.x == 0 || aagri.interceptPoint.y == 0 || aagri.interceptPoint.z > 10 * 60 || aagri.interceptPoint.z <= 0 ||
                          (targetAirgroupTimeToIntercept.ContainsKey(aagri.agi.airGroup) && targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept > Time.current() && (aagri.interceptPoint.z > targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept - 120 ||
                             targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept < Time.current() + 120
                            )
                           ) ||  // In case this target already has an a/g attacking it, skip - unless the old intercept time is still in the future more than 2 minutes out & new intercept time is better than the old one by a fair bit (120 seconds). In other words skip it, unless  the new one is quite a bit better than the old one, and the old one isn't almost ready to be intercepted regardless

                          (attackingAirgroupTimeToIntercept.ContainsKey(airGroup) && attackingAirgroupTimeToIntercept[airGroup].timeToIntercept > Time.current() &&
                             (
                                  attackingAirgroupTimeToIntercept[airGroup].timeToIntercept < Time.current() + 120 ||
                                  aagri.interceptPoint.z > attackingAirgroupTimeToIntercept[airGroup].timeToIntercept - 120  //In case this a/g already has an existing intercept, unless this one is quite a bit better than the current one (ie, a quicker intercept), skip it
                             )
                          )
                       )
                    {
                        try
                        {
                            Console.WriteLine("MoveBomb: Skipping {0} intercept {1} {2} {3} " + agActor.Name(),aagri.agi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z);

                            if (aagri.interceptPoint.x == null || aagri.interceptPoint.x == 0 || aagri.interceptPoint.y == 0 || aagri.interceptPoint.z > 10 * 60 || aagri.interceptPoint.z <= 0) { Console.WriteLine("MoveBomb: Skipping {0} intercept because no intercept or too distant {1} {2} {3} " + agActor.Name(), aagri.agi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z); }
                            else
                            {

                                Console.WriteLine("MoveBomb: Skipping for another reason . . . target: {0} attacker:" + agActor.Name(), aagri.agi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z);
                                if (attackingAirgroupTimeToIntercept.ContainsKey(airGroup) && attackingAirgroupTimeToIntercept[airGroup].timeToIntercept > Time.current()) Console.WriteLine("MoveBomb: Skipping because attacker {0} already has an existing intercept {1} " + attackingAirgroupTimeToIntercept[airGroup].timeToIntercept.ToString("N0"), aagri.pagi.playerNames, aagri.agi.playerNames);
                                if (targetAirgroupTimeToIntercept.ContainsKey(aagri.agi.airGroup) && targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept > Time.current()) Console.WriteLine("MoveBomb: Skipping because target {1} already has an existing interceptor {0} " + targetAirgroupTimeToIntercept[aagri.agi.airGroup].timeToIntercept.ToString("N0"), aagri.pagi.playerNames, aagri.agi.playerNames);
                            }

                            //this is the best non-intercept contact if either none already exists, or it is closest to the contact in question
                            if (bestNoninterceptAagri == null) bestNoninterceptAagri = aagri;
                            if (Calcs.CalculatePointDistance(bestNoninterceptAagri.pagi.pos, bestNoninterceptAagri.agi.pos) > Calcs.CalculatePointDistance(aagri.agi.pos, aagri.pagi.pos)) bestNoninterceptAagri = aagri;

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
                            //Console.WriteLine("MoveBomb: Moving {0} to intercept {1:N0} {2:N0} {3:N0} {4} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask());

                            Console.WriteLine("MoveBomb: Found an acceptable intercept! {0} to best intercept so far {1} {2:N0} {3:N0} {4:N0} {5} . Now, is it better?" + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask());

                            //If this is the first one we have found (iPoint.z==0) or better than our best interception point so far, then we accept it as the new intercept point
                            if (bestAagri == null || iPoint.z == 0)
                            {
                                iPoint = aagri.interceptPoint;
                                bestAagri = aagri;
                                goodintercept = true;
                                Console.WriteLine("MoveBomb: Moving {0} to best intercept so far {1} {2:N0} {3:N0} {4:N0} {5} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask());
                            }
                            else if (aagri.interceptPoint.z > 0 && iPoint.z > aagri.interceptPoint.z)
                            {
                                iPoint = aagri.interceptPoint;
                                bestAagri = aagri;
                                goodintercept = true;
                                Console.WriteLine("MoveBomb: Moving {0} to best intercept so far {1} {2:N0} {3:N0} {4:N0} {5} " + agAircraft.InternalTypeName(), aagri.pagi.playerNames, aagri.pagi.playerNames, aagri.interceptPoint.x, aagri.interceptPoint.y, aagri.interceptPoint.z, airGroup.getTask());
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
                    //if there is no 'good' intercept we'll still make them chase if they are within about 7 miles and reasonable altitude difference
                    //(less than 1700m to climb or 4000m to dive
                    double dis_m = Calcs.CalculatePointDistance(bestNoninterceptAagri.agi.AGGpos, bestNoninterceptAagri.pagi.pos);
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
                    Console.WriteLine("MoveBombINER: Returning - no good intercept found for airgroup: " + agActor.Name());

                    return false;
                }

                //OK, so now iPoint becomes our actual x,y,z point of intercept, which is our calculated intercept point, plus some potential
                //altitude over the target, with some randomness added to it in x,y,z
                interceptTime_sec = iPoint.z;
                iPoint.x += ran.NextDouble() * 3000 - 1500;
                iPoint.y += ran.NextDouble() * 3000 - 1500;
                iPoint.z = bestAagri.agi.pos.z + 2000 + ran.NextDouble() * 3000 - 1500;


                Console.WriteLine("MoveBombINER: Making new intercept for " + bestAagri.pagi.playerNames + " to attack " + bestAagri.agi.playerNames);

                //we have an actual good intercept not just a "best non intercept" vector, so we register
                //
                // if (bestAagri != null && bestNoninterceptAagri != bestAagri)
                
                if (targetAirgroupTimeToIntercept.ContainsKey(bestAagri.agi.airGroup))
                {
                    Console.WriteLine("MoveBombINER: Adding new/improved attacker " + bestAagri.pagi.playerNames + " for " + bestAagri.agi.playerNames);
                    //Do something to get rid of the old/worse pursuer
                    //AiAirGroup airGroupToRemove = targetAirgroupTimeToIntercept[bestAagri.agi.airGroup].attackingAirGroup;
                    removeAttackingAirGroup(targetAirgroupTimeToIntercept[bestAagri.agi.airGroup]);
                }
                //targetAirgroupTimeToIntercept.Add(bestAagri.agi.airGroup, Time.current() + bestAagri.interceptPoint.z + 125.0 + ran.NextDouble() * 240.0 - 120.0);  //target can't get another interceptor assigned until this time is up, the actual time to the intercept plus 2 mins +/- 2 mins
                targetAirgroupTimeToIntercept[bestAagri.agi.airGroup] = new incpt(Time.current() + interceptTime_sec, 125.0 + ran.NextDouble() * 240.0 - 120.0, bestAagri.pagi.airGroup, bestAagri.agi.airGroup, iPoint, positionintercept, this); //pagi is the attacker ("player" airgroup), agi is the target

                //however, if this is a "bestNoninterceptAagri" type intercept, we don't consider it an actual intercept (because they WON'T intercept) but rather a move to see if
                //the attacker can get in position to actually have an intercept.  So we don't register a targetAirgroupTimeToIntercept at all, which allows another attacker to take an intercept if there is one.
                
            }
            catch (Exception ex) { Console.WriteLine("MoveBomb Intercept ERROR2: " + ex.ToString()); return false; }

            try
            {
                if (attackingAirgroupTimeToIntercept.ContainsKey(bestAagri.pagi.airGroup)) Console.WriteLine("MoveBombINER: Adding new/improved intercept for attacker " + bestAagri.pagi.playerNames + " to attack " + bestAagri.agi.playerNames);  //This is only an FYI to let us know that this airGroup had a previous target we were attacking & now we are updating it.

                //attackingAirgroupTimeToIntercept[bestAagri.pagi.airGroup] = Time.current() + bestAagri.interceptPoint.z + 125.0 + ran.NextDouble() * 240.0 - 120.0;  //attacker can't get another intercept unti lthis time is up, the actual time to the intercept plus 2 mins +/- 2 mins

                //we always replace this value as it is represents what our current airgroup is doing, and we have decided to attack this target.  Sometimes it replaces
                //a previous target sometimes is just a new target
                attackingAirgroupTimeToIntercept[bestAagri.pagi.airGroup] = new incpt(Time.current() + interceptTime_sec, 125.0 + ran.NextDouble() * 240.0 - 120.0, bestAagri.pagi.airGroup, bestAagri.agi.airGroup, iPoint, positionintercept, this);


                AiWayPoint[] CurrentWaypoints = airGroup.GetWay();

                //for testing
                foreach (AiWayPoint wp in CurrentWaypoints)
                {
                    AiWayPoint nextWP = wp;
                    Console.WriteLine("Add intcpt -  Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }
                
                int currWay = airGroup.GetCurrentWayPoint();
                double speedDiff = 0;
                double altDiff_m = 0;

                //Console.WriteLine("MBTITG: 2");
                if (currWay < CurrentWaypoints.Length) Console.WriteLine("IntcpCalc: {0}", new object[] { CurrentWaypoints[currWay] });
                //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { CurrentWaypoints[currWay].Speed });
                //if (currWay < CurrentWaypoints.Length) Console.WriteLine( "WP: {0}", new object[] { (CurrentWaypoints[currWay] as AiAirWayPoint).Action });

                List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
                int count = 0;
                //Console.WriteLine("MBTITG: 3");

                bool update = false;

                NewWaypoints.Add(CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action)); //Always have to add current pos/speed as first point or things go w-r-o-n-g

                foreach (AiWayPoint wp in CurrentWaypoints)
                {
                    AiWayPoint nextWP = wp;
                    //Console.WriteLine( "Target: {0}", new object[] { wp });

                    if ((wp as AiAirWayPoint).Action == null) return false;


                    if (count == currWay)
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
                                Console.WriteLine("WP before{0}: {1:N0} {2:N0} {3:N0} {4:N0}", new object[] { count, wp.Speed, wp.P.x, wp.P.y, wp.P.z });
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
                                Console.WriteLine("Added{0}: {1:N0} {2:N0} {3:N0} {4:N0}", new object[] { count, nextWP.Speed, nextWP.P.x, nextWP.P.y, nextWP.P.z });
                                //Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                                update = true;

                        /*
                                break;


                        }
                        */
                    }
                    if (count >= currWay)
                    {
                        NewWaypoints.Add(nextWP);

                        if (update)
                        {
                            Console.WriteLine( "Added{0}: {1}", new object[] { count, nextWP.Speed });
                            Console.WriteLine( "Added: {0}", new object[] { (nextWP as AiAirWayPoint).Action });
                        }

                    }

                    //Console.WriteLine("MBTITG: 4");
                    count++;



                }


                foreach (AiWayPoint wp in NewWaypoints)
                {
                    AiWayPoint nextWP = wp;
                    Console.WriteLine( "Add intcpt - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }


                //NewWaypoints.Add(CurrentPosWaypoint(airGroup));
                //NewWaypoints.AddRange(SetWaypointBetween(airGroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));
                //NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));


                if (update)
                {
                    //Console.WriteLine("MBTITG: Updating this course");
                    airGroup.SetWay(NewWaypoints.ToArray());
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
    public void removeAttackingAirGroup(incpt intc)
    {
        try
        {
            AiAirGroup airGroup = intc.attackingAirGroup;
            AiWayPoint[] CurrentWaypoints = airGroup.GetWay();
            if (CurrentWaypoints == null || CurrentWaypoints.Length == 0) return;

            foreach (AiWayPoint wp in CurrentWaypoints)
            {
               
                Console.WriteLine("RemoveAttackingAG - Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

            }


            int currWay = airGroup.GetCurrentWayPoint();
            Console.WriteLine("RemoveAttackingAG - currWay: {0} {1:n0} {2:n0}", new object[] {currWay, intc.pos.x, intc.pos.y});

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
                    if (Math.Abs(nextWP.P.x - intc.pos.x) < 100 && Math.Abs(nextWP.P.y - intc.pos.y) < 100 && Math.Abs(nextWP.P.z - intc.pos.z) < 100  &&
                          ((nextWP as AiAirWayPoint).Action == AiAirWayPointType.AATTACK_FIGHTERS || (nextWP as AiAirWayPoint).Action == AiAirWayPointType.AATTACK_BOMBERS))
                    {
                        update = true;
                        count++;
                        continue;
                    }
                    NewWaypoints.Add(nextWP);
                }
                count++;

            }
            if (update)
            {
                //Console.WriteLine("MBTITG: Updating this course");
                airGroup.SetWay(NewWaypoints.ToArray());

                foreach (AiWayPoint wp in NewWaypoints)
                {                    
                    Console.WriteLine("RemoveAttackingAG - Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

                }

            }
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb RemoveIntercept: " + ex.ToString()); }
    }


    public override void OnTickGame()
    {
        base.OnTickGame();

        if (Time.tickCounter() % 305 == 41)
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
            Console.WriteLine("MoveBomb: Aircraft levels speed {0:N0} ammo {1:N0} sFuel {2:N0} iFuel {3:N0} {4}", speed, ammo, sFuel, iFuel, aircraft.InternalTypeName());
            return iFuel;
        }
        catch (Exception ex) { Console.WriteLine("MoveBomb Fuelreport ERROR: " + ex.ToString()); return -1; }

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
