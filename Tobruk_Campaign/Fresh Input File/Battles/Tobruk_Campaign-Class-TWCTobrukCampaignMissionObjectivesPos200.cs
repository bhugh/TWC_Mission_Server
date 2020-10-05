//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 

/**********************************************************************************
 * 
        ******TOBRUK CAMPAIGN BATTLE SETUP INSTRUCTIONS*******
        *
        * Generally, Mission Objective information & values can be set up in three places:
        *  
        *  #1. Tobruk_Campaign.cs - generally speaking, only a few defaults values & info
        *  
        *  #2. Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectives.cs - all of the general objectives, mobile objectives, values and settings we anticipate using for the entire campaign, or that we anticipate using as DEFAULT values for the entire campaign, which can be overriden in individual battle files if we want
        *  
        *  #3. Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesPos100.cs or similarly named file
        *      --> This file is named according to the score needed.  So Pos100.cs means a score of +100 (or probably a range +50 to +150), Pos200.cs is +200, Pos300.cs is +300, Neg100.cs is -100,
        *          Neg200.cs is -200, Zero.cs is 0 etc.  (Naming is just a convention we'll use to keep things straight.  The actual score used is set in the variables below.)
        *      --> The actual score values where this file will be activated are set below. Something like this for Pos100.cs:
        *             leastScore = 50;
        *             mostScore = 150;
        *             
        *             However...note that the last battle created, it would be smart to set the score to some huge value so that we are never left without a battle file to use.  
        *             So if our last file is say Pos500.cs then set the values something like
        *             
        *                leastScore = 450;
        *                mostScore = 10000000000;
        *                
        *            This will make Pos500.cs used for score 500 OR anything above it.    
        *            
        *  **********>>>>>>>>>>>> All that seems kind of complicated (and it is) but the purpose is so that file #1 sets up default values which can be superceded and added to 
        *  in file/class #2 and then further superceded and added to in file/class #3.  So you can have objectives and settings and code valid for any campaign we make, for this 
        *  whole campaign, or for one particular battle within the campaign
        *  
        *  How to set up a Battle:
        *            
        *   #1. In your Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesPos100.cs  or similarly named file you set up a number of variables with your Battle's subdirectory, airport files, bumrush files, objective files, etc etc etc.
        *          Look below for  battle_subdirectory = "Battles/BattlePos100/";  and a number of lines below that
        *          
        *   #2. Set up your Pos100 (or whatever your new battle .cs file is called) in several places in ...\Fresh Input File\Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectives.cs .  
        *       Try searching for "Pos100" to find all relevant places.
        *       
        *        * Add a line like //$include "$user\missions\Multi\Fatal\Tobruk_Campaign\Fresh Input File\Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesPos100.cs"  near the top of the file
        *        
        *        *  In method TWCTobrukCampaignMissionObjectives() add lines like these, being careful to make ALL needed adjustments:
        *   
        *                 TWCTCMO battle_pos200 = new BattlePos200(g, m, sc);
        *                 Console.WriteLine("Pos200 inited (return), {0} {1} {2} {3}", battle_pos200.score, battle_pos200.leastScore, battle_pos200.mostScore, battle_pos200.focus_airport_misfile_name);
        *                 register_subclass(battle_pos200);
        *   
        *   #3. On mission startup, method MissionObjectiveAirfieldFocusBumrushSetup() in file ...\Fresh Input File\Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesBattles.cs  
        *       will go through & read all the airport & bumrush files you have have set up in these variables, and verify that they exist.  
        *       
        *       ***IMPORTANT!*** If they don't exist, have a different name, not in the subdirectory specified, etc, they mission will simply EXIT with an error message.
        *       If you can't get your mission to start at all, this is almost certainly the problem.
        *       
        *   #4. Additionally, on startup method MissionObjectiveAirfieldFocusBumrushSetup() sets up your specified bumrush airports as Objectives with primaryobjectiveweight 200 and objective points=30.  
        *   
        *        Our usual scheme for points:  60 points to start the bumrush phase.  That is 6 normal objectives set for 5 points each (30 points) and then the bumrush airport at 30 points.
        *        Then 120 points to turn the map - the first 60 points ot start Bumrush and then 60 more points for winning it.
        *        Note that all these point values are set in \Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectives.cs BumRushCampaignValuesSetup() and you can override them 
        *        in each individual battle if you like, but setting up BumRushCampaignValuesSetup() in your \Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesPos100.cs 
        *        (or similarly named) file
        *        
        *   #5. Set up the primary objectives you want for the battle. To make items the primary objectives for this battle, you need to do three things below:
        *   
        *        #1 Add the objective here under MissionObjectiveTriggersSetup (OR add some code to change the values of an existing objective to make it into a primary objective)
        *        
        *        #2 Make the points value what you want it (usually 5 points): ... "1006_Chief", 2, 5, "RTobrukGasrResupplyConvoy", ...  (2 is owner army, 
        *           5 is points value, RTobrukGasrResupplyConvoy is objective ID
        *             
        *        #3 Set the primary target weight high enough.  This value varies 0-200, so if you want a target definitely or usually chosen set it to 200 or very close.  
        *           0=never chosen; 200=highest probability of being chosen.  (Even at 200 it won't necessarily ALWAYS be chosen because the selection process randomizes 
        *           the order of the list and then adds primary objectives until reaching the required total primary objective score. So if it needs 40 points and 
        *           you have 7 possible objectives each worth 10 points, it will keep choosing until it choses 4 of them and has 40 points, then stop--even if all are set 
        *           at 200 weight.)
        *           
        *            #3A. If you make more Primary Objectives than strictly necessary, that can be a good thing if the battle ends up being played repeatedly.  
        *            At minimum you need 6 objectives at 5pts each.  But say you make 10 objectives and make all of them 5 points & 200 weight.  
        *            Now every time the map is turned to this battle & new objectives selected, 6 of those 10 objectives will be randomly selected.
        *            
        *            #3B. Also I like to add a point or two to the objectives required score, which will ensure that one or two of the various airports, 
        *            mobile radar, desert radar, etc etc etc objectives around the  map are always included as primaries.
        *        
        *        #4 If you want to, go to file \Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesPos100.cs 
        *        (or similarly named file for your battle, \Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesXXXYYY.cs ), 
        *        un-rem the values in BumRushCampaignValuesSetup()  and change them as desired.  Note that these are set in Tobruk_Campaign.cs and 
        *        then overriden in Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectives.cs and then you can override them again here if you want.
        *        
        *        #5. There are various other things you can personalize per campaign or per battle, such as flak files.
        *        
        *        
        ***************************************************************************************************************************************
 * 
 * ********************************************************************************************/

