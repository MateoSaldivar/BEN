#if UNITY_EDITOR
//using BEN;

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

        List<FileAction> fileActions = new List<FileAction>();
        List<bool> ActionExpanded = new List<bool>();
        Vector2 scrollPos = Vector2.zero;


        // Editor window initialization
        [MenuItem("BEN/File Actions Editor")]
        public static void ShowWindow() {
            EditorWindow.GetWindow(typeof(ActionEditorWindow));
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
                    op = FileAction.EffectOp.TRUE
                };
            }
            GUILayout.EndHorizontal();

            List<int> effectsToRemove = new List<int>();

            List<string> effectNames = new List<string>(Enum.GetNames(typeof(FileAction.EffectOp)));
            for (int i = 0; i < fileAction.environmentalPreconditions.Length; i++) {
                GUILayout.BeginHorizontal();
                string key = EditorGUILayout.TextField(fileAction.environmentalPreconditions[i].key, GUILayout.Width(100));
                FileAction.EffectOp value = fileAction.environmentalPreconditions[i].op;
                GUILayout.Space(10);
                int selectedIndex = effectNames.IndexOf(value.ToString());
                selectedIndex = EditorGUILayout.Popup(selectedIndex, effectNames.ToArray(), GUILayout.Width(100));
                value = (FileAction.EffectOp)Enum.Parse(typeof(FileAction.EffectOp), effectNames[selectedIndex]);
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
                    op = FileAction.EffectOp.TRUE
                };
            }
            GUILayout.EndHorizontal();

            List<int> effectsToRemove = new List<int>();

            List<string> effectNames = new List<string>(Enum.GetNames(typeof(FileAction.EffectOp)));
            for (int i = 0; i < fileAction.preconditions.Length; i++) {
                GUILayout.BeginHorizontal();
                string key = EditorGUILayout.TextField(fileAction.preconditions[i].key, GUILayout.Width(100));
                FileAction.EffectOp value = fileAction.preconditions[i].op;
                GUILayout.Space(10);
                int selectedIndex = effectNames.IndexOf(value.ToString());
                selectedIndex = EditorGUILayout.Popup(selectedIndex, effectNames.ToArray(), GUILayout.Width(100));
                value = (FileAction.EffectOp)Enum.Parse(typeof(FileAction.EffectOp), effectNames[selectedIndex]);
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
                    op = FileAction.EffectOp.TRUE
                };
            }
            GUILayout.EndHorizontal();

            List<int> effectsToRemove = new List<int>();

            List<string> effectNames = new List<string>(Enum.GetNames(typeof(FileAction.EffectOp)));
            for (int i = 0; i < fileAction.effects.Length; i++) {
                GUILayout.BeginHorizontal();
                string key = EditorGUILayout.TextField(fileAction.effects[i].key, GUILayout.Width(100));
                FileAction.EffectOp value = fileAction.effects[i].op;
                GUILayout.Space(10);
                int selectedIndex = effectNames.IndexOf(value.ToString());
                selectedIndex = EditorGUILayout.Popup(selectedIndex, effectNames.ToArray(), GUILayout.Width(100));
                value = (FileAction.EffectOp)Enum.Parse(typeof(FileAction.EffectOp), effectNames[selectedIndex]);
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
            
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save", buttonStyle, GUILayout.Width(50))) {
                SaveJson();
            }

            GUILayout.Space(10);

            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();
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

    

    

}

#endif