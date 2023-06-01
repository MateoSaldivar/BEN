using System.Collections.Generic;
using UnityEngine;
using System;
using ST = Utils.SymbolTable;
using GR = GlobalRegistry;

public enum TaskCategory {
    None,
    Work,
    Study,
    Exercise,
    Errands,
    Social,
    Rest,
    Leisure,
    Meal,
    Sleep,
    Life
}

[Serializable]
public class S_Task {
    public float startTime;
    public float endTime;
    public DayOfWeek day;
    public TaskCategory category;
    public string action;
    public int actionID;
    public S_Task(float startTime, float endTime, DayOfWeek day, TaskCategory category, string action) {
        this.startTime = startTime;
        this.endTime = endTime;
        this.day = day;
        this.category = category;
        this.action = action;
        actionID = ST.GetID(action);
    }
}

[Serializable]
public class Schedule {
    public Dictionary<DayOfWeek, List<S_Task>> tasksByDay = new Dictionary<DayOfWeek, List<S_Task>>();
    //public string name;

    public Schedule(string name) {
        //this.name = name;
        for (int i = 0; i < 7; i++) {
            tasksByDay[(DayOfWeek)i] = new List<S_Task>();
        }
    }

    public void AddTask(float startTime, float endTime, DayOfWeek day, TaskCategory category, string action) {
        S_Task newTask = new S_Task(startTime, endTime, day, category, action);

        // If there isn't a container for this day of the week yet, create one
        if (!tasksByDay.ContainsKey(day)) {
            tasksByDay.Add(day, new List<S_Task>());
        }

        // Insert the task into the container for this day, ordered by starting time
        int index = 0;
        while (index < tasksByDay[day].Count && startTime >= tasksByDay[day][index].startTime) {
            index++;
        }
        tasksByDay[day].Insert(index, newTask);
    }

    public void UpdateTask(S_Task task, float startTime, float endTime, DayOfWeek day, TaskCategory category, string action) {
        RemoveTask(task);
        AddTask(startTime, endTime, day, category, action);
    }

    public void RemoveTask(S_Task task) {
        if (tasksByDay.ContainsKey(task.day)) {
            tasksByDay[task.day].Remove(task);
        }
    }

    public void UpdateTask(S_Task updatedTask) {
        // Find the task in the dictionary and replace it with the updated task
        if (tasksByDay.ContainsKey(updatedTask.day)) {
            for (int i = 0; i < tasksByDay[updatedTask.day].Count; i++) {
                if (tasksByDay[updatedTask.day][i].startTime == updatedTask.startTime &&
                    tasksByDay[updatedTask.day][i].endTime == updatedTask.endTime) {
                    tasksByDay[updatedTask.day][i] = updatedTask;
                    return;
                }
            }
        }
    }

    public void DeleteTask(float startTime, float endTime, DayOfWeek day) {
        // Find the task in the dictionary and remove it
        if (tasksByDay.ContainsKey(day)) {
            for (int i = 0; i < tasksByDay[day].Count; i++) {
                if (tasksByDay[day][i].startTime == startTime && tasksByDay[day][i].endTime == endTime) {
                    tasksByDay[day].RemoveAt(i);
                    return;
                }
            }
        }
    }

    public int GetCurrentTask() {
        DayOfWeek currentDay = (DayOfWeek)(((GR.TimeManager.GetGameDay() - 1) % 7));
        float currentTime = GR.TimeManager.GetGameTime();

        // Check the tasks for the current day, in order of starting time
        if (tasksByDay.ContainsKey(currentDay)) {
            foreach (S_Task task in tasksByDay[currentDay]) {
                if (currentTime >= task.startTime && currentTime < task.endTime) {
                    return task.actionID;
                }
            }
        }

        return 0;
    }

    public S_Task GetTask(int hour, DayOfWeek day) {
        if (tasksByDay.ContainsKey(day)) {
            foreach (S_Task task in tasksByDay[day]) {
                if (task.startTime <= hour && task.endTime > hour) {
                    return task;
                }
            }
        }
        return null;
    }

    public string GetTaskCountPerDay() {
        string result = "";
        for (int i = 0; i < 7; i++) {
            DayOfWeek day = (DayOfWeek)i;
            int count = tasksByDay.ContainsKey(day) ? tasksByDay[day].Count : 0;
            result += $"{day} : {count}\n";
        }
        return result;
    }

    public Dictionary<DayOfWeek, List<S_Task>> InitializeTasksByDay() {
        Dictionary<DayOfWeek, List<S_Task>> table = new Dictionary<DayOfWeek, List<S_Task>>();
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek))) {
            table[day] = new List<S_Task>();
        }
        return table;
    }

    public S_Task IsTaskTaken(float startTime, float endTime, DayOfWeek day) {
        if (!tasksByDay.ContainsKey(day)) {
            return null;
        }

        foreach (S_Task task in tasksByDay[day]) {
            if (task.startTime < endTime && task.endTime > startTime) {
                // There is a task scheduled during this interval
                return task;
            }
        }

        // There are no tasks scheduled during this interval
        return null;
    }

    
}
