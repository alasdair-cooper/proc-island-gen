using System.Collections.Generic;
using System.Linq;

using Unity;
using UnityEngine;
using UnityEditor;

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

    public float LodFalloffMultiplier;

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
    public bool ErodeTerrain;

    public bool EnableConsoleChunkUpdates;

    private void Awake()
    {
        ResetChunks();
    }

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

        bool isIsland = Mathf.PerlinNoise((newChunkOriginPosition.x + 0.01f) / (NoiseMapInfo.MacroChunkSize), (newChunkOriginPosition.y + 0.01f) / (NoiseMapInfo.MacroChunkSize)) > NoiseMapInfo.IslandDensity;
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
                currentChunk = new MacroChunk(newChunkOriginPosition, NoiseMapInfo, WaterMaterial, ChunkMaterial, WaterHeight, MacroChunkParent.transform, VarietyDistribution, FalloffDistribution, isIsland, ErodeTerrain && GenerateWithFalloff);
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
                        && ((currentMacroChunk.IsTerrain && currentMacroChunk.IsEroded) || !ErodeTerrain))
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
            DestroyImmediate(chunk.ChunkObject.gameObject);
        }

        PreviousLocalChunks = CurrentLocalChunks.ToDictionary(entry => entry.Key, entry => entry.Value); 
        CurrentLocalChunks.Clear();
    }

    public void GenerateSingleLocalChunk(Vector2 newChunkOriginPosition, float[] heightmap, int initialPoint)
    {
        LocalChunk currentChunk;
        float[] chunkHeightmap = SliceHeightmap(heightmap, initialPoint, NoiseMapInfo.LocalChunkSize + 1, NoiseMapInfo.MacroChunkSize);
        Vector2 newChunkCentre = newChunkOriginPosition + new Vector2(1, 1) * 0.5f * NoiseMapInfo.LocalChunkSize;
        int lod = Mathf.CeilToInt(Vector2.Distance(newChunkCentre, new Vector2(transform.position.x, transform.position.z)) / (NoiseMapInfo.LocalChunkSize * (1 / LodFalloffMultiplier)));
        if (PreviousLocalChunks.TryGetValue(newChunkOriginPosition, out currentChunk))
        {
            currentChunk.UpdateChunk(chunkHeightmap, lod);
            CurrentLocalChunks.Add(newChunkOriginPosition, currentChunk);
            PreviousLocalChunks.Remove(newChunkOriginPosition);
        }
        else
        {
            if (EnableConsoleChunkUpdates)
            {
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                currentChunk = new LocalChunk(chunkHeightmap, newChunkOriginPosition, NoiseMapInfo, lod, ChunkMaterial, LocalChunkParent.transform, VarietyDistribution, FalloffDistribution);
                stopwatch.Stop();
                Debug.Log($"Local chunk at {newChunkOriginPosition} was created in {stopwatch.ElapsedMilliseconds}ms.");
            }
            else
            {
                currentChunk = new LocalChunk(chunkHeightmap, newChunkOriginPosition, NoiseMapInfo, lod, ChunkMaterial, LocalChunkParent.transform, VarietyDistribution, FalloffDistribution);
            }

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

    public void ResetChunks()
    {
        DestroyImmediate(LocalChunkParent);
        DestroyImmediate(MacroChunkParent);

        GameObject[] localParents = GameObject.FindGameObjectsWithTag("LocalChunkParent");

        for (int i = 0; i < localParents.Length; i++)
        {
            DestroyImmediate(localParents[i]);
        }

        GameObject[] macroParents = GameObject.FindGameObjectsWithTag("MacroChunkParent");

        for (int i = 0; i < localParents.Length; i++)
        {
            DestroyImmediate(macroParents[i]);
        }

        LocalChunkParent = new GameObject("Local Chunk Parent");
        LocalChunkParent.tag = "LocalChunkParent";
        MacroChunkParent = new GameObject("Macro Chunk Parent");
        MacroChunkParent.tag = "MacroChunkParent";

        PreviousLocalChunks.Clear();
        PreviousMacroChunks.Clear();

        CurrentLocalChunks.Clear();
        CurrentMacroChunks.Clear();

        UpdateChunks();
    }
}
