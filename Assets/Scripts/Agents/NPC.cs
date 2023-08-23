using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using Newtonsoft.Json;
using ST = Utils.SymbolTable;
using GR = GlobalRegistry;
using GOBEN;


public class NPC : MonoBehaviour {

	// Public properties
	public Agent agent { get; set; }

	// Essential properties
	public string homeAddress = "";
	public string workAddress = "";
	public int money = 0;

	// Hidden properties
	[HideInInspector] public List<Agent> closeAgents;
	[HideInInspector] public DynamicDisableable dynamicDisableable;
	[HideInInspector] public Schedule schedule;

	// Flags and counters
	private bool checkingAgents = false;
	private int RandomFrames;

	// Components and systems
	public NPCMovement mover;
	public NPCActioner actioner;
	public NPCVitality vitality;
	public NPCInventory inventory;
	public NPCEconomy economy;
	public NPCSocial social;

	// Interaction with other agents
	public NPC agentOfInterest;

	// Conversation related
	private float conversationDuration;
	private bool isTalking;

	// Methods to set components
	public void SetMover(NPCMovement m) => mover = m;
	public void SetActioner(NPCActioner a) => actioner = a;
	public void SetVitality(NPCVitality v) => vitality = v;
	public void SetInventory(NPCInventory i) => inventory = i;
	public void SetEconomy(NPCEconomy e) => economy = e;
	public void SetSocial(NPCSocial s) => social = s;
	void Start() {
		if (agent == null) agent = new Agent();
		RandomFrames = UnityEngine.Random.Range(0, 60);
		mover = GetComponent<NPCMovement>();
		dynamicDisableable = GetComponent<DynamicDisableable>();
		LoadAgentData(gameObject.name);

		agent.UpdateBelief("Outside",true);
	}


	void Update() {
		HandleDebugInput();
		UpdateAgent();
		UpdateAgentDetection();
		UpdatePlayerInteraction();
	}

	void HandleDebugInput() {
		if (Input.GetKeyDown(KeyCode.P)) {
			agent.AddDesire(new Desire("InWork", true));
		}
		if (Input.GetKeyDown(KeyCode.O)) {
			agent.AddDesire(new Desire("InHome", true));
		}
	}

	void UpdateAgent() {
		agent.Update(Time.deltaTime);
	}

	void UpdateAgentDetection() {
		if (!checkingAgents) {
			checkingAgents = true;
			StartCoroutine(DelayedFunction(5 + RandomFrames, () => {
				DetectOtherAgents(2f);
				checkingAgents = false;
			}));
		}
	}

	void UpdatePlayerInteraction() {
		if (dynamicDisableable.visible) {
			CheckForPlayer(1f);
			ResumeMovementIfPlayerIsGone(1f);
		}
	}

	public IEnumerator DelayedFunction(int frameDelay, System.Action function) {
		for (int i = 0; i < frameDelay; i++) {
			yield return null;
		}
		function.Invoke();
	}

	public void Test() {
		PrepareTestingArea();

		//ActionGraph.MakePlan(ST.GetID("Hungry"),false,agent);
		agent.AddDesire(new Desire("InWork", true));

		SimulateUpdate(1000);
	}

	private void PrepareTestingArea() {
		if (Utils.WorldState.worldstate == null) Utils.WorldState.SetUp();
		ST.Destroy();
		ActionGraph.Destroy();
		LoadAgentData(gameObject.name);
	}

	private void SimulateUpdate(int loops = 10) {
		int counter = 0;
		while (counter < loops) {
			counter++;
			agent.Update(Time.deltaTime);
		}
	}
	public State SetAgentOfInterest(int id = 0) {
		List<NPC> availableNPCs = FindAvailableNPCs();

		if (availableNPCs.Count > 0) {
			NPC newAgent = SelectRandomNPC(availableNPCs);

			SetNewAgentOfInterest(newAgent);
			agent.UpdateBelief("HasAgentOfInterest", true);
			return State.Success;
		} else {
			HandleNoAvailableNPCs();
			agent.UpdateBelief("HasAgentOfInterest", false);
			return State.Failed;
		}
	}

	List<NPC> FindAvailableNPCs() {
		NPC[] allNPCs = FindObjectsOfType<NPC>();
		List<NPC> availableNPCs = new List<NPC>();

		foreach (NPC npc in allNPCs) {
			if (npc != this) {
				availableNPCs.Add(npc);
			}
		}

		return availableNPCs;
	}

