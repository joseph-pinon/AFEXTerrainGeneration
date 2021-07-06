using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trees : MonoBehaviour
{
    public static void PlantTrees(int size, Vector2[] treeLocations, float [] map, MapSettings settings)
    {
        for (int i = 0; i < treeLocations.Length; i++)
        {
            if (treeLocations[i] != null)
            {
                float halfSize = size / 2;
                
                GameObject tree = settings.tree;
                
                
                Vector3 pos = new Vector3(treeLocations[i].x-halfSize, map[i]*settings.heightMult, treeLocations[i].y-halfSize);
                Instantiate(tree, pos, Quaternion.identity);
            }
        }
    }
}
