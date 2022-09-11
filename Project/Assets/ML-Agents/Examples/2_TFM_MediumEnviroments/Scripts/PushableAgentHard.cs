using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// import ML-Agents package 
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


// Delete Update() since we are not using it, but keep Start()
public class PushableAgentHard : Agent {
    // reference to the Rigidbody component to reset the Agent's velocity and later to apply force to it 
    Rigidbody rBody;

    // public field of type Transform to the RollerAgent class for reference to move the target
    public Transform Target;
    public Transform Object;


    public Material winMaterial;
    public Material loseMaterial;
    public Material normalMaterial;
    public MeshRenderer floorMeshRenderer;

    public float GoalRange = 1.2f;

    public float moveSpeed = 10; // public class variable (can set the value from the Inspector window)
    public float turnSpeed = 2000;
    public float forceAmount = 1;

    Vector3 m_JumpStartingPos;
    public float jumpingTime;
    public float jumpAmount = 1000;
    public float maxJumpHeight = 3;

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.5f;
    private float forceMagnitude = 5f;

    private bool isOnGround = false;
    private bool isOnGoal = false;
    private float cubeMinDist = float.PositiveInfinity;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    //// Begin the jump sequence
    //public void Jump()
    //{
    //    jumpingTime = 0.2f;
    //    m_JumpStartingPos = this.transform.localPosition;
    //}

