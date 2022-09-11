using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class FinalAgentCooperativeEnviromentBridge : MonoBehaviour
{

    // Agent Class
    [System.Serializable]
    public class PlayerInfo
    {
        public FinalAgentCooperativeBridge Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }

    // Button Class
    [System.Serializable]
    public class ButtonInfo
    {
        public Transform T;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
    }
    public MeshRenderer button1MeshRenderer;
    public MeshRenderer button2MeshRenderer;

    // Get Target and Bridge possition
    public Transform Target;

    [SerializeField] private TrackCheckpointsMulti trackCheckpoints; // get next checkpoint
    [SerializeField] private UnityEvent ResetCheckpointsEvent; // event to reset checkpoints order

    // Bridge position
    [SerializeField]
    private GameObject bridge; // Bridge object
    [SerializeField]
    private GameObject bridgeStart; //Bridge StartPosition
    [SerializeField]
    private GameObject bridgeEnd; //Bridge EndPosition

    [HideInInspector]
    public FinalAgentCooperativeBridge Agent1;
    [HideInInspector]
    public FinalAgentCooperativeBridge Agent2;

    // Button and Target, true if collision detected
    private bool buttonPressed1 = false;
    private bool buttonPressed2 = false;
    private bool agent1Goal = false;
    private bool agent2Goal = false;


    // Max Academy steps before this platform resets
    public int MaxEnvironmentSteps = 25000;

    // The area bounds.
    //[HideInInspector]
    //public Bounds areaBounds;

    // The ground. The bounds are used to spawn the elements and to change the material.
    public GameObject ground;
    public GameObject ground2;

    //public GameObject area;

    // We will be changing the ground material based on success/failue
    Material m_GroundMaterial; //cached on Awake()
    Renderer m_GroundRenderer;
    Renderer m_GroundRenderer2;

    //List of Agents and Blocks On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    public List<ButtonInfo> ButtonList = new List<ButtonInfo>();

    // Enviroment Variables
    public bool UseRandomAgentPosition = false;
    public bool UseRandomAgentRotation = false;
    public bool UseRandomButtonPosition = false;
    public bool UseRandomBridgePosition = false;
    public bool UseRandomGoalPosition = false;
    public float RandomQuantity = 1;

    private FinalAgentCooperativeSettings m_FinalAgentCooperativeSettings;

    private int m_NumberOfRemainingAgents;

    private SimpleMultiAgentGroup m_AgentGroup;

    private int m_ResetTimer;
    //private int checkpointNumber = 5;

    // Rewards
    public float PositiveReward = 0.5f;
    public float ButtonFindReward = 0.1f;
    public float CorrectCheckpointReward = 0.25f;
    public float WrongCheckpointReward = 0.25f;
    public float FailedReward = -0.5f;
    public float timePenalty = -0.5f;
    

    // Check if both agents are in the goal
    private bool done = false;

    private Vector3 bridgeEpisodePosition;
    private Vector3 ground2StartPosition;

    //private Collider col1 = null;
    //private Collider col2 = null;
    private int checkpointNumber = 2;

    void Start()
    {
        StartCoroutine(moveAtoB(bridge, bridgeStart, 1f));

        bridgeEpisodePosition = bridgeStart.transform.localPosition;
        //agent1Goal = false;
        //agent2Goal = false;

        // Get the ground's bounds
        //areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundRenderer2 = ground2.GetComponent<Renderer>();
        ground2StartPosition = ground2.transform.localPosition;

        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;

        m_FinalAgentCooperativeSettings = FindObjectOfType<FinalAgentCooperativeSettings>();

        // Players Remaining
        m_NumberOfRemainingAgents = AgentsList.Count;

        // Initialize Button
        foreach (var item in ButtonList)
        {
            item.StartingPos = item.T.transform.position;
            item.StartingRot = item.T.transform.rotation;
        }
        // Initialize TeamManager
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            //item.Agent.GetComponent<Collider>().gameObject.SetActive(true);
            m_AgentGroup.RegisterAgent(item.Agent);
        }
        Agent1 = AgentsList[0].Agent;
        Agent2 = AgentsList[1].Agent;

        ResetScene();
    }

    // Failed Episode, time run out or agent fall 
    public void FailedEpisode()
    {
        StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.failMaterial, 0.5f));
        //m_AgentGroup.AddGroupReward(FailedReward);
        Agent1.AddReward(FailedReward);
        Agent2.AddReward(FailedReward);
        m_AgentGroup.GroupEpisodeInterrupted();
        ResetScene();
    }

    // Goal Reached
    public void EndEpisode()
    {
        StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.goalScoredMaterial, 0.5f));
        //m_AgentGroup.AddGroupReward(PositiveReward);
        Agent1.AddReward(PositiveReward);
        Agent2.AddReward(PositiveReward);
        m_AgentGroup.EndGroupEpisode();
        ResetScene();
    }

    void FixedUpdate()
    {
        CheckRewards();
        //print($"m_NumberOfRemainingAgents: {m_NumberOfRemainingAgents}");
        //print($"done :{done}");

        // End if time > MaxEnvironmentSteps
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            FailedEpisode();
        }

        // Check if we should move the bridge
        // at least 1 button pressed
        if (buttonPressed1)
        {
            print($"Button 1 Pressed: {ButtonFindReward} / MaxEnvironmentSteps) each");
            //Agent1.AddReward(ButtonFindReward / MaxEnvironmentSteps);
            //Agent2.AddReward(ButtonFindReward / MaxEnvironmentSteps);

        }
        if (buttonPressed2)
        {
            print($"Button 2 Pressed: {(2 * ButtonFindReward)} / MaxEnvironmentSteps) each");
            //Agent1.AddReward((2*ButtonFindReward) / MaxEnvironmentSteps);
            //Agent2.AddReward((2*ButtonFindReward) / MaxEnvironmentSteps);

        }

        if (buttonPressed1 || buttonPressed2)
        {
            StartCoroutine(moveAtoB(bridge, bridgeEnd, 1f));
            // Reward each time it founds a button
            //m_AgentGroup.AddGroupReward(ButtonFindReward / MaxEnvironmentSteps);
            
        }
        else
        // If no button is being pressed, move the bridge down
        //if (!buttonPressed1 && !buttonPressed2)
        {
            //float distanceToTarget = Vector3.Distance(bridge.transform.localPosition, bridgeEpisodePosition);
            //if (distanceToTarget > 0.5f)
            //{
            StartCoroutine(moveAtoB(bridge, bridgeStart, 0.25f));
            //m_AgentGroup.AddGroupReward((-ButtonFindReward/4) / MaxEnvironmentSteps);

            //}
        }
        //print($"Bridge pos: {bridge.transform.localPosition}");

        // Reset the Scene (ONLY USE WHEN HEURISTIC MODE IS ON)
        if (Input.GetKey(KeyCode.R))
        {
            ResetScene();
        }

        //Hurry Up Penalty
        //m_AgentGroup.AddGroupReward(timePenalty / MaxEnvironmentSteps);
        Agent1.AddReward(timePenalty / MaxEnvironmentSteps);
        Agent2.AddReward(timePenalty / MaxEnvironmentSteps);
    }

    // Swap ground material, wait time seconds, then swap back to the regular material.
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        m_GroundRenderer2.material = mat;

        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
        m_GroundRenderer2.material = m_GroundMaterial;

    }

    //// Button Collider Functions, Enter y Exit
    //Button 1
    public void Button1Enter(Collider col)
    {
        //print($"Scored on button 1: {ButtonFindReward / MaxEnvironmentSteps}");
        buttonPressed1 = true;
        button1MeshRenderer.material = m_FinalAgentCooperativeSettings.goalScoredMaterial;
    }


    public void Button1Exit(Collider col)
    {
        buttonPressed1 = false;
        button1MeshRenderer.material = m_FinalAgentCooperativeSettings.failMaterial;
    }

    //Button 2
    public void Button2Exit(Collider col)
    {
        buttonPressed2 = false;
        button2MeshRenderer.material = m_FinalAgentCooperativeSettings.failMaterial;
    }

    public void Button2Enter(Collider col)
    {
        //print($"Scored on button 2: {(ButtonFindReward*2) / MaxEnvironmentSteps}");
        buttonPressed2 = true;
        button2MeshRenderer.material = m_FinalAgentCooperativeSettings.goalScoredMaterial;
    }

    // Move the bridge from A to B
    IEnumerator moveAtoB(GameObject gameObjectA, GameObject gameObjectB, float speed)
    {
        while (gameObjectA.transform.localPosition != gameObjectB.transform.localPosition)
        {
            gameObjectA.transform.localPosition = Vector3.MoveTowards(gameObjectA.transform.localPosition, gameObjectB.transform.localPosition, speed * Time.deltaTime);
            yield return null;
        }
    }

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    //// Check if agents have reached the goal
    public void GoalEntered (Collider col)
    {
        //print($"GoalEntered on {col.name}");
        if (col.name == "Agent1")
        {
            //print("Goal Crossed by Agent1");
            agent1Goal = true;
        }
        else if (col.name == "Agent2")
        {
            //print("Goal Crossed by Agent2");
            agent2Goal = true;
        }
    }

    public void GoalExited(Collider col)
    {
        //print($"GoalExited on {col.gameObject.name}");
        if (col.name == "Agent1")
        {
            agent1Goal = false;
        }
        else if (col.name == "Agent2")
        {
            agent2Goal = false;
        }
    }


    public void CorrectCheckpointEntered(Collider checkpoint, Collider agent)
    {
        if (agent.name == "Agent1")
        {
            Debug.Log($"'{checkpoint.name}' correcto cruzado por '{agent.name}: {(CorrectCheckpointReward / checkpointNumber)}'");
            if (checkpoint.name == "CheckpointSingle1")
            {
                Agent1.AddReward(CorrectCheckpointReward / checkpointNumber);
            }
            else
            {
                Agent1.AddReward((CorrectCheckpointReward*2) / checkpointNumber);
            }
        }
        else if (agent.name == "Agent2")
        {
            Debug.Log($"'{checkpoint.name}' correcto cruzado por '{agent.name}: {(CorrectCheckpointReward / checkpointNumber)}'");
            if (checkpoint.name == "CheckpointSingle1")
            {
                Agent2.AddReward(CorrectCheckpointReward / checkpointNumber);
            }
            else
            {
                Agent2.AddReward((CorrectCheckpointReward * 2) / checkpointNumber);
            }
        }
        //Debug.Log($"Checkpoint correcto '{checkpoint.name}' cruzado por: '{agent.name}'");
        //m_AgentGroup.AddGroupReward(CorrectCheckpointReward / checkpointNumber);
    }

    public void WrongCheckpointEntered(Collider checkpoint, Collider agent)
    {
        //Debug.Log($"Checkpoint incorrecto '{checkpoint.name}' cruzado por: '{agent.name}'");
        //m_AgentGroup.AddGroupReward(WrongCheckpointReward / checkpointNumber);
    }

    // Called when the agent moves the button into the goal.
    public void ScoredAGoal(Collider col)
    {
        print($"Scored on {gameObject.name}");
        
        //Decrement the counter
        m_NumberOfRemainingAgents--;

        //Disable the agent
        col.gameObject.SetActive(false);

        //Are we done?
        done = m_NumberOfRemainingAgents == 0;

        //Give Agent Rewards
        //m_AgentGroup.AddGroupReward(PositiveReward);
        Agent1.AddReward(PositiveReward);
        Agent2.AddReward(PositiveReward);
        // Swap ground material for a bit to indicate we scored.
        //StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.goalScoredMaterial, 0.5f));
    }


    public void ResetAgentsPosition()
    {
        foreach (var item in AgentsList)
        {
            var pos = item.StartingPos;
            var rot = item.StartingRot;
            //print($"Pos {pos} on Agent");

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            //item.Agent.GetComponent<Collider>().gameObject.SetActive(true);
        }
    }

    public void ResetScene()
    {
        var randomDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("distance_offset", RandomQuantity);

        // Reset checkpoints order
        ResetCheckpointsEvent.Invoke();

        // Reset Variables
        m_ResetTimer = 0;
        done = false;
        buttonPressed1 = false;
        buttonPressed2 = false;
        //agent1Goal = false;
        //agent2Goal = false;

        //Reset bridge
        float moveBridgeZ = Random.Range(-5.0f, +5.0f);
        var bridgePos = UseRandomBridgePosition ? moveBridgeZ : 0;

        bridge.transform.localPosition = bridgeStart.transform.localPosition; ; //new Vector3(0, 0, bridgePos);
        //bridgeStart.transform.localPosition = new Vector3(0, 0, bridgePos);
        //bridgeEnd.transform.localPosition = new Vector3(0, 20, bridgePos);

        //bridgeEpisodePosition = bridgeStart.transform.localPosition;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            Vector3 randomPos = new Vector3(Random.Range(-22.0f, -16.0f), 4.47f, Random.Range(-15.0f, +15.0f));
            var pos = UseRandomAgentPosition ? randomPos : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;
            //print($"Pos {pos} on Agent");

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.GetComponent<Collider>().gameObject.SetActive(true);
        }
        Agent1 = AgentsList[0].Agent;
        Agent2 = AgentsList[1].Agent;

        //Reset Buttons
        foreach (var item in ButtonList)
        {
            Vector3 randomPos = Vector3.zero;

            //print($"{item.T.name}");
            if(item.T.name == "Switch1")
            {
                randomPos = new Vector3(Random.Range(-4.0f, +3.0f), 0f, Random.Range(-10.0f, +10.0f));
                if (UseRandomButtonPosition)
                {
                    item.T.transform.localPosition = randomPos;
                }
                else
                {
                    item.T.transform.position = item.StartingPos;
                }
            }
            //if (item.T.name == "Switch2")
            //{
            //    randomPos = new Vector3(Random.Range(19.0f, +23.0f), 0f, Random.Range(-10.0f, +10.0f));
            //}
            //if (UseRandomButtonPosition)
            //{
            //    item.T.transform.localPosition = randomPos;
            //}
            //else
            //{
            //    item.T.transform.position = item.StartingPos;
            //}
        }

        // Reset ground2
        ground2.transform.localPosition = new Vector3(Random.Range(0f, +7.0f), 0f, 0f) * randomDistance + ground2StartPosition;
        //Reset counter
        m_NumberOfRemainingAgents = AgentsList.Count;
        //print($"Reset done");
    }

    //private float distanceToTarget()
    //{
    //    float distance = Vector3.Distance(Ball.transform.localPosition, Target.transform.localPosition);
    //    // Debug.Log("Distance: " + distance);
    //    // Debug.Log("Ball: " + Ball.transform.position);
    //    // Debug.Log("Target: " + Target.transform.localPosition)
    //    return distance;
    //}

    public void CheckRewards()
    {

        float distance1 = Vector3.Distance(Agent1.transform.localPosition, ButtonList[0].T.localPosition);
        float distance2 = Vector3.Distance(Agent2.transform.localPosition, ButtonList[1].T.localPosition);

        //// Recompensas Blue - Agent1
        //// si el primero pulsa el boton, recompensarlo
        //if (distance1 <= 2.5f && buttonPressed1)
        //{
        //    print($"Reward 1 : Agent 1 on the Button");
        //    Agent1.AddReward(2f / MaxEnvironmentSteps);

        //    if (Agent2.transform.localPosition.x > -8) // si mientras se pulsa, el segundo comienza a cruzar
        //    {
        //        print($"Reward 1.1 : Agent 2 passed button");
        //        m_AgentGroup.AddGroupReward(4f / MaxEnvironmentSteps);
        //        Agent2.AddReward(1f / MaxEnvironmentSteps);
        //        Agent1.AddReward(2f / MaxEnvironmentSteps);
        //    }
        //    else // si aun no cruza
        //    {
        //        Agent1.AddReward(1f / MaxEnvironmentSteps);
        //    }
        //}
        //else // primero no pulsa el boton
        //{
        //    Agent1.AddReward(-1f / MaxEnvironmentSteps);

        //}

        //// Recompensas Pink - Agent2
        //// si el segundo cruza el puente
        //if (Agent2.transform.localPosition.x > 1f)
        //{
        //    print($"Reward 2: Agent 2 crossed the Bridge");
        //    Agent2.AddReward(4f / MaxEnvironmentSteps);

        //    // y agente 1 esta pulsado el primer boton
        //    if (distance1 <= 2.5f && buttonPressed1)
        //    {
        //        print($"Reward 2.1: button 1 Pressed");
        //        Agent1.AddReward(2f / MaxEnvironmentSteps);
        //        Agent2.AddReward(2f / MaxEnvironmentSteps);

        //    }

        //    // y agente 2 esta pulsado el segundo boton
        //    if (distance2 <= 2.5f && buttonPressed2)
        //    {
        //        print($"Reward 2.2: button 2 Pressed");
        //        Agent2.AddReward(4f / MaxEnvironmentSteps);
        //    }
        //}

        //// Recompensas grupo
        // si alguno esta esperando en la meta
        if (agent1Goal)
        {
            print($"Reward 3.1: agent1Goal  {0.5f} / MaxEnvironmentSteps) agent2");
            Agent2.AddReward(0.5f / MaxEnvironmentSteps);
        }
        if (agent2Goal)
        {
            print($"Reward 3.2: agent2Goal {0.5f} / MaxEnvironmentSteps) agent1");
            Agent1.AddReward(0.5f / MaxEnvironmentSteps);
        }
        // Booth agents crossed the bridge
        if (Agent1.transform.localPosition.x > 1f && Agent2.transform.localPosition.x > 1f)
        {
            print($"Reward 4: Both Agents hace crossed the Bridge {2f} / MaxEnvironmentSteps) each");
            //m_AgentGroup.AddGroupReward(5f / MaxEnvironmentSteps);
            Agent1.AddReward(2f / MaxEnvironmentSteps);
            Agent2.AddReward(2f / MaxEnvironmentSteps);
        }
        // End episode if all the agents get to the goal, give Positive Reward
        if ((agent1Goal && agent2Goal) || done)
        {
            print($"Reward 5: GOAL ACHIEVED {PositiveReward} each");
            EndEpisode();
        }

    }

    //public void OldCheckRewards()
    //{

    //    float distance = Vector3.Distance(Agent1.transform.localPosition, ButtonList[0].T.localPosition);
    //    //print($"Distance: {distance}");
    //    if (Agent2.transform.localPosition.x <= -12f)
    //    {
    //        print($"Reward 1.1 : Agent 1 on the Button");
    //        //m_AgentGroup.AddGroupReward(1f / MaxEnvironmentSteps);
    //        //Agent1.AddReward(1f / MaxEnvironmentSteps);
    //        Agent2.AddReward(-1f / MaxEnvironmentSteps);
    //    }
    //    else if (Agent2.transform.localPosition.x > -12f)
    //    {
    //        print($"Reward 1.2 : Agent 2 passed button");
    //        //m_AgentGroup.AddGroupReward(4f / MaxEnvironmentSteps);
    //        Agent2.AddReward(1f / MaxEnvironmentSteps);
    //        //Agent1.AddReward(2f / MaxEnvironmentSteps);
    //    }

    //    // si el primero no pasa, recompensarlo
    //    if (distance <= 2.5f && buttonPressed1)
    //    {
    //        print($"Reward 1.2 : Agent 1 on the Button");

    //        Agent1.AddReward(2f / MaxEnvironmentSteps);

    //        if (Agent2.transform.localPosition.x <= -12f)
    //        {
    //            print($"Reward 1.1 : Agent 1 on the Button");
    //            //m_AgentGroup.AddGroupReward(1f / MaxEnvironmentSteps);
    //            //Agent1.AddReward(1f / MaxEnvironmentSteps);
    //            Agent2.AddReward(-1f / MaxEnvironmentSteps);
    //        }
    //        else if (Agent2.transform.localPosition.x > -12f)
    //        {
    //            print($"Reward 1.2 : Agent 2 passed button");
    //            //m_AgentGroup.AddGroupReward(4f / MaxEnvironmentSteps);
    //            Agent2.AddReward(1f / MaxEnvironmentSteps);
    //            //Agent1.AddReward(2f / MaxEnvironmentSteps);
    //        }
    //    }

    //    if (Agent2.transform.localPosition.x > -3.5f)
    //    {
    //        print($"Reward 1.3 : Agent 2 crossing bridge");
    //        Agent2.AddReward(4f / MaxEnvironmentSteps);
    //        Agent1.AddReward(2f / MaxEnvironmentSteps);
    //    }
    //    else if (Agent2.transform.localPosition.x > -7.5f)
    //    {
    //        print($"Reward 1.3 : Agent 2 crossing bridge");
    //        //m_AgentGroup.AddGroupReward(4f / MaxEnvironmentSteps);
    //        Agent2.AddReward(2f / MaxEnvironmentSteps);
    //    }
    //    // si el primero pasa
    //    if (Agent2.transform.localPosition.x > 1f)
    //    {
    //        print($"Reward 2: Agent 2 crossed the Bridge");
    //        Agent2.AddReward(4f / MaxEnvironmentSteps);
    //        //m_AgentGroup.AddGroupReward(4f / MaxEnvironmentSteps);


    //        // y esta pulsado el primer boton
    //        if (buttonPressed1)
    //        {
    //            print($"Reward 2.1: buttonPressed1");
    //            Agent1.AddReward(2f / MaxEnvironmentSteps);
    //            Agent2.AddReward(2f / MaxEnvironmentSteps);

    //        }
    //        float distance1 = Vector3.Distance(Agent2.transform.localPosition, ButtonList[1].T.localPosition);

    //        // y esta pulsado el segundo boton
    //        if (distance1 <= 2.5f && buttonPressed2)
    //        {
    //            print($"Reward 2.2: buttonPressed2");
    //            //m_AgentGroup.AddGroupReward(8f / MaxEnvironmentSteps);
    //            Agent1.AddReward(1f / MaxEnvironmentSteps);
    //            Agent2.AddReward(4f / MaxEnvironmentSteps);

    //        }
    //    }
    //    // si el segundo no pasa pero el primero esta esperando en la meta
    //    else if (agent1Goal)
    //    {
    //        print($"Reward 3.1: agent1Goal");
    //        Agent1.AddReward(2f / MaxEnvironmentSteps);
    //    }
    //    if (agent2Goal)
    //    {
    //        print($"Reward 3.2: agent2Goal");
    //        Agent2.AddReward(2f / MaxEnvironmentSteps);
    //    }
    //    if (Agent1.transform.localPosition.x > 1f && Agent2.transform.localPosition.x > 1f)
    //    {
    //        print($"Reward 4: Both Agents hace crossed the Bridge");
    //        //m_AgentGroup.AddGroupReward(5f / MaxEnvironmentSteps);
    //        Agent1.AddReward(5f / MaxEnvironmentSteps);
    //        Agent2.AddReward(5f / MaxEnvironmentSteps);
    //    }

    //    // End episode if all the agents get to the goal, give Positive Reward
    //    if ((agent1Goal && agent2Goal) || done)
    //    {
    //        Agent1.AddReward(PositiveReward);
    //        Agent2.AddReward(PositiveReward);
    //        //Agent1.EndEpisode();
    //        //Agent2.EndEpisode();

    //        print("Reward 5: GOAL ACHIEVED");
    //        EndEpisode();
    //    }

    }
