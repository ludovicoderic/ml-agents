//Put this script on your blue cube.
using UnityEngine;
using UnityEngine.Events;

// import ML-Agents package
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FinalAgentCooperativeClimb : Agent
{
    [Header("Max Environment Steps")] public int MaxEnviromentSteps = 25000;

    [SerializeField] private TrackCheckpoints trackCheckpoints; // get next checkpoint
    [SerializeField] private UnityEvent ResetCheckpointsEvent; // event to reset checkpoints order

    private FinalAgentCooperativeSettings m_FinalAgentCooperativeSettings;
    private Rigidbody m_AgentRb;  //cached on initialization

    [Header("Trigger Collider Tag To Detect")]
    public string tagToDetect = "goal"; //collider tag to detect

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
    private GameObject agent2; // Other Agent object

    private float fallMultiplier = 2.5f;

    public float PositiveReward = 2f;
    public float FailedReward = -1f;
    //public float jumpReward = -0.1f;
    public float timePenalty = -0.5f;
    public float MoveStraightReward = 1f;
    public float wallPenalty = -0.5f;

    public float CorrectCheckpointReward = 0.25f;
    public float WrongCheckpointReward = -0.25f;

    //[SerializeField]
    //private GameObject button1; // Button1 object
    //[SerializeField]
    //private GameObject button2; // Button1 object

    private bool isOnGround = false;
    private bool isOnWall = false;
    private int checkpointNumber = 15;
    //private int m_ResetTimer = 0;
    private int indice = 0;

    public void CorrectCheckpointEntered(Collider col)
    {
        //Debug.Log($"Checkpoint correcto: {col}: {CorrectCheckpointReward / checkpointNumber}");
        AddReward(CorrectCheckpointReward / checkpointNumber);
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
        //AddReward(WrongCheckpointReward / checkpointNumber);

    }

    private void OnTriggerStay(Collider col)
    {
        // Entered Goal
        if (col.CompareTag(tagToDetect))
        {
            AddReward(PositiveReward);
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
        if ((collision.gameObject.layer == 11 && !isOnGround))
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
        if ((collision.gameObject.layer == 11 || collision.gameObject.layer == 6 || collision.gameObject.layer == 10) && !isOnGround)
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

        indice = 0;
        m_FinalAgentCooperativeSettings = FindObjectOfType<FinalAgentCooperativeSettings>();
        m_col = GetComponent<Collider>();

    }

    public override void Initialize()
    {
        // Cache the agent rb
        m_AgentRb = GetComponent<Rigidbody>();
        isOnWall = false;
    }

    public override void OnEpisodeBegin()
    {
        indice = 0;
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
        // Discrete Actions
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

        // Jump Action - if the agent is on contact with the floor
        if ((jumpAction == 1) && isOnGround)
        {
            m_AgentRb.velocity += (Vector3.up * m_FinalAgentCooperativeSettings.agentJumpAmount * Time.deltaTime);
            isOnGround = false;
            //AddReward(-0.001f);
        }
        if (m_AgentRb.velocity.y < 0)
        { // si estamos cayendo, caer nas rapido
            m_AgentRb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        // Movemos el agente
        if (isOnGround)
        {
            m_AgentRb.AddForce(dirToGo * speed, ForceMode.VelocityChange);
        }
        else
        {
            m_AgentRb.AddForce(dirToGo * speed / 4, ForceMode.VelocityChange);
        }
        //// Rotamos el agente
        this.transform.Rotate(rotateDir * m_FinalAgentCooperativeSettings.agentRotationSpeed, Time.deltaTime * 200f);

    }

    // Observing the Environment (what information to collect), 18 values
    public override void CollectObservations(VectorSensor sensor)
    {
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(target.localPosition);             // Target  (x,y,z)
        //sensor.AddObservation(bridge.transform.localPosition.y); // Bridge  (y)
        //sensor.AddObservation(button1.transform.localPosition);  // button1 (x,y,z)
        //sensor.AddObservation(button2.transform.localPosition);  // button2 (x,y,z)
        sensor.AddObservation(this.transform.localPosition);       // Agent   (x,y,z)
        sensor.AddObservation(agent2.transform.localPosition);     // Agent2  (x,y,z)

        //print($"indice: {indice}");

        Vector3 checkpointForward = trackCheckpoints.GetNextCheckpoint(indice).transform.localPosition;
        print($"Next checkpoint position is: {checkpointForward}");
        sensor.AddObservation(checkpointForward);

        // Agent velocity
        sensor.AddObservation(m_AgentRb.velocity.x);                // Agent Vel (x)
        sensor.AddObservation(m_AgentRb.velocity.z);                // Agent Vel (z)
    }

    void FixedUpdate()
    {
        if (isOnWall == true)
        {
            AddReward(wallPenalty / MaxEnviromentSteps);

        }
        if (this.transform.localPosition.y < -2f) // // Object.localPosition.y < -0.5 
        {
            print($"Agent fell of the ground: {FailedReward}");
            AddReward(FailedReward);
            endEpisodeEvent.Invoke();
        }
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (actionBuffers.DiscreteActions[0] == 1f && actionBuffers.DiscreteActions[2] == 0f)
        {
            AddReward(MoveStraightReward / MaxEnviromentSteps); // move straight without jumping
        } else if(actionBuffers.DiscreteActions[2] == 1f) {
            AddReward((-MoveStraightReward/2) / MaxEnviromentSteps); // move straight without jumping

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
