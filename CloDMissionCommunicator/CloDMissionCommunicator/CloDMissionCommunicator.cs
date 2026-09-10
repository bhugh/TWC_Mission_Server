//#define DEBUG  
//#define TRACE  
//#undef TRACE

///$reference parts/core/Strategy.dll
///$reference parts/core/gamePlay.dll
///$reference parts/core/gamePages.dll
///$reference System.Core.dll 

using System;
using System.Collections.Generic;
using System.Globalization;
//using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
//using System.Net;
using System.ComponentModel;
//using System.Data;
//using System.Core;
using System.Linq;
using maddox.GP;
using maddox.game;
using maddox.game.world; 
using maddox.game.play;
using maddox.game.page;
using part;

namespace TWCComms
{
    //public int comms = 2;
    //public List<Mission> missionsList = new List<Mission>();   
    public sealed class Communicator
    {
        private static readonly Communicator instance = new Communicator();
        // Explicit static constructor to tell C# compiler  
        // not to mark type as beforefieldinit  
        static Communicator()
        {
        }
        private Communicator()
        {
        }
        public static Communicator Instance
        {
            get
            {
                return instance;
            }
        }
        public IMainMission Main { get; set; }        
        public IStatsMission Stats { get; set; }
        public ISupplyMission Supply { get; set; }
        public IKnickebeinMission Knickebein { get; set; }
        public ICoverMission Cover { get; set; }

        //public dynamic Main { get; set; }
        //public dynamic Stats { get; set; }
        public bool WARP_CHECK { get; set; } //show 'warp check' messages, most to console some to screen
        public string stb_FullPath { get; set; }
        public string[] string1 { get; set; }
        public object[] object1 { get; set; }
        public bool[] bool1 { get; set; }
        public int[] int1 { get; set; }
        public double[] double1 { get; set; }
    }

}

public interface IStbStatRecorder
{
    string StbSr_MassagePlayername(string playerName, AiActor actor = null);
    int StbSr_RankAsIntFromName(string playerName);
    string StbSr_RankNameFromInt(int rank, int army = 1);
    string StbSr_RankFromName(string playerName, AiActor actor = null, bool highest = false);
    int StbSr_NumberOfKills(string playername);
}

public interface IStbSaveIPlayerStat
{
    void StbSis_AddToMissionStat(Player player, int index, int value);//mission stats are the total overall stats for a player's career; also saves to current session red/blue stat totals
    void StbSis_IncrementSessStat(Player player, int index); //Session stats are just a few selected stats per player and only for this session.
    void StbSis_AddSessStat(Player player, int index, int value);
}

public interface IStatsMission
{
    //IStbStatRecorder stb_StatRecorder { get; set; }
    IStbStatRecorder stb_IStatRecorder { get; set; }
    IStbSaveIPlayerStat stb_ISaveIPlayerStat { get; set; }
    string stb_LocalMissionIniDirectory { get; set; }              // Interface property (not implemented by definition)
    //string stb_LocalMissionIniDirectory;              // Interface property (not implemented by definition)
    int ot_GetCivilianBombings(string name);
    void Display_AceAndRank_ByName(Player player); // <career
    string Display_AircraftAvailable_ByName(Player player, bool nextAC = false, bool display = true, bool html = false); //<ac
    void Display_SessionStats(Player player); // <ses
    string Display_SessionStatsAll(Player player, int side = 0, bool display = false, bool html = true); // <net
    //if player sent, displays message to the player, if player==null just return the string (html formatted with <br>)
    string Display_SessionStatsTeam(Player player); // <obj
    bool Stb_isAiControlledPlane(AiAircraft aircraft);
    void Stb_RemovePlayerFromCart(AiCart cart, Player player = null);
    void Stb_RemovePlayerFromAircraftandDestroy(AiAircraft aircraft, Player player, double timeToRemove_sec = 1.0, double timetoDestroy_sec = 3.0);
    void Stb_RemoveAllPlayersFromAircraft(AiAircraft aircraft, double timeToRemove_sec = 1.0);
    void gpLogServerAndLog(Player[] to, object data, object[] third = null);

}

public interface IMainMission
{
    string MISSION_ID { get; set; }
    string CAMPAIGN_ID { get; set; }
    string SERVER_ID { get; set; }
    string SERVER_ID_SHORT { get; set; }
    string CLOD_PATH { get; set; }
    string FILE_PATH { get; set; }
    string STATS_FULL_PATH { get; set; }
    string STATSCS_FULL_PATH { get; set; }

    bool DEBUG { get; set; } //Whether to print debug messages (most to console, some to screen
    bool LOG { get; set; } //Whether to log debug messages to  a log file.

    Dictionary<AiAirGroup, SortedDictionary<string, IAiAirGroupRadarInfo>> ai_radar_info_store { get; set; }
    //CircularArray<Dictionary<AiAirGroup, IAirGroupInfo>> airGroupInfoCircArr { get; set; }

    Dictionary<string, IMissionObjective> SMissionObjectivesList(); //Stub of MissionObjectivesList with only the actually important & unchanging things in it

}

