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
using System.Linq;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;
using part;
using System.Runtime.InteropServices;


using TWCComms;

/*****************************************************************************
 * Class ObjectiveRepairMissions
 * 
 * Players can fly from distant airports to delivery works/gear/supplies to repair (friendly objectives like airports
 * In future maybe radar & other things, too
 * Also, they could (future) fly planes from distant airports, land them near the front, then 1 more of that a/c is
 * added to supply.
 * 
 *****************************************************************************/



public class ObjectiveRepairMission : AMission
{
    //public IMainMission TWCMainMission;
    //public ISupplyMission TWCSupplyMission;
    public Mission mainmission;
    public Parachute parachute;

    public Random ran;
    //DateTime = time repair load picked up, Point3d = where picked up, AiAircraft = player aircraft at that time, string = name of the load, int = how many loads (considering cover a/c loads)
    public Dictionary<Player, Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>> orm_PlayersOnRepairMissionExpiration;
    public double orm_maxLandingPointtoObjectiveDistance_m = 3500;
    public double orm_maxDropPointtoObjectiveDistance_m = 800;
    public double orm_minDeliveryDistance_m = 3500;
    public double orm_maxDeliveryTime_min = 65;
    public double orm_maxFrontDistanceForFerryDelivery_m = 60000;
    public double orm_maxDistanceForCoverToCount_m = 18000;
    public bool testmode = false;

    public enum RepairType { Repair_Load, Ferry, Defense_Load};
    public Dictionary <RepairType, string> RepairType_names = new Dictionary<RepairType, string>() { { RepairType.Repair_Load, "Repair Load" },
        { RepairType.Ferry, "Ferry" },
        { RepairType.Defense_Load, "Defense Load" } };
    List<RepairType> Load_repairTypes = new List<RepairType>() { RepairType.Repair_Load, RepairType.Defense_Load }; //the types where you are carrying loads in a bomber
    public enum DeliveryType { land_and_unload, air_drop };
    public Dictionary<int, string> RepairsFerries_thisStatsPeriod = new Dictionary<int, string>() { { 1, "" }, { 2, "" } };

    public ObjectiveRepairMission(Mission msn)
    {
        //TWCMainMission = TWCComms.Communicator.Instance.Main;

        //TWCComms.Communicator.Instance.Knickebein = (IKnickebeinMission)this; //allows -stats.cs to access this instance of Mission                        

        //Timeout(123, () => { checkAirgroupsIntercept_recur(); });
        ran = new Random();

        orm_PlayersOnRepairMissionExpiration = new Dictionary<Player, Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>>();
        //RepairsFerries_thisStatsPeriod[army] 
        mainmission = msn;
        parachute = new Parachute(msn);
        Console.WriteLine("-ObjectiveRepairMissions.cs successfully inited");
    }



