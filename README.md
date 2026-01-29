# Vantage Digital Labs - Unity SDK

Official Unity SDK for integrating Vantage AI-powered NPCs into your game.

![Unity](https://img.shields.io/badge/Unity-2021.3+-black?logo=unity)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Requirements

- Unity 2021.3 LTS or newer
- .NET Standard 2.1
- Platforms: Windows, macOS, Linux, iOS, Android, WebGL

## Installation

### Unity Package Manager (Recommended)

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL**
3. Enter: `https://github.com/vantage-digital-labs/unity-sdk.git`

### Manual

Download the `.unitypackage` from [Releases](https://github.com/vantage-digital-labs/unity-sdk/releases) and import into your project.

## Quick Start

### 1. Initialize

Add `VantageManager` to a GameObject in your first scene:

```csharp
using VantageLabs.Core;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private string apiKey = "vk_live_...";
    
    void Awake()
    {
        VantageClient.Init(apiKey, new VantageConfig
        {
            Region = VantageRegion.Tokyo,
            EnableVoice = true,
            Language = "ja"
        });
    }
}
```

### 2. Add NPCBrain to characters

```csharp
using VantageLabs.NPC;

public class Blacksmith : MonoBehaviour
{
    private NPCBrain brain;
    
    void Start()
    {
        brain = gameObject.AddComponent<NPCBrain>();
        brain.Configure(new NPCConfig
        {
            NpcId = "npc_blacksmith_01",
            Personality = "A grumpy but kind blacksmith who respects hard work",
            VoiceId = "voice_gruff_male_01",
            MemoryEnabled = true
        });
    }
    
    public async void OnPlayerInteract(string playerId, string message)
    {
        var response = await brain.Chat(playerId, message, new Context
        {
            { "player_gold", PlayerInventory.Gold },
            { "reputation", ReputationSystem.GetLevel(playerId) },
            { "time_of_day", WorldClock.CurrentTime }
        });
        
        // Display dialogue
        DialogueUI.Show(response.Dialogue);
        
        // Play voice
        if (response.HasAudio)
            AudioManager.PlayStream(response.AudioClip);
        
        // Trigger animation
        if (response.ActionTrigger != null)
            Animator.SetTrigger(response.ActionTrigger);
    }
}
```

### 3. Handle streaming dialogue

```csharp
brain.OnDialogueChunk += (chunk) => {
    DialogueUI.AppendText(chunk.Text);
};

brain.OnEmotionChange += (emotion) => {
    FaceAnimator.SetEmotion(emotion.State);
    // emotion.Sentiment (-1.0 to 1.0)
    // emotion.Trust (0 to 100)
};

brain.OnAudioReady += (clip) => {
    audioSource.clip = clip;
    audioSource.Play();
};
```

## NPC Memory

When `MemoryEnabled = true`, NPCs remember past interactions:

```csharp
// NPC recalls that this player helped them before
var response = await brain.Chat(playerId, "Need any help?");
// "You helped me with that shipment last week. I won't forget that."
```

Memory is persisted server-side and retrieved via vector similarity search. No local storage required.

## Voice Synthesis

Supported languages:
- Japanese (ja) - 8 voice presets
- Korean (ko) - 6 voice presets  
- Chinese (zh) - 6 voice presets
- English (en) - 10 voice presets

```csharp
brain.Configure(new NPCConfig
{
    VoiceId = "voice_elderly_female_02",
    Language = "ja",
    VoicePitch = 1.1f,
    VoiceSpeed = 0.9f
});
```

## Configuration Reference

| Field | Type | Description |
|-------|------|-------------|
| `NpcId` | string | Unique NPC identifier |
| `Personality` | string | System prompt defining character |
| `VoiceId` | string | TTS voice model ID |
| `MemoryEnabled` | bool | Enable episodic memory |
| `Language` | string | Primary language (ja/ko/zh/en) |
| `MaxTokens` | int | Max response length (default: 256) |
| `Temperature` | float | Response creativity (0.1-1.5) |

## Samples

See the `Samples~/` folder for complete example scenes:
- **BasicDialogue** - Simple NPC conversation
- **MemoryDemo** - NPC remembering past interactions
- **VoiceDemo** - Real-time voice synthesis
- **MultiNPC** - Multiple NPCs in one scene

## Support

- Documentation: https://vantage-digital.online/resources/documentation
- API Reference: https://vantage-digital.online/resources/api-reference
- Email: store@vantage-digital.online

## License

MIT License - see [LICENSE](LICENSE)
