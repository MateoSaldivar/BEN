using UnityEngine;

public class DynamicDisableable : MonoBehaviour {
    public int currentArea;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public bool visible = true;
	private void Update() {
        if (ObjectManager.IsCurrentAreaOrNeighbor(currentArea)) {
            float distanceX = Mathf.Abs(transform.position.x - GlobalRegistry.Player.transform.position.x);
            if (distanceX <= ObjectManager.instance.xDistance) {
                EnableObject();
            } else {
                DisableObject();
            }
        }
    }
	public void DisableObject() {
        visible = false;
        animator.enabled = false;
        spriteRenderer.enabled = false;
    }

    public void EnableObject() {
        visible = true;
        animator.enabled = true;
        spriteRenderer.enabled = true;
    }
}
