using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiskSampling
{
    public static Vector2[] GenerateTreeMap(MapSettings settings, int size, float [] map)
    {
        System.Random prng = new System.Random(settings.seed);
        int regionSize = settings.voronoiFrequency;
        int gridLength = (regionSize <= 0) ? 1 : regionSize * 2;
        int gridSize = (size - 1) / gridLength + 1;
        Vector2[] points = GeneratePoints(prng, size, gridSize, gridLength);
        
        for (int i = 0; i < gridLength; i++)
        {
            float height = map[i];
            if (height > 0.5)
            {
                points[i] = Vector2.zero;
            }
        }
        return points;
    }
    public static Vector2[] GeneratePoints(System.Random prng, int size, int gridSize, int gridLength)
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
