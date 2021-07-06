using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateHeightMap
{
   public static void CreateHeightMap(MapSettings settings, int seed, float [] heightMap, MapSettings.DrawMode mode, int iterations, int mapChunkSize, Vector2 majorOffset, bool normalizeMap, float [] weights = null )
    {
        FractalBrownianMotion.GenerateFBMHeightMap(heightMap, mapChunkSize, seed, settings, majorOffset, normalizeMap, mode, settings.heightMap, weights);
        
        //Erode Base Map
        if (iterations != 0)
        {
            Erosion.ErodeHeightMap(heightMap, mapChunkSize, iterations, settings, settings.erosionShader);
        }
    }
    public static float [,] GetHeightMapFromRange(float [,] heightMaps, int size, int startX, int startZ)
    {
        float[,] heightMap = new float[size, size];
        
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                float value = heightMaps[x+startX, z+startZ];
                heightMap[x, z] = value;
            }
        }
        return heightMap;
    }
    public static void GenerateIslandMask(float [] mask, int size, MapSettings settings)
    {
        ComputeShader gradientShader = settings.gradientShader;
        ComputeBuffer maskBuffer = new ComputeBuffer(mask.Length, sizeof(int));
        maskBuffer.SetData(mask);
        gradientShader.SetBuffer(0, "mask", maskBuffer);
        gradientShader.SetFloat("A", settings.maskWeight);
        gradientShader.SetFloat("size", size);
        
        int threadCount;
        
        if (mask.Length > 65535)
        {
            threadCount = 65535;
        }
        else
        {
            threadCount = mask.Length;
        }

        gradientShader.Dispatch(0, threadCount, 1, 1);
        maskBuffer.GetData(mask);
        maskBuffer.Release();
    }
    public static void ApplyMask(int size, float[,] map, float[,] mask)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                map[x, y] -= mask[x, y];
            }
        }
    }
    public static void GenerateFallofMap(float [,] map, int size, MapSettings settings)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector2 center = new Vector2(size / 2, size / 2);
                Vector2 point = new Vector2(i, j);
                Vector2 displacement = point - center;
                float value = displacement.magnitude/size;
                map[i, j] = Evaluate(value);
            } 
        }
    }
    public static float Evaluate(float value)
    {
        float a = 3;
        float b = 0.7f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
