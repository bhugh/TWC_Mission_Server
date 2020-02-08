//$reference parts/core/CLOD_Extensions.dll
//$reference parts/core/Strategy.dll
//$reference parts/core/gamePlay.dll
//$reference parts/core/gamePages.dll

using System;
using System.Collections.Generic;
using System.IO;

using maddox.game;
using maddox.game.world;
using maddox.GP;
using maddox.game.page;
using part;

using TF_Extensions;



public class Mission : AMission
{

    private static string PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/1C SoftClub/il-2 sturmovik cliffs of dover/missions/Multi/Fatal/Campaign21/Fresh Input File/";

    public List<string> BannedPlayerList;
    public Mission()
    {
        string[] banned = File.ReadAllLines(PATH + "bannedPlayers.txt");
        BannedPlayerList = new List<string>(banned);
        BannedPlayerList = BannedPlayerList.ConvertAll(d => d.ToUpper());
    }

    /* public override void Inited()
        {

            string[] banned = File.ReadAllLines(PATH + "bannedPlayers.txt");
            BannedPlayerList = new List<string>(banned);
            BannedPlayerList = BannedPlayerList.ConvertAll(d => d.ToUpper());


        }
        */

    public override void OnBattleStarted()
    {
        base.OnBattleStarted();

        if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban LOAD");  //load file banned.txt in Documents\1C SoftClub\il-2 sturmovik cliffs of dover.  Note: Don't try to change the filename, just use the default.

    }

