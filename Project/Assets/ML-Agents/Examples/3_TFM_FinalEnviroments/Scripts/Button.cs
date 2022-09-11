using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Button : MonoBehaviour
{
    public bool IsPressed
    {
        get;
        private set;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        {
            IsPressed = true;
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("switchOn") == true || collision.gameObject.layer == 16)
        {
            IsPressed = false;
        }
    }
}
