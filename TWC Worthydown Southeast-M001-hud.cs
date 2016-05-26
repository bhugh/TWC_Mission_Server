using System;
using maddox.game;
using maddox.game.world;

/****************************
 * http://www.checksix-forums.com/viewtopic.php?f=412&t=177564&p=1297757
 * HUD/ a/c parameters sample
 *****************************/
public class Mission : AMission
{
    //User Control
    bool do_get = true;
    bool do_hud = false;
    bool chronoTestBoost1 = false;
    bool TestBoost1Advise = false;
    bool TestWaterTempAdvise = false;
    bool TestOilTempAdvise = false;
    bool essai = false;
    bool intheair = false;

    //Define and Init
    AiAircraft cur_Plane;
    double cur_Time = 0.0;
    double cur_Time2 = 0.0;
    double chrono1 = 0.0;
    double takeOffTime = 0.0;
    //bool flag_do_once = false;
    double A_Undercarriage = 0.0;
    double C_WaterRadiator;
    double I_VelocityIAS = 0.0;
    double I_Altitude = 0.0;
    double I_Variometer = 0.0;
    double I_Peilzeiger = 0.0;
    double I_MagneticCompass = 0.0;
    double I_Slip = 0.0;
    double I_EngineRPM = 0.0;
    double I_EngineManPress;
    double I_EngineBoostPress;
    double I_EngineWatTemp;
    double I_EngineRadTemp;
    double I_EngineOilTemp;
    double I_EngineTemperature;
    double Z_Overload = 0.0;
    double Z_AltitudeAGL = 0.0;
    double Z_AltitudeMSL = 0.0;
    double Z_VelocityIAS = 0.0;
    double Z_VelocityTAS = 0.0;
    double Z_VelocityMach = 0.0;
    double Z_AmbientAirTemperature = 0.0;
    double C_Pitch;
    double C_Mix;
    double timerBoost = 0;
    double tempoInfoWaterTemp = 0;
    double tempoInfoOilTemp = 0;
    string str_hud = "";

    public override void OnAircraftTookOff(int missionNumber, string shortName, AiAircraft aircraft)
    {
        intheair = true;
        takeOffTime = Time.current();
        //GamePlay.gpHUDLogCenter("Took Off");
    }


    public override void OnAircraftLanded(int missionNumber, string shortName, AiAircraft aircraft)
    {
        //intheair = false;
        GamePlay.gpHUDLogCenter("On ground");
    }



