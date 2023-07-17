#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GOBEN;
using UnityEditor.UI;
using System;

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

	private string NPCName = "";
	private Personality personality;
	private string Home;
	private string WorkPlace;

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

	bool showUpAnimations = false;
	bool showDownAnimations = false;
	bool showLeftAnimations = false;
	bool showRightAnimations = false;
	bool showAnimations = false;
	bool showPersonality = false;

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

	

	private void OnGUI() {
		
		if (downAnimation2 != null) {
			DrawOnGUISprite(downAnimation2);
		}
		EditorGUILayout.BeginHorizontal();
		NPCName = EditorGUILayout.TextField("NPC Name:", NPCName);
		if (GUILayout.Button("Load")) {
			LoadData();
		}
		EditorGUILayout.EndHorizontal();

		showPersonality = EditorGUILayout.Foldout(showPersonality, "Agent data");
		if (showPersonality) {
			movementSpeed = EditorGUILayout.FloatField("Agent movement speed: ",movementSpeed);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Character addresses");
			Home = EditorGUILayout.TextField("Home Address: ",Home);
			WorkPlace = EditorGUILayout.TextField("Work Address: ", WorkPlace);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Personality Values");
			openness = EditorGUILayout.Slider("Openness",openness,0,1);
			consciousness = EditorGUILayout.Slider("Consciousness", consciousness,0,1);
			extroversion = EditorGUILayout.Slider("Extroversion",extroversion,0,1);
			agreeableness = EditorGUILayout.Slider("Agreeableness",agreeableness,0,1);
			neurotism = EditorGUILayout.Slider("Neurotism",neurotism,0,1);
		}

		showAnimations = EditorGUILayout.Foldout(showAnimations, "Animations");

		if (showAnimations) {
			icon = (Sprite)EditorGUILayout.ObjectField("icon", icon, typeof(Sprite), false);

			animationSpeed = EditorGUILayout.FloatField("Animation Speed:", animationSpeed);
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			showUpAnimations = EditorGUILayout.Foldout(showUpAnimations, "Up Animation");
			if (showUpAnimations) {
				upAnimation1 = (Sprite)EditorGUILayout.ObjectField("Up Animation 1", upAnimation1, typeof(Sprite), false);
				upAnimation2 = (Sprite)EditorGUILayout.ObjectField("Up Animation 2", upAnimation2, typeof(Sprite), false);
				upAnimation3 = (Sprite)EditorGUILayout.ObjectField("Up Animation 3", upAnimation3, typeof(Sprite), false);
			}

			showDownAnimations = EditorGUILayout.Foldout(showDownAnimations, "Down Animation");
			if (showDownAnimations) {
				downAnimation1 = (Sprite)EditorGUILayout.ObjectField("Down Animation 1", downAnimation1, typeof(Sprite), false);
				downAnimation2 = (Sprite)EditorGUILayout.ObjectField("Down Animation 2", downAnimation2, typeof(Sprite), false);
				downAnimation3 = (Sprite)EditorGUILayout.ObjectField("Down Animation 3", downAnimation3, typeof(Sprite), false);
			}

			showLeftAnimations = EditorGUILayout.Foldout(showLeftAnimations, "Left Animation");
			if (showLeftAnimations) {
				leftAnimation1 = (Sprite)EditorGUILayout.ObjectField("Left Animation 1", leftAnimation1, typeof(Sprite), false);
				leftAnimation2 = (Sprite)EditorGUILayout.ObjectField("Left Animation 2", leftAnimation2, typeof(Sprite), false);
				leftAnimation3 = (Sprite)EditorGUILayout.ObjectField("Left Animation 3", leftAnimation3, typeof(Sprite), false);
			}

			showRightAnimations = EditorGUILayout.Foldout(showRightAnimations, "Right Animation");
			if (showRightAnimations) {
				rightAnimation1 = (Sprite)EditorGUILayout.ObjectField("Right Animation 1", rightAnimation1, typeof(Sprite), false);
				rightAnimation2 = (Sprite)EditorGUILayout.ObjectField("Right Animation 2", rightAnimation2, typeof(Sprite), false);
				rightAnimation3 = (Sprite)EditorGUILayout.ObjectField("Right Animation 3", rightAnimation3, typeof(Sprite), false);
			}
			EditorGUILayout.EndScrollView();
		}

		if (GUILayout.Button("Create Prefab")) {
			CreationSystem();
		}

		GUILayout.Space(10);
		if (GUILayout.Button("Apply all", GUILayout.Width(60), GUILayout.Height(20))) {
			if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to change all NPC?", "Yes", "No")) {
				RemakeAllNPC();
			}
		}
	}

	void CreationSystem() {
		prefab_Blank = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/NPCdata/Blank.prefab", typeof(GameObject));

		SpriteRenderer[] childRenderers = prefab_Blank.GetComponentsInChildren<SpriteRenderer>();

		childRenderers[0].sprite = downAnimation2;
		childRenderers[1].sprite = icon;

		personality = new Personality(openness,consciousness,extroversion,agreeableness,neurotism);

		upAnimations = new Sprite[] { upAnimation1, upAnimation2, upAnimation3 };
		downAnimations = new Sprite[] { downAnimation1, downAnimation2, downAnimation3 };
		leftAnimations = new Sprite[] { leftAnimation1, leftAnimation2, leftAnimation3 };
		rightAnimations = new Sprite[] { rightAnimation1, rightAnimation2, rightAnimation3 };

		string savePath = CreateFolder(NPCName);


		if (NPCName == "") {
			ShowNotification(new GUIContent("NPC name is empty"));
			return;
		}
		if (upAnimations == null || downAnimations == null || leftAnimations == null || rightAnimations == null) {
			ShowNotification(new GUIContent("One or more animation arrays is empty"));
			return;
		}
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
		if(!Directory.Exists(path + "/Animations")) {
			Directory.CreateDirectory(path + "/Animations");
		}
		return path+ "/";
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

		data.iconName = "Sprites/Faces/"+icon.name;

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

			if(data.iconName != null)
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
			agreeableness =data.agreeableness;
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