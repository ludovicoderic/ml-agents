using UnityEngine;
using UnityEngine.Events;

public class CheckpointMultiBall : MonoBehaviour
{ 
    private Collider m_col; // Checkpoint Collider

    private TrackCheckpointsMultiBall trackCheckpointsMulti;

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("pushableObject") == true)
        {
            trackCheckpointsMulti.PlayerThroughCheckpoint(this, col, m_col); //Send CheckpointMulti, agentCollider, checkpointCollider
        }
    }

    public void SetTrackCheckpoints(TrackCheckpointsMultiBall trackCheckpointsMulti)
    {
        this.trackCheckpointsMulti = trackCheckpointsMulti;
    }

    // Start is called before the first frame update
    void Awake()
    {
        m_col = GetComponent<Collider>();
    }
}
