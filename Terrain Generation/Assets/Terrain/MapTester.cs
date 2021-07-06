using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapTester : MonoBehaviour
{

    const int mapChunkSize = 239;
    public MapSettings mapSettings;
    public MeshFilter testChunkFilter;
    public MeshRenderer testChunkRenderer;

    //Generates Test Chunk in editor
    public void GenerateTestChunk()
    {
        MapDisplay display = (MapDisplay)FindObjectOfType(typeof(MapDisplay));

        //Generate Maps
        float[] heightMap = new float[(mapChunkSize + 2) * (mapChunkSize + 2)];
        float[] weights = new float[(mapChunkSize+2) * (mapChunkSize+2)];
        float[] mask1D = new float[(mapChunkSize + 2) * (mapChunkSize + 2)];
        float[,] fallofMap = new float[(mapChunkSize + 2), (mapChunkSize + 2)];

        float[] voronoi = new float[(mapChunkSize + 2) * (mapChunkSize + 2)];

        GenerateHeightMap.CreateHeightMap(mapSettings, mapSettings.seed, weights, MapSettings.DrawMode.Normal, 0, mapChunkSize + 2, mapSettings.offset, false);
        GenerateHeightMap.CreateHeightMap(mapSettings, mapSettings.seed, heightMap, mapSettings.mode, mapSettings.iterations, mapChunkSize + 2, mapSettings.offset, true, weights);
        GenerateHeightMap.GenerateFallofMap(fallofMap, mapChunkSize+2, mapSettings);
        Voronoi.GenerateVoronoiMap(mapSettings, voronoi, mapChunkSize+2, 2);

        float[,] map = Convert.Make2D(heightMap, mapChunkSize + 2);

        Color[] colorMap = TextureGenerator.CreateValueColorMap(mapChunkSize + 2, fallofMap);
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize + 2);
        display.DrawTexture(texture);


        if (mapSettings.islandMask)
        {
            GenerateHeightMap.ApplyMask(mapChunkSize + 2, map, fallofMap);
        }
        
        
        MeshData meshData = MeshGenerator.CreateTerrainMesh(mapChunkSize + 2, map, mapSettings.heightMult, null, mapSettings.levelOfDetail);
        Mesh mesh = meshData.CreateMesh();
        display.DrawChunk(mesh, testChunkFilter, testChunkRenderer);

    }
}