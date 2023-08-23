#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GOBEN;
using UnityEditor.UI;
using System;
using System.Linq;

using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class NPCData {
	public string NPCName;
	public float AnimationSpeed;
	public float movementSpeed;
	public string SpriteSheetPath;

	public string home;
	public string work;

	public float openness;
	public float consciousness;
	public float extroversion;
	public float agreeableness;
	public float neurotism;

	public string iconName;

	public string[] upAnimationNames;
	public string[] downAnimationNames;
	public string[] rightAnimationNames;
	public string[] leftAnimationNames;
}


public class NPCPrefabCreator : EditorWindow {

	private string NPCName = "Name";
	private int Age = 16;
	private Personality personality;
	private string Home = "";
	private string WorkPlace = "";
	private string Description = "";

	Sprite upAnimation1;
	Sprite upAnimation2;
	Sprite upAnimation3;
	Sprite downAnimation1;
	Sprite downAnimation2;
	Sprite downAnimation3;
	Sprite leftAnimation1;
	Sprite leftAnimation2;
	Sprite leftAnimation3;
	Sprite rightAnimation1;
	Sprite rightAnimation2;
	Sprite rightAnimation3;

	private float openness = 0.5f;
	private float consciousness = 0.5f;
	private float extroversion = 0.5f;
	private float agreeableness = 0.5f;
	private float neurotism = 0.5f;

	bool showUpAnimations = true;
	bool showDownAnimations = true;
	bool showLeftAnimations = true;
	bool showRightAnimations = true;
	bool showAnimations = true;
	bool showPersonality = true;

	private Sprite icon;

	private Sprite[] upAnimations;
	private Sprite[] downAnimations;
	private Sprite[] leftAnimations;
	private Sprite[] rightAnimations;

	private float animationSpeed = 0.16f;
	private float movementSpeed = 45f;
	Vector2 scrollPos;

	private Dictionary<string, Sprite> SpriteDictionary;

	private GameObject prefab_Blank;

	[MenuItem("BEN/Create NPC Prefab")]
	public static void ShowWindow() {
		EditorWindow.GetWindow(typeof(NPCPrefabCreator));
	}

	private void OnEnable() {
		LoadData();

		minSize = new Vector2(400, 350);
		maxSize = new Vector2(400, 350);

	}

	public enum Page {
		general,
		personality,
		visual
	}
	public Page currentPage = Page.general;
	private void OnGUI() {
		if (currentPage == Page.general) {
			DrawDownAnimation();

			EditorGUILayout.BeginHorizontal();
			NPCName = EditorGUILayout.TextField("NPC Name:", NPCName);
			if (GUILayout.Button("Load")) {
				LoadData();
			}
			EditorGUILayout.EndHorizontal();
			Age = EditorGUILayout.IntField("NPC Age:", Age);

			EditorGUILayout.LabelField("Character addresses");
			Home = EditorGUILayout.TextField("Home Address: ", Home);
			WorkPlace = EditorGUILayout.TextField("Work Address: ", WorkPlace);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Description");
			Description = EditorGUILayout.TextArea(Description, GUILayout.Height(50));
		} else if (currentPage == Page.personality) {
			DrawPersonality();
		} else if (currentPage == Page.visual) {
			//DrawAnimations();

			DrawAnimationButtons();
			GUILayout.Space(10);
			//DrawCreatePrefabButton();
			//DrawApplyAllButton();
		}


		// Button to go back
		DrawMovementButtons();
	}

	private float buttonSize = 60f; // Size of the buttons
	private Sprite image; // Your image texture
	private direction currentDirection = direction.down;
	private enum direction {
		up, down, right, left
	}
	private void DrawAnimationButtons() {
		if (image == null && downAnimation2 != null) image = downAnimation2;
		icon = (Sprite)EditorGUILayout.ObjectField("icon", icon, typeof(Sprite), false);
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();

		GUILayout.FlexibleSpace();

		GUILayout.BeginVertical();

		GUILayout.FlexibleSpace();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Up", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize))) {
			if (upAnimation2 != null) {
				image = upAnimation2;
			}
			currentDirection = direction.up;
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();

		GUILayout.BeginHorizontal(); // Center the "Left" and "Right" buttons

		if (GUILayout.Button("Left", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize))) {
			if (downAnimation2 != null) {
				image = leftAnimation2;
			}
			currentDirection = direction.left;
		}

