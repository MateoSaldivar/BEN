using UnityEngine;
using GR = GlobalRegistry;
/*
* ZiroDev Copyright(c)
*
*/
public class AreaTrigger : MonoBehaviour {
	
	public int area;
	public AreaTrigger[] neighbours;
	public StaticDisableable[] objects;

	private void Start() {
		objects = GetComponentsInChildren<StaticDisableable>();
	}
	private void OnTriggerEnter(Collider other) {
		
		if (other.CompareTag("Player")) {
			GR.Player.currentArea = area;
			GR.Player.prevArea = 0;
		}
		if (other.CompareTag("NPC")) {
			other.GetComponent<DynamicDisableable>().currentArea = area;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.CompareTag("Player")) {
			GR.Player.currentArea = 0;
			GR.Player.prevArea = area;
		}
	}
}
