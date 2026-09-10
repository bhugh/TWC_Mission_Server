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
//using Mission;


public class TWCTobrukCampaignCampaignBumrushSetup {
    Mission msn;
    Mission.MissionObjectives mmo;
    public TWCTobrukCampaignCampaignBumrushSetup(Mission m, Mission.MissionObjectives mo)
    {
        msn = m;
        mmo = mo;
        //mmo = msn.mission_objectives;

    }

    private void runme(Mission msn) {

        
    }

    String[] MO_BRBumrushTargetAirfieldsList = { "Tariq Al Ghubay Airfield", "Scegga No3 Airfield", "Gasr el Abid South Airfield", "Sidi Azeiz Airfield", "Sidi Rezegh LG153 Airfield" };

    //IMPORTANT: If you have different spawn points for the same airfield (say, in different .mis files) this will cause problems here!  Make sure spawnpoints for these airports are in one .mis file only!

    public void MO_BRMissionObjectiveAirfieldFocusBumrushSetup(Mission msn, maddox.game.IGamePlay gp, int level)
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

        gpLogServerAndLog(null, "***Starting Focus Airport/Bumrush setup", null);




        foreach (string af in MO_BRBumrushTargetAirfieldsList)
        {
            if (!MissionObjectivesList.ContainsKey(af + "_spawn")) //.Replace(' ', '_')
                gpLogServerAndLog(null, "**************WARNING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! A target Airport does not exist, check FocusBumrushSetup: " + af, null);

        }

        int blueTarget = level;
        int redTarget = level + 1;

        MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName = MO_BRBumrushTargetAirfieldsList[redTarget] + "_spawn";
        MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName = MO_BRBumrushTargetAirfieldsList[blueTarget] + "_spawn";

        MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName = MO_BRBumrushTargetAirfieldsList[redTarget];
        MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName = MO_BRBumrushTargetAirfieldsList[blueTarget];


        if (!MissionObjectivesList.ContainsKey(MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName))
        {
            gpLogServerAndLog(null, "", null);
            gpLogServerAndLog(null, "*********MAJOR STARTUP ERROR!!!! RED Target Airport does not exist!!!!! Exiting....", null);
            gpLogServerAndLog(null, "", null);
            //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");  //doesn't work for some unknown reason//!????
            System.Environment.Exit(1);
        }
        else
        {
            MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective = MissionObjectivesList[MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName];
            MissionObjective mo = MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective;
            mo.IsPrimaryTarget = true;
            mo.IsFocus = true;
            mo.PrimaryTargetWeight = 200;
            mo.Points = 30; // 6 primary objects * 5 + the main airport makes 60 points required to start bumrush
            gpLogServerAndLog(null, "RED Primary Target Airport is " + MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective.AirfieldName, null);

        }

        if (!MissionObjectivesList.ContainsKey(MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName))
        {
            gpLogServerAndLog(null, "", null);
            gpLogServerAndLog(null, "*********MAJOR STARTUP ERROR!!!! BLUE Target Airport does not exist!!!!! Exiting....", null);
            gpLogServerAndLog(null, "", null);
            //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");
            System.Environment.Exit(1);
        }
        else
        {
            MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective = MissionObjectivesList[MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName];
            MissionObjective mo = MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective;
            mo.IsPrimaryTarget = true;
            mo.IsFocus = true;
            mo.PrimaryTargetWeight = 200;
            mo.Points = 30; // 6 primary objects * 5 + the main airport at 30 points makes 60 points required to start bumrush

            gpLogServerAndLog(null, "BLUE Primary Target Airport is " + MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective.AirfieldName, null);
        }

        //Check if the files actually exist.  Does NOT actually load them, just checks if they are there with correct name!!!!
        bool blue_red = LoadRandomSubmission(fileID: MISSION_ID + "-Rush-Blue-" + MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName, subdir: "Bumrushes", check: true);
        bool red_red = LoadRandomSubmission(fileID: MISSION_ID + "-Rush-Red-" + MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName, subdir: "Bumrushes", check: true);
        bool blue_blue = LoadRandomSubmission(fileID: MISSION_ID + "-Rush-Blue-" + MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName, subdir: "Bumrushes", check: true);
        bool red_blue = LoadRandomSubmission(fileID: MISSION_ID + "-Rush-Red-" + MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName, subdir: "Bumrushes", check: true);

        if (!blue_red || !red_red || !blue_blue || !red_blue)
        {
            gpLogServerAndLog(null, "", null);
            gpLogServerAndLog(null, "*********MAJOR STARTUP ERROR!!!! One of the REQUIRED " + MISSION_ID + "-Rush....mis files is MISSING or MISNAMED!!!!!! Exiting....", null);
            gpLogServerAndLog(null, "", null);
            if (!blue_red) gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + MISSION_ID + " - Rush-Blue-" + MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName, null);
            if (!red_red) gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + MISSION_ID + "-Rush-Red-" + MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName, null);
            if (!blue_blue) gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + MISSION_ID + "-Rush-Blue-" + MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName, null);
            if (!red_blue) gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + MISSION_ID + "-Rush-Red-" + MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName, null);
            gpLogServerAndLog(null, "", null);
            //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");
            System.Environment.Exit(1);

        }
        else
        {
            gpLogServerAndLog(null, "All needed BUMRUSH .mis files for both Blue & Red Primary Target Airports are in place.", null);
        }

        /*

        m_TargetAirfields.Add(ARMY_RED, "Habata_LG79", 9000, 1);
        m_TargetAirfields.Add(ARMY_RED, "Tariq_Al_Ghubay", 9000, 20);// denotes initial airport to attack and occupy 
        m_TargetAirfields.Add(ARMY_RED, "Scegga_No1", 9000, 1);
        m_TargetAirfields.Add(ARMY_RED, "Awdyat_ash_Ahiyah", 9000, 1);
        m_TargetAirfields.Add(ARMY_RED, "Bir_el_Malla_South_LG76", 9000, 1);
        m_TargetAirfields.Add(ARMY_RED, "Buq_Buq_LG01", 9000, 1);
        m_TargetAirfields.Add(ARMY_RED, "Bir_Basur_LG69", 9000, 1);
        /// blue airfields 
        m_TargetAirfields.Add(ARMY_BLUE, "Almiah_Alkhafia_LG15", 9000, 1);
        m_TargetAirfields.Add(ARMY_BLUE, "el_Adem_No1_LG144", 9000, 1);
        m_TargetAirfields.Add(ARMY_BLUE, "Gazala_No2_LG150", 9000, 1);
        m_TargetAirfields.Add(ARMY_BLUE, "Tobruk_No5", 9000, 1);
        m_TargetAirfields.Add(ARMY_BLUE, "Gambut_No5_West", 9000, 1);
        m_TargetAirfields.Add(ARMY_BLUE, "Sidi_Azeiz", 9000, 20);//denotes initial airport to attack and occupy
        m_TargetAirfields.Add(ARMY_BLUE, "Sidi_Rezegh_LG153", 9000, 1);
        */
    }


}
