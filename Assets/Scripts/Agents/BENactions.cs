using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Utils;

namespace GOBEN {
	public enum State {
		Inactive,
		Running,
		Success,
		Failed
	}
	[Serializable]
	public class Action {
		public OrderedMap<bool> environmentalPreconditions;
		public OrderedMap<bool> preconditions;
		public OrderedMap<bool> effects;
		public string name;
		public int[] connections;
		public int actionID;
		public int function;
		public int utilityBelief;
		public int parameterID;
		protected Func<int, State> action;

		public Action() {
			this.preconditions = new OrderedMap<bool>();
			this.environmentalPreconditions = new OrderedMap<bool>();
		}

		public Action(int preconditionid, bool precondition, int effectid, bool effect) {
			this.preconditions = new OrderedMap<bool>();
			this.environmentalPreconditions = new OrderedMap<bool>();
			this.preconditions.Insert(preconditionid, precondition);
			this.effects = new OrderedMap<bool>();
			this.effects.Insert(effectid, effect);
		}

		public virtual State Tick(int parameter_id = 0) {
			return action.Invoke(parameter_id);
		}

		public virtual State Tick(Agent agent, int parameter_id = 0) {
			return action.Invoke(parameter_id);
		}
		public virtual State Tick(Agent agent, float deltaTime, int parameter_id = 0) {
			return action.Invoke(parameter_id);
		}

		public virtual bool CheckPreconditions(Agent agent) {
			if (CheckEnvironmentalPreconditions()) {
				return CheckLocalPreconditions(agent);
			}
			return false;
		}

		public virtual bool CheckEnvironmentalPreconditions() {
			if (environmentalPreconditions.Count > 0) {
				while (!environmentalPreconditions.CompletedList()) {
					int ID = environmentalPreconditions.GetCurrentKey();
					bool PreconditionItem = environmentalPreconditions.GetCurrentValue();
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
			if (preconditions.Count > 0) {
				while (!preconditions.CompletedList()) {
					int ID = preconditions.GetCurrentKey();
					bool PreconditionItem = preconditions.GetCurrentValue();
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

		private static bool Compare(object check, bool op) {
			return (bool)check == op;
		}
	}

	[Serializable]
	public class CompositeAction : Action {
		public List<Action> subActions;

		public CompositeAction() {
			subActions = new List<Action>();
		}

		public CompositeAction(int preconditionid, bool precondition, int effectid, bool effect)
			: base(preconditionid, precondition, effectid, effect) {
			subActions = new List<Action>();
		}

		public override bool CheckPreconditions(Agent agent) {
			if (CheckEnvironmentalPreconditions()) {
				return CheckLocalPreconditions(agent) && CheckSubActionsPreconditions(agent);
			}
			return false;
		}

		private bool CheckSubActionsPreconditions(Agent agent) {
			foreach (Action subAction in subActions) {
				if (!subAction.CheckPreconditions(agent)) {
					return false;
				}
			}
			return true;
		}

		public override State Tick(int parameter_id = 0) {
			State result = base.Tick(parameter_id);
			if (result != State.Failed) {
				ExecuteSubActions(parameter_id);
			}
			return result;
		}

		private void ExecuteSubActions(int parameter_id) {
			foreach (Action subAction in subActions) {
				subAction.Tick(parameter_id);
			}
		}
	}

	[Serializable]
	public class SequenceAction : CompositeAction {
		private int currentActionIndex;

		public SequenceAction() {
			currentActionIndex = 0;
		}

		public SequenceAction(int preconditionid, bool precondition, int effectid, bool effect)
			: base(preconditionid, precondition, effectid, effect) {
			currentActionIndex = 0;
		}

		public override State Tick(int parameter_id = 0) {
			if (currentActionIndex < subActions.Count) {
				Action currentAction = subActions[currentActionIndex];
				State result = currentAction.Tick(parameter_id);
				if (result != State.Inactive) // Assuming State.Inactive represents a non-executable state
				{
					if (result == State.Success) {
						currentActionIndex++;
						if (currentActionIndex >= subActions.Count) {
							return State.Success;
						}
					} else if (result == State.Failed) {
						return State.Failed;
					}
				}
			}
			currentActionIndex = 0;
			return State.Inactive;
		}
	}

	[Serializable]
	public class ParallelAction : CompositeAction {
		private List<State> subActionStates;

		public ParallelAction() {
			subActionStates = new List<State>();
		}

		public ParallelAction(int preconditionid, bool precondition, int effectid, bool effect)
			: base(preconditionid, precondition, effectid, effect) {
			subActionStates = new List<State>();
		}

		public override bool CheckPreconditions(Agent agent) {
			bool allPreconditionsMet = base.CheckPreconditions(agent);
			if (allPreconditionsMet) {
				InitializeSubActionStates();
			}
			return allPreconditionsMet;
		}

		private void InitializeSubActionStates() {
			subActionStates.Clear();
			for (int i = 0; i < subActions.Count; i++) {
				subActionStates.Add(State.Inactive);
			}
		}

		public override State Tick(int parameter_id = 0) {
			bool allActionsComplete = true;

			for (int i = 0; i < subActions.Count; i++) {
				if (subActionStates[i] != State.Success && subActionStates[i] != State.Failed) {
					Action currentAction = subActions[i];
					State result = currentAction.Tick(parameter_id);
					subActionStates[i] = result;

					if (result != State.Success) {
						allActionsComplete = false;
					}
				}
			}

			if (allActionsComplete) {
				return State.Success;
			}

			return State.Running;
		}
	}

	[Serializable]
	public class FuzzySelector : CompositeAction {
		public override State Tick(Agent agent, int parameter_id = 0) {
			Action selectedAction = null;
			float highestUtility = int.MinValue;

			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					float utility = CalculateUtility(agent, subAction);
					if (utility > highestUtility) {
						highestUtility = utility;
						selectedAction = subAction;
					}
				}
			}

			if (selectedAction != null) {
				State result = selectedAction.Tick(parameter_id);
				return result != State.Inactive ? result : State.Failed;
			}

			return State.Failed;
		}

		private float CalculateUtility(Agent agent, Action action) {
			return (float)agent.beliefs[action.utilityBelief].value;
		}
	}

	[Serializable]
	public class Selector : CompositeAction {
		public override State Tick(Agent agent, int parameter_id = 0) {
			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					State result = subAction.Tick(parameter_id);
					if (result == State.Failed) {
						// Try the next action if the current one failed
						continue;
					} else {
						return result;
					}
				}
			}
			return State.Failed;
		}
	}

