#define DEBUG  
#define TRACE  

//$reference parts/core/CloDMissionCommunicator.dll
////$reference parts/core/CLOD_Extensions.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll
///$reference Microsoft.csharp.dll 
//$reference WPF/PresentationFramework.dll
//$reference WPF/PresentationCore.dll
//$reference WPF/WindowsBase.dll
//$reference System.Xaml.dll
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.ComponentModel;
using System.Text.RegularExpressions;
//using System.Data;
//using System.Core;
using System.Linq;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;
using part;
using Ini;
//using TF_Extensions;  //not working for now?
using TWCComms;

public class SupplyMission : AMission, ISupplyMission
{

    //all these values are pulled over from -main.cs so they are consistent locations
    string MISSION_ID { get; set; }
    string CAMPAIGN_ID { get; set; }
    string SERVER_ID { get; set; }
    string SERVER_ID_SHORT { get; set; }
    string CLOD_PATH { get; set; }
    string FILE_PATH { get; set; }
    string STATS_FULL_PATH { get; set; }
    string STATSCS_FULL_PATH { get; set; }
    int MAX_AC_IN_INVENTORY { get; set; }

    public Mission mainmission;
    public IMainMission TWCMainMission;
    public IStatsMission TWCStatsMission;
    //public AIniFile TWCIniFile;
    public Dictionary<ArmiesE, Dictionary<string, double>> AircraftSupply { get; set; }
    public Dictionary<ArmiesE, Dictionary<string, double>> AircraftIncrease { get; set; }
    public HashSet<AiActor> aircraftCheckedOut { get; set; }
    public Dictionary<AiActor, Tuple<int, string, string, DateTime>> aircraftCheckedOutInfo { get; set; } //Info about each a/c that is checked out <Army, Pilot name(s), Aircraft Type, time checked out>
    public Dictionary<AiActor, Tuple<Player, string, double, double, DateTime, DateTime>> damagedAircraft { get; set; } //Info about each a/c that is on the damaged list: Player, player name, damage amount (0-1), hours to repair, time damaged UTC, time repaired UTC
    public HashSet<AiActor> aircraftCheckedIn { get; set; }//set of AiActor, to guarantee each Actor checked IN once only
    public HashSet <AiActor> aircraftCheckedInButLaterKilled { get; set; }  //set of AiActor, to guarantee actors which were first reported AOK but later turned out to be killed, are able to be killed later & removed from the active a/c list, but ONCE ONLY

    //public string SupplyFilename { get; set; }
    Ini.SupplyIniFile iniFile;
    string supplySuffix { get; set; }

    static public List<string> ArmiesL = new List<string>() { "None", "Red", "Blue" };
    //public enum ArmiesE { None, Red, Blue };


    //initializer method
    public SupplyMission(Mission msn)
    {
        mainmission = msn;
        TWCComms.Communicator.Instance.Supply = (ISupplyMission)this; //allows -stats.cs to access this instance of Mission
        TWCMainMission = TWCComms.Communicator.Instance.Main;
        TWCStatsMission = TWCComms.Communicator.Instance.Stats;
        //TWCIniFile = TWCComms.Communicator.Instance.Ini;
        //if (TWCMainMission == null) Console.WriteLine("TWC Supply: BIG ERROR - can't connect to -main, won't be able to run");
        //else {
        MISSION_ID = mainmission.MISSION_ID;
        CAMPAIGN_ID = mainmission.CAMPAIGN_ID;
        SERVER_ID = mainmission.SERVER_ID;
        SERVER_ID_SHORT = mainmission.SERVER_ID_SHORT;
        CLOD_PATH = mainmission.CLOD_PATH;
        FILE_PATH = mainmission.FILE_PATH;
        STATS_FULL_PATH = mainmission.STATS_FULL_PATH;
        STATSCS_FULL_PATH = mainmission.STATSCS_FULL_PATH;
        Console.WriteLine("Supp. Start: {0} {1} {2} {3} {4}", MISSION_ID, CAMPAIGN_ID, SERVER_ID, SERVER_ID_SHORT, CLOD_PATH);
        //}
        AircraftSupply = new Dictionary<ArmiesE, Dictionary<string, double>>();
        AircraftIncrease = new Dictionary<ArmiesE, Dictionary<string, double>>();
        MAX_AC_IN_INVENTORY = 250;
        //This hashset checking is a failsafe & prevents any aircraft from being checked in or out more than once
        //This prevents things like a player slipping into an already-existing aircraft (which does not do a "check out" because check outs
        //are done only when the aircraft is created with a player in it) but then leaves it, checking it back in & getting an extra aircraft
        //Also, we can warrantee that no aircraft is checked back in unless it was checked out first.
        aircraftCheckedOut = new HashSet<AiActor>(); //set of AiActor, to guarantee each Actor checked out ONCE ONLY
        aircraftCheckedOutInfo = new Dictionary<AiActor, Tuple<int, string, string, DateTime>>(); //Info about each a/c that is checked out <Army, Pilot name(s), Aircraft Type>
        damagedAircraft = new Dictionary<AiActor, Tuple<Player, string, double, double, DateTime, DateTime>>(); //Info about each a/c that is on the damaged list: Player, player name, damage amount (0-1), hours to repair, time damaged UTC, time repaired UTC
        aircraftCheckedIn = new HashSet<AiActor>(); //set of AiActor, to guarantee each Actor checked IN once only
        aircraftCheckedInButLaterKilled = new HashSet<AiActor>(); //set of AiActor, to guarantee actors which were first reported AOK but later turned out to be killed, are able to be killed later & removed from the active a/c list, but ONCE ONLY

        supplySuffix = "_supply";


    }
    public override void Inited()
    {
		base.Inited();
        ReadSupply(supplySuffix);
        SaveSupplyRecursive(true);
        Console.WriteLine(DisplayNumberOfAvailablePlanes(0, null, false).Replace(Environment.NewLine, ", "));

    }

    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        //Console.WriteLine("-supply.cs OnMissionLoaded {0} {1} ", missionNumber, MissionNumber);

        if (missionNumber != MissionNumber) return; //only do this when this particular mission is loaded.

        Console.WriteLine("-supply.cs successfully loaded");