using System;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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



public class BattlePos200 : TWCMissionBattles
 
{
    public override Mission msn { get; set; }
    public override Mission.MissionObjectives mmo { get; set; }
    public override TWCTCMO current_subclass { get; set; }
    public override maddox.game.IGamePlay gp { get; set; }
    public override double score { get; set; }

    public override string battle_subdirectory { get; set; } //specific values for all of these are defined below
    public override string bumrush_subdirectory { get; set; }
    public override string focusairport_subdirectory { get; set; }
    public override string primaryobjectives_subdirectory { get; set; }

    public override string red_target_airfield { get; set; }
    public override string blue_target_airfield { get; set; }

    public override string focus_airport_misfile_name { get; set; }

    public override string bumrush_redred_misfile_namematch { get; set; }
    public override string bumrush_redblue_misfile_namematch { get; set; }
    public override string bumrush_bluered_misfile_namematch { get; set; }
    public override string bumrush_blueblue_misfile_namematch { get; set; } 

    public override double leastScore { get; set; } 
    public override double mostScore { get; set; }   

    //This will set up Tobruk-specific overall/general mission objectives for the overall mission, according to the current Campaign Score (score)
    //It will then pass the routines along at the appropriate points, and to the appropriate


    public BattlePos200(maddox.game.IGamePlay g, Mission m, double sc)
    //public TWCTobrukCampaignMissionObjectives(maddox.game.IGamePlay g, Mission m, Mission.MissionObjectives mo, double sc)
    //public MissionNeg10()
    {
        msn = m;
        //mmo = mo;
        mmo = msn.mission_objectives; //so when first passed (in Mission constructor) this is NULL.  It is updated/initialized after a while.  It MUST be updated via a call to UpdateMissionObjectives() later (UpdateMissionObjectives() is defined in "public abstract class TWCTCMO" in separate file)
        gp = g;
        score = sc;

        //leastScore and mostScore tell which scoring range this battle is used for.  Soe Battle Pos100 is the Battle for score = 100 but we need to BRACKET that target score so there are NO 'blank spots' between adjacent battles (meaning, whatever score the campaign has, it is covered by SOME battle.  You can leave the first & last battles at something like score = -1000000, -1000 and 1000, 1000000.
        //Also, the score can go up OR down a little from its anchor point (0, 100, 200, 300 etc). 
        //So if this battle is for score 100 then you don't want to drop to a previous battle if the Campaign Score drops a bit to 99 or 97.
        //Remember players earn a few points here & there for various things, but 100 points (MANY times more) for winning a Battle.
        //leastScore = 50;
        //mostScore = 150;

        leastScore = 150; //until we get other battles in place we just leave this very small/large to cover any possibility of score
        mostScore = 2000000000; //Will be 250 when we have Pos300 etc;

        battle_subdirectory = "Battles/BattlePos200/"; //this will be found inside the main mission directory. Suggest making it match this Class Name within the 'Battles' subdirectory, like "Battles/BattlePos100/" Must end with /

        //Note: You can put each of these into a separate subdirectory if you want but after trying it I found it easier to just keep them all in the battle_subdirectory where you can keep an eye on them more easily
        bumrush_subdirectory = ""; //this will be found inside the battle_subdirectory.  Must end with /
        focusairport_subdirectory = ""; //this will be found inside the battle_subdirectory.  Must end with /
        primaryobjectives_subdirectory = ""; //this will be found inside the battle_subdirectory.  Must end with /
        //It is used in RadarPositionTriggersSetup and MissionObjectiveTriggersSetup as the directory to look for any submission files loaded
        //as part of Primary Objectives for this battle

        red_target_airfield = "Sidi Azeiz Airfield";  //The names of these two airfield must match EXACTLY with the name given 
        blue_target_airfield = "Gasr el Abid South Airfield"; //to these airports/spawnpoints in the file  focus_airport_misfile_name

        focus_airport_misfile_name = "FocusAirports-Gasr_el_abid SidiAzeiz.mis"; //this file must be found in subdir battle_subdirectory + focusairport_subdirectory but DON'T ADD THOSE IN HERE or you'll be unhappy;

        //all the "namematch" string below are "match" strings, meaning that you ***DON'T*** need to include the .mis extension
        //and for example "Tobruk_Campaign-Rush-Red-Gasr el Abid South Airfield" will match ANY OF:
        //  -  Tobruk_Campaign-Rush-Red-Gasr el Abid South Airfield-1.mis
        //  -  Tobruk_Campaign-Rush-Red-Gasr el Abid South Airfield-2.mis
        //  -  Tobruk_Campaign-Rush-Red-Gasr el Abid South Airfield-amyfunnystuffyouwantattheend.mis
        //  -  etc
        //So you can include several files with different endings in your subdirectory and a the right time, ONE of them will be picked randomly.
        //These airports should be found in the battle_subdirectory + bumrush_subdirectory subdirectory.

        bumrush_redred_misfile_namematch = "Tobruk_Campaign-Rush-Red-Sidi Azeiz Airfield"; //redred means red won the initial mission & is trying to advance to the airport; red is currently bumrushing that airport
        bumrush_redblue_misfile_namematch = "Tobruk_Campaign-Rush-Blue-Sidi Aziez Airfield"; //redblue means red won the initial mission & is trying to advance to that airport; blue is currently counterattacking that airport
        bumrush_bluered_misfile_namematch = "Tobruk_Campaign-Rush-Red-Gasr el Abid South Airfield";  //Scegga No3 is the airport Blue is trying to take over in this mission; this is the Red counterattack
        bumrush_blueblue_misfile_namematch = "Tobruk_Campaign-Rush-Blue-Gasr el Abid South Airfield";  //Scegga No3 is the airport Blue is trying to take over in this mission; this is the Blue attack

        Console.WriteLine("Pos200 inited, {0} {1} {2} {3}", score, leastScore, mostScore, focus_airport_misfile_name);

    }

