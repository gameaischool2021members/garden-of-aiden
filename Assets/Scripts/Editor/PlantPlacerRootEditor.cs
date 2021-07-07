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

        if (GUILayout.Button("Start training"))
        {
            UnityEngine.Debug.LogFormat("Starting training");
            var trainedModel = TrainModel(1f);
            SaveToModelAsset(trainedModel, TargetModel);
        }
    }

    private static readonly string relativePythonScriptPath = Path.Combine("Assets", "ModelTraining", "TrainModel.py");
    private const int modelTrainingTimeout = 5000;
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
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        try
        {
            CollectAndSendTrainingDataToTrainer(process);

            var hasClosed = process.WaitForExit(modelTrainingTimeout);

            Assert.IsTrue(hasClosed, "Timed out waiting for training to complete");
        }
        catch (IOException e)
        {
            UnityEngine.Debug.LogErrorFormat("Error: {0}", e.Message);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }

        var outputString = process.StandardOutput.ReadToEnd();

        ReportSubProcessOutputs(process, "Python ML");

        // Debug implementation
        var output = float.Parse(outputString);
        return output;
    }

    private void CollectAndSendTrainingDataToTrainer(Process process)
    {
        var scanner = new TreeScanner();

        var terrain = TargetComponent.GetComponent<Terrain>();
        var centerPointBounds = terrain.terrainData.bounds;
        centerPointBounds.Expand((Vector3.right + Vector3.forward) * -TargetComponent.tileWidthInWorldUnits / 2f);
        var centerPointMin = new Vector2(centerPointBounds.min.x, centerPointBounds.min.z);
        var centerPointMax = new Vector2(centerPointBounds.max.x, centerPointBounds.max.z);

        var stdInput = process.StandardInput;

        for (var attemptIndex = 0; attemptIndex < 1; ++attemptIndex)
        {
            var centerPoint = new Vector2(
                UnityEngine.Random.Range(centerPointMin.x, centerPointMax.x),
                UnityEngine.Random.Range(centerPointMin.y, centerPointMax.y)
            );

            // collect data
            var treeProximityMap = scanner.ScannForTrees(centerPoint, TargetComponent.tileWidthInWorldUnits / 2f, TargetComponent.proximityGradientWidthInTexels).GetJagged();

            // send data to process

            stdInput.WriteLine("begin_training_instance");
            foreach (var mapLine in treeProximityMap)
            {
                stdInput.WriteLine(String.Join(" ", mapLine));
            }
            stdInput.WriteLine("end_training_instance");
        }

        stdInput.WriteLine("finish");
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

    private PlantPlacerRoot TargetComponent =>
        (PlantPlacerRoot)serializedObject.targetObject;
    private PlantPlacerModel TargetModel => 
        (PlantPlacerModel)serializedObject.FindProperty(PlantPlacerRoot.modelPropertyName).objectReferenceValue;
}
