using UnityEngine;
/*
* ZiroDev Copyright(c)
*
*/
public class NPCPathDebugGiver : MonoBehaviour {
        public NPCMovement[] NPCs;

        private void Start() {
            NPCs = FindObjectsOfType<NPCMovement>();
        }

        private void Update() {
            foreach (NPCMovement npc in NPCs) {
                if (npc.path == null || npc.path.Count == 0) {
                    string randomNodeId = WayPointContainer.instance.nodes[Random.Range(0, WayPointContainer.instance.nodes.Length)].id;
                    npc.GetPath(randomNodeId);
                }
            }
        }
    }

