#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BEN;


[CustomEditor(typeof(NPC))]
public class NPCEditor : Editor {

	private bool showPersonality = false;

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		Agent npc = ((NPC)target).agent;
		if (npc != null) {
			showPersonality = EditorGUILayout.Foldout(showPersonality, "NPC Personality");
			if (showPersonality) {
				string name = ((NPC)target).gameObject.name;
				if (GUILayout.Button("Load data "+name)) {
					((NPC)target).LoadAgentData(name);
				}
				EditorGUILayout.Space();
				if (npc != null) {
					npc.GetPersonality().openness = EditorGUILayout.Slider("Openness", npc.GetPersonality().openness, 0, 1);
					npc.GetPersonality().consciousness = EditorGUILayout.Slider("Consciousness", npc.GetPersonality().consciousness, 0, 1);
					npc.GetPersonality().extroversion = EditorGUILayout.Slider("Extroversion", npc.GetPersonality().extroversion, 0, 1);
					npc.GetPersonality().agreeableness = EditorGUILayout.Slider("Agreeableness", npc.GetPersonality().agreeableness, 0, 1);
					npc.GetPersonality().neurotism = EditorGUILayout.Slider("Neurotism", npc.GetPersonality().neurotism, 0, 1);
				}
			}
		} else {
			EditorGUILayout.TextArea("Agent couldn't be loaded");
		}

		if (GUILayout.Button("Test System")) {
			((NPC)target).Test();
		}
	}
}
#endif