using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Data", menuName = "TerrainSettings", order = 1)]
public class MapSettings : ScriptableObject
{
    public bool autoUpdate;
    public bool GPUComputeMap;
    public bool GPUComputeErosion;
    public bool islandMask;
    public enum DrawMode {Normal, Biome, Hills, Mountains, Plains, Mesa, Desert}
    public DrawMode mode;

    [Header("Perlin Settings")]
    [Range(0, 6)]
    public int levelOfDetail;
    public int seed;
    public float scale;
    public int octaves;
    
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public float heightMult;
    public AnimationCurve meshHeightCurve;
    public Vector2 offset;
    public Vector2 majorOffset;

    [Header("Voronoi Settings")]
    public int voronoiFrequency;
    public int voronoiOctaves;
    public float voronoiPersistance;
    public float voronoiLacunarity;

    [Header("Map Settings")]
    public int height;
    public int width;
    
    public enum MapSize { small, medium, large };
    public MapSize mapSize;
    public LODInfo[] LODInfo;
    
    [Header("Biomes")]
    public BiomeInfo[] Biomes;

    [Header("HydraulicErosion")]
    public int iterations;
    public float inertia;
    public float gravity;
    public float erosion;
    public int erosionSeed;
    public float evaporation;
    public int maxPath;
    public float deposition;
    public int radius;
    public float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
    public float minSedimentCapacity = .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain

    public ComputeShader heightMap;
    public ComputeShader erosionShader;
    public ComputeShader gradientShader;
    public ComputeShader voronoiShader;
    public float maskWeight;

    public GameObject tree;
    public GameObject propParent;

    
    public void OnValidate()
    {
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
    }
}

[System.Serializable]
public struct LODInfo
{
    public int LOD;
    public float viewerDistance;
}
[System.Serializable]
public struct BiomeInfo
{
    public string name;
    public int seed;
    public float scale;
    public int octaves;

    [Range(0, 1)]
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
    public Vector2 majorOffset;
}