        if (GamePlay != null && GamePlay is GameDef)
        {
            //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
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



    /// <summary> Brent this is Kodiaks old code to limit aircraft on dedi server.......
    /// This Dictionary contains the InternalTypeName of the limited actor and the number of available actors 
    /// </summary> this should limit the aicraft initially then as war progresses 
    /// more supply can be added to dictionary as needed ie supply convoys aircraft production etc
    // menu can list available aircraft for each side or summary can list aircraft left in stats
    /// <remarks>Actors which are not in List are not limited</remarks>
    ///    

    //Below values are to set INITIAL DEFAULTS and also AVAILABLE AIRCRAFT with their initial quantities.
    //To add a new aircraft, you MUST add it below in the .cs
    //To edit values after the first run of a new campaign you'll need to edit the .ini file
    // - You can edit INCREASE values in the .ini just about any time. The .cs will pick up new values whenever a new mission starts
    // - To edit SUPPLY values, you'll have to turn the server off, edit, then restart.  SUPPLY values and continually updated and save throughout 
    //    the mission, so whatever you save to the .ini file will be overwritten by new values periodically as long as the mission is running
    // - To add a new AIRCRAFT TYPE, add the initial entries with EXACT name, initial QUANTITY and desired INCREASE below, then re-start the server.
    //    After that initial run, edit values in the .ini file as described above.
    // - Note that aircraft names must be EXACT.  A space before OR after OR in the middle OR any other similar small deviation will result in malfunction.

    /**************************************************************************
     * 
     * AIRCRAFT DEFAULT/INITIAL ****QUANTITY**** VALUES
     * 
     * 
     * These are initial values and will fluctuate over time as aircraft are flown, crashed, shot down, wasted, etc,
     * and also increased per the INCREASE amounts specified in the INCREASE dictionary, with additional
     * variations introduced at the end of each mission based on mission performance (in -main.cs section SaveMapState())
     * and/or admin preference.
     * 
     * 
     **************************************************************************/
    public Dictionary<ArmiesE, Dictionary<string, double>> AircraftSupplyDefault = new Dictionary<ArmiesE, Dictionary<string, double>>
    {
        { ArmiesE.Red, new Dictionary<string,double>() {
        //Keep in mind that /cover will require 25 or 50 or 100
        //or whatever amount of stock in supply before those
        //a/c will show up as available under /cover

        // {bob."aircraft as known to game name",number available},
		
        {"bob:Aircraft.BeaufighterMkIF", 22},
        {"bob:Aircraft.BeaufighterMkINF",10},
        {"tobruk:Aircraft.BeaufighterMkIF_Late", 30},
        {"tobruk:Aircraft.BeaufighterMkINF_Late", 42},
        {"tobruk:Aircraft.BeaufighterMkIC", 93},
        //{"tobruk:Aircraft.BeaufighterMkIC_Trop", 0},
        //{"tobruk:Aircraft.BeaufighterMkIF_Late_Trop", 0},
        //{"tobruk:Aircraft.BeaufighterMkINF_Late_Trop", 0},
        {"bob:Aircraft.DefiantMkI",90},        
        {"bob:Aircraft.SunderlandMkI",90},
        {"bob:Aircraft.WalrusMkI",20}, //seaplane, biplane, has 2X250lb depth charges
        {"bob:Aircraft.AnsonMkI",90},        
        {"bob:Aircraft.BlenheimMkI",90},
        {"bob:Aircraft.BlenheimMkIF",90},
        {"bob:Aircraft.BlenheimMkINF",90},
        {"bob:Aircraft.BlenheimMkIV",90},
        {"bob:Aircraft.BlenheimMkIVF",50},
        {"bob:Aircraft.BlenheimMkIVF_Late",50},
        {"bob:Aircraft.BlenheimMkIVNF",50},
        {"bob:Aircraft.BlenheimMkIVNF_Late",90},
        {"bob:Aircraft.BlenheimMkIV_Late",90},
        //{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below
        {"bob:Aircraft.DH82A-2",0},
        {"bob:Aircraft.HurricaneMkI",50},
        {"bob:Aircraft.HurricaneMkI_100oct",50},
        {"bob:Aircraft.HurricaneMkI_100oct-NF",50},
        {"bob:Aircraft.HurricaneMkI_dH5-20",50},
        {"bob:Aircraft.HurricaneMkI_dH5-20_100oct",50},
        {"bob:Aircraft.HurricaneMkI_FB",50},
        {"bob:Aircraft.SpitfireMkI",50},
        {"bob:Aircraft.SpitfireMkIa",50},
        {"bob:Aircraft.SpitfireMkIa_100oct",50},
        {"bob:Aircraft.SpitfireMkI_100oct",50},
        {"bob:Aircraft.SpitfireMkIIa",6},
        {"bob:Aircraft.WellingtonMkIc",100}, /*bob:Aircraft.WellingtonMkIc=300.647357579865 */

        { "tobruk:Aircraft.GladiatorMkII_trop", 32},
            //{"tobruk:Aircraft.HurricaneMkI_FB-Trop", 0},
            {"tobruk:Aircraft.HurricaneMkIIa", 32},
            //{"tobruk:Aircraft.HurricaneMkIIaTrop", 0},
            {"tobruk:Aircraft.HurricaneMkIIb", 92},
            {"tobruk:Aircraft.HurricaneMkIIb-Late", 92},
            //{"tobruk:Aircraft.HurricaneMkIIbTrop", 0},
            //{"tobruk:Aircraft.HurricaneMkIIbTrop-Late", 0},
            {"tobruk:Aircraft.HurricaneMkIIc", 32},
            {"tobruk:Aircraft.HurricaneMkIIc-Late", 32},
            //{"tobruk:Aircraft.HurricaneMkIIc-Trop", 0},
            //{"tobruk:Aircraft.HurricaneMkIIc-Trop-Late", 0},
            {"tobruk:Aircraft.HurricaneMkIId", 32},
            //{"tobruk:Aircraft.HurricaneMkIId-Trop", 0},
            {"tobruk:Aircraft.KittyhawkMkIA", 5},
            //{"tobruk:Aircraft.KittyhawkMkIA-Trop", 0},
            {"tobruk:Aircraft.MartletMkIII", 5},
            //{"tobruk:Aircraft.MartletMkIII_Trop", 0},
            {"tobruk:Aircraft.SpitfireMkIIb", 32},
            {"tobruk:Aircraft.SpitfireMkVa", 28},
            {"tobruk:Aircraft.SpitfireMkVb", 20},
            {"tobruk:Aircraft.SpitfireMkVb-HF", 12},
            {"tobruk:Aircraft.SpitfireMkVb-HF-Late", 12},
            //{"tobruk:Aircraft.SpitfireMkVb-HF-Trop", 0},
            {"tobruk:Aircraft.SpitfireMkVbLate", 32},
            //{"tobruk:Aircraft.SpitfireMkVbTrop", 0},
            {"tobruk:Aircraft.TomahawkMkII", 8},
            {"tobruk:Aircraft.TomahawkMkII-Late", 8},
            //{"tobruk:Aircraft.TomahawkMkII-Late-Trop", 0},
            {"tobruk:Aircraft.TomahawkMkII-Trop", 0},
            //{"tobruk:Aircraft.WellingtonMkIa_trop", 0}, //This is a great plane BUT seems to cause the bug where ppl can't spawn into the server any more. 
            {"tobruk:Aircraft.WellingtonMkIc_Late", 95},
            //{"tobruk:Aircraft.WellingtonMkIc_Late_trop", 0}, //5.003 WON'T DROP BOMBS (more than 1 a mission) SO ELIMINATING IT FOR NOW. 5.017 SHOULD BE FIXED (2021/02), so re-adding it
            {"tobruk:Aircraft.WellingtonMkIc_t", 95},
            {"tobruk:Aircraft.WellingtonMkIc_Torpedo", 25},
            {"tobruk:Aircraft.WellingtonMkIc_Torpedo_Trop", 110},
            //{"tobruk:Aircraft.WellingtonMkIc_trop", 0}, //5.003 WON'T DROP BOMBS (more than 1 a mission) SO ELIMINATING IT FOR NOW  5.017 SHOULD BE FIXED (2021/02), so re-adding it

        } },

        /* 113 Days summer 2021 - full BLUE a/c list:
         *   Aircraft.Bf-108B-2
              Aircraft.Bf-109E-1
              Aircraft.Bf-109E-1B
              Aircraft.Bf-109E-3
              Aircraft.Bf-109E-3B
              Aircraft.Bf-109E-4
              Aircraft.Bf-109E-4_Late
              Aircraft.Bf-109E-4B
              Aircraft.Bf-109E-4B_Late
              Aircraft.Bf-109E-4N
              Aircraft.Bf-109E-4N_Late
              tobruk:Aircraft.Bf-109E-7
              tobruk:Aircraft.Bf-109E-7N
              tobruk:Aircraft.Bf-109E-7Z
              tobruk:Aircraft.Bf-109F-1
              tobruk:Aircraft.Bf-109F-2
              tobruk:Aircraft.Bf-109F-2_Late
              tobruk:Aircraft.Bf-109F-4
              tobruk:Aircraft.Bf-109F-4_Derated
              tobruk:Aircraft.Bf-109F-4Z  
              tobruk:Aircraft.D520_Serie1  
              Aircraft.Bf-110C-2
              Aircraft.Bf-110C-4
              Aircraft.Bf-110C-4B
              Aircraft.Bf-110C-4Late
              Aircraft.Bf-110C-4N
              Aircraft.Bf-110C-4-NJG
              Aircraft.Bf-110C-6
              Aircraft.Bf-110C-7
              Aircraft.BR-20M
              Aircraft.CR42
              Aircraft.G50
              Aircraft.He-111H-2
              tobruk:Aircraft.He-111H-6
              Aircraft.He-111P-2
              Aircraft.Ju-87B-2
              Aircraft.Ju-88A-1
              tobruk:Aircraft.Ju-88A-5
              tobruk:Aircraft.Ju-88A-5Late
              tobruk:Aircraft.Ju-88C-1
              tobruk:Aircraft.Ju-88C-2
              tobruk:Aircraft.Ju-88C-4
              tobruk:Aircraft.Ju-88C-4Late
              tobruk:Aircraft.Macchi-C202-SeriesIII
              tobruk:Aircraft.Macchi-C202-SeriesIII-AltoQuota  
              tobruk:Aircraft.Macchi-C202-SeriesVII
              tobruk:Aircraft.Macchi-C202-SeriesVII-AltoQuota

        */
        { ArmiesE.Blue, new Dictionary <string,double>(){
        {"bob:Aircraft.Bf-109E-1",100},
        {"bob:Aircraft.Bf-109E-1B",50},
        {"bob:Aircraft.Bf-109E-3",100},
        {"bob:Aircraft.Bf-109E-3B",40},
        {"bob:Aircraft.Bf-109E-4",50},
        {"bob:Aircraft.Bf-109E-4_Late",10},
        {"bob:Aircraft.Bf-110C-2",50},
        {"bob:Aircraft.Bf-110C-4",10},
        {"bob:Aircraft.Bf-110C-4-NJG",50},
        {"bob:Aircraft.Bf-110C-4B" ,100},
        {"bob:Aircraft.Bf-110C-4Late",10},
        {"bob:Aircraft.Bf-110C-4N",50},
        {"bob:Aircraft.Bf-110C-6",100},
        {"bob:Aircraft.Bf-110C-7",100},
        {"bob:Aircraft.BR-20M",10},	
        //{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below
        {"bob:Aircraft.DH82A-2",0},
        {"bob:Aircraft.G50",15},
        {"bob:Aircraft.He-111H-2",100},
        {"bob:Aircraft.He-111P-2",150},
        {"bob:Aircraft.Ju-87B-2",100},
        {"bob:Aircraft.Ju-88A-1",100},
        {"bob:Aircraft.Bf-109E-4B",35},
        {"bob:Aircraft.Bf-109E-4B_Late",35},
        {"bob:Aircraft.Bf-109E-4N",6},
        {"bob:Aircraft.Bf-109E-4N_Late",6},
        //{"bob:Aircraft.Do-17Z-1", 80}, //exists as an a/c but we haven't used it because it's so similar to Z-2, and blue has tons of AI bombers
        {"bob:Aircraft.Do-17Z-2", 80},
        {"bob:Aircraft.Do-215B-1", 80},

            //{"tobruk:Aircraft.Bf-108B-2_Trop",00},
            {"tobruk:Aircraft.Bf-109E-7",22},
            //{"tobruk:Aircraft.Bf-109E-7_Trop",0},
            {"tobruk:Aircraft.Bf-109E-7N",22},
            //{"tobruk:Aircraft.Bf-109E-7N_Trop",0},
            {"tobruk:Aircraft.Bf-109E-7Z",22},
            {"tobruk:Aircraft.Bf-109F-1",22},
            {"tobruk:Aircraft.Bf-109F-2",22},
            {"tobruk:Aircraft.Bf-109F-2_Late",22},
            //{"tobruk:Aircraft.Bf-109F-2_Trop",0},
            {"tobruk:Aircraft.Bf-109F-4",22},
            {"tobruk:Aircraft.Bf-109F-4_Derated",22},
            //{"tobruk:Aircraft.Bf-109F-4_Trop",0},
            //{"tobruk:Aircraft.Bf-109F-4_trop_Derated",0},
            {"tobruk:Aircraft.Bf-109F-4Z",35},
            //{"tobruk:Aircraft.Bf-109F-4Z_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-4B_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-4N-NJG_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-6_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-7_Trop",0},
            //{"tobruk:Aircraft.BR-20M_Trop",0},
            //{"tobruk:Aircraft.CR42_Trop",0},
            {"tobruk:Aircraft.D520_Serie1",52},
            //{"tobruk:Aircraft.D520_Serie1_Trop",0},
            //{"tobruk:Aircraft.DH82A_Trop",0}, //Tiger Moth/no weapons at all
            //{"tobruk:Aircraft.G50_Trop",0},
            //{"tobruk:Aircraft.He-111H-2_Trop",0},
            {"tobruk:Aircraft.He-111H-6",92},
            {"tobruk:Aircraft.He-111H-6_torpedo",92},  //fake way to make the H-6 torpedo version avail. for <cover
            //{"tobruk:Aircraft.He-111H-6_Trop",0},
            //{"tobruk:Aircraft.He-111H-6_Trop_torpedo",0},
            //{"tobruk:Aircraft.Ju-87B-2_Trop",0},
            {"tobruk:Aircraft.Ju-88A-5",92},
            //{"tobruk:Aircraft.Ju-88A-5_Trop",0},
            {"tobruk:Aircraft.Ju-88A-5Late",92},
            //{"tobruk:Aircraft.Ju-88A-5Late_Trop",0},
            {"tobruk:Aircraft.Ju-88C-1",92},
            {"tobruk:Aircraft.Ju-88C-2",92},
            //{"tobruk:Aircraft.Ju-88C-2_Trop",0},
            {"tobruk:Aircraft.Ju-88C-4",92},
            //{"tobruk:Aircraft.Ju-88C-4_Trop",0},
            {"tobruk:Aircraft.Ju-88C-4Late",92},
            //{"tobruk:Aircraft.Ju-88C-4Late_Trop",0},
            {"tobruk:Aircraft.Macchi-C202-SeriesIII",8},
            {"tobruk:Aircraft.Macchi-C202-SeriesIII-AltoQuota",8},
            {"tobruk:Aircraft.Macchi-C202-SeriesVII",8},
            {"tobruk:Aircraft.Macchi-C202-SeriesVII-AltoQuota",8},
        }
        }

        };

    /**************************************************************************
     * 
     * AIRCRAFT *********REGULAR INCREASE*******  VALUES
     * 
     * Thinks of these values as the amount the aircraft stock will increase, on average,
     * every ONE MISSION. So for example if missions last 6 hours, and SpitIIAs increase by 0.25 per mission,
     * that means Red will gain 4X0.25 or 1 new SpitIIA every day of real time, on average.
     * 
     * The idea is that the stock for each type of aircraft will increase by the amount
     * given below at the end of each mission.  If only a part mission is complete,
     * the proportional change is made to stock.
     * 
     * Additionally, rewards are given for points earned or winning a mission/turning the map. 
     * Points are reduced due to poor performance or losing the maps.  If one team turns the map,
     * that team gets 2x the usual stock increase while the other gets only 50% the usual increase.
     * 
     * Gather regular kill points, mission points, etc results in similar but much smaller adjustments.
     * 
     * Also, admins can enter a fiat INCREASE multiplier value for Blue or Red with chat commands 
     * <bluestock and <redstock.  These will be in addition to any normal mission multipliers.
     * 
     * Admins can give positive or negative multipliers.  For example <redstock -1 would negate the usual
     * +1 stock increase and result in no net gain.  <redstock -2 would be combined with the usual +1 gain to 
     * result in a net -1 gain.  <redstock 3 would combine with the usual 1 gain to result in 4X the usual
     * gain in aircraft stock from one mission, and so on.
     * 
     * 
     **************************************************************************/

    public Dictionary<ArmiesE, Dictionary<string, double>> AircraftIncreaseDefault = new Dictionary<ArmiesE, Dictionary<string, double>>
    {
        { ArmiesE.Red, new Dictionary<string,double>() {

        // {bob."aircraft as known to game name", a/c increase amount},
		
        {"bob:Aircraft.BeaufighterMkIF", 2},
        {"bob:Aircraft.BeaufighterMkINF",1},
        {"tobruk:Aircraft.BeaufighterMkIF_Late", 2},
        {"tobruk:Aircraft.BeaufighterMkINF_Late", 1},
        {"tobruk:Aircraft.BeaufighterMkIC", 2},
        //{"tobruk:Aircraft.BeaufighterMkIC_Trop", 0},
        //{"tobruk:Aircraft.BeaufighterMkIF_Late_Trop", 0},
        //{"tobruk:Aircraft.BeaufighterMkINF_Late_Trop", 0},
        {"bob:Aircraft.DefiantMkI",0.5},
        {"bob:Aircraft.SunderlandMkI",0.5},
        {"bob:Aircraft.WalrusMkI",0.5}, //seaplane, biplane, has 2X250lb depth charges
        {"bob:Aircraft.AnsonMkI",1},
        {"bob:Aircraft.BlenheimMkI",3},
        {"bob:Aircraft.BlenheimMkIF",2},
        {"bob:Aircraft.BlenheimMkINF",2},
        {"bob:Aircraft.BlenheimMkIV",9},
        {"bob:Aircraft.BlenheimMkIVF",4},
        {"bob:Aircraft.BlenheimMkIVF_Late",4},
        {"bob:Aircraft.BlenheimMkIVNF",4},
        {"bob:Aircraft.BlenheimMkIVNF_Late",4},
        {"bob:Aircraft.BlenheimMkIV_Late",9},
        //{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below
        {"bob:Aircraft.HurricaneMkI",5},
        {"bob:Aircraft.HurricaneMkI_100oct",5},
        {"bob:Aircraft.HurricaneMkI_100oct-NF",5},
        {"bob:Aircraft.HurricaneMkI_dH5-20",5},
        {"bob:Aircraft.HurricaneMkI_dH5-20_100oct",5},
        {"bob:Aircraft.HurricaneMkI_FB",5},
        {"bob:Aircraft.SpitfireMkI",4},
        {"bob:Aircraft.SpitfireMkIa",4},
        {"bob:Aircraft.SpitfireMkIa_100oct",5},
        {"bob:Aircraft.SpitfireMkI_100oct",4},
        {"bob:Aircraft.SpitfireMkIIa",0.25},
        {"bob:Aircraft.WellingtonMkIc",10}, /*bob:Aircraft.WellingtonMkIc=300.647357579865 */

        { "tobruk:Aircraft.GladiatorMkII_trop", 5},
            //{"tobruk:Aircraft.HurricaneMkI_FB-Trop", 0},
            {"tobruk:Aircraft.HurricaneMkIIa", 5},
            //{"tobruk:Aircraft.HurricaneMkIIaTrop", 0},
            {"tobruk:Aircraft.HurricaneMkIIb", 5},
            {"tobruk:Aircraft.HurricaneMkIIb-Late", 4},
            //{"tobruk:Aircraft.HurricaneMkIIbTrop", 0},
            //{"tobruk:Aircraft.HurricaneMkIIbTrop-Late", 0},
            {"tobruk:Aircraft.HurricaneMkIIc", 4},
            {"tobruk:Aircraft.HurricaneMkIIc-Late", 4},
            //{"tobruk:Aircraft.HurricaneMkIIc-Trop", 0},
            //{"tobruk:Aircraft.HurricaneMkIIc-Trop-Late", 0},
            {"tobruk:Aircraft.HurricaneMkIId", 4},
            //{"tobruk:Aircraft.HurricaneMkIId-Trop", 0},
            {"tobruk:Aircraft.KittyhawkMkIA", 1},
            //{"tobruk:Aircraft.KittyhawkMkIA-Trop", 1},
            {"tobruk:Aircraft.MartletMkIII", 1},
            //{"tobruk:Aircraft.MartletMkIII_Trop", 0},
            {"tobruk:Aircraft.SpitfireMkIIb", 4},
            {"tobruk:Aircraft.SpitfireMkVa", 4},
            {"tobruk:Aircraft.SpitfireMkVb", 4},
            {"tobruk:Aircraft.SpitfireMkVb-HF", 2},
            {"tobruk:Aircraft.SpitfireMkVb-HF-Late", 2},
            //{"tobruk:Aircraft.SpitfireMkVb-HF-Trop", 0},
            {"tobruk:Aircraft.SpitfireMkVbLate", 3},
            //{"tobruk:Aircraft.SpitfireMkVbTrop", 0},
            {"tobruk:Aircraft.TomahawkMkII", 4},
            {"tobruk:Aircraft.TomahawkMkII-Late", 4},
            //{"tobruk:Aircraft.TomahawkMkII-Late-Trop", 0},
            //{"tobruk:Aircraft.TomahawkMkII-Trop", 0},
            //{"tobruk:Aircraft.WellingtonMkIa_trop", 0}, //This is a great plane BUT seems to cause the bug where ppl can't spawn into the server any more. 
            {"tobruk:Aircraft.WellingtonMkIc_Late", 6},
            //{"tobruk:Aircraft.WellingtonMkIc_Late_trop", 0}, //5.003 WON'T DROP BOMBS (more than 1 a mission) SO ELIMINATING IT FOR NOW. 5.017 SHOULD BE FIXED (2021/02), so re-adding it
            {"tobruk:Aircraft.WellingtonMkIc_t", 8},
            {"tobruk:Aircraft.WellingtonMkIc_Torpedo", 2},
            {"tobruk:Aircraft.WellingtonMkIc_Torpedo_Trop", 6},
            //{"tobruk:Aircraft.WellingtonMkIc_Torpedo_Trop", 0},
            //{"tobruk:Aircraft.WellingtonMkIc_trop", 0}, //5.003 WON'T DROP BOMBS (more than 1 a mission) SO ELIMINATING IT FOR NOW  5.017 SHOULD BE FIXED (2021/02), so re-adding it

        } },
        { ArmiesE.Blue, new Dictionary <string,double>(){
        {"bob:Aircraft.Bf-109E-1",10},
        {"bob:Aircraft.Bf-109E-1B",8},
        {"bob:Aircraft.Bf-109E-3",6},
        {"bob:Aircraft.Bf-109E-3B",3},
        {"bob:Aircraft.Bf-109E-4",0.25},
        {"bob:Aircraft.Bf-109E-4_Late",1},
        {"bob:Aircraft.Bf-110C-2",5},
        {"bob:Aircraft.Bf-110C-4",1},
        {"bob:Aircraft.Bf-110C-4-NJG",4},
        {"bob:Aircraft.Bf-110C-4B" ,0.25},
        {"bob:Aircraft.Bf-110C-4Late",1},
        {"bob:Aircraft.Bf-110C-4N",4},
        {"bob:Aircraft.Bf-110C-6",4},
        {"bob:Aircraft.Bf-110C-7",1},
        {"bob:Aircraft.BR-20M",1},	
        //{"bob:Aircraft.DH82A-1",10},  aircraft remmed out or not on list have no restrictions so if you dont want any //of these available use amount 0 like below
        {"bob:Aircraft.DH82A-2",0},
        {"bob:Aircraft.G50",2},
        {"bob:Aircraft.He-111H-2",10},
        {"bob:Aircraft.He-111P-2",15},
        {"bob:Aircraft.Ju-87B-2",3},
        {"bob:Aircraft.Ju-88A-1",4},
        {"bob:Aircraft.Bf-109E-4B",0.25},
        {"bob:Aircraft.Bf-109E-4B_Late",0.25},
        {"bob:Aircraft.Bf-109E-4N",0.25},
        {"bob:Aircraft.Bf-109E-4N_Late",0.25},
        {"bob:Aircraft.Do-17Z-2", 3},
        {"bob:Aircraft.Do-215B-1", 3},

            //{"tobruk:Aircraft.Bf-108B-2_Trop",4},
            {"tobruk:Aircraft.Bf-109E-7",4},
            //{"tobruk:Aircraft.Bf-109E-7_Trop",0},
            {"tobruk:Aircraft.Bf-109E-7N",4},
            //{"tobruk:Aircraft.Bf-109E-7N_Trop",0},
            {"tobruk:Aircraft.Bf-109E-7Z",4},
            {"tobruk:Aircraft.Bf-109F-1",4},
            {"tobruk:Aircraft.Bf-109F-2",4},
            {"tobruk:Aircraft.Bf-109F-2_Late",4},
            //{"tobruk:Aircraft.Bf-109F-2_Trop",0},
            {"tobruk:Aircraft.Bf-109F-4",4},
            {"tobruk:Aircraft.Bf-109F-4_Derated",4},
            //{"tobruk:Aircraft.Bf-109F-4_Trop",0},
            //{"tobruk:Aircraft.Bf-109F-4_trop_Derated",0},
            {"tobruk:Aircraft.Bf-109F-4Z",4},
            //{"tobruk:Aircraft.Bf-109F-4Z_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-4B_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-4N-NJG_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-6_Trop",0},
            //{"tobruk:Aircraft.Bf-110C-7_Trop",0},
            //{"tobruk:Aircraft.BR-20M_Trop",0},
            //{"tobruk:Aircraft.CR42_Trop",0},
            {"tobruk:Aircraft.D520_Serie1",4},
            //{"tobruk:Aircraft.D520_Serie1_Trop",0},
            //{"tobruk:Aircraft.DH82A_Trop",0}, //Tiger Moth/no weapons at all
            //{"tobruk:Aircraft.G50_Trop",0},
            //{"tobruk:Aircraft.He-111H-2_Trop",0},
            {"tobruk:Aircraft.He-111H-6",4},
            {"tobruk:Aircraft.He-111H-6_torpedo",4}, //fake way to make the H-6 torpedo version avail. for <cover
            //{"tobruk:Aircraft.He-111H-6_Trop",0},
            //{"tobruk:Aircraft.He-111H-6_Trop_torpedo",0}, //fake way to make the H-6 torpedo version avail. for <cover
            //{"tobruk:Aircraft.Ju-87B-2_Trop",0},
            {"tobruk:Aircraft.Ju-88A-5",5},
            //{"tobruk:Aircraft.Ju-88A-5_Trop",0},
            {"tobruk:Aircraft.Ju-88A-5Late",5},
            //{"tobruk:Aircraft.Ju-88A-5Late_Trop",0},
            {"tobruk:Aircraft.Ju-88C-1",4},
            {"tobruk:Aircraft.Ju-88C-2",4},
            //{"tobruk:Aircraft.Ju-88C-2_Trop",0},
            {"tobruk:Aircraft.Ju-88C-4",5},
            //{"tobruk:Aircraft.Ju-88C-4_Trop",0},
            {"tobruk:Aircraft.Ju-88C-4Late",4},
            //{"tobruk:Aircraft.Ju-88C-4Late_Trop",0},
            {"tobruk:Aircraft.Macchi-C202-SeriesIII",1},
            {"tobruk:Aircraft.Macchi-C202-SeriesIII-AltoQuota",1},
            {"tobruk:Aircraft.Macchi-C202-SeriesVII",1},
            {"tobruk:Aircraft.Macchi-C202-SeriesVII-AltoQuota",1},

        }
        }

    };

    //save current state of supply every 10 mins or so; just a quick save
    public void SaveSupplyRecursive(bool firstTime=false)
    {
        Timeout(33.33, () => { SaveSupplyRecursive(false); } );
        //WritePrimarySupply(supplySuffix, false, firstTime);

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SUXX1 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping
        Task.Run(() => WritePrimarySupply(supplySuffix, false, firstTime));
    }



    //Reads the .ini file with the current Supply & Increase
    //or a replacement file you can supply to replace the current one. 
    public void ReadSupply(string suffix, string replacementFilename = "", Player player = null)
    {
        //MO_SelectPrimaryObjectives(army);

        Console.WriteLine("Supply: Reading Status from file (suffix) " + suffix + " : (replacement filename) " + replacementFilename);

        string filename = CAMPAIGN_ID + suffix + ".ini";
        string existingSupplyFilename = filename;

        if (replacementFilename.Length > 0) filename = replacementFilename;

        string filepath = STATSCS_FULL_PATH + filename;

        if (!File.Exists(filepath))
        {
            Console.WriteLine("SUPPLY ERROR!!! The given supply file does not exist! File path: " + filepath);
            if (replacementFilename.Length > 0)
            {
                if (player != null) GamePlay.gpLogServer(new Player[] { player }, "SUPPLY ERROR!!! The given supply file does not exist! File path: " + filepath, new object[] { });
                return;
            }

        }

        if (replacementFilename.Length > 0)
        {
            string appendix = "-replaced" + (new Random().Next(10, 99)).ToString("n0");
            try
            {
                File.Move(STATSCS_FULL_PATH + existingSupplyFilename, STATSCS_FULL_PATH + existingSupplyFilename + appendix);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Supply FILE ERROR - aborting read of new supply.ini: " + ex.ToString());
                GamePlay.gpLogServer(new Player[] { player }, "Supply: Could not make a copy of the old supply.ini file as a backup.  ABORTING the file read!", null);
                return;
            }
            try
            {
                //File.Create(STATSCS_FULL_PATH + existingSupplyFilename);
                File.Create(STATSCS_FULL_PATH + existingSupplyFilename).Close(); //must add the Close() or the file remains open/can't write to it later
            }
            catch (Exception ex)
            {
                Console.WriteLine("Supply FILE ERROR - aborting read of new supply.ini: " + ex.ToString());
                GamePlay.gpLogServer(new Player[] { player }, "Supply: Could open the new supply.ini file.  Trying to copy the old -supply.ini file back in place and ABORTING the new file read! You may need to clean this up manually.", null);
                File.Move(STATSCS_FULL_PATH + existingSupplyFilename + appendix, STATSCS_FULL_PATH + existingSupplyFilename);
                return;
            }
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, "SUPPLY: Copying existing Supply .ini to: " + existingSupplyFilename + appendix + " and reading new file: " + filename, new object[] { });
            if (player != null) GamePlay.gpLogServer(new Player[] { player }, "SUPPLY: Check server console for any errors found while reading the file." + filename, new object[] { });

        }

        Console.WriteLine("");
        Console.WriteLine("========SUPPLY: READING SUPPLY .INI FILE - BEGIN ==========================================================================================================================");
        Console.WriteLine("");

        try
        {
            iniFile = new Ini.SupplyIniFile(filepath);
            //Console.WriteLine("t: " + AircraftSupplyDefault[ArmiesE.Red].ToString());            
            AircraftSupply[ArmiesE.Red] = iniFile.IniReadDictionary("AircraftSupplyRed", AircraftSupplyDefault[ArmiesE.Red]);
            //Console.WriteLine("t: made it!");
            //Console.WriteLine("t: " + AircraftSupply[ArmiesE.Red].ToString());     
            AircraftSupply[ArmiesE.Blue] = iniFile.IniReadDictionary("AircraftSupplyBlue", AircraftSupplyDefault[ArmiesE.Blue]);
            //Console.WriteLine("t: made it 2!");
            AircraftIncrease[ArmiesE.Red] = iniFile.IniReadDictionary("AircraftIncreaseRed", AircraftIncreaseDefault[ArmiesE.Red]);
            //Console.WriteLine("t: made it3!");
            AircraftIncrease[ArmiesE.Blue] = iniFile.IniReadDictionary("AircraftIncreaseBlue", AircraftIncreaseDefault[ArmiesE.Blue]);
            //Console.WriteLine("t: made it4!");
        }
        catch (Exception ex) { Console.WriteLine("ReadSupply: " + ex.ToString()); }

        Console.WriteLine("");

        if (replacementFilename.Length>1)
        {
            WritePrimarySupply(suffix, quick: true, firstTime: true); //quick-true stops it from copying over to _old.  Firsttime:true makes it write increase as well as supply values
        }

        Console.WriteLine("");
        Console.WriteLine("========SUPPLY: READING SUPPLY .INI FILE - END ==========================================================================================================================");

    }

