//HOW TO ADD TRIGGERS FROM A SUBMISSION TO THE MAIN MISSION
//by  ATAG_Oskar
//2020-08-24


// First add this line in your main mission.

public override void OnBattleStarted()
{
DataDictionary.Add("MAIN.MISSION", this);
}




// Then add this to your sub-mission.

Mission mainMission;

public override void OnBattleStarted()
{
mainMission = (Mission)DataDictionary["MAIN.MISSON"];
}


public override void OnTrigger(int missionNumber, string shortName, bool active)
{
mainMission.OnTrigger(missionNumber, shortName, active);
} 