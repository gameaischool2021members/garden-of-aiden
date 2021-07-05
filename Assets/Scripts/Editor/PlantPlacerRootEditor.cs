using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
            UnityEngine.Debug.LogFormat("Starting training");
            var trainedModel = TrainModel(debugModelValue);
            SaveToModelAsset(debugModelValue, TargetModel);
        }

        EditorGUILayout.HelpBox("The following controls have not been implemented yet", MessageType.Info, wide:true);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Toggle("Orientation invariant", false);
        }
    }

    private static readonly string relativePythonScriptPath = Path.Combine("Assets", "ModelTraining", "TrainModel.py");
    private const int modelTrainingTimeout = 30000;
    private float TrainModel(float arbitraryInput)
    {
        var startInfo = new ProcessStartInfo();

        startInfo.FileName = "python.exe";
        var fullPathToPython = Path.Combine(System.IO.Directory.GetCurrentDirectory(), relativePythonScriptPath);
        startInfo.Arguments = String.Join(" ", new String[]{
            fullPathToPython,
            "--",
            arbitraryInput.ToString(),
        }.Select(arg => String.Format("\"{0}\"", arg)));
        UnityEngine.Debug.LogFormat("Running python script {0} - {1}", fullPathToPython, startInfo.Arguments);

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var hasClosed = process.WaitForExit(modelTrainingTimeout);

        Assert.IsTrue(hasClosed, "Timed out waiting for training to complete");

        var outputString = process.StandardOutput.ReadToEnd();

        ReportSubProcessOutputs(process, "Python ML");

        // Debug implementation
        var output = float.Parse(outputString);
        return output;
    }

    private static void DoIfAnyNonEmptyStrings(String output, Action<String> action)
    {
        if (output.Count() != 0)
        {
            action(output);
        }
    }

    private enum LogOrError
    {
        Log,
        Error
    }
    private static void ConditionalOutputStreamLog(LogOrError logType, string formatString, params object[] args)
    {
        switch(logType)
        {
            case LogOrError.Log:
                UnityEngine.Debug.LogFormat(formatString, args);
                break;
            case LogOrError.Error:
                UnityEngine.Debug.LogErrorFormat(formatString, args);
                break;
            default:
                UnityEngine.Debug.LogError("Unknown log type");
                UnityEngine.Debug.LogErrorFormat(formatString, args);
                break;
        }
    }

    private static void ReportSubProcessOutputs(Process process, string processIdentifier, LogOrError treatErrorsAs = LogOrError.Error)
    {
        DoIfAnyNonEmptyStrings(process.StandardError.ReadToEnd(), formattedOutput => ConditionalOutputStreamLog(treatErrorsAs, "{0} error:\n{1}\n\n\n", processIdentifier, formattedOutput));
    }

    private void SaveToModelAsset(float param, PlantPlacerModel outputModel)
    {
        outputModel.placerParameter = param;
    }

    private PlantPlacerModel TargetModel => 
        (PlantPlacerModel)serializedObject.FindProperty(PlantPlacerRoot.modelPropertyName).objectReferenceValue;
    
    private float debugModelValue = 1f;
}
