#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AreaContainer))]
public class AreaContainerEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        AreaContainer areaContainer = (AreaContainer)target;

        if (GUILayout.Button("Locate and Organize Areas")) {
            areaContainer.areas = FindObjectsOfType<AreaTrigger>();
            System.Array.Sort(areaContainer.areas, (x, y) => x.area.CompareTo(y.area));
        }
    }
}
#endif