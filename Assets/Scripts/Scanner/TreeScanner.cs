using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeScanner
{
    //Should i just use one variable? 
    private int textureSizeX = 256;
    private int textureSizeY = 256;
    private int textureGradiantRadius = 10;

    private float[,] texture;
    public const float scannerReach = 10f;
    private Vector2 scanerCenterPoint = new Vector2(0f, 0f);


    public TreeScanner()
    {
        texture = new float[textureSizeX, textureSizeY];
    }



    /* 
     * Summery: Call this to get your tree texture as a 2D Array
     * Takes:   The position
     * Returns: The texture as a 2D Array with values between 0 and 1
     */
    public float[,] ScannForTrees(Vector2 scanerCenterPoint)
    {
        //Reset for new use
        texture = new float[textureSizeX, textureSizeY];

        this.scanerCenterPoint = scanerCenterPoint;

        List<Vector2> trees = GetTreesInScannerReach();
        foreach (Vector2 tree in trees)
        {
            AddTreeToTexture(tree);
        }

        return texture;
    }


    //Private functions

    // Summery: Calles GetAllTreePositions() and FilterTreesOutOfScannerReach()
    private List<Vector2> GetTreesInScannerReach()
    {
        return FilterTreesOutOfScannerReach(GetAllTreePositions(), scanerCenterPoint);
    }


    // Summery: Gets all trees in the world (!) 
    private List<Vector2> GetAllTreePositions()
    {
        List<Vector2> treePositions = new List<Vector2>();

        GameObject[] trees = GameObject.FindGameObjectsWithTag("Trees");
        foreach (GameObject tree in trees)
        {
            Vector2 treePos = new Vector2(tree.transform.position.x, tree.transform.position.y);
            treePositions.Add(treePos);
        }

        return treePositions;
    }


    /* 
    * Summery: Takes a list of trees and returns a new list with only trees within the scanner
    * Takes:   All tree x,y coordinates, and the centerPoint of the scanner
    * Returns: A New list with only tree coordinates within the scanner range
    */
    private List<Vector2> FilterTreesOutOfScannerReach(List<Vector2> treePositions, Vector2 scanerCenterPoint)
    {
        List<Vector2> treesInScannerReach = new List<Vector2>();

        foreach (Vector2 treePosition in treePositions)
        {
            if (Mathf.Abs(scanerCenterPoint.x - treePosition.x) < scannerReach &&
               Mathf.Abs(scanerCenterPoint.y - treePosition.y) < scannerReach)
            {
                treesInScannerReach.Add(treePosition);
            }
        }
        return treesInScannerReach;
    }


    /* 
     * Summery: Maps the position of the tree within the scanner to the texture and makes a radiant around it
     * Takes:   World position of the three (x and y; in unity terms)
     */
    private void AddTreeToTexture(Vector2 treePosition)
    {
        //Change to scanerCenter point of reffrence (hope its right aaaahhh)
        treePosition -= scanerCenterPoint;

        Vector2 textureCoardinates;
        textureCoardinates.x = RemapValue(treePosition.x, -scannerReach, scannerReach, 0f, textureSizeX);
        textureCoardinates.y = RemapValue(treePosition.y, -scannerReach, scannerReach, 0f, textureSizeY);
        Vector2Int textureTreeCoardinatesRounded = new Vector2Int(Mathf.RoundToInt(textureCoardinates.x), Mathf.RoundToInt(textureCoardinates.x));

        //Crates gradiant and setes three position to 1 to be shure
        CreateGradiantArondTreeInTexture(textureTreeCoardinatesRounded);
        texture[Mathf.RoundToInt(textureCoardinates.x), Mathf.RoundToInt(textureCoardinates.x)] = 1f;
    }


    /* 
     * Summery: Creates a round gradient around the specified pixel
     * Takes:   Pixel coordinate of tree
     * Misc:    Loops through the square (length textureGradiantRadius * 2) with the tree in the center with 
     *          checks the distance from each pixel form center
     *          sets value of pixel accordingly
     */
    private void CreateGradiantArondTreeInTexture(Vector2Int textureTreeCoardinatesRounded)
    {
        for (int x = -textureGradiantRadius; x <= textureGradiantRadius; x++)
        {
            //Index out of bounds check
            if (0 > x || x > textureSizeX) { continue; }

            for (int y = -textureGradiantRadius; y <= textureGradiantRadius; y++)
            {
                //Index out of bounds check
                if (0 > y || y > textureSizeY) { continue; }

                Vector2Int currentPixel = new Vector2Int(x, y);
                float distanceToTree = Vector2Int.Distance(currentPixel, textureTreeCoardinatesRounded);
                distanceToTree = Mathf.Abs(distanceToTree); //just to be shure
                if (distanceToTree < textureGradiantRadius)
                {
                    //Callculate pixel intensety (inversed here, pixel with the most distance has value 1.0f)
                    float textureValue = RemapValue(distanceToTree, 0f, textureGradiantRadius, 0f, 1.0f);
                    textureValue = 1 - textureValue;

                    //Dont set the value if another tree is closer to this point
                    if (texture[currentPixel.x, currentPixel.y] < textureValue)
                    {
                        texture[currentPixel.x, currentPixel.y] = textureValue;
                    }

                }
            }
        }
    }


    /* 
     * Summery: Takes a value within a range and returns a value at the same percentage point at onther range
     * Returns: Maped value
     * Misc:    Whatch our for 0 divisons !!!!
     * Example: RemapValue(6,   0, 10,   0, 50) returns 30
     *          Maps value in range 0 to 10 to value in range 0 to 50
     */
    private float RemapValue(float inValue, float minIn, float maxIn, float minOut, float maxOut)
    {
        return (inValue - minIn) * (maxOut - minOut) / (maxIn - minIn) + minOut;
    }
}

