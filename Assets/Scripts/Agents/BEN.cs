using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Utils;


namespace BEN {

	[Serializable]
	public class Agent {

		#region Variables
		public Dictionary<int, Func<int, State>> actions;
		public OrderedMap<Belief> beliefs;
		private List<Belief> decomposingBeliefs;
		public Dictionary<int, Desire> desires;
		public Dictionary<int, Emotion> currentEmotions;
		public Intention currentIntention;
		private Personality personality;
		public ActionStack actionStack;
		#endregion

		public Agent() {
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new Dictionary<int, Desire>(30);
			currentIntention = null;
			personality = null;
		}
		public void InitializeActions(string jsonpath) {
			actions = new Dictionary<int, Func<int, State>>();

			// Request the array of action keys from ActionGraph
			var actionKeys = ActionGraph.GetActionKeys(jsonpath);
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new Dictionary<int, Desire>(30);
			actionStack = new ActionStack(this);
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
			desires.Add(desire.ID, desire);
		}
		public void RemoveDesire(int ID) {
			if (desires.ContainsKey(ID)) desires.Remove(ID);
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
	public class Belief : MentalState {
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
	public class Motivation : MentalState {
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
	public class Desire : Motivation {
		public Desire(string name, float priority) {
			this.name = name;
			this.priority = priority;
			preconditions = new List<int>();
		}
		public Desire(string name, float priority, params int[] preconditions) {
			this.name = name;
			this.priority = priority;
			this.preconditions = new List<int>(preconditions);
		}
	}
	[Serializable]
	public class Intention : Motivation {
		public Intention(string name, float priority) {
			this.name = name;
			this.priority = priority;
		}
		public Intention(string name, float priority, List<int> preconditions) {
			this.name = name;
			this.priority = priority;
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
	public class Emotion {

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


}

