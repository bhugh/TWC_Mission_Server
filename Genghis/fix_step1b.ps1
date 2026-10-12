$c = Get-Content Genghis-Class-CoverMission.cs -Raw
$c = $c -replace "tasktarget = playerAirGroup;`r`n`r`n                // FIX", "// FIX"
[IO.File]::WriteAllText('Genghis-Class-CoverMission.cs', $c)
'DONE'