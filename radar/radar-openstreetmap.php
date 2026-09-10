<!DOCTYPE html>
<html>
<head>
	
	<title>TWC - Mission Overview</title>

	  <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <link rel="shortcut icon" type="image/x-icon" href="docs/images/favicon.ico" />
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.2.0/dist/leaflet.css" integrity="sha512-M2wvCLH6DSRazYeZRIm1JnYyh22purTM+FDB5CsyxtQJYeKq83arPe5wgbNmcFXGqiSH2XR8dT/fJISVA1r/zQ==" crossorigin=""/>
    <script src="https://unpkg.com/leaflet@1.2.0/dist/leaflet.js" integrity="sha512-lInM/apFSqyy1o6s89K4iQUKg6ppXEgsVxT35HbzUupEVRh2Eu9Wdl4tHj7dZO0s1uvplcYGmt3498TtHq+log==" crossorigin=""></script>
    <script src="res/leaflet.rotatedMarker.js"></script>    
    <script src="res/leaflet.geometryutil.js"></script>

<style>
body {
    padding: 0;
    margin: 0;
}
html, body, #map {
    height: 100%;
    width: 100vw;
}

.red-div-icon {
	background: #f44;
	border: 1px solid #622;
  font-size: 16px;
  display: flex;
  align-items: flex-start;


	}

.blue-div-icon {
	background: #44f;
	border: 1px solid #226;
  font-size: 16px;
  display: flex;
  align-items: flex-start;


	}
.plain-div-icon {
	background: #444;
	border: 1px solid #222;
  font-size: 16px;
  display: flex;
  align-items: flex-start;


	} 
	</style>

	<style>body { padding: 0; margin: 0; } #map { height: 100%; width: 100vw; }</style>
	

    <input style = "hidden:true" type="file" id="fileinput" />
<script>var urlParams = <?php echo json_encode($_GET, JSON_HEX_TAG);?>;</script>
<script type="text/javascript">

var redBIcon = L.divIcon({
    className: 'red-div-icon',    
    iconSize: new L.Point(7, 16),
    html: '<b>B</b>' 
    });
var redFIcon = L.divIcon({
    className: 'red-div-icon',    
    iconSize: new L.Point(7, 11),
    html: '<b>F</b>' 
    });
var BlueBIcon = L.divIcon({
    className: 'blue-div-icon',    
    iconSize: new L.Point(7, 16),
    html: '<b>B</b>' 
    });
var blueFIcon = L.divIcon({
    className: 'blue-div-icon',    
    iconSize: new L.Point(7, 11),
    html: '<b>F</b>' 
    });
var plainIcon = L.divIcon({
    className: 'plain-div-icon',    
    iconSize: new L.Point(7, 11),
    html: '<b>U</b>' 
    });        
