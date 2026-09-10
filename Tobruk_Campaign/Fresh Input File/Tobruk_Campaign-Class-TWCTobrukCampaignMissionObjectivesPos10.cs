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



public class MissionPos10 : TWCTCMO
{
    public Mission msn;
    public Mission.MissionObjectives mmo;
    public maddox.game.IGamePlay gp;
    public override double score { get; set; }
    //public int? current_subclass; //was going to make this double but given that 
    private TWCTobrukCampaignMissionObjectives current_subclass; //was going to make this double but given that 
    //private SortedList<int, TWCTobrukCampaignMissionObjectives> MissionObjectives_subclasses = new SortedList<int, TWCTobrukCampaignMissionObjectives>();

    public string red_target_airfield = "Gasr el Abid South Airfield";
    public string blue_target_airfield = "Scegga No3 Airfield";

    public override string focus_airport_misfile_name { get; set; }

    public string bumrush_redred_misfile_namematch = "Tobruk_Campaign-Rush-Red-Gasr el Abid South Airfield";  //This is a "match" string; any .mis file in subdir Bumrushes matching this, like "*bumrush_redred_mismatch*.mis" maybe be picked for this bumrush file.  You MUST supply at least one .mis file matching this pattern but if you supply 2 or more then one will be randomly chosen
                               //redred means red won the initial mission & is trying to advance ot the airport; red is currently bumrushing that airport
    public string bumrush_redblue_misfile_namematch = "Tobruk_Campaign-Rush-Blue-Gasr el Abid South Airfield";  //This is a "match" string; any .mis file in subdir Bumrushes matching this
                                                                                                      //redblue means red won the initial mission & is trying to advance ot the airport; blue is currently bumrushing that airport
    public string bumrush_bluered_misfile_namematch = "Tobruk_Campaign-Rush-Red-Scegga No3 Airfield-1.mis";  //Scegga No3 is the airport Blue is trying to take over in this mission; this is the Red counterattack
    public string bumrush_blueblue_misfile_namematch = "Tobruk_Campaign-Rush-Blue-Scegga No3 Airfield-1.mis";  //Scegga No3 is the airport Blue is trying to take over in this mission; this is the Blue attack

    public override double leastScore { get; set; } //Every subclass MUST include this value, which is the least campaign score value for which it will be loaded (>=)
    public override double mostScore { get; set; }   //Every subclass MUST include this value, which is the greatest campaign score value for which it will be loaded (<)
                                            //Typically if the class if for say point value 10, you set it 5,15 so as to bracket the desired mission point value.
                                            //Or you set them -10,0 - 0,10 - 10,20 - 20-30, so that players much reach 10 points (for example) before that submission kicks in
                                            //Note that >= is used on the bottom and < on the top so that you can overlap as shown above & only one submission will be operative
                                            //HOWEVER nothing is checking that you have avoided overlapping.  So if you have -10,0 & -5,5 & 0,10 submission then, TWO of them
                                            //will be operative AT ONCE for score of -5 to 5.
                                            //When one side wins the Bumrush and turns the map, they are awarded 1000 player points.  1 "Score" point = 100 Player Points, so
                                            //Advancing 1000 Player Points means advancing 10 score point.  So think of each phase as being incremented by 10, like -40, -30, -20, -10, 0, 10, 20, 30, 40 etc

    //This will set up Tobruk-specific overall/general mission objectives for the overall mission, according to the current Campaign Score (score)
    //It will then pass the routines along at the appropriate points, and to the appropriate


