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
			if (environmentalPreconditions.Count == 0) {
				return true; // No preconditions, so the check passes.
			}

			ResetPointers(); // Reset the pointers before starting the check.

			while (!environmentalPreconditions.CompletedList()) {
				int ID = environmentalPreconditions.GetCurrentKey();
				bool PreconditionItem = environmentalPreconditions.GetCurrentValue();
				object state = WorldState.worldstate.Cycle(ID);

				if (state == null || !Compare(state, PreconditionItem)) {
					ResetPointers(); // Reset the pointers before returning.
					return false; // Preconditions check failed.
				}

				environmentalPreconditions.Advance();
			}

			ResetPointers(); // Reset the pointers after completing the check.
			return true; // All preconditions are met.
		}

		private void ResetPointers() {
			environmentalPreconditions.ResetPointer();
			WorldState.worldstate.ResetPointer();
		}

		public virtual bool CheckLocalPreconditions(Agent agent) {
			if (preconditions.Count == 0) {
				return true; // No preconditions, so the check passes.
			}

			ResetPointers(agent); // Reset the pointers before starting the check.

			while (!preconditions.CompletedList()) {
				int ID = preconditions.GetCurrentKey();
				bool PreconditionItem = preconditions.GetCurrentValue();
				Belief belief = agent.beliefs.Cycle(ID);

				if (belief == null || !Compare(belief.value, PreconditionItem)) {
					ResetPointers(agent); // Reset the pointers before returning.
					return false; // Preconditions check failed.
				}

				preconditions.Advance();
			}

			ResetPointers(agent); // Reset the pointers after completing the check.
			return true; // All preconditions are met.
		}

		private void ResetPointers(Agent agent) {
			preconditions.ResetPointer();
			agent.beliefs.ResetPointer();
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
				return CheckActionState(result);
			}

			currentActionIndex = 0;
			return State.Inactive;
		}

		protected State CheckActionState(State result) {
			if (result != State.Inactive) {
				if (result == State.Success) {
					currentActionIndex++;
					if (currentActionIndex >= subActions.Count) {
						return State.Success;
					}
				}
			}
			return result;
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
			UpdateSubActionStates(parameter_id);
			return DetermineOverallState();
		}

		private void UpdateSubActionStates(int parameter_id) {
			for (int i = 0; i < subActions.Count; i++) {
				if (subActionStates[i] != State.Success && subActionStates[i] != State.Failed) {
					Action currentAction = subActions[i];
					State result = currentAction.Tick(parameter_id);
					subActionStates[i] = result;
				}
			}
		}

		private State DetermineOverallState() {
			bool allActionsComplete = subActionStates.All(state => state == State.Success);
			return allActionsComplete ? State.Success : State.Running;
		}
	}

	[Serializable]
	public class FuzzySelector : CompositeAction {
		public override State Tick(Agent agent, int parameter_id = 0) {
			Action selectedAction = FindBestAction(agent);

			if (selectedAction != null) {
				State result = selectedAction.Tick(parameter_id);
				return result != State.Inactive ? result : State.Failed;
			}

			return State.Failed;
		}

		private Action FindBestAction(Agent agent) {
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

			return selectedAction;
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
					if (result != State.Failed) {
						return result;
					}
					// Try the next action if the current one failed
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
				Action currentAction = GetNextSubAction();
				State result = ExecuteSubAction(currentAction, parameter_id);
				return HandleSubActionResult(result);
			} else {
				ResetCounter();
				return State.Success;
			}
		}

		private State HandleSubActionResult(State result) {
			if (result == State.Success) {
				currentCount++;
				if (currentCount >= subActions.Count) {
					ResetCounter();
					return State.Success;
				}
			} else if (result == State.Failed) {
				ResetCounter();
				return State.Failed;
			}

			return State.Running;
		}

		private Action GetNextSubAction() {
			return subActions[currentCount];
		}

		private State ExecuteSubAction(Action action, int parameter_id) {
			return action.Tick(parameter_id);
		}

		private void ResetCounter() {
			currentCount = 0;
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
				return RunSubActions(agent, parameter_id);
			} else {
				ResetElapsedTime();
				return State.Success;
			}
		}

		private State RunSubActions(Agent agent, int parameter_id) {
			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					State result = ExecuteSubAction(subAction, parameter_id);
					if (result == State.Success) {
						ResetElapsedTime();
						return State.Success;
					}
				}
			}

			return State.Running;
		}

		private State ExecuteSubAction(Action action, int parameter_id) {
			return action.Tick(parameter_id);
		}

		private void ResetElapsedTime() {
			elapsedTime = 0f;
		}

	}

	[Serializable]
	public class ParallelSequence : CompositeAction {
		public override State Tick(Agent agent, int parameter_id = 0) {
			bool allActionsComplete = AreAllSubActionsComplete(agent, parameter_id);

			if (allActionsComplete) {
				return State.Success;
			}

			return State.Running;
		}

		private bool AreAllSubActionsComplete(Agent agent, int parameter_id) {
			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					State result = ExecuteSubAction(subAction, parameter_id);
					if (result != State.Success) {
						return false;
					}
				}
			}

			return true;
		}

		private State ExecuteSubAction(Action action, int parameter_id) {
			return action.Tick(parameter_id);
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
				State subActionResult = ExecuteSubActions(agent, parameter_id);
				return subActionResult;
			} else {
				ResetElapsedTime();
				return State.Failed;
			}
		}

		private State ExecuteSubActions(Agent agent, int parameter_id) {
			foreach (Action subAction in subActions) {
				if (subAction.CheckPreconditions(agent)) {
					return ExecuteSubAction(subAction, parameter_id);
				}
			}

			return State.Running;
		}

		private State ExecuteSubAction(Action action, int parameter_id) {
			return action.Tick(parameter_id);
		}

		private void ResetElapsedTime() {
			elapsedTime = 0f;
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
			InitializeDesireDictionary();
			int actionID = SymbolTable.GetID(action.name);
			actions[actionID] = action;
			UpdateDesireDictionary(action, actionID);
		}

		public void Remove(Action action) {
			int actionID = SymbolTable.GetID(action.name);
			actions.Remove(actionID);
			UpdateDesireDictionaryForRemoval(action);
		}

		private void InitializeDesireDictionary() {
			if (desireDictionary == null) {
				desireDictionary = new ActionReferences();
			}
		}

		private void UpdateDesireDictionary(Action action, int actionID) {
			for (int i = 0; i < action.effects.Count; i++) {
				desireDictionary.AddReference(action.effects.GetKey(i), action.effects[i], actionID);
			}
		}

		private void UpdateDesireDictionaryForRemoval(Action action) {
			int actionID = SymbolTable.GetID(action.name);
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

			// Extract the name and function from fileAction
			string extractedName = ExtractName(fileAction.name);
			newAction.name = fileAction.name;
			newAction.function = SymbolTable.GetID(extractedName);

			// Extract the parameterID from fileAction
			newAction.parameterID = ExtractParameterID(fileAction.name);

			newAction.utilityBelief = !string.IsNullOrEmpty(fileAction.utilityBelief) ? SymbolTable.GetID(fileAction.utilityBelief) : 0;
			newAction.actionID = fileAction.actionID;
			newAction.connections = fileAction.connections;

			TranslatePreconditions(fileAction.environmentalPreconditions, newAction.environmentalPreconditions);
			TranslatePreconditions(fileAction.preconditions, newAction.preconditions);
			TranslateEffects(fileAction.effects, ref newAction.effects);

			return newAction;
		}

		private static string ExtractName(string fullName) {
			int underscoreIndex = fullName.IndexOf('_');
			return (underscoreIndex >= 0) ? fullName.Substring(0, underscoreIndex) : fullName;
		}

		private static int ExtractParameterID(string fullName) {
			int underscoreIndex = fullName.IndexOf('_');
			if (underscoreIndex >= 0 && underscoreIndex < fullName.Length - 1) {
				string parameter = fullName.Substring(underscoreIndex + 1);
				return SymbolTable.GetID(parameter);
			}
			return 0;
		}

		private static void TranslatePreconditions(FileAction.WorldState[] preconditions, OrderedMap<bool> targetList) {
			for (int i = 0; i < preconditions.Length; i++) {
				int preconditionID = SymbolTable.GetID(preconditions[i].key);
				bool preconditionEffect = (bool)preconditions[i].op;
				targetList.Insert(preconditionID, preconditionEffect);
			}
		}

		private static void TranslateEffects(FileAction.WorldState[] effects, ref OrderedMap<bool> targetEffects) {
			if (targetEffects == null) {
				targetEffects = new OrderedMap<bool>();
			}
			for (int i = 0; i < effects.Length; i++) {
				int effectID = SymbolTable.GetID(effects[i].key);
				bool effectEffect = (bool)effects[i].op;
				targetEffects.Insert(effectID, effectEffect);
			}
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

			EnqueueDesires(desires, actionQueue, visited);

			while (actionQueue.Count > 0) {
				int current = actionQueue.Dequeue();
				if (actions[current].CheckPreconditions(agent)) {
					return GetPlan(current, parent);
				}
				EnqueueNeighbors(current, actions, actionQueue, visited, parent);
			}
			return null;
		}

		private void EnqueueDesires(int[] desires, Queue<int> actionQueue, HashSet<int> visited) {
			foreach (int i in desires) {
				actionQueue.Enqueue(i);
				visited.Add(i);
			}
		}

		private int[] GetPlan(int current, Dictionary<int, int> parent) {
			List<int> plan = new List<int>();
			while (current != -1) {
				plan.Insert(0, current);
				current = parent.ContainsKey(current) ? parent[current] : -1;
			}
			return plan.ToArray();
		}

		private void EnqueueNeighbors(int current, Dictionary<int, Action> actions, Queue<int> actionQueue,
									  HashSet<int> visited, Dictionary<int, int> parent) {
			foreach (int neighbor in actions[current].connections) {
				int n_id = actions.ElementAt(neighbor).Key;
				if (!visited.Contains(n_id)) {
					actionQueue.Enqueue(n_id);
					visited.Add(n_id);
					parent.Add(n_id, current);
				}
			}
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
			return currentAction.CheckPreconditions(agent);
		}

		public State Tick() {
			if (actionIds.Count == 0) {
				return State.Inactive;
			}

			int currentActionId = actionIds.Peek();
			Func<int, State> currentAction = agent.actions[currentActionId];

			State state = ExecuteCurrentAction(currentActionId, currentAction);

			if (state == State.Success) {
				actionIds.Pop();
				return Tick(); // Call Tick again to process the next action immediately
			} else if (state == State.Failed) {
				ClearActionQueue();
			}

			return state;
		}

		private State ExecuteCurrentAction(int actionId, Func<int, State> action) {
			int parameterId = ActionGraph.instance.GetAction(actionId).parameterID;
			return action.Invoke(parameterId);
		}

		private void ClearActionQueue() {
			actionIds.Clear();
		}
	}
}
