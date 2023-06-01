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
            
        }
    }