		if (image != null)
			DrawOnGUISpritesmall(image, 65);

		if (GUILayout.Button("Right", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize))) {
			if (rightAnimation2 != null) {
				image = rightAnimation2;
			}
			currentDirection = direction.right;
		}

		GUILayout.EndHorizontal(); // End centering

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Down", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize))) {
			if (downAnimation2 != null) {
				image = downAnimation2;
			}
			currentDirection = direction.down;
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.FlexibleSpace();

		GUILayout.EndVertical();

		GUILayout.FlexibleSpace();

		GUILayout.EndHorizontal();
		switch (currentDirection) {
			case direction.up:
				DrawFoldoutAnimations(ref showUpAnimations, ref upAnimation1, ref upAnimation2, ref upAnimation3);
				break;
			case direction.down:
				DrawFoldoutAnimations(ref showDownAnimations, ref downAnimation1, ref downAnimation2, ref downAnimation3);
				break;
			case direction.right:
				DrawFoldoutAnimations(ref showRightAnimations, ref rightAnimation1, ref rightAnimation2, ref rightAnimation3);
				break;
			case direction.left:
				DrawFoldoutAnimations(ref showLeftAnimations, ref leftAnimation1, ref leftAnimation2, ref leftAnimation3);
				break;
		}

		GUILayout.EndVertical();
	}


	private void DrawMovementButtons() {
		GUILayout.FlexibleSpace(); // Add flexible space above the buttons

		// Calculate button width and height
		float buttonWidth = 80;
		float buttonHeight = 30;

		GUILayout.BeginHorizontal(); // Start a horizontal layout group

		GUILayout.BeginArea(new Rect(10, Screen.height - buttonHeight - 30, buttonWidth, buttonHeight));
		if (currentPage != Page.general && GUILayout.Button("Back", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
			// Go back to the previous page
			currentPage--;
		}
		GUILayout.EndArea();

		GUILayout.BeginArea(new Rect(Screen.width - buttonWidth - 10, Screen.height - buttonHeight - 30, buttonWidth, buttonHeight));
		if (currentPage != Page.visual && GUILayout.Button("Next", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
			// Move to the next page
			currentPage++;
		}
		GUILayout.EndArea();

		if (currentPage == Page.visual) {
			// Create Prefab and Apply all buttons at the position of the Next button
			

			GUILayout.BeginArea(new Rect(Screen.width - buttonWidth - 10, Screen.height - buttonHeight - 30, buttonWidth, buttonHeight));
			if (GUILayout.Button("Apply all", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
				if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to change all NPC?", "Yes", "No")) {
					RemakeAllNPC();
				}
			}
			GUILayout.EndArea();

			GUILayout.BeginArea(new Rect(Screen.width - buttonWidth - 10, Screen.height - buttonHeight - 60, buttonWidth, buttonHeight));
			if (GUILayout.Button("Save", GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight))) {
				CreationSystem();
			}
			GUILayout.EndArea();
		}

		GUILayout.EndHorizontal(); // End the horizontal layout group
	}

	private void DrawDownAnimation() {
		if (downAnimation2 != null) {
			DrawOnGUISprite(downAnimation2);
		}
	}

	private void DrawPersonality() {
		EditorGUILayout.LabelField("Personality");

		//movementSpeed = EditorGUILayout.FloatField("Agent movement speed: ", movementSpeed);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Personality Values");
		EditorGUILayout.Space();
		openness = EditorGUILayout.Slider("Openness", openness, 0, 1);
		EditorGUILayout.Space();
		consciousness = EditorGUILayout.Slider("Consciousness", consciousness, 0, 1);
		EditorGUILayout.Space();
		extroversion = EditorGUILayout.Slider("Extroversion", extroversion, 0, 1);
		EditorGUILayout.Space();
		agreeableness = EditorGUILayout.Slider("Agreeableness", agreeableness, 0, 1);
		EditorGUILayout.Space();
		neurotism = EditorGUILayout.Slider("Neurotism", neurotism, 0, 1);
		EditorGUILayout.Space();
		string personalityDescription = GetPersonalityDescription();
		EditorGUILayout.LabelField("Personality Description: " + personalityDescription);

	}

	Dictionary<string, string[]> traitWords = new Dictionary<string, string[]>
		{
			{ "Openness", new string[] { "Imaginative", "Innovative", "Curious", "Empathetic", "Sensitive" } },
			{ "Conscientiousness", new string[] { "Detail-oriented", "Responsible", "Efficient", "Dependable", "Anxious" } },
			{ "Extraversion", new string[] { "Social", "Energetic", "Outgoing", "Friendly", "Assertive" } },
			{ "Agreeableness", new string[] { "Kind", "Considerate", "Cooperative", "Compassionate", "Warm" } },
			{ "Neuroticism", new string[] { "Anxious", "Worried", "Nervous", "Sensitive", "Emotional" } }
		};


	public string GetPersonalityDescription() {
		if (AllTraitsAtValue(0)) return "Apathetic";
		if (AllTraitsAtValue(0.5f)) return "Neutral";
		if (AllTraitsAtValue(1)) return "Exceptional";

		int[] orderIndex = { 0, 1, 2, 3, 4 };
		float[] traits = { openness, consciousness, extroversion, agreeableness, neurotism };

		// Create a custom comparer that sorts based on the traits array
		Array.Sort(orderIndex, (index1, index2) => traits[index2].CompareTo(traits[index1]));

		var highestTraitIndex = orderIndex[0];
		var secondHighestTraitIndex = orderIndex[1];

		if (traits[highestTraitIndex] - traits[secondHighestTraitIndex] >= 0.5f) {
			secondHighestTraitIndex = orderIndex[0];
		}

		var highestTraitKey = traitWords.ElementAt(highestTraitIndex).Key;

		// Return the word from the dictionary using the trait keys
		return traitWords[highestTraitKey][secondHighestTraitIndex];
	}

	public bool AllTraitsAtValue(float val) {
		return openness == val && consciousness == val && extroversion == val && agreeableness == val && neurotism == val;
	}



	private void DrawFoldoutAnimations(ref bool show, ref Sprite animation1, ref Sprite animation2, ref Sprite animation3) {
		
		EditorGUILayout.LabelField("Frames");

		EditorGUI.indentLevel++; // Increase the indent level

		GUILayout.Space(2); // Add a small space before the horizontal layout

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace(); // Center the following content

		// Create a custom style for preview boxes
		GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
		boxStyle.fixedWidth = 70; // Fixed width for the preview box
		boxStyle.fixedHeight = 60; // Fixed height for the preview box

		GUILayout.BeginHorizontal();

		// Draw the ObjectFields with preview boxes using EditorGUILayout
		animation1 = (Sprite)EditorGUILayout.ObjectField(animation1, typeof(Sprite), false, GUILayout.Width(70), GUILayout.Height(60));
		animation2 = (Sprite)EditorGUILayout.ObjectField(animation2, typeof(Sprite), false, GUILayout.Width(70), GUILayout.Height(60));
		animation3 = (Sprite)EditorGUILayout.ObjectField(animation3, typeof(Sprite), false, GUILayout.Width(70), GUILayout.Height(60));

		GUILayout.EndHorizontal();

		GUILayout.FlexibleSpace(); // Center the following content
		GUILayout.EndHorizontal();

		EditorGUI.indentLevel--; // Decrease the indent level

	}


	void DrawOnGUISpritesmall(Sprite aSprite, float targetSize) {
		Rect c = aSprite.rect;
		float spriteW = c.width;
		float spriteH = c.height;
		float aspectRatio = spriteW / spriteH;

		GUILayout.FlexibleSpace(); // Center the sprite vertically

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace(); // Center the sprite horizontally

		Rect rect = GUILayoutUtility.GetRect(targetSize * aspectRatio, targetSize);

		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();

		GUILayout.FlexibleSpace();

		if (Event.current.type == EventType.Repaint) {
			var tex = aSprite.texture;
			c.xMin /= tex.width;
			c.xMax /= tex.width;
			c.yMin /= tex.height;
			c.yMax /= tex.height;

			if (rect.width > rect.height) {
				rect.width = rect.height * aspectRatio;
			} else {
				rect.height = rect.width / aspectRatio;
			}

			GUI.DrawTextureWithTexCoords(rect, tex, c);
		}
	}

	private void DrawCreatePrefabButton() {
		if (GUILayout.Button("Create Prefab")) {
			CreationSystem();
		}
	}

	private void DrawApplyAllButton() {
		if (GUILayout.Button("Apply all", GUILayout.Width(60), GUILayout.Height(20))) {
			if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to change all NPC?", "Yes", "No")) {
				RemakeAllNPC();
			}
		}
	}

	void CreationSystem() {
		if (NPCName == "Name") {
			DateTime currentTime = DateTime.Now;
			string formattedTime = currentTime.ToString("yyMMddHHmm");
			ShowNotification(new GUIContent("Saving current advances, but NPC remains default"));
			NPCName += formattedTime;
		}

		if (NPCName == "") {
			ShowNotification(new GUIContent("NPC name is empty"));
			return;
		}

		if (upAnimations == null || downAnimations == null || leftAnimations == null || rightAnimations == null) {
			ShowNotification(new GUIContent("One or more animation arrays is empty"));
			return;
		}

		prefab_Blank = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/NPCdata/Blank.prefab", typeof(GameObject));

		SpriteRenderer[] childRenderers = prefab_Blank.GetComponentsInChildren<SpriteRenderer>();

		childRenderers[0].sprite = downAnimation2;
		childRenderers[1].sprite = icon;

		personality = new Personality(openness, consciousness, extroversion, agreeableness, neurotism);

		upAnimations = new Sprite[] { upAnimation1, upAnimation2, upAnimation3 };
		downAnimations = new Sprite[] { downAnimation1, downAnimation2, downAnimation3 };
		leftAnimations = new Sprite[] { leftAnimation1, leftAnimation2, leftAnimation3 };
		rightAnimations = new Sprite[] { rightAnimation1, rightAnimation2, rightAnimation3 };

		string savePath = CreateFolder(NPCName);

		SaveData();
		CreateAnimations(savePath);
		CreateNPCPrefab();
		SetAnimatorActiveInstances();
	}

	void RemakeAllNPC() {
		string[] npcFolders = Directory.GetDirectories(Application.dataPath + "/NPCdata/");

		foreach (string folder in npcFolders) {
			NPCName = Path.GetFileName(folder);
			Debug.Log(NPCName);
			LoadData();
			CreationSystem();
		}
	}

	void DrawOnGUISprite(Sprite aSprite) {
		Rect c = aSprite.rect;
		float spriteW = c.width;
		float spriteH = c.height;
		float spriteHeight = Screen.height / 3;
		Rect rect = GUILayoutUtility.GetRect(spriteW, spriteHeight);
		if (Event.current.type == EventType.Repaint) {
			var tex = aSprite.texture;
			c.xMin /= tex.width;
			c.xMax /= tex.width;
			c.yMin /= tex.height;
			c.yMax /= tex.height;
			float aspectRatio = spriteW / spriteH;
			if (rect.width > rect.height) {
				rect.width = rect.height * aspectRatio;
			} else {
				rect.height = rect.width / aspectRatio;
			}

			rect.x = (Screen.width - rect.width) / 2;
			GUI.DrawTextureWithTexCoords(rect, tex, c);
		}
	}

	AnimationClip upClip;
	AnimationClip downClip;
	AnimationClip leftClip;
	AnimationClip rightClip;


	AnimationClip upIdleClip;
	AnimationClip downIdleClip;
	AnimationClip leftIdleClip;
	AnimationClip rightIdleClip;

	void CreateAnimations(string savePath) {
		savePath += "Animations/";
		upClip = CreateAnimationClip(upAnimations, "Up" + NPCName, animationSpeed);
		ActivateLoopTime(upClip);
		downClip = CreateAnimationClip(downAnimations, "Down" + NPCName, animationSpeed);
		ActivateLoopTime(downClip);
		leftClip = CreateAnimationClip(leftAnimations, "Left" + NPCName, animationSpeed);
		ActivateLoopTime(leftClip);
		rightClip = CreateAnimationClip(rightAnimations, "Right" + NPCName, animationSpeed);
		ActivateLoopTime(rightClip);

		upIdleClip = CreateIdleAnimationClip(upAnimations, "UpIdle" + NPCName, animationSpeed);
		downIdleClip = CreateIdleAnimationClip(downAnimations, "DownIdle" + NPCName, animationSpeed);
		leftIdleClip = CreateIdleAnimationClip(leftAnimations, "LeftIdle" + NPCName, animationSpeed);
		rightIdleClip = CreateIdleAnimationClip(rightAnimations, "RightIdle" + NPCName, animationSpeed);

		AssetDatabase.CreateAsset(upClip, savePath + upClip.name + ".anim");
		AssetDatabase.CreateAsset(downClip, savePath + downClip.name + ".anim");
		AssetDatabase.CreateAsset(leftClip, savePath + leftClip.name + ".anim");
		AssetDatabase.CreateAsset(rightClip, savePath + rightClip.name + ".anim");
		AssetDatabase.CreateAsset(upIdleClip, savePath + upIdleClip.name + ".anim");
		AssetDatabase.CreateAsset(downIdleClip, savePath + downIdleClip.name + ".anim");
		AssetDatabase.CreateAsset(leftIdleClip, savePath + leftIdleClip.name + ".anim");
		AssetDatabase.CreateAsset(rightIdleClip, savePath + rightIdleClip.name + ".anim");
		AssetDatabase.SaveAssets();
		CreateAnimator(savePath);
	}


	public void CreateNPCPrefab() {

		GameObject originalPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/NPCdata/Blank.prefab", typeof(GameObject));

		GameObject newPrefabInstance = Instantiate(originalPrefab);
		SpriteRenderer prefabRenderer = newPrefabInstance.transform.GetChild(0).GetComponent<SpriteRenderer>();

		prefabRenderer.sprite = downAnimation2;

		Animator animator = newPrefabInstance.transform.GetChild(0).GetComponent<Animator>();
		animator.runtimeAnimatorController = (RuntimeAnimatorController)AssetDatabase.LoadAssetAtPath("Assets/NPCdata/" + NPCName + "/Animations/AnimatorController" + NPCName + ".controller", typeof(RuntimeAnimatorController));

		NPC data = newPrefabInstance.GetComponent<NPC>();
		data.agent = new Agent();
		data.agent.SetPersonality(personality);
		data.CreateBaseBeliefs(Home, WorkPlace);


		data.SaveAgentData(NPCName);

		data.homeAddress = Home;
		data.workAddress = WorkPlace;

		CharacterMovement mover = newPrefabInstance.GetComponent<CharacterMovement>();
		if (mover != null) {
			mover.speed = movementSpeed;
		}


		string path = "Assets/NPCdata/" + NPCName + "/" + NPCName + ".prefab";
		PrefabUtility.SaveAsPrefabAsset(newPrefabInstance, path);

		DestroyImmediate(newPrefabInstance);


	}

	void SetAnimatorActiveInstances() {
		GameObject instance = GameObject.Find(NPCName);
		if (instance != null) {
			instance.GetComponentInChildren<Animator>().runtimeAnimatorController = (RuntimeAnimatorController)AssetDatabase.LoadAssetAtPath("Assets/NPCdata/" + NPCName + "/Animations/AnimatorController" + NPCName + ".controller", typeof(RuntimeAnimatorController));
		}
	}

	void ActivateLoopTime(AnimationClip clip) {
		AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
		settings.loopTime = true;
		AnimationUtility.SetAnimationClipSettings(clip, settings);
	}

	string CreateFolder(string folderName) {
		string path = "Assets/NPCdata/" + folderName;
		if (!Directory.Exists(path)) {
			Directory.CreateDirectory(path);

		}
		if (!Directory.Exists(path + "/Animations")) {
			Directory.CreateDirectory(path + "/Animations");
		}
		return path + "/";
	}

	AnimationClip CreateAnimationClip(Sprite[] sprites, string clipName, float speed) {
		AnimationClip clip = new AnimationClip();
		clip.name = clipName;

		EditorCurveBinding spriteBinding = new EditorCurveBinding();
		spriteBinding.type = typeof(SpriteRenderer);
		spriteBinding.path = "";
		spriteBinding.propertyName = "m_Sprite";
		ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[5];
		for (int i = 0; i < 5; i++) {
			keyFrames[i] = new ObjectReferenceKeyframe();
			keyFrames[i].time = i * speed;
			keyFrames[i].value = sprites[i < sprites.Length ? i : 1];
		}

		AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);
		return clip;
	}


	AnimationClip CreateIdleAnimationClip(Sprite[] sprites, string clipName, float speed) {
		AnimationClip clip = new AnimationClip();
		clip.name = clipName;

		EditorCurveBinding spriteBinding = new EditorCurveBinding();
		spriteBinding.type = typeof(SpriteRenderer);
		spriteBinding.path = "";
		spriteBinding.propertyName = "m_Sprite";

		ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[1];
		keyFrames[0] = new ObjectReferenceKeyframe();
		keyFrames[0].time = 0;
		keyFrames[0].value = sprites[1];

		AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);
		return clip;
	}

	void CreateAnimator(string savePath) {
		AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath(savePath + "AnimatorController" + NPCName + ".controller");

		var baseLayer = animatorController.layers[0];


		var idleState = new AnimatorState { name = "Idle" };
		var motionState = new AnimatorState { name = "Motion" };

		idleState = baseLayer.stateMachine.AddState("Idle");
		motionState = baseLayer.stateMachine.AddState("Motion");


		// Create a new blend tree for idle animations
		BlendTree idleBlendTree = new BlendTree();
		idleBlendTree.name = "Idle";
		idleBlendTree.blendType = BlendTreeType.Simple1D;
		idleBlendTree.blendParameter = "Dir";


		// Add idle animations to the blend tree
		idleBlendTree.AddChild(downIdleClip, 0f);
		idleBlendTree.AddChild(upIdleClip, 0.333f);
		idleBlendTree.AddChild(rightIdleClip, 0.666f);
		idleBlendTree.AddChild(leftIdleClip, 1f);

		// Create a new blend tree for mover animations
		BlendTree motionBlendTree = new BlendTree();
		motionBlendTree.name = "Motion";
		motionBlendTree.blendType = BlendTreeType.SimpleDirectional2D;
		motionBlendTree.blendParameter = "movementX";
		motionBlendTree.blendParameterY = "movementY";

		// Add mover animations to the blend tree
		motionBlendTree.AddChild(rightClip, new Vector2(1f, 0f));
		motionBlendTree.AddChild(leftClip, new Vector2(-1f, 0f));
		motionBlendTree.AddChild(downClip, new Vector2(0f, -1f));
		motionBlendTree.AddChild(upClip, new Vector2(0f, 1f));

		// Add the blend trees to the controller
		motionState.motion = motionBlendTree;
		idleState.motion = idleBlendTree;

		// Add parameters to the controller
		animatorController.AddParameter("movementX", AnimatorControllerParameterType.Float);
		animatorController.AddParameter("movementY", AnimatorControllerParameterType.Float);
		animatorController.AddParameter("Dir", AnimatorControllerParameterType.Float);
		animatorController.AddParameter("Speed", AnimatorControllerParameterType.Float);


		AnimatorStateTransition transition1 = baseLayer.stateMachine.AddAnyStateTransition(motionState);
		transition1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
		transition1.duration = 0;
		transition1.canTransitionToSelf = false;

		AnimatorStateTransition transition2 = baseLayer.stateMachine.AddAnyStateTransition(idleState);
		transition2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
		transition2.duration = 0;
		transition2.canTransitionToSelf = false;

		AssetDatabase.CreateAsset(idleBlendTree, savePath + "IdleBlendTree" + NPCName + ".asset");
		AssetDatabase.CreateAsset(motionBlendTree, savePath + "MotionBlendTree" + NPCName + ".asset");
		AssetDatabase.SaveAssets();
	}

	void SaveData() {
		NPCData data = new NPCData();
		data.SpriteSheetPath = AssetDatabase.GetAssetPath(upAnimations[0]);
		data.SpriteSheetPath = data.SpriteSheetPath.Replace("Assets/Resources/", "");
		data.SpriteSheetPath = data.SpriteSheetPath.Replace(".png", "");

		data.NPCName = NPCName;
		data.AnimationSpeed = animationSpeed;
		data.movementSpeed = movementSpeed;

		data.home = Home;
		data.work = WorkPlace;

		data.openness = openness;
		data.consciousness = consciousness;
		data.agreeableness = agreeableness;
		data.neurotism = neurotism;
		data.extroversion = extroversion;

		data.iconName = "Sprites/Faces/" + icon.name;

		string[] upAnimationPaths = new string[3];
		for (int i = 0; i < upAnimations.Length; i++) {
			upAnimationPaths[i] = upAnimations[i].name;
		}
		data.upAnimationNames = upAnimationPaths;

		string[] downAnimationPaths = new string[3];
		for (int i = 0; i < downAnimations.Length; i++) {
			downAnimationPaths[i] = downAnimations[i].name;
		}
		data.downAnimationNames = downAnimationPaths;

		string[] leftAnimationPaths = new string[3];
		for (int i = 0; i < leftAnimations.Length; i++) {
			leftAnimationPaths[i] = leftAnimations[i].name;
		}
		data.leftAnimationNames = leftAnimationPaths;

		string[] rightAnimationPaths = new string[3];
		for (int i = 0; i < rightAnimations.Length; i++) {
			rightAnimationPaths[i] = rightAnimations[i].name;
		}
		data.rightAnimationNames = rightAnimationPaths;

		string savePath = "Assets/NPCdata/" + NPCName + "/" + NPCName + "prefab.dat";
		FileStream stream = new FileStream(savePath, FileMode.Create);
		BinaryFormatter formatter = new BinaryFormatter();
		formatter.Serialize(stream, data);
		stream.Close();

		AssetDatabase.Refresh();
	}

	private void LoadData() {
		string path = "Assets/NPCdata/" + NPCName + "/" + NPCName + "prefab.dat";
		if (File.Exists(path)) {
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(path, FileMode.Open);
			NPCData data = (NPCData)bf.Deserialize(file);
			file.Close();
			LoadDictionary(data.SpriteSheetPath);

			NPCName = data.NPCName;
			animationSpeed = data.AnimationSpeed;
			movementSpeed = data.movementSpeed;

			if (data.iconName != null)
				icon = Resources.Load<Sprite>(data.iconName);

			upAnimation1 = GetSpriteByName(data.upAnimationNames[0]);
			upAnimation2 = GetSpriteByName(data.upAnimationNames[1]);
			upAnimation3 = GetSpriteByName(data.upAnimationNames[2]);

			downAnimation1 = GetSpriteByName(data.downAnimationNames[0]);
			downAnimation2 = GetSpriteByName(data.downAnimationNames[1]);
			downAnimation3 = GetSpriteByName(data.downAnimationNames[2]);

			leftAnimation1 = GetSpriteByName(data.leftAnimationNames[0]);
			leftAnimation2 = GetSpriteByName(data.leftAnimationNames[1]);
			leftAnimation3 = GetSpriteByName(data.leftAnimationNames[2]);

			rightAnimation1 = GetSpriteByName(data.rightAnimationNames[0]);
			rightAnimation2 = GetSpriteByName(data.rightAnimationNames[1]);
			rightAnimation3 = GetSpriteByName(data.rightAnimationNames[2]);

			WorkPlace = data.work;
			Home = data.home;

			openness = data.openness;
			consciousness = data.consciousness;
			agreeableness = data.agreeableness;
			extroversion = data.extroversion;
			neurotism = data.neurotism;
		}
	}


	private void LoadDictionary(string path) {
		Sprite[] SpritesData = Resources.LoadAll<Sprite>(path);
		SpriteDictionary = new Dictionary<string, Sprite>();

		for (int i = 0; i < SpritesData.Length; i++) {
			SpriteDictionary.Add(SpritesData[i].name, SpritesData[i]);
		}
	}

	public Sprite GetSpriteByName(string name) {
		if (SpriteDictionary.ContainsKey(name))
			return SpriteDictionary[name];
		else
			return null;
	}

}

#endif