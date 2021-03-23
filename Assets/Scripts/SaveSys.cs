using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using UnityEngine;

public static class SaveSys
{
    /// <summary>
    /// Saves the world to an external file.
    /// </summary>
    /// <param name="world"></param>
    public static void SaveWorld(WorldData world)
    {
        // Get the save path.
        string savePath = World.Instance.appPath + "/saves/" + world.worldName + "/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        Debug.Log($"Saving {world.worldName}");
        // Debug.Log(savePath);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + "world.world", FileMode.Create);

        formatter.Serialize(stream, world);
        stream.Close();

        Thread thread = new Thread(() => SaveChunks(world));
        thread.Start();
    }

    /// <summary>
    /// Saves all chunks.
    /// </summary>
    /// <param name="world"></param>
    public static void SaveChunks(WorldData world)
    {
        // Duplicating modified chunks list and using the new one, to prevent changes while game is saving.
        List<ChunkData> chunks = new List<ChunkData>(world.modifiedChunks);
        world.modifiedChunks.Clear();

        int count = 0;
        foreach (ChunkData chunk in chunks)
        {
            SaveSys.SaveChunk(chunk, world.worldName);
            count++;
        }
        Debug.Log($"{count} chunks have been saved.");
    }

    /// <summary>
    /// Loads a world from an external file.
    /// </summary>
    /// <param name="worldName"></param>
    /// <param name="seed"></param>
    /// <returns></returns>
    public static WorldData LoadWorld(string worldName, int seed = 0)
    {
        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/";

        if (File.Exists(loadPath + "world.world"))
        {
            Debug.Log($"{worldName} found. Loading...");

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + "world.world", FileMode.Open);

            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();

            return new WorldData(world);
        }
        else
        {
            Debug.Log($"{worldName} not found. Creating a new world.");

            WorldData world = new WorldData(worldName, seed);
            SaveWorld(world);

            return world;
        }
    }

    /// <summary>
    /// Saves an inidividual chunk.
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="worldName"></param>
    public static void SaveChunk(ChunkData chunk, string worldName)
    {
        string chunkName = $"{chunk.position.x} - {chunk.position.y}";

        // Get the save path.
        string savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create);

        formatter.Serialize(stream, chunk);
        stream.Close();
    }

    /// <summary>
    /// Loads an individual chunk.
    /// </summary>
    /// <param name="worldName"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static ChunkData LoadChunk(string worldName, Vector2Int position)
    {
        string chunkName = $"{position.x} - {position.y}";

        string loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".chunk";

        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            ChunkData chunkData = formatter.Deserialize(stream) as ChunkData;
            stream.Close();

            return chunkData;
        }

        return null;
    }
}
