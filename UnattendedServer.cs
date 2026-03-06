using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

namespace UnattendedServer
{
    [BepInPlugin("com.kingcox22.sbg.unattended", "Unattended Server", "1.1.0")]
    public class UnattendedServerPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> ConfigStartDelay;
        public static ConfigEntry<int> ConfigHoleCount;
        public static ConfigEntry<bool> ConfigRandomizeHoles;
        
        private float _startTimer = 0f;
        private int _lastAnnouncedTime = -2;
        private string _status = "Lobby";
        private bool _isStarting = false;

        private void Awake()
        {
            ConfigStartDelay = Config.Bind("General", "Countdown Timer", 65f, "Seconds to wait in lobby.");
            ConfigHoleCount = Config.Bind("Match Settings", "Hole Count", 9, "Holes if randomizing.");
            ConfigRandomizeHoles = Config.Bind("Match Settings", "Randomize Holes", true, "If false, uses lobby selection.");

            var harmony = new Harmony("com.kingcox22.sbg.unattended");
            harmony.PatchAll();
        }

        private void Update()
        {
            if (!NetworkServer.active) return;

            if (IsMatchRunning())
            {
                _status = "In Game";
                _startTimer = 0f;
                _lastAnnouncedTime = -2;
                _isStarting = false; 
            }
            else if (_isStarting)
            {
                _status = "Loading...";
            }
            else
            {
                HandleAutoStart();
            }
        }

        private bool IsMatchRunning()
        {
            string sName = SceneManager.GetActiveScene().name;

            // STRICT LOBBY CHECK:
            // The only place the countdown should happen is the "Lobby" scene.            
            if (sName.Equals("Driving Range", StringComparison.OrdinalIgnoreCase))
            {
                return false; 
            }

            // If we are currently loading a scene, we are also considered "In Game" 
            // to prevent the timer from ticking during the transition.
            if (NetworkManager.loadingSceneAsync != null) return true;

            // If it's not the Lobby, it's a match.
            return true;
        }

        private void HandleAutoStart()
        {
            int pCount = CourseManager.CountActivePlayers();
            if (pCount > 0)
            {
                _startTimer += Time.deltaTime;
                float remaining = Mathf.Max(0, ConfigStartDelay.Value - _startTimer);
                int remainingInt = Mathf.CeilToInt(remaining);
                _status = $"Auto-Start: {remainingInt}s";

                if (remainingInt != _lastAnnouncedTime)
                {
                    if (remainingInt == 60 ||remainingInt == 30 ||remainingInt == 10 || (remainingInt <= 5 && remainingInt > 0))
                    {
                        string msg = ConfigRandomizeHoles.Value ? "Randomizing..." : "Starting...";
                        AnnounceChat($"SERVER: {msg} in {remainingInt} seconds");
                    }
                    _lastAnnouncedTime = remainingInt;
                }

                if (_startTimer >= ConfigStartDelay.Value)
                {
                    _isStarting = true;
                    _startTimer = 0f;
                    _lastAnnouncedTime = -1;
                    ExecuteStartSequence();
                }
            }
            else
            {
                _startTimer = 0f;
                _lastAnnouncedTime = -2;
                _status = "Waiting for players...";
            }
        }

        private void ExecuteStartSequence()
        {
            var menu = Resources.FindObjectsOfTypeAll<MatchSetupMenu>().FirstOrDefault();
            if (menu == null) { _isStarting = false; return; }

            if (ConfigRandomizeHoles.Value)
            {
                var allCourses = GameManager.AllCourses;
                if (allCourses != null)
                {
                    var randomHoleIndices = allCourses.allHoles
                        .OrderBy(x => UnityEngine.Random.value)
                        .Select(h => h.GlobalIndex)
                        .Take(ConfigHoleCount.Value).ToArray();

                    GameManager.ClientSetNonStandardCourse(randomHoleIndices, true);
                }
            }

            Traverse.Create(menu).Method("StartOrCancelMatch").GetValue();
        }

        private void AnnounceChat(string message)
        {
            if (TextChatManager.Instance == null || GameManager.LocalPlayerInfo == null) return;
            try {
                var rpcMethod = typeof(TextChatManager).GetMethod("RpcMessage", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                rpcMethod?.Invoke(TextChatManager.Instance, new object[] { message, GameManager.LocalPlayerInfo });
            } catch { }
        }

        private void OnGUI()
        {
            if (!NetworkServer.active) return;
            GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
            GUI.Box(new Rect(Screen.width - 210, 10, 200, 50), $"<b>SBG SERVER</b>\n{_status}");
        }
    }
}