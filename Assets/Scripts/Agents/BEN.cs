using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;


namespace BEN {
	public enum State {
		Inactive,
		Running,
		Success,
		Failed
	}
	[Serializable]
	public class Agent {

		#region Variables
		public Dictionary<int, Func<State>> actions;
		public OrderedMap<Belief> beliefs;
		private List<Belief> decomposingBeliefs;
		public Dictionary<int, Desire> desires;
		public Dictionary<int, Emotion> currentEmotions;
		public Intention currentIntention;
		public Plan currentPlan;
		private Personality personality;
		#endregion
		public Agent() {
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new Dictionary<int, Desire>(30);
			currentIntention = null;
			currentPlan = null;
			personality = null;

		}

		public void InitializeActions(string jsonpath) {
			actions = new Dictionary<int, Func<State>>();

			// Request the array of action keys from ActionGraph
			var actionKeys = ActionGraph.GetActionKeys(jsonpath);

			// Set all the keys in the actions dictionary of the agent to the keys inside the actionGraph container
			foreach (int key in actionKeys) {
				actions[key] = null;
			}
		}

		public void LoadNewAgent() {
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new Dictionary<int, Desire>(30);
			currentIntention = null;
			currentPlan = null;
		}
		public void UpdateBeliefs(float deltaTime) {
			for (int i = decomposingBeliefs.Count - 1; i >= 0; i--) {
				Belief belief = decomposingBeliefs[i];
				belief.Degrade(deltaTime);
				if (belief.lifeTime <= 0) {
					beliefs.Remove(belief.ID);
				}
			}
		}
		public void CheckBeliefs() {
			//beliefsChanged = true;
		}
		public void AddDesire(Desire desire) {
			desires.Add(desire.ID,desire);
		}
		public void RemoveDesire(int ID) {
			if(desires.ContainsKey(ID))desires.Remove(ID);
		}

		public State GetCurrentPlanState() {
			if (currentPlan != null) {
				return currentPlan.lastKnownState;
			} else {
				return State.Inactive;
			}
		}
		public void SetPersonality(float O, float C, float E, float A, float N) {
			personality = new Personality(O, C, E, A, N);
		}
		public void SetPersonality(Personality personality) {
			this.personality = personality.CopyPersonality();
		}
		public Personality GetPersonality() {
			return personality;
		}
		public static Emotion EmotionalContagion(Agent agent_i, Agent agent_j, int emotionID, float threshold = 0.25f) {
			Emotion em_i = agent_i.currentEmotions[emotionID];
			if (em_i == null) {
				return null;
			}

			float charisma_i = agent_i.GetPersonality().charisma;
			float receptivity_j = agent_j.GetPersonality().receptivity;
			float contagionFactor = charisma_i * receptivity_j;

			if (contagionFactor < threshold) {
				return null;
			}

			Emotion em_j = agent_j.currentEmotions[emotionID];
			float newIntensity;
			float newDecay;

			if (em_j != null) {
				newIntensity = em_j.intensity + em_i.intensity * contagionFactor;
				newDecay = em_i.intensity > em_j.intensity ? em_i.decay : em_j.decay;
			} else {
				newIntensity = em_i.intensity * contagionFactor;
				newDecay = em_i.decay;
				agent_j.currentEmotions.Add(emotionID, em_j);
			}

			newIntensity = Math.Clamp(newIntensity, 0, 1);
			em_j.intensity = newIntensity;
			em_j.decay = newDecay;

			return em_j;
		}
	}
	[Serializable]
	public class MentalState {
		public string name;
		public int ID;
		public float lifeTime;
		public float decompositionRate;
		public float priority;
		public float Degrade(float deltaTime) {
			lifeTime -= decompositionRate * deltaTime;
			return lifeTime;
		}
	}
	[Serializable]
	public class Belief : MentalState{
		public object value;
		public Belief(string name, object value) {
			this.ID = SymbolTable.GetID(name);
			this.name = name;
			this.value = value;
			lifeTime = 1;
			decompositionRate = 0;
		}
		public Belief(string name, object value, float lifeTime, float decompositionRate) {
			ID = SymbolTable.GetID(name);
			this.name = name;
			this.value = value;
			this.lifeTime = lifeTime;
			this.decompositionRate = decompositionRate;
		}
	}
	[Serializable]
	public class Uncertainty : MentalState {
		public Uncertainty(string name, float priority) {
			this.name = name;
			this.priority = priority;
			lifeTime = 1;
			decompositionRate = 0;
		}
		public Uncertainty(string name, float priority, float lifeTime, float decompositionRate) {
			this.name = name;
			this.priority = priority;
			this.lifeTime = lifeTime;
			this.decompositionRate = decompositionRate;
		}
	}

