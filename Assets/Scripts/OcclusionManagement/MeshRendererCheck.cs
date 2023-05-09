using UnityEngine;
/*
* ZiroDev Copyright(c)
*
*/
public class MeshRendererCheck : MonoBehaviour {
    MeshRenderer rendererChecker;

    private void Start() {
         rendererChecker = GetComponent<MeshRenderer>();
    }
	void Update() {
        
        if (rendererChecker != null && !rendererChecker.enabled) {
            rendererChecker.enabled = true;
        }
    }
}
