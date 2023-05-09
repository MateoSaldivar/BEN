using UnityEngine;
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
			Player.instance.currentArea = area;
			Player.instance.prevArea = 0;
		}
		if (other.CompareTag("NPC")) {
			other.GetComponent<DynamicDisableable>().currentArea = area;
		}
	}

	private void OnTriggerExit(Collider other) {
		if (other.CompareTag("Player")) {
			Player.instance.currentArea = 0;
			Player.instance.prevArea = area;
		}
	}
}
