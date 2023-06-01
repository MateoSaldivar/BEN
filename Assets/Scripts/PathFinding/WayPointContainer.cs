using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Utils;
using ST = Utils.SymbolTable;
using BEN;
/*
* ZiroDev Copyright(c)
*
*/
public class WayPointContainer : MonoBehaviour {
	public bool ShowGraph = true;
	public static Dictionary<int,Node> IdReferencer = new Dictionary<int,Node>();
	public Node[] nodes;




	private void Start() {
		nodes = GetComponentsInChildren<Node>();
		foreach(Node n in nodes) {
			n.id_num = SymbolTable.GetID(n.id);
			IdReferencer.Add(n.id_num, n);
		}

		FloydWarshall.LoadData();
	}

	public Node[] GetNodes() {
		SetNodeIndexes();
		return GetComponentsInChildren<Node>();
	}

	public Node GetClosestNode(NPCMovement a) {
		Node closestNode = null;
		float closestDistance = float.MaxValue;
		foreach (Node node in nodes) {
			float distance = SqrDistance(VCU.VectorXZ(a.transform.position), node.position);
			if (distance < closestDistance) {
				closestNode = node;
				closestDistance = distance;
			}
		}
		return closestNode;
	}

	public float SqrDistance(Vector2 point1, Vector2 point2) {
		float xDiff = point1.x - point2.x;
		float yDiff = point1.y - point2.y;
		return xDiff * xDiff + yDiff * yDiff;
	}

	private void OnDrawGizmos() {
		if (ShowGraph) {
			if (nodes.Length > 0) {
				Gizmos.color = Color.yellow;
				foreach (Node point in GetComponentsInChildren<Node>()) {
					Gizmos.DrawSphere(point.transform.position, 0.5f);
					Gizmos.DrawWireSphere(point.transform.position, point.radius);

#if UNITY_EDITOR
					Handles.Label(point.transform.position + Vector3.up * 2, point.id);
#endif
				}
			}
		}
	}

	public void SetNodeIndexes() {
		for (int i = 0; i < transform.childCount; i++) {
			Node node = transform.GetChild(i).GetComponent<Node>();
			if (node != null) {
				node.Index = i;
			}
		}
	}
}


public static class VCU {
	public static Vector2 VectorXZ(Vector3 position) {
		return new Vector2(position.x, position.z);
	}
}