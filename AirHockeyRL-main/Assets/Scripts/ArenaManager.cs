using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

[DisallowMultipleComponent]
public class ArenaManager : MonoBehaviour
{
    [Header("Scene References (drag these in Inspector)")]
    [SerializeField] public Transform puck         = null;
    [SerializeField] private PusherAgent bluePusher   = null; // tagged "Blue"
    [SerializeField] private PusherAgent orangePusher = null; // tagged "Orange"

    // (Optional) if your AIController lives on the same GameObject as PusherAgent
    [Header("attach one AIController to each prefab; leave disabled by default")]
    [SerializeField] private AIController blueAI     = null;
    [SerializeField] private AIController orangeAI   = null;

    // cached start‐pos + Rigidbodies
    Vector3   puckStart, blueStart, orangeStart;
    Rigidbody puckRb,   blueRb,   orangeRb;

    // prevents multiple simultaneous resets
    bool resetPending = false;

    private void Awake()
    {
        // 1) sanity‐check Inspector wiring
        if (puck == null || bluePusher == null || orangePusher == null)
        {
            Debug.LogError("[ArenaManager] Please assign Puck, BluePusher, and OrangePusher in the Inspector.", this);
            enabled = false;
            return;
        }

        // 2) cache initial positions
        puckStart    = puck.localPosition;
        blueStart    = bluePusher.transform.localPosition;
        orangeStart  = orangePusher.transform.localPosition;

        // 3) cache Rigidbodies
        puckRb    = SafeGetRigidbody(puck);
        blueRb    = SafeGetRigidbody(bluePusher.transform);
        orangeRb  = SafeGetRigidbody(orangePusher.transform);

        // 4) give each PusherAgent its context & “opponent” reference
        bluePusher.InitContext(this, orangePusher);
        orangePusher.InitContext(this, bluePusher);

        // 5) if user forgot to assign AIControllers in Inspector, try to grab them from the same GameObject
        if (blueAI    == null) blueAI    = bluePusher.GetComponent<AIController>();
        if (orangeAI  == null) orangeAI  = orangePusher.GetComponent<AIController>();

        // 6) make sure both AIControllers start disabled
        if (blueAI   != null) blueAI.enabled   = false;
        if (orangeAI != null) orangeAI.enabled = false;

        // 7) first episode’s mode‐flip (90% ML, 10% AI)
        RandomiseBothSides();
    }

    /// <summary>
    /// Called by each PusherAgent (in OnActionReceived when StepCount >= MaxStep) 
    /// to request an immediate reset next fixed update.
    /// </summary>
    public void RequestResetFromAgent()
    {
        if (resetPending) return;
        resetPending = true;
        StartCoroutine(ResetAtNextFixedUpdate());
    }

    IEnumerator ResetAtNextFixedUpdate()
    {
        // wait exactly one FixedUpdate so both agents can finish their EndEpisode() first
        yield return new WaitForFixedUpdate();
        ResetArena();
        resetPending = false;
    }

    /// <summary>
    /// Teleport puck & both pushers back to their start positions, zero velocities, then 
    /// randomise each side’s controller for the next episode (90% ML, 10% AI).
    /// </summary>
    public void ResetArena()
    {
        // teleport & zero velocities
        Teleport(puck,           puckRb,   puckStart);
        Teleport(bluePusher.transform,     blueRb,   blueStart);
        Teleport(orangePusher.transform,   orangeRb, orangeStart);

        // choose new modes for next episode
        RandomiseBothSides();
    }

    private void RandomiseBothSides()
    {
        // 90% “use NN (BehaviorName = AirHockeyBrain, teamId will be set in the prefab)”; 
        // 10% “use AIController instead.”

        ApplyModeToOneSide(
            go:      bluePusher.gameObject,
            ai:      blueAI,
            defaultTeamId: 0  // ensure your Blue behavior parameters have TeamId=0 in prefab
        );

        ApplyModeToOneSide(
            go:      orangePusher.gameObject,
            ai:      orangeAI,
            defaultTeamId: 1  // ensure your Orange behavior parameters have TeamId=1 in prefab
        );
    }

    /// <summary>
    /// If random >= 0.90, switch to AIController; otherwise keep ML‐Agents “AirHockeyBrain” on the existing team ID.
    /// </summary>
    private void ApplyModeToOneSide(GameObject go, AIController ai, int defaultTeamId)
    {
        float r = Random.value;
        bool useAI = (r >= 0.90f);

        var bp = go.GetComponent<BehaviorParameters>();
        var dr = go.GetComponent<DecisionRequester>();
        var ag = go.GetComponent<Agent>();

        if (bp == null || dr == null || ag == null)
        {
            Debug.LogWarning($"[ArenaManager] {go.name} is missing one of (BehaviorParameters / DecisionRequester / Agent).", go);
            return;
        }

        if (useAI)
        {
            // disable ML‐Agents pipeline
            bp.enabled = false;
            dr.enabled = false;
            ag.enabled = false;

            // enable AIController if it exists
            if (ai != null) ai.enabled = true;
        }
        else
        {
            // force both agents to use exactly the same BehaviorName ("AirHockeyBrain")
            // and keep their TeamId that was set in prefab (0 for Blue, 1 for Orange).
            bp.BehaviorName = "AirHockeyBrain";
            bp.TeamId       = defaultTeamId;

            // enable ML‐Agents pipeline components
            bp.enabled = true;
            dr.enabled = true;
            ag.enabled = true;

            // disable AIController if it exists
            if (ai != null) ai.enabled = false;
        }

        // zero out any lingering velocity
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    #region ──── Utility Methods ────────────────────────────

    static Rigidbody SafeGetRigidbody(Transform t)
    {
        if (!t.TryGetComponent<Rigidbody>(out var rb))
            Debug.LogWarning($"[ArenaManager] {t.name} has no Rigidbody.", t);
        return rb;
    }

    static void Teleport(Transform t, Rigidbody rb, Vector3 targetLocalPos)
    {
        t.localPosition = targetLocalPos;
        if (rb != null)
        {
            rb.linearVelocity        = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    #endregion
}
