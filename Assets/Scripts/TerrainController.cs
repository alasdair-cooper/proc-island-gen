using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Unity;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainController : MonoBehaviour
{
    public NoiseMapInfo NoiseMapInfo;
    private float timeSinceLastNoiseMapUpdate = 0;

    public AnimationCurve VarietyDistribution;
    public AnimationCurve FalloffDistribution;

    public int MacroChunkViewDistance;
    public int LocalChunkViewDistance;

    public Material ChunkMaterial;
    public float WaterHeight;
    public Material WaterMaterial;

    ComputeShader NoiseHeightmapShader;

    Dictionary<Vector2, LocalChunk> CurrentLocalChunks = new Dictionary<Vector2, LocalChunk>();
    Dictionary<Vector2, LocalChunk> PreviousLocalChunks = new Dictionary<Vector2, LocalChunk>();
    public GameObject LocalChunkParent;
    int localChunkRange;

    Dictionary<Vector2, MacroChunk> CurrentMacroChunks = new Dictionary<Vector2, MacroChunk>();
    Dictionary<Vector2, MacroChunk> PreviousMacroChunks = new Dictionary<Vector2, MacroChunk>();
    public GameObject MacroChunkParent;
    int macroChunkRange;

    public Utils.ChunkSizeOptions chunkSizeOptions;

    public Utils.NoiseType NoiseType;

    public bool AutoUpdate;
    public float AutoUpdateTime = 1;

    public bool PauseUpdate;

    public bool GenerateWithFalloff;
    public bool GenerateInfinitely;
    public bool PreserveChunks;

    public bool EnableConsoleChunkUpdates;

    // Start is called before the first frame update
    void Start()
    {
        ResetChunks();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMapInfo();
        UpdateFog();
        UpdateChunks();
    }

    public Vector2 GetLocalChunkPositionAtCamera()
    {
        Transform cameraTransform = GetComponent<Transform>();
        Vector2 playerPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.z);

        Vector2 currentChunkPosition = new Vector2(Mathf.Floor(playerPosition.x / NoiseMapInfo.LocalChunkSize), Mathf.Floor(playerPosition.y / NoiseMapInfo.LocalChunkSize)) * NoiseMapInfo.LocalChunkSize;

        return currentChunkPosition;
    }

    public Vector2 GetMacroChunkPositionAtCamera()
    {
        Transform cameraTransform = GetComponent<Transform>();
        Vector2 playerPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.z);

        Vector2 currentChunkPosition = new Vector2(Mathf.Floor(playerPosition.x / NoiseMapInfo.MacroChunkSize), Mathf.Floor(playerPosition.y / NoiseMapInfo.MacroChunkSize)) * NoiseMapInfo.MacroChunkSize;

        return currentChunkPosition;
    }

    public Vector2 GetMacroChunkPositionAtPosition(Vector2 position)
    {
        Vector2 currentChunkPosition = new Vector2(Mathf.Floor(position.x / NoiseMapInfo.MacroChunkSize), Mathf.Floor(position.y / NoiseMapInfo.MacroChunkSize)) * NoiseMapInfo.MacroChunkSize;

        return currentChunkPosition;
    }

    public void UpdateChunks()
    {
        if (AutoUpdate && timeSinceLastNoiseMapUpdate > AutoUpdateTime)
        {
            ResetChunks();
            timeSinceLastNoiseMapUpdate = 0;
        }
        else if (!PauseUpdate)
        {
            GenerateMacroChunks();
            GenerateLocalChunks();
        }
        timeSinceLastNoiseMapUpdate += Time.deltaTime;
    }

    public void UpdateFog()
    {
        RenderSettings.fogStartDistance = MacroChunkViewDistance * 0.9f;
        RenderSettings.fogEndDistance = MacroChunkViewDistance;
    }

    public void UpdateMapInfo()
    {
        (int, int) chunkSizes = Utils.ChunkSizes[chunkSizeOptions];

        NoiseMapInfo.LocalChunkSize = chunkSizes.Item1;
        NoiseMapInfo.MacroChunkSize = chunkSizes.Item2;

        localChunkRange = (int)Mathf.Floor(LocalChunkViewDistance / NoiseMapInfo.LocalChunkSize);
        macroChunkRange = (int)Mathf.Floor(MacroChunkViewDistance / NoiseMapInfo.MacroChunkSize);

        NoiseMapInfo.FalloffEnabled = GenerateWithFalloff ? 1 : 0;
    }

    public void GenerateMacroChunks()
    {
        Vector2 currentMacroChunkPosition = GetMacroChunkPositionAtCamera();

        for (int z = -macroChunkRange; z <= macroChunkRange; z++)
        {
            for (int x = -macroChunkRange; x <= macroChunkRange; x++)
            {
                Vector2 newChunkOriginPosition = currentMacroChunkPosition + (new Vector2(x, z) * NoiseMapInfo.MacroChunkSize);
                if (GenerateInfinitely)
                {
                    if (Vector2.Distance(currentMacroChunkPosition, newChunkOriginPosition) < macroChunkRange * NoiseMapInfo.MacroChunkSize)
                    {
                        GenerateSingleMacroChunk(newChunkOriginPosition);
                    }
                }
            }
        }

        foreach (MacroChunk chunk in PreviousMacroChunks.Values)
        {
            DestroyImmediate(chunk.WaterChunk.WaterPlane);
            DestroyImmediate(chunk.ChunkObject);
        }

        PreviousMacroChunks = CurrentMacroChunks.ToDictionary(entry => entry.Key, entry => entry.Value);
        CurrentMacroChunks.Clear();
    }

    public void GenerateSingleMacroChunk(Vector2 newChunkOriginPosition)
    {
        MacroChunk currentChunk;

        bool isIsland = Mathf.PerlinNoise(newChunkOriginPosition.x / NoiseMapInfo.MacroChunkSize + 0.01f, newChunkOriginPosition.y / NoiseMapInfo.MacroChunkSize + 0.01f) > NoiseMapInfo.IslandDensity;
        // Debug.Log(Mathf.PerlinNoise(newChunkOriginPosition.x / NoiseMapInfo.IslandSize + 0.01f, newChunkOriginPosition.y / NoiseMapInfo.IslandSize + 0.01f));

        if (PreviousMacroChunks.TryGetValue(newChunkOriginPosition, out currentChunk))
        {
            if ((currentChunk.ErosionThread != null && !currentChunk.ErosionThread.IsAlive))
            {
                currentChunk.IsEroded = true;
                currentChunk.ErosionThread = null;
            }

            CurrentMacroChunks.Add(newChunkOriginPosition, currentChunk);
            PreviousMacroChunks.Remove(newChunkOriginPosition);
        }
        else
        {
            if (EnableConsoleChunkUpdates)
            {
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                currentChunk = new MacroChunk(newChunkOriginPosition, NoiseMapInfo, WaterMaterial, ChunkMaterial, WaterHeight, MacroChunkParent.transform, VarietyDistribution, FalloffDistribution, isIsland);
                stopwatch.Stop();
                Debug.Log($"Macro chunk at {newChunkOriginPosition} was created in {stopwatch.ElapsedMilliseconds}ms.");
            }
            else
            {
                currentChunk = new MacroChunk(newChunkOriginPosition, NoiseMapInfo, WaterMaterial, ChunkMaterial, WaterHeight, MacroChunkParent.transform, VarietyDistribution, FalloffDistribution, isIsland);
            }

            CurrentMacroChunks.Add(newChunkOriginPosition, currentChunk);
        }
    }

    public void GenerateLocalChunks()
    {
        Vector2 currentChunkPosition = GetLocalChunkPositionAtCamera();

        for (int z = -localChunkRange; z <= localChunkRange; z++)
        {
            for (int x = -localChunkRange; x <= localChunkRange; x++)
            {
                Vector2 newChunkOriginPosition = currentChunkPosition + (new Vector2(x, z) * NoiseMapInfo.LocalChunkSize);

                Vector2 currentMacroChunkPosition = GetMacroChunkPositionAtPosition(newChunkOriginPosition);

                MacroChunk currentMacroChunk;

                if (PreviousMacroChunks.TryGetValue(currentMacroChunkPosition, out currentMacroChunk))
                {
                    if (GenerateInfinitely 
                        && Vector2.Distance(currentChunkPosition, newChunkOriginPosition) < localChunkRange * NoiseMapInfo.LocalChunkSize 
                        && currentMacroChunk.IsTerrain
                        && currentMacroChunk.IsEroded)
                    {
                        int initialPoint = (int)(newChunkOriginPosition.x - currentMacroChunkPosition.x) + (int)(newChunkOriginPosition.y - currentMacroChunkPosition.y) * (NoiseMapInfo.MacroChunkSize);

                        GenerateSingleLocalChunk(newChunkOriginPosition, currentMacroChunk.heightmap, initialPoint);

                        currentMacroChunk.ErosionThread = null;
                    } 
                }
            }
        }

        foreach (LocalChunk chunk in PreviousLocalChunks.Values)
        {
            DestroyImmediate(chunk.ChunkObject.transform.parent.gameObject);
        }

        PreviousLocalChunks = CurrentLocalChunks.ToDictionary(entry => entry.Key, entry => entry.Value); 
        CurrentLocalChunks.Clear();
    }

    public void GenerateSingleLocalChunk(Vector2 newChunkOriginPosition, float[] heightmap, int initialPoint)
    {
        LocalChunk currentChunk;
        if (PreviousLocalChunks.TryGetValue(newChunkOriginPosition, out currentChunk))
        {
            CurrentLocalChunks.Add(newChunkOriginPosition, currentChunk);
            PreviousLocalChunks.Remove(newChunkOriginPosition);
        }
        else
        {
            float[] chunkHeightmap = SliceHeightmap(heightmap, initialPoint, NoiseMapInfo.LocalChunkSize + 1, NoiseMapInfo.MacroChunkSize);

            if (EnableConsoleChunkUpdates)
            {
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                currentChunk = new LocalChunk(chunkHeightmap, newChunkOriginPosition, NoiseMapInfo, ChunkMaterial, VarietyDistribution, FalloffDistribution);
                stopwatch.Stop();
                Debug.Log($"Local chunk at {newChunkOriginPosition} was created in {stopwatch.ElapsedMilliseconds}ms.");
            }
            else
            {
                currentChunk = new LocalChunk(chunkHeightmap, newChunkOriginPosition, NoiseMapInfo, ChunkMaterial, VarietyDistribution, FalloffDistribution);
            }

            SetupTransforms($"Chunk {newChunkOriginPosition}", new Vector3(newChunkOriginPosition.x, 0, newChunkOriginPosition.y), new Transform[] { currentChunk.ChunkObject.transform });

            CurrentLocalChunks.Add(newChunkOriginPosition, currentChunk);
        }
    }

    public float[] SliceHeightmap(float[] heightmap, int startIndex, int width, int gapSize)
    {
        List<float> heightmapSlice = new List<float>();
        for (int z = 0; z < width; z++)
        {
            for (int x = startIndex + z * gapSize; x < startIndex + z * gapSize + width; x++)
            {
                heightmapSlice.Add(heightmap[x]);
            }
        }
        return heightmapSlice.ToArray();
    }

    public void SetupTransforms(string parentName, Vector3 parentPosition, Transform[] children)
    {
        GameObject parentObject = new GameObject()
        {
            name = parentName
        };
        parentObject.transform.position = parentPosition;
        
        foreach (Transform child in children)
        {
            child.transform.parent = parentObject.transform;
        }

        parentObject.transform.parent = LocalChunkParent.transform;
    }

    public void ResetChunks()
    {
        DestroyImmediate(LocalChunkParent);
        DestroyImmediate(MacroChunkParent);

        LocalChunkParent = new GameObject("Local Chunk Parent");
        MacroChunkParent = new GameObject("Macro Chunk Parent");

        PreviousLocalChunks.Clear();
        PreviousMacroChunks.Clear();

        CurrentLocalChunks.Clear();
        CurrentMacroChunks.Clear();

        UpdateChunks();
    }
}
