using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// import ML-Agents package 
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


// Delete Update() since we are not using it, but keep Start()
public class FinalAgentCooperative_Test : Agent
{
    // reference to the Rigidbody component to reset the Agent's velocity and later to apply force to it 
    Rigidbody rBody;

    // public field of type Transform to the RollerAgent class for reference to move the target
    public Transform Target;
    [SerializeField]
    private GameObject bridge; // Bridge object
    [SerializeField]
    private GameObject bridgeStart; //Bridge StartPosition
    [SerializeField]
    private GameObject bridgeEnd; //Bridge EndPosition

    [HideInInspector]
    public Vector3 AgentStartingPos;
    [HideInInspector]
    public Quaternion AgentStartingRot;

    [HideInInspector]
    public Vector3 ObjectStartingPos;
    [HideInInspector]
    public Quaternion ObjectStartingRot;

    public Material winMaterial;
    public Material loseMaterial;
    public Material normalMaterial;
    public MeshRenderer floorMeshRenderer;
    public MeshRenderer buttonMeshRenderer;

    public float GoalRange = 1.2f;

    public float moveSpeed = 10; // public class variable (can set the value from the Inspector window)
    public float turnSpeed = 2000;
    public float forceAmount = 1;

    public float jumpAmount = 1000;

    public float fallMultiplier = 2.5f;

    private bool isOnGround = false;
    private bool isOnWall = false;
    private bool shouldOpen = false;


    private void endEpisode(float reward)
    {
        StopCoroutine("moveAtoB");
        AddReward(reward);
        EndEpisode();
    }


    bool IsInside(Vector3 point)
    {
        var collider = GetComponent<Collider>();

        Vector3 closest = collider.ClosestPoint(point);
        // Because closest=point if inside - not clear from docs I feel
        return closest == point;
    }

