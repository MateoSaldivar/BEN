using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Utils;

namespace BEN {
	public enum State {
		Inactive,
		Running,
		Success,
		Failed
	}
	[Serializable]
	public class Action {
		public enum Effect {
			TRUE,
			FALSE,
			PLUS,
			MINUS
		}

		public OrderedMap<Effect> environmentalPreconditions;
		public OrderedMap<Effect> preconditions;
		public OrderedMap<Effect> effects;
		public string name;
		public int[] connections;
		public int actionID;
		public int utilityBelief;
		protected Func<int,State> action;

		public Action() {
			this.preconditions = new OrderedMap<Effect>();
			this.environmentalPreconditions = new OrderedMap<Effect>();
		}

		public Action(int preconditionid, Effect precondition, int effectid, Effect effect) {
			this.preconditions = new OrderedMap<Effect>();
			this.environmentalPreconditions = new OrderedMap<Effect>();
			this.preconditions.Insert(preconditionid, precondition);
			this.effects = new OrderedMap<Effect>();
			this.effects.Insert(effectid, effect);
		}

		public State Tick(int parameter_id = 0) {
			return action.Invoke(parameter_id);
		}
		public virtual bool CheckPreconditions(Agent agent) {
			if (CheckEnvironmentalPreconditions()) {
				return CheckLocalPreconditions(agent);
			}
			return false;
		}

		public virtual bool CheckEnvironmentalPreconditions() {
			if (environmentalPreconditions.length > 0) {
				while (!environmentalPreconditions.CompletedList()) {
					int ID = environmentalPreconditions.GetCurrentKey();
					Effect PreconditionItem = environmentalPreconditions.GetCurrentValue();
					object state = WorldState.worldstate.Cycle(ID);
					if (state == null || !Compare(state, PreconditionItem)) {
						environmentalPreconditions.ResetPointer();
						WorldState.worldstate.ResetPointer();
						return false;
					} else {
						environmentalPreconditions.Advance();
					}
				}
				environmentalPreconditions.ResetPointer();
				WorldState.worldstate.ResetPointer();
			}
			return true;
		}

		public virtual bool CheckLocalPreconditions(Agent agent) {
			if (preconditions.length > 0) {
				while (!preconditions.CompletedList()) {
					int ID = preconditions.GetCurrentKey();
					Effect PreconditionItem = preconditions.GetCurrentValue();
					Belief belief = agent.beliefs.Cycle(ID);
					if (belief == null || !Compare(belief.value, PreconditionItem)) {
						preconditions.ResetPointer();
						agent.beliefs.ResetPointer();
						return false;
					} else {
						preconditions.Advance();
					}
				}

				preconditions.ResetPointer();
				agent.beliefs.ResetPointer();
			}
			return true;
		}

		private static bool Compare(object check, Effect op) {
			if (op == Effect.TRUE && (bool)check) return true;
			if (op == Effect.FALSE && !(bool)check) return true;
			else return false;
		}
	}



	[Serializable]
	public class ActionGraph {
		public static ActionGraph instance;
		private Dictionary<int, Action> actions;
		public ActionReferences desireDictionary;


		public static int[] GetActionKeys(string jsonPath) {
			if (instance == null) {
				instance = new ActionGraph();
				instance.LoadActionsFromJson(jsonPath);
			}
			if (instance.actions == null) {
				instance.LoadActionsFromJson(jsonPath);
			}

			int[] keys = new int[instance.actions.Count];
			instance.actions.Keys.CopyTo(keys, 0);

			return keys;
		}

		public ActionGraph(Dictionary<int, Action> actions) {
			this.actions = actions;
		}


		public ActionGraph() {
			this.actions = new Dictionary<int, Action>();
		}


		public void Insert(Action action) {
			if (desireDictionary == null) desireDictionary = new ActionReferences();
			int actionID = SymbolTable.GetID(action.name);
			actions[actionID] = action;

			for (int i = 0; i < action.effects.length; i++) {
				desireDictionary.AddReference(action.effects.GetKey(i), action.effects[i], actionID);
			}
		}

		public void Remove(Action action) {
			int actionID = SymbolTable.GetID(action.name);
			actions.Remove(actionID);

			for (int i = 0; i < action.effects.length; i++) {
				desireDictionary.RemoveReference((action.effects.GetKey(i), action.effects[i]), actionID);
			}
		}
		public void LoadActionsFromJson(string jsonFilePath) {
			string json = File.ReadAllText(jsonFilePath);
			FileActionWrapper fileActionWrapper = JsonConvert.DeserializeObject<FileActionWrapper>(json);

			for (int i = 0; i < fileActionWrapper.actions.Length; i++) {
				Action newAction = TranslateFileActionToAction(fileActionWrapper.actions[i]);
				Insert(newAction);
			}
		}

