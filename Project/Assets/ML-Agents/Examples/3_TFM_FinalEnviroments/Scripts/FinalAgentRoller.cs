using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// import ML-Agents package 
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


// Delete Update() since we are not using it, but keep Start()
public class FinalAgentRoller : Agent
{
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    [SerializeField] private TrackCheckpointsBall trackCheckpoints; // get next checkpoint
    [SerializeField] private UnityEvent ResetCheckpointsEvent;// event to reset checkpoints order

    // reference to the Rigidbody component to reset the Agent's velocity and later to apply force to it 
    Rigidbody rBody;
    Rigidbody rBall;

    // public field of type Transform to the RollerAgent class for reference to move the target
    public GameObject Target;
    public Transform Bumps;
    [HideInInspector]
    public Vector3 BumpsStartingPos;

    [SerializeField]
    private GameObject Ball;

    [HideInInspector]
    public Vector3 AgentStartingPos;
    [HideInInspector]
    public Quaternion AgentStartingRot;

    [HideInInspector]
    public Vector3 BallStartingPos;

    public Material winMaterial;
    public Material loseMaterial;
    public GameObject ground;
    Material m_GroundMaterial;
    Renderer m_GroundRenderer;

    public float moveSpeed = 10; // public class variable (can set the value from the Inspector window)
    public float turnSpeed = 2000;
    public float jumpAmount = 1000;
    public float fallMultiplier = 2.5f;

    public float PositiveReward = 2f;
    public float FailedReward = -1f;
    public float CorrectCheckpointReward = 0.5f;
    public float WrongCheckpointReward = -0.5f;
    public float jumpReward = 0.1f;
    public float timeReward = -0.1f;
    public float wallCollisionReward = -0.1f;

    public bool UseRandomAgentPosition = false;
    public bool UseRandomAgentRotation = false;
    public bool UseRandomBallPosition = false;
    public bool UseRandomBumps = false;
    private float bumpsMaxHeight = 0.75f;
    public float RandomQuantity = 1;

    private bool isOnGround = false;
    //private bool isOnWall = false;

    private int checkpointNumber = 7;
    private int m_ResetTimer = 0;
    private int indice = 0;

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    private void endEpisode(float reward)
    {
        AddReward(reward);
        EndEpisode();
    }

