using System;
using UnityEngine;

public class PusherController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    [SerializeField] private int mass = 100;
    [SerializeField] private float speed = 3;

    public float xMin, xMax; // Boundaries for the pusher
    public LayerMask tableLayer; // Layer for detecting mouse hits on the table

    private Camera mainCamera;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        mainCamera = Camera.main; // Cache the main camera
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleMouseMovement();

        // Clamp the position
        float clampedX = Mathf.Clamp(transform.position.x, xMin, xMax);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private void HandleKeyboardMovement()
    {
        float moveZ = Input.GetAxis("Vertical");
        float moveX = Input.GetAxis("Horizontal");

        Vector3 speedVector = new Vector3(moveX, 0.0f, moveZ);
        speedVector = speedVector.normalized * speed;
        _rigidbody.linearVelocity = speedVector;
    }

    private void HandleMouseMovement()
    {
        if (Input.GetMouseButton(0)) // Check if the left mouse button is held
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tableLayer))
            {
                Vector3 targetPosition = hit.point;
                targetPosition.y = transform.position.y; // Keep the pusher on the same vertical plane
                targetPosition.x = Mathf.Clamp(targetPosition.x, xMin, xMax); // Clamp within bounds

                // Move the pusher towards the target position
                Vector3 direction = (targetPosition - transform.position).normalized;
                _rigidbody.linearVelocity = direction * speed;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody body = collision.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
            return;

        Vector3 pushDir = new Vector3(collision.relativeVelocity.x, 0, collision.relativeVelocity.z);
        Vector3 pusherVelocity = _rigidbody.linearVelocity;
        Vector3 collidedVelocity = body.linearVelocity;
        float collidedMass = body.mass;
        float totalMass = collidedMass + mass;

        Vector3 newPusherVelocity =
            ((mass - collidedMass) / totalMass) * pusherVelocity
            + ((2 * collidedMass) / totalMass) * collidedVelocity;
        Vector3 newCollidedVelocity =
            ((2 * mass) / totalMass) * pusherVelocity
            + ((collidedMass - mass) / totalMass) * collidedVelocity;
        newPusherVelocity.y = 0;
        newCollidedVelocity.y = 0;

        _rigidbody.linearVelocity = newPusherVelocity;
        body.linearVelocity = newCollidedVelocity;
    }
}
