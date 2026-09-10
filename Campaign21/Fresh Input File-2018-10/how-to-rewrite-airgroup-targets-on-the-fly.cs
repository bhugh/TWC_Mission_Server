using System;
using System.Collections;
using System.Collections.Generic;
using maddox.game;
using maddox.game.world;
using maddox.GP;


public class Mission : AMission
{

    AiAirGroup WayPointTestAirgroup;
    AiAirport AirGroupAirfield;
    bool toHome = false;


    public AiWayPoint CurrentPosWaypoint(AiAirGroup airGroup)
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


    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        MissionNumberListener = -1;

        WayPointTestAirgroup = base.GamePlay.gpActorByName("0:BoB_LW_JG27_I.01") as AiAirGroup;
        AirGroupAirfield = GetAirfieldAt(WayPointTestAirgroup.Pos());
    }


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

    public override void OnTickGame()
    {
        base.OnTickGame();

        if (Time.tickCounter() % 1800 == 1799)
        {
            GamePlay.gpLogServer(null, "TASK: {0}", new object[] { WayPointTestAirgroup.getTask() });
            if (!toHome)
            {
                foreach (AiActor ac in WayPointTestAirgroup.GetItems())
                {
                    GamePlay.gpLogServer(null, "Plane: {0} , Fuel: {1}, Taskcomplete: {2}", new object[] { (ac as AiAircraft).Name(), (ac as AiAircraft).getParameter(part.ParameterTypes.S_FuelReserve, -1), (ac as AiAircraft).IsTaskComplete() });

                    if ((ac as AiAircraft).getParameter(part.ParameterTypes.S_FuelReserve, -1) < 150)
                    {
                        toHome = true;
                        List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();

                        NewWaypoints.Add(CurrentPosWaypoint(WayPointTestAirgroup));
                        NewWaypoints.AddRange(SetWaypointBetween(WayPointTestAirgroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));
                        NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));

                        WayPointTestAirgroup.SetWay(NewWaypoints.ToArray());


                        //WayPointTestAirgroup.setTask(AiAirGroupTask.LANDING, null);
                    }
                }
            }
        }
    }


    public override void OnTrigger(int missionNumber, string shortName, bool active)
    {
        base.OnTrigger(missionNumber, shortName, active);

        if (shortName.Equals("trigger"))
        {
            GamePlay.gpGetTrigger(shortName).Enable = false;

            List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();

            NewWaypoints.Add(CurrentPosWaypoint(WayPointTestAirgroup));
            NewWaypoints.AddRange(WaitingWayPoints(GetXYCoord(WayPointTestAirgroup), 4000.0, 80.0, 10000.0, 5000.0, 20, AiAirWayPointType.HUNTING));

            NewWaypoints.AddRange(SetWaypointBetween(WayPointTestAirgroup.Pos(), AirGroupAirfield.Pos(), 4000, 90.0));

            NewWaypoints.Add(GetLandingWaypoint(AirGroupAirfield, 1000.0));

            WayPointTestAirgroup.SetWay(NewWaypoints.ToArray());
        }

        if (shortName.Equals("AttackTrigger"))
        {

            GamePlay.gpGetTrigger(shortName).Enable = false;

            AiAirGroup TestGroup = getNearestEnemyAirgroup(WayPointTestAirgroup);

            if (TestGroup != null)
            {
                GamePlay.gpLogServer(null, "Nächste Airgroup: {0}", new object[] { TestGroup.Name() });
            }

            if (getDistanceToNearestEnemyAirgroup(WayPointTestAirgroup).HasValue)
            {
                if (getDistanceToNearestEnemyAirgroup(WayPointTestAirgroup).Value < 10000.0)
                {
                    GamePlay.gpLogServer(null, "Entfernung: {0}", new object[] { getDistanceToNearestEnemyAirgroup(WayPointTestAirgroup).Value });
                    WayPointTestAirgroup.setTask(AiAirGroupTask.ATTACK_AIR, TestGroup);
                }
            }
        }
    }
}