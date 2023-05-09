using UnityEngine;

public class AreaContainer : MonoBehaviour {

    #region Variables
    public static AreaContainer instance;
    public AreaTrigger[] areas;

	#endregion


	private void Start() {
		instance = this;
	}
}