	NPC SelectRandomNPC(List<NPC> npcList) {
		int randomIndex = UnityEngine.Random.Range(0, npcList.Count);
		return npcList[randomIndex];
	}

	void SetNewAgentOfInterest(NPC newAgent) {
		agentOfInterest = newAgent;
		Debug.Log("Agent of interest has been set to NPC: " + newAgent.name);
	}

	void HandleNoAvailableNPCs() {
		Debug.Log("Error: No other NPCs found in the scene.");
	}


	public State TalkTo(int id = 0) {
		if (agentOfInterest == null) {
			HandleNoAgentOfInterest();
			return State.Failed;
		}

		if (!isTalking) {
			StartConversation();
		}

		UpdateConversationDuration();

		if (conversationDuration <= 0f) {
			EndConversation();
			return State.Success;
		}

		return State.Running;
	}

	void HandleNoAgentOfInterest() {
		Debug.Log("Error: agentOfInterest is not set.");
	}

	void StartConversation() {
		isTalking = true;
		conversationDuration = 1.0f;
		Debug.Log("Started talking to " + agentOfInterest.name);
	}

	void UpdateConversationDuration() {
		conversationDuration -= Time.deltaTime;
	}

	void EndConversation() {
		isTalking = false;
		Debug.Log("Finished talking to " + agentOfInterest.name);
	}

	public State BuyFood(int id = 0) {
		print("buy food "+ST.GetString(id));
		return State.Success;
	}
	public State Work(int id = 0) {
		print("work");
		return State.Success;
	}

	public State Eat(int id = 0) {
		print("eat");
		return State.Success;
	}

	public State WalkTo(int id = 0) {
		Debug.Log("walking to "+ id);

		int node = SetNode(id);

		if (mover.LastNode.id_num == node) {
			return ArrivedAtDestination(ST.GetString(id));
		}

		if(!mover.walking || mover.targetNode != node) {
			mover.GetPath(node);
		}

		return State.Running;
	}

	private int SetNode(int id) {
		int node;
		if (id == ST.GetID("Home")) {
			node = ST.GetID(homeAddress);
		} else if (id == ST.GetID("Work")) {
			node = ST.GetID(workAddress);
		} else {
			node = id;
		}
		return node;
	}

	private State ArrivedAtDestination(string destination) {
		string placeBelief = "At" + destination;
		agent.UpdateBelief(placeBelief, true);
		return State.Success;
	}

	public State Enter(int id = 0) {
		Debug.Log("entering to " + id);
		transform.GetChild(0).gameObject.SetActive(false);
		agent.UpdateBelief("Outside", false);

		return State.Success;
	}

	public State Exit(int id = 0) {
		transform.GetChild(0).gameObject.SetActive(true);
		agent.UpdateBelief("Outside", true);
		return State.Success;
	}

	public void Request(string fact, bool state, string utility, params (int,bool)[] preconditions) {
		int key = ST.GetID(fact);
		if (!agent.desires.ContainsKey(key)) {
			agent.desires.Insert(key, new Desire(fact,state,ST.GetID(utility),preconditions));
		}
	}

	public void InitializeActions(Agent agent) {
		agent.InitializeActions(Application.dataPath + "/ActionData/ActionFile.json");
		// Get all the NPC methods that return a State and have no parameters
		Dictionary<string, Func<int, State>> npcActions = GetAllMethods();

		int[] keys = ActionGraph.GetActionKeys(Application.dataPath + "/ActionData/ActionFile.json");

		ApplyNpcActionsToAgent(agent, npcActions, keys);
	}

	private void ApplyNpcActionsToAgent(Agent agent, Dictionary<string, Func<int, State>> npcActions, int[] keys) {
		foreach (int key in keys) {
			string functionName = ST.GetString(ActionGraph.instance.GetAction(key).function);
			if (npcActions.TryGetValue(functionName, out Func<int, State> npcAction)) {
				agent.actions[key] = npcAction;
			}
		}
	}


	private Dictionary<string, Func<int, State>> GetAllMethods() {
		Dictionary<string, Func<int, State>> npcActions = new Dictionary<string, Func<int, State>>();
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo[] methods = GetType().GetMethods(bindingFlags);

		foreach (MethodInfo method in methods) {
			if (IsValidMethod(method)) {
				Func<int, State> func = CreateDelegateForMethod(method);
				npcActions[method.Name] = func;
			}
		}

		return npcActions;
	}

	bool IsValidMethod(MethodInfo method) {
		return method.ReturnType == typeof(State) &&
			   method.GetParameters().Length == 1 &&
			   method.GetParameters()[0].ParameterType == typeof(int);
	}

