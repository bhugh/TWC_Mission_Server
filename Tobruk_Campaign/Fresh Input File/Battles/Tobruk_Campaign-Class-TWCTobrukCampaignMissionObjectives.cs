////$include "$user\missions\Multi\Fatal\Tobruk_Campaign\Fresh Input File\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesNeg10.cs"
//$include "$user\missions\Multi\Fatal\Tobruk_Campaign\Fresh Input File\Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesBattles.cs"
//$include "$user\missions\Multi\Fatal\Tobruk_Campaign\Fresh Input File\Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesPos100.cs"

//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 


using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using maddox.game;
using maddox.game.world;
using maddox.GP;
using maddox.game.page;
using part;
using mmoobj = Mission.MissionObjectives;
using M = Mission;
//using Mission;

/*
//Used to store (you guessed it) info about the SubMissions, such as which campaign scores they are valid for
public class SubMissionClassInfo
{
    double leastScore;
    double mostScore;
    TWCTobrukCampaignMissionObjectives subMission;

    public SubMissionClassInfo(
        double least_score, double most_score, TWCTobrukCampaignMissionObjectives sub_mission
        )
    {
        leastScore = least_score;
        mostScore = most_score;
        subMission = sub_mission;
    }
}
*/


public abstract class TWCTCMO
{
    public abstract Mission.MissionObjectives mmo { get; set; }
    public abstract TWCTCMO current_subclass { get; set; }
    public abstract double score { get; set; } //-1000000.111 is a "magic number" meaning no subclass
    //private int? current_subclass; //was going to make this double but given that     
    public abstract string focus_airport_misfile_name { get; set; }
    public abstract double leastScore { get; set; } //Every subclass MUST include this value, which is the least campaign score value for which it will be loaded (>=)
    public abstract double mostScore { get; set; }   //Every subclass MUST include this value, which is the greatest campaign score value for which it will be loaded (<)


    public TWCTCMO()
    {

    }

    public void UpdateMissionObjectives(M.MissionObjectives mo)
    {
        mmo = mo;
        try
        {
            if (current_subclass != null) current_subclass.UpdateMissionObjectives(mmo);
        }
        catch (Exception ex) { Console.WriteLine("UpdateMissionObjectives ERROR: " + ex.ToString()); }
    }

    public abstract void OverrideCampaignTurnMapRequirements();

    public abstract void RadarPositionTriggersSetup(bool addNewOnly = false);

    public abstract void MissionObjectiveTriggersSetup(bool addNewOnly = false);

    public abstract void FlakDictionariesSetup();

    public abstract void BumRushCampaignValuesSetup();

    public abstract void MissionObjectiveAirfieldFocusBumrushSetup();

    //Nothing needed at this level; we just call the corresponding routine in current_subclass.ReadInitialFocusAirportSubmission which does all the mission-specific work
    public abstract void ReadInitialFocusAirportSubmission();

    public abstract void LaunchABumrush(int objectivesAchievedArmy, int attackingArmy, string AirportName);

}




public class TWCTobrukCampaignMissionObjectives : TWCTCMO {
    public Mission msn;
    public override Mission.MissionObjectives mmo { get; set; }
    public maddox.game.IGamePlay gp;
    public override double score { get; set; } 
    //private int? current_subclass; //was going to make this double but given that 
    private List<TWCTCMO> TWCMissionObjectives_subclasses;
    public override TWCTCMO current_subclass { get; set; } 
    public override string focus_airport_misfile_name { get; set; }
    public override double leastScore { get; set; } //Every subclass MUST include this value, which is the least campaign score value for which it will be loaded (>=)
    public override double mostScore { get; set; }   //Every subclass MUST include this value, which is the greatest campaign score value for which it will be loaded (<)
                                                     //Typically if the class is for say point value 100, you set it 50,150 so as to bracket the desired mission point value.
                                                     //Or you set them -100,0 ; 0,100 ; 100,200 ; 200-300, so that players much reach 10 points (for example) before that submission kicks in
                                                     //Note that >= is used on the bottom and < on the top so that you can overlap as shown above & only one submission will be operative
                                                     //HOWEVER nothing is checking that you have avoided overlapping.  So if you have -100,0 & -50,50 & 0,100 submission then, TWO of them
                                                     //will be operative AT ONCE for score of -50 to 50.  Only one will actually be chosen and you won't be able to predict which.  Just avoid doing this.
                                                     //When one side wins the Bumrush and turns the map, they are awarded 10,000 player points.  1 "Score" point = 100 Player Points, so
                                                     //Advancing 10,000 Player Points means advancing 100 score point.  So think of each phase as being incremented by 100, like -400, -300, -200, -100, 0, 100, 200, 300, 400 etc

    //An initializer with no input fields is required in order to make descendants of this class
    public TWCTobrukCampaignMissionObjectives()
    {
    }

