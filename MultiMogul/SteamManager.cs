using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiMogul.MultiMogul {
    public class SteamManager : MonoBehaviour {
        public static SteamManager Instance;

        private uint appID = 480; // 3846120

        public bool IsInitialized => SteamClient.IsValid;

        private void Awake() {
            if (Instance != null) {
                Debug.LogWarning($"multiple instances of {nameof(SteamManager)} detected. possibly undefined behaviour?");
                return;
            }

            Instance = this;
        }

        private void Start() {
            try {
                if (!SteamClient.IsValid) {
                    SteamClient.Init(this.appID, true);
                    Debug.Log($"Steamworks successfully initialized: {SteamClient.SteamId}");
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"Steamworks failed to initialize: {e.Message}");
            }

            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDisable() {
            SteamClient.Shutdown();
        }
    }
}
