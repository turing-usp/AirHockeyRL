using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Scene objects")]
    public Transform puck;
    public Transform player1Pusher;     // keyboard player (blue)
    public Transform player2Pusher;     // AI (orange)

    private Vector3 puckStartPos;
    private Vector3 p1StartPos;
    private Vector3 p2StartPos;

    /* cached rigidbodies */
    private Rigidbody puckRb;
    private Rigidbody p1Rb;
    private Rigidbody p2Rb;

    /* ─────────── Singleton ─────────── */
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        /* Cache start positions */
        puckStartPos = puck.position;
        p1StartPos   = player1Pusher.position;
        p2StartPos   = player2Pusher.position;

        /* Cache rigidbodies */
        puckRb = puck.GetComponent<Rigidbody>();
        p1Rb   = player1Pusher.GetComponent<Rigidbody>();
        p2Rb   = player2Pusher.GetComponent<Rigidbody>();
    }

    /* ─────────── Manual reset key ─────────── */
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            ResetPositions();
    }

    /* ─────────── Public API ─────────── */
    public void ResetPositions()
    {
        /* Puck */
        puck.position          = puckStartPos;
        puckRb.linearVelocity        = Vector3.zero;
        puckRb.angularVelocity = Vector3.zero;

        /* Player pusher */
        player1Pusher.position = p1StartPos;
        p1Rb.linearVelocity          = Vector3.zero;
        p1Rb.angularVelocity   = Vector3.zero;

        /* AI pusher */
        player2Pusher.position = p2StartPos;
        p2Rb.linearVelocity          = Vector3.zero;
        p2Rb.angularVelocity   = Vector3.zero;
    }
}
