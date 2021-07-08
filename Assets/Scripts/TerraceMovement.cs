using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerraceMovement : MonoBehaviour
{
    public Terrain terrain;


    // Start is called before the first frame update
    void Start()
    {
        ResetTerrain();
    }

    // Update is called once per frame
    void Update()
    {

    }


    private void OnMouseDown()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
            if (hit.collider != null)
            {
                // find the heightmap position of hit for putting it OnMouseDown
                float relativeHitTerX = (hit.point.x - terrain.transform.position.x) / terrain.terrainData.size.x;
                float relativeHitTerZ = (hit.point.z - terrain.transform.position.z) / terrain.terrainData.size.z;

                float relativeTerCoordX = terrain.terrainData.heightmapResolution * relativeHitTerX;
                float relativeTerCoordZ = terrain.terrainData.heightmapResolution * relativeHitTerZ;

                int hitPointTerX = Mathf.FloorToInt(relativeTerCoordX);
                int hitPointTerZ = Mathf.FloorToInt(relativeTerCoordZ);

                float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);


                if (heights[hitPointTerZ, hitPointTerX] == 0)
                {
                    heights[hitPointTerZ, hitPointTerX] = 0.01f;
                }
                else
                {
                    heights[hitPointTerZ, hitPointTerX] = heights[hitPointTerZ, hitPointTerX] * 5.05f; //the 5.05f parameter is the high of each click
                }

                terrain.terrainData.SetHeights(0, 0, heights);
                terrain.terrainData.SyncHeightmap();

            }
    }


    private void ResetTerrain()
    {
        float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);


        for (var z = 0; z < terrain.terrainData.heightmapResolution; z++)
        {
            for (var x = 0; x < terrain.terrainData.heightmapResolution; x++)
            {
                heights[z, x] = 0;
            }
        }

        terrain.terrainData.SetHeights(0, 0, heights);
    }

}    