public interface IAiAirGroupRadarInfo
{
    double time { get; set; } //Battle.time.current;
                                     //public SortedDictionary<string, AirGroupInfo> interceptList {get; set;}
    IAirGroupInfo agi { get; set; }  //airgroup for TARGET airgroup
    IAirGroupInfo pagi { get; set; } //airgroup info for SOURCE airgroup (ie the 'player' or the one that will be targeting the TARGET
    Point3d interceptPoint { get; set; } //intcpt, with x,y as location and z as intcpt time in seconds
    bool climbPossible { get; set; } //climb_possible        
    AMission mission { get; set; }


}

public enum aiorhuman { AI, Mixed, Human };

public interface IAirGroupInfo
{
    double time { get; set; } //Battle.time.current;
    HashSet<AiAirGroup> nearbyAirGroups { get; set; } //those groups that are nearby OR near any nearby aircraft of the same type (greedy)
    HashSet<AiAirGroup> groupedAirGroups { get; set; } //groups that have been nearby for that past X iterations, thus counting as part of the same Group
    Point3d pos { get; set; }
    Point3d vel { get; set; }
    bool belowRadar { get; set; }
    double altAGL_ft { get; set; }
    double altAGL_m { get; set; }
    int count { get; set; }
    string type { get; set; }
    bool isHeavyBomber { get; set; }
    bool isAI { get; set; }
    string playerNames { get; set; }
    AiActor actor { get; set; }
    AiAirGroup airGroup { get; set; }
    bool isLeader { get; set; }
    AiAirGroup leader { get; set; }
    string sector { get; set; }
    string sectorKeyp { get; set; }
    int giantKeypad { get; set; }




    //Above are individual airgroup/aircraft values - below are the composite values for the entire airgroup ("Air Group Grouping" - AGG) - in case this is the leader of the grouping.  Otherwise blank/default
    Point3d AGGpos { get; set; }    //exact loc of the primary a/c
    Point3d AGGavePos { get; set; } //average loc of all a/c      
    string AGGsector { get; set; }
    string AGGsectorKeyp { get; set; }
    int AGGgiantKeypad { get; set; }
    Point3d AGGvel { get; set; }
    int AGGcount { get; set; } //total # in group, including all above & below radar
    int AGGcountAboveRadar { get; set; } //if countAboveRadar is 0 this group won't show up at all.  This is the count that shows to ordinary players
    int AGGcountBelowRadar { get; set; }
    bool AGGradarDropout { get; set; }
    double AGGminAlt_m { get; set; }
    double AGGmaxAlt_m { get; set; }
    double AGGaveAlt_m { get; set; }
    double AGGavealtAGL_ft { get; set; }
    string AGGtypeNames { get; set; }
    string AGGplayerNames { get; set; }
    string AGGids { get; set; }  //the actor.Name()s compiled into a string
    aiorhuman AGGAIorHuman { get; set; }
    string AGGtype { get; set; }    //the actual type: "F" or "B".
    string AGGmixupType { get; set; } //the type that will actually display on user radar, which is sometimes/often "mixed up".  "F" "B" or "U" for unknown
    bool AGGisHeavyBomber { get; set; }
    AMission mission { get; set; }

}

public interface IMissionObjective
{

    string ID { get; set; } //unique name, often the Triggername or static name
    string Name { get; set; } //Name the will be displayed to the user in messages etc
    int AttackingArmy { get; set; } // Army this is an objective for (ie, whose task is to destroy it); can be 1=red, 2=blue,0=none
    int OwnerArmy { get; set; } // Army that owns this object (ie, is harmed if it is destroyed)
    bool IsEnabled { get; set; } //TODO: This is only partially implemented.  But could be used in case of bad data or whatever to just disable the objective.
    bool IsPrimaryTarget { get; set; } //One of the primary/required targets for this mission?
    double Points { get; set; }
    bool Destroyed { get; set; }
    double DestroyedPercent { get; set; } //some items can potentially be partially destroyed, it 50% = 0.5, 100%=1.0; also higher numbers possible such as 200% = 2.0;
    bool ObjectiveAchievedForPoints { get; set; } //We set this when one side destroys an objective & gets the points for it.  We can then remove this when that side
                                                         //turns the map so that they have a clean slate of new objectives.  But the points is independent of whether
                                                         //it is actually still destroyed or not. 
    DateTime? TimeToUndestroy_UTC { get; set; } //if destroyed, what time UTC is the d/t it should be undestroyed/repaired.  This is in real time, ie if item destroyed 10pm, Tues Jan 24, 2022 UTC 
    DateTime? LastHitTime_UTC { get; set; } //Last time target attacked/hit
    bool Scouted { get; set; } //whether or not it has been scouted or reconnoitered by the enemy; if so they can get access to exact coordinates etc
                                      //Dictionary<Player,int> PlayersWhoScouted { get; set; } //list of any players who have scouted this objective //Player is not serializable, alas.
    Point3d Pos { get; set; }
    double radius { get; set; } //extent of the object, ie, for airfields, how far center to the perimeter.  This is more FYI as info about the object, although it can be used to determine whether hits to a certain object are effective, or how effective (as we do with airfields)
    string Sector { get; set; }
    string bigSector { get; set; } //a block of several sectors, somewhat randomly selected, and the target is somewhere within it
    string HUDMessage { get; set; }
    string LOGMessage { get; set; }
    double RadarEffectiveRadius { get; set; }
    double TriggerDestroyRadius { get; set; } //What is set in the .mis file as the effective radius for this trigger object.  This is slightly different from "radius" in that it is a number used only internally to determine if the destroy objects are within the effective area of the trigger.
    double OrdnanceRequiredToTrigger_kg { get; set; } //For type PointArea
    double OrdnanceOnTarget_kg { get; set; } //For PointArea & similar - how many KG ordnance have hit this target already
    double ObjectsRequiredToTrigger_num { get; set; } ////For type JerryCanArea: buildings, static objects, etc within the given radius required to trigger it.  Using actual number instead of percent, because I don't think we cna get the listing of now many buildings etc in a given area. NOT IMPLEMENTED YET
    double ObjectsDestroyed_num { get; set; } //how many objects have been destroyed so far. NOT IMPLEMENTED YET
    List<string> StaticNames { get; set; } //for static targets, the list of static names that will determine if the target is destroyed
    double StaticPercentageRequired { get; set; } //what percentage of those static targets must be destroyed to eliminate the objective
}



