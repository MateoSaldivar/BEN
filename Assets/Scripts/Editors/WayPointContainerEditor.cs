#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;


[CustomEditor(typeof(WayPointContainer))]
public class WayPointContainerEditor : Editor {

	private WayPointContainer _target;

	private void OnEnable() {
		_target = (WayPointContainer)target;
	}

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();
		if (GUILayout.Button("Print PathData")) {
			PrintPathData();
		}
		if (GUILayout.Button("Set Node Indexes")) {
			SetNodeIndexes();
		}
		if (GUILayout.Button("Test Path data")) {

			FloydWarshall.LoadData();

			if (FloydWarshall.PathData == null) {
				UnityEngine.Debug.LogError("PathData is null, make sure you load the data before calling this method.");
				return;
			}
			WayPointContainer.instance = _target;
			int i = 0;
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			Node longestPathStart = null;
			Node longestPathEnd = null;

			int longestPathLength = 0;
			int totalPathLength = 0;
			for(int x = 0; x < 1; x++)
			foreach (Node n in _target.nodes) {
				foreach (Node m in _target.nodes) {
					if (n != m) {
						Queue<Node> path = new Queue<Node>(FloydWarshall.ConstructPath(n, m));
						int pathLength = path.Count;
						totalPathLength += pathLength;
						if (pathLength > longestPathLength) {
							longestPathLength = pathLength;
							longestPathStart = n;
							longestPathEnd = m;
						}
						if (path.Count == 0) {
							UnityEngine.Debug.LogError("Error found in path: from " + n.name + " to " + m.name + " achieved " + i + " paths");
							return;
						} else {
							i++;
						}
					}
				}
			}
			stopwatch.Stop();
			float averagePathLength = (float)totalPathLength / i;
			UnityEngine.Debug.Log("Path testing successful, tested: " + i + " paths");
			UnityEngine.Debug.Log("Total time: " + stopwatch.Elapsed.TotalSeconds + " seconds");
			UnityEngine.Debug.Log("Average path length: " + averagePathLength);
			UnityEngine.Debug.Log("Longest path: from " + longestPathStart.name + " to " + longestPathEnd.name + " with length " + longestPathLength + " nodes");
		}



	}
	private Vector2 scrollPos;
	private void PrintPathData() {

		if (FloydWarshall.PathData == null) {
			FloydWarshall.LoadData();
		}
		if (FloydWarshall.PathData == null) {
			UnityEngine.Debug.LogError("PathData is null, make sure you load the data before calling this method.");
			return;
		}
		UnityEngine.Debug.Log("");
		for (int i = 0; i < FloydWarshall.PathData.GetLength(0); i++) {
			string row = "";
			for (int j = 0; j < FloydWarshall.PathData.GetLength(1); j++) {
				row += FloydWarshall.PathData[i, j] + " ";
			}
			UnityEngine.Debug.Log("[" + row + "]");
		}
		UnityEngine.Debug.Log("");
	}

	private void SetNodeIndexes() {
		List<Node> newNodes = new List<Node>();
		for (int i = 0; i < _target.transform.childCount; i++) {
			Node node = _target.transform.GetChild(i).GetComponent<Node>();
			if (node != null) {
				node.gameObject.name = node.id;
				node.Index = i;
				newNodes.Add(node);
			}
		}
		_target.nodes = newNodes.ToArray();
	}
}




#endif
