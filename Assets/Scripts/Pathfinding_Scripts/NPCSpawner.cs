using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField] private GameObject agent;
    [SerializeField] private int nToSpawn;
    public BbGrid grid;

    public bool spawned;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // init vals
        spawned = false;
        // spawn npcs
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnAgents(int numToSpawn)
    {
        // for loop from 0 to numToSpawn
            // get random number between 0-64
            // if valid spot, spawn an agent at position of square at index
            // send agent into network
                // we can do this with signals
    }

    public NPC SpawnAgentOnIndex(int idx)
    {
        if (!grid.IsIndexOpen(idx))
        {
            Debug.Log("tried to spawn in an invalid cell!");
            return null;
        }
        GameObject npc = Instantiate(agent);
        npc.transform.position = grid.squares[idx].transform.position;
        spawned = true;
        return npc.GetComponent<NPC>();
    }
    
}
