using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Added this for float to decimal conversion
using System;


public class TreePlacer
{
	//The texture
	private decimal [,] decimalTexture;
	//Used for the search
	private TexturePixelNode[,] textureNodes;
	private bool[,] crossedOutPixels;
	private bool[,] pixelsThatAreTrees;


	//Is this right? Or do i rotate the texture here
	private int textureSizeX;
	private int textureSizeY;

	public List<Vector2Int> GetTreePositionsInOnTexture(float[,] texture)
	{
		InitializeData(texture);
		ReduceNumberOfColorsInTexture();
		List<Vector2Int> threePositions = FindTrees();
		return threePositions;
	}

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
				decimalTexture[x, y] = Convert.ToDecimal(texture[x, y]);
				crossedOutPixels[x, y] = false; //Just to make shure

				pixelsThatAreTrees[x, y] = false;
			}
		}
	}

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


	//Recursive search for a tree
	//always returns  (0,0)
	private Vector2Int FindTreeForPixel(TexturePixelNode pixel)
    {
		// InitializeSearchSpace();
		Vector2Int treePosition = new Vector2Int(0,0);
		GroupIslandAnswerStruct result = GroupIslandBFS(pixel);

		if (result.foundBiggerValue)
		{
			CrossOutLowNonTreePixels(result.notesInIsland);
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
					CrossOutLowNonTreePixels(result.notesInIsland);
					return new Vector2Int(-1, -1);
                }
			}


			//YAY! We found a NEW tree (I hope)
			treePosition = AveragePoint(result.notesInIsland);
			CrossOutLowNonTreePixels(result.notesInIsland);

			//Mark trees as trees
			foreach(TexturePixelNode tree in result.notesInIsland)
            {
				pixelsThatAreTrees[tree.position.x, tree.position.y] = true;
            }
		}

		return treePosition;
	}

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

	private GroupIslandAnswerStruct GroupIslandBFS(TexturePixelNode start)
	{
		Queue<TexturePixelNode> queue = new Queue<TexturePixelNode>();

		//Begin: return data
		bool foundBiggerValue = false;
		Vector2Int biggerValueNotePosition = new Vector2Int(0, 0);
		List<TexturePixelNode> notesInIsland = new List<TexturePixelNode>();
		//End: return data

		decimal islandValue = start.value;
		queue.Enqueue(start);
		notesInIsland.Add(start);
		// start.isVisited = true;
		bool[,] isVisited = new bool[textureSizeX, textureSizeY];
		for (int x = 0; x < textureSizeX; x++)
		{
			for (int y = 0; y < textureSizeY; y++)
			{
				isVisited[x, y] = false;
			}
		}

		isVisited[start.position.x, start.position.y] = true;

		while (queue.Count > 0)
		{
			TexturePixelNode currentNote = queue.Dequeue();

			foreach (TexturePixelNode neighbor in currentNote.Get8Neighbors(textureNodes))
			{
				if (!isVisited[neighbor.position.x, neighbor.position.y])
				{
					if (neighbor.value == islandValue)
					{
						queue.Enqueue(neighbor);
						notesInIsland.Add(neighbor);
					}
					else if (islandValue < neighbor.value)
					{
						foundBiggerValue = true;
						biggerValueNotePosition = neighbor.position;
					}
					isVisited[neighbor.position.x, neighbor.position.y] = true;
				}
			}
		}

		return new GroupIslandAnswerStruct(foundBiggerValue, biggerValueNotePosition, notesInIsland);
	}

	private void CrossOutLowNonTreePixels(List<TexturePixelNode> pixels)
	{
		foreach (TexturePixelNode node in pixels)
		{
			//decimalTexture[node.position.x, node.position.y] = -1;
			crossedOutPixels[node.position.x, node.position.y] = true;
			//Do it for both to be sure but only use decimalTexture for the values and textureNodes for search
			//textureNodes[node.position.x, node.position.y].value = -1;
		}
	}


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
}


public class TexturePixelNode
{
	public decimal value;
	// public bool isVisited = false;
	// public bool isFinished = false;
	public Vector2Int position;

	public TexturePixelNode(Vector2Int position, decimal value)
    {
		this.value = value;
		this.position = position;
    }

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

public struct GroupIslandAnswerStruct
{
	public bool foundBiggerValue;
	public Vector2Int biggerValueNotePosition;
	public List<TexturePixelNode> notesInIsland;

	public GroupIslandAnswerStruct(bool foundBiggerValue, Vector2Int biggerValueNotePosition, List<TexturePixelNode> notesInIsland)
    {
		this.foundBiggerValue = foundBiggerValue;
		this.biggerValueNotePosition = biggerValueNotePosition;
		this.notesInIsland = notesInIsland;
    }
}

