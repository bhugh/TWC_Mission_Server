Mission mainMission;

public override void OnBattleStarted()
{
mainMission = (Mission)DataDictionary["MAIN.MISSON"];
}


public override void OnTrigger(int missionNumber, string shortName, bool active)
{
mainMission.OnTrigger(missionNumber, shortName, active);
} 
