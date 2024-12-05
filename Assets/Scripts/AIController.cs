using System;
using UnityEngine;

public class AIController : MonoBehaviour
{
    private Rigidbody _rigidbody;
    [SerializeField] private int mass = 100;
    [SerializeField] private float speed = 3;

    public Transform puck;

    public float xMin, xMax; // Boundaries for the pusher

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPosition = new Vector3(puck.position.x, puck.position.y, puck.position.z);
        if (targetPosition.x < xMin || targetPosition.x > xMax)
        {
            targetPosition.x = (xMin - xMax) / 2;
        }

        float moveX = targetPosition.x - transform.position.x;
        float moveZ = targetPosition.z - transform.position.z;

        Vector3 speedVector = new Vector3(moveX, 0.0f, moveZ);
        speedVector = speedVector.normalized * speed;
        _rigidbody.linearVelocity = speedVector;

        // Clamp the position
        float clampedX = Mathf.Clamp(transform.position.x, xMin, xMax);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
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
