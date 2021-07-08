using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Added this for float to decimal conversion
using System;


public class TreePlacer
{
	//The texture
	private decimal [,] decimalTexture;
	private int textureSizeX;
	private int textureSizeY;

	//Data for the search
	private TexturePixelNode[,] textureNodes;
	private bool[,] crossedOutPixels;
	private bool[,] pixelsThatAreTrees;

	/* Summery: Gets a (single channel?) texture with circular gradients
	 *			Every circular gradient represents a tree
	 *			Does some processing to find exact position of trees        
	 * Returns:	List of tree position on texture
	 * Misc:    Orientation of input data is consistent with orientation of output data
	 */
	public List<Vector2Int> GetTreePositionsInTexture(float[,] texture)
	{
		InitializeData(texture);
		ReduceNumberOfColorsInTexture();
		List<Vector2Int> threePositions = FindTrees();
		return threePositions;
	}
	
	/* Summery: Gets a (single channel?) texture with circular gradients
	 *			Every circular gradient represents a tree
	 *			Does some processing to find exact position of trees        
	 * Returns:	List of tree position on texture
	 * Misc:    Orientation of input data is consistent with orientation of output data
	 */
	public List<Vector2> GetTreePositionsInWorld(float[,] texture, float scannerReach, Vector2 scannerPosition)
	{
		List<Vector2Int> treePositionsInTexture = GetTreePositionsInTexture(texture);
		List<Vector2> treePositions = new List<Vector2>();
		foreach (Vector2Int treePosition in treePositionsInTexture)
		{
			// We need to take the current position of the scanner into the consideration
			float x = RemapValue(treePosition.x, 0, textureSizeX, -scannerReach + scannerPosition.x, scannerReach + scannerPosition.x);
			float y = RemapValue(treePosition.y, 0, textureSizeY, -scannerReach + scannerPosition.y, scannerReach + scannerPosition.y);
			treePositions.Add(new Vector2(x, y));
		}
		return treePositions;
	}

	//Summery: Data initialization
	void InitializeData(float[,] texture)
	{
		textureSizeX = texture.GetLength(0);
		textureSizeY = texture.GetLength(1);

		decimalTexture = new decimal[textureSizeX, textureSizeY];
		crossedOutPixels = new bool[textureSizeX, textureSizeY];
		pixelsThatAreTrees = new bool[textureSizeX, textureSizeY];

		for (int x = 0; x < textureSizeX; x++)
		{
			for (int y = 0; y < textureSizeY; y++)
			{
				decimalTexture[x, y] = Convert.ToDecimal(texture[x, y]); //For save comparisons(==) and rounding
				crossedOutPixels[x, y] = false; 
				pixelsThatAreTrees[x, y] = false;
			}
		}
	}


	//Summery: To make the search easier, each value of the texture (between 0 and 1) gets rounded so a given decimal place
	void ReduceNumberOfColorsInTexture()
	{
		for (int x = 0; x < textureSizeX; x++)
		{
			for (int y = 0; y < textureSizeY; y++)
			{
				decimalTexture[x, y] = decimal.Round(decimalTexture[x, y], 1);
			}
		}
	}

   /*Summery:   Linear iteration over the texture halts when potential tree is found
	*			if it is a tree adds the tree to the list of tree positions
	*			continues iteration
	*			
	*Returns:	List of tree position on texture
	*/
	private List<Vector2Int> FindTrees()
    {
		InitializeSearchSpace();
		List<Vector2Int> treePositionsOnTexture = new List<Vector2Int>();
		for (int x = 0; x < textureSizeX; x++)
		{
			for (int y = 0; y < textureSizeY; y++)
			{
				// We're not checking crossed out pixels
				// Also only checking the pixels which have non zero values
				if (!crossedOutPixels[x, y] && 0 < textureNodes[x, y].value) 
                {
					Vector2Int result = FindTreeForPixel(textureNodes[x, y]);
					if(! (result == new Vector2Int(-1, -1)) )
                    {
						treePositionsOnTexture.Add(result);
					}
				}			
			}
		}
		return treePositionsOnTexture;
    }


