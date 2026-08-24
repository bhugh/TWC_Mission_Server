// Decompiled with JetBrains decompiler
// Type: TacviewRecorder.TacviewMission
// Assembly: TacviewRecorder, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: E5A89372-DCC4-4522-B9A9-B1BE0AF06DA5
// Assembly location: C:\Users\Brent Hugh.BRENT-DESKTOP\Documents\1C SoftClub\il-2 sturmovik cliffs of dover\missions\Multi\Fatal\Genghis-2026-08-08-allworking - forgithub\TacviewRecorder.dll

using maddox.game;
using maddox.game.world;
using maddox.GP;
using part;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace TacviewRecorder
{
  public abstract class TacviewMission : AMission
  {
    private static readonly CultureInfo ci = new CultureInfo("en-us");
    private string _destinationFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\1C SoftClub\\il-2 sturmovik cliffs of dover\\Tacview\\";
    private StreamWriter oLogStream;
    private FileStream oLogFile;
    private TacviewCore oTacViewCore;
    private int _startDelay;
    private bool _showPlayer = true;
    private bool _showPlayerAsHuman;
    private TacviewMission.TypeOfMission _missionType = TacviewMission.TypeOfMission.BigMission;
    private bool _zipFile = true;
    private bool IsDisable;
    private SortedList<string, TacviewCore.TacViewWayPoint> tempWaypoints = new SortedList<string, TacviewCore.TacViewWayPoint>();
    private Queue<string> tempBookmark = new Queue<string>();
    private bool pDebug;

    ~TacviewMission()
    {
      try
      {
      }
      finally
      {
        // ISSUE: explicit finalizer call
        // ISSUE: explicit non-virtual call
        __nonvirtual (((object) this).Finalize());
      }
    }

    private void InitLog()
    {
      this.oLogFile = new FileStream(this._destinationFolder + "\\tac-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log", FileMode.Append);
      this.oLogStream = new StreamWriter((Stream) this.oLogFile, Encoding.UTF8);
    }

    internal void ToLog(string message, bool toConsole = false) => this.ToLogForce(message, toConsole);

    internal void ToLogForce(string message, bool toConsole = true)
    {
      try
      {
        if (toConsole)
          this.GamePlay.gpLogServer("Recorder : " + message);
        if (!this.pDebug)
          return;
        if (this.oLogStream == null)
          this.InitLog();
        string str = this.Time.current().ToString("0.00", (IFormatProvider) TacviewMission.ci).Trim();
        this.oLogStream.WriteLine(DateTime.Now.ToString("[dd-MM-yyyy hh:mm:ss.fff]") + ";" + str + ";" + message);
        this.oLogStream.Flush();
      }
      catch (Exception ex)
      {
        this.GamePlay.gpLogServer("Base - ToLog : " + ex.Message + " " + ex.StackTrace);
      }
    }

    private void CloseLog()
    {
      if (this.oLogStream == null)
        return;
      this.oLogStream.Flush();
      this.oLogStream.Close();
      this.oLogStream.Dispose();
    }

    public bool InLog
    {
      get => this.pDebug;
      set => this.pDebug = value;
    }

    public TacviewMission.TypeOfMission MissionType
    {
      get => this._missionType;
      set => this._missionType = value;
    }

    public int StartDelay
    {
      get => this._startDelay;
      set => this._startDelay = value;
    }

    public bool ShowPlayer
    {
      get => this._showPlayer;
      set => this._showPlayer = value;
    }

    public bool ShowPlayerAsHuman
    {
      get => this._showPlayerAsHuman;
      set => this._showPlayerAsHuman = value;
    }

    public string DestinationFolder
    {
      get => this._destinationFolder;
      set
      {
        try
        {
          if (!(value != ""))
            return;
          if (Directory.Exists(value))
          {
            try
            {
              WindowsIdentity current = WindowsIdentity.GetCurrent();
              WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
              foreach (FileSystemAccessRule accessRule in (ReadOnlyCollectionBase) Directory.GetAccessControl(value).GetAccessRules(true, true, typeof (NTAccount)))
              {
                if ((current.User.Equals((object) accessRule.IdentityReference) || windowsPrincipal.IsInRole(accessRule.IdentityReference.Value)) && (FileSystemRights.Write & accessRule.FileSystemRights) != (FileSystemRights) 0 && accessRule.AccessControlType == AccessControlType.Allow)
                {
                  this._destinationFolder = value;
                  return;
                }
              }
              this.GamePlay.gpLogServer("Tacview - Destination folder : access denied.");
            }
            catch (Exception ex)
            {
              this.GamePlay.gpLogServer("Tacview - Destination folder error : " + ex.Message);
            }
          }
          else
            this.GamePlay.gpLogServer("Tacview - Destination folder : not exists");
        }
        catch (Exception ex)
        {
          this.GamePlay.gpLogServer("Tacview - Destination folder Error : " + ex.Message);
        }
      }
    }

    public bool ZipFinalFile
    {
      get => this._zipFile;
      set => this._zipFile = value;
    }

    public void AddBookmark(string message)
    {
      try
      {
        if (message == "")
          return;
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.AddBookmark(message)));
        else
          this.tempBookmark.Enqueue(message);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - AddBookmark error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    private void AddWaypoint(string name, Point3d pos, int army = 0)
    {
      try
      {
        if (this.oTacViewCore == null)
          return;
        this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.AddWaypoint(name, pos.x, pos.y, pos.z, army)));
      }
      catch (Exception ex)
      {
        this.oTacViewCore.ToLogForce("Base - AddWaypoint error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public void AddWaypoint(string name, double x, double y, double z, int army = 0)
    {
      try
      {
        if (this.oTacViewCore != null)
        {
          if (x < 0.0 || y < 0.0 || z < 0.0)
            return;
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.AddWaypoint(name, x, y, z, army)));
        }
        else
        {
          if (this.tempWaypoints.ContainsKey(name))
            this.tempWaypoints.Remove(name);
          this.tempWaypoints.Add(name, new TacviewCore.TacViewWayPoint()
          {
            Name = name,
            TacID = "",
            X = x,
            Y = y,
            Z = z,
            Army = army
          });
        }
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - AddWaypoint(string name, double x, double y, double z, int army = 0) error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public void AddGeologicalWaypoint(
      string name,
      double latitude,
      double longitude,
      double altitude)
    {
      this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.AddGeologicalWaypoint(name, latitude, longitude, altitude)));
    }

    public virtual void Init(ABattle battle, int missionNumber)
    {
      base.Init(battle, missionNumber);
      this.MissionNumberListener = -1;
    }

    public void RemoveWaypoint(string name)
    {
      try
      {
        if (this.oTacViewCore != null)
        {
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.RemoveWaypoint(name)));
        }
        else
        {
          if (!this.tempWaypoints.ContainsKey(name))
            return;
          this.tempWaypoints.Remove(name);
        }
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - RemoveWaypoint error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public void StartRecorder()
    {
      try
      {
        if (this.oTacViewCore == null || this.oTacViewCore.state == TacviewCore.State.idle)
          return;
        this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.Start()));
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - StartRecorder error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public void PauseRecorder()
    {
      try
      {
        if (this.oTacViewCore == null || this.oTacViewCore.state == TacviewCore.State.idle)
          return;
        this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.Pause()));
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - PauseRecorder error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public void StopRecorder()
    {
      try
      {
        if (this.oTacViewCore == null || this.oTacViewCore.state == TacviewCore.State.idle)
          return;
        this.ToLog("Base - Stop recorder");
        this.oTacViewCore.Stop();
        this.oTacViewCore = (TacviewCore) null;
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - StopRecorder error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public void DisableRecorder()
    {
      try
      {
        if (this.IsDisable)
          return;
        if (this.oTacViewCore != null)
          this.oTacViewCore.Disable();
        this.IsDisable = true;
        this.oTacViewCore = (TacviewCore) null;
      }
      catch (Exception ex)
      {
        this.oTacViewCore.ToLogForce("Base - DisableRecorder error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnBattleInit()
    {
      try
      {
        if (!this.IsDisable)
        {
          this.oTacViewCore = TacviewCore.GetInstance();
          if (this.oTacViewCore != null)
          {
            this.ToLog("Base - OnBattleInit : Inititalize recorder.");
            this.oTacViewCore.Initialize(this, this._missionType, this._startDelay, this._destinationFolder, this._showPlayer, this._showPlayerAsHuman, this._zipFile, this.pDebug);
            if (this.tempWaypoints.Count > 0)
            {
              foreach (TacviewCore.TacViewWayPoint tacViewWayPoint in (IEnumerable<TacviewCore.TacViewWayPoint>) this.tempWaypoints.Values)
              {
                TacviewCore.TacViewWayPoint wayPoint = tacViewWayPoint;
                this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.AddWaypoint(wayPoint.Name, wayPoint.X, wayPoint.Y, wayPoint.Z, wayPoint.Army)));
              }
              this.tempWaypoints.Clear();
            }
            while (this.tempBookmark.Count > 0)
            {
              string message = this.tempBookmark.Dequeue();
              this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.AddEvent(TacviewCore.TacViewEventType.Bookmark, "", message)));
            }
          }
        }
        base.OnBattleInit();
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnBattleInit error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnBattleStarted()
    {
      try
      {
        if (this.oTacViewCore != null && this.oTacViewCore.state == TacviewCore.State.idle)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnBattleStarted()));
        base.OnBattleStarted();
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnBattleStarted error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnBattleStoped()
    {
      try
      {
        this.ToLogForce("Base - OnBattleStoped");
        if (this.oTacViewCore != null)
        {
          this.StopRecorder();
          TacviewCore.DestroyInstance();
          this.oTacViewCore = (TacviewCore) null;
        }
        this.CloseLog();
        base.OnBattleStoped();
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnBattleStoped error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnActorCreated(int missionNumber, string shortName, AiActor actor)
    {
      try
      {
        if (this.oTacViewCore != null)
        {
          if (shortName == "NONAME")
            return;
          if (actor is AiAircraft || actor is AiGroundActor)
            this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnActorCreated(missionNumber, shortName, actor)));
        }
        base.OnActorCreated(missionNumber, shortName, actor);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnActorCreated error : " + ex.Message + " - " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnActorDamaged(
      int missionNumber,
      string shortName,
      AiActor actor,
      AiDamageInitiator initiator,
      NamedDamageTypes damageType)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType)));
        base.OnActorDamaged(missionNumber, shortName, actor, initiator, damageType);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base : OnActorDamaged error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnActorDead(
      int missionName,
      string shortName,
      AiActor actor,
      List<DamagerScore> damages)
    {
      try
      {
        if (actor.Name() != "NONAME" && this.oTacViewCore != null && !(actor is AiAircraft))
        {
          Array copyDamages = (Array) damages.ToArray();
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnActorDead(missionName, shortName, actor, copyDamages)));
        }
        base.OnActorDead(this.MissionNumber, shortName, actor, damages);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnActorDead error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnActorDestroyed(int missionNumber, string shortName, AiActor actor)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnActorDestroyed(missionNumber, shortName, actor)));
        base.OnActorDestroyed(missionNumber, shortName, actor);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnActorDestroyed error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftDamaged(
      int missionNumber,
      string shortName,
      AiAircraft aircraft,
      AiDamageInitiator initiator,
      NamedDamageTypes damageType)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, new NamedDamageTypes?(damageType))));
        base.OnAircraftDamaged(missionNumber, shortName, aircraft, initiator, damageType);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base : OnAircraftDamaged error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftLimbDamaged(
      int missionNumber,
      string shortName,
      AiAircraft aircraft,
      AiLimbDamage limbDamage)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftDamaged(missionNumber, shortName, aircraft)));
        base.OnAircraftLimbDamaged(missionNumber, shortName, aircraft, limbDamage);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base : OnAircraftLimbDamaged error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftCutLimb(
      int missionNumber,
      string shortName,
      AiAircraft aircraft,
      AiDamageInitiator initiator,
      LimbNames limbName)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftDamaged(missionNumber, shortName, aircraft)));
        base.OnAircraftCutLimb(missionNumber, shortName, aircraft, initiator, limbName);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base : OnAircraftCutLimb error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftKilled(int missionNumber, string shortName, AiAircraft aircraft)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftKilled(missionNumber, shortName, aircraft)));
        base.OnAircraftKilled(missionNumber, shortName, aircraft);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base : OnAircraftKilled error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftCrashLanded(
      int missionNumber,
      string shortName,
      AiAircraft aircraft)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftCrashLanded(missionNumber, shortName, aircraft)));
        base.OnAircraftCrashLanded(missionNumber, shortName, aircraft);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnAircraftCrashLanded error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftTookOff(missionNumber, shortName, aircraft)));
        base.OnAircraftTookOff(missionNumber, shortName, aircraft);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnAircraftTookOff error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnAircraftLanded(missionNumber, shortName, aircraft)));
        base.OnAircraftLanded(missionNumber, shortName, aircraft);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnAircraftLanded error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnMissionLoaded(int missionNumber)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnMissionLoaded(missionNumber)));
        base.OnMissionLoaded(missionNumber);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnMissionLoaded error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPersonMoved(AiPerson person, AiActor fromCart, int fromPlaceIndex)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPersonMoved(person, fromCart, fromPlaceIndex)));
        base.OnPersonMoved(person, fromCart, fromPlaceIndex);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPersonMoved error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPersonHealth(
      AiPerson person,
      AiDamageInitiator initiator,
      float deltaHealth)
    {
      try
      {
        if (this.oTacViewCore != null && person.Player() != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPersonHealth(person, initiator, deltaHealth)));
        base.OnPersonHealth(person, initiator, deltaHealth);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPersonHealth error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPersonParachuteLanded(AiPerson person)
    {
      try
      {
        if (this.oTacViewCore != null && person.Player() != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPersonParachuteLanded(person)));
        base.OnPersonParachuteFailed(person);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPersonParachuteLanded error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPersonParachuteFailed(AiPerson person)
    {
      try
      {
        if (this.oTacViewCore != null && person.Player() != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPersonParachuteFailed(person)));
        base.OnPersonParachuteFailed(person);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPersonParachuteFailed error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPlaceEnter(player, actor, placeIndex)));
        base.OnPlaceEnter(player, actor, placeIndex);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPlaceEnter error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPlaceLeave(Player player, AiActor actor, int placeIndex)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPlaceLeave(player, actor, placeIndex)));
        base.OnPlaceLeave(player, actor, placeIndex);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPlaceLeave error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnPlayerArmy(Player player, int army)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPlayerArmy(player, army)));
        base.OnPlayerConnected(player);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPlayerArmy error : " + ex.Message + " " + ex.StackTrace);
        throw;
      }
    }

    public virtual void OnPlayerConnected(Player player)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPlayerConnected(player)));
        base.OnPlayerConnected(player);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPlayerConnected error : " + ex.Message + " " + ex.StackTrace);
        throw;
      }
    }

    public virtual void OnPlayerDisconnected(Player player, string diagnostic)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnPlayerDisconnected(player, diagnostic)));
        base.OnPlayerDisconnected(player, diagnostic);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnPlayerDisconnected error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnBuildingKilled(
      string title,
      Point3d pos,
      AiDamageInitiator initiator,
      int eventArgInt)
    {
      try
      {
        if (this.oTacViewCore != null && this.oTacViewCore.missionType == TacviewMission.TypeOfMission.BigMission)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnBuildingKilled(title, pos, initiator, eventArgInt)));
        base.OnBuildingKilled(title, pos, initiator, eventArgInt);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnBuildingKilled error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnStationaryKilled(
      int missionNumber,
      GroundStationary _stationary,
      AiDamageInitiator initiator,
      int eventArgInt)
    {
      try
      {
        if (this.oTacViewCore != null && this.oTacViewCore.missionType == TacviewMission.TypeOfMission.BigMission)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnStationaryKilled(missionNumber, _stationary, initiator, eventArgInt)));
        base.OnStationaryKilled(missionNumber, _stationary, initiator, eventArgInt);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnStationaryKilled error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public virtual void OnBombExplosion(
      string title,
      double mass,
      Point3d pos,
      AiDamageInitiator initiator,
      int eventArgInt)
    {
      try
      {
        if (this.oTacViewCore != null)
          this.oTacViewCore.Enqueue((Action) (() => this.oTacViewCore.OnBombExplosion(title, mass, pos, initiator, eventArgInt, 5)));
        base.OnBombExplosion(title, mass, pos, initiator, eventArgInt);
      }
      catch (Exception ex)
      {
        this.ToLogForce("Base - OnBombExplosion error : " + ex.Message + " " + ex.StackTrace);
        throw ex;
      }
    }

    public enum TypeOfMission
    {
      DogFight,
      Normal,
      BigMission,
    }
  }
}
