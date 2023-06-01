#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NPCMovement))]
public class NPCMovementEditor : Editor {
    private string nodeId;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        NPCMovement movement = (NPCMovement)target;

        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        nodeId = EditorGUILayout.TextField("Node ID", nodeId);
        if (GUILayout.Button("Get Path")) {
            movement.path.Clear();
            movement.GetPath(Utils.SymbolTable.GetID(nodeId));
            nodeId = "";
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif