using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrackCheckpointsMulti : MonoBehaviour
{
    // Events
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider, Collider>
    {
    }
    [Header("Trigger Callbacks")]
    public TriggerEvent OnPlayerCorrectCheckpoint = new TriggerEvent();
    public TriggerEvent OnPlayerWrongCheckpoint = new TriggerEvent();

    [SerializeField]
    private List<Transform> agentTransformList;
    private List<CheckpointMulti> checkpointSingleList;
    private List<int> nextCheckpointSingleIndexList;

    private void Awake()
    {
        Transform checkpointsTransform = transform.Find("Checkpoints");

        checkpointSingleList = new List <CheckpointMulti>();

        foreach (Transform checkpointsMultiTransform in checkpointsTransform) {
            CheckpointMulti checkpointMulti = checkpointsMultiTransform.GetComponent<CheckpointMulti>();

            checkpointMulti.SetTrackCheckpoints(this);

            checkpointSingleList.Add(checkpointMulti);

            //Debug.Log(checkpointsSingleTransform);
        }

        nextCheckpointSingleIndexList = new List<int>();
        foreach (Transform agentTransform in agentTransformList) {
            nextCheckpointSingleIndexList.Add(0);
        }
    }

    public void PlayerThroughCheckpoint(CheckpointMulti checkpointMulti, Collider agent, Collider checkpoint)
    {
        int nextCheckpointSingleIndex = nextCheckpointSingleIndexList[agentTransformList.IndexOf(agent.transform)];
        if (checkpointSingleList.IndexOf(checkpointMulti) == nextCheckpointSingleIndex)
        {
            // Correct checkpoint
            //Debug.Log($"Checkpoint correcto: {checkpointSingleList.IndexOf(checkpointMulti)}");
            nextCheckpointSingleIndexList[agentTransformList.IndexOf(agent.transform)]
                = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            OnPlayerCorrectCheckpoint.Invoke(checkpoint, agent);
        }
        else {
            // Wrong checkpoint
            //Debug.Log($"Checkpoint incorrecto: {checkpointSingleList.IndexOf(checkpointMulti)}");
            OnPlayerWrongCheckpoint.Invoke(checkpoint, agent);
        }
    }

    public void OnEpisodeBegin()
    {
        Awake();
    }
}

