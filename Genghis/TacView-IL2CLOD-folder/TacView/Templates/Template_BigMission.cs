//$reference TacviewRecorder.dll
using TacviewRecorder;

public class Mission : TacviewMission
{
	public override void Inited()
	{
		MissionType = TypeOfMission.BigMission;
		base.Inited();
	}
}
