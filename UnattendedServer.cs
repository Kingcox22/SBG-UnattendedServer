using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Mirror;
using System.Linq;

namespace UnattendedServer
{
    [BepInPlugin("com.dan.sbg.unattended", "Unattended Server", "1.0")]
    public class UnattendedServerPlugin : BaseUnityPlugin
    {
        private static ConfigEntry<float> _configStartDelay;
        private static ConfigEntry<int> _configHoleCount;

        private float _startTimer = 0f;
        private string _status = "Lobby";

        private void Awake()
        {
            _configStartDelay = Config.Bind("General", "Start Delay", 60f, "Lobby wait time.");
            _configHoleCount = Config.Bind("Match Settings", "Hole Count", 9, "Total holes.");

            var harmony = new Harmony("com.dan.sbg.unattended");
            harmony.PatchAll();
            Logger.LogInfo("Unattended Server v1.0 Initialized.");
        }

        private void Update()
        {
            if (NetworkServer.active && !IsMatchRunning()) 
            { 
                HandleAutoStart(); 
            }
        }

        private bool IsMatchRunning() 
        {
            if (!SingletonNetworkBehaviour<CourseManager>.HasInstance) return false;
            return CourseManager.MatchState != (MatchState)0 && CourseManager.MatchState < MatchState.Ended;
        }

        private void HandleAutoStart()
        {
            int pCount = CourseManager.CountActivePlayers();
            if (pCount > 0)
            {
                _startTimer += Time.deltaTime;
                float remaining = _configStartDelay.Value - _startTimer;
                _status = $"Host: Starting in {Mathf.Ceil(remaining)}s";

                if (_startTimer >= _configStartDelay.Value)
                {
                    _startTimer = 0f;
                    CourseManager.StartCourse();
                }
            }
            else 
            { 
                _startTimer = 0f; 
                _status = "Waiting for players..."; 
            }
        }

        private void OnGUI()
{
            if (!NetworkServer.active) return;

            // Define dimensions
            float boxWidth = 250f;
            float boxHeight = 65f;
            float margin = 15f;

            // Calculate position for top-right
            // Screen.width - boxWidth - margin puts it on the right edge with a 15px gap
            Rect guiRect = new Rect(Screen.width - boxWidth - margin, margin, boxWidth, boxHeight);

            GUI.backgroundColor = new Color(0, 0, 0, 0.85f);
            GUI.Box(guiRect, $"<b><size=14>[SBG SERVER]</size></b>\nSTATUS: {_status}");
        }

        [HarmonyPatch(typeof(CourseManager), "ServerSetCourse")]
        public static class Patch_ServerSetCourse {
            static void Postfix(CourseData course) { if (course != null) Traverse.Create(course).Field("holeCount").SetValue(_configHoleCount.Value); }
        }

        [HarmonyPatch(typeof(CourseManager), "OnStartServer")]
        public static class Patch_OnStartServer {
            static void Postfix(CourseManager __instance) {
                var trav = Traverse.Create(__instance);
                trav.Field("totalHoles").SetValue(_configHoleCount.Value);
                trav.Field("maxHoles").SetValue(_configHoleCount.Value);
            }
        }
    }
}