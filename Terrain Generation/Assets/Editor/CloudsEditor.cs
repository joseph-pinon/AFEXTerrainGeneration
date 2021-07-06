using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CreateClouds))]
public class CloudsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CreateClouds cloudTester = (CreateClouds)target;
        
        if (DrawDefaultInspector())
        {
            if (cloudTester.autoUpdate)
            {
                cloudTester.GenerateClouds(cloudTester.majorOffset, cloudTester.octaveOffset);
            }

        }

        if (GUILayout.Button("Generate"))
        {
            cloudTester.GenerateClouds(cloudTester.majorOffset, cloudTester.octaveOffset);
        }

        serializedObject.Update();
    }
}

