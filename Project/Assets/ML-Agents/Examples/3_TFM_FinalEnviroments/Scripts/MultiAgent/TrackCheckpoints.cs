using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrackCheckpoints : MonoBehaviour
{
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }
    [Header("Trigger Callbacks")]
    public TriggerEvent OnPlayerCorrectCheckpoint = new TriggerEvent();
    public TriggerEvent OnPlayerWrongCheckpoint = new TriggerEvent();

    private List <CheckpointSingle> checkpointSingleList;

    private int nextCheckpointSingleIndex;

    private void Awake()
    {
        Transform checkpointsTransform = transform.Find("Checkpoints");

        checkpointSingleList = new List <CheckpointSingle>();
        nextCheckpointSingleIndex = 0;

        foreach (Transform checkpointsSingleTransform in checkpointsTransform) {
            CheckpointSingle checkpointSingle = checkpointsSingleTransform.GetComponent<CheckpointSingle>();

            checkpointSingle.SetTrackCheckpoints(this);

            checkpointSingleList.Add(checkpointSingle);
        }
    }

    public void PlayerThroughCheckpoint(CheckpointSingle checkpointSingle, Collider m_col)
    {
        if (checkpointSingleList.IndexOf(checkpointSingle) == nextCheckpointSingleIndex)
        {
            // Correct checkpoint
            nextCheckpointSingleIndex = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            OnPlayerCorrectCheckpoint.Invoke(m_col);
        }
        else {
            // Wrong checkpoint
            OnPlayerWrongCheckpoint.Invoke(m_col);
        }
    }

    public void OnEpisodeBegin()
    {
        Awake();
    }
    public CheckpointSingle GetNextCheckpoint(int indice) {
        return checkpointSingleList[indice];
    }
}

