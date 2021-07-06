using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer renderer;
    public Material terrain;
    public void DrawChunk(Mesh mesh, MeshFilter chunkFilter, MeshRenderer chunkRenderer)
    {
        chunkFilter.sharedMesh = mesh;
        chunkRenderer.material = terrain;
    }
    public void DrawTexture(Texture2D texture)
    {
        renderer.sharedMaterial.mainTexture = texture;
        renderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
}

