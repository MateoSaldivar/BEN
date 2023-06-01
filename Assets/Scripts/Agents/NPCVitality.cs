using UnityEngine;
using AVA = AgentVariableAdjuster;
using GR = GlobalRegistry;

public class NPCVitality : MonoBehaviour {

    #region Variables
    public float hungerFill;
    public float thirstFill;
    public float energy;
    public float temperature;
    public float clothingHeatValueTemp;//Will come from inventory class
    NPC main;

    
    #endregion

    #region Unity Methods

    private void Awake() {
        main = GetComponent<NPC>();
        main.SetVitality(this);
    }

    void Start() {
        GR.TimeManager.OnHourChanged += OnHourChanged;
    }

    private void OnDestroy() {
        GR.TimeManager.OnHourChanged -= OnHourChanged;
    }

    void Update() {
        UpdateHunger();
        UpdateThirst();
        UpdateEnergy();
        UpdateTemperature(clothingHeatValueTemp);
	}

    private void UpdateThirst() {
        thirstFill -= main.actioner.excertion * AVA.thirstDecreaseRate * Time.deltaTime;
       
    }

    private void UpdateEnergy() {
        energy -= main.actioner.excertion * AVA.energyDecreaseRate * Time.deltaTime;
        if(energy < 0.1f && GR.TimeManager.GetTimeOfDay()== TimeOfDay.Night) {
            
		}
    }

    private void UpdateHunger() {
        hungerFill -= main.actioner.excertion * AVA.hungerDecreaseRate * Time.deltaTime;
    }

    private void UpdateTemperature(float clothingHeatValue) {
        float worldTemperature = GR.TimeManager.temperature;
        float adjustedTemperature = worldTemperature + clothingHeatValue;
        temperature =  adjustedTemperature;
    }

    private void OnHourChanged() {
   
        

    }
    #endregion
}
