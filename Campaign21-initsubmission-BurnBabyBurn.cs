

using System;
using System.Collections.Generic;
using System.IO;

using maddox.game;
using maddox.game.world;
using part;


public class Mission : AMission {

    private static string PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/1C SoftClub/il-2 sturmovik cliffs of dover/missions/Multi/Fatal/Campaign21/Fresh Input File/";

    public List<string> BannedPlayerList;


   public override void Inited() {

        string[] banned = File.ReadAllLines(PATH + "bannedPlayers.txt");
		BannedPlayerList = new List<string>(banned);
    }


    public override void OnPlaceEnter(Player player, AiActor actor, int placeIndex) {
		
		if (BannedPlayerList.Contains(player.Name())) {
			AiAircraft aircraft = actor as AiAircraft;
			if (aircraft != null) {
				aircraft.hitNamed(part.NamedDamageTypes.FuelTank0Fire);
				aircraft.hitNamed(part.NamedDamageTypes.FuelTank1Fire);
			}
		}
	}


}
