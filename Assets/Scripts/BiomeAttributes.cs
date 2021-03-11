using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName="VankoCraft/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    #region Data members
    [Header("Biome settings")]
    public string biomeName;
    public int offset;
    public float scale;

    public int terrainHeight; // how tall the terrain generations are
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Big Flora")]
    public int majorFloraIndex;
    public float bigFloraZoneScale = 1.3f;
    [Range(0.1f, 1f)] 
    public float bigFloraZoneTreshold = 0.6f; // more or less forest
    public float bigFloraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float bigFloraPlacementThreshold = 0.8f;

    public bool placeBigFlora = true;

    public int maxHeight = 12;
    public int minHeight = 5;

    public Lode[] lodes;
    #endregion
}

[System.Serializable]
public class Lode
{
    #region Data members
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
    #endregion
}
