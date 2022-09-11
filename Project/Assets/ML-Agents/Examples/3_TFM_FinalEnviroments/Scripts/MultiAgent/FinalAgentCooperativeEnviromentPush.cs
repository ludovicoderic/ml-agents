using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class FinalAgentCooperativeEnviromentPush : MonoBehaviour
{
    [SerializeField] private UnityEvent ResetCheckpointsEvent; // event to reset checkpoints order

    public Transform FullMap;
    [HideInInspector]
    public Quaternion MapStartingRot;
    [HideInInspector]
    public Vector3 BumpsStartingPos;


    public Transform Bumps;

    [System.Serializable]
    public class PlayerInfo
    {
        public FinalAgentCooperativePush Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }

    [System.Serializable]
    public class ButtonInfo
    {
        public Transform T;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
    }

    public GameObject Target;

    Rigidbody rBall;
    [SerializeField]
    private GameObject Ball;
    [HideInInspector]
    public Vector3 BallStartingPos;

    //private bool agent1Goal = false;
    //private bool agent2Goal = false;


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnviromentSteps = 25000;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;
    //public GameObject area;

    Material m_GroundMaterial; //cached on Awake()

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    //List of Blocks On Platform

    public bool UseRandomAgentPosition = false;
    public bool UseRandomAgentRotation = false;
    public bool UseRandomBallPosition = false;
    public bool UseRandomMapPosition = false;

    public bool UseRandomBumps = false;
    private float bumpsMaxHeight = 0.75f;
    public float RandomQuantity = 1;

    private FinalAgentCooperativeSettings m_FinalAgentCooperativeSettings;

    private int m_NumberOfRemainingAgents;

    private SimpleMultiAgentGroup m_AgentGroup;

    private int checkpointNumber = 8;
    private int m_ResetTimer = 0;
    //private int totalReward = 0;

    public float GoalDistanceRange = 4f;
    public float AgentsGroupDistance = 4f;
    public float BallDistance = 3f;
    
    public float PositiveReward = 2f;
    public float FailedReward = -1f;
    public float timePenalty = -0.5f;

    public float CorrectCheckpointReward = 0.5f;
    public float WrongCheckpointReward = 0.5f;
    public float AgentsListGroupReward = 0.5f;
    public float AgentsBallCloseReward = 0.5f;

    private FinalAgentCooperativePush Agent1;
    private FinalAgentCooperativePush Agent2;
    //private FinalAgentCooperativePush Agent3;
    //private FinalAgentCooperativePush Agent4;

    private bool done = false;

    //private Collider col1 = null;
    //private Collider col2 = null;

    void Start()
    {
        Agent1 = AgentsList[0].Agent;
        Agent2 = AgentsList[1].Agent;
        //Agent3 = AgentsList[2].Agent;
        //Agent4 = AgentsList[3].Agent;

        //agent1Goal = false;
        //agent2Goal = false;
        rBall = Ball.GetComponent<Rigidbody>();
        BallStartingPos = Ball.transform.localPosition;

        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();

        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        m_FinalAgentCooperativeSettings = FindObjectOfType<FinalAgentCooperativeSettings>();

        //Reset Players Remaining
        m_NumberOfRemainingAgents = AgentsList.Count;

        MapStartingRot = FullMap.transform.rotation;
        BumpsStartingPos = Bumps.transform.localPosition;

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
        ResetScene();
    }

    public void CorrectCheckpointEntered(Collider checkpoint)
    {
        Debug.Log($"Checkpoint correcto '{checkpoint.name}' cruzado: {CorrectCheckpointReward / checkpointNumber}");
        m_AgentGroup.AddGroupReward(CorrectCheckpointReward / checkpointNumber);
    }

    public void WrongCheckpointEntered(Collider checkpoint)
    {
        Debug.Log($"Checkpoint incorrecto '{checkpoint.name}' cruzado: {WrongCheckpointReward / checkpointNumber}");
        m_AgentGroup.AddGroupReward(WrongCheckpointReward / checkpointNumber);
    }



    private float distanceToTarget()
    {
        float distance = Vector3.Distance(Ball.transform.localPosition, Target.transform.localPosition);
        // Debug.Log("Distance: " + distance);
        // Debug.Log("Ball: " + Ball.transform.position);
        // Debug.Log("Target: " + Target.transform.localPosition)
        return distance;
    }

    private float distanceToBall()
    {
        float distance1 = Vector3.Distance(Agent1.transform.localPosition, Ball.transform.localPosition);
        float distance2 = Vector3.Distance(Agent2.transform.localPosition, Ball.transform.localPosition);
        //float distance3 = Vector3.Distance(Agent3.transform.localPosition, Ball.transform.localPosition);
        //float distance4 = Vector3.Distance(Agent4.transform.localPosition, Ball.transform.localPosition);
        // Debug.Log("Distance: " + distance);
        // Debug.Log("Ball: " + Ball.transform.position);
        // Debug.Log("Target: " + Target.transform.localPosition)
        return ((distance1 + distance2 ) / 2);
    }

    private float distanceBetweenAgents()
    {
        float distance1 = Vector3.Distance(Agent1.transform.localPosition, Agent2.transform.localPosition);
        //float distance2 = Vector3.Distance(Agent2.transform.localPosition, Agent3.transform.localPosition);
        //float distance3 = Vector3.Distance(Agent3.transform.localPosition, Agent1.transform.localPosition);
        //float distance4 = Vector3.Distance(Agent1.transform.localPosition, Agent4.transform.localPosition);
        //float distance5 = Vector3.Distance(Agent2.transform.localPosition, Agent4.transform.localPosition);
        //float distance6 = Vector3.Distance(Agent3.transform.localPosition, Agent4.transform.localPosition);
        //Debug.Log("Distance: " + distance);
        // Debug.Log("Ball: " + Ball.transform.position);
        // Debug.Log("Target: " + Target.transform.localPosition)
        return ((distance1 ) ); 
    }

    void FixedUpdate()
    {
        //print($"m_NumberOfRemainingAgents: {m_NumberOfRemainingAgents}");
        //print($"done :{done}");

        // End if time > MaxEnviromentSteps
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnviromentSteps && MaxEnviromentSteps > 0)
        {
            print($"MaxEnviromentSteps: {FailedReward}");

            m_AgentGroup.GroupEpisodeInterrupted();
            FailedEpisode();
        }

        if ( Ball.transform.localPosition.y < -2f)//|| isOnWall == true) // // Object.localPosition.y < -0.5 
        {
            print($"Sphere fell of the ground: {FailedReward}");
            FailedEpisode();
        }
        // End episode if all the agents get tthe ball to the goal
        float distanceTarget = distanceToTarget();
        if (distanceTarget <= GoalDistanceRange)
        {
            print($"GoalEntered with the sphere: {PositiveReward}");
            StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.goalScoredMaterial, 0.5f));
            m_AgentGroup.AddGroupReward(PositiveReward);
            EndEpisode();
        }

        float distanceAgents = distanceBetweenAgents();
        if (distanceAgents <= AgentsGroupDistance)
        {
            print($"Agents are in a group: {AgentsListGroupReward / MaxEnviromentSteps}");
            m_AgentGroup.AddGroupReward(AgentsListGroupReward / MaxEnviromentSteps);
        }
        else
        {
            m_AgentGroup.AddGroupReward(-AgentsListGroupReward / MaxEnviromentSteps);

        }

        float distanceBall = distanceToBall();
        if (distanceBall <= BallDistance)
        {
            print($"Agents are close to the ball: {AgentsBallCloseReward / MaxEnviromentSteps}");

            m_AgentGroup.AddGroupReward(AgentsBallCloseReward / MaxEnviromentSteps);
        }
        else
        {
            m_AgentGroup.AddGroupReward(-AgentsBallCloseReward / MaxEnviromentSteps);
        }

        //print($"Distance between Agents: {(int)distanceAgents}");
        //print($"Distance to the Ball (mean): {(int)distanceBall}");
        if (Input.GetKey(KeyCode.R))
        {
            ResetScene();
        }
        //print($"Steps: {m_ResetTimer}");
        m_AgentGroup.AddGroupReward(timePenalty / MaxEnviromentSteps);
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
    }


    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }



    public void GoalEntered(Collider col)
    {
        //print($"GoalEntered with: {col.name}");
        //if (col.name == "Agent1")
        //{
        //    agent1Goal = true;
        //}
        //else if (col.name == "Agent2")
        //{
        //    agent2Goal = true;
        //}
    }

    public void GoalExited(Collider col)
    {
        //print($"GoalExited with: {col.gameObject.name}");
        //if (col.name == "Agent1")
        //{
        //    agent1Goal = false;
        //}
        //else if (col.name == "Agent2")
        //{
        //    agent2Goal = false;
        //}
    }



    /// <summary>
    /// Called when the agent moves the button into the goal.
    /// </summary>
    public void ScoredAGoal(Collider col)
    {
        print($"Scored on {gameObject.name}: {PositiveReward}");

        //Decrement the counter
        m_NumberOfRemainingAgents--;

        //Disable the agent
        //col.gameObject.SetActive(false);

        //Are we done?
        done = m_NumberOfRemainingAgents == 0;

        //Give Agent Rewards
        //m_AgentGroup.AddGroupReward(PositiveReward);

        // Swap ground material for a bit to indicate we scored.
        //StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.goalScoredMaterial, 0.5f));
    }

    public void FailedEpisode()
    {
        StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.failMaterial, 0.5f));
        m_AgentGroup.AddGroupReward(FailedReward);
        m_AgentGroup.EndGroupEpisode();
        ResetScene();
    }

    public void EndEpisode()
    {
        m_AgentGroup.EndGroupEpisode();
        ResetScene();
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
        // Reset checkpoints order
        ResetCheckpointsEvent.Invoke();

        // Reset Variables
        m_ResetTimer = 0;
        done = false;
        //agent1Goal = false;
        //agent2Goal = false;
        var randomDistance = Academy.Instance.EnvironmentParameters.GetWithDefault("distance_offset", RandomQuantity);


        //Reset Agents
        foreach (var item in AgentsList)
        {
            Vector3 randomPos = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-5.0f, +5.0f)) + item.StartingPos;
            var pos = UseRandomAgentPosition ? randomPos : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;
            //print($"Pos {pos} on Agent");

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.GetComponent<Collider>().gameObject.SetActive(true);
        }

        // Reset Ball
        Vector3 randomPosBall = Vector3.zero;
        randomPosBall = new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-4f, +4f)) + BallStartingPos;
        var posTarget = UseRandomBallPosition ? randomPosBall : BallStartingPos;
        Ball.transform.localPosition = posTarget;
        rBall.angularVelocity = Vector3.zero;
        rBall.velocity = Vector3.zero;


        // Reset MapRot
        var randomRotMap = UseRandomMapPosition ? GetRandomRot() : MapStartingRot;
        FullMap.transform.rotation = randomRotMap;

        // ResetBumps
        Vector3 randomPosBumps = Vector3.zero;
        randomPosBumps = new Vector3(0f, Random.Range(0, bumpsMaxHeight), 0f) * randomDistance + BumpsStartingPos;
        var posBumps = UseRandomBumps ? randomPosBumps : BumpsStartingPos;
        Bumps.transform.localPosition = posBumps;

        //Reset counter
        m_NumberOfRemainingAgents = AgentsList.Count;
        //print($"Reset done");
    }
}
