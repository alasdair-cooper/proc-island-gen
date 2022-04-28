using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Unity;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;

public class MacroChunk
{
    public GameObject ChunkObject { get; set; }

    public Vector2 ChunkPosition;

    public float[] heightmap { get; set; }

    public int ActualSize;

    public WaterPlaneChunk WaterChunk { get; set; }

    public bool IsTerrain;
    public bool IsEroded = false;

    NoiseMapInfo MapInfo;

    AnimationCurve VarietyDistribution;
    AnimationCurve FalloffDistribution;

    public Thread ErosionThread;

    public System.Diagnostics.Stopwatch Stopwatch;

    public MacroChunk(Vector2 position, NoiseMapInfo mapInfo, Material waterMaterial, Material chunkMaterial, float waterHeight, Transform macroChunksParent, AnimationCurve varietyDistribution, AnimationCurve falloffDistribution, bool isTerrain = false, bool erodeTerrain = true)
    {
        ChunkPosition = position;
        MapInfo = mapInfo;
        VarietyDistribution = varietyDistribution;
        FalloffDistribution = falloffDistribution;
        IsTerrain = isTerrain;

        this.ActualSize = MapInfo.MacroChunkSize + 1;

        WaterChunk = new WaterPlaneChunk(mapInfo.MacroChunkSize, waterMaterial);
        WaterChunk.WaterPlane.transform.position = new Vector3(position.x + MapInfo.MacroChunkSize / 2, waterHeight, position.y + MapInfo.MacroChunkSize / 2);
        WaterChunk.WaterPlane.transform.parent = macroChunksParent;

        if (isTerrain)
        {
            heightmap = new float[ActualSize * ActualSize];

            NativeArray<float> heights = new NativeArray<float>(ActualSize * ActualSize, Allocator.TempJob);

            System.Random random = new System.Random(MapInfo.Seed);
            NativeArray<int> xOffsets = new NativeArray<int>(mapInfo.Octaves, Allocator.TempJob);
            NativeArray<int> zOffsets = new NativeArray<int>(mapInfo.Octaves, Allocator.TempJob);

            for (int i = 0; i < mapInfo.Octaves; i++)
            {
                xOffsets[i] = random.Next(-100000, 100000);
                zOffsets[i] = random.Next(-100000, 100000);
            }

            NoiseHeightJob jobData = new NoiseHeightJob();

            jobData.heights = heights;
            jobData.xOffsets = xOffsets;
            jobData.zOffsets = zOffsets;
            jobData.size = mapInfo.MacroChunkSize;
            jobData.octaves = mapInfo.Octaves;
            jobData.mode = mapInfo.Mode;
            jobData.scale = mapInfo.NoiseScale;
            jobData.persistence = mapInfo.Persistence;
            jobData.lacunarity = mapInfo.Lacunarity;
            jobData.xOffset = position.x;
            jobData.zOffset = position.y;

            JobHandle handle = jobData.Schedule(heights.Length, 1);

            handle.Complete();

            heightmap = heights.ToArray();

            //for (int i = 0; i < heightmap.Length; i++)
            //{
            //    heightmap[i] = varietyDistribution.Evaluate(heightmap[i]);
            //}

            heights.Dispose();
            xOffsets.Dispose();
            zOffsets.Dispose();

            if (erodeTerrain)
            {
                Stopwatch = new System.Diagnostics.Stopwatch();

                Stopwatch.Start();

                Erosion eroder = new Erosion();

                ThreadStart starter = () => eroder.Erode(heightmap, MapInfo.MacroChunkSize, MapInfo.Seed, MapInfo.ErosionIterations);

                starter += () => OnErosionFinish();

                ErosionThread = new Thread(starter);
                ErosionThread.Start();
            }
        }
    }

    void OnErosionFinish()
    {
        
    }

    void GenerateChunkMesh(Vector2 position, Transform parent, Material material)
    {
        GameObject planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planeObject.name = $"Macro Chunk at {position}";
        planeObject.transform.parent = parent;
        planeObject.transform.position = new Vector3(position.x, 0, position.y);

        MeshFilter meshFilter = planeObject.GetComponent<MeshFilter>();
        MeshCollider meshCollider = planeObject.GetComponent<MeshCollider>();
        MeshRenderer meshRenderer = planeObject.GetComponent<MeshRenderer>();

        int width = MapInfo.MacroChunkSize + 1;

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[width * width];
        (int, int[]) triangles = (0, new int[(width - 1) * (width - 1) * 6]);
        Vector2[] uvs = new Vector2[width * width];

        int vertexIndex = 0;
        for (int z = 0; z < width; z += 1)
        {
            for (int x = 0; x < width; x += 1)
            {
                float worldX = x + position.x;
                float worldZ = z + position.y;

                float falloffFactor = 1;
                if (MapInfo.FalloffEnabled == 1)
                {
                    falloffFactor = Mathf.Min(1, Vector2.Distance(
                        new Vector2(worldX % MapInfo.MacroChunkSize, worldZ % MapInfo.MacroChunkSize),
                        new Vector2(0.5f * MapInfo.MacroChunkSize, 0.5f * MapInfo.MacroChunkSize)) / (0.5f * MapInfo.MacroChunkSize));
                    falloffFactor = -falloffFactor + 1;
                    falloffFactor = VarietyDistribution.Evaluate(FalloffDistribution.Evaluate(falloffFactor));
                }

                float currentNoiseHeight = heightmap[z * MapInfo.MacroChunkSize + x];
                Vector3 currentVertex = new Vector3(x, currentNoiseHeight * MapInfo.VerticalScale * falloffFactor, z);

                vertices[vertexIndex] = currentVertex;
                uvs[vertexIndex] = new Vector2((float)x / width, (float)z / width);

                if (x < width - 1 && z < width - 1)
                {
                    AddMeshTriangles(ref triangles, vertexIndex, width);
                }

                vertexIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.Item2;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        //mesh.RecalculateTangents();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshRenderer.sharedMaterial = material;

        ChunkObject = planeObject;
    }

    void AddMeshTriangles(ref (int, int[]) triangles, int vertexIndex, int verticesPerSide)
    {
        // First triangle
        triangles.Item2[triangles.Item1 + 2] = vertexIndex;
        triangles.Item2[triangles.Item1 + 1] = vertexIndex + verticesPerSide + 1;
        triangles.Item2[triangles.Item1] = vertexIndex + verticesPerSide;
        // Second triangle
        triangles.Item2[triangles.Item1 + 5] = vertexIndex + verticesPerSide + 1;
        triangles.Item2[triangles.Item1 + 4] = vertexIndex;
        triangles.Item2[triangles.Item1 + 3] = vertexIndex + 1;

        triangles.Item1 += 6;
    }
}
