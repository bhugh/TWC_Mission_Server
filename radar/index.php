<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
<head>
<title>TWC Radar Plotting Table & Map</title>
<meta http-equiv="content-type" content="text/html; charset=UTF-8" />
<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js"></script>




<link rel="stylesheet" href="res/css/stats-style.css" type="text/css" />
</head>
<body>
<img style=" max-height: 10em;"src="http://twcclan.com/wp-content/uploads/2013/12/cropped-flying_tigers___col__edward_rector_by_roen911-d4msc2k.jpg" align=right width=50%>
<h1>TWC Radar Control</h1>

<p><i><b>Go to:</b> <a href="http://brenthugh.com/twc/mission-server-stats.htm">Mission Server Stats</a> - <a href="http://brenthugh.com/twc/training-server-stats.htm">Tactical Server Stats</a> - <a href="http://brenthugh.com/twc/practice-server-stats.htm">Practice Server Stats</a> - <a href="http://brenthugh.com/twc/stats-archive.htm">Older Stats Archive</a></i></p>

<br><br>
<center>
<div style="clear:both;">
<br><br>
<div style="clear:both; width:80%; background-color:#aaaabb">
<h2>Enter the TWC Server Contact Plotting Table</h2>
<form action="/twc/radar/radar.php">
  <b>Your Steam Username:</b><br>
  <input type="text" name="user" value="" required>
  <br><br>
  <b>Password:</b><br>
  <input type="password" name="pass" value="" required>
  <br><i>Password is <b>TWC</b>. For special events, the password may be different - check with TWC or event leaders.</i><br><br>
  <b>Server:</b><br>
  <input type="radio" name="server" value="Mission" checked required>Mission Server 
  <input type="radio" name="server" value="Tactical">Tactical Server
  <input type="radio" name="server" value="Other">Other:<input type="text" name="serveroth" value=""><br><br>
  
  <b>Side:</b><br>
  <input type="radio" name="side" value="Red" required>Red
  <input type="radio" name="side" value="Blue">Blue
  <input type="radio" name="side" value="Other">Other:<input type="text" name="sideoth" value=""><br>
  <i>Please use the Radar Control <b>only for the side you are actually flying or participating on</b></i>
  <br><br>
  
  <button type="submit" value="Enter the Contact Plotting Table">Click to enter the Radar Plotting Table & Map!</button>
  <br><br>
</form> 
</div>
</div>
</center>
  <br><br>
  <h2>About the Contact Plotting Table & Map</h2>
