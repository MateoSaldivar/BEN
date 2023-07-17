#if UNITY_EDITOR
using GOBEN;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor {
    public class ActionEditorWindow : EditorWindow {

        public List<FileAction> fileActions = new List<FileAction>();
        List<bool> ActionExpanded = new List<bool>();
        Vector2 scrollPos = Vector2.zero;
        

        // Editor window initialization
        [MenuItem("BEN/Actions Editor")]
        public static void ShowWindow() {
            GetWindow(typeof(ActionEditorWindow));
        }

        void OnEnable() {
            // Load JSON file if it exists and add actions to the list
            string filePath = Application.dataPath + "/ActionData/ActionFile.json";
            if (File.Exists(filePath)) {
                string json = File.ReadAllText(filePath);
                SerializableFileActions serializableFileActions = JsonUtility.FromJson<SerializableFileActions>(json);
                if (serializableFileActions != null && serializableFileActions.actions != null) {
                    fileActions = serializableFileActions.actions.ToList();
                }
            } else {
                UnityEngine.Debug.Log("Action file not found at " + filePath);
            }

            // Set the ActionExpanded list to the same size as fileActions and set all values to false
            ActionExpanded = new List<bool>(new bool[fileActions.Count]);

            // If no actions were loaded, add a default action
            if (fileActions.Count == 0) {
                AddNewAction();
            }
        }

        void OnGUI() {

            // Draw the background for the file actions section
            Rect fileActionsRect = new Rect(10, 10, position.width - 20, (position.height * 0.9f) - 10);
            EditorGUI.DrawRect(fileActionsRect, new Color(0.3f, 0.3f, 0.3f));

            // Draw the list of file actions
            if (fileActions?.Count > 0) {
                GUILayout.BeginArea(fileActionsRect);
                DrawFileActions();
                GUILayout.EndArea();
            }

            // Draw the buttons
            Rect buttonsRect = new Rect(0, position.height * 0.9f, position.width, position.height * 0.1f);
            GUILayout.BeginArea(buttonsRect);
            DrawButtons();
            GUILayout.EndArea();

        }

        void DrawFileActions() {
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height * 0.9f - 10));

            for (int i = 0; i < fileActions.Count; i++) {
                GUILayout.BeginHorizontal();

                ActionExpanded[i] = EditorGUILayout.Foldout(ActionExpanded[i], fileActions[i].name);

                GUILayout.FlexibleSpace();

                GUILayout.Label("[" + fileActions[i].actionID + "]", GUILayout.Width(30));

                if (GUILayout.Button("-", GUILayout.Width(20))) {
                    fileActions.RemoveAt(i);
                    ActionExpanded.RemoveAt(i);

                    // Update the action IDs of the remaining actions
                    for (int j = i; j < fileActions.Count; j++) {
                        fileActions[j].actionID = j;
                    }

                    i--;
                }

                GUILayout.EndHorizontal();

                if (i >= 0 && ActionExpanded[i]) {
                    DrawActionData(fileActions[i]);
                }
            }

            GUILayout.EndScrollView();
        }

        void DrawActionData(FileAction fileAction) {
            GUILayout.BeginHorizontal();

            fileAction.name = EditorGUILayout.TextField("Name", fileAction.name);

            if (fileActions.IndexOf(fileAction) < fileActions.Count - 1 && GUILayout.Button("D", GUILayout.Width(20))) {
                // Move the file action down in the list
                int index = fileActions.IndexOf(fileAction);
                if (index < fileActions.Count - 1) {
                    fileActions.RemoveAt(index);
                    fileActions.Insert(index + 1, fileAction);

                    // Update the action IDs of the remaining actions
                    for (int i = 0; i < fileActions.Count; i++) {
                        fileActions[i].actionID = i;
                    }

                    // Update the expanded state of the actions
                    bool expanded = ActionExpanded[index];
                    ActionExpanded.RemoveAt(index);
                    ActionExpanded.Insert(index + 1, expanded);
                }
            }
          
            if (fileActions.IndexOf(fileAction) != 0 && GUILayout.Button("U", GUILayout.Width(20))) {
                // Move the file action up in the list
                int index = fileActions.IndexOf(fileAction);
                if (index > 0) {
                    fileActions.RemoveAt(index);
                    fileActions.Insert(index - 1, fileAction);

                    // Update the action IDs of the remaining actions
                    for (int i = 0; i < fileActions.Count; i++) {
                        fileActions[i].actionID = i;
                    }

                    // Update the expanded state of the actions
                    bool expanded = ActionExpanded[index];
                    ActionExpanded.RemoveAt(index);
                    ActionExpanded.Insert(index - 1, expanded);
                }
            }

            GUILayout.EndHorizontal();
            fileAction.utilityBelief = EditorGUILayout.TextField("Utility Belief", fileAction.utilityBelief);
            DrawEnvironmentalPreconditions(fileAction);
            DrawPreconditions(fileAction);
            DrawEffects(fileAction);
        }



        void DrawEnvironmentalPreconditions(FileAction fileAction) {
            // Draw the effects
            if (fileAction.environmentalPreconditions == null) {
                fileAction.environmentalPreconditions = new FileAction.WorldState[0];
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Environmental Preconditions");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(30))) {
                Array.Resize(ref fileAction.environmentalPreconditions, fileAction.environmentalPreconditions.Length + 1);
                fileAction.environmentalPreconditions[fileAction.environmentalPreconditions.Length - 1] = new FileAction.WorldState() {
                    key = "Name",
                    op = true
                };
            }
            GUILayout.EndHorizontal();

            List<int> effectsToRemove = new List<int>();

            for (int i = 0; i < fileAction.environmentalPreconditions.Length; i++) {
                GUILayout.BeginHorizontal();
                string key = EditorGUILayout.TextField(fileAction.environmentalPreconditions[i].key, GUILayout.Width(100));
                bool value = fileAction.environmentalPreconditions[i].op;
                GUILayout.Space(10);
                value = EditorGUILayout.Toggle(value, GUILayout.Width(100));
                fileAction.environmentalPreconditions[i].key = key;
                fileAction.environmentalPreconditions[i].op = value;
                if (GUILayout.Button("-", GUILayout.Width(20))) {
                    effectsToRemove.Add(i);
                }
                GUILayout.EndHorizontal();
            }

            // Remove the effects that were marked for removal
            foreach (int index in effectsToRemove) {
                List<FileAction.WorldState> tempList = new List<FileAction.WorldState>(fileAction.environmentalPreconditions);
                tempList.RemoveAt(index);
                fileAction.environmentalPreconditions = tempList.ToArray();
            }
        }

        void DrawPreconditions(FileAction fileAction) {
            // Draw the effects
            if (fileAction.preconditions == null) {
                fileAction.preconditions = new FileAction.WorldState[0];
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Preconditions");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(30))) {
                Array.Resize(ref fileAction.preconditions, fileAction.preconditions.Length + 1);
                fileAction.preconditions[fileAction.preconditions.Length - 1] = new FileAction.WorldState() {
                    key = "Name",
                    op = true
                };
            }
            GUILayout.EndHorizontal();

            List<int> effectsToRemove = new List<int>();

            for (int i = 0; i < fileAction.preconditions.Length; i++) {
                GUILayout.BeginHorizontal();
                string key = EditorGUILayout.TextField(fileAction.preconditions[i].key, GUILayout.Width(100));
                bool value = fileAction.preconditions[i].op;
                GUILayout.Space(10);
                int selectedIndex = value ? 0 : 1;
                value = EditorGUILayout.Toggle(value, GUILayout.Width(100));
                fileAction.preconditions[i].key = key;
                fileAction.preconditions[i].op = value;
                if (GUILayout.Button("-", GUILayout.Width(20))) {
                    effectsToRemove.Add(i);
                }
                GUILayout.EndHorizontal();
            }

            // Remove the effects that were marked for removal
            foreach (int index in effectsToRemove) {
                List<FileAction.WorldState> tempList = new List<FileAction.WorldState>(fileAction.preconditions);
                tempList.RemoveAt(index);
                fileAction.preconditions = tempList.ToArray();
            }
        }

        void DrawEffects(FileAction fileAction) {
            // Draw the effects
            if (fileAction.effects == null) {
                fileAction.effects = new FileAction.WorldState[0];
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Effects");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(30))) {
                Array.Resize(ref fileAction.effects, fileAction.effects.Length + 1);
                fileAction.effects[fileAction.effects.Length - 1] = new FileAction.WorldState() {
                    key = "Name",
                    op = true
                };
            }
            GUILayout.EndHorizontal();

            List<int> effectsToRemove = new List<int>();

            for (int i = 0; i < fileAction.effects.Length; i++) {
                GUILayout.BeginHorizontal();
                string key = EditorGUILayout.TextField(fileAction.effects[i].key, GUILayout.Width(100));
                bool value = fileAction.effects[i].op;
                GUILayout.Space(10);
                int selectedIndex = value ? 0 : 1;
                value = EditorGUILayout.Toggle(value, GUILayout.Width(100));
                fileAction.effects[i].key = key;
                fileAction.effects[i].op = value;
                if (GUILayout.Button("-", GUILayout.Width(20))) {
                    effectsToRemove.Add(i);
                }
                GUILayout.EndHorizontal();
            }

            // Remove the effects that were marked for removal
            foreach (int index in effectsToRemove) {
                List<FileAction.WorldState> tempList = new List<FileAction.WorldState>(fileAction.effects);
                tempList.RemoveAt(index);
                fileAction.effects = tempList.ToArray();
            }
        }


        void DrawButtons() {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 16;
            buttonStyle.fontStyle = FontStyle.Bold;

            GUILayout.BeginVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("+", buttonStyle, GUILayout.Width(50))) {
                // Add a new action
                AddNewAction();
            }

            GUILayout.Space(5); // Add space between buttons

            if (GUILayout.Button("Norm Editor", buttonStyle, GUILayout.Width(120))) {
                OpenNormEditorWindow();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(50))) {
                SaveJson();
            }

            GUILayout.Space(10);

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();
        }

        void OpenNormEditorWindow() {
            // Create and show the Norm editor window
            NormEditorWindow normEditorWindow = EditorWindow.GetWindow<NormEditorWindow>();
            normEditorWindow.Show();
        }


        void SaveJson() {
            CheckConnections();

            // Convert fileActions list to JSON string
            string json = JsonUtility.ToJson(new SerializableFileActions(fileActions), true);

            // Create the directory if it doesn't exist
            string directoryPath = Application.dataPath + "/ActionData";
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            // Write JSON string to file
            string filePath = directoryPath + "/ActionFile.json";
            File.WriteAllText(filePath, json);

            UnityEngine.Debug.Log("File saved to " + filePath);
        }
        void CheckConnections() {
            foreach (FileAction actionA in fileActions) {
                for (int i = 0; i < fileActions.Count; i++) {
                    if (i == actionA.actionID) {
                        continue; // Don't compare action with itself
                    }
                    FileAction actionB = fileActions[i];
                    foreach (FileAction.WorldState preconditionA in actionA.preconditions) {
                        foreach (FileAction.WorldState effectB in actionB.effects) {
                            if (preconditionA.key == effectB.key) {
                                if (actionA.connections == null) {
                                    actionA.connections = new int[0];
                                }
                                if (!actionA.connections.Contains(actionB.actionID)) {
                                    List<int> connectionsList = new List<int>(actionA.connections);
                                    connectionsList.Add(actionB.actionID);
                                    actionA.connections = connectionsList.ToArray();
                                }
                                foreach (FileAction.WorldState preconditionB in actionB.preconditions) {
                                    if (preconditionA.key == preconditionB.key) {
                                        if (!actionA.connections.Contains(actionB.actionID)) {
                                            List<int> connectionsList = new List<int>(actionA.connections);
                                            connectionsList.Add(actionB.actionID);
                                            actionA.connections = connectionsList.ToArray();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void AddNewAction() {
            FileAction newAction = new FileAction();
            newAction.name = "New Action";
            newAction.actionID = fileActions.Count;
            fileActions.Add(newAction);
            ActionExpanded.Add(false);
        }

    }


    public class NormEditorWindow : EditorWindow {
        private List<Norm> norms = new List<Norm>();
        private int selectedNormIndex = 0;

        // Variables for creating a new norm
        private string normName = "";
        private string intentionId = "";
        private string contextId = "";
        private bool contextEnabled = false;
        private float obedienceThreshold = 0f;
        private float priority = 0f;
        private int[] behaviorActions = new int[0];
        private float violationTime = 0f;

        private List<FileAction> fileActions = new List<FileAction>();

       

        void OnEnable() {
            LoadFileActions();
        }

        void LoadFileActions() {
            // Load file actions from your desired source
            // In this example, we assume the file actions are already loaded in the ActionEditorWindow
            ActionEditorWindow actionEditorWindow = EditorWindow.GetWindow<ActionEditorWindow>();
            if (actionEditorWindow != null) {
                fileActions = actionEditorWindow.fileActions;
            }
        }

        void OnGUI() {
            GUILayout.Label("Norm Editor", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            // Dropdown menu for selecting a norm
            EditorGUILayout.BeginHorizontal();
            selectedNormIndex = EditorGUILayout.Popup("Select Norm", selectedNormIndex, GetNormNames().ToArray());

            if (GUILayout.Button("-", GUILayout.Width(20))) {
                RemoveNorm(selectedNormIndex);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Norm details
            if (selectedNormIndex >= 0 && selectedNormIndex < norms.Count) {
                Norm selectedNorm = norms[selectedNormIndex];

                // Norm name
                selectedNorm.name = EditorGUILayout.TextField("Norm Name", selectedNorm.name);

                // Intention ID
                selectedNorm.intentionId = EditorGUILayout.TextField("Intention ID", selectedNorm.intentionId);

                // Context
                EditorGUILayout.BeginHorizontal();
                selectedNorm.contextId = EditorGUILayout.TextField("Context",selectedNorm.contextId);
                selectedNorm.contextEnabled = EditorGUILayout.Toggle(selectedNorm.contextEnabled);
                EditorGUILayout.EndHorizontal();
                // Obedience Threshold
                selectedNorm.obedienceThreshold = EditorGUILayout.FloatField("Obedience Threshold", selectedNorm.obedienceThreshold);

                // Priority
                selectedNorm.priority = EditorGUILayout.FloatField("Priority", selectedNorm.priority);
                selectedNorm.violationTime = EditorGUILayout.FloatField("Violation Time", selectedNorm.violationTime);

                // Behavior
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Behavior");
                if (GUILayout.Button("Add Action")) {
                    AddBehaviorAction(selectedNorm);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel++;
                if (selectedNormIndex >= 0 && selectedNormIndex < norms.Count) {
                    

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    if (selectedNorm!= null && selectedNorm.behavior != null) {
                        for (int i = 0; i < selectedNorm.behavior.Length; i++) {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Action " + (i + 1), GUILayout.Width(100));
                            selectedNorm.behavior[i] = EditorGUILayout.Popup(selectedNorm.behavior[i], GetFileActionNames());

                            if (GUILayout.Button("-", GUILayout.Width(20))) {
                                RemoveBehaviorAction(selectedNorm, i);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // Buttons for managing norms
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Norm")) {
                AddNorm();
            }
            if (GUILayout.Button("Save Norms")) {
                SaveNorms();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RemoveNorm(int index) {
            if (index >= 0 && index < norms.Count) {
                norms.RemoveAt(index);
                if (selectedNormIndex >= norms.Count) {
                    selectedNormIndex = norms.Count - 1;
                }
            }
        }

        private void AddBehaviorAction(Norm norm) {
            int length = norm.behavior != null ? norm.behavior.Length + 1 : 0;
            int[] newBehaviorActions = new int[length];
            if(norm.behavior != null)
            for (int i = 0; i < norm.behavior.Length; i++) {
                newBehaviorActions[i] = norm.behavior[i];
            }

            // Assign the first action from fileActions to the new element
            if (fileActions.Count > 0) {
                newBehaviorActions[newBehaviorActions.Length - 1] = 0; // Assuming the first action index is 0
            }

            norm.behavior = newBehaviorActions;
        }

        private void RemoveBehaviorAction(Norm norm, int index) {
            // Shift the remaining elements in the array to fill the removed index
            for (int i = index + 1; i < norm.behavior.Length; i++) {
                norm.behavior[i - 1] = norm.behavior[i];
            }

            // Resize the array by creating a new array with one fewer element
            Array.Resize(ref norm.behavior, norm.behavior.Length - 1);
        }

        private string[] GetFileActionNames() {
            string[] fileActionNames = new string[fileActions.Count];
            for (int i = 0; i < fileActions.Count; i++) {
                fileActionNames[i] = fileActions[i].name;
            }
            return fileActionNames;
        }

        private void AddNorm() {
            Norm newNorm = new Norm();
            norms.Add(newNorm);
            newNorm.name = "New Norm "+norms.Count;
            selectedNormIndex = norms.Count - 1;
        }

        private void SaveNorms() {
            // Convert norms list to JSON string
            string json = JsonUtility.ToJson(new SerializableFileNorms(norms), true);

            // Create the directory if it doesn't exist
            string directoryPath = Application.dataPath + "/NormData";
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            // Write JSON string to file
            string filePath = directoryPath + "/NormFile.json";
            File.WriteAllText(filePath, json);

            UnityEngine.Debug.Log("Norms saved to " + filePath);
        }

        private string[] GetNormNames() {
            string[] normNames = new string[norms.Count];
            for (int i = 0; i < norms.Count; i++) {
                normNames[i] = norms[i].name;
            }
            return normNames;
        }
    }





}

#endif