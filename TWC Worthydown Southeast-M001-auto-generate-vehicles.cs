// # define DEBUG
//http://forum.1cpublishing.eu/showthread.php?t=26112
//Various emergency & service cars script by naryv posted an sukhoi.ru today.
//Based on an ambulance script created by fearless frog
//http://simhq.com/forum/ubbthreads.php/topics/3387619/Ambulance_Station_notes_from_t.html
//Hacked extensively by bhugh


//There has been some problem with auto-generated trucks running into aircraft as they spawn in on the airfield.
//To solve this, the trucks now look for these objects, in this order, as their "home base" or "magnet point":
// 1. Two specific Stationary Ground Objects, one each for GB & DE:
//     - Bedford_MW_tank 
//     - Opel_Blitz_fuel
//     - Note that in FMB these are all statics/vehicles  NOT vehicles.  Vehicles move around and require you to set a path for them. Statics just remain motionless in place.
//
// 2. If no Stationary Ground Objects are found nearby, the "birthplace" (ie, spawn point at the airport) is used.
// 3. If no birthplace is found, the airport location is used
// What this does is allows you to set Stationary Ground Objects in place round the perimeter of the airport.  The ground vehicles will use
//these vehicles (the closest one) as their home base. Thus, you can direct the ground vehicle traffic by placing these stationary/static trucks
// Initially we were using the "birthplace" as the home location.  But FMB does not allow the birthplace location to be easily edited.
//So the solution was using these static trucks/ArmoredCar/tractors to indicate the home base.  These can easily be inserted and edited in FMB.
//
//At spawn-in, the auto-generated trucks will appear on a direct line between the spawn-in point and the nearest stationary truck. It will 
//then drive to the stationary truck.
//At landing or crash landing, the auto-generated trucks will appear near the stationary truck (on a straight line from the aircraft location 
// the stationary truck), drive to the aircraft, then return in the direction of the stationary truck.
//
//So you need to ensure that the location of your airport spawn points and your stationary trucks located on the perimeter of the airport
//is suitable. If the straight line between one spawn point and the nearest stationary truck crosses a second spawn point, that is a recipe
//for disaster--when two players spawn in at once.
//
//The auto-generated vehicles won't cross a runway, so you need to ensure that there are stationary trucks on either side of each runway, at equal distance, to act
//as "home base" for any trucks that start/end on that side
//
//You can gain further control of the Ground Vehicles by placing these statationary/static objects on the map, particularly in the area of airports:
//
//These two objects make every Ground Vehicle that spawns for any aircraft within 500 meters of these objects, to have a very short life (about 20 seconds):
//
//   - Bedford_MW_tent
//   - Opel_Blitz_tent
//
//These two objects **prevent any ground vehicles from spawning at all** for any aircraft within 250 meters of these objects,
//   - Bicycle_UK1
//   - Bicycle_GER1
//
// Note that the actual distances these various vehicles are effective is set in these variables:
//        public int magnetTruck_dis_m = 2000; //this distance at which this type of truck becomes effective/starts attracting the ground vehicles
//        public int shortTimeTruck_dis_m = 500; //effective distance/range of this type of truck 
//        public int shortTimeTruck_time_sec = 2
//
//
//TODO
// - Make vehicles avoid and/or spawn out if they approach too close to any of the spawn-in points at the nearest spawn-in point (birthplace).  And/or, if players are killed upon spawn-in by a ground vehicle, figure out some way to make it not count against them in stats.
// X Some trailers are still not despawning - end in _13 (_Chief_Ammo_13)
// - Ground Vehicles just spawn out if they get too close to an aircraft now. This should entirely prevent GV just mowing over aircraft sitting on a field or just spawned in.
// - We could make vehicles drive a ways out from the airport before de-spawning (that way de-spawns would never be seen by human eyes)
// - Still some vehicles are not being spawned out @ the end, and some trailers.  Prob. missed in the onmissionloaded method, or perhaps because the aircraftnumber doesn't match up. 
// - Probalby need and onactorcreated filter & set a max life to everything as it is spawned in. base.OnActorCreated(missionNumber, shortName, actor);
// X Also we could put spawn-out timeouts on everything, all aiactors included trailers, as it comes in onmissionloaded instead of just the aigroups
// - Include the aircraftnumber in the vehicle names as one part of them so that we **know** the a/c number when it spawns in rather than just needing to guess it as we do know by hoping aircraftnumber hasn't changed between the isectionfile read & when the mission file is actually read into onmissionloaded
// - Also vehicles are not spawned out if they are killed midway through their task.  Probably need and onactordead filter here. 
// X Detect when a/c have stopped moving & send vehicles then.  That would detect
//most other types of landings & crashes that are not now detected. 
// X Fix prisoner wagon & various other types that aren't working
// - Sometimes vehicles, esp. emergency vehicles, just drive right over the target a/c.  If alive, it will kill it.  One way to stop this would be to make the target point of arrival for these vehicles a bit to the left or right side of the a/c rather than a straight line from the place where the vehicle starts to the exactly center of the a/c.  Move it randomly 10-15 meters right or left of this straight line.  Then if they overshoot, they will just miss anyway. 
// - Lots of dead code
// - Get try/catch going on all methods
// - instead of disappearing vehicles that get too close to an a/c, just make them immediately stop in their tracks.  (ontick subroutine)
// X Trucks & firetrucks with trailers & perhaps some other types leave behind some sub-items when they de-spawn.  Not sure how to get rid of them.
// - Sometimes sub-missions that include stationary items have them all removed when the first object in that mission spawns out.  Could make it more directly linked to the various objects within the mission
// X Fire trucks don't seem to spawn out in a timely manner.  Not sure why, or why ONLY Fire Trucks.
// - Would be nice to get all this work done in a helper thread, nothing here is super time-sensitive
// X Would be nice to use HashSet instead of List for a few lists, but it doesn't seem to work.  Even though HashSet is supposed to work in .Net 3.5 and higher & Environment.Version here reports 4.0.
// - If you just keep sitting in plane after crash landing, it will keep spawning more & more emerg. vehicles to come to you.  Perhaps bec. the plane keeps self-damaging & that re-adds it to the airplane list & then it detects you have stopped.  Probably, don't add plane to active list if it self-damages (or perhaps even outside damage?) unless it is moving.  Or keep a list of planes that have already landed/crashed/stopped etc and dont' re-add them to the active a/c list unless/until (something--maybe just a wait of 5 minutes).
// - Simple blow rads & slight crash on landing triggered 4(!) emerg. veh. groups, not sure why.  Might be related to the issue above; where a landing triggers some vehicle groups but then there is also self-damage still happening after that, triggering more. 
// - After being hit by bombs etc (ie, killed) vehicles don't spawn out ever, they just stay there.
// - Make sure players can't evade emerg vehicles after (eg) a crash landing just be exiting their plane quickly
// - similarly, if they exit the plane just before a crash they still get the emerg vehicles
// - Potential big addition, could send fire trucks etc when something is bombed or explodes, like ground targets.  Probably only for quite big ones.
// - 2016/05/25 - a/c are sometimes weathercocking into the initial tender vehicles, perhaps make them spawn a bit further away
// - OK, we can refactor the entire process of creating & naming groundgroups and then picking them up after the ISectionFile is loaded.  It will be more efficient and not lose groups, and also the groups will be tied in with their aircraft with no chance of mixup.  Just include the aircraft ID# in the groundgroup name, like 0_Chief_Fire_XX_01.  When creating the ISectionFile, also create a Dictionary or LIst of all groundactors etc created. You could also include the names of statics created. Then use OnActorCreated to check for actor.Name() being in this list/dictionary.  Then you can associate the TechCars exactly with the related PlanesQueue item, get all the TechCars with their proper characteristics, etc.
// - 
//
//Code:


/* / / - $ debug */

//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference System.Core.dll
/* / / $ reference parts/core/maddox.dll */
/* / / $ reference parts/core/part.dll */
/* / / $ reference parts/core/core.dll
*  / / $ reference parts/core/gameWorld.dll
* / / $reference Campaign.dll
*/




using System;
//using System.Core;
using System.Collections;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using part;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using System.Linq;

public class Mission : AMission
{
    
    public bool DEBUG=false;
    public int MIN_VEHICLE_LIFE_SEC = 120; //This sets only a low benchmark for a certain loop. Lifetime for each individual type of techcar is set below. 
    //public int MIN_VEHICLE_LIFE_SEC = 10; //for testing
    public int MAX_VEHICLE_LIFE_SEC = 240; //Not used for anything right now.
    //public int MAX_VEHICLE_LIFE_SEC = 20; //for testing
    //public double CAR_POS_RADIUS = 80; //distance vehicles will be positioned from the center point of the Birthplace, Airfield, etc where cars are positioned
    public double CAR_POS_RADIUS = 35; //distance vehicles will be positioned from the center point of the Birthplace, Airfield, etc where cars are positioned
    public int TICKS_PER_MINUTE=1986; //empirically, based on a timeout test.  This is approximate & varies slightly.
    
    //for landing or crash, they start SPAWN_START_DISTANCE_M away from the plane in the direction of the nearest BirthPlace or Airport point.  They end SPAWN_END_DISTANCE_M away from the a/c.  (Distances are approx., various randomness & functions added on top of these values.)  
    public int SPAWN_START_DISTANCE_M = 140; //how far away they start from the     a/c when approaching after landing
    public int SPAWN_END_DISTANCE_M = 20;  //how close they approach you upon (eg) landing
    
    //For spawn-in, the vehicles start this close to the a/c and proceed to
    // the birthplace or airport point.
    public int SPAWN_START_DISTANCE_REVERSE_M = 16; //how close they start when spawning in onPlaceEnter

    string USER_DOC_PATH;
    string CLOD_PATH;
    string FILE_PATH;
    string FULL_PATH;

    private HashSet<AiActor> actorPlaceEnterList;  
    private HashSet<AiAircraft> aircraftDamagedList;
    private HashSet<AiAircraft> aircraftActiveList;
    private HashSet<AiAircraft> aircraftAbandonedList;
    private HashSet<AiAircraft> aircraftRecentLeaveList;

    Random rnd = new Random();

    maddox.game.ABattle Battle;

    //IGamePlay GP = GamePlay as IGamePlay;

    public Mission () {
        //HashSet<int> evenNumbers = new HashSet<int>(); 
        //actorPlaceEnterList = new List<AiActor>();
        //aircraftDamagedList = new List<AiAircraft>();
        //aircraftActiveList = new List<AiAircraft>();
        actorPlaceEnterList = new HashSet<AiActor>();
        aircraftDamagedList = new HashSet<AiAircraft>();
        aircraftActiveList = new HashSet<AiAircraft>();
        aircraftRecentLeaveList = new HashSet<AiAircraft>();

        USER_DOC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // DO NOT CHANGE
        CLOD_PATH = USER_DOC_PATH + @"/1C SoftClub/il-2 sturmovik cliffs of dover - MOD/";  // DO NOT CHANGE
        FILE_PATH = @"missions/Multi/Fatal/M003";   // mission install directory (CHANGE AS NEEDED)
        FULL_PATH = CLOD_PATH + FILE_PATH + @"/";
        rnd = new Random(Guid.NewGuid().GetHashCode()); //randomize the seed . . . 





    }
    //Listen to events of every mission
    public override void Init(maddox.game.ABattle battle, int missionNumber)
    {
        base.Init(battle, missionNumber);
        Battle=battle;                       
        MissionNumberListener = -1; //Listen to events of every mission
        //This is what allows you to catch all the OnTookOff, OnAircraftDamaged, and other similar events.  Vitally important to make this mission/cs file work!
        //If we load missions as sub-missions, as we often do, it is vital to have this in Init, not in "onbattlestarted" or some other place where it may never be detected or triggered if this sub-mission isn't loaded at the very start.
        if (DEBUG) Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", typeof(AircraftType).Namespace,  typeof(AircraftType).Module,  typeof(AircraftType).ToString(), typeof(AircraftType).IsEnum, typeof(AircraftType).IsValueType,typeof(AircraftType).GetType()); //, AircraftType.GetType()
        if (DEBUG) GamePlay.gpLogServer(null, ".Net framework: " +  Environment.Version, new object[] { });
        
    }


    public override void OnBattleStarted()
    {
        base.OnBattleStarted();
        //MissionNumberListener = -1;
    }

    public class BasePos
    {
        internal string _Name;
        internal Point3d _Pos;
        public bool startAllowed = true;
        public int maxTime_sec = 0;

        internal bool DEBUG = false;

        public BasePos(string name, Point3d pos, bool sa=true, int mt=0)
        {
            if (name != null)
                this._Name = name;
            else this._Name = "";
            //if (pos!=null)
            this._Pos = pos;
            //else this.Pos=new Point3d(0,0,0); 
            this.startAllowed = sa;
            this.maxTime_sec = mt;
        }

        public BasePos(BasePos bp)
        {
            this._Name = bp.Name();
            this._Pos = bp.Pos();
            if (DEBUG) Console.WriteLine("BasePos inited: " + this.ToString("F2"));
            this.startAllowed = bp.startAllowed;
            this.maxTime_sec = bp.maxTime_sec;
        }

        //a default constructor . . .
        public BasePos(object o = null)
        {
            this._Name = "";
            this._Pos = new Point3d(0, 0, 0);
            this.startAllowed = true;
            this.maxTime_sec = 0;
        }

        public string Name(string name = null)
        {

            if (name == null) return this._Name;
            else
            {
                this._Name = name;
                return null;
            }

        }

        public Point3d Pos(Point3d pos)
        {
            this._Pos = pos;
            return this._Pos;
        }

        public Point3d Pos()
        {
            return this._Pos;
        }

        public virtual string ToString(string format="0.0")
        {
            return _Name + " "
            + _Pos.x.ToString(format) + " "
            + _Pos.y.ToString(format) + " "
            + _Pos.z.ToString(format) + " "
            + "Start: " + startAllowed.ToString() + " "
            + "maxTime: " + maxTime_sec.ToString();

        }

    }



    internal class ActorPos : BasePos
    {

        public AircraftType _Type;
        public ActorPos(string name, Point3d pos, AircraftType type, bool sa=true, int mt =0)
        {
            if (name != null)
                this._Name = name;
            else this._Name = "";
            //if (pos!=null)
            this._Pos = pos;
            //else this.Pos=new Point3d(0,0,0);
            this._Type = type;
            this.startAllowed = sa;
            this.maxTime_sec = mt;

    }

    public AircraftType Type(maddox.game.world.AircraftType type)
        {
            this._Type = type;
            return this._Type;
        }

        public AircraftType Type()
        {
            return this._Type;
        }

        public override string ToString(string format = "0.0")
        {
            return _Name + " "
            + _Pos.x.ToString(format) + " "
            + _Pos.y.ToString(format) + " "
            + _Pos.z.ToString(format) + " "
            + "Start: " + startAllowed.ToString() + " "
            + "maxTime: " + maxTime_sec.ToString();

        }
    }

    internal class TechCars
    {
        //private readonly AMission _mission; //allows us to reference methods etc from the Mission class as 'outer'  
        internal bool DEBUG = false;      
        internal AiGroundGroup TechCar { get; set; }
        internal BasePos basePos { get; set; }
        internal IRecalcPathParams cur_rp { get; set; }
        internal int RouteFlag = 0;
        internal int cartype = 0;
        internal int servPlaneNum = -1;
        internal int MAX_CARS = 100;
        internal ServiceType CarType { get { return (ServiceType)cartype; } set { cartype = (int)value; } }

