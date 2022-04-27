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
        { ChunkSizeOptions.Small, (16, 128) },
        { ChunkSizeOptions.Medium, (32, 256)},
        { ChunkSizeOptions.Large, (64, 512)},
        { ChunkSizeOptions.ExtraLarge, (128, 1024)}
    };
}