    // test if we are in contact with a collider (ground) to reset the jump
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 11 && !isOnGround)
        {
            isOnGround = true;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rigidbody = hit.collider.attachedRigidbody;
        if (rigidbody != null)
        {
            Vector3 forceDirection = hit.gameObject.transform.position - transform.position;
            forceDirection.Normalize();
    
            rigidbody.AddForceAtPosition(forceMagnitude * forceDirection, transform.position, ForceMode.Impulse);
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == 12 && !isOnGoal)
        {
            isOnGoal = true;
        }
    }

    // set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        isOnGoal = false;

        // If the Agent fell of the platform, zero its momentum
        if (this.transform.localPosition.y < 0)
        {
            // reset the Agent's velocity and put back onto the floor
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.8f, 0);
            this.transform.localRotation = Quaternion.Euler(0, 0, 0);
        }

        // Move the target to a new spot (moved to a new random location)
        this.transform.localPosition = new Vector3(Random.Range(-20.0f, +20.0f), 0.8f, Random.Range(-20.0f, +20.0f));
        Object.localPosition = new Vector3(Random.Range(-10f, +10.0f), 4.0f, Random.Range(-10.0f, +10.0f));
        Object.localRotation = Quaternion.Euler(0, Random.Range(0f, +45.0f), 0);
        Target.localPosition = new Vector3(Random.Range(-14f, +14f), 4.0f, Random.Range(-14f, +14f));

    }


    // Observing the Environment (what information to collect), 8 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(Target.localPosition);            // Target (x,y,z)
        sensor.AddObservation(Object.localPosition);            // Object (x,y,z)
        sensor.AddObservation(this.transform.localPosition);    // Agent  (x,y,z)
        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);                // Agent Vel (x)
        sensor.AddObservation(rBody.velocity.z);                // Agent Vel (z)
    }

    // Taking Actions and Assigning Rewards
    // to move towards the target the agent needs 2 actions: determines the force applied along the x-axis and the z-axis
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        float forwardAmount = 0f;
        //float turnAmount = 0f;
        float sideAmount = 0f;


        var dirToGoForwardAction = actionBuffers.DiscreteActions[0];
        var dirToGoSideAction = actionBuffers.DiscreteActions[1];
        //var rotateDirAction = actionBuffers.DiscreteActions[2];
        //var jumpAction = actionBuffers.DiscreteActions[3];

        // Discrete Actions
        switch (dirToGoForwardAction) {
            case 0: forwardAmount = 0f; break;
            case 1: forwardAmount = +1f; break;
            case 2: forwardAmount = -1f; break;
        }

        switch (dirToGoSideAction) {
            case 0: sideAmount = 0f; break;
            case 1: sideAmount = +1f; break;
            case 2: sideAmount = -1f; break;
        }

        //switch (rotateDirAction) {
        //    case 0: turnAmount = 0f; break;
        //    case 1: turnAmount = +1f; break;
        //    case 2: turnAmount = -1f; break;

        //}

        // Jump Action - if the agent is on contact with the floor
        //if ((jumpAction == 1) && isOnGround) //(this.transform.position.y < maxJumpHeight))
        //{
        //    rBody.velocity += (Vector3.up * jumpAmount * Time.deltaTime);
        //    isOnGround = false;
        //}
        //if (rBody.velocity.y < 0) { // si estamos cayendo, caer nas rapido
        //    rBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        //}

        controlSignal.z = forwardAmount; // force applied along the x-axis, MoveX
        controlSignal.y = 0;
        controlSignal.x = sideAmount; // force applied along the z-axis, MoveZ
        controlSignal.Normalize();

       
        // Movemos el agente
        this.transform.position += (controlSignal * moveSpeed * Time.deltaTime);

        // Rotamos el agente
        //this.transform.Rotate(new Vector3(0, turnAmount * turnSpeed, 0));
        //this.transform.localRotation = Quaternion.Euler(0, turnAmount * turnSpeed, 0);
        if (controlSignal != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(controlSignal, Vector3.up);
            this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, toRotation, turnSpeed * Time.deltaTime);
        }



        // RollerAgent applies the values from the action[] array to its Rigidbody component rBody, using Rigidbody.AddForce()
        rBody.AddForce(controlSignal * forceAmount * Time.deltaTime); //  * Time.deltaTime);

        // Rewards
        // calculates the distance to detect when it reaches the target
        float distanceToTarget = Vector3.Distance(Object.localPosition, Target.localPosition);

        // Reward Intermedia - acercar/alejar el cubo a la meta
        if (distanceToTarget < cubeMinDist)
        {
            AddReward(0.001f);
        }
        // if(distanceToTarget >= cubeMinDist) {
        //    AddReward(-0.00001f);
        //}

        // Reached target
        // the Agent is given a reward of 1.0 for reaching the Target cube
        if (isOnGoal && distanceToTarget< GoalRange)
        {
            AddReward(5f);
            EndEpisode();
            StartCoroutine(
                GoalScoredSwapGroundMaterial(winMaterial, 2));
            
        }

        // Fell off platform
        // if the Agent falls off the platform, end the episode so that it can reset itself
        else if (this.transform.localPosition.y < 0 || Object.localPosition.y < 0)
        {
            SetReward(-0.5f);
            EndEpisode();
            StartCoroutine(
                GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }
        //AddReward(-1f / 5000001);

    }

    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        floorMeshRenderer.material = mat;
        yield return new WaitForSeconds(time); //wait for 2 sec
        floorMeshRenderer.material = normalMaterial;
    }

    // private void OnTriggerEnter(Collider other){
    // if (other.TryGetComponent<Goal>(out Goal goal)){ 
    // SetReward(1f);
    // EndEpisode();
    // }

    // if (other.TryGetComponent<Wall>(out Wall wall))
    //      {
    // SetReward(1f);
    // EndEpisode();
    // }
    // }

    // to first test your environment by controlling the Agent using the keyboard, extending the Heuristic() method
    // the heuristic will generate an action corresponding to the values of the "Horizontal" and "Vertical"
    // input axis (which correspond to the keyboard arrow keys)
    //public override void Heuristic(in ActionBuffers actionsOut)
    //{
    //    var continuousActionsOut = actionsOut.ContinuousActions;
    //    continuousActionsOut[0] = Input.GetAxis("Horizontal");
    //    continuousActionsOut[1] = Input.GetAxis("Vertical");
    //    continuousActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    //}

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        // Forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1; // forward
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2; // backwards
        }
        //Side 
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 1; // right
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 2; // left
        }
    //    //Rotate
    //    if (Input.GetKey(KeyCode.E))
    //    {
    //        discreteActionsOut[2] = 1; // turn right
    //    }
    //    if (Input.GetKey(KeyCode.Q))
    //    {
    //        discreteActionsOut[2] = 2; // turn left
    //    }
    //    //Jump
    //    discreteActionsOut[3] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

}
