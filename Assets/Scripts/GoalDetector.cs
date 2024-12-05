using UnityEngine;
using System.Collections;

public class GoalDetector : MonoBehaviour
{
    public string scoringPlayer; // "Player1" or "Player2"

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Puck"))
        {
            // Update score
            bool done = ScoreManager.Instance.AddScore(scoringPlayer);

            // Start the coroutine to wait before resetting
            StartCoroutine(WaitAndReset(done));
        }
    }

    private IEnumerator WaitAndReset(bool resetScore)
    {
        // Wait for 2 seconds
        yield return new WaitForSeconds(3);

        // Reset puck and pushers position
        GameManager.Instance.ResetPositions();

        if (resetScore)
            ScoreManager.Instance.ResetScore();
    }
}
