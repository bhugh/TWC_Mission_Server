using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using maddox.GP;
using maddox.game;
using maddox.game.world;
using part;

public class statsMission : AMission
{
    #region Customizable Variables
    //Log Related
    public bool stb_LogErrors = true;//creates a local file, errors in txt format, useful for IO or upload errors
    public bool stb_LogStats = true;//creates a local file, full stats in txt format
    public bool stb_LogStatsCreateHtmlLow = true;//creates a local file, summerized stats in html format
    public bool stb_LogStatsCreateHtmlMed = false;//not available yet
    public bool stb_LogStatsUploadHtmlLow = false;//set true to upload
    //Upload sorttable.js and Style.css to the same location on your site manually, created htm depends on them to be viewed properly
    public bool stb_LogStatsUploadHtmlMed = false;//not available yet
    public string stb_LogStatsUploadAddressLow = "ftp://ftp.yoursite.com/www/stats.htm";
    public string stb_LogStatsUploadAddressMed = "";//not available yet
    public string stb_LogStatsUploadUserName = "yourftpusername";
    public string stb_LogStatsUploadPassword = "yourftppassword";
    public double stb_LogStatsDelay = 60.0;//60.0 default
    //Mission Related
    public bool stb_DestroyOnPlaceLeave = true;
    public bool stb_SpawnAntiAir = true;//base AAguns and defense ships
    public double stb_SpawnAntiAirDelay = 444.0;//444.0 default, some AA guns stop firing(probably run out of ammo) in less than 250 secs
    public bool stb_SpawnFrontline1 = true;//continuous ships @ 1.path (left)
    public bool stb_SpawnFrontline2 = true;//continuous ships @ 2.path (left-center)
    public bool stb_SpawnFrontline3 = true;//continuous ships @ 3.path (right-center)
    public bool stb_SpawnFrontline4 = true;//continuous armors @ 4.path (right)
    public bool stb_SpawnBombers = true;//loads 2 bomber missions, turnbased
    //Balance Related
    public bool stb_FighterBalanceActive = true;//false to disable balance code for fighters
    public bool stb_BalanceFightersByRatio = true;//true for balance by ratio, false for balance by fighter count difference
    public double stb_DesiredRatio = 2.0;//max delta fighter ratio for balance, default 2.0
    public int stb_DesiredDelta = 5;//max delta fighter count for balance, default 5
    public int stb_MinCountForBalance = 10;//min fighter count for balance (for both ratio and delta methods), default 10
    public string stb_BalanceMsgRus = "Извините, Есть слишком много истребителей прямо сейчас." +
                                       " Выберите бомбардировщиков или Изменение вашей армии или Попробуйте еще ​​раз позже.";
    public string stb_BalanceMsgEng = "Sorry, There are too many fighters right now." +
                                       " Choose bombers or change your army or try again later.";
    public string stb_BalanceMsgHud = "Aircraft Disabled: There are too many fighters right now!";
    #endregion