    public override void OnPlayerConnected(Player player)
    {
        if (MissionNumber > -1)
        {
            if (player != null &&
            player.Name() != null && player.Name() != "")

            {
                //string msg = "BurnBabyBurn: Player logged in: " + player.Name() + " " + player.ConnectAddress() + " " + TF_Extensions.TF_Player.GetSteamID(player);
                string msg = "BurnBabyBurn: Player logged in: " + player.Name() + " " + player.ConnectAddress();
                logToFile(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + msg, PATH + "loggedInPlayers.log");

                if (player != null &&
                    (player.Name() != null && player.Name() != "" && BannedPlayerList.Contains(player.Name().ToUpper())) ||
                    (player.ConnectAddress() != null && player.ConnectAddress() != "" && BannedPlayerList.Contains(player.ConnectAddress().ToUpper()))

                )
                {

                    msg = "BurnBabyBurn: Player banned, kicking from server: " + player.Name()
                    + " " + player.ConnectAddress() + " " + player.ConnectPort();
                    Console.WriteLine(msg);
                    //msg = "BurnBabyBurn: Player banned: " + player.Name() + " " + player.ConnectAddress() + " " + TF_Extensions.TF_Player.GetSteamID(player); 
                    msg = "BurnBabyBurn: Player banned: " + player.Name() + " " + player.ConnectAddress();
                    logToFile(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + msg, PATH + "bannedPlayers.log");
                    if (GamePlay is GameDef) Console.WriteLine("GAMEDEF is initialized");
                    if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban ADD NAME \"" + player.Name() + "\"");
                    if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban SAVE");
                    if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("kick \"" + player.Name() + "\"");

                    //Battle.OnEventGame(GameEventId.PlayerDisconnected, player, "banned", 0);
                }


            }
        }
    }

    public override void OnPlayerArmy(Player player, int Army)
    {
        if (MissionNumber > -1)
        {

            if (player != null &&
        (player.Name() != null && player.Name() != "" && BannedPlayerList.Contains(player.Name().ToUpper())) ||
        (player.ConnectAddress() != null && player.ConnectAddress() != "" && BannedPlayerList.Contains(player.ConnectAddress().ToUpper()))

        )
            {

                string msg = "BurnBabyBurn: Selecting Army, player banned, kicking from server: " + player.Name()
                + " " + player.ConnectAddress() + " " + player.ConnectPort();
                Console.WriteLine(msg);
                //msg = "BurnBabyBurn: Player banned: " + player.Name() + " " + player.ConnectAddress() + " " + TF_Extensions.TF_Player.GetSteamID(player); 
                msg = "BurnBabyBurn: Player banned: " + player.Name() + " " + player.ConnectAddress();
                logToFile(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + msg, PATH + "bannedPlayers.log");
                if (GamePlay is GameDef) Console.WriteLine("GAMEDEF is initialized");
                if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban ADD NAME \"" + player.Name() + "\"");
                if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban SAVE");
                if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("kick \"" + player.Name() + "\"");

                //Battle.OnEventGame(GameEventId.PlayerDisconnected, player, "banned", 0);
            }


        }


    }
    


    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
    {
        base.OnPlaceEnter(player, actor, placeIndex);

        Random ran = new Random();



        if (player != null &&
            (player.Name() != null && player.Name() != "" && BannedPlayerList.Contains(player.Name().ToUpper())) ||
            (player.ConnectAddress() != null && player.ConnectAddress() != "" && BannedPlayerList.Contains(player.ConnectAddress().ToUpper()))

            )
        {

            string msg = "BurnBabyBurn: Player banned, initiating engine destruction after random wait: " + player.Name()
            + " " + player.ConnectAddress() + " " + player.ConnectPort();
            Console.WriteLine(msg);
            //msg = "BurnBabyBurn: Player banned: " + player.Name() + " " + player.ConnectAddress() + " " + TF_Extensions.TF_Player.GetSteamID(player); 
            msg = "BurnBabyBurn: Player banned: " + player.Name() + " " + player.ConnectAddress();
            logToFile(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " : " + msg, PATH + "bannedPlayers.log");
            if (GamePlay is GameDef) Console.WriteLine("GAMEDEF is initialized");
                if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban ADD NAME \"" + player.Name() + "\"");
            if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("ban SAVE");
            if (GamePlay is GameDef) (GamePlay as GameDef).gameInterface.CmdExec("kick \"" + player.Name() + "\"");

            Battle.OnEventGame(GameEventId.PlayerDisconnected, player, "banned", 0);



            Timeout(2, () => { player.PlaceLeave(0); });

            if (actor as AiCart != null)
            {
             Timeout(1, ()=> { Stb_RemovePlayerFromCart(actor as AiCart, player); });
                Timeout(2, () => { Stb_RemovePlayerFromCart(actor as AiCart, player); });
                Timeout(3, () => { Stb_RemovePlayerFromCart(actor as AiCart, player); });
                Timeout(4, () => { Stb_RemovePlayerFromCart(actor as AiCart, player); });
                Timeout(5, () => { Stb_RemovePlayerFromCart(actor as AiCart, player); });

                Console.WriteLine("BurnBabyBurn: Player banned, stb_reove." + player.Name());

            }

                    player.PlaceLeave(player.PlacePrimary());
            player.PlaceLeave(placeIndex);
            player.PlaceLeave(player.PlaceSecondary());
                player.PlaceLeave(0);



            if (GamePlay is GameDef)
            {
                AiActor newActor = (GamePlay as GameDef).gpActorByName("Flak");
                player.PlaceEnter(newActor, 0);
            }


            Console.WriteLine("BurnBabyBurn: Player banned, kicked out of aircraft/vehicle." + player.Name());
            return;

            //(actor as AiCart).Destroy();
            //AiCart.Places()
            //(actor as AiCart).Destroy();

            //Player.PlaceLeave(int)

            Timeout(ran.Next(1 * 60) + 15, () =>
            {
                if (player.Place() != null)
                {
                    (player.Place() as AiCart).Destroy();
                    Console.WriteLine("BurnBabyBurn: Player banned, kicked out of aircraft/vehicle." + player.Name());
                }
            });

            AiAircraft aircraft = actor as AiAircraft;
            if (aircraft != null)
            {
                int iNumOfEngines = 1;
                try
                {
                    iNumOfEngines = (aircraft.Group() as AiAirGroup).aircraftEnginesNum();
                }
                catch (Exception ex) { Console.WriteLine("BurnBabyBurn ERROR1: " + ex.ToString()); };
                //aircraft.hitNamed(part.NamedDamageTypes.FuelTank0Fire);
                //aircraft.hitNamed(part.NamedDamageTypes.FuelTank1Fire); //can't do fueltank1 if only 1 engine
                //aircraft.hitNamed(part.NamedDamageTypes.Eng0OilLineBroken);
                /*
                 * //so these don't work, not sure why not.
                aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled); //NamedDamageTypes.ControlsAileronsDisabled
                aircraft.hitNamed(part.NamedDamageTypes.ControlsAileronsDisabled);
                aircraft.hitNamed(part.NamedDamageTypes.ControlsElevatorDisabled);
                */

                aircraft.hitNamed(NamedDamageTypes.Machinegun00BeltBroken);
                aircraft.RearmPlane(true); //guns only, take away the bombs
                aircraft.RefuelPlane(2);

                //These work REAL good.
                /*
                aircraft.cutLimb(part.LimbNames.AileronL0);
                aircraft.cutLimb(part.LimbNames.AileronR0);

                aircraft.cutLimb(part.LimbNames.ElevatorL0);
                aircraft.cutLimb(part.LimbNames.ElevatorR0);

                aircraft.cutLimb(part.LimbNames.Rudder0);
                */

                //aircraft.hitLimb(part.LimbNames.WingL7, -0.7);
                //Flames out the engine BUT waits randomly for up to 6 mins for it to happen
                //aircraft.hitLimb(part.LimbNames.WingL4, 0.0); //small hole about 2/3 of the way out on the wingtop
                //aircraft.hitLimb(part.LimbNames.Rudder0, 0.0); //small holes in rudder

                Timeout(ran.Next(1 * 60) + 30, () =>
                {
                    aircraft.hitNamed(part.NamedDamageTypes.FuelTank0Fire);
                    //aircraft.hitNamed(part.NamedDamageTypes.FuelTank1Fire);
                    aircraft.hitNamed(part.NamedDamageTypes.Eng0OilLineBroken); //can't do oilline 1 if only 1 engine
                                                                                //aircraft.hitNamed(part.NamedDamageTypes.Eng1OilLineBroken);

                    aircraft.cutLimb(part.LimbNames.BayDoor0);
                    Console.WriteLine("BurnBabyBurn: Player banned, engine is now destroyed: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken
                Timeout(ran.Next(1 * 60) + 1 * 60 + 30, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                    aircraft.hitLimb(part.LimbNames.Rudder0, 1);
                    Console.WriteLine("BurnBabyBurn: Player banned, RUDDER is now DAMAGED: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken
                Timeout(ran.Next(1 * 60) + 2 * 60 + 30, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                    if (iNumOfEngines > 1)
                    {
                        aircraft.hitNamed(part.NamedDamageTypes.Eng1OilLineBroken);
                        aircraft.hitNamed(part.NamedDamageTypes.FuelTank1Fire);
                        aircraft.hitLimb(part.LimbNames.WingR4, 0.0);
                        Console.WriteLine("BurnBabyBurn: Player banned, SECOND ENGINE is now DAMAGED: " + player.Name());
                    }
                }); //NamedDamageTypes.Eng1OilLineBroken

                Timeout(ran.Next(1 * 60) + 3 * 60 + 90, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                    aircraft.hitLimb(part.LimbNames.WingL4, 0.0);
                    aircraft.hitLimb(part.LimbNames.WingL3, 0.0);
                    Console.WriteLine("BurnBabyBurn: Player banned, WING is now DAMAGED: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken

                Timeout(ran.Next(1 * 60) + 3 * 60 + 90, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);                    
                    aircraft.cutLimb(part.LimbNames.Rudder0);
                    Console.WriteLine("BurnBabyBurn: Player banned, WING is now DAMAGED: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken

                Timeout(ran.Next(1 * 60) + 4 * 60 + 30, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                    aircraft.cutLimb(part.LimbNames.Rudder0);
                    aircraft.hitLimb(part.LimbNames.WingR3, 0.9);
                    Console.WriteLine("BurnBabyBurn: Player banned, RUDDER is now DESTROYED: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken

                Timeout(ran.Next(1 * 60) + 5 * 60 + 30, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                    aircraft.cutLimb(part.LimbNames.AileronL0);
                    Console.WriteLine("BurnBabyBurn: Player banned, LEFT AILERON is now DESTROYED: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken

                Timeout(ran.Next(1 * 60) + 5 * 60 + 30, () =>
                {
                    //aircraft.hitNamed(part.NamedDamageTypes.ControlsRudderDisabled);
                    aircraft.cutLimb(part.LimbNames.ElevatorL0);
                    aircraft.hitNamed(part.NamedDamageTypes.Eng0CylinderHeadFire);
                    aircraft.hitNamed(part.NamedDamageTypes.Eng0FuelSecondariesFire);
                    aircraft.hitNamed(part.NamedDamageTypes.Eng0OilSecondariesFire);
                    Console.WriteLine("BurnBabyBurn: Player banned, LEFT AILERON is now DESTROYED: " + player.Name());
                }); //NamedDamageTypes.Eng1OilLineBroken

                /*********************************************************************
                 * 
                 * TO MAKE HITNAMED work requires a trick--take the player out of the a/c momentarily
                 * 
                 * SEE CODE BELOW.  But, even the first trick doesn't work FOR SOME REASON.
                 * 
                 * Supposedly the second trick works.   However, I have not tested it.
                 * 
                 * Source: https://theairtacticalassaultgroup.com/forum/showthread.php?t=31609
                 * Author: varrattu
                 * 
                 * ******************************************************************/

                /*
                 * public class Mission : AMission {
	
                    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex) {
                        base.OnPlaceEnter(player, actor, placeIndex);

                        AiAircraft aircraft = player.Place() as AiAircraft;

                        GamePlay.gpLogServer(null, aircraft.ToString(), new object [] { });

                        if (aircraft.Player(0) != null) {
                            player = aircraft.Player(0);

                            GamePlay.gpLogServer(null, player.ToString(), new object [] { });

                            /// Remove the player from the aircraft for a few ms.
                            /// A workaround to do damage to player aircraft.
                            player.PlaceLeave(0);

                            Timeout(0.1, () => {
                
                                aircraft.hitNamed(part.NamedDamageTypes.Eng0CylinderHeadFire);
                                aircraft.hitNamed(part.NamedDamageTypes.Eng0FuelSecondariesFire);
                                aircraft.hitNamed(part.NamedDamageTypes.Eng0OilSecondariesFire);

                                player.PlaceEnter(aircraft, 0);
                            });
                        }
                   */

                /*
                    using System.Collections.Generic;

                    using maddox.game;
                    using maddox.game.world;
                    using maddox.GP;

                    public class Mission : AMission
                    {
                        /// choose aircraft to be damaged
                        string targetAiAircraft = "Wellington";

                        /// choose delay for damage 
                        int delayInSeconds = 10;

                        public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex)
                        {
                            base.OnPlaceEnter(player, actor, placeIndex);

                            if (player.Place() != null)
                                {

                                    Point3d playerPos3d = player.Place().Pos();

                                    DamageTargetAiAircraft(playerPos3d, delayInSeconds, targetAiAircraft);
                                }


                        }


                        public void DamageTargetAiAircraft(Point3d playerPos3d, int timeInSeconds, string targetType)
                        {

                            foreach (int army in GamePlay.gpArmies())
                            {
                                if (GamePlay.gpAirGroups(army) != null)

                                    foreach (AiAirGroup group in GamePlay.gpAirGroups(army))
                                    {
                                        if (group.GetItems() != null)

                                            foreach (AiActor actor in group.GetItems())
                                            {
                                                if ((actor as AiAircraft) != null
                                                        && (actor as AiAircraft).InternalTypeName().Contains(targetType))
                                                {
                                                    Timeout(timeInSeconds, () => {

                                                        AiAircraft aircraft = actor as AiAircraft;

                                                        aircraft.hitNamed(part.NamedDamageTypes.Eng1CylinderHeadFire);
                                                        aircraft.hitNamed(part.NamedDamageTypes.Eng1FuelSecondariesFire);
                                                        aircraft.hitNamed(part.NamedDamageTypes.Eng1OilSecondariesFire);

                                                    });
                                                }
                                            }
                                    }
                            }
                        }
                    } 

                 * */






            }
        }
    }



    public void Stb_RemovePlayerFromCart(AiCart cart, Player player = null) //removes a certain player from any aircraft, artillery, vehicle, ship, or whatever actor/cart the player is in.  Removes from ALL places.
                                                                            //if player = null then remove ALL players from ALL positions
                                                                            //I am not 100% sure this really works, it is quirky at teh very least.
    {
        try
        {

            if (cart == null)
                return;

            //check if the player is in any of the "places" - if so remove
            for (int i = 0; i < cart.Places(); i++)
            {
                if (cart.Player(i) == null) continue;
                if (player != null)
                {
                    if (cart.Player(i).Name() == player.Name()) player.PlaceLeave(i); //we tell if they are the same player by their username.  Not sure if there is a better way.
                }
                else
                {
                    cart.Player(i).PlaceLeave(i);
                }
            }

        }
        catch (Exception ex) { Console.WriteLine(ex.ToString()); };
    }

    public void logToFile(object data, string messageLogPath)
    {
        try
        {
            FileInfo fi = new FileInfo(messageLogPath);
            StreamWriter sw;
            if (fi.Exists) { sw = new StreamWriter(messageLogPath, true, System.Text.Encoding.UTF8); }
            else { sw = new StreamWriter(messageLogPath, false, System.Text.Encoding.UTF8); }
            sw.WriteLine((string)data);
            sw.Flush();
            sw.Close();
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); };
    }


}
