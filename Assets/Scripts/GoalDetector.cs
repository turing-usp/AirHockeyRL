using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    public string scoringPlayer; // "Player1" or "Player2"

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Puck"))
        {
            // Update score
            ScoreManager.Instance.AddScore(scoringPlayer);

            // Reset puck and pushers position
            GameManager.Instance.ResetPositions();
        }
    }
}
