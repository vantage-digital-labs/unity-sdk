using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace VantageLabs.Core
{
    public enum VantageRegion
    {
        Tokyo,
        Seoul,
        Shanghai,
        USWest
    }

    [Serializable]
    public class VantageConfig
    {
        public VantageRegion Region = VantageRegion.Tokyo;
        public bool EnableVoice = true;
        public string Language = "ja";
        public int TimeoutMs = 10000;
        public int MaxRetries = 3;
    }

    public static class VantageClient
    {
        private static string _apiKey;
        private static VantageConfig _config;
        private static VantageWebSocket _ws;
        private static bool _initialized;

        private static readonly string[] RegionEndpoints = {
            "https://api-tokyo.vantage-digital.online",
            "https://api-seoul.vantage-digital.online",
            "https://api-cn.vantage-digital.online",
            "https://api-us.vantage-digital.online"
        };

        public static event Action OnConnected;
        public static event Action<string> OnError;

        public static void Init(string apiKey, VantageConfig config = null)
        {
            _apiKey = apiKey;
            _config = config ?? new VantageConfig();
            _initialized = true;

            _ws = new VantageWebSocket(GetEndpoint(), _apiKey);
            _ws.OnOpen += () => OnConnected?.Invoke();
            _ws.OnError += (err) => OnError?.Invoke(err);
            _ws.Connect();

            Debug.Log($"[Vantage] Initialized - Region: {_config.Region}");
        }

        public static string GetEndpoint()
        {
            return RegionEndpoints[(int)_config.Region];
        }

        public static string ApiKey => _apiKey;
        public static VantageConfig Config => _config;
        public static bool IsConnected => _ws?.IsConnected ?? false;

        internal static VantageWebSocket WebSocket => _ws;

        internal static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("VantageClient.Init() must be called before use.");
        }
    }
}