    public MissionPos10(maddox.game.IGamePlay g, Mission m, Mission.MissionObjectives mo, double sc)
    //public TWCTobrukCampaignMissionObjectives(maddox.game.IGamePlay g, Mission m, Mission.MissionObjectives mo, double sc)
    //public MissionNeg10()
    {
        msn = m;
        mmo = mo;
        gp = g;
        score = sc;

        leastScore = 5;
        mostScore = 15;
        focus_airport_misfile_name = "Tobruk_Campaign-initairports-Gasr_el_abid-Skegga-No3.mis"; //this file must be found in subdir "FocusAirports";

        Console.WriteLine("Pos10 inited, {0} {1} {2} {3}", score, leastScore, mostScore, focus_airport_misfile_name);

    /*msn = base.msn;
    mmo = base.mmo;
    gp = base.gp;
    score = base.score; */
    //base.register_subclass(this); //so this doesn't work; different base class for each & every new subclass...

    //setup_subclasses();
    //current_subclass = null; //in future we could  have a sub-subclass?  But for now, no.

    //set_current_subclass();
    //mmo = msn.mission_objectives;

    //TWCTobrukCampaignMissionObjectives mission_neg10 = new MissionNeg10(g, m, mo, sc);
    //TWCTobrukCampaignMissionObjectives mission_0 = new Mission0(g, m, mo, sc);
    //TWCTobrukCampaignMissionObjectives mission_pos10 = new MissionPos10(g, m, mo, sc);

    //RadarPositionTriggersSetup(addNewOnly: false); //We'll let mmo call this as it might need to be called specifically @ certain times.
    //MissionObjectiveTriggersSetup(addNewOnly: false); //We'll let mmo call this as it might need to be called specifically @ certain times.
    //FlakDictionariesSetup();
    }

    public override void OverrideCampaignTurnMapRequirements()
    {
        /*
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




    //public List<SubMissionClassInfo> MissionObjectives_subclasses = new List<int, SubMissionClassInfo>()  { };

    //public List<TWCTobrukCampaignMissionObjectives> TWCMissionObjectives_subclasses = new List<TWCTobrukCampaignMissionObjectives>();

    //So every subclass must register itself during initilization via
    //  base.register_subclass (this);  The numbers are the LEAST and GREATEST campaign score for which that subclass should be activated.
    //Note that 

    /*
    private void register_subclass(TWCTobrukCampaignMissionObjectives twcmo)
    {
        TWCMissionObjectives_subclasses.Add(twcmo);
    } 
    */

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
        //    mmo.addRadarPointArea("Dunkirk Freya RadaRPA",           "DuRN", 2, 1, 2, "RTarget38RPA",  "", 1000, 4, 77, 339793, 232797,  100, 20000, false, 35, "", add);
        mmo.addRadarPointArea("Herderlot-Plage Freya Radar", "HePR", 2, 4, 2, "RTarget39RPA", "", 1000, 4, 264882, 178115, 85, 20000, false, 35, "", add); //Mission in mission file
        mmo.addRadarPointArea("Berck Freya Radar", "BrkR", 2, 4, 2, "RTarget40RPA", "", 1000, 4, 263234, 153713, 85, 20000, false, 5, "", add); //Mission in mission file
        //mmo.addRadarPointArea("Radar Dieppe", "DieR", 2, 4, 2, "RTarget41RPA",  "", 1000, 4, 232727, 103248, 85, 20000, false, 5, "", add); //This trigger exists in the .mis file but I don't believe there are any actual radars/stationaries in this area at all
        mmo.addRadarPointArea("Radar Le Treport", "TreR", 2, 4, 2, "RTarget42RPA", "", 1000, 4, 250599, 116531, 85, 20000, false, 15, "", add); // Mission in mission file
        mmo.addRadarPointArea("Radar Somme River", "SomR", 2, 4, 2, "RTarget43RPA", "", 1000, 4, 260798, 131885, 85, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadarPointArea("Radar AMBETEUSE", "AmbR", 2, 4, 2, "RTarget44RPA", "", 1000, 4, 266788, 197956, 85, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadarPointArea("Radar BOULOGNE", "BlgR", 2, 4, 2, "RTarget45RPA", "", 1000, 4, 264494, 188674, 85, 20000, false, 35, "", add); //Mission in mission file           
        mmo.addRadarPointArea("Radar Le Touquet", "L2kR", 2, 4, 2, "RTarget46RPA", "", 1000, 4, 265307, 171427, 85, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadarPointArea("Radar Dieppe", "FreR", 2, 5, 2, "RTarget47RPA", "", 1000, 4, 232580, 103325, 85, 20000, false, 15, "", add); //Mission in mission file
        mmo.addRadarPointArea("Veulettes-sur-Mer Radar", "VeuR", 2, 5, 2, "RTarget48RPA", "", 1000, 4, 195165, 93441, 85, 20000, false, 5, "", add);//Mission in mission file
        mmo.addRadarPointArea("Le Havre Freya Radar", "LhvR", 2, 5, 2, "RTarget49RPA", "", 1000, 4, 157636, 60683, 85, 20000, false, 15, "", add);//Mission in mission file
        mmo.addRadarPointArea("Ouistreham Freya Radar", "OuiR", 2, 5, 2, "RTarget50RPA", "", 1000, 4, 135205, 29918, 85, 20000, false, 15, "", add);// Mission in mission file
        mmo.addRadarPointArea("Bayeux Beach Freya Radar", "BayR", 2, 5, 2, "RTarget51RPA", "", 1000, 4, 104279, 36659, 85, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadarPointArea("Beauguillot Beach Freya Radar", "BchR", 2, 5, 2, "RTarget52RPA", "", 1000, 4, 65364, 43580, 85, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadarPointArea("Radar Tatihou", "TatR", 2, 5, 2, "RTarget53RPA", "", 1000, 4, 60453, 63873, 85, 30000, false, 5, "", add); //Mission in mission file
        mmo.addRadarPointArea("Radar Querqueville", "QueR", 2, 5, 2, "RTarget54RPA", "", 1000, 4, 17036, 77666, 85, 30000, false, 15, "", add); // Mission in mission file
        mmo.addRadarPointArea("Local Radar Wimereux", "", 2, 2, 1.2, "RWimereuxRadar", "", 85, 1, 266719, 193028, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Local Radar Etaples", "", 2, 2, 1.2, "REtaplesRadar", "", 85, 1, 264833, 166251, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Local Radar Calais", "", 2, 2, 1.2, "RCalaisRadar", "", 85, 1, 288993, 218087, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Local Radar Dunkirk", "", 2, 2, 1.2, "RDunkirkRadar", "", 85, 1, 327244, 227587, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Local Radar Marquise", "", 2, 2, 1.2, "RMarquiseRadar", "", 85, 1, 274709, 201374, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Local Radar Tripod", "", 2, 2, 1.2, "RTripodRadar", "", 85, 1, 277579, 208243, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Local Radar Wailly-Beaucamp", "", 2, 2, 1.2, "RWaillyBeaucampRadar", "", 85, 1, 277423, 156268, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Ramsgate Local Radar", "", 1, 2, 1.2, "RRamsgateRadar", "", 85, 1, 253029, 259510, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Shoreham Local Radar", "", 1, 2, 1.2, "RShorehamRadar", "", 85, 1, 134312, 197580, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Dover Local Radar", "", 1, 2, 1.2, "RDoverRadar", "", 85, 1, 244253, 233078, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Eastchurch Local Radar", "", 1, 2, 1.2, "REastChurchRadar", "", 85, 1, 218306, 261855, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Bexhill Local Radar", "", 1, 2, 1.2, "RBexhillRadar", "", 85, 1, 190722, 201588, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Bembridge-Sandown Local Radar", "", 1, 2, 1.2, "RBembridgeSandownRadar", "", 85, 1, 72109, 180094, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Gosport Local Radar", "", 1, 2, 1.2, "RGosportRadar", "", 85, 1, 77878, 191392, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Deal Local Radar", "", 1, 2, 1.2, "RDealRadar", "", 85, 1, 250530, 244563, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Canterbury Local Radar", "", 1, 2, 1.2, "RCanterburyRadar", "", 85, 1, 236454, 248239, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        mmo.addRadarPointArea("Gravesend Local Radar", "", 1, 2, 1.2, "RGravesendRadar", "", 85, 1, 182026, 266767, 150, 5000, false, 33, "", add); //in -initsubmission-miniRadars.mis
        */

        /*
        mmo.addRadar("Oye Plage Freya Radar", "OypR", 2, 4, 2, "RTarget28R", "TGroundDestroyed", 61, 294183, 219444, 100, 20000, false, 35, "", add);
        mmo.addRadar("Coquelles Freya Radar", "CoqR", 2, 4, 2, "RTarget29R", "TGroundDestroyed", 63, 276566, 214150, 100, 20000, false, 35, "", add);
        mmo.addRadar("Dunkirk Radar #2", "DuRN", 2, 4, 2, "RTarget30R", "TGroundDestroyed", 77, 341887, 232695, 100, 20000, false, 35, "", add);
        //    mmo.addRadar("Dunkirk Freya Radar",           "DuRN", 2, 1, 2, "RTarget38R", "TGroundDestroyed", 77, 339793, 232797,  100, 20000, false, 35, "", add);
        mmo.addRadar("Herderlot-Plage Freya Radar", "HePR", 2, 4, 2, "RTarget39R", "TGroundDestroyed", 85, 264882, 178115, 100, 20000, false, 35, "", add); //Mission in mission file
        mmo.addRadar("Berck Freya Radar", "BrkR", 2, 4, 2, "RTarget40R", "TGroundDestroyed", 86, 263234, 153713, 100, 20000, false, 5, "", add); //Mission in mission file
        //mmo.addRadar("Radar Dieppe", "DieR", 2, 4, 2, "RTarget41R", "TGroundDestroyed", 85, 232727, 103248, 100, 20000, false, 5, "", add); //This trigger exists in the .mis file but I don't believe there are any actual radars/stationaries in this area at all
        mmo.addRadar("Radar Le Treport", "TreR", 2, 4, 2, "RTarget42R", "TGroundDestroyed", 86, 250599, 116531, 50, 20000, false, 15, "", add); // Mission in mission file
        mmo.addRadar("Radar Somme River", "SomR", 2, 4, 2, "RTarget43R", "TGroundDestroyed", 86, 260798, 131885, 50, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadar("Radar AMBETEUSE", "AmbR", 2, 4, 2, "RTarget44R", "TGroundDestroyed", 86, 266788, 197956, 50, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadar("Radar BOULOGNE", "BlgR", 2, 4, 2, "RTarget45R", "TGroundDestroyed", 85, 264494, 188674, 50, 20000, false, 35, "", add); //Mission in mission file           
        mmo.addRadar("Radar Le Touquet", "L2kR", 2, 4, 2, "RTarget46R", "TGroundDestroyed", 66, 265307, 171427, 50, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadar("Radar Dieppe", "FreR", 2, 4, 2, "RTarget47R", "TGroundDestroyed", 99, 232580, 103325, 50, 20000, false, 15, "", add); //Mission in mission file
        mmo.addRadar("Veulettes-sur-Mer Radar", "VeuR", 2, 4, 2, "RTarget48R", "TGroundDestroyed", 100, 195165, 93441, 50, 20000, false, 5, "", add);//Mission in mission file
        mmo.addRadar("Le Havre Freya Radar", "LhvR", 2, 4, 2, "RTarget49R", "TGroundDestroyed", 100, 157636, 60683, 50, 20000, false, 15, "", add);//Mission in mission file
        mmo.addRadar("Ouistreham Freya Radar", "OuiR", 2, 4, 2, "RTarget50R", "TGroundDestroyed", 100, 135205, 29918, 50, 20000, false, 15, "", add);// Mission in mission file
        mmo.addRadar("Bayeux Beach Freya Radar", "BayR", 2, 4, 2, "RTarget51R", "TGroundDestroyed", 100, 104279, 36659, 50, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadar("Beauguillot Beach Freya Radar", "BchR", 2, 4, 2, "RTarget52R", "TGroundDestroyed", 100, 65364, 43580, 50, 20000, false, 5, "", add); //Mission in mission file
        mmo.addRadar("Radar Tatihou", "TatR", 2, 4, 2, "RTarget53R", "TGroundDestroyed", 77, 60453, 63873, 50, 30000, false, 5, "", add); //Mission in mission file
        mmo.addRadar("Radar Querqueville", "QueR", 2, 4, 2, "RTarget54R", "TGroundDestroyed", 100, 17036, 77666, 50, 30000, false, 15, "", add); // Mission in mission file
        */

        /*
        BTarget15R TGroundDestroyed 75 248739 253036 200
        BTarget16R TGroundDestroyed 75 249454 247913 200
        BTarget17R TGroundDestroyed 75 246777 235751 200
        BTarget18R TGroundDestroyed 75 212973 220079 200
        BTarget19R TGroundDestroyed 50 221278 214167 200
        BTarget20R TGroundDestroyed 75 178778 197288 200
        BTarget21R TGroundDestroyed 76 123384 196295 200
        BTarget22R TGroundDestroyed 75 70423 171706 200
        BTarget28 TGroundDestroyed 61 180207 288435 200
RTarget28R TGroundDestroyed 61 294183 219444 50
RTarget29R TGroundDestroyed 63 276566 214150 50
RTarget30R TGroundDestroyed 77 341887 232695 100
RTarget38R TGroundDestroyed 85 341866 232710 50
RTarget39R TGroundDestroyed 85 276567 214150 50
RTarget40R TGroundDestroyed 86 263234 153713 50
RTarget41R TGroundDestroyed 85 232576 103318 50
RTarget42R TGroundDestroyed 86 250599 116531 50
RTarget43R TGroundDestroyed 86 262560 133020 50
RTarget44R TGroundDestroyed 86 266788 197956 50
RTarget45R TGroundDestroyed 85 264266 188554 50
RTarget46R TGroundDestroyed 66 266625 169936 50
RTarget47R TGroundDestroyed 99 185931 88085 50
RTarget48R TGroundDestroyed 100 195165 93441 50
RTarget49R TGroundDestroyed 100 157636 60683 50
RTarget50R TGroundDestroyed 100 135205 29918 50
RTarget51R TGroundDestroyed 100 103641 36893 50
RTarget52R TGroundDestroyed 100 65637 44013 50
RTarget53R TGroundDestroyed 77 60453 63873 50
RTarget54R TGroundDestroyed 100 17036 77666 50


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

        bool add = addNewOnly;

        //Format: mmo.addTrigger(M.MO_ObjectiveType.Building (Aircraft, airport, etc), "Name,                      OwnerArmy,Points,ID,TriggerType,PercRequired,XLoc,YLoc,Radius,IsPrimaryTarget,IsPrimaryTargetWeight,TimeToRepairIfDestroyed_hours,Comment "");
        //PercRequired doesn't actually do anything because the perc required is set in the TRIGGER in the .mis file.  However if you accurately record here the same
        //percent to kill value in the .mis file we can do interesting/helpful things with it here.

        //BLUE TARGETS

        // mmo.addTrigger(M.MO_ObjectiveType.Building, "Coal mines in Normandy", "Shor", 1, 3, "BTargNormandyMiningCenter", "TGroundDestroyed", 50, 67510, 26083, 50, false, 100, 24, "", add);			
        //mmo.addTrigger(M.MO_ObjectiveType.Building, "Shoreham Submarine Base", "", 1, 3, "BTargShorehamSubmarineBase", "TGroundDestroyed", 10, 137054, 198034, 50, false, 120, 24, "", add);           

        //RED & BLUE CONVOY PRIMARY OBJECTIVES.
        //These are in sub-missions and includes Oskar's new code to pass the trigger on to the main mission

        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Tobruk-Gasr Resupply Convoy", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-RTobrukGasrResupplyConvoy-objective.mis", "1006_Chief", 2, 5, "RTobrukGasrResupplyConvoy", "TGroupDestroyed", 100, 197907, 95422, 100, false, 200, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Alsmar-Gasr Resupply Convoy", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-RAlsmarGasrResupplyConvoy-objective.mis", "1007_Chief", 2, 5, "RAlsmarGasrResupplyConvoy", "TGroupDestroyed", 100, 150983, 37668, 100, false, 200, 24, "", add);  //g            

        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Sidi-Scegga Resupply Convoy", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-BSidiSceggaResupplyConvoy-objective.mis", "1010_Chief", 1, 5, "BSidiSceggaResupplyConvoy", "TGroupDestroyed", 100, 327607, 126528, 100, false, 200, 24, "", add);  //g
        mmo.addTrigger(M.MO_ObjectiveType.Convoy, "Siwi-Scegga Resupply Convoy", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-BSiwiSceggaResupplyConvoy-objective.mis", "1008_Chief", 1, 5, "RSiwiSceggaResupplyConvoy", "TGroupDestroyed", 100, 286184, 35204, 100, false, 200, 24, "", add);  //g 



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

        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Derna Fuel", "Dern", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-DernaFuel-objective.mis", 2, 5, "RTargDernaFuel", 32277, 264622, 200, 150, 1800, 4, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Tobruk Docks Fuel/Ammo Dump", "Tobr", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-TobrukDockFuel2-objective.mis", 2, 5, "RTargTobrukFuel", 160306, 186548, 200, 150, 1800, 2, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Coastal Fuel Dump", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-GermanFuelDump-objective.mis", 2, 5, "RCoastalFuel", 127599, 197411, 200, 150, 1800, 2, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Armor Camp", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Red-GermanArmorCamp-objective.mis", 2, 5, "RArmorCamp", 195633, 122432, 125, 100, 1800, 8, 200, 48, false, true, 3, 7, "", add);


        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Sidi Barrani Fuel Dump", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-SidiBarraniFuel.mis", 1, 5, "BSidiBarraniFuelDump", 346862, 135133, 150, 125, 1800, 8, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Scegga Armor Camp", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-SceggaBritishArmorCamp-objective.mis", 1, 5, "BSceggaArmorCamp", 263026, 51966, 150, 125, 1800, 8, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Habata Armor Camp", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-HabataBritishArmorCamp-objective.mis", 1, 5, "BHabataArmorCamp", 301770, 89683, 150, 125, 1800, 8, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "BuqBuq Armor Camp", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-BuqBuqBritishArmorCamp-objective.mis", 1, 5, "BBuqBuqArmorCamp", 306575, 116772, 150, 125, 1800, 8, 200, 48, false, true, 3, 7, "", add);
        mmo.addPointArea(M.MO_ObjectiveType.MilitaryArea, "Awdyat Fuel Dump", "", "PrimaryObjectives/Tobruk_Campaign-LOADONCALL-Blue-AwdyatFuel.mis", 1, 5, "BAwdyatFuelDump", 235348, 27829, 150, 125, 1800, 8, 200, 48, false, true, 3, 7, "", add);



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
				 	
				Oye Plage Freya Radar",    	
				Coquelles Freya Radar",    
                Dunkirk Freya Radar",      
                Herderlot-Plage Freya Radar
                Berck Freya Radar",        
                Radar Dieppee",            
                Radar Le Treport",         
                Radar Somme River",        
                Radar AMBETEUSE",          
                Radar BOULOGNE",           
                Radar Le Touquet",                      
                Veulettes-sur-Mer Radar",  
                Le Havre Freya Radar",     
                Ouistreham Freya Radar",   
                Bayeux Beach Freya Radar", 
                Beauguillot Beach Freya Rad
				Radar Tatihou",            
				Radar Querqueville", 
added Rouen Flak 
				*/	
					
					
					
					
					
					
					
					
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

        msn.gpLogServerAndLog(null, "***Starting Focus Airport/Bumrush setup", null);


        if (!msn.MissionObjectivesList.ContainsKey(red_target_airfield + "_spawn")) //.Replace(' ', '_')
                msn.gpLogServerAndLog(null, "**************WARNING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! A target Airport does not exist, check FocusBumrushSetup: " + red_target_airfield, null);

        if (!msn.MissionObjectivesList.ContainsKey(blue_target_airfield + "_spawn")) //.Replace(' ', '_')
            msn.gpLogServerAndLog(null, "**************WARNING!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! A target Airport does not exist, check FocusBumrushSetup: " + blue_target_airfield, null);

        msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName = red_target_airfield + "_spawn";
        msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName = blue_target_airfield + "_spawn";

        msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushAirportName = red_target_airfield;
        msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushAirportName = blue_target_airfield;


        if (!msn.MissionObjectivesList.ContainsKey(msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName))
        {
            msn.gpLogServerAndLog(null, "", null);
            msn.gpLogServerAndLog(null, "*********MAJOR STARTUP ERROR!!!! RED Target Airport does not exist as an airfield in the FocusAirports/*.mis file!!!!! Perhaps it is misspelled there? Exiting....", null);
            msn.gpLogServerAndLog(null, "", null);
            //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");  //doesn't work for some unknown reason//!????
            System.Environment.Exit(1);
        }
        else
        {
            msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective = msn.MissionObjectivesList[msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjectiveName];
            M.MissionObjective mo = msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective;
            mo.IsPrimaryTarget = true;
            mo.IsFocus = true;
            mo.PrimaryTargetWeight = 200;
            mo.Points = 30; // 6 primary objects * 5 + the main airport makes 60 points required to start bumrush
            msn.gpLogServerAndLog(null, "RED Primary Target Airport is " + msn.MO_BRBumrushInfo[ArmiesE.Red].BumrushObjective.AirfieldName, null);

        }

        if (!msn.MissionObjectivesList.ContainsKey(msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjectiveName))
        {
            msn.gpLogServerAndLog(null, "", null);
            msn.gpLogServerAndLog(null, "*********MAJOR STARTUP ERROR!!!! BLUE Target Airport does not exist as an airfield in the FocusAirports/*.mis file!!!!! Perhaps it is misspelled there? Exiting....", null);
            msn.gpLogServerAndLog(null, "", null);
            //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");
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

            msn.gpLogServerAndLog(null, "BLUE Primary Target Airport is " + msn.MO_BRBumrushInfo[ArmiesE.Blue].BumrushObjective.AirfieldName, null);
        }

        //Check if the files actually exist.  Does NOT actually load them, just checks if they are actually there with correct name!!!!
        bool blue_red = msn.LoadRandomSubmission(fileID: bumrush_bluered_misfile_namematch, subdir: "Bumrushes", check: true);
        bool red_red = msn.LoadRandomSubmission(fileID: bumrush_redred_misfile_namematch, subdir: "Bumrushes", check: true);
        bool blue_blue = msn.LoadRandomSubmission(fileID: bumrush_blueblue_misfile_namematch, subdir: "Bumrushes", check: true);
        bool red_blue = msn.LoadRandomSubmission(fileID: bumrush_redblue_misfile_namematch, subdir: "Bumrushes", check: true);

        if (!blue_red || !red_red || !blue_blue || !red_blue)
        {
            msn.gpLogServerAndLog(null, "", null);
            msn.gpLogServerAndLog(null, "*********MAJOR STARTUP ERROR!!!! One of the REQUIRED bumrush .mis files in subdir Bumrushes/ is MISSING or MISNAMED!!!!!! Exiting....", null);
            msn.gpLogServerAndLog(null, "", null);
            if (!blue_red) msn.gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + bumrush_bluered_misfile_namematch, null);
            if (!red_red) msn.gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + bumrush_redred_misfile_namematch, null);
            if (!blue_blue) msn.gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + bumrush_blueblue_misfile_namematch, null);
            if (!red_blue) msn.gpLogServerAndLog(null, "*********MISSING OR MISNAMED FILE: Bumrushes/" + bumrush_redblue_misfile_namematch, null);
            msn.gpLogServerAndLog(null, "", null);
            //(GamePlay as GameDef).gameInterface.CmdExec("battle stop");
            System.Environment.Exit(1);

        }
        else
        {
            msn.gpLogServerAndLog(null, "All needed BUMRUSH .mis files for both Blue & Red Primary Target Airports are in place.", null);
        }

    }

    public override void ReadInitialFocusAirportSubmission()
    {
        Console.WriteLine("ReadInitFocusAirport reading {0}", focus_airport_misfile_name);
        msn.ReadInitialSubmissions(focus_airport_misfile_name, 0, 0, subdir: "FocusAirports"); //The "focus airports" file(s) for the TOBRUK mission 2020-08
    }
}
