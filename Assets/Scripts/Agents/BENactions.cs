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

		public OrderedMap<Effect> preconditions;
		public OrderedMap<Effect> effects;
		public string name;
		public int[] connections;
		public int actionID;
		public int utilityBelief;
		protected Func<State> action;

		public Action() {
			this.preconditions = new OrderedMap<Effect>();
		}

		public Action(int preconditionid, Effect precondition, int effectid, Effect effect) {
			this.preconditions = new OrderedMap<Effect>();
			this.preconditions.Insert(preconditionid, precondition);
			this.effects = new OrderedMap<Effect>();
			this.effects.Insert(effectid, effect);
		}

		public State Tick() {
			return action.Invoke();
		}

		public virtual bool CheckPreconditions(Agent agent) {
			while (!preconditions.CompletedList()) {
				int ID = preconditions.GetCurrentKey();
				Effect PreconditionItem = preconditions.GetCurrentValue();

				if (!Compare(agent.beliefs.Cycle(ID).value, PreconditionItem)) {
					preconditions.ResetPointer();
					agent.beliefs.ResetPointer();
					return false;
				} else {
					preconditions.Advance();
				}
			}

			preconditions.ResetPointer();
			agent.beliefs.ResetPointer();
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
			newAction.utilityBelief = SymbolTable.GetID(fileAction.utilityBelief);
			newAction.actionID = fileAction.actionID;
			newAction.connections = fileAction.connections;

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

		public static int[] GOAP(int id, Action.Effect effect, Agent agent) {
			int[] desires = instance.desireDictionary.GetReferences(id, effect).ToArray();

			float weightUtility = 1.0f; // Adjust this weight to balance utility

			Dictionary<int, float> fScore = new Dictionary<int, float>();
			Dictionary<int, int> cameFrom = new Dictionary<int, int>();

			foreach (int actionId in desires) {
				fScore[actionId] = weightUtility * GetUtility(actionId, agent);
			}

			while (desires.Length > 0) {
				int current = GetActionWithHighestFScore(desires, fScore);

				if (instance.actions[current].CheckPreconditions(agent)) {
					return ReconstructPath(cameFrom, current);
				}

				desires = desires.Where(a => a != current).ToArray();

				foreach (int actionId in instance.actions[current].connections) {
					float utility = GetUtility(actionId, agent);

					if (!fScore.ContainsKey(actionId) || utility > fScore[actionId]) {
						cameFrom[actionId] = current;
						fScore[actionId] = utility;
					}
				}
			}


			throw new Exception("No plan found. Unable to achieve the desired effect.");
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
			return (float)agent.beliefs[instance.actions[actionId].utilityBelief].value;
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