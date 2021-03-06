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


public class AIRadarTarget
{
    public AiAircraft aircraft { get; set;}
    public AiActor actor { get; set;}
    public Point3d aircraftPos { get; set;}
    public Player player { get; set;}
    public AiActor place { get; set;}
    public Point3d playerPos { get; set;}
    public double targetBearingAngle_deg { get; set;} //angle from player to target in bearing    
    public double targetHeightAngle_deg { get; set;} //angle from player in altitude/height
    public double targetDistance_m { get; set;} //distance from player to target in km

    public double XDist_m { get; set;} //distance front/back of target from player direction
    public double YDist_m { get; private set; } //distance left/right of target from player direction
    public double ZDist_m { get; set;} //altitude distance player to target

    public string DirectionPip { get; set;}
    public string AltitudePip { get; set;}
    public string DistancePip { get; set;}    

    public bool inRange { get; set;}
    public bool turnedOn { get; set;}

    AMission mission { set; get; }

    public AIRadarTarget(Player player, AiAircraft aircraft, AMission mission)
    {
        this.player = player;
        this.aircraft = aircraft;
        this.actor = aircraft as AiActor;
        this.player = player;
        this.place = player.Place();        
        this.mission = mission;
        this.recalculate();
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

    //regets player & target aircraft position & then recalculates the angles & pips
    public bool recalculate()
    {
        this.aircraftPos = actor.Pos();
        this.playerPos = place.Pos();

        this.targetDistance_m = Calcs.CalculatePointDistance(this.playerPos, this.aircraftPos);

        this.targetBearingAngle_deg = Calcs.CalculateGradientAngle(this.playerPos, this.aircraftPos);
        if (targetBearingAngle_deg > 180) targetBearingAngle_deg -= 360;
        if (targetBearingAngle_deg < -180) targetBearingAngle_deg += 360;
        this.XDist_m = targetDistance_m * Math.Cos(Calcs.DegreesToRadians(targetBearingAngle_deg));
        this.YDist_m = targetDistance_m * Math.Sin(Calcs.DegreesToRadians(targetBearingAngle_deg));
        this.ZDist_m = aircraftPos.z - playerPos.z;

        this.targetHeightAngle_deg = Calcs.CalculatePitchDegree(this.playerPos, aircraftPos);
        if (targetHeightAngle_deg > 180) targetHeightAngle_deg -= 360;
        if (targetHeightAngle_deg < -180) targetHeightAngle_deg += 360;

        
        return this.calculatePips();
    }

    private bool calculatePips() {

        inRange = true;
        checkInScope();
        calcDistancePip();
        calcDirectionPip();
        calcAltitudePip();
        if (!inRange) setOutOfRangePips();
        return inRange;
    }

    //Does the target blip lie within the circle of the radar scope?  If not, it's not in Range.
    //The circle of the radar scope covered a circle of radius approx 4000m, centered at 4000m in front of the aircraft.
    private void checkInScope()
    {
        if (actor == null)
        {
            inRange = false;
            return;
        }

        if (Math.Pow(XDist_m - 4000, 2) + Math.Pow(YDist_m, 2) > 16000000 
        || (Math.Pow(XDist_m - 4000, 2) + Math.Pow(ZDist_m, 2) > 16000000)
        )
        inRange = false; //4000^2
    }

    private void calcDistancePip()
    {

        //max range of the AI was: ~25000ft, OR the altitude the aircraft was flying (ie if flying at 10000ft it couldn't see more than 10000ft forward)
        //min range was 400 feet - closer than that the image merged with the transmission pulse (ie, the radar aircraft)
        if (this.targetDistance_m >  7500 || this.targetDistance_m > playerPos.z || this.targetDistance_m < 133)
        {
            inRange = false;            
            return;
        }

        char c = '|';
        string ret = new string(c, Convert.ToInt32(Math.Round(Math.Abs(targetDistance_m / 800))));
        string pad = new string(' ', 12 - ret.Length);
        DistancePip = pad + ret;


    }

    private void calcDirectionPip()
    {
        if ( 
               (targetBearingAngle_deg < -90 || targetBearingAngle_deg > 90) 
           )
        {
            inRange = false;            
            return;
        }

        if (Math.Abs(YDist_m) <= 400)
        {
            DirectionPip = new string(' ', 11) + "=";
            return;
        }

        char c = '-';
        if (YDist_m > 0) c = '+';
        string ret = new string(c,Convert.ToInt32( Math.Round(Math.Abs(YDist_m/400))));
        string pad = new string(' ', 12 - ret.Length);
        DirectionPip = pad + ret;
    }

    private void calcAltitudePip()
    {
        if (targetHeightAngle_deg < -90 || targetHeightAngle_deg > 90)
        {
            inRange = false;
            return;
        }

        char c = '-';
        if (ZDist_m > 0) c = '+';
        string ret = new string(c, Convert.ToInt32(Math.Round(Math.Abs(ZDist_m / 400))));
        string pad = new string(' ', 12 - ret.Length);
        AltitudePip = ret + pad;
    }

    private void setOutOfRangePips()
    {
        DistancePip = new string(' ', 6) + "?" + new string(' ', 6);
        AltitudePip = new string(' ', 6) + "?" + new string(' ', 6);
        DirectionPip = new string(' ', 6) + "?" + new string(' ', 6); 
    }


    public double displayPips()
    {
        if (actor == null) return 0;
        calculatePips();
        //Tuple<double, string> distT = new Tuple<double, string>(targetDistance_m, sbyte);
        string disp = DirectionPip + " " + DistancePip + " " + AltitudePip;
        disp += " " + (XDist_m / 1000.0).ToString("F1") + " " + (YDist_m / 1000.0).ToString("F1") + " " + (ZDist_m / 1000.0).ToString("F1");
        if (mission.GamePlay != null) mission.GamePlay.gpHUDLogCenter(new Player[] { player }, disp);
        return targetDistance_m;
    }

    private void display_recurs()
    {
        if (!this.turnedOn) return;
        double t = 5.3;
        //double dist = displayPips();
        displayPips();
        if (targetDistance_m<5000) t = targetDistance_m/10000*5; //we update the display more frequently when the player is near the target aircraft
        if (t < 1) t = 1;//Not sure what the frequency of display update was on the real radar units, we'll say one second refresh at best?
        mission.Timeout(t, () => display_recurs());
        //knickebeins[player] = new KnickebeinTarget(player, 123, 23, this);        
    }
}




public class AIRadarMission : AMission
{
    //public IMainMission TWCMainMission;
    //public ISupplyMission TWCSupplyMission;
    public Random ran;
    
