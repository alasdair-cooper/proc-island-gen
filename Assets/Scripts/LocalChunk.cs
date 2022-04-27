using System.Collections.Generic;
using UnityEngine;

public class LocalChunk
{
    public GameObject ChunkObject { get; set; }
    
    readonly Vector2 _chunkPosition;
    Dictionary<int, Mesh> _chunkLODs;
    int _currentLod = 0;
    int _maxLod = -1;

    int _actualWidth;
    
    NoiseMapInfo MapInfo { get; set; }

    readonly Material _chunkMaterial;

    readonly AnimationCurve _varietyDistribution;
    readonly AnimationCurve _falloffDistribution;

    public LocalChunk(float[] heightMap, Vector2 position, NoiseMapInfo mapInfo, int lod, Material chunkMaterial, Transform chunkParent, AnimationCurve varietyDistribution, AnimationCurve falloffDistribution)
    {
        MapInfo = mapInfo;
        _chunkMaterial = chunkMaterial;
        _varietyDistribution = varietyDistribution;
        _falloffDistribution = falloffDistribution;
        _chunkPosition = position;

        _chunkLODs = new Dictionary<int, Mesh>();

        _actualWidth = MapInfo.LocalChunkSize + 1;

        ChunkObject = new GameObject($"Local Chunk LOD{lod}");
        ChunkObject.transform.SetPositionAndRotation(new Vector3(position.x, 0, position.y), Quaternion.identity);
        ChunkObject.transform.parent = chunkParent;

        ChunkObject.AddComponent<MeshFilter>();
        ChunkObject.AddComponent<MeshRenderer>();
        ChunkObject.AddComponent<MeshCollider>();

        UpdateChunk(heightMap, lod);
    }

    public void UpdateChunk(float[] heightmap, int lod)
    {
        if (_currentLod == lod)
        {
            return;
        }
        else
        {
            lod = SetChunkMesh(heightmap, lod);

            _currentLod = lod;

            if (lod > _maxLod)
            {
                _maxLod = lod;
            }
            ChunkObject.name = $"Local Chunk LOD{lod}";
        }
    }

    public int SetChunkMesh(float[] heightmap, int lod)
    {
        Mesh mesh;
        if (_chunkLODs.ContainsKey(lod))
        {
            mesh = _chunkLODs[lod];
        }
        else
        {
            (lod, mesh) = GenerateChunkMesh(heightmap, lod);
            _chunkLODs[lod] = mesh;
        }

        MeshFilter meshFilter = ChunkObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = ChunkObject.GetComponent<MeshRenderer>();
        MeshCollider meshCollider = ChunkObject.GetComponent<MeshCollider>();

        meshFilter.sharedMesh = mesh;
        meshRenderer.sharedMaterial = _chunkMaterial;
        meshCollider.sharedMesh = mesh;

        return lod;
    }

