#define DEBUG  
#define TRACE  
//$reference System.Core.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference parts/core/CloDMissionCommunicator.dll
using System;
using System.Globalization;
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
using System.Media;

/*   MAKE LANDING GROUND
 *   
 *   Pilots can make a temporary Landing Ground by landing planes and chat command <makelg
 * 
 * ******************************************/


public class LandingGroundMission : AMission
{
    Mission mainmission;
    Random ran;
    int MissionNumberListener;

    public LandingGroundMission(Mission msn)
    {
        try
        {
            mainmission = msn; //getting instance of mainmission via constructor
            ran = new Random();

            MissionNumberListener = -1;


            Console.WriteLine("-MakeLandingGround.cs successfully constructed");
        }
        catch (Exception ex) { Console.WriteLine("MakeLandingGround() ERROR: " + ex.ToString()); }
    }

    public override void Init(ABattle b, int missionNumber)
    {
        try
        {
            base.Init(b, missionNumber);

            MissionNumberListener = -1;
            Console.WriteLine("-MakeLandingGround.cs successfully inited");

        }
        catch (Exception ex) { Console.WriteLine("MakeLandingGround Init() ERROR: " + ex.ToString()); }
    }



    int stb_lastMissionLoaded = -1;

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        try
        {
            //Console.WriteLine("-cover.cs OnMissionLoaded {0} {1} ", missionNumber, MissionNumber);

            stb_lastMissionLoaded = missionNumber;


            if (missionNumber == MissionNumber)
            {

            }
        }
        catch (Exception ex) { Console.WriteLine("MakeLandingGround OnMissionLoaded() ERROR: " + ex.ToString()); }
    }

    //Checks if the LG is one of the listed mission objectives; also gives points etc if so (sends to MO_DestroyObjective())
    public string checkIfTempLandingGroundIsMissionObjective(Point3d pos, int army, HashSet<string> planes)
    {
        foreach (Mission.MissionObjective mo in mainmission.MissionObjectivesList.Values)
        {

            //mainmission.MissionObjective mo
            if (mo.MOTriggerType != Mission.MO_TriggerType.TemporaryLandingGround) continue;

            double dist_m = Calcs.CalculatePointDistance(pos, mo.Pos);

            if (dist_m > 1500) continue;
            if (army != mo.OwnerArmy) continue;

            if (mo.TimeToUndestroy_UTC.HasValue && mo.TimeToUndestroy_UTC.Value.CompareTo(DateTime.UtcNow) > 0) continue; //The tempLG has already been made and is still active; can't re-make it

            //It IS on the objectives list!
            //We'll say temp landing grounds last for 3 days
            mo.BirthplaceACTypes = planes;
            mainmission.MO_DestroyObjective(mo.ID); //This will set the OBJECTIVE as 'complete' (=destroyed) and keep the new LG alive for the time amount specified in the creation of the MissionObjective.  Could be a few days, whatever.            
            return mo.Name;

            //renewTempLandingGround(mo.Pos, mo.OwnerArmy, mo.BirthplaceACTypes, mo.AirfieldName);
        }

        return "";

    }

    //We can just run all MOs through this routine, say at the start of a mission, and it will re-constitute any LGs that are still alive
    public void renewTempLandingGround(Mission.MissionObjective mo)
    {
        Console.WriteLine("renewTempLandingGround: Starting to renew " + mo.ID + " " + mo.Name);
        if (mo.MOTriggerType == null ||  mo.MOTriggerType != Mission.MO_TriggerType.TemporaryLandingGround )
        {
            Console.WriteLine("renewTempLandingGround PROBLEM: Objective was not triggertype TemporaryLandingGround " + mo.ID);
            return;
        }

        //mo.TimeToUndestroy_UTC.HasValue && mo.TimeToUndestroy_UTC.Value.CompareTo(DateTime.UtcNow) > 0

        if (!mo.TimeToUndestroy_UTC.HasValue ||  mo.TimeToUndestroy_UTC.Value.CompareTo(DateTime.UtcNow) <= 0)
        {
            Console.WriteLine("renewTempLandingGround: Objective's time has expired, so the Landing Ground was not created, for " + mo.ID);
            return;
        }

        if (mo.BirthplaceACTypes != null) Console.WriteLine("TempLG: PLaneset way before {0} " + mo.BirthplaceACTypes.ToString(), mo.BirthplaceACTypes.Count);

        HashSet<string> planeSet = new HashSet<string>(); //The routines below alter planeSet in various ways & we don't necessarily want that to apply to mo.BirthplacACTypes.  So we make a fresh copy.
                                                                              //(HashSet is a reference type, so anything that happens in the functions changes the original, too)

        if (mo.BirthplaceACTypes != null) planeSet = new HashSet<string>(mo.BirthplaceACTypes); //The routines below alter planeSet in various ways & we don't necessarily8 want that to apply to mo.BirthplacACTypes.  So we make a fresh copy.

        renewTempLandingGround(mo.Pos, mo.OwnerArmy, planeSet, mo.Name);

    }

    //Make a temporary landing ground spawn point
    /*
     * TODO:
     *   * Require 2-3-4 or whatever planes landed in the area
     *   * Check that planes are un damaged or have less than some threshhold of damage
     *   * Check that it is at or near an actual LG?
     *   * Probably need to add airdrome points, runway points or ?  Perhaps  only if not on an LG?
     *   
     */
    public void renewTempLandingGround(Point3d pos, int army, HashSet<string> planeSet, string name)
    {
        
        AiAirport ap = nearestAirportWithNoSpawn(pos, army: 0, isSeaplane: false, checkNoSpawn: false); //Get nearest airport, REGARDLESS of spawn point existing.  Get airports of both armies; we'll have to check that later.

        Point3d apPos = (ap as AiActor).Pos();

        double nearestAirfield_dist_m = Calcs.CalculatePointDistance(pos, (ap as AiActor).Pos());

        int terr = GamePlay.gpFrontArmy(apPos.x, apPos.y);

        if (nearestAirfield_dist_m > 1500  || ( terr != 0 && terr != army) ) //reject if nearest ap is too far away OR the wrong army (neutral ground is OK)
        {
            Console.WriteLine("PROBLEM: Couldn't RENEW temporary Landing Ground in sector " + Calcs.correctedSectorName(this, pos) + " distance: {0} ownerarmy: {1} territory army: {2}", new object[] {nearestAirfield_dist_m, army, terr });
        }        

        ISectionFile f = GamePlay.gpCreateSectionFile();

        f = CreateBirthPlace(f, apPos.x, apPos.y, 0, army, planeSet, name: name);

        GamePlay.gpPostMissionLoad(f);

        Console.WriteLine("RENEWED temporary Landing Ground {0} in sector " + Calcs.correctedSectorName(this, apPos), new object[] { name });
        string saveName = "makeLG.txt";
        try
        {
            if (saveName != null)
            {
                string sn = mainmission.CLOD_PATH + mainmission.FILE_PATH + "/sectionfiles" + "/" + saveName;
                Console.WriteLine("Saving section file to " + sn);
                f.save(sn); //testing
            }

        }
        catch (Exception ex) { Console.WriteLine("renewTempLandingGround ERROR: " + ex.ToString()); }

    }


    //Make a temporary landing ground spawn point
    /*
     * TODO:
     *   * Require 2-3-4 or whatever planes landed in the area
     *   * Check that planes are un damaged or have less than some threshhold of damage
     *   * Check that it is at or near an actual LG?
     *   * Probably need to add airdrome points, runway points or ?  Perhaps  only if not on an LG?
     *   
     */
    public void createTempLandingGround(Player player, bool testing = false)
    {
        if (player == null || player.Place() == null || player.Army() == null)
        {
            mainmission.twcLogServer(new Player[] { player }, "Can't make a landing ground - no player or aircraft.", null);
            return;
        }
        AiAircraft aircraft = player.Place() as AiAircraft;
        Point3d pos = aircraft.Pos();
        int terr = GamePlay.gpFrontArmy(player.Place().Pos().x, player.Place().Pos().y);
        double vel_mps = Calcs.CalculatePointDistance((player.Place() as AiAircraft).AirGroup().Vwld());

        int playerDamages = StatCalcs.listDamages(GamePlay, player, bShowMessages: true);

        AiAirport ap = nearestAirportWithNoSpawn(pos, army: 0, isSeaplane: false);

        if (testing)
        {
            pos = (ap as AiActor).Pos();
            vel_mps = 0;
        }

        //double nearestAirfield_dist_m = mainmission.covermission.Stb_nearestAirport_distance_m(pos, army: 0, isSeaplane: false);

        double nearestAirfield_dist_m = Calcs.CalculatePointDistance(pos, (ap as AiActor).Pos());

        if (testing ||
                    (player.Place() != null && aircraft != null &&
                      (terr == player.Army() || terr == 0) &&   //OWN territory OR neutral territory
                                                                //Stb_distanceToNearestAirport(actor) < 3100 &&
                    vel_mps < 2 &&  //stopped
                    nearestAirfield_dist_m < 1200 &&
                    player.Place().IsAlive()) &&
                    playerDamages < 8
                    )
        {//it's good 
         //(do nothing)
        }
        else
        {//it's no good
            string reason = "";
            if (player.Place() == null || (player.Place() as AiAircraft) == null) reason += "Not in an aircraft - ";
            if (terr != player.Army() && terr != 0) reason += "Not in on friendly or neutral territory - ";
            //Stb_distanceToNearestAirport(actor) < 3100 &&

            if (vel_mps >= 2) reason += "You are still moving (" + vel_mps.ToString("F1") + ") - ";
            if (nearestAirfield_dist_m <= 5000) reason += "You are NOT close to any unused Landing Ground (" + nearestAirfield_dist_m.ToString("N0") + ") - ";
            if (!player.Place().IsAlive()) reason += "CLOD says your aircraft is dead/crashed - ";
            if (playerDamages >= 8) reason += "Your aircraft has too much damage to be repaired, except at a fully-equipped base.";            

            mainmission.twcLogServer(new Player[] { player }, "You can't create a Landing Ground here.", new object[] { });
            Timeout(0.05, () =>
            {
                mainmission.twcLogServer(new Player[] { player }, "Reasons: " + reason, new object[] { });
            });

            return;
        }

        HashSet<string> planeSet = new HashSet<string>();

        planeSet.Add(aircraft.InternalTypeName());  //for now, only the player's a/c

        //Add planes of anything parked on or near the airport, into the birthplace list  (checks close, right army, and velocity_mps < 2
        List<AiActor> planes = Calcs.GetActorsNear(GamePlay, mainmission, mainmission.AllAircraftDict, (ap as AiActor).Pos(), 1500, player.Army(), type: "aircraft");

        foreach (AiActor plane in planes)
        {
            if ((plane as AiAircraft) == null) continue;
            double plane_vel_mps = Calcs.CalculatePointDistance((plane as AiAircraft).AirGroup().Vwld());
            if (plane_vel_mps > 2) continue;
            int planeDamages = StatCalcs.listDamages(GamePlay, bShowMessages: false, aircraft: (plane as AiAircraft));
            if (planeDamages >8) {
                mainmission.twcLogServer(new Player[] { player }, "Aircraft {0} has too much damage to be used at the Landing Ground.", new object[] { Calcs.GetAircraftType(plane as AiAircraft) });
                continue;
            }
            planeSet.Add((plane as AiAircraft).InternalTypeName());
        }

        //TODO: Could have a minimum # of aircraft required to make the LG here.

        if (planeSet.Count < 3 && !testing)
        {
            mainmission.twcLogServer(new Player[] { player }, "You can't create a Landing Ground here now.  You need at least 3 aircraft, all in good condition, landed at the new LG.", new object[] { });
            mainmission.twcLogServer(new Player[] { player }, "You had only {0}.", new object[] { planeSet.Count });
            return;
        }

        Console.WriteLine("TempLG: PLaneset on creation {0} " + planeSet.ToString(), planeSet.Count);
        

        string name = checkIfTempLandingGroundIsMissionObjective(pos, player.Army(), planeSet); //Checks if the LG is one of the listed mission objectives; also gives points etc if so
        
        name = name.Replace(" ", "_"); //Loading birthplaces, it doesn't seem to like SPACES at all.  So, we won't use any.

        if (name == "")
        {
            name = "Landing_Ground_" + ran.Next(1000, 9999).ToString("F0");
            mainmission.twcLogServer(null, "A new temporary Landing Ground was created by {0} in sector " + Calcs.correctedSectorName(this, pos), new object[] { player.Name() });
        }

        ISectionFile f = GamePlay.gpCreateSectionFile();

        f = CreateBirthPlace(f, pos.x, pos.y, 0, player.Army(), planeSet, name: name);

        GamePlay.gpPostMissionLoad(f);        

        
        string saveName = "makeLG.txt";
        try
        {
            if (saveName != null)
            {
                string sn = mainmission.CLOD_PATH + mainmission.FILE_PATH + "/sectionfiles" + "/" + saveName;
                Console.WriteLine("Saving section file to " + sn);
                f.save(sn); //testing
            }

        }
        catch (Exception ex) { Console.WriteLine("createTempLandingGround ERROR: " + ex.ToString()); }

    }


    //By Kodiak, our hero
    //http://forum.1cpublishing.eu/showpost.php?p=438212&postcount=40
    //
    public ISectionFile CreateBirthPlace(ISectionFile f, double x, double y, double z, int army, HashSet<string> planeSet, string name = "", int maxplanes = 1, bool setonpark = true, bool isparachute = true, string _country = "", string _hierarchy = "", string _regiment = "")
    {

        //ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "BirthPlace";

        //key = "Landing Ground " + ran.Next(1000, 9999).ToString("F0");
        key = "Landing_Ground_" + ran.Next(1000, 9999).ToString("F0");

        //name = name.Replace(" ", "_").Trim(); //Loading birthplaces, it doesn't seem to like SPACES at all.  So, we won't use any.

        int maxLen = 24;
        
        if (name != null & name.Length > 0)
        {
            name = name.Replace(" ", "_").Trim();

            if (name.Length < 25) maxLen = name.Length; //cant send substring a value > the actual Length of the string. Boo.

            if (name.Length >= 25) name = name.Substring(0, maxLen); //The name seems to be restricted to 25 chars at most?  Or something?

            if (name.Length > 0) key = name;
        }

        int setOnPark = 0;

        if (setonpark)
            setOnPark = 1;

        int isParachute = 0;

        if (isparachute)
            isParachute = 1;


        string country = ".";

        if (_country != null && _country.Length > 0)
            country = _country;


        string hierarchy = ".";

        if (_hierarchy != null && _hierarchy.Length > 0)
            hierarchy = _hierarchy;

        string regiment = ".";

        if (_regiment != null && _regiment.Length > 0)
            regiment = _regiment;


        //And so apparently the x,y,z coordinates here cannot have any decimal points. 
        //Despite the fact the they are OK in EVERY other similar place.  Arrggghhh.
        value = army.ToString(CultureInfo.InvariantCulture) + " " + x.ToString("F0") + " "
            + y.ToString("F0") + " " + z.ToString("F0") + " "
            + maxplanes.ToString("F0") + " " + setOnPark.ToString("F0") + " "
            + isParachute.ToString("F0") + " " + country + " " + hierarchy + " " + regiment + " 0"; //not sure what that last 0 is but 5.0 seems to have it?

        //Console.WriteLine("Creating Birthplace: " + value);

        f.add(sect, key, value);

        //Console.WriteLine("Creating Birthplace: 1");
        sect = "BirthPlace0";

        Console.WriteLine("TempLG: PLaneset before {0} " + planeSet.ToString(), planeSet.Count);
            
        //They always get the observation plane.  Also...prevents blank a/c list which would make (by default) ALL aircraft included at the bp
        if (army == 2) planeSet.Add("tobruk:Aircraft.Bf-108B-2_Trop");            
        if (army == 1) planeSet.Add("tobruk:Aircraft.DH82A_Trop");

        Console.WriteLine("TempLG: PLaneset after {0} " + planeSet.ToString(), planeSet.Count);

        foreach (string plane in planeSet)
        {
            //Console.WriteLine("Creating Birthplace: 2");                
            key = plane;
            value = "";
            f.add(sect, key, value);
        }

        //Console.WriteLine("Creating Birthplace: 3");
        return f;
    }


    //nearest airport to a point
    //army=0 is neutral, meaning found airports of any army
    //otherwise, find only airports matching that army
    //Will return water airports ONLY for seaplane=true, land airports ONLY for seaplane=false and both types for seaplane=null
    public AiAirport nearestAirportWithNoSpawn(Point3d location, int army = 0, bool? isSeaplane = null, bool checkNoSpawn = true)
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
                if (checkNoSpawn && Calcs.distanceToNearestBirthplace(GamePlay, a.Pos(), army: 0) < 1500 ) continue; //here we're looking for ANY spawnpoint, either army, within 1500m of this ap.  IF there is one, we can't use it.
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




}