	Func<int, State> CreateDelegateForMethod(MethodInfo method) {
		return (Func<int, State>)Delegate.CreateDelegate(typeof(Func<int, State>), this, method);
	}


	public void CreateBaseBeliefs(string home, string workPlace) {
		//agent.CreateMentalState(Address.Home, Modality_enum.Belief, home);
		//agent.CreateMentalState(Address.Work, Modality_enum.Belief, workPlace);

	}

	public void SaveAgentData(string name) {
		string path = GetAgentDataPath(name);
		SerializeAgentData(path);
	}

	string GetAgentDataPath(string name) {
		return Application.dataPath + "/NPCdata/" + name + "/agentData" + name + ".data";
	}

	void SerializeAgentData(string path) {
		BinaryFormatter formatter = new BinaryFormatter();
		FileStream stream = new FileStream(path, FileMode.Create);

		formatter.Serialize(stream, agent);
		stream.Close();
	}

	public void LoadAgentData(string name) {
		LoadAgent(name);
		LoadScheduleAndInitializeActions(name);
	}

	void LoadAgent(string name) {
		string agentDataPath = GetAgentDataPath(name);

		if (File.Exists(agentDataPath)) {
			BinaryFormatter formatter = new BinaryFormatter();
			FileStream stream = new FileStream(agentDataPath, FileMode.Open);
			agent = formatter.Deserialize(stream) as GOBEN.Agent; // 203
			stream.Close();
		}

		agent.LoadNewAgent();
	}

	void LoadScheduleAndInitializeActions(string name) {
		schedule = LoadSchedule(name);
		InitializeActions(agent);
	}

	private Schedule LoadSchedule(string name) {
		string schedulePath = GetSchedulePath(name);

		if (File.Exists(schedulePath)) {
			string json = File.ReadAllText(schedulePath);
			return DeserializeSchedule(json);
		} else {
			return CreateNewSchedule(name);
		}
	}

	string GetSchedulePath(string name) {
		return Application.dataPath + "/NPCdata/" + name + "/" + name + "_Schedule.json";
	}

	Schedule DeserializeSchedule(string json) {
		return JsonConvert.DeserializeObject<Schedule>(json);
	}

	Schedule CreateNewSchedule(string name) {
		return new Schedule(name);
	}
	public void DetectOtherAgents(float detectionRange) {
		int otherAgentsLayerMask = 1 << LayerMask.NameToLayer("Agent");
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, otherAgentsLayerMask);

		closeAgents.Clear();

		for (int i = 0; i < hitColliders.Length; i++) {
			if (hitColliders[i].gameObject != gameObject) {
				NPC tmp;
				hitColliders[i].gameObject.TryGetComponent(out tmp);

				if (tmp != null) closeAgents.Add(tmp.agent);
			}
		}
	}

	public void CheckForPlayer(float detectionRadius) {
		int playerLayerMask = 1 << LayerMask.NameToLayer("Player");

		// Calculate the position and direction of the sphere cast
		Vector3 sphereOrigin = transform.position + mover.forwardVector * detectionRadius;

		// Perform the sphere cast to detect the player
		Collider[] hits = Physics.OverlapSphere(sphereOrigin, detectionRadius, playerLayerMask);
		foreach (Collider hit in hits) {
			Player player = hit.gameObject.GetComponent<Player>();
			if (player != null && !closeAgents.Contains(player.agent)) {
				closeAgents.Add(player.agent);

				// stop the agent's mover
				mover.Stop();

				// do something else if the player is detected, e.g., call a method on the player
			}
		}
	}


	public void ResumeMovementIfPlayerIsGone(float detectionRadius) {
		if (mover.stopped && closeAgents.Contains(GR.Player.agent)) {
			// Player was in closeAgents list
			int playerLayerMask = 1 << LayerMask.NameToLayer("Player");

			// Calculate the position of the sphere check
			Vector3 sphereOrigin = transform.position + mover.forwardVector * detectionRadius;

			// Check if the player is within the sphere
			if (!Physics.CheckSphere(sphereOrigin, detectionRadius, playerLayerMask)) {
				// Player is no longer in the vicinity
				closeAgents.Remove(GR.Player.agent);
				mover.stopped = false;
			}
		}
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.red;

		// Calculate the position of the sphere check
		if (mover != null) {
			Vector3 sphereOrigin = transform.position + mover.forwardVector * 1f;

			// Draw a wire sphere at the sphere check position
			Gizmos.DrawWireSphere(sphereOrigin, 1f);
		}
	}


}
