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
            var trainedModel = TrainModel();
            SaveToModelAsset(trainedModel, TargetModel);
        }
    }

    private static readonly string relativePythonScriptPath = Path.Combine("Assets", "ModelTraining", "TrainModel.py");
    private const int modelTrainingTimeout = 500000000;
    private float TrainModel()
    {
        var startInfo = new ProcessStartInfo();

        startInfo.FileName = PlantPlacerPythonRunner.testPathToPython;
        var fullPathToPython = Path.Combine(System.IO.Directory.GetCurrentDirectory(), relativePythonScriptPath);
        startInfo.Arguments = String.Join(" ", new String[]{
            fullPathToPython,
        }.Select(arg => String.Format("\"{0}\"", arg)));
        UnityEngine.Debug.LogFormat("Running python script {0} - {1}", fullPathToPython, startInfo.Arguments);

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += OutputDataReceived;
        process.ErrorDataReceived += ErrorOutputDataReceived;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            CollectAndSendTrainingDataToTrainer(process);

            process.WaitForExit();
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

        //var outputString = process.StandardOutput.ReadToEnd();

        //ReportSubProcessOutputs(process, "Python ML");

        // Debug implementation
        //var output = float.Parse(outputString);
        return 1f;
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!String.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.Log(e.Data);
        }
    }

    private void ErrorOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!String.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.Log(e.Data);
        }
    }

    private const int heightMapResolution = 256;

    private void CollectAndSendTrainingDataToTrainer(Process process)
    {
        var scanner = new VegetationScanner();

        var terrain = TargetComponent.GetComponent<Terrain>();
        var centerPointBounds = terrain.terrainData.bounds;
        centerPointBounds.Expand((Vector3.right + Vector3.forward) * -TargetComponent.tileWidthInWorldUnits / 2f);
        var centerPointMin = new Vector2(centerPointBounds.min.x, centerPointBounds.min.z);
        var centerPointMax = new Vector2(centerPointBounds.max.x, centerPointBounds.max.z);

        var stdInput = process.StandardInput;

        for (var attemptIndex = 0; attemptIndex < TargetComponent.numberOfDioramaSamples; ++attemptIndex)
        {
            var centerPoint = new Vector2(
                UnityEngine.Random.Range(centerPointMin.x, centerPointMax.x),
                UnityEngine.Random.Range(centerPointMin.y, centerPointMax.y)
            );

            // collect data
            var treeProximityMap = scanner.ScanForTrees(centerPoint, TargetComponent.tileWidthInWorldUnits / 2f, TargetComponent.proximityGradientWidthInTexels);

            var heightMap = new float[heightMapResolution, heightMapResolution];
            var cornerOrigin2d = centerPoint - Vector2.one * TargetComponent.tileWidthInWorldUnits / 2f;
            var cornerOrigin = Vector3.right * cornerOrigin2d.x + Vector3.forward * cornerOrigin2d.y;
            for (var x = 0; x < heightMapResolution; ++x)
            {
                for (var y = 0; y < heightMapResolution; ++y)
                {
                    var offset = (Vector3.right * x + Vector3.forward * y) * TargetComponent.tileWidthInWorldUnits / heightMapResolution;
                    var pollPosition = cornerOrigin + offset;
                    heightMap[x,y] = terrain.SampleHeight(pollPosition);
                }
            }

            // send data to process

            stdInput.WriteLine("begin_training_instance");

            stdInput.WriteLine("plants");
            WriteMap(treeProximityMap, stdInput);

            stdInput.WriteLine("heights");
            WriteMap(heightMap, stdInput);

            stdInput.WriteLine("end_training_instance");
        }

        stdInput.WriteLine("finish");
    }

    public static void WriteMap(float[,] input, StreamWriter output)
    {
        PlantPlacerPythonRunner.WriteMap(input, output);
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
