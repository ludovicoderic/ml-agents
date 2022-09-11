//Put this script on your blue cube.
using UnityEngine;
using UnityEngine.Events;

// import ML-Agents package
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class FinalAgentCooperativeBridge : Agent
{
    public int MaxEnvironmentSteps = 25000;

    private FinalAgentCooperativeSettings m_FinalAgentCooperativeSettings;
    [SerializeField] private TrackCheckpoints trackCheckpoints; // get next checkpoint
    private Rigidbody m_AgentRb;  //cached on initialization

    [Header("Trigger Collider Tag To Detect")]
    public string tagToDetect = "goal"; //collider tag to detect

    [Header("Goal Value")]
    //public float GoalValue = 2;
    //public float FallPenalty = -1;


    private Collider m_col;
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
    public EpisodeEvent endEpisodeEvent = new EpisodeEvent();  // reset scene, end episode
    [SerializeField] private UnityEvent ResetCheckpointsEvent; // event to reset checkpoints order

    public Transform Target;
    [SerializeField]
    private GameObject bridge; // Bridge object
    [SerializeField]
    private GameObject agent2; // Other Agent object
    [SerializeField]
    private GameObject button1; // Button1 object
    [SerializeField]
    private GameObject button2; // Button1 object

    private bool isOnWall = false;
    private bool OnTarget = false;
    private bool OnButton1 = false;
    private bool OnButton2 = false;
    

    public float PositiveReward = 0.5f;
    public float ButtonFindReward = 0.5f;
    public float FailedReward = -1f;
    public float timePenalty = -0.5f;
    public float moveReward = 0.5f;
    
    public float CorrectCheckpointReward = 0.25f;
    public float WrongCheckpointReward = -0.25f;

    //private int checkpointNumber = 5;
    //private int indice = 0;
    private bool buttonPressed = false;

    //private bool bridgeCrossed = false;
    private bool areaCrossed = false;
    void Awake()
    {
        //print($"Other agent name: {}");

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
        isOnWall = false;
        //bridgeCrossed = false;
        areaCrossed = false;
}

    private void OnTriggerEnter(Collider col)
    {
        
        if (col.gameObject.CompareTag("wall") == true || col.gameObject.layer == 10)
        {
            isOnWall = true;
        }
        if (col.gameObject.CompareTag("switchOn") == true)
        {
            buttonPressed = true;
            //print("button pressed");
        }
    }

    private void OnTriggerStay(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            OnTarget = true;
            onTriggerEnterEvent.Invoke(m_col);
            //AddReward(PositiveReward);
        }
        if (col.gameObject.CompareTag("switchOn") == true)
        {
            //print("button stay pressed");
            //AddReward(ButtonFindReward / MaxEnviromentSteps);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            OnTarget = false;

            onTriggerEnterExit.Invoke(m_col);
            //AddReward((-ButtonFindReward / 4) / MaxEnviromentSteps);
        }
        if (col.gameObject.CompareTag("switchOn") == true)
        {
            //print("button stay pressed");
            buttonPressed = false;
        }
    }

    public void CorrectCheckpointEntered(Collider col)
    {
        //AddReward(CorrectCheckpointReward / checkpointNumber);
        //if (indice < checkpointNumber - 1)
        //{
        //    indice = indice + 1;
        //}
        //else
        //{
        //    indice = 0;
        //}
        //print($"Checkpoint {indice} correcto: {CorrectCheckpointReward / checkpointNumber} by {col}");
    }

    public void WrongCheckpointEntered(Collider col)
    {
        //Debug.Log($"Checkpoint incorrecto: {col}: {WrongCheckpointReward / checkpointNumber}");
        //AddReward(WrongCheckpointReward / checkpointNumber);

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
        this.transform.Rotate(rotateDir * m_FinalAgentCooperativeSettings.agentRotationSpeed, Time.deltaTime * 200f);
        m_AgentRb.AddForce(dirToGo * speed, ForceMode.VelocityChange);

        if (m_AgentRb.velocity.y < 0)
        { // si estamos cayendo, caer nas rapido
            m_AgentRb.velocity += Vector3.up * Physics.gravity.y * (2.5f - 1) * Time.deltaTime;
        }
    }

    // Observing the Environment (what information to collect), 18 values
    public override void CollectObservations(VectorSensor sensor)
    {
        float d = 30f;
        // Normalizar valores !!!!!!!!!!!!!!!!!!!!!
        // Target and Agent positions 
        sensor.AddObservation(Target.localPosition.x / d);                  // Target       (x,z)
        sensor.AddObservation(Target.localPosition.z / d);
        sensor.AddObservation(bridge.transform.localPosition.x / d);        // Bridge       (x,z)
        sensor.AddObservation(bridge.transform.localPosition.z / d);       

        sensor.AddObservation(this.transform.localPosition / d);            // Agent        (x,y,z)
        sensor.AddObservation(this.transform.localRotation.y);              // AgentRot     (y)

        sensor.AddObservation(agent2.transform.localPosition.x / d);        // Agent2 Pos   (x,z)
        sensor.AddObservation(agent2.transform.localPosition.z / d);

        sensor.AddObservation(button1.transform.localPosition.x / d);       // button1      (x,z)
        sensor.AddObservation(button1.transform.localPosition.z / d);       //              
        sensor.AddObservation(button2.transform.localPosition.x / d);       // button2      (x,z)
        sensor.AddObservation(button2.transform.localPosition.z / d);       //              
        sensor.AddObservation(OnButton1);                                   // OnButton1    (bool)
        sensor.AddObservation(OnButton2);                                   // OnButton2    (bool)


        // Agent velocity
        //sensor.AddObservation(m_AgentRb.velocity.x);                    // Agent Vel (x)
        //sensor.AddObservation(m_AgentRb.velocity.z);                    // Agent Vel (z)
        //print($"Goal position is: {Target.localPosition}");
        //print($"Bridge position is: {bridge.transform.localPosition}");
        //print($"Agent1 position: {this.transform.localPosition}");
        //print($"Agent2 position: {agent2.transform.localPosition}");
        //print(OnButton1);                                   // OnButton1    (bool)
        //print(OnButton2);
        //print($"Button1 position: {button1.transform.localPosition}");
        //print($"Button2 position: {button2.transform.localPosition}");


        //Vector3 checkpointForward = trackCheckpoints.GetNextCheckpoint(indice).transform.localPosition;
        ////print($"Next checkpoint position is: {checkpointForward.x}"); 

        ////float directionDot = Vector3.Dot(transform.forward, checkpointForward);
        //sensor.AddObservation(checkpointForward.x / d);               // NextCheck Pos (x)
    }   

    void FixedUpdate()
    {
        if (isOnWall == true) // // Object.localPosition.y < -0.5 
        {
            //AddReward(FailedReward);
            endEpisodeEvent.Invoke();
        }
        AddReward(timePenalty / MaxEnvironmentSteps);
        //print($"OnButton1: {OnButton1}");
        //print($"OnButton2: {OnButton2}");
    }

    /// <summary>
    /// Called every step of the engine. Here the agent takes an action.
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Move the agent using the action.
        if (actionBuffers.DiscreteActions[0] == 1f && (!buttonPressed && !OnTarget)) {
            AddReward(moveReward / MaxEnvironmentSteps);
            //print("move front reward");
        }
        MoveAgent(actionBuffers.DiscreteActions);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (agent2.name == "Agent1") {
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
        } else {
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
        }
        
    }
    //// Button Collider Functions, Enter y Exit
    //Button 1
    public void Button1Enter(Collider col)
    {
        //print($"Scored on button {gameObject.name}: {ButtonFindReward / MaxEnviromentSteps}");
        OnButton1 = true;
    }

    public void Button1Exit(Collider col)
    {
        OnButton1 = false;
    }

    //Button 2
    public void Button2Exit(Collider col)
    {
        OnButton2 = false;
    }

    public void Button2Enter(Collider col)
    {
        //print($"Scored on button {gameObject.name}: {ButtonFindReward / MaxEnviromentSteps}");
        OnButton2 = true;
    }

    public void CheckRewards()
    {
        float distance1 = Vector3.Distance(this.transform.localPosition, button1.transform.localPosition);
        float distance2 = Vector3.Distance(this.transform.localPosition, button2.transform.localPosition);

        if (agent2.transform.localPosition.x < -5f && this.transform.localPosition.x < -5f  && OnButton1) // si aun no ha cruzado y pulsa el boton
        {
            AddReward(0.5f / MaxEnvironmentSteps);
        }
        else if (agent2.transform.localPosition.x > -1f && this.transform.localPosition.x > -1f && OnButton1) // si han cruzado los dos
        {
            if (distance2 <= 2.5f && OnButton2) // y esta sobre el segundo
            {
                AddReward(-1f / MaxEnvironmentSteps);
            }
        }


            //if (agent2.transform.localPosition.x > -1) // si el otro ya ha cruza cruzado
            //{
            //    //print($"Reward 1.1 : Agent 2 passed button");
            //    //AddReward(-0.5 / MaxEnvironmentSteps);
            //    if (distance1 <= 2.5f && OnButton1) // si esta sobre el primer boton
            //    {
            //        AddReward(1f / MaxEnvironmentSteps);
            //    }
            //}
            //else

            if (this.transform.localPosition.x > -1f && !areaCrossed) // si ha cruzado
        {
            areaCrossed = true;
            AddReward(1.5f);

            //if (distance2 <= 2.5f && OnButton2) // y esta sobre el segundo boton
            //{
            //    AddReward(2f / MaxEnvironmentSteps);
            //}
        }
        //else if (this.transform.localPosition.x > -5f && !bridgeCrossed) // si comienza a cruzar
        //{
        //    bridgeCrossed = true;
        //    AddReward(0.5f);
        //}
        if (agent2.transform.localPosition.x > -1f) //si ha cruzado el otro
        {
            if (distance1 <= 2.5f && OnButton1) // y esta sobre el primer boton
            {
                AddReward(-1f / MaxEnvironmentSteps);
            }
        }
        else if (agent2.transform.localPosition.x > -5f) // si el otro comienza a cruzar
        {
            AddReward(0.5f / MaxEnvironmentSteps);

            if (distance1 <= 2.5f && OnButton1) // y esta sobre el primer boton
            {
                AddReward(1.5f / MaxEnvironmentSteps);
            }
            else if (distance2 <= 2.5f && OnButton2) // y esta sobre el segundo
            {
                AddReward(2f / MaxEnvironmentSteps);
            }
        }
    }
}


