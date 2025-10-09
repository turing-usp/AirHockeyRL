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
    [SerializeField] private float wallBounceKick = 1.0f;
    [Header("Table Root (arraste o Transform da mesa)")]
    [SerializeField] private Transform tableRoot = null;

    [Tooltip("If the post-bounce speed is below this, force it outward at this speed to avoid sticking.")]
    [SerializeField] private float unstickSpeed = 0.15f;

    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();
    private enum WallSide { Left, Right, Front, Back, Floor }

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
    if (tableRoot == null)
    {
        Debug.LogWarning("[PuckCollisionHandler] Arraste o TableRoot no Inspector.", this);
        return;
    }

    // 1) Converta a normal mundial para o espaço local da mesa
    Vector3 nLocal = tableRoot.InverseTransformDirection(col.contacts[0].normal).normalized;
    WallSide side  = GetSide(nLocal);

    Vector3 v = rb.linearVelocity; // cache

    switch (side)
    {
        case WallSide.Floor:               // ---------- PISO ----------
            v.y = 0f;
            rb.linearVelocity = v;
            break;

        /* ---------- PAREDES LATERAIS ---------- */
        case WallSide.Left:
        case WallSide.Right:
        case WallSide.Front:
        case WallSide.Back:
        {
            // normal 2-D no plano XZ local
            Vector3 nXZ = new Vector3(nLocal.x, 0f, nLocal.z).normalized;

            // decompor velocidade, refletir perfeitamente e adicionar "kick"
            Vector3 vXZ   = new Vector3(v.x, 0f, v.z);
            Vector3 vPerp = Vector3.Project(vXZ, nXZ);   // componente perpendicular
            Vector3 vPara = vXZ - vPerp;                 // componente paralela

            Vector3 refl  = vPara - vPerp;               // reflexão elástica
            refl += nXZ * wallBounceKick;                // chute extra (SOMA, não multiplicação)

            // garantir velocidade mínima para não grudar
            if (refl.sqrMagnitude < unstickSpeed * unstickSpeed)
                refl = nXZ * unstickSpeed;

            // remontar vetor 3-D (mantém Y zero)
            v.x = refl.x;
            v.z = refl.z;
            v.y = 0f;
            rb.linearVelocity = v;
            break;
        }
    }
}

/* ------------------------------------------------------- */
/* -------------  helper que decide o lado --------------- */
private WallSide GetSide(Vector3 nLocal)
{
    // Piso se a componente Y domina
    if (Mathf.Abs(nLocal.y) > 0.7f)
        return WallSide.Floor;

    // Decide se a normal aponta mais para X ou Z
    if (Mathf.Abs(nLocal.x) > Mathf.Abs(nLocal.z))
        return nLocal.x > 0f ? WallSide.Right : WallSide.Left;
    else
        return nLocal.z > 0f ? WallSide.Back  : WallSide.Front;
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
