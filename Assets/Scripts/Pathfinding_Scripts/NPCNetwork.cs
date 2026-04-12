using System.Collections.Generic;
using UnityEngine;

public class NPCNetwork : MonoBehaviour
{
    [SerializeField] private Dictionary<NPC, Queue<int>> _agentPaths = new Dictionary<NPC, Queue<int>>();
    [SerializeField] private NPCSpawner spawner;
    public GameObject square;
    private bool done = false;

    private BbGrid _grid;
    // private Queue<int> _path = new Queue<int>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _grid = spawner.grid;
        RegisterAgent(spawner.SpawnAgentOnIndex(54));
        RegisterAgent(spawner.SpawnAgentOnIndex(30));
        // spawner.SpawnAgentOnIndex(54);
    }

    // Update is called once per frame
    void Update()
    {
        // should only ever be run once, ignore warnings of expensive calls
        if (_grid.initialized && !done && spawner.spawned && _agentPaths.Count > 0)
        {
            // agents[0].SetMoveTarget(grid.squares[9].transform.position);
            // RegisterAgent(agents[0]);
            foreach (KeyValuePair<NPC, Queue<int>> agent in _agentPaths)
            {
                MoveAgentToTarget(agent.Key);
            }
            done = true;
        }
    }

    public void RegisterAgent(NPC agent)
    {
        if (!agent) return;
        _agentPaths.Add(agent, new Queue<int>());
        agent.Arrived += OnAgentArrived;
    }

    public void UnregisterAgent(NPC agent)
    {
        agent.Arrived -= OnAgentArrived;
        _agentPaths.Remove(agent);
    }

    private void OnAgentArrived(NPC agent)
    {
        // temp code
        // if (agent != agents[0])
        // {
        //     return;
        // }
        // has been moving already
        // not a deeper clone, so should be a reference
        Queue<int> path = _agentPaths[agent];
        if (path.Count != 0)
        {
            agent.SetMoveTarget(_grid.squares[path.Dequeue()].transform.position);
        }
        
    }
    
    private void MoveAgentToTarget(NPC agent)
    {
        // not a deeper clone, so should be a reference
        Queue<int> path = _agentPaths[agent];
        // get index that agent is at
        int agentToGridIndex = _grid.WorldToIndex(agent.transform.position);
        if (!_grid.CreatePathToTarget(path, agentToGridIndex))
        {
            // PrintPath(path);
            return;
        }
        // PrintPath(path);
        // start moving the agent
        agent.SetMoveTarget(_grid.squares[path.Dequeue()].transform.position);
    }

    private void PrintPath(Queue<int> path)
    {
        Queue<int> copy = new Queue<int>(path);
        string s = "";
        for (int i = 0; i < path.Count-1; ++i)
        {
            s += copy.Dequeue() + ", ";
        }

        s += copy.Dequeue();
        
        Debug.Log(s);
    }
    
}
