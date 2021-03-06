using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeSettings", menuName = "FlatWorld Biomes/Biome Settings")]
public class BiomeAttribute : ScriptableObject
{
    public string biomeName;

    public int solidGroundHeight;
    public int terrainHeight;
    public float terrainScale;

    public Lode[] lodes;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshhold;
    public float noiseOffset;
}