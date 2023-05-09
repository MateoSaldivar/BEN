using UnityEngine;
using AVA = AgentVariableAdjuster;
/*
* ZiroDev Copyright(c)
*
*/
public class NPCSocial : MonoBehaviour {

    #region Variables
    NPC main;

    #endregion

    #region Unity Methods

    private void Awake() {
        main = GetComponent<NPC>();
        main.SetSocial(this);
    }
    void Start() {
        
    }

    void Update() {
        
    }

    #endregion
}
