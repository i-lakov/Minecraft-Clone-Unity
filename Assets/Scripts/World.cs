﻿using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;

public class World : MonoBehaviour
{
    #region Data members
    public Settings settings;

    [Header("World generation values")]
    public BiomeAttributes[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;
    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifiations = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();

    public GameObject debugScreen;
    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    public GameObject loadingScreenWindow;
    private float loadingScreenTimer = 8f; // Loading screen will be drawn for 8 seconds each time the game starts.
    private bool inLoadingScreen = true;

    private bool _inUI = false;

    public Clouds clouds;

    Thread chunkUpdateThread;
    public object chunkUpdateThreadLock = new object();
    public object chunkListThreadLock = new object();

    public int timeHour;
    public float timeMin;
    public float timeSpeedModifier = 1;

    public WorldData worldData;

    public string appPath;

    private static World _instance;
    public static World Instance { get { return _instance; } }
    #endregion

    private void Awake()
    {
        // If there's more than one World Instance, delete this one.
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else _instance = this;

        appPath = Application.persistentDataPath;
    }

    private void Start()
    {
        Debug.Log($"Generating world using seed <{VoxelData.seed}>.");

        worldData = SaveSys.LoadWorld("Test world");

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        Random.InitState(VoxelData.seed);

        Shader.SetGlobalFloat("MinGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("MaxGlobalLightLevel", VoxelData.maxLightLevel);

        if (settings.enableThreading)
        {
            chunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            chunkUpdateThread.Start();
        }

        // New world time begins at noon.
        timeHour = 12;
        timeMin = 00;
        globalLightLevel = 1f;

        SetGlobalLightValue();

        spawnPosition = new Vector3(VoxelData.WorldCenter, VoxelData.ChunkHeight - 50f, VoxelData.WorldCenter);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private void Update()
    {
        // Update global light level, depending on what time it is.
        TimeCycle();
        SetGlobalLightValue();

        if (inLoadingScreen)
        {
            loadingScreenTimer -= Time.deltaTime;
            if (loadingScreenTimer < 0)
            {
                loadingScreenWindow.SetActive(false);
                inLoadingScreen = false;
            }
        }

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        // Updating the chunks when the player moves to new chunk.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if (chunksToCreate.Count > 0)
        {
            CreateChunk();
        }

        if (chunksToDraw.Count > 0)
        {
            chunksToDraw.Dequeue().CreateMesh();
        }

        if (!settings.enableThreading)
        {
            if (!applyingModifiations)
            {
                ApplyModifications();
            }

            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SaveSys.SaveWorld(worldData);
        }
    }

    #region In-game time and day/night cycle
    /// <summary>
    /// Handles the time keeping in 24 hour format.
    /// </summary>
    private void TimeCycle()
    {
        if (timeMin < 59)
        {
            timeMin += 1 * Time.deltaTime * timeSpeedModifier;
        }
        else if (timeMin >= 59 && timeHour < 23)
        {
            timeMin = 0;
            timeHour++;
            AdjustGlobalLightLevel(timeHour);
        }
        else if (timeMin >= 59 && timeHour >= 23)
        {
            timeMin = 0;
            timeHour = 0;
            AdjustGlobalLightLevel(timeHour);
        }
    }

    /// <summary>
    /// Adjusts the global light level, based on current time.
    /// </summary>
    private void AdjustGlobalLightLevel(int _timeHour)
    {
        switch (_timeHour)
        {
            case 23: break;
            case 0: break;
            case 1: break;
            case 2: break;
            case 3: break;
            case 4: break;
            case 5: globalLightLevel += 0.125f; break;
            case 6: globalLightLevel += 0.125f; break;
            case 7: globalLightLevel += 0.125f; break;
            case 8: globalLightLevel += 0.125f; break;
            case 9: globalLightLevel += 0.125f; break;
            case 10: globalLightLevel += 0.125f; break;
            case 11: globalLightLevel += 0.125f; break;
            case 12: globalLightLevel += 0.125f; break;
            case 13: break;
            case 14: break;
            case 15: globalLightLevel -= 0.125f; break;
            case 16: globalLightLevel -= 0.125f; break;
            case 17: globalLightLevel -= 0.125f; break;
            case 18: globalLightLevel -= 0.125f; break;
            case 19: globalLightLevel -= 0.125f; break;
            case 20: globalLightLevel -= 0.125f; break;
            case 21: globalLightLevel -= 0.125f; break;
            case 22: globalLightLevel -= 0.125f; break;
        }
    }
    #endregion

    /// <summary>
    /// Sets a certain value as the global light level.
    /// </summary>
    public void SetGlobalLightValue()
    {
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }

    void LoadWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; z++)
            {
                worldData.LoadChunk(new Vector2Int(x, z));
            }
        }
    }

