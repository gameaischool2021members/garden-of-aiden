using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreePlacerTest : MonoBehaviour
{
    public Terrain terrain;
    private VegetationScanner vegetationScanner;
    private VegetationPlacer treePlacer;
    private VegetationPlacer bushPlacer;
    private bool didScanTrees = false;
    private bool didScanBushes = false;
    private float[,] treeTexture;
    private float[,] bushTexture;
    private float scannerReach = 50f;

    [SerializeField]
    private GameObject treePrefab;
    [SerializeField]
    private GameObject bushPrefab;

    private void Awake()
    {
        vegetationScanner = new VegetationScanner();
        treePlacer = new VegetationPlacer();
        bushPlacer = new VegetationPlacer();
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            Vector2 position = new Vector2(transform.position.x, transform.position.z);
            
            // Scanning the Trees
            treeTexture = vegetationScanner.ScanForTrees(position, scannerReach, 25);
            didScanTrees = true;

            List<GameObject> trees = FilterVegetationOutOfScannerReach(GetAllTrees(), position);

            foreach (GameObject tree in trees)
            {
                TreeParticles particles = tree.GetComponent<TreeParticles>();
                if(particles != null)
                {
                    particles.DestroyTree();
                }
                Debug.Log("X: " + tree.transform.position.x + " Y: " + tree.transform.position.z);
                Destroy(tree);
            }
            
            // Scanning the bushes
            bushTexture = vegetationScanner.ScanForBushes(position, scannerReach, 25);
            didScanBushes = true;

            List<GameObject> bushes = FilterVegetationOutOfScannerReach(GetAllBushes(), position);

            foreach (GameObject bush in bushes)
            {
                TreeParticles particles = bush.GetComponent<TreeParticles>();
                if(particles != null)
                {
                    particles.DestroyTree();
                }
                Debug.Log("X: " + bush.transform.position.x + " Y: " + bush.transform.position.z);
                Destroy(bush);
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (didScanTrees)
            {
                Vector2 scannerPosition = new Vector2(transform.position.x, transform.position.z);
                List<Vector2> treePositions = treePlacer.GetVegetationPositionsInWorld(treeTexture, scannerReach, scannerPosition);
                Debug.Log("Number of spawned Trees: " + treePositions.Count);

                foreach (Vector2 treePosition2D in treePositions)
                {
                    Debug.Log("X: " + treePosition2D.x + " Y: " + treePosition2D.y);
                    // To find the elevation, SampleHeight function of the terrain is used
                    Vector3 sampleHeightInput = new Vector3(treePosition2D.x, 0, treePosition2D.y);
                    Vector3 treePosition = new Vector3(treePosition2D.x, terrain.SampleHeight(sampleHeightInput),
                        treePosition2D.y);
                    Instantiate(treePrefab, treePosition, Quaternion.identity);
                }
                // Resetting to prevent several placing
                didScanTrees = false;
            }
            
            if (didScanBushes)
            {
                Vector2 scannerPosition = new Vector2(transform.position.x, transform.position.z);
                List<Vector2> bushPositions = treePlacer.GetVegetationPositionsInWorld(bushTexture, scannerReach, scannerPosition);
                Debug.Log("Number of spawned Bushes: " + bushPositions.Count);

                foreach (Vector2 bushPosition2D in bushPositions)
                {
                    Debug.Log("X: " + bushPosition2D.x + " Y: " + bushPosition2D.y);
                    // To find the elevation, SampleHeight function of the terrain is used
                    Vector3 sampleHeightInput = new Vector3(bushPosition2D.x, 0, bushPosition2D.y);
                    Vector3 bushPosition = new Vector3(bushPosition2D.x, terrain.SampleHeight(sampleHeightInput),
                        bushPosition2D.y);
                    Instantiate(bushPrefab, bushPosition, Quaternion.identity);
                }
                // Resetting to prevent several placing
                didScanBushes = false;
            }
        }
    }


    // Summery: Gets all Trees in the world (!) 
    public List<GameObject> GetAllTrees()
    {
        List<GameObject> trees = new List<GameObject>();

        foreach (GameObject tree in GameObject.FindGameObjectsWithTag("Trees"))
        {
            trees.Add(tree);
        }

        return trees;
    }
    
    // Summery: Gets all Bushes in the world (!) 
    public List<GameObject> GetAllBushes()
    {
        List<GameObject> bushes = new List<GameObject>();

        foreach (GameObject bush in GameObject.FindGameObjectsWithTag("Bushes"))
        {
            bushes.Add(bush);
        }

        return bushes;
    }


    /* 
    * Summery: Takes a list of vegies and returns a new list with only vegies within the scanner
    * Takes:   All tree x,y coordinates, and the centerPoint of the scanner
    * Returns: A New list with only tree coordinates within the scanner range
    */
    public List<GameObject> FilterVegetationOutOfScannerReach(List<GameObject> vegies, Vector2 scannerCenterPoint)
    {
        List<GameObject> treesInScannerReach = new List<GameObject>();

        foreach (GameObject vegie in vegies)
        {
            Vector3 vegiePosition = vegie.transform.position;

            if (Mathf.Abs(scannerCenterPoint.x - vegiePosition.x) < 50f &&
                Mathf.Abs(scannerCenterPoint.y - vegiePosition.z) < 50f)
            {
                treesInScannerReach.Add(vegie);
            }
        }

        return treesInScannerReach;
    }
}
