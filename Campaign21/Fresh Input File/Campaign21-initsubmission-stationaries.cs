using System;
using System.Collections.Generic;
using maddox.GP;
using maddox.game;
using maddox.game.world;



public class Mission : AMission
{

    Dictionary<GroundStationary, int> Stationaries = new Dictionary<GroundStationary, int>();

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        MissionNumberListener = -1;
    }


    public override void OnMissionLoaded(int missionNumber)
    {
        base.OnMissionLoaded(missionNumber);

        foreach (GroundStationary stationary in GamePlay.gpGroundStationarys())
        {
            if (!Stationaries.ContainsKey(stationary))
            {
                Stationaries[stationary] = missionNumber;
                Console.WriteLine("Stat: {0} {1} {2} {3}", missionNumber, stationary.Name, stationary.Category, stationary.Title);
            }
        }
    }
}