		public static Action TranslateFileActionToAction(FileAction fileAction) {
			Action newAction = new Action();
			SymbolTable.GetID(fileAction.name);

			newAction.name = fileAction.name;
			
			newAction.utilityBelief = fileAction.utilityBelief != ""? SymbolTable.GetID(fileAction.utilityBelief):0;
			newAction.actionID = fileAction.actionID;
			newAction.connections = fileAction.connections;

			// Translate environmentalpreconditions
			for (int i = 0; i < fileAction.environmentalPreconditions.Length; i++) {
				int preconditionID = SymbolTable.GetID(fileAction.environmentalPreconditions[i].key);
				Action.Effect preconditionEffect = (Action.Effect)fileAction.environmentalPreconditions[i].op;
				newAction.environmentalPreconditions.Insert(preconditionID, preconditionEffect);
			}

			// Translate preconditions
			for (int i = 0; i < fileAction.preconditions.Length; i++) {
				int preconditionID = SymbolTable.GetID(fileAction.preconditions[i].key);
				Action.Effect preconditionEffect = (Action.Effect)fileAction.preconditions[i].op;
				newAction.preconditions.Insert(preconditionID, preconditionEffect);
			}

			// Translate effects
			for (int i = 0; i < fileAction.effects.Length; i++) {
				int effectID = SymbolTable.GetID(fileAction.effects[i].key);
				Action.Effect effectEffect = (Action.Effect)fileAction.effects[i].op;
				if (newAction.effects == null) {
					newAction.effects = new OrderedMap<Action.Effect>();
				}
				newAction.effects.Insert(effectID, effectEffect);
			}

			return newAction;
		}

		public static void Destroy() {
			if (instance != null) {
				instance.actions.Clear();
				instance.actions = null;
			}
			instance = null;
		}

		public static void MakePlan(int id, Action.Effect effect, Agent agent) {
			int[] actionIndex = instance.GOAP(id, effect, agent);
			foreach(int i in actionIndex)
				agent.actionStack.AddAction(i);
		}

		//possible optimization: create a queue of requests, run a request every frame, no more, 
		//use a single static container to avoid realocating memory every query
		private int[] GOAP(int id, Action.Effect effect, Agent agent) {
			int[] desires = instance.desireDictionary.GetReferences(id, effect).ToArray();
			Queue<int> actionQueue = new Queue<int>();
			HashSet<int> visited = new HashSet<int>();
			Dictionary<int, int> parent = new Dictionary<int, int>();

			foreach (int i in desires) {
				actionQueue.Enqueue(i);
				visited.Add(i);
			}

			while(actionQueue.Count > 0) {
				int current = actionQueue.Dequeue();
				if (actions[current].CheckPreconditions(agent)) {
					List<int> plan = new List<int>();
					while(current != -1) {
						plan.Insert(0, current);
						current = parent.ContainsKey(current) ? parent[current] : -1;
					}
					return plan.ToArray();
				}
				//using symbol table id instead of action id
				foreach(int neighbor in actions[current].connections) {
					int n_id = actions.ElementAt(neighbor).Key;
					if (!visited.Contains(n_id)) {
						actionQueue.Enqueue(n_id);
						visited.Add(n_id);
						parent.Add(n_id, current);
					}
				}
			}
			return null;
		}

		private static int GetActionWithHighestFScore(int[] actions, Dictionary<int, float> fScore) {
			int maxAction = actions[0];
			float maxFScore = fScore[maxAction];

			foreach (int actionId in actions) {
				if (fScore[actionId] > maxFScore) {
					maxAction = actionId;
					maxFScore = fScore[actionId];
				}
			}

			return maxAction;
		}

		private static int[] ReconstructPath(Dictionary<int, int> cameFrom, int current) {
			List<int> path = new List<int> { current };

			while (cameFrom.ContainsKey(current)) {
				current = cameFrom[current];
				path.Insert(0, current);
			}

			return path.ToArray();
		}

		private static float GetUtility(int actionId, Agent agent) {
			Belief belief;
			if (agent.beliefs.TryGetValue(instance.actions[actionId].utilityBelief, out belief))
				return (float)belief.value;
			else
				return 0f;
		}

	}

	[Serializable]
	public class ActionReferences {
		private Dictionary<(int, Action.Effect), List<int>> references;

		public ActionReferences() {
			references = new Dictionary<(int, Action.Effect), List<int>>();
		}

		public void AddReference(int worldStateID, Action.Effect effect, int actionID) {
			var key = (worldStateID, effect);
			if (!references.ContainsKey(key)) {
				references[key] = new List<int>();
			}
			references[key].Add(actionID);
		}

		public void RemoveReference((int, Action.Effect) key, int actionID) {
			if (references.ContainsKey(key)) {
				references[key].Remove(actionID);
			}
		}

		public List<int> GetReferences(int worldStateID, Action.Effect effect) {
			var key = (worldStateID, effect);
			return references.ContainsKey(key) ? references[key] : new List<int>();
		}
	}

	public class FileActionWrapper {
		public FileAction[] actions;
	}


	public class ActionStack {
		private Stack<int> actionIds;
		private Agent agent;
		public bool empty { get { return actionIds.Count == 0; } set { } }

		public ActionStack(Agent agent) {
			this.agent = agent;
			actionIds = new Stack<int>();
		}

		public void AddAction(int actionId) {
			actionIds.Push(actionId);
		}

		public State Tick() {
			if (actionIds.Count == 0) {
				return State.Inactive;
			}

			int currentActionId = actionIds.Peek();
			var currentAction = agent.actions[currentActionId];

			State state = agent.actions[currentActionId].Invoke();

			if (state == State.Success) {
				actionIds.Pop();
				return Tick(); // Call Tick again to process the next action immediately
			} else if (state == State.Failed) {
				actionIds.Clear();
			}

			return state;
		}
	}
}