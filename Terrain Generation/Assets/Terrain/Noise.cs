using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{
    //Returns Fractal Brownian Motion Values Between 0 and 1    
    public static float FractalBrownianMotion(float x, float y, int seed, int octaves, float persistance, float lacunarity, Vector2 offset, MapSettings.DrawMode mode)
    {
        //Create a new random number generator with given seed
        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        float amplitude = 1;
        float frequency = 1;
        float noiseValue = 0;
        //Cycle through octaves
        for (int currentOctave = 0; currentOctave < octaves; currentOctave++)
        {
            float sampleX = x * frequency + octaveOffsets[currentOctave].x;
            float sampleZ = y * frequency + octaveOffsets[currentOctave].y;
            float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2 - 1;
            //Apply Filters here
            perlinValue = CalculateFilters(perlinValue, mode);

            noiseValue += perlinValue * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return noiseValue;
    }
    public static float CalculateFilters(float value, MapSettings.DrawMode mode)
    {
        if (mode == MapSettings.DrawMode.Plains)
        {
            return Plains(value);

        }
        else if (mode == MapSettings.DrawMode.Hills)
        {
            return Hills(value);
        }
        else if (mode == MapSettings.DrawMode.Mountains)
        {
            return Mountains(value);

        }
        else if (mode == MapSettings.DrawMode.Desert)
        {
            return Deserts(value);

        }
        else
        {
            return value;
        }

    }
    public static float Mountains(float value)
    {
        value = 1 - 2*Mathf.Abs(value);
        return value;
    }
    public static float Plains(float value)
    {
        value = value/20;
        return value;
    }
    public static float Hills(float value)
    {
        value = value / 10;
        return value;
    }
    public static float Deserts(float value)
    {
        value = 1 - 2 * Mathf.Abs(value);
        value = value / 20;
        return value;
    }
    public static float Mesa(float value)
    {
        value = -(value * value * value * value) + 1;
        return value;
    }


    //Renormalizes values between 0 and 1 (Consider moving this to GenerateHeightMap Class)
    public static void RenormalizeMap2D(float[,] noiseMap, int size, float minNoiseHeight, float maxNoiseHeight)
    {
        for (int x = 0; x <= size - 1; x++)
        {
            for (int y = 0; y <= size - 1; y++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
    }
    public static void RenormalizeMap1D(float [] noiseMap, int size, float minNoiseHeight, float maxNoiseHeight)
    {
        for (int x = 0; x <= size - 1; x++)
        {
            for (int y = 0; y <= size - 1; y++)
            {
                noiseMap[y*size+x] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[y*size+x]);
            }
        }
    }
}
