using System;
using System.Collections.Generic;
using Utils;
//todo
//finish norms
//make norm editor
//load norms
//design intentions 
//laws and obligations tresholds
//Add reaction module

namespace GOBEN {

	[Serializable]
	public class Agent {
		public struct agentPerceived {
			public Agent agent;
			public float timer;
		}

		#region Variables
		public Dictionary<int, Func<int, State>> actions;
		public OrderedMap<Belief> beliefs;
		private List<Belief> decomposingBeliefs;
		public OrderedMap<Desire> desires;
		public OrderedMap<Obligation> obligations;
		public Dictionary<int, Emotion> currentEmotions;
		public Dictionary<Agent, Relationship> relationships;
		public float emotionalObedienceMultiplier = 1;
		public Intention currentIntention;
		private Personality personality;
		public ActionStack actionStack;
		public bool followingObligations;

		public List<agentPerceived> perceivedAgents;

		float intentionCooldownTimer = 0f;
		#endregion

		public Agent() {
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new OrderedMap<Desire>(30);
			currentIntention = null;
			personality = null;

			actions = new Dictionary<int, Func<int, State>>();
			perceivedAgents = new List<agentPerceived>();
			currentEmotions = new Dictionary<int, Emotion>();
			relationships = new Dictionary<Agent, Relationship>();
			actionStack = new ActionStack(this);
			followingObligations = false;
		}
		public void InitializeActions(string jsonpath) {
			actions = new Dictionary<int, Func<int, State>>();

			// Request the array of action keys from ActionGraph
			var actionKeys = ActionGraph.GetActionKeys(jsonpath);
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new OrderedMap<Desire>(30);
			actionStack = new ActionStack(this);
			// Set all the keys in the actions dictionary of the agent to the keys inside the actionGraph container
			foreach (int key in actionKeys) {
				actions[key] = null;
			}
		}
		public void LoadNewAgent() {
			beliefs = new OrderedMap<Belief>(30);
			decomposingBeliefs = new List<Belief>(30);
			desires = new OrderedMap<Desire>(30);
			currentIntention = null;
		}

		public void Update(float deltaTime) {
			UpdateBeliefs(deltaTime);
			UpdatePerceivedAgents(deltaTime);
			UpdateEmotions(deltaTime);

			MakeDecision(deltaTime);
			actionStack.Tick();
		}

		private void UpdateEmotions(float deltaTime) {
			List<int> emotionsToRemove = new List<int>();
			if(currentEmotions == null) currentEmotions = new Dictionary<int, Emotion> ();
			foreach (var emotion in currentEmotions) {
				emotion.Value.Degrade(deltaTime);
				if (emotion.Value.intensity <= 0) {
					emotionsToRemove.Add(emotion.Key);
				}
			}

			foreach (var emotionKey in emotionsToRemove) {
				currentEmotions.Remove(emotionKey);
			}
		}

		private void UpdateBeliefs(float deltaTime) {
			if(decomposingBeliefs == null)decomposingBeliefs = new List<Belief> ();
			for (int i = decomposingBeliefs.Count - 1; i >= 0; i--) {
				Belief belief = decomposingBeliefs[i];
				belief.Degrade(deltaTime);
				if (belief.lifeTime <= 0) {
					beliefs.Remove(belief.ID);
				}
			}
		}

		private void UpdatePerceivedAgents(float deltaTime) {
			if(perceivedAgents == null) perceivedAgents = new List<agentPerceived> { };
			for (int i = perceivedAgents.Count - 1; i >= 0; i--) {
				agentPerceived perceivedAgent = perceivedAgents[i];
				perceivedAgent.timer -= deltaTime;
				if (perceivedAgent.timer <= 0) {
					perceivedAgents.RemoveAt(i);
				}
			}
		}

