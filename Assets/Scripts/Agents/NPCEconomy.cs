using UnityEngine;
using AVA = AgentVariableAdjuster;
/*
* ZiroDev Copyright(c)
*
*/
public class NPCEconomy : MonoBehaviour {

	#region Variables
	NPC main;

	#endregion

	#region Unity Methods
	private void Awake() {
		main = GetComponent<NPC>();
		main.SetEconomy(this);
	}

	void Start() {
        
    }

    void Update() {
        
    }

    #endregion
}
