using UnityEngine;

public class StaticDisableable : MonoBehaviour {
    private MeshRenderer[] meshRenderers;
    private Collider[] colliders;
    [HideInInspector] public bool active = true;
    private void Start() {
        SetUp();
    }

	public void SetUp() {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        colliders = GetComponentsInChildren<Collider>();
    }
    public void DisableObject() {
        foreach (MeshRenderer meshRenderer in meshRenderers) {
            meshRenderer.enabled = false;
        }

        foreach (Collider collider in colliders) {
            collider.enabled = false;
        }
    }

    public void EnableObject() {
        if(meshRenderers != null && meshRenderers.Length > 0)
        foreach (MeshRenderer meshRenderer in meshRenderers) {
            meshRenderer.enabled = true;
        }

        if (colliders != null && colliders.Length > 0)
            foreach (Collider collider in colliders) {
            collider.enabled = true;
        }
    }
}