//var isANumber = isNaN(theValue) === false;
String.prototype.isNumber = function(){return isNaN(this) === false;}
var xmlhttp = new XMLHttpRequest();
xmlhttp.onreadystatechange = function(){
  if(xmlhttp.status==200 && xmlhttp.readyState==4){    
    //var lines = xmlhttp.responseText.split('/\r?\n/g');
    var lines = xmlhttp.responseText.split('\n');
    lines.forEach(maplines);
    function maplines (item) {
        var words = item.split(',');
        //alert(words[0].isNumber());//isNumber(words[1]));
        //alert(words[1].isNumber());//isNumber(words[1]));        
        if (words.length > 1 && words[0].isNumber() && words[1].isNumber() ) { // && words[1].isNumber) {
         var ic = plainIcon;
         /* if (words[2]=='1' && words[3] == 'F') ic = redFIcon;
         if (words[2]=='1' && words[3] == 'B') ic = redBIcon;
         if (words[2]=='2' && words[3] == 'F') ic =  blueFIcon;
         if (words[2]=='2' && words[3] == 'B') ic = blueBIcon; */
         var ra = words[4];
         
         var color = 'plain';
         if (words[2]=='1') color = 'red';
         if (words[2]=='2') color = 'blue';
         //ra =0;
         var pl = "";
         if (words[10] != null) pl = " " + words[10];
         
         var pop = words[4] + "&#176 " + parseFloat(words[5]).toFixed(0) + "mph A" + words[6] + " " + words[7] + " " + words[8]+ "x" + words[9] + pl;
         
         //ic = redFIcon;
         /*
        	L.marker([words[0], words[1]], {
               riseOnHover: true,
               rotationAngle: ra,
               icon: L.divIcon({
                  className: color + '-div-icon',    
                  iconSize: new L.Point(words[5]/20 + 6, words[6] * 1.5 + 1),
                  html: '<center><b>' + words[3] + '</b></center>' 
                  })
          }).addTo(mymap)
          
  		      .bindTooltip(words[4] + "&#176 " + parseFloat(words[5]).toFixed(0) + "mph A" + words[6] + " " + words[7] + " " + words[8] + pl);
          */        
         	 L.marker([words[0], words[1]], {
               riseOnHover: true,
               //rotationAngle: parseFloat(ra),
               //rotationAngle: ra,
               icon: L.divIcon({
                  className: color + '-div-icon',    
                  iconSize: new L.Point(4,4),
                  
                  //html: '<center><b>' + words[8] + words[3] + '</b></center>' 
                  html: '<b>' + words[8] + words[3] + '</b>'                  
                  })
          }).addTo(mymap)
          .bindTooltip(pop)
          .bindPopup(pop);
          
                             
          var p1 = L.latLng([words[0], words[1]]); 
          //var p2 = L.GeometryUtil.destination(p1, parseFloat(words[4]), words[6]*800+1500);
          var p2 = L.GeometryUtil.destination(p1, parseFloat(words[4]), words[5]*20+1000);
          var pointList = [p1,p2];

          var mypolyline = new L.Polyline(pointList, {
              color: color,
              weight: 4,
              opacity: 0.65,
              smoothFactor: 1
          })
          .bindTooltip(pop)
          .bindPopup(pop)
          .addTo(mymap);
          
          //var p3 = L.GeometryUtil.destination(p1, parseFloat(words[4]) + 90, words[5]*10+500);
          //var p4 = L.GeometryUtil.destination(p1, parseFloat(words[4]) - 90, words[5]*10+500);
          var p3 = L.GeometryUtil.destination(p1, parseFloat(words[4]) + 90, words[6]*400+750);
          var p4 = L.GeometryUtil.destination(p1, parseFloat(words[4]) - 90, words[6]*400+750);
          
          
          pointList = [p3,p4];
          var basecolor='black';
          if (words[10] != null) basecolor = 'white';

          mypolyline = new L.Polyline(pointList, {
              color: basecolor,
              weight: 3,
              opacity: 1,
              smoothFactor: 1
          })
          .bindTooltip(pop)
          .bindPopup(pop)
          .addTo(mymap);
          
        }

    }
    
  }
}

if (urlParams['mission'] != null && urlParams['pass'] == "mum") {
 xmlhttp.open("GET",urlParams['mission'] + "_radar.txt",true);
  xmlhttp.send();
}
</script>
	
</head>
<body>


<div id="mapid" style="width: 1200px; height: 800px;"></div>
<script>

	var mymap = L.map('mapid').setView([50.185, 0.94], 8);

	L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token=pk.eyJ1IjoiYmh1Z2giLCJhIjoiY2o5ZW56OHJxMjRsMzJ4cGFzbHhrZXpxYyJ9.hXyrqZeJGVw32ngUb3ZIiQ', {
		maxZoom: 18,
		attribution: 'Map data &copy; <a href="http://openstreetmap.org">OpenStreetMap</a> contributors, ' +
			'<a href="http://creativecommons.org/licenses/by-sa/2.0/">CC-BY-SA</a>, ' +
			'Imagery © <a href="http://mapbox.com">Mapbox</a>',
		id: 'mapbox.streets'
	}).addTo(mymap);


	var popup = L.popup();

	function onMapClick(e) {
		popup
			.setLatLng(e.latlng)
			.setContent(e.latlng.toString())
			.openOn(mymap);
	}

	mymap.on('click', onMapClick);

</script>

<pre>
<? 

if ($_GET['pass'] == "mum" && $_GET['mission'] !==null ) {
   //echo $_GET['pass'] . $_GET['mission'] . "_players.txt";

   include ($_GET['mission'] . "_players.txt");
}
?>

</pre>

</body>
</html>
