// Scripts\AIController.cs
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Good practice to ensure Rigidbody exists
public class AIController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    [SerializeField] private int mass = 100; // Consider making this Rigidbody.mass if appropriate
    [SerializeField] private float speed = 3;

    // --- References relative to this environment ---
    [HideInInspector] // Hide if set by ArenaManager, otherwise assign manually in prefab
    public Transform puck;
    private Transform environmentRoot; // Reference to the parent "Environment" object

    // --- Boundaries in LOCAL X relative to environmentRoot ---
    // Note: These values might need adjustment depending on your environment prefab's layout.
    // For example, if the AI plays on the right half (positive Z usually),
    // its local X boundaries might be something like -1.5 to +1.5,
    // while its local Z stays positive. Adjust based on your setup.
    [Header("Boundaries (Local X)")]
    [SerializeField] private float localXMin = -1.5f; // Example value
    [SerializeField] private float localXMax = 1.5f; // Example value
    // Optional: Add local Z boundaries if needed
    // [SerializeField] private float localZMin = 0.5f;  // Example: AI stays in its half
    // [SerializeField] private float localZMax = 3.0f;  // Example: Max reach

    void Awake() // Use Awake for component fetching
    {
        _rigidbody = GetComponent<Rigidbody>();
        // Find the parent Environment object tagged "Env"
        environmentRoot = GetEnvironmentRoot(transform);
        if (environmentRoot == null)
        {
            Debug.LogError("AIController could not find parent Environment object with tag 'Env'.", this);
        }

        // --- How to get the puck reference? ---
        // Option 1: Assigned by ArenaManager (Preferred if you modify ArenaManager)
        //   // In ArenaManager.Awake():
        //   // aiControllerBlue = bluePusher.GetComponent<AIController>(); // Assuming AIController is on the same object as PusherAgent or similar
        //   // if (aiControllerBlue) aiControllerBlue.Initialize(this);
        //   // aiControllerOrange = orangePusher.GetComponent<AIController>();
        //   // if (aiControllerOrange) aiControllerOrange.Initialize(this);
        //
        //   // Add to AIController:
        //   // public void Initialize(ArenaManager arena) {
        //   //    this.puck = arena.puck;
        //   //    // Set boundaries based on which side the AI is on?
        //   // }

        // Option 2: Find it within the environment hierarchy (Simpler if AIController is separate)
        if (puck == null && environmentRoot != null) // Find puck if not assigned
        {
            Transform puckTransform = environmentRoot.Find("Puck"); // Assumes puck GameObject is named "Puck" and is a direct child
            if (puckTransform != null) {
                 puck = puckTransform;
            } else {
                 // Alternative search if nested deeper
                 PuckCollisionHandler puckScript = environmentRoot.GetComponentInChildren<PuckCollisionHandler>();
                 if (puckScript != null) {
                     puck = puckScript.transform;
                 } else {
                    Debug.LogError("AIController could not find the Puck within its environment.", this);
                 }
            }
        }
         if (puck == null) {
             Debug.LogError("AIController: Puck reference is not set!", this);
             this.enabled = false; // Disable script if puck is missing
             return;
         }
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


    void FixedUpdate() // Use FixedUpdate for Rigidbody physics manipulation
    {
        if (puck == null || environmentRoot == null) return; // Don't run if references are missing

        // --- Calculate target position in LOCAL space relative to the environment root ---
        // 1. Get puck's position in the environment's local space
        Vector3 puckLocalPos = environmentRoot.InverseTransformPoint(puck.position);
        // 2. Get AI's current position in the environment's local space
        Vector3 currentLocalPos = environmentRoot.InverseTransformPoint(transform.position); // Or just use transform.localPosition if AI is direct child

        // Target the puck's local position (but keep AI's Y level constant)
        Vector3 targetLocalPos = new Vector3(puckLocalPos.x, currentLocalPos.y, puckLocalPos.z);

        // --- (Optional) Modify target position based on AI strategy ---
        // Example: Maybe the AI should try to stay behind the puck relative to its goal?
        // targetLocalPos.z = Mathf.Clamp(targetLocalPos.z, localZMin, localZMax); // Example Z clamp

        // --- Calculate movement vector in LOCAL space ---
        float moveX = targetLocalPos.x - currentLocalPos.x;
        float moveZ = targetLocalPos.z - currentLocalPos.z;

        Vector3 localMoveVector = new Vector3(moveX, 0.0f, moveZ);

        // --- Convert local move vector to WORLD space direction for applying velocity ---
        Vector3 worldMoveDirection = environmentRoot.TransformDirection(localMoveVector.normalized);

        // Apply velocity in world space
        _rigidbody.linearVelocity = worldMoveDirection * speed;

        // --- Clamp position using LOCAL coordinates ---
        // Use transform.localPosition directly IF the AI is a direct child of environmentRoot
        // Otherwise, recalculate currentLocalPos if needed (though it was calculated above)
        Vector3 clampedLocalPos = transform.localPosition; // Assumes AI is child of environmentRoot
        clampedLocalPos.x = Mathf.Clamp(clampedLocalPos.x, localXMin, localXMax);
        // Optional Z clamp:
        // clampedLocalPos.z = Mathf.Clamp(clampedLocalPos.z, localZMin, localZMax);

        // Apply the clamped LOCAL position
        transform.localPosition = clampedLocalPos;

        // --- Alternative Clamping if AI is NOT a direct child: ---
        // Vector3 currentWorldPos = transform.position;
        // Vector3 currentLocalPosForClamp = environmentRoot.InverseTransformPoint(currentWorldPos);
        // currentLocalPosForClamp.x = Mathf.Clamp(currentLocalPosForClamp.x, localXMin, localXMax);
        // // Optional Z clamp here
        // transform.position = environmentRoot.TransformPoint(currentLocalPosForClamp);
    }


    // OnCollisionEnter should generally work okay as it deals with physics properties,
    // but ensure the 'mass' variable here matches Rigidbody.mass if that's intended.
    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody body = collision.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
            return;

        // Make sure we are using the Rigidbody's actual mass if it's set there
        float aiMass = _rigidbody.mass; // Use Rigidbody mass for consistency

        // The physics calculation itself is generally coordinate-system independent
        Vector3 pushDir = new Vector3(collision.relativeVelocity.x, 0, collision.relativeVelocity.z).normalized; // Get direction
        Vector3 pusherVelocity = _rigidbody.linearVelocity;
        Vector3 collidedVelocity = body.linearVelocity;
        float collidedMass = body.mass;
        float totalMass = collidedMass + aiMass; // Use aiMass

        // Check for zero total mass to avoid division by zero
        if (Mathf.Approximately(totalMass, 0f)) return;

        // Standard 1D elastic collision formulas applied component-wise (often simplified)
        // Your original calculation might be okay, but let's use a slightly more standard vector form
        // based on the normal of the collision for impulse calculation.
        // However, sticking to your original calculation for now:
        Vector3 newPusherVelocity =
            ((aiMass - collidedMass) / totalMass) * pusherVelocity
            + ((2 * collidedMass) / totalMass) * collidedVelocity;
        Vector3 newCollidedVelocity =
            ((2 * aiMass) / totalMass) * pusherVelocity
            + ((collidedMass - aiMass) / totalMass) * collidedVelocity;

        // Ensure Y velocity remains zero after collision
        newPusherVelocity.y = 0;
        newCollidedVelocity.y = 0;

        _rigidbody.linearVelocity = newPusherVelocity;
        // Only apply velocity to the *other* object if it's the puck, maybe?
        // Or let the PuckCollisionHandler manage the puck's reaction.
        // Applying it here might override specific puck physics. Consider commenting this out
        // if PuckCollisionHandler should be solely responsible for the puck's post-collision velocity.
        // body.linearVelocity = newCollidedVelocity; // <--- Potential conflict with PuckCollisionHandler
    }
}