    (int, Mesh) GenerateChunkMesh(float[] heightmap, int lod)
    {
        int trueLod = (int)Mathf.Pow(2, lod);

        int verticesPerSide;
        if (MapInfo.LocalChunkSize % trueLod != 0) 
        {
            _maxLod = (int)Mathf.Log(Mathf.NextPowerOfTwo(_actualWidth)) + 1;
            lod = _maxLod;
            trueLod = (int)Mathf.Pow(2, lod);
        }
        verticesPerSide = ((_actualWidth - 1) / trueLod) + 1;


        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
        (int, int[]) triangles = (0, new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6]);
        Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];

        System.Random random = new System.Random(MapInfo.Seed);
        Vector2[] offsets = new Vector2[MapInfo.Octaves];

        for (int i = 0; i < offsets.Length; i++)
        {
            offsets[i] = new Vector2(random.Next(-100000, 100000), random.Next(-100000, 100000));
        }

        int vertexIndex = 0;
        for (int z = 0; z < _actualWidth; z += trueLod)
        {
            for (int x = 0; x < _actualWidth; x += trueLod)
            {
                float worldX = x + _chunkPosition.x;
                float worldZ = z + _chunkPosition.y;

                float falloffFactor = 1;
                if (MapInfo.FalloffEnabled == 1)
                {
                    falloffFactor = Mathf.Min(1, Vector2.Distance(
                        new Vector2(worldX % MapInfo.MacroChunkSize, worldZ % MapInfo.MacroChunkSize),
                        new Vector2(0.5f * MapInfo.MacroChunkSize, 0.5f * MapInfo.MacroChunkSize)) / (0.5f * MapInfo.MacroChunkSize));
                    falloffFactor = -falloffFactor + 1;
                    falloffFactor = _varietyDistribution.Evaluate(_falloffDistribution.Evaluate(falloffFactor));

                }

                float currentNoiseHeight = heightmap[z * _actualWidth + x];
                Vector3 currentVertex = new Vector3(x, currentNoiseHeight * MapInfo.VerticalScale * falloffFactor, z);

                vertices[vertexIndex] = currentVertex;
                uvs[vertexIndex] = new Vector2((float)x / _actualWidth, (float)z / _actualWidth);

                if (x < _actualWidth - 1 && z < _actualWidth - 1)
                {
                    AddMeshTriangles(ref triangles, vertexIndex, verticesPerSide);
                }

                vertexIndex++;
            }
        }

        //if (lod == 0)
        //{

        //    for (int x = 1; x < verticesPerSide; x += 2)
        //    {
        //        vertices[x].y = (vertices[x - 1].y + vertices[x + 1].y) / 2;
        //        Debug.Log(x);
        //        vertices[verticesPerSide * (verticesPerSide - 1) + x].y = (vertices[verticesPerSide * (verticesPerSide - 1) + (x - 1)].y + vertices[verticesPerSide * (verticesPerSide - 1) + (x + 1)].y) / 2;
        //    }

        //    for (int z = 1; z < verticesPerSide; z += 2)
        //    {
        //        vertices[z * verticesPerSide].y = (vertices[(z - 1) * verticesPerSide].y + vertices[(z + 1) * verticesPerSide].y) / 2;
        //        vertices[z * verticesPerSide + (verticesPerSide - 1)].y = (vertices[(z - 1) * verticesPerSide + (verticesPerSide - 1)].y + vertices[(z + 1) * verticesPerSide + (verticesPerSide - 1)].y) / 2;
        //    }
        //}
        mesh.vertices = vertices;
        mesh.triangles = triangles.Item2;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return (lod, mesh);
    }

    //public MeshRenderer GenerateMeshObject(Transform parentTransform, int lod, Material chunkMaterial)
    //{
    //    Mesh mesh = GenerateChunkMesh(lod);

    //    if (mesh == null)
    //    {
    //        return null;
    //    }
    //    else
    //    {
    //        GameObject planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
    //        planeObject.name = $"PlaneLODLevel{lod}";
    //        planeObject.transform.parent = parentTransform;
    //        planeObject.transform.position = parentTransform.position;

    //        MeshRenderer meshRenderer = planeObject.GetComponent<MeshRenderer>();
    //        MeshFilter meshFilter = planeObject.GetComponent<MeshFilter>();
    //        MeshCollider meshCollider = planeObject.GetComponent<MeshCollider>();

    //        meshFilter.sharedMesh = mesh;
    //        meshCollider.sharedMesh = mesh;
    //        meshRenderer.sharedMaterial = chunkMaterial;

    //        return meshRenderer;
    //    }
    //}

    //Mesh GenerateChunkMesh(int lod)
    //{
    //    int trueLod = (int)Mathf.Pow(2, lod);
    //    int width = (int)MapInfo.LocalChunkSize + 1;

    //    int verticesPerSide;
    //    if (MapInfo.LocalChunkSize % trueLod == 0)
    //    {
    //        verticesPerSide = ((width - 1) / trueLod) + 1;
    //    }
    //    else
    //    {
    //        return null;
    //    }

    //    Mesh mesh = new Mesh();
    //    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    //    Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
    //    (int, int[]) triangles = (0, new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6]);
    //    Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];

    //    System.Random random = new System.Random(MapInfo.Seed);
    //    Vector2[] offsets = new Vector2[MapInfo.Octaves];

    //    for (int i = 0; i < offsets.Length; i++)
    //    {
    //        offsets[i] = new Vector2(random.Next(-100000, 100000), random.Next(-100000, 100000));
    //    }

    //    int vertexIndex = 0;
    //    for (int z = 0; z < width; z += trueLod)
    //    {
    //        for (int x = 0; x < width; x += trueLod)
    //        {
    //            float worldX = x + ChunkObject.transform.position.x;
    //            float worldZ = z + ChunkObject.transform.position.z;

    //            float falloffFactor = 1;
    //            if (MapInfo.FalloffEnabled == 1)
    //            {
    //                falloffFactor = Mathf.Min(1, Vector2.Distance(
    //                    new Vector2(worldX % MapInfo.MacroChunkSize, worldZ % MapInfo.MacroChunkSize),
    //                    new Vector2(0.5f * MapInfo.MacroChunkSize, 0.5f * MapInfo.MacroChunkSize)) / (0.5f * MapInfo.MacroChunkSize));
    //                falloffFactor = -falloffFactor + 1;
    //            }

    //            float currentNoiseHeight = CalculateNoiseHeight(worldX, worldZ, offsets);
    //            Vector3 currentVertex = new Vector3(x, currentNoiseHeight * MapInfo.VerticalScale * falloffFactor, z);

    //            vertices[vertexIndex] = currentVertex;
    //            uvs[vertexIndex] = new Vector2((float)x / width, (float)z / width);

    //            if (x < width - 1 && z < width - 1)
    //            {
    //                AddMeshTriangles(ref triangles, vertexIndex, verticesPerSide);
    //            }

    //            vertexIndex++;
    //        }
    //    }

    //    mesh.vertices = vertices;
    //    mesh.triangles = triangles.Item2;
    //    mesh.uv = uvs;

    //    mesh.RecalculateNormals();
    //    mesh.RecalculateTangents();

    //    return mesh;
    //}

    //Mesh GenerateChunkMeshViaShader(int lod)
    //{


    //    lod = 0;
    //    int trueLod = (int)Mathf.Pow(2, lod);
    //    int width = (int)MapInfo.LocalChunkSize + 1;

    //    int verticesPerSide;
    //    Debug.Log(trueLod);
    //    if (MapInfo.LocalChunkSize % trueLod == 0)
    //    {
    //        verticesPerSide = ((width - 1) / trueLod) + 1;
    //    }
    //    else
    //    {
    //        return null;
    //    }

    //    Mesh mesh = new Mesh();
    //    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    //    Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
    //    (int, int[]) triangles = (0, new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6]);
    //    Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];
    //    Vector4[] tangents = new Vector4[verticesPerSide * verticesPerSide];

    //    int bufferId = Shader.PropertyToID("_NoiseHeights");

    //    int enabledId = Shader.PropertyToID("_Enabled");
    //    int falloffEnabledId = Shader.PropertyToID("_FalloffEnabled");
    //    int noiseTypeId = Shader.PropertyToID("_NoiseType");
    //    int widthId = Shader.PropertyToID("_Width");
    //    int offsetXId = Shader.PropertyToID("_OffsetX");
    //    int offsetZId = Shader.PropertyToID("_OffsetZ");
    //    int noiseScaleId = Shader.PropertyToID("_NoiseScale");
    //    int verticalScaleId = Shader.PropertyToID("_VerticalScale");
    //    int octavesId = Shader.PropertyToID("_Octaves");
    //    int lacunarityId = Shader.PropertyToID("_Lacunarity");
    //    int persistenceId = Shader.PropertyToID("_Persistence");

    //    ComputeBuffer heightmapBuffer = new ComputeBuffer(width * width, 4);

    //    NoiseShader.SetBuffer(0, bufferId, heightmapBuffer);

    //    NoiseShader.SetInt(enabledId, 0);
    //    NoiseShader.SetInt(falloffEnabledId, 0);
    //    NoiseShader.SetInt(noiseTypeId, 0);
    //    NoiseShader.SetInt(widthId, width);
    //    NoiseShader.SetFloat(offsetXId, ChunkObject.transform.position.x);
    //    NoiseShader.SetFloat(offsetZId, ChunkObject.transform.position.z);
    //    NoiseShader.SetFloat(noiseScaleId, MapInfo.NoiseScale);
    //    NoiseShader.SetFloat(verticalScaleId, MapInfo.VerticalScale);
    //    NoiseShader.SetInt(octavesId, MapInfo.Octaves);
    //    NoiseShader.SetFloat(lacunarityId, MapInfo.Lacunarity);
    //    NoiseShader.SetFloat(persistenceId, MapInfo.Persistence);

    //    int groups = Mathf.CeilToInt(width / 8f);

    //    NoiseShader.Dispatch(0, groups, groups, 1);

    //    float[] noiseHeights = new float[width * width];
    //    heightmapBuffer.GetData(noiseHeights);

    //    heightmapBuffer.Release();

    //    int vertexIndex = 0;
    //    for (int z = 0; z < width; z++)
    //    {
    //        for (int x = 0; x < width; x++)
    //        {
    //            float falloffFactor = Mathf.Min(1, Vector2.Distance(new Vector2(x % MapInfo.MacroChunkSize, z % MapInfo.MacroChunkSize), new Vector2(0.5f * MapInfo.MacroChunkSize, 0.5f * MapInfo.MacroChunkSize)) / (0.5f * MapInfo.MacroChunkSize));

    //            vertices[vertexIndex] = new Vector3(x, noiseHeights[x + z * width] * MapInfo.VerticalScale, z);
    //            uvs[vertexIndex] = new Vector2((float)x / width, (float)z / width);

    //            if (x < width - 1 && z < width - 1)
    //            {
    //                AddMeshTriangles(ref triangles, vertexIndex, verticesPerSide);
    //            }

    //            vertexIndex++;
    //        }
    //    }

    //    mesh.vertices = vertices;
    //    mesh.triangles = triangles.Item2;
    //    mesh.uv = uvs;

    //    mesh.RecalculateNormals();
    //    mesh.tangents = tangents;

    //    return mesh;
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
