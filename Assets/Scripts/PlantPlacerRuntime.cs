using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlantPlacerRuntime : MonoBehaviour
{
    [SerializeField]
    private PlantPlacerModel model = null;

    private List<Vector2Int> queuedUpdates = new List<Vector2Int>();

    public void OnLandscapeUpdated(Vector2Int tileIndex)
    {
        // clear any existing queued instances of this tile
        RemoveExistingQueuedTileGenerations(tileIndex);
        EnqueueTileGeneration(tileIndex);
    }

    private void EnqueueTileGeneration(Vector2Int tileIndex)
    {
        queuedUpdates.Add(tileIndex);
    }

    private void RemoveExistingQueuedTileGenerations(Vector2Int tileIndex)
    {
        queuedUpdates.RemoveAll(tile => tile == tileIndex);
    }

    private void Start()
    {
        StartCoroutine(RegenerateTiles());
    }

    private IEnumerator RegenerateTiles()
    {
        var pythonRunner = new PlantPlacerPythonRunner(model);
        while (true)
        {
            yield return new WaitUntil(() => queuedUpdates.Count > 0);

            var thisUpdateTile = queuedUpdates.First();
            queuedUpdates.RemoveAt(0);

            pythonRunner.StartGenerating(thisUpdateTile);

            yield return new WaitUntil(() => pythonRunner.PollTileGenerationComplete());
        }
    }
}