public enum ArmiesE { None, Red, Blue };

public interface ISupplyMission
{    
    Dictionary<ArmiesE, Dictionary<string, double>> AircraftSupply { get; set; }
    Dictionary<ArmiesE, Dictionary<string, double>> AircraftIncrease { get; set; }
    HashSet<AiActor> aircraftCheckedOut { get; set; }
    Dictionary<AiActor, Tuple<int, string, string, DateTime>> aircraftCheckedOutInfo { get; set; } //Info about each a/c that is checked out <Army, Pilot name(s), Aircraft Type, time checked out>
    HashSet<AiActor> aircraftCheckedIn { get; set; }//set of AiActor, to guarantee each Actor checked IN once only
    HashSet<AiActor> aircraftCheckedInButLaterKilled { get; set; }  //set of AiActor, to guarantee actors which were first reported AOK but later turned out to be killed, are able to be killed later & removed from the active a/c list, but ONCE ONLY
    string DisplayNumberOfAvailablePlanes(int army = 0, Player player = null, bool display = false, bool html = false, string match = "");
    string ListAircraftLost(int army = 0, Player player = null, bool display = true, bool html = false, string match = "", string playerNameMatch = "");
    bool IsLimitReached(AiActor actor);
    bool IsLimitReached(string internalTypeName, int army);
    int AircraftStockRemaining(AiActor actor);
    int AircraftStockRemaining(string internalTypeName, int army);
    HashSet<AiActor> AircraftActorsCurrentlyInAir();
    void SupplyOnPlaceEnter(Player player, AiActor actor, int placeIndex = 0 );
    void SupplyAICheckout(Player player, AiActor actor, int placeIndex = 0);
    void SupplyOnPlaceLeave(Player player, AiActor actor, int placeIndex = 0, bool softExit = false, double forceDamage = 0);
    bool SupplyEndMission(double redMult = 1, double blueMult = 1);
    bool SupplySaveStatus();

}

public interface IKnickebeinMission
{
    void KniOnStartOrNext(Player player);
    void KniNext(Player player);
    void KniPrev(Player player);
    void KniList(Player player);
    void KniStop(Player player);
    void KniStart(Player player);
    void KniInfo(Player player);
    Point3d KniPoint(Player player);    
    void KniDelete(Player player, int waypointToDelete);   
}

public interface ICoverMission
{
    void checkoutCoverAircraft(Player player, string aircraftName);
    void landCoverAircraft(Player player);
    string listPositionCurrentCoverAircraft(Player player = null, bool display = true, bool html = false);
    string listCoverAircraftCurrentlyAvailable(ArmiesE army, Player player = null, bool display = true, bool html = false);

}

/*
public abstract class AIniFile
{

    public abstract void IniNew (string INIPath) { }

    public abstract void IniDeleteSection(string Section);

    public abstract void IniWriteValue(string Section, string Key, string Value);

    public abstract void IniWriteList(string Section, string Key, List<string> Value);

    public abstract List<string> IniReadList(string Section, string Key);

    //overloaded for string, int, double, bool.  Could do others like single, float, whatever.  String[] int[] double[] etc.
    public abstract string IniReadValue(string Section, string Key, string def);

    public abstract int IniReadValue(string Section, string Key, int def);

    public abstract double IniReadValue(string Section, string Key, double def);

    public abstract bool IniReadValue(string Section, string Key, bool def);
}
*/

/*
public abstract class SMission : AMission, IStatsMission
{
    public string stb_LocalMissionIniDirectory { get; set; }              // Interface property (not implemented by definition)
    public abstract int ot_GetCivilianBombings(string name);
}
*/

/*


*/