	[Serializable]
	public class Ideal : MentalState {

		public float praiseworthiness;
		public Ideal(string name, float praiseworthiness) {
			this.name = name;
			this.praiseworthiness = praiseworthiness;
			lifeTime = 1;
			decompositionRate = 0;
		}
		public Ideal(string name, float praiseworthiness, float lifeTime, float decompositionRate) {
			this.name = name;
			this.praiseworthiness = praiseworthiness;
			this.lifeTime = lifeTime;
			this.decompositionRate = decompositionRate;
		}
	}
	[Serializable]
	public class Motivation : MentalState{
		public List<int> preconditions;
		public int utilityID;
		public bool IsAchievable(Agent agent) {
			return true;
		}
		public void AddPrecondition(int precondition) {
			preconditions.Add(precondition);
		}
		public Intention ToIntention(Agent agent) {
			//if (!agent.planLibrary.CheckPreconditions(preconditions, agent.beliefs)) {
			//	return null;
			//}
			Intention intention = new Intention(name, priority);
			intention.preconditions = new List<int>(preconditions);

			return intention;
		}
	}
	[Serializable]
	public class Desire : Motivation{
		public Desire(string name,float priority) {
			this.name = name;
			this.priority = priority;
			preconditions = new List<int>();
		}
		public Desire(string name, float priority ,params int[] preconditions) {
			this.name = name;
			this.priority =priority;
			this.preconditions = new List<int>(preconditions);
		}
	}
	[Serializable]
	public class Intention : Motivation{
		public Intention(string name, float priority) {
			this.name = name;
			this.priority = priority;
		}
		public Intention(string name, float priority,List<int> preconditions) {
			this.name = name;
			this.priority=priority;
			this.preconditions = preconditions;
		}
	}
	[Serializable]
	public class Obligation : Motivation {
		public Obligation(string name, float priority) {
			this.name = name;
			this.priority = priority;
		}
		public Obligation(string name, float priority, List<int> preconditions) {
			this.name = name;
			this.priority = priority;
			this.preconditions = preconditions;
		}
	}
	[Serializable]
	public class Plan {
		public string name;
		public int priority;
		public string intention;
		public State lastKnownState;
		public List<string> preconditions;
		private List<Func<State>> actions;

		private int actionIndex = 0;
		public Plan(string name, string intention, params Func<BEN.State>[] actions) {
			this.name = name;
			this.priority = 0;
			this.intention = intention;
			this.actions = actions.ToList();
			actionIndex = 0;
		}
		public Plan GetCopy() {
			Func<State>[] actionsCopy = actions.ToArray();
			Plan planCopy = new Plan(name, intention, actionsCopy);
			planCopy.priority = priority;
			planCopy.preconditions = preconditions != null ? new List<string>(preconditions) : null;
			return planCopy;
		}
		public State Tick() {
			while (true) {
				State state = lastKnownState = actions[actionIndex].Invoke();//<-Tick action
				if (state != State.Success) return state;
				if (++actionIndex >= actions.Count) {
					return State.Success;
				}
			}
		}
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
		protected Func<State> action;

		public Action() {
			this.preconditions = new OrderedMap<Effect> ();
		}

		public Action(int preconditionid, Effect precondition, int effectid, Effect effect) {
			this.preconditions = new OrderedMap<Effect>();
			this.preconditions.Insert(preconditionid, precondition);
			this.effects = new OrderedMap<Effect>();
			this.effects.Insert(effectid, effect);
		}

		public State Excecute() {
			return action.Invoke();
		}

