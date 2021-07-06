using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class MapController : MonoBehaviour
{
    const int mapChunkSize = 239;
    public MapSettings settings;
    TerrainChunk[,] map;

    public Transform camera;
    public Transform terrainParent;

    [HideInInspector]
    public int height;
    [HideInInspector]
    public int width;

    public delegate void MapDelegate();
    public event MapDelegate mapGeneratedEvent;
    public GameObject testMap;
    static MapDisplay display;



    public void Start()
    {
        //Beginning of Map Generation Proccess
        display = (MapDisplay)FindObjectOfType(typeof(MapDisplay));
        testMap.SetActive(false);

        GenerateMap();
    }
    public void Update()
    {
        //Update each chunk in map every frame
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                TerrainChunk chunk = map[x, z];
                chunk.UpdateChunk();
            }
        }
    }

    public void GenerateMap()
    {
        //Setup minimum height and width values
        MapSettings.MapSize mapDim = settings.mapSize;
        height = settings.height;
        width = settings.width;

        if (mapDim == MapSettings.MapSize.medium)
        {
            height *= 2;
            width *= 2;
        }
        else if (mapDim == MapSettings.MapSize.large)
        {
            height *= 4;
            width *= 4;
        }

        //Instantiate Terrain and heightmap arrays
        map = new TerrainChunk[width, height];
        int iterations = settings.iterations * width * height;
        int mapSize = width * (mapChunkSize + 2);

        float[] rawHeightMap = new float[mapSize * mapSize];
        float[] weights = new float[mapSize * mapSize];
        float[,] fallOfMap = new float[mapSize,mapSize];
        
        //Generate height map
        GenerateHeightMap.CreateHeightMap(settings, settings.seed, weights, MapSettings.DrawMode.Normal, 0, mapSize, settings.offset, true);
        GenerateHeightMap.CreateHeightMap(settings, settings.seed, rawHeightMap, settings.mode, iterations, mapSize, settings.offset, true, weights);
        GenerateHeightMap.GenerateFallofMap(fallOfMap, mapSize, settings);
        
        float[,] heightMaps = Convert.Make2D(rawHeightMap, mapSize);
        

        if (settings.islandMask)
        {
            GenerateHeightMap.ApplyMask(mapSize, heightMaps, fallOfMap);

        }

        //Generate Terrain Chunk Map
        Vector3 positionOffset = new Vector3((width - 1) * (mapChunkSize - 1), 0, -(height - 1) * (mapChunkSize - 1))*.5f;
        
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                //Position of chunk
                Vector3 position = new Vector3(x, 0, -z) * (mapChunkSize - 1);

                //Center map by positionOffset
                position -= positionOffset;

                int startX = x * (mapChunkSize-1);
                int startZ = z * (mapChunkSize-1);
                float [,] heightMap = GenerateHeightMap.GetHeightMapFromRange(heightMaps, mapChunkSize + 2, startX, startZ);
                

                //Create LODMesh Data
                LODMesh[] LODMeshData = new LODMesh[settings.LODInfo.Length];
                for (int i = 0; i < settings.LODInfo.Length; i++)
                {
                    //Generate Mesh Data
                    MeshData meshData = MeshGenerator.CreateTerrainMesh(mapChunkSize + 2, heightMap, settings.heightMult, null, settings.LODInfo[i].LOD);
                    Mesh mesh = meshData.CreateMesh();
                    float viewerDistance = settings.LODInfo[i].viewerDistance;
                    int LOD = settings.LODInfo[i].LOD;
                    LODMesh LODMesh = new LODMesh(mesh, viewerDistance, LOD);
                    LODMeshData[i] = LODMesh;
                }

                
                Texture2D texture = new Texture2D(5,5);
                TerrainChunk chunk = new TerrainChunk(position, LODMeshData, texture, camera, terrainParent);
                chunk.DrawTerrainChunk();
                map[x, z] = chunk;
            }
        }

        //Map Generation finished (Release event)
        if (mapGeneratedEvent != null)
        {
            mapGeneratedEvent();
        }
    }
    public class TerrainChunk
    {
        //Create an empty gameobject
        GameObject chunk = new GameObject("TerrainChunk");

        //Render Info
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        Texture2D chunkTexture;

        //Mesh Info
        Mesh chunkMesh;
        LODMesh[] LODMeshData;

        //Misc
        Transform camera;
        int previousLOD;
        int currentLOD;


        public TerrainChunk(Vector3 position, LODMesh[] LODMeshData, Texture2D texture, Transform camera, Transform terrainParent)
        {
            //Add render components to empty gameobject
            meshFilter = chunk.AddComponent<MeshFilter>();
            meshRenderer = chunk.AddComponent<MeshRenderer>();

            //Assign position and scale
            chunk.transform.position = position;
            chunk.layer = 2;

            //Assign Mesh Data
            this.LODMeshData = LODMeshData;
            int lowestLOD = LODMeshData.Length - 1;
            previousLOD = LODMeshData[lowestLOD].LOD;
            chunkMesh = LODMeshData[lowestLOD].mesh;

            //Assign Texture
            chunkTexture = texture;

            //Camera and parent
            this.camera = camera;
            chunk.transform.parent = terrainParent;
        }
        public void DrawTerrainChunk()
        {
            display.DrawChunk(chunkMesh, meshFilter, meshRenderer);
        }
        public void UpdateChunk()
        {
            //Get distance between player and chunk
            Vector3 cameraPosition = new Vector3(camera.position.x, 0, camera.position.z);
            float distance = (chunk.transform.position - cameraPosition).magnitude;

            Debug.DrawRay(cameraPosition, chunk.transform.position - cameraPosition, Color.green, 1f);

            //Depending on distance display different LOD within mesh
            foreach (LODMesh lodMesh in LODMeshData)
            {
                if (distance <= lodMesh.viewerDistance)
                {
                    chunkMesh = lodMesh.mesh;
                    currentLOD = lodMesh.LOD;
                    break;
                }
            }
            //Only redraw chunk if previousLOD and currentLOD are different
            if (previousLOD != currentLOD)
            {
                display.DrawChunk(chunkMesh, meshFilter, meshRenderer);
                previousLOD = currentLOD;
            }
        }
    }
}

//Contains information about mesh with specific LOD
public class LODMesh
{
    public Mesh mesh;
    public float viewerDistance;
    public int LOD;

    public LODMesh(Mesh mesh, float viewerDistance, int LOD)
    {
        this.mesh = mesh;
        this.viewerDistance = viewerDistance;
        this.LOD = LOD;
    }
}
