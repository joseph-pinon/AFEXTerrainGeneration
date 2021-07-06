using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Erosion
{

    static int[][] erosionBrushIndices;
    static float[][] erosionBrushWeights;
    static System.Random prng;
    public ComputeShader erosion;

    static void Initialize(int size, int seed, int radius)
    {
        prng = new System.Random(seed);
        InitializeBrushIndices(size, radius);
    }
    public static void ErodeHeightMap(float[] heightMap, int size, int iterations, MapSettings settings, ComputeShader erosionShader)
    {
        if (settings.GPUComputeErosion)
        {
            GPUErode(heightMap, size, settings, erosionShader);
        }
        else
        {
            CPUErode(heightMap, size, iterations, settings);
        }
    }
    public static void CPUErode(float[] map, int size, int iterations, MapSettings mapSettings)
    {
        Initialize(size, mapSettings.erosionSeed, mapSettings.radius);

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            //Initialize Droplet
            float posX = prng.Next(0, size-1);
            float posY = prng.Next(0, size-1);
            Vector2 intialPos = new Vector2(posX, posY);
            Vector2 initialDir = Vector2.zero;
            float initialVel = 1;
            float initialWater = 1;
            float initialSediment = 0;
          
            Droplet droplet = new Droplet(intialPos, initialDir, initialVel, initialWater, initialSediment);

            //Iterate through droplet lifetime
            for (int path = 0; path < mapSettings.maxPath; path++)
            {
                int gridX = (int)droplet.pos.x;
                int gridY = (int)droplet.pos.y;
                float offsetX = droplet.pos.x - gridX;
                float offsetY = droplet.pos.y - gridY;
                int dropletIndex = gridY * size + gridX;

                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, droplet.pos.x, droplet.pos.y, size);

                float dirX = (droplet.dir.x * mapSettings.inertia - heightAndGradient.gradient.x * (1 - mapSettings.inertia));
                float dirY = (droplet.dir.y * mapSettings.inertia - heightAndGradient.gradient.y * (1 - mapSettings.inertia));
                
                Vector2 dir = new Vector2(dirX, dirY);
                droplet.dir = dir.normalized;
                
                posX += droplet.dir.x;
                posY += droplet.dir.y;

                // Stop simulating droplet if it's not moving or has flowed over edge of map
                if ((dir.x == 0 && dir.y == 0)||posX < 0 || posX >= size - 1 || posY < 0 || posY >= size - 1)
                {
                    break;
                }

                //Update droplet position
                droplet.pos.x = posX;
                droplet.pos.y = posY;

                // Find the droplet's new height and calculate the deltaHeight
                float newHeight = CalculateHeightAndGradient(map, droplet.pos.x, droplet.pos.y, size).height;
                float deltaHeight = newHeight - heightAndGradient.height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Mathf.Max(-deltaHeight * droplet.vel * droplet.water * mapSettings.sedimentCapacityFactor, mapSettings.minSedimentCapacity);

                if (droplet.sediment > sedimentCapacity || deltaHeight > 0)
                {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, droplet.sediment) : (droplet.sediment - sedimentCapacity) * mapSettings.deposition;
                    droplet.sediment -= amountToDeposit;
                    

                    // Add the sediment to the four nodes of the current cell using bilinear interpolation
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    map[dropletIndex] += amountToDeposit * (1 - offsetX) * (1 - offsetY);
                    map[dropletIndex + 1] += amountToDeposit * offsetX * (1 - offsetY);
                    map[dropletIndex + size] += amountToDeposit * (1 - offsetX) * offsetY;
                    map[dropletIndex + size + 1] += amountToDeposit * offsetX * offsetY;

                }
                else
                {
                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Mathf.Min((sedimentCapacity - droplet.sediment) * mapSettings.erosion, -deltaHeight);

                    // Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletIndex].Length; brushPointIndex++)
                    {
                        int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];

                        float weighedErodeAmount = amountToErode * erosionBrushWeights[dropletIndex][brushPointIndex];
                        float deltaSediment = (map[nodeIndex] < weighedErodeAmount) ? map[nodeIndex] : weighedErodeAmount;

                        map[nodeIndex] -= deltaSediment;
                        droplet.sediment += deltaSediment;
                    }
                }

                // Update droplet's speed and water content
                droplet.vel = Mathf.Sqrt(droplet.vel * droplet.vel + deltaHeight * mapSettings.gravity);
                droplet.water *= (1 - mapSettings.evaporation);
                
            }
        }
    }
    public static void GPUErode(float [] map, int mapSizeWithBorder, MapSettings settings, ComputeShader erosion)
    {
        int size = mapSizeWithBorder - settings.radius * 2;
        int threadCount = settings.iterations / 1024;
        
        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();
        int erosionBrushRadius = settings.radius;

        float weightSum = 0;
        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * size + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Generate random indices for droplet placement
        int[] randomIndices = new int[settings.iterations];
        for (int i = 0; i < settings.iterations; i++)
        {
            int randomX = Random.Range(erosionBrushRadius, size + erosionBrushRadius);
            int randomY = Random.Range(erosionBrushRadius, size + erosionBrushRadius);
            randomIndices[i] = randomY * size + randomX;
        }

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        mapBuffer.SetData(map);
        erosion.SetBuffer(0, "map", mapBuffer);

        // Settings
        erosion.SetInt("borderSize", erosionBrushRadius);
        erosion.SetInt("size", mapSizeWithBorder);
        erosion.SetInt("brushLength", brushIndexOffsets.Count);
        erosion.SetInt("maxLifetime", settings.maxPath);
        erosion.SetFloat("inertia", settings.inertia);
        erosion.SetFloat("sedimentCapacityFactor", settings.sedimentCapacityFactor);
        erosion.SetFloat("minSedimentCapacity", settings.minSedimentCapacity);
        erosion.SetFloat("depositSpeed", settings.deposition);
        erosion.SetFloat("erodeSpeed", settings.erosion);
        erosion.SetFloat("evaporateSpeed", settings.evaporation);
        erosion.SetFloat("gravity", settings.gravity);

        // Run compute shader
        erosion.Dispatch(0, threadCount, 1, 1);
        mapBuffer.GetData(map);

        // Release buffers
        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }



    static void InitializeBrushIndices(int mapSize, int radius)
    {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength(0); i++)
        {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius)
            {
                weightSum = 0;
                addIndex = 0;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        float sqrDst = x * x + y * y;
                        if (sqrDst < radius * radius)
                        {
                            int coordX = centreX + x;
                            int coordY = centreY + y;

                            if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize)
                            {
                                float weight = 1 - Mathf.Sqrt(sqrDst) / radius;
                                weightSum += weight;
                                weights[addIndex] = weight;
                                xOffsets[addIndex] = x;
                                yOffsets[addIndex] = y;
                                addIndex++;
                            }
                        }
                    }
                }
            }

            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++)
            {
                erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }
    static HeightAndGradient CalculateHeightAndGradient(float[] nodes, float posX, float posY, int mapSize)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        // Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;
        Vector2 gradient = new Vector2(gradientX, gradientY);

        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient() { height = height, gradient = gradient };
    }
    struct HeightAndGradient
    {
        public float height;
        public Vector2 gradient;
    }
}
public class Droplet
{
    public Vector2 pos;
    public Vector2 dir;
    public float vel;
    public float water;
    public float sediment;
    public float capacity;
    public Droplet(Vector2 pos, Vector2 dir, float vel, float water, float sediment)
    {
        this.pos = pos;
        this.dir = dir;
        this.water = water;
        this.vel = vel;
        this.water = water;
        this.sediment = sediment;
    }

}
