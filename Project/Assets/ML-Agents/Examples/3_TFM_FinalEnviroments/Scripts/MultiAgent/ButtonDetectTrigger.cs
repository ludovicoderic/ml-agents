using UnityEngine;
using UnityEngine.Events;

public class ButtonDetectTrigger : MonoBehaviour
{

    [Header("Trigger Collider Tag To Detect")]
    public string tagToDetect = "agent"; //collider tag to detect

    [Header("Goal Value")]
    public float GoalValue = 0.01f;

    private Collider m_col;
    [System.Serializable]
    public class TriggerEvent : UnityEvent<Collider>
    {
    }

    [Header("Trigger Callbacks")]
    public TriggerEvent onTriggerEnterEvent = new TriggerEvent();
    public TriggerEvent onTriggerStayEvent = new TriggerEvent();
    public TriggerEvent onTriggerExitEvent = new TriggerEvent();

    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            onTriggerEnterEvent.Invoke(m_col);
        }
    }

    private void OnTriggerStay(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            onTriggerStayEvent.Invoke(m_col);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.CompareTag(tagToDetect))
        {
            onTriggerExitEvent.Invoke(m_col);
        }
    }
    // Start is called before the first frame update
    void Awake()
    {
        m_col = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
