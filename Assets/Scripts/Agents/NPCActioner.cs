using UnityEngine;
using AVA = AgentVariableAdjuster;
/*
* ZiroDev Copyright(c)
*
*/
public class NPCActioner : MonoBehaviour {

    #region Variables
    NPC main;
    public float excertion;
    #endregion

    #region Unity Methods

    private void Awake() {
        main = GetComponent<NPC>();
        main.SetActioner(this);
    }
    void Start() {
        
    }

    void Update() {
        
    }

    #endregion
}
