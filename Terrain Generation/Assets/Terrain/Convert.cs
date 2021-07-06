using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Convert
{
    public static float[,] Make2D(float[] input, int size)
    {
        float[,] output = new float[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                output[i, j] = input[(i * size) + j];
            }
        }
        return output;
    }
    public static float[] Make1D(float[,] input, int size)
    {
        float[] output = new float[size * size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                output[(i * size) + j] = input[i, j];
            }
        }
        return output;
    }
    public static int[,] Make2DInt(int[] input, int size)
    {
        int[,] output = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                output[i, j] = input[(i * size) + j];
            }
        }
        return output;
    }

}
