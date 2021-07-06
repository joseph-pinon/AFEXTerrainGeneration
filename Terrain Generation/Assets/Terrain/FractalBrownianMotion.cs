using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FractalBrownianMotion
{

    public static void GenerateFBMHeightMap(float [] heightMap, int size, int seed, MapSettings settings, Vector2 majorOffset, bool normalizeMap, MapSettings.DrawMode mode, ComputeShader heightMapShader, float[] weights = null)
    {
        if (settings.GPUComputeMap)
        {
            GPUGenerateFBMHeightMap(heightMap, size, seed, settings, majorOffset, normalizeMap, mode, heightMapShader, weights);
        }
        else
        {
            CPUGenerateFBMHeightMap(heightMap, size, settings, majorOffset, normalizeMap, mode, weights);
        }
    }
    public static void GPUGenerateFBMHeightMap(float [] map, int size, int seed, MapSettings settings, Vector2 majorOffset, bool normalizeMap, MapSettings.DrawMode mode, ComputeShader heightMapShader, float[] weights = null)
    {
        //Seeds and Number Generators
        
        System.Random normprng = new System.Random(seed);
        System.Random plaprng = new System.Random(settings.seed);
        System.Random montprng = new System.Random(settings.seed);
        
        foreach (BiomeInfo biome in settings.Biomes)
        {
            if (biome.name == "Plains")
            {
                plaprng = new System.Random(biome.seed);

            }
            else if (biome.name == "Mountains")
            {
                montprng = new System.Random(biome.seed);
            }
        }
        
       

        //Create Offsets
        Vector2[] normalOffsets = new Vector2[settings.octaves];
        Vector2[] mountainOffsets = new Vector2[settings.octaves];
        Vector2[] plainOffsets = new Vector2[settings.octaves];

        for (int i = 0; i < settings.octaves; i++)
        {
            normalOffsets[i] = new Vector2(normprng.Next(-10000, 10000), normprng.Next(-10000, 10000));
            mountainOffsets[i] = new Vector2(montprng.Next(-10000, 10000), montprng.Next(-10000, 10000));
            plainOffsets[i] = new Vector2(plaprng.Next(-10000, 10000), plaprng.Next(-10000, 10000));
        }

        ComputeBuffer normalOffsetsBuffer = new ComputeBuffer(normalOffsets.Length, sizeof(float) * 2);
        ComputeBuffer mountainOffsetsBuffer = new ComputeBuffer(mountainOffsets.Length, sizeof(float) * 2);
        ComputeBuffer plainOffsetsBuffer = new ComputeBuffer(plainOffsets.Length, sizeof(float) * 2);
        
        normalOffsetsBuffer.SetData(normalOffsets);
        mountainOffsetsBuffer.SetData(mountainOffsets);
        plainOffsetsBuffer.SetData(plainOffsets);

        heightMapShader.SetBuffer(0, "normalOffsets", normalOffsetsBuffer);
        heightMapShader.SetBuffer(0, "mountainOffsets", mountainOffsetsBuffer);
        heightMapShader.SetBuffer(0, "plainOffsets", plainOffsetsBuffer);

        //Instantiate Map
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(int));
        mapBuffer.SetData(map);
        heightMapShader.SetBuffer(0, "fbm", mapBuffer);

        //Instantiate Min Max
        int floatToIntMultiplier = 1000;
        int[] minMaxHeight = {floatToIntMultiplier * settings.octaves, 0};
        ComputeBuffer minMaxBuffer = new ComputeBuffer(minMaxHeight.Length, sizeof(int));
        minMaxBuffer.SetData(minMaxHeight);
        heightMapShader.SetBuffer(0, "minMax", minMaxBuffer);

        //Instantiate Settings
        heightMapShader.SetInt("size", size);
        heightMapShader.SetInt("octaves", settings.octaves);
        heightMapShader.SetFloat("lacunarity", settings.lacunarity);
        heightMapShader.SetFloat("persistance", settings.persistance);
        heightMapShader.SetFloat("scale", settings.scale);
        heightMapShader.SetInt("floatToIntMult", floatToIntMultiplier);
        heightMapShader.SetFloat("majorOffsetX", majorOffset.x);
        heightMapShader.SetFloat("majorOffsetY", majorOffset.y);

        //Calculate Mode
        int bitMode = CalculateMode(mode);
        heightMapShader.SetInt("mode", bitMode);

        //Instantiate Biomes
        Biome[] biomes = CreateBiomes(settings.Biomes);
        int biomeStride = sizeof(int) + sizeof(float) * 7;
        ComputeBuffer biomeBuffer = new ComputeBuffer(biomes.Length, biomeStride);
        biomeBuffer.SetData(biomes);
        heightMapShader.SetBuffer(0, "biomeInfo", biomeBuffer);

        //Instantiate Weights
        ComputeBuffer weightsBuffer;
        
        if (weights != null)
        {
            weightsBuffer = new ComputeBuffer(weights.Length, sizeof(int));
            weightsBuffer.SetData(weights);
            heightMapShader.SetBuffer(0, "weights", weightsBuffer);
        }
        else
        {
            weightsBuffer = new ComputeBuffer(1, sizeof(int));
            float[] foo = new float[1];
            weightsBuffer.SetData(foo);
            heightMapShader.SetBuffer(0, "weights", weightsBuffer);
        }

        

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

        //Dispatch and Recieve Data
        heightMapShader.Dispatch(0, threadCount, 1, 1);

        mapBuffer.GetData(map);
        minMaxBuffer.GetData(minMaxHeight);
        
        //Release Memory
        mapBuffer.Release();
        minMaxBuffer.Release();
        normalOffsetsBuffer.Release();
        mountainOffsetsBuffer.Release();
        plainOffsetsBuffer.Release();
        weightsBuffer.Release();
        biomeBuffer.Release();

        //Recalculate min and max
        float minValue = (float)minMaxHeight[0] / (float)floatToIntMultiplier;
        float maxValue = (float)minMaxHeight[1] / (float)floatToIntMultiplier;
        
        //Normalize map
        if (normalizeMap)
        {
            for (int i = 0; i < map.Length; i++)
            {
                map[i] = Mathf.InverseLerp(minValue, maxValue, map[i]);
            }
        }
    }
    public static int CalculateMode(MapSettings.DrawMode mode)
    {
        if (mode == MapSettings.DrawMode.Normal)
        {
            return 0;
        }
        else if (mode == MapSettings.DrawMode.Plains)
        {
            return 1;
        }
        else if (mode == MapSettings.DrawMode.Mountains)
        {
            return 2;
        }
        else
        {
            return -1;
        }
    }
    public static void CPUGenerateFBMHeightMap(float [] map, int size, MapSettings settings, Vector2 majorOffset, bool normalizeMap, MapSettings.DrawMode mode, float [] weights = null)
    {
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        
        float halfSize = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float sampleX = (x - halfSize + majorOffset.x);
                float sampleY = (y - halfSize + majorOffset.y);
                float weight = 0;
                
                if (weights != null)
                {
                    weight = weights[y*size+x];
                }
                
                float value;

                if (mode == MapSettings.DrawMode.Normal)
                {
                    float scale = settings.scale;

                    if (scale <= 0)
                    {
                        scale = 0.0001f;
                    }

                    int seed = settings.seed;
                    int octaves = settings.octaves;
                    float persistance = settings.persistance;
                    float lacunarity = settings.lacunarity;
                    Vector2 offset = settings.offset;

                    value = Noise.FractalBrownianMotion(sampleX / scale, sampleY / scale, seed, octaves, persistance, lacunarity, offset, mode);
                }
                else
                {
                    float plainsValue = 0;
                    float mountainValue = 0;
                    float mesaValue = 0;
                    float hillsValue = 0;
                    float desertValue = 0;
                    
                    foreach (BiomeInfo biome in settings.Biomes)
                    {
                        float scale = biome.scale;
                        
                        if (scale <= 0)
                        {
                            scale = 0.0001f;
                        }

                        int seed = biome.seed;
                        int octaves = biome.octaves;
                        float persistance = biome.persistance;
                        float lacunarity = biome.lacunarity;
                        Vector2 offset = biome.offset;
                        
                        if (biome.name == "Plains")
                        {
                            plainsValue = Noise.FractalBrownianMotion(sampleX / scale, sampleY / scale, seed, octaves, persistance, lacunarity, offset, MapSettings.DrawMode.Plains);
                        }
                        else if (biome.name == "Mountains")
                        {
                            mountainValue = Noise.FractalBrownianMotion(sampleX / scale, sampleY / scale, seed, octaves, persistance, lacunarity, offset, MapSettings.DrawMode.Mountains);
                        }
                        else if (biome.name == "Mesa")
                        {
                            //mesaValue = Noise.FractalBrownianMotion(sampleX / scale, sampleY / scale, seed, octaves, persistance, lacunarity, offset, MapSettings.DrawMode.Mesa);
                        }
                        else if (biome.name == "Hills")
                        {
                            //hillsValue = Noise.FractalBrownianMotion(sampleX / scale, sampleY / scale, seed, octaves, persistance, lacunarity, offset, MapSettings.DrawMode.Hills);
                        }
                        else if (biome.name == "Desert")
                        {
                            //desertValue = Noise.FractalBrownianMotion(sampleX / scale, sampleY / scale, seed, octaves, persistance, lacunarity, offset, MapSettings.DrawMode.Desert);
                        }

                    }
                    if (mode == MapSettings.DrawMode.Plains)
                    {
                        value = plainsValue;

                    }
                    else if (mode == MapSettings.DrawMode.Mountains)
                    {
                        value = mountainValue;

                    }
                    else if (mode == MapSettings.DrawMode.Mesa)
                    {
                        value = mesaValue;

                    }
                    else if (mode == MapSettings.DrawMode.Hills)
                    {
                        value = hillsValue;

                    }
                    else if (mode == MapSettings.DrawMode.Desert)
                    {
                        value = desertValue;
                    }
                    else
                    {
                        //value = CalculateBiomeWeights(plainsValue, mountainValue, mesaValue, desertValue, hillsValue, weight);
                        value = SimpleBiomeInterpolation(mountainValue, plainsValue, weight);
                    }
                   
                }

                map[y*size+x] = value;
                if (value > maxValue)
                {
                    maxValue = value;
                }
                if (value < minValue)
                {
                    minValue = value;
                }
            }
        }
        if (normalizeMap)
        {
            Noise.RenormalizeMap1D(map, size, minValue, maxValue);
        }
    }
    static float SimpleBiomeInterpolation(float v1, float v2, float weight)
    {
        return Mathf.Lerp(v1, v2, weight);
    }
    static float CalculateBiomeWeights(float v1, float v2, float v3, float v4, float v5, float masterWeight)
    {
        //float value = Mathf.Lerp(value1, value2, weight);
        float w1 = CalculateWeight(5, 0, masterWeight);
        float w2 = CalculateWeight(5, 1, masterWeight);
        float w3 = CalculateWeight(5, 2, masterWeight);
        float w4 = CalculateWeight(5, 3, masterWeight);
        float w5 = CalculateWeight(5, 4, masterWeight);
        float value = (w1 * v1) + (w2 * v2) + (w3 * v3) + (w4 * v4) + (w5 * v5);
        return value;
    }
    static float CalculateWeight(int n, int i, float x)
    {
        return (-Mathf.Abs(n * x - i) + 1);
    }
    static Biome [] CreateBiomes(BiomeInfo[] biomeInfo)
    {
        Biome[] biomes = new Biome[biomeInfo.Length];
        for (int i = 0; i < biomeInfo.Length; i++)
        {
            BiomeInfo b = biomeInfo[i];
            Biome biome = new Biome(b.scale, b.octaves, b.persistance, b.lacunarity, b.offset, b.majorOffset);
            biomes[i] = biome;
        }

        return biomes;
    }
}
struct Biome
{
    float scale;
    int octaves;
    float persistance;
    float lacunarity;
    Vector2 offset;
    Vector2 majorOffset;
    public Biome(float scale, int octaves, float persistance, float lacunarity, Vector2 offset, Vector2 majorOffset)
    {
        this.scale = scale;
        this.octaves = octaves;
        this.persistance = persistance;
        this.lacunarity = lacunarity;
        this.offset = offset;
        this.majorOffset = majorOffset;
    }
}

