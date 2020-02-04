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


/*
 * The beam angles were so dramatically reduced that it was only a few tens of yards wide over the target.
 * 
 * The beam from a single transmitter would guide the bombers towards the target, but could not tell them when they were over it. To add this ranging feature, a second transmitter similar to the first was set up so its beam crossed the guidance beam at the point where the bombs should be dropped. The aerials could be rotated to make the beams from two transmitters cross over the target. The bombers would fly into the beam of one and ride it until they started hearing the tones from the other (on a second receiver). When the steady "on course" sound was heard from the second beam, they dropped their bombs.[6] 
 * 
 * 
 * */

public class KnickebeinTarget
{
    public Point3d initialPoint;
    public Point3d targetPoint;
    public double targetBearingAngle_deg; //angle from initial to target points
    public double targetBearingAngle_magnetic_deg; //equivalent of this on pilot's magnetic compass
    public Player player;
    public bool turnedOn;
    Mission mission;
    
    public KnickebeinTarget(Player player, Point3d initialPoint, Point3d targetPoint, Mission mission)
    {
        this.player = player;
        this.initialPoint = initialPoint;
        this.targetPoint = targetPoint;
        this.targetBearingAngle_deg = Calcs.CalculateGradientAngle(this.initialPoint, this.targetPoint);
        this.targetBearingAngle_magnetic_deg = Calcs.realBearingDegreetoCompass(this.targetBearingAngle_deg);
        this.mission = mission;
        
    }
    public KnickebeinTarget(Player player, Point3d targetPoint, Mission mission)
    {
        this.mission = mission;
        this.player = player;        
        this.targetPoint = targetPoint;
        this.initialPoint = new Point3d (0,0,0);
        if (player.Place() != null) this.initialPoint = player.Place().Pos();
        this.targetBearingAngle_deg = Calcs.CalculateGradientAngle(this.initialPoint, this.targetPoint);
        this.targetBearingAngle_magnetic_deg = Calcs.realBearingDegreetoCompass(this.targetBearingAngle_deg);

    }
    public KnickebeinTarget(Player player, double angle_deg, double dist, Mission mission)
    {
        this.mission = mission;
        this.player = player;
        this.initialPoint = new Point3d(0, 0, 0);
        if (player.Place() != null) this.initialPoint = player.Place().Pos();
        double dist_m = dist * 1000; //if army==2 we assume distance in km
        if (player.Army() == 1) dist_m = Calcs.miles2meters(dist);
        this.targetPoint = Calcs.EndPointfromStartPointAngleDist(this.initialPoint, angle_deg, dist_m);
        this.targetBearingAngle_deg = angle_deg;
        this.targetBearingAngle_magnetic_deg = Calcs.realBearingDegreetoCompass(this.targetBearingAngle_deg);
        if (Math.Round(this.targetBearingAngle_deg) != Math.Round(Calcs.CalculateGradientAngle(this.initialPoint, this.targetPoint)))
        {
            Console.WriteLine("Knicke: HELP! Given angle {0} calced angle {1}", this.targetBearingAngle_deg, Calcs.CalculateGradientAngle(this.initialPoint, this.targetPoint));
        }
        
    }
    //start display
    public void turnOn()
    {
        this.turnedOn = true;
        display_recurs();
    }
    //end display
    public void turnOff()
    {
        this.turnedOn = false;
        if (mission.GamePlay != null) mission.GamePlay.gpHUDLogCenter(new Player[] { player }, ""); //clear the HUD
    }

    public void resetInitialPoint(Player player)
    {
        this.mission = mission;
        this.player = player;
        this.initialPoint = new Point3d(0, 0, 0);
        if (player.Place() != null) this.initialPoint = player.Place().Pos();
    }
    /*
    public string directionPip(Player player)
    {
        AiActor actor = player.Place();
        if (actor == null) return "";
        double currentTargetBearingAngle_deg = Calcs.CalculateGradientAngle(actor.Pos(), this.targetPoint);
        double deltaBearingAngle_deg = this.targetBearingAngle_deg - currentTargetBearingAngle_deg;
        //we want the result in the range +180 to -180 so that we will display between +5/-5 degrees of the current heading
        //this also works for 
        if (deltaBearingAngle_deg > 180) deltaBearingAngle_deg -= 360;
        if (deltaBearingAngle_deg < -180) deltaBearingAngle_deg += 360;
        int deltaBear = Convert.ToInt32(Math.Round(deltaBearingAngle_deg));
        if (deltaBear > 5) deltaBear = 5;
        if (deltaBear < -5) deltaBear = -5;
        if (deltaBear == 0) return "=";
        char c = '-';
        if (deltaBear < 0) c = '+';
        string ret = new string(c, Math.Abs(deltaBear));
        ///
        for (int i=0; i<Math.Abs(deltaBear); i++)
        {
            if (deltaBear < 0) ret += "+";
            if (deltaBear > 0) ret += "-";
        }
        //
        return ret;
    }
    */


