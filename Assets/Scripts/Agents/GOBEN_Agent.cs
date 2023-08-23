using System;
using System.Collections.Generic;
using Utils;

namespace GOBEN {

    [Serializable]
    public class Agent {
        [Serializable]
        public struct AgentPerceived {
            public Agent agent;
            public float timer;
        }
        #region Variables
        #region Variables - Beliefs and Desires

        public Dictionary<int, Func<int, State>> actions;
        public OrderedMap<Belief> beliefs;
        private List<Belief> decomposingBeliefs;
        public OrderedMap<Desire> desires;
        public OrderedMap<Obligation> obligations;
        public Dictionary<int, Emotion> currentEmotions;
        public Dictionary<Agent, Relationship> relationships;
        public List<AgentPerceived> perceivedAgents;

        #endregion

        #region Variables - Intentions and Actions

        public Intention currentIntention;
        public ActionStack actionStack;
        public bool followingObligations;
        private float currentNormThreshold = 1;
        private float DesireExpectedUtility = 1f;
        public float emotionalObedienceMultiplier = 1;
        private float intentionCooldownTimer = 0f;

        #endregion

        #region Variables - Personality and Traits

        private Personality personality;

        #endregion
        #endregion

        public Agent() {
            InitializeDataStructures();
            InitializeSettings();
        }

        private void InitializeDataStructures() {
            beliefs = new OrderedMap<Belief>(30);
            decomposingBeliefs = new List<Belief>(30);
            desires = new OrderedMap<Desire>(30);
            perceivedAgents = new List<AgentPerceived>();
            currentEmotions = new Dictionary<int, Emotion>();
            relationships = new Dictionary<Agent, Relationship>();
        }

        private void InitializeSettings() {
            currentIntention = null;
            personality = null;
            actions = new Dictionary<int, Func<int, State>>();
            actionStack = new ActionStack(this);
            followingObligations = false;
        }

        public void InitializeActions(string jsonPath) {
            InitializeActionDictionary(jsonPath);
            ClearExistingData();
        }

        private void InitializeActionDictionary(string jsonPath) {
            actions = new Dictionary<int, Func<int, State>>();

            var actionKeys = ActionGraph.GetActionKeys(jsonPath);

            foreach (int key in actionKeys) {
                actions[key] = null;
            }
        }

        private void ClearExistingData() {
            beliefs = new OrderedMap<Belief>(30);
            decomposingBeliefs = new List<Belief>(30);
            desires = new OrderedMap<Desire>(30);
            actionStack = new ActionStack(this);
            currentIntention = null;
        }

        public void LoadNewAgent() {
            ClearExistingData();
        }

        public void Update(float deltaTime) {
            UpdateBeliefs(deltaTime);
            UpdatePerceivedAgents(deltaTime);
            UpdateEmotions(deltaTime);

            UpdateCognitiveEngine(deltaTime);
        }

        private void UpdateCognitiveEngine(float deltaTime) {
            MakeDecision(deltaTime);
            actionStack.Tick();
        }

        private void UpdateEmotions(float deltaTime) {
            DegradeEmotions(deltaTime);
            List<int> emotionsToRemove = GetEmotionsToRemove();
            RemoveEmotions(emotionsToRemove);
        }

        private void DegradeEmotions(float deltaTime) {
            if (currentEmotions == null)
                currentEmotions = new Dictionary<int, Emotion>();

            foreach (var emotion in currentEmotions) {
                emotion.Value.Degrade(deltaTime);
            }
        }

        private List<int> GetEmotionsToRemove() {
            List<int> emotionsToRemove = new List<int>();
            foreach (var emotion in currentEmotions) {
                if (emotion.Value.intensity <= 0) {
                    emotionsToRemove.Add(emotion.Key);
                }
            }
            return emotionsToRemove;
        }

        private void RemoveEmotions(List<int> emotionsToRemove) {
            foreach (var emotionKey in emotionsToRemove) {
                currentEmotions.Remove(emotionKey);
            }
        }
       