	/* Summery: Gets called when FindTrees() finds a pixel (with 0<value) that wasn't looked at by GroupPixelsWithTheSameValueToIslandBFS()
	 *			This can mean that a new tree is found
	 *			Starts a recursion until GroupPixelsWithTheSameValueToIslandBFS() dos not find a island that has an higher value
	 *			Checks if island is a new tree, marks data, and returns position of the tree
	 * Takes:   The node of a pixel with value>0 that wasn't looked at before
	 * Returns:	Single Vector2Int that has the position of the tree or a Vector2Int(-1, -1) if the tree was already known
	 */
	private Vector2Int FindTreeForPixel(TexturePixelNode pixel)
    {
		// InitializeSearchSpace();
		Vector2Int treePosition = new Vector2Int(0,0);
		GroupIslandAnswerStruct result = GroupPixelsWithTheSameValueToIslandBFS(pixel);

		if (result.foundBiggerValue)
		{
			CrossOutExaminedPixels(result.notesInIsland);
			//When no bigger value is left recursion stops
			treePosition = FindTreeForPixel(textureNodes[result.biggerValueNotePosition.x, result.biggerValueNotePosition.y]);
		}
		else
        {

			foreach (TexturePixelNode tree in result.notesInIsland)
			{
				int x = tree.position.x;
				int y = tree.position.y;
				if(pixelsThatAreTrees[x, y])
                {
					//Naw its a old tree
					CrossOutExaminedPixels(result.notesInIsland);
					return new Vector2Int(-1, -1);
                }
			}


			//YAY! We found a NEW tree (I hope)
			treePosition = AveragePoint(result.notesInIsland);
			CrossOutExaminedPixels(result.notesInIsland);

			//Mark trees as trees
			foreach(TexturePixelNode tree in result.notesInIsland)
            {
				pixelsThatAreTrees[tree.position.x, tree.position.y] = true;
            }
		}

		return treePosition;
	}


	/* Summery: Initializes the search space
	 *			with nodes that hold some extra data that could be extended upon
	 */
	private void InitializeSearchSpace()
	{
		textureNodes = new TexturePixelNode[textureSizeX, textureSizeY];

		for (int x = 0; x < textureSizeX; x++)
		{
			for (int y = 0; y < textureSizeY; y++)
			{
				textureNodes[x, y] = new TexturePixelNode(new Vector2Int(x, y), decimalTexture[x, y]);
			}
		}
	}


	/* Summery: Goups pixels in to an "island". All pixels are adjacent to each other and share the same value.
	 *			Also checks if there is an adjacent group of pixels with a lager value
	 * Takes:   The node of the starting pixel
	 * Returns:	A GroupIslandAnswerStruct for more information on this go there	
	 */
	private GroupIslandAnswerStruct GroupPixelsWithTheSameValueToIslandBFS(TexturePixelNode startPixelNode)
	{
		//Initialize queue for BFS
		Queue<TexturePixelNode> queue = new Queue<TexturePixelNode>();

		//Begin: Initialize return data
		bool foundBiggerValue = false;
		Vector2Int biggerValueNotePosition = new Vector2Int(0, 0);
		List<TexturePixelNode> notesInIsland = new List<TexturePixelNode>();
		//End: Initialize return data

		//Begin: Initialize data for BFS
		decimal islandValue = startPixelNode.value;
		queue.Enqueue(startPixelNode);
		notesInIsland.Add(startPixelNode);
		// start.isVisited = true;
		bool[,] isVisited = new bool[textureSizeX, textureSizeY];
		for (int x = 0; x < textureSizeX; x++)
		{
			for (int y = 0; y < textureSizeY; y++)
			{
				isVisited[x, y] = false;
			}
		}
		isVisited[startPixelNode.position.x, startPixelNode.position.y] = true;
		//End: Initialize data for BFS

		//Breadth first search
		while (queue.Count > 0)
		{
			TexturePixelNode currentNote = queue.Dequeue();

			foreach (TexturePixelNode neighbor in currentNote.Get8Neighbors(textureNodes))
			{
				if (!isVisited[neighbor.position.x, neighbor.position.y])
				{
					//Only goups pixels with the same value
					if (neighbor.value == islandValue)
					{
						queue.Enqueue(neighbor);
						notesInIsland.Add(neighbor);					 //For the return data
					}
					else if (islandValue < neighbor.value)
					{
						foundBiggerValue = true;						 //For the return data
						biggerValueNotePosition = neighbor.position;	 //For the return data
					}
					isVisited[neighbor.position.x, neighbor.position.y] = true;
				}
			}
		}

		return new GroupIslandAnswerStruct(foundBiggerValue, biggerValueNotePosition, notesInIsland);
	}


	/* Summery: Pixels that have been looked at by GroupIslandBFS() 
	 *			have to be crossed out so that the search on them won't get called twice
	 * Takes:   A the list of nodes that GroupIslandBFS() found
	 * Misc:	The check on crossed out pixels is in/for the FindTrees() function
	 *			not GroupIslandBFS()
	 */
	private void CrossOutExaminedPixels(List<TexturePixelNode> pixels)
	{
		foreach (TexturePixelNode node in pixels)
		{
			//decimalTexture[node.position.x, node.position.y] = -1;
			crossedOutPixels[node.position.x, node.position.y] = true;
			//Do it for both to be sure but only use decimalTexture for the values and textureNodes for search
			//textureNodes[node.position.x, node.position.y].value = -1;
		}
	}

