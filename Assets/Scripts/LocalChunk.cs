using System.Collections.Generic;
using UnityEngine;

public class LocalChunk
{
    public GameObject ChunkObject { get; set; }
    
    public Vector2 ChunkPosition;
    Dictionary<int, Mesh> _chunkLODs;
    int _currentLod = -1;
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
        ChunkPosition = position;

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
                float worldX = x + ChunkPosition.x;
                float worldZ = z + ChunkPosition.y;

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

        mesh.vertices = vertices;
        mesh.triangles = triangles.Item2;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return (lod, mesh);
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