    //Remove the repair load if the player exits the plane/enters a different one
    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {

        base.OnPlaceEnter(player, actor, placeIndex);

        if (player == null) return;
        if (!orm_PlayersOnRepairMissionExpiration.ContainsKey(player)) return;
        if (player.Place() == null || player.Place() as AiCart == null || player.Place() as AiAircraft == null) {
            if (!testmode) orm_PlayersOnRepairMissionExpiration.Remove(player);
            GamePlay.gpLogServer(new Player[] { player }, ">>>You are no longer in your aircraft. Your Repair Supply Load has been abandoned.", null);
            return;
        }
        AiAircraft pickupAircraft = orm_PlayersOnRepairMissionExpiration[player].Item3;

        if (pickupAircraft != player.Place() as AiAircraft)
        {
            if (!testmode) orm_PlayersOnRepairMissionExpiration.Remove(player);
            GamePlay.gpLogServer(new Player[] { player }, ">>>You are no longer in your aircraft. Your Repair Supply Load has been abandoned.", null);
            return;
        }

    }

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
    }



    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        //TWCSupplyMission = TWCComms.Communicator.Instance.Supply;

        //Console.WriteLine("-interceptradar.cs OnMissionLoaded {0} {1} ", missionNumber, MissionNumber);


        if (missionNumber == MissionNumber)
        {
            if (GamePlay != null && GamePlay is GameDef)
            {
                //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                Console.WriteLine("-ObjectiveRepairMissions initializing eventchat.");
                (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
            }


            Console.WriteLine("-ObjectiveRepairMissions - onMissionLoaded");


        }
    }

    public override void OnBattleStoped()
    {
        base.OnBattleStoped();

        if (GamePlay != null && GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
            //If we don't remove the new EventChat when the battle is stopped
            //we tend to get several copies of it operating, if we're not careful
        }
    }

    

    void Mission_EventChat(Player player, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 

        //Player player = from as Player;
        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;

        string msg_orig = msg;
        msg = msg.ToLower();
        if (msg.StartsWith("<pitest"))

        {
            orm_handlePickupRequest(player, testing:true);

            //GamePlay.gpLogServer(new Player[] { player }, "AIRadar: Started. <AStop or <as to stop.", null);
        }
        else if (msg.StartsWith("<pi") || msg.StartsWith("<pickup "))

        {
            orm_handlePickupRequest(player);

            //GamePlay.gpLogServer(new Player[] { player }, "AIRadar: Started. <AStop or <as to stop.", null);
        }
        else if (msg.StartsWith("<pd") || msg.StartsWith("<pickupdefense"))

        {
            orm_handlePickupRequest(player, repairType: RepairType.Defense_Load);

            //GamePlay.gpLogServer(new Player[] { player }, "AIRadar: Started. <AStop or <as to stop.", null);
        }
        else if (msg.StartsWith("<fe"))

        {            
            orm_handleFerryRequest(player);

            //GamePlay.gpLogServer(new Player[] { player }, "AIRadar: Started. <AStop or <as to stop.", null);
        }
        else if ((msg.StartsWith("<de")  && !msg.StartsWith("<dest") && !msg.StartsWith("<debug")) || msg.StartsWith("<da"))
        {
            double altAGL_m = 0;
            double velocity_mps = 0;

            bool dadvice = false;
            if (msg.StartsWith("<da")) dadvice = true;

            if (aircraft != null) 
            {
                altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                velocity_mps = Calcs.CalculatePointDistance(aircraft.AirGroup().Vwld());
            }
            /*
            var objType = Mission.MO_ObjectiveType.Radar;
            if (velocity_mps < 10 && altAGL_m < 20) objType = Mission.MO_ObjectiveType.Military_Airfield;
            */

            var deliveryType = DeliveryType.air_drop;
            if (velocity_mps < 10 && altAGL_m < 20) deliveryType = DeliveryType.land_and_unload;

            orm_handleDeliveryRequest(player, false, deliveryType, dadvice: dadvice);
        }
        else if (msg.StartsWith("<ab"))

        {
            orm_handleAbandonRequest(player);
        }
        else if (msg.StartsWith("<dpoint") && mainmission.admin_privilege_level(player) >= 2)
        {
            string msg89 = "<dpoint x y z - simulate <deliver at that x,y & AGL; use one space, no comma";

            GamePlay.gpLogServer(new Player[] { player }, msg89, new object[] { });

            Point3d? test = new Point3d(237074, 243644, 20000);
            //(69050,182277)
            //Point3d? test = new Point3d(69050, 182277, 15000);

            string[] words = msg_orig.Split(' ');
            if (words.Length >= 4 && words[1].Length > 0 && words[2].Length > 0 && words[3].Length > 0)
            {
                double x = Convert.ToInt32(words[1]);
                double y = Convert.ToInt32(words[2]);
                double z = Convert.ToInt32(words[3]);
                test = new Point3d(x, y, z);
                GamePlay.gpLogServer(new Player[] { player }, string.Format("REPAIR: testing point is {0:n0}, {1:n0}, {2:n0}, - if z less than 20m it is land&unload, otherwise air_drop. Testing...", new object[] { x,y,z }), null);
            }
            DeliveryType deliveryType = DeliveryType.air_drop;
            if (test.Value.z < 20) deliveryType = DeliveryType.land_and_unload;

            orm_handleDeliveryRequest(player, deliveryType: deliveryType, deliveryTestPoint : test);

        }
        else if (msg.StartsWith("<dtest") && mainmission.admin_privilege_level(player) > 1)

        {
            GamePlay.gpLogServer(new Player[] { player }, "Repair: TESTING delivery - will ignore plane changes and distance from objective", null);
            double altAGL_m = 0;
            double velocity_mps = 0;

            if (aircraft != null)
            {
                altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                velocity_mps = Calcs.CalculatePointDistance(aircraft.AirGroup().Vwld());
            }

            var deliveryType = DeliveryType.air_drop;
            if (velocity_mps < 10 && altAGL_m < 20) deliveryType = DeliveryType.land_and_unload;

            orm_handleDeliveryRequest(player, true, deliveryType);
        }
        else if (msg.StartsWith("<dtmodeon") && mainmission.admin_privilege_level(player) > 1)

        {
            GamePlay.gpLogServer(new Player[] { player }, "Repair: TEST MODE ON. Jumps into/out of aircraft will be ignored.  Distance from target will be ignored. Cover a/c distance from target will be ignored.  (Can turn off to test distance/height functions after switching a/c)", null);
            testmode = true;
        }
        if (msg.StartsWith("<dtmodeoff") && mainmission.admin_privilege_level(player) > 1)

        {
            GamePlay.gpLogServer(new Player[] { player }, "Repair: TEST MODE OFF", null);
            testmode = false;
        }
        else if (msg.StartsWith("<dpara") && mainmission.admin_privilege_level(player) >= 2)
        {
            string msg89 = "<dpara x y z a- drop a fake parachute at that x,y, AGL & army; use one space, no comma; <dpara alt1 alt2 does it @ your position/army";

            GamePlay.gpLogServer(new Player[] { player }, msg89, new object[] { });
            

            Point3d test = new Point3d(237074, 243644, 20000);
            //(69050,182277)
            //Point3d? test = new Point3d(69050, 182277, 15000);
            int army = 0;
            double za1 = -50;
            double za2 = -100;
            Vector3d? vwld = null;
            string[] words = msg_orig.Split(' ');
            if (words.Length >= 4 && words[1].Length > 0 && words[2].Length > 0 && words[3].Length > 0)
            {
                double x = Convert.ToInt32(words[1]);
                double y = Convert.ToInt32(words[2]);
                double z = Convert.ToInt32(words[3]);
                army = Convert.ToInt32(words[4]);
                test = new Point3d(x, y, z);
                
            }
            else if (player != null && player.Place() != null)
            {
                //So...when spawning in an a/c, the Z in position is AGL.  !!!!!!!
                double altAGL_m = 0;
                altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                test = player.Place().Pos();
                if (altAGL_m > 0) test.z = altAGL_m;
                army = player.Army();
                vwld = (player.Place() as AiAircraft).AirGroup().Vwld();
                if (words.Length >= 3)
                {
                    za1 = Convert.ToInt32(words[1]);
                    za2 = Convert.ToInt32(words[2]);
                }
                //AiAircraft aircraft = (player.Place() as AiAircraft);
                Point3d orientation = new Point3d(
                    aircraft.getParameter(part.ParameterTypes.Z_Orientation, 0),
                    aircraft.getParameter(part.ParameterTypes.Z_Orientation, 1),
                    aircraft.getParameter(part.ParameterTypes.Z_Orientation, 2)
                    );
                GamePlay.gpLogServer(new Player[] { player }, "Your orientation: {0} {1} {2}", new object[] {orientation.x, orientation.y, orientation.z });

            }

            GamePlay.gpLogServer(new Player[] { player }, string.Format("REPAIR: parachute drop point is {0:n0}, {1:n0}, {2:n0} army: {3}", new object[] { test.x, test.y, test.z, army }), null);

            //parachute.dropParachute(test, army, za1, za2);
            parachute.dropParachute(test, army: army, z_add_m: -100, z_add2_m: -100, vwld:vwld);
        }
        else if (msg.StartsWith("<phelp5"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>ASSET REPAIR & FERRY MISSIONS HELP 5/5", null);
            GamePlay.gpLogServer(new Player[] { player }, "Use Tab-4-4-4 menu OR General Situation Map/online radar", null);
            GamePlay.gpLogServer(new Player[] { player }, "for lists of which radar, airports, and other assets are damaged, and exact sector,", null);
            GamePlay.gpLogServer(new Player[] { player }, "and which aircraft supplies are low.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Note: This is a Good Piloting Challenge! You must take off and land cleanly, get to your destination quickly with only a minimum of fuel loaded,", null);
            GamePlay.gpLogServer(new Player[] { player }, "and successfully herd your Cover Aircraft - while evading any enemy.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Clean, solid, expert piloting and navigation is absolutely required.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Get lost - crash land - overheat your engines - can't find your objective - any little thing goes wrong - and it's ALL over.", null);
            GamePlay.gpLogServer(new Player[] { player }, "And... most navigation aids are unavailable for Repair/Ferry missions.", null);
            GamePlay.gpLogServer(new Player[] { player }, "You'll be flying by the good old compass, map, and seat-of-the-pants...", null);
            GamePlay.gpLogServer(new Player[] { player }, "Tip: Abbreviate <pickup, <ferry, <deliver, <abandon to <pi, <fe, <de, <ab", null);

        }
        else if (msg.StartsWith("<phelp3"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>ASSET REPAIR & FERRY MISSIONS HELP 4/5", null);
            GamePlay.gpLogServer(new Player[] { player }, "You must initiate <pickup, <pdefense, and <ferry starting at airports in the *far* corner of the map", null);
            GamePlay.gpLogServer(new Player[] { player }, "Even if a particular aircraft supply is zero, you can still pick up an aircraft of that type for yourself and your <cover aircraft, in the <ferry area ONLY.", null);
            GamePlay.gpLogServer(new Player[] { player }, "If you <deliver the aircraft successfully to a front-line airport, these aircraft will then be added supply.", null);
            GamePlay.gpLogServer(new Player[] { player }, string.Format("To count for Repair Load or Ferry Aircraft actually delivered, Cover Aircraft must be within {0:n0}km of you at moment of <delivery", orm_maxDistanceForCoverToCount_m / 1000), null);
            GamePlay.gpLogServer(new Player[] { player }, "Repair Airports: Your target airport is damaged, so landing is tricky.  Land anywhere nearby & survive, as you are able.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Repair radar & other objectives, or deliver Defense Units: Fly over the target, right at 900m/3000ft AGL, straight, level, and slow - 200kph/120mph. <deliver above your target - or within 1km distance at most", null);        
            GamePlay.gpLogServer(new Player[] { player }, "If you drop too high the parachutes will drift too far.  Too low and there is no time for the parachutes to open. So paratroopers & parachute supplies will not exit the aircraft unless you are at just the right altitude, attitude, and speed.", null);
            GamePlay.gpLogServer(new Player[] { player }, "If your parameters are unsafe, the jumpmaster will inform you of the corrections you need to make.", null); 
            GamePlay.gpLogServer(new Player[] { player }, "Command <da gets advice from the jumpmaster about your current situation. You can use <da to practice training drops in bomber without a bomb load - even if you haven't picked up a repair/defense load first.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Once you have given the signal to drop the load (<de), hold steady for several seconds as the paratroops/supplies unload.", null);
            GamePlay.gpLogServer(new Player[] { player }, "If successful, you'll see parachutes, a flare to guide parachute troops to their landing around and ground crews to your drop point, and a message.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Nearly all types of objectives can be repaired and fortified with extra defense. Airfields, radar, and fuel production/supplies are the most vital.", null);
            GamePlay.gpLogServer(new Player[] { player }, "<phelp5 for more...", null);


        }
        else if (msg.StartsWith("<phelp3"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>ASSET REPAIR & FERRY MISSIONS HELP 3/5", null);
            GamePlay.gpLogServer(new Player[] { player }, "<pdefense is similar to <pickup and <ferry - but brings defense/repair squads to the objective for 1 week", null);
            GamePlay.gpLogServer(new Player[] { player }, "<pd at an airport in the far corner of the map, then add <cover aircraft.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Land and <deliver the defense loads at an airfield, or air drop and <deliver over an objective at 600m/2000ft AGL.", null); GamePlay.gpLogServer(new Player[] { player }, "<phelp4 for more...", null);
        }
        else if (msg.StartsWith("<phelp2"))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>>ASSET REPAIR & FERRY MISSIONS HELP 2/5", null);
            GamePlay.gpLogServer(new Player[] { player }, "<ferry: This adds new aircraft to your army's supply OR a new aircraft type to a Landing Ground", null);
            GamePlay.gpLogServer(new Player[] { player }, "Spawn into an airport, in the far corner of the map.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Spawn into the type of aircraft whose supply you wish to replenish", null);
            GamePlay.gpLogServer(new Player[] { player }, "<cover 1 2 (or similar, repeatedly) to bring in cover aircraft to ferry with you (up to 10).", null);
            GamePlay.gpLogServer(new Player[] { player }, string.Format("Fly to a front line airport (less than {0:n0}km from the front) OR to an established Landing Ground.", orm_maxFrontDistanceForFerryDelivery_m / 1000), null);
            GamePlay.gpLogServer(new Player[] { player }, "Land safely, then chat command <deliver", null);
            GamePlay.gpLogServer(new Player[] { player }, "Aircraft of that type will be added to your supply AND - if you delivered to a Landing Ground - to the LG's inventory.", null);
            GamePlay.gpLogServer(new Player[] { player }, "<phelp3 for more . . . ", null);

        }

        else if (msg.StartsWith("<phelp"))
        {

            GamePlay.gpLogServer(new Player[] { player }, ">>>>ASSET REPAIR & FERRY MISSIONS HELP", null);
            GamePlay.gpLogServer(new Player[] { player }, "Commands: <pickup <pdefense <ferry <deliver <abandon <phelp", null);
            GamePlay.gpLogServer(new Player[] { player }, "<pickup: Go to an airport far back from the front lines", null);
            GamePlay.gpLogServer(new Player[] { player }, "in a heavy bomber w/no bomb load.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Chat command <pickup to pick up a load of repair staff/material", null);
            //GamePlay.gpLogServer(new Player[] { player }, "Chat command <pickup to pick up a load of repair staff/material", null);
            GamePlay.gpLogServer(new Player[] { player }, "<cover 1 2 (or similar, repeatedly) to bring in cover aircraft to carry loads with you (up to 10).", null);
            GamePlay.gpLogServer(new Player[] { player }, "Land at an airport OR fly over radar or other objective needing repair and drop via parachute at straight, level, and slow at 600m/2000ft AGL.", null);
            GamePlay.gpLogServer(new Player[] { player }, "Chat command <deliver or <de", null);
            GamePlay.gpLogServer(new Player[] { player }, "Repair of that objective will be accelerated!", null);
            GamePlay.gpLogServer(new Player[] { player }, "<dadvice or <da for the Jumpmaster's advice on how your current parachute <deliver parameters look.", null);

            GamePlay.gpLogServer(new Player[] { player }, "<phelp2 for more . . . ", null);
        }


        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1.6; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.5;

            string msg41 = "<pickup <pdefense <ferry <deliver <dadvice Ferry in new aircraft, or pickup/deliver supplies and Defense Units to speed up repair of airfields, radar, etc; <phelp More info";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
        else if (msg.StartsWith("<admin") && mainmission.admin_privilege_level(player) > 1)
        {
            double to = 5.2; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.5;

            string msg41 = "<dtest - test <deliver; <pitest; <dtmodeon; <dtmodeoff - turn <deliver test mode on/off/; <dpoint - simulate <deliver at that x,y & AGL";

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }

    }

    private readonly string[] orm_RepairPickup_items = { "A repair team with their equipment.", "A pair of mini-dozers with operators and mechanics.", "A big load of concrete.", "A big load of cement and rebar.", "12 dozen gold-plated shovels and wheelbarrows so the top brass can take a turn at filling bomb craters.", "A crack repair team and their equipment.", "Fuel, lube, and repair parts for the bulldozers", "A giant load of shovels and gunny sacks.", "A big load of special, top-secret, non-bombable dirt.", "A big load of cement powder.", "Fuel, lube, and repair parts for the bulldozers", "A new team of repair specialists and their equipment.", "Much needed food and drink for the tired repair workers.", "Diesel fuel to run the bulldozers 24/7.", "Orders from the General putting repair work at highest possible priority along with a crack team of repair organizers.", "A team of construction experts", "2,500kg of fuel", "Replacement parts and supplies", "2,500kg of fuel", "2,000kg of fuel and 500kg of oil", "A big load of iron bars and welding equipment", "Crack construction crews and their equipment", "2,500kg of high-octane fuel" };

    private readonly string[] orm_DefensePickup_items = { "AA squads and defense troops.", "A crack defense squad and their AA units", "High trained AA units", "AA units and troops to reinforce defenses", "Crack anti-aircraft units and the latest AA guns", "Defense units", "Crack defense units and their equipment", "Defensive units with their AA guns and ammo", "Anti-aircraft squadrons and observers", "Anti-aircraft units, equipment, and ammo", "Training squadrons to whip area defenders and AA into tip-top shape", "AA squadrons and extra ammo", "The latest upgrades for area AA guns and squadrons to improve their accuracy and effectiveness", "Extra AA guns and troops"  };


    public bool orm_isPlayerOnRepairMission(Player player) { 
        if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
        {
            return true; //we don't really want to check or update their repair/cargo status here, because if has expired say they are still carrying it and they need to land and deliver it (will fail) or abandon.  They can't/shouldn't just abandon it mid-flight, though, and get <cover <knickebein, etc etc all back all the sudden
        }
        else return false;
    }

    private bool orm_isPlayerAllowedRepairPickup(Player player, bool testing = false, bool coverPickup = false, RepairType repairType= RepairType.Repair_Load)
    {
        string taskName = "pick up Repair Supplies";
        if (repairType == RepairType.Ferry) taskName = "ferry aircraft";
        if (repairType == RepairType.Defense_Load) taskName = "pick up Defense Troops & gear";
        

        if (player == null) return false;
        if (player.Place() == null || player.Place() as AiCart == null) return false;
        Point3d player_pos = player.Place().Pos();

        if (player.Place() as AiAircraft == null)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be in an aircraft to {0} (you're not).", new object[] { taskName});
            return false;
        }
        AiAircraft aircraft = player.Place() as AiAircraft;
        //if ((player.Place() as AiCart).Places() < 2) return false; //simply restricting this to planes with 2 or more seats, for now (2021/06)

        string coverExpl = "";
        if (coverPickup) coverExpl = "for your cover aircraft ";

        if (!Calcs.isHeavyBomber(aircraft) && Load_repairTypes.Contains(repairType))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be in a heavy bomber {0}to {1}.", new object[] { coverExpl, taskName });
            return false;
        }
        if (Calcs.isHeavyBomber(aircraft) && aircraft.AirGroup().hasBombs())
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, heavy bombers must have **no bombs aboard** {0}to {1}.", new object[] { coverExpl, taskName });
            return false;
        }
        if (GamePlay.gpFrontArmy(player_pos.x, player_pos.y) != player.Army())
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be on friendly territory {0}to {1}.", new object[] { coverExpl, taskName });
            return false;
        }
        if (Calcs.CalculatePointDistance((player.Place() as AiAircraft).AirGroup().Vwld()) > 2)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be stopped at an airport {0}to {1} (you are moving).", new object[] { coverExpl, taskName });
            return false;
        }
        if (Calcs.distanceToNearestAirport(GamePlay, aircraft as AiActor) > 2200)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be stopped at a friendly airport {0}to {1} (you too far from an airport).", new object[] { coverExpl, taskName });
            return false;
        }
        double altAGL_m = 0;
        altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
        if ((aircraft as AiActor).Pos().z > 2000 || altAGL_m > 20)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be **on the ground** {0}to {1}.", new object[] { coverExpl, taskName });
            return false;
        }
        if (player.Army() == 1 && !isPointInFerryArea(player.Army(), player_pos) && !testmode)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be at an airport far back from the front line {0}to {1}. Try Watchfield, Yatesbury, Reading, Harwell, Farnborough, Odiham, Upavon, Netheravon, Larkhill, or White Waltham.", new object[] { coverExpl, taskName });
            return false;
        }

        if (player.Army() == 2 && !isPointInFerryArea(player.Army(), player_pos) && !testmode)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be at an airport far back from the front line {0}to {1}. Try Amiens Glisy, Rosieres en Santerre, Montdidier, Creil, Roye Amy, Persan Beaumont, Beauvais Nivillers, Beauvais Tille, or Crecy.", new object[] { coverExpl, taskName });
            return false;
        }

        return true;
    }

    private bool orm_isPlayerAllowedRepairDelivery(Player player, bool testing = false, DeliveryType deliveryType = DeliveryType.land_and_unload, RepairType repairType = RepairType.Repair_Load, Point3d? deliveryTestPoint = null, bool dadvice = false)
    {
        try
        {
            if (player == null) return false;
            if (player.Place() == null || player.Place() as AiCart == null) return false;

            Point3d player_pos = player.Place().Pos();
            if (deliveryTestPoint.HasValue) player_pos = deliveryTestPoint.Value; //allows testing of various points to see if they'll work

            bool dadvice_success = true;
            string taskName = "Repair Supplies";
            if (repairType == RepairType.Ferry) taskName = "ferry aircraft";
            if (repairType == RepairType.Defense_Load) taskName = "Defense Troops & gear";
            var Load_repairTypes = new List<RepairType>() { RepairType.Repair_Load, RepairType.Defense_Load }; //the types where you are carrying loads in a bomber

            if (player.Place() as AiAircraft == null)
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be in an aircraft to deliver {0} (you're not).", new object[] { taskName });
                return false;
            }
            AiAircraft aircraft = player.Place() as AiAircraft;

            if (!orm_PlayersOnRepairMissionExpiration.ContainsKey(player) && !dadvice)
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must first pick up {0} before delivering them (you haven't pick up any {0} yet).", new object[] { taskName });
                return false;
            }

            //defaults (used when dadvice == true)
            AiAircraft pickupAircraft = aircraft;
            DateTime expiration_time = DateTime.UtcNow.AddHours(1);
            Point3d pickupPoint = new Point3d(20000, 300000, 0);
            if (player.Army() == 2) pickupPoint = new Point3d(20000, 20000, 0);
            string playerLoad = "(Training Repair Load)";

            if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
            {
                pickupAircraft = orm_PlayersOnRepairMissionExpiration[player].Item3;
                expiration_time = orm_PlayersOnRepairMissionExpiration[player].Item1;
                pickupPoint = orm_PlayersOnRepairMissionExpiration[player].Item2;
                playerLoad = orm_PlayersOnRepairMissionExpiration[player].Item4;
            }

            TimeSpan time_left = expiration_time.Subtract(DateTime.UtcNow);

            if (pickupAircraft != aircraft && !testing)
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must deliver your {0} in the same aircraft you used to pick it up (you are in a different aircraft).", new object[] { taskName });
                return false;
            }

            if (time_left.TotalSeconds < 0)
            {
                GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Sorry, the time limit to deliver your {1} has expired! (It expired {0:n1} minutes ago).", new object[] { -time_left.TotalMinutes, taskName }), null);
                return false;
            }

            //if ((player.Place() as AiCart).Places() < 2) return false; //simply restricting this to planes with 2 or more seats, for now (2021/06)
            if (!Calcs.isHeavyBomber(aircraft) && Load_repairTypes.Contains(repairType))
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be in a heavy bomber to deliver {0}.", new object[] { taskName });
                return false;
            }

            if (Calcs.isHeavyBomber(aircraft) && aircraft.AirGroup().hasBombs())
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, if you are in a heavy bomber it must have **no bombs aboard** to deliver {0}.", new object[] { taskName });
                return false;
            }
            //OK, they can deliver to enemy if they like. For supplies, not ferry. Also for tobruk, many times airport is in neutral or enemy territory
            /*
            int terr = GamePlay.gpFrontArmy(player_pos.x, player_pos.y);
            //eliminating this - they can deliver a/c to the enemy if they so wish!
            //also, sometimes an LG might be in enemy territory
            if (repairType == RepairType.Ferry &&  terr != 0 && terr != player.Army())
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be on friendly or neutral territory to deliver {0}.", new object[] { taskName });
                return false;
            }
            */

            double distanceTraveled_m = RepairCalcs.CalculatePointDistance(pickupPoint, player_pos);
            if (distanceTraveled_m < 20000 && !testing)
            {
                GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Sorry, you must travel at least 20km to deliver {0} (you traveled {1:n1}km).", new object[] { taskName, distanceTraveled_m }), new object[] { taskName });
                if (!dadvice) return false;
            }

            double altAGL_m = 0;
            altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
            Vector3d vwld = aircraft.AirGroup().Vwld();
            double vel_mps = Calcs.CalculatePointDistance(vwld);
            if (deliveryTestPoint.HasValue) altAGL_m = player_pos.z; //use the z val for AGL, if testing
                                                                     //part.ParameterTypes.Z_Orientation
                                                                     //0 = yaw orientation, -180 to 180, 0 = east, +90 = north
                                                                     //1 = pitch orientation, - 90 to 90, 0 = level, +90 is nose straight down
                                                                     //2 = roll orientation, -90 to 90, 0 = level, +90 is right wing up
            Point3d orientation = new Point3d(
                aircraft.getParameter(part.ParameterTypes.Z_Orientation, 0),
                aircraft.getParameter(part.ParameterTypes.Z_Orientation, 1),
                aircraft.getParameter(part.ParameterTypes.Z_Orientation, 2)
                );

            //Special requirements for AIRDROP
            if (Load_repairTypes.Contains(repairType) && deliveryType == DeliveryType.air_drop)
            {
                if (altAGL_m < 800 || altAGL_m > 1010)
                {
                    if (player.Army() == 2) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: You must be above the target, AGL between 850 and 950 meters, to drop {1} via {0}. (Your AGL is " + altAGL_m.ToString("n0") + "m)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    else GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: You must be above the target, AGL between 2800 and 3200 feet, to drop {1} via {0}. (Your AGL is " + Calcs.meters2feet(altAGL_m).ToString("n0") + "ft)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    if (!testmode && !dadvice) return false;
                    else if (testmode) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>REPAIR: TESTMODE ON, AGL too high is ignored for now. (Your AGL is " + altAGL_m.ToString("n0") + "m)", new object[] { }), null);
                    dadvice_success = false;
                }
                if (vel_mps < 40 || vel_mps > 70)
                {
                    if (player.Army() == 2) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: Your airspeed must be between 150 and 210km/h to drop {1} via {0}. (Your airspeed is " + Calcs.meterspsec2kmphour(vel_mps).ToString("n0") + "km/h)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    else GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: Your airspeed must be between 90 and 130mph to drop {1} via {0}. (Your airspeed is " + Calcs.meterspsec2milesphour(vel_mps).ToString("n0") + "mph)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    if (!testmode && !dadvice) return false;
                    else if (testmode) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>REPAIR: TESTMODE ON, airspeed too high is ignored for now. (Your airspeed is " + Calcs.meterspsec2milesphour(vel_mps).ToString("n0") + "mph)", new object[] { }), null);
                    dadvice_success = false;
                }
                if (vwld.z < -4.8 || vwld.z > 4.8)
                {
                    if (player.Army() == 2) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: You must be in straight and level flight to drop your parachute load safely - vertical speed less than 4 mps. (Your vertical speed is " + (Math.Abs(vwld.z)).ToString("n1") + "mps)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    else GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: You must be in straight and level flight to drop your parachute load safely - vertical speed less than 750 feet/minute. (Your vertical speed is " + (Calcs.meterspsec2ftpermin(Math.Abs(vwld.z))).ToString("n0") + "ft/min)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    if (!testmode && !dadvice) return false;
                    else if (testmode) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>REPAIR: TESTMODE ON, vertical speed too high is ignored for now. (Your vertical speed is " + Calcs.meterspsec2ftpermin(vel_mps).ToString("n0") + "ft/min)", new object[] { }), null);
                    dadvice_success = false;
                }

                if (Math.Abs(orientation.y) > 8 || Math.Abs(orientation.z) > 8)
                {
                    GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Jumpmaster: You must be in straight and level flight to drop your parachute load safely - level in both pitch and roll, and in no event more than 8 degrees in either. (Your pitch is " + Math.Abs(orientation.y).ToString("n1") + " degrees and roll is " + Math.Abs(orientation.z).ToString("n1") + " degrees.)", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);

                    if (!testmode && !dadvice) return false;
                    else if (testmode) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>REPAIR: TESTMODE ON, pitch/roll too high is ignored for now. (Your pitch is " + Math.Abs(orientation.y).ToString("n0") + " degrees and roll is " + Math.Abs(orientation.z).ToString("n0") + " degrees.)", new object[] { }), null);
                    dadvice_success = false;
                }

                //don't need to do distance; we do it later as we step through all MOs.
            }

            //Special requirements for type LAND AND UNLOAD
            else if (repairType == RepairType.Ferry || Load_repairTypes.Contains(repairType) && deliveryType == DeliveryType.land_and_unload)
            {
                if (Calcs.CalculatePointDistance((player.Place() as AiAircraft).AirGroup().Vwld()) > 2)
                {
                    GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Sorry, you must be landed and stopped to deliver {1} to via {0} (you are moving).", new object[] { deliveryType.ToString().Replace('_', ' '), taskName }), null);
                    if (!dadvice) return false;
                    dadvice_success = false;
                }

                double distToAirport_m = Calcs.distanceToNearestAirport(GamePlay, aircraft as AiActor);
                if (deliveryTestPoint.HasValue) distToAirport_m = Calcs.distanceToNearestAirport(GamePlay, player_pos, player.Army());

                if (distToAirport_m > orm_maxLandingPointtoObjectiveDistance_m && !testmode && repairType == RepairType.Ferry )
                {
                    GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Sorry, you must be stopped within {1:n0}m of the center of a friendly airfield to deliver {2} (your distance is {0:n0}m).", new object[] { distToAirport_m, orm_maxLandingPointtoObjectiveDistance_m, taskName }), null);
                    if (!dadvice) return false;
                    dadvice_success = false;
                }

                if (altAGL_m > 20)
                {
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be **on the ground** to deliver {0} via " + deliveryType.ToString().Replace('_', ' ') + ".", new object[] { taskName });
                    if (!dadvice) return false;
                    dadvice_success = false;
                }
            }

            //So to delivery ferry aircraft, they must fly them right up to the front.  For CHANNEL maps we'll say 60km from the
            //front.  If front is centered in channel, that gives each side a few airports around French point, English point.
            //If front gets close to one side, though, then they can ferry aircraft to tons more airports along their coast.
            //So this potentially gives the losing side a pretty good leg up if they want to take it. 
            //Like a delivery to Le Havre or Dieppe is pretty easy once the front closes in on them.

            if (repairType == RepairType.Ferry)
            {
                double playerFrontDistance_m = GamePlay.gpFrontDistance(3 - player.Army(), player_pos.x, player_pos.y);

                double playerLandingGroundDistance_m = mainmission.landinggroundmission.distancetoNearestLandingGround(player_pos);

                if (playerFrontDistance_m > orm_maxFrontDistanceForFerryDelivery_m && playerLandingGroundDistance_m > orm_maxLandingPointtoObjectiveDistance_m)
                {
                    GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Sorry, you must be at an airport within {1:n0}km of the front line OR at an established Landing Ground  to deliver {0}.", new object[] { taskName, orm_maxFrontDistanceForFerryDelivery_m / 1000 }), null);
                    GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>You are {2:n0}km from the front line and NOT at a Landing Ground.", new object[] { taskName, orm_maxFrontDistanceForFerryDelivery_m / 1000, playerFrontDistance_m / 1000 }), null);
                    if (!dadvice) return false;
                    dadvice_success = false;
                }

            }
            if (dadvice && !dadvice_success) return false;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("REPAIR PlayerAllowedRepairDelivery ERROR: " + ex.ToString());
            GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER isPlayerAllowedRepairDelivery!!!!! Please inform Flug.", null);
            return false;
        }
    }

    //objType is the guess at objtype (radar or airfield) based on whether the player has landed or still flying
    private bool orm_handleRepairMissionRepair(Player player, bool testing = false, DeliveryType deliveryType = DeliveryType.land_and_unload, Point3d? deliveryTestPoint = null, bool dadvice = false)
    {
        try
        {
            if (player == null) return false;
            var repairType = RepairType.Repair_Load;
            if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player)) {
                var playerRepairInfo = orm_PlayersOnRepairMissionExpiration[player];
                repairType = playerRepairInfo.Item6;
            }
            if (Load_repairTypes.Contains(repairType))
                return orm_handleRepairMissionRepairObjective(player, testing, deliveryType, deliveryTestPoint: deliveryTestPoint, dadvice: dadvice);
            else if (repairType == RepairType.Ferry)
                return orm_handleRepairMissionFerryComplete(player, testing, deliveryTestPoint: deliveryTestPoint, dadvice: dadvice);
            else return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("REPAIR handleRepairMissionRepair ERROR: " + ex.ToString());            
            GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER REPAIR (handler)!!!!! Please inform Flug.", null);
            return false;
        }

    }
