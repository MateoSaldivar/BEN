using UnityEngine;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using AVA = AgentVariableAdjuster;
using SY = Utils.SymbolTable;
using E = BEN.Action.Effect;
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
		SY.Destroy();
		ActionGraph.Destroy();
		LoadAgentData(gameObject.name);
		ActionGraph.MakePlan(SY.GetID("Hungry"),E.FALSE,agent);

		int counter = 0;
		while (!agent.actionStack.empty && counter < 1000) {
			counter++;
			agent.actionStack.Tick();
		}
/*
		Agent agent = new Agent();
		agent.beliefs.Insert(SY("Healthy"),new Belief("Healthy",true));

		BEN.Action heal = new BEN.Action();
		heal.preconditions.Insert(SY("Healthy"), BEN.Action.Effect.TRUE);

		print(heal.CheckPreconditions(agent));
*/
	}

	public State BuyFood() {
		print("buy food");
		return State.Success;
	}
	public State Work() {
		print("work");
		return State.Success;
	}

	public State Eat() {
		print("eat");
		return State.Success;
	}
	public State WalkHome() {
		print("walking home");
		mover.GetPath(homeAddress);
		return State.Success;
	}
	public State WalkWork() {
		print("walkwork");
		return State.Success;
	}
	public State WalkGather() {
		print("walkgather");
		return State.Success;
	}


	public void InitializeActions(Agent agent) {
		agent.InitializeActions(Application.dataPath + "/ActionData/ActionFile.json");
		// Get all the NPC methods that return a State and have no parameters
		Dictionary<string, Func<State>> npcActions = GetAllMethods();

		int[] keys = ActionGraph.GetActionKeys(Application.dataPath + "/ActionData/ActionFile.json");

		foreach (int key in keys) {
			string actionName = SY.GetString(key);
			if (npcActions.TryGetValue(actionName, out Func<State> npcAction)) {
				agent.actions[key] = npcAction;
			}
		}
	}


	private Dictionary<string, Func<State>> GetAllMethods() {
		Dictionary<string, Func<State>> npcActions = new Dictionary<string, Func<State>>();
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		MethodInfo[] methods = GetType().GetMethods(bindingFlags);

		foreach (MethodInfo method in methods) {
			// Check if the method returns a State and has no parameters
			if (method.ReturnType == typeof(State) && method.GetParameters().Length == 0) {
				Func<State> func = (Func<State>)Delegate.CreateDelegate(typeof(Func<State>), this, method);
				npcActions[method.Name] = func;
			}
		}

		return npcActions;
	}

	//public void Test() {
	//	PlanLibrary.ClearPlans();
	//	Agent agent = new Agent();

	//	// Initialize the "isNight" and "isDay" beliefs
	//	agent.AddBelief("isNight", true);

	//	// Add the "GoToSleep" and "GoToWork" desires
	//	agent.AddDesire(new Desire("intention1", "isNight"));

	//	Plan plan = new Plan(
	//		"Name", "intention1",
	//		() => MoveTo("Home"),
	//		() => MoveTo("Work"),
	//		() => MoveTo("Bed")
	//		);
	//	PlanLibrary.AddPlan(plan);

	//	// Update the agent's beliefs and execute the current plan until it has completed
	//	int loopCount = 0;
	//	while (agent.Update(Time.deltaTime) != BEN.State.Inactive && loopCount < 1000) loopCount++;


	//}

	//int tmp = 3;
	//public BEN.State MoveTo(string location) {
	//	print("Moving to " + location + " progress: " + tmp);

	//	tmp--;
	//	if (tmp == 0) {
	//		tmp = 3;
	//		return BEN.State.Success;
	//	} else {
	//		return BEN.State.Running;
	//	}
	//}



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
		string path = Application.dataPath + "/NPCdata/" + name + "/agentData" + name + ".data";
		if (File.Exists(path)) {
			BinaryFormatter formatter = new BinaryFormatter();
			FileStream stream = new FileStream(path, FileMode.Open);
			agent = formatter.Deserialize(stream) as BEN.Agent;
			stream.Close();
		}
		agent.LoadNewAgent();
		
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
		if (mover.stopped && closeAgents.Contains(Player.instance.agent)) {
			// Player was in closeAgents list
			int playerLayerMask = 1 << LayerMask.NameToLayer("Player");

			// Calculate the position of the sphere check
			Vector3 sphereOrigin = transform.position + mover.forwardVector * detectionRadius;

			// Check if the player is within the sphere
			if (!Physics.CheckSphere(sphereOrigin, detectionRadius, playerLayerMask)) {
				// Player is no longer in the vicinity
				closeAgents.Remove(Player.instance.agent);
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
