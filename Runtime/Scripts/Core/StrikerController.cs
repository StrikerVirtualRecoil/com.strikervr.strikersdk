using StrikerLink.Shared.Client;
using StrikerLink.Unity.Runtime.HapticEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Logger = StrikerLink.Shared.Utils.Logger;

namespace StrikerLink.Unity.Runtime.Core
{
    [ExecuteAlways] // So we can kill the client in the editor
    public class StrikerController : MonoBehaviour
    {
        public bool autoConnect = true;
        public bool overrideConnection = false;
        public string overrideIp = "127.0.0.1";
        public int overridePort = 5060;
        public Logger.LogLevel logLevel = Logger.LogLevel.WARNING;

        [Header("Haptics")]
        public string libraryPrefix = "myStrikerApp_";
        public List<HapticLibraryAsset> hapticLibraries;

        [Header("Events")]
        public UnityEvent OnClientConnected;
        public UnityEvent OnClientDisconnected;
        public UnityEvent OnConnectionFailed;

        StrikerClient strikerClient;

        static StrikerController _controller;

        bool queueConnectionEvent;
        bool queueDisconnectionEvent;
        bool queueFailedEvent;

        bool wasConnected = false;

        public static StrikerController Controller
        {
            get
            {
                if (_controller == null)
                    _controller = FindObjectOfType<StrikerController>();

                return _controller;
            }
        }

        public static bool IsConnected
        {
            get
            {
                return _controller != null && _controller.strikerClient != null && _controller.strikerClient.IsConnected;
            }
        }

        private void Awake()
        {
            if(Application.isPlaying && autoConnect)
            {
                Connect();
            }
        }

        public StrikerClient GetClient()
        {
            return strikerClient;
        }

        public void Connect()
        {
            strikerClient = new StrikerClient(new UnityConsoleHandler());

            strikerClient.OnClientConnected += StrikerClient_OnClientConnected;
            strikerClient.OnClientDisconnected += StrikerClient_OnClientDisconnected;

            if (overrideConnection)
                strikerClient.Connect(overrideIp, overridePort);
            else
                strikerClient.Connect();
        }

        private void StrikerClient_OnClientDisconnected(object sender, System.EventArgs e)
        {
            if (!wasConnected)
                queueFailedEvent = true;
            else
            {
                Logger.Info("[CONTROLLER] StrikerClient has disconnected");
                queueDisconnectionEvent = true;
            }

            wasConnected = false;
        }

        private void StrikerClient_OnClientConnected(object sender, System.EventArgs e)
        {
            Logger.Info("[CONTROLLER] StrikerClient has connected");

            wasConnected = true;
            queueConnectionEvent = true;
        }

        public void UpdateHapticLibrary()
        {
            /*if(libraryOverride == null)
            {
                if (hapticLibrary == null)
                    return;

                Logger.Info("[CONTROLLER] Sending haptic library to StrikerLink Runtime");

                strikerClient.UpdateAppLibrary(libraryPrefix, hapticLibrary.text);
            } else
            {
                strikerClient.UpdateAppLibrary(libraryPrefix, libraryOverride);
            }*/

            if (hapticLibraries == null)
                return;

            foreach(HapticLibraryAsset asset in hapticLibraries)
            {
                if (asset == null)
                    continue;

                Logger.Info("[CONTROLLER] Sending haptic library '" + libraryPrefix + asset.libraryKey + "' to StrikerLink Runtime");
                strikerClient.UpdateAppLibrary(libraryPrefix + asset.libraryKey, asset.json);
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(!Application.isPlaying && strikerClient != null)
            {
                StrikerClient.Disconnect();
                strikerClient = null;
            }

            if(queueConnectionEvent)
            {
                queueConnectionEvent = false;
                HandleConnectionInMainThread();
            }

            if(queueDisconnectionEvent)
            {
                queueDisconnectionEvent = false;
                HandleDisconnectionInMainThread();
            }

            if(queueFailedEvent)
            {
                queueFailedEvent = false;
                HandleFailedConnectionInMainThread();
            }
        }

        void HandleConnectionInMainThread()
        {
            UpdateHapticLibrary();

            OnClientConnected.Invoke();
        }

        void HandleDisconnectionInMainThread()
        {
            OnClientDisconnected.Invoke();
        }

        void HandleFailedConnectionInMainThread()
        {
            OnConnectionFailed.Invoke();
        }

        private void OnDestroy()
        {
            StrikerClient.Disconnect();
        }
    }

    class UnityConsoleHandler : Logger.IConsoleHandler
    {
        public void Log(string msg, Logger.LogLevel level = Logger.LogLevel.INFO)
        {
            switch (level)
            {
                case Logger.LogLevel.TRACE:
                    Debug.Log(msg);
                    break;
                case Logger.LogLevel.DEBUG:
                    Debug.Log(msg);
                    break;
                case Logger.LogLevel.INFO:
                    Debug.Log(msg);
                    break;
                case Logger.LogLevel.WARNING:
                    Debug.LogWarning(msg);
                    break;
                case Logger.LogLevel.ERROR:
                    Debug.LogError(msg);
                    break;
                case Logger.LogLevel.CRITICAL:
                    Debug.LogError(msg);
                    break;
                case Logger.LogLevel.OFF:
                    break;
            }
        }
    }
}