/*********************************************
 * 
 * This is a an overarching class for all the battles classes, ie 
 * BattlePos100, BattleNeg100, BattleNeg200, BattleZero, etc
 * 
 * ******************************************/

//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll 


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


public abstract class TWCMissionBattles : TWCTCMO
{
    public abstract Mission msn { get; set; }
    public override Mission.MissionObjectives mmo { get; set; }
    public override TWCTCMO current_subclass { get; set; }
    public abstract maddox.game.IGamePlay gp { get; set; }
    public override double score { get; set; }   

    public abstract string battle_subdirectory { get; set; } //this will be found inside the main mission directory
    public abstract string bumrush_subdirectory { get; set; } //this will be found inside the battle_subdirectory
    public abstract string focusairport_subdirectory { get; set; } //this will be found inside the battle_subdirectory
    public abstract string primaryobjectives_subdirectory { get; set; } //this will be found inside the battle_subdirectory

    public abstract string red_target_airfield { get; set; }
    public abstract string blue_target_airfield { get; set; }

    public override string focus_airport_misfile_name { get; set; }

    public abstract string bumrush_redred_misfile_namematch { get; set; }  //This is a "match" string; any .mis file in subdir Bumrushes matching this, like "*bumrush_redred_mismatch*.mis" maybe be picked for this bumrush file.  You MUST supply at least one .mis file matching this pattern but if you supply 2 or more then one will be randomly chosen
                                                                  //redred means red won the initial mission & is trying to advance ot the airport; red is currently bumrushing that airport
    public abstract string bumrush_redblue_misfile_namematch { get; set; }  //This is a "match" string; any .mis file in subdir Bumrushes matching this
                                                                   //redblue means red won the initial mission & is trying to advance ot the airport; blue is currently bumrushing that airport
    public abstract string bumrush_bluered_misfile_namematch { get; set; }  //Scegga No3 is the airport Blue is trying to take over in this mission; this is the Red counterattack
    public abstract string bumrush_blueblue_misfile_namematch { get; set; }  //Scegga No3 is the airport Blue is trying to take over in this mission; this is the Blue attack

    public override double leastScore { get; set; } //Every subclass MUST include this value, which is the least campaign score value for which it will be loaded (>=)
    public override double mostScore { get; set; }   //Every subclass MUST include this value, which is the greatest campaign score value for which it will be loaded (<)
                                            //Typically if the class is for say point value 100, you set it 50,150 so as to bracket the desired mission point value.
                                            //Or you set them -100,0 ; 0,100 ; 100,200 ; 200-300, so that players much reach 10 points (for example) before that submission kicks in
                                            //Note that >= is used on the bottom and < on the top so that you can overlap as shown above & only one submission will be operative
                                            //HOWEVER nothing is checking that you have avoided overlapping.  So if you have -100,0 & -50,50 & 0,100 submission then, TWO of them
                                            //will be operative AT ONCE for score of -50 to 50.  Only one will actually be chosen and you won't be able to predict which.  Just avoid doing this.
                                            //When one side wins the Bumrush and turns the map, they are awarded 10,000 player points.  1 "Score" point = 100 Player Points, so
                                            //Advancing 10,000 Player Points means advancing 100 score point.  So think of each phase as being incremented by 100, like -400, -300, -200, -100, 0, 100, 200, 300, 400 etc

    //This will set up Tobruk-specific overall/general mission objectives for the overall mission, according to the current Campaign Score (score)
    //It will then pass the routines along at the appropriate points, and to the appropriate


    public TWCMissionBattles()

    {

    }

    public override void LaunchABumrush(int objectivesAchievedArmy, int attackingArmy, string AirportName)
    {
        string aword = "Red";
        if (attackingArmy == 2) aword = "Blue";
        Console.WriteLine("Starting a new Bumrush for " + aword + " at " + AirportName);

        string brfile = "";
        if (objectivesAchievedArmy == 1 && attackingArmy == 1) brfile = bumrush_redred_misfile_namematch;
        if (objectivesAchievedArmy == 1 && attackingArmy == 2) brfile = bumrush_redblue_misfile_namematch;
        if (objectivesAchievedArmy == 2 && attackingArmy == 1) brfile = bumrush_bluered_misfile_namematch;
        if (objectivesAchievedArmy == 2 && attackingArmy == 2) brfile = bumrush_blueblue_misfile_namematch;

        msn.LoadRandomSubmission(fileID: brfile, subdir: battle_subdirectory + bumrush_subdirectory);
    }

