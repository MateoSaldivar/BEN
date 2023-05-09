#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StaticDisableable))]
public class StaticDisableableEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        StaticDisableable item = (StaticDisableable)target;

        if (GUILayout.Button("Change Current State")) {
            item.SetUp();
			if (item.active) {
                item.active = false;
                item.DisableObject();

			} else {
                item.active = true;
                item.EnableObject();

            }
        }
    }
}
#endif