    //At mission end, anything in the air still is returned to stock
    //This is usually called at like 30 seconds until the end, giving enough time to wrap things up without panic
    //If you just wait until the end many of the onPlayerDisconnected and OnPlaceLeave type events are never recorded because the thread has terminated
    public void ReturnAircraftToSupplyAtMissionEnd()
    {
        if (GamePlay != null && GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)
        {

            foreach (Player py in GamePlay.gpRemotePlayers())
            {                
                string pl = "(none)";
                if (py.Place() != null)
                {                  
                    AiActor act = py.Place();
                    pl = act.Name();

                    if (act as AiAircraft != null)
                    {
                        SupplyOnPlaceLeave(py, act,0, true, reason: "SAFE_Supply: End of mission shutdown");
                        Console.WriteLine("SupplyEnd: Returning to stock " + py.Name() +
                            " " + pl);
                    }
                }
            }
        }
    }

    //For now, aircraft damaged are just held until mission end and then returned to supply
    //Later we could do tricky things like saving out more severely damaged aircraft for $X hours or whatever
    //For now we are showing hours to repair etc but then not really doing that, just returning to supply
    //at mission end
    public void ReturnDamagedAircraftToSupplyAtMissionEnd()
    {
        foreach (AiActor act in damagedAircraft.Keys)
        {
            if (act as AiAircraft != null)
            {
                Player py = damagedAircraft[act].Item1;
                string pl = act.Name();
                SupplyOnPlaceLeave(py, act, 0, true, reason:"DEPENDS_Supply: Aircraft damaged during mission & not yet fully repaired.");
                Console.WriteLine("SupplyEnd: Returning to stock " + py.Name() +
                    " " + pl);
            }
        }
    }

