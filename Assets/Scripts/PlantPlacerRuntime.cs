using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlantPlacerRuntime : MonoBehaviour
{
    [SerializeField]
    private PlantPlacerModel model = null;

    [SerializeField]
    private float tileWidth = 10f;

    [SerializeField]
    private Terrain targetTerrain = null;

    [SerializeField]
    private GameObject spawnableTreePrefab = null;

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
        StartCoroutine(PlaceTrees());

        OnLandscapeUpdated(Vector2Int.zero);
    }

    private IEnumerator RegenerateTiles()
    {
        var pythonRunner = new PlantPlacerPythonRunner(model);
        while (true)
        {
            yield return new WaitUntil(() => queuedUpdates.Count > 0);

            var thisUpdateTile = queuedUpdates.First();
            queuedUpdates.RemoveAt(0);

            pythonRunner.StartGenerating(CollectHeightMapAtTile(thisUpdateTile));

            yield return new WaitUntil(() => pythonRunner.PollTileGenerationComplete());

            var tileGenerationResults = pythonRunner.CachedPreviousResult;
            EnqueueTreePlacements(thisUpdateTile, tileGenerationResults.relativeTreePositions);
        }
    }

    private void EnqueueTreePlacements(Vector2Int updateTile, List<Vector2> tileGenerationResults)
    {
        var tileWorldOrigin = new Vector2(updateTile.x, updateTile.y) * tileWidth;
        var worldPositions = tileGenerationResults.Select(localTreePosition => tileWorldOrigin + localTreePosition);
        queuedTreePlacements.AddRange(worldPositions);
    }

    private IEnumerator PlaceTrees()
    {
        while (true)
        {
            yield return new WaitUntil(() => queuedTreePlacements.Count > 0);

            var thisNewTree = queuedTreePlacements.First();
            queuedTreePlacements.RemoveAt(0);

            SpawnTree(thisNewTree);
        }
    }

    private void SpawnTree(Vector2 treeXzPosition)
    {
        var treeXzPosition3 = new Vector3(treeXzPosition[0], 0f, treeXzPosition[1]);
        var height = targetTerrain.SampleHeight(treeXzPosition3);
        var treePosition = treeXzPosition3 + Vector3.up * height;
        Object.Instantiate(spawnableTreePrefab, treePosition, Quaternion.identity);
    }

    private float[,] CollectHeightMapAtTile(Vector2Int tile)
    {
        return new float[256,256];
    }

    private List<Vector2> queuedTreePlacements = new List<Vector2>();
}