        private void UpdateBeliefs(float deltaTime) {
            if (decomposingBeliefs != null) {
                for (int i = decomposingBeliefs.Count - 1; i >= 0; i--) {
                    Belief belief = decomposingBeliefs[i];
                    belief.Degrade(deltaTime);
                    if (belief.lifeTime <= 0) {
                        beliefs.Remove(belief.ID);
                    }
                }
            }
        }

        private void UpdatePerceivedAgents(float deltaTime) {
            if (perceivedAgents != null) {
                for (int i = perceivedAgents.Count - 1; i >= 0; i--) {
                    AgentPerceived perceivedAgent = perceivedAgents[i];
                    perceivedAgent.timer -= deltaTime;
                    if (perceivedAgent.timer <= 0) {
                        perceivedAgents.RemoveAt(i);
                    }
                }
            }
        }

        public void UpdateBelief(string name, object value) {
            if (beliefs != null) {
                Belief existingBelief;
                if (beliefs.TryGetValue(SymbolTable.GetID(name), out existingBelief)) {
                    existingBelief.value = value;
                } else {
                    Belief newBelief = new Belief(name, value);
                    beliefs.Insert(newBelief.ID, newBelief);
                }
            }
        }

        public void PerceiveAgent(Agent agent) {
            // Check if the agent is already perceived
            int index = GetPerceivedAgentIndex(agent);
            if (index >= 0) {
                // If the agent is already perceived, update the timer to 60 seconds
                AgentPerceived perceivedAgent = perceivedAgents[index];
                perceivedAgent.timer = 60f;
                perceivedAgents[index] = perceivedAgent;
            } else {
                // If the agent is not yet perceived, add it to the perceivedAgents list with a timer value of 60 seconds
                perceivedAgents.Add(new AgentPerceived { agent = agent, timer = 60f });
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
                if (!actionStack.CanDoAction()) {
                    SetNewPlan();
                }
            } else if (HasObligationsAndDesires()) {
                MakeIntention();
                SetNewPlan();
            }
        }

        private bool HasObligationsAndDesires() {
            return (obligations != null && obligations.Count > 0) || (desires != null && desires.Count > 0);
        }

        private void SetNewPlan() {
            if (currentIntention != null) {
                Norm norm = ActionGraph.instance.GetNormPlan(currentIntention.ID, currentObedience());
                if (norm != null && norm.behavior != null) {
                    InsertNormToActionStack(norm);
                } else {
                    MakeDefaultPlan();
                }
            }
        }

        private void InsertNormToActionStack(Norm norm) {
            foreach (int actionId in norm.behavior) {
                actionStack.AddAction(actionId);
            }
        }

        private void MakeDefaultPlan() {
            (int, bool) state = currentIntention.desiredState;
            ActionGraph.MakePlan(state.Item1, state.Item2, this);
            currentIntention = null;
        }

        private bool KeepCurrentIntention(float deltaTime) {
            if (currentIntention == null) {
                SetIntentionCooldownTimer();
                return false;
            }

            if (intentionCooldownTimer > 0f) {
                ReduceIntentionCooldown(deltaTime);
                return true;
            }

            SetIntentionCooldownTimer();
            return ShouldKeepIntentionWithProbability();
        }

        private void SetIntentionCooldownTimer() {
            intentionCooldownTimer = personality.consciousness * 80f;
        }

        private void ReduceIntentionCooldown(float deltaTime) {
            intentionCooldownTimer -= deltaTime;
        }

        private bool ShouldKeepIntentionWithProbability() {
            Random random = new Random();
            double randomValue = random.NextDouble();
            return randomValue <= personality.probabilityOfKeepingIntention;
        }

        public void MakeIntention() {
            InitializeObligationsAndDesires();
            (Motivation, float) bestObligation = ChooseIntention(obligations);
            (Motivation, float) bestDesire = ChooseIntention(desires);
            followingObligations = ObeyObligations(currentNormThreshold);

            SetCurrentIntentionBasedOnPriority(bestObligation, bestDesire);
        }