    public string directionPip(Player player)
    {
        AiActor actor = player.Place();
        if (actor == null) return new string(' ', 12);
        double currentTargetBearingAngle_deg = Calcs.CalculateGradientAngle(actor.Pos(), this.targetPoint);
        double deltaBearingAngle_deg = this.targetBearingAngle_deg - currentTargetBearingAngle_deg;
        //we want the result in the range +180 to -180 so that we will display between +5/-5 degrees of the current heading
        //this also works for 
        if (deltaBearingAngle_deg > 180) deltaBearingAngle_deg -= 360;
        if (deltaBearingAngle_deg < -180) deltaBearingAngle_deg += 360;

        double distFromLine = Calcs.distancePointToLine(this.initialPoint, this.targetPoint, actor.Pos());
                    

        int deltaBear = Convert.ToInt32(Math.Round(distFromLine/100));
        //bool noshow = false;
        //if (distFromLine > 6000) noshow = true; //The wide 'guide beam' (ie similar to X-gerat) is let's say 6000m from the center
        //if (distFromLine < 12000 && mission.ran.NextDouble() * 6000 < distFromLine - 6000) noshow = false; //it phases in 6000-12000m from center
        if (deltaBear > 12) deltaBear = 12;
        //if (deltaBear < -5) deltaBear = -5;
        
        if (deltaBear == 0) return new string(' ', 11) + "=" ;
        char c = '-';
        if (deltaBearingAngle_deg < 0) c = '+';
        //if (noshow) c = ' ';
        string ret = new string(c, Math.Abs(deltaBear));
        string pad = new string(' ', 12-ret.Length);
        /*
        for (int i=0; i<Math.Abs(deltaBear); i++)
        {
            if (deltaBear < 0) ret += "+";
            if (deltaBear > 0) ret += "-";
        }
        */
        return pad + ret;
    }
    public Tuple<double,string> distancePip(Player player)
    {
        AiActor actor = player.Place();
        if (actor == null) return new Tuple<double, string>(0,"");

        //So we want to use distance from the aircraft location to the CROSSBEAM, not simply distance to the targetpoint
        //This makes a difference in several ways, as it is useful to be able to tell when crossing the beam even if you're not
        //near or at the crossing point of the two beams.
        //So this is right triangle with aircraft point - targetPoint the hypotenuse & aircraftpoint-beam + beam crossing point-target point the two sides
        //So, just the distance formula b = sqrt(c^2-a^2)
        //we double calculate distFromBeam_m here & in directionPip() so a bit of work could avoid that
        double currentTargetPointdistance_m = Calcs.CalculatePointDistance( actor.Pos(), this.targetPoint);
        double distFromBeam_m = Calcs.distancePointToLine(this.initialPoint, this.targetPoint, actor.Pos());
        double distFromCrossBeam_m = Math.Sqrt(currentTargetPointdistance_m * currentTargetPointdistance_m - distFromBeam_m * distFromBeam_m);

        //bool noshow = false;
        //if (distFromCrossBeam_m > 10000) noshow = true; //The wide 'guide beam' (ie similar to X-gerat) is let's say 6000m from the center
        //if (distFromCrossBeam_m < 16000 && mission.ran.NextDouble() * 6000 < distFromCrossBeam_m - 10000) noshow = false;

        int deltaDist_km = Convert.ToInt32(Math.Round(distFromCrossBeam_m / 1000));
        if (deltaDist_km > 10) deltaDist_km = 10;        
        //if (deltaDist_km == 0) return new Tuple<double, string>(currentTargetdistance_m, "=");
        
        char c = '|';
        //if (noshow) c = ' ';
        string ret = new string(c, deltaDist_km);

        if (distFromCrossBeam_m < 1000)
        {
            c = '.';
            ret = new string(c, Convert.ToInt32(Math.Round(distFromCrossBeam_m / 100)));
        }
        if (distFromCrossBeam_m < 100) ret = "*";
        ret += new string(' ', 11 - ret.Length);
        return new Tuple <double,string> (distFromCrossBeam_m, ret);
    }

    public double displayPips(Player player=null)
    {
        if (player == null) player = this.player;
        AiActor actor = player.Place();
        if (actor == null) return 0;

        Tuple<double, string> distT = distancePip(player);
        double dist = distT.Item1;
        string disp = directionPip(player) + " " + distT.Item2;        
        if (mission.GamePlay != null) mission.GamePlay.gpHUDLogCenter(new Player[] { player }, disp);
        return dist;

    }

    private void display_recurs()
    {
        if (!this.turnedOn) return;
        double t = 5.3;
        double dist = displayPips();
        if (dist < 10000 && dist > 0) t = dist/10000*5;
        if (t < 0.05) t = 0.05;
        mission.Timeout(t, () => display_recurs());
        //knickebeins[player] = new KnickebeinTarget(player, 123, 23, this);
        
    }


}

public class Knickebeinholder
{

    public Dictionary<Player, KnickebeinTarget> knickebeins { set; get; }
    public Dictionary<Player, List<Point3d>> knickebeinWaypoints { set; get; }
    public Dictionary<Player, int> knickebeinCurrentWaypoint { set; get; }

    public Mission mission;

