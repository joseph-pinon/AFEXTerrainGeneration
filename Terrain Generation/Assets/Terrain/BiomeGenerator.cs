using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    void GenerateBiomeMap(MapSettings settings, float[,] heightMap, int size)
    {
        /*float[] temperatureMap = new float[size * size];
        float[] moistureMap = new float[size * size];
        GenerateHeightMap.CreateHeightMap(settings, settings.seed + 10, temperatureMap, MapSettings.DrawMode.Normal, 0, size, settings.majorOffset, true);
        GenerateHeightMap.CreateHeightMap(settings, settings.seed + 20, moistureMap, MapSettings.DrawMode.Normal, 0, size, settings.majorOffset, true);

        float[,] tempMap = Convert.Make2D(temperatureMap, size);
        float[,] moistMap = Convert.Make2D(moistureMap, size);
        
        float ColdestValue = 0.05f;
        float ColderValue = 0.18f;
        float ColdValue = 0.4f;
        float WarmValue = 0.6f;
        float WarmerValue = 0.8f;

        float DryestValue = 0.27f;
        float DryValue = 0.4f;
        float WetValue = 0.6f;
        float WetterValue = 0.8f;
        float WettestValue = 0.9f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float heatValue = tempMap[x, y];
                float moistureValue = moistMap[x, y];
                float height = heightMap[x, y];

                MapSettings.MoistureType moistureType;

                if (moistureValue <= DryestValue) moistureType = MapSettings.MoistureType.Dryest;
                else if (moistureValue <= DryValue) moistureType = MapSettings.MoistureType.Dryer;
                else if (moistureValue <= WetValue) moistureType = MapSettings.MoistureType.Dry;
                else if (moistureValue <= WetterValue) moistureType = MapSettings.MoistureType.Wet;
                else if (moistureValue <= WettestValue) moistureType = MapSettings.MoistureType.Wetter;
                else moistureType = MapSettings.MoistureType.Wettest;

                float heatValue = temperatureMap[x, y];
                MapSettings.HeatType heatType;

                if (heatValue <= ColdestValue) heatType = MapSettings.HeatType.Coldest;
                else if (heatValue <= ColderValue) heatType = MapSettings.HeatType.Colder;
                else if (heatValue <= ColdValue) heatType = MapSettings.HeatType.Cold;
                else if (heatValue <= WarmValue) heatType = MapSettings.HeatType.Warm;
                else if (heatValue <= WarmerValue) heatType = MapSettings.HeatType.Warmer;
                else heatType = MapSettings.HeatType.Warmest;
                string biomeName = settings.biomeTable.moistureTable[(int)moistureType].heatTable[(int)heatType];

                //temp -= (height * 0.5f);
            }
        }*/
    }
}
