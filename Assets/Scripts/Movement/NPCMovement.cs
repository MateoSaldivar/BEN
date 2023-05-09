using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AVA = AgentVariableAdjuster;
/*
* ZiroDev Copyright(c)
*
*/
public class NPCMovement : CharacterMovement {
    public Vector2 targetPosition;
    public Node LastNode;
    [HideInInspector] public Queue<Node> path;
    private bool hasReachedTarget = true;

    public bool stopped = false;
    private float cooldownTimer = 0f;
    private float freezeTime = 0f;
    NPC main;

	private void Awake() {
		main = GetComponent<NPC>();
        main.SetMover(this);
	}
	void Start() {
        path = new Queue<Node>();
        if(LastNode == null) LastNode = WayPointContainer.instance.GetClosestNode(this);
    }

    public void GetPath(string id) {
        if (id == "") return;

        if (LastNode == null) LastNode = WayPointContainer.instance.GetClosestNode(this);

        Node targetNode = WayPointContainer.IdReferencer[id];
        path = new Queue<Node>(FloydWarshall.ConstructPath(LastNode, targetNode));
    }

    void Update() {
        if (path?.Count > 0) {
            if (hasReachedTarget) {
                Node nextNode = path.Peek();
                if (nextNode.radius > 0) {
                    targetPosition = nextNode.GetRandomPositionInsideRadius();
                } else {
                    targetPosition = nextNode.position;
                }
                hasReachedTarget = false;
            }

            // Check if the agent is currently stopped due to player detection and the cooldown has expired
            if (stopped && freezeTime <= 0f) {
                Resume();
            }

            // Decrement the cooldown timer if it's running
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (freezeTime > 0f) freezeTime -= Time.deltaTime;
            

            if(!stopped) MoveTowardsPoint(targetPosition);
            else Move(Vector3.zero);
        } else {
            Move(Vector3.zero);
		}
        ApplyGravity();
    }

    public void MoveTowardsPoint(Vector2 targetPoint) {
        Vector2 moveDirection = (targetPoint - VCU.VectorXZ(transform.position)).normalized;
       
        Move(new Vector3(-moveDirection.x,0, -moveDirection.y) * speed);
 
        if((Vector2.Distance(VCU.VectorXZ(transform.position), path.Peek().position) < Mathf.Abs(path.Peek().radius-0.1f) && path.Count > 1) || Vector2.Distance(VCU.VectorXZ(transform.position), targetPoint) < 0.45f) { 
            LastNode = path.Dequeue();
            hasReachedTarget = true;
        }
    }

    public void Stop() {
        if (!stopped && cooldownTimer <= 0f) {
            freezeTime = 3f;
            stopped = true;
            //Invoke("Resume", 3f);
        }
    }

    private void Resume() {
        stopped = false;
        cooldownTimer = 10f;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos() {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.0f);
        if (path != null && path.Count > 0) {
			// Draw a sphere at each node in the path
			foreach (Node n in path) {
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(n.transform.position, 0.51f);
			}
        }
	}
#endif

}