    public override void MissionObjectiveAirfieldFocusBumrushSetup()
    {
        //*********************************************************************
        //WARNING!!!!!!!!!!!!!!!!!!!WARNING!!!!!!!!!!!!!!!!!!!WARNING!!!!!!!!!!!!!!!!!!!WARNING!!!!!!!!!!!!!!!!!!!WARNING!!!!!!!!!!!!!!!!!!!
        //
        // The Airport name as spelled here must EXACTLY match the name of the airport as found in the BIRTHPLACE listing of the .mis file
        // If you mis-spell it there you MUST mis-spell it EXACTLY the same here or IT WILL NOT WORK AT ALL!!!!!
        //[BirthPlace]
        //        Tariq_mysnot 1 226402 43716 0 1 1 1. . . 0
        //        Habatha_Kabatha 1 311428 81015 0 1 1 1. . . 0
        //        Wourk-el-SlABU 1 311428 41015 0 1 1 1. . . 0
        //
        //  String[] TargetAirfieldsList = { "Tariq_mysnot", "Habatha_Kabatha", "Wourk-el-SlABU" };

        //Also the objectives list has underlines instead of spaces, like Scegga_No2_airfield - so we'll have to adjust there; not sure why it is.
        Console.WriteLine("***Starting Focus Airport/Bumrush setup (Battles/Tobruk_Campaign-Class-TWCTobrukCampaignMissionObjectivesBattles.cs)");

        try
        {

            //msn.gpLogServerAndLog(null, "***Starting Focus Airport/Bumrush setup", null);


            if (!msn.MissionObjectivesList.ContainsKey(red_target_airfield + "_spawn")) //.Replace(' ', '_')
                Console.WriteLine(" **************WARNING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! A target Airport does not exist, check FocusBumrushSetup: " + red_target_airfield, null);

            if (!msn.MissionObjectivesList.ContainsKey(blue_target_airfield + "_spawn")) //.Replace(' ', '_')
                Console.WriteLine("**************WARNING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! A target Airport does not exist, check FocusBumrushSetup: " + blue_target_airfield, null);

            msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName = red_target_airfield + "_spawn";
            msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName = blue_target_airfield + "_spawn";

            msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName = red_target_airfield;
            msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName = blue_target_airfield;


            if (!msn.MissionObjectivesList.ContainsKey(msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName))
            {
                Console.WriteLine("", null);
                Console.WriteLine("*********MAJOR STARTUP ERROR!!!! RED Target Airport does not exist as an airfield in the FocusAirports/*.mis file!!!!! Perhaps it is misspelled there? Exiting....", null);
                Console.WriteLine("", null);
                //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");  //doesn't work for some unknown reason//!????
                System.Threading.Thread.Sleep(2200);
                System.Environment.Exit(1);
            }
            else
            {
                //Here is where we set up the two Bumrush airports to be primary objectives with primarytargetweight=200 and points=30
                msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective = msn.MissionObjectivesList[msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName];
                M.MissionObjective mo = msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective;
                mo.IsPrimaryTarget = true;
                mo.IsFocus = true;
                mo.PrimaryTargetWeight = 200;
                mo.Points = 30; // 6 primary objects * 5 + the main airport makes 60 points required to start bumrush
                Console.WriteLine("RED Primary Target Airport is " + msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective.AirfieldName, null);

            }

            if (!msn.MissionObjectivesList.ContainsKey(msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName))
            {
                Console.WriteLine("", null);
                Console.WriteLine("*********MAJOR STARTUP ERROR!!!! BLUE Target Airport does not exist as an airfield in the FocusAirports/*.mis file!!!!! Perhaps it is misspelled there? Exiting....", null);
                Console.WriteLine("", null);
                //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");
                System.Threading.Thread.Sleep(2200);
                System.Environment.Exit(1);
            }
            else
            {
                msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective = msn.MissionObjectivesList[msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName];
                M.MissionObjective mo = msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective;
                mo.IsPrimaryTarget = true;
                mo.IsFocus = true;
                mo.PrimaryTargetWeight = 200;
                mo.Points = 30; // 6 primary objects * 5 + the main airport at 30 points makes 60 points required to start bumrush

                Console.WriteLine("BLUE Primary Target Airport is " + msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective.AirfieldName, null);
            }

            bumrush_bluered_misfile_namematch = stripmis(bumrush_bluered_misfile_namematch); //strip off any ".mis" that may be added to th end
            bumrush_redred_misfile_namematch = stripmis(bumrush_redred_misfile_namematch); //strip off any ".mis" that may be added to th end
            bumrush_redblue_misfile_namematch = stripmis(bumrush_redblue_misfile_namematch); //strip off any ".mis" that may be added to th end
            bumrush_blueblue_misfile_namematch = stripmis(bumrush_blueblue_misfile_namematch); //strip off any ".mis" that may be added to th end

            //Check if the files actually exist.  Does NOT actually load them, just checks if they are actually there with correct name!!!!
            bool blue_red = msn.LoadRandomSubmission(fileID: bumrush_bluered_misfile_namematch, subdir: battle_subdirectory + bumrush_subdirectory, check: true);
            bool red_red = msn.LoadRandomSubmission(fileID: bumrush_redred_misfile_namematch, subdir: battle_subdirectory + bumrush_subdirectory, check: true);
            bool blue_blue = msn.LoadRandomSubmission(fileID: bumrush_blueblue_misfile_namematch, subdir: battle_subdirectory + bumrush_subdirectory, check: true);
            bool red_blue = msn.LoadRandomSubmission(fileID: bumrush_redblue_misfile_namematch, subdir: battle_subdirectory + bumrush_subdirectory, check: true);

            if (!blue_red || !red_red || !blue_blue || !red_blue)
            {
                Console.WriteLine("", null);
                Console.WriteLine("*********MAJOR STARTUP ERROR!!!! One of the REQUIRED bumrush .mis files in subdir Bumrushes/ is MISSING or MISNAMED!!!!!! Exiting....", null);
                Console.WriteLine("", null);
                if (!blue_red) Console.WriteLine("*********MISSING OR MISNAMED FILE: " + bumrush_bluered_misfile_namematch, null);
                if (!red_red) Console.WriteLine("*********MISSING OR MISNAMED FILE: " + bumrush_redred_misfile_namematch, null);
                if (!blue_blue) Console.WriteLine("*********MISSING OR MISNAMED FILE: " + bumrush_blueblue_misfile_namematch, null);
                if (!red_blue) Console.WriteLine("*********MISSING OR MISNAMED FILE: " + bumrush_redblue_misfile_namematch, null);
                Console.WriteLine("", null);
                //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");
                System.Threading.Thread.Sleep(2200);
                System.Environment.Exit(1);

            }
            else
            {
                Console.WriteLine("All needed BUMRUSH .mis files for both Blue & Red Primary Target Airports are in place.", null);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("*********MAJOR STARTUP ERROR!!!!");
            Console.WriteLine("Battles: MissionObjectiveAirfieldFocusBumrushSetup() SERIOUS ERROR!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! " + ex.ToString());
            Console.WriteLine("*********MAJOR STARTUP ERROR!!!!");
            Console.WriteLine();
        }

    }

    public override void ReadInitialFocusAirportSubmission()
    {
        try
        {
            Console.WriteLine("ReadInitFocusAirport reading {0}", focus_airport_misfile_name);
            focus_airport_misfile_name = stripmis(focus_airport_misfile_name);
            Console.WriteLine("ReadInitFocusAirport reading UPDATED {0}", focus_airport_misfile_name);
            int res = msn.ReadInitialSubmissions(focus_airport_misfile_name, 0, 0, subdir: battle_subdirectory + focusairport_subdirectory); //The "focus airports" file(s) for the TOBRUK mission 2020-08
        }
        catch (Exception ex) {
            Console.WriteLine();
            Console.WriteLine("*********MAJOR STARTUP ERROR!!!!");
            Console.WriteLine("Battles: ReadInitialFocusAirportSubmission() MAJOR STARTUP ERROR!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! " + ex.ToString());
            Console.WriteLine("*********MAJOR STARTUP ERROR!!!!");
            Console.WriteLine();
        }

    }

    public string stripmis(string s)
    {
        if (s.Length<4) return s;
        if (s.ToLower().EndsWith(".mis")) return s.Substring(0, s.Length - 4);
        return s;
    }
}