    public Knickebeinholder (Mission mission)
    {
        this.mission = mission;

        knickebeins = new Dictionary<Player, KnickebeinTarget>();
        knickebeinWaypoints = new Dictionary<Player, List<Point3d>>();
        knickebeinCurrentWaypoint = new Dictionary<Player, int>();
        Console.WriteLine("Knickebeinholder inited!");

    }
    public void startQuickKnickebein(Player player, double angle_deg, double distance)
    {
        try
        {
            if (player == null) return;
            //Distance is auto adjusted to miles for Red & km for Blue
            //knickebeins[player] = new KnickebeinTarget(player, angle_deg, distance, mission);
            //knickebeins[player].turnOn();
            //startKnickebein_recurs(player);

            //Make a new knickebein point based on the angle, distance
            Point3d initialPoint = new Point3d(0, 0, 0);
            if (player.Place() != null) initialPoint = player.Place().Pos();
            double dist_m = distance * 1000; //if army==2 we assume distance in km
            if (player.Army() == 1) dist_m = Calcs.miles2meters(distance); //If army = 1 distance inmiles
            Point3d targetPoint = Calcs.EndPointfromStartPointAngleDist(initialPoint, angle_deg, dist_m);
            
            //add it to the knickebein list
            KniAdd(player, targetPoint);

            //Make the current knickebein waypoint the one we've just added
            int currWay = 0;
            if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];


            List<Point3d> currpoints = new List<Point3d>();
            if (knickebeinWaypoints.ContainsKey(player)) currpoints = knickebeinWaypoints[player];
            currWay = currpoints.Count - 1;
            knickebeinCurrentWaypoint[player] = currWay;

            //and, start the knickebein we've just added
            KniStart(player, startWay_onebased: currWay + 1);

        }
        catch (Exception ex) { Console.WriteLine("Knickebein  SQKB: " + ex.ToString()); }
    }
    public void KniStop(Player player, bool display = true)
    {
        //Distance is auto adjusted to miles for Red & km for Blue
        if (display && mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Display turned off.", new object[] { });
        if (!knickebeins.ContainsKey(player)) return;        
        knickebeins[player].turnOff();
        
        
        //startKnickebein_recurs(player);
    }
    public void KniTest()
    {
        //Console.WriteLine("A Test!" + this.knickebeinWaypoints.;
        Console.WriteLine("Test" + this.knickebeinWaypoints.Count.ToString());
        
    }
    public int KniAdd (Player player, Point3d point)
    {
        try
        {
            List<Point3d> currpoints = new List<Point3d>();
            if (knickebeinWaypoints.ContainsKey(player)) currpoints = knickebeinWaypoints[player];
            currpoints.Add(point);
            knickebeinWaypoints[player] = currpoints;
            return currpoints.Count;
        }
        catch (Exception ex) { Console.WriteLine("Knickebeins.KniAdd: " + ex.ToString()); return 0; }
    }
    public void KniDelete(Player player, int wp_oneBased)
    {

        int currWay = 0;
        if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];
        if (wp_oneBased - 1 == currWay)
        {

            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Waypoint #{0} is the current active waypoint - you've deleted it from the list but it is still active. Use <kon, <koff, <kstop to proceed.", new object[] { wp_oneBased });
        }

        if (wp_oneBased - 1 < currWay) currWay -= 1;
        knickebeinCurrentWaypoint[player] = currWay;

        List<Point3d> currpoints = new List<Point3d>();
        if (knickebeinWaypoints.ContainsKey(player)) currpoints = knickebeinWaypoints[player];
        if (wp_oneBased < 1 || wp_oneBased > currpoints.Count)
        {
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Your Knickebein Waypoints List didn't include item #{0}", new object[] { wp_oneBased });
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Your Knickebein Waypoints List has {0} waypoints", new object[] { currpoints.Count });
            return;
        }
        currpoints.RemoveAt(wp_oneBased - 1); //publicly displayed waypoint lists are 1-based numbering whereas c# internal number is 0-based
        //.Add(point);
        knickebeinWaypoints[player] = currpoints;

        

    }
    //Starts with waypoint 0 if continue = false, startWay =0
    //Continues currently set way without stopping/restarting if contin = true (ignores startWay), if currently set way exists for the player
    //This is like an display off/on switch
    //Do do a re-start of current way, you'd call it again with the same waypoint.
    public bool KniStart(Player player, bool contin = false, bool next = false, bool prev = false, int startWay_onebased=0)
    {

        if (contin && knickebeins.ContainsKey(player)) contin = true;
        else contin = false;

        if (!contin) KniStop(player, false);
        List<Point3d> currpoints = new List<Point3d>();
        if (knickebeinWaypoints.ContainsKey(player)) currpoints = knickebeinWaypoints[player];
        else
        {
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: No waypoints entered yet", new object[] { });
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "<khelp for help with the Knickebein system", new object[] { });
            return false;
        }
        int currWay = 0;
        if (startWay_onebased > 0) currWay = startWay_onebased - 1;        
        if ((contin || next || prev) && knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];
        int currWaySave = currWay;
        if (next) currWay++;
        if (prev) currWay--;
        if (currpoints.Count - 1 < currWay) currWay = currpoints.Count - 1;
        if (currWay < 0) currWay = 0;
        
        knickebeinCurrentWaypoint[player] = currWay;

        Point3d currPoint = currpoints[currWay];

        string sector = Calcs.correctedSectorNameDoubleKeypad(mission, currPoint);

        if (contin)
        {
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Continuing with Knickebein waypoint #{0} to {1} ({2:N0}/{3:N0})", new object[] { currWay + 1, sector, Math.Round(currPoint.x), Math.Round(currPoint.y) });
        }
        else if ((next || prev) && currWay == currWaySave)
        {
            string wd = "next";
            if (prev) wd = "previous";
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: No {4} Knickebein Waypoint. Continuing with Knickebein waypoint #{0} to {1} ({2:N0}/{3:N0})", new object[] { currWay + 1, sector, Math.Round(currPoint.x), Math.Round(currPoint.y), wd });

        }
        else {
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Starting Knickebein waypoint #{0} to {1} ({2:N0},{3:N0})", new object[] { currWay + 1, sector, Math.Round(currPoint.x), Math.Round(currPoint.y) });
        }
        

        if (!contin && !((next || prev) && currWay == currWaySave)) knickebeins[player] = new KnickebeinTarget(player, currPoint, mission);

        knickebeins[player].turnOn();

        if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Make your heading {0:F0} degrees magnetic", new object[] { Math.Round(knickebeins[player].targetBearingAngle_magnetic_deg) }); //note the gplogserver IGNORES any formatting requests such as N0 or F0 . . . .
        return true;
       
    }
    
    //So this is the all-purpose start/on/next
    //If not started, it starts with #1.  If off turns on.  If started waypoing #N, moves to #N+1
    //
    public bool KniOnStartOrNext(Player player)
    {               
                
        List<Point3d> currpoints = new List<Point3d>();
        if (knickebeinWaypoints.ContainsKey(player)) currpoints = knickebeinWaypoints[player];
        else
        {
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: No waypoints entered yet", new object[] { });
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "<khelp for help with the Knickebein system", new object[] { });
            return false;
        }
        if (knickebeins.ContainsKey(player) && !knickebeins[player].turnedOn)
        {
            knickebeins[player].turnOn();
            int currWay1 = 0;
            if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay1 = knickebeinCurrentWaypoint[player];
            Point3d currPoint1 = currpoints[currWay1];
            string sector1 = Calcs.correctedSectorNameDoubleKeypad(mission, currPoint1);
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Resuming Knickebein waypoint #{0} to {1} ({2:N0},{3:N0})", new object[] { currWay1 + 1, sector1, Math.Round(currPoint1.x), Math.Round(currPoint1.y) });
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Make your heading {0:F0} degrees magnetic", new object[] { Math.Round(knickebeins[player].targetBearingAngle_magnetic_deg) }); //note the gplogserver IGNORES any formatting requests such as N0 or F0 . . . .

            return true;
        }

        int currWay = -1;        
        if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];
        int currWaySave = currWay;
        currWay++;        
        if (currWay > currpoints.Count - 1 ) currWay = 0; //Wraps around to 0 again after top of list
        //if (currWay < 0) currWay = currpoints.Count - 1; //Wraps around to top of list after 0
        //Don't do this here ^^^^ as we're using currWay == -1 to indicate no knickebein ever started yet

        knickebeinCurrentWaypoint[player] = currWay;
        Console.WriteLine("currWay: " + currWay.ToString());

        Point3d currPoint = currpoints[currWay];

        string sector = Calcs.correctedSectorNameDoubleKeypad(mission, currPoint);


        if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Starting Knickebein waypoint #{0} to {1} ({2:N0},{3:N0})", new object[] { currWay + 1, sector, Math.Round(currPoint.x), Math.Round(currPoint.y) });


        if (currWay != currWaySave)
        {
            if (knickebeins.ContainsKey(player))
            {
                knickebeins[player].turnOff();
                knickebeins[player] = null;
            }
            knickebeins[player] = new KnickebeinTarget(player, currPoint, mission);
        }

        knickebeins[player].turnOn();

        if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Make your heading {0:F0} degrees magnetic", new object[] { Math.Round(knickebeins[player].targetBearingAngle_magnetic_deg) }); //note the gplogserver IGNORES any formatting requests such as N0 or F0 . . . .
        return true;

    }

    public void KniInfo (Player player)
    {

        if (!knickebeins.ContainsKey(player))
        {
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: No Knickebein Waypoint is currently active", null);
            return;
        }


        int currWay = 0;
        if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];

        string initialSector = Calcs.correctedSectorNameDoubleKeypad(mission, knickebeins[player].initialPoint);
        string targetSector = Calcs.correctedSectorNameDoubleKeypad(mission, knickebeins[player].targetPoint);

        if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Current Knickebein: Waypoint #{0} magnetic bearing {1} degrees from {2} ({3:N0}/{4:N0})  to {5} ({6:N0}/{7:N0})", new object[] { currWay + 1, Math.Round(knickebeins[player].targetBearingAngle_magnetic_deg), initialSector, Math.Round(knickebeins[player].initialPoint.x), Math.Round(knickebeins[player].initialPoint.y), targetSector, Math.Round(knickebeins[player].targetPoint.x), Math.Round(knickebeins[player].targetPoint.y) });     
        
    }
    public Point3d KniPoint(Player player)
    {

        if (!knickebeins.ContainsKey(player) || !knickebeins[player].turnedOn)
        {
            //if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Knickebein: No Knickebein Waypoint is currently active", null);
            return new Point3d(-1,-1,-1);
        }


        //int currWay = 0;
        //if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];

        // string initialSector = Calcs.correctedSectorNameDoubleKeypad(mission, knickebeins[player].initialPoint);
        //string targetSector = Calcs.correctedSectorNameDoubleKeypad(mission, knickebeins[player].targetPoint);

        /*
        if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "Current Knickebein: Waypoint #{0} magnetic bearing {1} degrees from {2} ({3:N0}/{4:N0})  to {5} ({6:N0}/{7:N0})", new object[] { currWay + 1, Math.Round(knickebeins[player].targetBearingAngle_magnetic_deg), initialSector, Math.Round(knickebeins[player].initialPoint.x), Math.Round(knickebeins[player].initialPoint.y), targetSector, Math.Round(knickebeins[player].targetPoint.x), Math.Round(knickebeins[player].targetPoint.y) });
        */
        
        //Point3d currPoint = knickebeinWaypoints[player][currWay];
        //Point3d tp = knickebeins[player].targetPoint;
        //Console.WriteLine("KB: currPoint 2 ways: {0:F0} {1:F0} {2:F0} : {3:F0} {4:F0} {5:F0}  ", new object[] { currPoint.x, currPoint.y, currPoint.z, tp.x, tp.y, tp.z }) ;
        return knickebeins[player].targetPoint;

    }
    public bool KniNext(Player player)
    {
        return KniStart(player, false, true);
    }
    public bool KniPrev(Player player)
    {
        return KniStart(player, false, false, true);
    }
    public void KniClear(Player player)
    {
        if (player == null) return;
        if (knickebeins.ContainsKey(player))
        {
            knickebeins[player].turnOff();
            knickebeins.Remove(player);
        }
        if (knickebeinWaypoints.ContainsKey(player)) knickebeinWaypoints.Remove(player);
        if (knickebeinCurrentWaypoint.ContainsKey(player)) knickebeinCurrentWaypoint.Remove(player);
        if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, "All Knickebein Waypoints cleared!", new object[] { });
    }
    public string KniList(Player player, bool display = true, bool html = false )
    {
        string retmsg = "";
        string msg = "";
        string newline = Environment.NewLine;
        if (html) newline = "<br>" + Environment.NewLine;

        List<Point3d> currpoints = new List<Point3d>();
        if (knickebeinWaypoints.ContainsKey(player)) currpoints = knickebeinWaypoints[player];
        else
        {
            msg = "Knickebein: No waypoints entered yet";
            if (mission.GamePlay != null) mission.GamePlay.gpLogServer(new Player[] { player }, msg, new object[] { });
            return msg;
        }
        int currWay = 0;
        if (knickebeinCurrentWaypoint.ContainsKey(player)) currWay = knickebeinCurrentWaypoint[player];

        double bearing_deg = 0;
        if (knickebeins.ContainsKey(player)) bearing_deg = knickebeins[player].targetBearingAngle_deg;


        msg = ">>>>>Your current Knickebein Waypoints List";
        retmsg += msg + newline;
        mission.GamePlay.gpLogServer(new Player[] { player }, msg, new object[] { });
        //Point3d currPoint = currpoints[currWay];

        int ct = 0;
        foreach (Point3d currPoint in currpoints)
        {
            ct++;
            string sector = Calcs.correctedSectorNameDoubleKeypad(mission, currPoint);
            msg = string.Format("Waypoint #{0} to {1} ({2:F0}/{3:F0})", new object[] { ct, sector, currPoint.x, currPoint.y });
            retmsg += msg + newline;
            mission.GamePlay.gpLogServer(new Player[] { player }, msg, null);
        }
        msg = string.Format("Your currently active Knickebein Waypoint: #{0}; active Waypoint bearing {1:F0} degrees", new object[] { currWay + 1, bearing_deg });
        retmsg += msg + newline;
        mission.GamePlay.gpLogServer(new Player[] { player }, msg, null);

        return retmsg;

    }
    //GamePlay.gpLogServer(new Player[] { player }, "To use: <knistart <knistop <kninext <kniprev <knilist <knidel", null);


}

