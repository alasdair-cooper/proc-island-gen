using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainController : MonoBehaviour
{
    public NoiseMapInfo NoiseMapInfo;
    private float timeSinceLastNoiseMapUpdate = 0;

    public int chunkSize;
    public int initialViewDistance;

    public Material chunkMaterial;
    public float WaterHeight;
    public Material WaterMaterial;
    public ComputeShader noiseHeightmapShader;

    Dictionary<Vector2, Chunk> chunks = new Dictionary<Vector2, Chunk>();
    public GameObject chunkParent;
    int chunkRange;

    public Utils.NoiseType noiseType;

    public bool autoUpdate;

    // Start is called before the first frame update
    void Start()
    {
        ResetChunks();
        chunkRange = (int)Mathf.Floor(initialViewDistance / chunkSize);
    }

    // Update is called once per frame
    void Update()
    {
        if (autoUpdate && timeSinceLastNoiseMapUpdate > 1)
        {
            ResetChunks();
            timeSinceLastNoiseMapUpdate = 0;
        }
        timeSinceLastNoiseMapUpdate += Time.deltaTime;
    }

    public void GenerateChunks()
    {
        Transform cameraTransform = GetComponent<Transform>();
        Vector2 playerPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.z);

        Vector2 currentChunk = new Vector2(Mathf.Floor(playerPosition.x / chunkSize), Mathf.Floor(playerPosition.y / chunkSize));

        for (int z = -chunkRange; z <= chunkRange; z++)
        {
            for (int x = -chunkRange; x <= chunkRange; x++)
            {
                Vector2 newChunk = (currentChunk + new Vector2(x, z)) * chunkSize;

                if (!chunks.ContainsKey(newChunk) && newChunk.x >= 0 && newChunk.y >= 0 && newChunk.x < NoiseMapInfo.IslandWidth && newChunk.y < NoiseMapInfo.IslandWidth)
                {
                    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    Chunk chunk = new Chunk(newChunk, NoiseMapInfo, chunkMaterial, noiseHeightmapShader);
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
