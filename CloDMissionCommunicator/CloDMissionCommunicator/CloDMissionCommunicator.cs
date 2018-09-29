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

        //public dynamic Main { get; set; }
        //public dynamic Stats { get; set; }
        public string stb_FullPath { get; set; }
        public string string1 { get; set; }
        public string string2 { get; set; }
        public string string3 { get; set; }
        public object object1 { get; set; }
        public object object2 { get; set; }
        public object object3 { get; set; }
    }

}


public interface IStatsMission
{
    string stb_LocalMissionIniDirectory { get; set; }              // Interface property (not implemented by definition)
    //string stb_LocalMissionIniDirectory;              // Interface property (not implemented by definition)
    int ot_GetCivilianBombings(string name);
    void Display_AceAndRank_ByName(Player player); // <career
    void Display_SessionStats(Player player); // <ses
    string Display_SessionStatsAll(Player player, int side = 0, bool display = false); // <net
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

}

public interface ISupplyMission
{
    string DisplayNumberOfAvailablePlanes(int army = 0, Player player = null, bool display = false, bool html = false, string match = "");
    void SupplyOnPlaceEnter(Player player, AiActor actor, int placeIndex = 0 );
    void SupplyOnPlaceLeave(Player player, AiActor actor, int placeIndex = 0, bool softExit = false, double forceDamage = 0);
    bool SupplyEndMission(double redMult = 1, double blueMult = 1);
    bool SupplySaveStatus();

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
