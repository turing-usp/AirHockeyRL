using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Goal-trigger reward logic for one arena.
/// Place this on each goal trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RLGoalRewarder : MonoBehaviour
{
    [Header("Who scores when this trigger is hit")]
    public string scoringPlayer = "Player1"; // "Player1" or "Player2"

    [Header("Reward Values")]
    [SerializeField] private float rewardForScoring = 1f;
    [SerializeField] private float penaltyForConcede = -1f;

    [Header("Environment Reward Overrides")]
    [SerializeField] private bool useEnvironmentRewardOverrides = true;
    [SerializeField] private string rewardForScoringParam = "reward_goal_scored";
    [SerializeField] private string penaltyForConcedeParam = "reward_goal_conceded";

    private ArenaManager arena;
    private bool overridesLoaded;

    private void Awake()
    {
        arena = GetComponentInParent<ArenaManager>();
    }

    private void Start()
    {
        LoadRewardOverrides();
    }

    private void LoadRewardOverrides()
    {
        if (overridesLoaded || !useEnvironmentRewardOverrides) return;

        EnvironmentParameters env = Academy.Instance.EnvironmentParameters;
        rewardForScoring = env.GetWithDefault(rewardForScoringParam, rewardForScoring);
        penaltyForConcede = env.GetWithDefault(penaltyForConcedeParam, penaltyForConcede);
        overridesLoaded = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Puck")) return;

        LoadRewardOverrides();
        arena.ResetArena();

        foreach (PusherAgent agent in arena.GetComponentsInChildren<PusherAgent>())
        {
            bool scored =
                (scoringPlayer == "Player1" && agent.CompareTag("Blue")) ||
                (scoringPlayer == "Player2" && agent.CompareTag("Orange"));

            agent.AddReward(scored ? rewardForScoring : penaltyForConcede);
            agent.EndEpisode();
        }
    }
}
