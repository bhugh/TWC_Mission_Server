#define DEBUG  
#define TRACE  
//#undef TRACE
/******************************************************************************************************************
* ALMOST ALL USER CUSTOMIZATIONS ARE IN THE stats.ini FILE. YOU SHOULDN'T NEED TO EDIT ANY VARIABLES IN THIS FILE *
* EXCEPT FOR THE TWO VARIABLES BELOW THAT GIVE THE DIRECTORY AND NAME OF THE stats.ini FILE                       *
* THESE TWO VARIABLES ARE FOUND BELOW UNDER "#region Customizable Variables"                                      *
******************************************************************************************************************/

//IMPORTANT!!!! Most errors are logged to an ERROR LOG that you set up--something like stb_ErrorLogPath = @"TWCTRAINING_errorlog.txt
//If the program is mysteriously malfunctioning with no visible errors, please check the ERROR LOG FILE first!
//Console.WriteLine() fails silently if called in a background thread! Most likely to happen in StbSR class; don't use it there; use StbSr_WriteLine() instead.
//Everything still works--you just won't see any Console.WriteLine() messages, making you *think* that everything is failing. 

//TODO:
// - Stop transfer of radar files older than say 30 minutes
// - NETSTATS KILL POINTS are coming out as higher than TWC Kill points. Figure out why/could be because some a/c crashes, landings, etc are still not coming through in 4.5, or some other bug
// - When parachuting as a Bomber pilot, and if you die, the MAIN career is ended instead of the BOMBER career.
// - Need to add details for new 4.5 aircraft into <rr section
// - Make HTML save only maybe every 10 or 15 minutes, rather than every 2.  Stats file should still save every 2.
// - Make self-kill only register if there is actually NO damage to the plane from other aircraft or flak etc
// - Sometimes deaths reported to not actually result in the death/end of that career.  Specifically, when landing safe in enemy territory & reported "captured" etc.  Specifically also when doing this in a bomber (not sure about fighter)
// - Death of bomber pilot is sometimes (often!?) attributed to the fighter personality instead.  Specifically when parachuted out of the plane & both personalities were reported as 'captured'
// - afterwards in-game it shows as pilot killed & new career shown, but then when server rolls over it reverts to the old version of the bomber pilot (??!???)
// - sometimes rank for (bomber) career is reported under (fighter) career  & vice-versa. This shows up on stats html page.  Maybe because that pilot is currently flying as (bomber) while the stats is being saved & rank calculated for (fighter).  Maybe this problem extends to <ac and <nextac as well.
// - Went into a bomber after parachuting out of another bomber a few minutes earlier & when going it was kicked out as "not allowed" and then (at THAT time stamp) was considered as "killed" by the server.  Not sure why, perhaps because one personality exited the previous aircraft via parachute but perhaps the other personality didn't?  Or something?
// - Probably related, if kicked out of plane in air-spawn (bec not allowed that aircraft or whatever, recent death) stats seems to pick it up as "you parachuted out of the plane" and since it is generally in enemy territory or whatever it gets counted as a death
// X On multi-position plane, when using "External View" (Alt-F11 or maybe often mapped to ALT-F2) to return to position, needed to 
// <ac and <nextac need to distinguish between (bomber) and fighter careers, and/or maybe list both
// release the 2ndary position back to AI control, it instead tells the pilot the sortie has been ended.
// - When e.g. self-kill happens to bomber pilots it can kill their fighter pilot personality instead and not kill the bomber personality
// - Sorties are not being counted/tallied correctly for some reason (too few are tallied by quite a lot). 9/14/2017 progress made but not sure if it is solved
// - When reprieve is given for death due to explosion, it doesn't seem to properly take into account whether you were over water, on enemy territory, etc.
//    - specifically when clod kills you but stats give a reprieve (ie, low-speed crash into hanger) then stats should process that incident as though 
//        the plane has crash landed at that spot, or the pilot parachuted, or whatever.  So if in enemy water, you might drown, on enemy territory you might 
//        be captured, whatever.  That final part is not working right.
// - Bomber pilots who die or parachute, land in enemy territory, etc, will receive TWO messages indicating what happened to them, and sometimes they contradict each other
// - Related, when bombers crash etc the bomber pilot receives TWO death messages (one for each "place" occupied at the time.  Sometimes these are contradictory, like
//        one says you landed on the water & drowned, the other says you were saved
// - Make bombers like a "squadron" with several lives & new lives gained with every victory, minute flown, promotion, or ???
// - Need to make separate careers for Blue vs Red (along w/ fighter vs bomber, thus 4 possible careers in total)
// - Be able to report /carf /carb /carfb /carfr etc to get stats for 4 different types of careers
// X Allow rank/promotion messages to be turned off (per setting in stats.ini file)
// - Allow rank/promotion system to be entirely turned off (per setting in stats.ini file)
// - Rename all variables that set as defaults in the .cs files & then typically overridden by stats.ini file as e.g. stb_ResetPlayerStatsWhenKilled_default to emphasis that they are defaults that
//   are usually overridden
// X stationaries that are killed on friendly ground are not counted as victories, even if they are enemy vehicles or stationaries
// X Ground actors (chiefs) my still have the problem of ground stationaries (just above) - not counted as kills if on friendly territory
// - Is there some better way to let stbSr read all of the .ini file settings other than the annoyingly redundant way of passing them via the method call?  So much typing, so convoluted . . .
// - Before counting a kill on de-spawn (or generally other than "landing") perhaps check that the a/c is NOT on the ground
// X Planes were being "written off" even though they were actually landed at a friendly airport.
// X Related to the above, the "find the nearest friendly airport" routine is fixed.
//   very near a friendly airport.  It's possible we're over-counting kills bec. some a/c land at/near airpots but CloD never reports it as a 'landing'
// - when a player crashes & dies, the plane is written off.  But it seems that often (always?) the write-off goes to the "new" life rather than the previous life.
// - if a player shoots or bombs an object (ac or ground object), then leaves the server or switches to a different a/c OR switches say from fighter to bomber, then later on the ac or ground object finally dies, the original player probably doesn't get credit because they actor they used to kill that particular ac or ground object no longer exists.  Or if it DOES exist it might be credited to the wrong career because the player has switched to a new career (say (bomber)) and the ondead routine has no way to know that
// X Dis-incentives for bomber pilots to fly because they die so often. Create a separate career for a player's heavy bomber missions or come up with some other solution to make it fairer for & encourage more bomber pilots.
// X When you land quite safely away from an airport, even completely undamaged, you are kick out of a/c and it's listed as 'aircraft written off'.  But, you actually might want to take off again & are capable of doing that.  See ~ lines 7583.
// - General code cleanup & dead code removal 
// X Add planes written off, pwo per sortie to <career
// - & perhaps a few more relevant stats to <career and/or <sess
// X Flight time wasn't registering properly/often wasn't saved (end of mission etc)
// X MANY kills were not being recorded, esp of the aiaircraft just landed somewhere & didn't die/explode
// X Rank system advances pilot far too quickly up the ranks - more 'in between' ranks needed, plus a general slow-down in advancement
// X Pilots die/lose career when they do slow-speed crashes (ie into hangars) or are vulched.  CloD is too anxious to put the 'explosion' effect into play in low-speed ground collisions
// X Need to handle case of when player leaves a/c or disconnects - die instantly or what?
// X Players are being given death only when 'onActorDead' is called, which is generally only when an a/c crashes/explodes spectacularly.  Need to also take care of what happens when they parachute in friendly/unfriendly/land/water location, land or crash land, in various locations etc.  In many cases we are giving the player a message "you drowned" or whatever, but in reality **nothing happens** in the stats or to affect the players career.  The only thing that happens is that little message shows up.
// X When you land outside of an airfield pretty soon the server kicks you out of the plane etc.  But for various interesting special missions you should be able to just stay there & take off again etc if undamaged.

// X NULL REFERENCE EXCEPTION somewhere in onactordead
// X When you are dead, after a while you are kicked out of the aircraft.  Then this registers as "X parachuted" blah blah blah presumably because the kickout-on-death is a place-leave.
// X Also, similarly, if you crash into the ground & then you're kicked out of the A/C, then you get a message "XXX left an aircraft in motion. . . . "  (Just needed messaging massage.)
// X Could prevent/stop various messages that usually happen when you end a flight or leave your position, when they happen
//   because the server has KICKED YOU from a plane or whatever.
// - bombs & bullets don't seem to show up in practice server.  Might be because they are set to unlimited in that server?  <sess  I think it was working with different server settings while I was testing it offline. Fixed it partly by changing the bomb/bullet lines in <ses & <car to simply hide themselves if not data
// X Bombs & bullets don't seem to be showing up on the stats pages
// Remove pilot from a/c routine may need some improvements. In particular, what happens if an unallowed pilot jumps into the 2nd seat of a bomber etc.  The bomber will probably just be destroyed.  Not good.
// Some bombers have up to FOUR Positions, so each time the bomber dies the pilot will get FOUR kills assigned.\
// Also they receive the message about "if you land & take off at the same airport" FOUR TIMES whne leaving the a/c.  Probably should check
// & limit this message to once every 5 minutes or something
// X disable aircraft restriction by rank temporarily (for training etc).  (Partly/mostly done - via <training <untraining
// X Air spawns do not count sorties & missions correctly
// - Is it killing the player if they are still flying when the server ends? Maybe? OR perhaps something funny happens when you use the right-click menu to skip around among different a/c & vehicles in the server.  Often you 
// - It may be losing stats when player dies.  Or perhaps just a bug of server rollover to new directory location.
// X Create separate page for dead pilots
// - Create more pages for pilots dead during certain time periods (per month?) and/or just remove them
// - Remove dead pilots & possibly live pilots after a certain time period
// X Make sure good landings link to create a mission if takeoff from same location
// - Fix onplaceleave issue where opponents may get a kill score when the a/c has actually landed just fine but CloD doesn't register a landing
// X Fix the size issue of the dictionary int[] so that in can flexibly be 800 or
//900 or whatever and can be changed through one constant, not many diddly lines
//of code
// X Kills that die after you die are credit to your new mission rather than the old one.  (Probably just have to let this one go . . . fix would probably be pretty complicated. Having the "pause" after your death during which you can't re-spawn in helps this some.)
// X Health doesnt' ever seem to detect anything
// X switch kills/mission to start of list
// X switch health damage to end of damage field since it doesnt' work
// X Make a system for partial & shared kills as well as full kills, like SOW.  Give extra credit for first damage, pilot or gunner kill, maybe a few other things.
// - It may not always reliably detect that pilots have been killed, especially if they are not in an a/c at the moment, or other unusual situations  
// X make it do a final stats save just before killing the server for re-start
// X keep track of "dumb" deaths & crashes, ie self-inflicted  (perhaps if you die or leave place when the only damage caused is self-inflicted)
// X What about people who just  leave their plane in mid-air etc?  Do they die, lose their mission?
// - On continuemission, check altitude, too, so that people can't just leave the server above an airport & then spawn in again right at the airport ot continue their mission
// - 'XXX returned to base' or 'XXX is safe on the ground' or "XXX landed safely" is indication that the landing registered
// X Fix the thing where just sitting on the ground registers as flight time.  Don't count flight time unless there is an actual take-off. 
// X On player died page, split the name field into "died on" and 'name' fields.
// - Track last time the player was active on the server (whenever stats are written or saved) and then use that to filter out old/inactive players (partly complete)
// X on placeleave shows a message even when you are just changing places in a bomber or parachuting out (the message is nonsensical in those situations)
//
// $reference System.Core.dll  is needed to make HashSet work.  For some reason.
// /Strategy.dll & /gamePlay.dll are needed to various parts of CLOD
//The two $references below + the [rts] scriptAppDomain=0 references on conf.ini & confs.ini are (perhaps!?) necessary for some of the code below to work, esp. intercepting chat messages etc.

//$reference parts/core/CloDMissionCommunicator.dll
//$reference parts/core/CLOD_Extensions.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll
//$reference System.Core.dll
///$reference Microsoft.csharp.dll 
//$reference WPF/PresentationFramework.dll
//$reference WPF/PresentationCore.dll
//$reference WPF/WindowsBase.dll
//$reference System.Xaml.dll
using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.ComponentModel;
//using System.Data;
//using System.Core;
using System.Linq;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using maddox.game.play;
using maddox.game.page;
using part;
using Ini;
using TF_Extensions;  //not working for now?
using TWCComms;

//test



public class Mission : AMission, IStatsMission
{
    #region Customizable Variables
    //SERVER NAME & INFO CUSTOMIZATIONS
    /**********************************************************************************
    * THE TWO VARIABLES IMMEDIATELY BELOW ARE THE ONLY VARIABLES THAT YOU ****MUST*****
    * CUSTOMIZE IN ORDER TO POINT TO YOUR .ini FILE. IF YOU POINT TO THE .INI         *
    * FILE CORRECTLY THEN THE REMAINDER OF THE CUSTOMIZABLE VARIABLES WILL            *
    * BE READ FROM THE .INI FILE USING METHOD stb_loadINI                             *
    ***********************************************************************************
    ***********************************************************************************
    * IMPORTANT!!!!!!                                                                 *
    *                                                                                 *
    * Most values below are simply defaults and WILL be overridden by any values in   *
    * your stats.ini file (or whatever you choose to name it, below                   *
    *                                                                                 *
    ***********************************************************************************
    */
    public string stb_LocalMissionIniDirectory { get; set; }
    
    //public string stb_LocalMissionIniDirectory = @"missions\Multi\Fatal\"; //Local directory (ie, on the same hard drive as the CloD Server) where your stats.ini file will be located. This is in relation to the directory where Launcher.exe /server is found.    
    public string stb_StatsINIFilename = "stats.ini";  //If this stats.ini exists in the directory indicated, then all values in that file will be read in & replace the DEFAULT values in the section below

    //NOTE: Values below are DEFAULTS and will be OVERWRITTEN by the values in the stats.ini file
    //Check under method "public Mission ()" to see how all these strings are combined to create the actual complete directories, filenames, etc 


    #region DEFAULTS
    //Note: initial @ (ie @"missions\Multi\Fatal\") allows you to incorporate backslashes into the file or directory name easily. Normally c# considers \ an escape character.  Instead of using @ You could just type \\ every time you want \ but that is confusing. If you need a double-quote character " in your @string, just type "" - like @"<img src=""\mydirectory\myfile.png"">"
    public bool stb_Debug = false;
    public string stb_ServerName_Public = "My Server"; //The name used publicly ie on the stats web pages & in "Welcome to the XXX" type messages.
    public string stb_ServerFilenameIdentifier = "MyServer"; //The "private" name of the server used as part of filenames. Will be combined with various suffixes to create filenames for e.g. stats files, log files for this server. Best to avoid using spaces or any strange characters that will cause trouble if part of a filename.
    public string stb_AdminPlayernamePrefix = @"ATAG_"; //anyone whose username starts with this prefix will be allowed access to admin commands in the Chat Commands system. You could also set this to a full username if you want just one certain user to have admin access.
    public string stb_LogStatsPublicAddressLow = "MyServer.com"; //Web address to visit for stats page. Used in announcement messages such as "Check your stats online at XXX". Used in-game, not clickable, so best to keep it simple.
    public string stb_LocalMissionStatsDirectory = @"missions\Multi\MyMissions\"; //Local directory (ie, on the same hard drive as the CloD Server) where local copies of stats files, .htm files, log files, etc will be kept.  This is in relation to the directory where Launcher.exe /server is found.
    //If you specify the same directory for several missions (ie, several missions you run in rotation on your server) then all missions will share the same stats files and accumulate stats from mission to mission throughout the rotation. If you want to maintain separate stats for each separate mission, then use separate -stats.mis and -stats.cs files for each mission and modify each one ot use a different stb_LocalMissionStatsDirectory

    public string stb_LogStatsUploadBasenameLow = "myserver-stats"; //Will be used as the prefix as the initial part of the filename for public web pages created by the server. Directory & suffix will be added. EX: IF you enter XXX, you will get something like http://yourserver.com/yourdirectory/XXX.htm                                                                           
    public string stb_StatsWebPageTagLine = "<p>Visit <a href=\"http://myserver.com\">My Server</a> for more information about My Server.</p>"; //This is added at 3 points in the stats web page - allows you to link back to your main web page etc in a customizable way   
    public string stb_StatsWebPageLinksLine = "<p><i><b>Go to:</b> <a href=\"http://myserver.com/mydirectory/myserver-stats.htm\">My Server Stats</a> - <a href=\"http://myserver.com/myotherdirectory/myotherserver-stats.htm\">My Other Server Stats</a></i></p>";//This is added at 3 points in the stats web page & allows you to link to other mission stats pages you have or basically anything else you want to insert at these points. You'll have to use full http addresses for any links. Escape any need " characters with backslash, like \"
    public string stb_LogStatsUploadFtpBaseDirectory = "ftp://ftp.myserver.com/mydirectory/"; //filenames will be added to this; trailing slash required.  Used for the FTP upload (ie, the FTP directory, not the publicly visible HTTP directory)
    public string stb_LogStatsUploadUserName = "myFTPusername";  //FTP username
    public string stb_LogStatsUploadPassword = "myFTPpassword";  //FTP password
    //IMPORTANT NOTE: Upload sorttable.js and Style.css and the ENTIRE CONTENTS of the 'res' subdirectory to stb_LogStatsUploadFtpBaseDirectory also--the stats web pages that will be uploaded to that directory depend on them to be viewed properly



    //FUNCTIONALITY RELATED CUSTOMIZATIONS
    public bool stb_ResetPlayerStatsWhenKilled = true;//If TRUE: When a player is killed, all stats reset to 0 (Old stats are still avail under different "dead player" name however). If FALSE: Player stats are compiled continuously regardless of player death.
    public string stb_LogStatsDeadPilotsSuffix = "-dead-pilots"; //Will be added to stb_LogStatsUploadBasenameLow when saving stats pages for the dead pilots list. Generally, no need to change or customize th is.
    public string stb_LogStatsTeamSuffix = "-team"; //Will be added to stb_LogStatsUploadBasenameLow when saving stats pages for the team stats page. Generally, no need to change or customize this.

    public bool stb_NoRankMessages = false;//if TRUE: Messages about rank and/or promotions will not be displayed during gameplay.

    public bool stb_NoRankTracking = false;//This is not implemented yet, but when implemented setting to TRUE will turn off all rank/promotion tracking & display


    public bool stb_PlayerTimeoutWhenKilled = true; //Whether to give a player a "timeout" when killed--a period of time when the player can't log in & play again
    public double stb_PlayerTimeoutWhenKilledDuration_hours = 3.0; //Time (in hours) for the player timeout on death. Only active if stb_PlayerTimeoutWhenKilled  is TRUE. 0.083334 hours = 5 minutes
    public bool stb_PlayerTimeoutWhenKilled_OverrideAllowed = true; //Whether to give players the choice to override the death timeout

    public bool stb_restrictAircraftByKills = true; //Whether to restrict A/C until a certain # of kills is reached
    //int stb_restrictAircraftByKills_RequiredKillNumber = 200; //this has been replaced by a more customizable dictionary approach now

    public bool stb_restrictAircraftByRank = true; //Whether to restrict A/C until a certain ranks are reached
    //Note that stb_restrictAircraftByRank is the masterswitch for  the entire "restrict aircraft" system and 
    //stb_restrictAircraftByKills is just a little tweak you can add on to the rank system.  You can't
    //just turn on stb_restrictAircraftByKills independently if stb_restrictAircraftByRank is turned off.

    public bool stb_AnnounceStatsMessages = true;
    public int stb_AnnounceStatsMessagesFrequency = 19 * 60; //seconds
    public bool stb_StatsServerAnnounce = true;
    public string stb_LogStatsUploadAddressMed = "";//not available yet
    public double stb_LogStatsDelay = 120.0;//seconds 60.0 default
                                            //public double stb_LogStatsDelay = 10.0;//for testing

    public double stb_ChangeAttackProbHigh = 1; //If being attacked, airgroups can change to attack the attacker.  This governs the probability they will do so.  At runtime Stats chooses a random value between high & low given here.  This governs all airgroups EXCEPT bombers
    public double stb_ChangeAttackProbLow = 0.8;

    public double stb_ChangeBomberAttackProbHigh = 1; //If being attacked, Bomber airgroups can change to attack the attacker.  This governs the probability they will do so.  At runtime Stats chooses a random value between high & low given here.  If the bomber airgroups do this it probably distracts them from their main mission objective (bombing or whatever) so maybe don't need to do this as often with bombers as with other airgroup types
    public double stb_ChangeBomberAttackProbLow = 0.8;

    public double stb_ChangeAttackProb_SmallGroupHigh = 1;//If being attacked, small groups can act differently from large ones.  Say there are only 2 bombers left, etc, they could be more likely to attack than if the whole group is left
    public double stb_ChangeAttackProb_SmallGroupLow = 0.8;
    public double stb_ChangeAttackProb_SmallGroupThresh = 2; //number of a/c in airgroup at or below which the _smallGroup prob holds, supplanting the other probs.  If you set to 0 this won't be used at all.
    
    public double stb_ChangeAttackProb = .8; //Will be assigned randomly later between low & high
    public double stb_ChangeBomberAttackProb = .8; //Will be assigned randomly later between low & high
    public double stb_ChangeAttackProb_SmallGroup = .8; //Will be assigned randomly later between low & high
    //LOG RELATED CUSTOMIZATIONS
    public bool stb_LogStats = true;//creates a local file, full stats in txt format. This is the main on/off switch for this entire module. If set to FALSE almost nothing else works--no stats collected, no files saves, etc.
    public bool stb_LogErrors = true;//creates a local file, errors in txt format, useful for IO or upload errors    
    public bool stb_LogStatsCreateHtmlLow = true;//creates a local file, summerized stats in html format
    public bool stb_LogStatsCreateHtmlMed = false;//not available yet - no output even if set to true. Concept is to create a stats page with higher level of detail than HtmlLow.
    public bool stb_LogStatsUploadHtmlLow = true;//set true to upload - this is the main stats web page in HTML format
    //Upload sorttable.js and Style.css to the same location on your site manually, created htm depends on them to be viewed properly
    public bool stb_LogStatsUploadHtmlMed = false;//not available yet
    #endregion
    //END - of options that are defaults & will be overwritten by values from the stats.ini file    

    //MISSION RELATED CUSTOMIZATIONS
    //TWC is mostly not using these options currently
    //For that reason I have not added them to the .ini file or iniread method
    public bool stb_DestroyOnPlaceLeave = true; //This helps stats by eliminating the problem with people getting credit for a kill when actually the plane just landed.  However, works best if values af1, af2, af3, af4 are set for airfield locations, as below.  I think it will still help regardless, so leaving it on for now.  TODO: Get a list of actual airports in the mission, not just using the af1, af2, etc.
    public bool stb_SpawnAntiAir = false;//base AAguns and defense ships
    public double stb_SpawnAntiAirDelay = 444.0;//444.0 default, some AA guns stop firing(probably run out of ammo) in less than 250 secs
    public bool stb_SpawnFrontline1 = false;//continuous ships @ 1.path (left)
    public bool stb_SpawnFrontline2 = false;//continuous ships @ 2.path (left-center)
    public bool stb_SpawnFrontline3 = false;//continuous ships @ 3.path (right-center)
    public bool stb_SpawnFrontline4 = false;//continuous armors @ 4.path (right)
    public bool stb_SpawnBombers = false;//loads 2 bomber missions, turnbased
    //Balance Related
    public bool stb_FighterBalanceActive = false;//false to disable balance code for fighters
    public bool stb_BalanceFightersByRatio = false;//true for balance by ratio, false for balance by fighter count difference
    public double stb_DesiredRatio = 2.0;//max delta fighter ratio for balance, default 2.0
    public int stb_DesiredDelta = 5;//max delta fighter count for balance, default 5
    public int stb_MinCountForBalance = 10;//min fighter count for balance (for both ratio and delta methods), default 10
    public string stb_BalanceMsgRus = "Извините, Есть слишком много истребителей прямо сейчас." +
                                       " Выберите бомбардировщиков или Изменение вашей армии или Попробуйте еще ​​раз позже.";
    public string stb_BalanceMsgEng = "Sorry, There are too many fighters right now." +
                                       " Choose bombers or change your army or try again later.";
    public string stb_BalanceMsgHud = "Aircraft Disabled: There are too many fighters right now!";

    public Int64 lastGpLogServerMsg_tick = 0;
    public Int64 GpLogServerMsgDelay_tick = 1000000; //1 mill ticks or 0.1 second
    public Int64 GpLogServerMsgOffset_tick = 500000; //Different modules or submissions can use a different offset to preclude sending gplogservermessages @ the same moment; 500K ticks or 0.05 second    
    public string stb_LogStatsUploadAddressLow;
    public string stb_LogStatsUploadAddressExtLow;
    public string stb_LogStatsUploadFilenameLow;
    public string stb_LogStatsUploadFilenameDeadPilotsLow;
    public string stb_LogStatsUploadFilenameTeamLow;
    public string stb_LogStatsUploadFilenameTeamPrevLow;
    public string stb_MissionServer_LogStatsUploadFilenameLow;
    public string stb_iniFileErrorMessages;

    //TODO: All these should be added to .ini file.
    //public readonly string [] PARACHUTE_LAND_FRIENDLY_MSG = {  };
    public readonly string[] stb_LANDED_OK_MSG = { "{0} landed in friendly territory. " };
    public readonly string[] stb_LANDED_ENEMY_MSG = { "{0} landed in enemy territory. " };
    public readonly string[] stb_ASR_FAIL_DROWNED = { "{0} drowned! ", "{0} tried to swim for shore, but drowned! ", "{0} drowned when the life raft deflated! " };
    public readonly string[] stb_ASR_RESCUE_MSG = { "{0} was rescued by local fishermen! ", "{0} was rescued by friendly fishermen!", "{0} was rescued by ASR ", "{0} was rescued by a passing ship! ", "{0} was rescued after hanging onto a life raft for 28 hours! ", "{0} was rescued. Now {0} can join the Goldfish Club! " };
    public readonly string[] stb_ASR_CAPTURE_MSG = { "{0} was captured by enemy forces and shot while escaping! ", "{0} was captured by enemy forces" };
    public readonly string[] stb_CAPTURED_MSG = { "{0} was captured. ", "{0} was captured by enemy forces and shot while escaping! ", "{0} was captured and spent the rest of the war in a POW camp. ", };
    public readonly string[] stb_ESCAPED_MSG = { "{0} escaped capture and is hiding in a resistance fighter's hayloft! ", "{0} escaped and is hiding the forest. ", "{0} escaped and is hiding in a resistance fighter's barn! ", "{0} escaped--now has to find the way home!" };
    public readonly string[] stb_CRASHLAND_ENEMY_MSG = { "{0} crash landed in enemy territory. " };
    public readonly string[] stb_CRASHLAND_FRIENDLY_MSG = { "{0} crash landed in friendly territory. Now you have explain that to the CO. Good luck! " };

    public readonly string[] stb_LANDAWAYAIRPORT_SAFE_FRIENDLY_MSG = { "{0} landed in friendly territory away from an airport. To avoid having your aircraft written off, you must return it to an airport. " };
    
    public readonly string[] stb_PARACHUTED_FRIENDLY_MSG = { "{0} parachuted into friendly territory. ", "{0} parachuted, landing in friendly territory." };
    public readonly string[] stb_PARACHUTED_ENEMY_MSG = { "{0} parachuted into enemy territory. " };

    //Setting the probability of surviving quite high, mostly bec. we haven't previously had any consequence or death for landing in water etc.
    double stb_POW_EscapeChanceRed = 0.90;      // probability (0-1) of allied pilot escaping after landing in enemy territory
    double stb_POW_EscapeChanceBlue = 0.90;     // probability (0-1) of LW pilot escaping after landing in enemy territory
    double stb_ASR_RescueChanceRed = 0.90;      // probability (0-1) of allied pilot being air-sea rescued (captured) after landing in water.  Per Wikipedia, ASR rescue was in the 30-35% range during WWII
    double stb_ASR_RescueChanceBlue = 0.90;     // probability (0-1) of LW pilot being air-sea rescued after landing in water
    double stb_ASR_RescueChanceFriendly = 1.06;     //multiplier for ASR_RescueChance if landing in friendly waters
    double stb_ASR_RescueChanceEnemy = .9;      //multiplier for ASR_RescueChance if landing in hostile waters
    double stb_ParachuteFailureRecoveryChance = .99; //If CloD decides your "parachute failed" what is your chance of being able to deploy your reserve chute? 
                                                     //CloD does something like 20-50% parachute failure rate. Wherease US AF in training in WWII found one main chute failure PER WEEK in a large training facility, so that is one per hundreds of jumps.  And that is the main chute. The reserve shoot fails maybe 1% of the time or less also.  So even .99 here is probably too LOW, certainly not too high.
                                                     //In reality in combat it might have been somewhat less than that, but still . . . 


    #endregion

    #region stb Core
    //these variables change in runtime, do not alter their default values
    public Point3d af1 = new Point3d(13800.0, 30890.0, 1.0);
    public Point3d af2 = new Point3d(18180.0, 30760.0, 1.0);
    public Point3d af3 = new Point3d(12180.0, 10520.0, 1.0);
    public Point3d af4 = new Point3d(18830.0, 9810.0, 1.0);
    public bool stb_BomberMissionTurn = true;
    public int stb_RedFighters = 0;
    public int stb_BlueFighters = 0;
    public double stb_Ratio = 0d;
    public double stb_Delta = 0d;
    public string stb_AppPath = (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
    public string stb_FullPath = "";//will be created in runtime
    public string stb_ErrorLogPath;
    public string stb_StatsPathTxt;
    public string stb_StatsPathHtmlLow;
    public string stb_StatsPathHtmlExtLow;
    public string stb_StatsPathHtmlMed;
    public ISectionFile stb_BaseAntiAirDefenses;
    public ISectionFile stb_Frontline1;
    public ISectionFile stb_Frontline2;
    public ISectionFile stb_Frontline3;
    public ISectionFile stb_Frontline4;
    public ISectionFile stb_Bombers1;
    public ISectionFile stb_Bombers2;
    public NumberFormatInfo stb_nf = new NumberFormatInfo();
    public int stb_MissionsCount = 1;

    Random stb_random = new Random();

    public ABattle battle = null;

    public IMainMission TWCMainMission;
    //public AIniFile TWCIniFile;

    //initializer method
    public Mission () {
        stb_LocalMissionIniDirectory = @"missions\Multi\Fatal\"; //Local directory (ie, on the same hard drive as the CloD Server) where your stats.ini file will be located. This is in relation to the Cliffs of Dover documents directory, ie C:\Users\XXXXXXXX\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\
        TWCComms.Communicator.Instance.Stats = (IStatsMission)this; //allows -stats.cs to access this instance of Mission
        TWCMainMission = TWCComms.Communicator.Instance.Main;
        //TWCIniFile = TWCComms.Communicator.Instance.Ini;

        string s = stb_AppPath.Remove(stb_AppPath.Length - 5, 5);
        string stb_FullPath_ini = s + stb_LocalMissionIniDirectory; // something like @"missions\Multi\Fatal\"
        stb_loadINI(stb_FullPath_ini + stb_StatsINIFilename );
        //stb_LogStatsUploadAddressLow = "ftp://ftp.brenthugh.com/brenthugh.com/twc/" + stb_LogStatsUploadBasenameLow;        
        stb_LogStatsUploadAddressLow = stb_LogStatsUploadFtpBaseDirectory + stb_LogStatsUploadBasenameLow;
        stb_LogStatsUploadAddressExtLow = ".htm";
        stb_LogStatsUploadFilenameLow = stb_LogStatsUploadBasenameLow + stb_LogStatsUploadAddressExtLow;
        stb_MissionServer_LogStatsUploadFilenameLow = "mission-stats" + stb_LogStatsUploadAddressExtLow;
        stb_LogStatsUploadFilenameDeadPilotsLow = stb_LogStatsUploadBasenameLow + stb_LogStatsDeadPilotsSuffix + stb_LogStatsUploadAddressExtLow;
        stb_LogStatsUploadFilenameTeamLow = stb_LogStatsUploadBasenameLow + stb_LogStatsTeamSuffix + stb_LogStatsUploadAddressExtLow;
        stb_LogStatsUploadFilenameTeamPrevLow = stb_LogStatsUploadBasenameLow + stb_LogStatsTeamSuffix;
        stb_ErrorLogPath = stb_ServerFilenameIdentifier + @"_errorlog.txt";//will be combined with fullpath in runtime
        stb_StatsPathTxt = stb_ServerFilenameIdentifier + @"_playerstats_full.txt";//"
        stb_StatsPathHtmlLow = stb_ServerFilenameIdentifier + @"_playerstats_low";//"
        stb_StatsPathHtmlExtLow = @".htm";//"
        stb_StatsPathHtmlMed = stb_ServerFilenameIdentifier + @"_playerstats_med.htm";//"          
        
        //int z = 0;
        //int maxIntValue = 2147483647;
        //z = maxIntValue + 10;
        //uint y = (uint)z;
        //Console.WriteLine("overflow {0} {1} {2}", z, y, (uint)y);

        stb_ChangeAttackProb = (stb_ChangeAttackProbHigh - stb_ChangeAttackProbLow) * stb_random.NextDouble() + stb_ChangeAttackProbLow;
        stb_ChangeBomberAttackProb = (stb_ChangeBomberAttackProbHigh - stb_ChangeBomberAttackProbLow) * stb_random.NextDouble() + stb_ChangeBomberAttackProbLow;
        stb_ChangeAttackProb_SmallGroup = (stb_ChangeAttackProb_SmallGroupHigh - stb_ChangeAttackProb_SmallGroupLow) * stb_random.NextDouble() + stb_ChangeAttackProb_SmallGroupLow;            

        //if (stb_Debug) Console.WriteLine("stb_ChangeAttackProb: " + stb_ChangeAttackProb + " stb_ChangeBomberAttackProb:" + stb_ChangeBomberAttackProb);

}

//To add a new general parameter, you need to 1. initialize the variable @ the top of this class with default value 2. Add it to the .ini file 3. Add the line in stb_loadINI to read that new variable 4. Add the name of the variable to the ini_vars string below, so that you can see it output @ console when stb_Debug is on
//Probably there is a smoother way to do all of this, perhaps by using a Dictionary (or 2-3 dictionaries for string, int, bool, double, etc) instead of individual variable names
private void stb_loadINI(string file)
    {

        
        IniFile ini = new IniFile(file, this);        

        stb_ServerName_Public = ini.IniReadValue("NAMES", "stb_ServerName_Public", stb_ServerName_Public);
        stb_ServerFilenameIdentifier = ini.IniReadValue("NAMES", "stb_ServerFilenameIdentifier", stb_ServerFilenameIdentifier);
        stb_AdminPlayernamePrefix = ini.IniReadValue("NAMES", "stb_AdminPlayernamePrefix", stb_AdminPlayernamePrefix);
        stb_LogStatsPublicAddressLow = ini.IniReadValue("NAMES", "stb_LogStatsPublicAddressLow", stb_LogStatsPublicAddressLow);
        stb_LocalMissionStatsDirectory = ini.IniReadValue("NAMES", "stb_LocalMissionStatsDirectory", stb_LocalMissionStatsDirectory);
        stb_LogStatsUploadBasenameLow = ini.IniReadValue("NAMES", "stb_LogStatsUploadBasenameLow", stb_LogStatsUploadBasenameLow);
        stb_StatsWebPageTagLine = ini.IniReadValue("NAMES", "stb_StatsWebPageTagLine", stb_StatsWebPageTagLine);
        stb_StatsWebPageLinksLine = ini.IniReadValue("NAMES", "stb_StatsWebPageLinksLine", stb_StatsWebPageLinksLine);
        stb_LogStatsUploadFtpBaseDirectory = ini.IniReadValue("NAMES", "stb_LogStatsUploadFtpBaseDirectory", stb_LogStatsUploadFtpBaseDirectory);
        stb_LogStatsUploadUserName = ini.IniReadValue("NAMES", "stb_LogStatsUploadUserName", stb_LogStatsUploadUserName);
        stb_LogStatsUploadPassword = ini.IniReadValue("NAMES", "stb_LogStatsUploadPassword", stb_LogStatsUploadPassword);        
        stb_NoRankMessages = ini.IniReadValue("FUNCTIONALITY", "stb_NoRankMessages", stb_NoRankMessages);
        stb_NoRankTracking = ini.IniReadValue("FUNCTIONALITY", "stb_NoRankTracking", stb_NoRankTracking);        
        stb_LogStatsDeadPilotsSuffix = ini.IniReadValue("FUNCTIONALITY", "stb_LogStatsDeadPilotsSuffix", stb_LogStatsDeadPilotsSuffix);
        stb_LogStatsTeamSuffix = ini.IniReadValue("FUNCTIONALITY", "stb_LogStatsTeamSuffix", stb_LogStatsTeamSuffix);
        stb_PlayerTimeoutWhenKilled = ini.IniReadValue("FUNCTIONALITY", "stb_PlayerTimeoutWhenKilled", stb_PlayerTimeoutWhenKilled);
        stb_PlayerTimeoutWhenKilledDuration_hours = ini.IniReadValue("FUNCTIONALITY", "stb_PlayerTimeoutWhenKilledDuration_hours", stb_PlayerTimeoutWhenKilledDuration_hours);
        stb_PlayerTimeoutWhenKilled_OverrideAllowed = ini.IniReadValue("FUNCTIONALITY", "stb_PlayerTimeoutWhenKilled_OverrideAllowed", stb_PlayerTimeoutWhenKilled_OverrideAllowed);
        stb_restrictAircraftByKills = ini.IniReadValue("FUNCTIONALITY", "stb_restrictAircraftByKills", stb_restrictAircraftByKills);
        stb_restrictAircraftByRank = ini.IniReadValue("FUNCTIONALITY", "stb_restrictAircraftByRank", stb_restrictAircraftByRank);
        stb_AnnounceStatsMessages = ini.IniReadValue("FUNCTIONALITY", "stb_AnnounceStatsMessages", stb_AnnounceStatsMessages);
        stb_AnnounceStatsMessagesFrequency = ini.IniReadValue("FUNCTIONALITY", "stb_AnnounceStatsMessagesFrequency", stb_AnnounceStatsMessagesFrequency);
        stb_StatsServerAnnounce = ini.IniReadValue("FUNCTIONALITY", "stb_StatsServerAnnounce", stb_StatsServerAnnounce);
        stb_LogStatsUploadAddressMed = ini.IniReadValue("FUNCTIONALITY", "stb_LogStatsUploadAddressMed", stb_LogStatsUploadAddressMed);
        
        stb_ChangeAttackProbHigh = ini.IniReadValue("FUNCTIONALITY", "stb_ChangeAttackProbHigh", stb_ChangeAttackProbHigh);
        stb_ChangeAttackProbLow = ini.IniReadValue("FUNCTIONALITY", "stb_ChangeAttackProbLow", stb_ChangeAttackProbLow);

        stb_ChangeBomberAttackProbHigh = ini.IniReadValue("FUNCTIONALITY", "stb_ChangeBomberAttackProbHigh", stb_ChangeBomberAttackProbHigh);
        stb_ChangeBomberAttackProbLow = ini.IniReadValue("FUNCTIONALITY", "stb_ChangeBomberAttackProbLow", stb_ChangeBomberAttackProbLow);

        stb_ChangeAttackProb_SmallGroupHigh = ini.IniReadValue("FUNCTIONALITY", "stb_ChangeAttackProb_SmallGroupHigh", stb_ChangeAttackProb_SmallGroupHigh);
        stb_ChangeAttackProb_SmallGroupLow = ini.IniReadValue("FUNCTIONALITY", "stb_ChangeAttackProb_SmallGroupLow", stb_ChangeAttackProb_SmallGroupLow);

        stb_POW_EscapeChanceRed = ini.IniReadValue("FUNCTIONALITY", "stb_POW_EscapeChanceRed", stb_POW_EscapeChanceRed);
        stb_POW_EscapeChanceBlue = ini.IniReadValue("FUNCTIONALITY", "stb_POW_EscapeChanceBlue", stb_POW_EscapeChanceBlue);
        stb_ASR_RescueChanceRed = ini.IniReadValue("FUNCTIONALITY", "stb_ASR_RescueChanceRed", stb_ASR_RescueChanceRed);
        stb_ASR_RescueChanceBlue = ini.IniReadValue("FUNCTIONALITY", "stb_ASR_RescueChanceBlue", stb_ASR_RescueChanceBlue);
        stb_ASR_RescueChanceFriendly = ini.IniReadValue("FUNCTIONALITY", "stb_ASR_RescueChanceFriendly", stb_ASR_RescueChanceFriendly);
        stb_ASR_RescueChanceEnemy = ini.IniReadValue("FUNCTIONALITY", "stb_ASR_RescueChanceEnemy", stb_ASR_RescueChanceEnemy);
        stb_ParachuteFailureRecoveryChance = ini.IniReadValue("FUNCTIONALITY", "stb_ParachuteFailureRecoveryChance", stb_ParachuteFailureRecoveryChance);


        stb_LogStatsDelay = ini.IniReadValue("FUNCTIONALITY", "stb_LogStatsDelay", stb_LogStatsDelay);
        stb_LogStats = ini.IniReadValue("LOG", "stb_LogStats", stb_LogStats);
        stb_LogErrors = ini.IniReadValue("LOG", "stb_LogErrors", stb_LogErrors);
        stb_LogStatsCreateHtmlLow = ini.IniReadValue("LOG", "stb_LogStatsCreateHtmlLow", stb_LogStatsCreateHtmlLow);
        stb_LogStatsCreateHtmlMed = ini.IniReadValue("LOG", "stb_LogStatsCreateHtmlMed", stb_LogStatsCreateHtmlMed);
        stb_LogStatsUploadHtmlLow = ini.IniReadValue("LOG", "stb_LogStatsUploadHtmlLow", stb_LogStatsUploadHtmlLow);
        stb_LogStatsUploadHtmlMed = ini.IniReadValue("LOG", "stb_LogStatsUploadHtmlMed", stb_LogStatsUploadHtmlMed);
        stb_Debug = ini.IniReadValue("LOG", "stb_Debug", stb_Debug);

        if (stb_Debug)
        {
            string[] ini_vars = { "stb_ServerName_Public", "stb_ServerFilenameIdentifier", "stb_AdminPlayernamePrefix", "stb_LogStatsPublicAddressLow", "stb_LocalMissionStatsDirectory", "stb_LogStatsUploadBasenameLow", "stb_StatsWebPageTagLine", "stb_StatsWebPageLinksLine", "stb_LogStatsUploadFtpBaseDirectory", "stb_LogStatsUploadUserName", "stb_LogStatsUploadPassword", "stb_ResetPlayerStatsWhenKilled", "stb_NoRankTracking", "stb_ResetPlayerStatsWhenKilled", "stb_LogStatsDeadPilotsSuffix", "stb_LogStatsTeamSuffix", "stb_PlayerTimeoutWhenKilled", "stb_PlayerTimeoutWhenKilledDuration_hours", "stb_PlayerTimeoutWhenKilled_OverrideAllowed", "stb_restrictAircraftByKills", "stb_restrictAircraftByRank", "stb_AnnounceStatsMessages", "stb_AnnounceStatsMessagesFrequency", "stb_StatsServerAnnounce", "stb_LogStatsUploadAddressMed", "stb_LogStatsDelay", "stb_LogStats", "stb_LogErrors", "stb_LogStatsCreateHtmlLow", "stb_LogStatsCreateHtmlMed", "stb_LogStatsUploadHtmlLow", "stb_LogStatsUploadHtmlMed", "stb_Debug"};
            foreach (string ini_var in ini_vars)
            {
                Console.WriteLine("Ini value read: {0} = {1}", ini_var, this.GetType().GetField(ini_var).GetValue(this));
            }
        }


        

        /*
         * This is all remarked out because these variables are now given a default & then overridden by the values in the stats.ini file, if it exists  (see code above)
         * These remarked-out code can probably just be removed now
    public string stb_ServerName_Public = "TWC Training Server"; //The name used publicly ie on the stats web pages & in "Welcome to the XXX" type messages.
    public string stb_ServerFilenameIdentifier = "TWCTRAINING"; //The "private" name of the server used as part of filenames. Will be combined with various suffixes to create filenames for e.g. stats files, log files for this server. Best to avoid using spaces or any strange characters that will cause trouble if part of a filename.
    public string stb_AdminPlayernamePrefix = @"TWC_"; //anyone whose username starts with this prefix will be allowed access to admin commands in the Chat Commands system. You could also set this to a full username if you want just one certain user to have admin access.
    public string stb_LogStatsPublicAddressLow = "TWCClan.com"; //Web address to visit for stats page. Used in announcement messages such as "Check your stats online at XXX". Used in-game, not clickable, so best to keep it simple.
    public string stb_LocalMissionStatsDirectory = @"missions\Multi\Fatal\"; //Local directory (ie, on the same hard drive as the CloD Server) where local copies of stats files, .htm files, log files, etc will be kept.  This is in relation to the directory where Launcher.exe /server is found.
    //If you specify the same directory for several missions (ie, several missions you run in rotation on your server) then all missions will share the same stats files and accumulate stats from mission to mission throughout the rotation. If you want to maintain separate stats for each separate mission, then use separate -stats.mis and -stats.cs files for each mission and modify each one ot use a different stb_LocalMissionStatsDirectory
    public string stb_LogStatsUploadBasenameLow = "training-server-stats"; //Will be used as the prefix as the initial part of the filename for public web pages created by the server. Directory & suffix will be added. EX: IF you enter XXX, you will get something like http://yourserver.com/yourdirectory/XXX.htm                                                                           
    public string stb_StatsWebPageTagLine = "<p>Visit <a href=\"http://twcclan.com\">TWCClan.com</a> for more information about TWC and the TWC Training Server.</p>"; //This is added at 3 points in the stats web page - allows you to link back to your main web page etc in a customizable way   
    public string stb_StatsWebPageLinksLine = "<p><i><b>Go to:</b> <a href=\"http://brenthugh.com/twc/mission-server-stats.htm\">Mission Server Stats</a> - <a href=\"http://brenthugh.com/twc/training-server-stats.htm\">Training Server Stats</a> - <a href=\"http://brenthugh.com/twc/practice-server-stats.htm\">Practice Server Stats</a> - <a href=\"http://brenthugh.com/twc/stats-archive.htm\">Older Stats Archive</a></i></p>";//This is added at 3 points in the stats web page & allows you to link to other mission stats pages you have or basically anything else you want to insert at these points. You'll have to use full http addresses for any links. Escape any need " characters with backslash, like \"
    public string stb_LogStatsUploadFtpBaseDirectory = "ftp://ftp.brenthugh.com/brenthugh.com/twc/"; //filenames will be added to this; trailing slash required.  Used for the FTP upload (ie, the FTP directory, not the publicly visible HTTP directory)
    public string stb_LogStatsUploadUserName = "exampleusername";  //FTP username
    public string stb_LogStatsUploadPassword = "examplepassword";  //FTP password
    //IMPORTANT NOTE: Upload sorttable.js and Style.css to stb_LogStatsUploadFtpBaseDirectory also--the stats web pages that will be uploaded to that directory depend on them to be viewed properly

    //FUNCTIONALITY RELATED CUSTOMIZATIONS
    public bool stb_ResetPlayerStatsWhenKilled = true;//If TRUE: When a player is killed, all stats reset to 0 (Old stats are still avail under different "dead player" name however). If FALSE: Player stats are compiled continuously regardless of player death.
    public string stb_LogStatsDeadPilotsSuffix = "-dead-pilots"; //Will be added to stb_LogStatsUploadBasenameLow when saving stats pages for the dead pilots list. Generally, no need to change or customize th is.

    public bool stb_PlayerTimeoutWhenKilled = true; //Whether to give a player a "timeout" when killed--a period of time when the player can't log in & play again
    public double stb_PlayerTimeoutWhenKilledDuration_hours = 0.083334; //Time (in hours) for the player timeout on death. Only active if stb_PlayerTimeoutWhenKilled  is TRUE
    public bool stb_PlayerTimeoutWhenKilled_OverrideAllowed = true; //Whether to give players the choice to override the death timeout

    public bool stb_restrictAircraftByKills = true; //Whether to restrict A/C until a certain # of kills is reached
    //int stb_restrictAircraftByKills_RequiredKillNumber = 200; //this has been replaced by a more customizable dictionary approach now

    public bool stb_restrictAircraftByRank = true; //Whether to restrict A/C until a certain ranks are reached

    public bool stb_AnnounceStatsMessages = true;
    public int stb_AnnounceStatsMessagesFrequency = 29 * 60; //seconds
    public bool stb_StatsServerAnnounce = true;
    public string stb_LogStatsUploadAddressMed = "";//not available yet
    public double stb_LogStatsDelay = 120.0;//seconds 60.0 default
                                            //public double stb_LogStatsDelay = 10.0;//for testing

    //LOG RELATED CUSTOMIZATIONS
    public bool stb_LogStats = true;//creates a local file, full stats in txt format. This is the main on/off switch for this entire module. If set to FALSE almost nothing else works--no stats collected, no files saves, etc.
    public bool stb_LogErrors = true;//creates a local file, errors in txt format, useful for IO or upload errors    
    public bool stb_LogStatsCreateHtmlLow = true;//creates a local file, summerized stats in html format
    public bool stb_LogStatsCreateHtmlMed = false;//not available yet - no output even if set to true. Concept is to create a stats page with higher level of detail than HtmlLow.
    public bool stb_LogStatsUploadHtmlLow = true;//set true to upload - this is the main stats web page in HTML format
    //Upload sorttable.js and Style.css to the same location on your site manually, created htm depends on them to be viewed properly
    public bool stb_LogStatsUploadHtmlMed = false;//not available yet

    *** BOTTOM OF SECTION THAT IS REMARKED OUT AND INOPERATIVE (REPLACED BY STATS.INI FILE SYSTEM)
    */
}

public StbContinueMissionRecorder stb_ContinueMissionRecorder;
    public StbRankToAllowedAircraft stb_RankToAllowedAircraft;
    public StbSaveIPlayerStat stb_SaveIPlayerStat;
    public StbAircraftParamStack stb_AircraftParamStack;
    //public KilledActorsWrapper stb_KilledActors;


    public struct StbContinueMission
    {
        public Point3d placeLeaveLoc;
        public Point3d placeEnterLoc;
        public bool alive;
        public bool mayContinue;
        public bool damagedSinceTakeoff;            
        public bool damagedOnlyBySelf;
        public bool isInPlanePlaceChange;
        public int selfDamageThisFlight;
        public int flightStartTime_sec;
        public int lastPositionEnter_sec; //seconds since 2016 or 0 if never
        public AiActor posLeftActor;
        public AiActor posEnterActor;
        public bool isPlaneLeave;
        public bool parachuted;
        public bool isForcedPlaceMove;

        public StbContinueMission (Point3d plc, bool al=true, bool mc=true)
        {
            placeLeaveLoc = plc;
            placeEnterLoc = new Point3d(-100000,-100000,-100000);
            alive=al;
            mayContinue=mc;
            damagedSinceTakeoff=false; //Between these two we can figure out if the only
            damagedOnlyBySelf=true;  //damage upon landing or de-spawning is self-inflicted
            isInPlanePlaceChange = false;
            selfDamageThisFlight =0; //amount of self-damage sustained during the flight. Perhaps we can be merciful if the amt of self-damage is slight.
            flightStartTime_sec= Calcs.TimeSince2016_sec();
            lastPositionEnter_sec = 0;
            posLeftActor = null;
            posEnterActor = null;
            isPlaneLeave = false;
            parachuted = false;
            isForcedPlaceMove = false;
        }

        //public override string ToString (string frmt)
        public override string ToString()
        {
            string frmt = "N1";
            return "(" + placeLeaveLoc.x.ToString(frmt) + ", "
               + placeLeaveLoc.y.ToString(frmt) + ", "
               + placeLeaveLoc.z.ToString(frmt) + ") "
               + "Alive: " + alive.ToString()
               + " MC: " + mayContinue.ToString()
               + " Dmgd since takeoff: " + damagedSinceTakeoff.ToString()
               + " Dmgd only by self: " + damagedOnlyBySelf.ToString()
               + " Was it an in aircraft place switch? " + isInPlanePlaceChange.ToString()
               + " Amt self damage this flight: " + selfDamageThisFlight.ToString()
               + " Flight start time: " + flightStartTime_sec.ToString()
               + " IsPlaneLeave: " + isPlaneLeave.ToString()
               + " parachuted: " + parachuted.ToString()
               + " isForcedPlaceMove: " + isForcedPlaceMove.ToString(); ;
        }
    }


    public class AircraftParams
    {

        public Point3d Loc;
        public Vector3d Vwld;
        public double time_tick = 0;

        public double Z_AltitudeAGL = 0;
        public double Z_VelocityMach = 0;
        public double Z_VelocityMPH = 0;
        public double vel_mps = 0;
        public double vel_mph = 0 ;
        public double heading = 0;
        public double pitch = 0;
        public AiActor actor = null;

        public AircraftParams()
        {
        }

        public AircraftParams(Point3d l, Vector3d v, double t = 0, double agl = 0, double vm=0, double vmps = 0, double vmph = 0, double h = 0, double p = 0, AiActor a = null)
        {
            Loc = l;
            Vwld = v;
            time_tick = Calcs.TimeSince2016_ticks();
            Z_AltitudeAGL = agl;
            Z_VelocityMach = vm;
            Z_VelocityMPH = vm * 600;
            vel_mps = vmps;
            vel_mph = vmph;
            heading = h;
            pitch = p;
            actor = a;
        }

        public AircraftParams (AiActor a) {
            time_tick = Calcs.TimeSince2016_ticks();
            if (a == null) return;
            actor = a;
            Loc = actor.Pos();
            if ((a as AiAircraft) != null) {
                AiAircraft aircraft = a as AiAircraft;
                Z_VelocityMach = aircraft.getParameter(part.ParameterTypes.Z_VelocityMach, 0);
                Z_VelocityMPH = Math.Abs(Z_VelocityMach) * 600; //this is an approximation but good enough for our purposes.
                                                                     //We use Z_VelocityMach because it seems more stable/predictable when passed through onDeadActor and also it is
                                                                     //unit invariant--it comes back as a % of mach whether we are using English or metric units
                                                                     //sometimes Z_VelocityMach is negative, which seems to indicate you are going backwards @ that speed.


                //double I_VelocityIAS = 0; // aircraft1.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
                Z_AltitudeAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                //double S_GunReserve = aircraft1.getParameter(part.ParameterTypes.S_GunReserve, 0);
                //double S_GunClipReserve = aircraft1.getParameter(part.ParameterTypes.S_GunClipReserve, 0);
                //double S_GunReserve = 0; aircraft1.getParameter(part.ParameterTypes.S_GunReserve, 0);
                //double S_GunClipReserve = 0; aircraft1.getParameter(part.ParameterTypes.S_GunClipReserve, 0);

                Vwld = aircraft.AirGroup().Vwld();
                vel_mps = Calcs.CalculatePointDistance(Vwld);
                vel_mph = Calcs.meterspsec2milesphour(vel_mps);
                heading = (Calcs.CalculateBearingDegree(Vwld));
                pitch = Calcs.CalculatePitchDegree(Vwld);

                //So, pitch seems to work well for some aircraft (ie Hurricane) but almost always shows as "0.0" for others (ie Blennie)
                //So it seems too unreliable ot use here.  Also vel_mph & other data from Vwld seem unreliable in the case of a
                //a crash like this, perhaps for the same reason.  It is perhaps being sampled a bit too late to be of use to us here.


                //vel_mps vel_mph heading pitch


            }



        }


        //public override string ToString (string frmt)
        public override string ToString()
        {
            string frmt = "N1";
            string ret = "(" + Loc.x.ToString(frmt) + ", "
               + Loc.y.ToString(frmt) + ", "
               + Loc.z.ToString(frmt) + ") "
               + "(" + Vwld.x.ToString(frmt) + ", "
               + Vwld.y.ToString(frmt) + ", "
               + Vwld.z.ToString(frmt) + ") "
               + "Time: " + time_tick.ToString()
               + " Alt: " + Z_AltitudeAGL.ToString(frmt)
               + " Vel_mps: " + vel_mps.ToString(frmt)
               + " Vel_mph: " + vel_mph.ToString(frmt)
               + " heading: " + heading.ToString(frmt)
               + " pitch: " + pitch.ToString(frmt);

            if (actor != null && actor.Name() != null) ret += " actor: " + actor.Name();
            return ret;
        }
    }

    

    //Save aircraft state for players periodically & save the last few seconds data, so that when something happens (ie crash, landing,
    //whatever) we have some idea of what was going on just before that
    public class StbAircraftParamStack {

        Dictionary<string, CircularArray<AircraftParams>> aircraftParams = new Dictionary<string, CircularArray<AircraftParams>>();
        double recursInterval = 0.2; //time between saving params
        int array_size = 6; //Number of param sets to save on the stack, for each player/aircraft
        Mission mission;

        public StbAircraftParamStack ( Mission m ) {
            mission = m;
            saveAircraftParamsRecursive();
        }

        private void addParams(Player player ) {
        
            CircularArray<AircraftParams> apa = new CircularArray<AircraftParams>(array_size);

            if (!aircraftParams.TryGetValue(player.Name(), out apa)) apa = new CircularArray<AircraftParams>(array_size);

            AircraftParams ap = new AircraftParams(player.Place());

            //if (mission.stb_Debug) Console.WriteLine("addParams: " + ap.ToString());

            apa.Push(ap);

            aircraftParams[player.Name()] = apa;

        }

        //returns the player's paramstack as an array (and as a stack, ie latest params are index 0, index 1 is before that, etc)
        public AircraftParams[] returnParamsArray(Player player)
        {

            CircularArray<AircraftParams> apa = new CircularArray<AircraftParams>(array_size);

            if (!aircraftParams.TryGetValue(player.Name(), out apa)) apa = new CircularArray<AircraftParams>(array_size);

            return apa.ArrayStack; 

        }

        //figures the max/min of a few key values over the life of the params - steepest pitch, highest downward/z velocity, highest speed, etc
        public AircraftParams returnMaxes(Player player)
        {

            CircularArray<AircraftParams> apa = new CircularArray<AircraftParams>(array_size);

            if (!aircraftParams.TryGetValue(player.Name(), out apa)) apa = new CircularArray<AircraftParams>(array_size);

            AircraftParams apMax = new AircraftParams();
            bool starting = true;

            //return apa.ArrayStack;
            foreach (AircraftParams ap in  apa.ArrayStack) {

                //if (starting || ap.Vwld.z < apMax.Vwld.z) apMax.Vwld = ap.Vwld;  //Gets Vwld @ moment of max downward velocity 
                if (starting || apMax.Vwld.z <= 0)
                {
                    apMax.Vwld = ap.Vwld;  //Gets Vwld.z just before hitting ground (ie Vwld.z = 0 or greater)
                    apMax.vel_mph = ap.vel_mph; //also gets the overall velocity at this moment--the moment just before impact w/ the ground
                    //if we didn't hit the ground (say, Vwld.z is always 0 or greater) then we must have hit into something as we were flying or rolling across the ground.  In that case we get the first value on the stack which is OK--this is the speed etc just before impact.
                }
                //if (starting || ap.vel_mph > apMax.vel_mph) apMax.vel_mph = ap.vel_mph;  //Gets largest vel_mph
                if (apMax.vel_mph <=0 && ap.vel_mph > 0) apMax.vel_mph = ap.vel_mph;  //One exception--if the vel we pick up @ moment of impact is zero then we'll replace it with the previous one, if that is larger
                //if (starting || ap.pitch < apMax.pitch) apMax.pitch = ap.pitch;  //Gets smallest pitch (ie, most negative)
                if (starting || apMax.pitch <= 0 ) apMax.pitch = ap.pitch;  //Similarly, gets pitch just before hitting ground, ie, just before pitch goes 0 or positive
                if (starting) apMax.actor = ap.actor;

                starting = false;


            }

            return apMax;

        }
        //returns a particular param for the player.  0 is most recent, 1 just before that etc.
        public AircraftParams returnParams(Player player, int index)
        {

            CircularArray<AircraftParams> apa = new CircularArray<AircraftParams>(array_size);

            if (!aircraftParams.TryGetValue(player.Name(), out apa)) apa = new CircularArray<AircraftParams>(array_size);

            //return apa.GetStack(index); //Note - 10/2018 - I belive the GetStack is incorrect for this purpose & that GetStack will return the OLDEST param in the bin
            return apa.Get(index);  //Get will return the NEWEST item, the one most recently pushed  on.  
            //This doesn't seem to vital as this function is never used (?)

        }

        private void saveAircraftParamsRecursive()
        {
            try
            {

                mission.Timeout(recursInterval, () => { saveAircraftParamsRecursive(); });

                if (mission.GamePlay.gpPlayer() != null)
                {

                    addParams(mission.GamePlay.gpPlayer());
                } // Multiplayer
                if (mission.GamePlay.gpRemotePlayers() != null || mission.GamePlay.gpRemotePlayers().Length > 0)
                {
                    foreach (Player p in mission.GamePlay.gpRemotePlayers())
                    {

                        addParams(p);
                    }
                }




            }
            catch (Exception e) { System.Console.WriteLine("saveAicraftParamsRecurs: " + e.ToString()); }

        }


    }



    //This is a partial implementation of maddox.game.IPlayerStat with only the particular fields that we are interested in saving
    //It has an additional int[] array where we can save current session values when they go into StatRecorder, thus
    //having a record of things that happened in this session ONLY in addition to the overall/all time records in StatRecorder
    public class Stb_PlayerSessStat {        

        private Mission mission;
        public int landings = 0;
        public int bombsFire = 0;
        public int bombsHit = 0;
        public double bombsWeight = 0; //This appears to be the weight of all bombs fired, not just those that hit a target, in kg
        public int bulletsFire = 0;
        public int bulletsHit = 0; //bullets hitting any thing (presumably any target?)
        public int bulletsHitAir = 0;  //bullets hitting an aircraft
        public double fkills = 0;
        //gkills = 0;  //presumably ground kills, though I haven't been able to get any to register.  Array of int
        //fgkills = 0; //presumably friendly ground kills.  int[]
        public double kills = 0;  //mirror of the kills # shown in CloD netstats
        public int deaths = 0;
        public int bails = 0;
        public int ditches = 0;
        public int planeChanges = 0;
        public int planesWrittenOff = 0;
        public Player player = null;
        //public int[] sessStats = null;
        public Dictionary<int, int> sessStats = new Dictionary<int, int>();

        
       
        public Stb_PlayerSessStat (Mission msn) {

            mission = msn;
            //sessStats=new int[mission.stb_StatRecorder.stbSr_numStats];
        }
        

        //set
        public void Set(IPlayerStat Ips)
        {
            //create a null IPlayerStat for initializing any player's IPStat
            landings=Ips.landings;
            bombsFire=Ips.bombsFire;
            bombsHit=Ips.bombsHit;
            bombsWeight=Ips.bombsWeight;
            bulletsFire=Ips.bulletsFire;
            bulletsHit=Ips.bulletsHit;
            bulletsHitAir=Ips.bulletsHitAir;
            fkills=Ips.fkills;
            //gkills=Ips.gkills;
            //fgkillIps.fgkills;
            kills=Ips.kills;
            deaths=Ips.deaths;
            bails=Ips.bails;
            ditches=Ips.ditches;
            planeChanges=Ips.planeChanges;
            planesWrittenOff=Ips.planesWrittenOff;
            player = Ips.player;

            //we don't touch the existing sessStats array, which is one reason we need to update an existing sessStats here instead of just starting one afresh
        }

        //Gets the value @ sessStat[index] while bumbling around the little irritating thing you have to do with Dictionaries in case the index doesn't already exist
        public int getSessStat(int index) {

            int currValue = 0;

            if (!sessStats.TryGetValue(index, out currValue))
            {
                currValue = 0;
            }

            return currValue;

        }

        public string ToString () {
            
            return String.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13}",
                    landings,
                    bombsFire,
                    bombsHit,
                    bombsWeight,
                    bulletsFire,
                    bulletsHit,
                    bulletsHitAir,
                    fkills,
                    //gkills,
                    //fgkillIps.fgkills,
                    kills,
                    deaths,
                    bails,
                    ditches,
                    planeChanges,
                    planesWrittenOff,
                    player);

        }

    }

    //the purpose of this class is to track certain stats that CLoD provides over the course of a mission for each player.  Clod provides a running total of these stats for the player's entire session.
    //We save these stats for each player when the player joins, when the player quits, and a few times in between.
    //Then we do some calculations to figure out how much of each of these various stats the player has accumulated
    //over the course of the entire session and save the stat accumulation to our TWC stats for the player.
    //This is complicated by the fact that we 1. Don't want to double-count stats for a player but 2. Want to save them somewhat
    //often to ensure that they are not lost by some glitch or session end and 3. The player will sometimes die etc and
    //for our purposes we care about stats PER LIFE not necessarily per session or per mission.  So we have to 
    //track the CloD stats in various ways and then split them up or assign them to the player's lives at various points, being
    //sure not to double-count
    public class StbSaveIPlayerStat
    {
        public Dictionary<string, Stb_PlayerSessStat> stbSis_saveIPlayerStat = new Dictionary<string, Stb_PlayerSessStat>();
        Mission mission;
        private bool stbSis_LogErrors;
        private string stbSis_ErrorLogPath;

        public Stb_PlayerSessStat BlueSessStats; //the overall stats summary for both teams for <obj
        public Stb_PlayerSessStat RedSessStats;



        //constructor
        public StbSaveIPlayerStat(Mission msn, bool le, string elp)
        {

            stbSis_LogErrors = le;
            stbSis_ErrorLogPath = elp;

            mission = msn;

            BlueSessStats = new Stb_PlayerSessStat(mission);
            RedSessStats = new Stb_PlayerSessStat(mission);

        }

        //run through all entries (by playername) in the dictionary, and save them. This depends on a dictionary entry each player 
        //having been initialized at some point, so do StbSis_Save(player) for each player at some logical point, like when they connect
        public void StbSis_SaveAll()
        {
            //OK, so StbSis_Save changes the value of stbSis_saveIPlayerStat during its run.  So that means
            //we can't use stbSis_saveIPlayerStat in the foreach loop because it changes mid-loop
            //So instead we run the loop with a temp copy
            Dictionary<string, Stb_PlayerSessStat> stbSis_saveIPlayerStatTEMP = new Dictionary<string, Stb_PlayerSessStat>(stbSis_saveIPlayerStat);
            //stbSis_saveIPlayerStatTEMP = stbSis_saveIPlayerStat; //doing it this way just makes them two different names for the same actual object

            foreach (KeyValuePair<string, Stb_PlayerSessStat> entry in stbSis_saveIPlayerStatTEMP)
            {
                StbSis_Save(entry.Value.player);
            }
            /*foreach (string playername in stbSis_saveIPlayerStat.Keys.ToList())
            {
                StbSis_Save(stbSis_saveIPlayerStat[playername]);
            }*/
        }

        /*

        //Saves by Player Name. If the stats dictionary entry for that player doesn't yet exist, it is created.
        public void StbSis_Save(string playername)    
        {
            var Stats = new Stb_PlayerSessStat();

            if (!stbSis_saveIPlayerStat.TryGetValue(player.Name(), out Stats))
            {
                Stats = new Stb_PlayerSessStat();
            } else StbSis_Save(Stats.player); //already has an entry in the dictionary, so we can look up the .player there & proceed
        }
        */

        //Saves by Player. If the stats dictionary entry for that player doesn't yet exist, it is created.
        public Stb_PlayerSessStat StbSis_Save(Player player)
        {
            if (player is IPlayer && player.Name() != null && player.Name().Length > 0)
            {
                var OldStats = new Stb_PlayerSessStat(mission);

                if (!stbSis_saveIPlayerStat.TryGetValue(player.Name(), out OldStats))
                {
                    OldStats = new Stb_PlayerSessStat(mission);
                } //See explanation for this idiom under StbSr_UpdateStatsForMission

                IPlayerStat CurrStats = (player as IPlayer).GetBattleStat() as IPlayerStat;
                StbSis_WriteDiff(player as Player, OldStats, CurrStats);
                OldStats.Set(CurrStats);
                stbSis_saveIPlayerStat[player.Name()] = OldStats;  //now that we have updated the Mission stats with any new/updated values in PlayerStat, we update the PlayerStat to the current values, so we can rinse & repeat next time around.

                //if (stbSis_LogErrors) 
                //Console.WriteLine("Stats for " + player.Name() + ": " + OldStats.ToString());

                //               player.Name(), st.landings, st.kills, st.fkills, st.gkills[0], st.fgkills[0], st.deaths, st.bails, st.ditches, st.planeChanges, st.planesWrittenOff, M_Health, M_CabinState, M_SystemWear, cdam, ndam, st.bombsFire, st.bombsHit, st.bombsWeight, st.bulletsFire, st.bulletsHit, st.bulletsHitAir, GetDictionary(st.killsTypes), GetDictionary(st.tTotalTypes), st.gkills[1], st.gkills[2], st.gkills[3]));

                return OldStats; //OldStats is actually now current, so return it in case it is wanted.
            }
            else return null;
        }

        private void StbSis_WriteDiff(Player player, Stb_PlayerSessStat OldStats, IPlayerStat CurrStats)
        {

            //Console.WriteLine("Writing DIFF {0}", player.Name());
            StbSis_AddToMissionStat(player, 835, (int)(Math.Round((CurrStats.kills - OldStats.kills) * 100)));
            StbSis_AddToMissionStat(player, 836, (int)(Math.Round((CurrStats.fkills - OldStats.fkills) * 100)));
            StbSis_AddToMissionStat(player, 837, CurrStats.bulletsFire - OldStats.bulletsFire);
            StbSis_AddToMissionStat(player, 838, CurrStats.bulletsHit - OldStats.bulletsHit);
            StbSis_AddToMissionStat(player, 839, CurrStats.bulletsHitAir - OldStats.bulletsHitAir);
            StbSis_AddToMissionStat(player, 840, CurrStats.bombsFire - OldStats.bombsFire);
            StbSis_AddToMissionStat(player, 841, CurrStats.bombsHit - OldStats.bombsHit);

            double bombsOnTarget_kg = 0;
            if ((CurrStats.bombsFire - OldStats.bombsFire) > 0) bombsOnTarget_kg = (double)(CurrStats.bombsHit - OldStats.bombsHit) / (double)(CurrStats.bombsFire - OldStats.bombsFire) * (CurrStats.bombsWeight - OldStats.bombsWeight); //This is an approximation is which is exact if all bombs deployed were of the same weight, and our best guess of KG on target if the various bombs were of varying weights.  If bombs were of varying weights, we just don't have the info here about which of them were the ones that hit & which missed.


            StbSis_AddToMissionStat(player, 842, (int)(Math.Round(bombsOnTarget_kg)));

            //We do 'writeDiff' for CloD generated stats where we need to occasionally 'write the difference' to figure out what happened in the CloD stats and save the changes for this session.
            //But for 845 we are generating this stat directly so can just save it directly using StbSis_AddSessStat so we don't need to ALSO do the writeDiff
            //StbSis_AddToMissionStat(player, 845, CurrStats.planesWrittenOff - OldStats.planesWrittenOff);  //Note that this is the TWC 'corrected' planes written off stat (845) NOT the raw Netstats planes written off (843)

            //gkills=Ips.gkills;
            //fgkillIps.fgkills;

            //player = Ips.player;


        }

        public void StbSis_AddToMissionStat(Player player, int index, int value)
        {
            if (mission.stb_Debug) Console.WriteLine("Writing CloD Stats for {0}: {1} {2}", player.Name(), index, value);
            if (value == 0) return; //If it's zero we don't need to do anything
            Mission.StbStatTask sst1 = new Mission.StbStatTask(Mission.StbStatCommands.Mission, player.Name(), new int[] { index, value }, player as AiActor);
            mission.stb_StatRecorder.StbSr_EnqueueTask(sst1);

            int currValue = 0;
            //Now add the value to the team stats for this session
            if (player.Army() == 2)
            {
                currValue = BlueSessStats.getSessStat(index);
                BlueSessStats.sessStats[index] = currValue + value;
            }
            if (player.Army() == 1)
            {

                currValue = RedSessStats.getSessStat(index);
                RedSessStats.sessStats[index] = currValue + value;

            }



        }

        public void StbSis_IncrementSessStat(Player player, int index)
        {
            var PlayerStats = new Stb_PlayerSessStat(mission);

            if (!stbSis_saveIPlayerStat.TryGetValue(player.Name(), out PlayerStats))
            {
                PlayerStats = StbSis_Save(player);
            }

            int currValue = PlayerStats.getSessStat(index);

            PlayerStats.sessStats[index] = currValue + 1;
            /* if (index < 0 || index >= PlayerStats.sessStats.Length) return; // index out of range
            PlayerStats.sessStats[index]++;
            */

            //Now add the value to the team stats for this session
            if (player.Army() == 2)
            {
                currValue = BlueSessStats.getSessStat(index);
                BlueSessStats.sessStats[index] = currValue + 1;
            }
            if (player.Army() == 1)
            {

                currValue = RedSessStats.getSessStat(index);
                RedSessStats.sessStats[index] = currValue + 1;
            }
        }

        public void StbSis_AddSessStat(Player player, int index, int value)
        {
            var PlayerStats = new Stb_PlayerSessStat(mission);

            if (!stbSis_saveIPlayerStat.TryGetValue(player.Name(), out PlayerStats))
            {
                PlayerStats = StbSis_Save(player);
            }

            int currValue = PlayerStats.getSessStat(index);

            PlayerStats.sessStats[index] = currValue + value;

            /*  The int[] way - but since we are saving just a very few values from a large int[] array the Dictionary approach seems better

            if (index < 0 || index >= PlayerStats.sessStats.Length) return; // index out of range
            PlayerStats.sessStats[index] += value;\

            */

            //Now add the value to the team stats for this session
            if (player.Army() == 2)
            {
                currValue = BlueSessStats.getSessStat(index);
                BlueSessStats.sessStats[index] = currValue + value;
            }
            if (player.Army() == 1)
            {

                currValue = RedSessStats.getSessStat(index);
                RedSessStats.sessStats[index] = currValue + value;

            }


        }
    }


    public class StbContinueMissionRecorder
    {
        //private readonly Mission outer; //allows us to reference methods etc from the Mission class as 'outer'
        public Dictionary<string, StbContinueMission> stbCmr_ContinueMissionInfo;
        bool stbCmr_LogErrors;
        string stbCmr_ErrorLogPath;
        StbStatRecorder stbcmr_StatRecorder; 
        private Mission mission;

        public StbContinueMissionRecorder (Mission mission, bool le, string elp) {
        
          stbCmr_ContinueMissionInfo = new Dictionary<string, StbContinueMission>();        
          stbCmr_LogErrors = le;
          stbCmr_ErrorLogPath = elp;
          this.mission = mission; //gets current instance of Mission for use later
          //Mission.Mission.StbStatRecorder stbcmr_StatRecorder = new Mission.Mission.StbStatRecorder();
        
        }
        
        
        //TODO: There may be an isuse with positionenter / positionleave when
        //bombers are involve,d becaused players often leave & enter various
        //positions numerous times during a single flight.  Needs testing.
        public void StbCmr_SavePositionEnter (Player player, AiActor actor) 
        {   
          try 
          {
            //Console.WriteLine("PlaceEnter: 3 ");
            if (player is Player && actor is AiActor && player!=null && actor != null && player.Name() != null ) {
            
              StbContinueMission cm = new StbContinueMission ();              
              if ( stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm))
              { //Ok the person has newly spawned in and was previously flying, we check to see if 1. the person has died since last takeoff and 2. mayContinue is true (mayContinue is a signal from the previous event, the positionLeave in this case, that it was Ok to continue the mission @ that point). 3. Is taking off from the same place they recently landed.  If haven't died, mayContinue is true & taking off from same place, within 2000 of the place where they left their previous plane/position, they can continue the mission.
                  
                  //This is used by onPositionLeave to determine whether the PositionLeave is the end of a flight or not
                  //PositionLeave waits 5 seconds, then checks whether there has been a recent Position Enter.
                  //If so , PositionLeave aborts, as this is just a bomber pilot switching positions w/in the aircraft
                  cm.lastPositionEnter_sec = Calcs.TimeSince2016_sec();
                  cm.posEnterActor = actor;

                  //Console.WriteLine("PlaceEnter: 4 ");
                  cm.placeEnterLoc.x = actor.Pos().x;
                  cm.placeEnterLoc.y = actor.Pos().y;
                  double dis_meters = Calcs.CalculatePointDistance (cm.placeLeaveLoc, actor.Pos());
                  bool maycont=false;
                  if (cm.mayContinue && cm.alive && ( dis_meters < 2000)) maycont=true;  
                        cm.mayContinue=maycont;

                  /* Console.WriteLine("SPE: Place Change by " + dis_meters.ToString() + " meters. PELx: " + cm.placeEnterLoc.x.ToString("N0")
                    + " PELy: " + cm.placeEnterLoc.y.ToString("N0") + " PLLx: " + cm.placeLeaveLoc.x.ToString("N0")
                    + " PELy: " + cm.placeLeaveLoc.y.ToString("N0"));
                  */

                  //Console.WriteLine("SPE: " + cm.ToString());
                  cm.isInPlanePlaceChange = false;
                  if (dis_meters < 5)
                  {
                        cm.isInPlanePlaceChange = true; //hoping that dis_meters > 5 will catch e.g. bombers where the player switches position to position but it's still the same flight
                            //Console.WriteLine("That was an in-plane place change!" + dis_meters.ToString() + " meters");
                  }

                  //we re-set these (below) here rather than on takeoff complete, bec. we want to count any take-off damage.
                  cm.damagedSinceTakeoff=false; //Between these two we can figure out if the only
                  cm.damagedOnlyBySelf=true;  //damage upon landing or de-spawning is self-inflicted
                  cm.selfDamageThisFlight=0;
                  cm.flightStartTime_sec= Calcs.TimeSince2016_sec(); //we'll reset this ontookoff, but this keeps it from being zero regardless; we don't want that 
                  
                                     
              
              } else { //newly entered player; this can't be a continuation
                  //if the player doesn't yet exist, we initialize it
                  //with start position; so far the player is alive & bu may not continue (ie, the current flight is not a continuation of a previous successful flight)
                  //p = player.Place() as AiAircraft;          
                  //p.Pos().x, p.Pos().y)
                  cm = new StbContinueMission ();
                  cm.placeLeaveLoc.x=-1000000;
                  cm.placeLeaveLoc.y=-1000000;
                  cm.alive=true;
                  cm.mayContinue=false;
                  cm.damagedSinceTakeoff=false; //Between these two we can figure out if the only
                  cm.damagedOnlyBySelf=true;  //damage upon landing or de-spawning is self-inflicted
                  cm.selfDamageThisFlight=0;
                  cm.flightStartTime_sec= Calcs.TimeSince2016_sec();
                  cm.lastPositionEnter_sec = cm.flightStartTime_sec;



              }


                //record time of last access while we have it handy             
                Mission.StbStatTask sst = new Mission.StbStatTask(Mission.StbStatCommands.Mission, player.Name(), new int[] { 829, 0, cm.flightStartTime_sec
                }, player.Place() as AiActor); //putting the # in the [2] position (ie, 829, 0, MYNEWVALUE) in StbStatCommands.Mission means to set the value to that number, vs putting it in the [1] position which ADDS the value to the existing entry
                    this.mission.stb_StatRecorder.StbSr_EnqueueTask(sst);
                
                stbCmr_ContinueMissionInfo[player.Name()]= cm;
                //Console.WriteLine( "PosEnter: " + player.Name() + " " + cm.ToString("F0"));
            }                                      
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        }

        private void StbCmr_SavePositionLeave_work(Player player, AiActor actor, StbContinueMission cm)
        {
            bool endSortieDamagedAndOnlyBySelf = false;
            cm.isPlaneLeave = true; //Yes, it IS a plane leave, NOT just a position switch.  OnPlaceLeave() will be able to detect this (using a Timeout, since this is calced 0.1 seconds after the initial onPlaceLeave() is called) and take appropriate action.


            //p = player.Place() as AiAircraft;          
            //p.Pos().x, p.Pos().y)
            //Save the position where they left the a/c.  If the player is alive, the player may continue the mission, if they (can) take off from near the same location again.  Exception: If they have just had a mission where they only had self-injury (ie, they blew their rads or just crashed for no special reason) they cannot continue the mission

            bool actorAlive = actor.IsAlive();
            endSortieDamagedAndOnlyBySelf = cm.damagedSinceTakeoff && cm.damagedOnlyBySelf;

            int endFlightTime_sec = Calcs.TimeSince2016_sec();
            int flightDuration_sec = endFlightTime_sec - cm.flightStartTime_sec;
            if (cm.flightStartTime_sec == 0 || flightDuration_sec > 24 * 60 * 60) flightDuration_sec = 0; //sanity check; often 'place leave' is the first time cm is initialized for a given pilot. 

            //Console.WriteLine("PosLeave: " + cm.flightStartTime_sec.ToString() + " " + endFlightTime_sec.ToString() + " " + flightDuration_sec.ToString() + " " + ((double)flightDuration_sec / 60).ToString("F1"));

            bool endSortieDamagedAndOnlyBySelfandShortFlightorLongFlightAndMuchSelfDamage = false;

            if (endSortieDamagedAndOnlyBySelf && (flightDuration_sec < 10 * 60)  && flightDuration_sec > 0)
                endSortieDamagedAndOnlyBySelfandShortFlightorLongFlightAndMuchSelfDamage = true; //Short flight & all self-damage, your mission is over

            if (endSortieDamagedAndOnlyBySelf && (flightDuration_sec >= 10 * 60) && (cm.selfDamageThisFlight > 10))
                endSortieDamagedAndOnlyBySelfandShortFlightorLongFlightAndMuchSelfDamage = true;   //OK, we're giving people a break if they have had a long flight & only a few self-damages, but if they have a LOT Of self-damage their Mission is still over

            cm.flightStartTime_sec = endFlightTime_sec; //reset the flight start time (though the 'real' flight start time is onTookOff; this is belt'n'suspenders)

            //record the time for the flight              
            Mission.StbStatTask sst = new Mission.StbStatTask(Mission.StbStatCommands.Mission, player.Name(), new int[] { 792, flightDuration_sec }, player.Place() as AiActor);
            if (flightDuration_sec > 0) this.mission.stb_StatRecorder.StbSr_EnqueueTask(sst);



            //record time of last access while we're at it               
            sst = new Mission.StbStatTask(Mission.StbStatCommands.Mission, player.Name(), new int[] { 829, 0, endFlightTime_sec
                                }, player.Place() as AiActor);  //putting the # in the [2] position (ie, 829, 0, MYNEWVALUE) in StbStatCommands.Mission means to set the value to that number, vs putting it in the [1] position which ADDS the value to the existing entry
            this.mission.stb_StatRecorder.StbSr_EnqueueTask(sst);

            if (cm.alive && actorAlive && !endSortieDamagedAndOnlyBySelfandShortFlightorLongFlightAndMuchSelfDamage && flightDuration_sec > 0)
            {
                cm.mayContinue = true;
                if (!cm.isForcedPlaceMove) this.mission.Stb_Message(new Player[] { player }, "Important Notice: When you land safely at an airport and and take off again from that same airport, your Continuous Mission will continue unbroken (for stats purposes).", new object[] { });
                //this.mission.Stb_Message(new Player[] { player }, "If you choose to take off from a *different* airport, that ends your continuous flight. Kills-per-flight is an important factor in accelerating your promotion to higher rank. Check stats at " + this.mission.stb_LogStatsPublicAddressLow + " or in-game using commands <career and <sess.", new object[] { });
                if (!cm.isForcedPlaceMove) this.mission.Stb_Message(new Player[] { player }, "Your last sortie was " + ((double)flightDuration_sec / 60).ToString("F1") + " minutes long", new object[] { });


            }


            //Record the fact that the sortie was ended with only self-damage.  ID=791
            if (cm.alive && actorAlive && endSortieDamagedAndOnlyBySelfandShortFlightorLongFlightAndMuchSelfDamage && flightDuration_sec > 0)
            {
                Mission.StbStatTask sst1 = new Mission.StbStatTask(Mission.StbStatCommands.Mission, player.Name(), new int[] { 791 }, player.Place() as AiActor);
                this.mission.stb_StatRecorder.StbSr_EnqueueTask(sst1);
                this.mission.Stb_Message(new Player[] { player }, "Notice: On your last sortie, you self-damaged your aircraft.  For that reason, your next sortie will start a new mission (for stats purposes).", new object[] { });
                //this.mission.Stb_Message(new Player[] { player }, "Self-damage was registered " + cm.selfDamageThisFlight.ToString() + " times on the last sortie.  Check stats at " + this.mission.stb_LogStatsPublicAddressLow, new object[] { });
                //this.mission.Stb_Message(new Player[] { player }, "Your last flight was " + ((double)flightDuration_sec / 60).ToString("F1") + " minutes long", new object[] { });

            }

            if (!cm.alive || !actorAlive)
            {
                //OK, for some reason this message appears sometimes when the player doesn't actually die.  Maybe when they are not alive beforehand?  Possible Answer: .IsAlive() is actually for the aircraft/vehicle indicating whether it is operative or not.  Nothing to do with whether the person is alive.  This is how it works with AiAirdraft.IsKilled().
                //So we have moved this to a different place
                //this.mission.Stb_Message(new Player [] { player } , "Notice: When you die, your stats are reset. Check stats at TWCClan.com.", new object[] { });                                
                //this.mission.Stb_Message(new Player[] { player }, "Player death recorded. Check stats at TWCClan.com.", new object[] { });
            }
            //return endSortieDamagedAndOnlyBySelf;            
            cm.isForcedPlaceMove = false; //if it was an <rr or other forced move that's over now and it's definitely not one now
            stbCmr_ContinueMissionInfo[player.Name()] = cm;
            //Console.WriteLine( "PosLeave: " + player.Name() + " " + cm.ToString("F0"));
        }
        
        
        public void StbCmr_SavePositionLeave (Player player, AiActor actor, bool immed=false) 
        {
          //bool posLeave = false;
          try                                 
          {
            
            if (player!=null && actor != null && player.Name() != null ) {

                    StbContinueMission cm1 = new StbContinueMission();
                    bool ret1;                    
                    ret1 = stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm1);
                    if (!ret1) cm1 = new StbContinueMission(); //on unsuccessful trygetvalue cm comes out as "not an object"

                    cm1.posLeftActor = actor;
                    cm1.placeLeaveLoc.x = actor.Pos().x;
                    cm1.placeLeaveLoc.y = actor.Pos().y;
                    //Console.WriteLine("PLL: PositionLeave cm1 PELx: " + cm1.placeEnterLoc.x.ToString("N0")
                    //+ " PELy: " + cm1.placeEnterLoc.y.ToString("N0") + " PLLx: " + cm1.placeLeaveLoc.x.ToString("N0")
                    //+ " PLLy: " + cm1.placeLeaveLoc.y.ToString("N0"));
                    //Console.WriteLine("Pll: "+cm1.ToString()); 

                    stbCmr_ContinueMissionInfo[player.Name()] = cm1;

                    if (immed) { //call from onBattleStoped or similar where we know this is the end of sortie and don't need to wait/da any calculations to figure it out.  Just save the end-of-sortie stats & get out of here immediately
                        StbCmr_SavePositionLeave_work(player, actor, cm1);
                        //posLeave = true;
                        //return posLeave;
                    }

                    Console.WriteLine("PPI,PPS pre: " + player.PlacePrimary().ToString() + " " + player.PlaceSecondary().ToString()); //When both 'persons' are out this looks like -1,-1.  The 2nd to last one out looks like 0,-1 or -1, 0 maybe. If switching out of a spot mid-flight to allow AI to take over one position it looks like -1, 1 or -1,2 0,1 0,2 or other things.  But both gone is always -1,-1


                    mission.Timeout (0.1,  () => { //this needs to be less than 0.2 bec that is what onPlaceLeave uses!!!!
                        StbContinueMission cm = new StbContinueMission();
                        bool ret;

                        Console.WriteLine("PPI,PPS post: " + player.PlacePrimary().ToString() + " " + player.PlaceSecondary().ToString()); //When both 'persons' are out this looks like -1,-1.  The 2nd to last one out looks like 0,-1 or -1, 0 maybe. If switching out of a spot mid-flight to allow AI to take over one position it looks like -1, 1 or -1,2 0,1 0,2 or other things.  But both gone is always -1,-1 or at least -1 0 or 2 -1 or the like

                        ret = stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm);
                        if (!ret) cm = new StbContinueMission(); //on unsuccessful trygetvalue cm comes out as "not an object"

                        //Console.WriteLine("Pll2: " + cm.ToString());
                        double dis_meters = Calcs.CalculatePointDistance(cm.placeLeaveLoc, cm.placeEnterLoc);

                        /* Console.WriteLine("PLL2: Place Change by " + dis_meters.ToString() + " meters. PELx: " + cm.placeEnterLoc.x.ToString("N0")
                            + " PELy: " + cm.placeEnterLoc.y.ToString("N0") + " PLLx: " + cm.placeLeaveLoc.x.ToString("N0")
                            + " PLLy: " + cm.placeLeaveLoc.y.ToString("N0"));
                        */

                        //This is used by onPositionLeave to determine whether the PositionLeave is the end of a flight or not
                        //PositionLeave waits 0.1 seconds, then checks whether there has been a recent Position Enter AND that the aircraft/actor is the same.  Difference in time can be up to 1 second, as we are using teimsince2016 which only
                        //has a resolution of 1 second, & we are waiting 0.1 sec.

                        //If so , PositionLeave aborts, as this is just a bomber pilot switching positions w/in the aircraft

                        //This needs more work as if you are flying & exit to map screen & immediately enter another a/c you can
                        //do it on zero seconds & this doesn't seem to catch the difference.

                        //It seems to work pretty well as far as not accidentally ENDING your mission when you are just switching seats in a bomber,
                        //But it won't always catch a REAL new flight in a completely different a/c.

                        //OK, here is the solution.  What you do is similar to the "destroy aircraft when human leaves it so AI don't fly it all crazy routine.  You wait 0.5 seconds or so after a player leaves the position, then check whether that (previous) a/c is AI controlled, ie no players in any position.  If it is AI controlled, then the player has left it.
                        //This could probably be improved by checking if THIS PLAYER is still in any of hte positions on the aircraft
                        //the person just left.  Better still, check for player.Name() vs this player & make sure that player isn't in the a/c any more
                        //The edge cases are where 2-3 people are flying bomber together.  If one of them leaves the a/c is that the end of the flight for that player? What if they return later, etc.

                        bool leftPlane = false;
                        bool parachuted = false;
                        if (actor != null && (actor as AiAircraft) != null && mission.Stb_isAiControlledPlane(actor as AiAircraft)) leftPlane = true;
                        if (player.PlacePrimary() == -1) parachuted = true; //OK, this doesn't actually work. Unfortunately.
                        cm.parachuted = parachuted;
                        
                        int currTime = Calcs.TimeSince2016_sec();
                        //We were using dis_meters < 10 which seems to work when actually switching positions in a bomber.  But . . . if you use ALT-F11 (ALT-F2, whatever) to
                        //got to "external view" then the the time & distance noted here is the time since the last position move within the plane - but the ALT-F2 move doesn't
                        //seem to do a pos leave before the pos enter (or something) so the time & distance could be MINUTES and 100s of kms, rather than basically 0 as 
                        //with a usual place move within the aircraft
                        //if (!leftPlane && currTime - cm.lastPositionEnter_sec <= 1 && (dis_meters<10)) { //Abort, Abort!  This is no end of flight, just a bomber pilot switching to a new position

                        //So we abandon that scheme & just use the process of seeing if any of the positions in the plane are occupied
                        if (!leftPlane) { //Abort, Abort!  This is no end of flight, just a bomber pilot switching to a new position

                        //Console.WriteLine("PLACE LEAVE: Just a pilot switching positions within an a/c " + (currTime - cm.lastPositionEnter_sec).ToString() + " " + dis_meters.ToString("0.0"));
                            cm.isPlaneLeave = false; //not a plane leave, just a position switch.
                            stbCmr_ContinueMissionInfo[player.Name()] = cm;
                            //posLeave = false;
                            //return posLeave; 
                            return; //just a position switch w/in an aircraft, not a "real" position leave
                        } else {
                             
                                  //Console.WriteLine("PLACE LEAVE: REAL sortie end--saving sortie / plane is AI controlled" + (currTime - cm.lastPositionEnter_sec).ToString() + " " + dis_meters.ToString("0.0"));
                        }

                        //Ok, so it's a real position leave, now save the stats, do the actual work etc.
                        StbCmr_SavePositionLeave_work(player, actor, cm);
                        //posLeave = true;
                        //return posLeave;                                          

                    });
            }
                return;
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); return; }
          
        } 
        
        //OK, this one isn't really necessary/somewhat redundant, but if
        //LeavePosition somehow doesn't trigger, this will catch any crash landings
        //(ie, off of an official airport) and flag them as not continue
        public void StbCmr_SaveOnCrashLanded (AiAircraft a) 
        {   
          try 
          {  
            if (! ( a is AiAircraft)) return;
            //Do this for every place/position in the a/c that took off
            for (int i = 0; i < a.Places(); i++)
            {
              //if (aiAircraft.Player(i) != null) return false;
              //OK, we go through each individual player in the a/c and decide
              //individually whether this is a mission continuation or not
              if ( a.Player(i) is Player && a.Player(i) != null && a.Player(i).Name() != null ) 
              {
                  
                  Player player = a.Player(i);
                  StbContinueMission cm = new StbContinueMission ();
                  bool ret; 
                  ret = stbCmr_ContinueMissionInfo.TryGetValue(player.Name(), out cm);
                  if (!ret) cm = new StbContinueMission (); //on unsuccessful trygetvalue cm comes out as "not an object"
                              
                  //p = player.Place() as AiAircraft;          
                  //p.Pos().x, p.Pos().y)
                  //Save the position where they left the a/c.  If the player is alive, the player may continue the mission, if they (can) take off from near the same location again.
                  AiActor actor=a as AiActor;
                  cm.placeLeaveLoc.x=-1000000;
                  cm.placeLeaveLoc.y=-1000000;
                  cm.alive=actor.IsAlive();
                  cm.mayContinue=false;
                                   
                
                  stbCmr_ContinueMissionInfo[player.Name()]= cm;
                  //Console.WriteLine( "CrashLand: " + player.Name() + " " + cm.ToString("F0"));
              }                          
            }
          }  
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        }
        
        public void StbCmr_SavePositionDied (string playerName) 
        {   
          try 
          {
            if ( playerName != null ) {
            
              StbContinueMission cm = new StbContinueMission ();
              bool ret; 
              ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
              if (!ret) cm = new StbContinueMission (); //on unsuccessful trygetvalue cm comes out as "not an object"
              
              //End the flight & save the flight time
              int endFlightTime_sec= Calcs.TimeSince2016_sec();
              int flightDuration_sec = endFlightTime_sec - cm.flightStartTime_sec;
              if (cm.flightStartTime_sec==0 || flightDuration_sec>24*60*60) flightDuration_sec = 0; //sanity check; often 'place leave' is the first time cm is initialized for a given pilot. 
              
              //Console.WriteLine( "Died: " + cm.flightStartTime_sec.ToString() + " " + endFlightTime_sec.ToString() + " " + flightDuration_sec.ToString() + " " + ((double)flightDuration_sec/60).ToString("F1")  );
                            
              cm.flightStartTime_sec= endFlightTime_sec; //reset the flight start time (though the 'real' flight start time is onTookOff; this is belt'n'suspenders)
              
              //record the time for the flight
              Mission.StbStatTask sst = new Mission.StbStatTask(Mission.StbStatCommands.Mission, playerName, new int[] { 792, flightDuration_sec  });
              if (flightDuration_sec > 0) this.mission.stb_StatRecorder.StbSr_EnqueueTask(sst);            
                          
              //p = player.Place() as AiAircraft;          
              //p.Pos().x, p.Pos().y)
              //When the player dies they cannot continue the mission any more
              //Setting alive=false is the key value here
              cm.placeLeaveLoc.x=-1000000;
              cm.placeLeaveLoc.y=-1000000;             
              cm.alive=false;              
              cm.mayContinue=false;
                               
            
              stbCmr_ContinueMissionInfo[playerName]= cm;
              
              //Console.WriteLine( "Died: " + playerName + " " + cm.ToString("F0"));
            }                          
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        }
        
        public bool StbCmr_OnTookOff (string playerName, AiActor actor=null) 
        {   
          try 
          {
                //if ((actor as AiAircraft).IsAirborne()) check if plane is airborn
                bool continueMission =true;                                                                
                StbContinueMission cm = new StbContinueMission ();
                cm.alive=false;
                cm.mayContinue=false;
                
                bool ret; 
                ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
                if (!ret) cm = new StbContinueMission (); //on unsuccessful trygetvalue cm comes out as "not an object"            
                //If the player died since last take off OR doesn't have 
                //'maycontinue' permission from the last placeEnter
                //then this is a New Mission & we increment the new mission counter
                //Otherwise, this is a continuation of a mission & we skip incrementing the mission counter            
                if ( ! cm.alive || !cm.mayContinue) {
                   //StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 779 });
                   //outer.stb_StatRecorder.StbSr_EnqueueTask(sst1);
                   continueMission=false;                 
                }


                //OK, we were resetin loc.x & loc.y to -1000000 on take off, just as a belt 'n suspenders thing, but actually having the real location is quite useful when we are trying to detect whether (eg bomber pilot) place enters are new sorties or just switching around within one single sortie.  So we will save the .pos when possible
                if (actor != null) {
                    cm.placeLeaveLoc.x = actor.Pos().x;
                    cm.placeLeaveLoc.y = actor.Pos().y;
                }
                else {
                   cm.placeLeaveLoc.x=-1000000;
                   cm.placeLeaveLoc.y=-1000000;
                }


                //This is the moment we reset alive & may continue & damagedsincetakeoff & damagedonlybyself & selfdamage this flight 
                //ie, successful takeoff is where the player actually
                //becomes 'alive' for stats & continueMission purposes

                cm.alive=true;
                cm.mayContinue=true;
                
                cm.flightStartTime_sec= Calcs.TimeSince2016_sec();                 
                
                //we don't reset these here, but in StbCmr_SavePositionEnter, because we want to capture any self-damage on taxi & takeoff 
                //cm.damagedSinceTakeoff=false; //Between these two we can figure out if the only
                //cm.damagedOnlyBySelf=true;  //damage upon landing or de-spawning is self-inflicted
                //cm.selfDamageThisFlight=0;
              
                stbCmr_ContinueMissionInfo[playerName]= cm;
                
                //Console.WriteLine( "TookOff: " + playerName                   + " Mission Continuing: "                   + continueMission.ToString() + " " + cm.ToString("F0"));                                            
                
                return continueMission;                                        
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); return false;}
        } //method

        //Keep track of whether we have just called a <rr for this player
        public void StbCmr_SetIsForcedPlaceMove(string playerName)
        {
            StbContinueMission cm = new StbContinueMission();
            bool ret;
            ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
            if (!ret) cm = new StbContinueMission(); //on unsuccessful trygetvalue cm comes out as "not an object"
            cm.isForcedPlaceMove = true;
            stbCmr_ContinueMissionInfo[playerName] = cm;

        }
        public void StbCmr_ClearIsForcedPlaceMove(string playerName)
        {
            StbContinueMission cm = new StbContinueMission();
            bool ret;
            ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
            if (!ret) cm = new StbContinueMission(); //on unsuccessful trygetvalue cm comes out as "not an object"
            cm.isForcedPlaceMove = false;
            stbCmr_ContinueMissionInfo[playerName] = cm;

        }
        public bool StbCmr_IsForcedPlaceMove(string playerName)
        {
            StbContinueMission cm = new StbContinueMission();
            bool ret;
            ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
            if (!ret) cm = new StbContinueMission(); //on unsuccessful trygetvalue cm comes out as "not an object"
            return cm.isForcedPlaceMove;
        }
        //onPlaceEnter is calling & needs to check if this placeEnter is a real sortie start (ie, an airspawn)
        //or is it just a place switch within (say) different positions in one bomber
        public bool StbCmr_IsItASortieStart(string playerName ) {
            StbContinueMission cm = new StbContinueMission();
            bool ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
            if (!ret) return true; // if the player hasn't been flying before, all is AOK, it really is a takeoff
            //Console.WriteLine(cm.ToString());
            return !cm.isInPlanePlaceChange;  //if the placeenter was not a place change, then it IS a sortie start!

        }
        public bool StbCmr_HasPlayerLeftPlane(string playerName)
        {
            StbContinueMission cm = new StbContinueMission();
            bool ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
            if (!ret) return false; // if the player hasn't been flying before, we're going to say the player couldn't possible have "left" a plane recently
            //Console.WriteLine(cm.ToString());
            return cm.isPlaneLeave;  

        }
        public bool StbCmr_IsPlayerDead(string playerName)
        {
            StbContinueMission cm = new StbContinueMission();
            bool ret = stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm);
            if (!ret) return false; // if the player hasn't been flying before, has just come in and thus is "alive".  "Not alive" = "Dead" mostly means, was just recently flying or whatever and crashed and died or was killed.  That is usually what we care about, not just that the player didn't exist at all a little while ago.
            //Console.WriteLine(cm.ToString());
            return !cm.alive;

        }
        public void StbCmr_UpdateSelfDamage (string playerName="", bool thisIsSelfDamage=false) 
        {   
          try 
          {
            bool prevDamage=false;
            if (playerName != null ) {
            
              StbContinueMission cm = new StbContinueMission ();              
              if ( stbCmr_ContinueMissionInfo.TryGetValue(playerName, out cm))
              { //Ok the person has newly spawned in and was previously flying, we check to see if 1. the person has died since last takeoff and 2. mayContinue is true (mayContinue is a signal from the previous event, the positionLeave in this case, that it was Ok to continue the mission @ that point). 3. Is taking off from the same place they recently landed.  If haven't died, mayContinue is true & taking off from same place, within 2000 of the place where they left their previous plane/position, they can continue the mission.                                                  
                  
                  prevDamage=cm.damagedSinceTakeoff;
                  cm.damagedSinceTakeoff=true; //Between these two we can figure out if the only damage upon landing or de-spawning is self-inflicted
                  cm.damagedOnlyBySelf=(cm.damagedOnlyBySelf && thisIsSelfDamage);  //if even one hit of non-self-damage is received, set this to false
                  cm.selfDamageThisFlight++; //we track how many times the self-damage happens  

              
              } else {//in case the player record doesn't exist yet; shouldn't happen too often
                  prevDamage=false;
                  cm = new StbContinueMission ();
                  cm.placeLeaveLoc.x=-1000000;
                  cm.placeLeaveLoc.y=-1000000;
                  cm.alive=true;
                  cm.mayContinue=false;
                  cm.damagedSinceTakeoff=true; //Between these two we can figure out if the only damage upon landing or de-spawning is self-inflicted
                  cm.damagedOnlyBySelf=(thisIsSelfDamage);  //if even one hit of non-self-damage is received, set this to false.  Initially we can set it to the value of this damage hit.
                  cm.selfDamageThisFlight=1;
                  cm.flightStartTime_sec= Calcs.TimeSince2016_sec();
                  
                  
              }                                                                                      
              stbCmr_ContinueMissionInfo[playerName]= cm;
              Mission.StbStatTask sst = new Mission.StbStatTask(Mission.StbStatCommands.Mission, playerName, new int[] { 790 });
              this.mission.stb_StatRecorder.StbSr_EnqueueTask(sst);
              
              //Console.WriteLine( "UpdSelfDamage: " + playerName + " " + cm.ToString("F0"));     
              if (thisIsSelfDamage && !prevDamage) this.mission.Stb_Message(null, this.mission.stb_StatRecorder.StbSr_RankFromName(playerName) + playerName + " just damaged own aircraft", new object[] { });  //Display this message TO EVERYONE but only if it is the plane's initial damage since takeoff (to avoid having it display numerous times in case of, for example, a self-crash)                       
            }                          
          }
          catch (Exception ex) { StbCmr_PrepareErrorMessage(ex); }
        }
        
        public bool StbCmr_IsSelfDamage (AiAircraft aircraft = null, AiDamageInitiator initiator = null) {                    
            string msg = "is damaged by ";
            bool thisIsSelfDamage=false;  //we start off by assuming this is true & then check each place in the a/c and set it to false if we find the damageinitiator in one of the places
            
            if (aircraft != null)                                
            {
              if (initiator != null && initiator.Player != null && initiator.Player.Name() != null) {                 
               
                msg += initiator.Player.Name();
                for (int i = 0; i < aircraft.Places(); i++) //check each place in the a/c etc
                {                  
                  if ( aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null ) {                                      
                    if (aircraft.Player(i).Name() == initiator.Player.Name()) thisIsSelfDamage=true;  
                    msg = aircraft.Player(i).Name() + " " + msg;                        
                  }    
                }
                            
              } else {
                // initiator is null or has no player, which usually means ai. So that means that the player has received damage from someone besides self. 
                msg += "nobody/AI  ";
                thisIsSelfDamage=false;                                              
              }
              
              if (!thisIsSelfDamage) { //this means that the damage initiator is not any of the pilots/operates of the actor.  So we can go ahead & say that this a/c has received some damage that is not self damage                 
                  msg += "nobody/AI  ";
              } 
                            
              for (int i = 0; i < aircraft.Places(); i++)
              {
                if ( aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null ) {                                                                                                      
                  StbCmr_UpdateSelfDamage (aircraft.Player(i).Name(), thisIsSelfDamage );  //record that fact that the a/c received damage and also whether or not it was self damage                        
                }    
              }

                                            
            } 
            //StbCmr_LogError(msg);            
            return thisIsSelfDamage;
            
            //TODO: We could do this for other actor types besides aircraft, too, though not sure why it would be needed   
        }  

        
        public void StbCmr_PrepareErrorMessage(Exception ex)
        {
            if (stbCmr_LogErrors)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(StbCmr_LogError),
                    //(object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
                    (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.ToString()));
            }
        }
        
        public void StbCmr_LogError(object data)
        {
            try
            {

                /*
                FileInfo fi = new FileInfo(stbCmr_ErrorLogPath);
                StreamWriter sw;
                if (fi.Exists) { sw = new StreamWriter(stbCmr_ErrorLogPath, true, System.Text.Encoding.UTF8); }
                else { sw = new StreamWriter(stbCmr_ErrorLogPath, false, System.Text.Encoding.UTF8); }
                sw.WriteLine((string)data);
                sw.Flush();
                sw.Close();
                */
                if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX11", null); //testing disk output for warps
                string date = DateTime.UtcNow.ToString("u");
                Task.Run(() => File.AppendAllText(stbCmr_ErrorLogPath, "\n" + date + " - " + (string)data));
                //File.AppendAllText(stbCmr_ErrorLogPath, "\n" + date + " - " + (string)data);
            }
            //catch (Exception ex) { Console.WriteLine(ex.Message); };
            catch (Exception ex) { Console.WriteLine(ex.ToString()); };
        }
    } //Continue Mission Recorder class

    public enum StbStatCommands : int { None = 0, Damage = 1, Dead = 2, CutLimb = 3, TaskCurrent = 4, Save = 5, PlayerKilled = 6, Mission = 7 };

    public struct StbStatTask
    {
        public StbStatCommands command;
        public string player;
        public AiActor actor;
        public int[] parameters;
        public StbStatTask(StbStatCommands cmd, string pname, int[] prms, AiActor actr = null)
        {
            command = cmd;
            player = pname;
            parameters = prms;
            actor = actr;
            
        }
    }
        
    //for use in cases where you know the aiaircraft but NOT the playername
    //if the a/c has actual players they will get the stats.  Otherwise, nothing
    //happens
    public void StbStatTaskAircraft (StbStatCommands cmd, AiAircraft a, int [] prms) {
         //Console.WriteLine ("Starting Aircraft Stat Update for " + a.Type());
         for (int i = 0; i < a.Places(); i++)
            {
              //if (aiAircraft.Player(i) != null) return false;
              if ( a.Player(i) is Player && a.Player(i) != null && a.Player(i).Name() != null ) {
                string playerName=a.Player(i).Name();                  
                //Console.WriteLine ("Aircraft Stat Update for " + playerName);                                 
                StbStatTask sst1 = new StbStatTask(cmd, playerName, prms, a as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst1);
              }    
            }
    }

    public StbStatRecorder stb_StatRecorder;

    #endregion

    #region stb Methods

    // Error Methods-----------------------------------------------------------------------------------------------------------

    public void Stb_PrepareErrorMessage(Exception ex)
    {
        if (stb_LogErrors)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(Stb_LogError),
                //(object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
                (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.ToString()));
        }
    }

    public void Stb_LogError(object data)
    {
        try
        {
            /*
            FileInfo fi = new FileInfo(stb_ErrorLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(stb_ErrorLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(stb_ErrorLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
            */
            //TODO: Should just AppendAllText(    string path,    string contents ) instead of all the above
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX8", null); //testing disk output for warps
            string date = DateTime.UtcNow.ToString("u");
            Task.Run(() => File.AppendAllText(stb_ErrorLogPath, "\n" + date + " - " + (string)data));
            //File.AppendAllText(stb_ErrorLogPath, "\n" + date + " - " + (string)data);
        }
        //catch (Exception ex) { Console.WriteLine(ex.Message); };
        catch (Exception ex) { Console.WriteLine(ex.ToString()); };
    }

    // Balance Methods---------------------------------------------------------------------------------------------------------

    private void Stb_BalanceUpdate(Player p, AiActor actor, int pIndex, bool entered)
    {
        try
        {
            AiAircraft aircraft = actor as AiAircraft;
            if (aircraft == null) { return; }
            if (aircraft.Type() == AircraftType.Fighter || aircraft.Type() == AircraftType.HeavyFighter)
            {
                if (aircraft.CrewFunctionPlace(pIndex) == CrewFunction.Pilot)
                {
                    if (p.Army() == 1) { stb_RedFighters += entered ? 1 : -1; return; }
                    if (p.Army() == 2) { stb_BlueFighters += entered ? 1 : -1; return; }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private bool Stb_BalanceCheckForBan(Player p, AiActor actor, int placeIndex)
    {
        try
        {
            stb_Ratio = (stb_RedFighters > stb_BlueFighters) ?
                ((double)stb_RedFighters / (double)stb_BlueFighters) : ((double)stb_BlueFighters / (double)stb_RedFighters);
            stb_Delta = (stb_RedFighters > stb_BlueFighters) ?
                (stb_RedFighters - stb_BlueFighters) : (stb_BlueFighters - stb_RedFighters);
            if (stb_BalanceFightersByRatio)
            {
                if (stb_Ratio > stb_DesiredRatio &&
                    (stb_RedFighters + stb_BlueFighters) > stb_MinCountForBalance)
                {
                    Stb_BalanceMessageForBan(p, actor, placeIndex); return true;
                }
            }
            else
            {
                if (stb_Delta > stb_DesiredDelta &&
                    (stb_RedFighters + stb_BlueFighters) > stb_MinCountForBalance)
                {
                    Stb_BalanceMessageForBan(p, actor, placeIndex); return true;
                }
            }
            return false;
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); return false; }
    }

    private void Stb_BalanceMessageForBan(Player p, AiActor actor, int placeIndex)
    {
        try
        {
            GamePlay.gpLogServerBegin(new Player[] { p }, stb_BalanceMsgRus);
            GamePlay.gpLogServerEnd();
            GamePlay.gpLogServerBegin(new Player[] { p }, stb_BalanceMsgEng);
            GamePlay.gpLogServerEnd();
            GamePlay.gpHUDLogCenter(new Player[] { p }, stb_BalanceMsgHud);
            Stb_BalanceDisableForBan(actor);
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_BalanceDisableForBan(AiActor actor)
    {
        try
        {
            AiAircraft aircraft = actor as AiAircraft;
            if (aircraft != null)
            {
                Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsElevatorDisabled);
                Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsAileronsDisabled);
                Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsRudderDisabled);
                Stb_DamagePlane(aircraft, NamedDamageTypes.Eng0TotalSeizure);
                AiAirGroup aag = aircraft.AirGroup();
                if (aag != null)
                {
                    if (aag.aircraftEnginesNum() == 2) { Stb_DamagePlane(aircraft, NamedDamageTypes.Eng1TotalSeizure); }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    // StartUp Methods---------------------------------------------------------------------------------------------------------

    private void Stb_LoadAntiAirDraftToMemory()
    {
        string[,] shipsRed = new string[6, 2];
        string[,] shipsBlue = new string[6, 2];

        shipsRed[0, 0] = "13630.00 31515.00 38.40  0 2 5.56";
        shipsRed[0, 1] = "13610.00 31555.00 38.40";
        shipsRed[1, 0] = "14210.00 31680.00 38.40  0 2 5.56";
        shipsRed[1, 1] = "14255.00 31690.00 38.40";
        shipsRed[2, 0] = "13100.00 31150.00 38.40  0 2 5.56";
        shipsRed[2, 1] = "13065.00 31130.00 38.40";
        shipsRed[3, 0] = "18600.00 31645.00 38.40  0 2 5.56";
        shipsRed[3, 1] = "18615.00 31685.00 38.40";
        shipsRed[4, 0] = "18035.00 31795.00 38.40  0 2 5.56";
        shipsRed[4, 1] = "17970.00 31800.00 38.40";
        shipsRed[5, 0] = "19030.00 31310.00 38.40  0 2 5.56";
        shipsRed[5, 1] = "19060.00 31285.00 38.40";

        shipsBlue[0, 0] = "11690.00 9905.00 38.40  0 2 5.56";
        shipsBlue[0, 1] = "11620.00 9905.00 38.40";
        shipsBlue[1, 0] = "13025.00 10260.00 38.40  0 2 5.56";
        shipsBlue[1, 1] = "13075.00 10275.00 38.40";
        shipsBlue[2, 0] = "12450.00 10030.00 38.40  0 2 5.56";
        shipsBlue[2, 1] = "12455.00 9990.00 38.40";
        shipsBlue[3, 0] = "18260.00 9100.00 38.40  0 2 5.56";
        shipsBlue[3, 1] = "18240.00 9070.00 38.40";
        shipsBlue[4, 0] = "17930.00 9345.00 38.40  0 2 5.56";
        shipsBlue[4, 1] = "17895.00 9370.00 38.40";
        shipsBlue[5, 0] = "18675.00 8940.00 38.40  0 2 5.56";
        shipsBlue[5, 1] = "18695.00 8930.00 38.40";

        string[] gunsRed = new string[28];
        string[] gunsBlue = new string[28];

        gunsBlue[0] = "Artillery.4_cm_Flak_28 de 18030 9660 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[1] = "Artillery.4_cm_Flak_28 de 18370 9650 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[2] = "Artillery.4_cm_Flak_28 de 18710 9640 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[3] = "Artillery.4_cm_Flak_28 de 18990 9630 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[4] = "Artillery.4_cm_Flak_28 de 19400 9620 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[5] = "Artillery.4_cm_Flak_28 de 11630 10310 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[6] = "Artillery.4_cm_Flak_28 de 11980 10400 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[7] = "Artillery.4_cm_Flak_28 de 12300 10480 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[8] = "Artillery.4_cm_Flak_28 de 12650 10570 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[9] = "Artillery.4_cm_Flak_28 de 12970 10650 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[10] = "Artillery.Flak37 de 18710 10110 240 /timeout 0/radius_hide 3000 ";
        gunsBlue[11] = "Artillery.Flak37 de 18775 10110 -90 /timeout 0/radius_hide 3000 ";
        gunsBlue[12] = "Artillery.Flak37 de 18840 10110 -45 /timeout 0/radius_hide 3000 ";
        gunsBlue[13] = "Artillery.Flak37 de 18710 10190 135 /timeout 0/radius_hide 3000 ";
        gunsBlue[14] = "Artillery.Flak37 de 18775 10190 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[15] = "Artillery.Flak37 de 18840 10190 45 /timeout 0/radius_hide 3000 ";
        gunsBlue[16] = "Artillery.Flak37 de 12410 10710 225 /timeout 0/radius_hide 3000 ";
        gunsBlue[17] = "Artillery.Flak37 de 12475 10710 -90 /timeout 0/radius_hide 3000 ";
        gunsBlue[18] = "Artillery.Flak37 de 12540 10710 -45 /timeout 0/radius_hide 3000 ";
        gunsBlue[19] = "Artillery.Flak37 de 12410 10790 135 /timeout 0/radius_hide 3000 ";
        gunsBlue[20] = "Artillery.Flak37 de 12475 10790 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[21] = "Artillery.Flak37 de 12540 10790 45 /timeout 0/radius_hide 3000 ";
        gunsBlue[22] = "Artillery.RRH_GER1 de 18710 10150 180 /timeout 0/radius_hide 3000 ";
        gunsBlue[23] = "Artillery.RRH_GER1 de 18775 10150 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[24] = "Artillery.RRH_GER1 de 18840 10150 0 /timeout 0/radius_hide 3000 ";
        gunsBlue[25] = "Artillery.RRH_GER1 de 12410 10750 180 /timeout 0/radius_hide 3000 ";
        gunsBlue[26] = "Artillery.RRH_GER1 de 12475 10750 90 /timeout 0/radius_hide 3000 ";
        gunsBlue[27] = "Artillery.RRH_GER1 de 12540 10750 0 /timeout 0/radius_hide 3000 ";
        gunsRed[0] = "Artillery.Bofors gb 13280 30560 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[1] = "Artillery.Bofors gb 18770 30530 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[2] = "Artillery.Bofors gb 18430 30620 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[3] = "Artillery.Bofors gb 13610 30730 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[4] = "Artillery.Bofors gb 18110 30700 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[5] = "Artillery.Bofors gb 17770 30790 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[6] = "Artillery.Bofors gb 13890 30880 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[7] = "Artillery.Bofors gb 17440 30880 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[8] = "Artillery.Bofors gb 14190 31040 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[9] = "Artillery.Bofors gb 14500 31210 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[10] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13710 30610 -135 /timeout 0/radius_hide 3000 ";
        gunsRed[11] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13775 30610 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[12] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13840 30610 -45 /timeout 0/radius_hide 3000 ";
        gunsRed[13] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17710 30610 -135 /timeout 0/radius_hide 3000 ";
        gunsRed[14] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17775 30610 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[15] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13710 30690 135 /timeout 0/radius_hide 3000 ";
        gunsRed[16] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17840 30610 -45 /timeout 0/radius_hide 3000 ";
        gunsRed[17] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13775 30690 90 /timeout 0/radius_hide 3000 ";
        gunsRed[18] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 13840 30690 45 /timeout 0/radius_hide 3000 ";
        gunsRed[19] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17710 30690 135 /timeout 0/radius_hide 3000 ";
        gunsRed[20] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17775 30690 90 /timeout 0/radius_hide 3000 ";
        gunsRed[21] = "Artillery.3_inch_20_CWT_QF_Mk_I gb 17840 30690 45 /timeout 0/radius_hide 3000 ";
        gunsRed[22] = "Artillery.SoundLocator_MkIIIV_UK1 gb 13710 30650 180 /timeout 0/radius_hide 3000 ";
        gunsRed[23] = "Artillery.SoundLocator_MkIIIV_UK1 gb 13775 30650 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[24] = "Artillery.SoundLocator_MkIIIV_UK1 gb 13840 30650 0 /timeout 0/radius_hide 3000 ";
        gunsRed[25] = "Artillery.SoundLocator_MkIIIV_UK1 gb 17720 30650 180 /timeout 0/radius_hide 3000 ";
        gunsRed[26] = "Artillery.SoundLocator_MkIIIV_UK1 gb 17775 30650 -90 /timeout 0/radius_hide 3000 ";
        gunsRed[27] = "Artillery.SoundLocator_MkIIIV_UK1 gb 17840 30650 0 /timeout 0/radius_hide 3000 ";

        ISectionFile f = GamePlay.gpCreateSectionFile();
        string s = "";
        string k = "";
        string v = "";

        s = "Chiefs";
        v = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        for (int i = 0; i < 6; i++)
        {
            k = "baseDefenseShip_R_" + i.ToString("00");
            f.add(s, k, v);
        }
        v = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        for (int i = 0; i < 6; i++)
        {
            k = "baseDefenseShip_B_" + i.ToString("00");
            f.add(s, k, v);
        }

        v = "";
        for (int i = 0; i < 6; i++)
        {
            s = "baseDefenseShip_R_" + i.ToString("00") + "_Road";
            k = shipsRed[i, 0];
            f.add(s, k, v);
            k = shipsRed[i, 1];
            f.add(s, k, v);
        }
        for (int i = 0; i < 6; i++)
        {
            s = "baseDefenseShip_B_" + i.ToString("00") + "_Road";
            k = shipsBlue[i, 0];
            f.add(s, k, v);
            k = shipsBlue[i, 1];
            f.add(s, k, v);
        }

        s = "Stationary";
        for (int i = 0; i < 28; i++)
        {
            k = "baseDefenseGun_R_" + i.ToString("00");
            v = gunsRed[i];
            f.add(s, k, v);
        }
        for (int i = 0; i < 28; i++)
        {
            k = "baseDefenseGun_B_" + i.ToString("00");
            v = gunsBlue[i];
            f.add(s, k, v);
        }

        stb_BaseAntiAirDefenses = f;
    }

    private void Stb_LoadFrontline1DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontShip_R1";
        value = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        key = "FrontShip_B1";
        value = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        value = "";

        sect = "FrontShip_R1_Road";
        key = "7500.00 27500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "7500.00 14400.00 38.4"; f.add(sect, key, value);

        sect = "FrontShip_B1_Road";
        key = "7500.00 14400.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "7500.00 27500.00 38.40"; f.add(sect, key, value);

        stb_Frontline1 = f;
    }

    private void Stb_LoadFrontline2DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontShip_R2";
        value = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        key = "FrontShip_B2";
        value = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        value = "";

        sect = "FrontShip_R2_Road";
        key = "13600.00 27500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "13427.00 18871.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "13171.00 17635.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "13600.00 14400.00 38.4"; f.add(sect, key, value);

        sect = "FrontShip_B2_Road";
        key = "13600.00 14400.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "13171.00 17635.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "13427.00 18871.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "13600.00 27500.00 38.40"; f.add(sect, key, value);

        stb_Frontline2 = f;
    }

    private void Stb_LoadFrontline3DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontShip_R3";
        value = "Ship.Minensuchboote gb /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        key = "FrontShip_B3";
        value = "Ship.Minensuchboote de /sleep 0/skill 0/slowfire 1";
        f.add(sect, key, value);

        value = "";

        sect = "FrontShip_R3_Road";
        key = "15900.00 27500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "15700.00 23500.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "17500.00 20600.00 38.4  0 2 5.56"; f.add(sect, key, value);
        key = "17100.00 14400.00 38.4"; f.add(sect, key, value);

        sect = "FrontShip_B3_Road";
        key = "17100.00 14400.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "17500.00 20600.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "15700.00 23500.00 38.40  0 2 5.56"; f.add(sect, key, value);
        key = "15900.00 27500.00 38.40"; f.add(sect, key, value);

        stb_Frontline3 = f;
    }

    private void Stb_LoadFrontline4DraftToMemory()
    {
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect;
        string key;
        string value;

        sect = "Chiefs";
        key = "FrontArmor_R1";
        value = "Armor.Cruiser_Mk_IVA gb /num_units 6";
        f.add(sect, key, value);

        key = "FrontArmor_B1";
        value = "Armor.Pz_IIC de /num_units 6";
        f.add(sect, key, value);

        value = "";

        sect = "FrontArmor_R1_Road";
        key = "20700.00 25800.00 38.4  0 2 7.14"; f.add(sect, key, value);
        key = "20900.00 25000.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "20609.00 23948.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "20849.00 23059.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21284.00 22438.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21591.00 22044.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21700.00 21400.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21638.00 20728.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21806.00 20209.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22187.00 19677.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22400.00 19100.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22200.00 18400.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22300.00 17700.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "22211.00 16941.00 38.4  0 2 3.57"; f.add(sect, key, value);
        key = "21854.00 16291.00 38.4"; f.add(sect, key, value);

        sect = "FrontArmor_B1_Road";
        key = "21854.00 16291.00 38.40  0 2 7.14"; f.add(sect, key, value);
        key = "22211.00 16941.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22300.00 17700.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22200.00 18400.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22400.00 19100.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "22187.00 19677.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21806.00 20209.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21638.00 20728.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21700.00 21400.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21591.00 22044.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "21284.00 22438.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20849.00 23059.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20609.00 23948.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20900.00 25000.00 38.40  0 2 3.57"; f.add(sect, key, value);
        key = "20700.00 25800.00 38.40"; f.add(sect, key, value);

        stb_Frontline4 = f;
    }

    //use loc.x = loc.y = loc.z 0 for default location
    //returns the name of the newly created a/c, which actually won't be created until the isect file is loaded, so wait 1 sec. or so before using.
    private string Stb_LoadSubAircraft(Point3d loc, string type = "SpitfireMkIa_100oct", string callsign = "26", string hullNumber="3", string serialNumber="001", string regiment="gb02", string fuelStr = "", string weapons= "", double velocity=0, string fighterbomber="", string skin_filename="", string delay_sec="" )
    {
        /*  //sample .mis file with parked a/c
         *  [AirGroups]
            BoB_RAF_F_141Sqn_Early.01
            [BoB_RAF_F_141Sqn_Early.01]
            Flight0  1
            Class Aircraft.SpitfireMkIa_100oct
            Formation VIC3
            CallSign 26
            Fuel 100
            Weapons 1
            SetOnPark 1
            Skill 0.3 0.3 0.3 0.3 0.3 0.3 0.3 0.3
            [BoB_RAF_F_141Sqn_Early.01_Way]
            TAKEOFF 76923.96 179922.36 0 0 

          */
        //default spawn location is Bembridge, landed, 0 mph & on the ground
        string locx = "76923.96";
        //string locy = "179922.36"; //real Bembridge location
        string locy = "178322.36"; //1600 meters off Bembridge
        string locz = "0";
        string vel = "0";

        int hullNumber_int = 1;
        try
        { 
            hullNumber_int = Convert.ToInt32(hullNumber);
        }catch {
            hullNumber_int = 1;
        }
        //if (hullNumber_int < 1) hullNumber_int = 1; //sanity check; not sure on exact highest allowed number here  //OK, CloD seems to allow any neg or positive integer so we're leaving it at that

        //Ok this is wierd. But if aiaircraft group spawn-in point is near or in the middle of the airport, then CLOD seems to use the
        //built-in spawn points for that airport, regardless of what you have put in place.
        //But if the given spawn-in point is like a thousand or a couple thousand meters away, then it finds the nearest airport AND uses the airdrome points that you have created in FMB
        //So, we're going to try it.

        //TODO: What we shoudl really do, if this idea works, is to #1. find the nearest airport,  #2, move our loc to 2000 meters or whatever away from it. #3. Make sure that target airport is still our nearest airport

        if (loc.x != 0 && loc.y != 0 && loc.z != 0 ) {
            locx = (loc.x - 1000).ToString("F2"); //1000 m off the actual location
            locy = (loc.y - 1600).ToString("F2"); //1600 m off the actual location
            if (velocity>0) {
             locz = loc.z.ToString("F2");
             vel = velocity.ToString("F2");
            } else {
                locz = "0";
                vel = "0";
            }
        }
                
        string rnumb= ".01";
        string regiment_isec = regiment + rnumb;

        ISectionFile f = GamePlay.gpCreateSectionFile();
        string s = "";
        string k = "";
        string v = "";

        s = "AirGroups";
        k = regiment_isec; v = ""; f.add(s, k, v);        
        s = regiment_isec;
        k = "Flight0"; v = hullNumber_int.ToString(); f.add(s, k, v);
        k = "Class"; v = "Aircraft." + type; f.add(s, k, v);
        k = "Formation"; v = "VIC3"; f.add(s, k, v);
        k = "CallSign"; v = callsign; f.add(s, k, v);
        //k = "Fuel"; v = fuel.ToString(); f.add(s, k, v);
        //k = "Weapons"; v = weapons; f.add(s, k, v);

        f= Stb_AddLoadoutForPlane(f, s, type, fighterbomber, weapons, delay_sec, fuelStr);


        /* if (type.Contains("Spitfire") || type.Contains("Hurricane"))  //We'll have to figure out what to do for DE aircraft, blennies, etc . . . 
        {
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 11 11 9 11");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 9 11 11 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 11 9 11 11 11 11 10");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 11 11 11");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 11 11 9");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 11 11 9 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 11 11 11 9");
        } */
        f.add(s, "Serial0", serialNumber);
        if (skin_filename.Length>0) f.add(s, "Skin0", skin_filename);
        else f.add(s, "Skin0", "default.jpg");  //Not sure if this file needs to be in the relevant a/c folder Documents\1C SoftClub\il-2 sturmovik cliffs of dover - MOD\PaintSchemes\Skins\MYAIRCRAFT of the user, the server, or what.  Also don't know how to find out which skin the player is currently using
        //f.add(s, "Flight0", hullNumber);  
        k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
        if (velocity <= 0) { 
            k = "SetOnPark"; v = "1"; f.add(s, k, v);
            k = "Idle"; v = "1"; f.add(s, k, v);
        }

        s = regiment_isec + "_Way";
        if (velocity <= 0) k = "TAKEOFF";
        else k = "NORMFLY";
        v = locx + " " + locy + " " + locz + " " + vel; f.add(s, k, v);

        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + stb_FullPath + "aircraftSpawn-ISectionFile.txt", new object[] { }); //testing
        //f.save(stb_FullPath + "aircraftSpawn-ISectionFile.txt"); //testing

        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX13", null); //testing disk output for warps
        //load it in
        GamePlay.gpPostMissionLoad(f);

        return (stb_lastMissionLoaded + 1).ToString() + ":" + regiment + ".000";  //There is a better way to do this (get the actual name via onmission loaded) but this might work for now



    }

    private ISectionFile Stb_AddLoadoutForPlane(ISectionFile f, string s, string type, string fighterbomber = "", string weapons = "", string delay_sec="", string fuelStr="")
    {
        string k = "";
        string v = "";

        if (weapons == null) weapons = "";
        if (delay_sec == null || delay_sec == "")
        {
            delay_sec = "1"; //1 sec should work for Blenheim but maybe not for some/all DE aircraft.  So 0.08 sec delay for the DE a/c
            if (type.Contains("Bf-109") || type.Contains("He-111") || type.Contains("110C") || type.Contains("BR-20") || type.Contains("Ju-8")) delay_sec = "0.08";
        }

        int fuel = 100;
        try
        {
            fuel = Convert.ToInt32(fuelStr);

            if (fuel < 0) fuel = 100;  //negative is not sensible, we're assuming it is just nonsense
            if (fuel > 100) fuel = 100; // We're allowing fuel=0 even though I can't imagine how this is useful to anyone
        }
        catch (Exception ex) //any problem with int32 conversion
        {
            fuel = 0;
            //Console.WriteLine("Spawn: Using default value for fuel load, 100% or 30%");
        }

        //k = "Weapons"; v = weapons; f.add(s, k, v);
        //"Belt" must be a CAPITOL B here in the .mis file, though it is spelled "belt" in the user.ini file . . . 
        if (type.Contains("Spitfire") || type.Contains("Hurricane"))  //We'll have to figure out what to do for DE aircraft, blennies, etc . . . 
        {
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII MainBelt 11 11 9 11 Residual 50 ResidueBelt 11 10 11 10 11 10 11 10");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 9 11 11 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 11 9 11 11 11 11 10 Residual 50 ResidueBelt 11 11 11 11 9");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII MainBelt 9 11 11 11");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 11 11 11 11 9");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII MainBelt 11 11 9");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 11 11 9 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 11 11 11 9 Residual 50 ResidueBelt 11 10 11 10 11 10 11 10");
            if (type.Contains("HurricaneMkI_FB"))
            {
                if (weapons.Length == 0) weapons = "1 2"; //default
                if (fighterbomber == "f") weapons = "1 0";
                f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 3 0 " + delay_sec);
            }
            else
            {
                if (weapons.Length == 0) weapons = "1"; //default
                if (fighterbomber == "b") weapons = "1 2";
            }
            
            if (fuel == 0) fuel = 100;

        }
        else if (type==("BlenheimMkIV")) 
        {
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 10 12 9 10 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 0 30 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 5 0 2"; //default (updated for 4.53
                if (fighterbomber=="f") weapons = "1 1 0 0 0"; 
            }
            if (fuel == 0) fuel = 35;
        }
        else if (type == ("BlenheimMkIV_Late"))
        {
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 0 30 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 2 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 0 0 0";
            }
            if (fuel == 0) fuel = 35;
        }
        else if (type == ("BlenheimMkIVF") || type == "BlenheimMkIVNF" ) 
        {
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.VickersK MainBelt 12 9 9 11 11 10 2 2");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.Bomb_GP_40lb_MkIII 0 30 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_250lb_MkIV 3 0 " + delay_sec);
            f.add(s, "Detonator", "Bomb.Bomb_GP_500lb_MkIV 3 0 " + delay_sec);
            if (weapons.Length == 0)
            {
                    weapons = "1 1 1 0 0 2"; //default
                    if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }

            if (fuel == 0) fuel = 30;
        }
        else if (type == ("BlenheimMkIVF_Late") || type == "BlenheimMkIVNF_Late" )  //still needs update 4.5 
        {
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun01 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 Gun.Browning303MkII MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun03 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Browning303MkII_Fuselage MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII-B1-TwinTurret MainBelt 9 11 11 11 10 11 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }
            if (fuel == 0) fuel = 30;
        }
        else if (type == ("BeaufighterMkIF") || type == "BeaufighterMkINF")  //could add residuals
        {

            f.add(s, "Belt", "_Gun01 Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun03 Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun06 Gun.Browning303MkII MainBelt 10 11 9 11 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun09 Gun.Browning303MkII MainBelt 10 11 9 11 9 0 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun02 Gun.Hispano_Mk_I MainBelt 0 1 0 1 0 1 0 1");
            f.add(s, "Belt", "_Gun08 Gun.Browning303MkII MainBelt 9 10 11 9 11 9 11 2 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun07 Gun.Browning303MkII MainBelt 9 10 11 9 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun05 Gun.Browning303MkII MainBelt 9 11 10 9 2 11 9 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun04 Gun.Browning303MkII MainBelt 10 9 9 11 9 9 11 9 9 11 Residual 50 ResidueBelt 10 9 10 11");
            f.add(s, "Belt", "_Gun00 Gun.Hispano_Mk_I MainBelt 1 1 0 1 0 1 0 1 0 0");            
            if (weapons.Length == 0)
            {
                weapons = "1 1"; //default                
            }
            if (fuel == 0) fuel = 30;
        }
        else if (type == "G50")
        {
            f.add(s, "Belt", "_Gun00 Gun.Breda-SAFAT-12,7mm MainBelt 7 3 0 4 6 3 0 1 4 6 Residual 50 ResidueBelt 3 4 7 4 6 7 0 3 7");
            f.add(s, "Belt", "_Gun01 Gun.Breda-SAFAT-12,7mm MainBelt 3 0 1 4 7 0 1 3 4 6 Residual 50 ResidueBelt 0 3 7 3 4 7 4 6 7");
            
            if (weapons.Length == 0) weapons = "1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type == "Bf-109E-1B") //we do E-1B first so that then we can cover all remaining E-1 types with type.Contains.  Similarly for E-3, E-4
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17_Wing MainBelt 4 4 4 0 0 0 2 5 5 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 5 5 5 1 0 0 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 0 0 0 4 4 4 1 5 5 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17_Wing MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type == "Bf-109E-3B") //we do E-3B first so that then we can cover all remaining E-3 types with type.Contains.  Similarly for E-3, E-4
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 4 3 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 4 1 4");

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 2"; //default
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-4B"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 5 1 5");

            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 3"; //default
                if (fighterbomber == "f") weapons = "1 1 0";
            }
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-1")) 
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17_Wing MainBelt 4 4 4 0 0 0 2 5 5 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 5 5 5 1 0 0 0 4 4 4");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 0 0 0 4 4 4 1 5 5 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17_Wing MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-3"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 4 3 4");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 4 1 4");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        else if (type.Contains("Bf-109E-4"))  //also covers E-4N E-4N-Derated etc
        {
            f.add(s, "Belt", "_Gun02 Gun.MGFF_Wing MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MGFF_Wing MainBelt 5 1 5");

            if (weapons.Length == 0) weapons = "1 1"; //default
            if (fuel == 0) fuel = 100;

        }
        
        else if (type.Contains("He-111H")) //covers both H-2 & P-2 with just one line different
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2");       
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun03 Gun.MG15 MainBelt 4 4 4 2 0 0 0 2 5 5 5 2 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 0 2"); //Only the h-2 has Gun03; the P-2 lacks it & throws an error if it is included
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");            
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);            

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("He-111P")) //covers both H-2 & P-2 with just one line different
        {
            f.add(s, "Belt", "_Gun04 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");            
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 4 4 2 4 0 0 0 2 5 5 2 5 Residual 50 ResidueBelt 4 2 4 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 2 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 1 1 0";
            }
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("110C-2")) 
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 4 1 4");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 4 4 1");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("110C-4"))
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 5 1 5");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");

            if (weapons.Length == 0) weapons = "1 1 1"; //default
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("110C-7")) //also covers 110C-7-late
        {
            f.add(s, "Belt", "_Gun02 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun06 Gun.MG15 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun05 Gun.MGFF MainBelt 5 1 5");
            f.add(s, "Belt", "_Gun04 Gun.MGFF MainBelt 5 3 5");
            f.add(s, "Belt", "_Gun03 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun00 Gun.MG17 MainBelt 2 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17 MainBelt 5 5 5 2 0 0 0 4 4 4 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_A 0 -1 " + delay_sec);
            if (weapons.Length == 0)
            {
                weapons = "1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0";
            }
            
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("BR-20"))
        {
            f.add(s, "Belt", "_Gun01 Gun.Breda-SAFAT-12,7mm_Turret MainBelt 8 3 5 1 6 7 3 5 1 8");
            f.add(s, "Belt", "_Gun00 Gun.Breda-SAFAT-7,7mm MainBelt 4 3 1 2 3 4");
            f.add(s, "Belt", "_Gun02 Gun.Breda-SAFAT-7,7mm MainBelt 1 2 3 4 1 2 3 4");

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0";
            }
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("Ju-87")) 
        {
            f.add(s, "Belt", "_Gun00 Gun.MG17_Wing MainBelt 4 4 4 0 0 0 2 5 5 5 Residual 50 ResidueBelt 4 4 4 2 5 5 5 2 0 0 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG17_Wing MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 1 0 4 4 4 5 5 5 0 0 Residual 50 ResidueBelt 0 1 2 4 4 4 2 5 5 5 2 0 0 2");
            
            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_J 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J_DivePreferred 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250_JB 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_E 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type2_J 2 -1 " + delay_sec);

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 1"; //default
                if (fighterbomber == "f") weapons = "1 1 0 0";
            }
            if (fuel == 0) fuel = 30;

        }
        else if (type.Contains("Ju-88"))
        {
            f.add(s, "Belt", "_Gun00 Gun.MG15 MainBelt 4 4 2 4 0 0 2 0 2 5 5 2 5 Residual 50 ResidueBelt 4 4 2 4 2 5 5 2 5 2 0 0 2 0 2");
            f.add(s, "Belt", "_Gun01 Gun.MG15 MainBelt 1 0 2 4 4 2 4 5 5 2 5 0 2 0 Residual 50 ResidueBelt 0 1 2 4 4 2 4 2 5 5 2 5 2 0 2 0 2");
            f.add(s, "Belt", "_Gun02 Gun.MG15 MainBelt 1 2 0 4 2 4 4 5 2 5 5 2 0 0 Residual 50 ResidueBelt 0 1 2 4 4 2 4 2 5 5 2 5 2 0 2 0 2");

            f.add(s, "Detonator", "Bomb.SC-500_GradeIII_K 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-250 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-250_Type1_J 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SD-500_A 0 -1 " + delay_sec);
            f.add(s, "Detonator", "Bomb.SC-50_GradeII_J 1 -1 " + delay_sec);            

            if (weapons.Length == 0)
            {
                weapons = "1 1 1 2 2 4"; //default
                if (fighterbomber == "f") weapons = "1 1 1 0 0 0";
            }
            if (fuel == 0) fuel = 30;

        }
        k = "Weapons"; v = weapons; f.add(s, k, v);
        k = "Fuel"; v = fuel.ToString(); f.add(s, k, v);

        return f;
    }


   private void Stb_LoadBombers1DraftToMemory()
   {
       ISectionFile f = GamePlay.gpCreateSectionFile();
       string s = "";
       string k = "";
       string v = "";

       s = "AirGroups";
       k = "gb02.03"; v = ""; f.add(s, k, v);
       k = "g02.03"; v = ""; f.add(s, k, v);
       s = "gb02.03";
       k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
       k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
       k = "Class"; v = "Aircraft.WellingtonMkIc"; f.add(s, k, v);
       k = "Formation"; v = "VIC3"; f.add(s, k, v);
       k = "CallSign"; v = "26"; f.add(s, k, v);
       k = "Fuel"; v = "66"; f.add(s, k, v);
       k = "Weapons"; v = "1 1 3"; f.add(s, k, v);
       k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       s = "gb02.03_Way";
       k = "NORMFLY"; v = "19500.00 39000.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "19500.00 31000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "17500.00 24000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "17000.00 18000.00 2000.00 250.00"; f.add(s, k, v);
       k = "GATTACK_POINT"; v = "15660.00 10800.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "15660.00 9800.00 2200.00 333.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "19500.00 39000.00 3000.00 366.00"; f.add(s, k, v);
       s = "g02.03";
       k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
       k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
       k = "Class"; v = "Aircraft.He-111H-2"; f.add(s, k, v);
       k = "Formation"; v = "VIC3"; f.add(s, k, v);
       k = "CallSign"; v = "30"; f.add(s, k, v);
       k = "Fuel"; v = "66"; f.add(s, k, v);
       k = "Weapons"; v = "1 1 1 1 1 1 2"; f.add(s, k, v);
       k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       s = "g02.03_Way";
       k = "NORMFLY"; v = "10500.00 2000.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "10500.00 10000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "12500.00 18000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "13000.00 24000.00 2000.00 250.00"; f.add(s, k, v);
       k = "GATTACK_POINT"; v = "16300.00 31350.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "16300.00 32350.00 2200.00 333.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "10500.00 2000.00 3000.00 366.00"; f.add(s, k, v);
       s = "Stationary";
       k = "BmbArt_B0"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_B1"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_B2"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_B3"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R0"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R1"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R2"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R3"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);

       stb_Bombers1 = f;
   }

   private void Stb_LoadBombers2DraftToMemory()
   {
       ISectionFile f = GamePlay.gpCreateSectionFile();
       string s = "";
       string k = "";
       string v = "";

       s = "AirGroups";
       k = "gb02.03"; v = ""; f.add(s, k, v);
       k = "g02.03"; v = ""; f.add(s, k, v);
       s = "gb02.03";
       k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
       k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
       k = "Class"; v = "Aircraft.WellingtonMkIc"; f.add(s, k, v);
       k = "Formation"; v = "VIC3"; f.add(s, k, v);
       k = "CallSign"; v = "26"; f.add(s, k, v);
       k = "Fuel"; v = "66"; f.add(s, k, v);
       k = "Weapons"; v = "1 1 3"; f.add(s, k, v);
       k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       s = "gb02.03_Way";
       k = "NORMFLY"; v = "11600.00 38900.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "12500.00 30000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "11500.00 24000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "12500.00 18000.00 2000.00 250.00"; f.add(s, k, v);
       k = "GATTACK_POINT"; v = "15660.00 10800.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "15660.00 9800.00 2200.00 333.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "11600.00 38900.00 3000.00 366.00"; f.add(s, k, v);
       s = "g02.03";
       k = "Flight0"; v = "1 2 3"; f.add(s, k, v);
       k = "Flight1"; v = "11 12 13"; f.add(s, k, v);
       k = "Class"; v = "Aircraft.He-111H-2"; f.add(s, k, v);
       k = "Formation"; v = "VIC3"; f.add(s, k, v);
       k = "CallSign"; v = "30"; f.add(s, k, v);
       k = "Fuel"; v = "66"; f.add(s, k, v);
       k = "Weapons"; v = "1 1 1 1 1 1 2"; f.add(s, k, v);
       k = "Skill0"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill1"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill2"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       k = "Skill10"; v = "0.88 0.88 0.88 0.88 0.88 0.88 0.99 0.99"; f.add(s, k, v);
       k = "Skill11"; v = "0.66 0.66 0.66 0.66 0.66 0.66 0.99 0.99"; f.add(s, k, v);
       k = "Skill12"; v = "0.44 0.44 0.44 0.44 0.44 0.44 0.99 0.99"; f.add(s, k, v);
       s = "g02.03_Way";
       k = "NORMFLY"; v = "20500.00 2000.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "20500.00 10000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "18500.00 18000.00 2000.00 240.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "18500.00 24000.00 2000.00 250.00"; f.add(s, k, v);
       k = "GATTACK_POINT"; v = "16300.00 31350.00 2000.00 250.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "16300.00 32350.00 2200.00 333.00"; f.add(s, k, v);
       k = "NORMFLY"; v = "20500.00 2000.00 3000.00 366.00"; f.add(s, k, v);
       s = "Stationary";
       k = "BmbArt_B0"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_B1"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10770 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_B2"; v = "Artillery.3_7_inch_QF_Mk_I de 15630 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_B3"; v = "Artillery.3_7_inch_QF_Mk_I de 15690 10830 90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R0"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R1"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31320 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R2"; v = "Artillery.3_7_inch_QF_Mk_I gb 16270 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);
       k = "BmbArt_R3"; v = "Artillery.3_7_inch_QF_Mk_I gb 16330 31380 -90 /timeout 0/radius_hide 4000"; f.add(s, k, v);

       stb_Bombers2 = f;
   }

   // Recursive Methods-------------------------------------------------------------------------------------------------------

   private void Stb_SpawnAntiAirRecursive()
   {
       try
       {
           for (int i = (stb_MissionsCount - 1); i > (-1); i--)
           {
               bool willBreak = false;
               for (int j = 0; j < 6; j++)
               {
                   string fullActorName = i.ToString() + ":baseDefenseShip_R_" + j.ToString("00");
                   AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                   if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
               }
               for (int j = 0; j < 6; j++)
               {
                   string fullActorName = i.ToString() + ":baseDefenseShip_B_" + j.ToString("00");
                   AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                   if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
               }
               if (willBreak) { break; }
           }
           for (int i = (stb_MissionsCount - 1); i > (-1); i--)
           {
               bool willBreak = false;
               for (int j = 0; j < 28; j++)
               {
                   string fullActorName = i.ToString() + ":baseDefenseGun_R_" + j.ToString("00");
                   AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                   if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
               }
               for (int j = 0; j < 28; j++)
               {
                   string fullActorName = i.ToString() + ":baseDefenseGun_B_" + j.ToString("00");
                   AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                   if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
               }
               if (willBreak) { break; }
           }
           GamePlay.gpPostMissionLoad(stb_BaseAntiAirDefenses);
           Timeout(stb_SpawnAntiAirDelay, Stb_SpawnAntiAirRecursive);
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_SpawnFrontline1Recursive()
   {
       try
       {
           if (stb_SpawnFrontline1)
           {
               GamePlay.gpPostMissionLoad(stb_Frontline1);
               Timeout(600.0, Stb_SpawnFrontline1Recursive);
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_SpawnFrontline2Recursive()
   {
       try
       {
           if (stb_SpawnFrontline2)
           {
               GamePlay.gpPostMissionLoad(stb_Frontline2);
               Timeout(660.0, Stb_SpawnFrontline2Recursive);
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_SpawnFrontline3Recursive()
   {
       try
       {
           if (stb_SpawnFrontline3)
           {
               GamePlay.gpPostMissionLoad(stb_Frontline3);
               Timeout(720.0, Stb_SpawnFrontline3Recursive);
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_SpawnFrontline4Recursive()
   {
       try
       {
           if (stb_SpawnFrontline4)
           {
               GamePlay.gpPostMissionLoad(stb_Frontline4);
               Timeout(420.0, Stb_SpawnFrontline4Recursive);
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_SpawnBombersRecursive()
   {
       try
       {
           if (stb_SpawnBombers)
           {
               for (int i = (stb_MissionsCount - 1); i > (-1); i--)
               {
                   bool willBreak = false;
                   for (int k = 0; k < 2; k++)
                   {
                       for (int j = 0; j < 3; j++)
                       {
                           string fullActorName = i.ToString() + ":g02.0" + k.ToString() + j.ToString();
                           AiAircraft aiAircraft = GamePlay.gpActorByName(fullActorName) as AiAircraft;
                           if (aiAircraft != null)
                           {
                               Timeout(456.0, () => { Stb_DestroyPlaneUnsafe(aiAircraft); });
                               willBreak = true;
                           }
                       }
                   }
                   for (int k = 0; k < 2; k++)
                   {
                       for (int j = 0; j < 3; j++)
                       {
                           string fullActorName = i.ToString() + ":gb02.0" + k.ToString() + j.ToString();
                           AiAircraft aiAircraft = GamePlay.gpActorByName(fullActorName) as AiAircraft;
                           if (aiAircraft != null)
                           {
                               Timeout(456.0, () => { Stb_DestroyPlaneUnsafe(aiAircraft); });
                               willBreak = true;
                           }
                       }
                   }
                   if (willBreak) { break; }
               }
               for (int i = (stb_MissionsCount - 1); i > (-1); i--)
               {
                   bool willBreak = false;
                   for (int j = 0; j < 4; j++)
                   {
                       string fullActorName = i.ToString() + ":BmbArt_R" + j.ToString("0");
                       AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                       if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                   }
                   for (int j = 0; j < 4; j++)
                   {
                       string fullActorName = i.ToString() + ":BmbArt_B" + j.ToString("0");
                       AiGroundActor aiGroundActor = GamePlay.gpActorByName(fullActorName) as AiGroundActor;
                       if (aiGroundActor != null) { aiGroundActor.Destroy(); willBreak = true; }
                   }
                   if (willBreak) { break; }
               }
               if (stb_BomberMissionTurn)
               {
                   GamePlay.gpPostMissionLoad(stb_Bombers1);
                   stb_BomberMissionTurn = !stb_BomberMissionTurn;
               }
               else
               {
                   GamePlay.gpPostMissionLoad(stb_Bombers2);
                   stb_BomberMissionTurn = !stb_BomberMissionTurn;
               }
               Timeout(600.0, Stb_SpawnBombersRecursive);
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_LogStatsRecursive()
   {
       try
       {
           if (stb_LogStats)
           {
               Timeout(stb_LogStatsDelay, Stb_LogStatsRecursive);
               //Do the regular stats save/upload business.  last "0" of 10,0,0 means do all various stats save EXCEPT the radar upload
               StbStatTask sst = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 10, 0, 0 });  //3rd # means 0=no radar upload, 1=radar upload
               stb_StatRecorder.StbSr_EnqueueTask(sst);
               
               //Upload RADAR files (the "1" in 10, 0, 1 indicates to radar upload) waiting 1/4 and 3/4 of the stats upload period.  so if stat save every 2 mins then
               //radar uploads happen every 1 min but offset from the stats save/upload.  Trying to spread the joy around a bit.
              
              Timeout(stb_LogStatsDelay / 4, () =>
                   {
                       StbStatTask sst1 = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 10, 0, 1 });  //3rd # means 0=no radar upload, 1=radar upload
                   stb_StatRecorder.StbSr_EnqueueTask(sst1);
                   });
              Timeout(stb_LogStatsDelay * 3 / 4, () =>
                  {
                      StbStatTask sst2 = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 10, 0, 1 });  //3rd # means 0=no radar upload, 1=radar upload
                   stb_StatRecorder.StbSr_EnqueueTask(sst2);
                  });          

              
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }


   private void Stb_StatsServerAnnounceRecursive()
   {
       try
       {   
           if (stb_AnnounceStatsMessages)         
           {
               string msg = "Check your Mission Stats online at " + stb_LogStatsPublicAddressLow + " or in-game using Tab-4 menu";
               if (stb_restrictAircraftByRank) msg += ", <ac, <nextac.";
               Stb_Message(null, msg, new object[] { });
               if (stb_iniFileErrorMessages != null && stb_iniFileErrorMessages.Length > 0) Timeout(5, () => { Stb_Message(null, stb_iniFileErrorMessages); }); //Also display the error message bec it is quite important & shouldn't even exist under normal circumstances
               Timeout(stb_AnnounceStatsMessagesFrequency, Stb_StatsServerAnnounceRecursive);
           }
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }






   // Other Methods-----------------------------------------------------------------------------------------------------------
   public void Stb_Message(Player[] players, string msg, object[] objs = null) //gets around the fact that child objects can't reference GamePlay
   {
       //GamePlay.gpLogServer( players, msg, objs);
       gpLogServerAndLog(players, msg, objs); //using the wrapper with delay etc.
   }
   //wrapper for Gameplay.GPLogServer so that we can do things to it, like log it, suppress all output, delay successive messages etc.
   public void gpLogServerAndLog(Player[] to, object data, object[] third = null)
   {
       //this is already logged to logs.txt so no need for this: if (LOG) logToFile (data, LOG_FULL_PATH);
       //gpLogServerWithDelay(to, (string)data, third);

       //gplogserver chokes on long chat messages, so we will break them up into chunks . . . 
       string str = (string)data;
       int maxChunkSize = 200;

       IEnumerable<string> lines = Calcs.SplitToLines(str, maxChunkSize);
       //for (int i = 0; i < str.Length; i += maxChunkSize)
       //for (int i=0; i<lines.GetLength(); i++) gpLogServerWithDelay(to, lines[i], third);

       foreach (string line in lines ) gpLogServerWithDelay(to, line, third);


   }
   //This is designed to space out gplogserver calls, as (say) 5-10 of these in a row will cause a very noticeable stutter
   //It's sort of a stack for gplogserver messages
   public void gpLogServerWithDelay(Player[] to, object data, object[] third = null)
   {
       //defined above:
       //public Int64 lastGpLogServerMsg_tick = 0;
       //public Int64 GpLogServerMsgDelay_tick = 1000000; //1 mill ticks or 0.1 second
       //public Int64 GpLogServerMsgOffset_tick = 500000; //Different modules or submissions can use a different offset to preclude sending gplogservermessages @ the same moment; 500K ticks or 0.05 second
       DateTime currentDate = DateTime.Now;
       //currentDate.Ticks
       Int64 nextMsg_tick = Math.Max(currentDate.Ticks, lastGpLogServerMsg_tick + GpLogServerMsgDelay_tick);
       Int64 remainder;
       Int64 roundTo = 1000000; //round nextMsg_tick UP to the next 1/10 second.  This is to allow different missions/modules to output at different portions of the 0.1 second interval, with the objective of avoiding stutters when messages from different .mis files or modules pile up
       nextMsg_tick = (Math.DivRem(nextMsg_tick - 1, roundTo, out remainder) + 1) * roundTo; // -1 handles the specific but common situation where we want a 0.1 sec delay but it always rounds it up to 0.2 sec.  This makes it round up for anything greater than roundTo, rather than greater than OR EQUAL TO roundTo.
       double nextMsgDelay_sec = (double)(nextMsg_tick - currentDate.Ticks) / 10000000;
       //string msg = (string)data + "(Delayed: " + nextMsgDelay_sec.ToString("0.00") + ")"; //for testing
       string msg = (string)data; 
       //GamePlay.gpLogServer(null, nextMsg_tick.ToString() + " " + nextMsgDelay_sec.ToString("0.00"), null); //for debugging
       Timeout(nextMsgDelay_sec, () => { GamePlay.gpLogServer(to, msg, third); });
       lastGpLogServerMsg_tick = nextMsg_tick; //Save the time_tick that this message will be displayed; next message will be at least GpLogServerMsgDelay_tick after this

   }

   //This is broken (broadcasts to everyone, not just the Player) but has one BIG advantage:
   //the messages can be seen on the lobby/map screen
   public void Stb_Chat(string line, Player player)
   {
       if (GamePlay is GameDef)
       {
           (GamePlay as GameDef).gameInterface.CmdExec("chat " + line + " TO " + player.Name());
       }
   }
   private void Stb_CutPlane(AiAircraft aircraft, LimbNames ln)
   {
       try { aircraft.cutLimb(ln); }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   private void Stb_DamagePlane(AiAircraft aircraft, NamedDamageTypes ndt)
   {
       try { aircraft.hitNamed(ndt); }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   public bool Stb_isAiControlledPlane(AiAircraft aircraft)
   {
       try {   

           if (aircraft == null)
               return false;

           //check if a player is in any of the "places"
           for (int i = 0; i < aircraft.Places(); i++)
               if (aircraft.Player(i) != null)
                   return false;

           return true;
       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); return false; }
   }

    //Makes sure no other players are in the a/c except for the given player
    //Note: does not actually check to make sure this player is also in the a/c.  OnPlaceLeave etc the player might be gone already
    //but we really need to make sure NO ONE ELSE is in there at this point, and the player is not still in TWO positions rather than one or zero.
    public bool Stb_isLastPlayerInAircraftandOneOrNoPositions(AiAircraft aircraft, Player player)
    {
        try
        {
 

            if (aircraft == null)
                return false;
            int count = 0;
            for (int i = 0; i < aircraft.Places(); i++)
            {
                if (aircraft.Player(i) == null) {
                    count++;
                    continue; }
                if (aircraft.Player(i) != player)
                    return false;
            }

            //When player is fully leaving plane this often looks like -1,-1 onPlaceLeave.  But also often 2,-1, 0,-1, etc.  Only commonality seems to be   Other possibilities if leaving only one position or switching
            //within the aircraft etc are 0,1 0,-1 -1,1 0,1 0,2 etc.  But -1,-1 seems reliable as far as this person as left the plane entirely.
            if (player.PlacePrimary() != null && player.PlaceSecondary() != null && ( player.PlacePrimary() == -1 || player.PlaceSecondary() == -1)) return true;
            if (aircraft.Places() == count) return true; //Case of NO live players left in plane
            return false;


        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); return false; }
    }
    
    public bool Stb_isPlayerFirstInAircraft(AiAircraft aircraft, Player player)
    {
        try
        {

            if (aircraft == null)
                return false;

            //check if player is in any of the "places"
            //and that this player is the FIRST actual player we encounter.  If so, return true
            for (int i = 0; i < aircraft.Places(); i++)
                if (aircraft.Player(i) == player)
                    return true;
                else if (aircraft.Player(i) == null) continue;
                else return false;

            return false;
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); return false; }
    }
    public void Stb_RemovePlayerFromCart(AiCart cart, Player player=null) //removes a certain player from any aircraft, artillery, vehicle, ship, or whatever actor/cart the player is in.  Removes from ALL places.
   //if player = null then remove ALL players from ALL positions
   {
       try
       {

           if (cart == null)
               return;

           //check if the player is in any of the "places" - if so remove
           for (int i = 0; i < cart.Places(); i++) {
               if (cart.Player(i) == null) continue;
               if (player != null)
               {
                   if (cart.Player(i).Name() == player.Name()) player.PlaceLeave(i); //we tell if they are the same player by their username.  Not sure if there is a better way.
               } else {
                   cart.Player(i).PlaceLeave(i);
               }
           }

       }
       catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
   }

   //First removes the player from the aircraft (after 1 second), ALL POSITIONS, then destroys the aircraft itself (IF it is AI controlled), after 3 more seconds
   public void Stb_RemovePlayerFromAircraftandDestroy(AiAircraft aircraft, Player player, double timeToRemove_sec = 1.0, double timetoDestroy_sec = 3.0)
   {
               Timeout(timeToRemove_sec, () => {

                   //player.PlaceLeave(0);
                   Stb_RemovePlayerFromCart(aircraft as AiCart, player);
                   Timeout(timetoDestroy_sec, () => {
                       if (Stb_isAiControlledPlane(aircraft)) Stb_DestroyPlaneUnsafe(aircraft);
                   }); //Destroy it a bit later--but only if no other players are in it

                   //Stb_DestroyPlaneUnsafe(actor as AiAircraft);  //OK, this approach seems to cause some problems when a player gets kicked out here &  then tries to click on the flags to change armies etc.
                   //Also, it can cause problems if there are TWO players in a plane & one is allowed, the other isn't.  The plane will be destroyed for both.
                   //also, if they jump into another plane (via right-click or whatever) in general that plane will just be destroyed ASAP.
                   //Stb_destroyAnyPlane(actor, false, 30);
                   //Stb_BalanceDisableForBan(actor); //can try this, too
                   //Stb_DestroyPlaneUnsafe(actor as AiAircraft);

               });
   }

   //Removes ALL players from an a/c after a specified period of time (seconds)
   public void Stb_RemoveAllPlayersFromAircraft(AiAircraft aircraft, double timeToRemove_sec = 1.0)
   {
       Timeout(timeToRemove_sec, () => {

           //player.PlaceLeave(0);

           for (int place = 0; place < aircraft.Places(); place++)
           {
               if (aircraft.Player(place) != null)
               {
                   //Stb_RemovePlayerFromCart(aircraft as AiCart, aircraft.Player(place));
                   Stb_RemovePlayerFromCart(aircraft as AiCart); //BEC. we're removing ALL players from this a/c we don't care about matching by name.  This can cause problems if the player is ie in a bomber in two different places, so better just to remove ALL no matter what.
               }
           }

       });
   }

   private void Stb_destroyAnyPlane(AiActor actor, bool checkForAI=true, int timeToWait=300)
   {
       try { 
           if (actor == null || !(actor is AiAircraft))
           {
               return;
           }

           AiAircraft aircraft = (actor as AiAircraft);

           if (checkForAI && !Stb_isAiControlledPlane(aircraft))
           {
               return;
           }

           if (aircraft == null)
           {
               return;
           }

           aircraft.hitNamed(part.NamedDamageTypes.ControlsElevatorDisabled);
           aircraft.hitNamed(part.NamedDamageTypes.ControlsAileronsDisabled);
           aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
           aircraft.hitNamed(part.NamedDamageTypes.FuelPumpFailure);
           aircraft.hitNamed(part.NamedDamageTypes.Eng0TotalFailure);
           aircraft.hitNamed(part.NamedDamageTypes.ElecPrimaryFailure);
           aircraft.hitNamed(part.NamedDamageTypes.ElecBatteryFailure);

           aircraft.hitLimb(part.LimbNames.WingL1, -0.5);
           aircraft.hitLimb(part.LimbNames.WingL2, -0.5);
           aircraft.hitLimb(part.LimbNames.WingL3, -0.5);
           aircraft.hitLimb(part.LimbNames.WingL4, -0.5);
           aircraft.hitLimb(part.LimbNames.WingL5, -0.5);
           aircraft.hitLimb(part.LimbNames.WingL6, -0.5);
           aircraft.hitLimb(part.LimbNames.WingL7, -0.5);

           int iNumOfEngines = (aircraft.Group() as AiAirGroup).aircraftEnginesNum();
           for (int i = 0; i < iNumOfEngines; i++)
           {
               aircraft.hitNamed((part.NamedDamageTypes)Enum.Parse(typeof(part.NamedDamageTypes), "Eng" + i.ToString() + "TotalFailure"));
           }

           /***Timeout (240, () =>
                   {explodeFuelTank (aircraft);}
               );
               * ***/

        Timeout(timeToWait, () =>
        { Stb_DestroyPlaneUnsafe(aircraft); }
            );

        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex);
        }

    }



    private void Stb_DestroyPlaneSafe(AiAircraft aircraft)
    {
        try
        {
            if (aircraft == null) { return; }
            for (int place = 0; place < aircraft.Places(); place++)
            {
                if (aircraft.Player(place) != null) { return; }
            }
            if (aircraft.IsAirborne())
            {
                bool insideAf = false;
                if ((aircraft.Pos().distance(ref af1) < 500.0) && (aircraft.Pos().z < 10.0)) { insideAf = true; }
                if ((aircraft.Pos().distance(ref af2) < 500.0) && (aircraft.Pos().z < 70.0)) { insideAf = true; }
                if ((aircraft.Pos().distance(ref af3) < 500.0) && (aircraft.Pos().z < 40.0)) { insideAf = true; }
                if ((aircraft.Pos().distance(ref af4) < 500.0) && (aircraft.Pos().z < 70.0)) { insideAf = true; }
                if (insideAf && stb_Debug) { Console.WriteLine("Destroying aircraft -stats.cs DPS1"); aircraft.Destroy(); }
                else
                {
                    Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsElevatorDisabled);
                    Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsAileronsDisabled);
                    Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsRudderDisabled);
                    Stb_DamagePlane(aircraft, NamedDamageTypes.Eng0TotalSeizure);
                    AiAirGroup aag = aircraft.AirGroup();
                    if (aag != null)
                    {
                        if (aag.aircraftEnginesNum() == 2) { Stb_DamagePlane(aircraft, NamedDamageTypes.Eng1TotalSeizure); }
                    }
                }
            }
            else {
                //Console.WriteLine("Destroying aircraft -stats.cs DPS");
                aircraft.Destroy(); 
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyPlaneUnsafe(AiAircraft aircraft)
    {
        try
        {
            if (aircraft != null)
            {
                //Console.WriteLine("Destroying aircraft -stats.cs DPU");
                aircraft.Destroy();
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyFrontShip(AiGroundActor aiGroundActor)
    {
        try
        {
            if (aiGroundActor != null) { if (aiGroundActor.Name().Contains("FrontShip")) { aiGroundActor.Destroy(); } }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyFrontArmor(AiGroundActor aiGroundActor)
    {
        try
        {
            if (aiGroundActor != null) { if (aiGroundActor.Name().Contains("FrontArmor")) { aiGroundActor.Destroy(); } }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    //Kill the player/aircraft/whatever after a specified amount of time
    private bool Stb_killActor(AiActor actor, int waitTime = 0)
    {
        if (actor != null)
        {
            Timeout(waitTime, () => { 
                //Console.WriteLine("KillActor: " + actor.Name() );
                //Battle.OnActorDead(0, player.Name(), actor, OnBattleStarted.GetDamageInitiators(actor); 
                Battle.OnEventGame(GameEventId.ActorDead, actor, Battle.GetDamageInitiators(actor), 0);
            });
            
            return true;

            //Battle.OnEventGame(GameEventId.ActorDead, actor, Battle.GetDamageInitiators(actor), 0); //or similar might do the same thing.  If instead of null AIDamageInitiator is included, that would be better . . . 
        }
        //Console.WriteLine("KillActor: Actor was NULL");
        return false;
    }

    private void Stb_KillACNowIfInAircraftKilled(AiAircraft aircraft) {

        if (aircraft != null && (aircraft as AiActor) != null && stb_aircraftKilled.Contains((aircraft as AiActor).Name()) )
        {  //In case this aircraft was listed as "killed" earlier it will count as a victory for the damagers but not a death for the player(s) in the a/c
            Stb_RemoveAllPlayersFromAircraft(aircraft, 5); //remove all players 5 sec
            Stb_killActor((aircraft as AiActor), 7); //kill this a/c 6 sec
            stb_aircraftKilled.Remove((aircraft as AiActor).Name());
        }

    }

    public void Stb_destroyAllAIAircraft()
    {
        //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in GamePlay.gpArmies())
            {
                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                {
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft a = actor as AiAircraft;
                                    if (a != null)
                                    {
                                        Stb_DestroyPlaneSafe(a);
                                        if (stb_Debug) Console.WriteLine("Destroy All AI A/C: " + actor.Name());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //Put a player into a certain place of a certain plane.
    private bool Stb_putPlayerIntoAircraftPosition(Player player, AiActor actor, int place)
    {
        if (player != null && actor != null && (actor as AiAircraft != null))
        {
            AiAircraft aircraft = actor as AiAircraft;
            player.PlaceEnter(aircraft, place);
            return true;
        }
        return false;
    }

    //Experiment with changingn the airgroup target to player, seeing if they will attack better
    public void Stb_changeTargetToPlayerRecurs(Player player) {
        Timeout(28, () => { Stb_changeTargetToPlayerRecurs(player); });
        Timeout (5, () => { Console.WriteLine("CHANGETARGET: Just changed for all"); });
        Stb_changeTargetAllAIAircraft(player);
        //Timeout(28, () => { Stb_changeTargetToPlayerRecurs(player); });
    }

    public void Stb_changeTargetAllAIAircraft(Player player)
    {
        return;
        //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
        if (player.Place() == null || (player.Place() as AiAircraft)==null) return;

        if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
        {
            foreach (int army in GamePlay.gpArmies())
            {
                if (army == player.Army()) continue;
                if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                {
                    foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                    {
                        if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                        {
                            foreach (AiActor actor in airGroup.GetItems())
                            {
                                if (actor is AiAircraft)
                                {
                                    AiAircraft aircraft = actor as AiAircraft;
                                    if (aircraft != null)
                                    {
                                      if (Stb_isAiControlledPlane(aircraft) && aircraft.IsAirborne()) {
                                            //AiAirGroup.candidates()
                                            //AiAirGroup.enemies()
                                            //AiAirGroup.getTask()
                                            //AiAirGroup.isAircraftType(maddox.game.world.AircraftType)
                                            //AiAirGroup.Vwld()
                                            //AiAirGroupTask.ATTACK_AIR
                                            // && airGroup.getTask() == AiAirGroupTask.ATTACK_AIR to affect only those a/c who are in attack mode, etc.
                                            //&& airGroup.getTask() != AiAirGroupTask.ATTACK_GROUND to make all attack except (ie) bombers
                                            //&& !AiAirGroup.isAircraftType(AircraftType.Bomber) //to exclude bombers

                                            
                                            airGroup.setTask(AiAirGroupTask.ATTACK_AIR, (player.Place() as AiAircraft).AirGroup());
                                            airGroup.changeGoalTarget(player.Place());
                                            if (stb_Debug) Console.WriteLine("ChangeGoalTarget: " + actor.Name() + " to " + player.Name());

                                            break; //each airGroup has only one target so no need to do this more than once.
                                      }   
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    //Each aiairgroup can have only one goal target, which becomes kind of unrealistic.  
    //What we do is call this to change the target to a different a/c.  We could do this based on proximity or whatever
    //but for now we are mostly doing it based on damage or kills.  If you damage/kill one of the a/c in a group then
    //the group will likely turn on that damager as the new target.
    public void Stb_changeTargetOneAirgroupToPlayer(Player player, AiAircraft aircraft, string source = "", bool noTaskChange = true,
        bool onlyAirAttackers = false, bool excludeBombers = false, bool excludeGroundAttackers = false)
    {
        try
        {
            double luck3 = stb_random.NextDouble();

            if (source == "damage" && luck3 < 0.9) return;
            if (source == "cutlimb" && luck3 < 0.5) return;
            if (source == "dead" && luck3 < 0.5) return;

            //List<Tuple<AiAircraft, int>> aircraftPlaces = new List<Tuple<AiAircraft, int>>();
            if (player.Place() == null || (player.Place() as AiAircraft) == null) return;
            if (aircraft == null || !Stb_isAiControlledPlane(aircraft)) return;
            if (aircraft.Army() == player.Army()) return;
            AiAirGroup airGroup = aircraft.AirGroup();
            if (airGroup == null) return;
            if (airGroup.GetItems() == null || airGroup.GetItems().Length == 0) return;

            //AiAirGroup.candidates()
            //AiAirGroup.enemies()
            //AiAirGroup.getTask()
            //AiAirGroup.isAircraftType(maddox.game.world.AircraftType)
            //AiAirGroup.Vwld()
            //AiAirGroupTask.ATTACK_AIR
            // && airGroup.getTask() == AiAirGroupTask.ATTACK_AIR to affect only those a/c who are in attack mode, etc.
            //&& airGroup.getTask() != AiAirGroupTask.ATTACK_GROUND to make all attack except (ie) bombers
            //&& !AiAirGroup.isAircraftType(AircraftType.Bomber) //to exclude bombers

            //We set some overall variables to govern the overall probability of airgroups (bomber or otherwise) changing their current
            //goal to attack the attacking a/c
            double luck = stb_random.NextDouble();
            double luck2 = stb_random.NextDouble();
            if (stb_Debug) Stb_Message(new Player[] { player }, "CHANGETARGET: luck: " + luck.ToString("N2") + " NOfAir: " + airGroup.NOfAirc.ToString() 
            + " stb_ChangeAttackProb " + stb_ChangeAttackProb.ToString("N2") + "  stb_ChangeBomberAttackProb " +  stb_ChangeBomberAttackProb.ToString("N2") + " stb_ChangeAttackProb_SmallGroup  " + 
            stb_ChangeAttackProb_SmallGroup.ToString("N2") + " PROBS: " + (airGroup.NOfAirc <= stb_ChangeAttackProb_SmallGroupThresh && luck < stb_ChangeAttackProb_SmallGroup).ToString() + " " + ((airGroup.isAircraftType(AircraftType.Bomber) && luck < stb_ChangeBomberAttackProb)).ToString() + " " + (luck < stb_ChangeAttackProb).ToString() + " source " + source, new object[] { });

            
            
            if 
            ( 
                    (airGroup.NOfAirc <= stb_ChangeAttackProb_SmallGroupThresh && luck < stb_ChangeAttackProb_SmallGroup) 
                    || ( (airGroup.isAircraftType(AircraftType.Bomber) && luck < stb_ChangeBomberAttackProb) )
                    || (!airGroup.isAircraftType(AircraftType.Bomber) && (luck < stb_ChangeAttackProb) )
            ){
            
            
               if (onlyAirAttackers && airGroup.getTask() != AiAirGroupTask.ATTACK_AIR) return;
               if (excludeBombers && airGroup.isAircraftType(AircraftType.Bomber)) return;
               if (excludeGroundAttackers && airGroup.getTask() == AiAirGroupTask.ATTACK_GROUND) return;
               

                if (stb_Debug) Stb_Message(new Player[] { player }, "CHANGETARGET: CHANGING", new object[] { });

                //if (!noTaskChange) airGroup.setTask(AiAirGroupTask.ATTACK_AIR, (player.Place() as AiAircraft).AirGroup());
                //Not sure which is best, so we'll try them all . . .
                if (luck2 < 0.3333) airGroup.changeGoalTarget(player.Place());
                else if (luck2 <0.666667) airGroup.setTask(AiAirGroupTask.ATTACK_AIR, (player.Place() as AiAircraft).AirGroup());
                else if (luck2 < .83 ) {
                    airGroup.changeGoalTarget(player.Place());
                    airGroup.setTask(AiAirGroupTask.ATTACK_AIR, (player.Place() as AiAircraft).AirGroup());
                }
                else {                    
                    airGroup.setTask(AiAirGroupTask.ATTACK_AIR, (player.Place() as AiAircraft).AirGroup());
                    airGroup.changeGoalTarget(player.Place());
                }

                if (stb_Debug) Stb_Message(new Player[] { player }, "CHANGETARGET: " + Calcs.GetAircraftType(aircraft) + " to " + player.Name(), new object[] { });
                string playername = "unknown/AI";
                if (player != null) playername = player.Name();
                Console.WriteLine("CHANGETARGET: " + Calcs.GetAircraftType(aircraft) + " to " + playername);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.Stb_changeTargetOneAirgroupToPlayer - Exception: " + ex.ToString());
        }
    }
    /*
     *         foreach (AiBirthPlace a in GamePlay.gpBirthPlaces())
        {
            if (a.Army() != actor.Army()) continue;


            //if (!a.IsAlive()) continue;


            Point3d pp;
            pp = a.Pos();
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                d2Min = d2;
                //GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString() + " distance " + d2.ToString("n0"), new object[] { });
            }

        }
        //GamePlay.gpLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
        return Math.Sqrt(d2Min);
 
    */
    //returns distance to nearest friendly airport to actor, in meters. Count all friendly airports, alive or not.
    //In case of birthplace find, get the nearest birthplace regardless of friendly or not
    private double Stb_distanceToNearestAirport(AiActor actor, bool birthplacefind=false) {
        double d2 = 10000000000000000; //we compare distanceSQUARED so this must be the square of some super-large distance in meters && we'll return anything closer than this.  Also if we don't find anything we return the sqrt of this number, which we would like to be a large number to show there is nothing nearby.  If say d2 = 1000000 then sqrt (d2) = 1000 meters which probably not too helpful.
        double d2Min = d2;
        if (actor == null) return d2Min;
        Point3d pd = actor.Pos();

        int n;
        if (birthplacefind) n = GamePlay.gpBirthPlaces().Length;
        else n = GamePlay.gpAirports().Length;

        //AiActor[] aMinSaves = new AiActor[n + 1];
        //int j = 0;
        //GamePlay.gpLogServer(null, "Checking distance to nearest airport", new object[] { });
        for (int i = 0; i < n; i++)
        {
            AiActor a;
            if (birthplacefind) a = (AiActor)GamePlay.gpBirthPlaces()[i];
            else a = (AiActor)GamePlay.gpAirports()[i];

            if (a == null) continue;
            //if (actor.Army() != a.Army()) continue; //only count friendly airports
            //if (actor.Army() != (a.Pos().x, a.Pos().y)
            //OK, so the a.Army() thing doesn't seem to be working, so we are going to try just checking whether or not it is on the territory of the Army the actor belongs to.  For some reason, airports always (or almost always?) list the army = 0.

            //GamePlay.gpLogServer(null, "Checking airport " + a.Name() + " " + GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) + " " + a.Pos().x.ToString ("N0") + " " + a.Pos().y.ToString ("N0") , new object[] { });

            if (!birthplacefind && GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != actor.Army()) continue;

            
            //if (!a.IsAlive()) continue;

            
            Point3d pp;
            pp = a.Pos();
            pd.z = pp.z;
            d2 = pd.distanceSquared(ref pp);
            if (d2 < d2Min)
            {
                d2Min = d2;
                //GamePlay.gpLogServer(null, "Checking airport / added to short list" + a.Name() + " army: " + a.Army().ToString(), new object[] { });
            }

        }
        //GamePlay.gpLogServer(null, "Distance:" + Math.Sqrt(d2Min).ToString(), new object[] { });
        return Math.Sqrt(d2Min);
    }

    //nearest airport to a point
    //army=0 is neutral, meaning found airports of any army
    //otherwise, find only airports matching that army
    public AiAirport Stb_nearestAirport(Point3d location, int army=0)
    {
        AiAirport NearestAirfield = null;
        AiAirport[] airports = GamePlay.gpAirports();
        Point3d StartPos = location;

        if (airports != null)
        {
            foreach (AiAirport airport in airports)
            {
                AiActor a = airport as AiActor;
                if (army != 0 && GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != army) continue;                
                if (NearestAirfield != null)
                {
                    if (NearestAirfield.Pos().distanceSquared(ref StartPos) > airport.Pos().distanceSquared(ref StartPos))
                        NearestAirfield = airport;
                }
                else NearestAirfield = airport;
            }
        }


        //AirfieldDisable(NearestAirfield); //for testing
        //Console.WriteLine("Destroying airfield " + NearestAirfield.Name());
        return NearestAirfield;
    }

    //nearest airport to an actor
    public AiAirport Stb_nearestAirport(AiActor actor, int army= 0)
    {
        if (actor == null) return null;
        Point3d pd = actor.Pos();
        return Stb_nearestAirport(pd, army);
    }

    //nearest BIRTHPLACE to a point
    //army=0 is neutral, meaning found airports of any army
    //otherwise, find only airports matching that army
    public AiBirthPlace Stb_nearestBirthPlace(Point3d location, int army = 0)
    {
        AiBirthPlace NearestAirfield = null;
        AiBirthPlace[] airports = GamePlay.gpBirthPlaces();
        Point3d StartPos = location;

        if (airports != null)
        {
            foreach (AiBirthPlace airport in airports)
            {
                AiActor a = airport as AiActor;
                if (army != 0 && GamePlay.gpFrontArmy(a.Pos().x, a.Pos().y) != army) continue;
                if (NearestAirfield != null)
                {
                    if (NearestAirfield.Pos().distanceSquared(ref StartPos) > airport.Pos().distanceSquared(ref StartPos))
                        NearestAirfield = airport;
                }
                else NearestAirfield = airport;
            }
        }
        return NearestAirfield;
    }

    //nearest airport to an actor
    public AiBirthPlace Stb_nearestBirthPlace(AiActor actor, int army = 0)
    {
        if (actor == null) return null;
        Point3d pd = actor.Pos();
        return Stb_nearestBirthPlace(pd, army);
    }


    #endregion

    // Overrides---------------------------------------------------------------------------------------------------------------

    public override void Init(ABattle b, int missionNumber)
    {
        base.Init(b, missionNumber);

        this.battle = b;
        MissionNumberListener = -1; //This is what allows you to catch all the OnTookOff, OnAircraftDamaged, and other similar events.  Vitally important to make this work!
        //If we load missions as sub-missions, as we often do, it is vital to have this in Init, not in "onbattlestarted" or some other place where it may never be detected or triggered if this sub-mission isn't loaded at the very start.
        //Console.WriteLine("starting chat1");
        //Start Chat Server
        if (GamePlay is GameDef)
        {
            //Console.WriteLine("starting chat2");
            (GamePlay as GameDef).EventChat += new GameDef.Chat(Mission_EventChat);
        }
    }

    //OK, this never gets called in -stats.cs files because the main .cs file loads initial submissions during OnBattleStarted!  So any initial things you need, need to be loaded elsewhere.
    public override void OnBattleStarted()
    {
        #region stb
        base.OnBattleStarted();
        #endregion


        //DON'T PUT ANYTHING HERE!!!!
        //IT WILL NEVER BE CALLED!!!
        

    }
    public override void OnBattleStoped()
    {
        #region
        base.OnBattleStoped();

        Console.WriteLine("Stats-OnBattleStoped1");

        try
        {
            //Get list of all players, then send them to StbCmr_SavePositionLeave
            //We don't want ot actually send them to OnPlaceLeave because that may kill them etc depending on where
            //they are. This will just close out their stats without any chance of killing etc.
            List<Player> Players = new List<Player>();

            // Multiplayer
            if (GamePlay.gpRemotePlayers() != null && GamePlay.gpRemotePlayers().Length > 0)
            {
                foreach (Player p in GamePlay.gpRemotePlayers())
                {

                    Players.Add(p);
                }
            }
            /*
            //Singleplayer or Dedi Server
            if (GamePlay.gpPlayer() != null)
            {

                Players.Add(GamePlay.gpPlayer());
            } 
            */
            if (Players != null && Players.Count > 0)
            {

                foreach (Player player in Players)
                {
                    stb_ContinueMissionRecorder.StbCmr_SavePositionLeave(player, player.Place(), true); //immediately end their sortie & save stats (immed=true)

                    //So, if we people are still flying when the mission ends we want to return their aircraft to supply & not destroy them.
                    //we can do this multiple times per aircraft bec. only the first one counts, but we're still going to save by only doing it for the first position encountered
                    AiActor actor = player.Place();
                    if (actor != null && Stb_isPlayerFirstInAircraft(actor as AiAircraft, player))
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, player.PlacePrimary(), true); //Since this is a real position leave, -supply.cs handles the details of returning the a/c to supply
                }
            }
        } catch (Exception ex) { Console.WriteLine("Stats-OnBattleStoped ERROR1: " + ex.ToString()); }

        Console.WriteLine("Stats-OnBattleStoped2");
        //More cleanup    
        try
        {
            Stb_destroyAllAIAircraft();

        }
        catch (Exception ex) { Console.WriteLine("Stats-OnBattleStoped ERROR1: " + ex.ToString()); }
        //OK, any aircraft that were previously called via onAircraftKilled, if we haven't given credit for them yet via onActorDead, we will do it now @ the end.

        Console.WriteLine("Stats-OnBattleStoped3");

        try
        {
            foreach (string aname in stb_aircraftKilled)
            {
                //if (stb_Debug) Console.WriteLine("This a/c was killed but wasn't registered earlier: " + aname);
                AiActor actor = GamePlay.gpActorByName(aname);
                if (actor as AiAircraft != null) Stb_KillACNowIfInAircraftKilled(actor as AiAircraft); //In case this aircraft was listed as "killed" earlier it will count as a victory for the damagers but not a death for the player(s) in the a/c
            }
        }
        catch (Exception ex) { Console.WriteLine("Stats-OnBattleStoped ERROR2: " + ex.ToString()); }

        Console.WriteLine("Stats-OnBattleStoped4");

        try
        {
            foreach (string aname in stb_deadActors)
            {
                if (stb_Debug) Console.WriteLine("Actor killed this battle: " + aname);
            }
        }
        catch (Exception ex) { Console.WriteLine("Stats-OnBattleStoped ERROR3: " + ex.ToString()); }

        Console.WriteLine("Stats-OnBattleStoped5");

        try {
            if (GamePlay is GameDef)
            {
                //Console.WriteLine ( (GamePlay as GameDef).EventChat.ToString());
                (GamePlay as GameDef).EventChat -= new GameDef.Chat(Mission_EventChat);
                //If we don't remove the new EventChat when the battle is stopped
                //we tend to get several copies of it operating, if we're not careful


            }
        }
        catch (Exception ex) { Console.WriteLine("Stats-OnBattleStoped ERROR4: " + ex.ToString()); }

        Console.WriteLine("Stats-OnBattleStoped6");

        try
        {
            stb_SaveIPlayerStat.StbSis_SaveAll(); //Save the stats CloD has been accumulating. We need to call this (at MIN! when the player first arrives on the server & when the player leaves and/or the server shuts down. We also call it on player death.  could be called more times at convenient intervals or whatever, which could prevent any stats from being lost in case of unexpected shutdown etc.
            if (stb_LogStats)
            {
                Console.WriteLine("OnBattleStoped - saving stats");
                StbStatTask sst = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 0, 1, 0 }); //2nd entry "1" means final save of the mission
                stb_StatRecorder.StbSr_EnqueueTask(sst);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); } //So, for example we can't use the Stb_PrepareErrorMessage scheme any more (?)

        Console.WriteLine("Stats-OnBattleStoped7");

        try
        {
            stb_StatRecorder.StbSr_FinishWaitingTasks(); //THIS CLOSES THE SECOND THREAD SO STATS WON'T WORK AT ALL AFTER THIS IS CALLED!!!

        }
        catch (Exception ex) { Console.WriteLine("Stats-OnBattleStoped ERROR: " + ex.ToString()); } //Stb_PrepareErrorMessage(ex); } //So, for example we can't use the Stb_PrepareErrorMessage scheme any more (?)

        Console.WriteLine("Stats-OnBattleStoped8");

        #endregion
    }

    ISupplyMission TWCSupplyMission;
    int stb_lastMissionLoaded = -1;
    public override void OnMissionLoaded(int missionNumber)
    {
        #region stb
        base.OnMissionLoaded(missionNumber);

        //TWCComms.Communicator.Instance.Stats = (IStatsMission)this; //allows -stats.cs to access this instance of Mission

        //AMission TWCMainMission;
        //IStatsMission TWCStatsMission;

        //TWCMainMission = TWCComms.Communicator.Instance.Main;
        TWCSupplyMission = TWCComms.Communicator.Instance.Supply;
        //Console.WriteLine("GetType15: " + (TWCStatsMission.GetType().ToString()));
        //Console.WriteLine("GetType16: " + TWCStatsMission.stb_ServerName_Public);
        //Console.WriteLine("GetType17: ");
        //TWCStatsMission.Inited();
        //Console.WriteLine("GetType18: ");
        //TWCMainMission.Inited();
        //Console.WriteLine("GetType19: " + TWCStatsMission.stb_LocalMissionIniDirectory);
        //Console.WriteLine("GetType20: " + TWCStatsMission.ot_GetCivilianBombings("TWC_Flug"));

        
        stb_MissionsCount++;
        stb_lastMissionLoaded = missionNumber;
        #endregion
        //add your code here

        if (missionNumber==MissionNumber ) {
            //Display any error messages that occured while reading the .ini file
            //Wait a while to do it because usually nobody is on right @ the beginning to even see it.  It displays right away in the console anyway.
            //if (stb_iniFileErrorMessages != null && stb_iniFileErrorMessages.Length > 0) Timeout(240, () => { Stb_Message(null, stb_iniFileErrorMessages, null); });             

            //Escort21.coord.Communicate coms = new Escort21.coord.Communicate();

            //Console.WriteLine("STATS: " + Communicate.test.ToString());
            string s = stb_AppPath.Remove(stb_AppPath.Length - 5, 5);
            stb_FullPath = s + stb_LocalMissionStatsDirectory; //@"missions\Multi\Fatal\"            
            TWCComms.Communicator.Instance.stb_FullPath = stb_FullPath;
            stb_ErrorLogPath = stb_FullPath + stb_ErrorLogPath;
            stb_StatsPathTxt = stb_FullPath + stb_StatsPathTxt;
            stb_StatsPathHtmlLow = stb_FullPath + stb_StatsPathHtmlLow;
            Stb_LoadAntiAirDraftToMemory();
            Stb_LoadFrontline1DraftToMemory();
            Stb_LoadFrontline2DraftToMemory();
            Stb_LoadFrontline3DraftToMemory();
            Stb_LoadFrontline4DraftToMemory();
            Stb_LoadBombers1DraftToMemory();
            Stb_LoadBombers2DraftToMemory();
            stb_StatRecorder = new StbStatRecorder(this, stb_LogStats, stb_LogStatsCreateHtmlLow, stb_LogStatsCreateHtmlMed, stb_StatsPathTxt,
                                                    stb_LogErrors, stb_ErrorLogPath, stb_StatsPathHtmlLow, stb_StatsPathHtmlExtLow, stb_StatsPathHtmlMed,
                                                    stb_LogStatsUploadHtmlLow, stb_LogStatsUploadHtmlMed,
                                                    stb_LogStatsUploadAddressLow,stb_LogStatsUploadAddressExtLow, stb_LogStatsUploadAddressMed,
                                                    stb_LogStatsUploadUserName, stb_LogStatsUploadPassword,
                                                    stb_LogStatsUploadFilenameLow, stb_MissionServer_LogStatsUploadFilenameLow, stb_LogStatsUploadFilenameDeadPilotsLow,
                                                    stb_LogStatsUploadFilenameTeamLow, stb_LogStatsUploadFilenameTeamPrevLow,
                                                    stb_ResetPlayerStatsWhenKilled, stb_NoRankMessages, stb_NoRankTracking, stb_PlayerTimeoutWhenKilled, stb_PlayerTimeoutWhenKilledDuration_hours
                                                    );
    

            stb_ContinueMissionRecorder=new StbContinueMissionRecorder(this,stb_LogErrors, stb_ErrorLogPath);
            stb_RankToAllowedAircraft = new StbRankToAllowedAircraft(this, stb_LogErrors, stb_ErrorLogPath);
            stb_SaveIPlayerStat = new StbSaveIPlayerStat(this, stb_LogErrors, stb_ErrorLogPath);

            stb_AircraftParamStack = new StbAircraftParamStack(this);

            //stb_KilledActors = new KilledActorsWrapper(); // keeps track of all damage to actor & which actor did it, so it can be compiled for stats purposes later

            if (stb_SpawnAntiAir) { Stb_SpawnAntiAirRecursive(); }
            if (stb_SpawnFrontline1) { Stb_SpawnFrontline1Recursive(); }
            if (stb_SpawnFrontline2) { Stb_SpawnFrontline2Recursive(); }
            if (stb_SpawnFrontline3) { Stb_SpawnFrontline3Recursive(); }
            if (stb_SpawnFrontline4) { Stb_SpawnFrontline4Recursive(); }
            if (stb_SpawnBombers) { Stb_SpawnBombersRecursive(); }
            if (stb_LogStats) { Stb_LogStatsRecursive(); }
            if (stb_StatsServerAnnounce) { Stb_StatsServerAnnounceRecursive();}
            SetAirfieldTargets();


        }

        

    }    

    void Mission_EventChat(Player from, string msg)
    {
        if (!msg.StartsWith("<")) return; //trying to stop parser from being such a CPU hog . . . 
        
        Player player = from as Player;
        AiAircraft aircraft = null;
        if (player.Place() as AiAircraft != null) aircraft = player.Place() as AiAircraft;

        string msg_orig = msg;
        msg=msg.ToLower();
        //Stb_Message(null, "Stats msg recvd.", null);
        if (msg.StartsWith("<deban") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "Pilot restriction after death removed until end of mission.", new object[] { });
            stb_PlayerTimeoutWhenKilled = false;
            stb_StatRecorder.StbSr_Deban();
        }
        else if (msg.StartsWith("<reban") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "Pilot restriction after death restored.", new object[] { });
            stb_PlayerTimeoutWhenKilled = true;
            stb_StatRecorder.StbSr_Reban();
        }
        if (msg.StartsWith("<training") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "Pilot timeout after death, aircraft restrictions by rank, stats removed until end of mission.", new object[] { });
            stb_PlayerTimeoutWhenKilled = false;
            stb_StatRecorder.StbSr_Deban();
            stb_restrictAircraftByRank = false;
            stb_LogStats = false;
            stb_StatRecorder.StbSr_LogStats_off();
        }
        if (msg.StartsWith("<untraining") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "Pilot timeout after death, aircraft restrictions by rank, stats turned on again.", new object[] { });
            stb_PlayerTimeoutWhenKilled = true;
            stb_StatRecorder.StbSr_Reban();
            stb_restrictAircraftByRank = true;
            stb_LogStats = true;
            stb_StatRecorder.StbSr_LogStats_on();
        }
        else if (msg.StartsWith("<lista") && player.Name().Substring(0, 4) == @"TWC_")
        {
            int cnt = 0;
            //string acEnter = Substring
            string acEnter_str = "";
            if (msg.Length>=7) acEnter_str = msg.Substring(7);
            int acEnter = -1;

            try
            {
                acEnter = Convert.ToInt32(acEnter_str);
            }

            catch (Exception ex) //anything else
            {
                acEnter = -1;
            }            

            if (GamePlay.gpArmies() != null && GamePlay.gpArmies().Length > 0)
            {
                foreach (int army in GamePlay.gpArmies())
                {
                    //List a/c in player army if "inOwnArmy" == true; otherwise lists a/c in all armies EXCEPT the player's own army
                    if (GamePlay.gpAirGroups(army) != null && GamePlay.gpAirGroups(army).Length > 0)
                    {
                        foreach (AiAirGroup airGroup in GamePlay.gpAirGroups(army))
                        {
                            
                            if (airGroup.GetItems() != null && airGroup.GetItems().Length > 0)
                            {
                                
                                foreach (AiActor actor in airGroup.GetItems())
                                {
                                    
                                    if (actor is AiAircraft)
                                    {
                                        cnt++;
                                        if (acEnter <0)Stb_Message(new Player[] { player }, cnt.ToString() + ": " + actor.Name() + " " + Calcs.GetAircraftType(actor as AiAircraft), new object[] { });
                                        else if (acEnter==cnt) Stb_putPlayerIntoAircraftPosition(player, actor, 0);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            

        }
        else if (msg.StartsWith("<ent") && player.Name().Substring(0, 4) == @"TWC_")
        {

            string name = msg.Substring(5);
            AiActor actor = GamePlay.gpActorByName(name) as AiActor;
            if (actor != null) gpLogServerAndLog(new Player[] { player }, "Moving to " + name, new object[] { });
            else
            {
                gpLogServerAndLog(new Player[] { player }, name + " not found.  Must have exact name as reported in right-click/View Aircraft-Ships-Misc", new object[] { });
                gpLogServerAndLog(new Player[] { player }, "with 'Show Object Names' selected.Example: '23:BoB_RAF_F_141Sqn_Early.000'", new object[] { });
            }
            Stb_putPlayerIntoAircraftPosition(player, actor, 0);

        }
        if (msg.StartsWith("<listp") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            //Dictionary<string, Dictionary<string, List<Tuple<string, string, double>>> stb_PilotTypeRecordbyAC = new Dictionary<string, Dictionary<string, List<Tuple<string, string, double>>>>();

            string msgl;

            foreach (KeyValuePair<string, Dictionary<string, List<Tuple<string, string, double>>>> ACEntry in stb_PilotTypeRecordbyAC)
            {
                msgl = ACEntry.Key + ": ";
                foreach (KeyValuePair<string, List<Tuple<string, string, double>>> PilotEntry in ACEntry.Value)
                {
                    msgl += PilotEntry.Key + ": ";
                    foreach (Tuple<string, string, double> PosEntry in PilotEntry.Value)
                    {
                        msgl += PosEntry.ToString() + " ";
                    }
                }
                Stb_Message(new Player[] { player }, msgl, new object[] { });            

            }

        }

        else if (msg.StartsWith("<car") || msg.StartsWith("<ran"))  //career, rank
        {
            Stb_Message(new Player[] { player }, ">>>Please use Tab-4 menu for in-game stats summaries", null);
            stb_StatRecorder.StbSr_Display_AceAndRank_ByName(player.Name(), player.Place() as AiActor, player);
        }
        else if (msg.StartsWith("<ses"))
        {
            Stb_Message(new Player[] { player }, ">>>Please use Tab-4 menu for in-game stats summaries", null);
            stb_StatRecorder.StbSr_Display_SessionStats(player);
        }
        else if (msg.StartsWith("<net"))
        {
            Stb_Message(new Player[] { player }, ">>>Please use Tab-4 menu for in-game stats summaries", null);
            stb_StatRecorder.StbSr_Display_SessionStatsAll(player, 0); //display "Netstats" summary of current session stats for all players to this player
        }        
        else if (msg.StartsWith("<obj"))
        {
            Stb_Message(new Player[] { player }, ">>>Please use Tab-4 menu for in-game stats summaries", null);
            stb_StatRecorder.StbSr_Display_SessionStatsTeam(player);
        }
            
        else if (msg.StartsWith("<ac")) //aircraft available
        {
            Stb_Message(new Player[] { player }, ">>>Please use Tab-4 menu for in-game career info", null);
            Display_AircraftAvailable_ByName(player, nextAC: false, display: true, html: false);
            /*
            if (!stb_restrictAircraftByRank && !stb_restrictAircraftByKills)
            {
                Stb_Message(new Player[] { player }, "This server has no aircraft restriction by rank or ace level.  You have access to all available aircraft.", null);
                return;
            }

            if (stb_restrictAircraftByRank)
            {
                string message1 = "Currently unlocked aircraft for your rank, " + stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForRank(player);
                Stb_Message(new Player[] { player }, message1, null);
            }

            

            if (stb_restrictAircraftByKills)
            Timeout(2.0, () =>
            {
                string message2 = stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForAce(player);
                if (message2 == "") message2 = "(none)";
                Stb_Message(new Player[] { player }, "Unlocked aircraft for your number of kills: " + message2, null);
            });
            */

        }
        else if (msg.StartsWith("<nextac") || msg.StartsWith("<nex")) //aircraft available @ next promotion
        {
            Stb_Message(new Player[] { player }, ">>>Please use Tab-4 menu for in-game career info", null);
            Display_AircraftAvailable_ByName(player, nextAC: true, display: true, html: false);

            /*
            if (!stb_restrictAircraftByRank && !stb_restrictAircraftByKills)
            {
                Stb_Message(new Player[] { player }, "This server has no aircraft restriction by rank or ace level.  You have access to all available aircraft.", null);
                return;
            }

            string message3 = "Aircraft that will be unlocked when you are promoted or increase ace level: " + stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForNextRank(player);

            Stb_Message(new Player[] { player }, message3, null);



            Timeout(2.0, () =>
            {
                string message4 = stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForNextAce(player);
                if (message4 != "") Stb_Message(new Player[] { player }, "Aircraft you can unlock via more kills: " + message4, null);
            });
            */
        }
        else if (stb_PlayerTimeoutWhenKilled && stb_PlayerTimeoutWhenKilled_OverrideAllowed && msg.StartsWith("<override"))
        {
            string message4 = "Override: Flight restriction removed - you may proceed";
            Stb_Message(new Player[] { player }, message4, null);
            Timeout(2, () =>
            {
                string message5 = "Override: Note that any stats, damage, and kills will accrue to your old career for the duration of the flight restriction time.";
                Stb_Message(new Player[] { player }, message5, null);
            });
            Timeout(4, () =>
            {
                string message6 = "Override: The override period lasts for 5 minutes. If you need another override after that you'll have to make a second request.";
                Stb_Message(new Player[] { player }, message6, null);
            });
            stb_StatRecorder.StbSr_PlayerTimedOutDueToDeath_override(player.Name());

        } 
        else if (msg.StartsWith("<air"))
        {
            Timeout(0.2, () =>
            {
                string msg6 = "Checking your position via radar to find nearest friendly airport . . . ";
                Stb_Message(new Player[] { player }, msg6, null);
            });

            Timeout(12, () =>
            {

                //double d = Stb_distanceToNearestAirport(aircraft as AiActor);

                AiAirport ap = Stb_nearestAirport(aircraft as AiActor, player.Army());
                AiActor a = ap as AiActor;

                Point3d aPos = a.Pos();
                double distanceToAirport_m = aircraft.Pos().distance(ref aPos);
                double bearing_deg = Calcs.CalculateGradientAngle(aircraft.Pos(), a.Pos());
                double bearing_deg10 = Calcs.GetDegreesIn10Step(bearing_deg);
                string dis_string = (distanceToAirport_m / 1000).ToString("N0") + " km ";
                if (player.Army() == 1) dis_string = (Calcs.meters2miles(distanceToAirport_m)).ToString("N0") + " mi ";

                string message6 = dis_string + bearing_deg10.ToString("N0") + "° to the nearest friendly airport";
                if (distanceToAirport_m < 2500) message6 = "You are AT the nearest friendly airport";
                if (distanceToAirport_m > 100000000) message6 = "Nearest friendly airport not found";                                
                Stb_Message(new Player[] { player }, message6, null);

            });

        }
        else if (msg.StartsWith("<ter") )
        {
                if (player.Army() != null && aircraft != null)
                {

                    Timeout(0.2, () =>
                    {                        
                        string msg6 = "Checking your position via radar . . . ";                        
                        Stb_Message(new Player[] { player }, msg6, null);
                    });

                Timeout(12, () =>
                    {
                        int terr= GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y);
                        string msg6 = "You are in ENEMY territory";
                        if (terr == 00) msg6 = "You are in NEUTRAL territory";
                        if (player.Army() == terr ) msg6 = "You are in FRIENDLY territory";                                        
                        Stb_Message(new Player[] { player }, msg6, null);
                     });

                }
        }
        else if (msg.StartsWith("<rrhelp2")) // && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "<rrHELP: Extended command example: <rr 50 d: 0.08 n: MyNick s: MySkin.jpg w: 1 1 1 2 ", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: d: 0.08 - sets bomb delay to 0.08. Check CloD loadout page for valid values.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: w: 1 1 1 2 - sets weapon options & bomb selection per aircraft as on CloD Loadout Page 'Weapon Sets' or corresponding 'weapons' entry in user.ini", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: All <rr bombers come with reasonable default loadout. You can force a different bomb loadout with the w: parameter.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: n: MyNick - sets a/c 'Serial Number' as on CloD loadout page, which some set to a nickname or user name.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: s: MySkin.jpg - skin filename; this file must reside in proper subdirectory of your CloD 1C docs /PaintSchemes/Skins (EXPERIMENTAL FEATURE)", new object[] { });

            Stb_Message(new Player[] { player }, "<rrHELP: <rr auto-transfers your convergence settings, regiment, tailnumber, and many other settings from old a/c to new.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: The extended <rr commands d: n: s: w: allow you to set a few options that can't be auto-transferred.", new object[] { });
        }
        else if (msg.StartsWith("<rrhelp1")) // && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "<rrHELP: <rrf forces 'fighter mode' which means NO BOMBS will be loaded. ", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: <rr always loads bombs on a/c that can carry them, so <rrf is useful for 109E-1B, Ju-87, etc if you don't want the bombs loaded.", new object[] { });

            Stb_Message(new Player[] { player }, "<rrHELP: <rrb forces 'bomber mode' which means BOMBS WILL be loaded if possible for the a/c.  Useful for 109E-1B, Ju-87, etc", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: Combine commands: <rrf50 - <rrb25 - rrf100 - etc.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: <rrhelp2 for more . . . ", new object[] { });

        }
        else if (msg.StartsWith("<rrhelp")) // && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            Stb_Message(new Player[] { player }, "<rrHELP: You can <rr at any friendly airfield, active or inactive.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: You can <rr ANY aircraft, bomber or fighter. You get a new a/c so it is OK if your a/c is damaged.", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: <rr30 <rr50 <rr100 etc give fuel load 30%, 50%, 100% etc. Default for <rr is 100% for fighters and 30% for heavy bombers", new object[] { });
            Stb_Message(new Player[] { player }, "<rrHELP: <rrhelp1 for more . . . ", new object[] { });
        }
        else if (msg.StartsWith("<rr")) // && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            //Quite a few problems to solve, such as transferring the plane's loadout (& other setup?) figuring out why it spawns at that one particular place on the airport,
            //making sure another plane isn't already there before the next one spawns in, making sure the a/c is stopped, undamaged, etc etc etc before spawning.
            //They spawn in with engine on and brake on.
            //You can put the loadout into the .mis file like this:
            /*Weapons 1
             * Belt _Gun03 Gun.Browning303MkII MainBelt 9
             * Belt _Gun06 Gun.Browning303MkII MainBelt 9
             * Belt _Gun00 Gun.Browning303MkII MainBelt 1 9 
             * Belt _Gun01 Gun.Browning303MkII MainBelt 9
             * Belt _Gun07 Gun.Browning303MkII MainBelt 1 9
             * Belt _Gun02 Gun.Browning303MkII MainBelt 9
             * Belt _Gun04 Gun.Browning303MkII MainBelt 9 
             */
            //But I don't know where to GET the current loadout from.

            //OK, here is how to read the loadout per Salmo:
            //https://theairtacticalassaultgroup.com/forum/showthread.php?t=24310&p=258293&viewfull=1#post258293
            /*
             *      AiBirthplace birthplace = somebirthplacereference;
             *      AiAircraft aircraft = someaircraftreference;
             *      System.Collections.BitArray weaponsmask = birthplace.GetWeaponsMask(aircraft.TypedName());
             *      
             *      He also says:
             *      
             *      * Serial Number ......................................... string SerialNumber = aircraft.HullNumber();
             *         -->> Actually this is the "Tail Number" as identified in the CloD loadout screen.  One letter.
             *      * Paint Scheme (ie "Skin") file ......................... Not avalbale via script at runtime
             *      * Noseart left/right (file) ................................ Not avalbale via script at runtime
             *      * Whether or not markings are shown ............ Not avalbale via script at runtime
             *      * Visual Weathering Setting ........................... Not avalbale via script at runtime
             * */


            //Also must despawn/destroy current aircraft. Also must figure out how to transfer hull #, serial #, regiment, skin
            //regiment BoB_LW_KuFlGr_706
            //hullNumber FL
            //serialNumber FLUG
            //loadout 4x250 BlenheimMkI_WingGun Default BlenheimMkI_TurretGun Default BlenheimMkI_CentralBombBay 4x250lbs BlenheimMkI_WingBombBays Empty BlenheimMkI_ExternalBombRack Empty


            //GamePlay.gpPostMissionLoad(stb_Bombers1);            
            //AiAircraft aircraft = player.Place() as AiAircraft;  //this is initialized a few lines earlier now

            if (player.Place() == null || aircraft == null)
            {
                Stb_Message(new Player[] { player }, "You must be in an aircraft to R&R.", new object[] { });
                return;
            }



            double Z_VelocityMach = aircraft.getParameter(part.ParameterTypes.Z_VelocityMach, 0);
            double Z_VelocityIAS = aircraft.getParameter(part.ParameterTypes.Z_VelocityIAS, 0);
            double Z_AltitudeAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
            double distToNearest = Stb_distanceToNearestAirport(aircraft as AiActor);
            bool inFriendlyTerritory = (player.Army() == GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y));
            // double I_EngineRPM = aircraft.getParameter(part.ParameterTypes.I_EngineRPM, -1); //for some reason this doesn't work so omitting it

            //Make sure they are allowed to spawn into a new a/c at this moment            
            //Had Z_VelocityMach < -.006 but for some reason on initial spawn often velocity is -4.6 or so?  Not sure what negative velociy measn in this context anyway?
            if (aircraft == null || Z_AltitudeAGL > 10 || Z_VelocityMach > 0.006 || Z_VelocityMach < -5 || !inFriendlyTerritory || distToNearest > 1750)
            {

                double VelocityMPH = Math.Abs(Z_VelocityMach) * 600;
                string ms = "You must be stopped at or near a friendly airport (active or inactive) before you can request a new aircraft.";
                //if (stb_Debug) ms += " Your current alt: " + Z_AltitudeAGL.ToString("N1") + " Velocity: " + (600 * Z_VelocityMach).ToString("N1") + " IAS: " + Z_VelocityIAS.ToString("N1") + " In friendly territory? " + inFriendlyTerritory.ToString()  + " Nearest friendly airport: " + distToNearest.ToString("N0") + " meters";
                Stb_Message(new Player[] { player }, ms, new object[] { });
                //
                //I_EngineRPM.ToString("N5") + " "

                return;
            }

            Stb_Message(new Player[] { player }, "<rr examples: <rr30 | <rrf25 (forces fighter mode; no bombs) | <rr 50 d: 0.08 n: MyNick s: MySkin.jpg w: 1 1 1 2 | More details: <rrhelp or @ web page", new object[] { });

            Point3d myLoc = new Point3d(0, 0, 0);
            myLoc = player.Place().Pos();
            string acType = "HurricaneMkI_100oct";
            string hullNumber = "3";
            string serialNumber = "001";
            string regiment = "gb02";
            string callsign = "26";
            string delay_sec = "";
            string skin_filename = "";
            string weapons = "";
            string fighterbomber = "";
            //string fuelStr = msg.Replace("<rr", "");
            string fuelStr = "";
            acType = Calcs.GetAircraftType(aircraft);

            //So we can't figure out how to access current serial #, instead make it from their username.  It can be overridden via n: in chat command
            
            serialNumber = player.Name();
            if (serialNumber.Contains("_"))
            {
                string[] serials = serialNumber.Split('_');
                if (serials.Length > 1 && serials[0].Length < 8) serialNumber = serials[1];
            }
            
            //2018-09-24 - the new way, using aircraft.HullNumber(); - we'll see if it works
            //serialNumber = aircraft.HullNumber();
            //hullNumber = aircraft.HullNumber();
            //serialNumber = maddox.game.page.OptionsPlane.textSerialNumber().Text();

            //parse the chat message
            string msgTrim = msg_orig.Replace("<rr", "");
            if (msgTrim.StartsWith("b", true, null) || msgTrim.StartsWith("f", true, null)) //case insensitive match
            {
                fighterbomber = msgTrim[0].ToString().ToLower();
                msgTrim = msgTrim.Substring(1);

            }


            //now we parse the string.  Any section starting with d is the delay, w is weapons string, s is skin filename,
            //and blank/nothing is fuel %
            //int parseL = msgTrim.LastIndexOfAny(new char[] { 'd', 'w', 's' });

            int parseL = Calcs.LastIndexOfAny(msgTrim, new string[] { "d:", "w:", "s:", "n:", "t:" });

            if (msgTrim.Length > 0 && parseL > -1)
            {
                //int d_loc = msgTrim.LastIndexOf('d');
                //int w_loc = msgTrim.LastIndexOf('w');
                List<string> sections = new List<string>();
                //int l = msgTrim.LastIndexOfAny(new char[] { 'd', 'w', 's' });
                while (parseL > -1)
                {
                    sections.Add(msgTrim.Substring(parseL));
                    msgTrim = msgTrim.Substring(0, parseL);
                    parseL = Calcs.LastIndexOfAny(msgTrim, new string[] { "d:", "w:", "s:", "n:", "t:" });
                }
                sections.Add(msgTrim);

                /*int first_loc = d_loc;
                int sec_loc = w_loc;
                if (w_loc<d_loc) { first_loc = w_loc;  sec_loc=d_loc};
                if (sec_loc > -1) { last_sec = msgTrim.Substring(sec_loc); msgTrim = msgTrim.Substring(0, sec_loc); }
                if (first_loc > -1) { mid_sec = msgTrim.Substring(first_loc); msgTrim = msgTrim.Substring(0, first_loc); }
                first_sec = msgTrim;
                string[] sections = new string[] { first_sec, mid_sec, last_sec }; */
                foreach (string str in sections)
                {
                    string strLower = str.ToLower();
                    if (strLower.StartsWith("d:")) delay_sec = str.Substring(2).Trim();
                    else if (strLower.StartsWith("w:")) weapons = str.Substring(2).Trim();
                    else if (strLower.StartsWith("s:")) skin_filename = str.Substring(2).Trim();
                    else if (strLower.StartsWith("n:")) serialNumber = str.Substring(2).Trim();
                    else if (strLower.StartsWith("t:") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix) acType = str.Substring(2).Trim();
                    else fuelStr = str.Trim();
                }
            }
            else if (msgTrim.Length > 0) fuelStr = msgTrim.Trim();


            //if (stb_Debug) Console.WriteLine("<RR PARSE RESULTS: fb: " + fighterbomber + " f: " + fuelStr + " d: " + delay_sec + " w: " + weapons + " s: " + skin_filename);




            hullNumber = aircraft.HullNumber();
            regiment = aircraft.Regiment().name();
            callsign = aircraft.CallSignNumber().ToString();
            


            //ISectionFile f = GamePlay.gpCreateSectionFile();




            //private string Stb_LoadSubAircraft(Point3d loc, string type = "SpitfireMkIa_100oct", string callsign = "26", string hullNumber="3", string serialNumber="001", string regiment="gb02", int fuel = 100, string weapons= "1", double velocity=0 )

            //if (stb_Debug) Stb_Message(new Player[] { player }, "Spawning new a/c at " + myLoc.x.ToString("F0") + " " + myLoc.y.ToString("F0") + " " + acType + " " + aircraft.TypedName() +
                 // " hn:" + hullNumber + " r:" + regiment + " cS:" + callsign + " sN:" + serialNumber, new object[] { });

            string newACActorName = Stb_LoadSubAircraft(loc: myLoc, type: acType, callsign: callsign, hullNumber: hullNumber, fuelStr: fuelStr, serialNumber: serialNumber, regiment: regiment, weapons: weapons, velocity: 0, delay_sec: delay_sec, skin_filename: skin_filename, fighterbomber: fighterbomber);





            stb_ContinueMissionRecorder.StbCmr_SetIsForcedPlaceMove(player.Name());
            Point3d ac1loc = (aircraft as AiActor).Pos();
            Stb_RemoveAllPlayersFromAircraft(aircraft, 0.1);


            //put the player in the new a/c
            Timeout(3.05, () =>
            //Timeout(0.15, () =>
            {
                AiActor newActor = GamePlay.gpActorByName(newACActorName);



                Point3d ac2loc = newActor.Pos();
                bool spawnInFriendlyTerritory = (player.Army() == GamePlay.gpFrontArmy(ac2loc.x, ac2loc.y));
                double distanceToSpawn_m = ac2loc.distance(ref ac1loc);
                if (!spawnInFriendlyTerritory || distanceToSpawn_m > 1800)
                {
                    if (distanceToSpawn_m > 1800) Timeout(0.5, () => { Stb_Message(new Player[] { player }, "Sorry, you were too far from the nearest friendly airfield for R&R (" + distanceToSpawn_m.ToString("N0") + " meters)", new object[] { }); });
                    else if (!spawnInFriendlyTerritory) Timeout(0.5, () => { Stb_Message(new Player[] { player }, "Sorry, you can't R&R at an enemy airfield.", new object[] { }); });
                    (newActor as AiCart).Destroy();
                    return;
                }

                Timeout(2, () => { Stb_Message(new Player[] { player }, "Transferring you " + distanceToSpawn_m.ToString("N0") + " meters to a new " + acType + ". Note that your Parking Brake is SET - just tap your brakes once to release it.", new object[] { }); });

                stb_ContinueMissionRecorder.StbCmr_SetIsForcedPlaceMove(player.Name());
                player.PlaceEnter(newActor, 0);  //ToDO: need to test whether the placeenter was successful, somehow.
                                                 //Also, what about moving any OTHER people in other places in the A/C to the new A/C
                stb_ContinueMissionRecorder.StbCmr_SetIsForcedPlaceMove(player.Name());
                player.PlaceLeave(0);
                Timeout(0.4, () =>
                {
                    stb_ContinueMissionRecorder.StbCmr_SetIsForcedPlaceMove(player.Name());
                    player.PlaceEnter(newActor, 0);

                
                    stb_ContinueMissionRecorder.StbCmr_ClearIsForcedPlaceMove(player.Name());

                
                }); //can't wait too long or some of our other .cs files will destroy the plane to prevent ai takeover. 0.5 sec to destroy in ..MAIN.cs, so must be less than that
                  //Changing this bec in Clod 4.5 the plane warms up real fast if AI are in control.  So leaving it for a second longer gives a more warmed up plane, in theory.
                  
            });

        }
        else if (msg.StartsWith("<put") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {

            string newACActorName = msg_orig.Replace("<put ", "");
            gpLogServerAndLog(new Player[] { player }, "Put: Putting you into " + newACActorName, new object[] { });
            AiActor newActor = GamePlay.gpActorByName(newACActorName);
            player.PlaceEnter(newActor, 0);
        }
        else if (msg.StartsWith("<land") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {

            OnAircraftLanded(player.Place() as AiActor, player, player.Place() as AiAircraft, 0);
        }
        else if (msg.StartsWith("<admin") && player.Name().Substring(0, stb_AdminPlayernamePrefix.Length) == stb_AdminPlayernamePrefix)
        {
            double to = 2; //make sure this comes AFTER the main mission <admin listing
            Timeout(to, () => { gpLogServerAndLog(new Player[] { player }, "<training <untraining remove/return special server features (rank, death timeout, stats) <deban <reban temporarily turn off/on the pilot ban after death <lista or <lista 13 Get a list of current a/c in game OR enter the specified a/c", new object[] { }); });


        }
        else if (msg.StartsWith("<help") || msg.StartsWith("<HELP"))// || msg.StartsWith("<"))
        {
            double to = 1; //make sure this comes AFTER the main mission <help listing, or WAY after if it is responding to the "<"
            if (!msg.StartsWith("<help")) to = 5;

            string msg41 = "<rr Get new a/c; <rrhelp Help for <rr; Use Tab-4 menu for various stats summaries";
            if (stb_restrictAircraftByRank)
            {
                msg41 += "; <ac check available aircraft at your current rank; <nex list aircraft you can unlock at next promotion";
            }

            Timeout(to, () => { gpLogServerAndLog(new Player[] { player }, msg41, new object[] { }); });
            //GamePlay.gp(, from);
        }
    }


    /***************************************************************
     * PENALTIES FOR BOMBS DROPPED ON CIVILIAN AREAS
     *
     * Handle bombs dropping on civilian areas
     * We use two special static vehicles to mark civilian areas, then here we figure out if bombs have dropped close to them.
     *   * Regent II Bus (static vehicle) defines a circle of 500 meters radius that is "civilian territory".  Note that the 500m radius is implicit in the GamePlay.gpGroundStationarys(pos.x, pos.y, 500) above
     *   * Maddox Games TA Sports Car (static vehicle)  defines a circle of 250 meters radius that is "civilian territory"
     * Then we assess various penalties (negative kill points, finally kicking out of a/c) if bombs are dropped on civilian areas
     * 
     * TODO: Rather than actually subtracting points from various kill totals, it might be smarter to keep a separate tally of various
     * types of penalty points, them just subtract them out when calculating things like ace, rank, whatever.  Thus we would know how many actual kills plus how many penalties received
     * rather than just having them lumped together in an undifferentiated way.
     ****************************************************************/
    
    Dictionary<string, Tuple<int, DateTime>> ot_CivilianBombings = new Dictionary<string, Tuple<int, DateTime>>();

    public int ot_GetCivilianBombings(string name)
    {
        Tuple<int, DateTime> temp;
        int infract = 0; //This is the default & will be recorded if this player is not found in the dictionary at all
        if (ot_CivilianBombings.TryGetValue(name, out temp))
        {
            infract = temp.Item1;  //Still within the delay time, so we leave the infraction count unchanged
                                   //We also leave the time since last infraction unchanged (rather than changing it to 'now')
        }

        return infract;

    }

    //Records a civilian bombing infraction for a player, but only if this is a new infraction happening
    //after a delay time specified below, and returns the number of infractions
    //recorded for that player so far
    //The idea is that one 'salvo' of bombs dropped rather close together in time only counts as one
    //'infraction' for that player.
    public int ot_IncCivilianBombings(string name)
    {

        Tuple<int, DateTime> temp;
        int infract = 1; //This is the default & will be recorded if this player is not found in the dictionary at all
        DateTime now = DateTime.Now;
        DateTime newtime = now;

        if (ot_CivilianBombings.TryGetValue(name, out temp))
        {

            double delay_sec = 5; //time to wait before a new infraction can be recorded
            newtime = temp.Item2;  //We retain the time of last infraction unless we determine below that this is a new infraction


            if (DateTime.Compare(now, temp.Item2.AddSeconds(delay_sec)) > 0)  //Is current time later than the most recent recorded infraction plus delay_sec
            {

                infract = temp.Item1 + 1; //Longer than the delay time, so we increment the infraction count
                newtime = now; //New infraction, so we start counting time from this moment again
            }
            else
            {
                infract = temp.Item1;  //Still within the delay time, so we leave the infraction count unchanged
                //We also leave the time since last infraction unchanged (rather than changing it to 'now')
            }

        }

        //GamePlay.gpLogServer(null, "Infractions for " + name + ": " + infract.ToString() + " " + newtime.ToString("O"), new object[] { }); //just for testing

        ot_CivilianBombings[name] = new Tuple<int, DateTime>(infract, newtime);
        return infract;

    }


    //Hand out penalties for bombing civilian areas
    //Basically no penalty for first salvo, after that there is a penalty of -1 kill points per bomb dropped on civilian areas
    //After 4 infractions in one session the penalty doubles and after 8 infractions in one session it doubles again
    //These negative kill points will have a bad effect on the player's rank, ace level, and also on the entire army's 
    //point level as recorded by <obj.  Kill point totals can actually go negative for bad infractions.
    //Also every 8 infractions the player is kicked out of the plane (usually results in player death/loss of career)
    public void ot_HandleCivilianBombings(Player player, Point3d pos, AiDamageInitiator initiator, double mass_kg)
    {
        if (player == null) return;

        int army = player.Army();
        string playername = player.Name();
        int prev_infractions = ot_GetCivilianBombings(playername);
        int infractions = ot_IncCivilianBombings(playername);


        /* string firetype = "BuildingFireSmall";
        if (mass_kg > 200) firetype = "BuildingFireBig"; //500lb bomb or larger */
        string firetype = "BigSitySmoke_1";
        loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, 1200, stb_FullPath);

        double score = 0.5; //50 lb bomb; 22kg
        if (mass_kg > 0) score = 0.06303 * Math.Pow(mass_kg, 0.67);


        //GamePlay.gpLogServer(null, "Infraction/penalties for " + playername + ": " + infractions.ToString(), new object[] { });
        if (infractions == 1) //First infraction
        {
            if (prev_infractions != infractions) //only display the message for each new 'salvo' that was dropped
            {
                GamePlay.gpLogServer(null, "A civilian area has been bombed by " + playername + "! Penalties to you & your army.", new object[] { player });
                GamePlay.gpHUDLogCenter("Civilian area bombed by " + playername + "! Severe repercussions!!");
                stb_RecordStatsOnActorDead(initiator, 4, -.8, 1, AiDamageToolType.Ordance);//each bomb dropped on a civi area gives -1 kill points, -100% in TWC kill points, type 4 (ground kill), ordinance type 2 = bombs
                GamePlay.gpLogServer(new Player[] { player }, "Bombed civilian area: " + (-1 * score).ToString("0.0") + " point penalty", new object[] { });
            }
        }
        else if (infractions > 0 && infractions > 8) //This statement will be called for 4, 8, 12, 16, etc infractions.  So you can add additional penalties at each 4 infractions. 
        {
            if (prev_infractions != infractions) //only display the message for each new 'salvo' that was dropped
            {
                GamePlay.gpLogServer(null, "A civilian area has been bombed repeatedly by " + playername + " despite sever warnings and penalties! You and your army have incurred very serious penalties.", new object[] { });
                GamePlay.gpHUDLogCenter("Civilian area bombed repeatedly by " + playername + " despite many warnings! Very serious penalties for player & army");
            }
            //Do some actions penalize the army that has too many infractions by adding more objectives to their required amount or awarding an objective to their opponent (Own goal) or whatever you like
            //? Nothing here yet, needs to be added ?
            stb_RecordStatsOnActorDead(initiator, 4, -4 * score, 1, AiDamageToolType.Ordance);//each bomb dropped on a civi area gives -4 (!) kill points, -100% in TWC kill points, type 4 (ground kill), ordinance type 2 = bombs
            GamePlay.gpLogServer(new Player[] { player }, "Bombed civilian area: " + ( -4 * score ).ToString("0.0") + " point penalty", new object[] { });

        }
        else if (infractions > 0 && infractions > 4) //This statement will be called for 4, 8, 12, 16, etc infractions.  So you can add additional penalties at each 4 infractions. 
        {
            if (prev_infractions != infractions) //only display the message for each new 'salvo' that was dropped
            {
                GamePlay.gpLogServer(null, "A civilian area has been bombed repeatedly by " + playername + " despite warnings! You and your army have incurred very serious penalties.", new object[] { });
                GamePlay.gpHUDLogCenter("Civilian area bombed repeatedly by " + playername + "! Serious penalties for player & army");
                
            }
            //Do some actions penalize the army that has too many infractions by adding more objectives to their required amount or awarding an objective to their opponent (Own goal) or whatever you like
            //? Nothing here yet, needs to be added ?

            stb_RecordStatsOnActorDead(initiator, 4, -2, 1, AiDamageToolType.Ordance);//each bomb dropped on a civi area gives -2 kill points, -100% in TWC kill points, type 4 (ground kill), ordinance type 2 = bombs
            GamePlay.gpLogServer(new Player[] { player }, "Bombed civilian area: " + (-2 * score).ToString("0.0") + " point penalty", new object[] { });

        }

        else //another infraction, more than 1 but less than 4
        {
            if (prev_infractions != infractions) //only display the message for each new 'salvo' that was dropped
            {
                GamePlay.gpLogServer(null, "A civilian area has been bombed repeatedly by " + playername + "! Penalties become more severe.", new object[] { });
                GamePlay.gpHUDLogCenter("Civilian area bombed by " + playername + "! Player & army penalized");
            }
            //Do some actions to somewhat penalize the army.
            //? Nothing here yet, needs to be added ?

            stb_RecordStatsOnActorDead(initiator, 4, -1, 1, AiDamageToolType.Ordance);//each bomb dropped on a civi area gives -1 kill points, -100% in TWC kill points, type 4 (ground kill), ordinance type 2 = bombs
            GamePlay.gpLogServer(new Player[] { player } , "Bombed civilian area: " + (-1 * score).ToString("0.0") + " point penalty", new object[] { });
        }


        //After every new infraction we will actually FORCE the demotion that they have probably well earned by this point.
        if (prev_infractions != infractions && infractions > 0)
        {
            Timeout(2, () =>
            {
                stb_StatRecorder.StbSr_Calc_AceAndRank_ByName(player.Name(), player.Place() as AiActor, false, player, true);
            });
        }
        //After every EIGHT infractions we will kick them out of the plane
        if (prev_infractions != infractions && infractions > 0 && infractions % 8 == 0)
        {
            Timeout(2, () =>
            {
                GamePlay.gpLogServer(null, "Because of repeated bombing of civilian areas, and insubordination in disobeying orders to cease such bombing, "+ playername 
                    + "'s co-pilot has ejected " + playername + " from the aircraft and assumed command.", new object[] { });

                string peHud_message = "Removed from command - repeated bombing of civilian areas & insubordination";
                GamePlay.gpHUDLogCenter(new Player[] { player }, peHud_message, null);
                Timeout(10.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, peHud_message, null); });
                Timeout(20.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, peHud_message, null); });              
                if (player.Place() != null && player.Place() as AiAircraft !=null ) Stb_RemovePlayerFromAircraftandDestroy(player.Place() as AiAircraft, player, 1.0, 3.0);

            });
        }
    }

    /***************************************************************
     * 
     * Methods for Export
     * 
     * Methods that will be included in the CloDMissionCommunicator.dll so that they can be called directly in the -main.cs.
     *  
     * CloDMissionCommunicator.dll
     * 
     * **************************************************************/

    public void Display_AceAndRank_ByName(Player player)
    {
        stb_StatRecorder.StbSr_Display_AceAndRank_ByName(player.Name(), player.Place() as AiActor, player);
    }

    //     string Display_AircraftAvailable_ByName(Player player, bool nextAC = false, bool display = true, bool html = false); //<ac
    public string Display_AircraftAvailable_ByName(Player player, bool nextAC = false, bool display = true, bool html = false)
    {
        return stb_StatRecorder.StbSr_Display_AircraftAvailable_ByName(player, nextAC, display, html);
    }

    public void Display_SessionStats(Player player)
    {
        stb_StatRecorder.StbSr_Display_SessionStats(player);
    }

    public string Display_SessionStatsAll(Player player, int side = 0, bool display = true, bool html = true)
    {
        //Player player = null, int side = 0, bool display=true
        return stb_StatRecorder.StbSr_Display_SessionStatsAll(player, side, display, html); //display "Netstats" summary of current session stats for all players to this player
    }

    //if player sent, displays message to the player, if player==null just return the string (html formatted with <br>)
    public string Display_SessionStatsTeam(Player player)
    {
        return stb_StatRecorder.StbSr_Display_SessionStatsTeam(player);
    }
        

    /***************************************************************
     * Handle Area Bombings
     *
     * "Live" areas for bombing, such as industrial or military areas.
     * 
     * We set up jerrycans to designate the bombable areas.  These are detected
     * below in OnBombExplosion, then sent here to be handled.
     * 
     *    //The JerryCan_GER1_1 (static - environment - jerrycan) covers a radius of 71 meters which is just enough to fill a 100 meter square (seen in FMB at full zoom) to all corners if placed in the center of the 100m square.
     *    // JerryCan_GER1_2 covers 141m radius (covers 4 100m squares to the corners if placed in the center)
     *    // JerryCan_GER1_3 covers 282m radius (covers 16 100m squares to the corners if placed in the center)
     *    // JerryCan_GER1_5 covers 1410m radius (a 1km square to the corner if placed in the center)
     *    
     *    // In MISSION BUILDER these show up as Environment Jerrycan, Environment Jerrycan x2, Environment Jerrycan x3, and Environment Jerrycan x5.
     *    
     *    //Do NOT use JerryCan_UK1_4-Gallon_X or any other jerry can - they will NOT register hits.
     *    //In MISSION BUILDER these jerry cans that DO NOT REGISTER show up as Jerrycan 2-gallonxX or Jerrycan 4-gallonxX.
     *    
     *    Bombers receive points for bombing these areas (calculated below, depends on size of bomb etc)
     *    Also each bomb is marked with a smoke plume.
     *    
     *    TODO: Make sure they are bombing enemy territory/point deductions for bombing friendly
     * 
     ****************************************************************/


    public void ot_HandleAreaBombings(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, Player player, int isEnemy = 1, string targetType = "Ground Area", double  multiplier = 1, double aircraftCorrection = 1, bool crater = false ) //IsEnemy 0=friendly, 1 enemy, 2 neutral, crater = true places a crater instead of the smoke, useful for roads, railroads, etc
    {
        if (player == null) return;  //This routine only scores points so there is no point in doing it unless we have a live player bombing
        string playername = "AI";
        if (player != null) playername = player.Name();
        //GamePlay.gpLogServer(new Player[] { player }, "Area bombing by " + playername + " " + mass_kg.ToString("n0") + "kg at " + pos.x.ToString("n0") + " " + pos.y.ToString("n0"), new object[] { });

        //So, the Sadovsky formula is a way of estimating the effect of a blast wave from an explosion. https://www.metabunk.org/attachments/blast-effect-calculation-1-pdf.2578/
        //Simplifying slightly, it turns out that the radius of at least partial destruction/partial collapse of buildings is:
        // 50 lb - 30m; 100 lb - 40 m; 250 lb - 54 m; 500 lb - 67 m; 100 lb - 85 m; etc.
        //Turning this radius to an 'area of destruction' (pi * r^2) gives us an "area of destruction factor" for that size bomb.  
        //Since we are scoring the amount of destruction in e.g. an industrialized area, counting the destruction points as area (square footage, square meters, whatever) is reasonable.
        //Scaling our points in proportion to this "area of destruction factor" so that a 50 lb pound bomb gives 0.5 points, then we see that destruction increases with size, but lower than linearly.
        //So if a 50 lb bomb gives 0.5 points, a 100 lb bomb gives 0.72 points; 250 lb 1.41 points; 500 lb 2.33 points, 1000 lb 4.0 points, 2000 lb 6.48 points, etc
        //The formula below is somewhat simplified from this but approximates it pretty closely and gives a reasonable value for any mass_kg

        //double scoreBase = 0.06303;
        double scoreBase = 0.031515; //halving the score we were giving at first, since the point totals seem to be coming up quite high in comparison with fighter kills
        //if (blenheim) scoreBase *= 4; //8X score for Blenheims since their bomb load is pathetic  (Blenheim = 4X 250 lb bombs, HE111 = 32X 100kg bombs  -> 4*8 = 32
        scoreBase *= aircraftCorrection; //correcting for lower tonnage carried by Blenheim, HE111, etc
        scoreBase *= multiplier;  //We can adjust score for various type of terrain or target areas etc by sending a different multipler


        
        if (mass_kg <= 0) mass_kg = 22;  //50 lb bomb; 22kg
        double score = scoreBase * Math.Pow (mass_kg, 0.67);

        /* Another way to reach the same end- probably quicker but less flexible & doesn't interpolate:
         * 
         * //Default is 0.5 points for ie 50 lb bomb
         * if (mass_kg > 45) score = 0.722; //100 lb  (calcs assume radius of partial/serious building destruction per Sadovsky formula, dP > 0.10, explosion on surface of ground, and that 50% of bomb weight is TNT)
        if (mass_kg > 110) score = 1.41; //250 
        if (mass_kg > 220) score = 2.33; //500
        if (mass_kg > 440) score = 3.70; //1000
        if (mass_kg > 880) score = 5.92; //2000
        if (mass_kg > 1760) score = 9.33 ; //4000
        
         */

        if (isEnemy == 0 || isEnemy == 2)
        {
            score = -score;  //Bombing on friendly/neutral territory earns you a NEGATIVE score
                             //but, still helps destroy the objects/area (for your enemies) as usual
            GamePlay.gpLogServer(null, player.Name() + " has bombed a friendly or neutral zone. Serious repercussions for player AND team.", new object[] { });

        }

        //TODO: Should collect these over a 5-10 sec time frame & print summary, bec. many bombs often hit very close to same time
        ////TESTING: turning off ground target  hit messages to see if that helps our warping problem 9/28/2018
        
        Timeout(0.4 + stb_random.NextDouble() * 25, () =>
        {
            GamePlay.gpLogServer(new Player[] { player }, targetType + " hit: " + mass_kg.ToString("n0") + "kg " + score.ToString("n1") + " points ", new object[] { });
        }); //+ pos.x.ToString("n0") + " " + pos.y.ToString("n0")
        

        stb_RecordStatsOnActorDead(initiator, 4, score, 1, initiator.Tool.Type);  //So they have dropped a bomb on an active industrial area or area bombing target they get a point.
        //TODO: More/less points depending on bomb tonnage.

        //TF_Extensions.TF_GamePlay.Effect smoke = TF_Extensions.TF_GamePlay.Effect.SmokeSmall;
        // TF_Extensions.TF_GamePlay.gpCreateEffect(GamePlay, smoke, pos.x, pos.y, pos.z, 1200);
        string firetype = "BuildingFireSmall";
        //if (mass_kg > 200) firetype = "BigSitySmoke_1"; //500lb bomb or larger
        if (mass_kg > 200) firetype = "Smoke1"; //500lb bomb or larger


        if (crater)
        {
            firetype = "BombCrater_firmSoil_mediumkg";
            if (mass_kg > 100) firetype = "BombCrater_firmSoil_largekg"; //250lb bomb or larger
            if (mass_kg > 200) firetype = "BombCrater_firmSoil_EXlargekg"; //500lb bomb or larger.  EXLarge is actually 3 large craters slightly offset to make 1 bigger crater
        }

        loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, 20, stb_FullPath);
        //todo: finer grained bigger/smaller fire depending on bomb tonnage

        //BigSitySmoke_0 BigSitySmoke_1 BuildingFireBig BuildingFireSmall Smoke1 Smoke2


    }

  /************************************************************
  * 
  * handle airport bombing
  * most credit/script idea for airport bombing & destruction goes to reddog/Storm of War
  * 
  * We give credit (points) for any bomb that hits within the radius of an airfield.
  * Also, these bomb hits are marked with a plume of smoke and additionally a bomb crater is added that is dangerous/will kill aircraft taxiing on the ground
  * 
  * Craters are different sizes, depending on tonnage of bomb dropped.  Also, craters will be repaired, taking a shorter time for smaller craters & a longer time for bigger craters
  * Additionally, the more craters dropped on an airport the longer it will take to get to the next crater  & repair it.
  * Also, if a threshold of tonnage (counted as points, which are proportional to damage done) is reached, the airport is put out of commission by severely cratering it
  * 
  * //Version for -stats.cs//
  *************************************************************/

    public Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>> AirfieldTargets = new Dictionary<AiAirport, Tuple<bool, string, double, double, DateTime, double, Point3d>>();
    //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius, Point3d airfield center (position)
    //TODO: it would nice to have a struct or something to hold this instead of a tuple . . . 

    public void SetAirfieldTargets()
    {
        foreach (AiAirport ap in GamePlay.gpAirports()) //Loop through all airfields in the game
        {

            //We're just going to add ALL airfields as targets, but then make sure there are no duplicates (bec. built-in & .mis-added airports sometimes overlap).

            //It's going to take blue pilots more points/bombs to knock out an airfield, vs Red (Blenheims very limited as far as the # of bombs they can carry)

            ////Use this for TACTICAL SERVER (where Reds only have Blenheims)
            //UPDATE 2017/11/06: We don't need this adjustment bec. we have adjusted the points received
            //so that blenheims receive relatively more & the blue bombers relatively less.  So this 
            //should handle the discrepancy between the sides with no further adjustment necessary
            //int pointstoknockout = 30;
            //if (ap.Army() != null && ap.Army() == 1) pointstoknockout = 65;

            ////Use this for MISSION SERVER  && TACTICAL SERVER 
            int pointstoknockout = 65;  //This is about two HE111 or JU88 loads (or 1 full load & just a little more) and about 4 Blennie loads, but it depends on how accurate the bombs are, and how large

            double radius = ap.FieldR();
            Point3d center = ap.Pos();


            //GamePlay.gpAirports() includes both built-in airports and any new airports we have added in our .mis files. This results in duplication since
            //most .mis airports are placed on top of an existing built-in airport. We check whether this airport has already been added & skip adding it if so.
            Point3d pos = ap.Pos();
            bool add = true;
            foreach (AiAirport apk in AirfieldTargets.Keys)//Loop through the targets
            {
                if (apk != null & apk.Pos().distance(ref pos) <= apk.FieldR())
                {
                    //AirfieldTargets[apk].Item3
                    add = false; //
                    if (apk.FieldR() != null && apk.FieldR() > 1) radius = apk.FieldR(); //The field radius set in the .mis file becomes operative if it exists & is reasonable
                    center = apk.Pos();  //We use the position of the airport set i nthe .mis file for the center, if it exists - thus we can change/move the center position as we wish
                    break;
                }
            }

            //We'll get the NAME of the airport from the birthplace/spawn point declare in a .mis file, if it exists

            string apName = ap.Name();
            foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())
            {
                if (bp != null & bp.Pos().distance(ref pos) <= ap.FieldR())
                {
                    if (bp.Name() != null && !(bp.Name().ToUpper().Contains("BIRTHPLACE"))) apName = bp.Name();  //We will use the spawn point/birthplace name UNLESS it is just "BirthPlace0" or whatever
                    break;
                }
            }


            if (add) AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, apName, pointstoknockout, 0, DateTime.Now, radius, center)); //Adds airfield to dictionary, requires approx 2 loads of 32 X 50lb bombs of bombs to knock out.
                                                                                                                                                                   //Tuple is: bool airfield disabled, string name, double pointstoknockout, double damage point total, DateTime time of last damage hit, double airfield radius
                                                                                                                                                                   //if you want to add only some airfields as targets, use something like: if (ap.Name().Contains("Manston")) { }

        }
        GamePlay.gpLogServer(null, "SetAirfieldTargets initialized.", null);
    }

    public void ListAirfieldTargetDamage(Player player = null, int army = -1, bool all = false)
    {
        int count = 0;
        foreach (AiAirport ap in AirfieldTargets.Keys)
        {

            double PointsTaken = AirfieldTargets[ap].Item4;
            bool disabled = AirfieldTargets[ap].Item1;

            if (!all && PointsTaken == 0 && !disabled) continue; //we'll list only airports damaged or disabled, skipping those with no damage at all, unless called with all=true
            if (army != -1 & army != ap.Army()) continue; //List only the army requested, skipping the others.  army = -1 means list both/all armies

            count++;
            double PointsToKnockOut = AirfieldTargets[ap].Item3;
            string Mission = AirfieldTargets[ap].Item2;
            DateTime lastBombHit = AirfieldTargets[ap].Item5;

            double percent = 0;
            if (PointsToKnockOut > 0)
            {
                percent = PointsTaken / PointsToKnockOut;
            }

            double timereduction = 0;
            if (percent > 0)
            {
                timereduction = (DateTime.Now - lastBombHit).TotalSeconds;
            }

            double timetofix = PointsTaken * 20 * 60 - timereduction; //50 lb bomb scores 0.5 so will take 10 minutes to repair.  Larger bombs will take longer; 250 lb about 1.4 points so 28 minutes to repeari
                                                                      //But . . . it is ADDITIVE. So the first 50 lb bomb takes 10 minutes, the 2nd another 10, the 3rd another 10, and so on on.  So if you drop 32 50 bl bombs it will take 320 minutes before the 32nd bomb crater is repaired.
                                                                      //Sources: "A crater from a 500lb bomb could be repaired and resurfaced in about 40 minutes" says one 2nd hand source. That seems about right, depending on methods & surface. https://www.airspacemag.com/multimedia/these-portable-runways-helped-win-war-pacific-180951234/
                                                                      //unfortunately we can repair only the bomb crater; the SMOKE will remain for the entire mission because clod internals don't allow its removal.
                                                                      //TODO: We could keep track of when the last bomb was dropped at each airport and deduct time here depending on how much repair had been done since the last bomb dropped

            if (PointsTaken >= PointsToKnockOut) //airport knocked out
            {
                percent = 1;
                timetofix = 24 * 60 * 60; //24 hours to repair . . . 
            }

            GamePlay.gpLogServer(new Player[] { player }, Mission + " " + (percent * 100).ToString("n0") + "% destroyed; last hit " + (timereduction / 60).ToString("n0") + " minutes ago", new object[] { });


        }
        if (count == 0) GamePlay.gpLogServer(new Player[] { player }, "No airports damaged or destroyed yet", new object[] { });
    }

    //stamps a rectangular pattern of craters over an airfield to disable it
    public void AirfieldDisable(AiAirport ap)

    {
        string apName = ap.Name();
        double radius = ap.FieldR();
        Point3d pos = ap.Pos();

        if (AirfieldTargets.ContainsKey(ap))
        {
            apName = AirfieldTargets[ap].Item2;
            radius = AirfieldTargets[ap].Item6;
            pos = AirfieldTargets[ap].Item7;

        }

        GamePlay.gpHUDLogCenter(null, "Airfield " + apName + " has been disabled");

        //
        /** OK, instead of putting the 'peperoni pizza' pattern of craters to disable an airfield, we're just going to disable the associated spawn point **/
        /*
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";

        string val1 = "Stationary";
        string type = "BombCrater_firmSoil_largekg";
        int count = 0;
        string value = "";



        for (double x = pos.x - radius * .6; x < pos.x + radius * .6; x = x + 50)
        {
            //for (double y = pos.y - radius * .6; y < pos.y + radius * .6; y = y + 100)
            for (double y =  pos.y-Math.Abs(x); y <=pos.y+ Math.Abs(x) +1; y = y + Math.Abs(x))

            {
                //if (Math.Abs(x) != Math.Abs(y)) continue;
                string key = "Static" + count.ToString();
                value = val1 + ".Environment." + type + " nn " + (x - 25 + 50 * stb_random.NextDouble()).ToString("0.00") + " " + (y - 25 + 50 * stb_random.NextDouble()).ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + pos.z.ToString("0.00");
                f.add(sect, key, value);
                count++;

            }
            string key = "Static" + count.ToString();
            value = val1 + ".Environment." + type + " nn " + (x - 25 + 50 * stb_random.NextDouble()).ToString("0.00") + " " + (y - 25 + 50 * stb_random.NextDouble()).ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + pos.z.ToString("0.00");
            f.add(sect, key, value);
            count++;

        }
        f.save(stb_FullPath + "airfielddisable-ISectionFile.txt"); //testing
        GamePlay.gpPostMissionLoad(f);
        //Timeout(stb_random.NextDouble() * 5, () => { GamePlay.gpPostMissionLoad(f); });
        */

        //Now actually destroy the birthplace so ppl can't spawn in
        AiBirthPlace bp = Stb_nearestBirthPlace(ap as AiActor, ap.Army());
        bp.destroy();


    }


    public void LoadAirfieldSpawns()
    {
        return;

        //Ok, this part would need to be placed in -MAIN.cs to work, and also know that mission ID and also we would need to set up special .mis files with each airport.
        //so, maybe we will do all that later, and maybe not
        //For now we are just skipping this part altogether (return;)
        //Instead we just disable the destroyed airport in-game by covering it with the dangerous type of bomb crater

        foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())

        {
            bp.destroy();//Removes all spawnpoints
        }
        GamePlay.gpPostMissionLoad("missions/London Raids/nondestroyable.mis");

        foreach (AiAirport ap in AirfieldTargets.Keys)
        {
            if (AirfieldTargets[ap].Item1)
            {
                //Airfield still active so load mission
                GamePlay.gpPostMissionLoad("missions/YOUR MISSION FOLDER/" + AirfieldTargets[ap].Item2 + ".mis");
            }
        }
    }

    /*************************************************
    *
    * Handle smoke, fire, crater ISection files for area bombing, civilian bombing, airport bombing
    * 
    ********************************************************/

    public void loadSmokeOrFire(double x, double y, double z, string type = "", double duration_s = 300, string path = "", string type2 = "")
    {
        //for testing - disable all craters, smoke, and fire from airfield, civilian, other bombings & targets
        

        //duration_s is how long the item will last before being destroyed (ie, disappearing) BUT (IMPORTANT!) it only works for the bomb craters, NOT for the smoke effects.
        // Choices for string type are: Smoke1, Smoke2, BuildingFireSmall, BuildingFireBig, BigSitySmoke_0, BigSitySmoke_1, 
        // BombCrater_firmSoil_mediumkg, BombCrater_firmSoil_smallkg, BombCrater_firmSoil_largekg
        // BombCrater_firmSoil_EXlargekg doesn't actually exist but will place TWO BombCrater_firmSoil_largekg craters near each other
        /* Samples: 
         * Static555 Smoke.Environment.Smoke1 nn 63748.22 187791.27 110.00 /height 16.24
    Static556 Smoke.Environment.Smoke1 nn 63718.50 187780.80 110.00 /height 16.24
    Static557 Smoke.Environment.Smoke2 nn 63688.12 187764.03 110.00 /height 16.24
    Static534 Smoke.Environment.BuildingFireSmall nn 63432.15 187668.28 110.00 /height 15.08
    Static542 Smoke.Environment.BuildingFireBig nn 63703.02 187760.81 110.00 /height 15.08
    Static580 Smoke.Environment.BigSitySmoke_0 nn 63561.45 187794.80 110.00 /height 17.01
    Static580 Smoke.Environment.BigSitySmoke_1 nn 63561.45 187794.80 110.00 /height 17.01
    Static0 Stationary.Environment.BombCrater_firmSoil_mediumkg nn 251217.50 257427.39 -520.00 
    Static2 Stationary.Environment.BombCrater_firmSoil_largekg nn 251211.73 257398.98 -520.00 
    Static1 Stationary.Environment.BombCrater_firmSoil_smallkg nn 251256.22 257410.66 -520.00

        Not sure if height is above sea level or above ground level.
    */
        /*
         * OK, so the whole idea to remove the smokes after a while doesn't seem to work at all, because the smokes
         * do not show up in the GamePlay list of stationary ground objects at all.  They don't seem to show up 
         * in search of aigroundactors either.  So, it seems to be impossible to control or remove them once they have been loaded
        Timeout(2.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete)", new object[] { }); });
        GamePlay.gpLogServer(null, "Setting up to delete stationary smokes in " + duration_s.ToString("0.0") + " seconds.", new object[] { });
        Timeout(3.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete2)", new object[] { }); });
        Timeout(4.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete3)", new object[] { }); });
        */
        /*
        Timeout(duration_s, () => {
            //GamePlay.gpLogServer(null, "Deleting object (delete4)", new object[] { });
            Point2d P = new Point2d(x, y);
            GamePlay.gpLogServer(null, "Deleting object (delete4) at" + P.x.ToString ("0.0") + " " + P.y.ToString("0.0"), new object[] { });
                                try
                                {
                //GamePlay.gpRemoveGroundStationarys(P, 10);

                //GamePlay.gpGroundStationarys(x, y, distance)



                    foreach (GroundStationary gg in GamePlay.gpGroundStationarys(x, y, 2)) //all stationaries w/i 500000 or whatever meters of this object
                    {
                        //if (gg.Name.StartsWith(subStatPrefix))
                        {
                            GamePlay.gpLogServer(null, "Destroying " + gg.Name, new object[] { });
                            gg.Destroy();
                            
                        }

                    }




            } catch (Exception ex)
            {
                System.Console.WriteLine("smoke&fire - Exception: " + ex.ToString());                                    
            }
    }); */
        Timeout(duration_s, () =>
        {
            //Console.WriteLine("Deleting stationary smokes . . . ");
            //This should work for bomb craters but HAS NO EFFECT for smoke effects
            //GamePlay.gpLogServer(null, "Deleting stationary bomb effects . . . ", new object[] { });
            Point2d P = new Point2d(x, y);
            //GamePlay.gpRemoveGroundStationarys(P, 10); //this is part of TF_extensions and doesn't work; it just fails silently when the timeout is called & entire timeout fails to run
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(x, y, 2))
            {
                if (sta == null) continue;
                Console.WriteLine("Deleting " + sta.Name + " " + sta.Title);
                if (sta.Name.Contains("smoke") || sta.Title.Contains("fire") || sta.Title.Contains("crater"))
                {
                    Console.WriteLine("Deleting stationary bomb effect " + sta.Name + " - end of life");
                    sta.Destroy();
                }
            }


        });

        //AMission mission = GamePlay as AMission;
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";
        string keybase = "Static";
        int count = 0;

        List<string> types = new List<string> { };
        if (type.Length > 0) types.Add(type);
        if (type2.Length > 0) types.Add(type2);

        foreach (string t in types)
        {

            string ttemp = t;
            string val1 = "Smoke";
            if (t.Contains("BombCrater")) val1 = "Stationary";
            int lines = 1;
            if (t == "BombCrater_firmSoil_EXlargekg")
            {
                ttemp = "BombCrater_firmSoil_largekg";
                lines = 2;
            }

            count++;
            string key = keybase + count.ToString();
            string value = val1 + ".Environment." + ttemp + " nn " + x.ToString("0.00") + " " + y.ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + z.ToString("0.00");
            f.add(sect, key, value);

            if (lines == 2)
            {

                count++;
                key = keybase + count.ToString();
                value = val1 + ".Environment." + ttemp + " nn " + (x + 9).ToString("0.00") + " " + (y - 9).ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + z.ToString("0.00");
                f.add(sect, key, value);

                count++;
                key = keybase + count.ToString();
                value = val1 + ".Environment." + ttemp + " nn " + (x - 9).ToString("0.00") + " " + (y + 9).ToString("0.00") + " " + stb_random.Next(0, 181).ToString("0.0") + " /height " + z.ToString("0.00");
                f.add(sect, key, value);
            }
        }

        /*
        sect = "Stationary";
        key = "Static2";
        value = "Smoke.Environment." + "Smoke1" + " nn " + x.ToString("0.00") + " " + (y  + 130).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static3";
        value = "Smoke.Environment." + "Smoke2" + " nn " + x.ToString("0.00") + " " + (y + 260).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static4";
        value = "Smoke.Environment." + "BuildingFireSmall" + " nn " + x.ToString("0.00") + " " + (y + 390).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static5";
        value = "Smoke.Environment." + "BuildingFireBig" + " nn " + x.ToString("0.00") + " " + (y + 420).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static6";
        value = "Smoke.Environment." + "BigSitySmoke_0" + " nn " + x.ToString("0.00") + " " + (y + 550).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static7";
        value = "Smoke.Environment." + "BigSitySmoke_1" + " nn " + x.ToString("0.00") + " " + (y + 680).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static8";
        value = "Smoke.Environment." + "BigSitySmoke_2" + " nn " + x.ToString("0.00") + " " + (y + 710).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);
        */

        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + path + "smoke-ISectionFile.txt", new object[] { }); //testing
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX9", null); //testing disk output for warps
        //f.save(path + "smoke-ISectionFile.txt"); //testing
        //load the file after a random wait (to avoid jamming them all in together on mass bomb drop
        Timeout(stb_random.NextDouble()*45, () => { GamePlay.gpPostMissionLoad(f); });


        //TODO: The part to delete smokes after a while isn't working; it never finds or stops any of the smokes.   It does delete craters, however.     



    }

    /*****************************************************************************
     * 
     * OnBombExplosion - handling points & other routines for area bombing, bombing of civilian areas, and bombing of airports
     * 
     *****************************************************************************/

    //Do various things when a bomb is dropped/explodes.  For now we are assessing whether or not the bomb is dropped in a civilian area, and giving penalties if that happens.
    //TODO: Give points/credit for bombs dropped on enemy airfields and/or possibly other targets of interest.
    //TODO: This is the sort of thing that could be pushed to the 2nd thread/multi-threaded
    int lastBombMessageTime_sec = Calcs.TimeSince2016_sec();

    public override void OnBombExplosion(string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {

        base.OnBombExplosion(title, mass_kg, pos, initiator, eventArgInt);

        //Task.Run(() => OnBombExplosion_DoWork(title, mass_kg, pos, initiator, eventArgInt)); //OK, don't do this as when many bombs explode it also explodes the CPU with way too many threads at once.

        //Spread them out a little over time
        //TODO: this could all be done in a worker thread (just not 1000 worker threads as we attempted above)
        double wait = stb_random.NextDouble() * 10;
        Timeout(wait, () =>
            OnBombExplosion_DoWork(title, mass_kg, pos, initiator, eventArgInt)
        );
    }

    public void OnBombExplosion_DoWork (string title, double mass_kg, Point3d pos, AiDamageInitiator initiator, int eventArgInt)
    {
        //GamePlay.gpLogServer(null, "bombe 1", null);
        bool ai = true;
        if (initiator != null && initiator.Player != null && initiator.Player.Name() != null) ai = false;

        //GamePlay.gpLogServer(null, "bombe 2", null);
        int isEnemy = 1; //0 friendly, 1 = enemy, 2 = neutral
        int terr = GamePlay.gpFrontArmy(pos.x, pos.y);

        //GamePlay.gpLogServer(null, "bombe 3", null);
        if (terr == 00) isEnemy = 2;
        if (!ai && initiator.Player.Army() == terr) isEnemy = 0;

        /***************************
        * 
        * Handle bomb landing on WATER
        * 
        ***************************/
        //For now, all things we handle below are on land, so if the land type is water we just
        //get out of here immediately
        maddox.game.LandTypes landType = GamePlay.gpLandType(pos.x, pos.y);
        if (landType == maddox.game.LandTypes.WATER) return;


        //This is to give some score parity to various types of bombers, encouraging pilots to fly them.
        //The destruction done remains proportional to tonnage of bomb etc, but they multipliers for their personal score that
        //make up for some planes carrying far less tonnage.
        //Factor is the max tonnage carried by each a/c under TF 4.5, scaled to JU88==1.  So ie Blenheim carries just 1/4 of that tonnage so gets a score correction factor of 1/(1/4) or 4.
        double aircraftCorrection = 1;
        AiAircraft aircraft = initiator.Actor as AiAircraft;
        string acType = Calcs.GetAircraftType(aircraft);
        if (acType.Contains("Blenheim")) aircraftCorrection = 4;
        if (acType.Contains("He-111")) aircraftCorrection = 1.5;
        if (acType.Contains("BR-20")) aircraftCorrection = 2;

        //for testing
        //ot_HandleAreaBombings(title, mass, pos, initiator, initiator.Player); return;

        //if (!ai && stb_Debug) GamePlay.gpLogServer(null, "OnBombExplosion called: " + title + " " + mass.ToString() + " " + initiator.Player.Name(), new object[] { });

        //maddox.game.world.GroundStationary TF_GamePlay.gpGroundStationarys(GamePlay, new maddox.GP.Point2d(pos.x, pos.y));

        /***************************
         * 
         * Handle bombing civilian areas AND area bombing generally
         * 
         ***************************/
        //Give penalties to players if they bomb civilian areas
        if (!ai) foreach (GroundStationary sta in GamePlay.gpGroundStationarys(pos.x, pos.y, 500))
            {
                if (sta == null) continue;
                //if (stb_Debug) GamePlay.gpLogServer(null, "OnBombExplosion near: " + sta.Name + " " + sta.Title, new object[] { });


                double dis_m = sta.pos.distance(ref pos);  //distance from this groundstationary to the bomb detonation location, meters

                //GamePlay.gpLogServer(null, "OnBombExplosion near: " + sta.Name + " " + sta.Title + " " + dis_m.ToString("n0"), new object[] { });

                if (sta.Title.Contains("JerryCan_GER1_1") || sta.Title.Contains("JerryCan_GER1_2") || sta.Title.Contains("JerryCan_GER1_3") || sta.Title.Contains("JerryCan_GER1_5"))
                {
                    //We use jerrycans to designate area targets or bombable areas.  
                    //The JerryCan_GER1_1 (static - environment - jerrycan) covers a radius of 71 meters which is just enough to fill a 100 meter square (seen in FMB at full zoom) to all corners if placed in the center of the 100m square.
                    // JerryCan_GER1_2 covers 141m radius (covers 4 100m squares to the corners if placed in the center)
                    // JerryCan_GER1_3 covers 282m radius (covers 16 100m squares to the corners if placed in the center)
                    // JerryCan_GER1_5 covers 1410m radius (a 1km square to the corner if placed in the center)
                    if (sta.Title.Contains("JerryCan_GER1_5") && dis_m <= 1410) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                    if (sta.Title.Contains("JerryCan_GER1_3") && dis_m <= 282) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                    if (sta.Title.Contains("JerryCan_GER1_2") && dis_m <= 141) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                    if (sta.Title.Contains("JerryCan_GER1_1") && dis_m <= 71) { ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Ground Area", 1, aircraftCorrection); return; }
                    // return;: Once we have found one bombable area marker, that is all we're looking for. Skip any further search for bombable area marks AND also for the civilian penalty markers; if it is within the given diestance of a jerrycan then this is by definition an enemy target zone
                    //If we want to do anything further below, such as give credit for bombing airfields, we'll need to re-write this somehow.
                }


                //Regent II Bus (static vehicle) defines a circle of 500 meters radius that is "civilian territory".  Note that the 500m radius is implicit in the GamePlay.gpGroundStationarys(pos.x, pos.y, 500) above, we've added && sta.pos.distance(ref pos) <= 500 so that we can change the radius above if necessary.
                //Maddox Games TA Sports Car (static vehicle)  defines a circle of 250 meters radius that is "civilian territory"
                //If they manage to hit a ROAD HIGHWAY or RAILROAD within a civilian area then they don't get negative points
                if (landType != maddox.game.LandTypes.ROAD && landType != maddox.game.LandTypes.ROAD_MASK && landType != maddox.game.LandTypes.HIGHWAY && landType != maddox.game.LandTypes.RAIL)
                {
                    if (sta.Title.Contains("AEC_Regent_II") && dis_m <= 500) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
                    if (sta.Title.Contains("MG_TA") && dis_m <= 250) ot_HandleCivilianBombings(initiator.Player, pos, initiator, mass_kg);
                }


            }


        //TF_GamePlay.gpIsLandTypeCity(maddox.game.IGamePlay, pos);       

        /********************
         * 
         * Handle airport bombing
         * 
         *******************/

        //GamePlay.gpLogServer(null, "bombe 5", null);

        var apkeys = new List<AiAirport>(AirfieldTargets.Keys.Count);
        apkeys = AirfieldTargets.Keys.ToList();

        //GamePlay.gpLogServer(null, "bombe 6", null);

        foreach (AiAirport ap in apkeys)//Loop through the targets; we do it on a separate copy of the keys list bec. we are changing AirfieldTargets mid-loop, below
        {
            /* if (!AirfieldTargets[ap].Item1)
            {//airfield has already been knocked out so do nothing
            }
            else
            { */

            //GamePlay.gpLogServer(null, "bombe 7", null);
            double radius = AirfieldTargets[ap].Item6;
            Point3d APPos = AirfieldTargets[ap].Item7;
            double distFromCenter = 1000000000;
            if (ap != null) distFromCenter = APPos.distance(ref pos);
            //Check if bomb fell inside radius and if so increment up
            if (ap != null & distFromCenter <= radius)//has bomb landed inside airfield check
            {


                //So, the Sadovsky formula is a way of estimating the effect of a blast wave from an explosion. https://www.metabunk.org/attachments/blast-effect-calculation-1-pdf.2578/
                //Simplifying slightly, it turns out that the radius of at least partial destruction/partial collapse of buildings is:
                // 50 lb - 30m; 100 lb - 40 m; 250 lb - 54 m; 500 lb - 67 m; 100 lb - 85 m; etc.
                //Turning this radius to an 'area of destruction' (pi * r^2) gives us an "area of destruction factor" for that size bomb.  
                //Since we are scoring the amount of destruction in e.g. an industrialized area, counting the destruction points as area (square footage, square meters, whatever) is reasonable.
                //Scaling our points in proportion to this "area of destruction factor" so that a 50 lb pound bomb gives 0.5 points, then we see that destruction increases with size, but lower than linearly.
                //So if a 50 lb bomb gives 0.5 points, a 100 lb bomb gives 0.72 points; 250 lb 1.41 points; 500 lb 2.33 points, 1000 lb 4.0 points, 2000 lb 6.48 points, etc
                //The formula below is somewhat simplified from this but approximates it pretty closely and gives a reasonable value for any mass_kg
                //This score is also closely related to the amount of ground churn the explosive will do, which is going to be our main effect on airport closure


                //double scoreBase = 0.06303;
                double scoreBase = 0.031515; //halving the score we were giving at first, since the Bomber pilot point totals seem to be coming up quite high in comparison with fighter kills
                //if (blenheim) scoreBase *= 8; //double score for Blenheims since their bomb load is pathetic      

                scoreBase *= aircraftCorrection; //Correcting tonnage effect for various types of bombers

                //Give more points for hitting more near the center of the airfield.  This will be the (colored) airfield marker that shows up IE on the map screen
                //TODO: Could also give more if exactly on the the runway, or near it, or whatever
                double multiplier = 0.5;
                if (distFromCenter <= 2 * radius / 3) multiplier = 1;
                if (distFromCenter <= radius / 3) multiplier = 1.5;

                //If 'road' then this seems to mean it is a PAVED runway or taxiway, so we give extra credit                
                if (landType == maddox.game.LandTypes.ROAD || landType == maddox.game.LandTypes.ROAD_MASK || landType == maddox.game.LandTypes.HIGHWAY)
                {
                    multiplier = 1.6;
                }

                scoreBase *= multiplier;


                if (mass_kg <= 0) mass_kg = 22;  //50 lb bomb; 22kg
                double score = scoreBase * Math.Pow(mass_kg, 0.67);

                /* Another way to reach the same end- probably quicker but less flexible & doesn't interpolate:
                 * 
                 * //Default is 0.5 points for ie 50 lb bomb
                 * if (mass_kg > 45) score = 0.722; //100 lb  (calcs assume radius of partial/serious building destruction per Sadovsky formula, dP > 0.10, explosion on surface of ground, and that 50% of bomb weight is TNT)
                if (mass_kg > 110) score = 1.41; //250 
                if (mass_kg > 220) score = 2.33; //500
                if (mass_kg > 440) score = 3.70; //1000
                if (mass_kg > 880) score = 5.92; //2000
                if (mass_kg > 1760) score = 9.33 ; //4000

                //UPDATE 5 Nov 2017: Bomber scores seem relatively too high so cutting this in half (though doubling it for Blennies since they are bomb-impaired)

                 */

                //Spread out these messages to no more than 1 per second
                int TimeNow_sec = Calcs.TimeSince2016_sec();
                double timeout = 0;
                if (TimeNow_sec - lastBombMessageTime_sec < 1) timeout = lastBombMessageTime_sec - TimeNow_sec + 1;
                lastBombMessageTime_sec = TimeNow_sec + (int)timeout + 1; //Which should be the same as lastBombMessageTime_sec +1;
                Console.WriteLine("Airportbombing: Delay airport bomb message by " + timeout.ToString("n1"));
                if (timeout < 0) { timeout = 0; }

                double individualscore = score;

                if (!ai && (isEnemy == 0 || isEnemy == 2))
                {
                    individualscore = -individualscore;  //Bombing on friendly/neutral territory earns you a NEGATIVE score
                                                         //but, still helps destroy that base (for your enemies) as usual
                    Timeout(timeout, () =>
                    {
                        GamePlay.gpLogServer(null, initiator.Player.Name() + " has bombed a friendly or neutral airport. Serious repercussions for player AND team.", new object[] { });
                    });
                }



                //TF_Extensions.TF_GamePlay.Effect smoke = TF_Extensions.TF_GamePlay.Effect.SmokeSmall;
                // TF_Extensions.TF_GamePlay.gpCreateEffect(GamePlay, smoke, pos.x, pos.y, pos.z, 1200);
                string firetype = "BuildingFireSmall";
                if (mass_kg > 200) firetype = "BuildingFireBig"; //500lb bomb or larger
                if (stb_random.NextDouble() > 0.25) firetype = "";
                //todo: finer grained bigger/smaller fire depending on bomb tonnage

                //set placeholder variables
                double PointsToKnockOut = AirfieldTargets[ap].Item3;
                double PointsTaken = AirfieldTargets[ap].Item4 + score;
                string Mission = AirfieldTargets[ap].Item2;
                bool disabled = AirfieldTargets[ap].Item1;
                DateTime lastBombHit = AirfieldTargets[ap].Item5;



                string cratertype = "BombCrater_firmSoil_mediumkg";
                if (mass_kg > 100) cratertype = "BombCrater_firmSoil_largekg"; //250lb bomb or larger
                if (mass_kg > 200) cratertype = "BombCrater_firmSoil_EXlargekg"; //500lb bomb or larger.  EXLarge is actually 3 large craters slightly offset to make 1 bigger crater

                double percent = 0;
                double prev_percent = 0;
                double points_reduction_factor = 1;
                if (PointsToKnockOut > 0)
                {
                    percent = PointsTaken / PointsToKnockOut;
                    prev_percent = (PointsTaken - score) / PointsToKnockOut;
                    if (prev_percent > 1) prev_percent = 1;
                    if ((prev_percent == 1) && (percent > 1)) points_reduction_factor = percent * 2; // So if they keep bombing after the airport is 100% knocked out, they keep getting points but not very many.  The more bombing the less the points per bomb.  So they can keep bombing for strategic reasons if they way (deny use of the AP) but they won't continue to accrue a whole bunch of points for it.
                }

                //GamePlay.gpLogServer(null, "bombe 8", null);

                individualscore = individualscore / points_reduction_factor;  //reduce the score if needed 

                if (!ai) stb_RecordStatsOnActorDead(initiator, 4, individualscore, 1, initiator.Tool.Type);  //So they have dropped a bomb on a target so they get some point score


                double timereduction = 0;
                if (prev_percent > 0)
                {
                    timereduction = (DateTime.Now - lastBombHit).TotalSeconds;
                }

                double timetofix = PointsTaken * 20 * 60 - timereduction; //50 lb bomb scores 0.5 so will take 10 minutes to repair.  Larger bombs will take longer; 250 lb about 1.4 points so 28 minutes to repeari
                                                                          //But . . . it is ADDITIVE. So the first 50 lb bomb takes 10 minutes, the 2nd another 10, the 3rd another 10, and so on on.  So if you drop 32 50 bl bombs it will take 320 minutes before the 32nd bomb crater is repaired.
                                                                          //Sources: "A crater from a 500lb bomb could be repaired and resurfaced in about 40 minutes" says one 2nd hand source. That seems about right, depending on methods & surface. https://www.airspacemag.com/multimedia/these-portable-runways-helped-win-war-pacific-180951234/
                                                                          //unfortunately we can repair only the bomb crater; the SMOKE will remain for the entire mission because clod internals don't allow its removal.
                                                                          //TODO: We could keep track of when the last bomb was dropped at each airport and deduct time here depending on how much repair had been done since the last bomb dropped

                if (timetofix < score * 20 * 60) timetofix = score * 20 * 60; //timetofix is never less than the time needed to fix this one bomb crater, even if the airport has accrued some repair time

                if (PointsTaken >= PointsToKnockOut) //airport knocked out
                {
                    percent = 1;
                    timetofix = 24 * 60 * 60; //24 hours to repair . . . 
                }
                //Advise player of hit/percent/points
                //if (!ai) GamePlay.gpLogServer(new Player[] { initiator.Player }, "Airport hit: " + (percent * 100).ToString("n0") + "% destroyed " + mass_kg.ToString("n0") + "kg " + individualscore.ToString("n1") + " pts " + (timetofix/3600).ToString("n1") + " hr to repair " , new object[] { }); //+ (timereduction / 3600).ToString("n1") + " hr spent on repairs since last bomb drop"

                Timeout(timeout, ()=>{
                    //Experiment: removing the message to see if it helps with warps  9/28/2018
                    if (!ai) GamePlay.gpLogServer(new Player[] { initiator.Player }, "Airport hit: " + mass_kg.ToString("n0") + "kg " + individualscore.ToString("n1") + " pts " + (timetofix / 3600).ToString("n1") + " hr to repair " + (percent * 100).ToString("n0") + "% destroyed " + ap.StripState(0).ToString(), new object[] { }); //+ (timereduction / 3600).ToString("n1") + " hr spent on repairs since last bomb drop" + (percent * 100).ToString("n0") + "% destroyed "
                    
                    //Sometimes, advise all players of percent destroyed, but only when crossing 25, 50, 75, 100% points
                    Timeout(0.3, () => { if (percent * 100 % 25 < prev_percent * 100 % 25) GamePlay.gpLogServer(null, ap.Name() + " " + (percent * 100).ToString("n0") + "% destroyed ", new object[] { }); });

                    loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, timetofix, stb_FullPath, cratertype);
                    //loadSmokeOrFire(pos.x, pos.y, pos.z, firetype, 180, stb_FullPath); //for testing, they are supposed to disappear after 180 seconds

                });

                

                if (PointsTaken >= PointsToKnockOut) //has points limit to knock out the airport been reached?
                {
                    AirfieldTargets.Remove(ap);
                    AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(true, Mission, PointsToKnockOut, PointsTaken, DateTime.Now, radius, APPos));
                    if (!disabled)
                    {

                        //We do this part only in -stats.cs and & will stamp craters all over the ap but only if the ap was disabled by LIVE pilots, not AI . . . 
                        //UPDATE 2018/09: We don't stamp the craters but actually disable the birthplace so that it no longer functions as a spawn point
                        AirfieldDisable(ap);


                        //LoadAirfieldSpawns(); //loads airfield spawns and removes inactive airfields. (on TWC this is not working/not doing anything for now)
                        //This airport has been destroyed, so remove the spawn point
                        //** We do this only in the -MAIN.cs, not -stats.cs
                        /*
                        if (ap != null)
                        {
                            foreach (AiBirthPlace bp in GamePlay.gpBirthPlaces())
                            {
                                Point3d bp_pos = bp.Pos();
                                if (ap.Pos().distance(ref bp_pos) <= ap.FieldR()) bp.destroy();//Removes the spawnpoint associated with that airport (ie, if located within the field radius of the airport)
                            }
                        }
                        */

                    }
                }
                else
                {
                    AirfieldTargets.Remove(ap);
                    AirfieldTargets.Add(ap, new Tuple<bool, string, double, double, DateTime, double, Point3d>(false, Mission, PointsToKnockOut, PointsTaken, DateTime.Now, radius, APPos));
                }
                //GamePlay.gpLogServer(null, "bombe 11", null);
                break;  //sometimes airports are listed twice (for various reasons).  We award points only ONCE for each bomb & it goes to the airport FIRST ON THE LIST (dictionary) in which the bomb has landed.
            }
        }

        /***************************
        * 
        * Handle bomb landing on ROAD, HIGHWAY, or RAIL
        * 
        * We save this for last because we can test for road etc in airport bombings to see if something is on the runway or whatever
        * 
        ***************************/

        if (landType == maddox.game.LandTypes.ROAD || landType == maddox.game.LandTypes.ROAD_MASK || landType == maddox.game.LandTypes.HIGHWAY)
        {
            ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Road/Highway", .4, aircraftCorrection, true);
            return;
        }

        if (landType == maddox.game.LandTypes.RAIL)
        {
            ot_HandleAreaBombings(title, mass_kg, pos, initiator, initiator.Player, isEnemy, "Railroad", .75, aircraftCorrection, true);
            return;
        }

    }


    /*******************************************************************
    //We're not using their onplaceenter bec. it does balance etc etc that we don't want or need. 
    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceEnter(player, actor, placeIndex);
        try
        {
            Stb_BalanceUpdate(player, actor, placeIndex, true);
            if (!Stb_BalanceCheckForBan(player, actor, placeIndex))
            {
                if (placeIndex == 0 && player.PlacePrimary() == 0 && player.PlaceSecondary() == -1)
                {
                    AiAircraft aircraft = actor as AiAircraft;
                    if (aircraft != null)
                    {
                        StbStatTask sst;
                        if (player.Army() == 1 && actor.Name().Contains(":BoB_RAF_B_7Sqn.000"))
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 2 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ground Units");
                        }
                        else if (player.Army() == 2 && actor.Name().Contains(":BoB_LW_KG1_I.000"))
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 2 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ground Units");
                        }
                        else if (aircraft.Type() == AircraftType.Fighter || aircraft.Type() == AircraftType.HeavyFighter)
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 1 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Aircrafts");
                        }
                        else
                        {
                            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 3 });
                            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ships");
                        }
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    ***************************************************************/



    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceEnter(player, actor, placeIndex);
        //if (stb_playersForcedOut.Contains(player.Name())) return; //This is a place enter that happened because we forced it, above in onPlaceLeave
        try               
        {
            //Stb_Message(null, "Player last died . . . ", new object[] { });
            //Console.WriteLine(stb_StatRecorder.StbSr_TimeSincePlayerLastDied("TWC_Flug"));
            //Console.WriteLine("Version: " + Environment.Version.ToString());
            //Console.WriteLine("PlaceEnter: 1 ");


            stb_SaveIPlayerStat.StbSis_Save(player); //Save the stats CloD has been accumulating and/or initiate the stats collection. We need to call this (at MIN! when the player first arrives on the server & when the player leaves and/or the server shuts down. We also call it on player death.  Could be called more times at convenient intervals or whatever, which could prevent any stats from being lost in case of unexpected shutdown etc.

            //Stb_changeTargetToPlayerRecurs(player);

            AiAircraft aircraft = actor as AiAircraft; //we allow them to enter artillery, tank, etc, just not an aircraft
            string aircraft_type = Calcs.GetAircraftType(aircraft);
            string airc = "aircraft";
            if (aircraft == null) airc = "not an aircraft";
            string person = "person";
            AiPerson pers = actor as AiPerson;
            if (pers == null) person = "not a person";
            string ground = "ground actor";
            AiGroundActor aga = actor as AiGroundActor;
            if (aga == null) ground = "not a ground actor";
            //Console.WriteLine("OnPlaceEnter: Setting pilot type of " + player.Name() + " " + actor.Name() + " " + placeIndex.ToString() + " " + aircraft_type + " " + ground + " " + person + " " + airc + " PlacePrimary: " + player.PlacePrimary()); //PlacePrimary supposedly == -1 on parachute bail

            //Set the player career type (ie bomber, fighter etc)
            //We have a problem in that when a player bails out of an aircraft that triggers OnPlaceEnter (for some reason!?) and in that one case
            //we don't want to change the pilot type - ie, if they are a bomber pilot, they will still be on their bomber pilot career after they
            //parachute out until they enter another type of aircraft or actor.
            //So we skip setPilotType ONLY IF all the following are true: NOT a ground actor NOT in an aircraft AND they just parachuted (player.PlacePrimary() != -1 seems to be best/only way to determine this?)
            if (aga == null && aircraft == null && player.PlacePrimary() == -1)
            {
                //All the above criteria are met, so do nothing, this is a person who just parachuted out
                //We could do anything here we wanted, to people who just bailed out of an aircraft
            }
            else
            {
                //not parachuting out but entering some type of Actor place, so DO set pilot type
                stb_setPilotType(player, actor);

                //We can send the a/c to -supply.cs as often as we like, it will only register a given a/c once and sending it whenever someone enters a Place ensures we don't accidently overlook it
                if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceEnter(player, actor);
            }
            //Console.WriteLine("OnPlaceEnter: Setting pilot type of " + player.Name() + " " + placeIndex.ToString());

            //check if the player is timed out due to a previous death
            int TimedOut_seconds = stb_StatRecorder.StbSr_IsPlayerTimedOutDueToDeath(player.Name());

            if (TimedOut_seconds > 0 && aircraft != null) {
                int timeOut_minutes = (int)(TimedOut_seconds / 60);
                string message = stb_StatRecorder.StbSr_RankFromName(player.Name(), actor) + player.Name() + " is restricted from flying. " + Calcs.SecondsToFormattedString(TimedOut_seconds) + " remains in your pilot restriction.";
                //System.Console.WriteLine(message);
                Stb_Message(new Player[] { player }, message, null);
                Timeout(2.0, () => { Stb_Message(new Player[] { player }, "To encourage a more realistic approach to piloting and battle, pilots who die are grounded for a period of time. You could try operating artillery or a ground vehicle.", null); });
                Timeout(4.0, () => { Stb_Message(new Player[] { player }, "A pause after player death also allows us to properly wrap up player stats for your previous life.", null); });
                if (stb_PlayerTimeoutWhenKilled &&  stb_PlayerTimeoutWhenKilled_OverrideAllowed) Timeout(6.0, () => { Stb_Message(new Player[] { player }, "If you are philosophically opposed to this idea, enter the chat command <override to continue immediately.", null); });
                //Timeout(10.0, () => { Stb_Message(new Player[] { player }, message, null); });
                //Timeout(20.0, () => { Stb_Message(new Player[] { player }, message, null); });                
                //Force a player into a certain place:
                //Player.Place() = (Actor as AiAircraft).Place(placeIndex);
                //player.Place() = null;//And OK--that doesn't work at all.
                //This should work: player.PlaceEnter(aircraft, place);  int place is indxplace
                // and: Player.PlaceLeave(int indxPlace)

                stb_ContinueMissionRecorder.StbCmr_SetIsForcedPlaceMove(player.Name());
                Stb_RemovePlayerFromAircraftandDestroy(aircraft, player, 1.0, 3.0);
                //Timeout(1.1, () => { stb_ContinueMissionRecorder.StbCmr_ClearIsForcedPlaceMove(player.Name()); }); //don't really need to remove it as onpositionleave does that already

                string peHud_message = "Sorry, you cannot fly--grounded for " + Calcs.SecondsToFormattedString(TimedOut_seconds) + " due to recent death";
                GamePlay.gpHUDLogCenter(new Player[] { player }, message, null);
                Timeout(10.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, message, null); });
                Timeout(20.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, message, null); });
                Timeout(30.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, message, null); });
                return; //and . . . don't save new mission/sortie etc etc etc

                //} else if (stb_restrictAircraftByKills && stb_StatRecorder.StbSr_NumberOfKills(player.Name()) < stb_restrictAircraftByKills_RequiredKillNumber && (aircraft_type.Contains("Spitfire") || aircraft_type.Contains("Bf-109"))) {

            } else if (!stb_RankToAllowedAircraft.StbRaa_isPlayerAllowedAircraft(aircraft, player, actor) && aircraft != null) {

                string playerName = stb_StatRecorder.StbSr_MassagePlayername(player.Name());
                string message0 = stb_StatRecorder.StbSr_RankFromName(player.Name(), actor) + playerName + " is restricted from flying " + aircraft_type + " until sufficient kills OR rank have been achieved. ";

                //System.Console.WriteLine(message0);
                Stb_Message(new Player[] { player }, message0, null);
                //Stb_Message(new Player[] { player }, "Please choose a different aircraft.", null);                

                Timeout(5.0, () => {
                    string message2 = "Unlocked aircraft for your rank, " + stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForRank(player);
                    Stb_Message(new Player[] { player }, message2, null); 
                });
                                
                Timeout(10.0, () => {
                    string message3 = stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForAce(player);
                    if (message3 != "") Stb_Message(new Player[] { player }, "Unlocked aircraft for your number of kills: " + message3, null); 
                });

                Timeout(15.0, () => { Stb_Message(new Player[] { player }, "Chat commands <ac and <nextac show your current & potential unlocked aircraft.", null); });

                stb_ContinueMissionRecorder.StbCmr_SetIsForcedPlaceMove(player.Name());
                Stb_RemovePlayerFromAircraftandDestroy(aircraft, player, 1.0, 3.0);


                string pe2Hud_message = "Sorry, you are restricted from " + aircraft_type +  ". Read Chat Msg & Mission Briefing for details";
                GamePlay.gpHUDLogCenter(new Player[] { player }, pe2Hud_message, null);
                Timeout(10.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, pe2Hud_message, null); });
                Timeout(20.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, pe2Hud_message, null); });
                Timeout(30.0, () => { GamePlay.gpHUDLogCenter(new Player[] { player }, pe2Hud_message, null); });

                return; //and . . . don't save new mission/sortie etc etc etc

            }

            //Stb_Message(null, "Aircraft" + aircraft.TypedName() + aircraft.Type().ToString() + aircraft.Name(), null);

            //if (aircraft.Type()=AircraftType.SpitfireMkI_100oct) Stb_Message(null, "Your a/c is a spitfire", null);

            //Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", typeof(AircraftType).Namespace, typeof(AircraftType).Module, typeof(AircraftType).ToString(), typeof(AircraftType).IsEnum, typeof(AircraftType).IsValueType, typeof(AircraftType).GetType()); //, AircraftType.GetType()

            //Note: .InternalTypeName() is the key.  But we should use Calcs.GetAircraftType(aircraft) instead - cleaner.
            //Console.WriteLine("typedname: {0}, name: {1}, typestring: {2}, typeint: {3}, hull: {4}, itypename: {5}", aircraft.TypedName(), aircraft.Name(), aircraft.Type().ToString(), ((int)aircraft.Type()).ToString(), aircraft.HullNumber(), aircraft.InternalTypeName()); //, AircraftType.GetType()









            /************* Sample TASK from volcanicisland - we're not using 
             *tasks for now.
             **************************************************************/
            /* StbStatTask sst;
            sst = new StbStatTask(StbStatCommands.TaskCurrent, player.Name(), new int[] { 3 });
            GamePlay.gpHUDLogCenter(new Player[] { player }, "Your Task: Engage Enemy Ships");

            stb_StatRecorder.StbSr_EnqueueTask(sst);
            */
            if (player != null ) {
                StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 787 }, player.Place() as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst1);

                /*
                 * Trying to record the SORTIE START here but it doesnt' work because of weirdness with bomber player being in multiple places at once etc.
                string playerName = player.Name();
                bool realsortiestart = stb_ContinueMissionRecorder.StbCmr_IsItASortieStart(playerName);
                if (realsortiestart)
                {                    
                    //We record start of sortie here.  If it turns out to be less than 4 minutes later, we'll subtract it back out.
                    StbStatTask sst00 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 844 }, player.Place() as AiActor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst00);
                }
                */
            }


            

            //set up the possibility to continue a mission over numerous sorties
            //Console.WriteLine("PlaceEnter: 2 ");
            stb_ContinueMissionRecorder.StbCmr_SavePositionEnter (player, actor);



            /*
             * 2016/01/19 - moved this routine to onActorCreated, where it should work better
             * 
            //Bec this has an air s pawn point, our "ontookoff" routine doesn't work, so we are 
            //going to try to do it here instead (in addition to) the ontookoff
            //The routine below works great for onTookOff, but in onPlaceEnter it has a serious problem, that the person
            //might just be moving around places within (say) a bomber.  Not sure how to detect this (very common) issue.
            //TimedOut_seconds <= 0 keeps us from counting new flights/sorties when the person is actually banned from flying right now . . . 
            //Special for TWCTrainingFrance mission
            if (aircraft != null && TimedOut_seconds <= 0)
            {
                //770 = record a take off
                //We're not going to do that here, because whatever else an air spawn is, it is NOT a take off!
                //StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 770 });

                for (int i = 0; i < aircraft.Places(); i++)
                {
                    //if (aiAircraft.Player(i) != null) return false;
                    if (aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null)
                    {
                        string playerName = aircraft.Player(i).Name();
                        //Console.WriteLine("Place Enter / Start Sortie " + playerName);


                        bool realsortiestart = stb_ContinueMissionRecorder.StbCmr_IsItASortieStart(playerName);
                        //If this is not a continuation mission, we increment the mission counter for this player                                 
                        if (realsortiestart && !stb_ContinueMissionRecorder.StbCmr_OnTookOff(playerName, actor))
                        {

                            //Console.WriteLine("New Mission for " + playerName);
                            StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 779 }, actor);
                            stb_StatRecorder.StbSr_EnqueueTask(sst1);
                        };

                    }
                }


                //779 = Mission 

                //check if this is a continuation of the previous flight from near the
                //same position the a/c previously landed etc.  If so, it's not a "new mission" but a continuation of the previous mission, so don't increment the mission counter.  If not, we increment the mission counter.

            }
            */
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }


    //787 Enter place
    //788 Leave place
    //HashSet<string> stb_playersForcedOut = new HashSet<string>();
    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceLeave(player, actor, placeIndex);

        //Console.WriteLine("OnPlaceLeave: place " + player.Place().Name() + " person.place " + player.PersonPrimary().Place() + " type " + player.PersonPrimary().Cart().InternalTypeName());
        /* if (player.Place() != null) Console.WriteLine("OnPlaceLeave: place " + player.Place().Name());
        else Console.WriteLine("OnPlaceLeave: place is NULL");
        if (player.PlacePrimary() != null) Console.WriteLine("OnPlaceLeave: placeprimary " + player.PlacePrimary().ToString());  // =-1 seems to indicate parachuting situation
        else Console.WriteLine("OnPlaceLeave: placeprimary is NULL");
        if (player.PersonPrimary() != null) Console.WriteLine("OnPlaceLeave: person.place " + player.PersonPrimary().Place().ToString());
        else Console.WriteLine("OnPlaceLeave: primary person is NULL");
        if (actor as AiPerson != null) Console.WriteLine("PLACELEAVE: Actor is AiPerson");
        */
        //Console.WriteLine("OnPlaceLeave: person.place " + player.PersonPrimary().Place());
        //Console.WriteLine(" type " + player.PersonPrimary().Cart().InternalTypeName());
        //if (stb_playersForcedOut.Contains(player.Name())) return; //This is a place leave that happened because we forced it, below
        try
        {        
                 
          StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 788 }, actor);
          stb_StatRecorder.StbSr_EnqueueTask(sst1);

          //Console.WriteLine("PLACE LEAVE: " + player.Name());
          //Stb_Chat("Place Leave", player);
            //set up the possibility to continue a mission over numerous flights
            stb_ContinueMissionRecorder.StbCmr_SavePositionLeave (player, actor);


            /*if (actor as AiPerson != null ) { //OK, the player is leaving a "person", ie, while parachuting down

                {
                    AiPerson ourPerson = (actor as AiPerson);

                    //Player.Place() = (actor as AiAircraft).Place(placeIndex);
                    //we force the player back into place momentarily
                    //stb_playersForcedOut.Add(player.Name());
                    //player.PlaceEnter(actor as AiAircraft, placeIndex);

                    Stb_Message(new Player[] { player }, "You left your parachute in the air. You get the same results as though you'd ridden it out.", new object[] { });
                    Console.WriteLine("Player left parachute");
                    if (stb_random.NextDouble() < .8) OnPersonParachuteLanded(ourPerson);
                    else OnPersonParachuteFailed(ourPerson);

                    //player.PlaceLeave(placeIndex);
                    //stb_playersForcedOut.Remove(player.Name());
                }

            }
            */
            
            bool isLastPlayer = Stb_isLastPlayerInAircraftandOneOrNoPositions(actor as AiAircraft,player);
            if (isLastPlayer) Console.WriteLine("IS last player");
            else Console.WriteLine("Is NOT last player");
            //Console.WriteLine("PPI,PPS: " + player.PlacePrimary().ToString() + " " + player.PlaceSecondary().ToString()); //When both 'persons' are out this looks like -1,-1.  The 2nd to last one out looks like 0,-1 or -1, 0 maybe. If switching out of a spot mid-flight to allow AI to take over one position it looks like -1, 1 or -1,2 0,1 0,2 or other things.  But both gone is always -1,-1

            //We need to wait more than 0.1 seconds to do this, because the "determination about whether it is a real place leave by StbCmr takes 0.1 seconds to compute"
            Timeout(0.2, () => //This Timeout must be LONGER!!! than the one in StbCmr_SavePositionLeave or we won't get reliable results!!!!!
            {
                //If the player is NOT already dead @ this point, we're assuming they have exiting the game, exited the a/c, or whatever, and treat it exactly as though they had hit the parachute at this moment.
                //The other possibility however is a ground collision at somewhat slow speed, which our onDead routine
                //has judged to be nonfatal.  The game kicks them out of their place at that point, to watch the fiery explosion.
                //If they HAVE already died then we just skip this bit.

                //Stb_Chat("Checking place leave . . . ", player);

                //there are a number of reasons the pilot may have left the place.  We try to sort them all out here. 

                //1. if a/c crashes (ie fireball) CloD pops the pilot out for external view
                //2. In this case, the pilot MIGHT be dead but (thanks for our merciful calculations above), also maybe not
                //3. realPosLeave is false when it is just a move within (say) a bomber to a different position
                int oldDeathTime = 0;
                int currTime = Calcs.TimeSince2016_sec();
                if (!stb_PlayerDeathAndTime.TryGetValue(player.Name(), out oldDeathTime)) oldDeathTime = 0;
        
                bool realPosLeave = stb_ContinueMissionRecorder.StbCmr_HasPlayerLeftPlane(player.Name());
                
                bool isPlayerAlreadyDead = stb_ContinueMissionRecorder.StbCmr_IsPlayerDead(player.Name()); //if FALSE the player may have recently died OR just not taken off yet - the point where cm.alive is set to true

                
                if (realPosLeave && (actor as AiAircraft) != null)
                {
                    //So if we are here it's a real pos leave & we're in an aircraft.  If we're the LAST PLAYER we're going to call it
                    //a real aircraft abandon.
                    //Below, we will also force destruction of the aircraft in certain situations such as big crash.  But this will be our 'main' 
                    //place to say, "Yes, the final pilot has left the aircraft"
                    if (isLastPlayer && TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor); //Since this is a real position leave, -supply.cs handles the details of returning the a/c to supply
                }

                /*
                if (realPosLeave && isPlayerAlreadyDead && (actor as AiAircraft) != null && currTime - oldDeathTime > 5 )
                {
                    Console.WriteLine("Forcing exit 2");
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                }
                */

                if (isPlayerAlreadyDead) Console.WriteLine("Checking place leave - player already dead . . . ", player);
                if (realPosLeave) Console.WriteLine("Checking place leave - realposleave . . . ", player);

                if (realPosLeave && !isPlayerAlreadyDead && (actor as AiAircraft) != null && currTime - oldDeathTime > 5) // && player.PlacePrimary() != -1)  // PlacePrimary==-1 means parachuting.  If they are doing that we just let them go
                {

                    //Stb_Chat("Checking place leave 1 . . . ", player);
                    if (player.Place() != null) Console.WriteLine("OnPlaceLeave: place " + player.Place().Name());
                    else Console.WriteLine("OnPlaceLeave: place is NULL");
                    if (player.PlacePrimary() != null) Console.WriteLine("OnPlaceLeave: placeprimary " + player.PlacePrimary().ToString());
                    else Console.WriteLine("OnPlaceLeave: placeprimary is NULL");
                    if (player.PersonPrimary() != null) Console.WriteLine("OnPlaceLeave: person.place " + player.PersonPrimary().Place().ToString());
                    else Console.WriteLine("OnPlaceLeave: primary person is NULL");

                    Console.WriteLine("POSLeave: " + realPosLeave.ToString() + " " + isPlayerAlreadyDead.ToString() + " " + currTime.ToString() + " " + oldDeathTime.ToString());                    //if (actor as AiPerson != null) Console.WriteLine("PLACELEAVE: Actor is AiPerson");

                    //OK, this is the place we need to deal with: player left a/c mid-flight, player lost internet connection, player landed @ some random spot then left the plane, player used the 'flag screen" to leave the plane mid-flight, etc etc etc
                    //This a bit awkward because we are doing this in StbCmr instead of in mission itself . . . 

                    //double Z_VelocityTAS = a.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
                    //if (Z_VelocityTAS == 0) OnAircraftStopped(a);

                    double Z_AltitudeAGL = (actor as AiAircraft).getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);

                    if (Z_AltitudeAGL <= 5)
                    {


                        //Stb_Chat("Checking place leave 2 . . . ", player);
                        double injuries = stb_CalcExtentOfInjuriesOnActorDead(player.Name(), 2, actor, player, true);// killtype 2 bec. they left the position voluntarily = self.kill.  This actually makes injuries LESS serious in certain cases (ie, very low speed).

                        //we force the player back into place momentarily
                        //this is necessary in order fo the killActor() routine to work properly
                        //stb_playersForcedOut.Add(player.Name());
                        //player.PlaceEnter(actor as AiAircraft, placeIndex);

                        /*
                        if (injuries >= 0.5 || Stb_distanceToNearestAirport(AiActor actor) > 2000 )
                        {
                            Stb_Message(new Player[] { player }, "Your aircraft was written off due to damage.", new object[] { });
                            stb_StatRecorder.StbSr_EnqueueTask(new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 845 }, player as AiActor));

                        }
                        */

                        if (injuries < 1)
                        {
                            //if (injuriesmission.Stb_killActor(actor, 0);  //as long as injuries<1 they shouldn't actually die, but it will be treated as though they had a ground collision OR landing at this moment.
                            //Stb_Chat("Player left plane: Injuries <1", player);

                            //Stb_Chat("Checking place leave 3 . . . ", player);

                            if (injuries > 0.1)
                            {
                                string line = player.Name() + " crashed with injuries.";
                                //Stb_Message(new Player[] { player }, player.Name() + " left an aircraft in motion. This is treated the same as a ground crash at the same speed.", new object[] { });
                                Stb_Chat(line, player);

                                //OnAircraftCrashLanded((actor as AiCart).Person(placeIndex), player); //treat as landing.  they will live/die/captured etc depending on whether water, land, friendly, enemy, 
                                OnAircraftCrashLanded(actor, player, actor as AiAircraft, injuries); //treat as landing.  they will live/die/captured etc depending on whether water, land, friendly, 
                                                                                                     //The player has really left the aircraft and the aircraft has no further  players in it.  Therefore, do the work of returning to supply
                                
                                //Here we could do *injuries* or **partial damage** to aircraft being returned, but for now we are just considering them landing whole in one piece OK.
                                //if (isLastPlayer && TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor); //Since this is a real position leave, -supply.cs handles the details of returning the a/c to supply
                            }
                            else
                            {                                

                                //Stb_Chat("Player left plane: Injuries <=0.1; treating as a normal landing", player);
                                //OnAircraftLanded((actor as AiCart).Person(placeIndex), player); //treat as landing.  they will live/die/captured etc depending on whether water, land, friendly, enemy, etc.
                                string line3 = player.Name() + " landed and left the aircraft.";
                                //Stb_Message(new Player[] { player }, player.Name() + " left an aircraft in motion. This is treated the same as a ground crash at the same speed.", new object[] { });
                                Stb_Chat(line3, player);
                                OnAircraftLanded(actor, player, actor as AiAircraft, injuries); //treat as landing.  they will live/die/captured etc depending on whether water, land, friendly, enemy,
                                
                                //The player has really left the aircraft and the aircraft has no further  players in it.  Therefore, do the work of returning to supply
                                //if (isLastPlayer && TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor); //Since this is a real position leave, -supply.cs handles the details of returning the a/c to supply
                            }
                        }
                        else
                        { //injuries = 1 

                            //Stb_Chat("Checking place leave 4 . . . ", player);
                            Stb_Chat("Player left plane: Injuries = 1; player death", player);
                            stb_RecordStatsForKilledPlayerOnActorDead(player.Name(), 1, actor, player, false);

                            //Here we are forcing aircraft loss in case of severe injuries.
                            //This MIGHT duplicate the 'leaving aircraft' call above but will carry additional info of "and the plane was destroyed"
                            if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                        }
                    }

                    else //If they actually did leave the plane just while it was in mid-air.  And they haven't recently died etc etc etc.  So this is just a person voluntarily leaving (or perhaps they lost internet connection, whatever). We treat it as though they have jumped from the plane at this moment in their parachute.  If they land in enemy territory, on water, etc then consequences will be appropriate for that action.  CloD seems to do parachute fail about 20-50% of the time, so our routines are set up to handle that, so we'll duplicate that functionality here.
                    {
                        /* AiPerson ourPerson = (actor as AiCart).Person(placeIndex);

                        //Player.Place() = (actor as AiAircraft).Place(placeIndex);


                        Stb_Message(new Player[] { player }, "You left your aircraft in the air. This is treated as though you suddenly parachuted from your aircraft.", new object[] { });
                        Console.WriteLine("Player left plane: Injuries =1 and parachuting");
                        //if (stb_random.NextDouble() < .8) OnPersonParachuteLanded((actor as AiCart).Person(placeIndex));
                        //else OnPersonParachuteFailed((actor as AiCart).Person(placeIndex));
                        if (stb_random.NextDouble() < .8) OnPersonParachuteLanded(player as AiPerson);
                        else OnPersonParachuteFailed(player as AiPerson);

                        */

                        //Stb_Chat("Checking place leave 5 . . . ", player);
                        string line2 = player.Name() + " has parachuted from the aircraft.";
                        Stb_Chat(line2, player);
                        OnPersonParachuteLanded(actor, player);
                        Console.WriteLine("Forcing exit 3");
                        if (isLastPlayer && TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off, in case they were the last person on board AND they have just parachuted out

                    }

                    //Stb_Chat("Checking place leave 6 . . . ", player);
                    //now we remove the player from the place again
                    //player.PlaceLeave(placeIndex);
                    //stb_playersForcedOut.Remove(player.Name());

                }
                
            });

            Stb_BalanceUpdate(player, actor, placeIndex, false);
              if (stb_DestroyOnPlaceLeave)
              {
                  AiAircraft aircraft = actor as AiAircraft;
                  if (aircraft != null)
                  {
                      Timeout(60.0, () => { Stb_DestroyPlaneSafe(aircraft); });//works fine if you havent take off or flying in neutral zone
                      Timeout(70.0, () => { Stb_DestroyPlaneSafe(aircraft); });//second call for cleanup
                      //game thinks that you are still airborne after you landed inside an airfield perimeter
                      //because we havent set landing waypoints & airfields properly
                      //this is why we call Stb_DestroyPlaneSafe again, we could do the same by adding an ai with airport.cpp
                      //instead of calling this method twice but airport.cpp does the cleanup by killing planes 
                      //which leads to wrong stats collection, for ex a user who just damaged an enemy plane normally takes only
                      //damage bonus, but with airport.cpp, if the damaged plane lands, he would also take a kill participation bonus
                  }
              }          
        }  
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
        base.OnActorCreated(missionNumber, shortName, actor);

        //Stb_Message(null, shortName + " created!", new object[] { });

        //Ground objects (except AA Guns) will die after X min when counted from their birth

        /*** //Experimental method to create wounded AI aicraft for testing purposes.
        if (actor is AiAircraft)
        {
           Timeout(30, () =>
           {
               //experimental - were damaging all AiAC to find out what happens when the crash land etc but don't die
               AiAircraft aircraft = actor as AiAircraft;
               //Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsElevatorDisabled);
               //Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsAileronsDisabled);
               Stb_DamagePlane(aircraft, NamedDamageTypes.ControlsRudderDisabled);
               Stb_DamagePlane(aircraft, NamedDamageTypes.Eng0TotalSeizure);
           });


        }
        */

        /* We're counting sortie start by looking for any a/c creating & then seeing if there is a player inside.  Then this increments
         * the sortie counter.  Later if the player leaves & the flight time was less than 4 minutes we subtract that sortie back out.
         * This will miss a few cases:
         *  - Offline play where the player can jump into an already-existing a/c
         *  - Jumping into a tank or AA (it already exists)
         *  - A 2nd/3rd player jumping into a bomber (it already exists)
         *  */

        if (actor != null && actor is AiAircraft ) {
            //IsAirborne is false right now, but if we wait a little bit becomes true, when we are spawning in at an airspawn
            /* moving this part, to detect sortie start, to onplaceenter . . .
                AiAircraft aircraft = actor as AiAircraft;
                for (int i = 0; i < aircraft.Places(); i++)
                {
                    //if (aiAircraft.Player(i) != null) return false;
                    if (aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null)
                    {

                        string playerName = aircraft.Player(i).Name();
                        //We record start of sortie here.  If it turns out to be less than 4 minutes later, we'll subtract it back out.
                        StbStatTask sst0 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 844 }, aircraft.Player(i) as AiActor);
                        stb_StatRecorder.StbSr_EnqueueTask(sst0);
                    }
                }
               */

            AiAircraft aircraft = actor as AiAircraft;

            Timeout(0.15, () =>
            {
                //AiAircraft aircraft = actor as AiAircraft;
                double Z_AltitudeAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                //Console.WriteLine("Checking AirSpawn . . ." + shortName + " " + actor.Name() + " " + Z_AltitudeAGL.ToString("0.0") + " " + aircraft.IsAirborne().ToString());

                if (aircraft != null && !Stb_isAiControlledPlane(aircraft) && aircraft.IsAirborne()) //aircraft + NOT aicontrolled + airborne @ creation = airspawn
                {
                    //this is an airspawn takeoff; thus "IsTakeOff" is false & we don't add an actual take-off to the player's stats
                    Console.WriteLine("AirSpawn!!!!");
                    ProcessAircraftTakeOff(aircraft: aircraft, IsTakeOff: false);
                }

                //2018-09-24 testing system to find out weapons loadout for <rr
                if (aircraft != null && !Stb_isAiControlledPlane(aircraft)) //live pilot has spawned in an aircraft
                {

                    //This seems the best place to detect creation of aircraft to subtract it from supply.
                    //We already know there is a player in the a/c, but we need to know the player, and should only do this once per a/c
                    //(ie, for the first player we find)
                    //need to do it before the weaponsmask stuff bec. it causes an error?

                    for (int i = 0; i < aircraft.Places(); i++)
                    {
                        if (aircraft.Player(i) is Player && aircraft.Player(i) != null)
                        {
                            if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceEnter(aircraft.Player(i), actor);
                            //we send it multiple times without concern; -supply.cs makes sure a single a/c is never checked out more than once.
                            //break; //only do this ONCE although the player will probably be in two places in a multi-player
                        }
                    }
                    //This is an idea about how to load aircraft loadout, however it isnt working 2018/09 flug
                    //AiBirthplace birthplace = somebirthplacereference;
                    Console.WriteLine("Checking Loadout/weaponsmask . . . ");
                    AiBirthPlace birthplace = Stb_nearestBirthPlace(aircraft as AiActor, 0);
                    Console.WriteLine("Checking Loadout/weaponsmask: " + birthplace.Name() + " " + aircraft.TypedName() + " " + aircraft.InternalTypeName()
                         + " " + aircraft.HullNumber() 
                         + " " + aircraft.CallSignNumber());
                    System.Collections.BitArray weaponsmask = birthplace.GetWeaponsMask(aircraft.InternalTypeName());
                    //System.Collections.BitArray weaponsmask = birthplace.GetWeaponsMask(aircraft.Name());
                    Console.WriteLine("Got loadout/weaponsmask");
                    //Console.WriteLine("Loadout/weaponsmask: " + weaponsmask.ToString());
                    if (weaponsmask != null) Console.WriteLine("not null");
                    else Console.WriteLine("null");
                    if (weaponsmask.Count>2) Console.WriteLine("2");
                    else Console.WriteLine("3");
                    if (weaponsmask.Length > 2) Console.WriteLine("2");
                    else Console.WriteLine("3");
                    Console.WriteLine("weaponsmask  info:");
                    Console.WriteLine("   Count:    {0}", weaponsmask.Count);
                    Console.WriteLine("   Length:   {0}", weaponsmask.Length);
                    Console.WriteLine("   Values:");
                    Calcs.PrintValues(weaponsmask, 8);




                }

            });
       
    }
}

public override void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
{ 
    #region stb
    base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);
    //try
    {
        //stb_KilledActors.Add(actor, initiator, damageType); // Save all damages/damager info for stats purposes so it will be available if/when the actor is finally killed.
        //System.Console.WriteLine("Actor damaged: " + actor.Army() + " " + ((int)damageType).ToString() );
        //System.Console.WriteLine("Ground Actor damaged: " + actor.Army());
        bool willReportDamage = false;                                    

        if (initiator != null)
        {
            if (initiator.Player != null)
            {
                if (initiator.Player.Army() == 1 && actor.Army() == 2) { willReportDamage = true; }
                if (initiator.Player.Army() == 2 && actor.Army() == 1) { willReportDamage = true; }
            }
        }
        if (willReportDamage)
        {
            AiGroundActor ga = actor as AiGroundActor; //only do ground actors here because aircraft are done elsewhere
            if (ga != null)
            {
                if (ga.Type() == AiGroundActorType.AAGun || ga.Type() == AiGroundActorType.Artillery || ga.Type() == AiGroundActorType.Tank)
                {
                    StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 2 }, actor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst);
                }
                else if (ga.Type() == AiGroundActorType.ShipBattleship || ga.Type() == AiGroundActorType.ShipCarrier ||
                         ga.Type() == AiGroundActorType.ShipCruiser || ga.Type() == AiGroundActorType.ShipDestroyer ||
                         ga.Type() == AiGroundActorType.ShipMisc || ga.Type() == AiGroundActorType.ShipSmallWarship ||
                         ga.Type() == AiGroundActorType.ShipSubmarine || ga.Type() == AiGroundActorType.ShipTransport)
                {
                    StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 3 }, actor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst);
                } else //any other type of groundactor besides AA/Tank OR Ship = type 4 //bhugh
                {
                    StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 4 }, actor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst);
                }
            }
        }                                    
    }
    //catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    #endregion
    //add your code here
}

//On OnAircraftKilled is called when a plane is not longer flyable, for example if the pilot get killed or its too heavy damaged, but you get not the damager.
//OnActorDead is called later after a plane is hitting the ground or a GroundActor or AiPerson or AiGroup (a Ships is an AiGroup) is killed.And you can check the damagers.
//OnActorDestroyed is called every time a Actor is removed from the game, so its also called if a script removed a empty plane. So you should use OnAircraftKilled or OnActorDead.
//http://forum.1cpublishing.eu/archive/index.php/t-28611.html

HashSet<string> stb_aircraftKilled = new HashSet<string>(); //set of AiActor.Name()s

public override void OnAircraftKilled(int missionNumber, string shortName, AiAircraft aircraft)
{
    base.OnAircraftKilled(missionNumber, shortName, aircraft);

    //if it's ai controlled we consider it "killed" at this point, so go ahead & assign points.
    if (Stb_isAiControlledPlane(aircraft)) Stb_killActor((aircraft as AiActor), 30);
    else { //if it is player-controlled then we let nature take its course for now.  But we save the actor on a list & if it turns it it wasn't recorded later on then we record it later.

        //Stb_RemoveAllPlayersFromAircraft(AiAircraft aircraft, Player player, double timeToRemove_sec = 1.0)
        stb_aircraftKilled.Add((aircraft as AiActor).Name());
    }

}

public override void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
{
    base.OnActorDestroyed(missionNumber, shortName, actor);

    try
    {

        AiAircraft aircraft = actor as AiAircraft;

        if (aircraft != null)
        {
            Stb_KillACNowIfInAircraftKilled(aircraft); //In case this aircraft was listed as "killed" earlier it will count as a victory for the damagers but not a death for the player(s) in the a/c


            double Z_AltitudeAGL = aircraft.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);

            if (Z_AltitudeAGL < 5 && GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y) == LandTypes.WATER) // ON GROUND & IN THE WATER = DEAD    
            {
                //if (stb_Debug) Console.WriteLine("OnDestroy: " + actor.Name() + "'s destruction counts as a kill because on water.");
                Stb_killActor(actor); //it's dead, Jim

            }
            // crash landing in solid ground

            else if (Z_AltitudeAGL < 5 && GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y) != aircraft.Army())    // landed in enemy territory
            {
                //if (stb_Debug) Console.WriteLine("OnDestroy: " + actor.Name() + "'s destruction counts as a kill because ground in enemy territory.");
                Stb_killActor(actor); //Also dead; counts as a kill

            }
            else if (Z_AltitudeAGL < 5 && Stb_distanceToNearestAirport(actor) > 2000 )  // crash landed in friendly or neutral territory, on land, not w/i 2000 meters of an airport
            {                    
                //if (stb_Debug) Console.WriteLine("OnDestroy: " + actor.Name() + "'s destruction counts as a kill because on ground in friendly territory but not near an airport.");
                Stb_killActor(actor); //Also dead, or at least, counts as a kill for anyone who contributed to the crash landing?  That's how we're playing it for now . . . 

            }
        }


    }
    catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
}



public override void OnAircraftDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, NamedDamageTypes damageType)
{
    #region stb
    base.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
    try
    {
        //System.Console.WriteLine("Aircraft Actor damaged: " + aircraft.Army());

        StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 781 }); //player damaged, 781                                                                        
        bool willReportDamage = false;

        bool selfDam = stb_ContinueMissionRecorder.StbCmr_IsSelfDamage (aircraft, initiator);

        if (initiator != null)
        {
            if (initiator.Player != null)
            {                       
                if (initiator.Player.Army() == 1 && aircraft.Army() == 2) { willReportDamage = true; }
                if (initiator.Player.Army() == 2 && aircraft.Army() == 1) { willReportDamage = true; }
            }
        }
        if (willReportDamage)
        {
            if (initiator.Player != null) Stb_changeTargetOneAirgroupToPlayer(initiator.Player, aircraft, "damage");
            StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 1 }, initiator.Player.Place() as AiActor);
            stb_StatRecorder.StbSr_EnqueueTask(sst);
        }
    }
    catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    #endregion
    //add your code here
}

public override void OnAircraftCutLimb(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, LimbNames limbName)
{
    #region stb
    base.OnAircraftCutLimb(missionNumber, shortName, aircraft, initiator, limbName);
    try
    {

        StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 782 }); //player limb cut 782
        bool willReportCutLimb = false;

        bool ret = stb_ContinueMissionRecorder.StbCmr_IsSelfDamage (aircraft, initiator);


        if (initiator != null && aircraft != null)
        {
            if (initiator.Player != null)
            {
                //if ( ret ) Stb_Message(null, initiator.Player.Name() + " damaged self!", new object[] { });
                if (aircraft.Army() == 1 && initiator.Player.Army() == 2) { willReportCutLimb = true; }
                if (aircraft.Army() == 2 && initiator.Player.Army() == 1) { willReportCutLimb = true; }
            }
        }
        if (willReportCutLimb)
        {
            if (initiator.Player != null) Stb_changeTargetOneAirgroupToPlayer(initiator.Player, aircraft, "cutlimb");
            if (((int)limbName > 0) && ((int)limbName < 121))
            {
                StbStatTask sst = new StbStatTask(StbStatCommands.CutLimb, initiator.Player.Name(), new int[] { (int)limbName }, initiator.Player.Place() as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst);
            }
        }
    }
    catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    #endregion
    //add your code here
}


//Take care of logging write-offs, with a method to prevent double-counting.
//TODO: We could check whether or not there is any enemy damage to the a/c, then additionally track
//"self-write-offs" as a separate/additional category
//TODO: Write-offs when the player crashes & dies, may be be assigned to the 'next' life rather than the previous life.  Possibly because the death is recorded
//prior to the write-off.  Not sure how to fix this . . . 
new Dictionary<string, int> stb_ACWrittenOffAndTime = new Dictionary<string, int>();

public void stb_recordAircraftWrittenOff(Player player, AiActor actor, double injuries = 0, double distance = 0)
{
    int oldWrittenOffTime = 0;
    int currTime = Calcs.TimeSince2016_sec();
    if (!stb_ACWrittenOffAndTime.TryGetValue(player.Name(), out oldWrittenOffTime)) oldWrittenOffTime = 0;
    if (currTime - oldWrittenOffTime > 30)
    {  //prevents dup write-offs for same plane    
        stb_StatRecorder.StbSr_EnqueueTask(new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 845 }, actor));
        stb_SaveIPlayerStat.StbSis_AddSessStat(player, 845, 1);//Also save this for current session stats

        string reason = ". Distance to nearest friendly airport: " + distance.ToString("N0") + " meters.";
        if (injuries >= 0.5) reason = " due to damage. Damage severity: " + injuries.ToString();
        Stb_Message(new Player[] { player }, "Your aircraft was written off" + reason, new object[] { });
        Console.WriteLine("Forcing exit 4");
        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
        Console.WriteLine("Forcing exit 4a");
    }

    Console.WriteLine("Forcing exit 4b");
    stb_ACWrittenOffAndTime[player.Name()] = currTime;
    Console.WriteLine("Forcing exit 4c");
}


//We want to give bomber pilots a separate career.  Possibly we could give people different careers for red/blue
//as well in the future.  So this dictionary records the type of career the current pilot is in.  Called from
//placeEnter to set the pilot type & other places as necessary to read it.
//First string of Tuple is (bomber) or "" ; second string is unused for now but coudl be (red) or (blue)
//double is the current TIME as base.Time.current() - same system used in maddox.game.world.DamagerScore and the
//idea is that if we want, after an actor dies we could sort out which damages where done at what time and thus 
//assign them to the CORRECT career.  To do thise we'll need more of a catalog of all the player's careers
//during this mission along with time stamps for when they change, not just the most recent change.
Dictionary<string,Tuple <string,string,double>> stb_PilotType = new Dictionary<string, Tuple<string, string, double>>();


//OK, the following dictionary is designed to solve this problem:
// - if a player shoots or bombs an object (ac or ground object), then leaves the server or switches to a different a/c OR switches say from fighter to bomber, then later on the ac or ground object finally dies, the original player probably doesn't get credit because they actor they used to kill that particular ac or ground object no longer exists.  Or if it DOES exist it might be credited to the wrong career because the player has switched to a new career (say (bomber)) and the ondead routine has no way to know that

//However, there is still the problem that if the original damager actor is gone (ie, destroyed) when the damaged actor dies, then the onDead
//routine probably has no way to discover which player was linked to that damager actor, in order to give credit to the player. This requires more testing to determine exactly what happens in this situation.  What if the player has quite the server entirely at this point, or has left that aircraft (which is then destroyed rather quickly) and entered another a/c.
//A separate issue is that of knowing which of the player's CAREERs the credit should go to.  That is the problem the dictionary
//below is designed to solve. However, the complex structure below is probably overkill for that problem. All we need is a list
//of players that were (ever) in this a/c and also the relevant career tuple for that player.
//Below is more detailed & could be used to actually determine which player was in the a/c at which time, thus given even greater fine-grained control of who gets credit for what, except that it is missing
//one specific bit of info needed for that: What time the player LEFT the a/c.
Dictionary<string, Dictionary<string, List< Tuple<string, string, double>>>> stb_PilotTypeRecordbyAC = new Dictionary<string, Dictionary<string, List<Tuple<string, string, double>>>>();

public void stb_setPilotType(Player player, AiActor actor)
{

    if (player.Name() == null || player.Name() == "") return;

    AiAircraft aircraft = null;
    string acType = "";
    string playerBomberFighter = "";
    string playerTeam = "";
    if (actor is AiAircraft)
    {

        aircraft = actor as AiAircraft;
        acType = Calcs.GetAircraftType(aircraft);

    }
    else if (player.Place() != null && (player.Place() as AiAircraft) != null)
    {
        //Player player = actor as Player;
        aircraft = player.Place() as AiAircraft;
        acType = Calcs.GetAircraftType(aircraft);
    }

    //if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType == ("BlenheimMkIV") || acType == ("BlenheimMkIV_Late") )
    //Even though there is technically a Blenheim fighter we're calling it "bomber" for this purpose.  It carries (some) bombs.
    if (acType.Contains("Ju-88") || acType.Contains("He-111") || acType.Contains("BR-20") || acType.Contains("Blenheim"))
    {
        playerBomberFighter = " (bomber)";

    }

    //Save the info needed for career type of this player at this moment (generally, the moment of entering a position)
    stb_PilotType[player.Name()] = new Tuple<string, string, double>(playerBomberFighter, playerTeam, base.Time.current());
        Console.WriteLine("stb_setPilotType: " + playerBomberFighter + " " + playerTeam + " " + base.Time.current().ToString());

    //Save a record of all players who have entered this aircraft, along with time entered & other info about their career
    if (aircraft != null && player.Name() != null)
    {
        Dictionary<string, List<Tuple<string, string, double>>> currAircraftRecord = new Dictionary<string, List<Tuple<string, string, double>>>();

        if (!stb_PilotTypeRecordbyAC.TryGetValue(actor.Name(), out currAircraftRecord))
        {
            currAircraftRecord = new Dictionary<string, List<Tuple<string, string, double>>>();
        }

        List<Tuple<string, string, double>> currPilotRecord = new List<Tuple<string, string, double>>();

        if (!currAircraftRecord.TryGetValue(player.Name(), out currPilotRecord))
        {
            currPilotRecord = new List<Tuple<string, string, double>>();
        }

        currPilotRecord.Add(stb_PilotType[player.Name()]);
        currAircraftRecord[player.Name()] = currPilotRecord;
        stb_PilotTypeRecordbyAC[actor.Name()] = currAircraftRecord;
    }
}

public Tuple<string, string, double> stb_getPilotType(string playerName)
{
    Tuple<string, string, double> temp = new Tuple<string, string, double>("", "", 0);

    if (!stb_PilotType.TryGetValue(playerName, out temp))
    {
        temp = new Tuple<string, string, double >("", "", 0);
    }

    return temp;
}

public Tuple<string, string, double> stb_getPilotType(Player player)
{
    if (player == null || player.Name() == null || player.Name() == "") return new Tuple<string, string, double>("", "", 0);

    else return stb_getPilotType(player.Name());
}


public string stb_getPilotTypeString(string playerName)
{
    Tuple<string, string, double> tmp = stb_getPilotType(playerName);

    string tempStr = tmp.Item1 + tmp.Item2;

    return tempStr;
}

public string stb_getPilotTypeString(Player player)
{
    if (player == null || player.Name() == null || player.Name() == "") return "";

    else return stb_getPilotTypeString(player.Name());
}



//OK, CloD seems a bit too willing to hand out gruesome deaths sometimes.  IE when a player runs into a hangar wall at 5MPH.
//So we are going to convert some of those CLOD deaths into injury situations for our stats/career purposes.
public double stb_CalcExtentOfInjuriesOnActorDead(string playerName, int killType, AiActor actor, Player player, bool allowTakeBack=false)
{
    double injuries = 1; //default assumption is 1 = yup, they're dead
    AiAircraft aircraft1 = null;
    if (actor as AiAircraft != null) aircraft1 = actor as AiAircraft;
    else if (actor as AiPerson != null && aircraft1 == null)
    {

        AiActor place1 = (actor as AiPerson).Player().Place();
        if (place1 as AiAircraft != null) aircraft1 = place1 as AiAircraft;
        if (stb_Debug) System.Console.WriteLine("CalcInjuries: Person killed.");
        if (stb_Debug && aircraft1 != null) System.Console.WriteLine("CalcInjuries: Person killed was in aircraft.");
    }
    if (!allowTakeBack) {
        if (stb_Debug) System.Console.WriteLine("CalcInjuries: Death takeback not allowed here, so no calculation.");
        return injuries; //yup, they are dead
    }
    if (aircraft1 == null)
    {

        if (stb_Debug) System.Console.WriteLine("CalcInjuries: Not an aircraft, so no injury calc.");
        return injuries; //we're only going to turn some deaths into injuries for aircraft; if not an a/c or person in an a/c then it is just 1 = yup, they are dead
    }
    else
    {
        if (stb_Debug) System.Console.WriteLine("CalcInjuries: Calculating extent of injuries . . . ");
        //AiAircraft aircraft = actor as AiAircraft;
        //double Z_VelocityIAS = aircraft1.getParameter(part.ParameterTypes.Z_VelocityIAS, 0);
        //double Z_VelocityTAS = aircraft1.getParameter(part.ParameterTypes.Z_VelocityTAS, 0);
        double Z_VelocityMach = aircraft1.getParameter(part.ParameterTypes.Z_VelocityMach, 0);
        double VelocityMPH = Math.Abs(Z_VelocityMach) * 600; //this is an approximation but good enough for our purposes.
        //We use Z_VelocityMach because it seems more stable/predictable when passed through onDeadActor and also it is
        //unit invariant--it comes back as a % of mach whether we are using English or metric units
        //sometimes Z_VelocityMach is negative, which seems to indicate you are going backwards @ that speed.


        //double I_VelocityIAS = 0; // aircraft1.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
        double Z_AltitudeAGL = aircraft1.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
        //double S_GunReserve = aircraft1.getParameter(part.ParameterTypes.S_GunReserve, 0);
        //double S_GunClipReserve = aircraft1.getParameter(part.ParameterTypes.S_GunClipReserve, 0);
        //double S_GunReserve = 0; aircraft1.getParameter(part.ParameterTypes.S_GunReserve, 0);
        //double S_GunClipReserve = 0; aircraft1.getParameter(part.ParameterTypes.S_GunClipReserve, 0);

        Vector3d Vwld = aircraft1.AirGroup().Vwld();
        double vel_mps = Calcs.CalculatePointDistance(Vwld);
        double vel_mph = Calcs.meterspsec2milesphour(vel_mps);
        double heading = (Calcs.CalculateBearingDegree(Vwld));
        double pitch = Calcs.CalculatePitchDegree(Vwld);

        //So, pitch seems to work well for some aircraft (ie Hurricane) but almost always shows as "0.0" for others (ie Blennie)
        //So it seems too unreliable ot use here.  Also vel_mph & other data from Vwld seem unreliable in the case of a
        //a crash like this, perhaps for the same reason.  It is perhaps being sampled a bit too late to be of use to us here.


        AircraftParams maxAp = stb_AircraftParamStack.returnMaxes(player);



        /* //This is the old way of doing it
        //vel_mps vel_mph heading pitch

        if (Z_AltitudeAGL < 5 && VelocityMPH < 10) injuries = 0.1;
        else if (Z_AltitudeAGL < 4 && VelocityMPH < 30) injuries = 0.2;
        else if (Z_AltitudeAGL < 3 && VelocityMPH < 70) injuries = 0.5;
        else if (Z_AltitudeAGL < 2 && VelocityMPH < 100) injuries = 0.9;
        else injuries = 1;

        */
            /* http://boards.straightdope.com/sdmb/showpost.php?p=19162752&postcount=16 
             * 300 fpm or less = 3.5mph, good landing
             * 600-800 fpm = 8 mph, slightly hard landing
             * 1000 fpm = 11 mph, very hard, some damage
             * 1500 fpm = 17 mph, definite damage, edge of much damage
             * 2000 fpm = 23 mph, getting into serious damage
             * 
             */

            double vertSpeed_mph = Calcs.meterspsec2milesphour(maxAp.Vwld.z);
            if (vertSpeed_mph >= 0) vertSpeed_mph = 0;
            vertSpeed_mph = Math.Abs(vertSpeed_mph);
            double operativeVel_MPH = maxAp.vel_mph + 2 * vertSpeed_mph;
            if (Z_AltitudeAGL < 5 && operativeVel_MPH < 10) injuries = 0.1;
            else if (Z_AltitudeAGL < 5 && operativeVel_MPH < 30) injuries = 0.2;
            else if (Z_AltitudeAGL < 4 && operativeVel_MPH < 50) injuries = 0.5;
            else if (Z_AltitudeAGL < 4 && operativeVel_MPH < 70) injuries = 0.6;
            else if (Z_AltitudeAGL < 4 && operativeVel_MPH < 90) injuries = 0.8;
            else if (Z_AltitudeAGL < 4 && operativeVel_MPH < 110) injuries = 0.9;
            else if (Z_AltitudeAGL < 4 && operativeVel_MPH < 130) injuries = 0.95;
            else injuries = 1;

            if (vertSpeed_mph > 26) injuries = 1;
            else if (vertSpeed_mph > 23) injuries += 0.85;
            else if (vertSpeed_mph > 20) injuries += 0.75;
            else if (vertSpeed_mph > 15) injuries += 0.6;
            else if (vertSpeed_mph > 13) injuries += 0.4;
            else if (vertSpeed_mph > 12) injuries += 0.3;
            else if (vertSpeed_mph > 11) injuries += 0.2;

            if (injuries > 1) injuries = 1;

            //We would do some probability-based kills here rather than just injuries.  Like if they are 90% injured maybe they have a 25% probability of death or whatever.

            if (killType == 2 && injuries <= 0.2) injuries = injuries / 2; //If it is a self-injury on the ground and slow speed we are going to assume it is a fairly non-serious dumb thing the player did, and reduce the extent of injuries. If done by another person/player/actor, though we'll assume it is more serious 

            /*if (stb_Debug) System.Console.WriteLine("CalcInjuries: Calculating extent of injuries: " + injuries.ToString("0.0") + " for " + playerName + " Army " + actor.Army() + " ZvelocityMACH in MPH " + (Z_VelocityMach * 600).ToString("0.00000") + " altitude AGL " + Z_AltitudeAGL.ToString("0.00") + " vel_mps " + vel_mps.ToString("N1") + " vel_mph " + vel_mph.ToString("N1") + " heading " + heading.ToString("N1") + " pitch " + pitch.ToString("N1") + " Vwld: " + Vwld.x.ToString("N1") + " " + Vwld.y.ToString("N1") + " " + Vwld.z.ToString("N1")
                + " operativeVel_MPH " + operativeVel_MPH.ToString("N1") + " Mvel_mph " + maxAp.vel_mph.ToString("N1") + " MaxvertSpeed_mph: " + vertSpeed_mph.ToString("N1") + " Mpitch " + maxAp.pitch.ToString("N1")
                );
            */

        }
        if (stb_Debug) System.Console.WriteLine("CalcInjuries: Calculating extent of injuries: " + injuries.ToString("N1"));
        return injuries;
    }

    new Dictionary<string, int> stb_PlayerDeathAndTime = new Dictionary<string, int>();
    new Dictionary<string, int> stb_PlayerParachute_Crashed_LandedTime = new Dictionary<string, int>();

    //returns a double ranging 0 - 1.  0 means NO DEATH at all because it was previously recorded OR we have decided that even though CLOD thinks this is a death, we don't
    //a number BETWEEN 0 and 1 indicates extent of injuries created by this incident, but as long as the return is less than 1 there is no death
    //Note that if actor is NOT an AiAircraft (ie, a Player or Person or whatever) then this will ALWAYS record a kill and not 
    //make any sort of determination about how hard or fatal the crash was.  This is a pretty important side effect that is used
    //in several places (player as AiActor)
    public double stb_RecordStatsForKilledPlayerOnActorDead(string playerName, int killType, AiActor actor, Player player, bool allowTakeBack=false)
    {
        try
        {
            double recordedInjuries = 0;
            int oldDeathTime = 0;
            int currTime = Calcs.TimeSince2016_sec();
            if (!stb_PlayerDeathAndTime.TryGetValue(playerName, out oldDeathTime)) oldDeathTime = 0;
            if (currTime - oldDeathTime > 60)
            { //This keeps you from killing yourself more than 1X per 60 seconds, more particularly avoids recording the same death more than once.
                recordedInjuries = stb_CalcExtentOfInjuriesOnActorDead(playerName, killType, actor, player, allowTakeBack);
                bool bothPositionsDead = true;

                //We handle the case where the player is occupying two positions and one of them is dead but the other still alive
                //Logic is a bit complex because we have to first check the person is occupying two positions, THEN that exactly ONE of those two is still alive
                //Note that ^ is EXCLUSIVE OR operator, guaranteeing that one but not both of the player's positions still has some health.
                if (player.PersonPrimary() != null && player.PersonSecondary() != null && 
                    (player.PersonPrimary().Health > 0 ^ player.PersonSecondary().Health > 0))
                {
                    //Console.WriteLine("OnPlayerDead: Primary: " + player.PersonPrimary().Cart().InternalTypeName());
                    //Console.WriteLine("OnPlayerDead: Secondary: " + player.PersonSecondary().Cart().InternalTypeName());
                    //Console.WriteLine("OnPlayerDead: Primary: " + player.PersonPrimary().Health);
                    //Console.WriteLine("OnPlayerDead: Secondary: " + player.PersonSecondary().Health);
                    bothPositionsDead = false;
                }

                //Console.WriteLine("OnPlayerDead: Player " + playerName + " both dead " + bothPositionsDead.ToString() + " ");

                //Console.WriteLine("OnPlayerDead: primary:" + player.PersonPrimary().Health.ToString("n1") + " secondary: " + player.PersonSecondary().Health.ToString("n1"));

                if (recordedInjuries == 1 && bothPositionsDead)
                {

                    StbStatTask sst1 = new StbStatTask(StbStatCommands.PlayerKilled, playerName, new int[] { killType }, actor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst1);
                    stb_PlayerDeathAndTime[playerName] = currTime;
                    Console.WriteLine("Forcing exit 6");
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forces 100% damage of aircraft/write-off
                    if (stb_Debug) Console.WriteLine("OnPlayerDead: Player " + playerName + " death WAS recorded because previous death recorded " + (currTime - oldDeathTime).ToString() + " seconds ago & injuries were severe & both positions were dead");

                }
                else if (recordedInjuries > 0 || !bothPositionsDead)
                {
                    //Console.WriteLine("OnPlayerDead: Player " + playerName + " injuries were recorded, but no death");
                    //we can do other things here, like save the injuries to the stats file and/or give them a timeout depending on how 
                    //serious the injuries were.  But for now, we just proceed since it wasn't an actual death.
                }
                if (recordedInjuries == 1  && !bothPositionsDead)
                {
                    //Console.WriteLine("OnPlayerDead: Player " + playerName + " had one position/place killed, but the other still remains alive.  So we record a death on the player's stats but don't actually kill this personality yet.");
                    StbStatTask sst0 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 778, 1 }, player.Place() as AiActor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst0);
                    stb_SaveIPlayerStat.StbSis_AddSessStat(player, 778, 1);//Also save this for current session stats
                    recordedInjuries = -1; //-1 means this position was killed but another position still remains alive, for now.
                }

                double dist = Stb_distanceToNearestAirport(actor);
                if (bothPositionsDead && (recordedInjuries >= 0.5 || dist > 2000))
                {
                    stb_recordAircraftWrittenOff(player, actor, recordedInjuries, dist);
                }

            }
            else
            {
                if (stb_Debug)  Console.WriteLine("OnPlayerDead: Player " + playerName + " death was NOT recorded because previous death recorded just " + (currTime - oldDeathTime).ToString() + " seconds ago");
                recordedInjuries = 0;  //no recorded injuries because this death was previously recorded & we're doing nothing new here
            }


            
            return recordedInjuries;    

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("stb_RecordStatsForKilledPlayerOnActorDead - Exception: " + ex.ToString());
            return 1;
        }
    }

    public void stb_RecordStatsOnActorDead(AiDamageInitiator initiator, int killtype, double score, double totalscore, AiDamageToolType toolType) {
        //stb_StatRecorder.StbSr_WriteLine("Recording Damage: {0} {1} {2} {3}", score, totalscore, killtype, toolType);
        

        //Save the percentage credit towards the kill (all kill types lumped together into one grand total)
        int percent_score = (int)Math.Round(score / totalscore * 100);
        if (percent_score < -3000) percent_score = -3000; //limit negative/removed points possible.  Just for sanity.
        int percent_score_norm = (int)Math.Round((double)percent_score / (double)100);  //normed to 1=1 victory (rather than 100% = one victory)

        int percent_score_fordead = 1;
        if (percent_score_norm < 0 ) percent_score_fordead = percent_score_norm; //allowing us to deduct points now.

        StbStatTask sst = new StbStatTask(StbStatCommands.Dead, initiator.Player.Name(), new int[] { killtype, percent_score_fordead }, initiator.Player.Place() as AiActor);
        stb_StatRecorder.StbSr_EnqueueTask(sst);

        //Save penalty points, if that is what these are (negative points)
        if (percent_score_norm < 0)
        {
            StbStatTask sst0 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 847, percent_score_norm }, initiator.Player.Place() as AiActor);
            stb_StatRecorder.StbSr_EnqueueTask(sst0);
            stb_SaveIPlayerStat.StbSis_AddSessStat(initiator.Player, 847, percent_score);//Also save this for current session stats
        }

        StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 798, percent_score }, initiator.Player.Place() as AiActor);
        stb_StatRecorder.StbSr_EnqueueTask(sst1);
        stb_SaveIPlayerStat.StbSis_AddSessStat(initiator.Player, 798, percent_score);//Also save this for current session stats

        //stb_StatRecorder.StbSr_WriteLine("1 Recording Damage: {0} {1} {2} {3}", score, totalscore, killtype, toolType);
        StbStatTask sst2 = new StbStatTask();
        //Award Total Victory, Shared Victory, or Assist (>=75%, 40%-75%, >0 <40% respectively) (all kill types lumped together into one grand total)
        if (percent_score >= 75) sst2 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 799 }, initiator.Player.Place() as AiActor);
        else if (percent_score >= 40) sst2 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 800 }, initiator.Player.Place() as AiActor);
        else if (percent_score > 0) sst2 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 801 }, initiator.Player.Place() as AiActor);
        //allowing for removal of victories for bombing/damaging civilian areas
        //Note that the -3000 puts a limit on the # of victories that can be removed using this system.
        else if (percent_score <= -75 && percent_score >= -3000) sst2 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 799, percent_score_norm }, initiator.Player.Place() as AiActor);

        if (percent_score > 0 || (percent_score <= -75 && percent_score >= -3000)) stb_StatRecorder.StbSr_EnqueueTask(sst2);

        //stb_StatRecorder.StbSr_WriteLine("2Recording Damage: {0} {1} {2} {3}", score, totalscore, killtype, toolType);
        //Save the percentage credit towards the kill (separating out each individual kill type - air, AA/Tank, Naval, Ground)
        StbStatTask sst4 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 798 + killtype*4, percent_score }, initiator.Player.Place() as AiActor);
        stb_StatRecorder.StbSr_EnqueueTask(sst4);

        stb_SaveIPlayerStat.StbSis_AddSessStat(initiator.Player, 798 + killtype*4, percent_score);//Also save this for current session stats

        StbStatTask sst3 = new StbStatTask();
        //Award Total Victory, Shared Victory, or Assist (>=75%, 40%-75%, >0 <40% respectively) (separating out each individual kill type - air, AA/Tank, Naval, Ground)
        if (percent_score >= 75) sst3 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 799 + killtype * 4 }, initiator.Player.Place() as AiActor);
        else if (percent_score >= 40) sst3 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 800 + killtype * 4 }, initiator.Player.Place() as AiActor);
        else if (percent_score > 0) sst3 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 801 + killtype * 4 }, initiator.Player.Place() as AiActor);
        //allowing for removal of victories for bombing/damaging civilian areas
        else if (percent_score <= -75 && percent_score >= -3000) sst3 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 799 + killtype * 4, percent_score_norm }, initiator.Player.Place() as AiActor);
        if (percent_score > 0 || (percent_score <= -75 && percent_score >= -3000)) stb_StatRecorder.StbSr_EnqueueTask(sst3);

        //stb_StatRecorder.StbSr_WriteLine("3Recording Damage: {0} {1} {2} {3}", score, totalscore, killtype, toolType);
        //Save the raw damage points towards the kill (all kill types lumped together into one grand total as well as separated air/AA/naval/otherground)
        int rawscore = (int)Math.Round(score * 1000); //some raw scores are like .003843828
        StbStatTask sst5 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 818,rawscore }, initiator.Player.Place() as AiActor);
        stb_StatRecorder.StbSr_EnqueueTask(sst5);
        stb_SaveIPlayerStat.StbSis_AddSessStat(initiator.Player, 818, rawscore);//Also save this for current session stats
        StbStatTask sst6 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 818+killtype, rawscore }, initiator.Player.Place() as AiActor);
        stb_StatRecorder.StbSr_EnqueueTask(sst6);
        stb_SaveIPlayerStat.StbSis_AddSessStat(initiator.Player, 818+killtype, rawscore);//Also save this for current session stats

        //stb_StatRecorder.StbSr_WriteLine("4Recording Damage: {0} {1} {2} {3}", score, totalscore, killtype, toolType);
        //if (toolType.Equals(AiDamageToolType.Ordance)) stb_StatRecorder.StbSr_WriteLine("1. Recording BOMB Damage: {0} {1} {2} ", rawscore, killtype, toolType);
        //Save the raw damage points specifically for BOMBS towards the kill (all kill types lumped together into one grand total as well as separated air/AA/naval/otherground)
        if ((int)toolType==2 || toolType.Equals(AiDamageToolType.Ordance)) {            //not sure if toolType == AiDamageToolType.Ordance or similar works? But something like (int)toolType==2 definitely does
            StbStatTask sst7 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 830, rawscore }, initiator.Player.Place() as AiActor);
            stb_StatRecorder.StbSr_EnqueueTask(sst7);
            StbStatTask sst8 = new StbStatTask(StbStatCommands.Mission, initiator.Player.Name(), new int[] { 830 + killtype, rawscore }, initiator.Player.Place() as AiActor);
            stb_StatRecorder.StbSr_EnqueueTask(sst8);

            //stb_StatRecorder.StbSr_WriteLine("2. Recording BOMB Damage: {0} {1} {2} ", rawscore, killtype, toolType);
        }
    }

    HashSet<string> stb_deadActors = new HashSet<string>(); //actor.Name() of actors we have already run through the onActorDead routine.  Helps prevent us from giving double credit to damagers of actors who are killed.

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        #region stb
        base.OnActorDead(missionNumber, shortName, actor, damages);
        //try
        {
            //if (stb_Debug) Console.WriteLine("OnActorDead: 1");
            // Console.WriteLine("OnActorDead: 1");
            if (stb_deadActors.Contains(actor.Name())) {
                //if (stb_Debug) Console.WriteLine("OnActorDead: " + actor.Name() + "'s death was registered already; skipping double-count.");
                return; //This is an actor we've already 'killed' therefore we won't double count it.
                        //double-counting can happen e.g. when we get an OnAircraftKilled report then later actorDead for same a/c, or onCrashLanded then later actorDead, or whatever
            } else {
                //if (stb_Debug) 
                // Console.WriteLine("OnActorDead: 2");
                //if (stb_Debug) Console.WriteLine("OnActorDead: " + actor.Name() + "'s death has not yet been registered; registering now.");
                //if (stb_Debug) Console.WriteLine("Old list: " + string.Join(" | ", stb_deadActors));
                stb_deadActors.Add(actor.Name());
                //if (stb_Debug) Console.WriteLine("New list: " + string.Join(" | ", stb_deadActors));
            }

            //if (stb_Debug) 
            //Console.WriteLine("OnActorDead: 3");
            //stb_KilledActors.Add(actor, damages); // sav
            AiAircraft aircraft1 = null;
            if (actor as AiAircraft != null) aircraft1 = actor as AiAircraft;
            else if (actor as AiPerson != null && (actor as AiPerson).Player() != null) {

                AiActor place1 = (actor as AiPerson).Player().Place();
                if (place1 as AiAircraft != null) aircraft1 = place1 as AiAircraft;
            }

            //if (stb_Debug) 
            //Console.WriteLine("OnActorDead: 4");
            if (aircraft1 != null)
            {
                //AiAircraft aircraft = actor as AiAircraft;
                double Z_VelocityIAS = aircraft1.getParameter(part.ParameterTypes.Z_VelocityIAS, 0);
                double Z_VelocityTAS = aircraft1.getParameter(part.ParameterTypes.Z_VelocityTAS, 0);
                double Z_VelocityMach = aircraft1.getParameter(part.ParameterTypes.Z_VelocityMach, 0);
                double I_VelocityIAS = 0; // aircraft1.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
                double Z_AltitudeAGL = aircraft1.getParameter(part.ParameterTypes.Z_AltitudeAGL, 0);
                //double S_GunReserve = aircraft1.getParameter(part.ParameterTypes.S_GunReserve, 0);
                //double S_GunClipReserve = aircraft1.getParameter(part.ParameterTypes.S_GunClipReserve, 0);
                double S_GunReserve = 0;  aircraft1.getParameter(part.ParameterTypes.S_GunReserve, 0);
                double S_GunClipReserve = 0;  aircraft1.getParameter(part.ParameterTypes.S_GunClipReserve, 0);

                //System.Console.WriteLine("!!!!!ACTOR DEAD!!!!!: " + shortName + " Army " + actor.Army() + " velocityias " + Z_VelocityIAS.ToString("0.00") + " velocityTAS " + Z_VelocityTAS.ToString("0.00") + " ZvelocityMACH in MPH " + (Z_VelocityMach*600).ToString("0.00000")+  " IvelocityIAS " + I_VelocityIAS.ToString("0.00") + " altitude AGL " + Z_AltitudeAGL.ToString("0.00") + " S_GunReserve " + S_GunReserve.ToString("0.0") + " S_GunClipReserve " + S_GunClipReserve.ToString("0.0"));

            }


            //if (stb_Debug) 
            //Console.WriteLine("OnActorDead: 5");
            string msg = "killed by ";
            bool selfKill=true; 
            List<string> deadPlayerNames = new List <string>();
            List<Player> deadPlayers = new List<Player>();
            //if (stb_Debug) Console.WriteLine("OnActorDead: 6");

            /* AiDamageinitiator  has these attributes (possibly not all of them in every case, though? Like, sometimes there isn't a Player because it is AI instead)
            this.Actor = Actor;
            this.Person = Person;
            this.Player = Player;
            this.Tool = Tool;
            Tool.Name() string
            Tool.Type() maddox.game.world.AiDamageToolType 
            maddox.game.world.AiDamageToolType  enum Cannon Collision Ordance Unknown  [NOTE spelling for "Ordance" NOT "Ordnance"]
            */
            //if (stb_Debug) Console.WriteLine("OnActorDead: 6");
            double totalscore = 0;
            foreach (DamagerScore ds in damages)
            {
                
                string iName = "ai";
                if (ds.initiator.Player is Player && ds.initiator.Player != null && ds.initiator.Player.Name() != null) iName=ds.initiator.Player.Name();
                string typename=ds.initiator.Actor.Name();
                int armyV = actor.Army();
                int armyI = ds.initiator.Actor.Army();
                if (armyV != armyI) totalscore += ds.score; //only count damage caused by the other army in the total!  Mostly damage from own army will be self-damage, but even if it isn't, we won't count it . . .
                //stb_StatRecorder.StbSr_WriteLine("Actor {0} killed; Damager {1} {2} {3} {4} {5} | {6} {7} {8}", shortName, iName, ds.score, ds.time, typename, armyI, armyV, actor.Name(), ds.initiator.Tool.Type);
            }
            if (totalscore == 0) totalscore = 0.0001; //avoid division by zero errors.  If totalscore==0 then the individual scores must all be 0 also, so it doesn't really matter what its value is, but we don't want it to be zero since we divide by totalscore later . . . 
            //Console.WriteLine("Actor {0} killed; total damage {1} ", shortName, totalscore);

            //if (stb_Debug) Console.WriteLine("OnActorDead: 7");
            AiAircraft aiAircraft = actor as AiAircraft;
            if (aiAircraft != null)
            {
                //Get total amt of damage recorded for the kill


                //get list of player names in the a/c
                for (int i = 0; i < aiAircraft.Places(); i++)
                {
                    if (aiAircraft.Player(i) is Player && aiAircraft.Player(i) != null && aiAircraft.Player(i).Name() != null)
                    {
                        deadPlayerNames.Add(aiAircraft.Player(i).Name());
                        deadPlayers.Add(aiAircraft.Player(i));
                        msg = aiAircraft.Player(i).Name() + " " + msg;
                    }
                }

                //So, this reports 1 kill for everyone who had any damage at all on the kill. "Any Kill Participation" stat.
                // Damager score has 4 elements AiActor actor, AiDamageInitiator initiator, double score, time (the time the damage occured) (guessing this is type DateTime, but not sure)

                //if (stb_Debug) 
                //Console.WriteLine("OnActorDead: 8");
                foreach (DamagerScore ds in damages)
                {
                    bool willReportDead = false;
                    if (ds.initiator != null)
                    {
                        int initiatorArmy = 0;
                        if (ds.initiator.Actor != null && ds.initiator.Actor.Army() != null) initiatorArmy = ds.initiator.Actor.Army();
                        if (ds.initiator.Player != null && ds.initiator.Player.Army() != null) initiatorArmy = ds.initiator.Player.Army();


                        if (ds.initiator.Player != null)
                        {
                            if (aiAircraft.Army() == 1 && initiatorArmy == 2) { willReportDead = true; } //Only report death to stats if an actual player was involved AND the player was from the opposing army.  IF only AI were involved in the death there is no point in saving their stats
                            if (aiAircraft.Army() == 2 && initiatorArmy == 1) { willReportDead = true; }

                            msg += ds.initiator.Player.Name() + " ";
                            if (!deadPlayerNames.Contains(ds.initiator.Player.Name())) selfKill = false; // if even one other player contributed to the kill, it's not a self kill! 
                        }
                        else
                        {
                            selfKill = false; // if an AI (ie, non player) contributed to the kill, it's not a self=kill
                            msg += "nobody/AI  ";
                        }
                    }
                    if (willReportDead)
                    {
                        int dc = damages.Count();
                        if (dc == 0) dc = 1;
                        if (ds.initiator.Player != null) Stb_changeTargetOneAirgroupToPlayer(ds.initiator.Player, aiAircraft, "dead");
                        stb_RecordStatsOnActorDead(ds.initiator, 1, ds.score, totalscore, ds.initiator.Tool.Type); //type 1 is aerial kill
                    }
                }
                //Stb_LogError (msg); //OK, we can't really just do Stb_LogError.  Instead you must "prepare message" etc which queues it up.


                // if (player.Place () is AiAircraft)) {  //if player==null or not in an a/c we use the very first a/c encountered as a "stand-in"
                //p = player.Place() as AiAircraft;
                //if (stb_Debug) Console.WriteLine("OnActorDead: 9");
                for (int i = 0; i < aiAircraft.Places(); i++)
                {
                    //if (aiAircraft.Player(i) != null) return false;
                    if (aiAircraft.Player(i) is Player && aiAircraft.Player(i) != null && aiAircraft.Player(i).Name() != null)
                    {
                        string playerName = aiAircraft.Player(i).Name();
                        //StbSr_UpdateStatsForKilledPlayer(playerName);

                        if (stb_StatRecorder.StbSr_IsPlayerTimedOutDueToDeath(playerName) > 0) continue; //for pilots, skip recording the death if they are currently under a piloting ban. They only managed to kill themselves in the few seconds they are allowed in before disappearing

                        /*
                        StbStatTask sst1 = new StbStatTask(StbStatCommands.PlayerKilled, playerName, new int[] { killType }, aiAircraft.Player(i) as AiActor);
                        stb_StatRecorder.StbSr_EnqueueTask(sst1);
                        */


                            int killType = 1;
                        if (selfKill) killType = 2;

                        double injuriesExtent = stb_RecordStatsForKilledPlayerOnActorDead(playerName, killType, aiAircraft as AiActor, aiAircraft.Player(i), true);

                        bool RPCL = recentlyParachutedOrCrashedOrLanded_RO(playerName); //Trying to stop too many messages for bombers etc, but we don't record death YET as they may not be dead! We only know actually dead or not based on injuriesextent, but need the existing value as we process injuriesextent.
                        
                        //injurieExtent==1 means dead, 0 means no/no injury (or death already recorded),  between 0&1 means injured but not dead
                        //Later we can do fancy things depending on extent of injuries, but for now we're just ignoring it unless it's an actual death
                        if (injuriesExtent == 0) continue;  //If this death has already been recorded for this playerName then we skip doing all the same stuff again                    
                        else if (injuriesExtent < 1 && injuriesExtent > 0)
                        {
                            string severity = "slightly ";
                            if (injuriesExtent > .2) severity = "seriously ";
                            if (injuriesExtent >= .5) severity = "very seriously ";
                            string msg4 = stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + " was " + severity + "injured in that incredible terrible incident, but you somehow survived--for now . . . ";
                            if (!RPCL) Stb_Message(new Player[] { aiAircraft.Player(i) }, msg4, new object[] { });
                            stb_SaveIPlayerStat.StbSis_Save(aiAircraft.Player(i)); //Save the stats CloD has been accumulating
                            OnAircraftLanded(actor, aiAircraft.Player(i));
                            continue;
                        }
                        else if (injuriesExtent == -1) //case of (say) a bomber where the player is inhabiting two positions and one of them is killed, but the other not (-1)
                        {
                            string msg4 = "One of " + stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + "'s positions was killed, but the other lives on--for now . . . ";
                            if (!RPCL) Stb_Message(new Player[] { aiAircraft.Player(i) }, msg4, new object[] { });
                            stb_SaveIPlayerStat.StbSis_Save(aiAircraft.Player(i)); //Save the stats CloD has been accumulating

                        } else recentlyParachutedOrCrashedOrLanded(playerName); //OK< they are actually dead so now record the fact in the Dict to prevent too many multiple messages


                        msg = "";
                        if (selfKill)
                        {
                            //After player death they are demoted so showing "Tyro XXXX" was killed or whatever doesnt' really make sense, bec it was their PREVIOUS life/rank who was killed
                            //msg = "Self-kill: " + stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + ". ";
                            msg = "Self-kill: " + playerName + ". ";                            
                        }
                        else
                        {
                            //After player death they are demoted so showing "Tyro XXXX" was killed or whatever doesnt' really make sense, bec it was their PREVIOUS life/rank who was killed
                            //msg = stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + " killed or captured. ";
                            msg = playerName + " killed or captured. ";
                        }
                        stb_SaveIPlayerStat.StbSis_Save(aiAircraft.Player(i)); //Save the stats CloD has been accumulating

                        if (!RPCL) Stb_Message(new Player[] { aiAircraft.Player(i) }, msg, new object[] { });

                        if (stb_ResetPlayerStatsWhenKilled)
                        {
                            Stb_Message(new Player[] { aiAircraft.Player(i) }, "Notice: Your death was recorded. When you die, your stats and rank are reset and you begin a new career. Check stats at " + stb_LogStatsPublicAddressLow + " or in-game using commands <career.", new object[] { });
                        }
                        else
                        {
                            Stb_Message(new Player[] { aiAircraft.Player(i) }, "Your death was recorded. Check stats at " + stb_LogStatsPublicAddressLow + " or in-game using commands <career.", new object[] { });
                        }


                        if (stb_PlayerTimeoutWhenKilled)
                        {
                            msg += "To encourage a more realistic approach to piloting and battle, players who are killed are restricted from flying for " + Calcs.SecondsToFormattedString((int)(stb_PlayerTimeoutWhenKilledDuration_hours * 60 * 60)) + ". Please log off the server to allow others a chance to fly.";
                            if (!RPCL) Stb_Message(new Player[] { aiAircraft.Player(i) }, msg, new object[] { });
                        }

                        if (!RPCL && stb_PlayerTimeoutWhenKilled && stb_PlayerTimeoutWhenKilled_OverrideAllowed) Timeout(2.0, () => { Stb_Message(new Player[] { aiAircraft.Player(i) }, "If you are philosophically opposed to the idea of a timeout, enter the chat command <override to continue immediately.", null); });

                        Console.WriteLine("Forcing exit 7");
                        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(aiAircraft.Player(i), actor, 0, false, 1); //the final "1" forced 100% damage/death of aircraft



                        //And now that they have died, they can't continue their mission
                        //for stats purposes
                        stb_ContinueMissionRecorder.StbCmr_SavePositionDied(playerName);


                    }
                }




            }
            else
            {
                //if (stb_Debug) 
                //Console.WriteLine("OnActorDead: 10");
                AiGroundActor aiGroundActor = actor as AiGroundActor;
                if (aiGroundActor != null)
                {
                    //For now we're not recording deaths of players when they are playing in AiGround. We're just reporting kills for players who KILL an AiGround
                    if (shortName.Length == 12) { Timeout(99.9, () => { Stb_DestroyFrontShip(aiGroundActor); }); }
                    else if (shortName.Length == 14) { Timeout(99.9, () => { Stb_DestroyFrontArmor(aiGroundActor); }); }

                    //if (stb_Debug) Console.WriteLine("OnActorDead: 11");
                    foreach (DamagerScore ds in damages)
                    {
                        bool willReportDead = false;
                        if (ds.initiator != null)
                        {
                            if (ds.initiator.Player != null)
                            {
                                if (aiGroundActor.Army() == 1 && ds.initiator.Player.Army() == 2) { willReportDead = true; }
                                if (aiGroundActor.Army() == 2 && ds.initiator.Player.Army() == 1) { willReportDead = true; }
                            }
                        }
                        if (willReportDead)
                        {
                            if (aiGroundActor.Type() == AiGroundActorType.AAGun ||
                                aiGroundActor.Type() == AiGroundActorType.Artillery ||
                                aiGroundActor.Type() == AiGroundActorType.Tank)
                            {
                                //StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 2 }, ds.initiator.Player.Place() as AiActor);
                                //stb_StatRecorder.StbSr_EnqueueTask(sst);
                                if (willReportDead) stb_RecordStatsOnActorDead(ds.initiator, 2, ds.score, totalscore, ds.initiator.Tool.Type); //type 2 is aa, artillery, tank
                            }
                            else if (aiGroundActor.Type() == AiGroundActorType.ShipBattleship ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipCarrier ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipCruiser ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipDestroyer ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipMisc ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipSmallWarship ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipSubmarine ||
                                     aiGroundActor.Type() == AiGroundActorType.ShipTransport)
                            {
                                //StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 3 }, ds.initiator.Player.Place() as AiActor);
                                //stb_StatRecorder.StbSr_EnqueueTask(sst);
                                if (willReportDead) stb_RecordStatsOnActorDead(ds.initiator, 3, ds.score, totalscore, ds.initiator.Tool.Type); //type 3 is ships/naval
                            }
                            else //All other ground types except those specified above are type 4 //bhugh, 2016/09
                            {
                                //StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 4 }, ds.initiator.Player.Place() as AiActor);
                                //stb_StatRecorder.StbSr_EnqueueTask(sst);
                                if (willReportDead) stb_RecordStatsOnActorDead(ds.initiator, 4, ds.score, totalscore, ds.initiator.Tool.Type); //type 4 is any other ground actor kill except those listed above
                            }

                        }
                    }
                }
                else
                {

                    //if (stb_Debug) 
                    //Console.WriteLine("OnActorDead: 12");
                    //TODO: we need to do all the self-kill checking here as well; sometimes you can PK yourself . . . 
                    AiPerson person = actor as AiPerson;
                    Player player2 = actor as Player;
                    if (person != null || player2 != null) //two similar cases here: the actor is a "person" or the actor is a "player" - probably sent via killActor()
                    {
                        Player player = null;
                        if (person != null) player = person.Player();
                        else player = player2;
                        //Stb_Message(null, "Person died: " + person.Player().Name(), new object[] { });    

                        if (player != null && player.Name() != null)
                        {
                            //System.Console.WriteLine("Person died: " + player.Name());
                            string playerName = person.Player().Name();

                            bool RPCL = recentlyParachutedOrCrashedOrLanded(playerName);

                            int selfKillPers = 2;
                            string msg2 = "";


                            //StbSr_UpdateStatsForKilledPlayer(playerName);
                            if (stb_StatRecorder.StbSr_IsPlayerTimedOutDueToDeath(playerName) == 0) //for pilots, skip recording the death if they are currently under a piloting ban. They only managed to kill themselves somehow in the few seconds they are allowing in the aircraft.
                            {
                                foreach (DamagerScore ds in damages)
                                {

                                    if (ds.initiator != null)
                                    {
                                        if (ds.initiator.Player != null)
                                        {
                                            //msg += ds.initiator.Player.Name() + " ";
                                            if (!playerName.Contains(ds.initiator.Player.Name())) selfKillPers = 1; // if even one other player contributed to the kill, it's not a self kill! 

                                        }
                                        else
                                        {
                                            selfKillPers = 1; // if an AI (ie, non player) contributed to the kill, it's not a self=kill
                                            //msg += "nobody/AI  ";
                                        }
                                    }
                                    //We could report pilot kills etc here but for now we're not
                                }


                                /* StbStatTask sst1 = new StbStatTask(StbStatCommands.PlayerKilled, playerName, new int[] { 1 }); //1 = NORMAL DEATH, 2=SELF-KILL
                                    stb_StatRecorder.StbSr_EnqueueTask(sst1); */

                                //injuriesExtent==1 means dead, 0 means no/no injury (or death already recorded),  between 0&1 means injured but not dead
                                //Later we can do fancy things depending on extent of injuries, but for now we're just ignoring it unless it's an actual death

                                double injuriesExtent = stb_RecordStatsForKilledPlayerOnActorDead(playerName, selfKillPers, person as AiActor, person.Player(), true); //1 = NORMAL DEATH, 2=SELF-KILL

                                if (injuriesExtent == 1)  //If this death has already been recorded for this playerName then we skip doing all the same stuff again; if it hasn't been recorded yet then do all this:
                                {
                                    stb_SaveIPlayerStat.StbSis_Save(person.Player()); //Save the stats CloD has been accumulating

                                    if (selfKillPers == 2)
                                        msg2 = "Self-kill: " + stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + ". ";
                                    else msg2 = stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + " was killed or captured.";

                                    if (!RPCL) Stb_Message(new Player[] { player }, msg2, new object[] { });

                                    if (stb_PlayerTimeoutWhenKilled)
                                    {
                                        string msg1 = "To encourage a more realistic approach to piloting and battle, players who are killed or captured are restricted from flying for " + Calcs.SecondsToFormattedString((int)(stb_PlayerTimeoutWhenKilledDuration_hours * 60 * 60)) + ". You could try artillery or a ground vehicle.";
                                        if (!RPCL) Stb_Message(new Player[] { player }, msg1, new object[] { });

                                        if (stb_PlayerTimeoutWhenKilled && stb_PlayerTimeoutWhenKilled_OverrideAllowed) Timeout(2.0, () => { Stb_Message(new Player[] { player }, "If you are philosophically opposed to this idea, enter the chat command <override to continue immediately.", null); });

                                    }

                                    //And now that they have died, they can't continue their mission
                                    //for stats purposes
                                    stb_ContinueMissionRecorder.StbCmr_SavePositionDied(playerName);

                                }
                                else if (injuriesExtent > 0)
                                {
                                    string severity = "";
                                    if (injuriesExtent > .2) severity = "seriously ";
                                    if (injuriesExtent >= .5) severity = "very seriously ";
                                    string msg3 = stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + " was " + severity + "injured in that incredible fiery explosion, but somehow survived.  Your career will continue.";
                                    if (!RPCL) Stb_Message(new Player[] { player }, msg3, new object[] { });
                                    stb_SaveIPlayerStat.StbSis_Save(player); //Save the stats CloD has been accumulating
                                }
                                else if (injuriesExtent == -1) //case of (say) a bomber where the player is inhabiting two positions and one of them is killed, but the other not (-1)
                                {
                                    string msg4 = "One of " + stb_StatRecorder.StbSr_RankFromName(playerName, actor) + playerName + "'s positions was killed, but the other lives on--for now . . . ";
                                    if (!RPCL) Stb_Message(new Player[] { player }, msg4, new object[] { });
                                    stb_SaveIPlayerStat.StbSis_Save(player); //Save the stats CloD has been accumulating

                                }
                            }
                            //We could record how many persons (ai or human players) that players kill here, similarly to the way we track aircraft & ground
                            //kills above.  But it seems a bit creepy to do so?
                            //Anyway, that is why most airplane kills/crashes come through this routine 2X or more--once for the aircraft & once more 
                            //for each human/AI pilot, bombadier, etc on board.
                        }
                    }
                    else
                    {  //probably a static object or something?  We'll see . . . 
                        //if (stb_Debug) Console.WriteLine("OnActorDead: 13");
                        //if (willReportDead) stb_RecordStatsOnActorDead(ds.initiator, 4, ds.score, totalscore); //type 4 is any other ground kill except

                        //Console.WriteLine("OnActorDead: {0}'s death is not being recorded!", shortName);

                    }

                }
            }                

        }
        //catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    //public virtual void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary _stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    //GroundStationary: string .Name .pos string .Title AiGroundActorType .Type .IsAlive string .country string .Category

    public override void OnStationaryKilled(int missionNumber, maddox.game.world.GroundStationary stationary, maddox.game.world.AiDamageInitiator initiator, int eventArgInt)
    {
        #region stb
        base.OnStationaryKilled(missionNumber, stationary, initiator, eventArgInt);
        try
        {
            //stb_KilledActors.Add(actor, damages); // save 
            //System.Console.WriteLine("Actor dead: Army " + actor.Army() );
            string msg = "Stationary " + stationary.Name + " " + stationary.country + " " + stationary.Title + " " + stationary.Type.ToString() + " " + "killed by ";

            Player player = null;
            if (initiator != null && initiator.Player != null) player = initiator.Player;


            /* AiDamageinitiator  has these attributes (possibly not all of them in every case, though? Like, sometimes there isn't a Player because it is AI instead)
            this.Actor = Actor;
            this.Person = Person;
            this.Player = Player;
            this.Tool = Tool;
            */


            bool willReportDead = false;
            if (initiator != null)
            {
                if (initiator.Player != null)
                {
                        int statArmy = GamePlay.gpFrontArmy(stationary.pos.x, stationary.pos.y);
                        msg += initiator.Player.Name() + " army: " + initiator.Player.Army().ToString() + " statarmy: " + statArmy.ToString();
                        
                    //Ok, this is not really working as it should.  What we should do is use the stationary.pos() info 
                    //to check which side of enemy lines it is on, and if on the enemy side then we save it as as kill, ie
                    //if (player.Army() == GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y)) // friendly territory

                    //When the player makes a ground kill, we need to decide whether it was a friendly kill or an enemy kill.  This is not always very easy to do in CLOD.  For one thing, there isn't a method GroundStationary.Army() to just tell us what army they are in.  So we use a series of kludges, as outlined below.

                    //OK, the below lines don't work because CLOD does not report the stationary.country that the mission
                    //designer assigns (and it also doesn't report stationary.army at all, even though FMB allows it to 
                    // be set).  Instead, it reports the country as set internally somehow.  So e.g. in this .mis code:
                    //     Static109 Stationary.Environment.JerryCan_GER1_1 gb 269430.19 165952.20 720.00 /hstart -2
                    // Static109 will always be reported as DE even though the mission designer as specified it as gb
                    // And static110 will  always be reported as GB even though the mission designer as specified it as de
                    // Other stationaries are reported as NN or whatever, even though the mission designates them to a certain country/army
                    //  So for now, this code is remm-ed out and unused.
                    //Mission designers will need to consider that any stationaries simply belong to whichever army depending
                    //on which side of the front lines they are on
                
                    // if ((stationary.country == "de" || stationary.country == "it") && initiator.Player.Army() == 1) { willReportDead = true; }
                    //if ((stationary.country == "gb" || stationary.country == "us") && initiator.Player.Army() == 2) { willReportDead = true; }

                        //Since the scheme above does not work, we use this: We assume that we can tell whether they are enemy or friendly depending on WHICH SIDE OF THE FRONT THEY ARE ON.
                        //
                        //if ( (stationary.country == "nn") || (stationary.country == "fr") &&  
                        if 
                          (
                            (initiator.Player.Army() ==1 &&  statArmy == 2) || (initiator.Player.Army() == 2 && statArmy == 1)
                            
                          ) 
                            {
                        
                                    willReportDead = true;
                                    msg += "(enemy ground target)";

                            }        else {
                                    msg += "(friendly ground target)";
                            }                                        
                }                    
                else
                {                            
                    msg += "nobody/AI  ";
                }
            }

            int score = 1;

            if (stationary.Title.Contains("JerryCan_GER1") || stationary.Title.Contains("TelegaBallon_UK")) score = 4;
            if (willReportDead) stb_RecordStatsOnActorDead(initiator, 4, score, 1, initiator.Tool.Type); //type 4 is any other ground type
            //for actor deaths we get a score & we can total scores of various damage initiators to get at total kill
            //score.  But for these stationaries it just appears to be reported when they are killed, along with the
            //initiators.  So we are just fabricating a damage.score of 1 here & the complete 1 points goes to the actor reported in 
            //initiator, resulting in 1 kill pt (100%) per ground target killed.  Special target jerryan, telegaballon (hydrogen tank0 used to designate high-value
            //targets, get more points
            //if (willReportDead) stb_RecordStatsOnActorDead(initiator, 4, 10, 5, initiator.Tool.Type); //moving it to 2 pts/200% per tround stationary killed

            //Report ground kills but spread them out a bit in case many die @ once
            //TESTING: turning off ground target  hit messages to see if that helps our warping problem  9/28/2018
            Timeout(1 + stb_random.NextDouble() * 25, () => { if (score > 0) GamePlay.gpLogServer(new Player[] { player }, "Ground Target Destroyed: " + score.ToString("n1") + " points", new object[] { }); });

            //Stb_LogError(msg);
            //if (willReportDead) 
            Console.WriteLine(msg);

        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    //For various overrides below, we use these codes to collect info
    // 770 Takeoff 
    // 771 Safe Landing 
    // 772 Crash Landing
    // 773 Health Damaged
    // 774 Parachute Failed
    // 775 Parachute Landing
    // 776 Player Connected
    // 777 Player Disconnected
    // 778 Player Death
    // 779 Continuous Missions Count (ie, several connected sorties
    // 780 Player moved
    // 781 Player a/c damage (number of times)
    // 844 Sortie count (sorties at least 5 minutes in duration)

    public void OnAircraftCrashLanded(AiActor actor, Player player, AiAircraft aircraft=null, double injuries = 0)
    {
        try
        {
            bool playerDied = false;

            string PlayerName = "";
            string PlayerNameM = "";

            Console.WriteLine("OACL starting: " + PlayerNameM);

            if (player != null && player.Name() != null)
            {
                PlayerName = player.Name();
                PlayerNameM = stb_StatRecorder.StbSr_MassagePlayername(PlayerName, actor); //Name massaged with (bomber) etc.
                if (recentlyParachutedOrCrashedOrLanded(PlayerName)) return;
            }

            /*Point3d pos;
            if (actor != null) pos = actor.Pos();
            else pos = player.Pos(); */
            Console.WriteLine("OACL not recentlyPCL: " + PlayerNameM);

            if (player != null) // human pilot
            {                

                //OK, there really isn't ANY neutral water in CLOD, so I'm re-doing this
                //if (GamePlay.gpFrontArmy(aircraft.Pos().x, aircraft.Pos().y) == 0 &&
                //       GamePlay.gpLandType(aircraft.Pos().x, aircraft.Pos().y) == LandTypes.WATER) // crash-landed in neutral water
                //If landing in water, you chance of rescue goes up 25% if home waters, down 25% if enemy water
                //Would be cool to change chances if near land, near ASR, or whatever, vs far from them, but maybe next time . .. 
                if (GamePlay.gpLandType(actor.Pos().x, actor.Pos().y) == LandTypes.WATER) // crash-landed in water
                {

                    //Loss of a/c, even if life saved
                    Console.WriteLine("Forcing exit 8");
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off

                    double Luck = stb_random.NextDouble();
                    double Luck2 = stb_random.NextDouble();
                    double RescueChance = stb_ASR_RescueChanceRed;
                    if (player.Army() == 2) RescueChance = stb_ASR_RescueChanceBlue;

                    

                    if (player.Army() == GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y))
                        RescueChance = stb_ASR_RescueChanceFriendly* RescueChance;
                    else RescueChance = stb_ASR_RescueChanceEnemy* RescueChance;

                    if (Luck < RescueChance)
                    {   // ASR success
                        Console.WriteLine("Crashlanded Water: Luck " + Luck.ToString("F2") + " " + RescueChance.ToString("F2"));
                        if (player.Army() == GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y)) // friendly territory
                        {
                            gpLogServerAndLog(null, Calcs.randSTR(stb_ASR_RESCUE_MSG), new object[] { PlayerNameM });
                        }
                        else
                        {   // unfriendly
                            //Player has landed in enemy waters & picked up by enemy ASR but has a chance to escape
                            double EscapeChance = stb_POW_EscapeChanceRed;
                            if (actor.Army() == 2) EscapeChance = stb_POW_EscapeChanceBlue;
                            if (Luck2 > (1 - EscapeChance))
                            {   // player escaped capture
                                gpLogServerAndLog(null, Calcs.randSTR(stb_ASR_CAPTURE_MSG) + Calcs.randSTR(stb_ESCAPED_MSG), new object[] { PlayerNameM });
                            }
                            else
                            {
                                gpLogServerAndLog(null, Calcs.randSTR(stb_ASR_CAPTURE_MSG) + " " + Calcs.randSTR(stb_ASR_CAPTURE_MSG), new object[] { PlayerNameM });
                                playerDied = true;
                            }

                        }
                    }
                    else
                    {   // ASR failure
                        Console.WriteLine("Crashlanded Water: Luck " + Luck.ToString("F2") + " " + RescueChance.ToString("F2"));
                        gpLogServerAndLog(null, Calcs.randSTR(stb_ASR_FAIL_DROWNED), new object[] { PlayerNameM });
                        playerDied = true;
                    }
                }

                // crash landing in solid ground

                else if (GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y) != actor.Army())    // landed in enemy territory
                {

                    //Loss of a/c, even if life saved
                    Console.WriteLine("Forcing exit 8");
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off

                    //Only give consequences if the player leaves the a/c here, OR is injured on landing OR the a/c was killed
                    //Otherwise the pilot might just take off again & escape.
                    if (aircraft == null || injuries >= 0.35 || stb_aircraftKilled.Contains((aircraft as AiActor).Name())) {
                        double Luck = stb_random.NextDouble();
                        double EscapeChance = stb_POW_EscapeChanceRed;
                        if (player.Army() == 2) EscapeChance = stb_POW_EscapeChanceBlue;

                        if (Luck > (1 - EscapeChance))
                        {   // player escaped capture
                            gpLogServerAndLog(null, Calcs.randSTR(stb_CRASHLAND_ENEMY_MSG) + Calcs.randSTR(stb_ESCAPED_MSG), new object[] { PlayerNameM });
                        }
                        else
                        {   // player captured
                            gpLogServerAndLog(null, Calcs.randSTR(stb_CRASHLAND_ENEMY_MSG) + Calcs.randSTR(stb_CAPTURED_MSG), new object[] { PlayerNameM });
                            playerDied = true;
                        }
                    }
                }
                else // crash landed in friendly land territory
                {
                    if (injuries >= 0.2 || stb_aircraftKilled.Contains((aircraft as AiActor).Name())) {
                        gpLogServerAndLog(null, Calcs.randSTR(stb_CRASHLAND_FRIENDLY_MSG), new object[] { PlayerNameM });
                    } else {
                        gpLogServerAndLog(null, Calcs.randSTR(stb_LANDAWAYAIRPORT_SAFE_FRIENDLY_MSG), new object[] { PlayerNameM });
                    }

                }

                double dist = Stb_distanceToNearestAirport(actor);
                if (injuries >= 0.5 || stb_aircraftKilled.Contains((aircraft as AiActor).Name()))
                {
                    stb_recordAircraftWrittenOff(player, actor, injuries, dist);
                    
                    //SUPPLY: Loss of a/c, even if life saved
                    Console.WriteLine("Forcing exit 8");
                    if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off

                } else if (dist > 2000 && aircraft == null ) //this is the case where we are more than 2000 m from airport AND player has left  the a/c (aircraft == null) so we are being sent through this routine with no a/c attached
                  //Note that we DO NOT write off the a/c if the player has landed and is just sitting in it uninjured.  They might just take off again, which would be AOK if they can successfully do it.
                {
                    stb_recordAircraftWrittenOff(player, actor, injuries, dist);
                }

            }


  
            if (aircraft != null)
            {
                StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 772 });
                //Stb_Message(null, "Aircraft crash landed/stats: " + actor.Name(), new object[] { });
                stb_ContinueMissionRecorder.StbCmr_SaveOnCrashLanded(aircraft);
            } else {
                StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 772 }, player as AiActor);
                //Stb_Message(null, "Aircraft abandon: " + actor.Name(), new object[] { });                
                stb_StatRecorder.StbSr_EnqueueTask(sst1);
                

            }


            //we're taking care of several cases here: Where the aircraft has been killed & we want to kill all players, when called after a player has left the position/aircraft suddenly, etc
            //If coming from placeLeave then aircraft==null because the player has already left it. Actor in that situation is the "person".

            //TODO: We need to take care of one more situation here: Where the a/c is landed purposefully away from an airport & the
            //pilot wishes to take off again.  So check if plane is undamaged or if engine is still running or something?  And
            //only actually remove the player and destroy the a/c if the plane is damage or the engine is turned off or whatever.
            if (aircraft == null || !Stb_isAiControlledPlane(aircraft)) //aircraft==null means we came here from onplaceleave, with just a player & human actor, but the a/c is already ai controlled as the player has exited
            { //if it is player-controlled then we kick them out rather quickly, then the aircraft is counted as killed
              //This means that the damagers get credit for the victory, but the player has saved his/her life
                if (playerDied == true)
                {
                    if (aircraft == null) stb_RecordStatsForKilledPlayerOnActorDead(player.Name(), 2, player as AiActor, player, false); //They are dead, so kill immediately (ie, before players are removed from a/c)
                    else Stb_killActor((aircraft as AiActor), 0);
                    if (aircraft != null) Stb_RemoveAllPlayersFromAircraft(aircraft, 5);
                }
                else if (aircraft != null && ( injuries > 0.35 || stb_aircraftKilled.Contains((aircraft as AiActor).Name()) )) {
                    Stb_killActor((aircraft as AiActor), 10); //Wait 10 seconds (ie, 5 seconds after players are removed), so that a/c is destroyed but not players killed
                    Stb_RemoveAllPlayersFromAircraft(aircraft, 5);

                    if (injuries > 0.35) Stb_Message(new Player[] { player }, PlayerNameM + " was injured in that crash - cannot continue." , new object[] { });
                    if (stb_aircraftKilled.Contains((aircraft as AiActor).Name())) Stb_Message(new Player[] { player }, PlayerNameM + "'s plane was damaged too heavily - cannot continue.",  new object[] { });
                }
            }


        }

        catch (Exception e)
        {   // write an error message
            Console.WriteLine("Error (Stats.onCrashLanded): " + e.Message);
        }
    }


    public override void OnAircraftCrashLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
		base.OnAircraftCrashLanded (missionNumber, shortName, aircraft);
        Player player = aircraft.Player(0); //need to do something different here, such as check all positions
        //if (Stb_isAiControlledPlane(aircraft)) Console.WriteLine("OnAcCrashLanded: AI controlled");
        //else Console.WriteLine("OnAcCrashLanded: Player controlled");
        

        try
        {
            AiActor actor = aircraft as AiActor;
            //Don't need to do this as the onplace leave handles it . . .
            OnAircraftCrashLanded(actor, player, aircraft);
            //Destroy all crashed AC after a decent period
            

            //if it's ai controlled we consider it "killed" at this point, so go ahead & assign points.
            //This may have a bug in that it kills live pilots as well as ai aircraft; needs testing.
            if (Stb_isAiControlledPlane(aircraft)) Stb_killActor((aircraft as AiActor), 30);

            /* else
            { //if it is player-controlled then we kick them out rather quickly, then the aircraft is counted as killed
              //This means that the damagers get credit for the victory, but the player has saved his/her life
                if (playerDied == true) Stb_killActor((aircraft as AiActor), 0); //They are dead, so kill immediately (ie, before players are removed from a/c)
                else Stb_killActor((aircraft as AiActor), 10); //Wait 10 seconds (ie, 5 seconds after players are removed), so that a/c is destroyed but not players killed
                Stb_RemoveAllPlayersFromAircraft(aircraft, 5);
            }
            */

            Timeout(300, () =>
            { Stb_DestroyPlaneUnsafe(aircraft); }

            );

        }

        catch (NullReferenceException n)
        {
            // don't show null object errors in the server console 
        }
        catch (Exception e)
        {   // write an error message
            Console.WriteLine("Error (onCrashLanded): " + e.Message);
        }

        

    }    

    public void OnAircraftLanded (AiActor actor, Player player, AiAircraft aircraft = null, double injuries = 0) {
        if (player != null) // human pilot
        {

            Console.WriteLine("On aircraft landed (stats).");

            if (player == null || player.Name() == null) return; //no point in doing anything at all here in these cases
            string PlayerName = player.Name();
            string PlayerNameM = stb_StatRecorder.StbSr_MassagePlayername(PlayerName, actor); //Name massaged with (bomber) etc.

            if (GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y) != actor.Army())    // landed in enemy territory, presumably @ enemy airport or whatever
            {
                Console.WriteLine("On aircraft landed (stats), enemy land.");
                //Here we are forcing aircraft loss in case of landing in enemy territory.
                //This MIGHT duplicate the 'leaving aircraft' call above but will carry additional info of "and the plane was destroyed"
                if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off

                Console.WriteLine("On aircraft landed (stats), enemy land2.");
                //gpLogServerAndLog(null, Calcs.randSTR(stb_LANDED_ENEMY_MSG) + Calcs.randSTR(stb_CAPTURED_MSG), new object[] { PlayerNameM });
                double Luck = stb_random.NextDouble();
                double EscapeChance = stb_POW_EscapeChanceRed;
                if (actor.Army() == 2) EscapeChance = stb_POW_EscapeChanceBlue;

                if (Luck < EscapeChance)
                {   // player escaped capture
                    gpLogServerAndLog(null, Calcs.randSTR(stb_LANDED_ENEMY_MSG) + Calcs.randSTR(stb_ESCAPED_MSG), new object[] { PlayerNameM });
                    Console.WriteLine("On aircraft landed (stats), enemy land3.");
                }
                else
                {   // player captured
                    Console.WriteLine("On aircraft landed (stats), enemy land4.");
                    gpLogServerAndLog(null, Calcs.randSTR(stb_LANDED_ENEMY_MSG) + Calcs.randSTR(stb_CAPTURED_MSG), new object[] { PlayerNameM });
                    if (aircraft == null) stb_RecordStatsForKilledPlayerOnActorDead(PlayerName, 2, player as AiActor, player, false);
                    else Stb_killActor(actor, 0);
                    Console.WriteLine("On aircraft landed (stats), enemy land4a.");
                    //prevent double-counting/messages for a player who landed etc.
                    if (recentlyParachutedOrCrashedOrLanded(PlayerName))
                    {
                        Console.WriteLine("HI!");
                        return;
                    }
                    Console.WriteLine("On aircraft landed (stats), enemy land4b.");
                }

            }
            else   // landed in friendly territory
            {
                Console.WriteLine("On aircraft landed (stats), friendly land");
                if (player.Name() != null && !stb_ContinueMissionRecorder.StbCmr_IsForcedPlaceMove(player.Name()) ) gpLogServerAndLog(null, Calcs.randSTR(stb_LANDED_OK_MSG), new object[] { PlayerNameM });
            }

            double dist = Stb_distanceToNearestAirport(actor);
            if (injuries >= 0.5 || dist > 2000)
            {

                Console.WriteLine("On aircraft landed (stats), injuries or far from friendly a/p");

                stb_recordAircraftWrittenOff(player, actor, injuries, dist);

                //Here we are forcing aircraft loss in case of moderate to severe injuires OR landing away from airport
                //We'll usually pick this up elsewhere, but in this case we KNOW this airport is written off, so we're forcing it
                //This MIGHT duplicate the 'leaving aircraft' call above but will carry additional info of "and the plane was destroyed"
                //if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off
                Console.WriteLine("On aircraft landed (stats), injuries or far from friendly a/p2");
                recentlyParachutedOrCrashedOrLanded(PlayerName);
                Console.WriteLine("On aircraft landed (stats), injuries or far from friendly a/p3");
            } else
            {
                //They have returned the aircraft successfully!  If we were keeping track of aircraft stock etc we would return this aircraft to stock at this point.
            }

            Console.WriteLine("On aircraft landed (stats), exit 1");
        }
        Console.WriteLine("On aircraft landed (stats), exit 2");

    }

    public override void OnAircraftLanded (int missionNumber, string shortName, AiAircraft aircraft) 
    {
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
        //Stb_Message(null, "Aircraft landed/stats: " + shortName, new object[] { });
        
        StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 771 });

        Player player = aircraft.Player(0);   //actuall we need to do something different here--find where an active player is, or check all the positions, or something

        try
        {
            //don't do this now as we are handling it via onplaceleave 2017/09
            OnAircraftLanded(aircraft as AiActor, player, aircraft);

            Stb_KillACNowIfInAircraftKilled(aircraft); //In case this aircraft was listed as "killed" earlier it will count as a victory for the damagers but not a death for the player(s) in the a/c
            Timeout(300, () =>
            //{ destroyPlane(aircraft); } //Not sure why to destroy **ALL** planes just bec. landed?  Best to check if a pilot is still in it & just destroy aicontrolled planes, like this:

                { 
                    //destroyAiControlledPlane(aircraft);
                    Stb_DestroyPlaneSafe(aircraft);
                }
            );
        }
        catch (NullReferenceException n)
        {
            // don't show null object errors in the server console 
        }
        catch (Exception e)
        {   // write an error message
            Console.WriteLine("Error (OAL): " + e.Message);
        }    
         
    }
    
    public override void OnPersonHealth(maddox.game.world.AiPerson person, maddox.game.world.AiDamageInitiator initiator, float deltaHealth)
    {
        #region stats
        base.OnPersonHealth(person, initiator, deltaHealth);
        try
        {
          //Stb_Message(null, "Health Changed for " + person.Player().Name(), new object[] { });
          if (person != null ) {      
            Player player = person.Player();                
            //if (deltaHealth>0 && player != null && player.Name() != null) {
            if (player != null && player.Name() != null) {
              //System.Console.WriteLine("Stats: OnPersonHealth for " + player.Name());
              StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 773 }, player.Place() as AiActor);
              stb_StatRecorder.StbSr_EnqueueTask(sst1);
            }  
          }  
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonHealth - Exception: " + ex.ToString());
        }
        #endregion

    }

    public override void OnPersonParachuteFailed(maddox.game.world.AiPerson person)
    {
        #region stats
        base.OnPersonParachuteFailed(person);

        //We are basically ignoring the many parachute failures & pretending a reserve chute always opens @ the last moment
        if (person.Player() != null && person.Player().Name() !=null) Stb_Message(new Player[] { person.Player() }, "Luckily, at the last moment, " + person.Player().Name() + " deployed the reserve parachute.", new object[] { });

        /* //2016/01/19 - thanks to our new scheme, where exiting the plane (whether via parachute or just clicking to leave place) acts as parachuting
         * //we actually don't need to do anything here. IN fact we don't WANT to do anything here because it will contradict what we did on PlaceLeave,
         * //where we already determined whether they will survive etc.
         * 
         * 
        try
        {
            double Luck = stb_random.NextDouble();
            bool playerDied = false;

            //we record the stat first, so that if the player is killed by this event the stat will be recorded under the correct life
            if (person != null && person.GetType().GetMethod("Player") != null)
            {
                Player player = person.Player();
                //System.Console.WriteLine("OnPersonParachuteLanded for " + player.Name());    
                if (player != null && player.Name() != null)
                {
                    StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 774 }, player.Place() as AiActor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst1);

                }
            }

            //So apparently if parachute fails CloD does not send this person through OnActorDead or any such. Instead it is up to 
            //us to do something right here about it.
            //So, to be nice we give some chance of opening the "reserve chute" or otherwise they die.  This is just bec. CloD seems to have a very high rate of parachute failure; we might want to be nicer (or meaner, whatever) in setting our chances for this mission.

            //Note that there can be a conflict with any other .mis files (ie, MAIN.cs or whatever) - they can choose to send the player through onActorDead at this point, contradicting what we are doing here.
            if (Luck > stb_ParachuteFailureRecoveryChance) playerDied = true;
            if (playerDied)
            {
                Stb_Message(new Player[] { person.Player() }, "Parachute failure. " + person.Player().Name() + " was killed.", new object[] { });
                Stb_killActor(person as AiActor, 0);
            }
            else
            {
                Stb_Message(new Player[] { person.Player() }, "Luckily, at the last moment, " + person.Player().Name() + " deployed the reserve parachute.", new object[] { });
                Battle.OnEventGame(GameEventId.PersonParachuteLanded, person, null, 0);
            }
        

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonParachuteFailed - Exception: " + ex.ToString());
        }
        */
        #endregion
    }

    //Return TRUE if the player has recently been through the Parachute and/or crash-landed routine
    //FALSE if they haven't died recently.  Also save the fact that they have recently done this, along with the time.
    public bool recentlyParachutedOrCrashedOrLanded(string PlayerName)
    {
        //If recently parachuted or crashlanded and decided on an outcome for it, don't do it all again for another 60 seconds . . . 
        Console.WriteLine("recentlyParachCrLanded: 1 " + PlayerName);
        int currTime = Calcs.TimeSince2016_sec();
        int oldParachuteTime = 0;
        if (stb_PlayerParachute_Crashed_LandedTime.ContainsKey(PlayerName)) oldParachuteTime = stb_PlayerParachute_Crashed_LandedTime[PlayerName];
        //if (!stb_PlayerParachute_Crashed_LandedTime.TryGetValue(PlayerName, out oldParachuteTime)) oldParachuteTime = 0;
        if (currTime - oldParachuteTime <= 60) {
            Console.WriteLine("recentlyParachCrLanded: YES for " + PlayerName);
            return true; }
        Console.WriteLine("recentlyParachCrLanded: 2 " + PlayerName);
        stb_PlayerParachute_Crashed_LandedTime[PlayerName] = currTime; //If we are going through the parachute thing now, save the time so we are stopped from doing it again for 60 seconds (to prevent multiple parachute landing incidents from happening in quick succession, with different outcomes, which has been happening for some unknown reason 
        return false;
    }

    //Return TRUE if the player has recently been through the Parachute and/or crash-landed routine
    //FALSE if they haven't died recently.  But DON'T save the fact for later.
    public bool recentlyParachutedOrCrashedOrLanded_RO(string PlayerName)
    {
        //If recently parachuted or crashlanded and decided on an outcome for it, don't do it all again for another 60 seconds . . . 
        int currTime = Calcs.TimeSince2016_sec();
        int oldParachuteTime = 0;
        if (!stb_PlayerParachute_Crashed_LandedTime.TryGetValue(PlayerName, out oldParachuteTime)) oldParachuteTime = 0;
        if (currTime - oldParachuteTime <= 60) return true;    
        return false;
    }

public void OnPersonParachuteLanded(AiActor actor, Player player, maddox.game.world.AiPerson person = null)
    {
        
        if (player == null || player.Name() == null) return; //no point in doing anything at all here in these cases
        string PlayerName = player.Name();
        string PlayerNameM = stb_StatRecorder.StbSr_MassagePlayername(PlayerName, actor); //Name massaged with (bomber) etc.
        if (recentlyParachutedOrCrashedOrLanded(PlayerName)) return;

        //All these result in loss of a/c, even if life saved
        Console.WriteLine("Forcing exit 1");
        if (TWCSupplyMission != null) TWCSupplyMission.SupplyOnPlaceLeave(player, actor, 0, false, 1); //the final "1" forced 100% damage of aircraft/write-off

        bool playerDied = false;
        if (GamePlay.gpLandType(actor.Pos().x, actor.Pos().y) == LandTypes.WATER) // landed in water
        {
            double Luck = stb_random.NextDouble();
            double Luck2 = stb_random.NextDouble();
            double RescueChance = stb_ASR_RescueChanceRed;
            string parachute_message = Calcs.randSTR(stb_PARACHUTED_FRIENDLY_MSG);
                    if (actor.Army() == 2) RescueChance = stb_ASR_RescueChanceBlue;

                    if (actor.Army() == GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y))
                        RescueChance = stb_ASR_RescueChanceFriendly* RescueChance;
                    else
                    {
                        RescueChance = stb_ASR_RescueChanceEnemy* RescueChance;
                        parachute_message = Calcs.randSTR(stb_PARACHUTED_ENEMY_MSG);
                    }


                    if (Luck < RescueChance)
                    {   // ASR success
                        Console.WriteLine("Parachute Water: Luck " + Luck.ToString("F2") + " " + RescueChance.ToString("F2"));
                        if (actor.Army() == GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y)) // landed in friendly waters
                        {
                            gpLogServerAndLog(null, parachute_message + " " + Calcs.randSTR(stb_ASR_RESCUE_MSG), new object[] { PlayerNameM });
                        }
                        else
                        {   // landed in enemy waters
                            //Player has landed in enemy waters & picked up by enemy ASR but has a chance to escape
                            double EscapeChance = stb_POW_EscapeChanceRed;
                            if (actor.Army() == 2) EscapeChance = stb_POW_EscapeChanceBlue;
                            if (Luck2 > (1 - EscapeChance))
                            {   // player escaped capture
                                gpLogServerAndLog(null, Calcs.randSTR(stb_PARACHUTED_ENEMY_MSG) + Calcs.randSTR(stb_ESCAPED_MSG), new object[] { PlayerNameM });
                            }
                            else
                            {
                                gpLogServerAndLog(null, parachute_message + " " + Calcs.randSTR(stb_ASR_CAPTURE_MSG), new object[] { PlayerNameM });
                                playerDied = true;
                            }
                        }
                    }
                    else
                    {   // ASR failure
                        Console.WriteLine("Parachute Water: Luck " + Luck.ToString("F2") + " " + RescueChance.ToString("F2"));
                        gpLogServerAndLog(null, parachute_message + " " + Calcs.randSTR(stb_ASR_FAIL_DROWNED), new object[] { PlayerNameM });
                        playerDied = true;
                    }
                }

                // landed on solid ground & enemy
                else if (GamePlay.gpFrontArmy(actor.Pos().x, actor.Pos().y) != actor.Army())
                {

                    double Luck = stb_random.NextDouble();
                    double EscapeChance = stb_POW_EscapeChanceRed;
                    if (actor.Army() == 2) EscapeChance = stb_POW_EscapeChanceBlue;

                    if (Luck > (1 - EscapeChance))
                    {   // player escaped capture
                        gpLogServerAndLog(null, Calcs.randSTR(stb_PARACHUTED_ENEMY_MSG) + Calcs.randSTR(stb_ESCAPED_MSG), new object[] { PlayerNameM });
                    }
                    else
                    {   // player captured
                        gpLogServerAndLog(null, Calcs.randSTR(stb_PARACHUTED_ENEMY_MSG) + Calcs.randSTR(stb_CAPTURED_MSG), new object[] { PlayerNameM });
                        playerDied = true;
                    }
                }
                else   // landed in friendly territory
                {
                    gpLogServerAndLog(null, Calcs.randSTR(stb_PARACHUTED_FRIENDLY_MSG), new object[] { PlayerNameM });
                }



            //if killed by any of the incidents above, here is where the player actually dies
            if (playerDied)
            {
                double res = 0;
                //2=self-kill, 1 = regular kill; we're going to say parachuting death is never a self-kill as you were
                //intending to parachute and survive, but got killed SOMEHOW regardless.
                if (person == null) res = stb_RecordStatsForKilledPlayerOnActorDead(player.Name(), 1, player as AiActor, player, false); //They are dead, so kill immediately (ie, before players are removed from a/
                else Stb_killActor(person as AiActor, 0);
                //Give an indication we're supposed to be killing this player
                gpLogServerAndLog(null, "{0} has been killed or captured - the end of a glorious career!  (" + res.ToString() + ")", new object[] { PlayerNameM });
            }

            //Stb_Message(null, "Parachute Landed " + person.Player().Name(), new object[] { });
            //Player player = person as Player;
            if (player != null && player.Name() != null)
            {
                int record = 775; //successful parachute
                if (playerDied) record = 774; //failed parachute
                StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { record }, player.Place() as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst1);

            
                stb_recordAircraftWrittenOff(player, actor, 1);
            
            }


}

public override void OnPersonParachuteLanded(maddox.game.world.AiPerson person)
{
#region stats
base.OnPersonParachuteLanded(person);
try
{
    /* if (person.Player() == null) Console.WriteLine("OnPARA: player is null" );
    else
    {   
        Player player= person.Player();
        Console.WriteLine("OnPARA: person.player.name:" + player.Name());
        if (player.Place() != null) Console.WriteLine("OnPARA: place " + player.Place().Name());
        else Console.WriteLine("OnPARA: place is NULL");
        if (player.PlacePrimary() != null) Console.WriteLine("onPARA: placeprimary " + player.PlacePrimary().ToString());
        else Console.WriteLine("OnPARA: placeprimary is NULL");
        if (player.PersonPrimary() != null) Console.WriteLine("onPARA: person.place " + player.PersonPrimary().Place().ToString());
        else Console.WriteLine("OnPARA: primary person is NULL");
        if (person as AiAircraft != null) Console.WriteLine("onPARA: Person is AIAIRCRAFT");
    }
    bool playerDied = false;

    string PlayerName = person.Player() != null ? person.Player().Name() : person.Name();
    */

        /* //2016/01/19 - thanks to our new scheme, where exiting the plane (whether via parachute or just clicking to leave place) acts as parachuting
         * //we actually don't need to do anything here. IN fact we don't WANT to do anything here because it will contradict what we did on PlaceLeave,
         * //where we already determined whether they will survive etc.
         * //Doing it there solves two problems:
         * //#1. What to do if the person exits their a/c prematuraly via placeleave--just treat it exactly like jumping out with a parachute at that moment
         * //#2. What to do if the person parachutes but then exits the game or enters a new aircraft before landing with the parachute.  Since we have 
         * //determined the outcome of the parachute jump the moment they jumped, we don't care if they exit later . . . 
         * 
        if (person != null)
        {
            OnPersonParachuteLanded(person as AiActor, person.Player(), person);
        }
        */

    }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonParachuteLanded - Exception: " + ex.ToString());
        }
        #endregion
    }


    public override void OnPlayerArmy(maddox.game.Player player, int army)
    {
        #region stats
        base.OnPlayerArmy(player, army);
        try
        {

            
            stb_SaveIPlayerStat.StbSis_Save(player); //Save the stats CloD has been accumulating. We need to call this (at MIN! when the player first arrives on the server & when the player leaves and/or the server shuts down. We also call it on player death.  Could be called more times at convenient intervals or whatever, which could prevent any stats from being lost in case of unexpected shutdown etc.

            //783 Joined Red army (Army 1)
            //784 Joined Blue army (Army 2)
            //785 Joined Army 3
            //786 Joined Army 4
            if (army >=1 && army <= 4) {
                int cmd=782 + army;  //1= red, 2=blue, 3 & 4 might be used sometimes?
                StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { cmd }, player.Place() as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst1);
            }

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPlayerArmy - Exception: " + ex.ToString());
        }
        #endregion

    } 

    
    public override void OnPlayerConnected(maddox.game.Player player)
    {
        #region stats
        base.OnPlayerConnected(player);
        try
        {
            //System.Console.WriteLine("OnPlayerConnected");
            StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 776 }, player.Place() as AiActor);
            stb_StatRecorder.StbSr_EnqueueTask(sst1);

            stb_SaveIPlayerStat.StbSis_Save(player); //Save the stats CloD has been accumulating. We need to call this (at MIN! when the player first arrives on the server & when the player leaves and/or the server shuts down. We also call it on player death.  could be called more times at convenient intervals or whatever, which could prevent any stats from being lost in case of unexpected shutdown etc.
        

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPlayerConnected - Exception: " + ex.ToString());
        }

        #endregion
        // Your code here
    }
    
    
    public override void OnPlayerDisconnected(maddox.game.Player player, string diagnostic)
    {
        #region stats
        base.OnPlayerDisconnected(player, diagnostic);
        try
        {
            System.Console.WriteLine("OnPlayerDisconnected");            
            StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 777 }, player.Place() as AiActor);
            stb_StatRecorder.StbSr_EnqueueTask(sst1);
            stb_SaveIPlayerStat.StbSis_Save(player); //Save the stats CloD has been accumulating

        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPlayerDisconnected - Exception: " + ex.ToString());
        }

        #endregion
        // Your code here
    }    
    
    public virtual void OnPersonMoved(maddox.game.world.AiPerson person, maddox.game.world.AiActor fromCart, int fromPlaceIndex){
        base.OnPersonMoved(person, fromCart, fromPlaceIndex);
        try
        {
           if (person != null) {
              Player player = person.Player();
              if (player != null && player.Name() != null) {
                StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, player.Name(), new int[] { 780 }, player.Place() as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst1);
              }
           } 
        }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnPersonMoved - Exception: " + ex.ToString());
        }
    
    }  

    //Do the processing on an a/c on takeoff. We should call this in two situations:
    //1. OnTookOff occurred
    //2. AirSpawn.  In case of AirSpawn set IsTakeOff=false
    public void ProcessAircraftTakeOff (AiAircraft aircraft, bool IsTakeOff=true) {

        //770 = take off
        if (IsTakeOff) StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 770 });

        for (int i = 0; i < aircraft.Places(); i++)
        {
            //if (aiAircraft.Player(i) != null) return false;
            if (aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null)
            {
                
                string playerName = aircraft.Player(i).Name();
                int TimedOut_seconds = stb_StatRecorder.StbSr_IsPlayerTimedOutDueToDeath(playerName);
                if (TimedOut_seconds > 0) continue; //this is a person who is banned from flying due to recent death or whatever; they can slip in for a few seconds via onActorCreated before being de-spawned

                Console.WriteLine ("Took Off / Start Sortie " + playerName);

                //Record start of a new sortie.  Many different schemes have been tried & failed, so what we are going to do is simply
                //count sorties by takeoffs.  This will count ONLY AIRCRAFT sorties, not tanks, AA, etc.
                //It should count whether they are airfield take-offs or air spanws.                
                var sst = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 844 }, aircraft.Player(i) as AiActor);
                //    sst = new Mission.StbStatTask(Mission.StbStatCommands.Mission, player.Name(), new int[] { 844, -1 }, player.Place() as AiActor);
                stb_StatRecorder.StbSr_EnqueueTask(sst);
                

                //If this is not a continuation mission, we increment the mission counter for this player                                 
                if (!stb_ContinueMissionRecorder.StbCmr_OnTookOff(playerName, aircraft as AiActor))
                {

                    //Console.WriteLine ("New Mission for " + playerName);
                    StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 779 }, aircraft.Player(i) as AiActor);
                    stb_StatRecorder.StbSr_EnqueueTask(sst1);
                };

            }
        }


        //779 = Mission 

        //check if this is a continuation of the previous flight from near the
        //same position the a/c previously landed etc.  If so, it's not a "new mission" but a continuation of the previous mission, so don't increment the mission counter.  If not, we increment the mission counter.


    }

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        #region stats
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
        /*try
        {

        }
        catch (Exception)
        {

            throw;
        }
        */
        try
        {
            /* 
           //770 = take off
            StbStatTaskAircraft(StbStatCommands.Mission, aircraft, new int[] { 770 });
            
            for (int i = 0; i < aircraft.Places(); i++)
            {
              //if (aiAircraft.Player(i) != null) return false;
              if ( aircraft.Player(i) is Player && aircraft.Player(i) != null && aircraft.Player(i).Name() != null ) {
                string playerName=aircraft.Player(i).Name();                  
                //Console.WriteLine ("Took Off / Start Sortie " + playerName);
                
                //If this is not a continuation mission, we increment the mission counter for this player                                 
                if ( ! stb_ContinueMissionRecorder.StbCmr_OnTookOff(playerName, aircraft as AiActor) )   
                {
                   
                   //Console.WriteLine ("New Mission for " + playerName);
                   StbStatTask sst1 = new StbStatTask(StbStatCommands.Mission, playerName, new int[] { 779 }, aircraft.Player(i) as AiActor);
                   stb_StatRecorder.StbSr_EnqueueTask(sst1);
                };    
                
              }    
            }
            */

            ProcessAircraftTakeOff(aircraft: aircraft, IsTakeOff: true);

                //779 = Mission 

                //check if this is a continuation of the previous flight from near the
                //same position the a/c previously landed etc.  If so, it's not a "new mission" but a continuation of the previous mission, so don't increment the mission counter.  If not, we increment the mission counter.

            }
        catch (Exception ex)
        {
            System.Console.WriteLine("Stats.OnAircraftTookOff - Exception: " + ex.ToString());
        }
        #endregion
        //add your code here
    }
	  	

}

//Not using this now - dead code
//
//KilledActorsWrapper Class by Salmo
//Keeps track of all damage to an actor so that you can retrieve it all upon actor death & award various stats
//http://theairtacticalassaultgroup.com/forum/archive/index.php/t-10653.html
//
//Clear() // clear the list of damage initiators, damagetypes, damagescores. eg KilledActors.Clear();
//Count // returns an integer value indicating the length of the KilledActor's list eg. KilledActors.Count;
//GetDamagerScore // returns a List<DamagerScore> for the killed actor eg KilledActors.GetDamagerScore(killedActor);
//GetDamageInitiators // returns the List<AiDamageInitiator> eg KilledActors.GetDamagerScore(killedActor);
//GetNamedDamageTypes; // returns the List<NamedDamageTypes> for the killed actor. eg .GetNamedDamageTypes(killedActor);
//
public class KilledActorsWrapper
#region KilledActorsWrapper class
{
    public KilledActorsWrapper()
    { // constructor
        m_KilledActors = new List<Tuple<AiActor, AiDamageInitiator, NamedDamageTypes, List<DamagerScore>>>();
    }

    private static readonly KilledActorsWrapper instance = new KilledActorsWrapper(); // only one instance of class allowed

    public static KilledActorsWrapper Instance
    {
        get { return instance; }
    }

    public void Add(AiActor killedActor, List<DamagerScore> damages)
    {
        //Console.WriteLine("ENTERED ADD METHOD A");
        Tuple<AiActor, AiDamageInitiator, NamedDamageTypes, List<DamagerScore>> tuple = new Tuple<AiActor, AiDamageInitiator, NamedDamageTypes,
        List<DamagerScore>>(killedActor, null, NamedDamageTypes.AirbrakeDriveFailure, damages);
        //List<DamagerScore>>(killedActor, null, null, damages);

        //Console.WriteLine("ENTERED ADD METHOD A-1");
        m_KilledActors.Add(tuple);
        //Console.WriteLine("ENTERED ADD METHOD A-2");
    }

    public void Add(AiActor killedActor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        //Console.WriteLine("ENTERED ADD METHOD B");
        Tuple<AiActor, AiDamageInitiator, NamedDamageTypes, List<DamagerScore>> tuple = new Tuple<AiActor, AiDamageInitiator, NamedDamageTypes,
        List<DamagerScore>>(killedActor, initiator, damageType, null);
        //Console.WriteLine("ENTERED ADD METHOD B-1");
        this.m_KilledActors.Add(tuple);
        //Console.WriteLine("ENTERED ADD METHOD B-2");
    }

    public int Count
    {
        get { return m_KilledActors.Count; }
    }

    public List<DamagerScore> GetDamagerScore(AiActor killedActor)
    {
        List<DamagerScore> damagerscore = new List<DamagerScore>();
        for (int i = 0; i < m_KilledActors.Count; i++)
        {
            if (m_KilledActors[i].Item1 == killedActor)
            {
                damagerscore.Add((DamagerScore)m_KilledActors[i].Item1);
            }
        }
        return damagerscore;
    }

    public List<AiDamageInitiator> GetDamageInitiators(AiActor killedActor)
    {
        List<AiDamageInitiator> damageinitiators = new List<AiDamageInitiator>();
        for (int i = 0; i < m_KilledActors.Count; i++)
        {
            if (m_KilledActors[i].Item1 == killedActor)
            {
                damageinitiators.Add((AiDamageInitiator)m_KilledActors[i].Item2);
            }
        }
        return damageinitiators;
    }

    public List<NamedDamageTypes> GetNamedDamageTypes(AiActor actor)
    {
        List<NamedDamageTypes> nameddamagetypes = new List<NamedDamageTypes>();
        for (int i = 0; i < m_KilledActors.Count; i++)
        {
            if (m_KilledActors[i].Item1 == actor)
            {
                nameddamagetypes.Add((NamedDamageTypes)m_KilledActors[i].Item3);
            }
        }
        return nameddamagetypes;
    }

    public void Clear()
    {
        m_KilledActors.Clear();
    }
    private List<Tuple<AiActor, AiDamageInitiator, NamedDamageTypes, List<DamagerScore>>> m_KilledActors { get; set; }
}
#endregion


public class StbRankToAllowedAircraft
{
    //The idea is that you unlock various aircraft by reaching certain ranks.  But a couple of a/c you can get earlier, simply by getting X kills.
    public Dictionary<string, int> stbRaa_AllowedAircraftByRank_Red; //string is a/c name & int is rank #
    public Dictionary<string, int> stbRaa_AllowedAircraftByRank_Blue;

    public Dictionary<string, int> stbRaa_AllowedAircraftByAce_Red; //string is a/c name & int is # of kills required 
    public Dictionary<string, int> stbRaa_AllowedAircraftByAce_Blue;


    bool stbRaa_LogErrors;
    string stbRaa_ErrorLogPath;
    StbStatRecorder stbcmr_StatRecorder;
    private Mission mission;

    public StbRankToAllowedAircraft(Mission mission, bool le, string elp)
    {


        stbRaa_AllowedAircraftByRank_Blue = new Dictionary<string, int>();
        stbRaa_LogErrors = le;
        stbRaa_ErrorLogPath = elp;
        this.mission = mission; //gets current instance of Mission for use later
                                //Mission.StbStatRecorder stbcmr_StatRecorder = new Mission.StbStatRecorder();

        StbRaa_init_AllowedAircraftByRank_Red();
        StbRaa_init_AllowedAircraftByRank_Blue();
        StbRaa_init_AllowedAircraftByAce_Red();
        StbRaa_init_AllowedAircraftByAce_Blue();

    }

    //Various inits
    private void StbRaa_init_AllowedAircraftByRank_Red()
    {
        stbRaa_AllowedAircraftByRank_Red = new Dictionary<string, int>(); //string is a/c name & int is rank #. That a/c is allowed at or above that particular rank level
                                                                          /*
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BlenheimMkIV", 0);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("HurricaneMkI_100oct", 0);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BeaufighterMkIF", 0);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("HurricaneMkI_FB", 0);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BlenheimMkIVF", 0);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("DH82A", 0);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BlenheimMkIV_Late", 1);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("SpitfireMkI", 1);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("DH82A-1", 1);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("DH82A-2", 2);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("He-111P-2", 2);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BlenheimMkIVF_Late", 3);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BR-20M", 6);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("SpitfireMkIa_100oct", 7);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("SpitfireMkI_100oct", 9);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("Ju-88A-1", 10);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("SpitfireMkIIa", 11);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("HurricaneMkI_dH5-20", 12);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("HurricaneMkI_dH5-20_100oct", 13);            
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("He-111H-2", 14);        
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("HurricaneMkI_100oct-NF", 15);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("SpitfireMkIa", 17);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("BlenheimMkIVNF", 20);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("SpitfireMkI_Heartbreaker", 21);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("G50", 22);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("Bf-110C-7Late", 24);
                                                                          stbRaa_AllowedAircraftByRank_Red.Add("Bf-109E-4N", 25);
                                                                          */
    }
    private void StbRaa_init_AllowedAircraftByRank_Blue()
    {
        /*
        stbRaa_AllowedAircraftByRank_Blue = new Dictionary<string, int>(); //string is a/c name & int is rank #. That a/c is allowed at or above that particular rank level            
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-1", 0);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-1B", 0); //E-1 isn't spawning in (?!) in TF 4.5 so we're putting this @ level 0 as a replacement.
        stbRaa_AllowedAircraftByRank_Blue.Add("G50", 0);
        stbRaa_AllowedAircraftByRank_Blue.Add("He-111P-2", 0);
        stbRaa_AllowedAircraftByRank_Blue.Add("DH82A", 0);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-6", 0);
        stbRaa_AllowedAircraftByRank_Blue.Add("DH82A-1", 1);
        stbRaa_AllowedAircraftByRank_Blue.Add("DH82A-2", 2);
        stbRaa_AllowedAircraftByRank_Blue.Add("Ju-88A-1", 2);
        stbRaa_AllowedAircraftByRank_Blue.Add("Ju-87B-2", 3);
        stbRaa_AllowedAircraftByRank_Blue.Add("He-111H-2", 4);            
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-3", 5);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-2", 5);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-4", 6);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4", 7);
        //stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-4N-DeRated", 9); //Eliminated in 4.5
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-4N", 8);
        stbRaa_AllowedAircraftByRank_Blue.Add("BR-20M", 10);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-3B", 13);
        //stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4N-DeRated", 14); //Eliminated in 4.5
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-7", 15);
        //stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-7Late", 16); //Eliminated in 4.5
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-4-NJG", 17);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4_Late", 18);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4N", 19);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4B_Late", 20);            
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4B", 21); //BlenheimMkIV        
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-109E-4N_Late", 22);
        stbRaa_AllowedAircraftByRank_Blue.Add("Bf-110C-4_Late", 23);
        stbRaa_AllowedAircraftByRank_Blue.Add("BlenheimMkIV", 24);            
        stbRaa_AllowedAircraftByRank_Blue.Add("HurricaneMkI_100oct",25);
        stbRaa_AllowedAircraftByRank_Blue.Add("SpitfireMkIIa",26);
        stbRaa_AllowedAircraftByRank_Blue.Add("SpitfireMkIa_100oct",27); 
        */
    }
    private void StbRaa_init_AllowedAircraftByAce_Red()
    {
        stbRaa_AllowedAircraftByAce_Red = new Dictionary<string, int>(); //string is a/c name & int is # of kills required to unlock it. That a/c is allowed at or above that particular # of kills.   
                                                                         /*
                                                                         stbRaa_AllowedAircraftByAce_Red.Add("SpitfireMkIa_100oct", 5);
                                                                         stbRaa_AllowedAircraftByAce_Red.Add("BlenheimMkIV_Late", 5);
                                                                         stbRaa_AllowedAircraftByAce_Red.Add("BeaufighterMkINF", 10);            
                                                                         stbRaa_AllowedAircraftByAce_Red.Add("BlenheimMkIVNF_Late", 15);
                                                                         */
        stbRaa_AllowedAircraftByAce_Red.Add("SpitfireMkIa_100oct", 5);
        stbRaa_AllowedAircraftByAce_Red.Add("SpitfireMkIIa", 8);

    }
    private void StbRaa_init_AllowedAircraftByAce_Blue()
    {
        stbRaa_AllowedAircraftByAce_Blue = new Dictionary<string, int>(); //string is a/c name & int is # of kills required to unlock it. That a/c is allowed at or above that particular # of kills.
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-4B", 5);
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-4N", 8);
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-4B_Late", 10);
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-4N_Late", 12);

        /*
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-3", 5);
        stbRaa_AllowedAircraftByAce_Blue.Add("BR-20M", 5);
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-4_Late", 10);    
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-110C-4Late", 15);
        stbRaa_AllowedAircraftByAce_Blue.Add("Bf-109E-4N_Late", 20);
        stbRaa_AllowedAircraftByAce_Blue.Add("Ju-87B-2", 10);            
        */

    }



    public bool StbRaa_isPlayerAllowedAircraft(AiAircraft aircraft, Player player, AiActor actor)
    {

        string playerName = this.mission.stb_StatRecorder.StbSr_MassagePlayername(player.Name());
        int rank = this.mission.stb_StatRecorder.StbSr_RankAsIntFromName(playerName);
        int numberofkills = this.mission.stb_StatRecorder.StbSr_NumberOfKills(playerName);
        string aircraft_type = Calcs.GetAircraftType(aircraft);

        //It turns out you can't use NULL in a dictionary key lookup, so here is how we handle it.
        //Not sure WHY we are getting null here at all, but whatever.
        //Update - I think it is if the player is put into a parachute, and also perhaps (?) things like ground vehicles
        if (!(actor is AiAircraft)) return true; //we don't restrict anything but AIRCRAFT here. Tanks, parachute, whatever else is OK.
        if (aircraft == null || aircraft_type == null)
        {
            if (this.mission.stb_restrictAircraftByKills || this.mission.stb_restrictAircraftByRank)
            {
                //if (mission.stb_Debug) Console.WriteLine("StbRaa_isPlayerAllowedAircraft: Reached with Aircraft==null");
                //if (player != null) mission.Stb_Message(new Player[] { player }, "Restricted from this aircraft because of your rank or ace level OR a general error has occured. Please report to the admins if you feel this is in error.", new object[] { });
                return false;
            }
            else return true;
        }

        //Console.WriteLine("Rank: {0} Kills: {1} Type: {2}", rank, numberofkills, aircraft_type);

        Dictionary<string, int> allowedAircraftByAce = stbRaa_AllowedAircraftByAce_Red;
        if (player.Army() == 2) allowedAircraftByAce = stbRaa_AllowedAircraftByAce_Blue;

        Dictionary<string, int> allowedAircraftByRank = stbRaa_AllowedAircraftByRank_Red;
        if (player.Army() == 2) allowedAircraftByRank = stbRaa_AllowedAircraftByRank_Blue;

        int required_kills;

        if (!allowedAircraftByAce.TryGetValue(aircraft_type, out required_kills)) required_kills = -1;  // -1 means this a/c wasn't listed in the table.  If not listed, it will be ALLOWED
                                                                                                        //Console.WriteLine("reqkills: {0}", required_kills);
                                                                                                        //if ( (!this.mission.stb_restrictAircraftByKills  && !this.mission.stb_restrictAircraftByRank) || numberofkills >= required_kills) return true;  //This a/c is allowed for this player due to # of kills 
        if (this.mission.stb_restrictAircraftByKills && required_kills != -1)
        {
            if (numberofkills >= required_kills) return true;  //This a/c is allowed for this player due to # of kills 
            else if (!this.mission.stb_restrictAircraftByRank) return false;  //Not allowed via #ofkills, but we still need to check ByRank if that is turned on.  Otherwise, we're done.
        }

        //if ( !this.mission.stb_restrictAircraftByRank) return true;  //This a/c is allowed for this player due to # of kills 

        int required_rank;

        if (!allowedAircraftByRank.TryGetValue(aircraft_type, out required_rank)) required_rank = -1;  //-1 means, this a/c not listed in the table
                                                                                                       //Console.WriteLine("reqrank: {0}", required_rank);
        if (!this.mission.stb_restrictAircraftByRank || rank == -1 || rank >= required_rank) return true;  //This a/c is allowed for this player due to #1 restriction not turned on or @2 this a/c not found in the table or #3. player's rank allows it

        return false;
    }

    public string StbRaa_ListOfAllowedAircraftForRank(int rank, int army)
    {

        Dictionary<string, int> allowedAircraftByRank = stbRaa_AllowedAircraftByRank_Red;
        if (army == 2) allowedAircraftByRank = stbRaa_AllowedAircraftByRank_Blue;

        string msg = "";
        string rankname = this.mission.stb_StatRecorder.StbSr_RankNameFromInt(rank, army);
        if (rankname == "") return ""; //if our rank is out of bounds etc we'll return ""

        msg += rankname + ": ";

        bool first = true;
        foreach (KeyValuePair<string, int> entry in allowedAircraftByRank)
        {
            if (entry.Value > rank) continue;
            if (!first) msg += ", ";
            msg += entry.Key;
            first = false;
        }
        return msg;

    }

    public string StbRaa_ListOfAllowedAircraftForRank(Player player)
    {
        string playerName = this.mission.stb_StatRecorder.StbSr_MassagePlayername(player.Name());
        int rank = this.mission.stb_StatRecorder.StbSr_RankAsIntFromName(playerName);
        int army = player.Army();
        return StbRaa_ListOfAllowedAircraftForRank(rank, army);
    }

    public string StbRaa_ListOfAllowedAircraftForAce(int kills, int army)
    {

        Dictionary<string, int> allowedAircraftByAce = stbRaa_AllowedAircraftByAce_Red;
        if (army == 2) allowedAircraftByAce = stbRaa_AllowedAircraftByAce_Blue;
        string msg = "";
        bool first = true;
        foreach (KeyValuePair<string, int> entry in allowedAircraftByAce)
        {
            if (entry.Value > kills) continue;
            if (!first) msg += ", ";
            msg += entry.Key;
            first = false;
        }
        return msg;
    }

    public string StbRaa_ListOfAllowedAircraftForAce(Player player)
    {
        string playerName = this.mission.stb_StatRecorder.StbSr_MassagePlayername(player.Name());
        int numberofkills = this.mission.stb_StatRecorder.StbSr_NumberOfKills(playerName);
        int army = player.Army();
        return StbRaa_ListOfAllowedAircraftForAce(numberofkills, army);
    }

    //List NEW a/c the player will have access to at a particular rank (or if none, keep skipping up to show the next promotion higher that actually gets a new a/c)
    //Note the rank given is CURRENT RANK; this will start searching for next a/c ad rank+1.
    public string StbRaa_ListOfAllowedAircraftForNextRank(int rank, int army)
    {
        rank++;
        Dictionary<string, int> allowedAircraftByRank = stbRaa_AllowedAircraftByRank_Red;
        if (army == 2) allowedAircraftByRank = stbRaa_AllowedAircraftByRank_Blue;
        string msg = "";
        string rankname = this.mission.stb_StatRecorder.StbSr_RankNameFromInt(rank, army);
        if (rankname == "") return ""; //if our rank is out of bounds etc we'll return ""

        msg += rankname + ": ";

        bool first = true;
        foreach (KeyValuePair<string, int> entry in allowedAircraftByRank)
        {
            if (entry.Value != rank) continue;
            if (!first) msg += ", ";
            msg += entry.Key;
            first = false;
        }
        if (!first) return msg;
        else return StbRaa_ListOfAllowedAircraftForNextRank(rank, army); //little recursion here, but it will always eventually exit bec. we check for out of bounds on the dictionary at each step.  So it will eventually hit a rank that has something to report, or go out of bounds and return ""
    }

    public string StbRaa_ListOfAllowedAircraftForNextRank(Player player)
    {
        int rank = this.mission.stb_StatRecorder.StbSr_RankAsIntFromName(player.Name());
        int army = player.Army();
        return StbRaa_ListOfAllowedAircraftForNextRank(rank, army);
    }

    //List a/c the player will have access to via getting more kills, if any
    public string StbRaa_ListOfAllowedAircraftForNextAce(int kills, int army)
    {

        Dictionary<string, int> allowedAircraftByAce = stbRaa_AllowedAircraftByAce_Red;
        if (army == 2) allowedAircraftByAce = stbRaa_AllowedAircraftByAce_Blue;
        string msg = "";
        bool first = true;
        foreach (KeyValuePair<string, int> entry in allowedAircraftByAce)
        {
            if (entry.Value <= kills) continue;
            if (!first) msg += ", ";
            msg += entry.Key + " (" + entry.Value.ToString() + " kills)";
            first = false;
        }
        return msg;
    }

    public string StbRaa_ListOfAllowedAircraftForNextAce(Player player)
    {
        string playerName = this.mission.stb_StatRecorder.StbSr_MassagePlayername(player.Name());
        int numberofkills = this.mission.stb_StatRecorder.StbSr_NumberOfKills(playerName);
        int army = player.Army();
        return StbRaa_ListOfAllowedAircraftForNextAce(numberofkills, army);
    }




    public void StbRaa_PrepareErrorMessage(Exception ex, string src = "")
    {
        if (stbRaa_LogErrors)
        {
            if (src == "") src = "General StbRaa error";
            ThreadPool.QueueUserWorkItem(new WaitCallback(StbRaa_LogError),
                //(object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message) + " Source: " + src);
                (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.ToString()));
        }
    }

    public void StbRaa_LogError(object data)
    {
        try
        {
            /*
            FileInfo fi = new FileInfo(stbRaa_ErrorLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(stbRaa_ErrorLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(stbRaa_ErrorLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
            */
            //TODO: Should just AppendAllText(    string path,    string contents ) instead of all the above
            if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX10", null); //testing disk output for warps
            string date = DateTime.UtcNow.ToString("u");
            Task.Run(() => File.AppendAllText(stbRaa_ErrorLogPath, "\n" + date + " - " + (string)data));
            //File.AppendAllText(stbRaa_ErrorLogPath, "\n" + date + " - " + (string)data);
        }
        //catch (Exception ex) { Console.WriteLine(ex.Message); };
        catch (Exception ex) { Console.WriteLine(ex.ToString(), "stbRAA_LE"); };
    }
} //class


public class StbStatRecorder
{
    //private readonly Mission outer; //allows us to reference methods etc from the Mission class as 'outer'        
    EventWaitHandle stbSr_Wh = new AutoResetEvent(false);
    Thread stbSr_Worker;
    readonly object stbSr_Locker = new object();
    Queue<Mission.StbStatTask> stbSr_Tasks = new Queue<Mission.StbStatTask>(2000);
    NumberFormatInfo stbSr_nf = new NumberFormatInfo();
    public Dictionary<string, int[]> stbSr_AllPlayerStats;
    public Dictionary<string, Tuple<string, DateTime>> stbSr_DeadPlayers;
    bool stbSr_LogStats;
    bool stbSr_LogStatsCreateHtmlLow;
    bool stbSr_LogStatsCreateHtmlMed;
    bool stbSr_LogStatsUploadHtmlLow;
    bool stbSr_LogStatsUploadHtmlMed;
    string stbSr_LogStatsUploadAddressLow;
    string stbSr_LogStatsUploadAddressExtLow;
    string stbSr_LogStatsUploadAddressMed;
    string stbSr_LogStatsUploadUserName;
    string stbSr_LogStatsUploadPassword;
    bool stbSr_LogErrors;
    string stbSr_ErrorLogPath;
    string stbSr_PlayerStatsPathTxt;
    string stbSr_PlayerStatsPathHtmlLow;
    string stbSr_PlayerStatsPathHtmlExtLow;
    string stbSr_PlayerStatsPathHtmlMed;
    public string stbSr_LogStatsUploadFilenameLow;
    public string stbSr_MissionServer_LogStatsUploadFilenameLow;
    public string stbSr_LogStatsUploadFilenameDeadPilotsLow;
    public string stbSr_LogStatsUploadFilenameTeamLow;
    public string stbSr_LogStatsUploadFilenameTeamPrevLow;
    public bool stbSr_ResetPlayerStatsWhenKilled;
    public bool stbSr_NoRankMessages;
    public bool stbSr_NoRankTracking;
    public bool stbSr_PlayerTimeoutWhenKilled;
    public double stbSr_PlayerTimeoutWhenKilledDuration_hours;
    private Mission mission;
    private Random stbSr_random;

    public int stbSr_numStats; //# of fields recorded in the stats Dictionary/File etc      



    public StbStatRecorder(Mission mission, bool logStats, bool logStatsCreateHtmlLow, bool logStatsCreateHtmlMed, string statsPathTxt,
                           bool logErrors, string errorLogPath, string statsPathHtmlLow, string statsPathHtmlExtLow, string statsPathHtmlMed,
                           bool logStatsUploadHtmlLow, bool logStatsUploadHtmlMed,
                           string logStatsUploadAddressLow, string logStatsUploadAddressExtLow, string logStatsUploadAddressMed,
                           string logStatsUploadUserName, string logStatsUploadPassword,
                           string logStatsUploadFilenameLow, string missionServer_LogStatsUploadFilenameLow, string logStatsUploadFilenameDeadPilotsLow,
                           string logStatsUploadFilenameTeamLow, string logStatsUploadFilenameTeamPrevLow,
                           bool ResetPlayerStatsWhenKilled, bool NoRankMessages, bool NoRankTracking, bool PlayerTimeoutWhenKilled,
                           double PlayerTimeoutWhenKilledDuration_hours = 3.00
                           )

    {
        stbSr_AllPlayerStats = new Dictionary<string, int[]>();
        stbSr_DeadPlayers = new Dictionary<string, Tuple<string, DateTime>>();
        stbSr_random = new Random();
        stbSr_LogStats = logStats;
        stbSr_LogStatsCreateHtmlLow = logStatsCreateHtmlLow;
        stbSr_LogStatsCreateHtmlMed = logStatsCreateHtmlMed;
        stbSr_LogStatsUploadHtmlLow = logStatsUploadHtmlLow;
        stbSr_LogStatsUploadHtmlMed = logStatsUploadHtmlMed;
        stbSr_LogStatsUploadAddressLow = logStatsUploadAddressLow;
        stbSr_LogStatsUploadAddressExtLow = logStatsUploadAddressExtLow;
        stbSr_LogStatsUploadAddressMed = logStatsUploadAddressMed;
        stbSr_LogStatsUploadUserName = logStatsUploadUserName;
        stbSr_LogStatsUploadPassword = logStatsUploadPassword;
        stbSr_PlayerStatsPathTxt = statsPathTxt;
        stbSr_PlayerStatsPathHtmlLow = statsPathHtmlLow;
        stbSr_PlayerStatsPathHtmlExtLow = statsPathHtmlExtLow;
        stbSr_PlayerStatsPathHtmlMed = statsPathHtmlMed;
        stbSr_LogStatsUploadFilenameLow = logStatsUploadFilenameLow;
        stbSr_MissionServer_LogStatsUploadFilenameLow = missionServer_LogStatsUploadFilenameLow;
        stbSr_LogStatsUploadFilenameDeadPilotsLow = logStatsUploadFilenameDeadPilotsLow;
        stbSr_LogStatsUploadFilenameTeamLow = logStatsUploadFilenameTeamLow;
        stbSr_LogStatsUploadFilenameTeamPrevLow = logStatsUploadFilenameTeamPrevLow;
        stbSr_ResetPlayerStatsWhenKilled = ResetPlayerStatsWhenKilled;
        stbSr_NoRankMessages = NoRankMessages;
        stbSr_NoRankTracking = NoRankTracking;
        stbSr_PlayerTimeoutWhenKilled = PlayerTimeoutWhenKilled;
        stbSr_PlayerTimeoutWhenKilledDuration_hours = PlayerTimeoutWhenKilledDuration_hours;

        this.mission = mission; //gets current instance of Mission for use later

        stbSr_LogErrors = logErrors;
        stbSr_ErrorLogPath = errorLogPath;
        stbSr_numStats = 850;  //If you increase this, increase by increments of **50** only - otherwise many errors!  Should be a multiple of 50.
        StbSr_ReadStatsFromFile();
        stbSr_Worker = new Thread(StbSr_Work);
        //stbSr_Worker.Priority = ThreadPriority.BelowNormal;
        stbSr_Worker.Start();
    }

    //Use this instead of Console.WriteLine(), which malfunctions silently when called in a 2nd/background thread
    //We can send the output to the errorlog file OR the CloD window here
    public void StbSr_WriteLine(string format, params object[] values)
    {

        if (this.mission.stb_Debug) this.mission.Stb_Message(null, format, values); //only for debugging, it's quite verbose & goes to the CloD window, not console

        //FYI the below plan didn't work out
        //string str = format;
        //if (values != null) 
        //string str = String.Format(format, values);
        //Using logerror doesn't work because of thread conflicts
        //StbSr_LogError((object)str);
    }

    public void StbSr_AlwaysWriteLine(string format, params object[] values)
    {

        this.mission.Stb_Message(null, format, values);

        //FYI the below plan didn't work out
        //string str = format;
        //if (values != null) 
        //string str = String.Format(format, values);
        //Using logerror doesn't work because of thread conflicts
        //StbSr_LogError((object)str);
    }
    //Here we can do tricky things like change the name to reflect whether this is a bomber pilot, red or blue pilot, etc
    //Since the stats are keyed to playerName, putting an addendum to the name essentially creates a new separate career or personality

    //TODO: Better than doing this here, each & every time a particular stat is saved or accessed, would be to set this 
    //whenever a player goes through 'placeenter'.  Save the players current fighter/bomber status in a dictionary & then look
    //it up here as needed.  The problem that will solve, is what happens to a player's stats when they are killed or kicked
    //out of a plane or just leave the place.  Under this system here, the second they leave the place/aircraft ALL stats
    //& data will start flowing back to their main career rather than their bomber career.  This probably means losing 
    //a few key stats every time they start/stop a mission etc.
    public string StbSr_MassagePlayername(string playerName, AiActor actor = null)//p[0]=NamedDamageTypeNo,p[1]=DamageType
    {
        string newPlayerName = playerName;
        string careerTypes = mission.stb_getPilotTypeString(playerName);
        //Console.WriteLine("StbSR_MassagePlayername: " + playerName + " " + careerTypes);


        if (playerName.Contains("||PLAYER DIED"))
        {

            string ts = playerName.Substring(14); //chop off "||PLAYER DIED"
                                                  //entry.Key.Substring()                              
            string deathDate = ts.Substring(0, ts.IndexOf(" || "));
            string name = ts.Substring(ts.IndexOf(" || ") + 4);

            string selfKill = "";
            if (name.Contains(" (self-kill)"))
                selfKill = " (self-kill)";
            name = name.Replace(" (self-kill)", "");

            newPlayerName = "||PLAYER DIED " + deathDate + " || " + name + careerTypes + selfKill;

        }
        else
        {
            newPlayerName = playerName + careerTypes;
        }

        return newPlayerName;
    }

    public void StbSr_UpdateStatsForDamage(string playerName, int[] p, AiActor actor)//p[0]=NamedDamageTypeNo,p[1]=DamageType
    {
        try
        {

            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] temp = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
            {
                temp = new int[stbSr_numStats];
            } //See explanation for this idiom under StbSr_UpdateStatsForMission

            if (p[1] == 1) //p[1]=DamageType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[642]++; //temp[642]=AirTasksCompletedCount
                }
                temp[(p[0])]++;//p[0]=NamedDamageTypeNo
                stbSr_AllPlayerStats[playerName] = temp;
            }
            else if (p[1] == 2) //p[1]=DamageType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 2 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[643]++; //temp[643]=GroundTasksCompletedCount
                }
                temp[p[0]]++; //p[0]=NamedDamageTypeNo
                stbSr_AllPlayerStats[playerName] = temp;
            }
            else if (p[1] == 3) //p[1]=DamageType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[644]++; //temp[644]=NavalTasksCompletedCount
                }
                temp[p[0]]++; //p[0]=NamedDamageTypeNo
                stbSr_AllPlayerStats[playerName] = temp;
            }
            else if (p[1] == 4) //p[1]=DamageType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 4 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[793]++; //temp[793]=OtherGroundTaskCompletedCount
                }
                temp[p[0]]++; //p[0]=NamedDamageTypeNo
                stbSr_AllPlayerStats[playerName] = temp;
            }
            else
            { //some types of damage maybe don't have a p[1]==1, 2, or 3?
                temp[p[0]]++; //p[0]=NamedDamageTypeNo
                stbSr_AllPlayerStats[playerName] = temp;
            }

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_UpdateStatsForDead(string playerName, int[] p, AiActor actor)//p[0]=KillType
    {
        try //fyi CloD seems to run in an "unchecked" context, so if any of the ints here should overflow they will just silently roll over to minInt without throwing an exception.
        {
            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] temp = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
            {
                temp = new int[stbSr_numStats];
            }

            //Actually, don't use Console.WriteLine in the 2nd thread, it just seems to silently kill it for some reason.  bhugh, 2016/09/03
            //Console.WriteLine("Recording damage " + playerName + " " + p[0] + " " + p[1]);

            if (p[0] == 1)//p[0]=KillType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[642]++; //temp[642]=AirTasksCompletedCount
                }

                if (p.Length < 2)
                {
                    temp[647]++; //temp[647]=AirKillParticipationCount
                }
                else if (p.Length == 2)
                {
                    temp[647] += p[1];
                }

                stbSr_AllPlayerStats[playerName] = temp;
            }
            else if (p[0] == 2)//p[0]=KillType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 2 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[643]++; //temp[643]=GroundTasksCompletedCount
                }
                if (p.Length < 2)
                {
                    temp[648]++; //temp[648]=GroundKillParticipationCount (AA, etc)
                }
                else if (p.Length == 2)
                {
                    temp[648] += p[1];
                }

                stbSr_AllPlayerStats[playerName] = temp;
            }
            else if (p[0] == 3)//p[0]=KillType(1:air,2:artillery/AA/Tank,3:naval,4:other ground)
            {
                if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[644]++; //temp[644]=NavalTasksCompletedCount
                }
                if (p.Length < 2)
                {
                    temp[649]++; //temp[649]=NavalKillParticipationCount
                }
                else if (p.Length == 2)
                {
                    temp[649] += p[1];
                }

                stbSr_AllPlayerStats[playerName] = temp;
            }
            else if (p[0] == 4)//p[0]=KillType(1:air,2:ground,3:naval)
            {
                if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                {
                    temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                    temp[793]++; //temp[644]=NavalTasksCompletedCount
                }
                if (p.Length < 2)
                {
                    temp[794]++; //temp[794]=OtherGroundKillParticipationCount
                }
                else if (p.Length == 2)
                {
                    temp[794] += p[1];
                }

                stbSr_AllPlayerStats[playerName] = temp;
            }

            StbSr_Calc_AceAndRank_ByName(playerName, actor);

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_UpdateStatsForKilledPlayer(string playerName, int[] p, AiActor actor) //p[0]=KillType - 1 = normal, 2 = self-kill
    {
        try
        {
            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] temp = new int[stbSr_numStats];
            //StbSr_WriteLine("FKP1: {0} {1} ", playerName, p[0]);

            //If already on the deadplayers list we avoid doing all this "update stats for a dead pilot" routine.
            //  The deadplayers list is a list of players who have died recently.
            //  The idea is to avoid multiple kills being recorded to the same player
            //for the same death.  Also it stops players who are already in "death timeout" from having any further deaths recorded AND ALSO
            //from having their just-saved dead-pilot stats overwritten by a new (empty) dead pilot stats             
            //ALSO (if the dead pilot timeout is in effect) it allows any activity/kills from the deceased pilot's last action to finish playing out &
            //be recorded under their old stats (rather than under their new pilot stat)
            if (StbSr_IsInDeadPlayers(playerName) == null && !playerName.Contains("||PLAYER DIED"))
            {
                //The logic is convoluted here because this can be called several times/recursively almost. If we are NOT resetting player stats
                //on death then we just add their playerName to the deadplayers list & detect it that way.  But if we ARE resetting player 
                //stats then the next death notice for that player will hit the enqueuetask method first and be replaced by "||PLAYER DIED blahblahblah 
                //playerName".  So, we have to look for "||PLAYER DIED", too, and skip any further death-related piling on
                //for this poor player--who, it turns out, is already quite dead and doesn't need to be killed again.

                //If stbSr_ResetPlayerStatsWhenKilled then we're going to save the old stats entry with a new name (key)
                //indicating the time & manner of death, thus resetting the person's stats
                //If NOT stbSr_ResetPlayerStatsWhenKilled then the new name is just the same as the existing name.  
                //We do this up here because we need the new dead pilot name for the DeadPlayers entry.  But we can't make new blank stats entry
                //for the pilot until we've updated the existing stats entry etc etc etc
                //StbSr_WriteLine("FKP2: {0} {1} ", playerName, p[0]);
                string date, newplayerName;
                if (stbSr_ResetPlayerStatsWhenKilled)
                {
                    //When a player is killed, just rename their existing stats/dictionary key to something indicated that player is dead now.
                    date = DateTime.UtcNow.ToString("u").Replace(":", "."); // ":" is the escape character in the stats.txt save file, so we can have it in strings but it is a bit awkward looking; we'll just leave it out
                    newplayerName = "||PLAYER DIED " + date + " || " + playerName;
                    if (p[0] == 2) newplayerName += " (self-kill)";
                    //Now we have the name - we'll actually rename the Dictionary entry later
                }
                else
                {
                    newplayerName = playerName; //If reset player stats on death is off, we just retain the existing playername for the deadplayers list
                }


                //If stb_PlayerTimeoutWhenKilled is SET, When player dies, add new name & time the timeout expires, to a stack we will check whenever adding new stats
                //Thus the stats during the death timeout will go to the dead pilot career, not the new pilot career
                //If stb_PlayerTimeoutWhenKilled is NOT SET, we still add it to the stack for a few seconds.  Reason is, CloD typically
                //sends at least two "death notices" for the player in quick succession (plane killed, pilot killed, maybe even more in case of 
                //multi-crew a/c.  So this avoids double-entering the death for the same player.
                //Also it solves an issue with the -stats.cs module where the time of death is only saved with resolution of one second.
                //So if two deaths occur within one second one of the prior lives will be overwritten.
                DateTime currTime_plusTimeOut;
                if (this.mission.stb_PlayerTimeoutWhenKilled) currTime_plusTimeOut = DateTime.Now.AddHours(this.mission.stb_PlayerTimeoutWhenKilledDuration_hours);
                else currTime_plusTimeOut = DateTime.Now.AddSeconds(5);

                var a = new Tuple<string, DateTime>(newplayerName, currTime_plusTimeOut);
                stbSr_DeadPlayers.Add(playerName, a);


                //First add one death to the stats.
                if (!stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    temp = new int[stbSr_numStats];
                }

                //The system seems to report deaths twice in case of self kills (once as 'regular' and once as 'self'). So we only count death++ when it is NOT a self-kill to avoid double counting.
                if (p[0] == 1) temp[778]++; //temp[778]=Deaths
                if (p[0] == 2) temp[789]++; //temp[789]=Self-Kills         
                temp[795] = Calcs.TimeSince2016_sec(); //record time of player death

                stbSr_AllPlayerStats[playerName] = temp;

                //StbSr_WriteLine("FKP3: {0} {1} ", playerName, newplayerName);
                //OK, NOW we can change the player's stats entry key to its new name, if that option is enabled
                if (stbSr_ResetPlayerStatsWhenKilled)
                {
                    StbSr_Calc_AceAndRank_ByName(playerName, actor);

                    //int totalkills = StbSR_TotalKills(stbSr_AllPlayerStats[playerName]);



                    //And, initiate a new stats which so far only shows the player's time of last death
                    int[] temp2 = new int[stbSr_numStats];

                    //transfer over a few key values:
                    temp2[824] = stbSr_AllPlayerStats[playerName][824] + StbSR_TotalKills(stbSr_AllPlayerStats[playerName]); //prev accumulated total kills plus thos from the current life added to new life
                    temp2[823] = Math.Max(stbSr_AllPlayerStats[playerName][797], stbSr_AllPlayerStats[playerName][823]); //max of current rank & prev highest rank
                                                                                                                         //add total kill point, full victory, shared victory, assist totals from this life to the previous life accumulations
                                                                                                                         //& save them in the new life as "previous life totals"
                    for (int i = 0; i < 4; i++)
                    {
                        temp2[825 + i] = stbSr_AllPlayerStats[playerName][825 + i] + stbSr_AllPlayerStats[playerName][798 + i];
                    }

                    temp2[795] = Calcs.TimeSince2016_sec(); //record time of player death

                    //Don't change the keyname until we're done reading everything  from it!  Durr!!!
                    Calcs.changeKey(stbSr_AllPlayerStats, playerName, newplayerName);

                    stbSr_AllPlayerStats[playerName] = temp2;
                    //Don't use WriteLine in the 2nd thread . . . kills everything silently, weird
                    //StbSr_WriteLine("FKP: {0} {1} ", playerName, newplayerName);

                    //StbSr_WriteLine("FKP4: {0} {1} ", playerName, newplayerName);
                    StbSr_Calc_AceAndRank_ByName(playerName, actor);
                }
            }

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_UpdateStatsForMission(string playerName, int[] p, AiActor actor)//p[0]=Mission update type, p[1] (option) is amount to increment item p[0].  p[2] is the amount to set the item to (not add, just set). If p[2] exists then p[1] is ignored. If p[1] & p[2] both don't exist, (ie, p.Length()<2), the item is incremented by 1 
                                                                                      // 770 Takeoff Count 
                                                                                      // 771 Safe Landing Count
                                                                                      // 772 Crash Landing Count
                                                                                      // 773 Health Damaged Count
                                                                                      // 774 Parachute Failed Count
                                                                                      // 775 Parachute Landing Count
                                                                                      // 776 Player Connected Count
                                                                                      // 777 Player Disconnected Count
                                                                                      // 778 Player Death Count
                                                                                      // 779 Continuous Missions Count (ie, potentially several connected sorties, if the pilot lands & then takes off again from the same airport each time
                                                                                      // 844 Sortie count - aircraft sorties that continued at least as far as a/c takeoff
                                                                                      //Generally we use this method to increase ANY of the items that need increasing and also, any that need some # added to them, and any that need their value set to a certain #.  Adding to damage totals, kill points, saving accumulated time flown, saving the last time accessed, etc etc etc.  See list of indexes @ in comment @ the bottom of this file
                                                                                      //Generally, anything you put into p[0] will be incremented by 1 . . . if you also include a p[1] then p[1] will be added to existing value at index p[0] instead.  If you (instead) include p[2] then p[1] will be ignored and the existing value will be set to p[2].
                                                                                      //so, ah,  you'd better make sure what you put there actually exists & is what you want

    {
        try
        {
            playerName = StbSr_MassagePlayername(playerName, actor);

            //int[] temp = new int[stbSr_numStats];
            int[] temp = new int[stbSr_numStats];

            //if ( stbSr_AllPlayerStats is Dictionary<string, int[]> ) { StbSr_WriteLine ("Dict YES"); } else {StbSr_WriteLine ("Dict NO");} 
            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
            {
                temp = new int[stbSr_numStats];
            } //OK. 4 hours of testing later. It turns out that if you send a var like temp through TryGetValue even if there is no value returned temp gets all munged up and is not even an object when it returns.  So, you have to reinitialize it if you want to use it.  So an idiom like this will grab the existing stats if they exist OR create a blank temp to add them, if they don't.


            if (p.Length < 2)
            {
                //StbSr_WriteLine("Updating MISSION writing " + playerName + " " + p[0]);
                temp[p[0]]++;
                //StbSr_WriteLine( "UpdStatsMission: " + p[0] + " " + p.Length + " " + temp[p[0]]  );
            }
            else if (p.Length == 2)
            {
                //don't use Console.WriteLine in worker thread
                //StbSr_WriteLine("Updating MISSION writing " + playerName + " " + p[0] + " adding:" + p[1]);
                //StbSr_WriteLine("Updating MISSION writing " + playerName + " " + p[0] + " adding:" + p[1], null);
                temp[p[0]] += p[1];
                //StbSr_WriteLine( "UpdStatsMission: " + p[0] + " " + p[1] + " " + p.Length + " " + temp[p[0]]  );                
            }
            else if (p.Length == 3)
            {
                //don't use Console.WriteLine in worker thread
                //StbSr_WriteLine("Updating MISSION writing " + playerName + " " + p[0] + " adding:" + p[1]);
                //StbSr_WriteLine("Updating MISSION writing " + playerName + " " + p[0] + " setting to:" + p[2], null);
                temp[p[0]] = p[2];
                //StbSr_WriteLine( "UpdStatsMission: " + p[0] + " " + p[1] + " " + p.Length + " " + temp[p[0]]  );
            }

            stbSr_AllPlayerStats[playerName] = temp; //save the changes . . . 
            if (p[0] >= 770 && p[0] <= 779 && p[0] != 773) StbSr_Calc_AceAndRank_ByName(playerName, actor); //we want to update rank/ace at key points here, like takeoff, landing, whatever but not just willy nilly every time there is a bit of damage or whatever.

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_UpdateStatsForCutLimb(string playerName, int[] p, AiActor actor)//p[0]=LimbNamesNo,p[1]=TaskNo
    {
        //try
        {
            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] temp = new int[stbSr_numStats];
            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
            {
                temp = new int[stbSr_numStats];
            }

            if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
            {
                temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                temp[642]++; //temp[642]=AirTasksCompletedCount
            }                     //p[0]=LimbNamesNo
            temp[(p[0] + 649)]++; //temp[(p[0] + 649)]=CorrespondingLimbNamesNo
            stbSr_AllPlayerStats[playerName] = temp;

        }
        //catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_UpdateStatsForTaskCurrent(string playerName, int[] p, AiActor actor)//p[0]=TaskNo
    {
        try
        {
            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] temp = new int[stbSr_numStats];
            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
            {
                temp = new int[stbSr_numStats];
            }

            temp[0]++; //temp[0]=PlaceEnters
            temp[645] = p[0]; //temp[645]=CurrentTaskNo p[0]=TaskNo(1:air,2:ground,3:naval)
            temp[646] = 0; //temp[646]=CurrentTaskCompletedBool
            stbSr_AllPlayerStats[playerName] = temp;

            //stbSr_AllPlayerStats.Add(playerName, temp); //this is only needed if we need to create a new Dict entry, but the line above handles either case with no prob.

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_ReadStatsFromFile()
    {
        try
        {
            if (stbSr_LogStats)
            {
                FileInfo fi = new FileInfo(stbSr_PlayerStatsPathTxt);
                if (fi.Exists)
                {
                    if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX1", null); //testing disk output for warps
                    StreamReader sr = File.OpenText(stbSr_PlayerStatsPathTxt);
                    string s = sr.ReadToEnd();
                    sr.Close();
                    if (s == null) return;
                    if (s == "") return;
                    string[] retrievedStrings = s.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string retrievedStr in retrievedStrings)
                    {
                        StbSr_UpdateSingleUserStat(Calcs.unescapeSemicolon(retrievedStr));
                    }
                }
                StbSr_Calc_All_AceAndRank();
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_UpdateSingleUserStat(string retrievedString)
    {
        try
        {
            string userName = "";
            int[] userData;
            //Takes care of allowing ; and : characters in usernames.  Should be rare/nonexistent but  let's make sure 
            string[] retrievedData = retrievedString.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            //Don't use WriteLine in worker  thread!
            //StbSr_WriteLine("Retrieving: " + retrievedData.Length + " entries for " + retrievedData[0]);
            int numLines = (int)Math.Ceiling(stbSr_numStats / (double)50);  //stbSr_numStats SHOULD BE a multiple of 50 but just in case it isn't we always want to round up to the next 50 so as not to miss any stats
            if (retrievedData.Length == 2)
            {
                userName = Calcs.unescapeColon(retrievedData[0]);
                userData = new int[stbSr_numStats];
                string[] retrievedLines = retrievedData[1].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                int numRet = 0;
                /*
                if (retrievedLines.Length == numLines)
                {

                    for (int j = 0; j < numLines; j++)
                    {
                        string[] values = retrievedLines[j].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        if (j < 15 && values.Length == 50)
                        {
                            for (int i = 0; i < 50; i++)
                            {
                                userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                                numRet++;
                            }
                        }
                        else if (j == 15 && values.Length == 50)
                        {
                            for (int i = 0; i < 50; i++)
                            {
                                userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                                numRet++;
                            }
                        }
                    }
                }*/

                //We're expecting an array 50 X numLines, where 50 * numLines = stbSr_numStats
                //the problem is what to do if the data input doesn't match this scheme.  What we're doing here in that case it just padding it with zeros if we don't have enough data
                //and cutting it off at stbSr_numStats if it is too large.
                //DON'T use console.writeline in worker thread
                if (retrievedLines.Length != numLines) this.mission.Stb_Message(null, "STATS ERROR: " + userName + " had " + retrievedLines.Length.ToString() + " lines instead of the expected " + numLines.ToString(), new object[] { });


                for (int j = 0; j < numLines; j++)
                {
                    string[] values;
                    if (j >= retrievedLines.Length) { values = new string[] { }; }
                    else { values = retrievedLines[j].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries); }

                    if (values.Length != 50) StbSr_WriteLine("STATS ERROR: " + userName + " had " + values.Length.ToString() + " entries instead of the expected 50");
                    for (int i = 0; i < 50; i++)
                    {
                        if (i + (j * 50) >= stbSr_numStats)
                        {
                            StbSr_WriteLine("STATS ERROR: " + userName + " had more than the allowed number of " + stbSr_numStats.ToString());
                            break; //our array is only this large, don't read any more if we reach that point!  /failsafe
                        }
                        if (i >= values.Length) userData[i + (j * 50)] = 0;
                        else
                        {
                            try
                            {
                                userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                            }
                            //If there is data corruption we don't want to just exit here & omit the rest of that player's entry or the rest of the entries in the file.  So, lots of catches:
                            catch (ArgumentOutOfRangeException ex) //value is String.Empty
                            {
                                userData[i + (j * 50)] = 0;
                                //Console.WriteLine("Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat: " + ex.Message); //this is in a worker thread so most likely won't display at all, but we can try . . .
                                StbSr_PrepareErrorMessage(ex, "Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat");
                            }
                            catch (FormatException ex) //value contains a character that is not a valid digit
                            {
                                userData[i + (j * 50)] = 0;
                                //Console.WriteLine("Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat: " + ex.Message); //this is in a worker thread so most likely won't display at all, but we can try . . .
                                StbSr_PrepareErrorMessage(ex, "Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat");
                            }
                            catch (OverflowException ex) //value represents a number that is less than Int32.MinValue or greater than Int32.MaxValue
                            {
                                userData[i + (j * 50)] = Int32.MaxValue; //We're going to assume this is an overflow since underflows our values only increase, never decrease.  In reality it is more likely to be data corruption, but whatever . . . . 
                                                                         //Console.WriteLine("Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat: " + ex.Message); //this is in a worker thread so most likely won't display at all, but we can try . . .
                                StbSr_PrepareErrorMessage(ex, "Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat");
                            }
                            catch (Exception ex) //anything else
                            {
                                userData[i + (j * 50)] = 0;
                                //Console.WriteLine("Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat: " + ex.Message); //this is in a worker thread so most likely won't display at all, but we can try . . .
                                StbSr_PrepareErrorMessage(ex, "Reading *_playerstats_full.txt file, StbSr_UpdateSingleUserStat");
                            }


                        }
                        numRet++;
                    }


                }


                userData[645] = 0;
                userData[646] = 0;
                stbSr_AllPlayerStats.Add(userName, userData);
                //StbSr_WriteLine("Retrieved: " + numRet + " entries for " + retrievedData[0]);
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    HashSet<string> stbSr_PlayerTimedOutDueToDeath_overridelist = new HashSet<string>();

    //override - allows this player to override the timeout
    public void StbSr_PlayerTimedOutDueToDeath_override(string playername)
    {
        if (playername != null && playername != "")
        {
            stbSr_PlayerTimedOutDueToDeath_overridelist.Add(playername);
            mission.Timeout(5 * 60, () => { stbSr_PlayerTimedOutDueToDeath_overridelist.Remove(playername); }); //override lasts for 5 minutes only

            //playerBomberFighter = " (bomber)"; //do we need to add this?  shouldn't, but maybe?

        }
    }

    public bool StbSr_PlayerTimedOutDueToDeath_IsPlayerOnOverrideList(string playername)
    {

        //if (playername != null && playername != "" && stbSr_PlayerTimedOutDueToDeath_overridelist.Exists(element => element == playername)) return true; //this is the c# LIST way to do it
        if (playername != null && playername != "" && stbSr_PlayerTimedOutDueToDeath_overridelist.Contains(playername)) return true; //This is the HashSet way to do it
        else return false;

    }

    //seconds since last death of this player name, or -1 if no death recorded
    public int StbSr_TimeSincePlayerLastDied_seconds(string playername)
    {

        string playerBomberFighter = " (bomber)";
        if (playername.EndsWith(playerBomberFighter)) playername.Remove(playername.Length - 9, 9);
        string[] names = new string[] { playername, playername + playerBomberFighter };
        int timesincedead = -1;
        int temptimesincedead = -1;

        //Step through each player account (fighter, bomber, etc) and find the MOST RECENT player death, and return that time
        foreach (string n in names)
        {

            int[] temp = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(n, out temp))
            {
                temp = new int[stbSr_numStats];
            }
            int timeDied = temp[795];
            if (timeDied != 0)
            {
                temptimesincedead = Calcs.TimeSince2016_sec() - timeDied;
                //return the most recent time since death, but -1 means we haven't found a death time yet
                if (timesincedead == -1 || temptimesincedead < timesincedead) timesincedead = temptimesincedead;
            }
        }

        return timesincedead;
    }

    //returns the number of seconds until player timeout due to death expires, or 0 if no timeout in effect
    public int StbSr_IsPlayerTimedOutDueToDeath(string playername)
    {
        if (!stbSr_PlayerTimeoutWhenKilled || StbSr_PlayerTimedOutDueToDeath_IsPlayerOnOverrideList(playername)) return 0;
        int timeSinceDied_seconds = this.StbSr_TimeSincePlayerLastDied_seconds(playername);
        //StbSr_WriteLine ("Died values: " + timeSinceDied_seconds.ToString() + " " + ((int)(stbSr_PlayerTimeoutWhenKilledDuration_hours * 60 * 60)).ToString() + " " + stbSr_PlayerTimeoutWhenKilledDuration_hours.ToString() + " " + Calcs.TimeSince2016_sec().ToString());
        if (timeSinceDied_seconds < 0) return 0;
        int timeUntilTimeoutExpires_seconds = (int)(stbSr_PlayerTimeoutWhenKilledDuration_hours * 60 * 60) - timeSinceDied_seconds;
        if (timeUntilTimeoutExpires_seconds < 0) timeUntilTimeoutExpires_seconds = 0;
        return timeUntilTimeoutExpires_seconds;
        //public bool stb_PlayerTimeoutWhenKilledDuration_hours = 0.05; //Time (in hours) for the player timeout on death. Only active if stb_PlayerTimeoutWhenKilled
    }

    //Turn PlayerTimeoutWhenKilled either on OR off
    public void StbSr_Deban()
    {
        stbSr_PlayerTimeoutWhenKilled = false;
    }

    public void StbSr_Reban()
    {
        stbSr_PlayerTimeoutWhenKilled = true;
    }

    public void StbSr_LogStats_off()
    {
        stbSr_LogStats = false;
    }

    public void StbSr_LogStats_on()
    {
        stbSr_LogStats = true;
    }

    //Pilots are awarded these 'ace awards' when they reach the kill numbers listed
    public readonly int[] ACE_AWARD_KILL_VALUES = { 0, 5, 10, 15, 20, 25, 30, 40, 50, 75, 100, 150, 200, 300, 500, 1000, 1500, 2000, 3000, 4000, 5000, 10000 };
    public readonly string[] ACE_AWARD_NAMES = { "", "Ace", "Double Ace", "Triple Ace", "Quadruple Ace", "Quintuple Ace", "Sextuple Ace", "Octuple Ace", "Half-Century Ace", "75-kill Ace", "Century Ace", "150-kill Ace", "200-kill Ace", "300-kill Ace", "Half-Millenium Ace", "Millenium Ace", "Millenium-and-a-Half Ace", "Double Millenium Ace", "Triple Millenium Ace", "Quadruple Millenium Ace", "Ace Ruler of the Universe", "Ace of Aces" };

    //Pilots are awarded these ranks when they reach ALL OF the # of flights/missions and the # of minutes and the amount of damage done.  # of minutes flight time is the primary determinant, with flights/missions as just a bit of a fail-safe.  This is to prevent cheating both by flying many very short flights (which we want to discourage; actually we encourage linking together many shorter flights into a longer single mission when possible--so we don't want to punish pilots who do that) OR just jumping in & sitting on the ground, thereby racking up many minutes.
    //Count/Index                                     0           1                        2                        3                         4                         5                            6                            7                          8                          9                         10                        11                        12                      13                             14               15                                16                  17                           18                       19                    20                          21                          22                                                     23                                         24                                         25    
    public readonly int[] RANK_TIME_VALUES_MIN = { 0, 60, 120, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400, 1500, 1650, 1800, 2000, 2300, 2650, 3000, 3500, 4000, 6000 }; //# of minutes of flight time required to reach this rank
    public readonly int[] RANK_FLIGHT_VALUES = { 0, 3, 6, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 83, 90, 100, 115, 133, 150, 175, 200, 300 }; //# of flights required to reach this rank
    public readonly int[] RANK_DAMAGE_VALUES = { 0, 30, 60, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700, 750, 825, 900, 1000, 1150, 1330, 1500, 1750, 2000, 3000 }; //amount of damage done required to reach this rank ("Total Damage Done" in stats). To even things  up for pilots we will add 20 damage per naval, AA, or ground kill (because for bombers, any of these kills usually just adds 1 damage point, not MANY as it should be. Ave damage per kill in air-to-air engagements it 10 damage per kill.) Typical flight is 20 min & 20 damage, 2 kills.           
    public readonly int[] RANK_KILLPERCENT_VALUES = { 0, 4, 9, 15, 22, 30, 37, 45, 52, 60, 68, 75, 82, 90, 97, 105, 112, 122, 132, 150, 177, 203, 225, 263, 300, 450 }; //amount of kills % require to reach this rank.  Abt 2 kills per mission but we'll assume that kill % is about 3/4 of that.
    public readonly string[] RANK_NAMES_GB = { "Tyro", "Pilot Officer 2nd Cl.", "Pilot Officer 1st Cl.", "Flying Officer 2nd Cl.", "Flying Officer 1st Cl.", "Flight Lieutenant 2nd Cl.", "Flight Lieutenant 1st Cl.", "Squadron Leader 2nd Cl.", "Squadron Leader 1st Cl.", "Wing Commander 2nd Cl.", "Wing Commander 1st Cl.", "Group Captain 2nd Cl.", "Group Captain 1st Cl.", "Air Commodore (provisional)", "Air Commodore", "Air Vice-Marshal (provisional)", "Air Vice-Marshal", "Air Marshal (provisional)", "Air Marshal", "Air Chief Marshal", "Marshal of the RAF", "Marshal of Great Britain", "Grand Marshal of Great Britain and the Commonwealth", "Exalted Grand Marshal and Generalissimo", "Supreme Commander of Allied Forces", "Grand Dictator of the Universe and Everything" };
    public readonly string[] RANK_NAMES_DE = { "Neuling", "Leutnant 2. Cl.", "Leutnant 1. Cl.", "Oberleutnant 2. Cl.", "Oberleutnant 1. Cl.", "Hauptmann 2. Cl.", "Hauptmann 1. Cl.", "Major 2. Cl.", "Major 1. Cl.", "Oberstleutnant 2. Cl.", "Oberstleutnant 1. Cl.", "Oberst 2. Cl.", "Oberst 1. Cl.", "Charakter als Generalmajor", "Generalmajor", "Charakter als Generalleutnant", "Generalleutnant", "General der Flieger", "General der Luftwaffe", "Generaloberst", "Generalfeldmarschall", "Reichsmarschall", "Grosser Reichsmarschall", "Hoechster Reichsmarschall", "Oberkommando der Wehrmacht", "Erhabener Diktator u. Herrscher des Universums u. Alles" };


    /*
    public readonly int[] RANK_TIME_VALUES_MIN =    { 0, 60, 120, 200, 300, 400, 500, 600, 700, 800, 1000, 1500, 2000, 3000, 4000, 6000}; //# of minutes of flight time required to reach this rank
    public readonly int[] RANK_FLIGHT_VALUES =      { 0,  3,   6,  10,  15,  20,  25,  30,  35,  40,   50,   75,  100,  150,  200,  300}; //# of flights required to reach this rank
    public readonly int[] RANK_DAMAGE_VALUES =      { 0, 30,  60, 100, 150, 200, 250, 300, 350, 400,  500,  750, 1000, 1500, 2000, 3000}; //amount of damage done required to reach this rank ("Total Damage Done" in stats). To even things  up for pilots we will add 20 damage per naval, AA, or ground kill (because for bombers, any of these kills usually just adds 1 damage point, not MANY as it should be. Ave damage per kill in air-to-air engagements it 10 damage per kill.) Typical flight is 20 min & 20 damage, 2 kills.           
    public readonly int[] RANK_KILLPERCENT_VALUES = { 0,  4,   9,   15,  22, 30,  37,  45,  52,  60,   75,  112,  150,  225,  300,  450}; //amount of kills % require to reach this rank.  Abt 2 kills per mission but we'll assume that kill % is about 3/4 of that.
    public readonly string[] RANK_NAMES_GB = { "Tyro", "Pilot Officer", "Flying Officer", "Flight Lieutenant", "Squadron Leader", "Wing Commander", "Group Captain", "Air Commodore", "Air Vice-Marshal", "Air Marshal", "Air Chief Marshal", "Marshal of the RAF", "Marshal of Great Britain", "Grand Marshal of Great Britain and the Commonwealth", "Exalted Grand Marshal and Generalissimo of Planet Earth", "Grand Dictator of the Universe and Everything" };
    public readonly string[] RANK_NAMES_DE = { "Neuling", "Leutnant", "Oberleutnant", "Hauptmann", "Major", "Oberstleutnant", "Oberst", "Generalmajor", "Generalleutnant", "General der Luftwaffe", "Generaloberst", "Generalfeldmarschall", "Reichsmarschall", "Grosser Reichsmarschall", "Hoechster Reichsmarschall der Ganzen Erde", "Erhabener Diktator u. Herrscher des Universums u. Alles" };
    */
    //Possibility to add more ranks @ lower levels
    //Design so Marshal of Great Britain/Reichsmarshall arrives at about 100 flights
    //public readonly string[] RANK_NAMES_GB = { "Tyro", "Pilot Officer 2nd Cl.", "Pilot Officer 1st Cl.", "Flying Officer 2nd Cl.", "Flying Officer 1st Cl.", "Flight Lieutenant 2nd Cl.", "Flight Lieutenant 1st Cl.", "Squadron Leader 2nd Cl.", "Squadron Leader 1st Cl.", "Wing Commander 2nd Cl.", "Wing Commander 1st Cl.", "Group Captain 2nd Cl.", "Group Captain 1st Cl.", "Air Commodore 2nd Cl.", "Air Commodore 1st Cl.", "Air Vice-Marshal", "Air Marshal", "Air Chief Marshal", "Marshal of the RAF", "Marshal of Great Britain", "Grand Marshal of Great Britain and the Commonwealth", "Exalted Grand Marshal and Generalissimo", "Grand Dictator of the Universe and Everything" };
    //public readonly string[] RANK_NAMES_DE = { "Neuling", "Leutnant 2. Cl.", "Leutnant 1. Cl.", "Oberleutnant 2. Cl.", "Oberleutnant 1. Cl.", "Hauptmann 2. Cl.", "Hauptmann 1. Cl.", "Major 2. Cl.", "Major 1. Cl.", "Oberstleutnant 2. Cl.", "Oberstleutnant 1. Cl.", "Oberst 2. Cl.", "Oberst 1. Cl.", "Generalmajor  2. Cl.", "Generalmajor  1. Cl.", "Generalleutnant  2. Cl.", "Generalleutnant  1. Cl.", "General der Luftwaffe", "Generaloberst", "Generalfeldmarschall", "Reichsmarschall", "Grosser Reichsmarschall", "Hoechster Reichsmarschall", "Erhabener Diktator u. Herrscher des Universums u. Alles" };
    public static int DAY_SEC = 24 * 60 * 60; //# of seconds in a day

    //Returns either the specified army (if valid) OR determines it from the stats array
    //1 = BG ; 2 = DE ; 3 & 4 might be valid/used sometimes?  0 means use the default/according to stats/best guess
    public int StbSR_GetArmyForRankName(int[] Value, int army = 0)
    {
        if (army > 0 && army < 3) return army; //if we have specified & army for the player  we'll use that
        if (Value[784] > Value[783]) return 2; //if not specified, then if the player has entered more often with Blue army we'll use that / DE
        return 1; //otherwise go with British / GB
    }


    //Gets the "ground kill" number, summing several sub-categories of kill
    public int StbSR_NavalGroundKills(int[] Value)
    {
        try
        {
            return (Value[648] + Value[649] + Value[794]);
        }
        catch
        {
            return 0;
        }
    }
    //Gets the "total kill" number for ACE purposes, summing several sub-categories of kill with a special formula
    public double StbSR_TotalAceKills(int[] Value)
    {
        try
        {
            return ((double)Value[799] + (double)Value[800] / 2 + (double)Value[801] / 4);

        }
        catch
        {
            return 0;
        }
    }
    public double StbSr_NumberOfAceKills(string playername)
    {
        try
        {


            int[] value = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playername, out value)) return 0;

            return StbSR_TotalAceKills(value);
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_nok"); return 0; }


    }

    //Gets the "total kill" number, summing several sub-categories of kill
    public int StbSR_TotalKills(int[] Value)
    {
        try
        {
            return (Value[647] + Value[648] + Value[649] + Value[794]);
        }
        catch
        {
            return 0;
        }
    }

    public int StbSr_NumberOfKills(string playername)
    {
        try
        {


            int[] value = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playername, out value)) return 0;

            return StbSR_TotalKills(value);
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_nok"); return 0; }


    }

    //returns rank of playerName as integer value
    public int StbSr_RankAsIntFromName(string playerName)
    {
        try
        {

            int[] value = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out value)) return 0;

            return value[797];

        }

        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_rfn"); return 0; }

    }

    public string StbSr_RankNameFromInt(int rank, int army = 1)
    {

        string[] rank_names = RANK_NAMES_GB; //English names are default
        if (army == 2) rank_names = RANK_NAMES_DE; //but if we have specified & army for the player  we'll use that 

        //sanity check
        if (rank < 0) return "";
        if (rank >= rank_names.Length) return "";

        return rank_names[rank];

    }

    //returns the player's rank (including a final space if needed; or empty string if not available)
    // so you can use like StbSr_RankFromName(playerName) + playerName
    // or this.mission.stb_StatRecorder.StbSr_RankFromName  or stb_StatRecorder.StbSr_RankFromName dep on context
    //if actor isnt' available just omit it in function call & it will figure a default based on which army the player favors
    //if highest=true, returns the HIGHEST rank for the player over multiple lifetimes
    public string StbSr_RankFromName(string playerName, AiActor actor = null, bool highest = false)
    {
        try
        {

            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] value = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out value)) return "";

            int army = 0;
            if (actor != null && actor.Army() > 0) army = actor.Army();
            army = StbSR_GetArmyForRankName(value, army);

            string[] rank_names = RANK_NAMES_GB; //English names are default
            if (army == 2) rank_names = RANK_NAMES_DE; //but if we have specified & army for the player  we'll use that            

            //we can return either current rank OR the highest rank ever achieved over multiple lifetimes
            if (highest)
            {
                int highest_rank = Math.Max(value[797], value[823]);
                return StbSr_RankNameFromInt(highest_rank, army) + " ";
            }
            else return StbSr_RankNameFromInt(value[797], army) + " ";

        }

        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_rfn"); return ""; }

    }

    //Use this when you want to FORCE the display of the rank & stats info every time.
    public void StbSr_Display_AceAndRank_ByName(string playerName, AiActor actor = null, Player player = null, bool forceDemotion = false)
    {
        playerName = StbSr_MassagePlayername(playerName, actor);
        StbSr_Calc_AceAndRank_ByName(playerName, actor, true, player, forceDemotion);
    }

    //Use this (ie, with display=null or omitted) when you want to display of the rank & stats info only when it has changed
    public void StbSr_Calc_AceAndRank_ByName(string playerName, AiActor actor = null, bool display = false, Player player = null, bool forceDemotion = false)
    {
        try
        {

            playerName = StbSr_MassagePlayername(playerName, actor);

            int[] value = new int[stbSr_numStats];

            if (!stbSr_AllPlayerStats.TryGetValue(playerName, out value)) return;

            KeyValuePair<string, int[]> entry = new KeyValuePair<string, int[]>(playerName, value);

            int army = 0;
            //StbSr_WriteLine("army {0}", army);                
            if (actor != null && actor.Army() > 0) army = actor.Army();
            //StbSr_WriteLine("army {0}", army);
            army = StbSR_GetArmyForRankName(value, army);
            //StbSr_WriteLine("army {0}", army);                

            StbSr_Calc_Single_AceAndRank(entry, 0, display, army, player, forceDemotion);
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_arbn"); }
    }

    //display = true forces the display of current rank/stats always.  display = false displays only if there is some change in the situation
    //TODO: This displays the stats to everyone; when a person reqests <stats it would be better to just show it to the requesting person
    public void StbSr_Calc_Single_AceAndRank(KeyValuePair<string, int[]> entry, int curr_time_sec = 0, bool display = false, int army = 0, Player player = null, bool forceDemotion = false)
    {
        try
        {
            if (curr_time_sec == 0) curr_time_sec = Calcs.TimeSince2016_sec();
            //if (curr_time_sec - entry.Value[795] > DAY_SEC && entry.Key.Contains("||PLAYER DIED")) return; //no rank or ace for dead players--too bad for them!  This only applies if the stb_ResetPlayerStatsWhenKilled is set to true / everything still works OK otherwise
            int total_flights_minus_selfdamage_and_deaths_cm = entry.Value[779] - entry.Value[791] - entry.Value[778];  //# of continuous missions with flights ending in self-damage subtracted and also player deaths subtracted . . . 
            int total_flights_minus_selfdamage_and_deaths_sorties = entry.Value[844] - entry.Value[791] - entry.Value[778];  //# of sorties (>4 min duration) with sorties ending in self-damage subtracted and also player deaths subtracted . . . 

            //When we started this Jan 2017 we weren't recording # of sorties.  Now that we are we'd prefer to use that measure here but players with older stats
            //Won't have it.  So if contin. mission count happens ot be higher than the sortie count, we'll count it via cm instead of sorties
            int total_flights_minus_selfdamage_and_deaths = total_flights_minus_selfdamage_and_deaths_sorties;
            if (total_flights_minus_selfdamage_and_deaths_cm > total_flights_minus_selfdamage_and_deaths) total_flights_minus_selfdamage_and_deaths = total_flights_minus_selfdamage_and_deaths_cm;

            int sumAllDamage = 0;
            for (int i = 1; i < 642; i++) { sumAllDamage += entry.Value[i]; }//Damage is 1 to 641 & 650-769
            for (int i = 650; i < 770; i++) { sumAllDamage += entry.Value[i]; }//Damage is 1 to 641 & 650-769     

            double flight_time_min = (double)entry.Value[792] / 60;

            //int totalkills = (entry.Value[647] + entry.Value[648] + entry.Value[649] + entry.Value[794]);
            int totalkills = StbSR_TotalKills(entry.Value);
            double totalacekills = StbSR_TotalAceKills(entry.Value);

            int aa_naval_ground_kills = StbSR_NavalGroundKills(entry.Value);

            int adjustedDamageTotal = sumAllDamage + aa_naval_ground_kills * 20; //We add on an adjustment for aa/naval/ground kills because otherwise bomber pilots are at a great disadvantage

            //decide whether to use GB or DE names for the ranks
            string[] rank_names = RANK_NAMES_GB; //English names are default
            int pref_army = StbSR_GetArmyForRankName(entry.Value, army);
            if (pref_army == 2) rank_names = RANK_NAMES_DE; //but if we have specified & army for the player  we'll use that


            //Calc new RANK
            double divisor = .7; //larger divisor makes it HARDER.  To make it easier, make it range from >0 to <1.  Had it at 1 but on reflection, setting to 0.3 to make it quite a bit easier. Then put it on .6 to make it 2X that hard.
            if (entry.Key.Contains("(bomber)")) divisor = 1.6; //bomber pilots are rated basically on kills & damage only, not sorties.  (Since they don't survive for long.)  But this results is some really super-fast promotions for just one good mission.  So we need to calm it down a bit.
            if (!stbSr_ResetPlayerStatsWhenKilled) divisor = 5; //If stbSr_ResetPlayerStatsWhenKilled is off, we adjust the ranks to require MUCH higher amounts of kills etc to progress through the ranks. TODO: These divisors should be set in the stats.ini file               & be customizable per server

            //The RAF/LW only recognize kills according their ace scheme . . . so we calc it here & use this in promotion calcs rather than kpm or kps
            double kpm_ace = 0; if (entry.Value[779] > 2) { kpm_ace = totalacekills / (double)entry.Value[779]; }
            if (kpm_ace < 1) kpm_ace = 0; if (kpm_ace > 15) kpm_ace = 15; //Some sanity limits. No kpm boost if your kpm is <1, and 15 boost (=2X easier to receive promotion) is the max allowed.  Some people now are ave 8-9 kpm so 15 gives us some headroom.
            divisor = divisor * (1 - (kpm_ace / 30));  //we can make it up to 2X as easy to get a promotion, if your ave kills per mission is >1 

            double kpm = 0;
            if (entry.Value[779] != 0) { kpm = (double)entry.Value[798] / (double)entry.Value[779] / 100.0; } //of kill points per continuous mission

            double kps = 0; //Ave Kill Points per Sortie                
            if (entry.Value[844] != 0) { kps = (double)entry.Value[798] / (double)entry.Value[844] / 100.0; } //of kill points per sortie                

            double pwopm = 0; //Ave Planes Written Off per Mission
            if (entry.Value[779] != 0) { pwopm = (double)entry.Value[845] / (double)entry.Value[779]; } //PWO per mission

            double pwops = 0; //Ave Planes Written Off per Sortie
            if (entry.Value[844] != 0) { pwops = (double)entry.Value[845] / (double)entry.Value[844]; } //PWO per sortie      

            //Plane return bonus
            //This is successful landings MINUS number of  planes written off.
            //It is added to the kill percentage total, so bringing your own plane back in one piece & landing is about the equivalent of a
            //victory against the enemy.
            //If you have too many planes written off, this can go negative but when negative it is divided by 2 & limited to -5 to limit the damage it can do to your rank
            double prb = 0;
            prb = (double)entry.Value[845] - (double)entry.Value[771];  //number of landings minus the number of planes written off
            if (prb < 0)
            {
                prb = prb / 2;
                if (prb < -5) prb = -5;
            }





            double divisor2 = 1; //larger divisor2 makes it HARDER. To make it easier, make it range from >0 to <1
                                 //We make it easier to work your way up the ranks if, in a previous life, you had a higher rank.
                                 //This is the one way previous lives affect the current career.

            int current_rank = entry.Value[797];
            double highest_rank_from_previous_lives = (double)entry.Value[823];

            //So someone who had reached 50 mission rank (10 of 15 possible ranks) will move up 
            //abt 1.5X faster, about.  If you manage to achieve highest possible rank (300 missions) next time you 
            //move through 2X faster. Etc.
            //This only applies up until you reach the rank you were at before.
            // factor * (1 - current_rank/highest_rank_from_previous_lives) in the equation makes it so that you get the full
            //boost a rank level 0 but as you climb closer to your highest previous rank your boost gradually diminishes until
            //there is no boost at all once you reach your previous rank. 
            //Squaring it like * (1 - current_rank * current_rank / highest_rank_from_previous_lives
            //highest_rank_from_previous_lives) means that the boost factor stays higher until you are closer to the previous
            //rank, compared with just doing a linear function
            //This stops the ranks flipping back & forth like crazy when you reach your previous rank the 2nd time around
            double factor = (double)rank_names.Length * 2;
            divisor2 = 1;
            // if (highest_rank_from_previous_lives > 0 && highest_rank_from_previous_lives > current_rank ) divisor2 = divisor2 * (1 - ((double)highest_rank_from_previous_lives / factor) * (1 - (double)current_rank * (double)current_rank / (double)highest_rank_from_previous_lives / (double)highest_rank_from_previous_lives) );

            //adjustment if your planes written off per sorties is too large
            double divisor3 = 1;
            if (pwops > 0.15 && entry.Value[845] > 3) divisor3 = 1 + pwops / 3;
            if (divisor3 > 2) divisor3 = 2;
            if (entry.Key.Contains("(bomber)")) divisor3 = 1; //not worried about bomber pilots writing off planes


            double divisor4 = 1;
            if (entry.Key.Contains("(bomber)")) divisor4 = 0.1; //For bomber pilots, who typically have a shorter but more meteoric career, we don't require as much seat time to get a promotion

            int new_rank_time = Calcs.array_find_equalorless(RANK_TIME_VALUES_MIN, (int)Math.Floor((double)flight_time_min / divisor / divisor2 / divisor3 / divisor4));  //we want FLOOR for this as we actually want them to achieve the specified requirement before they get the promotion, rather than just being halfway to it.
            int new_rank_flights = Calcs.array_find_equalorless(RANK_FLIGHT_VALUES, (int)Math.Floor((double)total_flights_minus_selfdamage_and_deaths / divisor / divisor2 / divisor3));
            int new_rank_damage = Calcs.array_find_equalorless(RANK_DAMAGE_VALUES, (int)Math.Floor((double)adjustedDamageTotal / divisor / divisor2 / divisor3));
            int new_rank_killpercentage = Calcs.array_find_equalorless(RANK_KILLPERCENT_VALUES, (int)Math.Floor(((double)entry.Value[798] + prb) / 100 / divisor / divisor2 / divisor3));

            int[] rank_scores = new int[] { new_rank_time, new_rank_flights, new_rank_damage, new_rank_killpercentage };
            Array.Sort(rank_scores);
            double rank_ave = (new_rank_time + new_rank_flights + new_rank_damage + new_rank_killpercentage) / 4;

            //int new_rank = Math.Min(new_rank_time, Math.Min(new_rank_flights, Math.Min(new_rank_damage,new_rank_killpercentage))); //this takes the lowest contributing score

            //int new_rank = rank_scores[1]; //We take the 2nd lowest contributing score, sort of like dropping your lowest test score on your semester grade
            int new_rank = (int)Math.Floor((rank_scores[1] + rank_ave) / 2); //We take the 2nd lowest contributing score, sort of like dropping your lowest test score on your semester grade
                                                                             //if (entry.Key.Contains("(bomber)")) new_rank = rank_scores[1]; //For bomber pilots we are nicer & take the 3rd lowest contributing score . . . 


            //StbSr_WriteLine("Rank: {0} t: {1} f: {2} d: {3} k%: {4} div: {5} div2: {6} B: {7} R: {8} A: {9} N: {10} ", new_rank, new_rank_time, new_rank_flights, new_rank_damage, new_rank_killpercentage, divisor, divisor2, entry.Value[784], entry.Value[783], pref_army, entry.Key);

            //StbSr_WriteLine("Rank: {0} t: {1} f: {2} d: {3} k%: {4} div: {5} div2: {6} B: {7} R: {8} A: {9} N: {10} ", new_rank, new_rank_time, new_rank_flights, new_rank_damage, new_rank_killpercentage, divisor, divisor2, entry.Value[784], entry.Value[783], pref_army, entry.Key);
            //Leetle sanity check here . . . 
            if (new_rank < 0) new_rank = 0;
            if (new_rank > rank_names.Length) new_rank = rank_names.Length;

            //No demotions unless your calculated rank is more than TWO levels below your current rank
            //This is to make your promotions more 'sticky' so that people don't keep flipping back & forth when they are just on the edge of a promotion.
            //But also to be more realistic because once promoted you usually aren't busted down a rank unless you REALLY screw up somehow
            if (!forceDemotion)
            {
                int rank_diff = current_rank - new_rank;
                //turning this off for testing purposes
                if (rank_diff > 0 && (rank_diff < 4 || rank_diff / current_rank < 0.25)) new_rank = current_rank;
                entry.Value[797] = new_rank;
            }

            //If we have player (eg when the player has requested the stats) we can direct the message to that player.  Otherwise, it goes to all.
            Player[] to = null;
            if (player != null) to = new Player[] { player };

            if ((display || new_rank != current_rank) && (!stbSr_NoRankMessages && (player != null)))  //Display when display is forced via display=true, or if rank has changed, but stbSr_NoRankMessages=true shuts off ALL rank/promotion messages regardless, except when specifically requested by the player
            {
                string posthumously = ""; string congrat = "Congratulations!";
                if (StbSr_IsInDeadPlayers(entry.Key) != null)
                {
                    posthumously = "posthumously ";
                    congrat = "With our condolences and gratitude to the late " + rank_names[new_rank] + "'s family and friends.";
                }
                string dir = "been " + posthumously + "promoted from " + rank_names[current_rank] + " to";
                if (new_rank < current_rank) { dir = "been " + posthumously + "demoted from " + rank_names[current_rank] + " to"; congrat = "Condolences!"; }
                if (new_rank == current_rank) { dir = "reached"; congrat = ""; }
                this.mission.Timeout(2.1, () =>
                {
                    string pmsg = entry.Key + " has " + dir + " the rank of: " + rank_names[new_rank] + ". " + congrat;
                    pmsg = pmsg.Replace(@"..", @".");  //Replace any double periods, which can happen if rank ends with Cl. or the like
                    this.mission.Stb_Message(to, pmsg, null);
                });
            }

            //Calc new ACE LEVEL
            int current_acelevel = entry.Value[796];
            int new_acelevel = Calcs.array_find_equalorless(ACE_AWARD_KILL_VALUES, (int)Math.Floor(totalacekills)); //For Ace purposes we round DOWN
            if (new_acelevel < 0) new_acelevel = 0; //With negative kills possible (penalties) still the lowest possible ace level is just 0, not negative. A negative number here causes many problems elsewhere.
            entry.Value[796] = new_acelevel;
            if (display || new_acelevel != current_acelevel)
            {
                string congrat_al = "";
                string posthumously_al = "";
                if (new_acelevel > current_acelevel) congrat_al = "Congratulations!";
                bool deadp = false;
                if (StbSr_IsInDeadPlayers(entry.Key) != null)
                {
                    posthumously_al = "posthumously ";
                    congrat_al = "Our condolences and gratitude to the late " + rank_names[new_rank] + "'s family and friends.";
                    deadp = true;

                }

                string m1 = " has " + posthumously_al + "reached the level of ";
                if (display) m1 = "is a";
                if (display && deadp) m1 = "was a";

                //char[] vowels = new char[] { 'A', 'E', 'I', 'O', 'U'  };
                string vowels = "AEIOU"; //"is/was a" must be "is/was an" whenever the ace award starts with a vowel . . . 
                if (display && ACE_AWARD_NAMES[new_acelevel].Length > 0 && vowels.Contains(ACE_AWARD_NAMES[new_acelevel][0].ToString())) m1 += "n";

                if (ACE_AWARD_NAMES[new_acelevel] != "")
                    this.mission.Timeout(0.1, () =>
                    {
                        this.mission.Stb_Message(to, entry.Key + " " + m1 + " " + ACE_AWARD_NAMES[new_acelevel] + ". " + congrat_al, null);
                    });
            }

            //Show some stats (if requested)
            if (display)
            {

                this.mission.Timeout(1, () =>
                {
                    this.mission.Stb_Message(to, rank_names[new_rank] + " " + entry.Key + " stat summary: " + totalkills.ToString() + " Total Kills (any participation); " + totalacekills.ToString("0.0") + " Total Kills (Ace Formula); " + entry.Value[799].ToString() + "/" + entry.Value[800].ToString() + "/" + entry.Value[801].ToString() + " Full/Shared/Assist Victories; ", null);
                });


                this.mission.Timeout(2, () => {
                    this.mission.Stb_Message(to, ((double)entry.Value[802] / 100).ToString("0.0") + "/" + ((double)entry.Value[806] / 100).ToString("0.0") + "/" + ((double)entry.Value[810] / 100).ToString("0.0") + "/" + ((double)entry.Value[814] / 100).ToString("0.0") + " Air/AA/Naval/Ground Kill Points; " + adjustedDamageTotal.ToString()
                       + " Total Damage Hits; " + ((uint)(entry.Value[818])).ToString("N0") + " Total Damage Points; ", null);
                });

                this.mission.Timeout(3, () => {
                    this.mission.Stb_Message(to, flight_time_min.ToString("0.0") + " min. Flight Time; " //casting 818 to uint because it could overflow . . . uint gives us twice the headroom
                      + entry.Value[844].ToString() + " Sorties; " + kps.ToString("F2") + " Kill Points per Sortie; " + entry.Value[779].ToString() + " Continuous Missions; " + kpm.ToString("F2") + " Kill Points per Continuous Mission; " + entry.Value[845].ToString() + " Planes written off; " + pwopm.ToString("F1") + " Planes written off per mission; " + entry.Value[791].ToString() + " Flights Ended by Self-Damage; " + entry.Value[778].ToString()
                      + " Deaths.", null);
                });

                this.mission.Timeout(4, () => {
                    this.mission.Stb_Message(to, "All time: " + (entry.Value[824] + totalkills).ToString() + " Total Kills; Highest Rank: " + this.mission.stb_StatRecorder.StbSr_RankFromName(entry.Key, null, true), null);
                });
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_saar"); }
    }

    public void StbSr_Calc_All_AceAndRank()
    {

        try
        {
            if (stbSr_LogStats)
            {
                if (stbSr_AllPlayerStats.Count == 0) return;
                //StbSr_WriteLine("Stats: Calculating Ace Level & Rank.");
                int curr_time_sec = Calcs.TimeSince2016_sec();


                foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                {
                    StbSr_Calc_Single_AceAndRank(entry, curr_time_sec);
                }
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_aaar"); }
    }

    //Displays and/or returns text string or html string showing players currently available aircraft
    //if nextAC = true, returns available aircraft at player's NEXT rank and ace level
    public string StbSr_Display_AircraftAvailable_ByName(Player player = null, bool nextAC = false, bool display = true, bool html = false)
    {
        string retmsg = "";
        string msg = "";
        string newline = Environment.NewLine;
        if (html) newline = "<br>" + Environment.NewLine;

        if (!this.mission.stb_restrictAircraftByRank && !this.mission.stb_restrictAircraftByKills)
        {
            msg = "This server has no aircraft restriction by rank or ace level.  You have access to all available aircraft.";
            retmsg += msg;
            if (display) this.mission.Stb_Message(new Player[] { player }, msg, null);
            return retmsg;
        }

        if (this.mission.stb_restrictAircraftByRank)
        {
            string message1 = "";
            if (nextAC) message1 = "Aircraft that will be unlocked when you are promoted or increase ace level: " + this.mission.stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForNextRank(player);
            else message1 = "Currently unlocked aircraft for your rank, " + this.mission.stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForRank(player);
            retmsg += message1;
            if (display) this.mission.Stb_Message(new Player[] { player }, message1, null);
        }



        if (this.mission.stb_restrictAircraftByKills)
        {
            string message1 = "";
            string message2 = "";

            if (nextAC)
            {
                message1 = "Aircraft you can unlock via more kills: ";
                message2 = this.mission.stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForNextAce(player);
            }
            else
            {
                message1 = "Unlocked aircraft for your number of kills: ";
                message2 = this.mission.stb_RankToAllowedAircraft.StbRaa_ListOfAllowedAircraftForAce(player);
            }
            if (message2 == "") message2 = "(none)";
            if (retmsg.Length > 0) retmsg += newline;
            retmsg += message1 + message2;
            mission.Timeout(2.0, () =>
            {
                if (display) this.mission.Stb_Message(new Player[] { player }, message1 + message2, null);
            });
        }
        return retmsg;
    }

    public void StbSr_Display_SessionStats(Player player)
    {

        //playerName = StbSr_MassagePlayername(player.Name(), player as AiActor);

        var currSessStat = new Mission.Stb_PlayerSessStat(mission); //save current state of stats, plus gets it back for current use
        currSessStat = mission.stb_SaveIPlayerStat.StbSis_Save(player);



        double bulletsPerc = 0;
        double bulletsAirPerc = 0;
        if (currSessStat.bulletsFire > 0)
        {
            bulletsPerc = (double)currSessStat.bulletsHit / (double)currSessStat.bulletsFire * 100;
            bulletsAirPerc = (double)currSessStat.bulletsHitAir / (double)currSessStat.bulletsFire * 100;
        }

        //Stb_PlayerSessStat PlayerStats = mission.stb_SaveIPlayerStat.StbSis_Save(player);  //save current state of stats, plus gets it back for current use
        //currSessStat.getSessStat(778);
        //PlayerStats.getSessStat(778);

        string msg1 = string.Format("Current Session Stats: {0:0.00} total Kill Points; {1:0.00}/{2:0.00}/{3:0.00}/{4:0.00} Air/AA/Naval/Ground Kill Points",
            (double)(currSessStat.getSessStat(798)) / 100, (double)(currSessStat.getSessStat(802)) / 100, (double)(currSessStat.getSessStat(806)) / 100, (double)(currSessStat.getSessStat(810)) / 100, ((double)currSessStat.getSessStat(814)) / 100);

        //Also include player's penalty points if there are any.
        if (currSessStat.getSessStat(847) < 0)
        {
            msg1 += string.Format("; Penalty Points: {0:0.00}", (double)(currSessStat.getSessStat(847)) / 100);
        }

        this.mission.Stb_Message(new Player[] { player }, msg1, null);


        mission.Timeout(2, () => { //Apparently Timeout is a method of mission . . . who knew?
            if (currSessStat.bulletsFire > 0) //OK, so if we have unlimited ammo in the server, it seems that CLOD never passes down the bullets & bombs info
                                              //In that case, and also just in case there actually haven't been any fired yet, we'll just hide these items as they are quite useless
            {
                string msg2 = string.Format("{0} bullets fired, {1:0.0}% hit a target, {2:0.0}% hit an aircraft",
                    currSessStat.bulletsFire, bulletsPerc, bulletsAirPerc);
                this.mission.Stb_Message(new Player[] { player }, msg2, null);
            }
        });

        mission.Timeout(4, () => {
            if (currSessStat.bombsFire > 0)
            {

                double bombsPerc = 0;
                double bombsOnTarget_kg = 0;
                if (currSessStat.bombsFire > 0)
                {
                    bombsPerc = (double)currSessStat.bombsHit / (double)currSessStat.bombsFire * 100;
                    bombsOnTarget_kg = (double)currSessStat.bombsHit / (double)currSessStat.bombsFire * (double)currSessStat.bombsWeight;
                }
                string msg3 = string.Format("{0} bombs dropped, {1:0.0}% hit targets, {2:N0} kg on targets", //, {3} bombs hit, {4} KG bombs dropped"
                    currSessStat.bombsFire, bombsPerc, bombsOnTarget_kg); //, currSessStat.bombsHit, currSessStat.bombsWeight);
                this.mission.Stb_Message(new Player[] { player }, msg3, null);
            }
        });

        //this.mission.Stb_Message(new Player[] { player }, "Current Session Stats: {0:0.00} Total Kill Points; {1:0.00}/{2:0.00}/{3:0.00}/{4:0.00} Air/AA/Naval/Ground Kill Points; {5} bullets fired, {6:0.0}% hit any target, {7:0.0}% hit an aircraft; {8} bombs dropped, {9:0.0}% hit targets, {10} kg on targets", new object[] { (double)(currSessStat.getSessStat(798))/100, (double)(currSessStat.getSessStat(802)) / 100, (double)(currSessStat.getSessStat(806)) / 100, (double)(currSessStat.getSessStat(810)) / 100, ((double)currSessStat.getSessStat(814)) / 100, currSessStat.bulletsFire, bulletsPerc, bulletsAirPerc, currSessStat.bombsFire, currSessStat.bombsHit, bombsPerc, bombsOnTarget_kg });
        //this.mission.Stb_Message(new Player[] { player }, "Current Session Stats: {0:0.00} Total Kill Points; {1:0.00}/{2:0.00}/{3:0.00}/{4:0.00} Air/AA/Naval/Ground Kill Points; {5} bullets fired, {6:0.0}% hit any target, {7:0.0}% hit an aircraft; {8} bombs dropped, {9:0.0}% hit targets, {10} kg on targets", new object[] { currSessStat.getSessStat(798), currSessStat.getSessStat(802), currSessStat.getSessStat(806), currSessStat.getSessStat(810), currSessStat.getSessStat(814), currSessStat.bulletsFire, bulletsPerc, bulletsAirPerc, currSessStat.bombsFire, currSessStat.bombsHit, bombsPerc, bombsOnTarget_kg });

        //int burg = currSessStat.getSessStat(798);
        //string burg=currSessStat.ToString();
        //int burg = 1;
        //this.mission.Stb_Message(new Player[] { player }, "Current Session Stats: {0:0.00} Total Kill Points; {1:0.00}/{2:0.00}/{3:0.00}/{4:0.00} Air/AA/Naval/Ground Kill Points; {5} bullets fired, {6:0.0}% hit any target, {7:0.0}% hit an aircraft; {8} bombs dropped, {9:0.0}% hit targets, {10} kg on targets", new object[] { burg, burg, burg, burg, currSessStat.bulletsFire, currSessStat.bulletsFire, bulletsPerc, bulletsAirPerc, currSessStat.bombsFire, currSessStat.bombsHit, bombsPerc, bombsOnTarget_kg });

        /* landings,
                bombsFire,
                bombsHit,
                bombsWeight,
                bulletsFire,
                bulletsHit,
                bulletsHitAir,
                fkills,
                //gkills,
                //fgkillIps.fgkills,
                kills,
                deaths,
                bails,
                ditches,
                planeChanges,
                planesWrittenOff,
                player
                */

    }


    //Display summary of kill point stats for ALL players to a certain player; Display only those on a certain side if side > 0.  Display to all players if player == null
    public string StbSr_Display_SessionStatsAll(Player player = null, int side = 0, bool display = true, bool html = true)
    {

        //playerName = StbSr_MassagePlayername(player.Name(), player as AiActor);

        double delay = 0.2;
        double delay_interval = 0.1;
        int total = 0;
        string res = "";
        string newline = Environment.NewLine;
        if (html) newline = "<br>" + Environment.NewLine;


        string msg = "A DE PL TotalK Air/AA/Naval/Ground(/Penalty) (KgOnTarget) Name";
        if (display) mission.Timeout(delay, () => { this.mission.Stb_Message(new Player[] { player }, msg, null); });
        else res += msg + newline;




        Dictionary<string, Mission.Stb_PlayerSessStat> stbSis_saveIPlayerStatTEMP = new Dictionary<string, Mission.Stb_PlayerSessStat>(mission.stb_SaveIPlayerStat.stbSis_saveIPlayerStat);
        //stbSis_saveIPlayerStatTEMP = stbSis_saveIPlayerStat; //doing it this way just makes them two different names for the same actual object
        /*
        List<KeyValuePair<string, Stb_PlayerSessStat>> sortList = stbSis_saveIPlayerStatTEMP.ToList();

        sortList.Sort(
            delegate (KeyValuePair<string, Stb_PlayerSessStat> pair1,
            KeyValuePair<string, Stb_PlayerSessStat> pair2)
            {
                return (pair1.Value.kills > pair2.Value.kills);
            }
        );
        */

        List<Tuple<double, string, string>> resultList = new List<Tuple<double, string, string>>(); //Tuple = # of kills (double), pilot  name, message line

        //testing only
        /*
        stbSis_saveIPlayerStatTEMP.Add("TWC_Flug1", stbSis_saveIPlayerStatTEMP["TWC_Flug"]);
        stbSis_saveIPlayerStatTEMP.Add("TWC_Flug2", stbSis_saveIPlayerStatTEMP["TWC_Flug"]);
        stbSis_saveIPlayerStatTEMP.Add("TWC_Flug15", stbSis_saveIPlayerStatTEMP["TWC_Flug"]);
        stbSis_saveIPlayerStatTEMP.Add("TWC_Flug152323234342", stbSis_saveIPlayerStatTEMP["TWC_Flug"]);
        */

        foreach (KeyValuePair<string, Mission.Stb_PlayerSessStat> entry in stbSis_saveIPlayerStatTEMP)
        {

            //string line = "";
            var currSessStat = new Mission.Stb_PlayerSessStat(mission); //save current state of stats, plus gets it back for current use
            Player currPlayer = entry.Value.player;
            currSessStat = mission.stb_SaveIPlayerStat.StbSis_Save(currPlayer);

            if (side > 0 && side != currPlayer.Army()) continue;  //skip any players in the wrong army, if side is specified

            string army = "";
            if (currPlayer.Army() != null)
            {
                if (currPlayer.Army() == 1) army = "R";
                if (currPlayer.Army() == 2) army = "B";
            }

            //Only show IF there is some non-zero stats to show
            double change = (double)currSessStat.deaths + (double)currSessStat.planesWrittenOff +
                (double)(currSessStat.getSessStat(798)) + (double)(currSessStat.getSessStat(802)) +
                (double)(currSessStat.getSessStat(806)) + (double)(currSessStat.getSessStat(810)) +
                ((double)currSessStat.getSessStat(814)) - currSessStat.getSessStat(847) + currSessStat.bombsHit;

            //PlayerStats.getSessStat(778); //is death total

            if (change > 0.01 || change < -0.01)
            {
                total++;
                string msg1 = "";

                //This doesn't work for some unknown reason, so just putting name @ end always
                //if (!display) msg1 = string.Format ("{0,-12} ",entry.Key); //left aligned & 12 spaces wide //doesn't work in-game as the in-game font is proportional, not fixed width, but we can use it for ie web page display, just make sure to use a fixed-width font 

                double twcKillPoints = (double)(currSessStat.getSessStat(798)) / 100;

                //for testing
                //twcKillPoints = stbSr_random.NextDouble() * 125.0;

                /* //This would work with fixed-width but doesn't work with variable width fonts
                 * msg1 += string.Format(army + " {0:0} {1:0} {2,5:0.00} {3,5:0.00}/{4,5:0.00}/{5,5:0.00}/{6,5:0.00}", (double)currSessStat.deaths,
                    (double)currSessStat.planesWrittenOff,
                    twcKillPoints, (double)(currSessStat.getSessStat(802)) / 100, (double)(currSessStat.getSessStat(806)) / 100, (double)(currSessStat.getSessStat(810)) / 100, ((double)currSessStat.getSessStat(814)) / 100);
                    */

                //we were using (double)currSessStat.deaths for deaths, which is the CloD deaths value over the session.  Now we'll try instead using
                //SessStat 778 which is the TWC way
                msg1 += string.Format(army + " {0:00} {1:00} {2:000.00} {3:00.00}/{4:00.00}/{5:00.00}/{6:00.00}", (currSessStat.getSessStat(778)),
                    (double)currSessStat.planesWrittenOff,
                    twcKillPoints, (double)(currSessStat.getSessStat(802)) / 100, (double)(currSessStat.getSessStat(806)) / 100, (double)(currSessStat.getSessStat(810)) / 100, ((double)currSessStat.getSessStat(814)) / 100);

                //Also include player's penalty points if there are any.
                if (currSessStat.getSessStat(847) < 0)
                {
                    msg1 += string.Format("/{0:0.00}", (double)(currSessStat.getSessStat(847)) / 100);
                }




                if (currSessStat.bombsFire > 0)
                {

                    double bombsPerc = 0;
                    double bombsOnTarget_kg = 0;
                    bombsPerc = (double)currSessStat.bombsHit / (double)currSessStat.bombsFire * 100;
                    bombsOnTarget_kg = (double)currSessStat.bombsHit / (double)currSessStat.bombsFire * (double)currSessStat.bombsWeight;
                    msg1 += string.Format(" {0:N0}kg", //, {3} bombs hit, {4} KG bombs dropped"
                        bombsOnTarget_kg); //, currSessStat.bombsHit, currSessStat.bombsWeight);

                }


                msg1 += " " + entry.Key; //Player's name last for in-game display where no fixed width formatting possible

                //if (display) mission.Timeout(delay, () => { this.mission.Stb_Message(new Player[] { player }, msg1, null); });
                //line += msg1;

                //Save this to a list along with a sortkey, then sort by the sortkey, then print it out.

                resultList.Add(new Tuple<double, string, string>(twcKillPoints, entry.Key, msg1));

            }
        }

        string playername = "";
        if (player != null && player.Name() != null) playername = player.Name();
        //Sorted w/ high score @ end, but also TODO with requesting player listed at VERY end
        resultList.Sort(
            delegate (Tuple<double, string, string> pair1, Tuple<double, string, string> pair2)
            {
                if (pair1.Item2 == playername) return 1; //list requesting player last, if there is one
                    else if (pair2.Item2 == playername) return -1; //list requesting player last, if there is one
                    return (pair1.Item1.CompareTo(pair2.Item1));
            });

        foreach (Tuple<double, string, string> entry in resultList)
        {
            delay += delay_interval;

            if (display) mission.Timeout(delay, () => { this.mission.Stb_Message(new Player[] { player }, entry.Item3, null); });

            res += entry.Item3 + newline;
        }



        if (total == 0)
        {
            string msg2 = "***No Netstats to report***";
            if (display) mission.Timeout(delay, () => { this.mission.Stb_Message(new Player[] { player }, msg2, null); });
            else res += msg2 + newline;
        }

        return res;

    }


    //<obj summary of team scores per session
    public string StbSr_Display_SessionStatsTeam(Player player)
    {

        /* 
         *             StbSis_AddToMissionStat(player, 835,(int)(Math.Round((CurrStats.kills - OldStats.kills)*100)));
    StbSis_AddToMissionStat(player, 836, (int)(Math.Round((CurrStats.fkills - OldStats.fkills) * 100)));
    StbSis_AddToMissionStat(player, 837, CurrStats.bulletsFire - OldStats.bulletsFire);
    StbSis_AddToMissionStat(player, 838, CurrStats.bulletsHit - OldStats.bulletsHit);
    StbSis_AddToMissionStat(player, 839, CurrStats.bulletsHitAir - OldStats.bulletsHitAir);
    StbSis_AddToMissionStat(player, 840, CurrStats.bombsFire - OldStats.bombsFire);
    StbSis_AddToMissionStat(player, 841, CurrStats.bombsHit - OldStats.bombsHit);

    double bombsOnTarget_kg = 0;
    if ((CurrStats.bombsFire - OldStats.bombsFire) > 0 ) bombsOnTarget_kg = (double)(CurrStats.bombsHit - OldStats.bombsHit) / 
       (double)(CurrStats.bombsFire - OldStats.bombsFire) * (CurrStats.bombsWeight - OldStats.bombsWeight); //This is an approximation is which is exact if all bombs deployed were of the same weight, and our best guess of KG on target if the various bombs were of varying weights.  If bombs were of varying weights, we just don't have the info here about which of them were the ones that hit & which missed.


    StbSis_AddToMissionStat(player, 842, (int)(Math.Round(bombsOnTarget_kg)));
    StbSis_AddToMissionStat(player, 845, CurrStats.planesWrittenOff - OldStats.planesWrittenOff);

        */

        Mission.Stb_PlayerSessStat BS = mission.stb_SaveIPlayerStat.BlueSessStats;
        Mission.Stb_PlayerSessStat RS = mission.stb_SaveIPlayerStat.RedSessStats;
        string ms = "";

        //Write out the current Red & Blue TEAM totals so that -main.cs can use them as part of mission objectives etc.
        try
        {
            if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX2", null); //testing disk output for warps
            using (StreamWriter sw = new StreamWriter(mission.stb_FullPath + "SessStats.txt"))
            {

                //Point team TOTALS for red & blue, respectively
                sw.WriteLine(RS.getSessStat(798));
                sw.WriteLine(BS.getSessStat(798));
                sw.WriteLine(DateTime.Now.ToUniversalTime().ToString("R"));

                //Now Air/AA/Naval/Ground points, then planes written off, for red, then blue
                sw.WriteLine(RS.getSessStat(802));
                sw.WriteLine(RS.getSessStat(806));
                sw.WriteLine(RS.getSessStat(810));
                sw.WriteLine(RS.getSessStat(814));
                sw.WriteLine(RS.getSessStat(845));
                sw.WriteLine(BS.getSessStat(802));
                sw.WriteLine(BS.getSessStat(806));
                sw.WriteLine(BS.getSessStat(810));
                sw.WriteLine(BS.getSessStat(814));
                sw.WriteLine(BS.getSessStat(845));

            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_writeSessStats.txt"); }




        double bulletsPerc = 0;
        double bulletsAirPerc = 0;
        if (BS.getSessStat(837) > 0) //bulletsfire
        {
            bulletsPerc = (double)BS.getSessStat(838) / (double)BS.getSessStat(837) * 100;
            bulletsAirPerc = (double)BS.getSessStat(839) / (double)BS.getSessStat(837) * 100;
        }

        //Stb_PlayerSessStat PlayerStats = mission.stb_SaveIPlayerStat.StbSis_Save(player);  //save current state of stats, plus gets it back for current use
        //currSessStat.getSessStat(778);
        //PlayerStats.getSessStat(778);

        string msg1 = string.Format("BLUE session totals: {0:0.0} total Kill Points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground Kill Points",
          (double)(BS.getSessStat(798)) / 100, (double)(BS.getSessStat(802)) / 100, (double)(BS.getSessStat(806)) / 100,
          (double)(BS.getSessStat(810)) / 100, (double)(BS.getSessStat(814)) / 100);

        //Also include Team's penalty points if there are any.
        if (BS.getSessStat(847) < 0)
        {
            msg1 += string.Format("; Penalty Points: {0:0.00}", (double)(BS.getSessStat(847)) / 100);
        }


        if (player != null) mission.Timeout(2, () => {
            this.mission.Stb_Message(new Player[] { player }, msg1, null);
        });

        ms = msg1;



        if (BS.bulletsFire > 0) //OK, so if we have unlimited ammo in the server, it seems that CLOD never passes down the bullets & bombs info
                                //In that case, and also just in case there actually haven't been any fired yet, we'll just hide these items as they are quite useless
        {
            string msg2 = string.Format("{0} bullets fired, {1:0.0}% hit a target, {2:0.0}% hit an aircraft",
                BS.getSessStat(838), bulletsPerc, bulletsAirPerc);
            if (player != null) mission.Timeout(3, () => {
                this.mission.Stb_Message(new Player[] { player }, msg2, null);
            });
            ms += "<br>" + msg2;
        }




        if (BS.getSessStat(840) > 0)
        {

            double bombsPerc = 0;
            if (BS.getSessStat(840) > 0)
            {
                bombsPerc = (double)BS.getSessStat(841) / (double)BS.getSessStat(840) * 100;
            }
            string msg3 = string.Format("{0} bombs dropped, {1:0.0}% hit targets, {2:N0} kg on targets", //, {3} bombs hit, {4} KG bombs dropped"
                (double)BS.getSessStat(840), bombsPerc, (double)BS.getSessStat(842)); //, currSessStat.bombsHit, currSessStat.bombsWeight);
            if (player != null) mission.Timeout(4, () => {
                this.mission.Stb_Message(new Player[] { player }, msg3, null);
            });
            ms += "<br>" + msg3;
        }



        //OK, now show the stats for RED
        bulletsPerc = 0;
        bulletsAirPerc = 0;
        if (RS.getSessStat(837) > 0) //bulletsfire
        {
            bulletsPerc = (double)RS.getSessStat(838) / (double)RS.getSessStat(837) * 100;
            bulletsAirPerc = (double)RS.getSessStat(839) / (double)RS.getSessStat(837) * 100;
        }

        //Stb_PlayerSessStat PlayerStats = mission.stb_SaveIPlayerStat.StbSis_Save(player);  //save current state of stats, plus gets it back for current use
        //currSessStat.getSessStat(778);
        //PlayerStats.getSessStat(778);

        string msg4 = string.Format("RED session totals: {0:0.0} total Kill Points; {1:0.0}/{2:0.0}/{3:0.0}/{4:0.0} Air/AA/Naval/Ground Kill Points",
              (double)(RS.getSessStat(798)) / 100, (double)(RS.getSessStat(802)) / 100, (double)(RS.getSessStat(806)) / 100,
              (double)(RS.getSessStat(810)) / 100, (double)(RS.getSessStat(814)) / 100);

        //Also include Team's penalty points if there are any.
        if (RS.getSessStat(847) < 0)
        {
            msg4 += string.Format("; Penalty Points: {0:0.00}", (double)(RS.getSessStat(847)) / 100);
        }


        if (player != null) mission.Timeout(5, () => {
            this.mission.Stb_Message(new Player[] { player }, msg4, null);
        });
        ms += "<br>" + msg4;



        if (RS.bulletsFire > 0) //OK, so if we have unlimited ammo in the server, it seems that CLOD never passes down the bullets & bombs info
                                //In that case, and also just in case there actually haven't been any fired yet, we'll just hide these items as they are quite useless
        {
            string msg5 = string.Format("{0} bullets fired, {1:0.0}% hit a target, {2:0.0}% hit an aircraft",
                RS.getSessStat(838), bulletsPerc, bulletsAirPerc);
            if (player != null) mission.Timeout(6, () => {
                this.mission.Stb_Message(new Player[] { player }, msg5, null);
            });
            ms += "<br>" + msg5;
        }




        if (RS.getSessStat(840) > 0)
        {

            double bombsPerc = 0;
            if (RS.getSessStat(840) > 0)
            {
                bombsPerc = (double)RS.getSessStat(841) / (double)RS.getSessStat(840) * 100;
            }
            string msg6 = string.Format("{0} bombs dropped, {1:0.0}% hit targets, {2:N0} kg on targets", //, {3} bombs hit, {4} KG bombs dropped"
                (double)RS.getSessStat(840), bombsPerc, (double)RS.getSessStat(842)); //, currSessStat.bombsHit, currSessStat.bombsWeight);
            if (player != null) mission.Timeout(7, () => {
                this.mission.Stb_Message(new Player[] { player }, msg6, null);
            });
            ms += "<br>" + msg6;
        }


        return ms;
    }


    int StbSr_SPSCount = 0;
    public void StbSr_SavePlayerStats(int[] p) //p[0] is the time delay requested, p[1]=1 means this is end-of-session stats requested, so force immediate save
                                               //p[2]=1 means do the uploadsituationmaps bit only, and then exit, if p[2]==1 doesn't exist then DON'T do the uploadsituationmaps
    {
        try
        {
            //timeouts give a little delay between saves, perhaps saves some server stuttering
            //StbSr_Calc_All_AceAndRank(); //OK, we calc ace/rank when we read the file in & at significant events like actor killed etc. Prob. no need to calc them again for every player EVERY TIME the files are saved.  They'll be calc-ed anew @ start of every mission if all else fails.

            //Do the FTP situation map thing 2X, separated by 60 seconds, to double the upload speed.  A Kludge; we should have the time between saves separate from the other things & setable via stats.ini
            //StbSr_UploadSituationMapFilesLowFilter();

            //OK, it looks like doing this via this.mission puts it right back into the main thread?  So, no savings on threading
            //and causes stutters/warps in the game.  Not good!
            //this.mission.Timeout(60, () => {  StbSr_UploadSituationMapFilesLowFilter();  });
            //StbSr_AlwaysWriteLine("Saving 1");
            if (p.Length >= 3 && p[2] == 1)
            {
                StbSr_UploadSituationMapFilesLowFilter();
                //StbSr_AlwaysWriteLine("Saving 2");
                return;
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        try
        {

            //StbSr_AlwaysWriteLine("Saving 2.5");
            StbSr_SavePlayerStatsStringToFileFull(p);
            StbSr_Display_SessionStatsTeam(null); //need to do this every 2 minutes or so or the -MAIN.cs file complains
                                                  //StbSr_AlwaysWriteLine("Saving 3");
                                                  //Only save HTML files once every 7X (or if this is the final save of the session), to save a bit on uploading/server capacity.  If stats save time is 2 minutes this gives 14 minute stats updates
            StbSr_SPSCount++;
            if (StbSr_SPSCount % 7 == 0 || p[1] == 1)
            {
                StbSr_SavePlayerStatsStringToFileMedium(p);
                StbSr_SavePlayerStatsStringToFileLow(p);
                //StbSr_AlwaysWriteLine("Saving 4");
            }
            //StbSr_AlwaysWriteLine("Saving 5");
            StbSr_BackupPlayerStats(p);
            //StbSr_AlwaysWriteLine("Saving 6");

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_BackupPlayerStats(int[] p)
    {
        try
        {
            if (stbSr_LogStats)
            {
                //So we have been having an issue where player stats are lost bec the stats file write is interrupted somehow
                //Then only part of the file is written and everything after that point is lost.
                //To solve this problem, we now write to a stbSr_PlayerStatsPathTxt + ".tmp" file
                //When that is 100% complete we then call this routine to move that 100% complete file to stbSr_PlayerStatsPathTxt
                //While we're at it we create some backups which are named according to the current date.
                //End result is that we will end up with two backup file for each date.
                //(Two files for each date might be overkill, we can remove one in the future if that turns out to be the case..)
                //TODO: Delete the older dated backup files after a certain period of time.

                //StbSr_WriteLine("Stats: Making stats file backups. S12.");


                if (!System.IO.File.Exists(stbSr_PlayerStatsPathTxt + ".tmp")) return;
                if (!System.IO.File.Exists(stbSr_PlayerStatsPathTxt + ".completeflag")) return;

                DateTime dt = DateTime.Now;

                var fullPath = Path.GetDirectoryName(stbSr_PlayerStatsPathTxt);
                var fileName = Path.GetFileName(stbSr_PlayerStatsPathTxt);
                var backPath = fullPath + @"\stats-data-backups\";
                string backupTxtFile1 = backPath + fileName + "-" + dt.ToString("yyyy-MM-dd") + "-A";
                string backupTxtFile2 = backPath + fileName + "-" + dt.ToString("yyyy-MM-dd") + "-B";

                //Create the directory for the playerstats.txt backup files, if it doesn't exist
                if (!System.IO.File.Exists(backPath))
                {


                    try
                    {
                        //System.IO.File.Create(backPath);
                        System.IO.Directory.CreateDirectory(backPath);
                    }
                    catch (Exception ex1) { StbSr_PrepareErrorMessage(ex1, "stbsr_backup"); }



                }

                //StbSr_WriteLine("Stats: times: " + System.IO.File.GetLastWriteTime(backupTxtFile2) + " " + dt.AddHours(-12) + " " + DateTime.Compare(System.IO.File.GetLastWriteTime(backupTxtFile2), dt.AddHours(-12)).ToString());

                int cmp = DateTime.Compare(System.IO.File.GetLastWriteTime(backupTxtFile2), dt.AddHours(-12));

                if (!System.IO.File.Exists(backPath)) //only try to make the backup files if the proper directory exists.  Otherwise the whole thing will fail & we won't end up with a -playerstats_full.txt file at all . . .
                {
                    //Only make the -B backup once every 12 hours, or if it doesn't exist
                    if (!System.IO.File.Exists(backupTxtFile2) ||
                       (cmp < 0))
                    {

                        //StbSr_WriteLine("Stats: Overwriting -B file - " + backupTxtFile2 + " " + backupTxtFile1);

                        try
                        {
                            System.IO.File.Delete(backupTxtFile2);
                        }
                        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_backup"); }

                        try
                        {
                            System.IO.File.Move(backupTxtFile1, backupTxtFile2);
                        }
                        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_backup"); }
                    }
                    try
                    {
                        System.IO.File.Delete(backupTxtFile1);
                    }
                    catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_backup"); }

                    try
                    {
                        System.IO.File.Move(stbSr_PlayerStatsPathTxt, backupTxtFile1);
                    }
                    catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_backup"); }


                }

                try
                {
                    System.IO.File.Delete(stbSr_PlayerStatsPathTxt);
                }
                catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_backup_delete_curr"); }

                try
                {
                    System.IO.File.Move(stbSr_PlayerStatsPathTxt + ".tmp", stbSr_PlayerStatsPathTxt);
                }
                catch (Exception ex)
                {
                    StbSr_PrepareErrorMessage(ex, "stbsr_backup_move_tmp_to_txt");
                    //If the rename of the .tmp to .txt fails this is DISASTROUS so we will try to
                    //save a copy of the current data file at least.  It will be namnd
                    //xxxx.txt-3030301 with date/time appended to end
                    Random ran = new Random();
                    //string r = ran.Next(1000000, 9999999).ToString();   
                    string r = dt.ToString("yyyy-MM-dd-HHmmss");

                    try
                    {
                        System.IO.File.Move(stbSr_PlayerStatsPathTxt + ".tmp", stbSr_PlayerStatsPathTxt + "-" + r);
                    }
                    catch (Exception ex1) { StbSr_PrepareErrorMessage(ex1, "stbsr_backup"); }
                }
                /* // Copy a file asynchronously
                using (FileStream SourceStream = File.Open(stbSr_PlayerStatsPathTxt, FileMode.Open))
                    {
                        using (FileStream DestinationStream = File.Create(backupTxtFile))
                        {
                            await SourceStream.CopyToAsync(DestinationStream);
                        }
                    }

                */
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_backup"); }
    }

    public void StbSr_SavePlayerStatsStringToFileFull(int[] p)
    {
        try
        {

            //We delete this file before starting to write the data to the .tmp file
            //Then we create the file again when complete
            //Thus, later, we can use the existence of this file as an indication
            //that the .tmp file has been successfully & completely written 
            try
            {
                System.IO.File.Delete(stbSr_PlayerStatsPathTxt + ".completeflag");
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_completeflag_delete"); }

            if (stbSr_LogStats)
            {
                if (stbSr_AllPlayerStats.Count == 0) return;
                //StbSr_WriteLine("Stats: Writing stats file to hard drive.");

                int currTime_sec = Calcs.TimeSince2016_sec();


                if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX3", null); //testing disk output for warps
                int numLines = (int)Math.Ceiling((stbSr_numStats / (double)50)); //numStats should be a mult of 50 but we round up here just in case.
                using (StreamWriter sw = new StreamWriter(stbSr_PlayerStatsPathTxt + ".tmp", false, System.Text.Encoding.UTF8))
                {
                    foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                    {
                        //TODO: time to drop stats should be settable in stats.ini file
                        if (entry.Value[829] > 0 && (currTime_sec - entry.Value[829]) > 60 * 60 * 24 * 30) continue; //drop any stats more than 30 days old TODO: Should be set in stats.ini

                        /* //The old way of writing out the stats, inflexible
                        sw.Write(";" + Calcs.escapeSemicolon(Calcs.escapeColon(entry.Key)) + ":");
                        sw.WriteLine();
                        for (int j = 0; j < 15; j++)
                        {
                            for (int i = 0; i < 50; i++)
                            {
                                sw.Write(entry.Value[i + (j * 50)].ToString());
                                sw.Write(",");
                            }
                            sw.WriteLine();
                        }
                        for (int k = 750; k < 800; k++)
                        {
                            sw.Write(entry.Value[k].ToString());
                            sw.Write(",");
                        }
                        sw.WriteLine();
                        */

                        sw.Write(";" + Calcs.escapeSemicolon(Calcs.escapeColon(entry.Key)) + ":");
                        sw.WriteLine();
                        for (int j = 0; j < numLines; j++)
                        {
                            for (int i = 0; i < 50; i++)
                            {
                                if (i + (j * 50) < stbSr_numStats) sw.Write(entry.Value[i + (j * 50)].ToString() + ",");   //(i + (j * 50)< stbSr_numStats) is a failsafe in case stbSr_numStats by chance is not a multiple of 50.                                      
                            }
                            sw.WriteLine();
                        }
                    }
                }
            }

            try
            {
                System.IO.File.Create(stbSr_PlayerStatsPathTxt + ".completeflag");
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stbsr_completeflag_create"); }

        }
        catch (Exception ex)
        {
            StbSr_PrepareErrorMessage(ex, "stbsr_spss");
            //if we get any kind of error in this routine, we just delete the (possibly/probably) incomplete .tmp file
            //Thus any incomplete tmp files are not propagated down into the backups
            try
            {
                System.IO.File.Delete(stbSr_PlayerStatsPathTxt + ".tmp");
            }
            catch (Exception ex1) { StbSr_PrepareErrorMessage(ex1, "stbsr_deleting_tmp_file"); }

        }
    }

    public void StbSr_SavePlayerStatsStringToFileMedium(int[] p)
    {
        try
        {
            if (stbSr_LogStats)
            {
                if (stbSr_LogStatsCreateHtmlMed)
                {
                    //later
                }
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    public void StbSr_SavePlayerStatsStringToFileLow(int[] p)
    {
        try
        {
            bool immediate_save = false;
            if (p.Length >= 2 && p[1] == 1) immediate_save = true;
            try
            {
                StbSr_SaveTeamStatsStringToFileLowFilter(mission.stb_LogStatsTeamSuffix, immediate_save);
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "statsTeamSave"); }
            try
            {
                StbSr_SavePlayerStatsStringToFileLowFilter("", true);
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "statsHTMLSave"); }
            try
            {
                StbSr_SavePlayerStatsStringToFileLowFilter(mission.stb_LogStatsDeadPilotsSuffix, false);
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "statsHTMLDeadSave"); }

            if (immediate_save)
            {

                //Immediate_save happens on end of mission/exit, so we delete the SessStats file here so it won't be around for the next mission

                try
                {
                    if (File.Exists(mission.stb_FullPath + "SessStats.txt")) File.Delete(mission.stb_FullPath + "SessStats.txt");
                }
                catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "stats-delete-SessStats.txt"); }
            }

        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "statsstringtofilelow"); }
    }
    public void StbSr_SavePlayerStatsStringToFileLowFilter(string fileSuffix, bool listLive = true)
    {
        try
        {
            if (stbSr_LogStats)
            {
                if (stbSr_LogStatsCreateHtmlLow)
                {
                    if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX4", null); //testing disk output for warps
                    using (StreamWriter sw = new StreamWriter(stbSr_PlayerStatsPathHtmlLow + fileSuffix + stbSr_PlayerStatsPathHtmlExtLow, false, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
                        sw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");
                        sw.WriteLine("<head>");
                        sw.WriteLine("<title>" + this.mission.stb_ServerName_Public + " Stats</title>");
                        sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\" />");
                        //sw.WriteLine("<script type = \"text/javascript\" src = \"//code.jquery.com/jquery-1.8.2.min.js\" ></script >");
                        //sw.WriteLine("<script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jquery/1.4.2/jquery.min.js\"></script>");
                        //sw.WriteLine("<script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.4/jquery-ui.min.js\"></script>");
                        //	
                        //
                        //
                        //<script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.4/jquery-ui.min.js"></script>

                        //sw.WriteLine("<script type=\"text/javascript\" src=\"res/javascript/jquery.fixheadertable.min.js\"></script>");

                        //sw.WriteLine("<script type=\"text/javascript\" src=\"http://ajax.googleapis.com/ajax/libs/jqueryui/1.8.4/jquery-ui.min.js\"></script>");
                        sw.WriteLine("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js\"></script>");
                        sw.WriteLine("<script src=\"res/js/jquery.stickytableheaders.js\"></script>");
                        sw.WriteLine("<script src=\"res/js/jquery.tablesorter.js\"></script>");
                        //	<link rel="stylesheet" media="all" href="css/custom.css" type="text/css">
                        //  <link rel="stylesheet" media="all" href="css/tablesorter.css" type="text/css">
                        //
                        //sw.WriteLine("<link rel=\"stylesheet\" media=\"all\" href=\"res/css/custom.css\" type=\"text/css\">");
                        sw.WriteLine("<link rel=\"stylesheet\" media=\"all\" href=\"res/css/tablesorter.css\" type=\"text/css\">");
                        sw.WriteLine("<script type=\"text/javascript\" src=\"res/js/stats-sticky.js\"></script>");
                        sw.WriteLine("<script type=\"text/javascript\" src=\"res/js/filter_table_rows.js\"></script>");


                        sw.WriteLine("");
                        sw.WriteLine("");
                        sw.WriteLine("");
                        sw.WriteLine("");
                        //sw.WriteLine("<script type=\"text/javascript\" src=\"sorttable.js\"></script>");
                        sw.WriteLine("<link rel=\"stylesheet\" href=\"res/css/stats-style.css\" type=\"text/css\" />");
                        sw.WriteLine("</head>");
                        //sw.WriteLine("<body onload= \"initSort()\">");
                        sw.WriteLine("<body>");

                        sw.WriteLine("<img style=\" max-height: 10em;\"src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-flying_tigers___col__edward_rector_by_roen911-d4msc2k.jpg\" align=right width=50%>");
                        sw.WriteLine("<h1>" + this.mission.stb_ServerName_Public + " Stats</h1>");


                        sw.WriteLine("<p>Last Update: " + DateTime.Now.ToUniversalTime().ToString("R") + "</p>");


                        //   public string stb_LogStatsUploadFilenameLow = stb_LogStatsUploadBasenameLow + stb_LogStatsUploadAddressExtLow;
                        // public string stb_LogStatsUploadFilenameDeadPilotsLow = stb_LogStatsUploadBasenameLow + stb_LogStatsDeadPilotsSuffix + stb_LogStatsUploadAddressExtLow;
                        sw.WriteLine(this.mission.stb_StatsWebPageLinksLine);
                        string alive_dead_pilots_line = "<p><i><b>Click for " + this.mission.stb_ServerName_Public + ":</b> <a href=\"" + stbSr_LogStatsUploadFilenameLow + "\">Current ALIVE pilots stats list</a> - <a href=\"" + stbSr_LogStatsUploadFilenameDeadPilotsLow + "\">DEAD pilots stats list (archive)</a></i></p>";
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) sw.WriteLine(alive_dead_pilots_line);

                        string ms = StbSr_Display_SessionStatsTeam(null) + "<br><br>" + StbSr_GetCampaignSummary();
                        ms += "<br>" + "<br>" + StbSr_Display_SessionStatsAll(null, 0, false);
                        sw.WriteLine("<table style=\"width:50%; margin-right:0px; margin-left:auto; float:right;\" border =\"1\" cellpadding=\"0\" cellspacing=\"1\">");
                        sw.WriteLine("<tr class=\"\"><td class=\"\"><h3>" + "TEAM Totals for Current Session" + "</h3></td></tr>");
                        sw.WriteLine("<tr class=\"\"><td class=\"\">" + ms + "</td></tr>");
                        sw.WriteLine("<tr class=\"\"><td class=\"\">" + "<a style=\"color:lightgrey;\" href=\"" + stbSr_LogStatsUploadFilenameTeamLow + "\">TEAM stats archive</a>" + "</td></tr>");
                        sw.WriteLine("</table>");

                        sw.WriteLine("<a href=\"#description\">Jump to server & stats description, rules & guidelines, career & promotion tips</a><p>");



                        sw.WriteLine("Filter stats: <input class=\"searchInput\" style=\"width: 13em\" value=\"(by Pilot Name, Rank, etc)\">");
                        sw.WriteLine("<table style=\"clear:both\" border=\"0\" cellpadding=\"0\" cellspacing=\"1\">");
                        sw.WriteLine("<thead>");
                        sw.WriteLine("<tr class=\"rh1\"><td class=\"dh bg0\" style=\"width:40em\">Name & Rank<hr size=\"1\" noshade=\"noshade\"/></td><td class=\"dh bg1\"style=\"width:50em\">Flights & Kills Summary<hr size=\"1\" noshade=\"noshade\"/></td>");
                        sw.WriteLine("<td class=\"dh bg2\"  style=\"width:30em\">Details about Sorties and Damage to Player<hr size=\"1\" noshade=\"noshade\"/></td>");
                        sw.WriteLine("<td class=\"dh bg3\" style=\"width:30em\">Details about Player Damage to Enemy<hr size=\"1\" noshade=\"noshade\"/></td>");
                        sw.WriteLine("<td class=\"dh bg4\" >Misc<hr size=\"1\" noshade=\"noshade\"/></td>");
                        sw.WriteLine("</table>");



                        sw.WriteLine("<table id =\"stats\" class=\"sortable tableWithFloatingHeader tablesorter\" border =\"0\" cellpadding=\"0\" cellspacing=\"1\">");
                        sw.WriteLine("<thead>");
                        //sw.WriteLine("<tr class=\"rh2\">");
                        sw.WriteLine("<th class=\"bg0\">Name<hr size=\"1\" noshade=\"noshade\"/></th>");
                        if (!listLive)
                        {
                            sw.WriteLine("<th class=\"bg0\">Death Date<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg0\">Self Kill?<hr size=\"1\" noshade=\"noshade\"/></th>");
                        }
                        sw.WriteLine("<th class=\"bg0\">Rank<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg0\">Ace Level<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Continuous Missions<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Sorties<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Full/ Shared/ Assist Victories<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Total Kills (any partic- ipation)<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">TWC Kill Point Total<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Ave. Kill Points Per Contin. Mission<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Ave. Kill Points Per Sortie<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Air/ AA/ Naval/ Ground Kill Point Totals<hr size=\"1\" noshade=\"noshade\"/></th>");

                        sw.WriteLine("<th class=\"bg1\">NetStats Kill Point Total<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg1\">Bombs: KG on Target<hr size=\"1\" noshade=\"noshade\"/></th>");
                        //sw.WriteLine("<th class=\"bg2\">Take-offs<hr size=\"1\" noshade=\"noshade\"/></th>");

                        if (listLive)
                        {
                            sw.WriteLine("<th class=\"bg0\">Deaths<hr size=\"1\" noshade=\"noshade\"/></th>");
                        }
                        sw.WriteLine("<th class=\"bg2\">Time (min.)<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg2\">Land- ings at/ away from Air- port<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg2\">Para- chute Suc- cess/ Fail <hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg2\">Times Player Health Damaged/ Aircraft Damaged/ Parts Cut Off<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg2\"># sorties ended due to self- damage/ Times self- damaged/ Planes written off<hr size=\"1\" noshade=\"noshade\"/></th>");
                        //sw.WriteLine("<th class=\"bg3\">Total Rounds Shot<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Rounds: Hit %<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Rounds: Hit Aircraft %<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Rounds: Total Shot<hr size=\"1\" noshade=\"noshade\"/></th>");
                        //sw.WriteLine("<th class=\"bg3\">Rounds Shot: Hit Air- craft %<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Bombs: Hit %<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Bombs: Total Dropped<hr size=\"1\" noshade=\"noshade\"/></th>");
                        //sw.WriteLine("<th class=\"bg3\">Bombs Dropped: Hit %<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Total Raw Dam- age Points<hr size=\"1\" noshade=\"noshade\"/></th>");
                        //sw.WriteLine("<th class=\"bg3\">Total Raw Dam- age Points<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Raw Dam- age Points from Bombing<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Raw Dam- age Points from Bombing Air/ AA/ Naval/ Ground<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Total Dam- age Hits (adj.)<hr size=\"1\" noshade=\"noshade\"/></th>");

                        sw.WriteLine("<th class=\"bg3\">Fuel / Systems / Guns / Controls-Flaps-Wheels / Cockpit / Engine Damage<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg3\">Damage to Ship or Tank<hr size=\"1\" noshade=\"noshade\"/></th>");

                        sw.WriteLine("<th class=\"bg3\">Wing/ Other Parts Cut<hr size=\"1\" noshade=\"noshade\"/></th>");


                        //sw.WriteLine("<th class=\"bg4\">Entered/ Left Position<hr size=\"1\" noshade=\"noshade\"/></th>");
                        //sw.WriteLine("<th class=\"bg4\">Moved Pos- it- ion<hr size=\"1\" noshade=\"noshade\"/></th>");                            
                        sw.WriteLine("<th class=\"bg4\">Highest Rank (all time)<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg4\">Total Kills (any partic, all time)<hr size=\"1\" noshade=\"noshade\"/></th>");
                        sw.WriteLine("<th class=\"bg4\">Con- nected/ Dis- con- nected<hr size=\"1\" noshade=\"noshade\"/></th>");

                        //sw.WriteLine("</tr></thead>");
                        sw.WriteLine("</thead>");
                        sw.WriteLine("<tbody id=\"filterable\">"); //"filterable" needed by the js table filtering routine
                        bool alternate = true;



                        foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                        {

                            if (listLive && entry.Key.Contains("||PLAYER DIED")) continue;
                            if (!listLive && !entry.Key.Contains("||PLAYER DIED")) continue;

                            if (alternate) { sw.WriteLine("<tr class=\"r1\">"); }
                            else { sw.WriteLine("<tr class=\"r2\">"); }
                            alternate = !alternate;

                            string deathDate = "";
                            string deathSelf = "";
                            string ts = "";
                            string name = "";
                            if (!listLive && entry.Key.Length > 14)
                            {
                                ts = entry.Key.Substring(14);
                                //entry.Key.Substring()                              
                                deathDate = ts.Substring(0, ts.IndexOf(" || "));
                                name = ts.Substring(ts.IndexOf(" || ") + 4);

                                if (name.Contains(" (self-kill)"))
                                {
                                    deathSelf = "Y";
                                    name = name.Replace(" (self-kill)", "");
                                }
                                sw.WriteLine("<td class=\"dn\">" + name + "</td>");//Name
                                sw.WriteLine("<td class=\"dn\">" + deathDate + "</td>");//death Date
                                sw.WriteLine("<td class=\"dn\">" + deathSelf + "</td>");//Self death?
                            }
                            else
                            {
                                sw.WriteLine("<td class=\"dn\">" + entry.Key + "</td>");//Name
                            }
                            //sw.WriteLine("<td>" + entry.Value[797].ToString() + " "  + entry.Value[796].ToString() + "</td>"); //testing
                            //sw.WriteLine("<td>" + this.mission.stb_StatRecorder.StbSr_RankFromName (entry.Key) + "</td>");//Rank appropriate
                            //we could just do RANK_NAMES_GB[entry.Value[797]] here, but there is a whole rather complicated
                            //thing to do to decide whether they should show the GB or DE rank name
                            // "class=invisible" spans are sortkeys - the js tablesorter will sort on string by default, so it
                            //we start the cell with an invisible string that will sort the entries correctly, we're all set.
                            sw.WriteLine("<td><span class=invisible>" + "[" + entry.Value[797].ToString("00") + "]</span>" + this.mission.stb_StatRecorder.StbSr_RankFromName(entry.Key) + "</td>");//Rank appropriate
                            sw.WriteLine("<td><span class=invisible>" + "[" + entry.Value[796].ToString("00") + "]</span>" + ACE_AWARD_NAMES[entry.Value[796]] + "</td>");//Ace

                            sw.WriteLine("<td>" + entry.Value[779].ToString() + "</td>");//Continuous Missions
                            sw.WriteLine("<td>" + entry.Value[844].ToString() + "</td>");//Sorties proceeding at least as far as a/c takeoff

                            //int totalkills = (entry.Value[647] + entry.Value[648] + entry.Value[649] + entry.Value[794]);
                            sw.WriteLine("<td> <span class=invisible>" + entry.Value[799].ToString("0000") + "|</span>" + entry.Value[799].ToString() + "/" + entry.Value[800].ToString() + "/" + entry.Value[801].ToString() + "</td>");//Full Victories/Shared Victories/Assists  (ALL TYPES COMBINED)       
                            int totalkills = StbSR_TotalKills(entry.Value);
                            sw.WriteLine("<td>" + totalkills.ToString() + "</td>");//Total kills
                            sw.WriteLine("<td>" + ((double)entry.Value[798] / 100).ToString("0.00") + "</td>");//Total of Kill POINTS (ALL TYPES COMBINED)

                            double kpm = 0;
                            //if ( entry.Value[779] != 0) {kpm = (double) totalkills / (double) entry.Value[779]; }  
                            if (entry.Value[779] != 0) { kpm = (double)entry.Value[798] / (double)entry.Value[779] / 100.0; } //of kill points per continuous mission
                            sw.WriteLine("<td>" + kpm.ToString("F2") + "</td>");//Ave Kill Points per Continuous Mission
                            double kps = 0;
                            //if ( entry.Value[779] != 0) {kpm = (double) totalkills / (double) entry.Value[779]; }  
                            if (entry.Value[844] != 0) { kps = (double)entry.Value[798] / (double)entry.Value[844] / 100.0; } //of kill points per sortie
                            sw.WriteLine("<td>" + kps.ToString("F2") + "</td>");//Ave Kill Points per Continuous Mission

                            sw.WriteLine("<td> <span class=invisible>" + entry.Value[802].ToString("000000") + "|</span>" + ((double)entry.Value[802] / 100).ToString("0.00") + "/" + ((double)entry.Value[806] / 100).ToString("0.00") + "/" + ((double)entry.Value[810] / 100).ToString("0.00") + "/" + ((double)entry.Value[814] / 100).ToString("0.00") + "</td>");//Air / AA / Naval / Ground Kill Point Totals   
                            sw.WriteLine("<td>" + ((double)entry.Value[835] / 100).ToString("0.00") + "</td>");//Total of NETSTATS Kill POINTS
                            sw.WriteLine("<td>" + entry.Value[842].ToString() + "</td>");//BOMBS/KG on target


                            //sw.WriteLine("<td>" + entry.Value[770].ToString() + "</td>");//Take Offs
                            if (listLive)
                            {
                                sw.WriteLine("<td>" + entry.Value[778].ToString() + "</td>");//Deaths
                            }
                            sw.WriteLine("<td>" + ((double)entry.Value[792] / 60).ToString("F1") + "</td>");//Flying time in minutes
                            sw.WriteLine("<td> <span class=invisible>" + entry.Value[771].ToString("0000") + "|</span>" + entry.Value[771].ToString() + "/" + entry.Value[772].ToString() + "</td>");//Safe landing / Crash Landing
                            sw.WriteLine("<td>" + entry.Value[775].ToString() + "/" + entry.Value[774].ToString() + "</td>");//Parachute success / fail                         
                            sw.WriteLine("<td> <span class=invisible>" + entry.Value[773].ToString("0000") + "|</span>" + entry.Value[773].ToString() + "/" + (entry.Value[781] + "/" + entry.Value[782]).ToString() + "</td>");//Health Damaged times / Aircraft Damaged times / Parts Cut Off times
                            sw.WriteLine("<td> <span class=invisible>" + entry.Value[791].ToString("0000") + "|</span>" + entry.Value[791].ToString() + "/" + entry.Value[790].ToString()
                             + "/" + entry.Value[845].ToString() + "</td>");//Sorties ended due to self damage / Times self-damaged / Planes Written Off                                 

                            int roundsShot = entry.Value[837];
                            //sw.WriteLine("<td>" + roundsShot.ToString()+"</td>");//Rounds Shot
                            double hitPerc = 0;
                            double hitAircraftPerc = 0;
                            if (roundsShot > 0)
                            {
                                hitPerc = (double)(entry.Value[838]) / (double)(roundsShot) * 100;
                                hitAircraftPerc = (double)(entry.Value[839]) / (double)(roundsShot) * 100;
                            }
                            //sw.WriteLine("<td>" + hitPerc.ToString("0.0")+"</td>");//Hit (any target) percentage of shots
                            //sw.WriteLine("<td> <span class=invisible>" + hitAircraftPerc.ToString("000.0") + "|</span>" +  hitAircraftPerc.ToString("0.0") + "%/" + hitPerc.ToString("0.0") + "%/" + roundsShot.ToString() + "</td>");//Hit (any target) percentage of shots
                            sw.WriteLine("<td>" + hitAircraftPerc.ToString("0.0") + "</td>");//Hit (any target) percentage of shots
                            sw.WriteLine("<td>" + hitPerc.ToString("0.0") + "</td>");//Hit (any target) percentage of shots
                            sw.WriteLine("<td>" + roundsShot.ToString() + "</td>");//Hit (any target) percentage of shots

                            //sw.WriteLine("<td>" + hitAircraftPerc.ToString("0.0") + "</td>");//Hit aircraft percentage of shots

                            int bombsShot = entry.Value[840];
                            //sw.WriteLine("<td>" + bombsShot.ToString() + "</td>");//Bombs fired
                            double bombHitPerc = 0;
                            if (bombsShot > 0) bombHitPerc = (double)(entry.Value[841]) / (double)(entry.Value[840]) * 100; // bombsShot;

                            sw.WriteLine("<td>" + bombHitPerc.ToString("0.0") + "</td>");//Bombs hit (any target) percentage of shots                    
                            sw.WriteLine("<td>" + bombsShot.ToString() + "</td>");//Bombs hit (any target) percentage of shots                 
                            int sumAllDamage = 0;
                            for (int i = 1; i < 642; i++) { sumAllDamage += entry.Value[i]; }//Damage is 1 to 641 & 650-769
                            for (int i = 650; i < 770; i++) { sumAllDamage += entry.Value[i]; }//Damage is 1 to 641 & 650-769 


                            int aa_naval_ground_kills = StbSR_NavalGroundKills(entry.Value);

                            sw.WriteLine("<td> <span class=invisible>" + ((uint)(entry.Value[818])).ToString("0000000000") + "|</span>" + ((uint)(entry.Value[818])).ToString("N0") + "</td>");//Raw Damage Points  //RDPs are formatted with commas, which makes the sorter parse them as strings, so we put in an invisible non-comma version @ the beginning for sorting purposes
                            sw.WriteLine("<td> <span class=invisible>" + ((uint)(entry.Value[830])).ToString("0000000000") + "|</span>" + ((uint)(entry.Value[830])).ToString("N0") + "</td>");//Raw Damage Points from Bombing
                            sw.WriteLine("<td> <span class=invisible>" + ((uint)(entry.Value[831])).ToString("0000000000") + "|</span>" + ((uint)(entry.Value[831])).ToString("N0") + "/" + ((uint)(entry.Value[832])).ToString("N0") + "/" + ((uint)(entry.Value[833])).ToString("N0") + "/" + ((uint)(entry.Value[834])).ToString("N0") + "</td>");//Damage Points from bombing detail

                            int adjustedDamageTotal = sumAllDamage + aa_naval_ground_kills * 20; //We add on an adjustment for aa/naval/ground kills because otherwise bomber pilots are at a great disadvantage
                            sw.WriteLine("<td>" + adjustedDamageTotal.ToString() + "</td>");//All Damage Total


                            int sumFuel = 0;
                            for (int i = 1; i < 58; i++) { sumFuel += entry.Value[i]; }//Fuel part damages are [1...57]                                
                            int sumElecHydPne = 0;
                            for (int i = 58; i < 78; i++) { sumElecHydPne += entry.Value[i]; }//ElecHydPne part damages are [58...77]

                            int sumGun = 0;
                            for (int i = 78; i < 138; i++) { sumGun += entry.Value[i]; }//Gun part damages are [78...137]

                            int sumCFBW = 0;
                            for (int i = 138; i < 169; i++) { sumCFBW += entry.Value[i]; }//CFBW part damages are [138...168]

                            int sumCockpit = 0;
                            for (int i = 169; i < 184; i++) { sumCockpit += entry.Value[i]; }//Cockpit part damages are [169...183]                                
                            int sumEng = 0;
                            for (int i = 184; i < 632; i++) { sumEng += entry.Value[i]; }//Engine part damages are [184...631]
                            int sumShipTank = 0;
                            for (int i = 632; i < 642; i++) { sumShipTank += entry.Value[i]; }//ShipTank part damages are [632...641]
                            sw.WriteLine("<td> <span class=invisible>" + sumFuel.ToString("00000") + "|</span>" + sumFuel.ToString() + "/" + sumElecHydPne.ToString() + "/" + sumCFBW.ToString() + "/" + sumCockpit.ToString() + "/" + sumEng.ToString() + "/" + sumShipTank.ToString() + "</td>");//Cockpit Parts
                            sw.WriteLine("<td>" + sumShipTank.ToString() + "</td>");//Cockpit Parts
                                                                                    //[642-649] is various tasks & kill participation counts
                                                                                    //sw.WriteLine("<td>" + entry.Value[647].ToString() + "/" + entry.Value[648].ToString() + "/" + entry.Value[649].ToString() + "</td>");//aircraft/ground/naval kill participation counts
                            int sumWingPartsCut = 0;
                            for (int i = 650; i < 683; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                            for (int i = 687; i < 691; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                            for (int i = 697; i < 705; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]    
                            int sumAllOthersCut = 0;
                            for (int i = 683; i < 687; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                            for (int i = 691; i < 697; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                            for (int i = 705; i < 770; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                            sw.WriteLine("<td>" + (sumWingPartsCut + sumAllOthersCut).ToString() + "</td>");//All Others Cut                                                                                                
                                                                                                            //sw.WriteLine("<td>" + entry.Value[787].ToString() + "/" + entry.Value[788].ToString() + "</td>");//Entered/Left Position
                                                                                                            //sw.WriteLine("<td>" + entry.Value[780].ToString() + "</td>");//Moved position                    
                            sw.WriteLine("<td>" + this.mission.stb_StatRecorder.StbSr_RankFromName(entry.Key, null, true) + "</td>");//highest rank ever achieved, in appropriate army
                            sw.WriteLine("<td>" + (entry.Value[824] + totalkills).ToString() + "</td>");//Total multi-lifetime kills
                            sw.WriteLine("<td> <span class=invisible>" + (entry.Value[776]).ToString("00000") + "|</span>" + entry.Value[776].ToString() + "/" + entry.Value[777].ToString() + "</td>");//Connected/Disconnected                                                       

                            sw.WriteLine("</tr>");
                        }
                        sw.WriteLine("</tbody></table>");
                        sw.WriteLine("<br>");
                        sw.WriteLine(this.mission.stb_StatsWebPageLinksLine); //specified in customization section
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) sw.WriteLine(alive_dead_pilots_line);

                        sw.WriteLine(this.mission.stb_StatsWebPageTagLine); //line referring/linking back to your main web page or whatever, specified in customization section above


                        //sw.WriteLine("<p><b>STATS NOTES:</b> Stats run the duration of one life, until you are killed or captured.</p>");
                        sw.WriteLine("<a name=\"description\"></a>");
                        sw.WriteLine("<img style=\" max-height: 10em;\" src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-de_havilland_dh_98_mosquito_by_jncarter-d61sl88-960x600.jpg\" align=right width=50%>");

                        sw.WriteLine("<h1>" + this.mission.stb_ServerName_Public + " &  Careers Overview</h1>");

                        string msg = "<p>These missions are designed as campaign, training, practice, and career servers, where you can fly and advance your career through promotions";
                        if (this.mission.stb_restrictAircraftByRank || this.mission.stb_restrictAircraftByKills) msg += ", gaining access to more aircraft and privileges";
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) msg += "--until you die, ending your career";

                        sw.WriteLine(msg + ".<p>");


                        sw.WriteLine("<h4>What the Promotion System Rewards</h4>");
                        sw.WriteLine("On the server you have a FIGHTER PILOT CAREER and a separate BOMBER PILOT CAREER. Fly clean, fight hard, and above all STAY ALIVE to keep your career moving forward.<p>");

                        sw.WriteLine("The promotion system rewards pilot who achieve victories, who fly continuous missions (ie, landing & taking off again from the same airport rather than hopping all over the map), who land their planes in one piece--writing off as few as possible, who avoid self-damaging their aircraft, and who stay alive through many sorties and missions.<p>");

                        sw.WriteLine("Above all, it rewards pilots who pursue a long and varied career, who develop many facets of their pilot career, rather than simply focussing on dogfighting, who fly complete extended missions rather than just jumping to into the nearest dogfight furball, and who use their sorites to help their team reach strategic and mission objectives.<p>");

                        sw.WriteLine("<h4>When Things Go Wrong--What Are the Best Options to Preserve Your Career?</h4>");
                        sw.WriteLine("Parachuting or landing safely anywhere on friendly ground is quite safe; parachuting or landing in friendly water slightly less safe; parachuting or landing on enemy ground quite dangerous; and parachuting or landing on enemy waters the most dangerous of all (as it was in real life).<p>");

                        sw.WriteLine("For career purposes, best option is to land your plane at a friendly airport in one piece. Next best is to land or parachute anywhere in friendly territory--this is quite safe. Far worse is to land or parachute in enemy territory--you have a fighting chance, at best, to escape and survive.  Worst of all, of course, is to get shot down or crash your aircraft.<p>");

                        sw.WriteLine("In contrast to most CloD servers, parachuting to the ground is quite safe (as it was in real life during WW2). What happens after you reach the ground in enemy territory or on water may be different, however.<p>");

                        sw.WriteLine("In contrast to most CloD servers, once you are on the ground and stopped (or moving very slowly) you are quite unlikely to have a career-ending death.  You may lose your aircraft to enemy attack or low-speed crash, but you will most likely survive. This is simply because these ground activites are not modeled as part of the game--you can't jump out of your plane and dive into a ditch in-game, for example. So, for career purposes, once you are on friendly ground and stopped, you are pretty safe.<p>");

                        sw.WriteLine("If you are still in the air at the end of a mission (or when the server disconnects for any reason) all is well and your career will continue. Successful landings do bolster your career--but missing the opportunity to record a successful landing is the only consequence of flying until server disconnect.");

                        sw.WriteLine("<h4>In-Game Chat Commands</h4>");
                        if (this.mission.stb_restrictAircraftByRank)
                        {

                            sw.WriteLine("<p>In-game, use chat commands <i>&lt;career</i>, <i>&lt;session</i>, <i>&ltac</i>, <i>&ltnextac</i>, and <i>&ltobj</i> to check your current career (rank, stats, kills), session stats, available aircraft, and aircraft that will be available when you receive your next promotion.<p>");

                            sw.WriteLine("<p>You start out your career with access to just two red and two blue aircraft. However, if you fly productive missions, you should receive a promotion(and unlock access to further aircraft) every 3 - 4 flights / 1 - 1.5 hours of flight time. Additionally, you unlock access to the Spitfire 1A(100 Oct) and the ME109 - E3 (fighter pilots) or HE-111P-2 and BR.20M (bomber pilots)  immediately when you reach five kills. <p>");

                            sw.WriteLine("<p>When you spawn in, you will see many aircraft listed. However, a beginning pilot has access to only two of them. If you spawn into an unauthorized aircraft, you will receive a message. Use Chat Command <i>&ltac</i> to check your authorized aircraft.<p>");
                        }
                        else if (this.mission.stb_restrictAircraftByKills)
                        {

                            sw.WriteLine("<p>In-game, use chat commands <i>&lt;career</i>, <i>&lt;session</i>, <i>&ltac</i>, <i>&ltnextac</i>, and <i>&ltobj</i> to check your current career (rank, stats, kills), session stats, available aircraft, and aircraft that will be available when you receive your next promotion.<p>");

                            sw.WriteLine("<p>You start out your career with restricted access to advanced aircraft. However, if you fly productive missions, you unlock access to advanced aircraft once you achieve 5-10 kills. <p>");

                            sw.WriteLine("<p>When you spawn in, you will see many aircraft listed. However, a beginning pilot has access to only some of them. If you spawn into an unauthorized aircraft, you will receive a message. Use Chat Command <i>&ltac</i> to check your authorized aircraft.<p>");
                        }
                        {
                            sw.WriteLine("<p>In-game, use chat commands <i>&lt;career</i>, <i>&lt;session</i>, and <i>&ltobj</i> to check your current career (rank and stats) and session stats.<p>");
                        }

                        sw.WriteLine("<h4>Bomber vs. Fighter Pilot Careers</h4>");
                        sw.WriteLine("Bomber pilots in Cliffs of Dover tend to have short but storied careers--it is just a fact of the situation and theater. For that reason, bomber pilot can rise through the ranks very quickly, and with few air-hours, in comparison with fighter pilots. Bomber pilots will want to concentrate on destroying as many targets as possible, while also extending their career through as many sorties as possible, to reach the highest possible rank before their inevitable-but heroic--demise.<p>");

                        sw.WriteLine("Because the career arc of bomber vs fighter pilots is so different, you have a separate career as each type of pilot.  Look for--and search for--<i>(bomber)</i> in the stats listings above to see bomber pilot careers.<p>");

                        sw.WriteLine("<h4>When You Die</h4>");
                        if (this.mission.stb_ResetPlayerStatsWhenKilled)
                        {
                            sw.WriteLine("<p>When you die, your career, rank, and aircraft access is reset. In addition, there is a short timeout before you can fly again, allowing the server to finalize stats from your previous career. So you want to work HARD to stay alive! Disengage early, while your aircraft is still flyable. Use your parachute. Try to parachute or crash land on land--not on water--if at all possible. Try to crash land or parachute onto friendly territory rather than enemy territory--if you are captured, your career ends. If on water, aim for landing in friendly territory, where your chance of survival is much higher.<p>");

                            sw.WriteLine("<p>Some traces of past lives are recorded as part of your stats, and those who have previously reached high ranks may find that it is just a <i>bit</i> easier to reach that rank the second time around . . . <p>");
                        }


                        sw.WriteLine("<p>We'll be looking for our first 20 - 50 - 100 - 150 flight (and even higher!) careers. Additional ranks and aircraft unlock all the way up!<p>");

                        sw.WriteLine("<h3>Ground Deaths</h3><p>" + this.mission.stb_ServerName_Public + " Stats are more lenient than Cliffs of Dover Stats in one specific area: Deaths while you are on the ground.</p><p>Many times if you hit a hangar or other ground object at low speed, after your aircraft explodes in CloD, you will see a message, <i>'You were injured in that incredible fiery explosion, but somehow survived.  Your career will continue.'</i></p> <p>This happens when you are on the ground and the collision speed is relatively low.</p><p> To maximize your chances of survival, make sure you are <i>on the ground</i> and <i>going as slowly as possible</i> prior to any impact or collision. For example, if you slide along the ground and crash into a hangar or house at 10-20MPH, you will very likely survive. If you crash into the building 20 feet up at 90MPH, you will almost certainly die.<p>");
                        sw.WriteLine("<h3>Ground Vehicles</h3><p>The " + this.mission.stb_ServerName_Public + " features <i>Ground Support Vehicles</i> at every airport--and also across the countryside wherever you should land or crash land.</p><p> Ground Support vehicles are servicing your aircraft when you spawn in, and will soon depart. When you land, Ground Support Vehicles will soon arrive to service your aircraft. If you have damage--or if you crash land--you will be met by one or more ambulances and fire trucks, depending on the severity of your damage and injuries. If you land in enemy territory, you maybe met by an armored vehicle, there to search for you and take you prisoner.</p><p>Ground vehicles will generally avoid you and should not crash into you or damage your aircraft (and if they do, it is generally a low-speed ground crash resulting in damage to aircraft but not loss of your life). However--<i><b>if <u>you</u> crash into <u>them</u>, you can very well kill yourself or do severe damage</b></i>.</p><p>Check carefully for ground vehicles before taxiing, taking off, or landing. Before taking off, <i>zoom your view</i> in the direction of your take off and ensure that no Ground Vehicles are in your path. Ground vehicle <i>can and do cross the runway, or park on or very near the runway</i>.  It is your job to ensure that your takeoff path is clear before using it.</p><P>Be aware of the behavior of Ground Vehicles when you land. Wherever you land or crash land, Ground Support Vehicles will gather.  So if you land and taxi to the spawn-in area before de-planing, Ground Vehicles will gather around your aircraft, creating a nuisance for anyone trying to spawn in.  Consider taxiing to the opposite side of the runway from the spawn-in area before deplaning. Try to get off the runway if possible before de-planing. If you stop on the runway, Ground Support Vehicles will rush to your location, closing the runway for a few minutes. </p><p>If you crash land, Ground Emergency Vehicles will gather to your aircraft, wherever it is. So, for example, if you crash on a runway, that runway will be closed for several minutes as Emergency Vehicles attend to you and remove the stricken aircraft.  It may be a smart strategic decision to crash land off the runway--perhaps in a remote corner of your airbase instead--simply to avoid closing it for your teammates.<p> <p>Note that Enemy Ground Support Vehicles are valid targets that will gain you Ground Kill Points. Fuel trucks, in particular, explode spectacularly.</p> ");



                        sw.WriteLine("<img style=\" max-height: 10em;\" src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-spitfire___free_flight_by_jncarter-d4bzkuy-960x600.jpg\" align=right width=50%>");
                        sw.WriteLine("<h2><b>Notes and Details about Rank & Ace Levels</b></h2>");
                        msg = "<H3>Ace Level</h3><p> Your <b>Ace Level</b> is based purely on the number of kills you have participated in.  Full Victory counts 1, Shared Victory counts 0.5, and Assist counts 0.25. 5 kills=Ace, 10 kills=Double Ace, etc. ";
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) msg += "When you die, your Ace Level--and the privileges that go with it--are reset.";
                        msg += "</p>";
                        sw.WriteLine(msg);


                        if (this.mission.stb_restrictAircraftByKills)
                        {
                            if (this.mission.stb_restrictAircraftByRank)
                                sw.WriteLine("<i>Your Ace Level partially determines which aircraft you have access to.</i> Five kills earns access to specific Spitfire & ME109 aircraft, independent of your current rank. In game, use Chat Commands <i>&lt;ac</i> and <i>&lt;nextac</i> for details about aircraft available currently and in the future.</p>");
                            else
                            {
                                sw.WriteLine("<i>Your Ace Level partially determines whether you have access to certain advanced aircraft.</p>");

                            }
                        }

                        msg = "<p><H3>Rank</h3>Your <b>rank</b> is based on a combination of number of sorties, hours of flight time, combat effectiveness (ie, kills per Continuous Mission), and total damage done. You can be promoted <i>or</i> demoted in rank--particularly for damaging your own aircraft or having a greatly lower combat effectiveness than previously. You get demerits against your rank when you damage your own aircraft";
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) msg += ". When you die, your rank--and all the privileges that go with it--are gone.  Your career also ends if you are captured. ";
                        else msg += " or die. ";
                        msg += "In short, the more and better you fly, the more productive your flights, the more you are able to link your individual sorties together to create seamless Continusou Missions, and the longer you live, the higher your rank.</p>";
                        sw.WriteLine(msg);
                        //int i = 0;
                        //TODO:need to put this divisor in the stats.ini file etc.
                        //also, ranks & various rank-related arrays should be in stats.ini
                        double divisor = 5;
                        if (mission.stb_ResetPlayerStatsWhenKilled) divisor = .7;
                        sw.WriteLine("<center><H4> British & German Ranks with approx. Number of Sorties Required to Attain</h4><table><hr><td>RAF Rank</td><td>Dienstgrade der Luftwaffe</td><td>Approx. # of Missions to Attain</td></th>");
                        //for (int i = 1; i < RANK_NAMES_GB.Length; i++)
                        for (int i = 0; i < 22; i++)
                        {
                            sw.WriteLine("<tr><td>" + RANK_NAMES_GB[i] + "</td><td>" + RANK_NAMES_DE[i] + "</td><td>~" + Math.Round(divisor * (double)RANK_FLIGHT_VALUES[i]).ToString("0") + " sorties</td></tr>");



                            /* sw.WriteLine("<H3>RAF vs Luftwaffe Ranks</h3><p>RAF ranks in order from lowest to highest are: <ul><li>Tyro ~ 0 <li>Pilot Officer ~ 3 <li>Flying Officer ~ 6 <li>Flight Lieutenant ~ 10 <li>Squadron Leader ~ 15 <li>Wing Commander ~ 20 <li>Group Captain ~ 25 <li>Air Commodore ~ 30 <li>Air Vice - Marshal ~ 35 <li>Air Marshal ~ 40 <li>Air Chief Marshal ~ 50</ul>Corresponding Luftwaffe ranks are: <ul><li>Neuling ~ 0 <li>Leutnant ~ 3 <li>Oberleutnant ~ 6 <li>Hauptmann ~ 10 <li>Major ~ 15 <li>Oberstleutnant ~ 20<li>Oberst ~ 25 <li>Generalmajor ~ 30 <li>Generalmajor ~ 35 <li>Generalleutnant ~ 40 <li>General der Luftwaffe ~ 50</ul></p>");
                             */
                        }
                        sw.WriteLine("</table></center>");
                        sw.WriteLine("<p>The number given after each rank is the approximate number of sorties you will need to complete to reach that rank.  This is, however, <i>only</i> an approximation, as your promotions do depend in a very dynamic way on your productivity as a pilot, your kills and damage done, your skill in linking several sorties together into one Continuous Mission, and hours spent in the air. You can approximately double the speed at which you attain a certain rank by consistently good flying.</p>");

                        sw.WriteLine("You can switch sides (from Blue to Red or Red to Blue) and retain your corresponding rank and privileges when you fly for the other team.</P><p> Note that the listed ranks are just the beginning--there are a large number of secret, special higher ranks you can reach if you achieve even more flights than those listed!</p>");

                        if (this.mission.stb_restrictAircraftByRank)
                        {
                            sw.WriteLine("<p><H3>Rank, Ace Level, and Access to Aircraft</h3>Your access to more advanced aircraft is determined mostly by your rank.</p>");
                            sw.WriteLine("<center><H4>Aircraft Available By Rank - Red</h4><table>");

                            foreach (KeyValuePair<string, int> item in mission.stb_RankToAllowedAircraft.stbRaa_AllowedAircraftByRank_Red)
                            {

                                if (item.Value < 22) sw.WriteLine("<tr><td>" + RANK_NAMES_GB[item.Value] + "</td><td>" + item.Key + "</td></tr>");

                            }
                            sw.WriteLine("</table><H4>Aircraft Available By Rank - Blue</h4><table>");
                            foreach (KeyValuePair<string, int> item in mission.stb_RankToAllowedAircraft.stbRaa_AllowedAircraftByRank_Blue)
                            {

                                if (item.Value < 22) sw.WriteLine("<tr><td>" + RANK_NAMES_DE[item.Value] + "</td><td>" + item.Key + "</td></tr>");

                            }

                            sw.WriteLine("</table></center><p>Again, this is just the beginning--if you are able to fly even more missions and achieve more victories, you will find even more special ranks and more special/secret aircraft unlocked above those levels.</p>");

                            if (this.mission.stb_restrictAircraftByKills)
                            {
                                sw.WriteLine("<H4>Aircraft Available By Ace Level</h4>");
                                sw.WriteLine("<p>One important bonus aircraft for each side (Spit 1A 100 Oct / ME109E-3 for fighter pilots; He-111P-2 for red bomber pilots & BR-20M for blue bomber pilots) is awarded when you achieve the rank of ace (5 kills).</p>");
                                sw.WriteLine("<p>What that means is that even a Tyro/Neuling with five kills has access to the most important aircraft needed to fly and have fun in CloD--a good turn fighter, a good energy fighter, and a good bomber with large bomb racks for each side. Skilled pilots should be able to complete reach that level within just one or two flights.</p><p>Yet at the same time, if you continue to move through the ranks, you will have access to even more aircraft at each level.</p>");
                                sw.WriteLine("<p>It also ensure that the proportion of aircraft is more realistic.  In the Battle of Britain, the skies were not filled with 109E-4/Ns and Spit IIAs. Small numbers of those more advanced aircraft were available to only a very, very few of the most skilled pilots. For example, <a href=\"https://en.wikipedia.org/wiki/Messerschmitt_Bf_109_variants\">only 15 E-4/Ns were ever built</a>. Far more pilots were flying Hurricanes, 109E-3s, and such.</p>");
                            }

                            sw.WriteLine("<p>In game, check your currently available aircraft using Chat Command <i>&lt;ac</i>. Check the list of aircraft that will be available with your next promotion using Chat Command <i>&lt;nextac</i>.</p>");
                        }
                        sw.WriteLine("<img style=\" max-height: 10em;\" src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-adolf_galland_by_jncarter-d4j9ck6-960x600.jpg\" align=right width=50%>");
                        sw.WriteLine("<h2><b>Notes and Details about Statistics</b></h2");
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) msg = "<p><H3>Stats Reflect One Career</h3>In this server, your career ends and your stats are re-set when you die. You can view your older stats (from previous lives) using the <a href=\"" + stbSr_LogStatsUploadFilenameDeadPilotsLow + "\">DEAD pilots stats list</a>.";
                        else msg = "<p>In this server, stats are compiled continuously over many flights/lives. Many deaths, as well as self-harm, will adversely affect your progress through the ranks, but your career will continue through death.</p>";
                        sw.WriteLine(msg);
                        //sw.WriteLine("<p>Note that you can continue a flight (in order to boost your kills-per-flight average, one of your most important statistics) by landing at / near an airport safely and then taking off from that same airport again.</ p > ");
                        //sw.WriteLine("<p>The number of times you were forced to abandon your sortie due to self-damage is tracked.  In addition, the number of times self-damage is registered is tracked. For example, if you blow your rads, that registered one incident of self-damage.  Soon your engine will overheat and fail in a few different ways--that will register 3-4-5 more incidents of self-damage. And so on.</p>");
                        sw.WriteLine("<H3>Sortie vs Continuous Mission; How to Link Several Sorties to Create a Continuous Mission; Self Damage</h3>A \"sortie\" is one take-off/one landing. Stats track sorties that proceeded at least as far as takeoff.  Just jumping into & right back out of an aircraft is not counted as a sortie, for example.");
                        sw.WriteLine("A \"Continuous Mission,\" as reported in the statistics, consists of several connected sorties, if each time you land safely (alive) and then take off again from the same airport where you landed. Since average Kill Points per Continuous Mission is a very important statistic (affecting, among other things, your rate of promotion through the ranks), it is to your advantage to string together as many sorties as possible into one Continuous Mission, by always landing at an active airport and then taking off again from that same airport.</p>");
                        sw.WriteLine("<p>Note that sorties aborted due to self-damage (ie, blown rads or botched landing for no reason) make that \"Continous Mission\" end upon landing, with no possibility of continuation. If you land away from an airport, are forced to exit your aircraft via parachute, or take off from a different airport than the one you just landed at, your Continuous Mission will end and a new Continuous Mission start.</p>");
                        sw.WriteLine("<H3>Tracking Kills</h3><p>Kills and damage are tracked several different ways:</P>");
                        sw.WriteLine("<ul><li><b>Total Kills (any participation):</b> Tracks every kill you had any part of--even 1% involvement counts. All four types of victories are counted: Aerial, AA, Naval, and Other Ground.");
                        sw.WriteLine("<li><b>Full/Shared/Assist Victories:</b> A system similar to that used on Storm of War, where contributing 75% or more to a victory is \"Full\"; 40%-75% is \"Shared\" and 0-40% is an \"Assist\". Again, all four types of victories are counted.");
                        sw.WriteLine("<li><b>TWC Kill Points Total:</b> TWC Kill Points are similar to the NetStats Kill Points system in CloD, where 75% participation in a kill awards 0.75 Kill Points, 20% awards 0.2 points, and so on. Different from NetStats is that this tracks all four types of victories--Air, AA, Naval, and Ground. One column shows Kill Points for each of these four types broken out separately, another column gives the overall Kill Points Total for all types combined, and a third column gives NetStats Kill Points for comparison.</ul>");
                        sw.WriteLine("<P>Both NetStats and TWC Kill Points weight the lethality of your damage in assessing points.  A small hole in the end of the wing carries a very low weight. Hits that damage the engine or cooling system count for more. Cutting off a wing or tail part counts even more. All damage to an object before it is killed, weighted by these factors, is tallied up and Kill Points awarded in proportion to the damage done that contributed to that kill.  This is in contrast to the damage reports (see below) which--for the most part--simply report the number of hits in various locations without making a determination of their lethality.</p>");
                        /* sw.WriteLine("<P>Another difference between NetStats and TWC Kill Points is that NetStats appears to award Kill Points whenever the other aircraft lands (at or away from any airport--but note the issues outlined in 'Landings Away from Airport' below) or ends its flight. TWC Kill Points (similarly to ATAG, Storm of War, and other similar campaigns) are awarded only when the enemy actually crash lands or dies. So if the enemy makes it back to land at an airport safely, survives until the end of the mission, or similar, no Kill Points are awarded. For those reasons, TWC Kill Points may be lower than NetStats points.  On the flip side, NetStats misses awarding many points for enemy planes you force down because CloD does not detect that these planes have 'crash landed'. By contrast, TWC Stats always detects your damage and victories over these planes--after a few minutes of inactivity or at the end of the mission.  TWC Stats will award the Kill Points properly for any enemy aircraft you have forced down, whether on friendly or enemy territory. Only planes that return to base (or reach the edge of the map, where they are de-spawned) escape from your Kill Point totals. So in this area, TWC Stats is more complete and more accurate than NetStats, and you may find that your Kill Points are significantly higher than NetStats for some missions.</p>"); */
                        sw.WriteLine("<H3>Four Type of Victories</h3><p>We track statistics in four areas, depending on the target. 1) Air kills are other aircraft. 2) AA kills are antiaircraft, artillery, and tanks. 3) Naval kills are ships (not counting amphibious). 4) Ground kills are any other type of ground or naval object.");
                        sw.WriteLine("<H3>CloD NetStats vs TWC Stats</h3><P>TWC Kill Points uses the same damage weighting system that CloD NetStats uses--using the history of damage to an object the CloD provides along with the weights for different types of damage to the object over time--all tracked and provided by CloD. In general, TWC Kill Points and NetStat points awarded for a particular engagement will be the same or very similar--with differences occuring in edge cases, as outlined above.</p>");
                        sw.WriteLine("<p>However, many times after a mission you might compare the two stats and notice that the CloD NetStats credits you with Kill Points that are not reflected in on the TWC stats page--or the other way around. This is because in-game NetStats credit damage when the opposing aircraft's flight ends, whether by crashing or simply flying home and landing. TWC Kill Stats (all four types listed above) count only those objects that were actually killed or crash landed, and not those that returned safely to base. However, the various damage counts on the stats page DO total all damage done by you, whether that ended in the ultimate destruction of the aircraft or not.</p>");
                        sw.WriteLine("<p>And on the flip side, you will receive TWC stats points for many things that are not captured in online NetStats--such as bombing a ship or killing a ground target. This is the same system that NetStats carries out for Aerial Victories, but we also include AA, Naval, and Ground Victories in our totals.<p><p>Additionally, NetStats misses awarding many points for enemy planes you force down because CloD does not detect that these planes have 'crash landed'. If you completely destroy the aircraft, CloD rarely misses it. But you wound it severely so that it is forced to ditch nearby, CloD will often miss the victory. By contrast, TWC Stats always detects your damage and victories over these planes--after a few minutes of aircraft inactivity on the ground or, if all else fails, at the end of the mission.  TWC Stats will award the Kill Points properly for any enemy aircraft you have destroyed <i>or</i> forced down, whether on friendly or enemy territory. Only planes that return to base (or reach the edge of the map, where they are de-spawned) escape from your Kill Point totals. So in this area, TWC Stats is more complete and more accurate than NetStats, and you may find that your Kill Points are significantly higher than NetStats for some missions.</p>");

                        sw.WriteLine("<img style=\" max-height: 10em;\" src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-spitfire___free_flight_by_jncarter-d4bzkuy-960x600.jpg\" align=right width=50%>");
                        sw.WriteLine("<H2>Damage Reports and Types</h2><p>Most types of damage recorded on the stats page are simple counts of how many times damage was done without necessarily factoring in the amount or severity of damage. For example, if you hit an enemy on the right wing, the tail, and fuselage, that will register as doing damage '3 times'. If you hit them pretty hard, you may register damage 6 or 8 times hitting those three parts.  But in general, the damage totals simply count up how many times you have damaged the various parts and systems of the enemy aircraft.  It has little relation to how much damage your hit has done. For example, a bomb destroying a factory is one hit; so is a machine gun bullet passing through the fabric of a wing and leaving a small hole. Think of the damage counts as indicating how many times you have hit enemy aircraft or vehicles in the locations indicated without trying to give any indication about how significant that damage is.</p>");
                        sw.WriteLine("<p>This is in contrast to the \"Kill Point\" system mentioned above, which <i>does</i> weight various types of damage according to their lethality.</p>");
                        sw.WriteLine("<h3>Total Damage Hits</h3><p><b>'Total Damage Hits (adj.)'</b> is literally a count of each and every time you have damaged an enemy. Each hit counts as 1, regardless of how hard or how soft a hit it was. It includes an adjustment/bonus for AA, Naval, & Ground kills because those are otherwise undercounted in our damage totals. The adjustment helps to bring bomber pilots to parity with fighter pilots in accumulating points and rank.</p>");
                        sw.WriteLine("<h3>Parts Cut Off</h3><p><b>'Parts Cut Off'</b> are when aircraft wings, tails, nose, etc are cut, smashed, broken, or shot off.  This could be, for example, just the wing tip, half the wing, or a whole wing, whole tail, whole nose, etc. Generally speaking, these are very major hits--just one of them can down an aircraft.</p>");
                        sw.WriteLine("<h3>Raw Damage Points</h3><p><b>'Raw Damage Points' (RDPs)</b> are the one exception to the general rule that damage counts don't factor in damage severity. Raw Damage Points <i>are</i> scaled according to an assessment of the damage each hit caused. For that reason, RDPs are a very good assessment of how hard you are hitting the enemy.   RDPs work this way:  For every hit you make on an enemy object, which eventually leads to a kill, CloD assesses a damage value which is larger or smaller depending in the strength or potential damage of the hit. The Kill Point system works by adding up all of these individual RDP values for one particular kill and then setting that Kill Total to 1.  So if it took 50,000 RDPs to down an enemy and you contributed 25,000 of them, you are award 0.5 Kill Points.</p><p>Raw Damage Points simply total all of these Raw Damage Points without further refining or proportioning them. Raw Damage Points for a single damage hit might range from 1 to several thousand. Total Raw Damage Points are given and in addition, to help bomber pilots track their progress, Raw Damage Points from bombing missions are broken out separately.</p>");

                        sw.WriteLine("<img style=\" max-height: 10em;\" src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-flying_tigers___col__edward_rector_by_roen911-d4msc2k.jpg\" align=right width=50%>");
                        sw.WriteLine("<h2>Landings, Parachute</h2><h3>Landings at Airport</h3><p><b>'Landings at Airport'</b> are registered when you see a message from the server 'XX has returned to base', 'XX has landed safely', 'XX is safe on the ground', or similar. Generally to receive this message & register your landing, you must be on the ground at an airport with your aircraft stopped.  Note that you receive this message whether it was a great landing or you aircraft disintegrated. As long as you are alive, on the ground, and stopped at an airport, the landing will register. Note, however, that landings involving self-damage will register in the 'self-damage' stats and will also prevent you from continuing your continuous Flight (for stats purposes).</p>");
                        sw.WriteLine("<h3>Landings Away From Airport</h3><p><b>'Landings Away From Airport'</b>--any landing or crash landing that is not at a friendly airport. Please note that CloD's tracking of landings away from airport is a bit flakey--it doesn't record all such landings. The landings you will see recorded here are the ones where CloD reports 'XX has crash landed'. <i>What is the risk to my life/career?</i> Landing in friendly territory is pretty safe, as long as you survive the landing. However, landing in enemy territory carries a risk of capture, which will end your career.</p>");
                        sw.WriteLine("<h3>Landings in Water</h3><p><b>'Landings in Water'</b> carry more risk than if you are able to put down on dry land. Note that streams and lakes are equally dangerous as the ocean.  Note that your chance of rescue is higher if you put down on friendly waters vs enemy waters.</p>");
                        sw.WriteLine("<h3>Parachute Landings</h3><p>We feel that CloD overmodels the danger of <b>parachute</b> failure. In this server, your main or reserve chute will open <i>almost</i> every time (note Chat Messages whenever you parachute). Your primary dangers in parachuting are landing in water or in enemy terroritory. So the stats as recorded here do not reflect whether your chute opened or not (because it opens essentially every time). Instead, the report is whether you survived the entire experience--that is, whether you drowned, were captured upon landing, or survived. </p><P><i>What is the risk to my life/career?</i> Odds of drowning or capture when your parachute are the same as if you 'Land Away From Airport' as outlined above. Parachuting onto friendly land is quite safe. If you parachute onto water you have a risk of drowning. Enemy territory carries a risk of capture. Try to parachute onto dry land if possible, and onto friendly territory. You'll note that you find out the result of your parachute jump immediately upon exiting the aircraft (check Chat Messages), so there is no advantage, but also no further negative consequence, if you exit the server or jump into a new plane before your parachute reaches the ground.</p>");
                        sw.WriteLine("<h3>Capture</h3><p>If you land or parachute, on land or water, in enemy territory, you stand a chance of <b>'capture'</b>. Capture ends your career--the same consequence for your career and statistics as death.</p>");
                        sw.WriteLine("<h3>Planes Written Off</h3><p>Any time your plane is severely damaged or destroyed--or if you land in water or far from a friendly airport--your plane is considered destroyed and 'written off'. If you parachute (or exit the game mid-flight, which is the same as parachuting at that moment), of course your plane will crash and be written off.</p><p>Even good pilots will have to write off some planes--enemy action may damage your aircraft, and preserving your life and experience and training as a pilot is a far higher priority than saving and aircraft. Nevertheless, if you write off too high a proportion if your aircraft, and brass starts to look askance at you when the time gives to hand out promotions. Try to preserve your life most of all, but when you can, preserve your aircraft, too. Most of all, try to avoid write-offs that result only from self-damage.  It is one thing if your aircraft is forced down by enemy action--quite another if you simply crash land due to carelessness. Careless self-damage and resulting write-offs count against you most of all when you are in line for a promotion.</p>");
                        sw.WriteLine("<h3>Disconnects - Quitting the Server - Exiting Your Plane <u>in the Air</u></h3><p>In some servers you can avoid death or avoid giving credit to other players who have injured you, by simply exiting the server or jumping into another plane quickly. Not so on this server! Exiting the aircraft has exactly the same consequences as exiting your aircraft by parachute at the same moment--you will lose the plane, but probably/possibly save your life. If you exit over enemy territory or water, you stand a chance of drowning or capture.  If you exit over friendly territory, you stand a very good chance of survival--exactly the same as if you parachuted. This means that you can exit the server quickly if need be (especially if over friendly territory) but you gain no particular advantage by doing so, because you could gain the same result by simply parachuting out.</p>");
                        sw.WriteLine("<h3>Disconnects - Quitting the Server - Exiting Your Plane <u>on the Ground</u></h3><p>If you exit your aircraft while it is on the ground, the consequence is the same as if your aircraft collided with an object at its current speed. So if you exit at a low speed--stationary or under 5MPH--you should be fine.  If you are going 30 MPH when you disconnect, you may see some consequences and if you are going 80 MPH you will likely die. Generally speaking, try to come to a stop before disconnecting or exiting the plane. But, for example, if your internet connection should die while you are taxiing, you'll most likely sustain some damage but survive.</p>");
                        sw.WriteLine("<P></p><p><i>Note that stats are for fun and for information only. These stats will differ from in-game Netstats for a variety of reasons. These statistics depend on the information passed by CLoD to the server, which is not always complete or reliable. In addition, the statistics code may have bugs or be incomplete. In short, take these statistics as some potentially fun and useful information about your flying, but not necessarily complete or completely accurate.</i></p>");

                        sw.WriteLine(this.mission.stb_StatsWebPageLinksLine); //specified in customization section
                        if (this.mission.stb_ResetPlayerStatsWhenKilled) sw.WriteLine(alive_dead_pilots_line);

                        sw.WriteLine(this.mission.stb_StatsWebPageTagLine); //line referring/linking back to your main web page or whatever, specified in customization section above

                        sw.WriteLine("</body></html>");
                    }

                    //now upload the file via FTP
                    StbSr_UploadSSL(stbSr_LogStatsUploadAddressLow + fileSuffix + stbSr_LogStatsUploadAddressExtLow,
 stbSr_LogStatsUploadUserName, stbSr_LogStatsUploadPassword,
 stbSr_PlayerStatsPathHtmlLow + fileSuffix + stbSr_PlayerStatsPathHtmlExtLow);

                }


            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
    }

    /********************************************
     * 
     * Save TEAM stats to html file & upload via ftp 
     * 
     ***********************************************/

    string StbSr_STSS_old_ms = "";
    public void StbSr_SaveTeamStatsStringToFileLowFilter(string fileSuffix, bool immediate_save)
    {
        try
        {
            if (stbSr_LogStats)
            {
                if (stbSr_LogStatsCreateHtmlLow)
                {
                    if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX8", null); //testing disk output for warps
                    int save_min = 15;
                    string filename = stbSr_PlayerStatsPathHtmlLow + fileSuffix + stbSr_PlayerStatsPathHtmlExtLow;

                    //string previous_filename = stbSr_PlayerStatsPathHtmlLow + fileSuffix + "-previous" + stbSr_PlayerStatsPathHtmlExtLow;

                    string prev_date_for_filename = DateTime.Now.AddDays(-1).ToUniversalTime().ToString("-yyyy-MM-dd");
                    string previous_filename = stbSr_PlayerStatsPathHtmlLow + fileSuffix + prev_date_for_filename + stbSr_PlayerStatsPathHtmlExtLow;
                    bool file_exists = File.Exists(filename);
                    bool prev_file_exists = File.Exists(previous_filename);
                    DateTime lastwrite = new DateTime(0);

                    try
                    {
                        if (file_exists) lastwrite = File.GetLastWriteTimeUtc(filename);
                    }
                    catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "lastwritetime"); }

                    DateTime now = DateTime.UtcNow;

                    /* Random ran = new Random();
                    //KLUDGY stuff, but . . .
                    //We save this only once every 15 stats saves.  So about every 30 minutes assuming stats save every 2 minutes.
                    if (file_exists && ran.Next(1, 15) != 1) return;

                    //Most of the time we will append but every once in a while we overwrite (1/60 means about once every 10 missions)
                    //This gives us some history but keeps file from getting TOO huge
                    bool append = true;
                    if (!file_exists || ran.Next(1, 60) == 1) append=false;
                    */

                    bool append = true;
                    if (!file_exists || lastwrite.ToString("dd") != now.ToString("dd")) append = false; //every day when UTC date changes we start a new file & save the previous one day                        


                    //Copy over to prev-day file if we are starting a new file here, OR the prev-day file doesn't exist
                    if (file_exists && (!append || !prev_file_exists))
                    {
                        try
                        {
                            if (prev_file_exists) File.Delete(previous_filename);
                        }
                        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "teamdelete"); }
                        try
                        {
                            File.Copy(filename, previous_filename);
                        }
                        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "teamcopy"); }
                    }


                    string ms = StbSr_Display_SessionStatsTeam(null);
                    bool changed = (ms != StbSr_STSS_old_ms);
                    StbSr_STSS_old_ms = ms;


                    //Now append the Campaign Update (created by -main.cs) if it exists, but only on the FINAL team score summary for the mission
                    //Testing.mis experiment: Saving it for ALL team score summaries, not just the final one.
                    //if (immediate_save)
                    {

                        ms += "<br><br>" + StbSr_GetCampaignSummary();

                    }
                    //Add on the NetStats summary of all player action, but only for the FINAL save of the game
                    if (immediate_save)
                    {
                        ms += "<br>" + "<br>" + StbSr_Display_SessionStatsAll(null, 0, false);
                    }

                    //and exit, unless time has arrived to make a new file AND the data has actually changed, OR there is no existing file OR this is a forced immediate save
                    if (file_exists && !immediate_save && (lastwrite.AddMinutes(save_min) > now || !changed)) return;

                    if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX5", null); //testing disk output for warps
                    using (StreamWriter sw = new StreamWriter(filename, append, System.Text.Encoding.UTF8))
                    {

                        if (!append)
                        {
                            sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
                            sw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");
                            sw.WriteLine("<head>");
                            sw.WriteLine("<title>" + this.mission.stb_ServerName_Public + " Stats</title>");
                            sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\" />");
                            sw.WriteLine("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js\"></script>");
                            sw.WriteLine("<script src=\"res/js/jquery.stickytableheaders.js\"></script>");
                            sw.WriteLine("<script src=\"res/js/jquery.tablesorter.js\"></script>");
                            sw.WriteLine("<link rel=\"stylesheet\" media=\"all\" href=\"res/css/tablesorter.css\" type=\"text/css\">");
                            sw.WriteLine("<script type=\"text/javascript\" src=\"res/js/stats-sticky.js\"></script>");
                            sw.WriteLine("<script type=\"text/javascript\" src=\"res/js/filter_table_rows.js\"></script>");


                            sw.WriteLine("");
                            sw.WriteLine("");
                            sw.WriteLine("");
                            sw.WriteLine("");
                            sw.WriteLine("<link rel=\"stylesheet\" href=\"res/css/stats-style.css\" type=\"text/css\" />");
                            sw.WriteLine("</head>");
                            sw.WriteLine("<body>");

                            sw.WriteLine("<img style=\" max-height: 10em;\"src=\"http://twcclan.com/wp-content/uploads/2013/12/cropped-flying_tigers___col__edward_rector_by_roen911-d4msc2k.jpg\" align=right width=50%>");
                            sw.WriteLine("<h1>" + this.mission.stb_ServerName_Public + " TEAM Stats</h1>");


                            sw.WriteLine("<p>For " + DateTime.Now.ToUniversalTime().ToString("ddd, dd MMM yyyy 'GMT'") + " - scroll to end for most recent stats</p>");
                            sw.WriteLine("<p><a href=\"" + stbSr_LogStatsUploadFilenameTeamPrevLow + prev_date_for_filename + mission.stb_LogStatsUploadAddressExtLow + "\">Go to Team Stats for " + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("ddd, dd MMM yyyy 'GMT'") + "</a>" + "</p>");

                            sw.WriteLine(this.mission.stb_StatsWebPageLinksLine);
                            string alive_dead_pilots_line = "<p><i><b>Click for " + this.mission.stb_ServerName_Public + ":</b> <a href=\"" + stbSr_LogStatsUploadFilenameLow + "\">Current ALIVE pilots stats list</a> - <a href=\"" + stbSr_LogStatsUploadFilenameDeadPilotsLow + "\">DEAD pilots stats list (archive)</a></i></p>";
                            if (this.mission.stb_ResetPlayerStatsWhenKilled) sw.WriteLine(alive_dead_pilots_line);






                        }


                        sw.WriteLine("<table style=\"\" border =\"1\" cellpadding=\"0\" cellspacing=\"1\">");
                        if (immediate_save) sw.WriteLine("<tr class=\"\"><td class=\"\"><h3>" + "FINAL TEAM TOTALS for mission ending at " + DateTime.Now.ToUniversalTime().ToString("R") + "</h3></td></tr>");
                        else sw.WriteLine("<tr class=\"\"><td class=\"\"><h3>" + "TEAM Totals for " + DateTime.Now.ToUniversalTime().ToString("R") + "</h3></td></tr>");
                        sw.WriteLine("<tr class=\"\"><td class=\"\">" + ms + "</td></tr>");
                        sw.WriteLine("</table>");

                    }
                    if (stbSr_LogStatsUploadHtmlLow)
                    {
                        //upload the main file
                        StbSr_UploadSSL(stbSr_LogStatsUploadAddressLow + fileSuffix + stbSr_LogStatsUploadAddressExtLow,
                          stbSr_LogStatsUploadUserName, stbSr_LogStatsUploadPassword,
                          filename);
                        //upload the 'previous' file
                        StbSr_UploadSSL(stbSr_LogStatsUploadAddressLow + fileSuffix + prev_date_for_filename + stbSr_LogStatsUploadAddressExtLow,
                          stbSr_LogStatsUploadUserName, stbSr_LogStatsUploadPassword,
                          previous_filename);


                    }
                }
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "teamhtm file"); }
    }

    public string StbSr_GetCampaignSummary()
    {
        string ms = "";
        try
        {
            if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX12", null); //testing disk output for warps
            string filepath = mission.stb_FullPath + "CampaignSummary.txt";
            string campaignSummary = "";
            if (File.Exists(filepath))
            {
                DateTime timeCreated = File.GetLastWriteTimeUtc(filepath);
                if (timeCreated.AddMinutes(5) > DateTime.UtcNow) //Only add this in if the campaignsummary file was written in the past 5 minutes
                {
                    //Console.WriteLine("CampaignSummary: Including the CampaignSummary.txt file");
                    campaignSummary = File.ReadAllText(filepath);
                    ms = campaignSummary;
                }
                else
                {
                    //Console.WriteLine("CampaignSummary: The file was too old to include."); 
                }
            }
            else
            {
                //Console.WriteLine("CampaignSummary: The file did not exist."); 
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "Reading team CampaignSummary file"); }

        return ms;
    }

    public void StbSr_UploadSituationMapFilesLowFilter()
    {
        try
        {
            if (stbSr_LogStats)
            {
                if (stbSr_LogStatsCreateHtmlLow)
                {


                    //upload the 'radar' file
                    //This will transfer any file that fits the mask *radar.txt - a bit of a kludge
                    //List<string> list = new List<string>();
                    string[] filenames = Directory.GetFiles(mission.stb_FullPath, "*radar.txt");
                    string[] filenames2 = Directory.GetFiles(mission.stb_FullPath, "*players.txt");

                    List<string> list = new List<string>(filenames);
                    List<string> list2 = new List<string>(filenames2); //combining both lists together
                    list2 = list2.Concat(list).ToList(); //must add .ToList() bec it concats but returns an ienumerable rather than a list

                    //Console.WriteLine("FTP Radar files . . . ");
                    foreach (string file in list2)
                    {
                        //Console.WriteLine("FTP Radar files: " + file);
                        string shortname = Path.GetFileName(file);
                        StbSr_UploadSSL(mission.stb_LogStatsUploadFtpBaseDirectory + "radar/" + shortname,
                          stbSr_LogStatsUploadUserName, stbSr_LogStatsUploadPassword,
                          file);

                        //Console.WriteLine("DELETING " + file); //delete this line once we're sure it's working
                        File.Delete(file);  //experimental: once radar files are uploaded, delete them.  Prevents duplicate/problem uploads over time
                    }

                }
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "situation map file"); }
    }

    private void StbSr_Upload(string ftpServer, string userName, string password, string filename)
    {
        try
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                //StbSr_WriteLine("Stats: Uploading stats via ftp.");
                client.Credentials = new System.Net.NetworkCredential(userName, password);
                client.UploadFile(ftpServer, "STOR", filename);
            }
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "upload"); }
    }

    //SSL FTP
    public bool StbSr_UploadSSL(string ftpServer, string username, string password, string filename = null)
    {
        try
        {
            //StbSr_WriteLine("Stats: Uploading stats via sftp.");
            if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX14");
            if (String.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException("Source filename missing.");

            //if (String.IsNullOrWhiteSpace(destFilePath))
            //    destFilePath = Path.GetFileName(filname);

            Uri serverUri = new Uri(ftpServer);

            //// the serverUri should start with the ftp:// scheme.
            if (serverUri.Scheme != Uri.UriSchemeFtp)
                return false;

            // get the object used to communicate with the server.
            FtpWebRequest request = CreateFtpRequest(serverUri, WebRequestMethods.Ftp.UploadFile, username, password);

            if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX6", null); //testing disk output for warps
                                                                                                // read file into byte array
            StreamReader sourceStream = new StreamReader(filename);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            request.ContentLength = fileContents.Length;

            // send bytes to server
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            //StbSr_WriteLine("Response status: {0} - {1}", response.StatusCode, response.StatusDescription);

            return true;
        }
        catch (Exception ex) { StbSr_PrepareErrorMessage(ex, "uploadSSL"); return false; }
    }

    private FtpWebRequest CreateFtpRequest(Uri serverUri, string method, string username, string password)
    {
        FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUri);
        request.EnableSsl = false;
        request.UsePassive = true;
        request.UseBinary = false;
        request.KeepAlive = true;
        request.Credentials = new NetworkCredential(username, password);
        request.Method = method;
        return request;
    }

    /*
    private Uri GetUri(string remoteFilePath)
    {
        return new Uri(_baseUri, remoteFilePath);
    }
    */

    //c# ssl ftp 

    //Checks DeadPlayers & if name is in it, returns the replacement name that should be used instead (the "dead player name")
    //If name is NOT in it then or the DeadPlayer time has expired, remove the expired deadplayer from the Dictionary if needed & return the null string
    public String StbSr_IsInDeadPlayers(string name)
    {
        //StbSr_WriteLine("IsInDeadPlayers: {0}", name);

        //We need to ban BOTH the main player & the "bomber" version, and return WHICHEVER may be in the dead pilots list.
        string playerBomberFighter = " (bomber)";
        if (name.EndsWith(playerBomberFighter)) name.Remove(name.Length - 9, 9);
        string[] names = new string[] { name, name + playerBomberFighter };

        foreach (string n in names)
        {
            Tuple<string, DateTime> temp;
            if (stbSr_DeadPlayers.TryGetValue(n, out temp))
            {
                DateTime now = DateTime.Now;
                //StbSr_WriteLine("IsInDeadPlayers: {0} {1} {2} {3}", name, temp.Item1.ToString(), now, temp.Item2);
                if (now < temp.Item2) return temp.Item1; //if the player is in the dead players list & the timeout is not yet expired, then replace the player name with the dead player name
                else
                {
                    stbSr_DeadPlayers.Remove(name); //if the player isn the dead players list & the timeout is expired, just remove that person from the list
                    return null;
                }
            }

        }
        return null;
    }

    public void StbSr_EnqueueTask(Mission.StbStatTask task)
    {
        //Ok, the following line seemed like a good idea, but maybe not.  The trouble is that stats will continue to accrue if a player damaged a/c but then died. These are OK.  What we don't want is new placeenters or player deaths to register.  So rem-ing it out.
        //if (StbSr_IsPlayerTimedOutDueToDeath(task.player)) return; //don't enqueue/save any stats for a player who is timed out due to death                        
        //If the player is on the "deadlist" we send all stats to their "dead name" until their deadlist entry expires
        string replacement_name_if_on_deadlist = StbSr_IsInDeadPlayers(task.player);
        if (replacement_name_if_on_deadlist != null) task.player = replacement_name_if_on_deadlist;

        lock (stbSr_Locker) stbSr_Tasks.Enqueue(task);
        //StbSr_WriteLine ("Task enqueued");
        stbSr_Wh.Set();
    }

    void StbSr_Work()
    {
        while (true)
        {
            Mission.StbStatTask task = new Mission.StbStatTask(Mission.StbStatCommands.None, "", new int[] { 0 });
            lock (stbSr_Locker)
                if (stbSr_Tasks.Count > 0)
                {
                    task = stbSr_Tasks.Dequeue();
                    if (task.command == Mission.StbStatCommands.None) return;
                }
            if (task.command != Mission.StbStatCommands.None)
            {
                switch (task.command)
                {
                    case Mission.StbStatCommands.Damage:
                        StbSr_UpdateStatsForDamage(task.player, task.parameters, task.actor);
                        break;
                    case Mission.StbStatCommands.Dead:
                        StbSr_UpdateStatsForDead(task.player, task.parameters, task.actor);
                        break;
                    case Mission.StbStatCommands.PlayerKilled:
                        StbSr_UpdateStatsForKilledPlayer(task.player, task.parameters, task.actor);
                        break;
                    case Mission.StbStatCommands.Mission:
                        StbSr_UpdateStatsForMission(task.player, task.parameters, task.actor);
                        break;
                    case Mission.StbStatCommands.CutLimb:
                        StbSr_UpdateStatsForCutLimb(task.player, task.parameters, task.actor);
                        break;
                    case Mission.StbStatCommands.TaskCurrent:
                        StbSr_UpdateStatsForTaskCurrent(task.player, task.parameters, task.actor);
                        break;
                    case Mission.StbStatCommands.Save:
                        StbSr_SavePlayerStats(task.parameters);
                        break;
                    default:
                        break;
                }
            }
            else
                stbSr_Wh.WaitOne();
        }
    }

    public void StbSr_FinishWaitingTasks()
    {
        Mission.StbStatTask t = new Mission.StbStatTask(Mission.StbStatCommands.None, "", new int[] { 0 });
        StbSr_EnqueueTask(t);// Signal to exit.
        stbSr_Worker.Join();// Wait for the thread to finish.
        stbSr_Wh.Close();// Release resources.
    }

    public void StbSr_PrepareErrorMessage(Exception ex, string loc = "")
    {
        if (stbSr_LogErrors)
        {
            if (loc == "") loc = "StbSr_general_error";
            ThreadPool.QueueUserWorkItem(new WaitCallback(StbSr_LogError),
                //(object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
                (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.ToString() + "Origin: " + loc));
        }
    }

    public void StbSr_LogError(object data)
    {
        try
        {
            /*
            FileInfo fi = new FileInfo(stbSr_ErrorLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(stbSr_ErrorLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(stbSr_ErrorLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
            */
            if (TWCComms.Communicator.Instance.WARP_CHECK) StbSr_AlwaysWriteLine("SXX7", null); //testing disk output for warps
            string date = DateTime.UtcNow.ToString("u");

            Task.Run(() => File.AppendAllText(stbSr_ErrorLogPath, "\n" + date + " - " + (string)data));
        }
        //catch (Exception ex) { StbSr_WriteLine(ex.Message); };
        catch (Exception ex) { Console.WriteLine(ex.ToString()); };
    }
} //class Stb StatRecorder




//Various helpful calculations, formulas, etc.
public static class Calcs
{
    //Various public/static methods
    //http://stackoverflow.com/questions/6499334/best-way-to-change-dictionary-key    

    private static Random clc_random = new Random();

    public static bool changeKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey oldKey, TKey newKey)
    {
      TValue value;
      if (!dict.TryGetValue(oldKey, out value))
          return false;
    
      dict.Remove(oldKey);  // do not change order
      dict[newKey] = value;  // or dict.Add(newKey, value) depending on ur comfort
      return true;
    }

    //gets LAST occurence of any element of a specified string[] ; CASE INSENSITIVE
    public static int LastIndexOfAny(string test, string[] values)
    {
        int last = -1;
        test = test.ToLower();
        foreach (string item in values)
        {
            int i = test.IndexOf(item.ToLower());
            if (i >= 0)
            {
                if (last > 0)
                {
                    if (i > last)
                    {
                        last = i;
                    }
                }
                else
                {
                    last = i;
                }
            }
        }
        return last;
    }

    public static string escapeColon(string s) {
     return s.Replace("##", "##*").Replace(":", "##@");
    }
    
    public static string unescapeColon(string s) {
     return s.Replace("##@", ":").Replace("##*", "##");
    }
    
    public static string escapeSemicolon(string s) {
     return s.Replace("%%", "%%*").Replace(";", "%%@");
    }
    
    public static string unescapeSemicolon(string s) {
     return s.Replace("%%@", ";").Replace("%%*", "%%");
    }            
        
    public static double distance (double a, double b){
    
       return (double)Math.Sqrt(a*a+b*b);
    
    }
    
    public static double meters2miles (double a){
    
         return (a / 1609.344);
    
    }
    
    public static double meterspsec2milesphour (double a) {
         return (a * 2.23694);
    }
    
    public static double meters2feet (double a){
    
         return (a / 1609.344*5280);
    
    }


    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }

    public static double RadiansToDegrees(double radians)
    {
        return radians * (180.0 / Math.PI);
    }

    public static double CalculateGradientAngle(
                              Point3d startPoint,
                              Point3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = endPoint.x - startPoint.x;
        double diffY = endPoint.y - startPoint.y;

        //Calculates the Tan to get the radians (TAN(alpha) = opposite / adjacent)
        //Math.PI/2 - atan becase we need to change to bearing where North =0, East = 90 vs regular math coordinates where East=0 and North=90.
        double radAngle = Math.PI / 2 - Math.Atan2(diffY, diffX);

        //Converts the radians in degrees
        double degAngle = RadiansToDegrees(radAngle);

        if (degAngle < 0)
        {
            degAngle = degAngle + 360;
        }

        return degAngle;
    }

    public static int GetDegreesIn10Step(double degrees)
    {
        degrees = Math.Round((degrees / 10), MidpointRounding.AwayFromZero) * 10;

        if ((int)degrees == 360)
            degrees = 0.0;

        return (int)degrees;
    }

    public static double CalculatePointDistance(
                            Point3d startPoint,
                            Point3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);
    
        return distance(diffX,diffY);
    }
    public static double CalculatePointDistance(
                            Vector3d startPoint,
                            Vector3d endPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(endPoint.x - startPoint.x);
        double diffY = Math.Abs(endPoint.y - startPoint.y);
    
        return distance(diffX,diffY);
    } 
    public static double CalculatePointDistance(
                            Point3d startPoint)
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);
    
        return distance(diffX,diffY);
    }
    public static double CalculatePointDistance(
                            Vector3d startPoint)                            
    {
        //Calculate the length of the adjacent and opposite
        double diffX = Math.Abs(startPoint.x);
        double diffY = Math.Abs(startPoint.y);
    
        return distance(diffX,diffY);
    }

    public static double CalculateBearingDegree(Vector3d vector)
    {        
        Vector2d matVector = new Vector2d(vector.y, vector.x);
        // the value of direction is in rad so we need *180/Pi to get the value in degrees.  We subtract from pi/2 to convert to compass directions

        double bearing = (matVector.direction()) * 180.0 / Math.PI;
        return (bearing > 0.0 ? bearing : (360.0 + bearing));
    }


    public static double CalculateBearingDegree(Vector2d vector)
    {
        Vector2d newVector = new Vector2d(vector.y, vector.x);
        // the value of direction is in rad so we need *180/Pi to get the value in degrees.  We subtract from pi/2 to convert to compass directions
        double bearing = (newVector.direction()) * 180.0 / Math.PI;
        return (bearing > 0.0 ? bearing : (360.0 + bearing));  //we want bearing to be 0-360, generally
    }

    public static double CalculatePitchDegree(Vector3d vector)
    {
        double d = distance(vector.x, vector.y);  //size of vector in x/y plane
        Vector2d matVector = new Vector2d(d, vector.z);
        // the value of direction is in rad so we need *180/Pi to get the value in degrees.  

        double pitch = (matVector.direction()) * 180.0 / Math.PI;
        return (pitch < 180 ? pitch : (pitch - 360.0)); //we want pitch to be between -180 and 180, generally
    }



    public static int TimeSince2016_sec()                            
    {
          DateTime epochStart = new DateTime(2016, 1, 1); //we need to fit this into an int; Starting 2016/01/01 it should last longer than CloD does . . . 
          DateTime currentDate = DateTime.Now;
      
          long elapsedTicks = currentDate.Ticks - epochStart.Ticks;
          int elapsedSeconds= (int)(elapsedTicks/10000000);
          return elapsedSeconds; 
    }

    public static long TimeSince2016_ticks()
    {
        DateTime epochStart = new DateTime(2016, 1, 1); //we need to fit this into an int; Starting 2016/01/01 it should last longer than CloD does . . . 
        DateTime currentDate = DateTime.Now;

        long elapsedTicks = currentDate.Ticks - epochStart.Ticks;
        return elapsedTicks;
    }

    public static long TimeNow_ticks()
    {
        DateTime currentDate = DateTime.Now;
        return currentDate.Ticks;        
    }

    public static string SecondsToFormattedString (int sec) {
        try
        {
            var timespan = TimeSpan.FromSeconds(sec);
            if (sec < 10 * 60) return timespan.ToString(@"m\mss\s");
            if (sec < 60 * 60) return timespan.ToString(@"m\m");
            if (sec < 24 * 60 * 60) return timespan.ToString(@"hh\hmm\m");
            else return timespan.ToString(@"d\dhh\hmm\m");
        } catch (Exception ex)
        {
            System.Console.WriteLine("Calcs.SecondsToFormatted - Exception: " + ex.ToString());
            return sec.ToString();
        }
    }

    //returns index of largest array element which is equal to OR less than the value
    //assumes a sorted list of in values. 
    //If less than the 1st element or array empty, returns -1
    public static Int32 array_find_equalorless(int[] arr, Int32 value)
    {
        if (arr==null || arr.GetLength(0) == 0 || value < arr[0]) return -1;
        int index = Array.BinarySearch(arr, value);
        if (index < 0)
        {
            index = ~index - 1;
        }
        if (index < 0) return -1;
        return index;
    }

    //Splits a long string into a maxLineLength respecting word boundaries (IF possible)
    //http://stackoverflow.com/questions/22368434/best-way-to-split-string-into-lines-with-maximum-length-without-breaking-words
    public static IEnumerable<string> SplitToLines(string stringToSplit, int maxLineLength)
    {
        string[] words = stringToSplit.Split(' ');
        StringBuilder line = new StringBuilder();
        foreach (string word in words)
        {
            if (word.Length + line.Length <= maxLineLength)
            {
                line.Append(word + " ");
            }
            else
            {
                if (line.Length > 0)
                {
                    yield return line.ToString().Trim();
                    line.Clear();
                }
                string overflow = word;
                while (overflow.Length > maxLineLength)
                {
                    yield return overflow.Substring(0, maxLineLength);
                    overflow = overflow.Substring(maxLineLength);
                }
                line.Append(overflow + " ");
            }
        }
        yield return line.ToString().Trim();
    }

    //Salmo @ http://theairtacticalassaultgroup.com/forum/archive/index.php/t-4785.html
    public static string GetAircraftType(AiAircraft aircraft)
    { // returns the type of the specified aircraft
        string result = null;
        if (aircraft != null)
        {
            string type = aircraft.InternalTypeName(); // eg type = "bob:Aircraft.Bf-109E-3".  FYI this is a property of AiCart inherited by AiAircraft as a descendant class.  So we could do this with any type of AiActor or AiCart
            string[] part = type.Trim().Split('.');
            result = part[1]; // get the part after the "." in the type string
        }
        return result;
    }

    public static string randSTR(string[] strings)
    {
        //Random clc_random = new Random();
        return strings[clc_random.Next(strings.Length)];
    }

    public static void loadSmokeOrFire(maddox.game.IGamePlay GamePlay, Mission mission, double x, double y, double z, string type, double duration_s = 300, string path = "")
    {
        /* Samples: 
         * Static555 Smoke.Environment.Smoke1 nn 63748.22 187791.27 110.00 /height 16.24
 Static556 Smoke.Environment.Smoke1 nn 63718.50 187780.80 110.00 /height 16.24
 Static557 Smoke.Environment.Smoke2 nn 63688.12 187764.03 110.00 /height 16.24
 Static534 Smoke.Environment.BuildingFireSmall nn 63432.15 187668.28 110.00 /height 15.08
 Static542 Smoke.Environment.BuildingFireBig nn 63703.02 187760.81 110.00 /height 15.08
 Static580 Smoke.Environment.BigSitySmoke_0 nn 63561.45 187794.80 110.00 /height 17.01
 Static580 Smoke.Environment.BigSitySmoke_1 nn 63561.45 187794.80 110.00 /height 17.01

        Not sure if height is above sea level or above ground level.
 */

        mission.Timeout(2.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete)", new object[] { }); });
        //GamePlay.gpLogServer(null, "Setting up to delete stationary smokes in " + duration_s.ToString("0.0") + " seconds.", new object[] { });
        mission.Timeout(3.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete2)", new object[] { }); });
        mission.Timeout(4.0, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete3)", new object[] { }); });
        mission.Timeout(4.5, () => { GamePlay.gpLogServer(null, "Testing the timeout (delete4)", new object[] { }); });

        mission.Timeout(5.0, () => {
            GamePlay.gpLogServer(null, "Executing the timeout (delete5)", new object[] { });
            //Point2d P = new Point2d(x, y);
            //GamePlay.gpRemoveGroundStationarys(P, 10);
        });
        /*
        mission.Timeout(duration_s, () =>
        {
            //Console.WriteLine("Deleting stationary smokes . . . ");
            GamePlay.gpLogServer(null, "Deleting stationary smokes . . . ", new object[] { });
            Point2d P = new Point2d(x, y);
            GamePlay.gpRemoveGroundStationarys(P, 10);
            foreach (GroundStationary sta in GamePlay.gpGroundStationarys(x, y, z + 1))
            {
                if (sta == null) continue;
                Console.WriteLine("Deleting , , , " + sta.Name + " " + sta.Title);
                if (sta.Name.Contains(key) || sta.Title.Contains(key)) {
                    Console.WriteLine("Deleting stationary smoke " + sta.Name + " - end of life");
                    sta.Destroy();
                }
            }


        });

     */
        //AMission mission = GamePlay as AMission;
        ISectionFile f = GamePlay.gpCreateSectionFile();
        string sect = "Stationary";
        string key = "Static1";
        string value = "Smoke.Environment." + type + " nn " + x.ToString("0.00") + " " + y.ToString("0.00") + " " + (duration_s/60).ToString ("0.0") + " /height " + z.ToString("0.00");
        f.add(sect, key, value);

        /*
        sect = "Stationary";
        key = "Static2";
        value = "Smoke.Environment." + "Smoke1" + " nn " + x.ToString("0.00") + " " + (y  + 130).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static3";
        value = "Smoke.Environment." + "Smoke2" + " nn " + x.ToString("0.00") + " " + (y + 260).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static4";
        value = "Smoke.Environment." + "BuildingFireSmall" + " nn " + x.ToString("0.00") + " " + (y + 390).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static5";
        value = "Smoke.Environment." + "BuildingFireBig" + " nn " + x.ToString("0.00") + " " + (y + 420).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static6";
        value = "Smoke.Environment." + "BigSitySmoke_0" + " nn " + x.ToString("0.00") + " " + (y + 550).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static7";
        value = "Smoke.Environment." + "BigSitySmoke_1" + " nn " + x.ToString("0.00") + " " + (y + 680).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);

        sect = "Stationary";
        key = "Static8";
        value = "Smoke.Environment." + "BigSitySmoke_2" + " nn " + x.ToString("0.00") + " " + (y + 710).ToString("0.00") + " 110.00 /height " + z.ToString("0.00");
        f.add(sect, key, value);
        */



        //maybe this part dies silently some times, due to f.save or perhaps section file load?  PRobably needs try/catch
        //GamePlay.gpLogServer(null, "Writing Sectionfile to " + path + "smoke-ISectionFile.txt", new object[] { }); //testing
        //f.save(path + "smoke-ISectionFile.txt"); //testing
        if (TWCComms.Communicator.Instance.WARP_CHECK) Console.WriteLine("SXX9", null); //testing disk output for warps
        GamePlay.gpPostMissionLoad(f);


        //TODO: This part isn't working; it never finds any of the smokes again.
        //get rid of it after the specified period


        
    }

    public static void PrintValues(IEnumerable myList, int myWidth)
    {
        int i = myWidth;
        foreach (Object obj in myList)
        {
            if (i <= 0)
            {
                i = myWidth;
                Console.WriteLine();
            }
            i--;
            Console.Write("{0,8}", obj);
        }
        Console.WriteLine();
    }



}

//Circular array which operates as a limited size queue OR stack
//Based on https://www.codeproject.com/Articles/31652/A-Generic-Circular-Array
public class CircularArray<T>
{
    private readonly T[] _baseArray;
    private readonly T[] _facadeArray;
    private int _head;
    private bool _isFilled;

    public CircularArray(int length)
    {
        _baseArray = new T[length];
        _facadeArray = new T[length];
    }

    //Array in queue order (first queued = first of array)
    public T[] Array
    {
        get
        {
            int pos = _head;
            for (int i = 0; i < _baseArray.Length; i++)
            {
                Math.DivRem(pos, _baseArray.Length, out pos);
                _facadeArray[i] = _baseArray[pos];
                pos++;
            }
            return _facadeArray;
        }
    }

    //Array in stack order (last queued = first of array)
    public T[] ArrayStack
    {
        get
        {
            int pos = _head - 1; // + 2*_baseArray.Length;  //by adding 2*_baseArray.Length we can count downwards by _baseArray.Length with no worries about going below 0 for our index.  We have to go 2* bec _head might be zero meaning our starting point might be -1
            for (int i = 0; i < _baseArray.Length; i++)
            {
                Math.DivRem(pos, _baseArray.Length, out pos);
                //Console.WriteLine("ArrayStack: " + i.ToString());
                _facadeArray[i] = _baseArray[pos];
                pos--;
                pos = pos < 0 ? pos + _baseArray.Length : pos;
            }
            return _facadeArray;
        }
    }

    public T[] BaseArray
    {
        get { return _baseArray; }
    }

    public bool IsFilled
    {
        get { return _isFilled; }
    }

    public void Push(T value)
    {
        if (!_isFilled && _head == _baseArray.Length - 1)
            _isFilled = true;

        Math.DivRem(_head, _baseArray.Length, out _head);
        _baseArray[_head] = value;
        _head++;
    }

    //Gets end of queue (ie, the first value entered) if 0 or 2nd, 3rd, etc value entered if index 1, 2, 3 etc
    //10/2018 - this seems incorrect. This gets the last value pushed onto the array if 0, 2nd to last if 1, etc.
    public T Get(int indexBackFromHead)
    {
        int pos = _head - indexBackFromHead - 1;
        pos = pos < 0 ? pos + _baseArray.Length : pos;
        Math.DivRem(pos, _baseArray.Length, out pos);
        return _baseArray[pos];
    }

    //Gets top of the stack (ie, the last value entered) if 0 or 2nd to last, 3rd to last, etc if index 1, 2, 3 etc 
    ////10/2018 - this seems incorrect. This gets the tail of the array, ie the first value pushed onto the array (that still remains), ie the oldest value in the array, if 0, 2nd to last if 1, etc.
    public T GetStack(int indexForwardFromHead)
    {
        int pos = _head + indexForwardFromHead;
        pos = pos < 0 ? pos + _baseArray.Length : pos;
        Math.DivRem(pos, _baseArray.Length, out pos);
        return _baseArray[pos];
    }
}



/*
public class AircraftDead
    {
        public TimeSpan MissionTime { get; set; }

        public DateTime UtcTime { get; set; }

        public string DeadAircraftName { get; set; }

        public string DeadAircraftTypeName { get; set; }

        public int DeadAircraftType { get; set; }

        public int DeadAircraftArmy { get; set; }

        public string DamagerActorName { get; set; }

        public string DamagerGroundTypeName { get; set; }

        public int DamagerGroundType { get; set; }

        public string DamagerPersonName { get; set; }

        public int DamagerArmy { get; set; }

        public string DamageToolName { get; set; }

        public int DamageToolType { get; set; }

        public double Score { get; set; }

        public string Country { get; set; }
    }
*/

namespace Ini
{
    /// <summary>
    /// Create a New INI file to store or load data
    /// https://www.codeproject.com/Articles/1966/An-INI-file-handling-class-using-C
    /// </summary>
    public class IniFile
    {
        public string path;
        public Mission mission;
        public int iniErrorCount;

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <param name="INIPath"></param>
        public IniFile(string INIPath, Mission msn)
        {
            path = INIPath;
            mission = msn;
            iniErrorCount = 0;

        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <param name="Section"></param>
        /// Section name
        /// <param name="Key"></param>
        /// Key Name
        /// <param name="Value"></param>
        /// Value Name
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.path);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Key"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        //overloaded for string, int, double, bool.  Could do others like single, float, whatever.  String[] int[] double[] etc.
        public string IniReadValue(string Section, string Key, string def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            if (temp.Length > 0) return temp.ToString();
            {
                IniReadError(Section, Key);
                return def;
            }
        }        
        public int IniReadValue(string Section, string Key, int def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);            
            int a;
            if (temp.Length > 0 && int.TryParse(temp.ToString(), out a)) return a;
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public double IniReadValue(string Section, string Key, double def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);            
            double a;
            if (temp.Length > 0 && double.TryParse(temp.ToString(), out a)) return a;
            {
                IniReadError(Section, Key);
                return def;
            }
        }
        public bool IniReadValue(string Section, string Key, bool def)
        {
            StringBuilder temp = new StringBuilder(1024);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1024, this.path);
            if (temp.ToString().Trim() == "1") temp = new StringBuilder("True", 4); //allow 0 & 1 to be used, or True/true/False/false
            if (temp.ToString().Trim() == "0") temp = new StringBuilder("False", 5);
            bool a;
            if (temp.Length > 0 && bool.TryParse(temp.ToString(), out a)) return a;
            else
            { 
                IniReadError(Section, Key); 
                return def;
            }
        }
        private void IniReadError (String Section, String Key) {
            //broadcasting this a bit widely bec. .ini file error is quite serious
            iniErrorCount++;
            Console.WriteLine("-stats.cs: ERROR reading stats.ini file: Key {0} in Section {1} was not found. Using default value instead.", Key, Section);

            mission.stb_iniFileErrorMessages += "-stats.cs: ERROR (#" + iniErrorCount.ToString() + ") reading stats.ini file: Key " + Key + " in Section " + Section + " was not found. Using default value instead.\n";
            //The ideas below don't work - I think because it is just too early in program invocation.  Things are not initialized.
            //maddox.game.IGamePlay.gpLogServer(null, "Error reading .ini file: Key {0} in Section {1} was not found. Using default value instead.", null);
            //mission.Stb_Message(null, "Error reading .ini file: Key {0} in Section {1} was not found. Using default value instead.", new object [] { Key, Section });
            //this.mission.Stb_Message(null, "Error reading .ini file: Key " + Key + " in Section " + Section +" was not found. Using default value instead.", null);
            
        }
    }
}

//The below appears to be needed for the WinXP version for some reason. Hopefully it will not do any harm in other Win versions.
// you need this once (only), and it must be in this namespace
//http://stackoverflow.com/questions/1522605/using-extension-methods-in-net-2-0
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class
         | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}


/******************************************************************************
 * 
 * 
 * These are the definition of the int values found in "XXX_playerstats_full.txt" which are separated by comma for each user.

The first field contains a text string with the player name and info about the player's death (if they died).  So each player may have several entries in the file, one for each death over a period of time.

After that there are integer fields with various stats.

Note that as the stats package develops we may add more fields at the end with more stats.  However, we won't change or renumber any of the existing fields.


0	PlaceEnter Counts (Place Enters which are disabled by balance are not included)
1	NamedDamageTypes.FuelPumpFailure Damage Count
2	NamedDamageTypes.FuelTank0TinyLeak Damage Count
3	NamedDamageTypes.FuelTank1TinyLeak Damage Count
4	NamedDamageTypes.FuelTank2TinyLeak Damage Count
5	NamedDamageTypes.FuelTank3TinyLeak Damage Count
6	NamedDamageTypes.FuelTank4TinyLeak Damage Count
7	NamedDamageTypes.FuelTank5TinyLeak Damage Count
8	NamedDamageTypes.FuelTank6TinyLeak Damage Count
9	NamedDamageTypes.FuelTank7TinyLeak Damage Count
10	NamedDamageTypes.FuelTank0SmallLeak Damage Count
11	NamedDamageTypes.FuelTank1SmallLeak Damage Count
12	NamedDamageTypes.FuelTank2SmallLeak Damage Count
13	NamedDamageTypes.FuelTank3SmallLeak Damage Count
14	NamedDamageTypes.FuelTank4SmallLeak Damage Count
15	NamedDamageTypes.FuelTank5SmallLeak Damage Count
16	NamedDamageTypes.FuelTank6SmallLeak Damage Count
17	NamedDamageTypes.FuelTank7SmallLeak Damage Count
18	NamedDamageTypes.FuelTank0LargeLeak Damage Count
19	NamedDamageTypes.FuelTank1LargeLeak Damage Count
20	NamedDamageTypes.FuelTank2LargeLeak Damage Count
21	NamedDamageTypes.FuelTank3LargeLeak Damage Count
22	NamedDamageTypes.FuelTank4LargeLeak Damage Count
23	NamedDamageTypes.FuelTank5LargeLeak Damage Count
24	NamedDamageTypes.FuelTank6LargeLeak Damage Count
25	NamedDamageTypes.FuelTank7LargeLeak Damage Count
26	NamedDamageTypes.FuelTank0Fire Damage Count
27	NamedDamageTypes.FuelTank1Fire Damage Count
28	NamedDamageTypes.FuelTank2Fire Damage Count
29	NamedDamageTypes.FuelTank3Fire Damage Count
30	NamedDamageTypes.FuelTank4Fire Damage Count
31	NamedDamageTypes.FuelTank5Fire Damage Count
32	NamedDamageTypes.FuelTank6Fire Damage Count
33	NamedDamageTypes.FuelTank7Fire Damage Count
34	NamedDamageTypes.FuelTank0Exploded Damage Count
35	NamedDamageTypes.FuelTank1Exploded Damage Count
36	NamedDamageTypes.FuelTank2Exploded Damage Count
37	NamedDamageTypes.FuelTank3Exploded Damage Count
38	NamedDamageTypes.FuelTank4Exploded Damage Count
39	NamedDamageTypes.FuelTank5Exploded Damage Count
40	NamedDamageTypes.FuelTank6Exploded Damage Count
41	NamedDamageTypes.FuelTank7Exploded Damage Count
42	NamedDamageTypes.FuelTank0HosePerforated Damage Count
43	NamedDamageTypes.FuelTank1HosePerforated Damage Count
44	NamedDamageTypes.FuelTank2HosePerforated Damage Count
45	NamedDamageTypes.FuelTank3HosePerforated Damage Count
46	NamedDamageTypes.FuelTank4HosePerforated Damage Count
47	NamedDamageTypes.FuelTank5HosePerforated Damage Count
48	NamedDamageTypes.FuelTank6HosePerforated Damage Count
49	NamedDamageTypes.FuelTank7HosePerforated Damage Count
50	NamedDamageTypes.FuelTank0PumpFailure Damage Count
51	NamedDamageTypes.FuelTank1PumpFailure Damage Count
52	NamedDamageTypes.FuelTank2PumpFailure Damage Count
53	NamedDamageTypes.FuelTank3PumpFailure Damage Count
54	NamedDamageTypes.FuelTank4PumpFailure Damage Count
55	NamedDamageTypes.FuelTank5PumpFailure Damage Count
56	NamedDamageTypes.FuelTank6PumpFailure Damage Count
57	NamedDamageTypes.FuelTank7PumpFailure Damage Count
58	NamedDamageTypes.ElecGeneratorFailure Damage Count
59	NamedDamageTypes.ElecBatteryFailure Damage Count
60	NamedDamageTypes.ElecPrimaryFailure Damage Count
61	NamedDamageTypes.ElecSecondaryFailure Damage Count
62	NamedDamageTypes.ElecMasterCompassFailure Damage Count
63	NamedDamageTypes.ElecWeaponryFailure Damage Count
64	NamedDamageTypes.ElecPriNavigationFailure Damage Count
65	NamedDamageTypes.ElecSecNavigationFailure Damage Count
66	NamedDamageTypes.ElecTransceiverFailure Damage Count
67	NamedDamageTypes.ElecIlluminationFailure Damage Count
68	NamedDamageTypes.HydraulicsPumpFailure Damage Count
69	NamedDamageTypes.HydraulicsPrimaryHosePerforated Damage Count
70	NamedDamageTypes.HydraulicsPistonOpened Damage Count
71	NamedDamageTypes.HydraulicsEmergencyFailure Damage Count
72	NamedDamageTypes.HydraulicsTankPerforated Damage Count
73	NamedDamageTypes.PneumaticsCompressorFailure Damage Count
74	NamedDamageTypes.PneumaticsPrimaryContainerPerforated Damage Count
75	NamedDamageTypes.PneumaticsSecondaryContainerPerforated Damage Count
76	NamedDamageTypes.PneumaticsPrimaryHosePerforated Damage Count
77	NamedDamageTypes.PneumaticsSecondaryHosePerforated Damage Count
78	NamedDamageTypes.Machinegun00Failure Damage Count
79	NamedDamageTypes.Machinegun01Failure Damage Count
80	NamedDamageTypes.Machinegun02Failure Damage Count
81	NamedDamageTypes.Machinegun03Failure Damage Count
82	NamedDamageTypes.Machinegun04Failure Damage Count
83	NamedDamageTypes.Machinegun05Failure Damage Count
84	NamedDamageTypes.Machinegun06Failure Damage Count
85	NamedDamageTypes.Machinegun07Failure Damage Count
86	NamedDamageTypes.Machinegun08Failure Damage Count
87	NamedDamageTypes.Machinegun09Failure Damage Count
88	NamedDamageTypes.Machinegun10Failure Damage Count
89	NamedDamageTypes.Machinegun11Failure Damage Count
90	NamedDamageTypes.Machinegun00BeltBroken Damage Count
91	NamedDamageTypes.Machinegun01BeltBroken Damage Count
92	NamedDamageTypes.Machinegun02BeltBroken Damage Count
93	NamedDamageTypes.Machinegun03BeltBroken Damage Count
94	NamedDamageTypes.Machinegun04BeltBroken Damage Count
95	NamedDamageTypes.Machinegun05BeltBroken Damage Count
96	NamedDamageTypes.Machinegun06BeltBroken Damage Count
97	NamedDamageTypes.Machinegun07BeltBroken Damage Count
98	NamedDamageTypes.Machinegun08BeltBroken Damage Count
99	NamedDamageTypes.Machinegun09BeltBroken Damage Count
100	NamedDamageTypes.Machinegun10BeltBroken Damage Count
101	NamedDamageTypes.Machinegun11BeltBroken Damage Count
102	NamedDamageTypes.Machinegun00Jammed Damage Count
103	NamedDamageTypes.Machinegun01Jammed Damage Count
104	NamedDamageTypes.Machinegun02Jammed Damage Count
105	NamedDamageTypes.Machinegun03Jammed Damage Count
106	NamedDamageTypes.Machinegun04Jammed Damage Count
107	NamedDamageTypes.Machinegun05Jammed Damage Count
108	NamedDamageTypes.Machinegun06Jammed Damage Count
109	NamedDamageTypes.Machinegun07Jammed Damage Count
110	NamedDamageTypes.Machinegun08Jammed Damage Count
111	NamedDamageTypes.Machinegun09Jammed Damage Count
112	NamedDamageTypes.Machinegun10Jammed Damage Count
113	NamedDamageTypes.Machinegun11Jammed Damage Count
114	NamedDamageTypes.Machinegun00Charged Damage Count
115	NamedDamageTypes.Machinegun01Charged Damage Count
116	NamedDamageTypes.Machinegun02Charged Damage Count
117	NamedDamageTypes.Machinegun03Charged Damage Count
118	NamedDamageTypes.Machinegun04Charged Damage Count
119	NamedDamageTypes.Machinegun05Charged Damage Count
120	NamedDamageTypes.Machinegun06Charged Damage Count
121	NamedDamageTypes.Machinegun07Charged Damage Count
122	NamedDamageTypes.Machinegun08Charged Damage Count
123	NamedDamageTypes.Machinegun09Charged Damage Count
124	NamedDamageTypes.Machinegun10Charged Damage Count
125	NamedDamageTypes.Machinegun11Charged Damage Count
126	NamedDamageTypes.Machinegun00LineDamaged Damage Count
127	NamedDamageTypes.Machinegun01LineDamaged Damage Count
128	NamedDamageTypes.Machinegun02LineDamaged Damage Count
129	NamedDamageTypes.Machinegun03LineDamaged Damage Count
130	NamedDamageTypes.Machinegun04LineDamaged Damage Count
131	NamedDamageTypes.Machinegun05LineDamaged Damage Count
132	NamedDamageTypes.Machinegun06LineDamaged Damage Count
133	NamedDamageTypes.Machinegun07LineDamaged Damage Count
134	NamedDamageTypes.Machinegun08LineDamaged Damage Count
135	NamedDamageTypes.Machinegun09LineDamaged Damage Count
136	NamedDamageTypes.Machinegun10LineDamaged Damage Count
137	NamedDamageTypes.Machinegun11LineDamaged Damage Count
138	NamedDamageTypes.ControlsGenericKickdown Damage Count
139	NamedDamageTypes.ControlsAileronsDisabled Damage Count
140	NamedDamageTypes.ControlsElevatorDisabled Damage Count
141	NamedDamageTypes.ControlsRudderDisabled Damage Count
142	NamedDamageTypes.LandingFlapsPistonFailure1 Damage Count
143	NamedDamageTypes.LandingFlapsPistonFailure2 Damage Count
144	NamedDamageTypes.LandingFlapsKinematicFailure1 Damage Count
145	NamedDamageTypes.LandingFlapsKinematicFailure2 Damage Count
146	NamedDamageTypes.LandingFlapsDriveFailure Damage Count
147	NamedDamageTypes.LandingFlapsHosePerforated Damage Count
148	NamedDamageTypes.AirbrakeHosePerforated Damage Count
149	NamedDamageTypes.AirbrakeDriveFailure Damage Count
150	NamedDamageTypes.AirbrakePistonFailure Damage Count
151	NamedDamageTypes.WheelBrakesFailureL Damage Count
152	NamedDamageTypes.WheelBrakesFailureR Damage Count
153	NamedDamageTypes.WheelBrakesTotalFailure Damage Count
154	NamedDamageTypes.WheelBrakesHosePerforated Damage Count
155	NamedDamageTypes.UndercarriageShockFailureL Damage Count
156	NamedDamageTypes.UndercarriageShockFailureR Damage Count
157	NamedDamageTypes.UndercarriageShockFailureC Damage Count
158	NamedDamageTypes.UndercarriageUpLockFailureL Damage Count
159	NamedDamageTypes.UndercarriageUpLockFailureR Damage Count
160	NamedDamageTypes.UndercarriageUpLockFailureC Damage Count
161	NamedDamageTypes.UndercarriageDownLockFailureL Damage Count
162	NamedDamageTypes.UndercarriageDownLockFailureR Damage Count
163	NamedDamageTypes.UndercarriageDownLockFailureC Damage Count
164	NamedDamageTypes.UndercarriageKinematicFailureL Damage Count
165	NamedDamageTypes.UndercarriageKinematicFailureR Damage Count
166	NamedDamageTypes.UndercarriageKinematicFailureC Damage Count
167	NamedDamageTypes.UndercarriageHosePerforated Damage Count
168	NamedDamageTypes.UndercarriageDriveDamaged Damage Count
169	NamedDamageTypes.CockpitDamageFlag00 Damage Count
170	NamedDamageTypes.CockpitDamageFlag01 Damage Count
171	NamedDamageTypes.CockpitDamageFlag02 Damage Count
172	NamedDamageTypes.CockpitDamageFlag03 Damage Count
173	NamedDamageTypes.CockpitDamageFlag04 Damage Count
174	NamedDamageTypes.CockpitDamageFlag05 Damage Count
175	NamedDamageTypes.CockpitDamageFlag06 Damage Count
176	NamedDamageTypes.CockpitDamageFlag07 Damage Count
177	NamedDamageTypes.CockpitDamageFlag08 Damage Count
178	NamedDamageTypes.CockpitDamageFlag09 Damage Count
179	NamedDamageTypes.CockpitDamageFlag10 Damage Count
180	NamedDamageTypes.CockpitDamageFlag11 Damage Count
181	NamedDamageTypes.CockpitDamageFlag12 Damage Count
182	NamedDamageTypes.CockpitDamageFlag13 Damage Count
183	NamedDamageTypes.CockpitDamageFlag14 Damage Count
184	NamedDamageTypes.Eng0TotalFailure Damage Count
185	NamedDamageTypes.Eng0TotalSeizure Damage Count
186	NamedDamageTypes.Eng0IntakeBurnt Damage Count
187	NamedDamageTypes.Eng0CompressorFailure Damage Count
188	NamedDamageTypes.Eng0CompressorGovernorFailure Damage Count
189	NamedDamageTypes.Eng0CompressorSeizure Damage Count
190	NamedDamageTypes.Eng0IntercoolerBurnt Damage Count
191	NamedDamageTypes.Eng0CarbFailure Damage Count
192	NamedDamageTypes.Eng0CarbControlsFailure Damage Count
193	NamedDamageTypes.Eng0FuelLinePerforated Damage Count
194	NamedDamageTypes.Eng0FuelPumpFailure Damage Count
195	NamedDamageTypes.Eng0FuelSecondariesFire Damage Count
196	NamedDamageTypes.Eng0Magneto0Failure Damage Count
197	NamedDamageTypes.Eng0Magneto1Failure Damage Count
198	NamedDamageTypes.Eng0OilPumpFailure Damage Count
199	NamedDamageTypes.Eng0OilRadiatorPerforated Damage Count
200	NamedDamageTypes.Eng0OilLineBroken Damage Count
201	NamedDamageTypes.Eng0OilGasketLeak Damage Count
202	NamedDamageTypes.Eng0OilSecondariesFire Damage Count
203	NamedDamageTypes.Eng0OilSecondariesExtinguished Damage Count
204	NamedDamageTypes.Eng0OilTankPerforated Damage Count
205	NamedDamageTypes.Eng0WaterPumpFailure Damage Count
206	NamedDamageTypes.Eng0WaterRadiatorPerforated Damage Count
207	NamedDamageTypes.Eng0WaterLineBroken Damage Count
208	NamedDamageTypes.Eng0WaterTankPerforated Damage Count
209	NamedDamageTypes.Eng0WaterJacketBroken Damage Count
210	NamedDamageTypes.Eng0CylinderHeadFire Damage Count
211	NamedDamageTypes.Eng0CylinderHeadExtinguished Damage Count
212	NamedDamageTypes.Eng0ExhaustHeadFailure Damage Count
213	NamedDamageTypes.Eng0GovernorFailure Damage Count
214	NamedDamageTypes.Eng0GovernorSeizure Damage Count
215	NamedDamageTypes.Eng0ThrottleControlBroken Damage Count
216	NamedDamageTypes.Eng0PropControlBroken Damage Count
217	NamedDamageTypes.Eng0TretiaryControlBroken Damage Count
218	NamedDamageTypes.Eng0PropBlade0Broken Damage Count
219	NamedDamageTypes.Eng0PropBlade1Broken Damage Count
220	NamedDamageTypes.Eng0PropBlade2Broken Damage Count
221	NamedDamageTypes.Eng0PropBlade3Broken Damage Count
222	NamedDamageTypes.Eng0Plug00Failure Damage Count
223	NamedDamageTypes.Eng0Plug01Failure Damage Count
224	NamedDamageTypes.Eng0Plug02Failure Damage Count
225	NamedDamageTypes.Eng0Plug03Failure Damage Count
226	NamedDamageTypes.Eng0Plug04Failure Damage Count
227	NamedDamageTypes.Eng0Plug05Failure Damage Count
228	NamedDamageTypes.Eng0Plug06Failure Damage Count
229	NamedDamageTypes.Eng0Plug07Failure Damage Count
230	NamedDamageTypes.Eng0Plug08Failure Damage Count
231	NamedDamageTypes.Eng0Plug09Failure Damage Count
232	NamedDamageTypes.Eng0Plug10Failure Damage Count
233	NamedDamageTypes.Eng0Plug11Failure Damage Count
234	NamedDamageTypes.Eng0Plug12Failure Damage Count
235	NamedDamageTypes.Eng0Plug13Failure Damage Count
236	NamedDamageTypes.Eng0Plug14Failure Damage Count
237	NamedDamageTypes.Eng0Plug15Failure Damage Count
238	NamedDamageTypes.Eng0Plug16Failure Damage Count
239	NamedDamageTypes.Eng0Plug17Failure Damage Count
240	NamedDamageTypes.Eng1TotalFailure Damage Count
241	NamedDamageTypes.Eng1TotalSeizure Damage Count
242	NamedDamageTypes.Eng1IntakeBurnt Damage Count
243	NamedDamageTypes.Eng1CompressorFailure Damage Count
244	NamedDamageTypes.Eng1CompressorGovernorFailure Damage Count
245	NamedDamageTypes.Eng1CompressorSeizure Damage Count
246	NamedDamageTypes.Eng1IntercoolerBurnt Damage Count
247	NamedDamageTypes.Eng1CarbFailure Damage Count
248	NamedDamageTypes.Eng1CarbControlsFailure Damage Count
249	NamedDamageTypes.Eng1FuelLinePerforated Damage Count
250	NamedDamageTypes.Eng1FuelPumpFailure Damage Count
251	NamedDamageTypes.Eng1FuelSecondariesFire Damage Count
252	NamedDamageTypes.Eng1Magneto0Failure Damage Count
253	NamedDamageTypes.Eng1Magneto1Failure Damage Count
254	NamedDamageTypes.Eng1OilPumpFailure Damage Count
255	NamedDamageTypes.Eng1OilRadiatorPerforated Damage Count
256	NamedDamageTypes.Eng1OilLineBroken Damage Count
257	NamedDamageTypes.Eng1OilGasketLeak Damage Count
258	NamedDamageTypes.Eng1OilSecondariesFire Damage Count
259	NamedDamageTypes.Eng1OilSecondariesExtinguished Damage Count
260	NamedDamageTypes.Eng1OilTankPerforated Damage Count
261	NamedDamageTypes.Eng1WaterPumpFailure Damage Count
262	NamedDamageTypes.Eng1WaterRadiatorPerforated Damage Count
263	NamedDamageTypes.Eng1WaterLineBroken Damage Count
264	NamedDamageTypes.Eng1WaterTankPerforated Damage Count
265	NamedDamageTypes.Eng1WaterJacketBroken Damage Count
266	NamedDamageTypes.Eng1CylinderHeadFire Damage Count
267	NamedDamageTypes.Eng1CylinderHeadExtinguished Damage Count
268	NamedDamageTypes.Eng1ExhaustHeadFailure Damage Count
269	NamedDamageTypes.Eng1GovernorFailure Damage Count
270	NamedDamageTypes.Eng1GovernorSeizure Damage Count
271	NamedDamageTypes.Eng1ThrottleControlBroken Damage Count
272	NamedDamageTypes.Eng1PropControlBroken Damage Count
273	NamedDamageTypes.Eng1TretiaryControlBroken Damage Count
274	NamedDamageTypes.Eng1PropBlade0Broken Damage Count
275	NamedDamageTypes.Eng1PropBlade1Broken Damage Count
276	NamedDamageTypes.Eng1PropBlade2Broken Damage Count
277	NamedDamageTypes.Eng1PropBlade3Broken Damage Count
278	NamedDamageTypes.Eng1Plug00Failure Damage Count
279	NamedDamageTypes.Eng1Plug01Failure Damage Count
280	NamedDamageTypes.Eng1Plug02Failure Damage Count
281	NamedDamageTypes.Eng1Plug03Failure Damage Count
282	NamedDamageTypes.Eng1Plug04Failure Damage Count
283	NamedDamageTypes.Eng1Plug05Failure Damage Count
284	NamedDamageTypes.Eng1Plug06Failure Damage Count
285	NamedDamageTypes.Eng1Plug07Failure Damage Count
286	NamedDamageTypes.Eng1Plug08Failure Damage Count
287	NamedDamageTypes.Eng1Plug09Failure Damage Count
288	NamedDamageTypes.Eng1Plug10Failure Damage Count
289	NamedDamageTypes.Eng1Plug11Failure Damage Count
290	NamedDamageTypes.Eng1Plug12Failure Damage Count
291	NamedDamageTypes.Eng1Plug13Failure Damage Count
292	NamedDamageTypes.Eng1Plug14Failure Damage Count
293	NamedDamageTypes.Eng1Plug15Failure Damage Count
294	NamedDamageTypes.Eng1Plug16Failure Damage Count
295	NamedDamageTypes.Eng1Plug17Failure Damage Count
296	NamedDamageTypes.Eng2TotalFailure Damage Count
297	NamedDamageTypes.Eng2TotalSeizure Damage Count
298	NamedDamageTypes.Eng2IntakeBurnt Damage Count
299	NamedDamageTypes.Eng2CompressorFailure Damage Count
300	NamedDamageTypes.Eng2CompressorGovernorFailure Damage Count
304	NamedDamageTypes.Eng2CarbControlsFailure Damage Count
301	NamedDamageTypes.Eng2CompressorSeizure Damage Count
302	NamedDamageTypes.Eng2IntercoolerBurnt Damage Count
303	NamedDamageTypes.Eng2CarbFailure Damage Count

305	NamedDamageTypes.Eng2FuelLinePerforated Damage Count
306	NamedDamageTypes.Eng2FuelPumpFailure Damage Count
307	NamedDamageTypes.Eng2FuelSecondariesFire Damage Count
308	NamedDamageTypes.Eng2Magneto0Failure Damage Count
309	NamedDamageTypes.Eng2Magneto1Failure Damage Count
310	NamedDamageTypes.Eng2OilPumpFailure Damage Count
311	NamedDamageTypes.Eng2OilRadiatorPerforated Damage Count
312	NamedDamageTypes.Eng2OilLineBroken Damage Count
313	NamedDamageTypes.Eng2OilGasketLeak Damage Count
314	NamedDamageTypes.Eng2OilSecondariesFire Damage Count
315	NamedDamageTypes.Eng2OilSecondariesExtinguished Damage Count
316	NamedDamageTypes.Eng2OilTankPerforated Damage Count
317	NamedDamageTypes.Eng2WaterPumpFailure Damage Count
318	NamedDamageTypes.Eng2WaterRadiatorPerforated Damage Count
319	NamedDamageTypes.Eng2WaterLineBroken Damage Count
320	NamedDamageTypes.Eng2WaterTankPerforated Damage Count
321	NamedDamageTypes.Eng2WaterJacketBroken Damage Count
322	NamedDamageTypes.Eng2CylinderHeadFire Damage Count
323	NamedDamageTypes.Eng2CylinderHeadExtinguished Damage Count
324	NamedDamageTypes.Eng2ExhaustHeadFailure Damage Count
325	NamedDamageTypes.Eng2GovernorFailure Damage Count
326	NamedDamageTypes.Eng2GovernorSeizure Damage Count
327	NamedDamageTypes.Eng2ThrottleControlBroken Damage Count
328	NamedDamageTypes.Eng2PropControlBroken Damage Count
329	NamedDamageTypes.Eng2TretiaryControlBroken Damage Count
330	NamedDamageTypes.Eng2PropBlade0Broken Damage Count
331	NamedDamageTypes.Eng2PropBlade1Broken Damage Count
332	NamedDamageTypes.Eng2PropBlade2Broken Damage Count
333	NamedDamageTypes.Eng2PropBlade3Broken Damage Count
334	NamedDamageTypes.Eng2Plug00Failure Damage Count
335	NamedDamageTypes.Eng2Plug01Failure Damage Count
336	NamedDamageTypes.Eng2Plug02Failure Damage Count
337	NamedDamageTypes.Eng2Plug03Failure Damage Count
338	NamedDamageTypes.Eng2Plug04Failure Damage Count
339	NamedDamageTypes.Eng2Plug05Failure Damage Count
340	NamedDamageTypes.Eng2Plug06Failure Damage Count
341	NamedDamageTypes.Eng2Plug07Failure Damage Count
342	NamedDamageTypes.Eng2Plug08Failure Damage Count
343	NamedDamageTypes.Eng2Plug09Failure Damage Count
344	NamedDamageTypes.Eng2Plug10Failure Damage Count
345	NamedDamageTypes.Eng2Plug11Failure Damage Count
346	NamedDamageTypes.Eng2Plug12Failure Damage Count
347	NamedDamageTypes.Eng2Plug13Failure Damage Count
348	NamedDamageTypes.Eng2Plug14Failure Damage Count
349	NamedDamageTypes.Eng2Plug15Failure Damage Count
350	NamedDamageTypes.Eng2Plug16Failure Damage Count
351	NamedDamageTypes.Eng2Plug17Failure Damage Count
352	NamedDamageTypes.Eng3TotalFailure Damage Count
353	NamedDamageTypes.Eng3TotalSeizure Damage Count
354	NamedDamageTypes.Eng3IntakeBurnt Damage Count
355	NamedDamageTypes.Eng3CompressorFailure Damage Count
356	NamedDamageTypes.Eng3CompressorGovernorFailure Damage Count
357	NamedDamageTypes.Eng3CompressorSeizure Damage Count
358	NamedDamageTypes.Eng3IntercoolerBurnt Damage Count
359	NamedDamageTypes.Eng3CarbFailure Damage Count
360	NamedDamageTypes.Eng3CarbControlsFailure Damage Count
361	NamedDamageTypes.Eng3FuelLinePerforated Damage Count
362	NamedDamageTypes.Eng3FuelPumpFailure Damage Count
363	NamedDamageTypes.Eng3FuelSecondariesFire Damage Count
364	NamedDamageTypes.Eng3Magneto0Failure Damage Count
365	NamedDamageTypes.Eng3Magneto1Failure Damage Count
366	NamedDamageTypes.Eng3OilPumpFailure Damage Count
367	NamedDamageTypes.Eng3OilRadiatorPerforated Damage Count
368	NamedDamageTypes.Eng3OilLineBroken Damage Count
369	NamedDamageTypes.Eng3OilGasketLeak Damage Count
370	NamedDamageTypes.Eng3OilSecondariesFire Damage Count
371	NamedDamageTypes.Eng3OilSecondariesExtinguished Damage Count
372	NamedDamageTypes.Eng3OilTankPerforated Damage Count
373	NamedDamageTypes.Eng3WaterPumpFailure Damage Count
374	NamedDamageTypes.Eng3WaterRadiatorPerforated Damage Count
375	NamedDamageTypes.Eng3WaterLineBroken Damage Count
376	NamedDamageTypes.Eng3WaterTankPerforated Damage Count
377	NamedDamageTypes.Eng3WaterJacketBroken Damage Count
378	NamedDamageTypes.Eng3CylinderHeadFire Damage Count
379	NamedDamageTypes.Eng3CylinderHeadExtinguished Damage Count
380	NamedDamageTypes.Eng3ExhaustHeadFailure Damage Count
381	NamedDamageTypes.Eng3GovernorFailure Damage Count
382	NamedDamageTypes.Eng3GovernorSeizure Damage Count
383	NamedDamageTypes.Eng3ThrottleControlBroken Damage Count
384	NamedDamageTypes.Eng3PropControlBroken Damage Count
385	NamedDamageTypes.Eng3TretiaryControlBroken Damage Count
386	NamedDamageTypes.Eng3PropBlade0Broken Damage Count
387	NamedDamageTypes.Eng3PropBlade1Broken Damage Count
388	NamedDamageTypes.Eng3PropBlade2Broken Damage Count
389	NamedDamageTypes.Eng3PropBlade3Broken Damage Count
390	NamedDamageTypes.Eng3Plug00Failure Damage Count
391	NamedDamageTypes.Eng3Plug01Failure Damage Count
392	NamedDamageTypes.Eng3Plug02Failure Damage Count
393	NamedDamageTypes.Eng3Plug03Failure Damage Count
394	NamedDamageTypes.Eng3Plug04Failure Damage Count
395	NamedDamageTypes.Eng3Plug05Failure Damage Count
396	NamedDamageTypes.Eng3Plug06Failure Damage Count
397	NamedDamageTypes.Eng3Plug07Failure Damage Count
398	NamedDamageTypes.Eng3Plug08Failure Damage Count
399	NamedDamageTypes.Eng3Plug09Failure Damage Count
400	NamedDamageTypes.Eng3Plug10Failure Damage Count
401	NamedDamageTypes.Eng3Plug11Failure Damage Count
402	NamedDamageTypes.Eng3Plug12Failure Damage Count
403	NamedDamageTypes.Eng3Plug13Failure Damage Count
404	NamedDamageTypes.Eng3Plug14Failure Damage Count
405	NamedDamageTypes.Eng3Plug15Failure Damage Count
406	NamedDamageTypes.Eng3Plug16Failure Damage Count
407	NamedDamageTypes.Eng3Plug17Failure Damage Count
408	NamedDamageTypes.Eng4TotalFailure Damage Count
409	NamedDamageTypes.Eng4TotalSeizure Damage Count
410	NamedDamageTypes.Eng4IntakeBurnt Damage Count
411	NamedDamageTypes.Eng4CompressorFailure Damage Count
412	NamedDamageTypes.Eng4CompressorGovernorFailure Damage Count
413	NamedDamageTypes.Eng4CompressorSeizure Damage Count
414	NamedDamageTypes.Eng4IntercoolerBurnt Damage Count
415	NamedDamageTypes.Eng4CarbFailure Damage Count
416	NamedDamageTypes.Eng4CarbControlsFailure Damage Count
417	NamedDamageTypes.Eng4FuelLinePerforated Damage Count
418	NamedDamageTypes.Eng4FuelPumpFailure Damage Count
419	NamedDamageTypes.Eng4FuelSecondariesFire Damage Count
420	NamedDamageTypes.Eng4Magneto0Failure Damage Count
421	NamedDamageTypes.Eng4Magneto1Failure Damage Count
422	NamedDamageTypes.Eng4OilPumpFailure Damage Count
423	NamedDamageTypes.Eng4OilRadiatorPerforated Damage Count
424	NamedDamageTypes.Eng4OilLineBroken Damage Count
425	NamedDamageTypes.Eng4OilGasketLeak Damage Count
426	NamedDamageTypes.Eng4OilSecondariesFire Damage Count
427	NamedDamageTypes.Eng4OilSecondariesExtinguished Damage Count
428	NamedDamageTypes.Eng4OilTankPerforated Damage Count
429	NamedDamageTypes.Eng4WaterPumpFailure Damage Count
430	NamedDamageTypes.Eng4WaterRadiatorPerforated Damage Count
431	NamedDamageTypes.Eng4WaterLineBroken Damage Count
432	NamedDamageTypes.Eng4WaterTankPerforated Damage Count
433	NamedDamageTypes.Eng4WaterJacketBroken Damage Count
434	NamedDamageTypes.Eng4CylinderHeadFire Damage Count
435	NamedDamageTypes.Eng4CylinderHeadExtinguished Damage Count
436	NamedDamageTypes.Eng4ExhaustHeadFailure Damage Count
437	NamedDamageTypes.Eng4GovernorFailure Damage Count
438	NamedDamageTypes.Eng4GovernorSeizure Damage Count
439	NamedDamageTypes.Eng4ThrottleControlBroken Damage Count
440	NamedDamageTypes.Eng4PropControlBroken Damage Count
441	NamedDamageTypes.Eng4TretiaryControlBroken Damage Count
442	NamedDamageTypes.Eng4PropBlade0Broken Damage Count
443	NamedDamageTypes.Eng4PropBlade1Broken Damage Count
444	NamedDamageTypes.Eng4PropBlade2Broken Damage Count
445	NamedDamageTypes.Eng4PropBlade3Broken Damage Count
446	NamedDamageTypes.Eng4Plug00Failure Damage Count
447	NamedDamageTypes.Eng4Plug01Failure Damage Count
448	NamedDamageTypes.Eng4Plug02Failure Damage Count
449	NamedDamageTypes.Eng4Plug03Failure Damage Count
450	NamedDamageTypes.Eng4Plug04Failure Damage Count
451	NamedDamageTypes.Eng4Plug05Failure Damage Count
452	NamedDamageTypes.Eng4Plug06Failure Damage Count
453	NamedDamageTypes.Eng4Plug07Failure Damage Count
454	NamedDamageTypes.Eng4Plug08Failure Damage Count
455	NamedDamageTypes.Eng4Plug09Failure Damage Count
456	NamedDamageTypes.Eng4Plug10Failure Damage Count
457	NamedDamageTypes.Eng4Plug11Failure Damage Count
458	NamedDamageTypes.Eng4Plug12Failure Damage Count
459	NamedDamageTypes.Eng4Plug13Failure Damage Count
460	NamedDamageTypes.Eng4Plug14Failure Damage Count
461	NamedDamageTypes.Eng4Plug15Failure Damage Count
462	NamedDamageTypes.Eng4Plug16Failure Damage Count
463	NamedDamageTypes.Eng4Plug17Failure Damage Count
464	NamedDamageTypes.Eng5TotalFailure Damage Count
465	NamedDamageTypes.Eng5TotalSeizure Damage Count
466	NamedDamageTypes.Eng5IntakeBurnt Damage Count
467	NamedDamageTypes.Eng5CompressorFailure Damage Count
468	NamedDamageTypes.Eng5CompressorGovernorFailure Damage Count
469	NamedDamageTypes.Eng5CompressorSeizure Damage Count
470	NamedDamageTypes.Eng5IntercoolerBurnt Damage Count
471	NamedDamageTypes.Eng5CarbFailure Damage Count
472	NamedDamageTypes.Eng5CarbControlsFailure Damage Count
473	NamedDamageTypes.Eng5FuelLinePerforated Damage Count
474	NamedDamageTypes.Eng5FuelPumpFailure Damage Count
475	NamedDamageTypes.Eng5FuelSecondariesFire Damage Count
476	NamedDamageTypes.Eng5Magneto0Failure Damage Count
477	NamedDamageTypes.Eng5Magneto1Failure Damage Count
478	NamedDamageTypes.Eng5OilPumpFailure Damage Count
479	NamedDamageTypes.Eng5OilRadiatorPerforated Damage Count
480	NamedDamageTypes.Eng5OilLineBroken Damage Count
481	NamedDamageTypes.Eng5OilGasketLeak Damage Count
482	NamedDamageTypes.Eng5OilSecondariesFire Damage Count
483	NamedDamageTypes.Eng5OilSecondariesExtinguished Damage Count
484	NamedDamageTypes.Eng5OilTankPerforated Damage Count
485	NamedDamageTypes.Eng5WaterPumpFailure Damage Count
486	NamedDamageTypes.Eng5WaterRadiatorPerforated Damage Count
487	NamedDamageTypes.Eng5WaterLineBroken Damage Count
488	NamedDamageTypes.Eng5WaterTankPerforated Damage Count
489	NamedDamageTypes.Eng5WaterJacketBroken Damage Count
490	NamedDamageTypes.Eng5CylinderHeadFire Damage Count
491	NamedDamageTypes.Eng5CylinderHeadExtinguished Damage Count
492	NamedDamageTypes.Eng5ExhaustHeadFailure Damage Count
493	NamedDamageTypes.Eng5GovernorFailure Damage Count
494	NamedDamageTypes.Eng5GovernorSeizure Damage Count
495	NamedDamageTypes.Eng5ThrottleControlBroken Damage Count
496	NamedDamageTypes.Eng5PropControlBroken Damage Count
497	NamedDamageTypes.Eng5TretiaryControlBroken Damage Count
498	NamedDamageTypes.Eng5PropBlade0Broken Damage Count
499	NamedDamageTypes.Eng5PropBlade1Broken Damage Count
500	NamedDamageTypes.Eng5PropBlade2Broken Damage Count
501	NamedDamageTypes.Eng5PropBlade3Broken Damage Count
502	NamedDamageTypes.Eng5Plug00Failure Damage Count
503	NamedDamageTypes.Eng5Plug01Failure Damage Count
504	NamedDamageTypes.Eng5Plug02Failure Damage Count
505	NamedDamageTypes.Eng5Plug03Failure Damage Count
506	NamedDamageTypes.Eng5Plug04Failure Damage Count
507	NamedDamageTypes.Eng5Plug05Failure Damage Count
508	NamedDamageTypes.Eng5Plug06Failure Damage Count
509	NamedDamageTypes.Eng5Plug07Failure Damage Count
510	NamedDamageTypes.Eng5Plug08Failure Damage Count
511	NamedDamageTypes.Eng5Plug09Failure Damage Count
512	NamedDamageTypes.Eng5Plug10Failure Damage Count
513	NamedDamageTypes.Eng5Plug11Failure Damage Count
514	NamedDamageTypes.Eng5Plug12Failure Damage Count
515	NamedDamageTypes.Eng5Plug13Failure Damage Count
516	NamedDamageTypes.Eng5Plug14Failure Damage Count
517	NamedDamageTypes.Eng5Plug15Failure Damage Count
518	NamedDamageTypes.Eng5Plug16Failure Damage Count
519	NamedDamageTypes.Eng5Plug17Failure Damage Count
520	NamedDamageTypes.Eng6TotalFailure Damage Count
521	NamedDamageTypes.Eng6TotalSeizure Damage Count
522	NamedDamageTypes.Eng6IntakeBurnt Damage Count
523	NamedDamageTypes.Eng6CompressorFailure Damage Count
524	NamedDamageTypes.Eng6CompressorGovernorFailure Damage Count
525	NamedDamageTypes.Eng6CompressorSeizure Damage Count
526	NamedDamageTypes.Eng6IntercoolerBurnt Damage Count
527	NamedDamageTypes.Eng6CarbFailure Damage Count
528	NamedDamageTypes.Eng6CarbControlsFailure Damage Count
529	NamedDamageTypes.Eng6FuelLinePerforated Damage Count
530	NamedDamageTypes.Eng6FuelPumpFailure Damage Count
531	NamedDamageTypes.Eng6FuelSecondariesFire Damage Count
532	NamedDamageTypes.Eng6Magneto0Failure Damage Count
533	NamedDamageTypes.Eng6Magneto1Failure Damage Count
534	NamedDamageTypes.Eng6OilPumpFailure Damage Count
535	NamedDamageTypes.Eng6OilRadiatorPerforated Damage Count
536	NamedDamageTypes.Eng6OilLineBroken Damage Count
537	NamedDamageTypes.Eng6OilGasketLeak Damage Count
538	NamedDamageTypes.Eng6OilSecondariesFire Damage Count
539	NamedDamageTypes.Eng6OilSecondariesExtinguished Damage Count
540	NamedDamageTypes.Eng6OilTankPerforated Damage Count
541	NamedDamageTypes.Eng6WaterPumpFailure Damage Count
542	NamedDamageTypes.Eng6WaterRadiatorPerforated Damage Count
543	NamedDamageTypes.Eng6WaterLineBroken Damage Count
544	NamedDamageTypes.Eng6WaterTankPerforated Damage Count
545	NamedDamageTypes.Eng6WaterJacketBroken Damage Count
546	NamedDamageTypes.Eng6CylinderHeadFire Damage Count
547	NamedDamageTypes.Eng6CylinderHeadExtinguished Damage Count
548	NamedDamageTypes.Eng6ExhaustHeadFailure Damage Count
549	NamedDamageTypes.Eng6GovernorFailure Damage Count
550	NamedDamageTypes.Eng6GovernorSeizure Damage Count
551	NamedDamageTypes.Eng6ThrottleControlBroken Damage Count
552	NamedDamageTypes.Eng6PropControlBroken Damage Count
553	NamedDamageTypes.Eng6TretiaryControlBroken Damage Count
554	NamedDamageTypes.Eng6PropBlade0Broken Damage Count
555	NamedDamageTypes.Eng6PropBlade1Broken Damage Count
556	NamedDamageTypes.Eng6PropBlade2Broken Damage Count
557	NamedDamageTypes.Eng6PropBlade3Broken Damage Count
558	NamedDamageTypes.Eng6Plug00Failure Damage Count
559	NamedDamageTypes.Eng6Plug01Failure Damage Count
560	NamedDamageTypes.Eng6Plug02Failure Damage Count
561	NamedDamageTypes.Eng6Plug03Failure Damage Count
562	NamedDamageTypes.Eng6Plug04Failure Damage Count
563	NamedDamageTypes.Eng6Plug05Failure Damage Count
564	NamedDamageTypes.Eng6Plug06Failure Damage Count
565	NamedDamageTypes.Eng6Plug07Failure Damage Count
566	NamedDamageTypes.Eng6Plug08Failure Damage Count
567	NamedDamageTypes.Eng6Plug09Failure Damage Count
568	NamedDamageTypes.Eng6Plug10Failure Damage Count
569	NamedDamageTypes.Eng6Plug11Failure Damage Count
570	NamedDamageTypes.Eng6Plug12Failure Damage Count
571	NamedDamageTypes.Eng6Plug13Failure Damage Count
572	NamedDamageTypes.Eng6Plug14Failure Damage Count
573	NamedDamageTypes.Eng6Plug15Failure Damage Count
574	NamedDamageTypes.Eng6Plug16Failure Damage Count
575	NamedDamageTypes.Eng6Plug17Failure Damage Count
576	NamedDamageTypes.Eng7TotalFailure Damage Count
577	NamedDamageTypes.Eng7TotalSeizure Damage Count
578	NamedDamageTypes.Eng7IntakeBurnt Damage Count
579	NamedDamageTypes.Eng7CompressorFailure Damage Count
580	NamedDamageTypes.Eng7CompressorGovernorFailure Damage Count
581	NamedDamageTypes.Eng7CompressorSeizure Damage Count
582	NamedDamageTypes.Eng7IntercoolerBurnt Damage Count
583	NamedDamageTypes.Eng7CarbFailure Damage Count
584	NamedDamageTypes.Eng7CarbControlsFailure Damage Count
585	NamedDamageTypes.Eng7FuelLinePerforated Damage Count
586	NamedDamageTypes.Eng7FuelPumpFailure Damage Count
587	NamedDamageTypes.Eng7FuelSecondariesFire Damage Count
588	NamedDamageTypes.Eng7Magneto0Failure Damage Count
589	NamedDamageTypes.Eng7Magneto1Failure Damage Count
590	NamedDamageTypes.Eng7OilPumpFailure Damage Count
591	NamedDamageTypes.Eng7OilRadiatorPerforated Damage Count
592	NamedDamageTypes.Eng7OilLineBroken Damage Count
593	NamedDamageTypes.Eng7OilGasketLeak Damage Count
594	NamedDamageTypes.Eng7OilSecondariesFire Damage Count
595	NamedDamageTypes.Eng7OilSecondariesExtinguished Damage Count
596	NamedDamageTypes.Eng7OilTankPerforated Damage Count
597	NamedDamageTypes.Eng7WaterPumpFailure Damage Count
598	NamedDamageTypes.Eng7WaterRadiatorPerforated Damage Count
599	NamedDamageTypes.Eng7WaterLineBroken Damage Count
600	NamedDamageTypes.Eng7WaterTankPerforated Damage Count
601	NamedDamageTypes.Eng7WaterJacketBroken Damage Count
602	NamedDamageTypes.Eng7CylinderHeadFire Damage Count
603	NamedDamageTypes.Eng7CylinderHeadExtinguished Damage Count
604	NamedDamageTypes.Eng7ExhaustHeadFailure Damage Count
605	NamedDamageTypes.Eng7GovernorFailure Damage Count
606	NamedDamageTypes.Eng7GovernorSeizure Damage Count
607	NamedDamageTypes.Eng7ThrottleControlBroken Damage Count
608	NamedDamageTypes.Eng7PropControlBroken Damage Count
609	NamedDamageTypes.Eng7TretiaryControlBroken Damage Count
610	NamedDamageTypes.Eng7PropBlade0Broken Damage Count
611	NamedDamageTypes.Eng7PropBlade1Broken Damage Count
612	NamedDamageTypes.Eng7PropBlade2Broken Damage Count
613	NamedDamageTypes.Eng7PropBlade3Broken Damage Count
614	NamedDamageTypes.Eng7Plug00Failure Damage Count
615	NamedDamageTypes.Eng7Plug01Failure Damage Count
616	NamedDamageTypes.Eng7Plug02Failure Damage Count
617	NamedDamageTypes.Eng7Plug03Failure Damage Count
618	NamedDamageTypes.Eng7Plug04Failure Damage Count
619	NamedDamageTypes.Eng7Plug05Failure Damage Count
620	NamedDamageTypes.Eng7Plug06Failure Damage Count
621	NamedDamageTypes.Eng7Plug07Failure Damage Count
622	NamedDamageTypes.Eng7Plug08Failure Damage Count
623	NamedDamageTypes.Eng7Plug09Failure Damage Count
624	NamedDamageTypes.Eng7Plug10Failure Damage Count
625	NamedDamageTypes.Eng7Plug11Failure Damage Count
626	NamedDamageTypes.Eng7Plug12Failure Damage Count
627	NamedDamageTypes.Eng7Plug13Failure Damage Count
628	NamedDamageTypes.Eng7Plug14Failure Damage Count
629	NamedDamageTypes.Eng7Plug15Failure Damage Count
630	NamedDamageTypes.Eng7Plug16Failure Damage Count
631	NamedDamageTypes.Eng7Plug17Failure Damage Count
632	NamedDamageTypes.ChunkSmallDamage Damage Count
633	NamedDamageTypes.ChunkLargeDamage Damage Count
634	NamedDamageTypes.PartSmallDamage Damage Count
635	NamedDamageTypes.PartLargeDamage Damage Count
636	NamedDamageTypes.WeaponSmallDamage Damage Count
637	NamedDamageTypes.WeaponLargeDamage Damage Count
638	NamedDamageTypes.EngineSmallDamage Damage Count
639	NamedDamageTypes.EngineLargeDamage Damage Count
640	NamedDamageTypes.LifeKeeperPartSmallDamage Damage Count
641	NamedDamageTypes.LifeKeeperPartLargeDamage Damage Count
642	Air Tasks Completed Count
643	Air Tasks Completed Count
644	Air Tasks Completed Count
645	CurrentTaskNum
646	CurrentTaskcompletedBool
647	Aircraft Kill Participation Count
648	Ground Objects(AAGuns,Artilleries and Tanks only) Kill Participation Count
649	Naval(All kinds of Ships, amphibians not included) Kill Participation Count
650	LimbNames.CF Cut Count
651	LimbNames.WingL0 Cut Count
652	LimbNames.WingL1 Cut Count
653	LimbNames.WingL2 Cut Count
654	LimbNames.WingL3 Cut Count
655	LimbNames.WingL4 Cut Count
656	LimbNames.WingL5 Cut Count
657	LimbNames.WingL6 Cut Count
658	LimbNames.WingL7 Cut Count
659	LimbNames.WingR0 Cut Count
660	LimbNames.WingR1 Cut Count
661	LimbNames.WingR2 Cut Count
662	LimbNames.WingR3 Cut Count
663	LimbNames.WingR4 Cut Count
664	LimbNames.WingR5 Cut Count
665	LimbNames.WingR6 Cut Count
666	LimbNames.WingR7 Cut Count
667	LimbNames.AileronL0 Cut Count
668	LimbNames.AileronL1 Cut Count
669	LimbNames.AileronR0 Cut Count
670	LimbNames.AileronR1 Cut Count
671	LimbNames.LandingFlapL0 Cut Count
672	LimbNames.LandingFlapL1 Cut Count
673	LimbNames.LandingFlapR0 Cut Count
674	LimbNames.LandingFlapR1 Cut Count
675	LimbNames.StabilizerL0 Cut Count
676	LimbNames.StabilizerL1 Cut Count
677	LimbNames.StabilizerR0 Cut Count
678	LimbNames.StabilizerR1 Cut Count
679	LimbNames.ElevatorL0 Cut Count
680	LimbNames.ElevatorL1 Cut Count
681	LimbNames.ElevatorR0 Cut Count
682	LimbNames.ElevatorR1 Cut Count
683	LimbNames.Keel0 Cut Count
684	LimbNames.Keel1 Cut Count
685	LimbNames.Keel2 Cut Count
686	LimbNames.Keel3 Cut Count
687	LimbNames.Rudder0 Cut Count
688	LimbNames.Rudder1 Cut Count
689	LimbNames.Rudder2 Cut Count
690	LimbNames.Rudder3 Cut Count
691	LimbNames.Hatch0 Cut Count
692	LimbNames.Hatch1 Cut Count
693	LimbNames.Hatch2 Cut Count
694	LimbNames.Hatch3 Cut Count
695	LimbNames.Hatch4 Cut Count
696	LimbNames.Hatch5 Cut Count
697	LimbNames.Slat0 Cut Count
698	LimbNames.Slat1 Cut Count
699	LimbNames.Slat2 Cut Count
700	LimbNames.Slat3 Cut Count
701	LimbNames.AirBrakeL0 Cut Count
702	LimbNames.AirBrakeL1 Cut Count
703	LimbNames.AirBrakeR0 Cut Count
704	LimbNames.AirBrakeR1 Cut Count
705	LimbNames.Separator Cut Count
706	LimbNames.Sponger00 Cut Count
707	LimbNames.Sponger01 Cut Count
708	LimbNames.Sponger02 Cut Count
709	LimbNames.Sponger03 Cut Count
710	LimbNames.Sponger04 Cut Count
711	LimbNames.Sponger05 Cut Count
712	LimbNames.Sponger06 Cut Count
713	LimbNames.Sponger07 Cut Count
714	LimbNames.Sponger08 Cut Count
715	LimbNames.Sponger09 Cut Count
716	LimbNames.Sponger10 Cut Count
717	LimbNames.Sponger11 Cut Count
718	LimbNames.Sponger12 Cut Count
719	LimbNames.Sponger13 Cut Count
720	LimbNames.Sponger14 Cut Count
721	LimbNames.Sponger15 Cut Count
722	LimbNames.Sponger16 Cut Count
723	LimbNames.Sponger17 Cut Count
724	LimbNames.Sponger18 Cut Count
725	LimbNames.Sponger19 Cut Count
726	LimbNames.Sponger20 Cut Count
727	LimbNames.Sponger21 Cut Count
728	LimbNames.Sponger22 Cut Count
729	LimbNames.Sponger23 Cut Count
730	LimbNames.Engine0 Cut Count
731	LimbNames.Engine1 Cut Count
732	LimbNames.Engine2 Cut Count
733	LimbNames.Engine3 Cut Count
734	LimbNames.Engine4 Cut Count
735	LimbNames.Engine5 Cut Count
736	LimbNames.Engine6 Cut Count
737	LimbNames.Engine7 Cut Count
738	LimbNames.Nose0 Cut Count
739	LimbNames.Nose1 Cut Count
740	LimbNames.Nose2 Cut Count
741	LimbNames.Nose3 Cut Count
742	LimbNames.UC0 Cut Count
743	LimbNames.UC1 Cut Count
744	LimbNames.UC2 Cut Count
745	LimbNames.UC3 Cut Count
746	LimbNames.UC4 Cut Count
747	LimbNames.UC5 Cut Count
748	LimbNames.Wheel0 Cut Count
749	LimbNames.Wheel1 Cut Count
750	LimbNames.Wheel2 Cut Count
751	LimbNames.Wheel3 Cut Count
752	LimbNames.Wheel4 Cut Count
753	LimbNames.Wheel5 Cut Count
754	LimbNames.Tail0 Cut Count
755	LimbNames.Tail1 Cut Count
756	LimbNames.Tail2 Cut Count
757	LimbNames.Tail3 Cut Count
758	LimbNames.Tail4 Cut Count
759	LimbNames.Tail5 Cut Count
760	LimbNames.Tail6 Cut Count
761	LimbNames.Tail7 Cut Count
762	LimbNames.BayDoor0 Cut Count
763	LimbNames.BayDoor1 Cut Count
764	LimbNames.BayDoor2 Cut Count
765	LimbNames.BayDoor3 Cut Count
766	LimbNames.BayDoor4 Cut Count
767	LimbNames.BayDoor5 Cut Count
768	LimbNames.BayDoor6 Cut Count
769	LimbNames.BayDoor7 Cut Count
770 Takeoff Count
771 Safe Landing Count
772 Crash Landing Count
773 Health Damaged
774 Parachute Failed
775 Parachute Landing
776 Player Connected
777 Player Disconnected
778 Death Count
779 Continuous Missions Count - a continuous mission can consist of several connected sorties, if the player lands & then takes off again from the same airport each time
780 Player moved position
781 Player a/c damage (number of times)
782 Player limbs cut (number of times)
783 Joined Red army (Army 1)
784 Joined Blue army (Army 2)
785 Joined Army 3
786 Joined Army 4
787 Enter place (note dupl w/ #0)
788 Leave place
789 Self Kills
790 Times self damaged
791 Times ended a sortie with only self damage (ie, no enemy damage, only blew rads or crashed on landing etc)
792 Flight time (seconds) - counting from onTookOff to onPlaceLeave
793	Ground Tasks (ANYTHING BESIDES: AAGuns,Artilleries and Tanks only) Completed Count
794	Ground Objects(All EXCEPT AAGuns, Artilleries and Tanks) Kill Participation Count
795 Time of last player death (in seconds since Jan 1, 2016)		
796 Player Ace Level (see about line 64 of -stats.cs for the list of Ace Award Names & required kill values to accompany this value)
797 Player Rank (see about line 67 of -stats.cs for the list of ranks & requirements associated with each rank)
ALL FOUR TYPES COMBINED/TOTALLED (Aerial, AA, Naval, Other Surface)
798 Total kill percentage (adds up percentage credit towards each completed kill for a grand total)
799 Number of Total Victories (>=75% of kill)
800 Number of Shared Victories (40-75% of kill)
801 Number of Assists (1-40% of kill)
AERIAL ONLY
802 Total kill percentage (adds up percentage credit towards each completed kill for a grand total)
803 Number of Full Victories (>=75% of kill)
804 Number of Shared Victories (40-75% of kill)
805 Number of Assists (1-40% of kill)
AA/Artillery/Tank ONLY
806 Total kill percentage (adds up percentage credit towards each completed kill for a grand total)
807 Number of Full Victories (>=75% of kill)
808 Number of Shared Victories (40-75% of kill)
809 Number of Assists (1-40% of kill)
NAVAL/SHIP ONLY
810 Total kill percentage (adds up percentage credit towards each completed kill for a grand total)
811 Number of Full Victories (>=75% of kill)
812 Number of Shared Victories (40-75% of kill)
813 Number of Assists (1-40% of kill)
OTHER GROUND ONLY
814 Total kill percentage (adds up percentage credit towards each completed kill for a grand total)
815 Number of Full Victories (>=75% of kill)
816 Number of Shared Victories (40-75% of kill)
817 Number of Assists (1-40% of kill)
RAW DAMAGE POINTS 
818 Raw damage points (All types combined) We save 1000X the value CloD reports.  Note that these values are large enough that they just might roll over maxInt. When displayed they should be cast to uint just to give us 2X the headroom.  If they exceed maxInt they will be displayed as negative ints in the playerstats_full.txt file.
819 Raw damage points (Aerial)
820 Raw damage points (AA/Artillery/Tank)
821 Raw damage points (Naval)
822 Raw damage points (Other Ground)
PREVIOUS LIVES
823 Highest Rank reached in previous lives
824 Total kill participation (any type) accumulated from previous lives
825 Total kill percentage accumulated from previous lives
826 Number of Full Victories (>=75% of kill) accumulated from previous lives
827 Number of Shared Victories (40-75% of kill) accumulated from previous lives
828 Number of Assists (1-40% of kill) accumulated from previous lives
829 Time of last player access (in seconds since Jan 1, 2016) - can be used to prune out old accounts from time to time
830 Raw damage points FROM BOMBS (All types combined) We save 1000X the value CloD reports.  Note that these values are large enough that they just might roll over maxInt. When displayed they should be cast to uint just to give us 2X the headroom.  If they exceed maxInt they will be displayed as negative ints in the playerstats_full.txt file.
831 Raw damage points (Aerial, from Bombing)
832 Raw damage points (AA/Artillery/Tank, from Bombing)
833 Raw damage points (Naval, from Bombing)
834 Raw damage points (Other Ground, from Bombing)

835 Kill Points as recorded by CloD NetStats
836 Kill Points for friendly kills as recorded by CloD NetStats
837 Bullets fired
838 Bullets that hit (something? targets? actors? statics?)
839 Bullets that hit an aircraft
840 Bombs deployed
841 bombs hit
842 Bomb kg on target
843 Planes written off (ie, destroyed while player was flying them) per CloD.  This rarely seems to register anything; pretty useless.
844 # of sorties (ie, a flight that proceeded at least as far as take-off)
845 Planes written off (TWC custom calc - parachuting, crash landing anywhere but friendly a/p, injury > 0.5, etc etc all mean plane written off)
846 Planes written off, which are due solely to self-damage (TWC custom calc same as above, but with the additional criterion that the written-off a/c has no enemy damage at all)  TODO
847 Penalty Points - bombing civilian areas etc can result in negative points assess, each point counts against your victory point total for rank & ace purposes.  Always negative.
**************************************************************************************/