public class Mission : AMission, IKnickebeinMission
{
    public IMainMission TWCMainMission;
    public ISupplyMission TWCSupplyMission;
    public Random ran;
    public Knickebeinholder knickeb;
    public Mission()
    {
        TWCMainMission = TWCComms.Communicator.Instance.Main;

        TWCComms.Communicator.Instance.Knickebein = (IKnickebeinMission)this; //allows -stats.cs to access this instance of Mission                        

        //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
        ran = new Random();
        Console.WriteLine("-Knickebein.cs successfully inited");
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

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        TWCSupplyMission = TWCComms.Communicator.Instance.Supply;


        if (missionNumber == MissionNumber)
        {
            if (GamePlay is GameDef)
            {
                //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
            }

            knickeb = new Knickebeinholder(this);
            knickeb.KniTest();


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

    void Mission_EventChat(Player from, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 

        Player player = from as Player;
        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;

        string msg_orig = msg;
        msg = msg.ToLower();
        //Stb_Message(null, "Stats msg recvd.", null);

        /*
        if (msg.StartsWith("<!deban") && (admin_privilege_level(player) < 2))
        {

        }
        */

        if (msg.StartsWith("<kni"))

        {
            try
            {

                /*int parseL = Calcs.LastIndexOfAny(msgTrim, new string[] { " " });

                if (msgTrim.Length > 0 && parseL > -1)
                {*/
                /*List<string> sections = new List<string>();
                while (parseL > -1)
                {
                    sections.Add(msgTrim.Substring(parseL));
                    msgTrim = msgTrim.Substring(0, parseL);
                    parseL = Calcs.LastIndexOfAny(msgTrim, new string[] { " " });
                }
                sections.Add(msgTrim);
                */

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

                    if (angle_deg != 0 && distance != 0)
                    {
                        try
                        {
                            GamePlay.gpLogServer(new Player[] { player }, "Knickebein set up with bearing: " + angle_deg.ToString("N0") + " degrees Distance: " + distance.ToString("N0") + " " + units, null);
                            knickeb.startQuickKnickebein(player, angle_deg, distance);

                        }
                        catch (Exception ex) { Console.WriteLine("Knickebein Kni: " + ex.ToString()); }

                    }
                    else
                    {
                        GamePlay.gpLogServer(new Player[] { player }, "Knickebein setup ERROR. Knickebein not set up. Entered values as interpreted - angle: " + angle_deg.ToString("N0") + "  distance: " + distance.ToString("N0") + " " + units, null);
                        GamePlay.gpLogServer(new Player[] { player }, "To set up Knickebein, use chat command: <kni 90 25", null);
                        GamePlay.gpLogServer(new Player[] { player }, "This means set Knickebein target 90 degrees 25 " + units + " from your current position", null);
                    }
                }
                else
                {
                    GamePlay.gpLogServer(new Player[] { player }, "To set up Knickebein, use chat command: <kni 90 25", null);
                    GamePlay.gpLogServer(new Player[] { player }, "This means set Knickebein target 90 degrees 25 " + units + " from your current position", null);
                }
            }
            catch (Exception ex) { Console.WriteLine("Knickebein Kni: " + ex.ToString()); }
        }
        else if (msg.StartsWith("<ka")) //<kadd
        {
            try
            {
                string units = "km";
                if (player.Army() == 1) units = "miles";

                string newmsg = msg.Replace("<kadd", "").Replace("<ka", "").Replace(",", " ").Replace("(", " ").Replace(")", " ").Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Trim(); // remove the comma
                string[] words = newmsg.Split(' ');
                List<Point3d> points = new List<Point3d>();


                //Case of entering apoint like <kniadd 300000 100000
                if (words.Length == 2 && Calcs.isDigitOrPlusMinusPoint(words[0]) && Calcs.isDigitOrPlusMinusPoint(words[1]))

                {
                    double x = 0;
                    double y = 0;
                    try { if (words[0].Length > 0) x = Convert.ToDouble(words[0]); }
                    catch (Exception ex) { }
                    try { if (words[1].Length > 0) y = Convert.ToDouble(words[1]); }
                    catch (Exception ex) { }

                    if (x != 0 && y != 0)
                    {
                        points.Add(new Point3d(x, y, 0));
                    }
                }
                //Case of entering doublekeypad sectors, possibly in a row like <kniadd AA30.3.2 BA30.2.1 BD9.2
                else if (words.Length > 0)
                {
                    foreach (string word in words)
                    {
                        if (word.StartsWith("<ka")) continue;
                        Point3d point = Calcs.sectordoublekeypad2point(word.Trim().ToUpper());
                        if (point.x == 0 && point.y == 0) continue;
                        points.Add(point);
                    }
                }
                if (points.Count > 0)
                {
                    foreach (Point3d point in points)
                    {
                        if (point.x == 0 && point.y == 0) continue;
                        //Knickebeins.startQuickKnickebein(Player player, double angle_deg, double distance);
                        int wp = knickeb.KniAdd(player, point);
                        GamePlay.gpLogServer(new Player[] { player }, "Knickebein Waypoint #{0} at {1} added to your flight plan ({2:N0},{3:N0})", new object[] { wp, Calcs.correctedSectorNameDoubleKeypad(this, point), Math.Round(point.x), Math.Round(point.y) });
                    }
                    GamePlay.gpLogServer(new Player[] { player }, "To use: Tab-4-4-4-4 menu OR <kstart <knext <kprev <koff <kon <kclear <khelp", null);
                }
                else
                {

                    //They tried to enter something but it didn't work somehow
                    if (words.Length > 1)
                    {
                        GamePlay.gpLogServer(new Player[] { player }, "Knickebein setup entry ERROR. Knickebein waypointsnot set up. Please check <khelp.", null);
                    }

                    //They just entered <kadd to find out how it works
                    GamePlay.gpLogServer(new Player[] { player }, "To set up Knickebein waypoints, use chat command like: <kadd AB21.2.1 BE9.4.9 BA14.2.1", null);
                    GamePlay.gpLogServer(new Player[] { player }, "This means add those map sector points to your Knickebein Waypoints List.", null);
                    GamePlay.gpLogServer(new Player[] { player }, "You can add one or more points at a time. Formats BA14, AC9.2, AZ.2.6 all work. ", null);
                    GamePlay.gpLogServer(new Player[] { player }, "For higher accuracy waypoints, use x/y map coordinates: <kadd 142381 150321", null);
                    GamePlay.gpLogServer(new Player[] { player }, "Then to use: Tab-4-4-4-4 menu OR <kstart <knext <kprev <koff <kon <kclear <khelp", null);
                }
            }
            catch (Exception ex) { Console.WriteLine("Knickebein Kadd: " + ex.ToString()); }
        }
        else if (msg.StartsWith("<kn")) //<knext
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for Knickebein commands, when possible", null);
            knickeb.KniNext(player);
        }
        else if (msg.StartsWith("<kp")) //<kprev
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for Knickebein commands, when possible", null);
            knickeb.KniPrev(player);
        }
        else if (msg.StartsWith("<kl")) //<klist
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for Knickebein commands, when possible", null);
            knickeb.KniList(player);
        }


        else if (msg.StartsWith("<kstop") || msg.StartsWith("<koff")) //<kstop
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for Knickebein commands, when possible", null);
            knickeb.KniStop(player);
        }
        else if (msg.StartsWith("<kon")) //<kon
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for Knickebein commands, when possible", null);
            knickeb.KniStart(player, contin: true);
        }
        else if (msg.StartsWith("<kz")) //<kon
        {

            knickeb.KniOnStartOrNext(player);
        }
        else if (msg.StartsWith("<ki")) //<kinfo
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Please use Tab-4-4-4-4 menu for Knickebein commands, when possible", null);
            knickeb.KniInfo(player);
        }
        else if (msg.StartsWith("<kd")) //<kdel
        {
            string[] words = msg.Split(' ');
            int wp = 0;
            if (words.Length > 1)
            {
                try { if (words[1].Length > 0) wp = Convert.ToInt32(words[1]); }
                catch (Exception ex) { }

                if (wp > 0)
                {
                    GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Deleting Waypoint #{0}", new object[] { wp });
                    knickeb.KniDelete(player, wp);
                }
                else
                {
                    GamePlay.gpLogServer(new Player[] { player }, "I didn't understand your <kdel command {0}; no waypoints deleted.", new object[] { words[1] });
                    GamePlay.gpLogServer(new Player[] { player }, "Example: <kdel 3  - deletes Waypoint #3", new object[] { words[1] });
                    GamePlay.gpLogServer(new Player[] { player }, "<khelp for help", null);
                }
            }
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, "I didn't understand your <kdel command; no waypoints deleted.", null);
                GamePlay.gpLogServer(new Player[] { player }, "<khelp for help", null);
            }
        }
        else if (msg.StartsWith("<kc")) //<kclear
        {
            knickeb.KniClear(player);
        }
        else if (msg.StartsWith("<khelp6") || msg.StartsWith("<kh6"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 6/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "Display looks like \"---- ||||||\" OR \"++++++ ||||||||\"", null);
            GamePlay.gpLogServer(new Player[] { player }, "OR \"= ....\" OR \"= *\"", null);
            GamePlay.gpLogServer(new Player[] { player }, "Each + or - is 100 meters left/right of the beam center (=)", null);
            GamePlay.gpLogServer(new Player[] { player }, "Each | or . represents 1km or 100m from target, max 10 km shown (*narrow* beam)", null);
            GamePlay.gpLogServer(new Player[] { player }, "At \"= *\" you are on target within a 100 meter square, the approx. accuracy of the historic Knickebein system.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Can you \"ride the beam\" and drop your bombs accurately on target at dusk or at night, as historic WWII-era pilots did?", null);
            GamePlay.gpLogServer(new Player[] { player }, "<<<End Knickebein system help>>>", null);

        }
        else if (msg.StartsWith("<khelp5") || msg.StartsWith("<kh5"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 5/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "Knickebein used one narrow beam to keep pilots on course to target and another beam crossing the first to indicate when target was reached.", null);
            GamePlay.gpLogServer(new Player[] { player }, "On your display, the directional beam is shown via symbols + (turn right), - (turn left), or = (you're right on).", null);
            GamePlay.gpLogServer(new Player[] { player }, "The second beam (distance to target) is shown via symbols | (1 km) or . (100 meters)", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp6 for more", null);
        }
        else if (msg.StartsWith("<khelp4") || msg.StartsWith("<kh4"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 4/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kstart <knext <kprev will all vector you from wherever you currently are", null);
            GamePlay.gpLogServer(new Player[] { player }, "to the given waypoint. So you can <kadd a large number of potential", null);
            GamePlay.gpLogServer(new Player[] { player }, "targets before takeoff, then during flight <klist <knext <kprev to select ", null);
            GamePlay.gpLogServer(new Player[] { player }, "the one you want, and vector direct from where you are to it.", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp5 for more", null);
        }
        else if (msg.StartsWith("<khelp3") || msg.StartsWith("<kh3"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 3/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "Command detail:  <kstart 4 or <k 4 - starts Knickebein @ waypoint #4 (#1 if none given),", null);
            GamePlay.gpLogServer(new Player[] { player }, "<knext - skip to next waypoint, <kprev - back to previous waypoint,  <koff - display off,", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kon - display back on, <klist - list waypoints,  <kdel 4 - delete waypoint #4 ", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kclear - clear all waypoints, <kinfo - info abt current waypoint", null);
            GamePlay.gpLogServer(new Player[] { player }, "Most commands can be abbreviated to 2 letters, so <ks, <kn, <kp <kl, <kd, <kc, <ki, <kh etc", null);
            GamePlay.gpLogServer(new Player[] { player }, "Main commands accessed via Tab-4-4-4-4 menu to avoid cluttering Chat", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp4 for more", null);
        }
        else if (msg.StartsWith("<khelp2") || msg.StartsWith("<kh2"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 2/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kadd: Formats like BA14 AC9.2 AZ.2.6 all work. Add 1-10 waypoints at a time.", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kadd 200311 192300 adds an exact map coordinate (x,y) point; get coordinates from TWC radar web site or Full Mission Builder", null);
            GamePlay.gpLogServer(new Player[] { player }, "Main Knickebein commands on Tab-4-4-4-4 menu OR Chat Commands <kstart <knext <kprev <koff <kon <kclear", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp3 for more", null);
        }
        else if (msg.StartsWith("<khelp") || msg.StartsWith("<kh"))
        {
            string units = "km";
            if (player.Army() == 1) units = "miles";

            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN SIMPLE OPERATION", null);
            GamePlay.gpLogServer(new Player[] { player }, "To set up Knickebein, use chat command: <kni 90 25", null);
            GamePlay.gpLogServer(new Player[] { player }, "This means set Knickebein target 90 degrees 25 " + units + " from your current position", null);

            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION", null);
            GamePlay.gpLogServer(new Player[] { player }, "To set up Knickebein waypoints: <kadd AW23.2.8 AY26.1.2 BB21.1.8 (etc)", null);
            GamePlay.gpLogServer(new Player[] { player }, "Then Tab-4-4-4-4 menu to start, stop, move to next on list, etc", null);
            GamePlay.gpLogServer(new Player[] { player }, "OR use Chat Commands like: <kstart - (reach 1st waypoint) - <knext - (reach 2nd wp) ", null);
            GamePlay.gpLogServer(new Player[] { player }, "<knext - (reach 3rd wp) - <koff", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp2 for more . . . ", null);

        }


        else if (msg.StartsWith("<ks") || msg.StartsWith("<k")) //<kstart
        {
            //knickeb.KniStart(player);
            //Calcs.isDigit(words[1])
            //sector = sector.Replace(",", ""); // remove the comma

            //Ok, this is tricky.  If we want to use "<k" as a shortcut it can only be like "<k" or "<k3" or "<k 3" because otherwise 
            //we start matching all kinds of things like "<ko" or "<km" or whatever, that might means something       

            if (msg.StartsWith("<k") && !((msg.Trim() == "<k") || (msg.StartsWith("<ks")) || (msg.StartsWith("<k ")) || Calcs.isDigit(msg.Replace("<k", "").Trim()))) return;

            string wp_string = msg.Replace("<kstart ", "").Replace("<kstart", "").Replace("<ks", "").Replace("<k", "").Trim();
            //string[] words = msg.Split(' ');
            int wp = 0;
            if (Calcs.isDigit(wp_string))
            {
                try { if (wp_string.Length > 0) wp = Convert.ToInt32(wp_string); }
                catch (Exception ex) { }

                if (wp > 0)
                {
                    GamePlay.gpLogServer(new Player[] { player }, "Knickebein: Starting at waypoint #{0}", new object[] { wp });
                    knickeb.KniStart(player, startWay_onebased: wp);
                }
                else
                {
                    knickeb.KniStart(player);
                }
            }
            else
            {
                knickeb.KniStart(player);
            }
        }
        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1.5; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.2;

            string msg41 = "<kni simple Knickebein; <kadd advanced Knickebein; <khelp Knickebein help";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
    }
    public void KniOnStartOrNext(Player player)
    {
        knickeb.KniOnStartOrNext(player);
    }
    public void KniNext(Player player) {
        knickeb.KniNext(player);
    }
    public void KniPrev(Player player)
    {
        knickeb.KniPrev(player);
    }
    public void KniList(Player player)
    {
        knickeb.KniList(player);
    }
    public void KniStop(Player player)
    {
        knickeb.KniStop(player);
    }
    public void KniStart(Player player)
    {
        knickeb.KniStart(player, contin: true);
    }
    public void KniInfo(Player player)
    {
        knickeb.KniInfo(player);
    }
    public Point3d KniPoint(Player player)
    {
        return knickeb.KniPoint(player);
    }
    public void KniDelete(Player player, int waypointToDelete)
    {
        knickeb.KniDelete(player, waypointToDelete);
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
        if (newch.Count >2) //If  more than 2 letters we only accept the right-most (least significant) two & just ignore the rest
        {
            newch[0] = newch[newch.Count - 2];
            newch[1] = newch[newch.Count - 1];
        }
        int total = 10000; //AA represents point 10000 - if map changes we'll have to change this
        //if (ch[0] == 'A') total += 0;
        //else if (ch[0] == 'B') total += 260000;
        int val0 = (int)(newch[0]);
        total += (val0-65)*260000;

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
        return new Point3d((xK * size)/3, (yK * size)/3, 0); //div by 3 because we end up with a number 0-2 and the range (0-3) should be the full size.  If we dont' /3 then we get 3x the range we really want
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



}
