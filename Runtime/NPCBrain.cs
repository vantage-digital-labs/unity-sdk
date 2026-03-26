using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VantageLabs.NPC
{
    [Serializable]
    public class NPCConfig
    {
        public string NpcId;
        public string Personality;
        public string VoiceId;
        public bool MemoryEnabled = true;
        public string Language = "ja";
        public int MaxTokens = 256;
        public float Temperature = 0.8f;
        public float VoicePitch = 1.0f;
        public float VoiceSpeed = 1.0f;
    }

    [Serializable]
    public class ChatResponse
    {
        public string Dialogue;
        public string Emotion;
        public float Sentiment;
        public int Trust;
        public string AudioUrl;
        public AudioClip AudioClip;
        public string ActionTrigger;
        public bool HasAudio => !string.IsNullOrEmpty(AudioUrl);
        public float LatencyMs;
    }

    [Serializable]
    public class EmotionState
    {
        public string State;
        public float Sentiment;
        public int Trust;
        public string AnimTrigger;
    }

    public class Context : Dictionary<string, object> { }

    public class NPCBrain : MonoBehaviour
    {
        private NPCConfig _config;
        private int _exchangeCount;

        public event Action<DialogueChunk> OnDialogueChunk;
        public event Action<EmotionState> OnEmotionChange;
        public event Action<AudioClip> OnAudioReady;
        public event Action<string> OnError;

        public NPCConfig Config => _config;
        public int ExchangeCount => _exchangeCount;

        public void Configure(NPCConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "NPCConfig cannot be null");
            if (string.IsNullOrEmpty(config.NpcId))
                throw new ArgumentException("NPCConfig.NpcId must not be empty", nameof(config));

            _config = config;
            Core.VantageClient.EnsureInitialized();
        }

        public async Task<ChatResponse> Chat(string playerId, string message, Context context = null)
        {
            Core.VantageClient.EnsureInitialized();

            var startTime = Time.realtimeSinceStartup;

            var request = new Dictionary<string, object>
            {
                ["npcId"] = _config.NpcId,
                ["playerId"] = playerId,
                ["message"] = message,
                ["personality"] = _config.Personality,
                ["language"] = _config.Language,
                ["maxTokens"] = _config.MaxTokens,
                ["temperature"] = _config.Temperature,
                ["memoryEnabled"] = _config.MemoryEnabled,
                ["voiceEnabled"] = !string.IsNullOrEmpty(_config.VoiceId),
                ["voiceId"] = _config.VoiceId
            };

            if (context != null)
                request["context"] = context;

            var response = await SendRequest("/v2/npc/chat", request);
            _exchangeCount++;

            response.LatencyMs = (Time.realtimeSinceStartup - startTime) * 1000f;

            if (response.HasAudio)
            {
                response.AudioClip = await LoadAudioStream(response.AudioUrl);
                OnAudioReady?.Invoke(response.AudioClip);
            }

            var emotionState = new EmotionState
            {
                State = response.Emotion,
                Sentiment = response.Sentiment,
                Trust = response.Trust,
                AnimTrigger = response.ActionTrigger
            };
            OnEmotionChange?.Invoke(emotionState);

            return response;
        }

        private async Task<ChatResponse> SendRequest(string path, Dictionary<string, object> body)
        {
            var endpoint = Core.VantageClient.GetEndpoint() + path;
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            using var www = UnityEngine.Networking.UnityWebRequest.Post(endpoint, json, "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {Core.VantageClient.ApiKey}");
            www.SetRequestHeader("X-SDK-Version", "2.4.0-unity");
            www.timeout = Core.VantageClient.Config.TimeoutMs / 1000;

            var op = www.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                throw new Exception($"[Vantage] Request failed: {www.error}");

            return Newtonsoft.Json.JsonConvert.DeserializeObject<ChatResponse>(www.downloadHandler.text);
        }

        private async Task<AudioClip> LoadAudioStream(string url)
        {
            using var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(
                url, AudioType.OGGVORBIS);
            www.SetRequestHeader("Authorization", $"Bearer {Core.VantageClient.ApiKey}");
            // Allow extra time for voice data — audio files can be several MB
            www.timeout = Core.VantageClient.Config.TimeoutMs / 1000 * 3;

            var op = www.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                OnError?.Invoke($"[Vantage] Audio stream failed: {www.error}");
                return null;
            }

            return UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
        }
    }

    [Serializable]
    public class DialogueChunk
    {
        public string Text;
        public bool IsFinal;
    }
}
