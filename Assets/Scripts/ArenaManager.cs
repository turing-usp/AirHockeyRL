using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;

[DisallowMultipleComponent]
public class ArenaManager : MonoBehaviour
{
    [Header("Scene References (assign in Inspector)")]
    [SerializeField] public Transform puck = null;
    [SerializeField] private PusherAgent bluePusher = null;   // tag: Blue
    [SerializeField] private PusherAgent orangePusher = null; // tag: Orange

    [Header("Optional AI Controllers")]
    [SerializeField] private AIController blueAI = null;
    [SerializeField] private AIController orangeAI = null;

    [Header("Behavior Names")]
    [SerializeField] private bool splitBehaviorNames = false;
    [SerializeField] private string sharedBehaviorName = "AirHockeyBrain";
    [SerializeField] private string blueBehaviorName = "BlueBrain";
    [SerializeField] private string orangeBehaviorName = "OrangeBrain";

    [Header("Environment Parameters")]
    [SerializeField] private bool useEnvironmentBehaviorOverrides = true;
    [SerializeField] private string splitBehaviorParam = "split_behavior_names";
    [SerializeField] private string trainVsDeterministicAIParam = "train_vs_deterministic_ai";
    [SerializeField] private string deterministicAIProbabilityParam = "deterministic_ai_probability";

    [Header("Training Opponent Mix")]
    [SerializeField] private bool trainVsDeterministicAI = false;
    [Range(0f, 1f)]
    [SerializeField] private float deterministicAIProbability = 0.5f;

    // cached start positions and rigidbodies
    private Vector3 puckStart;
    private Vector3 blueStart;
    private Vector3 orangeStart;

    private Rigidbody puckRb;
    private Rigidbody blueRb;
    private Rigidbody orangeRb;

    // prevents duplicated reset requests on the same frame
    private bool resetPending;

    private void Awake()
    {
        if (puck == null || bluePusher == null || orangePusher == null)
        {
            Debug.LogError("[ArenaManager] Assign Puck, BluePusher and OrangePusher in Inspector.", this);
            enabled = false;
            return;
        }

        ResolveBehaviorOverrides();

        puckStart = puck.localPosition;
        blueStart = bluePusher.transform.localPosition;
        orangeStart = orangePusher.transform.localPosition;

        puckRb = SafeGetRigidbody(puck);
        blueRb = SafeGetRigidbody(bluePusher.transform);
        orangeRb = SafeGetRigidbody(orangePusher.transform);

        bluePusher.InitContext(this, orangePusher);
        orangePusher.InitContext(this, bluePusher);

        if (blueAI == null) blueAI = bluePusher.GetComponent<AIController>();
        if (orangeAI == null) orangeAI = orangePusher.GetComponent<AIController>();

        if (blueAI != null) blueAI.enabled = false;
        if (orangeAI != null) orangeAI.enabled = false;

        RandomiseBothSides();
    }

    private void ResolveBehaviorOverrides()
    {
        if (!useEnvironmentBehaviorOverrides) return;

        EnvironmentParameters env = Academy.Instance.EnvironmentParameters;

        float split = env.GetWithDefault(splitBehaviorParam, splitBehaviorNames ? 1f : 0f);
        splitBehaviorNames = split > 0.5f;

        float trainVsAI = env.GetWithDefault(trainVsDeterministicAIParam, trainVsDeterministicAI ? 1f : 0f);
        trainVsDeterministicAI = trainVsAI > 0.5f;

        float probability = env.GetWithDefault(deterministicAIProbabilityParam, deterministicAIProbability);
        deterministicAIProbability = Mathf.Clamp01(probability);
    }

    /// <summary>
    /// Request one reset at the next FixedUpdate. Useful to avoid duplicate resets
    /// when both agents call EndEpisode in the same frame.
    /// </summary>
    public void RequestResetFromAgent()
    {
        if (resetPending) return;
        resetPending = true;
        StartCoroutine(ResetAtNextFixedUpdate());
    }

    private IEnumerator ResetAtNextFixedUpdate()
    {
        yield return new WaitForFixedUpdate();
        ResetArena();
        resetPending = false;
    }

    public void ResetArena()
    {
        Teleport(puck, puckRb, puckStart);
        Teleport(bluePusher.transform, blueRb, blueStart);
        Teleport(orangePusher.transform, orangeRb, orangeStart);

        RandomiseBothSides();
    }

    private void RandomiseBothSides()
    {
        string blueName = splitBehaviorNames ? blueBehaviorName : sharedBehaviorName;
        string orangeName = splitBehaviorNames ? orangeBehaviorName : sharedBehaviorName;

        if (trainVsDeterministicAI)
        {
            bool againstAIThisEpisode = Random.value < deterministicAIProbability;
            if (againstAIThisEpisode)
            {
                // One side uses deterministic AI, the opposite side remains ML.
                bool blueUsesAI = Random.value < 0.5f;
                ApplyModeToOneSide(bluePusher.gameObject, blueAI, 0, blueName, blueUsesAI);
                ApplyModeToOneSide(orangePusher.gameObject, orangeAI, 1, orangeName, !blueUsesAI);
                return;
            }

            // Fallback episode: both sides ML (self-play / dual training).
            ApplyModeToOneSide(bluePusher.gameObject, blueAI, 0, blueName, false);
            ApplyModeToOneSide(orangePusher.gameObject, orangeAI, 1, orangeName, false);
            return;
        }

        ApplyModeToOneSideRandom(bluePusher.gameObject, blueAI, 0, blueName);
        ApplyModeToOneSideRandom(orangePusher.gameObject, orangeAI, 1, orangeName);
    }

    /// <summary>
    /// 90% ML, 10% classic AI.
    /// </summary>
    private void ApplyModeToOneSideRandom(GameObject go, AIController ai, int defaultTeamId, string behaviorName)
    {
        bool useAI = Random.value >= 0.90f;
        ApplyModeToOneSide(go, ai, defaultTeamId, behaviorName, useAI);
    }

    private void ApplyModeToOneSide(
        GameObject go,
        AIController ai,
        int defaultTeamId,
        string behaviorName,
        bool useAI
    )
    {

        BehaviorParameters bp = go.GetComponent<BehaviorParameters>();
        DecisionRequester dr = go.GetComponent<DecisionRequester>();
        Agent ag = go.GetComponent<Agent>();

        if (bp == null || dr == null || ag == null)
        {
            Debug.LogWarning(
                $"[ArenaManager] {go.name} is missing BehaviorParameters/DecisionRequester/Agent.",
                go
            );
            return;
        }

        if (useAI)
        {
            bp.enabled = false;
            dr.enabled = false;
            ag.enabled = false;
            if (ai != null) ai.enabled = true;
        }
        else
        {
            bp.BehaviorName = behaviorName;
            bp.TeamId = defaultTeamId;

            bp.enabled = true;
            dr.enabled = true;
            ag.enabled = true;
            if (ai != null) ai.enabled = false;
        }

        if (go.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private static Rigidbody SafeGetRigidbody(Transform t)
    {
        if (!t.TryGetComponent(out Rigidbody rb))
        {
            Debug.LogWarning($"[ArenaManager] {t.name} has no Rigidbody.", t);
        }
        return rb;
    }

    private static void Teleport(Transform t, Rigidbody rb, Vector3 targetLocalPos)
    {
        t.localPosition = targetLocalPos;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
