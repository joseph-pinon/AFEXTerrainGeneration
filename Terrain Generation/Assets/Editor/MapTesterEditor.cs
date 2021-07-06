using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapTester))]
public class MapTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapTester mapTester = (MapTester)target;

        if (DrawDefaultInspector())
        {
            if (mapTester.mapSettings.autoUpdate)
            {
                mapTester.GenerateTestChunk();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            mapTester.GenerateTestChunk();
        }

        serializedObject.Update();
    }
}

