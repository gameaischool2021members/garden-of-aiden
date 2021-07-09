using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerraceMovement : MonoBehaviour
{
    public Terrain terrain;

    public float heightChange = 1.0f;
    public float brushRadius = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        ResetTerrain();
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseIsDown)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
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

                    AddRadialGradient(ref heights, new Vector2Int(hitPointTerZ, hitPointTerX), Time.deltaTime);

                    terrain.terrainData.SetHeights(0, 0, heights);
                    terrain.terrainData.SyncHeightmap();
                }
            }
        }
    }

    private void OnMouseDown()
    {
        mouseIsDown = true;
    }

    private void OnMouseUp()
    {
        mouseIsDown = false;
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

    private void AddRadialGradient(ref float[,] heights, Vector2Int center, float deltaTime)
    {
        var brushRadiusInt = (int)Mathf.Ceil(brushRadius);
        for (var xOffset = -brushRadiusInt; xOffset < brushRadiusInt; ++xOffset)
        {
            var texelX = center.x + xOffset;
            if (texelX < 0 || texelX >= heights.GetLength(0))
            {
                continue;
            }

            for (var yOffset = -brushRadiusInt; yOffset < brushRadiusInt; ++yOffset)
            {
                var texelY = center.y + yOffset;
                if (texelY < 0 || texelY >= heights.GetLength(1))
                {
                    continue;
                }

                var radialHeightChange = CalculateRadialGradientAtOffset(xOffset, yOffset) * heightChange * deltaTime;
                heights[texelX, texelY] += radialHeightChange;
            }
        }
    }

    private float CalculateRadialGradientAtOffset(int xOffset, int yOffset)
    {
        var distance = Mathf.Sqrt(xOffset * xOffset + yOffset * yOffset);
        var t = Mathf.InverseLerp(0f, brushRadius, distance);
        var cosResult = Mathf.Cos(t * Mathf.PI);
        var impact = (cosResult + 1f) * 0.5f;
        return impact;
    }

    private bool mouseIsDown = false;
}    
