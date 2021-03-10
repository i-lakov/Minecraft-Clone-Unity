using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    public Vector3 position;

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
    World world;

    private bool _isActive;
    private bool isVoxelMapPopulated = false;
    private bool threadLocked = false;
    #endregion

    public Chunk(ChunkCoord _coord, World _world, bool genOnLoad)
    {
        coord = _coord;
        world = _world;
        isActive = true;

        if(genOnLoad) Initialize();
    }

    public void Initialize()
    {
        chunkObject  = new GameObject();
        meshFilter   = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = $"Chunk {coord.x}, {coord.z}";
        position = chunkObject.transform.position;

        if (world.enableThreading)
        {
            Thread thread = new Thread(new ThreadStart(PopulateVoxelMap));
            thread.Start();
        }
        else PopulateVoxelMap();
    }

    void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));
                }
            }
        }

        _UpdateChunk();
        isVoxelMapPopulated = true;
    }

    public void UpdateChunk()
    {
        if (world.enableThreading)
        {
            Thread thread = new Thread(new ThreadStart(_UpdateChunk));
            thread.Start();
        }
        else _UpdateChunk();
    }

    private void _UpdateChunk()
    {
        threadLocked = true;

        while(modifications.Count > 0)
        {
            lock(modifications)
            {
                VoxelMod vm = modifications.Dequeue();
                if (vm == null) Debug.Log("VM ERROR");
                Vector3 pos = vm.position -= position;
                
                lock(voxelMap)
                {
                    voxelMap[(int)pos.x, (int)pos.y, (int)pos.z].id = vm.id;
                }
            }   
        }

        ClearMeshData();
        CalculateLight();

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if(world.blocktypes[voxelMap[x, y, z].id].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }

        lock(world.chunksToDraw)
        {
            world.chunksToDraw.Enqueue(this);
        }

        threadLocked = false;
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
                    VoxelState thisVoxel = voxelMap[x, y, z];

                    if (thisVoxel.id > 0 && world.blocktypes[thisVoxel.id].transparency < lightRay)
                    {
                        lightRay = world.blocktypes[thisVoxel.id].transparency;
                    }

                    thisVoxel.globalLightPercent = lightRay;

                    voxelMap[x, y, z] = thisVoxel;

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
                    if(voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercent < voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff)
                    {
                        voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercent = voxelMap[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if(voxelMap[neighbour.x, neighbour.y, neighbour.z].globalLightPercent > VoxelData.lightFalloff)
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

        voxelMap[xCheck, yCheck, zCheck].id = newID;

        // Update surrounding chunks, to prevent blank mesh sides.
        UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        _UpdateChunk();
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 curVoxel = thisVoxel + VoxelData.faceChecks[p];

            if(!IsVoxelInChunk((int)curVoxel.x, (int)curVoxel.y, (int)curVoxel.z))
            {
                world.GetChunkFromVertor3(curVoxel + position).UpdateChunk();
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
            return world.GetVoxelState(pos + position);
        }    

        return voxelMap[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return voxelMap[xCheck, yCheck, zCheck];
    }

    void UpdateMeshData(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockID = voxelMap[x, y, z].id;
        //bool isTransparent = world.blocktypes[blockID].renderNeighbourFaces;

        for (int p = 0; p < 6; p++)
        {
            VoxelState neighbor = CheckVoxel(pos + VoxelData.faceChecks[p]);

            if(neighbor != null && world.blocktypes[neighbor.id].renderNeighbourFaces)
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                AddTexture(world.blocktypes[blockID].GetTextureID(p));

                float lightLevel = neighbor.globalLightPercent;

                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));
                colors.Add(new Color(0, 0, 0, lightLevel));

                //if(!isTransparent)
                //{
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                //}
                //else
                //{
                //  transparentTriangles.Add(vertexIndex);
                //  transparentTriangles.Add(vertexIndex + 1);
                //  transparentTriangles.Add(vertexIndex + 2);
                //  transparentTriangles.Add(vertexIndex + 2);
                //  transparentTriangles.Add(vertexIndex + 1);
                //  transparentTriangles.Add(vertexIndex + 3);
                //}
                
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

        mesh.RecalculateNormals();

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

    public bool isEditable
    {
        get
        {
            if (!isVoxelMapPopulated || threadLocked)
            {
                return false;
            }
            else return true;
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