		public void PerceiveAgent(Agent agent) {
			// Check if the agent is already perceived
			int index = GetPerceivedAgentIndex(agent);
			if (index >= 0) {
				// Update the timer to 60 seconds
				agentPerceived perceivedAgent = perceivedAgents[index];
				perceivedAgent.timer = 60f;
				perceivedAgents[index] = perceivedAgent;
			} else {
				// Add the agent to the perceivedAgents list with a timer value of 60 seconds
				perceivedAgents.Add(new agentPerceived { agent = agent, timer = 60f });
			}
		}

		private int GetPerceivedAgentIndex(Agent agent) {
			for (int i = 0; i < perceivedAgents.Count; i++) {
				if (perceivedAgents[i].agent == agent) {
					return i;
				}
			}
			return -1; // Agent not found
		}

		public void MakeDecision(float deltaTime) {
			if (KeepCurrentIntention(deltaTime)) {
				if (actionStack.CanDoAction()) {
					return;
				} else {
					SetNewPlan();
				}
			} else {
				MakeIntention();
				SetNewPlan();
			}
		}

		void SetNewPlan() {
			if (currentIntention == null) return;

			Norm norm = ActionGraph.instance.GetNormPlan(currentIntention.ID, currentObedience());
			if (norm.behavior != null) {
				foreach (int actionId in norm.behavior) {
					actionStack.AddAction(actionId);
				}
			} else {
				(int, bool) state = currentIntention.desiredState;
				ActionGraph.MakePlan(state.Item1, state.Item2, this);
			}
		}

		bool KeepCurrentIntention(float deltaTime) {
			if (currentIntention == null) {
				intentionCooldownTimer = personality.consciousness * 80f;
				return false;
			}

			if (intentionCooldownTimer > 0f) {
				intentionCooldownTimer -= deltaTime;  
				return true;  
			}

			Random random = new Random();
			double randomValue = random.NextDouble();
			intentionCooldownTimer = personality.consciousness * 80f;

			return randomValue <= personality.probabilityOfKeepingIntention;
		}

		float currentNormTreshold = 1;

		public void MakeIntention() {
			if (obligations == null) obligations = new OrderedMap<Obligation>();
			if (desires == null) desires = new OrderedMap<Desire>();
			(Motivation, float) bestObligation = ChooseIntention(obligations);
			(Motivation, float) bestDesire = ChooseIntention(desires);
			followingObligations = ObeyObligations(currentNormTreshold);
			if (followingObligations && bestObligation.Item1 is Obligation bestObligationIntention) {
				currentIntention = bestObligationIntention.ToIntention(this);
			} else if (bestDesire.Item1 is Desire bestDesireIntention) {
				currentIntention = bestDesireIntention.ToIntention(this);
			}
		}

		float DesireExpectedUtility = 1f;
		public float CalculateEmotionalObedienceMultiplier() {
			float sumObedienceModifiers = 0;
			int count = 0;

			foreach (var emotion in currentEmotions.Values) {
				sumObedienceModifiers += emotion.obedienceModifier;
				count++;
			}

			return count > 0 ? sumObedienceModifiers / count : 1; // Return the average or 1 if no emotions present
		}

		public float CalculateSocialObedienceMultiplier() {
			float perceivedAgentsCount = perceivedAgents.Count;
			float relationshipMultiplier = 1;

			foreach (var perceivedAgent in perceivedAgents) {
				Agent agent = perceivedAgent.agent;
				if (relationships.ContainsKey(agent)) {
					Relationship relationship = relationships[agent];

					// Check if the other agent is following obligations
					if (agent.followingObligations) {
						relationshipMultiplier += relationship.liking;
					} else {
						relationshipMultiplier -= relationship.liking;
					}
				} else {
					// Agent is not known, apply a light effect on obedience multiplier
					relationshipMultiplier += 0.05f;
				}
			}

			float perceptionMultiplier = Math.Clamp(1 - perceivedAgentsCount * 0.1f, 0.1f, 1);

			return perceptionMultiplier * relationshipMultiplier;
		}

		public bool ObeyObligations(float obedienceThreshold) {
			

			return currentObedience() - DesireExpectedUtility > obedienceThreshold;
		}

