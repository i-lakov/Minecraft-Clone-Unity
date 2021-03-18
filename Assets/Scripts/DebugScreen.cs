using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    #region Data members
    World world;
    Text text;
    float frameRate;
    float timer;
    public Toolbar toolbar;
    public Settings settings;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    string timeFormat = "00";
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    // Update is called once per frame
    void Update()
    {
        string debugText = $"Vankocraft Beta v.{settings.version} Debug Screen (F3)\n";
        debugText += $"FPS: {frameRate}\n\n";
        debugText += $"Coordinates:\nX: {(Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels)} Y: {Mathf.FloorToInt(world.player.transform.position.y)} Z: {(Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels)}\n";
        debugText += $"Chunk: {(world.playerChunkCoord.x - halfWorldSizeInChunks)}, {(world.playerChunkCoord.z - halfWorldSizeInChunks)}\n";
        debugText += $"Current time: {world.timeHour}:{world.timeMin.ToString(timeFormat)}\n\n";
        debugText += $"Toolbar slot: {toolbar.slotIndex}\n\n";
        debugText += $"View distance: {settings.viewDistance}\n";
        debugText += $"Sensitivity: {settings.mouseSensitivity}";


        text.text = debugText;

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else timer += Time.deltaTime;
    }
}
