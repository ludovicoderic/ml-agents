using UnityEngine;
using UnityEngine.Events;

public class CheckpointSingle : MonoBehaviour
{
    //private Collider m_col; // Checkpoint Collider
    private TrackCheckpoints trackCheckpoints;

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("agent") == true)
        {
            trackCheckpoints.PlayerThroughCheckpoint(this, col);
            // Debug.Log("Checkpoint Cruzado");
        }
    }

    public void SetTrackCheckpoints(TrackCheckpoints trackCheckpoints)
    {
        this.trackCheckpoints = trackCheckpoints;
    }

    //private void OnTriggerExit(Collider col)
    //{
    //    if (col.gameObject.CompareTag("agent") == true)
    //    {
    //        Debug.Log("Checkpoint Exited");
    //        //onTriggerExitEvent.Invoke(m_col, GoalValue);
    //    }
    //}
    // Start is called before the first frame update
    //void Awake()
    //{
    //    m_col = GetComponent<Collider>();
    //}
}