//public enum moo = Mission.MO_ObjectiveType;

//public List<Mission.MO_ObjectiveType> workingObjectiveTypes = new List<Mission.MO_ObjectiveType> { moo.Radar, moo.Civilian_Building, }; 
/*Military_Building, Military_Airfield, Civilian_Airfield, Bridge, Dam, Naval_Dock_Area, Railroad_Yard, Railroad, Railroad_Bridge, Road, Airfield_Complex, Factory_Complex, ArmyBase, MilitaryProductionArea, MilitaryArea, MilitaryHeadquarters, ProductionFacility, MilitaryProductionFacility, CivilianStorageFacility, MilitaryStorageFacility, CivilianFuelStorage, MilitaryFuelStorage, MilitaryFuelProduction, MilitaryRepairFacility, WeaponsStorage, AmmunitionStorage };
} */

    private bool orm_handleRepairMissionRepairObjective(Player player, bool testing = false, DeliveryType deliveryType = DeliveryType.land_and_unload, Point3d? deliveryTestPoint = null, bool dadvice = false)
    {
        try
        {
            Point3d player_pos = player.Place().Pos();
            AiAircraft aircraft = player.Place() as AiAircraft;
            if (deliveryTestPoint.HasValue) player_pos = deliveryTestPoint.Value;

            //So...when spawning in an a/c, the Z in position is AGL.  !!!!!!!
            double altAGL_m = 0;
            altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
            Point3d parachute_pos = player_pos;
            if (altAGL_m > 0) parachute_pos.z = altAGL_m;

            int army = player.Army();
            bool ret = false;
            double closest_dist_m = 10000000;
            double closest_dist_2nd_m = 10000000;
            Mission.MissionObjective foundMO = null;
            Mission.MissionObjective closestMO = null;
            DateTime currTime_UTC = DateTime.UtcNow;
            Point3d target_pos = player_pos;
            double dist_to_check_m = orm_maxLandingPointtoObjectiveDistance_m;
            Vector3d vwld = aircraft.AirGroup().Vwld();
            double vel_mps = Calcs.CalculatePointDistance(vwld);
            double forwardTime_s = 10;
            if (deliveryType == DeliveryType.air_drop)
            {
                dist_to_check_m = orm_maxDropPointtoObjectiveDistance_m;
                //we move the aim position forward 5.5seconds in the direction the player is
                //traveling - this should allow use of the bomb site for a good go 
                //at precise drops.  13.6 sec to drop from 2000ft, but these are parachutes
                //so allow for some trajectory time, then parachute straight down time
                target_pos = new Point3d(player_pos.x + vwld.x * forwardTime_s, player_pos.y + vwld.y * forwardTime_s, player_pos.z);
                //put the _pos 5.5 sec in the future, and than ALSO we'll wait the 8.5 sec to 
                //launch the parachutes
                parachute_pos = new Point3d(player_pos.x + vwld.x * forwardTime_s, player_pos.y + vwld.y * forwardTime_s, parachute_pos.z);             
            }

            //defaults - used when dadvice == true
            Console.WriteLine("orm_handleRepairMissionRepairObjective #1");
            int numLoads = 1;
            RepairType repairType = RepairType.Repair_Load;
            string taskName = "Repair Supplies";
            Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType> playerRepairInfo = null;

            if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
            {
                playerRepairInfo = orm_PlayersOnRepairMissionExpiration[player];
                numLoads = playerRepairInfo.Item5;
                repairType = orm_PlayersOnRepairMissionExpiration[player].Item6;
                taskName = "Repair Supplies";
                if (repairType == RepairType.Defense_Load) taskName = "Defense Troops & gear";
            }
            
            else if (!dadvice && !testing) return false;

            //so we count any cover aircraft still alive AND within 15000m (or whatever is set) of the player at this moment, as additional loads
            int numCoverACCheckedOut = 0;
            int totalNumCoverACCheckedOut = 0;
            double distanceToCountCoverRepair = orm_maxDistanceForCoverToCount_m;
            if (testmode) distanceToCountCoverRepair = 0; //meaning, ignore the distance requirement, just return ALL cover a/c

            if (mainmission.covermission != null)
            {
                numCoverACCheckedOut = mainmission.covermission.numberAircraftCurrentlyCheckedOutPlayer(player, dist_m: distanceToCountCoverRepair); //13km =  ~8 miles.  Cover a/c are not too well behaved
                numLoads = numCoverACCheckedOut + 1; // +1 for the player's own a/c
                totalNumCoverACCheckedOut = mainmission.covermission.numberAircraftCurrentlyCheckedOutPlayer(player, dist_m: 0);
            }
            else
            {
                Console.WriteLine("REPAIR MISSION ERROR (Objective Repair Mission complete): mainmission.covermission is NULL, cannot check current cover aircraft!!!!!");
                numLoads = playerRepairInfo.Item5; //not the best option but better than just counting NOTHING here

            }

            Console.WriteLine("orm_handleRepairMissionRepairObjective #2");
            //We allow maximum of loads of the amount the player originally checked out (little cheater check)
            //if (numLoads > playerRepairInfo.Item5) numLoads = playerRepairInfo.Item5;
            //2022-04-13 - skipping the above in case it is causing a bug of some kind.

            if (numLoads < 1) numLoads = 1; //Just little sanity check, should never be <1

            if (totalNumCoverACCheckedOut - (numLoads - 1) > 0)
            {

                GamePlay.gpLogServer(new Player[] { player }, "Repair: " + (totalNumCoverACCheckedOut - (numLoads - 1)).ToString("F0") + " of your cover aircraft were too far away from you, or lost en route.  Unfortunately, their {0} were lost.", new object[] { taskName });
            }

            foreach (Mission.MissionObjective mo in mainmission.MissionObjectivesList.Values)
            {

                //Console.WriteLine("LATD: " + mo.ID);
                if (mo.OwnerArmy != army) continue;

                //We were restricting this to certain objective types, but why?  We'll allow ALL of them now, why not.
                //if (mo.MOObjectiveType != deliveryType) continue; //We could include airfield complex & other such things at some future time???!?

                //if (!workingObjectiveTypes.Contains(mo.MOObjectiveType)) continue;

                double dist_m = RepairCalcs.CalculatePointDistance(target_pos, mo.Pos);

                //ACtually time to undestroy is often/usually set (not sure why) and it's meaningless unless ALSO
                //mo.Destroyed == true;

                //Oops, can't use that timetoundestroy as meaning anything UNLESS the mo.Destroyed is also true.
                //So for this purpose, just IGNORE it entirely.
                //if ((!mo.Destroyed && (!mo.TimeToUndestroy_UTC.HasValue || mo.TimeToUndestroy_UTC.Value.CompareTo(currTime_UTC) <= 0)) && mo.DestroyedPercent == 0 && mo.AirfieldDamagePoints == 0) continue;

                //so now we are allowing negative % repairs, meaning, the OBJ is storing up extra
                //repair supplies to repair faster
                /*
                if (repairType == RepairType.Repair_Load && !mo.Destroyed && mo.DestroyedPercent <= 0 && mo.AirfieldDamagePoints <= 0)
                {
                    if (dist_m < dist_to_check_m)
                    {
                        GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>You dropped {2} within {0:n1}km of {1}. However, destroyedpercent == 0{1} is not damaged.", new object[] { dist_m / 1000, mo.Name, taskName }), null);
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Continuing to look for any other nearby objectives needing repair . . . ", new object[] { });
                    }
                    continue;
                }
                */

                //if (mo.DestroyedPercent == 0 && mo.AirfieldDamagePoints == 0 ) continue;
                //NOTE should have been: if (mo.DestroyedPercent <= 0 && mo.AirfieldDamagePoints <= 0 ) continue;
                if (dist_m < closest_dist_2nd_m)
                {
                    closestMO = mo;
                    closest_dist_2nd_m = dist_m;
                }
                if (dist_m > dist_to_check_m && dist_m > mo.radius && !testmode) continue; //thus we can turn off testmode & see if the distance/height business is working OK
                if (deliveryType == DeliveryType.air_drop && mo.MOObjectiveType == Mission.MO_ObjectiveType.Military_Airfield) continue; //must LAND at airports to deliver - can't just drop it
                if (closest_dist_m < dist_m) continue; //already found one closer

                foundMO = mo;
                closest_dist_m = dist_m;
            }

            Console.WriteLine("orm_handleRepairMissionRepairObjective #3");
            /*mo.DestroyedPercent = mo_old.DestroyedPercent;
    mo.ObjectiveAchievedForPoints = mo_old.ObjectiveAchievedForPoints;
    mo.TimeToUndestroy_UTC = mo_old.TimeToUndestroy_UTC;*/



            if (foundMO == null)
            {
                string objtype = "damaged/destroyed";
                if (repairType == RepairType.Defense_Load) objtype = "";
                
                if (dadvice) {
                    Timeout(1.5, () =>
                    {
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Jumpmaster: SUCCESSFUL training drop. You are in position and at the right altitude, speed, and attitude to make a safe parachute drop or on-ground delivery. However you are not near an objective.", null);
                        if (closestMO != null) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Closest {2} objective that can be repaired/defended is {1} at {0:n1}km from your current location.", new object[] { closest_dist_2nd_m / 1000, closestMO.Name, objtype }), null);
                    });
                    return false;
                }
            
                GamePlay.gpLogServer(new Player[] { player }, ">>>Jumpmaster: You are not close enough to a {1} objective to " + deliveryType.ToString().Replace('_', ' ') + " your {0}.", new object[] { taskName, objtype });
                if (closestMO != null) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Closest {2} objective that can be repaired/defended is {1} at {0:n1}km from your current location.", new object[] { closest_dist_2nd_m / 1000, closestMO.Name, objtype }), null);
                return false;
            }

            if (dadvice)
            {
                Timeout(1.5, () =>
                {
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Jumpmaster: SUCCESSFUL training drop. You are in position and at the right altitude, speed, and attitude to make a safe parachute drop or on-ground delivery. A drop at this point would have successfully provisioned " + foundMO.Name + ".", null);
                });
                return false;
            }
            if (deliveryType == DeliveryType.air_drop)
            {
                for (int i = 1; i < forwardTime_s; i = i + 2)
                {
                    Timeout(i, () =>
                    {
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Jumpmaster: Hold steady...", null);
                    });
                }
            }

            Console.WriteLine("orm_handleRepairMissionRepairObjective #4");

            //Add credit for player's loads to that objective
            if (foundMO.PlayersWhoRepairedNamesTimes.ContainsKey(player.Name()))
            {
                foundMO.PlayersWhoRepairedNamesTimes[player.Name()] += numLoads;
            }
            else
            {
                foundMO.PlayersWhoRepairedNamesTimes[player.Name()] = numLoads;
            }

            Tuple<string, DateTime> stl = mainmission.showTimeLeft(player: null, showMessage: false, inGameTimeOnly: true);
            string inGameTime = stl.Item1;

            string nl = "<br>" + Environment.NewLine;

            RepairsFerries_thisStatsPeriod[army] += string.Format("{0}: {1} delivered {2:n0} loads of {4} to {3}", inGameTime, player.Name(), numLoads, foundMO.Name, taskName) + nl;

            //We update repair mission info for player with the actual number of loads delivered (used later to go the player the right stats)
            orm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>(playerRepairInfo.Item1, playerRepairInfo.Item2, playerRepairInfo.Item3, playerRepairInfo.Item4, numLoads, playerRepairInfo.Item6);

            Console.WriteLine("orm_handleRepairMissionRepairObjective #5");

            //Make delivery trucks show up (for 30 secs).
            if (deliveryType == DeliveryType.land_and_unload && aircraft != null)
            {
                double heading = aircraft.getParameter(part.ParameterTypes.Z_Orientation, 0);
                List<string> truck_ids = StatCalcs.createTruck(GamePlay, player_pos.x, player_pos.y, heading);
                //Task.Delay(30000).ContinueWith(t => StatCalcs.destroyTruck(GamePlay, player_pos.x, player_pos.y, heading, truck_ids));
                Task.Delay(30000).ContinueWith(t => StatCalcs.destroyTruck(GamePlay, player_pos.x, player_pos.y, heading, truck_ids));
                //.ContinueWith(t => DoSomething(), TaskScheduler.FromCurrentSynchronizationContext());
            }

            foundMO.fixDestructionValues();
            if (repairType == RepairType.Repair_Load)
            {
                //Airfields are a bit complicated.  You want to #1. move up the timeToUndestroy. #2. Reduce Destroyed Percent
                //#3. Reduce AirfieldDamagePoints by the same factor
                //we might also need to deal with the airfieldTargets dictionary, which duplicates a bunch of that same info :-(
                //RADAR we can just treat the same, except for removing craters.
                //plus for airdrops but drop a flare @ point of drop

                //bhugh temp XX2021/10 - repair times greatly increased in campaign.cs 
                //so here repair times also increased
                //So full load (player + 10 bombers) can repair an objective.  We'll say.
                //2021-11 leaving this in place
                //point_fact makes high-level (high point) OBJs harder to repair.  Just as they are 
                //harder to destroy.  The idea is that an airfield is "par" and things are harder/
                //easier than the in proportion.  airfields are 9 points  but that is a bit
                //exaggerated, relaly they probably should be more like 6-7 points.
                double point_fact = 7 / foundMO.Points;
                if (point_fact > 1.4) point_fact = 1.4;
                double repairMult = 5;
                double reduction_percent = 1 - Math.Pow(.70, numLoads) * point_fact;  //how much, in percentage points, to reduce the damage to this OBJ.  So say r_p = 1 would reduce damage by 100%, or say from 50% to -50%. Each load reduces damage by abt 30%, but reduced effect when damage is smaller. Was .88, now .75 2021-10, now .76 2021-12 - now .70 2022/11
                double maximum_percent_after_reduction = 1 - .08 * numLoads * point_fact; //% of 100%, ie full damage amount rather than the current  existing damage
                                                                             //was 1-.06 but 2021-10 making it 1-.090909 so 11 loads fixes 100%
                                                                             //2021-12, making it 1-.09 so that 11 loads does not QUITE fix it 100%.
                if (maximum_percent_after_reduction < 0) maximum_percent_after_reduction = 0;
                double minimum_reduction_hours = 9 * numLoads * point_fact; //was 6, 2021-10, now 12 - 2022-11 now 14
                double percent_destroyed_after_reduction = 0;
                //AIRFIELD special thing
                if (foundMO.MOObjectiveType == Mission.MO_ObjectiveType.Military_Airfield && foundMO.AirfieldPointsRequired > 0)
                {
                    //airfield damage points are only used by airports, and it is better to 
                    //figure them out first & then figure DestroyedPercent from that.  For airports.
                    double reductionPoints = foundMO.AirfieldPointsRequired * reduction_percent;
                    double newDamagePoints = foundMO.AirfieldDamagePoints - reductionPoints;

                    Console.WriteLine("Airfield repair: reduction percent {0:f2}, reduction points {1:f2}, newdamagepoints {2:f2}, afpointsreq {3:f0}, afdamagepoints: {4:f0}", reduction_percent, reductionPoints, newDamagePoints, foundMO.AirfieldPointsRequired, foundMO.AirfieldDamagePoints);

                    //So if the Damage Points becomes negative, we give them diminishing returns
                    //on the amount of "excess repaires" they can do.  
                    //It is basically 40% credit for first  290 excess points, then 
                    //Formula for "excess repair" points <0 is 6.8*(sqrt(abs(negative points))).  Inverse is, (pts/6.8)^2
                    //update, that seems to stingy, so moving to a formula that uses x^0.7 instead
                    //of x^0.5, and gives 50% credit for the first 100% excess repairs:
                    // 0.5 * (apr/apr^0.7) * x^0.7
                    if (newDamagePoints < 0)
                    {
                        double fact = 5;
                        if (foundMO.AirfieldPointsRequired != 0) fact = 0.5 * foundMO.AirfieldPointsRequired / Math.Pow(Math.Abs(foundMO.AirfieldPointsRequired),0.7); // 6.8; //AirfieldDamagePointsRequired is 180 
                        if (foundMO.AirfieldDamagePoints < 0)
                        {
                            newDamagePoints = -fact * Math.Pow(Math.Pow(Math.Abs(foundMO.AirfieldDamagePoints) / fact, 10.0/7.0) + Math.Abs(reductionPoints), 0.7);
                            Console.WriteLine("Airfield repair ndp<0 afd<0: fact {0:f3}, newDamagePoints {1:f2}", fact, newDamagePoints);
                        }
                        else
                        {
                            double excessReductionPoints = reductionPoints - foundMO.AirfieldDamagePoints;
                            newDamagePoints = -fact * Math.Pow(Math.Abs(excessReductionPoints), 0.7);
                            Console.WriteLine("Airfield repair ndp<0 afd>=0: fact {0:f3}, newDamagePoints {1:f2}", fact, newDamagePoints);
                        }
                    }

                    double minDamagePoints = foundMO.AirfieldPointsRequired * maximum_percent_after_reduction;
                    if (minDamagePoints < 0) minDamagePoints = 0; //Here we will never go below zero.  This is a sort of minimum reward the pilot gets for flying a repair mission.
                    if (newDamagePoints > minDamagePoints) newDamagePoints = minDamagePoints;
                    if (foundMO.AirfieldDamagePoints < newDamagePoints) newDamagePoints = foundMO.AirfieldDamagePoints; //should ever happen, but just in case - we will never INCREASE damage points somehow
                    //if (newDamagePoints < 0) newDamagePoints = 0;
                    foundMO.AirfieldDamagePoints = newDamagePoints;
                    //if (foundMO.AirfieldDamagePoints < 5) foundMO.AirfieldDamagePoints = 5; //we always leave a little damage]
                    double apr = foundMO.AirfieldPointsRequired;
                    if (apr == 0) apr = 180;
                    percent_destroyed_after_reduction = foundMO.adjustDestroyedPercent ( foundMO.AirfieldDamagePoints / apr);
                    //if (foundMO.DestroyedPercent < 0) foundMO.DestroyedPercent = 0;
                    
                    Console.WriteLine("airfield repair final: minDamagePoints {0:f2}, newdamagepoint {1:f2}, foundMO.ADP {2:f2}, foundMO.DP {3:f2}, foundMO.apr {4:f2}", minDamagePoints, newDamagePoints, foundMO.AirfieldDamagePoints, foundMO.DestroyedPercent, foundMO.AirfieldPointsRequired);
                }
                else
                {

                    percent_destroyed_after_reduction = foundMO.DestroyedPercent - reduction_percent;

                    //Allows negative destroyed percentages, but "with a discount"
                    if (percent_destroyed_after_reduction < 0)
                    {
                        //100% is full percent destroyed and that = 1, so the formula becomes:
                        double fact = 0.5 * 1 / Math.Pow(1, 0.7);

                        if (foundMO.DestroyedPercent < 0)
                        {
                            percent_destroyed_after_reduction = -fact * Math.Pow(Math.Pow(Math.Abs(foundMO.DestroyedPercent) / fact, 10.0/7.0) + Math.Abs(reduction_percent), 0.7);
                            Console.WriteLine("Obj repair: pdar<0 PD<0: pecdesaftred {0:f2}, fact {1:f2}, reductionpercent {2:f2}, foundMO.DP {3:f2}", percent_destroyed_after_reduction, fact, reduction_percent, foundMO.DestroyedPercent);
                        }
                        else
                        {
                            double excessReductionPercent = reduction_percent - foundMO.DestroyedPercent;
                            percent_destroyed_after_reduction = -fact * Math.Pow(Math.Abs(excessReductionPercent), 0.7);
                            Console.WriteLine("Obj repair: pdar<0 PD>=0: pecdesaftred {0:f2}, fact {1:f2}, reductionpercent {2:f2}, foundMO.DP {3:f2}, excessreductionpercent {4:f2}", percent_destroyed_after_reduction, fact, reduction_percent, foundMO.DestroyedPercent, excessReductionPercent);
                        }


                    }

                    if (percent_destroyed_after_reduction > maximum_percent_after_reduction) percent_destroyed_after_reduction = maximum_percent_after_reduction;
                    foundMO.adjustDestroyedPercent(percent_destroyed_after_reduction);
                    //if (foundMO.DestroyedPercent < 0) foundMO.DestroyedPercent = 0;
                    //if (foundMO.DestroyedPercent < 0.04) foundMO.DestroyedPercent = 0.04; //we always leave a little % damaged. Will be repaired when the "time to undestroy" is up

                    Console.WriteLine("OBJ repair final: pctdesaftred {0:f2}, maxpctaftred {1:f2}, foundMO.DP {2:f2}", percent_destroyed_after_reduction, maximum_percent_after_reduction, foundMO.DestroyedPercent);
                }

                Console.WriteLine("orm_handleRepairMissionRepairObjective #6");

                TimeSpan time_remaining = TimeSpan.Zero;
                if (foundMO.TimeToUndestroy_UTC.HasValue) time_remaining = foundMO.TimeToUndestroy_UTC.Value.Subtract(currTime_UTC);

                double hours_left = time_remaining.TotalHours;
                if (hours_left < 0 || !foundMO.Destroyed) hours_left = 0; //timetoundestroy is meaniningless unless the MO is actually destroyed
                double hours_left_after_reduction = percent_destroyed_after_reduction * hours_left; //12% reduction for each load.  If done 5X it will half repair time
                if (hours_left_after_reduction > hours_left - minimum_reduction_hours) hours_left_after_reduction = hours_left - minimum_reduction_hours; //but always 6-hour time reduction AT LEAST
                if (hours_left <= 0 || !foundMO.Destroyed) hours_left_after_reduction = 0; //timetoundestroy is meaningless unless the MO is actually destroyed - if we don't set this to 0 we get nonsense hours time reduction
                double time_until_repaired_hours = hours_left_after_reduction;
                if (time_until_repaired_hours < 0.25) time_until_repaired_hours = 0.25; //getting all fixed < 30 mins is unrealistic
                foundMO.TimeToUndestroy_UTC = currTime_UTC.AddHours(time_until_repaired_hours);



                //AIRFIELD special thing
                if (foundMO.MOObjectiveType == Mission.MO_ObjectiveType.Military_Airfield)
                {
                    if (foundMO.DestroyedPercent >= 0) mainmission.restoreAirfield(foundMO, (int)Math.Round(percent_destroyed_after_reduction * 100)); //removes some craters etc right now
                }

                //Air Drop special thing - flare on the ground
                else if (deliveryType == DeliveryType.air_drop)
                {
                    //For radar and other air drops, we drop a flare at the spot where the player dropped the 'parachutes' with the supplies
                    double wait = 11.5;
                    if (parachute_pos.z > 10) wait = parachute_pos.z / 120;  //person's terminal velocity is 50 m/s, we'll say something like a flare is a bit higher, say 120
                    //parachute_pos.z is AGL
                    Timeout(wait, () =>
                    {
                        Calcs.loadCratersAndSmoke(GamePlay, mainmission, target_pos.x, target_pos.y, 0, "BuildingFireSmall");  //this is the smallest type of smoke  "BuildingFireLarge" a bit larger.  Smoke1 Smoke2 BigSitySmoke etc all larger yet
                    });
                    Timeout(forwardTime_s, () =>
                    {
                        parachute.dropParachute(parachute_pos, army: player.Army(), z_add_m: -100, z_add2_m: -100, vwld: vwld, vel_mps: vel_mps * 0.8);
                    });
                }

                Console.WriteLine("orm_handleRepairMissionRepairObjective #7");

                Timeout(forwardTime_s + 0.25, () =>
                    { //wait until after other messages to send these.
                      //playerRepairInfo.Item5
                        string loadExpl = "1 Repair Load";
                        if (numLoads > 1) loadExpl = string.Format("{0:n0} Repair Loads", numLoads);
                        GamePlay.gpLogServer(new Player[] { player }, ">>>You started with {0:n0} loads of {1}. Your loads will assist " + foundMO.Name + ".", new object[] { totalNumCoverACCheckedOut + 1, taskName }); //playerRepairInfo.Item5 is BROKEN for some reason and always seems to be just 1.
                        string addReserve = "";
                        if (percent_destroyed_after_reduction < -1) addReserve = String.Format(", including {0:n0}% repair reserves laid up against future damage.", Math.Abs(percent_destroyed_after_reduction * 100));
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Your {0} will help reduce damage to  " + (percent_destroyed_after_reduction * 100.0).ToString("n1") + "%", new object[] { loadExpl });
                        if (time_remaining.TotalSeconds > 0)
                        {
                            if (hours_left_after_reduction > 0) GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Your {2} will speed up repair by {0:n1} hours; repairs will be completed in {1:n1} hours now!", new object[] { hours_left - time_until_repaired_hours, time_until_repaired_hours, loadExpl }), null);
                            else GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Your {2} means repairs will be completed in the next few minutes!", new object[] { hours_left - time_until_repaired_hours, time_until_repaired_hours, loadExpl }), null);
                        }

                    });
            }
            else if (repairType == RepairType.Defense_Load)
            {

                Console.WriteLine("orm_handleRepairMissionRepairObjective #7");
                //HERE is where we need to put code to implement whatever it is we are going to implement  for defense troops
                var retrn = foundMO.addDefenseUnits(numLoads);
                int totalDUs = retrn.Item1;
                double repairTimeSaved_hr = retrn.Item2;

                Timeout(forwardTime_s + 0.25, () =>
                { //wait until after other messages to send these.
                  //playerRepairInfo.Item5
                    string loadExpl = "1 load of Defense Troops & gear";
                    if (numLoads > 1) loadExpl = string.Format("{0:n0} loads of Defense Troops & gear", numLoads);
                    GamePlay.gpLogServer(new Player[] { player }, ">>>You started with {0:n0} loads of {1}. Your loads will assist " + foundMO.Name + ".", new object[] { totalNumCoverACCheckedOut + 1, taskName }); //playerRepairInfo.Item5 is BROKEN for some reason and always seems to be just 1.

                    GamePlay.gpLogServer(new Player[] { player }, ">>>Your {0} will help bolster the defense of this area - providing additional AA coverage - and provide troops to perform any needed repairs faster, for 13 days", new object[] { loadExpl });

                    if (repairTimeSaved_hr > 0) GamePlay.gpLogServer(new Player[] { player }, ">>>Your {0} sped up repairs at {1} by {2} hours", new object[] { loadExpl, foundMO.Name, String.Format("{0:f1}", repairTimeSaved_hr) });

                    GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>{0} now has {1} total defense units. ", new object[] { foundMO.Name, totalDUs }), null);

                });

                if (deliveryType == DeliveryType.air_drop)
                {
                    //For radar and other air drops, we drop a flare at the spot where the player dropped the 'parachutes' with the supplies
                    
                    double wait = 10;
                    if (parachute_pos.z > 10) wait = parachute_pos.z / 120;  //person's terminal velocity is 50 m/s, we'll say something like a flare is a bit higher, say 120
                    Timeout(wait, () =>
                    {
                        Calcs.loadCratersAndSmoke(GamePlay, mainmission, target_pos.x, target_pos.y, 0, "BuildingFireSmall");  //this is the smallest type of smoke  "BuildingFireLarge" a bit larger.  Smoke1 Smoke2 BigSitySmoke etc all larger yet
                    });

                    //NOTE: parachute_pos.z is AGL - needed for spawn in of A/C as that is via AGL!!!
                    //We wait some time here to try to get the drop point lined up roughly
                    //with the bomb site
                    Timeout(forwardTime_s, () =>
                    {
                        parachute.dropParachute(parachute_pos, army: player.Army(), z_add_m: -100, z_add2_m: -100, vwld: vwld, vel_mps: vel_mps * 0.8);
                    });
                }

            }

        return true;
        }
        catch (Exception ex) { Console.WriteLine("REPAIR mission handle repair load complete ERROR: " + ex.ToString());
            GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER REPAIR!!!!! Please inform Flug.", null);
            return false; }
    }

    private bool orm_handleRepairMissionFerryComplete(Player player, bool testing = false, Point3d? deliveryTestPoint = null, bool dadvice = false)
    {
        try
        {
            Point3d player_pos = player.Place().Pos();
            if (deliveryTestPoint.HasValue) player_pos = deliveryTestPoint.Value;

            int army = player.Army();
            bool ret = false; 
            double closest_dist_m = 10000000;
            Mission.MissionObjective foundMO = null;
            DateTime currTime_UTC = DateTime.UtcNow;
            double dist_to_check_fromLG_m = orm_maxLandingPointtoObjectiveDistance_m; //using same as objective airport for now, means landed at OR NEAR the airport        

            var playerRepairInfo = orm_PlayersOnRepairMissionExpiration[player];
            int numLoads = playerRepairInfo.Item5;
            AiAircraft aircraft = playerRepairInfo.Item3;
            string aircraft_name = RepairCalcs.GetAircraftType(aircraft);

            //so we count any cover aircraft still alive AND within 15000m of the player at this moment, as additional loads
            int numCoverACCheckedOut = 0;
            int totalNumCoverACCheckedOut = 0;
            double distanceToCountCoverRepair = orm_maxDistanceForCoverToCount_m;
            if (testmode) distanceToCountCoverRepair = 0; //meaning, ignore the distance requirement, just return ALL cover a/c

            if (mainmission.covermission != null)
            {
                numCoverACCheckedOut = mainmission.covermission.numberAircraftCurrentlyCheckedOutPlayer(player, dist_m: distanceToCountCoverRepair); //13km =  ~8 miles.  Cover a/c are not too well behaved
                numLoads = numCoverACCheckedOut + 1; // +1 for the player's own a/c
                totalNumCoverACCheckedOut = mainmission.covermission.numberAircraftCurrentlyCheckedOutPlayer(player, dist_m: 0);
            } else
            {
                Console.WriteLine("REPAIR MISSION ERROR (Ferry complete): mainmission.covermission is NULL, cannot check current cover aircraft!!!!!");
                numLoads = playerRepairInfo.Item5; //not the best option but better than just counting NOTHING here
                //playerRepairInfo.Item5 is all aircraft checked out with repair loads/ferry, INCLUDING the player's own aircraft
            }

            //We allow maximum of loads of the amount the player originally checked out (little cheater check)
            //if (numLoads > playerRepairInfo.Item5) numLoads = playerRepairInfo.Item5;
            //2022-04-13 - skipping the above in case it is causing a bug of some kind.

            if (numLoads < 1) numLoads = 1; //Just little sanity check, should never be <1

            if (totalNumCoverACCheckedOut - (numLoads - 1) > 0)
            {

                GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: " + (totalNumCoverACCheckedOut - (numLoads - 1)).ToString("F0") + " of your cover aircraft were too far away from you, or lost en route.  Unfortunately, they were lost to supply.", new object[] { });
            }

            if (numLoads > 0)
            {
                if (!dadvice)
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: You ferried " + numLoads.ToString("F0") + " aircraft of type " + aircraft_name + " - requesting their addition to your supply.", new object[] { });
                else
                    GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: Training ferry drop SUCCESSFUL. You would have successfully ferried " + numLoads.ToString("F0") + " aircraft of type " + aircraft_name + ". Use command <de to make a real delivery.", new object[] { });
            }

            if (dadvice) return false;

            var numresults = new Tuple<int, int>(0, 0);

            //tuple is <numadded, total now in supply>
            if (mainmission.supplymission != null) numresults = mainmission.supplymission.addAircraftToSupplyFerry(player, aircraft_name, howMany: numLoads);

            int numadded = numresults.Item1;
            int totalsupply = numresults.Item2;
            bool lg_added = false;

            Tuple<string, DateTime> stl = mainmission.showTimeLeft(player: null, showMessage: false, inGameTimeOnly: true);
            string inGameTime = stl.Item1;

            if (numLoads >= 0 && mainmission.landinggroundmission != null)
            {
                var lgm = mainmission.landinggroundmission;
                double lgdist = lgm.distancetoNearestLandingGround(player_pos);
                if (lgdist <= dist_to_check_fromLG_m)
                {
                    if (numLoads < 4)
                    {
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: You landed at a Landing Ground with a new aircraft to add to that Landing Grounds' inventory.", new object[] { });
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: However, you delivered only {0} aircraft and adding an aircraft to an LG's inventory requires at least 4 aircraft delivered.", new object[] { numLoads });
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: Sorry!", new object[] { });

                    }
                    else
                    {
                        lgm.addAircraftToLandingGround(player_pos, aircraft);
                        lgm.extendLandingGroundLifetime(player_pos, 21 * 24);
                        lg_added = true;
                        RepairsFerries_thisStatsPeriod[army] += string.Format("{0}: {1} delivered {2:n0} aircraft of type {3} to a Landing Ground - adding that type to the LG's supply.", inGameTime, player.Name(), numadded, aircraft_name);

                        Timeout(5, () =>
                        {
                            GamePlay.gpLogServer(new Player[] { player }, ">>Ferry: You landed at a Landing Ground with a new aircraft to add to that Landing Grounds' inventory.", new object[] { });
                            //GamePlay.gpLogServer(new Player[] { player }, "Ferry: you successfully delivered {0} of your aircraft.", new object[] { numLoads });
                            GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: The new aircraft type {0} has been added to this LG, and the LG has been renewed until 21 days from now!", new object[] { Calcs.GetAircraftType(aircraft) });
                        });
                        
                    }

                }
            }

            if (numadded > 0)
            {

                GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: Permission granted to add " + numadded.ToString("F0") + " aircraft of type " + aircraft_name + " to your supply.", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: The supply of {0} is now {1:n0}.", new object[] { aircraft_name, totalsupply });
                // + AircraftSupply[(ArmiesE)army][planeKey].ToString("F0")



                string nl = "<br>" + Environment.NewLine;

                RepairsFerries_thisStatsPeriod[army] += string.Format("{0}: {1} delivered {2:n0} aircraft of type {3} to a forward base (supply now {4:n0})", inGameTime, player.Name(), numadded, aircraft_name, totalsupply) + nl;

                //We update repair mission info for player with the actual number of loads delivered (used later to go the player the right stats)
                orm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>(playerRepairInfo.Item1, playerRepairInfo.Item2, playerRepairInfo.Item3, playerRepairInfo.Item4, numLoads, playerRepairInfo.Item6);


                return true;
            }
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Ferry: Something went wrong with your ferry mission for " + aircraft_name + " aircraft. No aircraft were added to supply. Please notify HQ!", new object[] { });
                return false;
            }

            
        }
        catch (Exception ex) { Console.WriteLine("REPAIR mission ferry complete ERROR: " + ex.ToString()); return false; }

    }

    private bool orm_isPlayerAllowedToAbandon(Player player, bool testing = false)
    {
        if (player == null) return false;
        if (player.Place() == null || player.Place() as AiCart == null) return false;
        if (player.Place() as AiAircraft == null)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be in an aircraft to abandon your loads (when you left your aircraft, your supplies were abandoned immediately).", null);
            return false;
        }
        AiAircraft aircraft = player.Place() as AiAircraft;

        if (!orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must first pick up a cargo/repair/defense load before abandoning it (you don't have a cargo load at this time).", null);
            return false;
        }
        
        AiAircraft pickupAircraft = orm_PlayersOnRepairMissionExpiration[player].Item3;
        DateTime expiration_time = orm_PlayersOnRepairMissionExpiration[player].Item1;
        Point3d pickupPoint = orm_PlayersOnRepairMissionExpiration[player].Item2;
        string playerLoad = orm_PlayersOnRepairMissionExpiration[player].Item4;
        RepairType repairType = orm_PlayersOnRepairMissionExpiration[player].Item6;

        TimeSpan time_left = expiration_time.Subtract(DateTime.UtcNow);

        if (pickupAircraft != aircraft) return true; //if in a different a/c must abandon immediately        
        
        double altAGL_m = 0;
        altAGL_m = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
        if ((aircraft as AiActor).Pos().z > 2000 || altAGL_m > 20)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be **on the ground** to abandon your {0}.", new object[] { RepairType_names[repairType] });
            return false;
        }

        if (Calcs.CalculatePointDistance((player.Place() as AiAircraft).AirGroup().Vwld()) > 2)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Sorry, you must be stopped to abandon your {0} (you are moving).", new object[] { RepairType_names[repairType] });
            return false;
        }

        return true;
    }

    public void orm_handlePickupRequest(Player player, RepairType repairType = RepairType.Repair_Load,   double delay_s = 7, bool testing = false)
    {
        Console.WriteLine("Objective Repair Mission pickup command received.");


        if (!orm_isPlayerAllowedRepairPickup(player, repairType: repairType) && !testing ) return;

        if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
        {

            GamePlay.gpLogServer(new Player[] { player }, ">>>You had a previous incomplete Repair/Defense Mission - it has been disbanded.", new object[] { });

        }

        if (mainmission.covermission != null) mainmission.covermission.landCoverAircraft(player, fromRepair: true); //shed any existing <cover aircraft

        string playerLoad = Calcs.randSTR(orm_RepairPickup_items);
        if (repairType == RepairType.Defense_Load) playerLoad = Calcs.randSTR(orm_DefensePickup_items);
        AiAircraft aircraft = player.Place() as AiAircraft;

        Point3d savePlayerPlace_pos = player.Place().Pos();
        //We use the pos to doubliecheck same repair flight before deleting the dictdionary entry later
        //But if they spawn int othe same airport/spawnpoint again the x,y could be teh same.
        //Thus, added random #s
        savePlayerPlace_pos.x += ran.NextDouble();
        savePlayerPlace_pos.y += ran.NextDouble();

        orm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>(DateTime.UtcNow.AddMinutes(orm_maxDeliveryTime_min), savePlayerPlace_pos, aircraft, playerLoad, 1, repairType);
        GamePlay.gpLogServer(new Player[] { player }, ">>>You have picked up your {0} - " + playerLoad, new object[] {RepairType_names[repairType] });
        GamePlay.gpLogServer(new Player[] { player },string.Format(">>>{0:n0} minutes to complete delivery.", new object[] { orm_maxDeliveryTime_min }), null);
        GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Delivery aircraft are allocated and filled to the minimum fuel level for your a/c ({0}). Monitor fuel usage carefully.", new object[] { aircraft.GetMinimumFuelInPercent() }), null);
        aircraft.RefuelPlane(aircraft.GetMinimumFuelInPercent());

        string sector = Calcs.correctedSectorNameDoubleKeypad(this, (aircraft as AiActor).Pos());
        Timeout(5.32, () =>
        {

            GamePlay.gpLogServer(null, ">>>" + player.Name() + " has just picked up a vital Objective {0} in sector " + sector, new object[] {RepairType_names[repairType] });
        });
        Timeout(3.32, () =>
        {

            GamePlay.gpLogServer(new Player[] { player }, ">>>Repair: To maximize cargo load, your fuel is loaded at the minimum allowed. Fly carefully!", new object[] { });
        });

        Timeout(7.32, () =>
        {

            GamePlay.gpLogServer(new Player[] { player }, ">>>Repair: You can now add cover aircraft to your cargo squadron - <cover 1 2 etc", new object[] { });
        });

        //failsafe to remove the player from the dictionary
        //If the player is in the dictionary, the knickebein & aerial radar won't work
        //BUT we don't want to remove any NEW record the player has added to the dictionary 
        // We failsafe this by removing only if player place is identical.

        Timeout((orm_maxDeliveryTime_min + 5) * 60, () =>
         {
             if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player)  
                 && savePlayerPlace_pos.x == orm_PlayersOnRepairMissionExpiration[player].Item2.x
                 && savePlayerPlace_pos.y == orm_PlayersOnRepairMissionExpiration[player].Item2.y
                 && savePlayerPlace_pos.z == orm_PlayersOnRepairMissionExpiration[player].Item2.z)
             {
                 orm_PlayersOnRepairMissionExpiration.Remove(player);
             }
         });


    }

    //If the player addes a cover ac while flying, and at the original <pickup or <ferry airport
    //Then those cover a/c are allowed & add a load for each cover a/c added
    public bool orm_handleCoverPickupOrFerryRequest(Player player, int numCoverACadded = 1)
    {
        Console.WriteLine("Objective Repair Mission COVER pickup/ferry command received for " + player.Name() + " - # cover/repair planes added: " + numCoverACadded.ToString("n0"));

    try
        {
            
            if (!orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
            {

                GamePlay.gpLogServer(new Player[] { player }, ">>>You must first <pickup your own Repair Load or start a <ferry before your Repair Mission Cover Aircraft can be created.", new object[] { });
                return false;

            }

            var playerRepairInfo = orm_PlayersOnRepairMissionExpiration[player];

            if (!orm_isPlayerAllowedRepairPickup(player, coverPickup: true, repairType: playerRepairInfo.Item6)) return false;

            
            RepairType repairType = playerRepairInfo.Item6;
            string typeName = "Repair Load";
            string loadsName = "loads of Repair Cargo";
            if (repairType == RepairType.Ferry) { typeName = "Ferry"; loadsName = "Ferry Aircraft"; }
            if (repairType == RepairType.Defense_Load) { typeName = "Defense Load"; loadsName = "Loads of Defense Troops & Cargo"; }

            DateTime expiration_time = playerRepairInfo.Item1;
            TimeSpan time_left = expiration_time.Subtract(DateTime.UtcNow);
            if (time_left.TotalSeconds <= 0)
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Your {0} has expired - you can't pick up any new Cover Repair Load Aircraft.", new object[] { typeName });
                orm_PlayersOnRepairMissionExpiration.Remove(player);
                return false;

            }

            string playerLoad = playerRepairInfo.Item4;
            AiAircraft aircraft = player.Place() as AiAircraft;
            int numLoaded = playerRepairInfo.Item5 + numCoverACadded;
            DateTime deliveryTimeLimit = playerRepairInfo.Item1;

            //we reset the 1 hour time limit
            orm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>(playerRepairInfo.Item1, playerRepairInfo.Item2, playerRepairInfo.Item3, playerRepairInfo.Item4, numLoaded, playerRepairInfo.Item6);

            Timeout(5.4, () =>
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>You have picked up {0:n0} additional {1} via Cover Aircraft (" + playerLoad + ")", new object[] { numCoverACadded, loadsName });
                GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>You now have {0} total {1}.", new object[] { numLoaded, loadsName }), null);
            });

            return true;
        }
        catch (Exception ex) { Console.WriteLine("REPAIR mission cover handle repair or ferry ERROR: " + ex.ToString()); return false; }
    }

    public void orm_handleFerryRequest(Player player, double delay_s = 7)
    {
        Console.WriteLine("Objective Repair Mission FERRY command received.");

        if (!orm_isPlayerAllowedRepairPickup(player, repairType: RepairType.Ferry)) return;

        if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>You had a previous incomplete Repair or Ferry Mission - it has been disbanded.", new object[] { });
        }

        //string playerLoad = Calcs.randSTR(orm_RepairPickup_items);
        AiAircraft aircraft = player.Place() as AiAircraft;
        string playerLoad = RepairCalcs.GetAircraftType(aircraft);

        if (mainmission.covermission != null) mainmission.covermission.landCoverAircraft(player, fromRepair: true); //any cover they have at this moment is gone.

        Point3d savePlayerPlace_pos = player.Place().Pos();
        //We use the pos to doubliecheck same repair flight before deleting the dictdionary entry later
        //But if they spawn int othe same airport/spawnpoint again the x,y could be teh same.
        //Thus, added random #s
        savePlayerPlace_pos.x += ran.NextDouble();
        savePlayerPlace_pos.y += ran.NextDouble();

        orm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>(DateTime.UtcNow.AddMinutes(orm_maxDeliveryTime_min), savePlayerPlace_pos, aircraft, playerLoad, 1, RepairType.Ferry);
        GamePlay.gpLogServer(new Player[] { player }, ">>>You have picked up your ferry aircraft - " + playerLoad, new object[] { });
        GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>{0:n0} minutes to complete delivery.", new object[] { orm_maxDeliveryTime_min }), null);        
        aircraft.RefuelPlane(aircraft.GetMinimumFuelInPercent());

        string sector = Calcs.correctedSectorNameDoubleKeypad(this, (aircraft as AiActor).Pos());
        Timeout(5.32, () =>
        {

            GamePlay.gpLogServer(null, ">>>" + player.Name() + " has just launched Ferry Squadron in sector " + sector, new object[] { });
        });
        Timeout(3.32, () =>
        {

            //GamePlay.gpLogServer(null, ">>>Ferry: Because of fuel shortages, your fuel is loaded at the minimum allowed. Fly carefully!", new object[] { });
            GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>Ferry aircraft are loaded at the minimum fuel allowed fuel level ({0}). Fly carefully!", new object[] { aircraft.GetMinimumFuelInPercent() }), null);
        });

        Timeout(7.32, () =>
        {

            GamePlay.gpLogServer(null, ">>>Ferry: You can now add cover aircraft to your ferry squadron - <cover 1 2 etc", new object[] { });
        });

        //failsafe to remove the player from the dictionary
        //If the player is in the dictionary, the knickebein & aerial radar won't work
        //BUT we don't want to remove any NEW record the player has added to the dictionary 
        // We failsafe this by removing only if player place is identical.

        Timeout((orm_maxDeliveryTime_min + 5) * 60, () =>
        {
            if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player)
                && savePlayerPlace_pos.x == orm_PlayersOnRepairMissionExpiration[player].Item2.x
                && savePlayerPlace_pos.y == orm_PlayersOnRepairMissionExpiration[player].Item2.y
                && savePlayerPlace_pos.z == orm_PlayersOnRepairMissionExpiration[player].Item2.z)
            {
                orm_PlayersOnRepairMissionExpiration.Remove(player);
            }
        });
    }

    public void orm_handleAbandonRequest(Player player, bool testing = true)
    {
        Console.WriteLine("Objective Repair Mission abandon command received.");

        var repairType = RepairType.Repair_Load;

        if (!orm_isPlayerAllowedToAbandon(player, testing))
        {


            if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>To abandon your load, you must land and come to a full stop. Then use chat command <ab", new object[] { });
                //GamePlay.gpLogServer(new Player[] { player }, ">>>If you want to abandon your load: Land, then use chat command <ab", new object[] { time_left.TotalMinutes });
                repairType = orm_PlayersOnRepairMissionExpiration[player].Item6;

                DateTime expiration_time = orm_PlayersOnRepairMissionExpiration[player].Item1;
                TimeSpan time_left = expiration_time.Subtract(DateTime.UtcNow);
                if (time_left.TotalSeconds > 0)
                    GamePlay.gpLogServer(new Player[] { player }, ">>>You have " + time_left.TotalMinutes.ToString("n0") + " minutes remaining to complete your {0} Mission", new object[] { RepairType_names[repairType] });

            }
            Console.WriteLine("Objective Repair Mission: Not allowed abandon.");
            return;
        }

        string playerLoad = "";
        int numLoads = 1;
        

        if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
        {
            playerLoad = orm_PlayersOnRepairMissionExpiration[player].Item4;
            numLoads = orm_PlayersOnRepairMissionExpiration[player].Item5;
            repairType = orm_PlayersOnRepairMissionExpiration[player].Item6;
            orm_PlayersOnRepairMissionExpiration.Remove(player);
        }

        if (mainmission.covermission != null) mainmission.covermission.landCoverAircraft(player, fromRepair: true);

        //osm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, AiAircraft, String>(DateTime.UtcNow, aircraft, playerLoad);
        string loadExpl = "Repair Load cargo";
        string loadXXX = "";
        if (numLoads > 1)
        {
            loadExpl = string.Format("{0:n0} Repair Loads", numLoads);
            loadXXX = " X" + numLoads.ToString("n0");
        }

        if (repairType == RepairType.Ferry)
        {
            loadExpl = string.Format("{0:n0} Ferry Aircraft", numLoads);
        }
                
        GamePlay.gpLogServer(new Player[] { player }, ">>>You have successfully abandoned your {0} - " + playerLoad+ loadXXX, new object[] { loadExpl});

    }

    //public void orm_handleDeliveryRequest(Player player, bool testing = false, Mission.MO_ObjectiveType objType = Mission.MO_ObjectiveType.Military_Airfield)
    public void orm_handleDeliveryRequest(Player player, bool testing = false, DeliveryType deliveryType = DeliveryType.land_and_unload, Point3d? deliveryTestPoint = null, bool dadvice = false)
    {
        Console.WriteLine("Objective Repair Mission delivery command received.");
    
        if (!orm_PlayersOnRepairMissionExpiration.ContainsKey(player) && !testing && !dadvice)
        {
            GamePlay.gpLogServer(new Player[] { player }, ">>>Repair: You can't deliver defense/repair loads or ferry aircraft - you have not picked up a load or started a ferry mission yet.  Sorry! (<pi or <fe)", new object[] { });
            return;
        }

        RepairType repairType = RepairType.Repair_Load; //default for testing
        try
        {
            if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player)) repairType = orm_PlayersOnRepairMissionExpiration[player].Item6;
        }
        catch (Exception ex)
        {
            Console.WriteLine("REPAIR mission handle repair load VERY early ERROR: " + ex.ToString());
            repairType = RepairType.Repair_Load;
        }

        try
        {
            if (!orm_isPlayerAllowedRepairDelivery(player, testing, deliveryType, repairType: repairType, deliveryTestPoint: deliveryTestPoint, dadvice:dadvice) || !orm_handleRepairMissionRepair(player, testing, deliveryType, deliveryTestPoint: deliveryTestPoint, dadvice: dadvice))
            {
                if (dadvice)
                {
                    Timeout(2, () =>
                    {
                        GamePlay.gpLogServer(new Player[] { player }, ">>>Delivery training advice complete. Use command <de to actually deliver your load.", new object[] { });
                    });
                    return;
                }

                if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
                {
                    DateTime expiration_time = orm_PlayersOnRepairMissionExpiration[player].Item1;
                    TimeSpan time_left = expiration_time.Subtract(DateTime.UtcNow);
                    if (time_left.TotalSeconds > 0)
                        GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>You have {0:n0} minutes remaining to complete your {1}", new object[] { time_left.TotalMinutes, RepairType_names[repairType]  }), null);
                    GamePlay.gpLogServer(new Player[] { player }, ">>>If you want to abandon your load: Land, then use chat command <ab", new object[] { time_left.TotalMinutes });

                }
                Console.WriteLine("Objective Repair Mission: Not allowed delivery.");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("REPAIR mission handle repair load early ERROR: " + ex.ToString());
            GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER REPAIR (early)!!!!! Please inform Flug.", null);         
        }
        if (dadvice)
        {
            Timeout(2, () =>
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>Delivery training advice complete - your delivery would have been successful. Use command <de to actually deliver your load.", new object[] { });
            });
            return;
        }


        DateTime expiration_time2 = orm_PlayersOnRepairMissionExpiration[player].Item1;
        string playerLoad = orm_PlayersOnRepairMissionExpiration[player].Item4;
        int numLoads = orm_PlayersOnRepairMissionExpiration[player].Item5; //With fix in orm_handleRepairMissionRepairObjective this numLoads should now be correct.  2022/12
    TimeSpan time_left2 = expiration_time2.Subtract(DateTime.UtcNow);
        if (repairType == RepairType.Ferry)
        {
            try
            {
                //We put this up top in case there is an error below (which it seems there is !?)  2021/11
                //remove from the dict so that aerial radar, knickebein, cover etc etc all work again
                //And also they can't re-deliver with this same load again - once only
                if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
                    orm_PlayersOnRepairMissionExpiration.Remove(player);

                //When repair cover a/c are done with the unloading/delivery they are done, they now must land.
                if (mainmission.covermission != null) mainmission.covermission.landCoverAircraft(player, fromRepair: true); //FromRepair makes land immediately and also changes msg to player a bit
            }
            catch (Exception ex)
            {
                Console.WriteLine("REPAIR mission removing db entry & landcoverAC ERROR: " + ex.ToString());
                GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER REPAIR MISSION ENDING!!!!! Please inform Flug.", null);
            }
        } else
        {
            //Switch the TYPE to FERRY so they can now DELIVER the aircraft just like a FERRY
            try
            {
                if (orm_PlayersOnRepairMissionExpiration.ContainsKey(player))
                {
                    var playerRepairInfo = orm_PlayersOnRepairMissionExpiration[player];
                    repairType = orm_PlayersOnRepairMissionExpiration[player].Item6;
                    orm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, Point3d, AiAircraft, string, int, RepairType>(playerRepairInfo.Item1, playerRepairInfo.Item2, playerRepairInfo.Item3, playerRepairInfo.Item4, playerRepairInfo.Item5, RepairType.Ferry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("REPAIR mission handle repair load SWITCH TO FERRY ERROR: " + ex.ToString());
                repairType = RepairType.Repair_Load;
            }
            Timeout(12, () =>
            {
                GamePlay.gpLogServer(new Player[] { player }, ">>>You can now ALSO complete a FERRY of your delivery aircraft - just land at a front line airfield a use command <de", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, ">>>If you DON'T want to deliver the ferry aircraft, release them with command <abandon (<ab).", new object[] { });
            });

        }


        //osm_PlayersOnRepairMissionExpiration[player] = new Tuple<DateTime, AiAircraft, String>(DateTime.UtcNow, aircraft, playerLoad);
        string loadExpl = "load of ";
        string loadXXX = "";
        if (numLoads > 1)
        {
            //Actual we don't know how many of these loads were successfully delivered, so just make it vague "loads".  Update 2022/12 - we SHOULD knonw numLoads no...
            loadExpl = string.Format("{0:n0} loads of ", numLoads);
            loadXXX = " X" + numLoads.ToString("n0");
            //loadExpl = string.Format("loads of ");
        }
        try
        {
            Timeout(10, () =>
           {
               string loadName = "repair cargo";
               if (repairType == RepairType.Defense_Load) loadName = "Defense Troops & gear";
               if (repairType == RepairType.Ferry) { loadName = "ferry aircraft"; loadExpl = ""; }

               GamePlay.gpLogServer(new Player[] { player }, ">>>You have successfully delivered your {0}{1} - " + playerLoad + loadXXX, new object[] { loadExpl, loadName });
               GamePlay.gpLogServer(new Player[] { player }, string.Format(">>>You completed the mission with {0:n0} minutes to spare!", new object[] { time_left2.TotalMinutes }), null);
           });

            //So we give them points - new a/c same as air victory / 1.5; new ground repair same as ground kill / 1.5 
            Mission.MO_ObjectiveType objectiveType = Mission.MO_ObjectiveType.MilitaryArea; //just some ground type so it will be registered as ground rather t han air/aa/ship
            if (repairType == RepairType.Ferry) objectiveType = Mission.MO_ObjectiveType.MilitaryArea;


            mainmission.MO_AddPlayerStatsScoresForObjectiveDestructionOrRepair(player, player.Name(), mo: null, score: ((double)numLoads) / 1.5, objectiveType: objectiveType); //2022-11, was 2.0, now 1.5 to give them a bit more credit  for a somewhat boring/long mission
        }
        catch (Exception ex)
        {
            Console.WriteLine("REPAIR mission handle repair load complete - record stats ERROR: " + ex.ToString());
            GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER REPAIR - STATS!!!!! Please inform Flug.", null);
         }

        /*
         * 850 Number of repair loads delivered
         * 851 Number of aircraft ferried
         */
        try {
            if (mainmission.statsmission != null)
            {
                if (repairType == RepairType.Ferry)
                {
                    //campaign stats
                    mainmission.statsmission.stb_ISaveIPlayerStat.StbSis_AddSessStat(player, 851, numLoads);
                    //current mission stats
                    mainmission.statsmission.stb_ISaveIPlayerStat.StbSis_AddToMissionStat(player, 851, numLoads);
                } else
                {
                    //campaign stats
                    mainmission.statsmission.stb_ISaveIPlayerStat.StbSis_AddSessStat(player, 850, numLoads);
                    //current mission stats
                    mainmission.statsmission.stb_ISaveIPlayerStat.StbSis_AddToMissionStat(player, 850, numLoads);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("REPAIR mission handle repair load complete - record stats2 ERROR: " + ex.ToString());
            GamePlay.gpLogServer(new Player[] { player }, ">>>MAJOR ERROR in COVER REPAIR - RECORD STATS-2!!!!! Please inform Flug.", null);
        }


    }

    public bool isPointInFerryArea(AiAircraft aircraft)
    {
        if (aircraft == null) return false;        
        return isPointInFerryArea(aircraft.AirGroup().getArmy(), aircraft.Pos());
    }

    public bool isPointInFerryArea(Player player)
    {
        if (player == null) return false;
        if (player.Place() == null) return false;
        return isPointInFerryArea(player.Army(), player.Place().Pos());
    }

    public bool isPointInFerryArea(int army, Point3d pos)
    {
        //if (army == 1 && pos.x < 79197 && pos.y > 260032) return true; //orig version Yatesbury, Watchfield, Harwell
        //if (army == 1 && pos.x < 94700 && pos.y > 260032) return true; //2021-11 includes reading also
        if (army == 1 && pos.x < 103600 && pos.y > 238032) return true; //2022-11 includes reading, White Waltham,
        //Farnborough, Odiham, Upavon, Netheravon, Larkhill
        //else if (army == 2 && pos.x > 313499 && pos.y < 76016) return true; //orig version
        //else if (army == 2 && pos.x > 315322 && pos.y < 98249) return true; //315332, 98249, 2021-11 expanded to Amiens Glisy, Persan Beaumont
        else if (army == 2 && pos.x > 298868 && pos.y < 98249) return true; //315332, 98249, 2022-11 expanded to Beauvais Nivillers, Beauvais Tille, Crecy, 
        else return false;

    }

    public string orm_GetAndClearPlayersWhoRepairedFerriedforTeamStatsList(int army, bool clear = true)
    {

        if (!RepairsFerries_thisStatsPeriod.ContainsKey(army)) return "";
        string ret = RepairsFerries_thisStatsPeriod[army];
        if (clear) RepairsFerries_thisStatsPeriod[army] = "";
        return ret;
    }

} //end class

public class Parachute
{
    public Mission msn { set; get; }

    public Parachute()
    {
        msn = null;
    }
    public Parachute(Mission m)
    {
        msn = m;
    }
    DateTime lastParachuteTime_dt = new DateTime();
    Dictionary<int, List<string>> parachuteplanes = new Dictionary<int, List<string>>() { {1,new List<string>() { "bob:Aircraft.BlenheimMkINF", //BMKI is best for red, needs a couple more seconds
        //"bob:Aircraft.HurricaneMkI", "bob:Aircraft.DefiantMkI" 
    } },
                                                                                                                  //"bob:Aircraft.WellingtonMkIc", "bob:Aircraft.BlenheimMkI", "bob:Aircraft.BlenheimMkIV", "bob:Aircraft.HurricaneMkI", } },
            { 2, new List<string> {
                "bob:Aircraft.BlenheimMkINF"
            //"bob:Aircraft.Ju-88A-1",
            //"bob:Aircraft.He-111H-2", //this is best for blue, turns into a spear right away, needs a couple more seconds
            //"bob:Aircraft.Ju-87B-2",
            //"bob:Aircraft.DH82A-2",
            //"bob:Aircraft.G50",


            //"bob:Aircraft.Do-17Z-2",
            //"tobruk:Aircraft.Bf-108B-2_Trop",
            //"bob:Aircraft.WellingtonMkIc"
            } }};

    public int dropParachute(Point3d pos, int army = 0, double z_add_m = -100, double z_add2_m = -150, Vector3d? vwld = null, double vel_mps = 80, double delay_s = 0.01 )
    {

        
        DateTime currTime = DateTime.UtcNow;
        //do this once every .1 minutes at most
        if ((currTime.Subtract(lastParachuteTime_dt)).TotalMinutes < 0.01) return 0;
        double nump = (double)Calcs.gpNumberOfPlayers(msn.GamePlay);
        
        if (nump > 50) return 0;        

        lastParachuteTime_dt = currTime;
        double spawnAlt = pos.z + z_add_m;
        if (spawnAlt < 525) spawnAlt = 525; //with the Blenheim, above, this is about the minimum AGL that will work without the a/c crashing into the ground, kind of ruins the effect
        /*
        double le_m = Calcs.LandElevation_m(pos);
        if (le_m < 0) le_m = 0;
        if (spawnAlt < le_m + 25) spawnAlt = le_m + 110;
        */
        Point3d spawnPos = new Point3d(pos.x, pos.y, spawnAlt);
        Point3d attackPos = new Point3d(pos.x, pos.y, spawnAlt+ z_add2_m);
        if (vwld.HasValue) //same direction as aircraft  &30 seconds ahead
        {
            attackPos.x += vwld.Value.x * 30;
            attackPos.y += vwld.Value.y * 30;
        }
        
        int numAC = 1;//So one for each 10 DUs, but round up always.
        int numGroups = 1;
        int numAC_remaining = numAC;
        
        //By setting delay we can make this target spot line up better with the
        //place the bomb site aims to
        msn.Timeout(delay_s, () =>
        //Timeout(0.15, () =>
        {
            string regiment = "gb01";
            if (army == 1) regiment = "BoB_RAF_F_141Sqn_Early";
            if (army == 2) regiment = "BoB_LW_JG77_I";

            
            for (int i = 0; i < numGroups; i++)
            {
                int numAC_ingroup = numAC_remaining;
                if (numAC_ingroup > 3) numAC_ingroup = 3;
                if (numAC_ingroup <= 0) break;
                numAC_remaining = numAC_remaining - numAC_ingroup;
                string acType = Calcs.chooseRandomElement(parachuteplanes[army]);

                string newACActorName = msn.covermission.Stb_LoadSubAircraft(spawnPos, type: acType, callsign: msn.random.Next(1, 25).ToString(), hullNumber: msn.random.Next(1, 100).ToString(), serialNumber: msn.random.Next(1, 1000).ToString("000"), regiment: regiment, fuelStr: "1", weapons: "", velocity_mps: vel_mps, fighterbomber: "f", numAC: numAC_ingroup, fromCover: false, loc2: attackPos, requestedNumInFlight: 1, army: army, exactPos: true);

                //Stb_LoadSubAircraft(Point3d loc, string type = "SpitfireMkIa_100oct", string callsign = "26", string hullNumber = "3", string serialNumber = "001", string regiment = "gb02", string fuelStr = "", string weapons = "", double velocity_mps = 0, string fighterbomber = "", string skin_filename = "", string delay_sec = "", string escortedGroup = "", int numAC = 2, string formation = "VIC3", Player player = null, Vector3d? vwld = null, bool fromCover = true, Point3d? loc2 = null, int requestedNumInFlight = 0)


                msn.Timeout(0.55, () => //was 1.05
                //Timeout(0.15, () =>
                {
                    //AiActor newActor = GamePlay.gpActorByName(newACActorName);
                    AiActor newActor = msn.GamePlay.gpActorByName(newACActorName);
                    //Console.WriteLine("NewActorloaded: " + newActor.Name() + " for " + player.Name());
                    AiAircraft newAircraft = newActor as AiAircraft;
                    AiAirGroup newAirgroup = newAircraft.AirGroup();
                    Console.WriteLine("makeParachute - new Airgroup loaded: " + newAirgroup.Name() + " newACActorName: " + newACActorName);
                    msn.DoDamageToAirplane(newAircraft, 0.001, severe: true);
                    msn.Timeout(15, () => { (newActor as AiCart).Destroy(); });
                });
            }

            
        });

        Console.WriteLine("parachute: - just launched & destroyed {0} aircraft to make parachutes.", numAC);
        return numAC;
    }
}

//Various helpful calculations, formulas, etc.
public static class RepairCalcs 
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

    //Vwld is the direction an aircraft is going, say from their Vwld
    //point1 is the location of the aircraft.  Point2 is the location of the target aircraft
    //return angle is the degrees left/right from the primary a/c current course that a/c must turn to point at the 2nd aircraft point
    public static double CalculateDifferenceAngle( Vector3d Vwld,
                      Point3d point1,
                      Point3d point2)
    {

        Point3d v1 = new Point3d(Vwld.x, Vwld.y, Vwld.z);
        Point3d v2 = new Point3d (point2.x-point1.x, point2.y-point1.y, 0);
        return CalculateDifferenceAngle(v1, v2);
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
        ret.x = startPoint.x + Math.Sin(RepairCalcs.DegreesToRadians(angle_deg)) * dist;
        ret.y = startPoint.y + Math.Cos(RepairCalcs.DegreesToRadians(angle_deg)) * dist;
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

    public static string correctedSectorNameDoubleKeypad(ObjectiveRepairMission msn, Point3d p)
    {

        string s = correctedSectorName(msn, p) + "." + doubleKeypad(p);
        return s;

    }

    public static string correctedSectorNameKeypad(ObjectiveRepairMission msn, Point3d p)
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

    public static string correctedSectorName(ObjectiveRepairMission msn, Point3d p)
    {
        if (msn.GamePlay == null) return "";

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
        double sizey = 310000; //CLOD
        //double sizey = 360000; //TOBRUK
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
    public static List<AiAircraft> AllAircraftNearSorted(AMission msn, Point3d pos, Vector3d Vwld, double distance_m, double radius_m)
    {
        double dist = distance(Vwld.x, Vwld.y);

        Point3d point2 = pos; //if current velocity = 0.

        if (dist > 0) {

            point2 = new Point3d(Vwld.x / dist * distance_m + pos.x, Vwld.y / dist * distance_m+ pos.y, pos.z);
        }

        var alist = AllAircraftNear(msn, point2, radius_m);
        var retdict = new SortedList<double, AiAircraft>();

        foreach (AiAircraft a in alist)
        {
            Point3d actorPos = (a as AiActor).Pos();
            if (pos.x == actorPos.x && pos.y == actorPos.y && pos.z == actorPos.z) continue; //the player aircraft, don't knoclue it
            double d = CalculatePointDistance(pos, actorPos);
            //Console.WriteLine("AIR: Looking at " + GetAircraftType(a) + " " + d.ToString("F0") + " " + (a as AiActor).Pos().x.ToString("F0") + " " + (a as AiActor).Pos().y.ToString("F0"));
            retdict[d]= a;
        }

        //var ListOrderedByDistance = retdict.OrderBy(kvp => kvp.Value).ToList();
        return retdict.Values.ToList();
    }

    public static List<AiAircraft> AllAircraftNear(AMission msn, Point3d pos, double radius_m)
    {
        var ret = new List<AiAircraft>();

        var allAc = AllAircraftInGame(msn);
        foreach (AiAircraft a in allAc)
        {
            double d = CalculatePointDistance((a as AiActor).Pos(), pos);
            if (d <= radius_m ) ret.Add(a);
            //Console.WriteLine("AIR: Near looking at " + GetAircraftType(a) + " " + d.ToString("F0") );
        }

        return ret;
    }

    public static List<AiAircraft> AllAircraftInGame(AMission msn)
    {
        var ret = new List<AiAircraft>();

        if (msn.GamePlay!=null && msn.GamePlay.gpArmies() != null && msn.GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in msn.GamePlay.gpArmies())
            {
                if (msn.GamePlay.gpAirGroups(army) != null && msn.GamePlay.gpAirGroups(army).Length > 0)
                    foreach (AiAirGroup airGroup in msn.GamePlay.gpAirGroups(army))
                    {
                        if (airGroup != null && airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
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

    public static void loadSmokeOrFire(maddox.game.IGamePlay GamePlay, ObjectiveRepairMission mission, double x, double y, double z, string type, double duration_s = 300, string path = "")
    {

        if (GamePlay == null) return;
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