        private void InitializeObligationsAndDesires() {
            if (obligations == null)
                obligations = new OrderedMap<Obligation>();
            if (desires == null)
                desires = new OrderedMap<Desire>();
        }

        private void SetCurrentIntentionBasedOnPriority((Motivation, float) bestObligation, (Motivation, float) bestDesire) {
            if (followingObligations && bestObligation.Item1 is Intention bestObligationIntention) {
                SetCurrentIntention(bestObligationIntention);
            } else if (bestDesire.Item1 is Intention bestDesireIntention) {
                SetCurrentIntention(bestDesireIntention);
                RemoveDesire(((Desire)currentIntention.origin).ID);
            }
        }

        private void SetCurrentIntention(Intention intention) {
            currentIntention = intention;
        }

        public float CalculateEmotionalObedienceMultiplier() {
            float totalObedienceModifier = 0;
            int emotionCount = 0;

            // Calculate the total obedience modifier and count the number of emotions
            foreach (var emotion in currentEmotions.Values) {
                totalObedienceModifier += emotion.obedienceModifier;
                emotionCount++;
            }

            // Calculate the average obedience modifier (emotional obedience multiplier)
            return emotionCount > 0 ? totalObedienceModifier / emotionCount : 1;
        }

        public float CalculateSocialObedienceMultiplier() {
            float perceivedAgentsCount = GetPerceivedAgentsCount();
            float relationshipMultiplier = CalculateRelationshipMultiplier();

            float perceptionMultiplier = CalculatePerceptionMultiplier(perceivedAgentsCount);

            return perceptionMultiplier * relationshipMultiplier;
        }

        private float GetPerceivedAgentsCount() {
            return perceivedAgents.Count;
        }

        private float CalculateRelationshipMultiplier() {
            float relationshipMultiplier = 1;

            foreach (var perceivedAgent in perceivedAgents) {
                Agent agent = perceivedAgent.agent;
                float likingMultiplier = GetLikingMultiplier(agent);

                if (relationships.ContainsKey(agent)) {
                    Relationship relationship = relationships[agent];
                    relationshipMultiplier += relationship.liking * likingMultiplier;
                } else {
                    relationshipMultiplier += 0.05f;
                }
            }

            return relationshipMultiplier;
        }

        private float GetLikingMultiplier(Agent agent) {
            return agent.followingObligations ? 1 : -1;
        }

        private float CalculatePerceptionMultiplier(float perceivedAgentsCount) {
            float perceptionMultiplier = Math.Clamp(1 - perceivedAgentsCount * 0.1f, 0.1f, 1);
            return perceptionMultiplier;
        }

        public bool ObeyObligations(float obedienceThreshold) {
            return currentObedience() - DesireExpectedUtility > obedienceThreshold;
        }

        public float currentObedience() {
            float emotionalObedienceMultiplier = CalculateEmotionalObedienceMultiplier();
            float socialObedienceMultiplier = CalculateSocialObedienceMultiplier();

            float obedienceValue = CalculateBaseObedienceValue(emotionalObedienceMultiplier, socialObedienceMultiplier);

            float randomizedObedienceValue = ApplyRandomFactor(obedienceValue);

            return randomizedObedienceValue;
        }

        private float CalculateBaseObedienceValue(float emotionalObedienceMultiplier, float socialObedienceMultiplier) {
            return personality.obedience * emotionalObedienceMultiplier * socialObedienceMultiplier;
        }

        private float ApplyRandomFactor(float obedienceValue) {
            Random random = new Random();
            double randomFactor = random.NextDouble() * 0.2 + 0.9;
            return obedienceValue * (float)randomFactor;
        }

