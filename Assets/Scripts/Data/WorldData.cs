using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldData
{
    #region Data members
    public string worldName = "Test world";
    public int seed;

    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();
    #endregion

    #region Constructors
    public WorldData (string _worldName, int _seed)
    {
        worldName = _worldName;
        seed = _seed;
    }

    public WorldData (WorldData _wd)
    {
        worldName = _wd.worldName;
        seed = _wd.seed;
    }
    #endregion

    public void AddToModifiedChunkList(ChunkData chunk)
    {
        if(!modifiedChunks.Contains(chunk))
        {
            modifiedChunks.Add(chunk);
        }
    }

    public ChunkData RequestChunk (Vector2Int coord, bool toCreate)
    {
        ChunkData chunkData;

        lock(World.Instance.chunkListThreadLock)
        {
            if (chunks.ContainsKey(coord))
            {
                chunkData = chunks[coord];
            }
            else if (!toCreate)
            {
                chunkData = null;
            }
            else
            {
                LoadChunk(coord);
                chunkData = chunks[coord];
            }
        }

        return chunkData;
    }

    /// <summary>
    /// Loads the chunk in memory, if it's not loaded already.
    /// </summary>
    /// <param name="coord"></param>
    public void LoadChunk(Vector2Int coord)
    {
        if(chunks.ContainsKey(coord))
        {
            return;
        }

        ChunkData chunk = SaveSys.LoadChunk(worldName, coord);
        if(chunk != null)
        {
            chunks.Add(coord, chunk);
            return;
        }

        chunks.Add(coord, new ChunkData(coord));
        chunks[coord].Populate();
    }

    /// <summary>
    /// A check to see if the voxel is inside the world.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Modifies the given voxel in the world.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="value"></param>
    public void SetVoxel(Vector3 pos, byte value)
    {
        if (!IsVoxelInWorld(pos))
        {
            return;
        }

        // Get the ChunkCoord of given voxel.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Reverse that to get the actual position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Does the chunk exist? If not, we need to create it.
        ChunkData chunkData = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)(pos.y), (int)(pos.z - z));
        chunkData.map[voxel.x, voxel.y, voxel.z].id = value;

        // Assign this chunk in the list to save.
        AddToModifiedChunkList(chunkData);
    }

    public VoxelState GetVoxel(Vector3 pos)
    {
        if (!IsVoxelInWorld(pos))
        {
            return null;
        }

        // Get the ChunkCoord of given voxel.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Reverse that to get the actual position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Does the chunk exist? If not, we need to create it.
        ChunkData chunkData = RequestChunk(new Vector2Int(x, z), true);

        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)(pos.y), (int)(pos.z - z));
        return chunkData.map[voxel.x, voxel.y, voxel.z];
    }
}
