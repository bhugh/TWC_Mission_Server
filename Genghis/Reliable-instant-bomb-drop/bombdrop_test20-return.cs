using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

using System.Net;

using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;
using part;

public class Mission : AMission
{
    public Random ran;
    public int dispCount = 0;
    
	public override void Inited()
	{
		base.Inited();
	}


 public override void Init(ABattle b, int missionNumber)

    {
        base.Init(b, missionNumber);
        MissionNumberListener = -1; //This is what allows you to catch all the OnTookOff, OnAircraftDamaged, and other similar events.  Vitally important to make this work!
        ran = new Random();
         
    }

    public override void OnBombExplosion(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {

        base.OnBombExplosion(title, mass_kg, pos, initiator, eventArgInt);
        //Console.WriteLine("Bomb (main) {0} {1} ({2:N0} {3:N0} {4:N0})", title, mass_kg, pos.x, pos.y, pos.z);
        
    }
    
    public override void OnTickGame()
    {
        base.OnTickGame();

        //if (Time.tickCounter() % 305 == 41) //about 1.5 seconds?
        if (Time.tickCounter() % 50 == 41) //2105 about 5 seconds?  2020/03/31; 5105 - 2023-01
        {
						         Console.WriteLine("===========RUN #{0}==============",dispCount);
                                  AllAircraftInGame(this);
								  dispCount ++;
        }
    }
    
    public List<part.ParameterTypes> plist = new List<part.ParameterTypes> () {
    part.ParameterTypes.M_Health, part.ParameterTypes.S_Bombenabwurfgerat, ParameterTypes.S_BombReserve, ParameterTypes.S_FuelReserve, ParameterTypes.S_GunClipReserve, ParameterTypes.S_GunOperation, ParameterTypes.S_GunReserve };
    
    public List<AiAirGroupTask> tlist = new List<AiAirGroupTask> () {AiAirGroupTask.ATTACH, AiAirGroupTask.ATTACK_AIR, AiAirGroupTask.ATTACK_GROUND, AiAirGroupTask.DEFENDING, AiAirGroupTask.DO_NOTHING, AiAirGroupTask.FLY_WAYPOINT, AiAirGroupTask.PURSUIT, AiAirGroupTask.UNKNOWN};
    
    public List<AiAirWayPointType> aalist = new List<AiAirWayPointType>() {AiAirWayPointType.AATTACK_BOMBERS, AiAirWayPointType.AATTACK_FIGHTERS, AiAirWayPointType.COVER, AiAirWayPointType.ESCORT, AiAirWayPointType.FOLLOW, AiAirWayPointType.GATTACK_POINT, AiAirWayPointType.GATTACK_TARG, AiAirWayPointType.HUNTING, AiAirWayPointType.NORMFLY, AiAirWayPointType.RECON }; 
    
    public int ct = 0; 
    public AiAirGroup targ = null;
    
    public void changeWP(AiAirGroup airGroup, AiActor act) {
       try {
       
         if (airGroup == null) return;
         //Console.WriteLine("changeWP1");
         List<AiWayPoint> NewWaypoints = new List<AiWayPoint>();
         //Console.WriteLine("changeWP2");
         Point3d apos = (airGroup as AiActor).Pos();
         //Console.WriteLine("changeWP3");
         
         for (int i =0; i<14; i++) {
            //Console.WriteLine("changeWP4");
            double add = ran.Next(-10000,10000);
            if (i==0) add = ran.Next(-100,100);
            add = i * -10;      
            if (i>1) add = i * -10000;

            var pos = new Point3d (apos.x + add, apos.y, apos.z );
            //var pos = new Point3d (apos.x + ran.Next(10000), apos.y - ran.Next(10000), apos.z + ran.Next(400) );
            double speed = ran.Next(100); 
            var aaWP = new AiAirWayPoint(ref pos, speed);
			aaWP.Action = AiAirWayPointType.NORMFLY;
            if (i==1) aaWP.Action =AiAirWayPointType.GATTACK_POINT;
            //if (act !=null) aaWP.Target = act;
            Console.WriteLine("New Waypoint: {0:N0} {1:N0} {2:N0} {3:N0} {4}", pos.x, pos.y, pos.z, speed, aaWP.Action);
            NewWaypoints.Add(aaWP);                    
         }                      
         airGroup.SetWay(NewWaypoints.ToArray());
         //airGroup.changeGoalTarget(null);
		 //airGroup.setTask (AiAirGroupTask.ATTACK_GROUND, null);
         Console.WriteLine("Task Change, {0} ", airGroup.Name());
      } catch (Exception ex) {Console.WriteLine("EXCEPTION: {0}", ex);}   
       
    }
    
    public List<AiAircraft> AllAircraftInGame(AMission msn)
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
                            //airGroup.setTask(AiAirGroupTask.ATTACK_GROUND, null) ;
                            //if (DEBUG) DebugAndLog ("DEBUG: Army, # in airgroup:" + army.ToString() + " " + airGroup.GetItems().Length.ToString());            
                            Console.WriteLine("DEBUG: {0} task: {1} # in airgroup:" + airGroup.GetItems().Length.ToString(), airGroup.Name(), airGroup.getTask());
                            
                            //airGroup.setTask(tlist[ct%8], targ);
                            if (targ == null) targ = airGroup;
                            if (dispCount  >= 300) changeWP(airGroup, targ as AiActor);
                            //airGroup.changeGoalTarget(targ as AiActor);
                            //targ = airGroup;
                            
                            ct ++;
                            if (airGroup.GetItems().Length > 0) foreach (AiActor actor in airGroup.GetItems())
                                {
                                    
                                    if (actor != null && (actor as AiAircraft != null))
                                    {
                                        ret.Add(actor as AiAircraft);
                                        AiAircraft a = actor as AiAircraft;
                                        //AircraftControls controls = a.GetControls();
                                        /*
                                        foreach  (ParameterTypes pt in plist) {
                                            for (int i = 0; i <256 ; i++) {
                                                    try {
                                                        Console.WriteLine ("Param {0} #{1}: {2}", pt, i, a.getParameter(pt, i));
                                                    } catch (Exception ex) {}
                                                }
                                        }  */
                                        
                                    }

                                }
                        }
                    }
            }

        }
        return ret;
    }

}
