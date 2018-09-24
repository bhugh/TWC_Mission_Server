#define DEBUG  
#define TRACE  
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
        //public dynamic Main { get; set; }
        //public dynamic Stats { get; set; }

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
}

public interface IMainMission
{
}

/*
public abstract class SMission : AMission, IStatsMission
{
    public string stb_LocalMissionIniDirectory { get; set; }              // Interface property (not implemented by definition)
    public abstract int ot_GetCivilianBombings(string name);
}
*/
