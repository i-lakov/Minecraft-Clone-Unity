using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName="VankoCraft/Biome Attribute")]
public class BiomeAttributes : ScriptableObject
{
    #region Data members
    public string biomeName;

    public int solidGroundHeight; // always solid below this level
    public int terrainHeight; // how tall the terrain generations are
    public float terrainScale;

    [Header("Trees")]
    public float treeZoneScale = 1.3f;
    [Range(0.1f, 1f)] 
    public float treeZoneTreshold = 0.6f; // more or less forest
    public float treePlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float treePlacementThreshold = 0.8f;

    public int maxTreeHeight = 12;
    public int minTreeHeight = 5;

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
