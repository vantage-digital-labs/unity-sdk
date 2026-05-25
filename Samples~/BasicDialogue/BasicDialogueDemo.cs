using UnityEngine;
using VantageLabs.Core;

public class BasicDialogueDemo : MonoBehaviour
{
    [SerializeField] private string npcId = "merchant-001";
    [SerializeField] private string playerMessage = "What do you have for sale?";

    private VantageClient _client;

    void Start()
    {
        _client = new VantageClient();
        _client.Initialize(System.Environment.GetEnvironmentVariable("VANTAGE_API_KEY"));
    }

    public async void TalkToNPC()
    {
        var response = await _client.Dialogue.GenerateAsync(npcId, playerMessage);
        Debug.Log($"NPC: {response.Text}");
        Debug.Log($"Emotion: {response.Emotion}");
    }
}
