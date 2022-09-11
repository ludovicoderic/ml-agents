using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// import ML-Agents package 
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


// Delete Update() since we are not using it, but keep Start()
public class FinalAgentBridge : Agent
{
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    [SerializeField] private UnityEvent ResetCheckpointsEvent;// event to reset checkpoints order

    // reference to the Rigidbody component to reset the Agent's velocity and later to apply force to it 
    Rigidbody rBody;

    // public field of type Transform to the RollerAgent class for reference to move the target
    public Transform Target;
    [HideInInspector]
    public Vector3 TargetStartingPos;

    [SerializeField]
    private GameObject bridge; // Bridge object
    [SerializeField]
    private GameObject bridgeStart; //Bridge StartPosition
    [SerializeField]
    private GameObject bridgeEnd; //Bridge EndPosition
    [SerializeField]
    private GameObject button;
    public MeshRenderer buttonMeshRenderer;

    public GameObject ground;
    public GameObject ground2;
    Renderer m_GroundRenderer;
    Renderer m_GroundRenderer2;
    Material m_GroundMaterial; //cached on Awake()

    public Material winMaterial;
    public Material loseMaterial;

    // Agent Variables
    [HideInInspector]
    public Vector3 AgentStartingPos;
    [HideInInspector]
    public Quaternion AgentStartingRot;

    public float moveSpeed = 1f; // public class variable (can set the value from the Inspector window)
    public float turnSpeed = 1f;
    //public float jumpAmount = 1000;
    private float fallMultiplier = 2.5f;

    // Enviroment Variables
    [HideInInspector]
    public Vector3 BridgetStartingPos;
    [HideInInspector]
    public Quaternion BridgeStartingRot;
    [HideInInspector]
    public Vector3 ButtonStartingPos;

    public float PositiveReward = 0.5f;
    public float ButtonFindReward = 0.1f;
    public float CorrectCheckpointReward = 0.25f;
    public float WrongCheckpointReward = 0.25f;
    public float FailedReward = -1f;



    private bool isOnGround = false;
    private bool isOnWall = false;
    private bool shouldOpen = false;
    //private bool bridgeCrossed = false;

