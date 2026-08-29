<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Tacview Mission Downloads</title>
    <style>
        body { font-family: sans-serif; margin: 40px; background: #f4f6f9; color: #333; }
        h1 { color: #2c3e50; }
        ul { list-style: none; padding: 0; }
        li { background: white; margin: 8px 0; padding: 12px; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); display: flex; justify-content: space-between; }
        a { color: #3498db; text-decoration: none; font-weight: bold; }
        a:hover { text-decoration: underline; }
        .size { color: #7f8c8d; font-size: 0.9em; }
    </style>
</head>
<body>

    <h1>TWC Campaign Server - Tacview Files From Recent Sessions</h1>
    <p>Open & view the files with the <a href="https://www.tacview.net/">Tacview software</a>.</p>
    <p>Filename is the date and time the session started, in <a href="https://www.utctime.net/utc-time-zone-converter">UTC</a>.</p>
    <p>If the file is very small, it likely does not contain useful data - just a quick server re-start or turnover.</p>
    <p><i>Files are kept for 2 months, then deleted.</i></p>
    <p></p>
    
   <h3>Tacview Files for Download</h3>  
    <ul>
      <?php
          
        //******** DELETE OLD FILES > 1 month old ********
        
        
           // 1. Update pattern to use curly braces and comma-separated extensions
          $dir = __DIR__ . '/*.{zip.acmi,txt.acmi}';

          // 2. Calculate the cutoff timestamp for exactly 2 months ago
          $cutoffDate = new DateTime('-2 month');
          $cutoffTimestamp = $cutoffDate->getTimestamp();

          // 3. Find all matching files using the GLOB_BRACE flag
          $files = glob($dir, GLOB_BRACE);
          //echo $files;
          
          

          if (!empty($files)) {
              foreach ($files as $filePath) {
                  // Get just the filename (e.g., "2026-08-27@20.32.22Z.zip.acmi")
                  $fileName = basename($filePath);
                  
                  
                  // Explode by the '@' symbol to extract the date portion before it
                  $parts = explode('@', $fileName);
                  
                  if (count($parts) > 1) {
                      $dateString = $parts[0]; // Result: "2026-08-27"
                      
                      // Convert the extracted date string into a Unix timestamp
                      $fileTimestamp = strtotime($dateString);
                      
                      
                      // 4. If parsing succeeded and the date is older than 1 month, delete it
                      //echo $fileName . "<br>";
                      //echo $fileTimestamp . " : " . $cutoffTimestamp  . "<br>";
                      
                      if ($fileTimestamp !== false && $fileTimestamp < $cutoffTimestamp) {
                          //echo "Deleted old file: " . htmlspecialchars($fileName);
                          if (unlink($filePath)) {
                            //  echo "Deleted old file: " . htmlspecialchars($fileName) . "<br>";
                            echo "<li>";
                            echo "<i>Deleted old file: " . htmlspecialchars($fileName) . "</i>";
                            //echo "  <span class='size'>$size</span>";
                            echo "</li>";
                          } else {
                              echo "<li>";
                              echo "Failed to delete old file: " . htmlspecialchars($fileName);
                              echo "</li>";
                          }
                          
                          
                          
                          
                          //echo "would delete here";
                      }
                      
                          
                  }
                  
              }
          }
          
          

        //******** DISPLAY ALL REMAINING FILES ********
        // Grab all files ending in .zip.acmi
        $files = glob("*.acmi");

        if (empty($files)) {
            echo "<li>No Tacview files available.</li>";
        } else {
            // Sort files so the newest missions appear at the top
            rsort($files);

            foreach ($files as $file) {
                // Get human-readable file size
                $bytes = filesize($file);
                $size = round($bytes / 1024 / 1024, 2) . ' MB';
                
                // Optional: Make the printed name look nice (e.g., "20260826-133342")
                $displayName = htmlspecialchars($file);

                echo "<li>";
                echo "  <a href='" . rawurlencode($file) . "'>$displayName</a>";
                echo "  <span class='size'>$size</span>";
                echo "</li>";
            }
        }
        ?>
    </ul>
    <h3>About Tacview Recorder for Cliffs of Dover</h3>
    <p><a href="https://forum.il2sturmovik.com/topic/88789-tacview-recorder-20-add-on-is-available/">CLoD Tacview Recorder is a project of jdu/FlyBy</a>. You can see <a href="https://www.youtube.com/@FlyBy2507">videos of the current and future version on his youtube page.</a> </p>
    <p>Players are shown as green infantrymen inside an aircraft - thus listed as "infantry" in Tacview's system.</p>
    <p>To preserve the "fog of war" and the fun of the TWC Server, the files do not show a massive, radar-like overview of the entire server and everything in it.
    Rather, the view is similar to what the player pilots see in the sim, showing just a limited sight-bubble around each live pilot. <a href="https://github.com/syn111/tacview-filter">syn111's tacview-filter script</a> is used to create the "fog of war" files.</p>
    <p>This is an early version of the recorder.  So it works but has quite a few quirks: Objects - especially ground objects - often have unusual or generic names, messages often indicate objects or aircraft are "Destroyed" when actually they have just left the battle or been removed by the server for internal server or performance reasons etc, some objects - especially ground objects - don't show up even when close to a player. Map locations are all displaced to the middle of the North Atlantic. Many insignificant ground objects (tables, jerrycans, fuel barrels) show up just as big as major, important ones looking just like tanks or planes or hangars. You will see players flying in to their spawnpoint from the lower left corner of the map, or flying between their crashpoint and spawnpoint.</p>
    <p>So just don't worry about all of those details and it is a very useful and helpful tool for seeing how missions and engagements went, that will only improve in the future.</p>
    <p><a href="https://steamcommunity.com/app/754530/discussions/0/687493774987888855/">Team Fusion has hinted about a Tacview implemention in a forthcoming release</a> - so we may see more to come!</p>

</body>
</html>