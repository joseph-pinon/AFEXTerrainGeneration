using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voronoi
{
    public static void GenerateVoronoiMap(MapSettings settings, float[] map, int size, int points)
    {
        ComputeShader voronoiShader = settings.voronoiShader;

        System.Random prng = new System.Random(settings.seed);
        
        int regionSize = settings.voronoiFrequency;

        int gridLength = (regionSize <= 0) ? 1 : regionSize * 2;
        int gridSize = (size - 1) / gridLength + 1;

        Vector2[] controlPoints = GenerateControlPoints(prng, size, gridSize, gridLength);
        Vector2[] octaveOffsets = new Vector2[settings.voronoiOctaves];

        for (int i = 0; i < settings.voronoiOctaves; i++)
        {
            octaveOffsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));
        }

        ComputeBuffer controlPointsBuffer = new ComputeBuffer(controlPoints.Length, sizeof(float)*2);
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        ComputeBuffer octaveOffsetsBuffer = new ComputeBuffer(octaveOffsets.Length, sizeof(float) * 2);
        
        controlPointsBuffer.SetData(controlPoints);
        octaveOffsetsBuffer.SetData(octaveOffsets);
        mapBuffer.SetData(map);
       

        voronoiShader.SetBuffer(0, "controlPoints", controlPointsBuffer);
        voronoiShader.SetBuffer(0, "map", mapBuffer);
        voronoiShader.SetBuffer(0, "octavesOffsets", octaveOffsetsBuffer);


        voronoiShader.SetInt("size", size);
        voronoiShader.SetInt("gridLength", gridLength);
        voronoiShader.SetInt("gridSize", gridSize);
        voronoiShader.SetInt("octaves", settings.voronoiOctaves);
        voronoiShader.SetFloat("persistance", settings.voronoiPersistance);
        voronoiShader.SetFloat("lacunarity", settings.voronoiLacunarity);


        //Calculate ThreadCount
        int threadCount;
        if (map.Length > 65535)
        {
            threadCount = 65535;
        }
        else
        {
            threadCount = map.Length;
        }
        voronoiShader.Dispatch(0, threadCount, 1, 1);
        
        mapBuffer.GetData(map);

        //Release Memory
        mapBuffer.Release();
        controlPointsBuffer.Release();
        octaveOffsetsBuffer.Release();
    }
    public static void GenerateColorIndex(int[] colorIndex, int totalColors, int gridLength, System.Random prng)
    {
        for (int i = 0; i < gridLength*gridLength; i++)
        {
            int color;
            if (i % gridLength == 0 || i / gridLength == 0 || i /gridLength == gridLength-1 || i % gridLength == gridLength-1)
            {
                color = 2;
            }
            else
            {
                color = (int)prng.Next(0, totalColors);
               
            }
            
            colorIndex[i] = color;

        } 
    }
    public static Vector2[] GenerateControlPoints(System.Random prng, int size, int gridSize, int gridLength)
    {
        Vector2[] points = new Vector2[gridLength * gridLength];

        for (int x = 0; x < (gridLength); x++)
        {
            int xBound = (gridSize * x);
            for (int z = 0; z < (gridLength); z++)
            {
                int zBound = (gridSize * z);
                int i = (x * gridLength) + z;
                points[i] = new Vector2(prng.Next(xBound, xBound + gridSize), prng.Next(zBound, zBound + gridSize));
            }
        }

        return points;
    }
}
