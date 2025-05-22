// Scripts\PusherController.cs
using UnityEngine;
// Removed ML-Agents usings as they weren't used here

[RequireComponent(typeof(Rigidbody))]
public class PusherController : MonoBehaviour
{
    /* ───────────── Tunables ───────────── */

    [Header("Speed (m/s)")]
    [SerializeField] private float maxSpeed = 3f;

    [Header("Feel (m/s²)")]
    [SerializeField] private float accelRate = 25f;
    [SerializeField] private float decelRate = 18f;

    // --- Changed Header to LOCAL X ---
    [Header("Invisible borders (LOCAL X relative to parent)")]
    [SerializeField] private float localXMin = -3f; // Assumes pusher is child of env root
    [SerializeField] private float localXMax = 3f; // Assumes pusher is child of env root

    /* ───────────── Internals ───────────── */

    private Rigidbody rb;
    private Camera cam;
    private int tableLayerMask;
    private Transform environmentRoot; // Reference to the parent "Environment" object

    /* ───────────── Lifecycle ───────────── */

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main; // Assuming one main camera is okay

        // Find the parent Environment object tagged "Env"
        environmentRoot = GetEnvironmentRoot(transform);
         if (environmentRoot == null)
        {
            Debug.LogError("PusherController could not find parent Environment object with tag 'Env'.", this);
        }


        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                         RigidbodyConstraints.FreezeRotation;