		public float currentObedience() {
			emotionalObedienceMultiplier = CalculateEmotionalObedienceMultiplier();
			float socialObedienceMultiplier = CalculateSocialObedienceMultiplier();

			float obedienceValue = personality.obedience * emotionalObedienceMultiplier * socialObedienceMultiplier;

			Random random = new Random();
			double randomFactor = random.NextDouble() * 0.2 + 0.9; // Adjust the range and value based on the impact of randomness

			obedienceValue *= (float)randomFactor;
			return obedienceValue;
		}

		public (Motivation, float) ChooseIntention<T>(OrderedMap<T> container) where T : Motivation {
			float totalUtility = 0;
			for (int i = 0; i < container.length; i++) {
				float util = beliefs.ContainsKey(container[i].utilityID) ? (float)beliefs[container[i].utilityID].value : 0;
				totalUtility += util;
			}

			Random random = new Random();
			float randomValue = (float)random.NextDouble() * totalUtility;

			for (int i = 0; i < container.length; i++) {
				float util = beliefs.ContainsKey(container[i].utilityID) ? (float)beliefs[container[i].utilityID].value : 0;
				totalUtility -= util;

				if (randomValue <= 0 && container[i].CheckEnvironmentalPreconditions()) {
					return (Motivation.TranslateToIntent(container[i]), util);
				}
			}

			return (null, 0f);
		}

		public void AddDesire(Desire desire) {
			desires.Insert(desire.ID, desire);
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

	#region Cognitive Engine
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
		public (int, bool) desiredState;
		public List<(int, bool)> preconditions;
		public List<(int, bool)> environmentalPreconditions;
		public int utilityID;
		public bool IsAchievable(Agent agent) {
			return true;
		}
		public void AddPrecondition((int, bool) precondition) {
			preconditions.Add(precondition);
		}
		public Intention ToIntention(Agent agent) {
			Intention intention = new Intention(name, priority);
			intention.desiredState = desiredState;
			intention.preconditions = new List<(int, bool)>(preconditions);
			intention.environmentalPreconditions = new List<(int, bool)>(environmentalPreconditions);
			intention.utilityID = utilityID;
			return intention;
		}

		public virtual bool CheckEnvironmentalPreconditions() {

			return true;
		}

		public static Intention TranslateToIntent(Motivation motivation) {
			if (motivation is Desire desire) {
				return new Intention(desire.name, desire.priority, desire.preconditions);
			} else if (motivation is Obligation obligation) {
				return new Intention(obligation.name, obligation.priority, obligation.preconditions);
			} else {
				// Handle other cases or throw an exception if needed
				return null;
			}
		}

	}
	[Serializable]
	public class Desire : Motivation {
		public Desire(string name) {
			this.name = name;
			preconditions = new List<(int, bool)>();
		}
		public Desire(string name, bool state, int utilityID = 0, params (int, bool)[] preconditions) {
			this.name = name;
			this.desiredState = (SymbolTable.GetID(name), state);
			this.utilityID = utilityID;
			this.preconditions = new List<(int, bool)>(preconditions);
		}
	}
	[Serializable]
	public class Intention : Motivation {
		public Intention(string name, float priority) {
			this.name = name;
			this.priority = priority;
		}
		public Intention(string name, float priority, List<(int, bool)> preconditions) {
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
		public Obligation(string name, float priority, List<(int, bool)> preconditions) {
			this.name = name;
			this.priority = priority;
			this.preconditions = preconditions;
		}
	}

	#endregion

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
		public float obedienceModifier = 1;

		public Emotion(string p_name, string p_predicate, Agent p_agentResponsable, float p_intensity, float p_decay) {
			name = p_name;
			predicate = p_predicate;
			agentResponsable = p_agentResponsable;
			intensity = p_intensity;
			decay = p_decay;
		}

		public void Degrade(float deltaTime) {
			if (intensity > 0) {
				intensity -= deltaTime * 0.01f;
			}
		}
	}

	[Serializable]
	public class Relationship {
		public Agent otherAgent;
		public float liking;
		public float degreeOfPower;
		public float solidarity;
		public float familiarity;
		public float trust;
	}


}

