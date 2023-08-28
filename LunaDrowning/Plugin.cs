using BepInEx;
using BepInEx.Configuration;
using Photon.Pun;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace LunaDrowning
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // internal
        bool isOn = false;
        public static int InWaterSec = 0;

        // config
        public static ConfigEntry<int> InitialOxygen;
        public static ConfigEntry<bool> KillOnDrown;

        // sounds
        public static AudioSource DrownWarningSource = null;

        static Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream("LunaDrowning.Resources.drowning");
        static AssetBundle bundle = AssetBundle.LoadFromStream(str);
        public static AudioClip DrowningClip = bundle.LoadAsset("drowning") as AudioClip;
        public static AudioClip DrownDieClip = bundle.LoadAsset("drowndead") as AudioClip;

        // air meter
        public static int RemainingOxygen;
        public static GameObject AirMeterObject = null;
        public static Text MeterText = null;


        void Awake() { Utilla.Events.GameInitialized += GameInitialized; }
        private void GameInitialized(object sender, EventArgs e)
        {
            // sounds
            DrownWarningSource = GorillaTagger.Instance.mainCamera.AddComponent<AudioSource>();

            // config binding
            ConfigFile customFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "LunaDrowning.cfg"), true);
            InitialOxygen = customFile.Bind("Mechanics", "Oxygen", 10, "Amount of seconds before you drown.");
            KillOnDrown = customFile.Bind("Mechanics", "KillOnDrown", false, "Close the game when you drown.");

            // air meter
            RemainingOxygen = InitialOxygen.Value;
            AirMeterObject = new GameObject("Air Meter");
            AirMeterObject.transform.SetParent(GorillaTagger.Instance.mainCamera.transform, false);
            AirMeterObject.transform.localPosition = new Vector3(0.1f, -0.05f, 0.5f);
            AirMeterObject.transform.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

            AirMeterObject.AddComponent<Canvas>();

            GameObject textObject = new GameObject("AirText");
            textObject.transform.SetParent(AirMeterObject.transform, false);
            textObject.AddComponent<CanvasRenderer>();
            MeterText = textObject.AddComponent<Text>();
            MeterText.font = GorillaTagger.Instance.offlineVRRig.playerText.font;
            MeterText.text = $"Air: {RemainingOxygen}/{InitialOxygen.Value}";
            AirMeterObject.SetActive(false);

            // run function every second
            InvokeRepeating("DrowningTick", 0f, 1f);
        }

        void OnEnable() { isOn = true; }
        void OnDisable() { isOn = false; }


        // Runs every second after game initializes and everything is set up
        // Invoked by InvokeRepeating
        void DrowningTick()
        {
            if (GorillaLocomotion.Player.Instance != null && isOn && PhotonNetwork.InRoom)
            { 
                if (GorillaLocomotion.Player.Instance.HeadInWater)
                {
                    InWaterSec++;
                    RemainingOxygen--;
                    AirMeterObject.SetActive(true);
                    MeterText.text = $"Air: {RemainingOxygen}/{InitialOxygen.Value}";
                    if (InWaterSec >= InitialOxygen.Value / 2)
                    {
                        if (InWaterSec >= InitialOxygen.Value)
                        {
                            if (!KillOnDrown.Value)
                            {
                                InWaterSec = 0;
                                RemainingOxygen = InitialOxygen.Value;
                                AirMeterObject.SetActive(false);
                                PhotonNetwork.Disconnect();
                                DrownWarningSource.PlayOneShot(DrownDieClip);
                            } else { Application.Quit(); }
                        }
                        MeterText.color = Color.red;
                        DrownWarningSource.PlayOneShot(DrowningClip);
                    }
                } 
                else 
                {
                    MeterText.color = Color.white;
                    AirMeterObject.SetActive(false);
                    InWaterSec = 0;
                    RemainingOxygen = InitialOxygen.Value;
                }
            }
        }
    }
}