$content = Get-Content Genghis-Class-CoverMission.cs -Raw
$old = "aawpt = AiAirWayPointType.FOLLOW; //SEtting to Escort seems to make them drop their bombs?  Maybe?`r`n                                                  //aawpt = AiAirWayPointType.ESCORT; //SEtting to Escort seems to make them drop their bombs?  Maybe?`r`n\t\t\t\ttasktarget = playerAirGroup;\t\t\t\t\t\t\t\t  `r`n`r`n                //2021/07 - also we tried setting the task below, the a/c are not behaving, trying to remove the task"
$new = "aawpt = AiAirWayPointType.FOLLOW; //SEtting to Escort seems to make them drop their bombs?  Maybe?`r`n                                                  //aawpt = AiAirWayPointType.ESCORT; //SEtting to Escort seems to make them drop their bombs?  Maybe?`r`n                 // FIX: Prevent cover aircraft from attacking their own player when >4000m away.`r`n                // The FOLLOW waypoint already brings them back; FLY_WAYPOINT clears any stale ATTACK_AIR task.`r`n                task = AiAirGroupTask.FLY_WAYPOINT;`r`n                tasktarget = null;`r`n`r`n                //2021/07 - also we tried setting the task below, the a/c are not behaving, trying to remove the task"
if ($content.Contains($old)) {
    $content = $content.Replace($old, $new)
    [System.IO.File]::WriteAllText('Genghis-Class-CoverMission.cs', $content)
    'SUCCESS'
} else {
    'OLD TEXT NOT FOUND'
}