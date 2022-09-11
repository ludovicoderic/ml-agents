using UnityEngine;
using UnityEngine.Events;

public class CheckpointMulti : MonoBehaviour {
    private Collider m_col; // Checkpoint Collider

    private TrackCheckpointsMulti trackCheckpointsMulti;

    private void OnTriggerEnter(Collider col) {
        if (col.gameObject.CompareTag("agent") == true) {
            trackCheckpointsMulti.PlayerThroughCheckpoint(this, col, m_col); //Send CheckpointMulti, agentCollider, checkpointCollider
        }
    }

    public void SetTrackCheckpoints(TrackCheckpointsMulti trackCheckpointsMulti){
        this.trackCheckpointsMulti = trackCheckpointsMulti;
    }

    // Start is called before the first frame update
    void Awake()
    {
        m_col = GetComponent<Collider>(); 
    }
}
