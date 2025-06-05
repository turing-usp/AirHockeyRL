using Unity.MLAgents;

using Unity.MLAgents.Actuators;

using Unity.MLAgents.Sensors;

using UnityEngine;



[RequireComponent(typeof(Rigidbody))]

public class PusherAgent : Agent

{

    [Header("Movement")]

    [SerializeField] float maxSpeed = 2.5f; // Corrected to match actual max speed

 [SerializeField] float accelRate = 25f;

    [SerializeField] float decelRate = 18f;



    [Header("Borders (local X)")]

    [SerializeField] float xMin = -3f; // Will be overridden in Initialize

 [SerializeField] float xMax = 3f; // Will be overridden in Initialize

 [SerializeField] float halfTableWidth = 2f; // Table half-width (x from -2 to 2)



 /* refs filled by ArenaManager */

 [HideInInspector] public Transform puck;

    [HideInInspector] public Rigidbody puckRb;

    [HideInInspector] public PusherAgent opponent;



    Rigidbody rb;

    ArenaManager arena;

    int mirror = 1; // +1 for Blue, 1 for Orange



 /* called once by ArenaManager */

 public void InitContext(ArenaManager ctx, PusherAgent opp)

    {

        arena = ctx;

        opponent = opp;

        puck = ctx.puck;

        puckRb = ctx.puck.GetComponent<Rigidbody>();

    }



 /* --------------- ML-Agents life-cycle --------------- */

 public override void Initialize()

    {

        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezePositionY |

                RigidbodyConstraints.FreezeRotation;



        mirror = CompareTag("Orange") ? -1 : 1;



  // Set boundaries based on agent side

  if (CompareTag("Blue"))

        {

            xMin = -2f; // Blue: x from -2 to 0 (negative side)

   xMax = 0f;

        }

        else // Orange

  {

            xMin = 0f; // Orange: x from 0 to 2 (positive side)

   xMax = 2f;

        }

    }



    public override void OnEpisodeBegin()
{
        //

    // Its own Rigidbody state is reset by ArenaManager.ResetArena()
    if (rb != null) { // rb should be initialized in Initialize()
         rb.linearVelocity = Vector3.zero;
         rb.angularVelocity = Vector3.zero;
    }
}



 /* --------------- Observations --------------- */

 public override void CollectObservations(VectorSensor sensor)

    {

        sensor.AddObservation(NormPos(transform.localPosition));

        sensor.AddObservation(NormVel(rb.linearVelocity));

        sensor.AddObservation(mirror == 1 ? 0 : 1);

        if (opponent)

        {

            sensor.AddObservation(NormPos(opponent.transform.localPosition));

            sensor.AddObservation(NormVel(opponent.GetComponent<Rigidbody>().linearVelocity));

        }

        else sensor.AddObservation(new float[4]);



        sensor.AddObservation(NormPos(puck.localPosition));

        sensor.AddObservation(NormVel(puckRb.linearVelocity));

    }



    Vector2 NormPos(Vector3 w)

    {

  // Normalize x to -1 to 1 for Blue, 1 to -1 for Orange

  float normalizedX = mirror * ((w.x / halfTableWidth) * 2 + (mirror == 1 ? 1 : -1));

        float normalizedZ = mirror * w.z / halfTableWidth; // Z normalized by halfWidth

  return new Vector2(normalizedX, normalizedZ);

    }



    Vector2 NormVel(Vector3 v) => new(mirror * v.x / maxSpeed, mirror * v.z / maxSpeed); // Normalize by maxSpeed (2.5)



 /* --------------- Actions --------------- */

 public override void OnActionReceived(ActionBuffers act)

    {

        MoveByAction(act.DiscreteActions[0]);



  // Small existence penalty to encourage efficiency

  AddReward(-0.001f);



  // Reward based on puck position (using local position)

  float puckX = puck.localPosition.x;

        bool puckOnAgentSide = (mirror == 1 && puckX < 0) || (mirror == -1 && puckX > 0);

        if (puckOnAgentSide)

        {

            AddReward(-0.0015f); // Negative reward for puck on agent's side

  }

        else

        {

            AddReward(0.002f); // Positive reward for puck on opponent's side

  }
        if (StepCount >= MaxStep - 1)
    {
        // 1) Punish both agents (optional, your choice of value)
        AddReward(-100f);
        if (opponent != null)
            opponent.AddReward(-100f);

        // 2) End both episodes immediately
        EndEpisode();
        if (opponent != null)
            opponent.EndEpisode();

            // 3) Tell the ArenaManager to reset the puck + both agents once.
            //    (Assumes you've added a public RequestResetFromAgent() in ArenaManager.)
            arena.ResetArena();
    }
    }



    void MoveByAction(int a)

    {

        Vector3 dir = a switch

        {

            1 => Vector3.forward,  // +Z

   2 => Vector3.back,  // Z

   3 => Vector3.left,  // X

   4 => Vector3.right,  // +X

   _ => Vector3.zero

        };



        dir.x *= mirror;

        dir.z *= mirror;



        Vector3 target = dir * maxSpeed;

        float rate = dir == Vector3.zero ? decelRate : accelRate;



        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity,

                           target,

                           rate * Time.fixedDeltaTime);



  /* ---------- LOCAL-SPACE CLAMP ---------- */

  Vector3 p = transform.localPosition;

        if (p.x < xMin)

        {

            p.x = xMin;

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);

            rb.MovePosition(transform.parent.TransformPoint(p)); // Use Rigidbody for physics

  }

        else if (p.x > xMax)

        {

            p.x = xMax;

            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, rb.linearVelocity.z);

            rb.MovePosition(transform.parent.TransformPoint(p)); // Use Rigidbody for physics

  }

    }



 /* --------------- Touch reward --------------- */

 void OnCollisionEnter(Collision c)

    {
        if (c.collider.CompareTag("Puck"))
        {
            AddReward(+5f);          // small bonus for the hitter
    
            // NEW: give the opponent a symmetric negative reward
            if (opponent != null)
                opponent.AddReward(-3f);
        }
 }

}