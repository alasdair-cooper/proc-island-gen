using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public GameObject ChunkObject { get; set; }
    ComputeShader NoiseShader;
    NoiseMapInfo MapInfo { get; set; }

    public Chunk(Vector2 position, NoiseMapInfo mapInfo, Material chunkMaterial, ComputeShader noiseShader)
    {
        MapInfo = mapInfo;
        NoiseShader = noiseShader;

        ChunkObject = new GameObject("ChunkLODGroup");
        ChunkObject.AddComponent(typeof(LODGroup));
        ChunkObject.transform.SetPositionAndRotation(new Vector3(position.x, 0, position.y), Quaternion.identity);

        LODGroup lodGroup = ChunkObject.GetComponent<LODGroup>();

        List<LOD> lodList = new List<LOD>();
        for (int i = 0; i <= MapInfo.MaxLod; i++)
        {
            MeshRenderer meshRenderer = GenerateMeshObject(ChunkObject.transform, i, chunkMaterial);
            if (meshRenderer != null)
            {
                lodList.Add(new LOD(1f / Mathf.Pow(2, i), new Renderer[] { meshRenderer }));
            }
            else
            {
                break;
            }
        }

        LOD[] lodArray = lodList.ToArray();
        lodArray[lodArray.Length - 1].screenRelativeTransitionHeight = 0;

        lodGroup.SetLODs(lodArray);
        lodGroup.RecalculateBounds();
    }

    public MeshRenderer GenerateMeshObject(Transform parentTransform, int lod, Material chunkMaterial)
    {
        Mesh mesh = GenerateChunkMesh(lod);

        if (mesh == null)
        {
            return null;
        }
        else
        {
            GameObject planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.name = $"PlaneLODLevel{lod}";
            planeObject.transform.parent = parentTransform;
            planeObject.transform.position = parentTransform.position;

            MeshRenderer meshRenderer = planeObject.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = planeObject.GetComponent<MeshFilter>();
            MeshCollider meshCollider = planeObject.GetComponent<MeshCollider>();

            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshRenderer.sharedMaterial = chunkMaterial;

            //Debug.Log(((Mathf.PerlinNoise((10 + MapInfo.WidthOffset), (0 + MapInfo.HeightOffset)) * 2) - 1) + " / " + ((Mathf.PerlinNoise((-10 + MapInfo.WidthOffset), (0 + MapInfo.HeightOffset)) * 2) - 1));

            return meshRenderer;
        }
    }

    Mesh GenerateChunkMesh(int lod)
    {
        int trueLod = (int)Mathf.Pow(2, lod);
        int width = MapInfo.Width + 1;

        int verticesPerSide;
        if (MapInfo.Width % trueLod == 0)
        {
            verticesPerSide = ((width - 1) / trueLod) + 1;
        }
        else
        {
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
        (int, int[]) triangles = (0, new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6]);
        Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];
        Vector4[] tangents = new Vector4[verticesPerSide * verticesPerSide];

        int vertexIndex = 0;
        for (int z = 0; z < width; z += trueLod)
        {
            Vector3 preVertex = new Vector3(-trueLod, CalculateNoiseHeight(-trueLod, z) * MapInfo.VerticalScale, z);

            for (int x = 0; x < width; x += trueLod)
            {
                float worldX = x + ChunkObject.transform.position.x;
                float worldZ = z + ChunkObject.transform.position.z;

                float falloffFactor = Mathf.Min(1, Vector2.Distance(new Vector2(worldX % MapInfo.IslandWidth, worldZ % MapInfo.IslandWidth), new Vector2(0.5f * MapInfo.IslandWidth, 0.5f * MapInfo.IslandWidth)) / (0.5f * MapInfo.IslandWidth));
                falloffFactor = -falloffFactor + 1;

                float currentNoiseHeight = CalculateNoiseHeight(worldX, worldZ);
                float nextNoiseHeight = CalculateNoiseHeight(worldX + trueLod, z + worldZ);
                Vector3 currentVertex = new Vector3(x, currentNoiseHeight * MapInfo.VerticalScale * falloffFactor, z);
                Vector3 nextVertex = new Vector3(x + trueLod, nextNoiseHeight * MapInfo.VerticalScale * falloffFactor, z);

                Vector3 vertexLeft = new Vector3(x - trueLod, x == 0 ? preVertex.y : vertices[vertexIndex - 1].y, z);
                Vector3 vertexRight = new Vector3(x + trueLod, nextVertex.y, z);

                Vector3 tangent = (vertexLeft - vertexRight).normalized;

                vertices[vertexIndex] = currentVertex;
                uvs[vertexIndex] = new Vector2((float)x / width, (float)z / width);
                tangents[vertexIndex] = new Vector4(tangent.x, tangent.y, tangent.z, -1.0f);

                if (x < width - 1 && z < width - 1)
                {
                    AddMeshTriangles(ref triangles, vertexIndex, verticesPerSide);
                }

                vertexIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.Item2;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.tangents = tangents;

        return mesh;
    }

    Mesh GenerateChunkMeshViaShader(int lod)
    {

        
        lod = 0;
        int trueLod = (int)Mathf.Pow(2, lod);
        int width = MapInfo.Width + 1;

        int verticesPerSide;
        Debug.Log(trueLod);
        if (MapInfo.Width % trueLod == 0)
        {
            verticesPerSide = ((width - 1) / trueLod) + 1;
        }
        else
        {
            return null;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
        (int, int[]) triangles = (0, new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6]);
        Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];
        Vector4[] tangents = new Vector4[verticesPerSide * verticesPerSide];

        int bufferId = Shader.PropertyToID("_NoiseHeights");

        int enabledId = Shader.PropertyToID("_Enabled");
        int falloffEnabledId = Shader.PropertyToID("_FalloffEnabled");
        int noiseTypeId = Shader.PropertyToID("_NoiseType");
        int widthId = Shader.PropertyToID("_Width");
        int offsetXId = Shader.PropertyToID("_OffsetX");
        int offsetZId = Shader.PropertyToID("_OffsetZ");
        int noiseScaleId = Shader.PropertyToID("_NoiseScale");
        int verticalScaleId = Shader.PropertyToID("_VerticalScale");
        int octavesId = Shader.PropertyToID("_Octaves");
        int lacunarityId = Shader.PropertyToID("_Lacunarity");
        int persistenceId = Shader.PropertyToID("_Persistence");

        ComputeBuffer heightmapBuffer = new ComputeBuffer(width * width, 4);

        NoiseShader.SetBuffer(0, bufferId, heightmapBuffer);

        NoiseShader.SetInt(enabledId, 0);
        NoiseShader.SetInt(falloffEnabledId, 0);
        NoiseShader.SetInt(noiseTypeId, 0);
        NoiseShader.SetInt(widthId, width);
        NoiseShader.SetFloat(offsetXId, ChunkObject.transform.position.x);
        NoiseShader.SetFloat(offsetZId, ChunkObject.transform.position.z);
        NoiseShader.SetFloat(noiseScaleId, MapInfo.NoiseScale);
        NoiseShader.SetFloat(verticalScaleId, MapInfo.VerticalScale);
        NoiseShader.SetInt(octavesId, MapInfo.Octaves);
        NoiseShader.SetFloat(lacunarityId, MapInfo.Lacunarity);
        NoiseShader.SetFloat(persistenceId, MapInfo.Persistence);

        int groups = Mathf.CeilToInt(width / 8f);

        NoiseShader.Dispatch(0, groups, groups, 1);

        float[] noiseHeights = new float[width * width];
        heightmapBuffer.GetData(noiseHeights);

        heightmapBuffer.Release();

        int vertexIndex = 0;
        for (int z = 0; z < width; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float falloffFactor = Mathf.Min(1, Vector2.Distance(new Vector2(x % MapInfo.IslandWidth, z % MapInfo.IslandWidth), new Vector2(0.5f * MapInfo.IslandWidth, 0.5f * MapInfo.IslandWidth)) / (0.5f * MapInfo.IslandWidth));

                vertices[vertexIndex] = new Vector3(x, noiseHeights[x + z * width] * MapInfo.VerticalScale, z);
                uvs[vertexIndex] = new Vector2((float)x / width, (float)z / width);

                if (x < width - 1 && z < width - 1)
                {
                    AddMeshTriangles(ref triangles, vertexIndex, verticesPerSide);
                }

                vertexIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.Item2;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.tangents = tangents;

        return mesh;
    }

    float CalculateNoiseHeight(float x, float z)
    {
        // Factors to modify the noise by
        float frequency = 1;
        float amplitude = 1;

        // Noise value for this point (x, z)
        float noiseHeight = 0;

        for (int i = 0; i < MapInfo.Octaves; i++)
        {
            float xValue = (x  / MapInfo.NoiseScale) * frequency;
            float zValue = (z / MapInfo.NoiseScale) * frequency;

            float noiseSample =  Mathf.Clamp(Mathf.PerlinNoise(xValue, zValue), 0, 1);
            noiseHeight += noiseSample * amplitude;

            frequency *= MapInfo.Lacunarity;
            amplitude *= MapInfo.Persistence;
        }

        return noiseHeight;
    }

    //float CalculateNoiseHeight(int x, int z)
    //{
    //    x += (int)ChunkObject.transform.position.x;
    //    z += (int)ChunkObject.transform.position.z;

    //    // Factors to modify the noise by
    //    float frequency = 1;
    //    float amplitude = 1;

    //    // Noise value for this point (x, z)
    //    float noiseHeight = 0;

    //    System.Random prng = new System.Random(MapInfo.Seed);

    //    for (int i = 0; i < MapInfo.Octaves; i++)
    //    {
    //        float xValue = (((x + prng.Next(-10000, 10000))) / MapInfo.NoiseScale) * frequency;
    //        float zValue = (((z + prng.Next(-10000, 10000))) / MapInfo.NoiseScale) * frequency;

    //        float noiseSample = (Mathf.PerlinNoise(xValue, zValue) * 2) - 1;
    //        noiseHeight += noiseSample * amplitude;

    //        frequency *= MapInfo.Lacunarity;
    //        amplitude *= MapInfo.Persistence;
    //    }

    //    return noiseHeight;
    //}

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
