using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlantPlacerRoot))]
public class PlantPlacerRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        debugModelValue = EditorGUILayout.FloatField("DEBUG Model value", debugModelValue);

        if (GUILayout.Button("Start training"))
        {
            Debug.LogFormat("Starting training");
            SaveToModelAsset(debugModelValue, TargetModel);
        }

        EditorGUILayout.HelpBox("The following controls have not been implemented yet", MessageType.Info, wide:true);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Orientation invariant", false);
        }
    }

    private void SaveToModelAsset(float param, PlantPlacerModel outputModel)
    {
        outputModel.placerParameter = param;
    }

    private PlantPlacerModel TargetModel => 
        (PlantPlacerModel)serializedObject.FindProperty(PlantPlacerRoot.modelPropertyName).objectReferenceValue;
    
    private float debugModelValue = 1f;
}
