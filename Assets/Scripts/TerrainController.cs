using System.Collections.Generic;
using System.Linq;

using Unity;
using UnityEngine;
using UnityEditor;

using NaughtyAttributes;

[ExecuteInEditMode]
public class TerrainController : MonoBehaviour
{
    public NoiseMapInfo NoiseMapInfo;
    private float timeSinceLastNoiseMapUpdate = 0;

    public AnimationCurve VarietyDistribution;

    public int MacroChunkViewDistance;
    public int LocalChunkViewDistance;

    public Material ChunkMaterial;
    public float WaterHeight;
    public Material WaterMaterial;

    [Min(0.001f)]
    public float LodFalloffMultiplier;

    Dictionary<Vector2, LocalChunk> CurrentLocalChunks = new Dictionary<Vector2, LocalChunk>();
    Dictionary<Vector2, LocalChunk> PreviousLocalChunks = new Dictionary<Vector2, LocalChunk>();
    public GameObject LocalChunkParent;

    Dictionary<Vector2, MacroChunk> CurrentMacroChunks = new Dictionary<Vector2, MacroChunk>();
    Dictionary<Vector2, MacroChunk> PreviousMacroChunks = new Dictionary<Vector2, MacroChunk>();
    public GameObject MacroChunkParent;

    public Utils.ChunkSizeOptions chunkSizeOptions;

    public Utils.NoiseType NoiseType;

    bool AutoUpdate = false;
    float AutoUpdateTime = 1;

    public bool PauseUpdate;

    public bool GenerateWithFalloff;
    [EnableIf("GenerateWithFalloff")]
    public AnimationCurve FalloffDistribution;

    public bool GenerateInfinitely;
    [DisableIf("GenerateInfinitely")]
    public bool UseLod;
    [DisableIf("GenerateInfinitely")]
    public int ChunksToGenerateInX;
    [DisableIf("GenerateInfinitely")]
    public int ChunksToGenerateInY;

    public bool ErodeTerrain;

    public bool EnableConsoleChunkUpdates;

    public Transform Target;

    public int FlythroughLength;

    public Vector3 FlythroughDirection;

    public float FlythroughSpeed;

    public bool StartFpsRecorder;

    bool recordingStarted;

    public int NumberOfSecondsToCount;

    float recordingTimer = -1;

    double[] fps;

    private void Awake()
    {
        ResetChunks();
    }

    // Start is called before the first frame update
    void Start()
    {
        ResetChunks();
        fps = new double[NumberOfSecondsToCount];
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMapInfo();
        UpdateFog();
        UpdateChunks();

        StartTest();
        CheckTestDone();
    }

    public void StartTest()
    {
        if (StartFpsRecorder)
        {
            fps = new double[NumberOfSecondsToCount];
            StartFpsRecorder = false;
            recordingTimer = 0;
            recordingStarted = true;
        }
    }

    public void CheckTestDone()
    {
        if (recordingStarted && recordingTimer < NumberOfSecondsToCount)
        {
            Target.position += FlythroughSpeed * Time.deltaTime * FlythroughDirection;
            fps[Mathf.FloorToInt(recordingTimer)] += 1;
            recordingTimer += Time.deltaTime;
        }
        else if (recordingStarted && recordingTimer >= NumberOfSecondsToCount)
        {
            Debug.Log(fps.Average());
            fps = new double[NumberOfSecondsToCount];
            recordingStarted = false;
        }
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
        //RenderSettings.fogStartDistance = MacroChunkViewDistance * 0.9f;
        //RenderSettings.fogEndDistance = MacroChunkViewDistance;
    }

    public void UpdateMapInfo()
    {
        (int, int) chunkSizes = Utils.ChunkSizes[chunkSizeOptions];

        NoiseMapInfo.LocalChunkSize = chunkSizes.Item1;
        NoiseMapInfo.MacroChunkSize = chunkSizes.Item2;

        NoiseMapInfo.Mode = (int)NoiseType;

        NoiseMapInfo.FalloffEnabled = GenerateWithFalloff ? 1 : 0;
    }

    public void GenerateMacroChunks()
    {
        Vector2 currentMacroChunkPosition = GetMacroChunkPositionAtCamera();

        for (int z = -MacroChunkViewDistance; z <= MacroChunkViewDistance; z++)
        {
            for (int x = -MacroChunkViewDistance; x <= MacroChunkViewDistance; x++)
            {
                Vector2 newChunkOriginPosition = currentMacroChunkPosition + (new Vector2(x, z) * NoiseMapInfo.MacroChunkSize);
                if (GenerateInfinitely 
                    || ((newChunkOriginPosition.x / NoiseMapInfo.MacroChunkSize) < ChunksToGenerateInX
                    && (newChunkOriginPosition.y / NoiseMapInfo.MacroChunkSize) < ChunksToGenerateInY
                    && newChunkOriginPosition.x >= 0
                    && newChunkOriginPosition.y >= 0))
                {
                    if (Vector2.Distance(currentMacroChunkPosition, newChunkOriginPosition) < MacroChunkViewDistance * NoiseMapInfo.MacroChunkSize)
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

        bool isIsland;
        if (GenerateWithFalloff)
        {
            System.Random random = new System.Random((int)(NoiseMapInfo.Seed * (newChunkOriginPosition.x + newChunkOriginPosition.y) / NoiseMapInfo.MacroChunkSize ));
            isIsland = (float)random.Next(0, 10000) / 100000 < NoiseMapInfo.IslandDensity;
        }
        else
        {
            isIsland = true;
        }

        if (PreviousMacroChunks.TryGetValue(newChunkOriginPosition, out currentChunk))
        {
            if ((currentChunk.ErosionThread != null && !currentChunk.ErosionThread.IsAlive))
            {
                currentChunk.IsEroded = true;
                currentChunk.ErosionThread = null;

                Debug.Log($"Chunk at {currentChunk.ChunkPosition} eroded in {currentChunk.Stopwatch.ElapsedMilliseconds}ms.");
                currentChunk.Stopwatch.Stop();
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

        for (int z = -LocalChunkViewDistance; z <= LocalChunkViewDistance; z++)
        {
            for (int x = -LocalChunkViewDistance; x <= LocalChunkViewDistance; x++)
            {
                Vector2 newChunkOriginPosition = currentChunkPosition + (new Vector2(x, z) * NoiseMapInfo.LocalChunkSize);

                Vector2 currentMacroChunkPosition = GetMacroChunkPositionAtPosition(newChunkOriginPosition);

                MacroChunk currentMacroChunk;

                if (PreviousMacroChunks.TryGetValue(currentMacroChunkPosition, out currentMacroChunk))
                {
                    if (Vector2.Distance(currentChunkPosition, newChunkOriginPosition) < LocalChunkViewDistance * NoiseMapInfo.LocalChunkSize 
                        && currentMacroChunk.IsTerrain && ( currentMacroChunk.IsEroded || !ErodeTerrain))
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

        int lod;
        if (!GenerateInfinitely && !UseLod)
        {
            lod = 0;
        }
        else
        {
            lod = Mathf.FloorToInt(Vector2.Distance(newChunkCentre, new Vector2(Target.position.x, Target.position.z)) / (NoiseMapInfo.LocalChunkSize * (1 / LodFalloffMultiplier)));
        }

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
