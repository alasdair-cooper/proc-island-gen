using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.InteropServices;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public class NoiseMapInfo
{
    public int LocalChunkSize { get; set; }

    public int MacroChunkSize { get; set; }

    [field: SerializeField, Range(0, 1)]
    public float IslandDensity { get; set; }

    [field: SerializeField, Min(1)]
    public int ErosionIterations { get; set; }

    [field: SerializeField]
    public int Seed { get; set; }

    [field: SerializeField, Min(0.01f)]
    public float NoiseScale { get; set; }

    [field: SerializeField, Min(0.01f)]
    public float VerticalScale { get; set; }

    [field: SerializeField, Min(1)]
    public int Octaves { get; set; }

    [field: SerializeField, Range(0, 100)]
    public float Lacunarity { get; set; }

    [field: SerializeField, Range(0, 1)]
    public float Persistence { get; set; }

    [field: SerializeField, Range(0, 10)]
    public int MaxLod { get; set; }

    [field: SerializeField, Range(1, 10)]
    public float LacunarityMult { get; set; }

    [field: SerializeField, Range(1, 10)]
    public float PersistenceMult { get; set; }

    [field: SerializeField, HideInInspector]
    public int FalloffEnabled { get; set; }


    public NoiseMapInfo(
        int chunkSize = 10,
        int islandSize = 100,
        int seed = 0,
        float noiseScale = 1,
        float verticalScale = 1,
        int octaves = 1,
        float lacunarity = 25,
        float persistence = 0.5f,
        int maxLod = 1,
        float lacunarityMult = 1,
        float persistenceMult = 1,
        bool falloffEnabled = false)
    {
        LocalChunkSize = chunkSize;
        MacroChunkSize = islandSize;

        Seed = seed;

        NoiseScale = noiseScale;
        VerticalScale = verticalScale;

        Octaves = octaves;

        Lacunarity = lacunarity;
        Persistence = persistence;

        MaxLod = maxLod;

        LacunarityMult = lacunarityMult;
        PersistenceMult = persistenceMult;

        FalloffEnabled = falloffEnabled ? 1 : 0;
    }
}