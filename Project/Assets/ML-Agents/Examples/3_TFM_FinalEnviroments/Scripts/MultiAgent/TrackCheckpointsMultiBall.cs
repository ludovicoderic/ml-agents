using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrackCheckpointsMultiBall : MonoBehaviour
{
    // Events
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }
    [Header("Trigger Callbacks")]
    public TriggerEvent OnPlayerCorrectCheckpoint = new TriggerEvent();
    public TriggerEvent OnPlayerWrongCheckpoint = new TriggerEvent();

    [SerializeField]
    private List<Transform> agentTransformList;
    private List<CheckpointMultiBall> checkpointSingleList;
    private List<int> nextCheckpointSingleIndexList;

    private void Awake()
    {
        Transform checkpointsTransform = transform.Find("Checkpoints");

        checkpointSingleList = new List<CheckpointMultiBall>();

        foreach (Transform checkpointsMultiTransform in checkpointsTransform)
        {
            CheckpointMultiBall checkpointMulti = checkpointsMultiTransform.GetComponent<CheckpointMultiBall>();

            checkpointMulti.SetTrackCheckpoints(this);

            checkpointSingleList.Add(checkpointMulti);

            //Debug.Log(checkpointsSingleTransform);
        }

        nextCheckpointSingleIndexList = new List<int>();
        foreach (Transform agentTransform in agentTransformList)
        {
            nextCheckpointSingleIndexList.Add(0);
        }
    }

    public void PlayerThroughCheckpoint(CheckpointMultiBall checkpointMulti, Collider agent, Collider checkpoint)
    {
        int nextCheckpointSingleIndex = nextCheckpointSingleIndexList[agentTransformList.IndexOf(agent.transform)];
        if (checkpointSingleList.IndexOf(checkpointMulti) == nextCheckpointSingleIndex)
        {
            // Correct checkpoint
            //Debug.Log($"Checkpoint correcto: {checkpointSingleList.IndexOf(checkpointMulti)}");
            nextCheckpointSingleIndexList[agentTransformList.IndexOf(agent.transform)]
                = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            OnPlayerCorrectCheckpoint.Invoke(checkpoint);
        }
        else
        {
            // Wrong checkpoint
            //Debug.Log($"Checkpoint incorrecto: {checkpointSingleList.IndexOf(checkpointMulti)}");
            OnPlayerWrongCheckpoint.Invoke(checkpoint);
        }
    }

    public void OnEpisodeBegin()
    {
        Awake();
    }
}
