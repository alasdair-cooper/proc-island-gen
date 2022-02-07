using System.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct ParallelPerlinJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> x;
    [ReadOnly]
    public NativeArray<float> z;

    public NativeArray<float> result;

    public float octaves;
    public float noiseScale;
    public float lacunarity;
    public float persistence;

    public void Execute(int index)
    {
        // Factors to modify the noise by
        float frequency = 1;
        float amplitude = 1;

        // Noise value for this point (x, z)
        float noiseHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            float xValue = (x[index] / noiseScale) * frequency;
            float zValue = (z[index] / noiseScale) * frequency;

            float noiseSample = (Mathf.PerlinNoise(xValue, zValue) * 2) - 1;
            noiseHeight += noiseSample * amplitude;

            frequency *= lacunarity;
            amplitude *= persistence;
        }

        result[index] = noiseHeight;
    }
}

