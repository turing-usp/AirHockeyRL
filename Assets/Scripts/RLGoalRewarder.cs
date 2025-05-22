using UnityEngine;
using Unity.MLAgents;

/// Handles goal-reward logic for **one** arena (placed on each goal trigger).
[RequireComponent(typeof(Collider))]
public class RLGoalRewarder : MonoBehaviour
{
    [Header("Who scores when this trigger is hit")]
    public string scoringPlayer = "Player1";   // "Player1" or "Player2"

    [Header("Reward values")]
    [SerializeField] float rewardForScoring  = +1f;
    [SerializeField] float penaltyForConcede = -1f;

    ArenaManager arena;        // parent context

    void Awake() => arena = GetComponentInParent<ArenaManager>();

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Puck")) return;

        /* 1 – reset only this table */
        arena.ResetArena();

        /* 2 – reward or penalise the two agents that belong to this arena */
        foreach (var agent in arena.GetComponentsInChildren<PusherAgent>())
        {
            bool scored =
                (scoringPlayer == "Player1" && agent.CompareTag("Blue")) ||
                (scoringPlayer == "Player2" && agent.CompareTag("Orange"));

            agent.AddReward(scored ? rewardForScoring : penaltyForConcede);
            agent.EndEpisode();
        }
    }
}
