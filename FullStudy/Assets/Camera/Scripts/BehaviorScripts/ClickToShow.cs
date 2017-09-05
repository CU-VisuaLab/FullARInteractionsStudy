using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using UnityEngine.UI;

public class ClickToShow : MonoBehaviour, IInputClickHandler {
    public GameObject[] objectsToShow;
    public bool logging;
	// Use this for initialization
	void Start () {
        foreach (GameObject obj in objectsToShow)
        {
            obj.SetActive(false);
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnInputClicked (InputEventData eventData)
    {
        if (logging) Debug.Log("Select: Clicked" + gameObject.name + " at " + Time.time);
        foreach (GameObject obj in objectsToShow)
        {
            obj.SetActive(true);
        }
    }
}