    //stores the aircraft in the Damaged list & returns the hours to repair
    public double AddAircraftToDamagedSupply(Player player, AiActor actor, double forceDamage = 0, bool offAirfield = false, double nearestAirfield_distance_m = 0)
    {
        if (actor == null || actor as AiAircraft == null) return 0;
        DateTime timeDamaged = DateTime.UtcNow;
        double hoursToRepair = forceDamage * 96;
		if (offAirfield) hoursToRepair += 24 * 3 + nearestAirfield_distance_m/10000*24; //several days say to retrieve & transport to repair place
        DateTime timeRepaired = timeDamaged.AddHours(hoursToRepair);
        if (damagedAircraft.ContainsKey(actor) && damagedAircraft[actor].Item3 <= forceDamage + 0.01) return -1; //redundant entry, maybe because 2 positions reporting same aircraft.  However if it is greater damage this time, we'll take it!                

        damagedAircraft[actor] = new Tuple<Player, string, double, double, DateTime, DateTime >(player, player.Name(), forceDamage, hoursToRepair, timeDamaged, timeRepaired);
        return hoursToRepair;
    }

    public HashSet<AiActor> AircraftActorsCurrentlyInAir()
    {
        HashSet<AiActor> retHS = new HashSet<AiActor>();
        if (GamePlay != null && GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)
        {

            foreach (Player py in GamePlay.gpRemotePlayers())
            {                
                if (py.Place() != null)
                {
                    AiActor act = py.Place();
                    
                    if (act as AiAircraft != null)
                    {
                        retHS.Add(act);
                    }
                }
            }
        }
        return retHS;
    }

    //OK, this is a kludge until we get SupplyEndMission tied into -main.cs
    //but redMult=-1000000 or blueMult=-1000000 means, it is an intermediate win & ignore that red or blue value
    //but add in the other one.  so that is used for intermediate victories, where we add in aircraft gains for one side, but not the other, mid-mission.
    //Also, don't do MIssionEnd stuff in that case.
    //Arghhh.
    public bool SupplyEndMission(double redMult = 1, double blueMult = 1)
    {
        double magicNumber = -1000000;
        Console.WriteLine("SupplyEndMission: Red: " + redMult.ToString() + " Blue: " + blueMult.ToString());
        bool doMissionEnd = true;
        if (redMult == magicNumber || blueMult == magicNumber) doMissionEnd = false;
        if (doMissionEnd)
        {
            ReturnDamagedAircraftToSupplyAtMissionEnd();
            ReturnAircraftToSupplyAtMissionEnd();
            ListAircraftLost(0, null, true, false, match: "");
        }
        if (redMult != magicNumber)
        {
            GamePlay.gpLogServer(null, "Red aircraft resupplied at strength " + (redMult * 100.0).ToString("F0"), new object[] { });  //NEVER multiply by an integer.  100.0<<<<<
            AddIncrease(ArmiesE.Red, redMult);
        }
        if (blueMult != magicNumber)
        {
            GamePlay.gpLogServer(null, "Blue aircraft resupplied at strength " + (blueMult * 100.0).ToString("F0"), new object[] { }); //100 = par (ie, 1.00) for resupply, as shown to players
            AddIncrease(ArmiesE.Blue, blueMult);
        }
        
        
        return WritePrimarySupply(supplySuffix);
    }

    public bool SupplySaveStatus()
    {
        return WritePrimarySupply(supplySuffix);
    }

    //Add in the regular increase in supply (whether by mission ,day, week, whatever is up to you
    //mult>1 will make the increase larger than normal, mult<1 will make it smaller than normal.
    //So mult=2 gives 2X regular increase in supply, mult=0.5 gives 1/2 the regular increase.  
    //mult=-1 would give a DECREASE in supply.  etc.
    public void AddIncrease(ArmiesE armE, double mult)
    {

        //foreach (AircraftIncrease[ArmiesE.Red] )
        foreach (KeyValuePair<string, double> current in AircraftIncrease[armE])
        {
            try
            {

                if (AircraftSupply[armE].ContainsKey(current.Key))
                {
                    if (AircraftSupply[armE][current.Key] < 0) AircraftSupply[armE][current.Key] = 0;  //If you start out with say 0.6 in supply & lose one you can actually go to negative value; we'll say that's not physically possible.  This actually gives armies a little break on getting that first a/c back after they have lost them all
                                                                                                       //Console.WriteLine("Supply Upd: " + current.Key + " " + AircraftSupply[armE][current.Key].ToString("n3"));
                    AircraftSupply[armE][current.Key] += current.Value * mult;
                    //Console.WriteLine("Supply Upd: " + current.Key + " " + AircraftSupply[armE][current.Key].ToString("n3"));
                }
                else AircraftSupply[armE][current.Key] = current.Value * mult;

                //can't go below zero.  With a negative mult we could actually end up with -number even if we started out positive.
                if (AircraftSupply[armE][current.Key] < 0) AircraftSupply[armE][current.Key] = 0;

                //if mult>2, meaning the side has turned the map or something, then they will get a minimum of 5 of each plane type
                //reward for turning the map, plus for very slow-to-resupply aircraft even with a large mult you sometimes don't even get 1 aircraft out of it.
                //so it smooths out the difference in resupply for large vs small resupply values when a mult is incorporated.
                if (mult > 3.5 && AircraftSupply[armE][current.Key] < 5) AircraftSupply[armE][current.Key] = 5;

                //we'll say that max # of inventory of any aircraft is 400.  After that we run out of storage space or something.
                if (AircraftSupply[armE][current.Key] > MAX_AC_IN_INVENTORY) AircraftSupply[armE][current.Key] = MAX_AC_IN_INVENTORY;
            }catch (Exception ex) { Console.WriteLine("SupplyAI ERROR!: " + ex.ToString()); }
        }      

    }

    public bool WritePrimarySupply(string suffix, bool quick = false, bool firstTime = false)
    {

        DateTime dt = DateTime.UtcNow;
        string date = dt.ToString("u");
        bool ret = true;
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("UXX1 " + DateTime.UtcNow.ToString("T")); //Testing for potential causes of warping

        //Console.WriteLine("MO_Write #2");

        string filepath = STATSCS_FULL_PATH + CAMPAIGN_ID + suffix + ".ini";
        string filepath_old = STATSCS_FULL_PATH + CAMPAIGN_ID + suffix + "_old.ini";
        string currentContent = String.Empty;
        if (!quick)
        {
            //Save most recent copy of Supply.ini with suffix _old
            try
            {
                if (File.Exists(filepath_old)) { File.Delete(filepath_old); }
                File.Copy(filepath, filepath_old); //We could use File.Move here if we want to eliminate the previous .ini file before writing new data to it, thus creating an entirely new .ini.  But perhaps better to just delete specific sections as we do below.
            }
            catch (Exception ex) { Console.WriteLine("Supply Write Inner: " + ex.ToString()); ret = false; }


            //Console.WriteLine("MO_Write Save #3");
        }

        try
        {

            //Ini.IniFile ini = new Ini.IniFile(filepath, this);
            Ini.SupplyIniFile ini = new Ini.SupplyIniFile(filepath);

            //.ini keeps the same file & just adds or updates entries already there. Unless you delete them.
            //Delete all entries in these sections first

            //First time we read the INCREASE sections in from file (replacing them with defaults if they don't exist) and then immediately write that back out to file with the defaults added if necessary.
            //but the second and succeeding times we write on SUPPLY only and don't overwrite the INCREASE sections. This allows us to edit
            //the INCREASE sections of the .ini file while the program is running and have any changes picked up next time the mission starts.

            ini.IniDeleteSection("AircraftSupplyRed");
            ini.IniDeleteSection("AircraftSupplyBlue");
            if (firstTime) ini.IniDeleteSection("AircraftIncreaseRed");
            if (firstTime) ini.IniDeleteSection("AircraftIncreaseBlue");


            //Write the new data in the two sections
            ini.IniWriteDictionary("AircraftSupplyRed", AircraftSupply[ArmiesE.Red]);
            ini.IniWriteDictionary("AircraftSupplyBlue", AircraftSupply[ArmiesE.Blue]);
            if (firstTime) ini.IniWriteDictionary("AircraftIncreaseRed", AircraftIncrease[ArmiesE.Red]);
            if (firstTime) ini.IniWriteDictionary("AircraftIncreaseBlue", AircraftIncrease[ArmiesE.Blue]);

            //Save supply list to special directory as a bit of a backup/record of objectives over time
        }
        catch (Exception ex) { Console.WriteLine("Supply Write: " + ex.ToString()); ret = false; }

        if (!quick)
        {
            var backPath = STATSCS_FULL_PATH + CAMPAIGN_ID + @" campaign backups\";
            string filepath_date = backPath + CAMPAIGN_ID + suffix + "-" + dt.ToString("yyyy-MM-dd-HH") + ".ini";

            //Create the directory for the backup files, if it doesn't exist
            if (!System.IO.File.Exists(backPath))
            {

                try
                {
                    //System.IO.File.Create(backPath);
                    System.IO.Directory.CreateDirectory(backPath);
                }
                catch (Exception ex) { Console.WriteLine("MO_Write Dir Create Date: " + ex.ToString()); ret = false; }

            }

            //Save most recent copy of supply file to the backup directory with suffix like  -2018-05-13.ini
            try
            {
                if (File.Exists(filepath_date)) { File.Delete(filepath_date); }
                File.Copy(filepath, filepath_date);
            }
            catch (Exception ex) { Console.WriteLine("Supply Write Date: " + ex.ToString()); ret = false; }
        }

        return ret;


    }


    private bool IsArmy(AiActor actor)
    {    
        if (actor != null && actor.Army() != null && (actor.Army() == 1 || actor.Army() == 2)) return true;
        else return false;
    }


    public bool IsLimitReached(AiActor actor)
    {
        bool limitReached = false;
        AiCart cart = actor as AiCart;

        //Console.WriteLine("IsLimitReached " + cart.InternalTypeName() + " " + actor.Army().ToString());
        //if (AircraftSupply[(ArmiesE)(actor.Army())].ContainsKey(cart.InternalTypeName())) Console.WriteLine("containskey true");

        if (cart != null && IsArmy(actor))
            if (AircraftSupply[(ArmiesE)(actor.Army())].ContainsKey(cart.InternalTypeName()))
                if (AircraftSupply[(ArmiesE)(actor.Army())][cart.InternalTypeName()] < 0.5)   //We're using doubles and ROUNDING so for example 0.6 a/c will show as 1, 0.4 will show as 0.
                    limitReached = true;
         
        return limitReached;
    }

    public bool IsLimitReached(string internalTypeName, int army)
    {
        bool limitReached = false;
        if (army==1 || army==2)
            if (AircraftSupply[(ArmiesE)(army)].ContainsKey(internalTypeName))
                if (AircraftSupply[(ArmiesE)(army)][internalTypeName] < 0.5)   //We're using doubles and ROUNDING so for example 0.6 a/c will show as 1, 0.4 will show as 0.
                    limitReached = true;

        return limitReached;
    }

    //-1 if the aircraft type doesn't even exist in the table
    public int AircraftStockRemaining(AiActor actor)
    {
        int remaining = -1;
        AiCart cart = actor as AiCart;

        //Console.WriteLine("IsLimitReached " + cart.InternalTypeName() + " " + actor.Army().ToString());
        //if (AircraftSupply[(ArmiesE)(actor.Army())].ContainsKey(cart.InternalTypeName())) Console.WriteLine("containskey true");

        if (cart != null && IsArmy(actor))
            if (AircraftSupply[(ArmiesE)(actor.Army())].ContainsKey(cart.InternalTypeName()))
                remaining = (int)Math.Round(AircraftSupply[(ArmiesE)(actor.Army())][cart.InternalTypeName()]);   //We're using doubles and ROUNDING so for example 0.6 a/c will show as 1, 0.4 will show as 0.

        return remaining;
    }

    //-1 if the aircraft type or army doesn't even exist in the table
    public int AircraftStockRemaining(string internalTypeName, int army)
    {
        int remaining = -1;
        if (army == 1 || army == 2)
            if (AircraftSupply[(ArmiesE)(army)].ContainsKey(internalTypeName))
                remaining = (int)Math.Round(AircraftSupply[(ArmiesE)(army)][internalTypeName]);   //We're using doubles and ROUNDING so for example 0.6 a/c will show as 1, 0.4 will 
        return remaining;
    }
    /*
    public Dictionary<string,int> AircraftStockRemaining(int army)
    {
        int remaining = -1;
        if (army == 1 || army == 2)
            if (AircraftSupply[(ArmiesE)(army)].ContainsKey(internalTypeName)
                remaining = Math.Round(AircraftSupply[(ArmiesE)(army)][internalTypeName]);   //We're using doubles and ROUNDING so for example 0.6 a/c will show as 1, 0.4 will 
        return remaining;
    }
    */


