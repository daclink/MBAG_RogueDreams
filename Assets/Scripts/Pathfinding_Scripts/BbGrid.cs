using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BbGrid : MonoBehaviour
{
    [FormerlySerializedAs("targetIndex")] public int targetIdx;
    public GameObject prefab;

    public Transform origin;
    
    private Bitboard _bb;

    [NonSerialized] public GameObject[] squares;
    [NonSerialized] public int[] distances = new int[64];

    [NonSerialized] public bool initialized = false;
    
    private const ulong 
        northMask   = 0x00FFFFFFFFFFFFFFUL,
        southMask   = 0xFFFFFFFFFFFFFF00UL,
        westMask    = 0x7F7F7F7F7F7F7F7FUL,
        eastMask    = 0xFEFEFEFEFEFEFEFEUL;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*
        '0' = obstacle, '1' = open
        0 0 0 0 0 0 0 0
        0 1 1 1 1 1 1 0
        0 1 0 0 0 1 1 0
        0 1 1 1 0 1 1 0
        0 1 0 0 0 1 1 0
        0 1 1 1 0 0 0 0
        1 1 0 1 1 1 1 0
        0 0 0 0 0 0 0 0
        */
        // 0000000001111110010001100111111001111010011000101111111000000000
        
        _bb.setData(0b0000000001111110010001100111011001000110011101001101111000000000);

        squares = new GameObject[64];
        const int defaultValue = -1;
        Array.Fill(distances, defaultValue);
        InstantiateSquares();
        
        FloodFill(targetIdx);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool IsIndexOpen(int idx)
    {
        return (_bb.getData() & (1UL << idx)) > 0;
    }
    
    public int WorldToIndex(Vector2 worldPos)
    {
        // ideally, worldPos is exactly the same as a square pos, let's assume that first
        // convert formula for position to translate back into index
        
        // save bounds for convenience
        Vector2 bounds = prefab.GetComponent<SpriteRenderer>().bounds.size;
        // who says you don't use algebra after high school?
        int y = (int)((worldPos.y - origin.position.y - ((bounds.y) / 2)) / bounds.y),
            x = (int)(((worldPos.x - origin.position.x - ((bounds.x) / 2)) / bounds.x) - 7)*-1;
        
        // formula for index used from before
        return y * 8 + x;
    }
    private void InstantiateSquares()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int index = y * 8 + x;
                
                // if there is a 1 here, open
                Vector2 pos = new Vector2(
                    origin.position.x + prefab.GetComponent<SpriteRenderer>().bounds.size.x / 2 +
                    prefab.GetComponent<SpriteRenderer>().bounds.size.x * (7 - x),
                    origin.position.y + prefab.GetComponent<SpriteRenderer>().bounds.size.y / 2 +
                    prefab.GetComponent<SpriteRenderer>().bounds.size.y * y);
                
                GameObject squarePrefab = Instantiate(prefab);
                squarePrefab.transform.position = new Vector3(pos.x, pos.y);
                
                // keeping it with branching behavior because identifier is not concrete yet
                if ((_bb.getData() & (1UL << index)) > 0)
                {
                    // squarePrefab.GetComponent<SpriteRenderer>().color = Color.green;
                }
                else // else, obstacle
                {
                    squarePrefab.GetComponent<SpriteRenderer>().color = Color.gray4;
                }

                squares[index] = squarePrefab;
            }
        }

        initialized = true;
    }

    private void FloodFill(int srcIndex)
    {
        // if the square at this index is obstacle, return/fail
        
        // check in 4 directions, BFS, init distance, queue
        // checking in 4 directions by index in a bitboard
            // bitboard & (1UL << index)
        // if square at each index is open, add into queue
        // per loop in while, increase distance
        
        // how to check if square at index is obstacle, bitboard at index 0 or 1
        if ((_bb.getData() & (1UL << srcIndex)) == 0) // if square at index is obstacle
        {
            Debug.Log("Tried to flood fill in a wall!");
            return;
        }

        // how to check all 4 directions with bb?
        // shifting, << 8 and 1, >> 8 and 1
        
        // with index = y*width + x, x = index%width and y = index/width

        // Bitboard print = new Bitboard((_bb.getData() & ~eastMask));
        // print.printBitboard();

        int distance = 0;
        Queue<int> q = new Queue<int>();
        q.Enqueue(srcIndex);
        int[] visited = new int[64];
        const int defaultValue = -1;
        Array.Fill(visited, defaultValue);
        while (q.Count != 0)
        {
            distance++;
            int size = q.Count;
            for (int i = 0; i < size; ++i)
            {
                int currIndex = q.Dequeue();
                visited[currIndex] = distance;
                
                // assign distance to square at current index
                squares[currIndex].GetComponent<SpriteRenderer>().color = new Color(1-(0.08f*(distance-1)), 1-(0.08f*(distance-1)), 1f);
                // now check 4 directions
                
                // check north
                
                // condition: if currIndex is on top row, don't check north
                
                if ((((_bb.getData() & northMask) & (1UL << (currIndex + 8))) != 0) &&
                    ((visited[currIndex + 8] > distance) || (visited[currIndex + 8] == -1)))
                {
                    // north of current is open
                    q.Enqueue(currIndex + 8);
                }
                // check south
                if ((((_bb.getData() & southMask) & (1UL << (currIndex - 8))) != 0) &&
                    ((visited[currIndex - 8] > distance) || (visited[currIndex - 8] == -1)))
                {
                    // south is open
                    q.Enqueue(currIndex - 8);
                }
                // check west
                if ((((_bb.getData() & westMask) & (1UL << (currIndex - 1))) != 0) &&
                ((visited[currIndex - 1] > distance) || (visited[currIndex - 1] == -1)))
                {
                    // west is open
                    q.Enqueue(currIndex - 1);
                }
                // check east
                if ((((_bb.getData() & eastMask) & (1UL << (currIndex + 1))) != 0) &&
                ((visited[currIndex + 1] > distance) || (visited[currIndex + 1] == -1)))
                {
                    // east is open
                    q.Enqueue(currIndex + 1);
                }
            }
        }

        distances = visited;
    }

    public bool CreatePathToTarget(Queue<int> path, int srcIdx)
    {
        // if unreachable
        if (distances[srcIdx] == -1)
        {
            Debug.Log("Cannot reach target from this index!");
            return false;
        }
        path.Clear();
        // check distances in 4 directions, can't use bit boards then
        int[] dirs = { -1, 0, 1, 0, -1 };

        int currIdx = srcIdx;
        // can do this because flood fill already considers cells that where it is impossible to reach
        while (currIdx != targetIdx)
        {
            // minimize space taken up by bestDistance
            int bestDist = Int16.MaxValue, bestIdx = currIdx;
            // check 4 dirs here with for loop, update 
            for (int i = 0; i < 4; ++i)
            {
                int offset = dirs[i+1] * 8 + dirs[i];
                // Debug.Log("Offset: " + offset);
                // Debug.Log("Other Offset: " + otherOffset);
                if (currIdx + offset > 63 || currIdx + offset < 0 || distances[currIdx + offset] == -1)
                {
                    continue;
                }
                // Debug.Log("===== NEXT  =====");
                if (distances[currIdx + offset] < bestDist)
                {
                    bestDist = distances[currIdx + offset];
                    bestIdx = currIdx + offset;
                }

                if (currIdx + offset == targetIdx)
                {
                    path.Enqueue(targetIdx);
                    return true;
                }
            }
            path.Enqueue(bestIdx);
            currIdx = bestIdx;
        }

        return true;
    }
}