    public override void OverrideCampaignTurnMapRequirements()
    {

        //IF YOU WANT TO CHANGE THE SCORING FOR THIS SPECIFIC BATTLE to be different from the usual way of scoring
        //the battles, you can un-rem this & do so here.
        //As a rule, you can just leave this alone.

        /*
        //What percent of primary targets is actually required ot turn the map
        //If you make it 100% you have to get them all, but if some are difficult or impossible then that army will be stuck
        msn.MO_PercentPrimaryTargetsRequired = new Dictionary<ArmiesE, double>() {
            {ArmiesE.Red, 80 },
        };
            {ArmiesE.Blue, 80 }

        //TODO: Use similar scheme for total points, objectives completed list, objectives completed
        //Points required, assuming they are doing it entirely with Primary Targets; ie, secondary or other targets do not count towards this total
        //at all
        msn.MO_PointsRequiredToTurnMap = new Dictionary<ArmiesE, double>() {
            {ArmiesE.Red, 120 },
            {ArmiesE.Blue, 120 }
        };


        msn.MO_BRBumrushInfo = new Dictionary<ArmiesE, M.MO_BRBumrushInfoType>() {
            { ArmiesE.Red, new M.MO_BRBumrushInfoType() {
                PointsRequiredToBeginBumrush= 60,
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
            }
            catch (Exception ex) { Console.WriteLine("TWCTobrukCampaignMissionObjectives ERROR: " + ex.ToString()); }
        }
        */
    }