        public (Motivation, float) ChooseIntention<T>(OrderedMap<T> container) where T : Motivation {
            if (container.Count == 1) {
                return ChooseSingleIntention(container[0]);
            }

            float totalUtility = CalculateTotalUtility(container);
            float randomValue = GenerateRandomValue(totalUtility);

            return ChooseIntentionBasedOnRandomValue(container, totalUtility, randomValue);
        }

        private (Motivation, float) ChooseSingleIntention(Motivation motivation) {
            float utilityValue = GetUtilityValue(motivation);
            return (Motivation.TranslateToIntent(motivation), utilityValue);
        }

        private float CalculateTotalUtility<T>(OrderedMap<T> container) where T : Motivation {
            float totalUtility = 0;
            for (int i = 0; i < container.Count; i++) {
                float utilityValue = GetUtilityValue(container[i]);
                totalUtility += utilityValue;
            }
            return totalUtility;
        }

        private float GenerateRandomValue(float totalUtility) {
            Random random = new Random();
            return (float)random.NextDouble() * totalUtility;
        }

        private (Motivation, float) ChooseIntentionBasedOnRandomValue<T>(OrderedMap<T> container, float totalUtility, float randomValue) where T : Motivation {
            for (int i = 0; i < container.Count; i++) {
                float utilityValue = GetUtilityValue(container[i]);
                totalUtility -= utilityValue;

                if (randomValue <= 0 && container[i].CheckEnvironmentalPreconditions()) {
                    return (Motivation.TranslateToIntent(container[i]), utilityValue);
                }
            }

            return (null, 0f);
        }

        private float GetUtilityValue(Motivation motivation) {
            return beliefs.ContainsKey(motivation.utilityID) ? (float)beliefs[motivation.utilityID].value : 0;
        }

        public void AddDesire(Desire desire) {
            desires.Insert(desire.ID, desire);
        }

        public void RemoveDesire(int ID) {
            if (desires.ContainsKey(ID))
                desires.Remove(ID);
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
            Emotion em_i = agent_i.currentEmotions.ContainsKey(emotionID) ? agent_i.currentEmotions[emotionID] : null;

            if (em_i == null) {
                return null;
            }

            float charisma_i = agent_i.GetPersonality().charisma;
            float receptivity_j = agent_j.GetPersonality().receptivity;
            float contagionFactor = CalculateContagionFactor(charisma_i, receptivity_j);

            if (contagionFactor < threshold) {
                return null;
            }

            Emotion em_j = agent_j.currentEmotions.ContainsKey(emotionID) ? agent_j.currentEmotions[emotionID] : null;

            ProcessEmotion(agent_j, emotionID, em_i, contagionFactor, ref em_j);

            return em_j;
        }

        private static void ProcessEmotion(Agent agent_j, int emotionID, Emotion em_i, float contagionFactor, ref Emotion em_j) {
            if (em_j != null) {
                UpdateExistingEmotion(em_j, em_i, contagionFactor);
            } else {
                em_j = CreateNewEmotion(em_i, contagionFactor, agent_j);
                agent_j.currentEmotions.Add(emotionID, em_j);
            }
        }

        private static float CalculateContagionFactor(float charisma_i, float receptivity_j) {
            return charisma_i * receptivity_j;
        }

        private static void UpdateExistingEmotion(Emotion em_j, Emotion em_i, float contagionFactor) {
            float newIntensity = em_j.intensity + em_i.intensity * contagionFactor;
            float newDecay = em_i.intensity > em_j.intensity ? em_i.decay : em_j.decay;
            newIntensity = Math.Clamp(newIntensity, 0, 1);
            em_j.intensity = newIntensity;
            em_j.decay = newDecay;
        }

        private static Emotion CreateNewEmotion(Emotion em_i, float contagionFactor, Agent producer) {
            float newIntensity = em_i.intensity * contagionFactor;
            newIntensity = Math.Clamp(newIntensity, 0, 1);
            return new Emotion(em_i.name, em_i.predicate, producer, newIntensity, em_i.decay);
        }
    }

}