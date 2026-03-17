using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PusherAgent : Agent
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 2.5f;
    [SerializeField] private float accelRate = 25f;
    [SerializeField] private float decelRate = 18f;

    [Header("Borders (local X)")]
    [SerializeField] private float xMin = -3f; // overridden in Initialize by tag
    [SerializeField] private float xMax = 3f;  // overridden in Initialize by tag
    [SerializeField] private float halfTableWidth = 2f;

    [Header("Rewards")]
    [SerializeField] private bool useEnvironmentRewardOverrides = true;
    [SerializeField] private float alivePenalty = -0.001f;
    [SerializeField] private float puckSideRewardMagnitude = 0.00015f;
    [SerializeField] private float puckTouchReward = 0.0005f;
    [SerializeField] private float opponentTouchPenalty = -0.0005f;
    [SerializeField] private float timeoutPenalty = -5f;

    [Header("Reward Param Names")]
    [SerializeField] private string alivePenaltyParam = "reward_step_alive";
    [SerializeField] private string puckSideRewardParam = "reward_puck_side";
    [SerializeField] private string puckTouchRewardParam = "reward_puck_touch";
    [SerializeField] private string timeoutPenaltyParam = "reward_timeout";

    /* refs filled by ArenaManager */
    [HideInInspector] public Transform puck;
    [HideInInspector] public Rigidbody puckRb;
    [HideInInspector] public PusherAgent opponent;

    private Rigidbody rb;
    private ArenaManager arena;
    private int mirror = 1; // +1 Blue, -1 Orange
    private bool rewardOverridesLoaded;

    public void InitContext(ArenaManager ctx, PusherAgent opp)
    {
        arena = ctx;
        opponent = opp;
        puck = ctx.puck;
        puckRb = ctx.puck.GetComponent<Rigidbody>();
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        mirror = CompareTag("Orange") ? -1 : 1;

        if (CompareTag("Blue"))
        {
            xMin = -2f;
            xMax = 0f;
        }
        else
        {
            xMin = 0f;
            xMax = 2f;
        }

        LoadRewardOverrides();
    }

    public override void OnEpisodeBegin()
    {
        LoadRewardOverrides();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void LoadRewardOverrides()
    {
        if (rewardOverridesLoaded || !useEnvironmentRewardOverrides) return;

        EnvironmentParameters env = Academy.Instance.EnvironmentParameters;
        alivePenalty = env.GetWithDefault(alivePenaltyParam, alivePenalty);
        puckSideRewardMagnitude = Mathf.Abs(env.GetWithDefault(puckSideRewardParam, puckSideRewardMagnitude));
        puckTouchReward = env.GetWithDefault(puckTouchRewardParam, puckTouchReward);
        timeoutPenalty = env.GetWithDefault(timeoutPenaltyParam, timeoutPenalty);

        // keep symmetric sign convention: opponent gets negative when this agent touches puck.
        opponentTouchPenalty = -Mathf.Abs(puckTouchReward);
        rewardOverridesLoaded = true;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(NormPos(transform.localPosition));
        sensor.AddObservation(NormVel(rb.linearVelocity));
        sensor.AddObservation(mirror == 1 ? 0 : 1);

        if (opponent != null)
        {
            sensor.AddObservation(NormPos(opponent.transform.localPosition));
            sensor.AddObservation(NormVel(opponent.GetComponent<Rigidbody>().linearVelocity));
        }
        else
        {
            sensor.AddObservation(new float[4]);
        }

        sensor.AddObservation(NormPos(puck.localPosition));
        sensor.AddObservation(NormVel(puckRb.linearVelocity));
    }

    private Vector2 NormPos(Vector3 w)
    {
        float normalizedX = mirror * ((w.x / halfTableWidth) * 2 + (mirror == 1 ? 1 : -1));
        float normalizedZ = mirror * w.z / halfTableWidth;
        return new Vector2(normalizedX, normalizedZ);
    }

    private Vector2 NormVel(Vector3 v)
    {
        return new Vector2(mirror * v.x / maxSpeed, mirror * v.z / maxSpeed);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveByAction(actions.DiscreteActions[0]);

        AddReward(alivePenalty);

        float puckX = puck.localPosition.x;
        bool puckOnAgentSide = (mirror == 1 && puckX < 0) || (mirror == -1 && puckX > 0);
        AddReward(puckOnAgentSide ? -puckSideRewardMagnitude : puckSideRewardMagnitude);

        if (StepCount >= MaxStep - 1)
        {
            AddReward(timeoutPenalty);
            EndEpisode();

            if (opponent != null)
            {
                opponent.AddReward(timeoutPenalty);
                opponent.EndEpisode();
            }

            if (arena != null)
            {
                arena.ResetArena();
            }
        }
    }

    private void MoveByAction(int action)
    {
        Vector3 dir = action switch
        {
            1 => Vector3.forward,
            2 => Vector3.back,
            3 => Vector3.left,
            4 => Vector3.right,
            _ => Vector3.zero
        };

        dir.x *= mirror;
        dir.z *= mirror;

        Vector3 target = dir * maxSpeed;
        float rate = dir == Vector3.zero ? decelRate : accelRate;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, target, rate * Time.fixedDeltaTime);

        Vector3 p = transform.localPosition;
        if (p.x < xMin)
        {
            p.x = xMin;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
            rb.MovePosition(transform.parent.TransformPoint(p));
        }
        else if (p.x > xMax)
        {
            p.x = xMax;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);
            rb.MovePosition(transform.parent.TransformPoint(p));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Puck")) return;

        AddReward(puckTouchReward);
        if (opponent != null)
        {
            opponent.AddReward(opponentTouchPenalty);
        }
    }
}
