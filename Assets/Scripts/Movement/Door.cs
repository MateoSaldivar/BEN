using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Door : MonoBehaviour {
    private MeshRenderer doorObject;
    public bool hasMeshRenderer;
    public bool isLocked = false;
    public Transform exitPoint;

    public Door connection; // Reference to the connected door
    public Collider teleportCollider; // Collider that teleports the agent

    public float closeDelay = 1f; // Time to wait before closing the door
    private bool isDelayed = false; // Flag to track if the door is currently delayed
    private float delayTimer = 0f; // Timer to track the delay time

    private int numColliders = 0;

    private void OnTriggerEnter(Collider other) {
        if (!isLocked && (other.CompareTag("Player") || other.CompareTag("NPC"))) {
            numColliders++;
            if (hasMeshRenderer) {
                doorObject.enabled = true;
            }
            isDelayed = false; // Reset the delay flag if the agent is close
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!isLocked && (other.CompareTag("Player") || other.CompareTag("NPC"))) {
            numColliders--;
            if (numColliders == 0 && hasMeshRenderer) {
                if (!isDelayed) {
                    isDelayed = true;
                    delayTimer = 0f;
                }
            }
        }
    }

    private void Start() {
        if (hasMeshRenderer) {
            doorObject = GetComponent<MeshRenderer>();
            if (doorObject != null) {
                doorObject.enabled = false; // Make sure door is initially disabled
            }
        }
    }

    private void TeleportAgent(Collider other) {
        // Make sure the other collider is the teleport collider and the connected door exists
        if (connection != null) {
            // Teleport the agent to the exit point of the connected door
            other.transform.position = connection.exitPoint.position;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (!isLocked && (collision.collider.CompareTag("Player") || collision.collider.CompareTag("NPC"))) {
            TeleportAgent(collision.collider);
        }
    }

    private void Update() {
        if (isDelayed) {
            delayTimer += Time.deltaTime;
            if (delayTimer >= closeDelay) {
                doorObject.enabled = false;
                isDelayed = false;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (exitPoint != null)
            Gizmos.DrawSphere(exitPoint.position, 0.2f);
    }
#endif
}