		public virtual bool CheckPreconditions(Agent agent) {
			while (!preconditions.CompletedList()) {
				int ID = preconditions.GetCurrentKey();
				Effect PreconditionItem = preconditions.GetCurrentValue();
				
				if (!Compare(agent.beliefs.Cycle(ID).value,PreconditionItem)) {
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
			if(op == Effect.TRUE && (bool)check) return true;
			if (op == Effect.FALSE && !(bool)check) return true;
			else return false;
		}

		private static bool Compare(object left, string op, object right) {
			switch (op) {
				case "==":
					return Equals(left, right);
				case "!=":
					return !Equals(left, right);
				case ">":
					return Comparer<object>.Default.Compare(left, right) < 0;
				case ">=":
					return Comparer<object>.Default.Compare(left, right) <= 0;
				case "<":
					return Comparer<object>.Default.Compare(left, right) > 0;
				case "<=":
					return Comparer<object>.Default.Compare(left, right) >= 0;
				case "<>":
					if (left is Interval intervalOUT) {
						return !intervalOUT.Contains(right);
					} else {
						throw new ArgumentException("Invalid comparison with operator '<>'");
					}
				case "><":
					if (left is Interval intervalIN) {
						return intervalIN.Contains(right);
					} else {
						throw new ArgumentException("Invalid comparison with operator '><'");
					}
				default:
					throw new ArgumentException("Invalid operator: " + op);
			}
		}

		
	}

	[Serializable]
	public class Interval {
		public float Min;
		public float Max;
		public Interval(float min, float max) {
			Min = min;
			Max = max;
		}

		public bool Contains(object value) {
			switch (value) {
				case float floatValue:
					return floatValue >= Min && floatValue <= Max;
				case int intValue:
					return intValue >= Min && intValue <= Max;
				case double doubleValue:
					return doubleValue >= Min && doubleValue <= Max;
				default:
					throw new ArgumentException("Invalid value type.");
			}
		}

		public bool Contains(float value) {
			return value >= Min && value <= Max;
		}

		public bool Contains(int value) {
			return value >= Min && value <= Max;
		}

		public bool Contains(double value) {
			return value >= Min && value <= Max;
		}
	}

	[Serializable]
	public class ActionGraph {
		public static ActionGraph instance;
		private Dictionary<int,Action> actions;

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
			actions[SymbolTable.GetID(action.name)] = action;
		}

		public void Remove(Action action) {
			int actionID = SymbolTable.GetID(action.name);
			actions.Remove(actionID);
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

	}

	public class FileActionWrapper {
		public FileAction[] actions;
	}


	[Serializable]
	public class Personality {
		public float openness;
		public float consciousness;
		public float extroversion;
		public float agreeableness;
		public float neurotism;
		public float charisma;
		public float receptivity;
		public float obedience;
		public float probabilityOfKeepingPlan;
		public float probabilityOfKeepingIntention;
		public Personality(float O, float C, float E, float A, float N) {
			openness = O;
			consciousness = C;
			extroversion = E;
			agreeableness = A;
			neurotism = N;

			charisma = E;
			receptivity = 1 - N;
			obedience = MathF.Sqrt(C * A * 0.5f);
			probabilityOfKeepingPlan = MathF.Sqrt(C);
			probabilityOfKeepingIntention = MathF.Sqrt(C);
		}

		public Personality CopyPersonality() {
			Personality newPersonality = new Personality(openness, consciousness, extroversion, agreeableness, neurotism);
			newPersonality.charisma = charisma;
			newPersonality.receptivity = receptivity;
			newPersonality.obedience = obedience;
			newPersonality.probabilityOfKeepingPlan = probabilityOfKeepingPlan;
			newPersonality.probabilityOfKeepingIntention = probabilityOfKeepingIntention;
			return newPersonality;
		}
	}

	[Serializable]
	public class Emotion  {

		public string name;
		public string predicate;
		public Agent agentResponsable;
		public float intensity;
		public float decay;

		public Emotion(string p_name, string p_predicate, Agent p_agentResponsable, float p_intensity, float p_decay) {
			name = p_name;
			predicate = p_predicate;
			agentResponsable = p_agentResponsable;
			intensity = p_intensity;
			decay = p_decay;
		}

		public void Degrade() {
			if (intensity > 0) {
				intensity -= UnityEngine.Time.deltaTime * 0.01f;
			}
		}


	}

	[Serializable]
	public class SymbolTable {
		static private Dictionary<string, int> _IDTable;
		static int _NextID = 0;

		public static int GetID(string key) {
			if (string.IsNullOrEmpty(key)) {
				throw new ArgumentException("Key cannot be null or empty");
			}

			if (_IDTable == null) {
				_NextID = 0;
				_IDTable = new Dictionary<string, int>();
			}

			if (_IDTable.ContainsKey(key)) {
				return _IDTable[key];
			} else {
				int ID = _NextID++;
				_IDTable[key] = ID;
				return ID;
			}
		}

		public static string GetString(int id) {
			if (_IDTable == null) {
				throw new InvalidOperationException("SymbolTable is not initialized");
			}

			foreach (var entry in _IDTable) {
				if (entry.Value == id) {
					return entry.Key;
				}
			}

			throw new ArgumentException($"ID {id} not found in SymbolTable");
		}

		public static void Destroy() {
			if(_IDTable != null) _IDTable.Clear();
			_IDTable = null;
			_NextID = 0;
		}
	}


	[Serializable]
	public class OrderedMap<T> {
		private const int DefaultCapacity = 4;

		private (int, T)[] container;
		private int lastIndex;
		private int pointer;

		public OrderedMap() {
			container = new (int, T)[DefaultCapacity];
			lastIndex = 0;
			pointer = 0;

			// Initialize the container with (int.MaxValue, default(T)) values
			for (int i = 0; i < container.Length; i++) {
				container[i] = (int.MaxValue, default(T));
			}
		}

		public OrderedMap(int capacity) {
			container = new (int, T)[capacity];
			lastIndex = 0;
			pointer = 0;

			// Initialize the container with (int.MaxValue, default(T)) values
			for (int i = 0; i < container.Length; i++) {
				container[i] = (int.MaxValue, default(T));
			}
		}

		public void Insert(int key, T value) {
			if (lastIndex == container.Length) {
				Array.Resize(ref container, container.Length * 2);

				// Fill the new slots with (int.MaxValue, null)
				for (int i = lastIndex; i < container.Length; i++) {
					container[i] = (int.MaxValue, default(T));
				}
			}

			int index = lastIndex;

			// Find the insertion index based on the key order
			while (index > 0 && container[index - 1].Item1 > key) {
				container[index] = container[index - 1];
				index--;
			}

			container[index] = (key, value);
			lastIndex++;
		}

		public void Remove(int key) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));

