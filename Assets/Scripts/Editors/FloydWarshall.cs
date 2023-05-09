using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
/*
* ZiroDev Copyright(c)
*
*/
public class FloydWarshall : MonoBehaviour {
	//public static List<Node> nodes;
	private int[,] next;
	public static int[,] PathData;

	public static void LoadData() {
		Load("PathDataFile");
	}
	public static void Compute(Node[] nodes) {
		try {
			int n = nodes.Length;
			int[,] neighbours = new int[n, n];

			int[,] distance = new int[n, n];
			int[,] nextHop = new int[n, n];

			for (int i = 0; i < n; i++) {
				for (int j = 0; j < n; j++) {
					if (i == j) {
						neighbours[i, j] = 0;
					} else {
						neighbours[i, j] = int.MaxValue;
					}
				}
			}
			
			for (int i = 0; i < nodes.Length; i++) {
				for (int j = 0; j < nodes[i].Neighbours.Length; j++) {
					neighbours[i, Array.IndexOf(nodes, nodes[i].Neighbours[j])] = 1;
				}
			}
			
			for (int i = 0; i < n; i++) {
				for (int j = 0; j < n; j++) {
					
					distance[i, j] = neighbours[i, j];

					if (neighbours[i, j] == int.MaxValue) {
						nextHop[i, j] = -1;
					} else {
						nextHop[i, j] = j;
					}
				}
			}
			
			for (int k = 0; k < n; k++) {
				for (int i = 0; i < n; i++) {
					for (int j = 0; j < n; j++) {

						if (distance[i, k] == int.MaxValue || distance[k, j] == int.MaxValue) {
							continue;
						}

						if (distance[i, j] > distance[i, k] + distance[k, j]) {
							distance[i, j] = distance[i, k] + distance[k, j];
							nextHop[i, j] = nextHop[i, k];
						}
					}
				}
			}

			Save(nextHop, "PathDataFile");
		} catch (System.Exception e) {
			Debug.LogError("An error occurred while computing the shortest paths: " + e.Message);
		}
	}


	public static void Save(int[,] nextHop, string fileName) {
		try {
			var watch = System.Diagnostics.Stopwatch.StartNew();

			FileStream file = File.Create(Application.dataPath + "/PathData/" + fileName);
			BinaryWriter writer = new BinaryWriter(file);

			int numNodes = nextHop.GetLength(0);

			writer.Write(nextHop.GetLength(0));
			writer.Write(nextHop.GetLength(1));

			for (int i = 0; i < nextHop.GetLength(0); i++) {
				for (int j = 0; j < nextHop.GetLength(1); j++) {
					writer.Write(nextHop[i, j]);
				}
			}
			long dataSize = file.Length;
			writer.Close();
			file.Close();
			watch.Stop();

			var elapsedTime = watch.Elapsed;
			float dataSizeKb = dataSize / 1024f;

			
			Debug.Log("Saved data for " + numNodes + " nodes in " + dataSizeKb + "KB in " + elapsedTime.TotalSeconds + " seconds");

		} catch (IOException e) {
			Debug.LogError("An error occurred while saving the data: " + e.Message);
		}
	}

	

	public static void Load(string fileName) {
		try {
			string path = Application.dataPath + "/PathData/" + fileName;
			FileStream file = File.OpenRead(path);
			BinaryReader reader = new BinaryReader(file);

			int rows = reader.ReadInt32();
			int cols = reader.ReadInt32();

			PathData = new int[rows, cols];
			for (int i = 0; i < rows; i++) {
				for (int j = 0; j < cols; j++) {
					PathData[i, j] = reader.ReadInt32();
				}
			}
			print("Loaded data for "+fileName+", the file has a length of "+PathData.Length);
			//PrintPathData();
			reader.Close();
			file.Close();
		} catch (IOException e) {
			Debug.LogError("An error occurred while loading the data: " + e.Message);
		}
	}

	public static void PrintPathData(int[,] input) {
		
		for (int i = 0; i < input.GetLength(0); i++) {
			for (int j = 0; j < input.GetLength(1); j++) {
				Debug.Log("[" + i + "," + j + "]: " + input[i, j]);
			}
		}
	}

	public static Queue<Node> ConstructPath(Node currentNode, Node targetNode) {

		int currentIndex = Array.IndexOf(WayPointContainer.instance.nodes, currentNode);
		int targetIndex = Array.IndexOf(WayPointContainer.instance.nodes, targetNode);

		if (PathData[currentIndex, targetIndex] == -1) return null;
		

		if (PathData == null) {
			Debug.LogError("PathData is null, make sure you load the data before calling this method.");
			return null;
		}
		// Storing the path in a vector
		List<int> path = new List<int>();
		path.Add(currentIndex);
		//string str = "";
		while (currentIndex != targetIndex) {
			currentIndex = PathData[currentIndex, targetIndex];
			path.Add(currentIndex);
			//str += "["+currentIndex+"]";
		}
		//print(str);
		return CreateQueueFromList(path);
	}

	static Queue<Node> CreateQueueFromList(List<int> indices) {
		Queue<Node> queue = new Queue<Node>();
		foreach (int index in indices) {
			queue.Enqueue(WayPointContainer.instance.nodes[index]);
		}
		return queue;
	}

}