    public bool UseRandomAgentPosition = false;
    public bool UseRandomAgentRotation = false;
    public bool UseRandomButtonPosition = false;
    public bool UseRandomBridgePosition = false;
    public bool UseRandomGoalPosition = false;
    public float RandomQuantity = 1;

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

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
        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 14 || collision.gameObject.layer == 16 || collision.gameObject.layer == 17) && !isOnGround)
        {
            isOnGround = true;
        }
        if (collision.gameObject.CompareTag("goal") == true || collision.gameObject.layer == 12)
        {
            endEpisode(PositiveReward);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(winMaterial, 2));
        }
        if (collision.gameObject.CompareTag("wall") == true || collision.gameObject.layer == 10)
        {
            isOnWall = true;
        }
        //if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        //{
        //    shouldOpen = true;
        //    buttonMeshRenderer.material = winMaterial;
        //    AddReward(ButtonFindReward);
        //}
        //if (collision.gameObject.CompareTag("switchOff") == true || collision.gameObject.layer == 17) {
        //    shouldOpen = false;
        //}
    }



    private void OnCollisionExit(Collision collision)
    {
        // si dejas de tocar suelo o algun objeto, no puedes saltar
        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 14 || collision.gameObject.layer == 16 || collision.gameObject.layer == 17))
        {
            isOnGround = false;
        }
        //if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        //{
        //    shouldOpen = true;
        //    buttonMeshRenderer.material = loseMaterial;
        //}
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("switchOn") == true || col.gameObject.layer == 16)
        {
            if (!shouldOpen)
            {
                shouldOpen = true;
                print($"Scored on button {gameObject.name}");
                AddReward(ButtonFindReward);
                buttonMeshRenderer.material = winMaterial;
            }
            //else{
            //    AddReward(-0.001);
            //}
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("switchOn") == true)
        {
            //shouldOpen = true;
            //buttonMeshRenderer.material = loseMaterial;
        }
    }

    public void CorrectCheckpointEntered(Collider col)
    {
        Debug.Log($"Checkpoint correcto: {col}");
        AddReward(CorrectCheckpointReward);
    }

    public void WrongCheckpointEntered(Collider col)
    {
        Debug.Log($"Checkpoint incorrecto: {col}");
        AddReward(WrongCheckpointReward);
    }

    void Start()
    {
        // Reset Variables
        //bridgeCrossed = false;

        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundRenderer2 = ground2.GetComponent<Renderer>();

        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        //bridgeMoved = false;
        rBody = GetComponent<Rigidbody>();
        AgentStartingPos = this.transform.localPosition;
        AgentStartingRot = this.transform.rotation;

        BridgetStartingPos = bridgeStart.transform.localPosition;

        ButtonStartingPos = button.transform.localPosition;
        TargetStartingPos = Target.transform.localPosition;
    }

    void FixedUpdate()
    {
        if (shouldOpen) // Button pressed
        {
            StartCoroutine(moveAtoB(bridge, bridgeEnd, 1f));
        }
        if (!shouldOpen) // Button NOT pressed
        {
            StartCoroutine(moveAtoB(bridge, bridgeStart, 1f));
        }
        if (isOnWall == true) // End Episode if Agents Fall
        {
            endEpisode(FailedReward);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }
    }

    // set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        // Reset checkpoints order
        ResetCheckpointsEvent.Invoke();

        // Reset Variables
        shouldOpen = false;
        isOnGround = false;
        isOnWall = false;
        //bridgeCrossed = false;

        Vector3 randomPosAgent = Vector3.zero;
        Vector3 randomPosButton = Vector3.zero;
        Vector3 randomPosTarget = Vector3.zero;
        var randomDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("distance_offset", RandomQuantity);

        // Reset bridge
        float moveBridgeZ = Random.Range(-5f, +5f) * randomDistance;
        var randomPosBridge = UseRandomBridgePosition ? moveBridgeZ : 0;
        bridge.transform.localPosition = new Vector3(0, 0, randomPosBridge);
        bridgeStart.transform.localPosition = new Vector3(0, 0, randomPosBridge);
        bridgeEnd.transform.localPosition = new Vector3(0, 20, randomPosBridge);

        // Reset Agents
        randomPosAgent = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-15.0f, +15.0f))* randomDistance + AgentStartingPos;
        //Vector3 randomPos = new Vector3(Random.Range(-22.0f, -16.0f), 4.47f, Random.Range(-15.0f, +15.0f)) * RandomQuantity;
        var posAgent = UseRandomAgentPosition ? randomPosAgent : AgentStartingPos;
        var rotAgent = UseRandomAgentRotation ? GetRandomRot() : AgentStartingRot;
        this.transform.localPosition = posAgent;
        this.transform.rotation = rotAgent;
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;

        // Reset Button
        randomPosButton = new Vector3(Random.Range(-4.0f, +3.0f), 0f, Random.Range(-10.0f, +10.0f))* randomDistance + ButtonStartingPos;
        var posButton = UseRandomButtonPosition ? randomPosButton : ButtonStartingPos;
        button.transform.localPosition = posButton;
        buttonMeshRenderer.material = loseMaterial;

        // Reset Goal
        randomPosTarget = new Vector3(Random.Range(-5f, 0f), 0f, Random.Range(-12f, +12f))* randomDistance + TargetStartingPos;
        var posTarget = UseRandomGoalPosition ? randomPosTarget : TargetStartingPos;
        Target.transform.localPosition = posTarget;

    }


    // Observing the Environment (what information to collect), 13 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(Target.localPosition.x);                  // Target (x,z)
        sensor.AddObservation(Target.localPosition.z);               
        sensor.AddObservation(shouldOpen);                              // Bridge (bool)
        sensor.AddObservation(bridge.transform.localPosition.z);        // Bridge (z)
        sensor.AddObservation(button.transform.localPosition.x);        // button (x,z)
        sensor.AddObservation(button.transform.localPosition.z);      

        sensor.AddObservation(this.transform.localPosition);            // Agent  (x,y,z)
        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);                        // Agent Vel (x)
        sensor.AddObservation(rBody.velocity.z);                        // Agent Vel (z)
    }

    // Taking Actions and Assigning Rewards
    // to move towards the target the agent needs 2 actions: determines the force applied along the x-axis and the z-axis
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var dirToGoForwardAction = actionBuffers.DiscreteActions[0];
        var rotateDirAction = actionBuffers.DiscreteActions[1];

        var speed = moveSpeed;
        switch (dirToGoForwardAction)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                //AddReward(1f / 25000);
                //print("forward");
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                speed = moveSpeed * 0.75f;
                //AddReward(-10f / 25000);
                //print("backwards");

                break;
        }
        switch (rotateDirAction) { 
            case 1:
                rotateDir = transform.up * 1f;
                break;
            case 2:
                rotateDir = transform.up * -1f;
                break;
        }
        this.transform.Rotate(rotateDir * turnSpeed, Time.deltaTime * 200f);
        rBody.AddForce(dirToGo * speed, ForceMode.VelocityChange);

        if (rBody.velocity.y < 0)
        { // si estamos cayendo, caer nas rapido
            rBody.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // Jump Action - if the agent is on contact with the floor
        //if ((jumpAction == 1) && isOnGround) //(this.transform.position.y < maxJumpHeight))
        //{
        //    rBody.velocity += (Vector3.up * jumpAmount * Time.deltaTime);
        //    isOnGround = false;
        //    //AddReward(-0.001f);
        //}


        //controlSignal.z = forwardAmount; // force applied along the x-axis, MoveX
        //controlSignal.y = 0;
        //controlSignal.x = sideAmount; // force applied along the z-axis, MoveZ
        //controlSignal.Normalize();

        // Movemos el agente
        //this.transform.position += (controlSignal * moveSpeed * Time.deltaTime);
        //rBody.AddForce(dirToGo * moveSpeed,ForceMode.VelocityChange);

        // Rotamos el agente
        //this.transform.Rotate(new Vector3(0, turnAmount * turnSpeed, 0));
        //this.transform.Rotate(rotateDir, Time.fixedDeltaTime * 200f);

        //if (controlSignal != Vector3.zero)
        //{
        //    Quaternion toRotation = Quaternion.LookRotation(controlSignal, Vector3.up);
        //    this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, toRotation, turnSpeed * Time.deltaTime);
        //}

        // RollerAgent applies the values from the action[] array to its Rigidbody component rBody, using Rigidbody.AddForce()
        //rBody.AddForce(controlSignal * forceAmount * Time.deltaTime); //  * Time.deltaTime);

        // Rewards
        // float distanceToTarget = Vector3.Distance(Object.localPosition, Target.localPosition)

        // Fell off platform
        // if the Agent falls off the platform, end the episode so that it can reset itself

        // Penalty each Step
        AddReward(-1f / 25000);
    }


    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        m_GroundRenderer2.material = mat;
        yield return new WaitForSeconds(time); //wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
        m_GroundRenderer2.material = m_GroundMaterial;
    }

    IEnumerator moveAtoB(GameObject gameObjectA, GameObject gameObjectB, float speed)
    {
        while(gameObjectA.transform.position != gameObjectB.transform.position)
        {
            gameObjectA.transform.position = Vector3.MoveTowards(gameObjectA.transform.position, gameObjectB.transform.position, speed * Time.deltaTime);
            yield return null;
        }
    }


    // Continous actions, too much complex
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
        ////Side rotation
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 1; // right
        }
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 2; // left
        }
        ////Rotate
        //if (Input.GetKey(KeyCode.E))
        //{
        //    discreteActionsOut[2] = 1; // turn right
        //}
        //if (Input.GetKey(KeyCode.Q))
        //{
        //    discreteActionsOut[2] = 2; // turn left
        //}
        // Test Reset
        if (Input.GetKey(KeyCode.R))
        {
            EndEpisode();
        }
        //Jump
        //discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

}
