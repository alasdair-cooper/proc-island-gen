using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainController : MonoBehaviour
{
    public int chunkSize;
    public int islandSize;

    public int initialViewDistance;
    [Range(0, 10)]
    public int maxLod;

    public int seed = 0;

    [Min(0.01f)]
    public float noiseScale = 1;
    [Min(0.01f)]
    public float verticalScale = 1;

    [Min(1)]
    public int octaves = 5;

    [Range(0, 100)]
    public float lacunarity;
    [Range(0, 1)]
    public float persistence;

    [Range(1, 10)]
    public float lacunarityMultiplier;
    [Range(1, 10)]
    public float persistenceMultiplier;

    public Utils.NoiseType noiseType;

    public bool generateFalloff = false;

    public Material chunkMaterial;

    public float WaterHeight;
    public Material WaterMaterial;

    public ComputeShader noiseHeightmapShader;

    Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();
    public GameObject chunkParent;
    int chunkRange;

    // Start is called before the first frame update
    void Start()
    {
        ResetChunks();
        chunkRange = (int)Mathf.Floor(initialViewDistance / chunkSize);
    }

    // Update is called once per frame
    void Update()
    {
        GenerateChunks();
    }

    public void GenerateChunks()
    {
        NoiseMapInfo mapInfo = new NoiseMapInfo(chunkSize, islandSize, seed, noiseScale, verticalScale, octaves, lacunarity, persistence, maxLod);

        Transform cameraTransform = GetComponent<Transform>();
        Vector2 playerPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.z);

        Vector2 currentChunk = new Vector2(Mathf.Floor(playerPosition.x / chunkSize), Mathf.Floor(playerPosition.y / chunkSize));

        for (int z = -chunkRange; z <= chunkRange; z++)
        {
            for (int x = -chunkRange; x <= chunkRange; x++)
            {
                Vector2 newChunk = (currentChunk + new Vector2(x, z)) * chunkSize;

                if (!chunks.ContainsKey(newChunk) && newChunk.x >= 0 && newChunk.y >= 0 && newChunk.x < mapInfo.IslandWidth && newChunk.y < mapInfo.IslandWidth)
                {
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Chunk chunk = new Chunk(newChunk, mapInfo, chunkMaterial, noiseHeightmapShader);
                    stopwatch.Stop();
                    //Debug.Log($"Chunk at {newChunk} was created in {stopwatch.ElapsedMilliseconds}ms.");
                    chunks.Add(newChunk, chunk);
                    WaterPlaneChunk waterChunk = new WaterPlaneChunk(chunkSize, WaterMaterial);
                    waterChunk.WaterPlane.transform.position = new Vector3(newChunk.x + chunkSize / 2, WaterHeight, newChunk.y + chunkSize / 2);
                    GameObject gameObject = new GameObject();
                    gameObject.name = $"Chunk {newChunk}";
                    gameObject.transform.position = new Vector3(newChunk.x, 0, newChunk.y);
                    waterChunk.WaterPlane.transform.parent = gameObject.transform;
                    chunk.ChunkObject.transform.parent = gameObject.transform;
                    gameObject.transform.parent = chunkParent.transform;
                }
            }
        }
    }

    public void ResetChunks()
    {
        DestroyImmediate(chunkParent);
        chunkParent = new GameObject("Chunks");
        chunks.Clear();

        GenerateChunks();
    }
}
