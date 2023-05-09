#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Newtonsoft.Json;


public class ScheduleEditorWindow : EditorWindow {

	[MenuItem("BEN/Schedule Editor")]
	public static void ShowWindow() {
		GetWindow<ScheduleEditorWindow>("Schedule Editor");
	}
	private const int NUM_DAYS = 7;
	private const int NUM_HOURS = 24;

	private Schedule schedule;
	private S_Task selectedTask;
	private bool creatingTask;
	private float taskStartTime;
	private float taskEndTime;
	private DayOfWeek taskDay;
	private TaskCategory taskCategory;
	private string taskAction;
	private string agentName = "";
	private void OnGUI() {
		if (schedule == null) {
			GUILayout.Label("No schedule loaded.");
			if (GUILayout.Button("Load Schedule")) {
				Load();
			}
		} else {
			GUILayout.Label("Schedule for " + agentName);

			// Draw the timetable
			DrawTimetable();

			// Draw the legend
			DrawLegend();

			// Draw the create task button
			if (GUILayout.Button("Create Task")) {
				creatingTask = true;
				taskStartTime = 0f;
				taskEndTime = 1f;
				taskDay = DayOfWeek.Monday;
				taskCategory = TaskCategory.None;
				taskAction = "";
			}

			// Draw the selected task (if any)
			if (selectedTask != null) {
				GUILayout.Space(10f);
				GUILayout.Label("Selected Task:");
				GUILayout.Label("Start Time: " + selectedTask.startTime);
				GUILayout.Label("End Time: " + selectedTask.endTime);
				GUILayout.Label("Day: " + selectedTask.day);
				GUILayout.Label("Category: " + selectedTask.category);
				GUILayout.Label("Action: " + selectedTask.action);

				// Draw the edit and delete buttons
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Edit")) {
					creatingTask = false;
					taskStartTime = selectedTask.startTime;
					taskEndTime = selectedTask.endTime;
					taskDay = selectedTask.day;
					taskCategory = selectedTask.category;
					taskAction = selectedTask.action;
				}
				if (GUILayout.Button("Delete")) {
					schedule.DeleteTask(selectedTask.startTime, selectedTask.endTime, selectedTask.day);
					selectedTask = null;
				}
				GUILayout.EndHorizontal();
			}

			// Draw the save button
			if (GUILayout.Button("Save Schedule")) {
				ScheduleManager.SaveSchedule(agentName, schedule);
			}
			if (GUILayout.Button("Load Schedule")) {
				Load();
			}
		}
	}

	private void Load() {
		string filePath = EditorUtility.OpenFilePanel("Load Schedule", "Assets/NPCdata", "json");
		if (!string.IsNullOrEmpty(filePath)) {
			string json = File.ReadAllText(filePath);
			schedule = JsonConvert.DeserializeObject<Schedule>(json);

			string fileName = Path.GetFileNameWithoutExtension(filePath);
			string[] nameParts = fileName.Split('_');

			if (nameParts.Length > 0) {
				agentName = nameParts[0];
			}
		}
	}

	float widthAlignment;
	float spaceheader;
	private void DrawTimetable() {
		DrawColumnHeaders();
		DrawTimeTableGrid();
		HandleTaskCreation();
	}

	private void DrawColumnHeaders() {
		widthAlignment = 66;
		spaceheader = 48;
		GUILayout.Space(10f);
		GUILayout.BeginHorizontal(GUILayout.Width(widthAlignment * NUM_DAYS));
		GUILayout.BeginHorizontal();
		GUILayout.Space(spaceheader);

		GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
		labelStyle.fontSize = 10;
		labelStyle.alignment = TextAnchor.MiddleCenter;

		for (int i = 0; i < NUM_DAYS; i++) {
			GUILayout.Label(((DayOfWeek)i).ToString(), labelStyle, GUILayout.ExpandWidth(true), GUILayout.Width(widthAlignment));
		}

		GUILayout.EndHorizontal();
		GUILayout.EndHorizontal();
	}

	private void DrawTimeTableGrid() {
		for (int j = 0; j < NUM_HOURS; j++) {
			GUILayout.BeginHorizontal(GUILayout.Width(70f));
			GUILayout.Label(j.ToString("D2") + ":00", GUILayout.Width(38f));
			for (int i = 0; i < NUM_DAYS; i++) {
				Rect rect = GUILayoutUtility.GetRect(70f, 20f);
				bool hasTask = false;


				// Check if there is a task scheduled at this time
				if (schedule.tasksByDay != null && schedule.tasksByDay.ContainsKey((DayOfWeek)i)) {
					foreach (S_Task task in schedule.tasksByDay[(DayOfWeek)i]) {
						if (task.startTime <= j && task.endTime > j) {
							EditorGUI.DrawRect(rect, GetTaskColor(task.category));
							hasTask = true;
							break;
						}
					}
				}

				// Draw the background color
				if (!hasTask) {
					EditorGUI.DrawRect(rect, new Color(0.8f, 0.8f, 0.8f));
					EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), Color.white);
				}

				// Handle clicking on the timetable cell
				if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
					selectedTask = null;
					foreach (S_Task task in schedule.tasksByDay[(DayOfWeek)i]) {
						if (task.startTime <= j && task.endTime > j) {
							selectedTask = task;
							break;
						}
					}
					Repaint();
				}
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.Space(10f);
	}

	private void HandleTaskCreation() {
		if (creatingTask) {
			GUILayout.Label("Create Task:");
			taskStartTime = EditorGUILayout.FloatField("Start Time:", taskStartTime);
			taskEndTime = EditorGUILayout.FloatField("End Time:", taskEndTime);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Day:");
			taskDay = (DayOfWeek)EditorGUILayout.EnumPopup(taskDay);
			if (taskDay == DayOfWeek.Always) {
				GUILayout.Label("(Every Day)");
			}
			EditorGUILayout.EndHorizontal();

			taskCategory = (TaskCategory)EditorGUILayout.EnumPopup("Category:", taskCategory);
			taskAction = EditorGUILayout.TextField("Action:", taskAction);

			if (GUILayout.Button("Add Task")) {
				// Check if there are any overlapping tasks
				bool overlap = false;
				S_Task overlappingTask = null;
				if (taskDay == DayOfWeek.Always) {
					// If the day is "Always", check for overlapping tasks on every day
					for (int i = 0; i < NUM_DAYS; i++) {
						DayOfWeek dayOfWeek = (DayOfWeek)i;
						if (dayOfWeek != DayOfWeek.Always) {
							S_Task task = schedule.IsTaskTaken(taskStartTime, taskEndTime, dayOfWeek);
							if (task != null) {
								overlap = true;
								overlappingTask = task;
								break;
							}
						}
					}
				} else {
					// Check for overlapping tasks on the specified day
					overlappingTask = schedule.IsTaskTaken(taskStartTime, taskEndTime, taskDay);
					overlap = (overlappingTask != null);
				}

				// If there is an overlapping task, show a message box asking the user what to do
				if (overlap) {
					bool removeOverlappingTask = EditorUtility.DisplayDialog("Overlap Detected",
						"The task you are trying to add overlaps with another task:\n" + overlappingTask.action +
						"\n\nWhat would you like to do?", "Remove Overlapping Task", "Cancel Adding Task");
					if (removeOverlappingTask) {
						// If the overlapping task is on the specified day, remove it
						if (overlappingTask.day == taskDay) {
							schedule.RemoveTask(overlappingTask);
							Repaint(); // Update the editor window
						} else {
							// If the day is "Always", remove the overlapping task from all days
							if (taskDay == DayOfWeek.Always) {
								for (int i = 0; i < NUM_DAYS; i++) {
									DayOfWeek dayOfWeek = (DayOfWeek)i;
									if (dayOfWeek != DayOfWeek.Always) {
										S_Task task = schedule.IsTaskTaken(taskStartTime, taskEndTime, dayOfWeek);
										if (task != null && task == overlappingTask) {
											schedule.RemoveTask(overlappingTask);
											break;
										}
									}
								}
							}
							// If the day is not "Always", show a message box asking the user what to do
							else {
								bool removeOverlappingTaskFromAllDays = EditorUtility.DisplayDialog("Remove from All Days?",
									"The task you are trying to add overlaps with a task on a different day. " +
									"Would you like to remove the overlapping task from all days?", "Yes", "No");
								if (removeOverlappingTaskFromAllDays) {
									// Remove the overlapping task from all days
									for (int i = 0; i < NUM_DAYS; i++) {
										DayOfWeek dayOfWeek = (DayOfWeek)i;
										if (dayOfWeek != DayOfWeek.Always) {
											S_Task task = schedule.IsTaskTaken(overlappingTask.startTime, overlappingTask.endTime, dayOfWeek);
											if (task != null && task == overlappingTask) {
												schedule.RemoveTask(task);
											}
										}
									}
									Repaint(); // Update the editor window
								} else {
									return; // Don't add the new task
								}
							}
						}
					} else {
						return; // Don't add the new task
					}
				}

				// If there are no overlapping tasks, add the new task
				if (taskDay == DayOfWeek.Always) {
					for (int i = 0; i < NUM_DAYS; i++) {
						DayOfWeek dayOfWeek = (DayOfWeek)i;
						if (dayOfWeek != DayOfWeek.Always) {
							schedule.AddTask(taskStartTime, taskEndTime, dayOfWeek, taskCategory, taskAction);
						}
					}
				} else {
					schedule.AddTask(taskStartTime, taskEndTime, taskDay, taskCategory, taskAction);
				}

				Debug.Log(schedule.GetTaskCountPerDay());
				creatingTask = false;
			}
		}
	}

	

	private bool legendFoldout = true;
	private Vector2 legendScrollPos = Vector2.zero;
	private float legendRowHeight = 15f;

	private void DrawLegend() {

		legendFoldout = EditorGUILayout.Foldout(legendFoldout, "Legend:");
		if (legendFoldout) {
			int numCategories = Enum.GetValues(typeof(TaskCategory)).Length;
			float legendWidth = 80f + numCategories * 30f;

			legendScrollPos = GUILayout.BeginScrollView(legendScrollPos, GUILayout.MaxHeight(4 * legendRowHeight));
			GUILayout.BeginHorizontal(GUILayout.Width(legendWidth));

			foreach (TaskCategory category in Enum.GetValues(typeof(TaskCategory))) {
				GUILayout.BeginVertical(GUILayout.Width(30f));
				EditorGUI.DrawRect(GUILayoutUtility.GetRect(10f, 10f), GetTaskColor(category));
				GUILayout.Label(category.ToString(), GUILayout.Width(80f));
				GUILayout.EndVertical();
			}

			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		}

	}



	private Color GetTaskColor(TaskCategory category) {
		switch (category) {
			case TaskCategory.Work:
				return new Color(1f, 0.5f, 0.5f);
			case TaskCategory.Study:
				return new Color(0.5f, 1f, 0.5f);
			case TaskCategory.Exercise:
				return new Color(0.5f, 0.5f, 1f);
			case TaskCategory.Errands:
				return new Color(1f, 1f, 0.5f);
			case TaskCategory.Social:
				return new Color(1f, 0.5f, 1f);
			case TaskCategory.Rest:
				return new Color(0.5f, 1f, 1f);
			case TaskCategory.Leisure:
				return new Color(1f, 0.75f, 0.5f);
			case TaskCategory.Meal:
				return new Color(0.75f, 1f, 0.5f);
			case TaskCategory.Sleep:
				return new Color(0.75f, 0.5f, 1f);
			case TaskCategory.Life:
				return new Color(0.5f, 0.75f, 1f);
			default:
				return Color.gray;
		}
	}
}
#endif