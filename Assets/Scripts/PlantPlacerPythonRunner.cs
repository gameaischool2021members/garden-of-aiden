using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Assertions;

public class PlantPlacerPythonRunner
{
    public PlantPlacerPythonRunner(PlantPlacerModel model)
    {
        StartProcess();
    }

    ~PlantPlacerPythonRunner()
    {
        if (!process.HasExited)
        {
            process.Kill();
        }
    }

    private static readonly string relativePythonScriptPath = Path.Combine("Assets", "ModelTraining", "Inference.py");

    // uncomment the one you want

    // Sam
    public static readonly string testPathToPython = Path.GetFullPath("Assets\\ModelTraining\\.venv\\Scripts\\python.exe");

    // Anthony
    // public static readonly string testPathToPython = Path.GetFullPath("Assets\\ModelTraining\\.venv\\Scripts\\python.exe");

    public static readonly string relativeModelPath = "first_training_pass.h5";
    private void StartProcess()
    {
        var startInfo = new ProcessStartInfo(testPathToPython);
        var fullPathToPython = Path.Combine(System.IO.Directory.GetCurrentDirectory(), relativePythonScriptPath);
        var fullPathToModel = Path.Combine(System.IO.Directory.GetCurrentDirectory(), relativeModelPath);
        startInfo.Arguments = String.Join(" ", new String[]{
            fullPathToPython,
            "--model", fullPathToModel,
        }.Select(arg => String.Format("\"{0}\"", arg)));
        startInfo.RedirectStandardInput = true;

        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        process = new Process();
        process.StartInfo = startInfo;
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += OutputDataReceived;
        process.ErrorDataReceived += ErrorOutputDataReceived;
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityEngine.Debug.Log(e.Data);

        if (String.IsNullOrEmpty(e.Data))
        {
            return;
        }

        if (!currentlyReading)
        {
            if (ShouldStartReading(e.Data))
            {
                readingIndex = 0;
                return;
            }
        }
        else
        {
            ReadLineToCachedArray(e.Data, readingIndex);
            ++readingIndex;
            if (ReachedEndOfData(readingIndex))
            {
                cachedPreviousResult.relativeTreePositions = readingData;
                ++lastGeneratedGeneration;
                StopReading();
            }
        }
    }

    private int readingIndex = -1;
    private bool currentlyReading => readingIndex >= 0;

    private void ErrorOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!String.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.Log(e.Data);
        }
    }

    private void ReadLineToCachedArray(string dataLine, int lineIndex)
    {
        try
        {
            var numbers = dataLine.Split(' ').Select(individualNumberString => float.Parse(individualNumberString)).ToArray();
            Assert.IsTrue(numbers.Length >= 256);

            for(var xIndex = 0; xIndex < 256; ++xIndex)
            {
                readingData[lineIndex, xIndex] = numbers[xIndex];
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.Message);
        }
    }


    public static void WriteMap(float[,] input, StreamWriter output)
    {
        foreach (var mapLine in input.GetJagged())
        {
            output.WriteLine(String.Join(" ", mapLine));
        }
    }

    public void StartGenerating(float[,] heightMap)
    {
        process.StandardInput.WriteLine("begin_inference_instance");
        process.StandardInput.WriteLine("heights");
        WriteMap(heightMap, process.StandardInput);
        process.StandardInput.WriteLine("end_inference_instance");
        process.StandardInput.WriteLine("finish");

        process.StandardInput.Flush();
    }

    public bool PollTileGenerationComplete()
    {
        if (lastPolledGeneration != lastGeneratedGeneration)
        {
            lastPolledGeneration = lastGeneratedGeneration;
            return true;
        }

        return false;
    }

    static private bool ShouldStartReading(string stdoutLine)
    {
        return stdoutLine.Trim() == "TreeProxMap";
    }

    static private bool ReachedEndOfData(int lineIndex)
    {
        return lineIndex >= 256;
    }

    private void StopReading()
    {
        readingIndex = -1;
    }

    private Process process;

    private float[,] readingData = new float[256,256];

    public struct GenerationResult
    {
        public float[,] relativeTreePositions;
    }

    public GenerationResult CachedPreviousResult => cachedPreviousResult;
    private GenerationResult cachedPreviousResult = new GenerationResult();

    private int lastGeneratedGeneration = 0;
    private int lastPolledGeneration = 0;
}
