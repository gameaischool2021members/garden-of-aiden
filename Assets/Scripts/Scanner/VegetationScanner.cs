using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VegetationScanner
{
    //Should I just use one variable? 
    private const int textureSizeX = 256;
    private const int textureSizeY = 256;

    private float[,] treeTexture;
    private float[,] bushTexture;

    public VegetationScanner()
    {
        treeTexture = new float[textureSizeX, textureSizeY];
        bushTexture = new float[textureSizeX, textureSizeY];
    }


    /* 
     * Summery: Call this to get your tree treeTexture as a 2D Array
     * Takes:   The position
     * Returns: The treeTexture as a 2D Array with values between 0 and 1
     */
    public float[,] ScanForTrees(Vector2 scannerCenterPoint, float scannerReach, int textureGradiantRadius)
    {
        //Reset for new use
        treeTexture = new float[textureSizeX, textureSizeY];

        List<Vector2> trees = GetTreesInScannerReach(scannerCenterPoint, scannerReach);
        Debug.Log("Number of scanned trees: " + trees.Count);
        foreach (Vector2 tree in trees)
        {
            AddVegetationToTexture(scannerCenterPoint, tree, scannerReach, textureGradiantRadius, "trees");
        }

        return treeTexture;
    }
    
    /* 
     * Summery: Call this to get your bushes treeTexture as a 2D Array
     * Takes:   The position
     * Returns: The treeTexture as a 2D Array with values between 0 and 1
     */
    public float[,] ScanForBushes(Vector2 scannerCenterPoint, float scannerReach, int textureGradiantRadius)
    {
        //Reset for new use
        bushTexture = new float[textureSizeX, textureSizeY];

        List<Vector2> bushes = GetBushesInScannerReach(scannerCenterPoint, scannerReach);
        Debug.Log("Number of scanned bushes: " + bushes.Count);
        foreach (Vector2 bush in bushes)
        {
            AddVegetationToTexture(scannerCenterPoint, bush, scannerReach, textureGradiantRadius, "bushes");
        }

        return bushTexture;
    }


    //Private functions

    // Summery: Calls GetAllTreesPositions() and FilterVegetationOutOfScannerReach()
    private List<Vector2> GetTreesInScannerReach(Vector2 scannerCenterPoint, float scannerReach)
    {
        return FilterVegetationOutOfScannerReach(GetAllTreesPositions(), scannerReach, scannerCenterPoint);
    }
    
    // Summery: Calls GetBushesInScannerReach() and FilterVegetationOutOfScannerReach()
    private List<Vector2> GetBushesInScannerReach(Vector2 scannerCenterPoint, float scannerReach)
    {
        return FilterVegetationOutOfScannerReach(GetAllBushesPositions(), scannerReach, scannerCenterPoint);
    }


    // Summery: Gets all trees in the world (!) 
    private List<Vector2> GetAllTreesPositions()
    {
        List<Vector2> treePositions = new List<Vector2>();

        GameObject[] trees = GameObject.FindGameObjectsWithTag("Trees");
        foreach (GameObject tree in trees)
        {
            Vector2 treePos = new Vector2(tree.transform.position.x, tree.transform.position.z);
            treePositions.Add(treePos);
        }

        return treePositions;
    }
    
    // Summery: Gets all trees in the world (!) 
    private List<Vector2> GetAllBushesPositions()
    {
        List<Vector2> bushPositions = new List<Vector2>();

        GameObject[] bushes = GameObject.FindGameObjectsWithTag("Bushes");
        foreach (GameObject bush in bushes)
        {
            Vector2 bushPosition = new Vector2(bush.transform.position.x, bush.transform.position.z);
            bushPositions.Add(bushPosition);
        }

        return bushPositions;
    }


    /* 
    * Summery: Takes a list of the vegetation and returns a new list with only vegetation within the scanner
    * Takes:   All tree x,y coordinates, and the centerPoint of the scanner
    * Returns: A New list with only vegetation coordinates within the scanner range
    */
    private List<Vector2> FilterVegetationOutOfScannerReach(List<Vector2> vegetationPositions, float scannerReach, Vector2 scannerCenterPoint)
    {
        List<Vector2> vegetationInScannerReach = new List<Vector2>();

        foreach (Vector2 vegetationPosition in vegetationPositions)
        {
            if (Mathf.Abs(scannerCenterPoint.x - vegetationPosition.x) < scannerReach &&
               Mathf.Abs(scannerCenterPoint.y - vegetationPosition.y) < scannerReach)
            {
                vegetationInScannerReach.Add(vegetationPosition);
            }
        }
        return vegetationInScannerReach;
    }


    /* 
     * Summery: Maps the position of the tree within the scanner to the treeTexture and makes a radiant around it
     * Takes:   World position of the three (x and y; in unity terms)
     */
    private void AddVegetationToTexture(Vector2 scannerCenterPoint, Vector2 vegetationPosition, float scannerReach, int textureGradiantRadius, string type)
    {
        //Change to scannerCenter point of reference (hope its right aaaahhh)
        vegetationPosition -= scannerCenterPoint;

        Vector2 textureCoordinates;
        textureCoordinates.x = RemapValue(vegetationPosition.x, -scannerReach, scannerReach, 0f, textureSizeX);
        textureCoordinates.y = RemapValue(vegetationPosition.y, -scannerReach, scannerReach, 0f, textureSizeY);
        Vector2Int textureTreeCoordinatesRounded = new Vector2Int(Mathf.RoundToInt(textureCoordinates.x), Mathf.RoundToInt(textureCoordinates.y));

        // Creates gradiant and sets tree position to 1 to be sure
        CreateGradiantAroundVegetationInTexture(textureTreeCoordinatesRounded, textureGradiantRadius, type);
    }


    /* 
     * Summery: Creates a round gradient around the specified pixel
     * Takes:   Pixel coordinate of tree
     * Misc:    Loops through the square (length textureGradiantRadius * 2) with the tree in the center with 
     *          checks the distance from each pixel form center
     *          sets value of pixel accordingly
     */
    private void CreateGradiantAroundVegetationInTexture(Vector2Int textureCoordinatesRounded, int textureGradiantRadius, string type)
    {
        for (int x = textureCoordinatesRounded.x - textureGradiantRadius; x <= textureCoordinatesRounded.x + textureGradiantRadius; x++)
        {
            //Index out of bounds check
            if (0 > x || x >= textureSizeX) { continue; }

            for (int y = textureCoordinatesRounded.y - textureGradiantRadius; y <= textureCoordinatesRounded.y + textureGradiantRadius; y++)
            {
                // Index out of bounds check
                if (0 > y || y >= textureSizeY) { continue; }

                Vector2Int currentPixel = new Vector2Int(x, y);
                float distanceToVeg = Vector2Int.Distance(currentPixel, textureCoordinatesRounded);
                distanceToVeg = Mathf.Abs(distanceToVeg); //just to be shure
                if (distanceToVeg < textureGradiantRadius)
                {
                    // Calculate pixel intensity (inversed here, pixel with the most distance has value 1.0f)
                    float textureValue = RemapValue(distanceToVeg, 0f, textureGradiantRadius, 0f, 1.0f);
                    textureValue = 1 - textureValue;
                    if (type == "trees")
                    {
                        // Dont set the value if another tree is closer to this point
                        if (treeTexture[currentPixel.x, currentPixel.y] < textureValue)
                        {
                            treeTexture[currentPixel.x, currentPixel.y] = textureValue;
                        }
                    }
                    else if (type == "bushes")
                    {
                        // Dont set the value if another bush is closer to this point
                        if (bushTexture[currentPixel.x, currentPixel.y] < textureValue)
                        {
                            bushTexture[currentPixel.x, currentPixel.y] = textureValue;
                        }
                    }
                    else
                    {
                        Debug.LogError("Wrong type of vegetation has been passed to gradiant generator!");
                    }
                }
            }
        }
    }


    /* 
     * Summery: Takes a value within a range and returns a value at the same percentage point at onther range
     * Returns: Mapped value
     * Misc:    Watch out for 0 divisions !!!!
     * Example: RemapValue(6,   0, 10,   0, 50) returns 30
     *          Maps value in range 0 to 10 to value in range 0 to 50
     */
    private float RemapValue(float inValue, float minIn, float maxIn, float minOut, float maxOut)
    {
        return (inValue - minIn) * (maxOut - minOut) / (maxIn - minIn) + minOut;
    }
}

