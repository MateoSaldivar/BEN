using UnityEngine;
/*
* ZiroDev Copyright(c)
*
*/
public class Node : MonoBehaviour {
    [HideInInspector] public Vector2 position;
    public int Index;
    public string id;
    public float radius;
    public Node[] Neighbours;

    private void Awake() {
        position = new Vector2(transform.position.x, transform.position.z);
    }

    public Vector2 GetRandomPositionInsideRadius() {
        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(0f, radius);
        float x = position.x + randomDistance * Mathf.Cos(randomAngle);
        float y = position.y + randomDistance * Mathf.Sin(randomAngle);
        
        return new Vector2(x, y);
    }

    private void OnDrawGizmos() {
        if (Neighbours.Length > 0) {
            Gizmos.color = Color.red;
            position = new Vector2(transform.position.x, transform.position.z);
            foreach (Node n in Neighbours) {
               Gizmos.DrawLine(new Vector3(position.x,transform.position.y,position.y), n.transform.position);
            }
        }
        
    }
}