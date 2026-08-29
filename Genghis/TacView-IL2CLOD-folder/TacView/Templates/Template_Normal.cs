//$reference TacviewRecorder.dll
using TacviewRecorder;

public class Mission : TacviewMission
{
	public override void Inited()
	{
		MissionType = TypeOfMission.Normal;
		base.Inited();
	}
}