    //Returns TRUE if newly added; false if already there.
    private bool aircraftCheckOut_add (AiActor actor, Player player)
    {
        bool retur = true;
        string pilotNames = "";
        string aircraftTy8pe = "";
        AiAircraft ac = actor as AiAircraft;
        AiCart cart = actor as AiCart;
        if (ac != null)
        {
            //get name(s) of any pilot(s) in the aircraft
            HashSet<string> namesHS = new HashSet<string>();
            bool first = true;
            for (int i = 0; i < ac.Places(); i++)
            {
                if (ac.Player(i) != null && ac.Player(i).Name() != null && !namesHS.Contains(ac.Player(i).Name()))
                {
                    if (!first) pilotNames += " - ";
                    pilotNames += ac.Player(i).Name();
                    namesHS.Add(ac.Player(i).Name());
                    first = false;

                }
            }
            

            //(AircraftSupply[(ArmiesE)(actor.Army())][cart.InternalTypeName()]
            


        }
        if (pilotNames == "")
        {
            if (player != null && player.Name() != null) pilotNames = player.Name();
            else pilotNames = "(AI/No Pilot Listed)";
        }

        if (!aircraftCheckedOut.Contains(actor)) aircraftCheckedOut.Add(actor);
        else retur = false; //meaning, aircraftCheckedOut already contains actor

        if (!aircraftCheckedOutInfo.ContainsKey(actor)) aircraftCheckedOutInfo.Add(actor, new Tuple<int,string,string, DateTime> (actor.Army(), pilotNames, cart.InternalTypeName(), DateTime.UtcNow));
        return retur;
    }

    /* 
     * 
     * 
     */
    //Returns any a/c a player  may have checked out currently, or null
    private AiActor lookup_aircraftCheckOut_ByPlayer(Player player)
    {
        if (player == null) return null;
        string playerNameMatch = player.Name();
           if (playerNameMatch.Length == 0) return null;
        
        foreach (AiActor actor in aircraftCheckedOutInfo.Keys)
        {
           
            Tuple<int, string, string, DateTime> entry = aircraftCheckedOutInfo[actor];

            if (entry.Item2.ToLowerInvariant().Equals(playerNameMatch.Trim().ToLowerInvariant())) return actor;

        }

        return null;
    }
    

    public string selectSupplyPlane(string acName, ArmiesE army)
    {
        string retplane = null;
        if (!(army == ArmiesE.Blue || army == ArmiesE.Red) ) return null;        

        //else if (army == ArmiesE.None) { armylist.Add(ArmiesE.Red); armylist.Add(ArmiesE.Blue); }

        int numChoice = -1;
        if (!Int32.TryParse(acName, out numChoice)) numChoice = -1;
        //if (numChoice >= 0 && numChoice < CoverAircraftCurrentlyAvailable[army].Count) returnCoverAircraftCurrentlyAvailable[army][numChoice].Key;
        int count = 1;

        foreach (string key in AircraftSupply[army].Keys)
        {
            //string acn = returnCoverAircraftCurrentlyAvailable[army][key];
            //string msg = string.Format("#{0} {1} {2}", i, Calcs.ParseTypeName(CoverAircraftCurrentlyAvailable[a].Key), CoverAircraftCurrentlyAvailable[a].Entry);
            
            //returns the very first match, or null
            if (numChoice > 0 && count == numChoice) { retplane = key; break; }
            if (numChoice <= 0 && acName.Length > 0 && key.ToLowerInvariant().Contains(acName.Trim().ToLowerInvariant()))
            {
                retplane = key;
                break;
            }
            count++;

        }

        return retplane;

    }

    //Tuple is how many added, how many total afterwards 
    //<-1,-1> is error condition, where we couldn't find that type of plane to add
    //testName means we just check to see if that aircraftType can add to the supply totals <-1,-1> on failure
    //also allows NEGATIVE ADD (ie, taking a plane out, even putting stock in negative numbers)
    //which we need here & there.
    public Tuple<int,int> addAircraftToSupplyFerry(Player player, string aircraftType, int howMany, bool checkACNameAvailableInSupply=false)
    {
        try
        {

            int numToAdd = howMany;
            int army = player.Army();
            string aircraftName = aircraftType.Trim();

            //GamePlay.gpLogServer(new Player[] { player }, "Cover: numAC1 " + numAC.ToString(), new object[] { });


            if (numToAdd == 0 && !checkACNameAvailableInSupply)
            {
                GamePlay.gpLogServer(new Player[] { player }, "Ferry: 0 aircraft requested for addition to the Supply. Sorry!", new object[] { });
                
                return new Tuple<int,int>(0,0);
            }



            string planeKey = selectSupplyPlane(aircraftName, (ArmiesE)army);

            if (planeKey == null)
            {
                if (!checkACNameAvailableInSupply) GamePlay.gpLogServer(new Player[] { player }, "Ferry: 0 aircraft requested for addition to the Supply.  There was a problem identifying the type of aircraft to add.  Please notify HQ!", new object[] { });
                return new Tuple<int, int>(-1, -1);
            }


            
            AircraftSupply[(ArmiesE)army][planeKey] += numToAdd;

            //We'll let objectiverepairmissions send the player message
            //GamePlay.gpLogServer(new Player[] { player }, "Ferry: You have ferried " + numToAdd.ToString("F0") + " type " + planeKey + " to army " + ArmiesL[army] + " - new total: " + AircraftSupply[(ArmiesE)army][planeKey].ToString("F0"), new object[] { });
            
            return new Tuple<int, int>(numToAdd, (int)Math.Round(AircraftSupply[(ArmiesE)army][planeKey]));
        }

        catch (Exception ex) { Console.WriteLine("Supply Ferry Add ERROR: " + ex.ToString()); return new Tuple<int, int>(-1, -1); }

    }

    public void addAircraftToSupply(Player player, string selectString)
    {
        try
        {

            string[] sections = selectString.Split(' ');

            GamePlay.gpLogServer(new Player[] { player }, "Supply: Call " + selectString + " - Number values: " + (sections.Count() -1 ).ToString(), new object[] { });

            string aircraftName = "";
            string numAddString = "";
            string armyName = "";

            if (sections.Count()  == 4)
            {
                armyName = sections[1];
                aircraftName = sections[2];
                numAddString = sections[3];
            }
            else
            {
                GamePlay.gpLogServer(new Player[] { player }, "Supply Add: ERROR adding new aircraft to Supply.  Command had the wrong format. 3 values needed--number entered: " + (sections.Count() -1).ToString("F0"), new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "HELP: <suppadd 2 6 -10   - remove 10 aircraft from Army 2=Blue, aircraft type #6.   Get aircraft type # from Tab-4 supply listing.", new object[] { });

            }

            int numToAdd = 0;
            try
            {
                numToAdd = Convert.ToInt32(numAddString);
            }
            catch { numToAdd = 0; }

            int army = 0;
            try
            {
                army = Convert.ToInt32(armyName);
            }
            catch { army = 0; }

            aircraftName = aircraftName.Trim();

            //GamePlay.gpLogServer(new Player[] { player }, "Cover: numAC1 " + numAC.ToString(), new object[] { });


            if (numToAdd == 0)
            {
                GamePlay.gpLogServer(new Player[] { player }, "Supply Add: 0 aircraft requested.  Error in command format?", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "Result: " + numToAdd.ToString("F0") + " type " + aircraftName + " to army " + ArmiesL[army], new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "HELP: <suppadd 1 8 12   - add 12 aircraft to Army 1=Red, aircraft type #8.  Get aircraft type # from Tab-4 supply listing. - " + sections.Count().ToString(), new object[] { });
                return;
            }
                
            

            string planeKey = selectSupplyPlane(aircraftName, (ArmiesE)army);

            if (planeKey == null)
            {
                GamePlay.gpLogServer(new Player[] { player }, "Supply Add: ERROR adding new aircraft to stock.  Command had the wrong format.", new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "Result: " + numToAdd.ToString("F0") + " type " + planeKey + " to army " + ArmiesL[army], new object[] { });
                GamePlay.gpLogServer(new Player[] { player }, "HELP: <suppadd 1 12 60   - add 60 aircraft to Army 1=Red, aircraft type #12.   Get aircraft type # from Tab-4 supply listing.", new object[] { });

                return;
            }

            
            //Point3d ac1loc = (aircraft as AiActor).Pos();



            //AircraftSupply[(ArmiesE)army][aircraftName] += numToAdd;
            AircraftSupply[(ArmiesE)army][planeKey] += numToAdd;

            GamePlay.gpLogServer(new Player[] { player }, "Supply Add: Adding " + numToAdd.ToString("F0") + " type " + planeKey + " to army " + ArmiesL[army] + " - new total: " + AircraftSupply[(ArmiesE)army][planeKey].ToString("F0"), new object[] { });
        }

        catch (Exception ex) { Console.WriteLine("Supply Add ERROR: " + ex.ToString()); }

    }

    public string ListAircraftLost(int army = 0, Player player = null, bool display = true, bool html = false, string match = "", string playerNameMatch = "")
    {
        try
        {

            HashSet<AiActor> actorsNotCheckedInorInAir = new HashSet<AiActor>(aircraftCheckedOut);
            HashSet<AiActor> aCIA = AircraftActorsCurrentlyInAir();

            actorsNotCheckedInorInAir.ExceptWith(aircraftCheckedIn); //remove all a/c that have been checked in
            actorsNotCheckedInorInAir.ExceptWith(aCIA); //remove all a/c still in the air

            double delay = 0;
            double add = 0;
            if (display) add = 0.2;
            string returnmsg = "";
            Player[] playerL = null;
            if (GamePlay != null) playerL = new Player[] { GamePlay.gpPlayer() }; //displays to SERVER only
            if (player != null) playerL = new Player[] { player };
            else display = false;
            

            string nl = Environment.NewLine;
            if (html) nl = "<br>" + nl;

            
            int low = 1;
            int high = 2;
            if (army == 1 || army == 2) { low = army; high = army; }
            for (int x = low; x <= high; x++)
            {
                bool haveRetForArmy = false;
                string msg = ">>>>" + ArmiesL[x] + "  Aircraft Destroyed or Lost This Session";
                if (display)
                {
                    delay += add;
                    if (GamePlay != null) Timeout(delay, () => { GamePlay.gpLogServer(playerL, msg, null); });
                }
                returnmsg += msg + nl;

                foreach (AiActor actor in actorsNotCheckedInorInAir)
                {
                    if (!aircraftCheckedOutInfo.ContainsKey(actor)) continue;
                    Tuple<int, string, string, DateTime> entry = aircraftCheckedOutInfo[actor];
                    if (entry.Item1 != x) continue;

                    if (match.Length > 0 && !entry.Item3.ToLowerInvariant().Contains(match.Trim().ToLowerInvariant())) continue; //implement substring matching "<lost hurri" etc

                    if (playerNameMatch.Length > 0 && !entry.Item2.ToLowerInvariant().Contains(playerNameMatch.Trim().ToLowerInvariant())) continue; //implement substring matching for match by player name

                    string msg1 = ParseTypeName(entry.Item3) + " " + entry.Item2 + " " + entry.Item4.ToString("u");
                    //string msg1 = current.Key + ": " + current.Value.ToString("n1");
                    if (display && GamePlay != null)
                    {
                        delay += add;
                        Timeout(delay, () => { GamePlay.gpLogServer(playerL, msg1, null); });
                    }
                    returnmsg += msg1 + nl;
                    haveRetForArmy = true;

                }

                if (!haveRetForArmy)
                {
                    returnmsg += "(none)" + nl;
                    delay += add;
                    if (display && GamePlay != null) Timeout(delay, () => { GamePlay.gpLogServer(playerL, "(none)", null); });
                }

                if (army == 0 && x == 1) returnmsg += nl; //add a space in between the two lists, for text or html purposes

                //(AircraftSupply[(ArmiesE)(actor.Army())][cart.InternalTypeName()]



            }

            return returnmsg;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Supply ListAircraftLost ERROR: " + ex.ToString());
            return "";
        }
       
    }

    public bool CheckActorOut_fromStatsDeath(AiActor actor, Player player = null, bool Force = false)
    {
        if (actor == null || actor as AiAircraft == null || NumberPlayerInActor(actor) > 0) return false;
        bool status = CheckActorOut(actor, player: null, Force: false, AICheckout: true, fromStats:
            true);
       // string acName = ParseTypeName((actor as AiCart).InternalTypeName());
       
        return status;
    }

