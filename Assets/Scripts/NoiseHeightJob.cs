using System.Collections;
using UnityEngine;

using Unity.Jobs;
using Unity.Collections;

using Unity.Mathematics;

public struct NoiseHeightJob : IJobParallelFor
{
    [WriteOnly]
    public NativeArray<float> heights;

    [ReadOnly]
    public NativeArray<int> xOffsets;

    [ReadOnly]
    public NativeArray<int> zOffsets;

    public int size;
    public int octaves;

    public int mode;

    public float scale;
    public float persistence;
    public float lacunarity;

    public float xOffset;
    public float zOffset;

    public void Execute(int index)
    {
        float x = index % size + xOffset;
        float z = Mathf.RoundToInt(index / size) + zOffset;

        // Factors to modify the noise by
        float frequency = 1;
        float amplitude = 1;

        // Noise value for this point (x, z)
        float noiseHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
            float xValue = xOffsets[i] + (x / scale) * frequency;
            float zValue = zOffsets[i] + (z / scale) * frequency;

            //if (i == 0)
            //{
            //    noiseSample = VarietyDistribution.Evaluate(Mathf.Clamp(Mathf.PerlinNoise(xValue, zValue), 0, 1));
            //}
            //else
            //{
            //    noiseSample = Mathf.Clamp(Mathf.PerlinNoise(xValue, zValue), 0, 1);
            //}

            float2 position = new float2(xValue, zValue);

            if (mode == 0)
            {
                noiseHeight += Mathf.Clamp(Mathf.PerlinNoise(xValue, zValue), 0, 1) * amplitude;
            }
            else if(mode == 1)
            {
                noiseHeight += Mathf.Clamp(noise.snoise(position), 0, 1) * amplitude;
            }
            frequency *= lacunarity;
            amplitude *= persistence;
        }

        heights[index] = noiseHeight;
    }
}