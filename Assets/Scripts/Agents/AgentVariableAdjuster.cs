using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.IO;


public static class AgentVariableAdjuster {

	public static float hungerDecreaseRate = 1f;
	public static float thirstDecreaseRate = 1f;
	public static float energyDecreaseRate = 1f;

	static AgentVariableAdjuster() {
		LoadVariablesFromFile();
	}

	public static void LoadVariablesFromFile() {
		var fields = typeof(AgentVariableAdjuster).GetFields(BindingFlags.Public | BindingFlags.Static);
		var numFields = fields.Length;

		var filePath = Application.dataPath + "/GameFiles/AgentVariables.txt";
		if (File.Exists(filePath)) {
			var lines = File.ReadAllLines(filePath);
			for (int i = 0; i < lines.Length && i < numFields; i++) {
				float.TryParse(lines[i], out float value);
				fields[i].SetValue(null, value);
			}
		}
	}

	public static void SaveVariablesToFile() {
		var fields = typeof(AgentVariableAdjuster).GetFields(BindingFlags.Public | BindingFlags.Static);
		var filePath = Application.dataPath + "/GameFiles/AgentVariables.txt";
		using (var writer = new StreamWriter(filePath)) {
			foreach (var field in fields) {
				if (field.FieldType == typeof(float)) {
					var value = (float)field.GetValue(null);
					writer.WriteLine(value);
				}
			}
		}
	}
}


#if UNITY_EDITOR
public class AgentVariableAdjusterEditor : EditorWindow {
	private Type agentVariableAdjusterType;
	private FieldInfo[] staticFields;

	[MenuItem("BEN/Agent Variable Adjuster")]
	public static void OpenWindow() {
		GetWindow<AgentVariableAdjusterEditor>("Agent Variable Adjuster");
	}

	private void OnEnable() {
		agentVariableAdjusterType = typeof(AgentVariableAdjuster);
		staticFields = agentVariableAdjusterType.GetFields(BindingFlags.Public | BindingFlags.Static);
	}

	private void OnGUI() {
		foreach (var field in staticFields) {
			var fieldType = field.FieldType;

			// Ignore non-float fields
			if (fieldType != typeof(float))
				continue;

			// Get the field value and name
			var fieldName = ObjectNames.NicifyVariableName(field.Name);
			var fieldValue = (float)field.GetValue(null);

			// Draw a label and a slider for the field
			//EditorGUILayout.LabelField(fieldName);
			var newValue = EditorGUILayout.Slider(fieldName,fieldValue, -1f, 10f);

			// Update the field value if it has changed
			if (newValue != fieldValue) {
				field.SetValue(null, newValue);
				EditorPrefs.SetFloat(fieldName, newValue);
			}
		}

		if (GUILayout.Button("Inject Values")) {
			AgentVariableAdjuster.SaveVariablesToFile();
		}
		if (GUILayout.Button("Load Values")) {
			AgentVariableAdjuster.LoadVariablesFromFile();
		}
	}


}
#endif