    void GenerateWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++)
            {
                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x, z] = new Chunk(newChunk);
                chunksToCreate.Add(newChunk);
            }
        }

        player.position = spawnPosition;
        CheckViewDistance();
    }

    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].Initialize();
    }

    void UpdateChunks()
    {

        lock (chunkUpdateThreadLock)
        {
            chunksToUpdate[0].UpdateChunk();

            if (!activeChunks.Contains(chunksToUpdate[0].coord))
            {
                activeChunks.Add(chunksToUpdate[0].coord);
            }

            chunksToUpdate.RemoveAt(0);
        }
    }

    void ThreadedUpdate()
    {
        while (true)
        {
            if (!applyingModifiations)
            {
                ApplyModifications();
            }

            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }
        }
    }

    private void OnDisable()
    {
        if (settings.enableThreading)
        {
            chunkUpdateThread.Abort();
        }
    }

    void ApplyModifications()
    {
        applyingModifiations = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();
            while (queue.Count > 0)
            {
                VoxelMod vm = queue.Dequeue();

                worldData.SetVoxel(vm.position, vm.id);
            }
        }

        applyingModifiations = false;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVertor3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        return chunks[x, z];
    }

    void CheckViewDistance()
    {
        clouds.UpdateClouds();

        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {
                ChunkCoord chunkCoord = new ChunkCoord(x, z);

                if (IsChunkInWorld(chunkCoord))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(chunkCoord);
                        chunksToCreate.Add(chunkCoord);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(chunkCoord);
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(chunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        foreach (ChunkCoord cc in previouslyActiveChunks)
        {
            chunks[cc.x, cc.z].isActive = false;
        }
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        VoxelState voxel = worldData.GetVoxel(pos);

        if (blocktypes[voxel.id].isSolid)
        {
            return true;
        }
        else return false;
    }

    public VoxelState GetVoxelState(Vector3 pos)
    {
        return worldData.GetVoxel(pos);
    }

    public bool inUI
    {
        get { return _inUI; }
        set
        {
            _inUI = value;

            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        //      -- IMMUTABLES --

        // Outside the world => air
        if (!IsVoxelInWorld(pos))
            return 0;

        // Bottom of chunk => bedrock
        if (yPos == 0)
            return 1;

        //      -- BIOME SELECT --

        int solidGroundHeight = 42; // always solid below this level
        float sumOfHeights = 0f;
        int counter = 0;
        (float weight, int biome_index) strongestWeight = (0, 0);

        for (int i = 0; i < biomes.Length; i++)
        {
            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);
            // Assign a "strongest" weight.
            if (weight > strongestWeight.weight)
            {
                strongestWeight.weight = weight;
                strongestWeight.biome_index = i;
            }

            // Get biome's terrain height and mult. by weigth.
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;

            if (height > 0)
            {
                sumOfHeights += height;
                counter++;
            }
        }

        BiomeAttributes biome = biomes[strongestWeight.biome_index];
        sumOfHeights /= counter;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);

        //      -- TERRAIN PASS --

        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = biome.surfaceBlock;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlock;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        //      -- SECOND PASS --

        if (voxelValue == 2)
        {
            foreach (Lode l in biome.lodes)
            {
                if (yPos > l.minHeight && yPos < l.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, l.noiseOffset, l.scale, l.threshold))
                    {
                        voxelValue = l.blockID;
                    }
                }
            }
        }

        //      -- FLORA PASS --

        if (yPos == terrainHeight && biome.placeBigFlora)
        {
            if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.bigFloraZoneScale) > biome.bigFloraZoneTreshold)
            {
                if (Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.bigFloraPlacementScale) > biome.bigFloraPlacementThreshold)
                {
                    var test = Structure.GenerateBigFlora(biome.majorFloraIndex, pos, biome.minHeight, biome.maxHeight);
                    modifications.Enqueue(test);
                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
        {
            return true;
        }

        return false;
    }

    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
        {
            return true;
        }

        return false;
    }
}

[System.Serializable]
public class BlockType
{
    #region Data members
    public string blockName;
    public bool isSolid;
    public bool renderNeighbourFaces;
    public float transparency;
    public byte stackSize;
    public Sprite icon;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;
    #endregion

    // Back, Front, Top, Bottom, Left, Right
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    #region Constructors
    /// <summary>
    /// Default constructor.
    /// </summary>
    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    /// <summary>
    /// General purpose constructor.
    /// </summary>
    /// <param name="_position"></param>
    /// <param name="_id"></param>
    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
    #endregion
}

[System.Serializable]
public class Settings
{
    [Header("Game data")]
    public string version = "1.23";

    [Header("Performance")]
    public int loadDistance = 16;
    public int viewDistance = 8;
    public bool enableThreading = true;
    public CloudStyle cloudStyle = CloudStyle.FAST;
    public bool enableAnimatedChunkLoading = true;

    [Header("Controls")]
    [Range(1f, 20f)]
    public float mouseSensitivity = 2f;
}