    #region stb Core
    //these variables change in runtime, do not alter their default values
    private Point3d af1 = new Point3d(13800.0, 30890.0, 1.0);
    private Point3d af2 = new Point3d(18180.0, 30760.0, 1.0);
    private Point3d af3 = new Point3d(12180.0, 10520.0, 1.0);
    private Point3d af4 = new Point3d(18830.0, 9810.0, 1.0);
    private bool stb_BomberMissionTurn = true;
    private int stb_RedFighters = 0;
    private int stb_BlueFighters = 0;
    private double stb_Ratio = 0d;
    private double stb_Delta = 0d;
    private string stb_AppPath = (Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
    private string stb_FullPath = "";//will be created in runtime
    private string stb_ErrorLogPath = @"StbVolcanicIslands2_errorlog.txt";//will be combined with fullpath in runtime
    private string stb_StatsPathTxt = @"StbVolcanicIslands2_playerstats_full.txt";//"
    private string stb_StatsPathHtmlLow = @"StbVolcanicIslands2_playerstats_low.htm";//"
    private string stb_StatsPathHtmlMed = @"StbVolcanicIslands2_playerstats_med.htm";//"
    private ISectionFile stb_BaseAntiAirDefenses;
    private ISectionFile stb_Frontline1;
    private ISectionFile stb_Frontline2;
    private ISectionFile stb_Frontline3;
    private ISectionFile stb_Frontline4;
    private ISectionFile stb_Bombers1;
    private ISectionFile stb_Bombers2;
    private NumberFormatInfo stb_nf = new NumberFormatInfo();
    private int stb_MissionsCount = 1;

    public enum StbStatCommands : int { None = 0, Damage = 1, Dead = 2, CutLimb = 3, TaskCurrent = 4, Save = 5 };



    public struct StbStatTask
    {
        public StbStatCommands command;
        public string player;
        public int[] parameters;
        public StbStatTask(StbStatCommands cmd, string pname, int[] prms)
        {
            command = cmd;
            player = pname;
            parameters = prms;
        }
    }

    public StbStatRecorder stb_StatRecorder;

    public class StbStatRecorder
    {
        EventWaitHandle stbSr_Wh = new AutoResetEvent(false);
        Thread stbSr_Worker;
        readonly object stbSr_Locker = new object();
        Queue<StbStatTask> stbSr_Tasks = new Queue<StbStatTask>(2000);
        NumberFormatInfo stbSr_nf = new NumberFormatInfo();
        public Dictionary<string, int[]> stbSr_AllPlayerStats;
        bool stbSr_LogStats;
        bool stbSr_LogStatsCreateHtmlLow;
        bool stbSr_LogStatsCreateHtmlMed;
        bool stbSr_LogStatsUploadHtmlLow;
        bool stbSr_LogStatsUploadHtmlMed;
        string stbSr_LogStatsUploadAddressLow;
        string stbSr_LogStatsUploadAddressMed;
        string stbSr_LogStatsUploadUserName;
        string stbSr_LogStatsUploadPassword;
        bool stbSr_LogErrors;
        string stbSr_ErrorLogPath;
        string stbSr_PlayerStatsPathTxt;
        string stbSr_PlayerStatsPathHtmlLow;
        string stbSr_PlayerStatsPathHtmlMed;

        public StbStatRecorder(bool logStats, bool logStatsCreateHtmlLow, bool logStatsCreateHtmlMed, string statsPathTxt,
                               bool logErrors, string errorLogPath, string statsPathHtmlLow, string statsPathHtmlMed,
                               bool logStatsUploadHtmlLow, bool logStatsUploadHtmlMed,
                               string logStatsUploadAddressLow, string logStatsUploadAddressMed,
                               string logStatsUploadUserName, string logStatsUploadPassword)
        {
            stbSr_AllPlayerStats = new Dictionary<string, int[]>();
            stbSr_LogStats = logStats;
            stbSr_LogStatsCreateHtmlLow = logStatsCreateHtmlLow;
            stbSr_LogStatsCreateHtmlMed = logStatsCreateHtmlMed;
            stbSr_LogStatsUploadHtmlLow = logStatsUploadHtmlLow;
            stbSr_LogStatsUploadHtmlMed = logStatsUploadHtmlMed;
            stbSr_LogStatsUploadAddressLow = logStatsUploadAddressLow;
            stbSr_LogStatsUploadAddressMed = logStatsUploadAddressMed;
            stbSr_LogStatsUploadUserName = logStatsUploadUserName;
            stbSr_LogStatsUploadPassword = logStatsUploadPassword;
            stbSr_PlayerStatsPathTxt = statsPathTxt;
            stbSr_PlayerStatsPathHtmlLow = statsPathHtmlLow;
            stbSr_PlayerStatsPathHtmlMed = statsPathHtmlMed;
            stbSr_LogErrors = logErrors;
            stbSr_ErrorLogPath = errorLogPath;
            StbSr_ReadStatsFromFile();
            stbSr_Worker = new Thread(StbSr_Work);
            //stbSr_Worker.Priority = ThreadPriority.BelowNormal;
            stbSr_Worker.Start();
        }

        public void StbSr_UpdateStatsForDamage(string playerName, int[] p)//p[0]=NamedDamageTypeNo,p[1]=DamageType
        {
            try
            {
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    if (p[1] == 1) //p[1]=DamageType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[642]++; //temp[642]=AirTasksCompletedCount
                        }
                        temp[(p[0])]++;//p[0]=NamedDamageTypeNo
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[1] == 2) //p[1]=DamageType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 2 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[643]++; //temp[643]=GroundTasksCompletedCount
                        }
                        temp[p[0]]++; //p[0]=NamedDamageTypeNo
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[1] == 3) //p[1]=DamageType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[644]++; //temp[644]=NavalTasksCompletedCount
                        }
                        temp[p[0]]++; //p[0]=NamedDamageTypeNo
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForDead(string playerName, int[] p)//p[0]=KillType
        {
            try
            {
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    if (p[0] == 1)//p[0]=KillType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[642]++; //temp[642]=AirTasksCompletedCount
                        }
                        temp[647]++; //temp[647]=AirKillParticipationCount
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[0] == 2)//p[0]=KillType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 2 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[643]++; //temp[643]=GroundTasksCompletedCount
                        }
                        temp[648]++; //temp[648]=GroundKillParticipationCount
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                    else if (p[0] == 3)//p[0]=KillType(1:air,2:ground,3:naval)
                    {
                        if (temp[645] == 3 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                        {
                            temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                            temp[644]++; //temp[644]=NavalTasksCompletedCount
                        }
                        temp[649]++; //temp[649]=NavalKillParticipationCount
                        stbSr_AllPlayerStats[playerName] = temp;
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForCutLimb(string playerName, int[] p)//p[0]=LimbNamesNo,p[1]=TaskNo
        {
            try
            {
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))//p[0]=LimbNo
                {
                    if (temp[645] == 1 && temp[646] == 0) //temp[645]=CurrentTaskNo temp[646]=CurrentTaskCompletedBool
                    {
                        temp[646] = 1; //temp[646]=CurrentTaskCompletedBool
                        temp[642]++; //temp[642]=AirTasksCompletedCount
                    }                     //p[0]=LimbNamesNo
                    temp[(p[0] + 649)]++; //temp[(p[0] + 649)]=CorrespondingLimbNamesNo
                    stbSr_AllPlayerStats[playerName] = temp;
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_UpdateStatsForTaskCurrent(string playerName, int[] p)//p[0]=TaskNo
        {
            try
            {
                int[] temp;
                if (stbSr_AllPlayerStats.TryGetValue(playerName, out temp))
                {
                    temp[0]++; //temp[0]=PlaceEnters
                    temp[645] = p[0]; //temp[645]=CurrentTaskNo p[0]=TaskNo(1:air,2:ground,3:naval)
                    temp[646] = 0; //temp[646]=CurrentTaskCompletedBool
                    stbSr_AllPlayerStats[playerName] = temp;
                }
                else//for first time user
                {
                    temp = new int[770];
                    temp[0]++; //temp[0]=PlaceEnters
                    temp[645] = p[0]; //temp[645]=CurrentTaskNo p[0]=TaskNo(1:air,2:ground,3:naval)
                    stbSr_AllPlayerStats.Add(playerName, temp);
                }
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
                        StreamReader sr = File.OpenText(stbSr_PlayerStatsPathTxt);
                        string s = sr.ReadToEnd();
                        sr.Close();
                        if (s == null) return;
                        if (s == "") return;
                        string[] retrievedStrings = s.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string retrievedStr in retrievedStrings)
                        {
                            StbSr_UpdateSingleUserStat(retrievedStr);
                        }
                    }
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
                string[] retrievedData = retrievedString.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (retrievedData.Length == 2)
                {
                    userName = retrievedData[0];
                    userData = new int[770];
                    string[] retrievedLines = retrievedData[1].Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (retrievedLines.Length == 16)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            string[] values = retrievedLines[j].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            if (j < 15 && values.Length == 50)
                            {
                                for (int i = 0; i < 50; i++)
                                {
                                    userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                                }
                            }
                            else if (j == 15 && values.Length == 20)
                            {
                                for (int i = 0; i < 20; i++)
                                {
                                    userData[i + (j * 50)] = Convert.ToInt32(values[i]);
                                }
                            }
                        }
                    }
                    userData[645] = 0;
                    userData[646] = 0;
                    stbSr_AllPlayerStats.Add(userName, userData);
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStats()
        {
            try
            {
                StbSr_SavePlayerStatsStringToFileFull();
                StbSr_SavePlayerStatsStringToFileMedium();
                StbSr_SavePlayerStatsStringToFileLow();
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStatsStringToFileFull()
        {
            try
            {
                if (stbSr_LogStats)
                {
                    if (stbSr_AllPlayerStats.Count == 0) return;
                    using (StreamWriter sw = new StreamWriter(stbSr_PlayerStatsPathTxt, false, System.Text.Encoding.UTF8))
                    {
                        foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                        {
                            sw.Write(";" + entry.Key + ":");
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
                            for (int k = 750; k < 770; k++)
                            {
                                sw.Write(entry.Value[k].ToString());
                                sw.Write(",");
                            }
                            sw.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_SavePlayerStatsStringToFileMedium()
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

        public void StbSr_SavePlayerStatsStringToFileLow()
        {
            try
            {
                if (stbSr_LogStats)
                {
                    if (stbSr_LogStatsCreateHtmlLow)
                    {
                        using (StreamWriter sw = new StreamWriter(stbSr_PlayerStatsPathHtmlLow, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
                            sw.WriteLine("<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\">");
                            sw.WriteLine("<head>");
                            sw.WriteLine("<title>StatsCompact</title>");
                            sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\" />");
                            sw.WriteLine("<script type=\"text/javascript\" src=\"sorttable.js\"></script>");
                            sw.WriteLine("<link rel=\"stylesheet\" href=\"Style.css\" type=\"text/css\" />");
                            sw.WriteLine("</head>");
                            sw.WriteLine("<body>");
                            sw.WriteLine("<p>Last Update: " + DateTime.Now.ToString("R") + "</p>");
                            sw.WriteLine("<table class=\"sortable\" border=\"0\" cellpadding=\"0\" cellspacing=\"1\">");
                            sw.WriteLine("<thead>");
                            sw.WriteLine("<tr class=\"rh1\"><td class=\"dh bg0\">&nbsp;</td><td class=\"dh bg0\">&nbsp;</td>");
                            sw.WriteLine("<td class=\"dh bg1\" colspan=\"3\">Tasks Completed<hr size=\"1\" noshade=\"noshade\"/></td>");
                            sw.WriteLine("<td class=\"dh bg2\" colspan=\"3\">Kill Participations<hr size=\"1\" noshade=\"noshade\"/></td>");
                            sw.WriteLine("<td class=\"dh bg3\" colspan=\"7\">Damage Counts<hr size=\"1\" noshade=\"noshade\"/></td>");
                            sw.WriteLine("<td class=\"dh bg4\" colspan=\"2\">Cut Limb Counts<hr size=\"1\" noshade=\"noshade\"/></td></tr>");
                            sw.WriteLine("<tr class=\"rh2\">");
                            sw.WriteLine("<th class=\"bg0\">Name<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg0\">Place Enters<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg1\">Air Tasks<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg1\">Ground Tasks<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg1\">Naval Tasks<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg2\">Air Kills<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg2\">Ground Kills<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg2\">Naval Kills<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Fuel Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Electric Hydraulic Pneumatic Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Gun Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Control Flap Brake Wheel Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Cockpit Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Engine Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg3\">Ship Tank Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg4\">Wing Parts<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("<th class=\"bg4\">All Others<hr size=\"1\" noshade=\"noshade\"/></th>");
                            sw.WriteLine("</tr></thead>");
                            sw.WriteLine("<tbody>");
                            bool alternate = true;
                            foreach (KeyValuePair<string, int[]> entry in stbSr_AllPlayerStats)
                            {
                                if (alternate) { sw.WriteLine("<tr class=\"r1\">"); }
                                else { sw.WriteLine("<tr class=\"r2\">"); }
                                alternate = !alternate;
                                sw.WriteLine("<td class=\"dn\">" + entry.Key + "</td>");//Name
                                sw.WriteLine("<td>" + entry.Value[0].ToString() + "</td>");//Place Enters
                                sw.WriteLine("<td>" + entry.Value[642].ToString() + "</td>");//Air Tasks
                                sw.WriteLine("<td>" + entry.Value[643].ToString() + "</td>");//Ground Tasks
                                sw.WriteLine("<td>" + entry.Value[644].ToString() + "</td>");//Naval Tasks
                                sw.WriteLine("<td>" + entry.Value[647].ToString() + "</td>");//Air Kills
                                sw.WriteLine("<td>" + entry.Value[648].ToString() + "</td>");//Ground Kills
                                sw.WriteLine("<td>" + entry.Value[649].ToString() + "</td>");//Naval Kills
                                int sumFuel = 0;
                                for (int i = 1; i < 58; i++) { sumFuel += entry.Value[i]; }//Fuel part damages are [1...57]
                                sw.WriteLine("<td>" + sumFuel.ToString() + "</td>");//Fuel Parts
                                int sumElecHydPne = 0;
                                for (int i = 58; i < 78; i++) { sumElecHydPne += entry.Value[i]; }//ElecHydPne part damages are [58...77]
                                sw.WriteLine("<td>" + sumElecHydPne.ToString() + "</td>");//Electric Hydraulic Pneumatic Parts
                                int sumGun = 0;
                                for (int i = 78; i < 138; i++) { sumGun += entry.Value[i]; }//Gun part damages are [78...137]
                                sw.WriteLine("<td>" + sumGun.ToString() + "</td>");//Gun Parts
                                int sumCFBW = 0;
                                for (int i = 138; i < 169; i++) { sumCFBW += entry.Value[i]; }//CFBW part damages are [138...168]
                                sw.WriteLine("<td>" + sumCFBW.ToString() + "</td>");//Control Flap Brake Wheel Parts
                                int sumCockpit = 0;
                                for (int i = 169; i < 184; i++) { sumCockpit += entry.Value[i]; }//Cockpit part damages are [169...183]
                                sw.WriteLine("<td>" + sumCockpit.ToString() + "</td>");//Cockpit Parts
                                int sumEng = 0;
                                for (int i = 184; i < 632; i++) { sumEng += entry.Value[i]; }//Engine part damages are [184...631]
                                sw.WriteLine("<td>" + sumEng.ToString() + "</td>");//Engine Parts
                                int sumShipTank = 0;
                                for (int i = 632; i < 642; i++) { sumShipTank += entry.Value[i]; }//ShipTank part damages are [632...641]
                                sw.WriteLine("<td>" + sumShipTank.ToString() + "</td>");//Ship Tank Parts
                                int sumWingPartsCut = 0;
                                for (int i = 650; i < 683; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                                for (int i = 687; i < 691; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                                for (int i = 697; i < 705; i++) { sumWingPartsCut += entry.Value[i]; }//WingPart cuts are [650...682][687...690][697...704]
                                sw.WriteLine("<td>" + sumWingPartsCut.ToString() + "</td>");//Wing Parts Cut
                                int sumAllOthersCut = 0;
                                for (int i = 683; i < 687; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                                for (int i = 691; i < 697; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                                for (int i = 705; i < 770; i++) { sumAllOthersCut += entry.Value[i]; }//AllOther cuts are [683...686][691...696][705...769]
                                sw.WriteLine("<td>" + sumAllOthersCut.ToString() + "</td>");//All Others Cut
                                sw.WriteLine("</tr>");
                            }
                            sw.WriteLine("</tbody></table></body></html>");
                        }
                        if (stbSr_LogStatsUploadHtmlLow)
                        {
                            StbSr_Upload(stbSr_LogStatsUploadAddressLow, stbSr_LogStatsUploadUserName, stbSr_LogStatsUploadPassword, stbSr_PlayerStatsPathHtmlLow);
                        }
                    }
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        private void StbSr_Upload(string ftpServer, string userName, string password, string filename)
        {
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Credentials = new System.Net.NetworkCredential(userName, password);
                    client.UploadFile(ftpServer, "STOR", filename);
                }
            }
            catch (Exception ex) { StbSr_PrepareErrorMessage(ex); }
        }

        public void StbSr_EnqueueTask(StbStatTask task)
        {
            lock (stbSr_Locker) stbSr_Tasks.Enqueue(task);
            stbSr_Wh.Set();
        }

        void StbSr_Work()
        {
            while (true)
            {
                StbStatTask task = new StbStatTask(StbStatCommands.None, "", new int[] { 0 });
                lock (stbSr_Locker)
                    if (stbSr_Tasks.Count > 0)
                    {
                        task = stbSr_Tasks.Dequeue();
                        if (task.command == StbStatCommands.None) return;
                    }
                if (task.command != StbStatCommands.None)
                {
                    switch (task.command)
                    {
                        case StbStatCommands.Damage:
                            StbSr_UpdateStatsForDamage(task.player, task.parameters);
                            break;
                        case StbStatCommands.Dead:
                            StbSr_UpdateStatsForDead(task.player, task.parameters);
                            break;
                        case StbStatCommands.CutLimb:
                            StbSr_UpdateStatsForCutLimb(task.player, task.parameters);
                            break;
                        case StbStatCommands.TaskCurrent:
                            StbSr_UpdateStatsForTaskCurrent(task.player, task.parameters);
                            break;
                        case StbStatCommands.Save:
                            StbSr_SavePlayerStats();
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
            StbStatTask t = new StbStatTask(StbStatCommands.None, "", new int[] { 0 });
            StbSr_EnqueueTask(t);// Signal to exit.
            stbSr_Worker.Join();// Wait for the thread to finish.
            stbSr_Wh.Close();// Release resources.
        }

        public void StbSr_PrepareErrorMessage(Exception ex)
        {
            if (stbSr_LogErrors)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(StbSr_LogError),
                    (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
            }
        }

        public void StbSr_LogError(object data)
        {
            try
            {
                FileInfo fi = new FileInfo(stbSr_ErrorLogPath);
                StreamWriter sw;
                if (fi.Exists) { sw = new StreamWriter(stbSr_ErrorLogPath, true, System.Text.Encoding.UTF8); }
                else { sw = new StreamWriter(stbSr_ErrorLogPath, false, System.Text.Encoding.UTF8); }
                sw.WriteLine((string)data);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); };
        }
    }

    #endregion

    #region stb Methods

    // Error Methods-----------------------------------------------------------------------------------------------------------

    public void Stb_PrepareErrorMessage(Exception ex)
    {
        if (stb_LogErrors)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(Stb_LogError),
                (object)("Error @ " + ex.TargetSite.Name + "  Message: " + ex.Message));
        }
    }

    public void Stb_LogError(object data)
    {
        try
        {
            FileInfo fi = new FileInfo(stb_ErrorLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(stb_ErrorLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(stb_ErrorLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); };
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
                StbStatTask sst = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 0 });
                stb_StatRecorder.StbSr_EnqueueTask(sst);
                Timeout(stb_LogStatsDelay, Stb_LogStatsRecursive);
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    // Other Methods-----------------------------------------------------------------------------------------------------------

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
                if (insideAf) { aircraft.Destroy(); }
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
            else { aircraft.Destroy(); }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
    }

    private void Stb_DestroyPlaneUnsafe(AiAircraft aircraft)
    {
        try
        {
            if (aircraft != null)
                aircraft.Destroy();
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

    #endregion

    // Overrides---------------------------------------------------------------------------------------------------------------

    public override void Init(ABattle battle, int missionNumber)
    {
        base.Init(battle, missionNumber);
        MissionNumberListener = -1;
    }
    



    public override void OnMissionLoaded(int missionNumber)
    {
        #region stb
        base.OnMissionLoaded(missionNumber);
        stb_MissionsCount++;
         
     #endregion       
    }

        //add your code here
        //GamePlay.gpLogServer(null, "Stats package started 1", new object[] { });         
        //if (missionNumber==MissionNumber ) {
     public override void OnBattleStarted() {
          //public static void statsMissionInit() {
              #region stb
              //DebugAndLog("Stats package started");
              //GamePlay.gpLogServer(null, "Stats package started 2", new object[] { });          
              //base.OnBattleStarted();
              string s = stb_AppPath.Remove(stb_AppPath.Length - 5, 5);
              //string s="hithere";
              stb_FullPath = s + @"missions\Multi\Fatal\";
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
              stb_StatRecorder = new StbStatRecorder(stb_LogStats, stb_LogStatsCreateHtmlLow, stb_LogStatsCreateHtmlMed, stb_StatsPathTxt,
                                                     stb_LogErrors, stb_ErrorLogPath, stb_StatsPathHtmlLow, stb_StatsPathHtmlMed,
                                                     stb_LogStatsUploadHtmlLow, stb_LogStatsUploadHtmlMed,
                                                     stb_LogStatsUploadAddressLow, stb_LogStatsUploadAddressMed,
                                                     stb_LogStatsUploadUserName, stb_LogStatsUploadPassword);
              if (stb_SpawnAntiAir) { Stb_SpawnAntiAirRecursive(); }
              if (stb_SpawnFrontline1) { Stb_SpawnFrontline1Recursive(); }
              if (stb_SpawnFrontline2) { Stb_SpawnFrontline2Recursive(); }
              if (stb_SpawnFrontline3) { Stb_SpawnFrontline3Recursive(); }
              if (stb_SpawnFrontline4) { Stb_SpawnFrontline4Recursive(); }
              if (stb_SpawnBombers) { Stb_SpawnBombersRecursive(); }
              if (stb_LogStats) { Stb_LogStatsRecursive(); }
              #endregion
              //add your code here
          //}

        }
           
    

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

    public override void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
        #region stb
        base.OnPlaceLeave(player, actor, placeIndex);
        try
        {
            Stb_BalanceUpdate(player, actor, placeIndex, false);
            if (stb_DestroyOnPlaceLeave)
            {
                AiAircraft aircraft = actor as AiAircraft;
                if (aircraft != null)
                {
                    Stb_DestroyPlaneSafe(aircraft);//works fine if you havent take off or flying in neutral zone
                    Timeout(10.0, () => { Stb_DestroyPlaneSafe(aircraft); });//second call for cleanup
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

    public override void OnActorDamaged(int missionNumber, string shortName, AiActor actor, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        #region stb
        base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);
        try
        {
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
                AiGroundActor ga = actor as AiGroundActor;
                if (ga != null)
                {
                    if (ga.Type() == AiGroundActorType.AAGun || ga.Type() == AiGroundActorType.Artillery || ga.Type() == AiGroundActorType.Tank)
                    {
                        StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 2 });
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                    else if (ga.Type() == AiGroundActorType.ShipBattleship || ga.Type() == AiGroundActorType.ShipCarrier ||
                             ga.Type() == AiGroundActorType.ShipCruiser || ga.Type() == AiGroundActorType.ShipDestroyer ||
                             ga.Type() == AiGroundActorType.ShipMisc || ga.Type() == AiGroundActorType.ShipSmallWarship ||
                             ga.Type() == AiGroundActorType.ShipSubmarine || ga.Type() == AiGroundActorType.ShipTransport)
                    {
                        StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 3 });
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnAircraftDamaged(int missionNumber, string shortName, AiAircraft aircraft, AiDamageInitiator initiator, NamedDamageTypes damageType)
    {
        #region stb
        base.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
        try
        {
            bool willReportDamage = false;
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
                StbStatTask sst = new StbStatTask(StbStatCommands.Damage, initiator.Player.Name(), new int[] { (int)damageType, 1 });
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
            bool willReportCutLimb = false;
            if (initiator != null && aircraft != null)
            {
                if (initiator.Player != null)
                {
                    if (aircraft.Army() == 1 && initiator.Player.Army() == 2) { willReportCutLimb = true; }
                    if (aircraft.Army() == 2 && initiator.Player.Army() == 1) { willReportCutLimb = true; }
                }
            }
            if (willReportCutLimb)
            {
                if (((int)limbName > 0) && ((int)limbName < 121))
                {
                    StbStatTask sst = new StbStatTask(StbStatCommands.CutLimb, initiator.Player.Name(), new int[] { (int)limbName });
                    stb_StatRecorder.StbSr_EnqueueTask(sst);
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnActorDead(int missionNumber, string shortName, AiActor actor, List<DamagerScore> damages)
    {
        #region stb
        base.OnActorDead(missionNumber, shortName, actor, damages);
        try
        {
            AiAircraft aiAircraft = actor as AiAircraft;
            if (aiAircraft != null)
            {
                foreach (DamagerScore ds in damages)
                {
                    bool willReportDead = false;
                    if (ds.initiator != null)
                    {
                        if (ds.initiator.Player != null)
                        {
                            if (aiAircraft.Army() == 1 && ds.initiator.Player.Army() == 2) { willReportDead = true; }
                            if (aiAircraft.Army() == 2 && ds.initiator.Player.Army() == 1) { willReportDead = true; }
                        }
                    }
                    if (willReportDead)
                    {
                        StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 1 });
                        stb_StatRecorder.StbSr_EnqueueTask(sst);
                    }
                }
            }
            else
            {
                AiGroundActor aiGroundActor = actor as AiGroundActor;
                if (aiGroundActor != null)
                {
                    if (shortName.Length == 12) { Timeout(99.9, () => { Stb_DestroyFrontShip(aiGroundActor); }); }
                    else if (shortName.Length == 14) { Timeout(99.9, () => { Stb_DestroyFrontArmor(aiGroundActor); }); }

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
                                StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 2 });
                                stb_StatRecorder.StbSr_EnqueueTask(sst);
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
                                StbStatTask sst = new StbStatTask(StbStatCommands.Dead, ds.initiator.Player.Name(), new int[] { 3 });
                                stb_StatRecorder.StbSr_EnqueueTask(sst);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

    public override void OnBattleStoped()
    {
        #region
        base.OnBattleStoped();
        try
        {
            if (stb_LogStats)
            {
                StbStatTask sst = new StbStatTask(StbStatCommands.Save, "noname", new int[] { 0 });
                stb_StatRecorder.StbSr_EnqueueTask(sst);
            }
            stb_StatRecorder.StbSr_FinishWaitingTasks();
        }
        catch (Exception ex) { Stb_PrepareErrorMessage(ex); }
        #endregion
        //add your code here
    }

}