    private bool CheckActorOut(AiActor actor, Player player = null, bool Force = false, bool AICheckout = false, bool fromStats = false)
    {
        try
        {
            AiCart cart = actor as AiCart;

            Console.WriteLine("CheckActorOut " + cart.InternalTypeName() + " " + actor.Army().ToString() + " fromStats: {0}", fromStats);
            //if (AircraftSupply[(ArmiesE)(actor.Army())].ContainsKey(cart.InternalTypeName())) Console.WriteLine("containskey true");

            bool newCheckOut = false;

            //Don't double check-out aircraft, unless Forced to do so via new info from -stats.cs.  Force means we accidentally check it back in & so we're checking it out again for good.
            if (aircraftCheckedOut.Contains(actor) && !Force)
            {
                Console.WriteLine("Supply: This aircraft has already been checked OUT before: " + cart.InternalTypeName());
                return false;
            }
            else newCheckOut = aircraftCheckOut_add(actor,player);

            DisplayNumberOfAvailablePlanes(actor); //Show this to player, but only on first time plane checked out.

            //Console.WriteLine("valout1=" + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString());
            if (cart != null && IsArmy(actor))
                if (AircraftSupply[(ArmiesE)actor.Army()].ContainsKey(cart.InternalTypeName()))
                {
                    AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()] -= 1;
                    if (fromStats) return newCheckOut; //that's all we need if from stats, no messages

                    double print = new Random().NextDouble();
                    string numString = AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString("n0");
                    string acName = ParseTypeName(cart.InternalTypeName());
                    Timeout(4.33, () =>
                    {
                        GamePlay.gpLogServer(new Player[] { player }, numString + " "
                            + acName + " remain in reserve", null);
                    });

                    //special quick-start instructions for Blennie, Beauf, BR.20:
                    if (
                            player != null && !AICheckout && !Force &&
                            (acName.Contains("Blenheim") || acName.Contains("Beaufighter") || acName.Contains("BR-20"))
                       )
                    {
                        Timeout(4.81, () =>
                        {
                            if (GamePlay != null) GamePlay.gpLogServer(new Player[] { player }, "QUICK START INSTRUCTIONS FOR " + acName.ToUpper() + ": 1. Fuel, throttle, radiators to normal start position.", null);
                        });
                        Timeout(4.83, () =>
                        {
                            if (GamePlay != null) GamePlay.gpLogServer(new Player[] { player }, "2. Switch position to (any) rear gunner or observer.", null);
                        });
                        Timeout(4.85, () =>
                        {
                            if (GamePlay != null) GamePlay.gpLogServer(new Player[] { player }, "3. Switch position back to pilot.", null);
                        });
                        Timeout(4.87, () =>
                        {
                            if (GamePlay != null) GamePlay.gpLogServer(new Player[] { player }, "4. ALT-F2 (RELEASE POSITION) command.  You will leave pilot position & move to rear gunner/observer.", null);
                        });
                        Timeout(4.89, () =>
                        {
                            if (GamePlay != null) GamePlay.gpLogServer(new Player[] { player }, "5. AI will take over pilot's position & start engines. Wait until engines are started, then move back to pilot position. Engines will be warmed up & ready to go.", null);
                        });


                    }

                    Console.WriteLine("valout2=" + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString());
                    if (Force && newCheckOut) //In this case we hav e probably just announced to the player that the a/c was safely returned to stock.  and newcheckOut==true indicates, nope we actually just checked it out
                    {
                        //If this is from <Cover, we have likely lost the owning player that point, oh well.
                        Timeout(4.33, () =>
                        {
                            GamePlay.gpLogServer(new Player[] { player }, String.Format( "UPDATE: {0} was lost in action; {1:N0} remain in reserve", cart.InternalTypeName(), AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()]), null);
                        });
                        Console.WriteLine("valout2= FORCED!");
                    }
                }
            return newCheckOut;
        }
        catch (Exception ex) { Console.WriteLine("Supply - CheckActorOut ERROR: " + ex.ToString());  return false; }
    }


    private void CheckActorIn(AiActor actor, Player player = null)
    {
        try
        {
            AiCart cart = actor as AiCart;
            //Console.WriteLine("valin1=" + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString());

            if (aircraftCheckedIn.Contains(actor))
            {
                Console.WriteLine("Supply: This aircraft has already been checked IN before: " + cart.InternalTypeName());
                return;
            }
            else aircraftCheckedIn.Add(actor);

            if (!aircraftCheckedOut.Contains(actor))
            {
                Console.WriteLine("Supply: This aircraft has never been checked OUT but someone is trying to check it IN (common with AI aircraft): " + cart.InternalTypeName());
                return;
            }

            if (cart != null && IsArmy(actor))
                if (AircraftSupply[(ArmiesE)actor.Army()].ContainsKey(cart.InternalTypeName()))
                {
                    AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()] += 1;
                    Console.WriteLine("valin2=" + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString());
                    string numString = AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString("n0");
                    Timeout(3.33, () =>
                    {
                        GamePlay.gpLogServer(new Player[] { player }, ParseTypeName(cart.InternalTypeName()) + " returned and added to stock; "
                            + numString + " " + "currently in stock", null);
                    });

                }
        }
        catch (Exception ex) { Console.WriteLine("Supply - CheckActorIn ERROR: " + ex.ToString()); }
    }

    /*
        private void DebugPrintNumberOfAvailablePlanes()
        {
            foreach (KeyValuePair<string, int> current in AircraftLimitations)
            {
                GamePlay.gpLogServer(new Player[] { GamePlay.gpPlayer() }, "InternalTypeName: {0}, Available: {1}", new object[] { current.Key, current.Value.ToString(CultureInfo.InvariantCulture) });
            }
        }
        */
    public string DisplayNumberOfAvailablePlanes(AiActor actor, Player player = null, bool AICheckout = false)
    {
        try
        {


            AiCart cart = actor as AiCart;

            if (cart != null && actor != null)
            {

                if (!AircraftSupply.ContainsKey((ArmiesE)actor.Army()) || !AircraftSupply[(ArmiesE)actor.Army()].ContainsKey(cart.InternalTypeName())) return "";
                string acName = ParseTypeName(cart.InternalTypeName());
                string m = acName + "s remaining: " + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString("n1");

                if (player != null)
                {
                    Timeout(0.41, () =>
                    {
                        if (GamePlay != null) GamePlay.gpLogServer(new Player[] { player }, m, null);
                    });
                }
                else Console.WriteLine(m);                

                return m;

            }
            else return "";
        }catch (Exception ex)
        {
            AiCart cart = actor as AiCart;
            Console.WriteLine("Supply DisplayNumberOfAvailablePlanes ERROR: " + ex.ToString());
            if (cart != null && actor != null)
            {
                Console.WriteLine("Cart typename: {0}, actor.Army: {1}", cart.InternalTypeName(), actor.Army());
            }
                return "";
        }
    }

    public string DisplayNumberOfAvailablePlanes(int army = 0, Player player = null, bool display = false, bool html = false, string match = "")
    {
        try
        {


            double delay = 0;
            double add = 0;
            if (display) add = 0.2;
            string returnmsg = "";
            Player[] playerL = null;
            if (GamePlay != null) playerL = new Player[] { GamePlay.gpPlayer() }; //displays to SERVER only
            if (player != null) playerL = new Player[] { player };
            else display = false;

            string nl = Environment.NewLine;
            if (html) nl = "<br>" + nl;
            int low = 1;
            int high = 2;
            if (army == 1 || army == 2) { low = army; high = army; }
            for (int x = low; x <= high; x++)
            {
                string msg = ">>>>Available aircraft for " + ArmiesL[x];
                if (display)
                {
                    delay += add;
                    if (GamePlay != null) Timeout(delay, () => { GamePlay.gpLogServer(playerL, msg, null); });
                }
                returnmsg += msg + nl;
                int count = 1;
                foreach (KeyValuePair<string, double> current in AircraftSupply[(ArmiesE)x])
                {
                    if (match.Length > 0 && !current.Key.ToLowerInvariant().Contains(match.Trim().ToLowerInvariant())) continue; //implement substring matching "<stock hurri" etc
                    //string msg1 = ParseTypeName(current.Key) + ": " + current.Value.ToString("n1");
                    string msg1 = "#" + count.ToString("F0") + " " + current.Value.ToString("n0") + " " + ParseTypeName(current.Key);                   
                    //string msg1 = current.Key + ": " + current.Value.ToString("n1");
                    if (display && GamePlay != null)
                    {
                        delay += add;
                        Timeout(delay, () => { GamePlay.gpLogServer(playerL, msg1, null); });
                    }
                    returnmsg += msg1 + nl;
                    count++;
                }
            }
            return returnmsg;
        }catch (Exception ex)
        {
            Console.WriteLine("Supply DisplayNumber ERROR: " + ex.ToString());
            return "";
        }
    }