    //This will set up Tobruk-specific overall/general mission objectives for the overall mission, according to the current Campaign Score (score)
    //It will then pass the routines along at the appropriate points, and to the appropriate
    public TWCTobrukCampaignMissionObjectives(maddox.game.IGamePlay g, Mission m, double sc)
    {
        TWCMissionObjectives_subclasses = new List<TWCTCMO>() { };
        msn = m;
        //mmo = mo;
        mmo = msn.mission_objectives; //so when first passed (in Mission constructor) this is NULL.  It is updated/initialized after a while.  It MUST be updated via a call to UpdateMissionObjectives() later (UpdateMissionObjectives() is defined in TWCTMO above)
        gp = g;

        mostScore = 100000000; //This is the generic base mission stuff, so it should apply to ANY AND EVERY score possible
        leastScore = -100000000; //Also, the most & least stuff isn't really implemented for this base mission (only for the Pos100 Neg100 etc specific Battle Missions)

        
        score = sc;
        //score = 100; //TESTING!!!!! Using a fake/test score. To use the REAL SCORE rem out these lines.      
        //Console.WriteLine(" TWCTobrukCampaignMissionObjectives real score is {0} - for testing setting it to {1}", sc, score);


        focus_airport_misfile_name = "";

        /*****************************************************************************************************
         * 
         * ADD BATTLE .cs SCRIPT FILES HERE
         * Add the $include file for the .cs above, then add the class and register it below
         * 
         * ***************************************************************************************************/

        TWCTCMO battle_pos100 = new BattlePos100(g, m, sc);
        Console.WriteLine("Pos100 inited (return), {0} {1} {2} {3}", battle_pos100.score, battle_pos100.leastScore, battle_pos100.mostScore, battle_pos100.focus_airport_misfile_name);
        register_subclass(battle_pos100);

        //TWCTobrukCampaignMissionObjectives mission_neg10 = new MissionNeg10();
        //TWCTobrukCampaignMissionObjectives mission_0 = new Mission0(g, m, mo, sc);
        //TWCTobrukCampaignMissionObjectives mission_pos10 = new MissionPos10(g, m, mo, sc);

        //RadarPositionTriggersSetup(addNewOnly: false); //We'll let mmo call this as it might need to be called specifically @ certain times.
        //MissionObjectiveTriggersSetup(addNewOnly: false); //We'll let mmo call this as it might need to be called specifically @ certain times.

        set_current_subclass();                
        
    }

    /*
    public override void UpdateMissionObjectives(M.MissionObjectives mo)
    {
        mmo = mo;
        try
        {
            if (current_subclass != null) current_subclass.UpdateMissionObjectives(mmo);
        }
        catch (Exception ex) { Console.WriteLine("UpdateMissionObjectives ERROR: " + ex.ToString()); }
    }
    */



    //public List<SubMissionClassInfo> MissionObjectives_subclasses = new List<int, SubMissionClassInfo>()  { };



    //So every subclass must register itself during initilization via
    //  base.register_subclass (this);  The numbers are the LEAST and GREATEST campaign score for which that subclass should be activated.
    //Note that 

    public void register_subclass(TWCTCMO twcmo)
    {
        //if (TWCMissionObjectives_subclasses == null) TWCMissionObjectives_subclasses = new List<TWCTobrukCampaignMissionObjectives>() { };
        Console.WriteLine("TWCTobrukCampaignMissionObjectives adding new subclass... {0}, {1}, {2}, {3}", TWCMissionObjectives_subclasses.Count, twcmo.leastScore, twcmo.mostScore, twcmo.focus_airport_misfile_name);
        TWCMissionObjectives_subclasses.Add(twcmo);
        Console.WriteLine("TWCTobrukCampaignMissionObjectives adding new subclass (after) ... {0}", this.TWCMissionObjectives_subclasses.Count);
    }

    //Set current_subclass to be the double value of the MissionObjectives_subclasses member with key equal to OR NEAREST TO the current score (Campaign Score)
    //If no match at all/no current_score then returns NULL
    //Step through all the MissionObjectives_subclasses, which are sorted in order, and choose the one that is equal (hopefully!)
    //or if not that, the one nearest.  I

    /*
    private void set_current_subclass()
    {
        int? ret = null;
        foreach (KeyValuePair<double, TWCTobrukCampaignMissionObjectives> p in MissionObjectives_subclasses)
        {
            if (p.Key == score) { ret = p.Key; break; }
            if (p.Key < score) { ret = p.Key; break; }
            if (p.Key > score)
            {
                if (!ret.HasValue) { ret = p.Key; break; }
                double ave = (double)ret.Value / (double)p.Key;
                if (score >= ave) { ret = p.Key; break; }
                else break; //we use current value of ret (ie, the lower of the two matching keys is used
            }
        }
        current_subclass = ret;
    }*/

    private void set_current_subclass()
    {

        //int? ret = null;

        Console.WriteLine("TWC CAMPAIGN MISSIONS Checking TWCMissionObjectives_subclasses, {0} to check...", TWCMissionObjectives_subclasses.Count);

        foreach (TWCTCMO twc in TWCMissionObjectives_subclasses)
        {
            Console.WriteLine("set_current_subclass checking for match: {0} {1} {2}",twc.leastScore, twc.mostScore, twc.focus_airport_misfile_name);//NOTE THIS DOESN"T WORK DUE TO FORMATTING/fatal error
            /*
            if (!(twc.leastScore).HasValue && !(twc.mostScore).HasValue) { current_subclass = twc; Console.WriteLine("set_current_subclass success 1"); return; }
            if (!twc.leastScore.HasValue && twc.mostScore.HasValue && twc.mostScore.Value > score) { current_subclass = twc; Console.WriteLine("set_current_subclass success 2"); return; }
            if (twc.leastScore.HasValue && twc.leastScore.Value <= score && !twc.mostScore.HasValue) { current_subclass = twc; Console.WriteLine("set_current_subclass success 3"); return; }
            if (twc.leastScore.HasValue && twc.leastScore.Value <= score && twc.mostScore.HasValue && twc.mostScore.Value > score) { current_subclass = twc; Console.WriteLine("set_current_subclass success 4"); return; } */

            //if (!(twc.leastScore).HasValue && !(twc.mostScore).HasValue) { current_subclass = twc; Console.WriteLine("set_current_subclass success 1"); return; }
            //if (twc.mostScore.Value > score) { current_subclass = twc; Console.WriteLine("set_current_subclass success 2"); return; }
            //if (twc.leastScore.Value <= score && !twc.mostScore.HasValue) { current_subclass = twc; Console.WriteLine("set_current_subclass success 3"); return; }
            if (twc.leastScore <= score && twc.mostScore > score) { current_subclass = twc; Console.WriteLine("set_current_subclass success 4"); return; }
        }
        Console.WriteLine("TWC CAMPAIGN MISSIONS MAJOR ERROR!!!!! NO CAMPAIGN SUBOBJECTIVES LOADED!!!!!!!  CHECK TWCMissionObjectives_subclasses for PROPER SETUP. Exiting...");                

    }

