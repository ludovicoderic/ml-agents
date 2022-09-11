//Put this script on your blue cube.
using UnityEngine;
using UnityEngine.Events;

// import ML-Agents package
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FinalAgentCooperativePush : Agent
{
    [SerializeField] private UnityEvent ResetCheckpointsEvent; // event to reset checkpoints order
    [SerializeField] private TrackCheckpointsBall trackCheckpoints; // get next checkpoint

    public int MaxEnviromentSteps = 10000;
    private FinalAgentCooperativeSettings m_FinalAgentCooperativeSettings;
    private Rigidbody m_AgentRb;  //cached on initialization
    Rigidbody rBall;

    [Header("Trigger Collider Tag To Detect")]
    public string tagToDetect = "goalArea"; //collider tag to detect

    private Collider m_col; // Agent Collider

    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }
    [System.Serializable]
    public class EpisodeEvent : UnityEvent
    {
    }

    [Header("Trigger Callbacks")]
    public TriggerEvent onTriggerEnterEvent = new TriggerEvent();
    public TriggerEvent onTriggerEnterExit = new TriggerEvent();
    public EpisodeEvent endEpisodeEvent = new EpisodeEvent();

    public Transform target;
    [SerializeField]
    private GameObject Ball;
    [SerializeField]
    private GameObject agent2; // Other Agent object
    [SerializeField]
    private GameObject agent3; // Other Agent object
    [SerializeField]
    private GameObject agent4; // Other Agent object

    private float fallMultiplier = 2.5f;

    //[SerializeField]
    //private GameObject button1; // Button1 object
    //[SerializeField]
    //private GameObject button2; // Button1 object
    //public float PositiveReward = 0.5f;
    public float FailedReward = -1f;

    public float CorrectCheckpointReward = 0.25f;
    public float WrongCheckpointReward = 0.25f;

    public float timePenalty = -0.5f;
    public float MoveStraightReward = 1f;
    public float wallPenalty = -0.5f;

    private int checkpointNumber = 8;
    private int indice = 0;
    private bool isOnGround = false;
    private bool isOnWall = false;

    //private float NextCheckpointPos = -5;

    public void CorrectCheckpointEntered(Collider col)
    {
        //Debug.Log($"Checkpoint correcto: {col}: {CorrectCheckpointReward / checkpointNumber}");
        AddReward(CorrectCheckpointReward / checkpointNumber);
        //NextCheckpointPos = NextCheckpointPos + 10f;
        if (indice < checkpointNumber - 1)
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
        //Debug.Log($"Checkpoint incorrecto: {col}: {WrongCheckpointReward / checkpointNumber}");
        AddReward(WrongCheckpointReward / checkpointNumber);

    }

    private void OnTriggerEnter(Collider col)
    {
        // Entered Goal
        if (col.CompareTag(tagToDetect))
        {
            onTriggerEnterEvent.Invoke(m_col);
            //print($"Goal Entered with: {m_col}");

        }
    }

    private void OnTriggerExit(Collider col)
    {
        // Exited Goal
        if (col.CompareTag(tagToDetect))
        {
            onTriggerEnterExit.Invoke(m_col);
            //print($"Goal Exited with: {m_col}");

        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((collision.gameObject.layer == 11 || collision.gameObject.layer == 6) && !isOnGround))
        {
            isOnGround = true;
        }
        if (collision.gameObject.CompareTag("wall") == true || collision.gameObject.layer == 10)
        {
            isOnWall = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Stay on the ground
        if (((collision.gameObject.layer == 11 || collision.gameObject.layer == 6) && !isOnGround))
        {
            isOnGround = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // si dejas de tocar suelo o algun objeto, no puedes saltar
        if ((collision.gameObject.layer == 11))
        {
            isOnGround = false;
        }
    }

    void Awake()
    {
        m_FinalAgentCooperativeSettings = FindObjectOfType<FinalAgentCooperativeSettings>();
        m_col = GetComponent<Collider>();
        rBall = Ball.GetComponent<Rigidbody>();
    }

    public override void Initialize()
    {
        // Cache the agent rb
        m_AgentRb = GetComponent<Rigidbody>();
        isOnWall = false;
    }

    public override void OnEpisodeBegin()
    {
        isOnWall = false;
    }

    /// <summary>
    /// Moves the agent according to the selected action.
    /// </summary>
    public void MoveAgent(ActionSegment<int> actionBuffers)
    {
        Vector3 controlSignal = Vector3.zero;

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        //float forwardAmount = 0f;
        //float sideAmount = 0f;

        var dirToGoForwardAction = actionBuffers[0];
        var rotateDirAction = actionBuffers[1];
        var jumpAction = actionBuffers[2];

        var speed = m_FinalAgentCooperativeSettings.agentRunSpeed;
        switch (dirToGoForwardAction)
        {
            case 1:
                dirToGo = transform.forward * 1f;
                //AddReward(1f / 25000);
                //print("forward");
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                speed = speed * 0.75f;
                //AddReward(-10f / 25000);
                //print("backwards");

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
        this.transform.Rotate(rotateDir * m_FinalAgentCooperativeSettings.agentRotationSpeed, Time.deltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * speed, ForceMode.VelocityChange);

        // Jump Action - if the agent is on contact with the floor
        if ((jumpAction == 1) && isOnGround) //(this.transform.position.y < maxJumpHeight))
        {
            m_AgentRb.velocity += (Vector3.up * m_FinalAgentCooperativeSettings.agentJumpAmount * Time.deltaTime);
            isOnGround = false;
            //AddReward(-0.001f);
        }
        if (m_AgentRb.velocity.y < 0)
        { // si estamos cayendo, caer nas rapido
            m_AgentRb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }

        //controlSignal.z = forwardAmount; // force applied along the x-axis, MoveX
        //controlSignal.y = 0;
        //controlSignal.x = sideAmount; // force applied along the z-axis, MoveZ
        //controlSignal.Normalize();

        //// Movemos el agente
        //this.transform.position += (controlSignal * m_FinalAgentCooperativeSettings.agentRunSpeed * Time.deltaTime);

        //if (controlSignal != Vector3.zero)
        //{
        //    Quaternion toRotation = Quaternion.LookRotation(controlSignal, Vector3.up);
        //    this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, toRotation, m_FinalAgentCooperativeSettings.agentRotationSpeed * Time.deltaTime);
        //}
    }

    // Observing the Environment (what information to collect), 18 values
    public override void CollectObservations(VectorSensor sensor)
    {
        //float d = 30f;

        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(Ball.transform.localPosition);       // Ball    (x,y,z)
        sensor.AddObservation(this.transform.localPosition);       // Agent   (x,y,z)
        sensor.AddObservation(this.transform.localRotation.y);     // AgentRot     (y)

        sensor.AddObservation(agent2.transform.localPosition.x);     // Agent2  (x,y,z)
        sensor.AddObservation(agent2.transform.localPosition.z);    
        sensor.AddObservation(agent3.transform.localPosition.x);     // Agent3  (x,y,z)
        sensor.AddObservation(agent3.transform.localPosition.z);     
        sensor.AddObservation(agent4.transform.localPosition.x);     // Agent4  (x,y,z)
        sensor.AddObservation(agent4.transform.localPosition.z);    

        // Agent and Ball velocity
        sensor.AddObservation(m_AgentRb.velocity.x);                // Agent Vel (x)
        sensor.AddObservation(m_AgentRb.velocity.z);                // Agent Vel (z)
        sensor.AddObservation(rBall.velocity.x);                    // Agent Vel (x)
        sensor.AddObservation(rBall.velocity.z);                    // Agent Vel (z)

        Vector3 checkpointForward = trackCheckpoints.GetNextCheckpoint(indice).transform.localPosition;

        //print($"Ball position is: {Ball.transform.localPosition}");
        //print($"Next checkpoint position is: {checkpointForward.x}"); 
        sensor.AddObservation(checkpointForward.x);               // NextCheck Pos (x)

    }

    void FixedUpdate()
    {
        if ( isOnWall == true) {
            AddReward(wallPenalty / MaxEnviromentSteps);

        }
        if (this.transform.localPosition.y < -2f ) // // Object.localPosition.y < -0.5 
        {
            print($"Agent fell of the ground: FailedReward: {FailedReward}");
            endEpisodeEvent.Invoke();
        }
        //print($"NextCheckpointPos: {NextCheckpointPos}");
        //AddReward(timePenalty / MaxEnviromentSteps);
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (actionBuffers.DiscreteActions[0] == 1f && actionBuffers.DiscreteActions[2] == 0f)
        {
            AddReward(MoveStraightReward / MaxEnviromentSteps);
            //print("move front reward");
        }
        // Move the agent using the action.
        MoveAgent(actionBuffers.DiscreteActions);
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
        // Jump
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
