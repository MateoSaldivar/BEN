using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class PlanParser : MonoBehaviour {
	public string[] Places;
	public string[] Names;
	public string[] Items;


	private string npcFolderPath = "Assets/NPCdata/";
	private string folderPath = "Assets/GameFiles";
	private string npcFileName = "NPCNames.txt";
	private string placesFileName = "Places.txt";
	private string npcFilePath;
	private string placesFilePath;

	public void Start() {
		// Set the file paths to the correct locations
		npcFilePath = Path.Combine(folderPath, npcFileName);
		placesFilePath = Path.Combine(folderPath, placesFileName);

	}

	public string[] GetNPCNames(bool ForceUpdate = false) {
		string[] npcNames;

		// Check if the file exists
		if (File.Exists(npcFilePath) && !ForceUpdate) {
			// Read the names from the file
			npcNames = File.ReadAllLines(npcFilePath);
		} else {
			// Get the names from the folders and write them to the file
			string[] folderNames = Directory.GetDirectories(npcFolderPath);
			npcNames = new string[folderNames.Length];

			for (int i = 0; i < folderNames.Length; i++) {
				npcNames[i] = Path.GetFileName(folderNames[i]);
			}

			// Create the directory if it doesn't exist
			string directoryPath = Path.GetDirectoryName(npcFilePath);
			if (!Directory.Exists(directoryPath)) {
				Directory.CreateDirectory(directoryPath);
			}

			File.WriteAllLines(npcFilePath, npcNames);
		}

		return npcNames;
	}


	public string[] GetPlaces(bool ForceUpdate = false) {
		string[] places;

		// Check if the file exists
		if (File.Exists(placesFilePath) && !ForceUpdate) {
			// Read the names from the file
			places = File.ReadAllLines(placesFilePath);
		} else {
			// Get the places from the nodes and write them to the file
			Node[] nodes = FindObjectOfType<WayPointContainer>().GetNodes();
			string[] placeNames = new string[nodes.Length];

			for (int i = 0; i < nodes.Length; i++) {
				placeNames[i] = nodes[i].id;
			}

			// Create the directory if it doesn't exist
			string directoryPath = Path.GetDirectoryName(placesFilePath);
			if (!Directory.Exists(directoryPath)) {
				Directory.CreateDirectory(directoryPath);
			}

			File.WriteAllLines(placesFilePath, placeNames);

			places = placeNames;
		}

		return places;
	}



	public string[] ParsePlanString(string planString) {
		string[] splitString = SplitString(planString);

		string action = splitString[0];
		string plo = splitString[1];
		string place = splitString.Length == 3 ? splitString[2] : "";

		Debug.Log("Action: " + action);
		Debug.Log("PLO: " + plo);
		Debug.Log("Place: " + place);

		return new string[] { action, plo, place };
	}

	private string[] SplitString(string input) {
		string[] splitString = new string[3];
		int lastSplit = 0;
		int splitIndex = 0;

		for (int i = 0; i < input.Length; i++) {
			if (Char.IsUpper(input[i])) {
				splitString[splitIndex] = input.Substring(lastSplit, i - lastSplit);
				lastSplit = i;
				splitIndex++;
			}
		}

		splitString[splitIndex] = input.Substring(lastSplit);

		for (int i = 0; i < splitString.Length; i++) {
			splitString[i] = splitString[i].ToLower();

			if (Places != null && Array.Exists(Places, element => element.ToLower() == splitString[i])) {
				splitString[i] = "place";
			} else if (Names != null && Array.Exists(Names, element => element.ToLower() == splitString[i])) {
				splitString[i] = "agent";
			} else if (Items != null && Array.Exists(Items, element => element.ToLower() == splitString[i])) {
				splitString[i] = "object";
			}
		}

		return splitString;
	}
}


#if UNITY_EDITOR

[CustomEditor(typeof(PlanParser))]
public class PlanParserEditor : Editor {
	private string planString = "";

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		PlanParser planParser = (PlanParser)target;

		GUILayout.Space(20);

		planString = EditorGUILayout.TextField("Plan String", planString);
		GUILayout.Space(10);
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Parse Plan String")) {
			planParser.Start();
			planParser.Names = planParser.GetNPCNames();
			planParser.Places = planParser.GetPlaces();
			string[] parsedPlan = planParser.ParsePlanString(planString);
			Debug.Log("Parsed Plan: " + string.Join(", ", parsedPlan));
		}

		if (GUILayout.Button("Create NPC Names File")) {
			planParser.Start();
			string[] npcNames = planParser.GetNPCNames(true);
			Debug.Log("NPC Names File Created: " + string.Join(", ", npcNames));
		}

		if (GUILayout.Button("Create Places File")) {
			planParser.Start();
			string[] places = planParser.GetPlaces(true);
			Debug.Log("Places File Created: " + string.Join(", ", places));
		}

		EditorGUILayout.EndHorizontal();
	}
}



#endif