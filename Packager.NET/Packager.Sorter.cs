using System;
using System.Collections.Generic;

namespace Packager
{

	public class Sorter
	{
		public int[] result;
		public List<string> Sorted = new List<string>();

		public Sorter(Dictionary<string, Asset> assets)
		{
			List<Asset> list = new List<Asset>();

			foreach (Asset asset in assets.Values)
			{
				foreach (string provides in asset.Provides)
				{
					var newAsset = new Asset()
					{
						Name = asset.Path
					};
					newAsset.Provides.Add(provides);
					newAsset.Requires.AddRange(asset.Requires);
					list.Add(newAsset);
				}
			}

			this.GenericSorter(list);

			foreach (int i in this.result)
			{
				if (!Sorted.Contains(list[i].Name))
				{
					Sorted.Add(list[i].Name);
				}
			}
		}

		public void GenericSorter(List<Asset> assets)
		{
			Dictionary<string, int> indexes = new Dictionary<string, int>();
			TopologicalSorter sorter = new TopologicalSorter(assets.Count);

			for (int i = 0; i < assets.Count; i++)
			{
				indexes[assets[i].Provides[0].ToLower()] = sorter.AddVertex(i);
			}
			for (int i = 0; i < assets.Count; i++)
			{
				if (assets[i].Requires != null)
				{
					for (int j = 0; j < assets[i].Requires.Count; j++)
					{
						sorter.AddEdge(i, indexes[assets[i].Requires[j].ToLower()]);
					}
				}
			}

			int[] result = sorter.Sort();
			int[] reversed = new int[result.Length];
			for (int i = 0; i < result.Length; i++)
			{
				reversed[i] = result[result.Length - i - 1];
			}
			this.result = reversed;
		}
	}

	/// <summary>
	/// Taken from http://tawani.blogspot.com/2009/02/topological-sorting-and-cyclic.html
	/// </summary>
	class TopologicalSorter
	{
		private int[] vertices;
		private int[,] matrix;
		private int numberOfVertices;
		private int[] sortedArray;

		public TopologicalSorter(int size)
		{
			vertices = new int[size];
			this.matrix = new int[size, size];
			this.numberOfVertices = 0;
			for (int i = 0; i < size; i++)
				for (int j = 0; j < size; j++)
					this.matrix[i, j] = 0;
			sortedArray = new int[size];
		}

		public int AddVertex(int vertex)
		{
			vertices[this.numberOfVertices++] = vertex;
			return this.numberOfVertices - 1;
		}

		public void AddEdge(int start, int end)
		{
			this.matrix[start, end] = 1;
		}

		public int[] Sort()
		{
			while (this.numberOfVertices > 0)
			{
				int currentVertex = noSuccessors();
				if (currentVertex == -1)
					throw new Exception("Graph has cycles");

				sortedArray[this.numberOfVertices - 1] = vertices[currentVertex];

				deleteVertex(currentVertex);
			}

			return sortedArray;
		}

		private int noSuccessors()
		{
			for (int row = 0; row < this.numberOfVertices; row++)
			{
				bool isEdge = false;
				for (int col = 0; col < this.numberOfVertices; col++)
				{
					if (this.matrix[row, col] > 0)
					{
						isEdge = true;
						break;
					}
				}
				if (!isEdge)
					return row;
			}
			return -1;
		}

		private void deleteVertex(int vertexToDelete)
		{
			if (vertexToDelete != this.numberOfVertices - 1)
			{
				for (int j = vertexToDelete; j < this.numberOfVertices - 1; j++)
					vertices[j] = vertices[j + 1];

				for (int row = vertexToDelete; row < this.numberOfVertices - 1; row++)
					moveRowUp(row, this.numberOfVertices);

				for (int col = vertexToDelete; col < this.numberOfVertices - 1; col++)
					moveColLeft(col, this.numberOfVertices - 1);
			}
			this.numberOfVertices--;
		}

		private void moveRowUp(int row, int length)
		{
			for (int col = 0; col < length; col++)
				this.matrix[row, col] = this.matrix[row + 1, col];
		}

		private void moveColLeft(int col, int length)
		{
			for (int row = 0; row < length; row++)
				this.matrix[row, col] = this.matrix[row, col + 1];
		}
	}

}
