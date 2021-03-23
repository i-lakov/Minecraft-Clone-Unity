using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    #region Data members
    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    private bool _isActive;

    ChunkData chunkData;
    #endregion

    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;
    }

    public void Initialize()
    {
        chunkObject  = new GameObject();
        meshFilter   = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = $"Chunk {coord.x}, {coord.z}";
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);

        lock(World.Instance.chunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Add(this);
        }

        if(World.Instance.settings.enableAnimatedChunkLoading)
        {
            chunkObject.AddComponent<ChunkLoadAnimation>();
        }
    }

    public void UpdateChunk()
    {
        ClearMeshData();
        CalculateLight();

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if(World.Instance.blocktypes[chunkData.map[x, y, z].id].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        World.Instance.chunksToDraw.Enqueue(this);
    }

    void CalculateLight()
    {
        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for(int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.ChunkWidth; z++)
            {
                // Percentage of global illumination amount.
                float lightRay = 1f;

                for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--)
                {
                    VoxelState thisVoxel = chunkData.map[x, y, z];

                    if (thisVoxel.id > 0 && World.Instance.blocktypes[thisVoxel.id].transparency < lightRay)
                    {
                        lightRay = World.Instance.blocktypes[thisVoxel.id].transparency;
                    }

                    thisVoxel.globalLightPercent = lightRay;

                    chunkData.map[x, y, z] = thisVoxel;

                    if(lightRay > VoxelData.lightFalloff)
                    {
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        // Affect neighbouring blocks' light levels.
        while(litVoxels.Count > 0)
        {
            Vector3Int v = litVoxels.Dequeue();

            for(int p = 0; p < 6; p++)
            {
                Vector3 currentVoxel = v + VoxelData.faceChecks[p];
                Vector3Int neighbour = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                // If it's inside the chunk we're working on currently
                if (IsVoxelInChunk(neighbour.x, neighbour.y, neighbour.z))
                {
                    if(chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPercent < chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff)
                    {
                        chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPercent = chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if(chunkData.map[neighbour.x, neighbour.y, neighbour.z].globalLightPercent > VoxelData.lightFalloff)
                        {
                            litVoxels.Enqueue(neighbour);
                        }
                    }
                }
            }
        }
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
        {
            return false;
        }
        return true;
    }

    public void EditVoxel(Vector3 pos, byte newID)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.map[xCheck, yCheck, zCheck].id = newID;
        World.Instance.worldData.AddToModifiedChunkList(chunkData);

        lock(World.Instance.chunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Insert(0, this);

            // Update surrounding chunks, to prevent blank mesh sides.
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
        }
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 curVoxel = thisVoxel + VoxelData.faceChecks[p];

            if(!IsVoxelInChunk((int)curVoxel.x, (int)curVoxel.y, (int)curVoxel.z))
            {
                World.Instance.chunksToUpdate.Add(World.Instance.GetChunkFromVertor3(curVoxel + position));
            }
        }
    }

    VoxelState CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if(!IsVoxelInChunk(x, y, z))
        {
            return World.Instance.GetVoxelState(pos + position);
        }    

        return chunkData.map[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map[xCheck, yCheck, zCheck];
    }

    void UpdateMeshData(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockID = chunkData.map[x, y, z].id;
        //bool isTransparent = World.Instance.blocktypes[blockID].renderNeighbourFaces;

        for (int p = 0; p < 6; p++)
        {
            VoxelState neighbor = CheckVoxel(pos + VoxelData.faceChecks[p]);

            if(neighbor != null && World.Instance.blocktypes[neighbor.id].renderNeighbourFaces)
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for (int i = 0; i < 4; i++) 
                {
                    normals.Add(VoxelData.faceChecks[p]);
                }

                AddTexture(World.Instance.blocktypes[blockID].GetTextureID(p));

                float lightLevel = neighbor.globalLightPercent;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                if(!World.Instance.blocktypes[neighbor.id].renderNeighbourFaces)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                  transparentTriangles.Add(vertexIndex);
                  transparentTriangles.Add(vertexIndex + 1);
                  transparentTriangles.Add(vertexIndex + 2);
                  transparentTriangles.Add(vertexIndex + 2);
                  transparentTriangles.Add(vertexIndex + 1);
                  transparentTriangles.Add(vertexIndex + 3);
                }
                
                vertexIndex += 4;
            }
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;
    }

    public bool isActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            if(chunkObject != null)
            {
                chunkObject.SetActive(value);
            }
        }
    }

    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector3(x, y));
        uvs.Add(new Vector3(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector3(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector3(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    #region Constructors
    /// <summary>
    /// Default constructor, sets x & z to 0.
    /// </summary>
    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    /// <summary>
    /// General purpose constructor, taking x & y as parameters.
    /// </summary>
    /// <param name="_x"></param>
    /// <param name="_z"></param>
    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    /// <summary>
    /// Constructor, which takes in a Vector3 position as a parameter and works out the x & y values.
    /// </summary>
    /// <param name="pos"></param>
    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;
    }
    #endregion

    public bool Equals (ChunkCoord other)
    {
        if (other == null)
            return false;
        else if (other.x == x && other.z == z)
            return true;
        else
            return false;
    }
}

[System.Serializable]
public class VoxelState
{
    #region Data members
    public byte id;
    public float globalLightPercent;
    #endregion

    #region Constructors
    /// <summary>
    /// Default constructor.
    /// </summary>
    public VoxelState()
    {
        id = 0;
        globalLightPercent = 0f;
    }

    /// <summary>
    /// Constructor, which sets the id (byte) to the given value.
    /// </summary>
    /// <param name="_id"></param>
    public VoxelState(byte _id)
    {
        id = _id;
        globalLightPercent = 0f;
    }
    #endregion
}