#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Node))]
public class NodeEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Node node = (Node)target;

        if (GUILayout.Button("Create New Node")) {
            GameObject newNode = new GameObject("New Node");
            newNode.AddComponent<Node>();
            newNode.transform.parent = node.transform.parent;

            newNode.transform.position = node.transform.position + new Vector3(1, 0, 0);

            Node newNodeScript = newNode.GetComponent<Node>();
            newNodeScript.Neighbours = new Node[] { node };

            Node[] newNeighbours = new Node[node.Neighbours.Length + 1];
            for (int i = 0; i < node.Neighbours.Length; i++) {
                newNeighbours[i] = node.Neighbours[i];
            }
            newNeighbours[node.Neighbours.Length] = newNodeScript;
            node.Neighbours = newNeighbours;
        }
    }
}
#endif