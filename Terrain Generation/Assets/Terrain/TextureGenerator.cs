using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator : MonoBehaviour
{
    public static Color[] CreateCloudColorMap(int size, float[,] map, float threshold)
    {
        Color[] colorMap = new Color[(size) * (size)];
        for (int i = 0, x = 0; x <= size - 1; x++)
        {
            for (int y = 0; y <= size - 1; y++, i++)
            {
               
                if (map[x, y] < threshold)
                {
                    colorMap[i] = Color.clear;
                }
                else
                {
                    colorMap[i] = Color.white;
                }
            }
        }
        return colorMap;
    }
    public static Texture2D TextureFromColorMap(Color[] colorMap, int size)
    {
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }
    public static Color[] CreateValueColorMap(int size, float[,] heightMap)
    {
        Color[] colorMap = new Color[(size) * (size)];
        for (int i = 0, x = 0; x <= size - 1; x++)
        {
            for (int y = 0; y <= size - 1; y++, i++)
            {
                colorMap[i] = Color.Lerp(Color.white, Color.black, heightMap[x, y]);
                
            }
        }
        return colorMap;

    }
    public static Color[] CreateVoronoiDiagramColorMap(int size, int[,] map)
    {
        Color[] colorMap = new Color[(size) * (size)];
        for (int i = 0, x = 0; x <= size - 1; x++)
        {
            for (int y = 0; y <= size - 1; y++, i++)
            {
                int index = map[x, y];
                
                if (index == 0)
                {
                    colorMap[i] = new Color(1, 0, 0);
                }
                if (index == 1)
                {
                    colorMap[i] = new Color(0, 1, 0);
                }
                if (index == 2)
                {
                    colorMap[i] = new Color(0, 0, 1);
                } 
            }
        }
        return colorMap;

    }
}
