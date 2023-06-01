using UnityEngine;
using TMPro;

public class HUDTextUpdater : MonoBehaviour {

    #region Variables
    public TMP_Text timeText;
    public TMP_Text moneyText;

    public Animator timeIcon;

    private int lastValue = -1;
    #endregion

    #region Unity Methods
    
    void Start() {
        
    }

    void Update() {
        timeText.text = GlobalRegistry.TimeManager.GetTimeText();
        moneyText.text = GlobalRegistry.Player.GetMoney();

        int timeOfDay = (int)GlobalRegistry.TimeManager.GetTimeOfDay();
        if (lastValue != timeOfDay) {
            lastValue = timeOfDay;
            timeIcon.SetTrigger(GlobalRegistry.TimeManager.GetTimeOfDay().ToString());
        }

    }

    #endregion
}
