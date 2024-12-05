using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public int P1Score = 0;
    public int P2Score = 0;

    public TextMeshProUGUI P1ScoreText;
    public TextMeshProUGUI P2ScoreText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool AddScore(string scoringPlayer)
    {
        if (scoringPlayer == "Player1")
        {
            P1Score++;
            P1ScoreText.text = P1Score.ToString();
        }
        else if (scoringPlayer == "Player2")
        {
            P2Score++;
            P2ScoreText.text = P2Score.ToString();
        }

        if (P1Score >= 7 || P2Score >= 7)
        {
            return true;
        }

        return false;
    }

    public void ResetScore()
    {
        P1Score = 0;
        P1ScoreText.text = P1Score.ToString();
        P2Score = 0;
        P2ScoreText.text = P2Score.ToString();
    }
}