        public Dictionary<int, int> ServiceLife;
        internal int Life()
        {
            {
                //Console.WriteLine("tc: c:" + ServiceLife.Count);
                if (DEBUG) if (ServiceLife.ContainsKey((int)cartype)) Console.WriteLine("tc:" + Convert.ToInt32(CarType).ToString() + " " + cartype.ToString());

                //Console.WriteLine("tc:" + Convert.ToInt32(CarType).ToString() + " " + cartype.ToString());
                
                //return 20;  //for testing
                
                int value = 0;
                if (ServiceLife.TryGetValue((int)cartype, out value))
                {
                    //Console.WriteLine("tc from ServiceLife: " + Convert.ToInt32(CarType).ToString() + " " + cartype.ToString() + " " + value.ToString());
                    //return ServiceLife[Convert.ToInt32(CarType)];                    
                    
                }
                else
                {
                    //Console.WriteLine("tc default:" + Convert.ToInt32(CarType).ToString() + " " + cartype.ToString());
                    value = 120;
                }
                if (basePos.maxTime_sec > 0 && value > basePos.maxTime_sec) value = basePos.maxTime_sec; //in NearestTruck we set a max time allowed for the cars in that area to exist, in some cases
                return value;
            }
        }


        internal TechCars(AiGroundGroup car, BasePos airoport, IRecalcPathParams rp)
        {
            try
            {
                this.TechCar = car;
                this.basePos = airoport;
                this.cur_rp = rp;


                ServiceLife = new Dictionary<int, int>(); //NOT Dictionary<int, int> ServiceLife = new Dictionary<int, int>();  - DURR because that creates a new ServiceLife, local in scope to the method
                ServiceLife.Add((int)ServiceType.NONE, 0);
                ServiceLife.Add((int)ServiceType.EMERGENCY, 120);
                ServiceLife.Add((int)ServiceType.FIRE, 180);
                ServiceLife.Add((int)ServiceType.FUEL, 120);
                ServiceLife.Add((int)ServiceType.AMMO, 120);
                ServiceLife.Add((int)ServiceType.BOMBS, 120);
                ServiceLife.Add((int)ServiceType.PRISONERCAPTURE, 360);
                ServiceLife.Add((int)ServiceType.SPAWNIN, 120);
                /*ServiceLife.Add(0, 120); //default
                ServiceLife.Add(1, 120);
                ServiceLife.Add(2, 240);
                ServiceLife.Add(3, 120);
                ServiceLife.Add(4, 120);
                ServiceLife.Add(5, 120);
                ServiceLife.Add(6, 240);
                ServiceLife.Add(7, 120);
                Console.WriteLine("TechCars created. serviceLIfe: " + this.ServiceLife[0].ToString());
                */

                //Console.WriteLine(".Net framework: " + System.Environment.Version); //GamePlay.gpLogServer(null, ".Net framework: " + System.Environment.Version, new object[] { });
                if (DEBUG) Console.WriteLine("TechCars created. basePos: " + this.basePos.ToString("F2") + " " + ServiceLife[4].ToString());
            }
            catch (Exception e) { System.Console.WriteLine("techcars: " + e.ToString()); }
        }
    }


    [Flags]
    public enum ServiceType // ??? ????????????? ???????
                            //note that all these types don't work; the actual type is determined by
                            //createemrgcarmission depending on a/c type, army, and a few other things.
                            //So it's not really determined by the settings in the curTechCar.CarType field 
    {
        NONE = 0,
        EMERGENCY = 1,
        FIRE = 2,
        FUEL = 4,
        AMMO = 8,
        BOMBS = 16,
        PRISONERCAPTURE = 32,
        SPAWNIN = 64
    }

    /*internal enum ServiceLife // ??? ????????????? ???????     
    {
        NONE = 0,
        EMERGENCY = 120,
        FIRE = 240,
        FUEL = 120,
        AMMO = 120,
        BOMBS = 120,
        PRISONERCAPTURE = 240,
        SPAWNIN = 64
    } */

    //Like AiActor or AiBirthplace but only has .Name() & .Loc()    



    internal class PlanesQueue {
        internal AiAircraft aircraft { get; set; }
        internal BasePos basePos { get; set; } 
        internal int state = 0;        
        internal ServiceType State { get { return (ServiceType)state; } set { state = (int)value; } }        
        internal int Lifetime = 0;
        internal float health = 1;
        public PlanesQueue(AiAircraft aircraft, BasePos basePos, int state)
        {
            this.aircraft = aircraft;
            this.basePos = basePos as BasePos;
            this.state = state;            
        }                 
                 
    }

    internal List<TechCars> CurTechCars = new List<TechCars>();
    internal List<PlanesQueue> CurPlanesQueue = new List<PlanesQueue>();
    TechCars TmpCar = null;
    bool MissionLoading = false;
    int MissionLoadingAircraftNumber = -1;

    internal double PseudoRnd(double MinValue, double MaxValue)
    {
        return rnd.NextDouble() * (MaxValue - MinValue) + MinValue;
    }



      public override void OnActorTaskCompleted(int missionNumber, string shortName, AiActor actor)
      {
        base.OnActorTaskCompleted(missionNumber, shortName, actor);
        if (DEBUG) GamePlay.gpLogServer(null, "OnActorTaskComplete", new object[] { });

        AiActor ai_actor = actor as AiActor;        
        if (ai_actor != null)
        {
            if (ai_actor is AiGroundGroup)
                for (int i = 0; i < CurTechCars.Count; i++) // ???? ????????????? ??????? ??????? ?? ?????????????? ????????, ????????? ?? ????????????
                {
                    if (CurTechCars[i].TechCar == ai_actor as AiGroundGroup) {
                        //if (CurTechCars[i].RouteFlag == 1)
                        TechCars car = CurTechCars[i] as TechCars; 
                        if (DEBUG) GamePlay.gpLogServer(null, "OnActorTaskComplete - ending plane service for " + i.ToString() + " in " + car.Life() + " sec. " + car.cartype + " " + car.CarType, new object[] { });
                        //this is basically to ensure that AI objects don't just hang around indefinitely when their tasks are done.
                        //In normal behavior, they may complete several tasks in the course of moving abou the airport, so we don't just want
                        //to destroy them immediately when task is done
                        Timeout((int)car.Life(), () =>
                        {
                            if (DEBUG) GamePlay.gpLogServer(null, "OnActorTaskComplete - ending plane service for " + i.ToString() + " now", new object[] { });
                            EndPlaneService(car, ai_actor as AiGroundGroup);
                            
                        });
                    }   
                           

                       //we're just destroying them @ this point, so no 'else' needed
                       // else
                       //     CheckNotServicedPlanes(i);
                };
        }  
    }
    

    internal void CheckNotServicedPlanes(int techCarIndex)
    {
        for (int j = 0; j < CurPlanesQueue.Count; j++)  
        {
            if (CurTechCars[techCarIndex].TechCar.IsAlive() && (CurPlanesQueue[j].basePos == CurTechCars[techCarIndex].basePos) && ((CurTechCars[techCarIndex].CarType & CurPlanesQueue[j].State) != 0) && (CurTechCars[techCarIndex].servPlaneNum == -1))
            {
                if (SetEmrgCarRoute(j, techCarIndex))   // ?????????? ??????? ??????????? ????????? ???????
                {
                    return;
                }
            }
        }  
    }

    //Removes the ground vehicle from the CurTechCars list & also destroys the AI object
    //We call it with the List item (not the index) because the index can change between call & execution, esp. if call via a timeout, which is common
    //We also include the TechCar field, (an AiGroundGroup) because sometimes the List item can be destroyed but the actual Ai Airgroup is still floating around undead
    //
    internal void EndPlaneService(TechCars tC, AiGroundGroup ground=null)
    { 
      try 
      {
         if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning now", new object[] { });
         if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning " + tC.servPlaneNum.ToString(), new object[] { });
         if (tC != null) { 
                if (DEBUG) GamePlay.gpLogServer(null, " Number of objects: " + tC.TechCar.GetItems().Length, new object[] { });
                //if (CurTechCars[techCarIndex].cur_rp == null) return;        
                tC.cur_rp = null; // ?????????? ???????
                
                /*//Just destroy the ground items at this point.
                if (tC.TechCar.GetItems() != null && tC.TechCar.GetItems().Length > 0)
                {
                   if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning 1 ", new object[] { });                         
                    foreach (AiActor actor in tC.TechCar.GetItems()) 
                    {
                      if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning 2 " , new object[] { });                     
                      //(actor as AiGroundActor).Destroy();
                      destroyAGVGroundGroup(actor as AiGroundGroup);
                    }
                }                 
                */
                if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning 1 ", new object[] { });
                destroyAGVGroundGroup(tC.TechCar);
                  
                CurTechCars.Remove(tC);
        }      
        
        if (ground != null) {
               if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning 2 ", new object[] { });                         
               /* foreach (AiActor actor in ground.GetItems()) 
               {
                      if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning 4 " , new object[] { }); 
                      
                      if (actor as AiGroundGroup != null ) {
                         if (DEBUG) GamePlay.gpLogServer(null, "EndPlaneService/despawning 4 " , new object[] { });
                         destroyAGVGroundGroup ( actor as AiGroundGroup );
                      } 
                      (actor as AiGroundActor).Destroy();
               } */
               
              destroyAGVGroundGroup(ground);
               
               
                
        }            
                /*
                                 
                if (CurTechCars[techCarIndex].servPlaneNum >= 0)
                {
                    
                    CurPlanesQueue[CurTechCars[techCarIndex].servPlaneNum].State &= ~CurTechCars[techCarIndex].CarType; // ??????? ??? ???????????? ? ?????????????? ????????, ???? ?? ????
                    
                    CurTechCars[techCarIndex].servPlaneNum = -1; // ?????????? ????? ?????????????? ????????
                    Timeout(5f, () =>
                    {
                        if (!MoveFromRWay(techCarIndex))// ????????? ?? ????? ?? ?? ???????, ? ??????? ? ??? ???? ???.
                        {
                            CurTechCars[techCarIndex].RouteFlag = 0;
                            CheckNotServicedPlanes(techCarIndex);   // ? ???????, ??? ?? ??? ????????????? ?????????
                        }
                    });
    
                }
                else Timeout(5f, () =>
                {
                        CurTechCars[techCarIndex].RouteFlag = 0;
                        CheckNotServicedPlanes(techCarIndex);   // ? ???????, ??? ?? ??? ????????????? ?????????                
                });
                */
        }
        catch (Exception e) {System.Console.WriteLine ("EndPlaneService: " + e.ToString());}     
    }
    
    
    internal bool MoveFromRWay(int carNum)
    {
        bool result = false;        
        if (DEBUG) GamePlay.gpLogServer(null, "Removing aircraft from runway at " + CurTechCars[carNum].basePos.Name(), new object[] { });
        if ((GamePlay.gpLandType(CurTechCars[carNum].TechCar.Pos().x, CurTechCars[carNum].TechCar.Pos().y) & LandTypes.ROAD) == 0)
            return result;
        
        Point3d TmpPos = CurTechCars[carNum].TechCar.Pos();
        /* while (((GamePlay.gpLandType(TmpPos.x, TmpPos.y) & LandTypes.ROAD) != 0))
            {
                TmpPos.x +=  10f;
                TmpPos.y +=  10f;
            };
        */
        //OK, while loops can be infinite. We'll limit ourselves to 100 tries to get off the runway    
        for (int i=0; i<200; i++) {
           if ((GamePlay.gpLandType(TmpPos.x, TmpPos.y) & LandTypes.ROAD) == 0) break;
           TmpPos.x +=  10f;
           TmpPos.y +=  10f;          
        }        
        Point2d EmgCarStart, EmgCarFinish;
        EmgCarStart.x = CurTechCars[carNum].TechCar.Pos().x; EmgCarStart.y = CurTechCars[carNum].TechCar.Pos().y;
        EmgCarFinish.x = TmpPos.x; EmgCarFinish.y = TmpPos.y;        
        CurTechCars[carNum].servPlaneNum = -1;
        CurTechCars[carNum].RouteFlag = 0;
        CurTechCars[carNum].cur_rp = null;
        CurTechCars[carNum].cur_rp = GamePlay.gpFindPath(EmgCarStart, 10f, EmgCarFinish, 10f, PathType.GROUND, CurTechCars[carNum].TechCar.Army());

        result = true;        
        return result;
    }


    public  bool SetEmrgCarRoute(int aircraftNumber,int carNum)
    { 
      try
      {      
        bool result = false;
        if (DEBUG) GamePlay.gpLogServer(null, "Setting a Car Route "+aircraftNumber.ToString() + " " + carNum.ToString() + " at " + CurTechCars[carNum].basePos.Name() , new object[] { });
        if (CurTechCars[carNum].TechCar != null)
        {
            CurTechCars[carNum].servPlaneNum = aircraftNumber; // ????????????? ????? ?????????????? ????????
            if (CurTechCars[carNum].cur_rp == null)
                {
                    Point2d EmgCarStart, EmgCarFinish, LandedPos;
                    LandedPos.x = CurPlanesQueue[aircraftNumber].aircraft.Pos().x; LandedPos.y = CurPlanesQueue[aircraftNumber].aircraft.Pos().y;
                    int Sign = ((carNum % 2) == 0) ? 2 : -2;
                    EmgCarStart.x = CurTechCars[carNum].TechCar.Pos().x; EmgCarStart.y = CurTechCars[carNum].TechCar.Pos().y;
                    
        //Drive the car from where it is to the point located in the direction of the aircraft position but 20 meters short of it.
        double disx, disy;            
        disx=Math.Abs(EmgCarStart.x - LandedPos.x) - 20;
        if (disx<10) disx=20;
        disy=Math.Abs(EmgCarStart.y - EmgCarStart.y) - 20;
        if (disy<10) disy=20;

        EmgCarFinish.x = EmgCarStart.x - disx * ((EmgCarStart.x - LandedPos.x) / Math.Abs(EmgCarStart.x - LandedPos.x)); EmgCarFinish.y = EmgCarStart.x - disy * ((EmgCarStart.y - LandedPos.y) / Math.Abs(EmgCarStart.y - LandedPos.y));                    
                    
                    //EmgCarFinish.x = LandedPos.x - PseudoRnd(0f, 1f) * ((LandedPos.x - EmgCarStart.x) / (Math.Abs(LandedPos.x - EmgCarStart.x))) - Sign;
                    //EmgCarFinish.y = LandedPos.y - PseudoRnd(0f, 1f) * ((LandedPos.y - EmgCarStart.y) / (Math.Abs(LandedPos.y - EmgCarStart.y))) - Sign;
                    
                    //For spawn-in, we want the cars to start in close to the a/c & drive away
                  /*  if ( ((int)CurTechCars[carNum].CarType & (int)ServiceType.SPAWNIN) !=0  ) {
                     Point2d tempStart = EmgCarStart;
                     EmgCarStart=EmgCarFinish;
                     EmgCarFinish=tempStart;
                    }
                  */   
                    
                    CurTechCars[carNum].cur_rp = GamePlay.gpFindPath(EmgCarStart, 15f, EmgCarFinish, 15f, PathType.GROUND, CurTechCars[carNum].TechCar.Army());
                    if (DEBUG) GamePlay.gpLogServer(null, "Setting a Car Route "+aircraftNumber.ToString() + " " + carNum.ToString() + " " + EmgCarStart.ToString() + " to " + EmgCarFinish.ToString() + " at " + CurTechCars[carNum].basePos.Name() , new object[] { });
                    result = true;
                }    
        }
        return result;
      } 
      catch (Exception e) {System.Console.WriteLine ("Emrg1: " + e.ToString()); return false; }  
         
    }