    // test if we are in contact with a collider (ground) to reset the jump
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with " + collision.transform.name);

        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 14 || collision.gameObject.layer == 16) && !isOnGround)
        {
            isOnGround = true;
        }
        if (collision.gameObject.CompareTag("goal") == true || collision.gameObject.layer == 12)
        {
            endEpisode(1.0f);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(winMaterial, 2));
        }
        if (collision.gameObject.CompareTag("wall") == true || collision.gameObject.layer == 10)
        {
            isOnWall = true;
        }
        if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        {
            shouldOpen = true;
            buttonMeshRenderer.material = winMaterial;
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        {
            shouldOpen = true;
            buttonMeshRenderer.material = winMaterial;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log("Stop Collision with " + collision.transform.name);
        // si dejas de tocar suelo o algun objeto, no puedes saltar
        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 14 || collision.gameObject.layer == 16 || collision.gameObject.layer == 17))
        {
            isOnGround = false;
        }
        if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        {
            shouldOpen = false;
            buttonMeshRenderer.material = loseMaterial;
        }
    }


    void FixedUpdate()
    {
        if (shouldOpen)
        {
            StartCoroutine(moveAtoB(bridge, bridgeEnd, 5f));
        }
        else if (!shouldOpen)
        {
            StartCoroutine(moveAtoB(bridge, bridgeStart, 1f));
        }
    }



    void Start()
    {
        buttonMeshRenderer.material = loseMaterial; // color del boton

        //bridgeMoved = false;
        rBody = GetComponent<Rigidbody>();
        AgentStartingPos = this.transform.localPosition;
        AgentStartingRot = this.transform.rotation;

        ObjectStartingPos = bridgeStart.transform.localPosition;
        ObjectStartingRot = bridgeStart.transform.rotation;
    }

    // set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        shouldOpen = false;
        // If the Agent fell of the platform, zero its momentum
        if (isOnWall || this.transform.localPosition.y < 0)
        {
            // reset the Agent's velocity and put back onto the floor
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 5f, 0);
            this.transform.localRotation = Quaternion.Euler(0, 0, 0);
            isOnWall = false;
        }
        this.transform.localPosition = AgentStartingPos;
        this.transform.rotation = AgentStartingRot;
        float moveBridgeZ = Random.Range(-10.0f, +10.0f);
        bridge.transform.localPosition = new Vector3(0, 0, moveBridgeZ);
        bridgeStart.transform.localPosition = new Vector3(0, 0, moveBridgeZ);
        bridgeEnd.transform.localPosition = new Vector3(0, 20, moveBridgeZ);

        // Move the target to a new spot (moved to a new random location)
        //this.transform.localPosition = new Vector3(Random.Range(-20.0f, +20.0f), 5f, Random.Range(-20.0f, +20.0f));
        Target.transform.localPosition = new Vector3(Random.Range(18f, +22f), 6.32f, Random.Range(-10f, +10f));

    }


    // Observing the Environment (what information to collect), 8 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(Target.localPosition);            // Target (x,y,z)
        sensor.AddObservation(bridge.transform.localPosition);            // Object (x,y,z)
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
        var jumpAction = actionBuffers.DiscreteActions[2];

        // Discrete Actions
        switch (dirToGoForwardAction)
        {
            case 0: forwardAmount = 0f; break;
            case 1: forwardAmount = +1f; break;
            case 2: forwardAmount = -1f; break;
        }

        switch (dirToGoSideAction)
        {
            case 0: sideAmount = 0f; break;
            case 1: sideAmount = +1f; break;
            case 2: sideAmount = -1f; break;
        }

        // Jump Action - if the agent is on contact with the floor
        if ((jumpAction == 1) && isOnGround) //(this.transform.position.y < maxJumpHeight))
        {
            rBody.velocity += (Vector3.up * jumpAmount * Time.deltaTime);
            isOnGround = false;
            //AddReward(-0.001f);
        }
        if (rBody.velocity.y < 0)
        { // si estamos cayendo, caer nas rapido
            rBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }

        controlSignal.z = forwardAmount; // force applied along the x-axis, MoveX
        controlSignal.y = 0;
        controlSignal.x = sideAmount; // force applied along the z-axis, MoveZ
        controlSignal.Normalize();


        // Movemos el agente
        this.transform.position += (controlSignal * moveSpeed * Time.deltaTime);

        if (controlSignal != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(controlSignal, Vector3.up);
            this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, toRotation, turnSpeed * Time.deltaTime);
        }

        // RollerAgent applies the values from the action[] array to its Rigidbody component rBody, using Rigidbody.AddForce()
        rBody.AddForce(controlSignal * forceAmount * Time.deltaTime); //  * Time.deltaTime);

        // Fell off platform
        // if the Agent falls off the platform, end the episode so that it can reset itself
        if (this.transform.localPosition.y < -2.8f || isOnWall == true) // // Object.localPosition.y < -0.5 
        {
            endEpisode(-0.5f);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }
        // Penalty each Step
        //AddReward(-1f / 5000001);
    }


    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        floorMeshRenderer.material = mat;
        yield return new WaitForSeconds(time); //wait for 2 sec
        floorMeshRenderer.material = normalMaterial;
    }

    IEnumerator ButtonSwapGroundMaterial(Material mat, float time)
    {
        buttonMeshRenderer.material = mat;
        yield return new WaitForSeconds(time); //wait for 2 sec
        buttonMeshRenderer.material = loseMaterial;
    }

    IEnumerator moveAtoB(GameObject gameObjectA, GameObject gameObjectB, float speed)
    {
        if (gameObjectA.transform.position != gameObjectB.transform.position)
        {
            while (gameObjectA.transform.position != gameObjectB.transform.position)
            {
                gameObjectA.transform.position = Vector3.MoveTowards(gameObjectA.transform.position, gameObjectB.transform.position, speed * Time.deltaTime);
                yield return null;
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        // Forward
        if (Input.GetKey(KeyCode.I))
        {
            discreteActionsOut[0] = 1; // forward
        }
        if (Input.GetKey(KeyCode.K))
        {
            discreteActionsOut[0] = 2; // backwards
        }
        //Side 
        if (Input.GetKey(KeyCode.L))
        {
            discreteActionsOut[1] = 1; // right
        }
        if (Input.GetKey(KeyCode.J))
        {
            discreteActionsOut[1] = 2; // left
        }
        //    //Jump
        discreteActionsOut[2] = Input.GetKey(KeyCode.M) ? 1 : 0;
    }

}
