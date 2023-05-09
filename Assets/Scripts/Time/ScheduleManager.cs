using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class ScheduleManager : MonoBehaviour {
    private static ScheduleManager instance;

    public static ScheduleManager Instance {
        get {
            if (instance == null) {
                instance = FindObjectOfType<ScheduleManager>();
                if (instance == null) {
                    instance = new GameObject("ScheduleManager").AddComponent<ScheduleManager>();
                }
            }
            return instance;
        }
    }

    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public static void SaveSchedule(string agentName, Schedule schedule) {

        string filePath = GetFilePath(agentName);

        string directoryPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directoryPath)) {
            Directory.CreateDirectory(directoryPath);
        }

        string json = JsonConvert.SerializeObject(schedule, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static Schedule LoadSchedule(string agentName) {
        string filePath = GetFilePath(agentName);

        if (File.Exists(filePath)) {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Schedule>(json);
        }

        return null;
    }

    private static string GetFilePath(string agentName) {
        string directoryPath = "Assets/NPCdata/" + agentName+"/"+ agentName+ "_Schedule.json";

        return directoryPath;
    }
}
