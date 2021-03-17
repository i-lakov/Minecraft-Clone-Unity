using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    #region Data members
    public int cloudHeight = 110; // How much above the ground the clouds are.
    public int cloudDepth = 4; // Height/Depth of the cloud blocks.

    [SerializeField] 
    private Texture2D cloudPattern = null;
    [SerializeField]
    private Material cloudMaterial = null;
    [SerializeField]
    private World world = null;

    bool[,] cloudData; // Stores where clouds are.

    int cloudTexWidth;

    int cloudTileSize;
    Vector3Int offset;

    Dictionary<Vector2Int, GameObject> clouds = new Dictionary<Vector2Int, GameObject>();
    #endregion

    private void Start()
    {
        cloudTexWidth = cloudPattern.width;
        cloudTileSize = VoxelData.ChunkWidth;
        offset = new Vector3Int(-(cloudTexWidth / 2), 0, -(cloudTexWidth / 2));

        transform.position = new Vector3(VoxelData.WorldCenter, cloudHeight, VoxelData.WorldCenter);

        LoadCloudData();
        CreateClouds();
    }

    private void LoadCloudData()
    {
        cloudData = new bool[cloudTexWidth, cloudTexWidth];
        Color[] cloudTex = cloudPattern.GetPixels();

        // If opacity > 0, then there's a cloud there, otherwise - not.
        for (int x = 0; x < cloudTexWidth; x++)
        {
            for (int y = 0; y < cloudTexWidth; y++)
            {
                cloudData[x, y] = (cloudTex[y * cloudTexWidth + x].a > 0);
            }
        }
    }

    /// <summary>
    /// Creates initial set of clouds.
    /// </summary>
    private void CreateClouds()
    {
        if (world.settings.cloudStyle == CloudStyle.OFF)
        {
            return;
        }

        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int y = 0; y < cloudTexWidth; y += cloudTileSize)
            {
                Mesh mesh;
                if (world.settings.cloudStyle == CloudStyle.FAST)
                {
                    mesh = CreateCloudMeshFast(x, y);
                }
                else mesh = CreateCloudMeshFancy(x, y);

                Vector3 position = new Vector3(x, cloudHeight, y);
                position += transform.position - new Vector3(cloudTexWidth / 2f, 0f, cloudTexWidth / 2f); // Start clouds centered
                position.y = cloudHeight; // Sets clouds to the correct height.
                clouds.Add(CloudTilePosFromVector3(position), CreateCloudTile(mesh, position));
            }
        }
    }

    /// <summary>
    /// Updates visible clouds.
    /// </summary>
    public void UpdateClouds()
    {
        if(world.settings.cloudStyle == CloudStyle.OFF)
        {
            return;
        }

        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int y = 0; y < cloudTexWidth; y += cloudTileSize)
            {
                Vector3 position = world.player.position + new Vector3(x, 0, y) + offset;
                position = new Vector3(RoundToCloud(position.x), cloudHeight, RoundToCloud(position.z));
                Vector2Int cloudPosition = CloudTilePosFromVector3(position);

                clouds[cloudPosition].transform.position = position;
            }
        }
    }

    private int RoundToCloud(float input)
    {
        return Mathf.FloorToInt(input / cloudTileSize) * cloudTileSize;
    }

    /// <summary>
    /// Checks whether a cloud exists at the specified point.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    private bool CheckCloudData(Vector3Int point)
    {
        // Clouds are 2D, so there can't be a cloud below or above cloud level.
        if(point.y != 0)
        {
            return false;
        }

        int x = point.x;
        int z = point.z;

        // Clouds wrap arount infinitely, so do the values.
        if(point.x < 0)
        {
            x = cloudTexWidth - 1;
        }
        if(point.x > cloudTexWidth - 1)
        {
            x = 0;
        }
        
        if(point.z < 0)
        {
            z = cloudTexWidth - 1;
        }
        if(point.z > cloudTexWidth - 1)
        {
            z = 0;
        }

        return cloudData[x, z];
    }

    /// <summary>
    /// Handles the creation of 2D/"Fast" clouds.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Mesh CreateCloudMeshFast(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for(int xIncr = 0; xIncr < cloudTileSize; xIncr++)
        {
            for (int zIncr = 0; zIncr < cloudTileSize; zIncr++)
            {
                int xVal = x + xIncr;
                int zVal = z + zIncr;

                if(cloudData[xVal, zVal])
                {
                    vertices.Add(new Vector3(xIncr, 0, zIncr));
                    vertices.Add(new Vector3(xIncr, 0, zIncr + 1));
                    vertices.Add(new Vector3(xIncr + 1, 0, zIncr + 1));
                    vertices.Add(new Vector3(xIncr + 1, 0, zIncr));

                    // Clouds are facing down only.
                    for (int i = 0; i < 4; i++)
                    {
                        normals.Add(Vector3.down);
                    }

                    // First triangle.
                    triangles.Add(vertCount + 1);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 2);

                    // Second triangle.
                    triangles.Add(vertCount + 2);
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 3);

                    vertCount += 4;
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        return mesh;
    }

    /// <summary>
    /// Handles creation of 3D/"Luxorious" clouds.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Mesh CreateCloudMeshFancy(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int xIncr = 0; xIncr < cloudTileSize; xIncr++)
        {
            for (int zIncr = 0; zIncr < cloudTileSize; zIncr++)
            {
                int xVal = x + xIncr;
                int zVal = z + zIncr;

                if (cloudData[xVal, zVal])
                {
                    // Checking "neighbours".
                    for(int p = 0; p < 6; p++)
                    {
                        // If there is no cloud next to it, draw the face.
                        if(!CheckCloudData(new Vector3Int(xVal, 0, zVal) + VoxelData.faceChecks[p]))
                        {
                            for(int i = 0; i < 4; i++)
                            {
                                Vector3 vert = new Vector3Int(xIncr, 0, zIncr);
                                vert += VoxelData.voxelVerts[VoxelData.voxelTris[p, i]];
                                vert.y *= cloudDepth;
                                vertices.Add(vert);
                            }

                            for (int i = 0; i < 4; i++)
                            {
                                normals.Add(VoxelData.faceChecks[p]);
                            }

                            triangles.Add(vertCount);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 2);
                            triangles.Add(vertCount + 1);
                            triangles.Add(vertCount + 3);

                            vertCount += 4;
                        }
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        return mesh;
    }

    /// <summary>
    /// Creates a single cloud tile.
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="pos"></param>
    /// <returns></returns>
    private GameObject CreateCloudTile(Mesh mesh, Vector3 pos)
    {
        GameObject newCloudTile = new GameObject();
        newCloudTile.transform.position = pos;
        newCloudTile.transform.parent = transform;
        newCloudTile.name = $"Cloud {pos.x}, {pos.z}";
        MeshFilter filter = newCloudTile.AddComponent<MeshFilter>();
        MeshRenderer renderer = newCloudTile.AddComponent<MeshRenderer>();

        renderer.material = cloudMaterial;
        filter.mesh = mesh;

        return newCloudTile;
    }

    private Vector2Int CloudTilePosFromVector3 (Vector3 pos)
    {
        return new Vector2Int(CloudTileCoordFromFloat(pos.x), CloudTileCoordFromFloat(pos.z));
    }

    private int CloudTileCoordFromFloat (float value)
    {
        float a = value / (float)cloudTexWidth;
        a -= Mathf.FloorToInt(a);
        int b = Mathf.FloorToInt((float)cloudTexWidth * a);

        return b;
    }
}

public enum CloudStyle
{
    OFF,
    FAST,
    FANCY
}
