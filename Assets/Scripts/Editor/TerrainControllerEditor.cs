using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(TerrainController))]
public class customButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainController terrainController = (TerrainController)target;
        if (GUILayout.Button("Generate"))
        {
            terrainController.ResetChunks();
        }
    }

}