<p>The radar plotting table is designed to duplicate much of the functionality--and also many of the disadvantages and blind spots--of an early WWII-era contact plotting table based on radar returns and observer reports.<p>
<p style ="text-align:center; width:100%"><img align=center width=100% alt="WWII-era radar operator" src="graphics/early-wwii-radar-operator-and-station.jpg" caption="Chain Home: WAAF radar operator Denise Miley plotting aircraft on 
the CRT (cathode ray tube) of an RF7 Receiver in the Receiver Room at Bawdsey CH. Her right hand has selected the direction or height finding and her left hand is ready to register the goniometer setting to the calculator. RAF Bawdsey was originally an experimental system set up at Bawdsey Manor, home of Robert Watson-Watt's radar development team. When the team was moved away from Bawdsey, the radar station became a part of the operational Chain Home (CH) network. The main display is a large CRT, partially masked off by a metal box so only the lower half of the CRT remains visible. In earlier versions a scale running 
across the top of the opening allowed the range to the target to be measured. In this later version, a knob is used to move a cursor line across the screen to lie over a selected return. The cursor is driven by the same timing electronics as the rest of the radar, ensuring it is properly calibrated at all times. The large knob on the left of the image is the goniometer control. Unlike later 
systems, CH used separate transmitters and receivers. The transmitter broadcast a semi-directional signal in front of the station, known as the 'line of shoot', filing space with the signal. The receiver was a radio direction finder that searched that space for echoes. The goniometer knob changed the directional sensitivity of the receivers, allowing the angle to the target to be determined. This was a trial-and-error process of hunting for the maximum (or minimum) return in a noisy signal. Like most RDF systems, the antennas were equally sensitive in two directions; the small push-button to the upper left of the 
knob, the 'sense button', mutes down one of these directions to determine which one is correct. This button is not visible in the cropped but higher quality version of the image seen here, it can be selected below. A series of switches near Miley's right hand are used to select among several antennas on the receiver masts. Selecting a pair of these allows the goniometer to determine vertical angles instead of horizontal azimuth. With some calculation effort, this could be used to determine altitude. Additional crewmembers known as 
'plotters', normally located behind the operator, were sent a stream of angle and range information and had to calculate the map location of the targets being measured. These were then reported up the Dowding system's telephone network to Fighter Command headquarters in London. Due to the noise caused by the different plotters calling out calculations to each other, the radar operator was 
connected to the main plotter via the intercom Miley wears around her neck. This version, from later in the war, has been equipped with sensors to automate the plotting of the aircraft. One of these sensors can be seen attached to the goniometer control, the box-like object to the right of Miley's left hand. After measuring an angle with the goniometer and setting the cursor to measure the range of the selected 'blip', the button under Miley's left hand was pressed to send these settings to a mechanical computer known as the 'fruit machine'. It carried out all of these calculations internally and then directly output the map location and altitude of the targets. This greatly reduced workload and allowed the stations to have smaller crews. The metal box covering the CRT is 
also a later addition, a simple anti-jamming system. The Germans could jam the CH stations by broadcasting false echoes when they received a pulse from the CH station. These false signals were only partially synchronized, deliberately, so they jumped around the display and cluttered it up. The CRT was originally supplied with a fast-acting phosphor of a light blue color, but was later modified by placing a second layer of slower-acting, less-sensitive yellow phosphor on top. Signals that stayed in the same location long enough would 
cause the yellow layer to begin glowing in that location. When encountering jamming, the operator would pull the small metal tabs on the left side of the metal cover to move a yellow-coloured gel in front of the tube, filtering out the now noisy blue layer and leaving only the stable (but slower reacting) yellow signals visible. The marks on the metal cover suggest it has been used as an impromptu chalkboard. In this example, the radar receiver system and display 
are co-located. In later setups, the display was removed from the receiver and placed beside the plotting boards. This provided a much more compact layout and allowed the plotters to see the display directly. Although it cannot be seen in this photo, the shaft from the goniometer runs out the bottom of the display 
cabinet and into the receiver chassis, and can be seen to be at a slightly different angle than the display itself. The later systems combined this into a single cabinet."></p>

<p>Radar operators went through a complex, time-consuming, and somewhat error-prone process to determine the bearing, altitude, distance, and possible makeup of each individual radar contact.  This information was collected and collated by a large team onto a local radar plot table, and then transmitted via telephone to a central location, where another large team worked to assemble information collected from various outlying radar facilities, combined with observer reports, to create a unified overall view of the battlespace.</p>  

<p>This is the view that the TWC Contact Plotting Table & Map is designed to replicate.</p>

<p style ="text-align:center"><img align=center width=100% alt="Radar installation contact plotting table" src="graphics/Royal_Air_Force_Radar_Installation_Plotting_Table,_1939-145._CH15331.jpg"></p>

<p>From this central contact plot, information was relayed--again by telephone--to local command headquarters, where the plots were again duplicated on a plotting map, and from there instructions were relayed to pilots in the field.</p>

<p>Considering the complexity, large dispersed team and equipment required to gather and collate data, and the vast distances involved, the system worked relatively smoothly and quickly.</p>

<p>Nevertheless, by today's standards the system was very, very slow.  Response time from plot detection to combat pilot update was measured more in minutes than in milliseconds.</p>  

