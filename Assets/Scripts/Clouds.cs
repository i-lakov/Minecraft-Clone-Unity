using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    #region Data members
    public int cloudHeight = 100;

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

    private void CreateClouds()
    {
        for (int x = 0; x < cloudTexWidth; x += cloudTileSize)
        {
            for (int y = 0; y < cloudTexWidth; y += cloudTileSize)
            {
                Vector3 position = new Vector3(x, cloudHeight, y);
                clouds.Add(CloudTilePosFromVector3(position), CreateCloudTile(CreateCloudMesh(x, y), position));
            }
        }
    }

    public void UpdateClouds()
    {
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

    private Mesh CreateCloudMesh(int x, int z)
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
