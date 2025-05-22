using UnityEngine;

/// <summary>
/// Puck–table–pusher collision logic.
/// Table walls: elastic + additive kick + guaranteed un-sticking speed.
/// Table floor: wipe Y velocity.
/// Pusher: elastic + absorbs pusher velocity.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PuckCollisionHandler : MonoBehaviour
{
    [Header("Wall feel (side walls only)")]
    [Tooltip("Meters / s added along the wall’s outward normal.")]
    [SerializeField] private float wallBounceKick = 0.4f;

    [Tooltip("If the post-bounce speed is below this, force it outward at this speed to avoid sticking.")]
    [SerializeField] private float unstickSpeed = 0.15f;

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    /* ─────────────────── ENTER & STAY ─────────────────── */

    private void OnCollisionEnter(Collision col) => Dispatch(col);

    private void Dispatch(Collision col)
    {
        if (col.gameObject.CompareTag("Table")) HandleTableCollision(col);
        else if (col.gameObject.CompareTag("Orange")||col.gameObject.CompareTag("Blue")) HandlePusherCollision(col);
    }

    /* ─────────────────── TABLE ─────────────────── */

    private void HandleTableCollision(Collision col)
    {
        Vector3 normal = col.contacts[0].normal;
        Vector3 v = rb.linearVelocity;                     // cache

        /* ─── Side wall ─── */
        if (Mathf.Abs(normal.y) < 0.99f)
        {
            Vector3 nXZ = new Vector3(normal.x, 0f, normal.z);
            nXZ.Normalize();

            // Decompose & reflect
            Vector3 vXZ = new Vector3(v.x, 0f, v.z);
            Vector3 vPerp = Vector3.Project(vXZ, nXZ);
            Vector3 vPara = vXZ - vPerp;
            Vector3 refl = vPara - vPerp;                // perfect bounce

            // Add the extra kick
            refl += nXZ * wallBounceKick;

            // Guarantee a minimum outward speed
            if (refl.sqrMagnitude < unstickSpeed * unstickSpeed)
                refl = nXZ * unstickSpeed;

            // Re-assemble the final velocity
            v.x = refl.x;
            v.z = refl.z;
            v.y = 0f;                                     // stay on table
            rb.linearVelocity = v;
        }
        /* ─── Floor ─── */
        else
        {
            v.y = 0f;
            rb.linearVelocity = v;
        }
    }

    /* ─────────────────── PUSHER ─────────────────── */

    private void HandlePusherCollision(Collision col)
    {
        Rigidbody pusher = col.rigidbody;
        if (pusher == null || pusher.isKinematic) return;

        Vector3 n = col.contacts[0].normal.normalized;

        float m1 = rb.mass, m2 = pusher.mass;
        Vector3 v1 = rb.linearVelocity,
                v2 = pusher.linearVelocity;

        // Decompose along the normal
        float v1n = Vector3.Dot(v1, n);
        float v2n = Vector3.Dot(v2, n);
        Vector3 v1t = v1 - v1n * n;

        // 1-D elastic formula
        float v1nAfter = (v1n * (m1 - m2) + 2f * m2 * v2n) / (m1 + m2);

        // Rebuild velocity & absorb pusher velocity along *same* normal
        Vector3 newVel = v1t + v1nAfter * n + n * v2.magnitude;

        newVel.y = 0f;                                   // lock to table
        rb.linearVelocity = newVel;
    }
}
