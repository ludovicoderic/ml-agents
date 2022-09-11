using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TrackCheckpointsBall : MonoBehaviour
{
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }
    [Header("Trigger Callbacks")]
    public TriggerEvent OnPlayerCorrectCheckpoint = new TriggerEvent();
    public TriggerEvent OnPlayerWrongCheckpoint = new TriggerEvent();

    private List <CheckpointSingleBall> checkpointSingleList;

    private int nextCheckpointSingleIndex;

    private void Awake()
    {
        Transform checkpointsTransform = transform.Find("Checkpoints");

        checkpointSingleList = new List <CheckpointSingleBall>();
        nextCheckpointSingleIndex = 0;

        foreach (Transform checkpointsSingleTransform in checkpointsTransform) {
            CheckpointSingleBall checkpointSingle = checkpointsSingleTransform.GetComponent<CheckpointSingleBall>();

            checkpointSingle.SetTrackCheckpoints(this);

            checkpointSingleList.Add(checkpointSingle);

            //Debug.Log(checkpointsSingleTransform);
        }

    }

    public void PlayerThroughCheckpoint(CheckpointSingleBall checkpointSingle, Collider m_col)
    {
        if (checkpointSingleList.IndexOf(checkpointSingle) == nextCheckpointSingleIndex)
        {
            // Correct checkpoint
            //Debug.Log($"Checkpoint correcto: {checkpointSingleList.IndexOf(checkpointSingle)}");
            nextCheckpointSingleIndex = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
            OnPlayerCorrectCheckpoint.Invoke(m_col);
        }
        else {
            // Wrong checkpoint
            //Debug.Log($"Checkpoint incorrecto: {checkpointSingleList.IndexOf(checkpointSingle)}");
            OnPlayerWrongCheckpoint.Invoke(m_col);
        }
    }

    public CheckpointSingleBall GetNextCheckpoint(int indice)
    {
        //var nextCheckpointIndex = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;
        //print($"indice: {indice}");
        //print(checkpointSingleList[indice]);
        return checkpointSingleList[indice];
    }

    public void OnEpisodeBegin()
    {
        Awake();
    }
}
