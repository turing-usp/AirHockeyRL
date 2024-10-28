using System;
using UnityEngine;

public class PusherController : MonoBehaviour
{
    private CharacterController _characterController;
    [SerializeField] private int mass = 10;
    [SerializeField] private float speed = 50;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        float moveX = Input.GetAxis("Vertical");
        float moveZ = Input.GetAxis("Horizontal");

        Vector3 speedVector = new Vector3(-1 * moveX * speed, 0,moveZ * speed);
        _characterController.SimpleMove(speedVector);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        
        if (body == null || body.isKinematic)
            return;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.y);
        Vector3 pusherVelocity = _characterController.velocity;
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

        // _characterController.SimpleMove(newPusherVelocity);
        body.AddForce(newCollidedVelocity - collidedVelocity, ForceMode.VelocityChange);
    }
}