    // test if we are in contact with a collider (ground) to reset the jump
    private void OnCollisionEnter(Collision collision)
    {
        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 14) && !isOnGround)
        {
            isOnGround = true;
        }
        if (collision.gameObject.CompareTag("wall") == true || collision.gameObject.layer == 10)
        {
            //isOnWall = true;
            AddReward(wallCollisionReward / MaxEnvironmentSteps);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 6 || collision.gameObject.layer == 10) && !isOnGround)
        {
            isOnGround = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // si dejas de tocar suelo o algun objeto, no puedes saltar
        if (collision.gameObject.layer == 11 || collision.gameObject.layer == 6 || collision.gameObject.layer == 10)
        {
            isOnGround = false;
        }
    }

    private float distanceToTarget()
    {
        float distance = Vector3.Distance(Ball.transform.localPosition, Target.transform.localPosition);
        // Debug.Log("Distance: " + distance);
        // Debug.Log("Ball: " + Ball.transform.position);
        // Debug.Log("Target: " + Target.transform.localPosition)
        return distance;
    }

    public void CorrectCheckpointEntered(Collider col)
    {
        Debug.Log($"Checkpoint correcto: {col}");
        AddReward(CorrectCheckpointReward / checkpointNumber);
        if (indice < checkpointNumber-1)
        {
            indice = indice + 1;
        }
        else
        {
            indice = 0;
        }
    }

    public void WrongCheckpointEntered(Collider col)
    {
        Debug.Log($"Checkpoint incorrecto: {col}");
        AddReward(WrongCheckpointReward / checkpointNumber);
    }

    void Start()
    {
        m_ResetTimer = 0;

        // Get the ground renderer
        m_GroundRenderer = ground.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        rBody = GetComponent<Rigidbody>();
        rBall = Ball.GetComponent<Rigidbody>();

        AgentStartingPos = this.transform.localPosition;
        AgentStartingRot = this.transform.rotation;
        BallStartingPos = Ball.transform.localPosition;
        BumpsStartingPos = Bumps.transform.localPosition;

    }

    void FixedUpdate()
    {
        // End if time > MaxEnvironmentSteps
        m_ResetTimer += 1;
        //print(m_ResetTimer);
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            endEpisode(FailedReward);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }

        float distance = distanceToTarget();
        if (distance <= 4f)
        {
            endEpisode(PositiveReward);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(winMaterial, 2));
        }
        // Fell off platform
        // if the Agent falls off the platform, end the episode so that it can reset itself
        if (this.transform.localPosition.y < -2f || Ball.transform.localPosition.y < -2f)// || isOnWall == true) // // Object.localPosition.y < -0.5 
        {
            endEpisode(FailedReward);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }
        //print($"isonground: {isOnGround}");
    }

    // set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        indice = 0;
        m_ResetTimer = 0;

        // Reset checkpoints order
        ResetCheckpointsEvent.Invoke();

        Vector3 randomPosAgent = Vector3.zero;
        Vector3 randomPosBall = Vector3.zero;
        Vector3 randomPosBumps = Vector3.zero;

        var randomDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("distance_offset", RandomQuantity);

        // Reset Agents
        randomPosAgent = new Vector3(Random.Range(-22.0f, -18.0f), 0.8f, Random.Range(-5.0f, +5.0f));
        var posAgent = UseRandomAgentPosition ? randomPosAgent : AgentStartingPos;
        var rotAgent = UseRandomAgentRotation ? GetRandomRot() : AgentStartingRot;
        this.transform.localPosition = posAgent;
        this.transform.rotation = rotAgent;
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;

        // Reset Ball
        randomPosBall = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-4f, +4f)) * randomDistance + BallStartingPos;
        var posTarget = UseRandomBallPosition ? randomPosBall : BallStartingPos;
        Ball.transform.localPosition = posTarget;
        rBall.angularVelocity = Vector3.zero;
        rBall.velocity = Vector3.zero;

        // Reset Bumps
        randomPosBumps = new Vector3(0f, Random.Range(0, bumpsMaxHeight), 0f) * randomDistance + BumpsStartingPos;
        var posBumps = UseRandomBumps ? randomPosBumps : BumpsStartingPos;
        Bumps.transform.localPosition = posBumps;
    }


    // Observing the Environment (what information to collect), 8 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Ball and Agent positions 
        sensor.AddObservation(Ball.transform.localPosition);    // Ball   (x,y,z)
        sensor.AddObservation(this.transform.localPosition);    // Agent  (x,y,z)

        // next checkpoint position
        Vector3 checkpointForward = trackCheckpoints.GetNextCheckpoint(indice).transform.localPosition;
        sensor.AddObservation(checkpointForward);               // NextCheck Pos (x,y,z)
        //print($"Next checkpoint position is: {trackCheckpoints.GetNextCheckpoint(indice).transform.localPosition}");

        // Agent and Ball velocity
        sensor.AddObservation(rBody.velocity.x);                // Agent Vel (x)
        sensor.AddObservation(rBody.velocity.z);                // Agent Vel (z)
        sensor.AddObservation(rBall.velocity.x);                // Ball Vel (x)
        sensor.AddObservation(rBall.velocity.z);                // Ball Vel (z)
    }

    // Taking Actions and Assigning Rewards
    // to move towards the target the agent needs 2 actions: determines the force applied along the x-axis and the z-axis
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        //float forwardAmount = 0f;
        //float sideAmount = 0f;


        var dirToGoForwardAction = actionBuffers.DiscreteActions[0];
        var rotateDirAction = actionBuffers.DiscreteActions[1];
        var jumpAction = actionBuffers.DiscreteActions[2];

        var speed = moveSpeed;
        // Discrete Actions
        switch (dirToGoForwardAction)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                //forwardAmount = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                //forwardAmount = -1f;
                speed = moveSpeed * 0.75f;
                break;
        }

        switch (rotateDirAction)
        {
            case 1:
                rotateDir = transform.up * 1f;
                break;
            case 2:
                rotateDir = transform.up * -1f;
                break;
        }

        // Jump Action - if the agent is on contact with the floor
        if ((jumpAction == 1) && isOnGround) //(this.transform.position.y < maxJumpHeight))
        {
            rBody.velocity += (Vector3.up * jumpAmount * Time.deltaTime);
            isOnGround = false;
            AddReward(-jumpReward / MaxEnvironmentSteps);
        }
        if (jumpAction != 1)
        {
            AddReward(jumpReward / MaxEnvironmentSteps);
        }
        if (rBody.velocity.y < 0)
        { // si estamos cayendo, caer nas rapido
            rBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // Movemos el agente
        if (isOnGround)
        {
            rBody.AddForce(dirToGo * speed, ForceMode.VelocityChange);
        }
        else
        {
            rBody.AddForce(dirToGo * speed / 4, ForceMode.VelocityChange);
        }
        //// Rotamos el agente
        this.transform.Rotate(rotateDir * turnSpeed, Time.deltaTime * 200f);

        //controlSignal.z = forwardAmount; // force applied along the x-axis, MoveX
        //controlSignal.y = 0;
        //controlSignal.x = sideAmount; // force applied along the z-axis, MoveZ
        //controlSignal.Normalize();


        //// Movemos el agente
        //this.transform.position += (controlSignal * moveSpeed * Time.deltaTime);

        //if (controlSignal != Vector3.zero)
        //{
        //    Quaternion toRotation = Quaternion.LookRotation(controlSignal, Vector3.up);
        //    this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, toRotation, turnSpeed * Time.deltaTime);
        //}

        // RollerAgent applies the values from the action[] array to its Rigidbody component rBody, using Rigidbody.AddForce()
        // rBody.AddForce(controlSignal * forceAmount * Time.deltaTime); //  * Time.deltaTime);

        // Rewards
        // calculates the distance to detect when it reaches the target
        // float distanceToTarget = Vector3.Distance(Object.localPosition, Target.localPosition);

        // Reward Intermedia - acercar/alejar el cubo a la meta
        //if (distanceToTarget < cubeMinDist)
        //{
        //    AddReward(0.001f);
        //}
        // if(distanceToTarget >= cubeMinDist) {
        //    AddReward(-0.00001f);
        //}


        // Penalty each Step
        AddReward(timeReward / MaxEnvironmentSteps);
    }


    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); //wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }

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
        if (Input.GetKey(KeyCode.R))
        {
            EndEpisode();
        }
        //    //Jump
       discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

}