        int tableLayer = LayerMask.NameToLayer("Table");
        tableLayerMask = 1 << tableLayer; // Only hit objects on the "Table" layer
    }

     // Helper to find the root Environment object
    Transform GetEnvironmentRoot(Transform currentTransform)
    {
        Transform parent = currentTransform.parent;
        while (parent != null)
        {
            if (parent.CompareTag("Env")) // Check if the parent has the "Env" tag
            {
                return parent;
            }
            parent = parent.parent; // Go up one level
        }
        return null; // Not found
    }


    /* ───────────── Physics loop ───────────── */

    private void FixedUpdate()
    {
        if (environmentRoot == null) return;

        /* 1. Handle input → desired velocity */
        Vector3 desiredVel = GetDesiredVelocity(); // This now returns world-space velocity target
        float rate = desiredVel.sqrMagnitude > 0.01f ? accelRate : decelRate;

        // Calculate new velocity based on acceleration/deceleration towards the desired world velocity
        Vector3 newVel = Vector3.MoveTowards(rb.linearVelocity,
                                             desiredVel, // Target the desired world velocity
                                             rate * Time.fixedDeltaTime);


        /* 2. Cap speed (magnitude of world velocity) */
        if (newVel.sqrMagnitude > maxSpeed * maxSpeed)
            newVel = newVel.normalized * maxSpeed;

        rb.linearVelocity = newVel; // Apply the calculated world velocity

        /* --- 3. Invisible X-borders using LOCAL position --- */
        // Assumes the PusherController's GameObject is a direct child of the environmentRoot
        Vector3 localPos = transform.localPosition;

        // Clamp the local X position
        float clampedLocalX = Mathf.Clamp(localPos.x, localXMin, localXMax);

        // If clamping occurred, update local position and potentially kill world X velocity
        if (!Mathf.Approximately(localPos.x, clampedLocalX))
        {
            localPos.x = clampedLocalX;
            transform.localPosition = localPos;

            // If pusher hit the boundary, zero out its world X velocity component relative to the boundary
             // Project velocity onto environment's right vector (local X axis)
             Vector3 localVelocity = environmentRoot.InverseTransformDirection(rb.linearVelocity);
             if ((clampedLocalX == localXMin && localVelocity.x < 0f) || (clampedLocalX == localXMax && localVelocity.x > 0f))
             {
                 localVelocity.x = 0f;
                 rb.linearVelocity = environmentRoot.TransformDirection(localVelocity); // Convert back to world velocity
             }
        }
         // --- Alternative Clamping if NOT direct child (less ideal) ---
         /*
         Vector3 worldPos = transform.position;
         Vector3 currentLocalPos = environmentRoot.InverseTransformPoint(worldPos);
         float originalLocalX = currentLocalPos.x;
         currentLocalPos.x = Mathf.Clamp(currentLocalPos.x, localXMin, localXMax);

         if (!Mathf.Approximately(originalLocalX, currentLocalPos.x))
         {
             transform.position = environmentRoot.TransformPoint(currentLocalPos);
             // Velocity adjustment logic would be more complex here
              Vector3 localVelocity = environmentRoot.InverseTransformDirection(rb.linearVelocity);
             if ((currentLocalPos.x == localXMin && localVelocity.x < 0f) || (currentLocalPos.x == localXMax && localVelocity.x > 0f))
             {
                 localVelocity.x = 0f;
                 rb.linearVelocity = environmentRoot.TransformDirection(localVelocity);
             }
         }
         */
    }


    /* ───────────── Input helpers ───────────── */

    // Returns desired velocity in WORLD space
    private Vector3 GetDesiredVelocity()
    {
        /* Mouse drag (priority) */
        if (Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // Raycast against the table layer mask
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tableLayerMask))
            {
                 // Ensure the hit object belongs to this pusher's environment
                Transform hitEnvironmentRoot = GetEnvironmentRoot(hit.transform);
                 if(hitEnvironmentRoot == this.environmentRoot) // Check if the hit table is part of *this* environment
                 {
                    // Calculate direction from pusher's current world position to the world hit point
                    Vector3 worldDir = hit.point - transform.position;
                    worldDir.y = 0f; // Keep movement planar

                    // Return the direction scaled by maxSpeed (desired world velocity)
                    // No need to normalize if we want faster movement for further clicks
                    // return worldDir.normalized * maxSpeed; // Capped speed towards point
                    // Alternative: Move faster the further the mouse is? (May need clamping)
                    float distance = worldDir.magnitude;
                    float speedFactor = Mathf.Clamp01(distance / 2.0f); // Example: Scale speed based on distance up to 2 units away
                    return worldDir.normalized * maxSpeed * speedFactor; // Or simply return worldDir * some_factor;
                 }
            }
        }

        /* Keyboard fallback */
        // Input axes are generally world-oriented (or view-oriented depending on project settings)
        // Assuming standard WASD/Arrows map to world X/Z:
        // Note: Horizontal axis usually maps to X, Vertical to Z in 3D. Adjust if needed.
        float horizontalInput = Input.GetAxis("Horizontal"); // Typically A/D or Left/Right Arrow
        float verticalInput = Input.GetAxis("Vertical");     // Typically W/S or Up/Down Arrow

        // Create world direction vector based on input
        Vector3 worldInputAxis = new Vector3(horizontalInput, 0f, verticalInput);
        return worldInputAxis.normalized * maxSpeed; // Return desired world velocity
    }


    /* ───────────── Elastic hit with puck ───────────── */
    // This physics calculation should be fine as it uses relative properties.
    private void OnCollisionEnter(Collision col)
    {
        Rigidbody other = col.rigidbody;
        // Ensure collision is with a puck *within the same environment*
        if (other == null || other.isKinematic || !col.gameObject.CompareTag("Puck")) return;

        // Optional check: Ensure the puck belongs to the same environment
        Transform puckEnv = GetEnvironmentRoot(other.transform);
        if (puckEnv != this.environmentRoot) return; // Ignore collisions with pucks from other environments


        Vector3 n = col.contacts[0].normal;
        ResolveElastic(rb, other, n); // Pass rigidbodies and normal
    }

    // Static helper method for elastic collision resolution
    private static void ResolveElastic(Rigidbody a, Rigidbody b, Vector3 n)
    {
        // Ensure normal is normalized
        n.Normalize();

        float m1 = a.mass;
        float m2 = b.mass;
        float totalMass = m1 + m2;

        // Check for zero mass or zero total mass
        if (Mathf.Approximately(m1, 0f) || Mathf.Approximately(m2, 0f) || Mathf.Approximately(totalMass, 0f)) return;

        Vector3 v1 = a.linearVelocity;
        Vector3 v2 = b.linearVelocity;

        // Decompose velocities into normal and tangential components
        float v1nScalar = Vector3.Dot(v1, n); // Velocity of a along normal
        float v2nScalar = Vector3.Dot(v2, n); // Velocity of b along normal

        Vector3 v1t = v1 - v1nScalar * n; // Tangential velocity of a
        Vector3 v2t = v2 - v2nScalar * n; // Tangential velocity of b

        // Calculate normal components after 1D elastic collision
        float v1nScalarAfter = (v1nScalar * (m1 - m2) + 2f * m2 * v2nScalar) / totalMass;
        float v2nScalarAfter = (v2nScalar * (m2 - m1) + 2f * m1 * v1nScalar) / totalMass;

        // Recombine tangential components (which don't change) with new normal components
        a.linearVelocity = v1t + v1nScalarAfter * n;
        b.linearVelocity = v2t + v2nScalarAfter * n;
    }
}