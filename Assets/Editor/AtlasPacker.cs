using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class AtlasPacker : EditorWindow
{
    int blockSize = 16;
    int atlasSizeInBlocks = 16;
    int atlasSize;

    Object[] rawTextures = new Object[256];
    List<Texture2D> sorted = new List<Texture2D>();
    Texture2D atlas;

    [MenuItem("Vankocraft/Atlas Creator")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockSize * atlasSizeInBlocks;

        GUILayout.Label("Vankocraft Texture Atlas Creator", EditorStyles.boldLabel);
        blockSize = EditorGUILayout.IntField("Block size", blockSize);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas size", atlasSizeInBlocks);

        GUILayout.Label(atlas);

        if(GUILayout.Button("Load textures"))
        {
            LoadTextures();
            CreateAtlas();

            Debug.Log("AtlasCreator: Textures loaded.");
        }

        if(GUILayout.Button("Clear textures"))
        {
            atlas = new Texture2D(atlasSize, atlasSize);

            Debug.Log("AtlasCreator: Textures cleared.");
        }

        if(GUILayout.Button("Save texture atlas"))
        {
            byte[] b = atlas.EncodeToPNG();

            try
            {
                File.WriteAllBytes(Application.dataPath + "/Textures/Packed_atlas.png", b);
            }
            catch
            {
                Debug.Log("AtlasCreator: Saving to file failed.");
            }
        }
    }

    void LoadTextures()
    {
        sorted.Clear();
        rawTextures = Resources.LoadAll("AtlasPacker", typeof(Texture2D));

        int index = 0;
        foreach(Object o in rawTextures)
        {
            Texture2D t = (Texture2D)o;
            if (t.width == blockSize && t.height == blockSize)
            {
                sorted.Add(t);
            }
            else Debug.LogError($"AtlasCreator: Incorrect size of texture. {o.name} not loaded.");
            
            index++;
        }
        Debug.Log($"AtlasCreator: {sorted.Count} textures loaded.");
    }

    void CreateAtlas()
    {
        atlas = new Texture2D(atlasSize, atlasSize);
        Color[] pixels = new Color[atlasSize * atlasSize];

        for (int x = 0; x < atlasSize; x++)
        {
            for (int y = 0; y < atlasSize; y++)
            {
                (int x, int y) currentBlock = (x / blockSize, y / blockSize);
                int index = currentBlock.y * atlasSizeInBlocks + currentBlock.x;

                //(int x, int y) currentPixel = (x - (currentBlock.x * blockSize), y - (currentBlock.y * blockSize));
                if (index < sorted.Count)
                {
                    pixels[(atlasSize - y - 1) * atlasSize + x] = sorted[index].GetPixel(x, blockSize - y - 1);
                }
                else pixels[(atlasSize - y - 1) * atlasSize + x] = new Color(0f, 0f, 0f, 0f);
            }
        }

        atlas.SetPixels(pixels);
        atlas.Apply();
    }
}