    //Boucle Principale
    public override void OnTickGame()
    {
        //On initialise le compteur
        base.OnTickGame();

        //Fait a interval regulier
        if (Time.tickCounter() % 30 == 1)
        {
            //Avion jouer
            cur_Plane = GamePlay.gpPlayer().Place() as AiAircraft;

            if (cur_Plane != null)
            {
                //On enregistre les données
                if (do_get)
                {
                    cur_Time = Time.current();
                    cur_Time2 = Time.current() + 10;
                    I_MagneticCompass = cur_Plane.getParameter(part.ParameterTypes.I_MagneticCompass, -1);
                    I_Altitude = cur_Plane.getParameter(part.ParameterTypes.I_Altitude, -1);
                    I_VelocityIAS = cur_Plane.getParameter(part.ParameterTypes.I_VelocityIAS, -1);
                    I_Variometer = cur_Plane.getParameter(part.ParameterTypes.I_Variometer, -1);
                    I_Peilzeiger = cur_Plane.getParameter(part.ParameterTypes.I_Peilzeiger, -1);
                    I_Slip = cur_Plane.getParameter(part.ParameterTypes.I_Slip, -1);
                    Z_Overload = cur_Plane.getParameter(part.ParameterTypes.Z_Overload, 2);
                    Z_AltitudeAGL = cur_Plane.getParameter(part.ParameterTypes.Z_AltitudeAGL, -1);
                    Z_AltitudeMSL = cur_Plane.getParameter(part.ParameterTypes.Z_AltitudeMSL, -1);
                    Z_VelocityIAS = cur_Plane.getParameter(part.ParameterTypes.Z_VelocityIAS, -1);
                    Z_VelocityTAS = cur_Plane.getParameter(part.ParameterTypes.Z_VelocityTAS, -1);
                    Z_VelocityMach = cur_Plane.getParameter(part.ParameterTypes.Z_VelocityMach, -1);
                    Z_AmbientAirTemperature = cur_Plane.getParameter(part.ParameterTypes.Z_AmbientAirTemperature, -1);
                    I_EngineRPM = cur_Plane.getParameter(part.ParameterTypes.I_EngineRPM, 0);
                    I_EngineManPress = cur_Plane.getParameter(part.ParameterTypes.I_EngineManPress, 0);
                    I_EngineBoostPress = cur_Plane.getParameter(part.ParameterTypes.I_EngineBoostPress, 0);
                    I_EngineWatTemp = cur_Plane.getParameter(part.ParameterTypes.I_EngineWatTemp, 0);
                    I_EngineRadTemp = cur_Plane.getParameter(part.ParameterTypes.I_EngineRadTemp, 0);
                    I_EngineOilTemp = cur_Plane.getParameter(part.ParameterTypes.I_EngineOilTemp, 0);
                    I_EngineTemperature = cur_Plane.getParameter(part.ParameterTypes.I_EngineTemperature, 0);
                    C_Pitch = cur_Plane.getParameter(part.ParameterTypes.C_Pitch, -1);
                    C_Mix = cur_Plane.getParameter(part.ParameterTypes.C_Mix, -1);
                    //A_Undercarriage         = cur_Plane.getParameter(part.ParameterTypes.A_Undercarriage, -1);
                    C_WaterRadiator = cur_Plane.getParameter(part.ParameterTypes.C_WaterRadiator, -1);

                }

                //Affichage HUD
                if (do_hud)
                {
                    str_hud = "    EngineRPM: " + I_EngineRPM.ToString("0.00") +
                              "    EngineManPress: " + I_EngineManPress.ToString("0.00") +
                              "    IAS: " + I_VelocityIAS.ToString("0.00") +
                              "    EngineWTemp: " + I_EngineWatTemp.ToString("0.00") +
                              "    EngineOTemp: " + I_EngineOilTemp.ToString("0.00") +
                              "    Pitch: " + C_Pitch.ToString("0.00") +
                              "    Time: " + cur_Time.ToString("0.00") +
                              "    Mix: " + C_Mix.ToString("0.00");
                    //  "    G: " + Z_Overload.ToString("0.00");
                    GamePlay.gpHUDLogCenter(str_hud);
                }

                //GamePlay.gpHUDLogCenter(cur_Time.ToString("0.00") + "        " + cur_Time2.ToString("0.00"));
                //GamePlay.gpHUDLogCenter(Z_Overload.ToString("0.00"));
                //GamePlay.gpHUDLogCenter(C_WaterRadiator.ToString());
                //GamePlay.gpHUDLogCenter(I_VelocityIAS.ToString("0.00"));


                if (I_EngineManPress > 8)
                {
                    if (!chronoTestBoost1)
                    {
                        chronoTestBoost1 = true;
                        //temps limite = timede début de boost > 9 + 3*60 = 180 secondes (3 minutes)
                        timerBoost = Time.current() + 180;
                    }

                    if (Time.current() > timerBoost && chronoTestBoost1 && !TestBoost1Advise)
                    {
                        GamePlay.gpHUDLogCenter("Trop longue utilisation du Boost a fond. Reduisez le Boost");
                        TestBoost1Advise = true;
                    }
                }

                if (chronoTestBoost1 && I_EngineManPress < 5)
                {
                    chronoTestBoost1 = false;
                    TestBoost1Advise = false;
                }


                //
                // On Test les G
                //

                if (Z_Overload > 4.5)
                {
                    GamePlay.gpHUDLogCenter("Soyez plus souple. La structure souffre sous cette charge !!");
                }


                //
                // On Test si on a atteint la vitesse de rotation au décollage
                //

                if (!intheair && I_VelocityIAS > 95 && Z_AltitudeAGL < 10)
                {
                    GamePlay.gpHUDLogCenter("Rotation");
                }


                //
                // On Test la temp eau
                //

                if (I_EngineWatTemp > 110 && !TestWaterTempAdvise)
                {
                    if (C_WaterRadiator > 0.99)
                    {
                        GamePlay.gpHUDLogCenter("Temp eau haute. Reduire Boost");
                        TestWaterTempAdvise = true;
                        tempoInfoWaterTemp = Time.current() + 30;
                    }
                    else
                    {
                        GamePlay.gpHUDLogCenter("Temp eau haute. Ouvrir en grand le radiateur");
                        TestWaterTempAdvise = true;
                        tempoInfoWaterTemp = Time.current() + 30;
                    }
                }

                if (tempoInfoWaterTemp < Time.current())
                {
                    TestWaterTempAdvise = false;
                }



                //
                // On Test la temp huile
                //

                if (I_EngineOilTemp > 96 && !TestOilTempAdvise && intheair)
                {
                    GamePlay.gpHUDLogCenter("Temp Huile haute. Reduire RPM");
                    TestOilTempAdvise = true;
                    tempoInfoOilTemp = Time.current() + 30;
                }

                if (tempoInfoOilTemp < Time.current())
                {
                    TestOilTempAdvise = false;
                }


                //
                // On Test la vitesse de décrochage
                //

                if (intheair && I_VelocityIAS < 79)
                {
                    if (I_EngineManPress > 8.5)
                    {
                        GamePlay.gpHUDLogCenter("La vitesse est a la limite basse. Poussez un peu sur le manche pour reprendre de la vitesse");
                        TestWaterTempAdvise = true;
                    }
                    else
                    {
                        GamePlay.gpHUDLogCenter("La vitesse est a la limite basse. Ajouté du boost et lachez un peu la pression du manche");
                        TestWaterTempAdvise = true;
                    }

                }


            }
        }
    }
}