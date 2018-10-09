//$reference System.Core.dll
using System;
using System.Collections;
using System.Collections.Generic;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using System.Text;

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
    public Mission ()
    {

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
            { AiAirWayPointType.GATTACK_POINT, new changeLimit (700, 0, 0, 10, 15000) },
            { AiAirWayPointType.GATTACK_TARG, new changeLimit (700, 0, 0, 10, 15000) },
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
        AiAirWayPoint aaWP = null;
        double speed = (airGroup.GetItems()[0] as AiAircraft).getParameter(part.ParameterTypes.Z_VelocityTAS, -1);

        Point3d CurrentPos = airGroup.Pos();

        aaWP = new AiAirWayPoint(ref CurrentPos, speed);
        aaWP.Action = AiAirWayPointType.NORMFLY;

        return aaWP;
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

    List<AiAirGroup> airGroups = new List<AiAirGroup>();
    HashSet<AiAirGroup> AirgroupsWayPointProcessed = new HashSet<AiAirGroup>();

    public void GetCurrentAiAirgroups()
    {

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

/*
    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        if (missionNumber != MissionNumber) return; //only do this when this particular mission is loaded.

        GetCurrentAiAirgroups();

        //airGroup = GamePlay.gpActorByName("0:BoB_LW_StG2_II.07") as AiAirGroup;

    }
    */
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
            Console.WriteLine("MBT: Attack point NOT within an airfield {0:n0} {1:n0} {2:n0} {3:n0} {4:n0}", nearestAirfield.Pos().x, nearestAirfield.Pos().y, p.x, p.y, Calcs.CalculatePointDistance(nearestAirfield.Pos(), p));
            return null;
        }

        


        //Get the random airport within the given radius
        AiAirport ap = GetRandomAirfieldNear(p, moveAirportsDistance_m);

        Console.WriteLine("MBT: Attack point IS within an airfield {0:n0} {1:n0} {2:n0} {3:n0} {4:n0} {5} to {6}", nearestAirfield.Pos().x, nearestAirfield.Pos().y, p.x, p.y, Calcs.CalculatePointDistance(nearestAirfield.Pos(), p), nearestAirfield.Name(), ap.Name());

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
            retPos.x = p.x - nearestAirfield.Pos().x + ap.Pos().x;
            retPos.y = p.y - nearestAirfield.Pos().y + ap.Pos().y;
            retPos.z = 0;
            Console.WriteLine("MBT: New attack point: {0:n0} {1:n0} {2:n0} {3:n0} {4:n0} {5} to {6}", ap.Pos().x, ap.Pos().y, retPos.x, retPos.y, Calcs.CalculatePointDistance(ap.Pos(), retPos), nearestAirfield.Name(), ap.Name());
            return retPos;
        }
        else return null;

    }

    public bool updateAirWaypoints (AiAirGroup airGroup)
    {
        if (airGroup == null || airGroup.GetWay() == null || !isAiControlledAirGroup(airGroup)) return false;

        AiWayPoint[] CurrentWaypoints = airGroup.GetWay();

        foreach (AiWayPoint wp in CurrentWaypoints)
        {
            AiWayPoint nextWP = wp;
            Console.WriteLine("Target before: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

        }
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

        NewWaypoints.Add(CurrentPosWaypoint(airGroup, (CurrentWaypoints[currWay] as AiAirWayPoint).Action)); //Always have to add current pos/speed as first point or things go w-r-o-n-g

        foreach (AiWayPoint wp in CurrentWaypoints)
        {
            AiWayPoint nextWP = wp;
            //Console.WriteLine( "Target: {0}", new object[] { wp });

            if ((wp as AiAirWayPoint).Action == null) return false;

            Point3d? newAirportPosition = wp.P as Point3d?;

            if (moveAirports && ((wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_TARG || (wp as AiAirWayPoint).Action == AiAirWayPointType.GATTACK_POINT )) { newAirportPosition = ChangeAirports(wp.P); }

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
                        
                        if (speedDiff==0)  speedDiff = wp.Speed * (ran.NextDouble() * 2.0 * changeL.speed_percent / 100.0 - changeL.speed_percent / 100);
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
                        Console.WriteLine("MBT: Looking for nearby stationary");
                        for (int i = 1; i < 20; i++)
                        {
                            
                            if (stationaries.Length == 0) break;
                            int newStaIndex = ran.Next(stationaries.Length - 1);
                            if (stationaries[newStaIndex] != null && stationaries[newStaIndex].IsAlive && 
                                (stationaries[newStaIndex].pos.x != pos.x ||
                                stationaries[newStaIndex].pos.y != pos.y ))
                            {                              
                                newTarget = stationaries[newStaIndex];
                                Console.WriteLine("MBT: FOUND a stationary");
                                break;
                            }                            
                        }
                        //In case we didn't find a ground target there, expand the search radius a bit & try again
                        if (newTarget == null)
                        {
                            Console.WriteLine("MBT: Looking for further afield stationaries");
                            GroundStationary[] stationaries2 = GamePlay.gpGroundStationarys(pos.x, pos.y, 3*changeL.XY_m);
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
                            Console.WriteLine("MBT: Found a stationary, updating attack position");
                            newPos.x = newTarget.pos.x;
                            newPos.y = newTarget.pos.y;
                        }
                        //3rd approach, just move the attack point by our usual amount
                        else
                        {
                            Console.WriteLine("MBT: No stationary found, updating attack position");
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
                        Console.WriteLine("Old Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (wp as AiAirWayPoint).Action, nm, (wp as AiAirWayPoint).P.x, (wp as AiAirWayPoint).P.y, (wp as AiAirWayPoint).GAttackPasses, (wp as AiAirWayPoint).GAttackType });
                        Console.WriteLine ("New Ground Target: {0} {1} {2:n0} {3:n0} {4} {5}", new object[] { (wp as AiAirWayPoint).Action, nm, (nextWP as AiAirWayPoint).P.x, (nextWP as AiAirWayPoint).P.y, (nextWP as AiAirWayPoint).GAttackPasses, (nextWP as AiAirWayPoint).GAttackType });
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
                            Console.WriteLine("MBT: Moving airport of attack!");
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
                        if (altDiff_m == 0 || zSave * (1 - changeL.alt_percent / 100) > zSave + altDiff_m )
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
            Console.WriteLine( "Target after: {0} {1:n0} {2:n0} {3:n0} {4:n0}", new object[] { (wp as AiAirWayPoint).Action, (wp as AiAirWayPoint).Speed, wp.P.x, wp.P.y, wp.P.z });

        }


        //NewWaypoints.Add(CurrentPosWaypoint(airGroup));
        //NewWaypoints.AddRange(SetWaypointBetween(airGroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));
        //NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));


        if (update)
        {
            Console.WriteLine("MBTITG: Updating this course");
            airGroup.SetWay(NewWaypoints.ToArray());
            return true;
        } else
        { return false; }
    }

    Random ran = new Random();
    public override void OnTickGame()
    {
        base.OnTickGame();

        if (Time.tickCounter() % 305 == 41)
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