    public override void OnMissionLoaded(int missionNumber)
    {        
    base.OnMissionLoaded(missionNumber);
    try {        
        if (missionNumber > 0) //whenever a new mission loads, this slurps up any matching groundcars into the curTechCars list so they can be manipulated etc 
        //if (missionNumber==MissionNumber ) // important check!
        {
            if (DEBUG) GamePlay.gpLogServer(null, "Starting vehicle sub-mission loaded", new object[] { });            
            List<string> CarTypes = new List<string>();
            CarTypes.Add(":0_Chief_Emrg_");
            CarTypes.Add(":0_Chief_Fire_");
            CarTypes.Add(":0_Chief_Fuel_"); 
            CarTypes.Add(":0_Chief_Ammo_");
            CarTypes.Add(":0_Chief_Bomb_");
            CarTypes.Add(":0_Chief_Prisoner_");

            //listAllGroundActors(missionNumber); //testing
            //listAllStationaries(missionNumber); //testing

            //Timeout (30, () => { destroyAllGroundActors(missionNumber); }); //testing



            AiGroundGroup MyCar = null;
            
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < CarTypes.Count; j++)
                {
                    MyCar = GamePlay.gpActorByName(missionNumber.ToString() + CarTypes[j] + i.ToString()) as AiGroundGroup;
                    
                    
                    if (MyCar != null)
                    {
                        
                        MissionLoading = false;
                        if (DEBUG) GamePlay.gpLogServer(null, "Creating groundcar group for " + missionNumber.ToString() + CarTypes[j] + i.ToString() + " " + MyCar.Name() + " " + MyCar.ID(), new object[] { });
                        BasePos ap = new BasePos();
                            
                        if (CurPlanesQueue.Count>= MissionLoadingAircraftNumber  && CurPlanesQueue[MissionLoadingAircraftNumber].basePos != null)
                            ap = CurPlanesQueue[MissionLoadingAircraftNumber].basePos;
                        else ap = FindNearestAirport(MyCar, true);
                        
                        if (DEBUG) GamePlay.gpLogServer(null, "Creating groundcar group for " + missionNumber.ToString() + CarTypes[j] + i.ToString() + " " + MyCar.Name() + " " + MyCar.ID() + " " + ap.ToString() , new object[] { });

                            if (!ap.startAllowed) //no vehicles allowed to spawn in this location. They shouldn't be spawning in at all of !startAllowed, but we'll just make double-sure of that right here.
                            {                                
                                destroyAGVGroundGroup(MyCar as AiGroundGroup); //destroy this car immediately
                                continue; //no point in doing any of the rest of the setup
                            }

                        //We're going to do the destroy routine first - that way if any error should happen later in the routine, the cars wil
                        //be destroyed regardless
                        TmpCar = new TechCars(MyCar, ap, null);
                        Timeout(1.1 * (int)TmpCar.Life(), () =>
                        {
                            //(MyCar as AiGroundActor).Destroy();

                            destroyAGVGroundGroup(MyCar as AiGroundGroup);
                        });
                        //if (DEBUG) GamePlay.gpLogServer(null, "Creating groundcar group at " + TmpCar.basePos.Name() + " " + MissionLoadingAircraftNumber.ToString(), new object[] { });
                        TmpCar.CarType = (ServiceType)(1 << j);
                        TmpCar.cur_rp = null;
                        TmpCar.servPlaneNum=MissionLoadingAircraftNumber;
                        if (!CurTechCars.Contains(TmpCar))
                             CurTechCars.Add(TmpCar);
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + MyCar.GetItems().Length, new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.CarType.ToString(), new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.servPlaneNum.ToString(), new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.cartype.ToString(), new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.ServiceLife[TmpCar.cartype].ToString(), new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.Life().ToString(), new object[] { });     

                        //if (CurTechCars.count < MAX_CARS)   CurTechCars.Add(TmpCar);
                        //These things are unruly, so we're setting a max life on them.                      
     
                        
                    };
                }             
            }               
        }
        
      }
      catch (Exception e) {System.Console.WriteLine ("miss loaded: " + e.ToString());}  
    }

    private void destroyAllGroundActors(int missionNumber = -1)
    {
        //TODO: Make it list only the actors in that mission by prefixing "XX:" if missionNumber is included.
        try
        {
            if (!DEBUG) return;
            if (DEBUG) GamePlay.gpLogServer(null, "Destroying all ground actors:", new object[] { });

            int group_count = 0;
            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                foreach (int army in GamePlay.gpArmies())
                {
                    //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                    if (GamePlay.gpGroundGroups(army) != null && GamePlay.gpGroundGroups(army).Length > 0)
                    {
                        foreach (AiGroundGroup group in GamePlay.gpGroundGroups(army))
                        {
                            group_count++;
                            if (group.GetItems() != null && group.GetItems().Length > 0)
                            {
                                //poscount = group.NOfAirc;
                                foreach (AiGroundActor actor in group.GetItems())
                                {
                                    if (actor != null)
                                    {
                                        if (DEBUG) GamePlay.gpLogServer(null, "Destroying: " + actor.Name(), new object[] { });
                                        actor.Destroy();                                        
                                        AiGroundGroup actorSubGroup = actor as AiGroundGroup;
                                        if (actorSubGroup != null && (actorSubGroup).GetItems() != null && actorSubGroup.GetItems().Length > 0)
                                        {
                                            foreach (AiGroundActor a in actorSubGroup.GetItems())
                                            {
                                                if (DEBUG) GamePlay.gpLogServer(null, "Destroying sub-actor: " + a.Name(), new object[] { });
                                                a.Destroy();
                                                
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
        catch (Exception e) { System.Console.WriteLine("laa: " + e.ToString()); }



    }

    private void listAllGroundActors(int missionNumber=-1)
    {
        //TODO: Make it list only the actors in that mission by prefixing "XX:" if missionNumber is included.
        try
        {
            if (!DEBUG) return;
            if (DEBUG) GamePlay.gpLogServer(null,"Listing all ground actors:", new object[] { });

            int group_count = 0;
            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                foreach (int army in GamePlay.gpArmies())
                {
                    //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                    if (GamePlay.gpGroundGroups(army) != null && GamePlay.gpGroundGroups(army).Length > 0)
                    {
                        foreach (AiGroundGroup group in GamePlay.gpGroundGroups(army))
                        {
                            group_count++;
                            if (group.GetItems() != null && group.GetItems().Length > 0)
                            {
                                //poscount = group.NOfAirc;
                                foreach (AiActor actor in group.GetItems())
                                {
                                    if (actor != null)
                                    {
                                        if (DEBUG) GamePlay.gpLogServer(null, actor.Name(), new object[] { });
                                        AiGroundGroup actorSubGroup = actor as AiGroundGroup;
                                        if (actorSubGroup != null && (actorSubGroup).GetItems() != null && actorSubGroup.GetItems().Length > 0)
                                        {
                                            foreach (AiActor a in actorSubGroup.GetItems())
                                            {
                                                if (DEBUG) GamePlay.gpLogServer(null, a.Name(), new object[] { });
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
        catch (Exception e) { System.Console.WriteLine("laa: " + e.ToString()); }



    }

    private void listAllStationaries(int missionNumber=-1)
    {
        try
        {
            if (!DEBUG) return;
            if (DEBUG) GamePlay.gpLogServer(null, "Listing all stationaries:", new object[] { });


            foreach (GroundStationary group in GamePlay.gpGroundStationarys())
            {

                if (group != null)
                {
                    if (DEBUG) GamePlay.gpLogServer(null, group.Name+ " " + group.Title , new object[] { });
                }
            }            

        }
        catch (Exception e) { System.Console.WriteLine("las: " + e.ToString()); }



    }
    public override void OnTickGame() {
      base.OnTickGame();
      try {
            if ( (Time.tickCounter()) == 0) {
            //  if (DEBUG) GamePlay.gpLogServer(null, "Ground vehicles started ", new object[] { });
              
            }
              
            if ((Time.tickCounter() % (TICKS_PER_MINUTE/6)) == 12 ) {
           
            checkForStoppedAircraft(aircraftActiveList);
            }

            //This checks whether the car is too near a friendly a/c and if so, then it will de-spawn the car
            //Moving it to its own separate loop so that we can make it run a bit more often.  % 32 should be about 1X per second.
            if ((Time.tickCounter() % (TICKS_PER_MINUTE / 63)) == 5)
            {
                try
                {

                    List<TechCars> CurTechCarsCopy = new List<TechCars>(CurTechCars); //we need a copy for the loop because we might change TechCars mid-loop by deleting something in it
                    foreach (TechCars car in CurTechCarsCopy)  // (int i = 0; i < CurTechCars.Count; i++)       //again ix-nay on the loop-for-ay . . . .
                    {

                        if (car.TechCar != null)
                        {


                            Point3d pos_ct = car.TechCar.Pos();
                            //IGamePlay GP = GamePlay as IGamePlay;
                            //if < 15 m from nearest plane, just despawn
                            if ((car.TechCar as AiActor) != null) { if (DEBUG) GamePlay.gpLogServer(null, "car.TechCar is an actor " + (car.TechCar as AiActor).Pos().x, new object[] { }); }
                            else if (DEBUG) GamePlay.gpLogServer(null, "car.TechCar is NOT an actor", new object[] { });
                            AiAircraft nearest_ac = GetNearestFriendlyAircraft(car.TechCar as AiActor);
                            if (nearest_ac != null)
                            {
                                double dist_to_nearest_ac = (nearest_ac as AiActor).Pos().distance(ref pos_ct);
                                if (DEBUG) GamePlay.gpLogServer(null, "distance to nearest ac = " + dist_to_nearest_ac, new object[] { });
                                if (dist_to_nearest_ac < 15)
                                {
                                    if (DEBUG) GamePlay.gpLogServer(null, "De-spawning vehicle because it is too close to an aircraft. " + car.basePos.Name(), new object[] { });
                                    EndPlaneService(car, car.TechCar);
                                    continue;
                                }
                            }
                        }
                    }
                } 

                catch (Exception e) { System.Console.WriteLine("autoveh-tickloop3: " + e.ToString()); }

            
               
               
                
            }


            if (Time.tickCounter() % 64 == 0)
            {  
            try
            { 
                //if (DEBUG) GamePlay.gpLogServer(null, "Ground vehicles continue . . . ", new object[] { });
                for (int i = 0; i < CurPlanesQueue.Count; i++)
                { 
                    CurPlanesQueue[i].Lifetime++;
                    if (DEBUG) GamePlay.gpLogServer(null, "Lifetime:  " + CurPlanesQueue[i].Lifetime, new object[] { });
                    if ((CurPlanesQueue[i].State == ServiceType.NONE) || (CurPlanesQueue[i].aircraft == null)  || (CurPlanesQueue[i].Lifetime > (int)((double)MIN_VEHICLE_LIFE_SEC*TICKS_PER_MINUTE/64/60))) // (int)((double)VEHICLE_LIFE_SEC*TICKS_PER_MINUTE/64/60))) 
                    {
                        int numTechCarsforThisPlane=0;
                        foreach ( TechCars car in CurTechCars ) //don't use a for count/index loop here as we are destroying some of the objects mid-loop . . . arghh 
                        {
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.cartype.ToString(), new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.ServiceLife[TmpCar.cartype].ToString(), new object[] { });
                        //if (DEBUG) GamePlay.gpLogServer(null, "tmpcar " + TmpCar.Life().ToString(), new object[] { });
                            if (car.servPlaneNum == i){
                                numTechCarsforThisPlane++;                        
                                if (CurPlanesQueue[i].Lifetime > (int)((int)car.Life()*TICKS_PER_MINUTE/64/60))  {
                                    if (DEBUG) GamePlay.gpLogServer(null, "Removing ground car for plane " + car.servPlaneNum + " " + car.CarType + " in 5 seconds", new object[] { }); 
                                    //EndPlaneService(j);
                                    Timeout ( 5f, () => { EndPlaneService(car, car.TechCar); });
                                }                                  
                            }    
                        }        
                        if (numTechCarsforThisPlane==0) CurPlanesQueue.RemoveAt(i); 
                    }                  
                };
                //This loop isn't doing too much useful now.  We could probably
                //just eliminate it.  The purpose for it would be re-using
                //cars after their initial mission is done, but since we 
                //are not doing that now anyway, no point.
                List<TechCars> CurTechCarsCopy = new List<TechCars>(CurTechCars); //we need a copy for the loop because we might change TechCars mid-loop by deleting something in it
                foreach ( TechCars car in CurTechCarsCopy )  // (int i = 0; i < CurTechCars.Count; i++)       //again ix-nay on the loop-for-ay . . . .
                {
                    if (DEBUG) GamePlay.gpLogServer(null, "Ground car at " + car.basePos.Name() + car.CarType, new object[] { });
                        //TechCars car = CurTechCars[i];
                        /*
                            * //not sure what the code below does, honestly, so it's gone. I think it has to do with repurposing the techCars & changing their paths
                        if ((car.TechCar != null && car.cur_rp != null) && (car.cur_rp.State == RecalcPathState.SUCCESS))
                        {
                            if (car.TechCar.IsAlive()) // && (car.RouteFlag == 0)) // && (car.servPlaneNum != -1))
                            {
                                car.RouteFlag = 1;
                                car.cur_rp.Path[0].P.x = car.TechCar.Pos().x; car.cur_rp.Path[0].P.y = car.TechCar.Pos().y;
                                car.TechCar.SetWay(car.cur_rp.Path);
                                //if (car.servPlaneNum != -1) car.RouteFlag = 0;
                            }
                           
                            //The code below avoids the current plane, right?  But, I'm worried about these ground vehicles hitting **all the other planes** that might be about this particular airport . . . . maybe some fixup needed
                            //double Dist = Math.Sqrt((car.cur_rp.Path[car.cur_rp.Path.Length - 1].P.x - car.TechCar.Pos().x) * (car.cur_rp.Path[car.cur_rp.Path.Length - 1].P.x - car.TechCar.Pos().x) + (car.cur_rp.Path[car.cur_rp.Path.Length - 1].P.y - car.TechCar.Pos().y) * (car.cur_rp.Path[car.cur_rp.Path.Length - 1].P.y - car.TechCar.Pos().y));

                            //the above seems to calculate the distance between the car's actual position & the end of it's planned path.  Then it would spawn out (or, originally, end it's mission so that it could be re-used.
                            //Instead let's calc the distance between the car & the plane;  If it gets too close then it will instantly spawn out.
                        }*/
                        if (car.TechCar != null)
                        {
                            if (CurPlanesQueue.Count> car.servPlaneNum &&  CurPlanesQueue[car.servPlaneNum].aircraft != null)
                            {                        
                                Point3d acpos = CurPlanesQueue[car.servPlaneNum].aircraft.Pos();

                                double Dist = Math.Sqrt((acpos.x - car.TechCar.Pos().x) * (acpos.x - car.TechCar.Pos().x) + (acpos.y - car.TechCar.Pos().y) * (acpos.y - car.TechCar.Pos().y));
                                if (car.servPlaneNum != -1)
                                {
                                    if (Dist < ((CurPlanesQueue[car.servPlaneNum].aircraft.Type() == AircraftType.Bomber) ? 20f : 10f))
                                        //EndPlaneService(i);
                                        EndPlaneService(car, car.TechCar);
                                }
                                else if (Dist < 15f)
                                {
                                    //EndPlaneService(i);
                                    EndPlaneService(car, car.TechCar);
                                }
                            }

                        }
                        if ((car.cur_rp == null) && (car.RouteFlag == 0) && (car.servPlaneNum != -1))
                        {
                            //EndPlaneService(car, car.TechCar);                      
                        };
                        if (car.servPlaneNum == -1 || car.TechCar == null)
                            //EndPlaneService(i);  //Once it is no longer serving a plane, we just zap it.
                            EndPlaneService(car, car.TechCar);
                        
                };
            }
            catch (Exception e) { System.Console.WriteLine ("autoveh-tickloop" +e.ToString());}  
            }
        }  
        catch (Exception e) {System.Console.WriteLine ("autoveh-tickloop2: " + e.ToString());}
    }
    



   
    internal BasePos FindNearestAirport(AiActor actor, bool returnActualNearestAirport=false)
    { 
      //try
      {
        if (actor==null) return null;
        Point3d pd = actor.Pos();
        if (DEBUG) GamePlay.gpLogServer(null, "Checking airport " + actor.Name(), new object[] { });
        return FindNearestAirport(pd, returnActualNearestAirport) as BasePos;
      }
      //catch (Exception e) {System.Console.WriteLine ("baseposfind: " + e.ToString()); BasePos ret3=null;  return ret3; }  

    }

    internal BasePos FindNearestAirport(Point3d pd, bool returnActualNearestAirport = false)
    {
        //try
        {
            GroundStationary aMinS = null;
            AiBirthPlace aMinB = null;
            AiActor aMin = null;          
            double d2Min = 0;
            BasePos ret = new BasePos();
            BasePos ret2 = new BasePos();
            Point3d retpd;

            //If we find a birthplace (ie, spawnpoint) closer than 2km we return that
            //otherwise we'll search all airports for something closer
            //And . . AiBirthPlace & AiAirport & AiActor are ALMOST the same thing but then again not quite so we have to dance a bit. 

            //TruckMessageLocation nearestStationaryTrucks = new TruckMessageLocation();

            if (DEBUG) GamePlay.gpLogServer(null, "Checking nearest items for " + pd.x.ToString("F0")
                + " " + pd.y.ToString("F0"), new object[] { });

            TruckMessageLocation nearestStationaryTrucks = FindNearestStationaryTrucks(pd);
            bool startAllowed = true;
            int maxTime_sec = 0;
            if (nearestStationaryTrucks.noVehiclesTruck != null ) startAllowed = false;            
            if (nearestStationaryTrucks.shortTimeTruck != null) maxTime_sec = nearestStationaryTrucks.shortTimeTruck_time_sec;


            if (DEBUG) GamePlay.gpLogServer(null, "Found " + nearestStationaryTrucks.magnetTruck + " " + nearestStationaryTrucks.shortTimeTruck + "  " + nearestStationaryTrucks.noVehiclesTruck, new object[] { });

            aMinS = nearestStationaryTrucks.magnetTruck;
            //if (aMinS == null) aMinB = FindNearestBirthplace(pd);
            if (aMinS != null) {
                d2Min = aMinS.pos.distance(ref pd);
                if (DEBUG) GamePlay.gpLogServer(null, "Checking nearest TRUCKs - found " + aMinS.Name + " " + aMinS.pos.distance(ref pd).ToString("F0")
                + " " + aMinS.pos.ToString(), new object[] { });

                if (d2Min < 2000)
                {
                    retpd = aMinS.pos;
                    if (retpd.z == 0) retpd.z = pd.z; //BirthPlaces usu. have elevation 0 which makes the ai route finder die horribly 
                    ret = new BasePos(aMinS.Name, retpd, startAllowed, maxTime_sec);
                    return ret;
                }


            }
            aMinB = FindNearestBirthplace(pd); 

            
            if (DEBUG && aMin != null) GamePlay.gpLogServer(null, "Checking trucks & airport (Birthplace) found " + aMin.Name() + " " + aMin.Pos().distance(ref pd).ToString("F0"), new object[] { });
            //if (1==0 && aMinB!= null) {
            if (aMinB != null)
            {

                d2Min = aMinB.Pos().distance(ref pd);
                if (DEBUG) GamePlay.gpLogServer(null, "Checking nearest TRUCKs then AIRPORT (Birthplace) found " + aMinB.Name() + " " + aMinB.Pos().distance(ref pd).ToString("F0")
                + " " + aMinB.Pos().ToString(), new object[] { });

                if (d2Min < 2000)
                {
                    retpd = aMinB.Pos();
                    if (retpd.z == 0) retpd.z = pd.z; //BirthPlaces usu. have elevation 0 which makes the ai route finder die horribly 
                    ret = new BasePos(aMinS.Name, retpd, startAllowed, maxTime_sec);                    
                    return ret;
                }
                if (DEBUG) GamePlay.gpLogServer(null, "Checking airport (Birthplace) NOfound " + d2Min.ToString("F0"), new object[] { });
            }
           


            int n = GamePlay.gpAirports().Length;
            AiActor[] aMinSaves = new AiActor[n + 1];
            int j = 0;
            for (int i = 0; i < n; i++)
            {
                AiActor a = (AiActor)GamePlay.gpAirports()[i];


                if (a == null) continue;
                if (!a.IsAlive()) continue;

                //if (DEBUG) GamePlay.gpLogServer(null, "Checking airport " + a.Name(), new object[] { });
                Point3d pp;
                pp = a.Pos();
                pd.z = pp.z;
                double d2 = pd.distanceSquared(ref pp);
                if ((aMin == null) || (d2 < d2Min))
                {
                    aMinSaves[j] = aMin;
                    j++;
                    aMin = a;
                    d2Min = d2;
                    //if (DEBUG) GamePlay.gpLogServer(null, "Checking airport / added to short list" + aMin.Name(), new object[] { });
                }
                else if (d2 < 10000000000) //we include some airports in the stack if they are close to the pd.  100,000meters=100km=62m squared =10mill meters squared
                {
                    aMinSaves[j] = a;
                    j++;
                    //if (DEBUG) GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name(), new object[] { });
                }
            }
            if (DEBUG && aMin != null) GamePlay.gpLogServer(null, "CAirport Found: " + aMin.Name() + " " + aMin.Pos().ToString() + " dist " + d2Min.ToString("F2"), new object[] { });

            //Hmm, with our new scheme it doesn't really matter if aMin is very
            //distant or what. The cars always start relatively close to the a/c
            //and **in the direction of** the airport, but not *at* the airport
            //if (d2Min > 2250000.0)
            //    aMin = null;

            //return aMin as BasePos


            //if the aircraft/point is close to an airport we return that airport (specifically the closest spawnpoint or 'birthplace'.  
            //But if further away, we return some random airports so that the
            //emergency vehicles come from a few directions, not just one. 
            int k = 0;
            if (aMin != null)
            {
                ret2 = new BasePos(aMin.Name(), aMin.Pos(), startAllowed, maxTime_sec);                
                aMinSaves[j] = aMin;
                if (d2Min < 2000 * 2000 || returnActualNearestAirport)
                {
                    ret = ret2;
                    if (DEBUG) GamePlay.gpLogServer(null, "CAirport Returning actual closest airport: " + ret2.Name() + " " + ret2.Pos().ToString() + " dist " + ret2.Pos().distance(ref pd).ToString("F0") + " " + d2Min + " " + returnActualNearestAirport.ToString() + " " + (j - k).ToString() + " choices " + ret2.ToString("F0"), new object[] { });
                }
                else
                {

                    if (j > 12) k = j - 12; //choose randomly from the 12 closest matches.  Note that most airports will generate 2-3 different matches, thus this is maybe 3-4-5 actual different airports
                    int ran = rnd.Next(k, j + 1);
                    if (DEBUG) GamePlay.gpLogServer(null, "CAirport Returning: ran=" + ran.ToString(), new object[] { });
                    ret2 = new BasePos(aMinSaves[ran].Name(), aMinSaves[ran].Pos(), startAllowed, maxTime_sec); ;
                }

            }
            if (DEBUG) GamePlay.gpLogServer(null, "CAirport Returning: " + ret2.Name() + " " + ret2.Pos().ToString() + " dist " + ret2.Pos().distance(ref pd).ToString("F0") + " " + (j - k).ToString() + " " + k.ToString() + " " + j.ToString() + " choices " + ret2.ToString("F0"), new object[] { });
        return ret2;
      }
      //catch (Exception e) {System.Console.WriteLine ("basepos2: " + e.ToString()); BasePos ret3=null;  return ret3; }
    }
    
    public AiBirthPlace GetBirthPlaceByName(string birthPlaceName)
    {
      try 
      {
          foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())
          {
              if (DEBUG) GamePlay.gpLogServer(null, "Checking airport " + bp.Name(), new object[] { });
              if (bp.Name() == birthPlaceName)
                  return bp;
          }
  
          return null;
      }
      catch (Exception e) {System.Console.WriteLine ("gbpbn: " + e.ToString()); return null;}
    }

    public AiBirthPlace FindNearestBirthplace(AiActor actor)
    {
        //AiBirthPlace nearestBirthplace = null;
        //AiBirthPlace[] birthPlaces = GamePlay.gpBirthPlaces();

        Point3d pos = actor.Pos();

        return FindNearestBirthplace(pos);
    }
    
    public AiBirthPlace FindNearestBirthplace (Point3d pos)
    {
      try
      {
        AiBirthPlace nearestBirthplace = null;
        AiBirthPlace[] birthPlaces = GamePlay.gpBirthPlaces();

        if (birthPlaces != null)
        {
            foreach (AiBirthPlace airport in birthPlaces)
            {
                if (nearestBirthplace != null)
                {
                    //if (DEBUG) GamePlay.gpLogServer(null, "Checking airport " + airport.Name() + " " 
                    //  + airport.Pos().distance(ref pos).ToString("F0"), new object[] { });
                    if (nearestBirthplace.Pos().distance(ref pos) > airport.Pos().distance(ref pos))
                        nearestBirthplace = airport;
                }
                else nearestBirthplace = airport;
            }
        }
        //AiActor ret=new AiActor();
        //ret.Pos( nearestBirthplace.Pos());
        //ret.Name(nearestBirthplace.Name()); 
        if (DEBUG) GamePlay.gpLogServer(null, "Checking airport FOUND" + nearestBirthplace.Name() + " " 
                      + nearestBirthplace.Pos().distance(ref pos).ToString("F0"), new object[] { });
        return nearestBirthplace;
      }
      catch (Exception e) {System.Console.WriteLine ("fnbirth: " + e.ToString()); return null; }  
    }

    public class TruckMessageLocation
    {
        public GroundStationary magnetTruck = null; //This type of truck attracts the ground vehicles to it, if w/in the magnetTruck_dis & it's the closest magnet truck
        public int magnetTruck_dis_m = 2000; //this distance at which this type of truck becomes effective/starts attracting the ground vehicles
        public GroundStationary shortTimeTruck = null; //This type of truck makes ground vehicles last a short time only
        public int shortTimeTruck_dis_m = 500; //effective distance/range of this type of truck 
        public int shortTimeTruck_time_sec = 20; //# of seconds ground vehicles will live when this truck type is close to them
        public GroundStationary noVehiclesTruck = null; //This type of truck makes ground vehicles disappear immediately/never appear at all
        public int noVehiclesTruck_dis_m = 250; //effective distance/range of this type of truck 
        public TruckMessageLocation ()
        { 
            magnetTruck = null; //This type of truck attracts the ground vehicles to it, if w/in the magnetTruck_dis & it's the closest magnet truck
            magnetTruck_dis_m = 2000; //this distance at which this type of truck becomes effective/starts attracting the ground vehicles
            shortTimeTruck = null; //This type of truck makes ground vehicles last a short time only
            shortTimeTruck_dis_m = 500; //effective distance/range of this type of truck 
            shortTimeTruck_time_sec = 20; //# of seconds ground vehicles will live when this truck type is close to them
            noVehiclesTruck = null; //This type of truck makes ground vehicles disappear immediately/never appear at all
            noVehiclesTruck_dis_m = 500; //effective distance/range of this type of truck 
        }


    
        public string ToString() {
            string mt = "";
            string stt = "";
            string nvt = "";
            if (magnetTruck != null) mt = magnetTruck.Name;
            if (shortTimeTruck != null) mt = shortTimeTruck.Name;
            if (noVehiclesTruck != null) mt = noVehiclesTruck.Name;
            return mt + " " + stt + " " + nvt;

        }
    }

    public TruckMessageLocation FindNearestStationaryTrucks(AiActor actor)
    {
        //AiBirthPlace nearestBirthplace = null;
        //AiBirthPlace[] birthPlaces = GamePlay.gpBirthPlaces();

        Point3d pos = actor.Pos();

        return FindNearestStationaryTrucks(pos);
    }

    public TruckMessageLocation FindNearestStationaryTrucks(Point3d pos)
    {
        //try
        {
            //try

            //GroundStationary nearestStationaryTruck = null;
            TruckMessageLocation nearestStationaryTrucks = new TruckMessageLocation();
            //double distance = 4000;  //look for anything within say 4 km ?  It is narrowed down to 2km later I believe
            double distance = Math.Max(nearestStationaryTrucks.magnetTruck_dis_m, Math.Max(nearestStationaryTrucks.noVehiclesTruck_dis_m, nearestStationaryTrucks.shortTimeTruck_dis_m));
            GroundStationary[] StationaryTrucks = GamePlay.gpGroundStationarys(pos.x, pos.y, distance);

            //catch (Exception e) { System.Console.WriteLine("#1: " + e.ToString()); return null; }
            //AiGroundActorType[] typesToUse = new AiGroundActorType[] { AiGroundActorType.Truck, AiGroundActorType.ArmoredCar, AiGroundActorType.Tractor };
            string nbsp = ((char)(160)).ToString(); //the groundsationary.Title uses non-breaking spaces in a couple of spots; nbsp is char #160
            string[] titlesForMagnet = new string[] { "Stationary" + nbsp + "Car" + nbsp + "Bedford_MW_tank", "Stationary" + nbsp + "Car" + nbsp + "Opel_Blitz_fuel" };
            string[] titlesForShortTime = new string[] { "Stationary" + nbsp + "Car" + nbsp + "Bedford_MW_tent", "Stationary" + nbsp + "Car" + nbsp + "Opel_Blitz_tent" };
            string[] titlesForNoVehicles = new string[] { "Stationary" + nbsp + "Bicycles" + nbsp + "Bicycle_UK1", "Stationary" + nbsp + "Bicycles" + nbsp + "Bicycle_GER1" };

            if (DEBUG) GamePlay.gpLogServer(null, "Checking trucks: " + StationaryTrucks.Length.ToString() + " found", new object[] { });

            //if (StationaryTrucks != null)
            //{
                foreach (GroundStationary truck in StationaryTrucks)
                {
                    if (DEBUG) GamePlay.gpLogServer(null, "Checking trucks: type " + truck.Type.ToString() + " - " + truck.Title, new object[] { });

                    double truckDist = truck.pos.distance(ref pos);
                    //for shortTime & noVehicles trucks, we don't need the "best" truck--we just need to know that one
                    //exists AT ALL within the specified distance
                    if (nearestStationaryTrucks.shortTimeTruck == null && (Array.Exists(titlesForShortTime, element => element == truck.Title)) && truckDist <= nearestStationaryTrucks.shortTimeTruck_dis_m) nearestStationaryTrucks.shortTimeTruck = truck;
                    if (nearestStationaryTrucks.noVehiclesTruck == null & (Array.Exists(titlesForNoVehicles, element => element == truck.Title)) && truckDist <= nearestStationaryTrucks.noVehiclesTruck_dis_m) nearestStationaryTrucks.noVehiclesTruck = truck;

                    if (!(Array.Exists(titlesForMagnet , element => element == truck.Title)) )
                    //if ("Stationary" + nbsp + "Car" + nbsp + "Bedford_MW_tank" != truck.Title)
                    {
                        /* int diff=Calcs.GetFirstDiffIndex("Stationary Car Bedford_MW_tank", truck.Title);
                        char c1 = truck.Title[diff - 1];
                        char c2 = truck.Title[diff];
                        char c3 = truck.Title[diff + 1];
                        if (DEBUG) GamePlay.gpLogServer(null, "Diff: " + diff.ToString() + " 1. " + (int)c1 + " 2. " + (int)c2 + " 3. " + (int)c3 + " Stationary Car Bedford_MW_tank != " + truck.Title, new object[] { });
                        */
                        continue;
                    }
                                        
                    if (truckDist > nearestStationaryTrucks.magnetTruck_dis_m) continue;
                
                    if (DEBUG) GamePlay.gpLogServer(null, "Checking trucks: type " + truck.Type.ToString() + " - " + truck.Title + " is GOOD! " + truck.pos.distance(ref pos).ToString("F0"), new object[] { });                    
                    if (nearestStationaryTrucks.magnetTruck != null)
                    {
                        
                        if (DEBUG) GamePlay.gpLogServer(null, "Checking truck " + truck.Name + " - " + truck.Title + " - "
                          + truck.pos.distance(ref pos).ToString("F0"), new object[] { });
                        if (nearestStationaryTrucks.magnetTruck.pos.distance(ref pos) > truckDist )
                        nearestStationaryTrucks.magnetTruck = truck;
                    }
                    else nearestStationaryTrucks.magnetTruck = truck;
                }
            //}
            
            //catch (Exception e) { System.Console.WriteLine("#2: " + e.ToString()); return null; }

            //AiActor ret=new AiActor();
            //ret.Pos( nearestStationaryTruck.Pos());
            //ret.Name(nearestStationaryTruck.Name()); 
            //if (DEBUG) GamePlay.gpLogServer(null, "Checking Stationary Trucks FOUND" + nearestStationaryTrucks.magnetTruck.Name + " "
            //                       + nearestStationaryTrucks.magnetTruck.pos.distance(ref pos).ToString("F0"), new object[] { });
            //if (DEBUG) GamePlay.gpLogServer(null, "Checking Stationary Trucks FOUND" + nearestStationaryTruck.Name, new object[] { });
            return nearestStationaryTrucks;
            
    }
        //catch (Exception e) { System.Console.WriteLine("fnstationary: " + e.ToString()); return null; }
    }


    ///TODO: This is highly repetitious & could be abstracted to a single simpler
    //method called 3X.  Then it would be easy to add several more types of 
    //cars etc.

    internal ISectionFile CreateEmrgCarMission(Point3d startPos, double fRadius, int portArmy, int planeArmy, AircraftType type, float health, Point3d aircraftPos, bool reverse=false, ServiceType planeState=ServiceType.NONE )
    //Note that planeState isn't actually used below . . . health is, plus
    //a/c type, army, etc.
    {  
      try
      { 
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value, value1, value2;
        string ChiefName1 = "0_Chief_" + (health < 1f ? "Fire_" : "Fuel_");
        string ChiefName2 = "0_Chief_" + (health < 1f ? "Emrg_" : "Ammo_");
        string ChiefName3 = "0_Chief_" + (health < 1f ? "Bomb_" : "Bomb_");

        //startPos=aircraftPos; //bhugh special, we're just going to spawn them in very near the actual a/c
        if (DEBUG) GamePlay.gpLogServer(null, "Ground car group created at " + startPos.ToString () + " for " + aircraftPos.ToString () + " " + FindNearestAirport(aircraftPos, true).Name()+ " " + portArmy.ToString() + " " + planeArmy.ToString()+ " health: " + health.ToString(), new object[] { });
        if (portArmy == planeArmy) //???? ????????
        {
            switch (portArmy)
            {
                case 1:
                    if (health < 1f)
                    {                        
                        sect = "CustomChiefs";
                        key = "";
                        value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma"; //???????
                        //value = "Vehicle.custom_chief_emrg_0 gb /tow00_00 1_Static";  
                        f.add(sect, key, value);
                        value = "Vehicle.custom_chief_emrg_1 $core/icons/tank.mma";//??????
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.Austin_K2_ATV";
                            
                        f.add(sect, key, value);

                        /* sect = "Stationary";
                        key = "1_Static";
                        value = "TrailerUnit.Fire_pump_UK2_Transport gb 0.00 0.00 0.00";
                        f.add(sect, key, value);
                        */
                         
                        key = "TrailerUnit.Fire_pump_UK2_Transport";
                        value = "1";
                        f.add(sect, key, value); 
                        
                        sect = "Vehicle.custom_chief_emrg_1";
                        key = "Car.Austin_K2_Ambulance";
                        value = "";
                        f.add(sect, key, value);

                        sect = "Chiefs";
                        key = "0_Chief_Fire_0";
                        value = "Vehicle.custom_chief_emrg_0 gb /skin0 materialsSummer_RAF";
                        f.add(sect, key, value);
                        key = "0_Chief_Emrg_1";
                        value = "Vehicle.custom_chief_emrg_1 gb /skin0 materialsSummer_RAF";
                        f.add(sect, key, value);
                    }
                    else
                    {                        
                        sect = "CustomChiefs";
                        key = "";
                        value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma";
                        f.add(sect, key, value);
                        value = "Vehicle.custom_chief_emrg_1 $core/icons/tank.mma";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_0"; // ??????????
                        key = "Car.Albion_AM463";
                        value = "";
                        f.add(sect, key, value);
                        if (type == AircraftType.Bomber)  // ??? ???????? ?????? ??????? ? ????? ????????
                        {
                            key = "Car.Fordson_N";
                            value = "";
                            f.add(sect, key, value);
                            
                            key = "TrailerUnit.Towed_Bowser_UK1_Transport";
                            value = "1";
                            f.add(sect, key, value);
                            
                        }

                        sect = "Vehicle.custom_chief_emrg_1"; // ??????
                        value = "";
                        key = "Car.Bedford_MW_open";
                        f.add(sect, key, value);


                        if (type == AircraftType.Bomber)  // ??? ???????? ????? ????????
                        {
                            sect = "CustomChiefs";
                            key = "";
                            value = "Vehicle.custom_chief_emrg_2 $core/icons/tank.mma";
                            f.add(sect, key, value);
                            sect = "Vehicle.custom_chief_emrg_2";
                            value = "";
                            key = "Car.Fordson_N";
                            value = "";
                            
                            f.add(sect, key, value);
                            key = "TrailerUnit.BombLoadingCart_UK1_Transport";
                            value = "1";
                            
                            f.add(sect, key, value);
                            key = "TrailerUnit.BombLoadingCart_UK1_Transport";
                            f.add(sect, key, value);
                        };
                        
                        sect = "Chiefs";
                        key = "0_Chief_Fuel_0";
                        value = "Vehicle.custom_chief_emrg_0 gb /skin0 materialsSummer_RAF";
                        f.add(sect, key, value);

                        key = "0_Chief_Ammo_1";
                        value = "Vehicle.custom_chief_emrg_1 gb /skin0 materialsSummer_RAF/tow00_00 1_Static";
                        f.add(sect, key, value);

                        if (type == AircraftType.Bomber)
                        {
                            key = "0_Chief_Bomb_2";
                            value = "Vehicle.custom_chief_emrg_2 gb /tow01_00 2_Static/tow01_01 3_Static/tow01_02 4_Static/tow01_03 5_Static/tow02_00 6_Static/tow02_01 7_Static";
                            f.add(sect, key, value);
                        }
                        sect = "Stationary";
                        key = "1_Static";
                        value = "Stationary.Morris_CS8-Bedford_MW_CargoAmmo3 gb 0.00 0.00 0.00";
                        f.add(sect, key, value);
                        if (type == AircraftType.Bomber) // ????? ??????
                        {
                            key = "2_Static";
                            value = "Stationary.Weapons_.Bomb_B_GP_250lb_MkIV gb 0.00 0.00 0.00";
                            f.add(sect, key, value);
                            key = "3_Static";
                            value = "Stationary.Weapons_.Bomb_B_GP_250lb_MkIV gb 0.00 0.00 0.00";
                            f.add(sect, key, value);
                            key = "4_Static";
                            value = "Stationary.Weapons_.Bomb_B_GP_250lb_MkIV gb 0.00 0.00 0.00";
                            f.add(sect, key, value);
                            key = "5_Static";
                            value = "Stationary.Weapons_.Bomb_B_GP_250lb_MkIV gb 0.00 0.00 0.00";
                            f.add(sect, key, value);
                            key = "6_Static";
                            value = "Stationary.Weapons_.Bomb_B_GP_500lb_MkIV gb 0.00 0.00 0.00";
                            f.add(sect, key, value);
                            key = "7_Static";
                            value = "Stationary.Weapons_.Bomb_B_GP_500lb_MkIV gb 0.00 0.00 0.00";
                            f.add(sect, key, value);
                        };
                    };
                    break;
                case 2:
                    
                    sect = "CustomChiefs";          //???????
                    key = "";
                    value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma";
                    f.add(sect, key, value);
                    value = "Vehicle.custom_chief_emrg_1 $core/icons/tank.mma";//??????
                    f.add(sect, key, value);
                    if (health < 1f)
                    {
                        
                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.Renault_UE";
                        value = "";
                        f.add(sect, key, value);
                        key = "TrailerUnit.Foam_Extinguisher_GER1_Transport";
                        value = "1";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_1";
                        if (PseudoRnd(0f, 1f) < 0.5f)
                        {
                            key = "Car.Opel_Blitz_med-tent";
                        }
                        else { key = "Car.Opel_Blitz_cargo_med"; };
                        value = "";
                        f.add(sect, key, value);
                        sect = "Chiefs";
                        key = "0_Chief_Fire_0";// "0_Chief_emrg";
                        value = "Vehicle.custom_chief_emrg_0 de ";
                        f.add(sect, key, value);
                        key = "0_Chief_Emrg_1";// "0_Chief_emrg";
                        value = "Vehicle.custom_chief_emrg_1 de ";
                        f.add(sect, key, value);
                    }
                    else
                    {
                        
                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.Opel_Blitz_fuel";
                        value = "";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_1";
                        key = "Car.Renault_UE";
                        f.add(sect, key, value);
                        key = "TrailerUnit.Oil_Cart_GER1_Transport";
                        value = "1";
                        f.add(sect, key, value);
                        key = "Car.Renault_UE";
                        value = "";
                        f.add(sect, key, value);
                        key = "TrailerUnit.Anlasswagen_(starter)_GER1_Transport";
                        value = "1";
                        f.add(sect, key, value);

                        if (type == AircraftType.Bomber) // ????? ??????
                        {
                            sect = "CustomChiefs";
                            key = "";
                            value = "Vehicle.custom_chief_emrg_2 $core/icons/tank.mma";
                            f.add(sect, key, value);
                            sect = "Vehicle.custom_chief_emrg_2";
                            key = "Car.Renault_UE";
                            value = "";
                            f.add(sect, key, value);
                            key = "TrailerUnit.HydraulicBombLoader_GER1_Transport";
                            value = "1";
                            f.add(sect, key, value);
                            key = "Car.Renault_UE";
                            value = "";
                            f.add(sect, key, value);
                            key = "TrailerUnit.BombSled_GER1_Transport";
                            value = "1";
                            f.add(sect, key, value);
                        }

                        sect = "Chiefs";
                        key = "0_Chief_Fuel_0";
                        value = "Vehicle.custom_chief_emrg_0 de";
                        f.add(sect, key, value);
                        key = "0_Chief_Ammo_1";
                        value = "Vehicle.custom_chief_emrg_1 de";
                        f.add(sect, key, value);
                        if (type == AircraftType.Bomber)
                        {
                            key = "0_Chief_Bomb_2";
                            value = "Vehicle.custom_chief_emrg_2 de /tow01_00 1_Static/tow03_00 2_Static";
                            f.add(sect, key, value);
                            sect = "Stationary";
                            key = "1_Static";
                            value = "Stationary.Weapons_.Bomb_B_SC-250_Type2_J de 0.00 0.00 0.00";
                            f.add(sect, key, value);
                            key = "2_Static";
                            value = "Stationary.Weapons_.Bomb_B_SC-1000_C de 0.00 0.00 0.00";
                            f.add(sect, key, value);
                        };
                    };
                    break;                    
                default:
                    
                    break;

            }
        }
        else
        {
            switch (portArmy)
            {
                case 1:
                    
                    if (health < 1f)
                    {
                        
                        sect = "CustomChiefs";
                        key = "";
                        value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma"; //???????
                        f.add(sect, key, value);
                        value = "Vehicle.custom_chief_emrg_1 $core/icons/tank.mma";//??????
                        f.add(sect, key, value);
                        value = "Vehicle.custom_chief_emrg_2 $core/icons/tank.mma";//????????
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.Austin_K2_ATV";
                        value = "";
                        f.add(sect, key, value);
                        key = "TrailerUnit.Fire_pump_UK2_Transport";
                        value = "1";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_1";
                        key = "Car.Austin_K2_Ambulance";
                        value = "";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_2";
                        key = "Car.Beaverette_III";
                        value = "";
                        f.add(sect, key, value);

                        sect = "Chiefs";
                        key = "0_Chief_Fire_0";
                        value = "Vehicle.custom_chief_emrg_0 gb /skin0 materialsSummer_RAF";
                        f.add(sect, key, value);
                        key = "0_Chief_Emrg_1";
                        value = "Vehicle.custom_chief_emrg_1 gb /skin0 materialsSummer_RAF";
                        f.add(sect, key, value);
                        key = "0_Chief_Prisoner_2";
                        value = "Vehicle.custom_chief_emrg_2 gb ";
                        f.add(sect, key, value);
                        ChiefName3 = "0_Chief_Prisoner_";

                    }
                    else
                    {
                        
                        sect = "CustomChiefs";
                        key = "";
                        value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma"; //???????
                        f.add(sect, key, value);
                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.Beaverette_III";
                        value = "";
                        f.add(sect, key, value);
                        sect = "Chiefs";
                        key = "0_Chief_Prisoner_0";
                        value = "Vehicle.custom_chief_emrg_0 gb ";
                        f.add(sect, key, value);
                        ChiefName1 = "0_Chief_Prisoner_";
                    };
                    break;
                case 2:
                    
                    if (health < 1f)
                    {
                        
                        sect = "CustomChiefs";
                        key = "";
                        value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma"; //???????
                        f.add(sect, key, value);
                        value = "Vehicle.custom_chief_emrg_1 $core/icons/tank.mma";//??????
                        f.add(sect, key, value);
                        value = "Vehicle.custom_chief_emrg_2 $core/icons/tank.mma";//????????
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.Renault_UE";
                        value = "";
                        f.add(sect, key, value);
                        key = "TrailerUnit.Foam_Extinguisher_GER1_Transport";
                        value = "1";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_1";
                        key = "Car.Opel_Blitz_cargo_med";
                        value = "";
                        f.add(sect, key, value);

                        sect = "Vehicle.custom_chief_emrg_2";
                        key = "Car.SdKfz_231_6Rad";
                        value = "";
                        f.add(sect, key, value);

                        sect = "Chiefs";
                        key = "0_Chief_Fire_0";
                        value = "Vehicle.custom_chief_emrg_0 de";
                        f.add(sect, key, value);
                        key = "0_Chief_Emrg_1";
                        value = "Vehicle.custom_chief_emrg_1 de";
                        f.add(sect, key, value);
                        key = "0_Chief_Prisoner_2";
                        value = "Vehicle.custom_chief_emrg_2 de /marker0 1940-42_var1";
                        f.add(sect, key, value);
                        ChiefName3 = "0_Chief_Prisoner_";

                    }
                    else
                    {
                        
                        sect = "CustomChiefs";
                        key = "";
                        value = "Vehicle.custom_chief_emrg_0 $core/icons/tank.mma"; //???????
                        f.add(sect, key, value);
                        sect = "Vehicle.custom_chief_emrg_0";
                        key = "Car.SdKfz_231_6Rad";
                        value = "";
                        f.add(sect, key, value);
                        sect = "Chiefs";
                        key = "0_Chief_Prisoner_0";
                        value = "Vehicle.custom_chief_emrg_0 de /marker0 1940-42_var1";
                        f.add(sect, key, value);
                        ChiefName1 = "0_Chief_Prisoner_";
                    };
                    break;
                default:
                    
                    break;
            };
        }

                
        //Little cleanup on variables; avoid div by zero later on; route finder doesn't like it if the points are underground - not sure what happens if they are far above the ground but below ground causes a serious fault, so we'll avoid that
        if (startPos.z == 0 ) startPos.z=1000;
        //startPos.z=1000; // little test . . . seemed to work ok 
        if (aircraftPos.z == 0 ) aircraftPos.z=1000;
        if ((startPos.x - aircraftPos.x )< 5 ) startPos.x = aircraftPos.x + 75;    
        if ((startPos.y - aircraftPos.y )< 5 ) startPos.y = aircraftPos.y + 75;
            
        //Instead of starting way out of sight, we're going to start
        //in the direction of the startPos, but at distance dis, which
        //is closer in to the a/c
        Point3d closerStartPos=startPos;
        int leftrightdis=10;
        int leftrightplus=0;
        double edisplus=2;
        double edisplus1=3;
        double edisplus2=4;
        
        if (!reverse) {      
          double dis=SPAWN_START_DISTANCE_M;
          closerStartPos.x = aircraftPos.x+ dis * ((startPos.x - aircraftPos.x) / Math.Abs(startPos.x - aircraftPos.x)); closerStartPos.y = aircraftPos.y + dis * ((startPos.y - aircraftPos.y) / Math.Abs(startPos.y - aircraftPos.y));                     
        }
        Point3d TmpStartPos = closerStartPos;
        TmpStartPos.x += PseudoRnd(-30f, 30f) + fRadius; TmpStartPos.y += PseudoRnd(-30f, 30f) + fRadius; 
        Point3d BirthPos = EmrgVehicleStartPos(TmpStartPos, startPos); //You will always end up @ TmpStartPos here unless that point is in water.
        //OK, that was really supposed to be EmrgVehicleStartPos(TmpStartPos, closerStartPos).  However, using startPos instead has the advantage that it is usually on dry ground & thus the routine will usually find dry ground to start the cars on.  Even if it is many miles from the acutal crashed a/c.  We can use that to our advantage now but when we implement boat or floatplane rescue we'll want to change this here & below.  
                        
        double disx, disy;
        
        sect = ChiefName1+"0" + "_Road";
        key = "";
        value1 = BirthPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.z.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "  0 92 5 ";
        if (DEBUG) Console.WriteLine(value1);
        //BirthPos.x -= 50f * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x)); BirthPos.y -= 50f * ((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y));

        double edis=SPAWN_END_DISTANCE_M; //how far away from the plane we stop, when driving in towards it.
        if (reverse) edis= SPAWN_START_DISTANCE_REVERSE_M; //IN CASE OF reverse we call this the 'start' distance because we are starting rather then ending here.  (Thus the name, 'reverse').  This tells where the vehicles start, when starting close & then driving out away from the a/c.
        
        if (rnd.Next(2)==0) leftrightdis=-leftrightdis; //randomly move the target point left or rightwards from the center of the plane
        edisplus=PseudoRnd(-3,2);
        leftrightplus=rnd.Next(-3,4);
          
        disx=Math.Abs(BirthPos.x - aircraftPos.x) - (edis-edisplus);
        if (disx<(edis-2)) disx=(edis-edisplus);
        disy=Math.Abs(BirthPos.y - aircraftPos.y) - edis;
        if (disy<edis) disy=edis;

        BirthPos.x -= disx * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x)) + (leftrightdis + leftrightplus) * ((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y)); BirthPos.y -= disy * ((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y)) - (leftrightdis + leftrightplus) * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x));
        value2 = BirthPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.z.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "  0 92 5 ";;
        if (DEBUG) Console.WriteLine(value2);
        
        if (reverse ) {
          f.add(sect, key, value2);
          f.add(sect, key, value1);
        }else {
          f.add(sect, key, value1);
          f.add(sect, key, value2);
        }  

        TmpStartPos = closerStartPos;
        TmpStartPos.x += PseudoRnd(-30f, 30f) - fRadius; TmpStartPos.y += PseudoRnd(-30f, 30f) + fRadius;
        BirthPos = EmrgVehicleStartPos(TmpStartPos, startPos);
        //BirthPos = TmpStartPos;
        sect = ChiefName2+"1" + "_Road";
        key = "";
        value1 = BirthPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.z.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "  0 92 5 ";
        if (DEBUG) Console.WriteLine(value1);
        
        if (rnd.Next(2)==0) leftrightdis=-leftrightdis;
        edisplus1=PseudoRnd(1,8);
        edisplus2=PseudoRnd(-5,2);
        leftrightplus=rnd.Next(-3,4);
        
        disx=Math.Abs(BirthPos.x - aircraftPos.x) - (edis+edisplus1);
        if (disx<(edis+edisplus1)) disx=(edis+edisplus1);
        disy=Math.Abs(BirthPos.y - aircraftPos.y) - (edis-edisplus2);
        if (disy<(edis-edisplus2)) disy=(edis-edisplus2);

        BirthPos.x -= disx * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x)) - (leftrightdis + leftrightplus) * ((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y)); BirthPos.y -= disy * ((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y)) + (leftrightdis + leftrightplus) * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x));
        

        value2 = BirthPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.z.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat)  + "  0 92 5 ";;
        if (DEBUG) Console.WriteLine(value2);
        
        if (reverse ) {
          f.add(sect, key, value2);
          f.add(sect, key, value1);
        }else {
          f.add(sect, key, value1);
          f.add(sect, key, value2);
        }  


        TmpStartPos = closerStartPos;
        TmpStartPos.x += PseudoRnd(-30f, 30f) + fRadius; TmpStartPos.y += PseudoRnd(-30f, 30f) - fRadius;
        BirthPos = EmrgVehicleStartPos(TmpStartPos, startPos);
        
        sect = ChiefName3 + "2" + "_Road";
        key = "";
        value1 = BirthPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.z.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "  0 92 5 ";
        if (DEBUG) Console.WriteLine(value1);
        
        if (rnd.Next(2)==0) leftrightdis=-leftrightdis;
        edisplus1=PseudoRnd(-3,2);
        edisplus2=PseudoRnd(2,7);
        leftrightplus=rnd.Next(-3,4);
        
        disx=Math.Abs(BirthPos.x - aircraftPos.x) - (edis-edisplus1);
        if (disx<(edis-edisplus1)) disx=(edis-edisplus1);
        disy=Math.Abs(BirthPos.y - aircraftPos.y) - (edis+edisplus2);
        if (disy<(edis+edisplus2)) disy=(edis+edisplus2);

        BirthPos.x -= disx * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x)) - (leftrightdis + leftrightplus)*((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y)); BirthPos.y -= disy * ((BirthPos.y - aircraftPos.y) / Math.Abs(BirthPos.y - aircraftPos.y)) + (leftrightdis + leftrightplus) * ((BirthPos.x - aircraftPos.x) / Math.Abs(BirthPos.x - aircraftPos.x));
        value2 = BirthPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " " + BirthPos.z.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "  0 92 5 ";;
        if (DEBUG) Console.WriteLine(value2);
        if (reverse ) {
          f.add(sect, key, value2);
          f.add(sect, key, value1);
        }else {
          f.add(sect, key, value1);
          f.add(sect, key, value2);
        }

        if (DEBUG)
        {
            GamePlay.gpLogServer(null, "Writing Sectionfile to " + FULL_PATH + "autovehicle-ISectionFile.txt", new object[] { }); //testing
            f.save(FULL_PATH + "autovehicle-ISectionFile.txt"); //testing
        }

        return f;
        
      }
      catch (Exception e) {System.Console.WriteLine ("Section file: " + e.ToString()); return null; }  
    }

    //This simply traverses 10 points along a line from startPos to EndPos
    //looking for a spot that isn't water.  If all water, you end up with endPos.  If no water, you end up with startPos.
    internal Point3d EmrgVehicleStartPos(Point3d startPos, Point3d endPos)
    {
      try
      {
        Point3d TmpPos = startPos;

        /*
        while (((GamePlay.gpLandType(TmpPos.x, TmpPos.y) & LandTypes.WATER) != 0) ) 
        {
            TmpPos.x -= (TmpPos.x - endPos.x) / 10f;
            TmpPos.y -= (TmpPos.y - endPos.y) / 10f;
        };
        */
        //OK, while loops can be infinite. As we discovered via nasty server lockup issues.  So, more reasonably:
        for (int i=0; i<300; i++) {
           if ((GamePlay.gpLandType(TmpPos.x, TmpPos.y) & LandTypes.WATER) == 0) break;
           TmpPos.x -= (startPos.x - endPos.x) / 10f;
           TmpPos.y -= (startPos.y - endPos.y) / 10f;          
        }
        if ((GamePlay.gpLandType(TmpPos.x, TmpPos.y) & LandTypes.WATER) == 0) 
        return TmpPos;
        else return new Point3d (0,0,1000); //for now we just return a far distant point if the result is on water.  TODO: Make it a boat instead of a car, when on water.
      }
      catch (Exception e) {System.Console.WriteLine ("evsp: " + e.ToString()); return new Point3d (0,0,0); }  
        
    }

    internal void CheckEmrgCarOnAirport(int aircraftNumber, int delay_sec=0)
    {
      try
      {
        // ????????? ???? ?? ??????? ? ?????????         
        //MissionLoading=false; //kludgy fix, this seems to be in place to avoid mission load loops or something, but it doesn't work
        AiGroundGroup MyCar = null;   
         /****for unknown reasons, the section below just doesn't work, so 
          *we are not using it          
         if (DEBUG) GamePlay.gpLogServer(null, "Checking if cars on an airport " + CurPlanesQueue[aircraftNumber].basePos.Name(), new object[] { });     
        for (int i = 0; i < CurTechCars.Count; i++)
          {
              if (CurTechCars[i].TechCar != null )
              {                                                             
                  if (DEBUG) GamePlay.gpLogServer(null, "Checking cars on an airport 1 " + ( ( ~ServiceType.SPAWNIN & CurTechCars[i].CarType) == (ServiceType.SPAWNIN & CurPlanesQueue[aircraftNumber].State)).ToString() + " " +  CurPlanesQueue[aircraftNumber].State.ToString() + " " + CurTechCars[i].CarType.ToString(), new object[] { });
                  
                  if (CurTechCars[i].TechCar.IsAlive() && CurTechCars[i].basePos == CurPlanesQueue[aircraftNumber].basePos && (( CurTechCars[i].CarType & CurPlanesQueue[aircraftNumber].State) != 0 )) //can reuse cars if car type matches ANY of the plane states
                  {
                    //MissionLoading = false;
                    
                    
                    if ((CurTechCars[i].cur_rp == null) && (CurTechCars[i].RouteFlag == 0) && (CurTechCars[i].servPlaneNum == -1)) // ???? ????? ??? ???? - ???????? ????????
                        MyCar = CurTechCars[i].TechCar;
                        if (DEBUG) GamePlay.gpLogServer(null, "Re-using an old car group & setting new route at " + CurPlanesQueue[aircraftNumber].basePos.Name(), new object[] { });
                        SetEmrgCarRoute(aircraftNumber, i);                               
                    }
              }
          };             
        if ((MyCar == null) && !MissionLoading)
        */
        {           
            if (DEBUG) GamePlay.gpLogServer(null, "Creating a new car group at " + CurPlanesQueue[aircraftNumber].basePos.Name()  + " for " + aircraftNumber, new object[] { }); 
            MissionLoading = true;
            MissionLoadingAircraftNumber=aircraftNumber; //this is a kludge, trying to get the missions as they load in to match up with the aircraft # in the CurPlanesQueue

            //ArmyPos is which army controls the position where the airplane is.  We get if from the a/c position plus the Front info if it exists.
            //Otherwise, we just go with whatever the aircraft's army is.  (We could tweek this to use the army of the nearest airport instead, but that sounds like work . . . )
            int ArmyPos = 0;
            if (GamePlay.gpFrontExist())
            {
                ArmyPos = GamePlay.gpFrontArmy(CurPlanesQueue[aircraftNumber].aircraft.Pos().x, CurPlanesQueue[aircraftNumber].aircraft.Pos().y);
            }
            else { ArmyPos = CurPlanesQueue[aircraftNumber].aircraft.Army(); };            
            // ??????? ?????? ? ?????????
            //In case of spawn-in, we reverse the direction making it away from the plane.  
            
            if (DEBUG) GamePlay.gpLogServer(null, "Creating a new car group at " + ((int)CurPlanesQueue[aircraftNumber].State).ToString()+" " + ((int)ServiceType.SPAWNIN).ToString()+ " " + ( ((int)CurPlanesQueue[aircraftNumber].State & (int)ServiceType.SPAWNIN) ==0  ).ToString() + " basePos: " + CurPlanesQueue[aircraftNumber].basePos.ToString(), new object[] { });
                                                 
            bool reverse=true;
            if ( ((int)CurPlanesQueue[aircraftNumber].State & (int)ServiceType.SPAWNIN) ==0  ) reverse=false;

            //Start the vehicles, if it is allowed in this case
            if (CurPlanesQueue[aircraftNumber].basePos.startAllowed ) {
                ISectionFile f = CreateEmrgCarMission(CurPlanesQueue[aircraftNumber].basePos.Pos(), CAR_POS_RADIUS, ArmyPos, CurPlanesQueue[aircraftNumber].aircraft.Army(), CurPlanesQueue[aircraftNumber].aircraft.Type(), CurPlanesQueue[aircraftNumber].health, CurPlanesQueue[aircraftNumber].aircraft.Pos(), reverse, CurPlanesQueue[aircraftNumber].State);
                if (delay_sec == 0)
                    GamePlay.gpPostMissionLoad(f);
                else Timeout(delay_sec, () => {
                    MissionLoadingAircraftNumber = aircraftNumber;
                    GamePlay.gpPostMissionLoad(f);
                });
            }
              
             
            
        }        
        return ;
      }
      catch (Exception e) {System.Console.WriteLine ("cecoa: " + e.ToString());}  
    }

    public override void OnAircraftLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
    base.OnAircraftLanded(missionNumber, shortName, aircraft);  
    /*public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    { 
        base.OnAircraftLanded(missionNumber, shortName, aircraft); */
                
        StartVehiclesForAircraft (aircraft );
        aircraftActiveList.Remove(aircraft);
    } 


    public override void OnAircraftCrashLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
       base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
       StartVehiclesForAircraft (aircraft, false, 2, 25);
       aircraftActiveList.Remove(aircraft);
     /*{
     
        Timeout(5, () =>
        {
            aircraft.Destroy();
        });
    }*/
    
    
    
    
    }
    public override void OnPersonParachuteFailed(maddox.game.world.AiPerson person)
    {
        #region oppf
        base.OnPersonParachuteFailed(person);
        try
        {
            //GamePlay.gpLogServer(null, "Parachute Failed " + person.Player().Name(), new object[] { }); 
            
            //prob need person.Pos() or perhaps (person as AiActor).Pos()
        
            if (person != null) 
            {
              Player player=person.Player();
              System.Console.WriteLine("OnPersonParachuteFailed for " + player.Name());    
              StartVehiclesForAircraft (person as AiAircraft,false, 1, 20, true);
              if (player != null ) {
                
              }
            }
        }
        catch (Exception e) {System.Console.WriteLine ("parach1: " + e.ToString());}          
        #endregion
    }
    
    public override void OnPersonParachuteLanded(maddox.game.world.AiPerson person)
    {
        #region oppl
        base.OnPersonParachuteLanded(person);
        try
        {
            //GamePlay.gpLogServer(null, "Parachute Failed " + person.Player().Name(), new object[] { }); 
            
            //prob need person.Pos() or perhaps (person as AiActor).Pos()
        
            if (person != null) 
            {
              Player player=person.Player();
              System.Console.WriteLine("OnPersonParachuteLanded for " + player.Name());
              //AiAircraft fakeAC= new AiAircraft();
              //fakeAC
              StartVehiclesForAircraft (person as AiAircraft,false, 1, 20, true);
                  
              
              if (player != null ) {
                
              }
            }
        }
        catch (Exception e) {System.Console.WriteLine ("parach2: " + e.ToString());}          
        #endregion
    }      

//TODO: For bombers, this will re-spawn vehicles whenever they change places

public override void OnPlaceEnter (Player player, AiActor actor, int placeIndex) 
  {
        base.OnPlaceEnter(player, actor, placeIndex);
        try 
        { 
    
            AiAircraft aircraft= actor as AiAircraft;
        
            if (player != null) if (DEBUG) GamePlay.gpLogServer(null, "Place Enter: " + 
              player.Name() + " " + 
              actor.Name() + " " + 
              placeIndex.ToString() + " " , new object[] { });
          
            //do this only once per actor (avoids many multiple vehicle spawn-ins whwenever bomber players move between positions, triggering this OnplaceEnter repeatedly)
            if (!actorPlaceEnterList.Contains(actor)) {
               StartVehiclesForAircraft (aircraft, true ); //this is spawn-in, so we reverse vehicle direction
               actorPlaceEnterList.Add(actor);
            }


        }
        catch (Exception e) { System.Console.WriteLine("opeav: " + e.ToString()); }

    }

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {

        base.OnPlaceLeave(player, actor, placeIndex);
        try
        {
            if (actor as AiAircraft != null) {
                AiAircraft aircraft = actor as AiAircraft;
                
                //Add a/c to the recentleave list for 3.5 minutes after they leave the position.  Thus they can't avoid consequences like bringing
                //emergency vehicles by exiting just before the crash or before they have completely stopped & register as 'landed'
                aircraftRecentLeaveList.Add(aircraft);
                Timeout(210, () =>
                {
                    
                   aircraftRecentLeaveList.Remove(aircraft);

                });
            }
            
        }
        catch (Exception e) { System.Console.WriteLine("oplav: " + e.ToString()); }

    }



    public override void OnCarter(AiActor actor, int placeIndex)
    {
        base.OnCarter(actor, placeIndex);
    
        AiAircraft aircraft= actor as AiAircraft;
        
        if (DEBUG & actor != null) GamePlay.gpLogServer(null, "OnCarter: " +            
          actor.Name() + " " + 
          placeIndex.ToString() + " " , new object[] { });
        
    }

    public override void OnPersonMoved(AiPerson person, AiActor fromCart, int fromPlaceIndex)
    {
      try
      {
        base.OnPersonMoved(person, fromCart, fromPlaceIndex);
    
        AiAircraft aircraft= fromCart as AiAircraft;
        
        if (DEBUG) GamePlay.gpLogServer(null, "OnPersonMoved: " +            
          fromCart.Name() + " " +
          person.Name() + " " + 
          fromPlaceIndex.ToString() + " " , new object[] { });
      }
      catch (Exception e) {System.Console.WriteLine ("opm: " + e.ToString());}  
    }
       
    
    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        try
        {
        //AiAircraft aircraft= actor as AiAircraft;
        
           if (DEBUG) GamePlay.gpLogServer(null, "Starting vehicle/took off", new object[] { });
           
           if ( !isAiControlledPlane2(aircraft)) aircraftActiveList.Add(aircraft);
        }
        catch (Exception e) {System.Console.WriteLine ("oato: " + e.ToString());}
        
    } 
    
    
    public override void OnAircraftDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {              
        base.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
        try
        {
            if (aircraft==null) return;
            //just keep a list of all damaged aircraft
            aircraftDamagedList.Add(aircraft);
            //Add a/c to the active list if they are damaged & moving & not AI
            //Adding them to the active list if damaged while stopped leads to problems of many multiple emergency vehicles being sent.  Until we fix that
            //issue, we'll just not add them if they are damaged & also stopped.
            if ( aircraft.getParameter(part.ParameterTypes.Z_VelocityTAS, -1) > 2 && (!isAiControlledPlane2(aircraft) || aircraftRecentLeaveList.Contains(aircraft))) aircraftActiveList.Add(aircraft);                             

        }
        catch (Exception e) {System.Console.WriteLine ("oad: " +e.ToString());}
        
        //add your code here
    }

    public override void OnAircraftCutLimb(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, LimbNames limbName)    
    {
        
        base.OnAircraftCutLimb(missionNumber, shortName, aircraft, initiator, limbName);
        try
        {
            //just keep a list of all damaged aircraft
            aircraftDamagedList.Add(aircraft);
            if ( !isAiControlledPlane2(aircraft) || aircraftRecentLeaveList.Contains(aircraft)) aircraftActiveList.Add(aircraft);
        }
        catch (Exception e) {System.Console.WriteLine ("oacl: " + e.ToString());}
            
    }
    
    public void OnAircraftStopped (AiAircraft aircraft)
    {
        //base.OnAircraftStopped(aircraft);
        try
        {   
           if (aircraft != null ) {
           
                      if (DEBUG) GamePlay.gpLogServer(null, "Aircraft detected as stopped/landed: " + 
                      //aircraft.Player(0).Name() + " " + 
                      aircraft.Name() + " ", new object[] { });

              StartVehiclesForAircraft (aircraft );
              aircraftActiveList.Remove(aircraft);  //The only way they can get back on the list is by taking off again, or by being damaged.  Given the way CloD works, this might not be possible if they just landed in a field or whatever.  But them's the breaks . . . 
           }   
               
        }
        catch (Exception e) {System.Console.WriteLine ("oas: " + e.ToString());}     
        
    }         
    
    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        #region oad
        base.OnActorDead(missionNumber, shortName, actor, damages);
        try
        {   
           if (actor as AiAircraft != null ) {
              //Actor = dead = big explosion = send in 3 groups of emerg. vehicles @ interval 10 secs
              StartVehiclesForAircraft (actor as AiAircraft, false, 3, 25 );
              aircraftActiveList.Remove(actor as AiAircraft);
           }      
               
        }
        catch (Exception e) {System.Console.WriteLine ("OnActorDead: " + e.ToString());}
        #endregion
        
    }
  
    //re=write to use actorPos instead of AiAircraft.  It will just pass the
    //name, pos, & type on & that's all we need.
    //new overload methods for AiAircraft & Person, Actor, whatever
     public void StartVehiclesForAircraft (AiAircraft aircraft, bool spawnIn=false, int groupsToSpawn=1, int timeToWait_sec=25, bool isPerson=false ) {
          //try 
          {

            if (DEBUG) GamePlay.gpLogServer(null, "StartVehicles, status: Overall-" + ((isAiControlledPlane2(aircraft) || (!aircraftActiveList.Contains(aircraft)) && !spawnIn && !isPerson && !aircraftRecentLeaveList.Contains(aircraft))).ToString() + " isaicontr-" + isAiControlledPlane2(aircraft).ToString() + " spawnin-" + spawnIn.ToString() + " activelist-" + aircraftActiveList.Contains(aircraft).ToString() + " isperson-"+ isPerson.ToString() + " acleavelist-" + aircraftRecentLeaveList.Contains(aircraft).ToString() + " ", new object[] { });
            if (aircraft==null ||  (( isAiControlledPlane2(aircraft) || (!aircraftActiveList.Contains(aircraft)) && !spawnIn && !isPerson && !aircraftRecentLeaveList.Contains(aircraft))) ) {
              if (DEBUG) GamePlay.gpLogServer(null, "aircraft = null OR AIcontroledaircraft OR not on active list (with a few exceptions); exiting startvehicles", new object[] { }); 
              return;
            }  
            if (DEBUG) GamePlay.gpLogServer(null, "Getting Airports for startvehicles", new object[] { });
            BasePos NearestAirport = new BasePos();
            NearestAirport = FindNearestAirport(aircraft);          
    
            if (DEBUG) GamePlay.gpLogServer(null, "Starting vehicles at " + NearestAirport.Name(), new object[] { });
            
            if (NearestAirport != null && NearestAirport.startAllowed)
            {
                
                PlanesQueue CurPlane = new PlanesQueue(aircraft, NearestAirport, 0);
                int ArmyPos = 0;
                CurPlane.health = (float)aircraft.getParameter(part.ParameterTypes.M_Health, -1);
                
                if (aircraftDamagedList.Contains(aircraft)) CurPlane.health /=2;
                //CurPlane.health=0; //testing
                
                /*
                //The code below runs fine but Battle.GetDamageInitiators(aircraft as AiActor); never seems to return anything but null? 
                //ArrayList a = new ArrayList();
                //a = Battle.GetDamageInitiators(aircraft as AiActor);
                //if (a.Count>0) CurPlane.health=0.5F;//The plane has been hit . . .
                //if (DEBUG) GamePlay.gpLogServer(null, "Times a/c damaged: " + a.Count , new object[] { });
                if (DEBUG) {
                  foreach (AiDamageInitiator ds in a) {
                   GamePlay.gpLogServer(null, "di = " + ds.Player.Name() , new object[] { });
                  }
                }  
                 
                */
                
                float cdam = (float)aircraft.getParameter(part.ParameterTypes.M_CabinDamage, 1);
                float ndam = (float)aircraft.getParameter(part.ParameterTypes.M_NamedDamage, 1);
               
                //note that MANY of the CurPlane.States set below do not do anything
                //because the actual type of ai vehicle spawned is determined
                //in the createemrgcarmission method via various criteria including
                //army, health, type of plane etc but not really using the states
                //set here AT ALL. FYI. 
                if (DEBUG) GamePlay.gpLogServer(null, "Health = " + CurPlane.health.ToString() + " " + cdam.ToString() + " " + ndam.ToString(), new object[] { });  
                
                if (spawnIn) CurPlane.State |= ServiceType.SPAWNIN;
                if (GamePlay.gpFrontExist())
                {
                    ArmyPos = GamePlay.gpFrontArmy(NearestAirport.Pos().x, NearestAirport.Pos().y);
                }
                else { ArmyPos = aircraft.Army(); };
                //if (true || CurPlane.health < 1f) //testing
                //Worse health, more damage - more ambulances etc.
                if (CurPlane.health < 1f)  
                {
                    CurPlane.State |= ServiceType.EMERGENCY;
                    CurPlane.State |= ServiceType.FIRE;
                    groupsToSpawn++;
                    if (CurPlane.health < 0.5f) groupsToSpawn++;
                    if (CurPlane.health < 0.05f) groupsToSpawn++;
                }
                else if (aircraft.Army() == ArmyPos)
                {
                    CurPlane.State |= ServiceType.FUEL;
                    CurPlane.State |= ServiceType.AMMO;
                    if (aircraft.Type() == AircraftType.Bomber) CurPlane.State |= ServiceType.BOMBS;
                    CurPlane.State |= ServiceType.BOMBS;
                };
                //if (true || !(aircraft.Army() == ArmyPos)) CurPlane.State |= ServiceType.PRISONERCAPTURE; //testing
                if (!(aircraft.Army() == ArmyPos)) CurPlane.State |= ServiceType.PRISONERCAPTURE;
                if (!CurPlanesQueue.Contains(CurPlane))
                {
                    //if (DEBUG) GamePlay.gpLogServer(null, "Starting vehicles at " + NearestAirport.Name()+ " Type: " + CurPlane.State, new object[] { });
                    //Spawn groupsToSpawn groups @ interval timeToWait
                    
                    
                    //  Timeout (timeToWait_sec*1, () => {
                    //for some reason we can't add planes to the queue on timeout, then immediately checkemrg . . . 
                    //so instead we add all planes ot queue first, then we can call checkemrgcar at our leisure 
                    try {

                      if (groupsToSpawn > 2) groupsToSpawn = 2;
                      int newpc=0;
                      for (int i=0; i<groupsToSpawn; i++) {
                        if (DEBUG) GamePlay.gpLogServer(null, "Starting vehicles at " + CurPlane.basePos.Name() + " Type: " + CurPlane.State, new object[] { }); 
                        CurPlanesQueue.Add(CurPlane);
                        newpc = CurPlanesQueue.Count - 1;
                        CheckEmrgCarOnAirport(newpc, timeToWait_sec*i);
                        Point3d temp3d = CurPlane.basePos.Pos();
                        if (aircraft.Pos().distance(ref temp3d)>3000 ) CurPlane.basePos=FindNearestAirport(aircraft); //Get a new airport (in cases where the plane isn't too close to an airport anyway.
                      }  
                      /*for (int i=0; i<groupsToSpawn; i++) {
                        int savei=i;  //you can't send loop, foreach, List & similar variables through Timeout, dur, they malfunction in a few ways
                        //Timeout (timeToWait_sec*i+2, () => {
                          Console.WriteLine ("savei="+i.ToString());
                          CheckEmrgCarOnAirport(newpc[savei]); 
                        //});                        
                      }*/  
                    } catch (Exception e) {System.Console.WriteLine ("IFOR: " + e.ToString());}
                    //  });  
                    //}                  
                }
                else
                {
                    for (int i = 0; i < CurPlanesQueue.Count; i++)
                        if (CurPlanesQueue[i] == CurPlane)
                        { 
                            CheckEmrgCarOnAirport(i);                        
                            break;
                        }
                }
                CurPlane = null;
            }
          } 
          //catch (Exception e) {System.Console.WriteLine ("StartVehicles: " + e.ToString());} 
     }

     private void checkForStoppedAircraft (HashSet <AiAircraft> aircraftList){
       try 
       {
         int a_count=0;
         double Z_VelocityTAS;
         
         foreach (AiAircraft a in aircraftList ) {
         
           Z_VelocityTAS = a.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
           
           //If true airspeed is 0 now, check again in 5 seconds.  If still 0 we're assuming crashed and/or landed somehow.
           if (Z_VelocityTAS == 0) {
           
              Timeout (5, () => { 
                 Z_VelocityTAS = a.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
                 if (Z_VelocityTAS == 0) OnAircraftStopped(a);
              
              });
           
           
           }
         
         }
       }  
       catch (Exception e) {System.Console.WriteLine ("StoppedMoving: " + e.ToString());}  
         
     
     
     }    
    
     //Returns whether aircraft is an Ai plane (no humans in any seats)
     //Actually this should work for any actor, not just AiAircraft
     private bool isAiControlledPlane2(AiAircraft aircraft)  
     { // returns true if specified aircraft is AI controlled with no humans aboard, otherwise false
       try
       {
        if (aircraft == null) return false;
        //check if a player is in any of the "places"
        for (int i = 0; i < aircraft.Places(); i++)
        {
           if (aircraft.Player(i) != null) return false;
        }
        return true;
       }
       catch (Exception e) {System.Console.WriteLine ("iacp: " + e.ToString()); return true;} 
     }
     
     /*private bool isAiControlledActor2(AiActor a)  
     { // returns true if specified item is AI controlled with no humans aboard, otherwise false
        if (a == null) return false;
        //check if a player is in any of the "places"
        for (int i = 0; i < a.Places(); i++)
        {
           if (a.Player(i) != null) return false;
        }
        return true;
     } */
    
    /*    
    private bool destroyAIGroup (AiGroup group, bool destroyElements=true, bool destroyGroup=true ) {
      bool success=false;
            
      if (group != null) 
      {
          if (destroyElements && group.GetItems() != null && group.GetItems().Length > 0)
          {
              foreach (AiActor a in group.GetItems())
              {
    
                  if (a != null && (a as AiAircraft) != null && isAiControlledPlane2(a as AiAircraft))
                  {                                               
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying group element: " + //a.AirGroup() + " " 
                     + a.CallSign() + " " 
                     + a.Type() + " " 
                     + a.TypedName() + " " 
                     //+  a.AirGroup().ID()
                     , new object[] { });                                            
                    
                    a.Destroy();
                    success=true;                                                                        
                  }
              }
          }
          if (destroyGroup) {
             if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Finished destroying group: " +  
             + group.CallSign() + " " 
             + group.Type() + " " 
             + group.TypedName() + " " 
             + group.AirGroup().ID()
             , new object[] { });
   
            //group.Destroy();
            
            (group as AiActor).Destroy();
            success=true;
         }   
      }        
      return success; 
    }
    */
    
    private bool destroyAirGroup (AiAirGroup group, bool destroyElements=true, bool destroyGroup=true ) {
      try
      {
          bool success=false;
                
          if (group != null) 
          {
              if (destroyElements && group.GetItems() != null && group.GetItems().Length > 0)
              {
                  foreach (AiActor a in group.GetItems())
                  {
        
                      AiAircraft aircraft=(a as AiAircraft);   
                      if (aircraft != null && isAiControlledPlane2(aircraft))
                      {                                               
                        if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying group element: " + aircraft.AirGroup() + " " 
                         + aircraft.CallSign() + " " 
                         + aircraft.Type() + " " 
                         + aircraft.TypedName() + " " 
                         +  aircraft.AirGroup().ID()
                         , new object[] { });                                            
                        
                        aircraft.Destroy();
                        success=true;                                                                        
                      }
                  }
              }
              if (destroyGroup) {
                 if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Finished destroying group: "  
                 //+ group.CallSign() + " " 
                 //+ group.Type() + " " 
                 //+ group.TypedName() + " " 
                 //+ group.AirGroup().ID()
                 , new object[] { });
       
                //group.Destroy();
                
                if ((group as AiAircraft) != null) (group as AiAircraft).Destroy();
                success=true;
             }   
          }        
          return success; 
       }
       catch (Exception e) {System.Console.WriteLine ("dag1: " + e.ToString()); return false;}   
    }

    //This doesn't destroy all members of an arbitrary ground group.  Rather it is to specifically destroy
    //the members of ground groups created in Auto Generate Vehicles via ISectionFiles above.
    //These may have some static members with names like 2:1_Static
    //They will have several ground group members with names like 2:0_Chief_Fire_0 or 2:0_Chief_Ammo_1 (last # ranges 0 through 2)
    //Then their elements are named like 2:0_Chief_Fire_00, 2:0_Chief_Fire_01, 2:0_Chief_Fire_02, 2:0_Chief_Fire_03 or 2:0_Chief_Ammo_10, 2:0_Chief_Ammo_11, etc
    //Last number is usually just 0 or 1 but can range 0-3.  Also often the 2nd element is a trailer, so 2:0_Chief_Fire_00 is the main actor, 2:0_Chief_Fire_01 is the  trailer & so
    //it doesn't seem to show up in the "get items" of the groundgroup.  You just sort of have to know it is there & attached to 2:0_Chief_Fire_00
    //Anyway, to delete the items in a group we created named 2:0_Chief_Fire_0, all we have to do is cycle through 2:0_Chief_Fire_00, 2:0_Chief_Fire_01, 2:0_Chief_Fire_02, 2:0_Chief_Fire_03
    //and delete any or all.
    //This isn't a generic "delete any ground group" routine but is specific to the way we created our groups above using the ISectionFiles.
    private bool destroyAGVGroundGroup (AiGroundGroup group, bool destroyElements = true, bool destroyGroup = true)
    {
      try
      {
        bool success=false;

              
        if (group != null) 
        {

            //first, we get rid of any stationaries associated with this mission
            string missionNumber = group.Name().Split(':')[0];

            
            //string subStatName = missionNumber + ":" + i.ToString() + "_Static";
            string subStatPrefix = missionNumber + ":";
                
            if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying Stationaries associated with Mission "
                    + subStatPrefix, new object[] { });

            if (GamePlay.gpGroundStationarys() != null)
            {
                foreach (GroundStationary gg in GamePlay.gpGroundStationarys(group.Pos().x, group.Pos().y, 5000.0)) //all stationaries w/i 5000 meters of this object
                {
                    if (gg.Name.StartsWith(subStatPrefix)) {
                        gg.Destroy();
                            if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed "
                        + gg.Name, new object[] { });
                    }
                        
                }
            }


            //Now we destroy any Actors or sub-Actors of this group
            if (destroyElements)
            {
                for (int i = 0; i < 4 ;  i++ )
                {
                    string subName = group.Name() +(i).ToString();
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying group element: "
                            + subName, new object[] { });
                    AiGroundActor subActor = GamePlay.gpActorByName(subName) as AiGroundActor;
                    if (subActor != null)
                    {
                        if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed : "
                            + subActor.Name(), new object[] { });

                        //destroyGroundGroup(subActor as AiGroundGroup);
                        (subActor as AiCart).Destroy();
                    }
                }   
            }      
                       

            if (destroyGroup)
            {
                if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Finished destroying group: "
                    + group.Name() + " "
                //+ group.CallSign() + " " 
                //+ group.Type() + " " 
                //+ group.TypedName() + " " 
                //+ group.AirGroup().ID()
                //+ group.GroupType() + " "
                //+ group.ID() + " "
                , new object[] { });


                //group.Destroy();

                if ((group as AiGroundActor) != null) (group as AiGroundActor).Destroy();
                success = true;

            }            
             
        }
          
        return success;
      }
      catch (Exception e) {System.Console.WriteLine ("dag2: " + e.ToString()); return false;}  
    }


    //This isn't used now & doesn't completely work.  PRobably can get rid of it.
    private bool destroyGroundGroup(AiGroundGroup group, bool destroyElements = true, bool destroyGroup = true)
    {
        try
        {
            bool success = false;


            if (group != null)
            {

                //first, we get rid of any stationaries associated with this mission
                string missionNumber = group.Name().Split(':')[0];


                //string subStatName = missionNumber + ":" + i.ToString() + "_Static";
                string subStatPrefix = missionNumber + ":";

                if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying Stationaries associated with Mission "
                        + subStatPrefix, new object[] { });

                if (GamePlay.gpGroundStationarys() != null)
                {
                    foreach (GroundStationary gg in GamePlay.gpGroundStationarys(group.Pos().x, group.Pos().y, 5000.0)) //all stationaries w/i 5000 meters of this object
                    {
                        if (gg.Name.StartsWith(subStatPrefix))
                        {
                            gg.Destroy();
                            if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed "
                        + gg.Name, new object[] { });
                        }

                    }
                }

                /*
                //we're just destroying ALL statics relate to the mission immediately with the first mission item destroyed
                //We can refine this later if wanted
                string missionNumber = group.Name().Split(':')[0];

                {
                    string subStaticName = missionNumber + ":" + i.ToString() + "_Static";
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying : "
                            + subStaticName, new object[] { });
                    GroundStationary stat = GamePlay.gpActorByName(subStaticName) as GroundStationary;
                    if (stat != null)
                    {
                        if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed : "
                            + stat.Name, new object[] { });

                        destroyGroundGroup(subGroupActor as AiGroundGroup);
                        (subGroupActor as AiCart).Destroy();
                    }
                }
                */

                /* GroundStationary stat = GamePlay.gpActorByName(subStaticName) as GroundStationary;
                if (stat != null)
                {
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed : "
                        + stat.Name, new object[] { });

                    destroyGroundGroup(subGroupActor as AiGroundGroup);
                    (subGroupActor as AiCart).Destroy();
                }*/

                //Now we destroy any Actors or sub-Actors of this group
                if (destroyElements && group.GetItems() != null && group.GetItems().Length > 0)
                {
                    foreach (AiActor a in group.GetItems())
                    {

                        AiGroundActor actor = (a as AiGroundActor);
                        if (actor != null)
                        {
                            //Getting rid of the trailers
                            //They have a name ending in 01 or 03 or the like
                            for (int i = 0; i < 2; i++)
                            {
                                string subName = actor.Name() + ((int)(2 * i + 1)).ToString();
                                if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying : "
                                        + subName, new object[] { });
                                AiGroundActor subActor = GamePlay.gpActorByName(subName) as AiGroundActor;
                                if (subActor != null)
                                {
                                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed : "
                                        + subActor.Name(), new object[] { });

                                    destroyGroundGroup(subActor as AiGroundGroup);
                                    (subActor as AiCart).Destroy();
                                }
                            }


                            if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying group element: " // + a.AirGroup() + " " 
                                                                                                      //+ a.CallSign() + " " 
                            + actor.Type() + " "
                            + a.Name() + " "
                            + group.GroupType() + " "
                            + group.ID() + " "
                            //+ a.TypedName() + " " 
                            //+  a.AirGroup().ID()
                            , new object[] { });

                            //actor.Destroy();  //hmm, perhaps this needs to be recursive here?  Can we have nested objects @ several levels?
                            destroyGroundGroup(actor as AiGroundGroup); //for now this doesn't seem to do anything but I think it is at least potentially true that an actor can also have group members
                            actor.Destroy();  //hmm, perhaps this needs to be recursive here?  Can we have nested objects @ several levels?
                            success = true;

                            //trying to get the trailers etc




                        }
                    }
                }
                /*
                string subGroupName = group.Name() + "1";
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying : "
                            + subGroupName, new object[] { });
                    AiGroundActor subGroupActor = GamePlay.gpActorByName(subGroupName) as AiGroundActor;
                if (subGroupActor != null)
                {
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed : "
                        + subGroupActor.Name(), new object[] { });

                    destroyGroundGroup(subGroupActor as AiGroundGroup);
                    (subGroupActor as AiCart).Destroy();
                }
                */

                //Getting rid of the trailers
                //They have a name ending in 01 or 03, 11, 13, or the like
                for (int i = 0; i < 2; i++)
                {
                    string subName = group.Name() + ((int)(2 * i + 1)).ToString();
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroying : "
                            + subName, new object[] { });
                    AiGroundActor subActor = GamePlay.gpActorByName(subName) as AiGroundActor;
                    if (subActor != null)
                    {
                        if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Destroyed : "
                            + subActor.Name(), new object[] { });

                        destroyGroundGroup(subActor as AiGroundGroup);
                        (subActor as AiCart).Destroy();
                    }
                }



                if (destroyGroup)
                {
                    if (DEBUG) GamePlay.gpLogServer(null, "DEBUG: Finished destroying group: "
                        + group.Name() + " "
                    //+ group.CallSign() + " " 
                    //+ group.Type() + " " 
                    //+ group.TypedName() + " " 
                    //+ group.AirGroup().ID()
                    //+ group.GroupType() + " "
                    //+ group.ID() + " "
                    , new object[] { });


                    //group.Destroy();

                    if ((group as AiGroundActor) != null) (group as AiGroundActor).Destroy();
                    success = true;
                }
            }
            return success;
        }
        catch (Exception e) { System.Console.WriteLine("dag2: " + e.ToString()); return false; }
    }

    public AiAircraft GetNearestFriendlyAircraft(AiActor act)
    {   // Purpose: Returns the nearest frinedly aircraft to the specified actor. 
        // Use: GamePlay.gpNearestFriendlyAircraft(actor);
        if (act == null) return null;
        AiAircraft NearestAircraft = null;
        Point3d P = act.Pos();
        

        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in GamePlay.gpArmies())
            {                
                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                {
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {                        
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft aircraft = actor as AiAircraft;
                                    if (aircraft != null)
                                    {
                                        if ( NearestAircraft == null || aircraft.Pos().distance(ref P) < NearestAircraft.Pos().distance(ref P))
                                        //if ( aircraft.Pos().distance(ref P) < NearestAircraft.Pos().distance(ref P))
                                            NearestAircraft = aircraft;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return NearestAircraft;
    }

    //Put a player into a certain place of a certain plane.
    private bool putPlayerIntoAircraftPosition (Player player, AiActor actor, int place){
      if (player != null && actor!=null && (actor as AiAircraft!=null)) 
      {
         AiAircraft aircraft = actor as AiAircraft;
         player.PlaceEnter(aircraft, place);
            return true;
      }
      return false;
    }
        
     private string Left(string Original, int Count)
     {      
        if (Original == null || Original == string.Empty || Original.Length <
        Count) {
          return Original;
        } else {
          // Return a sub-string of the original string, starting at index 0.
          return Original.Substring(0, Count);
        }
     }       


}

//Various helpful calculations, formulas, etc.
public static class Calcs
{

    public static int GetFirstDiffIndex(string str1, string str2)
    {
        if (str1 == null || str2 == null) return -1;

        int length = Math.Min(str1.Length, str2.Length);

        for (int index = 0; index < length; index++)
        {
            if (str1[index] != str2[index])
            {
                return index;
            }
        }

        return -1;
    }
}

/*
public class FakeAircraft : AiAircraft
{
        private Point3d _pos;
        private string _name;
        public override Point3d Pos( object p = null)        
        {
          base.Pos(p);
          if (p!= null & ( p as Point3d != null ) _pos=p as Point3d;
          return _pos;      
        }
        public override string Name( string s = null)        
        {
          base.Name(s);
          if (s != null) _name=s;
          return _name;      
        }
 
}
*/

