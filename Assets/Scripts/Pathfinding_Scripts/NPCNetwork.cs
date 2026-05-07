using System.Collections.Generic;
using UnityEngine;

public class NPCNetwork : MonoBehaviour
{
    [SerializeField] private NPCSpawner spawner;
    public GameObject square;

    private BbGrid _grid;
    private Bitboard _occupancy;

    // Path queue per agent.
    private readonly Dictionary<NPC, Queue<int>> _agentPaths = new Dictionary<NPC, Queue<int>>();
    // Which grid index each agent currently occupies on the bitboard.
    private readonly Dictionary<NPC, int> _agentOccupancyIdx = new Dictionary<NPC, int>();
    // Where a jumper is heading — occupancy only moves to this on arrival.
    private readonly Dictionary<NPC, int> _jumperPendingDest = new Dictionary<NPC, int>();

    void Start()
    {
        _grid = spawner.grid;
        RegisterAgent(spawner.SpawnAgentOnIndex(54, NPCType.Walker));
        RegisterAgent(spawner.SpawnAgentOnIndex(30, NPCType.Jumper));
    }

    void Update()
    {
        if (!_grid.initialized || !spawner.spawned || _agentPaths.Count == 0)
            return;

        foreach (KeyValuePair<NPC, Queue<int>> kvp in _agentPaths)
        {
            NPC npc = kvp.Key;
            if (npc.HasTarget()) continue;

            int fromIdx = _agentOccupancyIdx[npc];
            Queue<int> path = kvp.Value;

            if (npc is Jumper jumper)
                TryMoveJumper(jumper, path, fromIdx);
            else
                TryMoveWalker(npc, path, fromIdx);
        }
    }

    public void RegisterAgent(NPC agent)
    {
        if (!agent) return;
        int startIdx = _grid.WorldToIndex(agent.transform.position);
        _agentPaths[agent] = new Queue<int>();
        _agentOccupancyIdx[agent] = startIdx;
        _occupancy.TryLock(startIdx);
        agent.Arrived += OnAgentArrived;
    }

    public void UnregisterAgent(NPC agent)
    {
        agent.Arrived -= OnAgentArrived;
        if (_agentOccupancyIdx.TryGetValue(agent, out int idx))
            _occupancy.Unlock(idx);
        _agentPaths.Remove(agent);
        _agentOccupancyIdx.Remove(agent);
        _jumperPendingDest.Remove(agent);
    }

    private void OnAgentArrived(NPC agent)
    {
        if (agent is Jumper)
        {
            // Jumper held its source cell occupied during the jump; move occupancy to destination now.
            if (_jumperPendingDest.TryGetValue(agent, out int destIdx))
            {
                // Destination was locked on jump start; release the source now.
                _occupancy.Unlock(_agentOccupancyIdx[agent]);
                _agentOccupancyIdx[agent] = destIdx;
                _jumperPendingDest.Remove(agent);
            }
            agent.RecordLanding();
            return;
        }
        // Walkers are driven entirely by Update; arrival just unblocks the next poll.
    }

    private void TryMoveWalker(NPC npc, Queue<int> path, int fromIdx)
    {
        if (path.Count == 0)
            if (!_grid.CreatePathToTarget(path, fromIdx)) return;
        if (path.Count == 0) return;

        int nextIdx = path.Peek();

        if (!_occupancy.TryTransfer(fromIdx, nextIdx)) return; // cell taken — wait

        path.Dequeue();
        _agentOccupancyIdx[npc] = nextIdx;
        npc.SetMoveTarget(_grid.squares[nextIdx].transform.position);
    }

    private void TryMoveJumper(Jumper jumper, Queue<int> path, int fromIdx)
    {
        int steps = jumper.ConsumeMovementSteps();
        if (steps == 0) return;

        if (path.Count == 0)
            if (!_grid.CreatePathToTarget(path, fromIdx)) return;
        if (path.Count == 0) return;

        // Drain up to `steps` cells, then pick the farthest with clear LOS and no occupant.
        var buffer = new List<int>(steps);
        for (int i = 0; i < steps && path.Count > 0; i++)
            buffer.Add(path.Dequeue());

        if (buffer.Count == 0) return;

        int targetIdx = -1;
        for (int i = buffer.Count - 1; i >= 0; i--)
        {
            if (HasLineOfSight(fromIdx, buffer[i]) && !_occupancy.IsSet(buffer[i]))
            {
                targetIdx = buffer[i];
                break;
            }
        }

        if (targetIdx < 0) return; // all candidates blocked — skip this jump

        // Lock destination before releasing source — jumper occupies both cells mid-jump.
        if (!_occupancy.TryLock(targetIdx)) return;
        _jumperPendingDest[jumper] = targetIdx;
        jumper.SetMoveTarget(_grid.squares[targetIdx].transform.position);
    }

    /// <summary>
    /// Bresenham's line on the 8x8 grid. Returns true if every cell between
    /// fromIdx and toIdx is open on the bitboard.
    /// </summary>
    private bool HasLineOfSight(int fromIdx, int toIdx)
    {
        int x0 = fromIdx % 8, y0 = fromIdx / 8;
        int x1 = toIdx   % 8, y1 = toIdx   / 8;

        int dx  = Mathf.Abs(x1 - x0);
        int dy  = Mathf.Abs(y1 - y0);
        int sx  = x0 < x1 ? 1 : -1;
        int sy  = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int x = x0, y = y0;
        while (true)
        {
            if (!_grid.IsIndexOpen(y * 8 + x)) return false;
            if (x == x1 && y == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 <  dx) { err += dx; y += sy; }
        }
        return true;
    }

    private void PrintPath(Queue<int> path)
    {
        Queue<int> copy = new Queue<int>(path);
        string s = "";
        for (int i = 0; i < path.Count - 1; ++i)
            s += copy.Dequeue() + ", ";
        s += copy.Dequeue();
        Debug.Log(s);
    }
}
