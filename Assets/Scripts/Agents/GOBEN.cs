using System;
using System.Collections.Generic;
using Utils;


namespace GOBEN {

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
			intention.environmentalPreconditions = environmentalPreconditions == null ? null : new List<(int, bool)>(environmentalPreconditions);
			intention.utilityID = utilityID;
			return intention;
		}

		public virtual bool CheckEnvironmentalPreconditions() {

			return true;
		}

		public static Intention TranslateToIntent(Motivation motivation) {
			if (motivation is Desire desire) {
				Intention tmpIntention =  new Intention(desire.name, desire.priority, desire.desiredState,desire.preconditions);
				tmpIntention.origin = desire;
				return tmpIntention;
			} else if (motivation is Obligation obligation) {
				Intention tmpIntention = new Intention(obligation.name, obligation.priority, obligation.desiredState, obligation.preconditions);
				tmpIntention.origin = obligation;
				return tmpIntention;
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
		public object origin;
		public Intention(string name, float priority) {
			this.name = name;
			this.priority = priority;
		}
		public Intention(string name, float priority, (int, bool) desiredState, List<(int, bool)> preconditions) {
			this.name = name;
			this.priority = priority;
			this.desiredState = desiredState;
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
			SetPersonalityInference(C,E,A,N);
		}

		private void SetPersonalityInference(float C, float E, float A, float N) {
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

