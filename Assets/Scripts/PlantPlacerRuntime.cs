using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlantPlacerRuntime : MonoBehaviour
{
    [SerializeField]
    private PlantPlacerModel model = null;

    [SerializeField]
    private float tileWidth = 200f;

    [SerializeField]
    private Terrain targetTerrain = null;

    [SerializeField]
    private GameObject spawnableTreePrefab = null;

    [SerializeField]
    private VegetationPlacer vegetationPlacer;

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
        //SynchronousRegenerateTiles();
        StartCoroutine(PlaceTrees());

        OnLandscapeUpdated(new Vector2Int((int)tileWidth/2, (int)tileWidth/2));
        vegetationPlacer = new VegetationPlacer();
    }

    private void SynchronousRegenerateTiles()
    {
        var pythonRunner = new PlantPlacerPythonRunner(model);

        var thisUpdateTile = Vector2Int.zero;

        pythonRunner.StartGenerating(CollectHeightMapAtTile(thisUpdateTile));

        while(!pythonRunner.PollTileGenerationComplete());

        var tileGenerationResults = pythonRunner.CachedPreviousResult;
        EnqueueTreePlacements(thisUpdateTile, tileGenerationResults.relativeTreePositions);
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

            Debug.Log("Polling for generated tile...");
            yield return new WaitUntil(() => pythonRunner.PollTileGenerationComplete());
            Debug.Log("Generated tile!");

            var tileGenerationResults = pythonRunner.CachedPreviousResult;
            EnqueueTreePlacements(thisUpdateTile, tileGenerationResults.relativeTreePositions);
        }
    }

    private void EnqueueTreePlacements(Vector2Int updateTile, float[,] tileGenerationResults)
    {
        Debug.Log($"Tile co-ords: {updateTile}");
        var tileWorldOrigin = new Vector2(updateTile.x + (tileWidth / 2), updateTile.y + (tileWidth / 2));
        //var worldPositions = tileGenerationResults.Select(localTreePosition => tileWorldOrigin + localTreePosition);

        var worldPositions = vegetationPlacer.GetVegetationPositionsInWorld(tileGenerationResults, tileWidth, tileWorldOrigin);

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

    private float[,] CollectHeightMapAtTile(Vector2Int centerPoint)
    {
        var heightMapResolution = 256;
        var heightMap = new float[heightMapResolution, heightMapResolution];
        var cornerOrigin2d = centerPoint - Vector2.one * tileWidth / 2f;
        var cornerOrigin = Vector3.right * cornerOrigin2d.x + Vector3.forward * cornerOrigin2d.y;
        for (var x = 0; x < heightMapResolution; ++x)
        {
            for (var y = 0; y < heightMapResolution; ++y)
            {
                var offset = (Vector3.right * x + Vector3.forward * y) * tileWidth / heightMapResolution;
                var pollPosition = cornerOrigin + offset;
                heightMap[x, y] = targetTerrain.SampleHeight(pollPosition);
            }
        }
        
        return heightMap;
    }

    private List<Vector2> queuedTreePlacements = new List<Vector2>();
}
