using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// import ML-Agents package 
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;


// Delete Update() since we are not using it, but keep Start()
public class FinalAgentBridge_alter : Agent
{
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

    public float moveSpeed = 10; // public class variable (can set the value from the Inspector window)
    public float turnSpeed = 2000;
    //public float jumpAmount = 1000;
    public float fallMultiplier = 2.5f;
    // Enviroment Variables
    [HideInInspector]
    public Vector3 BridgetStartingPos;
    [HideInInspector]
    public Quaternion BridgeStartingRot;
    [HideInInspector]
    public Vector3 ButtonStartingPos;

    public float ButtonFindReward = 0.5f;
    public float PositiveReward = 2f;
    public float FailedReward = -1f;

    private bool isOnGround = false;
    private bool isOnWall = false;
    private bool shouldOpen = false;

    public bool UseRandomAgentPosition = false;
    public bool UseRandomAgentRotation = false;
    public bool UseRandomButtonPosition = false;
    public bool UseRandomBridgePosition = false;
    public bool UseRandomGoalPosition = false;

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
        print($"Scored on button {gameObject.name}");
        if (col.gameObject.CompareTag("switchOn") == true || col.gameObject.layer == 16)
        {
            shouldOpen = true;
            buttonMeshRenderer.material = winMaterial;
            AddReward(ButtonFindReward);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("switchOn") == true)
        {
            shouldOpen = true;
            buttonMeshRenderer.material = loseMaterial;
        }
    }

    void Start()
    {
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
        if (shouldOpen)
        {
            StartCoroutine(moveAtoB(bridge, bridgeEnd, 0.5f));
        }
        if (!shouldOpen)
        {
            StartCoroutine(moveAtoB(bridge, bridgeStart, 0.5f));
        }
    }

    // set-up the environment for a new episode
    public override void OnEpisodeBegin()
    {
        // Reset Variables
        shouldOpen = false;
        isOnGround = false;
        isOnWall = false;

        Vector3 randomPosAgent = Vector3.zero;
        Vector3 randomPosButton = Vector3.zero;
        Vector3 randomPosTarget = Vector3.zero;

        // Reset bridge
        float moveBridgeZ = Random.Range(-5f, +5f);
        var randomPosBridge = UseRandomBridgePosition ? moveBridgeZ : 0;
        bridge.transform.localPosition = new Vector3(0, 0, randomPosBridge);
        bridgeStart.transform.localPosition = new Vector3(0, 0, randomPosBridge);
        bridgeEnd.transform.localPosition = new Vector3(0, 20, randomPosBridge);

        if (UseRandomBridgePosition)
        {
            button.transform.localPosition = new Vector3(0, 0, randomPosBridge) + ButtonStartingPos;
        }
        else
        {
            button.transform.localPosition = ButtonStartingPos;
        }
        // Reset Button

        // Reset Agents
        randomPosAgent = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-13.0f, +13.0f)) + AgentStartingPos;
        var posAgent = UseRandomAgentPosition ? randomPosAgent : AgentStartingPos;
        var rotAgent = UseRandomAgentRotation ? GetRandomRot() : AgentStartingRot;
        this.transform.localPosition = posAgent;
        this.transform.rotation = rotAgent;
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;       

        // Reset Goal
        randomPosTarget = new Vector3(Random.Range(-5f, 0f), 0f, Random.Range(-10f, +10f)) + TargetStartingPos;
        var posTarget = UseRandomGoalPosition ? randomPosTarget : TargetStartingPos;
        Target.transform.localPosition = posTarget;

    }


    // Observing the Environment (what information to collect), 8 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(Target.localPosition);                // Target (x,y,z)
        sensor.AddObservation(bridge.transform.localPosition);      // Bridge (x,y,z)
        sensor.AddObservation(button.transform.localPosition);      // button (x,y,z)
        sensor.AddObservation(this.transform.localPosition);        // Agent  (x,y,z)
        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);                    // Agent Vel (x)
        sensor.AddObservation(rBody.velocity.z);                    // Agent Vel (z)
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
        float sideAmount = 0f;


        var dirToGoForwardAction = actionBuffers.DiscreteActions[0];
        var dirToGoSideAction = actionBuffers.DiscreteActions[1];
        //var jumpAction = actionBuffers.DiscreteActions[2];

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
        //if ((jumpAction == 1) && isOnGround) //(this.transform.position.y < maxJumpHeight))
        //{
        //    rBody.velocity += (Vector3.up * jumpAmount * Time.deltaTime);
        //    isOnGround = false;
        //    //AddReward(-0.001f);
        //}
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
        //rBody.AddForce(controlSignal * forceAmount * Time.deltaTime); //  * Time.deltaTime);

        // Rewards
        // float distanceToTarget = Vector3.Distance(Object.localPosition, Target.localPosition)

        // Fell off platform
        // if the Agent falls off the platform, end the episode so that it can reset itself
        if (isOnWall == true) // Object.localPosition.y < -0.5 
        {
            endEpisode(FailedReward);
            StartCoroutine(
                GoalScoredSwapGroundMaterial(loseMaterial, 2));
        }
        // Penalty each Step
        //AddReward(-1f / 5000001);
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
        while (gameObjectA.transform.position != gameObjectB.transform.position)
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
        //Jump
        //discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

}