    private void exitGame()
    {
        if (gp != null && gp is GameDef)
        {
            (gp as GameDef).gameInterface.CmdExec("battle stop"); //TESTING
            //(gp as GameDef).gameInterface.CmdExec("exit"); //maybe better for real server???
        }

        return;

        msn.Timeout(10, () =>  //failsafe kill switch
        {
            System.Environment.Exit(1);
            Process.GetCurrentProcess().Kill();
        });
    }

    
    public override void OverrideCampaignTurnMapRequirements()
    {

        //What percent of primary targets is actually required ot turn the map
        //If you make it 100% you have to get them all, but if some are difficult or impossible then that army will be stuck
        msn.MO_PercentPrimaryTargetsRequired = new Dictionary<ArmiesE, double>() {
            {ArmiesE.Red, 80 },
            {ArmiesE.Blue, 80 }
        };

        //TODO: Use similar scheme for total points, objectives completed list, objectives completed
        //Points required, assuming they are doing it entirely with Primary Targets; ie, secondary or other targets do not count towards this total
        //at all
        msn.MO_PointsRequiredToTurnMap = new Dictionary<ArmiesE, double>() {
            {ArmiesE.Red, 121 },
            {ArmiesE.Blue, 120 }
        };


        msn.MO_BRBumrushInfo = new Dictionary<ArmiesE, M.MO_BRBumrushInfoType>() {
            { ArmiesE.Red, new M.MO_BRBumrushInfoType() {
                PointsRequiredToBeginBumrush= 61, //guarantees all our important MOs (5 pts X 6) plus the Focus Airport (30 pts) plus at least one more to get the extra 6 point.
                BumrushStatus= 0,
                BumrushObjective = null,
              }
            },
            { ArmiesE.Blue, new M.MO_BRBumrushInfoType() {
                PointsRequiredToBeginBumrush= 60,
                BumrushStatus= 0,
                BumrushObjective = null,
              }
            },
        };

        if (current_subclass != null)
        {
            try
            {
                current_subclass.OverrideCampaignTurnMapRequirements();
            } catch (Exception ex) { Console.WriteLine("TWCTobrukCampaignMissionObjectives ERROR: " + ex.ToString()); }
        }
    }


    public override void RadarPositionTriggersSetup(bool addNewOnly = false)
    {

        bool add = addNewOnly;
        //using msn.mission_objectives;

        //ID is the ID used in the [Trigger] portion of the .mis file. The central portion of the line can be copy/pasted from the  .mis file (then lightly edited)
        //Console.Write("#1a");

        //MissionObjective(Name,          Flak ID, OwnerArmy,points,ID, Days to repair, Trigger Type,Trigger percentage, location x, location y, trigger radius, radar effective radius, isPrimaryTarget, PrimaryTargetWeight (0-200), comment) {
        //weights change 0-200, many weights below adjusted, 2020-01
        //Prior to 2/15/2020 I had moved most Red Radar trigger % to about 40%.  Then 2/24 moved up to 70% to make it harder.  Now moved to 80% to make it even harder.  2020/02/27.
        //Note that these are in the .mis file.

        /*
        mmo.addRadar("Westgate Radar", "WesR", 1, 5, 2, "BTarget14R", "TGroundDestroyed", 39, 244791, 262681, 150, 25000, false, 30, "", add);
        mmo.addRadar("Sandwich Radar", "SanR", 1, 5, 2, "BTarget15R", "TGroundDestroyed", 50, 248579, 253159, 200, 25000, false, 30, "", add);
        mmo.addRadar("Deal Radar", "DeaR", 1, 5, 2, "BTarget16R", "TGroundDestroyed", 75, 249454, 247913, 200, 25000, false, 30, "", add);
        mmo.addRadar("Dover Radar", "DovR", 1, 5, 2, "BTarget17R", "TGroundDestroyed", 75, 246777, 235751, 200, 25000, false, 30, "", add);
        mmo.addRadar("Brookland Radar", "BroR", 1, 5, 2, "BTarget18R", "TGroundDestroyed", 75, 212973, 220079, 200, 25000, false, 30, "", add);
        mmo.addRadar("Dungeness Radar", "DunR", 1, 5, 2, "BTarget19R", "TGroundDestroyed", 50, 221278, 214167, 200, 25000, false, 30, "", add);
        mmo.addRadar("Eastbourne Radar", "EasR", 1, 5, 2, "BTarget20R", "TGroundDestroyed", 75, 178778, 197288, 200, 25000, false, 10, "", add);
        mmo.addRadar("Littlehampton Radar", "LitR", 1, 5, 2, "BTarget21R", "TGroundDestroyed", 76, 123384, 196295, 200, 35000, false, 10, "", add);
        mmo.addRadar("Ventnor Radar", "VenR", 1, 5, 2, "BTarget22R", "TGroundDestroyed", 75, 70423, 171706, 200, 35000, false, 10, "", add);
        mmo.addRadar("Radar Communications HQ", "HQR", 1, 11, 3, "BTarget28", "TGroundDestroyed", 61, 180207, 288435, 200, 350000, false, 5, "", add);
        mmo.addRadar("Radar Poole", "PooR", 1, 6, 2, "BTarget23R", "TGroundDestroyed", 75, 15645, 170552, 200, 35000, false, 5, "", add);

        //public void mmo.addPointArea(M.MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double largearearadius = 100, double smallercentertargettrigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)
        //mmo.addPointArea(M.MO_ObjectiveType.Building, "Dover Naval HQ", "Dove", "", 1, 3, "BTargDoverNavalOffice", 245567, 233499, 50, 50, 800, 4, 120, 48, true, true, 4, 7, "", add);
        //NOTE: renaming the radar targets as "RPA in stead of just "R" so they no longer match the .mis file trigger names (which WILL still trigger if they are still in the file).
        mmo.addRadarPointArea("Oye Plage Freya Radar", "OypR", 2, 4, 2, "RTarget28RPA", "", 1000, 4, 294183, 219444, 85, 20000, false, 35, "", add);
        mmo.addRadarPointArea("Coquelles Freya Radar", "CoqR", 2, 4, 2, "RTarget29RPA", "", 1000, 4, 276566, 214150, 85, 20000, false, 35, "", add);
        mmo.addRadarPointArea("Dunkirk Radar #2", "DuRN", 2, 4, 2, "RTarget30RPA", "", 1000, 4, 341887, 232695, 85, 20000, false, 35, "", add);



        */

        //At the end of this this we call the corresponding routine in the current_subclass, so that it can add more or update
        if (current_subclass != null)
        {
            try
            {
                current_subclass.RadarPositionTriggersSetup(addNewOnly);
            }
            catch (Exception ex) { Console.WriteLine("TWCTobrukCampaignMissionObjectives ERROR: " + ex.ToString()); }
        }

    }

