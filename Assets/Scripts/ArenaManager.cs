using UnityEngine;

/// One instance per “Environment” prefab.  
/// Stores the start-positions and can reset ONLY the objects that live
/// under the same prefab.
public class ArenaManager : MonoBehaviour
{
    [Header("Scene objects (drag from children)")]
    public Transform     puck;
    public PusherAgent   bluePusher;      // tag = “Blue”
    public PusherAgent   orangePusher;    // tag = “Orange”

    /* cached start-state */
    Vector3   puckStart, blueStart, orangeStart;
    Rigidbody puckRb,    blueRb,   orangeRb;

    void Awake()
    {
        /* remember local-space start poses */
        puckStart   = puck.localPosition;
        blueStart   = bluePusher.transform.localPosition;
        orangeStart = orangePusher.transform.localPosition;

        puckRb   = puck.GetComponent<Rigidbody>();
        blueRb   = bluePusher.GetComponent<Rigidbody>();
        orangeRb = orangePusher.GetComponent<Rigidbody>();

        /* give each agent a reference to THIS arena + its opponent */
        bluePusher  .InitContext(this, orangePusher);
        orangePusher.InitContext(this, bluePusher);
    }

    /// Teleport puck & both paddles back to start and zero their velocity.
    public void ResetArena()
    {
        puck.localPosition = puckStart;
        puckRb.linearVelocity = puckRb.angularVelocity = Vector3.zero;

        bluePusher.transform.localPosition = blueStart;
        blueRb.linearVelocity = blueRb.angularVelocity = Vector3.zero;

        orangePusher.transform.localPosition = orangeStart;
        orangeRb.linearVelocity = orangeRb.angularVelocity = Vector3.zero;
    }
}