	[Serializable]
	public class SequenceRepeater : CompositeAction {
		private int repeatCount;
		private int currentCount;

		public SequenceRepeater(int count) {
			repeatCount = count;
			currentCount = 0;
		}

		public override State Tick(Agent agent, int parameter_id = 0) {
			if (currentCount < repeatCount) {
				Action currentAction = subActions[currentCount];
				State result = currentAction.Tick(parameter_id);
				if (result == State.Success) {
					currentCount++;
					if (currentCount >= subActions.Count) {
						currentCount = 0;
						return State.Success;
					}
				} else if (result == State.Failed) {
					currentCount = 0;
					return State.Failed;
				}
			} else {
				currentCount = 0;
				return State.Success;
			}

			return State.Running;
		}
	}

	[Serializable]
	public class ParallelRepeater : CompositeAction {
		private float duration;
		private float elapsedTime;

		public ParallelRepeater(float duration) {
			this.duration = duration;
			elapsedTime = 0f;
		}

		public override State Tick(Agent agent, float deltaTime, int parameter_id = 0) {
			if (elapsedTime < duration) {
				elapsedTime += deltaTime;

				foreach (Action subAction in subActions) {
					if (subAction.CheckPreconditions(agent)) {
						State result = subAction.Tick(parameter_id);
						if (result == State.Success) {
							elapsedTime = 0f;
							return State.Success;
						}
					}
				}

				return State.Running;
			} else {
				elapsedTime = 0f;
				return State.Success;
			}
		}
	}

	[Serializable]
	public class ParallelSequence : CompositeAction {
		public override State Tick(Agent agent, int parameter_id = 0) {
			bool allActionsComplete = true;

			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					State result = subAction.Tick(parameter_id);
					if (result != State.Success) {
						allActionsComplete = false;
					}
				}
			}

			if (allActionsComplete) {
				return State.Success;
			}

