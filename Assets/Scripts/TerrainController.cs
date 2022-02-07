using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainController : MonoBehaviour
{
    public int chunkSize;
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
    public GameObject waterPlaneObject;

    Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();
    int chunkRange;

    // Start is called before the first frame update
    void Start()
    {
        chunkRange = (int)Mathf.Floor(initialViewDistance / chunkSize);

    }

    // Update is called once per frame
    void Update()
    {
        GenerateChunks();
    }

    public void GenerateChunks()
    {
        NoiseMapInfo mapInfo = new NoiseMapInfo(chunkSize, seed, noiseScale, verticalScale, octaves, lacunarity, persistence, maxLod);

        Transform cameraTransform = GetComponent<Transform>();
        Vector2 playerPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.z);

        Vector2 currentChunk = new Vector2(Mathf.Floor(playerPosition.x / chunkSize), Mathf.Floor(playerPosition.y / chunkSize));

        for (int z = -chunkRange; z <= chunkRange; z++)
        {
            for (int x = -chunkRange; x <= chunkRange; x++)
            {
                Vector2 newChunk = (currentChunk + new Vector2(x, z)) * chunkSize;

                if (!chunks.ContainsKey(newChunk) && newChunk.x >= 0 && newChunk.y >= 0)
                {
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Chunk chunk = new Chunk(newChunk, mapInfo, chunkMaterial);
                    stopwatch.Stop();
                    Debug.Log($"Chunk at {newChunk.ToString()} was created in {stopwatch.ElapsedMilliseconds}ms.");
                    chunks.Add(newChunk, chunk);
                }
            }
        }
    }

    public void ResetChunks()
    {
        foreach (Chunk chunk in chunks.Values)
        {
            GameObject.Destroy(chunk.ChunkObject);
        }
        chunks.Clear();

        GenerateChunks();
    }
}
