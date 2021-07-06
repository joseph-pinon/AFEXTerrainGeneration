using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateClouds : MonoBehaviour
{
    public bool autoUpdate;
    public int height;
    public int width;
    public int seed;
    public float scale;
    public int octaves;
    public float persistance;
    public float lacunarity;
    public float speed;
    public float strength;

    public float threshold;

    public ComputeShader cloudComputeShader;

    private float currentTime;
    public Vector2 majorOffset;
    public Vector2 octaveOffset;
    const int mapChunkSize = 239;

    public Renderer renderer;


    
    public void GenerateClouds(Vector2 majorOffset, Vector2 octaveOffset)
    {
        //int size = height * width * mapChunkSize;
        //Calculate height and width of texture
        float[] clouds = new float[mapChunkSize*mapChunkSize];
        
        //Create Starting Noise Texture
        
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        
        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));
        }

        ComputeBuffer octaveOffsetsBuffer = new ComputeBuffer(octaveOffsets.Length, sizeof(float) * 2);
        ComputeBuffer cloudsBuffer = new ComputeBuffer(clouds.Length, sizeof(float));

        octaveOffsetsBuffer.SetData(octaveOffsets);
        cloudsBuffer.SetData(clouds);

        //Instantiate Min Max
        int floatToIntMultiplier = 1000;
        int[] minMaxHeight = { floatToIntMultiplier * octaves, 0 };
        ComputeBuffer minMaxBuffer = new ComputeBuffer(minMaxHeight.Length, sizeof(int));
        minMaxBuffer.SetData(minMaxHeight);
       
        cloudComputeShader.SetBuffer(0, "minMax", minMaxBuffer);
        cloudComputeShader.SetBuffer(0, "map", cloudsBuffer);
        cloudComputeShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);

        //Instantiate Settings
        cloudComputeShader.SetInt("size", mapChunkSize);
        cloudComputeShader.SetInt("octaves", octaves);
        cloudComputeShader.SetFloat("lacunarity", lacunarity);
        cloudComputeShader.SetFloat("persistance", persistance);
        cloudComputeShader.SetFloat("scale", scale);
        cloudComputeShader.SetInt("floatToIntMult", floatToIntMultiplier);
        cloudComputeShader.SetFloat("majorOffsetX", majorOffset.x);
        cloudComputeShader.SetFloat("majorOffsetY", majorOffset.y);
        cloudComputeShader.SetFloat("octaveOffsetX", octaveOffset.x);
        cloudComputeShader.SetFloat("octaveOffsetY", octaveOffset.y);

        //Calculate ThreadCount
        int threadCount;
        if (clouds.Length > 65535)
        {
            threadCount = 65535;
        }
        else
        {
            threadCount = clouds.Length/1024;
        }
        
        //Dispatch and Recieve Data
        cloudComputeShader.Dispatch(0, threadCount, 1, 1);
        
        cloudsBuffer.GetData(clouds);
        minMaxBuffer.GetData(minMaxHeight);
        
        //Release Memory
        cloudsBuffer.Release();
        minMaxBuffer.Release();
        octaveOffsetsBuffer.Release();

        //Recalculate min and max
        float minValue = (float)minMaxHeight[0] / (float)floatToIntMultiplier;
        float maxValue = (float)minMaxHeight[1] / (float)floatToIntMultiplier;

        //Normalize map
       
        for (int i = 0; i < clouds.Length; i++)
        {
            clouds[i] = Mathf.InverseLerp(minValue, maxValue, clouds[i]);
        }
        
        float[,] clouds2D = Convert.Make2D(clouds, mapChunkSize);
        Color[] colorMap = TextureGenerator.CreateCloudColorMap(mapChunkSize, clouds2D, threshold);
        Texture2D texture = TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize);

        renderer.sharedMaterial.mainTexture = texture;
        renderer.transform.localScale = new Vector3(texture.width/10f, 1, texture.height/10f);

    }
    public void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime > speed)
        {
            //Generate New Noise Texture with adjusted Octaves
            majorOffset.x += strength;
            majorOffset.y += strength;
            GenerateClouds(majorOffset, octaveOffset);
            currentTime = 0;
        }
    }
}
