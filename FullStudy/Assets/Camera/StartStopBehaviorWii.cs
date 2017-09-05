using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.UI;

public class StartStopBehaviorWii : MonoBehaviour, IInputClickHandler
{
    public bool logging;
    private int colorCounter;
    // Use this for initialization
    void Start () {
        colorCounter = 0;
	}
	
	// Update is called once per frame
	void Update () {

    }

    public void OnInputClicked(InputEventData eventData)
    {
        if (colorCounter++ % 2 == 0)
        {
            if (logging) Debug.Log("*Begin Task at " + Time.time);
            GetComponent<Image>().color = new Color(0, 255, 0, 255);
        }
        else
        {
            Debug.Log("*Finish Task at " + Time.time);
            GetComponent<Image>().color = new Color(255, 0, 0, 255);
        }
    }
}