    public override void MissionObjectiveTriggersSetup(bool addNewOnly = false)
    {

        bool add = addNewOnly;


        /********************************************************************************************************************
        * POINT AREA TYPE OBJECTIVES
        * 
        * These are the most flexible and easy-to use type of objectives.  You simply define the center point of the target area and the
        * circumference (size).  You can put objects/targets in that area in the main .mis file or put them into a special .mis file
        * that will be loaded when the objective is initialized.  This is an advantage, because you can set these objectives to be active/visible
        * only sometimes.  During the times they are inactive the special submission .mis file won't loaded, but a few smoking ruins
        * will be placed at that location.
        * 
        * ********************************************************************************************************************/



        //public void mmo.addPointArea(M.MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double rad = 100, double trigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)
        // n is the DESCRIPTIVE NAME for the target--what the player sees
        //rad is the radius-extent to the object itself.  Say an airfield will have a certain radius, or a general industrial area or military base.
        //trigrad is like the center of the bullseye--the radius from the center within which the bombs, killed objectives etc will count THE MOST towards disabling the objective.  Anything outside this radius (but still inside the main radius of the object) won't count as much--but still counts.  Anything inside the radius counts more.  Anything outside both radii counts zero.
        //   - for example you might have an airbase with radius 2500 meters but you want the bombs to hit more in the center of that to count, so you set rad=2500, trigrad=1000.
        // tn is the INTERNAL KEY for the object.  It can be anything you want to identify it to yourself and the computer, but it must be unique (ie,different from ALL OTHER objectives listed here).
        //orttkg = kg of ordnance that must be dumped in this area to knock it out
        //ortt = number of objects (statics, buildings, AiActors like vehicles, trains, whatever) that must be killed within the area to knock it out. Note however that there is a scoring system and for example ships count 8-20 points, artillery/tank 4, planes on the ground 2, bridge 10, trucks/armoured vehicles 2, etc.   See MO_HandlePointAreaObjectives.
        //     - if you set this higher than 0 you MUST have some objects within the radius of the area, preferably the trigger radius, or taking out the objective will be impossible
        //     - AiActors such as vehicles, ships, trains, planes, artillery, etc also count as 'objects' that count towards this goal.  BUT they must be strictly within the radius given.  If they wander outside the radius, they are no longer part of this objective
        //     - Buildings can be counted to a limited extent in the future, but for now they are not (until TF resolves the OnBuildingKilled thing, which currently works for AI but not live players
        //     - You can add objects/actors, etc to the -main.cs file OR to a XXXXMission-initsubmission.mis file, which is loaded by this .cs file right at mission startup.  It is a lot cleaner to keep things in separate submission.mis files.
        //     - Note that you cannot add BUILDINGS in an -initsubmission file.  You can add them in FMB but they don't show up in-game.  However you could add a building in the -main.mis file and then other objects in an -initsubmission.mis file.
        //You can specify orttkg OR ortt OR both - if both, the player must satisfy both conditions to knock out
        //initSub is a submission that will be loaded when the mission starts, if this objective is enabled
        //ptp = primary target weight, ie, 0-200 increase or decrease chance of selection as a primary target.        
        //ttr_hours = time to repair, in hours, if taken out 100%.
        //af = add auto-flak batteries always
        //afip = add auto-flak batteries only if a primary target
        //fb = number of flak batteries to add (if a primary target)
        //fnib = number of guns in each battery (if a primary target)
        //Note that the number of batteries & guns per battery is only used if the objective is a current primary target. Otherwise just a much smaller amount of flak is put in place.
        //That's because too many flak installations seems to bring the server to its knees.
        //public void mmo.addPointArea(M.MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double largearearadius = 100, double smallercentertargettrigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)

        //POINTAREA Samples:
        // mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Derna Fuel", "Dern", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-DernaFuel-objective.mis", 2, 5, "RTargDernaFuel", 32277, 264622, 200, 150, 1800, 4, 200, 48, false, true, 3, 7, "", add);
        // mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Tobruk Docks Fuel/Ammo Dump", "Tobr", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-TobrukDockFuel2-objective.mis", 2, 5, "RTargTobrukFuel", 160306, 186548, 200, 150, 1800, 2, 200, 48, false, true, 3, 7, "", add);

        /********************************************************************************************************************
         * TRIGGER TYPE OBJECTIVES
         * 
         * These work by using the trigger function in FMB/.mis files.  You can set various parameters there. When the trigger is triggered, the routine is called & the objective destroyed.
         * 
         * Triggers are a particular advantage when the objective is mobile, like a convoy or ship.  Wherever the target it, if it is destroyed the trigger will be called.
         * 
         * Tracking moving triggers is a particular problem.  However if you create a unique name for the "chief" in the .mis file then you can enter that name in the addTrigger call (see 
         * example below "1006_Chief") and then recon of that area will find the exact position of that unit at that moment.
         * 
         * ********************************************************************************************************************/

        //Format: mmo.addTrigger(M.MO_ObjectiveType.Building (Aircraft, airport, etc), "Name,                      OwnerArmy,Points,ID,TriggerType,PercRequired,XLoc,YLoc,Radius,IsPrimaryTarget,IsPrimaryTargetWeight,TimeToRepairIfDestroyed_hours,Comment "");
        //PercRequired doesn't actually do anything because the perc required is set in the TRIGGER in the .mis file.  However if you accurately record here the same
        //percent to kill value in the .mis file we can do interesting/helpful things with it here.

        // mmo.addTrigger(M.MO_ObjectiveType.Building, "Coal mines in Normandy", "Shor", 1, 3, "BTargNormandyMiningCenter", "TGroundDestroyed", 50, 67510, 26083, 50, false, 100, 24, "", add);			
        //    mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Tobruk-Gasr Resupply Convoy", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-RTobrukGasrResupplyConvoy-objective.mis", "1006_Chief", 2, 5, "RTobrukGasrResupplyConvoy", "TGroupDestroyed", 100, 197907, 95422, 100, false, 200, 24, "", add);  //g          


        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Tobruk Tanker", "Tobr", "", "", 2, 2, "RTobrukTanker", "TGroupDestroyed", 100, 172859, 214570, 100, false, 0, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Tobruk Cruiser", "Tobr", "", "", 2, 2, "RTobrukCruiser", "TGroupDestroyed", 100, 172859, 214570, 100, false, 0, 24, "", add);  //g            

        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Sidi Barrani Tanker", "", "", "", 1, 2, "BSidiBarraniTanker", "TGroupDestroyed", 100, 295708, 193686, 100, false, 0, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Sidi Barrani Corvette", "", "", "", 1, 2, "BSidiBarraniCorvette", "TGroupDestroyed", 100, 357990, 237331, 100, false, 0, 24, "", add);  //g            


        //public void mmo.addPointArea(M.MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double rad = 100, double trigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)
        // n is the DESCRIPTIVE NAME for the target--what the player sees
        //rad is the radius-extent to the object itself.  Say an airfield will have a certain radius, or a general industrial area or military base.
        //trigrad is like the center of the bullseye--the radius from the center within which the bombs, killed objectives etc will count THE MOST towards disabling the objective.  Anything outside this radius (but still inside the main radius of the object) won't count as much--but still counts.  Anything inside the radius counts more.  Anything outside both radii counts zero.
        //   - for example you might have an airbase with radius 2500 meters but you want the bombs to hit more in the center of that to count, so you set rad=2500, trigrad=1000.
        // tn is the INTERNAL KEY for the object.  It can be anything you want to identify it to yourself and the computer, but it must be unique (ie,different from ALL OTHER objectives listed here).
        //orttkg = kg of ordnance that must be dumped in this area to knock it out
        //ortt = number of objects (statics, buildings, AiActors like vehicles, trains, whatever) that must be killed within the area to knock it out. Note however that there is a scoring system and for example ships count 8-20 points, artillery/tank 4, planes on the ground 2, bridge 10, trucks/armoured vehicles 2, etc.   See MO_HandlePointAreaObjectives.
        //     - if you set this higher than 0 you MUST have some objects within the radius of the area, preferably the trigger radius, or taking out the objective will be impossible
        //     - AiActors such as vehicles, ships, trains, planes, artillery, etc also count as 'objects' that count towards this goal.  BUT they must be strictly within the radius given.  If they wander outside the radius, they are no longer part of this objective
        //     - Buildings can be counted to a limited extent in the future, but for now they are not (until TF resolves the OnBuildingKilled thing, which currently works for AI but not live players
        //     - You can add objects/actors, etc to the -main.cs file OR to a XXXXMission-initsubmission.mis file, which is loaded by this .cs file right at mission startup.  It is a lot cleaner to keep things in separate submission.mis files.
        //     - Note that you cannot add BUILDINGS in an -initsubmission file.  You can add them in FMB but they don't show up in-game.  However you could add a building in the -main.mis file and then other objects in an -initsubmission.mis file.
        //You can specify orttkg OR ortt OR both - if both, the player must satisfy both conditions to knock out
        //initSub is a submission that will be loaded when the mission starts, if this objective is enabled
        //ptp = primary target weight, ie, 0-200 increase or decrease chance of selection as a primary target.        
        //ttr_hours = time to repair, in hours, if taken out 100%.
        //af = add auto-flak batteries always
        //afip = add auto-flak batteries only if a primary target
        //fb = number of flak batteries to add (if a primary target)
        //fnib = number of guns in each battery (if a primary target)
        //Note that the number of batteries & guns per battery is only used if the objective is a current primary target. Otherwise just a much smaller amount of flak is put in place.
        //That's because too many flak installations seems to bring the server to its knees.
        //public void mmo.addPointArea(M.MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double largearearadius = 100, double smallercentertargettrigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)

        /********************************************************************************************************************
         * MOBILE TYPE OBJECTIVES
         * 
         * This are auto-generated by main-.cs according to various recipes.  They can appear at various positions and move every X hours or days, depending on the parameter passed below.
         * 
         * ********************************************************************************************************************/


        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Army Camp", "", 2, 1, "RMobileArmyCamp", 270276, 169671, 200, 150, 7000, 15, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.ArmyEncampment, 15, 10000, 10000, 278420, 271000, 2, 7, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Army Camp", "", 1, 2, "BMobileArmyCamp", 279965, 104219, 200, 150, 7000, 15, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.ArmyEncampment, 15, 197500, 10000, 370000, 190000, 2, 7, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Secret Base", "", 1, 2, "BSecretAirbase", 289965, 104219, 700, 600, 10000, 10, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.SecretAirbaseGB, 60, 197500, 10000, 370000, 190000, 10, 35, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Secret Base", "", 2, 1, "RSecretAirbase", 180276, 169671, 800, 600, 10000, 10, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.SecretAirbaseDE, 60, 10000, 10000, 278420, 271000, 10, 35, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Intelligence Unit", "", 1, 1, "BMobileIntelligence", 249965, 114219, 550, 450, 10000, 10, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.SecretAircraftResearchGB, 30, 197500, 10000, 370000, 190000, 10, 45, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Intelligence Unit", "", 2, 5, "RMobileIntelligence", 180276, 179671, 550, 450, 10000, 10, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.SecretAircraftResearchGB, 30, 10000, 10000, 278420, 271000, 10, 45, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.Radar, "Mobile Radar 1", "", 2, 1, "RMobileRadar1", 210276, 169671, 200, 150, 7000, 12, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.MobileRadar1, 15, 10000, 10000, 278420, 271000, 5, 35, M.MO_ProducerOrStorageType.None, "", add, radar_effective_radius_m: 20000);
        mmo.addMobile(M.MO_ObjectiveType.Radar, "Mobile Radar 1", "", 1, 1, "BMobileRadar1", 279965, 104219, 200, 150, 7000, 12, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.MobileRadar1, 30, 197500, 10000, 370000, 190000, 5, 35, M.MO_ProducerOrStorageType.None, "", add, radar_effective_radius_m: 30000);
        mmo.addMobile(M.MO_ObjectiveType.Radar, "Mobile Radar 2", "", 2, 1, "RMobileRadar2", 210276, 179671, 300, 250, 7000, 12, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.MobileRadar1, 45, 10000, 10000, 278420, 271000, 10, 45, M.MO_ProducerOrStorageType.None, "", add, radar_effective_radius_m: 20000);
        mmo.addMobile(M.MO_ObjectiveType.Radar, "Mobile Radar 2", "", 1, 1, "BMobileRadar2", 279965, 184219, 300, 250, 7000, 12, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.MobileRadar1, 45, 197500, 10000, 370000, 190000, 10, 45, M.MO_ProducerOrStorageType.None, "", add, radar_effective_radius_m: 30000);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Armour Unit 1", "", 2, 1, "RMobileArmour1", 231978, 220669, 250, 200, 7000, 15, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.SmallArmourGroup, 15, 10000, 10000, 278420, 271000, 2, 5, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Mobile Armour Unit 1", "", 1, 1, "BMobileArmour1", 240196, 143824, 250, 200, 7000, 15, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.SmallArmourGroup, 15, 197500, 10000, 370000, 190000, 2, 5, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Large Mobile Armour Unit", "", 2, 1, "RLargeMobileArmour", 145508, 231881, 250, 200, 9000, 17, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.LargeArmourGroup, 15, 10000, 10000, 278420, 271000, 2, 1, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Large Mobile Armour Unit", "", 1, 1, "BLargeMobileArmour", 221410, 136331, 250, 200, 9000, 17, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.LargeArmourGroup, 15, 197500, 10000, 370000, 190000, 2, 1, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Intelligence Listening Post", "", 2, 1, "RIntelligenceListening", 262939, 143597, 250, 200, 4000, 15, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.CamoGroup, 45, 10000, 10000, 278420, 271000, 5, 20, M.MO_ProducerOrStorageType.None, "", add);
        mmo.addMobile(M.MO_ObjectiveType.MilitaryArea, "Intelligence Listening Post", "", 1, 1, "BIntelligenceListening", 212979, 182491, 250, 200, 4000, 15, 1, 36, true, true, 1, 3, M.MO_MobileObjectiveType.CamoGroup, 45, 197500, 10000, 370000, 190000, 5, 20, M.MO_ProducerOrStorageType.None, "", add);



        if (current_subclass != null)
        {
            try
            {
                current_subclass.MissionObjectiveTriggersSetup(addNewOnly);
            }
            catch (Exception ex) { Console.WriteLine("TWCTobrukCampaignMissionObjectives ERROR: " + ex.ToString()); }
        }
    }



    public override void FlakDictionariesSetup()
    {
        //Names of the flak areas and link to file name
        //Name is used in list of objectives aboe & must match exactly.  You can change the name below but then the name in the mmo.addTrigger etc above must also be changed to match
        //file name must match exactly with the filename
        try
        {
            if (mmo.FlakMissions == null) return;
            mmo.FlakMissions = new Dictionary<string, string>()
            {
                    { "az", "/Flak areas/AbiarZaidflak.mis" },
                    { "ar", "/Flak areas/Akramhaflak.mis" },
                    { "aah", "/Flak areas/AlmiaAhalkhafiaflak.mis" },
                    { "aaq", "/Flak areas/AlsmarAlmafqudflak.mis" },
                    { "ahb", "/Flak areas/Althaebanflak.mis" },
                    { "AA", "/Flak areas/AlukhtAlddayieaflak.mis" },
                    { "AS1AS2", "/Flak areas/Amseat1_2flak.mis" },
                    { "AwD", "/Flak areas/Awdyatflak.mis" },
                    { "Bard", "/Flak areas/Baradiaflak.mis" },
                    { "BeL", "/Flak areas/BeltatAlQazahflak.mis" },
                    { "BrH", "/Flak areas/BirAlHakimflak.mis" },
                    { "BBas", "/Flak areas/BirBasurflak.mis" },
                    { "BBaH2", "/Flak areas/BirelBaheira2_1flak.mis" },
                    { "BieGasr", "/Flak areas/BirelGasrflak.mis" },
                    { "BirMalN", "/Flak areas/BirElMallaNorthflak.mis" },
                    { "BirMalS", "/Flak areas/BirElMallaSouthflak.mis" },
                    { "BombaN", "/Flak areas/BombaNorthflak.mis" },
                    { "BuQBuQ", "/Flak areas/BuqBuqflak.mis" },
                    { "Burj", "/Flak areas/BurjAleaqarabflak.mis" },
                    { "DernaCity", "/Flak areas/DernaCityflak.mis" },
                    { "DerEW", "/Flak areas/DernaEast&Westflak.mis" },
                    { "DernaSi", "/Flak areas/DernaSiretchreibaflak.mis" },
                    { "EAdem1_2", "/Flak areas/ElAdemNO1_NO2flak.mis" },
                    { "Gan2_3", "/Flak areas/Gambt2_3flak.mis" },
                    { "Gam1_5", "/Flak areas/Gambut1_5flak.mis" },
                    { "GasrAbid", "/Flak areas/GasrAbid_FortNeghil.mis" },
                    { "GasrAbidS", "/Flak areas/GasrAbidSouthflak.mis" },
                    { "GasrArid", "/Flak areas/GasrAridflak.mis" },// watch tricky spelling
                    { "Gaz1", "/Flak areas/Gazala1flak.mis" },
                    { "Gaz2_3", "/Flak areas/Gazala2_3flak.mis" },
                    { "Habat", "/Flak areas/Habataflak.mis" },
                    { "Helfire", "/Flak areas/Halfayaflak.mis" },
                    { "HaQfat", "/Flak areas/Haqfatsflak.mis" },
                    { "Mart2", "/Flak areas/Martuba2flak.mis" },
                    { "Mart3", "/Flak areas/Martuba3flak.mis" },
                    { "Mart4", "/Flak areas/Martuba4_1flak.mis" },
                    { "Mart5", "/Flak areas/Martuba5flak.mis" },
                    { "Menlo", "/Flak areas/MeneloBayflak.mis" },
                    { "Swani", "/Flak areas/Sawaniflak.mis" },
                    { "Sceg", "/Flak areas/SceggaAllflak.mis" },
                    { "SidiAZ", "/Flak areas/SidiAzeizflak.mis" },
                    { "SidiBar", "/Flak areas/SidiBarraniairfieldflak.mis" },
                    { "SidiCoast", "/Flak areas/SidiBarraniCoastalflak.mis" },
                    { "SidiRez", "/Flak areas/SidiRezeghflak.mis" },
                    { "Siwi", "/Flak areas/Siwiflak.mis" },
                    { "TarQ", "/Flak areas/Tariqflak.mis" },
                    { "ToB5", "/Flak areas/Tobruk_5flak.mis" },
                    { "ToBC_1", "/Flak areas/TobrukCity&1flak.mis" },
                    { "ToB2_3", "/Flak areas/Torbuk2_3flak.mis" },
                    { "Trimy", "/Flak areas/Trimmi2_1flak.mis" },
                    { "None", "/Flak areas/Noneflak.mis" },


            };
        }
        catch (Exception ex) { Console.WriteLine("TTMO Flak: " + ex.ToString()); }

        try { 
        if (mmo.Airfield_to_FlakMissions == null) return;
        mmo.Airfield_to_FlakMissions = new Dictionary<string, string>()
            {
                    { "az", "/Flak areas/AbiarZaidflak.mis" },
                    { "ar", "/Flak areas/Akramhaflak.mis" },
                    { "aah", "/Flak areas/AlmiaAhalkhafiaflak.mis" },
                    { "aaq", "/Flak areas/AlsmarAlmafqudflak.mis" },
                    { "ahb", "/Flak areas/Althaebanflak.mis" },
                    { "AA", "/Flak areas/AlukhtAlddayieaflak.mis" },
                    { "AS1AS2", "/Flak areas/Amseat1_2flak.mis" },
                    { "AwD", "/Flak areas/Awdyatflak.mis" },
                    { "Bard", "/Flak areas/Baradiaflak.mis" },
                    { "BeL", "/Flak areas/BeltatAlQazahflak.mis" },
                    { "BrH", "/Flak areas/BirAlHakimflak.mis" },
                    { "BBas", "/Flak areas/BirBasurflak.mis" },
                    { "BBaH2", "/Flak areas/BirelBaheira2_1flak.mis" },
                    { "BieGasr", "/Flak areas/BirelGasrflak.mis" },
                    { "BirMalN", "/Flak areas/BirElMallaNorthflak.mis" },
                    { "BirMalS", "/Flak areas/BirElMallaSouthflak.mis" },
                    { "BombaN", "/Flak areas/BombaNorthflak.mis" },
                    { "BuQBuQ", "/Flak areas/BuqBuqflak.mis" },
                    { "Burj", "/Flak areas/BurjAleaqarabflak.mis" },
                    { "DernaCity", "/Flak areas/DernaCityflak.mis" },
                    { "DerEW", "/Flak areas/DernaEast&Westflak.mis" },
                    { "DernaSi", "/Flak areas/DernaSiretchreibaflak.mis" },
                    { "EAdem1_2", "/Flak areas/ElAdemNO1_NO2flak.mis" },
                    { "Gan2_3", "/Flak areas/Gambt2_3flak.mis" },
                    { "Gam1_5", "/Flak areas/Gambut1_5flak.mis" },
                    { "GasrAbid", "/Flak areas/GasrAbid_FortNeghil.mis" },
                    { "GasrAbidS", "/Flak areas/GasrAbidSouthflak.mis" },
                    { "GasrArid", "/Flak areas/GasrAridflak.mis" },// watch tricky spelling
                    { "Gaz1", "/Flak areas/Gazala1flak.mis" },
                    { "Gaz2_3", "/Flak areas/Gazala2_3flak.mis" },
                    { "Habat", "/Flak areas/Habataflak.mis" },
                    { "Helfire", "/Flak areas/Halfayaflak.mis" },
                    { "HaQfat", "/Flak areas/Haqfatsflak.mis" },
                    { "Mart2", "/Flak areas/Martuba2flak.mis" },
                    { "Mart3", "/Flak areas/Martuba3flak.mis" },
                    { "Mart4", "/Flak areas/Martuba4_1flak.mis" },
                    { "Mart5", "/Flak areas/Martuba5flak.mis" },
                    { "Menlo", "/Flak areas/MeneloBayflak.mis" },
                    { "Swani", "/Flak areas/Sawaniflak.mis" },
                    { "Sceg", "/Flak areas/SceggaAllflak.mis" },
                    { "SidiAZ", "/Flak areas/SidiAzeizflak.mis" },
                    { "SidiBar", "/Flak areas/SidiBarraniairfieldflak.mis" },
                    { "SidiCoast", "/Flak areas/SidiBarraniCoastalflak.mis" },
                    { "SidiRez", "/Flak areas/SidiRezeghflak.mis" },
                    { "Siwi", "/Flak areas/Siwiflak.mis" },
                    { "TarQ", "/Flak areas/Tariqflak.mis" },
                    { "ToB5", "/Flak areas/Tobruk_5flak.mis" },
                    { "ToBC_1", "/Flak areas/TobrukCity&1flak.mis" },
                    { "ToB2_3", "/Flak areas/Torbuk2_3flak.mis" },
                    { "Trimy", "/Flak areas/Trimmi2_1flak.mis" },
                    { "None", "/Flak areas/Noneflak.mis" },



            };
        }
        catch (Exception ex) { Console.WriteLine("TTMO Flak2: " + ex.ToString()); }

        /*************************************************************
         * 
         * DON'T call the subclass FlakDictionariesSetup() here, it is called in each class when it is initialized
         * The others methods ARE NOT call when initialized, so we need ot call any subclass routine at that point, when it is called (later on in Mission initalization)
         * 
         * 
         * ***********************************************************/
    }//FlakDictionariesSetup()

    public override void BumRushCampaignValuesSetup()
    {
        //What percent of primary targets is actually required ot turn the map
        //If you make it 100% you have to kill ALL primary objectives in order to turn the map.  But if some are difficult or impossible then that army will be stuck.  So this may be TOO difficult & frustrating for players
        //If you make it more like 80%, then if there is one certain target that is too difficult or bugged, players can rack up points killing other, non-primary
        //objectives and STILL turn the map.  So something like 80% is more realistic

        msn.MO_PercentPrimaryTargetsRequired = new Dictionary<ArmiesE, double>() {
            {ArmiesE.Red, 80 },
            {ArmiesE.Blue, 80 }
        };

        //For the Tobruk/Bumrush Campaign, points go this way:
        // 5points for each of 6 required primary objectives = 30 points
        // 30 points for the required airport primary object = 30 points.  30 + 30 = 60 which is enough to trigger the Bumrush phase.
        //Winning the Bumrush Phase wins another 60 points, for 120 total - to TURN the map.
        //So this totals 120 points.  BUt . . . oOn turning the map the winning side is actually award 1000 points.  
        //In the system used here (ie, variable "score") 1 point == 100 player points.  So awarding 1000 player points means their 'score' was increased by 10 (10X100 = 1000)
        //So in setting points required to advance mission, keep in mind advance or retreating in 10 point increments -50, -40, -30, -20, -10, 0, 10, 20, 30, 40 50 etc
        msn.MO_PointsRequiredToTurnMap = new Dictionary<ArmiesE, double>() {
            {ArmiesE.Red, 120 },
            {ArmiesE.Blue, 120 }
        };

        msn.MO_BRBumrushInfo[ArmiesE.Red].PointsRequiredToBeginBumrush = 60;
        msn.MO_BRBumrushInfo[ArmiesE.Blue].PointsRequiredToBeginBumrush = 60;

        if (current_subclass != null)
        {
            try
            {
                current_subclass.BumRushCampaignValuesSetup();
            }
            catch (Exception ex) { Console.WriteLine("TWCTobrukCampaignMissionObjectives ERROR: " + ex.ToString()); }
        }

    }

    /******************************************************************************************************
     * 
     * INITIALIZE BUMRUSH MISSION WITH FOCUS AIRPORT .mis FILE AND BUMRUSH FILES
     * 
     EXACT airfield names to use in ALL OF:
        * blueTarget, redTarget variable in this file
        * the focus airport .mis file as the airfield name there
        * Tobruk_Campaign-initairports-Gasr_el_abid-Skegga-No3

     are: "Tariq Al Ghubay Airfield", "Scegga No3 Airfield", "Gasr el Abid South Airfield", "Sidi Azeiz Airfield", "Sidi Rezegh LG153 Airfield" 

    //IMPORTANT: If you have different spawn points for the same airfield (say, in different .mis files) this will cause problems here!  Make sure spawnpoints for these airports are in one .mis file only!

    */

    //Nothing needed at this level; we just call the corresponding routine in current_subclass.MissionObjectiveAirfieldFocusBumrushSetup() which does all the mission-specific work
    //This must be called 
    public override void MissionObjectiveAirfieldFocusBumrushSetup()
    {
        try
        {
            current_subclass.MissionObjectiveAirfieldFocusBumrushSetup();
        }
        catch (Exception ex)
        {
            Console.WriteLine("*********************************************");
            Console.WriteLine("*********************************************MissionObjectiveAirfieldFocusBumrushSetup() ERROR: " + ex.ToString());
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }

    }

    //Nothing needed at this level; we just call the corresponding routine in current_subclass.ReadInitialFocusAirportSubmission which does all the mission-specific work
    public override void ReadInitialFocusAirportSubmission()
    {
        try
        {
            current_subclass.ReadInitialFocusAirportSubmission();
        }
        catch (Exception ex)
        {
            Console.WriteLine("*********************************************");
            Console.WriteLine("*********************************************TWCTobrukCampaignReadInitialFocusAirportSubmission() ERROR: " + ex.ToString());
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }
    }

    //Another passthrough to the current_subclass
    public override void LaunchABumrush(int objectivesAchievedArmy, int attackingArmy, string AirportName)
    {
        try
        {
            current_subclass.LaunchABumrush(objectivesAchievedArmy, attackingArmy, AirportName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("*********************************************");
            Console.WriteLine("TWCTobrukCampaign LaunchABumrush() ERROR: " + ex.ToString());
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        }

    }


}
