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
using BEN;


public class NPC : MonoBehaviour {

	public Agent agent { get; set; }

	public string homeAddress = "";
	public string workAddress = "";
	public int money = 0;
	[HideInInspector] public List<Agent> closeAgents;
	[HideInInspector] public DynamicDisableable dynamicDisableable;
	[HideInInspector] public Schedule schedule;
	private bool checkingAgents = false;
	private int RandomFrames;

	public NPCMovement mover;
	public NPCActioner actioner;
	public NPCVitality vitality;
	public NPCInventory inventory;
	public NPCEconomy economy;
	public NPCSocial social;

	public void SetMover(NPCMovement m) => mover = m;
	public void SetActioner(NPCActioner a) => actioner = a;
	public void SetVitality(NPCVitality v) => vitality = v;
	public void SetInventory(NPCInventory i) => inventory = i;
	public void SetEconomy(NPCEconomy e) => economy = e;
	public void SetSocial(NPCSocial s) => social = s;

	void Start() {

		RandomFrames = UnityEngine.Random.Range(0, 60);
		mover = GetComponent<NPCMovement>();
		dynamicDisableable = GetComponent<DynamicDisableable>();
		LoadAgentData(gameObject.name);
		

	}


	void Update() {
		//if (mover.path != null && mover.path.Count == 0) {
		//	if (mover.LastNode.id == (string)agent.GetBelief(Address.Home).value) {
		//		mover.GetPath((string)agent.GetBelief(Address.Work).value);
		//	} else {
		//		mover.GetPath((string)agent.GetBelief(Address.Home).value);
		//	}
		//}
		if (!checkingAgents) {
			checkingAgents = true;
			StartCoroutine(DelayedFunction(5 + RandomFrames, () => {
				DetectOtherAgents(2f);
				checkingAgents = false;
			}));
		}

		if (dynamicDisableable.visible) {
			CheckForPlayer(1f);
			ResumeMovementIfPlayerIsGone(1f);
		}
	}

	public void AddDesire(string desire) {

	}

	public IEnumerator DelayedFunction(int frameDelay, System.Action function) {
		for (int i = 0; i < frameDelay; i++) {
			yield return null;
		}
		function.Invoke();
	}

	public void Test() {
		if (Utils.WorldState.worldstate == null) Utils.WorldState.SetUp();
		ST.Destroy();
		ActionGraph.Destroy();
		LoadAgentData(gameObject.name);
		ActionGraph.MakePlan(ST.GetID("Hungry"),false,agent);

		int counter = 0;
		while (!agent.actionStack.empty && counter < 1000) {
			counter++;
			agent.actionStack.Tick();
		}
/*
		Agent agent = new Agent();
		agent.beliefs.Insert(SY("Healthy"),new Belief("Healthy",true));

		print(heal.CheckPreconditions(agent));
*/
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
		return State.Success;
		int node;
		if(id == ST.GetID("Home")) {
			node = ST.GetID(homeAddress);
		}else if(id == ST.GetID("Work")) {
			node = ST.GetID(workAddress);
		} else {
			node = id;
		}
		if (mover.LastNode.id_num == node) return State.Success;

		if(!mover.walking || mover.targetNode != node) {
			mover.GetPath(node);
		}

		return State.Running;
	}

	public void Request(string fact, State state, string utility, params (int,State)[] preconditions) {
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
			// Check if the method returns a State and has a single integer parameter
			if (method.ReturnType == typeof(State) && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(int)) {
				Func<int, State> func = (Func<int, State>)Delegate.CreateDelegate(typeof(Func<int, State>), this, method);
				npcActions[method.Name] = func;
			}
		}

		return npcActions;
	}

	



	public void CreateBaseBeliefs(string home, string workPlace) {
		//agent.CreateMentalState(Address.Home, Modality_enum.Belief, home);
		//agent.CreateMentalState(Address.Work, Modality_enum.Belief, workPlace);

	}

	public void SaveAgentData(string name) {
		string path = Application.dataPath + "/NPCdata/" + name + "/agentData" + name + ".data";

		BinaryFormatter formatter = new BinaryFormatter();
		FileStream stream = new FileStream(path, FileMode.Create);

		formatter.Serialize(stream, agent);
		stream.Close();
	}

	public void LoadAgentData(string name) {
		// Load agent data
		string agentDataPath = Application.dataPath + "/NPCdata/" + name + "/agentData" + name + ".data";
		if (File.Exists(agentDataPath)) {
			BinaryFormatter formatter = new BinaryFormatter();
			FileStream stream = new FileStream(agentDataPath, FileMode.Open);
			agent = formatter.Deserialize(stream) as BEN.Agent;
			stream.Close();
		}
		agent.LoadNewAgent();

		// Load schedule
		string schedulePath = Application.dataPath + "/NPCdata/" + name + "/" + name + "_Schedule.json";
		if (File.Exists(schedulePath)) {
			string json = File.ReadAllText(schedulePath);
			schedule = JsonConvert.DeserializeObject<Schedule>(json);
		} else {
			// Create a new schedule if the file doesn't exist
			schedule = new Schedule(name);
		}

		InitializeActions(agent);
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