    public override void RadarPositionTriggersSetup(bool addNewOnly = false)
    {

        //IF YOU WANT TO SET UP SPECIFIC RADAR TARGETS OR OBJECTIVES FOR THIS SPECIFIC BATTLE to be different from the usual way of scoring
        //the battles, you can un-rem this & do so here.
        //As a rule, you can just leave this alone and use the same radar setup as the main mission.


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

        //public void mmo.addPointArea(M.MO_ObjectiveType mot, string n, string flak, string initSub, int ownerarmy, double pts, string tn, double x = 0, double y = 0, double largearearadius = 100, double smallercentertargettrigrad=300, double orttkg = 8000, double ortt = 0, double ptp = 100, double ttr_hours = 24, bool af, bool afip, int fb, int fnib, string comment = "", bool addNewOnly = false)
        //mmo.addPointArea(M.MO_ObjectiveType.Building, "Dover Naval HQ", "Dove", "", 1, 3, "BTargDoverNavalOffice", 245567, 233499, 50, 50, 800, 4, 120, 48, true, true, 4, 7, "", add);
        //NOTE: renaming the radar targets as "RPA in stead of just "R" so they no longer match the .mis file trigger names (which WILL still trigger if they are still in the file).
        mmo.addRadarPointArea("Oye Plage Freya Radar", "OypR", 2, 4, 2, "RTarget28RPA", "", 1000, 4, 294183, 219444, 85, 20000, false, 35, "", add);
        mmo.addRadarPointArea("Coquelles Freya Radar", "CoqR", 2, 4, 2, "RTarget29RPA", "", 1000, 4, 276566, 214150, 85, 20000, false, 35, "", add);

        */

        
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
        try
        {

            //IF YOU WANT TO SET UP SPECIFIC MISSION OBJECTIVES FOR THIS SPECIFIC BATTLE
            //
            //As a rule, this is where a major part of your battle setup will go - the individualized Primary Objective setup for each specific mission.
            //
            //Conventions we've been following (which you  can tweak or alter for each Battle as you like):
            //  - 6 primary objectives for each side, and each scored at 5 points.  Thus total needed to move to bumrush phase is 6*5 = 30 points
            //     - You can add one or two extra primary objectives here, valued at 5 points, and in that case the mission setup routine will randomly choose from among those until reaching 30 points
            //     - You DON'T need to specify the Focus Airport for each side; that is worth 30 points and is set up separately below
            //
            //Notes re: Objective setup:
            //   - You have a couple of different major types to choose from "Trigger" and "PointArea"
            //      - more detail & examples of each time below
            //   - You can specify a submission to load when this objective is active.  Generally it is kept in the po_dir subdirectly which you'll see defined below based on your inputs above.
            //       - It is usually something like maindirectory/Battles/BattlePos100/PrimaryObjectives
            //   - You can specify a TRIGGER NAME for trigger-type objectives.  If you use a trigger objective it is VITAL that you spell the trigger name here EXACTLY as it is spelled in the submission file
            //       - If you add a trigger to a submission you MUST also include a specific script file with that submission. Info here: https://theairtacticalassaultgroup.com/forum/showthread.php?t=34113&p=363197#post363197
            //   - For objectives that move (ie, convoys, ships) you can specify a "chief name" for that objective. This gives our script the opportunity to locate that objective wherever it is and then 
            //     report that position to players (ie, if they have reconned the area, then the ACTUAL position will show up--not simply the static position where the actors spawned in)
            //      -technique for this is to give the chief a distinctive name in the .mis file.  See examples below "1006_Chief" ""1009_Chief" etc.  You can use about anything reasonable "2020_TWCTrain" etc
            //      - Be sure to rename all corresponding "roads" in the .mis to match.
            //      - Be sure to name the groups covered by any TRIGGERS in the .mis to match.  Otherwise your triggers will altogether stop working!
            //      - If you edit your file in FMB it will possibly/probably change your unique name back to something less you (or just, something different) and you will have to re-edit it again
            //        by hand afterwards to make sure you still have the same unique "chief name" that you have entered below.
            /*       Example (note special name "1009_Chief" in two places): 
             *          [Chiefs]
             *            1009_Chief Vehicle.VehicleRush5 gb 
             *          [1009_Chief_Road]
             *            327607.78 126528.52 38.40  0 8 1.67
             *            S 108 53 0.58 20.00 P 327551.06 126100.30
             *            ...
             *          [Trigger]
             *            RTobrukGasrResupplyConvoy TGroupDestroyed 1006_Chief 100

             *
             *  
             *  
             *  
             * *****************************************************/
            bool add = addNewOnly;

            //Format: mmo.addTrigger(M.MO_ObjectiveType.Building (Aircraft, airport, etc), "Name,                      OwnerArmy,Points,ID,TriggerType,PercRequired,XLoc,YLoc,Radius,IsPrimaryTarget,IsPrimaryTargetWeight,TimeToRepairIfDestroyed_hours,Comment "");
            //PercRequired doesn't actually do anything because the perc required is set in the TRIGGER in the .mis file.  However if you accurately record here the same
            //percent to kill value in the .mis file we can do interesting/helpful things with it here.

            /*************************************************************************************
            ******TOBRUK CAMPAIGN*******
            To make items the primary objectives for this battle, you need to do three things

             * Add the objective here under MissionObjectiveTriggersSetup (OR add some code to change the values of an existing objective to make it into a primary objective)

             * Make the points value what you want it (usually 5 points): ... "1006_Chief", 2, 5, "RTobrukGasrResupplyConvoy", ...  (2 is owner army, 5 is points value, RTobrukGasrResupplyConvoy is objective ID

             * Set the primary target weight high enough.  This varies 0-200 so if you want a target definitely or usually chosen set it to 200 or very close.  0=never chosen.  (Even at 200 it won't necessarily ALWAYS be chosen because it randomizes the order of the list and then adds primary objectives until reaching the required total primary objective score.)

             * If you want to, un-rem the values in BumRushCampaignValuesSetup() above and change them as desired.  Note that these are set in Tobruk_Campaign.cs and then overriden in Battles\Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectives.cs and then you can override them again here if you want.

            *******************************************/

            //BLUE TARGETS

            // mmo.addTrigger(M.MO_ObjectiveType.Building, "Coal mines in Normandy", "Shor", 1, 3, "BTargNormandyMiningCenter", "TGroundDestroyed", 50, 67510, 26083, 50, false, 100, 24, "", add);			
            //mmo.addTrigger(M.MO_ObjectiveType.Building, "Shoreham Submarine Base", "", 1, 3, "BTargShorehamSubmarineBase", "TGroundDestroyed", 10, 137054, 198034, 50, false, 120, 24, "", add);           

            //RED & BLUE CONVOY PRIMARY OBJECTIVES.
            //These are in sub-missions and includes Oskar's new code to pass the trigger on to the main mission
            string po_dir = battle_subdirectory + primaryobjectives_subdirectory;

            /*
            mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Tobruk-Gasr Resupply Convoy", "", po_dir + "Tobruk_Campaign-LOADONCALL-Red-RTobrukGasrResupplyConvoy-objective.mis", "1006_Chief", 2, 5, "RTobrukGasrResupplyConvoy", "TGroupDestroyed", 100, 197907, 95422, 100, false, 200, 24, "", add);  //g
            mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Alsmar-Gasr Resupply Convoy", "", po_dir + "Tobruk_Campaign-LOADONCALL-Red-RAlsmarGasrResupplyConvoy-objective.mis", "1007_Chief", 2, 5, "RAlsmarGasrResupplyConvoy", "TGroupDestroyed", 100, 150983, 37668, 100, false, 200, 24, "", add);  //g    

            mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Sidi-Scegga Resupply Convoy", "", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-BSidiSceggaResupplyConvoy-objective.mis", "1009_Chief", 1, 5, "BSidiSceggaResupplyConvoy", "TGroupDestroyed", 100, 327607, 126528, 100, false, 200, 24, "", add);  //g
            mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Siwi-Scegga Resupply Convoy", "", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-BSiwiSceggaResupplyConvoy-objective.mis", "1008_Chief", 1, 5, "RSiwiSceggaResupplyConvoy", "TGroupDestroyed", 100, 286184, 35204, 100, false, 200, 24, "", add);  //g 
            */

            /*"2006_Chief", 1, 5, - this is "chief name", OWNER army, and objective points.
             * Note that the name like "Tobruk_Campaign-LOADONCALL-Blue-HabataResupplyConvoy-objective.mis" means that this a Blue **TARGET**.  So it is a target of Army #2, meaning it is OWNED By Army #1.
             * 
             * So  "Tobruk_Campaign-LOADONCALL-Blue-HabataResupplyConvoy-objective.mis", "2006_Chief", 1, 5, is correct - BLUE target (Army #2), RED owner (Army #1)
             */

        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Habata Resupply Convoy", "Habat", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-HabataResupplyConvoy-objective.mis", "2006_Chief", 1, 5, "BHabataResupplyConvoy", "TGroupDestroyed", 100, 345037, 134111, 100, false, 200, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "El Adem Resupply Convoy", "EAdem1_2", po_dir + "Tobruk_Campaign-LOADONCALL-Red-ElAdemResupplyConvoy-objective.mis", "2007_Chief", 2, 5, "RTobrukElAdemResupplyConvoy", "TGroupDestroyed", 100, 79756, 158961, 100, false, 200, 24, "", add);  //g    

        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Scegga Resupply Convoy", "Sceg", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-SceggaResupplyConvoy-objective.mis", "2011_Chief", 1, 5, "BSceggaResupplyConvoy", "TGroupDestroyed", 100, 330157, 24385, 100, false, 200, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Bomba Resupply Convoy", "BombaN", po_dir + "Tobruk_Campaign-LOADONCALL-Red-BombaResupplyConvoy-objective.mis", "2008_Chief", 2, 5, "RBombaResupplyConvoy", "TGroupDestroyed", 100, 73677, 230014, 100, false, 200, 24, "", add);  //g 


        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Tobruk Tanker", "Tobr", "", "2002_Chief", 2, 4, "RTobrukTanker", "TGroupDestroyed", 100, 172859, 214570, 100, false, 200, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Tobruk Cruiser", "Tobr", "", "2003_Chief", 2, 5, "RTobrukCruiser", "TGroupDestroyed", 100, 172859, 214570, 100, false, 200, 24, "", add);  //g            
        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Bardia U-Boot", "", "Battles/Objectives/Tobruk_Campaign-LOADONCALL-GerSubmarine-objective.mis", "2001_Chief", 2, 5, "RBardiaUboot", "TGroupDestroyed", 100, 267437, 149991, 100, false, 200, 24, "", add);  //g            

        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Sidi Barrani Tanker", "", "", "2005_Chief", 1, 4, "BSidiBarraniTanker", "TGroupDestroyed", 100, 295708, 193686, 100, false, 200, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Sidi Barrani Corvette", "", "", "2004_Chief", 1, 5, "BSidiBarraniCorvette", "TGroupDestroyed", 100, 357990, 237331, 100, false, 200, 24, "", add);  //g     
        mmo.addTrigger(M.MO_ObjectiveType.Ship, "Sidi Barrani Submarine", "", "Battles/Objectives/Tobruk_Campaign-LOADONCALL-BritSubmarine-objective.mis", "2010_Chief", 1, 5, "BBarraniSub", "TGroupDestroyed", 100, 346954, 136122, 100, false, 200, 24, "", add);  //g      

        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Gazala Fuel", "Gaz2_3", po_dir + "Tobruk_Campaign-LOADONCALL-Red-GazalaFuel-objective.mis", 2, 5, "RTargGazalaFuel", 97004, 199277, 200, 150, 2500, 8, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Marsa Bela Farit Dock Fuel ", "BeLa", po_dir + "Tobruk_Campaign-LOADONCALL-Red-Marsa BelafaritDockFuel2-objective.mis", 2, 5, "RTargMarsaBelaFuel", 205707.13, 176739.11, 200, 150, 2800, 7, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Inland Fuel Dump", "", po_dir + "Tobruk_Campaign-LOADONCALL-Red-GermanFuelDump-objective.mis", 2, 5, "RTargetInlandFuel", 127599, 197411, 200, 150, 2500, 8, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Armor Camp", "", po_dir + "Tobruk_Campaign-LOADONCALL-Red-GermanArmorCamp-objective.mis", 2, 5, "RArmorCamp", 195633, 122432, 125, 100, 1800, 30, 200, 48, false, true, 3, 7, "", add);




        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Bir El Malla Fuel Dump", "SidiBar", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-BirElMallaFuel.mis", 1, 5, "BSidiBarraniFuelDump", 359414, 98907, 150, 125, 2500, 6, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Alam El Neiyat Armor Camp", "", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-AlamElNeiyatBritishArmorCamp-objective.mis", 1, 5, "BTargAlamNeiyat", 267854, 76291, 150, 125, 2800, 8, 200, 48, false, true, 3, 7, "", add);
       // mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "", "", po_dir + "", 1, 5, "AlamElNeiyat", 301770, 89683, 150, 125, 1800, 30, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Alam Barghut Armor Camp", "BuQBuQ", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-AlamBarghutBritishArmorCamp-objective.mis", 1, 5, "BTagAlamBarghut", 287733, 118539, 150, 125, 1800, 30, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Awdyat Armor Camp", "AwD", po_dir + "Tobruk_Campaign-LOADONCALL-Blue-AwdatAshAhiyahBritishArmorCamp-objective.mis", 1, 5, "BTargAwdyat", 231676, 30075, 150, 125, 2800, 8, 200, 48, false, true, 3, 7, "", add);



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
                    
        }
        catch (Exception ex) {
            Console.WriteLine();
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("TWCTobrukCampaignMissionObjectivesXXX MAJOR ERROR IN OBJECTIVE SETUP!!!!!!!!!!!!!!!!!!!!!! " + ex.ToString());
            Console.WriteLine("MISSION OBJECTIVES FOR THIS BATTLE ****NOT**** SET UP!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine();
        }

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
        //IF YOU WANT TO SET UP SPECIFIC FLAK FILE FOR OBJECTIVES FOR THIS SPECIFIC BATTLE, ADD THEM HERE
        //
        //As a rule, this is set up in TWCTobrukCampaignMissionObjectives and you don't need to change it here.
        //Note that as written below, what you do here will REPLACE what is done in TWCTobrukCampaignMissionObjectives. 
        //You could rewrite to add to it, replace part of it, or whatever you want.


        /*
        //Names of the flak areas and link to file name

        //Name is used in list of objectives aboe & must match exactly.  You can change the name below but then the name in the mmo.addTrigger etc above must also be changed to match
        //file name must match exactly with the filename
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


        /*
            };
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

        //IF YOU WANT TO SET UP SPECIFIC MISSION POINTS REQUIRED ETC FOR THIS SPECIFIC BATTLE, ADD/CHANGE THEM THEM HERE
        //
        //As a rule, this is set up in TWCTobrukCampaignMissionObjectives and you don't need to change it here.
        //
        //FOR TOBRUK, we should not need to change these values at all for each individual battle.
        //However, if we want OR NEED to change them due to the way the battle is set up, we can do that here.

        /*
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

        */

        if (current_subclass != null)
        {
            try
            {
                current_subclass.BumRushCampaignValuesSetup();
            }
            catch (Exception ex) { Console.WriteLine("TWCTobrukCampaignMissionObjectives ERROR: " + ex.ToString()); }
        }

    }


    /* 
     * The methods below are in public abstract class MissionBattles and generally you should not need to override them.
     * However, you can do so if you want to!
     * 
    public override void MissionObjectiveAirfieldFocusBumrushSetup()
    {
        
    }

    public override void ReadInitialFocusAirportSubmission()
    {
      
    }
    */

}
