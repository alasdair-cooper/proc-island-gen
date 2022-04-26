using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public enum NoiseType
    {
        perlin,
        simplex,
        displacement
    }

    public enum RenderMode
    {
        CPU,
        GPU,
        flat
    }

    public enum DebugType
    {
        none,
        normals,
        slope
    }

    public enum ChunkSizeOptions
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    public static Dictionary<ChunkSizeOptions, (int, int)> ChunkSizes = new Dictionary<ChunkSizeOptions, (int, int)>() 
    {
        { ChunkSizeOptions.Small, (10, 100) },
        { ChunkSizeOptions.Medium, (30, 300)},
        { ChunkSizeOptions.Large, (50, 500)},
        { ChunkSizeOptions.ExtraLarge, (100, 1000)}
    };
}