    public AIRadarMission()
    {
        //TWCMainMission = TWCComms.Communicator.Instance.Main;

        //TWCComms.Communicator.Instance.Knickebein = (IKnickebeinMission)this; //allows -stats.cs to access this instance of Mission                        

        //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
        ran = new Random();
        Console.WriteLine("-AerialInterceptRadar.cs successfully inited");
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

        //TWCSupplyMission = TWCComms.Communicator.Instance.Supply;


        if (missionNumber == MissionNumber)
        {
            if (GamePlay is GameDef)
            {
                //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
            }

            //knickeb = new Knickebeinholder(this);
            //knickeb.KniTest();

            Console.WriteLine("-AerialInterceptRadar.cs - onMissionLoaded");


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
    

    Dictionary<Player,Tuple<AIRadarTarget, int>> PlayerCurrentAIRadarTargetandACnum = new Dictionary<Player, Tuple<AIRadarTarget, int>>();
    
    void Mission_EventChat(Player player, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 

        //Player player = from as Player;
        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;

        string msg_orig = msg;
        msg = msg.ToLower();

        if (msg.StartsWith("<an"))

        {
            try
            {
                if (player == null || player.Place() == null)
                {
                    GamePlay.gpLogServer(new Player[] { player }, "AIRadar: Can't help if you're not in an aircraft, sorry.", new object[] { });
                    //Here we can restrict this to certain aircraft etc.
                    return;
                }
                var playerAirGroup = player.Place().Group() as AiAirGroup;

                if (playerAirGroup == null) return;

                int acNum = 0;
                AIRadarTarget AIRT = null;

                if (PlayerCurrentAIRadarTargetandACnum.ContainsKey(player))
                {
                    //PlayerCurrentAC[player]++;
                    var tup = PlayerCurrentAIRadarTargetandACnum[player];
                    acNum = tup.Item2 + 1; //advance to the next ac #
                    AIRT = tup.Item1;
                } 
                var aircraftList = Calcs.AllAircraftNearSorted(this, player.Place().Pos(), playerAirGroup.Vwld(), 4000, 4000);
                if (aircraftList == null || aircraftList.Count == 0)
                {
                    GamePlay.gpLogServer(new Player[] { player }, "AIRadar: No aircraft nearby to track, sorry.", new object[] { });
                    return;
                }
                else
                {
                    if (AIRT != null && acNum < aircraftList.Count && aircraftList.Keys[acNum] == AIRT.aircraft) acNum++; //we're trying to skip past the current AC, so if we have hit it again, we advance the acNum

                    if (acNum >= aircraftList.Count) //means, turn OFF the AIRadar
                    {
                        if (AIRT != null) AIRT.turnOff();                                               
                        //PlayerCurrentAIRadarTargetandACnum[player] = new Tuple<AIRadarTarget, int>(AIRT, 0);
                        GamePlay.gpLogServer(new Player[] { player }, "AIRadar: No targets or past last target - radar off", new object[] { });
                        if (PlayerCurrentAIRadarTargetandACnum.ContainsKey(player)) PlayerCurrentAIRadarTargetandACnum.Remove(player);
                        return;

                    } else { //means, turn ON the AIRadar on this target #
                        AIRT = new AIRadarTarget(player, aircraftList.Keys[acNum], this);
                        AIRT.turnOn();
                        PlayerCurrentAIRadarTargetandACnum.Add(player, new Tuple<AIRadarTarget, int>(AIRT, acNum));
                        GamePlay.gpLogServer(new Player[] { player }, "AIRadar: Radar on - changed to next target", new object[] { });
                    }
                    
                }
            }
            catch (Exception ex) { Console.WriteLine("AIRadar: " + ex.ToString()); }
        }
        
        else if (msg.StartsWith("<ahelp6") || msg.StartsWith("<kh6"))
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
        else if (msg.StartsWith("<ahelp5") || msg.StartsWith("<kh5"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 5/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "Knickebein used one narrow beam to keep pilots on course to target and another beam crossing the first to indicate when target was reached.", null);
            GamePlay.gpLogServer(new Player[] { player }, "On your display, the directional beam is shown via symbols + (turn right), - (turn left), or = (you're right on).", null);
            GamePlay.gpLogServer(new Player[] { player }, "The second beam (distance to target) is shown via symbols | (1 km) or . (100 meters)", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp6 for more", null);
        }
        else if (msg.StartsWith("<ahelp4") || msg.StartsWith("<kh4"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 4/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kstart <knext <kprev will all vector you from wherever you currently are", null);
            GamePlay.gpLogServer(new Player[] { player }, "to the given waypoint. So you can <kadd a large number of potential", null);
            GamePlay.gpLogServer(new Player[] { player }, "targets before takeoff, then during flight <klist <knext <kprev to select ", null);
            GamePlay.gpLogServer(new Player[] { player }, "the one you want, and vector direct from where you are to it.", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp5 for more", null);
        }
        else if (msg.StartsWith("<ahelp3") || msg.StartsWith("<kh3"))
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
        else if (msg.StartsWith("<ahelp2") || msg.StartsWith("<kh2"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>KNICKEBEIN ADVANCED OPERATION (part 2/6)", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kadd: Formats like BA14 AC9.2 AZ.2.6 all work. Add 1-10 waypoints at a time.", null);
            GamePlay.gpLogServer(new Player[] { player }, "<kadd 200311 192300 adds an exact map coordinate (x,y) point; get coordinates from TWC radar web site or Full Mission Builder", null);
            GamePlay.gpLogServer(new Player[] { player }, "Main Knickebein commands on Tab-4-4-4-4 menu OR Chat Commands <kstart <knext <kprev <koff <kon <kclear", null);
            GamePlay.gpLogServer(new Player[] { player }, "<khelp3 for more", null);
        }
        else if (msg.StartsWith("<ahelp") || msg.StartsWith("<kh"))
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


        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1.6; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.2;

            string msg41 = "<as Start Aerial Intercept Radar on closest target; <ahelp AIRadar help";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
        
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

    //Pitch angle, starting from p1 and going to p2
    public static double CalculatePitchDegree(Point3d p1, Point3d p2)
    {
        Vector3d v = new Vector3d(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z);
        return CalculatePitchDegree(v);
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

    public static string correctedSectorNameDoubleKeypad(AIRadarMission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + doubleKeypad(p);
        return s;

    }

    public static string correctedSectorNameKeypad(AIRadarMission msn, Point3d p)
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

    public static string correctedSectorName(AIRadarMission msn, Point3d p)
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

    //so this figures all aircraft in a circle of radius_m that is in front of the given position by distance_m. "In front of" defined by
    //the vector Vwld.  Sorted by DISTANCE from point pos.
    public static SortedList<AiAircraft, double> AllAircraftNearSorted(AMission msn, Point3d pos, Vector3d Vwld, double distance_m, double radius_m)
    {
        double dist = distance(Vwld.x, Vwld.y);

        Point3d point2 = pos; //if current velocity = 0.

        if (dist > 0) {

            point2 = new Point3d(Vwld.x / dist * distance_m, Vwld.y / dist * distance_m, pos.z);
        }

        var alist = AllAircraftNear(msn, point2, radius_m);
        var retlist = new SortedList<AiAircraft, double>();

        foreach (AiAircraft a in alist)
        {
            double d = CalculatePointDistance(pos, (a as AiActor).Pos());
            retlist.Add(a, d);
        }
        return retlist;
    }

    public static List<AiAircraft> AllAircraftNear(AMission msn, Point3d pos, double radius_m)
    {
        var ret = new List<AiAircraft>();

        var allAc = AllAircraftInGame(msn);
        foreach (AiAircraft a in allAc)
        {
            if (CalculatePointDistance((a as AiActor).Pos(), pos) <= radius_m ) ret.Add(a);
        }

        return ret;
    }

    public static List<AiAircraft> AllAircraftInGame(AMission msn)
    {
        var ret = new List<AiAircraft>();

        if (msn.GamePlay.gpArmies() != null && msn.GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in msn.GamePlay.gpArmies())
            {
                if (msn.GamePlay.gpAirGroups(army) != null && msn.GamePlay.gpAirGroups(army).Length > 0)
                    foreach (AiAirGroup airGroup in msn.GamePlay.gpAirGroups(army))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            //if (DEBUG) DebugAndLog ("DEBUG: Army, # in airgroup:" + army.ToString() + " " + airGroup.GetItems().Length.ToString());            
                            if (airGroup.GetItems().Length > 0) foreach (AiActor actor in airGroup.GetItems())
                                {
                                    if (actor != null && (actor as AiAircraft != null))
                                    {
                                        ret.Add(actor as AiAircraft);
                                    }

                                }
                        }
                    }
            }

        }
        return ret;
    }

    public static string randSTR(string[] strings)
    {
        //Random clc_random = new Random();
        return strings[clc_random.Next(strings.Length)];
    }

    public static void loadSmokeOrFire(maddox.game.IGamePlay GamePlay, AIRadarMission mission, double x, double y, double z, string type, double duration_s = 300, string path = "")
    {


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

        //AMission mission = GamePlay as AMission;
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";
        string key = "Static1";
        string value = "Smoke.Environment." + type + " nn " + x.ToString("0.00") + " " + y.ToString("0.00") + " " + (duration_s / 60).ToString("0.0") + " /height " + z.ToString("0.00");
        f.add(sect, key, value);


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

