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
        timeText.text = TimeManager.Instance.GetTimeText();
        moneyText.text = Player.instance.GetMoney();

        int timeOfDay = (int)TimeManager.Instance.GetTimeOfDay();
        if (lastValue != timeOfDay) {
            lastValue = timeOfDay;
            timeIcon.SetTrigger(TimeManager.Instance.GetTimeOfDay().ToString());
        }

    }

    #endregion
}