	/* 
	 * Summery: Takes a group of nodes got idetified as a tree. This function figures out the "exact" position of the tree
	 * Takes:   A list of nodes that got idetified as a tree
	 * Returns: A singele Verctor2Int that is the position of the tree on the texture
	 */
	private Vector2Int AveragePoint(List<TexturePixelNode> pixels)
    {
		int averageX = 0;
		int averageY = 0;

		foreach (TexturePixelNode pixel in pixels)
        {
			averageX += pixel.position.x;
			averageY += pixel.position.y;
		}


		float pixelCount = pixels.Count;
		averageX = Mathf.RoundToInt(averageX / pixelCount);
		averageY = Mathf.RoundToInt(averageY / pixelCount);

		return new Vector2Int(averageX, averageY);

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




/* 
 * Summery:					Class
 * notesInIsland:			A List of pixels all adjacent to each other with the same value called "island"
 * foundBiggerValue:		Bool that is true if adjacent to the group/island is a pixel with a larger value
 * biggerValueNotePosition: Position of the pixel with the lager value 
 * 
 * Misc: Might be obsolete if we want to construct an extra bool array during the BFS
 *       but i won't change it now bc of time and i don't want to break stuff
 *       also might be useful for extension later
 */
public class TexturePixelNode
{
	public decimal value;
	// public bool isVisited = false;
	// public bool isFinished = false; //shoud not be needed in BFS
	public Vector2Int position;

	//Constructor
	public TexturePixelNode(Vector2Int position, decimal value)
    {
		this.value = value;
		this.position = position;
    }


	/* 
	 * Summery: Gets the 4 direct neighbors of the node this function is called on
	 * Takes:   Array of nodes that also holds the neighbors of this node (and the node itself)
	 * Returns: List of the 4 neighbors
	 */
	public List<TexturePixelNode> Get4Neighbors(TexturePixelNode[,] textureNodes)
	{
		List<TexturePixelNode> neighbors = new List<TexturePixelNode>();

		if (0 < position.x) 
		{ 
			neighbors.Add(textureNodes[position.x - 1, position.y]);
		}
		if (position.y < textureNodes.GetLength(1) - 1)
		{
			neighbors.Add(textureNodes[position.x, position.y + 1]);
		}
		if (position.x < textureNodes.GetLength(0) - 1)
		{
			neighbors.Add(textureNodes[position.x + 1, position.y]);
		}
		if (0 < position.y)
		{
			neighbors.Add(textureNodes[position.x, position.y - 1]);
		}

		return neighbors;
	}

	/* 
	 * Summery: Gets the 4 direct neighbors and the 4 diagonal neighbors of the node this function is called on
	 * Takes:   Array of nodes that also holds the neighbors of this node (and the node itself)
	 * Returns: List of the 8 neighbors
	 */
	public List<TexturePixelNode> Get8Neighbors(TexturePixelNode[,] textureNodes)
	{
		List<TexturePixelNode> neighbors = new List<TexturePixelNode>();


		if (0 < position.x)
		{
			neighbors.Add(textureNodes[position.x - 1, position.y]);
		}
		if (position.y < textureNodes.GetLength(1) - 1)
		{
			neighbors.Add(textureNodes[position.x, position.y + 1]);
		}
		if (position.x < textureNodes.GetLength(0) - 1)
		{
			neighbors.Add(textureNodes[position.x + 1, position.y]);
		}
		if (0 < position.y)
		{
			neighbors.Add(textureNodes[position.x, position.y - 1]);
		}

		if (0 < position.x && 0 < position.y)
		{
			neighbors.Add(textureNodes[position.x - 1, position.y -1]);
		}
		if (position.x < textureNodes.GetLength(0) - 1 && position.y < textureNodes.GetLength(1) - 1)
		{
			neighbors.Add(textureNodes[position.x + 1, position.y + 1]);
		}
		if (position.x < textureNodes.GetLength(0) - 1 && 0 < position.y)
		{
			neighbors.Add(textureNodes[position.x + 1, position.y - 1]);
		}
		if (position.x < textureNodes.GetLength(0) - 1 && 0 < position.y)
		{
			neighbors.Add(textureNodes[position.x + 1, position.y - 1]);
		}

		return neighbors;
	}
}




/* 
 * Summery: Data struct that gets returned  by GroupIslandBFS( )
 * notesInIsland: A List of pixels all adjacent to each other with the same value called "island"
 * foundBiggerValue: Bool that is true if adjacent to the group/island is a pixel with a larger value
 * biggerValueNotePosition: Position of the pixel with the lager value 
 */
public struct GroupIslandAnswerStruct
{

	public List<TexturePixelNode> notesInIsland;
	public bool foundBiggerValue;
	public Vector2Int biggerValueNotePosition;

	public GroupIslandAnswerStruct(bool foundBiggerValue, Vector2Int biggerValueNotePosition, List<TexturePixelNode> notesInIsland)
    {
		this.foundBiggerValue = foundBiggerValue;
		this.biggerValueNotePosition = biggerValueNotePosition;
		this.notesInIsland = notesInIsland;
    }
}