private int NumberPlayerInActor(AiActor actor)
    {
        int number = 0;

        AiCart cart = actor as AiCart;

        if (cart != null)
            for (int i = 0; i < cart.Places(); i++)
                if (cart.Player(i) != null)
                    number++;

        return number;
    }


    private AiAirport GetNearestAirfield(AiActor actor)
    {
        if (!(actor != null)) return null;

        AiAirport nearestAirfield = null;
        AiAirport[] airports = GamePlay.gpAirports();

        Point3d actorPos = actor.Pos();

        if (airports != null)
        {
            foreach (AiAirport airport in airports)
            {
                if (nearestAirfield != null)
                {
                    if (nearestAirfield.Pos().distance(ref actorPos) > airport.Pos().distance(ref actorPos))
                        nearestAirfield = airport;
                }
                else nearestAirfield = airport;
            }
        }
        return nearestAirfield;
    }


    private bool LandedOnAirfield(AiActor actor, AiAirport airport, double maxdistance)
    {
        if (actor == null || airport == null || !IsActorGrounded(actor)) return false;

        Point3d ActorPos = actor.Pos();

        if (airport.Pos().distance(ref ActorPos) < maxdistance)
            return true;
        return false;
    }


    private bool IsActorGrounded(AiActor actor)
    {
        bool onGround = false;
        AiAircraft aircraft = actor as AiAircraft;

        if (aircraft != null)
            if (aircraft.getParameter(ParameterTypes.Z_AltitudeAGL, -1) <= 15.0 //was 2 but that is not large enough for some aircraft.  2.6xxx for Beaufighter for exajple.
               || aircraft.getParameter(ParameterTypes.Z_VelocityTAS, -1) <= 2.0)
                onGround = true;

        return onGround;
    }


    private bool IsActorDamaged(AiActor actor)
    {
        foreach (AiActor ac in Battle.GetDamageVictims())
            if (ac == actor)
                return true;

        return false;
    }


    private string ParseTypeName(string typeName)
    {
        string[] tempString = null;
        string parsedName = "";
        tempString = typeName.Split('.');

        parsedName = tempString[1].Replace("_", " ");

        return parsedName;
    }


    public void CheckActorAvailability(Player player, AiActor actor, int placeIndex, bool AICheckout = false)
    {
        if (actor != null)
        {
            //we don't ever want to do this for the 2nd person entering an a/c
            //also, we keep sending people back through OnPlaceEnter repeatedly (on creating of a/c, on actually entering the place, a few other reasons)
            //because there is no harm bec. CheckActorOut will only process an aircraft at most once.  BUT . . . this routine also needs to 
            //avoid processing an aircraft at most once.
            //another alternative is an AI (cover aircraft) checkout, where there are zero players in the aircraft
            if ((NumberPlayerInActor(actor) == 1 && !aircraftCheckedOut.Contains(actor))                ||
                (AICheckout && NumberPlayerInActor(actor) == 0) )

            {
                //repairmission  business is so that if the stock of a particular a/c is completely 
                //depleted, you can still get ONE aircraft by spawning in at one of the Ferry airports
                //This allows you to then ferry a number of these planes to the front to add to supply
                //However, as soon as they a/c is spawned in we reduced supply by 1, so it will be like -1 instead of 0.
                //This keeps players from cheating by spawn in @ <ferry location, then just spawn  out &
                //a/c available have gone 0 to 1.  With our scheme it goes to -1 then when they spawn
                //back out it goes to 0 again.
                //If they FERRY it somewhere then objectiverepairmissions adds an EXTRA new plane to supply when they land,
                //So they'll end up with -1 + their aircraft returned + objectiverepairmission+1 added, total = 1 aircraft in 
                //supply.  Which is what we want.
                bool checkingOutFerryWithLimitDepleted = IsLimitReached(actor) && (player != null && mainmission.objectiverepairmission != null && mainmission.objectiverepairmission.isPointInFerryArea(player));

                if (!IsLimitReached(actor) || checkingOutFerryWithLimitDepleted)
                {
                    CheckActorOut(actor, player, AICheckout: AICheckout);
                    if (checkingOutFerryWithLimitDepleted)
                        addAircraftToSupplyFerry(player, Calcs.GetAircraftType(actor), howMany: -1);
                }
                else
                {
                    //So, first we were checking them out & then adding to the checkout_add list so they could be checked back in.  But, onactordestroyed doesn't
                    //actually call onplaceleave, not sure what happens with onplace leave there, but if we haven't check it out & just won't
                    //subtract any from stock here, it should work.
                    //aircraftCheckOut_add(actor);//Being rejected amounts to the same thing as being checked out, so we add the actor to the list for two reasons:
                    // #1. avoid double processing here or in CheckActorOut #2. CheckActorIn won't process the plane when the player is rejected, unless they are added to the aircraftCheckedOut list.
                    AiCart cart = actor as AiCart;

                    if (cart != null && IsArmy(actor))
                    {
                        GamePlay.gpHUDLogCenter(new Player[] { player }, "Stock of {0} depleted - This aircraft not available", new object[] { ParseTypeName(cart.InternalTypeName()) });
                        Timeout(3.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, "Stock of {0} depleted - This aircraft not available", new object[] { ParseTypeName(cart.InternalTypeName()) }); });
                        GamePlay.gpLogServer(new Player[] { player }, ">>>>>No stock of {0} remaining - please choose another aircraft. Check Mission Briefing, chat command <stock, Tab-4 menu for details.", new object[] { ParseTypeName(cart.InternalTypeName()) });

                        mainmission.statsmission.Display_AceAndRank_ByName(player); //testing
                        Console.WriteLine("valCAA1=" + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString() + " " + player.Name() + "{0:F0} {1:F0} {2:F0} {3} ", actor.Pos().z, actor.Pos().z, actor.Pos().z, player.Ping());
                        //if (AircraftSupply[(ArmiesE)actor.Army()].ContainsKey(cart.InternalTypeName()))
                        //    AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()] -= 1; // We somehow end up -2 stock lower than we started, so trying add 1 here to correct.

                        //TWCStatsMission.Stb_RemovePlayerFromAircraftandDestroy(actor as AiAircraft, player);
                        mainmission.statsmission.Stb_RemovePlayerFromAircraftSafelyandDestroy(actor as AiAircraft, player, fromSupply: true);

                        Console.WriteLine("valCAA2=" + AircraftSupply[(ArmiesE)actor.Army()][cart.InternalTypeName()].ToString() + " " + player.Name());
                        mainmission.statsmission.Display_AceAndRank_ByName(player); //testing
                        /*
                         * We'll use ours here
                        player.PlaceLeave(placeIndex); // does not work on Dedi correctly
                        Timeout(2, cart.Destroy);      // so destroy the plane
                        */

                    }
                }
            }
        }
    }


    private bool OverEnemyTerritory(AiActor actor)
    {
        if (actor == null || GamePlay == null ) return false;
        if (!GamePlay.gpFrontExist()) return false;

        return GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y) != actor.Army();
            
    }
	
	private bool OverFriendlyTerritory(AiActor actor)
    {
        if (actor == null || GamePlay == null ) return false;
        if (!GamePlay.gpFrontExist()) return false;

        return GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y) == actor.Army();
    }


    public void SupplyOnPlaceEnter(Player player, AiActor actor, int placeIndex=0)
    {
        //base.OnPlaceEnter(player, actor, placeIndex);        
        string ret = DisplayNumberOfAvailablePlanes(actor);
        if (ret == "") return;//"" returned if not a real place change or actor/player doesn't exist

        Console.WriteLine("PlaceEnter " + player.Name() + " " + (actor as AiCart).InternalTypeName());
        GamePlay.gpHUDLogCenter(new Player[] { player }, "", null); //clear the HUD, clears out old "not allowed aircraft" messages etc
        CheckActorAvailability(player, actor, placeIndex);
        //DisplayNumberOfAvailablePlanes(actor); //don't display it here bec we are sent here any time ie a bomber pilot changes positions.  Instead we'll show it the first time only, at CheckActorOut
        // DebugPrintNumberOfAvailablePlanes(); // for testing
        //DisplayNumberOfAvailablePlanes(0, player, true);
    }

    public void SupplyAICheckout(Player player, AiActor actor, int placeIndex = 0)
    {
        //base.OnPlaceEnter(player, actor, placeIndex);
        Console.WriteLine("AI Checkout " + player.Name() + " " + (actor as AiCart).InternalTypeName());
        DisplayNumberOfAvailablePlanes(actor);

        CheckActorAvailability(player, actor, placeIndex, AICheckout: true);
        //DisplayNumberOfAvailablePlanes(actor); //don't display it here bec we are sent here any time ie a bomber pilot changes positions.  Instead we'll show it the first time only, at CheckActorOut
        // DebugPrintNumberOfAvailablePlanes(); // for testing
        //DisplayNumberOfAvailablePlanes(0, player, true);
    }
	
	//This version required for ISupplyMission interface
	public void SupplyOnPlaceLeave(Player player, AiActor actor, int placeIndex = 0, bool softExit = false, double forceDamage = 0) {
		try {
			SupplyOnPlaceLeave(player, actor, placeIndex, softExit, forceDamage, reason: ""	);
		} catch (Exception ex) { Console.WriteLine("SupplyOnPlaceLeave1 ERROR: " + ex.ToString()); }
	}

    public void SupplyOnPlaceLeave(Player player, AiActor actor, int placeIndex = 0, bool softExit = false, double forceDamage = 0, string reason = "")
    {
        //base.OnPlaceLeave(player, actor, placeIndex);
        try
        {
            //So, sometimes Acotr is PLayer, AiPerson, AiGroundActor, & other weird things. 
            string playername = "Unknown";
            if (player != null && player.Name() != null) playername = player.Name();

            // We can only deal with actor==AiAicraft here.  So if it isn't, we try to get it two ways.
            if (!(actor is AiAircraft))
            {
                if (player.Place() != null && player.Place() is AiAircraft) actor = player.Place();
                else actor = lookup_aircraftCheckOut_ByPlayer(player);
            }

            //if act still isn't AiAircraft we just can't do anything.

            if (actor == null && !(actor is AiAircraft))
            {
                Console.WriteLine("Supply: PlaceLeave " + playername + ": Actor isn't an AiAircraft and player isn't in an aircraft and player doesn't have an a/c checked out.  The player's a/c may have been checked in already. Doing nothing, exiting.");
                return;
            }

			
	

            
          
		
			DisplayNumberOfAvailablePlanes(actor);
			AiAircraft aircraft = actor as AiAircraft;
			
			string destroyReason = "";
			
			if(mainmission.AircraftDestroyedList.ContainsKey(aircraft))  {
				destroyReason = mainmission.AircraftDestroyedList[aircraft];				
			}
			
			
			
			Console.WriteLine("Supply: PlaceLeave " + playername + " " + (actor as AiCart).InternalTypeName() + " {0} destroyReason: {1} ", reason, destroyReason);

			


                //So, sometimes we get an "all clear" onplaceleave but then a moment or two later realize, oh yeah the person actually died a horrible death.
                //-stats.cs figures things like that out and sends us a message.  We want to allow this "takeback" of the Check-in, but obviously
                //only do so one time per actor
                //Later we could force partial damage also with forceDamage between 0 & 1.
                if (forceDamage >= 1 && aircraftCheckedIn.Contains(actor) && !aircraftCheckedInButLaterKilled.Contains(actor))
                {
                    Console.WriteLine("SupOPL: Forcing check-out");
                    CheckActorOut(actor, player, Force: true);  //Force the re-checkout and loss of aircraft
                    aircraftCheckedInButLaterKilled.Add(actor); //make sure we can do this once only 
                    if (player != null) Task.Run(() => mainmission.MO_SpoilPlayerScoutPhotos(actor as Player));                   
                }

                double Z_AltitudeAGL = 0;
                if (aircraft != null) Z_AltitudeAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                //Only person in plane, low to ground (<5 meters, gives a bit of margin), landed at or near airfield, not in enemy territory. 
                //We could add in some scheme for damage later

                double altAGL_m_alt = Calcs.AltitudeAGL_m(actor);
				
				bool onWater = GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y) == LandTypes.WATER ;
				bool onEnemy = OverEnemyTerritory(actor);
				bool onFriendly = OverFriendlyTerritory(actor);
				double airport_distance = 3000;
				if (onWater) airport_distance = 7000;

                Console.WriteLine("Supply: NumPlayerInPlane: {0} AltAGL: {1:N2} AltAGL(alt): {6:N2} LandedonAirfield: {2} OverEnemyTerritory: {3} {4} Damage: {5} OnWater: {7} ", NumberPlayerInActor(actor), Z_AltitudeAGL, LandedOnAirfield(actor, GetNearestAirfield(actor), airport_distance), onEnemy, onFriendly,
                    forceDamage, altAGL_m_alt, onWater);
					
				AiAirport nearestAirfield = GetNearestAirfield(actor);	
				double nearestAirfield_distance_m = mainmission.Stb_distanceToNearestAirport(actor);
					
				bool loa = LandedOnAirfield(actor, nearestAirfield, 3000.0);
				bool recoverOffAirfield = false;
				if (!loa && !onWater && onFriendly && (new Random().Next() > forceDamage)) recoverOffAirfield = true; 

                if (NumberPlayerInActor(actor) == 0 && Z_AltitudeAGL < 15 
					&& (loa || recoverOffAirfield) && !onEnemy
                    && forceDamage < 1 /*&& !IsActorDamaged(actor)*/)
                {
                    if (forceDamage > 0 && forceDamage < 1)
                    {
                        Console.WriteLine("SupOPL: Check-in but with damage.  Reason: {0} destroyReason: {1}", reason, destroyReason);
						
                        double hoursToRepair = AddAircraftToDamagedSupply(player, actor, forceDamage, recoverOffAirfield, nearestAirfield_distance_m); //-1 if this damage already added
						
						if (recoverOffAirfield) {
							(GamePlay as GameDef).gameInterface.CmdExec("chat >>>Astonishing luck! A Maintenance Unit was able to recover your aircraft for repair. TO " + player.Name());
						}								
						
                        AiCart cart = actor as AiCart;
                        if (hoursToRepair > 0 && player != null)
                        {
							
                            string line1 = ParseTypeName(cart.InternalTypeName()) + " returned damaged; ";
                            string line2 = hoursToRepair.ToString("F1") + " hours required for repair and re-stock";
							if (recoverOffAirfield) line2 = hoursToRepair.ToString("F1") + " hours required for recovery, transport, repair and re-stock";

                            //Stb_Chat(line, player); //Sending this to stb_chat insures the player will see it on the flag screen (usual place they'll be after exiting an a/c); also it will be broadcast to all players...
                            //GamePlay.gpLogServer(new Player[] { player }, line, null);

                            
                            (GamePlay as GameDef).gameInterface.CmdExec("chat " + line1 + " TO " + player.Name()); //Sending this to chat insures the player will see it on the flag screen (usual place they'll be after exiting an a/c); also it will be broadcast to all players...
                            (GamePlay as GameDef).gameInterface.CmdExec("chat " + line2 + " TO " + player.Name()); 

                        }
                        double timeToRepair_sec = hoursToRepair * 60 * 60;
                        double timeLeft_sec = mainmission.calcTimeLeft_min() * 60;
                        if (timeToRepair_sec > timeLeft_sec - 60) timeToRepair_sec = timeLeft_sec - 60;
                        //So, the time to repair thing is not totally implemented acroos mission restarts but what we do is,
                        //the return of the a/c is delayed by the time to repair unless it is longer than the time remaining in session.  That is the upper limit.
                        //Also, because this is done on a timeout, there is the danger of losing this a/c if the server crashes between now & repair time (end of mission time).  So, that's the breaks.
                        Timeout(timeToRepair_sec + new Random().NextDouble()*30-15, () =>
                        {
                            Console.WriteLine("SupOPL: Check-in (delayed due to a/c damage)");
                            CheckActorIn(actor, player);
                        });

                        //The higher the damage rate the higher the chance of spoiling the photos.
                        if (new Random().NextDouble() < forceDamage) Task.Run(() => mainmission.MO_SpoilPlayerScoutPhotos(actor as Player));

                    }
                    else
                    {

                        Console.WriteLine("SupOPL: Check-in, no damage.  Reason: {0} destroyReason: {1}", reason, destroyReason);
                        CheckActorIn(actor, player);
                    }
                }
                else if (softExit) {
					Console.WriteLine("SupOPL: Check-in, softExit, checking aircraft in. Reason: {0} destroyReason: {1}", reason, destroyReason);
					CheckActorIn(actor, player); //softExit is ie when the mission ends.  In that case we don't penalize players if they are not back at airport, in enemy territory, high in the air, etc.
				} else {
					
					Console.WriteLine("Supply: Actor .destroyed, 100% damage reported. Will destroy unless 'reasons' indicate otherwise. Reason: {0} destroyReason: {1}", reason, destroyReason);
					
					if (reason.ToLower().StartsWith ("dead")) {
						//don't check-in
					} else if (reason.ToLower().StartsWith ("safe")) {
						CheckActorIn(actor, player);
					} else { //"DEPENDS" or don't know reason
					
						if (destroyReason.ToLower().StartsWith ("dead")) {

						//don't check-in
						} else if (destroyReason.ToLower().StartsWith ("safe")) {
							CheckActorIn(actor, player);
							
						} else {
							
						}
					}
					
					
				}

                //DisplayNumberOfAvailablePlanes(actor);

                /*
                 * We already do this elsewhere
                if (NumberPlayerInActor(actor) == 0)
                    if (actor is AiCart)
                        Timeout(5, () =>
                        {
                            if (actor as AiCart != null)
                                (actor as AiCart).Destroy();
                        });
                */
            
        } 
		catch (Exception ex) { Console.WriteLine("SupplyOnPlaceLeave ERROR: " + ex.ToString()); }
    }



    /*************************************************************************************
     * 
     * 
     * 
     * /****************************************************************
     * 
     * ADMIN PRIVILEGE
     * 
     * Determine if player is an admin, and what level
     * 
     ****************************************************************/
    public string[] admins_basic = new String[] { "TWC_", "Rostic" };
    public string[] admins_full = new String[] { "TWC_Flug", "TWC_Fatal_Error", "EvilUg", "Server" };

    public int admin_privilege_level(Player player)
    {
        if (player == null || player.Name() == null) return 0;
        string name = player.Name();
        //name = "TWC_muggle"; //for testing
        if (admins_full.Contains(name)) return 2; //full admin - must be exact character match (CASE SENSITIVE) to the name in admins_full
        if (admins_basic.Any(name.Contains)) return 1; //basic admin - player's name must INCLUDE the exact (CASE SENSITIVE) stub listed in admins_basic somewhere--beginning, end, middle, doesn't matter
        return 0;

    }

    void Mission_EventChat(Player from, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 

        Player player = from as Player;
        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;
        AiActor actor = aircraft as AiActor;

        string msg_orig = msg;
        msg = msg.ToLower();
        //Stb_Message(null, "Stats msg recvd.", null);

        /*
        if (msg.StartsWith("<!deban") && (admin_privilege_level(player) < 2))
        {

        }
        */
        if (msg.StartsWith("<suppadd") && (admin_privilege_level(player) > 1))
        {
            addAircraftToSupply(player, msg);

        }

        if (msg.StartsWith("<suppfile") && (admin_privilege_level(player) > 1))
        {
            string[] sections = msg_orig.Split(' ');
            if (sections.Length<=1) {
                GamePlay.gpLogServer(new Player[] { player }, "Supply <suppfile: No filename provided.  It must be in the same directory as the normal supply.ini file, and include NO spaces in the filename.", new object[] { });
                return;
            }
            string filename = "";
            
            for (int i = 1; i <sections.Length; i++) filename += sections[i] + " ";
            filename = filename.Trim();

            GamePlay.gpLogServer(new Player[] { player }, "Supply <suppfile: Reading supply file " + filename, new object[] { });
            ReadSupply(supplySuffix, filename, player);

        }
        

        else if (msg.StartsWith("<admin") && admin_privilege_level(player) > 1)// || msg.StartsWith("<"))
        {
            double to = 1.6; //make sure this comes AFTER the main mission, stats mission, <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5.2;

            string msg41 = "<suppadd 1 3 20 - add 20 planes to army 1 aircraft #3.  Get aircraft # from supply listing.";            

            Timeout(to, () => { GamePlay.gpLogServer(new Player[] { player }, msg41, new object[] { }); });
            string msg42 = "<suppadd 1 beau 20 - add 20 planes to army 1 aircraft 'Beaufighter'.  Will match 1st plane on list.";
            Timeout(to + 0.05, () => { GamePlay.gpLogServer(new Player[] { player }, msg42, new object[] { }); });

            string msg43 = "<suppfile myfilename.ini - loads a new .ini file with supply & increase values, to replace the current values";
            Timeout(to + 0.05, () => { GamePlay.gpLogServer(new Player[] { player }, msg43, new object[] { }); });
            //ReadSupply(string suffix, string replacementFilename = "", Player player= null)
            //GamePlay.gp(, from);
        }
    }

    /********************************************************************************************************
     *  END chat commands
     * *******************************************************************************************************/


}