			return State.Running;
		}
	}

	[Serializable]
	public class FallbackSelector : CompositeAction {
		public override State Tick(Agent agent, int parameter_id = 0) {
			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					State result = subAction.Tick(parameter_id);
					return result;
				}
			}

			return State.Failed;
		}
	}

	[Serializable]
	public class TimeLimitedSelector : CompositeAction {
		private float timeLimit;
		private float elapsedTime;

		public TimeLimitedSelector(float limit) {
			timeLimit = limit;
			elapsedTime = 0f;
		}

		public override State Tick(Agent agent, float deltaTime, int parameter_id = 0) {
			if (elapsedTime < timeLimit) {
				elapsedTime += deltaTime;

				foreach (Action subAction in subActions) {
					if (subAction.CheckPreconditions(agent)) {
						State result = subAction.Tick(parameter_id);
						return result;

					}
				}

				return State.Running;
			} else {
				elapsedTime = 0f;
				return State.Failed;
			}
		}
	}

	[Serializable]
	public class RandomSelector : CompositeAction {
		private Random random;

		public RandomSelector() {
			random = new Random();
		}

		public override State Tick(Agent agent, int parameter_id = 0) {
			List<Action> availableActions = subActions.FindAll(subAction => subAction.CheckPreconditions(agent));
			if (availableActions.Count > 0) {
				int randomIndex = random.Next(0, availableActions.Count);
				Action selectedAction = availableActions[randomIndex];
				return selectedAction.Tick(parameter_id);
			}

			return State.Failed;
		}
	}

	[Serializable]
	public class Norm {
		public string name;
		public string intentionId;
		public string contextId;
		public bool contextEnabled;
		public float obedienceThreshold;
		public float priority;
		public int[] behavior;
		public float violationTime;

		public Norm() {
			// Default constructor
		}

		public Norm(string name, string intentionId, string contextId, bool contextEnabled, float obedienceThreshold, float priority, int[] behavior, float violationTime) {
			this.name = name;
			this.intentionId = intentionId;
			this.contextId = contextId;
			this.contextEnabled = contextEnabled;
			this.obedienceThreshold = obedienceThreshold;
			this.priority = priority;
			this.behavior = behavior;
			this.violationTime = violationTime;
		}
	}

	[Serializable]
	public class SerializableFileNorms {
		public List<Norm> norms;

		public SerializableFileNorms(List<Norm> norms) {
			this.norms = norms;
		}
	}

	[Serializable]
	public class ActionGraph {
		public static ActionGraph instance;
		private Dictionary<int, Action> actions;
		public ActionReferences desireDictionary;
		public Dictionary<int, Norm> Norms;

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
			Norms = new Dictionary<int, Norm>();
		}


		public ActionGraph() {
			actions = new Dictionary<int, Action>();
			Norms = new Dictionary<int, Norm>();
		}


		public void Insert(Action action) {
			if (desireDictionary == null) desireDictionary = new ActionReferences();
			int actionID = SymbolTable.GetID(action.name);
			actions[actionID] = action;

			for (int i = 0; i < action.effects.Count; i++) {
				desireDictionary.AddReference(action.effects.GetKey(i), action.effects[i], actionID);
			}
		}

		public void Remove(Action action) {
			int actionID = SymbolTable.GetID(action.name);
			actions.Remove(actionID);

			for (int i = 0; i < action.effects.Count; i++) {
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

			// Extract the substring before the underscore
			int underscoreIndex = fileAction.name.IndexOf('_');
			string extractedName = (underscoreIndex >= 0)
				? fileAction.name.Substring(0, underscoreIndex)
				: fileAction.name;

			newAction.name = fileAction.name;
			newAction.function = SymbolTable.GetID(extractedName);

			// Extract the second part as a parameter
			string parameter = (underscoreIndex >= 0 && underscoreIndex < fileAction.name.Length - 1)
				? fileAction.name.Substring(underscoreIndex + 1)
				: string.Empty;

			newAction.parameterID = parameter != string.Empty ? SymbolTable.GetID(parameter) : 0;

			newAction.utilityBelief = fileAction.utilityBelief != "" ? SymbolTable.GetID(fileAction.utilityBelief) : 0;
			newAction.actionID = fileAction.actionID;
			newAction.connections = fileAction.connections;

			// Translate environmentalpreconditions
			for (int i = 0; i < fileAction.environmentalPreconditions.Length; i++) {
				int preconditionID = SymbolTable.GetID(fileAction.environmentalPreconditions[i].key);
				bool preconditionEffect = (bool)fileAction.environmentalPreconditions[i].op;
				newAction.environmentalPreconditions.Insert(preconditionID, preconditionEffect);
			}

			// Translate preconditions
			for (int i = 0; i < fileAction.preconditions.Length; i++) {
				int preconditionID = SymbolTable.GetID(fileAction.preconditions[i].key);
				bool preconditionEffect = (bool)fileAction.preconditions[i].op;
				newAction.preconditions.Insert(preconditionID, preconditionEffect);
			}

			// Translate effects
			for (int i = 0; i < fileAction.effects.Length; i++) {
				int effectID = SymbolTable.GetID(fileAction.effects[i].key);
				bool effectEffect = (bool)fileAction.effects[i].op;
				if (newAction.effects == null) {
					newAction.effects = new OrderedMap<bool>();
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

		public static void MakePlan(int id, bool effect, Agent agent) {
			int[] actionIndex = instance.GOAP(id, effect, agent);
			if (actionIndex != null) {
				foreach (int i in actionIndex)
					agent.actionStack.AddAction(i);
			}
		}

		//possible optimization: create a queue of requests, run a request every frame, no more, 
		//use a single static container to avoid realocating memory every query
		private int[] GOAP(int id, bool effect, Agent agent) {
			int[] desires = instance.desireDictionary.GetReferences(id, effect).ToArray();
			Queue<int> actionQueue = new Queue<int>();
			HashSet<int> visited = new HashSet<int>();
			Dictionary<int, int> parent = new Dictionary<int, int>();

			foreach (int i in desires) {
				actionQueue.Enqueue(i);
				visited.Add(i);
			}

			while (actionQueue.Count > 0) {
				int current = actionQueue.Dequeue();
				if (actions[current].CheckPreconditions(agent)) {
					List<int> plan = new List<int>();
					while (current != -1) {
						plan.Insert(0, current);
						current = parent.ContainsKey(current) ? parent[current] : -1;
					}
					return plan.ToArray();
				}
				//using symbol table id instead of action id
				foreach (int neighbor in actions[current].connections) {
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

		public static float GetUtility(int actionId, Agent agent) {
			Belief belief;
			if (agent.beliefs.TryGetValue(instance.actions[actionId].utilityBelief, out belief))
				return (float)belief.value;
			else
				return 0f;
		}

		public Action GetAction(int id) {
			return instance.actions[id];
		}


		public Norm GetNormPlan(int intentionId, float obedience) {
			if (Norms.TryGetValue(intentionId, out Norm norm)) {
				if (obedience >= norm.obedienceThreshold) {
					return norm;
				}
			}
			return null;
		}
	}

	[Serializable]
	public class ActionReferences {
		private Dictionary<(int, bool), List<int>> references;

		public ActionReferences() {
			references = new Dictionary<(int, bool), List<int>>();
		}

		public void AddReference(int worldStateID, bool effect, int actionID) {
			var key = (worldStateID, effect);
			if (!references.ContainsKey(key)) {
				references[key] = new List<int>();
			}
			references[key].Add(actionID);
		}

		public void RemoveReference((int, bool) key, int actionID) {
			if (references.ContainsKey(key)) {
				references[key].Remove(actionID);
			}
		}

		public List<int> GetReferences(int worldStateID, bool effect) {
			var key = (worldStateID, effect);
			return references.ContainsKey(key) ? references[key] : new List<int>();
		}


	}

	[Serializable]
	public class FileActionWrapper {
		public FileAction[] actions;
	}

	[Serializable]
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
		public bool CanDoAction() {
			if (actionIds.Count == 0) {
				return false;
			}

			int currentActionId = actionIds.Peek();
			Action currentAction = ActionGraph.instance.GetAction(currentActionId);
			if (!currentAction.CheckPreconditions(agent)) {
				return false;
			}

			return true;
		}

		public State Tick() {
			if (actionIds.Count == 0) {
				return State.Inactive;
			}

			int currentActionId = actionIds.Peek();
			Func<int, State> currentAction = agent.actions[currentActionId];

			State state = currentAction.Invoke(ActionGraph.instance.GetAction(currentActionId).parameterID);

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

