using UnityEngine;
using AVA = AgentVariableAdjuster;
/*
* ZiroDev Copyright(c)
*
*/
public class NPCInventory : Inventory {

    #region Variables
    NPC main;

	#endregion

	#region Unity Methods
	private void Awake() {
		main = GetComponent<NPC>();
        main.SetInventory(this);
	}
	void Start() {
        
    }

    void Update() {
        
    }

    #endregion
}
