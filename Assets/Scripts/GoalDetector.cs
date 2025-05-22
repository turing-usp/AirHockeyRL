using UnityEngine;

/// Goal trigger that updates ONLY the arena it belongs to
/// (no singletons, no cross-table teleporting).
[RequireComponent(typeof(Collider))]
public class GoalDetector : MonoBehaviour
{
    [Tooltip("“Player1” means the blue side scores here, “Player2” = orange")]
    public string scoringPlayer = "Player1";

    ArenaManager  arena;        // local context
    ScoreManager  score;        // local score board

    void Awake()
    {
        arena = GetComponentInParent<ArenaManager>();
        score = GetComponentInParent<ScoreManager>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Puck")) return;

        /* 1 – update THIS table’s score UI */
        bool reachedLimit = score.AddScore(scoringPlayer);

        /* 2 – reset ONLY this arena */
        arena.ResetArena();

        /* 3 – if someone won, zero this table’s score */
        if (reachedLimit) score.ResetScore();
    }
}
