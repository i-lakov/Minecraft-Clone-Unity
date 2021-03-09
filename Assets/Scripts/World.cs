using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    #region Data members
    public int seed;
    public BiomeAttributes biome;

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
    List<Chunk> chunksToUpdate = new List<Chunk>();

    public GameObject debugScreen;
    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    private bool _inUI = false;
    #endregion

    private void Start()
    {
        Random.InitState(seed);

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight - 50f, (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        // Updating the chunks when the player moves to new chunk.
        if(!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if(!applyingModifiations)
        {
            ApplyModifications();
        }

        if(chunksToCreate.Count > 0)
        {
            CreateChunk();
        }

        if(chunksToUpdate.Count > 0)
        {
            UpdateChunks();
        }

        if(chunksToDraw.Count > 0)
        {
            lock(chunksToDraw)
            {
                if(chunksToDraw.Peek().isEditable)
                {
                    chunksToDraw.Dequeue().CreateMesh();
                }
            }
        }

        if(Input.GetKeyDown(KeyCode.F3))
        {
            debugScreen.SetActive(!debugScreen.activeSelf);
        }
    }

    void GenerateWorld()
    {
        for(int x = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; x < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - VoxelData.ViewDistanceInChunks; z < (VoxelData.WorldSizeInChunks / 2) + VoxelData.ViewDistanceInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        player.position = spawnPosition;
    }

    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        activeChunks.Add(c);
        chunks[c.x, c.z].Initialize();
    }

    void UpdateChunks()
    {
        bool updated = false;
        int index = 0;

        while(!updated && index < (chunksToUpdate.Count - 1))
        {
            if (chunksToUpdate[index].isEditable)
            {
                chunksToUpdate[index].UpdateChunk();
                chunksToUpdate.RemoveAt(index); 
                updated = true;
            }
            else index++;
        }
    }

    void ApplyModifications()
    {
        applyingModifiations = true;

        while(modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();
            if(queue == null)
            {
                // NULLREFERENCE FOR SOME REASON.
                Debug.Log("testQueue");
            }
            while (queue.Count > 0)
            {
                VoxelMod vm = queue.Dequeue();
                ChunkCoord c = GetChunkCoordFromVector3(vm.position);

                if (chunks[c.x, c.z] == null)
                {
                    chunks[c.x, c.z] = new Chunk(c, this, true);
                    activeChunks.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(vm);

                if (!chunksToUpdate.Contains(chunks[c.x, c.z]))
                {
                    chunksToUpdate.Add(chunks[c.x, c.z]);
                }
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
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for(int x = coord.x - VoxelData.ViewDistanceInChunks; x < coord.x + VoxelData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - VoxelData.ViewDistanceInChunks; z < coord.z + VoxelData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorld (new ChunkCoord (x, z)))
                {
                    if(chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        foreach(ChunkCoord cc in previouslyActiveChunks)
        {
            chunks[cc.x, cc.z].isActive = false;
        }
    }

    public bool CheckForVoxel (Vector3 pos)
    {
        ChunkCoord curChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(curChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
        {
            return false;
        }

        if (chunks[curChunk.x, curChunk.z] != null && chunks[curChunk.x, curChunk.z].isEditable)
        {
            return blocktypes[chunks[curChunk.x, curChunk.z].GetVoxelFromGlobalVector3(pos)].isSolid;
        }

        return blocktypes[GetVoxel(pos)].isSolid;
    }

    public bool CheckForVoxelTransparent(Vector3 pos)
    {
        ChunkCoord curChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(curChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
        {
            return false;
        }

        if (chunks[curChunk.x, curChunk.z] != null && chunks[curChunk.x, curChunk.z].isEditable)
            return blocktypes[chunks[curChunk.x, curChunk.z].GetVoxelFromGlobalVector3(pos)].isTransparent;

        return blocktypes[GetVoxel(pos)].isTransparent;
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
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        // -- IMMUTABLES --

        // Outside the world => air
        if(!IsVoxelInWorld(pos))
            return 0;

        // Bottom of chunk => bedrock
        if (yPos == 0) 
            return 1;

        // -- BASIC TERRAIN PASS --

        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale) + biome.solidGroundHeight);
        byte voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = 5;
        else if (yPos > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        // -- SECOND PASS --

        if (voxelValue == 2)
        {
            foreach(Lode l in biome.lodes)
            {
                if(yPos > l.minHeight && yPos < l.maxHeight)
                {
                    if(Noise.Get3DPerlin(pos, l.noiseOffset, l.scale, l.threshold))
                    {
                        voxelValue = l.blockID;
                    }
                }
            }
        }

        // -- TREE PASS --

        if(yPos == terrainHeight)
        {
            if(Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treeZoneScale) > biome.treeZoneTreshold)
            {
                if(Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.treePlacementScale) > biome.treePlacementThreshold)
                {
                    var test = Structure.MakeTree(pos, biome.minTreeHeight, biome.maxTreeHeight);
                    modifications.Enqueue(test);
                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld (ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
        {
            return true;
        }

        return false;
    }

    bool IsVoxelInWorld (Vector3 pos)
    {
        if(pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
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
    public bool isTransparent;
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
        switch(faceIndex)
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