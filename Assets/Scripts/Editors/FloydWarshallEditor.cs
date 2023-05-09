#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


public class FloydWarshallEditor : EditorWindow
{
    [MenuItem("BEN/Floyd-Warshall")]
    public static void ShowWindow()
    {
        GetWindow<FloydWarshallEditor>("Floyd-Warshall");
    }

    private void OnGUI()
    {
        // Add a button to run the FloydWarshall script
        if (GUILayout.Button("Generate Path Data"))
        {
            FloydWarshall.Compute(FindObjectOfType<WayPointContainer>().GetNodes());
        }
    }
}

#endif