namespace Ini
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// https://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C
    /// </summary>
    public class SupplyIniFile
    {
        public string path;
        //public Mission mission;
        public int iniErrorCount;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /*
        [DllImport("kernel32.dll")]
        public static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);
        */

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <param name="INIPath"></param>
        //public IniFile(string INIPath, Mission msn)
        public SupplyIniFile(string INIPath)
        {
            path = INIPath;
            //mission = msn;
            iniErrorCount = 0;

        }
        public void IniDeleteSection(string Section)
        {
            WritePrivateProfileString(Section, null, null, this.path);
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <param name="Section"></param>
        /// Section name
        /// <param name="Key"></param>
        /// Key Name
        /// <param name="Value"></param>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        public void IniWriteList(string Section, string Key, List<string> Value)
        {
            int count = 0;
            WritePrivateProfileString(Section, "Count", Value.Count.ToString(), this.path);
            foreach (string s in Value)
            {
                WritePrivateProfileString(Section, Key + "[" + count.ToString() + "]", s, this.path);
                count++;
            }
        }
        public void IniWriteDictionary(string Section, Dictionary<string,double> Value)
        {
            int count = 0;           
            //foreach (string s in Value)
            foreach (KeyValuePair<string, double> pair in Value)
            {
                //Console.WriteLine("SKV: " + Section + " " + pair.Key + " " + pair.Value.ToString());
                WritePrivateProfileString(Section, pair.Key, pair.Value.ToString(), this.path);
                count++;
            }
        }
        //So, this will ONLY read values in the .ini file already in the defined dictionary,
        //and the value in the defined dictionary (above) is the default value for that key   
        public Dictionary<string, double> IniReadDictionary(string Section, Dictionary<string, double> deflt)
        {
            Dictionary<string, double> ret = new Dictionary<string, double>(deflt);
            foreach (KeyValuePair<string,double> entry in deflt)
            {
                //double val = Convert.ToDouble(IniReadValue(Section, s, ""));


                double val = Convert.ToDouble(IniReadValue(Section, entry.Key, entry.Value));
                ret[entry.Key]=val;                
                
                //Console.WriteLine("SKV Read: " + Section + " " + entry.Key + " " + val.ToString());
            }

            return ret;
        }
        /*//Reads in the entire section with key->dict.Key and value->dict.Value
        public Dictionary<string, double> IniReadDictionary(string Section, Dictionary<string, double> deflt)
        {

            //Dictionary<string, string> resList = new Dictionary<string, string>();
            Dictionary<string, double> retList = new Dictionary<string, double>();


            //resList = GetIniSection(Section, path);

            string[] resList =GetAllKeysInIniFileSection(Section, path);

           
            if (resList == null || resList.Length == 0) Console.WriteLine("It's null!");
            if (resList == null || resList.Length == 0) return deflt; //Only in case there are NO keys in the section do we use the default dictionary

            //int total = IniReadValue(Section, "Count", (int)0);
            //var d = new Dictionary<string, double>();
            //foreach (string s in resList)
            for (int x=0; x<resList.Length; x++)
            {
                //double val = Convert.ToDouble(IniReadValue(Section, s, ""));
                
                string[] spl = resList[x].Split('=');
                Console.WriteLine("SKV Spl: " + Section + " " + spl[0] + " " + spl[1]);
                if (spl[0].Length == 0 || spl[0].Length > 150) continue;
                double d = Convert.ToDouble(spl[1]);
                retList[spl[0]] = d ;
                Console.WriteLine("SKV Read: " + Section + " " + spl[0] + " " + spl[1]);
            }

            return retList;
        }*/
/*
        //Reads in the entire section with key->dict.Key and value->dict.Value
        public Dictionary<string, double> IniReadDictionary(string Section, Dictionary<string, double> deflt)
        {

            Dictionary<string,string> resList= new Dictionary<string,string>();
            Dictionary<string, double> retList = new Dictionary<string, double>();

            resList = GetIniSection(Section, path);

  
            if (resList == null || resList.Count == 0) Console.WriteLine("It's null!");
            if (resList == null || resList.Count==0) return deflt; //Only in case there are NO keys in the section do we use the default dictionary

            //int total = IniReadValue(Section, "Count", (int)0);
            //var d = new Dictionary<string, double>();
            foreach (KeyValuePair<string,string> entry in resList)
            {
                double val = Convert.ToDouble(entry.Value);
                retList.Add(entry.Key, val);
                Console.WriteLine("SKV: " + Section + " " + entry.Key + " " + val.ToString());
            }

            return retList;
        }
*/
/*
        //Reads in the entire section with key->dict.Key and value->dict.Value
        public Dictionary<string,double> IniReadDictionary(string Section, Dictionary<string,double> deflt)
        {
            List<string> l = IniReadKeys(Section);
            if (l == null) Console.WriteLine("It's null!");
            if (l==null) return deflt; //Only in case there are NO keys in the section do we use the default dictionary

            //int total = IniReadValue(Section, "Count", (int)0);
            var d = new Dictionary<string, double>();
            foreach (string s in l) {
                double val = Convert.ToDouble(IniReadValue(Section, s, ""));
                d.Add(s, val);
                Console.WriteLine("SKV: " + Section + " " + s + " " + val.ToString());
            }

            return d;
        }
        public List<string> IniReadKeys(string Section)
        {
            List<string> l = new List<string>();

            //int total = IniReadValue(Section, "Count", (int)0);

            StringBuilder temp = new StringBuilder(30000);

            //if sent with key==null returns long string with all keys delimited by \0 and with two \0s at the end to delimit the end
            int i = GetPrivateProfileString(Section, null, "", temp, 30000, this.path);
            if (temp.Length > 0) {

                Console.WriteLine("Keys: " + temp.Length.ToString() + " : " + temp);
                l = temp.ToString().Split('\x00').ToList<string>();
                if (l.Count > 1) l.RemoveRange(l.Count - 2, 2);
                else return new List<string>();
                return l;
            }
            else
            {
                IniReadError(Section);
                return null;
            }
              
        }
*/
        /// <summary> Return an entire INI section as a list of lines.  Blank lines are ignored and all spaces around the = are also removed. </summary>
        /// <param name="section">[Section]</param>
        /// <param name="file">INI File</param>
        /// <returns> List of lines </returns>
        /*public static Dictionary<string, string> GetIniSection(string section, string file)
        {
            var result = new Dictionary<string, string>();
            string[] iniLines;
            if (GetPrivateProfileSection(section, file, out iniLines, file))
            {
                foreach (var line in iniLines)
                {
                    var m = Regex.Match(line, @"^([^=]+)\s*=\s*(.*)");
                    result.Add(m.Success
                                   ? result[m.Groups[1].Value]=m.Groups[2].Value
                                   : result[line]="");
                }
            }

            return result;
        }*/
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern UInt32 GetPrivateProfileSection
                    (
                        [In] [MarshalAs(UnmanagedType.LPStr)] string strSectionName,
                        // Note that because the key/value pars are returned as null-terminated
                        // strings with the last string followed by 2 null-characters, we cannot
                        // use StringBuilder.
                        [In] IntPtr pReturnedString,
                        [In] UInt32 nSize,
                        [In] [MarshalAs(UnmanagedType.LPStr)] string strFileName
                    );

        private static string[] GetAllKeysInIniFileSection(string strSectionName, string strIniFileName)
        {
            // Allocate in unmanaged memory a buffer of suitable size.
            // I have specified here the max size of 32767 as documentated
            // in MSDN.
            IntPtr pBuffer = Marshal.AllocHGlobal(32767);
            // Start with an array of 1 string only.
            // Will embellish as we go along.
            string[] strArray = new string[0];
            UInt32 uiNumCharCopied = 0;

            uiNumCharCopied = GetPrivateProfileSection(strSectionName, pBuffer, 256, strIniFileName);

            // iStartAddress will point to the first character of the buffer,
            int iStartAddress = pBuffer.ToInt32();
            // iEndAddress will point to the last null char in the buffer.
            int iEndAddress = iStartAddress + (int)uiNumCharCopied;

            // Navigate through pBuffer.
            while (iStartAddress < iEndAddress)
            {
                // Determine the current size of the array.
                int iArrayCurrentSize = strArray.Length;
                // Increment the size of the string array by 1.
                Array.Resize<string>(ref strArray, iArrayCurrentSize + 1);
                // Get the current string which starts at "iStartAddress".
                string strCurrent = Marshal.PtrToStringAnsi(new IntPtr(iStartAddress));
                // Insert "strCurrent" into the string array.
                strArray[iArrayCurrentSize] = strCurrent;
                // Make "iStartAddress" point to the next string.
                iStartAddress += (strCurrent.Length + 1);
                //Console.WriteLine("strCurrent:" +strCurrent);
            }

            Marshal.FreeHGlobal(pBuffer);
            pBuffer = IntPtr.Zero;

            return strArray;
        }

        static void Main(string[] args)
        {
            string[] strArray = GetAllKeysInIniFileSection("Section", "<path to INI file>");

            for (int i = 0; i < strArray.Length; i++)
            {
                Console.WriteLine("{0:S}", strArray[i]);
            }
        }
    
    public List<string> IniReadList(string Section, string Key)
        {
            List<string> l = new List<string>();

            int total = IniReadValue(Section, "Count", (int)0);

            if (total == 0) return l;
            for (int x = 0; x < total; x++)
            {

                l.Add(IniReadValue(Section, Key + "[" + x.ToString() + "]", ""));
            }

            return l;
        }



        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        //overloaded for string, int, double, bool.  Could do others like single, float, whatever.  String[] int[] double[] etc.
        public string IniReadValue(string Section, string Key, string def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            if (temp.Length > 0) return temp.ToString();
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public int IniReadValue(string Section, string Key, int def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            int a;
            if (temp.Length > 0 && int.TryParse(temp.ToString(), out a)) return a;
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public double IniReadValue(string Section, string Key, double def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            double a;
            if (temp.Length > 0 && double.TryParse(temp.ToString(), out a)) return a;
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public bool IniReadValue(string Section, string Key, bool def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            if (temp.ToString().Trim() == "1") temp = new StringBuilder("True", 4); //allow 0 & 1 to be used, or True/true/False/false
            if (temp.ToString().Trim() == "0") temp = new StringBuilder("False", 5);
            bool a;
            if (temp.Length > 0 && bool.TryParse(temp.ToString(), out a)) return a;
            else
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        private void IniReadError(String Section, String Key = "(none)")
        {
            iniErrorCount++;
            Console.WriteLine("-supply.cs: ERROR reading .ini file: Key {0} in Section {1} was not found.", Key, Section);

        }
    }
}
