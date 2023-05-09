using UnityEngine;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour {
    public static ObjectManager instance;
    public float xDistance;
    public float yDistance;

    private int playerCurrentArea;
    private int previousPlayerArea;
	private void Awake() {
        instance = this;
	}

	private void Update() {
        playerCurrentArea = Player.instance.currentArea;

        // Only check for objects in other areas if player's current area changed
        if (playerCurrentArea != previousPlayerArea) {
            previousPlayerArea = playerCurrentArea;
            AreaTrigger area = AreaContainer.instance.areas[playerCurrentArea];
            AreaTrigger[] neighbors = area.neighbours;

            // Disable objects in other areas
            foreach (AreaTrigger a in AreaContainer.instance.areas) {
                if (a.area != playerCurrentArea && !ArrayContainsValue(neighbors, a)) {
                    foreach (StaticDisableable obj in a.objects) {
                        obj.DisableObject();
                    }
                }
            }
        }

        // Check distance for objects in current area and neighbors
        AreaTrigger currentArea = AreaContainer.instance.areas[playerCurrentArea];
        AreaTrigger[] currentNeighbors = currentArea.neighbours;
        foreach (StaticDisableable obj in currentArea.objects) {
            float distanceX = Mathf.Abs(obj.transform.position.x - Player.instance.transform.position.x);
            float distanceY = Mathf.Abs(obj.transform.position.z - Player.instance.transform.position.z);
            if (distanceX <= xDistance && distanceY <= yDistance) {
                obj.EnableObject();
            } else {
                obj.DisableObject();
            }
        }
        foreach (AreaTrigger neighborArea in currentNeighbors) {
            foreach (StaticDisableable obj in neighborArea.objects) {
                float distanceX = Mathf.Abs(obj.transform.position.x - Player.instance.transform.position.x);
                float distanceY = Mathf.Abs(obj.transform.position.y - Player.instance.transform.position.y);
                if (distanceX <= xDistance && distanceY <= yDistance ) {
                    obj.EnableObject();
                } else {
                    obj.DisableObject();
                }
            }
        }
    }

    public static bool IsCurrentAreaOrNeighbor(int areaIndex) {
        AreaTrigger playerCurrentArea = AreaContainer.instance.areas[Player.instance.currentArea];
        return areaIndex == Player.instance.currentArea || ArrayContainsValue(AreaContainer.instance.areas[areaIndex].neighbours, playerCurrentArea);
    }


    public static bool ArrayContainsValue(AreaTrigger[] array, AreaTrigger value) {
        for (int i = 0; i < array.Length; i++) {
            if (array[i] == value) {
                return true;
            }
        }
        return false;
    }
}
