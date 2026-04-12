using System;
using UnityEngine;
using System.Numerics;
public static class BitOps
{
    // de Bruijn trick
    private static readonly int[] DeBruijnIdx64 = new int[64]
    {
        // all precomputed indices
        0, 1, 56, 2, 57, 49, 28, 3, 
        61, 58, 50, 42, 38, 29, 17, 4, 
        62, 55, 48, 27, 60, 41, 37, 16, 
        54, 47, 26, 40, 36, 15, 53, 25, 
        59, 51, 43, 39, 18, 63, 52, 44, 
        19, 45, 20, 46, 21, 22, 23, 24, 
        5, 6, 7, 8, 9, 10, 11, 12, 
        13, 14, 30, 31, 32, 33, 34, 35 
    };

    public static int TrailingZeroCount(ulong value) 
    {
        if (value == 0UL) 
            return 64; // match common convention: CTZ(0) = bit width, in this case 64

        // some voodoo magic
        ulong isolated = value & (~value + 1UL); // isolate lowest set bit (one-hot mask?)
        ulong magic = isolated * 0x03F79D71B4CB0A89UL; // Multiply by de Bruijn constant to encode index
        int index = (int)(magic >> 58); // Use top 6 bits as table index (64 entries)
        return DeBruijnIdx64[index]; // Return bit position of isolated lowest set bit
    }
}

public static class BitIter // For each set bit function that is used with manually defined function
{
    public static void ForEachSetBit(ulong data, Action<int> func)
    {
        while (data != 0UL)
        {
            int index = BitOps.TrailingZeroCount(data);
            func(index);
            data &= data - 1UL;
        }
    }
}

public struct Bitboard
{
    private ulong _data;

    public Bitboard(ulong data)
    {
        _data = data;
    }

    public void setData(ulong data)
    {
        _data = data;
    }

    public ulong getData()
    {
        return _data;
    }
        
    
    public void ForEachBit(Action<int> func)
    {
        BitIter.ForEachSetBit(_data, func);
    }

    private void printInt(int val)
    {
        Debug.Log(val);
    }

    public void printBitboard()
    {
        for (int x = 7; x >= 0; x--)
        {
            string s = "";
            for (int y = 7; y >= 0; y--)
            {
                int cell = x * 8 + y;
                if ((_data & (1UL << cell)) > 0)
                {
                    s += "X";
                }
                else
                {
                    s += ".";
                }

                s += " ";
            }
            Debug.Log(s);
        }
    }
}

public class Bitboards : MonoBehaviour
{
    private Bitboard bb;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bb.setData(0b0101010100000000111111110101001011000110000011110011001111110000);
        bb.printBitboard();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}