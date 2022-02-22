using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public class NoiseMapInfo
{
    public int Width { get; set; }
    public int IslandWidth { get; set; }

    public int Seed { get; set; }

    public float NoiseScale { get; set; }
    public float VerticalScale { get; set; }

    public int Octaves { get; set; }

    public float Lacunarity { get; set; }
    public float Persistence { get; set; }

    public int MaxLod { get; set; }

    public float LacunarityMult { get; set; }
    public float PersistenceMult { get; set; }


    public bool FalloffEnabled { get; set; }


    public NoiseMapInfo(
        int width,
        int islandWidth,
        int seed,
        float noiseScale,
        float verticalScale,
        int octaves,
        float lacunarity,
        float persistence,
        int maxLod = 1,
        float lacunarityMult = 1,
        float persistenceMult = 1,
        bool falloffEnabled = false)
    {
        Width = width;
        IslandWidth = islandWidth;

        Seed = seed;

        NoiseScale = noiseScale;
        VerticalScale = verticalScale;

        Octaves = octaves;

        Lacunarity = lacunarity;
        Persistence = persistence;

        MaxLod = maxLod;

        LacunarityMult = lacunarityMult;
        PersistenceMult = persistenceMult;

        FalloffEnabled = falloffEnabled;
    }
}