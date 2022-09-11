using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class FinalAgentCooperativeEnviromentClimb : MonoBehaviour
{
    [SerializeField] private UnityEvent ResetCheckpointsEvent; // event to reset checkpoints order
    [System.Serializable]
    public class CheckpointEvent : UnityEvent
    {
    }
    public CheckpointEvent Agent1CorrectCheckpoint = new CheckpointEvent();
    public CheckpointEvent Agent2CorrectCheckpoint = new CheckpointEvent();


        
    [System.Serializable]
    public class PlayerInfo
    {
        public FinalAgentCooperativeClimb Agent;
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
    public Transform Target;
    public Transform Bumps;
    [HideInInspector]
    public Vector3 BumpsStartingPos;

    private bool agent1Goal = false;
    private bool agent2Goal = false;


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

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
    public bool UseRandomBumps = false;
    private float bumpsMaxHeight = 2.5f;
    public float RandomQuantity = 1;

    private FinalAgentCooperativeSettings m_FinalAgentCooperativeSettings;

    private int m_NumberOfRemainingAgents;

    //private SimpleMultiAgentGroup m_AgentGroup;

    private int m_ResetTimer;

    public float PositiveReward = 2f;
    public float FailedReward = -1f;
    public float CorrectCheckpointReward = 0.5f;
    public float WrongCheckpointReward = 0.5f;

    private bool done = false;
    private FinalAgentCooperativeClimb Agent1;
    private FinalAgentCooperativeClimb Agent2;
    //private Collider col1 = null;
    //private Collider col2 = null;

    void Start()
    {
        Agent1 = AgentsList[0].Agent;
        Agent2 = AgentsList[1].Agent;
        agent1Goal = false;
        agent2Goal = false;

        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;
        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();

        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        m_FinalAgentCooperativeSettings = FindObjectOfType<FinalAgentCooperativeSettings>();
        BumpsStartingPos = Bumps.transform.localPosition;

        //Reset Players Remaining
        m_NumberOfRemainingAgents = AgentsList.Count;

        // Initialize TeamManager
        //m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            //item.Agent.GetComponent<Collider>().gameObject.SetActive(true);
           // m_AgentGroup.RegisterAgent(item.Agent);
        }

        ResetScene();
    }

    void FixedUpdate()
    {
        //print($"m_NumberOfRemainingAgents: {m_NumberOfRemainingAgents}");
        //print($"done :{done}");

        // End if time > MaxEnvironmentSteps
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            print($"MaxEnviromentSteps: {FailedReward}");
            endEpisode();
            //m_AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
        // End episode if all the agents get to the goal
        if ((agent1Goal && agent2Goal))
        {
            print($"GOAL ACHIEVED: {PositiveReward}");

            Agent1.AddReward(PositiveReward);
            Agent2.AddReward(PositiveReward);
            StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.goalScoredMaterial, 0.5f));
            endEpisode();
            ResetScene();
        }            

        if (Input.GetKey(KeyCode.R))
        {
            ResetScene();

        }
        Agent1.AddReward(-1 / MaxEnvironmentSteps);
        Agent2.AddReward(-1 / MaxEnvironmentSteps);

        //Hurry Up Penalty
        // m_AgentGroup.AddGroupReward(-0.5f / MaxEnvironmentSteps);
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


    IEnumerator moveAtoB(GameObject gameObjectA, GameObject gameObjectB, float speed)
    {
        while (gameObjectA.transform.position != gameObjectB.transform.position)
        {
            gameObjectA.transform.position = Vector3.MoveTowards(gameObjectA.transform.position, gameObjectB.transform.position, speed * Time.deltaTime);
            yield return null;
        }
    }

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    public void GoalEntered (Collider col)
    {
        print($"GoalEntered with: {col.name}");
        if (col.name == "Agent1")
        {
            agent1Goal = true;


        }
        else if (col.name == "Agent2")
        {
            agent2Goal = true;

        }
    }

    public void GoalExited(Collider col)
    {
        print($"GoalExited with: {col.gameObject.name}");
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
        Debug.Log($"Checkpoint correcto '{checkpoint.name}' cruzado por: '{agent.name}'");
        //Agent1.AddReward(CorrectCheckpointReward);
        //Agent2.AddReward(CorrectCheckpointReward);
        if (agent.name == "Agent1")
        {
            Agent1CorrectCheckpoint.Invoke();
        }
        else if (agent.name == "Agent2")
        {
            Agent2CorrectCheckpoint.Invoke();
        }
    }

    public void WrongCheckpointEntered(Collider checkpoint, Collider agent)
    {
        Debug.Log($"Checkpoint incorrecto '{checkpoint.name}' cruzado por: '{agent.name}'");
        //Agent1.AddReward(WrongCheckpointReward);
    }

    /// <summary>
    /// Called when the agent moves the button into the goal.
    /// </summary>
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
        Agent1.AddReward(PositiveReward);
        Agent2.AddReward(PositiveReward);

        // Swap ground material for a bit to indicate we scored.
        //StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.goalScoredMaterial, 0.5f));
    }

    public void FailedEpisode()
    {
        StartCoroutine(GoalScoredSwapGroundMaterial(m_FinalAgentCooperativeSettings.failMaterial, 0.5f));
        Agent1.AddReward(FailedReward);
        Agent2.AddReward(FailedReward);
        //m_AgentGroup.EndGroupEpisode();
        endEpisode();
        ResetScene();
    }

    public void endEpisode()
    {
        Agent1.EndEpisode();
        Agent2.EndEpisode();

        //m_AgentGroup.EndGroupEpisode();
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
        //agent1Goal = false;
        //agent2Goal = false;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            Vector3 randomPos = new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f)) + item.StartingPos;
            var pos = UseRandomAgentPosition ? randomPos : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;
            //print($"Pos {pos} on Agent");

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.GetComponent<Collider>().gameObject.SetActive(true);
        }

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
