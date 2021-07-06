using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class PlantPlacerPythonRunner
{
    public PlantPlacerPythonRunner(PlantPlacerModel model)
    {
        StartProcess();
        FeedModelToProcess(model);
    }

    private void StartProcess()
    {
        var startInfo = new ProcessStartInfo("path/to/executable");
        startInfo.RedirectStandardInput = true;
        startInfo.UseShellExecute = false;

        process = new Process();
        process.StartInfo = startInfo;
        process.Start();
    }

    private void FeedModelToProcess(PlantPlacerModel model)
    {
        var streamWriter = process.StandardInput;
        streamWriter.WriteLine("model={0}", model.placerParameter);
    }

    public void StartGenerating(Vector2Int tileIndex)
    {
        var streamWriter = process.StandardInput;
        streamWriter.WriteLine("generate={0},{1}", tileIndex[0], tileIndex[1]);

        cachedPreviousRequest = tileIndex;
    }

    public bool PollTileGenerationComplete()
    {
        return true;
    }

    private Process process;

    private struct GenerationResult
    {
        public List<Vector2> relativeTreePositions;
    }
    private Vector2Int cachedPreviousRequest;
    private GenerationResult cachedPreviousResult;
}
