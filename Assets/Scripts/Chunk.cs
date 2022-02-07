using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public GameObject ChunkObject { get; set; }
    NoiseMapInfo MapInfo { get; set; }

    public Chunk(Vector2 position, NoiseMapInfo mapInfo, Material chunkMaterial)
    {
        MapInfo = mapInfo;

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

        (int, Vector3[]) vertices = (0, new Vector3[verticesPerSide * verticesPerSide]);
        (int, int[]) triangles = (0, new int[(verticesPerSide - 1) * (verticesPerSide - 1) * 6]);
        Vector2[] uvs = new Vector2[verticesPerSide * verticesPerSide];
        Vector4[] tangents = new Vector4[verticesPerSide * verticesPerSide];

        for (int z = 0; z < width; z += trueLod)
        {
            Vector3 preVertex = new Vector3(-trueLod, CalculateNoiseHeight(-trueLod, z) * MapInfo.VerticalScale, z);

            for (int x = 0; x < width; x += trueLod)
            {
                float currentNoiseHeight = CalculateNoiseHeight(x + ChunkObject.transform.position.x, z + ChunkObject.transform.position.z);
                float nextNoiseHeight = CalculateNoiseHeight(x + ChunkObject.transform.position.x + trueLod, z + ChunkObject.transform.position.z);
                Vector3 currentVertex = new Vector3(x, currentNoiseHeight * MapInfo.VerticalScale, z);
                Vector3 nextVertex = new Vector3(x + trueLod, nextNoiseHeight * MapInfo.VerticalScale, z);

                Vector3 vertexLeft = new Vector3(x - trueLod, x == 0 ? preVertex.y : vertices.Item2[vertices.Item1 - 1].y, z);
                Vector3 vertexRight = new Vector3(x + trueLod, nextVertex.y, z);

                Vector3 tangent = (vertexLeft - vertexRight).normalized;

                vertices.Item2[vertices.Item1] = currentVertex;
                uvs[vertices.Item1] = new Vector2((float)x / width, (float)z / width);
                tangents[vertices.Item1] = new Vector4(tangent.x, tangent.y, tangent.z, -1.0f);

                if (x < width - 1 && z < width - 1)
                {
                    AddMeshTriangles(ref triangles, vertices.Item1, verticesPerSide);
                }

                vertices.Item1++;
            }
        }

        mesh.vertices = vertices.Item2;
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

            float noiseSample = (Mathf.PerlinNoise(xValue, zValue) * 2) - 1;
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
