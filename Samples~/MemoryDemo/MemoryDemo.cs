using UnityEngine;
using VantageLabs.Core;

public class MemoryDemo : MonoBehaviour
{
    [SerializeField] private string npcId = "companion-001";

    private VantageClient _client;

    void Start()
    {
        _client = new VantageClient();
        _client.Initialize(System.Environment.GetEnvironmentVariable("VANTAGE_API_KEY"));
    }

    public async void RememberEvent(string eventDescription)
    {
        await _client.Memory.StoreAsync(npcId, eventDescription);
        Debug.Log($"NPC now remembers: {eventDescription}");
    }

    public async void RecallMemory(string query)
    {
        var memories = await _client.Memory.RecallAsync(npcId, query);
        foreach (var memory in memories)
        {
            Debug.Log($"Recalled: {memory.Content} (importance: {memory.Importance})");
        }
    }
}
