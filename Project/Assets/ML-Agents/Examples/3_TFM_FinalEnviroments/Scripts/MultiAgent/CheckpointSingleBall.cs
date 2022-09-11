using UnityEngine;
using UnityEngine.Events;

public class CheckpointSingleBall : MonoBehaviour
{
    private Collider m_col;

    private TrackCheckpointsBall trackCheckpoints;

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("pushableObject") == true)
        {
            trackCheckpoints.PlayerThroughCheckpoint(this, m_col);
            // Debug.Log("Checkpoint Cruzado");
        }
    }

    public void SetTrackCheckpoints(TrackCheckpointsBall trackCheckpoints)
    {
        this.trackCheckpoints = trackCheckpoints;
    }
    // Start is called before the first frame update
    void Awake()
    {
        m_col = GetComponent<Collider>();
    }
}
