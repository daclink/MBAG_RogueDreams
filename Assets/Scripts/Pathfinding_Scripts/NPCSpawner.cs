using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Serializable]
    public struct NPCPrefabEntry
    {
        public NPCType type;
        public GameObject prefab;
    }

    [SerializeField] private NPCPrefabEntry[] _npcPrefabs;
    public BbGrid grid;
    public bool spawned;

    private Dictionary<NPCType, GameObject> _prefabLookup;

    void Awake()
    {
        BuildLookup();
    }

    void Start()
    {
        spawned = false;
    }

    public void SpawnAgents(int numToSpawn)
    {
        // for loop from 0 to numToSpawn
            // get random number between 0-64
            // if valid spot, spawn an agent at position of square at index
            // send agent into network via signals
    }

    public NPC SpawnAgentOnIndex(int idx, NPCType type)
    {
        if (!grid.IsIndexOpen(idx))
        {
            Debug.LogWarning($"NPCSpawner: index {idx} is not open.");
            return null;
        }

        if (!_prefabLookup.TryGetValue(type, out GameObject prefab))
        {
            Debug.LogWarning($"NPCSpawner: no prefab registered for NPCType.{type}.");
            return null;
        }

        GameObject go = Instantiate(prefab, grid.squares[idx].transform.position, Quaternion.identity);
        spawned = true;
        return go.GetComponent<NPC>();
    }

    private void BuildLookup()
    {
        _prefabLookup = new Dictionary<NPCType, GameObject>(_npcPrefabs.Length);

        foreach (NPCPrefabEntry entry in _npcPrefabs)
        {
            if (_prefabLookup.ContainsKey(entry.type))
            {
                Debug.LogWarning($"NPCSpawner: duplicate entry for NPCType.{entry.type} — ignoring.");
                continue;
            }
            _prefabLookup[entry.type] = entry.prefab;
        }

        // Warn at startup for any NPCType that has no registered prefab.
        foreach (NPCType type in Enum.GetValues(typeof(NPCType)))
        {
            if (!_prefabLookup.ContainsKey(type))
                Debug.LogWarning($"NPCSpawner: NPCType.{type} has no prefab registered.");
        }
    }
}