<p>So you will find that the TWC Contact Plotting Table transmits and updates data much, much slower than other modern 'radar screen' type game applications you may be familiar with.  It is simulating the pace of information updates in a WWII environment--and, if anything, is far, far faster and more responsive than such systems were in real life.</p> 

<p><a href="https://www.raf.mod.uk/campaign/battle-of-britain-75th/the-battle/battle-of-britain-in-detail-part-4/">The RAF history</a> estimates the time from radar contact to information at local command headquarters at about four minutes, which is astonishingly fast--and was probably more-or-less a best case scenario. The TWC Contact Plotting Table generally has updates to the operator in about two minutes--so about twice the best speed of the actual Chain Home system.</p>
<p style ="text-align:center"><img align=center width=100% alt="Central Plotting Table at RAF Bentley Priory, WWII" src="graphics/raf-bentley-priory-filter-room-map-table-early-wwii.jpg"></p>


           <p>According to the RAF history, the type of order commanders would give based on contact plotting map data was “Scramble 92 and 72, Patrol Canterbury, Angels 25", which meant that those squadrons were to climb to 25,000 feet and form a patrol line over the city of Canterbury.</p>
           <p>With today's modern radar, we might expect to be able to radio a pilot "You've got an enemy on your six. Turn hard right NOW!"  With the WWII-era plotting table system, commanders simply did not have information that specific or timely.  Rather, they would be able to provide pilots in the field with a best estimate of the general location enemy aircraft might be 5-20 minutes in the future, based on radar and observer information that was collected 4-20 minutes previously.</p>
           
           
<p>So, the contact plotting table gives an overview of the airspace similar that available to air commanders in some early WWII-era battles, and can provide invaluable strategic insights.</p> 

<p>But don't expect it to provide the sort of instantaneous and smooth updates you see in a modern aircraft or gaming 'radar' style display.</p> 

<p>An excellent description of a WWII contact plotting system in action can be found at the <a href="https://en.wikipedia.org/wiki/Chain_Home#Distance_and_bearing_measurement">Wikipedia article on Great Britain's Chain Home system</a>.</p>   

<h3>Image Sources</h3>
The source of each image (links below) has much more information about each image.

<ul><li><a href="http://www.iwm.org.uk/collections/item/object/205196699">Chain Home: WAAF radar operator Denise Miley plotting aircraft on the CRT (cathode ray tube) of an RF7 Receiver in the Receiver Room at Bawdsey CH. Her right hand has selected the direction or heightfinding and her left hand is ready to register the goniometer setting to the calculator, Imperial War Museums</a><br></li> <br>
<li><a href="http://www.iwm.org.uk/collections/item/object/205210716"> Chain Home: Flight Officer P M Wright supervises (right) as Sergeant K F Sperrin and WAAF operators Joan Lancaster, Elaine Miley, Gwen Arnold and Joyce Hollyoak work on the plotting map in the Receiver Room at Bawdsey CH, Suffolk, Imperial War Museums</a>                                                       <br></li>     <br>
<li><a href="https://www.raf.mod.uk/campaign/battle-of-britain-75th/the-battle/battle-of-britain-in-detail-part-4/">Central Contact Plotting Table at RAF Bentley Priory, RAF</a>                                                <br></li><br>
</ul>

<p><i><b>Go to:</b> <a href="http://brenthugh.com/twc/mission-server-stats.htm">Mission Server Stats</a> - <a href="http://brenthugh.com/twc/training-server-stats.htm">Tactical Server Stats</a> - <a href="http://brenthugh.com/twc/practice-server-stats.htm">Practice Server Stats</a> - <a href="http://brenthugh.com/twc/stats-archive.htm">Older Stats Archive</a></i></p>

<p><i>Visit <a href="http://twcclan.com">TWCClan.com</a> for more information about TWC and the TWC servers.</i></p>

</body></html>
