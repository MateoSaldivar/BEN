using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class MoveRectTransform : MonoBehaviour {
	public Vector3 pointA;
	public Vector3 pointB;

	public RectTransform rectTransform;
	public GameObject holder;
	private Vector3 targetPosition;
	public float speed = 50f;


	void Start() {
		targetPosition = pointA;
		rectTransform.localPosition = pointA;
	}

	void Update() {

		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (targetPosition == pointA) {
				targetPosition = pointB;
			} else if (targetPosition == pointB) {
				targetPosition = pointA;
			}
		}

		rectTransform.localPosition = Vector3.MoveTowards(rectTransform.localPosition, targetPosition, Time.deltaTime * speed);

		holder.SetActive(rectTransform.localPosition != pointA);
		
	}
}





#if UNITY_EDITOR

[CustomEditor(typeof(MoveRectTransform))]
public class MoveRectTransformEditor : Editor {
	private MoveRectTransform script;

	private void OnEnable() {
		script = (MoveRectTransform)target;
	}

	public override void OnInspectorGUI() {
		base.OnInspectorGUI();

		if (GUILayout.Button("Set Point A")) {
			script.pointA = script.rectTransform.localPosition;
		}

		if (GUILayout.Button("Set Point B")) {
			script.pointB = script.rectTransform.localPosition;
		}
	}
}
#endif