			if (index >= 0) {
				// Found the key, shift the rest of the items to the left
				for (int i = index; i < lastIndex - 1; i++) {
					container[i] = container[i + 1];
				}

				// Set the last item to (int.MaxValue, default(T))
				container[lastIndex - 1] = (int.MaxValue, default(T));

				lastIndex--;
			}
		}

		public object GetValue(int key) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));
			return index >= 0 ? container[index].Item2 : null;
		}

		public T Cycle(int key) {
			while (pointer < lastIndex && key <= container[pointer].Item1) {
				if (container[pointer].Item1 == key) {
					return container[pointer].Item2;
				}

				pointer++;
			}

			return default(T);
		}

		public int GetCurrentKey() {
			return container[pointer].Item1;
		}

		public void ResetPointer() {
			pointer = 0;
		}

		public T CleanCycle() {
			ResetPointer();
			return Cycle(int.MaxValue);
		}

		public int GetPointerValue() {
			return pointer;
		}

		public T GetCurrentValue() {
			return pointer < lastIndex ? container[pointer].Item2 : default(T);
		}

		public void Advance() {
			pointer++;
		}

		public bool CompletedList() {
			return pointer >= lastIndex;
		}

		public (int, T) GetData() {
			return container[pointer];
		}

		public bool ContainsKey(int key) {
			int index = Array.BinarySearch(container, 0, lastIndex, (key, default(T)));
			return index >= 0;
		}

		public T this[int index] {
			get {
				if (index >= 0 && index < lastIndex) {
					return container[index].Item2;
				} else {
					throw new IndexOutOfRangeException();
				}
			}
		}
	}

	


}

