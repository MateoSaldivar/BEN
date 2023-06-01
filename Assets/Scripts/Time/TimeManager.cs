using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
public enum TimeOfDay {
	Dawn,
	Day,
	Dusk,
	Night
}

public enum Weather {
	Clear,
	Cloudy,
	Raining,
	Thunder
}

public enum DayOfWeek {
	Monday,
	Tuesday,
	Wednesday,
	Thursday,
	Friday,
	Saturday,
	Sunday,
	Always,
}

public static class TemperatureRange {
	public static float tooCold = 18.0f;
	public static float tooHot = 32.0f;
}

public static class TimeStamps {
	static public float dawnStart = 5f;
	static public float dawnEnd = 7f;
	static public float duskStart = 17f;
	static public float duskEnd = 19f;
}


public class TimeManager : MonoBehaviour {
	private static TimeManager instance;

	
	public event Action OnHourChanged;
	public float timeMultiplier = 1f;
	private float gameHour;
	private float gameTime;
	private int gameDay = 1; // Starting day is Monday 1
	[Range(0f, 10f)]
	public float timeScale = 1;
	public float startingHour = 0f; // Hour at which the game day starts
	public float temperature;
	public Weather currentWeather;
	private int previousHour;

	private void Update() {
		Time.timeScale = timeScale;
		gameTime += Time.deltaTime * Mathf.Max(timeMultiplier, 0);
		gameTime = gameTime % 86400f;
		gameDay = (int)((gameTime + startingHour) / 24f) + 1;
		gameHour = (gameTime + startingHour) % 24f;
		UpdateTemperature();

		int newHour = (int)gameHour;
		if (newHour != previousHour) {
			previousHour = newHour;
			OnHourChanged?.Invoke();
		}
	}



	public string GetTime() {
		int hour = (int)gameHour;
		string period = hour >= 12 ? "pm" : "am";
		hour = hour % 12;
		if (hour == 0 && (int)gameHour != 0) {
			hour = 12;
		}
		return hour.ToString("00") + ":" + Mathf.Floor(gameHour % 1 * 60).ToString("00") + " " + period;
	}

	public float GetGameTime() {
		return gameHour;
	}

	public int GetGameDay() {
		return gameDay;
	}

	public void SetTimeMultiplier(float multiplier) {
		timeMultiplier = multiplier;
	}

	private string GetDayOfWeek() {
		string[] daysOfWeek = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
		int dayIndex = (gameDay - 1) % 7;

		return daysOfWeek[dayIndex] + ". " + gameDay.ToString("00");
	}

	public string GetTimeText() {
		return GetTime() + " | " + GetDayOfWeek();
	}

	public TimeOfDay GetTimeOfDay() {
		if (gameHour >= TimeStamps.dawnStart && gameHour < TimeStamps.dawnEnd) {
			return TimeOfDay.Dawn;
		} else if (gameHour >= TimeStamps.dawnEnd && gameHour < TimeStamps.duskStart) {
			return TimeOfDay.Day;
		} else if (gameHour >= TimeStamps.duskStart && gameHour < TimeStamps.duskEnd) {
			return TimeOfDay.Dusk;
		} else {
			return TimeOfDay.Night;
		}
	}

	private int temperaturePreviousUpdateHour = -1; // Initialize to -1 to force initial temperature update
	private float dailyTemperatureModifier = 0;
	private void UpdateTemperature() {
		int currentHour = (int)gameHour;

		if (currentHour == temperaturePreviousUpdateHour) {
			return;
		}

		temperaturePreviousUpdateHour = currentHour;

		float baseTemperature = temperature;
		float temperatureRange = 10f; // Range of temperature variation throughout the day
		float temperatureChangePerHour = temperatureRange / 12f; // Temperature change per hour, assuming a 12-hour day

		// Determine the weather conditions
		bool isClear = currentWeather == Weather.Clear;
		bool isRaining = currentWeather == Weather.Raining;
		bool isThunder = currentWeather == Weather.Thunder;

		// Adjust temperature based on weather conditions
		if (isRaining || isThunder) {
			baseTemperature -= 2f; // If it's raining or thundering, the temperature is lower
		}

		// Adjust temperature based on time of day
		if (GetTimeOfDay() == TimeOfDay.Day) {
			if (isClear && gameHour > 10f && gameHour < 14f) {
				baseTemperature += temperatureChangePerHour * (gameHour - 10f) + dailyTemperatureModifier;
			} else {
				baseTemperature += temperatureChangePerHour * 2f + dailyTemperatureModifier;
			}
		} else if (GetTimeOfDay() == TimeOfDay.Night) {
			baseTemperature -= temperatureChangePerHour * 1.5f + dailyTemperatureModifier;
		}

		// Clamp temperature between 12 and 38 degrees
		baseTemperature = Mathf.Clamp(baseTemperature, 12f, 38f);

		// Update the daily temperature modifier at the start of a new day
		if (currentHour == 1f) {
			float noiseValue = Mathf.PerlinNoise(Time.time, 0f) * 2f - 1f;
			float noiseModifier = Mathf.Lerp(0.2f, 0.6f, (noiseValue + 1f) / 2f);


			dailyTemperatureModifier = noiseModifier;

			//SaveTemperatures();
			//print(string.Format("Noise Value: {0:F2}, Noise Modifier: {1:F2}", noiseValue, noiseModifier));
		}

		temperature = baseTemperature;
		//int hour = (int)gameHour;
		//int day = GetGameDay(); // Assumes that the game starts at day 1

		//string temperatureData = string.Format("{0},{1},{2:F2},{3:F2}", day, hour, temperature, dailyTemperatureModifier);
		//temperatureList.Add(temperatureData);
		//print(string.Format("Game Hour: {0:F1}, Temperature: {1}°C, Daily Modifier: {2:F1}, Weather: {3}", gameHour, temperature, dailyTemperatureModifier, currentWeather.ToString()));

	}
	List<string> temperatureList = new List<string>();
	private void SaveTemperatures() {
		string path = Application.dataPath + "/temperaturesDebug.txt";
		using (StreamWriter writer = new StreamWriter(path, true)) {
			foreach (string temperatureData in temperatureList) {
				writer.WriteLine(temperatureData);
			}
		}
		
	}


}

