using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Transform puck;
    public Transform player1Pusher;
    public Transform player2Pusher;
    private Vector3 puckStartPos;
    private Vector3 player1StartPos;
    private Vector3 player2StartPos;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        puckStartPos = puck.position;
        player1StartPos = player1Pusher.position;
        player2StartPos = player2Pusher.position;
    }

    private void Update()
    {
        // Check for forced reset key
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPositions();
        }
    }

    public void ResetPositions()
    {
        // Reset puck
        puck.position = puckStartPos;
        Rigidbody puckRb = puck.GetComponent<Rigidbody>();
        puckRb.linearVelocity = Vector3.zero;
        puckRb.angularVelocity = Vector3.zero;

        // Reset player pusher
        player1Pusher.position = player1StartPos;

        // Reset AI pusher
        player2Pusher.position = player2StartPos;
    }
}
