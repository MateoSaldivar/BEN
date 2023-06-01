using UnityEngine;
using UnityEditor;
using System.Collections ;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GR = GlobalRegistry;

public class SunMover : MonoBehaviour {

	#region Variables
	public static SunMover instance;

	public float temperatureAtMidday = 4500f;
	public float temperatureAtDawnAndDusk = 1500;

    public Light sun;

    [ColorUsage(true, true)]
    public Color dayColor;
    [ColorUsage(true, true)]
    public Color nightColor;
    public Volume globalVolume;

    private Quaternion initialRotation;
    private float middayRotation = 0f;
    private float dayLength = 24f;
    private float transitionSpeed;


    #endregion

    #region Unity Methods

    private void Awake() {
		if (instance != null && instance != this) {
			Destroy(gameObject);
		} else {
			instance = this;
			DontDestroyOnLoad(gameObject);
		}
	}

	void Start() {
		initialRotation = sun.transform.rotation;
		SetRotationForTime(12f);
        transitionSpeed = CalculateTransitionSpeed();
	}



	void Update() {
        RotateSun();
        SetTemperatureOfLight();

    }

    void RotateSun() {
        float gameTime = GR.TimeManager.GetGameTime();
        float rotation = middayRotation + (gameTime / dayLength) * 360f;

        Vector3 globalZAxis = Vector3.forward;
        Quaternion globalZRotation = Quaternion.AngleAxis(rotation, globalZAxis);
        Quaternion localZRotation = Quaternion.Inverse(initialRotation) * globalZRotation * initialRotation;
        sun.transform.rotation = initialRotation * localZRotation;
    }

    private float transitionValue = 1;
    private float transitionTemperatureValue = 1;

    private Color currentVolumeColor;
    private float temperatureChangeSpeed = 2f;
    void SetTemperatureOfLight() {
        TimeOfDay timeOfDay = GR.TimeManager.GetTimeOfDay();

        switch (timeOfDay) {
            case TimeOfDay.Dusk:
                sun.intensity = Mathf.Lerp(0f, 3f, transitionValue);
                sun.colorTemperature = Mathf.Lerp(temperatureAtDawnAndDusk, temperatureAtMidday, transitionTemperatureValue);
                LerpVolumeColorFilter(nightColor, dayColor, transitionValue);
                if (transitionValue > 0) transitionValue -= transitionSpeed * Time.deltaTime;
                if (transitionTemperatureValue > 0) transitionTemperatureValue -= transitionSpeed * Time.deltaTime * temperatureChangeSpeed;
                break;
            case TimeOfDay.Day:
                if (currentVolumeColor != dayColor) SetVolumeColorFilter(dayColor);

                transitionValue = transitionTemperatureValue = 1;
                sun.intensity = 3f;
                sun.colorTemperature = temperatureAtMidday;
                break;
            case TimeOfDay.Dawn:
                sun.intensity = Mathf.Lerp(0f, 3f, transitionValue);
                sun.colorTemperature = Mathf.Lerp(temperatureAtDawnAndDusk, temperatureAtMidday, transitionValue * transitionTemperatureValue);
                LerpVolumeColorFilter(nightColor, dayColor, transitionValue);
                if (transitionValue < 1) transitionValue += transitionSpeed * Time.deltaTime;
                if (transitionTemperatureValue < 1) transitionTemperatureValue += transitionSpeed * Time.deltaTime * temperatureChangeSpeed;
                break;
            case TimeOfDay.Night:
                if (currentVolumeColor != nightColor) SetVolumeColorFilter(nightColor);

                transitionValue = transitionTemperatureValue = 0;
                sun.intensity = 0f;
                sun.colorTemperature = temperatureAtDawnAndDusk;
                break;
        }
    }

    private void LerpVolumeColorFilter(Color startColor, Color targetColor, float t) {
        
        if (globalVolume != null) {
            ColorAdjustments colorVolume;
            if (globalVolume.profile.TryGet(out colorVolume)) {
                currentVolumeColor = colorVolume.colorFilter.value = Color.Lerp(startColor, targetColor, t);
            }
        }
    }

    private void SetVolumeColorFilter(Color newColor) {
        if (globalVolume != null) {
            ColorAdjustments colorVolume;
            if (globalVolume.profile.TryGet(out colorVolume)) {
                currentVolumeColor = colorVolume.colorFilter.value = newColor;
            }
        }
    }

    float CalculateTransitionSpeed() {
        float desiredTimeInterval = TimeStamps.dawnEnd-TimeStamps.dawnStart;
        return 1 / (desiredTimeInterval / GR.TimeManager.timeMultiplier);
    }

    public void SetRotationForTime(float time) {
        middayRotation = -time * 360f / dayLength;
    }

    #endregion




}

